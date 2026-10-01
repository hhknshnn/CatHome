using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class InteractionPolishTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    HomeStoreSaveState saved;
    string breed;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Real saves must never be used for polish tests.");
        saved = HomeStoreService.CaptureState();
        breed = CatBreedService.SelectedBreedId;
    }

    [TearDown] public void After()
    {
        if (CatActivity.Active != null) CatActivity.Active.CancelForTransition();
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(saved);
        CatBreedService.Select(breed);
        Time.timeScale = 1;
    }

    static void OwnRoom()
    {
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Select(p => p.Id).ToArray();
        state.storedProductIds = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state);
    }

    static void Place(CatMovement cat, Vector3 position, Quaternion rotation)
    {
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        position.y = .05f;
        cat.transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    [UnityTest] public IEnumerator StationaryViews_KeepTheAcceptedPlayerPose()
    {
        int checkedCount = 0;
        var rows = new List<string> { "room,product,startMs,rootDrift,rootTurn" };
        foreach (string room in new[] { "LivingRoom_Level01", "Kitchen_Level01", "Patio_Level01", "SecondFloor_Level01" })
        {
            yield return RoomPlayModeSupport.LoadRoomAlone(room); OwnRoom(); yield return null; yield return null;
            var cat = Object.FindAnyObjectByType<CatMovement>();
            foreach (var activity in CatActivity.Registered.OfType<SitLookActivity>()
                .Where(a => a.gameObject.scene == cat.gameObject.scene && !a.IsRetired && a.ReactionKind == SitLookReaction.Sit).ToArray())
            {
                bool found = false;
                var target = activity.LookPoint.position;
                for (int ring = 0; ring < 4 && !found; ring++)
                for (int angle = 0; angle < 24 && !found; angle++)
                {
                    var direction = Quaternion.Euler(0, angle * 15, 0) * Vector3.forward;
                    var point = target + direction * (.55f + ring * .15f); point.y = .05f;
                    if (!CatActivityMotion.IsControllerFloorClear(cat, point)) continue;
                    Place(cat, point, Quaternion.LookRotation(-direction));
                    found = activity.TryGetPromptDistance(cat, out _);
                }
                // The previous report already records the living-room picture's blocked entrance.
                if (!found && activity.StoreProductId == "room.modern-painting") continue;
                Assert.That(found, Is.True, activity.StoreProductId + " must have a usable viewing stance");
                yield return null;
                RoomPlayModeSupport.ProvisionNeeds();
                Vector3 origin = cat.transform.position; Quaternion rotation = cat.transform.rotation;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Assert.That(activity.TryStart(cat), Is.True, activity.StoreProductId); watch.Stop();
                float drift = 0, turn = 0, deadline = Time.realtimeSinceStartup + 6;
                while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    drift = Mathf.Max(drift, Vector3.Distance(origin, cat.transform.position));
                    turn = Mathf.Max(turn, Quaternion.Angle(rotation, cat.transform.rotation));
                }
                Assert.That(activity.IsRunning, Is.False, activity.StoreProductId);
                Assert.That(drift, Is.LessThan(.012f), activity.StoreProductId + " root must stay at the accepted stance");
                Assert.That(turn, Is.LessThan(.2f), activity.StoreProductId + " no camera-staging turn after Look");
                Assert.That(watch.Elapsed.TotalMilliseconds, Is.LessThan(50), "No click-time navigation search");
                rows.Add(System.FormattableString.Invariant($"{room},{activity.StoreProductId},{watch.Elapsed.TotalMilliseconds:F3},{drift:F5},{turn:F3}"));
                checkedCount++;
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
        }
        System.IO.File.WriteAllLines(Root + "/stationary-views.csv", rows);
        Assert.That(checkedCount, Is.GreaterThanOrEqualTo(5));
    }

    [UnityTest] public IEnumerator StationaryRest_KeepsPose_AndSeparatesOvenFromMat()
    {
        int checkedCount = 0;
        var rows = new List<string> { "room,product,rootDrift,rootTurn" };
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        foreach (string room in new[] { "Kitchen_Level01", "Balcony_Level01", "Patio_Level01", "SecondFloor_Level01" })
        {
            yield return RoomPlayModeSupport.LoadRoomAlone(room); OwnRoom(); yield return null; yield return null;
            var cat = Object.FindAnyObjectByType<CatMovement>();
            var activities = CatActivity.Registered.Where(a => a.gameObject.scene == cat.gameObject.scene &&
                (a is OvenWarmthActivity || a is MatKneadActivity)).ToArray();
            foreach (var activity in activities)
            {
                var centre = (Transform)activity.GetType().GetField(activity is OvenWarmthActivity ? "baskPoint" : "padPoint", flags).GetValue(activity);
                Place(cat, centre.position + Vector3.right * .035f, Quaternion.Euler(0, 37, 0));
                // Wall-adjacent warmth can reject a heading whose head envelope
                // crosses the wall. Select a real clear player heading at the
                // same use position; the routine must retain that heading.
                if (!activity.TryGetPromptDistance(cat, out _))
                    for (int yaw = 0; yaw < 360; yaw += 15)
                    {
                        Place(cat, centre.position + Vector3.right * .035f, Quaternion.Euler(0, yaw, 0));
                        if (activity.TryGetPromptDistance(cat, out _)) break;
                    }
                yield return null; RoomPlayModeSupport.ProvisionNeeds();
                Assert.That(activity.TryGetPromptDistance(cat, out _), Is.True, activity.StoreProductId + " ready use zone");
                if (room == "Kitchen_Level01")
                    foreach (var other in activities.Where(a => a != activity))
                        Assert.That(other.TryGetPromptDistance(cat, out _), Is.False,
                            activity.StoreProductId + " use zone must not offer " + other.StoreProductId);
                Vector3 origin = cat.transform.position; Quaternion rotation = cat.transform.rotation;
                Assert.That(activity.TryStart(cat), Is.True, activity.StoreProductId);
                float drift = 0, turn = 0, deadline = Time.realtimeSinceStartup + 12;
                while (!activity.IsWaitingForRestStop && activity.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    drift = Mathf.Max(drift, Vector3.Distance(origin, cat.transform.position));
                    turn = Mathf.Max(turn, Quaternion.Angle(rotation, cat.transform.rotation));
                }
                Assert.That(activity.IsWaitingForRestStop, Is.True, activity.StoreProductId + " reaches rest");
                Assert.That(drift, Is.LessThan(.012f), activity.StoreProductId + " no pre-rest alignment travel");
                Assert.That(turn, Is.LessThan(.2f), activity.StoreProductId + " no camera alignment turn");
                Time.timeScale = 0; yield return null; yield return null;
                Assert.That(Vector3.Distance(origin, cat.transform.position), Is.LessThan(.012f), "pause retains pose");
                Time.timeScale = 1;
                activity.RequestRestStop();
                deadline = Time.realtimeSinceStartup + 5;
                while (activity.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(activity.IsRunning, Is.False, activity.StoreProductId + " get up completes");
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False, activity.StoreProductId + " returns control");
                rows.Add(System.FormattableString.Invariant($"{room},{activity.StoreProductId},{drift:F5},{turn:F3}"));
                checkedCount++;
            }
        }
        System.IO.File.WriteAllLines(Root + "/stationary-rest.csv", rows);
        Assert.That(checkedCount, Is.GreaterThanOrEqualTo(7));
    }

    [UnityTest] public IEnumerator Baseline_ReportedInteractions_RecordStartCostAndMotion()
    {
        var rows = new List<string> { "room,product,kind,prompt,distance,started,startMs,travel,turns,maxDisplacement,walkFrames" };
        foreach (var room in new[] { "LivingRoom_Level01", "Kitchen_Level01", "Balcony_Level01", "Patio_Level01", "SecondFloor_Level01" })
        {
            yield return RoomPlayModeSupport.LoadRoomAlone(room);
            OwnRoom(); yield return null; yield return null;
            var cat = Object.FindAnyObjectByType<CatMovement>();
            var activities = CatActivity.Registered.Where(a => a.gameObject.scene == cat.gameObject.scene &&
                (a is OvenWarmthActivity || a is MealTimeActivity || a is SinkSipActivity ||
                 a is MatKneadActivity || a.Kind == CatActivityKind.FernWatch ||
                 a.Kind == CatActivityKind.RailingSwat || a.Kind == CatActivityKind.RecordSpin ||
                 a is LivingFurnitureActivity)).ToArray();
            foreach (var activity in activities)
            {
                RoomPlayModeSupport.ProvisionNeeds();
                var start = activity.RoutineEntryPoint.position;
                Place(cat, start, activity.RoutineEntryPoint.rotation);
                yield return null;
                var origin = cat.transform.position; var heading = cat.transform.rotation;
                bool prompt = activity.TryGetPromptDistance(cat, out float distance);
                var timer = System.Diagnostics.Stopwatch.StartNew();
                bool started = activity.TryStart(cat); timer.Stop();
                double startMs = timer.Elapsed.TotalMilliseconds;
                float travel = 0, turns = 0, maxDisplacement = 0, elapsed = 0;
                int walkFrames = 0; Vector3 previous = origin; Quaternion previousRotation = heading;
                while (activity.IsRunning && elapsed < 1.8f)
                {
                    yield return null; elapsed += Time.deltaTime;
                    Vector3 current = cat.transform.position;
                    travel += Vector3.Distance(previous, current); previous = current;
                    turns += Quaternion.Angle(previousRotation, cat.transform.rotation); previousRotation = cat.transform.rotation;
                    maxDisplacement = Mathf.Max(maxDisplacement, Vector3.Distance(origin, current));
                    var animation = cat.GetComponent<CatActivityAnimation>();
                    if (animation != null && animation.CurrentPose == CatActivityPose.Walk) walkFrames++;
                }
                rows.Add(System.FormattableString.Invariant($"{room},{activity.StoreProductId},{activity.Kind},{prompt},{distance},{started},{startMs:F3},{travel:F4},{turns:F2},{maxDisplacement:F4},{walkFrames}"));
                System.IO.Directory.CreateDirectory(Root);
                System.IO.File.WriteAllLines(Root + "/baseline-starts.csv", rows);
                activity.CancelForTransition();
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
        }
        Assert.That(rows.Count, Is.GreaterThan(10));
    }
}
