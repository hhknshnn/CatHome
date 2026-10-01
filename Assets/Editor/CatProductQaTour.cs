using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Real-room renders and live routine contact photos on the isolated QA save.</summary>
public static class CatProductQaTour
{
    public static string Output="Docs/QA/CAT_2026-09-06_Refinement";
    static CatEnrichmentActivity[] products;static CatMovement cat;static int index;static bool started,shot;static double next,deadline,readySince;
    public static string Status{get;private set;}="Idle";
    public static void Start()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Isolated Play required");
        UiQaVisualTour.Clear();var owned=new System.Collections.Generic.List<string>();
        foreach(var d in Object.FindObjectsByType<StoreProductDisplay>(FindObjectsInactive.Include))owned.Add(d.ProductId);
        owned.Add(HomeStoreService.BallBasketId);owned.Add(HomeStoreService.ScratchPostId);
        var state=HomeStoreSaveState.CreateDefault();state.ownedProductIds=owned.ToArray();state.storedProductIds=owned.FindAll(CatCollectionPolicy.IsCatItem).ToArray();HomeStoreService.ApplySavedState(state);
        cat=Object.FindAnyObjectByType<CatMovement>();products=Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include).OrderBy(x=>x.name).ToArray();
        index=0;started=false;Time.timeScale=1;next=EditorApplication.timeSinceStartup+1;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!Application.isPlaying){EditorApplication.update-=Tick;return;}
        if(EditorApplication.timeSinceStartup<next)return;
        if(index>=products.Length){Status="Complete";EditorApplication.update-=Tick;return;}
        var a=products[index];Status=a.name;
        if(!started)
        {
            foreach(var p in HomeStoreService.Products)if(CatCollectionPolicy.IsCatItem(p.Id)&&HomeStoreService.IsOwned(p.Id))HomeStoreService.TrySetStored(p.Id,true);
            HomeStoreService.TrySetPlacement(a.StoreProductId,new Vector3(a.Mode==CatEnrichmentMode.Tunnel?1.4f:0,0,-.8f),0);HomeStoreService.TrySetStored(a.StoreProductId,false);
            Object.FindAnyObjectByType<EnergySystem>().ApplySavedValue(70);
            var controller=cat.GetComponent<CharacterController>();controller.enabled=false;cat.transform.position=new Vector3(0,0,-1.7f);controller.enabled=true;
            if(!a.TryStart(cat)){Status="REFUSED "+a.name;EditorApplication.update-=Tick;return;}
            started=true;shot=false;readySince=0;deadline=EditorApplication.timeSinceStartup+20;next=EditorApplication.timeSinceStartup+.6;return;
        }
        var animation=cat.GetComponent<CatActivityAnimation>();
        bool ready=a.ContactCount>0;
        if(a.Mode==CatEnrichmentMode.Nap||a.Mode==CatEnrichmentMode.Hide)ready=animation.CurrentPose==CatActivityPose.Sleep;
        if(a.Mode==CatEnrichmentMode.Tunnel)ready=animation.CurrentPose==CatActivityPose.Crawl&&Vector3.Distance(cat.transform.position,a.transform.position)<.3f;
        if(!ready)readySince=0;else if(readySince==0)readySince=EditorApplication.timeSinceStartup;
        double settle=a.Mode==CatEnrichmentMode.Nap||a.Mode==CatEnrichmentMode.Hide?.7:a.Mode==CatEnrichmentMode.Tunnel?.08:.06;
        if(a.IsRunning&&!shot&&ready&&EditorApplication.timeSinceStartup-readySince>settle)
        {
            // Give the skeletal pose one rendered frame to settle before the camera samples it.
            Capture(a.name+"_contact",a.transform.position+new Vector3(0,.22f,0),a.transform.rotation*new Vector3(1.7f,1.25f,-2.1f));shot=true;
        }
        if(!a.IsRunning||EditorApplication.timeSinceStartup>deadline)
        {if(a.IsRunning){a.enabled=false;a.enabled=true;}started=false;index++;next=EditorApplication.timeSinceStartup+.5;}
    }
    public static void Capture(string name,Vector3 target,Vector3 offset)
    {
        Directory.CreateDirectory(Output+"/screens");var source=Camera.main;var camera=new GameObject("QA product camera").AddComponent<Camera>();
        camera.CopyFrom(source);camera.enabled=false;camera.transform.position=target+offset;camera.transform.LookAt(target);camera.fieldOfView=38;camera.nearClipPlane=.03f;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var texture=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);camera.targetTexture=texture;var previous=RenderTexture.active;
        try
        {
            camera.Render();RenderTexture.active=texture;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Output+"/screens/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(camera.gameObject);}
    }
}
