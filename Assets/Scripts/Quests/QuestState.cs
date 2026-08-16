namespace CatHome.Quests
{
    // Lifecycle of a single quest. The values are persisted inside
    // CatHomeSaveData, so never reorder or reuse them.
    public enum QuestState
    {
        Locked = 0,
        Active = 1,
        Completed = 2,
        Claimed = 3
    }
}
