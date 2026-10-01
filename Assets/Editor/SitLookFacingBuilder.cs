using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Bake a small set of real, camera-visible product surfaces for seated gaze/contact.</summary>
public static class SitLookFacingBuilder
{
    const float Grid = .10f;
    const int MaximumTargets = 32;
    struct Triangle
    {
        public Vector3 a, b, c, normal;
        public string material;
    }

    public static void Configure(GameObject root, StoreCatalogAsset definition)
    {
        var activities = root.GetComponentsInChildren<SitLookActivity>(true);
        if (activities.Length == 0) return;
        Vector3 camera = Quaternion.Inverse(Quaternion.Euler(0, definition.DefaultYaw, 0)) *
                         (HomeRoomCameraProfile.Position - definition.DefaultPosition);
        ConfigureTargets(root.transform, activities, camera, definition.ProductId);
    }

    public static void ConfigureSceneSurface(SitLookActivity activity, Transform surface)
    {
        if (activity == null || surface == null) throw new ArgumentNullException("Activity and visible scene surface are required.");
        Vector3 camera = HomeRoomCameraProfile.Position;
        foreach (var sceneRoot in activity.gameObject.scene.GetRootGameObjects())
        foreach (var candidate in sceneRoot.GetComponentsInChildren<Camera>(true))
            if (candidate.GetComponent<HomeWorldViewport>() != null) camera = candidate.transform.position;
        ConfigureTargets(surface, new[] { activity }, surface.InverseTransformPoint(camera), activity.name + "/" + surface.name);
    }

    static void ConfigureTargets(Transform root, SitLookActivity[] activities, Vector3 camera, string identity)
    {
        var triangles = ReadTriangles(root, out Bounds bounds);
        if (triangles.Count == 0)
            throw new InvalidOperationException(identity + ": no rendered surface for gaze targets.");
        foreach (var activity in activities)
        {
            if (activity.LookPoint == null) continue;
            if (activity.StoreProductId == HomeStoreService.BalconyRailingFlowersId &&
                activity.ReactionKind != SitLookReaction.Sit)
            {
                ConfigureFlowerLeaves(root, activity, triangles);
                continue;
            }
            bool contact = activity.ReactionKind != SitLookReaction.Sit;
            float authoredY = root.InverseTransformPoint(activity.LookPoint.position).y;
            float desiredY = contact ? authoredY : Mathf.Lerp(bounds.min.y, bounds.max.y, .58f);
            var cells = new Dictionary<Vector3Int, Vector3>();
            foreach (var triangle in triangles)
            {
                Vector3 centre = (triangle.a + triangle.b + triangle.c) / 3f;
                if (Vector3.Dot(triangle.normal, (camera - centre).normalized) <= .10f) continue;
                if (contact)
                {
                    // Intersect the actual triangle with the authored paw-height
                    // plane. A tall ornament must not replace a reachable handle.
                    var cuts = new List<Vector3>(3);
                    Cut(triangle.a, triangle.b, authoredY, cuts);
                    Cut(triangle.b, triangle.c, authoredY, cuts);
                    Cut(triangle.c, triangle.a, authoredY, cuts);
                    if (cuts.Count >= 2)
                    {
                        int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(cuts[0], cuts[1]) / Grid));
                        for (int i = 0; i <= steps; i++) Add(Vector3.Lerp(cuts[0], cuts[1], (float)i / steps), desiredY, cells);
                    }
                    if (Mathf.Abs(centre.y - authoredY) <= .025f) Add(centre, desiredY, cells);
                }
                else
                {
                    // Barycentric samples remain on the final mesh, including
                    // broad two-triangle pictures and shelf fronts.
                    float edge = Mathf.Max(Vector3.Distance(triangle.a, triangle.b),
                        Mathf.Max(Vector3.Distance(triangle.b, triangle.c), Vector3.Distance(triangle.c, triangle.a)));
                    int steps = Mathf.Clamp(Mathf.CeilToInt(edge / Grid), 1, 24);
                    for (int i = 0; i <= steps; i++) for (int j = 0; j <= steps - i; j++)
                    {
                        Vector3 p = triangle.a + (triangle.b - triangle.a) * ((float)i / steps) +
                                    (triangle.c - triangle.a) * ((float)j / steps);
                        if (p.y < Mathf.Lerp(bounds.min.y, bounds.max.y, .28f) ||
                            p.y > Mathf.Lerp(bounds.min.y, bounds.max.y, .90f)) continue;
                        Add(p, desiredY, cells);
                    }
                }
            }
            var candidates = new List<Vector3>(cells.Values);
            candidates.Sort((a, b) =>
            {
                int height = Mathf.Abs(a.y - desiredY).CompareTo(Mathf.Abs(b.y - desiredY));
                if (height != 0) return height;
                int x = a.x.CompareTo(b.x); return x != 0 ? x : a.z.CompareTo(b.z);
            });
            var selected = new List<Vector3>(MaximumTargets);
            while (candidates.Count > 0 && selected.Count < MaximumTargets)
            {
                int best = 0; float distance = -1;
                if (selected.Count > 0) for (int i = 0; i < candidates.Count; i++)
                {
                    float nearest = float.PositiveInfinity;
                    foreach (Vector3 point in selected) nearest = Mathf.Min(nearest, (point - candidates[i]).sqrMagnitude);
                    if (nearest > distance) { distance = nearest; best = i; }
                }
                Vector3 candidate = candidates[best]; candidates.RemoveAt(best);
                if (Occluded(camera, candidate, triangles)) continue;
                selected.Add(candidate);
            }
            if (selected.Count == 0)
                throw new InvalidOperationException(identity + ": no visible " +
                    (contact ? "surface at authored paw height " + authoredY.ToString("F3") : "gaze surface") + ".");
            var local = new Vector3[selected.Count];
            for (int i = 0; i < local.Length; i++)
                local[i] = activity.transform.InverseTransformPoint(root.TransformPoint(selected[i]));
            activity.EditorConfigureVisibleLookTargets(local);
            EditorUtility.SetDirty(activity);
        }
    }

    /// <summary>Refresh only this product's measured contact points; no product or room rebuild.</summary>
    public static int ConfigureFlowerContacts(SitLookActivity activity)
    {
        if (activity == null || activity.StoreProductId != HomeStoreService.BalconyRailingFlowersId)
            throw new ArgumentException("The railing flower activity is required.");
        var triangles = ReadTriangles(activity.transform, out _);
        return ConfigureFlowerLeaves(activity.transform, activity, triangles);
    }

    static int ConfigureFlowerLeaves(Transform root, SitLookActivity activity, List<Triangle> triangles)
    {
        // In this measured source the oak box ends at .48 m, the separate mint
        // leaf blades occupy .494-.556 m, and the flowers start at .704 m.
        // Mint ornaments down at .15 m are part of the box, never paw targets.
        float boxTop = float.NegativeInfinity;
        foreach (var triangle in triangles)
            if (triangle.material != null && triangle.material.Contains("CH_Cream_Oak"))
                boxTop = Mathf.Max(boxTop, triangle.a.y, triangle.b.y, triangle.c.y);
        if (float.IsNegativeInfinity(boxTop)) throw new InvalidOperationException("Railing flower box role was not found.");
        var cells = new Dictionary<Vector3Int, Vector3>();
        foreach (var triangle in triangles)
        {
            if (triangle.material == null || !triangle.material.Contains("CH_MintBright") || triangle.normal.z <= .05f) continue;
            float low = Mathf.Min(triangle.a.y, triangle.b.y, triangle.c.y);
            float high = Mathf.Max(triangle.a.y, triangle.b.y, triangle.c.y);
            // Whole triangles in the low leaf band exclude the tall stem
            // triangles and stay on rendered leaf geometry at every sample.
            if (low <= boxTop + .005f || high >= boxTop + .10f) continue;
            foreach (var point in new[] { triangle.a, triangle.b, triangle.c, (triangle.a + triangle.b + triangle.c) / 3f })
            {
                var cell = new Vector3Int(Mathf.FloorToInt(point.x / .04f),
                    Mathf.FloorToInt(point.y / .04f), Mathf.FloorToInt(point.z / .04f));
                if (!cells.TryGetValue(cell, out var old) || point.z > old.z) cells[cell] = point;
            }
        }
        var candidates = new List<Vector3>(cells.Values);
        candidates.Sort((a, b) => { int x = a.x.CompareTo(b.x); return x != 0 ? x : b.z.CompareTo(a.z); });
        var selected = new List<Vector3>();
        while (candidates.Count > 0 && selected.Count < MaximumTargets)
        {
            int best = 0; float farthest = -1;
            if (selected.Count > 0)
                for (int i = 0; i < candidates.Count; i++)
                {
                    float nearest = float.PositiveInfinity;
                    foreach (var old in selected) nearest = Mathf.Min(nearest, (old - candidates[i]).sqrMagnitude);
                    if (nearest > farthest) { farthest = nearest; best = i; }
                }
            selected.Add(activity.transform.InverseTransformPoint(root.TransformPoint(candidates[best])));
            candidates.RemoveAt(best);
        }
        if (selected.Count == 0) throw new InvalidOperationException("No measured railing leaf contact surfaces found.");
        activity.EditorConfigureVisibleLookTargets(selected.ToArray());
        EditorUtility.SetDirty(activity);
        return selected.Count;
    }

    static void Add(Vector3 point, float desiredY, Dictionary<Vector3Int, Vector3> cells)
    {
        var key = new Vector3Int(Mathf.FloorToInt(point.x / Grid), Mathf.FloorToInt(point.y / Grid), Mathf.FloorToInt(point.z / Grid));
        if (!cells.TryGetValue(key, out Vector3 old) || Mathf.Abs(point.y - desiredY) < Mathf.Abs(old.y - desiredY)) cells[key] = point;
    }

    static void Cut(Vector3 a, Vector3 b, float y, List<Vector3> points)
    {
        if (Mathf.Abs(a.y - y) < .00001f) points.Add(a);
        if ((a.y - y) * (b.y - y) < 0f) points.Add(Vector3.Lerp(a, b, (y - a.y) / (b.y - a.y)));
    }

    static List<Triangle> ReadTriangles(Transform root, out Bounds bounds)
    {
        var result = new List<Triangle>(); bounds = new Bounds(); bool first = true;
        // Ownership hides these exact containers in an unowned prefab. Read
        // their future owned geometry without reviving inactive legacy children
        // or changing any GameObject's actual authoring state.
        var ownershipGates = new HashSet<Transform>();
        foreach (var placement in root.GetComponentsInChildren<HomeProductPlacement>(true))
            ownershipGates.Add(placement.MovableRoot);
        foreach (var activity in root.GetComponentsInChildren<CatActivity>(true))
        {
            var content = new SerializedObject(activity).FindProperty("unlockedContent").objectReferenceValue as GameObject;
            if (content != null) ownershipGates.Add(content.transform);
        }
        foreach (var display in root.GetComponentsInChildren<StoreProductDisplay>(true))
        {
            var content = new SerializedObject(display).FindProperty("visualRoot").objectReferenceValue as GameObject;
            if (content != null) ownershipGates.Add(content.transform);
        }
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<Renderer>();
            if (filter.sharedMesh == null || renderer == null || !renderer.enabled) continue;
            bool active = true;
            for (Transform t = filter.transform; t != null && t != root; t = t.parent)
                active &= t.gameObject.activeSelf || ownershipGates.Contains(t);
            if (!active) continue;
            Vector3[] vertices = filter.sharedMesh.vertices;
            Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var materials = renderer.sharedMaterials;
            for (int submesh = 0; submesh < filter.sharedMesh.subMeshCount; submesh++)
            {
            int[] indices = filter.sharedMesh.GetTriangles(submesh);
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3 a = matrix.MultiplyPoint3x4(vertices[indices[i]]), b = matrix.MultiplyPoint3x4(vertices[indices[i + 1]]),
                    c = matrix.MultiplyPoint3x4(vertices[indices[i + 2]]);
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-12f) continue;
                result.Add(new Triangle { a = a, b = b, c = c, normal = normal.normalized,
                    material = submesh < materials.Length && materials[submesh] != null ? materials[submesh].name : string.Empty });
                if (first) { bounds = new Bounds(a, Vector3.zero); first = false; }
                bounds.Encapsulate(a); bounds.Encapsulate(b); bounds.Encapsulate(c);
            }
            }
        }
        return result;
    }

    static bool Occluded(Vector3 from, Vector3 target, List<Triangle> triangles)
    {
        Vector3 direction = target - from; float length = direction.magnitude;
        if (length < .001f) return false; direction /= length;
        foreach (var triangle in triangles)
        {
            Vector3 edge1 = triangle.b - triangle.a, edge2 = triangle.c - triangle.a;
            Vector3 cross = Vector3.Cross(direction, edge2); float determinant = Vector3.Dot(edge1, cross);
            if (Mathf.Abs(determinant) < 1e-8f) continue;
            float inverse = 1f / determinant; Vector3 offset = from - triangle.a;
            float u = Vector3.Dot(offset, cross) * inverse;
            if (u < 0f || u > 1f) continue;
            Vector3 q = Vector3.Cross(offset, edge1); float v = Vector3.Dot(direction, q) * inverse;
            if (v < 0f || u + v > 1f) continue;
            float distance = Vector3.Dot(edge2, q) * inverse;
            if (distance > .001f && distance < length - .012f) return true;
        }
        return false;
    }
}
