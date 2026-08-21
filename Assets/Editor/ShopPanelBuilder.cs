using System;
using System.Collections.Generic;
using System.IO;
using CatHome.Economy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Idempotently authors the real, coin-backed home store UI.</summary>
public static class ShopPanelBuilder
{
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private const string RootName = "ShopPanelCanvas";
    private const string PrefabFolder = "Assets/UI";
    private const string PrefabPath = PrefabFolder + "/ShopPanel.prefab";
    private const string ProductIconFolder = "Assets/Art/StoreProducts/Icons";
    private const int SortingOrder = 110;
    private const float PanelHeight = 840f;
    private const float PanelWidth = 1320f;
    private const float HomeLevelBadgeWidth = 210f;
    private const float HomeLevelBadgeHeight = 54f;
    // Anchored to the panel's top-right corner, inside the pink header to the right
    // of the title/subtitle. Clears the intro line and the wallet/collection row
    // that share the body below.
    private const float HomeLevelBadgeX = -108f;
    // Vertically centered on the close button / title midline in the header.
    private const float HomeLevelBadgeY = -39f;

    private static readonly Color Scrim = PremiumUiStyle.Night;
    private static readonly Color Ink = PremiumUiStyle.Night;
    private static readonly Color Cream = new Color32(255, 250, 235, 255);
    private static readonly Color WarmCream = new Color32(255, 239, 215, 255);
    private static readonly Color CardCream = new Color32(255, 248, 232, 255);
    private static readonly Color Orange = new Color32(255, 103, 122, 255);
    private static readonly Color Teal = new Color32(49, 214, 195, 255);
    private static readonly Color Gold = new Color32(255, 220, 70, 255);
    private static readonly Color Muted = PremiumUiStyle.Muted;
    private static readonly Color Purple = new Color32(174, 110, 237, 255);
    private static readonly Color Pink = new Color32(255, 122, 181, 255);
    private static readonly Color CandyPink = new Color32(255, 91, 164, 255);
    private static readonly Color BrightOrange = new Color32(255, 142, 52, 255);
    private static readonly Color CandySky = new Color32(82, 202, 244, 255);
    private static readonly Color CandyAqua = new Color32(71, 226, 210, 255);
    private static readonly Color CandyLilac = new Color32(176, 116, 242, 255);
    private static readonly Color CandyLemon = new Color32(255, 225, 83, 255);
    private static readonly Color CandyCream = new Color32(255, 250, 231, 255);
    private static readonly Color CoinGold = new Color32(255, 190, 52, 255);
    private static readonly Color CoinGoldDeep = new Color32(231, 133, 24, 255);
    private static readonly Color CoinGoldDark = new Color32(151, 78, 17, 255);
    private static readonly Color CoinHighlight = new Color32(255, 244, 165, 255);
    private static readonly Color DiamondBlue = new Color32(57, 191, 255, 255);
    private static readonly Color DiamondBlueDeep = new Color32(8, 127, 221, 255);
    private static readonly Color DiamondBlueDark = new Color32(18, 102, 186, 255);
    private static readonly Color DiamondHighlight = new Color32(191, 243, 255, 255);

    [MenuItem("Tools/Cat Home/Build Home Store")]
    public static void Build()
    {
        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Store", result, "OK");
    }

    public static string BuildSilently()
    {
        ConfigureProductIconImporters();
        PremiumUiFactory.ConfigureCurrencyImporters();
        Scene uiScene = SceneManager.GetSceneByPath(UiScenePath);
        bool openedForBuild = !uiScene.IsValid() || !uiScene.isLoaded;
        if (openedForBuild)
            uiScene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);

        GameObject existing = FindSceneRoot(uiScene, RootName);
        GameObject root = existing != null ? PrepareExistingRoot(existing) : CreateRoot(uiScene);
        TMP_FontAsset font = FindFont(uiScene);

        Canvas canvas = GetOrAdd<Canvas>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAdd<GraphicRaycaster>(root);

        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 1f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        ShopPanelController controller = GetOrAdd<ShopPanelController>(root);

        Image scrimImage = CreateImage("Scrim", root.transform, Scrim, true);
        Stretch(scrimImage.rectTransform);
        CanvasGroup scrimGroup = scrimImage.gameObject.AddComponent<CanvasGroup>();
        scrimGroup.alpha = 0f;
        Button scrimButton = scrimImage.gameObject.AddComponent<Button>();
        scrimButton.transition = Selectable.Transition.None;
        scrimButton.targetGraphic = scrimImage;

        Image dragSurface = CreateImage(
            "PlacementDragSurface",
            root.transform,
            new Color(1f, 1f, 1f, 0.001f),
            true);
        Stretch(dragSurface.rectTransform);
        CanvasGroup dragGroup = dragSurface.gameObject.AddComponent<CanvasGroup>();
        dragGroup.alpha = 0f;
        dragGroup.interactable = false;
        dragGroup.blocksRaycasts = false;
        dragSurface.gameObject.AddComponent<HomePlacementDragSurface>().EditorConfigure(controller);

        GameObject safeAreaObject = CreateRect("SafeArea", root.transform);
        RectTransform safeArea = safeAreaObject.GetComponent<RectTransform>();
        Stretch(safeArea);
        safeAreaObject.AddComponent<SafeAreaRect>();

        PanelData panel = BuildPanel(safeArea.transform, font);
        PurchaseDialogData purchase = BuildPurchaseDialog(panel.Rect, font);
        DiamondConfirmationData diamondConfirmation =
            BuildDiamondConfirmation(panel.Rect, font);
        DiamondStorePanel diamondStore = BuildDiamondStore(panel.Rect, font);
        PlacementData placement = BuildPlacementToolbar(safeArea.transform, font);

        SerializedObject serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", FindInScene<CatMovement>(uiScene));
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "scrimGroup", scrimGroup);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "safeArea", safeArea);
        Assign(serialized, "panel", panel.Rect);
        Assign(serialized, "panelGroup", panel.Group);
        Assign(serialized, "closeButton", panel.CloseButton);
        Assign(serialized, "balanceText", panel.BalanceText);
        Assign(serialized, "ownedCountText", panel.OwnedCountText);
        Assign(serialized, "homeLevelText", panel.HomeLevelText);
        Assign(serialized, "sectionTitleText", panel.SectionTitleText);
        Assign(serialized, "feedbackText", panel.FeedbackText);
        Assign(serialized, "productScrollRect", panel.ProductScrollRect);
        Assign(serialized, "purchaseGroup", purchase.Group);
        Assign(serialized, "purchaseIcon", purchase.Icon);
        Assign(serialized, "purchaseTitleText", purchase.Title);
        Assign(serialized, "purchaseMessageText", purchase.Message);
        Assign(serialized, "purchaseCoinButton", purchase.CoinButton);
        Assign(serialized, "purchaseCoinButtonText", purchase.CoinButtonText);
        Assign(serialized, "purchaseDiamondButton", purchase.DiamondButton);
        Assign(serialized, "purchaseDiamondButtonText", purchase.DiamondButtonText);
        Assign(serialized, "purchaseCancelButton", purchase.CancelButton);
        Assign(serialized, "diamondConfirmationGroup", diamondConfirmation.Group);
        Assign(serialized, "diamondConfirmationTitle", diamondConfirmation.Title);
        Assign(serialized, "diamondConfirmationMessage", diamondConfirmation.Message);
        Assign(serialized, "diamondConfirmationButtonText", diamondConfirmation.ConfirmText);
        Assign(serialized, "diamondConfirmationButton", diamondConfirmation.ConfirmButton);
        Assign(serialized, "diamondConfirmationCancelButton", diamondConfirmation.CancelButton);
        Assign(serialized, "diamondStorePanel", diamondStore);
        Assign(serialized, "placementGroup", placement.Group);
        Assign(serialized, "placementDragGroup", dragGroup);
        Assign(serialized, "placementTitleText", placement.Title);
        Assign(serialized, "placementCounterText", placement.Counter);
        Assign(serialized, "placementPreviousButton", placement.Previous);
        Assign(serialized, "placementNextButton", placement.Next);
        Assign(serialized, "placementConfirmButton", placement.Confirm);
        Assign(serialized, "placementCancelButton", placement.Cancel);
        serialized.FindProperty("widthFraction").floatValue = 0.76f;
        serialized.FindProperty("minWidth").floatValue = 1320f;
        serialized.FindProperty("maxWidth").floatValue = 1420f;
        serialized.FindProperty("safeAreaMargin").floatValue = 34f;
        AssignTabs(serialized, panel.Tabs);
        AssignCards(serialized, panel.Cards);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PremiumUiFactory.PolishHierarchy(root.transform, font);

        ValidateButtons(root);
        EnsureFolder();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(uiScene);
        EditorSceneManager.SaveScene(uiScene);

        if (openedForBuild)
            EditorSceneManager.CloseScene(uiScene, true);

        AssetDatabase.SaveAssets();
        return "Home Store rebuilt with CAT, ROOM and HOME tabs plus expandable level/currency locks.";
    }

    private static PanelData BuildPanel(Transform parent, TMP_FontAsset font)
    {
        GameObject panelObject = CreateRect("Panel", parent);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panelRect.anchoredPosition = Vector2.zero;
        CanvasGroup panelGroup = panelObject.AddComponent<CanvasGroup>();
        panelGroup.alpha = 0f;

        LowPolyPanelGraphic shadow = CreatePanel("Shadow", panelObject.transform, new Color32(4, 10, 15, 102), 42f, 0f, false);
        PremiumUiStyle.SetCenteredShadowStretch(shadow.rectTransform, 6f);
        LowPolyPanelGraphic ambientShadow = CreatePanel("AmbientShadow", panelObject.transform, new Color32(4, 10, 15, 40), 48f, 0f, false);
        PremiumUiStyle.SetCenteredShadowStretch(ambientShadow.rectTransform, 12f);
        LowPolyPanelGraphic frame = CreatePanel("Frame", panelObject.transform, Gold, 46f, 2f, true);
        Stretch(frame.rectTransform);
        LowPolyPanelGraphic face = CreatePanel("Face", panelObject.transform, CandyPink, 42f, 2f, false);
        StretchWithOffsets(face.rectTransform, 3f, 3f, -3f, -3f);
        LowPolyPanelGraphic innerRim = CreatePanel("InnerRim", panelObject.transform, Gold, 35f, 1.5f, false);
        StretchWithOffsets(innerRim.rectTransform, 8f, 8f, -8f, -8f);
        LowPolyPanelGraphic innerFace = CreatePanel(
            "InnerFace", panelObject.transform, CandyCream, 32f, 2f, false);
        StretchWithOffsets(innerFace.rectTransform, 10f, 10f, -10f, -10f);

        BuildHeader(panelObject.transform, font, out Button closeButton);
        BuildSummary(
            panelObject.transform,
            font,
            out TMP_Text balanceText,
            out TMP_Text ownedCountText,
            out TMP_Text homeLevelText);

        var tabs = new List<TabData>(3)
        {
            BuildTab(panelObject.transform, font, HomeStoreCategory.Cat, "CAT", -408f),
            BuildTab(panelObject.transform, font, HomeStoreCategory.Room, "ROOM", 0f),
            BuildTab(panelObject.transform, font, HomeStoreCategory.Home, "HOME", 408f)
        };

        TMP_Text section = CreateText("SectionTitle", panelObject.transform, font, 1f, Color.clear, TextAlignmentOptions.MidlineLeft);
        section.text = string.Empty;
        section.raycastTarget = false;
        SetRect(section.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(58f, 89f), new Vector2(1f, 1f));

        ScrollRect productScroll = BuildProductScroll(panelObject.transform);
        Transform productParent = productScroll.content;

        var products = new List<HomeStoreProduct>(HomeStoreService.Products.Count);
        for (int i = 0; i < HomeStoreService.Products.Count; i++)
            products.Add(HomeStoreService.Products[i]);
        products.Sort(CompareCatalogProducts);

        var cards = new List<ProductCardData>(products.Count);
        for (int i = 0; i < products.Count; i++)
        {
            HomeStoreProduct product = products[i];
            cards.Add(BuildProductCard(
                productParent,
                font,
                product.Id,
                0f,
                GetProductAccent(product),
                StoreCatalogAssets.GetIconPath(product.Id),
                GetFallbackIcon(product)));
        }

        for (int i = 0; i < cards.Count; i++)
        {
            if (HomeStoreService.TryGetProduct(cards[i].ProductId, out HomeStoreProduct product))
                cards[i].Group.gameObject.SetActive(product.StoreCategory == HomeStoreCategory.Cat);
        }

        LowPolyPanelGraphic footer = CreatePanel("Footer", panelObject.transform, CandyPink, 10f, 2f, false);
        SetRect(footer.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -374f), new Vector2(-150f, 40f));
        TMP_Text feedback = CreateText("Feedback", footer.transform, font, 16f, WarmCream, TextAlignmentOptions.Center);
        StretchWithOffsets(feedback.rectTransform, 18f, 3f, -18f, -3f);
        feedback.text = "EARN COINS IN CAT RUNNER  •  PURCHASES STAY IN YOUR HOME";
        feedback.fontStyle = FontStyles.Bold;
        feedback.characterSpacing = 2.2f;
        CreateFooterOrnament(footer.transform, -410f);
        CreateFooterOrnament(footer.transform, 410f);

        return new PanelData(
            panelRect,
            panelGroup,
            closeButton,
            balanceText,
            ownedCountText,
            homeLevelText,
            section,
            feedback,
            productScroll,
            tabs,
            cards);
    }

    private static PurchaseDialogData BuildPurchaseDialog(Transform parent, TMP_FontAsset font)
    {
        GameObject root = CreateRect("PurchaseDialog", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        LowPolyPanelGraphic blocker = CreatePanel(
            "DialogBlocker",
            root.transform,
            new Color32(55, 28, 91, 218),
            0f,
            0f,
            true);
        Stretch(blocker.rectTransform);
        BuildPurchaseBackdrop(root.transform);

        LowPolyPanelGraphic shadow = CreatePanel(
            "DialogShadow",
            root.transform,
            new Color32(2, 5, 8, 175),
            32f,
            0f,
            false);
        PremiumUiStyle.SetCenteredShadowRect(
            shadow.rectTransform,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(760f, 454f),
            5f);

        LowPolyPanelGraphic glow = CreatePanel(
            "DialogCandyGlow",
            root.transform,
            new Color32(245, 104, 157, 58),
            36f,
            0f,
            false);
        SetRect(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(778f, 472f));

        LowPolyPanelGraphic frame = CreatePanel(
            "DialogFrame",
            root.transform,
            Gold,
            31f,
            3f,
            false);
        SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 454f));
        LowPolyPanelGraphic face = CreatePanel(
            "DialogFace",
            frame.transform,
            CandyAqua,
            27f,
            2f,
            false);
        StretchWithOffsets(face.rectTransform, 4f, 4f, -4f, -4f);

        LowPolyPanelGraphic ribbon = CreatePanel(
            "PurrfectRibbon", face.transform, CandyPink, 14f, 2f, false);
        SetRect(ribbon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(34f, -15f), new Vector2(244f, 38f));
        LowPolyPanelGraphic ribbonShadow = CreatePanel(
            "PurrfectRibbonShadow", ribbon.transform, PremiumUiStyle.SoftShadow, 14f, 0f, false);
        PremiumUiStyle.SetCenteredShadowStretch(ribbonShadow.rectTransform, 4f);
        ribbonShadow.rectTransform.SetAsFirstSibling();
        RectTransform ribbonPaw = CreateRect("RibbonPaw", ribbon.transform).GetComponent<RectTransform>();
        SetRect(ribbonPaw, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(27f, 27f));
        CreatePawMark(ribbonPaw, Cream, 0.55f);
        TMP_Text eyebrow = CreateText(
            "Eyebrow",
            ribbon.transform,
            font,
            15f,
            Cream,
            TextAlignmentOptions.Center);
        eyebrow.text = "PURRFECT PICK!";
        eyebrow.fontStyle = FontStyles.Bold;
        eyebrow.characterSpacing = 1.4f;
        StretchWithOffsets(eyebrow.rectTransform, 40f, 2f, -10f, -2f);
        CreateSparkle(face.transform, "HeaderSparkleGold", new Vector2(294f, 188f), 16f, CoinHighlight);
        CreateSparkle(face.transform, "HeaderSparkleBlue", new Vector2(323f, 175f), 10f, DiamondHighlight);

        LowPolyPanelGraphic closeFace = CreatePanel(
            "DialogClose",
            face.transform,
            CandyLilac,
            19f,
            2f,
            true);
        SetRect(closeFace.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-22f, -18f), new Vector2(50f, 50f));
        Button closeButton = MakeButton(closeFace.gameObject, closeFace);
        TMP_Text closeLabel = CreateText(
            "CloseLabel", closeFace.transform, font, 25f, Gold, TextAlignmentOptions.Center);
        closeLabel.text = "×";
        closeLabel.fontStyle = FontStyles.Bold;
        Stretch(closeLabel.rectTransform);

        LowPolyPanelGraphic iconFrame = CreatePanel(
            "DialogIconFrame",
            face.transform,
            Gold,
            24f,
            2f,
            false);
        SetRect(iconFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(34f, -12f), new Vector2(244f, 294f));
        LowPolyPanelGraphic iconFace = CreatePanel(
            "DialogIconFace",
            iconFrame.transform,
            new Color32(82, 42, 123, 255),
            20f,
            2f,
            false);
        StretchWithOffsets(iconFace.rectTransform, 4f, 4f, -4f, -4f);
        CreateSparkle(iconFace.transform, "ProductSparkleA", new Vector2(-87f, 119f), 13f, CoinHighlight);
        CreateSparkle(iconFace.transform, "ProductSparkleB", new Vector2(91f, -120f), 10f, DiamondHighlight);
        GameObject iconObject = CreateRect("DialogProductIcon", iconFace.transform);
        EnsureCanvasRenderer(iconObject);
        RawImage icon = iconObject.AddComponent<RawImage>();
        icon.color = Color.white;
        icon.raycastTarget = false;
        StretchWithOffsets(icon.rectTransform, 12f, 12f, -12f, -12f);

        TMP_Text title = CreateText(
            "DialogTitle",
            face.transform,
            font,
            31f,
            Ink,
            TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(310f, -70f), new Vector2(400f, 54f));

        TMP_Text message = CreateText(
            "DialogMessage",
            face.transform,
            font,
            18f,
            Ink,
            TextAlignmentOptions.TopLeft);
        message.textWrappingMode = TextWrappingModes.Normal;
        SetRect(message.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(310f, -132f), new Vector2(400f, 76f));

        CreatePurchaseButton(
            face.transform,
            font,
            "CoinPurchaseButton",
            new Vector2(310f, -238f),
            false,
            out Button coinButton,
            out TMP_Text coinText);
        CreatePurchaseButton(
            face.transform,
            font,
            "DiamondPurchaseButton",
            new Vector2(310f, -326f),
            true,
            out Button diamondButton,
            out TMP_Text diamondText);

        LowPolyPanelGraphic ratePill = CreatePanel(
            "ExchangeRatePill", face.transform, PremiumUiStyle.Night, 12f, 1.5f, false);
        SetRect(ratePill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(660f, 30f));
        TMP_Text rate = CreateText(
            "ExchangeRate",
            ratePill.transform,
            font,
            13f,
            DiamondHighlight,
            TextAlignmentOptions.Center);
        rate.text = "1 DIAMOND = 100 COINS  •  DIAMONDS WORK ACROSS CAT HOME";
        rate.fontStyle = FontStyles.Bold;
        StretchWithOffsets(rate.rectTransform, 10f, 1f, -10f, -1f);

        return new PurchaseDialogData(
            group,
            icon,
            title,
            message,
            coinButton,
            coinText,
            diamondButton,
            diamondText,
            closeButton);
    }

    private static void BuildPurchaseBackdrop(Transform parent)
    {
        Color aqua = new Color32(68, 239, 225, 112);
        Color pink = new Color32(255, 91, 171, 116);
        Color orange = new Color32(255, 178, 61, 104);

        LowPolyPanelGraphic left = CreatePanel(
            "BackdropAquaRibbon", parent, aqua, 72f, 0f, false);
        SetRect(left.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(70f, 10f), new Vector2(480f, 1240f));
        left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);

        LowPolyPanelGraphic right = CreatePanel(
            "BackdropPinkRibbon", parent, pink, 72f, 0f, false);
        SetRect(right.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-60f, -20f), new Vector2(440f, 1200f));
        right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 17f);

        LowPolyPanelGraphic sun = CreatePanel(
            "BackdropSun", parent, orange, 180f, 0f, false);
        SetRect(sun.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(80f, -70f), new Vector2(360f, 360f));
        CreatePawMark(sun.transform, new Color32(255, 239, 202, 110), 3.8f);

        CreateSparkle(parent, "BackdropGlintA", new Vector2(-520f, 240f), 40f, CoinHighlight);
        CreateSparkle(parent, "BackdropGlintB", new Vector2(555f, -245f), 32f, DiamondHighlight);
        CreateSparkle(parent, "BackdropGlintC", new Vector2(485f, 250f), 20f, Cream);
    }

    private static DiamondConfirmationData BuildDiamondConfirmation(
        Transform parent,
        TMP_FontAsset font)
    {
        GameObject root = CreateRect("DiamondConfirmation", parent);
        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        LowPolyPanelGraphic blocker = CreatePanel(
            "ConfirmationBlocker", root.transform, new Color32(57, 29, 99, 224), 0f, 0f, true);
        Stretch(blocker.rectTransform);

        LowPolyPanelGraphic glow = CreatePanel(
            "ConfirmationGlow", root.transform, new Color32(62, 214, 255, 95), 44f, 0f, false);
        SetRect(glow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), Vector2.zero, new Vector2(650f, 390f));
        LowPolyPanelGraphic frame = CreatePanel(
            "ConfirmationFrame", root.transform, DiamondHighlight, 38f, 3f, false);
        SetRect(frame.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), Vector2.zero, new Vector2(630f, 370f));
        LowPolyPanelGraphic face = CreatePanel(
            "ConfirmationFace", frame.transform, CandyLilac, 34f, 2f, false);
        StretchWithOffsets(face.rectTransform, 5f, 5f, -5f, -5f);

        RectTransform diamond = CreateRect("DiamondEmblem", face.transform).GetComponent<RectTransform>();
        SetRect(diamond, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, .5f), new Vector2(0f, -72f), new Vector2(86f, 86f));
        BuildShinyDiamondIcon(diamond);
        CreateSparkle(face.transform, "ConfirmGlintA", new Vector2(-210f, 125f), 18f, CoinHighlight);
        CreateSparkle(face.transform, "ConfirmGlintB", new Vector2(220f, 104f), 14f, DiamondHighlight);

        TMP_Text title = CreateText(
            "ConfirmationTitle", face.transform, font, 31f, Cream, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, .5f), new Vector2(0f, -138f), new Vector2(520f, 48f));
        title.text = "CONFIRM PURCHASE";
        title.fontStyle = FontStyles.Bold;

        TMP_Text message = CreateText(
            "ConfirmationMessage", face.transform, font, 18f, WarmCream, TextAlignmentOptions.Center);
        SetRect(message.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, .5f), new Vector2(0f, -196f), new Vector2(520f, 58f));
        message.textWrappingMode = TextWrappingModes.Normal;

        LowPolyPanelGraphic confirmFace = CreatePanel(
            "ConfirmButton", face.transform, DiamondBlue, 22f, 3f, true);
        SetRect(confirmFace.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(.5f, .5f), new Vector2(82f, 61f), new Vector2(344f, 70f));
        Button confirm = MakeButton(confirmFace.gameObject, confirmFace);
        TMP_Text confirmText = CreateText(
            "Label", confirmFace.transform, font, 18f, Cream, TextAlignmentOptions.Center);
        Stretch(confirmText.rectTransform);
        confirmText.fontStyle = FontStyles.Bold;

        LowPolyPanelGraphic cancelFace = CreatePanel(
            "CancelButton", face.transform, new Color32(255, 105, 145, 255), 22f, 3f, true);
        SetRect(cancelFace.rectTransform, new Vector2(.5f, 0f), new Vector2(.5f, 0f),
            new Vector2(.5f, .5f), new Vector2(-180f, 61f), new Vector2(154f, 70f));
        Button cancel = MakeButton(cancelFace.gameObject, cancelFace);
        TMP_Text cancelText = CreateText(
            "Label", cancelFace.transform, font, 18f, Cream, TextAlignmentOptions.Center);
        Stretch(cancelText.rectTransform);
        cancelText.text = "NOT YET";
        cancelText.fontStyle = FontStyles.Bold;

        return new DiamondConfirmationData(
            group, title, message, confirmText, confirm, cancel);
    }

    private static DiamondStorePanel BuildDiamondStore(Transform parent, TMP_FontAsset font)
    {
        GameObject root = CreateRect("DiamondStore", parent);
        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        LowPolyPanelGraphic blocker = CreatePanel(
            "DiamondStoreBlocker", root.transform, new Color32(57, 29, 99, 232), 0f, 0f, true);
        Stretch(blocker.rectTransform);
        BuildPurchaseBackdrop(root.transform);

        LowPolyPanelGraphic frame = CreatePanel(
            "DiamondStoreFrame", root.transform, DiamondHighlight, 42f, 4f, false);
        SetRect(frame.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), Vector2.zero, new Vector2(1060f, 720f));
        LowPolyPanelGraphic face = CreatePanel(
            "DiamondStoreFace", frame.transform, CandySky, 38f, 2f, false);
        StretchWithOffsets(face.rectTransform, 5f, 5f, -5f, -5f);

        TMP_Text title = CreateText(
            "Title", face.transform, font, 46f, Cream, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -34f), new Vector2(-180f, 64f));
        title.text = "DIAMOND TREASURE";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 1.5f;

        TMP_Text subtitle = CreateText(
            "Subtitle", face.transform, font, 17f, WarmCream, TextAlignmentOptions.Center);
        SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -92f), new Vector2(-160f, 30f));
        subtitle.text = "ONE WALLET FOR EVERY CAT HOME ADVENTURE";

        LowPolyPanelGraphic closeFace = CreatePanel(
            "CloseButton", face.transform, CandyPink, 25f, 2f, true);
        SetRect(closeFace.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(58f, 58f));
        Button close = MakeButton(closeFace.gameObject, closeFace);
        TMP_Text closeText = CreateText(
            "Label", closeFace.transform, font, 26f, Cream, TextAlignmentOptions.Center);
        Stretch(closeText.rectTransform);
        closeText.text = "X";
        closeText.fontStyle = FontStyles.Bold;

        LowPolyPanelGraphic wallet = CreatePanel(
            "Wallet", face.transform, CandyLilac, 20f, 2f, false);
        SetRect(wallet.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
            new Vector2(.5f, 1f), new Vector2(0f, -133f), new Vector2(330f, 58f));
        RectTransform walletIcon = CreateRect("Icon", wallet.transform).GetComponent<RectTransform>();
        SetRect(walletIcon, new Vector2(0f, .5f), new Vector2(0f, .5f),
            new Vector2(.5f, .5f), new Vector2(34f, 0f), new Vector2(44f, 44f));
        BuildShinyDiamondIcon(walletIcon);
        TMP_Text balance = CreateText(
            "Balance", wallet.transform, font, 22f, Cream, TextAlignmentOptions.MidlineLeft);
        StretchWithOffsets(balance.rectTransform, 72f, 0f, -12f, 0f);
        balance.text = "0 DIAMONDS";
        balance.fontStyle = FontStyles.Bold;

        int count = DiamondPackCatalog.Packs.Count;
        string[] ids = new string[count];
        Button[] buttons = new Button[count];
        TMP_Text[] amounts = new TMP_Text[count];
        GameObject[] badges = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            DiamondPackDefinition pack = DiamondPackCatalog.Packs[i];
            ids[i] = pack.ProductId;
            int row = i / 3;
            int column = i % 3;
            float x = -330f + column * 330f;
            float y = 74f - row * 222f;

            LowPolyPanelGraphic cardGlow = CreatePanel(
                "PackGlow_" + pack.DiamondAmount, face.transform,
                new Color32(95, 225, 255, 95), 28f, 0f, false);
            SetRect(cardGlow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(x, y - 4f), new Vector2(286f, 196f));
            LowPolyPanelGraphic card = CreatePanel(
                "Pack_" + pack.DiamondAmount, face.transform,
                row == 0 ? CandyLilac : CandyPink,
                25f, 3f, true);
            SetRect(card.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(x, y), new Vector2(276f, 186f));
            buttons[i] = MakeButton(card.gameObject, card);

            RectTransform icon = CreateRect("Diamond", card.transform).GetComponent<RectTransform>();
            SetRect(icon, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                new Vector2(.5f, .5f), new Vector2(0f, -54f), new Vector2(76f, 76f));
            BuildShinyDiamondIcon(icon);
            amounts[i] = CreateText(
                "Amount", card.transform, font, 29f, Cream, TextAlignmentOptions.Center);
            SetRect(amounts[i].rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(.5f, 0f), new Vector2(0f, 54f), new Vector2(-20f, 42f));
            amounts[i].text = pack.DiamondAmount.ToString("N0");
            amounts[i].fontStyle = FontStyles.Bold;
            TMP_Text action = CreateText(
                "Action", card.transform, font, 14f, DiamondHighlight, TextAlignmentOptions.Center);
            SetRect(action.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(.5f, 0f), new Vector2(0f, 20f), new Vector2(-20f, 25f));
            action.text = "GET PACK";
            action.fontStyle = FontStyles.Bold;

            LowPolyPanelGraphic badge = CreatePanel(
                "BestMatch", card.transform, CoinGold, 10f, 1f, false);
            SetRect(badge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(104f, 28f));
            TMP_Text badgeText = CreateText(
                "Label", badge.transform, font, 11f, Ink, TextAlignmentOptions.Center);
            Stretch(badgeText.rectTransform);
            badgeText.text = "BEST MATCH";
            badgeText.fontStyle = FontStyles.Bold;
            badges[i] = badge.gameObject;
            badge.gameObject.SetActive(false);
        }

        TMP_Text feedback = CreateText(
            "Feedback", face.transform, font, 14f, Cream, TextAlignmentOptions.Center);
        SetRect(feedback.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(.5f, 0f), new Vector2(0f, 12f), new Vector2(-90f, 28f));
        feedback.text = "CHOOSE A DIAMOND PACK";
        feedback.fontStyle = FontStyles.Bold;

        DiamondStorePanel panel = root.AddComponent<DiamondStorePanel>();
        panel.EditorConfigure(group, close, balance, feedback, ids, buttons, amounts, badges);
        return panel;
    }

    private static void CreatePurchaseButton(
        Transform parent,
        TMP_FontAsset font,
        string name,
        Vector2 anchoredPosition,
        bool diamond,
        out Button button,
        out TMP_Text label)
    {
        Color rimColor = diamond ? DiamondBlueDark : BrightOrange;
        Color faceColor = diamond ? DiamondBlueDeep : CoinGold;
        Color labelColor = diamond ? Cream : Ink;
        LowPolyPanelGraphic rim = CreatePanel(name, parent, rimColor, 21f, 2f, true);
        SetRect(rim.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, 1f), anchoredPosition, new Vector2(400f, 70f));
        LowPolyPanelGraphic face = CreatePanel(name + "Face", rim.transform, faceColor, 18f, 2f, false);
        StretchWithOffsets(face.rectTransform, 3f, 3f, -3f, -3f);
        button = MakeButton(rim.gameObject, rim);

        Image gloss = CreateImage(name + "Gloss", face.transform, new Color32(255, 255, 255, 48), false);
        SetRect(gloss.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(-28f, 17f));

        LowPolyPanelGraphic iconBadge = CreatePanel(
            name + "IconBadge", face.transform, PremiumUiStyle.Night, 23f, 1.5f, false);
        SetRect(iconBadge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(46f, 46f));
        if (diamond)
            BuildShinyDiamondIcon(iconBadge.transform);
        else
            BuildShinyCoinIcon(iconBadge.transform);

        label = CreateText(name + "Label", face.transform, font, 18f, labelColor,
            TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Normal;
        label.fontWeight = FontWeight.Regular;
        label.characterSpacing = 1.05f;
        label.extraPadding = true;
        StretchWithOffsets(label.rectTransform, 62f, 4f, -46f, -4f);

        RectTransform paw = CreateRect(name + "Paw", face.transform).GetComponent<RectTransform>();
        SetRect(paw, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-17f, -1f), new Vector2(30f, 30f));
        Color pawColor = diamond ? DiamondHighlight : new Color32(105, 55, 18, 150);
        CreatePawMark(paw, pawColor, 0.56f);
    }

    private static Color GetProductAccent(HomeStoreProduct product)
    {
        switch (product.StoreCategory)
        {
            case HomeStoreCategory.Room:
                return product.Category.Contains("WALL") ? Purple :
                       product.Category.Contains("GREEN") ? Teal : Gold;
            case HomeStoreCategory.Home:
                return product.Category.Contains("OUTDOOR") ? Teal : Orange;
            default:
                return product.Category.Contains("REST") ? Purple :
                       product.Category.Contains("STYLE") ? Pink : Teal;
        }
    }

    private static int CompareCatalogProducts(HomeStoreProduct left, HomeStoreProduct right)
    {
        int category = left.StoreCategory.CompareTo(right.StoreCategory);
        if (category != 0)
            return category;

        long leftPrice = left.SupportsCoins ? left.CoinPrice :
            left.SupportsDiamonds ? left.DiamondPrice * HomeStoreService.CoinsPerDiamond : long.MaxValue;
        long rightPrice = right.SupportsCoins ? right.CoinPrice :
            right.SupportsDiamonds ? right.DiamondPrice * HomeStoreService.CoinsPerDiamond : long.MaxValue;
        int price = leftPrice.CompareTo(rightPrice);
        return price != 0 ? price : string.CompareOrdinal(left.Title, right.Title);
    }

    private static Action<Transform> GetFallbackIcon(HomeStoreProduct product)
    {
        if (product.Id == HomeStoreService.BallBasketId) return BuildBallBasketIcon;
        if (product.Id == HomeStoreService.ScratchPostId) return BuildScratchPostIcon;
        if (product.Id == HomeStoreService.CozyPodBedId ||
            product.Id == HomeStoreService.CloudBedId ||
            product.Id == HomeStoreService.CanopyBedId) return BuildBedIcon;
        if (product.Id == HomeStoreService.ToyMouseId) return BuildMouseIcon;
        if (product.Id == HomeStoreService.PlayTunnelId) return BuildTunnelIcon;
        if (product.StoreCategory == HomeStoreCategory.Home)
            return product.Category.Contains("OUTDOOR") ? BuildGardenIcon : BuildRoomsIcon;
        if (product.Category.Contains("WALL")) return BuildDecorIcon;
        if (product.Category.Contains("ENTERTAINMENT")) return BuildTvIcon;
        if (product.Category.Contains("LIGHT")) return BuildLampIcon;
        return BuildFurnitureIcon;
    }

    private static ScrollRect BuildProductScroll(Transform parent)
    {
        GameObject scrollObject = CreateRect("ProductScroll", parent);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        SetRect(scrollRect, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -134f), new Vector2(-144f, 420f));

        var viewportObject = CreateRect("Viewport", scrollObject.transform);
        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        StretchWithOffsets(viewport, 0f, 0f, -30f, 0f);
        viewportObject.AddComponent<RectMask2D>();

        var contentObject = CreateRect("Content", viewportObject.transform);
        RectTransform content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 2, 2);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LowPolyPanelGraphic scrollbarTrack = CreatePanel(
            "VerticalScrollbar",
            scrollObject.transform,
            CandyCream,
            9f,
            1.5f,
            true);
        SetRect(
            scrollbarTrack.rectTransform,
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(-3f, 0f),
            new Vector2(20f, -10f));
        LowPolyPanelGraphic scrollbarHandle = CreatePanel(
            "Handle",
            scrollbarTrack.transform,
            Gold,
            7f,
            1.5f,
            true);
        StretchWithOffsets(scrollbarHandle.rectTransform, 3f, 3f, -3f, -3f);
        Scrollbar verticalScrollbar = scrollbarTrack.gameObject.AddComponent<Scrollbar>();
        verticalScrollbar.targetGraphic = scrollbarHandle;
        verticalScrollbar.handleRect = scrollbarHandle.rectTransform;
        verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
        verticalScrollbar.transition = Selectable.Transition.ColorTint;
        Navigation scrollbarNavigation = verticalScrollbar.navigation;
        scrollbarNavigation.mode = Navigation.Mode.None;
        verticalScrollbar.navigation = scrollbarNavigation;
        verticalScrollbar.SetValueWithoutNotify(1f);

        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 28f;
        scroll.verticalScrollbar = verticalScrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scroll.verticalScrollbarSpacing = 6f;
        return scroll;
    }

    private static TabData BuildTab(
        Transform parent,
        TMP_FontAsset font,
        HomeStoreCategory category,
        string label,
        float centerX)
    {
        bool selected = category == HomeStoreCategory.Cat;
        Color color = selected ? CandyCream : CandyLilac;
        LowPolyPanelGraphic tabRim = CreatePanel("Tab_" + label + "Rim", parent, Gold, 20f, 2f, false);
        SetRect(tabRim.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(centerX, 140f), new Vector2(374f, 66f));
        LowPolyPanelGraphic face = CreatePanel("Tab_" + label, parent, color, 17f, 2f, true);
        SetRect(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(centerX, 137f), new Vector2(366f, 58f));
        Button button = MakeButton(face.gameObject, face);
        TMP_Text text = CreateText("Label", face.transform, font, 23f,
            selected ? Ink : PremiumUiStyle.ChampagneLight,
            TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 0.5f;
        RectTransform marker = CreateRect("SelectedMarker", face.transform).GetComponent<RectTransform>();
        SetRect(marker, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(27f, 0f), new Vector2(28f, 28f));
        CreatePawMark(marker, PremiumUiStyle.Coral, 0.76f);
        marker.gameObject.SetActive(selected);
        return new TabData(category, button, face, text, marker.gameObject);
    }

    private static void BuildHeader(Transform parent, TMP_FontAsset font, out Button closeButton)
    {
        LowPolyPanelGraphic header = CreatePanel("Header", parent, CandyPink, 30f, 4f, false);
        SetRect(header.rectTransform, Vector2.up, Vector2.one, new Vector2(0.5f, 1f),
            new Vector2(0f, -10f), new Vector2(-24f, 142f));

        LowPolyPanelGraphic emblemRim = CreatePanel("StoreEmblemRim", header.transform, Gold, 48f, 3f, false);
        SetRect(emblemRim.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(72f, 0f), new Vector2(96f, 96f));
        LowPolyPanelGraphic emblemFace = CreatePanel("StoreEmblemFace", emblemRim.transform, PremiumUiStyle.Navy, 40f, 2f, false);
        StretchWithOffsets(emblemFace.rectTransform, 5f, 5f, -5f, -5f);
        BuildStoreBagIcon(emblemFace.transform);

        TMP_Text title = CreateText("Title", header.transform, font, 48f, Cream, TextAlignmentOptions.MidlineLeft);
        title.text = "HOME STORE";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 2f;
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(142f, 18f), new Vector2(770f, 56f));

        TMP_Text subtitle = CreateText("Subtitle", header.transform, font, 16f, PremiumUiStyle.Ink, TextAlignmentOptions.MidlineLeft);
        subtitle.text = "MAKE EVERY ROOM YOUR CAT'S FAVORITE PLACE";
        subtitle.characterSpacing = 1f;
        SetRect(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(144f, -25f), new Vector2(760f, 24f));

        Image accentLine = CreateImage("AccentLine", header.transform, Gold, false);
        SetRect(accentLine.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(-48f, 2f));
        LowPolyPanelGraphic dividerDiamond = CreatePanel("DividerDiamond", header.transform, Gold, 2f, 0f, false);
        SetRect(dividerDiamond.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(10f, 10f));
        dividerDiamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        LowPolyPanelGraphic closeFace = CreatePanel("CloseButton", header.transform, CandyLilac, 29f, 2f, true);
        SetRect(closeFace.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-22f, 15f), new Vector2(54f, 54f));
        closeButton = MakeButton(closeFace.gameObject, closeFace);
        CreateBar("CrossA", closeFace.transform, Gold, new Vector2(28f, 4f), Vector2.zero, 45f);
        CreateBar("CrossB", closeFace.transform, Gold, new Vector2(28f, 4f), Vector2.zero, -45f);
    }

    private static PlacementData BuildPlacementToolbar(Transform parent, TMP_FontAsset font)
    {
        GameObject root = CreateRect("PlacementToolbar", parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1080f, 132f));
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        LowPolyPanelGraphic background = CreatePanel("Background", root.transform, CandyLilac, 24f, 6f, true);
        Stretch(background.rectTransform);
        Image accent = CreateImage("Accent", root.transform, Teal, false);
        SetRect(accent.rectTransform, Vector2.zero, new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(12f, -30f));

        TMP_Text title = CreateText("PlacementTitle", root.transform, font, 27f, Cream, TextAlignmentOptions.MidlineLeft);
        title.text = "PLACE ITEM";
        title.fontStyle = FontStyles.Bold;
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(36f, 25f), new Vector2(350f, 46f));

        TMP_Text counter = CreateText("PlacementCounter", root.transform, font, 18f, WarmCream, TextAlignmentOptions.MidlineLeft);
        counter.text = "DRAG ON FLOOR  •  GREEN = SAFE";
        counter.characterSpacing = 1f;
        SetRect(counter.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(38f, -26f), new Vector2(300f, 32f));

        Button previous = CreateToolbarButton(root.transform, font, "RotateLeft", "-45°", Teal, -500f, 84f);
        Button next = CreateToolbarButton(root.transform, font, "RotateRight", "+45°", Teal, -400f, 84f);
        Button confirm = CreateToolbarButton(root.transform, font, "ConfirmPlacement", "PLACE HERE", CandyAqua, -160f, 230f);
        Button cancel = CreateToolbarButton(root.transform, font, "CancelPlacement", "CANCEL", PremiumUiStyle.Disabled, -10f, 140f);

        return new PlacementData(group, title, counter, previous, next, confirm, cancel);
    }

    private static Button CreateToolbarButton(
        Transform parent,
        TMP_FontAsset font,
        string name,
        string label,
        Color color,
        float rightOffset,
        float width)
    {
        LowPolyPanelGraphic face = CreatePanel(name, parent, color, 30f, 4f, true);
        SetRect(face.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(rightOffset, 0f), new Vector2(width, 70f));
        Button button = MakeButton(face.gameObject, face);
        TMP_Text text = CreateText("Label", face.transform, font, 19f, Cream, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static void BuildSummary(
        Transform parent,
        TMP_FontAsset font,
        out TMP_Text balanceText,
        out TMP_Text ownedCountText,
        out TMP_Text homeLevelText)
    {
        TMP_Text intro = CreateText("Intro", parent, font, 20f, PremiumUiStyle.Ink, TextAlignmentOptions.MidlineLeft);
        intro.text = "Build a happy cat, a personal room and a growing home.";
        SetRect(intro.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.58f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(58f, 223f), new Vector2(-20f, 52f));

        LowPolyPanelGraphic wallet = CreatePanel("Wallet", parent, CandyAqua, 13f, 3f, false);
        SetRect(wallet.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-55f, 228f), new Vector2(230f, 58f));
        RectTransform walletCoin = CreateRect("CoinIcon", wallet.transform).GetComponent<RectTransform>();
        SetRect(walletCoin, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(15f, 0f), new Vector2(40f, 40f));
        BuildShinyCoinIcon(walletCoin);

        balanceText = CreateText("Balance", wallet.transform, font, 26f, Ink, TextAlignmentOptions.MidlineLeft);
        SetRect(balanceText.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            new Vector2(66f, 0f), new Vector2(-14f, 0f));
        balanceText.text = "0";
        balanceText.fontStyle = FontStyles.Bold;

        LowPolyPanelGraphic collectionRim = CreatePanel(
            "CollectionProgressRim", parent, new Color32(245, 104, 157, 255), 16f, 1.5f, false);
        SetRect(collectionRim.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-300f, 228f), new Vector2(330f, 58f));
        LowPolyPanelGraphic collectionFace = CreatePanel(
            "CollectionProgressFace", collectionRim.transform, CandyCream, 14f, 1.5f, false);
        StretchWithOffsets(collectionFace.rectTransform, 3f, 3f, -3f, -3f);
        LowPolyPanelGraphic collectionBadge = CreatePanel(
            "CollectionPawBadge", collectionFace.transform, new Color32(245, 104, 157, 255), 19f, 1.5f, false);
        SetRect(collectionBadge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(38f, 38f));
        CreatePawMark(collectionBadge.transform, Cream, 0.62f);
        TMP_Text collectionLabel = CreateText(
            "CollectionLabel", collectionFace.transform, font, 11f, CandyPink,
            TextAlignmentOptions.MidlineLeft);
        collectionLabel.text = "COLLECTION PROGRESS";
        collectionLabel.characterSpacing = 1.1f;
        StretchWithOffsets(collectionLabel.rectTransform, 55f, 26f, -12f, -2f);
        ownedCountText = CreateText(
            "OwnedCount", collectionFace.transform, font, 17f, Ink, TextAlignmentOptions.MidlineLeft);
        StretchWithOffsets(ownedCountText.rectTransform, 55f, 2f, -12f, -22f);
        ownedCountText.text = "0 OF 2  •  COLLECTED";
        ownedCountText.fontStyle = FontStyles.Normal;
        ownedCountText.fontWeight = FontWeight.Regular;
        CreateSparkle(collectionFace.transform, "CollectionGlint", new Vector2(145f, 13f), 8f, CoinHighlight);
        homeLevelText = BuildHomeLevelBadge(parent, font);
    }

    private static TMP_Text BuildHomeLevelBadge(Transform parent, TMP_FontAsset font)
    {
        GameObject badgeRoot = CreateRect("HomeLevelBadge", parent);
        RectTransform badgeRect = badgeRoot.GetComponent<RectTransform>();
        SetRect(
            badgeRect,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(HomeLevelBadgeX, HomeLevelBadgeY),
            new Vector2(HomeLevelBadgeWidth, HomeLevelBadgeHeight));

        // Gold rim + navy face, echoing the store emblem on the left so the header
        // is bookended by the same premium jewel treatment.
        LowPolyPanelGraphic rim = CreatePanel(
            "HomeLevelRim", badgeRoot.transform, Gold, 16f, 2f, false);
        Stretch(rim.rectTransform);

        LowPolyPanelGraphic face = CreatePanel(
            "HomeLevelFace", badgeRoot.transform, PremiumUiStyle.Navy, 13f, 1.5f, false);
        StretchWithOffsets(face.rectTransform, 2.5f, 2.5f, -2.5f, -2.5f);

        // Child of the face (drawn after the rim) so the text renders on top of
        // both panels instead of being hidden behind the inset face. Centered in
        // the pill with symmetric margins.
        TMP_Text homeLevelText = CreateText(
            "HomeLevelText", face.transform, font, 18f, Cream, TextAlignmentOptions.Center);
        StretchWithOffsets(homeLevelText.rectTransform, 10f, 2f, -10f, 2f);
        homeLevelText.text = "HOME LV. 1";
        homeLevelText.fontStyle = FontStyles.Bold;
        homeLevelText.characterSpacing = 0.5f;
        homeLevelText.gameObject.AddComponent<HomeLevelBadgeLabel>();
        return homeLevelText;
    }

    private static ProductCardData BuildProductCard(
        Transform parent,
        TMP_FontAsset font,
        string productId,
        float centerY,
        Color accent,
        string previewAssetPath,
        Action<Transform> buildIcon)
    {
        GameObject cardObject = CreateRect("Product_" + productId, parent);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 1f);
        cardRect.anchorMax = new Vector2(1f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(0f, 202f);
        LayoutElement layout = cardObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 202f;
        layout.flexibleWidth = 1f;
        CanvasGroup group = cardObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        LowPolyPanelGraphic cardShadow = CreatePanel("CardShadow", cardObject.transform, PremiumUiStyle.SoftShadow, 18f, 0f, false);
        PremiumUiStyle.SetCenteredShadowStretch(cardShadow.rectTransform, 3f);
        LowPolyPanelGraphic cardRim = CreatePanel("CardRim", cardObject.transform, Gold, 18f, 1.5f, false);
        Stretch(cardRim.rectTransform);
        LowPolyPanelGraphic card = CreatePanel("Card", cardObject.transform, CardCream, 15f, 1.5f, true);
        StretchWithOffsets(card.rectTransform, 2f, 2f, -2f, -2f);
        Image rail = CreateImage("AccentRail", cardObject.transform, accent, false);
        SetRect(rail.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(11f, 0f), new Vector2(8f, -34f));

        LowPolyPanelGraphic previewFrame = CreatePanel("PreviewFrame", cardObject.transform, Gold, 19f, 1.5f, false);
        SetRect(previewFrame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(190f, 174f));
        LowPolyPanelGraphic preview = CreatePanel("Preview", previewFrame.transform, PremiumUiStyle.Navy, 15f, 2f, false);
        StretchWithOffsets(preview.rectTransform, 3f, 3f, -3f, -3f);
        GetOrAdd<RectMask2D>(preview.gameObject);
        if (!BuildProductRender(preview.transform, previewAssetPath))
            buildIcon(preview.transform);
        RawImage previewImage = preview.GetComponentInChildren<RawImage>(true);

        TMP_Text category = CreateText("Category", cardObject.transform, font, 15f, accent, TextAlignmentOptions.MidlineLeft);
        category.fontStyle = FontStyles.Bold;
        category.characterSpacing = 2f;
        SetRect(category.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.72f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(238f, 66f), new Vector2(-12f, 26f));
        LowPolyPanelGraphic categoryPill = CreatePanel("CategoryPill", cardObject.transform,
            Color.Lerp(accent, PremiumUiStyle.Ivory, 0.82f), 12f, 1f, false);
        SetRect(categoryPill.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(230f, 64f), new Vector2(170f, 28f));
        category.transform.SetParent(categoryPill.transform, false);
        Stretch(category.rectTransform);
        category.alignment = TextAlignmentOptions.Center;
        category.characterSpacing = 0.4f;

        TMP_Text title = CreateText("ProductTitle", cardObject.transform, font, 31f, Ink, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 0.2f;
        SetRect(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.73f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(238f, 25f), new Vector2(-12f, 39f));

        TMP_Text description = CreateText("Description", cardObject.transform, font, 20f, new Color32(82, 80, 78, 255), TextAlignmentOptions.TopLeft);
        description.textWrappingMode = TextWrappingModes.Normal;
        SetRect(description.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.76f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(238f, -24f), new Vector2(-12f, 68f));

        LowPolyPanelGraphic priceRim = CreatePanel("PricePillRim", cardObject.transform, Gold, 24f, 1.5f, false);
        SetRect(priceRim.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-30f, 48f), new Vector2(268f, 54f));
        LowPolyPanelGraphic pricePill = CreatePanel("PricePill", priceRim.transform, PremiumUiStyle.WarmIvory, 21f, 1.5f, false);
        StretchWithOffsets(pricePill.rectTransform, 2f, 2f, -2f, -2f);

        RectTransform currencyGroup = CreateRect("CurrencyPriceGroup", pricePill.transform).GetComponent<RectTransform>();
        Stretch(currencyGroup);
        RectTransform coinIcon = CreateRect("CoinPriceIcon", currencyGroup).GetComponent<RectTransform>();
        SetRect(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(38f, 38f));
        BuildShinyCoinIcon(coinIcon);
        TMP_Text coinPrice = CreateText("CoinPrice", currencyGroup, font, 20f, Ink, TextAlignmentOptions.Center);
        coinPrice.fontStyle = FontStyles.Bold;
        SetRect(coinPrice.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(49f, 0f), new Vector2(76f, 0f));

        Image divider = CreateImage("CurrencyDivider", currencyGroup, new Color32(57, 191, 255, 120), false);
        SetRect(divider.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(132f, 0f), new Vector2(2f, 27f));

        RectTransform diamondIcon = CreateRect("DiamondPriceIcon", currencyGroup).GetComponent<RectTransform>();
        SetRect(diamondIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(143f, 0f), new Vector2(38f, 38f));
        BuildShinyDiamondIcon(diamondIcon);
        TMP_Text diamondPrice = CreateText("DiamondPrice", currencyGroup, font, 20f, DiamondBlueDark, TextAlignmentOptions.Center);
        diamondPrice.fontStyle = FontStyles.Bold;
        SetRect(diamondPrice.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(182f, 0f), new Vector2(72f, 0f));

        TMP_Text price = CreateText("SpecialPrice", pricePill.transform, font, 21f, Ink, TextAlignmentOptions.Center);
        price.fontStyle = FontStyles.Bold;
        StretchWithOffsets(price.rectTransform, 8f, 0f, -8f, 0f);
        HomeStoreProduct cardProduct;
        bool showsCurrency = HomeStoreService.TryGetProduct(productId, out cardProduct) &&
                             cardProduct.IsAvailable &&
                             (cardProduct.SupportsCoins || cardProduct.SupportsDiamonds);
        currencyGroup.gameObject.SetActive(showsCurrency);
        price.gameObject.SetActive(!showsCurrency);

        LowPolyPanelGraphic actionFace = CreatePanel("BuyButton", cardObject.transform, PremiumUiStyle.CoralLift, 28f, 2f, true);
        SetRect(actionFace.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-30f, -38f), new Vector2(268f, 64f));
        Button button = MakeButton(actionFace.gameObject, actionFace);
        Image actionGloss = CreateImage("ButtonGloss", actionFace.transform, new Color32(255, 255, 255, 45), false);
        SetRect(actionGloss.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(-24f, 18f));
        TMP_Text action = CreateText("ActionLabel", actionFace.transform, font, 19f, Cream, TextAlignmentOptions.Center);
        StretchWithOffsets(action.rectTransform, 8f, 0f, -32f, 0f);
        action.text = "BUY";
        action.fontStyle = FontStyles.Normal;
        action.fontWeight = FontWeight.Regular;
        action.characterSpacing = 1.35f;
        action.enableAutoSizing = true;
        action.fontSizeMin = 13f;
        action.fontSizeMax = 19f;
        action.extraPadding = true;
        RectTransform paw = CreateRect("PawMark", actionFace.transform).GetComponent<RectTransform>();
        SetRect(paw, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-13f, 0f), new Vector2(28f, 28f));
        CreatePawMark(paw, PremiumUiStyle.ChampagneLight, 0.75f);

        LowPolyPanelGraphic ownedBadge = CreatePanel("OwnedBadge", cardObject.transform, Teal, 9f, 2f, false);
        SetRect(ownedBadge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(1f, 1f), new Vector2(-21f, -14f), new Vector2(92f, 28f));
        TMP_Text ownedText = CreateText("OwnedText", ownedBadge.transform, font, 14f, Cream, TextAlignmentOptions.Center);
        Stretch(ownedText.rectTransform);
        ownedText.text = "AT HOME";
        ownedText.fontStyle = FontStyles.Bold;
        ownedBadge.gameObject.SetActive(false);

        return new ProductCardData(
            productId,
            button,
            group,
            category,
            title,
            description,
            price,
            currencyGroup.gameObject,
            coinPrice,
            diamondPrice,
            previewImage,
            action,
            actionFace,
            ownedBadge.gameObject);
    }

    private static void BuildBallBasketIcon(Transform parent)
    {
        LowPolyPanelGraphic basket = CreatePanel("Basket", parent, Purple, 15f, 4f, false);
        SetRect(basket.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(94f, 55f));
        CreateBar("BasketLineA", basket.transform, Cream, new Vector2(64f, 4f), new Vector2(0f, 10f), 0f);
        CreateBar("BasketLineB", basket.transform, Cream, new Vector2(56f, 4f), new Vector2(0f, -2f), 0f);
        CreateBall(parent, "BallOrange", Orange, new Vector2(-31f, 25f), 38f);
        CreateBall(parent, "BallTeal", Teal, new Vector2(9f, 35f), 43f);
        CreateBall(parent, "BallGold", Gold, new Vector2(36f, 17f), 33f);
    }

    private static void BuildScratchPostIcon(Transform parent)
    {
        LowPolyPanelGraphic basePiece = CreatePanel("Base", parent, Teal, 13f, 3f, false);
        SetRect(basePiece.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -48f), new Vector2(92f, 24f));
        Image post = CreateImage("Post", parent, Gold, false);
        SetRect(post.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(28f, 92f));
        for (int i = -3; i <= 3; i++)
            CreateBar("Rope" + i, post.transform, Cream, new Vector2(28f, 3f), new Vector2(0f, i * 10f), 0f);
        LowPolyPanelGraphic cap = CreatePanel("Top", parent, Orange, 10f, 3f, false);
        SetRect(cap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 51f), new Vector2(76f, 22f));
    }

    private static void BuildBedIcon(Transform parent)
    {
        LowPolyPanelGraphic basePiece = CreatePanel("BedBase", parent, Purple, 11f, 3f, false);
        SetRect(basePiece.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(112f, 30f));
        LowPolyPanelGraphic cushion = CreatePanel("Cushion", parent, WarmCream, 15f, 3f, false);
        SetRect(cushion.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(88f, 45f));
        LowPolyPanelGraphic back = CreatePanel("Back", parent, Orange, 14f, 3f, false);
        SetRect(back.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(106f, 39f));
    }

    private static void BuildMouseIcon(Transform parent)
    {
        LowPolyPanelGraphic body = CreatePanel("MouseBody", parent, Teal, 26f, 3f, false);
        SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(76f, 91f));
        CreateBall(parent, "EarLeft", Pink, new Vector2(-29f, 37f), 31f);
        CreateBall(parent, "EarRight", Pink, new Vector2(29f, 37f), 31f);
        CreateBall(parent, "Nose", Ink, new Vector2(0f, -55f), 17f);
        CreateBar("Tail", parent, Pink, new Vector2(61f, 6f), new Vector2(44f, 9f), -29f);
    }

    private static void BuildTunnelIcon(Transform parent)
    {
        LowPolyPanelGraphic outer = CreatePanel("TunnelOuter", parent, Teal, 44f, 4f, false);
        SetRect(outer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(125f, 114f));
        LowPolyPanelGraphic opening = CreatePanel("TunnelOpening", outer.transform, Ink, 35f, 3f, false);
        SetRect(opening.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(79f, 76f));
        CreateBar("TunnelBand", outer.transform, Orange, new Vector2(11f, 101f), new Vector2(44f, 0f), 0f);
        CreateBall(opening.transform, "HangingBall", Gold, new Vector2(0f, -17f), 21f);
    }

    private static void BuildTvIcon(Transform parent)
    {
        LowPolyPanelGraphic cabinet = CreatePanel("Cabinet", parent, Orange, 10f, 3f, false);
        SetRect(cabinet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(123f, 42f));
        LowPolyPanelGraphic frame = CreatePanel("ScreenFrame", parent, Ink, 10f, 4f, false);
        SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(112f, 76f));
        LowPolyPanelGraphic screen = CreatePanel("Screen", frame.transform, Teal, 5f, 2f, false);
        StretchWithOffsets(screen.rectTransform, 10f, 10f, -10f, -10f);
    }

    private static void BuildLampIcon(Transform parent)
    {
        LowPolyPanelGraphic shade = CreatePanel("Shade", parent, Orange, 18f, 3f, false);
        SetRect(shade.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(94f, 55f));
        CreateBar("Pole", parent, Gold, new Vector2(9f, 83f), new Vector2(0f, -24f), 0f);
        LowPolyPanelGraphic basePiece = CreatePanel("Base", parent, Ink, 14f, 3f, false);
        SetRect(basePiece.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -63f), new Vector2(73f, 23f));
    }

    private static void BuildSideTableIcon(Transform parent)
    {
        LowPolyPanelGraphic top = CreatePanel("TableTop", parent, Orange, 21f, 4f, false);
        SetRect(top.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 36f), new Vector2(117f, 39f));
        CreateBar("Pedestal", parent, Teal, new Vector2(15f, 72f), new Vector2(0f, -20f), 0f);
        LowPolyPanelGraphic basePiece = CreatePanel("TableBase", parent, Ink, 12f, 3f, false);
        SetRect(basePiece.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -61f), new Vector2(78f, 22f));
    }

    private static void BuildFurnitureIcon(Transform parent)
    {
        LowPolyPanelGraphic back = CreatePanel("SofaBack", parent, Teal, 13f, 3f, false);
        SetRect(back.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(106f, 58f));
        LowPolyPanelGraphic seat = CreatePanel("SofaSeat", parent, Orange, 11f, 3f, false);
        SetRect(seat.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -22f), new Vector2(118f, 38f));
        CreateBar("LegLeft", parent, Ink, new Vector2(8f, 23f), new Vector2(-42f, -50f), 0f);
        CreateBar("LegRight", parent, Ink, new Vector2(8f, 23f), new Vector2(42f, -50f), 0f);
    }

    private static void BuildDecorIcon(Transform parent)
    {
        LowPolyPanelGraphic frame = CreatePanel("PictureFrame", parent, Purple, 12f, 5f, false);
        SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 112f));
        LowPolyPanelGraphic art = CreatePanel("Art", frame.transform, WarmCream, 7f, 2f, false);
        StretchWithOffsets(art.rectTransform, 13f, 13f, -13f, -13f);
        CreateBar("MountainA", art.transform, Teal, new Vector2(62f, 9f), new Vector2(-16f, -7f), 35f);
        CreateBar("MountainB", art.transform, Orange, new Vector2(55f, 9f), new Vector2(21f, -4f), -40f);
        CreateBall(art.transform, "Sun", Gold, new Vector2(23f, 24f), 25f);
    }

    private static void BuildRoomsIcon(Transform parent)
    {
        CreateBar("RoofLeft", parent, Orange, new Vector2(82f, 12f), new Vector2(-27f, 39f), 35f);
        CreateBar("RoofRight", parent, Orange, new Vector2(82f, 12f), new Vector2(27f, 39f), -35f);
        LowPolyPanelGraphic house = CreatePanel("House", parent, WarmCream, 9f, 3f, false);
        SetRect(house.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(104f, 76f));
        LowPolyPanelGraphic door = CreatePanel("Door", house.transform, Teal, 7f, 2f, false);
        SetRect(door.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(31f, 51f));
    }

    private static void BuildGardenIcon(Transform parent)
    {
        CreateBar("Stem", parent, Teal, new Vector2(10f, 79f), new Vector2(0f, -18f), 0f);
        CreateBar("LeafLeft", parent, Teal, new Vector2(43f, 15f), new Vector2(-19f, -5f), 28f);
        CreateBar("LeafRight", parent, Teal, new Vector2(43f, 15f), new Vector2(19f, 12f), -28f);
        CreateBall(parent, "PetalA", Orange, new Vector2(-21f, 42f), 39f);
        CreateBall(parent, "PetalB", Purple, new Vector2(21f, 42f), 39f);
        CreateBall(parent, "FlowerCenter", Gold, new Vector2(0f, 39f), 35f);
        LowPolyPanelGraphic grass = CreatePanel("Grass", parent, Teal, 9f, 2f, false);
        SetRect(grass.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -58f), new Vector2(122f, 18f));
    }

    private static void BuildStoreBagIcon(Transform parent)
    {
        Image handleLeft = CreateImage("HandleLeft", parent, Gold, false);
        SetRect(handleLeft.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-13f, 18f), new Vector2(4f, 25f));
        Image handleRight = CreateImage("HandleRight", parent, Gold, false);
        SetRect(handleRight.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(13f, 18f), new Vector2(4f, 25f));
        Image handleTop = CreateImage("HandleTop", parent, Gold, false);
        SetRect(handleTop.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(30f, 4f));
        LowPolyPanelGraphic bag = CreatePanel("Bag", parent, PremiumUiStyle.NavyLift, 8f, 2f, false);
        SetRect(bag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -7f), new Vector2(48f, 48f));
        CreatePawMark(bag.transform, Gold, 0.78f);
    }

    private static void BuildShinyCoinIcon(Transform parent)
    {
        if (PremiumUiFactory.BuildCurrencyIcon(
                parent,
                PremiumUiFactory.CurrencyVisual.Coin))
        {
            return;
        }

        LowPolyPanelGraphic shadow = CreatePanel(
            "CoinShadow", parent, new Color32(74, 37, 8, 120), 18f, 0f, false);
        SetRect(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(1f, -2f), new Vector2(36f, 36f));
        LowPolyPanelGraphic rim = CreatePanel(
            "CoinGoldRim", parent, CoinGoldDark, 18f, 1.5f, false);
        SetRect(rim.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36f, 36f));
        LowPolyPanelGraphic face = CreatePanel(
            "CoinGoldFace", parent, CoinGold, 15f, 1.5f, false);
        SetRect(face.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(29f, 29f));
        LowPolyPanelGraphic stamp = CreatePanel(
            "CoinStamp", parent, CoinGoldDeep, 7f, 1f, false);
        SetRect(stamp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(15f, 17f));
        RectTransform paw = CreateRect("CoinPaw", stamp.transform).GetComponent<RectTransform>();
        Stretch(paw);
        CreatePawMark(paw, CoinHighlight, 0.34f);
        Image shine = CreateImage("CoinShine", parent, CoinHighlight, false);
        SetRect(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-8f, 7f), new Vector2(4f, 10f));
        shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -28f);
    }

    private static void BuildShinyDiamondIcon(Transform parent)
    {
        if (PremiumUiFactory.BuildCurrencyIcon(
                parent,
                PremiumUiFactory.CurrencyVisual.Diamond))
        {
            return;
        }

        Image shadow = CreateImage("GemShadow", parent, new Color32(2, 42, 91, 200), false);
        SetRect(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(1f, -3f), new Vector2(28f, 28f));
        shadow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image outline = CreateImage("GemOutline", parent, DiamondBlueDark, false);
        SetRect(outline.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), new Vector2(28f, 28f));
        outline.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image body = CreateImage("GemBody", parent, DiamondBlue, false);
        SetRect(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -1f), new Vector2(23f, 23f));
        body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image innerFacet = CreateImage("GemInnerFacet", parent, DiamondBlueDeep, false);
        SetRect(innerFacet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(1f, -1f), new Vector2(15f, 15f));
        innerFacet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image facet = CreateImage("GemFacet", parent, DiamondHighlight, false);
        SetRect(facet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-5f, 0f), new Vector2(4f, 14f));
        facet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -35f);
        CreateSparkle(parent, "GemGlint", new Vector2(8f, 10f), 8f, Color.white);
    }

    private static void CreateSparkle(
        Transform parent, string name, Vector2 position, float size, Color color)
    {
        RectTransform root = CreateRect(name, parent).GetComponent<RectTransform>();
        SetRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        Image vertical = CreateImage("Vertical", root, color, false);
        SetRect(vertical.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.23f, size));
        Image horizontal = CreateImage("Horizontal", root, color, false);
        SetRect(horizontal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size * 0.23f));
    }

    private static void CreatePawMark(Transform parent, Color color, float scale)
    {
        CreatePawDot(parent, "Pad", color, new Vector2(0f, -4f) * scale, new Vector2(15f, 12f) * scale);
        CreatePawDot(parent, "ToeL", color, new Vector2(-10f, 7f) * scale, new Vector2(8f, 10f) * scale);
        CreatePawDot(parent, "ToeM", color, new Vector2(0f, 11f) * scale, new Vector2(8f, 10f) * scale);
        CreatePawDot(parent, "ToeR", color, new Vector2(10f, 7f) * scale, new Vector2(8f, 10f) * scale);
    }

    private static void CreatePawDot(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        LowPolyPanelGraphic dot = CreatePanel(name, parent, color, Mathf.Min(size.x, size.y) * 0.5f, 1f, false);
        SetRect(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, size);
    }

    private static void CreateFooterOrnament(Transform parent, float x)
    {
        Image lineA = CreateImage("FooterLineA", parent, Gold, false);
        SetRect(lineA.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(100f, 2f));
        LowPolyPanelGraphic diamond = CreatePanel("FooterDiamond", parent, Gold, 2f, 1f, false);
        SetRect(diamond.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(x + (x < 0f ? 58f : -58f), 0f), new Vector2(9f, 9f));
        diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void CreateBall(Transform parent, string name, Color color, Vector2 position, float size)
    {
        LowPolyPanelGraphic ball = CreatePanel(name, parent, color, size * 0.42f, 3f, false);
        SetRect(ball.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
    }

    private static bool BuildProductRender(Transform parent, string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return false;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (texture == null)
            return false;

        GameObject renderObject = CreateRect("ProductRender", parent);
        EnsureCanvasRenderer(renderObject);
        RawImage render = renderObject.AddComponent<RawImage>();
        render.texture = texture;
        render.color = Color.white;
        render.raycastTarget = false;
        render.uvRect = RoomPreviewFit.CoverUv(texture.width, texture.height, 184f, 168f);
        SetRect(render.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f));
        return true;
    }

    private static Button MakeButton(GameObject root, Graphic target)
    {
        Button button = root.AddComponent<Button>();
        button.targetGraphic = target;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.86f, 1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.disabledColor = Color.white;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        return button;
    }

    private static LowPolyPanelGraphic CreatePanel(
        string name,
        Transform parent,
        Color color,
        float cornerCut,
        float bevel,
        bool raycastTarget)
    {
        GameObject go = CreateRect(name, parent);
        EnsureCanvasRenderer(go);
        LowPolyPanelGraphic panel = go.AddComponent<LowPolyPanelGraphic>();
        panel.color = color;
        panel.raycastTarget = raycastTarget;
        PremiumUiStyle.ConfigureSurface(panel, color, cornerCut, bevel);
        return panel;
    }

    private static bool SameColor(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.002f &&
               Mathf.Abs(a.g - b.g) < 0.002f &&
               Mathf.Abs(a.b - b.b) < 0.002f &&
               Mathf.Abs(a.a - b.a) < 0.002f;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject go = CreateRect(name, parent);
        EnsureCanvasRenderer(go);
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float size,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject go = CreateRect(name, parent);
        EnsureCanvasRenderer(go);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.extraPadding = true;
        text.isTextObjectScaleStatic = true;
        return text;
    }

    private static void CreateBar(
        string name,
        Transform parent,
        Color color,
        Vector2 size,
        Vector2 position,
        float rotation)
    {
        Image bar = CreateImage(name, parent, color, false);
        SetRect(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), position, size);
        bar.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return go;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchWithOffsets(
        RectTransform rect,
        float left,
        float bottom,
        float right,
        float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static GameObject CreateRoot(Scene scene)
    {
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static GameObject PrepareExistingRoot(GameObject root)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(root) &&
            PrefabUtility.GetNearestPrefabInstanceRoot(root) == root)
        {
            PrefabUtility.UnpackPrefabInstance(
                root,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        RemoveDuplicates<ShopPanelController>(root);
        RemoveDuplicates<GraphicRaycaster>(root);
        RemoveDuplicates<CanvasScaler>(root);
        RemoveDuplicates<Canvas>(root);
        RemoveDuplicates<CanvasGroup>(root);
        return root;
    }

    private static GameObject FindSceneRoot(Scene scene, string name)
    {
        if (!scene.IsValid())
            return null;
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid())
            return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }
        return null;
    }

    private static TMP_FontAsset FindFont(Scene scene)
    {
        TMP_Text existing = FindInScene<TMP_Text>(scene);
        if (existing != null && existing.font != null)
            return existing.font;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);
        if (font != null)
            return font;

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Fredoka-SemiBold SDF.asset");
        if (font != null)
            return font;

        string[] ids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        return ids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(ids[0]))
            : null;
    }

    private static void Assign(SerializedObject serialized, string name, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException("Missing ShopPanelController field: " + name);
        property.objectReferenceValue = value;
    }

    private static void AssignCards(SerializedObject serialized, List<ProductCardData> cards)
    {
        SerializedProperty property = serialized.FindProperty("cards");
        if (property == null)
            throw new InvalidOperationException("Missing ShopPanelController cards field.");
        property.arraySize = cards.Count;
        for (int i = 0; i < cards.Count; i++)
        {
            SerializedProperty item = property.GetArrayElementAtIndex(i);
            ProductCardData card = cards[i];
            item.FindPropertyRelative("productId").stringValue = card.ProductId;
            item.FindPropertyRelative("button").objectReferenceValue = card.Button;
            item.FindPropertyRelative("group").objectReferenceValue = card.Group;
            item.FindPropertyRelative("categoryText").objectReferenceValue = card.Category;
            item.FindPropertyRelative("titleText").objectReferenceValue = card.Title;
            item.FindPropertyRelative("descriptionText").objectReferenceValue = card.Description;
            item.FindPropertyRelative("priceText").objectReferenceValue = card.Price;
            item.FindPropertyRelative("currencyPriceGroup").objectReferenceValue = card.CurrencyPriceGroup;
            item.FindPropertyRelative("coinPriceText").objectReferenceValue = card.CoinPrice;
            item.FindPropertyRelative("diamondPriceText").objectReferenceValue = card.DiamondPrice;
            item.FindPropertyRelative("previewImage").objectReferenceValue = card.PreviewImage;
            item.FindPropertyRelative("actionText").objectReferenceValue = card.Action;
            item.FindPropertyRelative("actionBackground").objectReferenceValue = card.ActionBackground;
            item.FindPropertyRelative("ownedBadge").objectReferenceValue = card.OwnedBadge;
        }
    }

    private static void AssignTabs(SerializedObject serialized, List<TabData> tabs)
    {
        SerializedProperty property = serialized.FindProperty("tabs");
        if (property == null)
            throw new InvalidOperationException("Missing ShopPanelController tabs field.");
        property.arraySize = tabs.Count;
        for (int i = 0; i < tabs.Count; i++)
        {
            SerializedProperty item = property.GetArrayElementAtIndex(i);
            TabData tab = tabs[i];
            item.FindPropertyRelative("category").enumValueIndex = (int)tab.Category;
            item.FindPropertyRelative("button").objectReferenceValue = tab.Button;
            item.FindPropertyRelative("background").objectReferenceValue = tab.Background;
            item.FindPropertyRelative("label").objectReferenceValue = tab.Label;
            item.FindPropertyRelative("selectedMarker").objectReferenceValue = tab.SelectedMarker;
        }
    }

    private static void ValidateButtons(GameObject root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            if (buttons[i].targetGraphic == null)
                throw new InvalidOperationException("Store button has no target graphic: " + buttons[i].name);
    }

    private static void EnsureCanvasRenderer(GameObject go)
    {
        if (go.GetComponent<CanvasRenderer>() == null)
            go.AddComponent<CanvasRenderer>();
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T current = go.GetComponent<T>();
        return current != null ? current : go.AddComponent<T>();
    }

    private static void RemoveDuplicates<T>(GameObject go) where T : Component
    {
        T[] components = go.GetComponents<T>();
        for (int i = components.Length - 1; i > 0; i--)
            UnityEngine.Object.DestroyImmediate(components[i]);
    }

    private static void EnsureFolder()
    {
        if (!Directory.Exists(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "UI");
    }

    private static void ConfigureProductIconImporters()
    {
        if (!AssetDatabase.IsValidFolder(ProductIconFolder))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ProductIconFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;
            bool dirty = false;
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
            if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
            if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
            if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
            if (dirty)
                importer.SaveAndReimport();
        }
    }

    private readonly struct PanelData
    {
        public PanelData(
            RectTransform rect,
            CanvasGroup group,
            Button closeButton,
            TMP_Text balanceText,
            TMP_Text ownedCountText,
            TMP_Text homeLevelText,
            TMP_Text sectionTitleText,
            TMP_Text feedbackText,
            ScrollRect productScrollRect,
            List<TabData> tabs,
            List<ProductCardData> cards)
        {
            Rect = rect;
            Group = group;
            CloseButton = closeButton;
            BalanceText = balanceText;
            OwnedCountText = ownedCountText;
            HomeLevelText = homeLevelText;
            SectionTitleText = sectionTitleText;
            FeedbackText = feedbackText;
            ProductScrollRect = productScrollRect;
            Tabs = tabs;
            Cards = cards;
        }

        public RectTransform Rect { get; }
        public CanvasGroup Group { get; }
        public Button CloseButton { get; }
        public TMP_Text BalanceText { get; }
        public TMP_Text OwnedCountText { get; }
        public TMP_Text HomeLevelText { get; }
        public TMP_Text SectionTitleText { get; }
        public TMP_Text FeedbackText { get; }
        public ScrollRect ProductScrollRect { get; }
        public List<TabData> Tabs { get; }
        public List<ProductCardData> Cards { get; }
    }

    private readonly struct PurchaseDialogData
    {
        public PurchaseDialogData(
            CanvasGroup group,
            RawImage icon,
            TMP_Text title,
            TMP_Text message,
            Button coinButton,
            TMP_Text coinButtonText,
            Button diamondButton,
            TMP_Text diamondButtonText,
            Button cancelButton)
        {
            Group = group;
            Icon = icon;
            Title = title;
            Message = message;
            CoinButton = coinButton;
            CoinButtonText = coinButtonText;
            DiamondButton = diamondButton;
            DiamondButtonText = diamondButtonText;
            CancelButton = cancelButton;
        }

        public CanvasGroup Group { get; }
        public RawImage Icon { get; }
        public TMP_Text Title { get; }
        public TMP_Text Message { get; }
        public Button CoinButton { get; }
        public TMP_Text CoinButtonText { get; }
        public Button DiamondButton { get; }
        public TMP_Text DiamondButtonText { get; }
        public Button CancelButton { get; }
    }

    private readonly struct DiamondConfirmationData
    {
        public DiamondConfirmationData(
            CanvasGroup group,
            TMP_Text title,
            TMP_Text message,
            TMP_Text confirmText,
            Button confirmButton,
            Button cancelButton)
        {
            Group = group;
            Title = title;
            Message = message;
            ConfirmText = confirmText;
            ConfirmButton = confirmButton;
            CancelButton = cancelButton;
        }

        public CanvasGroup Group { get; }
        public TMP_Text Title { get; }
        public TMP_Text Message { get; }
        public TMP_Text ConfirmText { get; }
        public Button ConfirmButton { get; }
        public Button CancelButton { get; }
    }

    private readonly struct TabData
    {
        public TabData(
            HomeStoreCategory category,
            Button button,
            Graphic background,
            TMP_Text label,
            GameObject selectedMarker)
        {
            Category = category;
            Button = button;
            Background = background;
            Label = label;
            SelectedMarker = selectedMarker;
        }

        public HomeStoreCategory Category { get; }
        public Button Button { get; }
        public Graphic Background { get; }
        public TMP_Text Label { get; }
        public GameObject SelectedMarker { get; }
    }

    private readonly struct ProductCardData
    {
        public ProductCardData(
            string productId,
            Button button,
            CanvasGroup group,
            TMP_Text category,
            TMP_Text title,
            TMP_Text description,
            TMP_Text price,
            GameObject currencyPriceGroup,
            TMP_Text coinPrice,
            TMP_Text diamondPrice,
            RawImage previewImage,
            TMP_Text action,
            Graphic actionBackground,
            GameObject ownedBadge)
        {
            ProductId = productId;
            Button = button;
            Group = group;
            Category = category;
            Title = title;
            Description = description;
            Price = price;
            CurrencyPriceGroup = currencyPriceGroup;
            CoinPrice = coinPrice;
            DiamondPrice = diamondPrice;
            PreviewImage = previewImage;
            Action = action;
            ActionBackground = actionBackground;
            OwnedBadge = ownedBadge;
        }

        public string ProductId { get; }
        public Button Button { get; }
        public CanvasGroup Group { get; }
        public TMP_Text Category { get; }
        public TMP_Text Title { get; }
        public TMP_Text Description { get; }
        public TMP_Text Price { get; }
        public GameObject CurrencyPriceGroup { get; }
        public TMP_Text CoinPrice { get; }
        public TMP_Text DiamondPrice { get; }
        public RawImage PreviewImage { get; }
        public TMP_Text Action { get; }
        public Graphic ActionBackground { get; }
        public GameObject OwnedBadge { get; }
    }

    private readonly struct PlacementData
    {
        public PlacementData(
            CanvasGroup group,
            TMP_Text title,
            TMP_Text counter,
            Button previous,
            Button next,
            Button confirm,
            Button cancel)
        {
            Group = group;
            Title = title;
            Counter = counter;
            Previous = previous;
            Next = next;
            Confirm = confirm;
            Cancel = cancel;
        }

        public CanvasGroup Group { get; }
        public TMP_Text Title { get; }
        public TMP_Text Counter { get; }
        public Button Previous { get; }
        public Button Next { get; }
        public Button Confirm { get; }
        public Button Cancel { get; }
    }
}
