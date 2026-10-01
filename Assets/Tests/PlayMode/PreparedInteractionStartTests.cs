using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Native checks of player-pose readiness, independent of the old approach grid.</summary>
public sealed class PreparedInteractionStartTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    HomeStoreSaveState saved;
    string savedBreed;
    float capture, timeScale;
    CatMovement cat;
    Mesh skinSample;
    CapsuleCollider controllerProbe;
    readonly Collider[] overlap = new Collider[64];
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<string> failures = new List<string>();
    readonly List<string> rows = new List<string>();
    readonly QaMeshTopologyCache topology = new QaMeshTopologyCache();
    QaExactMeshContact exactMetric;
    QaExactMeshContact.Hit exactLastHit;
    float exactLastRay, exactEvidenceMaximum;
    int exactLastVotes;
    string exactEvidenceKey = string.Empty;
    readonly List<string> exactEvidenceRows = new List<string>();
    readonly Dictionary<string,int> exactEvidenceIndices = new Dictionary<string,int>();
    public string DeepestExactSkinDetail { get; private set; }
    public bool CaptureSkinEvidence { get; set; }
    public string BreedUnderTest { get; set; } = "oriental-shorthair";

    readonly Dictionary<BoxCollider, MeshCollider> visibleProxies = new Dictionary<BoxCollider, MeshCollider>();
    Collider[] roomSolids;
    public void ClearVisibleProxies()
    {
        foreach (var proxy in visibleProxies.Values) if (proxy != null) Object.DestroyImmediate(proxy.gameObject);
        visibleProxies.Clear();
    }
    MeshCollider VisibleProxy(BoxCollider box)
    {
        var filter = box.GetComponent<MeshFilter>();
        var renderer = box.GetComponent<Renderer>();
        if (filter == null || filter.sharedMesh == null || filter.sharedMesh.name == "Cube" || renderer == null || !renderer.enabled) return null;
        if (visibleProxies.TryGetValue(box, out var proxy)) return proxy;
        var holder = new GameObject("QA original rendered navigation-box geometry");
        Object.DontDestroyOnLoad(holder);
        holder.transform.SetPositionAndRotation(new Vector3(9000, -9000, 9000), filter.transform.rotation);
        holder.transform.localScale = filter.transform.lossyScale;
        proxy = holder.AddComponent<MeshCollider>(); proxy.sharedMesh = filter.sharedMesh;
        visibleProxies.Add(box, proxy); Physics.SyncTransforms(); return proxy;
    }
    int positiveStarts, angledStarts, angledRefusals;
    float incomingTurn;
    static readonly Vector3[] InsideDirections = {
        new Vector3(.013f, 1, .027f).normalized, new Vector3(1, .017f, .031f).normalized,
        new Vector3(.019f, .023f, 1).normalized, -new Vector3(.013f, 1, .027f).normalized,
        -new Vector3(1, .017f, .031f).normalized, -new Vector3(.019f, .023f, 1).normalized };

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Prepared-start tests require the copied QA save session.");
        saved = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        capture = Time.captureDeltaTime; timeScale = Time.timeScale;
        Time.timeScale = 1f; Time.captureFramerate = 30;
        skinSample = new Mesh();
        var probe = new GameObject("QA prepared-start controller probe");
        Object.DontDestroyOnLoad(probe); probe.transform.position = new Vector3(0, -12000, 0);
        controllerProbe = probe.AddComponent<CapsuleCollider>();
        controllerProbe.isTrigger = true; controllerProbe.direction = 1;
        failures.Clear(); rows.Clear(); topology.Clear();
        exactMetric = new QaExactMeshContact(topology); exactEvidenceKey=string.Empty;exactEvidenceMaximum=0;CaptureSkinEvidence=false;
        exactEvidenceRows.Clear();exactEvidenceIndices.Clear();
        exactEvidenceRows.Add("activity,frame,phase,skinVertex,skinPoint,collider,exactDistance,metricDetail,png");
        positiveStarts = angledStarts = angledRefusals = 0;
        rows.Add("room,activity,case,kind,prompt,started,reason,x,y,z,yaw,instantShift,instantTurn,preparationShift,preparationTurn,skinDepth,nativeFrames,startMs,incomingTurn");
        Directory.CreateDirectory(Root);
    }

    [TearDown] public void After()
    {
        exactMetric?.Clear(); topology.Clear(); ClearVisibleProxies();
        File.WriteAllLines(Root+"/exact-skin-metric-evidence.csv",exactEvidenceRows);
        Time.timeScale = 1f;
        if (CatActivity.Active != null) CatActivity.Active.CancelForTransition();
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(saved); CatBreedService.Select(savedBreed);
        Time.captureDeltaTime = capture; Time.timeScale = timeScale;
        if (skinSample != null) Object.DestroyImmediate(skinSample);
        if (controllerProbe != null) Object.DestroyImmediate(controllerProbe.gameObject);
        File.WriteAllLines(Root + "/prepared-" + TestContext.CurrentContext.Test.Name + ".csv", rows);
    }

    IEnumerator Prepare(string scene)
    {
        ClearVisibleProxies();
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Select(p => p.Id).ToArray();
        state.storedProductIds = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state); CatBreedService.Select(BreedUnderTest);
        cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(cat, Is.Not.Null);
        yield return QaBreedReadiness.WaitForSelected(cat, BreedUnderTest);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        Assert.That(cat.HasBodyGuardProfile, Is.True, "The measured body profile must be active.");
        RoomPlayModeSupport.ProvisionNeeds();
        roomSolids = Object.FindObjectsByType<Collider>()
            .Where(c => c.enabled && !c.isTrigger && c.gameObject.scene == cat.gameObject.scene &&
                c.GetComponentInParent<CatMovement>() == null && c.bounds.max.y > .12f).ToArray();
    }

    [UnityTest, Timeout(600000)]
    public IEnumerator EightRooms_ThirtyNineJumpEntries_KeepTheAcceptedLaunchPose()
    {
        int inventory = 0, rooms = 0;
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return Prepare(room.SceneName); rooms++;
            var activities = CatActivity.Registered.Where(a => a.gameObject.scene == cat.gameObject.scene &&
                FurnitureBodyClearanceTests.IsJumpActivity(a)).OrderBy(Id).ToArray();
            foreach (var activity in activities)
            {
                inventory++;
                if (!activity.HasPreparedStart)
                {
                    Fail(room.Id + "/" + Id(activity) + " has no prepared-start adapter");
                    Row(room.Id, activity, "centre", default, false, false, "missing-adapter");
                    continue;
                }
                if (!FindReadyPose(activity, out var accepted, out string reason))
                {
                    Fail(room.Id + "/" + Id(activity) + ": " + reason);
                    Row(room.Id, activity, "centre", accepted, false, false, reason);
                    continue;
                }
                // Settle the real CharacterController before evaluating click-time continuity.
                yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
                if (!activity.TryGetStartPose(cat, out accepted))
                {
                    Fail(Id(activity) + " loses readiness when the real controller settles");
                    Row(room.Id, activity, "centre", accepted, false, false, "settled-stance-rejected");
                    continue;
                }
                Vector3 playerPosition = cat.transform.position;
                // The accepted current heading is the mandatory positive for
                // this activity. A different in-cone heading may legitimately
                // fail the measured source preparation/recovery envelope.
                RoomPlayModeSupport.ProvisionNeeds();
                yield return ObserveStart(room.Id, activity, "accepted", accepted);
                Quaternion forward = Facing(playerPosition, accepted.ActionTarget, accepted.Rotation);
                foreach (float angle in new[] { 0f, -15f, 15f })
                {
                    Quaternion heading = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                    Place(playerPosition, heading);
                    RoomPlayModeSupport.ProvisionNeeds();
                    bool ready = activity.TryGetStartPose(cat, out var prepared);
                    bool prompt = activity.TryGetPromptDistance(cat, out _);
                    Check(prompt == ready, Id(activity) + " prompt/preflight disagreement at " + angle);
                    if (!ready)
                    {
                        bool physical = ControllerAndBodyClear(playerPosition, heading) &&
                            cat.IsInteractionPoseClear(playerPosition, heading);
                        CatJumpClearanceResolver.Rejection rejection = default;
                        bool sourceBlocked = prepared.Kind == CatActivityStartKind.GroundLaunch &&
                            !CatJumpClearanceResolver.EndsClear(cat, playerPosition, prepared.ActionTarget,
                                heading, true, true, out rejection);
                        Check(!physical || sourceBlocked, Id(activity) + " refuses a body/controller/source-clear in-cone stance at " + angle);
                        AssertRejectedWithoutMotion(activity, sourceBlocked ? "source-envelope" : "body-controller");
                        if (angle != 0) angledRefusals++;
                        Row(room.Id, activity, "yaw" + angle, prepared, prompt, false,
                            !physical ? "body-controller-blocked" : sourceBlocked ? "source-blocked-phase-" + F(rejection.phase) : "unexpected-refusal");
                        continue;
                    }
                    yield return ObserveStart(room.Id, activity, "yaw" + angle, prepared);
                }
                // Turning away and leaving the zone must not launch an old accepted pose.
                if (accepted.Kind == CatActivityStartKind.GroundLaunch)
                {
                    Place(playerPosition, Quaternion.AngleAxis(90, Vector3.up) * forward);
                    AssertRejectedWithoutMotion(activity, "outside-facing-cone");
                    Row(room.Id, activity, "yaw90", accepted, false, false, "outside-facing-cone");
                }
                Place(accepted.ZoneCentre + Vector3.right * 1.5f + Vector3.up * .05f, forward);
                AssertRejectedWithoutMotion(activity, "outside-use-zone");
            }
        }
        Check(rooms == 8, "Expected all eight rooms, saw " + rooms);
        Check(inventory == 39, "Expected the established 39 jumping routines, saw " + inventory);
        Check(positiveStarts == inventory, "Every inventory entry needs a positive start: " + positiveStarts + "/" + inventory);
        Check(angledStarts > 0, "No angled player stance was exercised");
        File.WriteAllText(Root + "/prepared-jump-summary.txt", "Inventory=" + inventory + "\nPositive=" + positiveStarts +
            "\nAngled=" + angledStarts + "\nBody/controller/source angle refusals=" + angledRefusals + "\n" + string.Join("\n", failures));
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest] public IEnumerator GroundCrawls_StartAtThePlayerPoseBeforeTheirRealEntryMotion()
    {
        int checkedCount = 0;
        foreach (string scene in new[] { "Bathroom_Level01", "Bedroom_Level01" })
        {
            yield return Prepare(scene);
            var activities = CatActivity.Registered.Where(a => a.gameObject.scene == cat.gameObject.scene &&
                ((a is LitterDigActivity litter && !litter.UsesRaisedPlanter) ||
                 (a is CanopyNapActivity canopy && canopy.NestPoint.position.y <= .12f))).ToArray();
            foreach (var activity in activities)
            {
                Assert.That(FindReadyPose(activity, out _, out string reason), Is.True, Id(activity) + " " + reason);
                yield return new WaitForEndOfFrame();
                Assert.That(activity.TryGetStartPose(cat, out var accepted), Is.True, Id(activity));
                yield return ObserveStart(scene, activity, "actual-crawl", accepted);
                checkedCount++;
            }
        }
        // The bedroom canopy's authored nest is .195 m high, so it belongs to
        // the 39 jumping routines. Only the bathroom tray is a ground entry.
        Assert.That(checkedCount, Is.EqualTo(1), "The current ground-entry inventory is the bathroom litter tray.");
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest] public IEnumerator EnergyRejection_AndPausedAirborneCancel_LeaveControlAndStanceConsistent()
    {
        yield return Prepare("LivingRoom_Level01");
        var activity = CatActivity.Registered.First(a => a.gameObject.scene == cat.gameObject.scene &&
            a is LivingFurnitureActivity && a.EnergyCost > 0);
        Assert.That(FindReadyPose(activity, out _, out string reason), Is.True, reason);
        yield return new WaitForEndOfFrame();
        Assert.That(activity.TryGetStartPose(cat, out var accepted), Is.True);
        var energy = RoomPlayModeSupport.ProvisionNeeds(); energy.ApplySavedValue(0);
        Vector3 before = cat.transform.position; Quaternion heading = cat.transform.rotation;
        Assert.That(activity.TryGetPromptDistance(cat, out _), Is.True, "The energy refusal is tested from a physically ready stance");
        Assert.That(activity.TryStart(cat), Is.False);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(0));
        Assert.That(Vector3.Distance(before, cat.transform.position), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.2f));
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat), Is.True);
        float deadline = Time.realtimeSinceStartup + 4;
        while (cat.transform.position.y < accepted.Position.y + .12f && activity.IsRunning && Time.realtimeSinceStartup < deadline)
            yield return new WaitForEndOfFrame();
        Assert.That(cat.transform.position.y, Is.GreaterThan(accepted.Position.y + .12f), "Actual native flight must be observed");
        Time.timeScale = 0;
        Vector3 paused = cat.transform.position; Quaternion pausedRotation = cat.transform.rotation;
        yield return null; yield return null;
        Assert.That(Vector3.Distance(paused, cat.transform.position), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(pausedRotation, cat.transform.rotation), Is.LessThan(.2f));
        activity.CancelForTransition();
        Assert.That(Vector3.Distance(accepted.Position, cat.transform.position), Is.LessThan(.001f), "Airborne cancellation restores the accepted floor");
        Assert.That(activity.IsRunning, Is.False); Assert.That(CatActivity.Active, Is.Null);
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Time.timeScale = 1;
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Row("living-room", activity, "energy-and-airborne-cancel", accepted, true, true, "checked");
    }

    IEnumerator ObserveStart(string room, CatActivity activity, string sample, CatActivityStart accepted)
    {
        Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        bool started = activity.TryStart(cat); timer.Stop();
        float instantShift = Vector3.Distance(origin, cat.transform.position);
        float instantTurn = Quaternion.Angle(heading, cat.transform.rotation);
        Check(started, Id(activity) + "/" + sample + " prompt accepted but click refused");
        Check(instantShift < .001f && instantTurn < .2f, Id(activity) + "/" + sample + " instant pose correction");
        if (!started)
        {
            Row(room, activity, sample, accepted, true, false, "click-refused", instantShift, instantTurn, ms: timer.Elapsed.TotalMilliseconds);
            yield break;
        }
        if (sample == "accepted") positiveStarts++;
        else if (sample.StartsWith("yaw", StringComparison.Ordinal)) angledStarts++;
        float drift = instantShift, turn = instantTurn, skinDepth = 0, elapsed = 0;
        int frames = 0, nativeFrames = 0;
        bool fixedLaunch = accepted.Kind == CatActivityStartKind.GroundLaunch;
        while (elapsed < .30f && activity.IsRunning)
        {
            yield return new WaitForEndOfFrame();
            elapsed += Time.deltaTime; frames++;
            drift = Mathf.Max(drift, Vector3.Distance(origin, cat.transform.position));
            turn = Mathf.Max(turn, Quaternion.Angle(heading, cat.transform.rotation));
            var animation = cat.GetComponent<CatActivityAnimation>();
            if (fixedLaunch && animation != null && animation.IsNativeJump)
            {
                nativeFrames++;
                var animator = cat.GetComponentInChildren<Animator>();
                Check(animator.GetCurrentAnimatorClipInfo(0).Any(c => c.clip.name.EndsWith("|Jump", StringComparison.Ordinal)),
                    Id(activity) + " must retain the original native Jump clip");
            }
            if (frames % 3 == 1) skinDepth = Mathf.Max(skinDepth, ActualSkinDepth());
        }
        if (fixedLaunch)
        {
            Check(drift < .001f, Id(activity) + "/" + sample + " pre-takeoff root shift " + F(drift));
            Check(turn < .2f, Id(activity) + "/" + sample + " pre-takeoff yaw shift " + F(turn));
            Check(nativeFrames >= 5, Id(activity) + " never entered native preparation");
            Check(skinDepth <= .025f, Id(activity) + "/" + sample + " actual skin penetrates " + F(skinDepth));
        }
        Row(room, activity, sample, accepted, true, true, fixedLaunch ? "native-preparation" : "real-activity-motion",
            instantShift, instantTurn, drift, turn, skinDepth, nativeFrames, timer.Elapsed.TotalMilliseconds);
        activity.CancelForTransition();
        Check(!activity.IsRunning && CatActivity.Active == null, Id(activity) + " cancellation retains activity ownership");
        Check(cat.GetComponent<CharacterController>().enabled, Id(activity) + " cancellation leaves the controller disabled");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Check(!cat.IsMovementPhysicallyLocked, Id(activity) + " cancellation retains a movement lock");
    }

    bool FindReadyPose(CatActivity activity, out CatActivityStart accepted, out string reason)
    {
        activity.TryGetStartPose(cat, out accepted);
        Vector3 centre = accepted.ZoneCentre, target = accepted.ActionTarget;
        int clear = 0, localApproach = 0;
        foreach (float radius in new[] { 0f, .055f, .11f, .165f, .215f })
        for (int angle = 0; angle < (radius == 0f ? 1 : 24); angle++)
        {
            Vector3 position = centre + Quaternion.Euler(0, angle * 15f, 0) * Vector3.forward * radius;
            position.y = .05f;
            Quaternion direct = Facing(position, target, Quaternion.identity);
            // Fixture player headings remain inside the existing 35-degree
            // readiness cone. Runtime does not rotate or move the player.
            foreach (float yaw in new[] { 0f, -15f, 15f, -30f, 30f })
            {
                Quaternion heading = Quaternion.AngleAxis(yaw, Vector3.up) * direct;
                if (!ControllerAndBodyClear(position, heading)) continue;
                clear++;
                if (!HasLocalApproach(position, heading, out incomingTurn)) continue;
                localApproach++;
                Place(position, heading);
                if (!activity.TryGetStartPose(cat, out accepted) || !activity.TryGetPromptDistance(cat, out _)) continue;
                reason = string.Empty;
                return true;
            }
        }
        reason = "no-ready-local-stance clear=" + clear + " incoming=" + localApproach;
        return false;
    }

    bool HasLocalApproach(Vector3 position, Quaternion finalHeading, out float turn)
    {
        // Preserve the original direct approach as the first choice. A real
        // player can also approach from an open side and pivot at the stance;
        // requiring the last 24 cm to be on the jump's exact axis rejected
        // reachable narrow-shelf entries. Every translation and intermediate
        // degree of the alternative turn must fit the actual CC and body.
        foreach (float angle in new[] { 0f, -30f, 30f, -60f, 60f, -90f, 90f, -120f, 120f, 180f })
        {
            Quaternion incoming = Quaternion.AngleAxis(angle, Vector3.up) * finalHeading;
            bool clear = ControllerAndBodyClear(position, incoming);
            for (int step = 1; clear && step <= 6; step++)
                clear = ControllerAndBodyClear(position - incoming * Vector3.forward * (.04f * step), incoming);
            int turns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(angle)));
            for (int step = 1; clear && step <= turns; step++)
                clear = ControllerAndBodyClear(position, Quaternion.Slerp(incoming, finalHeading, (float)step / turns));
            if (!clear) continue;
            turn = angle;
            return true;
        }
        turn = 0f;
        return false;
    }

    bool ControllerAndBodyClear(Vector3 position, Quaternion rotation)
    {
        if (!cat.IsBodyPoseClear(position, rotation)) return false;
        var controller = cat.GetComponent<CharacterController>();
        Vector3 scale = cat.transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(radius * 2, controller.height * Mathf.Abs(scale.y));
        Vector3 centre = position + rotation * Vector3.Scale(controller.center, scale);
        Vector3 rise = Vector3.up * (height * .5f - radius);
        controllerProbe.radius = radius; controllerProbe.height = height;
        int count = Physics.OverlapCapsuleNonAlloc(centre - rise, centre + rise, radius, overlap, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlap.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var solid = overlap[i];
            if (solid.GetComponentInParent<CatMovement>() != null || solid.gameObject.scene != cat.gameObject.scene) continue;
            if (Physics.ComputePenetration(controllerProbe, centre, Quaternion.identity, solid,
                solid.transform.position, solid.transform.rotation, out Vector3 normal, out float depth) &&
                depth > .012f && normal.y < .8f) return false;
        }
        var boundary = HomeRoomBoundary.FindFor(cat.gameObject.scene);
        return boundary == null || (position.x >= boundary.MinimumXZ.x && position.x <= boundary.MaximumXZ.x &&
            position.z >= boundary.MinimumXZ.y && position.z <= boundary.MaximumXZ.y);
    }

    void Place(Vector3 position, Quaternion rotation)
    {
        var controller = cat.GetComponent<CharacterController>(); controller.enabled = false;
        cat.transform.SetPositionAndRotation(position, rotation); controller.enabled = true;
        Physics.SyncTransforms();
    }

    void AssertRejectedWithoutMotion(CatActivity activity, string reason)
    {
        Vector3 position = cat.transform.position; Quaternion rotation = cat.transform.rotation;
        var energy = RoomPlayModeSupport.ProvisionNeeds(); float before = energy.CurrentEnergy;
        Check(!activity.TryGetPromptDistance(cat, out _), Id(activity) + " prompt remains visible " + reason);
        Check(!activity.TryStart(cat), Id(activity) + " starts " + reason);
        Check(!activity.IsRunning && CatActivity.Active == null, Id(activity) + " owns activity after rejection");
        Check(Vector3.Distance(position, cat.transform.position) < .001f && Quaternion.Angle(rotation, cat.transform.rotation) < .2f,
            Id(activity) + " rejection changes pose " + reason);
        Check(Mathf.Abs(before - energy.CurrentEnergy) < .001f, Id(activity) + " rejected start spends energy");
        if (activity.IsRunning) activity.CancelForTransition();
    }

    public string DeepestSkinCollider { get; private set; }
    public Vector3 DeepestSkinPoint { get; private set; }
    public int DeepestSkinVertex { get; private set; }
    void ObserveSkinDepth(float depth, Collider solid, Vector3 point, int index, ref float maximum, bool exactMesh = false)
    {
        if (depth <= maximum) return;
        maximum = depth; DeepestSkinCollider = solid.name;
        string material=string.Empty;
        var renderer=solid.GetComponent<Renderer>();
        if(exactMesh&&renderer!=null)
        {
            var materials=renderer.sharedMaterials;
            if(exactLastHit.submesh>=0&&exactLastHit.submesh<materials.Length&&materials[exactLastHit.submesh]!=null)
                material=materials[exactLastHit.submesh].name;
        }
        DeepestExactSkinDetail=exactMesh ? "insideVotes="+exactLastVotes+";rayExit="+F(exactLastRay)+
            ";triangle="+exactLastHit.triangle+";submesh="+exactLastHit.submesh+";material="+material+
            ";closest="+MetricVector(exactLastHit.point)+";normal="+MetricVector(exactLastHit.normal)+
            ";triangleTests="+exactLastHit.triangleTests : "exact-box-face";
        for (var parent = solid.transform.parent; parent != null; parent = parent.parent)
            DeepestSkinCollider = parent.name + "/" + DeepestSkinCollider;
        DeepestSkinPoint = point; DeepestSkinVertex = index;
    }
    float ActualSkinDepth()
    {
        var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        skin.BakeMesh(skinSample, true); skinSample.GetVertices(vertices);
        var entry = CatBreedCatalog.Load().Find(CatBreedService.SelectedBreedId);
        float maximum = 0;
        foreach (int index in entry.ContactVertexIndices)
        {
            Vector3 world = skin.transform.TransformPoint(vertices[index]);
            foreach (var solid in roomSolids)
            {
                if (solid == null || !solid.enabled || !solid.bounds.Contains(world)) continue;
                if (solid is BoxCollider box)
                {
                    // A broad navigation box can fill the air above a real
                    // sofa seat. Visual clipping uses the original rendered
                    // mesh, while controller clearance still uses the box.
                    var visible = VisibleProxy(box);
                    if (visible != null)
                    {
                        if (InsideMesh(visible, world - box.transform.position + visible.transform.position, out float visibleDepth, box.transform, world))
                            ObserveSkinDepth(visibleDepth, box, world, index, ref maximum, true);
                        continue;
                    }
                    Vector3 local = box.transform.InverseTransformPoint(world) - box.center;
                    Vector3 inside = box.size * .5f - new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
                    if (inside.x < 0 || inside.y < 0 || inside.z < 0) continue;
                    Vector3 scale = box.transform.lossyScale;
                    ObserveSkinDepth(Mathf.Min(inside.x * Mathf.Abs(scale.x),
                        Mathf.Min(inside.y * Mathf.Abs(scale.y), inside.z * Mathf.Abs(scale.z))), box, world, index, ref maximum);
                }
                else if (solid is MeshCollider mesh && InsideMesh(mesh, world, out float depth)) ObserveSkinDepth(depth, mesh, world, index, ref maximum, true);
            }
        }
        if(CaptureSkinEvidence)RecordExactSkinEvidence(maximum);
        return maximum;
    }

    bool InsideMesh(MeshCollider mesh, Vector3 point, out float depth, Transform originalTransform = null, Vector3 originalPoint = default)
    {
        depth = float.PositiveInfinity;
        var data = topology.Get(mesh.sharedMesh);
        bool previous = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
        int inside = 0;
        try
        {
            foreach (Vector3 direction in InsideDirections)
                if (mesh.Raycast(new Ray(point, direction), out var hit, 5f))
                {
                    int triangle = hit.triangleIndex * 3;
                    Vector3 normal = Vector3.Cross(data.vertices[data.triangles[triangle + 1]] - data.vertices[data.triangles[triangle]],
                        data.vertices[data.triangles[triangle + 2]] - data.vertices[data.triangles[triangle]]);
                    if (Vector3.Dot(mesh.transform.localToWorldMatrix.inverse.transpose.MultiplyVector(normal), direction) > 0)
                    { inside++; depth = Mathf.Min(depth, hit.distance); }
                }
            exactLastVotes=inside;exactLastRay=depth;
            if(inside<4)return false;
            // Exit rays classify inside/outside only. Their travel length is
            // not the Euclidean penetration depth on an oblique surface.
            if(exactMetric==null)exactMetric=new QaExactMeshContact(topology);
            exactLastHit=exactMetric.Measure(mesh.sharedMesh,originalTransform!=null?originalTransform:mesh.transform,
                originalTransform!=null?originalPoint:point);
            depth=exactLastHit.distance;
            return true;
        }
        finally { Physics.queriesHitBackfaces = previous; }
    }


    static string MetricVector(Vector3 p) => p.x.ToString("R",CultureInfo.InvariantCulture)+";"+
        p.y.ToString("R",CultureInfo.InvariantCulture)+";"+p.z.ToString("R",CultureInfo.InvariantCulture);
    void RecordExactSkinEvidence(float depth)
    {
        if(depth<=0)return;
        var owner=CatActivity.Active;
        string key=owner!=null ? cat.gameObject.scene.name+"-"+Id(owner)+"-x"+
            owner.transform.position.x.ToString("F3",CultureInfo.InvariantCulture) : exactEvidenceKey;
        if(string.IsNullOrEmpty(key))key=cat.gameObject.scene.name+"-unowned";
        if(key!=exactEvidenceKey){exactEvidenceKey=key;exactEvidenceMaximum=0;}
        if(depth<=exactEvidenceMaximum)return;exactEvidenceMaximum=depth;
        foreach(char invalid in Path.GetInvalidFileNameChars())key=key.Replace(invalid,'_');
        string png=Root+"/exact-skin-"+key+".png";
        CaptureActualGameFrame(png);
        var animation=cat.GetComponent<CatActivityAnimation>();
        string row=string.Join(",",key,Time.frameCount,
            animation!=null?animation.CurrentPose+"/"+F(animation.NativeJumpPhase):"none",DeepestSkinVertex,
            "\""+MetricVector(DeepestSkinPoint)+"\"","\""+DeepestSkinCollider+"\"",F(depth),
            "\""+DeepestExactSkinDetail.Replace("\"","'")+"\"","\""+png+"\"");
        if(exactEvidenceIndices.TryGetValue(key,out int rowIndex))exactEvidenceRows[rowIndex]=row;
        else{exactEvidenceIndices.Add(key,exactEvidenceRows.Count);exactEvidenceRows.Add(row);}
        File.WriteAllLines(Root+"/exact-skin-metric-evidence.csv",exactEvidenceRows);
    }
    static void CaptureActualGameFrame(string path)
    {
        // Called only by the post-EndOfFrame native skin observer. This is
        // the current Game render; no camera/actor positioning or extra pose.
        var target=RenderTexture.GetTemporary(Mathf.Max(1,Screen.width),Mathf.Max(1,Screen.height),0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;Texture2D pixels=null;
        try
        {
            ScreenCapture.CaptureScreenshotIntoRenderTexture(target);
            RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);
            // Screenshot-to-RT keeps the GPU texture origin. Normalize its
            // Direct3D top-origin rows before PNG encoding; no camera or pose
            // is changed and OpenGL's bottom-origin capture stays unchanged.
            if(SystemInfo.graphicsUVStartsAtTop)
            {
                var texels=pixels.GetPixels32();int width=pixels.width,height=pixels.height;
                for(int y=0;y<height/2;y++)for(int x=0;x<width;x++)
                {
                    int a=y*width+x,b=(height-1-y)*width+x;
                    var value=texels[a];texels[a]=texels[b];texels[b]=value;
                }
                pixels.SetPixels32(texels);
            }
            pixels.Apply(false,false);
            File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;if(pixels!=null)Object.DestroyImmediate(pixels);RenderTexture.ReleaseTemporary(target);}
    }

    static Quaternion Facing(Vector3 position, Vector3 target, Quaternion fallback)
    { Vector3 direction = target - position; direction.y = 0; return direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : fallback; }
    static string Id(CatActivity activity) => string.IsNullOrEmpty(activity.StoreProductId) ? activity.ActivityId : activity.StoreProductId;
    void Check(bool condition, string message) { if (!condition) Fail(message); }
    void Fail(string message) { if (!failures.Contains(message)) failures.Add(message); }
    static string F(float value) => value.ToString("F6", CultureInfo.InvariantCulture);
    void Row(string room, CatActivity activity, string sample, CatActivityStart start, bool prompt, bool started, string reason,
        float instantShift = 0, float instantTurn = 0, float drift = 0, float turn = 0, float skin = 0, int native = 0, double ms = 0)
        => rows.Add(string.Join(",", room, Id(activity), sample, start.Kind, prompt, started, reason,
            F(start.Position.x), F(start.Position.y), F(start.Position.z), F(start.Rotation.eulerAngles.y), F(instantShift),
            F(instantTurn), F(drift), F(turn), F(skin), native, ms.ToString("F3", CultureInfo.InvariantCulture), F(incomingTurn)));
// Proposed members for PreparedInteractionStartTests. Not compiled or run yet.
// Uses this fixture's prepared stance search, actual-skin measurement and QA
// ownership setup. Reuses JumpContinuityTests.Trace/Frame and its existing
// landing, native-clip, flight-heading, handoff and release assertions.

[UnityTest, Timeout(900000)]
public IEnumerator EightRooms_ThirtyNinePreparedRoutines_CompleteWithSafeNativeMotion()
    => CompleteFullCycles(null);

[UnityTest, Timeout(240000)]
public IEnumerator CounterStool_OccupiedIslandLandingIsRejected_AndFullCycleReturnsClearControl()
{
    yield return Prepare("Kitchen_Level01");
    // Exact endpoint/heading from the failed 39-routine trace: recovery-phase
    // head skin entered the island by 291 mm and the controller rose 18.9 mm.
    Vector3 blocked = new Vector3(-1.0554392f, .05f, 2.2695572f);
    Quaternion heading = Quaternion.LookRotation(new Vector3(-.9929208f, 0, -.1187794f));
    var island = Object.FindObjectsByType<HomeProductPlacement>().Single(p => p.ProductId == HomeStoreService.KitchenIslandId);
    Assert.That(island, Is.Not.Null);
    var meshes = island.GetComponentsInChildren<MeshCollider>(true).Where(m => m.enabled && !m.isTrigger).ToArray();
    Assert.That(meshes.Length, Is.GreaterThan(0));
    Assert.That(meshes.All(m => m.sharedMesh.isReadable || CatMeshContactSurface.HasGeometry(m.sharedMesh)), Is.True,
        "The real island requires its original triangle winding in player builds.");
    Vector3 before = cat.transform.position; Quaternion beforeRotation = cat.transform.rotation;
    Assert.That(cat.IsInteractionPoseClear(blocked, heading), Is.False, "A closed occupied volume cannot be selected as a descent floor.");
    Assert.That(Vector3.Distance(before, cat.transform.position), Is.LessThan(.000001f));
    Assert.That(Quaternion.Angle(beforeRotation, cat.transform.rotation), Is.LessThan(.0001f));
    yield return CompleteFullCycles(new[] { "kitchen.counter-stool" }, true);
}

[UnityTest, Timeout(120000)]
public IEnumerator PreparedBodyPose_ClosedVolumeAndSameFrameMoveUseCurrentOccupancy()
{
    yield return Prepare("Kitchen_Level01");
    Vector3 queryPosition = new Vector3(30, 10, 30);
    Quaternion queryRotation = Quaternion.identity;
    var solid = GameObject.CreatePrimitive(PrimitiveType.Cube);
    solid.name = "QA occupied body volume";
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(solid, cat.gameObject.scene);
    solid.GetComponent<BoxCollider>().enabled = false;
    var mesh = solid.AddComponent<MeshCollider>();
    mesh.sharedMesh = solid.GetComponent<MeshFilter>().sharedMesh; mesh.convex = false;
    solid.transform.SetPositionAndRotation(queryPosition + Vector3.up * .3f, Quaternion.Euler(0, 19, 0));
    solid.transform.localScale = Vector3.one * 2f;
    Vector3 before = cat.transform.position; Quaternion beforeRotation = cat.transform.rotation;
    try
    {
        Physics.SyncTransforms();
        Assert.That(cat.IsBodyPoseClear(queryPosition, queryRotation), Is.False,
            "Entirely contained capsules remain occupied even when PhysX sees no front face.");
        solid.transform.position += Vector3.right * 4;
        Physics.SyncTransforms();
        Assert.That(cat.IsBodyPoseClear(queryPosition, queryRotation), Is.True, "The removed obstacle must not leave stale refusal.");
        solid.transform.position -= Vector3.right * 4;
        Physics.SyncTransforms();
        Assert.That(cat.IsBodyPoseClear(queryPosition, queryRotation), Is.False, "Same-frame re-entry cannot reuse a prior permission.");
        Assert.That(Vector3.Distance(before, cat.transform.position), Is.LessThan(.000001f));
        Assert.That(Quaternion.Angle(beforeRotation, cat.transform.rotation), Is.LessThan(.0001f));
    }
    finally { Object.DestroyImmediate(solid); }
}

[UnityTest, Timeout(180000)]
public IEnumerator SofaAndPlanter_DescentIsClearBeforeTakeoff_WithoutCompletionTeleport()
    => CompleteFullCycles(new[]{"SofaLounge", "patio.herb-trough"});

[UnityTest, Timeout(240000)]
public IEnumerator ThreeWalls_SourceEnvelopePreservesCurrentStart_AndCompletesWithActualSkinClearance()
    => CompleteFullCycles(new[] { "kitchen.counter-stool", "bedroom.nightstand", "patio.herb-trough" }, true);

IEnumerator CompleteFullCycles(string[] selected, bool strictWallFocus = false)
{
    int inventory = 0, completedRoutines = 0, measuredNativeFrames = 0, restStops = 0, restingRoutines = 0;
    var summary = new List<string> {
        "room,activity,started,completed,nativeFrames,restStop,maximumSkinDepth,floorClear,controlReturned,continuityError,skinDetail" };
    var verifier = typeof(JumpContinuityTests).GetMethod("Verify",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
    Assert.That(verifier, Is.Not.Null, "Existing native continuity assertions must remain available.");
    foreach (var room in HomeRoomService.Rooms)
    {
        yield return Prepare(room.SceneName);
        foreach (var activity in CatActivity.Registered.Where(a =>
            a.gameObject.scene == cat.gameObject.scene && FurnitureBodyClearanceTests.IsJumpActivity(a) &&
            (selected == null || selected.Contains(Id(a)))).OrderBy(Id).ToArray())
        {
            inventory++;
            if (activity.SupportsContinuousRest) restingRoutines++;
            if (!FindReadyPose(activity, out _, out string reason))
            {
                Fail(room.Id + "/" + Id(activity) + " full-cycle readiness: " + reason);
                summary.Add(room.Id + "," + Id(activity) + ",false,0,0,false,0,false,false," + reason);
                continue;
            }
            yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
            if (!activity.TryGetStartPose(cat, out var accepted))
            {
                Fail(Id(activity) + " full-cycle stance changed when the controller settled");
                continue;
            }
            RoomPlayModeSupport.ProvisionNeeds();
            var trace = new JumpContinuityTests.Trace {
                room = room.Id, product = Id(activity), breed = CatBreedService.SelectedBreedId };
            Action<CatActivity> completion = value => { if (value == activity) trace.completed++; };
            var bones = FullTraceBones();
            var animation = cat.GetComponent<CatActivityAnimation>();
            var animator = cat.GetComponentInChildren<Animator>();
            bool requestedStop = false, started = false;
            int nativeFrames = 0, after = 0, frame = 0;
            float maximumSkinDepth = 0, startedAt = Time.time;
            string continuityError = string.Empty, skinDetail = string.Empty;
            CatActivity.Completed += completion;
            try
            {
                started = activity.TryStart(cat);
                Check(started, Id(activity) + " full-cycle click refused");
                if (started)
                {
                    animation = cat.GetComponent<CatActivityAnimation>();
                    float deadline = Time.realtimeSinceStartup + 35;
                    while ((activity.IsRunning || after < 15) && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame(); frame++;
                        if (!activity.IsRunning) after++;
                        if (animation.IsNativeJump) nativeFrames++;
                        if (strictWallFocus && animation.IsNativeJump)
                        {
                            var tag = cat.GetComponentInChildren<CatBreedVisualTag>();
                            var measured = CatJumpClearanceCatalog.Load()?.Find(tag.BreedId);
                            Check(measured != null, Id(activity) + " source jump catalog missing");
                            if (measured != null)
                            {
                                Vector3 zero = (cat.transform.worldToLocalMatrix * tag.transform.localToWorldMatrix).MultiplyPoint3x4(measured.sourceZeroCentre);
                                Vector3 expectedEnd = animation.CurrentPose == CatActivityPose.TowelJumpUp ? new Vector3(-zero.x, 0, -zero.z) : Vector3.zero;
                                Check(cat.transform.TransformVector(animation.NativeJumpEndOffset - expectedEnd).magnitude <= .002f,
                                    Id(activity) + " source catalog/native endpoint centering differs");
                            }
                            if (accepted.Kind == CatActivityStartKind.GroundLaunch && Time.time - startedAt <= .30f)
                                Check(cat.transform.TransformVector(animation.NativeJumpStartOffset).magnitude <= .002f,
                                    Id(activity) + " prepared first launch did not reset horizontal source offset");
                        }
                        trace.frames.Add(SampleFullTrace(activity, animation, animator, bones, Time.time - startedAt));
                        if (frame % 2 == 0)
                        {
                            float measuredDepth = ActualSkinDepth();
                            if (measuredDepth > maximumSkinDepth)
                            {
                                maximumSkinDepth = measuredDepth;
                                skinDetail = DeepestSkinCollider + " / " + DeepestSkinPoint + " / vertex " + DeepestSkinVertex +
                                    " / " + animation.CurrentPose + " / phase " + animation.NativeJumpPhase + " / root " + cat.transform.position;
                            }
                        }
                        // First preparation still starts at the accepted player
                        // pose; later jumps/turns belong to the actual activity.
                        if (accepted.Kind == CatActivityStartKind.GroundLaunch && Time.time - startedAt <= .30f)
                        {
                            Check(Vector3.Distance(accepted.Position, cat.transform.position) < .001f,
                                Id(activity) + " moved before the first native takeoff");
                            Check(Quaternion.Angle(accepted.Rotation, cat.transform.rotation) < .2f,
                                Id(activity) + " rotated before the first native takeoff");
                        }
                        if (!requestedStop && activity.IsWaitingForRestStop && activity.RestingSeconds >= 1.2f)
                        {
                            requestedStop = true;
                            Check(activity.RequestRestStop(), Id(activity) + " Get Up rejected during rest");
                        }
                    }
                    Check(!activity.IsRunning, Id(activity) + " full-cycle timeout");
                    Check(trace.completed == 1, Id(activity) + " expected exactly one real completion, saw " + trace.completed);
                    Check(nativeFrames > 10, Id(activity) + " did not produce a full native jump");
                    // The focused descent regression measures native continuity
                    // and the controller endpoint. Full visual clearance remains
                    // an independent assertion in the complete room matrix.
                    if (selected == null || strictWallFocus) Check(maximumSkinDepth <= .025f, Id(activity) + " full-cycle actual skin penetration " + F(maximumSkinDepth));
                    if (activity.IsRunning) activity.CancelForTransition();
                    yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                    trace.controllerFloorClear = ControllerAndBodyClear(cat.transform.position, cat.transform.rotation);
                    Check(trace.controllerFloorClear, Id(activity) + " did not finish on a body/controller-clear floor");
                    Check(cat.GetComponent<CharacterController>().enabled && !cat.IsMovementPhysicallyLocked,
                        Id(activity) + " full-cycle control was not returned");
                    try { verifier.Invoke(null, new object[] { trace }); }
                    catch (System.Reflection.TargetInvocationException error)
                    {
                        continuityError = error.InnerException != null ? error.InnerException.Message : error.Message;
                        Fail(Id(activity) + " full-cycle continuity: " + continuityError);
                    }
                    if (trace.completed == 1) completedRoutines++;
                    measuredNativeFrames += nativeFrames;
                    if (requestedStop) restStops++;
                }
            }
            finally
            {
                CatActivity.Completed -= completion;
                if (activity.IsRunning) activity.CancelForTransition();
                File.WriteAllText(Root + (strictWallFocus ? "/prepared-wall-" : "/prepared-full-") + Id(activity) + ".json", JsonUtility.ToJson(trace, true));
                summary.Add(string.Join(",", room.Id, Id(activity), started, trace.completed, nativeFrames, requestedStop,
                    F(maximumSkinDepth), trace.controllerFloorClear,
                    cat.GetComponent<CharacterController>().enabled && !cat.IsMovementPhysicallyLocked,
                    continuityError.Replace(",", ";").Replace("\n", " ").Replace("\r", " "), "\"" + skinDetail.Replace("\"", "'") + "\""));
                File.WriteAllLines(Root + "/prepared-full-" + (strictWallFocus ? "strict-walls" : selected == null ? "cycles" : "descent-regression") + ".csv", summary);
            }
        }
    }
    int expected = selected == null ? 39 : selected.Length;
    Check(inventory == expected, "Full-cycle inventory changed: " + inventory);
    Check(completedRoutines == inventory, "Full-cycle completions " + completedRoutines + "/" + inventory);
    Check(restStops == restingRoutines, "Real Get Up requests " + restStops + "/" + restingRoutines);
    Check(measuredNativeFrames > expected * 10, "No broad native-frame evidence was collected");
    Assert.That(failures, Is.Empty, string.Join("\n", failures));
}

Transform[] FullTraceBones()
{
    return new[] {
        "DEF-spine", "DEF-spine.003", "DEF-hand.L", "DEF-hand.R", "DEF-foot.L", "DEF-foot.R",
        "DEF-thigh.L", "DEF-thigh.R", "DEF-shin.L", "DEF-shin.R"
    }.Select(name => CatBreedVisualFactory.FindDescendant(cat.transform, name)).ToArray();
}

JumpContinuityTests.Frame SampleFullTrace(CatActivity activity, CatActivityAnimation animation,
    Animator animator, Transform[] bones, float seconds)
{
    var clips = animator.GetCurrentAnimatorClipInfo(0);
    var turning = cat.GetComponent<CatSurfaceTurnMotion>();
    return new JumpContinuityTests.Frame {
        time = seconds, root = cat.transform.position, visual = animator.transform.position,
        visualLocal = animator.transform.localPosition, nativeJump = animation.IsNativeJump,
        nativePhase = animation.NativeJumpPhase, hips = bones[0].position, shoulders = bones[1].position,
        leftHand = bones[2].position, rightHand = bones[3].position, leftFoot = bones[4].position, rightFoot = bones[5].position,
        leftHip = bones[6].position, rightHip = bones[7].position, leftKnee = bones[8].position, rightKnee = bones[9].position,
        pose = animation.CurrentPose.ToString(), clip = clips.Length == 0 ? "none" : clips[0].clip.name,
        phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime, running = activity.IsRunning,
        rest = activity.IsWaitingForRestStop, forward = cat.transform.forward,
        turning = turning != null && turning.IsTurning, turnSteps = turning != null ? turning.CompletedSteps : 0,
        turnContactError = turning != null && turning.IsTurning ? turning.MaximumContactError : 0f };
}

}
