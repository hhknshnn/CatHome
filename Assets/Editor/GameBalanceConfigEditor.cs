using UnityEditor;
using UnityEngine;

public static class GameBalanceConfigEditor
{
    private const string ResourcesFolder = "Assets/Resources";
    private const string AssetPath = ResourcesFolder + "/GameBalanceConfig.asset";

    [MenuItem("Tools/Cat Home/Create or Update Game Balance Config")]
    public static void CreateOrUpdate()
    {
        EnsureResourcesFolder();

        GameBalanceConfig config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(AssetPath);
        if (config == null && AssetDatabase.LoadMainAssetAtPath(AssetPath) != null)
        {
            Debug.LogError(
                $"Game Balance Config could not be created because another asset already exists at '{AssetPath}'."
            );
            return;
        }

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            config.EnsureProfiles();
            AssetDatabase.CreateAsset(config, AssetPath);
        }
        else
        {
            config.EnsureProfiles();
            EditorUtility.SetDirty(config);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
        Debug.Log($"Game Balance Config is ready at '{AssetPath}'.", config);
    }

    private static void EnsureResourcesFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            AssetDatabase.CreateFolder("Assets", "Resources");
    }
}
