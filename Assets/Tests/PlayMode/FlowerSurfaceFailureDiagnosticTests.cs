using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

// QA only: observes a refused plan. Never starts it, relaxes a gate, moves a
// target, or modifies production caches. Predicted skin is explicitly labelled.
public sealed class FlowerSurfaceFailureDiagnosticTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
    const BindingFlags Static=BindingFlags.NonPublic|BindingFlags.Static;
    HomeStoreSaveState saved;string breed;float capture;
    static object Field(object o,string n)=>o.GetType().GetField(n,Fields).GetValue(o);
    static MethodInfo Method(string name)=>typeof(CatPawReachResolver).GetMethod(name,Static);
    static string N(float v)=>v.ToString("F7",System.Globalization.CultureInfo.InvariantCulture);
    static string V(Vector3 v)=>N(v.x)+";"+N(v.y)+";"+N(v.z);
    static string Path(Transform t){string v=t.name;while(t.parent!=null){t=t.parent;v=t.name+"/"+v;}return "\""+v.Replace("\"","\"\"")+"\"";}
    [SetUp] public void Before(){Assert.That(EditorQaSession.IsActive,Is.True);saved=HomeStoreService.CaptureState();breed=CatBreedService.SelectedBreedId;capture=Time.captureDeltaTime;Time.captureFramerate=30;}
    [TearDown] public void After(){if(CatActivity.Active!=null)CatActivity.Active.CancelForTransition();RoomPlayModeSupport.ReleaseRoom();HomeStoreService.ApplySavedState(saved);CatBreedService.Select(breed);Time.timeScale=1;Time.captureDeltaTime=capture;}

    [UnityTest,Timeout(90000)] public IEnumerator KnownFlowerStance_ReportsRollingSearchAndRejectedRegions()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");yield return null;yield return null;
        var cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
        cat.transform.SetPositionAndRotation(new Vector3(-3.03f,.05f,1.17f),Quaternion.Euler(0,190,0));cc.enabled=true;Physics.SyncTransforms();
        var targets=((Vector3[])typeof(SitLookActivity).GetField("visibleLookTargets",Fields).GetValue(activity)).Select(activity.transform.TransformPoint).ToArray();
        Vector3 target=targets.OrderBy(p=>(p-new Vector3(-3.43f,.55f,1.17f)).sqrMagnitude).First();
        Assert.That(CatPawReachResolver.TryMeasureSurface(activity.transform,target,out var surface),Is.True);
        bool left=Vector3.Dot(surface.point-cat.transform.position,cat.transform.right)<=0;
        var rows=new List<string>{"frame,seconds,queryMs,ready,pending,math,physics,geometryBuild,geometryReady,requests,cursor,scanIndex,candidates,unreachable,pawIncomplete,actor,visual,target,activePaw"};
        var timer=System.Diagnostics.Stopwatch.StartNew();object geometry=null,request=null;bool ready=false;
        for(int frame=0;frame<600&&timer.Elapsed.TotalSeconds<25;frame++)
        {
            yield return null;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            ready=CatPawReachResolver.TryResolveSurface(cat,surface,left,CatActivityPose.Scratch,out _,32,35);watch.Stop();
            int math=CatPawReachResolver.SurfaceMathCandidatesLastQuery,physics=CatPawReachResolver.SurfacePhysicsCandidatesLastQuery;
            var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",Static).GetValue(null);
            geometry=cache.Values.Cast<object>().FirstOrDefault();int builds=0,requests=0,cursor=0,scan=0,count=0,unreachable=0,incomplete=0;bool built=false;
            if(geometry!=null)
            {
                builds=(int)Field(geometry,"buildCursor");built=(bool)Field(geometry,"ready");
                var dict=(IDictionary)Field(geometry,"requests");requests=dict.Count;request=dict.Values.Cast<object>().FirstOrDefault();
                if(request!=null)
                {
                    cursor=(int)Field(request,"cursor");scan=(int)Field(request,"scanIndex");var candidates=(IList)Field(request,"candidates");count=candidates.Count;
                    foreach(object candidate in candidates){if((bool)Field(candidate,"unreachable"))unreachable++;if(Field(candidate,"paws")==null)incomplete++;}
                }
            }
            rows.Add(FormattableString.Invariant($"{frame},{timer.Elapsed.TotalSeconds:F4},{watch.Elapsed.TotalMilliseconds:F4},{ready},{CatPawReachResolver.SurfaceQueryPending},{math},{physics},{builds},{built},{requests},{cursor},{scan},{count},{unreachable},{incomplete},{V(cat.transform.position)},{V(cat.GetComponentInChildren<CatBreedVisualTag>().transform.position)},{V(surface.point)},{(left?"left":"right")}"));
            if(frame%15==0||ready||!CatPawReachResolver.SurfaceQueryPending)System.IO.File.WriteAllLines(Root+"/flower-known-search-progress.csv",rows);
            if(ready||!CatPawReachResolver.SurfaceQueryPending)break;
        }
        System.IO.File.WriteAllLines(Root+"/flower-known-search-progress.csv",rows);
        Assert.That(geometry,Is.Not.Null);Assert.That(request,Is.Not.Null,"No candidate request was built within diagnostic limit");
        var entry=(CatPawReachCatalog.Entry)Field(geometry,"entry");
        bool fixedClear=(bool)Method("FixedTrajectoryClear").Invoke(null,new[]{cat,Field(geometry,"body")});
        // Preserve the historical whole-paw envelope comparison. These legacy
        // columns are not the production anatomical-region permission result.
        var details=new List<string>{"candidate,contactPhase,pitch,yaw,arcLift,fixedClear,upperClear,trajectoryReachable,firstUnreachablePhase,legacySingleCapsuleActiveClear,legacySingleCapsuleOtherClear,legacySingleCapsuleFirstBadPhase,legacySingleCapsuleFirstBadSide,legacySingleCapsuleCollider,legacySingleCapsuleMTD,predictedSkinExactDepth,predictedVertex,predictedPoint,insideVotes,sampledPhases,exactSkinMeasured,detailTruncated"};
        var candidatesNow=(IList)Field(request,"candidates");var selected=new List<object>();
        // First twelve plus one from each observed lift: bounded native detail,
        // while progress CSV reports the complete rolling candidate inventory.
        foreach(object c in candidatesNow){if(selected.Count<12)selected.Add(c);}
        var lifts=new HashSet<float>(selected.Select(c=>((CatPawReachPlan)Field(c,"plan")).Surface.ApproachLift));
        foreach(object c in candidatesNow)if(lifts.Add(((CatPawReachPlan)Field(c,"plan")).Surface.ApproachLift)&&selected.Count<18)selected.Add(c);
        var guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Fields).GetValue(cat);
        var penetrate=typeof(CatBodyGuard).GetMethod("Penetration",Fields);var solid=typeof(CatBodyGuard).GetMethod("Solid",Fields);
        var actualMetric=new QaExactMeshContact(new QaMeshTopologyCache());var witness=new FlowerContactPolishTests();
        var inside=typeof(FlowerContactPolishTests).GetMethod("PawWitnessInside",Fields);
        // Exact skin metrology is limited to three representative plans,
        // never all eighteen. Every source/paw loop yields after a 3ms slice;
        // the detail section stops after 12 seconds and labels partial rows.
        var detailTimer=System.Diagnostics.Stopwatch.StartNew();
        var slice=System.Diagnostics.Stopwatch.StartNew();int exactPlans=0;
        foreach(object candidate in selected)
        {
            if(detailTimer.Elapsed.TotalSeconds>=12)break;
            var plan=(CatPawReachPlan)Field(candidate,"plan");
            bool measureExact=exactPlans<3,exactMeasured=false,truncated=false;int sampledPhases=0;
            var upper=(CatBodyGuardCatalog.Probe[])Method("SurfaceUpper").Invoke(null,new[]{geometry,candidate,cat.transform.right});
            bool upperClear=cat.IsInteractionBodyClear(upper),reachable=true,activeClear=true,otherClear=true;
            float unreachablePhase=-1,badPhase=-1,depth=0,skinDepth=0;string badSide="",colliderPath="";int vertex=-1,votes=0;Vector3 point=default;
            var paws=new CatBodyGuardCatalog.Probe[entry.samples.Length*2];
            for(int s=0;s<entry.samples.Length;s++)
            {
                if(detailTimer.Elapsed.TotalSeconds>=12){truncated=true;break;}
                if(slice.Elapsed.TotalMilliseconds>=3){yield return null;slice.Restart();}
                sampledPhases++;
                if(!(bool)Method("FillSurfacePawSample").Invoke(null,new object[]{geometry,plan,cat.transform.right,paws,s}))
                {reachable=false;unreachablePhase=entry.samples[s].phase;break;}
                for(int side=0;side<2;side++)
                {
                    var probe=paws[s*2+side];bool clear=cat.IsInteractionBodyClear(new[]{probe},.002f);
                    if((side==0)==plan.Left)activeClear&=clear;else otherClear&=clear;
                    if(clear||badPhase>=0)continue;badPhase=entry.samples[s].phase;badSide=side==0?"left":"right";
                    Collider blocker=null;
                    foreach(var c in Physics.OverlapCapsule(probe.start,probe.end,probe.radius,~0,QueryTriggerInteraction.Ignore))
                    {
                        if(!(bool)solid.Invoke(guard,new object[]{c}))continue;
                        object[] args={probe.start,probe.end,probe.radius,c,Vector3.zero,0f};
                        if((bool)penetrate.Invoke(guard,args)&&(float)args[5]>depth){depth=(float)args[5];blocker=c;colliderPath=Path(c.transform);}
                    }
                    if(measureExact&&blocker is MeshCollider mesh&&CatPawReachResolver.TryPredictSurfaceSample(cat,plan,entry.samples[s],out var lp,out var rp,out _))
                    {
                        exactPlans++;exactMeasured=true;
                        var points=side==0?lp:rp;var definitions=side==0?entry.leftPaw:entry.rightPaw;
                        for(int v=0;v<points.Length;v++)
                        {
                            if(detailTimer.Elapsed.TotalSeconds>=12){truncated=true;break;}
                            if(slice.Elapsed.TotalMilliseconds>=3){yield return null;slice.Restart();}
                            if(!mesh.bounds.Contains(points[v]))continue;
                            object[] args={mesh,points[v],entry.samples[s].phase,badSide,definitions[v].vertexIndex,new List<string>(),0};
                            inside.Invoke(witness,args);int insideVotes=(int)args[6];if(insideVotes<4)continue;
                            var measured=actualMetric.Measure(mesh.sharedMesh,mesh.transform,points[v]);
                            if(measured.distance>skinDepth){skinDepth=measured.distance;vertex=definitions[v].vertexIndex;point=points[v];votes=insideVotes;}
                        }
                    }
                }
            }
            details.Add(string.Join(",",candidatesNow.IndexOf(candidate),N(plan.SourcePhase),N(plan.ChestPitch),N(plan.ChestYaw),N(plan.Surface.ApproachLift),fixedClear,upperClear,reachable,N(unreachablePhase),activeClear,otherClear,N(badPhase),badSide,colliderPath,N(depth),N(skinDepth),vertex,V(point),votes,sampledPhases,exactMeasured,truncated));
            System.IO.File.WriteAllLines(Root+"/flower-known-candidate-failures.csv",details);yield return null;
        }
        yield return TraceThreePaths(cat,surface,selected.Take(3).Select(c=>(CatPawReachPlan)Field(c,"plan")).ToArray());
        System.IO.File.WriteAllText(Root+"/flower-known-diagnostic-duration.txt",
            FormattableString.Invariant($"searchPlusDetailsSeconds={timer.Elapsed.TotalSeconds:F3}; detailsSeconds={detailTimer.Elapsed.TotalSeconds:F3}; detailedPlans={details.Count-1}; availableCandidates={candidatesNow.Count}; exactSkinPlans={exactPlans}; ready={ready}; pending={CatPawReachResolver.SurfaceQueryPending}\n"));
        // A diagnosis is successful if it records evidence; ready=false is NOT
        // a passing contact/appearance result, and no unsafe plan was run.
        Assert.That(details.Count,Is.GreaterThan(1),"No math candidate: progress CSV identifies exhaustion or pending geometry");
    }

    // A separate bounded path trace continues AFTER an unreachable sample so
    // the contact pose itself is still measured. Uses the same pure CCD as live
    // IK; it does not run a rejected plan or label prediction as rendered skin.
    IEnumerator TraceThreePaths(CatMovement cat,RaycastHit hit,CatPawReachPlan[] plans)
    {
        var rows=new List<string>{"plan,sourcePhase,isContactPhase,contactPhase,pitch,yaw,lift,envelope,target,targetNormal,approachNormal,patchFacingNormal,sourcePatch,goal,solvedPatch,goalError,upper,fore,hand,pawMin,pawMax,legacySingleCapsuleStart,legacySingleCapsuleEnd,legacySingleCapsuleRadius,legacySingleCapsuleStrictClear,legacySingleCapsuleBlocker,legacySingleCapsuleMTD,legacySingleCapsuleMTDNormal,targetOffsetSignedNormal,sourceToTargetDotNormal"};
        var visual=cat.GetComponentInChildren<CatBreedVisualTag>().transform;
        var guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Fields).GetValue(cat);
        var penetration=typeof(CatBodyGuard).GetMethod("Penetration",Fields);var solid=typeof(CatBodyGuard).GetMethod("Solid",Fields);
        var timer=System.Diagnostics.Stopwatch.StartNew();var slice=System.Diagnostics.Stopwatch.StartNew();bool truncated=false;
        for(int p=0;p<Mathf.Min(3,plans.Length);p++)
        {
            var plan=plans[p];
            foreach(var sample in plan.Surface.Entry.samples)
            {
                if(timer.Elapsed.TotalSeconds>=8){truncated=true;break;}
                if(slice.Elapsed.TotalMilliseconds>=3){yield return null;slice.Restart();}
                float weight=Mathf.Clamp01(sample.phase<=plan.SourcePhase?sample.phase/plan.SourcePhase:(1-sample.phase)/(1-plan.SourcePhase));
                Quaternion bend=Quaternion.AngleAxis(plan.ChestYaw*weight,Vector3.up)*Quaternion.AngleAxis(plan.ChestPitch*weight,cat.transform.right);
                Vector3 pivot=CatPawReachCatalog.WorldPoint(visual,sample.torsoPivot);
                var arm=plan.Left?sample.left:sample.right;var local=plan.Left?sample.leftPaw:sample.rightPaw;
                var points=new Vector3[local.Length];Vector3 sourcePatch=Vector3.zero;
                for(int i=0;i<points.Length;i++)points[i]=Map(local[i]);
                foreach(int index in plan.Surface.Vertices)sourcePatch+=points[index];sourcePatch/=plan.Surface.Vertices.Length;
                Vector3 upper=Map(arm.upper),fore=Map(arm.fore),hand=Map(arm.hand);
                Vector3 goal=CatPawSurfaceCcd.Goal(sourcePatch,plan.Surface.Point,weight,plan.Surface.ApproachNormal,plan.Surface.ApproachLift);
                var solved=CatPawSurfaceCcd.Solve(upper,fore,hand,sourcePatch,plan.Surface.Point,weight,plan.Surface.ApproachNormal,plan.Surface.ApproachLift);
                for(int i=0;i<points.Length;i++)points[i]=CatPawSurfaceCcd.TransformPoint(solved,hand,points[i]);
                var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
                var probe=(CatBodyGuardCatalog.Probe)Method("FitPaw").Invoke(null,new object[]{points,plan.Left?"left-paw":"right-paw"});
                bool clear=cat.IsInteractionBodyClear(new[]{probe},.002f);float maximumDepth=0;Vector3 normal=default;string blocker="";
                foreach(var collider in Physics.OverlapCapsule(probe.start,probe.end,probe.radius,~0,QueryTriggerInteraction.Ignore))
                {
                    if(!(bool)solid.Invoke(guard,new object[]{collider}))continue;
                    object[] args={probe.start,probe.end,probe.radius,collider,Vector3.zero,0f};
                    if((bool)penetration.Invoke(guard,args)&&(float)args[5]>maximumDepth)
                    {maximumDepth=(float)args[5];normal=(Vector3)args[4];blocker=Path(collider.transform);}
                }
                rows.Add(string.Join(",",p,N(sample.phase),Mathf.Abs(sample.phase-plan.SourcePhase)<.00001f,N(plan.SourcePhase),N(plan.ChestPitch),N(plan.ChestYaw),N(plan.Surface.ApproachLift),N(weight),
                    V(plan.Surface.Point),V(hit.normal),V(plan.Surface.ApproachNormal),V(plan.Surface.Normal),V(sourcePatch),V(goal),V(solved.Endpoint),N(Vector3.Distance(goal,solved.Endpoint)),
                    V(upper),V(fore),V(hand),V(bounds.min),V(bounds.max),V(probe.start),V(probe.end),N(probe.radius),clear,blocker,N(maximumDepth),V(normal),
                    N(Vector3.Dot(solved.Endpoint-plan.Surface.Point,plan.Surface.ApproachNormal)),N(Vector3.Dot(sourcePatch-plan.Surface.Point,plan.Surface.ApproachNormal))));
                Vector3 Map(Vector3 point)=>pivot+bend*(CatPawReachCatalog.WorldPoint(visual,point)-pivot);
            }
            System.IO.File.WriteAllLines(Root+"/flower-known-three-paths.csv",rows);if(truncated)break;yield return null;
        }
        System.IO.File.WriteAllLines(Root+"/flower-known-three-paths.csv",rows);
        System.IO.File.WriteAllText(Root+"/flower-known-three-paths-duration.txt",FormattableString.Invariant($"rows={rows.Count-1};seconds={timer.Elapsed.TotalSeconds:F3};truncated={truncated}\n"));
    }
}
