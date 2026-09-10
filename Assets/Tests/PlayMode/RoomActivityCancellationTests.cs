using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RoomActivityCancellationTests
{
    static readonly string[] Rooms = {
        "LivingRoom_Level01", "Bathroom_Level01", "Kitchen_Level01", "Bedroom_Level01",
        "Garden_Level01", "Balcony_Level01", "Patio_Level01", "SecondFloor_Level01"
    };
    HomeStoreSaveState savedStore;
    float savedSpeed;
    GameObject needsHost;
    int completed;

    [SetUp] public void Before()
    {
        savedStore = HomeStoreService.CaptureState();
        savedSpeed = Time.timeScale;
        CatActivity.Completed += OnCompleted;
    }

    [TearDown] public void After()
    {
        if (CatActivity.Active != null) CatActivity.Active.CancelForTransition();
        CatActivity.Completed -= OnCompleted;
        Time.timeScale = savedSpeed;
        RoomPlayModeSupport.ReleaseRoom();
        if (needsHost != null) Object.DestroyImmediate(needsHost);
        HomeStoreService.ApplySavedState(savedStore);
    }

    void OnCompleted(CatActivity activity) { completed++; }

    IEnumerator Prepare(string room)
    {
        Time.timeScale = 1f;
        yield return RoomPlayModeSupport.LoadRoomAlone(room);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p =>
            CatCollectionPolicy.IsCatItem(p.Id) ||
            HomeRoomService.Rooms.Any(r => HomeStoreService.IsProductInRoomCollection(r.Id, p.Id)))
            .Select(p => p.Id).ToArray();
        state.storedProductIds = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state);
        needsHost = new GameObject("Cancellation test needs");
        if (Object.FindAnyObjectByType<HungerSystem>() == null) needsHost.AddComponent<HungerSystem>();
        if (Object.FindAnyObjectByType<ThirstSystem>() == null) needsHost.AddComponent<ThirstSystem>();
        yield return null;
        yield return null;
        Physics.SyncTransforms();
    }

    static void Move(CatMovement cat, Vector3 point)
    {
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        point.y = .05f;
        cat.transform.position = point;
        cat.transform.rotation = Quaternion.identity;
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    sealed class PropPose
    {
        public Transform target;
        public Vector3 position;
        public Quaternion rotation;
    }

    static List<PropPose> CaptureProps(CatActivity activity)
    {
        var result = new List<PropPose>();
        foreach (var field in activity.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (field.FieldType != typeof(Transform)) continue;
            if (!field.Name.EndsWith("Pivot") && field.Name != "cartVisual" &&
                field.Name != "movingPart" && field.Name != "hangingToy" && field.Name != "toy") continue;
            var target = (Transform)field.GetValue(activity);
            if (target != null) result.Add(new PropPose {
                target = target, position = target.localPosition, rotation = target.localRotation
            });
        }
        return result;
    }

    static void AssertPropsRestored(IEnumerable<PropPose> props, string label)
    {
        foreach (var prop in props)
        {
            Assert.That(Vector3.Distance(prop.target.localPosition, prop.position), Is.LessThan(.001f), label + " prop position " + prop.target.name);
            Assert.That(Quaternion.Angle(prop.target.localRotation, prop.rotation), Is.LessThan(.1f), label + " prop rotation " + prop.target.name);
        }
    }

    static string TraceState(string label, string phase, CatMovement cat)
    {
        var cc = cat.GetComponent<CharacterController>();
        var p = cat.transform.position;
        var q = cat.transform.rotation;
        var s = cat.transform.localScale;
        var center = cat.transform.TransformPoint(cc.center);
        return string.Join(",", new object[] {
            label, phase, Time.frameCount, Time.deltaTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            p.x.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), p.y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), p.z.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            q.x.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), q.y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), q.z.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), q.w.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            s.x.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), s.y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), s.z.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            center.x.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), center.y.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), center.z.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            cc.radius.ToString("F6", System.Globalization.CultureInfo.InvariantCulture), cc.height.ToString("F6", System.Globalization.CultureInfo.InvariantCulture),
            cc.enabled, cc.isGrounded, cat.enabled, cat.IsMovementPhysicallyLocked, CatBreedService.SelectedBreedId
        });
    }

    static string RejectedStart(CatActivity activity, CatMovement cat)
    {
        var energy = Object.FindAnyObjectByType<EnergySystem>();
        var arguments = new object[] { null };
        bool canBegin = (bool)activity.GetType().GetMethod("CanBeginActivity", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(activity, arguments);
        return activity.Kind + " at=" + cat.transform.position.ToString("F6") +
            " busy=" + CatActionState.IsBusy(cat) + " worldBlocked=" + cat.AreWorldActionsBlocked +
            " unlocked=" + activity.IsUnlocked + " active=" + activity.isActiveAndEnabled +
            " energy=" + (energy != null ? energy.CurrentEnergy.ToString() : "missing") + " cost=" + activity.EnergyCost +
            " clear=" + CatActivityMotion.IsFloorClear(cat.transform.position) +
            " nearby=" + activity.TryGetPromptDistance(cat, out _) + " canBegin=" + canBegin + " reason=" + arguments[0];
    }

    static bool StartAtReachableEntrance(CatActivity activity, CatMovement cat, List<string> attempts)
    {
        if (activity.TryStart(cat)) return true;
        attempts.Add(RejectedStart(activity, cat));
        var renderers = activity.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
        if (renderers.Length == 0) return false;
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        var boundary = HomeRoomBoundary.FindFor(cat.gameObject.scene);
        var controller = cat.GetComponent<CharacterController>();
        float radius = controller.radius * Mathf.Max(cat.transform.lossyScale.x, cat.transform.lossyScale.z);
        foreach (float gap in new[] { .36f, .55f, .72f })
        for (int side = 0; side < 16; side++)
        {
            Vector3 outward = Quaternion.Euler(0f, side * 22.5f, 0f) * Vector3.forward;
            Vector3 point = bounds.ClosestPoint(bounds.center + outward * 10f) + outward * gap;
            point.y = .05f;
            if (boundary != null && Vector3.Distance(point, boundary.ClampPosition(point, radius + .03f)) > .001f) continue;
            if (!CatActivityMotion.IsFloorClear(point)) continue;
            Move(cat, point);
            if (!activity.TryGetPromptDistance(cat, out _)) continue;
            if (activity.TryStart(cat))
            {
                attempts.Add(activity.Kind + " accepted reachable edge at=" + point.ToString("F6"));
                return true;
            }
            attempts.Add(RejectedStart(activity, cat));
        }
        return false;
    }

    [UnityTest]
    public IEnumerator EightRooms_CancelKeepsRoutinesStopped_AndIdleDisableCannotTakeAnotherLock()
    {
        var checkedProducts = new HashSet<string>();
        var checkedTypes = new HashSet<System.Type>();
        var report = new List<string> { "room,product,activity,cancelled,physicsReleased,propsRestored,noLateReward,idleDisableSafe" };
        var trace = new List<string> { "room,product,activity,phase,frame,dt,x,y,z,qx,qy,qz,qw,sx,sy,sz,ccWorldX,ccWorldY,ccWorldZ,ccRadius,ccHeight,ccEnabled,grounded,movementEnabled,physicallyLocked,breed" };
        var entryAttempts = new List<string>();
        const string output = "Docs/QA/ACTION_STATE_AUDIT_2026-09-09";
        System.IO.Directory.CreateDirectory(output);
        System.IO.File.WriteAllLines(output + "/room-cancellation.csv", report);
        foreach (var room in Rooms)
        {
            yield return Prepare(room);
            var cat = Object.FindFirstObjectByType<CatMovement>();
            // Spontaneous bird/idle turns are independent gameplay. Their yaw
            // swings the capsule's .12 local Z offset and can legitimately move
            // its root during collision recovery. This fixture tests cancellation
            // lifetime and resumed idle gravity with a deterministic facing.
            foreach (var idle in Object.FindObjectsByType<CatIdleBehavior>(FindObjectsSortMode.None)) idle.enabled = false;
            foreach (var attention in Object.FindObjectsByType<GardenBirdAttention>(FindObjectsSortMode.None)) attention.enabled = false;
            var controller = cat.GetComponent<CharacterController>();
            var activities = CatActivity.Registered.Where(a => a.gameObject.scene == cat.gameObject.scene &&
                !a.IsRetired && (HomeStoreService.IsFixedRoomProduct(a.StoreProductId) || a is LivingFurnitureActivity)).ToArray();
            Assert.That(activities, Is.Not.Empty, room);
            foreach (var activity in activities)
            {
                string label = room + "/" + activity.Kind;
                RoomPlayModeSupport.ProvisionNeeds();
                var hunger = Object.FindAnyObjectByType<HungerSystem>();
                var thirst = Object.FindAnyObjectByType<ThirstSystem>();
                var energy = Object.FindAnyObjectByType<EnergySystem>();
                hunger.ApplySavedValue(35f);
                thirst.ApplySavedValue(35f);
                Move(cat, activity.RoutineEntryPoint.position);
                string traceLabel = room + "," + activity.StoreProductId + "," + activity.Kind;
                trace.Add(TraceState(traceLabel, "before", cat));
                System.IO.File.WriteAllLines(output + "/room-cancellation-trace.csv", trace);
                Vector3 originalScale = cat.transform.localScale;
                var props = CaptureProps(activity);
                Time.timeScale = 4f;
                bool started = StartAtReachableEntrance(activity, cat, entryAttempts);
                System.IO.File.WriteAllLines(output + "/room-cancellation-entrances.txt", entryAttempts);
                trace.Add(TraceState(traceLabel, started ? "started" : "start-rejected", cat));
                System.IO.File.WriteAllLines(output + "/room-cancellation-trace.csv", trace);
                Assert.That(started, Is.True, label + " start: " + string.Join("\n", entryAttempts.Skip(Mathf.Max(0, entryAttempts.Count - 4))));
                yield return new WaitForSeconds(1.1f);
                Assert.That(activity.IsRunning, Is.True, label + " must reach an interruptible phase");
                Assert.That(cat.IsMovementPhysicallyLocked, Is.True, label + " routine must own movement");

                // A room/menu transition cancels while the component is still enabled.
                activity.CancelForTransition();
                trace.Add(TraceState(traceLabel, "cancel", cat));
                System.IO.File.WriteAllLines(output + "/room-cancellation-trace.csv", trace);
                Assert.That(activity.enabled, Is.True, label + " cancellation must not require disabling a product");
                Assert.That(activity.IsRunning, Is.False, label);
                Assert.That(CatActivity.Active, Is.Null, label);
                Assert.That(controller.enabled, Is.True, label + " controller");
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False, label + " movement lock");
                Assert.That(cat.transform.localScale, Is.EqualTo(originalScale), label + " scale");
                Assert.That(Vector3.Dot(cat.transform.up, Vector3.up), Is.GreaterThan(.999f), label + " upright");
                AssertPropsRestored(props, label);

                Vector3 cancelledAt = cat.transform.position;
                float hungerAfter = hunger.CurrentHunger, thirstAfter = thirst.CurrentThirst, energyAfter = energy.CurrentEnergy;
                int completedAfter = completed;
                // The actual controller can depenetrate slightly on resuming
                // idle gravity (BookSet: 6.78 cm in one Move). Furniture-owned
                // coroutines are independent of CatMovement, so isolate their
                // lifetime first, then verify resumed controller stability.
                bool movementWasEnabled = cat.enabled;
                cat.enabled = false;
                try { yield return new WaitForSeconds(6f); }
                finally { cat.enabled = movementWasEnabled; }
                trace.Add(TraceState(traceLabel, "frozen-end", cat));
                System.IO.File.WriteAllLines(output + "/room-cancellation-trace.csv", trace);
                Vector3 movement = cat.transform.position - cancelledAt; movement.y = 0;
                Assert.That(movement.magnitude, Is.LessThan(.001f), label + " abandoned coroutine moved the cat with idle locomotion disabled");
                Assert.That(hunger.CurrentHunger, Is.LessThanOrEqualTo(hungerAfter + .01f), label + " late meal reward");
                Assert.That(thirst.CurrentThirst, Is.LessThanOrEqualTo(thirstAfter + .01f), label + " late water reward");
                Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(energyAfter + .01f), label + " late rest reward");
                Assert.That(completed, Is.EqualTo(completedAfter), label + " late completion");
                AssertPropsRestored(props, label);
                float idleTime = 0;
                while (idleTime < .4f)
                {
                    yield return null; idleTime += Time.deltaTime;
                    if (activity.Kind == CatActivityKind.DaisyRoll)
                        trace.Add(TraceState(traceLabel, "idle-settle-" + idleTime.ToString("F4", System.Globalization.CultureInfo.InvariantCulture), cat));
                }
                Vector3 settledAt = cat.transform.position;
                trace.Add(TraceState(traceLabel, "settled", cat));
                System.IO.File.WriteAllLines(output + "/room-cancellation-trace.csv", trace);
                Assert.That(CatActivityMotion.IsFloorClear(settledAt, .24f), Is.True, label + " controller must resume on clear floor");
                idleTime = 0;
                while (idleTime < .4f)
                {
                    yield return null; idleTime += Time.deltaTime;
                    if (activity.Kind == CatActivityKind.DaisyRoll)
                        trace.Add(TraceState(traceLabel, "idle-observe-" + idleTime.ToString("F4", System.Globalization.CultureInfo.InvariantCulture), cat));
                }
                trace.Add(TraceState(traceLabel, "idle-end", cat));
                System.IO.File.WriteAllLines(output + "/room-cancellation-trace.csv", trace);
                Vector3 settling = cat.transform.position - settledAt; settling.y = 0;
                Assert.That(settling.magnitude, Is.LessThan(.025f), label + " resumed idle controller must become stable");

                // Its cached controller and scale belong to the finished run. An
                // unrelated current owner must survive this now-idle product closing.
                var nextOwner = new GameObject("Next activity owner");
                cat.SetMovementLocked(nextOwner, true);
                controller.enabled = false;
                Vector3 nextScale = originalScale * .97f;
                cat.transform.localScale = nextScale;
                activity.enabled = false;
                Assert.That(controller.enabled, Is.False, label + " idle disable enabled another owner's physics");
                Assert.That(cat.transform.localScale, Is.EqualTo(nextScale), label + " idle disable restored stale scale");
                Assert.That(cat.IsMovementPhysicallyLocked, Is.True, label + " idle disable released another owner");
                cat.transform.localScale = originalScale;
                cat.SetMovementLocked(nextOwner, false);
                controller.enabled = true;
                Object.DestroyImmediate(nextOwner);
                activity.enabled = true;
                checkedTypes.Add(activity.GetType());
                if (HomeStoreService.IsFixedRoomProduct(activity.StoreProductId)) checkedProducts.Add(activity.StoreProductId);
                report.Add(room + "," + activity.StoreProductId + "," + activity.Kind + ",true,true,true,true,true");
                trace.Add(TraceState(traceLabel, "pass", cat));
                System.IO.File.WriteAllLines(output + "/room-cancellation.csv", report);
                System.IO.File.WriteAllLines(output + "/room-cancellation-trace.csv", trace);
            }
        }
        Assert.That(checkedProducts.Count, Is.EqualTo(78), "All current ROOM actions, including shared implementations, must be covered.");
        Assert.That(checkedTypes.Count, Is.GreaterThanOrEqualTo(20), "The matrix must exercise distinct motion/cleanup implementations.");
        System.IO.Directory.CreateDirectory("Docs/QA/ACTION_STATE_AUDIT_2026-09-09");
        System.IO.File.WriteAllLines("Docs/QA/ACTION_STATE_AUDIT_2026-09-09/room-cancellation.csv", report);
    }

    [UnityTest]
    public IEnumerator StoredToy_StopsImmediately_WithoutLateMotionOrContact()
    {
        yield return Prepare("LivingRoom_Level01");
        var cat = Object.FindFirstObjectByType<CatMovement>();
        var toyIds = HomeStoreService.Products.Where(p => CatCollectionPolicy.IsCatItem(p.Id)).Select(p => p.Id).ToArray();
        Assert.That(toyIds.Length, Is.EqualTo(17));
        foreach (var id in toyIds)
        {
            Assert.That(HomeStoreService.TrySetStored(id, false), Is.True, id);
            yield return null; yield return null;
            var activity = CatActivity.Registered.First(a => a.StoreProductId == id);
            RoomPlayModeSupport.ProvisionNeeds();
            Move(cat, activity.RoutineEntryPoint.position);
            var props = CaptureProps(activity);
            Vector3 scale = cat.transform.localScale;
            Time.timeScale = 2f;
            Assert.That(activity.TryStart(cat), Is.True, id);
            yield return new WaitForSeconds(.65f);
            Assert.That(activity.IsRunning, Is.True, id);
            Assert.That(HomeStoreService.TrySetStored(id, true), Is.True, id + " store while active");
            Assert.That(activity.IsRunning, Is.False, id + " store cancellation");
            Assert.That(CatActivity.Active, Is.Null, id);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False, id);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True, id);
            Assert.That(cat.transform.localScale, Is.EqualTo(scale), id);
            AssertPropsRestored(props, id);
            Vector3 stop = cat.transform.position;
            int completedAfter = completed;
            yield return new WaitForSeconds(4f);
            Vector3 movement = cat.transform.position - stop; movement.y = 0;
            Assert.That(movement.magnitude, Is.LessThan(.025f), id + " stale coroutine");
            Assert.That(completed, Is.EqualTo(completedAfter), id + " stale completion");
        }
    }

    [UnityTest]
    public IEnumerator BookSetCancellation_DistinguishesIdleControllerSettling()
    {
        yield return Prepare("LivingRoom_Level01");
        var cat = Object.FindFirstObjectByType<CatMovement>();
        var books = CatActivity.Registered.First(a => a.Kind == CatActivityKind.BookSetSniff);
        RoomPlayModeSupport.ProvisionNeeds();
        Move(cat, books.RoutineEntryPoint.position);
        Time.timeScale = 4f;
        Assert.That(books.TryStart(cat), Is.True);
        yield return new WaitForSeconds(1.1f);
        Assert.That(books.IsRunning, Is.True);
        books.CancelForTransition();
        Vector3 cancelledAt = cat.transform.position;
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        var boundary = (HomeRoomBoundary)typeof(CatMovement).GetField("roomBoundary", fields).GetValue(cat);
        float radius = (float)typeof(CatMovement).GetField("physicalFootprintRadius", fields).GetValue(cat);
        float clearance = (float)typeof(CatMovement).GetField("roomEdgeClearance", fields).GetValue(cat);
        Vector3 clampTarget = boundary != null ? boundary.ClampPosition(cancelledAt, radius + clearance) : cancelledAt;
        var idleMove = typeof(CatMovement).GetMethod("MoveCat", fields);
        bool wasEnabled = cat.enabled;
        try
        {
            // Derived routine coroutines run on the furniture component, not
            // CatMovement. Freeze only idle locomotion to expose orphan writes.
            cat.enabled = false;
            yield return new WaitForSeconds(6f);
            Vector3 frozenAt = cat.transform.position;
            Assert.That(Vector3.Distance(frozenAt, cancelledAt), Is.LessThan(.0001f), "A cancelled furniture coroutine must not move the cat while idle locomotion is disabled.");
            idleMove.Invoke(cat, new object[] { Vector3.zero });
            Vector3 firstIdle = cat.transform.position;
            for (int step = 0; step < 30; step++)
            {
                idleMove.Invoke(cat, new object[] { Vector3.zero });
                yield return null;
            }
            Vector3 settledAt = cat.transform.position;
            string report = "cancel=" + cancelledAt.ToString("F6") + " frozen=" + frozenAt.ToString("F6") +
                " boundary=" + clampTarget.ToString("F6") + " firstIdle=" + firstIdle.ToString("F6") +
                " settled=" + settledAt.ToString("F6") + " radius=" + radius + " edge=" + clearance;
            Debug.Log("BookSet cancellation movement attribution: " + report);
            System.IO.Directory.CreateDirectory("Docs/QA/ACTION_STATE_AUDIT_2026-09-09");
            System.IO.File.WriteAllText("Docs/QA/ACTION_STATE_AUDIT_2026-09-09/bookset-cancellation-motion.txt", report);
            Assert.That(books.IsRunning, Is.False);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(CatActivityMotion.IsFloorClear(settledAt, .24f), Is.True, "Idle controller must settle onto reachable floor.");
        }
        finally { cat.enabled = wasEnabled; }
    }

    [UnityTest]
    public IEnumerator DaisyRollCancellation_RecordsNormalAndAcceleratedIdleSteps()
    {
        var rows = new List<string> { "rate,time,deltaTime,x,y,z,grounded,collisionFlags" };
        var conclusions = new List<string>();
        bool normalStable = false;
        foreach (float rate in new[] { 1f, 4f })
        {
            yield return Prepare("Garden_Level01");
            var cat = Object.FindFirstObjectByType<CatMovement>();
            var activity = CatActivity.Registered.First(a => a.Kind == CatActivityKind.DaisyRoll);
            var controller = cat.GetComponent<CharacterController>();
            RoomPlayModeSupport.ProvisionNeeds();
            Move(cat, activity.RoutineEntryPoint.position);
            Time.timeScale = rate;
            Assert.That(activity.TryStart(cat), Is.True);
            yield return new WaitForSeconds(1.1f);
            Assert.That(activity.IsRunning, Is.True);
            activity.CancelForTransition();
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Vector3 cancelledAt = cat.transform.position;
            bool movementWasEnabled = cat.enabled;
            cat.enabled = false;
            try { yield return new WaitForSeconds(6f); }
            finally { cat.enabled = movementWasEnabled; }
            Assert.That(Vector3.Distance(cat.transform.position, cancelledAt), Is.LessThan(.001f), "Idle input disabled must expose any abandoned routine movement.");
            float elapsed = 0;
            Vector3 point04 = cancelledAt, point08 = cancelledAt, point12 = cancelledAt;
            bool captured04 = false, captured08 = false, captured12 = false;
            while (elapsed < 1.6f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Vector3 point = cat.transform.position;
                rows.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0},{1:F6},{2:F6},{3:F6},{4:F6},{5:F6},{6},{7}", rate, elapsed,
                    Time.deltaTime, point.x, point.y, point.z, controller.isGrounded, (int)controller.collisionFlags));
                if (!captured04 && elapsed >= .4f) { point04 = point; captured04 = true; }
                if (!captured08 && elapsed >= .8f) { point08 = point; captured08 = true; }
                if (!captured12 && elapsed >= 1.2f) { point12 = point; captured12 = true; }
                Assert.That(activity.IsRunning, Is.False);
            }
            Vector3 firstWindow = point08 - point04; firstWindow.y = 0;
            Vector3 lastWindow = cat.transform.position - point12; lastWindow.y = 0;
            string result = "rate=" + rate + " start=" + cancelledAt.ToString("F6") +
                " firstWindow=" + firstWindow.magnitude.ToString("F6") + " lastWindow=" + lastWindow.magnitude.ToString("F6") +
                " end=" + cat.transform.position.ToString("F6");
            conclusions.Add(result);
            Debug.Log("DaisyRoll idle controller sampling: " + result);
            if (rate == 1f)
                normalStable = firstWindow.magnitude < .025f && lastWindow.magnitude < .025f &&
                    CatActivityMotion.IsFloorClear(cat.transform.position, .24f);
        }
        const string output = "Docs/QA/ACTION_STATE_AUDIT_2026-09-09";
        System.IO.Directory.CreateDirectory(output);
        System.IO.File.WriteAllLines(output + "/daisy-idle-sampling.csv", rows);
        System.IO.File.WriteAllLines(output + "/daisy-idle-sampling.txt", conclusions);
        Assert.That(normalStable, Is.True, "Normal-speed cancellation must settle and remain stable within the existing .025 m limit.");
    }

    [UnityTest]
    public IEnumerator RefusedBallGame_DoesNotLeakPrivateCollisionScenes()
    {
        yield return Prepare("LivingRoom_Level01");
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.BallBasketId, false), Is.True);
        yield return null; yield return null;
        var cat = Object.FindFirstObjectByType<CatMovement>();
        var ballGame = CatActivity.Registered.OfType<BallChaseActivity>().First(a => a.StoreProductId == HomeStoreService.BallBasketId);
        Move(cat, ballGame.RoutineEntryPoint.position);
        Object.FindAnyObjectByType<EnergySystem>().ApplySavedValue(0f);
        int countBefore = SceneManager.sceneCount;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.That(ballGame.TryStart(cat), Is.False, "A tired cat must refuse the ball game.");
            yield return null; yield return null;
        }
        float deadline = Time.realtimeSinceStartup + 3f;
        while (SceneManager.sceneCount > countBefore && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(ballGame.IsRunning, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
        Assert.That(SceneManager.sceneCount, Is.EqualTo(countBefore), "Refused starts leaked additive physics-query scenes.");
        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(t => t.name == "Ball query geometry"), Is.Zero);
    }
}
