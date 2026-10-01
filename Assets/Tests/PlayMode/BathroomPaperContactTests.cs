using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BathroomPaperContactTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    bool savedReducedMotion;
    float savedTimeScale, savedCaptureDelta;
    PaperSpinActivity paper;
    CatMovement cat;
    GameObject modalOwner, temporaryContact;
    Transform contactToRestore;
    Vector3 contactPositionToRestore;
    int completions;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Run only inside the isolated native QA session.");
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedReducedMotion = CatRunnerProgressService.ReducedMotion;
        savedTimeScale = Time.timeScale; savedCaptureDelta = Time.captureDeltaTime;
        Time.timeScale = 1f; Time.captureFramerate = 60;
        CatRunnerProgressService.SetReducedMotion(false); CatActivity.Completed += Completed;
    }
    [TearDown] public void After()
    {
        if (paper != null) { paper.CancelForTransition(); paper.enabled = true; }
        if (cat != null && modalOwner != null) cat.ReleaseInputBlock(modalOwner);
        if (modalOwner != null) Object.DestroyImmediate(modalOwner);
        if (contactToRestore != null) contactToRestore.position = contactPositionToRestore;
        if (temporaryContact != null) Object.DestroyImmediate(temporaryContact);
        CatActivity.Completed -= Completed;
        CatBreedService.Select(savedBreed); RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(savedStore); CatRunnerProgressService.SetReducedMotion(savedReducedMotion);
        Time.timeScale = savedTimeScale; Time.captureDeltaTime = savedCaptureDelta;
    }
    void Completed(CatActivity target) { if (target == paper) completions++; }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        var store = HomeStoreSaveState.CreateDefault(); store.ownedProductIds = HomeStoreService.BathroomCollection.ToArray();
        HomeStoreService.ApplySavedState(store); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>();
        paper = CatActivity.Registered.OfType<PaperSpinActivity>().Single(a => a.UsesPaperTears);
        Assert.That(paper, Is.Not.Null); Assert.That(paper.RollPivot, Is.Not.Null);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        ReadyCat();
    }
    void ReadyCat()
    {
        CatActionState.CancelForTransition(cat);
        RoomPlayModeSupport.ProvisionNeeds().ApplySavedValue(70f);
        Vector3 point = paper.RoutineEntryPoint.position; point.y = .05f;
        cat.ApplySavedWorldPose(point, Quaternion.identity); Physics.SyncTransforms(); completions = 0;
        Assert.That(paper.TryGetPromptDistance(cat, out _), Is.True, "The real authored nearby action must be available.");
    }
    IEnumerator WaitForContact()
    {
        float deadline = Time.realtimeSinceStartup + 12f;
        while (paper.IsRunning && paper.PaperContactCount == 0 && Time.realtimeSinceStartup < deadline)
            yield return new WaitForEndOfFrame();
        Assert.That(paper.PaperContactCount, Is.GreaterThan(0), "The actual paw must reach the roll before any paper appears. " + ReachDiagnostic());
    }

    [UnityTest] public IEnumerator TenBreeds_RealPawTouchesTriggerEachTear_WithShortSideFacingReach()
    {
        yield return Prepare();
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            Assert.That(CatBreedService.Select(breed.Id), Is.True); yield return null; yield return null;
            ReadyCat(); Vector3 originalScale = cat.transform.localScale;
            Quaternion rest = paper.RollPivot.localRotation;
            Assert.That(paper.TryStart(cat), Is.True, breed.Id);
            int observed = 0; float maxTurn = 0;
            float deadline = Time.realtimeSinceStartup + 18f;
            while (paper.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(rest, paper.RollPivot.localRotation));
                Assert.That(cat.transform.localScale, Is.EqualTo(originalScale), breed.Id + " root proportions");
                if (paper.PaperContactCount == observed) continue;
                observed = paper.PaperContactCount;
                Assert.That(paper.LastPaperContactDistance, Is.LessThanOrEqualTo(CatPaperRollPawMotion.ContactTolerance), breed.Id);
                Assert.That(Vector3.Distance(paper.LastPaperContactPosition, paper.PaperContactPosition),
                    Is.LessThanOrEqualTo(CatPaperRollPawMotion.ContactTolerance), breed.Id + " actual hand arrival");
                Assert.That(NearestPawMeshPoint(), Is.LessThan(.055f), breed.Id + " visible paw geometry must touch the paper surface");
                var fx = paper.GetComponent<CatPaperTearFx>();
                Assert.That(fx.TotalTornPieces, Is.EqualTo(observed * 2), breed.Id + " exactly one tear event per hand contact");
                Assert.That(fx.ActivePieces, Is.InRange(1, CatPaperTearFx.MaximumPieces));
                Assert.That(fx.VisualRoot.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(1));
                var motion = cat.GetComponent<CatPaperRollPawMotion>();
                string side = observed % 2 == 1 ? "L" : "R";
                var bones = cat.GetComponentsInChildren<Transform>();
                Vector3 visibleForward = bones.Single(b => b.name == "DEF-spine.003").position -
                    bones.Single(b => b.name == "DEF-spine").position;
                Assert.That(CatActivityFacing.FacingDot(visibleForward, cat.transform.position, CatActivityFacing.CameraPosition(cat)),
                    Is.GreaterThanOrEqualTo(CatActivityFacing.MinimumViewDot),
                    breed.Id + " real working torso should face the player or be side-on, not face away");
                Transform arm = bones.Single(b => b.name == "DEF-upper_arm." + side);
                Transform fore = bones.Single(b => b.name == "DEF-forearm." + side);
                Transform hand = bones.Single(b => b.name == "DEF-hand." + side);
                Assert.That(Vector3.Distance(arm.position, fore.position), Is.EqualTo(motion.UpperLength).Within(.001f));
                Assert.That(Vector3.Distance(fore.position, hand.position), Is.EqualTo(motion.ForeLength).Within(.001f));
            }
            Assert.That(paper.IsRunning, Is.False, breed.Id + " routine timeout");
            Assert.That(observed, Is.EqualTo(paper.Swats), breed.Id + " each intended stroke must really touch. " + ReachDiagnostic());
            Assert.That(maxTurn, Is.GreaterThan(45f)); Assert.That(completions, Is.EqualTo(1));
            Assert.That(paper.RollPivot.localRotation, Is.EqualTo(rest));
            AssertStopped();
        }
    }

    [UnityTest] public IEnumerator MissingPhysicalContact_CannotSpinTearOrAwardCompletion()
    {
        yield return Prepare();
        var field = typeof(PaperSpinActivity).GetField("paperContactPoint", BindingFlags.NonPublic | BindingFlags.Instance);
        contactToRestore = (Transform)field.GetValue(paper);
        Transform contact = contactToRestore;
        if (contact == null)
        {
            temporaryContact = new GameObject("QA paper contact moved out of reach");
            contact = temporaryContact.transform; contact.position = paper.PaperContactPosition; field.SetValue(paper, contact);
        }
        else contactPositionToRestore = contact.position;
        Quaternion rest = paper.RollPivot.localRotation;
        Assert.That(paper.TryStart(cat), Is.True);
        // A target moved after an accepted start cannot be turned into a timed hit.
        contact.position += Vector3.up * 2f;
        float deadline = Time.realtimeSinceStartup + 12f;
        while (paper.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            Assert.That(Quaternion.Angle(rest, paper.RollPivot.localRotation), Is.LessThan(.001f));
            Assert.That(paper.PaperContactCount, Is.Zero);
        }
        Assert.That(paper.IsRunning, Is.False); Assert.That(completions, Is.Zero);
        Assert.That(paper.GetComponent<CatPaperTearFx>().TotalTornPieces, Is.Zero);
        AssertStopped();
    }

    [UnityTest] public IEnumerator RecordPlayer_UsesARealClearSideStand_AndKeepsTheDiscHorizontal()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("SecondFloor_Level01");
        var store = HomeStoreSaveState.CreateDefault();
        store.ownedProductIds = HomeStoreService.Products.Where(p => HomeStoreService.IsProductInRoomCollection(HomeRoomService.SecondFloorId,p.Id)).Select(p => p.Id).ToArray();
        HomeStoreService.ApplySavedState(store); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>();
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        paper = CatActivity.Registered.OfType<PaperSpinActivity>().Single(a => a.Kind == CatActivityKind.RecordSpin);
        RoomPlayModeSupport.ProvisionNeeds();
        var controller = cat.GetComponent<CharacterController>(); controller.enabled = false;
        Vector3 entry = paper.RoutineEntryPoint.position; entry.y = .05f; cat.transform.position = entry;
        controller.enabled = true; Physics.SyncTransforms();
        Assert.That(paper.TryStart(cat), Is.True, "The record must have a real reachable side, not a backwards work pose.");
        Quaternion original = paper.RollPivot.localRotation; int samples = 0; float turn = 0f;
        float deadline = Time.realtimeSinceStartup + 20f;
        while (paper.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            turn = Mathf.Max(turn, Quaternion.Angle(original,paper.RollPivot.localRotation));
            if (!paper.IsRunning || !paper.IsRecordTapping) continue;
            samples++;
            Assert.That(paper.RecordStandBlocked, Is.False);
            Assert.That(CatActivityMotion.IsFloorClear(paper.RecordStand), Is.True);
            Assert.That(CatActivityFacing.FacingDot(cat.transform.forward, cat.transform.position, CatActivityFacing.CameraPosition(cat)), Is.GreaterThanOrEqualTo(-.01f));
            Assert.That(Vector3.Dot(paper.RollPivot.up,Vector3.up), Is.GreaterThan(.999f), "Record rotation must remain around the upright spindle.");
        }
        Assert.That(samples, Is.GreaterThan(8)); Assert.That(turn, Is.GreaterThan(45f));
        Assert.That(paper.IsRunning, Is.False); Assert.That(completions, Is.EqualTo(1));
        Assert.That(paper.SpinAxis, Is.EqualTo(Vector3.up)); Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
    }

    [UnityTest] public IEnumerator PauseReducedMotionCancelDisableAndUnload_PreserveOwnershipAndReleasePaperResources()
    {
        yield return Prepare();
        CatRunnerProgressService.SetReducedMotion(true);
        Assert.That(paper.TryStart(cat), Is.True); yield return WaitForContact();
        var fx = paper.GetComponent<CatPaperTearFx>();
        Assert.That(fx.TotalTornPieces, Is.EqualTo(1)); Assert.That(fx.QuietMotion, Is.True);
        Transform visual = fx.VisualRoot; Mesh mesh = visual.GetComponent<MeshFilter>().sharedMesh;
        Material material = visual.GetComponent<MeshRenderer>().sharedMaterial;
        Time.timeScale = 0f; yield return new WaitForEndOfFrame();
        Vector3[] vertices = mesh.vertices; float age = fx.Elapsed;
        Quaternion roll = paper.RollPivot.localRotation; Vector3 paw = cat.GetComponent<CatPaperRollPawMotion>().PawPosition;
        int frozenContacts = paper.PaperContactCount;
        yield return new WaitForSecondsRealtime(.2f);
        // Update restores the source arm before the late IK pass. Compare the
        // rendered pose at the same frame boundary as the original sample.
        for (int frame = 0; frame < 8; frame++)
        {
            yield return new WaitForEndOfFrame();
            Assert.That(Vector3.Distance(cat.GetComponent<CatPaperRollPawMotion>().PawPosition, paw),
                Is.LessThan(.001f), "Paused final paw pose drifted at frame " + frame);
            Assert.That(paper.PaperContactCount, Is.EqualTo(frozenContacts));
        }
        Assert.That(mesh.vertices.SequenceEqual(vertices), Is.True); Assert.That(fx.Elapsed, Is.EqualTo(age));
        Assert.That(paper.RollPivot.localRotation, Is.EqualTo(roll));
        Time.timeScale = 1f;
        modalOwner = new GameObject("Paper QA independent modal owner"); cat.AcquireInputBlock(modalOwner);
        paper.CancelForTransition(); AssertStopped();
        Assert.That(cat.HasScopedInputBlock, Is.True); Assert.That(completions, Is.Zero);
        Assert.That(paper.TryStart(cat), Is.False);
        cat.ReleaseInputBlock(modalOwner); Object.DestroyImmediate(modalOwner); modalOwner = null;
        ReadyCat(); Assert.That(paper.TryStart(cat), Is.True); yield return WaitForContact();
        Assert.That(fx.VisualRoot, Is.SameAs(visual)); Assert.That(visual.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
        paper.enabled = false; AssertStopped(); paper.enabled = true;
        ReadyCat(); Assert.That(paper.TryStart(cat), Is.True); yield return WaitForContact();
        yield return RoomPlayModeSupport.LoadRoomAlone("Garden_Level01"); yield return null;
        Assert.That(visual == null && mesh == null && material == null, Is.True, "The bathroom owns all transient paper resources.");
        Assert.That(Object.FindObjectsByType<CatPaperTearFx>(FindObjectsInactive.Include), Is.Empty);
    }

    [UnityTest] public IEnumerator StrokeAndCompletionBoundaries_WaitForResume_BeforeChangingPoseOrAwarding()
    {
        yield return Prepare(); Assert.That(paper.TryStart(cat), Is.True);
        // At 60 captured frames/second, .996 is the final sampled phase of a
        // .92 second stroke. Pausing here exercises the coroutine boundary.
        yield return WaitForStrokeTail(1);
        yield return HoldBoundaryPaused();
        Time.timeScale = 1f;
        yield return WaitForStrokeTail(paper.Swats);
        yield return HoldBoundaryPaused();
        Time.timeScale = 1f;

        var speed = typeof(PaperSpinActivity).GetField("paperSpeed", BindingFlags.NonPublic | BindingFlags.Instance);
        var fx = paper.GetComponent<CatPaperTearFx>();
        float deadline = Time.realtimeSinceStartup + 16f;
        while (paper.IsRunning && ((float)speed.GetValue(paper) > 12f || fx.ActivePieces > 0) && Time.realtimeSinceStartup < deadline)
            yield return new WaitForEndOfFrame();
        Assert.That(paper.IsRunning, Is.True, "Observe the final rendered spin frame before terminal cleanup.");
        Assert.That((float)speed.GetValue(paper), Is.LessThanOrEqualTo(12f)); Assert.That(fx.ActivePieces, Is.Zero);
        yield return HoldBoundaryPaused();
        Time.timeScale = 1f;
        deadline = Time.realtimeSinceStartup + 4f;
        while (paper.IsRunning && Time.realtimeSinceStartup < deadline) yield return new WaitForEndOfFrame();
        Assert.That(paper.IsRunning, Is.False); Assert.That(paper.PaperContactCount, Is.EqualTo(paper.Swats));
        Assert.That(completions, Is.EqualTo(1)); AssertStopped();
    }

    IEnumerator WaitForStrokeTail(int index)
    {
        float deadline = Time.realtimeSinceStartup + 16f;
        var motion = cat.GetComponent<CatPaperRollPawMotion>();
        while (paper.IsRunning && !(motion.IsActive && motion.StrokeIndex == index && motion.NormalizedPhase > .98f)
               && Time.realtimeSinceStartup < deadline) yield return new WaitForEndOfFrame();
        Assert.That(paper.IsRunning && motion.IsActive, Is.True, "Expected the rendered final stroke frame.");
        Assert.That(motion.StrokeIndex, Is.EqualTo(index)); Assert.That(motion.NormalizedPhase, Is.GreaterThan(.98f));
    }

    IEnumerator HoldBoundaryPaused()
    {
        var motion = cat.GetComponent<CatPaperRollPawMotion>(); var pose = cat.GetComponent<CatActivityAnimation>();
        var fx = paper.GetComponent<CatPaperTearFx>();
        int stroke = motion.StrokeIndex, contacts = paper.PaperContactCount; float phase = motion.NormalizedPhase;
        bool activePaw = motion.IsActive; CatActivityPose heldPose = pose.CurrentPose;
        Quaternion roll = paper.RollPivot.localRotation;
        Time.timeScale = 0f;
        yield return new WaitForEndOfFrame();
        // Reject a phase transition on the very first paused frame. Sample the
        // final rendered skeleton after the animation/IK passes, as above.
        Assert.That(paper.IsRunning, Is.True); Assert.That(motion.IsActive, Is.EqualTo(activePaw));
        Assert.That(motion.StrokeIndex, Is.EqualTo(stroke)); Assert.That(pose.CurrentPose, Is.EqualTo(heldPose));
        var hands = cat.GetComponentsInChildren<Transform>().Where(b => b.name == "DEF-hand.L" || b.name == "DEF-hand.R").ToArray();
        Vector3[] paws = hands.Select(hand => hand.position).ToArray();
        var mesh = fx.VisualRoot.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices; float age = fx.Elapsed;
        for (int frame = 0; frame < 8; frame++)
        {
            yield return new WaitForEndOfFrame();
            Assert.That(paper.IsRunning, Is.True); Assert.That(cat.IsMovementPhysicallyLocked, Is.True);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.False);
            Assert.That(pose.CurrentPose, Is.EqualTo(heldPose)); Assert.That(motion.IsActive, Is.EqualTo(activePaw));
            Assert.That(motion.StrokeIndex, Is.EqualTo(stroke)); Assert.That(motion.NormalizedPhase, Is.EqualTo(phase));
            Assert.That(paper.PaperContactCount, Is.EqualTo(contacts)); Assert.That(completions, Is.Zero);
            Assert.That(paper.RollPivot.localRotation, Is.EqualTo(roll)); Assert.That(fx.Elapsed, Is.EqualTo(age));
            CollectionAssert.AreEqual(vertices, mesh.vertices);
            for (int hand = 0; hand < hands.Length; hand++)
                Assert.That(Vector3.Distance(hands[hand].position, paws[hand]), Is.LessThan(.001f), "Paused rendered forepaw moved.");
        }
    }

    float NearestPawMeshPoint()
    {
        var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var sample = new Mesh(); var vertices = new List<Vector3>();
        float nearest = float.PositiveInfinity;
        try
        {
            skin.BakeMesh(sample, true); sample.GetVertices(vertices);
            foreach (var vertex in vertices)
            {
                Vector3 world = skin.transform.TransformPoint(vertex);
                if (Vector3.Distance(world, paper.LastPaperContactPosition) > .12f) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(world, paper.PaperContactPosition));
            }
        }
        finally { Object.DestroyImmediate(sample); }
        return nearest;
    }
    string ReachDiagnostic()
    {
        var motion = cat != null ? cat.GetComponent<CatPaperRollPawMotion>() : null;
        return motion == null ? "No reach motion." :
            "Solved min=" + motion.MinimumDistance.ToString("F5") +
            "; shoulder→target min=" + motion.MinimumArmTargetDistance.ToString("F5") +
            "; arm span=" + (motion.UpperLength + motion.ForeLength).ToString("F5") +
            "; geometric deficit=" + motion.MinimumReachDeficit.ToString("F5") +
            "; shoulder=" + motion.ClosestShoulderPosition.ToString("F5") +
            "; root=" + motion.ClosestRootPosition.ToString("F5") +
            "; phase=" + motion.ClosestPhase.ToString("F3") +
            "; target=" + paper.PaperContactPosition.ToString("F5");
    }
    void AssertStopped()
    {
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True,
            "The released controller must fit beside the toilet with room to turn.");
        Assert.That(cat.GetComponent<CatPaperRollPawMotion>().IsActive, Is.False);
        var fx = paper.GetComponent<CatPaperTearFx>();
        Assert.That(fx.ActivePieces, Is.Zero);
        if (fx.VisualRoot != null) Assert.That(fx.VisualRoot.gameObject.activeSelf, Is.False);
    }
}
