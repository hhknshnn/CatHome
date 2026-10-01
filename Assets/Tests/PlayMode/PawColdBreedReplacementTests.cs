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

// Full normal Home fixture; direct production Select, never fixture.Breed or
// a QA preload before selection. Breadcrumbs survive an editor/native stall.
public sealed class PawColdBreedReplacementTests
{
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
    const string Root="Docs/QA/INTERACTION_POLISH_2026-09-16";
    CareAlignmentPolishTests fixture;bool prepared;
    readonly List<string> rows=new List<string>();
    static object Field(object value,string name)=>value.GetType().GetField(name,Instance).GetValue(value);
    static object Call(object value,string name,params object[] args)=>value.GetType().GetMethod(name,Instance).Invoke(value,args);
    static object CatalogField(string name)=>typeof(CatPawReachCatalog).GetField(name,Static).GetValue(null);
    static bool Pending(object slot)
    {var request=(ResourceRequest)Field(slot,"request");return request!=null&&!request.isDone;}
    static bool AlreadyStartedRequestsPending()
    {
        if(Pending(CatalogField("root")))return true;
        foreach(string key in new[]{"bodies","surfaces"})
            foreach(object slot in ((IDictionary)CatalogField(key)).Values)if(Pending(slot))return true;
        return false;
    }
    void Mark(string stage,string breed,double milliseconds=0)
    {
        rows.Add(FormattableString.Invariant($"{stage},{breed},{Time.frameCount},{Time.realtimeSinceStartup:F6},{milliseconds:F4}"));
        File.WriteAllLines(Root+"/paw-cold-breed-replacement.csv",rows);
    }
    [SetUp] public void Before()
    {fixture=new CareAlignmentPolishTests();fixture.Before();prepared=true;rows.Clear();rows.Add("stage,breed,frame,realtime,callMs");}
    [TearDown] public void After(){if(prepared)fixture.After();prepared=false;}

    [UnityTest,Timeout(300000)] public IEnumerator TenColdBreedChanges_ProductionPreload_AdvanceFrames_AndNormalRoomTransition()
    {
        Mark("home-before",CatBreedService.SelectedBreedId);
        yield return (IEnumerator)Call(fixture,"Home");
        var cat=(CatMovement)Field(fixture,"cat");
        Assert.That(CatActivity.Active,Is.Null);
        float deadline=Time.realtimeSinceStartup+15;
        while(AlreadyStartedRequestsPending()&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(AlreadyStartedRequestsPending(),Is.False,"Never unload an unfinished native resource request");
        CatPawReachCatalog.EditorClearResourceCacheForQa();
        Mark("cache-cleared",CatBreedService.SelectedBreedId);
        var breeds=CatBreedCatalog.Load().Entries.ToArray();Assert.That(breeds.Length,Is.EqualTo(10));
        int current=Array.FindIndex(breeds,b=>b.Id==CatBreedService.SelectedBreedId);Assert.That(current,Is.GreaterThanOrEqualTo(0));
        for(int i=1;i<=breeds.Length;i++)
        {
            string breed=breeds[(current+i)%breeds.Length].Id;
            Assert.That(breed,Is.Not.EqualTo(CatBreedService.SelectedBreedId),"Every selection must actually replace the Animator");
            var previousVisual=cat.GetComponentInChildren<CatBreedVisualTag>();
            var previousAnimator=cat.GetComponentInChildren<Animator>();
            string previousBreed=previousVisual.BreedId;
            var guard=Field(cat,"bodyGuard");var previousProbes=Field(guard,"probes");
            Mark("select-before",breed);
            var timer=System.Diagnostics.Stopwatch.StartNew();Assert.That(CatBreedService.Select(breed),Is.True);timer.Stop();
            Mark("select-returned",breed,timer.Elapsed.TotalMilliseconds);
            // Selection is immediate, but a cold Animator replacement must
            // remain behind the production data-ready + one-frame barrier.
            Assert.That(cat.GetComponentInChildren<CatBreedVisualTag>(),Is.SameAs(previousVisual));
            Assert.That(((IDictionary)CatalogField("bodies")).Contains(breed),Is.True,"Production selection must initiate body data load");
            deadline=Time.realtimeSinceStartup+15;int advanced=0;bool effectiveReady=false,reportedDataReady=false;
            while(Time.realtimeSinceStartup<deadline)
            {
                bool dataReady=CatPawReachCatalog.IsReadyFor(breed);
                var currentVisual=cat.GetComponentInChildren<CatBreedVisualTag>();
                if(!dataReady)
                {
                    Assert.That(previousVisual!=null&&previousVisual.gameObject.activeInHierarchy,Is.True,"Keep the old visible model until data is ready");
                    Assert.That(previousAnimator!=null&&previousAnimator.enabled,Is.True);
                    Assert.That(currentVisual,Is.SameAs(previousVisual));
                    Assert.That(currentVisual.BreedId,Is.EqualTo(previousBreed));
                    Assert.That(Field(guard,"probes"),Is.SameAs(previousProbes),"Keep old anatomy probes with the old visual");
                }
                if(dataReady&&!reportedDataReady){Mark("data-ready",breed);reportedDataReady=true;}
                effectiveReady=dataReady&&currentVisual!=null&&currentVisual.BreedId==breed;
                if(effectiveReady&&advanced>=3)break;
                int before=Time.frameCount;yield return null;Assert.That(Time.frameCount,Is.GreaterThan(before));advanced++;
                if(advanced<=3||advanced%15==0)Mark("frame-"+advanced,breed);
            }
            Assert.That(effectiveReady,Is.True,"Selected breed must become the effective live visual within 15 seconds");
            Assert.That(advanced,Is.GreaterThanOrEqualTo(3));
            Assert.That(CatPawReachCatalog.IsLoadingFor(breed),Is.False,"Selected breed async load deadline");
            Assert.That(CatPawReachCatalog.IsReadyFor(breed),Is.True);
            Assert.That(cat.GetComponentsInChildren<CatBreedVisualTag>(true).Length,Is.EqualTo(1));
            Assert.That(cat.GetComponentsInChildren<Animator>(true).Length,Is.EqualTo(1));
            Assert.That(cat.GetComponentInChildren<CatBreedVisualTag>().BreedId,Is.EqualTo(breed));
            Assert.That(Object.FindObjectsByType<CatMovement>().Length,Is.EqualTo(1));
            Assert.That(cat.AreWorldActionsBlocked||HomeUiFlow.IsHomeControlBlocked,Is.False);
            Mark("ready-one-visual-control",breed);
        }
        Mark("kitchen-before",CatBreedService.SelectedBreedId);
        yield return (IEnumerator)Call(fixture,"Room",HomeRoomService.KitchenId);
        cat=(CatMovement)Field(fixture,"cat");
        Assert.That(cat.gameObject.scene.name,Is.EqualTo(HomeRoomService.GetOrLivingRoom(HomeRoomService.KitchenId).SceneName));
        Assert.That(Object.FindObjectsByType<CatMovement>().Length,Is.EqualTo(1));
        Assert.That(cat.GetComponentsInChildren<CatBreedVisualTag>(true).Length,Is.EqualTo(1));
        Assert.That(cat.AreWorldActionsBlocked||HomeUiFlow.IsHomeControlBlocked,Is.False);
        Mark("kitchen-ready-one-visual-control",CatBreedService.SelectedBreedId);
    }
}
#endif
