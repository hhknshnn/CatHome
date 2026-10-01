using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Editor-only surface partition. Every source triangle belongs to one leaf,
// and all its vertices share that leaf's convex capsule. This covers the whole
// face, including partition seams, rather than merely covering isolated vertices.
public static class CatJumpSurfaceCoverageBuilder
{
    public struct Result
    {
        public CatBodyGuardCatalog.Probe envelope;
        public CatBodyGuardCatalog.Probe[] parts;
        public float minimumContainment;
    }
    static readonly MethodInfo FitBody = typeof(CatPawReachBuilder).GetMethod("FitBody", BindingFlags.Static | BindingFlags.NonPublic);
    public static int[] RegionTriangles(int[] triangles, int[] groups, int region)
    {
        var selected = new List<int>();
        for (int t = 0; t < triangles.Length; t += 3)
        {
            int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
            if (groups[a] != region && groups[b] != region && groups[c] != region) continue;
            selected.Add(a); selected.Add(b); selected.Add(c);
        }
        for (int v = 0; v < groups.Length; v++)
            if (groups[v] == region && !selected.Contains(v))
            { selected.Add(v); selected.Add(v); selected.Add(v); }
        return selected.ToArray();
    }
    public static Result Fit(Vector3[] vertices, int[] triangles, string region)
    {
        if (FitBody == null || triangles == null || triangles.Length == 0 || triangles.Length % 3 != 0)
            throw new InvalidOperationException("A measured, nonempty source body triangle set is required.");
        var leaf = Enumerable.Range(0, triangles.Length / 3).ToArray();
        var leaves = new List<int[]> { leaf };
        while (leaves.Count < CatJumpClearanceCatalog.MaxRegionProbes)
        {
            int largest = -1; float largestVolume = -1f;
            for (int i = 0; i < leaves.Count; i++)
            {
                if (leaves[i].Length < 2) continue;
                var bounds = BoundsOf(vertices, triangles, leaves[i]);
                float score = bounds.size.sqrMagnitude * leaves[i].Length;
                if (score > largestVolume) { largestVolume = score; largest = i; }
            }
            if (largest < 0) break;
            var source = leaves[largest];
            var centroidBounds = new Bounds(Centre(vertices, triangles, source[0]), Vector3.zero);
            foreach (int t in source) centroidBounds.Encapsulate(Centre(vertices, triangles, t));
            Vector3 size = centroidBounds.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;
            var sorted = source.OrderBy(t => Centre(vertices, triangles, t)[axis]).ThenBy(t => t).ToArray();
            int middle = sorted.Length / 2;
            leaves[largest] = sorted.Take(middle).ToArray();
            leaves.Insert(largest + 1, sorted.Skip(middle).ToArray());
        }
        var probes = new CatBodyGuardCatalog.Probe[leaves.Count];
        float minimum = float.PositiveInfinity;
        for (int i = 0; i < leaves.Count; i++)
        {
            var points = Points(vertices, triangles, leaves[i]);
            probes[i] = Capsule(region + "." + i, points);
            // Each assigned face is inside a single convex capsule, so every
            // barycentric point is also inside it with the existing 2mm shell.
            foreach (var point in points) minimum = Mathf.Min(minimum, Contains(probes[i], point));
        }
        var all = Points(vertices, triangles, leaf);
        var broad = Capsule(region + ".triangles", all);
        foreach (var point in all) minimum = Mathf.Min(minimum, Contains(broad, point));
        if (minimum < .00399f) throw new InvalidOperationException("Source body triangle escaped its measured capsule.");
        return new Result { envelope = broad, parts = probes, minimumContainment = minimum };
    }
    public static float TriangleContainment(Vector3[] vertices, int[] triangles, CatBodyGuardCatalog.Probe[] probes)
    {
        if (probes == null || probes.Length == 0) return float.NegativeInfinity;
        float worst = float.PositiveInfinity;
        for (int t = 0; t < triangles.Length; t += 3)
        {
            float best = float.NegativeInfinity;
            foreach (var probe in probes)
            {
                float face = Mathf.Min(Contains(probe, vertices[triangles[t]]),
                    Mathf.Min(Contains(probe, vertices[triangles[t + 1]]), Contains(probe, vertices[triangles[t + 2]])));
                best = Mathf.Max(best, face);
            }
            worst = Mathf.Min(worst, best);
        }
        return worst;
    }
    static CatBodyGuardCatalog.Probe Capsule(string name, List<Vector3> points) =>
        (CatBodyGuardCatalog.Probe)FitBody.Invoke(null, new object[] { name, points });
    static float Contains(CatBodyGuardCatalog.Probe p, Vector3 v)
    {
        Vector3 axis = p.end - p.start;
        float t = axis.sqrMagnitude > 1e-12f ? Mathf.Clamp01(Vector3.Dot(v - p.start, axis) / axis.sqrMagnitude) : 0;
        return p.radius - Vector3.Distance(v, p.start + t * axis);
    }
    static Vector3 Centre(Vector3[] vertices, int[] triangles, int t) =>
        (vertices[triangles[t * 3]] + vertices[triangles[t * 3 + 1]] + vertices[triangles[t * 3 + 2]]) / 3f;
    static List<Vector3> Points(Vector3[] vertices, int[] triangles, int[] ids)
    {
        var indices = new HashSet<int>();
        foreach (int t in ids) for (int k = 0; k < 3; k++) indices.Add(triangles[t * 3 + k]);
        return indices.OrderBy(i => i).Select(i => vertices[i]).ToList();
    }
    static Bounds BoundsOf(Vector3[] vertices, int[] triangles, int[] ids)
    {
        var result = new Bounds(vertices[triangles[ids[0] * 3]], Vector3.zero);
        foreach (int t in ids) for (int k = 0; k < 3; k++) result.Encapsulate(vertices[triangles[t * 3 + k]]);
        return result;
    }
}
