using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Measures anatomical skin, never the renderer's tail-inclusive bounding box.</summary>
public static class CatBodyGuardBuilder
{
    public const string AssetPath = "Assets/Resources/Home/CatBodyGuardCatalog.asset";
    private static readonly string[] Regions = { "pelvis", "chest", "neck", "head" };

    [MenuItem("Tools/Cat Home/Cat/Measure Body Guard")]
    private static void BuildMenu() => Debug.Log(Build());

    /// <summary>Call outside Play Mode after compilation. Returns measured dimensions for QA.</summary>
    public static string Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Measure the body guard outside Play Mode.");
        var breeds = CatBreedCatalog.Load();
        if (breeds == null) throw new InvalidOperationException("Cat breed catalog is missing.");
        var entries = new List<CatBodyGuardCatalog.Entry>();
        var report = new StringBuilder("breed,region,axis,radius_at_home_scale,start_x,start_y,start_z,end_x,end_y,end_z,max_excess_x,max_excess_y,max_excess_z,vertices,poses\n");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var breed in breeds.Entries)
            {
                var visual = CatBreedVisualFactory.Create(breed, breeds.GameplayController, null, "Body guard measurement");
                SceneManager.MoveGameObjectToScene(visual, preview);
                var mesh = new Mesh();
                try
                {
                    var animator = visual.GetComponentInChildren<Animator>();
                    animator.enabled = false;
                    var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    var weights = skin.sharedMesh.boneWeights;
                    var bones = skin.bones;
                    var groups = new int[weights.Length];
                    for (int v = 0; v < groups.Length; v++) groups[v] = -1;
                    foreach (int v in breed.ContactVertexIndices)
                        if (v >= 0 && v < groups.Length) groups[v] = Region(weights[v], bones);
                    var points = Regions.Select(_ => new List<Vector3>()).ToArray();
                    int poses = 0;
                    foreach (string kind in new[] { "Idle", "Walk", "Run" })
                    {
                        var clip = CatHomeLocomotionBuilder.FindClip(animator, kind);
                        // The root is unscaled; runtime applies the actual visual scale.
                        // 32 phases also cover the forward head excursion between steps.
                        for (int frame = 0; frame < 32; frame++)
                        {
                            clip.SampleAnimation(animator.gameObject, clip.length * frame / 32f);
                            skin.BakeMesh(mesh, true);
                            var vertices = mesh.vertices;
                            for (int v = 0; v < groups.Length; v++)
                                if (groups[v] >= 0)
                                    points[groups[v]].Add(visual.transform.InverseTransformPoint(
                                        skin.transform.TransformPoint(vertices[v])));
                            poses++;
                        }
                    }
                    var probes = new CatBodyGuardCatalog.Probe[Regions.Length];
                    for (int region = 0; region < probes.Length; region++)
                    {
                        if (points[region].Count == 0)
                            throw new InvalidOperationException(breed.Id + " has no " + Regions[region] + " skin samples.");
                        probes[region] = Fit(Regions[region], points[region], out int axis, out Vector3 excess);
                        var p = probes[region];
                        report.AppendLine(string.Join(",", breed.Id, p.region, "XYZ"[axis], F(p.radius * .5f),
                            F(p.start.x * .5f), F(p.start.y * .5f), F(p.start.z * .5f),
                            F(p.end.x * .5f), F(p.end.y * .5f), F(p.end.z * .5f),
                            F(excess.x * .5f), F(excess.y * .5f), F(excess.z * .5f), points[region].Count / poses, poses));
                    }
                    entries.Add(new CatBodyGuardCatalog.Entry { breedId = breed.Id, probes = probes,
                        sampledPoses = poses, sampledVertices = points.Sum(p => p.Count) });
                }
                finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(visual); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        var catalog = AssetDatabase.LoadAssetAtPath<CatBodyGuardCatalog>(AssetPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CatBodyGuardCatalog>();
            AssetDatabase.CreateAsset(catalog, AssetPath);
        }
        catalog.EditorConfigure(entries.ToArray());
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
        return report.ToString();
    }

    private static int Region(BoneWeight weight, Transform[] bones)
    {
        var totals = new float[4];
        Add(totals, bones, weight.boneIndex0, weight.weight0);
        Add(totals, bones, weight.boneIndex1, weight.weight1);
        Add(totals, bones, weight.boneIndex2, weight.weight2);
        Add(totals, bones, weight.boneIndex3, weight.weight3);
        float sum = totals.Sum();
        // Legs/paws remain covered by the existing CharacterController. Their
        // long airborne strides must not enlarge the ordinary torso envelope.
        if (sum < .5f) return -1;
        int best = 0;
        for (int i = 1; i < totals.Length; i++) if (totals[i] > totals[best]) best = i;
        return best;
    }

    private static void Add(float[] totals, Transform[] bones, int index, float weight)
    {
        if (weight <= 0f || index < 0 || index >= bones.Length || bones[index] == null) return;
        var bone = bones[index];
        string name = bone.name;
        int region = name == "DEF-spine" || name == "DEF-spine.001" ? 0 :
            name == "DEF-spine.002" || name == "DEF-spine.003" ? 1 :
            name == "DEF-spine.004" || name == "DEF-spine.005" ? 2 : -1;
        // Face, jaw and ears are real head skin; tail/limb descendants are not.
        if (region < 0)
            for (var parent = bone; parent != null; parent = parent.parent)
                if (parent.name == "DEF-spine.006") { region = 3; break; }
        if (region >= 0) totals[region] += weight;
    }

    private static CatBodyGuardCatalog.Probe Fit(string region, List<Vector3> points,
        out int selectedAxis, out Vector3 supportExcess)
    {
        var bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points) bounds.Encapsulate(point);
        var best = default(CatBodyGuardCatalog.Probe);
        float bestVolume = float.PositiveInfinity;
        selectedAxis = 0;
        // A head is taller than it is wide. A compulsory longitudinal capsule
        // turns ear height / vertical bob into a large horizontal sphere.
        // Choose the least-volume complete envelope across all three axes.
        for (int axis = 0; axis < 3; axis++)
        {
            var candidate = FitAxis(region, points, bounds, axis);
            float radius = candidate.radius;
            float length = Vector3.Distance(candidate.start, candidate.end);
            float volume = Mathf.PI * radius * radius * (length + 4f * radius / 3f);
            if (volume >= bestVolume) continue;
            bestVolume = volume;
            best = candidate;
            selectedAxis = axis;
        }
        Vector3 capsuleMin = Vector3.Min(best.start, best.end) - Vector3.one * best.radius;
        Vector3 capsuleMax = Vector3.Max(best.start, best.end) + Vector3.one * best.radius;
        supportExcess = Vector3.Max(bounds.min - capsuleMin, capsuleMax - bounds.max);
        return best;
    }

    private static CatBodyGuardCatalog.Probe FitAxis(string region, List<Vector3> points, Bounds bounds, int axis)
    {
        Vector3 centre = bounds.center;
        float radialSquared = 0f;
        foreach (var point in points)
        {
            Vector3 transverse = point - centre;
            transverse[axis] = 0f;
            radialSquared = Mathf.Max(radialSquared, transverse.sqrMagnitude);
        }
        float radius = Mathf.Sqrt(radialSquared);
        float start = float.PositiveInfinity, end = float.NegativeInfinity;
        foreach (var point in points)
        {
            Vector3 transverse = point - centre;
            transverse[axis] = 0f;
            float cap = Mathf.Sqrt(Mathf.Max(0f, radialSquared - transverse.sqrMagnitude));
            start = Mathf.Min(start, point[axis] + cap);
            end = Mathf.Max(end, point[axis] - cap);
        }
        if (start > end) start = end = (start + end) * .5f;
        Vector3 a = centre, b = centre;
        a[axis] = start;
        b[axis] = end;
        // Add clearance after fitting the cap endpoints, preserving 5 mm in
        // every direction at the gameplay half scale, including the nose.
        return new CatBodyGuardCatalog.Probe { region = region, radius = radius + .01f, start = a, end = b };
    }

    private static string F(float value) => value.ToString("F5", CultureInfo.InvariantCulture);
}
