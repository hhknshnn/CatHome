using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class PremiumPresentationTests
{
    private const string TitlePrefab = "Assets/UI/TitleScreen.prefab";
    private const string TitleHero = "Assets/Art/Title/CatHome_TitleHero_v1.png";
    private const string TitleLogo = "Assets/Art/Title/CatHome_MainMenuLogo_v1.png";
    private const string ShopCard = "Assets/Art/Title/MainMenu_ShopCard_v1.png";
    private const string RoomsCard = "Assets/Art/Title/MainMenu_RoomsCard_v1.png";
    private const string GamesCard = "Assets/Art/Title/MainMenu_GamesCard_v1.png";

    [Test]
    public void TitleScreen_UsesTheCinematicHeroAndSharedPremiumInteraction()
    {
        Texture2D hero = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleHero);
        Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleLogo);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefab);
        Assert.That(hero, Is.Not.Null, TitleHero);
        Assert.That(logo, Is.Not.Null, TitleLogo);
        Assert.That(prefab, Is.Not.Null, TitlePrefab);

        bool foundHero = false;
        bool foundLogo = false;
        foreach (RawImage image in prefab.GetComponentsInChildren<RawImage>(true))
        {
            if (image.texture == hero)
                foundHero = true;
            if (image.texture == logo)
                foundLogo = true;
        }
        Assert.That(foundHero, Is.True,
            "The first impression must keep the authored Cat Home hero background.");
        Assert.That(foundLogo, Is.True,
            "The main menu must keep the Blender-authored CAT HOME emblem.");
        Assert.That(prefab.transform.Find("SafeArea/MainMenuShortcuts/ShopShortcut"), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/MainMenuShortcuts/RoomsShortcut"), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/MainMenuShortcuts/GamesShortcut"), Is.Not.Null);
        AssertCardUsesTexture(prefab, "ShopShortcut", ShopCard);
        AssertCardUsesTexture(prefab, "RoomsShortcut", RoomsCard);
        AssertCardUsesTexture(prefab, "GamesShortcut", GamesCard);
        Assert.That(prefab.GetComponentInChildren<TitleCatPreview>(true), Is.Null,
            "The main menu must not reserve an empty live-cat portrait well.");
        TitleLogoNeonFx neonFx = prefab.GetComponentInChildren<TitleLogoNeonFx>(true);
        Assert.That(neonFx, Is.Not.Null,
            "The Blender title emblem must keep its animated neon presentation layer.");
        var neonSerialized = new SerializedObject(neonFx);
        Assert.That(neonSerialized.FindProperty("auraGroup").objectReferenceValue, Is.Not.Null);
        Assert.That(neonSerialized.FindProperty("pulseRoot").objectReferenceValue, Is.Not.Null);
        Assert.That(neonSerialized.FindProperty("logoGraphic").objectReferenceValue, Is.Not.Null);
        Assert.That(neonSerialized.FindProperty("sparkleRoot").objectReferenceValue, Is.Not.Null);
        Transform lightOrbit = prefab.transform.Find(
            "SafeArea/BrandDockLayout/BrandDock/LogoLightOrbit");
        Assert.That(lightOrbit, Is.Not.Null);
        Assert.That(lightOrbit.GetComponent<PremiumAmbientSparkle>(), Is.Null,
            "The neon orbit must have one transform writer: TitleLogoNeonFx.");
        foreach (string shortcut in new[] { "ShopShortcut", "RoomsShortcut", "GamesShortcut" })
        {
            Transform badge = prefab.transform.Find(
                "SafeArea/MainMenuShortcuts/" + shortcut + "/Visual/AfterTourBadge");
            Assert.That(badge, Is.Not.Null, shortcut + " must advertise its first-tour lock.");
        }
        TitleScreen titleScreen = prefab.GetComponent<TitleScreen>();
        Assert.That(titleScreen, Is.Not.Null);
        var titleSerialized = new SerializedObject(titleScreen);
        Assert.That(titleSerialized.FindProperty("shopAfterTourBadge").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("roomsAfterTourBadge").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("gamesAfterTourBadge").objectReferenceValue,
            Is.Not.Null);
        Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/Title/Models/CatHome_MainMenuLogo.fbx"), Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<LowPolyPanelGraphic>(true).Length,
            Is.GreaterThanOrEqualTo(12));
        foreach (Button button in prefab.GetComponentsInChildren<Button>(true))
        {
            string upper = button.name.ToUpperInvariant();
            if (upper.Contains("SCRIM") || upper.Contains("BLOCKER"))
                continue;
            Assert.That(button.GetComponent<PremiumButtonFx>(), Is.Not.Null, button.name);
        }
    }

    [Test]
    public void TitleAndSettings_KeepConfirmedNewGameAndBilingualBindings()
    {
        GameObject titlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefab);
        Assert.That(titlePrefab, Is.Not.Null);
        Assert.That(titlePrefab.transform.Find(
            "SafeArea/BrandDockLayout/BrandDock/NewGameButton"), Is.Not.Null);
        Assert.That(titlePrefab.transform.Find("SafeArea/NewGameOverlay/NewGameCard"), Is.Not.Null);
        TitleScreen title = titlePrefab.GetComponent<TitleScreen>();
        var titleSerialized = new SerializedObject(title);
        Assert.That(titleSerialized.FindProperty("newGameButton").objectReferenceValue, Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("newGameCancelButton").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("newGameConfirmButton").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("newGameGroup").objectReferenceValue, Is.Not.Null);
        Assert.That(titlePrefab.GetComponentsInChildren<LocalizedLabel>(true).Length,
            Is.GreaterThanOrEqualTo(16));

        GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UI/SettingsPanel.prefab");
        Assert.That(settingsPrefab, Is.Not.Null);
        Assert.That(settingsPrefab.transform.Find(
            "SafeArea/SettingsPanelVisual/Row_language"), Is.Not.Null);
        SettingsPanel settings = settingsPrefab.GetComponent<SettingsPanel>();
        var settingsSerialized = new SerializedObject(settings);
        Assert.That(settingsSerialized.FindProperty("rows").arraySize, Is.EqualTo(6));
        Assert.That(settingsPrefab.GetComponentsInChildren<LocalizedLabel>(true).Length,
            Is.GreaterThanOrEqualTo(9));
    }

    [Test]
    public void PremiumFurnitureKit_IsConnectedToTheVisibleRoomProducts()
    {
        var products = new Dictionary<string, string>
        {
            { "LoftArcLamp", "LoftArcLamp_Premium" },
            { "LoftBeanBag", "LoftBeanBag_Premium" },
            { "LoftRecordPlayer", "LoftRecordPlayer_Premium" },
            { "LoftStudyDesk", "LoftStudyDesk_Premium" },
            { "LoftTallBookcase", "LoftTallBookcase_Premium" },
            { "LoftChaiseLounge", "LoftChaiseLounge_Premium" },
            { "BalconyCushionBench", "BalconyCushionBench_Premium" },
            { "BalconyHangingChair", "BalconyHangingChair_Premium" },
            { "PatioWaterFountain", "PatioWaterFountain_Premium" },
            { "PatioFirePit", "PatioFirePit_Premium" },
            { "PatioParasol", "PatioParasol_Premium" },
            { "PatioDiningSet", "PatioDiningSet_Premium" },
            { "BathroomTub", "BathroomTub_Premium" },
            { "BathroomVanitySink", "BathroomVanitySink_Premium" },
            { "BathroomToilet", "BathroomToilet_Premium" },
            { "KitchenIsland", "KitchenIsland_Premium" },
            { "KitchenRefrigerator", "KitchenRefrigerator_Premium" },
            { "KitchenStoveOven", "KitchenStoveOven_Premium" },
            { "KitchenPantryShelf", "KitchenPantryShelf_Premium" },
            { "BedroomQueenBed", "BedroomQueenBed_Premium" },
            { "BedroomWardrobe", "BedroomWardrobe_Premium" },
            { "BedroomWindowDaybed", "BedroomWindowDaybed_Premium" },
            { "GardenPergola", "GardenPergola_Premium" },
            { "GardenSunLounger", "GardenSunLounger_Premium" },
            { "GardenBistroSet", "GardenBistroSet_Premium" },
            { "GardenHammock", "GardenHammock_Premium" }
        };

        foreach (KeyValuePair<string, string> pair in products)
        {
            string modelPath = "Assets/Art/PremiumFurniture/Models/" + pair.Value + ".fbx";
            string prefabPath = "Assets/Art/StoreProducts/Prefabs/" + pair.Key + ".prefab";
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), Is.Not.Null, modelPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);
            Assert.That(ContainsTransformName(prefab.transform, pair.Key + "_PremiumModel"),
                Is.True, prefabPath + " must use the Blender-authored premium visual.");
        }
    }

    [Test]
    public void GamesHubHeroArt_RemainsAvailableForBothGames()
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Art/Runner/UI/CatRunnerHero_v1.png"), Is.Not.Null);
        Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Art/Catch/UI/CatCatchHero_v1.png"), Is.Not.Null);
    }

    private static bool ContainsTransformName(Transform root, string expected)
    {
        if (root.name == expected)
            return true;
        for (int i = 0; i < root.childCount; i++)
        {
            if (ContainsTransformName(root.GetChild(i), expected))
                return true;
        }
        return false;
    }

    private static void AssertCardUsesTexture(GameObject prefab, string cardName, string path)
    {
        Texture2D expected = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Assert.That(expected, Is.Not.Null, path);
        Transform card = prefab.transform.Find("SafeArea/MainMenuShortcuts/" + cardName);
        Assert.That(card, Is.Not.Null, cardName);
        bool found = false;
        foreach (RawImage image in card.GetComponentsInChildren<RawImage>(true))
            found |= image.texture == expected;
        Assert.That(found, Is.True, cardName + " must use its matching Blender diorama.");
    }
}
