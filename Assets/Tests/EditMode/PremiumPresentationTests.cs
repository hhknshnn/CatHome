using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class PremiumPresentationTests
{
    private const string TitlePrefab = "Assets/UI/TitleScreen.prefab";
    private const string TitleHero = TitleShowcaseContentBuilder.PosterPath;
    private const string TitleLogo = "Assets/Art/Title/CatHome_MainMenuLogo_v1.png";
    private const string ShopCard = "Assets/Art/Title/MainMenu_ShopCard_v1.png";
    private const string RoomsCard = "Assets/Resources/PremiumInterface/Rooms.png";
    private const string GamesCard = "Assets/Resources/PremiumInterface/Games.png";

    [TestCase("FredokaDisplay")]
    [TestCase("FredokaEmphasis")]
    public void TurkishHeadings_UseTheirOwnGlyphsWithoutFallback(string name)
    {
        var font=Resources.Load<TMPro.TMP_FontAsset>("Typography/"+name);
        Assert.That(font,Is.Not.Null);
        Assert.That(new SerializedObject(font).FindProperty("m_ClearDynamicDataOnBuild").boolValue,Is.False,
            "Building must preserve the pre-baked Turkish atlas: "+name);
        foreach(char letter in "ĞğİıŞşÇçÖöÜü")
            Assert.That(font.HasCharacter(letter,false,false),Is.True,name+" missing "+letter);
    }

    [Test]
    public void TitleScreen_UsesLiveGameCatsAndSharedPremiumInteraction()
    {
        Texture2D hero = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleHero);
        Texture2D logo = AssetDatabase.LoadAssetAtPath<Texture2D>(TitleLogo);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefab);
        Assert.That(hero, Is.Not.Null, TitleHero);
        Assert.That(logo, Is.Not.Null, TitleLogo);
        Assert.That(prefab, Is.Not.Null, TitlePrefab);

        bool foundHero = false;
        foreach (RawImage image in prefab.GetComponentsInChildren<RawImage>(true))
        {
            if (image.texture == hero)
                foundHero = true;
        }
        Assert.That(foundHero, Is.True,
            "The fallback must show the real game cats in the HD title set.");
        Assert.That(hero.width, Is.GreaterThanOrEqualTo(1920));
        Assert.That(hero.height, Is.GreaterThanOrEqualTo(1080));
        var showcase = prefab.GetComponentInChildren<TitleCatShowcase>(true);
        Assert.That(showcase, Is.Not.Null);
        var presentation = new SerializedObject(showcase);
        Assert.That(presentation.FindProperty("stagePrefab").objectReferenceValue, Is.Not.Null);
        Assert.That(presentation.FindProperty("catalog").objectReferenceValue, Is.EqualTo(CatBreedCatalog.Load()));
        Assert.That(prefab.GetComponentInChildren<TitleScreenLayout>(true), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/BrandDockLayout/Wordmark"),Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/MainMenuShortcuts/ShopShortcut"), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/MainMenuShortcuts/RoomsShortcut"), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/MainMenuShortcuts/GamesShortcut"), Is.Not.Null);
        Assert.That(prefab.transform.Find("SafeArea/MainMenuShortcuts/ShopShortcut")
            .GetComponentInChildren<SelectedCatPortrait>(true), Is.Null,
            "The approved title uses a fixed UI cat symbol, not the selected breed portrait.");
        AssertCardUsesTexture(prefab, "ShopShortcut", StorybookTitleBuilder.CatIconPath);
        AssertCardUsesTexture(prefab, "RoomsShortcut", StorybookTitleBuilder.ActionIconPath);
        AssertCardUsesTexture(prefab, "GamesShortcut", StorybookTitleBuilder.ActionIconPath);
        Assert.That(prefab.GetComponentInChildren<TitleCatPreview>(true), Is.Null,
            "The main menu must not reserve an empty live-cat portrait well.");
        Assert.That(prefab.GetComponentInChildren<TitleLogoNeonFx>(true),Is.Null,
            "The approved wordmark is kept crisp; the live cats supply the motion.");
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
            "SafeArea/BrandDockLayout/NewGameButton"), Is.Not.Null);
        Assert.That(titlePrefab.transform.Find("SafeArea/NewGameOverlay/NewGameCard"), Is.Not.Null);
        Assert.That(titlePrefab.transform.Find(
            "SafeArea/AccountChoiceOverlay/AccountChoiceCard"), Is.Not.Null);
        Transform googleMark = titlePrefab.transform.Find(
            "SafeArea/AccountChoiceOverlay/AccountChoiceCard/GoogleSignInButton/Visual/GoogleBrandMark");
        Assert.That(googleMark, Is.Not.Null);
        RawImage googleImage = googleMark.GetComponent<RawImage>();
        Assert.That(googleImage, Is.Not.Null);
        Assert.That(googleImage.texture, Is.EqualTo(AssetDatabase.LoadAssetAtPath<Texture2D>(
            "Assets/Art/Title/Google/GoogleSignIn_G_Square_Light.png")));
        Assert.That(googleMark.GetComponent<RectTransform>().rect.width,
            Is.EqualTo(googleMark.GetComponent<RectTransform>().rect.height).Within(.01f));
        TitleScreen title = titlePrefab.GetComponent<TitleScreen>();
        var titleSerialized = new SerializedObject(title);
        Assert.That(titleSerialized.FindProperty("newGameButton").objectReferenceValue, Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("newGameCancelButton").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("newGameConfirmButton").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("newGameGroup").objectReferenceValue, Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("accountGoogleButton").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("accountGuestButton").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("accountBackButton").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titleSerialized.FindProperty("accountChoiceGroup").objectReferenceValue,
            Is.Not.Null);
        Assert.That(titlePrefab.GetComponentsInChildren<LocalizedLabel>(true).Length,
            Is.GreaterThanOrEqualTo(16));

        GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UI/SettingsPanel.prefab");
        Assert.That(settingsPrefab, Is.Not.Null);
        Assert.That(settingsPrefab.transform.Find(
            "SafeArea/SettingsPanelVisual/Row_language"), Is.Not.Null);
        Assert.That(settingsPrefab.transform.Find(
            "SafeArea/SettingsPanelVisual/Row_account"), Is.Not.Null);
        SettingsPanel settings = settingsPrefab.GetComponent<SettingsPanel>();
        var settingsSerialized = new SerializedObject(settings);
        Assert.That(settingsSerialized.FindProperty("rows").arraySize, Is.EqualTo(7));
        Assert.That(settingsPrefab.GetComponentsInChildren<LocalizedLabel>(true).Length,
            Is.GreaterThanOrEqualTo(9));
    }

    [Test]
    public void AccountChoiceAndSettingsRows_StayInsideTheirCardsWithoutOverlap()
    {
        GameObject titlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TitlePrefab);
        Transform accountCard = titlePrefab.transform.Find(
            "SafeArea/AccountChoiceOverlay/AccountChoiceCard");
        string[] accountRows =
        {
            "AccountChoiceTitle", "AccountChoiceSubtitle", "GoogleSignInButton",
            "GuestContinueButton", "GuestNote",
            "AccountChoiceStatus", "AccountBackButton"
        };
        AssertVerticalStack(accountCard, accountRows);

        GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UI/SettingsPanel.prefab");
        Transform settingsCard = settingsPrefab.transform.Find("SafeArea/SettingsPanelVisual");
        string[] settingsRows =
        {
            "Row_music", "Row_sound", "Row_game_sound", "Row_haptics",
            "Row_reduced_motion", "Row_language", "Row_account"
        };
        foreach(string row in settingsRows) Assert.That(settingsCard.Find(row),Is.Not.Null,row);
        for(int i=0;i<settingsRows.Length;i++)
        for(int j=i+1;j<settingsRows.Length;j++)
        {
            var a=(RectTransform)settingsCard.Find(settingsRows[i]);
            var b=(RectTransform)settingsCard.Find(settingsRows[j]);
            Rect ra=new Rect(a.anchoredPosition-a.sizeDelta*.5f,a.sizeDelta);
            Rect rb=new Rect(b.anchoredPosition-b.sizeDelta*.5f,b.sizeDelta);
            Assert.That(ra.Overlaps(rb),Is.False,settingsRows[i]+" / "+settingsRows[j]);
        }
    }

    [Test]
    public void PrivacyPanel_RemainsPremiumAndConflictChoiceIsNotPlayerFacing()
    {
        GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UI/SettingsPanel.prefab");
        SettingsPanel settings = settingsPrefab.GetComponent<SettingsPanel>();
        Assert.That(new SerializedObject(settings).FindProperty("privacyDataButton")
            .objectReferenceValue, Is.Not.Null);

        GameObject privacy = AssetDatabase.LoadAssetAtPath<GameObject>(
            OnlineServicesPanelBuilder.PrivacyPrefabPath);
        GameObject conflict = AssetDatabase.LoadAssetAtPath<GameObject>(
            OnlineServicesPanelBuilder.ConflictPrefabPath);
        Assert.That(privacy, Is.Not.Null);
        Assert.That(conflict, Is.Null,
            "CONTINUE must never stop on a device/cloud choice surface.");
        Assert.That(privacy.GetComponent<PrivacyDataPanel>(), Is.Not.Null);

        string[] privacyButtons =
        {
            "PrivacyPolicyButton", "DeleteInfoButton", "DataRequestButton",
            "SyncNowButton", "DeleteCloudAccountButton"
        };
        Transform privacyCard = privacy.transform.Find("SafeArea/PrivacyDataCard");
        foreach (string name in privacyButtons)
        {
            Transform button = privacyCard.Find(name);
            Assert.That(button, Is.Not.Null, name);
            Assert.That(button.GetComponent<PremiumButtonFx>(), Is.Not.Null, name);
        }

    }

    [Test]
    public void InGameDropDown_ContainsLocalizedReturnToMainMenuAction()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/UI/MainPanel.prefab");
        Assert.That(prefab, Is.Not.Null);

        Transform row = prefab.transform.Find("SafeArea/MenuList/Row_MAIN MENU");
        Assert.That(row, Is.Not.Null);
        Assert.That(row.GetComponent<Button>(), Is.Not.Null);
        Assert.That(row.GetComponent<PremiumButtonFx>(), Is.Not.Null);

        Transform label = row.Find("Content/Label");
        Assert.That(label, Is.Not.Null);
        LocalizedLabel localized = label.GetComponent<LocalizedLabel>();
        Assert.That(localized, Is.Not.Null);
        Assert.That(new SerializedObject(localized).FindProperty("textKey").stringValue,
            Is.EqualTo("menu.main_menu"));

        SerializedProperty rows = new SerializedObject(
            prefab.GetComponent<MainPanelController>()).FindProperty("rows");
        Assert.That(rows.arraySize, Is.EqualTo(6));
        Assert.That(rows.GetArrayElementAtIndex(1).FindPropertyRelative("id").stringValue,
            Is.EqualTo("QUESTS"));
        Assert.That(rows.GetArrayElementAtIndex(5).FindPropertyRelative("id").stringValue,
            Is.EqualTo("MAIN MENU"));
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

    private static void AssertVerticalStack(Transform parent, string[] names)
    {
        Assert.That(parent, Is.Not.Null);
        var parentRect = (RectTransform)parent;
        float halfParentHeight = parentRect.rect.height * .5f;
        RectTransform previous = null;
        foreach (string name in names)
        {
            var current = parent.Find(name) as RectTransform;
            Assert.That(current, Is.Not.Null, name);
            float top = current.anchoredPosition.y + current.rect.height * .5f;
            float bottom = current.anchoredPosition.y - current.rect.height * .5f;
            Assert.That(top, Is.LessThanOrEqualTo(halfParentHeight), name + " top");
            Assert.That(bottom, Is.GreaterThanOrEqualTo(-halfParentHeight), name + " bottom");
            if (previous != null)
            {
                float previousBottom = previous.anchoredPosition.y - previous.rect.height * .5f;
                Assert.That(top, Is.LessThanOrEqualTo(previousBottom),
                    previous.name + " overlaps " + current.name);
            }
            previous = current;
        }
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
        Assert.That(found, Is.True, cardName + " must use its matching authored Blender icon.");
    }
}
