using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Living room environment art only. Never purchases products or writes player data.</summary>
public static class LivingCompositionBuilder
{
    public const string Art="Assets/Art/LivingComposition";
    public const string Qa="Docs/QA/LIVING_COMPOSITION_2026-09-30";
    static readonly Dictionary<string,Color> Colors=new Dictionary<string,Color>{
        {"LC_Cream",new Color(.96f,.90f,.76f)}, {"LC_Mint",new Color(.43f,.69f,.59f)},
        {"LC_Oak",new Color(.67f,.43f,.24f)}, {"LC_Gold",new Color(.78f,.60f,.30f)},
        {"LC_Green",new Color(.22f,.40f,.10f)}, {"LC_Leaf",new Color(.39f,.59f,.18f)},
        {"LC_Soil",new Color(.17f,.12f,.07f)}, {"LC_Coral",new Color(.86f,.39f,.32f)}};
    public static Scene Room=>SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
    public static Transform Find(string name)=>Room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name==name);
    static void Check(){if(EditorApplication.isPlayingOrWillChangePlaymode||!Room.isLoaded)throw new InvalidOperationException("Loaded living room in Edit Mode required.");}
    public static Material Material(string name)
    {
        string path=Art+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m!=null)return m;
        Directory.CreateDirectory(Art+"/Materials");
        m=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/StoreProducts/Materials/CH_White.mat")){name=name};
        m.SetColor("_BaseColor",Colors.ContainsKey(name)?Colors[name]:Color.white);
        m.SetFloat("_Metallic",name=="LC_Gold"?.4f:0);m.SetFloat("_Smoothness",name=="LC_Gold"?.55f:.28f);
        m.SetTexture("_BaseMap",null);AssetDatabase.CreateAsset(m,path);return m;
    }
    public static GameObject Model(string name,Transform parent)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+name+".fbx");
        if(source==null)throw new InvalidOperationException("Missing model "+name);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.name=name;
        model.transform.SetParent(parent,false);model.transform.localPosition=Vector3.zero;
        // Authoring mesh is already Y-up. Remove the FBX object's conversion rotation.
        model.transform.localRotation=Quaternion.identity;model.transform.localScale=Vector3.one;
        foreach(var r in model.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>Material(m.name.Split('.')[0])).ToArray();
        return model;
    }
    public static void Artwork(Transform parent,int panel,float width,float height)
    {
        string meshPath=Art+"/ArtPanel"+panel+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null){mesh=new Mesh{name="ArtPanel"+panel};mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.triangles=new[]{0,2,1,0,3,2};float u=panel==0?0:panel==1?.25f:.5f;float end=panel==2?1:u+.25f;
            mesh.uv=new[]{new Vector2(u,0),new Vector2(end,0),new Vector2(end,1),new Vector2(u,1)};mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,meshPath);}
        var mat=Material("LC_Art");mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/LivingArt.png"));EditorUtility.SetDirty(mat);
        var go=new GameObject("PrintedArt",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=new Vector3(0,0,-.02f);go.transform.localScale=new Vector3(width,height,1);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
    }
    public static string ApplyStart()
    {
        Check();CatHomeEditPreview.Clear();var old=Find("LivingPermanentDecor");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("LivingPermanentDecor");SceneManager.MoveGameObjectToScene(root,Room);
        var f=Model("SmallWallFrame",root.transform);f.name="SmallCatPrint";f.transform.SetPositionAndRotation(new Vector3(-3.70f,1.82f,.55f),Quaternion.Euler(0,270,0));Artwork(f.transform,0,.352f,.492f);
        f=Model("SmallWallFrame",root.transform);f.name="SmallBotanicalPrint";f.transform.SetPositionAndRotation(new Vector3(-3.70f,1.78f,1.35f),Quaternion.Euler(0,270,0));f.transform.localScale=Vector3.one*.85f;Artwork(f.transform,1,.352f,.492f);
        var p=Model("SmallCeramicPlant",root.transform);p.name="WindowSillPlant";p.transform.position=new Vector3(3.56f,1.19f,-.14f);p.transform.localScale=Vector3.one*.75f;
        p=Model("SmallCeramicPlant",root.transform);p.name="CoffeeTablePlant";p.transform.position=new Vector3(1.50f,.485f,-.28f);p.transform.localScale=Vector3.one*.62f;
        var c=Model("ShortWindowCurtains",root.transform);c.transform.SetPositionAndRotation(new Vector3(3.57f,1.85f,-.43f),Quaternion.Euler(0,90,0));c.transform.localScale=new Vector3(.82f,1,1);
        Directory.CreateDirectory(Art+"/Prefabs");PrefabUtility.SaveAsPrefabAssetAndConnect(root,Art+"/Prefabs/LivingPermanentDecor.prefab",InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(Room);EditorSceneManager.SaveScene(Room);AssetDatabase.SaveAssets();CatHomeEditPreview.Refresh();return "Small decor installed; existing furniture, camera, lighting, gameplay anchors unchanged.";
    }
    public static string Preview(bool full)
    {
        Check();CatHomeEditPreview.Refresh();var s=Resources.FindObjectsOfTypeAll<EditorHomePreviewState>().First();
        var ids=full?new[]{HomeStoreService.BallBasketId,HomeStoreService.FeatherToyId,HomeStoreService.ToyMouseId,HomeStoreService.BellCollarId}:Array.Empty<string>();
        foreach(var root in Room.GetRootGameObjects()){
            foreach(var d in root.GetComponentsInChildren<StoreProductDisplay>(true))s.Show(new SerializedObject(d).FindProperty("visualRoot").objectReferenceValue as GameObject,full&&(HomeStoreService.IsLivingRoomCollectionProduct(d.ProductId)||ids.Contains(d.ProductId)));
            foreach(var a in root.GetComponentsInChildren<CatActivity>(true))if(a.RequiresStoreOwnership)s.Show(new SerializedObject(a).FindProperty("unlockedContent").objectReferenceValue as GameObject,full&&(HomeStoreService.IsLivingRoomCollectionProduct(a.StoreProductId)||ids.Contains(a.StoreProductId)));
        }
        var planner=s.GetComponent<CatRoomArrangement>();planner.Invalidate();if(!planner.TryPlan(ids,out var plan))throw new InvalidOperationException("Legal CAT preview failed: "+planner.LastFailure);
        foreach(var p in Room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HomeProductPlacement>(true)))if(plan.TryGetValue(p.ProductId,out var pose))s.Place(p.MovableRoot,pose.position,Quaternion.Euler(0,pose.yaw,0));
        // Edit Mode does not run the attachment Awake used by the real game.
        // Project the exact serialized attachment pose without granting ownership.
        var placements=Room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HomeProductPlacement>(true)).ToArray();
        foreach(var p in placements){var a=p.GetComponent<HomeRequiredProductAttachment>();if(a==null)continue;
            var target=placements.FirstOrDefault(x=>x.ProductId==a.RequiredProductId);if(target==null)continue;
            var so=new SerializedObject(a);var pos=so.FindProperty("targetLocalPosition").vector3Value;var angles=so.FindProperty("targetLocalEulerAngles").vector3Value;
            s.Place(p.MovableRoot,target.MovableRoot.TransformPoint(pos),target.MovableRoot.rotation*Quaternion.Euler(angles));}
        Physics.SyncTransforms();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();return "Read-only preview: "+(full?"10 ROOM + 4 legal CAT":"start room");
    }
    public static string Capture(string name)
    {
        var v=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        var rt=(RenderTexture)v.GetType().GetField("m_RenderTexture",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(v);
        var old=RenderTexture.active;var t=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        try{RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
            if(SystemInfo.graphicsUVStartsAtTop){var pixels=t.GetPixels();var row=new Color[rt.width];for(int y=0;y<rt.height/2;y++){int a=y*rt.width,b=(rt.height-1-y)*rt.width;Array.Copy(pixels,a,row,0,rt.width);Array.Copy(pixels,b,pixels,a,rt.width);Array.Copy(row,0,pixels,b,rt.width);}t.SetPixels(pixels);}
            t.Apply();Directory.CreateDirectory(Qa);File.WriteAllBytes(Qa+"/"+name+".png",t.EncodeToPNG());}
        finally{RenderTexture.active=old;Object.DestroyImmediate(t);}return Qa+"/"+name+".png";
    }
}
