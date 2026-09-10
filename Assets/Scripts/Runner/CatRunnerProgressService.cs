using System;
using CatHome.Economy;
using UnityEngine;

[Serializable]
public sealed class CatRunnerPendingResultSaveState
{
    public bool hasValue;
    public string runId;
    public float durationSeconds;
    public float distance;
    public int coinsCollected;
    public int collisions;
    public int careBonusPercent;
    public int score;

    public CatRunnerPendingResultSaveState Clone()
    {
        return new CatRunnerPendingResultSaveState
        {
            hasValue = hasValue,
            runId = runId,
            durationSeconds = durationSeconds,
            distance = distance,
            coinsCollected = coinsCollected,
            collisions = collisions,
            careBonusPercent = careBonusPercent,
            score = score
        };
    }
}

[Serializable]
public sealed class CatRunnerProgressSaveState
{
    public int bestScore;
    public bool tutorialCompleted;
    public bool reducedMotion;
    public bool soundEnabled;
    public bool hapticsEnabled;
    public string dailyMissionDayUtc;
    public int dailyCoins;
    public int dailyJumps;
    public int dailyDistance;
    public bool dailyCoinsClaimed;
    public bool dailyJumpsClaimed;
    public bool dailyDistanceClaimed;
    public CatRunnerPendingResultSaveState pendingResult;

    public static CatRunnerProgressSaveState CreateDefault(DateTime utcNow)
    {
        utcNow = RunnerEnergyService.ToSafeUtc(utcNow);
        return new CatRunnerProgressSaveState
        {
            bestScore = Mathf.Max(
                0,
                PlayerPrefs.GetInt(CatRunnerProgressService.LegacyBestScoreKey, 0)),
            tutorialCompleted = false,
            reducedMotion = false,
            soundEnabled = true,
            hapticsEnabled = true,
            dailyMissionDayUtc = utcNow.ToString(
                CatRunnerProgressService.DayFormat,
                System.Globalization.CultureInfo.InvariantCulture),
            dailyCoins = 0,
            dailyJumps = 0,
            dailyDistance = 0,
            dailyCoinsClaimed = false,
            dailyJumpsClaimed = false,
            dailyDistanceClaimed = false,
            pendingResult = new CatRunnerPendingResultSaveState()
        };
    }
}

/// <summary>
/// Persistent Cat Runner state that is not part of the wallet or entry-energy
/// system. The service owns best score, tutorial/accessibility preferences,
/// daily missions and crash-safe pending run settlement.
/// </summary>
public static class CatRunnerProgressService
{
    public const string LegacyBestScoreKey = "CatRunner_BestScore_v1";
    public const string DayFormat = "yyyy-MM-dd";
    public const int DailyCoinTarget = 50;
    public const int DailyJumpTarget = 10;
    public const int DailyDistanceTarget = 500;
    public const int DailyCoinReward = 25;
    public const int DailyJumpReward = 35;
    public const int DailyDistanceReward = 50;

    private static CatRunnerProgressSaveState state;
    private static bool initialized;

    public static event Action StateChanged;
    public static event Action PreferencesChanged;

    public static int BestScore
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return state.bestScore;
        }
    }

    public static bool TutorialCompleted
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return state.tutorialCompleted;
        }
    }

    public static bool ReducedMotion
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return state.reducedMotion;
        }
    }

    public static bool SoundEnabled
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return state.soundEnabled;
        }
    }

    public static bool HapticsEnabled
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return state.hapticsEnabled;
        }
    }

    public static int DailyCoins
    {
        get
        {
            EnsureCurrentDay(DateTime.UtcNow);
            return state.dailyCoins;
        }
    }

    public static int DailyJumps
    {
        get
        {
            EnsureCurrentDay(DateTime.UtcNow);
            return state.dailyJumps;
        }
    }

    public static int DailyDistance
    {
        get
        {
            EnsureCurrentDay(DateTime.UtcNow);
            return state.dailyDistance;
        }
    }

    public static bool HasPendingResult
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return IsValidPending(state.pendingResult);
        }
    }

    public static bool RecordBestScore(int score)
    {
        EnsureInitialized(DateTime.UtcNow);
        int safeScore = Mathf.Max(0, score);
        if (safeScore <= state.bestScore)
            return false;

        state.bestScore = safeScore;
        StateChanged?.Invoke();
        return true;
    }

    public static bool CompleteTutorial()
    {
        EnsureInitialized(DateTime.UtcNow);
        if (state.tutorialCompleted)
            return false;

        state.tutorialCompleted = true;
        StateChanged?.Invoke();
        return true;
    }

    public static void SetReducedMotion(bool value)
    {
        EnsureInitialized(DateTime.UtcNow);
        if (state.reducedMotion == value)
            return;
        state.reducedMotion = value;
        PreferencesChanged?.Invoke();
        StateChanged?.Invoke();
        CatHomeSaveSystem.SaveNow();
    }

    public static void SetSoundEnabled(bool value)
    {
        EnsureInitialized(DateTime.UtcNow);
        if (state.soundEnabled == value)
            return;
        state.soundEnabled = value;
        PreferencesChanged?.Invoke();
        StateChanged?.Invoke();
        CatHomeSaveSystem.SaveNow();
    }

    public static void SetHapticsEnabled(bool value)
    {
        EnsureInitialized(DateTime.UtcNow);
        if (state.hapticsEnabled == value)
            return;
        state.hapticsEnabled = value;
        PreferencesChanged?.Invoke();
        StateChanged?.Invoke();
        CatHomeSaveSystem.SaveNow();
    }

    public static void RecordCoinPickup(int amount = 1)
    {
        DateTime now = DateTime.UtcNow;
        EnsureCurrentDay(now);
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
            return;

        int before = state.dailyCoins;
        state.dailyCoins = Mathf.Min(
            DailyCoinTarget,
            state.dailyCoins > int.MaxValue - safeAmount
                ? int.MaxValue
                : state.dailyCoins + safeAmount);
        if (state.dailyCoins != before)
            StateChanged?.Invoke();
        TryGrantCompletedMissions(now);
    }

    public static void RecordJump()
    {
        DateTime now = DateTime.UtcNow;
        EnsureCurrentDay(now);
        if (state.dailyJumps < DailyJumpTarget)
        {
            state.dailyJumps++;
            StateChanged?.Invoke();
        }
        TryGrantCompletedMissions(now);
    }

    public static void RecordRunDistance(float distance)
    {
        DateTime now = DateTime.UtcNow;
        EnsureCurrentDay(now);
        int safeDistance = Mathf.Max(0, Mathf.RoundToInt(distance));
        if (safeDistance > state.dailyDistance)
        {
            state.dailyDistance = Mathf.Min(DailyDistanceTarget, safeDistance);
            StateChanged?.Invoke();
        }
        TryGrantCompletedMissions(now);
    }

    public static string GetDailyMissionSummary()
    {
        EnsureCurrentDay(DateTime.UtcNow);
        string coins = state.dailyCoinsClaimed
            ? GameContentCopy.Text("Jeton tamam", "Coins done")
            : GameContentCopy.Text($"Jeton {state.dailyCoins}/{DailyCoinTarget}",$"Coins {state.dailyCoins}/{DailyCoinTarget}");
        string jumps = state.dailyJumpsClaimed
            ? GameContentCopy.Text("Zıplama tamam", "Jumps done")
            : GameContentCopy.Text($"Zıplama {state.dailyJumps}/{DailyJumpTarget}",$"Jumps {state.dailyJumps}/{DailyJumpTarget}");
        string distance = state.dailyDistanceClaimed
            ? GameContentCopy.Text("Mesafe tamam", "Distance done")
            : GameContentCopy.Text($"Mesafe {state.dailyDistance}/{DailyDistanceTarget} m",$"Distance {state.dailyDistance}/{DailyDistanceTarget} m");
        return GameContentCopy.Text("Günün hedefleri", "Daily goals") + $"\n{coins}   ·   {jumps}   ·   {distance}";
    }

    public static void SetPendingResult(CatRunnerResult result, int score)
    {
        EnsureInitialized(DateTime.UtcNow);
        state.pendingResult = new CatRunnerPendingResultSaveState
        {
            hasValue = true,
            runId = result.RunId,
            durationSeconds = Mathf.Max(0f, result.DurationSeconds),
            distance = Mathf.Max(0f, result.Distance),
            coinsCollected = Mathf.Max(0, result.CoinsCollected),
            collisions = Mathf.Max(0, result.Collisions),
            careBonusPercent = Mathf.Clamp(result.CareBonusPercent, 0, 100),
            score = Mathf.Max(0, score)
        };
        RecordBestScore(score);
        StateChanged?.Invoke();
    }

    public static bool TrySettlePendingResult(out EconomyTransactionResult settlement)
    {
        EnsureInitialized(DateTime.UtcNow);
        if (!IsValidPending(state.pendingResult))
        {
            settlement = new EconomyTransactionResult(
                EconomyTransactionStatus.NoChange,
                null,
                false,
                "No pending Cat Runner result.");
            return true;
        }

        CatRunnerPendingResultSaveState pending = state.pendingResult.Clone();
        state.pendingResult = new CatRunnerPendingResultSaveState();

        CatRunnerResult result = CatRunnerResult.Create(
            pending.runId,
            pending.durationSeconds,
            pending.distance,
            pending.coinsCollected,
            pending.collisions,
            pending.careBonusPercent);
        settlement = CatRunnerRewardService.Grant(result);
        if (settlement.IsSettled)
        {
            StateChanged?.Invoke();
            CatHomeSaveSystem.SaveNow(forceSameFrame: true);
            return true;
        }

        state.pendingResult = pending;
        StateChanged?.Invoke();
        return false;
    }

    public static CatRunnerProgressSaveState CaptureState(DateTime utcNow)
    {
        EnsureCurrentDay(utcNow);
        return Clone(state);
    }

    public static void ApplySavedState(
        CatRunnerProgressSaveState savedState,
        DateTime utcNow)
    {
        utcNow = RunnerEnergyService.ToSafeUtc(utcNow);
        state = savedState == null
            ? CatRunnerProgressSaveState.CreateDefault(utcNow)
            : Clone(savedState);
        initialized = true;
        Sanitize(utcNow);
        EnsureCurrentDay(utcNow);
        TryGrantCompletedMissions(utcNow);
        StateChanged?.Invoke();
        PreferencesChanged?.Invoke();
    }

    private static bool TryGrantCompletedMissions(DateTime utcNow)
    {
        bool changed = false;
        changed |= TryGrantMission(
            state.dailyCoins >= DailyCoinTarget,
            ref state.dailyCoinsClaimed,
            "coins",
            DailyCoinReward,
            utcNow);
        changed |= TryGrantMission(
            state.dailyJumps >= DailyJumpTarget,
            ref state.dailyJumpsClaimed,
            "jumps",
            DailyJumpReward,
            utcNow);
        changed |= TryGrantMission(
            state.dailyDistance >= DailyDistanceTarget,
            ref state.dailyDistanceClaimed,
            "distance",
            DailyDistanceReward,
            utcNow);
        if (changed)
            StateChanged?.Invoke();
        return changed;
    }

    private static bool TryGrantMission(
        bool completed,
        ref bool claimed,
        string missionId,
        int reward,
        DateTime utcNow)
    {
        if (!completed || claimed)
            return false;

        claimed = true;
        string day = utcNow.ToString(
            DayFormat,
            System.Globalization.CultureInfo.InvariantCulture);
        EconomyTransactionResult result = EconomyService.GrantReward(
            RewardBundle.Coins(reward),
            EconomySource.DailyQuest,
            $"cat-runner-daily:{day}:{missionId}",
            EconomyPersistence.Immediate);
        if (result.IsSettled)
            return true;

        claimed = false;
        return false;
    }

    private static void EnsureInitialized(DateTime utcNow)
    {
        if (initialized)
            return;
        state = CatRunnerProgressSaveState.CreateDefault(utcNow);
        initialized = true;
    }

    private static void EnsureCurrentDay(DateTime utcNow)
    {
        utcNow = RunnerEnergyService.ToSafeUtc(utcNow);
        EnsureInitialized(utcNow);
        string day = utcNow.ToString(
            DayFormat,
            System.Globalization.CultureInfo.InvariantCulture);
        if (string.Equals(state.dailyMissionDayUtc, day, StringComparison.Ordinal))
            return;

        state.dailyMissionDayUtc = day;
        state.dailyCoins = 0;
        state.dailyJumps = 0;
        state.dailyDistance = 0;
        state.dailyCoinsClaimed = false;
        state.dailyJumpsClaimed = false;
        state.dailyDistanceClaimed = false;
        StateChanged?.Invoke();
    }

    private static void Sanitize(DateTime utcNow)
    {
        state.bestScore = Mathf.Max(0, state.bestScore);
        state.dailyCoins = Mathf.Clamp(state.dailyCoins, 0, DailyCoinTarget);
        state.dailyJumps = Mathf.Clamp(state.dailyJumps, 0, DailyJumpTarget);
        state.dailyDistance = Mathf.Clamp(state.dailyDistance, 0, DailyDistanceTarget);
        if (!DateTime.TryParseExact(
                state.dailyMissionDayUtc,
                DayFormat,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out _))
        {
            state.dailyMissionDayUtc = utcNow.ToString(
                DayFormat,
                System.Globalization.CultureInfo.InvariantCulture);
            state.dailyCoins = 0;
            state.dailyJumps = 0;
            state.dailyDistance = 0;
            state.dailyCoinsClaimed = false;
            state.dailyJumpsClaimed = false;
            state.dailyDistanceClaimed = false;
        }

        if (!IsValidPending(state.pendingResult) ||
            !IsFinite(state.pendingResult.durationSeconds) ||
            !IsFinite(state.pendingResult.distance))
        {
            state.pendingResult = new CatRunnerPendingResultSaveState();
        }
        else
        {
            state.pendingResult.durationSeconds =
                Mathf.Max(0f, state.pendingResult.durationSeconds);
            state.pendingResult.distance = Mathf.Max(0f, state.pendingResult.distance);
            state.pendingResult.coinsCollected =
                Mathf.Max(0, state.pendingResult.coinsCollected);
            state.pendingResult.collisions = Mathf.Max(0, state.pendingResult.collisions);
            state.pendingResult.careBonusPercent =
                Mathf.Clamp(state.pendingResult.careBonusPercent, 0, 100);
            state.pendingResult.score = Mathf.Max(0, state.pendingResult.score);
        }
    }

    private static bool IsValidPending(CatRunnerPendingResultSaveState pending)
    {
        return pending != null &&
               pending.hasValue &&
               !string.IsNullOrWhiteSpace(pending.runId);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static CatRunnerProgressSaveState Clone(CatRunnerProgressSaveState source)
    {
        if (source == null)
            return null;
        return new CatRunnerProgressSaveState
        {
            bestScore = source.bestScore,
            tutorialCompleted = source.tutorialCompleted,
            reducedMotion = source.reducedMotion,
            soundEnabled = source.soundEnabled,
            hapticsEnabled = source.hapticsEnabled,
            dailyMissionDayUtc = source.dailyMissionDayUtc,
            dailyCoins = source.dailyCoins,
            dailyJumps = source.dailyJumps,
            dailyDistance = source.dailyDistance,
            dailyCoinsClaimed = source.dailyCoinsClaimed,
            dailyJumpsClaimed = source.dailyJumpsClaimed,
            dailyDistanceClaimed = source.dailyDistanceClaimed,
            pendingResult = source.pendingResult != null
                ? source.pendingResult.Clone()
                : new CatRunnerPendingResultSaveState()
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        state = null;
        initialized = false;
        StateChanged = null;
        PreferencesChanged = null;
    }
}
