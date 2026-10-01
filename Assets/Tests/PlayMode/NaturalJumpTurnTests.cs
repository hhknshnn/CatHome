#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class NaturalJumpTurnTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    ActionReadyReviewTests review;
    CatMovement cat;
    MediaEncoder encoder;
    bool pauseDuring;
    [Serializable] sealed class Cycle { public string test,breed,room,id; public int fps,completed,turnFrames; public float totalYaw,seconds,raw,corrected,minSigned,maxSigned; public bool paused; }
    readonly List<string> trace=new List<string>();
    string Output=>SessionState.GetString("CatHome.QA.ResultDirectory","Temp");
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    [SetUp] public void Before(){home=new CareAlignmentPolishTests();home.Before();review=new ActionReadyReviewTests();Set(review,"home",home);trace.Clear();}
    [TearDown] public void After(){encoder?.Dispose();encoder=null;home.After();File.WriteAllLines(Path.Combine(Output,TestContext.CurrentContext.Test.Name+"-trace.csv"),trace);}
    IEnumerator Boot(){yield return (IEnumerator)Call(home,"Home");cat=Object.FindAnyObjectByType<CatMovement>();}
    IEnumerator Run(CatActivity activity,bool video,bool requireTurn=true)
    {
        var row=new ActionReadyReviewTests.Row{id=activity.ActivityId};
        yield return (IEnumerator)Call(review,"FindStance",activity,row);
        Assert.That(row.ready,Is.True,row.error);
        var button=Call(review,"Select",activity);
        int completed=0,frames=0,turnFrames=0;float duration=0,maxDuration=0,totalYaw=0,raw=0,corrected=0,minSigned=0,maxSigned=0;bool paused=false;
        var animation=cat.GetComponent<CatActivityAnimation>();
        Action<CatActivity> onComplete=a=>{if(a==activity)completed++;};CatActivity.Completed+=onComplete;
        if(video)encoder=new MediaEncoder(Path.Combine(Output,SessionState.GetString("CatHome.QA.JumpVideo","CatHome_Ziplama_Donusu.mp4")),new VideoTrackAttributes{frameRate=new MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        Quaternion previous=cat.transform.rotation;Vector3 position=cat.transform.position;
        float deadline=Time.realtimeSinceStartup+90;
        try
        {
            Call(review,"Tap",button);Assert.That(activity.IsRunning,Is.True);animation=cat.GetComponent<CatActivityAnimation>();
            while((activity.IsRunning||video&&frames<216)&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();
                float yaw=Quaternion.Angle(previous,cat.transform.rotation);
                bool turning=animation.IsSupportedPivot&&yaw>.1f;
                if(turning)
                {
                    turnFrames++;duration+=Time.deltaTime;totalYaw+=yaw;
                    Assert.That(Vector3.Distance(position,cat.transform.position),Is.LessThan(.0002f),"supported pivot root drift");
                    Assert.That(animation.IsNativeJump&&animation.NativeJumpPhase<.99f,Is.False,"turn in flight");
                    Assert.That(yaw,Is.LessThan(900f*Time.deltaTime+.2f),"instant snap");
                    Assert.That(animation.CurrentPose,Is.EqualTo(CatActivityPose.Walk));
                    var overlay=cat.GetComponent<CatNaturalTurnMotion>();raw+=overlay.LastRawContactTravel;corrected+=overlay.LastCorrectedContactTravel;
                    float signed=Vector3.SignedAngle(previous*Vector3.forward,cat.transform.forward,Vector3.up);minSigned=Mathf.Min(minSigned,signed);maxSigned=Mathf.Max(maxSigned,signed);
                    if(pauseDuring&&!paused)
                    {
                        paused=true;Time.timeScale=0;
                        var bones=new[]{"DEF-spine","DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"}.Select(n=>CatBreedVisualFactory.FindDescendant(cat.transform,n)).ToArray();
                        var points=bones.Select(b=>b.position).ToArray();var rotation=cat.transform.rotation;
                        yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();
                        Assert.That(Quaternion.Angle(rotation,cat.transform.rotation),Is.LessThan(.001f));
                        for(int i=0;i<bones.Length;i++)Assert.That(Vector3.Distance(points[i],bones[i].position),Is.LessThan(.001f),"paused supported pivot");
                        Time.timeScale=1;
                    }
                }
                else {maxDuration=Mathf.Max(maxDuration,duration);duration=0;}
                trace.Add(string.Join(",",CatBreedService.SelectedBreedId,activity.ActivityId,Time.captureFramerate,frames,cat.transform.position.ToString("F4"),cat.transform.eulerAngles.y,animation.CurrentPose,animation.IsNativeJump,yaw));
                previous=cat.transform.rotation;position=cat.transform.position;
                if(video&&frames<216)
                {
                    var texture=ScreenCapture.CaptureScreenshotAsTexture();
                    try{Assert.That(encoder.AddFrame(texture),Is.True);if(frames%3==0){var folder=Path.Combine(Output,Path.GetFileNameWithoutExtension(SessionState.GetString("CatHome.QA.JumpVideo","CatHome_Ziplama_Donusu.mp4"))+"-frames");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,frames.ToString("D3")+".png"),texture.EncodeToPNG());}}
                    finally{Object.Destroy(texture);}
                }
                frames++;
                if(activity.IsWaitingForRestStop&&activity.RestingSeconds>=.20f)Call(review,"Tap",Call(review,"Select",activity));
            }
        }
        finally{CatActivity.Completed-=onComplete;encoder?.Dispose();encoder=null;}
        Assert.That(activity.IsRunning,Is.False,activity.ActivityId);
        Assert.That(completed,Is.EqualTo(1),activity.ActivityId+" / "+CatJumpLimbClearance.LastRejection);
        Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
        if(requireTurn){Assert.That(turnFrames,Is.GreaterThan(0));Assert.That(totalYaw,Is.GreaterThan(40));}
        Assert.That(Mathf.Max(maxDuration,duration),Is.LessThan(.65f));
        var old=cat.GetComponent<CatSurfaceTurnMotion>();Assert.That(old==null||!old.IsTurning,Is.True);
        if(activity is LivingFurnitureActivity table&&table.Kind==CatActivityKind.CoffeeTablePlay){Assert.That(table.DidPush,Is.True);Assert.That(table.ContactDistance,Is.LessThan(.10f));}
        File.AppendAllText(Path.Combine(Output,"cycles.jsonl"),JsonUtility.ToJson(new Cycle{test=TestContext.CurrentContext.Test.Name,breed=CatBreedService.SelectedBreedId,room=activity.gameObject.scene.name,id=activity.ActivityId,fps=Time.captureFramerate,completed=completed,turnFrames=turnFrames,totalYaw=totalYaw,seconds=Mathf.Max(maxDuration,duration),raw=raw,corrected=corrected,minSigned=minSigned,maxSigned=maxSigned,paused=paused})+"\n");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
    }
    [UnityTest] public IEnumerator FinalChairGameView()
    {
        yield return Boot();yield return (IEnumerator)Call(home,"Breed","persian");yield return QaBreedReadiness.WaitForSelected(cat,"persian");
        Assert.That(HomeStoreService.TrySetStored("room.armchair",false),Is.True);yield return null;yield return null;
        Time.captureFramerate=24;yield return Run(CatActivity.Registered.Single(a=>a.StoreProductId=="room.armchair"&&a.isActiveAndEnabled),true);
    }
    [UnityTest] public IEnumerator SupportedPivot_PauseAndResume()
    {
        yield return Boot();Assert.That(HomeStoreService.TrySetStored("room.armchair",false),Is.True);yield return null;yield return null;
        pauseDuring=true;Time.captureFramerate=30;yield return Run(CatActivity.Registered.Single(a=>a.StoreProductId=="room.armchair"&&a.isActiveAndEnabled),false);
    }
    [UnityTest,Timeout(420000)] public IEnumerator EightRooms_ReadyJumpingSurfaces_Complete()
    {
        yield return Boot();int complete=0;Time.captureFramerate=30;
        foreach(var room in HomeRoomService.Rooms)
        {
            yield return (IEnumerator)Call(home,"Room",room.Id);cat=Object.FindAnyObjectByType<CatMovement>();yield return QaBreedReadiness.WaitForSelected(cat,CatBreedService.SelectedBreedId);
            foreach(var a in CatActivity.Registered.Where(a=>a.gameObject.scene==cat.gameObject.scene&&FurnitureBodyClearanceTests.IsJumpActivity(a)).ToArray())
            {
                if(!string.IsNullOrEmpty(a.StoreProductId))HomeStoreService.TrySetStored(a.StoreProductId,false);
                yield return null;yield return null;
                var row=new ActionReadyReviewTests.Row{id=a.ActivityId};yield return (IEnumerator)Call(review,"FindStance",a,row);
                if(!row.ready){File.AppendAllText(Path.Combine(Output,"unavailable.txt"),room.Id+"/"+a.ActivityId+"\n");continue;}
                yield return Run(a,false,false);complete++;
            }
        }
        Assert.That(complete,Is.GreaterThanOrEqualTo(24));
    }
    [UnityTest,Timeout(240000)] public IEnumerator LivingJumpSurfaces_TwoBreedsTwoRates_Complete()
    {
        yield return Boot();Assert.That(HomeStoreService.TrySetStored("room.armchair",false),Is.True);yield return null;yield return null;
        foreach(string breed in new[]{"domestic-shorthair","persian"})
        {
            yield return (IEnumerator)Call(home,"Breed",breed);yield return QaBreedReadiness.WaitForSelected(cat,breed);
            foreach(int fps in new[]{24,60})
            foreach(var a in CatActivity.Registered.Where(a=>a.isActiveAndEnabled&&(a is LivingFurnitureActivity||a.StoreProductId=="room.armchair")).ToArray())
            {Time.captureFramerate=fps;yield return Run(a,false);}
        }
    }
}
#endif



