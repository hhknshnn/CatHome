using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class PawColdSearchProgressTests
{
    FlowerContactPolishTests fixture;
    Action<string,double,int,bool> previous;
    readonly List<string> rows=new List<string>();
    int bodyPhases,maxBodySample=-1;
    [SetUp] public void Before()
    {
        fixture=new FlowerContactPolishTests();fixture.Before();
        previous=CatPawReachResolver.SurfaceQueryMeasurement;
        CatPawReachResolver.SurfaceQueryMeasurement=Measure;
        rows.Add("frame,stage,sample,ms,clear");
    }
    void Measure(string stage,double ms,int sample,bool clear)
    {
        if(stage=="body-phase"){bodyPhases++;maxBodySample=Mathf.Max(maxBodySample,sample);}
        if(stage=="fixed-body-cold"||stage=="body-phase")
            rows.Add(FormattableString.Invariant($"{Time.frameCount},{stage},{sample},{ms:F6},{clear}"));
    }
    [TearDown] public void After()
    {
        CatPawReachResolver.SurfaceQueryMeasurement=previous;
        const string root="Docs/QA/INTERACTION_POLISH_FINISH_2026-09-16/flower-knock";
        Directory.CreateDirectory(root);File.WriteAllLines(root+"/cold-search-progress.csv",rows);
        fixture?.After();
    }
    [UnityTest,Timeout(60000)] public IEnumerator SideTable_StationaryColdSearchAdvancesBeyondRepeatedFixedChecks()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();
        store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("russian-blue");
        var cat=Object.FindAnyObjectByType<CatMovement>();
        yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<KnockOffActivity>().Single(a=>
            a.gameObject.scene==cat.gameObject.scene&&a.StoreProductId==HomeStoreService.BalconySideTableId);
        Vector3 root=new Vector3(-3.01339769f,.05f,-.149999857f);
        Quaternion rotation=Quaternion.Euler(0,184.719849f,0);
        var cc=cat.GetComponent<CharacterController>();cc.enabled=false;
        cat.transform.SetPositionAndRotation(root,rotation);cc.enabled=true;Physics.SyncTransforms();
        yield return null;
        Assert.That(cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation),Is.True);
        float until=Time.realtimeSinceStartup+8;int queries=0;
        // This legal standing pose previously spent 900 frames on the same
        // prerequisite and evaluated zero candidate-body source phases. This
        // test proves progress only; full contact acceptance has separate tests.
        while(Time.realtimeSinceStartup<until&&queries<240&&maxBodySample<1)
        {
            yield return null;activity.TryGetStartPose(cat,out _);queries++;
            Assert.That(Vector3.Distance(cat.transform.position,root),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(cat.transform.rotation,rotation),Is.LessThan(.2f));
            Assert.That(activity.IsRunning,Is.False);
        }
        rows.Add(FormattableString.Invariant($"{Time.frameCount},summary,{maxBodySample},0,{bodyPhases>0}"));
        Assert.That(bodyPhases,Is.GreaterThan(0),"A yielded cold search must eventually evaluate candidate physics");
        Assert.That(maxBodySample,Is.GreaterThanOrEqualTo(1),"Repeated fixed-body validation must not consume every frame before source-phase progress");
    }
}
