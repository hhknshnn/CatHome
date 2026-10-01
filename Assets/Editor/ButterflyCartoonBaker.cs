using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Offline cartoon production. The stage is a disposable preview scene, never gameplay.</summary>
public static class ButterflyCartoonBaker
{
    public const int Fps=24,Frames=240;
    public const string Folder="Library/ButterflyCartoonFrames";
    static Scene scene;
    static GameObject stage;
    static Transform actor,butterfly,leftWing,rightWing;
    static Animator animator;
    static AnimationClip run;
    static Camera camera;
    static RenderTexture target;
    static Texture2D pixels;
    static readonly List<Object> owned=new List<Object>();
    static int frame;
    public static string Status {get;private set;}="Idle";
    static Material Mat(Color color,bool unlit=false)
    {
        var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.15f);owned.Add(m);return m;
    }
    static Transform Shape(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=pos;go.transform.localScale=scale;Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
    }
    public static string Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||stage!=null)throw new InvalidOperationException("Idle Edit Mode required");
        Directory.CreateDirectory(Folder);scene=EditorSceneManager.NewPreviewScene();
        stage=new GameObject("Offline butterfly cartoon");SceneManager.MoveGameObjectToScene(stage,scene);
        var camObject=new GameObject("Cartoon capture camera");camObject.transform.SetParent(stage.transform,false);
        camera=camObject.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;
        camera.orthographicSize=1.66f;camera.aspect=16f/9;camera.nearClipPlane=.05f;camera.farClipPlane=30;
        camera.transform.position=new Vector3(0,1.7f,-7);camera.transform.LookAt(new Vector3(0,.65f,0));
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.53f,.79f,.94f);
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=true;data.volumeLayerMask=0;
        var ground=Mat(new Color(.53f,.72f,.27f));Shape("Meadow",PrimitiveType.Plane,stage.transform,Vector3.zero,new Vector3(2,1,2),ground);
        var backdrop=Mat(Color.white,true);backdrop.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/LivingComposition/Textures/WindowCountryside.png"));
        Shape("Painted countryside",PrimitiveType.Quad,stage.transform,new Vector3(0,1.7f,4.1f),new Vector3(10,4,1),backdrop);
        var key=new GameObject("Cartoon key");key.transform.SetParent(stage.transform,false);key.transform.rotation=Quaternion.Euler(42,-35,0);
        var light=key.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.92f,.78f);light.intensity=1.25f;light.shadows=LightShadows.Soft;
        var fill=new GameObject("Cartoon fill");fill.transform.SetParent(stage.transform,false);fill.transform.localPosition=new Vector3(0,3,-3);
        var fl=fill.AddComponent<Light>();fl.type=LightType.Point;fl.range=15;fl.intensity=8;fl.color=new Color(.83f,.92f,1);
        var catalog=CatBreedCatalog.Load();actor=new GameObject("Curious kitten").transform;actor.SetParent(stage.transform,false);actor.localScale=Vector3.one*1.2f;
        var visual=CatBreedVisualFactory.Create(catalog.Find("persian")??catalog.Get(0),catalog.GameplayController,actor);
        animator=visual.GetComponentInChildren<Animator>();animator.enabled=false;animator.Rebind();
        run=catalog.GameplayController.animationClips.First(c=>c.name.EndsWith("|Run"));
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
        butterfly=new GameObject("Playful butterfly").transform;butterfly.SetParent(stage.transform,false);
        var purple=Mat(new Color(.70f,.30f,.92f),true);var coral=Mat(new Color(1,.46f,.45f),true);var gold=Mat(new Color(1,.80f,.24f),true);
        Shape("Body",PrimitiveType.Sphere,butterfly,Vector3.zero,new Vector3(.055f,.14f,.06f),gold);
        leftWing=new GameObject("Left wing pivot").transform;leftWing.SetParent(butterfly,false);
        rightWing=new GameObject("Right wing pivot").transform;rightWing.SetParent(butterfly,false);
        Shape("Left upper wing",PrimitiveType.Sphere,leftWing,new Vector3(-.11f,.05f,0),new Vector3(.22f,.23f,.028f),purple);
        Shape("Right upper wing",PrimitiveType.Sphere,rightWing,new Vector3(.11f,.05f,0),new Vector3(.22f,.23f,.028f),purple);
        Shape("Left lower wing",PrimitiveType.Sphere,leftWing,new Vector3(-.075f,-.065f,0),new Vector3(.15f,.14f,.028f),coral);
        Shape("Right lower wing",PrimitiveType.Sphere,rightWing,new Vector3(.075f,-.065f,0),new Vector3(.15f,.14f,.028f),coral);
        var petal=Mat(new Color(1,.95f,.72f),true);
        for(int i=0;i<22;i++)
        {
            float x=-3.2f+(i%11)*.64f,z=i<11?1.1f:-1.1f;
            var flower=new GameObject("Meadow daisy").transform;flower.SetParent(stage.transform,false);flower.localPosition=new Vector3(x,.07f,z);
            for(int j=0;j<5;j++){float a=j*Mathf.PI*2/5;Shape("Petal",PrimitiveType.Sphere,flower,new Vector3(Mathf.Cos(a)*.047f,0,Mathf.Sin(a)*.047f),new Vector3(.067f,.028f,.067f),petal);}
            Shape("Gold centre",PrimitiveType.Sphere,flower,new Vector3(0,.012f,0),new Vector3(.042f,.032f,.042f),gold);
        }
        target=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=2};target.Create();
        pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);frame=0;Status="Baking 0 / "+Frames;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;return Status;
    }
    static void Tick()
    {
        try
        {
            float t=frame/(float)Fps,phase=t*Mathf.PI*2/10;
            float x=1.55f*Mathf.Sin(phase),z=.30f*Mathf.Cos(phase);
            run.SampleAnimation(animator.gameObject,(t*run.length*3f)%run.length);
            actor.localPosition=new Vector3(x,.015f,z);
            actor.localRotation=Quaternion.LookRotation(new Vector3(1.55f*Mathf.Cos(phase),0,-.30f*Mathf.Sin(phase)));
            butterfly.localPosition=new Vector3(2.2f*Mathf.Sin(phase+.8f),1.15f+.20f*Mathf.Sin(phase*3),.30f*Mathf.Cos(phase+.8f)-.14f);
            butterfly.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase*3)*16);
            float flap=Mathf.Sin(t*Mathf.PI*2*6)*65;leftWing.localRotation=Quaternion.Euler(0,flap,0);rightWing.localRotation=Quaternion.Euler(0,-flap,0);
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            var previous=RenderTexture.active;try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Folder+"/"+frame.ToString("D4")+".png",pixels.EncodeToPNG());}finally{RenderTexture.active=previous;}
            frame++;Status="Baking "+frame+" / "+Frames;if(frame>=Frames){Status="Complete: 240 frames, 10 seconds, 24 FPS";Cleanup();}
        }
        catch(Exception e){Status="Failed: "+e;Cleanup();Debug.LogException(e);}
    }
    static void Cleanup()
    {
        EditorApplication.update-=Tick;
        if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);stage=null;
        if(target!=null){target.Release();Object.DestroyImmediate(target);target=null;}
        if(pixels!=null){Object.DestroyImmediate(pixels);pixels=null;}
        foreach(var o in owned)if(o!=null)Object.DestroyImmediate(o);owned.Clear();
    }
}
