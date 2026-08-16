using System;
using UnityEngine;

/// <summary>
/// One quest inside a progression level. Instances live inside the
/// ProgressionConfig asset; the questId must stay stable because saved
/// progress is keyed by it.
/// </summary>
[Serializable]
public sealed class QuestDefinition
{
    [Tooltip("Stable unique id. Saved quest progress is keyed by this value.")]
    [SerializeField] private string questId;
    [SerializeField] private QuestType type;
    [SerializeField, Min(1)] private int requiredCount = 1;
    [Tooltip("Short player-facing name for the quest UI. The description stays the long form.")]
    [SerializeField] private string title;
    [SerializeField, TextArea(3, 3)] private string description;

    [Header("Rewards")]
    // No [Min] on the long reward fields on purpose: Unity's MinDrawer routes an
    // Integer property through intValue, which would silently truncate a long
    // (see the same note in CurrencyHudController). The getters clamp instead.
    [SerializeField] private long rewardCoins;
    [SerializeField] private long rewardBondXp;
    [Tooltip("Reserved for the future economy. Quests never grant diamonds in this phase.")]
    [SerializeField] private long rewardDiamonds;

    [Header("Behaviour")]
    [Tooltip("Marks the quest as part of the daily set. The daily reset itself is not implemented yet.")]
    [SerializeField] private bool isDaily;
    [Tooltip(
        "Enabled: the moment the quest completes, ProgressionService claims it " +
        "automatically through TryClaimQuest, so the reward lands in the same step.\n" +
        "Disabled (default): the quest stops at Completed and keeps its reward until " +
        "an explicit TryClaimQuest request arrives. Its level cannot advance until " +
        "the quest is Claimed."
    )]
    [SerializeField] private bool autoClaim;

    public QuestDefinition()
    {
    }

    public QuestDefinition(
        string questId,
        QuestType type,
        int requiredCount,
        string title,
        string description,
        long rewardCoins,
        long rewardBondXp,
        bool autoClaim,
        bool isDaily = false)
    {
        this.questId = questId;
        this.type = type;
        this.requiredCount = requiredCount;
        this.title = title;
        this.description = description;
        this.rewardCoins = rewardCoins;
        this.rewardBondXp = rewardBondXp;
        this.autoClaim = autoClaim;
        this.isDaily = isDaily;
        rewardDiamonds = 0;
    }

    public string QuestId => questId;
    public QuestType Type => type;
    public int RequiredCount => requiredCount < 1 ? 1 : requiredCount;
    public string Title => title;
    public string Description => description;
    public long RewardCoins => rewardCoins < 0 ? 0 : rewardCoins;
    public long RewardBondXp => rewardBondXp < 0 ? 0 : rewardBondXp;
    public long RewardDiamonds => rewardDiamonds < 0 ? 0 : rewardDiamonds;
    public bool IsDaily => isDaily;
    public bool AutoClaim => autoClaim;
}
