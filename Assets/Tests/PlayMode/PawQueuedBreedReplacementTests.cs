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

public sealed class PawQueuedBreedReplacementTests
{
    const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    CareAlignmentPolishTests fixture;bool prepared,restoreHost,hostEnabled;
    CatBreedRuntimeController host;CatMovement cat;
    readonly List<string> rows=new List<string>();
    static object Field(object value,string name)=>value.GetType().GetField(name,Fields).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Fields).Invoke(value,args);
    void Mark(string stage)
    {
        string visual=cat!=null?cat.GetComponentInChildren<CatBreedVisualTag>()?.BreedId:"<no cat>";
        rows.Add(FormattableString.Invariant($"{stage},{Time.frameCount},{Time.realtimeSinceStartup:F5},{CatBreedService.SelectedBreedId},{visual},{CatPawReachCatalog.HasPendingLoads}"));
        File.WriteAllLines(Root+"/paw-queued-"+TestContext.CurrentContext.Test.Name+".csv",rows);
    }
    [SetUp] public void Before()
    {fixture=new CareAlignmentPolishTests();fixture.Before();prepared=true;rows.Clear();rows.Add("stage,frame,realtime,selected,visual,anyCatalogPending");}
    [UnityTearDown] public IEnumerator After()
    {
        if(restoreHost&&host!=null)host.enabled=hostEnabled;
        if(prepared){prepared=false;fixture.After();}
        // Restoring the saved selection may start a cold production preload.
        // The next fixture must not begin a Single scene load in that window.
        float deadline=Time.realtimeSinceStartup+15;
        while(CatPawReachCatalog.HasPendingLoads&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(CatPawReachCatalog.HasPendingLoads,Is.False,"Restored breed resources must settle before the next fixture");
        yield return null;
        Mark("teardown-resources-settled");
    }
    IEnumerator ColdHome()
    {
        yield return (IEnumerator)Call(fixture,"Home");cat=(CatMovement)Field(fixture,"cat");
        host=Object.FindAnyObjectByType<CatBreedRuntimeController>();Assert.That(host,Is.Not.Null);
        hostEnabled=host.enabled;restoreHost=true;Assert.That(hostEnabled,Is.True);
        float deadline=Time.realtimeSinceStartup+15;
        while(CatPawReachCatalog.HasPendingLoads&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(CatPawReachCatalog.HasPendingLoads,Is.False);Assert.That(CatActivity.Active,Is.Null);
        CatPawReachCatalog.EditorClearResourceCacheForQa();Mark("cold-cache");
    }
    string[] NextTwo()
    {
        var ids=new[]{"oriental-shorthair","persian","british-shorthair"}.Where(b=>b!=CatBreedService.SelectedBreedId).Take(2).ToArray();
        foreach(string id in ids)Assert.That(CatBreedCatalog.Load().Find(id),Is.Not.Null);
        return ids;
    }
    static void OneVisual(CatMovement actor,string expected)
    {
        Assert.That(actor,Is.Not.Null);
        Assert.That(actor.GetComponentsInChildren<CatBreedVisualTag>(true).Length,Is.EqualTo(1));
        Assert.That(actor.GetComponentsInChildren<Animator>(true).Length,Is.EqualTo(1));
        Assert.That(actor.GetComponentInChildren<CatBreedVisualTag>().BreedId,Is.EqualTo(expected));
    }
    IEnumerator Effective(string expected)
    {
        float deadline=Time.realtimeSinceStartup+15;
        while(Time.realtimeSinceStartup<deadline)
        {
            var visual=cat.GetComponentInChildren<CatBreedVisualTag>();
            if(visual!=null&&visual.BreedId==expected&&!CatPawReachCatalog.HasPendingLoads)break;
            yield return null;
        }
        OneVisual(cat,expected);Assert.That(CatPawReachCatalog.HasPendingLoads,Is.False);
        Assert.That(cat.AreWorldActionsBlocked||HomeUiFlow.IsHomeControlBlocked,Is.False);
        Mark("effective-latest");
    }

    [UnityTest,Timeout(120000)] public IEnumerator TwoColdSelectionsSameFrame_OnlyLatestSurvivesImmediateRoomChange()
    {
        yield return ColdHome();var ids=NextTwo();var original=cat.GetComponentInChildren<CatBreedVisualTag>();
        int frame=Time.frameCount;Mark("select-A-before");Assert.That(CatBreedService.Select(ids[0]),Is.True);Mark("select-A-returned");
        Assert.That(CatBreedService.Select(ids[1]),Is.True);Mark("select-B-returned");
        Assert.That(Time.frameCount,Is.EqualTo(frame));Assert.That(CatBreedService.SelectedBreedId,Is.EqualTo(ids[1]));
        Assert.That(cat.GetComponentInChildren<CatBreedVisualTag>(),Is.SameAs(original),"No same-frame Animator replacement");
        var loader=(LevelLoader)Field(fixture,"loader");
        Assert.That(loader.LoadRoom(HomeRoomService.KitchenId),Is.True);Mark("room-request-before-install");
        float deadline=Time.realtimeSinceStartup+25;
        while((!loader.IsReady||loader.CurrentRoom.Id!=HomeRoomService.KitchenId)&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(loader.IsReady&&loader.CurrentRoom.Id==HomeRoomService.KitchenId,Is.True);
        cat=Object.FindAnyObjectByType<CatMovement>();Assert.That(cat,Is.Not.Null);
        cat.GetComponent<CatIdleBehavior>().enabled=false;yield return Effective(ids[1]);
        Assert.That(Object.FindObjectsByType<CatMovement>().Length,Is.EqualTo(1));
        Assert.That(cat.gameObject.scene.name,Is.EqualTo(HomeRoomService.GetOrLivingRoom(HomeRoomService.KitchenId).SceneName));
        for(int i=0;i<3;i++){yield return null;OneVisual(cat,ids[1]);}
        Mark("room-latest-stable");
    }

    [UnityTest,Timeout(120000)] public IEnumerator DisabledPendingHost_KeepsOldRig_AndInstallsLatestAfterEnable()
    {
        yield return ColdHome();var ids=NextTwo();var original=cat.GetComponentInChildren<CatBreedVisualTag>();
        var animator=cat.GetComponentInChildren<Animator>();string oldBreed=original.BreedId;
        var guard=Field(cat,"bodyGuard");var probes=Field(guard,"probes");
        Assert.That(CatBreedService.Select(ids[0]),Is.True);host.enabled=false;
        Assert.That(CatBreedService.Select(ids[1]),Is.True);Mark("host-disabled-latest-selected");
        float deadline=Time.realtimeSinceStartup+15;int frames=0;
        while((CatPawReachCatalog.HasPendingLoads||frames<3)&&Time.realtimeSinceStartup<deadline)
        {
            yield return null;frames++;
            Assert.That(original!=null&&original.gameObject.activeInHierarchy,Is.True);
            Assert.That(animator!=null&&animator.enabled,Is.True);
            Assert.That(cat.GetComponentInChildren<CatBreedVisualTag>(),Is.SameAs(original));
            Assert.That(Field(guard,"probes"),Is.SameAs(probes));OneVisual(cat,oldBreed);
        }
        Assert.That(CatPawReachCatalog.HasPendingLoads,Is.False);Assert.That(frames,Is.GreaterThanOrEqualTo(3));
        Assert.That(CatBreedService.SelectedBreedId,Is.EqualTo(ids[1]));Mark("disabled-old-rig-retained");
        host.enabled=true;Mark("host-enabled");yield return Effective(ids[1]);
        for(int i=0;i<3;i++){yield return null;OneVisual(cat,ids[1]);}
    }
}
#endif
