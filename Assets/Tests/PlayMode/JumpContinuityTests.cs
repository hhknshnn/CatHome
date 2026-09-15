using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed partial class JumpContinuityTests
{
    [Serializable] public class Frame
    {
        public float time;
        public Vector3 root, visual, visualLocal, hips, shoulders, leftHand, rightHand, leftFoot, rightFoot;
        public Vector3 leftHip, rightHip, leftKnee, rightKnee;
        public string pose, clip;
        public float phase;
        public bool running, rest, nativeJump;
        public float nativePhase;
        public Vector3 forward;
        public bool turning;
        public float turnContactError;
        public int turnSteps;
    }
    [Serializable] public class Trace
    {
        public string room, product, breed;
        public int completed;
        public bool controllerFloorClear;
        public List<Frame> frames = new List<Frame>();
    }
    HomeStoreSaveState store;
    string breed;
    float timeScale, capture;
    CatMovement cat;
    Animator sourceAnimator;
    AnimationClip sourceJump;
    Transform[] sourceBones, actualBones;
    static string Folder => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Temp/JumpContinuity");
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        store = HomeStoreService.CaptureState(); breed = CatBreedService.SelectedBreedId;
        timeScale = Time.timeScale; capture = Time.captureDeltaTime;
        Time.timeScale = 1; Time.captureFramerate = 24;
    }
    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(store); CatBreedService.Select(breed);
        Time.timeScale = timeScale; Time.captureDeltaTime = capture;
    }
    IEnumerator Prepare(string scene, string room)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.GetRoomCollection(room).ToArray();
        HomeStoreService.ApplySavedState(state); CatBreedService.Select("oriental-shorthair"); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>(); cat.GetComponent<CatIdleBehavior>().enabled = false;
        var celebration = Object.FindAnyObjectByType<HomeLevelUpCelebrationView>();
        if (celebration != null && HomeLevelUpCelebrationView.IsAnyOpen)
        {
            typeof(HomeLevelUpCelebrationView).GetMethod("BeginClose", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(celebration, null);
            yield return new WaitForSecondsRealtime(.3f);
        }
    }
    IEnumerator Record(string room, string id, CatActivity selected = null, bool verify = false)
    {
        var activity = selected != null ? selected : CatActivity.Registered.Single(a => a.StoreProductId == id);
        var cc = cat.GetComponent<CharacterController>(); cc.enabled = false;
        var p = activity.RoutineEntryPoint.position; p.y = cat.transform.position.y;
        cat.transform.SetPositionAndRotation(p, Quaternion.identity); cc.enabled = true; Physics.SyncTransforms();
        yield return null;
        RoomPlayModeSupport.ProvisionNeeds();
        var trace = new Trace {room = room, product = id, breed = CatBreedService.SelectedBreedId};
        var animation = cat.GetComponent<CatActivityAnimation>(); var animator = cat.GetComponentInChildren<Animator>();
        var hips = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine");
        var shoulders = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.003");
        var paws = new[] {"DEF-hand.L", "DEF-hand.R", "DEF-foot.L", "DEF-foot.R"}.Select(n => CatBreedVisualFactory.FindDescendant(cat.transform, n)).ToArray();
        var hindJoints = new[] {"DEF-thigh.L", "DEF-thigh.R", "DEF-shin.L", "DEF-shin.R"}
            .Select(n => CatBreedVisualFactory.FindDescendant(cat.transform, n)).ToArray();
        Action<CatActivity> completed = a => { if (a == activity) trace.completed++; }; CatActivity.Completed += completed;
        Assert.That(activity.TryStart(cat), Is.True, id);
        animation = cat.GetComponent<CatActivityAnimation>();
        float start = Time.time, deadline = Time.realtimeSinceStartup + 30; int after = 0;
        try
        {
            while ((activity.IsRunning || after < 15) && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                if (!activity.IsRunning) after++;
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                var turning = cat.GetComponent<CatSurfaceTurnMotion>();
                trace.frames.Add(new Frame { time = Time.time - start, root = cat.transform.position, visual = animator.transform.position,
                    visualLocal = animator.transform.localPosition, nativeJump = animation.IsNativeJump, nativePhase = animation.NativeJumpPhase,
                    hips = hips.position, shoulders = shoulders.position, leftHand = paws[0].position, rightHand = paws[1].position,
                    leftFoot = paws[2].position, rightFoot = paws[3].position, pose = animation.CurrentPose.ToString(),
                    leftHip = hindJoints[0].position, rightHip = hindJoints[1].position,
                    leftKnee = hindJoints[2].position, rightKnee = hindJoints[3].position,
                    clip = clips.Length == 0 ? "none" : clips[0].clip.name, phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    running = activity.IsRunning, rest = activity.IsWaitingForRestStop, forward = cat.transform.forward,
                    turning = turning != null && turning.IsTurning,
                    turnSteps = turning != null ? turning.CompletedSteps : 0,
                    turnContactError = turning != null && turning.IsTurning ? turning.MaximumContactError : 0f });
                if (sourceAnimator != null && animation.IsNativeJump)
                {
                    sourceJump.SampleAnimation(sourceAnimator.gameObject, sourceJump.length * animation.NativeJumpPhase);
                    for (int bone = 0; bone < sourceBones.Length; bone++)
                    {
                        Assert.That(Quaternion.Angle(actualBones[bone].localRotation, sourceBones[bone].localRotation),
                            Is.LessThan(.06f), id + " changes native joint rotation: " + actualBones[bone].name);
                        Assert.That(Vector3.Distance(actualBones[bone].localPosition, sourceBones[bone].localPosition),
                            Is.LessThan(.0001f), id + " changes native bone length: " + actualBones[bone].name);
                    }
                }
                if (activity.IsWaitingForRestStop && activity.RestingSeconds > 1.2f) activity.RequestRestStop();
            }
        }
        finally
        {
            CatActivity.Completed -= completed; Directory.CreateDirectory(Folder);
            trace.controllerFloorClear = CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position);
            File.WriteAllText(Folder + "/" + id + "-motion.json", JsonUtility.ToJson(trace, true));
        }
        Assert.That(trace.completed, Is.EqualTo(1), id);
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        if (verify) Verify(trace);
    }

    static void Verify(Trace trace)
    {
        int landingFrames = 0, releases = 0;
        for (int i = 1; i < trace.frames.Count; i++)
        {
            var a = trace.frames[i - 1]; var b = trace.frames[i];
            if (a.nativeJump && b.nativeJump && b.pose == nameof(CatActivityPose.TowelJumpUp) &&
                a.nativePhase >= CatJumpMotion.Takeoff && b.nativePhase < CatJumpMotion.Touchdown && b.nativePhase > a.nativePhase)
            {
                Assert.That(Vector3.Angle(a.forward, b.forward), Is.LessThan(.1f), trace.product + " steers upward flight");
                var fromBody = a.shoulders - a.hips; fromBody.y = 0;
                var toBody = b.shoulders - b.hips; toBody.y = 0;
                Assert.That(Vector3.Angle(fromBody, toBody), Is.LessThan(2f), trace.product + " visible torso steers upward flight");
            }
            if (b.turning)
            {
                Assert.That(b.turnContactError, Is.LessThan(.025f), trace.product + " planted turning paw cannot reach support");
                if (b.turnSteps > 0) VerifyLegAnatomy(trace.product, b);
            }
            if (a.nativeJump && b.nativeJump && a.nativePhase >= CatJumpMotion.Touchdown && b.nativePhase >= a.nativePhase)
            {
                landingFrames++;
                Assert.That(Vector3.Distance(a.root, b.root), Is.LessThan(.0001f), trace.product + " root slides after touchdown");
                var delta = b.visualLocal - a.visualLocal; delta.y = 0f;
                Assert.That(delta.magnitude, Is.LessThan(.0001f), trace.product + " visual slides after touchdown");
                Assert.That(b.clip.EndsWith("|Jump", StringComparison.Ordinal), Is.True, trace.product + " original Jump required");
            }
            if (a.nativeJump && a.nativePhase >= .97f && !b.nativeJump)
            {
                releases++;
                Assert.That(Vector3.Distance(a.hips, b.hips), Is.LessThan(.09f), trace.product + " body pops when jump ownership ends");
            }
            if (!a.nativeJump && b.nativeJump && b.nativePhase < .05f)
                Assert.That(Vector3.Distance(a.hips, b.hips), Is.LessThan(.09f), trace.product + " body pops into jump preparation");
            if (!a.running && !b.running)
                Assert.That(Vector3.Distance(a.root,b.root), Is.LessThan(.003f), trace.product + " controller moves the cat after landing");
        }
        Assert.That(landingFrames, Is.GreaterThan(5), trace.product + " no measured jump landing");
        Assert.That(releases, Is.GreaterThan(0), trace.product + " no measured jump handoff");
        Assert.That(trace.controllerFloorClear, Is.True, trace.product + " landing needs full turning clearance");
        VerifyGroundedTurn(trace);
    }

    static void VerifyLegAnatomy(string product, Frame frame)
    {
        Vector3 right = Vector3.Cross(Vector3.up, frame.forward).normalized;
        Assert.That(Vector3.Dot(frame.rightFoot - frame.leftFoot, right), Is.GreaterThan(.025f),
            product + " crosses or stacks its hind paws while turning");
        foreach (var chain in new[] {
            new[] {frame.leftHip, frame.leftKnee, frame.leftFoot},
            new[] {frame.rightHip, frame.rightKnee, frame.rightFoot}})
        {
            Vector3 bend = Vector3.ProjectOnPlane(chain[1] - chain[0], (chain[2] - chain[0]).normalized);
            if (bend.magnitude < .015f) continue; // A straight leg has no measurable bend plane.
            Assert.That(Mathf.Abs(Vector3.Dot(bend.normalized, right)), Is.LessThan(.45f),
                product + " bends a hind knee sideways instead of in its anatomical plane");
            Assert.That(Vector3.Dot(bend.normalized, frame.forward), Is.GreaterThan(.45f),
                product + " reverses a hind knee's bend direction");
        }
    }

    static void VerifyGroundedTurn(Trace trace)
    {
        var turn = new List<Frame>();
        foreach (var frame in trace.frames)
        {
            if (frame.turning) { turn.Add(frame); continue; }
            if (turn.Count == 0) continue;
            var rates = new List<float>();
            for (int i = 1; i < turn.Count; i++)
            {
                var a = turn[i - 1]; var b = turn[i];
                if (a.turnSteps < 1) continue;
                // Measure the rendered bones, not the solver's requested targets.
                // A boundary frame may finish one paw and start the next. Between
                // boundaries, the other three paws must stay planted in the world.
                if (a.turnSteps == b.turnSteps)
                {
                    int moving = 0;
                    if (Vector3.Distance(a.leftHand, b.leftHand) > .0025f) moving++;
                    if (Vector3.Distance(a.rightHand, b.rightHand) > .0025f) moving++;
                    if (Vector3.Distance(a.leftFoot, b.leftFoot) > .0025f) moving++;
                    if (Vector3.Distance(a.rightFoot, b.rightFoot) > .0025f) moving++;
                    Assert.That(moving, Is.LessThanOrEqualTo(1), trace.product + " turns with simultaneous paw stamps or sliding supports");
                }
                rates.Add(Vector3.Angle(a.forward, b.forward) / Mathf.Max(.001f, b.time - a.time));
            }
            if (rates.Count > 12 && Vector3.Angle(turn[0].forward, turn[turn.Count - 1].forward) > 20f)
            {
                float average = rates.Average();
                foreach (float speed in rates.Skip(rates.Count / 4).Take(rates.Count / 2))
                    Assert.That(speed, Is.GreaterThan(average * .3f), trace.product + " stops its body at each footfall");
            }
            turn.Clear();
        }
    }

    [UnityTest] public IEnumerator RepresentativeJumps_RecordActualVisibleMotion()
    {
        yield return Prepare("Bedroom_Level01", HomeRoomService.BedroomId);
        yield return Record(HomeRoomService.BedroomId, HomeStoreService.BedroomWindowDaybedId);
        yield return Prepare("Bathroom_Level01", HomeRoomService.BathroomId);
        yield return Record(HomeRoomService.BathroomId, HomeStoreService.BathroomTowelStorageId);
        yield return Record(HomeRoomService.BathroomId, HomeStoreService.BathroomTubId);
        yield return Prepare("Kitchen_Level01", HomeRoomService.KitchenId);
        yield return Record(HomeRoomService.KitchenId, HomeStoreService.KitchenIslandId);
    }

    [UnityTest] public IEnumerator ReportedPerchTurns_TenBreeds_PreserveLegAnatomy()
    {
        foreach (var pair in new[] {
            new[] {HomeRoomService.BedroomId, "Bedroom_Level01", HomeStoreService.BedroomStarCanopyId},
            new[] {"second-floor-01", "SecondFloor_Level01", "loft.tall-bookcase"}})
        {
            var room = HomeRoomService.Rooms.Single(r => r.Id == pair[0]);
            yield return Prepare(room.SceneName, room.Id);
            var activity = CatActivity.Registered.Single(a => a.StoreProductId == pair[2]);
            foreach (var entry in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(entry.Id); yield return null; yield return null;
                yield return Record(room.Id, pair[2] + "-anatomy-" + entry.Id, activity, true);
            }
        }
    }

    [UnityTest] public IEnumerator NarrowFurnitureTurns_TenBreeds_PlantPawsAndKeepOriginalJumps()
    {
        foreach (var pair in new[] {
            new[] {HomeRoomService.LivingRoomId, "LivingRoom_Level01", "room.armchair"},
            new[] {HomeRoomService.BedroomId, "Bedroom_Level01", HomeStoreService.BedroomStarCanopyId},
            new[] {HomeRoomService.BalconyId, "Balcony_Level01", "balcony.hanging-chair"}})
        {
            yield return Prepare(pair[1], pair[0]);
            var activity = CatActivity.Registered.Single(a => a.StoreProductId == pair[2]);
            foreach (var entry in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(entry.Id); yield return null; yield return null;
                yield return Record(pair[0], pair[2] + "-" + entry.Id, activity, true);
            }
        }
    }

    [UnityTest] public IEnumerator SurfaceTurn_PauseAndCancel_KeepPoseAndReleaseControl()
    {
        yield return Prepare("Bedroom_Level01", HomeRoomService.BedroomId);
        var activity = CatActivity.Registered.Single(a => a.StoreProductId == HomeStoreService.BedroomWindowDaybedId);
        var cc = cat.GetComponent<CharacterController>(); cc.enabled = false;
        var entry = activity.RoutineEntryPoint.position; entry.y = cat.transform.position.y;
        cat.transform.position = entry; cc.enabled = true; Physics.SyncTransforms();
        RoomPlayModeSupport.ProvisionNeeds(); yield return null;
        var visual = cat.GetComponentInChildren<Animator>().transform;
        Vector3 visualStart = visual.localPosition;
        Assert.That(activity.TryStart(cat), Is.True);
        float deadline = Time.realtimeSinceStartup + 15;
        var animation=cat.GetComponent<CatActivityAnimation>();
        var previous=cat.transform.rotation;bool turning=false;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            turning=cat.transform.position.y>.12f&&animation.IsNativeJump&&animation.NativeJumpPhase>=.99f&&Quaternion.Angle(previous,cat.transform.rotation)>1f;
            if(turning)break;
            previous=cat.transform.rotation;
        }
        Assert.That(turning, Is.True, "No direct supported turn observed");
        Time.timeScale = 0;
        var bones = new[] {"DEF-spine", "DEF-hand.L", "DEF-hand.R", "DEF-foot.L", "DEF-foot.R"}
            .Select(n => CatBreedVisualFactory.FindDescendant(cat.transform, n)).ToArray();
        var positions = bones.Select(b => b.position).ToArray();
        yield return new WaitForSecondsRealtime(.15f);
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < bones.Length; i++)
            Assert.That(Vector3.Distance(positions[i], bones[i].position), Is.LessThan(.0001f), "Paused turning pose moved: " + bones[i].name);
        CatActionState.CancelForTransition(cat); Time.timeScale = 1;
        yield return null; yield return null;
        Assert.That(activity.IsRunning||animation.IsNativeJump, Is.False);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cc.enabled, Is.True);
        Assert.That(Vector3.Distance(visualStart, visual.localPosition), Is.LessThan(.001f), "Turn leaves visual offset after cancel");
        Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True);
    }

    [UnityTest,Timeout(180000)] public IEnumerator RaisedPlanters_TenBreeds_UseOriginalJumpAndContinuousLanding()
    {
        foreach(string roomId in new[]{HomeRoomService.GardenId,HomeRoomService.BalconyId,HomeRoomService.PatioId})
        {
            var room=HomeRoomService.Rooms.Single(r=>r.Id==roomId);yield return Prepare(room.SceneName,roomId);
            var planter=CatActivity.Registered.OfType<LitterDigActivity>().Single(a=>a.UsesRaisedPlanter);
            var catalog=CatBreedCatalog.Load();
            for(int index=0;index<catalog.Count;index++)
            {
                CatBreedService.Select(catalog.Get(index).Id);yield return null;yield return null;
                yield return Record(roomId,planter.StoreProductId+"-"+catalog.Get(index).Id,planter,true);
                VerifyDirectTurn(JsonUtility.FromJson<Trace>(File.ReadAllText(Folder+"/"+planter.StoreProductId+"-"+catalog.Get(index).Id+"-motion.json")),false);
            }
        }
    }

    [UnityTest] public IEnumerator EveryRoom_AllJumpingFurniture_HoldsLandingAndReleasesContinuously()
    {
        var errors = new List<string>(); int count = 0;
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return Prepare(room.SceneName, room.Id);
            var activities = CatActivity.Registered.Where(a => a.IsUnlocked && a.IsContentVisible &&
                (a is PerchNapActivity || a is TowelNestActivity || a is TubEdgeWalkActivity || a is SinkSipActivity ||
                 a is SwingRideActivity || a is PantryClimbActivity || a is LivingFurnitureActivity ||
                 a is SurfaceScatterActivity || a is HamperDiveActivity || (a is KnockOffActivity knock && knock.PerchPoint != null) ||
                 (a is CanopyNapActivity canopy && canopy.NestPoint.position.y > .12f) || a is GardenYarnChaseActivity ||
                 (a is LitterDigActivity planter && planter.UsesRaisedPlanter))).ToArray();
            foreach (var activity in activities)
            {
                string id = string.IsNullOrEmpty(activity.StoreProductId) ? activity.ActivityId : activity.StoreProductId;
                yield return Record(room.Id, id, activity);
                var trace = JsonUtility.FromJson<Trace>(File.ReadAllText(Folder + "/" + id + "-motion.json"));
                try { Verify(trace); } catch (AssertionException e) { errors.Add(e.Message); }
                count++;
            }
        }
        File.WriteAllText(Folder + "/all-rooms-jump-summary.txt", "Routines: " + count + "\n" + string.Join("\n", errors));
        Assert.That(count, Is.GreaterThanOrEqualTo(20));
        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    [UnityTest] public IEnumerator TenBreeds_At15_30_60Fps_PreserveNativeSkeletonAndLanding()
    {
        yield return Prepare("Bedroom_Level01", HomeRoomService.BedroomId);
        var catalog = CatBreedCatalog.Load();
        foreach (var entry in catalog.Entries)
        {
            CatBreedService.Select(entry.Id); yield return null; yield return null;
            var reference = CatBreedVisualFactory.Create(entry, catalog.GameplayController, null, "Native jump reference");
            reference.transform.position = Vector3.one * 500f;
            sourceAnimator = reference.GetComponentInChildren<Animator>(); sourceAnimator.enabled = false;
            sourceJump = catalog.GameplayController.animationClips.Single(c => c.name.EndsWith("|Jump", StringComparison.Ordinal));
            var names = new[] {"DEF-spine", "DEF-spine.003", "DEF-upper_arm.L", "DEF-forearm.L", "DEF-hand.L", "DEF-hand.R", "DEF-thigh.L", "DEF-shin.L", "DEF-foot.L", "DEF-foot.R"};
            actualBones = names.Select(n => CatBreedVisualFactory.FindDescendant(cat.transform,n)).Where(t=>t!=null).ToArray();
            sourceBones = actualBones.Select(b => CatBreedVisualFactory.FindDescendant(reference.transform,b.name)).ToArray();
            Assert.That(actualBones.Length, Is.GreaterThanOrEqualTo(6));
            try
            {
                foreach (int fps in new[] {15,30,60})
                {
                    Time.captureFramerate = fps;
                    yield return Record(HomeRoomService.BedroomId, HomeStoreService.BedroomWindowDaybedId, null, true);
                    File.Copy(Folder + "/" + HomeStoreService.BedroomWindowDaybedId + "-motion.json",
                        Folder + "/" + entry.Id + "-" + fps + "fps-motion.json", true);
                }
            }
            finally { sourceAnimator = null; Object.Destroy(reference); }
        }
    }

    [UnityTest] public IEnumerator LivingToyMouse_TenBreeds_HopAndReturnWithoutSliding()
    {
        yield return Prepare("LivingRoom_Level01", HomeRoomService.LivingRoomId);
        var state = HomeStoreService.CaptureState();
        var cats = HomeStoreService.Products.Where(p => CatCollectionPolicy.IsCatItem(p.Id)).Select(p=>p.Id).ToArray();
        state.ownedProductIds = state.ownedProductIds.Concat(cats).Distinct().ToArray();
        state.storedProductIds = cats;
        HomeStoreService.ApplySavedState(state);
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.ToyMouseId, false), Is.True);
        yield return null; yield return null;
        foreach (var entry in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(entry.Id); yield return null; yield return null;
            yield return Record(HomeRoomService.LivingRoomId, HomeStoreService.ToyMouseId, null, true);
            File.Copy(Folder + "/" + HomeStoreService.ToyMouseId + "-motion.json", Folder + "/" + entry.Id + "-toy-motion.json", true);
        }
    }
}

