using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies the remaining Stage 4 content without rebuilding every room from
/// scratch: Garden Runner scenery, Bond milestone play, and the rebalanced
/// quest chapters.
/// </summary>
public static class StageFourContentBuilder
{
    [MenuItem("Tools/Cat Home/Stage 4/Apply Remaining Content")]
    public static void ApplyFromMenu()
    {
        Debug.Log(ApplySilently());
    }

    public static string ApplySilently()
    {
        ProgressionConfig config = ProgressionConfigBuilder.CreateOrUpdateSilently();
        string feather = StoreProductContentBuilder.EnsureFeatherPlayActivity();
        string window = GameplayActivityContentBuilder.EnsureWindowWatchActivity();
        string birds = GardenLevelBuilder.EnsureBirdWatchActivity();
        string gardenRun = CatRunnerContentBuilder.EnsureGardenCourtyardVariants();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        int chapters = config != null ? config.ChapterCount : 0;
        return "stage-4-applied chapters=" + chapters +
               " " + feather + " " + window + " " + birds + " " + gardenRun;
    }
}
