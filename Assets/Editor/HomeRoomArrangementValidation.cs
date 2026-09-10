using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HomeRoomArrangementValidation
{
    public static List<string> Validate(Scene scene, string roomId)
    {
        var errors = new List<string>();
        if (roomId == HomeRoomService.LivingRoomId) return errors;
        var placements = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<HomeProductPlacement>(true)).ToArray();
        var bodies = new List<Bounds>(); var ids = new List<string>();
        foreach (var definition in StoreCatalogAssets.PlaceableProducts.Where(d => HomeStoreService.IsProductInRoomCollection(roomId, d.ProductId)))
        {
            if (!HomeRoomLayoutCatalog.TryGet(definition.ProductId, out var row))
            { errors.Add(definition.ProductId + ": missing automatic room plan"); continue; }
            var matching = placements.Where(p => p.ProductId == definition.ProductId).ToArray();
            if (matching.Length != 1) { errors.Add(definition.ProductId + ": expected one fixed product"); continue; }
            var placement = matching[0];
            if (row.roomId != roomId || Vector3.Distance(placement.transform.position, row.position) > .01f ||
                Quaternion.Angle(placement.transform.rotation, Quaternion.Euler(0, row.yaw, 0)) > .1f)
                errors.Add(definition.ProductId + ": scene differs from fixed room plan");
            var stamp = placement.GetComponent<RoomProductScaleStamp>();
            if (stamp == null || Mathf.Abs(stamp.AppliedScale - row.modelScale) > .001f || placement.transform.localScale != Vector3.one)
                errors.Add(definition.ProductId + ": visual scale is not baked at a unit root");
            if (row.height > 2.1201f || row.footprint.x <= 0 || row.footprint.y <= 0)
                errors.Add(definition.ProductId + ": invalid silhouette budget");
            var size = HomeRoomLayoutPlanner.RotatedSize(row.footprint, row.yaw);
            var body = new Bounds(row.position + Vector3.up * row.height * .5f, new Vector3(size.x, row.height, size.y));
            var boundary = HomeRoomBoundary.FindFor(scene);
            if (boundary != null && (body.min.x < boundary.MinimumXZ.x || body.max.x > boundary.MaximumXZ.x ||
                body.min.z < boundary.MinimumXZ.y || body.max.z > boundary.MaximumXZ.y))
                errors.Add(definition.ProductId + ": outside the room floor");
            if (row.zone != HomeRoomZone.FloorAccent && row.zone != HomeRoomZone.WallAccent &&
                body.min.z < HomeRoomLayoutPlanner.CorridorBack - .001f && body.max.x > -HomeRoomLayoutPlanner.CorridorHalfWidth + .001f &&
                body.min.x < HomeRoomLayoutPlanner.CorridorHalfWidth - .001f)
                errors.Add(definition.ProductId + ": blocks the central walking passage");
            if (row.zone == HomeRoomZone.FloorAccent) continue;
            for (int i = 0; i < bodies.Count; i++)
            {
                var other = bodies[i];
                if (body.max.y + .08f <= other.min.y || other.max.y + .08f <= body.min.y) continue;
                var expanded = body; expanded.Expand(new Vector3(HomeRoomLayoutPlanner.ItemGap * 2 - .002f, 0, HomeRoomLayoutPlanner.ItemGap * 2 - .002f));
                if (expanded.Intersects(other)) errors.Add(definition.ProductId + ": insufficient clearance from " + ids[i]);
            }
            bodies.Add(body); ids.Add(definition.ProductId);
        }
        foreach (var placement in placements)
            if (HomeStoreService.TryGetProduct(placement.ProductId, out var product) && product.StoreCategory == HomeStoreCategory.Cat)
                errors.Add(roomId + ": CAT collection belongs only in Living Room");
        var activities = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CatActivity>(true))
            .Where(a => HomeStoreService.IsProductInRoomCollection(roomId, a.StoreProductId) || string.IsNullOrEmpty(a.StoreProductId)).ToArray();
        for (int i = 0; i < activities.Length; i++)
        {
            if (activities[i].RoutineEntryPoint == null) continue;
            var entry = activities[i].RoutineEntryPoint.position; entry.y = 0;
            if (!HomeRoomLayoutPlanner.FitsPlayerView(entry + Vector3.up * .35f))
                errors.Add(activities[i].name + ": activity cuts the cat off at the screen edge");
            for (int j = 0; j < i; j++)
            {
                if (activities[j].RoutineEntryPoint == null) continue;
                var other = activities[j].RoutineEntryPoint.position; other.y = 0;
                if (Vector3.Distance(entry, other) < .619f)
                    errors.Add(activities[i].name + ": entrance competes with " + activities[j].name);
            }
        }
        return errors;
    }
}
