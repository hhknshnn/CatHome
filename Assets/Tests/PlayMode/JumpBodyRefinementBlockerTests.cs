#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class JumpBodyRefinementBlockerTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;GameObject obstacle;
    readonly List<string> rows=new List<string>();
    readonly List<string> timings=new List<string>{"stage,clear,milliseconds,managedBytes,refinedRegions,refinedTriangles,cachedRegions"};
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Private).Invoke(o,a);
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,Private).GetValue(o);
    [UnityTearDown]public IEnumerator After()
    {
        if(obstacle!=null)Object.DestroyImmediate(obstacle);fixture?.After();
        string root=UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/INTERACTION_POLISH_2026-09-16");
        Directory.CreateDirectory(root);File.WriteAllLines(Path.Combine(root,"jump-body-solid-blockers.csv"),rows);
        File.WriteAllLines(Path.Combine(root,"jump-weighted-query-cost.csv"),timings);
        yield return RoomPlayModeSupport.WaitForPendingContactData();
    }
    [UnityTest,Timeout(70000)]public IEnumerator FourSourceRegions_RejectRealPrimitiveAndClosedMeshBlockers_WithoutMutatingPose()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();yield return (IEnumerator)Call(fixture,"Prepare","Bedroom_Level01");
        var cat=Read<CatMovement>(fixture,"cat");
        var activity=CatActivity.Registered.OfType<CanopyNapActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BedroomStarCanopyId);
        Vector3 start=new Vector3(-.28f,.05f,1.46f);Quaternion heading=Quaternion.identity;
        Call(fixture,"Place",start,heading);yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();
        Assert.That(cat.IsInteractionPoseClear(start,heading),Is.True);
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();var entry=CatJumpClearanceCatalog.Load().Find(tag.BreedId);
        Assert.That(entry.samples.All(s=>s.bodyCoverageVersion==CatJumpClearanceCatalog.BodyCoverageVersion&&s.bodyCoverage.Length==4),Is.True);
        Assert.That(TimedClear(cat,start,activity.NestPoint.position,heading,"first-query"),Is.True);
        Assert.That(entry.weightedBodyVersion,Is.EqualTo(CatJumpClearanceCatalog.WeightedBodyVersion));
        Assert.That(entry.samples.All(s=>CatJumpWeightedBody.HasData(entry,s)),Is.True);
        var sample=entry.samples.Single(s=>Mathf.Abs(s.phase-CatJumpMotion.Touchdown)<.000001f);
        Matrix4x4 relative=cat.transform.worldToLocalMatrix*tag.transform.localToWorldMatrix;
        Vector3 zero=relative.MultiplyPoint3x4(entry.sourceZeroCentre),landing=activity.NestPoint.position;
        Matrix4x4 rootMatrix=Matrix4x4.TRS(landing,heading,cat.transform.lossyScale),matrix=rootMatrix*relative;
        Vector3 correction=rootMatrix.MultiplyVector(new Vector3(-zero.x,0,-zero.z))+Vector3.up*(landing.y-matrix.m13+.008f);
        var bones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        rows.Add("region,shape,rejected,sourcePhase,rootShift,yawChange");
        foreach(var region in entry.bodySurface)foreach(int shape in new[]{0,1,2})
        {
            Assert.That(TimedClear(cat,start,landing,heading,"warm-"+region.region+"-"+shape),Is.True,"Baseline before "+region.region);
            var localPositions=bones.Select(t=>t.localPosition).ToArray();var localRotations=bones.Select(t=>t.localRotation).ToArray();var localScales=bones.Select(t=>t.localScale).ToArray();
            var sourceFrame=CatPawWeightedSkin.Build(entry.bodyBones,sample.bodySkinMatrices,matrix,region.skin,correction);
            Assert.That(sourceFrame,Is.Not.Null);var actual=new Vector3[region.skin.Length];
            CatPawWeightedSkin.Fill(sourceFrame,Vector3.zero,Quaternion.identity,false,true,default,default,default,actual);
            obstacle=GameObject.CreatePrimitive(shape==1?PrimitiveType.Sphere:PrimitiveType.Cube);obstacle.name="QA body source blocker "+region.region;
            SceneManager.MoveGameObjectToScene(obstacle,cat.gameObject.scene);
            obstacle.transform.position=actual[region.triangles[0]]; // a measured source-skin point, never an empty capsule centre
            obstacle.transform.localScale=Vector3.one*.24f;
            if(shape==2)
            {
                Object.DestroyImmediate(obstacle.GetComponent<BoxCollider>());
                var collider=obstacle.AddComponent<MeshCollider>();collider.sharedMesh=obstacle.GetComponent<MeshFilter>().sharedMesh;collider.convex=false;
            }
            Physics.SyncTransforms();
            bool rejected=!CatJumpClearanceResolver.EndsClear(cat,start,landing,heading,true,true,out var rejection);
            rows.Add(FormattableString.Invariant($"{region.region},{(shape==2?"ClosedMesh":shape==1?"Sphere":"Box")},{rejected},{rejection.phase:R},{Vector3.Distance(cat.transform.position,start):R},{Quaternion.Angle(cat.transform.rotation,heading):R}"));
            Assert.That(rejected,Is.True,"Real occupied source volume must reject "+region.region+"/"+(shape==2?"mesh":shape==1?"sphere":"box"));
            Assert.That(cat.transform.position,Is.EqualTo(start));Assert.That(cat.transform.rotation,Is.EqualTo(heading));
            for(int i=0;i<bones.Length;i++){Assert.That(bones[i].localPosition,Is.EqualTo(localPositions[i]));Assert.That(bones[i].localRotation,Is.EqualTo(localRotations[i]));Assert.That(bones[i].localScale,Is.EqualTo(localScales[i]));}
            Vector3 blockedPosition=obstacle.transform.position;
            obstacle.transform.position+=Vector3.up*4;Physics.SyncTransforms();
            Assert.That(CatJumpClearanceResolver.EndsClear(cat,start,landing,heading,true,true,out _),Is.True,"Moved blocker releases warmed geometry immediately");
            obstacle.transform.position=blockedPosition;Physics.SyncTransforms();
            Assert.That(CatJumpClearanceResolver.EndsClear(cat,start,landing,heading,true,true,out _),Is.False,"Same warmed source rejects a moved-back blocker");
            Object.DestroyImmediate(obstacle);obstacle=null;Physics.SyncTransforms();
            Assert.That(CatJumpClearanceResolver.EndsClear(cat,start,landing,heading,true,true,out _),Is.True,"Removed blocker must immediately release source gate");
            yield return null;
        }
    }
    bool TimedClear(CatMovement cat,Vector3 start,Vector3 landing,Quaternion heading,string stage)
    {
        int regions=CatJumpWeightedBody.RegionQueries,triangles=CatJumpWeightedBody.RefinedTriangles;
        long before=GC.GetAllocatedBytesForCurrentThread();long started=System.Diagnostics.Stopwatch.GetTimestamp();
        bool clear=CatJumpClearanceResolver.EndsClear(cat,start,landing,heading,true,true,out _);
        double milliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000d/System.Diagnostics.Stopwatch.Frequency;
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        timings.Add(FormattableString.Invariant($"{stage},{clear},{milliseconds:R},{allocated},{CatJumpWeightedBody.RegionQueries-regions},{CatJumpWeightedBody.RefinedTriangles-triangles},{CatJumpWeightedBody.CachedRegions}"));
        if(stage.StartsWith("warm-",StringComparison.Ordinal))
        {
            Assert.That(milliseconds,Is.LessThan(50d),"A warm clearance query must not restore the former 1.7-second all-region path");
            Assert.That(allocated,Is.EqualTo(0),"Warm exact-region fallback allocates managed memory");
        }
        return clear;
    }
}
#endif
