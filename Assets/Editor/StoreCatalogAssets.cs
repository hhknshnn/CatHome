using System;
using UnityEngine;

/// <summary>
/// Editor-side asset map for every real, placeable store product. Runtime
/// economy data stays in HomeStoreService; this map owns source art, preview
/// output and room-placement geometry.
/// </summary>
public static class StoreCatalogAssets
{
    public const string IconFolder = "Assets/Art/StoreProducts/Icons";

    private const string CustomModelFolder = "Assets/Art/StoreProducts/Models/";
    private const string GeneratedPrefabFolder = "Assets/Art/StoreProducts/Prefabs/";
    private const string PetPrefabFolder =
        "Assets/Bublisher/Small Kit 3D Stylized Petshop Asset/Asset/Prefabs/";
    private const string RoomPrefabFolder = "Assets/LowPolyLivingRoomPack/Prefabs/";

    public static readonly StoreCatalogAsset[] PlaceableProducts =
    {
        Custom(HomeStoreService.CozyPodBedId, "CozyPodBed", new Vector2(1.2f, .95f), .6f,
            new Vector3(-2.82f, 0f, 1.58f)),
        Custom(HomeStoreService.ToyMouseId, "ToyMouse", new Vector2(.44f, .78f), .34f,
            new Vector3(-1.82f, 0f, -2.08f)),
        Custom(HomeStoreService.PlayTunnelId, "PlayTunnel", new Vector2(.58f, .76f), .60f,
            new Vector3(2.55f, 0f, -.55f)),
        RoomAt(HomeStoreService.TvUnitId, "TvUnit", "Nightstand_1.prefab",
            new Vector2(2.20f, .54f), .53f, 4f, new Vector3(-3.30f, 0f, -.60f),
            90f, HomeProductPlacementKind.WallEdge),
        RoomAt(HomeStoreService.ModernTelevisionId, "ModernTelevision", "TV_Modern.prefab",
            new Vector2(1.35f, .16f), .91f, 4f, new Vector3(-3.30f, 0f, -.60f),
            90f, HomeProductPlacementKind.ProductSurfaceOnly),
        Generated(HomeStoreService.GameConsoleId, "GameConsoleSet",
            new Vector2(.46f, .32f), .30f, new Vector3(-3.30f, 0f, -.60f),
            90f, HomeProductPlacementKind.Floor),
        RoomAt(HomeStoreService.FloorLampId, "FloorLamp", "Lamp_Tall.prefab",
            new Vector2(.52f, .52f), 1.40f, 3f, LivingProgressionComposition.LampPosition),
        Custom(HomeStoreService.SideTableId, "SideTable", new Vector2(1.02f, 1.02f), .78f,
            new Vector3(2.92f, 0f, 1.18f)),

        RoomAt(HomeStoreService.BookshelfId, "TallBookshelf", "Bookshelf_Tall.prefab",
            new Vector2(1.70f, .40f), 1.96f, 2.5f, LivingRoomReferenceLayout.BookshelfPosition,
            180f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BookSetId, "ColorfulBookSet",
            new Vector2(.20f, .20f), .26f, LivingRoomReferenceLayout.BookshelfPosition,
            180f, HomeProductPlacementKind.BookshelfOnly),
        RoomAt(HomeStoreService.TallPlantId, "TallHouseplant", "PottedPlant_Tall_1.prefab",
            new Vector2(.84f, .84f), 1.40f, 2.6f, LivingRoomGazeLayoutBuilder.TallPlantPosition),
        RoomAt(HomeStoreService.ModernPaintingId, "ModernPainting", "Painting_Modern_1.prefab",
            new Vector2(1.35f, .2f), .86f, 4f, new Vector3(1.65f, 0f, 2.69f),
            180f, HomeProductPlacementKind.WallEdge, new Vector3(0f, 1.55f, 0f)),
        Custom(HomeStoreService.WallClockId, "RoundWallClock", new Vector2(.8f, .2f), .85f,
            new Vector3(3.55f, 0f, .8f), 270f, HomeProductPlacementKind.WallEdge,
            new Vector3(0f, 1.35f, 0f)),

        Generated(HomeStoreService.BathroomBathMatId, "BathroomBathMat",
            new Vector2(1.9f, 1.15f), .08f, new Vector3(1.2f, 0f, .35f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomLaundryHamperId, "BathroomLaundryHamper",
            new Vector2(.82f, .82f), .92f, new Vector3(-2.9f, 0f, -1.05f),
            12f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomLitterBoxId, "BathroomLitterBox",
            new Vector2(1.3f, 1.02f), .48f, new Vector3(2.9f, 0f, -1.72f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomGroomingCartId, "BathroomGroomingCart",
            new Vector2(.92f, .64f), 1.02f, new Vector3(-1.55f, 0f, -1.85f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomTowelStorageId, "BathroomTowelStorage",
            new Vector2(1.02f, .5f), 1.82f, new Vector3(-3.05f, 0f, .25f),
            270f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BathroomMirrorId, "BathroomWallMirror",
            new Vector2(.92f, .16f), .78f, new Vector3(-3.28f, 0f, 1.55f),
            270f, HomeProductPlacementKind.WallEdge, 1.58f),
        Generated(HomeStoreService.BathroomToiletId, "BathroomToilet",
            new Vector2(.94f, 1.18f), 1.56f, new Vector3(3.05f, 0f, -.25f),
            270f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BathroomVanityId, "BathroomVanitySink",
            new Vector2(1.96f, .86f), 1.69f, new Vector3(-2.55f, 0f, 2.05f),
            180f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BathroomTubId, "BathroomTub",
            new Vector2(2.45f, 1.31f), 1.36f, new Vector3(.9f, 0f, 1.86f),
            180f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BathroomShowerId, "BathroomShower",
            new Vector2(1.65f, 1.28f), 2.21f, new Vector3(2.95f, 0f, 1.82f),
            180f, HomeProductPlacementKind.WallEdge),

        Generated(HomeStoreService.KitchenPawMatId, "KitchenPawMat",
            new Vector2(2.2f, 1.15f), .08f, new Vector3(0f, 0f, -2.25f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.KitchenFruitBasketId, "KitchenFruitBasket",
            new Vector2(.82f, .82f), .72f, KitchenScatterBuilder.BasketPosition,
            0f, HomeProductPlacementKind.ProductSurfaceOnly),
        Generated(HomeStoreService.KitchenFeedingStationId, "KitchenFeedingStation",
            new Vector2(1.35f, .72f), .48f, new Vector3(3f, 0f, -1.95f),
            348f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.KitchenCounterStoolId, "KitchenCounterStool",
            new Vector2(.72f, .66f), .82f, new Vector3(1.85f, 0f, -1.28f),
            330f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.KitchenPantryShelfId, "KitchenPantryShelf",
            new Vector2(1.15f, .55f), 1.85f, new Vector3(-3.45f, 0f, .95f),
            270f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.KitchenDishCartId, "KitchenDishCart",
            new Vector2(.92f, .62f), 1.15f, new Vector3(-1.78f, 0f, -.35f),
            8f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.KitchenSinkCabinetId, "KitchenSinkCabinet",
            new Vector2(1.65f, .75f), 1.16f, new Vector3(-2.25f, 0f, 2.42f),
            0f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.KitchenRefrigeratorId, "KitchenRefrigerator",
            new Vector2(1.25f, .78f), 2.25f, new Vector3(3.17f, 0f, 1.55f),
            0f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.KitchenStoveOvenId, "KitchenStoveOven",
            new Vector2(1.18f, .74f), 1.3f, new Vector3(-.42f, 0f, 2.43f),
            0f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.KitchenIslandId, "KitchenIsland",
            new Vector2(2.25f, 1.05f), 1.08f, new Vector3(.35f, 0f, .45f),
            0f, HomeProductPlacementKind.Floor),

        Generated(HomeStoreService.BedroomPawRugId, "BedroomPawRug",
            new Vector2(2.2f, 1.28f), .08f, new Vector3(0f, 0f, -.35f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BedroomNightLightId, "BedroomNightLight",
            new Vector2(.48f, .48f), .85f, new Vector3(3.12f, 0f, -1.92f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BedroomDreamArtId, "BedroomDreamArt",
            new Vector2(1.05f, .16f), .78f, new Vector3(-3.28f, 0f, .35f),
            90f, HomeProductPlacementKind.WallEdge, 1.62f),
        Generated(HomeStoreService.BedroomYarnBasketId, "BedroomYarnBasket",
            new Vector2(.72f, .72f), .55f, new Vector3(2.88f, 0f, 1.68f),
            18f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BedroomNightstandId, "BedroomNightstand",
            new Vector2(.7f, .52f), .72f, new Vector3(-3.22f, 0f, 1.82f),
            90f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BedroomVanityStoolId, "BedroomVanityStool",
            new Vector2(.62f, .62f), .48f, new Vector3(-1.72f, 0f, -1.88f),
            12f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BedroomWardrobeId, "BedroomWardrobe",
            new Vector2(1.15f, .58f), 2.15f, new Vector3(3.2f, 0f, .55f),
            270f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BedroomWindowDaybedId, "BedroomWindowDaybed",
            new Vector2(2f, .82f), .72f, new Vector3(-2.35f, 0f, 2.32f),
            180f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BedroomStarCanopyId, "BedroomStarCanopy",
            new Vector2(1.12f, 1.05f), 1.55f, new Vector3(1.52f, 0f, -1.52f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BedroomQueenBedId, "BedroomQueenBed",
            new Vector2(2.32f, 1.42f), 1.05f, new Vector3(.2f, 0f, 1.42f),
            0f, HomeProductPlacementKind.Floor),

        Generated(HomeStoreService.GardenYarnBallId, "GardenYarnBall",
            new Vector2(.45f, .45f), .28f, new Vector3(1.85f, 0f, -2.05f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenFlowerPotsId, "GardenFlowerPots",
            new Vector2(.85f, .7f), .62f, new Vector3(-2.85f, 0f, 1.55f),
            12f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenDaisyBedId, "GardenDaisyBed",
            new Vector2(1.1f, .7f), .42f, new Vector3(2.55f, 0f, 1.65f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenSaplingId, "GardenSapling",
            new Vector2(.85f, .85f), 1.85f, new Vector3(-2.95f, 0f, -1.65f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenBirdBathId, "GardenBirdBath",
            new Vector2(.7f, .7f), .82f, new Vector3(.15f, 0f, 1.95f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenSunLoungerId, "GardenSunLounger",
            new Vector2(1.4f, .55f), .48f, new Vector3(2.55f, 0f, -.85f),
            90f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenBistroSetId, "GardenBistroSet",
            new Vector2(1.35f, 1.15f), .78f, new Vector3(-1.55f, 0f, 1.75f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenGrillId, "GardenGrill",
            new Vector2(.85f, .7f), 1.05f, new Vector3(2.65f, 0f, .55f),
            270f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenHammockId, "GardenHammock",
            new Vector2(1.15f, .7f), .85f, new Vector3(-2.75f, 0f, .15f),
            90f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.GardenPergolaId, "GardenPergola",
            new Vector2(2f, 1.5f), 1.85f, new Vector3(.1f, 0f, -.15f),
            0f, HomeProductPlacementKind.Floor),

        Generated(HomeStoreService.BalconySunMatId, "BalconySunMat",
            new Vector2(1.9f, 1.15f), .08f, new Vector3(.2f, 0f, .2f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BalconyPlanterBoxId, "BalconyPlanterBox",
            new Vector2(.85f, .55f), .5f, new Vector3(2.9f, 0f, 1.6f),
            12f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BalconyHerbShelfId, "BalconyHerbShelf",
            new Vector2(1f, .5f), 1.4f, new Vector3(-3.22f, 0f, .9f),
            90f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BalconyRailingFlowersId, "BalconyRailingFlowers",
            new Vector2(1.6f, .35f), .9f, new Vector3(-1.4f, 0f, 2.5f),
            180f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BalconyBirdFeederId, "BalconyBirdFeeder",
            new Vector2(.6f, .6f), 1.4f, new Vector3(2.9f, 0f, -.4f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BalconyLanternStringId, "BalconyLanternString",
            new Vector2(1.6f, .3f), .3f, new Vector3(1.4f, 0f, 2.5f),
            180f, HomeProductPlacementKind.WallEdge, 1.92f),
        Generated(HomeStoreService.BalconyCushionBenchId, "BalconyCushionBench",
            new Vector2(1.6f, .6f), .55f, new Vector3(2.5f, 0f, -1.6f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BalconySideTableId, "BalconySideTable",
            new Vector2(.7f, .7f), .5f, new Vector3(1.4f, 0f, -1.4f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BalconyHangingChairId, "BalconyHangingChair",
            new Vector2(.9f, .9f), 1.5f, new Vector3(-2.7f, 0f, 1.6f),
            0f, HomeProductPlacementKind.Floor),
        // Hangs above the balcony door: its frame tops out at 2.2 and the wall at 3.
        Generated(HomeStoreService.BalconySunAwningId, "BalconySunAwning",
            new Vector2(2.4f, 1.1f), .71f, new Vector3(0f, 0f, 2.2f),
            180f, HomeProductPlacementKind.WallEdge, 2.24f),

        Generated(HomeStoreService.PatioStoneRugId, "PatioStoneRug",
            new Vector2(2.0f, 1.2f), .08f, new Vector3(0f, 0f, .2f),
            0f, HomeProductPlacementKind.Floor),
        // Right edge, but in FRONT of the swing. At z 1.5 the pot overlapped the
        // swing footprint (z .65 to 1.35) and the shell's right corner planter
        // (x 2.7 to 3.3, z 1.7 to 2.3), and the swing frame hid it from the room
        // camera outright. The right-edge lane behind the swing is only .35 deep
        // and the pot needs .75, so it moved forward instead of sideways: the
        // -X approach the room's second orientation rule depends on is kept.
        Generated(HomeStoreService.PatioPottedFernsId, "PatioPottedFerns",
            new Vector2(.8f, .6f), 1.1f, new Vector3(2.95f, 0f, -1.7f),
            12f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.PatioHerbTroughId, "PatioHerbTrough",
            new Vector2(1.6f, .35f), .5f, new Vector3(-1.2f, 0f, 2.5f),
            180f, HomeProductPlacementKind.WallEdge),
        // Stands on its own two posts; it used to hang at 1.95 off a wall the
        // Patio does not have. The back boundary here is a .63 low wall and a
        // 1.00 hedge, and the only tall thing back there is PatioPergolaArch —
        // a separate purchase, so the lights floated for anyone who owned them
        // without it. The free back pocket is x 1.2 to 2.7 between the arch and
        // the right corner planter, which is what sets the 1.40 footprint.
        Generated(HomeStoreService.PatioStringLightsId, "PatioStringLights",
            new Vector2(1.4f, .3f), 2.1f, new Vector3(1.95f, 0f, 2.45f),
            180f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.PatioWaterFountainId, "PatioWaterFountain",
            new Vector2(.8f, .8f), .9f, new Vector3(2.9f, 0f, -.5f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.PatioFirePitId, "PatioFirePit",
            new Vector2(.9f, .9f), .8f, new Vector3(-2.7f, 0f, 1.5f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.PatioDiningSetId, "PatioDiningSet",
            new Vector2(1.7f, 1.5f), .58f, new Vector3(0.6f, 0f, -1.5f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.PatioParasolId, "PatioParasol",
            new Vector2(2.2f, 2.2f), 1.85f, new Vector3(0.6f, 0f, -0.9f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.PatioPorchSwingId, "PatioPorchSwing",
            new Vector2(1.6f, .7f), 1.5f, new Vector3(2.4f, 0f, 1.0f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.PatioPergolaArchId, "PatioPergolaArch",
            new Vector2(2.4f, 1.0f), 1.85f, new Vector3(0f, 0f, 2.2f),
            180f, HomeProductPlacementKind.WallEdge),

        Generated(HomeStoreService.LoftFloorRunnerId, "LoftFloorRunner",
            new Vector2(1.9f, 1.15f), .08f, new Vector3(.2f, 0f, .2f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.LoftFloorCushionsId, "LoftFloorCushions",
            new Vector2(.9f, .9f), .5f, new Vector3(2.9f, 0f, 1.6f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.LoftBookStackId, "LoftBookStack",
            new Vector2(.6f, .5f), .6f, new Vector3(2.9f, 0f, -.4f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.LoftArcLampId, "LoftArcLamp",
            new Vector2(.7f, .7f), 1.85f, new Vector3(-3.2f, 0f, 1.6f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.LoftBeanBagId, "LoftBeanBag",
            new Vector2(.95f, .95f), .55f, new Vector3(-1.6f, 0f, 1.7f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.LoftRecordPlayerId, "LoftRecordPlayer",
            new Vector2(.9f, .6f), .7f, new Vector3(-1.7f, 0f, -1.4f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.LoftStudyDeskId, "LoftStudyDesk",
            new Vector2(1.4f, .9f), .8f, new Vector3(2.4f, 0f, -1.6f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.LoftWallGalleryId, "LoftWallGallery",
            new Vector2(1.6f, .2f), .9f, new Vector3(-1.4f, 0f, 2.5f),
            180f, HomeProductPlacementKind.WallEdge, 1.55f),
        Generated(HomeStoreService.LoftTallBookcaseId, "LoftTallBookcase",
            new Vector2(1.4f, .4f), 2.1f, new Vector3(1.4f, 0f, 2.55f),
            180f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.LoftChaiseLoungeId, "LoftChaiseLounge",
            new Vector2(1.6f, .7f), .7f, new Vector3(1.4f, 0f, 1.2f),
            0f, HomeProductPlacementKind.Floor),

        Pet(HomeStoreService.CeramicBowlId, "CeramicBowl", "Bowl1 V3.prefab",
            new Vector2(.50f, .42f), .16f, 1.4f),
        Pet(HomeStoreService.CloudBedId, "CloudBed", "Bed5 V3.prefab",
            new Vector2(1.15f, .85f), .48f, 1.4f),
        Pet(HomeStoreService.CanopyBedId, "CanopyBed", "Bed3 V1.prefab",
            new Vector2(1.2f, 1f), .75f, 1.35f),
        Pet(HomeStoreService.TreatJarId, "TreatJar", "Food3 V3.prefab",
            new Vector2(.38f, .38f), .48f, 1.8f),
        Pet(HomeStoreService.FeatherToyId, "FeatherToy", "Toy5 V3.prefab",
            new Vector2(.42f, .42f), .65f, 2.2f),
        Pet(HomeStoreService.CollarId, "ClassicCollar", "Collar1 V3.prefab",
            new Vector2(.48f, .44f), .16f, 1.7f),
        Pet(HomeStoreService.LeashId, "WalkingLeash", "Leash2 V2.prefab",
            new Vector2(.8f, .62f), .15f, 1.7f),
        PetAt(HomeStoreService.BellCollarId, "BellCollar", "Collar2 V2.prefab",
            new Vector2(.48f, .44f), .16f, 1.7f, new Vector3(2.15f, 0f, -2.15f)),
        PetAt(HomeStoreService.KibbleBagId, "KibbleBag", "Food2 V1.prefab",
            new Vector2(.50f, .44f), .44f, 1.35f, new Vector3(-2.55f, 0f, -.35f)),
        RoomAt(HomeStoreService.CatnipPlantId, "CatnipPlanter", "PottedPlant_Small_1.prefab",
            new Vector2(.52f, .52f), .44f, 3.2f, new Vector3(1.72f, 0f, 1.88f)),
        RoomAt(HomeStoreService.CardboardHideoutId, "CardboardHideout", "Box_Open.prefab",
            new Vector2(.78f, .78f), .48f, 1.15f, new Vector3(2.38f, 0f, 1.72f)),

        RoomAt(HomeStoreService.ArmchairId, "ClassicArmchair", "Armchair_Classic.prefab",
            new Vector2(1.05f, .79f), .84f, 4f, LivingProgressionComposition.ChairPosition,325f),
        Room(HomeStoreService.SmallPlantId, "TablePlant", "PottedPlant_Small_2.prefab",
            new Vector2(.72f, .76f), .58f, 4f),
        Room(HomeStoreService.MirrorId, "WallMirror", "Mirror.prefab",
            new Vector2(1f, .2f), 1.12f, 4f, HomeProductPlacementKind.WallEdge,
            new Vector3(0f, 1.3f, 0f)),
        Generated(HomeStoreService.StereoId, "SpeakerSystem",
            new Vector2(2.14f, .25f), .84f, new Vector3(-3.30f, 0f, -.60f),
            90f, HomeProductPlacementKind.Floor),
        Room(HomeStoreService.RetroTvId, "RetroTelevision", "TV_Retro.prefab",
            new Vector2(1.05f, .68f), 1.18f, 4f, HomeProductPlacementKind.WallEdge),
        Room(HomeStoreService.StoolId, "AccentStool", "Stool_3.prefab",
            new Vector2(.72f, .66f), .72f, 7f)
    };

    public static bool TryGet(string productId, out StoreCatalogAsset definition)
    {
        for (int i = 0; i < PlaceableProducts.Length; i++)
        {
            if (!string.Equals(PlaceableProducts[i].ProductId, productId, StringComparison.Ordinal))
                continue;
            definition = PlaceableProducts[i];
            return true;
        }
        definition = default;
        return false;
    }

    public static string GetIconPath(string productId)
    {
        if (TryGet(productId, out StoreCatalogAsset asset))
            return asset.IconPath;

        switch (productId)
        {
            case HomeStoreService.BallBasketId:
                return IconFolder + "/BallBasket_Icon.png";
            case HomeStoreService.ScratchPostId:
                return IconFolder + "/ScratchPost_Icon.png";
            case HomeStoreService.HomeRoomsPreviewId:
                return "Assets/Art/RoomPreviews/LivingRoomPreview.png";
            case HomeStoreService.HomeBathroomPreviewId:
                return "Assets/Art/RoomPreviews/BathroomPreview.png";
            case HomeStoreService.HomeKitchenPreviewId:
                return "Assets/Art/RoomPreviews/KitchenPreview.png";
            case HomeStoreService.HomeBedroomPreviewId:
                return "Assets/Art/RoomPreviews/BedroomPreview.png";
            case HomeStoreService.HomeGardenPreviewId:
                return "Assets/Art/RoomPreviews/GardenPreview.png";
            case HomeStoreService.HomeBalconyPreviewId:
                return "Assets/Art/RoomPreviews/BalconyPreview.png";
            case HomeStoreService.HomePatioPreviewId:
                return "Assets/Art/RoomPreviews/PatioPreview.png";
            case HomeStoreService.HomeSecondFloorPreviewId:
                return "Assets/Art/RoomPreviews/SecondFloorPreview.png";
            default:
                return null;
        }
    }

    private static StoreCatalogAsset Custom(
        string id, string name, Vector2 footprint, float height, Vector3 position,
        float yaw = 0f, HomeProductPlacementKind kind = HomeProductPlacementKind.Floor,
        Vector3 visualOffset = default)
    {
        return new StoreCatalogAsset(id, name, CustomModelFolder + name + ".fbx",
            footprint, height, position, yaw, kind, 1f, visualOffset, true);
    }

    private static StoreCatalogAsset Pet(
        string id, string name, string source, Vector2 footprint, float height, float scale)
    {
        return new StoreCatalogAsset(id, name, PetPrefabFolder + source, footprint, height,
            DefaultPosition(id), 0f, HomeProductPlacementKind.Floor, scale, Vector3.zero, true);
    }

    private static StoreCatalogAsset PetAt(
        string id, string name, string source, Vector2 footprint, float height, float scale,
        Vector3 position)
    {
        return new StoreCatalogAsset(id, name, PetPrefabFolder + source, footprint, height,
            position, 0f, HomeProductPlacementKind.Floor, scale, Vector3.zero, true);
    }

    private static StoreCatalogAsset Room(
        string id, string name, string source, Vector2 footprint, float height, float scale,
        HomeProductPlacementKind kind = HomeProductPlacementKind.Floor,
        Vector3 visualOffset = default)
    {
        return new StoreCatalogAsset(id, name, RoomPrefabFolder + source, footprint, height,
            DefaultPosition(id), 0f, kind, scale, visualOffset, true);
    }

    private static StoreCatalogAsset RoomAt(
        string id, string name, string source, Vector2 footprint, float height, float scale,
        Vector3 position, float yaw = 0f,
        HomeProductPlacementKind kind = HomeProductPlacementKind.Floor,
        Vector3 visualOffset = default)
    {
        return new StoreCatalogAsset(id, name, RoomPrefabFolder + source, footprint, height,
            position, yaw, kind, scale, visualOffset, true);
    }

    private static StoreCatalogAsset Generated(
        string id, string name, Vector2 footprint, float height,
        Vector3 position, float yaw, HomeProductPlacementKind kind,
        float hungHeight = 0f)
    {
        return new StoreCatalogAsset(
            id,
            name,
            GeneratedPrefabFolder + name + ".prefab",
            footprint,
            height,
            position,
            yaw,
            kind,
            1f,
            Vector3.zero,
            true,
            hungHeight);
    }

    // CAT collection has an authored play area; hash positions previously overlapped
    // the bookcase, side table, lamps and other toys when the collection was complete.
    public static bool TryGetCatPose(string id,out Vector3 position,out float yaw)
    {
        yaw=0;position=Vector3.zero;
        switch(id)
        {
            case "home.ball-basket":position=new Vector3(-2.95f,0,-2.1f);yaw=270;break;
            case "home.scratch-post":position=new Vector3(3.25f,0,-.55f);yaw=90;break;
            case "cat.cozy-pod-bed":position=new Vector3(-1.9f,0,1.65f);break;
            case "cat.cloud-bed":position=new Vector3(1.55f,0,1.7f);break;
            case "cat.canopy-bed":position=new Vector3(1.65f,0,-.35f);yaw=90;break;
            case "cat.play-tunnel":position=new Vector3(-2.3f,0,-.4f);break;
            case "cat.cardboard-hideout":position=new Vector3(1.35f,0,-2.2f);yaw=180;break;
            case "cat.nap-pillow":position=new Vector3(-1.55f,0,-2.15f);yaw=180;break;
            case "cat.toy-mouse":position=new Vector3(-.7f,0,-1.1f);break;
            case "cat.collar":position=new Vector3(.10f,0,-1.15f);break;
            case "cat.treat-jar":position=new Vector3(-.9f,0,.7f);yaw=180;break;
            case "cat.kibble-bag":position=new Vector3(-1.4f,0,-.15f);break;
            case "cat.ceramic-bowl":position=new Vector3(-2.2f,0,-1.95f);yaw=270;break;
            case "cat.catnip-plant":position=new Vector3(3.15f,0,-1.2f);yaw=90;break;
            case "cat.feather-toy":position=new Vector3(.8f,0,.65f);yaw=270;break;
            case "cat.leash":position=new Vector3(-.1f,0,-2.25f);yaw=180;break;
            case "cat.bell-collar":position=new Vector3(2.15f,0,-1.45f);yaw=90;break;
            default:return false;
        }
        return true;
    }
    private static Vector3 DefaultPosition(string id)
    {
        int hash = 17;
        for (int i = 0; i < id.Length; i++)
            hash = unchecked(hash * 31 + id[i]);
        hash &= 0x7fffffff;
        float x = -2.8f + (hash % 5) * 1.4f;
        float z = -2.1f + ((hash / 5) % 4) * 1.25f;
        return new Vector3(x, 0f, z);
    }
}

public readonly struct StoreCatalogAsset
{
    public StoreCatalogAsset(
        string productId, string prefabName, string sourceAssetPath,
        Vector2 footprint, float height, Vector3 defaultPosition, float defaultYaw,
        HomeProductPlacementKind placementKind, float visualScale,
        Vector3 visualOffset, bool generatePreview, float hungHeight = 0f, bool useRoomLayout = true)
    {
        ProductId = productId;
        PrefabName = prefabName;
        SourceAssetPath = sourceAssetPath;
        authoredFootprint = footprint;
        authoredHeight = height;
        authoredPosition = StoreCatalogAssets.TryGetCatPose(productId,out var catPosition,out var catYaw) ? catPosition : defaultPosition;
        authoredYaw = StoreCatalogAssets.TryGetCatPose(productId,out _,out catYaw) ? catYaw : defaultYaw;
        this.useRoomLayout = useRoomLayout;
        PlacementKind = placementKind;
        VisualScale = visualScale;
        VisualOffset = visualOffset;
        GeneratePreview = generatePreview;
        authoredHungHeight = hungHeight;
    }

    public string ProductId { get; }
    public string PrefabName { get; }
    public string SourceAssetPath { get; }
    private readonly Vector2 authoredFootprint;
    private readonly float authoredHeight, authoredYaw, authoredHungHeight;
    private readonly Vector3 authoredPosition;
    private readonly bool useRoomLayout;
    public Vector2 Footprint => useRoomLayout && HomeRoomLayoutCatalog.TryGet(ProductId,out var row) ? row.footprint : authoredFootprint;
    public float Height => useRoomLayout && HomeRoomLayoutCatalog.TryGet(ProductId,out var row) ? row.height : authoredHeight;
    public Vector3 DefaultPosition => useRoomLayout && HomeRoomLayoutCatalog.TryGet(ProductId,out var row) ? row.position : authoredPosition;
    public float DefaultYaw => useRoomLayout && HomeRoomLayoutCatalog.TryGet(ProductId,out var row) ? row.yaw : authoredYaw;
    public HomeProductPlacementKind PlacementKind { get; }
    public float VisualScale { get; }
    public Vector3 VisualOffset { get; }
    public bool GeneratePreview { get; }
    public float HungHeight => useRoomLayout && authoredHungHeight > .01f &&
        HomeRoomLayoutCatalog.TryGet(ProductId,out var row) ? row.position.y : authoredHungHeight;
    public string IconPath => StoreCatalogAssets.IconFolder + "/" + PrefabName + "_Icon.png";
    public StoreCatalogAsset WithoutRoomLayout() => new StoreCatalogAsset(ProductId,PrefabName,SourceAssetPath,
        authoredFootprint,authoredHeight,authoredPosition,authoredYaw,PlacementKind,VisualScale,VisualOffset,GeneratePreview,authoredHungHeight,false);
}
