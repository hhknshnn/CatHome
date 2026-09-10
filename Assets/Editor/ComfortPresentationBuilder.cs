using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Applies only the approved navigation rows and the grill's seated reaction.</summary>
public static class ComfortPresentationBuilder
{
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
        CatHomeEditPreview.Clear();
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Preserve unsaved scene work before authoring navigation.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach(string path in new[]{"Assets/Scenes/UI/CatHome_UI.unity","Assets/Scenes/Runner/CatRunner.unity","Assets/Scenes/Catch/CatCatch.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                foreach(var root in scene.GetRootGameObjects())MiniGameNavigationBuilder.Apply(root.transform);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            const string grillPath="Assets/Art/StoreProducts/Prefabs/GardenGrill.prefab";
            var grill=PrefabUtility.LoadPrefabContents(grillPath);
            try
            {
                var activity=grill.GetComponent<SitLookActivity>();
                if(activity==null||activity.StoreProductId!=HomeStoreService.GardenGrillId)throw new InvalidOperationException("Unexpected grill asset.");
                var serialized=new SerializedObject(activity);
                serialized.FindProperty("reactionKind").enumValueIndex=(int)SitLookReaction.Sit;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(grill,grillPath);
            }
            finally {PrefabUtility.UnloadPrefabContents(grill);}
            // Older authored room copies can carry an override of this one enum.
            var garden=EditorSceneManager.OpenScene("Assets/Scenes/Levels/Garden_Level01.unity",OpenSceneMode.Single);
            bool changed=false;
            foreach(var activity in garden.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SitLookActivity>(true)))
            {
                if(activity.StoreProductId!=HomeStoreService.GardenGrillId||activity.ReactionKind==SitLookReaction.Sit)continue;
                var serialized=new SerializedObject(activity);serialized.FindProperty("reactionKind").enumValueIndex=(int)SitLookReaction.Sit;
                serialized.ApplyModifiedPropertiesWithoutUndo();changed=true;
            }
            if(changed){EditorSceneManager.MarkSceneDirty(garden);EditorSceneManager.SaveScene(garden);}
            AssetDatabase.SaveAssets();
        }
        finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
}
