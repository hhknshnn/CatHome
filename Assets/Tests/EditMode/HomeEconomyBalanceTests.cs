using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Locks the authored economy curve at the route level. Product-level ordering
/// and the 100 Coin = 1 Diamond rule are covered elsewhere; these checks make
/// sure the complete eight-room journey remains reachable and proportionate.
/// </summary>
public sealed class HomeEconomyBalanceTests
{
    private static readonly string[] RoomIds =
    {
        HomeRoomService.LivingRoomId,
        HomeRoomService.BathroomId,
        HomeRoomService.KitchenId,
        HomeRoomService.BedroomId,
        HomeRoomService.GardenId,
        HomeRoomService.BalconyId,
        HomeRoomService.PatioId,
        HomeRoomService.SecondFloorId
    };

    private static readonly string[] PreviewIds =
    {
        null,
        HomeStoreService.HomeBathroomPreviewId,
        HomeStoreService.HomeKitchenPreviewId,
        HomeStoreService.HomeBedroomPreviewId,
        HomeStoreService.HomeGardenPreviewId,
        HomeStoreService.HomeBalconyPreviewId,
        HomeStoreService.HomePatioPreviewId,
        HomeStoreService.HomeSecondFloorPreviewId
    };

    [Test]
    public void CanonicalRoomRoute_HasTenItemsPerRoomAndAStableTotalBudget()
    {
        long routeCoins = 0L;
        for (int i = 0; i < RoomIds.Length; i++)
        {
            if (!string.IsNullOrEmpty(PreviewIds[i]))
            {
                Assert.That(HomeStoreService.TryGetProduct(
                    PreviewIds[i], out HomeStoreProduct preview), Is.True, PreviewIds[i]);
                routeCoins += preview.CoinPrice;
            }

            IReadOnlyList<string> collection = HomeStoreService.GetRoomCollection(RoomIds[i]);
            Assert.That(collection.Count, Is.EqualTo(10), RoomIds[i]);
            routeCoins += SumPrices(collection);
        }

        Assert.That(routeCoins, Is.EqualTo(145000L),
            "The complete room route budget changed; retune rewards and unlock pacing together.");
    }

    [Test]
    public void EveryRoomGate_IsReachableFromRequiredPriorInvestment()
    {
        long priorInvestmentXp = SumPrices(
            HomeStoreService.GetRoomCollection(HomeRoomService.LivingRoomId));
        long previousPreviewPrice = 0L;

        for (int i = 1; i < RoomIds.Length; i++)
        {
            Assert.That(HomeStoreService.TryGetProduct(
                PreviewIds[i], out HomeStoreProduct preview), Is.True, PreviewIds[i]);
            Assert.That(preview.CoinPrice, Is.GreaterThan(previousPreviewPrice), PreviewIds[i]);
            Assert.That(HomeProgressionService.LevelForXp(priorInvestmentXp),
                Is.GreaterThanOrEqualTo(preview.RequiredLevel),
                preview.Title + " must not demand Home XP unavailable from its required route.");

            priorInvestmentXp += preview.CoinPrice;
            priorInvestmentXp += SumPrices(HomeStoreService.GetRoomCollection(RoomIds[i]));
            previousPreviewPrice = preview.CoinPrice;
        }
    }

    [Test]
    public void RoomCompletionRewards_ReturnThreeToFivePercentOfCollectionCost()
    {
        for (int i = 0; i < RoomIds.Length; i++)
        {
            long collectionCost = SumPrices(HomeStoreService.GetRoomCollection(RoomIds[i]));
            CollectionMilestone milestone = FindRoomMilestone(RoomIds[i]);
            Assert.That(milestone.Id, Is.Not.Null.And.Not.Empty, RoomIds[i]);
            double rebatePercent = milestone.Coins * 100d / collectionCost;
            Assert.That(rebatePercent, Is.InRange(3d, 5d),
                RoomIds[i] + " completion reward is out of the intended rebate band.");
            Assert.That(milestone.BondXp, Is.EqualTo(15L), RoomIds[i]);
        }
    }

    private static long SumPrices(IReadOnlyList<string> productIds)
    {
        long total = 0L;
        for (int i = 0; i < productIds.Count; i++)
        {
            Assert.That(HomeStoreService.TryGetProduct(
                productIds[i], out HomeStoreProduct product), Is.True, productIds[i]);
            total += product.CoinPrice;
        }
        return total;
    }

    private static CollectionMilestone FindRoomMilestone(string roomId)
    {
        for (int i = 0; i < CollectionMilestoneService.Catalog.Count; i++)
        {
            CollectionMilestone milestone = CollectionMilestoneService.Catalog[i];
            if (string.Equals(milestone.RoomId, roomId, StringComparison.Ordinal))
                return milestone;
        }
        return default;
    }
}
