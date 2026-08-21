using CatHome.Economy;
using NUnit.Framework;
using UnityEngine;

public sealed class HomeStoreServiceTests
{
    [SetUp]
    public void SetUp()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    [TearDown]
    public void TearDown()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    [Test]
    public void Catalog_UsesEarlyRewardPricesAndThreeStoreCategories()
    {
        Assert.That(HomeStoreService.BallBasketPrice, Is.EqualTo(300));
        Assert.That(HomeStoreService.ScratchPostPrice, Is.EqualTo(400));
        Assert.That(HomeStoreService.Products.Count, Is.EqualTo(114));

        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.BallBasketId,
                out HomeStoreProduct catProduct),
            Is.True);
        Assert.That(catProduct.StoreCategory, Is.EqualTo(HomeStoreCategory.Cat));
        Assert.That(catProduct.RequiredLevel, Is.EqualTo(1));
        Assert.That(catProduct.IsAvailable, Is.True);

        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.ModernTelevisionId,
                out HomeStoreProduct roomProduct),
            Is.True);
        Assert.That(roomProduct.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
        Assert.That(roomProduct.IsAvailable, Is.True);
        Assert.That(roomProduct.SupportsCoins, Is.True);
        Assert.That(roomProduct.SupportsDiamonds, Is.True);
        Assert.That(roomProduct.DiamondPrice * HomeStoreService.CoinsPerDiamond,
            Is.EqualTo(roomProduct.CoinPrice));

        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeRoomsPreviewId,
                out HomeStoreProduct homeProduct),
            Is.True);
        Assert.That(homeProduct.StoreCategory, Is.EqualTo(HomeStoreCategory.Home));
    }

    [Test]
    public void ComingSoonCollection_CannotSpendEitherCurrency()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 5000, EconomySource.Debug);
        EconomyService.AddCurrency(CurrencyType.Diamond, 500, EconomySource.Debug);

        HomeStorePurchaseResult coinAttempt = HomeStoreService.TryPurchase(
            HomeStoreService.HomeRoomsPreviewId,
            CurrencyType.Coin);
        HomeStorePurchaseResult diamondAttempt = HomeStoreService.TryPurchase(
            HomeStoreService.HomeRoomsPreviewId,
            CurrencyType.Diamond);

        Assert.That(coinAttempt.Status, Is.EqualTo(HomeStorePurchaseStatus.ComingSoon));
        Assert.That(diamondAttempt.Status, Is.EqualTo(HomeStorePurchaseStatus.ComingSoon));
        Assert.That(EconomyService.Coins, Is.EqualTo(5000));
        Assert.That(EconomyService.Diamonds, Is.EqualTo(500));
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.HomeRoomsPreviewId), Is.False);
    }

    [Test]
    public void WallEdgePlacement_SnapsFurnitureNearTheClosestWall()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            ownedProductIds = new[] { HomeStoreService.TvUnitId }
        });

        GameObject product = new GameObject("WallEdgePlacementProduct");
        try
        {
            var placement = product.AddComponent<HomeProductPlacement>();
            placement.EditorConfigure(
                HomeStoreService.TvUnitId,
                product.transform,
                System.Array.Empty<Transform>(),
                new Vector2(1.72f, 0.62f),
                HomeProductPlacementKind.WallEdge);

            Assert.That(placement.BeginPreview(), Is.True);
            Assert.That(placement.PreviewWorldPosition(new Vector3(3f, 0f, 0f)), Is.True);
            Assert.That(placement.PreviewPosition.x,
                Is.EqualTo(HomeProductPlacement.DefaultRoomRight - 0.44f).Within(0.001f));
            Assert.That(placement.PreviewRotationY, Is.EqualTo(270f).Within(0.001f));
            placement.CancelPreview();
        }
        finally
        {
            Object.DestroyImmediate(product);
        }
    }

    [Test]
    public void Purchase_DeductsExactPriceAndPersistsOwnership()
    {
        long startingCoins = HomeStoreService.BallBasketPrice + 30;
        EconomyService.AddCurrency(CurrencyType.Coin, startingCoins, EconomySource.Debug);

        HomeStorePurchaseResult result =
            HomeStoreService.TryPurchase(HomeStoreService.BallBasketId);

        Assert.That(result.Status, Is.EqualTo(HomeStorePurchaseStatus.Purchased));
        Assert.That(EconomyService.Coins, Is.EqualTo(30));
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.BallBasketId), Is.True);

        HomeStoreSaveState saved = HomeStoreService.CaptureState();
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.BallBasketId), Is.False);
        HomeStoreService.ApplySavedState(saved);
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.BallBasketId), Is.True);
    }

    [Test]
    public void Purchase_RejectsInsufficientCoinsWithoutChangingState()
    {
        long startingCoins = HomeStoreService.ScratchPostPrice - 1;
        EconomyService.AddCurrency(CurrencyType.Coin, startingCoins, EconomySource.Debug);

        HomeStorePurchaseResult result =
            HomeStoreService.TryPurchase(HomeStoreService.ScratchPostId);

        Assert.That(result.Status, Is.EqualTo(HomeStorePurchaseStatus.InsufficientCoins));
        Assert.That(result.MissingCoins, Is.EqualTo(1));
        Assert.That(EconomyService.Coins, Is.EqualTo(startingCoins));
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.ScratchPostId), Is.False);
    }

    [Test]
    public void Purchase_CannotChargeAnOwnedProductTwice()
    {
        EconomyService.AddCurrency(
            CurrencyType.Coin,
            HomeStoreService.BallBasketPrice + 50,
            EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BallBasketId).Succeeded, Is.True);
        long balanceAfterFirstPurchase = EconomyService.Coins;

        HomeStorePurchaseResult repeat =
            HomeStoreService.TryPurchase(HomeStoreService.BallBasketId);

        Assert.That(repeat.Status, Is.EqualTo(HomeStorePurchaseStatus.AlreadyOwned));
        Assert.That(EconomyService.Coins, Is.EqualTo(balanceAfterFirstPurchase));
    }

    [Test]
    public void Purchase_HomeLevel_GatesItemsUntilRequirementMet()
    {
        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.PlayTunnelId, out HomeStoreProduct playTunnel),
            Is.True);
        Assert.That(playTunnel.RequiredLevel, Is.EqualTo(2));

        EconomyService.AddCurrency(
            CurrencyType.Coin,
            playTunnel.CoinPrice,
            EconomySource.Debug);
        long coinsBeforeAttempt = EconomyService.Coins;

        HomeStorePurchaseResult blocked =
            HomeStoreService.TryPurchase(HomeStoreService.PlayTunnelId);
        Assert.That(blocked.Status, Is.EqualTo(HomeStorePurchaseStatus.LevelLocked));
        Assert.That(HomeStoreService.IsOwned(playTunnel.Id), Is.False);
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(0L));
        Assert.That(EconomyService.Coins, Is.EqualTo(coinsBeforeAttempt));

        HomeProgressionService.GrantHomeXp(
            HomeProgressionService.CumulativeXpForLevel(playTunnel.RequiredLevel) -
            HomeProgressionService.HomeXp);
        long coinsAfterUnlock = EconomyService.Coins;
        // Home XP accumulates: the purchase adds the product's coin value on top of
        // the XP already earned to reach the required level.
        long xpAfterUnlock = HomeProgressionService.HomeXp;

        HomeStorePurchaseResult unlocked =
            HomeStoreService.TryPurchase(HomeStoreService.PlayTunnelId);
        Assert.That(unlocked.Status, Is.EqualTo(HomeStorePurchaseStatus.Purchased));
        Assert.That(HomeStoreService.IsOwned(playTunnel.Id), Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(coinsAfterUnlock - playTunnel.CoinPrice));
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(xpAfterUnlock + playTunnel.CoinPrice));

        HomeStorePurchaseResult repeated =
            HomeStoreService.TryPurchase(playTunnel.Id);
        Assert.That(repeated.Status, Is.EqualTo(HomeStorePurchaseStatus.AlreadyOwned));
        Assert.That(EconomyService.Coins, Is.EqualTo(coinsAfterUnlock - playTunnel.CoinPrice));
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(xpAfterUnlock + playTunnel.CoinPrice));
    }

    [Test]
    public void LivingRoomLevelOne_IsTenProductsOrderedFromLowToHighPrice()
    {
        Assert.That(HomeStoreService.LivingRoomCollection.Count, Is.EqualTo(10));
        Assert.That(HomeStoreService.LivingRoomCollection,
            Does.Not.Contain(HomeStoreService.CarpetId));
        Assert.That(HomeStoreService.LivingRoomCollection,
            Does.Not.Contain(HomeStoreService.CoffeeTableId));
        Assert.That(HomeStoreService.LivingRoomCollection,
            Does.Not.Contain(HomeStoreService.SofaId));
        Assert.That(HomeStoreService.LivingRoomCollection,
            Does.Contain(HomeStoreService.GameConsoleId));
        Assert.That(HomeStoreService.LivingRoomCollection,
            Does.Contain(HomeStoreService.StereoId));
        long previousPrice = -1L;
        for (int i = 0; i < HomeStoreService.LivingRoomCollection.Count; i++)
        {
            Assert.That(
                HomeStoreService.TryGetProduct(
                    HomeStoreService.LivingRoomCollection[i],
                    out HomeStoreProduct product),
                Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.CoinPrice % HomeStoreService.CoinsPerDiamond, Is.Zero);
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }
    }

    [Test]
    public void BathroomLevelOne_IsTenUniquePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.BathroomCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < HomeStoreService.BathroomCollection.Count; i++)
        {
            string id = HomeStoreService.BathroomCollection[i];
            Assert.That(ids.Add(id), Is.True, id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.TryGetProduct(id, out HomeStoreProduct product), Is.True);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }
    }

    [Test]
    public void KitchenLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.KitchenCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < HomeStoreService.KitchenCollection.Count; i++)
        {
            string id = HomeStoreService.KitchenCollection[i];
            Assert.That(ids.Add(id), Is.True, id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBathroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBedroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsGardenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.TryGetProduct(id, out HomeStoreProduct product), Is.True);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }
    }

    [Test]
    public void BedroomLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.BedroomCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < HomeStoreService.BedroomCollection.Count; i++)
        {
            string id = HomeStoreService.BedroomCollection[i];
            Assert.That(ids.Add(id), Is.True, id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBathroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsKitchenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsGardenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.TryGetProduct(id, out HomeStoreProduct product), Is.True);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }
    }

    [Test]
    public void GardenLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.GardenCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < HomeStoreService.GardenCollection.Count; i++)
        {
            string id = HomeStoreService.GardenCollection[i];
            Assert.That(ids.Add(id), Is.True, id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBathroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsKitchenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBedroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.TryGetProduct(id, out HomeStoreProduct product), Is.True);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }

        Assert.That(
            HomeStoreService.GetRequiredProductId(HomeStoreService.HomeGardenPreviewId),
            Is.EqualTo(HomeStoreService.HomeBedroomPreviewId));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeGardenPreviewId,
                out HomeStoreProduct gardenPreview),
            Is.True);
        Assert.That(gardenPreview.IsAvailable, Is.True);
        Assert.That(gardenPreview.RequiredLevel, Is.EqualTo(5));
        Assert.That(gardenPreview.CoinPrice, Is.EqualTo(HomeStoreService.GardenCoinPrice));
    }

    [Test]
    public void BalconyLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.BalconyCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < HomeStoreService.BalconyCollection.Count; i++)
        {
            string id = HomeStoreService.BalconyCollection[i];
            Assert.That(ids.Add(id), Is.True, id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBathroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsKitchenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBedroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsGardenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.TryGetProduct(id, out HomeStoreProduct product), Is.True);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }

        Assert.That(
            HomeStoreService.GetRequiredProductId(HomeStoreService.HomeBalconyPreviewId),
            Is.EqualTo(HomeStoreService.HomeGardenPreviewId));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeBalconyPreviewId,
                out HomeStoreProduct balconyPreview),
            Is.True);
        Assert.That(balconyPreview.IsAvailable, Is.True);
        Assert.That(balconyPreview.RequiredLevel, Is.EqualTo(9));
        Assert.That(balconyPreview.CoinPrice, Is.EqualTo(HomeStoreService.BalconyCoinPrice));
    }

    [Test]
    public void PatioLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.PatioCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < HomeStoreService.PatioCollection.Count; i++)
        {
            string id = HomeStoreService.PatioCollection[i];
            Assert.That(ids.Add(id), Is.True, id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBathroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsKitchenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBedroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsGardenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBalconyCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.TryGetProduct(id, out HomeStoreProduct product), Is.True);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }

        Assert.That(
            HomeStoreService.GetRequiredProductId(HomeStoreService.HomePatioPreviewId),
            Is.EqualTo(HomeStoreService.HomeBalconyPreviewId));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomePatioPreviewId,
                out HomeStoreProduct patioPreview),
            Is.True);
        Assert.That(patioPreview.IsAvailable, Is.True);
        Assert.That(patioPreview.RequiredLevel, Is.EqualTo(10));
        Assert.That(patioPreview.CoinPrice, Is.EqualTo(HomeStoreService.PatioCoinPrice));
    }

    [Test]
    public void SecondFloorLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.SecondFloorCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < HomeStoreService.SecondFloorCollection.Count; i++)
        {
            string id = HomeStoreService.SecondFloorCollection[i];
            Assert.That(ids.Add(id), Is.True, id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBathroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsKitchenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBedroomCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsGardenCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsBalconyCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.IsPatioCollectionProduct(id), Is.False, id);
            Assert.That(HomeStoreService.TryGetProduct(id, out HomeStoreProduct product), Is.True);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Room));
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice, Is.GreaterThan(previousPrice));
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            previousPrice = product.CoinPrice;
        }

        Assert.That(
            HomeStoreService.GetRequiredProductId(HomeStoreService.HomeSecondFloorPreviewId),
            Is.EqualTo(HomeStoreService.HomePatioPreviewId));
        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.HomeSecondFloorPreviewId,
                out HomeStoreProduct secondFloorPreview),
            Is.True);
        Assert.That(secondFloorPreview.IsAvailable, Is.True);
        Assert.That(secondFloorPreview.RequiredLevel, Is.EqualTo(12));
        Assert.That(secondFloorPreview.CoinPrice, Is.EqualTo(HomeStoreService.SecondFloorCoinPrice));
    }

    [Test]
    public void FreeTestingAcquisition_SpendsNothingAndAddsRequiredProducts()
    {
        Assert.That(HomeStoreService.FreePurchaseTestingEnabled, Is.True);

        HomeStorePurchaseResult room = HomeStoreService.TryAcquireForTesting(
            HomeStoreService.HomeKitchenPreviewId);
        HomeStorePurchaseResult television = HomeStoreService.TryAcquireForTesting(
            HomeStoreService.ModernTelevisionId);

        Assert.That(room.Status, Is.EqualTo(HomeStorePurchaseStatus.Purchased));
        Assert.That(television.Status, Is.EqualTo(HomeStorePurchaseStatus.Purchased));
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.HomeBathroomPreviewId), Is.True);
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.HomeKitchenPreviewId), Is.True);
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.TvUnitId), Is.True);
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.ModernTelevisionId), Is.True);
        Assert.That(EconomyService.Coins, Is.Zero);
        Assert.That(EconomyService.Diamonds, Is.Zero);
    }

    [Test]
    public void CatalogExpansion_AddsPlaceableCatProductsWithArt()
    {
        string[] expansionIds =
        {
            HomeStoreService.BellCollarId,
            HomeStoreService.KibbleBagId,
            HomeStoreService.NapPillowId,
            HomeStoreService.CatnipPlantId,
            HomeStoreService.CardboardHideoutId
        };

        for (int i = 0; i < expansionIds.Length; i++)
        {
            Assert.That(
                HomeStoreService.TryGetProduct(expansionIds[i], out HomeStoreProduct product),
                Is.True,
                expansionIds[i]);
            Assert.That(product.StoreCategory, Is.EqualTo(HomeStoreCategory.Cat));
            Assert.That(product.IsAvailable, Is.True);
            Assert.That(product.IsPlaceable, Is.True);
            Assert.That(product.CoinPrice % HomeStoreService.CoinsPerDiamond, Is.Zero);
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice));
            Assert.That(StoreCatalogAssets.TryGet(product.Id, out _), Is.True, product.Id);
            Assert.That(HomeStoreService.IsLivingRoomCollectionProduct(product.Id), Is.False);
        }
    }

    [Test]
    public void EveryPlaceableCatProduct_HasCatalogArt()
    {
        int placeableCats = 0;
        for (int i = 0; i < HomeStoreService.Products.Count; i++)
        {
            HomeStoreProduct product = HomeStoreService.Products[i];
            if (product.StoreCategory != HomeStoreCategory.Cat || !product.IsPlaceable)
                continue;
            placeableCats++;
            Assert.That(
                StoreCatalogAssets.GetIconPath(product.Id),
                Is.Not.Null.And.Not.Empty,
                product.Id);
            if (product.Id == HomeStoreService.BallBasketId ||
                product.Id == HomeStoreService.ScratchPostId)
                continue;
            Assert.That(
                StoreCatalogAssets.TryGet(product.Id, out StoreCatalogAsset asset),
                Is.True,
                product.Id);
            Assert.That(asset.Height, Is.LessThanOrEqualTo(HomeRoomShellMetrics.WallHeight),
                product.Id);
        }

        Assert.That(placeableCats, Is.GreaterThanOrEqualTo(17));
    }

    [Test]
    public void EveryPurchasableProduct_HasEquivalentCoinAndDiamondPrices()
    {
        for (int i = 0; i < HomeStoreService.Products.Count; i++)
        {
            HomeStoreProduct product = HomeStoreService.Products[i];
            if (!product.IsAvailable || product.CoinPrice <= 0L)
                continue;

            Assert.That(product.CoinPrice % HomeStoreService.CoinsPerDiamond, Is.Zero,
                product.Id);
            Assert.That(product.DiamondPrice * HomeStoreService.CoinsPerDiamond,
                Is.EqualTo(product.CoinPrice), product.Id);
        }
    }

    [Test]
    public void Purchase_CanUseEquivalentDiamondPrice()
    {
        Assert.That(HomeStoreService.TryGetProduct(
            HomeStoreService.BallBasketId, out HomeStoreProduct product), Is.True);
        EconomyService.AddCurrency(
            CurrencyType.Diamond,
            product.DiamondPrice,
            EconomySource.Debug);

        HomeStorePurchaseResult result = HomeStoreService.TryPurchase(
            product.Id,
            CurrencyType.Diamond);

        Assert.That(result.Status, Is.EqualTo(HomeStorePurchaseStatus.Purchased));
        Assert.That(EconomyService.Diamonds, Is.Zero);
        Assert.That(HomeStoreService.IsOwned(product.Id), Is.True);
    }

    [Test]
    public void BookSet_RequiresBookshelfBeforePurchase()
    {
        EconomyService.AddCurrency(
            CurrencyType.Coin,
            HomeStoreService.BookSetPrice + 2000L,
            EconomySource.Debug);

        HomeStorePurchaseResult blocked =
            HomeStoreService.TryPurchase(HomeStoreService.BookSetId);
        Assert.That(blocked.Status, Is.EqualTo(HomeStorePurchaseStatus.RequiredProductMissing));
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.BookSetId), Is.False);

        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BookshelfId).Succeeded, Is.True);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BookSetId).Succeeded, Is.True);
    }

    [Test]
    public void Television_RequiresTvUnitBeforePurchase()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 10000L, EconomySource.Debug);

        HomeStorePurchaseResult blocked =
            HomeStoreService.TryPurchase(HomeStoreService.ModernTelevisionId);
        Assert.That(blocked.Status, Is.EqualTo(HomeStorePurchaseStatus.RequiredProductMissing));
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.ModernTelevisionId), Is.False);

        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.TvUnitId).Succeeded, Is.True);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.ModernTelevisionId).Succeeded, Is.True);
    }

    [Test]
    public void Placement_RequiresOwnershipAndRoundTripsThroughSave()
    {
        Assert.That(
            HomeStoreService.TrySetPlacement(
                HomeStoreService.BallBasketId,
                new UnityEngine.Vector3(-2.4f, 0f, 0.3f),
                45f),
            Is.False);

        EconomyService.AddCurrency(CurrencyType.Coin, HomeStoreService.BallBasketPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BallBasketId).Succeeded, Is.True);
        UnityEngine.Vector3 expected = new UnityEngine.Vector3(-2.4f, 0f, 0.3f);
        Assert.That(HomeStoreService.TrySetPlacement(
            HomeStoreService.BallBasketId, expected, 45f), Is.True);
        Assert.That(HomeStoreService.TryGetWorldPlacement(
            HomeStoreService.BallBasketId, out UnityEngine.Vector3 placed, out float yaw), Is.True);
        Assert.That(placed, Is.EqualTo(expected));
        Assert.That(yaw, Is.EqualTo(45f));

        HomeStoreSaveState saved = HomeStoreService.CaptureState();
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        Assert.That(HomeStoreService.TryGetWorldPlacement(
            HomeStoreService.BallBasketId, out _, out _), Is.False);
        HomeStoreService.ApplySavedState(saved);
        Assert.That(HomeStoreService.TryGetWorldPlacement(
            HomeStoreService.BallBasketId, out placed, out yaw), Is.True);
        Assert.That(placed, Is.EqualTo(expected));
        Assert.That(yaw, Is.EqualTo(45f));
    }

    [Test]
    public void Load_IgnoresUnknownAndDuplicateProductIds()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            ownedProductIds = new[]
            {
                HomeStoreService.BallBasketId,
                "removed.product",
                HomeStoreService.BallBasketId
            }
        });

        HomeStoreSaveState clean = HomeStoreService.CaptureState();
        Assert.That(clean.ownedProductIds, Is.EqualTo(new[] { HomeStoreService.BallBasketId }));
    }

    [Test]
    public void FreePlacement_RejectsFurnitureOverlapAndAcceptsClearFloor()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, HomeStoreService.BallBasketPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BallBasketId).Succeeded, Is.True);

        GameObject product = new GameObject("PlacementTestProduct");
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            product.transform.position = new Vector3(-2.4f, 0f, -1.8f);
            var placement = product.AddComponent<HomeProductPlacement>();
            placement.EditorConfigure(
                HomeStoreService.BallBasketId,
                product.transform,
                System.Array.Empty<Transform>(),
                new Vector2(0.6f, 0.6f));

            obstacle.name = "ExistingFurniture";
            obstacle.transform.SetPositionAndRotation(new Vector3(0f, 0.35f, 0f), Quaternion.identity);
            obstacle.transform.localScale = new Vector3(0.9f, 0.7f, 0.9f);
            Physics.SyncTransforms();

            Assert.That(placement.BeginPreview(), Is.True);
            Assert.That(placement.PreviewWorldPosition(Vector3.zero), Is.False);
            Assert.That(placement.IsPreviewValid, Is.False);
            Assert.That(placement.RevertInvalidPreview(), Is.True);
            Assert.That(placement.PreviewPosition, Is.EqualTo(new Vector3(-2.4f, 0f, -1.8f)));
            Assert.That(placement.IsPreviewValid, Is.True);

            Assert.That(
                placement.PreviewWorldPosition(new Vector3(2.6f, 0f, -1.8f)),
                Is.True);
            Assert.That(placement.IsPreviewValid, Is.True);
            placement.CancelPreview();
        }
        finally
        {
            Object.DestroyImmediate(obstacle);
            Object.DestroyImmediate(product);
        }
    }

    [Test]
    public void FreePlacement_ClampsCloseToWallInsteadOfLeavingLargeDeadZone()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, HomeStoreService.BallBasketPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BallBasketId).Succeeded, Is.True);

        GameObject product = new GameObject("NearWallPlacementTestProduct");
        GameObject baseboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            product.transform.position = new Vector3(0f, 0f, -1.8f);
            baseboard.name = "Baseboard_Right";
            baseboard.transform.position = new Vector3(3.9f, 0.09f, 0f);
            baseboard.transform.localScale = new Vector3(0.28f, 0.18f, 6f);
            var placement = product.AddComponent<HomeProductPlacement>();
            placement.EditorConfigure(
                HomeStoreService.BallBasketId,
                product.transform,
                System.Array.Empty<Transform>(),
                new Vector2(0.6f, 0.6f));

            Assert.That(placement.BeginPreview(), Is.True);
            Assert.That(
                placement.PreviewWorldPosition(new Vector3(20f, 0f, 0f)),
                Is.True);

            // 0.30 m half-footprint + 0.09 m collision clearance is removed
            // from the near-wall authoring bounds. The visual edge therefore
            // sits about 0.11 m from the side/back wall collider faces.
            Assert.That(
                placement.PreviewPosition.x,
                Is.EqualTo(HomeProductPlacement.DefaultRoomRight - 0.39f).Within(0.001f));
            Assert.That(3.8f - (placement.PreviewPosition.x + 0.3f),
                Is.EqualTo(0.11f).Within(0.001f));
            placement.CancelPreview();
        }
        finally
        {
            Object.DestroyImmediate(baseboard);
            Object.DestroyImmediate(product);
        }
    }
}
