using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Regenerates the bright, rounded Cat Home candy UI system.</summary>
public static class PremiumUiSystemRebuild
{
    private const string UiScene = "Assets/Scenes/UI/CatHome_UI.unity";

    [MenuItem("Tools/Cat Home/UI/Rebuild Complete Premium System")]
    public static void Rebuild()
    {
        EditorSceneManager.OpenScene(UiScene, OpenSceneMode.Single);
        PremiumUiFactory.ConfigureCurrencyImporters();
        MainPanelBuilder.BuildSilently();
        CurrencyHudBuilder.BuildSilently();
        QuestPanelBuilder.BuildSilently();
        WhileYouWereAwayPopupBuilder.BuildSilently();
        StoreCatalogPreviewBuilder.BuildMissingAndBedroomPreviews();
        ShopPanelBuilder.BuildSilently();
        RoomSelectorPanelBuilder.BuildSilently();
        CatJournalPanelBuilder.BuildSilently();
        SettingsPanelBuilder.BuildSilently();
        TitleScreenBuilder.BuildSilently();
        PremiumUiRefreshBuilder.Build();
        PremiumWorldVisualBuilder.BuildSilently();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        // Builders author the isolated UI scene, but leaving that scene active
        // produces a black Game view because it intentionally has no camera.
        // Restore the real three-scene authoring stack for interactive use.
        if (!Application.isBatchMode)
            CatHomeAuthoringWorkspace.OpenFullHomePreview(false);

        Debug.Log("Cat Home bright candy UI system regenerated with shared premium surfaces, type and currency art.");
    }
}
