using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BathroomGentleActivityTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedTimeScale, savedCaptureDelta;
    CatMovement cat;
    CatActivity tracked;
    int completed;

    [SetUp] public void Before()
    {
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedTimeScale = Time.timeScale; savedCaptureDelta = Time.captureDeltaTime;
        Time.timeScale = 1f; Time.captureFramerate = 60;
        CatActivity.Completed += OnCompleted;
    }

    [TearDown] public void After()
    {
        if (cat != null) { CatActionState.CancelForTransition(cat); cat.enabled = true; }
        CatActivity.Completed -= OnCompleted;
        CatBreedService.Select(savedBreed); RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(savedStore);
        Time.timeScale = savedTimeScale; Time.captureDeltaTime = savedCaptureDelta;
    }

    void OnCompleted(CatActivity activity) { if (activity == tracked) completed++; }

    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.BathroomCollection.ToArray();
        HomeStoreService.ApplySavedState(state);
        yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>();
        CatActionState.CancelForTransition(cat);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
    }

    CatActivity[] Activities() => new CatActivity[] {
        CatActivity.Registered.OfType<MatKneadActivity>().Single(a => a.StoreProductId == HomeStoreService.BathroomBathMatId)
    };

    void Start(CatActivity activity)
    {
        CatActionState.CancelForTransition(cat); cat.enabled = true;
        RoomPlayModeSupport.ProvisionNeeds();
        var controller = cat.GetComponent<CharacterController>(); controller.enabled = false;
        Vector3 point = activity.RoutineEntryPoint.position; point.y = .05f;
        cat.transform.SetPositionAndRotation(point, Quaternion.identity);
        controller.enabled = true; Physics.SyncTransforms();
        tracked = activity; completed = 0;
        Assert.That(activity.TryStart(cat), Is.True, activity.StoreProductId + " authored entry must be accessible.");
    }

    IEnumerator WaitForGentle(CatActivity activity)
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            var gentle = cat.GetComponent<CatGentleKneadMotion>();
            if (gentle != null && gentle.IsActive) yield break;
            Assert.That(activity.IsRunning, Is.True, activity.StoreProductId + " ended before its paw gesture.");
        }
        Assert.Fail(activity.StoreProductId + " did not reach the gentle paw phase.");
    }

    [UnityTest]
    public IEnumerator TenBreeds_BathMatPawsAlternateWithRealContact_AndCancellationStopsThem()
    {
        yield return Prepare();
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            Assert.That(CatBreedService.Select(breed.Id), Is.True);
            yield return null; yield return null;
            foreach (var activity in Activities())
            {
                Start(activity); yield return WaitForGentle(activity);
                var motion = cat.GetComponent<CatGentleKneadMotion>();
                var pose = cat.GetComponent<CatActivityAnimation>();
                var contact = cat.GetComponent<CatToyContactMotion>();
                var animator = cat.GetComponentInChildren<Animator>();
                Vector3 position = cat.transform.position, scale = cat.transform.localScale;
                Quaternion rotation = cat.transform.rotation;
                bool left = false, right = false;
                float firstLeft = -1f, firstRight = -1f;
                float deadline = Time.time + 2.15f;
                string context = breed.Id + " / " + activity.StoreProductId;
                while (Time.time < deadline)
                {
                    Assert.That(motion.IsActive, Is.True, context);
                    Assert.That(motion.IsScraping, Is.False, context);
                    Assert.That(pose.CurrentPose, Is.EqualTo(CatActivityPose.GentleKnead), context);
                    Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), Is.True, context + " must not play PawSwat.");
                    Assert.That(Vector3.Distance(cat.transform.position, position), Is.LessThan(.002f), context + " torso pumping");
                    Assert.That(Quaternion.Angle(cat.transform.rotation, rotation), Is.LessThan(.5f), context + " torso rocking");
                    Assert.That(Vector3.Distance(cat.transform.localScale, scale), Is.LessThan(.0001f), context + " root squash");
                    float limit = CatGentleKneadMotion.MaximumLift;
                    Assert.That(motion.LeftLift, Is.InRange(0f, limit + .0001f));
                    Assert.That(motion.RightLift, Is.InRange(0f, limit + .0001f));
                    Assert.That(Mathf.Min(motion.LeftLift, motion.RightLift), Is.Zero, context + " one paw must remain planted");
                    if (motion.LeftLift > .006f)
                    {
                        left = true; if (firstLeft < 0f) firstLeft = Time.time;
                        Assert.That(contact.LeftDistance, Is.LessThan(.012f), context + " actual left contact");
                    }
                    if (motion.RightLift > .006f)
                    {
                        right = true; if (firstRight < 0f) firstRight = Time.time;
                        Assert.That(contact.RightDistance, Is.LessThan(.012f), context + " actual right contact");
                    }
                    Assert.That(Mathf.Abs(motion.LeftSweep) + Mathf.Abs(motion.RightSweep), Is.Zero,
                        context + " a pad press must not inherit the previous litter sweep");
                    yield return new WaitForEndOfFrame();
                }
                Assert.That(left && right, Is.True, context);
                Assert.That(firstRight - firstLeft, Is.InRange(.7f, 1.2f), context + " calm alternating tempo");
                activity.CancelForTransition();
                Assert.That(motion.IsActive || motion.IsScraping, Is.False, context);
                Assert.That(contact.LeftDistance, Is.EqualTo(float.PositiveInfinity));
                Assert.That(contact.RightDistance, Is.EqualTo(float.PositiveInfinity));
                Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True, context);
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False, context);
                Assert.That(completed, Is.Zero, context + " cancellation must not award completion");
                // Freeze normal locomotion to isolate an abandoned coroutine
                // from the controller's independent floor depenetration step.
                cat.enabled = false; Vector3 cancelledAt = cat.transform.position;
                for (int frame = 0; frame < 12; frame++) yield return new WaitForEndOfFrame();
                Assert.That(Vector3.Distance(cat.transform.position, cancelledAt), Is.LessThan(.001f), context);
                Assert.That(completed, Is.Zero, context + " late completion after cancellation");
                cat.enabled = true;
            }
        }
    }

    [UnityTest]
    public IEnumerator BathMat_CompletesOnce_AndStillWaitsForAnExplicitRestStop()
    {
        yield return Prepare();
        foreach (var activity in Activities())
        {
            Start(activity);
            float deadline = Time.realtimeSinceStartup + 18f;
            while (activity.IsRunning && !activity.IsWaitingForRestStop && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                if (activity.IsRunning)
                    Assert.That(cat.GetComponent<CatActivityAnimation>().CurrentPose, Is.Not.EqualTo(CatActivityPose.Paw));
            }
            if (activity is MatKneadActivity)
            {
                Assert.That(activity.IsWaitingForRestStop, Is.True, "The gentle intro must retain continuous mat rest.");
                Assert.That(cat.GetComponent<CatGentleKneadMotion>().IsActive, Is.False);
                Assert.That(cat.GetComponent<CatToyContactMotion>().LeftDistance, Is.EqualTo(float.PositiveInfinity));
                Assert.That(cat.GetComponent<CatActivityAnimation>().CurrentPose, Is.EqualTo(CatActivityPose.Sleep));
                Assert.That(completed, Is.Zero);
                yield return new WaitForSeconds(.3f);
                Assert.That(activity.IsWaitingForRestStop, Is.True);
                Assert.That(activity.RequestRestStop(), Is.True);
                while (activity.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
            }
            Assert.That(activity.IsRunning, Is.False, activity.StoreProductId);
            Assert.That(completed, Is.EqualTo(1), activity.StoreProductId + " completes once");
            Assert.That(cat.GetComponent<CatGentleKneadMotion>().IsActive, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(Vector3.Angle(cat.transform.up, Vector3.up), Is.LessThan(.5f));
            activity.CancelForTransition(); yield return null;
            Assert.That(completed, Is.EqualTo(1));
        }
    }

    [Test]
    public void BathroomGates_LeaveGardenAndOtherRoomKindsUnchanged()
    {
        var host = new GameObject("Gentle bathroom scope"); host.SetActive(false);
        try
        {
            var mat = host.AddComponent<MatKneadActivity>();
            var kind = typeof(CatActivity).GetField("kind", BindingFlags.Instance | BindingFlags.NonPublic);
            var product = typeof(CatActivity).GetField("storeProductId", BindingFlags.Instance | BindingFlags.NonPublic);
            kind.SetValue(mat, CatActivityKind.DaisyRoll); product.SetValue(mat, HomeStoreService.GardenDaisyBedId);
            Assert.That(mat.UsesGentleKneading, Is.True);
            kind.SetValue(mat, CatActivityKind.MatKnead); product.SetValue(mat, HomeStoreService.BathroomBathMatId);
            Assert.That(mat.UsesGentleKneading, Is.True);
            foreach (var value in new[] { CatActivityKind.KitchenMatKnead, CatActivityKind.BedroomMatKnead })
            { kind.SetValue(mat, value); Assert.That(mat.UsesGentleKneading, Is.False); }
            kind.SetValue(mat, CatActivityKind.MatKnead); product.SetValue(mat, "unrelated.custom-mat");
            Assert.That(mat.UsesGentleKneading, Is.False);
            var litter = host.AddComponent<LitterDigActivity>();
            kind.SetValue(litter, CatActivityKind.LitterDig); product.SetValue(litter, HomeStoreService.BathroomLitterBoxId);
            Assert.That(litter.UsesGentleScraping, Is.True);
            product.SetValue(litter, "unrelated.custom-litter"); Assert.That(litter.UsesGentleScraping, Is.False);
        }
        finally { Object.DestroyImmediate(host); }
    }
}
