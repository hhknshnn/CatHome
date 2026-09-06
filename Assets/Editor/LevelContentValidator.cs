using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CatHome.Economy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class LevelValidationReport
{
    public readonly List<string> Errors = new List<string>();
    public readonly List<string> Warnings = new List<string>();

    public bool IsValid => Errors.Count == 0;

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(IsValid ? "Cat Home level validation passed." : "Cat Home level validation failed.");
        builder.Append($" Errors: {Errors.Count}, warnings: {Warnings.Count}.");

        for (int i = 0; i < Errors.Count; i++)
            builder.Append("\nERROR: ").Append(Errors[i]);
        for (int i = 0; i < Warnings.Count; i++)
            builder.Append("\nWARNING: ").Append(Warnings[i]);

        return builder.ToString();
    }
}

public static class LevelContentValidator
{
    [MenuItem("Tools/Cat Home/Architecture/Validate Project Architecture")]
    public static void ValidateFromMenu()
    {
        LevelValidationReport report = ValidateProject();
        if (report.IsValid)
            Debug.Log(report.ToString());
        else
            Debug.LogError(report.ToString());
    }

    public static LevelValidationReport ValidateProject()
    {
        var report = new LevelValidationReport();
        ValidateBuildSettings(report);
        ValidateProgressionConfig(report);
        ValidateHomeStoreCatalog(report);
        ValidateBootstrapScene(report);
        ValidateUiScene(report);
        ValidateLevelScenes(report);
        ValidateHomeRoomScenes(report);
        ValidateRunnerScene(report);
        return report;
    }

    private static void ValidateHomeStoreCatalog(LevelValidationReport report)
    {
        Require(HomeStoreService.LivingRoomItemCount == 10,
            "Living Room Level 1 must contain exactly ten products.", report);

        long previousPrice = -1L;
        int bookshelfIndex = -1;
        int bookSetIndex = -1;
        int tvUnitIndex = -1;
        int televisionIndex = -1;
        for (int i = 0; i < HomeStoreService.LivingRoomCollection.Count; i++)
        {
            string productId = HomeStoreService.LivingRoomCollection[i];
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Living Room Level 1 references missing product '" + productId + "'.");
                continue;
            }

            Require(product.CoinPrice > previousPrice,
                "Living Room Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Living Room product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
            if (productId == HomeStoreService.BookshelfId) bookshelfIndex = i;
            if (productId == HomeStoreService.BookSetId) bookSetIndex = i;
            if (productId == HomeStoreService.TvUnitId) tvUnitIndex = i;
            if (productId == HomeStoreService.ModernTelevisionId) televisionIndex = i;
        }

        Require(bookshelfIndex >= 0 && bookSetIndex > bookshelfIndex,
            "The colorful book set must be listed after the tall bookshelf.", report);
        Require(HomeStoreService.GetRequiredProductId(HomeStoreService.BookSetId) ==
                HomeStoreService.BookshelfId,
            "The colorful book set must require the tall bookshelf.", report);
        Require(!HomeStoreService.IsLivingRoomCollectionProduct(HomeStoreService.CarpetId),
            "The existing living-room carpet must not be sold in Room Level 1.", report);
        Require(!HomeStoreService.IsLivingRoomCollectionProduct(HomeStoreService.CoffeeTableId) &&
                !HomeStoreService.IsLivingRoomCollectionProduct(HomeStoreService.SofaId),
            "The already furnished coffee table and two-seat sofa must not be sold in Room Level 1.",
            report);
        Require(HomeStoreService.IsLivingRoomCollectionProduct(HomeStoreService.GameConsoleId) &&
                HomeStoreService.IsLivingRoomCollectionProduct(HomeStoreService.StereoId),
            "Room Level 1 must include the game console and speaker system.", report);
        Require(tvUnitIndex >= 0 && televisionIndex > tvUnitIndex,
            "The television must be listed after the TV unit.", report);
        Require(HomeStoreService.GetRequiredProductId(HomeStoreService.ModernTelevisionId) ==
                HomeStoreService.TvUnitId,
            "The television must require the TV unit.", report);

        Require(HomeStoreService.BathroomCollection.Count == 10,
            "Bathroom Level 1 must contain exactly ten products.", report);
        previousPrice = -1L;
        var bathroomIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < HomeStoreService.BathroomCollection.Count; i++)
        {
            string productId = HomeStoreService.BathroomCollection[i];
            Require(bathroomIds.Add(productId),
                "Bathroom collection contains duplicate product '" + productId + "'.",
                report);
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Bathroom Level 1 references missing product '" + productId + "'.");
                continue;
            }
            Require(product.StoreCategory == HomeStoreCategory.Room && product.IsPlaceable,
                "Bathroom product '" + productId + "' must be a placeable ROOM item.",
                report);
            Require(product.CoinPrice > previousPrice,
                "Bathroom Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Bathroom product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
        }

        Require(HomeStoreService.KitchenCollection.Count == 10,
            "Kitchen Level 1 must contain exactly ten products.", report);
        previousPrice = -1L;
        var kitchenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < HomeStoreService.KitchenCollection.Count; i++)
        {
            string productId = HomeStoreService.KitchenCollection[i];
            Require(kitchenIds.Add(productId),
                "Kitchen collection contains duplicate product '" + productId + "'.",
                report);
            Require(!HomeStoreService.IsLivingRoomCollectionProduct(productId) &&
                    !HomeStoreService.IsBathroomCollectionProduct(productId) &&
                    !HomeStoreService.IsBedroomCollectionProduct(productId) &&
                    !HomeStoreService.IsGardenCollectionProduct(productId),
                "Kitchen product '" + productId + "' must be exclusive to Kitchen.",
                report);
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Kitchen Level 1 references missing product '" +
                                  productId + "'.");
                continue;
            }
            Require(product.StoreCategory == HomeStoreCategory.Room && product.IsPlaceable,
                "Kitchen product '" + productId + "' must be a placeable ROOM item.",
                report);
            Require(product.CoinPrice > previousPrice,
                "Kitchen Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Kitchen product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
        }

        Require(HomeStoreService.BedroomCollection.Count == 10,
            "Bedroom Level 1 must contain exactly ten products.", report);
        previousPrice = -1L;
        var bedroomIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < HomeStoreService.BedroomCollection.Count; i++)
        {
            string productId = HomeStoreService.BedroomCollection[i];
            Require(bedroomIds.Add(productId),
                "Bedroom collection contains duplicate product '" + productId + "'.",
                report);
            Require(!HomeStoreService.IsLivingRoomCollectionProduct(productId) &&
                    !HomeStoreService.IsBathroomCollectionProduct(productId) &&
                    !HomeStoreService.IsKitchenCollectionProduct(productId) &&
                    !HomeStoreService.IsGardenCollectionProduct(productId),
                "Bedroom product '" + productId + "' must be exclusive to Bedroom.",
                report);
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Bedroom Level 1 references missing product '" +
                                  productId + "'.");
                continue;
            }
            Require(product.StoreCategory == HomeStoreCategory.Room && product.IsPlaceable,
                "Bedroom product '" + productId + "' must be a placeable ROOM item.",
                report);
            Require(product.CoinPrice > previousPrice,
                "Bedroom Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Bedroom product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
        }

        Require(HomeStoreService.GardenCollection.Count == 10,
            "Garden Level 1 must contain exactly ten products.", report);
        previousPrice = -1L;
        var gardenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < HomeStoreService.GardenCollection.Count; i++)
        {
            string productId = HomeStoreService.GardenCollection[i];
            Require(gardenIds.Add(productId),
                "Garden collection contains duplicate product '" + productId + "'.",
                report);
            Require(!HomeStoreService.IsLivingRoomCollectionProduct(productId) &&
                    !HomeStoreService.IsBathroomCollectionProduct(productId) &&
                    !HomeStoreService.IsKitchenCollectionProduct(productId) &&
                    !HomeStoreService.IsBedroomCollectionProduct(productId),
                "Garden product '" + productId + "' must be exclusive to Garden.",
                report);
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Garden Level 1 references missing product '" +
                                  productId + "'.");
                continue;
            }
            Require(product.StoreCategory == HomeStoreCategory.Room && product.IsPlaceable,
                "Garden product '" + productId + "' must be a placeable ROOM item.",
                report);
            Require(product.CoinPrice > previousPrice,
                "Garden Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Garden product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
        }

        Require(HomeStoreService.BalconyCollection.Count == 10,
            "Balcony Level 1 must contain exactly ten products.", report);
        previousPrice = -1L;
        var balconyIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < HomeStoreService.BalconyCollection.Count; i++)
        {
            string productId = HomeStoreService.BalconyCollection[i];
            Require(balconyIds.Add(productId),
                "Balcony collection contains duplicate product '" + productId + "'.",
                report);
            Require(!HomeStoreService.IsLivingRoomCollectionProduct(productId) &&
                    !HomeStoreService.IsBathroomCollectionProduct(productId) &&
                    !HomeStoreService.IsKitchenCollectionProduct(productId) &&
                    !HomeStoreService.IsBedroomCollectionProduct(productId) &&
                    !HomeStoreService.IsGardenCollectionProduct(productId),
                "Balcony product '" + productId + "' must be exclusive to Balcony.",
                report);
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Balcony Level 1 references missing product '" +
                                  productId + "'.");
                continue;
            }
            Require(product.StoreCategory == HomeStoreCategory.Room && product.IsPlaceable,
                "Balcony product '" + productId + "' must be a placeable ROOM item.",
                report);
            Require(product.CoinPrice > previousPrice,
                "Balcony Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Balcony product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
        }

        Require(HomeStoreService.PatioCollection.Count == 10,
            "Patio Level 1 must contain exactly ten products.", report);
        previousPrice = -1L;
        var patioIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < HomeStoreService.PatioCollection.Count; i++)
        {
            string productId = HomeStoreService.PatioCollection[i];
            Require(patioIds.Add(productId),
                "Patio collection contains duplicate product '" + productId + "'.",
                report);
            Require(!HomeStoreService.IsLivingRoomCollectionProduct(productId) &&
                    !HomeStoreService.IsBathroomCollectionProduct(productId) &&
                    !HomeStoreService.IsKitchenCollectionProduct(productId) &&
                    !HomeStoreService.IsBedroomCollectionProduct(productId) &&
                    !HomeStoreService.IsGardenCollectionProduct(productId) &&
                    !HomeStoreService.IsBalconyCollectionProduct(productId),
                "Patio product '" + productId + "' must be exclusive to Patio.",
                report);
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Patio Level 1 references missing product '" +
                                  productId + "'.");
                continue;
            }
            Require(product.StoreCategory == HomeStoreCategory.Room && product.IsPlaceable,
                "Patio product '" + productId + "' must be a placeable ROOM item.",
                report);
            Require(product.CoinPrice > previousPrice,
                "Patio Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Patio product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
        }

        Require(HomeStoreService.SecondFloorCollection.Count == 10,
            "Second Floor Level 1 must contain exactly ten products.", report);
        previousPrice = -1L;
        var secondFloorIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < HomeStoreService.SecondFloorCollection.Count; i++)
        {
            string productId = HomeStoreService.SecondFloorCollection[i];
            Require(secondFloorIds.Add(productId),
                "Second Floor collection contains duplicate product '" + productId + "'.",
                report);
            Require(!HomeStoreService.IsLivingRoomCollectionProduct(productId) &&
                    !HomeStoreService.IsBathroomCollectionProduct(productId) &&
                    !HomeStoreService.IsKitchenCollectionProduct(productId) &&
                    !HomeStoreService.IsBedroomCollectionProduct(productId) &&
                    !HomeStoreService.IsGardenCollectionProduct(productId) &&
                    !HomeStoreService.IsBalconyCollectionProduct(productId) &&
                    !HomeStoreService.IsPatioCollectionProduct(productId),
                "Second Floor product '" + productId + "' must be exclusive to Second Floor.",
                report);
            if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
            {
                report.Errors.Add("Second Floor Level 1 references missing product '" +
                                  productId + "'.");
                continue;
            }
            Require(product.StoreCategory == HomeStoreCategory.Room && product.IsPlaceable,
                "Second Floor product '" + productId + "' must be a placeable ROOM item.",
                report);
            Require(product.CoinPrice > previousPrice,
                "Second Floor Level 1 prices must increase from top to bottom.", report);
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Every Second Floor product must use the 100 coins = 1 diamond exchange rate.",
                report);
            previousPrice = product.CoinPrice;
        }

        for (int i = 0; i < HomeStoreService.Products.Count; i++)
        {
            HomeStoreProduct product = HomeStoreService.Products[i];
            if (!product.IsAvailable || product.CoinPrice <= 0L)
                continue;
            Require(product.CoinPrice % HomeStoreService.CoinsPerDiamond == 0L &&
                    product.DiamondPrice * HomeStoreService.CoinsPerDiamond == product.CoinPrice,
                "Purchasable product '" + product.Id +
                "' must expose equivalent coin and diamond prices.", report);
        }

        long[] expectedDiamondPacks = { 10L, 20L, 50L, 100L, 500L, 1000L };
        Require(DiamondPackCatalog.Packs.Count == expectedDiamondPacks.Length,
            "The IAP preparation catalog must contain six diamond packs.", report);
        for (int i = 0; i < expectedDiamondPacks.Length &&
                        i < DiamondPackCatalog.Packs.Count; i++)
        {
            Require(DiamondPackCatalog.Packs[i].DiamondAmount == expectedDiamondPacks[i],
                "Diamond pack order or amount is invalid at index " + i + ".", report);
        }
    }

    private static void ValidateBuildSettings(LevelValidationReport report)
    {
        var enabledPaths = new HashSet<string>(StringComparer.Ordinal);
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
            if (scenes[i].enabled)
                enabledPaths.Add(scenes[i].path);

        Require(enabledPaths.Contains(SceneArchitectureBuilder.BootstrapScenePath),
            "Bootstrap scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(SceneArchitectureBuilder.UiScenePath),
            "Shared UI scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.LivingRoomScenePath),
            "Living Room scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.BathroomScenePath),
            "Bathroom scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.KitchenScenePath),
            "Kitchen scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.BedroomScenePath),
            "Bedroom scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.GardenScenePath),
            "Garden scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.BalconyScenePath),
            "Balcony scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.PatioScenePath),
            "Patio scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(HomeRoomService.SecondFloorScenePath),
            "Second Floor scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(CatRunnerContentBuilder.RunnerScenePath),
            "Cat Runner scene is not enabled in Build Settings.", report);
        Require(enabledPaths.Contains(CatCatchContentBuilder.ScenePath),
            "Cat Catch scene is not enabled in Build Settings.", report);
    }

    private static void ValidateProgressionConfig(LevelValidationReport report)
    {
        if (!ProgressionConfig.TryGetActive(out ProgressionConfig config))
        {
            report.Errors.Add("Resources/ProgressionConfig.asset is missing.");
            return;
        }

        var levelIds = new HashSet<string>(StringComparer.Ordinal);
        var questIds = new HashSet<string>(StringComparer.Ordinal);
        var enabledScenePaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.enabled)
                enabledScenePaths.Add(scene.path);

        for (int i = 0; i < config.ChapterCount; i++)
        {
            LevelDefinition level = config.GetChapter(i + 1);
            if (level == null)
            {
                report.Errors.Add($"Quest chapter slot {i + 1} is empty.");
                continue;
            }

            Require(!string.IsNullOrWhiteSpace(level.LevelId),
                $"Quest chapter {i + 1} has no stable id.", report);
            Require(levelIds.Add(level.LevelId),
                $"Duplicate quest chapter id '{level.LevelId}'.", report);
            Require(level.ChapterNumber == i + 1,
                $"Quest chapter '{level.LevelId}' must have chapter number {i + 1}.", report);
            Require(!string.IsNullOrWhiteSpace(level.ScenePath) &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(level.ScenePath) != null,
                $"Level '{level.LevelId}' references a missing scene '{level.ScenePath}'.", report);
            Require(enabledScenePaths.Contains(level.ScenePath),
                $"Level scene '{level.ScenePath}' is not enabled in Build Settings.", report);
            Require(!string.IsNullOrWhiteSpace(level.SpawnPointId),
                $"Level '{level.LevelId}' has no spawn point id.", report);
            Require(level.Quests.Count > 0,
                $"Level '{level.LevelId}' has no quests.", report);

            for (int q = 0; q < level.Quests.Count; q++)
            {
                QuestDefinition quest = level.Quests[q];
                if (quest == null)
                {
                    report.Errors.Add($"Level '{level.LevelId}' has an empty quest slot.");
                    continue;
                }

                Require(!string.IsNullOrWhiteSpace(quest.QuestId),
                    $"Level '{level.LevelId}' has a quest without an id.", report);
                Require(questIds.Add(quest.QuestId),
                    $"Duplicate quest id '{quest.QuestId}'.", report);
                Require(quest.RequiredCount > 0,
                    $"Quest '{quest.QuestId}' has an invalid target.", report);
            }
        }
    }

    private static void ValidateBootstrapScene(LevelValidationReport report)
    {
        ValidateScene(SceneArchitectureBuilder.BootstrapScenePath, scene =>
        {
            Require(FindInScene<LevelLoader>(scene) != null,
                "Bootstrap scene has no LevelLoader.", report);
            ValidateMissingScripts(scene, report);
        }, report);
    }

    private static void ValidateUiScene(LevelValidationReport report)
    {
        ValidateScene(SceneArchitectureBuilder.UiScenePath, scene =>
        {
            Require(FindInScene<EventSystem>(scene) != null, "Shared UI has no EventSystem.", report);
            Require(FindInScene<HungerSystem>(scene) != null, "Shared UI has no HungerSystem.", report);
            Require(FindInScene<ThirstSystem>(scene) != null, "Shared UI has no ThirstSystem.", report);
            Require(FindInScene<EnergySystem>(scene) != null, "Shared UI has no EnergySystem.", report);
            MainPanelController mainPanel = FindInScene<MainPanelController>(scene);
            Require(mainPanel != null,
                "Shared UI has no MainPanelController.", report);
            Require(mainPanel == null ||
                    (FindNamedInChildren(mainPanel.transform, "LightButton") == null &&
                     FindNamedInChildren(mainPanel.transform, "ClockDisplay") == null &&
                     FindNamedInChildren(mainPanel.transform, "BrightnessPanel") == null &&
                     mainPanel.GetComponent<BrightnessPanelView>() == null),
                "The shared top bar must not restore the removed clock or brightness controls.",
                report);
            Require(mainPanel == null ||
                    FindNamedInChildren(mainPanel.transform, "CatShopButton") != null,
                "The shared top bar needs the real-cat CAT SHOP button in the removed light slot.",
                report);
            CatBreedShopPanel[] breedShops = FindAllInScene<CatBreedShopPanel>(scene);
            Require(breedShops.Length == 1,
                "Shared UI must contain exactly one persistent CAT SHOP panel.", report);
            if (breedShops.Length == 1)
            {
                Require(breedShops[0].gameObject.name == CatBreedShopPanelBuilder.RootName,
                    "CAT SHOP must use its named persistent canvas root.", report);
                Require(breedShops[0].GetComponentInChildren<SafeAreaRect>(true) != null,
                    "CAT SHOP needs safe-area fitting for notched displays.", report);
                Require(breedShops[0].GetComponentsInChildren<CatBreedTurntablePreview>(true).Length == 1,
                    "CAT SHOP needs one interactive live 3D turntable.", report);
            }
            CatBreedCatalog breedCatalog = AssetDatabase.LoadAssetAtPath<CatBreedCatalog>(
                PolyperfectCatIntegrationBuilder.CatalogPath);
            Require(breedCatalog != null && breedCatalog.Count == 10,
                "CAT SHOP catalog must expose all ten imported cats.", report);
            if (breedCatalog != null)
            {
                Require(breedCatalog.GameplayController != null,
                    "CAT SHOP catalog has no Cat Home gameplay controller.", report);
                for (int i = 0; i < breedCatalog.Count; i++)
                {
                    CatBreedCatalog.Entry breed = breedCatalog.Get(i);
                    Require(breed != null && breed.SourcePrefab != null && breed.Portrait != null,
                        $"CAT SHOP breed entry {i} is missing its real model or portrait.", report);
                }
            }
            Require(FindInScene<QuestPanelController>(scene) != null,
                "Shared UI has no QuestPanelController.", report);
            Require(FindInScene<CurrencyHudController>(scene) != null,
                "Shared UI has no CurrencyHudController.", report);
            RoomSelectorPanel[] roomSelectors = FindAllInScene<RoomSelectorPanel>(scene);
            Require(roomSelectors.Length == 1,
                "Shared UI must contain exactly one persistent Room Selector.", report);
            if (roomSelectors.Length == 1)
            {
                Require(roomSelectors[0].gameObject.name == RoomSelectorPanelBuilder.RootName,
                    "Room Selector must use its named persistent canvas root.", report);
                Require(roomSelectors[0].GetComponentInChildren<SafeAreaRect>(true) != null,
                    "Room Selector needs safe-area fitting for notched displays.", report);
            }
            ShopPanelController shop = FindInScene<ShopPanelController>(scene);
            Require(shop != null,
                "Shared UI has no persistent Home Store panel.", report);
            Require(FindInScene<ActivityPromptController>(scene) != null,
                "Shared UI has no ActivityPromptController.", report);
            Require(FindInScene<CatRunnerLauncher>(scene) != null,
                "Shared UI has no Cat Runner PLAY launcher.", report);
            Require(FindInScene<GamesHubPanel>(scene) != null,
                "Shared UI has no Games hub for Cat Runner and Cat Catch.", report);
            Require(FindInScene<CatCatchLauncher>(scene) != null,
                "Shared UI has no Cat Catch launcher.", report);
            Require(FindNamedInScene(scene, "RunnerEnergyLabel") != null,
                "Shared UI has no Runner Energy indicator.", report);
            Require(FindNamedInScene(scene, "RewardedEnergyButton") != null,
                "Shared UI has no rewarded +2 Energy button.", report);
            GameObject productScrollObject = FindNamedInScene(scene, "ProductScroll");
            ScrollRect productScroll = productScrollObject != null
                ? productScrollObject.GetComponent<ScrollRect>()
                : null;
            Require(productScroll != null && productScroll.verticalScrollbar != null,
                "The CAT, ROOM and HOME catalog needs a visible vertical scrollbar.", report);
            Require(productScroll == null ||
                    productScroll.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.Permanent,
                "The store scrollbar must stay visible so scroll position is always clear.", report);
            Require(FindNamedInScene(scene, "PurchaseDialog") != null &&
                    FindNamedInScene(scene, "DialogProductIcon") != null &&
                    FindNamedInScene(scene, "CoinPurchaseButton") != null &&
                    FindNamedInScene(scene, "DiamondPurchaseButton") != null,
                "The Home Store needs its product-photo purchase dialog with coin and diamond choices.",
                report);
            Require(FindNamedInScene(scene, "ShopButton") != null,
                "The bottom dock must expose the general SHOP button.", report);
            string[] premiumLauncherButtons =
            {
                "ShopButton",
                "PlayCatRunnerButton",
                "RewardedEnergyButton"
            };
            for (int i = 0; i < premiumLauncherButtons.Length; i++)
            {
                GameObject premiumButton = FindNamedInScene(scene, premiumLauncherButtons[i]);
                Require(premiumButton != null &&
                        premiumButton.GetComponent<Button>() != null &&
                        premiumButton.GetComponent<PremiumButtonFx>() != null,
                    premiumLauncherButtons[i] +
                    " must retain PremiumButtonFx after the Runner launcher rebuild.",
                    report);
            }
            Require(FindNamedInScene(scene, "HomeDockShadow") == null,
                "The removed translucent HomeDockShadow must not be rebuilt.", report);

            GameObject sharedCanvas = FindRootInScene(scene, "Canvas");
            WhileYouWereAwayPopup[] offlinePopups =
                FindAllInScene<WhileYouWereAwayPopup>(scene);
            Require(offlinePopups.Length == 1,
                "Shared UI must contain exactly one While You Were Away popup.", report);
            if (offlinePopups.Length == 1)
            {
                Require(sharedCanvas != null &&
                        offlinePopups[0].transform.parent == sharedCanvas.transform,
                    "While You Were Away popup must be a direct child of the root Canvas.",
                    report);
            }

            CatRunnerLauncher[] launchers = FindAllInScene<CatRunnerLauncher>(scene);
            Require(launchers.Length == 1,
                "Shared UI must contain exactly one Cat Runner launcher.", report);
            if (launchers.Length == 1)
            {
                Require(sharedCanvas != null &&
                        launchers[0].transform.parent == sharedCanvas.transform,
                    "Cat Runner launcher must be a direct child of the root Canvas.",
                    report);
            }
            ValidateMissingScripts(scene, report);
        }, report);
    }

    private static void ValidateRunnerScene(LevelValidationReport report)
    {
        ValidateScene(CatRunnerContentBuilder.RunnerScenePath, scene =>
        {
            CatRunnerGameController game = FindInScene<CatRunnerGameController>(scene);
            CatRunnerPlayer player = FindInScene<CatRunnerPlayer>(scene);
            CatRunnerTrackManager track = FindInScene<CatRunnerTrackManager>(scene);
            CatRunnerAudioController audio = FindInScene<CatRunnerAudioController>(scene);
            CatRunnerResponsiveLayout responsive =
                FindInScene<CatRunnerResponsiveLayout>(scene);
            Canvas canvas = FindInScene<Canvas>(scene);
            Camera camera = FindInScene<Camera>(scene);
            EventSystem eventSystem = FindInScene<EventSystem>(scene);

            Require(game != null, "Cat Runner scene has no game controller.", report);
            Require(player != null, "Cat Runner scene has no player controller.", report);
            Require(track != null, "Cat Runner scene has no track manager.", report);
            Require(audio != null,
                "Cat Runner scene has no music/SFX/haptic controller.", report);
            Require(responsive != null,
                "Cat Runner UI has no responsive safe-area layout.", report);
            Require(track == null || track.ObjectApproachSpeedMultiplier > 1f,
                "Cat Runner obstacles must approach faster than the visual floor scroll.", report);
            Require(canvas != null, "Cat Runner scene has no HUD canvas.", report);
            Require(camera != null, "Cat Runner scene has no camera.", report);
            Require(FindAllInScene<AudioListener>(scene).Length == 1,
                "Cat Runner scene must author exactly one AudioListener.", report);
            Require(eventSystem == null,
                "Cat Runner must reuse CatHome_UI's shared EventSystem; remove the duplicate Runner EventSystem.",
                report);
            Require(FindNamedInScene(scene, "ChancesLabel") != null,
                "Cat Runner scene has no three-chance HUD indicator.", report);
            Require(FindAllInScene<SafeAreaRect>(scene).Length >= 4,
                "Cat Runner welcome, HUD, results and modal cards must respect SafeArea.",
                report);

            string[] requiredRunnerObjects =
            {
                "ScoreLabel",
                "ComboLabel",
                "PowerUpLabel",
                "PauseButton",
                "PausePanel",
                "TutorialPanel",
                "WelcomeRewardedEnergyButton",
                "DailyMissions",
                "ResultMissions",
                "NewBestBadge",
                "MagnetPowerUp_Template",
                "ShieldPowerUp_Template",
                "DoubleCoinsPowerUp_Template"
            };
            for (int i = 0; i < requiredRunnerObjects.Length; i++)
                Require(FindNamedInScene(scene, requiredRunnerObjects[i]) != null,
                    "Cat Runner is missing '" + requiredRunnerObjects[i] + "'.", report);

            string[] runnerButtons =
            {
                "PauseButton",
                "WelcomeStartButton",
                "WelcomeExitButton",
                "WelcomeRewardedEnergyButton",
                "CollectButton",
                "RetryButton",
                "ResumeButton",
                "PauseExitButton",
                "ReducedMotionButton",
                "SoundButton",
                "HapticsButton",
                "TutorialSkipButton"
            };
            for (int i = 0; i < runnerButtons.Length; i++)
            {
                GameObject button = FindNamedInScene(scene, runnerButtons[i]);
                Require(button != null &&
                        button.GetComponent<Button>() != null &&
                        button.GetComponent<PremiumButtonFx>() != null,
                    runnerButtons[i] +
                    " must retain Button and PremiumButtonFx.", report);
            }

            CatRunnerTrackObject[] trackObjects =
                FindAllInScene<CatRunnerTrackObject>(scene);
            int powerUps = 0;
            for (int i = 0; i < trackObjects.Length; i++)
            {
                CatRunnerTrackObject item = trackObjects[i];
                if (item == null)
                    continue;
                if (item.Kind == CatRunnerTrackObjectKind.PowerUp)
                    powerUps++;
                if (item.IsHazard)
                    Require(item.transform.Find("HazardWarningTelegraph") != null,
                        item.name + " has no readable hazard telegraph.", report);
            }
            Require(powerUps == 3,
                "Cat Runner must retain Magnet, Shield and Double Coins templates.",
                report);

            CatRunnerScenerySegment[] scenerySegments =
                FindAllInScene<CatRunnerScenerySegment>(scene);
            Require(scenerySegments.Length >= 8,
                "Cat Runner must keep a recyclable scenery corridor.", report);
            for (int i = 0; i < scenerySegments.Length; i++)
            {
                CatRunnerScenerySegment scenery = scenerySegments[i];
                if (scenery == null)
                    continue;
                Require(
                    scenery.VariantCount == CatRunnerContentBuilder.SceneryVariantCount,
                    scenery.name + " must author " +
                    CatRunnerContentBuilder.SceneryVariantCount +
                    " exclusive scenery silhouettes.",
                    report);
                for (int v = 0; v < CatRunnerContentBuilder.SceneryVariantNames.Length; v++)
                {
                    string variantName = CatRunnerContentBuilder.SceneryVariantNames[v];
                    Require(
                        scenery.transform.Find(variantName) != null,
                        scenery.name + " is missing " + variantName + ".",
                        report);
                }
            }

            string[] instancedMaterials =
            {
                "RunnerRoadPeach",
                "RunnerLaneMint",
                "RunnerCoinGold",
                "RunnerHazardWarning",
                "RunnerPowerMagnet"
            };
            for (int i = 0; i < instancedMaterials.Length; i++)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Art/Runner/Materials/" + instancedMaterials[i] + ".mat");
                Require(material != null && material.enableInstancing,
                    instancedMaterials[i] +
                    " must keep GPU instancing enabled.", report);
            }

            if (player != null)
            {
                Animator animator = player.GetComponentInChildren<Animator>(true);
                Require(animator != null && animator.runtimeAnimatorController != null,
                    "Cat Runner player does not use the current animated cat.", report);
                if (animator != null && animator.runtimeAnimatorController != null)
                    Require(
                        AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) ==
                        PolyperfectCatIntegrationBuilder.ControllerPath,
                        "Cat Runner player is not using the current cat animator controller.", report);
            }

            GameObject[] roots = scene.GetRootGameObjects();
            Require(roots.Length == 1 && roots[0].transform.position.y >= 900f,
                "Cat Runner world must remain isolated from the additively loaded home.", report);
            ValidateMissingScripts(scene, report);
        }, report);
    }

    private static void ValidateLevelScenes(LevelValidationReport report)
    {
        if (!ProgressionConfig.TryGetActive(out ProgressionConfig config))
            return;

        var validatedPaths = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < config.ChapterCount; i++)
        {
            LevelDefinition level = config.GetChapter(i + 1);
            if (level == null || string.IsNullOrWhiteSpace(level.ScenePath) ||
                !validatedPaths.Add(level.ScenePath))
            {
                continue;
            }

            ValidateScene(level.ScenePath, scene =>
            {
                Require(FindInScene<LevelSceneMarker>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no LevelSceneMarker.", report);
                Require(FindInScene<CatMovement>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no cat.", report);
                Require(FindInScene<BowlInteraction>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no bowl interaction.", report);
                Require(FindInScene<SleepInteraction>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no sleep interaction.", report);
                Require(FindInScene<PetInteraction>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no pet interaction.", report);
                Require(FindInScene<CatActivityReaction>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no cat activity reaction controller.", report);
                Require(FindInScene<BallChaseActivity>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no ball chase activity.", report);
                Require(FindInScene<ScratchPostActivity>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no scratching activity.", report);
                Require(FindInScene<MouseHuntActivity>(scene) != null,
                    $"Level scene '{level.ScenePath}' has no mouse hunt activity.", report);
                Require(FindSitLook(scene, CatActivityKind.WindowWatch) != null,
                    $"Level scene '{level.ScenePath}' has no window-watch activity.", report);
                StoreProductDisplay[] storeProducts = FindAllInScene<StoreProductDisplay>(scene);
                Require(storeProducts.Length >= 6,
                    $"Level scene '{level.ScenePath}' has fewer than six authored store products.",
                    report);
                var storeProductIds = new HashSet<string>(StringComparer.Ordinal);
                for (int productIndex = 0; productIndex < storeProducts.Length; productIndex++)
                {
                    StoreProductDisplay product = storeProducts[productIndex];
                    if(product!=null&&HomeStoreService.TryGetProduct(product.ProductId,out var catProduct)&&catProduct.StoreCategory==HomeStoreCategory.Cat)
                    {
                        var enrichment=product.GetComponent<CatEnrichmentActivity>();
                        Require(enrichment!=null&&enrichment.ContactPoint!=null&&enrichment.RoutineEntryPoint!=null,
                            "CAT product '"+product.ProductId+"' needs a measured cat interaction.",report);
                        if(enrichment!=null)
                        {
                            int matching=0;foreach(var routine in FindAllInScene<CatEnrichmentActivity>(scene))if(routine.Kind==enrichment.Kind)matching++;
                            Require(matching==1,"CAT activity kind must be unique: "+enrichment.Kind,report);
                        }
                    }
                    Require(product != null && !string.IsNullOrWhiteSpace(product.ProductId),
                        $"Level scene '{level.ScenePath}' has a store product without an id.", report);
                    if (product != null && !string.IsNullOrWhiteSpace(product.ProductId))
                    {
                        Require(storeProductIds.Add(product.ProductId),
                            $"Duplicate store product '{product.ProductId}' in '{level.ScenePath}'.",
                            report);
                    }
                    HomeProductPlacement placement = product != null
                        ? product.GetComponent<HomeProductPlacement>()
                        : null;
                    Require(placement != null,
                        $"Store product '{product?.ProductId}' has no placement controller.", report);
                    Require(placement == null || placement.SupportsRotation ==
                            !(HomeStoreService.IsFixedRoomProduct(product.ProductId) || CatCollectionPolicy.IsCatItem(product.ProductId)),
                        $"Store product '{product?.ProductId}' must keep its authored orientation.", report);
                }
                HomeBookshelfBookSet bookSet = FindInScene<HomeBookshelfBookSet>(scene);
                Require(bookSet != null && bookSet.BookCount == 10,
                    "Living Room Level 1 needs one ten-book colorful bookshelf set.", report);
                Require(bookSet != null && bookSet.ShelfRowCount == 3,
                    "The colorful book set must fill all three bookshelf rows.", report);
                Require(bookSet != null &&
                        bookSet.GetComponent<HomeProductPlacement>() != null &&
                        bookSet.GetComponent<HomeProductPlacement>().PlacementKind ==
                            HomeProductPlacementKind.BookshelfOnly,
                    "The colorful book set must only support bookshelf placement.", report);
                HomeRequiredProductAttachment tvAttachment =
                    FindAllInScene<HomeRequiredProductAttachment>(scene)
                        .FirstOrDefault(component =>
                            component.GetComponent<HomeProductPlacement>() != null &&
                            component.GetComponent<HomeProductPlacement>().ProductId ==
                                HomeStoreService.ModernTelevisionId);
                Require(tvAttachment != null &&
                        tvAttachment.RequiredProductId == HomeStoreService.TvUnitId,
                    "The modern television must attach only to the TV unit.", report);
                Require(tvAttachment == null ||
                        tvAttachment.GetComponent<HomeProductPlacement>().PlacementKind ==
                            HomeProductPlacementKind.ProductSurfaceOnly,
                    "The modern television must use TV-unit-only placement.", report);
                ValidateAuthoringHierarchy(scene, level.ScenePath, report);

                LevelSpawnPoint[] spawnPoints = FindAllInScene<LevelSpawnPoint>(scene);
                var spawnIds = new HashSet<string>(StringComparer.Ordinal);
                for (int s = 0; s < spawnPoints.Length; s++)
                    Require(spawnIds.Add(spawnPoints[s].SpawnPointId),
                        $"Duplicate spawn point '{spawnPoints[s].SpawnPointId}' in '{level.ScenePath}'.",
                        report);

                for (int chapterIndex = 0; chapterIndex < config.ChapterCount; chapterIndex++)
                {
                    LevelDefinition referencedLevel = config.GetChapter(chapterIndex + 1);
                    if (referencedLevel != null && referencedLevel.ScenePath == level.ScenePath)
                        Require(spawnIds.Contains(referencedLevel.SpawnPointId),
                            $"Level '{referencedLevel.LevelId}' references missing spawn " +
                            $"'{referencedLevel.SpawnPointId}'.", report);
                }

                ValidateMissingScripts(scene, report);
            }, report);
        }
    }

    private static void ValidateHomeRoomScenes(LevelValidationReport report)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<HomeRoomDefinition> rooms = HomeRoomService.Rooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            HomeRoomDefinition room = rooms[i];
            Require(ids.Add(room.Id), "Duplicate home room id '" + room.Id + "'.", report);
            ValidateScene(room.ScenePath, scene =>
            {
                HomeRoomSceneMarker explicitMarker = FindInScene<HomeRoomSceneMarker>(scene);
                LevelSceneMarker legacyMarker = FindInScene<LevelSceneMarker>(scene);
                string markerId = explicitMarker != null
                    ? explicitMarker.RoomId
                    : legacyMarker != null ? legacyMarker.SceneId : null;
                Require(string.Equals(markerId, room.Id, StringComparison.Ordinal),
                    $"Home room '{room.ScenePath}' has marker '{markerId}', expected '{room.Id}'.",
                    report);

                CatMovement cat = FindInScene<CatMovement>(scene);
                Camera[] cameras = FindAllInScene<Camera>(scene);
                int enabledCameras = cameras.Count(camera => camera.enabled);
                AudioListener[] listeners = FindAllInScene<AudioListener>(scene);
                int enabledListeners = listeners.Count(listener => listener.enabled);
                Require(cat != null, $"Home room '{room.ScenePath}' has no cat.", report);
                Require(FindAllInScene<GameTimeService>(scene).Length == 1,
                    $"Home room '{room.ScenePath}' must author exactly one GameTimeService.",
                    report);
                Require(cameras.Length == 1 && enabledCameras == 1,
                    $"Home room '{room.ScenePath}' must author exactly one enabled camera.", report);
                Require(listeners.Length == 1 && enabledListeners == 1,
                    $"Home room '{room.ScenePath}' must author exactly one enabled AudioListener.",
                    report);

                LevelSpawnPoint[] spawns = FindAllInScene<LevelSpawnPoint>(scene);
                Require(spawns.Count(spawn => spawn.SpawnPointId == room.SpawnPointId) == 1,
                    $"Home room '{room.ScenePath}' needs exactly one '{room.SpawnPointId}' spawn.",
                    report);
                HomeRoomBoundary boundary = FindInScene<HomeRoomBoundary>(scene);
                Require(boundary != null,
                    $"Home room '{room.ScenePath}' needs its movement boundary.", report);
                ValidateAuthoringHierarchy(scene, room.ScenePath, report);

                if (room.Id == HomeRoomService.LivingRoomId)
                    ValidateLivingRoomSafety(scene, report);
                if (room.Id == HomeRoomService.BathroomId)
                    ValidateBathroomRoom(scene, report);
                if (room.Id == HomeRoomService.KitchenId)
                    ValidateKitchenRoom(scene, report);
                if (room.Id == HomeRoomService.BedroomId)
                    ValidateBedroomRoom(scene, report);
                if (room.Id == HomeRoomService.GardenId)
                    ValidateGardenRoom(scene, report);
                if (room.Id == HomeRoomService.BalconyId)
                    ValidateBalconyRoom(scene, report);
                if (room.Id == HomeRoomService.PatioId)
                    ValidatePatioRoom(scene, report);
                if (room.Id == HomeRoomService.SecondFloorId)
                    ValidateSecondFloorRoom(scene, report);
                ValidateMissingScripts(scene, report);
            }, report);
        }
    }

    private static void ValidateLivingRoomSafety(Scene scene, LevelValidationReport report)
    {
        var interactions = new[]
        {
            (CatActivityKind.ArmchairNap, HomeStoreService.ArmchairId),
            (CatActivityKind.LampWatch, HomeStoreService.FloorLampId),
            (CatActivityKind.BookshelfSniff, HomeStoreService.BookshelfId),
            (CatActivityKind.BookSetSniff, HomeStoreService.BookSetId),
            (CatActivityKind.PlantSniff, HomeStoreService.TallPlantId),
            (CatActivityKind.PaintingWatch, HomeStoreService.ModernPaintingId),
            (CatActivityKind.TvUnitPaw, HomeStoreService.TvUnitId),
            (CatActivityKind.TelevisionWatch, HomeStoreService.ModernTelevisionId),
            (CatActivityKind.ConsolePaw, HomeStoreService.GameConsoleId),
            (CatActivityKind.SpeakerListen, HomeStoreService.StereoId)
        };
        foreach (var pair in interactions)
            RequireRoomActivity(scene, pair.Item1, pair.Item2, pair.Item1.ToString(), "Living Room", report);
        GameTimeService time = FindInScene<GameTimeService>(scene);
        bool fixedDaylight = false;
        if (time != null)
        {
            SerializedObject serialized = new SerializedObject(time);
            fixedDaylight = serialized.FindProperty("useTestTime").boolValue &&
                Mathf.Abs(serialized.FindProperty("testHour").floatValue -
                          HomeRoomGameplaySafetyBuilder.LivingRoomDaylightHour) < .001f;
        }
        Require(fixedDaylight,
            "Living Room must stay on its fixed daylight presentation.", report);

        RequireSolidFurniture(scene, HomeRoomGameplaySafetyBuilder.LivingSofaName, report);
        RequireSolidFurniture(scene, HomeRoomGameplaySafetyBuilder.LivingCoffeeTableName, report);
        foreach(var kind in new[]{CatActivityKind.SofaLounge,CatActivityKind.CoffeeTablePlay})
        {
            LivingFurnitureActivity match=null;
            foreach(var root in scene.GetRootGameObjects())foreach(var activity in root.GetComponentsInChildren<LivingFurnitureActivity>(true))
                if(activity.Kind==kind)match=activity;
            Require(match!=null && match.RoutineEntryPoint!=null && match.Perch!=null,
                "Living Room needs its measured jump routine: "+kind,report);
        }
    }

    private static void RequireSolidFurniture(
        Scene scene, string objectName, LevelValidationReport report)
    {
        GameObject furniture = FindNamedInScene(scene, objectName);
        BoxCollider collider = furniture != null ? furniture.GetComponent<BoxCollider>() : null;
        Require(collider != null && collider.enabled && !collider.isTrigger,
            $"Living Room fixed furniture '{objectName}' needs an enabled solid BoxCollider.",
            report);
    }

    private static void ValidateBathroomRoom(Scene scene, LevelValidationReport report)
    {
        Require(FindNamedInScene(scene, "BathroomTub") != null,
            "Bathroom needs its tub fixture.", report);
        Require(FindNamedInScene(scene, "BathroomVanitySink") != null,
            "Bathroom needs its vanity and sink fixture.", report);
        Require(FindNamedInScene(scene, "BathroomToilet") != null,
            "Bathroom needs its toilet fixture.", report);
        Require(FindNamedInScene(scene, "BathroomShower") != null,
            "Bathroom needs its shower fixture.", report);
        Require(FindNamedInScene(scene, "Glossy Floor Tiles") != null &&
                FindNamedInScene(scene, "Coral Lilac Tile Ribbon") != null,
            "Bathroom needs glossy tiles and its bright wall ribbon.", report);
        PaperSpinActivity paperSpin = FindInScene<PaperSpinActivity>(scene);
        Require(paperSpin != null,
            "Bathroom needs the toilet paper spin activity.", report);
        Require(paperSpin == null ||
                paperSpin.StoreProductId == HomeStoreService.BathroomToiletId,
            "The paper spin activity must be gated on owning BathroomToilet.", report);
        SinkSipActivity sinkSip = FindInScene<SinkSipActivity>(scene);
        Require(sinkSip != null,
            "Bathroom needs the vanity tap drinking activity.", report);
        Require(sinkSip == null ||
                sinkSip.StoreProductId == HomeStoreService.BathroomVanityId,
            "The vanity sip activity must be gated on owning BathroomVanitySink.", report);
        ShowerRinseActivity showerRinse = FindInScene<ShowerRinseActivity>(scene);
        Require(showerRinse != null,
            "Bathroom needs the rainbow shower rinse activity.", report);
        Require(showerRinse == null ||
                showerRinse.StoreProductId == HomeStoreService.BathroomShowerId,
            "The shower rinse activity must be gated on owning BathroomShower.", report);
        TowelNestActivity towelNest = FindInScene<TowelNestActivity>(scene);
        Require(towelNest != null,
            "Bathroom needs the towel niche nap activity.", report);
        Require(towelNest == null ||
                towelNest.StoreProductId == HomeStoreService.BathroomTowelStorageId,
            "The towel nest activity must be gated on owning BathroomTowelStorage.", report);
        LitterDigActivity litterDig = FindInScene<LitterDigActivity>(scene);
        Require(litterDig != null,
            "Bathroom needs the litter tray dig activity.", report);
        Require(litterDig == null ||
                litterDig.StoreProductId == HomeStoreService.BathroomLitterBoxId,
            "The litter dig activity must be gated on owning BathroomLitterBox.", report);
        GroomBrushActivity groomBrush = FindInScene<GroomBrushActivity>(scene);
        Require(groomBrush != null,
            "Bathroom needs the grooming cart brush activity.", report);
        Require(groomBrush == null ||
                groomBrush.StoreProductId == HomeStoreService.BathroomGroomingCartId,
            "The groom brush activity must be gated on owning BathroomGroomingCart.", report);
        HamperDiveActivity hamperDive = FindInScene<HamperDiveActivity>(scene);
        Require(hamperDive != null,
            "Bathroom needs the laundry hamper dive activity.", report);
        Require(hamperDive == null ||
                hamperDive.StoreProductId == HomeStoreService.BathroomLaundryHamperId,
            "The hamper dive activity must be gated on owning BathroomLaundryHamper.", report);
        MatKneadActivity matKnead = FindInScene<MatKneadActivity>(scene);
        Require(matKnead != null,
            "Bathroom needs the bath mat knead activity.", report);
        Require(matKnead == null ||
                matKnead.StoreProductId == HomeStoreService.BathroomBathMatId,
            "The mat knead activity must be gated on owning BathroomBathMat.", report);
        // The mirror reuses the shared sit-and-look, so it is found by kind.
        Require(FindSitLook(scene, CatActivityKind.MirrorGaze) != null,
            "Bathroom needs the wall mirror gaze activity.", report);
        TubEdgeWalkActivity tubEdge = FindInScene<TubEdgeWalkActivity>(scene);
        Require(tubEdge != null,
            "Bathroom needs the tub rim walk activity.", report);
        Require(tubEdge == null ||
                tubEdge.StoreProductId == HomeStoreService.BathroomTubId,
            "The tub edge walk activity must be gated on owning BathroomTub.", report);

        StoreProductDisplay[] products = FindAllInScene<StoreProductDisplay>(scene);
        var authoredProductIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < products.Length; i++)
        {
            StoreProductDisplay display = products[i];
            if (!HomeStoreService.IsBathroomCollectionProduct(display.ProductId))
                continue;
            Require(authoredProductIds.Add(display.ProductId),
                "Bathroom scene contains duplicate store product '" + display.ProductId + "'.",
                report);
            Renderer[] renderers = display.GetComponentsInChildren<Renderer>(true);
            Require(!renderers.Any(renderer => renderer.gameObject.activeInHierarchy),
                "Bathroom store product '" + display.ProductId +
                "' must stay hidden until acquired.", report);
        }
        Require(authoredProductIds.Count == HomeStoreService.BathroomCollection.Count,
            "Bathroom must author all ten hidden ROOM products.", report);
        for (int i = 0; i < HomeStoreService.BathroomCollection.Count; i++)
        {
            Require(authoredProductIds.Contains(HomeStoreService.BathroomCollection[i]),
                "Bathroom scene is missing store product '" +
                HomeStoreService.BathroomCollection[i] + "'.", report);
        }

        BathroomUsablePlaceholder[] usables =
            FindAllInScene<BathroomUsablePlaceholder>(scene);
        Require(usables.Length >= 4,
            "Bathroom needs bath, sink, grooming and litter usable anchors.", report);
        var usableIds = new HashSet<string>(StringComparer.Ordinal);
        var kinds = new HashSet<BathroomUsableKind>();
        for (int i = 0; i < usables.Length; i++)
        {
            Require(usables[i].IsConfigured,
                "Bathroom usable '" + usables[i].name + "' is incomplete.", report);
            Require(usableIds.Add(usables[i].UsableId),
                "Duplicate Bathroom usable id '" + usables[i].UsableId + "'.", report);
            kinds.Add(usables[i].Kind);
        }
        Require(kinds.Contains(BathroomUsableKind.Bath) &&
                kinds.Contains(BathroomUsableKind.Sink) &&
                kinds.Contains(BathroomUsableKind.Grooming) &&
                kinds.Contains(BathroomUsableKind.LitterBox),
            "Bathroom usable anchors do not cover all planned activities.", report);
    }

    private static void ValidateKitchenRoom(Scene scene, LevelValidationReport report)
    {
        Require(FindNamedInScene(scene, "Sunshine Checker Floor") != null &&
                FindNamedInScene(scene, "Candy Backsplash Ribbon") != null,
            "Kitchen needs its sunshine floor and candy backsplash ribbon.", report);
        Require(FindNamedInScene(scene, "Kitchen Sunrise Window") != null &&
                FindNamedInScene(scene, "Kitchen Paw Medallion") != null,
            "Kitchen needs its sunrise window and paw wall medallion.", report);

        // All ten Kitchen products ship a cat routine. Three of them reuse a
        // shared class (SitLookActivity twice, SinkSipActivity, MatKneadActivity,
        // PerchNapActivity twice), so those are found by kind rather than by
        // type — otherwise the second instance of a shared class is invisible
        // to a FindInScene<T> that returns the first hit.
        RequireRoomActivity(scene, CatActivityKind.IslandPerch,
            HomeStoreService.KitchenIslandId, "island counter perch", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.StoolPerch,
            HomeStoreService.KitchenCounterStoolId, "counter stool perch", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.FridgeStare,
            HomeStoreService.KitchenRefrigeratorId, "refrigerator stare", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.FruitSwat,
            HomeStoreService.KitchenFruitBasketId, "fruit basket swat", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.PantryClimb,
            HomeStoreService.KitchenPantryShelfId, "pantry climb", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.OvenWarmth,
            HomeStoreService.KitchenStoveOvenId, "oven basking", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.CartNudge,
            HomeStoreService.KitchenDishCartId, "dish cart nudge", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.MealTime,
            HomeStoreService.KitchenFeedingStationId, "feeding station meal", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.KitchenSip,
            HomeStoreService.KitchenSinkCabinetId, "kitchen sink sip", "Kitchen", report);
        RequireRoomActivity(scene, CatActivityKind.KitchenMatKnead,
            HomeStoreService.KitchenPawMatId, "kitchen runner knead", "Kitchen", report);

        StoreProductDisplay[] products = FindAllInScene<StoreProductDisplay>(scene);
        var authoredProductIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < products.Length; i++)
        {
            StoreProductDisplay display = products[i];
            if (!HomeStoreService.IsKitchenCollectionProduct(display.ProductId))
                continue;
            Require(authoredProductIds.Add(display.ProductId),
                "Kitchen scene contains duplicate store product '" + display.ProductId + "'.",
                report);
            Renderer[] renderers = display.GetComponentsInChildren<Renderer>(true);
            Require(!renderers.Any(renderer => renderer.gameObject.activeInHierarchy),
                "Kitchen store product '" + display.ProductId +
                "' must stay hidden until acquired.", report);
        }
        Require(authoredProductIds.Count == HomeStoreService.KitchenCollection.Count,
            "Kitchen must author all ten hidden ROOM products.", report);
        for (int i = 0; i < HomeStoreService.KitchenCollection.Count; i++)
        {
            Require(authoredProductIds.Contains(HomeStoreService.KitchenCollection[i]),
                "Kitchen scene is missing store product '" +
                HomeStoreService.KitchenCollection[i] + "'.", report);
        }
    }

    private static void ValidateBedroomRoom(Scene scene, LevelValidationReport report)
    {
        // All ten Bedroom products ship a cat routine, and most reuse a shared
        // class, so each is found by kind.
        RequireRoomActivity(scene, CatActivityKind.BedNap,
            HomeStoreService.BedroomQueenBedId, "queen bed nap", "Bedroom", report);
        RequireRoomActivity(scene, CatActivityKind.WardrobeScratch,
            HomeStoreService.BedroomWardrobeId, "wardrobe scratch", "Bedroom", report);
        RequireRoomActivity(scene, CatActivityKind.DaybedWatch,
            HomeStoreService.BedroomWindowDaybedId, "daybed window watch", "Bedroom",
            report);
        RequireRoomActivity(scene, CatActivityKind.KnockOff,
            HomeStoreService.BedroomNightstandId, "nightstand knock off", "Bedroom",
            report);
        RequireRoomActivity(scene, CatActivityKind.VanityStoolNap,
            HomeStoreService.BedroomVanityStoolId, "vanity stool nap", "Bedroom",
            report);
        RequireRoomActivity(scene, CatActivityKind.YarnSwat,
            HomeStoreService.BedroomYarnBasketId, "yarn basket pounce", "Bedroom",
            report);
        RequireRoomActivity(scene, CatActivityKind.NightLightGaze,
            HomeStoreService.BedroomNightLightId, "night light gaze", "Bedroom", report);
        RequireRoomActivity(scene, CatActivityKind.ArtGaze,
            HomeStoreService.BedroomDreamArtId, "dream art gaze", "Bedroom", report);
        RequireRoomActivity(scene, CatActivityKind.BedroomMatKnead,
            HomeStoreService.BedroomPawRugId, "bedside rug knead", "Bedroom", report);
        RequireRoomActivity(scene, CatActivityKind.CanopyNap,
            HomeStoreService.BedroomStarCanopyId, "star canopy nap", "Bedroom", report);

        Require(FindNamedInScene(scene, "Dreamy Wood Floor") != null &&
                FindNamedInScene(scene, "Moonlight Checker Floor") == null &&
                FindNamedInScene(scene, "Starlight Wall Ribbon") != null,
            "Bedroom needs its wood-plank floor and starlight wall ribbon.", report);
        Require(FindNamedInScene(scene, "Bedroom Moon Window") != null &&
                FindNamedInScene(scene, "Bedroom Paw Medallion") != null,
            "Bedroom needs its moon window and paw wall medallion.", report);
        CanopyNapActivity canopyNap = FindInScene<CanopyNapActivity>(scene);
        Require(canopyNap != null,
            "Bedroom needs the star tipi nap activity.", report);
        Require(canopyNap == null || canopyNap.StoreProductId == HomeStoreService.BedroomStarCanopyId,
            "The star tipi nap activity must be gated on owning BedroomStarCanopy.", report);

        StoreProductDisplay[] products = FindAllInScene<StoreProductDisplay>(scene);
        var authoredProductIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < products.Length; i++)
        {
            StoreProductDisplay display = products[i];
            if (!HomeStoreService.IsBedroomCollectionProduct(display.ProductId))
                continue;
            Require(authoredProductIds.Add(display.ProductId),
                "Bedroom scene contains duplicate store product '" + display.ProductId + "'.",
                report);
            Renderer[] renderers = display.GetComponentsInChildren<Renderer>(true);
            Require(!renderers.Any(renderer => renderer.gameObject.activeInHierarchy),
                "Bedroom store product '" + display.ProductId +
                "' must stay hidden until acquired.", report);
        }
        Require(authoredProductIds.Count == HomeStoreService.BedroomCollection.Count,
            "Bedroom must author all ten hidden ROOM products.", report);
        for (int i = 0; i < HomeStoreService.BedroomCollection.Count; i++)
        {
            Require(authoredProductIds.Contains(HomeStoreService.BedroomCollection[i]),
                "Bedroom scene is missing store product '" +
                HomeStoreService.BedroomCollection[i] + "'.", report);
        }
    }


    /// <summary>
    /// Balcony v1 shipped without cat routines on purpose. The wave-3 decision
    /// brings it up to the same contract as every other room, so the products
    /// are checked here the way the Garden's are.
    /// </summary>
    private static void ValidateBalconyRoom(Scene scene, LevelValidationReport report)
    {
        RequireRoomActivity(scene, CatActivityKind.AwningGaze,
            HomeStoreService.BalconySunAwningId, "sun awning gaze", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.HerbShelfClimb,
            HomeStoreService.BalconyHerbShelfId, "herb shelf climb", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.FeederShake,
            HomeStoreService.BalconyBirdFeederId, "bird feeder shake", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.EggChairNap,
            HomeStoreService.BalconyHangingChairId, "hanging chair nap", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.BenchNap,
            HomeStoreService.BalconyCushionBenchId, "cushion bench nap", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.TableKnockOff,
            HomeStoreService.BalconySideTableId, "side table knock off", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.SunMatBask,
            HomeStoreService.BalconySunMatId, "sun mat bask", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.PlanterDig,
            HomeStoreService.BalconyPlanterBoxId, "planter box dig", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.RailingSwat,
            HomeStoreService.BalconyRailingFlowersId, "railing flowers swat", "Balcony", report);
        RequireRoomActivity(scene, CatActivityKind.LanternGaze,
            HomeStoreService.BalconyLanternStringId, "lantern string gaze", "Balcony", report);
    }
    /// <summary>
    /// Patio v1 shipped with one routine — the porch swing ride — and nine
    /// products that were furniture and nothing else. Wave 3 gives every one of
    /// them a beat, so the room is checked the way the Garden and Balcony are.
    /// </summary>
    /// <summary>
    /// The loft shipped as ten pieces of furniture with no cat behaviour at
    /// all. Wave 3 gives every one of them a beat, so the room is checked the
    /// way the Garden, Balcony and Patio are. Nine of the ten share an activity
    /// class with another room, which is exactly why every lookup here is by
    /// KIND and not by component type.
    /// </summary>
    private static void ValidateSecondFloorRoom(Scene scene, LevelValidationReport report)
    {
        RequireRoomActivity(scene, CatActivityKind.RunnerKnead,
            HomeStoreService.LoftFloorRunnerId, "floor runner knead", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.CushionNest,
            HomeStoreService.LoftFloorCushionsId, "floor cushions nest", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.BookKnockOff,
            HomeStoreService.LoftBookStackId, "book stack knock off", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.LampGlowBask,
            HomeStoreService.LoftArcLampId, "arc lamp glow bask", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.BeanBagNap,
            HomeStoreService.LoftBeanBagId, "bean bag nap", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.RecordSpin,
            HomeStoreService.LoftRecordPlayerId, "record player spin", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.DeskPerch,
            HomeStoreService.LoftStudyDeskId, "study desk perch", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.GalleryGaze,
            HomeStoreService.LoftWallGalleryId, "wall gallery gaze", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.BookcaseClimb,
            HomeStoreService.LoftTallBookcaseId, "tall bookcase climb", "Second Floor", report);
        RequireRoomActivity(scene, CatActivityKind.ChaiseNap,
            HomeStoreService.LoftChaiseLoungeId, "chaise lounge nap", "Second Floor", report);
    }

    private static void ValidatePatioRoom(Scene scene, LevelValidationReport report)
    {
        RequireRoomActivity(scene, CatActivityKind.ArchClimb,
            HomeStoreService.PatioPergolaArchId, "pergola arch climb", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.SwingRide,
            HomeStoreService.PatioPorchSwingId, "porch swing ride", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.ParasolScratch,
            HomeStoreService.PatioParasolId, "parasol scratch", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.DiningPerch,
            HomeStoreService.PatioDiningSetId, "dining set perch", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.FirePitBask,
            HomeStoreService.PatioFirePitId, "fire pit bask", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.FountainSip,
            HomeStoreService.PatioWaterFountainId, "water fountain sip", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.FernWatch,
            HomeStoreService.PatioPottedFernsId, "potted ferns watch", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.HerbTroughDig,
            HomeStoreService.PatioHerbTroughId, "herb trough dig", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.FestoonGaze,
            HomeStoreService.PatioStringLightsId, "string lights gaze", "Patio", report);
        RequireRoomActivity(scene, CatActivityKind.StoneRugKnead,
            HomeStoreService.PatioStoneRugId, "stone rug knead", "Patio", report);
    }

    private static void ValidateGardenRoom(Scene scene, LevelValidationReport report)
    {
        Require(FindNamedInScene(scene, "Sunny Lawn") != null &&
                FindNamedInScene(scene, "GrassBlanket") != null &&
                FindNamedInScene(scene, "LawnTile_0_0") == null,
            "Garden lawn must be a continuous grass field, not checker tiles.", report);
        Require(FindNamedInScene(scene, "Sunny Garden Fence") != null &&
                FindNamedInScene(scene, "Garden Gate") != null &&
                FindNamedInScene(scene, "Courtyard Tree") != null &&
                FindNamedInScene(scene, "Open Sky") != null &&
                FindNamedInScene(scene, "Distant Path") != null &&
                FindNamedInScene(scene, "Garden Flower Border") != null,
            "Garden needs an open courtyard: fence, gate, tree, sky, path and flower border.", report);
        Require(FindInScene<GardenBirdFlock>(scene) != null &&
                FindInScene<GardenBirdAttention>(scene) != null &&
                FindInScene<GardenAmbientCritters>(scene) != null,
            "Garden needs visiting birds plus tiny bees and butterflies.", report);
        Require(FindSitLook(scene, CatActivityKind.BirdWatch) != null,
            "Garden needs a Bond-gated bird-watch activity.", report);
        GardenBirdFlock flock = FindInScene<GardenBirdFlock>(scene);
        Require(flock == null || flock.BirdCount <= 2,
            "Garden should keep only a couple of visiting birds.", report);

        StoreProductDisplay[] products = FindAllInScene<StoreProductDisplay>(scene);
        var authoredProductIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < products.Length; i++)
        {
            StoreProductDisplay display = products[i];
            if (!HomeStoreService.IsGardenCollectionProduct(display.ProductId))
                continue;
            Require(authoredProductIds.Add(display.ProductId),
                "Garden scene contains duplicate store product '" + display.ProductId + "'.",
                report);
            Renderer[] renderers = display.GetComponentsInChildren<Renderer>(true);
            Require(!renderers.Any(renderer => renderer.gameObject.activeInHierarchy),
                "Garden store product '" + display.ProductId +
                "' must stay hidden until acquired.", report);
        }
        Require(authoredProductIds.Count == HomeStoreService.GardenCollection.Count,
            "Garden must author all ten hidden ROOM products.", report);
        for (int i = 0; i < HomeStoreService.GardenCollection.Count; i++)
        {
            Require(authoredProductIds.Contains(HomeStoreService.GardenCollection[i]),
                "Garden scene is missing store product '" +
                HomeStoreService.GardenCollection[i] + "'.", report);
        }

        RequireRoomActivity(scene, CatActivityKind.PergolaClimb,
            HomeStoreService.GardenPergolaId, "pergola climb", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.TreeScratch,
            HomeStoreService.GardenSaplingId, "sapling scratch", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.BistroPerch,
            HomeStoreService.GardenBistroSetId, "bistro table perch", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.HammockSway,
            HomeStoreService.GardenHammockId, "hammock sway", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.SunBask,
            HomeStoreService.GardenSunLoungerId, "sun lounger bask", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.GrillWatch,
            HomeStoreService.GardenGrillId, "grill stare", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.BirdBathSip,
            HomeStoreService.GardenBirdBathId, "bird bath sip", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.PotDig,
            HomeStoreService.GardenFlowerPotsId, "flower pot dig", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.DaisyRoll,
            HomeStoreService.GardenDaisyBedId, "daisy bed knead", "Garden", report);
        RequireRoomActivity(scene, CatActivityKind.YarnBallChase,
            HomeStoreService.GardenYarnBallId, "yarn ball chase", "Garden", report);
    }

    private static void ValidateScene(
        string path,
        Action<Scene> validate,
        LevelValidationReport report)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            report.Errors.Add($"Scene asset '{path}' is missing.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(path);
        bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
        if (openedForValidation)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

        try
        {
            validate(scene);
        }
        finally
        {
            if (openedForValidation && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateMissingScripts(Scene scene, LevelValidationReport report)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    transforms[i].gameObject);
                if (missing > 0)
                    report.Errors.Add(
                        $"Scene '{scene.path}' object '{transforms[i].name}' has {missing} missing script(s)."
                    );
            }
        }
    }

    private static void ValidateAuthoringHierarchy(
        Scene scene,
        string scenePath,
        LevelValidationReport report)
    {
        string[] groupNames =
        {
            CatHomeAuthoringWorkspace.EnvironmentGroupName,
            CatHomeAuthoringWorkspace.FurnitureGroupName,
            CatHomeAuthoringWorkspace.CharacterGroupName,
            CatHomeAuthoringWorkspace.GameplayGroupName,
            CatHomeAuthoringWorkspace.PresentationGroupName,
            CatHomeAuthoringWorkspace.LocalUiGroupName,
            CatHomeAuthoringWorkspace.LevelSetupGroupName
        };

        var rootNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (GameObject root in scene.GetRootGameObjects())
            rootNames.Add(root.name);

        for (int i = 0; i < groupNames.Length; i++)
        {
            Require(
                rootNames.Contains(groupNames[i]),
                $"Level scene '{scenePath}' is missing hierarchy group '{groupNames[i]}'.",
                report);
        }
    }

    /// <summary>
    /// Finds an activity by KIND rather than by type. Both the Kitchen and the
    /// Bedroom need this: most of their routines come from a handful of shared
    /// classes, and a FindInScene&lt;T&gt; would only ever see the first
    /// instance of each.
    /// </summary>
    private static void RequireRoomActivity(
        Scene scene, CatActivityKind kind, string productId, string label,
        string room, LevelValidationReport report)
    {
        CatActivity[] activities = FindAllInScene<CatActivity>(scene);
        CatActivity found = null;
        for (int i = 0; i < activities.Length; i++)
        {
            if (activities[i] != null && activities[i].Kind == kind)
                found = activities[i];
        }

        Require(found != null, room + " needs the " + label + " activity.", report);
        Require(found == null || found.StoreProductId == productId,
            "The " + label + " activity must be gated on owning '" + productId + "'.",
            report);
    }

    private static SitLookActivity FindSitLook(Scene scene, CatActivityKind kind)
    {
        SitLookActivity[] activities = FindAllInScene<SitLookActivity>(scene);
        for (int i = 0; i < activities.Length; i++)
        {
            if (activities[i] != null && activities[i].Kind == kind)
                return activities[i];
        }

        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        T[] components = FindAllInScene<T>(scene);
        return components.Length > 0 ? components[0] : null;
    }

    private static GameObject FindNamedInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i].name == objectName)
                    return transforms[i].gameObject;
        }

        return null;
    }

    private static Transform FindNamedInChildren(Transform root, string objectName)
    {
        if (root == null)
            return null;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
            if (transforms[i].name == objectName)
                return transforms[i];
        return null;
    }

    private static GameObject FindRootInScene(Scene scene, string rootName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
                return root;
        }

        return null;
    }

    private static T[] FindAllInScene<T>(Scene scene) where T : Component
    {
        var components = new List<T>();
        foreach (GameObject root in scene.GetRootGameObjects())
            components.AddRange(root.GetComponentsInChildren<T>(true));
        return components.ToArray();
    }

    private static void Require(bool condition, string message, LevelValidationReport report)
    {
        if (!condition)
            report.Errors.Add(message);
    }
}
