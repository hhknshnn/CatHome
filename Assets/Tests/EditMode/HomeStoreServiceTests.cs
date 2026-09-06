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
    public void LivingRoomCollection_HasReadableDesignSetsAndPlacementLabels()
    {
        int reading = 0;
        int media = 0;
        for (int i = 0; i < HomeStoreService.LivingRoomCollection.Count; i++)
        {
            string productId = HomeStoreService.LivingRoomCollection[i];
            string set = HomeStoreService.GetLivingRoomDesignSetLabel(productId);
            string placement = HomeStoreService.GetPlacementFamilyLabel(productId);
            Assert.That(set, Is.Not.Empty, productId);
            Assert.That(placement, Is.Not.Empty, productId);
            Assert.That(HomeStoreService.GetCatalogEyebrow(productId, "FALLBACK"),
                Does.Contain(set).And.Contain(placement));
            if (set == "READING") reading++;
            if (set == "MEDIA") media++;
        }

        Assert.That(reading, Is.EqualTo(6));
        Assert.That(media, Is.EqualTo(4));
        Assert.That(reading + media, Is.EqualTo(HomeStoreService.LivingRoomItemCount));
        Assert.That(HomeStoreService.GetPlacementFamilyLabel(
            HomeStoreService.ModernTelevisionId), Is.EqualTo("TV UNIT"));
        Assert.That(HomeStoreService.GetPlacementFamilyLabel(
            HomeStoreService.BookSetId), Is.EqualTo("BOOKSHELF"));
    }

    [Test]
    public void LivingRoomDesignSets_ReportOneCanonicalRemainingBudget()
    {
        Assert.That(HomeStoreService.TryGetLivingRoomDesignSetProgress(
            "READING", out int readingOwned, out int readingTotal,
            out long readingRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetLivingRoomDesignSetProgress(
            "MEDIA", out int mediaOwned, out int mediaTotal,
            out long mediaRemaining), Is.True);

        Assert.That(readingOwned, Is.Zero);
        Assert.That(mediaOwned, Is.Zero);
        Assert.That(readingTotal, Is.EqualTo(6));
        Assert.That(mediaTotal, Is.EqualTo(4));
        Assert.That(readingRemaining + mediaRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.LivingRoomId)));

        Assert.That(HomeStoreService.TryGetProduct(
            HomeStoreService.FloorLampId, out HomeStoreProduct lamp), Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, lamp.CoinPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.FloorLampId).Succeeded,
            Is.True);
        Assert.That(HomeStoreService.TryGetLivingRoomDesignSetProgress(
            "READING", out readingOwned, out readingTotal, out long afterPurchase), Is.True);
        Assert.That(readingOwned, Is.EqualTo(1));
        Assert.That(afterPurchase, Is.EqualTo(readingRemaining - lamp.CoinPrice));
        Assert.That(HomeStoreService.GetRoomRemainingCoinCost(HomeRoomService.LivingRoomId),
            Is.EqualTo(readingRemaining + mediaRemaining - lamp.CoinPrice));
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
    public void AuthoredLayout_RejectsDragAndPreservesTheRoomPosition()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState { ownedProductIds = new[] { HomeStoreService.TvUnitId } });
        var product = new GameObject("FixedProduct");
        try
        {
            product.transform.position = new Vector3(-3.2f, 0f, .8f);
            product.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var placement = product.AddComponent<HomeProductPlacement>();
            placement.EditorConfigure(HomeStoreService.TvUnitId, product.transform,
                System.Array.Empty<Transform>(), new Vector2(1.8f, .6f), HomeProductPlacementKind.WallEdge);
            Vector3 position = product.transform.position;
            Quaternion rotation = product.transform.rotation;
            Assert.That(placement.BeginPreview(), Is.False);
            Assert.That(placement.PreviewWorldPosition(Vector3.zero), Is.False);
            placement.RotatePreview(45f);
            Assert.That(product.transform.position, Is.EqualTo(position));
            Assert.That(product.transform.rotation, Is.EqualTo(rotation));
        }
        finally { Object.DestroyImmediate(product); }
    }

    [Test]
    public void AuthoredLayout_RestoresTheAuthoredPoseOnSaveReload()
    {
        var product = new GameObject("FixedPose");
        try
        {
            product.transform.position = new Vector3(1f, 0f, 2f);
            var placement = product.AddComponent<HomeProductPlacement>();
            placement.EditorConfigure(HomeStoreService.FloorLampId, product.transform,
                System.Array.Empty<Transform>(), Vector2.one);
            product.transform.position = Vector3.zero;
            HomeStoreService.ApplySavedState(new HomeStoreSaveState { ownedProductIds = new[] { HomeStoreService.FloorLampId } });
            placement.ApplySavedPlacement();
            Assert.That(product.transform.position, Is.EqualTo(new Vector3(1f, 0f, 2f)));
            Assert.That(placement.SupportsRotation, Is.False);
        }
        finally { Object.DestroyImmediate(product); }
    }

    [Test]
    public void AuthoredLayout_RestoresPreviouslyStoredSupportAndAttachment()
    {
        HomeStoreService.ApplySavedState(new HomeStoreSaveState {
            storeVersion = 6,
            ownedProductIds = new[] { HomeStoreService.TvUnitId, HomeStoreService.ModernTelevisionId },
            storedProductIds = new[] { HomeStoreService.TvUnitId, HomeStoreService.ModernTelevisionId }
        });
        Assert.That(HomeStoreService.IsStored(HomeStoreService.TvUnitId), Is.False);
        Assert.That(HomeStoreService.IsStored(HomeStoreService.ModernTelevisionId), Is.False);
        Assert.That(HomeStoreService.IsProductDependencyMet(HomeStoreService.ModernTelevisionId), Is.True);
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
        int care = 0;
        int spa = 0;
        int floor = 0;
        int wall = 0;
        bool foundHamperLayout = false;
        bool foundTowelLayout = false;
        bool foundMirrorLayout = false;
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

            string set = HomeStoreService.GetBathroomDesignSetLabel(id);
            string placement = HomeStoreService.GetPlacementFamilyLabel(id);
            Assert.That(set, Is.Not.Empty, id);
            Assert.That(placement, Is.Not.Empty, id);
            Assert.That(HomeStoreService.GetCatalogEyebrow(id, "FALLBACK"),
                Is.EqualTo(set + "  •  " + placement), id);

            StoreCatalogAsset authoredAsset = default;
            bool foundAsset = false;
            for (int assetIndex = 0;
                 assetIndex < StoreCatalogAssets.PlaceableProducts.Length;
                 assetIndex++)
            {
                StoreCatalogAsset candidate =
                    StoreCatalogAssets.PlaceableProducts[assetIndex];
                if (candidate.ProductId != id)
                    continue;
                authoredAsset = candidate;
                foundAsset = true;
                break;
            }

            Assert.That(foundAsset, Is.True, id);
            if (id == HomeStoreService.BathroomLaundryHamperId)
            {
                foundHamperLayout = true;
                Assert.That(authoredAsset.DefaultPosition.z,
                    Is.EqualTo(-1.05f).Within(.001f));
            }
            else if (id == HomeStoreService.BathroomTowelStorageId)
            {
                foundTowelLayout = true;
                Assert.That(authoredAsset.DefaultYaw,
                    Is.EqualTo(270f).Within(.001f));
            }
            else if (id == HomeStoreService.BathroomMirrorId)
            {
                foundMirrorLayout = true;
                Assert.That(authoredAsset.DefaultYaw,
                    Is.EqualTo(270f).Within(.001f));
            }
            string authoredPlacement = authoredAsset.PlacementKind ==
                HomeProductPlacementKind.WallEdge ? "WALL" : "FLOOR";
            Assert.That(placement, Is.EqualTo(authoredPlacement), id);

            if (set == "CARE") care++;
            if (set == "SPA") spa++;
            if (placement == "FLOOR") floor++;
            if (placement == "WALL") wall++;
            previousPrice = product.CoinPrice;
        }

        Assert.That(care, Is.EqualTo(6));
        Assert.That(spa, Is.EqualTo(4));
        Assert.That(floor, Is.EqualTo(4));
        Assert.That(wall, Is.EqualTo(6));
        Assert.That(foundHamperLayout, Is.True);
        Assert.That(foundTowelLayout, Is.True);
        Assert.That(foundMirrorLayout, Is.True);
        Assert.That(HomeStoreService.TryGetBathroomDesignSetProgress(
            "CARE", out int careOwned, out int careTotal,
            out long careRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetBathroomDesignSetProgress(
            "SPA", out int spaOwned, out int spaTotal,
            out long spaRemaining), Is.True);
        Assert.That(careOwned, Is.Zero);
        Assert.That(spaOwned, Is.Zero);
        Assert.That(careTotal, Is.EqualTo(6));
        Assert.That(spaTotal, Is.EqualTo(4));
        Assert.That(careRemaining + spaRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.BathroomId)));
    }

    [Test]
    public void KitchenLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.KitchenCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        int cafe = 0;
        int chef = 0;
        int floor = 0;
        int wall = 0;
        bool foundFruitLayout = false;
        bool foundPantryLayout = false;
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

            string set = HomeStoreService.GetKitchenDesignSetLabel(id);
            string placement = HomeStoreService.GetPlacementFamilyLabel(id);
            Assert.That(set, Is.Not.Empty, id);
            Assert.That(placement, Is.Not.Empty, id);
            Assert.That(HomeStoreService.GetCatalogEyebrow(id, "FALLBACK"),
                Is.EqualTo(set + "  •  " + placement), id);

            StoreCatalogAsset authoredAsset = default;
            bool foundAsset = false;
            for (int assetIndex = 0;
                 assetIndex < StoreCatalogAssets.PlaceableProducts.Length;
                 assetIndex++)
            {
                StoreCatalogAsset candidate =
                    StoreCatalogAssets.PlaceableProducts[assetIndex];
                if (candidate.ProductId != id)
                    continue;
                authoredAsset = candidate;
                foundAsset = true;
                break;
            }

            Assert.That(foundAsset, Is.True, id);
            if (id == HomeStoreService.KitchenFruitBasketId)
            {
                foundFruitLayout = true;
                Assert.That(authoredAsset.DefaultPosition.x,
                    Is.EqualTo(-2.9f).Within(.001f));
                Assert.That(authoredAsset.DefaultPosition.z,
                    Is.EqualTo(-.55f).Within(.001f));
            }
            else if (id == HomeStoreService.KitchenPantryShelfId)
            {
                foundPantryLayout = true;
                Assert.That(authoredAsset.DefaultPosition.x,
                    Is.EqualTo(-3.45f).Within(.001f));
                Assert.That(authoredAsset.DefaultYaw,
                    Is.EqualTo(270f).Within(.001f));
            }
            string authoredPlacement = authoredAsset.PlacementKind ==
                HomeProductPlacementKind.WallEdge ? "WALL" : "FLOOR";
            Assert.That(placement, Is.EqualTo(authoredPlacement), id);

            if (set == "CAFE") cafe++;
            if (set == "CHEF") chef++;
            if (placement == "FLOOR") floor++;
            if (placement == "WALL") wall++;
            previousPrice = product.CoinPrice;
        }

        Assert.That(cafe, Is.EqualTo(4));
        Assert.That(chef, Is.EqualTo(6));
        Assert.That(floor, Is.EqualTo(6));
        Assert.That(wall, Is.EqualTo(4));
        Assert.That(foundFruitLayout, Is.True);
        Assert.That(foundPantryLayout, Is.True);
        Assert.That(HomeStoreService.TryGetKitchenDesignSetProgress(
            "CAFE", out int cafeOwned, out int cafeTotal,
            out long cafeRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetKitchenDesignSetProgress(
            "CHEF", out int chefOwned, out int chefTotal,
            out long chefRemaining), Is.True);
        Assert.That(cafeOwned, Is.Zero);
        Assert.That(chefOwned, Is.Zero);
        Assert.That(cafeTotal, Is.EqualTo(4));
        Assert.That(chefTotal, Is.EqualTo(6));
        Assert.That(cafeRemaining + chefRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.KitchenId)));
    }

    [Test]
    public void BedroomLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.BedroomCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        int cozy = 0;
        int royal = 0;
        int floor = 0;
        int wall = 0;
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

            string set = HomeStoreService.GetBedroomDesignSetLabel(id);
            string placement = HomeStoreService.GetPlacementFamilyLabel(id);
            Assert.That(set, Is.Not.Empty, id);
            Assert.That(placement, Is.Not.Empty, id);
            Assert.That(HomeStoreService.GetCatalogEyebrow(id, "FALLBACK"),
                Is.EqualTo(set + "  •  " + placement), id);

            StoreCatalogAsset authoredAsset = default;
            bool foundAsset = false;
            for (int assetIndex = 0;
                 assetIndex < StoreCatalogAssets.PlaceableProducts.Length;
                 assetIndex++)
            {
                StoreCatalogAsset candidate =
                    StoreCatalogAssets.PlaceableProducts[assetIndex];
                if (candidate.ProductId != id)
                    continue;
                authoredAsset = candidate;
                foundAsset = true;
                break;
            }

            Assert.That(foundAsset, Is.True, id);
            string authoredPlacement = authoredAsset.PlacementKind ==
                HomeProductPlacementKind.WallEdge ? "WALL" : "FLOOR";
            Assert.That(placement, Is.EqualTo(authoredPlacement), id);

            if (set == "COZY") cozy++;
            if (set == "ROYAL") royal++;
            if (placement == "FLOOR") floor++;
            if (placement == "WALL") wall++;
            previousPrice = product.CoinPrice;
        }

        Assert.That(cozy, Is.EqualTo(6));
        Assert.That(royal, Is.EqualTo(4));
        Assert.That(floor, Is.EqualTo(6));
        Assert.That(wall, Is.EqualTo(4));
        Assert.That(HomeStoreService.TryGetBedroomDesignSetProgress(
            "COZY", out int cozyOwned, out int cozyTotal,
            out long cozyRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetBedroomDesignSetProgress(
            "ROYAL", out int royalOwned, out int royalTotal,
            out long royalRemaining), Is.True);
        Assert.That(cozyOwned, Is.Zero);
        Assert.That(royalOwned, Is.Zero);
        Assert.That(cozyTotal, Is.EqualTo(6));
        Assert.That(royalTotal, Is.EqualTo(4));
        Assert.That(cozyRemaining + royalRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.BedroomId)));
    }

    [Test]
    public void GardenLevelOne_IsTenUniqueExclusivePlaceableRoomProductsOrderedByPrice()
    {
        Assert.That(HomeStoreService.GardenCollection.Count, Is.EqualTo(10));
        long previousPrice = -1L;
        int nature = 0;
        int patio = 0;
        int floor = 0;
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

            string set = HomeStoreService.GetGardenDesignSetLabel(id);
            string placement = HomeStoreService.GetPlacementFamilyLabel(id);
            Assert.That(set, Is.Not.Empty, id);
            Assert.That(placement, Is.EqualTo("FLOOR"), id);
            Assert.That(HomeStoreService.GetCatalogEyebrow(id, "FALLBACK"),
                Is.EqualTo(set + "  •  FLOOR"), id);

            StoreCatalogAsset authoredAsset = default;
            bool foundAsset = false;
            for (int assetIndex = 0;
                 assetIndex < StoreCatalogAssets.PlaceableProducts.Length;
                 assetIndex++)
            {
                StoreCatalogAsset candidate =
                    StoreCatalogAssets.PlaceableProducts[assetIndex];
                if (candidate.ProductId != id)
                    continue;
                authoredAsset = candidate;
                foundAsset = true;
                break;
            }

            Assert.That(foundAsset, Is.True, id);
            Assert.That(authoredAsset.PlacementKind,
                Is.EqualTo(HomeProductPlacementKind.Floor), id);

            if (set == "NATURE") nature++;
            if (set == "PATIO") patio++;
            if (placement == "FLOOR") floor++;
            previousPrice = product.CoinPrice;
        }

        Assert.That(nature, Is.EqualTo(5));
        Assert.That(patio, Is.EqualTo(5));
        Assert.That(floor, Is.EqualTo(10));
        Assert.That(HomeStoreService.TryGetGardenDesignSetProgress(
            "NATURE", out int natureOwned, out int natureTotal,
            out long natureRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetGardenDesignSetProgress(
            "PATIO", out int patioOwned, out int patioTotal,
            out long patioRemaining), Is.True);
        Assert.That(natureOwned, Is.Zero);
        Assert.That(patioOwned, Is.Zero);
        Assert.That(natureTotal, Is.EqualTo(5));
        Assert.That(patioTotal, Is.EqualTo(5));
        Assert.That(natureRemaining + patioRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.GardenId)));

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
        int sunny = 0;
        int lounge = 0;
        int floor = 0;
        int wall = 0;
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

            string set = HomeStoreService.GetBalconyDesignSetLabel(id);
            string placement = HomeStoreService.GetPlacementFamilyLabel(id);
            Assert.That(set, Is.Not.Empty, id);
            Assert.That(placement, Is.Not.Empty, id);
            Assert.That(HomeStoreService.GetCatalogEyebrow(id, "FALLBACK"),
                Is.EqualTo(set + "  •  " + placement), id);

            StoreCatalogAsset authoredAsset = default;
            bool foundAsset = false;
            for (int assetIndex = 0;
                 assetIndex < StoreCatalogAssets.PlaceableProducts.Length;
                 assetIndex++)
            {
                StoreCatalogAsset candidate =
                    StoreCatalogAssets.PlaceableProducts[assetIndex];
                if (candidate.ProductId != id)
                    continue;
                authoredAsset = candidate;
                foundAsset = true;
                break;
            }

            Assert.That(foundAsset, Is.True, id);
            string authoredPlacement = authoredAsset.PlacementKind ==
                HomeProductPlacementKind.WallEdge ? "WALL" : "FLOOR";
            Assert.That(placement, Is.EqualTo(authoredPlacement), id);

            if (set == "SUNNY") sunny++;
            if (set == "LOUNGE") lounge++;
            if (placement == "FLOOR") floor++;
            if (placement == "WALL") wall++;
            previousPrice = product.CoinPrice;
        }

        Assert.That(sunny, Is.EqualTo(5));
        Assert.That(lounge, Is.EqualTo(5));
        Assert.That(floor, Is.EqualTo(6));
        Assert.That(wall, Is.EqualTo(4));
        Assert.That(HomeStoreService.TryGetBalconyDesignSetProgress(
            "SUNNY", out int sunnyOwned, out int sunnyTotal,
            out long sunnyRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetBalconyDesignSetProgress(
            "LOUNGE", out int loungeOwned, out int loungeTotal,
            out long loungeRemaining), Is.True);
        Assert.That(sunnyOwned, Is.Zero);
        Assert.That(loungeOwned, Is.Zero);
        Assert.That(sunnyTotal, Is.EqualTo(5));
        Assert.That(loungeTotal, Is.EqualTo(5));
        Assert.That(sunnyRemaining + loungeRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.BalconyId)));

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
        int oasis = 0;
        int gather = 0;
        int floor = 0;
        int wall = 0;
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

            string set = HomeStoreService.GetPatioDesignSetLabel(id);
            string placement = HomeStoreService.GetPlacementFamilyLabel(id);
            Assert.That(set, Is.Not.Empty, id);
            Assert.That(placement, Is.Not.Empty, id);
            Assert.That(HomeStoreService.GetCatalogEyebrow(id, "FALLBACK"),
                Is.EqualTo(set + "  •  " + placement), id);

            StoreCatalogAsset authoredAsset = default;
            bool foundAsset = false;
            for (int assetIndex = 0;
                 assetIndex < StoreCatalogAssets.PlaceableProducts.Length;
                 assetIndex++)
            {
                StoreCatalogAsset candidate =
                    StoreCatalogAssets.PlaceableProducts[assetIndex];
                if (candidate.ProductId != id)
                    continue;
                authoredAsset = candidate;
                foundAsset = true;
                break;
            }

            Assert.That(foundAsset, Is.True, id);
            string authoredPlacement = authoredAsset.PlacementKind ==
                HomeProductPlacementKind.WallEdge ? "WALL" : "FLOOR";
            Assert.That(placement, Is.EqualTo(authoredPlacement), id);

            if (set == "OASIS") oasis++;
            if (set == "GATHER") gather++;
            if (placement == "FLOOR") floor++;
            if (placement == "WALL") wall++;
            previousPrice = product.CoinPrice;
        }

        Assert.That(oasis, Is.EqualTo(5));
        Assert.That(gather, Is.EqualTo(5));
        // Eight floor, two wall: PatioStringLights moved off the wall on
        // 5 Eylul 2026 because the Patio has no wall to hang a festoon from.
        Assert.That(floor, Is.EqualTo(8));
        Assert.That(wall, Is.EqualTo(2));
        Assert.That(HomeStoreService.TryGetPatioDesignSetProgress(
            "OASIS", out int oasisOwned, out int oasisTotal,
            out long oasisRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetPatioDesignSetProgress(
            "GATHER", out int gatherOwned, out int gatherTotal,
            out long gatherRemaining), Is.True);
        Assert.That(oasisOwned, Is.Zero);
        Assert.That(gatherOwned, Is.Zero);
        Assert.That(oasisTotal, Is.EqualTo(5));
        Assert.That(gatherTotal, Is.EqualTo(5));
        Assert.That(oasisRemaining + gatherRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.PatioId)));

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
        int nook = 0;
        int studio = 0;
        int floor = 0;
        int wall = 0;
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

            string set = HomeStoreService.GetSecondFloorDesignSetLabel(id);
            string placement = HomeStoreService.GetPlacementFamilyLabel(id);
            Assert.That(set, Is.Not.Empty, id);
            Assert.That(placement, Is.Not.Empty, id);
            Assert.That(HomeStoreService.GetCatalogEyebrow(id, "FALLBACK"),
                Is.EqualTo(set + "  •  " + placement), id);

            StoreCatalogAsset authoredAsset = default;
            bool foundAsset = false;
            for (int assetIndex = 0;
                 assetIndex < StoreCatalogAssets.PlaceableProducts.Length;
                 assetIndex++)
            {
                StoreCatalogAsset candidate =
                    StoreCatalogAssets.PlaceableProducts[assetIndex];
                if (candidate.ProductId != id)
                    continue;
                authoredAsset = candidate;
                foundAsset = true;
                break;
            }

            Assert.That(foundAsset, Is.True, id);
            string authoredPlacement = authoredAsset.PlacementKind ==
                HomeProductPlacementKind.WallEdge ? "WALL" : "FLOOR";
            Assert.That(placement, Is.EqualTo(authoredPlacement), id);

            if (set == "NOOK") nook++;
            if (set == "STUDIO") studio++;
            if (placement == "FLOOR") floor++;
            if (placement == "WALL") wall++;
            previousPrice = product.CoinPrice;
        }

        Assert.That(nook, Is.EqualTo(5));
        Assert.That(studio, Is.EqualTo(5));
        Assert.That(floor, Is.EqualTo(8));
        Assert.That(wall, Is.EqualTo(2));
        Assert.That(HomeStoreService.TryGetSecondFloorDesignSetProgress(
            "NOOK", out int nookOwned, out int nookTotal,
            out long nookRemaining), Is.True);
        Assert.That(HomeStoreService.TryGetSecondFloorDesignSetProgress(
            "STUDIO", out int studioOwned, out int studioTotal,
            out long studioRemaining), Is.True);
        Assert.That(nookOwned, Is.Zero);
        Assert.That(studioOwned, Is.Zero);
        Assert.That(nookTotal, Is.EqualTo(5));
        Assert.That(studioTotal, Is.EqualTo(5));
        Assert.That(nookRemaining + studioRemaining,
            Is.EqualTo(HomeStoreService.GetRoomRemainingCoinCost(
                HomeRoomService.SecondFloorId)));

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
        Assert.That(HomeStoreService.EconomyChecksEnabled, Is.False);
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
        Assert.That(HomeStoreService.IsStored(HomeStoreService.TvUnitId), Is.False);
        Assert.That(HomeStoreService.IsStored(HomeStoreService.ModernTelevisionId), Is.False);
        Assert.That(EconomyService.Coins, Is.Zero);
        Assert.That(EconomyService.Diamonds, Is.Zero);
    }

    [Test]
    public void FreshPlaceablePurchase_IsVisibleImmediately()
    {
        HomeStoreService.TryGetProduct(HomeStoreService.FloorLampId, out HomeStoreProduct product);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(product.Id).Succeeded, Is.True);
        Assert.That(HomeStoreService.IsOwned(product.Id), Is.True);
        Assert.That(HomeStoreService.IsStored(product.Id), Is.False);
        Assert.That(HomeStoreService.CaptureState().storedProductIds, Is.Empty);
    }

    [Test]
    public void ShopCompletion_DoesNotStartADragSession()
    {
        HomeStoreService.TryAcquireForTesting(HomeStoreService.KitchenFruitBasketId);
        var host = new GameObject("FixedShop");
        try
        {
            var shop = host.AddComponent<ShopPanelController>();
            typeof(ShopPanelController).GetMethod("BeginPlacement", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(shop, new object[] { HomeStoreService.KitchenFruitBasketId });
            Assert.That((bool)typeof(ShopPanelController).GetField("placementMode", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shop), Is.False);
            Assert.That(HomeStoreService.IsStored(HomeStoreService.KitchenFruitBasketId), Is.False);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void LegacyRoomEditor_CannotOpenOrHideOwnedItems()
    {
        var host = new GameObject("RetiredRoomEditor");
        try
        {
            var editor = host.AddComponent<HomeEditModeController>();
            editor.Open();
            Assert.That(editor.IsOpen, Is.False);
            Assert.That(HomeEditModeController.IsAnyOpen, Is.False);
        }
        finally { Object.DestroyImmediate(host); }
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
    public void LegacyPlacementMigration_DropsCoordinatesAndKeepsOwnershipAndWallet()
    {
        EconomyService.ApplyLegacyBalances(1200, 45);
        HomeStoreService.ApplySavedState(new HomeStoreSaveState {
            storeVersion = 6,
            ownedProductIds = new[] { HomeStoreService.FloorLampId },
            placements = new[] { new HomeStorePlacementEntry(HomeStoreService.FloorLampId, new Vector3(99f, 0f, 99f), 45f) }
        });
        var save = HomeStoreService.CaptureState();
        Assert.That(save.storeVersion, Is.EqualTo(8));
        Assert.That(save.placements, Is.Empty);
        Assert.That(save.ownedProductIds, Does.Contain(HomeStoreService.FloorLampId));
        Assert.That(EconomyService.Coins, Is.EqualTo(1200));
        Assert.That(EconomyService.Diamonds, Is.EqualTo(45));
        HomeStoreService.ApplySavedState(save);
        Assert.That(HomeStoreService.TryGetWorldPlacement(HomeStoreService.FloorLampId, out _, out _), Is.False);
    }

    [Test]
    public void AllRoomProducts_RemainVisibleAndRejectStorage()
    {
        var ids = new System.Collections.Generic.List<string>();
        foreach (var room in HomeRoomService.Rooms) ids.AddRange(HomeStoreService.GetRoomCollection(room.Id));
        HomeStoreService.ApplySavedState(new HomeStoreSaveState { ownedProductIds = ids.ToArray(), storedProductIds = ids.ToArray() });
        Assert.That(ids.Count, Is.EqualTo(80));
        foreach (string id in ids)
        {
            Assert.That(HomeStoreService.TrySetStored(id, true), Is.False, id);
            Assert.That(HomeStoreService.IsStored(id), Is.False, id);
            Assert.That(HomeStoreService.IsOwned(id), Is.True, id);
        }
        Assert.That(HomeStoreService.CaptureState().storedProductIds, Is.Empty);
    }

    [Test]
    public void Storage_RejectsUnownedAndNonPlaceableProducts()
    {
        Assert.That(HomeStoreService.TrySetStored(
            HomeStoreService.BallBasketId, true), Is.False);

        HomeStoreService.ApplySavedState(new HomeStoreSaveState
        {
            ownedProductIds = new[] { HomeStoreService.HomeBathroomPreviewId }
        });
        Assert.That(HomeStoreService.TrySetStored(
            HomeStoreService.HomeBathroomPreviewId, true), Is.False);
    }

    [Test]
    public void LegacyPlacementClear_IsIdempotentAndKeepsOwnership()
    {
        HomeStoreService.TryAcquireForTesting(HomeStoreService.FloorLampId);
        Assert.That(HomeStoreService.TrySetPlacement(HomeStoreService.FloorLampId, Vector3.one, 45f), Is.False);
        Assert.That(HomeStoreService.TryClearPlacement(HomeStoreService.FloorLampId), Is.True);
        Assert.That(HomeStoreService.TryClearPlacement(HomeStoreService.FloorLampId), Is.True);
        Assert.That(HomeStoreService.IsOwned(HomeStoreService.FloorLampId), Is.True);
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
    public void FixedPlacement_RejectsBothLegacySlotAndWorldMutation()
    {
        HomeStoreService.TryAcquireForTesting(HomeStoreService.FloorLampId);
        Assert.That(HomeStoreService.TrySetPlacement(HomeStoreService.FloorLampId, 1, 3), Is.False);
        Assert.That(HomeStoreService.TrySetPlacement(HomeStoreService.FloorLampId, Vector3.zero, 90f), Is.False);
        Assert.That(HomeStoreService.CaptureState().placements, Is.Empty);
    }

    [Test]
    public void RepeatedLegacyCloudImport_CannotRestoreStorageOrFreePlacement()
    {
        var legacy = new HomeStoreSaveState {
            storeVersion = 6,
            ownedProductIds = new[] { HomeStoreService.FloorLampId },
            storedProductIds = new[] { HomeStoreService.FloorLampId },
            placements = new[] { new HomeStorePlacementEntry(HomeStoreService.FloorLampId, Vector3.one, 270f) }
        };
        for (int i = 0; i < 2; i++)
        {
            HomeStoreService.ApplySavedState(legacy);
            var current = HomeStoreService.CaptureState();
            Assert.That(current.storedProductIds, Is.Empty);
            Assert.That(current.placements, Is.Empty);
            Assert.That(current.ownedProductIds, Does.Contain(HomeStoreService.FloorLampId));
        }
    }
}
