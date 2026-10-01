using System;
using System.Collections.Generic;
using CatHome.Economy;
using UnityEngine;
using QuestState = CatHome.Quests.QuestState;

[Serializable]
public sealed class DailyQuestSaveEntry
{
    public string questId;
    public int type;
    public int count;
    public int requiredCount;
    public long rewardCoins;
    public long rewardBondXp;
    public int state;
}

[Serializable]
public sealed class DailyRetentionSaveState
{
    public int retentionVersion = DailyRetentionService.SaveVersion;
    public string lastLoginDayUtc = string.Empty;
    public int loginStreak;
    public bool loginGrantedToday;
    public string dailyQuestDayUtc = string.Empty;
    public DailyQuestSaveEntry[] dailyQuests = Array.Empty<DailyQuestSaveEntry>();

    public static DailyRetentionSaveState CreateDefault()
    {
        return new DailyRetentionSaveState
        {
            retentionVersion = DailyRetentionService.SaveVersion,
            lastLoginDayUtc = string.Empty,
            loginStreak = 0,
            loginGrantedToday = false,
            dailyQuestDayUtc = string.Empty,
            dailyQuests = Array.Empty<DailyQuestSaveEntry>()
        };
    }
}

/// <summary>
/// UTC-day login streak and three rotating home dailies. It never spends
/// energy or lives and never rewrites chapter quests.
/// </summary>
public static class DailyRetentionService
{
    public const int SaveVersion = 1;
    public const int DailyQuestCount = 3;
    public const string QuestIdPrefix = "daily:";
    public const string DayFormat = "yyyy-MM-dd";

    private static readonly QuestType[] DailyPool =
    {
        QuestType.Eat,
        QuestType.Drink,
        QuestType.Pet,
        QuestType.PlayBall,
        QuestType.PlayRunner
    };

    private static DailyRetentionSaveState state = DailyRetentionSaveState.CreateDefault();
    private static bool initialized;
    private static string lastLoginGrantSummary = string.Empty;

    public static event Action StateChanged;

    public static int LoginStreak
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return state.loginStreak;
        }
    }

    public static bool LoginGrantedToday
    {
        get
        {
            EnsureInitialized(DateTime.UtcNow);
            return state.loginGrantedToday && IsToday(state.lastLoginDayUtc, DateTime.UtcNow);
        }
    }

    public static string LastLoginGrantSummary => lastLoginGrantSummary ?? string.Empty;

    public static bool IsDailyQuestId(string questId)
    {
        return !string.IsNullOrWhiteSpace(questId) &&
               questId.StartsWith(QuestIdPrefix, StringComparison.Ordinal);
    }

    public static void NotifySessionStart() => NotifySessionStart(DateTime.UtcNow);

    public static bool NotifySessionStart(DateTime utcNow)
    {
        utcNow = ToUtc(utcNow);
        EnsureCurrentDay(utcNow);
        string today = FormatDay(utcNow);
        if (string.Equals(state.lastLoginDayUtc, today, StringComparison.Ordinal) &&
            state.loginGrantedToday)
        {
            return false;
        }

        int previousStreak = state.loginStreak;
        if (IsYesterday(state.lastLoginDayUtc, utcNow))
            state.loginStreak = Mathf.Max(1, state.loginStreak + 1);
        else
            state.loginStreak = 1;

        long coins = LoginCoinsForStreak(state.loginStreak);
        long diamonds = state.loginStreak > 0 && state.loginStreak % 7 == 0 ? 1L : 0L;
        RewardBundle bundle = RewardBundle.FromQuestRewards(coins, 0L, diamonds);
        EconomyTransactionResult grant = EconomyService.GrantReward(
            bundle,
            EconomySource.Event,
            "daily-login:" + today,
            EconomyPersistence.DeferToCaller);
        if (!grant.IsSettled)
        {
            state.loginStreak = previousStreak;
            return false;
        }

        state.lastLoginDayUtc = today;
        state.loginGrantedToday = true;
        lastLoginGrantSummary = diamonds > 0
            ? "Daily login +" + coins + " coins, +1 diamond (streak " + state.loginStreak + ")"
            : "Daily login +" + coins + " coins (streak " + state.loginStreak + ")";
        StateChanged?.Invoke();
        return true;
    }

    public static long LoginCoinsForStreak(int streak)
    {
        int safe = Mathf.Max(1, streak);
        return Math.Min(80L, 15L + (safe - 1) * 10L);
    }

    public static bool RecordProgress(QuestType type)
    {
        EnsureCurrentDay(DateTime.UtcNow);
        bool changed = false;
        DailyQuestSaveEntry[] quests = state.dailyQuests;
        for (int i = 0; i < quests.Length; i++)
        {
            DailyQuestSaveEntry quest = quests[i];
            if (quest == null || quest.type != (int)type || quest.state != (int)QuestState.Active)
                continue;

            if (quest.count < int.MaxValue)
                quest.count++;
            if (quest.count >= quest.requiredCount)
            {
                quest.count = quest.requiredCount;
                quest.state = (int)QuestState.Completed;
            }

            changed = true;
        }

        if (changed)
            StateChanged?.Invoke();
        return changed;
    }

    public static bool TryClaim(string questId)
    {
        if (!IsDailyQuestId(questId))
            return false;

        EnsureCurrentDay(DateTime.UtcNow);
        DailyQuestSaveEntry quest = Find(questId);
        if (quest == null || quest.state != (int)QuestState.Completed)
            return false;

        RewardBundle bundle = RewardBundle.FromQuestRewards(
            quest.rewardCoins,
            quest.rewardBondXp,
            0L);
        EconomyTransactionResult grant = EconomyService.GrantReward(
            bundle,
            EconomySource.DailyQuest,
            "daily-quest:" + quest.questId,
            EconomyPersistence.DeferToCaller);
        if (!grant.IsSettled)
            return false;

        quest.state = (int)QuestState.Claimed;
        StateChanged?.Invoke();
        CatHomeSaveSystem.SaveNow();
        return true;
    }

    public static int CaptureDailyQuests(List<QuestSnapshot> buffer)
    {
        if (buffer == null)
            return 0;

        EnsureCurrentDay(DateTime.UtcNow);
        int written = 0;
        DailyQuestSaveEntry[] quests = state.dailyQuests;
        for (int i = 0; i < quests.Length; i++)
        {
            DailyQuestSaveEntry quest = quests[i];
            if (quest == null || string.IsNullOrWhiteSpace(quest.questId))
                continue;
            buffer.Add(ToSnapshot(quest));
            written++;
        }

        return written;
    }

    public static DailyRetentionSaveState CaptureState()
    {
        EnsureCurrentDay(DateTime.UtcNow);
        return Clone(state);
    }

    public static void ApplySavedState(DailyRetentionSaveState saved)
    {
        state = saved == null ? DailyRetentionSaveState.CreateDefault() : Clone(saved);
        initialized = true;
        lastLoginGrantSummary = string.Empty;
        EnsureCurrentDay(DateTime.UtcNow);
        StateChanged?.Invoke();
    }

    private static void EnsureCurrentDay(DateTime utcNow)
    {
        utcNow = ToUtc(utcNow);
        EnsureInitialized(utcNow);
        string today = FormatDay(utcNow);
        if (string.Equals(state.dailyQuestDayUtc, today, StringComparison.Ordinal) &&
            state.dailyQuests != null &&
            state.dailyQuests.Length == DailyQuestCount)
        {
            return;
        }

        state.dailyQuestDayUtc = today;
        state.dailyQuests = BuildDailyQuests(today);
        if (!string.Equals(state.lastLoginDayUtc, today, StringComparison.Ordinal))
            state.loginGrantedToday = false;
        StateChanged?.Invoke();
    }

    private static DailyQuestSaveEntry[] BuildDailyQuests(string day)
    {
        var selected = new List<QuestType>(DailyQuestCount);
        var rng = new System.Random(StableHash(day));
        while (selected.Count < DailyQuestCount)
        {
            QuestType type = DailyPool[rng.Next(DailyPool.Length)];
            if (selected.Contains(type))
                continue;
            selected.Add(type);
        }

        var quests = new DailyQuestSaveEntry[DailyQuestCount];
        for (int i = 0; i < selected.Count; i++)
        {
            QuestType type = selected[i];
            quests[i] = new DailyQuestSaveEntry
            {
                questId = QuestIdPrefix + day + ":" + type,
                type = (int)type,
                count = 0,
                requiredCount = 1,
                rewardCoins = 15L,
                rewardBondXp = 6L,
                state = (int)QuestState.Active
            };
        }

        return quests;
    }

    private static QuestSnapshot ToSnapshot(DailyQuestSaveEntry quest)
    {
        QuestType type = (QuestType)quest.type;
        return new QuestSnapshot(
            quest.questId,
            GameQuestCopy.Title(type,TitleFor(type)),
            GameQuestCopy.Description(type,quest.requiredCount,DescriptionFor(type,quest.requiredCount)),
            quest.count,
            Mathf.Max(1, quest.requiredCount),
            quest.rewardCoins,
            quest.rewardBondXp,
            0L,
            (QuestState)quest.state);
    }

    private static string TitleFor(QuestType type)
    {
        return type switch
        {
            QuestType.Eat => "Daily Bite",
            QuestType.Drink => "Daily Sip",
            QuestType.Pet => "Daily Cuddle",
            QuestType.PlayBall => "Daily Fetch",
            QuestType.PlayRunner => "Daily Dash",
            _ => "Daily Task"
        };
    }

    private static string DescriptionFor(QuestType type, int count)
    {
        string action = type switch
        {
            QuestType.Eat => "Feed your cat",
            QuestType.Drink => "Give your cat fresh water",
            QuestType.Pet => "Pet your cat",
            QuestType.PlayBall => "Play with the ball",
            QuestType.PlayRunner => "Play Cat Runner",
            _ => "Complete today's goal"
        };
        return action + (count > 1 ? " " + count + " times" : string.Empty) + ".";
    }

    private static DailyQuestSaveEntry Find(string questId)
    {
        DailyQuestSaveEntry[] quests = state.dailyQuests;
        for (int i = 0; i < quests.Length; i++)
        {
            if (quests[i] != null &&
                string.Equals(quests[i].questId, questId, StringComparison.Ordinal))
            {
                return quests[i];
            }
        }

        return null;
    }

    private static void EnsureInitialized(DateTime utcNow)
    {
        if (initialized)
            return;
        state = DailyRetentionSaveState.CreateDefault();
        initialized = true;
        EnsureCurrentDay(utcNow);
    }

    private static DailyRetentionSaveState Clone(DailyRetentionSaveState source)
    {
        DailyQuestSaveEntry[] quests = source.dailyQuests ?? Array.Empty<DailyQuestSaveEntry>();
        var copy = new DailyQuestSaveEntry[quests.Length];
        for (int i = 0; i < quests.Length; i++)
        {
            DailyQuestSaveEntry quest = quests[i];
            if (quest == null)
                continue;
            copy[i] = new DailyQuestSaveEntry
            {
                questId = quest.questId,
                type = quest.type,
                count = quest.count,
                requiredCount = quest.requiredCount,
                rewardCoins = quest.rewardCoins,
                rewardBondXp = quest.rewardBondXp,
                state = quest.state
            };
        }

        return new DailyRetentionSaveState
        {
            retentionVersion = SaveVersion,
            lastLoginDayUtc = source.lastLoginDayUtc ?? string.Empty,
            loginStreak = Math.Max(0, source.loginStreak),
            loginGrantedToday = source.loginGrantedToday,
            dailyQuestDayUtc = source.dailyQuestDayUtc ?? string.Empty,
            dailyQuests = copy
        };
    }

    private static bool IsToday(string day, DateTime utcNow)
    {
        return string.Equals(day, FormatDay(utcNow), StringComparison.Ordinal);
    }

    private static bool IsYesterday(string day, DateTime utcNow)
    {
        return string.Equals(day, FormatDay(utcNow.AddDays(-1d)), StringComparison.Ordinal);
    }

    private static string FormatDay(DateTime utcNow)
    {
        return ToUtc(utcNow).ToString(DayFormat, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static DateTime ToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;
        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static int StableHash(string value)
    {
        unchecked
        {
            int hash = 23;
            if (value == null)
                return hash;
            for (int i = 0; i < value.Length; i++)
                hash = hash * 31 + value[i];
            return hash;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        state = DailyRetentionSaveState.CreateDefault();
        initialized = false;
        lastLoginGrantSummary = string.Empty;
        StateChanged = null;
    }
}
