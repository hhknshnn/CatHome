using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Source-hand seeds choose actual triangle surfaces. This is a pure current-
/// stance resolver: no actor, prop or collider is moved while offering an action.
/// </summary>
public sealed class CatPawMeshContactResolver
{
    public struct Solution
    {
        public CatMeshContactSurface.Hit Surface;
        public CatPawReachPlan Plan;
        public Vector3 Translation;
        public Vector3 SurfacePoint => Surface.Point + Translation;
    }
    struct Candidate
    {
        public CatMeshContactSurface.Hit surface;
        public CatActivityPose pose;
        public bool left;
        public Vector3 target;
        public float cost;
    }
    readonly Transform root;
    readonly CatActivityPose[] poses;
    readonly CatMeshContactSurface.TargetSet surfaces;
    readonly MeshFilter[] filters;
    readonly Matrix4x4[] partMatrices;
    readonly Mesh[] meshes;
    readonly bool[] visibility;
    readonly Dictionary<Vector3, List<Candidate>> candidates = new Dictionary<Vector3, List<Candidate>>();
    CatMovement cachedActor;
    CatBreedVisualTag cachedVisual;
    string cachedBreed;
    Matrix4x4 actorMatrix;

    public CatPawMeshContactResolver(Transform root, params CatActivityPose[] poses)
    {
        this.root = root; this.poses = poses ?? Array.Empty<CatActivityPose>();
        surfaces = new CatMeshContactSurface.TargetSet(root);
        filters = root != null ? root.GetComponentsInChildren<MeshFilter>(true) : Array.Empty<MeshFilter>();
        partMatrices = new Matrix4x4[filters.Length]; meshes = new Mesh[filters.Length]; visibility = new bool[filters.Length];
    }

    public bool TryResolve(CatMovement actor, Vector3 translation, out Solution solution,
        Predicate<Solution> acceptsSequence = null, float maxPitch = CatPawReachResolver.MaximumChestPitch,
        float maxYaw = 0f)
    {
        solution = default;
        if (actor == null || root == null) return false;
        var visual = actor.GetComponentInChildren<CatBreedVisualTag>();
        if (visual == null || !CatPawReachCatalog.IsBodyReady(visual.BreedId)) return false;
        // Pending data must never become a cached empty geometric candidate set.
        RefreshGeometry(actor, visual);
        if (!candidates.TryGetValue(translation, out var options))
        {
            options = BuildCandidates(visual, translation);
            if (candidates.Count >= 8) candidates.Clear();
            candidates[translation] = options;
        }
        foreach (var candidate in options)
        {
            if (!candidate.surface.IsValid || !CatPawReachResolver.TryResolve(actor, candidate.target,
                candidate.left, candidate.pose, out var plan, maxPitch, maxYaw)) continue;
            var selected = new Solution { Surface = candidate.surface, Plan = plan, Translation = translation };
            // Physics approval is deliberately not cached. A caller may require
            // every later translated tap before advertising the first tap.
            if (acceptsSequence != null && !acceptsSequence(selected)) continue;
            solution = selected; return true;
        }
        return false;
    }

    public bool TryClosest(Vector3 point, out CatMeshContactSurface.Hit hit) => surfaces.TryClosest(point, out hit);

    void RefreshGeometry(CatMovement actor, CatBreedVisualTag visual)
    {
        Matrix4x4 matrix = visual.transform.localToWorldMatrix;
        bool changed = cachedActor != actor || cachedVisual != visual || cachedBreed != visual.BreedId || !actorMatrix.Equals(matrix);
        for (int i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            if (filter == null) { if (meshes[i] != null) changed = true; meshes[i] = null; continue; }
            bool visible = filter.gameObject.activeInHierarchy && filter.TryGetComponent<Renderer>(out var renderer) && renderer.enabled;
            Matrix4x4 current = filter.transform.localToWorldMatrix;
            changed |= meshes[i] != filter.sharedMesh || visibility[i] != visible || !partMatrices[i].Equals(current);
            meshes[i] = filter.sharedMesh; visibility[i] = visible; partMatrices[i] = current;
        }
        if (changed) candidates.Clear();
        cachedActor = actor; cachedVisual = visual; cachedBreed = visual.BreedId; actorMatrix = matrix;
    }

    List<Candidate> BuildCandidates(CatBreedVisualTag visual, Vector3 translation)
    {
        var result = new List<Candidate>();
        var catalog = CatPawReachCatalog.Load();
        if (catalog == null) return result;
        foreach (var pose in poses)
        {
            var entry = catalog.Find(visual.BreedId, pose);
            if (entry?.samples == null) continue;
            foreach (var sample in entry.samples)
            {
                if (sample == null || !ContactPhase(sample.phase)) continue;
                for (int side = 0; side < 2; side++)
                {
                    bool left = side == 0;
                    if (pose == CatActivityPose.BatLeft && !left || pose == CatActivityPose.BatRight && left) continue;
                    Vector3 hand = visual.transform.TransformPoint(left ? sample.left.hand : sample.right.hand) +
                        Vector3.up * CatPawReachCatalog.GroundClearance;
                    // A planned rigid translation has exact closest geometry
                    // without changing the live prop or synchronizing physics.
                    if (!surfaces.TryClosest(hand - translation, out var hit)) continue;
                    Vector3 actual = hit.Point + translation;
                    Vector3 outward = hand - actual;
                    Vector3 target = actual + (outward.sqrMagnitude > .00000001f ? outward.normalized * .012f : Vector3.zero);
                    bool duplicate = false;
                    foreach (var previous in result)
                        if (previous.pose == pose && previous.left == left &&
                            (previous.target - target).sqrMagnitude < .000025f) { duplicate = true; break; }
                    if (!duplicate) result.Add(new Candidate { surface = hit, pose = pose, left = left,
                        target = target, cost = (hand - target).sqrMagnitude });
                }
            }
        }
        result.Sort((a, b) => a.cost.CompareTo(b.cost));
        return result;
    }
    static bool ContactPhase(float phase) => Mathf.Abs(phase - .32f) < .001f || Mathf.Abs(phase - .42f) < .001f ||
        Mathf.Abs(phase - .52f) < .001f || Mathf.Abs(phase - .62f) < .001f;
}
