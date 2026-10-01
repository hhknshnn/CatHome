using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

// Diagnosis only. No rejected gesture runs, and no production cache or target is changed.
public sealed class FlowerEndpointPatchDiagnosticTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    const BindingFlags Static=BindingFlags.NonPublic|BindingFlags.Static;
    FlowerSurfaceFailureDiagnosticTests fixture;
    static string N(float v)=>v.ToString("F7",System.Globalization.CultureInfo.InvariantCulture);
    static string V(Vector3 v)=>N(v.x)+";"+N(v.y)+";"+N(v.z);
    static Vector3 Mean(Vector3[] points,int[] indices){Vector3 p=default;foreach(int i in indices)p+=points[i];return p/indices.Length;}
    static int[] Patch(Vector3[] points,CatPawReachCatalog.PawVertex[] defs,Vector3 normal)
    {
        int tip=Enumerable.Range(0,points.Length).Where(i=>defs[i].distal).OrderBy(i=>Vector3.Dot(points[i],normal)).First();
        return Enumerable.Range(0,points.Length).Where(i=>defs[i].distal&&(points[i]-points[tip]).sqrMagnitude<=.025f*.025f)
            .OrderBy(i=>(points[i]-points[tip]).sqrMagnitude).Take(6).ToArray();
    }
    [SetUp] public void Before(){fixture=new FlowerSurfaceFailureDiagnosticTests();fixture.Before();}
    [TearDown] public void After()=>fixture.After();

    [UnityTest,Timeout(90000)] public IEnumerator KnownContact_SeparatesRotatedPatchSkinFromConservativeCapsule()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");
        CatMovement cat=Object.FindAnyObjectByType<CatMovement>();
        float deadline=Time.realtimeSinceStartup+15;
        while(cat.GetComponentInChildren<CatBreedVisualTag>()?.BreedId!="domestic-shorthair"&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(cat.GetComponentInChildren<CatBreedVisualTag>().BreedId,Is.EqualTo("domestic-shorthair"));
        CatPawReachCatalog.Entry entry=null;deadline=Time.realtimeSinceStartup+15;
        while(Time.realtimeSinceStartup<deadline)
        {entry=CatPawReachCatalog.LoadSurface("domestic-shorthair",CatActivityPose.Scratch,out bool loading);if(!loading)break;yield return null;}
        Assert.That(entry,Is.Not.Null);cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
        cat.transform.SetPositionAndRotation(new Vector3(-3.03f,.05f,1.17f),Quaternion.Euler(0,190,0));cc.enabled=true;Physics.SyncTransforms();
        var targets=((Vector3[])typeof(SitLookActivity).GetField("visibleLookTargets",Fields).GetValue(activity)).Select(activity.transform.TransformPoint);
        var target=targets.OrderBy(p=>(p-new Vector3(-3.43f,.55f,1.17f)).sqrMagnitude).First();
        Assert.That(CatPawReachResolver.TryMeasureSurface(activity.transform,target,out var hit),Is.True);
        var mesh=hit.collider as MeshCollider;Assert.That(mesh,Is.Not.Null);
        bool left=Vector3.Dot(hit.point-cat.transform.position,cat.transform.right)<=0;
        var sample=entry.samples.Single(s=>Mathf.Abs(s.phase-.32f)<.00001f);
        var arm=left?sample.left:sample.right;var defs=left?entry.leftPaw:entry.rightPaw;var local=left?sample.leftPaw:sample.rightPaw;
        var visual=cat.GetComponentInChildren<CatBreedVisualTag>().transform;
        Quaternion bend=Quaternion.AngleAxis(20,Vector3.up);Vector3 pivot=CatPawReachCatalog.WorldPoint(visual,sample.torsoPivot);
        Vector3 Map(Vector3 p)=>pivot+bend*(CatPawReachCatalog.WorldPoint(visual,p)-pivot);
        var source=local.Select(Map).ToArray();Vector3 upper=Map(arm.upper),fore=Map(arm.fore),hand=Map(arm.hand);
        Vector3 normal=hit.normal;if(Vector3.Dot(normal,hand-hit.point)<0)normal=-normal;
        var patch=Patch(source,defs,normal);Assert.That(patch.Length,Is.GreaterThanOrEqualTo(3));
        var guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Fields).GetValue(cat);
        var solid=typeof(CatBodyGuard).GetMethod("Solid",Fields);var penetration=typeof(CatBodyGuard).GetMethod("Penetration",Fields);
        var fit=typeof(CatPawReachResolver).GetMethod("FitPaw",Static);
        var metric=new QaExactMeshContact(new QaMeshTopologyCache());var witness=new FlowerContactPolishTests();
        var inside=typeof(FlowerContactPolishTests).GetMethod("PawWitnessInside",Fields);
        var regions=CatPawSurfaceRegions.Build(defs,left?entry.leftPawTriangles:entry.rightPawTriangles);Assert.That(regions.Length,Is.InRange(1,32));
        Assert.That(regions.Sum(r=>r.Length),Is.EqualTo(defs.Length));
        Assert.That(regions.SelectMany(r=>r).Distinct().Count(),Is.EqualTo(defs.Length));
        var rows=new List<string>{"refinement,patchVertices,endpointError,endpointMeshDistance,strictCapsuleClear,capsuleMTD,maxPredictedSkinInside,insideVertexCount,deepestVertex,deepestPoint,minSignedPlane,leadingVertex,maxPatchSignedPlane,patchSpanAlongNormal,target,normal,solvedPatch,contactPhase,pitch,yaw,completeSkinScan,regionCount,strictRegionsClear,regionMTD,maxUncoveredVertex"};
        var timer=System.Diagnostics.Stopwatch.StartNew();var slice=System.Diagnostics.Stopwatch.StartNew();
        var seen=new HashSet<string>();
        for(int iteration=0;iteration<4;iteration++)
        {
            string patchKey=string.Join(";",patch.OrderBy(i=>i));if(!seen.Add(patchKey))break;
            var state=CatPawSurfaceCcd.Solve(upper,fore,hand,Mean(source,patch),hit.point,1);
            var points=source.Select(p=>CatPawSurfaceCcd.TransformPoint(state,hand,p)).ToArray();
            var probe=(CatBodyGuardCatalog.Probe)fit.Invoke(null,new object[]{points,left?"left-paw":"right-paw"});
            bool clear=cat.IsInteractionBodyClear(new[]{probe},.002f);float capsuleDepth=0;
            foreach(var c in Physics.OverlapCapsule(probe.start,probe.end,probe.radius,~0,QueryTriggerInteraction.Ignore))
            {
                if(!(bool)solid.Invoke(guard,new object[]{c}))continue;
                object[] args={probe.start,probe.end,probe.radius,c,Vector3.zero,0f};
                if((bool)penetration.Invoke(guard,args))capsuleDepth=Mathf.Max(capsuleDepth,(float)args[5]);
            }
            var regionProbes=new CatBodyGuardCatalog.Probe[regions.Length];float regionDepth=0,uncovered=0;
            for(int r=0;r<regions.Length;r++)
            {
                var part=regionProbes[r]=CatPawSurfaceRegions.Fit(points,regions[r],left?"left-paw":"right-paw");
                Vector3 segment=part.end-part.start;
                foreach(int v in regions[r])
                {
                    float t=segment.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(points[v]-part.start,segment)/segment.sqrMagnitude):0;
                    uncovered=Mathf.Max(uncovered,Vector3.Distance(points[v],part.start+segment*t)-part.radius);
                }
                foreach(var c in Physics.OverlapCapsule(part.start,part.end,part.radius,~0,QueryTriggerInteraction.Ignore))
                {
                    if(!(bool)solid.Invoke(guard,new object[]{c}))continue;
                    object[] args={part.start,part.end,part.radius,c,Vector3.zero,0f};
                    if((bool)penetration.Invoke(guard,args))regionDepth=Mathf.Max(regionDepth,(float)args[5]);
                }
            }
            Assert.That(uncovered,Is.LessThanOrEqualTo(.000001f),"Every measured skin vertex stays enclosed");
            bool regionsClear=cat.IsInteractionBodyClear(regionProbes,.002f);
            float skinDepth=0;int insideCount=0,deepest=-1;Vector3 deepestPoint=default;bool complete=true;
            for(int v=0;v<points.Length;v++)
            {
                if(timer.Elapsed.TotalSeconds>8){complete=false;break;}
                if(slice.Elapsed.TotalMilliseconds>3){yield return null;slice.Restart();}
                if(!mesh.bounds.Contains(points[v]))continue;
                object[] args={mesh,points[v],sample.phase,left?"left":"right",defs[v].vertexIndex,new List<string>(),0};
                inside.Invoke(witness,args);if((int)args[6]<4)continue;insideCount++;
                float d=metric.Measure(mesh.sharedMesh,mesh.transform,points[v]).distance;
                if(d>skinDepth){skinDepth=d;deepest=defs[v].vertexIndex;deepestPoint=points[v];}
            }
            int leading=Enumerable.Range(0,points.Length).OrderBy(i=>Vector3.Dot(points[i]-hit.point,normal)).First();
            float min=Vector3.Dot(points[leading]-hit.point,normal),patchMax=patch.Max(i=>Vector3.Dot(points[i]-hit.point,normal)),patchMin=patch.Min(i=>Vector3.Dot(points[i]-hit.point,normal));
            rows.Add(string.Join(",",iteration,string.Join(";",patch.Select(i=>defs[i].vertexIndex)),N(Vector3.Distance(state.Endpoint,hit.point)),N(metric.Measure(mesh.sharedMesh,mesh.transform,state.Endpoint).distance),clear,N(capsuleDepth),N(skinDepth),insideCount,deepest,V(deepestPoint),N(min),defs[leading].vertexIndex,N(patchMax),N(patchMax-patchMin),V(hit.point),V(normal),V(state.Endpoint),N(sample.phase),0,20,complete,regions.Length,regionsClear,N(regionDepth),N(uncovered)));
            System.IO.File.WriteAllLines(Root+"/flower-contact-patch-refinement.csv",rows);
            if(!complete)break;patch=Patch(points,defs,normal);yield return null;
        }
        System.IO.File.WriteAllText(Root+"/flower-contact-patch-refinement-duration.txt",FormattableString.Invariant($"candidates={rows.Count-1};seconds={timer.Elapsed.TotalSeconds:F3}\n"));
        Assert.That(rows.Count,Is.GreaterThan(1));
        // Passing this test means the diagnostic completed, not that a gesture is safe.
    }
}
