using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ChaseContactFacingTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedTime, savedCapture;
    bool savedReduced;
    CatMovement cat;
    GameObject ownerHost;
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        savedStore=HomeStoreService.CaptureState(); savedBreed=CatBreedService.SelectedBreedId;
        savedTime=Time.timeScale; savedCapture=Time.captureDeltaTime; savedReduced=CatRunnerProgressService.ReducedMotion;
        Time.timeScale=1f; Time.captureFramerate=60; CatRunnerProgressService.SetReducedMotion(false);
    }
    [TearDown] public void After()
    {
        if(cat!=null)CatActionState.CancelForTransition(cat);
        if(ownerHost!=null)Object.DestroyImmediate(ownerHost);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(savedStore);
        CatBreedService.Select(savedBreed); CatRunnerProgressService.SetReducedMotion(savedReduced);
        Time.timeScale=savedTime; Time.captureDeltaTime=savedCapture;
    }
    IEnumerator Prepare(bool garden)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(garden?"Garden_Level01":"LivingRoom_Level01");
        var store=HomeStoreSaveState.CreateDefault();
        store.ownedProductIds=garden?HomeStoreService.GardenCollection.ToArray():HomeStoreService.LivingRoomCollection.Concat(new[]{HomeStoreService.BallBasketId}).ToArray();
        HomeStoreService.ApplySavedState(store); yield return null; yield return null;
        cat=Object.FindAnyObjectByType<CatMovement>();
        var idle=cat.GetComponent<CatIdleBehavior>(); if(idle!=null)idle.enabled=false;
    }
    void Ready(CatActivity activity)
    {
        CatActionState.CancelForTransition(cat); RoomPlayModeSupport.ProvisionNeeds();
        Vector3 point=activity.RoutineEntryPoint.position; point.y=.05f;
        cat.ApplySavedWorldPose(point,Quaternion.identity); Physics.SyncTransforms();
    }
    void Facing(string label)
    {
        var bones=cat.GetComponentsInChildren<Transform>();
        Vector3 shoulders=bones.Single(t=>t.name=="DEF-spine.003").position, hips=bones.Single(t=>t.name=="DEF-spine").position;
        Assert.That(CatActivityFacing.FacingDot(shoulders-hips,(shoulders+hips)*.5f,CatActivityFacing.CameraPosition(cat)),
            Is.GreaterThanOrEqualTo(CatActivityFacing.MinimumViewDot),label);
    }
    [UnityTest] public IEnumerator BasketAndGarden_TenBreeds_CatchOnlyWithActualPaws_AndHoldVisiblePoses()
    {
        foreach(bool garden in new[]{false,true})
        {
            yield return Prepare(garden);
            var activities=(garden?CatActivity.Registered.OfType<GardenYarnChaseActivity>().Where(a=>a.StoreProductId==HomeStoreService.GardenYarnBallId).Cast<CatActivity>():
                CatActivity.Registered.OfType<BallChaseActivity>().Where(a=>a.StoreProductId==HomeStoreService.BallBasketId).Cast<CatActivity>())
                .Where(a=>a.gameObject.scene==cat.gameObject.scene).OrderBy(a=>a.ActivityId).ToArray();
            Assert.That(activities,Is.Not.Empty,"The current room must contain its real owned chase stations.");
            foreach(var activity in activities)
            foreach(var breed in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(breed.Id); yield return null; yield return null;
                Ready(activity); int observed=0,held=0;
                Vector3 previousPosition=cat.transform.position;
                Quaternion previousRotation=cat.transform.rotation;
                CatActivityPose previousPose=CatActivityPose.Walk;
                float stillSeconds=0f;
                Assert.That(activity.TryStart(cat),Is.True,breed.Id+" "+activity.ActivityId+" "+activity.transform.position);
                float deadline=Time.realtimeSinceStartup+30f;
                while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();
                    var currentPose=cat.GetComponent<CatActivityAnimation>().CurrentPose;
                    if(currentPose!=previousPose || Vector3.Distance(previousPosition,cat.transform.position)>.002f ||
                        Quaternion.Angle(previousRotation,cat.transform.rotation)>.5f)stillSeconds=0f;
                    else stillSeconds+=Time.deltaTime;
                    previousPosition=cat.transform.position;previousRotation=cat.transform.rotation;previousPose=currentPose;
                    if(activity.IsRunning&&currentPose==CatActivityPose.Sniff&&stillSeconds>=.30f)
                        Facing(breed.Id+" settled intro/contact wait");
                    int catches=garden?((GardenYarnChaseActivity)activity).CatchCount:((BallChaseActivity)activity).CatchCount;
                    if(catches>observed)
                    {
                        Assert.That(catches,Is.EqualTo(observed+1)); observed=catches;
                        Assert.That(garden?((GardenYarnChaseActivity)activity).LastHitDistance:((BallChaseActivity)activity).LastHitDistance,Is.LessThan(.095f));
                        Facing(breed.Id+" actual paw contact");
                    }
                    if(activity.IsRunning&&cat.GetComponent<CatActivityAnimation>().CurrentPose==CatActivityPose.Sit)
                    {Facing(breed.Id+" held pose");held++;}
                }
                Assert.That(activity.IsRunning,Is.False,"timeout");
                Assert.That(observed,Is.EqualTo(garden?((GardenYarnChaseActivity)activity).CatchGoal:((BallChaseActivity)activity).CatchGoal),
                    garden?((GardenYarnChaseActivity)activity).InterruptedReason:((BallChaseActivity)activity).InterruptedReason);
                Assert.That(held,Is.GreaterThan(0));
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(cat.IsMovementPhysicallyLocked,Is.False); Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
            }
        }
    }
    [UnityTest] public IEnumerator Garden_PauseAndDistantPawTarget_DoNotCountCatches_AndReleaseQueryScene()
    {
        yield return Prepare(true);
        var activities=CatActivity.Registered.OfType<GardenYarnChaseActivity>().Where(a=>a.StoreProductId==HomeStoreService.GardenYarnBallId&&a.gameObject.scene==cat.gameObject.scene).OrderBy(a=>a.ActivityId).ToArray();
        Assert.That(activities,Is.Not.Empty);
        foreach(var activity in activities)
        {
        Ready(activity); int scenes=SceneManager.sceneCount; CatRunnerProgressService.SetReducedMotion(true);
        Assert.That(activity.TryStart(cat),Is.True);
        Time.timeScale=0f; Vector3 before=activity.YarnBall.position;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(activity.CatchCount,Is.Zero); Assert.That(activity.YarnBall.position,Is.EqualTo(before));
        Time.timeScale=1f;
        float deadline=Time.realtimeSinceStartup+15f;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline&&cat.GetComponent<CatActivityAnimation>().CurrentPose!=CatActivityPose.BatLeft)yield return null;
        Assert.That(activity.IsRunning,Is.True); Assert.That(activity.CatchCount,Is.Zero);
        activity.YarnBall.position+=Vector3.right*50f;
        while(activity.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(activity.IsRunning,Is.False); Assert.That(activity.CatchCount,Is.Zero,"An unreachable paw must never receive a catch or completion.");
        Assert.That(activity.YarnBall.gameObject.activeSelf,Is.False);
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked,Is.False); Assert.That(cat.GetComponent<CharacterController>().enabled,Is.True);
        deadline=Time.realtimeSinceStartup+3f;
        while(SceneManager.sceneCount>scenes&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(SceneManager.sceneCount,Is.EqualTo(scenes));
        }
    }
    [UnityTest] public IEnumerator ExpiredReaction_CannotReplaceTheActiveOwnersWalkPose()
    {
        yield return Prepare(true);
        ownerHost=new GameObject("Reaction pose ownership fixture");
        ownerHost.transform.position=cat.transform.position;
        var owner=ownerHost.AddComponent<ChaseReactionPoseOwner>();
        owner.EditorConfigure("reaction-pose-qa","QA",CatActivityKind.BallChase,QuestType.PlayBall,0,"QA",1f,0f,ownerHost.transform,null,null);
        RoomPlayModeSupport.ProvisionNeeds(); Assert.That(owner.TryStart(cat),Is.True);
        var reaction=cat.GetComponent<CatActivityReaction>()??cat.gameObject.AddComponent<CatActivityReaction>();
        reaction.PlayPounceReaction(); cat.GetComponent<CatActivityAnimation>().SetPose(CatActivityPose.Walk);
        yield return new WaitForSeconds(.85f);
        Assert.That(reaction.IsReacting,Is.False);
        Assert.That(cat.GetComponent<CatActivityAnimation>().CurrentPose,Is.EqualTo(CatActivityPose.Walk));
        Assert.That(cat.IsMovementPhysicallyLocked,Is.True,"Only the reaction's own lock may be released.");
        owner.CancelForTransition(); Assert.That(cat.IsMovementPhysicallyLocked,Is.False);
    }
}

public sealed class ChaseReactionPoseOwner : CatActivity
{
    protected override bool BeginActivity(){Cat.SetMovementLocked(this,true);PlayCatPose(CatActivityPose.Walk);return true;}
}
