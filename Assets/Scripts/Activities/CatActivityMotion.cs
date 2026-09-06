using System.Collections.Generic;
using UnityEngine;

/// <summary>Floor clearance and short routes for furniture approaches.</summary>
public static class CatActivityMotion
{
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
            if (cat.gameObject.scene != scene || IsFloorClear(cat.transform.position, .25f)) continue;
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
    {
        path = new List<Vector3>();
        if (ClearSegment(from, to)) { path.Add(to); return true; }
        BuildGrid(from, out bool[,] open, out Vector2Int start);
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
            if ((p - new Vector3(to.x, 0f, to.z)).sqrMagnitude < .16f && ClearSegment(p, to))
            { end = current; found = true; break; }
            foreach (var delta in Neighbours)
            {
                var next = current + delta;
                if (!Inside(next) || seen[next.x, next.y] || !open[next.x, next.y]) continue;
                if (!ClearSegment(p, GridPoint(next.x, next.y))) continue;
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
            while (last + 1 < path.Count && ClearSegment(cursor, path[last + 1])) last++;
            if (last > i) path.RemoveRange(i, last - i);
            cursor = path[i];
        }
        return true;
    }

    public static bool ClearSegment(Vector3 from, Vector3 to)
    {
        int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / .08f));
        for (int i = 0; i <= samples; i++)
            if (!IsFloorClear(Vector3.Lerp(from, to, (float)i / samples))) return false;
        return true;
    }

    private static void BuildGrid(Vector3 origin, out bool[,] open, out Vector2Int seed)
    {
        open = new bool[Width, Depth]; seed = Vector2Int.zero;
        float best = float.PositiveInfinity;
        origin.y = 0f;
        for (int x = 0; x < Width; x++) for (int z = 0; z < Depth; z++)
        {
            Vector3 p = GridPoint(x, z);
            open[x, z] = IsFloorClear(p);
            float distance = (p - origin).sqrMagnitude;
            if (open[x, z] && distance < best) { best = distance; seed = new Vector2Int(x, z); }
        }
    }

    private static bool Inside(Vector2Int p) => p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Depth;

    public static Vector3 JumpPosition(Vector3 from, Vector3 to, float t, float clearance = .24f)
    {
        t = Mathf.Clamp01(t);
        Vector3 point = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
        point.y = Mathf.Lerp(from.y, to.y, t) + Mathf.Sin(Mathf.PI * t) *
            (Mathf.Abs(to.y - from.y) * .6f + clearance);
        return point;
    }
}
