using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Art/placement pass for the ten existing living-room purchases.</summary>
public static class LivingProgressionComposition
{
    public static readonly Vector3 ShelfPosition=new Vector3(-.25f,.289937258f,2.53f);
    public static readonly Vector3 ChairPosition=new Vector3(-2.65f,0,2.04f);
    public static readonly Vector3 LampPosition=new Vector3(3.32f,0,2.2f);
    public static readonly Vector3 PlantPosition=new Vector3(-3.33196688f,0,.80f);
    static readonly string[] Names={"TvUnit","ModernTelevision","GameConsoleSet","SpeakerSystem","FloorLamp","ClassicArmchair","TallHouseplant","TallBookshelf","ColorfulBookSet","ModernPainting"};
    public static void ApplyProductPrefab(string path)
    {
        if(!Names.Any(n=>path.EndsWith("/"+n+".prefab",StringComparison.Ordinal)))return;
        var root=PrefabUtility.LoadPrefabContents(path);
        try{ApplyProduct(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void ApplyProductPrefabs(){foreach(var n in Names)ApplyProductPrefab("Assets/Art/StoreProducts/Prefabs/"+n+".prefab");}
    static void ClearModel(Transform visual){foreach(Transform t in visual.Cast<Transform>().ToArray())Object.DestroyImmediate(t.gameObject);foreach(var c in visual.GetComponents<Collider>())Object.DestroyImmediate(c);}
    static void Box(Transform visual,Vector3 center,Vector3 size){var c=visual.gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;}
    static void Point(GameObject root,string name,Vector3 p){var t=root.transform.Find(name);if(t!=null)t.localPosition=p;}
    public static void ApplyProduct(GameObject root)
    {
        var placement=root.GetComponent<HomeProductPlacement>();if(placement==null)return;
        string id=placement.ProductId;var visual=root.transform.Find("VisualContent");if(visual==null)return;
        if(id==HomeStoreService.BookshelfId){
            ClearModel(visual);LivingCompositionBuilder.Model("OpenDisplayShelf",visual);
            foreach(float y in new[]{.925f,1.265f,1.605f})Box(visual,new Vector3(0,y,0),new Vector3(1.61f,.05f,.31f));
            foreach(float x in new[]{-.80f,.80f})Box(visual,new Vector3(x,1.415f,.09f),new Vector3(.095f,1.02f,.23f));
            var so=new SerializedObject(placement);so.FindProperty("footprintSize").vector2Value=new Vector2(1.70f,.40f);so.ApplyModifiedPropertiesWithoutUndo();
            Point(root,"LookPoint",new Vector3(0,1.40f,0));Point(root,"InteractionAnchor",new Vector3(-1.05f,0,1.18f));
        }else if(id==HomeStoreService.TallPlantId){
            ClearModel(visual);LivingCompositionBuilder.Model("LargeNaturalPlant",visual);Box(visual,new Vector3(0,.23f,0),new Vector3(.51f,.46f,.51f));
            var so=new SerializedObject(placement);so.FindProperty("footprintSize").vector2Value=new Vector2(.84f,.84f);so.ApplyModifiedPropertiesWithoutUndo();
            Point(root,"LookPoint",new Vector3(0,.95f,0));Point(root,"InteractionAnchor",LivingRoomGazeLayoutBuilder.TallPlantEntryLocal);
        }else if(id==HomeStoreService.ModernPaintingId){
            ClearModel(visual);var frame=LivingCompositionBuilder.Model("StatementFrame",visual);frame.transform.localPosition=new Vector3(0,1.62f,0);frame.transform.localRotation=Quaternion.Euler(0,180,0);
            LivingCompositionBuilder.Artwork(frame.transform,2,1.25f,.76f);Box(visual,new Vector3(0,1.62f,0),new Vector3(1.35f,.86f,.06f));
            Point(root,"LookPoint",new Vector3(0,1.62f,0));Point(root,"InteractionAnchor",new Vector3(-.20f,0,1.35f));
            var so=new SerializedObject(placement);so.FindProperty("footprintSize").vector2Value=new Vector2(1.35f,.20f);so.ApplyModifiedPropertiesWithoutUndo();
        }else if(id==HomeStoreService.BookSetId){
            Point(root,"InteractionAnchor",new Vector3(-1.05f,0,1.18f));
            var set=root.GetComponent<HomeBookshelfBookSet>();var so=new SerializedObject(set);var books=so.FindProperty("books");var pos=so.FindProperty("targetLocalPositions");
            for(int i=0;i<books.arraySize;i++){
                int row=i<4?0:i<7?1:2;int j=i<4?i:i<7?i-4:i-7;
                var p=new Vector3(-.52f+j*.095f,.962f+row*.34f,.04f);pos.GetArrayElementAtIndex(i).vector3Value=p;
                var t=books.GetArrayElementAtIndex(i).objectReferenceValue as Transform;if(t!=null)t.localPosition=p;
            }so.ApplyModifiedPropertiesWithoutUndo();
            var old=visual.Find("PurchasedShelfPlant");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var p1=LivingCompositionBuilder.Model("SmallCeramicPlant",visual);p1.name="PurchasedShelfPlant";p1.transform.localPosition=new Vector3(.48f,.95f,0);p1.transform.localScale=Vector3.one*.72f;
            old=visual.Find("PurchasedShelfPlantUpper");if(old!=null)Object.DestroyImmediate(old.gameObject);
            p1=LivingCompositionBuilder.Model("SmallCeramicPlant",visual);p1.name="PurchasedShelfPlantUpper";p1.transform.localPosition=new Vector3(.46f,1.63f,0);p1.transform.localScale=Vector3.one*.90f;
            if(AssetDatabase.LoadAssetAtPath<GameObject>(LivingCompositionBuilder.Art+"/Models/ShelfTrailingVine.fbx")!=null)
                LivingCompositionBuilder.Model("ShelfTrailingVine",p1.transform);
        }else if(id==HomeStoreService.FloorLampId||id==HomeStoreService.ArmchairId||id==HomeStoreService.TvUnitId){
            foreach(var r in visual.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>{
                if(m.name.Contains("Gold"))return LivingCompositionBuilder.Material("LC_Gold");
                if(id==HomeStoreService.FloorLampId&&(m.name.Contains("Lemon")||m.name.Contains("White")))return LivingCompositionBuilder.Material("LC_Cream");
                if(id==HomeStoreService.ArmchairId&&m.name.Contains("Coral"))return LivingCompositionBuilder.Material("LC_Coral");
                if(id==HomeStoreService.ArmchairId&&m.name.Contains("Cream"))return LivingCompositionBuilder.Material("LC_Cream");
                return m;}).ToArray();
        }
        // Re-bake visible targets from the new meshes instead of retaining points
        // on the removed cabinet, leaves or painting.
        if(StoreCatalogAssets.TryGet(id,out var definition))SitLookFacingBuilder.Configure(root,definition);
        EditorUtility.SetDirty(root);
    }
    public static string ApplyAndSave()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        CatHomeEditPreview.Clear();ApplyProductPrefabs();var room=LivingCompositionBuilder.Room;
        foreach(var p in room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HomeProductPlacement>(true))){
            if(!HomeStoreService.IsLivingRoomCollectionProduct(p.ProductId))continue;
            // Keep root identities and references; change only visual children and authored placement.
            if(PrefabUtility.IsPartOfPrefabInstance(p.gameObject))PrefabUtility.UnpackPrefabInstance(p.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            ApplyProduct(p.gameObject);
            if(StoreCatalogAssets.TryGet(p.ProductId,out var d))p.transform.SetPositionAndRotation(d.DefaultPosition,Quaternion.Euler(0,d.DefaultYaw,0));
        }
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(room);EditorSceneManager.SaveScene(room);AssetDatabase.SaveAssets();CatHomeEditPreview.Refresh();return "Existing ROOM products redesigned and placed. Purchase IDs, ownership, prices and runtime scripts unchanged.";
    }
}
