using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>One deterministic zoning and visibility policy for current and future room collections.</summary>
public static class HomeRoomLayoutPlanner
{
    private static int deepest;
    public const float ItemGap = .35f;
    public const float EntryRadius = .30f;
    public const float CorridorHalfWidth = .50f;
    public const float CorridorBack = 1.05f;
    public const float FrontLimit = -2.02f;

    public sealed class Item
    {
        public string id, roomId, name;
        public Vector2 size;
        public float height, scale = 1, hungHeight;
        public Vector3 entry, originalPosition;
        public Vector3 requiredFacing;
        public float originalYaw;
        public bool wallEdge;
        public string rejection;
        public readonly List<Vector3> activityViews = new List<Vector3>();
        public IReadOnlyList<Vector3> architecturalViews;
        public IReadOnlyList<Bounds> protectedFeatures, fixedObstacles;
        public bool Flat => height <= .12f && hungHeight <= .01f;
        public bool Hung => hungHeight > .01f;
        public readonly List<Candidate> candidates = new List<Candidate>();
    }
    public struct Candidate
    {
        public Vector3 position, entry;
        public float yaw, score;
        public Bounds body;
        public Vector3[] views;
    }

    // Scale changes preserve each model's proportions; seats/nests stay large enough for all breeds.
    public static float ScaleFor(string name)
    {
        switch (name)
        {
            case "BathroomLaundryHamper": return .82f;
            case "BathroomGroomingCart": return .84f;
            case "BathroomTowelStorage": return .90f;
            case "BathroomToilet": return .70f;
            case "BathroomVanitySink": return .82f;
            case "BathroomTub": return .82f;
            case "BathroomShower": return .88f;
            case "KitchenFruitBasket": return .68f;
            case "KitchenFeedingStation": return .82f;
            case "KitchenCounterStool": return .85f;
            case "KitchenPantryShelf": return .90f;
            case "KitchenDishCart": return .82f;
            case "KitchenRefrigerator": return .88f;
            case "KitchenStoveOven": return .90f;
            case "KitchenIsland": return .90f;
            case "BedroomNightLight": return .80f;
            case "BedroomYarnBasket": return .82f;
            case "BedroomWardrobe": return .90f;
            case "BedroomQueenBed": return .94f;
            case "GardenSapling": return .85f;
            case "GardenBirdBath": return .85f;
            case "GardenGrill": return .82f;
            case "GardenPergola": return .92f;
            case "BalconyHerbShelf": return .90f;
            case "BalconyBirdFeeder": return .82f;
            case "PatioPottedFerns": return .80f;
            case "PatioWaterFountain": return .85f;
            case "PatioFirePit": return .85f;
            case "PatioParasol": return .78f;
            case "PatioPergolaArch": return .92f;
            case "LoftBookStack": return .70f;
            case "LoftArcLamp": return .82f;
            case "LoftTallBookcase": return .86f;
            default: return 1f;
        }
    }

    public static List<HomeRoomLayoutEntry> Plan(IReadOnlyList<Item> input, IReadOnlyList<Vector3> architecturalViews = null,
        IReadOnlyList<Bounds> protectedFeatures = null, IReadOnlyList<Bounds> fixedObstacles = null)
    {
        var items = input.OrderByDescending(item => item.Hung).ThenByDescending(item => !item.Flat)
            .ThenByDescending(item => item.height * item.size.x * item.size.y).ThenBy(item => item.id, StringComparer.Ordinal).ToList();
        foreach (var item in items)
        {
            item.architecturalViews = architecturalViews; item.protectedFeatures = protectedFeatures;
            item.fixedObstacles = fixedObstacles; BuildCandidates(item);
        }
        var selected = new Candidate[items.Count]; int visited = 0; deepest = 0;
        if (!Search(items, selected, 0, ref visited))
            throw new InvalidOperationException("Room cannot fit its complete collection with clear approaches (depth " + deepest + ", visited " + visited + "): " +
                string.Join(", ", items.Select(item => item.name + "=" + item.candidates.Count)));
        var result = new List<HomeRoomLayoutEntry>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i]; var pose = selected[i];
            result.Add(new HomeRoomLayoutEntry { roomId = item.roomId, productId = item.id,
                position = pose.position, yaw = pose.yaw, modelScale = item.scale, footprint = item.size, height = item.height,
                zone = item.Hung ? HomeRoomZone.WallAccent : item.Flat ? HomeRoomZone.FloorAccent :
                    pose.body.min.z >= CorridorBack ? HomeRoomZone.RearFurniture :
                    pose.position.z < -.3f ? HomeRoomZone.FrontActivity : HomeRoomZone.SideFurniture });
        }
        return result;
    }

    public static List<HomeRoomLayoutEntry> PlanAuthored(IReadOnlyList<Item> items,
        IReadOnlyDictionary<string, Vector4> poses, IReadOnlyList<Vector3> architecturalViews,
        IReadOnlyList<Bounds> protectedFeatures, IReadOnlyList<Bounds> fixedObstacles)
    {
        var selected = new List<Candidate>();
        var result = new List<HomeRoomLayoutEntry>();
        foreach (var item in items)
        {
            if (!poses.TryGetValue(item.id, out var pose))
                throw new InvalidOperationException("Review the bathroom bay for the new product: " + item.id);
            item.architecturalViews = architecturalViews; item.protectedFeatures = protectedFeatures;
            item.fixedObstacles = fixedObstacles; item.candidates.Clear();
            Add(item, new Vector3(pose.x, pose.y, pose.z), pose.w, 0);
            if (item.candidates.Count != 1)
                throw new InvalidOperationException("Authored bay fails " + item.rejection + ": " + item.id);
            var candidate = item.candidates[0];
            for (int i = 0; i < selected.Count; i++)
                if (Conflict(item, candidate, items[i], selected[i]))
                    throw new InvalidOperationException("Authored bays conflict: " + item.id + " / " + items[i].id);
            selected.Add(candidate);
            result.Add(new HomeRoomLayoutEntry { roomId = item.roomId, productId = item.id,
                position = candidate.position, yaw = candidate.yaw, modelScale = item.scale,
                footprint = item.size, height = item.height, zone = item.Hung ? HomeRoomZone.WallAccent :
                    item.Flat ? HomeRoomZone.FloorAccent : candidate.body.min.z >= CorridorBack ?
                    HomeRoomZone.RearFurniture : candidate.position.z < -.3f ? HomeRoomZone.FrontActivity : HomeRoomZone.SideFurniture });
        }
        return result;
    }

    private static void BuildCandidates(Item item)
    {
        item.candidates.Clear();
        // The actual approach side includes products whose imported front is +Z or ±X.
        Vector3 front = item.entry;
        if (Mathf.Abs(front.z) > .1f) front = front.z > 0 ? Vector3.forward : Vector3.back;
        else if (Mathf.Abs(front.x) > .1f) front = front.x > 0 ? Vector3.right : Vector3.left;
        else front = Vector3.back;
        float yaw = Mathf.Repeat(Mathf.Round((180f - Mathf.Atan2(front.x, front.z) * Mathf.Rad2Deg) / 90f) * 90f, 360f);
        if (item.Hung)
        {
            // Existing wall decor respects its door/window mounting; future decor uses a rear-wall bay.
            Add(item, item.originalPosition, item.originalYaw, -10);
            for (int x = -3; x <= 3; x++) Add(item, new Vector3(x, item.hungHeight, 2.64f - item.size.y * .5f), yaw, x*x);
            foreach (float sign in new[] { -1f, 1f })
            {
                float sideYaw = Mathf.Repeat(yaw + (sign < 0 ? 270 : 90), 360);
                var sideSize = RotatedSize(item.size, sideYaw);
                for (float z = 1.8f; z >= -.6f; z -= .4f)
                    Add(item, new Vector3(sign * (3.48f - sideSize.x * .5f), item.hungHeight, z), sideYaw, 1);
            }
        }
        else if (item.Flat)
        {
            foreach (float z in new[] { -.2f, -.7f, .35f, .8f })
            foreach (float x in new[] { 0f, -.8f, .8f }) Add(item, new Vector3(x, 0, z), yaw, Mathf.Abs(x) + Mathf.Abs(z));
        }
        else
        {
            // Rear row for large silhouettes; side bays and low foreground activity bays flank an open spine.
            float halfX = RotatedSize(item.size, yaw).x * .5f;
            float halfZ = RotatedSize(item.size, yaw).y * .5f;
            for (float x = -3.48f + halfX; x <= 3.48f - halfX + .001f; x += .24f)
                Add(item, new Vector3(x, 0, 2.60f - halfZ), yaw, 0);
            foreach (float sign in new[] { -1f, 1f })
            {
                float sideYaw = item.wallEdge ? Mathf.Repeat(yaw + (sign < 0 ? 270 : 90), 360) : yaw;
                Vector2 sideSize = RotatedSize(item.size, sideYaw);
                for (float z = 1.80f; z >= FrontLimit + sideSize.y * .5f; z -= .30f)
                    Add(item, new Vector3(sign * (3.48f - sideSize.x * .5f), 0, z), sideYaw, 0);
            }
            if (!item.wallEdge)
                for (float z = FrontLimit + halfZ; z <= 2.60f - halfZ + .001f; z += .32f)
                for (float x = -3.36f + halfX; x <= 3.36f - halfX + .001f; x += .32f)
                    Add(item, new Vector3(x, 0, z), yaw, 0);
        }
        item.candidates.Sort((a, b) => a.score.CompareTo(b.score));
    }

    public static Vector2 RotatedSize(Vector2 size, float yaw)
    {
        float sin = Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)), cos = Mathf.Abs(Mathf.Cos(yaw * Mathf.Deg2Rad));
        return new Vector2(size.x * cos + size.y * sin, size.x * sin + size.y * cos);
    }
    private static void Add(Item item, Vector3 position, float yaw, float preference)
    {
        item.rejection = "bounds or central corridor";
        var size = RotatedSize(item.size, yaw);
        var body = new Bounds(position + Vector3.up * item.height * .5f, new Vector3(size.x, item.height, size.y));
        var entry = position + Quaternion.Euler(0, yaw, 0) * item.entry; entry.y = 0;
        if (item.requiredFacing.sqrMagnitude > .001f &&
            CatActivityFacing.FacingDot(Quaternion.Euler(0, yaw, 0) * item.requiredFacing,
                entry, HomeRoomCameraProfile.Position) < .30f)
        { item.rejection = "camera-facing physical action axis"; return; }
        if (body.min.x < -3.65f || body.max.x > 3.65f || body.min.z < FrontLimit || body.max.z > 2.76f) return;
        if (Mathf.Abs(entry.x) > 3.43f || entry.z < -2.35f || entry.z > 2.40f) return;
        if (!item.Flat && !item.Hung && body.min.z < CorridorBack && body.max.x > -CorridorHalfWidth && body.min.x < CorridorHalfWidth) return;
        if (!item.Hung && item.height >= 1.25f && position.z < .40f) return;
        if (!item.Hung && item.height >= 1.25f && body.min.z < CorridorBack && body.min.x > -3.20f && body.max.x < 3.20f) return;
        item.rejection = "architectural sightline";
        if (item.architecturalViews != null && item.architecturalViews.Any(view => Hides(view, body))) return;
        item.rejection = "door/window volume";
        if (item.protectedFeatures != null && item.protectedFeatures.Any(feature => feature.Intersects(body))) return;
        item.rejection = "fixed obstacle";
        if (item.fixedObstacles != null && item.fixedObstacles.Any(obstacle =>
            obstacle.Intersects(body) || Contains(obstacle, entry, EntryRadius))) return;
        float desiredZ = item.height >= 1.25f || item.size.x >= 1.7f ? 2.15f : item.height >= .8f ? .80f : -.75f;
        float score = preference + Mathf.Abs(position.z - desiredZ) * 3f + Mathf.Abs(Mathf.Abs(position.x) - 2.1f) * .35f;
        var rotation = Quaternion.Euler(0, yaw, 0);
        var views = item.activityViews.Select(view => position + rotation * view + Vector3.up * .18f).ToList();
        views.Add(entry + Vector3.up * .35f);
        // Reserve the visible cat, not merely its pivot: foreground edge activities
        // otherwise leave half the body outside the player's viewport.
        if (views.Any(view => !FitsPlayerView(view)))
        { item.rejection = "cat framing"; return; }
        views.Add(position + Vector3.up * Mathf.Max(.18f, item.height * .55f));
        item.rejection = "fixed obstacle sightline";
        if (item.fixedObstacles != null && views.Any(view => item.fixedObstacles.Any(obstacle => Hides(view, obstacle)))) return;
        item.candidates.Add(new Candidate { position = position, yaw = yaw, entry = entry, body = body, score = score, views = views.ToArray() });
    }

    private static bool Search(List<Item> items, Candidate[] selected, int depth, ref int visited)
    {
        deepest = Mathf.Max(deepest, depth);
        if (++visited > 450000) return false;
        if (depth == items.Count) return true;
        foreach (var candidate in items[depth].candidates)
        {
            bool clear = true;
            for (int j = 0; j < depth; j++)
                if (Conflict(items[depth], candidate, items[j], selected[j])) { clear = false; break; }
            if (!clear) continue;
            selected[depth] = candidate;
            if (Search(items, selected, depth + 1, ref visited)) return true;
        }
        return false;
    }
    public static bool Conflict(Item a, Candidate ap, Item b, Candidate bp)
    {
        if (!a.Flat && !b.Flat && ap.body.max.y + .08f > bp.body.min.y && bp.body.max.y + .08f > ap.body.min.y &&
            Overlap(ap.body, bp.body, ItemGap)) return true;
        if (!b.Flat && !b.Hung && Contains(bp.body, ap.entry, EntryRadius)) return true;
        if (!a.Flat && !a.Hung && Contains(ap.body, bp.entry, EntryRadius)) return true;
        if (Vector2.Distance(new Vector2(ap.entry.x, ap.entry.z), new Vector2(bp.entry.x, bp.entry.z)) < .62f) return true;
        return ap.views.Any(view => Hides(view, bp.body)) || bp.views.Any(view => Hides(view, ap.body));
    }
    private static bool Overlap(Bounds a, Bounds b, float gap) =>
        a.min.x < b.max.x + gap && a.max.x + gap > b.min.x && a.min.z < b.max.z + gap && a.max.z + gap > b.min.z;
    private static bool Contains(Bounds body, Vector3 point, float radius) =>
        point.x > body.min.x - radius && point.x < body.max.x + radius && point.z > body.min.z - radius && point.z < body.max.z + radius;
    private static bool Hides(Vector3 target, Bounds body)
    {
        Vector3 delta = target - HomeRoomCameraProfile.Position;
        return body.IntersectRay(new Ray(HomeRoomCameraProfile.Position, delta.normalized), out float distance) && distance < delta.magnitude - .18f;
    }

    public static bool FitsPlayerView(Vector3 center)
    {
        var inverse = Quaternion.Inverse(Quaternion.Euler(HomeRoomCameraProfile.Angles));
        float tangent = Mathf.Tan(HomeRoomCameraProfile.FieldOfView * .5f * Mathf.Deg2Rad);
        const float aspect = 1920f / 1000f;
        foreach (float x in new[] { -.34f, .34f })
        // Callers use a .35m body center: include the feet at floor height.
        // The old -.18 sample left the lowest .17m outside the framing check.
        foreach (float y in new[] { -.35f, .42f })
        {
            var local = inverse * (center + new Vector3(x, y, 0) - HomeRoomCameraProfile.Position);
            float halfHeight = local.z * tangent;
            if (local.z <= 0 || Mathf.Abs(local.x) > halfHeight * aspect * .94f ||
                local.y > halfHeight * .86f || local.y < -halfHeight * .96f) return false;
        }
        return true;
    }
}
