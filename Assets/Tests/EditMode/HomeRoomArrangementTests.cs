using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HomeRoomArrangementTests
{
    [Test]
    public void PlayerFraming_ReservesTheCatsBodyInsteadOfJustItsPivot()
    {
        Assert.That(HomeRoomLayoutPlanner.FitsPlayerView(new Vector3(0, .35f, -1)), Is.True);
        Assert.That(HomeRoomLayoutPlanner.FitsPlayerView(new Vector3(3.3f, .35f, -1.7f)), Is.False);
        Assert.That(HomeRoomLayoutPlanner.FitsPlayerView(new Vector3(-2.1f, .35f, -2.5f)), Is.False,
            "The record player's old entrance clipped the cat's feet in the real camera.");
    }
    [Test]
    public void ApplyingTheBakedScaleAgain_DoesNotShrinkTheModelOrMoveItsContacts()
    {
        StoreCatalogAssets.TryGet(HomeStoreService.PatioParasolId, out var definition);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/PatioParasol.prefab");
        var copy = UnityEngine.Object.Instantiate(source);
        try
        {
            var transforms = copy.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var scales = transforms.Select(t => t.localScale).ToArray();
            HomeRoomArrangementBuilder.ApplyProductScale(copy, definition);
            HomeRoomArrangementBuilder.ApplyProductScale(copy, definition);
            for (int i = 0; i < transforms.Length; i++)
            { Assert.That(transforms[i].localPosition, Is.EqualTo(positions[i])); Assert.That(transforms[i].localScale, Is.EqualTo(scales[i])); }
        }
        finally { UnityEngine.Object.DestroyImmediate(copy); }
    }

    [Test]
    public void NewRoom_ReservesItsDoorAndWindowInsteadOfUsingTheirWallSpace()
    {
        var window = new Bounds(new Vector3(-1f, 2f, 2.55f), new Vector3(1.6f, 1.2f, .4f));
        var door = new Bounds(new Vector3(1f, 1f, 2.35f), new Vector3(1.5f, 2f, .9f));
        var items = Enumerable.Range(0, 3).Select(i => new HomeRoomLayoutPlanner.Item {
            id = "cabinet-" + i, name = "cabinet", roomId = "future", size = new Vector2(.8f, .5f),
            height = 1.8f, wallEdge = true, entry = Vector3.back * .65f }).ToArray();
        foreach (var row in HomeRoomLayoutPlanner.Plan(items, null, new[] { window, door }))
        {
            var size = HomeRoomLayoutPlanner.RotatedSize(row.footprint, row.yaw);
            var bounds = new Bounds(row.position + Vector3.up * row.height * .5f, new Vector3(size.x, row.height, size.y));
            Assert.That(bounds.Intersects(window) || bounds.Intersects(door), Is.False);
        }
    }
    [Test]
    public void EveryOtherRoom_HasACompleteScaledFixedCollection()
    {
        foreach (var room in HomeRoomService.Rooms.Where(r => r.Id != HomeRoomService.LivingRoomId))
        foreach (var definition in StoreCatalogAssets.PlaceableProducts.Where(d => HomeStoreService.IsProductInRoomCollection(room.Id, d.ProductId)))
        {
            Assert.That(HomeRoomLayoutCatalog.TryGet(definition.ProductId, out var plan), Is.True, definition.ProductId);
            Assert.That(plan.roomId, Is.EqualTo(room.Id));
            Assert.That(plan.height, Is.LessThanOrEqualTo(2.12f));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/" + definition.PrefabName + ".prefab");
            Assert.That(prefab.GetComponent<RoomProductScaleStamp>().AppliedScale, Is.EqualTo(plan.modelScale).Within(.001f), definition.ProductId);
            Assert.That(prefab.GetComponent<HomeProductPlacement>().Footprint, Is.EqualTo(plan.footprint));
            Assert.That(prefab.transform.localScale, Is.EqualTo(Vector3.one), "The cat and product root never inherit a visual resize.");
            Assert.That(prefab.GetComponent<CatActivity>(), Is.Not.Null);
        }
    }

    [Test]
    public void AuthoredDimensionsRemainAvailable_ForIdempotentModelRebuilds()
    {
        StoreCatalogAssets.TryGet(HomeStoreService.PatioParasolId, out var parasol);
        Assert.That(parasol.WithoutRoomLayout().Footprint, Is.EqualTo(new Vector2(2.2f, 2.2f)));
        Assert.That(parasol.Footprint.x, Is.LessThan(1.8f));
        Assert.That(parasol.WithoutRoomLayout().Height, Is.EqualTo(1.85f));
        StoreCatalogAssets.TryGet(HomeStoreService.TvUnitId, out var living);
        Assert.That(living.Footprint, Is.EqualTo(living.WithoutRoomLayout().Footprint), "Approved salon scale is preserved.");
    }

    [Test]
    public void AnUnlistedNewRoom_UsesTheSameZones_RegardlessOfProductOrder()
    {
        var items = new List<HomeRoomLayoutPlanner.Item>();
        for (int i = 0; i < 10; i++)
        {
            bool tall = i < 3, flat = i == 9;
            items.Add(new HomeRoomLayoutPlanner.Item { id = "future-product-" + i, roomId = "unlisted-future-room", name = "Future " + i,
                size = flat ? new Vector2(1.4f, .8f) : tall ? new Vector2(.80f, .50f) : new Vector2(.58f, .50f),
                height = flat ? .08f : tall ? 1.5f : .45f, entry = Vector3.back * .65f, wallEdge = tall });
        }
        var first = HomeRoomLayoutPlanner.Plan(items).ToDictionary(row => row.productId);
        items.Reverse();
        var second = HomeRoomLayoutPlanner.Plan(items);
        Assert.That(second.Count, Is.EqualTo(10));
        foreach (var row in second)
        {
            Assert.That(row.position, Is.EqualTo(first[row.productId].position));
            Assert.That(row.yaw, Is.EqualTo(first[row.productId].yaw));
            if (row.zone == HomeRoomZone.FloorAccent) continue;
            var size = HomeRoomLayoutPlanner.RotatedSize(row.footprint, row.yaw);
            bool rear = row.position.z - size.y * .5f >= HomeRoomLayoutPlanner.CorridorBack;
            bool side = Mathf.Abs(row.position.x) - size.x * .5f >= HomeRoomLayoutPlanner.CorridorHalfWidth;
            Assert.That(rear || side, Is.True, "The walking spine must stay open: " + row.productId);
        }
    }

    [Test]
    public void APhysicallyImpossibleRoom_IsRejectedInsteadOfOverlappingItsFurniture()
    {
        var huge = new HomeRoomLayoutPlanner.Item { id = "oversized", roomId = "new", name = "Oversized", size = new Vector2(9, 9), height = 2, entry = Vector3.back };
        Assert.Throws<InvalidOperationException>(() => HomeRoomLayoutPlanner.Plan(new[] { huge }));
    }
}
