using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GardenGentleInteractionTests
{
    private HomeStoreSaveState saved;

    [SetUp]
    public void Before() => saved = HomeStoreService.CaptureState();

    [TearDown]
    public void After()
    {
        CatActionState.CancelForTransition(Object.FindAnyObjectByType<CatMovement>());
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(saved);
    }

    private static IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Garden_Level01");
        var owned = HomeStoreSaveState.CreateDefault();
        owned.ownedProductIds = new[] { HomeStoreService.GardenDaisyBedId, HomeStoreService.GardenGrillId };
        HomeStoreService.ApplySavedState(owned);
        yield return null;
        yield return null;
    }

    private static void MoveToEntry(CatMovement cat, CatActivity activity)
    {
        CatActionState.CancelForTransition(cat);
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        Vector3 point = activity.RoutineEntryPoint.position;
        point.y = .05f;
        cat.transform.position = point;
        controller.enabled = true;
        Physics.SyncTransforms();
        RoomPlayModeSupport.ProvisionNeeds();
    }

    [UnityTest]
    public IEnumerator Daisy_UsesSlowAlternatingPaws_WithoutRootBounce_AndCleansUpOnCancel()
    {
        yield return Prepare();
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var daisy = CatActivity.Registered.OfType<MatKneadActivity>()
            .Single(a => a.Kind == CatActivityKind.DaisyRoll);
        MoveToEntry(cat, daisy);
        Vector3 scale = cat.transform.localScale;
        Assert.That(daisy.TryStart(cat), Is.True);
        CatGentleKneadMotion knead = null;
        float deadline = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < deadline)
        {
            knead = cat.GetComponent<CatGentleKneadMotion>();
            if (knead != null && knead.IsActive) break;
            yield return null;
        }
        Assert.That(knead != null && knead.IsActive, Is.True);
        var pose = cat.GetComponent<CatActivityAnimation>();
        var contact = cat.GetComponent<CatToyContactMotion>();
        Vector3 padPosition = cat.transform.position;
        Quaternion padRotation = cat.transform.rotation;
        bool liftedLeft = false, liftedRight = false;
        float firstLeft = -1f, firstRight = -1f;
        while (knead.IsActive && Time.realtimeSinceStartup < deadline)
        {
            Assert.That(pose.CurrentPose, Is.EqualTo(CatActivityPose.GentleKnead));
            Assert.That(CatActivityAnimation.StateFor(pose.CurrentPose), Is.EqualTo("Idle"),
                "Kneading must not play the hard paw-swat clip.");
            Assert.That(Vector3.Distance(cat.transform.position, padPosition), Is.LessThan(.002f));
            Assert.That(Quaternion.Angle(cat.transform.rotation, padRotation), Is.LessThan(.5f));
            Assert.That(Vector3.Distance(cat.transform.localScale, scale), Is.LessThan(.0001f));
            Assert.That(knead.LeftLift, Is.InRange(0f, .0241f));
            Assert.That(knead.RightLift, Is.InRange(0f, .0241f));
            Assert.That(Mathf.Min(knead.LeftLift, knead.RightLift), Is.EqualTo(0f),
                "One front paw stays on the pad while the other lifts.");
            if (knead.LeftLift > .014f)
            {
                liftedLeft = true;
                if (firstLeft < 0f) firstLeft = Time.time;
                Assert.That(contact.LeftDistance, Is.LessThan(.012f), "The actual left paw reaches its gentle target.");
            }
            if (knead.RightLift > .014f)
            {
                liftedRight = true;
                if (firstRight < 0f) firstRight = Time.time;
                Assert.That(contact.RightDistance, Is.LessThan(.012f), "The actual right paw reaches its gentle target.");
            }
            if (liftedLeft && liftedRight) break;
            yield return null;
        }
        Assert.That(liftedLeft && liftedRight, Is.True);
        Assert.That(firstRight - firstLeft, Is.InRange(.8f, 1.3f),
            "Alternating presses need a calm cadence, not repeated fast strikes.");
        daisy.CancelForTransition();
        Assert.That(knead.IsActive, Is.False);
        Assert.That(contact.LeftDistance, Is.EqualTo(float.PositiveInfinity));
        Assert.That(contact.RightDistance, Is.EqualTo(float.PositiveInfinity));
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Vector3 cancelledAt = cat.transform.position;
        yield return null;
        yield return null;
        Assert.That(Vector3.ProjectOnPlane(cat.transform.position - cancelledAt, Vector3.up).magnitude,
            Is.LessThan(.003f), "No abandoned knead coroutine may keep moving the cat.");
    }

    [UnityTest]
    public IEnumerator Grill_UsesSeatedGazeWithoutPawSwats()
    {
        yield return Prepare();
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var grill = CatActivity.Registered.OfType<SitLookActivity>()
            .Single(a => a.Kind == CatActivityKind.GrillWatch);
        Assert.That(grill.ReactionKind, Is.EqualTo(SitLookReaction.Sit));
        MoveToEntry(cat, grill);
        Assert.That(grill.TryStart(cat), Is.True);
        var pose = cat.GetComponent<CatActivityAnimation>();
        float deadline = Time.realtimeSinceStartup + 15f;
        bool sat = false;
        while (grill.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            if (grill.GestureBeats > 0)
            {
                sat = true;
                Assert.That(pose.CurrentPose, Is.EqualTo(CatActivityPose.Sit));
            }
            yield return null;
        }
        Assert.That(sat, Is.True);
        Assert.That(grill.IsRunning, Is.False);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
    }
}
