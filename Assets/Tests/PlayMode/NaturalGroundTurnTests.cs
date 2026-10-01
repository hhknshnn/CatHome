#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class NaturalGroundTurnTests
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    CatMovement cat;
    MobileJoystick joystick;
    MediaEncoder encoder;
    readonly List<string> rows = new List<string>();
    string Output => SessionState.GetString("CatHome.QA.ResultDirectory", "Temp");
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
    [SetUp] public void Before() { home=new CareAlignmentPolishTests();home.Before();rows.Clear();rows.Add("breed,fps,angle,seconds,maxFrameYaw,rawContactSum,correctedContactSum,samples"); }
    [TearDown] public void After()
    {
        encoder?.Dispose(); encoder=null;
        if(joystick!=null)Input(Vector2.zero);
        home.After();File.WriteAllLines(Path.Combine(Output,TestContext.CurrentContext.Test.Name+"-turns.csv"),rows);
    }
    void Input(Vector2 value)=>typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick,value);
    IEnumerator Boot()
    {
        yield return (IEnumerator)Call(home,"Home");
        cat=Object.FindAnyObjectByType<CatMovement>();joystick=Object.FindAnyObjectByType<MobileJoystick>();
        Set(cat,"cameraTransform",null);
    }
    IEnumerator Place(float yaw=0)
    {
        Input(Vector2.zero);Call(home,"Place",new Vector3(-.9f,.05f,-1.4f),Quaternion.Euler(0,yaw,0));
        Set(cat,"turnVelocity",0f);
        for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();
    }
    [UnityTest] public IEnumerator BothSides_45_90_180_TwoBreeds_ThreeRates()
    {
        yield return Boot();
        foreach(string breed in new[]{"domestic-shorthair","persian"})
        {
            yield return (IEnumerator)Call(home,"Breed",breed);
            yield return QaBreedReadiness.WaitForSelected(cat,breed);
            foreach(int fps in new[]{24,30,60})
            foreach(float angle in new[]{45f,-45f,90f,-90f,179.9f,-179.9f})
            {
                Time.captureFramerate=fps;yield return Place();
                var target=Quaternion.Euler(0,angle,0);
                var d=target*Vector3.forward;Input(new Vector2(d.x,d.z)*.55f);
                float seconds=0,maxYaw=0,raw=0,corrected=0;int samples=0;
                Quaternion last=cat.transform.rotation;
                while(Quaternion.Angle(cat.transform.rotation,target)>.8f && seconds<1.25f)
                {
                    yield return new WaitForEndOfFrame();seconds+=Time.deltaTime;
                    maxYaw=Mathf.Max(maxYaw,Quaternion.Angle(last,cat.transform.rotation));last=cat.transform.rotation;
                    var turn=cat.GetComponent<CatNaturalTurnMotion>();
                    if(turn.LastRawContactTravel>.001f){raw+=turn.LastRawContactTravel;corrected+=turn.LastCorrectedContactTravel;samples++;}
                    Assert.That(cat.IsBodyPoseClear(cat.transform.position,cat.transform.rotation),Is.True,"body guard");
                }
                Assert.That(Quaternion.Angle(cat.transform.rotation,target),Is.LessThan(1f),breed+" angle "+angle);
                Assert.That(seconds,Is.LessThan(1f),"short continuous turn");
                Assert.That(maxYaw,Is.LessThanOrEqualTo(480f/fps+.2f),"no instant snap");
                Assert.That(cat.GetComponentInChildren<Animator>().applyRootMotion,Is.False);
                if(samples>2)Assert.That(corrected,Is.LessThan(raw*.85f),"source-contact drift must decrease");
                rows.Add(string.Join(",",breed,fps,angle.ToString(CultureInfo.InvariantCulture),seconds.ToString(CultureInfo.InvariantCulture),maxYaw.ToString(CultureInfo.InvariantCulture),raw.ToString(CultureInfo.InvariantCulture),corrected.ToString(CultureInfo.InvariantCulture),samples));
                Input(Vector2.zero);
            }
        }
    }
    [UnityTest] public IEnumerator RealBallInteraction_TurnsAndCompletesWithActualPawContacts()
    {
        yield return Boot();
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.BallBasketId,false),Is.True);
        yield return null;yield return null;
        var ball=CatActivity.Registered.OfType<BallChaseActivity>().Single(a=>a.StoreProductId==HomeStoreService.BallBasketId && a.isActiveAndEnabled);
        var review=new ActionReadyReviewTests();Set(review,"home",home);
        var row=new ActionReadyReviewTests.Row{id=HomeStoreService.BallBasketId};
        yield return (IEnumerator)Call(review,"FindStance",ball,row);
        Assert.That(row.ready,Is.True,row.error);
        var button=Call(review,"Select",ball);Call(review,"Tap",button);
        Assert.That(ball.IsRunning,Is.True);
        float deadline=Time.realtimeSinceStartup+35f,totalYaw=0;int turnFrames=0;
        Quaternion previous=cat.transform.rotation;
        while(ball.IsRunning && Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();
            float yaw=Quaternion.Angle(previous,cat.transform.rotation);previous=cat.transform.rotation;
            totalYaw+=yaw;
            if(yaw>.1f && cat.GetComponent<CatActivityAnimation>().CurrentPose==CatActivityPose.Walk)turnFrames++;
        }
        Assert.That(ball.IsRunning,Is.False,ball.InterruptedReason);
        Assert.That(ball.CatchCount,Is.EqualTo(ball.CatchGoal),ball.InterruptedReason);
        Assert.That(ball.LastHitDistance,Is.LessThan(.095f));
        Assert.That(totalYaw,Is.GreaterThan(30f));Assert.That(turnFrames,Is.GreaterThan(0));
        Assert.That(cat.IsMovementLocked,Is.False);
        File.WriteAllText(Path.Combine(Output,"real-interaction.json"),"{\"catchCount\":"+ball.CatchCount+",\"turnFrames\":"+turnFrames+",\"yaw\":"+totalYaw.ToString(CultureInfo.InvariantCulture)+"}");
    }
    [UnityTest] public IEnumerator WalkingSteering_AndInteractionFacing_ReleaseImmediately()
    {
        yield return Boot();yield return Place();Time.captureFramerate=60;
        Input(Vector2.up*.55f);for(int i=0;i<18;i++)yield return new WaitForEndOfFrame();
        Input(new Vector2(.4f,.4f));
        for(int i=0;i<24;i++)
        {
            Vector3 before=cat.transform.position;yield return new WaitForEndOfFrame();
            Vector3 travel=cat.transform.position-before;travel.y=0;
            Assert.That(travel.magnitude,Is.GreaterThan(.001f),"small steering stays in locomotion");
            File.AppendAllText(Path.Combine(Output,"steer-trace.txt"),"frame="+i+" before="+before.ToString("F5")+" after="+cat.transform.position.ToString("F5")+" travel="+travel.ToString("F5")+" forward="+cat.transform.forward.ToString("F5")+" flags="+cat.GetComponent<CharacterController>().collisionFlags+"\n");
            Assert.That(Vector3.Dot(travel.normalized,cat.transform.forward),Is.GreaterThan(.98f),"no sideways root sliding");
        }
        Input(Vector2.zero);yield return Place();
        var owner=new GameObject("Turn test owner");cat.SetMovementLocked(owner,true);
        Vector3 origin=cat.transform.position;float started=Time.time;
        yield return CatActivityMotion.TurnForStep(cat,Quaternion.Euler(0,180,0));
        Assert.That(Time.time-started,Is.LessThan(.7f));
        Assert.That(Vector3.Distance(origin,cat.transform.position),Is.LessThan(.002f),"interaction pivot must not translate");
        Assert.That(Quaternion.Angle(cat.transform.rotation,Quaternion.Euler(0,180,0)),Is.LessThan(.1f));
        cat.SetMovementLocked(owner,false);Object.Destroy(owner);
        Input(Vector2.down*.55f);yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        Assert.That(cat.GroundSpeed,Is.GreaterThan(.1f),"no post-turn wait");Input(Vector2.zero);
    }
    [UnityTest] public IEnumerator FinalEightSecondGameView()
    {
        yield return Boot();yield return (IEnumerator)Call(home,"Breed","domestic-shorthair");
        Time.captureFramerate=24;yield return Place(0);
        encoder=new MediaEncoder(Path.Combine(Output,"CatHome_Dogal_Donus.mp4"),new VideoTrackAttributes{frameRate=new MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        for(int frame=0;frame<192;frame++)
        {
            // Continuous shot: right 90, reverse 180, moving left/right arcs.
            Vector2 input=frame<6?Vector2.zero:frame<40?Vector2.right*.4f:frame<77?Vector2.left*.4f:
                frame<109?Vector2.up*.4f:frame<145?Vector2.down*.4f:frame<177?Vector2.right*.4f:Vector2.zero;
            Input(input);yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try{Assert.That(encoder.AddFrame(texture),Is.True);if(frame%4==0) {Directory.CreateDirectory(Path.Combine(Output,"frames"));File.WriteAllBytes(Path.Combine(Output,"frames",frame.ToString("D3")+".png"),texture.EncodeToPNG());}}
            finally{Object.Destroy(texture);}
        }
        encoder.Dispose();encoder=null;Input(Vector2.zero);
    }
}
#endif
