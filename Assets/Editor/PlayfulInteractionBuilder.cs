using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayfulInteractionBuilder
{
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Finish Play first.");
        int removed=0;
        foreach(string id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/StoreProducts/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(id);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var display=source!=null?source.GetComponent<StoreProductDisplay>():null;
            if(display==null||(display.ProductId!=HomeStoreService.GameConsoleId&&display.ProductId!=HomeStoreService.StereoId&&display.ProductId!=HomeStoreService.TvUnitId))continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try{removed+=Clean(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(string id in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes/Levels"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(id);var scene=SceneManager.GetSceneByPath(path);
            bool already=scene.IsValid()&&scene.isLoaded;
            if(!already)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            foreach(var root in scene.GetRootGameObjects())removed+=Clean(root);
            if(path==HomeRoomService.LivingRoomScenePath)LivingRoomReferenceLayout.Apply(scene);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            if(!already)EditorSceneManager.CloseScene(scene,true);
        }
        AssetDatabase.SaveAssets();return "Retired activities removed: "+removed+"; sofa cushions moved to the opposite arm.";
    }
    static int Clean(GameObject root)
    {
        int count=0;
        foreach(var activity in root.GetComponentsInChildren<CatActivity>(true))
        {
            if(activity==null||!activity.IsRetired)continue;
            if(activity.Kind==CatActivityKind.MouseHunt||activity.Kind==CatActivityKind.WindowWatch)Object.DestroyImmediate(activity.gameObject);
            else
            {
                var data=new SerializedObject(activity);var visual=data.FindProperty("unlockedContent").objectReferenceValue as GameObject;
                if(visual!=null)visual.SetActive(false);
                Object.DestroyImmediate(activity);
            }
            count++;
        }
        foreach(var display in root.GetComponentsInChildren<StoreProductDisplay>(true))
        {
            if(display.ProductId!=HomeStoreService.GameConsoleId&&display.ProductId!=HomeStoreService.StereoId&&display.ProductId!=HomeStoreService.TvUnitId)continue;
            var visual=new SerializedObject(display).FindProperty("visualRoot").objectReferenceValue as GameObject;
            if(visual!=null)visual.SetActive(false); // runtime ownership reveals purchased decor
        }
        return count;
    }
}
