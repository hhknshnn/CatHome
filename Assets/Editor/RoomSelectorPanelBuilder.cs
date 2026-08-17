using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Authors the persistent, navigation-only room picker in CatHome_UI. Purchases
/// stay in Home Store; this canvas only asks LevelLoader to visit owned rooms.
/// </summary>
public static class RoomSelectorPanelBuilder
{
    public const string RootName = "RoomSelectorPanelCanvas";
    public const string PrefabPath = "Assets/UI/RoomSelectorPanel.prefab";

    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private const string LivingPreviewPath =
        "Assets/Art/RoomPreviews/LivingRoomPreview.png";
    private const string BathroomPreviewPath =
        "Assets/Art/RoomPreviews/BathroomPreview.png";
    private const string KitchenPreviewPath =
        "Assets/Art/RoomPreviews/KitchenPreview.png";
    private const string BedroomPreviewPath =
        "Assets/Art/RoomPreviews/BedroomPreview.png";
    private const string GardenPreviewPath =
        "Assets/Art/RoomPreviews/GardenPreview.png";

    private static readonly Color Scrim = new Color32(44, 27, 67, 174);
    private static readonly Color Ink = PremiumUiStyle.Ink;
    private static readonly Color Cream = PremiumUiStyle.CandyCloud;

    [MenuItem("Tools/Cat Home/Rooms/Build Room Selector")]
    public static void BuildFromMenu()
    {
        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Rooms", result, "OK");
    }

    public static string BuildSilently()
    {
        ConfigurePreviewImporter(LivingPreviewPath);
        ConfigurePreviewImporter(BathroomPreviewPath);
        ConfigurePreviewImporter(KitchenPreviewPath);
        ConfigurePreviewImporter(BedroomPreviewPath);
        ConfigurePreviewImporter(GardenPreviewPath);

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
        canvas.sortingOrder = 116;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAdd<GraphicRaycaster>(root);

        RoomSelectorPanel controller = GetOrAdd<RoomSelectorPanel>(root);
        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        RectTransform safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea);
        GetOrAdd<SafeAreaRect>(safeArea.gameObject);

        Image scrim = CreateImage("RoomSelectorScrim", safeArea, Scrim, true);
        Stretch(scrim.rectTransform);
        Button scrimButton = GetOrAdd<Button>(scrim.gameObject);
        scrimButton.targetGraphic = scrim;
        scrimButton.transition = Selectable.Transition.None;

        RectTransform glow = CreateRect("AquaGlow", safeArea);
        SetCentered(glow, new Vector2(1544f, 868f), new Vector2(0f, -10f));
        LowPolyPanelGraphic glowGraphic = AddPanel(glow.gameObject,
            new Color32(53, 231, 216, 88), 48f, 10f, false);
        PremiumUiStyle.ConfigureShadowSurface(glowGraphic,
            new Color32(45, 220, 209, 96), 48f);

        RectTransform depth = CreateRect("CoralDepth", safeArea);
        SetCentered(depth, new Vector2(1540f, 864f), Vector2.zero);
        AddPanel(depth.gameObject, PremiumUiStyle.CoralLift, 44f, 10f, false);

        RectTransform panel = CreateRect("RoomSelectorPanelVisual", safeArea);
        SetCentered(panel, new Vector2(1520f, 840f), Vector2.zero);
        CanvasGroup panelGroup = GetOrAdd<CanvasGroup>(panel.gameObject);
        LowPolyPanelGraphic panelFace = AddPanel(panel.gameObject, Cream, 44f, 14f, true);
        PremiumUiStyle.ConfigureAccentSurface(
            panelFace,
            new Color32(255, 239, 191, 255),
            new Color32(211, 249, 234, 255),
            44f,
            14f);

        BuildHeader(panel, font);

        ScrollRect roomScroll = BuildRoomScroll(panel);
        RectTransform grid = roomScroll.content;
        var cards = new List<CardParts>(HomeRoomService.Rooms.Count);
        for (int i = 0; i < HomeRoomService.Rooms.Count; i++)
        {
            HomeRoomDefinition room = HomeRoomService.Rooms[i];
            GetRoomCardVisuals(room.Id, out string subtitle, out string previewPath,
                out Color accent, out string theme);
            cards.Add(BuildRoomCard(grid, font, room.Id, room.DisplayName, subtitle,
                previewPath, accent, theme));
        }

        TMP_Text feedback = CreateText("RoomFeedback", panel, font, 22f, Ink,
            TextAlignmentOptions.Center);
        SetCentered(feedback.rectTransform, new Vector2(1120f, 40f), new Vector2(0f, -378f));
        feedback.text = "CURRENT HOME  •  LIVING ROOM";
        feedback.fontStyle = FontStyles.Bold;
        feedback.characterSpacing = 1.1f;
        feedback.overflowMode = TextOverflowModes.Ellipsis;

        Button closeButton = BuildCloseButton(panel, font);
        BuildSparkle(panel, "SparkleLeft", new Vector2(-700f, 368f), 16f,
            PremiumUiStyle.CandyLemon, 18f);
        BuildSparkle(panel, "SparkleRight", new Vector2(700f, -368f), 14f,
            PremiumUiStyle.CandyPink, -12f);

        var serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", null);
        Assign(serialized, "levelLoader", null);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "safeArea", safeArea);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "panelVisual", panel);
        Assign(serialized, "panelGroup", panelGroup);
        Assign(serialized, "closeButton", closeButton);
        Assign(serialized, "feedbackText", feedback);
        Assign(serialized, "roomScroll", roomScroll);
        AssignCards(serialized, cards);
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

        return "Premium room selector built in CatHome_UI.";
    }

    private static void BuildHeader(RectTransform panel, TMP_FontAsset font)
    {
        LowPolyPanelGraphic badge = CreatePanel("RoomsBadge", panel,
            PremiumUiStyle.CandyPink, 26f, 8f, false);
        SetCentered(badge.rectTransform, new Vector2(190f, 48f), new Vector2(-560f, 372f));
        TMP_Text badgeText = CreateText("BadgeText", badge.transform, font, 20f,
            Color.white, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform);
        badgeText.text = "CAT HOME";
        badgeText.fontStyle = FontStyles.Bold;

        TMP_Text title = CreateText("Title", panel, font, 44f, Ink,
            TextAlignmentOptions.Center);
        SetCentered(title.rectTransform, new Vector2(560f, 58f), new Vector2(0f, 372f));
        title.text = "MY ROOMS";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 2.2f;
        title.overflowMode = TextOverflowModes.Overflow;

        TMP_Text subtitle = CreateText("Subtitle", panel, font, 20f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(subtitle.rectTransform, new Vector2(980f, 40f), new Vector2(0f, 328f));
        subtitle.text = "PICK A HAPPY PLACE FOR YOUR CAT";
        subtitle.fontStyle = FontStyles.Bold;
        subtitle.characterSpacing = 1.1f;
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        subtitle.overflowMode = TextOverflowModes.Ellipsis;
    }

    private const float CardWidth = 690f;
    private const float CardHeight = 500f;
    private const float PreviewWidth = 658f;
    private const float PreviewHeight = 370f;

    private static ScrollRect BuildRoomScroll(RectTransform panel)
    {
        RectTransform scrollRoot = CreateRect("RoomScroll", panel);
        SetCentered(scrollRoot, new Vector2(1440f, 618f), new Vector2(0f, -22f));

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        StretchWithOffsets(viewport, 0f, 0f, -22f, 0f);
        GetOrAdd<RectMask2D>(viewport.gameObject);

        RectTransform grid = CreateRect("RoomGrid", viewport);
        grid.anchorMin = new Vector2(0f, 1f);
        grid.anchorMax = new Vector2(1f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = Vector2.zero;
        grid.sizeDelta = Vector2.zero;

        GridLayoutGroup layout = GetOrAdd<GridLayoutGroup>(grid.gameObject);
        layout.cellSize = new Vector2(CardWidth, CardHeight);
        layout.spacing = new Vector2(24f, 22f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 2;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(8, 8, 4, 8);
        layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        layout.startAxis = GridLayoutGroup.Axis.Horizontal;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(grid.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LowPolyPanelGraphic track = CreatePanel("VerticalScrollbar", scrollRoot,
            PremiumUiStyle.CandyCloud, 9f, 1.5f, true);
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(1f, 0f);
        trackRect.anchorMax = new Vector2(1f, 1f);
        trackRect.pivot = new Vector2(1f, 0.5f);
        trackRect.sizeDelta = new Vector2(18f, -12f);
        trackRect.anchoredPosition = new Vector2(-2f, 0f);

        LowPolyPanelGraphic handle = CreatePanel("Handle", track.transform,
            PremiumUiStyle.CandyLemon, 7f, 1.5f, true);
        StretchWithOffsets(handle.rectTransform, 3f, 3f, -3f, -3f);

        Scrollbar scrollbar = GetOrAdd<Scrollbar>(track.gameObject);
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.transition = Selectable.Transition.None;

        ScrollRect scroll = GetOrAdd<ScrollRect>(scrollRoot.gameObject);
        scroll.content = grid;
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.scrollSensitivity = 42f;
        scroll.verticalScrollbar = scrollbar;
        scroll.verticalScrollbarVisibility =
            ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        return scroll;
    }

    private static void GetRoomCardVisuals(
        string roomId,
        out string subtitle,
        out string previewPath,
        out Color accent,
        out string theme)
    {
        if (string.Equals(roomId, HomeRoomService.BathroomId, StringComparison.Ordinal))
        {
            subtitle = "SPLASH, GROOM & RELAX";
            previewPath = BathroomPreviewPath;
            accent = PremiumUiStyle.CandyAqua;
            theme = "bathroom";
            return;
        }

        if (string.Equals(roomId, HomeRoomService.KitchenId, StringComparison.Ordinal))
        {
            subtitle = "COOK, SHARE & PURR";
            previewPath = KitchenPreviewPath;
            accent = PremiumUiStyle.CandyPeach;
            theme = "kitchen";
            return;
        }

        if (string.Equals(roomId, HomeRoomService.BedroomId, StringComparison.Ordinal))
        {
            subtitle = "DREAM, REST & SNUGGLE";
            previewPath = BedroomPreviewPath;
            accent = PremiumUiStyle.CandyPink;
            theme = "bedroom";
            return;
        }

        if (string.Equals(roomId, HomeRoomService.GardenId, StringComparison.Ordinal))
        {
            subtitle = "SUN, BIRDS & GRASS";
            previewPath = GardenPreviewPath;
            accent = PremiumUiStyle.CandyMint;
            theme = "garden";
            return;
        }

        if (string.Equals(roomId, HomeRoomService.LivingRoomId, StringComparison.Ordinal))
        {
            subtitle = "COZY PLAY & CARE";
            previewPath = LivingPreviewPath;
            accent = PremiumUiStyle.CandyMint;
            theme = "living";
            return;
        }

        subtitle = "COMING SOON";
        previewPath = string.Empty;
        accent = PremiumUiStyle.CandyLemon;
        theme = "future";
    }

    private static CardParts BuildRoomCard(
        RectTransform parent,
        TMP_FontAsset font,
        string roomId,
        string titleValue,
        string subtitleValue,
        string previewPath,
        Color accent,
        string theme)
    {
        RectTransform cardRoot = CreateRect("RoomCard_" + roomId, parent);
        cardRoot.sizeDelta = new Vector2(CardWidth, CardHeight);
        CanvasGroup group = GetOrAdd<CanvasGroup>(cardRoot.gameObject);

        RectTransform cardDepth = CreateRect("CardDepth", cardRoot);
        PremiumUiStyle.SetCenteredShadowStretch(cardDepth, 6f);
        AddPanel(cardDepth.gameObject, new Color(accent.r * .67f, accent.g * .67f,
            accent.b * .67f, 1f), 28f, 8f, false);

        RectTransform visual = CreateRect("CardVisual", cardRoot);
        StretchWithOffsets(visual, 0f, 4f, 0f, 0f);
        LowPolyPanelGraphic face = AddPanel(visual.gameObject, accent, 28f, 10f, true);
        PremiumUiStyle.ConfigureAccentSurface(
            face, Color.Lerp(accent, Color.white, .1f), accent, 28f, 10f);

        Button button = GetOrAdd<Button>(cardRoot.gameObject);
        button.targetGraphic = face;
        button.transition = Selectable.Transition.ColorTint;

        LowPolyPanelGraphic previewWell = CreatePanel("PreviewWell", visual,
            PremiumUiStyle.CandyCloud, 22f, 6f, false);
        RectTransform previewRect = previewWell.rectTransform;
        previewRect.anchorMin = new Vector2(.5f, 1f);
        previewRect.anchorMax = new Vector2(.5f, 1f);
        previewRect.pivot = new Vector2(.5f, 1f);
        previewRect.sizeDelta = new Vector2(PreviewWidth, PreviewHeight);
        previewRect.anchoredPosition = new Vector2(0f, -12f);
        RectMask2D mask = GetOrAdd<RectMask2D>(previewWell.gameObject);
        mask.padding = new Vector4(6f, 6f, 6f, 6f);

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(previewPath);
        if (texture != null)
        {
            RawImage preview = CreateRawImage("RoomPhoto", previewWell.transform, texture);
            Stretch(preview.rectTransform);
            preview.uvRect = RoomPreviewFit.CoverUv(
                texture.width, texture.height, PreviewWidth, PreviewHeight);
        }
        else
        {
            BuildFallbackPreview(previewWell.transform, theme);
        }

        LowPolyPanelGraphic themePill = CreatePanel("ThemePill", previewWell.transform,
            theme == "bathroom" ? PremiumUiStyle.CandyAqua
                : theme == "kitchen" ? PremiumUiStyle.CandyLemon
                : theme == "bedroom" ? PremiumUiStyle.CandyGrape : PremiumUiStyle.CandyMint,
            13f, 4f, false);
        RectTransform themeRect = themePill.rectTransform;
        themeRect.anchorMin = themeRect.anchorMax = themeRect.pivot = new Vector2(0f, 0f);
        themeRect.sizeDelta = new Vector2(168f, 28f);
        themeRect.anchoredPosition = new Vector2(18f, 12f);
        TMP_Text themeLabel = CreateText("ThemeText", themePill.transform, font, 13f,
            Ink, TextAlignmentOptions.Center);
        Stretch(themeLabel.rectTransform);
        themeLabel.text = theme == "bathroom" ? "AQUA SPA"
            : theme == "kitchen" ? "SUNNY KITCHEN"
            : theme == "bedroom" ? "DREAMY REST" : "COZY HOME";
        themeLabel.fontStyle = FontStyles.Bold;
        themeLabel.overflowMode = TextOverflowModes.Ellipsis;

        LowPolyPanelGraphic infoWell = CreatePanel(
            "RoomInfoWell", visual, PremiumUiStyle.CandyCloud, 20f, 5f, false);
        RectTransform infoRect = infoWell.rectTransform;
        infoRect.anchorMin = new Vector2(.5f, 0f);
        infoRect.anchorMax = new Vector2(.5f, 0f);
        infoRect.pivot = new Vector2(.5f, 0f);
        infoRect.sizeDelta = new Vector2(658f, 100f);
        infoRect.anchoredPosition = new Vector2(0f, 10f);
        PremiumUiStyle.ConfigureAccentSurface(
            infoWell,
            theme == "bathroom"
                ? new Color32(224, 250, 246, 255)
                : theme == "kitchen" ? new Color32(255, 242, 207, 255)
                : theme == "bedroom" ? new Color32(244, 226, 255, 255)
                : new Color32(255, 244, 210, 255),
            theme == "bathroom"
                ? new Color32(202, 235, 255, 255)
                : theme == "kitchen" ? new Color32(210, 250, 226, 255)
                : theme == "bedroom" ? new Color32(255, 214, 236, 255)
                : new Color32(255, 222, 234, 255),
            20f,
            5f);

        TMP_Text title = CreateText("RoomTitle", infoWell.transform, font, 24f, Ink,
            TextAlignmentOptions.Left);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.sizeDelta = new Vector2(-176f, 36f);
        titleRect.anchoredPosition = new Vector2(18f, -10f);
        title.text = titleValue;
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 1.2f;
        title.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text status = CreateText("RoomStatus", infoWell.transform, font, 16f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Left);
        RectTransform statusRect = status.rectTransform;
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0f);
        statusRect.pivot = new Vector2(0f, 0f);
        statusRect.sizeDelta = new Vector2(-176f, 28f);
        statusRect.anchoredPosition = new Vector2(18f, 12f);
        status.text = subtitleValue;
        status.fontStyle = FontStyles.Bold;
        status.overflowMode = TextOverflowModes.Ellipsis;

        LowPolyPanelGraphic action = CreatePanel("ActionFace", infoWell.transform,
            theme == "bathroom" ? PremiumUiStyle.CandyPink
                : theme == "kitchen" ? PremiumUiStyle.CandyBerry
                : theme == "bedroom" ? PremiumUiStyle.CandyGrape : PremiumUiStyle.CandyAqua,
            18f, 6f, false);
        RectTransform actionRect = action.rectTransform;
        actionRect.anchorMin = actionRect.anchorMax = actionRect.pivot = new Vector2(1f, .5f);
        actionRect.sizeDelta = new Vector2(148f, 52f);
        actionRect.anchoredPosition = new Vector2(-18f, 0f);
        TMP_Text actionText = CreateText("ActionText", action.transform, font, 16f,
            Color.white, TextAlignmentOptions.Center);
        Stretch(actionText.rectTransform);
        actionText.text = "VISIT";
        actionText.fontStyle = FontStyles.Bold;
        actionText.characterSpacing = 1.1f;
        actionText.overflowMode = TextOverflowModes.Ellipsis;

        GameObject lockBadge = BuildCornerBadge(previewWell.transform, font, "LockBadge",
            "LOCKED", PremiumUiStyle.CandyPeach, new Vector2(-18f, -18f));
        GameObject currentBadge = BuildCornerBadge(previewWell.transform, font,
            "CurrentBadge", "CURRENT", PremiumUiStyle.CandyLemon, new Vector2(-18f, -18f));
        lockBadge.SetActive(false);
        currentBadge.SetActive(false);

        return new CardParts(roomId, button, group, face, title, status,
            actionText, lockBadge, currentBadge);
    }

    private static GameObject BuildCornerBadge(Transform parent, TMP_FontAsset font,
        string name, string value, Color color, Vector2 anchoredPosition)
    {
        LowPolyPanelGraphic panel = CreatePanel(name, parent, color, 15f, 4f, false);
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(132f, 34f);
        rect.anchoredPosition = anchoredPosition;
        TMP_Text text = CreateText("BadgeText", panel.transform, font, 14f, Ink,
            TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.text = value;
        text.fontStyle = FontStyles.Bold;
        return panel.gameObject;
    }

    private static Button BuildCloseButton(RectTransform panel, TMP_FontAsset font)
    {
        RectTransform root = CreateRect("CloseButton", panel);
        SetCentered(root, new Vector2(62f, 62f), new Vector2(710f, 372f));
        RectTransform visual = CreateRect("CloseVisual", root);
        Stretch(visual);
        LowPolyPanelGraphic surface = AddPanel(visual.gameObject,
            PremiumUiStyle.CandyPink, 31f, 8f, true);
        PremiumUiStyle.ConfigureAccentSurface(surface,
            PremiumUiStyle.CoralLift, PremiumUiStyle.CandyPink, 31f, 8f);
        TMP_Text label = CreateText("CloseLabel", visual, font, 33f,
            Color.white, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        label.text = "×";
        label.fontStyle = FontStyles.Bold;
        Button button = GetOrAdd<Button>(root.gameObject);
        button.targetGraphic = surface;
        return button;
    }

    private static void BuildFallbackPreview(Transform parent, string theme)
    {
        bool bathroom = theme == "bathroom";
        bool kitchen = theme == "kitchen";
        bool bedroom = theme == "bedroom";
        Image sky = CreateImage("FallbackBackdrop", parent,
            bathroom ? new Color32(183, 250, 239, 255)
            : kitchen ? new Color32(255, 239, 174, 255)
            : bedroom ? new Color32(236, 214, 255, 255)
            : new Color32(255, 220, 174, 255), false);
        Stretch(sky.rectTransform);
        Image floor = CreateImage("FallbackFloor", parent,
            bathroom ? new Color32(98, 221, 211, 255)
            : kitchen ? new Color32(255, 194, 95, 255)
            : bedroom ? new Color32(255, 176, 210, 255)
            : new Color32(247, 150, 131, 255), false);
        floor.rectTransform.anchorMin = Vector2.zero;
        floor.rectTransform.anchorMax = new Vector2(1f, .32f);
        floor.rectTransform.offsetMin = floor.rectTransform.offsetMax = Vector2.zero;
        if (bathroom)
        {
            LowPolyPanelGraphic tub = CreatePanel("TubSilhouette", parent,
                PremiumUiStyle.CandyCloud, 25f, 6f, false);
            SetCentered(tub.rectTransform, new Vector2(290f, 86f), new Vector2(40f, -35f));
        }
        else if (kitchen)
        {
            LowPolyPanelGraphic island = CreatePanel("IslandSilhouette", parent,
                PremiumUiStyle.CandyAqua, 20f, 6f, false);
            SetCentered(island.rectTransform, new Vector2(230f, 84f), new Vector2(-32f, -42f));
            LowPolyPanelGraphic fridge = CreatePanel("FridgeSilhouette", parent,
                PremiumUiStyle.CandyMint, 18f, 6f, false);
            SetCentered(fridge.rectTransform, new Vector2(82f, 164f), new Vector2(145f, -2f));
        }
        else if (bedroom)
        {
            LowPolyPanelGraphic bed = CreatePanel("BedSilhouette", parent,
                PremiumUiStyle.CandyGrape, 22f, 7f, false);
            SetCentered(bed.rectTransform, new Vector2(270f, 92f), new Vector2(8f, -30f));
        }
        else
        {
            LowPolyPanelGraphic sofa = CreatePanel("SofaSilhouette", parent,
                PremiumUiStyle.CandyPink, 22f, 7f, false);
            SetCentered(sofa.rectTransform, new Vector2(290f, 108f), new Vector2(10f, -28f));
        }
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

    private static RawImage CreateRawImage(string name, Transform parent, Texture texture)
    {
        RectTransform rect = CreateRect(name, parent);
        GetOrAdd<CanvasRenderer>(rect.gameObject);
        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void StretchWithOffsets(RectTransform rect, float left, float bottom,
        float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
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
            throw new InvalidOperationException("Missing RoomSelectorPanel field '" + name + "'.");
        property.objectReferenceValue = value;
    }

    private static void AssignCards(SerializedObject serialized, List<CardParts> cards)
    {
        SerializedProperty property = serialized.FindProperty("cards");
        property.arraySize = cards.Count;
        for (int i = 0; i < cards.Count; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("roomId").stringValue = cards[i].RoomId;
            element.FindPropertyRelative("button").objectReferenceValue = cards[i].Button;
            element.FindPropertyRelative("group").objectReferenceValue = cards[i].Group;
            element.FindPropertyRelative("face").objectReferenceValue = cards[i].Face;
            element.FindPropertyRelative("titleText").objectReferenceValue = cards[i].Title;
            element.FindPropertyRelative("statusText").objectReferenceValue = cards[i].Status;
            element.FindPropertyRelative("actionText").objectReferenceValue = cards[i].Action;
            element.FindPropertyRelative("lockBadge").objectReferenceValue = cards[i].Lock;
            element.FindPropertyRelative("currentBadge").objectReferenceValue = cards[i].Current;
        }
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

    private static void ConfigurePreviewImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        bool changed = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            changed = true;
        }
        if (!importer.sRGBTexture) { importer.sRGBTexture = true; changed = true; }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; changed = true; }
        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }
        if (changed)
            importer.SaveAndReimport();
    }

    private static void Validate(GameObject root)
    {
        RoomSelectorPanel[] controllers = root.GetComponents<RoomSelectorPanel>();
        if (controllers.Length != 1)
            throw new InvalidOperationException("Room selector needs exactly one controller.");
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.targetGraphic == null)
                throw new InvalidOperationException("Room selector button has no visual: " + button.name);
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

    private readonly struct CardParts
    {
        public CardParts(string roomId, Button button, CanvasGroup group,
            LowPolyPanelGraphic face, TMP_Text title, TMP_Text status, TMP_Text action,
            GameObject locked, GameObject current)
        {
            RoomId = roomId;
            Button = button;
            Group = group;
            Face = face;
            Title = title;
            Status = status;
            Action = action;
            Lock = locked;
            Current = current;
        }

        public string RoomId { get; }
        public Button Button { get; }
        public CanvasGroup Group { get; }
        public LowPolyPanelGraphic Face { get; }
        public TMP_Text Title { get; }
        public TMP_Text Status { get; }
        public TMP_Text Action { get; }
        public GameObject Lock { get; }
        public GameObject Current { get; }
    }
}
