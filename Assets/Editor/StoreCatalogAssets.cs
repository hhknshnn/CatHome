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
        Custom(HomeStoreService.ToyMouseId, "ToyMouse", new Vector2(.55f, 1.2f), .48f,
            new Vector3(-1.82f, 0f, -2.08f)),
        Custom(HomeStoreService.PlayTunnelId, "PlayTunnel", new Vector2(.58f, .9f), .44f,
            new Vector3(2.55f, 0f, -.55f)),
        RoomAt(HomeStoreService.TvUnitId, "TvUnit", "Nightstand_1.prefab",
            new Vector2(1.82f, .58f), .58f, 4f, new Vector3(-3.34f, 0f, -.42f),
            90f, HomeProductPlacementKind.WallEdge),
        RoomAt(HomeStoreService.ModernTelevisionId, "ModernTelevision", "TV_Modern.prefab",
            new Vector2(1.52f, .18f), 1.02f, 4f, new Vector3(-3.34f, 0f, -.42f),
            90f, HomeProductPlacementKind.ProductSurfaceOnly),
        Generated(HomeStoreService.GameConsoleId, "GameConsoleSet",
            new Vector2(.58f, .48f), .45f, new Vector3(2.45f, 0f, -2.15f),
            0f, HomeProductPlacementKind.Floor),
        RoomAt(HomeStoreService.FloorLampId, "FloorLamp", "Lamp_Tall.prefab",
            new Vector2(.7f, .7f), 1.85f, 3f, new Vector3(3.28f, 0f, -2.02f)),
        Custom(HomeStoreService.SideTableId, "SideTable", new Vector2(1.02f, 1.02f), .78f,
            new Vector3(2.92f, 0f, 1.18f)),

        RoomAt(HomeStoreService.BookshelfId, "TallBookshelf", "Bookshelf_Tall.prefab",
            new Vector2(1.6f, .62f), 1.85f, 2.5f, new Vector3(-3.25f, 0f, 1.05f),
            90f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BookSetId, "ColorfulBookSet",
            new Vector2(.25f, .25f), .32f, new Vector3(-3.25f, 0f, 1.05f),
            90f, HomeProductPlacementKind.BookshelfOnly),
        RoomAt(HomeStoreService.TallPlantId, "TallHouseplant", "PottedPlant_Tall_1.prefab",
            new Vector2(1f, 1f), 1.25f, 2.6f, new Vector3(3.15f, 0f, 1.65f)),
        RoomAt(HomeStoreService.ModernPaintingId, "ModernPainting", "Painting_Modern_1.prefab",
            new Vector2(1.1f, .2f), .82f, 4f, new Vector3(-3.55f, 0f, .7f),
            90f, HomeProductPlacementKind.WallEdge, new Vector3(0f, 1.35f, 0f)),
        Custom(HomeStoreService.WallClockId, "RoundWallClock", new Vector2(.8f, .2f), .85f,
            new Vector3(3.55f, 0f, .8f), 270f, HomeProductPlacementKind.WallEdge,
            new Vector3(0f, 1.35f, 0f)),

        Generated(HomeStoreService.BathroomBathMatId, "BathroomBathMat",
            new Vector2(1.9f, 1.15f), .08f, new Vector3(1.2f, 0f, .35f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomLaundryHamperId, "BathroomLaundryHamper",
            new Vector2(.82f, .82f), .92f, new Vector3(-2.9f, 0f, -1.8f),
            12f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomLitterBoxId, "BathroomLitterBox",
            new Vector2(1.3f, 1.02f), .48f, new Vector3(2.9f, 0f, -1.72f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomGroomingCartId, "BathroomGroomingCart",
            new Vector2(.92f, .64f), 1.02f, new Vector3(-1.55f, 0f, -1.85f),
            0f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.BathroomTowelStorageId, "BathroomTowelStorage",
            new Vector2(1.02f, .5f), 1.82f, new Vector3(-3.05f, 0f, .25f),
            90f, HomeProductPlacementKind.WallEdge),
        Generated(HomeStoreService.BathroomMirrorId, "BathroomWallMirror",
            new Vector2(.92f, .16f), .78f, new Vector3(-3.28f, 0f, 1.55f),
            90f, HomeProductPlacementKind.WallEdge, 1.58f),
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
            new Vector2(.82f, .82f), .72f, new Vector3(-3.3f, 0f, -1.95f),
            12f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.KitchenFeedingStationId, "KitchenFeedingStation",
            new Vector2(1.35f, .72f), .48f, new Vector3(3f, 0f, -1.95f),
            348f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.KitchenCounterStoolId, "KitchenCounterStool",
            new Vector2(.72f, .66f), .82f, new Vector3(1.85f, 0f, -1.28f),
            330f, HomeProductPlacementKind.Floor),
        Generated(HomeStoreService.KitchenPantryShelfId, "KitchenPantryShelf",
            new Vector2(1.15f, .55f), 1.85f, new Vector3(-3.22f, 0f, .95f),
            0f, HomeProductPlacementKind.WallEdge),
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

        Pet(HomeStoreService.CeramicBowlId, "CeramicBowl", "Bowl1 V3.prefab",
            new Vector2(.65f, .55f), .18f, 1.4f),
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

        Room(HomeStoreService.ArmchairId, "ClassicArmchair", "Armchair_Classic.prefab",
            new Vector2(1.4f, 1.05f), 1.12f, 4f),
        Room(HomeStoreService.SmallPlantId, "TablePlant", "PottedPlant_Small_2.prefab",
            new Vector2(.72f, .76f), .58f, 4f),
        Room(HomeStoreService.MirrorId, "WallMirror", "Mirror.prefab",
            new Vector2(1f, .2f), 1.12f, 4f, HomeProductPlacementKind.WallEdge,
            new Vector3(0f, 1.3f, 0f)),
        Generated(HomeStoreService.StereoId, "SpeakerSystem",
            new Vector2(1.18f, .58f), .66f, new Vector3(2.95f, 0f, 1.1f),
            0f, HomeProductPlacementKind.Floor),
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
        Vector3 visualOffset, bool generatePreview, float hungHeight = 0f)
    {
        ProductId = productId;
        PrefabName = prefabName;
        SourceAssetPath = sourceAssetPath;
        Footprint = footprint;
        Height = height;
        DefaultPosition = defaultPosition;
        DefaultYaw = defaultYaw;
        PlacementKind = placementKind;
        VisualScale = visualScale;
        VisualOffset = visualOffset;
        GeneratePreview = generatePreview;
        HungHeight = hungHeight;
    }

    public string ProductId { get; }
    public string PrefabName { get; }
    public string SourceAssetPath { get; }
    public Vector2 Footprint { get; }
    public float Height { get; }
    public Vector3 DefaultPosition { get; }
    public float DefaultYaw { get; }
    public HomeProductPlacementKind PlacementKind { get; }
    public float VisualScale { get; }
    public Vector3 VisualOffset { get; }
    public bool GeneratePreview { get; }
    public float HungHeight { get; }
    public string IconPath => StoreCatalogAssets.IconFolder + "/" + PrefabName + "_Icon.png";
}
