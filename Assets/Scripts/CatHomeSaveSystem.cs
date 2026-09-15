using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CatHome.Economy;
using CatHome.Quests;
using UnityEngine;

[Serializable]
public sealed class CatHomeSaveData
{
    public int version;
    public float hunger;
    public float thirst;
    public float energy;
    public bool wasSleeping;
    public string lastSaveUtc;
    public bool hasCatPose;
    public Vector3 catWorldPosition;
    public Quaternion catWorldRotation;

    // Version 2: economy and progression. Older saves simply lack these
    // fields; JsonUtility leaves them at 0/null and MigrateSaveData fills in
    // safe defaults.
    //
    // Version 5 moved authority for coins and diamonds into EconomyService and
    // the `economy` section below. These two fields are still written on every
    // save as a legacy mirror, so a rollback to a version 2-4 build keeps the
    // player's wallet; the economy section wins whenever both are present.
    public long coins;
    public long diamonds;
    public long bondXp;
    // Legacy field name retained for version 2-5 save compatibility. Since the
    // progression redesign it stores the active quest chapter number, not a
    // player-facing level or unlock resource.
    public int playerLevel;

    // Version 4 added QuestProgressEntry.state and .completedTimeUtc inside this
    // array. The array itself is unchanged, so the field stays a version 2 field;
    // MigrateSaveData fills the new per-entry values in for older saves.
    public QuestProgressEntry[] questProgress;

    // Version 5: the economy section owned by EconomyService — every currency
    // balance keyed by a stable save key, plus the bounded window of settled
    // transaction ids that prevents a duplicate payout. Older saves lack it and
    // MigrateSaveData builds one from the legacy coins/diamonds fields, so no
    // balance is ever lost and no player progress is reset.
    public EconomySaveState economy;

    // Version 6: Cat Runner entry energy, offline regeneration anchor, rewarded
    // ad daily limit and verified unlimited-pass entitlement.
    public RunnerEnergySaveState runnerEnergy;

    // Version 7: room products purchased with earned coins. This is separate
    // from currency so ownership and price changes never rewrite the wallet.
    public HomeStoreSaveState homeStore;

    // Version 8: Cat Runner best score, guided tutorial/accessibility choices,
    // rotating daily missions and an idempotent pending-result recovery record.
    public CatRunnerProgressSaveState runnerProgress;

    // Version 9: Cat Catch independent lives and best score.
    public CatchLivesSaveState catchLives;
    public int catchBestScore;

    // Version 10: Home progression. Cumulative Home XP earned from home
    // improvements; the Home Level is always derived from it, never stored.
    // Older saves lack it and MigrateSaveData seeds a zero default, so no
    // existing wallet, ownership or need value is touched.
    public HomeProgressionSaveState homeProgression;

    // Version 11: daily login / rotating dailies and one-time achievements.
    // Missing slices migrate to empty defaults; wallets and rooms are untouched.
    public DailyRetentionSaveState dailyRetention;
    public AchievementSaveState achievements;
}

public static class CatHomeSaveSystem
{
    public readonly struct OfflineReturnSummary
    {
        public OfflineReturnSummary(
            long id,
            TimeSpan appliedDuration,
            bool offlineProgressApplied,
            bool wasSleeping,
            float hungerBefore,
            float hungerAfter,
            float thirstBefore,
            float thirstAfter,
            float energyBefore,
            float energyAfter)
        {
            Id = id;
            AppliedDuration = appliedDuration;
            OfflineProgressApplied = offlineProgressApplied;
            WasSleeping = wasSleeping;
            HungerBefore = hungerBefore;
            HungerAfter = hungerAfter;
            ThirstBefore = thirstBefore;
            ThirstAfter = thirstAfter;
            EnergyBefore = energyBefore;
            EnergyAfter = energyAfter;
        }

        public long Id { get; }
        public TimeSpan AppliedDuration { get; }
        public bool OfflineProgressApplied { get; }
        public bool WasSleeping { get; }
        public float HungerBefore { get; }
        public float HungerAfter { get; }
        public float ThirstBefore { get; }
        public float ThirstAfter { get; }
        public float EnergyBefore { get; }
        public float EnergyAfter { get; }
    }

    public const int CurrentSaveVersion = 11;
    public const string SaveFileName = "cat-home-save.json";
    public const string RecoveryFileSuffix = ".recovery";
    public const string NewGameBackupMarker = ".before-new-game-";

    private static HungerSystem hungerSystem;
    private static ThirstSystem thirstSystem;
    private static EnergySystem energySystem;
    private static SleepInteraction sleepInteraction;
    private static CatMovement catMovement;
    private static bool initialized;
    private static bool suspended;
    private static bool suspendedWhileSleeping;
    private static DateTime suspensionSaveUtc;
    private static DateTime lastSuccessfulSaveUtc;
    private static int lastSaveFrame = -1;
    private static long nextSummaryId;
    private static long lastPresentedSummaryId;
    private static OfflineReturnSummary latestSummary;
    private static bool hasLatestSummary;
    private static CatHomeSaveData pendingImportedData;

    public static string SaveFilePath =>
        Path.Combine(EditorQaSession.SaveDirectory, SaveFileName);

    public static string RecoveryFilePath => SaveFilePath + RecoveryFileSuffix;

    public static int PeekLegacyChapterNumber()
    {
        return TryReadSave(out CatHomeSaveData data)
            ? Mathf.Max(1, data.playerLevel)
            : 1;
    }

    /// <summary>
    /// Restores the home-store/room slice before LevelLoader chooses the first
    /// additive room. The complete save is still applied by Initialize once the
    /// room cat exists; this early, idempotent pass prevents a one-room flash and
    /// lets ownership validate a saved Bathroom selection.
    /// </summary>
    public static string PrepareRoomBootstrap()
    {
        if (TryReadSave(out CatHomeSaveData data))
            HomeStoreService.ApplySavedState(data.homeStore);
        else
            HomeRoomService.ApplySavedRoomId(HomeRoomService.LivingRoomId);

        return HomeRoomService.CurrentRoomId;
    }

    public static TimeSpan AppliedOfflineDuration { get; private set; }
    public static bool OfflineProgressApplied { get; private set; }
    public static event Action<OfflineReturnSummary> OfflineSummaryReady;
    public static event Action LocalSaveWritten;

    public static bool TryGetLatestOfflineSummary(out OfflineReturnSummary summary)
    {
        summary = latestSummary;
        return hasLatestSummary;
    }

    public static bool TryMarkOfflineSummaryPresented(long summaryId)
    {
        if (!hasLatestSummary || summaryId <= lastPresentedSummaryId ||
            summaryId != latestSummary.Id)
        {
            return false;
        }

        lastPresentedSummaryId = summaryId;
        return true;
    }

    public static void Initialize(CatMovement movement)
    {
        if (initialized)
        {
            // Additive room transitions replace the room-owned cat while the UI
            // need systems persist. Rebind only; never replay offline progress,
            // quest state or save rewards during a room change.
            if (movement != null && movement != catMovement)
            {
                catMovement = movement;
                sleepInteraction = movement.GetComponent<SleepInteraction>();
                if (hungerSystem == null)
                    hungerSystem = UnityEngine.Object.FindAnyObjectByType<HungerSystem>();
                if (thirstSystem == null)
                    thirstSystem = UnityEngine.Object.FindAnyObjectByType<ThirstSystem>();
                if (energySystem == null)
                    energySystem = UnityEngine.Object.FindAnyObjectByType<EnergySystem>();
                if (pendingImportedData != null && hungerSystem != null &&
                    thirstSystem != null && energySystem != null)
                {
                    CatHomeSaveData imported = pendingImportedData;
                    pendingImportedData = null;
                    ApplyLoadedValues(imported);
                    ApplyImportedCatState(imported);
                }
            }
            return;
        }

        catMovement = movement;
        hungerSystem = UnityEngine.Object.FindAnyObjectByType<HungerSystem>();
        thirstSystem = UnityEngine.Object.FindAnyObjectByType<ThirstSystem>();
        energySystem = UnityEngine.Object.FindAnyObjectByType<EnergySystem>();
        sleepInteraction = movement != null
            ? movement.GetComponent<SleepInteraction>()
            : null;

        if (catMovement == null || hungerSystem == null ||
            thirstSystem == null || energySystem == null)
        {
            // A room opened directly in the Editor starts its cat one frame
            // before GameScene and the shared need UI finish loading additively.
            // LevelLoader retries initialization after that hand-off, so this
            // expected pre-bootstrap pass must not pollute the Console.
            UnityEngine.SceneManagement.Scene sharedUi =
                UnityEngine.SceneManagement.SceneManager.GetSceneByPath(
                    LevelLoader.DefaultUiScenePath);
            if (sharedUi.IsValid() && sharedUi.isLoaded)
            {
                Debug.LogWarning(
                    "CatHomeSaveSystem could not initialize because the cat or a need component is missing."
                );
            }
            ClearReferences();
            return;
        }

        initialized = true;
        AppliedOfflineDuration = TimeSpan.Zero;
        OfflineProgressApplied = false;

        if (!TryReadSave(out CatHomeSaveData data))
            return;

        ApplyLoadedValues(data);

        if (data.wasSleeping)
        {
            RestoreSavedSleepingState();
        }
        else if (data.hasCatPose && IsFinite(data.catWorldPosition) &&
                 IsValidRotation(data.catWorldRotation))
        {
            catMovement.ApplySavedWorldPose(
                data.catWorldPosition,
                data.catWorldRotation.normalized
            );
        }

        if (!TryParseUtc(data.lastSaveUtc, out DateTime savedAtUtc))
        {
            Debug.LogWarning(
                "CatHomeSaveSystem loaded need values, but the UTC save timestamp was invalid. " +
                "Offline progress was skipped."
            );
            return;
        }

        ApplyOfflineProgress(savedAtUtc, data.wasSleeping, DateTime.UtcNow);
    }

    private static void RestoreSavedSleepingState()
    {
        string failureReason = null;
        if (sleepInteraction != null &&
            sleepInteraction.TryRestoreSleepingState(out failureReason))
        {
            return;
        }

        catMovement.SetMovementLocked(false);
        string reason = sleepInteraction == null
            ? "SleepInteraction is missing."
            : failureReason;
        Debug.LogWarning(
            "CatHomeSaveSystem could not restore the saved sleeping state. " +
            $"The cat started awake and movable instead. {reason}"
        );
    }

    public static void SaveNow(bool forceSameFrame = false)
    {
        if (!CanCaptureData() ||
            (!forceSameFrame && lastSaveFrame == Time.frameCount))
            return;

        DateTime savedAtUtc = DateTime.UtcNow;
        CatHomeSaveData data = CaptureData(savedAtUtc);

        if (TryWriteSaveData(data))
        {
            lastSuccessfulSaveUtc = savedAtUtc;
            lastSaveFrame = Time.frameCount;
            LocalSaveWritten?.Invoke();
        }
    }

    /// <summary>
    /// Starts a confirmed fresh journey while preserving settings, diamonds,
    /// verified unlimited-pass windows and settled purchase transaction ids.
    /// The existing file is copied to a timestamped support backup first.
    /// </summary>
    public static bool TryStartNewGame(out string report)
    {
        if (!CanCaptureData())
        {
            report = "The home is still loading. NEW GAME did not change the save.";
            return false;
        }

        DateTime utcNow = DateTime.UtcNow;
        if (!TryCreateNewGameBackup(SaveFilePath, utcNow, out string backupPath,
                out string backupFailure))
        {
            report = "The safety backup could not be created. NEW GAME was cancelled. " +
                     backupFailure;
            return false;
        }

        CatHomeSaveData reset = CreateNewGameData(
            utcNow,
            EconomyService.CaptureState(),
            RunnerEnergyService.CaptureState(utcNow),
            CatRunnerProgressService.CaptureState(utcNow),
            CatchLivesService.CaptureState(utcNow));

        if (!TryWriteSaveData(reset))
        {
            report = "The fresh save could not be written. Your current journey is safe.";
            return false;
        }

        ApplyNewGameRuntime(reset, utcNow);
        lastSuccessfulSaveUtc = utcNow;
        lastSaveFrame = Time.frameCount;
        LocalSaveWritten?.Invoke();
        report = string.IsNullOrEmpty(backupPath)
            ? "NEW GAME started."
            : "NEW GAME started. Safety backup: " + backupPath;
        return true;
    }

    private static CatHomeSaveData CreateNewGameData(
        DateTime utcNow,
        EconomySaveState currentEconomy,
        RunnerEnergySaveState currentRunnerEnergy,
        CatRunnerProgressSaveState currentRunnerProgress,
        CatchLivesSaveState currentCatchLives)
    {
        long preservedDiamonds = GetSavedBalance(
            currentEconomy,
            CurrencyCatalog.GetSaveKey(CurrencyType.Diamond),
            EconomyService.GetBalance(CurrencyType.Diamond));
        var runnerEnergy = RunnerEnergySaveState.CreateDefault(utcNow);
        if (currentRunnerEnergy != null)
            runnerEnergy.unlimitedUntilUtc = currentRunnerEnergy.unlimitedUntilUtc;

        var runnerProgress = CatRunnerProgressSaveState.CreateDefault(utcNow);
        runnerProgress.bestScore = 0;
        if (currentRunnerProgress != null)
        {
            runnerProgress.reducedMotion = currentRunnerProgress.reducedMotion;
            runnerProgress.soundEnabled = currentRunnerProgress.soundEnabled;
            runnerProgress.hapticsEnabled = currentRunnerProgress.hapticsEnabled;
        }

        var catchLives = CatchLivesSaveState.CreateDefault(utcNow);
        if (currentCatchLives != null)
            catchLives.unlimitedUntilUtc = currentCatchLives.unlimitedUntilUtc;

        string[] settledTransactions = currentEconomy?.processedTransactionIds == null
            ? Array.Empty<string>()
            : (string[])currentEconomy.processedTransactionIds.Clone();

        return new CatHomeSaveData
        {
            version = CurrentSaveVersion,
            hunger = 100f,
            thirst = 100f,
            energy = 100f,
            wasSleeping = false,
            lastSaveUtc = utcNow.ToString("O", CultureInfo.InvariantCulture),
            hasCatPose = false,
            coins = 0L,
            diamonds = preservedDiamonds,
            bondXp = 0L,
            playerLevel = 1,
            questProgress = Array.Empty<QuestProgressEntry>(),
            economy = new EconomySaveState
            {
                economyVersion = EconomyService.SaveVersion,
                balances = BuildResetBalances(preservedDiamonds),
                processedTransactionIds = settledTransactions
            },
            runnerEnergy = runnerEnergy,
            homeStore = HomeStoreSaveState.CreateDefault(),
            runnerProgress = runnerProgress,
            catchLives = catchLives,
            catchBestScore = 0,
            homeProgression = HomeProgressionSaveState.CreateDefault(),
            dailyRetention = DailyRetentionSaveState.CreateDefault(),
            achievements = AchievementSaveState.CreateDefault()
        };
    }

    private static void ApplyNewGameRuntime(CatHomeSaveData data, DateTime utcNow)
    {
        sleepInteraction?.ForceAwakeForNewGame();
        hungerSystem.ApplySavedValue(100f);
        thirstSystem.ApplySavedValue(100f);
        energySystem.ApplySavedValue(100f);
        catMovement.ApplySavedWorldPose(new Vector3(0f, 0f, -2f), Quaternion.identity);

        ProgressionService.ApplySavedState(
            data.coins, data.diamonds, data.bondXp, data.playerLevel, data.questProgress);
        EconomyService.ApplySavedState(data.economy);
        RunnerEnergyService.ApplySavedState(data.runnerEnergy, utcNow);
        HomeStoreService.ApplySavedState(data.homeStore);
        CatRunnerProgressService.ApplySavedState(data.runnerProgress, utcNow);
        CatchLivesService.ApplySavedState(data.catchLives, utcNow);
        CatCatchGameController.ApplyBestScore(0);
        HomeProgressionService.ApplySavedState(data.homeProgression);
        DailyRetentionService.ApplySavedState(data.dailyRetention);
        AchievementService.ApplySavedState(data.achievements);

        PetTutorialHint tutorial = UnityEngine.Object.FindAnyObjectByType<PetTutorialHint>(
            FindObjectsInactive.Include);
        if (tutorial != null)
            tutorial.ResetTutorialProgress();
        else
            PetTutorialHint.ClearProgressKeys();
        CatIdentityService.ResetForNewGame();
        CollectionMilestoneService.ClearAll();
        LocalNotificationService.Clear();
        PlayerPrefs.DeleteKey(CatRunnerProgressService.LegacyBestScoreKey);
        PlayerPrefs.Save();

        LevelLoader loader = UnityEngine.Object.FindAnyObjectByType<LevelLoader>(
            FindObjectsInactive.Include);
        if (loader != null)
            loader.LoadRoom(HomeRoomService.LivingRoomId);
    }

    private static CurrencyBalanceEntry[] BuildResetBalances(long preservedDiamonds)
    {
        IReadOnlyList<CurrencyDefinition> definitions = CurrencyCatalog.All;
        var entries = new CurrencyBalanceEntry[definitions.Count];
        for (int i = 0; i < definitions.Count; i++)
        {
            long amount = definitions[i].Type == CurrencyType.Diamond
                ? Math.Max(0L, preservedDiamonds)
                : 0L;
            entries[i] = new CurrencyBalanceEntry(definitions[i].SaveKey, amount);
        }
        return entries;
    }

    private static long GetSavedBalance(
        EconomySaveState state, string currencyKey, long fallback)
    {
        if (state?.balances == null || string.IsNullOrEmpty(currencyKey))
            return Math.Max(0L, fallback);
        for (int i = 0; i < state.balances.Length; i++)
        {
            CurrencyBalanceEntry entry = state.balances[i];
            if (entry != null && string.Equals(
                    entry.currencyKey, currencyKey, StringComparison.Ordinal))
                return Math.Max(0L, entry.amount);
        }
        return Math.Max(0L, fallback);
    }

    private static bool TryCreateNewGameBackup(
        string savePath, DateTime utcNow, out string backupPath, out string failure)
    {
        backupPath = string.Empty;
        failure = string.Empty;
        if (!File.Exists(savePath))
            return true;
        try
        {
            string stamp = utcNow.ToUniversalTime().ToString(
                "yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture);
            backupPath = savePath + NewGameBackupMarker + stamp;
            File.Copy(savePath, backupPath, false);
            return true;
        }
        catch (Exception exception)
        {
            backupPath = string.Empty;
            failure = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Atomically writes the given save data through the temporary-file and
    /// replace path shared by every writer. Returns false (and logs) if the
    /// write failed, leaving any existing save untouched.
    /// </summary>
    private static bool TryWriteSaveData(CatHomeSaveData data)
    {
        return TryWriteSaveDataAtPath(data, SaveFilePath);
    }

    private static bool TryWriteSaveDataAtPath(CatHomeSaveData data, string savePath)
    {
        try
        {
            string directory = Path.GetDirectoryName(savePath);
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("The save path has no parent directory.");
            Directory.CreateDirectory(directory);

            string temporaryPath = savePath + ".tmp";
            string previousPath = savePath + ".previous";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
            ReplaceFileSafely(temporaryPath, savePath, previousPath);
            TryRefreshRecoveryCopy(savePath, savePath + RecoveryFileSuffix);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"CatHomeSaveSystem could not write the local save: {exception.Message}"
            );
            return false;
        }
    }

    /// <summary>
    /// Returns the validated, migrated local save used by Cloud Save. The JSON
    /// leaves account identifiers and service tokens out because those are never
    /// fields in <see cref="CatHomeSaveData"/>.
    /// </summary>
    public static bool TryExportLocalSave(out string json, out CatHomeSaveData data)
    {
        json = string.Empty;
        data = null;
        if (!TryReadSave(out data))
            return false;
        json = JsonUtility.ToJson(data, true);
        return !string.IsNullOrWhiteSpace(json);
    }

    /// <summary>
    /// Preserves the cloud copy before a seamless newest-save decision overwrites
    /// it with this device. This support-only shadow is never loaded automatically.
    /// </summary>
    public static bool TryPreserveCloudShadow(string json, out string backupPath)
    {
        backupPath = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            string stamp = DateTime.UtcNow.ToString(
                "yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture);
            backupPath = SaveFilePath + ".cloud-shadow-" + stamp;
            File.WriteAllText(backupPath, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Cat Home could not preserve the older cloud shadow: " +
                             exception.Message);
            backupPath = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Validates and atomically installs the automatically selected cloud copy. A support
    /// backup is taken before replacement. The running home is then updated; a
    /// room change is delegated to LevelLoader so camera/event-system ownership
    /// remains canonical.
    /// </summary>
    public static bool TryImportCloudSave(string json, out string report)
    {
        report = string.Empty;
        if (string.IsNullOrWhiteSpace(json))
        {
            report = "The cloud save was empty.";
            return false;
        }

        CatHomeSaveData data;
        try
        {
            data = JsonUtility.FromJson<CatHomeSaveData>(json);
        }
        catch (Exception exception)
        {
            report = "The cloud save could not be read: " + exception.Message;
            return false;
        }

        if (data == null || data.version <= 0 || data.version > CurrentSaveVersion)
        {
            report = data != null && data.version > CurrentSaveVersion
                ? $"Cloud save v{data.version} is newer than supported v{CurrentSaveVersion}."
                : "The cloud save did not contain a supported Cat Home profile.";
            return false;
        }
        if (data.version < CurrentSaveVersion)
            MigrateSaveData(data);

        string backupPath = string.Empty;
        if (File.Exists(SaveFilePath))
        {
            string stamp = DateTime.UtcNow.ToString(
                "yyyyMMdd-HHmmss-fffffff", CultureInfo.InvariantCulture);
            backupPath = SaveFilePath + ".before-cloud-" + stamp;
            try
            {
                File.Copy(SaveFilePath, backupPath, false);
            }
            catch (Exception exception)
            {
                report = "The local safety backup failed; cloud import was cancelled. " +
                         exception.Message;
                return false;
            }
        }

        if (!TryWriteSaveData(data))
        {
            report = "The cloud save could not replace the local copy.";
            return false;
        }

        if (initialized && CanCaptureData())
        {
            string roomBefore = HomeRoomService.CurrentRoomId;
            ApplyLoadedValues(data);
            string roomAfter = HomeRoomService.CurrentRoomId;
            if (string.Equals(roomBefore, roomAfter, StringComparison.Ordinal))
            {
                ApplyImportedCatState(data);
            }
            else
            {
                pendingImportedData = data;
                LevelLoader loader = UnityEngine.Object.FindAnyObjectByType<LevelLoader>(
                    FindObjectsInactive.Include);
                loader?.LoadRoom(roomAfter);
            }
        }

        lastSuccessfulSaveUtc = DateTime.UtcNow;
        report = string.IsNullOrEmpty(backupPath)
            ? "Cloud save applied."
            : "Cloud save applied. Local safety backup: " + backupPath;
        return true;
    }

    private static void ApplyImportedCatState(CatHomeSaveData data)
    {
        if (data.wasSleeping)
            RestoreSavedSleepingState();
        else if (data.hasCatPose && IsFinite(data.catWorldPosition) &&
                 IsValidRotation(data.catWorldRotation))
            catMovement.ApplySavedWorldPose(
                data.catWorldPosition, data.catWorldRotation.normalized);
    }

    public static void SaveForSuspension()
    {
        if (!initialized)
            return;

        SaveNow();
        suspended = true;
        suspendedWhileSleeping = sleepInteraction != null && sleepInteraction.IsSleeping;
        suspensionSaveUtc = lastSuccessfulSaveUtc == default
            ? DateTime.UtcNow
            : lastSuccessfulSaveUtc;
    }

    public static void ResumeAfterSuspension()
    {
        if (!initialized || !suspended)
            return;

        suspended = false;
        ApplyOfflineProgress(
            suspensionSaveUtc,
            suspendedWhileSleeping,
            DateTime.UtcNow
        );
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor/test-only helper that resets ONLY the economy and progression
    /// portion of the on-disk save (player level to 1; coins, diamonds, bond XP
    /// to 0; quest progress cleared) while preserving every other field: needs,
    /// cat pose, sleep state, save timestamp and version. It performs a
    /// controlled read -> mutate -> safe-write on the save file through the same
    /// atomic path as normal saves instead of fragile text edits, and refuses to
    /// run while the game is playing. Wrapped in UNITY_EDITOR so it can never be
    /// reached from a runtime release build.
    /// </summary>
    /// <param name="report">Human-readable outcome for the Console.</param>
    /// <returns>True when the save was rewritten; false when nothing changed.</returns>
    public static bool EditorResetProgressionData(out string report)
    {
        if (Application.isPlaying)
        {
            report = "Reset Progression Test Data was blocked because the game is in Play Mode.";
            return false;
        }

        if (!TryReadSave(out CatHomeSaveData data))
        {
            report =
                $"Reset Progression Test Data found no readable save at '{SaveFilePath}'. " +
                "Nothing was written.";
            return false;
        }

        long previousCoins = data.coins;
        long previousDiamonds = data.diamonds;
        long previousBondXp = data.bondXp;
        int previousChapter = data.playerLevel;
        long previousHomeXp = data.homeProgression?.homeXp ?? 0L;
        int clearedQuestEntries = data.questProgress?.Length ?? 0;

        // Only the progression subset is touched; every other loaded field is
        // written back exactly as it was read.
        data.version = CurrentSaveVersion;
        data.coins = 0;
        data.diamonds = 0;
        data.bondXp = 0;
        data.playerLevel = 1;
        data.questProgress = Array.Empty<QuestProgressEntry>();
        data.homeProgression = HomeProgressionSaveState.CreateDefault();
        data.dailyRetention = DailyRetentionSaveState.CreateDefault();
        data.achievements = AchievementSaveState.CreateDefault();
        CollectionMilestoneService.ClearAll();
        LocalNotificationService.Clear();

        // Zeroed explicitly rather than left null: an empty economy section means
        // "keep what is loaded" on the load path, which would preserve exactly the
        // balances this reset is meant to clear. The settled-transaction window is
        // dropped with it so the reset quests can be claimed and paid again.
        data.economy = new EconomySaveState
        {
            economyVersion = EconomyService.SaveVersion,
            balances = BuildZeroedBalances(),
            processedTransactionIds = Array.Empty<string>(),
        };

        if (!TryWriteSaveData(data))
        {
            report =
                "Reset Progression Test Data could not write the updated save; " +
                "the existing save file was left unchanged.";
            return false;
        }

        report =
            "Reset Progression Test Data complete. Progression cleared " +
            $"(quest chapter {previousChapter} -> 1, coins {previousCoins} -> 0, " +
            $"diamonds {previousDiamonds} -> 0, bond XP {previousBondXp} -> 0, " +
            $"home XP {previousHomeXp} -> 0, " +
            $"{clearedQuestEntries} quest progress " +
            $"entr{(clearedQuestEntries == 1 ? "y" : "ies")} removed). " +
            "Hunger, thirst, energy, cat pose, sleep state, onboarding, pet tutorial " +
            "and offline-progress data were preserved.";
        return true;
    }

    /// <summary>Every registered currency at zero, for the editor reset above.</summary>
    private static CurrencyBalanceEntry[] BuildZeroedBalances()
    {
        return BuildResetBalances(0L);
    }
#endif

    private static CatHomeSaveData CaptureData(DateTime savedAtUtc)
    {
        Transform catTransform = catMovement.transform;
        return new CatHomeSaveData
        {
            version = CurrentSaveVersion,
            hunger = hungerSystem.CurrentHunger,
            thirst = thirstSystem.CurrentThirst,
            energy = energySystem.CurrentEnergy,
            wasSleeping = sleepInteraction != null && sleepInteraction.IsSleeping,
            lastSaveUtc = savedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            hasCatPose = true,
            catWorldPosition = catTransform.position,
            catWorldRotation = catTransform.rotation,
            // Legacy mirror of the two currencies that existed before the
            // economy section; the section below is the authority on load.
            coins = EconomyService.GetBalance(CurrencyType.Coin),
            diamonds = EconomyService.GetBalance(CurrencyType.Diamond),
            bondXp = ProgressionService.BondXp,
            playerLevel = ProgressionService.CurrentChapterNumber,
            questProgress = ProgressionService.CaptureQuestProgress(),
            economy = EconomyService.CaptureState(),
            runnerEnergy = RunnerEnergyService.CaptureState(savedAtUtc),
            homeStore = HomeStoreService.CaptureState(),
            runnerProgress = CatRunnerProgressService.CaptureState(savedAtUtc),
            catchLives = CatchLivesService.CaptureState(savedAtUtc),
            catchBestScore = CatCatchGameController.CaptureBestScore(),
            homeProgression = HomeProgressionService.CaptureState(),
            dailyRetention = DailyRetentionService.CaptureState(),
            achievements = AchievementService.CaptureState()
        };
    }

    private static bool CanCaptureData()
    {
        if (!initialized)
            return false;

        // Scene teardown destroys Unity objects before every quit/save callback
        // has necessarily finished. Unity's overloaded null check also catches
        // those destroyed references, so a late editor or application save can
        // safely become a no-op instead of dereferencing a stale component.
        if (catMovement != null && hungerSystem != null &&
            thirstSystem != null && energySystem != null)
        {
            return true;
        }

        ClearReferences();
        return false;
    }

    private static bool TryReadSave(out CatHomeSaveData data)
    {
        return TryReadSaveAtPath(SaveFilePath, out data);
    }

    private enum SaveReadFailure
    {
        None,
        Unreadable,
        Incompatible
    }

    private static bool TryReadSaveAtPath(string savePath, out CatHomeSaveData data)
    {
        data = null;
        string recoveryPath = savePath + RecoveryFileSuffix;

        if (!File.Exists(savePath))
        {
            string previousPath = savePath + ".previous";
            if (File.Exists(previousPath))
            {
                try
                {
                    File.Move(previousPath, savePath);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        $"CatHomeSaveSystem could not recover the previous save: {exception.Message}"
                    );
                    return false;
                }
            }
            else if (TryReadSaveFile(recoveryPath, out data, out SaveReadFailure recoveryFailure,
                         out string recoveryReason) &&
                     TryRestoreRecoveryCopy(recoveryPath, savePath))
            {
                Debug.LogWarning(
                    "CatHomeSaveSystem restored the missing local save from its recovery copy."
                );
                return true;
            }
            else
            {
                if (File.Exists(recoveryPath) && recoveryFailure != SaveReadFailure.None)
                    Debug.LogWarning(
                        $"CatHomeSaveSystem could not use the recovery copy: {recoveryReason}"
                    );
                return false;
            }
        }

        if (TryReadSaveFile(savePath, out data, out SaveReadFailure failure,
                out string failureReason))
            return true;

        if (failure == SaveReadFailure.Incompatible)
        {
            Debug.LogWarning(failureReason);
            BackupRejectedSave(savePath, "incompatible");
            return false;
        }

        Debug.LogWarning(
            $"CatHomeSaveSystem ignored an unreadable local save: {failureReason}"
        );
        BackupCorruptSave(savePath);

        if (!TryReadSaveFile(recoveryPath, out CatHomeSaveData recovered,
                out _, out string recoveryReadReason))
        {
            if (File.Exists(recoveryPath))
                Debug.LogWarning(
                    $"CatHomeSaveSystem could not use the recovery copy: {recoveryReadReason}"
                );
            return false;
        }

        if (!TryRestoreRecoveryCopy(recoveryPath, savePath))
            return false;

        data = recovered;
        Debug.LogWarning(
            "CatHomeSaveSystem restored the last successful local save from its recovery copy."
        );
        return true;
    }

    private static bool TryReadSaveFile(
        string path,
        out CatHomeSaveData data,
        out SaveReadFailure failure,
        out string failureReason)
    {
        data = null;
        failure = SaveReadFailure.None;
        failureReason = string.Empty;
        if (!File.Exists(path))
            return false;

        try
        {
            data = JsonUtility.FromJson<CatHomeSaveData>(File.ReadAllText(path));
            if (data == null)
                throw new InvalidDataException("The JSON did not contain save data.");

            if (data.version > CurrentSaveVersion)
            {
                failure = SaveReadFailure.Incompatible;
                failureReason =
                    $"CatHomeSaveSystem cannot load save version {data.version} because it is " +
                    $"newer than the supported version {CurrentSaveVersion}. The file was backed " +
                    "up and profile defaults are used.";
                data = null;
                return false;
            }

            if (data.version < CurrentSaveVersion)
                MigrateSaveData(data);
            return true;
        }
        catch (Exception exception)
        {
            failure = SaveReadFailure.Unreadable;
            failureReason = exception.Message;
            data = null;
            return false;
        }
    }

    /// <summary>
    /// Upgrades an older, accepted save in memory. A missing or zero version
    /// field is treated as version 1 (the version field has existed since the
    /// first release). Existing need, pose, sleep and timestamp fields are
    /// left untouched so v1 data survives completely.
    /// </summary>
    private static void MigrateSaveData(CatHomeSaveData data)
    {
        int loadedVersion = data.version < 1 ? 1 : data.version;

        if (loadedVersion < 2)
        {
            // v1 -> v2: economy and progression fields did not exist yet.
            if (data.coins < 0)
                data.coins = 0;
            if (data.diamonds < 0)
                data.diamonds = 0;
            if (data.bondXp < 0)
                data.bondXp = 0;
            if (data.playerLevel < 1)
                data.playerLevel = 1;
            data.questProgress ??= Array.Empty<QuestProgressEntry>();
        }

        if (loadedVersion < 4)
            MigrateQuestProgressToStates(data);

        if (loadedVersion < 5)
            MigrateEconomySection(data);

        if (loadedVersion < 6)
            data.runnerEnergy = RunnerEnergySaveState.CreateDefault(DateTime.UtcNow);

        if (loadedVersion < 7)
            data.homeStore = HomeStoreSaveState.CreateDefault();

        if (loadedVersion < 8)
            data.runnerProgress = CatRunnerProgressSaveState.CreateDefault(DateTime.UtcNow);

        if (loadedVersion < 9)
        {
            data.catchLives = CatchLivesSaveState.CreateDefault(DateTime.UtcNow);
            data.catchBestScore = 0;
        }

        if (loadedVersion < 10)
            data.homeProgression = HomeProgressionSaveState.CreateDefault();

        if (loadedVersion < 11)
        {
            data.dailyRetention = DailyRetentionSaveState.CreateDefault();
            data.achievements = AchievementSaveState.CreateDefault();
        }

        data.version = CurrentSaveVersion;
        Debug.Log(
            $"CatHomeSaveSystem migrated the local save from version {loadedVersion} " +
            $"to version {CurrentSaveVersion} with safe defaults for the new fields."
        );
    }

    /// <summary>
    /// v3 -> v4: gives every saved quest entry an explicit lifecycle state.
    ///
    /// A completed entry becomes <see cref="QuestState.Claimed"/>, never
    /// Completed: ProgressionService grants the reward the moment a quest
    /// completes, so these rewards are already paid. Mapping them to Completed
    /// would let the explicit claim step pay for them a second time.
    ///
    /// An unfinished entry becomes <see cref="QuestState.Active"/>. It must never
    /// be left at the enum default, because QuestState.Locked is 0 and nothing in
    /// the project transitions Locked -> Active, which would strand the quest.
    ///
    /// completedTimeUtc is deliberately left empty ("unknown"): older saves never
    /// recorded one, and any substituted value would be fabricated.
    /// </summary>
    private static void MigrateQuestProgressToStates(CatHomeSaveData data)
    {
        if (data.questProgress == null)
            return;

        foreach (QuestProgressEntry entry in data.questProgress)
        {
            if (entry == null)
                continue;

            entry.state = entry.completed ? QuestState.Claimed : QuestState.Active;
            entry.ClearCompletedTimeUtc();
        }
    }

    /// <summary>
    /// v4 -> v5: gives the save an economy section.
    ///
    /// The balances are copied from the legacy coins/diamonds fields, which is
    /// exactly what the player had, so nothing is granted and nothing is lost.
    /// The transaction id window starts empty: no transaction was ever settled
    /// through the economy before this version, so there is nothing to replay.
    ///
    /// The legacy fields themselves are deliberately left in place. They keep
    /// being written by every save from now on as a mirror, so downgrading to a
    /// pre-economy build still finds the player's wallet.
    /// </summary>
    private static void MigrateEconomySection(CatHomeSaveData data)
    {
        if (data.economy != null && data.economy.HasBalances)
            return;

        long migratedCoins = data.coins < 0 ? 0 : data.coins;
        long migratedDiamonds = data.diamonds < 0 ? 0 : data.diamonds;

        data.economy = new EconomySaveState
        {
            economyVersion = EconomyService.SaveVersion,
            balances = new[]
            {
                new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Coin), migratedCoins),
                new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Diamond), migratedDiamonds),
            },
            processedTransactionIds = Array.Empty<string>(),
        };

        Debug.Log(
            "CatHomeSaveSystem migrated the wallet into the economy section " +
            $"({migratedCoins.ToString(CultureInfo.InvariantCulture)} coins, " +
            $"{migratedDiamonds.ToString(CultureInfo.InvariantCulture)} diamonds). " +
            "Nothing was granted and nothing was reset."
        );
    }

    private static void ApplyLoadedValues(CatHomeSaveData data)
    {
        hungerSystem.ApplySavedValue(SanitizeNeed(data.hunger, hungerSystem.CurrentHunger));
        thirstSystem.ApplySavedValue(SanitizeNeed(data.thirst, thirstSystem.CurrentThirst));
        energySystem.ApplySavedValue(SanitizeNeed(data.energy, energySystem.CurrentEnergy));

        // Pure state restore: ProgressionService never raises quest events or
        // grants rewards from this call. It seeds the wallet from the legacy
        // coin/diamond mirror...
        ProgressionService.ApplySavedState(
            data.coins,
            data.diamonds,
            data.bondXp,
            data.playerLevel,
            data.questProgress
        );

        // ...and the authoritative economy section is applied on top, so it
        // always wins when both are present. A missing or empty section leaves
        // the seeded balances untouched rather than zeroing a wallet.
        EconomyService.ApplySavedState(data.economy);
        RunnerEnergyService.ApplySavedState(data.runnerEnergy, DateTime.UtcNow);
        HomeStoreService.ApplySavedState(data.homeStore);
        CatRunnerProgressService.ApplySavedState(data.runnerProgress, DateTime.UtcNow);
        CatchLivesService.ApplySavedState(data.catchLives, DateTime.UtcNow);
        CatCatchGameController.ApplyBestScore(data.catchBestScore);
        HomeProgressionService.ApplySavedState(data.homeProgression);
        DailyRetentionService.ApplySavedState(data.dailyRetention);
        AchievementService.ApplySavedState(data.achievements);
        DailyRetentionService.NotifySessionStart();
        AchievementService.Evaluate();
        // Back-fill Home XP for products acquired before the Home XP slice existed
        // (their purchase-time grant predates the save field). Ownership is already
        // restored above, and this never lowers a saved value.
        HomeProgressionService.EnsureFloor(HomeStoreService.SumOwnedHomeXp());
        CollectionMilestoneService.ScanOwned();

        // A completed run is recorded before payout. Clearing that record before
        // the idempotent economy grant means the grant's immediate save contains
        // both the settled transaction id and the cleared pending record.
        CatRunnerProgressService.TrySettlePendingResult(out _);
    }

    private static void ApplyOfflineProgress(
        DateTime savedAtUtc,
        bool wasSleeping,
        DateTime currentUtc)
    {
        AppliedOfflineDuration = TimeSpan.Zero;
        OfflineProgressApplied = false;

        float hungerBefore = hungerSystem.CurrentHunger;
        float thirstBefore = thirstSystem.CurrentThirst;
        float energyBefore = energySystem.CurrentEnergy;

        double elapsedSeconds = (currentUtc - savedAtUtc).TotalSeconds;
        if (elapsedSeconds < 0d)
        {
            Debug.LogWarning(
                "CatHomeSaveSystem detected that the device UTC clock moved backwards. " +
                "Offline progress was treated as zero."
            );
            PublishOfflineSummary(
                wasSleeping,
                hungerBefore,
                thirstBefore,
                energyBefore
            );
            return;
        }

        if (elapsedSeconds <= 0d ||
            !GameBalanceConfig.TryGetActiveBalance(
                out GameBalanceConfig.BalanceProfile balance
            ))
        {
            PublishOfflineSummary(
                wasSleeping,
                hungerBefore,
                thirstBefore,
                energyBefore
            );
            return;
        }

        float configuredMaximumHours = balance.MaximumOfflineProgressHours;
        if (float.IsNaN(configuredMaximumHours) ||
            float.IsInfinity(configuredMaximumHours))
        {
            Debug.LogWarning(
                "CatHomeSaveSystem skipped offline progress because Maximum Offline " +
                "Progress Hours is not a finite value."
            );
            PublishOfflineSummary(
                wasSleeping,
                hungerBefore,
                thirstBefore,
                energyBefore
            );
            return;
        }

        double maximumSeconds = Math.Max(0d, configuredMaximumHours * 60d * 60d);
        double appliedSeconds = Math.Min(elapsedSeconds, maximumSeconds);
        if (appliedSeconds <= 0d)
        {
            PublishOfflineSummary(
                wasSleeping,
                hungerBefore,
                thirstBefore,
                energyBefore
            );
            return;
        }

        float seconds = (float)appliedSeconds;
        hungerSystem.ApplyOfflineChange(-balance.HungerDecreasePerSecond * seconds);
        thirstSystem.ApplyOfflineChange(-balance.ThirstDecreasePerSecond * seconds);
        energySystem.ApplyOfflineChange(
            wasSleeping
                ? balance.SleepingEnergyRecoveryPerSecond * seconds
                : -balance.AwakeEnergyDecreasePerSecond * seconds
        );

        AppliedOfflineDuration = TimeSpan.FromSeconds(appliedSeconds);
        OfflineProgressApplied = true;
        PublishOfflineSummary(
            wasSleeping,
            hungerBefore,
            thirstBefore,
            energyBefore
        );
    }

    private static void PublishOfflineSummary(
        bool wasSleeping,
        float hungerBefore,
        float thirstBefore,
        float energyBefore)
    {
        latestSummary = new OfflineReturnSummary(
            ++nextSummaryId,
            AppliedOfflineDuration,
            OfflineProgressApplied,
            wasSleeping,
            hungerBefore,
            hungerSystem.CurrentHunger,
            thirstBefore,
            thirstSystem.CurrentThirst,
            energyBefore,
            energySystem.CurrentEnergy
        );
        hasLatestSummary = true;

        Action<OfflineReturnSummary> handlers = OfflineSummaryReady;
        if (handlers == null)
            return;

        foreach (Action<OfflineReturnSummary> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(latestSummary);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static void ReplaceFileSafely(
        string temporaryPath,
        string savePath,
        string previousPath)
    {
        if (!File.Exists(savePath))
        {
            File.Move(temporaryPath, savePath);
            return;
        }

        try
        {
            File.Replace(temporaryPath, savePath, previousPath);
            if (File.Exists(previousPath))
                File.Delete(previousPath);
        }
        catch (PlatformNotSupportedException)
        {
            ReplaceFileWithRecoverableMoves(temporaryPath, savePath, previousPath);
        }
    }

    private static void ReplaceFileWithRecoverableMoves(
        string temporaryPath,
        string savePath,
        string previousPath)
    {
        if (File.Exists(previousPath))
            File.Delete(previousPath);

        File.Move(savePath, previousPath);
        File.Move(temporaryPath, savePath);
        File.Delete(previousPath);
    }

    private static void TryRefreshRecoveryCopy(string savePath, string recoveryPath)
    {
        string temporaryPath = recoveryPath + ".tmp";
        string previousPath = recoveryPath + ".previous";
        try
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            File.Copy(savePath, temporaryPath);
            ReplaceFileSafely(temporaryPath, recoveryPath, previousPath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"CatHomeSaveSystem saved locally but could not refresh its recovery copy: " +
                exception.Message
            );
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // The primary save is already safe. A stale temp recovery file
                // can be replaced on the next successful save.
            }
        }
    }

    private static bool TryRestoreRecoveryCopy(string recoveryPath, string savePath)
    {
        string temporaryPath = savePath + ".recovery-restore.tmp";
        try
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            File.Copy(recoveryPath, temporaryPath);
            if (File.Exists(savePath))
                File.Delete(savePath);
            File.Move(temporaryPath, savePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"CatHomeSaveSystem could not restore the recovery copy: {exception.Message}"
            );
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // Preserve the original exception as the useful diagnostic.
            }
            return false;
        }
    }

    private static void BackupCorruptSave(string savePath)
    {
        BackupRejectedSave(savePath, "corrupt");
    }

    private static void BackupRejectedSave(string savePath, string reason)
    {
        try
        {
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
            string backupPath = savePath + "." + reason + "-" + timestamp;
            File.Move(savePath, backupPath);
            Debug.LogWarning(
                $"CatHomeSaveSystem preserved the rejected save at '{backupPath}'."
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                $"CatHomeSaveSystem could not preserve the rejected save: {exception.Message}"
            );
        }
    }

    private static bool TryParseUtc(string value, out DateTime utc)
    {
        return DateTime.TryParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out utc
        );
    }

    private static float SanitizeNeed(float value, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? Mathf.Clamp(fallback, 0f, 100f)
            : Mathf.Clamp(value, 0f, 100f);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsValidRotation(Quaternion value)
    {
        if (!IsFinite(value.x) || !IsFinite(value.y) ||
            !IsFinite(value.z) || !IsFinite(value.w))
        {
            return false;
        }

        return value.x * value.x + value.y * value.y +
               value.z * value.z + value.w * value.w > 0.0001f;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void ClearReferences()
    {
        hungerSystem = null;
        thirstSystem = null;
        energySystem = null;
        sleepInteraction = null;
        catMovement = null;
        initialized = false;
    }

#if UNITY_EDITOR
    // Fast Play can retain live scene components after Play stops. Forget the
    // copied session before its directory override is removed, so late save
    // callbacks cannot write that session's values into the player's save.
    public static void EditorEndCopiedSession() => ClearReferences();
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        ClearReferences();
        suspended = false;
        suspendedWhileSleeping = false;
        suspensionSaveUtc = default;
        lastSuccessfulSaveUtc = default;
        lastSaveFrame = -1;
        AppliedOfflineDuration = TimeSpan.Zero;
        OfflineProgressApplied = false;
        nextSummaryId = 0;
        lastPresentedSummaryId = 0;
        latestSummary = default;
        hasLatestSummary = false;
        pendingImportedData = null;
        OfflineSummaryReady = null;
        LocalSaveWritten = null;
    }
}
