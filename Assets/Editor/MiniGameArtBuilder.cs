using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Deterministic finishing pass shared by both scene builders.</summary>
public static class MiniGameArtBuilder
{
    public const string Models="Assets/Art/MiniGames/Models/";
    private static Material MaterialFor(Material original)
    {
        if(original==null)return null;
        original=ModernWorldArtBuilder.ResolveSourceMaterial(original);
        if(original.shader!=null&&original.shader.name==ModernWorldArtBuilder.ShaderName)return original;
        string name=original.name.Replace(" (Instance)","");
        string folder="Assets/Art/MiniGames/Materials";
        if(!AssetDatabase.IsValidFolder(folder)){System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();}
        string path=folder+"/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/StoreProducts/Materials/"+name+".mat");
        bool boulevard=name=="CH_Paving"||name=="CH_Roof"||name=="CH_Stucco"||name=="CH_Leaf"||name=="CH_Terracotta"||name=="CH_Porcelain";
        if(source==null&&boulevard)source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/StoreProducts/Materials/CH_White.mat");
        if(source==null)return original;
        if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
        Color color=source.color;
        switch(name)
        {
            case "CH_Cream":color=new Color32(224,201,166,255);break;
            case "CH_White":color=new Color32(251,244,228,255);break;
            case "CH_MintBright":color=new Color32(151,203,171,255);break;
            case "CH_TealLight":color=new Color32(73,147,136,255);break;
            case "CH_LilacBright":color=new Color32(170,155,194,255);break;
            case "CH_CoralBright":color=new Color32(224,137,119,255);break;
            case "CH_Screen":color=new Color32(89,140,148,255);break;
            case "CH_Gold":color=new Color32(199,157,87,255);break;
            case "CH_Paving":color=new Color32(202,186,158,255);break;
            case "CH_Roof":color=new Color32(60,103,99,255);break;
            case "CH_Stucco":color=new Color32(233,219,195,255);break;
            case "CH_Leaf":color=new Color32(79,127,86,255);break;
            case "CH_Terracotta":color=new Color32(170,95,71,255);break;
            case "CH_Porcelain":color=new Color32(250,244,231,255);break;
        }
        mat.color=color;
        if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",name=="CH_Screen"?.68f:name=="CH_Gold"?.4f:.27f);
        if(name=="CH_Cream"||name=="CH_MintBright"||name=="CH_LilacBright"||name=="CH_CoralBright")
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MiniGames/Textures/"+(name=="CH_Cream"?"OakGrain":"LinenWeave")+".png");
        EditorUtility.SetDirty(mat);return mat;
    }
    public static GameObject Model(string name,Transform parent,Vector3 position, float yaw=0)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Models+name+".fbx");
        if(source==null)throw new InvalidOperationException("Build the headless mini-game collection first: "+name);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source);
        go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials=r.sharedMaterials.Select(MaterialFor).ToArray();
        return go;
    }
    public static void Runner(Transform root)
    {
        var templates=root.Find("ScrollingTrack/Templates");
        string[] old={"YarnBasket","ScratchPost","FoodBowl","RobotVacuum","CatBed","TreatStack","CatCarrier","PillowPile","LeashArch","CatNapCanopy"};
        string[] art={"RunnerYarnBasket","RunnerScratchPost","RunnerFoodBowl","RunnerVacuum","RunnerCatBed","RunnerTreats","RunnerCarrier","RunnerPillows","RunnerRibbonGate","RunnerNapCanopy"};
        for(int i=0;i<old.Length;i++)
        {
            var target=templates.Find(old[i]+"_Template");
            if(target==null)throw new InvalidOperationException("Missing hazard "+old[i]);
            foreach(Transform child in target)
                if(!child.name.Contains("Telegraph")&&!child.name.Contains("Warning")) child.gameObject.SetActive(false);
            var model=Model(art[i],target,Vector3.zero);
            var bounds=new Bounds(model.transform.position,Vector3.zero);
            foreach(var r in model.GetComponentsInChildren<Renderer>(true))bounds.Encapsulate(r.bounds);
            var item=target.GetComponent<CatRunnerTrackObject>();
            var so=new SerializedObject(item);
            so.FindProperty("collisionBottomOffset").floatValue=i>=8?.50f:0f;
            so.FindProperty("collisionTopOffset").floatValue=bounds.max.y-target.position.y;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        int index=0;
        foreach(var segment in root.GetComponentsInChildren<CatRunnerScenerySegment>(true))
        {
            var near=segment.transform.Find("NearScenery");
            foreach(var t in near.GetComponentsInChildren<Transform>(true))
                if(t.name.Contains("PastelTownhouse"))t.gameObject.SetActive(false);
            Model("RunnerTownhouse"+(index%3),near,new Vector3(-5.4f,.08f,-1.3f),-32f);
            Model("RunnerTownhouse"+((index+1)%3),near,new Vector3(5.4f,.08f,1.5f),32f);
            index++;
        }
        Recolor("RunnerRoadPeach",new Color32(224,195,159,255));
        Recolor("RunnerRoadBerry",new Color32(207,178,175,255));
        Recolor("RunnerRoadAqua",new Color32(166,202,191,255));
        Recolor("RunnerFloorSunset",new Color32(104,155,136,255));
        Recolor("RunnerFloorCandy",new Color32(134,150,143,255));
        Recolor("RunnerFloorSky",new Color32(101,148,147,255));
        foreach(string name in new[]{"RunnerLaneMint","RunnerLanePink","RunnerLaneBlue"})Recolor(name,new Color32(255,241,202,255));
        var camera=root.GetComponentInChildren<Camera>();
        camera.transform.localPosition=new Vector3(0,2.15f,-4.65f);
        camera.transform.localRotation=Quaternion.LookRotation(new Vector3(0,.5f,5)-camera.transform.localPosition);
        var sky=GameObject.CreatePrimitive(PrimitiveType.Quad);sky.name="SoftHorizon";
        UnityEngine.Object.DestroyImmediate(sky.GetComponent<Collider>());sky.transform.SetParent(camera.transform,false);
        sky.transform.localPosition=new Vector3(0,0,105);sky.transform.localScale=new Vector3(220,120,1);
        const string skyPath="Assets/Art/MiniGames/Materials/SoftSky.mat";
        var skyMaterial=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if(skyMaterial==null){skyMaterial=new Material(Shader.Find("CatHome/MiniGameSky"));AssetDatabase.CreateAsset(skyMaterial,skyPath);}
        sky.GetComponent<Renderer>().sharedMaterial=skyMaterial;
        sky.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))if(ps.name.Contains("Speed"))
        {var main=ps.main;main.startSizeMultiplier=.018f;var emission=ps.emission;emission.rateOverTimeMultiplier=9;}
        var player=root.GetComponentInChildren<CatRunnerPlayer>();
        var visual=player.GetComponentInChildren<Animator>(true);
        if(visual.GetComponent<MiniGameCatAnimation>()==null)visual.gameObject.AddComponent<MiniGameCatAnimation>();
        RunnerBoulevardBuilder.Apply(root);
        ArcadeMiniGameArtBuilder.ApplyRunner(root);
    }
    private static void Recolor(string name,Color color)
    {
        var material=AssetDatabase.FindAssets(name+" t:Material")
            .Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(m=>m!=null&&m.name==name&&m.shader!=null&&m.shader.name!=ModernWorldArtBuilder.ShaderName);
        if(material==null)return;
        material.color=color;
        if(material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor",color*.08f);
        if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.22f);
        EditorUtility.SetDirty(material);
    }
    public static void Catch(Transform root)
    {
        var arena=root.Find("CatchArena");
        foreach(Transform child in arena)
            if(child.name=="Floor")child.GetComponent<Renderer>().enabled=false;
            else child.gameObject.SetActive(false);
        Model("CatchPlayroom",arena,Vector3.zero);
        foreach(var mouse in root.GetComponentsInChildren<CatCatchMouse>(true))
        {
            foreach(Transform child in mouse.transform)child.gameObject.SetActive(false);
            var model=Model("CatchFeltMouse",mouse.transform,new Vector3(0,-.12f,0));
            var marker=new GameObject("SelectedMouseRing");marker.transform.SetParent(model.transform,false);
            var ring=marker.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=40;ring.widthMultiplier=.016f;
            for(int i=0;i<40;i++){float angle=i*Mathf.PI*2/40;ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*.25f,.015f,Mathf.Sin(angle)*.25f));}
            const string markerPath="Assets/Art/MiniGames/Materials/SelectedMouse.mat";
            var markerMat=AssetDatabase.LoadAssetAtPath<Material>(markerPath);
            if(markerMat==null){markerMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));markerMat.color=new Color32(241,148,117,255);AssetDatabase.CreateAsset(markerMat,markerPath);}
            ring.sharedMaterial=markerMat;ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.enabled=false;
            model.AddComponent<CatchMousePresentation>();
        }
        var camera=root.GetComponentInChildren<Camera>();
        camera.transform.localPosition=new Vector3(0,8.1f,-7.65f);
        camera.transform.localRotation=Quaternion.Euler(48,0,0);camera.fieldOfView=42;
        var data=camera.GetComponent<UniversalAdditionalCameraData>();
        data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
        if(camera.GetComponent<CatCatchCameraRig>()==null)camera.gameObject.AddComponent<CatCatchCameraRig>();
        var visual=root.GetComponentInChildren<CatCatchPlayer>().GetComponentInChildren<Animator>(true);
        if(visual.GetComponent<MiniGameCatAnimation>()==null)visual.gameObject.AddComponent<MiniGameCatAnimation>();
        ArcadeMiniGameArtBuilder.ApplyCatch(root);
    }
}
