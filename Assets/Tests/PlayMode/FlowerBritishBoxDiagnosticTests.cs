using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

// Bounded measurement only: no rejected plan is played and no gate is bypassed.
public sealed class FlowerBritishBoxDiagnosticTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
    FlowerContactPolishTests fixture;
    BoxCollider box;
    static object Get(object o,string name)=>o.GetType().GetField(name,F).GetValue(o);
    static MethodInfo Method(string name)=>typeof(CatPawReachResolver).GetMethod(name,S);
    static string N(float value)=>value.ToString("F7",System.Globalization.CultureInfo.InvariantCulture);
    [SetUp] public void Before(){fixture=new FlowerContactPolishTests();fixture.Before();}
    [TearDown] public void After(){if(box!=null)Object.DestroyImmediate(box.gameObject);fixture.After();}
    [UnityTest,Timeout(90000)] public IEnumerator BritishKnownStance_ReportsStrictBoxAndSourceRejections() => Measure("british-shorthair");
    [UnityTest,Timeout(90000)] public IEnumerator DomesticKnownStance_ReportsCompleteArmRejections() => Measure("domestic-shorthair");
    [UnityTest,Timeout(90000)] public IEnumerator LonghairKnownStance_ReportsCompleteArmRejections() => Measure("domestic-longhair");
    IEnumerator Measure(string breed)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
        var cat=Object.FindAnyObjectByType<CatMovement>();
        yield return QaBreedReadiness.WaitForSelected(cat,breed);
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
        cat.transform.SetPositionAndRotation(new Vector3(-3.03f,.05f,1.17f),Quaternion.Euler(0,190,0));cc.enabled=true;Physics.SyncTransforms();
        var targets=((Vector3[])typeof(SitLookActivity).GetField("visibleLookTargets",F).GetValue(activity)).Select(activity.transform.TransformPoint);
        var target=targets.OrderBy(p=>(p-new Vector3(-3.43f,.55f,1.17f)).sqrMagnitude).First();
        Assert.That(CatPawReachResolver.TryMeasureSurface(activity.transform,target,out var surface),Is.True);
        bool left=Vector3.Dot(surface.point-cat.transform.position,cat.transform.right)<=0;
        var progress=new List<string>{"frame,seconds,ready,pending,math,physics,cursor,scan,candidates"};
        var timer=System.Diagnostics.Stopwatch.StartNew();object geometry=null,request=null;bool ready=false;
        for(int frame=0;frame<1200&&timer.Elapsed.TotalSeconds<20;frame++)
        {
            yield return null;
            ready=CatPawReachResolver.TryResolveSurface(cat,surface,left,CatActivityPose.Scratch,out _,32,35);
            var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",S).GetValue(null);
            geometry=cache.Values.Cast<object>().FirstOrDefault();
            if(geometry!=null)request=((IDictionary)Get(geometry,"requests")).Values.Cast<object>().FirstOrDefault();
            progress.Add(string.Join(",",frame,N((float)timer.Elapsed.TotalSeconds),ready,CatPawReachResolver.SurfaceQueryPending,
                CatPawReachResolver.SurfaceMathCandidatesLastQuery,CatPawReachResolver.SurfacePhysicsCandidatesLastQuery,
                request!=null?Get(request,"cursor"):0,request!=null?Get(request,"scanIndex"):0,
                request!=null?((IList)Get(request,"candidates")).Count:0));
            if(frame%30==0)System.IO.File.WriteAllLines(Root+"/flower-arm-"+breed+"-progress.csv",progress);
            if(ready||!CatPawReachResolver.SurfaceQueryPending)break;
        }
        System.IO.File.WriteAllLines(Root+"/flower-arm-"+breed+"-progress.csv",progress);
        Assert.That(geometry,Is.Not.Null);Assert.That(request,Is.Not.Null);
        var entry=(CatPawReachCatalog.Entry)Get(geometry,"entry");
        bool fixedClear=(bool)Method("FixedTrajectoryClear").Invoke(null,new[]{cat,Get(geometry,"body")});
        var guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",F).GetValue(cat);
        var solid=typeof(CatBodyGuard).GetMethod("Solid",F);
        var holder=new GameObject("QA remote British measured box");holder.transform.position=new Vector3(0,-15000,0);
        box=holder.AddComponent<BoxCollider>();box.isTrigger=true;
        var rows=new List<string>{"candidate,contact,pitch,yaw,lift,fixedClear,upperClear,endpointClear,reachable,strictWholePath,maximumBoxDepth,phase,blocker,readySearch"};
        var candidates=(IList)Get(request,"candidates");
        int stride=((int[][])Get(geometry,"leftRegions")).Length+((int[][])Get(geometry,"rightRegions")).Length;
        var probes=new CatBodyGuardBox[entry.samples.Length*stride];var endpoint=new CatBodyGuardBox[stride];
        var slice=System.Diagnostics.Stopwatch.StartNew();var detail=System.Diagnostics.Stopwatch.StartNew();
        var skinRows=new List<string>{"candidate,phase,actualPredictedDepth,insideVertices,vertex,side,fullScan"};
        var metric=new QaExactMeshContact(new QaMeshTopologyCache());var witness=new FlowerContactPolishTests();
        var inside=typeof(FlowerContactPolishTests).GetMethod("PawWitnessInside",F);
        for(int c=0;c<candidates.Count&&c<240&&detail.Elapsed.TotalSeconds<18;c++)
        {
            var candidate=candidates[c];var plan=(CatPawReachPlan)Get(candidate,"plan");
            var upper=(CatBodyGuardCatalog.Probe[])Method("SurfaceUpper").Invoke(null,new[]{geometry,candidate,cat.transform.right});
            bool upperClear=cat.IsInteractionBodyClear(upper),reachable=true,endpointClear=false;
            int contactIndex=Array.FindIndex(entry.samples,s=>Mathf.Abs(s.phase-plan.SourcePhase)<.00001f);
            if((bool)Method("FillSurfacePawRegions").Invoke(null,new object[]{geometry,plan,cat.transform.right,endpoint,contactIndex,0}))
                endpointClear=cat.IsInteractionBoxesClear(endpoint,.002f);
            float worst=0,phase=-1;string blocker="";
            for(int s=0;s<entry.samples.Length;s++)
            {
                if(slice.Elapsed.TotalMilliseconds>3){yield return null;slice.Restart();}
                if(!(bool)Method("FillSurfacePawRegions").Invoke(null,new object[]{geometry,plan,cat.transform.right,probes,s,s*stride}))
                {reachable=false;break;}
                for(int r=0;r<stride;r++)
                {
                    var p=probes[s*stride+r];box.size=p.halfExtents*2;
                    foreach(var hit in Physics.OverlapBox(p.centre,p.halfExtents,p.rotation,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(!(bool)solid.Invoke(guard,new object[]{hit}))continue;
                        if(Physics.ComputePenetration(box,p.centre,p.rotation,hit,hit.transform.position,hit.transform.rotation,out _,out float depth)&&depth>worst)
                        {worst=depth;phase=entry.samples[s].phase;blocker=hit.name;}
                    }
                }
            }
            rows.Add(string.Join(",",c,N(plan.SourcePhase),N(plan.ChestPitch),N(plan.ChestYaw),N(plan.Surface.ApproachLift),fixedClear,upperClear,
                endpointClear,reachable,reachable&&cat.IsInteractionBoxesClear(probes,.002f),N(worst),N(phase),blocker,ready));
            if((c==2||c==53||c==77)&&phase>=0&&surface.collider is MeshCollider mesh)
            {
                var sample=entry.samples.First(s=>Mathf.Abs(s.phase-phase)<.00001f);
                Assert.That(CatPawReachResolver.TryPredictSurfaceArmSample(cat,plan,sample,out var lp,out var rp),Is.True);
                float skinDepth=0;int count=0,vertex=-1;string side="";bool complete=true;
                for(int paw=0;paw<2;paw++)
                {
                    var points=paw==0?lp:rp;var defs=paw==0?entry.leftArmSkin:entry.rightArmSkin;
                    for(int v=0;v<points.Length;v++)
                    {
                        if(detail.Elapsed.TotalSeconds>=18){complete=false;break;}
                        if(slice.Elapsed.TotalMilliseconds>3){yield return null;slice.Restart();}
                        if(!mesh.bounds.Contains(points[v]))continue;
                        object[] args={mesh,points[v],phase,paw==0?"left":"right",defs[v].vertexIndex,new List<string>(),0};
                        inside.Invoke(witness,args);if((int)args[6]<4)continue;
                        count++;float d=metric.Measure(mesh.sharedMesh,mesh.transform,points[v]).distance;
                        if(d>skinDepth){skinDepth=d;vertex=defs[v].vertexIndex;side=paw==0?"left":"right";}
                    }
                }
                skinRows.Add(string.Join(",",c,N(phase),N(skinDepth),count,vertex,side,complete));
                System.IO.File.WriteAllLines(Root+"/flower-arm-"+breed+"-skin.csv",skinRows);
            }
            if(c%8==0)System.IO.File.WriteAllLines(Root+"/flower-arm-"+breed+"-candidates.csv",rows);
        }
        System.IO.File.WriteAllLines(Root+"/flower-arm-"+breed+"-candidates.csv",rows);
        Assert.That(rows.Count,Is.GreaterThan(1),"Diagnostic requires measured candidates; pass is not contact success.");
    }
}
