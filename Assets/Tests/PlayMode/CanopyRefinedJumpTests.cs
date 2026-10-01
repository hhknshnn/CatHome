#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class CanopyRefinedJumpTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    GameObject obstacle;
    readonly List<string> rows=new List<string>();
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Private).Invoke(o,args);
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,Private).GetValue(o);
    [TearDown]public void After()
    {
        if(obstacle!=null)Object.DestroyImmediate(obstacle);
        fixture?.After();
        string folder=UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
        Directory.CreateDirectory(folder);File.WriteAllLines(Path.Combine(folder,"canopy-refined-jump.csv"),rows);
    }
    [UnityTest,Timeout(120000)]public IEnumerator MeasuredClearCanopyStance_RejectsRealObstacle_ThenCompletesUnchangedSourceCycle()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();
        yield return (IEnumerator)Call(fixture,"Prepare","Bedroom_Level01");
        var cat=Read<CatMovement>(fixture,"cat");
        var activity=CatActivity.Registered.OfType<CanopyNapActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BedroomStarCanopyId);
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();Assert.That(tag.BreedId,Is.EqualTo("oriental-shorthair"));
        var source=CatJumpClearanceCatalog.Load().Find(tag.BreedId);
        Assert.That(source.samples.All(s=>s.headCoverageVersion==CatJumpClearanceCatalog.HeadCoverageVersion&&s.headProbes.Length>0),Is.True);
        var position=new Vector3(-.28f,.05f,1.46f);var heading=Quaternion.identity;
        Assert.That(cat.IsInteractionPoseClear(position,heading),Is.True);
        Call(fixture,"Place",position,heading);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        Assert.That(CatJumpClearanceResolver.EndsClear(cat,position,activity.NestPoint.position,heading,true,true,out var rejected),Is.True,"Measured source gate rejected at "+rejected.phase);
        Assert.That(activity.TryGetStartPose(cat,out var accepted),Is.True);
        Assert.That(activity.TryGetPromptDistance(cat,out _),Is.True);
        // Obstruction at a real source head vertex capsule, well inside a thick
        // solid box. The split must not accept genuine occupied head volume.
        var sample=source.samples.Single(s=>Mathf.Abs(s.phase-CatJumpMotion.Touchdown)<.000001f);
        Matrix4x4 relative=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        Vector3 zero=relative.MultiplyPoint3x4(source.sourceZeroCentre);
        Vector3 landing=activity.NestPoint.position;
        Matrix4x4 rootMatrix=Matrix4x4.TRS(landing,heading,cat.transform.lossyScale),matrix=rootMatrix*relative;
        Vector3 correction=rootMatrix.MultiplyVector(new Vector3(-zero.x,0,-zero.z))+Vector3.up*(landing.y-matrix.m13+.008f);
        var part=sample.headProbes[0];
        obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.name="QA real head blocker";
        obstacle.transform.position=matrix.MultiplyPoint3x4((part.start+part.end)*.5f)+correction;
        obstacle.transform.localScale=Vector3.one*.16f;Physics.SyncTransforms();
        var bones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        var localPositions=bones.Select(b=>b.localPosition).ToArray();var localRotations=bones.Select(b=>b.localRotation).ToArray();
        Assert.That(CatJumpClearanceResolver.EndsClear(cat,position,landing,heading,true,true,out _),Is.False,"A genuine new solid must remain a rejection.");
        for(int i=0;i<bones.Length;i++){Assert.That(bones[i].localPosition,Is.EqualTo(localPositions[i]));Assert.That(bones[i].localRotation,Is.EqualTo(localRotations[i]));}
        Assert.That(cat.transform.position,Is.EqualTo(position));Assert.That(cat.transform.rotation,Is.EqualTo(heading));
        Object.DestroyImmediate(obstacle);obstacle=null;Physics.SyncTransforms();
        Assert.That(activity.TryGetStartPose(cat,out accepted),Is.True);RoomPlayModeSupport.ProvisionNeeds();
        var animator=cat.GetComponentInChildren<Animator>();var traceBones=(Transform[])Call(fixture,"FullTraceBones");
        var trace=new JumpContinuityTests.Trace{room="bedroom",product=activity.StoreProductId,breed=tag.BreedId};
        Action<CatActivity> completed=a=>{if(a==activity)trace.completed++;};CatActivity.Completed+=completed;
        int native=0,after=0,frame=0;float depth=0,shift=0,turn=0,started=Time.time;
        string detail="";bool floor=false,control=false,stopped=false;
        rows.Add("complete,nativeFrames,maxExactSkinDepth,initialShift,initialYaw,floorClear,control,detail");
        try
        {
            Assert.That(activity.TryStart(cat),Is.True);
            var animation=cat.GetComponent<CatActivityAnimation>();Assert.That(animation,Is.Not.Null);
            float deadline=Time.realtimeSinceStartup+40;
            while((activity.IsRunning||after<15)&&Time.realtimeSinceStartup<deadline)
            {
                yield return new WaitForEndOfFrame();frame++;
                if(!activity.IsRunning)after++;
                if(animation.IsNativeJump)
                {native++;Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name.EndsWith("|Jump",StringComparison.Ordinal)),Is.True);}
                if(Time.time-started<=.30f)
                {shift=Mathf.Max(shift,Vector3.Distance(cat.transform.position,accepted.Position));turn=Mathf.Max(turn,Quaternion.Angle(cat.transform.rotation,accepted.Rotation));}
                trace.frames.Add((JumpContinuityTests.Frame)Call(fixture,"SampleFullTrace",activity,animation,animator,traceBones,Time.time-started));
                if(frame%2==0)
                {float measured=(float)Call(fixture,"ActualSkinDepth");if(measured>depth){depth=measured;detail=fixture.DeepestExactSkinDetail;}}
                if(!stopped&&activity.IsWaitingForRestStop&&activity.RestingSeconds>=1.2f)
                {stopped=true;Assert.That(activity.RequestRestStop(),Is.True);}
            }
            Assert.That(activity.IsRunning,Is.False);Assert.That(trace.completed,Is.EqualTo(1));
            Assert.That(native,Is.GreaterThan(10));Assert.That(shift,Is.LessThan(.001f));Assert.That(turn,Is.LessThan(.2f));
            Assert.That(depth,Is.LessThanOrEqualTo(.025f),"Exact rendered skin; no fabric/soft exemption: "+detail);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            floor=(bool)Call(fixture,"ControllerAndBodyClear",cat.transform.position,cat.transform.rotation);
            control=cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked;
            Assert.That(floor&&control,Is.True);trace.controllerFloorClear=floor;
            typeof(JumpContinuityTests).GetMethod("Verify",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{trace});
        }
        finally
        {
            CatActivity.Completed-=completed;if(activity.IsRunning)activity.CancelForTransition();
            rows.Add(FormattableString.Invariant($"{trace.completed},{native},{depth:R},{shift:R},{turn:R},{floor},{control},\"{(detail??"").Replace("\"","'")}\""));
        }
    }
}
#endif
