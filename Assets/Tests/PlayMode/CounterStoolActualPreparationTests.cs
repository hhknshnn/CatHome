#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

// QA measurement only. A naturally accepted cycle is stopped at its actual
// last StandUp, then the original departure pivot/pose/helper are evaluated at its authored heading and +/-30 degrees in three independent accepted routines.
// This diagnostic never supplies permission to production GroundLanding.
public sealed class CounterStoolActualPreparationTests
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly Vector3[] Directions={Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back};
    PreparedInteractionStartTests fixture;
    CatMovement cat;
    CatActivity activity;
    Mesh bake;
    QaExactMeshContact metric;
    readonly QaMeshTopologyCache topology=new QaMeshTopologyCache();
    readonly List<string> rows=new List<string>();
    float deadline;
    float headingOffset;
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Hidden).Invoke(target,args);
    static T Read<T>(object target,string name)=>(T)target.GetType().GetField(name,Hidden).GetValue(target);
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
    static string Csv(params object[] values)=>string.Join(",",values.Select(v=>"\""+(v is Vector3 p?FormattableString.Invariant($"{p.x:R};{p.y:R};{p.z:R}"):
        v is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):v?.ToString()??"").Replace("\"","\"\"")+"\""));
    [SetUp]public void Before()
    {
        rows.Clear();
        fixture=new PreparedInteractionStartTests();fixture.Before();bake=new Mesh();metric=new QaExactMeshContact(topology);
        rows.Add("activity,frame,phase,variant,rawEndsClear,rejectPhase,heading,root,startOffset,carriedLift,phaseVisualLift,requiredLift,reachLimited,legResidual,skinDepth,deepestVertex,deepestPoint,obstacle,witness140Depth,insideVertices,boneLengthDelta,rootDelta,yawDelta,solveMs,combinedPredictionError,predictionReady,plantedLegs,commonLiftCalls,commonLiftReason,commonLiftResidual,commonLiftAllowedByReach,commonLiftSourceLimit,commonLiftRejectLeg,commonLiftCandidateHeights,commonLiftCandidateDepths,commonLiftSceneBlocker");
    }
    [UnityTearDown]public IEnumerator After()
    {
        if(activity!=null&&activity.IsRunning)activity.CancelForTransition();
        Directory.CreateDirectory(Output);File.WriteAllLines(Path.Combine(Output,"counter-stool-supported-preparation-" + headingOffset.ToString(CultureInfo.InvariantCulture) + ".csv"),rows);
        if(bake!=null)Object.DestroyImmediate(bake);metric?.Clear();topology.Clear();fixture?.After();
        yield return RoomPlayModeSupport.WaitForPendingContactData();
    }
    [UnityTest,Timeout(40000)]
    public IEnumerator CounterStool_Preparation_AuthoredHeading() => Measure(0f);
    [UnityTest,Timeout(40000)]
    public IEnumerator CounterStool_Preparation_MinusThirtyHeading() => Measure(-30f);
    [UnityTest,Timeout(40000)]
    public IEnumerator CounterStool_Preparation_PlusThirtyHeading() => Measure(30f);
    IEnumerator Measure(float angle)
    {
        headingOffset=angle;
        deadline=Time.realtimeSinceStartup+30f;
        yield return (IEnumerator)Call(fixture,"Prepare","Kitchen_Level01");
        cat=Read<CatMovement>(fixture,"cat");
        var stool=CatActivity.Registered.OfType<PerchNapActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId=="kitchen.counter-stool");activity=stool;
        object[] args={activity,default(CatActivityStart),null};Assert.That((bool)Call(fixture,"FindReadyPose",args),Is.True,(string)args[2]);
        yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat),Is.True);var animation=cat.GetComponent<CatActivityAnimation>();
        bool captured=false;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();
            if(activity.IsWaitingForRestStop&&activity.RestingSeconds>=.2f)activity.RequestRestStop();
            if(animation.CurrentPose!=CatActivityPose.StandUp||animation.IsNativeJump||animation.NativeJumpPhase<.94f)continue;
            captured=true;break;
        }
        Assert.That(captured,Is.True,"No actual final supported StandUp before 30-second diagnostic deadline.");
        // The accepted setup is now a controlled QA observation, not a claimed
        // completed activity. Keep the same owner/locks/support solver alive.
        activity.StopAllCoroutines();
        Assert.That(activity.TryGetPreparedStart(cat,out var accepted),Is.True);
        Vector3 from=cat.transform.position,to=stool.FloorPoint.position;to.y=accepted.Position.y;
        Vector3 departure=Vector3.ProjectOnPlane(to-from,Vector3.up);
        departure=Quaternion.AngleAxis(headingOffset,Vector3.up)*departure;
        to=from+departure;to.y=accepted.Position.y;
        Quaternion heading=Quaternion.LookRotation(departure);
        yield return (IEnumerator)typeof(CatJumpMotion).GetMethod("TurnDirectly",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{cat,from,heading});
        yield return new WaitForEndOfFrame();
        Assert.That(cat.transform.position,Is.EqualTo(from));Assert.That(Quaternion.Angle(cat.transform.rotation,heading),Is.LessThan(.001f));
        var motion=cat.GetComponent<CatMeasuredSupportMotion>();Assert.That(motion!=null&&motion.IsActive,Is.True);
        bool rawClear=CatJumpClearanceResolver.EndsClear(cat,from,to,heading,false,false,out var rejected);
        animation.TryReadNativeJumpStartOffset(heading,out var predictedOffset);
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();var breed=CatBreedCatalog.Load().Find(tag.BreedId);
        var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);string clipPath=UnityEditor.AssetDatabase.GetAssetPath(entry.sourceClip);
        var sourceHash=UnityEditor.AssetDatabase.GetAssetDependencyHash(clipPath);
        var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();var animator=cat.GetComponentInChildren<Animator>();
        var combined=CatSupportedLimbSkin.Bind(CatCareSkinCatalog.Load().Find(tag.BreedId),animator,skin);Assert.That(combined,Is.Not.Null);
        var poses=new CatSupportedLimbSkin.Pose[4];
        // Measure every real scene mesh, including the adjacent kitchen island.
        // An owner-only check would hide the original occupied-island endpoint.
        var solids=Object.FindObjectsByType<MeshCollider>().Where(c=>
            c.gameObject.scene==cat.gameObject.scene&&c.enabled&&!c.isTrigger&&c.sharedMesh!=null&&
            c.GetComponentInParent<CatMovement>()==null).ToArray();
        Assert.That(solids.Length,Is.GreaterThan(0));
        animation.BeginNativeJump(false);float carried=Read<float>(animation,"jumpSupportLift");
        Assert.That(Vector3.Distance(animation.NativeJumpStartOffset,predictedOffset),Is.LessThan(.0001f),"Predicted horizontal centering differs from actual BeginNativeJump.");
        bool oldBackfaces=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        try
        {
            foreach(float phase in new[]{.1875f,.2083333f,.23f})
            {
                Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Bounded preparation diagnostic exceeded 30 seconds.");
                motion.Restore();animation.SetNativeJumpSample(CatActivityPose.TowelJumpDown,phase,0f);
                Call(animation,"LateUpdate");var source=World(skin);var lengths=Lengths(animator.transform);
                Assert.That(combined.Capture(),Is.True);float predictionError=0;
                Record("raw-no-carried-lift",source.Select(p=>p-Vector3.up*carried).ToArray(),0f,0d);
                Record("actual-carried-lift",source,0f,0d);
                Call(motion,"LateUpdate");var corrected=World(skin);var actualLengths=Lengths(animator.transform);
                float lengthDelta=lengths.Zip(actualLengths,(a,b)=>Mathf.Abs(a-b)).Max();
                var legs=Read<Array>(motion,"legs");Vector3 translation=Read<Vector3>(motion,"up")*motion.VisualLift;
                for(int leg=0;leg<4;leg++)
                {
                    object state=legs.GetValue(leg);object Field(string name)=>state.GetType().GetField(name).GetValue(state);
                    if((bool)Field("solve"))Assert.That(combined.TryPose(leg,(Vector3)Field("target"),(Vector3)Field("desiredLower"),translation,out poses[leg]),Is.True);
                    else poses[leg]=combined.SourcePose(leg,translation);
                }
                for(int slot=0;slot<combined.VertexCount;slot++)
                    predictionError=Mathf.Max(predictionError,Vector3.Distance(combined.CombinedPoint(slot,poses,translation),corrected[combined.SourceIndex(slot)]));
                Assert.That(predictionError,Is.LessThan(.0001f),"Combined four-limb source prediction differs from actual same-frame skin");
                Record("actual-supported-frame",corrected,lengthDelta,motion.LastSolveMs);
                Assert.That(lengthDelta,Is.LessThan(.0001f),"Same-frame source limb lengths changed.");
                Assert.That(Vector3.Distance(cat.transform.position,from),Is.LessThan(.000001f));
                Assert.That(Quaternion.Angle(cat.transform.rotation,heading),Is.LessThan(.001f));
                void Record(string variant,Vector3[] points,float lengthChange,double solveMs)
                {
                    float maximum=0,witness=0;int deepest=-1,inside=0;Vector3 deepestPoint=default;string colliderName="";
                    int sampled=0;
                    foreach(int index in breed.ContactVertexIndices)
                    {
                        if((sampled++&127)==0)Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),"Bounded actual-skin measurement exceeded30seconds.");
                        float depth=0;string collider="";
                        foreach(var solid in solids){float candidate=InsideDepth(solid,points[index]);if(candidate>depth){depth=candidate;collider=solid.name;}}
                        if(index==140)witness=depth;if(depth<=0)continue;inside++;
                        if(depth>maximum){maximum=depth;deepest=index;deepestPoint=points[index];colliderName=collider;}
                    }
                    rows.Add(Csv(activity.StoreProductId,Time.frameCount,phase,variant,rawClear,rejected.phase,heading.eulerAngles.y,from,predictedOffset,carried,
                        variant=="actual-supported-frame"?motion.VisualLift:0,variant=="actual-supported-frame"?motion.RequiredLift:0,
                        variant=="actual-supported-frame"&&motion.ReachLimited,variant=="actual-supported-frame"?motion.MaximumLegResidual:0,
                        maximum,deepest,deepestPoint,colliderName,witness,inside,lengthChange,Vector3.Distance(cat.transform.position,from),Quaternion.Angle(cat.transform.rotation,heading),solveMs,predictionError,
                        variant=="actual-supported-frame"&&Read<bool>(motion,"predictionReady"),
                        variant=="actual-supported-frame"?motion.PlantedLegCount:0,
                        variant=="actual-supported-frame"?motion.CommonLiftCalls:0,
                        variant=="actual-supported-frame"?motion.CommonLiftReason:"",
                        variant=="actual-supported-frame"?motion.CommonLiftResidual:0,
                        variant=="actual-supported-frame"?motion.CommonLiftAllowedByReach:0,
                        variant=="actual-supported-frame"?motion.CommonLiftSourceLimit:0,
                        variant=="actual-supported-frame"?motion.CommonLiftRejectLeg:-1,
                        variant=="actual-supported-frame"?string.Join(";",Read<float[]>(motion,"commonLiftHeights").Take(motion.CommonLiftRecordedCandidates).Select(v=>v.ToString("R",CultureInfo.InvariantCulture))):"",
                        variant=="actual-supported-frame"?string.Join(";",Read<float[]>(motion,"commonLiftDepths").Take(motion.CommonLiftRecordedCandidates).Select(v=>v.ToString("R",CultureInfo.InvariantCulture))):"",
                        variant=="actual-supported-frame"&&motion.CommonLiftBlocker!=null?motion.CommonLiftBlocker.name:""));
                }
            }
        }
        finally{Physics.queriesHitBackfaces=oldBackfaces;activity.CancelForTransition();}
        Assert.That(UnityEditor.AssetDatabase.GetAssetDependencyHash(clipPath),Is.EqualTo(sourceHash));
        Assert.That(rows.Count,Is.EqualTo(10));
        TestContext.WriteLine("Measurement only: the accepted cycle was deliberately stopped at StandUp. Inspect raw/carried/supported stool rows; this is not a completed-stool-cycle pass.");
    }
    Vector3[] World(SkinnedMeshRenderer skin){skin.BakeMesh(bake,true);return bake.vertices.Select(skin.transform.TransformPoint).ToArray();}
    static float[] Lengths(Transform root)
    {
        var result=new float[8];for(int i=0;i<4;i++)
        {string side=(i&1)==0?"L":"R";var upper=CatBreedVisualFactory.FindDescendant(root,(i<2?"DEF-upper_arm.":"DEF-thigh.")+side);
            var lower=CatBreedVisualFactory.FindDescendant(root,(i<2?"DEF-forearm.":"DEF-shin.")+side);var foot=CatBreedVisualFactory.FindDescendant(root,(i<2?"DEF-hand.":"DEF-foot.")+side);
            result[i*2]=Vector3.Distance(upper.position,lower.position);result[i*2+1]=Vector3.Distance(lower.position,foot.position);}
        return result;
    }
    float InsideDepth(MeshCollider collider,Vector3 point)
    {
        if(!collider.bounds.Contains(point))return 0;var shape=topology.Get(collider.sharedMesh);int votes=0;
        float reflection=collider.transform.localToWorldMatrix.determinant<0?-1f:1f;
        foreach(var direction in Directions)
        {
            if(!collider.Raycast(new Ray(point,direction),out var hit,3))continue;int at=hit.triangleIndex*3;
            if(at<0||at+2>=shape.triangles.Length)continue;
            Vector3 a=collider.transform.TransformPoint(shape.vertices[shape.triangles[at]]),b=collider.transform.TransformPoint(shape.vertices[shape.triangles[at+1]]),c=collider.transform.TransformPoint(shape.vertices[shape.triangles[at+2]]);
            if(Vector3.Dot(Vector3.Cross(b-a,c-a)*reflection,direction)>0)votes++;
        }
        return votes>=4?metric.Measure(collider.sharedMesh,collider.transform,point).distance:0f;
    }
}
#endif
