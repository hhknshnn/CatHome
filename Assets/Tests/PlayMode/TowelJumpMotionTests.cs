using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TowelJumpMotionTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedTime, savedCapture;
    CatMovement cat;
    TowelNestActivity activity;
    int completions;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedTime = Time.timeScale; savedCapture = Time.captureDeltaTime;
        Time.timeScale = 1f; Time.captureFramerate = 60; CatActivity.Completed += Completed;
    }
    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        CatActivity.Completed -= Completed; RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(savedStore); CatBreedService.Select(savedBreed);
        Time.timeScale = savedTime; Time.captureDeltaTime = savedCapture;
    }
    void Completed(CatActivity a) { if (a == activity) completions++; }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        var state = HomeStoreSaveState.CreateDefault(); state.ownedProductIds = HomeStoreService.BathroomCollection.ToArray();
        HomeStoreService.ApplySavedState(state); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>(); CatActionState.CancelForTransition(cat);
        cat.GetComponent<CatIdleBehavior>().enabled = false;
        activity = CatActivity.Registered.OfType<TowelNestActivity>().Single();
    }
    void Start()
    {
        RoomPlayModeSupport.ProvisionNeeds();
        var cc = cat.GetComponent<CharacterController>(); cc.enabled = false;
        var point = activity.FloorPoint.position; point.y = .05f;
        cat.transform.SetPositionAndRotation(point, Quaternion.identity);
        cc.enabled = true; Physics.SyncTransforms(); completions = 0;
        Assert.That(activity.TryStart(cat), Is.True);
    }
    IEnumerator WaitFor(CatTowelPhase phase)
    {
        float deadline = Time.realtimeSinceStartup + 18f;
        while (activity.Phase != phase && Time.realtimeSinceStartup < deadline)
        {
            if (activity.IsWaitingForRestStop) activity.RequestRestStop();
            Assert.That(activity.IsRunning, Is.True); yield return new WaitForEndOfFrame();
        }
        Assert.That(activity.Phase, Is.EqualTo(phase));
    }

    [UnityTest]
    public IEnumerator TenBreeds_UseNativeUpAndDown_ClearTheCabinet_AndLandOnce()
    {
        yield return Prepare();
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id); yield return null; yield return null;
            var bones = cat.GetComponentsInChildren<Transform>();
            var feet = bones.Where(b => b.name == "DEF-hand.L" || b.name == "DEF-hand.R" || b.name == "DEF-foot.L" || b.name == "DEF-foot.R").ToArray();
            var hips = bones.Single(b => b.name == "DEF-spine");
            var shoulders = bones.Single(b => b.name == "DEF-spine.003");
            var bounds = activity.GetComponentsInChildren<MeshRenderer>().Single(r => r.name.EndsWith("_PremiumModel")).bounds;
            Vector3 scale = cat.transform.localScale; Transform parent = cat.transform.parent;
            bool up = false, down = false, shelf = false, floor = false, rest = false;
            Start(); float deadline = Time.realtimeSinceStartup + 22f;
            while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                Assert.That(cat.transform.localScale, Is.EqualTo(scale), breed.Id + " no root squash");
                var phase = activity.Phase; var pose = cat.GetComponent<CatActivityAnimation>().CurrentPose;
                bool flying = phase == CatTowelPhase.Rising || phase == CatTowelPhase.Descending;
                if (flying)
                {
                    bool rising = phase == CatTowelPhase.Rising; up |= rising; down |= !rising;
                    Assert.That(pose, Is.EqualTo(rising ? CatActivityPose.TowelJumpUp : CatActivityPose.TowelJumpDown));
                    foreach (var foot in feet)
                    {
                        Vector3 p = foot.position;
                        bool inside = p.x > bounds.min.x + .015f && p.x < bounds.max.x - .015f &&
                            p.z > bounds.min.z + .015f && p.z < bounds.max.z - .015f && p.y > .08f && p.y < bounds.max.y - .018f;
                        Assert.That(inside, Is.False, breed.Id + " " + phase + " " + foot.name + " passes through the cabinet: " + p.ToString("F5") + " progress=" + activity.FlightProgress);
                    }
                }
                shelf |= phase == CatTowelPhase.LandOnShelf; floor |= phase == CatTowelPhase.LandOnFloor;
                if (activity.IsWaitingForRestStop)
                {
                    rest = true;
                    Assert.That(CatActivityFacing.FacingDot(shoulders.position - hips.position, (hips.position + shoulders.position) * .5f, CatActivityFacing.CameraPosition(cat)), Is.GreaterThanOrEqualTo(-.01f));
                    Assert.That(Vector3.Distance(cat.transform.position, activity.NestPoint.position), Is.LessThan(.001f));
                    if (activity.RestingSeconds > .55f) activity.RequestRestStop();
                }
            }
            Assert.That(up && down && shelf && floor && rest, Is.True, breed.Id + " all authored phases");
            Assert.That(activity.IsRunning, Is.False); Assert.That(completions, Is.EqualTo(1));
            Assert.That(cat.transform.parent, Is.SameAs(parent));
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True);
        }
    }

    [UnityTest]
    public IEnumerator JumpLandingAndWake_PauseAndCancelWithoutMovingOrReleasingAnotherOwner()
    {
        yield return Prepare();
        foreach (var stage in new[] { CatTowelPhase.Rising, CatTowelPhase.LandOnShelf, CatTowelPhase.Waking, CatTowelPhase.Descending, CatTowelPhase.LandOnFloor })
        {
            Start(); yield return WaitFor(stage);
            Time.timeScale = 0f; yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
            Vector3 position = cat.transform.position;
            var hips = cat.GetComponentsInChildren<Transform>().Single(b => b.name == "DEF-spine");
            Vector3 hip = hips.position;
            for (int frame = 0; frame < 8; frame++) yield return new WaitForEndOfFrame();
            Assert.That(cat.transform.position, Is.EqualTo(position));
            Assert.That(Vector3.Distance(hips.position, hip), Is.LessThan(.0001f), stage + " actual pose must freeze");
            var other = new GameObject("Towel cancellation input owner");
            try
            {
                cat.AcquireInputBlock(other); activity.CancelForTransition();
                Assert.That(cat.HasScopedInputBlock, Is.True);
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
                Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
                Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True);
                Assert.That(completions, Is.Zero);
            }
            finally { cat.ReleaseInputBlock(other); Object.Destroy(other); Time.timeScale = 1f; }
            yield return null;
            Assert.That(activity.GetComponentsInChildren<Transform>().Any(t => t.name == "Towel jump support"), Is.False);
        }
    }
}
