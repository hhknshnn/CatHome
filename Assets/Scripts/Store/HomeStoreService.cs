using System;
using System.Collections.Generic;
using CatHome.Economy;
using UnityEngine;

[Serializable]
public sealed class HomeStoreSaveState
{
    public int storeVersion = HomeStoreService.SaveVersion;
    public string[] ownedProductIds = Array.Empty<string>();
    // Legacy fields remain readable for v11 saves. Store v7 discards custom
    // ROOM placements and storage; legacy CAT toys keep their existing layout.
    public string[] storedProductIds = Array.Empty<string>();
    public HomeStorePlacementEntry[] placements = Array.Empty<HomeStorePlacementEntry>();
    // Version 5: last successfully activated home room. Room ownership remains
    // authoritative in ownedProductIds; a locked/unknown id falls back safely.
    public string currentRoomId = HomeRoomService.LivingRoomId;

    public static HomeStoreSaveState CreateDefault()
    {
        return new HomeStoreSaveState
        {
            storeVersion = HomeStoreService.SaveVersion,
            ownedProductIds = Array.Empty<string>(),
            storedProductIds = Array.Empty<string>(),
            placements = Array.Empty<HomeStorePlacementEntry>(),
            currentRoomId = HomeRoomService.LivingRoomId
        };
    }
}

[Serializable]
public sealed class HomeStorePlacementEntry
{
    public string productId;
    public int slotIndex;
    public bool hasWorldPosition;
    public float positionX;
    public float positionZ;
    public float rotationY;

    public HomeStorePlacementEntry() { }

    public HomeStorePlacementEntry(string id, int index)
    {
        productId = id;
        slotIndex = index;
    }

    public HomeStorePlacementEntry(string id, Vector3 position, float yaw)
    {
        productId = id;
        slotIndex = 0;
        hasWorldPosition = true;
        positionX = position.x;
        positionZ = position.z;
        rotationY = yaw;
    }

    public HomeStorePlacementEntry Clone()
    {
        return new HomeStorePlacementEntry
        {
            productId = productId,
            slotIndex = slotIndex,
            hasWorldPosition = hasWorldPosition,
            positionX = positionX,
            positionZ = positionZ,
            rotationY = rotationY
        };
    }
}

public enum HomeStoreCategory
{
    Cat = 0,
    Room = 1,
    Home = 2
}

public readonly struct HomeStoreProduct
{
    public HomeStoreProduct(
        string id,
        string title,
        HomeStoreCategory storeCategory,
        string category,
        string description,
        long coinPrice,
        long diamondPrice,
        int requiredLevel,
        bool isAvailable,
        bool isPlaceable)
    {
        Id = id;
        titleEnglish = title;
        StoreCategory = storeCategory;
        Category = category;
        descriptionEnglish = description;
        CoinPrice = Math.Max(0L, coinPrice);
        DiamondPrice = Math.Max(0L, diamondPrice);
        RequiredLevel = Math.Max(1, requiredLevel);
        IsAvailable = isAvailable;
        IsPlaceable = isPlaceable;
    }

    public string Id { get; }
    private readonly string titleEnglish;
    public string Title => GameProductCopy.Title(Id,titleEnglish);
    public HomeStoreCategory StoreCategory { get; }
    public string Category { get; }
    private readonly string descriptionEnglish;
    public string Description => GameProductCopy.Description(Id,descriptionEnglish);
    public long CoinPrice { get; }
    public long DiamondPrice { get; }
    public int RequiredLevel { get; }
    public bool IsAvailable { get; }
    public bool IsPlaceable { get; }
    public bool SupportsCoins => CoinPrice > 0L;
    public bool SupportsDiamonds => DiamondPrice > 0L;
}

public enum HomeStorePurchaseStatus
{
    Purchased,
    AlreadyOwned,
    InsufficientCoins,
    InsufficientDiamonds,
    LevelLocked,
    ComingSoon,
    CollectionIncomplete,
    RequiredProductMissing,
    UnknownProduct,
    Rejected
}

public readonly struct HomeStorePurchaseResult
{
    public HomeStorePurchaseResult(
        HomeStorePurchaseStatus status,
        HomeStoreProduct product,
        long missingAmount,
        CurrencyType currency = CurrencyType.None)
    {
        Status = status;
        Product = product;
        MissingAmount = Math.Max(0L, missingAmount);
        Currency = currency;
    }

    public HomeStorePurchaseStatus Status { get; }
    public HomeStoreProduct Product { get; }
    public long MissingAmount { get; }
    public long MissingCoins => Currency == CurrencyType.Coin ? MissingAmount : 0L;
    public CurrencyType Currency { get; }
    public bool Succeeded => Status == HomeStorePurchaseStatus.Purchased;
}

public readonly struct HomeStorePurchaseGoal
{
    public HomeStorePurchaseGoal(HomeStoreProduct product, long coinBalance)
    {
        Product = product;
        CoinBalance = Math.Max(0L, coinBalance);
        MissingCoins = Math.Max(0L, product.CoinPrice - CoinBalance);
    }

    public HomeStoreProduct Product { get; }
    public long CoinBalance { get; }
    public long MissingCoins { get; }
    public bool CanAfford => MissingCoins == 0L;
}

/// <summary>
/// Persistent ownership authority for room objects bought with earned coins.
/// Product presentation lives in the UI builder; product identity, price and
/// ownership live here so scene visuals and the shop always agree.
/// </summary>
public static class HomeStoreService
{
    public const int SaveVersion = 8;
    public static bool IsFixedRoomProduct(string productId) =>
        TryGetProduct(productId, out HomeStoreProduct product) &&
        product.IsPlaceable && product.StoreCategory == HomeStoreCategory.Room;
    // Temporary hands-on QA switch. Keep false while content/layout testing is
    // active; the real wallet path remains covered by TryPurchase tests and is
    // enabled only at the final economy/release gate.
    public const bool EconomyChecksEnabled = false;
    public const string BallBasketId = "home.ball-basket";
    public const string ScratchPostId = "home.scratch-post";
    public const string CozyPodBedId = "cat.cozy-pod-bed";
    public const string ToyMouseId = "cat.toy-mouse";
    public const string PlayTunnelId = "cat.play-tunnel";
    public const string CeramicBowlId = "cat.ceramic-bowl";
    public const string CloudBedId = "cat.cloud-bed";
    public const string CanopyBedId = "cat.canopy-bed";
    public const string TreatJarId = "cat.treat-jar";
    public const string FeatherToyId = "cat.feather-toy";
    public const string CollarId = "cat.collar";
    public const string LeashId = "cat.leash";
    public const string BellCollarId = "cat.bell-collar";
    public const string KibbleBagId = "cat.kibble-bag";
    public const string NapPillowId = "cat.nap-pillow";
    public const string CatnipPlantId = "cat.catnip-plant";
    public const string CardboardHideoutId = "cat.cardboard-hideout";
    public const string TvUnitId = "room.tv-unit";
    public const string ModernTelevisionId = "room.tv-console";
    // Kept as a save-compatible alias for older code and ownership records.
    public const string TvConsoleId = ModernTelevisionId;
    public const string GameConsoleId = "room.game-console";
    public const string FloorLampId = "room.floor-lamp";
    public const string SideTableId = "room.side-table";
    public const string SofaId = "room.sofa";
    public const string ArmchairId = "room.armchair";
    public const string CoffeeTableId = "room.coffee-table";
    public const string CarpetId = "room.carpet";
    public const string BookshelfId = "room.bookshelf";
    public const string BookSetId = "room.colorful-book-set";
    public const string TallPlantId = "room.tall-plant";
    public const string SmallPlantId = "room.small-plant";
    public const string ModernPaintingId = "room.modern-painting";
    public const string MirrorId = "room.mirror";
    public const string WallClockId = "room.wall-clock";
    public const string StereoId = "room.stereo";
    public const string RetroTvId = "room.retro-tv";
    public const string StoolId = "room.stool";
    public const string BathroomBathMatId = "bathroom.bath-mat";
    public const string BathroomLaundryHamperId = "bathroom.laundry-hamper";
    public const string BathroomLitterBoxId = "bathroom.litter-box";
    public const string BathroomGroomingCartId = "bathroom.grooming-cart";
    public const string BathroomTowelStorageId = "bathroom.towel-storage";
    public const string BathroomMirrorId = "bathroom.wall-mirror";
    public const string BathroomToiletId = "bathroom.toilet";
    public const string BathroomVanityId = "bathroom.vanity-sink";
    public const string BathroomTubId = "bathroom.tub";
    public const string BathroomShowerId = "bathroom.shower";
    public const string KitchenPawMatId = "kitchen.paw-mat";
    public const string KitchenFruitBasketId = "kitchen.fruit-basket";
    public const string KitchenFeedingStationId = "kitchen.feeding-station";
    public const string KitchenCounterStoolId = "kitchen.counter-stool";
    public const string KitchenPantryShelfId = "kitchen.pantry-shelf";
    public const string KitchenDishCartId = "kitchen.dish-cart";
    public const string KitchenSinkCabinetId = "kitchen.sink-cabinet";
    public const string KitchenRefrigeratorId = "kitchen.refrigerator";
    public const string KitchenStoveOvenId = "kitchen.stove-oven";
    public const string KitchenIslandId = "kitchen.island";
    public const string BedroomPawRugId = "bedroom.paw-rug";
    public const string BedroomNightLightId = "bedroom.night-light";
    public const string BedroomDreamArtId = "bedroom.dream-art";
    public const string BedroomYarnBasketId = "bedroom.yarn-basket";
    public const string BedroomNightstandId = "bedroom.nightstand";
    public const string BedroomVanityStoolId = "bedroom.vanity-stool";
    public const string BedroomWardrobeId = "bedroom.wardrobe";
    public const string BedroomWindowDaybedId = "bedroom.window-daybed";
    public const string BedroomStarCanopyId = "bedroom.star-canopy";
    public const string BedroomQueenBedId = "bedroom.queen-bed";
    public const string GardenYarnBallId = "garden.yarn-ball";
    public const string GardenFlowerPotsId = "garden.flower-pots";
    public const string GardenDaisyBedId = "garden.daisy-bed";
    public const string GardenSaplingId = "garden.sapling";
    public const string GardenBirdBathId = "garden.bird-bath";
    public const string GardenSunLoungerId = "garden.sun-lounger";
    public const string GardenBistroSetId = "garden.bistro-set";
    public const string GardenGrillId = "garden.grill";
    public const string GardenHammockId = "garden.hammock";
    public const string GardenPergolaId = "garden.pergola";
    public const string BalconySunMatId = "balcony.sun-mat";
    public const string BalconyPlanterBoxId = "balcony.planter-box";
    public const string BalconyHerbShelfId = "balcony.herb-shelf";
    public const string BalconyRailingFlowersId = "balcony.railing-flowers";
    public const string BalconyBirdFeederId = "balcony.bird-feeder";
    public const string BalconyLanternStringId = "balcony.lantern-string";
    public const string BalconyCushionBenchId = "balcony.cushion-bench";
    public const string BalconySideTableId = "balcony.side-table";
    public const string BalconyHangingChairId = "balcony.hanging-chair";
    public const string BalconySunAwningId = "balcony.sun-awning";
    public const string PatioStoneRugId = "patio.stone-rug";
    public const string PatioPottedFernsId = "patio.potted-ferns";
    public const string PatioHerbTroughId = "patio.herb-trough";
    public const string PatioStringLightsId = "patio.string-lights";
    public const string PatioWaterFountainId = "patio.water-fountain";
    public const string PatioFirePitId = "patio.fire-pit";
    public const string PatioDiningSetId = "patio.dining-set";
    public const string PatioParasolId = "patio.parasol";
    public const string PatioPorchSwingId = "patio.porch-swing";
    public const string PatioPergolaArchId = "patio.pergola-arch";
    public const string LoftFloorRunnerId = "loft.floor-runner";
    public const string LoftFloorCushionsId = "loft.floor-cushions";
    public const string LoftBookStackId = "loft.book-stack";
    public const string LoftArcLampId = "loft.arc-lamp";
    public const string LoftBeanBagId = "loft.bean-bag";
    public const string LoftRecordPlayerId = "loft.record-player";
    public const string LoftStudyDeskId = "loft.study-desk";
    public const string LoftWallGalleryId = "loft.wall-gallery";
    public const string LoftTallBookcaseId = "loft.tall-bookcase";
    public const string LoftChaiseLoungeId = "loft.chaise-lounge";
    public const string HomeRoomsPreviewId = "home.rooms-preview";
    public const string HomeGardenPreviewId = "home.garden-preview";
    public const string HomeKitchenPreviewId = "home.kitchen-preview";
    public const string HomeBedroomPreviewId = "home.bedroom-preview";
    public const string HomeBathroomPreviewId = "home.bathroom-preview";
    public const string HomeBalconyPreviewId = "home.balcony-preview";
    public const string HomePatioPreviewId = "home.patio-preview";
    public const string HomeSecondFloorPreviewId = "home.second-floor-preview";
    public const long BallBasketPrice = 300;
    public const long ScratchPostPrice = 400;
    public const long CoinsPerDiamond = 100;
    public const long BookSetPrice = 1300;
    public const long BathroomCoinPrice = 3000;
    public const long BathroomDiamondPrice = 30;
    public const long KitchenCoinPrice = 4000;
    public const long KitchenDiamondPrice = 40;
    public const long BedroomCoinPrice = 5000;
    public const long BedroomDiamondPrice = 50;
    public const long GardenCoinPrice = 6000;
    public const long GardenDiamondPrice = 60;
    public const long BalconyCoinPrice = 7000;
    public const long BalconyDiamondPrice = 70;
    public const long PatioCoinPrice = 8000;
    public const long PatioDiamondPrice = 80;
    public const long SecondFloorCoinPrice = 9000;
    public const long SecondFloorDiamondPrice = 90;

    private static readonly string[] LivingRoomCollectionInternal =
    {
        FloorLampId,
        ArmchairId,
        TallPlantId,
        BookshelfId,
        BookSetId,
        ModernPaintingId,
        TvUnitId,
        GameConsoleId,
        StereoId,
        ModernTelevisionId
    };

    private static readonly string[] LivingRoomReadingSetInternal =
    {
        FloorLampId,
        ArmchairId,
        TallPlantId,
        BookshelfId,
        BookSetId,
        ModernPaintingId
    };

    private static readonly string[] LivingRoomMediaSetInternal =
    {
        TvUnitId,
        GameConsoleId,
        StereoId,
        ModernTelevisionId
    };

    private static readonly string[] BathroomCollectionInternal =
    {
        BathroomBathMatId,
        BathroomLaundryHamperId,
        BathroomLitterBoxId,
        BathroomGroomingCartId,
        BathroomTowelStorageId,
        BathroomMirrorId,
        BathroomToiletId,
        BathroomVanityId,
        BathroomTubId,
        BathroomShowerId
    };

    private static readonly string[] BathroomCareSetInternal =
    {
        BathroomBathMatId,
        BathroomLaundryHamperId,
        BathroomLitterBoxId,
        BathroomGroomingCartId,
        BathroomTowelStorageId,
        BathroomMirrorId
    };

    private static readonly string[] BathroomSpaSetInternal =
    {
        BathroomToiletId,
        BathroomVanityId,
        BathroomTubId,
        BathroomShowerId
    };

    private static readonly string[] KitchenCollectionInternal =
    {
        KitchenPawMatId,
        KitchenFruitBasketId,
        KitchenFeedingStationId,
        KitchenCounterStoolId,
        KitchenPantryShelfId,
        KitchenDishCartId,
        KitchenSinkCabinetId,
        KitchenRefrigeratorId,
        KitchenStoveOvenId,
        KitchenIslandId
    };

    private static readonly string[] KitchenCafeSetInternal =
    {
        KitchenPawMatId,
        KitchenFruitBasketId,
        KitchenFeedingStationId,
        KitchenCounterStoolId
    };

    private static readonly string[] KitchenChefSetInternal =
    {
        KitchenPantryShelfId,
        KitchenDishCartId,
        KitchenSinkCabinetId,
        KitchenRefrigeratorId,
        KitchenStoveOvenId,
        KitchenIslandId
    };

    private static readonly string[] BedroomCollectionInternal =
    {
        BedroomPawRugId,
        BedroomNightLightId,
        BedroomDreamArtId,
        BedroomYarnBasketId,
        BedroomNightstandId,
        BedroomVanityStoolId,
        BedroomWardrobeId,
        BedroomWindowDaybedId,
        BedroomStarCanopyId,
        BedroomQueenBedId
    };

    private static readonly string[] BedroomCozySetInternal =
    {
        BedroomPawRugId,
        BedroomNightLightId,
        BedroomDreamArtId,
        BedroomYarnBasketId,
        BedroomNightstandId,
        BedroomVanityStoolId
    };

    private static readonly string[] BedroomRoyalSetInternal =
    {
        BedroomWardrobeId,
        BedroomWindowDaybedId,
        BedroomStarCanopyId,
        BedroomQueenBedId
    };

    private static readonly string[] GardenCollectionInternal =
    {
        GardenYarnBallId,
        GardenFlowerPotsId,
        GardenDaisyBedId,
        GardenSaplingId,
        GardenBirdBathId,
        GardenSunLoungerId,
        GardenBistroSetId,
        GardenGrillId,
        GardenHammockId,
        GardenPergolaId
    };

    private static readonly string[] GardenNatureSetInternal =
    {
        GardenYarnBallId,
        GardenFlowerPotsId,
        GardenDaisyBedId,
        GardenSaplingId,
        GardenBirdBathId
    };

    private static readonly string[] GardenPatioSetInternal =
    {
        GardenSunLoungerId,
        GardenBistroSetId,
        GardenGrillId,
        GardenHammockId,
        GardenPergolaId
    };

    private static readonly string[] BalconyCollectionInternal =
    {
        BalconySunMatId,
        BalconyPlanterBoxId,
        BalconyHerbShelfId,
        BalconyRailingFlowersId,
        BalconyBirdFeederId,
        BalconyLanternStringId,
        BalconyCushionBenchId,
        BalconySideTableId,
        BalconyHangingChairId,
        BalconySunAwningId
    };

    private static readonly string[] BalconySunnySetInternal =
    {
        BalconySunMatId,
        BalconyPlanterBoxId,
        BalconyHerbShelfId,
        BalconyRailingFlowersId,
        BalconyBirdFeederId
    };

    private static readonly string[] BalconyLoungeSetInternal =
    {
        BalconyLanternStringId,
        BalconyCushionBenchId,
        BalconySideTableId,
        BalconyHangingChairId,
        BalconySunAwningId
    };

    private static readonly string[] PatioCollectionInternal =
    {
        PatioStoneRugId,
        PatioPottedFernsId,
        PatioHerbTroughId,
        PatioStringLightsId,
        PatioWaterFountainId,
        PatioFirePitId,
        PatioDiningSetId,
        PatioParasolId,
        PatioPorchSwingId,
        PatioPergolaArchId
    };

    private static readonly string[] PatioOasisSetInternal =
    {
        PatioStoneRugId,
        PatioPottedFernsId,
        PatioHerbTroughId,
        PatioStringLightsId,
        PatioWaterFountainId
    };

    private static readonly string[] PatioGatherSetInternal =
    {
        PatioFirePitId,
        PatioDiningSetId,
        PatioParasolId,
        PatioPorchSwingId,
        PatioPergolaArchId
    };

    private static readonly string[] SecondFloorCollectionInternal =
    {
        LoftFloorRunnerId,
        LoftFloorCushionsId,
        LoftBookStackId,
        LoftArcLampId,
        LoftBeanBagId,
        LoftRecordPlayerId,
        LoftStudyDeskId,
        LoftWallGalleryId,
        LoftTallBookcaseId,
        LoftChaiseLoungeId
    };

    private static readonly string[] SecondFloorNookSetInternal =
    {
        LoftFloorRunnerId,
        LoftFloorCushionsId,
        LoftBookStackId,
        LoftArcLampId,
        LoftBeanBagId
    };

    private static readonly string[] SecondFloorStudioSetInternal =
    {
        LoftRecordPlayerId,
        LoftStudyDeskId,
        LoftWallGalleryId,
        LoftTallBookcaseId,
        LoftChaiseLoungeId
    };

    private static readonly HomeStoreProduct[] ProductsInternal =
    {
        new HomeStoreProduct(
            BallBasketId,
            "BALL BASKET",
            HomeStoreCategory.Cat,
            "TOY",
            "A basket full of soft balls. Unlocks Ball Chase in your living room.",
            BallBasketPrice,
            3L,
            1,
            true,
            true),
        new HomeStoreProduct(
            ScratchPostId,
            "SCRATCHING POST",
            HomeStoreCategory.Cat,
            "COMFORT",
            "A sturdy scratching post. Unlocks the Scratch activity at home.",
            ScratchPostPrice,
            4L,
            1,
            true,
            true),
        new HomeStoreProduct(
            CozyPodBedId,
            "COZY POD BED",
            HomeStoreCategory.Cat,
            "REST",
            "A soft low-sided bed made for naps beside the family.",
            1300L,
            13L,
            1,
            true,
            true),
        new HomeStoreProduct(
            ToyMouseId,
            "TOY MOUSE",
            HomeStoreCategory.Cat,
            "TOY",
            "A bright clockwork mouse for short bursts of indoor play.",
            600L,
            6L,
            1,
            true,
            true),
        new HomeStoreProduct(
            PlayTunnelId,
            "PLAY TUNNEL",
            HomeStoreCategory.Cat,
            "TOY",
            "A colorful tunnel with a hanging toy at the entrance.",
            1200L,
            12L,
            2,
            true,
            true),
        new HomeStoreProduct(
            CeramicBowlId,
            "CERAMIC BOWL",
            HomeStoreCategory.Cat,
            "FEEDING",
            "A weighty ceramic bowl that stays put during enthusiastic meals.",
            700L, 7L, 2, true, true),
        new HomeStoreProduct(
            CloudBedId,
            "CLOUD BED",
            HomeStoreCategory.Cat,
            "REST",
            "A plush oval bed for long afternoon naps in a quiet corner.",
            900L, 9L, 2, true, true),
        new HomeStoreProduct(
            CanopyBedId,
            "CANOPY CAT BED",
            HomeStoreCategory.Cat,
            "REST",
            "An open canopy bed: climb in, knead the cushion and curl up for a nap.",
            1500L, 15L, 3, true, true),
        new HomeStoreProduct(
            TreatJarId,
            "TREAT PUZZLE",
            HomeStoreCategory.Cat,
            "FEEDING",
            "A little treat puzzle with a paw button and a bouncing lid.",
            500L, 5L, 2, true, true),
        new HomeStoreProduct(
            FeatherToyId,
            "BOUNCY CAT TOY",
            HomeStoreCategory.Cat,
            "TOY",
            "A springy wobble toy that adds movement to the play corner.",
            800L, 8L, 2, true, true),
        new HomeStoreProduct(
            CollarId,
            "BALL TRACK",
            HomeStoreCategory.Cat,
            "TOY",
            "Bat the bright ball and watch it race around its circular track.",
            1000L, 10L, 3, true, true),
        new HomeStoreProduct(
            LeashId,
            "RIBBON PLAY MAT",
            HomeStoreCategory.Cat,
            "TOY",
            "Catch a wiggly ribbon on a soft mint play mat.",
            1100L, 11L, 3, true, true),
        new HomeStoreProduct(
            BellCollarId,
            "JINGLE ROLLER",
            HomeStoreCategory.Cat,
            "TOY",
            "A little rolling rattle that spins when your cat bats it.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            KibbleBagId,
            "FOOD PUZZLE",
            HomeStoreCategory.Cat,
            "FEEDING",
            "A food puzzle with a moving release button for curious paws.",
            600L, 6L, 2, true, true),
        new HomeStoreProduct(
            NapPillowId,
            "NAP PILLOW",
            HomeStoreCategory.Cat,
            "REST",
            "A squashy floor pillow for short sunbeam naps.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            CatnipPlantId,
            "CAT GRASS POT",
            HomeStoreCategory.Cat,
            "GREENERY",
            "A small leafy pot for the play corner and curious nibbles.",
            500L, 5L, 2, true, true),
        new HomeStoreProduct(
            CardboardHideoutId,
            "CARDBOARD HIDEOUT",
            HomeStoreCategory.Cat,
            "TOY",
            "An open box hideout. Sometimes the simplest toy is the favorite.",
            800L, 8L, 2, true, true),
        new HomeStoreProduct(
            ModernTelevisionId,
            "MODERN TELEVISION",
            HomeStoreCategory.Room,
            "ENTERTAINMENT",
            "A clean modern television that can only be installed on the TV unit.",
            2500L,
            25L,
            1,
            true,
            true),
        new HomeStoreProduct(
            TvUnitId,
            "TV UNIT",
            HomeStoreCategory.Room,
            "FURNITURE",
            "A wide low furniture unit designed to support the television.",
            1800L,
            18L,
            1,
            true,
            true),
        new HomeStoreProduct(
            GameConsoleId,
            "GAME CONSOLE SET",
            HomeStoreCategory.Room,
            "ENTERTAINMENT",
            "A modern game console with a classic controller for the living-room play corner.",
            2000L,
            20L,
            1,
            true,
            true),
        new HomeStoreProduct(
            FloorLampId,
            "FLOOR LAMP",
            HomeStoreCategory.Room,
            "LIGHTING",
            "A warm floor lamp that fits beside a sofa, bed or reading corner.",
            500L,
            5L,
            1,
            true,
            true),
        new HomeStoreProduct(
            SideTableId,
            "SIDE TABLE",
            HomeStoreCategory.Room,
            "FURNITURE",
            "A small round table for an open corner or beside the sofa.",
            700L,
            7L,
            1,
            true,
            true),
        new HomeStoreProduct(
            SofaId,
            "TWO-SEAT SOFA",
            HomeStoreCategory.Room,
            "FURNITURE",
            "A compact modern sofa that can become the main anchor of the room.",
            1800L, 18L, 1, true, true),
        new HomeStoreProduct(
            ArmchairId,
            "CLASSIC ARMCHAIR",
            HomeStoreCategory.Room,
            "FURNITURE",
            "A comfortable reading chair for an open corner near a lamp.",
            700L, 7L, 1, true, true),
        new HomeStoreProduct(
            CoffeeTableId,
            "COFFEE TABLE",
            HomeStoreCategory.Room,
            "FURNITURE",
            "A low wooden table sized for the centre of the living room.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            CarpetId,
            "WOVEN CARPET",
            HomeStoreCategory.Room,
            "TEXTILE",
            "A large woven carpet that instantly changes the room's color balance.",
            800L, 8L, 1, true, true),
        new HomeStoreProduct(
            BookshelfId,
            "TALL BOOKSHELF",
            HomeStoreCategory.Room,
            "STORAGE",
            "An empty tall bookshelf. Place it first, then add the colorful book set.",
            1100L, 11L, 1, true, true),
        new HomeStoreProduct(
            BookSetId,
            "COLORFUL BOOK SET",
            HomeStoreCategory.Room,
            "BOOKS",
            "Ten colorful books arranged across all three shelves. Drag the set directly onto the bookshelf.",
            BookSetPrice, 13L, 1, true, true),
        new HomeStoreProduct(
            TallPlantId,
            "TALL HOUSEPLANT",
            HomeStoreCategory.Room,
            "GREENERY",
            "A sculptural indoor plant for softening a bare corner.",
            900L, 9L, 1, true, true),
        new HomeStoreProduct(
            SmallPlantId,
            "TABLE PLANT",
            HomeStoreCategory.Room,
            "GREENERY",
            "A small potted plant for a table, shelf or sunny window corner.",
            500L, 5L, 2, true, true),
        new HomeStoreProduct(
            ModernPaintingId,
            "MODERN PAINTING",
            HomeStoreCategory.Room,
            "WALL DECOR",
            "A bold framed print that brings color to an empty wall.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            MirrorId,
            "WALL MIRROR",
            HomeStoreCategory.Room,
            "WALL DECOR",
            "A clean-lined mirror that makes a compact room feel brighter.",
            900L, 9L, 3, true, true),
        new HomeStoreProduct(
            WallClockId,
            "ROUND WALL CLOCK",
            HomeStoreCategory.Room,
            "WALL DECOR",
            "A classic wall clock for finishing the living room composition.",
            1100L, 11L, 1, true, true),
        new HomeStoreProduct(
            StereoId,
            "SPEAKER SYSTEM",
            HomeStoreCategory.Room,
            "ENTERTAINMENT",
            "A stereo main unit with two matching speakers for a fuller living-room sound setup.",
            2200L, 22L, 1, true, true),
        new HomeStoreProduct(
            RetroTvId,
            "RETRO TELEVISION",
            HomeStoreCategory.Room,
            "ENTERTAINMENT",
            "A characterful retro television for a warm nostalgic room theme.",
            1600L, 16L, 4, true, true),
        new HomeStoreProduct(
            StoolId,
            "ACCENT STOOL",
            HomeStoreCategory.Room,
            "FURNITURE",
            "A small accent stool that fits beside a table or reading chair.",
            600L, 6L, 2, true, true),
        new HomeStoreProduct(
            BathroomBathMatId,
            "PAW BATH MAT",
            HomeStoreCategory.Room,
            "BATHROOM TEXTILE",
            "A soft aqua bath mat with a bright paw detail for the bathroom floor.",
            500L, 5L, 1, true, true),
        new HomeStoreProduct(
            BathroomLaundryHamperId,
            "LAUNDRY HAMPER",
            HomeStoreCategory.Room,
            "BATHROOM STORAGE",
            "A colorful woven hamper for towels and small bathroom laundry.",
            700L, 7L, 1, true, true),
        new HomeStoreProduct(
            BathroomLitterBoxId,
            "CAT LITTER BOX",
            HomeStoreCategory.Room,
            "BATHROOM CAT CARE",
            "A tidy low-sided litter tray designed for the bathroom care corner.",
            900L, 9L, 1, true, true),
        new HomeStoreProduct(
            BathroomGroomingCartId,
            "GROOMING CART",
            HomeStoreCategory.Room,
            "BATHROOM CAT CARE",
            "A rolling care cart with brushes, bottles and room for grooming supplies.",
            1100L, 11L, 1, true, true),
        new HomeStoreProduct(
            BathroomTowelStorageId,
            "TOWEL STORAGE",
            HomeStoreCategory.Room,
            "BATHROOM STORAGE",
            "A slim gold-trimmed rack filled with rolled mint, coral and lilac towels.",
            1300L, 13L, 1, true, true),
        new HomeStoreProduct(
            BathroomMirrorId,
            "BUBBLE WALL MIRROR",
            HomeStoreCategory.Room,
            "BATHROOM WALL DECOR",
            "A bright round-edged mirror that makes the bathroom feel open and polished.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            BathroomToiletId,
            "PASTEL TOILET",
            HomeStoreCategory.Room,
            "BATHROOM FIXTURE",
            "A compact porcelain toilet with cheerful pastel accents.",
            1700L, 17L, 1, true, true),
        new HomeStoreProduct(
            BathroomVanityId,
            "VANITY & SINK",
            HomeStoreCategory.Room,
            "BATHROOM FIXTURE",
            "A roomy vanity with a glossy sink and useful storage underneath.",
            1900L, 19L, 1, true, true),
        new HomeStoreProduct(
            BathroomTubId,
            "CAT PAW BATHTUB",
            HomeStoreCategory.Room,
            "BATHROOM FIXTURE",
            "A deep premium bathtub for future splash and wash activities.",
            2100L, 21L, 1, true, true),
        new HomeStoreProduct(
            BathroomShowerId,
            "RAINBOW SHOWER",
            HomeStoreCategory.Room,
            "BATHROOM FIXTURE",
            "A glass shower enclosure with a bright premium frame and rain head.",
            2300L, 23L, 1, true, true),
        new HomeStoreProduct(
            KitchenPawMatId,
            "PAW BREAKFAST RUG",
            HomeStoreCategory.Room,
            "KITCHEN TEXTILE",
            "A sunny paw-print rug that adds a soft landing to the breakfast corner.",
            500L, 5L, 1, true, true),
        new HomeStoreProduct(
            KitchenFruitBasketId,
            "RAINBOW FRUIT BASKET",
            HomeStoreCategory.Room,
            "KITCHEN DECOR",
            "A gold-trimmed fruit basket for the kitchen counter. A curious paw can tip it over and scatter the fruit.",
            700L, 7L, 1, true, true),
        new HomeStoreProduct(
            KitchenFeedingStationId,
            "TWIN FEEDING STATION",
            HomeStoreCategory.Room,
            "KITCHEN CAT CARE",
            "A mint feeding nook with two easy-to-reach bowls and a cheerful paw badge.",
            900L, 9L, 1, true, true),
        new HomeStoreProduct(
            KitchenCounterStoolId,
            "BREAKFAST STOOL",
            HomeStoreCategory.Room,
            "KITCHEN SEATING",
            "A lilac counter stool with sturdy gold legs for the breakfast corner.",
            1100L, 11L, 1, true, true),
        new HomeStoreProduct(
            KitchenPantryShelfId,
            "CANDY PANTRY SHELF",
            HomeStoreCategory.Room,
            "KITCHEN STORAGE",
            "A tall open pantry filled with colorful jars, boxes and cat-safe treats.",
            1300L, 13L, 1, true, true),
        new HomeStoreProduct(
            KitchenDishCartId,
            "PASTEL DISH CART",
            HomeStoreCategory.Room,
            "KITCHEN STORAGE",
            "A compact aqua cart carrying cups, plates and tidy kitchen supplies.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            KitchenSinkCabinetId,
            "PAW SINK CABINET",
            HomeStoreCategory.Room,
            "KITCHEN FIXTURE",
            "A coral-and-cream sink cabinet with a polished gold faucet.",
            1700L, 17L, 1, true, true),
        new HomeStoreProduct(
            KitchenRefrigeratorId,
            "CANDY REFRIGERATOR",
            HomeStoreCategory.Room,
            "KITCHEN APPLIANCE",
            "A tall mint refrigerator with lilac handles and playful paw magnets.",
            1900L, 19L, 1, true, true),
        new HomeStoreProduct(
            KitchenStoveOvenId,
            "CUPCAKE STOVE & OVEN",
            HomeStoreCategory.Room,
            "KITCHEN APPLIANCE",
            "A peach stove and oven with bright burners, knobs and a dark glass window.",
            2100L, 21L, 1, true, true),
        new HomeStoreProduct(
            KitchenIslandId,
            "CAT KITCHEN ISLAND",
            HomeStoreCategory.Room,
            "KITCHEN FURNITURE",
            "A roomy cream-and-aqua island with coral panels and gold details.",
            2300L, 23L, 1, true, true),
        new HomeStoreProduct(
            BedroomPawRugId,
            "STARRY PAW RUG",
            HomeStoreCategory.Room,
            "BEDROOM TEXTILE",
            "A plush lilac rug with a mint paw print for sleepy paws.",
            200L, 2L, 1, true, true),
        new HomeStoreProduct(
            BedroomNightLightId,
            "MOON NIGHT LIGHT",
            HomeStoreCategory.Room,
            "BEDROOM LIGHTING",
            "A glowing lemon moon lamp that keeps bedtime soft and bright.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            BedroomDreamArtId,
            "DREAM WALL ART",
            HomeStoreCategory.Room,
            "BEDROOM WALL",
            "A pastel star-and-cloud painting that hangs at cat-eye height.",
            600L, 6L, 1, true, true),
        new HomeStoreProduct(
            BedroomYarnBasketId,
            "YARN BASKET",
            HomeStoreCategory.Room,
            "BEDROOM DECOR",
            "A coral basket of mint and lilac yarn balls for quiet play.",
            800L, 8L, 1, true, true),
        new HomeStoreProduct(
            BedroomNightstandId,
            "PASTEL NIGHTSTAND",
            HomeStoreCategory.Room,
            "BEDROOM FURNITURE",
            "A peach nightstand with a gold drawer and tiny paw knob.",
            1000L, 10L, 1, true, true),
        new HomeStoreProduct(
            BedroomVanityStoolId,
            "CLOUD VANITY STOOL",
            HomeStoreCategory.Room,
            "BEDROOM FURNITURE",
            "A round aqua stool with a cream cushion for the dressing corner.",
            1200L, 12L, 1, true, true),
        new HomeStoreProduct(
            BedroomWardrobeId,
            "RAINBOW WARDROBE",
            HomeStoreCategory.Room,
            "BEDROOM FURNITURE",
            "A tall mint wardrobe with coral doors and gold handles.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            BedroomWindowDaybedId,
            "WINDOW DAYBED",
            HomeStoreCategory.Room,
            "BEDROOM FURNITURE",
            "A peach window seat with mint pillows for sunny naps.",
            1800L, 18L, 1, true, true),
        new HomeStoreProduct(
            BedroomStarCanopyId,
            "STAR CANOPY",
            HomeStoreCategory.Room,
            "BEDROOM FURNITURE",
            "A lilac canopy nook with hanging stars for secret cat naps.",
            2100L, 21L, 1, true, true),
        new HomeStoreProduct(
            BedroomQueenBedId,
            "QUEEN CLOUD BED",
            HomeStoreCategory.Room,
            "BEDROOM FURNITURE",
            "A big cream bed with coral pillows and a mint headboard.",
            2500L, 25L, 1, true, true),
        new HomeStoreProduct(
            GardenYarnBallId,
            "SUNNY YARN BALL",
            HomeStoreCategory.Room,
            "GARDEN TOY",
            "A bright yarn ball for chasing across the lawn.",
            200L, 2L, 1, true, true),
        new HomeStoreProduct(
            GardenFlowerPotsId,
            "FLOWER POTS",
            HomeStoreCategory.Room,
            "GARDEN PLANTS",
            "A cluster of coral, lemon and lilac blooms in sunny pots.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            GardenDaisyBedId,
            "DAISY FLOWER BED",
            HomeStoreCategory.Room,
            "GARDEN PLANTS",
            "A low mint bed packed with daisies for the courtyard edge.",
            600L, 6L, 1, true, true),
        new HomeStoreProduct(
            GardenSaplingId,
            "LITTLE GARDEN TREE",
            HomeStoreCategory.Room,
            "GARDEN PLANTS",
            "A small shade tree just the right size for a courtyard.",
            800L, 8L, 1, true, true),
        new HomeStoreProduct(
            GardenBirdBathId,
            "BIRD BATH",
            HomeStoreCategory.Room,
            "GARDEN DECOR",
            "A shallow peach bath where courtyard birds come to splash.",
            1000L, 10L, 1, true, true),
        new HomeStoreProduct(
            GardenSunLoungerId,
            "SUN LOUNGER",
            HomeStoreCategory.Room,
            "GARDEN FURNITURE",
            "A mint balcony lounger for sleepy sun patches.",
            1200L, 12L, 1, true, true),
        new HomeStoreProduct(
            GardenBistroSetId,
            "BALCONY SET",
            HomeStoreCategory.Room,
            "GARDEN FURNITURE",
            "A peach table and two chairs for sunny snacks outside.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            GardenGrillId,
            "PAW GRILL",
            HomeStoreCategory.Room,
            "GARDEN FURNITURE",
            "A compact coral barbecue with gold knobs for courtyard cookouts.",
            1800L, 18L, 1, true, true),
        new HomeStoreProduct(
            GardenHammockId,
            "GARDEN HAMMOCK",
            HomeStoreCategory.Room,
            "GARDEN FURNITURE",
            "A lilac hammock strung between gold posts for long naps.",
            2100L, 21L, 1, true, true),
        new HomeStoreProduct(
            GardenPergolaId,
            "SUN PERGOLA",
            HomeStoreCategory.Room,
            "GARDEN FURNITURE",
            "A cream pergola with mint beams that frames the whole courtyard.",
            2500L, 25L, 1, true, true),
        new HomeStoreProduct(
            BalconySunMatId,
            "SUN MAT",
            HomeStoreCategory.Room,
            "BALCONY DECOR",
            "A soft striped mat for warm sun patches on the balcony floor.",
            200L, 2L, 1, true, true),
        new HomeStoreProduct(
            BalconyPlanterBoxId,
            "PLANTER BOX",
            HomeStoreCategory.Room,
            "BALCONY PLANTS",
            "A cheerful wooden box of coral and lemon blooms.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            BalconyHerbShelfId,
            "HERB SHELF",
            HomeStoreCategory.Room,
            "BALCONY PLANTS",
            "A little shelf of mint and basil pots for the sunny corner.",
            600L, 6L, 1, true, true),
        new HomeStoreProduct(
            BalconyRailingFlowersId,
            "RAILING FLOWERS",
            HomeStoreCategory.Room,
            "BALCONY PLANTS",
            "Flower boxes clipped along the railing over the city view.",
            800L, 8L, 1, true, true),
        new HomeStoreProduct(
            BalconyBirdFeederId,
            "BIRD FEEDER",
            HomeStoreCategory.Room,
            "BALCONY DECOR",
            "A hanging feeder where little birds visit the railing.",
            1000L, 10L, 1, true, true),
        new HomeStoreProduct(
            BalconyLanternStringId,
            "LANTERN STRING",
            HomeStoreCategory.Room,
            "BALCONY DECOR",
            "A string of warm gold lanterns for cozy evening light.",
            1200L, 12L, 1, true, true),
        new HomeStoreProduct(
            BalconyCushionBenchId,
            "CUSHION BENCH",
            HomeStoreCategory.Room,
            "BALCONY FURNITURE",
            "A low bench piled with mint and peach cushions.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            BalconySideTableId,
            "SIDE TABLE",
            HomeStoreCategory.Room,
            "BALCONY FURNITURE",
            "A round peach table just right for a cup and a nap.",
            1800L, 18L, 1, true, true),
        new HomeStoreProduct(
            BalconyHangingChairId,
            "HANGING CHAIR",
            HomeStoreCategory.Room,
            "BALCONY FURNITURE",
            "A lilac egg chair swinging gently from a gold hook.",
            2100L, 21L, 1, true, true),
        new HomeStoreProduct(
            BalconySunAwningId,
            "SUN AWNING",
            HomeStoreCategory.Room,
            "BALCONY FURNITURE",
            "A striped awning that shades the whole balcony from above.",
            2500L, 25L, 1, true, true),
        new HomeStoreProduct(
            PatioStoneRugId,
            "STONE PATIO RUG",
            HomeStoreCategory.Room,
            "PATIO DECOR",
            "A soft outdoor rug that warms the paved stone floor.",
            200L, 2L, 1, true, true),
        new HomeStoreProduct(
            PatioPottedFernsId,
            "POTTED FERNS",
            HomeStoreCategory.Room,
            "PATIO PLANTS",
            "A pair of leafy ferns in tall stone planters.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            PatioHerbTroughId,
            "HERB TROUGH",
            HomeStoreCategory.Room,
            "PATIO PLANTS",
            "A long trough of mint and lavender along the patio edge.",
            600L, 6L, 1, true, true),
        new HomeStoreProduct(
            PatioStringLightsId,
            "PATIO STRING LIGHTS",
            HomeStoreCategory.Room,
            "PATIO DECOR",
            "Warm bulbs strung overhead for cozy evenings outside.",
            800L, 8L, 1, true, true),
        new HomeStoreProduct(
            PatioWaterFountainId,
            "WATER FOUNTAIN",
            HomeStoreCategory.Room,
            "PATIO DECOR",
            "A little tiered stone fountain with trickling aqua water.",
            1000L, 10L, 1, true, true),
        new HomeStoreProduct(
            PatioFirePitId,
            "FIRE PIT",
            HomeStoreCategory.Room,
            "PATIO FURNITURE",
            "A round stone fire pit with a warm coral glow.",
            1200L, 12L, 1, true, true),
        new HomeStoreProduct(
            PatioDiningSetId,
            "PATIO DINING SET",
            HomeStoreCategory.Room,
            "PATIO FURNITURE",
            "A cream table and four chairs for sunny outdoor meals.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            PatioParasolId,
            "GARDEN PARASOL",
            HomeStoreCategory.Room,
            "PATIO FURNITURE",
            "A big mint parasol that shades the whole dining set.",
            1800L, 18L, 1, true, true),
        new HomeStoreProduct(
            PatioPorchSwingId,
            "PORCH SWING",
            HomeStoreCategory.Room,
            "PATIO FURNITURE",
            "A cushioned swing bench hung from a sturdy gold frame.",
            2100L, 21L, 1, true, true),
        new HomeStoreProduct(
            PatioPergolaArchId,
            "PERGOLA ARCH",
            HomeStoreCategory.Room,
            "PATIO FURNITURE",
            "A grand cream arch with climbing vines over the patio.",
            2500L, 25L, 1, true, true),
        new HomeStoreProduct(
            LoftFloorRunnerId,
            "LOFT FLOOR RUNNER",
            HomeStoreCategory.Room,
            "LOFT DECOR",
            "A long woven runner that warms the loft's wooden floor.",
            200L, 2L, 1, true, true),
        new HomeStoreProduct(
            LoftFloorCushionsId,
            "FLOOR CUSHIONS",
            HomeStoreCategory.Room,
            "LOFT DECOR",
            "A cozy pile of oversized floor cushions for lounging.",
            400L, 4L, 1, true, true),
        new HomeStoreProduct(
            LoftBookStackId,
            "BOOK STACKS",
            HomeStoreCategory.Room,
            "LOFT DECOR",
            "Neat stacks of colorful books for the reading loft.",
            600L, 6L, 1, true, true),
        new HomeStoreProduct(
            LoftArcLampId,
            "ARC FLOOR LAMP",
            HomeStoreCategory.Room,
            "LOFT LIGHTING",
            "A tall arc lamp that curves warm light over the nook.",
            800L, 8L, 1, true, true),
        new HomeStoreProduct(
            LoftBeanBagId,
            "BEAN BAG CHAIR",
            HomeStoreCategory.Room,
            "LOFT FURNITURE",
            "A squishy coral bean bag that molds to a napping cat.",
            1000L, 10L, 1, true, true),
        new HomeStoreProduct(
            LoftRecordPlayerId,
            "RECORD PLAYER",
            HomeStoreCategory.Room,
            "LOFT FURNITURE",
            "A retro turntable on a slim stand for lazy afternoons.",
            1200L, 12L, 1, true, true),
        new HomeStoreProduct(
            LoftStudyDeskId,
            "STUDY DESK",
            HomeStoreCategory.Room,
            "LOFT FURNITURE",
            "A tidy wooden desk with a lamp and a comfy chair.",
            1500L, 15L, 1, true, true),
        new HomeStoreProduct(
            LoftWallGalleryId,
            "WALL GALLERY",
            HomeStoreCategory.Room,
            "LOFT FURNITURE",
            "A framed gallery wall of bright little art prints.",
            1800L, 18L, 1, true, true),
        new HomeStoreProduct(
            LoftTallBookcaseId,
            "TALL BOOKCASE",
            HomeStoreCategory.Room,
            "LOFT FURNITURE",
            "A floor-to-ceiling bookcase packed with books and plants.",
            2100L, 21L, 1, true, true),
        new HomeStoreProduct(
            LoftChaiseLoungeId,
            "CHAISE LOUNGE",
            HomeStoreCategory.Room,
            "LOFT FURNITURE",
            "A velvet chaise by the window for sunny afternoon naps.",
            2500L, 25L, 1, true, true),
        new HomeStoreProduct(
            HomeRoomsPreviewId,
            "LIVING ROOM",
            HomeStoreCategory.Home,
            "CURRENT ROOM",
            "Your first complete room collection. Finish it to unlock the next room.",
            0L,
            0L,
            1,
            false,
            false),
        new HomeStoreProduct(
            HomeGardenPreviewId,
            "GARDEN",
            HomeStoreCategory.Home,
            "NEW ROOM",
            "A sunny fenced courtyard with grass, birds and its own 10-piece garden collection.",
            GardenCoinPrice,
            GardenDiamondPrice,
            5,
            true,
            false),
        new HomeStoreProduct(
            HomeBedroomPreviewId,
            "BEDROOM",
            HomeStoreCategory.Home,
            "NEW ROOM",
            "A calm pastel bedroom with its own completely new 10-piece collection.",
            BedroomCoinPrice, BedroomDiamondPrice, 4, true, false),
        new HomeStoreProduct(
            HomeBathroomPreviewId,
            "BATHROOM",
            HomeStoreCategory.Home,
            "NEW ROOM",
            "A warm cat-friendly bathroom with its own completely new 10-piece collection.",
            BathroomCoinPrice, BathroomDiamondPrice, 2, true, false),
        new HomeStoreProduct(
            HomeKitchenPreviewId,
            "KITCHEN",
            HomeStoreCategory.Home,
            "NEW ROOM",
            "A bright cat-friendly kitchen with its own completely new 10-piece collection.",
            KitchenCoinPrice, KitchenDiamondPrice, 3, true, false),
        new HomeStoreProduct(
            HomeBalconyPreviewId,
            "BALCONY",
            HomeStoreCategory.Home,
            "NEW ROOM",
            "A sunny balcony with plants, cushions and its own 10-piece balcony collection over a city view.",
            BalconyCoinPrice, BalconyDiamondPrice, 9, true, false),
        new HomeStoreProduct(
            HomePatioPreviewId,
            "GARDEN PATIO",
            HomeStoreCategory.Home,
            "NEW ROOM",
            "A paved garden patio with a fountain, fire pit and its own 10-piece patio collection.",
            PatioCoinPrice, PatioDiamondPrice, 10, true, false),
        new HomeStoreProduct(
            HomeSecondFloorPreviewId,
            "SECOND FLOOR",
            HomeStoreCategory.Home,
            "NEW ROOM",
            "A cozy upstairs loft with a big window and its own 10-piece loft collection.",
            SecondFloorCoinPrice, SecondFloorDiamondPrice, 12, true, false)
    };

    private static readonly Dictionary<string, HomeStoreProduct> ProductsById =
        new Dictionary<string, HomeStoreProduct>(StringComparer.Ordinal);
    private static readonly HashSet<string> OwnedProductIds =
        new HashSet<string>(StringComparer.Ordinal);
    private static readonly HashSet<string> StoredProductIds =
        new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, HomeStorePlacementEntry> PlacementByProductId =
        new Dictionary<string, HomeStorePlacementEntry>(StringComparer.Ordinal);

    static HomeStoreService()
    {
        for (int i = 0; i < ProductsInternal.Length; i++)
            ProductsById[ProductsInternal[i].Id] = ProductsInternal[i];
    }

    public static IReadOnlyList<HomeStoreProduct> Products => ProductsInternal;
    public static IReadOnlyList<string> LivingRoomCollection => LivingRoomCollectionInternal;
    public static IReadOnlyList<string> BathroomCollection => BathroomCollectionInternal;
    public static IReadOnlyList<string> KitchenCollection => KitchenCollectionInternal;
    public static IReadOnlyList<string> BedroomCollection => BedroomCollectionInternal;
    public static IReadOnlyList<string> GardenCollection => GardenCollectionInternal;
    public static IReadOnlyList<string> BalconyCollection => BalconyCollectionInternal;
    public static IReadOnlyList<string> PatioCollection => PatioCollectionInternal;
    public static IReadOnlyList<string> SecondFloorCollection => SecondFloorCollectionInternal;
    public static bool FreePurchaseTestingEnabled => !EconomyChecksEnabled;
    public static int LivingRoomItemCount => LivingRoomCollectionInternal.Length;
    public static int LivingRoomOwnedCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < LivingRoomCollectionInternal.Length; i++)
            {
                if (IsOwned(LivingRoomCollectionInternal[i]))
                    count++;
            }
            return count;
        }
    }
    public static bool IsLivingRoomComplete =>
        LivingRoomOwnedCount >= LivingRoomCollectionInternal.Length;
    public static int BathroomItemCount => BathroomCollectionInternal.Length;
    public static int BathroomOwnedCount => GetOwnedCount(BathroomCollectionInternal);
    public static bool IsBathroomComplete =>
        BathroomOwnedCount >= BathroomCollectionInternal.Length;
    public static int KitchenItemCount => KitchenCollectionInternal.Length;
    public static int KitchenOwnedCount => GetOwnedCount(KitchenCollectionInternal);
    public static bool IsKitchenComplete =>
        KitchenOwnedCount >= KitchenCollectionInternal.Length;
    public static int BedroomItemCount => BedroomCollectionInternal.Length;
    public static int BedroomOwnedCount => GetOwnedCount(BedroomCollectionInternal);
    public static bool IsBedroomComplete =>
        BedroomOwnedCount >= BedroomCollectionInternal.Length;
    public static int GardenItemCount => GardenCollectionInternal.Length;
    public static int GardenOwnedCount => GetOwnedCount(GardenCollectionInternal);
    public static bool IsGardenComplete =>
        GardenOwnedCount >= GardenCollectionInternal.Length;
    public static int BalconyItemCount => BalconyCollectionInternal.Length;
    public static int BalconyOwnedCount => GetOwnedCount(BalconyCollectionInternal);
    public static bool IsBalconyComplete =>
        BalconyOwnedCount >= BalconyCollectionInternal.Length;
    public static int PatioItemCount => PatioCollectionInternal.Length;
    public static int PatioOwnedCount => GetOwnedCount(PatioCollectionInternal);
    public static bool IsPatioComplete =>
        PatioOwnedCount >= PatioCollectionInternal.Length;
    public static int SecondFloorItemCount => SecondFloorCollectionInternal.Length;
    public static int SecondFloorOwnedCount => GetOwnedCount(SecondFloorCollectionInternal);
    public static bool IsSecondFloorComplete =>
        SecondFloorOwnedCount >= SecondFloorCollectionInternal.Length;
    public static int CatalogCount => ProductsInternal.Length;
    public static int OwnedCatalogCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < ProductsInternal.Length; i++)
            {
                if (IsOwned(ProductsInternal[i].Id))
                    count++;
            }
            return count;
        }
    }
    public static event Action<string> OwnershipChanged;
    public static event Action<string> PlacementChanged;
    public static event Action<string> StorageChanged;

    public static bool TryGetProduct(string productId, out HomeStoreProduct product)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            product = default;
            return false;
        }

        return ProductsById.TryGetValue(productId, out product);
    }

    public static bool IsOwned(string productId)
    {
        return !string.IsNullOrWhiteSpace(productId) && OwnedProductIds.Contains(productId);
    }

    public static bool IsStored(string productId)
    {
        if (IsFixedRoomProduct(productId)) return false;
        return IsOwned(productId) && StoredProductIds.Contains(productId);
    }

    /// <summary>
    /// Packs an owned placeable product away, or restores it to its last placement.
    /// Ownership, collection progress and purchase history never change.
    /// </summary>
    public static bool TrySetStored(string productId, bool stored)
    {
        if (IsFixedRoomProduct(productId)) return !stored && IsOwned(productId);
        if (!IsOwned(productId) ||
            !TryGetProduct(productId, out HomeStoreProduct product) ||
            !product.IsPlaceable)
        {
            return false;
        }

        if (!stored && !CatCollectionPolicy.CanDisplay(productId, out _)) return false;
        if (!stored && !CatRoomArrangement.PrepareAddition(productId)) return false;

        bool changed = stored
            ? StoredProductIds.Add(productId)
            : StoredProductIds.Remove(productId);
        if (!changed)
            return true;

        CatHomeSaveSystem.SaveNow();
        StorageChanged?.Invoke(productId);
        return true;
    }

    /// <summary>
    /// Total Home XP represented by the currently owned products: the sum of their
    /// coin prices, matching the amount each one grants on purchase. Used to
    /// back-fill Home progression at load for a home furnished before the Home XP
    /// slice existed, so already-owned products still count toward Home Level.
    /// </summary>
    public static long SumOwnedHomeXp()
    {
        long total = 0L;
        foreach (string id in OwnedProductIds)
        {
            if (ProductsById.TryGetValue(id, out HomeStoreProduct product))
                total = product.CoinPrice > long.MaxValue - total
                    ? long.MaxValue
                    : total + product.CoinPrice;
        }
        return total;
    }

    public static bool MeetsHomeLevelRequirement(HomeStoreProduct product)
    {
        return HomeProgressionService.MeetsHomeLevelRequirement(product.RequiredLevel);
    }

    public static bool MeetsHomeLevelRequirement(int requiredLevel)
    {
        return HomeProgressionService.MeetsHomeLevelRequirement(requiredLevel);
    }

    public static bool IsLivingRoomCollectionProduct(string productId)
    {
        return Array.IndexOf(LivingRoomCollectionInternal, productId) >= 0;
    }

    public static string GetLivingRoomDesignSetLabel(string productId)
    {
        if (Array.IndexOf(LivingRoomReadingSetInternal, productId) >= 0)
            return "READING";
        if (Array.IndexOf(LivingRoomMediaSetInternal, productId) >= 0)
            return "MEDIA";
        return string.Empty;
    }

    public static string GetBathroomDesignSetLabel(string productId)
    {
        if (Array.IndexOf(BathroomCareSetInternal, productId) >= 0)
            return "CARE";
        if (Array.IndexOf(BathroomSpaSetInternal, productId) >= 0)
            return "SPA";
        return string.Empty;
    }

    public static string GetKitchenDesignSetLabel(string productId)
    {
        if (Array.IndexOf(KitchenCafeSetInternal, productId) >= 0)
            return "CAFE";
        if (Array.IndexOf(KitchenChefSetInternal, productId) >= 0)
            return "CHEF";
        return string.Empty;
    }

    public static string GetBedroomDesignSetLabel(string productId)
    {
        if (Array.IndexOf(BedroomCozySetInternal, productId) >= 0)
            return "COZY";
        if (Array.IndexOf(BedroomRoyalSetInternal, productId) >= 0)
            return "ROYAL";
        return string.Empty;
    }

    public static string GetGardenDesignSetLabel(string productId)
    {
        if (Array.IndexOf(GardenNatureSetInternal, productId) >= 0)
            return "NATURE";
        if (Array.IndexOf(GardenPatioSetInternal, productId) >= 0)
            return "PATIO";
        return string.Empty;
    }

    public static string GetBalconyDesignSetLabel(string productId)
    {
        if (Array.IndexOf(BalconySunnySetInternal, productId) >= 0)
            return "SUNNY";
        if (Array.IndexOf(BalconyLoungeSetInternal, productId) >= 0)
            return "LOUNGE";
        return string.Empty;
    }

    public static string GetPatioDesignSetLabel(string productId)
    {
        if (Array.IndexOf(PatioOasisSetInternal, productId) >= 0)
            return "OASIS";
        if (Array.IndexOf(PatioGatherSetInternal, productId) >= 0)
            return "GATHER";
        return string.Empty;
    }

    public static string GetSecondFloorDesignSetLabel(string productId)
    {
        if (Array.IndexOf(SecondFloorNookSetInternal, productId) >= 0)
            return "NOOK";
        if (Array.IndexOf(SecondFloorStudioSetInternal, productId) >= 0)
            return "STUDIO";
        return string.Empty;
    }

    public static string GetRoomDesignSetLabel(string productId)
    {
        string livingRoomSet = GetLivingRoomDesignSetLabel(productId);
        if (!string.IsNullOrEmpty(livingRoomSet))
            return livingRoomSet;

        string bathroomSet = GetBathroomDesignSetLabel(productId);
        if (!string.IsNullOrEmpty(bathroomSet))
            return bathroomSet;

        string kitchenSet = GetKitchenDesignSetLabel(productId);
        if (!string.IsNullOrEmpty(kitchenSet))
            return kitchenSet;

        string bedroomSet = GetBedroomDesignSetLabel(productId);
        if (!string.IsNullOrEmpty(bedroomSet))
            return bedroomSet;

        string gardenSet = GetGardenDesignSetLabel(productId);
        if (!string.IsNullOrEmpty(gardenSet))
            return gardenSet;

        string balconySet = GetBalconyDesignSetLabel(productId);
        if (!string.IsNullOrEmpty(balconySet))
            return balconySet;

        string patioSet = GetPatioDesignSetLabel(productId);
        return !string.IsNullOrEmpty(patioSet)
            ? patioSet
            : GetSecondFloorDesignSetLabel(productId);
    }

    /// <summary>
    /// Returns the authored Living Room design-set size, owned count and the
    /// remaining Coin budget. This is display-only economy guidance: it never
    /// discounts, grants or spends currency and therefore cannot diverge from
    /// the canonical product catalog.
    /// </summary>
    public static bool TryGetLivingRoomDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "READING", StringComparison.OrdinalIgnoreCase))
            products = LivingRoomReadingSetInternal;
        else if (string.Equals(setLabel, "MEDIA", StringComparison.OrdinalIgnoreCase))
            products = LivingRoomMediaSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    public static bool TryGetBathroomDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "CARE", StringComparison.OrdinalIgnoreCase))
            products = BathroomCareSetInternal;
        else if (string.Equals(setLabel, "SPA", StringComparison.OrdinalIgnoreCase))
            products = BathroomSpaSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    public static bool TryGetKitchenDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "CAFE", StringComparison.OrdinalIgnoreCase))
            products = KitchenCafeSetInternal;
        else if (string.Equals(setLabel, "CHEF", StringComparison.OrdinalIgnoreCase))
            products = KitchenChefSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    public static bool TryGetBedroomDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "COZY", StringComparison.OrdinalIgnoreCase))
            products = BedroomCozySetInternal;
        else if (string.Equals(setLabel, "ROYAL", StringComparison.OrdinalIgnoreCase))
            products = BedroomRoyalSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    public static bool TryGetGardenDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "NATURE", StringComparison.OrdinalIgnoreCase))
            products = GardenNatureSetInternal;
        else if (string.Equals(setLabel, "PATIO", StringComparison.OrdinalIgnoreCase))
            products = GardenPatioSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    public static bool TryGetBalconyDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "SUNNY", StringComparison.OrdinalIgnoreCase))
            products = BalconySunnySetInternal;
        else if (string.Equals(setLabel, "LOUNGE", StringComparison.OrdinalIgnoreCase))
            products = BalconyLoungeSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    public static bool TryGetPatioDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "OASIS", StringComparison.OrdinalIgnoreCase))
            products = PatioOasisSetInternal;
        else if (string.Equals(setLabel, "GATHER", StringComparison.OrdinalIgnoreCase))
            products = PatioGatherSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    public static bool TryGetSecondFloorDesignSetProgress(
        string setLabel,
        out int owned,
        out int total,
        out long remainingCoins)
    {
        IReadOnlyList<string> products;
        if (string.Equals(setLabel, "NOOK", StringComparison.OrdinalIgnoreCase))
            products = SecondFloorNookSetInternal;
        else if (string.Equals(setLabel, "STUDIO", StringComparison.OrdinalIgnoreCase))
            products = SecondFloorStudioSetInternal;
        else
        {
            owned = 0;
            total = 0;
            remainingCoins = 0L;
            return false;
        }

        owned = GetOwnedCount(products);
        total = products.Count;
        remainingCoins = GetRemainingCoinCost(products);
        return true;
    }

    /// <summary>
    /// Coin value still needed to complete a room collection at catalog prices.
    /// Owned products contribute zero; unknown room ids safely use Living Room,
    /// matching <see cref="GetRoomCollection"/>.
    /// </summary>
    public static long GetRoomRemainingCoinCost(string roomId)
    {
        return GetRemainingCoinCost(GetRoomCollection(roomId));
    }

    /// <summary>
    /// Returns the next unowned product in the room's canonical price order and
    /// how many Coins the current wallet still needs. Display-only: no balance,
    /// ownership or save state is changed.
    /// </summary>
    public static bool TryGetNextRoomPurchaseGoal(
        string roomId,
        out HomeStorePurchaseGoal goal)
    {
        IReadOnlyList<string> collection = GetRoomCollection(roomId);
        for (int i = 0; i < collection.Count; i++)
        {
            string productId = collection[i];
            if (IsOwned(productId) ||
                !ProductsById.TryGetValue(productId, out HomeStoreProduct product) ||
                !product.SupportsCoins)
            {
                continue;
            }

            goal = new HomeStorePurchaseGoal(product, EconomyService.Coins);
            return true;
        }

        goal = default;
        return false;
    }

    public static string GetPlacementFamilyLabel(string productId)
    {
        if(productId==KitchenFruitBasketId)return "COUNTER";
        if (string.Equals(productId, BookSetId, StringComparison.Ordinal))
            return "BOOKSHELF";
        if (string.Equals(productId, ModernTelevisionId, StringComparison.Ordinal))
            return "TV UNIT";
        if (string.Equals(productId, BookshelfId, StringComparison.Ordinal) ||
            string.Equals(productId, ModernPaintingId, StringComparison.Ordinal) ||
            string.Equals(productId, TvUnitId, StringComparison.Ordinal))
        {
            return "WALL";
        }

        if (string.Equals(productId, BathroomTowelStorageId, StringComparison.Ordinal) ||
            string.Equals(productId, BathroomMirrorId, StringComparison.Ordinal) ||
            string.Equals(productId, BathroomToiletId, StringComparison.Ordinal) ||
            string.Equals(productId, BathroomVanityId, StringComparison.Ordinal) ||
            string.Equals(productId, BathroomTubId, StringComparison.Ordinal) ||
            string.Equals(productId, BathroomShowerId, StringComparison.Ordinal))
        {
            return "WALL";
        }

        if (string.Equals(productId, KitchenPantryShelfId, StringComparison.Ordinal) ||
            string.Equals(productId, KitchenSinkCabinetId, StringComparison.Ordinal) ||
            string.Equals(productId, KitchenRefrigeratorId, StringComparison.Ordinal) ||
            string.Equals(productId, KitchenStoveOvenId, StringComparison.Ordinal))
        {
            return "WALL";
        }

        if (string.Equals(productId, BedroomDreamArtId, StringComparison.Ordinal) ||
            string.Equals(productId, BedroomNightstandId, StringComparison.Ordinal) ||
            string.Equals(productId, BedroomWardrobeId, StringComparison.Ordinal) ||
            string.Equals(productId, BedroomWindowDaybedId, StringComparison.Ordinal))
        {
            return "WALL";
        }

        if (string.Equals(productId, BalconyHerbShelfId, StringComparison.Ordinal) ||
            string.Equals(productId, BalconyRailingFlowersId, StringComparison.Ordinal) ||
            string.Equals(productId, BalconyLanternStringId, StringComparison.Ordinal) ||
            string.Equals(productId, BalconySunAwningId, StringComparison.Ordinal))
        {
            return "WALL";
        }

        // PatioStringLights is deliberately NOT here: it stands on its own posts.
        // The Patio has no wall to hang a festoon from, only a .63 low wall.
        if (string.Equals(productId, PatioHerbTroughId, StringComparison.Ordinal) ||
            string.Equals(productId, PatioPergolaArchId, StringComparison.Ordinal))
        {
            return "WALL";
        }

        if (string.Equals(productId, LoftWallGalleryId, StringComparison.Ordinal) ||
            string.Equals(productId, LoftTallBookcaseId, StringComparison.Ordinal))
        {
            return "WALL";
        }

        return IsLivingRoomCollectionProduct(productId) ||
               IsBathroomCollectionProduct(productId) ||
               IsKitchenCollectionProduct(productId) ||
               IsBedroomCollectionProduct(productId) ||
               IsGardenCollectionProduct(productId) ||
               IsBalconyCollectionProduct(productId) ||
               IsPatioCollectionProduct(productId) ||
               IsSecondFloorCollectionProduct(productId)
            ? "FLOOR"
            : string.Empty;
    }

    public static string GetCatalogEyebrow(string productId, string fallbackCategory)
    {
        string set = GetRoomDesignSetLabel(productId);
        string placement = GetPlacementFamilyLabel(productId);
        if (!string.IsNullOrEmpty(set) && !string.IsNullOrEmpty(placement))
            return set + "  •  " + placement;
        return fallbackCategory ?? string.Empty;
    }

    public static bool IsBathroomCollectionProduct(string productId)
    {
        return Array.IndexOf(BathroomCollectionInternal, productId) >= 0;
    }

    public static bool IsKitchenCollectionProduct(string productId)
    {
        return Array.IndexOf(KitchenCollectionInternal, productId) >= 0;
    }

    public static bool IsBedroomCollectionProduct(string productId)
    {
        return Array.IndexOf(BedroomCollectionInternal, productId) >= 0;
    }

    public static bool IsGardenCollectionProduct(string productId)
    {
        return Array.IndexOf(GardenCollectionInternal, productId) >= 0;
    }

    public static bool IsBalconyCollectionProduct(string productId)
    {
        return Array.IndexOf(BalconyCollectionInternal, productId) >= 0;
    }

    public static bool IsPatioCollectionProduct(string productId)
    {
        return Array.IndexOf(PatioCollectionInternal, productId) >= 0;
    }

    public static bool IsSecondFloorCollectionProduct(string productId)
    {
        return Array.IndexOf(SecondFloorCollectionInternal, productId) >= 0;
    }

    public static IReadOnlyList<string> GetRoomCollection(string roomId)
    {
        if (string.Equals(roomId, HomeRoomService.BathroomId, StringComparison.Ordinal))
            return BathroomCollectionInternal;
        if (string.Equals(roomId, HomeRoomService.KitchenId, StringComparison.Ordinal))
            return KitchenCollectionInternal;
        if (string.Equals(roomId, HomeRoomService.BedroomId, StringComparison.Ordinal))
            return BedroomCollectionInternal;
        if (string.Equals(roomId, HomeRoomService.GardenId, StringComparison.Ordinal))
            return GardenCollectionInternal;
        if (string.Equals(roomId, HomeRoomService.BalconyId, StringComparison.Ordinal))
            return BalconyCollectionInternal;
        if (string.Equals(roomId, HomeRoomService.PatioId, StringComparison.Ordinal))
            return PatioCollectionInternal;
        if (string.Equals(roomId, HomeRoomService.SecondFloorId, StringComparison.Ordinal))
            return SecondFloorCollectionInternal;
        return LivingRoomCollectionInternal;
    }

    public static bool IsProductInRoomCollection(string roomId, string productId)
    {
        IReadOnlyList<string> collection = GetRoomCollection(roomId);
        for (int i = 0; i < collection.Count; i++)
        {
            if (string.Equals(collection[i], productId, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    public static int GetRoomOwnedCount(string roomId)
    {
        return GetOwnedCount(GetRoomCollection(roomId));
    }

    private static int GetOwnedCount(IReadOnlyList<string> collection)
    {
        int count = 0;
        for (int i = 0; i < collection.Count; i++)
        {
            if (IsOwned(collection[i]))
                count++;
        }
        return count;
    }

    private static long GetRemainingCoinCost(IReadOnlyList<string> collection)
    {
        long total = 0L;
        for (int i = 0; i < collection.Count; i++)
        {
            string productId = collection[i];
            if (IsOwned(productId) ||
                !ProductsById.TryGetValue(productId, out HomeStoreProduct product))
                continue;
            total = product.CoinPrice > long.MaxValue - total
                ? long.MaxValue
                : total + product.CoinPrice;
        }
        return total;
    }

    public static bool IsLivingRoomProductRevealed(string productId)
    {
        return IsLivingRoomCollectionProduct(productId);
    }

    public static string GetRequiredProductId(string productId)
    {
        if(string.Equals(productId,KitchenFruitBasketId,StringComparison.Ordinal))return KitchenIslandId;
        if (string.Equals(productId, BookSetId, StringComparison.Ordinal))
            return BookshelfId;
        if (string.Equals(productId, ModernTelevisionId, StringComparison.Ordinal))
            return TvUnitId;
        if (string.Equals(productId, HomeKitchenPreviewId, StringComparison.Ordinal))
            return HomeBathroomPreviewId;
        if (string.Equals(productId, HomeBedroomPreviewId, StringComparison.Ordinal))
            return HomeKitchenPreviewId;
        if (string.Equals(productId, HomeGardenPreviewId, StringComparison.Ordinal))
            return HomeBedroomPreviewId;
        if (string.Equals(productId, HomeBalconyPreviewId, StringComparison.Ordinal))
            return HomeGardenPreviewId;
        if (string.Equals(productId, HomePatioPreviewId, StringComparison.Ordinal))
            return HomeBalconyPreviewId;
        if (string.Equals(productId, HomeSecondFloorPreviewId, StringComparison.Ordinal))
            return HomePatioPreviewId;
        return null;
    }

    public static bool IsProductDependencyMet(string productId)
    {
        string requiredId = GetRequiredProductId(productId);
        return string.IsNullOrEmpty(requiredId) || IsOwned(requiredId);
    }

    public static int GetPlacementIndex(string productId)
    {
        if (IsFixedRoomProduct(productId)) return 0;
        return !string.IsNullOrWhiteSpace(productId) &&
               PlacementByProductId.TryGetValue(productId, out HomeStorePlacementEntry entry)
            ? Math.Max(0, entry.slotIndex)
            : 0;
    }

    public static bool TryGetWorldPlacement(
        string productId,
        out Vector3 position,
        out float rotationY)
    {
        if (IsFixedRoomProduct(productId)) { position = default; rotationY = 0f; return false; }
        if (!string.IsNullOrWhiteSpace(productId) &&
            PlacementByProductId.TryGetValue(productId, out HomeStorePlacementEntry entry) &&
            entry.hasWorldPosition &&
            IsFinite(entry.positionX) && IsFinite(entry.positionZ) && IsFinite(entry.rotationY))
        {
            position = new Vector3(entry.positionX, 0f, entry.positionZ);
            rotationY = entry.rotationY;
            return true;
        }

        position = default;
        rotationY = 0f;
        return false;
    }

    public static bool TrySetPlacement(string productId, int slotIndex, int slotCount)
    {
        if (IsFixedRoomProduct(productId)) return false;
        if (!IsOwned(productId) || slotCount <= 0 || slotIndex < 0 || slotIndex >= slotCount)
            return false;

        if (GetPlacementIndex(productId) == slotIndex)
            return true;

        PlacementByProductId[productId] = new HomeStorePlacementEntry(productId, slotIndex);
        CatHomeSaveSystem.SaveNow();
        PlacementChanged?.Invoke(productId);
        return true;
    }

    public static bool TrySetPlacement(string productId, Vector3 position, float rotationY)
    {
        if (IsFixedRoomProduct(productId)) return false;
        if (!IsOwned(productId) || !IsFinite(position.x) || !IsFinite(position.z) ||
            !IsFinite(rotationY))
        {
            return false;
        }

        PlacementByProductId[productId] =
            new HomeStorePlacementEntry(productId, position, Mathf.Repeat(rotationY, 360f));
        CatHomeSaveSystem.SaveNow();
        PlacementChanged?.Invoke(productId);
        return true;
    }

    /// <summary>Removes a custom transform so the authored/default placement is used again.</summary>
    public static bool TryClearPlacement(string productId)
    {
        if (IsFixedRoomProduct(productId)) return IsOwned(productId);
        if (!IsOwned(productId))
            return false;
        if (!PlacementByProductId.Remove(productId))
            return true;

        CatHomeSaveSystem.SaveNow();
        PlacementChanged?.Invoke(productId);
        return true;
    }

    public static HomeStorePurchaseResult TryPurchase(string productId)
    {
        return TryPurchase(productId, CurrencyType.Coin);
    }

    public static HomeStorePurchaseResult TryPurchase(
        string productId,
        CurrencyType currency)
    {
        if (!TryGetProduct(productId, out HomeStoreProduct product))
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.UnknownProduct, default, 0L);

        if (IsOwned(productId))
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.AlreadyOwned, product, 0L);

        if (!product.IsAvailable)
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.ComingSoon, product, 0L);

        if (!IsProductDependencyMet(productId))
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.RequiredProductMissing,
                product,
                0L);
        }

        if (product.Id == HomeBathroomPreviewId && !IsLivingRoomComplete)
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.CollectionIncomplete,
                product,
                Math.Max(0, LivingRoomItemCount - LivingRoomOwnedCount));
        }

        if (product.Id == HomeKitchenPreviewId && !IsBathroomComplete)
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.CollectionIncomplete,
                product,
                Math.Max(0, BathroomItemCount - BathroomOwnedCount));
        }

        if (product.Id == HomeBedroomPreviewId && !IsKitchenComplete)
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.CollectionIncomplete,
                product,
                Math.Max(0, KitchenItemCount - KitchenOwnedCount));
        }

        if (product.Id == HomeGardenPreviewId && !IsBedroomComplete)
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.CollectionIncomplete,
                product,
                Math.Max(0, BedroomItemCount - BedroomOwnedCount));
        }

        if (product.Id == HomeBalconyPreviewId && !IsGardenComplete)
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.CollectionIncomplete,
                product,
                Math.Max(0, GardenItemCount - GardenOwnedCount));
        }

        if (product.Id == HomePatioPreviewId && !IsBalconyComplete)
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.CollectionIncomplete,
                product,
                Math.Max(0, BalconyItemCount - BalconyOwnedCount));
        }

        if (product.Id == HomeSecondFloorPreviewId && !IsPatioComplete)
        {
            return new HomeStorePurchaseResult(
                HomeStorePurchaseStatus.CollectionIncomplete,
                product,
                Math.Max(0, PatioItemCount - PatioOwnedCount));
        }

        if (!MeetsHomeLevelRequirement(product))
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.LevelLocked, product, 0L);

        if (!product.SupportsCoins && !product.SupportsDiamonds)
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.ComingSoon, product, 0L);

        long price;
        HomeStorePurchaseStatus insufficientStatus;
        switch (currency)
        {
            case CurrencyType.Coin when product.SupportsCoins:
                price = product.CoinPrice;
                insufficientStatus = HomeStorePurchaseStatus.InsufficientCoins;
                break;
            case CurrencyType.Diamond when product.SupportsDiamonds:
                price = product.DiamondPrice;
                insufficientStatus = HomeStorePurchaseStatus.InsufficientDiamonds;
                break;
            default:
                return new HomeStorePurchaseResult(HomeStorePurchaseStatus.Rejected, product, 0L);
        }

        PurchasePreview preview = EconomyService.PreviewPurchase(currency, price);
        if (!preview.CanAfford)
        {
            return new HomeStorePurchaseResult(
                insufficientStatus,
                product,
                preview.Missing,
                currency);
        }

        // Defer the economy write until ownership is also updated. SaveNow then
        // captures both halves of the purchase in one save snapshot.
        EconomyTransactionResult spend = EconomyService.TrySpend(
            currency,
            price,
            EconomySource.Shop,
            null,
            EconomyPersistence.DeferToCaller);

        if (!spend.Succeeded)
        {
            HomeStorePurchaseStatus status =
                spend.Status == EconomyTransactionStatus.InsufficientFunds
                    ? insufficientStatus
                    : HomeStorePurchaseStatus.Rejected;
            long missing = Math.Max(0L, price - EconomyService.GetBalance(currency));
            return new HomeStorePurchaseResult(status, product, missing, currency);
        }

        OwnedProductIds.Add(productId);
        if (product.IsPlaceable && !IsFixedRoomProduct(productId))
            StoredProductIds.Add(productId);
        // Ownership immediately reveals the product at its authored location.
        // Home XP is earned before the save so the grant and the ownership change
        // are captured in the same file write.
        HomeProgressionService.GrantHomeXp(product.CoinPrice, productId);
        ProgressionService.RecordProgress(QuestType.BuyStoreItem);
        CatHomeSaveSystem.SaveNow();
        OwnershipChanged?.Invoke(productId);
        CollectionMilestoneService.HandleOwned(productId);
        return new HomeStorePurchaseResult(HomeStorePurchaseStatus.Purchased, product, 0L);
    }

    /// <summary>
    /// QA-only acquisition path used while EconomyChecksEnabled is false. It
    /// never reads or spends a balance, and acquires placement prerequisites so
    /// a single catalog click can immediately reveal the product in its room.
    /// </summary>
    public static HomeStorePurchaseResult TryAcquireForTesting(string productId)
    {
        return TryAcquireForTesting(productId, true);
    }

    private static HomeStorePurchaseResult TryAcquireForTesting(
        string productId,
        bool recordQuest)
    {
        if (!TryGetProduct(productId, out HomeStoreProduct product))
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.UnknownProduct, default, 0L);

        if (IsOwned(productId))
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.AlreadyOwned, product, 0L);

        if (!product.IsAvailable)
            return new HomeStorePurchaseResult(HomeStorePurchaseStatus.ComingSoon, product, 0L);

        string requiredId = GetRequiredProductId(productId);
        if (!string.IsNullOrEmpty(requiredId) && !IsOwned(requiredId))
        {
            HomeStorePurchaseResult required = TryAcquireForTesting(requiredId, false);
            if (!required.Succeeded && required.Status != HomeStorePurchaseStatus.AlreadyOwned)
            {
                return new HomeStorePurchaseResult(
                    HomeStorePurchaseStatus.RequiredProductMissing,
                    product,
                    0L);
            }
        }

        OwnedProductIds.Add(productId);
        if (product.IsPlaceable && !IsFixedRoomProduct(productId))
            StoredProductIds.Add(productId);
        HomeProgressionService.GrantHomeXp(product.CoinPrice, productId);
        if (recordQuest)
            ProgressionService.RecordProgress(QuestType.BuyStoreItem);
        CatHomeSaveSystem.SaveNow();
        OwnershipChanged?.Invoke(productId);
        CollectionMilestoneService.HandleOwned(productId);
        return new HomeStorePurchaseResult(HomeStorePurchaseStatus.Purchased, product, 0L);
    }

    public static HomeStoreSaveState CaptureState()
    {
        string[] owned = new string[OwnedProductIds.Count];
        OwnedProductIds.CopyTo(owned);
        Array.Sort(owned, StringComparer.Ordinal);
        string[] stored = new string[StoredProductIds.Count];
        StoredProductIds.CopyTo(stored);
        Array.Sort(stored, StringComparer.Ordinal);
        var placements = new List<HomeStorePlacementEntry>(PlacementByProductId.Count);
        foreach (KeyValuePair<string, HomeStorePlacementEntry> pair in PlacementByProductId)
        {
            if (OwnedProductIds.Contains(pair.Key) && pair.Value != null)
                placements.Add(pair.Value.Clone());
        }
        placements.Sort((a, b) => string.CompareOrdinal(a.productId, b.productId));

        return new HomeStoreSaveState
        {
            storeVersion = SaveVersion,
            ownedProductIds = owned,
            storedProductIds = stored,
            placements = placements.ToArray(),
            currentRoomId = HomeRoomService.CurrentRoomId
        };
    }

    public static void ApplySavedState(HomeStoreSaveState state)
    {
        OwnedProductIds.Clear();
        StoredProductIds.Clear();
        PlacementByProductId.Clear();
        if (state?.ownedProductIds != null)
        {
            for (int i = 0; i < state.ownedProductIds.Length; i++)
            {
                string id = state.ownedProductIds[i];
                if (ProductsById.ContainsKey(id))
                    OwnedProductIds.Add(id);
            }
        }

        // Store v7 deliberately ignores legacy ROOM storage and drag coordinates,
        // including those arriving from an older cloud/device save. Ownership,
        // currency and progression are untouched; the scene owns the layout.

        if (state?.storedProductIds != null)
        {
            for (int i = 0; i < state.storedProductIds.Length; i++)
            {
                string id = state.storedProductIds[i];
                if (!IsFixedRoomProduct(id) && OwnedProductIds.Contains(id) &&
                    TryGetProduct(id, out HomeStoreProduct product) &&
                    product.IsPlaceable)
                {
                    StoredProductIds.Add(id);
                }
            }
        }

        if (state?.placements != null)
        {
            for (int i = 0; i < state.placements.Length; i++)
            {
                HomeStorePlacementEntry entry = state.placements[i];
                if (entry != null && !IsFixedRoomProduct(entry.productId) && entry.slotIndex >= 0 &&
                    OwnedProductIds.Contains(entry.productId))
                {
                    if (!entry.hasWorldPosition ||
                        (IsFinite(entry.positionX) && IsFinite(entry.positionZ) &&
                         IsFinite(entry.rotationY)))
                    {
                        HomeStorePlacementEntry migrated = entry.Clone();
                        if (state.storeVersion < 4 &&
                            string.Equals(
                                migrated.productId,
                                BookSetId,
                                StringComparison.Ordinal))
                        {
                            // v3 stored the bookshelf's world yaw. v4 stores the
                            // book set's local angle relative to that bookshelf.
                            migrated.rotationY = 0f;
                        }
                        PlacementByProductId[entry.productId] = migrated;
                    }
                }
            }
        }

        // Stable catalog order makes old/cloud saves with too many visible toys
        // converge. Keep every ownership and saved pose; excess goes to collection.
        int displayedCats = 0; bool hasBed = false;
        foreach (var product in ProductsInternal)
        {
            if (!CatCollectionPolicy.IsCatItem(product.Id) || !IsOwned(product.Id) || IsStored(product.Id)) continue;
            bool bed = CatCollectionPolicy.IsBed(product.Id);
            if (displayedCats >= CatCollectionPolicy.Capacity || (bed && hasBed))
                StoredProductIds.Add(product.Id);
            else { displayedCats++; hasBed |= bed; }
        }

        // Apply only after ownership has been restored so Bathroom access can
        // be checked against the same canonical save section. Pre-v5 saves have
        // a null id and therefore migrate to the Living Room without a grant.
        HomeRoomService.ApplySavedRoomId(state?.currentRoomId);

        OwnershipChanged?.Invoke(string.Empty);
        PlacementChanged?.Invoke(string.Empty);
        StorageChanged?.Invoke(string.Empty);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        OwnedProductIds.Clear();
        StoredProductIds.Clear();
        PlacementByProductId.Clear();
        OwnershipChanged = null;
        PlacementChanged = null;
        StorageChanged = null;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
