using System;
using System.IO;
using TMPro;
using U = PremiumUiElements;
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
        TitleShowcaseContentBuilder.PosterPath;
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
    private const string GoogleSignInIconPath =
        "Assets/Art/Title/Google/GoogleSignIn_G_Square_Light.png";

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
        TitleShowcaseContentBuilder.BuildSilently();
        TitleShowcaseContentBuilder.CapturePoster();
        ConfigureHeroImporter();
        ConfigureTextureImporter(MainMenuLogoPath, true);
        ConfigureTextureImporter(ShopShortcutPath, false);
        ConfigureTextureImporter(RoomsShortcutPath, false);
        ConfigureTextureImporter(GamesShortcutPath, false);
        ConfigureTextureImporter(GoogleSignInIconPath, false);
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
        // The authoring Game view shows the room; TitleScreen.OnEnable shows
        // the welcome screen on entering Play, including DisableSceneReload.
        rootGroup.alpha = 0f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;

        RectTransform safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea);
        GetOrAdd<SafeAreaRect>(safeArea.gameObject);

        // Artwork covers the display, including Android's camera cutout inset.
        // Only interactive controls belong to the safe area.
        BuildBackdrop(root.transform as RectTransform, font);
        safeArea.SetAsLastSibling();
        TitleScreenBindings bindings = BuildCard(safeArea, font);
        safeArea.gameObject.AddComponent<TitleScreenLayout>().EditorConfigure(
            safeArea.Find("BrandDockLayout") as RectTransform,
            safeArea.Find("MainMenuShortcuts") as RectTransform);
        CanvasGroup creditsGroup = BuildCreditsOverlay(safeArea, font, out Button creditsClose);
        CanvasGroup newGameGroup = BuildNewGameOverlay(
            safeArea, font, out Button newGameCancel, out Button newGameConfirm,
            out TMP_Text newGameStatus);
        CanvasGroup accountChoiceGroup = BuildAccountChoiceOverlay(
            safeArea, font, out Button accountGoogle, out Button accountGuest,
            out Button accountBack, out TMP_Text accountStatus);

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
        Assign(serialized, "accountGoogleButton", accountGoogle);
        Assign(serialized, "accountGuestButton", accountGuest);
        Assign(serialized, "accountBackButton", accountBack);
        Assign(serialized, "greetingText", bindings.Greeting);
        Assign(serialized, "catNameText", bindings.CatName);
        Assign(serialized, "playLabel", bindings.PlayLabel);
        Assign(serialized, "homeLevelText", bindings.HomeLevel);
        Assign(serialized, "coinsText", bindings.Coins);
        Assign(serialized, "collectionText", bindings.Collection);
        Assign(serialized, "creditsGroup", creditsGroup);
        Assign(serialized, "newGameGroup", newGameGroup);
        Assign(serialized, "newGameStatusText", newGameStatus);
        Assign(serialized, "accountChoiceGroup", accountChoiceGroup);
        Assign(serialized, "accountStatusText", accountStatus);
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
        return "Premium Title Screen v5 with live Full HD game cats built in CatHome_UI.";
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
        hero.color = Color.white;
        hero.raycastTarget = false;
        heroRect.gameObject.AddComponent<TitleCatShowcase>().EditorConfigure(
            AssetDatabase.LoadAssetAtPath<GameObject>(TitleShowcaseContentBuilder.PrefabPath),
            hero.texture as Texture2D, CatBreedCatalog.Load());
        if (hero.texture != null)
            hero.uvRect = RoomPreviewFit.CoverUv(
                hero.texture.width, hero.texture.height, 1920f, 1080f);

        // The left reading area is part of the composition, with no stacked
        // decorative shells between the player and the animated cats.
        RectTransform veil = CreateRect("LeftPearlVeil", safeArea);
        veil.anchorMin = Vector2.zero;
        veil.anchorMax = new Vector2(.50f, 1f);
        veil.offsetMin = veil.offsetMax = Vector2.zero;
        veil.gameObject.AddComponent<PremiumReadingVeil>().raycastTarget = false;
    }

    private static TitleScreenBindings BuildCard(RectTransform safeArea, TMP_FontAsset font)
    {
        var bindings = new TitleScreenBindings();
        RectTransform layout = CreateRect("BrandDockLayout", safeArea);
        SetCentered(layout, new Vector2(540f, 960f), new Vector2(-632f, 0f));
        var logo = CreateText("Wordmark", layout, font, 54f, Ink, TextAlignmentOptions.Left);
        SetCentered(logo.rectTransform, new Vector2(480f, 82f), new Vector2(70f, 398f));
        logo.text = "CAT <color=#218F87>HOME</color>";
        logo.characterSpacing = 2f;
        var mark=U.Rect("PawWordmark",layout);U.At(mark,-220,398,62,62);
        PremiumUiFactory.BuildCurrencyIcon(mark,PremiumUiFactory.CurrencyVisual.Coin,false);
        bindings.Greeting = CreateText("Greeting", layout, font, 20f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Left);
        SetCentered(bindings.Greeting.rectTransform, new Vector2(500f, 42f), new Vector2(0f, 274f));
        bindings.Greeting.text = GameLanguageService.Text("title.greeting.empty");
        bindings.CatName = CreateText("CatName", layout, font, 80f, Ink, TextAlignmentOptions.Left);
        SetCentered(bindings.CatName.rectTransform, new Vector2(620f, 214f), new Vector2(60f, 139f));
        bindings.CatName.text = GameLanguageService.Text("title.home.empty");
        bindings.CatName.textWrappingMode = TextWrappingModes.Normal;
        bindings.CatName.enableAutoSizing = true;
        bindings.CatName.fontSizeMin = 62f;
        bindings.CatName.fontSizeMax = 80f;
        var promise = CreateText("ExperiencePromise", layout, font, 25f, Ink, TextAlignmentOptions.Left);
        SetCentered(promise.rectTransform, new Vector2(560f, 65f), new Vector2(30f, -8f));
        Localize(promise, "title.promise");
        bindings.Play = BuildButton(layout, font, "PlayButton", "CONTINUE",
            new Vector2(540f, 104f), new Vector2(20f, -140f), 32f,
            PremiumUiStyle.Coral, PremiumUiStyle.CoralLift);
        bindings.PlayLabel = bindings.Play.GetComponentInChildren<TMP_Text>(true);
        bindings.PlayLabel.rectTransform.offsetMin=new Vector2(96,8);
        bindings.PlayLabel.rectTransform.offsetMax=new Vector2(-68,-8);
        var playMedal=U.Rect("PawMedal",bindings.Play.targetGraphic.transform);U.At(playMedal,-211,0,72,72);
        PremiumUiFactory.BuildCurrencyIcon(playMedal,PremiumUiFactory.CurrencyVisual.Coin,false);
        U.Label("ContinueArrow",bindings.Play.targetGraphic.transform,font,36,Ink,219,0,42,62,TextAlignmentOptions.Center).text="→";
        bindings.NewGame = BuildButton(layout, font, "NewGameButton", "New game",
            new Vector2(220f, 64f), new Vector2(-140f, -247f), 23f, Cream, Cream, false);
        Localize(bindings.NewGame.GetComponentInChildren<TMP_Text>(true), "title.new_game");
        bindings.HomeLevel = BuildStatPill(layout, font, "HomeLevelPill",
            PremiumUiStyle.Mint, new Vector2(340f, 64f), new Vector2(-80f, -426f), "Home level 1");
        var journey=U.Panel("YourHomeJourney",layout,PremiumUiStyle.Mint,20,-333,540,94,22);
        string[] journeyArt={"Paw","Rooms","Games"};
        string[] journeyTr={"Bir bağ kur","Yuvanı kur","Birlikte oyna"};
        string[] journeyEn={"Make a friend","Make it home","Play together"};
        for(int i=0;i<3;i++)
        {
            float x=(i-1)*176;
            var icon=U.Rect("JourneyIcon"+i,journey.transform);U.At(icon,x,15,46,46);
            var image=icon.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("PremiumInterface/"+journeyArt[i]);image.raycastTarget=false;
            U.Label("JourneyCopy"+i,journey.transform,font,18,Ink,x,-25,174,28,TextAlignmentOptions.Center).gameObject.AddComponent<BilingualCopyLabel>().Configure(journeyTr[i],journeyEn[i]);
        }

        RectTransform utilities = CreateRect("TitleUtilities", safeArea);
        utilities.anchorMin = utilities.anchorMax = utilities.pivot = Vector2.one;
        utilities.anchoredPosition = new Vector2(-40f, -36f);
        utilities.sizeDelta = new Vector2(450f, 64f);
        bindings.Settings = BuildButton(utilities, font, "SettingsButton", "Settings",
            new Vector2(150f, 64f), new Vector2(-300f, 0f), 21f, Cream, Cream, false);
        bindings.Credits = BuildButton(utilities, font, "CreditsButton", "Credits",
            new Vector2(130f, 64f), new Vector2(-142f, 0f), 19f, Cream, Cream, false);
        bindings.Quit = BuildButton(utilities, font, "QuitButton", "Quit",
            new Vector2(108f, 64f), new Vector2(-5f, 0f), 19f, Cream, Cream, false);
        Localize(bindings.Settings.GetComponentInChildren<TMP_Text>(true), "title.settings");
        Localize(bindings.Credits.GetComponentInChildren<TMP_Text>(true), "title.credits");
        Localize(bindings.Quit.GetComponentInChildren<TMP_Text>(true), "title.quit");

        RectTransform shortcuts = CreateRect("MainMenuShortcuts", safeArea);
        SetCentered(shortcuts, new Vector2(756f, 146f), new Vector2(500f, -420f));
        bindings.Shop = BuildShortcutCard(shortcuts, font, "ShopShortcut", "My cat", "title.my_cat",
            null, PremiumUiStyle.Teal, new Vector2(-252f, 0f), out bindings.ShopAfterTourBadge);
        bindings.Rooms = BuildShortcutCard(shortcuts, font, "RoomsShortcut", "Rooms", "title.rooms",
            RoomsShortcutPath, PremiumUiStyle.Teal, Vector2.zero, out bindings.RoomsAfterTourBadge);
        bindings.Games = BuildShortcutCard(shortcuts, font, "GamesShortcut", "Games", "title.games",
            GamesShortcutPath, PremiumUiStyle.Teal, new Vector2(252f, 0f), out bindings.GamesAfterTourBadge);
        return bindings;
    }

    private static Button BuildShortcutCard(RectTransform parent, TMP_FontAsset font,
        string name, string label, string labelKey, string texturePath, Color accent, Vector2 position,
        out GameObject afterTourBadge)
    {
        RectTransform root = CreateRect(name, parent);
        SetCentered(root, new Vector2(228f, 146f), position);
        LowPolyPanelGraphic face = CreatePanel("Visual", root, Cream, 26f, 2f, true);
        Stretch(face.rectTransform);
        var preview = CreateRect("Preview", face.transform);
        SetCentered(preview, new Vector2(108f, 100f), new Vector2(-45f, 12f));
        if (texturePath == null)
        {
            var portrait = preview.gameObject.AddComponent<Image>();
            var catalog = CatBreedCatalog.Load();
            var entry = catalog.Find(CatBreedService.SelectedBreedId) ?? catalog.Get(0);
            portrait.sprite = entry.Portrait;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            preview.gameObject.AddComponent<SelectedCatPortrait>();
        }
        else
        {
            var image = preview.gameObject.AddComponent<RawImage>();
            image.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            image.raycastTarget = false;
        }
        var text = CreateText("Label", face.transform, font, 23f, Ink, TextAlignmentOptions.Right);
        SetCentered(text.rectTransform, new Vector2(192f, 36f), new Vector2(0f, -46f));
        Localize(text, labelKey);
        var badge = CreateText("AfterTourBadge", face.transform, font, 14f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(badge.rectTransform, new Vector2(200f, 28f), new Vector2(0f, 48f));
        Localize(badge, "title.after_tour");
        afterTourBadge = badge.gameObject;
        Button button = root.gameObject.AddComponent<Button>();
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

        var card=U.Panel("CreditsCard",overlay,Cream,0,0,920,640,32,true);
        U.Localize(U.Label("CreditsTitle",card.transform,font,44,Ink,0,225,792,70),"credits.title");
        U.Localize(U.Label("CreditsBody",card.transform,font,24,Ink,0,32,792,260),"credits.body");
        U.Label("Version",card.transform,font,19,PremiumUiStyle.Muted,0,-142,792,38).text="Cat Home · "+Application.version;
        closeButton=U.Action("CreditsCloseButton",card.transform,font,"common.close",PremiumUiStyle.Mint,0,-236,310,72,out var closeLabel);
        scrimButton.gameObject.AddComponent<UiButtonRelay>().Configure(closeButton);
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

        var card=U.Panel("NewGameCard",overlay,Cream,0,0,1060,620,32,true);
        var stage=PremiumMomentArt.Stage(card.transform,-326,0,338,552);
        var portrait=U.Rect("YourCat",stage);U.At(portrait,0,36,244,244);
        portrait.gameObject.AddComponent<Image>().raycastTarget=false;portrait.gameObject.AddComponent<SelectedCatPortrait>();
        PremiumMomentArt.Caption(stage,"Yeni bir başlangıç,\nyeni bir yolculuk.","A fresh start,\na new journey.",0,-169,284,92,27);
        U.Localize(U.Label("NewGameTitle",card.transform,font,37,Ink,176,215,574,94),"new_game.title");
        U.Panel("ResetSummary",card.transform,new Color32(246,222,207,255),176,92,574,132,22);
        U.Label("NewGameBody",card.transform,font,23,Ink,176,92,518,104).gameObject.AddComponent<BilingualCopyLabel>().Configure(
            "SIFIRLANIR\nJetonlar, odalar, eşyalar, seviyeler ve skorlar.","RESETS\nCoins, rooms, items, levels and scores.");
        U.Panel("KeptSummary",card.transform,PremiumUiStyle.Mint,176,-57,574,132,22);
        U.Label("KeptBody",card.transform,font,23,Ink,176,-57,518,104).gameObject.AddComponent<BilingualCopyLabel>().Configure(
            "KORUNUR\nElmaslar, satın alma hakları ve ayarlar.","STAYS WITH YOU\nDiamonds, purchase rights and settings.");
        statusText=U.Label("NewGameStatus",card.transform,font,18,new Color32(152,53,43,255),176,-151,574,50);
        cancelButton=U.Action("NewGameCancelButton",card.transform,font,"new_game.cancel",PremiumUiStyle.Mint,26,-222,274,80,out var cancelLabel);
        confirmButton=U.Action("NewGameConfirmButton",card.transform,font,"new_game.confirm",new Color32(163,56,48,255),326,-222,274,80,out var confirmLabel);
        confirmLabel.enableAutoSizing=true;confirmLabel.fontSizeMin=18;confirmLabel.fontSizeMax=24;
        confirmLabel.color=Color.white;
        scrimButton.gameObject.AddComponent<UiButtonRelay>().Configure(cancelButton);
        return group;
    }

    private static CanvasGroup BuildAccountChoiceOverlay(
        RectTransform safeArea,
        TMP_FontAsset font,
        out Button googleButton,
        out Button guestButton,
        out Button backButton,
        out TMP_Text statusText)
    {
        RectTransform overlay = CreateRect("AccountChoiceOverlay", safeArea);
        Stretch(overlay);
        CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image scrim = CreateImage("AccountChoiceScrim", overlay,
            new Color32(44, 27, 67, 184), true);
        Stretch(scrim.rectTransform);
        Button scrimButton = GetOrAdd<Button>(scrim.gameObject);
        scrimButton.targetGraphic = scrim;
        scrimButton.transition = Selectable.Transition.None;

        var card=U.Panel("AccountChoiceCard",overlay,Cream,0,0,860,710,32,true);
        U.Localize(U.Label("AccountChoiceTitle",card.transform,font,42,Ink,0,266,720,90),"account.choice.title");
        U.Localize(U.Label("AccountChoiceSubtitle",card.transform,font,23,PremiumUiStyle.Muted,0,167,720,78),"account.choice.subtitle");
        googleButton=U.Action("GoogleSignInButton",card.transform,font,"account.google.action",Color.white,0,55,660,84,out var googleLabel);
        BuildGoogleSignInBadge(googleButton,googleLabel);
        guestButton=U.Action("GuestContinueButton",card.transform,font,"account.guest.action",PremiumUiStyle.Mint,0,-53,660,76,out var guestLabel);
        U.Localize(U.Label("GuestNote",card.transform,font,20,PremiumUiStyle.Muted,0,-126,710,56),"account.guest.note");
        statusText=U.Label("AccountChoiceStatus",card.transform,font,20,new Color32(152,53,43,255),0,-200,710,66);
        backButton=U.Action("AccountBackButton",card.transform,font,"common.back",PremiumUiStyle.WarmIvory,0,-288,260,62,out var backLabel);
        scrimButton.gameObject.AddComponent<UiButtonRelay>().Configure(backButton);
        return group;
    }

    private static void BuildGoogleSignInBadge(Button button, TMP_Text label)
    {
        RectTransform face = button.transform.Find("Visual") as RectTransform;
        if (face == null)
            throw new InvalidOperationException("Google sign-in button is missing its Visual surface.");

        // The downloaded Google asset is the official pre-approved light square.
        // Keep its colours, aspect and white background intact; Cat Home's premium
        // treatment lives in the soft halo around it and in the parent button.
        LowPolyPanelGraphic halo = CreatePanel("GoogleBrandHalo", face,
            new Color32(116, 232, 214, 76), 20f, 3f, false);
        SetCentered(halo.rectTransform, new Vector2(76f, 76f), new Vector2(-242f, 0f));

        RectTransform markRect = CreateRect("GoogleBrandMark", face);
        SetCentered(markRect, new Vector2(58f, 58f), new Vector2(-242f, 0f));
        RawImage mark = markRect.gameObject.AddComponent<RawImage>();
        mark.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GoogleSignInIconPath);
        mark.color = Color.white;
        mark.raycastTarget = false;
        if (mark.texture == null)
            throw new InvalidOperationException("Official Google sign-in mark is missing.");

        // Centre the localized action inside the remaining space without letting
        // long Turkish copy collide with the protected logo padding.
        StretchWithOffsets(label.rectTransform, 92f, 8f, -24f, -8f);
    }

    private static TMP_Text BuildStatPill(Transform parent, TMP_FontAsset font, string name,
        Color color, Vector2 size, Vector2 position, string value)
    {
        var surface = CreatePanel(name, parent, Cream, 28f, 2f, false);
        SetCentered(surface.rectTransform, size, position);
        var text = CreateText("Label", surface.transform, font, 23f, Ink, TextAlignmentOptions.Center);
        StretchWithOffsets(text.rectTransform, 20f, 8f, -20f, -8f);
        text.text = value;
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
        string label, Vector2 size, Vector2 position, float fontSize, Color color, Color accent,
        bool showHeroBadge = true)
    {
        RectTransform rootRect = CreateRect(name, parent);
        SetCentered(rootRect, size, position);

        LowPolyPanelGraphic face = CreatePanel("Visual", rootRect,
            color, Mathf.Min(36f, size.y * .42f), 2f, true);
        Stretch(face.rectTransform);
        bool neutral = color == Cream || color == (Color)PremiumUiStyle.Ivory;
        if (neutral) PremiumUiStyle.ConfigureLightSurface(face, 22f, 2f);
        else PremiumUiStyle.ConfigureAccentSurface(face, accent, color, 32f, 2f);
        TMP_Text text = CreateText("Label", face.transform, font, fontSize,
            neutral || color == (Color)PremiumUiStyle.Coral ? Ink : Color.white,
            TextAlignmentOptions.Center);
        StretchWithOffsets(text.rectTransform, 20f, 8f, -20f, -8f);
        text.text = label;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(16f, fontSize * .8f);
        text.fontSizeMax = fontSize;
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
        importer.textureCompression = path == HeroBackgroundPath ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
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
