using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Authors the launch title / main menu overlay in CatHome_UI. Full-bleed candy
/// meadow, a two-column premium card, summary pills, coherent Blender-authored
/// shortcut dioramas and CREDITS. Sorting order 200. Adds no gameplay camera,
/// listener or EventSystem.
/// </summary>
public static class TitleScreenBuilder
{
    public const string RootName = "TitleScreenCanvas";
    public const string PrefabPath = "Assets/UI/TitleScreen.prefab";
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private const string HeroBackgroundPath =
        "Assets/Art/Title/CatHome_TitleHero_v1.png";
    private const string MainMenuLogoPath =
        "Assets/Art/Title/CatHome_MainMenuLogo_v1.png";
    private const string ShopShortcutPath =
        "Assets/Art/Title/MainMenu_ShopCard_v1.png";
    private const string RoomsShortcutPath =
        "Assets/Art/Title/MainMenu_RoomsCard_v1.png";
    private const string GamesShortcutPath =
        "Assets/Art/Title/MainMenu_GamesCard_v1.png";
    private const string PawCoinIconPath =
        "Assets/Art/PremiumCurrency/Icons/PawCoin_Icon.png";

    private static readonly Color Ink = PremiumUiStyle.Ink;
    private static readonly Color Cream = PremiumUiStyle.CandyCloud;
    private static readonly Color32 Gold = new Color32(255, 205, 64, 255);
    private static readonly Color32 HeaderPink = new Color32(242, 76, 140, 255);

    [MenuItem("Tools/Cat Home/UI/Build Title Screen")]
    public static void BuildBatch()
    {
        Debug.Log(BuildSilently());
    }

    public static string BuildSilently()
    {
        ConfigureHeroImporter();
        ConfigureTextureImporter(MainMenuLogoPath, true);
        ConfigureTextureImporter(ShopShortcutPath, false);
        ConfigureTextureImporter(RoomsShortcutPath, false);
        ConfigureTextureImporter(GamesShortcutPath, false);
        Scene uiScene = SceneManager.GetSceneByPath(UiScenePath);
        bool openedForBuild = !uiScene.IsValid() || !uiScene.isLoaded;
        if (openedForBuild)
            uiScene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
        if (!uiScene.IsValid())
            throw new InvalidOperationException("CatHome_UI scene could not be opened.");

        GameObject existing = FindSceneRoot(uiScene, RootName);
        GameObject root = existing != null ? PrepareRoot(existing) : CreateRoot(uiScene);
        TMP_FontAsset font = FindFont();

        Canvas canvas = GetOrAdd<Canvas>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAdd<GraphicRaycaster>(root);

        TitleScreen controller = GetOrAdd<TitleScreen>(root);
        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;

        RectTransform safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea);
        GetOrAdd<SafeAreaRect>(safeArea.gameObject);

        BuildBackdrop(safeArea, font);
        TitleScreenBindings bindings = BuildCard(safeArea, font);
        CanvasGroup creditsGroup = BuildCreditsOverlay(safeArea, font, out Button creditsClose);
        CanvasGroup newGameGroup = BuildNewGameOverlay(
            safeArea, font, out Button newGameCancel, out Button newGameConfirm,
            out TMP_Text newGameStatus);

        var serialized = new SerializedObject(controller);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "playButton", bindings.Play);
        Assign(serialized, "settingsButton", bindings.Settings);
        Assign(serialized, "creditsButton", bindings.Credits);
        Assign(serialized, "quitButton", bindings.Quit);
        Assign(serialized, "newGameButton", bindings.NewGame);
        Assign(serialized, "shopButton", bindings.Shop);
        Assign(serialized, "roomsButton", bindings.Rooms);
        Assign(serialized, "gamesButton", bindings.Games);
        Assign(serialized, "shopAfterTourBadge", bindings.ShopAfterTourBadge);
        Assign(serialized, "roomsAfterTourBadge", bindings.RoomsAfterTourBadge);
        Assign(serialized, "gamesAfterTourBadge", bindings.GamesAfterTourBadge);
        Assign(serialized, "creditsCloseButton", creditsClose);
        Assign(serialized, "newGameCancelButton", newGameCancel);
        Assign(serialized, "newGameConfirmButton", newGameConfirm);
        Assign(serialized, "greetingText", bindings.Greeting);
        Assign(serialized, "catNameText", bindings.CatName);
        Assign(serialized, "playLabel", bindings.PlayLabel);
        Assign(serialized, "homeLevelText", bindings.HomeLevel);
        Assign(serialized, "coinsText", bindings.Coins);
        Assign(serialized, "collectionText", bindings.Collection);
        Assign(serialized, "creditsGroup", creditsGroup);
        Assign(serialized, "newGameGroup", newGameGroup);
        Assign(serialized, "newGameStatusText", newGameStatus);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PremiumUiFactory.PolishHierarchy(root.transform, font);
        Validate(root);
        EnsureFolder("Assets/UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            root, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(uiScene);
        EditorSceneManager.SaveScene(uiScene);
        AssetDatabase.SaveAssets();

        if (openedForBuild)
            EditorSceneManager.CloseScene(uiScene, true);
        return "Premium Title Screen v4 built in CatHome_UI.";
    }

    private struct TitleScreenBindings
    {
        public Button Play;
        public Button Settings;
        public Button Credits;
        public Button Quit;
        public Button NewGame;
        public Button Shop;
        public Button Rooms;
        public Button Games;
        public GameObject ShopAfterTourBadge;
        public GameObject RoomsAfterTourBadge;
        public GameObject GamesAfterTourBadge;
        public TMP_Text Greeting;
        public TMP_Text CatName;
        public TMP_Text PlayLabel;
        public TMP_Text HomeLevel;
        public TMP_Text Coins;
        public TMP_Text Collection;
    }

    private static void BuildBackdrop(RectTransform safeArea, TMP_FontAsset font)
    {
        RectTransform heroRect = CreateRect("TitleHeroBackground", safeArea);
        Stretch(heroRect);
        RawImage hero = heroRect.gameObject.AddComponent<RawImage>();
        hero.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(HeroBackgroundPath);
        // The source is intentionally sunlit; a subtle neutral multiplier keeps
        // mint, coral and tabby markings rich after Unity's linear UI sampling.
        hero.color = new Color32(226, 226, 226, 255);
        hero.raycastTarget = true;
        if (hero.texture != null)
            hero.uvRect = RoomPreviewFit.CoverUv(
                hero.texture.width, hero.texture.height, 1920f, 1080f);

        // A warm glass veil gives the left-side controls stable contrast while
        // preserving the aspirational room and sunlight behind them.
        LowPolyPanelGraphic veil = CreatePanel("LeftPearlVeil", safeArea,
            new Color32(255, 247, 228, 205), 76f, 0f, false);
        SetCentered(veil.rectTransform, new Vector2(790f, 1180f), new Vector2(-610f, 0f));
        PremiumUiStyle.ConfigureShadowSurface(veil,
            new Color32(255, 247, 228, 205), 76f);

        Image lowerWarmth = CreateImage("LowerWarmth", safeArea,
            new Color32(255, 167, 112, 7), false);
        lowerWarmth.rectTransform.anchorMin = Vector2.zero;
        lowerWarmth.rectTransform.anchorMax = Vector2.one;
        lowerWarmth.rectTransform.offsetMin = lowerWarmth.rectTransform.offsetMax = Vector2.zero;

        BuildSparkle(safeArea, "SparkleA", new Vector2(-858f, 438f), 28f,
            PremiumUiStyle.CandyLemon, 18f);
        BuildSparkle(safeArea, "SparkleB", new Vector2(-728f, -438f), 22f,
            PremiumUiStyle.CandyPink, -12f);
    }

    private static TitleScreenBindings BuildCard(RectTransform safeArea, TMP_FontAsset font)
    {
        var bindings = new TitleScreenBindings();

        RectTransform layout = CreateRect("BrandDockLayout", safeArea);
        SetCentered(layout, new Vector2(520f, 1000f), new Vector2(-672f, 0f));

        LowPolyPanelGraphic glow = CreatePanel("BrandDockGlow", layout,
            new Color32(255, 199, 62, 196), 58f, 10f, false);
        SetCentered(glow.rectTransform, new Vector2(520f, 1000f), Vector2.zero);

        LowPolyPanelGraphic card = CreatePanel("BrandDock", layout, Cream, 52f, 12f, false);
        SetCentered(card.rectTransform, new Vector2(492f, 972f), Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(card,
            new Color32(255, 252, 238, 252), new Color32(255, 222, 231, 248), 52f, 12f);

        RectTransform neonRoot = CreateRect("LogoNeonAura", card.transform);
        SetCentered(neonRoot, new Vector2(482f, 404f), new Vector2(0f, 292f));
        CanvasGroup neonGroup = neonRoot.gameObject.AddComponent<CanvasGroup>();
        neonGroup.alpha = .72f;
        LowPolyPanelGraphic aquaAura = CreatePanel("NeonAquaRing", neonRoot,
            new Color32(34, 218, 255, 210), 56f, 5f, false);
        Stretch(aquaAura.rectTransform);
        PremiumUiStyle.ConfigureAccentSurface(aquaAura,
            new Color32(174, 255, 255, 220), new Color32(18, 172, 255, 224), 56f, 5f);
        LowPolyPanelGraphic mintAura = CreatePanel("NeonMintRing", neonRoot,
            new Color32(54, 255, 185, 218), 54f, 5f, false);
        SetCentered(mintAura.rectTransform, new Vector2(470f, 392f), Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(mintAura,
            new Color32(194, 255, 226, 226), new Color32(32, 224, 162, 226), 54f, 5f);
        LowPolyPanelGraphic goldAura = CreatePanel("NeonGoldRing", neonRoot,
            Gold, 53f, 5f, false);
        SetCentered(goldAura.rectTransform, new Vector2(460f, 382f), Vector2.zero);
        PremiumUiStyle.ConfigureMetalSurface(goldAura, Gold, 53f, 5f);

        LowPolyPanelGraphic logoGlow = CreatePanel("BlenderLogoGlow", card.transform,
            new Color32(163, 246, 255, 136), 52f, 5f, false);
        SetCentered(logoGlow.rectTransform, new Vector2(452f, 374f), new Vector2(0f, 292f));
        RectTransform logoRect = CreateRect("BlenderLogo", logoGlow.transform);
        StretchWithOffsets(logoRect, 7f, 7f, -7f, -7f);
        RawImage logoImage = logoRect.gameObject.AddComponent<RawImage>();
        logoImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MainMenuLogoPath);
        logoImage.color = new Color32(236, 253, 255, 255);
        logoImage.raycastTarget = false;
        // The Blender render is square and transparent; crop its authored clear
        // margin so the bright emblem, rather than empty pixels, owns the dock.
        logoImage.uvRect = new Rect(.08f, .12f, .84f, .73f);

        RectTransform logoSparkles = CreateRect("LogoLightOrbit", card.transform);
        SetCentered(logoSparkles, new Vector2(474f, 396f), new Vector2(0f, 292f));
        BuildSparkle(logoSparkles, "NeonSparkleTopLeft",
            new Vector2(-214f, 158f), 18f, new Color32(255, 240, 111, 230), -12f);
        BuildSparkle(logoSparkles, "NeonSparkleTopRight",
            new Vector2(216f, 144f), 15f, new Color32(108, 248, 255, 235), 8f);
        BuildSparkle(logoSparkles, "NeonSparkleBottomLeft",
            new Vector2(-218f, -146f), 14f, new Color32(100, 255, 197, 230), 6f);
        BuildSparkle(logoSparkles, "NeonSparkleBottomRight",
            new Vector2(214f, -154f), 17f, new Color32(255, 151, 210, 230), -8f);

        TitleLogoNeonFx neonFx = logoGlow.gameObject.AddComponent<TitleLogoNeonFx>();
        neonFx.EditorConfigure(neonGroup, neonRoot, logoImage, logoSparkles);

        bindings.Greeting = CreateText("Greeting", card.transform, font, 18f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(bindings.Greeting.rectTransform, new Vector2(432f, 34f),
            new Vector2(0f, 76f));
        bindings.Greeting.text = "YOUR COZY CAT AWAITS";
        bindings.Greeting.fontStyle = FontStyles.Bold;
        bindings.Greeting.characterSpacing = 1.8f;
        bindings.Greeting.overflowMode = TextOverflowModes.Truncate;

        LowPolyPanelGraphic nameRim = CreatePanel("WelcomeNameRim", card.transform,
            Gold, 24f, 6f, false);
        SetCentered(nameRim.rectTransform, new Vector2(440f, 66f), new Vector2(0f, 18f));
        LowPolyPanelGraphic namePlate = CreatePanel("WelcomeNamePlate", nameRim.transform,
            new Color32(255, 226, 239, 255), 20f, 5f, false);
        StretchWithOffsets(namePlate.rectTransform, 5f, 5f, -5f, -5f);
        PremiumUiStyle.ConfigureAccentSurface(namePlate,
            new Color32(255, 244, 211, 255), new Color32(255, 151, 196, 255), 20f, 5f);
        bindings.CatName = CreateText("CatName", namePlate.transform, font, 26f,
            Ink, TextAlignmentOptions.Center);
        StretchWithOffsets(bindings.CatName.rectTransform, 18f, 6f, -18f, -6f);
        bindings.CatName.text = "WELCOME HOME";
        bindings.CatName.fontStyle = FontStyles.Bold;
        bindings.CatName.characterSpacing = 1.2f;
        bindings.CatName.enableAutoSizing = true;
        bindings.CatName.fontSizeMin = 19f;
        bindings.CatName.fontSizeMax = 26f;

        bindings.HomeLevel = BuildStatPill(card.transform, font, "HomeLevelPill",
            PremiumUiStyle.CandyGrape, new Vector2(210f, 60f), new Vector2(-112f, -58f), "HOME LV. 1");
        bindings.Coins = BuildStatPill(card.transform, font, "CoinsPill",
            new Color32(255, 176, 64, 255), new Vector2(210f, 60f), new Vector2(112f, -58f), "0");
        bindings.Collection = BuildStatPill(card.transform, font, "CollectionPill",
            PremiumUiStyle.CandyPink, new Vector2(432f, 58f), new Vector2(0f, -130f),
            "0 OF " + CollectionMilestoneService.CatalogSize + " • COLLECTED");

        bindings.Play = BuildButton(card.transform, font, "PlayButton", "PLAY",
            new Vector2(432f, 108f), new Vector2(0f, -228f), 40f,
            new Color32(255, 112, 120, 255), new Color32(255, 177, 136, 255));
        bindings.PlayLabel = bindings.Play.GetComponentInChildren<TMP_Text>(true);

        bindings.NewGame = BuildButton(card.transform, font, "NewGameButton", "NEW GAME",
            new Vector2(102f, 62f), new Vector2(-171f, -340f), 14f,
            PremiumUiStyle.CandyAqua, new Color32(93, 215, 224, 255));
        Localize(bindings.NewGame.GetComponentInChildren<TMP_Text>(true), "title.new_game");
        bindings.Settings = BuildButton(card.transform, font, "SettingsButton", "SETTINGS",
            new Vector2(102f, 62f), new Vector2(-57f, -340f), 15f,
            PremiumUiStyle.CandyGrape, new Color32(190, 150, 235, 255));
        Localize(bindings.Settings.GetComponentInChildren<TMP_Text>(true), "title.settings");
        bindings.Credits = BuildButton(card.transform, font, "CreditsButton", "CREDITS",
            new Vector2(102f, 62f), new Vector2(57f, -340f), 15f,
            PremiumUiStyle.CandyBerry, new Color32(230, 140, 235, 255));
        Localize(bindings.Credits.GetComponentInChildren<TMP_Text>(true), "title.credits");
        bindings.Quit = BuildButton(card.transform, font, "QuitButton", "QUIT",
            new Vector2(102f, 62f), new Vector2(171f, -340f), 15f,
            new Color32(255, 145, 92, 255), new Color32(255, 186, 130, 255));
        Localize(bindings.Quit.GetComponentInChildren<TMP_Text>(true), "title.quit");

        TMP_Text promise = CreateText("ExperiencePromise", card.transform, font, 17f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(promise.rectTransform, new Vector2(420f, 34f), new Vector2(0f, -408f));
        promise.text = "CARE  •  DECORATE  •  PLAY";
        promise.fontStyle = FontStyles.Bold;
        promise.characterSpacing = 2.4f;
        Localize(promise, "title.promise");
        BuildSparkle(card.rectTransform, "PromiseSparkleLeft",
            new Vector2(-218f, -408f), 12f, PremiumUiStyle.CandyLemon, 0f);
        BuildSparkle(card.rectTransform, "PromiseSparkleRight",
            new Vector2(218f, -408f), 12f, PremiumUiStyle.CandyAqua, 0f);

        RectTransform shortcuts = CreateRect("MainMenuShortcuts", safeArea);
        SetCentered(shortcuts, new Vector2(316f, 900f), new Vector2(782f, 0f));
        LowPolyPanelGraphic shortcutHeader = CreatePanel("ShortcutHeader", shortcuts,
            new Color32(255, 247, 220, 246), 20f, 5f, false);
        SetCentered(shortcutHeader.rectTransform, new Vector2(306f, 48f), new Vector2(0f, 440f));
        TMP_Text shortcutTitle = CreateText("ShortcutTitle", shortcutHeader.transform, font, 18f,
            Ink, TextAlignmentOptions.Center);
        StretchWithOffsets(shortcutTitle.rectTransform, 12f, 5f, -12f, -5f);
        shortcutTitle.text = "CHOOSE YOUR HAPPY PLACE";
        shortcutTitle.fontStyle = FontStyles.Bold;
        shortcutTitle.characterSpacing = 1.2f;
        shortcutTitle.enableAutoSizing = true;
        shortcutTitle.fontSizeMin = 14f;
        shortcutTitle.fontSizeMax = 18f;
        Localize(shortcutTitle, "title.shortcuts");

        bindings.Shop = BuildShortcutCard(shortcuts, font, "ShopShortcut", "SHOP", "title.shop",
            ShopShortcutPath, new Color32(255, 119, 111, 255), new Vector2(0f, 274f),
            out bindings.ShopAfterTourBadge);
        bindings.Rooms = BuildShortcutCard(shortcuts, font, "RoomsShortcut", "ROOMS", "title.rooms",
            RoomsShortcutPath, new Color32(66, 218, 170, 255), Vector2.zero,
            out bindings.RoomsAfterTourBadge);
        bindings.Games = BuildShortcutCard(shortcuts, font, "GamesShortcut", "GAMES", "title.games",
            GamesShortcutPath, new Color32(177, 117, 239, 255), new Vector2(0f, -274f),
            out bindings.GamesAfterTourBadge);

        return bindings;
    }

    private static Button BuildShortcutCard(RectTransform parent, TMP_FontAsset font,
        string name, string label, string labelKey, string texturePath, Color accent, Vector2 position,
        out GameObject afterTourBadge)
    {
        RectTransform root = CreateRect(name, parent);
        SetCentered(root, new Vector2(304f, 250f), position);
        LowPolyPanelGraphic glow = CreatePanel("OuterGlow", root,
            new Color(accent.r, accent.g, accent.b, .40f), 38f, 7f, false);
        SetCentered(glow.rectTransform, new Vector2(322f, 268f), Vector2.zero);
        LowPolyPanelGraphic rim = CreatePanel("ChampagneRim", root,
            Gold, 35f, 8f, false);
        SetCentered(rim.rectTransform, new Vector2(312f, 258f), Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(rim,
            new Color32(255, 235, 137, 255), Gold, 35f, 8f);
        LowPolyPanelGraphic face = CreatePanel("Visual", root, Cream, 31f, 10f, true);
        SetCentered(face.rectTransform, new Vector2(300f, 246f), Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(face,
            new Color32(255, 253, 236, 255), Color.Lerp(accent, Color.white, .32f), 31f, 10f);

        LowPolyPanelGraphic imageRim = CreatePanel("PreviewRim", face.transform,
            Gold, 23f, 6f, false);
        SetCentered(imageRim.rectTransform, new Vector2(270f, 170f), new Vector2(0f, 30f));
        LowPolyPanelGraphic imageWell = CreatePanel("PreviewWell", imageRim.transform,
            new Color32(255, 250, 235, 255), 20f, 5f, false);
        StretchWithOffsets(imageWell.rectTransform, 5f, 5f, -5f, -5f);
        Mask mask = imageWell.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;
        RawImage image = CreateRect("Preview", imageWell.transform).gameObject.AddComponent<RawImage>();
        Stretch(image.rectTransform);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
        if (texture != null)
            image.uvRect = RoomPreviewFit.CoverUv(texture.width, texture.height, 260f, 160f);

        LowPolyPanelGraphic labelPlate = CreatePanel("LabelPlate", face.transform,
            accent, 18f, 6f, false);
        SetCentered(labelPlate.rectTransform, new Vector2(262f, 58f), new Vector2(0f, -82f));
        PremiumUiStyle.ConfigureAccentSurface(labelPlate,
            Color.Lerp(accent, Color.white, .08f),
            Color.Lerp(accent, PremiumUiStyle.Night, .10f), 18f, 6f);
        LowPolyPanelGraphic gloss = CreatePanel("TopGlossBand", labelPlate.transform,
            new Color32(255, 255, 255, 64), 7f, 2f, false);
        SetCentered(gloss.rectTransform, new Vector2(236f, 13f), new Vector2(0f, 15f));
        TMP_Text text = CreateText("Label", labelPlate.transform, font, 25f,
            Color.white, TextAlignmentOptions.Center);
        StretchWithOffsets(text.rectTransform, 18f, 7f, -18f, -7f);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 2f;
        Localize(text, labelKey);
        BuildSparkle(face.rectTransform, "CardSparkle", new Vector2(126f, 101f), 13f,
            PremiumUiStyle.CandyLemon, 0f);

        LowPolyPanelGraphic tourRim = CreatePanel("AfterTourBadge", face.transform,
            Gold, 13f, 4f, false);
        SetCentered(tourRim.rectTransform, new Vector2(132f, 38f), new Vector2(-62f, 91f));
        LowPolyPanelGraphic tourPlate = CreatePanel("Visual", tourRim.transform,
            new Color32(112, 83, 174, 252), 11f, 3f, false);
        StretchWithOffsets(tourPlate.rectTransform, 4f, 4f, -4f, -4f);
        PremiumUiStyle.ConfigureAccentSurface(tourPlate,
            new Color32(157, 124, 220, 255), new Color32(93, 68, 160, 255), 11f, 3f);
        TMP_Text tourText = CreateText("Label", tourPlate.transform, font, 13f,
            Color.white, TextAlignmentOptions.Center);
        StretchWithOffsets(tourText.rectTransform, 7f, 3f, -7f, -3f);
        tourText.text = "AFTER TOUR";
        tourText.fontStyle = FontStyles.Bold;
        tourText.characterSpacing = 1.1f;
        tourText.enableAutoSizing = true;
        tourText.fontSizeMin = 10f;
        tourText.fontSizeMax = 13f;
        Localize(tourText, "title.after_tour");
        afterTourBadge = tourRim.gameObject;

        Button button = GetOrAdd<Button>(root.gameObject);
        button.targetGraphic = face;
        button.transition = Selectable.Transition.None;
        return button;
    }

    private static CanvasGroup BuildCreditsOverlay(
        RectTransform safeArea, TMP_FontAsset font, out Button closeButton)
    {
        RectTransform overlay = CreateRect("CreditsOverlay", safeArea);
        Stretch(overlay);
        CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image scrim = CreateImage("CreditsScrim", overlay, new Color32(44, 27, 67, 160), true);
        Stretch(scrim.rectTransform);
        Button scrimButton = GetOrAdd<Button>(scrim.gameObject);
        scrimButton.targetGraphic = scrim;
        scrimButton.transition = Selectable.Transition.None;

        LowPolyPanelGraphic card = CreatePanel("CreditsCard", overlay, Cream, 36f, 10f, true);
        SetCentered(card.rectTransform, new Vector2(720f, 460f), Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(card,
            new Color32(255, 248, 226, 255), new Color32(255, 226, 236, 255), 36f, 10f);

        LowPolyPanelGraphic banner = CreatePanel("CreditsBanner", card.transform,
            HeaderPink, 22f, 6f, false);
        SetCentered(banner.rectTransform, new Vector2(620f, 80f), new Vector2(0f, 160f));
        TMP_Text title = CreateText("CreditsTitle", banner.transform, font, 36f,
            Color.white, TextAlignmentOptions.Center);
        Stretch(title.rectTransform);
        title.text = "CREDITS";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 2f;
        Localize(title, "credits.title");

        TMP_Text body = CreateText("CreditsBody", card.transform, font, 24f,
            Ink, TextAlignmentOptions.Center);
        SetCentered(body.rectTransform, new Vector2(620f, 180f), new Vector2(0f, 10f));
        body.text = "CAT HOME\nA COZY CAT CARE GAME\nMADE WITH CARE FOR CATS AND KIDS";
        body.fontStyle = FontStyles.Bold;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 12f;
        body.characterSpacing = 0.8f;
        body.enableAutoSizing = true;
        body.fontSizeMin = 18f;
        body.fontSizeMax = 24f;
        Localize(body, "credits.body");

        closeButton = BuildButton(card.transform, font, "CreditsCloseButton", "CLOSE",
            new Vector2(280f, 72f), new Vector2(0f, -150f), 28f,
            PremiumUiStyle.CandyMint, new Color32(120, 235, 180, 255));
        Localize(closeButton.GetComponentInChildren<TMP_Text>(true), "common.close");
        scrimButton.onClick.AddListener(closeButton.onClick.Invoke);
        return group;
    }

    private static CanvasGroup BuildNewGameOverlay(
        RectTransform safeArea,
        TMP_FontAsset font,
        out Button cancelButton,
        out Button confirmButton,
        out TMP_Text statusText)
    {
        RectTransform overlay = CreateRect("NewGameOverlay", safeArea);
        Stretch(overlay);
        CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image scrim = CreateImage("NewGameScrim", overlay,
            new Color32(44, 27, 67, 178), true);
        Stretch(scrim.rectTransform);
        Button scrimButton = GetOrAdd<Button>(scrim.gameObject);
        scrimButton.targetGraphic = scrim;
        scrimButton.transition = Selectable.Transition.None;

        LowPolyPanelGraphic glow = CreatePanel("NewGameAquaGlow", overlay,
            new Color32(64, 229, 220, 118), 44f, 8f, false);
        SetCentered(glow.rectTransform, new Vector2(790f, 510f), Vector2.zero);
        LowPolyPanelGraphic card = CreatePanel("NewGameCard", overlay, Cream, 40f, 12f, true);
        SetCentered(card.rectTransform, new Vector2(760f, 480f), Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(card,
            new Color32(255, 247, 216, 255), new Color32(255, 220, 235, 255), 40f, 12f);

        LowPolyPanelGraphic banner = CreatePanel("NewGameBanner", card.transform,
            HeaderPink, 24f, 7f, false);
        SetCentered(banner.rectTransform, new Vector2(650f, 82f), new Vector2(0f, 160f));
        TMP_Text title = CreateText("NewGameTitle", banner.transform, font, 34f,
            Color.white, TextAlignmentOptions.Center);
        StretchWithOffsets(title.rectTransform, 20f, 8f, -20f, -8f);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 1.6f;
        title.enableAutoSizing = true;
        title.fontSizeMin = 24f;
        title.fontSizeMax = 34f;
        Localize(title, "new_game.title");

        TMP_Text body = CreateText("NewGameBody", card.transform, font, 23f,
            Ink, TextAlignmentOptions.Center);
        SetCentered(body.rectTransform, new Vector2(650f, 150f), new Vector2(0f, 45f));
        body.fontStyle = FontStyles.Bold;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 10f;
        body.characterSpacing = .7f;
        body.enableAutoSizing = true;
        body.fontSizeMin = 18f;
        body.fontSizeMax = 23f;
        Localize(body, "new_game.body");

        statusText = CreateText("NewGameStatus", card.transform, font, 17f,
            PremiumUiStyle.CandyBerry, TextAlignmentOptions.Center);
        SetCentered(statusText.rectTransform, new Vector2(620f, 46f), new Vector2(0f, -58f));
        statusText.fontStyle = FontStyles.Bold;
        statusText.textWrappingMode = TextWrappingModes.Normal;
        statusText.text = string.Empty;

        cancelButton = BuildButton(card.transform, font, "NewGameCancelButton", "CANCEL",
            new Vector2(260f, 76f), new Vector2(-150f, -145f), 25f,
            PremiumUiStyle.CandyGrape, new Color32(190, 150, 235, 255));
        Localize(cancelButton.GetComponentInChildren<TMP_Text>(true), "new_game.cancel");
        confirmButton = BuildButton(card.transform, font, "NewGameConfirmButton", "YES, NEW GAME",
            new Vector2(310f, 76f), new Vector2(145f, -145f), 24f,
            new Color32(255, 112, 120, 255), new Color32(255, 177, 136, 255));
        Localize(confirmButton.GetComponentInChildren<TMP_Text>(true), "new_game.confirm");
        scrimButton.onClick.AddListener(cancelButton.onClick.Invoke);
        return group;
    }

    private static TMP_Text BuildStatPill(
        Transform parent, TMP_FontAsset font, string name, Color color,
        Vector2 size, Vector2 position, string label)
    {
        RectTransform root = CreateRect(name, parent);
        SetCentered(root, size, position);
        LowPolyPanelGraphic rim = CreatePanel("ChampagneRim", root, Gold,
            size.y * .42f, 6f, false);
        SetCentered(rim.rectTransform, size + new Vector2(7f, 7f), Vector2.zero);
        LowPolyPanelGraphic pill = CreatePanel("Visual", root, color,
            size.y * .40f, 6f, false);
        SetCentered(pill.rectTransform, size, Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(pill,
            Color.Lerp(color, Color.white, .12f),
            Color.Lerp(color, PremiumUiStyle.Night, .08f), size.y * .40f, 6f);
        LowPolyPanelGraphic gloss = CreatePanel("TopGlossBand", pill.transform,
            new Color32(255, 255, 255, 58), 7f, 2f, false);
        SetCentered(gloss.rectTransform, new Vector2(size.x - 24f, 11f),
            new Vector2(0f, size.y * .22f));
        TMP_Text text = CreateText("Label", pill.transform, font, 22f,
            Color.white, TextAlignmentOptions.Center);
        StretchWithOffsets(text.rectTransform, 12f, 6f, -12f, -6f);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 1.1f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 16f;
        text.fontSizeMax = 22f;
        return text;
    }

    private static void CreateSun(RectTransform parent)
    {
        LowPolyPanelGraphic halo = CreatePanel("SunHalo", parent,
            new Color32(255, 236, 150, 90), 140f, 8f, false);
        RectTransform haloRect = halo.rectTransform;
        haloRect.anchorMin = haloRect.anchorMax = haloRect.pivot = new Vector2(0f, 1f);
        haloRect.sizeDelta = new Vector2(340f, 340f);
        haloRect.anchoredPosition = new Vector2(90f, -70f);

        LowPolyPanelGraphic sun = CreatePanel("Sun", parent,
            new Color32(255, 222, 94, 255), 108f, 10f, false);
        PremiumUiStyle.ConfigureAccentSurface(sun,
            new Color32(255, 236, 150, 255), new Color32(255, 205, 90, 255), 108f, 10f);
        RectTransform rect = sun.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(200f, 200f);
        rect.anchoredPosition = new Vector2(160f, -140f);
    }

    private static void CreateHill(
        RectTransform parent, string name, Color color, Vector2 size, Vector2 position, float cut)
    {
        LowPolyPanelGraphic hill = CreatePanel(name, parent, color, cut, 8f, false);
        SetCentered(hill.rectTransform, size, position);
    }

    private static void BuildCloud(
        RectTransform parent, string name, Vector2 position, Vector2 size, float speed, float range)
    {
        LowPolyPanelGraphic cloud = CreatePanel(name, parent, Color.white, size.y * 0.48f, 6f, false);
        SetCentered(cloud.rectTransform, size, position);
        cloud.color = new Color32(255, 255, 255, 230);
        TitleCloudDrift drift = cloud.gameObject.AddComponent<TitleCloudDrift>();
        drift.EditorConfigure(speed, range);
    }

    private static void BuildPaw(
        RectTransform parent, string name, Vector2 position, float size, float rotation)
    {
        RectTransform paw = CreateRect(name, parent);
        SetCentered(paw, new Vector2(size * 1.4f, size), position);
        paw.localEulerAngles = new Vector3(0f, 0f, rotation);
        LowPolyPanelGraphic pad = AddPanel(paw.gameObject,
            new Color32(255, 255, 255, 70), size * 0.42f, 4f, false);
        pad.raycastTarget = false;
    }

    private static Button BuildButton(Transform parent, TMP_FontAsset font, string name,
        string label, Vector2 size, Vector2 position, float fontSize, Color color, Color accent)
    {
        RectTransform rootRect = CreateRect(name, parent);
        SetCentered(rootRect, size, position);

        LowPolyPanelGraphic aura = CreatePanel("OuterGlow", rootRect,
            new Color(accent.r, accent.g, accent.b, .28f), size.y * .46f, 6f, false);
        SetCentered(aura.rectTransform, size + new Vector2(18f, 18f), Vector2.zero);

        LowPolyPanelGraphic rim = CreatePanel("ChampagneRim", rootRect,
            Gold, size.y * .44f, 8f, false);
        SetCentered(rim.rectTransform, size + new Vector2(8f, 8f), Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(rim,
            new Color32(255, 235, 137, 255), Gold, size.y * .44f, 8f);

        LowPolyPanelGraphic face = CreatePanel("Visual", rootRect,
            color, size.y * .42f, 9f, true);
        SetCentered(face.rectTransform, size, Vector2.zero);
        PremiumUiStyle.ConfigureAccentSurface(face,
            Color.Lerp(accent, Color.white, .06f),
            Color.Lerp(color, PremiumUiStyle.Night, .10f), size.y * 0.42f, 9f);

        float glossHeight = Mathf.Max(10f, size.y * .18f);
        LowPolyPanelGraphic topGloss = CreatePanel("TopGlossBand", face.transform,
            new Color32(255, 255, 255, 70), glossHeight * .46f, 2f, false);
        SetCentered(topGloss.rectTransform,
            new Vector2(size.x - 24f, glossHeight), new Vector2(0f, size.y * .25f));
        LowPolyPanelGraphic depthBand = CreatePanel("InnerDepthBand", face.transform,
            new Color(color.r * .55f, color.g * .55f, color.b * .55f, .23f),
            5f, 1f, false);
        SetCentered(depthBand.rectTransform,
            new Vector2(size.x - 28f, Mathf.Max(7f, size.y * .08f)),
            new Vector2(0f, -size.y * .34f));

        bool heroAction = size.y >= 90f;
        float textInset = heroAction ? 88f : 14f;
        TMP_Text text = CreateText("Label", face.transform, font, fontSize, Color.white,
            TextAlignmentOptions.Center);
        StretchWithOffsets(text.rectTransform,
            heroAction ? textInset : 14f, 8f, -14f, -8f);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 2f;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, fontSize * .70f);
        text.fontSizeMax = fontSize;

        if (heroAction)
        {
            Texture2D coinTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PawCoinIconPath);
            RectTransform badge = CreateRect("ActionPawBadge", face.transform);
            SetCentered(badge, new Vector2(68f, 68f), new Vector2(-size.x * .36f, 0f));
            RawImage coin = badge.gameObject.AddComponent<RawImage>();
            coin.texture = coinTexture;
            coin.color = Color.white;
            coin.raycastTarget = false;
        }
        else
        {
            BuildSparkle(face.rectTransform, "ButtonSparkle",
                new Vector2(size.x * .38f, size.y * .27f), 7f,
                new Color32(255, 255, 255, 150), 0f);
        }

        Button button = GetOrAdd<Button>(rootRect.gameObject);
        button.targetGraphic = face;
        button.transition = Selectable.Transition.None;
        return button;
    }

    private static void BuildSparkle(RectTransform parent, string name, Vector2 position,
        float size, Color color, float rotation)
    {
        RectTransform sparkle = CreateRect(name, parent);
        SetCentered(sparkle, new Vector2(size, size * .34f), position);
        sparkle.localEulerAngles = new Vector3(0f, 0f, rotation);
        Image beamA = CreateImage("BeamA", sparkle, color, false);
        Stretch(beamA.rectTransform);
        Image beamB = CreateImage("BeamB", sparkle, color, false);
        Stretch(beamB.rectTransform);
        beamB.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
        PremiumAmbientSparkle motion = sparkle.gameObject.AddComponent<PremiumAmbientSparkle>();
        motion.EditorConfigure(1.5f, .12f, 10f, 1.4f);
    }

    private static GameObject CreateRoot(Scene scene)
    {
        var root = new GameObject(RootName, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static GameObject PrepareRoot(GameObject root)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(root) &&
            PrefabUtility.GetNearestPrefabInstanceRoot(root) == root)
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
        }
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        return root;
    }

    private static GameObject FindSceneRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        return rect;
    }

    private static LowPolyPanelGraphic CreatePanel(string name, Transform parent,
        Color color, float cut, float bevel, bool raycast)
    {
        RectTransform rect = CreateRect(name, parent);
        return AddPanel(rect.gameObject, color, cut, bevel, raycast);
    }

    private static LowPolyPanelGraphic AddPanel(GameObject gameObject, Color color,
        float cut, float bevel, bool raycast)
    {
        GetOrAdd<CanvasRenderer>(gameObject);
        LowPolyPanelGraphic panel = GetOrAdd<LowPolyPanelGraphic>(gameObject);
        panel.color = color;
        panel.raycastTarget = raycast;
        PremiumUiStyle.ConfigureSurface(panel, color, cut, bevel);
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycast)
    {
        RectTransform rect = CreateRect(name, parent);
        GetOrAdd<CanvasRenderer>(rect.gameObject);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font,
        float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        GetOrAdd<CanvasRenderer>(rect.gameObject);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.extraPadding = true;
        return text;
    }

    private static void Localize(TMP_Text text, string key)
    {
        if (text == null)
            return;
        LocalizedLabel localized = GetOrAdd<LocalizedLabel>(text.gameObject);
        localized.EditorConfigure(text, key);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void StretchWithOffsets(
        RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Assign(SerializedObject serialized, string name,
        UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException("Missing TitleScreen field '" + name + "'.");
        property.objectReferenceValue = value;
    }

    private static TMP_FontAsset FindFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);
        if (font == null)
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/Fredoka-SemiBold SDF.asset");
        return font;
    }

    private static void ConfigureHeroImporter()
    {
        ConfigureTextureImporter(HeroBackgroundPath, false);
    }

    private static void ConfigureTextureImporter(string path, bool alphaIsTransparency)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = alphaIsTransparency;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static void Validate(GameObject root)
    {
        if (root.GetComponents<TitleScreen>().Length != 1)
            throw new InvalidOperationException("Title screen needs exactly one controller.");
        if (root.GetComponent<Camera>() != null ||
            root.GetComponent<AudioListener>() != null ||
            root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null)
        {
            throw new InvalidOperationException(
                "Title screen must not add a camera, listener or EventSystem.");
        }
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.targetGraphic == null)
                throw new InvalidOperationException("Title button has no visual: " + button.name);
        if (root.GetComponentInChildren<SafeAreaRect>(true) == null)
            throw new InvalidOperationException("Title screen needs a SafeArea.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }
}
