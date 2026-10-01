using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class GroundCurrentFrameContactTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    GroundContactStartTests fixture;
    CatMovement cat;
    CatActivity activity;
    CatToyContactMotion contact;
    CatPawReachMotion reach;
    bool contactEnabled,reachEnabled;
    object Invoke(string name,params object[] args)=>typeof(GroundContactStartTests).GetMethod(name,Private).Invoke(fixture,args);
    T Field<T>(string name)=>(T)typeof(GroundContactStartTests).GetField(name,Private).GetValue(fixture);
    [SetUp]public void Before(){fixture=new GroundContactStartTests();fixture.Before();}
    [TearDown]public void After()
    {
        Time.timeScale=1;
        if(contact!=null)contact.enabled=contactEnabled;
        if(reach!=null)reach.enabled=reachEnabled;
        fixture?.After();
    }
    IEnumerator Prepare(bool bird)
    {
        yield return (IEnumerator)Invoke("Prepare","Balcony_Level01");
        cat=Field<CatMovement>("cat");
        activity=CatActivity.Registered.Single(a=>a.gameObject.scene==cat.gameObject.scene&&
            (bird?a is BirdFeederShakeActivity:a is KnockOffActivity k&&k.PerchPoint==null&&k.StoreProductId==HomeStoreService.BalconySideTableId));
        if(bird)
        {
            yield return (IEnumerator)Invoke("FindStance",activity);
            Assert.That(Field<bool>("foundStance"),Is.True,"A real reachable stance is mandatory.");
        }
        else
        {
            // Hold the independently recorded real player stance while the
            // cooperative search runs. Moving on every poll discards its work
            // and never exercises the actual current-frame contact assertion.
            CatBreedService.Select("russian-blue");
            yield return QaBreedReadiness.WaitForSelected(cat,"russian-blue");
            var controller=cat.GetComponent<CharacterController>();controller.enabled=false;
            cat.transform.SetPositionAndRotation(new Vector3(-2.74492979f,.05f,-.3049999f),Quaternion.Euler(0,244.254883f,0));
            controller.enabled=true;Physics.SyncTransforms();yield return null;
            Assert.That(cat.IsInteractionPoseClear(cat.transform.position,cat.transform.rotation),Is.True);
            float until=Time.realtimeSinceStartup+10;bool ready=false;
            do
            {
                ready=activity.TryGetStartPose(cat,out _);
                if(ready||!((KnockOffActivity)activity).IsStartSearchPending)break;
                yield return null;
            }while(Time.realtimeSinceStartup<until);
            Assert.That(ready,Is.True,"The real unchanged stance must finish its fresh bounded search.");
        }
        RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        contact=cat.GetComponent<CatToyContactMotion>();reach=cat.GetComponent<CatPawReachMotion>();
        contactEnabled=contact.enabled;reachEnabled=reach.enabled;
    }
    int Count()=>activity is BirdFeederShakeActivity bird?bird.ContactStrokes:((KnockOffActivity)activity).ContactStrokes;
    float Distance()=>activity is BirdFeederShakeActivity bird?bird.MinimumPawDistance:((KnockOffActivity)activity).MinimumPawDistance;
    Transform Prop()=>activity is BirdFeederShakeActivity bird?bird.FeederPivot:((KnockOffActivity)activity).GlassPivot;

    [UnityTest,Timeout(180000)] public IEnumerator Bird_ActualContactThenPause_PreservesRootAndProp()=>RunPositive(true);
    [UnityTest,Timeout(180000)] public IEnumerator GroundKnock_ActualContactThenPause_PreservesRootAndProp()=>RunPositive(false);
    IEnumerator RunPositive(bool bird)
    {
        yield return Prepare(bird);Vector3 root=cat.transform.position;Quaternion yaw=cat.transform.rotation;
        float deadline=Time.realtimeSinceStartup+20;
        while(activity.IsRunning&&Distance()>=.025f&&Time.realtimeSinceStartup<deadline)yield return new WaitForEndOfFrame();
        Assert.That(activity.IsRunning,Is.True);Assert.That(Distance(),Is.LessThan(.025f));
        Time.timeScale=0;yield return null;yield return new WaitForEndOfFrame();
        int count=Count();Vector3 prop=Prop().position;Quaternion rotation=Prop().rotation;
        for(int i=0;i<6;i++)
        {
            yield return null;yield return new WaitForEndOfFrame();
            Assert.That(Count(),Is.EqualTo(count));
            Assert.That(Vector3.Distance(Prop().position,prop),Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(Prop().rotation,rotation),Is.LessThan(.01f));
            Assert.That(Vector3.Distance(cat.transform.position,root),Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(cat.transform.rotation,yaw),Is.LessThan(.2f));
        }
        Time.timeScale=1;int completed=0;Action<CatActivity> done=a=>{if(a==activity)completed++;};CatActivity.Completed+=done;
        try
        {
            deadline=Time.realtimeSinceStartup+20;
            while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(activity.IsRunning,Is.False);Assert.That(completed,Is.EqualTo(1));Assert.That(Count(),Is.GreaterThan(0));
        }
        finally{CatActivity.Completed-=done;}
    }

    [UnityTest,Timeout(180000)] public IEnumerator Bird_StaleCachedZeroDistance_CannotAwardWithoutCurrentHand()=>RejectStale(true);
    [UnityTest,Timeout(180000)] public IEnumerator GroundKnock_StaleCachedZeroDistance_CannotAwardWithoutCurrentHand()=>RejectStale(false);
    IEnumerator RejectStale(bool bird)
    {
        yield return Prepare(bird);
        Assert.That(activity.TryGetPreparedStart(cat,out var start),Is.True);
        var hand=CatBreedVisualFactory.FindDescendant(cat.transform,start.PawPlan.Left?"DEF-hand.L":"DEF-hand.R");
        // Disable only the two IK consumers, retaining the native source pose.
        // Poison the prior-frame cached scalar deliberately; the current real
        // hand must be measured independently rather than trusting that scalar.
        reach.Clear();contact.Clear();reach.enabled=false;contact.enabled=false;
        cat.GetComponent<CatActivityAnimation>().SetTimedPose(CatActivityPose.Scratch,0);
        var distanceField=typeof(CatToyContactMotion).GetField("<Distance>k__BackingField",Private);
        Assert.That(distanceField,Is.Not.Null);
        int samples=0;float nearest=float.PositiveInfinity;float deadline=Time.realtimeSinceStartup+3;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
        {
            distanceField.SetValue(contact,0f);
            yield return new WaitForEndOfFrame();
            float actual=Vector3.Distance(hand.position,start.PawPlan.Target);nearest=Mathf.Min(nearest,actual);samples++;
            // The test is valid only while source alone does not touch. A
            // naturally touching source is not evidence for stale-cache refusal.
            Assert.That(actual,Is.GreaterThan(.025f),"Uncorrected source reached target; choose another existing reachable stance, never alter the limit.");
            Assert.That(Count(),Is.Zero);yield return null;
        }
        Assert.That(samples,Is.GreaterThan(5));Assert.That(activity.IsRunning,Is.False);
        Assert.That(Count(),Is.Zero);Assert.That(Distance(),Is.GreaterThanOrEqualTo(.025f));
        Directory.CreateDirectory("Docs/QA/INTERACTION_POLISH_2026-09-16");
        File.WriteAllText("Docs/QA/INTERACTION_POLISH_2026-09-16/current-frame-"+(bird?"bird":"knock")+".txt","uncorrected samples="+samples+";nearest="+nearest+";credited="+Count());
    }
}
