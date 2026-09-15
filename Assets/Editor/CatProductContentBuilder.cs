using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Premium CAT product prefabs and their measured interaction geometry.</summary>
public static class CatProductContentBuilder
{
    public const string Models="Assets/Art/PremiumPet/Models/";
    public const string Prefabs="Assets/Art/StoreProducts/Prefabs/";
    public static readonly string[] Names={"BallBasket","ScratchPost","CozyPodBed","ToyMouse","PlayTunnel","CeramicBowl","CloudBed","CanopyBed","TreatJar","FeatherToy","ClassicCollar","WalkingLeash","BellCollar","KibbleBag","NapPillow","CatnipPlanter","CardboardHideout"};
    public static readonly StoreCatalogAsset[] LegacyDefinitions={
        new StoreCatalogAsset(HomeStoreService.BallBasketId,"BallBasket",Models+"BallBasket_Premium.fbx",new Vector2(.78f,.62f),.52f,new Vector3(-3.15f,0,.75f),0,HomeProductPlacementKind.Floor,1,Vector3.zero,true),
        new StoreCatalogAsset(HomeStoreService.ScratchPostId,"ScratchPost",Models+"ScratchPost_Premium.fbx",new Vector2(.72f,.72f),.90f,new Vector3(3.15f,0,1.55f),0,HomeProductPlacementKind.Floor,1,Vector3.zero,true)
    };
    public static bool TryBuild(StoreCatalogAsset definition,IReadOnlyDictionary<string,Material> materials)
    {
        if(!HomeStoreService.TryGetProduct(definition.ProductId,out var product)||product.StoreCategory!=HomeStoreCategory.Cat)return false;
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Models+definition.PrefabName+"_Premium.fbx");
        if(asset==null)return false;
        var root=new GameObject("StoreProduct_"+definition.PrefabName);
        try
        {
            var visual=new GameObject("VisualContent");visual.transform.SetParent(root.transform,false);
            var body=AddModel(asset,visual.transform,"Body",materials);
            Bounds bodyBounds=LocalBounds(body,root.transform);
            if(bodyBounds.max.y>.12f && definition.PrefabName!="NapPillow" && definition.PrefabName!="WalkingLeash")
                foreach(var filter in body.GetComponentsInChildren<MeshFilter>())
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            var trigger=visual.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up*definition.Height*.5f;
            trigger.size=new Vector3(definition.Footprint.x,definition.Height,definition.Footprint.y);
            if(definition.PrefabName=="PlayTunnel")
            {
                // The crawl is an explicit activity from either mouth. Keep
                // manual movement outside, including the otherwise open ends.
                // The routine already owns/disables the cat controller; this
                // fitted obstacle therefore remains solid even after cancel.
                var blocker=visual.AddComponent<BoxCollider>();
                blocker.center=bodyBounds.center;blocker.size=bodyBounds.size;
            }
            Transform moving=null;
            var movingAsset=AssetDatabase.LoadAssetAtPath<GameObject>(Models+definition.PrefabName+"Moving_Premium.fbx");
            CatEnrichmentMode mode=ModeFor(definition.PrefabName);
            if(movingAsset!=null)
            {
                var model=AddModel(movingAsset,visual.transform,"MovingMesh",materials);
                Bounds b=LocalBounds(model,root.transform);Vector3 pivot=b.center;
                if(mode==CatEnrichmentMode.Track)pivot=new Vector3(0,b.center.y,0);
                if(mode==CatEnrichmentMode.Spring||mode==CatEnrichmentMode.Grass)pivot=new Vector3(b.center.x,b.min.y,b.center.z);
                moving=Point(visual.transform,"MovingPivot",pivot);model.transform.SetParent(moving,false);model.transform.localPosition=-pivot;
            }
            root.AddComponent<StoreProductDisplay>().EditorConfigure(definition.ProductId,visual);
            root.AddComponent<HomeProductPlacement>().EditorConfigure(definition.ProductId,root.transform,Array.Empty<Transform>(),definition.Footprint,definition.PlacementKind,"",definition.HungHeight);
            bool legacy=definition.ProductId==HomeStoreService.BallBasketId||definition.ProductId==HomeStoreService.ScratchPostId;
            if(!legacy)
            {
                float edge=bodyBounds.min.z;
                var entry=Point(root.transform,"InteractionAnchor",new Vector3(0,0,edge-.48f));
                var exit=Point(root.transform,"ExitPoint",new Vector3(0,0,mode==CatEnrichmentMode.Tunnel?bodyBounds.max.z+.48f:edge-.48f));
                bool inside=mode==CatEnrichmentMode.Nap||mode==CatEnrichmentMode.Hide||mode==CatEnrichmentMode.Tunnel;
                float y=inside&&mode!=CatEnrichmentMode.Tunnel?MeasurePadHeight(body,root.transform):0;
                Vector3 touch=inside?new Vector3(0,y,0):moving!=null?moving.localPosition:new Vector3(0,bodyBounds.center.y,bodyBounds.min.z);
                if(!inside && moving!=null)
                {
                    var mb=LocalBounds(moving.gameObject,root.transform);
                    touch=new Vector3(mb.center.x,mb.center.y,mb.min.z+.025f);
                    if(mode==CatEnrichmentMode.Spring)touch.y=Mathf.Lerp(mb.min.y,mb.max.y,.65f);
                    if(mode==CatEnrichmentMode.Feed)touch=new Vector3(0,mb.min.y+.025f,mb.min.z+.015f);
                    if(mode==CatEnrichmentMode.Track)touch=new Vector3(mb.center.x,mb.center.y,mb.center.z);
                }
                Vector2 foodRadii=Vector2.zero;
                if(definition.ProductId==HomeStoreService.CeramicBowlId)touch=MeasureCeramicFood(body,root.transform,out foodRadii);
                var contact=Point(root.transform,"ContactPoint",touch);
                if(mode==CatEnrichmentMode.Nap||mode==CatEnrichmentMode.Hide)
                {
                    var surface=contact.gameObject.AddComponent<CatActivitySurface>();
                    surface.EditorConfigure(new Vector2(definition.Footprint.x*.72f,definition.Footprint.y*.67f));
                }
                var activity=root.AddComponent<CatEnrichmentActivity>();
                CatActivityKind kind=KindFor(definition.PrefabName);
                QuestType quest=mode==CatEnrichmentMode.Nap?QuestType.Sleep:mode==CatEnrichmentMode.Feed?QuestType.Eat:
                    mode==CatEnrichmentMode.Tunnel?QuestType.TunnelPlay:mode==CatEnrichmentMode.Spring?QuestType.FeatherPlay:QuestType.PlayBall;
                activity.EditorConfigure("pet-"+definition.PrefabName,product.Title,kind,quest,0,mode==CatEnrichmentMode.Nap?"REST":"PLAY",1.35f,mode==CatEnrichmentMode.Nap?0:4,entry,null,visual);
                activity.EditorConfigureStoreProduct(definition.ProductId);activity.EditorConfigureEntry(entry);
                activity.EditorConfigureEnrichment(mode,contact,exit,moving,definition.Footprint);
                activity.EditorConfigureFoodSurface(foodRadii);
            }
            ModernWorldArtBuilder.ApplyRoot(root.transform,HomeRoomService.LivingRoomId);
            PrefabUtility.SaveAsPrefabAsset(root,Prefabs+definition.PrefabName+".prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(root);}
        return true;
    }
    public static void BuildLegacyAssets(IReadOnlyDictionary<string,Material> materials)
    {foreach(var definition in LegacyDefinitions)TryBuild(definition,materials);}
    public static void ApplyCeramicFoodContact()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        string path=Prefabs+"CeramicBowl.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var activity=root.GetComponent<CatEnrichmentActivity>();
            activity.ContactPoint.localPosition=MeasureCeramicFood(root,root.transform,out var radii);
            activity.EditorConfigureFoodSurface(radii);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static Vector3 MeasureCeramicFood(GameObject body,Transform root,out Vector2 radii)
    {
        Bounds bounds=default;bool found=false;
        foreach(var filter in body.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer=filter.GetComponent<Renderer>();if(renderer==null)continue;
            using(var data=MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
            using(var vertices=new Unity.Collections.NativeArray<Vector3>(data[0].vertexCount,Unity.Collections.Allocator.Temp))
            {
                data[0].GetVertices(vertices);
                for(int sub=0;sub<data[0].subMeshCount;sub++)
                {
                    var material=renderer.sharedMaterials[sub];
                    if(material==null||material.name.IndexOf("Cream",StringComparison.OrdinalIgnoreCase)<0)continue;
                    using(var indices=new Unity.Collections.NativeArray<int>(data[0].GetSubMesh(sub).indexCount,Unity.Collections.Allocator.Temp))
                    {
                        data[0].GetIndices(indices,sub);
                        foreach(int index in indices)
                        {
                            Vector3 point=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[index]));
                            if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);
                        }
                    }
                }
            }
        }
        if(!found)throw new InvalidOperationException("Ceramic bowl's actual food mesh is missing.");
        radii=new Vector2(bounds.extents.x,bounds.extents.z);
        return new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);
    }
    public static void BuildCollectionSilently()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var materials=new Dictionary<string,Material>();
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/StoreProducts/Materials"}))
        {var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));materials[material.name]=material;}
        foreach(var definition in StoreCatalogAssets.PlaceableProducts)TryBuild(definition,materials);
        BuildLegacyAssets(materials);
        var scene=SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(HomeRoomService.LivingRoomScenePath,OpenSceneMode.Additive);
        // Replace only CAT instances. Keep ROOM geometry and the user's shared presentation intact.
        foreach(var root in scene.GetRootGameObjects())
        foreach(var old in root.GetComponentsInChildren<StoreProductDisplay>(true))
        {
            if(!HomeStoreService.TryGetProduct(old.ProductId,out var product)||product.StoreCategory!=HomeStoreCategory.Cat)continue;
            foreach(var definition in StoreCatalogAssets.PlaceableProducts)
            {
                if(definition.ProductId!=old.ProductId)continue;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+definition.PrefabName+".prefab"),scene);
                instance.transform.SetParent(old.transform.parent,false);
                instance.transform.SetPositionAndRotation(definition.DefaultPosition,Quaternion.Euler(0,definition.DefaultYaw,0));
                instance.name=old.name;UnityEngine.Object.DestroyImmediate(old.gameObject);break;
            }
        }
        UpgradeLegacyStations(scene);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        if(opened)EditorSceneManager.CloseScene(scene,true);
    }
    public static void UpgradeLegacyStations(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())
        foreach(var activity in root.GetComponentsInChildren<CatActivity>(true))
        {
            string name=activity.StoreProductId==HomeStoreService.BallBasketId?"BallBasket":activity.StoreProductId==HomeStoreService.ScratchPostId?"ScratchPost":null;
            if(name==null)continue;
            if(name=="BallBasket" && activity.RoutineEntryPoint!=null)
                activity.RoutineEntryPoint.localPosition=new Vector3(0,0,-.68f);
            if(StoreCatalogAssets.TryGetCatPose(activity.StoreProductId,out var at,out var yaw))
            {
                activity.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                var placement=activity.GetComponent<HomeProductPlacement>();
                if(placement!=null)
                {
                    var slots=new SerializedObject(placement).FindProperty("legacySlots");
                    if(slots.arraySize>0)
                    {
                        var first=slots.GetArrayElementAtIndex(0).objectReferenceValue as Transform;
                        if(first!=null)first.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                    }
                }
            }
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+name+".prefab");if(prefab==null)continue;
            var data=new SerializedObject(activity);var content=data.FindProperty("unlockedContent").objectReferenceValue as GameObject;
            if(content==null)continue;
            Transform ball=null;
            if(activity is BallChaseActivity)ball=data.FindProperty("ball").objectReferenceValue as Transform;
            for(int i=content.transform.childCount-1;i>=0;i--)
            {var child=content.transform.GetChild(i);if(child!=ball)UnityEngine.Object.DestroyImmediate(child.gameObject);}
            foreach(var collider in content.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            var source=prefab.transform.Find("VisualContent");
            var copy=UnityEngine.Object.Instantiate(source.gameObject,content.transform);copy.name="PremiumPetVisual";copy.SetActive(true);
            if(ball!=null)
            {
                var moving=copy.transform.Find("MovingPivot");
                if(moving!=null)
                {
                    for(int i=ball.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(ball.GetChild(i).gameObject);
                    foreach(var renderer in ball.GetComponents<Renderer>())UnityEngine.Object.DestroyImmediate(renderer);
                    var mesh=moving.GetChild(0);Vector3 center=LocalBounds(mesh.gameObject,moving).center;
                    mesh.SetParent(ball,false);mesh.localPosition-=center;mesh.localScale=Vector3.one;
                    UnityEngine.Object.DestroyImmediate(moving.gameObject);
                    ball.localScale=Vector3.one;
                }
            }
            else
            {
                var scratch=activity as ScratchPostActivity;
                scratch.EditorConfigureToy(copy.transform.Find("MovingPivot"));
                var point=data.FindProperty("scratchPoint").objectReferenceValue as Transform;
                if(point!=null){point.localPosition=new Vector3(0,0,-.33f);scratch.EditorConfigureScratch(point,2.4f);}
                // Measure only the rope shaft, excluding its plinth, cap and hanging toy.
                Bounds rope=new Bounds();bool measured=false;
                foreach(var f in copy.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(f.transform.IsChildOf(copy.transform.Find("MovingPivot")))continue;
                    foreach(var v in f.sharedMesh.vertices)
                    {
                        var local=activity.transform.InverseTransformPoint(f.transform.TransformPoint(v));
                        if(local.y<.20f || local.y>.65f)continue;
                        if(!measured){rope=new Bounds(local,Vector3.zero);measured=true;}else rope.Encapsulate(local);
                    }
                }
                if(measured)scratch.EditorConfigureRope(rope.center,Mathf.Max(rope.extents.x,rope.extents.z));
            }
        }
    }
    static GameObject AddModel(GameObject asset,Transform parent,string name,IReadOnlyDictionary<string,Material> materials)
    {
        var model=PrefabUtility.InstantiatePrefab(asset) as GameObject;model.name=name;model.transform.SetParent(parent,false);
        // Geometry is authored Y-up. Clear the importer's root conversion, as the furniture builder does.
        model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
        {
            var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++)if(slots[i]!=null&&materials.TryGetValue(slots[i].name,out var material))slots[i]=material;
            renderer.sharedMaterials=slots;
        }
        return model;
    }
    public static Bounds LocalBounds(GameObject model,Transform root)
    {
        var bounds=new Bounds();bool started=false;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))foreach(var v in filter.sharedMesh.vertices)
        {Vector3 p=root.InverseTransformPoint(filter.transform.TransformPoint(v));if(!started){bounds=new Bounds(p,Vector3.zero);started=true;}else bounds.Encapsulate(p);}
        return bounds;
    }
    static float MeasurePadHeight(GameObject model,Transform root)
    {
        // Horizontal triangle centroids on the low central cushion; never a product bounding box.
        float y=0;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=filter.sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 a=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i]]));
                Vector3 b=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i+1]]));
                Vector3 c=root.InverseTransformPoint(filter.transform.TransformPoint(vertices[triangles[i+2]]));
                Vector3 p=(a+b+c)/3;
                if(Mathf.Abs(p.x)<.23f&&Mathf.Abs(p.z)<.23f&&p.y<.22f&&Vector3.Cross(b-a,c-a).normalized.y>.6f)y=Mathf.Max(y,p.y);
            }
        }
        return y;
    }
    static Transform Point(Transform parent,string name,Vector3 at)
    {var p=new GameObject(name).transform;p.SetParent(parent,false);p.localPosition=at;return p;}
    public static CatEnrichmentMode ModeFor(string name)
    {
        switch(name){case "CozyPodBed":case "CloudBed":case "CanopyBed":case "NapPillow":return CatEnrichmentMode.Nap;
        case "PlayTunnel":return CatEnrichmentMode.Tunnel;case "ToyMouse":return CatEnrichmentMode.Chase;
        case "ClassicCollar":return CatEnrichmentMode.Track;case "BellCollar":return CatEnrichmentMode.Roller;
        case "WalkingLeash":return CatEnrichmentMode.Ribbon;case "CeramicBowl":case "TreatJar":case "KibbleBag":return CatEnrichmentMode.Feed;
        case "CatnipPlanter":return CatEnrichmentMode.Grass;case "CardboardHideout":return CatEnrichmentMode.Hide;
        default:return CatEnrichmentMode.Spring;}
    }
    public static CatActivityKind KindFor(string name)
    {
        switch(name){case "CozyPodBed":return CatActivityKind.PodNap;case "ToyMouse":return CatActivityKind.ToyMousePlay;
        case "PlayTunnel":return CatActivityKind.TunnelPlay;case "CeramicBowl":return CatActivityKind.CeramicMeal;
        case "CloudBed":return CatActivityKind.CloudNap;case "CanopyBed":return CatActivityKind.PetCanopyNap;
        case "TreatJar":return CatActivityKind.TreatPuzzle;case "FeatherToy":return CatActivityKind.FeatherPlay;
        case "ClassicCollar":return CatActivityKind.BallTrack;case "WalkingLeash":return CatActivityKind.RibbonPlay;
        case "BellCollar":return CatActivityKind.BellRoller;case "KibbleBag":return CatActivityKind.FoodDispenser;
        case "NapPillow":return CatActivityKind.PillowNap;case "CatnipPlanter":return CatActivityKind.CatGrassPlay;
        case "CardboardHideout":return CatActivityKind.BoxHide;default:throw new ArgumentException(name);}
    }
}
