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
using Object = UnityEngine.Object;

// Measurement only. A completed diagnostic is not a completed flower activity.
// The existing production budget, candidate geometry and physics are unchanged.
public sealed class MaineFlowerReadinessDiagnosticTests
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic;
    const string Root = "Docs/QA/INTERACTION_POLISH_110MIN_2026-09-17/maine-readiness-diagnostic";
    FlowerContactPolishTests fixture;
    Action<string,double,int,bool> previousMeasurement;
    readonly List<string> queries = new List<string>(), stages = new List<string>(), requests = new List<string>();
    bool measuring;
    int query;
    string lastStage;
    static object Get(object value,string name) => value?.GetType().GetField(name,F)?.GetValue(value);
    static int Number(object value,string name) => Get(value,name) is int number ? number : -1;
    static string Csv(params object[] values) => string.Join(",",values.Select(value => "\"" +
        (value is IFormattable formattable ? formattable.ToString(null,CultureInfo.InvariantCulture) : value?.ToString() ?? "").Replace("\"","\"\"") + "\""));
    static string Point(Vector3 value) => value.x.ToString("G9",CultureInfo.InvariantCulture)+";"+
        value.y.ToString("G9",CultureInfo.InvariantCulture)+";"+value.z.ToString("G9",CultureInfo.InvariantCulture);
    static string Matrix(Matrix4x4 value) => string.Join(";",Enumerable.Range(0,16).Select(i=>value[i].ToString("G9",CultureInfo.InvariantCulture)));

    [SetUp] public void Before()
    {
        fixture = new FlowerContactPolishTests(); fixture.Before();
        previousMeasurement = CatPawReachResolver.SurfaceQueryMeasurement;
        CatPawReachResolver.SurfaceQueryMeasurement = Measure;
        queries.Add("query,frame,elapsedSeconds,queryMs,ready,pending,math,physics,targetBefore,targetAfter,geometryIdentity,build,geometryReady,requestCount,totalCursor,totalScan,totalCandidates,furthestCursor,furthestScan,bodyCursor,pawCursor,sourcePhase,pitch,chestYaw,upperScreened,endpointScreened,lastStage,initialBodyCCClear,root,yaw,visualMatrix");
        stages.Add("query,frame,stage,sample,ms,clear");
        requests.Add("query,request,target,normal,cursor,scan,candidates,accepted,sourcePhase,pitch,chestYaw,bodyCursor,pawCursor,unreachable,upperScreened,endpointScreened");
    }
    void Measure(string stage,double ms,int sample,bool clear)
    {
        if(!measuring)return;
        lastStage = stage;
        // Same callback pattern as PawQueryStageDiagnosticTests: no logging,
        // mesh sampling, physics queries or file I/O in the measured stage.
        stages.Add(Csv(query,Time.frameCount,stage,sample,ms,clear));
    }
    [TearDown] public void After()
    {
        measuring = false;
        CatPawReachResolver.SurfaceQueryMeasurement = previousMeasurement;
        try
        {
            Directory.CreateDirectory(Root);
            File.WriteAllLines(Root+"/maine-readiness-queries.csv",queries);
            File.WriteAllLines(Root+"/maine-readiness-stages.csv",stages);
            File.WriteAllLines(Root+"/maine-readiness-requests.csv",requests);
        }
        finally { fixture?.After(); }
    }

    [UnityTest,Timeout(90000)]
    public IEnumerator MaineCoon_ExactAuthoredStance_EightSecondSearchStageDiagnostic()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store = HomeStoreSaveState.CreateDefault();
        store.ownedProductIds = HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);
        CatBreedService.Select("maine-coon");
        var cat = Object.FindAnyObjectByType<CatMovement>();
        yield return QaBreedReadiness.WaitForSelected(cat,"maine-coon");
        cat.GetComponent<CatIdleBehavior>().enabled = false;
        var activity = CatActivity.Registered.OfType<SitLookActivity>().Single(a =>
            a.gameObject.scene == cat.gameObject.scene && a.Kind == CatActivityKind.RailingSwat);
        var targets = ((Vector3[])Get(activity,"visibleLookTargets")).Select(activity.transform.TransformPoint);
        var first = targets.OrderBy(p => Vector3.ProjectOnPlane(p-activity.RoutineEntryPoint.position,Vector3.up).sqrMagnitude).First();
        // Identical to FindStance's first authored position/heading, not rounded
        // world coordinates or a known-good point from a different breed.
        Vector3 point = first+activity.transform.forward*.40f; point.y=.05f;
        Vector3 toward = first-point; toward.y=0;
        var cc = cat.GetComponent<CharacterController>(); bool wasEnabled=cc.enabled;
        cc.enabled=false;
        cat.transform.SetPositionAndRotation(point,Quaternion.LookRotation(toward)*Quaternion.Euler(0,-80,0));
        cc.enabled=wasEnabled; Physics.SyncTransforms(); yield return null;
        bool initialClear=cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        IDictionary lastRequests=null;
        while(timer.Elapsed.TotalSeconds<8)
        {
            query++; lastStage="none";
            int before=Number(activity,"startSearchIndex");
            bool ready; var watch=System.Diagnostics.Stopwatch.StartNew();
            measuring=true;
            try { ready=activity.TryGetPromptDistance(cat,out _); }
            finally { measuring=false;watch.Stop(); }
            int math=CatPawReachResolver.SurfaceMathCandidatesLastQuery,physics=CatPawReachResolver.SurfacePhysicsCandidatesLastQuery;
            var cache=(IDictionary)typeof(CatPawReachResolver).GetField("surfaceGeometryCache",S).GetValue(null);
            var geometry=cache.Values.Cast<object>().FirstOrDefault();
            lastRequests=Get(geometry,"requests") as IDictionary;
            int totalCursor=0,totalScan=0,totalCandidates=0;
            object furthest=null,candidate=null;
            if(lastRequests!=null)foreach(DictionaryEntry pair in lastRequests)
            {
                object request=pair.Value;
                totalCursor+=Number(request,"cursor");totalScan+=Number(request,"scanIndex");
                totalCandidates+=(Get(request,"candidates") as IList)?.Count??0;
                if(furthest==null || Number(request,"scanIndex")>Number(furthest,"scanIndex") ||
                    Number(request,"scanIndex")==Number(furthest,"scanIndex") && Number(request,"cursor")>Number(furthest,"cursor"))furthest=request;
            }
            var candidates=Get(furthest,"candidates") as IList;int scan=Number(furthest,"scanIndex");
            if(candidates!=null&&scan>=0&&scan<candidates.Count)candidate=candidates[scan];
            var plan=candidate!=null?(CatPawReachPlan)Get(candidate,"plan"):default;
            var visual=cat.GetComponentInChildren<CatBreedVisualTag>();
            queries.Add(Csv(query,Time.frameCount,timer.Elapsed.TotalSeconds,watch.Elapsed.TotalMilliseconds,ready,
                activity.IsStartSearchPending,math,physics,before,Number(activity,"startSearchIndex"),
                geometry==null?0:System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(geometry),
                Number(geometry,"buildCursor"),Get(geometry,"ready")??false,lastRequests?.Count??0,totalCursor,totalScan,totalCandidates,
                Number(furthest,"cursor"),scan,Number(candidate,"bodyBuildCursor"),Number(candidate,"pawBuildCursor"),
                plan.SourcePhase,plan.ChestPitch,plan.ChestYaw,Get(candidate,"upperScreened")??false,Get(candidate,"endpointScreened")??false,
                lastStage,initialClear,Point(cat.transform.position),cat.transform.eulerAngles.y,Matrix(visual.transform.localToWorldMatrix)));
            if(ready||!activity.IsStartSearchPending)break;
            yield return null;
        }
        if(lastRequests!=null)
        {
            int index=0;
            foreach(DictionaryEntry pair in lastRequests)
            {
                var request=pair.Value;var list=Get(request,"candidates") as IList;int scan=Number(request,"scanIndex");
                object candidate=list!=null&&scan>=0&&scan<list.Count?list[scan]:null;
                var plan=candidate!=null?(CatPawReachPlan)Get(candidate,"plan"):default;
                requests.Add(Csv(query,index++,Point((Vector3)Get(pair.Key,"point")),Point((Vector3)Get(pair.Key,"normal")),
                    Number(request,"cursor"),scan,list?.Count??0,Get(request,"accepted")!=null,plan.SourcePhase,plan.ChestPitch,plan.ChestYaw,
                    Number(candidate,"bodyBuildCursor"),Number(candidate,"pawBuildCursor"),Get(candidate,"unreachable")??false,
                    Get(candidate,"upperScreened")??false,Get(candidate,"endpointScreened")??false));
            }
        }
        Assert.That(query,Is.GreaterThan(0),"Diagnostic must actually poll production readiness.");
        Assert.That(activity.IsRunning,Is.False,"A rejected plan must never be force-started.");
        // Ready=false/Pending=true remains an unresolved interaction result.
        // This assertion only verifies that the bounded observation completed.
        Assert.That(queries.Count,Is.EqualTo(query+1));
    }
}
