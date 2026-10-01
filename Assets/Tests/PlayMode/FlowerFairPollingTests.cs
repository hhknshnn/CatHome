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

public sealed class FlowerFairPollingTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    FlowerContactPolishTests fixture;CatMovement cat;SitLookActivity activity;
    GameObject blocker;readonly List<string> rows=new List<string>();
    static int Index(SitLookActivity value)=>(int)typeof(SitLookActivity).GetField("startSearchIndex",F).GetValue(value);
    [SetUp]public void Before(){fixture=new FlowerContactPolishTests();fixture.Before();rows.Add("frame,poll,ready,pending,nextIndex,math,physics,ms");}
    [TearDown]public void After()
    {
        if(blocker!=null){blocker.SetActive(false);Object.DestroyImmediate(blocker);}Physics.SyncTransforms();
        Directory.CreateDirectory("Docs/QA/INTERACTION_POLISH_2026-09-16");File.WriteAllLines("Docs/QA/INTERACTION_POLISH_2026-09-16/flower-fair-polls-"+TestContext.CurrentContext.Test.Name+".csv",rows);
        fixture?.After();
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-longhair");
        cat=Object.FindAnyObjectByType<CatMovement>();yield return QaBreedReadiness.WaitForSelected(cat,"domestic-longhair");
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        var all=((Vector3[])typeof(SitLookActivity).GetField("visibleLookTargets",F).GetValue(activity)).Select(activity.transform.TransformPoint);
        var target=all.OrderBy(p=>Vector3.ProjectOnPlane(p-activity.RoutineEntryPoint.position,Vector3.up).sqrMagnitude).First();
        // Full real-mesh contact verified this nearby longhair stance. The old
        // .40m point clears the idle body but has no verified full-arm path.
        // Place before polling; keep the search cold and the 45s budget intact.
        Vector3 point=target+activity.transform.forward*.395f;point.y=.05f;Vector3 heading=target-point;heading.y=0;
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;cat.transform.SetPositionAndRotation(point,Quaternion.LookRotation(heading)*Quaternion.Euler(0,-80,0));cc.enabled=true;
        Physics.SyncTransforms();yield return null;
        Assert.That(cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation),Is.True);
    }
    bool Poll(int call,out bool pending,out double elapsed)
    {
        var watch=System.Diagnostics.Stopwatch.StartNew();bool ready=activity.TryGetPromptDistance(cat,out _);watch.Stop();
        pending=activity.IsStartSearchPending;elapsed=watch.Elapsed.TotalMilliseconds;
        rows.Add(FormattableString.Invariant($"{Time.frameCount},{call},{ready},{pending},{Index(activity)},{CatPawReachResolver.SurfaceMathCandidatesLastQuery},{CatPawReachResolver.SurfacePhysicsCandidatesLastQuery},{elapsed:F6}"));
        return ready;
    }
    [UnityTest,Timeout(90000)]public IEnumerator ThreeSameFramePolls_KeepPendingTargetsFair_AndReady()
    {
        yield return Prepare();Vector3 root=cat.transform.position;Quaternion rotation=cat.transform.rotation;
        float deadline=Time.realtimeSinceStartup+45;bool ready=false;int zeroBudgetRepeated=0;
        while(!ready&&Time.realtimeSinceStartup<deadline)
        {
            yield return null;ready=Poll(0,out bool pending,out double firstMs);int next=Index(activity);
            bool expired=pending&&firstMs>=CatPawReachResolver.SurfaceSearchBudgetMilliseconds;
            for(int poll=1;poll<=2;poll++)
            {
                bool repeated=Poll(poll,out bool stillPending,out _);ready|=repeated;
                if(expired&&!repeated&&stillPending)
                {
                    Assert.That(Index(activity),Is.EqualTo(next),"A repeat HUD poll with no search budget must not skip another eligible surface");
                    Assert.That(CatPawReachResolver.SurfaceMathCandidatesLastQuery,Is.Zero);zeroBudgetRepeated++;
                }
            }
            Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(rotation,cat.transform.rotation),Is.LessThan(.2f));
        }
        Assert.That(zeroBudgetRepeated,Is.GreaterThan(1),"This regression must actually exercise repeated exhausted-budget polls");
        Assert.That(ready,Is.True,"A known legal player stance must reach production readiness within the unchanged 45-second search bound");
        Assert.That(activity.TryGetStartPose(cat,out var start),Is.True);Assert.That(start.HasPawPlan,Is.True);
    }
    [UnityTest,Timeout(90000)]public IEnumerator ReadySurface_RechecksSameFrameNewObstacle_AndRecovers()
    {
        yield return Prepare();float deadline=Time.realtimeSinceStartup+45;bool ready=false;
        while(!ready&&Time.realtimeSinceStartup<deadline){yield return null;ready=Poll(0,out _,out _);}
        Assert.That(ready,Is.True);Assert.That(activity.TryGetStartPose(cat,out var start),Is.True);
        var plan=start.PawPlan;Assert.That(plan.Surface?.IsValid,Is.True);
        Vector3 origin=cat.transform.position;Quaternion rotation=cat.transform.rotation;
        blocker=new GameObject("QA new real obstruction after flower readiness");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(blocker,cat.gameObject.scene);
        var box=blocker.AddComponent<BoxCollider>();box.size=Vector3.one*2;blocker.transform.position=origin+Vector3.up*.5f;
        int frame=Time.frameCount;Physics.SyncTransforms();
        Assert.That(activity.TryGetPromptDistance(cat,out _),Is.False,"Same-frame cached readiness must not bypass the real new solid");
        Assert.That(activity.TryStart(cat),Is.False,"Click must revalidate the same fresh obstruction");
        box.enabled=false;Physics.SyncTransforms();
        Assert.That(activity.TryGetPromptDistance(cat,out _),Is.True,"Removing a same-frame blocker must revalidate retained geometry immediately");
        Assert.That(Time.frameCount,Is.EqualTo(frame));Assert.That(activity.IsRunning,Is.False);
        Assert.That(Vector3.Distance(origin,cat.transform.position),Is.LessThan(.001f));Assert.That(Quaternion.Angle(rotation,cat.transform.rotation),Is.LessThan(.2f));
        // Also exercise the accepted surface cache itself, independently of the
        // early idle body gate, with the exact accepted request and live physics.
        box.size=Vector3.one*.06f;blocker.transform.position=plan.Surface.Point;box.enabled=true;Physics.SyncTransforms();
        Assert.That(CatPawReachResolver.TryResolveSurface(cat,plan.Surface.SourceRequest,plan.Left,plan.Pose,out _,32,35),Is.False,"A new solid around the real endpoint must invalidate a cached clear arm path");
        box.enabled=false;Physics.SyncTransforms();
        Assert.That(CatPawReachResolver.TryResolveSurface(cat,plan.Surface.SourceRequest,plan.Left,plan.Pose,out _,32,35),Is.True);
    }
}
