using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Real room/actor and measured current physics. No scene saves or changes to
// source clips, controller dimensions, body catalog or readiness tolerances.
public sealed class PreparedGroundRecoveryTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    PreparedInteractionStartTests fixture;
    CatMovement cat;
    readonly List<GameObject> temporary = new List<GameObject>();
    readonly List<string> evidence = new List<string>();

    [SetUp] public void Before()
    {
        fixture=new PreparedInteractionStartTests();fixture.Before();
        evidence.Clear();evidence.Add("case,detail");
    }
    [TearDown] public void After()
    {
        foreach(var go in temporary)if(go!=null)Object.DestroyImmediate(go);
        temporary.Clear();Physics.SyncTransforms();
        try{fixture?.After();}
        finally{Directory.CreateDirectory(Root);File.WriteAllLines(Root+"/ground-recovery-"+TestContext.CurrentContext.Test.Name+".csv",evidence);}
    }
    object Invoke(string method,params object[] args)=>typeof(PreparedInteractionStartTests).GetMethod(method,Private).Invoke(fixture,args);
    IEnumerator Prepare(string room)
    {
        yield return (IEnumerator)Invoke("Prepare",room);
        cat=(CatMovement)typeof(PreparedInteractionStartTests).GetField("cat",Private).GetValue(fixture);
        Assert.That(cat!=null&&cat.HasBodyGuardProfile,Is.True);
    }
    void Place(Vector3 p,Quaternion q)=>Invoke("Place",p,q);
    bool FindReady(CatActivity a,out CatActivityStart start)
    {
        object[] args={a,default(CatActivityStart),string.Empty};
        bool found=(bool)Invoke("FindReadyPose",args);start=(CatActivityStart)args[1];
        evidence.Add(a.ActivityId+",\""+args[2].ToString().Replace("\"","\"\"")+"\"");return found;
    }
    CatActivity Ready(Func<CatActivity,bool> choose,out CatActivityStart start)
    {
        start=default;
        foreach(var a in CatActivity.Registered.Where(a=>a!=null&&a.gameObject.scene==cat.gameObject.scene&&choose(a)))
            if(FindReady(a,out start))return a;
        Assert.Fail("No genuinely ready source/body/controller stance; see CSV. No radius or pose is calibrated to force success.");return null;
    }
    BoxCollider BoxAt(Vector3 root,Vector3 size)
    {
        var go=new GameObject("QA temporary recovery obstacle");temporary.Add(go);
        SceneManager.MoveGameObjectToScene(go,cat.gameObject.scene);
        go.transform.position=root+Vector3.up*.25f;
        var box=go.AddComponent<BoxCollider>();box.size=size;Physics.SyncTransforms();return box;
    }
    Vector3 OtherClear(CatActivityStart start)
    {
        foreach(float radius in new[]{.8f,1f,1.2f})for(int i=0;i<24;i++)
        {
            var p=start.Position+Quaternion.Euler(0,i*15,0)*Vector3.forward*radius;
            if(cat.IsInteractionPoseClear(p,start.Rotation))return p;
        }
        Assert.Fail("No second measured clear floor within 72 bounded candidates.");return start.Position;
    }
    CatCommandActivity Command()=>cat.GetComponent<CatCommandActivity>()??cat.gameObject.AddComponent<CatCommandActivity>();
    void AssertPose(Vector3 position,Quaternion rotation)
    {
        Assert.That(Vector3.Distance(cat.transform.position,position),Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(cat.transform.rotation,rotation),Is.LessThan(.2f));
    }

    [UnityTest,Timeout(90000)]
    public IEnumerator RaisedAcceptedFloor_ActualJumpCycle_UsesGroundRecoveryWithoutFinalSnap()
    {
        yield return Prepare("LivingRoom_Level01");
        var activity=Ready(a=>a is LivingFurnitureActivity,out var start);
        Vector3 raised=start.Position;raised.y=.13f;Place(raised,start.Rotation);
        Assert.That(activity.TryGetStartPose(cat,out start),Is.True,"The real source and exact controller must accept .13 m.");
        RoomPlayModeSupport.ProvisionNeeds();
        Vector3 completedPosition=default;int completed=0;
        Action<CatActivity> count=a=>{if(a==activity){completed++;completedPosition=cat.transform.position;}};
        CatActivity.Completed+=count;
        try
        {
            Assert.That(activity.TryStart(cat),Is.True);
            Assert.That(CatActivityStartResolver.LandsOnSupport(cat,start.Position),Is.False);
            Assert.That(CatActivityStartResolver.LandsOnSupport(cat,start.ActionTarget),Is.True);
            var driver=cat.GetComponent<CatActivityAnimation>();float began=Time.time;
            int initialFrames=0,descentFrames=0;Vector3 lastNativeGround=default;
            float deadline=Time.realtimeSinceStartup+65;
            while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
            {
                RoomPlayModeSupport.StopObservedRest(activity);
                yield return new WaitForEndOfFrame();
                if(Time.time-began<.30f)
                {AssertPose(start.Position,start.Rotation);Assert.That(driver.IsNativeJump,Is.True);initialFrames++;}
                if(driver.IsNativeJump&&driver.CurrentPose==CatActivityPose.TowelJumpDown)
                {
                    Assert.That(driver.NativeJumpEndOffset.magnitude,Is.LessThan(.00001f),"Ground touchdown cannot retain support centering at Y=.13.");
                    if(driver.NativeJumpPhase>=CatJumpMotion.Touchdown)
                    {descentFrames++;lastNativeGround=cat.transform.position;}
                }
                yield return null;
            }
            Assert.That(activity.IsRunning,Is.False);Assert.That(completed,Is.EqualTo(1));
            Assert.That(initialFrames,Is.GreaterThan(2));Assert.That(descentFrames,Is.GreaterThan(2));
            Assert.That(completedPosition.y,Is.EqualTo(.13f).Within(.001f));
            Assert.That(Vector3.Distance(completedPosition,lastNativeGround),Is.LessThan(.002f),"No cleanup snap after the actual native ground recovery.");
            evidence.Add("cycle,initial="+initialFrames+";descent="+descentFrames+";completed="+completedPosition);
        }
        finally{CatActivity.Completed-=count;}
    }

    [UnityTest,Timeout(90000)]
    public IEnumerator RaisedPlanterCleanup_PreservesMeasuredCompletedGroundPose()
    {
        yield return Prepare("Patio_Level01");
        var activity=(LitterDigActivity)Ready(a=>a is LitterDigActivity&&a.StoreProductId==HomeStoreService.PatioHerbTroughId,out var start);
        Vector3 raised=start.Position;raised.y=.13f;Place(raised,start.Rotation);
        Assert.That(activity.TryGetStartPose(cat,out start),Is.True);
        RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        // Exercise the real cleanup seam deterministically, not an invented
        // completion/PASS. The separate test above observes a complete jump.
        activity.StopAllCoroutines();
        Vector3 finish=OtherClear(start);Place(finish,start.Rotation);
        Assert.That(cat.IsInteractionPoseClear(finish,start.Rotation),Is.True);
        typeof(LitterDigActivity).GetMethod("RestoreCat",Private).Invoke(activity,null);
        AssertPose(finish,start.Rotation);
        evidence.Add("planter-cleanup,clear .13 floor retained="+finish);
    }

    [UnityTest,Timeout(90000)]
    public IEnumerator OccupiedAcceptedFloor_KeepsCurrentClearFloor_OnCancelAndNormalCompletion()
    {
        yield return Prepare("LivingRoom_Level01");Ready(a=>a is LivingFurnitureActivity,out var start);
        var current=OtherClear(start);
        foreach(bool cancel in new[]{true,false})
        {
            Place(start.Position,start.Rotation);var command=Command();int completions=0;
            Action<CatActivity> complete=a=>{if(a==command)completions++;};CatActivity.Completed+=complete;
            try
            {
            Assert.That(command.Issue(CatCompanionCommand.Meow),Is.True);
            Place(current,start.Rotation);var obstacle=BoxAt(start.Position,new Vector3(.36f,.65f,.36f));
            Assert.That(cat.IsInteractionPoseClear(start.Position,start.Rotation),Is.False);
            Assert.That(cat.IsInteractionPoseClear(current,start.Rotation),Is.True);
            if(cancel)command.CancelForTransition();
            else
            {
                float deadline=Time.realtimeSinceStartup+7;
                while(command.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.That(command.IsRunning,Is.False);
            }
            AssertPose(current,start.Rotation);Assert.That(cat.IsInteractionPoseClear(current,start.Rotation),Is.True);
            Assert.That(completions,Is.EqualTo(cancel?0:1),"Exercise cancellation and a real normal completion separately.");
            Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
            evidence.Add("occupied-old-floor,"+(cancel?"cancel":"normal")+" kept="+current);
            Object.DestroyImmediate(obstacle.gameObject);Physics.SyncTransforms();
            }
            finally{CatActivity.Completed-=complete;}
        }
    }

    [UnityTest,Timeout(90000)]
    public IEnumerator OccupiedAcceptedFloor_AirborneCancel_UsesOnlyClearSameXZFloorProjection()
    {
        yield return Prepare("LivingRoom_Level01");Ready(a=>a is LivingFurnitureActivity,out var start);
        var floor=OtherClear(start);Place(start.Position,start.Rotation);
        var command=Command();Assert.That(command.Issue(CatCompanionCommand.Meow),Is.True);
        BoxAt(start.Position,new Vector3(.36f,.65f,.36f));
        Place(floor+Vector3.up*.65f,start.Rotation);
        Assert.That(cat.IsInteractionPoseClear(start.Position,start.Rotation),Is.False);
        Assert.That(cat.IsInteractionPoseClear(floor,start.Rotation),Is.True);
        command.CancelForTransition();AssertPose(floor,start.Rotation);
        evidence.Add("projected-floor,same XZ floor="+floor);
    }

    [UnityTest,Timeout(90000)]
    public IEnumerator AllRecoveryChoicesBlocked_DoesNotInventOrWriteAnotherPose()
    {
        yield return Prepare("LivingRoom_Level01");Ready(a=>a is LivingFurnitureActivity,out var start);
        Place(start.Position,start.Rotation);var command=Command();Assert.That(command.Issue(CatCompanionCommand.Meow),Is.True);
        Vector3 current=start.Position+Vector3.right*.10f;Place(current,start.Rotation);
        BoxAt(start.Position,new Vector3(.9f,.8f,.9f));
        Assert.That(cat.IsInteractionPoseClear(start.Position,start.Rotation),Is.False);
        Assert.That(cat.IsInteractionPoseClear(current,start.Rotation),Is.False);
        Assert.That(CatActivityStartResolver.TryRecovery(cat,start,true,out _),Is.False);
        command.CancelForTransition();AssertPose(current,start.Rotation);
        Assert.That(command.IsRunning||cat.IsMovementPhysicallyLocked,Is.False);
        Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
        evidence.Add("all-blocked,no forced transform; normal body guard/gravity owns subsequent escape");
    }
}
