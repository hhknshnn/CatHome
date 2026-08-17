/// <summary>
/// The gameplay action a quest counts. Values are serialized in
/// ProgressionConfig assets and in quest ids, so never reorder or reuse them.
/// </summary>
public enum QuestType
{
    Eat = 0,
    Drink = 1,
    Sleep = 2,
    Pet = 3,
    PlayBall = 4,
    Scratch = 5,
    MouseHunt = 6,
    TunnelPlay = 7,
    WindowWatch = 8,
    FeatherPlay = 9,
    BirdWatch = 10,
    PlayRunner = 11,
    BuyStoreItem = 12
}
