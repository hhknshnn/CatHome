#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class CareReachGeometryDiagnosticTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    CareAlignmentPolishTests fixture;
    bool prepared;
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/INTERACTION_POLISH_2026-09-16");
    static T Read<T>(object value, string field) => (T)value.GetType().GetField(field, Flags).GetValue(value);
    static object Call(object value, string method, params object[] arguments) => value.GetType().GetMethod(method, Flags).Invoke(value, arguments);
    [SetUp] public void Before() { fixture = new CareAlignmentPolishTests(); fixture.Before(); prepared = true; }
    [TearDown] public void After() { if (prepared) fixture.After(); prepared = false; }

    [UnityTest, Timeout(180000)]
    public IEnumerator PureSourceSolve_MatchesNative_ThenFindsPhysicallyLegalCareApproaches()
    {
        var rows = new List<string> {
            "breed,radius,angle,root,yaw,floorMeasured,floorY,bodyClear,controllerClear,interactionClear,zoneDistance,zoneAndFacing,productionGeometryGate,solved,reachable,shoulderPitch,goalDistance,lowestFoodDistance,nearestFoodDistance,postShoulderReach,chainLength,leftMargin,rightMargin"
        };
        var evidence = new List<string> { "breed,event,root,yaw,actualMouth,solverFoodDistance,nearestPredicted,rootDrift,yawDrift,completions" };
        var work = new CatCareReachGeometry.Workspace();
        try
        {
            yield return (IEnumerator)Call(fixture, "Home");
            yield return (IEnumerator)Call(fixture, "Room", HomeRoomService.KitchenId);
            foreach (string breed in new[] { "oriental-shorthair", "persian" })
            {
                yield return (IEnumerator)Call(fixture, "Breed", breed); Call(fixture, "ReadyNeeds");
                var cat = Read<CatMovement>(fixture, "cat");
                var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene);
                Transform target = meal.BowlPoint, stand = Read<Transform>(meal, "standPoint");
                Call(fixture, "FindCareStance", target, stand, (Func<bool>)(() => meal.TryGetPromptDistance(cat, out _)));
                Vector3 sourceRoot = cat.transform.position; Quaternion sourceYaw = cat.transform.rotation;
                CatCareReachGeometry.Pose source = null;
                int completions = 0; Action<CatActivity> completed = a => { if (a == meal) completions++; };
                CatActivity.Completed += completed;
                try
                {
                    Click(cat, meal);
                    int frame = 0; float deadline = Time.realtimeSinceStartup + 20f;
                    while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame(); frame++;
                        if (frame != 224) continue;
                        var head = cat.GetComponent<CatMealHeadMotion>();
                        Assert.That(head != null && head.IsActive && Read<float>(head, "weight") >= .999f, Is.True);
                        float nativeDistance = head.Distance, nativeActual = Actual(target);
                        source = Capture(head, cat);
                        Assert.That(CatCareReachGeometry.TrySolve(source, target.position, 1f, work, out var prediction), Is.True);
                        evidence.Add(Csv(breed, "native-equivalence", sourceRoot, sourceYaw.eulerAngles.y, nativeActual,
                            prediction.foodDistance, prediction.nearestFoodDistance, 0, 0, completions));
                        // A shared preflight must reproduce the actual limited solve,
                        // including support rejection and the production early exit.
                        Assert.That(prediction.foodDistance, Is.EqualTo(nativeDistance).Within(.0005f));
                        Assert.That(prediction.nearestFoodDistance, Is.EqualTo(nativeActual).Within(.0005f));
                        Assert.That(Actual(target), Is.EqualTo(nativeActual).Within(.000002f), "Capture restored the live pose.");
                    }
                    Assert.That(source, Is.Not.Null); Assert.That(meal.IsRunning, Is.False);
                    evidence.Add(Csv(breed, "original-complete", cat.transform.position, cat.transform.eulerAngles.y,
                        Actual(target), "", "", Vector3.Distance(sourceRoot, cat.transform.position),
                        Quaternion.Angle(sourceYaw, cat.transform.rotation), completions));
                }
                finally { CatActivity.Completed -= completed; CatActionState.CancelForTransition(cat); }
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Call(fixture, "ReadyNeeds");
                var guard = Read<CatBodyGuard>(cat, "bodyGuard");
                var feeding = CatFeedingAlignmentCatalog.Load().Find(breed);
                Vector3 outward = stand.position - target.position; outward.y = 0; outward.Normalize();
                bool found = false; float bestRadius = float.PositiveInfinity;
                Vector3 selected = default; Quaternion selectedYaw = Quaternion.identity;
                CatCareReachGeometry.Solution selectedPrediction = default;
                foreach (float radius in new[] { .30f, .32f, .34f, .36f, .38f, .40f, .42f, .44f })
                foreach (int angle in new[] { 0, 15, -15, 30, -30, 45, -45, 60, -60, 75, -75, 90, -90, 120, -120, 180 })
                {
                    Vector3 side = Quaternion.Euler(0, angle, 0) * outward;
                    Vector3 position = target.position + side * radius; position.y = sourceRoot.y;
                    Quaternion rotation = Quaternion.LookRotation(-side);
                    Physics.SyncTransforms();
                    bool body = cat.IsBodyPoseClear(position, rotation), cc = guard.IsControllerClear(position, rotation);
                    bool interaction = cat.IsInteractionPoseClear(position, rotation);
                    float scale = cat.transform.lossyScale.y / .5f;
                    Vector3 mouthOffset = feeding.mouthOffset * scale;
                    Vector3 centre = target.position - rotation * mouthOffset; centre.y = 0;
                    Vector3 delta = position - centre; delta.y = 0;
                    Vector3 facing = target.position - position; facing.y = 0;
                    float zoneDistance = delta.magnitude;
                    bool zone = zoneDistance <= .16f * scale &&
                        Mathf.Abs((position + rotation * mouthOffset).y - target.position.y) <= .26f * scale &&
                        Vector3.Angle(rotation * Vector3.forward, facing) <= 25f;
                    // Re-express the real target in this immutable source stance.
                    // No candidate ever moves the live actor or changes its bones.
                    Vector3 mappedFood = sourceRoot + sourceYaw * Quaternion.Inverse(rotation) * (target.position - position);
                    bool solved = CatCareReachGeometry.TrySolve(source, mappedFood, 1f, work, out var solution);
                    bool reachable = solved && solution.Contact;
                    bool floor = FloorAt(cat, position, out float floorY);
                    bool gate = zone && interaction;
                    rows.Add(Csv(breed, radius, angle, position, rotation.eulerAngles.y, floor, floorY, body, cc, interaction,
                        zoneDistance, zone, gate, solved, reachable, solution.shoulderPitch, solution.goalDistance,
                        solution.foodDistance, solution.nearestFoodDistance, solution.postShoulderRequiredReach,
                        solution.chainLength, solution.leftMaximumMargin, solution.rightMaximumMargin));
                    if (floor && gate && reachable && radius < bestRadius)
                    { found = true; bestRadius = radius; selected = position; selectedYaw = rotation; selectedPrediction = solution; }
                    yield return null; // bounded native query/solve, one candidate per frame
                }
                if (!found)
                {
                    evidence.Add(Csv(breed, "no-legal-reachable-candidate", sourceRoot, sourceYaw.eulerAngles.y, "", "", "", 0, 0, 0));
                    continue; // diagnosis does not pretend that hiding a bad prompt solves care
                }
                // Show that current physics is rechecked independently of the
                // cached geometry plan when a real collider occupies this stance.
                var blocker = new GameObject("QA care dynamic candidate blocker");
                try
                {
                    blocker.transform.position = selected + Vector3.up * .25f;
                    var collider = blocker.AddComponent<BoxCollider>(); collider.size = new Vector3(.24f, .40f, .24f);
                    Physics.SyncTransforms();
                    Assert.That(cat.IsInteractionPoseClear(selected, selectedYaw), Is.False);
                }
                finally { Object.DestroyImmediate(blocker); Physics.SyncTransforms(); }
                Assert.That(cat.IsInteractionPoseClear(selected, selectedYaw), Is.True);
                Call(fixture, "Place", selected, selectedYaw); Call(fixture, "ReadyNeeds");
                Assert.That(meal.TryGetPromptDistance(cat, out _), Is.True, "The actual gate must agree after fixture placement.");
                completions = 0; float actualMinimum = float.PositiveInfinity, maxRoot = 0, maxYaw = 0;
                CatActivity.Completed += completed;
                try
                {
                    Click(cat, meal);
                    float deadline = Time.realtimeSinceStartup + 20f;
                    while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        if (meal.IsEating) actualMinimum = Mathf.Min(actualMinimum, Actual(target));
                        maxRoot = Mathf.Max(maxRoot, Vector3.Distance(selected, cat.transform.position));
                        maxYaw = Mathf.Max(maxYaw, Quaternion.Angle(selectedYaw, cat.transform.rotation));
                    }
                    evidence.Add(Csv(breed, "selected-actual-button", selected, selectedYaw.eulerAngles.y, actualMinimum,
                        selectedPrediction.foodDistance, selectedPrediction.nearestFoodDistance, maxRoot, maxYaw, completions));
                    Assert.That(meal.IsRunning, Is.False); Assert.That(completions, Is.EqualTo(1));
                    Assert.That(actualMinimum, Is.LessThanOrEqualTo(CatCareReachGeometry.ContactDistance));
                    Assert.That(maxRoot, Is.LessThan(.002f)); Assert.That(maxYaw, Is.LessThan(.15f));
                }
                finally { CatActivity.Completed -= completed; CatActionState.CancelForTransition(cat); }
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
            Assert.That(rows.Count - 1, Is.EqualTo(256));
        }
        finally
        {
            Directory.CreateDirectory(Output);
            File.WriteAllLines(Path.Combine(Output, "care-pure-source-approaches.csv"), rows);
            File.WriteAllLines(Path.Combine(Output, "care-pure-source-proof.csv"), evidence);
        }
    }

    static void Click(CatMovement cat, MealTimeActivity meal)
    {
        Call(cat.GetComponent<BowlInteraction>(), "Update");
        var prompt = Object.FindAnyObjectByType<ActivityPromptController>(); Call(prompt, "RefreshImmediate");
        Assert.That(Read<CatActivity>(prompt, "candidate"), Is.SameAs(meal));
        var button = Read<Button>(prompt, "actionButton");
        Assert.That(button != null && button.isActiveAndEnabled && button.interactable, Is.True);
        button.onClick.Invoke(); Assert.That(meal.IsRunning && meal.IsEating, Is.True);
    }
    float Actual(Transform target) => (float)Call(fixture, "ActualMouthDistance", target);
    static bool FloorAt(CatMovement cat, Vector3 position, out float y)
    {
        y = float.NaN;
        foreach (var hit in Physics.RaycastAll(position + Vector3.up * .25f, Vector3.down, .5f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
        {
            if (hit.collider.transform.IsChildOf(cat.transform)) continue;
            y = hit.point.y;
            return hit.normal.y >= .8f && Mathf.Abs(position.y - (y + .05f)) <= .015f;
        }
        return false;
    }

    static CatCareReachGeometry.Pose Capture(CatMealHeadMotion head, CatMovement cat)
    {
        var all = cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>(true);
        var positions = all.Select(t => t.localPosition).ToArray(); var rotations = all.Select(t => t.localRotation).ToArray();
        var scales = all.Select(t => t.localScale).ToArray();
        try
        {
            Call(head, "RestoreFrame");
            var torso = Read<Transform>(head, "torso"); var joints = Read<Transform[]>(head, "joints");
            var source = new CatCareReachGeometry.Pose { root = cat.transform.position, right = cat.transform.right,
                torsoPosition = torso.position, torsoRotation = torso.rotation, joints = new CatCareReachGeometry.Joint[3] };
            for (int i = 0; i < 3; i++)
            {
                int anchorIndex = Anchor(joints[i].parent, torso, joints, i - 1);
                Assert.That(anchorIndex, Is.GreaterThanOrEqualTo(-1));
                Transform anchor = anchorIndex < 0 ? torso : joints[anchorIndex];
                source.joints[i] = new CatCareReachGeometry.Joint { anchor = anchorIndex,
                    positionFromAnchor = Quaternion.Inverse(anchor.rotation) * (joints[i].position - anchor.position),
                    parentRotationFromAnchor = Quaternion.Inverse(anchor.rotation) * joints[i].parent.rotation,
                    sourceLocalRotation = joints[i].localRotation };
            }
            var mouth = new List<CatCareReachGeometry.Vertex>();
            foreach (object vertex in Read<IEnumerable>(head, "mouth"))
            {
                var bones = Read<Transform[]>(vertex, "bones"); var points = Read<Vector3[]>(vertex, "points");
                var weights = Read<float[]>(vertex, "weights"); var influences = new List<CatCareReachGeometry.Influence>(4);
                for (int i = 0; i < 4; i++)
                {
                    if (weights[i] <= 0) continue;
                    int anchorIndex = Anchor(bones[i], torso, joints, 2);
                    Vector3 point = bones[i].TransformPoint(points[i]);
                    if (anchorIndex != -2)
                    {
                        Transform anchor = anchorIndex == -1 ? torso : joints[anchorIndex];
                        point = Quaternion.Inverse(anchor.rotation) * (point - anchor.position);
                    }
                    influences.Add(new CatCareReachGeometry.Influence { anchor = anchorIndex, point = point, weight = weights[i] });
                }
                mouth.Add(new CatCareReachGeometry.Vertex { influences = influences.ToArray() });
            }
            source.mouth = mouth.ToArray(); var legs = Read<Array>(head, "forelegs");
            source.left = Leg(legs.GetValue(0)); source.rightLeg = Leg(legs.GetValue(1));
            return source;
        }
        finally
        { for (int i = 0; i < all.Length; i++) { all[i].localPosition = positions[i]; all[i].localRotation = rotations[i]; all[i].localScale = scales[i]; } }
    }
    static int Anchor(Transform bone, Transform torso, Transform[] joints, int last)
    {
        for (int i = last; i >= 0; i--) if (bone == joints[i] || bone.IsChildOf(joints[i])) return i;
        return bone == torso || bone.IsChildOf(torso) ? -1 : -2;
    }
    static CatCareReachGeometry.Leg Leg(object limb)
    {
        var arm = Read<Transform>(limb, "arm"); var fore = Read<Transform>(limb, "fore"); var hand = Read<Transform>(limb, "hand");
        return new CatCareReachGeometry.Leg { upper = arm.position, fore = fore.position,
            pawTarget = Read<Vector3>(limb, "pawPosition"), sourceElbow = Read<Vector3>(limb, "elbowPosition"),
            upperLength = Vector3.Distance(arm.position, fore.position), lowerLength = Vector3.Distance(fore.position, hand.position) };
    }
    static string Csv(params object[] values) => string.Join(",", values.Select(value =>
    {
        string text = value is Vector3 v ? FormattableString.Invariant($"{v.x:F7};{v.y:F7};{v.z:F7}") :
            value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }));
}
#endif
