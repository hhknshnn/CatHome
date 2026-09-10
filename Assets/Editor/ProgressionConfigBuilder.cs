using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates or updates Assets/Resources/ProgressionConfig.asset with the
/// initial test progression, following the GameBalanceConfigEditor pattern.
/// The asset is produced through the AssetDatabase API; its YAML is never
/// written by hand.
/// </summary>
public static class ProgressionConfigBuilder
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string AssetPath = ResourcesFolder + "/ProgressionConfig.asset";
    private const string LivingRoomScenePath =
        "Assets/Scenes/Levels/LivingRoom_Level01.unity";
    private const string DefaultSpawnPointId = "default";

    // Manual claim is the intended player-facing design: reaching the target
    // leaves the quest Completed, and the reward is paid only when the player
    // explicitly claims it through ProgressionService.TryClaimQuest. The level
    // stays put until every one of its quests is Claimed, so the claim is a real
    // step rather than a formality. Setting this to true would restore the older
    // behaviour where RecordProgress claims the quest in the same call.
    private const bool AutoClaim = false;

    // isDaily stays at the QuestDefinition constructor default (false) for all
    // four quests: the daily reset does not exist yet.

    [MenuItem("Tools/Cat Home/Create or Update Progression Config")]
    public static void CreateOrUpdate()
    {
        CreateOrUpdateInternal(true);
    }

    public static ProgressionConfig CreateOrUpdateSilently()
    {
        return CreateOrUpdateInternal(false);
    }

    private static ProgressionConfig CreateOrUpdateInternal(bool confirmOverwrite)
    {
        EnsureResourcesFolder();

        ProgressionConfig config = AssetDatabase.LoadAssetAtPath<ProgressionConfig>(AssetPath);
        if (config == null && AssetDatabase.LoadMainAssetAtPath(AssetPath) != null)
        {
            Debug.LogError(
                $"Progression Config could not be created because another asset already exists at '{AssetPath}'."
            );
            return null;
        }

        if (config != null)
        {
            bool overwrite = !confirmOverwrite || EditorUtility.DisplayDialog(
                "Update Progression Config",
                "ProgressionConfig.asset already exists. Overwrite its levels with the " +
                "default test progression? Manual edits to the asset will be lost.",
                "Overwrite",
                "Cancel"
            );

            if (!overwrite)
            {
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
                return config;
            }

            Undo.RecordObject(config, "Update Progression Config");
            config.EditorSetLevels(BuildDefaultLevels());
            EditorUtility.SetDirty(config);
        }
        else
        {
            config = ScriptableObject.CreateInstance<ProgressionConfig>();
            config.EditorSetLevels(BuildDefaultLevels());
            AssetDatabase.CreateAsset(config, AssetPath);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        Debug.Log($"Progression Config is ready at '{AssetPath}'.", config);
        return config;
    }

    private static LevelDefinition[] BuildDefaultLevels()
    {
        return new[]
        {
            new LevelDefinition(
                "first-meals",
                "First Play",
                1,
                LivingRoomScenePath,
                DefaultSpawnPointId,
                new[]
            {
                new QuestDefinition(
                    "level1_eat",
                    QuestType.Eat,
                    1,
                    "First Bite",
                    "Eat food once",
                    10,
                    5,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level1_drink",
                    QuestType.Drink,
                    1,
                    "First Sip",
                    "Drink water once",
                    10,
                    5,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level1_ball",
                    QuestType.PlayBall,
                    1,
                    "Ball Champion",
                    "Catch the ball three times",
                    15,
                    8,
                    AutoClaim
                )
            }),
            new LevelDefinition(
                "sweet-dreams",
                "Cozy Claws",
                2,
                LivingRoomScenePath,
                DefaultSpawnPointId,
                new[]
            {
                new QuestDefinition(
                    "level2_sleep",
                    QuestType.Sleep,
                    1,
                    "Nap Time",
                    "Sleep in the bed once",
                    15,
                    8,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level2_scratch",
                    QuestType.Scratch,
                    1,
                    "Fresh Claws",
                    "Use the scratching post",
                    20,
                    12,
                    AutoClaim
                )
            }),
            new LevelDefinition(
                "best-friends",
                "Little Hunter",
                3,
                LivingRoomScenePath,
                DefaultSpawnPointId,
                new[]
            {
                new QuestDefinition(
                    "level3_pet",
                    QuestType.Pet,
                    1,
                    "First Cuddle",
                    "Pet the cat once",
                    20,
                    10,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level3_mouse",
                    QuestType.MouseHunt,
                    1,
                    "Mighty Hunter",
                    "Catch at least three mice in one Cat Catch round",
                    25,
                    15,
                    AutoClaim
                )
            }),
            new LevelDefinition(
                "home-loop",
                "Home Loop",
                4,
                LivingRoomScenePath,
                DefaultSpawnPointId,
                new[]
            {
                new QuestDefinition(
                    "level4_runner",
                    QuestType.PlayRunner,
                    1,
                    "First Dash",
                    "Finish one Cat Runner run and earn coins for the house",
                    30,
                    10,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level4_shop",
                    QuestType.BuyStoreItem,
                    1,
                    "Home Makeover",
                    "Buy something from the Home Store to earn Home XP",
                    25,
                    8,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level4_tunnel",
                    QuestType.TunnelPlay,
                    1,
                    "Tunnel Zoom",
                    "Crawl through the play tunnel",
                    30,
                    12,
                    AutoClaim
                )
            }),
            new LevelDefinition(
                "garden-bond",
                "Garden Bond",
                5,
                LivingRoomScenePath,
                DefaultSpawnPointId,
                new[]
            {
                new QuestDefinition(
                    "level5_window",
                    QuestType.WindowWatch,
                    1,
                    "Curious Cat",
                    "Watch a bookshelf, plant or lamp",
                    25,
                    15,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level5_feather",
                    QuestType.FeatherPlay,
                    1,
                    "Feather Frenzy",
                    "Play with the bouncy feather toy after 150 Bond",
                    35,
                    18,
                    AutoClaim
                ),
                new QuestDefinition(
                    "level5_birds",
                    QuestType.BirdWatch,
                    1,
                    "Bird Friends",
                    "Watch the garden birds after 250 Bond",
                    40,
                    20,
                    AutoClaim
                )
            })
        };
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            AssetDatabase.CreateFolder("Assets", "Resources");
    }
}
