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

public sealed class PawQueryStageDiagnosticTests
{
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
    FlowerContactPolishTests fixture;readonly List<string> stages=new List<string>(),queries=new List<string>();
    int stance=-1;string mode="";
    static object Get(object value,string name)=>value?.GetType().GetField(name,F)?.GetValue(value);
    [SetUp]public void Before()
    {
        fixture=new FlowerContactPolishTests();fixture.Before();
        stages.Add("mode,stance,frame,stage,sample,ms,clear");
        queries.Add("stance,frame,ms,ready,pending,math,physics,geometryCount,build,requestCount,cursor,scan,candidates,bodyCursor,pawCursor,upperScreened,endpointScreened,sourcePhase,pitch,yaw,bodyCCClear");
        CatPawReachResolver.SurfaceQueryMeasurement=Measure;
    }
    void Measure(string stage,double ms,int sample,bool clear)
    {
        // Store outside the measured stage; no Unity logging or file I/O in
        // the callback. Fast clear stages need no per-face telemetry flood.
        if(ms>=.10||!clear)stages.Add(FormattableString.Invariant($"{mode},{stance},{Time.frameCount},{stage},{sample},{ms:F6},{clear}"));
    }
    [TearDown]public void After()
    {
        CatPawReachResolver.SurfaceQueryMeasurement=null;
        Directory.CreateDirectory(Root);File.WriteAllLines(Root+"/paw-query-stages-"+TestContext.CurrentContext.Test.Name+".csv",stages);
        File.WriteAllLines(Root+"/paw-query-cursors-"+TestContext.CurrentContext.Test.Name+".csv",queries);fixture?.After();
    }
    [UnityTest,Timeout(90000)]public IEnumerator FlowerColdSearch_AtomicStageCostsWithUnchangedBudget()
    {
        mode="flower";yield return fixture.CooperativePawSearch_SixtyFrames_BoundsEachCall();
        Assert.That(stages.Count,Is.GreaterThan(1));
    }
    [UnityTest,Timeout(90000)]public IEnumerator KnockTwoLegalStances_AtomicStageAndCursorDiagnostic()
    {
        mode="knock";yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("russian-blue");
        var cat=Object.FindAnyObjectByType<CatMovement>();yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<KnockOffActivity>().Single(a=>a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BalconySideTableId);
        Vector3[] points={new Vector3(-2.74492979f,.05f,-.3049999f),new Vector3(-3.01339769f,.05f,-.149999857f)};float[] yaws={244.254883f,184.719849f};
        for(stance=0;stance<points.Length;stance++)
        {
            var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.SetPositionAndRotation(points[stance],Quaternion.Euler(0,yaws[stance],0));cc.enabled=true;Physics.SyncTransforms();
            float deadline=Time.realtimeSinceStartup+12;
            for(int frame=0;frame<900&&Time.realtimeSinceStartup<deadline;frame++)
            {
                yield return null;var watch=System.Diagnostics.Stopwatch.StartNew();bool ready=activity.TryGetStartPose(cat,out _);watch.Stop();
                int math=CatPawReachResolver.SurfaceMathCandidatesLastQuery,physics=CatPawReachResolver.SurfacePhysicsCandidatesLastQuery;
                var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",S).GetValue(null);
                var geometry=cache.Values.Cast<object>().FirstOrDefault();var requests=Get(geometry,"requests") as IDictionary;
                object request=null,candidate=null;
                if(requests!=null)
                {
                    // Report the furthest active cursor, not an arbitrary
                    // dictionary entry when target fairness rotates surfaces.
                    request=requests.Values.Cast<object>().OrderByDescending(v=>(int)Get(v,"scanIndex")).ThenByDescending(v=>(int)Get(v,"cursor")).FirstOrDefault();
                    var list=Get(request,"candidates") as IList;int index=request!=null?(int)Get(request,"scanIndex"):-1;
                    if(list!=null&&index>=0&&index<list.Count)candidate=list[index];
                }
                var plan=candidate!=null?(CatPawReachPlan)Get(candidate,"plan"):default;
                queries.Add(string.Join(",",stance,Time.frameCount,watch.Elapsed.TotalMilliseconds.ToString("F6",CultureInfo.InvariantCulture),ready,activity.IsStartSearchPending,math,physics,
                    cache.Count,Get(geometry,"buildCursor")??-1,requests?.Count??0,Get(request,"cursor")??-1,Get(request,"scanIndex")??-1,
                    (Get(request,"candidates") as IList)?.Count??0,Get(candidate,"bodyBuildCursor")??-1,Get(candidate,"pawBuildCursor")??-1,
                    Get(candidate,"upperScreened")??false,Get(candidate,"endpointScreened")??false,
                    plan.SourcePhase.ToString("G9",CultureInfo.InvariantCulture),plan.ChestPitch.ToString("G9",CultureInfo.InvariantCulture),plan.ChestYaw.ToString("G9",CultureInfo.InvariantCulture),
                    cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation)));
                if(frame%60==0)File.WriteAllLines(Root+"/paw-query-cursors-"+TestContext.CurrentContext.Test.Name+".csv",queries);
                if(ready||!activity.IsStartSearchPending)break;
            }
        }
        Assert.That(queries.Count,Is.GreaterThan(2),"Diagnostic only; no-ready is never relabeled as a completed contact");
    }
}
