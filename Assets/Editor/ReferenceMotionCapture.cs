using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Records native frames at a fixed simulation step, for normal-speed visual QA.</summary>
public static class ReferenceMotionCapture
{
    public static string Status {get;private set;}="Idle";
    static int oldRate;static float oldScale;
    public static void Begin()
    {
        Begin(new[]{CatActivityKind.ScratchPost,CatActivityKind.TunnelPlay,CatActivityKind.CoffeeTablePlay,CatActivityKind.SofaLounge},"Library/ReferenceMotionFrames");
    }
    public static void Begin(CatActivityKind[] kinds,string outputDirectory)
    {
        if(!Application.isPlaying || !EditorQaSession.IsActive)throw new System.InvalidOperationException("Use isolated Play QA.");
        var cat=Object.FindAnyObjectByType<CatMovement>();
        if(cat==null)throw new System.InvalidOperationException("Missing live cat.");
        cat.StartCoroutine(Run(kinds,outputDirectory));
    }
    static IEnumerator Run(CatActivityKind[] kinds,string outputDirectory)
    {
        oldRate=Time.captureFramerate;oldScale=Time.timeScale;Time.captureFramerate=24;Time.timeScale=1;
        Status="Recording";
        try
        {
        var cat=Object.FindAnyObjectByType<CatMovement>();var controller=cat.GetComponent<CharacterController>();
        foreach(var kind in kinds)
        {
            var activity=CatActivity.Registered.First(a=>a.Kind==kind);
            Object.FindAnyObjectByType<EnergySystem>().ApplySavedValue(75);
            controller.enabled=false;cat.transform.SetPositionAndRotation(activity.RoutineEntryPoint.position,Quaternion.identity);controller.enabled=true;
            yield return null;
            string folder=Path.Combine(outputDirectory,kind.ToString());Directory.CreateDirectory(folder);
            if(!activity.TryStart(cat))throw new System.InvalidOperationException("Could not start "+kind);
            int frame=0;
            while(activity.IsRunning && frame<24*20)
            {
                yield return new WaitForEndOfFrame();
                var texture=ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(folder+"/"+frame.ToString("D4")+".jpg",texture.EncodeToJPG(95));Object.Destroy(texture);
                frame++;Status=kind+" "+frame;
                if(frame>=24*12 && activity.IsWaitingForRestStop)activity.RequestRestStop();
            }
            float until=Time.time+3;
            while(cat.IsMovementPhysicallyLocked && Time.time<until)yield return null;
        }
        Status="Complete";
        }
        finally
        {
            Time.captureFramerate=oldRate;Time.timeScale=oldScale;
            if(Status!="Complete")Status="Interrupted: "+Status;
        }
    }
}
