/// <summary>
/// What the quest UI should currently present. Returned by
/// <see cref="ProgressionService.CaptureActiveChapterQuests"/>; it is a pure
/// read-only description of progression, never a request to change it.
/// </summary>
public enum QuestBoardStatus
{
    /// <summary>
    /// No ProgressionConfig could be loaded, or it declares no chapters. The panel
    /// shows an explanatory message and no Claim button.
    /// </summary>
    Unavailable = 0,

    /// <summary>A chapter is active; its quests are in the snapshot buffer.</summary>
    ChapterInProgress = 1,

    /// <summary>
    /// Every configured chapter is finished. The panel shows the completion message
    /// and no Claim button; nothing is claimable in this state.
    /// </summary>
    AllChaptersCompleted = 2
}
