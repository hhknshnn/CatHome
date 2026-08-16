#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CatHomeNeedUiCleanup
{
    private const string MenuPath = "Tools/Cat Home/Remove Legacy Need Speech Bubbles";
    private static readonly string[] LegacyRootNames =
    {
        "HungerBubble",
        "ThirstBubble",
        "EnergyBubble"
    };

    [MenuItem(MenuPath)]
    public static void RemoveLegacyNeedSpeechBubbles()
    {
        int removed = 0;
        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include
        );

        foreach (Transform candidate in transforms)
        {
            if (candidate == null || !IsLegacyRootName(candidate.name) ||
                EditorUtility.IsPersistent(candidate.gameObject))
            {
                continue;
            }

            Scene scene = candidate.gameObject.scene;
            Undo.DestroyObjectImmediate(candidate.gameObject);
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
            removed++;
        }

        ApplyFourHourFallbackValues();
        Debug.Log(
            $"Cat Home need UI cleanup removed {removed} legacy speech-bubble root(s). " +
            "Hunger, Thirst, and Energy bars and systems were left intact."
        );
    }

    public static void UpdateGameSceneFromCommandLine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        RemoveLegacyNeedSpeechBubbles();
        PetInteractionBuilder.RebuildPetTutorialCard();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    private static void ApplyFourHourFallbackValues()
    {
        SetFallbackValue(Object.FindAnyObjectByType<HungerSystem>(), "decreasePerSecond");
        SetFallbackValue(Object.FindAnyObjectByType<ThirstSystem>(), "decreasePerSecond");
        SetFallbackValue(Object.FindAnyObjectByType<EnergySystem>(), "decreasePerSecond");
    }

    private static void SetFallbackValue(Object target, string propertyName)
    {
        if (target == null)
            return;

        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.floatValue = GameBalanceConfig.FourHourNeedDepletionPerSecond;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private static bool IsLegacyRootName(string objectName)
    {
        foreach (string legacyName in LegacyRootNames)
        {
            if (objectName == legacyName)
                return true;
        }

        return false;
    }
}
#endif
