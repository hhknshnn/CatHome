using UnityEditor;
using UnityEngine;

public static class ProductionPresentationBuilder
{
    public static void Build()
    {
        const string path="Assets/Resources/PremiumMomentArt.asset";
        var art=AssetDatabase.LoadAssetAtPath<PremiumMomentArtSet>(path);
        if(art==null){art=ScriptableObject.CreateInstance<PremiumMomentArtSet>();AssetDatabase.CreateAsset(art,path);}
        art.Coin=AssetDatabase.LoadAssetAtPath<Texture2D>(StorybookTitleBuilder.CatIconPath);
        EditorUtility.SetDirty(art);AssetDatabase.SaveAssets();
        TitleScreenBuilder.BuildSilently();
        foreach(var view in Object.FindObjectsByType<CatDialogueView>(FindObjectsInactive.Include))
        {view.Rebuild(PremiumTypography.Body,true);EditorUtility.SetDirty(view);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(view.gameObject.scene);}
        foreach(var tutorial in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include))
        {tutorial.RebuildCelebration(PremiumTypography.Body,true);EditorUtility.SetDirty(tutorial);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(tutorial.gameObject.scene);}
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}
