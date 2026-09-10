using UnityEngine;

/// <summary>Nearby, visible, capsule-clear floor beside the actual product geometry.</summary>
public sealed class CatActivityApproach
{
    public const float SurfaceReach = .80f;
    Transform root;
    Renderer[] renderers;
    readonly RaycastHit[] hits = new RaycastHit[32];
    static readonly RaycastHit[] sightHits = new RaycastHit[32];

    public static bool HasClearSight(CatActivity activity, CatMovement cat, Vector3 from, Vector3 target)
    {
        Vector3 ray = target - from;
        if (ray.sqrMagnitude < .000001f) return true;
        int count = Physics.RaycastNonAlloc(from, ray.normalized, sightHits, ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform hit = sightHits[i].transform;
            if (hit == null || (activity != null && hit.IsChildOf(activity.transform)) ||
                (cat != null && hit.IsChildOf(cat.transform))) continue;
            return false;
        }
        return true;
    }

    public bool HasGeometry(CatActivity activity)
    {
        Transform visual = activity is LivingFurnitureActivity furniture ? furniture.SelectionVisual : null;
        if (visual == null)
        {
            var placement = activity.GetComponent<HomeProductPlacement>();
            if (placement != null) visual = placement.MovableRoot;
        }
        if (visual == null) return false;
        if (root != visual || renderers == null)
        { root = visual; renderers = root.GetComponentsInChildren<Renderer>(true); }
        return renderers.Length > 0;
    }

    public bool TryResolve(CatActivity activity, CatMovement cat, out Vector3 stand, out float distance)
    {
        stand = default; distance = float.PositiveInfinity;
        if (cat == null || !HasGeometry(activity)) return false;
        Bounds bounds = default; bool found = false;
        Vector3 from=cat.transform.position;from.y=0;
        Vector3 edge=from;float nearest=float.PositiveInfinity;
        foreach (var renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                renderer is ParticleSystemRenderer || renderer is LineRenderer) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
            // Measure the rotated model in its own coordinates. A world-aligned
            // box would offer a remote action across the empty corner of a chair.
            Vector3 probe=new Vector3(from.x,renderer.bounds.center.y,from.z);
            Vector3 local=renderer.transform.InverseTransformPoint(probe);
            Vector3 point=renderer.transform.TransformPoint(renderer.localBounds.ClosestPoint(local));point.y=0;
            float square=(point-from).sqrMagnitude;
            if(square<nearest){nearest=square;edge=point;}
        }
        if (!found) return false;
        Vector3 outward = from - edge;
        distance = outward.magnitude;
        if (distance > Mathf.Min(SurfaceReach, activity.InteractionRadius)) return false;
        if (outward.sqrMagnitude < .0001f)
        {
            // Walkable mats can be used where the cat is already standing.
            if (bounds.max.y > .16f) return false;
            stand = from;
        }
        else
        {
            // A TV unit or a furniture base may extend beyond the visible target.
            // The player's clear nearby floor is the entry; routines then resolve
            // their own contact, without pulling that entry into a solid base.
            stand = from;
        }
        if (!CatActivityMotion.ClearSegment(from, stand)) return false;
        Vector3 sightFrom = from + Vector3.up * .40f;
        if (activity is SitLookActivity observation)
            return observation.TryGetVisibleLookPoint(cat, sightFrom, out _);
        // Watching a wall picture or lampshade uses its visible upper surface;
        // a low chair in front of the lamp does not hide the lampshade itself.
        float sightHeight=activity is SitLookActivity?Mathf.Max(.16f,bounds.max.y-.10f):Mathf.Clamp(bounds.center.y,.16f,.80f);
        Vector3 sightTo = edge + Vector3.up*sightHeight;
        Vector3 ray = sightTo - sightFrom;
        int count = Physics.RaycastNonAlloc(sightFrom, ray.normalized, hits, ray.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int index = 0; index < count; index++)
        {
            var hit = hits[index].transform;
            if (hit == null || hit.IsChildOf(root) || hit.IsChildOf(cat.transform)) continue;
            return false;
        }
        return true;
    }
}
