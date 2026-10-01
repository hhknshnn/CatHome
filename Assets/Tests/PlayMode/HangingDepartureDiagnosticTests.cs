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

public sealed class HangingDepartureDiagnosticTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture; CatActivity activity;
    readonly List<string> rows=new List<string>();
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Hidden).Invoke(target,args);
    static T Read<T>(object target,string name)=>(T)target.GetType().GetField(name,Hidden).GetValue(target);
    [SetUp] public void Before(){fixture=new PreparedInteractionStartTests();fixture.Before();}
    [UnityTearDown] public IEnumerator After()
    {
        if(activity!=null&&activity.IsRunning)activity.CancelForTransition();
        File.WriteAllLines("Docs/QA/INTERACTION_POLISH_110MIN_2026-09-17/hanging-departure.tsv",rows);
        fixture?.After(); yield return RoomPlayModeSupport.WaitForPendingContactData();
    }
    [UnityTest,Timeout(45000)] public IEnumerator ActualStandUp_ReportsFreshDepartureRejections()
    {
        yield return (IEnumerator)Call(fixture,"Prepare","Balcony_Level01");
        var cat=Read<CatMovement>(fixture,"cat");
        activity=CatActivity.Registered.Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId=="balcony.hanging-chair");
        object[] args={activity,default(CatActivityStart),null};
        Assert.That((bool)Call(fixture,"FindReadyPose",args),Is.True,(string)args[2]);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        var animation=cat.GetComponent<CatActivityAnimation>();float deadline=Time.realtimeSinceStartup+25;bool captured=false;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();
            if(activity.IsWaitingForRestStop&&activity.RestingSeconds>=.2f)activity.RequestRestStop();
            if(animation.CurrentPose==CatActivityPose.StandUp&&!animation.IsNativeJump&&animation.NativeJumpPhase>=.90f){captured=true;break;}
        }
        Assert.That(captured,Is.True,"Actual final StandUp not reached");activity.StopAllCoroutines();
        Assert.That(activity.TryGetPreparedStart(cat,out var accepted),Is.True);
        var mount=Read<Transform>(activity,"doorPoint");Vector3 from=cat.transform.position,to=mount.position;to.y=accepted.Position.y;
        Vector3 outward=to-from;outward.y=0;float distance=outward.magnitude;outward.Normalize();
        var motion=cat.GetComponent<CatMeasuredSupportMotion>();
        foreach(float angle in new[]{0f,-15f,15f,-30f,30f,-45f,45f,-60f,60f,-75f,75f,-90f,90f})
        for(int extension=0;extension<=6;extension++)
        {
            Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*outward;
            Vector3 candidate=from+direction*(distance+extension*.06f);candidate.y=to.y;Quaternion heading=Quaternion.LookRotation(direction);
            bool poseClear=cat.IsInteractionPoseClear(candidate,heading);
            bool clear=CatJumpClearanceResolver.EndsClear(cat,from,candidate,heading,false,false,out var reject);
            var attempts=new List<string>();for(int i=0;i<motion.NumericPlanAttemptCount;i++){var a=motion.NumericPlanAttemptAt(i);attempts.Add(FormattableString.Invariant($"{a.diagnosticCode};leg={a.leg};v={a.sourceVertex};depth={a.depth};lift={a.lift};tilt={a.tiltDegrees};reach={a.reachMargin};blocker={(a.blocker!=null?a.blocker.name:"")}"));}
            rows.Add(FormattableString.Invariant($"angle={angle}\textension={extension}\tposeClear={poseClear}\tclear={clear}\tphase={reject.phase}\tfrom={from}\tto={candidate}\tlift={motion.VisualLift}\tplan={motion.NumericPlanReason}\traw={motion.NumericPlanRawDepth}\twitness={motion.NumericPlanRawWitness}\tattempts={string.Join("|",attempts)}"));
            if(!clear&&reject.phase<=CatJumpMotion.Takeoff&&extension==0)AppendSkinNormals(motion,angle,reject.phase);
            if(!clear&&reject.phase<=CatJumpMotion.Takeoff)break;
        }
        var acceptedDirection=accepted.Position-from;acceptedDirection.y=0;
        bool acceptedClear=CatJumpClearanceResolver.EndsClear(cat,from,accepted.Position,Quaternion.LookRotation(acceptedDirection),false,false,out var acceptedReject);
        rows.Add(FormattableString.Invariant($"actual-entry={accepted.Position}\tclear={acceptedClear}\tphase={acceptedReject.phase}"));
        Assert.That(rows.Count,Is.GreaterThan(13));
        TestContext.WriteLine("Diagnostic only: stopped at StandUp; no activity completion claim.");
    }
    void AppendSkinNormals(CatMeasuredSupportMotion motion,float angle,float phase)
    {
        var source=Read<CatSupportedLimbSkin>(motion,"preparationSource");
        if(source==null)return;
        var profile=Read<CatCareSkinCatalog.Profile>(source,"source");
        for(int attempt=0;attempt<motion.NumericPlanAttemptCount;attempt++)
        {
            var a=motion.NumericPlanAttemptAt(attempt);
            if(a.sourceVertex<0)continue;
            int slot=source.Slot(a.sourceVertex);if(slot<0)continue;
            var weights=new List<string>();float limbWeight=0;
            for(int at=profile.starts[slot];at<profile.starts[slot+1];at++)
            {
                string path=profile.bonePaths[profile.bones[at]];
                weights.Add(path.Substring(path.LastIndexOf('/')+1)+":"+profile.weights[at].ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                if(path.Contains("/DEF-upper_arm.L")||path.Contains("/DEF-upper_arm.R")||
                    path.Contains("/DEF-thigh.L")||path.Contains("/DEF-thigh.R"))limbWeight+=profile.weights[at];
            }
            // This request concerns genuine body witnesses that no leg IK can
            // move. Mixed-weight limb witnesses remain in the main trace.
            if(limbWeight>0)continue;
            foreach(var mesh in activity.GetComponentsInChildren<MeshCollider>())
            {
                if(mesh==null||mesh.sharedMesh==null||mesh.isTrigger||!mesh.enabled||!mesh.bounds.Contains(a.worldPoint))continue;
                object[] insideArgs={mesh,a.worldPoint,0f,null,default(Vector3)};
                bool inside=(bool)Call(fixture,"InsideMesh",insideArgs);
                int votes=Read<int>(fixture,"exactLastVotes");
                bool measured=CatMeshContactSurface.TryMetric(mesh.sharedMesh,mesh.transform,a.worldPoint,out float distance,out Vector3 normal);
                Vector3 localExit=measured?normal*(distance+.002f):Vector3.zero;
                Vector3 horizontal=Vector3.ProjectOnPlane(localExit,Vector3.up);
                rows.Add(FormattableString.Invariant($"body-normal\tangle={angle}\tphase={phase}\tattempt={attempt}\tvertex={a.sourceVertex}\tlift={a.lift}\ttilt={a.tiltDegrees}\treason={a.diagnosticCode}\tpoint={a.worldPoint.ToString("R")}\tinside={inside}\tvotes={votes}\tindependentDepth={insideArgs[2]}\tmetric={measured}\tdistance={distance}\tnormal={normal.ToString("R")}\tnormalY={normal.y}\tlocalExit={localExit.ToString("R")}\thorizontalLength={horizontal.magnitude}\tweights={string.Join("|",weights)}\towner={mesh.name}"));
            }
        }
    }
}
#endif
