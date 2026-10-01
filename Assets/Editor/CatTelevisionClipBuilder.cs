using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Bakes the real in-game cats to HD frames for a low-cost looping TV clip.</summary>
public static class CatTelevisionClipBuilder
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static TitleCatShowcase showcase;
    static int frame;
    public static string Status {get;private set;}="Idle";
    public static void Begin()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Bake in Edit Mode.");
        showcase=UnityEngine.Object.FindAnyObjectByType<TitleCatShowcase>(FindObjectsInactive.Include);
        if(showcase==null)throw new InvalidOperationException("Title showcase missing from the home UI.");
        Directory.CreateDirectory("Library/CatTelevisionFrames");frame=0;
        typeof(TitleCatShowcase).GetMethod("CreateStage",Flags).Invoke(showcase,null);
        Status="Capturing";EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        try
        {
            typeof(TitleCatShowcase).GetMethod("TickActors",Flags).Invoke(showcase,new object[]{1f/24f,false});
            typeof(TitleCatShowcase).GetMethod("RenderFrame",Flags).Invoke(showcase,null);
            var output=showcase.Output;var previous=RenderTexture.active;RenderTexture.active=output;
            var still=new Texture2D(output.width,output.height,TextureFormat.RGB24,false);
            try{still.ReadPixels(new Rect(0,0,output.width,output.height),0,0);still.Apply();File.WriteAllBytes("Library/CatTelevisionFrames/"+frame.ToString("D4")+".png",still.EncodeToPNG());}
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(still);}
            frame++;Status="Capturing "+frame+" / 240";
            if(frame>=240){Status="Complete";Stop();}
        }
        catch(Exception e){Status="Failed: "+e.Message;Stop();Debug.LogException(e);}
    }
    static void Stop()
    {
        EditorApplication.update-=Tick;
        if(showcase!=null)typeof(TitleCatShowcase).GetMethod("Cleanup",Flags).Invoke(showcase,null);
        showcase=null;
    }
}
