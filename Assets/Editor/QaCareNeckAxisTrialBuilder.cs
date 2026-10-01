#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Editor QA metrology only. This class never creates/updates a Unity asset.
public static class QaCareNeckAxisTrialBuilder
{
    [Serializable] public sealed class Report { public Entry[] entries; public string coordinateSpace = "CatBreedVisualTag local; home scale .5"; }
    [Serializable] public sealed class Entry
    {
        public string breed;
        public Vector3 neckMin, neckMax, neckCentre, neckSize, torsoMin, torsoMax;
        public float paddingSource, minimumNeckClearanceAtHome, torsoMaxLateralAtHome, coreRadiusAtHome;
        public int poses, neckVerticesPerPose, sampledNeckVertices, sampledTorsoVertices;
        public Vector3 trialStart, trialEnd, trialAxis, principalAxis;
        public float trialRadius, trialVolume, minimumCapsuleClearanceAtHome;
        public int trialAxes;
    }
    public static string MeasureTo(string outputPath)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Measure outside Play Mode.");
        string project = Directory.GetParent(Application.dataPath).FullName;
        string qa = Path.GetFullPath(Path.Combine(project, "Docs", "QA")) + Path.DirectorySeparatorChar;
        string path = Path.GetFullPath(outputPath);
        if (!path.StartsWith(qa, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("QA report path required.");
        var breeds = CatBreedCatalog.Load();
        var region = typeof(CatBodyGuardBuilder).GetMethod("Region", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(BoneWeight), typeof(Transform[]) }, null);
        var names = typeof(CatBodyGuardBuilder).GetField("Regions", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as string[];
        if (region == null || region.ReturnType != typeof(int) || names == null ||
            !names.SequenceEqual(new[] { "pelvis", "chest", "neck", "head" }))
            throw new InvalidOperationException("Expected current source-vertex grouping; do not reinterpret an unfamiliar builder.");
        var entries = new List<Entry>(); var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var breed in breeds.Entries)
            {
                var visual = CatBreedVisualFactory.Create(breed, breeds.GameplayController, null, "QA exact source care bounds");
                SceneManager.MoveGameObjectToScene(visual, preview); var mesh = new Mesh();
                try
                {
                    var animator = visual.GetComponentInChildren<Animator>(); animator.enabled = false;
                    var skin = visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    var weights = skin.sharedMesh.boneWeights; var bones = skin.bones;
                    var neckIndices = new List<int>(); var torsoIndices = new List<int>();
                    foreach (int index in breed.ContactVertexIndices)
                    {
                        if (index < 0 || index >= weights.Length) continue;
                        int group = (int)region.Invoke(null, new object[] { weights[index], bones });
                        if (group == 2) neckIndices.Add(index);
                        if (group == 0 || group == 1) torsoIndices.Add(index);
                    }
                    if (neckIndices.Count == 0 || torsoIndices.Count == 0) throw new InvalidOperationException("Missing assigned source vertices: " + breed.Id);
                    var neck = new List<Vector3>(neckIndices.Count * 96); var torso = new List<Vector3>(torsoIndices.Count * 96);
                    int poses = 0;
                    foreach (string kind in new[] { "Idle", "Walk", "Run" })
                    {
                        var clip = CatHomeLocomotionBuilder.FindClip(animator, kind);
                        for (int phase = 0; phase < 32; phase++)
                        {
                            clip.SampleAnimation(animator.gameObject, clip.length * phase / 32f);
                            skin.BakeMesh(mesh, true); var vertices = mesh.vertices;
                            foreach (int index in neckIndices) neck.Add(visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index])));
                            foreach (int index in torsoIndices) torso.Add(visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index])));
                            poses++;
                        }
                    }
                    Bounds neckBounds = BoundsOf(neck), torsoBounds = BoundsOf(torso);
                    const float sourcePadding = .01f; // exactly5mm at gameplay half scale
                    Bounds padded = neckBounds; padded.Expand(sourcePadding * 2f);
                    float minimum = float.PositiveInfinity;
                    foreach (Vector3 point in neck)
                    {
                        Vector3 edge = padded.extents - Abs(point - padded.center);
                        minimum = Mathf.Min(minimum, edge.x, edge.y, edge.z);
                        if (edge.x < sourcePadding - .00001f || edge.y < sourcePadding - .00001f || edge.z < sourcePadding - .00001f)
                            throw new InvalidOperationException("A trial box lost assigned neck skin/padding: " + breed.Id);
                    }
                    if (poses != 96 || neck.Count != neckIndices.Count * 96) throw new InvalidOperationException("Incomplete source coverage.");
                    var fitted = Fit(neck, sourcePadding, out Vector3 principal, out int axes);
                    float capsuleMargin = float.PositiveInfinity;
                    foreach (var point in neck)
                    {
                        float margin = fitted.radius - SegmentDistance(point, fitted.start, fitted.end);
                        capsuleMargin = Mathf.Min(capsuleMargin, margin);
                        if (margin < sourcePadding - .00001f) throw new InvalidOperationException("Free-axis capsule lost neck skin/padding: " + breed.Id);
                    }
                    float lateral = Mathf.Max(Mathf.Abs(torsoBounds.min.x), Mathf.Abs(torsoBounds.max.x)) * .5f;
                    entries.Add(new Entry { breed = breed.Id, neckMin = neckBounds.min, neckMax = neckBounds.max,
                        neckCentre = padded.center, neckSize = padded.size, torsoMin = torsoBounds.min, torsoMax = torsoBounds.max,
                        paddingSource = sourcePadding, minimumNeckClearanceAtHome = minimum * .5f,
                        torsoMaxLateralAtHome = lateral, coreRadiusAtHome = lateral + .01f,
                        trialStart = fitted.start, trialEnd = fitted.end, trialAxis = fitted.axis, principalAxis = principal,
                        trialRadius = fitted.radius, trialVolume = fitted.volume, trialAxes = axes, minimumCapsuleClearanceAtHome = capsuleMargin * .5f,
                        poses = poses, neckVerticesPerPose = neckIndices.Count, sampledNeckVertices = neck.Count, sampledTorsoVertices = torso.Count });
                }
                finally { Object.DestroyImmediate(mesh); Object.DestroyImmediate(visual); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        string json = JsonUtility.ToJson(new Report { entries = entries.ToArray() }, true);
        Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, json);
        return path + " â€” " + entries.Count + " breeds, 96 poses each, exact free-axis neck capsule plus 5mm containment; no asset writes.";
    }
    struct FitResult { public Vector3 start, end, axis; public float radius, volume; }
    static FitResult Fit(List<Vector3> points, float padding, out Vector3 principal, out int axisCount)
    {
        Vector3 mean = Vector3.zero; foreach (var p in points) mean += p; mean /= points.Count;
        float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
        foreach (var point in points) { var p = point - mean; xx += p.x*p.x; xy += p.x*p.y; xz += p.x*p.z; yy += p.y*p.y; yz += p.y*p.z; zz += p.z*p.z; }
        // Power iteration from three starts prevents a symmetry-orthogonal seed
        // from hiding the largest eigenvector of the actual covariance.
        principal = Vector3.up; float largest = -1;
        foreach (var seed in new[] { Vector3.right, Vector3.up, Vector3.forward })
        {
            Vector3 v = seed;
            for (int i = 0; i < 40; i++)
            { Vector3 next = new Vector3(xx*v.x+xy*v.y+xz*v.z, xy*v.x+yy*v.y+yz*v.z, xz*v.x+yz*v.y+zz*v.z); if (next.sqrMagnitude < 1e-12f) break; v = next.normalized; }
            float eigenvalue = Vector3.Dot(v, new Vector3(xx*v.x+xy*v.y+xz*v.z, xy*v.x+yy*v.y+yz*v.z, xz*v.x+yz*v.y+zz*v.z));
            if (eigenvalue > largest) { largest = eigenvalue; principal = v; }
        }
        var axes = new List<Vector3> { Vector3.right, Vector3.up, Vector3.forward, principal };
        // Includes oblique long-neck directions that the production XYZ fitter
        // cannot represent, then refines around the measured principal axis.
        for (int step = 1; step < 36; step++) axes.Add(Quaternion.AngleAxis(step * 5f, Vector3.right) * Vector3.up);
        foreach (Vector3 around in new[] { Vector3.right, Vector3.up, Vector3.forward })
            foreach (float degrees in new[] { -10f, -5f, 5f, 10f }) axes.Add(Quaternion.AngleAxis(degrees, around) * principal);
        FitResult best = new FitResult { volume = float.PositiveInfinity }; axisCount = axes.Count;
        foreach (Vector3 axis in axes)
        {
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.x) < .8f ? Vector3.right : Vector3.up).normalized;
            Vector3 v = Vector3.Cross(axis, u).normalized;
            float minU = float.PositiveInfinity, maxU = float.NegativeInfinity, minV = minU, maxV = maxU;
            foreach (var point in points) { float a = Vector3.Dot(point,u), b=Vector3.Dot(point,v); minU=Mathf.Min(minU,a);maxU=Mathf.Max(maxU,a);minV=Mathf.Min(minV,b);maxV=Mathf.Max(maxV,b); }
            Vector3 origin = u * ((minU+maxU)*.5f) + v * ((minV+maxV)*.5f);
            float maximumRadial = 0;
            foreach (var point in points) { var delta=point-origin; float t=Vector3.Dot(delta,axis); maximumRadial=Mathf.Max(maximumRadial,(delta-axis*t).magnitude); }
            foreach (float extra in new[] { 0f, .005f, .01f, .02f, .03f, .04f, .06f, .08f })
            {
                float innerRadius = maximumRadial + extra;
                float low = float.PositiveInfinity, high = float.NegativeInfinity;
                foreach (var point in points)
                {
                    var delta=point-origin; float t=Vector3.Dot(delta,axis), radial=(delta-axis*t).sqrMagnitude;
                    float cap=Mathf.Sqrt(Mathf.Max(0,innerRadius*innerRadius-radial));
                    low=Mathf.Min(low,t+cap); high=Mathf.Max(high,t-cap);
                }
                if (low > high) low = high = (low+high)*.5f;
                Vector3 start=origin+axis*low,end=origin+axis*high;
                // Recompute the exact distance before padding, so accumulated
                // projection/cap rounding cannot discard a source vertex.
                float radius = 0; foreach(var point in points) radius=Mathf.Max(radius,SegmentDistance(point,start,end)); radius+=padding;
                float volume=Mathf.PI*radius*radius*(Vector3.Distance(start,end)+4f*radius/3f);
                if(volume<best.volume) best=new FitResult{start=start,end=end,axis=axis,radius=radius,volume=volume};
            }
        }
        return best;
    }
    static float SegmentDistance(Vector3 point, Vector3 a, Vector3 b)
    { Vector3 line=b-a; float t=line.sqrMagnitude>1e-12f?Mathf.Clamp01(Vector3.Dot(point-a,line)/line.sqrMagnitude):0;return Vector3.Distance(point,a+line*t); }
    static Bounds BoundsOf(List<Vector3> points) { var b = new Bounds(points[0], Vector3.zero); foreach (var p in points) b.Encapsulate(p); return b; }
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
}
#endif
