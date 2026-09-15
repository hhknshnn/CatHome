using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OutdoorPolishBuilder
{
    public static string Apply(string roomId)
    {
        if(Application.isPlaying||!OutdoorArrangementProfile.IsReviewedRoom(roomId))throw new InvalidOperationException("Reviewed room in Edit Mode required.");
        CatHomeEditPreview.Clear();
        HomeRoomArrangementBuilder.ReplanRoom(roomId);
        var report=new StringBuilder();
        foreach(var product in StoreCatalogAssets.PlaceableProducts.Where(d=>HomeStoreService.IsProductInRoomCollection(roomId,d.ProductId)))
            RoomProductInteractionBuilder.UpgradePrefab(product,report);
        HomeRoomService.TryGetRoom(roomId,out var room);
        var scene=SceneManager.GetSceneByPath(room.ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(room.ScenePath,OpenSceneMode.Additive);
        try
        {
            StoreProductContentBuilder.BuildRoomSceneProducts(scene,roomId,null);
            var errors=HomeRoomArrangementValidation.Validate(scene,roomId);
            if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        AssetDatabase.SaveAssets();return report.ToString();
    }

    public static void ConfigureScene(Scene scene,string roomId)
    {
        if(roomId!=HomeRoomService.GardenId)return;
        // These old lawn stations duplicated the purchasable yarn ball and
        // left their rails/balls in the central walking space.
        var old=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<GardenYarnChaseActivity>(true))
            .Where(a=>a.GetComponentInParent<HomeProductPlacement>()==null).ToArray();
        foreach(var activity in old)UnityEngine.Object.DestroyImmediate(activity.gameObject);
    }
    public static void ConfigureProduct(GameObject root,StoreCatalogAsset definition)
    {
        if (definition.ProductId == HomeStoreService.LoftChaiseLoungeId)
        {
            var chaiseData = new SerializedObject(root.GetComponent<CanopyNapActivity>());
            var door = chaiseData.FindProperty("doorPoint").objectReferenceValue as Transform;
            var anchor = chaiseData.FindProperty("interactionAnchor").objectReferenceValue as Transform;
            if (door != null) door.localPosition = new Vector3(.74f, 0, .44f);
            if (anchor != null) anchor.localPosition = new Vector3(.62f, 0, .66f);
            return;
        }
        if (definition.ProductId == HomeStoreService.PatioPottedFernsId)
        {
            var look = root.GetComponent<SitLookActivity>();
            look.EditorConfigureLook(look.LookPoint, SitLookReaction.Sit, 2.8f, "SO COZY!");
            var fernData = new SerializedObject(look);
            foreach (string name in new[] { "interactionAnchor", "routineEntryPoint" })
            {
                var point = fernData.FindProperty(name).objectReferenceValue as Transform;
                if (point != null) point.localPosition = new Vector3(.203f, 0, -.626f);
            }
            return;
        }
        bool lounger=definition.ProductId==HomeStoreService.GardenSunLoungerId;
        bool bath=definition.ProductId==HomeStoreService.GardenBirdBathId;
        if(!lounger&&!bath)return;
        var activity=root.GetComponent<CatActivity>();if(activity==null)return;
        var data=new SerializedObject(activity);
        var floor=data.FindProperty("floorPoint").objectReferenceValue as Transform;
        if(floor!=null)floor.localPosition=lounger?new Vector3(-1.08f,0,0):new Vector3(0,0,-.78f);
    }
}
