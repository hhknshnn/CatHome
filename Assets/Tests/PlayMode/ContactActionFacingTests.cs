using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ContactActionFacingTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedRate, savedCapture;
    CatMovement cat;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Run against the isolated QA save.");
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedRate = Time.timeScale; savedCapture = Time.captureDeltaTime;
        Time.timeScale = 1f; Time.captureFramerate = 60;
    }
    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(savedStore); CatBreedService.Select(savedBreed);
        Time.timeScale = savedRate; Time.captureDeltaTime = savedCapture;
    }
    IEnumerator Prepare(string scene, string room)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p => HomeStoreService.IsProductInRoomCollection(room,p.Id)).Select(p => p.Id).ToArray();
        HomeStoreService.ApplySavedState(state); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>(); CatActionState.CancelForTransition(cat);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
    }
    void Start(CatActivity activity, bool requireTravel = false)
    {
        CatActionState.CancelForTransition(cat); RoomPlayModeSupport.ProvisionNeeds();
        var controller = cat.GetComponent<CharacterController>(); controller.enabled = false;
        Vector3 entry = activity.RoutineEntryPoint.position; entry.y = .05f;
        cat.transform.SetPositionAndRotation(entry, Quaternion.Euler(0f, 165f, 0f));
        if (requireTravel)
        {
            bool found = false;
            foreach (float radius in new[] { .32f, .45f, .60f })
            {
                for (int side = 0; side < 24 && !found; side++)
                {
                    float angle = side * 15f * Mathf.Deg2Rad;
                    Vector3 candidate = entry + new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)) * radius;
                    if (!CatActivityMotion.IsFloorClear(candidate) || !CatActivityMotion.ClearSegment(candidate,entry)) continue;
                    cat.transform.position = candidate; Physics.SyncTransforms();
                    found = activity.TryGetPromptDistance(cat,out _);
                }
                if (found) break;
            }
            Assert.That(found,Is.True,activity.StoreProductId+" needs a real clear nearby approach for its travel contract.");
        }
        controller.enabled = true; Physics.SyncTransforms();
        Assert.That(activity.TryStart(cat), Is.True, activity.StoreProductId);
    }
    static Vector3 Flat(Vector3 value) { value.y = 0f; return value; }
    float ViewDot() => CatActivityFacing.FacingDot(cat.transform.forward,cat.transform.position,CatActivityFacing.CameraPosition(cat));

    [UnityTest]
    public IEnumerator Bathroom_RimAndEveryBrushPassTravelForward_AndHamperShowsTheHeldPose()
    {
        yield return Prepare("Bathroom_Level01", HomeRoomService.BathroomId);
        var tub = CatActivity.Registered.OfType<TubEdgeWalkActivity>().Single();
        var brush = CatActivity.Registered.OfType<GroomBrushActivity>().Single();
        var hamper = CatActivity.Registered.OfType<HamperDiveActivity>().Single();
        foreach (CatActivity activity in new CatActivity[] { tub, brush, hamper })
        {
            Start(activity); int heldSamples = 0, passes = 0; bool previousHeld = false;
            Vector3 previous = cat.transform.position;
            float deadline = Time.realtimeSinceStartup + 24f;
            while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame(); if (!activity.IsRunning) break;
                bool held = activity == tub ? tub.IsRimWalking : activity == brush ? brush.IsRubbing : hamper.IsHiding;
                if (held)
                {
                    heldSamples++; if (!previousHeld) passes++;
                    Assert.That(ViewDot(), Is.GreaterThanOrEqualTo(-.01f), activity.StoreProductId + " work direction");
                    if (activity != hamper && previousHeld)
                    {
                        Vector3 travel = Flat(cat.transform.position - previous);
                        if (travel.sqrMagnitude > .0000001f)
                            Assert.That(Vector3.Dot(travel.normalized,Flat(cat.transform.forward).normalized), Is.GreaterThan(.995f), activity.StoreProductId + " moonwalk");
                        Vector3 a = activity == tub ? tub.SelectedRimStart : brush.SelectedRubStart;
                        Vector3 b = activity == tub ? tub.SelectedRimEnd : brush.SelectedRubEnd;
                        Vector3 at = Flat(cat.transform.position - a), line = Flat(b - a);
                        Assert.That(Vector3.Cross(at,line.normalized).magnitude, Is.LessThan(.002f), activity.StoreProductId + " left the real contact line");
                    }
                }
                previousHeld = held; previous = cat.transform.position;
            }
            Assert.That(activity.IsRunning, Is.False); Assert.That(heldSamples, Is.GreaterThan(10));
            Assert.That(passes, Is.EqualTo(activity == brush ? brush.PassCount : 1));
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        }
    }

    [UnityTest]
    public IEnumerator RotatedRoomProps_KeepThePhysicalTargetAndPropAxis_AndWalkInTheTravelDirection()
    {
        string[] scenes = { "Kitchen_Level01", "Bedroom_Level01", "Balcony_Level01" };
        string[] rooms = { HomeRoomService.KitchenId, HomeRoomService.BedroomId, HomeRoomService.BalconyId };
        for (int index = 0; index < scenes.Length; index++)
        {
            yield return Prepare(scenes[index], rooms[index]);
            CatActivity activity = index == 0 ? CatActivity.Registered.OfType<CartNudgeActivity>().Single() :
                index == 1 ? CatActivity.Registered.OfType<KnockOffActivity>().Single() : CatActivity.Registered.OfType<BirdFeederShakeActivity>().Single();
            var cart = activity as CartNudgeActivity; var knock = activity as KnockOffActivity; var feeder = activity as BirdFeederShakeActivity;
            Transform prop = cart != null ? cart.CartVisual : knock != null ? knock.GlassPivot : feeder.FeederPivot;
            Vector3 home = prop.localPosition, target = prop.position, previousProp = prop.position;
            Quaternion rotation = prop.localRotation;
            Start(activity,true); var pose = cat.GetComponent<CatActivityAnimation>();
            Vector3 initial = cat.transform.position;
            var stages = new System.Collections.Generic.Dictionary<CatActivityPose,int>();
            Vector3 previous = cat.transform.position; CatActivityPose previousPose = pose.CurrentPose;
            int workSamples = 0, walkingSamples = 0; float deadline = Time.realtimeSinceStartup + 24f;
            while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame(); if (!activity.IsRunning) break;
                stages.TryGetValue(pose.CurrentPose,out int frames); stages[pose.CurrentPose] = frames + 1;
                Vector3 travel = Flat(cat.transform.position - previous);
                if (pose.CurrentPose == CatActivityPose.Walk && previousPose == CatActivityPose.Walk && travel.sqrMagnitude > .0000001f)
                {
                    walkingSamples++;
                    Assert.That(Vector3.Dot(travel.normalized,Flat(cat.transform.forward).normalized), Is.GreaterThan(.995f), activity.StoreProductId + " moonwalk");
                }
                if (pose.CurrentPose == CatActivityPose.Paw)
                {
                    workSamples++;
                    Vector3 direction = cart != null ? cart.WorldRollDirection : target - cat.transform.position;
                    Assert.That(Vector3.Dot(Flat(direction).normalized,Flat(cat.transform.forward).normalized), Is.GreaterThan(.995f), activity.StoreProductId + " must face the physical target");
                    if (cart == null)
                    {
                        Vector3 stand = knock != null ? knock.ContactStand : feeder.ContactStand;
                        Assert.That(knock != null ? knock.ContactStandBlocked : feeder.ContactStandBlocked, Is.False);
                        Assert.That(CatActivityMotion.IsFloorClear(stand), Is.True, activity.StoreProductId + " physical side stand");
                        Assert.That(CatActivityFacing.FacingDot(target-stand,stand,CatActivityFacing.CameraPosition(cat)), Is.GreaterThanOrEqualTo(.029f));
                    }
                    Vector3 delta = Flat(prop.position - previousProp);
                    if (previousPose == CatActivityPose.Paw && delta.sqrMagnitude > .0000001f && feeder == null)
                    {
                        Vector3 axis = cart != null ? cart.WorldRollDirection : knock.WorldFallDirection;
                        Assert.That(Mathf.Abs(Vector3.Dot(delta.normalized,Flat(axis).normalized)), Is.GreaterThan(.995f), activity.StoreProductId + " mixed local and world axes");
                    }
                }
                previous = cat.transform.position; previousProp = prop.position; previousPose = pose.CurrentPose;
            }
            string evidence = activity.StoreProductId+"/"+activity.Kind+"; start="+initial.ToString("F4")+
                "; entry="+activity.RoutineEntryPoint.position.ToString("F4")+"; final="+cat.transform.position.ToString("F4")+
                "; frames="+string.Join(",",stages.Select(p=>p.Key+":"+p.Value));
            Assert.That(activity.IsRunning, Is.False,evidence); Assert.That(workSamples, Is.GreaterThan(8),evidence);
            Assert.That(walkingSamples, Is.GreaterThan(1),evidence); Assert.That(prop.localPosition, Is.EqualTo(home));
            Assert.That(Quaternion.Angle(prop.localRotation,rotation), Is.LessThan(.001f));
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        }
    }

    [Test]
    public void AuthoredPropDirections_AreRotatedIntoWorldSpace()
    {
        var host = new GameObject("Direction contract"); host.SetActive(false);
        try
        {
            var cart = host.AddComponent<CartNudgeActivity>(); var knock = host.AddComponent<KnockOffActivity>();
            cart.EditorConfigureNudge(null,null,Vector3.right,.3f,2);
            knock.EditorConfigureKnock(null,null,Vector3.forward,.5f,2);
            foreach (float yaw in new[] { 0f, 50f, 90f, 180f, 270f })
            {
                host.transform.rotation = Quaternion.Euler(0f,yaw,0f);
                Assert.That(Vector3.Distance(cart.WorldRollDirection,host.transform.right), Is.LessThan(.0001f));
                Assert.That(Vector3.Distance(knock.WorldFallDirection,host.transform.forward), Is.LessThan(.0001f));
            }
        }
        finally { Object.DestroyImmediate(host); }
    }
}
