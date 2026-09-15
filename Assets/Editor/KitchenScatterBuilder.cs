using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class KitchenScatterBuilder
{
    public const float CounterTop=.74304f;
    public static readonly Vector3 BasketPosition=new Vector3(-1.72f,CounterTop,2.05f);
    public static readonly Vector3 BasketOnCounter=new Vector3(0,CounterTop,-.2164f);
    public static bool SupportedPair(string a,string b)=>(a==HomeStoreService.KitchenFruitBasketId&&b==HomeStoreService.KitchenIslandId)||(b==HomeStoreService.KitchenFruitBasketId&&a==HomeStoreService.KitchenIslandId);
    public static void Configure(GameObject root,StoreCatalogAsset definition)
    {
        if(definition.ProductId!=HomeStoreService.KitchenFruitBasketId)return;
        var visual=root.transform.Find("VisualContent");if(visual==null)throw new InvalidOperationException("Fruit visual missing");
        var model=visual.Find("BasketScatterModel");
        if(model==null)
        {
            var old=visual.GetChild(0);Vector3 scale=old.localScale;
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PremiumFurniture/Models/KitchenFruitBasket_Premium.fbx").GetComponentInChildren<Renderer>();
            var actual=old.GetComponentInChildren<Renderer>();var materials=new Dictionary<string,Material>();
            for(int i=0;i<original.sharedMaterials.Length;i++)materials[original.sharedMaterials[i].name]=actual.sharedMaterials[i];
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PremiumFurniture/Models/KitchenFruitBasket_Scatter.fbx");
            if(asset==null)throw new InvalidOperationException("Build kitchen scatter meshes first");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset);instance.name="BasketScatterModel";model=instance.transform;model.SetParent(visual,false);model.localScale=scale;
            foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>materials.TryGetValue(m.name,out var replacement)?replacement:m).ToArray();
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        // A multi-part FBX keeps the import rotation on every child. The old
        // single-mesh prefab cancelled it on its model root instead.
        model.localRotation=Quaternion.Euler(90,0,0);
        foreach(var old in root.GetComponents<CatActivity>())if(!(old is SurfaceScatterActivity))UnityEngine.Object.DestroyImmediate(old);
        var scatter=root.GetComponent<SurfaceScatterActivity>()??root.AddComponent<SurfaceScatterActivity>();
        var anchor=Point(root,"InteractionAnchor",new Vector3(.51f,-CounterTop,-1.05f));
        var perch=Point(root,"ScatterPerchPoint",new Vector3(.302f,0,.328f));
        var surface=perch.GetComponent<CatActivitySurface>()??perch.gameObject.AddComponent<CatActivitySurface>();surface.EditorConfigure(new Vector2(.65f,.43f));surface.EditorConfigurePose(CatActivityPose.GentleKnead,false);
        var fruit=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Fruit_",StringComparison.Ordinal)).OrderBy(t=>t.name).ToArray();
        if(fruit.Length!=10)throw new InvalidOperationException("Expected ten original fruit meshes");
        scatter.EditorConfigure("fruit-swat","FRUIT BASKET",CatActivityKind.FruitSwat,QuestType.KitchenWatch,0,"TIP BASKET",1.1f,4,anchor,null,visual.gameObject);
        scatter.EditorConfigureStoreProduct(definition.ProductId);
        scatter.EditorConfigureScatter(perch,model,fruit,new Vector3(.08f,.132f,.172f),new Vector3(0,0,-.46f),new Vector3(0,-CounterTop,-.95f),true,HomeStoreService.KitchenIslandId);
        var attachment=root.GetComponent<HomeRequiredProductAttachment>()??root.AddComponent<HomeRequiredProductAttachment>();
        attachment.EditorConfigure(definition.ProductId,HomeStoreService.KitchenIslandId,visual.gameObject,BasketOnCounter,Vector3.zero);
        root.GetComponent<HomeProductPlacement>().EditorConfigure(definition.ProductId,root.transform,Array.Empty<Transform>(),definition.Footprint,HomeProductPlacementKind.ProductSurfaceOnly,HomeStoreService.KitchenIslandId,CounterTop);
    }
    public static Transform Point(GameObject root,string name,Vector3 position)
    {var p=root.transform.Find(name);if(p==null)p=new GameObject(name).transform;p.SetParent(root.transform,false);p.localPosition=position;return p;}
}
