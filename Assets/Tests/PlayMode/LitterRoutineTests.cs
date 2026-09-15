using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class LitterRoutineTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedTimeScale, savedCaptureDelta;
    CatMovement cat;
    LitterDigActivity activity;
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
    void OnCompleted(CatActivity value) { if (value == activity) completed++; }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        var state = HomeStoreSaveState.CreateDefault(); state.ownedProductIds = HomeStoreService.BathroomCollection.ToArray();
        HomeStoreService.ApplySavedState(state); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>();
        CatActionState.CancelForTransition(cat);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        activity = CatActivity.Registered.OfType<LitterDigActivity>().Single(a => a.StoreProductId == HomeStoreService.BathroomLitterBoxId);
        Assert.That(activity.LitterSurface, Is.Not.Null, "The actual bathroom prefab must bind the separate sand mesh.");
        Assert.That(activity.LitterSurface.IsConfigured, Is.True);
    }
    void Start()
    {
        CatActionState.CancelForTransition(cat); cat.enabled = true; RoomPlayModeSupport.ProvisionNeeds();
        var controller = cat.GetComponent<CharacterController>(); controller.enabled = false;
        Vector3 entry = activity.RoutineEntryPoint.position; entry.y = .05f;
        cat.transform.SetPositionAndRotation(entry, Quaternion.identity);
        controller.enabled = true; Physics.SyncTransforms(); completed = 0;
        Assert.That(activity.TryStart(cat), Is.True, "The real authored tray entrance must be accessible.");
    }
    IEnumerator WaitFor(CatLitterPhase phase)
    {
        float deadline = Time.realtimeSinceStartup + 16f;
        while (activity.Phase != phase && Time.realtimeSinceStartup < deadline)
        { Assert.That(activity.IsRunning, Is.True, ReachFailure()); yield return new WaitForEndOfFrame(); }
        Assert.That(activity.Phase, Is.EqualTo(phase));
        yield return new WaitForEndOfFrame();
    }

    [UnityTest]
    public IEnumerator TenBreeds_DigRealHole_Crouch_Cover_AndCompleteOnce()
    {
        yield return Prepare();
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            Assert.That(CatBreedService.Select(breed.Id), Is.True); yield return null; yield return null;
            Mesh source = activity.LitterSurface.Sand.sharedMesh; Vector3 scale = cat.transform.localScale;
            Transform hips = cat.GetComponentsInChildren<Transform>().Single(b => b.name == "DEF-spine");
            Start(); yield return WaitFor(CatLitterPhase.Investigating);
            var phases = new List<CatLitterPhase>();
            var motion = cat.GetComponent<CatLitterRoutineMotion>();
            var contact = cat.GetComponent<CatToyContactMotion>();
            var pose = cat.GetComponent<CatActivityAnimation>();
            Quaternion digHeading = cat.transform.rotation;
            float standingHips = hips.position.y, lowestHips = standingHips;
            float deepestVertex = 0f, highestRim = 0f, biggestDepth = 0f, greatestShoulderDrop = 0f;
            int maximumGrains = 0, strokes = 0, replants = 0;
            Vector3 excavatedHole = Vector3.zero;
            bool left = false, right = false, headDown = false, clearedBeforeExit = false;
            float deadline = Time.realtimeSinceStartup + 26f;
            while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                CatLitterPhase phase = activity.Phase;
                if (phases.Count == 0 || phases[phases.Count - 1] != phase) phases.Add(phase);
                Assert.That(Vector3.Distance(cat.transform.localScale, scale), Is.LessThan(.0001f), breed.Id + " root squash");
                if (phase == CatLitterPhase.Positioning || phase == CatLitterPhase.Returning)
                    foreach (Transform foot in cat.GetComponentsInChildren<Transform>().Where(b => b.name == "DEF-foot.L" || b.name == "DEF-foot.R" || b.name == "DEF-hand.L" || b.name == "DEF-hand.R"))
                        Assert.That(activity.LitterSurface.Contains(foot.position, 0f), Is.True, breed.Id + " " + phase + " " + foot.name + " outside sand: " + foot.position.ToString("F5"));
                if (phase == CatLitterPhase.Digging || phase == CatLitterPhase.Squatting || phase == CatLitterPhase.Covering)
                {
                    Assert.That(motion.IsActive, Is.True, breed.Id);
                    Assert.That(activity.LitterSurface.Contains(motion.LeftTarget), Is.True, breed.Id + " left paw outside sand");
                    Assert.That(activity.LitterSurface.Contains(motion.RightTarget), Is.True, breed.Id + " right paw outside sand");
                    Assert.That(Quaternion.Angle(cat.transform.rotation, digHeading), Is.LessThan(.5f), breed.Id + " hiding the cover behind a half turn");
                    Assert.That(motion.HeadHeightAboveSand, Is.GreaterThan(.08f), breed.Id + " head joint must stay above sand");
                    Assert.That(motion.ChestPitch, Is.InRange(0f, CatLitterRoutineMotion.MaximumChestPitch), breed.Id + " bounded chest bend");
                    Assert.That(motion.RearSupportDisplacement, Is.LessThan(.00001f), breed.Id + " chest reach must preserve native hips and rear paw support");
                    Assert.That(motion.ForelegLengthChange, Is.LessThan(.00001f), breed.Id + " chest reach must not stretch either foreleg");
                    greatestShoulderDrop = Mathf.Max(greatestShoulderDrop, motion.ChestShoulderDrop);
                    headDown |= motion.HeadPitch > 20f;
                    Assert.That(Mathf.Min(motion.LeftLift, motion.RightLift), Is.Zero, breed.Id + " one forepaw must remain planted");
                    if (motion.LeftLift > .012f)
                    {
                        left = true;
                        Assert.That(motion.LeftGeometricDeficit, Is.LessThan(0f), breed.Id + " target must remain inside the real foreleg span");
                        Assert.That(contact.LeftDistance, Is.LessThan(.015f), breed.Id + " left paw target unreachable; geometric deficit=" + motion.LeftGeometricDeficit.ToString("F5"));
                    }
                    if (motion.RightLift > .012f)
                    {
                        right = true;
                        Assert.That(motion.RightGeometricDeficit, Is.LessThan(0f), breed.Id + " target must remain inside the real foreleg span; phase=" + phase +
                            "; target=" + motion.RightTarget.ToString("F5") + "; before=" + motion.RightDeficitBeforeSolve.ToString("F5") +
                            "; shoulderBefore=" + motion.RightShoulderBeforeSolve.ToString("F5") + "; shoulderAfter=" + motion.RightShoulder.ToString("F5"));
                        Assert.That(contact.RightDistance, Is.LessThan(.015f), breed.Id + " right paw target unreachable: phase=" + phase +
                            "; target=" + motion.RightTarget.ToString("F5") + "; root=" + cat.transform.position.ToString("F5") +
                            "; depth=" + activity.LitterSurface.Depth.ToString("F5") + "; lift=" + motion.RightLift.ToString("F5") +
                            "; geometric deficit=" + motion.RightGeometricDeficit.ToString("F5"));
                    }
                    maximumGrains = Mathf.Max(maximumGrains, activity.LitterSurface.ActiveGrains);
                    if (motion.ContactStrokes > strokes)
                    {
                        Vector3 contactDelta = motion.LastContactTarget - motion.HoleTarget; contactDelta.y = 0f;
                        Assert.That(contactDelta.magnitude, Is.LessThanOrEqualTo(CatLitterSandSurface.HoleRadius * 1.35f), breed.Id + " scrape must touch this hole or its displaced rim");
                        var actualHand = cat.GetComponentsInChildren<Transform>().Single(b => b.name == (motion.LastContactWasLeft ? "DEF-hand.L" : "DEF-hand.R"));
                        Assert.That(motion.LastContactDistance, Is.LessThan(CatLitterRoutineMotion.ContactTolerance), breed.Id + " acceptance must follow the actual IK solve");
                        Assert.That(Vector3.Distance(actualHand.position,motion.LastContactTarget), Is.LessThan(.015f),
                            breed.Id + " actual sand-contact paw arrival: phase=" + phase +
                            "; side=" + (motion.LastContactWasLeft ? "L" : "R") +
                            "; target=" + motion.LastContactTarget.ToString("F5") +
                            "; solved=" + motion.LastContactPawPosition.ToString("F5") +
                            "; rendered=" + actualHand.position.ToString("F5"));
                    }
                    strokes = Mathf.Max(strokes, motion.ContactStrokes);
                    replants = Mathf.Max(replants, motion.ReplantCount);
                    if (phase == CatLitterPhase.Digging) excavatedHole = motion.HoleTarget;
                    if (phase == CatLitterPhase.Covering)
                    {
                        Assert.That(Vector3.Distance(motion.HoleTarget, excavatedHole), Is.LessThan(.001f), breed.Id + " replant must cover the existing hole");
                        if (Mathf.Max(motion.LeftLift,motion.RightLift) > .012f)
                            Assert.That(motion.ReplantCount, Is.EqualTo(1), breed.Id + " capture fresh standing paws after the crouch");
                    }
                    biggestDepth = Mathf.Max(biggestDepth, activity.LitterSurface.Depth);
                    if (phase == CatLitterPhase.Squatting)
                    {
                        Assert.That(motion.PelvisHoleDistance, Is.LessThan(.005f), breed.Id + " pelvis must use the excavated hole");
                        foreach (Transform foot in cat.GetComponentsInChildren<Transform>().Where(b => b.name == "DEF-foot.L" || b.name == "DEF-foot.R"))
                            Assert.That(activity.LitterSurface.Contains(foot.position), Is.True, breed.Id + " rear paw outside sand");
                        lowestHips = Mathf.Min(lowestHips, hips.position.y);
                        Assert.That(pose.CurrentPose, Is.EqualTo(CatActivityPose.SitDown), breed.Id + " crouch must use the skeleton");
                        Assert.That(motion.ChestPitch, Is.Zero, breed.Id + " crouching uses the source spine and forelegs");
                    }
                    else Assert.That(pose.CurrentPose, Is.EqualTo(CatActivityPose.GentleKnead), breed.Id + " no aggressive swat");
                    var surface = activity.LitterSurface;
                    foreach (Vector3 vertex in surface.Sand.sharedMesh.vertices)
                    {
                        float delta = surface.Sand.transform.TransformPoint(vertex).y - surface.PlaneHeight;
                        deepestVertex = Mathf.Min(deepestVertex, delta); highestRim = Mathf.Max(highestRim, delta);
                    }
                }
                if (phase == CatLitterPhase.Exiting)
                    clearedBeforeExit |= activity.LitterSurface.Depth < .0001f && !motion.IsActive;
                yield return new WaitForEndOfFrame();
            }
            CollectionAssert.AreEqual(new[] { CatLitterPhase.Investigating, CatLitterPhase.Digging, CatLitterPhase.Positioning,
                CatLitterPhase.Squatting, CatLitterPhase.Returning, CatLitterPhase.Covering, CatLitterPhase.Exiting }, phases, breed.Id + " " + ReachFailure());
            Assert.That(left && right && headDown, Is.True, breed.Id + " visible alternating strokes/head inspection");
            Assert.That(standingHips - lowestHips, Is.GreaterThan(.015f), breed.Id + " actual crouch, not just a phase label");
            Assert.That(biggestDepth, Is.GreaterThan(.035f), breed.Id);
            if (breed.Id == CatBreedCatalog.DefaultBreedId)
                Assert.That(greatestShoulderDrop, Is.GreaterThan(.008f), breed.Id + " the measured short reach requires real chest lowering, not raised paw targets");
            Assert.That(deepestVertex, Is.LessThan(-.028f), breed.Id + " an actual excavated surface, not a decal");
            Assert.That(highestRim, Is.GreaterThan(.003f), breed.Id + " displaced sand at the rim");
            Assert.That(maximumGrains, Is.InRange(1, 24), breed.Id + " visible bounded scatter");
            Assert.That(strokes, Is.GreaterThanOrEqualTo(5), breed.Id + " strokes require measured paw contact");
            Assert.That(replants, Is.EqualTo(1), breed.Id + " the cover phase must replant once");
            Assert.That(clearedBeforeExit, Is.True, breed.Id + " covering fills the hole before leaving");
            Assert.That(activity.IsRunning, Is.False); Assert.That(completed, Is.EqualTo(1));
            Assert.That(activity.LitterSurface.Sand.sharedMesh, Is.SameAs(source));
            AssertClean();
        }
    }

    [UnityTest]
    public IEnumerator EachSandPhase_PausesExactly_AndCancellationRestoresTheMeshWithoutCompletion()
    {
        yield return Prepare();
        foreach (var phase in new[] { CatLitterPhase.Digging, CatLitterPhase.Positioning, CatLitterPhase.Squatting, CatLitterPhase.Returning, CatLitterPhase.Covering })
        {
            Mesh source = activity.LitterSurface.Sand.sharedMesh; Start(); yield return WaitFor(phase);
            yield return new WaitForSeconds(phase == CatLitterPhase.Digging ? 1.65f : .65f); Time.timeScale = 0f;
            yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
            var surface = activity.LitterSurface; var motion = cat.GetComponent<CatLitterRoutineMotion>();
            Vector3[] frozen = surface.Sand.sharedMesh.vertices;
            Vector3 target = motion.LeftTarget, position = cat.transform.position;
            float depth = surface.Depth, pitch = motion.HeadPitch, chestPitch = motion.ChestPitch;
            var chest = cat.GetComponentsInChildren<Transform>().Single(b => b.name == "DEF-spine.001");
            Quaternion chestRotation = chest.localRotation, sourceChestRotation = motion.ChestSourceLocalRotation;
            int grains = surface.ActiveGrains, strokes = motion.ContactStrokes;
            int wasteCount = activity.Waste != null ? activity.Waste.Emitted : 0;
            Vector3[] wasteMesh = activity.Waste != null && activity.Waste.IsActive ? activity.Waste.RuntimeMesh.vertices : null;
            for (int frame = 0; frame < 8; frame++) yield return new WaitForEndOfFrame();
            Assert.That(activity.Phase, Is.EqualTo(phase)); CollectionAssert.AreEqual(frozen, surface.Sand.sharedMesh.vertices);
            Assert.That(surface.Depth, Is.EqualTo(depth)); Assert.That(surface.ActiveGrains, Is.EqualTo(grains));
            Assert.That(motion.HeadPitch, Is.EqualTo(pitch)); Assert.That(motion.ContactStrokes, Is.EqualTo(strokes));
            Assert.That(motion.ChestPitch, Is.EqualTo(chestPitch));
            Assert.That(1f - Mathf.Abs(Quaternion.Dot(chest.localRotation, chestRotation)), Is.LessThan(.000001f), "actual chest pose must freeze");
            Assert.That(motion.LeftTarget, Is.EqualTo(target)); Assert.That(cat.transform.position, Is.EqualTo(position));
            Assert.That(activity.Waste != null ? activity.Waste.Emitted : 0, Is.EqualTo(wasteCount));
            if (wasteMesh != null) CollectionAssert.AreEqual(wasteMesh, activity.Waste.RuntimeMesh.vertices);
            activity.CancelForTransition(); Assert.That(surface.Sand.sharedMesh, Is.SameAs(source));
            if (chestPitch > 0f)
                Assert.That(1f - Mathf.Abs(Quaternion.Dot(chest.localRotation, sourceChestRotation)), Is.LessThan(.000001f), "cancel must immediately restore the source chest rotation");
            AssertClean(); Assert.That(completed, Is.Zero);
            cat.enabled = false; Vector3 cancelled = cat.transform.position; Time.timeScale = 1f;
            yield return new WaitForSeconds(2f);
            Assert.That(Vector3.Distance(cat.transform.position, cancelled), Is.LessThan(.001f));
            Assert.That(completed, Is.Zero); Assert.That(surface.ActiveGrains, Is.Zero);
            cat.enabled = true;
        }
    }

    void AssertClean()
    {
        Assert.That(activity.Waste == null || !activity.Waste.IsActive, Is.True);
        Assert.That(activity.Waste == null || activity.Waste.VisibleCount == 0, Is.True);
        Assert.That(activity.LitterSurface.IsActive, Is.False); Assert.That(activity.LitterSurface.ActiveGrains, Is.Zero);
        Assert.That(cat.GetComponent<CatLitterRoutineMotion>().IsActive, Is.False);
        Assert.That(cat.GetComponent<CatLitterRoutineMotion>().ChestPitch, Is.Zero);
        Assert.That(cat.GetComponent<CatToyContactMotion>().LeftDistance, Is.EqualTo(float.PositiveInfinity));
        Assert.That(cat.GetComponent<CatToyContactMotion>().RightDistance, Is.EqualTo(float.PositiveInfinity));
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False); Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
    }

    string ReachFailure()
    {
        var motion = cat != null ? cat.GetComponent<CatLitterRoutineMotion>() : null;
        return motion != null ? motion.FailureReason + " " + motion.FailureReportPath : string.Empty;
    }

    [UnityTest]
    public IEnumerator ToiletVariants_UseSameHole_DropThreePieces_ThenBuryEverything()
    {
        yield return Prepare();
        float[] heights = new float[2];
        for (int variant = 0; variant < 2; variant++)
        {
            Start(); yield return WaitFor(CatLitterPhase.Digging);
            Vector3 digRoot = cat.transform.position;
            yield return WaitFor(CatLitterPhase.Squatting);
            yield return new WaitForSeconds(1.65f); yield return new WaitForEndOfFrame();
            var motion = cat.GetComponent<CatLitterRoutineMotion>();
            var fx = activity.Waste;
            heights[variant] = motion.PelvisPosition.y;
            Assert.That(Vector3.Distance(digRoot, cat.transform.position), Is.GreaterThan(.2f), "walk forward before crouching");
            Assert.That(motion.PelvisHoleDistance, Is.LessThan(.005f));
            Assert.That(fx.IsSolid, Is.EqualTo(variant == 0));
            Assert.That(fx.Emitted, Is.EqualTo(variant == 0 ? 3 : 1));
            Assert.That(fx.VisibleCount, Is.EqualTo(fx.Emitted));
            Assert.That(Vector3.Distance(fx.Hole, motion.HoleTarget), Is.LessThan(.001f));
            yield return WaitFor(CatLitterPhase.Exiting);
            Assert.That(fx.VisibleCount, Is.Zero, "sand must bury every piece before departure");
            float deadline = Time.realtimeSinceStartup + 6f;
            while (activity.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(completed, Is.EqualTo(1)); AssertClean();
        }
        Assert.That(heights[0] - heights[1], Is.GreaterThan(.015f), "poop holds the actual hips higher than pee");
    }

    [Test]
    public void SandMesh_IsTrulyDeformed_OriginalAssetAndForeignOwnerAreProtected()
    {
        var host = new GameObject("Isolated litter surface"); host.SetActive(false);
        var foreign = new GameObject("Foreign litter owner"); foreign.SetActive(false);
        var original = new Mesh();
        try
        {
            original.vertices = new[] { new Vector3(-.5f, .2f, -.4f), new Vector3(-.5f,.2f,.4f), new Vector3(.5f,.2f,-.4f), new Vector3(.5f,.2f,.4f) };
            original.triangles = new[] { 0,1,2, 2,1,3 }; original.RecalculateBounds();
            var filter = host.AddComponent<MeshFilter>(); filter.sharedMesh = original; host.AddComponent<MeshRenderer>();
            var owner = host.AddComponent<LitterDigActivity>(); var other = foreign.AddComponent<LitterDigActivity>();
            owner.EditorConfigureLitterSurface(filter, new Vector3(0f,.2f,0f), new Vector2(1f,.8f));
            var sand = owner.LitterSurface; Assert.That(sand.Begin(owner), Is.True);
            Assert.That(sand.Begin(other), Is.False);
            sand.Excavate(owner, new Vector3(0f,.2f,0f), 1f);
            Assert.That(filter.sharedMesh, Is.Not.SameAs(original));
            Assert.That(filter.sharedMesh.vertices.Min(v => v.y), Is.LessThan(.17f));
            Assert.That(filter.sharedMesh.vertices.Max(v => v.y), Is.GreaterThan(.203f));
            Assert.That(original.vertices.All(v => Mathf.Approximately(v.y,.2f)), Is.True);
            sand.Stop(other); Assert.That(sand.IsActive, Is.True);
            sand.Excavate(other, Vector3.zero, 0f); Assert.That(sand.Depth, Is.EqualTo(CatLitterSandSurface.MaximumDepth));
            sand.Stop(owner); Assert.That(filter.sharedMesh, Is.SameAs(original));
            sand.Stop(owner); Assert.That(filter.sharedMesh, Is.SameAs(original));
        }
        finally { Object.DestroyImmediate(host); Object.DestroyImmediate(foreign); Object.DestroyImmediate(original); }
    }
}
