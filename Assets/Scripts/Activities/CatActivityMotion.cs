using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>Floor clearance and short routes for furniture approaches.</summary>
public static class CatActivityMotion
{
    /// <summary>Turn in a neutral pose; the caller retains action and support ownership.</summary>
    public static IEnumerator TurnForStep(CatMovement cat, Quaternion target, float minimumSeconds = .18f)
    {
        var pose = cat.GetComponent<CatActivityAnimation>();
        if (pose != null) pose.SetPose(CatActivityPose.GentleKnead);
        float angle = Quaternion.Angle(cat.transform.rotation, target);
        if (angle < .1f) yield break;
        yield return CatActivityFacing.Turn(cat, target, Mathf.Max(minimumSeconds, angle / 300f));
    }

    /// <summary>Traverse an already validated authored segment without idle walking or sideways sliding.</summary>
    public static IEnumerator WalkAuthoredStep(CatMovement cat, Vector3 target, Quaternion arrival, float minimumSeconds)
    {
        Vector3 from = cat.transform.position, flat = target - from; flat.y = 0f;
        if (flat.sqrMagnitude < .0001f)
        {
            cat.transform.position = target;
            yield return TurnForStep(cat, arrival);
            yield break;
        }
        var travel = Quaternion.LookRotation(flat);
        yield return TurnForStep(cat, travel);
        var pose = cat.GetComponent<CatActivityAnimation>();
        if (pose != null) pose.SetPose(CatActivityPose.Walk);
        float duration = Mathf.Max(minimumSeconds, flat.magnitude / 1.5f), elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cat.transform.SetPositionAndRotation(Vector3.Lerp(from, target, Mathf.Clamp01(elapsed / duration)), travel);
            yield return null;
        }
        cat.transform.position = target;
        yield return TurnForStep(cat, arrival);
    }

    public static IEnumerator Jump(CatMovement cat, Vector3 from, Vector3 to, Quaternion startRotation, Quaternion endRotation, float clearance = .20f, bool centerOnLanding = true)
    {
        Vector3 flat = to - from; flat.y = 0f;
        Quaternion launch = flat.sqrMagnitude > .001f ? Quaternion.LookRotation(flat) : startRotation;
        yield return CatJumpMotion.Play(cat, from, to, launch, endRotation, centerOnLanding && to.y > .12f, clearance);
    }
    private const float Step = .2f;
    private const int Width = 38, Depth = 32;
    private static readonly Collider[] overlaps = new Collider[64];
    private static readonly Vector2Int[] Neighbours =
        { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down,
          new Vector2Int(-1, -1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(1, 1) };

    public static bool IsFloorClear(Vector3 point, float radius = .27f, bool ignoreCatProducts = false)
    {
        point.y = 0f;
        int count = Physics.OverlapCapsuleNonAlloc(point + Vector3.up * (radius + .025f),
            point + Vector3.up * .40f, radius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false;
        for (int i = 0; i < count; i++)
        {
            if (overlaps[i].GetComponentInParent<CatMovement>() != null) continue;
            var product=overlaps[i].GetComponentInParent<HomeProductPlacement>();
            if(ignoreCatProducts && product!=null && CatCollectionPolicy.IsCatItem(product.ProductId)) continue;
            return false;
        }
        return true;
    }

    public static Vector3 GridPoint(int x, int z) => new Vector3(-3.7f + x * Step, 0f, -3.1f + z * Step);

    public static void KeepCatClearAfterPurchase(UnityEngine.SceneManagement.Scene scene)
    {
        if (!Application.isPlaying || CatActivity.Active != null) return;
        Physics.SyncTransforms();
        foreach (var cat in Object.FindObjectsByType<CatMovement>(FindObjectsSortMode.None))
        {
            if (cat.gameObject.scene != scene || cat.IsMovementPhysicallyLocked || IsFloorClear(cat.transform.position, .25f)) continue;
            var floor = ReachableFloor(cat.transform.position);
            Vector3 origin = cat.transform.position; origin.y = 0f;
            Vector3 best = origin;
            float distance = float.PositiveInfinity;
            foreach (var p in floor)
                if ((p - origin).sqrMagnitude < distance)
                { best = p; distance = (p - origin).sqrMagnitude; }
            if (float.IsInfinity(distance)) continue;
            var controller = cat.GetComponent<CharacterController>();
            bool enabled = controller != null && controller.enabled;
            if (controller != null) controller.enabled = false;
            cat.transform.position = best;
            if (controller != null) controller.enabled = enabled;
            Physics.SyncTransforms();
        }
    }

    public static List<Vector3> ReachableFloor(Vector3 origin)
    {
        var result = new List<Vector3>();
        BuildGrid(origin, out bool[,] open, out Vector2Int seed);
        var seen = new bool[Width, Depth];
        var queue = new Queue<Vector2Int>();
        if (!open[seed.x, seed.y]) return result;
        queue.Enqueue(seed); seen[seed.x, seed.y] = true;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(GridPoint(current.x, current.y));
            foreach (var delta in Neighbours)
            {
                var next = current + delta;
                if (!Inside(next) || seen[next.x, next.y] || !open[next.x, next.y]) continue;
                if (!ClearSegment(GridPoint(current.x, current.y), GridPoint(next.x, next.y))) continue;
                seen[next.x, next.y] = true; queue.Enqueue(next);
            }
        }
        return result;
    }

    public static bool TryFloorPath(Vector3 from, Vector3 to, out List<Vector3> path)
        => TryFloorPath(from, to, .27f, out path);

    // Controller movement needs the complete swept footprint: its centre is
    // authored ahead of the root and turns around it. Scripted animation paths
    // retain their established .27 m root capsule through the overload above.
    public static bool TryFloorPath(CatMovement cat, Vector3 from, Vector3 to, out List<Vector3> path)
        => TryFloorPath(from, to, ControllerFloorRadius(cat), out path);

    public static float ControllerFloorRadius(CatMovement cat)
    {
        var controller = cat != null ? cat.GetComponent<CharacterController>() : null;
        float radius = .27f;
        if (controller != null)
        {
            Vector3 scale = cat.transform.lossyScale;
            float body = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float offset = new Vector2(controller.center.x * scale.x, controller.center.z * scale.z).magnitude;
            radius = Mathf.Max(radius, body + offset);
        }
        return radius;
    }

    public static bool IsControllerFloorClear(CatMovement cat, Vector3 point)
    {
        float radius = ControllerFloorRadius(cat);
        if (!IsFloorClear(point,radius)) return false;
        var boundary = cat != null ? HomeRoomBoundary.FindFor(cat.gameObject.scene) : null;
        return boundary == null || (boundary.ClampPosition(point,radius)-point).sqrMagnitude < .000001f;
    }

    static bool TryFloorPath(Vector3 from, Vector3 to, float radius, out List<Vector3> path)
    {
        path = new List<Vector3>();
        if (ClearSegment(from, to, radius)) { path.Add(to); return true; }
        BuildGrid(from, out bool[,] open, out Vector2Int start, radius);
        if (!open[start.x, start.y]) return false;
        var seen = new bool[Width, Depth];
        var previous = new Vector2Int[Width, Depth];
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start); seen[start.x, start.y] = true;
        Vector2Int end = start;
        bool found = false;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            Vector3 p = GridPoint(current.x, current.y);
            if ((p - new Vector3(to.x, 0f, to.z)).sqrMagnitude < .16f && ClearSegment(p, to, radius))
            { end = current; found = true; break; }
            foreach (var delta in Neighbours)
            {
                var next = current + delta;
                if (!Inside(next) || seen[next.x, next.y] || !open[next.x, next.y]) continue;
                if (!ClearSegment(p, GridPoint(next.x, next.y), radius)) continue;
                previous[next.x, next.y] = current;
                seen[next.x, next.y] = true; queue.Enqueue(next);
            }
        }
        if (!found) return false;
        while (end != start) { path.Add(GridPoint(end.x, end.y)); end = previous[end.x, end.y]; }
        path.Add(GridPoint(start.x, start.y)); path.Reverse(); path.Add(to);
        // Remove staircase corners only when the swept capsule still fits.
        Vector3 cursor = from;
        for (int i = 0; i < path.Count; i++)
        {
            int last = i;
            while (last + 1 < path.Count && ClearSegment(cursor, path[last + 1], radius)) last++;
            if (last > i) path.RemoveRange(i, last - i);
            cursor = path[i];
        }
        return true;
    }

    public static bool ClearSegment(Vector3 from, Vector3 to, float radius = .27f)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / .08f));
        for (int i = 0; i <= samples; i++)
            if (!IsFloorClear(Vector3.Lerp(from, to, (float)i / samples), radius)) return false;
        return true;
    }

    private static void BuildGrid(Vector3 origin, out bool[,] open, out Vector2Int seed, float radius = .27f)
    {
        open = new bool[Width, Depth]; seed = Vector2Int.zero;
        float best = float.PositiveInfinity;
        origin.y = 0f;
        for (int x = 0; x < Width; x++) for (int z = 0; z < Depth; z++)
        {
            Vector3 p = GridPoint(x, z);
            open[x, z] = IsFloorClear(p, radius);
            float distance = (p - origin).sqrMagnitude;
            if (open[x, z] && distance < best) { best = distance; seed = new Vector2Int(x, z); }
        }
    }

    private static bool Inside(Vector2Int p) => p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Depth;

    public static Vector3 JumpPosition(Vector3 from, Vector3 to, float t, float clearance = .24f)
    {
        t = Mathf.Clamp01(t);
        Vector3 point = Vector3.Lerp(from, to, t);
        point.y = Mathf.Lerp(from.y, to.y, t) + 4f*t*(1f-t) *
            (Mathf.Abs(to.y - from.y) * .6f + clearance);
        return point;
    }
}
