using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Fixed kitchen dining furniture; no new purchase or collection entry.</summary>
public static class KitchenDiningSetBuilder
{
    public const string ActivityId="kitchen.dining-set";
    public static readonly Vector3 Position=new Vector3(.15f,0,-.35f);
    public static void Apply(Scene scene)
    {
        foreach(var old in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="KitchenDiningSet").ToArray())
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PremiumFurniture/Models/KitchenDiningSet_Premium.fbx");
        if(asset==null)throw new InvalidOperationException("Dining model missing");
        var root=new GameObject("KitchenDiningSet");SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=Position;
        var model=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);model.name="DiningModel";model.transform.SetParent(root.transform,false);
        PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        model.transform.localRotation=Quaternion.Euler(90,180,0);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/StoreProducts/Materials/"+m.name+".mat")??throw new InvalidOperationException(m.name)).ToArray();
        var loose=model.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("ThrowProp_",StringComparison.Ordinal)).OrderBy(t=>t.name).ToArray();
        if(loose.Length!=3)throw new InvalidOperationException("Three loose table props required");
        var props=new GameObject("TableProps").transform;props.SetParent(root.transform,false);
        foreach(var part in loose)part.SetParent(props,true);
        props.localPosition=new Vector3(.46f,0,-.08f);
        loose[1].localPosition+=new Vector3(-.18f,0,-.16f);
        loose[2].localPosition+=new Vector3(-.18f,0,.115f);
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;filter.gameObject.AddComponent<HomeRoomLayoutObstacle>();}
        var entry=KitchenScatterBuilder.Point(root,"InteractionAnchor",new Vector3(1.28f,0,.12f));
        var perch=KitchenScatterBuilder.Point(root,"ScatterPerchPoint",new Vector3(.29f,.78f,.16f));
        var surface=perch.gameObject.AddComponent<CatActivitySurface>();surface.EditorConfigure(new Vector2(1.72f,1.04f));surface.EditorConfigurePose(CatActivityPose.GentleKnead,false);
        var action=root.AddComponent<SurfaceScatterActivity>();
        action.EditorConfigure(ActivityId,"DINING TABLE",CatActivityKind.DiningScatter,QuestType.KnockOff,0,"TOSS THINGS",.8f,4,entry,null,model);
        action.EditorConfigureEntry(entry);
        // Throw off the open right end, away from both rows of chairs.
        action.EditorConfigureScatter(perch,props,loose,Vector3.zero,new Vector3(1.04f,.78f,-.18f),new Vector3(1.57f,0,-.12f),false);
        ModernWorldArtBuilder.ApplyRoot(root.transform,HomeRoomService.KitchenId);
        KitchenThemeBuilder.ApplyFurniture(root.transform, ActivityId);
    }
}
