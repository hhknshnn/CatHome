using System;
using System.Collections.Generic;
using CatHome.Economy;
using UnityEngine;

using QuestState = CatHome.Quests.QuestState;

/// <summary>
/// Static quest and level service, mirroring the CatHomeSaveSystem pattern: no
/// scene object, initialized from CatMovement.Start, reset on domain reload.
/// Progress is driven purely by events raised by the existing gameplay systems;
/// nothing is polled per frame.
///
/// Currencies are NOT owned here. Coins and diamonds live in
/// <see cref="EconomyService"/>, the single authority for every wallet change;
/// the <see cref="Coins"/> and <see cref="Diamonds"/> properties below are
/// read-through conveniences for existing callers. Bond XP stays here because it
/// is progression, not a spendable currency — the economy reaches it through a
/// registered reward payload handler, so a reward bundle can pay coins, diamonds
/// and bond XP in one atomic transaction.
/// </summary>
public static class ProgressionService
{
    private static ProgressionConfig config;
    private static bool configLoadAttempted;
    private static bool configValidated;

    private static HungerSystem hungerSystem;
    private static ThirstSystem thirstSystem;
    private static SleepInteraction sleepInteraction;
    private static PetInteraction petInteraction;
    private static bool subscribed;

    private static long bondXp;
    private static int currentChapterNumber = 1;
    private static readonly Dictionary<string, QuestProgressEntry> progressById =
        new Dictionary<string, QuestProgressEntry>(StringComparer.Ordinal);

    private static bool progressionCompleteLogged;
    private static bool economyBound;

    /// <summary>Read-through to the economy, which owns the balance.</summary>
    public static long Coins => EconomyService.GetBalance(CurrencyType.Coin);

    /// <summary>Read-through to the economy, which owns the balance.</summary>
    public static long Diamonds => EconomyService.GetBalance(CurrencyType.Diamond);

    public static long BondXp => bondXp;
    public static int CurrentChapterNumber => currentChapterNumber;
    public static bool IsInitialized => subscribed;

    public static bool IsProgressionComplete =>
        config != null && config.ChapterCount > 0 && currentChapterNumber > config.ChapterCount;

    /// <summary>Raised after any wallet, quest or level change. Debug/UI only.</summary>
    public static event Action StateChanged;

    /// <summary>
    /// Resolves the event sources and subscribes to them. Safe to call more
    /// than once: it re-subscribes only when a previously resolved source was
    /// destroyed (for example after a scene reload without a domain reload),
    /// and it never creates duplicate subscriptions.
    /// </summary>
    public static void Initialize(CatMovement movement)
    {
        EnsureEconomyBinding();

        if (subscribed && AreSourcesAlive())
            return;

        Unsubscribe();
        EnsureConfigLoaded();

        if (config == null)
            return;

        ValidateConfigOnce();

        hungerSystem = ResolveSource(movement, UnityEngine.Object.FindAnyObjectByType<HungerSystem>);
        thirstSystem = ResolveSource(movement, UnityEngine.Object.FindAnyObjectByType<ThirstSystem>);
        sleepInteraction = movement != null ? movement.GetComponent<SleepInteraction>() : null;
        petInteraction = movement != null ? movement.GetComponent<PetInteraction>() : null;
        UnityEngine.SceneManagement.Scene sharedUi =
            UnityEngine.SceneManagement.SceneManager.GetSceneByPath(
                LevelLoader.DefaultUiScenePath);
        bool reportMissingSources = sharedUi.IsValid() && sharedUi.isLoaded;

        if (hungerSystem != null)
            hungerSystem.Ate += OnAte;
        else if (reportMissingSources)
            Debug.LogWarning("ProgressionService: HungerSystem was not found; Eat quests will not progress.");

        if (thirstSystem != null)
            thirstSystem.Drank += OnDrank;
        else if (reportMissingSources)
            Debug.LogWarning("ProgressionService: ThirstSystem was not found; Drink quests will not progress.");

        if (sleepInteraction != null)
            sleepInteraction.SleepStarted += OnSleepStarted;
        else if (reportMissingSources)
            Debug.LogWarning("ProgressionService: SleepInteraction was not found; Sleep quests will not progress.");

        if (petInteraction != null)
            petInteraction.PettingStarted += OnPettingStarted;
        else if (reportMissingSources)
            Debug.LogWarning("ProgressionService: PetInteraction was not found; Pet quests will not progress.");

        subscribed = true;
        RaiseStateChanged();
    }

    /// <summary>
    /// Restores state loaded by CatHomeSaveSystem. Never grants rewards, never
    /// claims, never advances levels and never invents a completion timestamp;
    /// it only stores sanitized and normalized values.
    /// </summary>
    public static void ApplySavedState(
        long savedCoins,
        long savedDiamonds,
        long savedBondXp,
        int legacyPlayerLevel,
        QuestProgressEntry[] savedProgress)
    {
        EnsureEconomyBinding();

        // Balances are the economy's to hold. These are the save's legacy
        // coin/diamond fields, which stay in the file as a mirror for older
        // builds; CatHomeSaveSystem applies the authoritative economy section
        // right after this call, so a real economy section always wins.
        EconomyService.ApplyLegacyBalances(savedCoins, savedDiamonds);

        bondXp = savedBondXp < 0 ? 0 : savedBondXp;
        // The legacy playerLevel save field now stores only the current quest
        // chapter for backwards compatibility. It is not a gameplay resource.
        currentChapterNumber = legacyPlayerLevel < 1 ? 1 : legacyPlayerLevel;

        progressById.Clear();
        if (savedProgress != null)
        {
            EnsureConfigLoaded();
            foreach (QuestProgressEntry saved in savedProgress)
            {
                if (saved == null || string.IsNullOrWhiteSpace(saved.questId))
                {
                    Debug.LogWarning(
                        "ProgressionService: a saved quest progress entry without a questId was skipped."
                    );
                    continue;
                }

                if (progressById.ContainsKey(saved.questId))
                {
                    Debug.LogWarning(
                        $"ProgressionService: duplicate saved progress for quest '{saved.questId}' " +
                        "was skipped; the first entry wins."
                    );
                    continue;
                }

                QuestProgressEntry entry = saved.Clone();

                QuestDefinition definition = null;
                if (config != null && !TryFindQuestDefinition(entry.questId, out definition))
                {
                    Debug.LogWarning(
                        $"ProgressionService: saved progress references quest '{entry.questId}' " +
                        "which no longer exists in ProgressionConfig. The entry is kept so the " +
                        "save data is not lost."
                    );
                }

                NormalizeLoadedEntry(entry, definition);
                progressById[entry.questId] = entry;
            }
        }

        RaiseStateChanged();
    }

    /// <summary>
    /// Brings one loaded entry into a self-consistent lifecycle state. It is a
    /// pure repair step: it never grants a reward, never writes a completion
    /// timestamp (a migrated entry legitimately has none) and never advances a
    /// level. A Claimed entry always stays Claimed, so an already paid quest can
    /// never fall back to Completed and be claimed a second time.
    /// </summary>
    private static void NormalizeLoadedEntry(QuestProgressEntry entry, QuestDefinition definition)
    {
        if (entry.count < 0)
            entry.count = 0;

        if (definition != null && entry.count > definition.RequiredCount)
            entry.count = definition.RequiredCount;

        if (!entry.HasValidState)
        {
            // Corrupt or hand-edited enum value: fall back to the legacy flag,
            // which is the only other information the save carries. Completed
            // maps to Claimed because every pre-claim reward was already paid.
            Debug.LogWarning(
                $"ProgressionService: saved quest '{entry.questId}' had an unknown state value " +
                $"({(int)entry.state}). It was normalized from the legacy completed flag."
            );
            entry.SetState(entry.completed ? QuestState.Claimed : QuestState.Active);
        }

        if (entry.state == QuestState.Active && definition != null &&
            entry.count >= definition.RequiredCount)
        {
            // The target is already reached, so the quest is waiting to be
            // claimed rather than to be progressed. SetState (not MarkCompleted)
            // keeps completedTimeUtc untouched.
            entry.SetState(QuestState.Completed);
        }
        else
        {
            // Re-derives the legacy mirror from the authoritative state.
            entry.SetState(entry.state);
        }
    }

    /// <summary>Snapshot of all quest progress for the save system.</summary>
    public static QuestProgressEntry[] CaptureQuestProgress()
    {
        var result = new QuestProgressEntry[progressById.Count];
        int index = 0;
        foreach (QuestProgressEntry entry in progressById.Values)
            result[index++] = entry.Clone();

        return result;
    }

    public static LevelDefinition GetActiveChapterDefinition()
    {
        return config != null ? config.GetChapter(currentChapterNumber) : null;
    }

    public static bool TryGetQuestProgress(string questId, out int count, out bool completed)
    {
        count = 0;
        completed = false;
        if (string.IsNullOrEmpty(questId) ||
            !progressById.TryGetValue(questId, out QuestProgressEntry entry))
        {
            return false;
        }

        count = entry.count;
        completed = entry.completed;
        return true;
    }

    /// <summary>
    /// Read-only lifecycle query. Returns false for a quest that has no progress
    /// entry yet, which is indistinguishable from "not started". Purely
    /// informational: it never creates an entry and never changes state.
    /// </summary>
    public static bool TryGetQuestState(string questId, out QuestState state)
    {
        state = QuestState.Locked;
        if (string.IsNullOrEmpty(questId) ||
            !progressById.TryGetValue(questId, out QuestProgressEntry entry))
        {
            return false;
        }

        state = entry.state;
        return true;
    }

    /// <summary>
    /// Fills <paramref name="buffer"/> with a read-only snapshot of every quest in
    /// the active level and reports what the quest UI should present. This is the
    /// production Quest Panel's single read path.
    ///
    /// It is strictly a query: it never creates a progress entry, never grants a
    /// reward, never claims and never advances a level, so opening a panel can not
    /// change progression. The caller owns the buffer, so no internal collection is
    /// handed out; the snapshots themselves are immutable copies.
    /// </summary>
    /// <param name="buffer">Cleared and refilled. May be null to query only the status.</param>
    /// <param name="levelName">Active level's configured name, or null.</param>
    public static QuestBoardStatus CaptureActiveChapterQuests(
        List<QuestSnapshot> buffer,
        out string levelName)
    {
        buffer?.Clear();
        levelName = null;

        // Loading the ScriptableObject is side-effect free, and TryClaimQuest
        // already does the same, so the panel works even when it is opened before
        // ProgressionService.Initialize has run. Binding the economy here too
        // means a panel opened that early still receives balance changes.
        EnsureEconomyBinding();
        EnsureConfigLoaded();
        if (config == null || config.ChapterCount == 0)
            return QuestBoardStatus.Unavailable;

        // Same rule as IsProgressionComplete, evaluated here so the caller needs
        // exactly one query for the whole board.
        if (currentChapterNumber > config.ChapterCount)
            return QuestBoardStatus.AllChaptersCompleted;

        LevelDefinition level = config.GetChapter(currentChapterNumber);
        if (level == null)
            return QuestBoardStatus.Unavailable;

        levelName = level.LevelName;
        if (buffer == null)
            return QuestBoardStatus.ChapterInProgress;

        IReadOnlyList<QuestDefinition> quests = level.Quests;
        for (int i = 0; i < quests.Count; i++)
        {
            QuestDefinition quest = quests[i];

            // A quest without an id can never progress or be claimed; validation
            // already warned about it, so it is left out of the board instead of
            // being shown as a permanently stuck row.
            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
                continue;

            buffer.Add(CreateSnapshot(quest));
        }

        return QuestBoardStatus.ChapterInProgress;
    }

    private static QuestSnapshot CreateSnapshot(QuestDefinition quest)
    {
        int count = 0;

        // Mirrors GetOrCreateEntry: a quest that has never been recorded is
        // Active, not the QuestState.Locked enum default, because nothing in the
        // project transitions Locked -> Active. Reading must not create the entry,
        // so the same default is applied here instead.
        QuestState state = QuestState.Active;

        if (progressById.TryGetValue(quest.QuestId, out QuestProgressEntry entry))
        {
            count = entry.count < 0 ? 0 : entry.count;
            state = entry.HasValidState ? entry.state : QuestState.Active;
        }

        if (count > quest.RequiredCount)
            count = quest.RequiredCount;

        return new QuestSnapshot(
            quest.QuestId,
            quest.Title,
            quest.Description,
            count,
            quest.RequiredCount,
            quest.RewardCoins,
            quest.RewardBondXp,
            quest.RewardDiamonds,
            state
        );
    }

    // ----- Economy bridge -----

    /// <summary>
    /// Connects this service to <see cref="EconomyService"/> exactly once per
    /// domain: it registers the bond XP reward handler (so a reward bundle can
    /// pay bond XP atomically alongside coins and diamonds) and forwards balance
    /// changes to <see cref="StateChanged"/>, which is what the Quest Panel's
    /// wallet line and the debug overlay already listen to.
    ///
    /// Called from every public entry point instead of from a
    /// RuntimeInitializeOnLoadMethod, because EditMode tests never run those
    /// hooks and would otherwise see bond XP rewards rejected. Public so tests
    /// and bootstrap systems can establish the binding explicitly; the call is
    /// idempotent and cheap.
    /// </summary>
    public static void EnsureEconomyBinding()
    {
        if (economyBound)
            return;

        economyBound = true;
        EconomyService.RegisterPayloadHandler(RewardPayloadKind.BondXp, ApplyBondXpReward);
        EconomyService.AnyBalanceChanged -= RaiseStateChanged;
        EconomyService.AnyBalanceChanged += RaiseStateChanged;
    }

    /// <summary>
    /// The economy's callback for a bond XP reward line. Reached only from inside
    /// a committed economy transaction, never called directly.
    /// </summary>
    private static void ApplyBondXpReward(RewardItem reward)
    {
        AddBondXp(reward.Amount);
    }

    /// <summary>
    /// Bond XP is progression, not a spendable currency, so it stays here. Adding
    /// it through a reward bundle is preferred, because that makes it part of the
    /// same atomic transaction as the coins and diamonds of the same reward.
    /// </summary>
    public static void AddBondXp(long amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning(
                $"ProgressionService ignored a negative bond XP change ({amount})."
            );
            return;
        }

        if (amount == 0)
            return;

        bondXp = amount > long.MaxValue - bondXp ? long.MaxValue : bondXp + amount;
        RaiseStateChanged();
    }

    // ----- Quest progress -----

    private static void OnAte() => RecordProgress(QuestType.Eat);
    private static void OnDrank() => RecordProgress(QuestType.Drink);
    private static void OnSleepStarted() => RecordProgress(QuestType.Sleep);
    private static void OnPettingStarted() => RecordProgress(QuestType.Pet);

    /// <summary>
    /// Advances every matching Active quest of the active level by one. Locked,
    /// Completed and Claimed quests are skipped, so a quest can never progress
    /// or reward twice. Multiple quests of the same type in one level are all
    /// advanced by the same event.
    ///
    /// Reaching the target only marks the quest Completed; the reward is paid
    /// exclusively by <see cref="TryClaimQuest"/>, which this method calls
    /// immediately when the definition has AutoClaim enabled.
    /// </summary>
    public static void RecordProgress(QuestType type)
    {
        if (config == null)
            return;

        LevelDefinition level = config.GetChapter(currentChapterNumber);
        if (level == null)
        {
            LogProgressionCompleteOnce();
            return;
        }

        DateTime utcNow = DateTime.UtcNow;
        bool questCompletedNow = false;

        // Captured before the loop: an auto-claim can advance the chapter, and the
        // iteration must stay on the level the event was recorded against.
        IReadOnlyList<QuestDefinition> quests = level.Quests;
        for (int i = 0; i < quests.Count; i++)
        {
            QuestDefinition quest = quests[i];
            if (quest == null || quest.Type != type ||
                string.IsNullOrWhiteSpace(quest.QuestId))
            {
                continue;
            }

            QuestProgressEntry entry = GetOrCreateEntry(quest.QuestId);
            if (entry.state != QuestState.Active)
                continue;

            if (entry.count < int.MaxValue)
                entry.count++;

            if (entry.count < quest.RequiredCount)
                continue;

            entry.count = quest.RequiredCount;
            entry.MarkCompleted(utcNow);
            questCompletedNow = true;
            Debug.Log($"ProgressionService: quest '{quest.QuestId}' completed.");

            if (quest.AutoClaim)
                TryClaimQuest(quest.QuestId);
        }

        RaiseStateChanged();
        if (!questCompletedNow)
            return;

        // Persist only on meaningful changes (quest completion / claim /
        // level-up), not on every count increment. A claim already saved in this
        // frame; SaveNow is frame-guarded, so this is then a no-op.
        CatHomeSaveSystem.SaveNow();
    }

    /// <summary>
    /// The only path that pays a quest reward. Succeeds exactly once per quest:
    /// it requires <see cref="QuestState.Completed"/> and moves the entry to
    /// Claimed, so a repeated call on the same quest returns false without
    /// touching the wallet.
    /// </summary>
    /// <returns>True only when a reward was granted and the quest became Claimed.</returns>
    public static bool TryClaimQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        EnsureEconomyBinding();
        EnsureConfigLoaded();
        if (config == null)
            return false;

        if (!TryFindQuestDefinition(questId, out QuestDefinition quest))
        {
            Debug.LogWarning(
                $"ProgressionService: quest '{questId}' cannot be claimed because it does not " +
                "exist in ProgressionConfig."
            );
            return false;
        }

        if (!progressById.TryGetValue(questId, out QuestProgressEntry entry) ||
            !entry.IsClaimable)
        {
            return false;
        }

        // State flips only after the economy settled, so a rejected transaction
        // can never leave a quest Claimed without its reward.
        if (!GrantQuestRewards(quest))
            return false;

        entry.MarkClaimed();

        // A level waits for its quests to be Claimed, not merely Completed, so
        // advancement is evaluated here rather than on completion.
        TryAdvanceChapter();
        RaiseStateChanged();
        CatHomeSaveSystem.SaveNow();
        return true;
    }

    /// <summary>
    /// Reachable only from <see cref="TryClaimQuest"/>. The quest never touches a
    /// balance itself: it hands one reward bundle to the economy, which settles
    /// coins, diamonds and bond XP as a single atomic transaction. Zero rewards
    /// are dropped by the bundle, so a reward-less quest raises no change.
    ///
    /// Persistence is deferred to the caller on purpose: TryClaimQuest saves once
    /// after the quest is marked Claimed, so the payout and the claim land in the
    /// same file write instead of two, and an interruption between them can never
    /// persist the money without the claim.
    /// </summary>
    /// <returns>False when the economy rejected the payout; the claim is aborted.</returns>
    private static bool GrantQuestRewards(QuestDefinition quest)
    {
        RewardBundle reward = RewardBundle.FromQuestRewards(
            quest.RewardCoins,
            quest.RewardBondXp,
            quest.RewardDiamonds
        );

        EconomyTransactionResult result = EconomyService.GrantReward(
            reward,
            EconomySource.Quest,
            BuildQuestTransactionId(quest.QuestId),
            EconomyPersistence.DeferToCaller
        );

        if (!result.IsSettled)
        {
            Debug.LogWarning(
                $"ProgressionService: quest '{quest.QuestId}' was not claimed because the economy " +
                $"rejected its reward ({result}). The quest stays claimable."
            );
            return false;
        }

        Debug.Log(
            $"ProgressionService: quest '{quest.QuestId}' claimed. Rewards: {reward.Describe()}."
        );
        return true;
    }

    /// <summary>
    /// Idempotency key for a quest payout. The quest's own Claimed state is the
    /// primary guard against a double payout; this is the economy-side second
    /// one, which also covers a retry that happens before the state flip.
    /// </summary>
    private static string BuildQuestTransactionId(string questId) => "quest:" + questId;

    private static void TryAdvanceChapter()
    {
        // A loop so that a (misconfigured) level without quests can never
        // soft-lock progression; validation warns about such levels.
        while (true)
        {
            LevelDefinition level = config.GetChapter(currentChapterNumber);
            if (level == null)
            {
                LogProgressionCompleteOnce();
                return;
            }

            if (!AreAllQuestsClaimed(level))
                return;

            currentChapterNumber++;
            Debug.Log(
                $"ProgressionService: quest chapter completed. Active chapter is now {currentChapterNumber}."
            );
        }
    }

    /// <summary>
    /// A level is finished only when every one of its quests is Claimed. A quest
    /// that is Completed but still waiting for its reward holds the level back,
    /// which is what future manual-claim quests need.
    /// </summary>
    private static bool AreAllQuestsClaimed(LevelDefinition level)
    {
        IReadOnlyList<QuestDefinition> quests = level.Quests;
        for (int i = 0; i < quests.Count; i++)
        {
            QuestDefinition quest = quests[i];

            // Quests without an id can never progress; validation already
            // warned, so they are treated as claimed to avoid a soft-lock.
            if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
                continue;

            if (!progressById.TryGetValue(quest.QuestId, out QuestProgressEntry entry) ||
                entry.state != QuestState.Claimed)
            {
                return false;
            }
        }

        return true;
    }

    private static QuestProgressEntry GetOrCreateEntry(string questId)
    {
        if (!progressById.TryGetValue(questId, out QuestProgressEntry entry))
        {
            // A first-seen quest starts Active, never at the QuestState.Locked
            // enum default: nothing in the project transitions Locked -> Active,
            // so the default would strand the quest permanently.
            entry = new QuestProgressEntry { questId = questId };
            entry.MarkActive();
            progressById[questId] = entry;
        }

        return entry;
    }

    private static void LogProgressionCompleteOnce()
    {
        if (progressionCompleteLogged)
            return;

        progressionCompleteLogged = true;
        Debug.Log(
            "ProgressionService: all configured quest chapters are completed. " +
            "Progression stays safely in the completed state."
        );
    }

    // ----- Config -----

    private static void EnsureConfigLoaded()
    {
        if (config != null || configLoadAttempted)
            return;

        configLoadAttempted = true;
        ProgressionConfig.TryGetActive(out config);
    }

    private static void ValidateConfigOnce()
    {
        if (configValidated || config == null)
            return;

        configValidated = true;

        if (config.ChapterCount == 0)
        {
            Debug.LogWarning("ProgressionService: ProgressionConfig contains no quest chapters.");
            return;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int chapterNumber = 1; chapterNumber <= config.ChapterCount; chapterNumber++)
        {
            LevelDefinition level = config.GetChapter(chapterNumber);
            IReadOnlyList<QuestDefinition> quests = level != null
                ? level.Quests
                : Array.Empty<QuestDefinition>();

            if (quests.Count == 0)
            {
                Debug.LogWarning(
                    $"ProgressionService: chapter {chapterNumber} has no quests and will be skipped automatically."
                );
                continue;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId))
                {
                    Debug.LogWarning(
                        $"ProgressionService: chapter {chapterNumber} contains a quest without a questId; it is ignored."
                    );
                    continue;
                }

                if (!seenIds.Add(quest.QuestId))
                {
                    Debug.LogWarning(
                        $"ProgressionService: duplicate questId '{quest.QuestId}' in ProgressionConfig. " +
                        "Duplicate ids share one progress entry, which is almost certainly unintended."
                    );
                }
            }
        }
    }

    /// <summary>
    /// Finds a quest definition anywhere in the config, not just in the active
    /// level: a claim may arrive for a quest of an already advanced level.
    /// </summary>
    private static bool TryFindQuestDefinition(string questId, out QuestDefinition definition)
    {
        for (int chapterNumber = 1; chapterNumber <= config.ChapterCount; chapterNumber++)
        {
            LevelDefinition level = config.GetChapter(chapterNumber);
            if (level == null)
                continue;

            IReadOnlyList<QuestDefinition> quests = level.Quests;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                if (quest != null && string.Equals(quest.QuestId, questId, StringComparison.Ordinal))
                {
                    definition = quest;
                    return true;
                }
            }
        }

        definition = null;
        return false;
    }

    // ----- Subscription helpers -----

    private static T ResolveSource<T>(CatMovement movement, Func<T> findFallback)
        where T : Component
    {
        T source = movement != null ? movement.GetComponent<T>() : null;
        return source != null ? source : findFallback();
    }

    private static bool AreSourcesAlive()
    {
        // Unity's overloaded null-check reports destroyed objects as null, so
        // a scene reload without a domain reload triggers a clean re-subscribe.
        return (ReferenceEquals(hungerSystem, null) || hungerSystem != null) &&
               (ReferenceEquals(thirstSystem, null) || thirstSystem != null) &&
               (ReferenceEquals(sleepInteraction, null) || sleepInteraction != null) &&
               (ReferenceEquals(petInteraction, null) || petInteraction != null);
    }

    private static void Unsubscribe()
    {
        // ReferenceEquals keeps working after Destroy, so handlers are always
        // removed from the managed event even when the object is already gone.
        if (!ReferenceEquals(hungerSystem, null))
            hungerSystem.Ate -= OnAte;

        if (!ReferenceEquals(thirstSystem, null))
            thirstSystem.Drank -= OnDrank;

        if (!ReferenceEquals(sleepInteraction, null))
            sleepInteraction.SleepStarted -= OnSleepStarted;

        if (!ReferenceEquals(petInteraction, null))
            petInteraction.PettingStarted -= OnPettingStarted;

        hungerSystem = null;
        thirstSystem = null;
        sleepInteraction = null;
        petInteraction = null;
        subscribed = false;
    }

    private static void RaiseStateChanged()
    {
        Action handlers = StateChanged;
        if (handlers == null)
            return;

        foreach (Action handler in handlers.GetInvocationList())
        {
            try
            {
                handler();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        Unsubscribe();
        config = null;
        configLoadAttempted = false;
        configValidated = false;
        bondXp = 0;
        currentChapterNumber = 1;
        progressById.Clear();
        progressionCompleteLogged = false;
        StateChanged = null;

        // Balances are reset by EconomyService's own hook. Only the binding flag
        // is cleared here, so the handler and the event bridge are re-registered
        // on the first entry point of the new domain — the two reset hooks can
        // run in either order without leaving a stale subscription behind.
        economyBound = false;
    }
}
