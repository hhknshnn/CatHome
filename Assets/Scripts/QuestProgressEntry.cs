using System;
using System.Globalization;

using QuestState = CatHome.Quests.QuestState;

/// <summary>
/// Saved progress of a single quest. Serialized by JsonUtility inside
/// CatHomeSaveData, so the public fields follow the save-model style.
/// </summary>
[Serializable]
public sealed class QuestProgressEntry
{
    private const string TimestampFormat = "O";

    public string questId;
    public int count;

    /// <summary>
    /// Legacy completion flag, kept only so a rollback to a pre-claim build still
    /// reads correct data. <see cref="state"/> is the authority; this field is a
    /// mirror of it and must never be written directly. Use the Mark* / SetState
    /// helpers, which keep both in sync: false for Locked and Active, true for
    /// Completed and Claimed.
    /// </summary>
    public bool completed;

    /// <summary>
    /// Authoritative lifecycle state (save version 4). Populated for older saves by
    /// CatHomeSaveSystem.MigrateSaveData; a legacy completed quest migrates to
    /// Claimed because the pre-claim service already paid its reward on completion.
    /// </summary>
    public QuestState state;

    /// <summary>
    /// Round-trip ("O") UTC completion timestamp, empty when unknown. Stored as a
    /// string because JsonUtility cannot serialize DateTime. Migrated entries stay
    /// empty on purpose: the legacy save never recorded a completion time, and a
    /// substituted value would be a fabricated one.
    /// </summary>
    public string completedTimeUtc;

    /// <summary>Target reached; the reward is waiting to be claimed.</summary>
    public bool IsClaimable => state == QuestState.Completed;

    /// <summary>Target reached, regardless of whether the reward was paid yet.</summary>
    public bool IsCompletedOrClaimed =>
        state == QuestState.Completed || state == QuestState.Claimed;

    /// <summary>True for the four declared enum values, false for corrupt save data.</summary>
    public bool HasValidState =>
        state == QuestState.Locked || state == QuestState.Active ||
        state == QuestState.Completed || state == QuestState.Claimed;

    /// <summary>
    /// The single writer of <see cref="state"/>. Re-derives the legacy
    /// <see cref="completed"/> mirror so the two can never drift apart. It never
    /// touches the timestamp and never grants anything.
    /// </summary>
    public void SetState(QuestState newState)
    {
        state = newState;
        completed = newState == QuestState.Completed || newState == QuestState.Claimed;
    }

    public void MarkActive()
    {
        SetState(QuestState.Active);
    }

    /// <summary>
    /// Moves the quest to Completed and stamps the completion time. An already
    /// recorded, parseable timestamp is preserved, so re-marking an entry that is
    /// Completed or Claimed can never rewrite history. Rewards are not part of
    /// this step: only ProgressionService.TryClaimQuest pays them.
    /// </summary>
    public void MarkCompleted(DateTime utcNow)
    {
        bool keepExistingTimestamp = IsCompletedOrClaimed && TryGetCompletedTimeUtc(out _);
        SetState(QuestState.Completed);

        if (!keepExistingTimestamp)
            SetCompletedTimeUtc(utcNow);
    }

    /// <summary>
    /// Moves the quest to Claimed, keeping the completion timestamp untouched.
    /// Called only after the reward has been granted.
    /// </summary>
    public void MarkClaimed()
    {
        SetState(QuestState.Claimed);
    }

    /// <summary>
    /// Returns false when the timestamp is empty or unparseable, which both mean
    /// "unknown". Callers must not substitute a fallback date.
    /// </summary>
    public bool TryGetCompletedTimeUtc(out DateTime value)
    {
        if (string.IsNullOrEmpty(completedTimeUtc))
        {
            value = default;
            return false;
        }

        return DateTime.TryParseExact(
            completedTimeUtc,
            TimestampFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out value
        );
    }

    public void SetCompletedTimeUtc(DateTime value)
    {
        completedTimeUtc = value
            .ToUniversalTime()
            .ToString(TimestampFormat, CultureInfo.InvariantCulture);
    }

    public void ClearCompletedTimeUtc()
    {
        completedTimeUtc = string.Empty;
    }

    public QuestProgressEntry Clone()
    {
        return new QuestProgressEntry
        {
            questId = questId,
            count = count,
            completed = completed,
            state = state,
            completedTimeUtc = completedTimeUtc
        };
    }
}
