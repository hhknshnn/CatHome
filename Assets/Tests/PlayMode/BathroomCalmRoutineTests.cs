using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Authored bathroom products, real floor paths and skeletal poses on the isolated QA save.</summary>
public sealed class BathroomCalmRoutineTests
{
    HomeStoreSaveState savedStore;
    float savedTimeScale;
    CatMovement cat;
    TubEdgeWalkActivity tub;
    SitLookActivity mirror;
    Vector3 originalScale, spawn;
    Transform originalParent;
    GameObject modalOwner;
    CatActivity observedCompletionTarget;
    int completions;

    [UnitySetUp]
    public IEnumerator Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use the isolated QA save for native bathroom checks.");
        savedStore = HomeStoreService.CaptureState(); savedTimeScale = Time.timeScale;
        CatActivity.Completed += OnActivityCompleted;
        Time.timeScale = 1f;
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(cat, Is.Not.Null);
        CatActionState.CancelForTransition(cat);
        tub = Object.FindAnyObjectByType<TubEdgeWalkActivity>(FindObjectsInactive.Include);
        mirror = Object.FindObjectsByType<SitLookActivity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(a => a.Kind == CatActivityKind.MirrorGaze);
        Assert.That(tub, Is.Not.Null);
        var state = HomeStoreSaveState.CreateDefault(); state.currentRoomId = HomeRoomService.BathroomId;
        HomeStoreService.ApplySavedState(state);
        yield return null;
        Assert.That(tub.TryStart(cat), Is.False, "An unowned tub must stay unavailable.");
        Assert.That(mirror.TryStart(cat), Is.False, "An unowned mirror must stay unavailable.");
        state.ownedProductIds = HomeStoreService.Products.Where(p =>
            HomeStoreService.IsProductInRoomCollection(HomeRoomService.BathroomId, p.Id)).Select(p => p.Id).ToArray();
        Assert.That(state.ownedProductIds, Has.Length.EqualTo(10), "Keep every current bathroom neighbour present.");
        HomeStoreService.ApplySavedState(state);
        yield return null; yield return null;
        tub.RefreshUnlockPresentation(); mirror.RefreshUnlockPresentation();
        spawn = cat.transform.position; originalScale = cat.transform.localScale; originalParent = cat.transform.parent;
        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(cat.HasScopedInputBlock, Is.False);
    }

    [TearDown]
    public void After()
    {
        CatActionState.CancelForTransition(cat);
        CatActivity.Completed -= OnActivityCompleted;
        if (cat != null && modalOwner != null) cat.ReleaseInputBlock(modalOwner);
        if (modalOwner != null) Object.DestroyImmediate(modalOwner);
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(savedStore);
        Time.timeScale = savedTimeScale;
    }

    [UnityTest]
    public IEnumerator Tub_FinishesAtRimEnd_ThenJumpsDownWithoutWaterPawing()
    {
        observedCompletionTarget = tub; completions = 0;
        Assert.That(tub.TryStart(cat), Is.True);
        Assert.That(mirror.TryStart(cat), Is.False, "A second activity cannot replace the rim walk.");
        var animation = cat.GetComponent<CatActivityAnimation>();
        Vector3 rimStart = Field<Transform>(tub, "rimStartPoint").position;
        Vector3 rimEnd = Field<Transform>(tub, "rimEndPoint").position;
        Vector3 edge = rimEnd - rimStart; edge.y = 0;
        bool walkedRim = false, reachedEnd = false, jumpedDown = false;
        float peak = cat.transform.position.y;
        float deadline = Time.realtimeSinceStartup + 25f;
        while (tub.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            if (!tub.IsRunning) break;
            CatActivityPose pose = animation.CurrentPose;
            Assert.That(pose, Is.Not.EqualTo(CatActivityPose.Paw), "Tub balance must never switch to a water paw strike.");
            Assert.That(pose == CatActivityPose.BatLeft || pose == CatActivityPose.BatRight, Is.False);
            peak = Mathf.Max(peak, cat.transform.position.y);
            Vector3 along = cat.transform.position - rimStart; along.y = 0;
            float progress = Vector3.Dot(along, edge) / Mathf.Max(.0001f, edge.sqrMagnitude);
            bool onRim = cat.transform.position.y >= Mathf.Min(rimStart.y, rimEnd.y) - .04f;
            if (pose == CatActivityPose.Walk && onRim)
            {
                walkedRim = true;
                if (reachedEnd && !jumpedDown)
                    Assert.That(progress, Is.GreaterThan(.86f), "The finished rim walk must not reverse to the tub centre.");
                reachedEnd |= progress > .95f;
            }
            if (reachedEnd && pose == CatActivityPose.Hop) jumpedDown = true;
            Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        }
        Assert.That(tub.IsRunning, Is.False, "The balance routine must finish without another player action.");
        Assert.That(walkedRim && reachedEnd && jumpedDown, Is.True, "Observe a complete rim traversal and the departure jump.");
        Assert.That(peak, Is.GreaterThanOrEqualTo(Mathf.Max(rimStart.y, rimEnd.y) - .02f));
        Assert.That(completions, Is.EqualTo(1));
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        AssertRestored();
    }

    [UnityTest]
    public IEnumerator Mirror_ObservesWhileSeated_WithStillFeetAndBoundedHeadMotion()
    {
        observedCompletionTarget = mirror; completions = 0;
        Assert.That(mirror.ReactionKind, Is.EqualTo(SitLookReaction.Sit));
        Assert.That(mirror.TryStart(cat), Is.True);
        var animation = cat.GetComponent<CatActivityAnimation>();
        var animator = cat.GetComponentInChildren<Animator>();
        Vector3 seated = default; bool observed = false; int observedFrames = 0; float headMotion = 0;
        float deadline = Time.realtimeSinceStartup + 20f;
        while (mirror.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            if (!mirror.IsRunning || mirror.GestureBeats == 0) continue;
            if (animation.CurrentPose == CatActivityPose.StandUp) continue;
            Assert.That(animation.CurrentPose, Is.EqualTo(CatActivityPose.Sit), "Reflection viewing is a seated gaze, not a locomotion or swat loop.");
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(0f).Within(.001f));
            if (!observed) { seated = cat.transform.position; observed = true; }
            Vector3 delta = cat.transform.position - seated; delta.y = 0;
            Assert.That(delta.magnitude, Is.LessThan(.015f), "The cat's feet stay at the mirror viewing spot.");
            var gaze = cat.GetComponent<CatFurnitureGaze>();
            headMotion = Mathf.Max(headMotion, gaze != null ? gaze.Deflection : 0f);
            observedFrames++;
        }
        Assert.That(mirror.IsRunning, Is.False);
        Assert.That(observedFrames, Is.GreaterThan(10));
        Assert.That(headMotion, Is.GreaterThan(.25f), "Keep the real gentle head gaze instead of freezing the whole skeleton.");
        Assert.That(mirror.GestureBeats, Is.EqualTo(3));
        Assert.That(completions, Is.EqualTo(1));
        Assert.That(cat.GetComponent<CatFurnitureGaze>().Deflection, Is.EqualTo(0f));
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        AssertRestored();
    }

    [UnityTest]
    public IEnumerator BothRoutines_CancelCleanly_WithoutReleasingAnotherInputOwner()
    {
        foreach (CatActivity activity in new CatActivity[] { tub, mirror })
        {
            cat.ApplySavedWorldPose(spawn, Quaternion.identity);
            RoomPlayModeSupport.ProvisionNeeds();
            observedCompletionTarget = activity; completions = 0;
            Assert.That(activity.TryStart(cat), Is.True);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (Time.realtimeSinceStartup < deadline && activity.IsRunning &&
                   (activity == tub ? cat.transform.position.y < .4f : mirror.GestureBeats == 0))
                yield return null;
            Assert.That(activity.IsRunning, Is.True);
            Assert.That(activity == tub ? cat.transform.position.y >= .4f : mirror.GestureBeats > 0, Is.True);
            modalOwner = new GameObject("Bathroom cancellation input owner");
            cat.AcquireInputBlock(modalOwner);
            activity.CancelForTransition();
            Assert.That(completions, Is.Zero, "Cancelling cannot award the completion beat.");
            Assert.That(cat.HasScopedInputBlock, Is.True, "The modal owner survives physical-action cancellation.");
            AssertRestored(true);
            Assert.That(activity.TryStart(cat), Is.False, "A new action must still respect the other input owner.");
            cat.ReleaseInputBlock(modalOwner); Object.DestroyImmediate(modalOwner); modalOwner = null;
            Assert.That(cat.HasScopedInputBlock, Is.False);
            Assert.That(activity.TryStart(cat), Is.True, "Cancelling must leave the routine reusable after the modal closes.");
            activity.CancelForTransition();
            AssertRestored();
        }
    }

    void OnActivityCompleted(CatActivity activity) { if (activity == observedCompletionTarget) completions++; }
    void AssertRestored(bool keepModal = false)
    {
        Assert.That(CatActivity.Active, Is.Null);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cat.HasScopedInputBlock, Is.EqualTo(keepModal));
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(cat.GetComponent<CatActivityAnimation>().IsActive, Is.False);
        Assert.That(cat.transform.parent, Is.SameAs(originalParent));
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(Vector3.Dot(cat.transform.up, Vector3.up), Is.GreaterThan(.999f));
        Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True);
        if (cat.TryGetComponent<CatFurnitureGaze>(out var gaze)) Assert.That(gaze.Deflection, Is.Zero);
    }
    static T Field<T>(object target, string name) => (T)target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
}
