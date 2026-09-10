using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Observe real work poses after staging, including pause and the rendered support axis.</summary>
public sealed class ActivityFacingStagingTests
{
    private HomeStoreSaveState savedStore;
    private string savedBreed;
    private float savedTimeScale, savedCaptureDelta;
    private bool savedSound;
    private CatMovement cat;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use the isolated QA session for room activity tests.");
        savedStore = HomeStoreService.CaptureState();
        savedBreed = CatBreedService.SelectedBreedId;
        savedTimeScale = Time.timeScale;
        savedCaptureDelta = Time.captureDeltaTime;
        savedSound = HomeAudioService.SoundEnabled;
        HomeAudioService.SoundEnabled = false;
        Time.timeScale = 1f;
        Time.captureFramerate = 60;
    }

    [TearDown]
    public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        RoomPlayModeSupport.ReleaseRoom();
        CatBreedService.Select(savedBreed);
        HomeStoreService.ApplySavedState(savedStore);
        HomeAudioService.SoundEnabled = savedSound;
        Time.timeScale = savedTimeScale;
        Time.captureDeltaTime = savedCaptureDelta;
    }

    private IEnumerator Prepare(string sceneName, string roomId)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(sceneName);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products
            .Where(p => HomeStoreService.IsProductInRoomCollection(roomId, p.Id)).Select(p => p.Id).ToArray();
        HomeStoreService.ApplySavedState(state);
        yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(cat, Is.Not.Null);
        CatActionState.CancelForTransition(cat);
        var idle = cat.GetComponent<CatIdleBehavior>();
        if (idle != null) idle.enabled = false;
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
    }

    private void PlaceOnFloor(Vector3 point)
    {
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        point.y = .05f;
        Vector3 away = point - Camera.main.transform.position;
        away.y = 0f;
        cat.transform.SetPositionAndRotation(point, Quaternion.LookRotation(away));
        controller.enabled = true;
        Physics.SyncTransforms();
        RoomPlayModeSupport.ProvisionNeeds();
    }

    private CatActivity Start<T>() where T : CatActivity
    {
        var activity = CatActivity.Registered.OfType<T>().Single(a => a.isActiveAndEnabled && a.IsUnlocked);
        CatActionState.CancelForTransition(cat);
        PlaceOnFloor(activity.RoutineEntryPoint.position);
        Assert.That(activity.TryStart(cat), Is.True, activity.StoreProductId);
        return activity;
    }

    private static IEnumerator WaitForRest(CatActivity activity)
    {
        float deadline = Time.realtimeSinceStartup + 20f;
        while (!activity.IsWaitingForRestStop && Time.realtimeSinceStartup < deadline)
        {
            Assert.That(activity.IsRunning, Is.True, activity.StoreProductId + " ended before resting.");
            yield return null;
        }
        Assert.That(activity.IsWaitingForRestStop, Is.True, activity.StoreProductId);
        yield return new WaitForSeconds(.15f);
        yield return new WaitForEndOfFrame();
    }

    private static float FacingDot(Vector3 position, Vector3 forward)
    {
        Vector3 view = Camera.main.transform.position - position;
        view.y = forward.y = 0f;
        return Vector3.Dot(view.normalized, forward.normalized);
    }

    [UnityTest]
    public IEnumerator Commands_TurnBeforeTheHeldPose_AndPauseOrCancellationStopsTheTurn()
    {
        yield return Prepare("LivingRoom_Level01", HomeRoomService.LivingRoomId);
        var command = cat.GetComponent<CatCommandActivity>() ?? cat.gameObject.AddComponent<CatCommandActivity>();
        foreach (var kind in new[] { CatCompanionCommand.Sit, CatCompanionCommand.Loaf, CatCompanionCommand.Meow })
        {
            PlaceOnFloor(new Vector3(-.5f, .05f, -1.4f));
            Vector3 start = cat.transform.position, scale = cat.transform.localScale;
            Transform parent = cat.transform.parent;
            Assert.That(FacingDot(start, cat.transform.forward), Is.LessThan(-.95f));
            Assert.That(command.Issue(kind), Is.True, kind.ToString());
            yield return null;
            Time.timeScale = 0f;
            yield return new WaitForEndOfFrame();
            Quaternion pausedRotation = cat.transform.rotation;
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(Quaternion.Angle(cat.transform.rotation, pausedRotation), Is.LessThan(.01f), "Pause freezes the entry turn.");
            Time.timeScale = 1f;
            if (kind == CatCompanionCommand.Meow)
            {
                float deadline = Time.realtimeSinceStartup + 5f;
                while (cat.GetComponent<CatActivityAnimation>().CurrentPose != CatActivityPose.Meow && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(cat.GetComponent<CatActivityAnimation>().CurrentPose, Is.EqualTo(CatActivityPose.Meow));
                yield return new WaitForSeconds(.1f);
                yield return new WaitForEndOfFrame();
            }
            else yield return WaitForRest(command);
            Assert.That(FacingDot(cat.transform.position, cat.transform.forward), Is.GreaterThanOrEqualTo(-.01f), kind.ToString());
            Assert.That(Vector3.Distance(cat.transform.position, start), Is.LessThan(.01f), "A presentation turn cannot relocate the cat.");
            Assert.That(cat.transform.localScale, Is.EqualTo(scale));
            Assert.That(cat.transform.parent, Is.SameAs(parent));
            command.CancelForTransition();
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Quaternion cancelledRotation = cat.transform.rotation;
            yield return new WaitForSeconds(.1f);
            Assert.That(Quaternion.Angle(cat.transform.rotation, cancelledRotation), Is.LessThan(.01f), "The cancelled stage cannot keep rotating.");
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        }
    }

    [UnityTest]
    public IEnumerator MatAndWarmth_KeepTheirWorkFootprint_AndShowTheHeldPose()
    {
        yield return Prepare("Bathroom_Level01", HomeRoomService.BathroomId);
        CatActivity mat = Start<MatKneadActivity>();
        float deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            var knead = cat.GetComponent<CatGentleKneadMotion>();
            if (knead != null && knead.IsActive) break;
        }
        Assert.That(cat.GetComponent<CatGentleKneadMotion>()?.IsActive, Is.True, "The real paw work must start after staging.");
        Assert.That(FacingDot(cat.transform.position, cat.transform.forward), Is.GreaterThanOrEqualTo(-.01f));
        Vector3 workPosition = cat.transform.position;
        yield return new WaitForSeconds(.2f);
        Assert.That(Vector3.Distance(cat.transform.position, workPosition), Is.LessThan(.005f));
        mat.CancelForTransition();

        yield return Prepare("Kitchen_Level01", HomeRoomService.KitchenId);
        CatActivity warmth = Start<OvenWarmthActivity>();
        yield return WaitForRest(warmth);
        Assert.That(FacingDot(cat.transform.position, cat.transform.forward), Is.GreaterThanOrEqualTo(-.01f));
        var ovenDoor = (Transform)typeof(OvenWarmthActivity).GetField("doorPoint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(warmth);
        Vector3 towardOven = ovenDoor.position - cat.transform.position;
        towardOven.y = 0f;
        Assert.That(Mathf.Abs(Vector3.Dot(towardOven.normalized, cat.transform.forward)), Is.LessThan(.20f),
            "The cat still warms its flank; facing the viewer cannot turn its nose into the oven.");
        Assert.That(warmth.RequestRestStop(), Is.True);
        deadline = Time.realtimeSinceStartup + 8f;
        while (warmth.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(warmth.IsRunning, Is.False);
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True);
    }

    [UnityTest]
    public IEnumerator QuietGaze_UsesAnOpenViewingSide_AndKeepsLookingAtTheRealObject()
    {
        yield return Prepare("Garden_Level01", HomeRoomService.GardenId);
        var activity = CatActivity.Registered.OfType<SitLookActivity>()
            .Single(a => a.StoreProductId == HomeStoreService.GardenGrillId);
        PlaceOnFloor(activity.RoutineEntryPoint.position);
        Assert.That(activity.TryStart(cat), Is.True);
        float deadline = Time.realtimeSinceStartup + 12f;
        while (activity.GestureBeats == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(activity.GestureBeats, Is.GreaterThan(0));
        yield return new WaitForSeconds(.1f);
        yield return new WaitForEndOfFrame();
        Assert.That(activity.ViewStandBlocked, Is.False, "The authored grill bay must leave a legal front or side view.");
        Assert.That(FacingDot(cat.transform.position, cat.transform.forward), Is.GreaterThanOrEqualTo(-.01f));
        Vector3 target = activity.LookPoint.position - cat.transform.position;
        target.y = 0f;
        Assert.That(Vector3.Angle(cat.transform.forward, target), Is.LessThan(1f),
            "Moving to the viewing side must keep the real grill ahead, without a 90-degree neck twist.");
        Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .27f), Is.True);
        activity.CancelForTransition();
        Assert.That(cat.GetComponent<CatFurnitureGaze>().Deflection, Is.Zero);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
    }

    [UnityTest]
    public IEnumerator NarrowRests_ShowTheRenderedBodyAxis_AndKeepTheMovingSeatDuringPauseAndBreedSwap()
    {
        foreach (bool hammock in new[] { false, true })
        {
            yield return Prepare(hammock ? "Garden_Level01" : "Bathroom_Level01",
                hammock ? HomeRoomService.GardenId : HomeRoomService.BathroomId);
            CatActivity activity = hammock ? Start<SwingRideActivity>() : Start<TowelNestActivity>();
            yield return WaitForRest(activity);
            foreach (string breed in new[] { "domestic-shorthair", "maine-coon" })
            {
                Assert.That(CatBreedService.Select(breed), Is.True);
                yield return new WaitForSeconds(.2f);
                yield return new WaitForEndOfFrame();
                var animation = cat.GetComponent<CatActivityAnimation>();
                var surface = animation.ContactSurface;
                Assert.That(surface, Is.Not.Null);
                Assert.That(surface.GetComponent<CatActivitySurface>().AlignAlongSurface, Is.True);
                var bones = cat.GetComponentsInChildren<Transform>();
                Transform hips = bones.Single(b => b.name == "DEF-spine");
                Transform shoulders = bones.Single(b => b.name == "DEF-spine.003");
                Vector3 bodyDirection = shoulders.position - hips.position;
                Assert.That(FacingDot(hips.position, bodyDirection), Is.GreaterThanOrEqualTo(-.05f),
                    activity.StoreProductId + "/" + breed + " must show the rendered cat, not just a correctly rotated empty root.");
                Assert.That(Vector3.Distance(cat.transform.position, surface.position), Is.LessThan(.025f));
            }
            Time.timeScale = 0f;
            yield return new WaitForEndOfFrame();
            Vector3 pausedPosition = cat.transform.position;
            Quaternion pausedRotation = cat.transform.rotation;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Vector3.Distance(cat.transform.position, pausedPosition), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(cat.transform.rotation, pausedRotation), Is.LessThan(.01f));
            Time.timeScale = 1f;
            activity.CancelForTransition();
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        }
    }
}
