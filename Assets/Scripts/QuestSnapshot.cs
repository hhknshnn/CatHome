using QuestState = CatHome.Quests.QuestState;

/// <summary>
/// Immutable read-only view of one quest, combining its static definition with
/// its current saved progress. Produced by
/// <see cref="ProgressionService.CaptureActiveChapterQuests"/> so UI can render a
/// level without touching the service's internal progress dictionary or the
/// ProgressionConfig asset.
///
/// A snapshot is a copy taken at one instant: it is never written back, and
/// holding one can neither grant a reward nor change a quest's lifecycle state.
/// </summary>
public readonly struct QuestSnapshot
{
    public QuestSnapshot(
        string questId,
        string title,
        string description,
        int count,
        int requiredCount,
        long rewardCoins,
        long rewardBondXp,
        long rewardDiamonds,
        QuestState state)
    {
        QuestId = questId;
        Title = title;
        Description = description;
        Count = count;
        RequiredCount = requiredCount;
        RewardCoins = rewardCoins;
        RewardBondXp = rewardBondXp;
        RewardDiamonds = rewardDiamonds;
        State = state;
    }

    public string QuestId { get; }

    /// <summary>
    /// Player-facing name. May be empty for a quest authored without one; UI is
    /// expected to fall back to the description rather than to the raw id.
    /// </summary>
    public string Title { get; }

    public string Description { get; }
    public int Count { get; }
    public int RequiredCount { get; }
    public long RewardCoins { get; }
    public long RewardBondXp { get; }
    public long RewardDiamonds { get; }

    /// <summary>Authoritative lifecycle state, never the legacy completed flag.</summary>
    public QuestState State { get; }

    /// <summary>
    /// Target reached and the reward still waiting. This is the only state in
    /// which ProgressionService.TryClaimQuest can succeed, so it is also the only
    /// state in which a Claim button may be shown enabled.
    /// </summary>
    public bool IsClaimable => State == QuestState.Completed;

    public bool HasAnyReward => RewardCoins > 0 || RewardBondXp > 0 || RewardDiamonds > 0;
}
