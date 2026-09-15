using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BathroomArrangementProfileTests
{
    [Test]
    public void ReviewedBathroomBays_MatchTheCurrentCollectionAndKeepTheWalkingLaneOpen()
    {
        var products = StoreCatalogAssets.PlaceableProducts.Where(d =>
            HomeStoreService.IsProductInRoomCollection(HomeRoomService.BathroomId, d.ProductId)).ToArray();
        Assert.That(products.Length, Is.EqualTo(10));
        Assert.That(BathroomArrangementProfile.Poses.Keys,
            Is.EquivalentTo(products.Select(d => d.ProductId)));

        foreach (var product in products)
        {
            Assert.That(HomeRoomLayoutCatalog.TryGet(product.ProductId, out var row), Is.True, product.ProductId);
            var pose = BathroomArrangementProfile.Poses[product.ProductId];
            Assert.That(row.roomId, Is.EqualTo(HomeRoomService.BathroomId));
            Assert.That(Vector3.Distance(row.position, new Vector3(pose.x, pose.y, pose.z)),
                Is.LessThan(.0001f), product.ProductId);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(row.yaw, pose.w)), Is.LessThan(.0001f), product.ProductId);
            Assert.That(product.DefaultPosition, Is.EqualTo(row.position), product.ProductId);

            if (row.zone == HomeRoomZone.FloorAccent || row.zone == HomeRoomZone.WallAccent) continue;
            var size = HomeRoomLayoutPlanner.RotatedSize(row.footprint, row.yaw);
            bool behindLane = row.position.z - size.y * .5f >= HomeRoomLayoutPlanner.CorridorBack;
            bool besideLane = Mathf.Abs(row.position.x) - size.x * .5f >= HomeRoomLayoutPlanner.CorridorHalfWidth;
            Assert.That(behindLane || besideLane, Is.True, "Solid furniture must leave the central metre open: " + product.ProductId);
        }
    }

    [Test]
    public void Mirror_RemainsDecorationAtTheReviewedHeightAboveTheGroomingCart()
    {
        Assert.That(StoreCatalogAssets.TryGet(HomeStoreService.BathroomMirrorId, out var mirror), Is.True);
        Assert.That(HomeRoomLayoutCatalog.TryGet(mirror.ProductId, out var mirrorRow), Is.True);
        Assert.That(HomeRoomLayoutCatalog.TryGet(HomeStoreService.BathroomGroomingCartId, out var cartRow), Is.True);
        Assert.That(mirror.WithoutRoomLayout().HungHeight, Is.EqualTo(1.58f), "Rebuilding the source must keep its authored dimensions.");
        Assert.That(mirrorRow.position.y, Is.EqualTo(1.25f).Within(.0001f));
        Assert.That(mirror.HungHeight, Is.EqualTo(mirrorRow.position.y), "Scene authoring must not restore the old, higher mounting height.");
        Assert.That(mirrorRow.position.x, Is.EqualTo(cartRow.position.x));
        Assert.That(mirrorRow.position.y - (cartRow.position.y + cartRow.height), Is.GreaterThanOrEqualTo(.35f));

        var mirrorPrefab = Prefab(mirror);
        Assert.That(mirrorPrefab.GetComponentInChildren<CatActivity>(true), Is.Null);
        Assert.That(mirrorPrefab.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);

        Assert.That(StoreCatalogAssets.TryGet(HomeStoreService.BathroomGroomingCartId, out var cart), Is.True);
        Vector3 cartEntry = GroundEntry(Prefab(cart), cartRow);
        Assert.That(HomeRoomLayoutPlanner.FitsPlayerView(cartEntry + Vector3.up * .35f), Is.True,
            "The remaining grooming action keeps its visible entrance.");

        var otherRows = Catalog().Entries.Where(row => row.roomId != HomeRoomService.BathroomId).ToArray();
        Assert.That(otherRows.Length, Is.EqualTo(60));
        foreach (var row in otherRows)
        {
            Assert.That(StoreCatalogAssets.TryGet(row.productId, out var other), Is.True);
            Assert.That(other.HungHeight, Is.EqualTo(other.WithoutRoomLayout().HungHeight).Within(.0001f),
                "A bathroom mounting adjustment must not alter another room's mount: " + row.productId);
        }
    }

    [Test]
    public void AuthoredBays_StillRejectBlockedAndOverlappingPlacementsInsteadOfFallingBack()
    {
        var mat = Mat();
        var valid = BathroomArrangementProfile.Plan(new[] { mat }, null, null, null);
        Assert.That(valid.Single().position, Is.EqualTo(new Vector3(0, 0, .25f)));

        var obstacle = new Bounds(new Vector3(0, .15f, .25f), new Vector3(.6f, .3f, .5f));
        Assert.Throws<InvalidOperationException>(() => BathroomArrangementProfile.Plan(
            new[] { mat }, null, new[] { obstacle }, null));
        Assert.Throws<InvalidOperationException>(() => BathroomArrangementProfile.Plan(
            new[] { mat }, null, null, new[] { obstacle }));

        var unknown = Mat(); unknown.id = "bathroom.unreviewed-new-product";
        Assert.Throws<InvalidOperationException>(() => BathroomArrangementProfile.Plan(
            new[] { unknown }, null, null, null));

        var overlapping = new[] { Cabinet("a"), Cabinet("b") };
        foreach (var item in overlapping) item.height = .6f;
        var poses = overlapping.ToDictionary(item => item.id, _ => new Vector4(1.8f, 0, .5f, 0));
        var conflict = Assert.Throws<InvalidOperationException>(() => HomeRoomLayoutPlanner.PlanAuthored(
            overlapping, poses, null, null, null));
        Assert.That(conflict.Message, Does.Contain("conflict"));
    }

    [Test]
    public void AuthoredPlanning_IsRepeatableAndLeavesOtherRoomPlansAndTheCatalogUntouched()
    {
        var catalog = Catalog();
        var otherRows = catalog.Entries.Where(row => row.roomId != HomeRoomService.BathroomId).ToArray();
        Assert.That(otherRows.Length, Is.EqualTo(60));
        var savedRows = otherRows.ToDictionary(row => row.productId, JsonUtility.ToJson);
        var futureRoom = new[] { Cabinet("future-a"), Cabinet("future-b"), Cabinet("future-c") };
        var before = HomeRoomLayoutPlanner.Plan(futureRoom).ToDictionary(row => row.productId, JsonUtility.ToJson);

        var mat = Mat();
        var first = BathroomArrangementProfile.Plan(new[] { mat }, null, null, null).Single();
        var second = BathroomArrangementProfile.Plan(new[] { mat }, null, null, null).Single();
        Assert.That(JsonUtility.ToJson(second), Is.EqualTo(JsonUtility.ToJson(first)));
        Assert.That(mat.candidates.Count, Is.EqualTo(1), "Repeated authoring must replace its previous candidate.");
        foreach (var row in HomeRoomLayoutPlanner.Plan(futureRoom.Reverse().ToArray()))
            Assert.That(JsonUtility.ToJson(row), Is.EqualTo(before[row.productId]), row.productId);
        foreach (var row in catalog.Entries.Where(row => row.roomId != HomeRoomService.BathroomId))
            Assert.That(JsonUtility.ToJson(row), Is.EqualTo(savedRows[row.productId]), row.productId);
    }

    private static HomeRoomLayoutCatalog Catalog() => AssetDatabase.LoadAssetAtPath<HomeRoomLayoutCatalog>(
        "Assets/Resources/Home/RoomLayoutCatalog.asset");

    private static GameObject Prefab(StoreCatalogAsset product) => AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Art/StoreProducts/Prefabs/" + product.PrefabName + ".prefab");

    private static Vector3 GroundEntry(GameObject prefab, HomeRoomLayoutEntry row)
    {
        var local = prefab.transform.InverseTransformPoint(prefab.GetComponent<CatActivity>().RoutineEntryPoint.position);
        var world = row.position + Quaternion.Euler(0, row.yaw, 0) * local;
        world.y = 0;
        return world;
    }

    private static HomeRoomLayoutPlanner.Item Mat() => new HomeRoomLayoutPlanner.Item {
        id = HomeStoreService.BathroomBathMatId, name = "Bathroom mat", roomId = HomeRoomService.BathroomId,
        size = new Vector2(.5f, .4f), height = .08f, entry = Vector3.back * .65f
    };

    private static HomeRoomLayoutPlanner.Item Cabinet(string id) => new HomeRoomLayoutPlanner.Item {
        id = id, name = "Future cabinet", roomId = "unlisted-future-room", size = new Vector2(.8f, .5f),
        height = 1.8f, wallEdge = true, entry = Vector3.back * .65f
    };
}
