using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authored care strip, compact armchair, and permanent furniture interactions.</summary>
public static class LivingRoomArrangementBuilder
{
    public static string BuildSilently()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Finish Play before authoring.");
        CatHomeAuthoringWorkspace.OpenFullHomePreview(false);
        var scene=SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        StoreProductContentBuilder.RebuildProductWithExistingMaterials(HomeStoreService.ArmchairId);
        StoreCatalogAssets.TryGet(HomeStoreService.ArmchairId,out var definition);
        foreach(var root in scene.GetRootGameObjects())foreach(var p in root.GetComponentsInChildren<HomeProductPlacement>(true))
        {
            if(p.ProductId!=HomeStoreService.ArmchairId)continue;
            var copy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/ClassicArmchair.prefab"),scene);
            copy.name=p.name;copy.transform.SetParent(p.transform.parent,false);
            copy.transform.SetPositionAndRotation(definition.DefaultPosition,Quaternion.Euler(0,definition.DefaultYaw,0));
            UnityEngine.Object.DestroyImmediate(p.gameObject);break;
        }
        Apply(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Compact armchair, care strip, sofa/table jump routines and automatic CAT arrangement authored.";
    }
    public static void Apply(Scene scene)
    {
        Move(scene,"FoodBowl",new Vector3(3.22f,0,-.35f));
        Move(scene,"WaterBowl",new Vector3(3.22f,0,-.98f));
        Move(scene,"FoodInteractionPoint",new Vector3(2.75f,0,-.35f));
        Move(scene,"WaterInteractionPoint",new Vector3(2.75f,0,-.98f));
        var parent=Find(scene,"RoomFurniture");
        var root=Find(scene,"LivingFurniturePlay");
        if(root==null){root=new GameObject("LivingFurniturePlay").transform;SceneManager.MoveGameObjectToScene(root.gameObject,scene);root.SetParent(parent,false);}
        var sofa=Find(scene,HomeRoomGameplaySafetyBuilder.LivingSofaName);
        var table=Find(scene,HomeRoomGameplaySafetyBuilder.LivingCoffeeTableName);
        if(sofa!=null)
        {
            // The centre seam exposes the frame 16 cm below the cushions.
            // Rest wholly on the left cushion, measuring its real upper face.
            Vector3 support=new Vector3(-.40f,0,2.02f);support.y=MeasureTop(sofa,support,.52f);
            CreateActivity(root,"SofaLounge",CatActivityKind.SofaLounge,new Vector3(-.40f,0,1.30f),support,new Vector2(.72f,.58f),false,null);
        }
        if(table!=null)
        {
            Vector3 support=new Vector3(.04f,0,.56f);support.y=MeasureTop(table,support,.48f)+.018f;
            var mint=Find(scene,"MintCenterpiece");
            if(mint!=null)
            {
                mint.position=new Vector3(.25f,support.y+.032f,.56f);
                mint.localScale=new Vector3(.12f,.065f,.12f);
            }
            CreateActivity(root,"CoffeeTablePlay",CatActivityKind.CoffeeTablePlay,new Vector3(.95f,0,.56f),support,new Vector2(.80f,.58f),true,mint);
        }
        var arrangement=CatRoomArrangement.Request(scene);if(arrangement!=null)arrangement.Invalidate();
    }
    static void CreateActivity(Transform root,string name,CatActivityKind kind,Vector3 entry,Vector3 perch,Vector2 size,bool table,Transform toy)
    {
        var t=root.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(root,false);}
        var floor=Point(t,table?"TableJumpEntry":"SofaJumpEntry",entry);
        var support=Point(t,"MeasuredSupport",perch);support.rotation=Quaternion.Euler(0,90,0);
        var surface=support.GetComponent<CatActivitySurface>()??support.gameObject.AddComponent<CatActivitySurface>();
        surface.EditorConfigure(new Vector2(size.y,size.x));
        var activity=t.GetComponent<LivingFurnitureActivity>()??t.gameObject.AddComponent<LivingFurnitureActivity>();
        activity.EditorConfigure(name,table?"Sehpa oyunu":"Koltuk keyfi",kind,table?QuestType.KnockOff:QuestType.Sleep,0,
            table?"JUMP ON TABLE":"JUMP ON SOFA",1.4f,table?2:0,floor,null,null);
        activity.EditorConfigureEntry(floor);
        activity.EditorConfigureFurniture(support,table,toy,new Vector3(.60f,perch.y+.032f,.56f),new Vector3(.82f,.035f,.56f));
        EditorUtility.SetDirty(activity);EditorUtility.SetDirty(surface);
    }
    public static float MeasureTop(Transform root,Vector3 point,float near)
    {
        float best=near,distance=.20f;bool found=false;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=filter.sharedMesh;if(mesh==null)continue;
            var vertices=mesh.vertices;var triangles=mesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=filter.transform.TransformPoint(vertices[triangles[i]]);var b=filter.transform.TransformPoint(vertices[triangles[i+1]]);var c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                if(Vector3.Cross(b-a,c-a).normalized.y<.6f)continue;
                float denominator=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(denominator)<.000001f)continue;
                float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.z-c.z))/denominator;
                float v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.z-c.z))/denominator;
                if(u<-.001f||v<-.001f||u+v>1.001f)continue;
                float y=u*a.y+v*b.y+(1-u-v)*c.y;
                if(Mathf.Abs(y-near)<distance){distance=Mathf.Abs(y-near);best=y;found=true;}
            }
        }
        if(!found)throw new InvalidOperationException("No real support on "+root.name+" at "+point);
        return best;
    }
    static Transform Point(Transform parent,string name,Vector3 position)
    {var t=parent.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(parent,false);}t.position=position;return t;}
    static void Move(Scene scene,string name,Vector3 at){var t=Find(scene,name);if(t!=null)t.position=at;}
    static Transform Find(Scene scene,string name)
    {foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
}
