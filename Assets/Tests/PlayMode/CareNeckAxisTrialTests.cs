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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Pure remote-shape trials. Never changes a live controller/probe or skips a production gate.
public sealed class CareNeckAxisTrialTests
{
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    [Serializable] sealed class Data { public Entry[] entries; }
    [Serializable] sealed class Entry
    {
        public string breed;
        public Vector3 neckMin, neckMax, neckCentre, neckSize, torsoMin, torsoMax;
        public float paddingSource, minimumNeckClearanceAtHome, torsoMaxLateralAtHome, coreRadiusAtHome;
        public int poses, neckVerticesPerPose, sampledNeckVertices, sampledTorsoVertices;
        public Vector3 trialStart, trialEnd, trialAxis, principalAxis;
        public float trialRadius, trialVolume, minimumCapsuleClearanceAtHome;
        public int trialAxes;
    }
    struct Contact { public bool clear; public float maximumDepth; public string collider; public Vector3 normal; }
    sealed class Candidate { public Vector3 position; public Quaternion rotation; public float radius; public int angle; public string variants; }
    CareAlignmentPolishTests fixture; bool prepared;
    CapsuleCollider capsule;
    readonly List<string> contacts = new List<string>();
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/INTERACTION_POLISH_2026-09-16");
    static T Read<T>(object instance, string name) => (T)instance.GetType().GetField(name, Instance).GetValue(instance);
    static object Call(object instance, string name, params object[] args) => instance.GetType().GetMethod(name, Instance).Invoke(instance, args);
    [SetUp] public void Before() { fixture = new CareAlignmentPolishTests(); fixture.Before(); prepared = true; }
    [TearDown] public void After()
    {
        if (capsule != null) Object.DestroyImmediate(capsule.gameObject);
        if (prepared) fixture.After(); prepared = false;
    }

    [UnityTest, Timeout(300000)]
    public IEnumerator SourceContainedObliqueNeckCapsule_KeepsOriginalController_AndMeasuresActualSkin()
    {
        var rows = new List<string> { "breed,radius,angle,root,yaw,trialStart,trialEnd,trialRadius,oldCCRadius,controllerHeight,controllerCentre,oldBody,otherThreeCaps,trialNeck,oldCC,zone,floor,sourceReach,sourceMouth,baseline,neckOnly" };
        var skinRows = new List<string> { "breed,variants,radius,angle,clip,phase,root,yaw,collider,colliderType,meshAsset,insideVertices,maximumDepth,deepestPoint" };
        contacts.Clear(); contacts.Add("breed,radius,angle,shape,region,collider,colliderType,depth,normal,blocks");
        var work = new CatCareReachGeometry.Workspace();
        try
        {
            string path = Path.Combine(Output, "care-neck-axis-trial-measure.json");
            Assert.That(File.Exists(path), Is.True, "Run the QA-only96-pose builder first.");
            var data = JsonUtility.FromJson<Data>(File.ReadAllText(path));
            Assert.That(data?.entries?.Length, Is.EqualTo(10));
            foreach (var entry in data.entries)
            {
                Assert.That(entry.poses, Is.EqualTo(96));
                Assert.That(entry.sampledNeckVertices, Is.EqualTo(entry.neckVerticesPerPose * 96));
                Assert.That(entry.minimumCapsuleClearanceAtHome, Is.GreaterThanOrEqualTo(.00499f));
            }
            CareAlignmentPolishTests.TransitionMark("Neck before Home");
            yield return (IEnumerator)Call(fixture, "Home");
            CareAlignmentPolishTests.TransitionMark("Neck after Home");
            yield return (IEnumerator)Call(fixture, "Room", HomeRoomService.KitchenId);
            foreach (string breed in new[] { "oriental-shorthair", "persian" })
            {
                yield return (IEnumerator)Call(fixture, "Breed", breed); Call(fixture, "ReadyNeeds");
                var cat = Read<CatMovement>(fixture, "cat");
                var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene);
                Transform target = meal.BowlPoint, stand = Read<Transform>(meal, "standPoint");
                CareAlignmentPolishTests.TransitionMark("Neck before stance " + breed);
                Call(fixture, "FindCareStance", target, stand, (Func<bool>)(() => meal.TryGetPromptDistance(cat, out _)));
                CareAlignmentPolishTests.TransitionMark("Neck after stance " + breed);
                Vector3 sourceRoot = cat.transform.position; Quaternion sourceYaw = cat.transform.rotation;
                CatCareReachGeometry.Pose source = null;
                try
                {
                    var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
                    CareAlignmentPolishTests.TransitionMark("Neck before HUD refresh");
                    Call(cat.GetComponent<BowlInteraction>(), "Update"); Call(prompt, "RefreshImmediate");
                    CareAlignmentPolishTests.TransitionMark("Neck after HUD refresh");
                    Assert.That(Read<CatActivity>(prompt, "candidate"), Is.SameAs(meal));
                    CareAlignmentPolishTests.TransitionMark("Neck before click");
                    Read<Button>(prompt, "actionButton").onClick.Invoke();
                    CareAlignmentPolishTests.TransitionMark("Neck after click"); Assert.That(meal.IsRunning && meal.IsEating, Is.True);
                    int frame = 0; float deadline = Time.realtimeSinceStartup + 20f;
                    while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame(); frame++;
                        if (frame != 224) continue;
                        var head = cat.GetComponent<CatMealHeadMotion>();
                        Assert.That(head != null && head.IsActive && Read<float>(head, "weight") >= .999f, Is.True);
                        source = (CatCareReachGeometry.Pose)typeof(CareReachGeometryDiagnosticTests).GetMethod("Capture", Static).Invoke(null, new object[] { head, cat });
                        Assert.That(CatCareReachGeometry.TrySolve(source, target.position, 1f, work, out var original), Is.True);
                        Assert.That(original.foodDistance, Is.EqualTo(head.Distance).Within(.0005f));
                    }
                    Assert.That(source, Is.Not.Null); Assert.That(meal.IsRunning, Is.False);
                }
                finally { CatActionState.CancelForTransition(cat); }
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                var shape = data.entries.Single(e => e.breed == breed);
                var guard = Read<CatBodyGuard>(cat, "bodyGuard");
                var probes = Read<CatBodyGuardCatalog.Probe[]>(guard, "probes");
                Assert.That(probes.Length, Is.EqualTo(4), "Do not silently compare a changed baseline catalog.");
                var other = probes.Where(p => p.region != "neck").ToArray(); Assert.That(other.Length, Is.EqualTo(3));
                var visual = cat.GetComponentInChildren<CatBreedVisualTag>().transform;
                var controller = cat.GetComponent<CharacterController>();
                Vector3 rootScale = Abs(cat.transform.lossyScale), visualScale = Abs(visual.lossyScale);
                float oldRadius = controller.radius * Mathf.Max(rootScale.x, rootScale.z);
                float height = Mathf.Max(oldRadius * 2, controller.height * rootScale.y);
                Vector3 centreLocal = Vector3.Scale(controller.center, cat.transform.lossyScale);
                EnsureQueries(cat.gameObject.scene);
                Vector3 outward = stand.position - target.position; outward.y = 0; outward.Normalize();
                var feeding = CatFeedingAlignmentCatalog.Load().Find(breed);
                var selected = new Dictionary<string, Candidate>();
                foreach (float radius in new[] { .30f, .32f, .34f, .36f, .38f, .40f, .42f, .44f })
                foreach (int angle in new[] { 0, 15, -15, 30, -30, 45, -45, 60, -60, 75, -75, 90, -90, 120, -120, 180 })
                {
                    Vector3 side = Quaternion.Euler(0, angle, 0) * outward;
                    Vector3 position = target.position + side * radius; position.y = sourceRoot.y;
                    Quaternion rotation = Quaternion.LookRotation(-side);
                    Physics.SyncTransforms();
                    bool oldBody = true, threeClear = true;
                    foreach (var probe in probes)
                    {
                        var hit = Capsule(guard, position + rotation * probe.start, position + rotation * probe.end, probe.radius,
                            false, breed, radius, angle, "original-body", probe.region);
                        oldBody &= hit.clear; if (probe.region != "neck") threeClear &= hit.clear;
                    }
                    Assert.That(oldBody, Is.EqualTo(cat.IsBodyPoseClear(position, rotation)), "Remote original capsules reproduce production exactly.");
                    Quaternion mapRotation = rotation * Quaternion.Inverse(cat.transform.rotation);
                    Vector3 trialStart = position + mapRotation * (visual.TransformPoint(shape.trialStart) - cat.transform.position);
                    Vector3 trialEnd = position + mapRotation * (visual.TransformPoint(shape.trialEnd) - cat.transform.position);
                    float trialRadius = shape.trialRadius * Mathf.Max(visualScale.x, visualScale.y, visualScale.z);
                    bool trialClear = Capsule(guard, trialStart, trialEnd, trialRadius, false, breed, radius, angle, "measured-oblique-neck", "neck").clear;
                    Vector3 centre = position + rotation * centreLocal;
                    Vector3 oldRise = Vector3.up * (height * .5f - oldRadius);
                    bool oldCC = Capsule(guard, centre - oldRise, centre + oldRise, oldRadius, true, breed, radius, angle, "original-controller", "controller").clear;
                    Assert.That(oldCC, Is.EqualTo(guard.IsControllerClear(position, rotation)), "Remote original controller reproduces production exactly.");
                    float scale = cat.transform.lossyScale.y / .5f;
                    Vector3 mouthOffset = feeding.mouthOffset * scale, useCentre = target.position - rotation * mouthOffset;
                    Vector3 delta = position - useCentre; delta.y = 0;
                    bool zone = delta.magnitude <= .16f * scale && Mathf.Abs((position + rotation * mouthOffset).y - target.position.y) <= .26f * scale;
                    bool floor = (bool)typeof(CareReachGeometryDiagnosticTests).GetMethod("FloorAt", Static).Invoke(null,
                        new object[] { cat, position, 0f });
                    Vector3 mappedFood = sourceRoot + sourceYaw * Quaternion.Inverse(rotation) * (target.position - position);
                    bool reach = CatCareReachGeometry.TrySolve(source, mappedFood, 1f, work, out var solution) && solution.Contact;
                    bool common = zone && floor && reach;
                    bool baseline = common && oldBody && oldCC;
                    bool neckOnly = common && threeClear && trialClear && oldCC;
                    rows.Add(Csv(breed, radius, angle, position, rotation.eulerAngles.y, trialStart, trialEnd, trialRadius, oldRadius, height, centreLocal,
                        oldBody, threeClear, trialClear, oldCC, zone, floor, reach, solution.foodDistance, baseline, neckOnly));
                    Select(selected, "oblique-neck-only", neckOnly && !baseline, position, rotation, radius, angle);
                    yield return null;
                }
                // No candidate is used to start an action while the original
                // production collider would reject it. Test only independent skin.
                foreach (var candidate in selected.Values.GroupBy(c => new { c.position, c.rotation }).Select(g =>
                    new Candidate { position = g.Key.position, rotation = g.Key.rotation, radius = g.First().radius,
                        angle = g.First().angle, variants = string.Join(";", g.Select(c => c.variants)) }))
                    yield return SkinAtCandidate(cat, guard, breed, candidate, skinRows);
            }
            Assert.That(rows.Count - 1, Is.EqualTo(256));
        }
        finally
        {
            Directory.CreateDirectory(Output);
            File.WriteAllLines(Path.Combine(Output, "care-neck-axis-trial-stances.csv"), rows);
            File.WriteAllLines(Path.Combine(Output, "care-neck-axis-trial-contacts.csv"), contacts);
            File.WriteAllLines(Path.Combine(Output, "care-neck-axis-trial-skin.csv"), skinRows);
        }
    }

    void EnsureQueries(Scene scene)
    {
        if (capsule == null)
        {
            var obj = new GameObject("QA remote care capsule") { hideFlags = HideFlags.HideAndDontSave };
            obj.transform.position = new Vector3(0, -20000, 0); SceneManager.MoveGameObjectToScene(obj, scene);
            capsule = obj.AddComponent<CapsuleCollider>(); capsule.direction = 2; capsule.isTrigger = true; capsule.enabled = true;
        }
    }
    bool Solid(CatBodyGuard guard, Collider collider) => (bool)Call(guard, "Solid", collider);
    Contact Capsule(CatBodyGuard guard, Vector3 a, Vector3 b, float radius, bool controller,
        string breed, float stanceRadius, int angle, string kind, string region)
    {
        capsule.radius = radius; capsule.height = Vector3.Distance(a, b) + radius * 2f;
        Quaternion rotation = (b - a).sqrMagnitude > .000001f ? Quaternion.LookRotation(b - a) : Quaternion.identity;
        var result = new Contact { clear = true };
        foreach (var collider in Physics.OverlapCapsule(a, b, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!Solid(guard, collider) || !Physics.ComputePenetration(capsule, (a + b) * .5f, rotation,
                collider, collider.transform.position, collider.transform.rotation, out var normal, out float depth)) continue;
            bool blocks = controller ? normal.y < .8f && depth > .002f : depth > .015f;
            result.clear &= !blocks;
            if (depth > result.maximumDepth) result = new Contact { clear = result.clear, maximumDepth = depth, normal = normal, collider = PathOf(collider.transform) };
            contacts.Add(Csv(breed, stanceRadius, angle, kind, region, PathOf(collider.transform), collider.GetType().Name, depth, normal, blocks));
        }
        return result;
    }
    static void Select(Dictionary<string, Candidate> selected, string variant, bool legal,
        Vector3 position, Quaternion rotation, float radius, int angle)
    {
        if (legal && (!selected.TryGetValue(variant, out var old) || radius < old.radius))
            selected[variant] = new Candidate { position = position, rotation = rotation, radius = radius, angle = angle, variants = variant };
    }
    IEnumerator SkinAtCandidate(CatMovement cat, CatBodyGuard guard, string breed, Candidate candidate, List<string> rows)
    {
        var animator = cat.GetComponentInChildren<Animator>(); var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var transforms = animator.GetComponentsInChildren<Transform>(true);
        var positions = transforms.Select(t => t.localPosition).ToArray(); var rotations = transforms.Select(t => t.localRotation).ToArray();
        var scales = transforms.Select(t => t.localScale).ToArray(); var mesh = new Mesh();
        var indices = CatBreedCatalog.Load().Find(breed).ContactVertexIndices;
        var depthMethod = typeof(CareAlignmentPolishTests).GetMethod("CareBlockerDepth", Static);
        var penetration = typeof(CatBodyGuard).GetMethod("Penetration", Instance);
        try
        {
            foreach (string kind in new[] { "Idle", "Walk" })
            {
                var clip = animator.runtimeAnimatorController.animationClips.First(c => c.name.EndsWith("|" + kind, StringComparison.Ordinal));
                for (int frame = 0; frame < 32; frame++)
                {
                    Vector3[] points;
                    try
                    {
                        Restore(); clip.SampleAnimation(animator.gameObject, clip.length * frame / 32f);
                        skin.BakeMesh(mesh, true); var vertices = mesh.vertices;
                        // Preserve the real gameplay scale. InverseTransformPoint
                        // would divide by root scale and double a half-scale cat.
                        Quaternion sourceToCandidate = candidate.rotation * Quaternion.Inverse(cat.transform.rotation);
                        points = indices.Select(i => candidate.position + sourceToCandidate *
                            (skin.transform.TransformPoint(vertices[i]) - cat.transform.position)).ToArray();
                    }
                    finally { Restore(); }
                    Bounds bounds = new Bounds(points[0], Vector3.zero); foreach (var point in points) bounds.Encapsulate(point);
                    int inspected = 0;
                    foreach (var collider in Physics.OverlapBox(bounds.center, bounds.extents + Vector3.one * .002f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (!Solid(guard, collider)) continue; inspected++;
                        object result = depthMethod.Invoke(null, new object[] { guard, penetration, collider, points });
                        rows.Add(Csv(breed, candidate.variants, candidate.radius, candidate.angle, kind, frame / 32f,
                            candidate.position, candidate.rotation.eulerAngles.y, PathOf(collider.transform), collider.GetType().Name,
                            collider is MeshCollider mc ? UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh) : "",
                            Read<int>(result, "inside"), Read<float>(result, "depth"), Read<Vector3>(result, "point")));
                    }
                    if (inspected == 0) rows.Add(Csv(breed, candidate.variants, candidate.radius, candidate.angle, kind, frame / 32f,
                        candidate.position, candidate.rotation.eulerAngles.y, "<clear>", "", "", 0, 0, Vector3.zero));
                    yield return null;
                }
            }
        }
        finally { Restore(); Object.DestroyImmediate(mesh); }
        void Restore() { for (int i = 0; i < transforms.Length; i++) { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; } }
    }
    static Vector3 Abs(Vector3 p) => new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
    static string PathOf(Transform t) { string p = t.name; while (t.parent != null) { t = t.parent; p = t.name + "/" + p; } return p; }
    static string Csv(params object[] values) => string.Join(",", values.Select(value =>
    {
        string text = value is Vector3 p ? FormattableString.Invariant($"{p.x:F7};{p.y:F7};{p.z:F7}") :
            value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }));
}
#endif
