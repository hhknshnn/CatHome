using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Idempotent Editor builder for the CP1 main-menu shell. It owns exactly one
/// scene root — "MainPanelCanvas" — and rebuilds only that subtree. Everything
/// else in the scene (HUD Canvas, joystick, ActionButton, need indicators,
/// popups, tutorial, CatSpeechBubble, ProgressionDebugOverlay, …) is left
/// untouched.
///
/// The old right-side drawer (heavy scrim, ~44% slide-in panel) is replaced by a
/// compact low-poly list that drops down from the top-right menu button:
///
///   MainPanelCanvas (Canvas overlay, sortingOrder 100 + MainPanelController)
///     Scrim               near-invisible full-screen outside-tap catcher
///     SafeArea            (SafeAreaRect)
///       LightButton       top-right low-poly button, left of Shop
///       ShopButton        top-right low-poly button, left of the hamburger
///       MenuButton        top-right low-poly button (+ BadgeAnchor)
///       MenuList          drop-down list, pivot top-right, below the button
///         Row_QUESTS      independent low-poly card
///         Row_REWARDS
///         Row_NOTIFICATIONS
///         Row_SETTINGS
///
/// Running the menu item again reuses the existing "MainPanelCanvas" root: its
/// children are cleared and rebuilt, so no duplicate Canvas, EventSystem,
/// button, scrim or rows are produced.
/// </summary>
public static class MainPanelBuilder
{
    private static bool suppressDialogs;
    private const string RootName = "MainPanelCanvas";
    private const string PrefabFolder = "Assets/UI";
    private const string PrefabPath = PrefabFolder + "/MainPanel.prefab";
    private const int PanelSortingOrder = 100;

    // List geometry (canvas units, 1920x1080 reference). Width is finalized at
    // runtime by the controller (fraction + min/max clamp); this is the fallback.
    // The old drawer used 440-wide, 112-tall rows; the compact list trims the
    // width ~22% and shrinks each row into a slim ~46-unit list item.
    private const float FallbackListWidth = 390f;
    private const float RowHeight = 68f;
    private const float RowSpacing = 0f;
    // Gap between the icon and the title inside each card's centered content group.
    private const float IconLabelGap = 18f;
    // Fixed footprint of the procedural section icon (authored in a centered space).
    private const float IconSize = 46f;

    // Top-bar buttons (hamburger + shop). Both share one size, one top offset and
    // one right margin so the shop button reads as a twin of the hamburger.
    private const float TopButtonWidth = 80f;
    private const float TopButtonHeight = 68f;
    private const float TopButtonTop = -22f;
    // Right margin of the hamburger. Nudged in from the old 30 so the button sits
    // a little further from the screen corner and is comfortable to tap on mobile.
    private const float MenuButtonRightMargin = 44f;
    // Small, equal gap between the shop button and the hamburger.
    private const float TopButtonGap = 14f;
    // Shop button is placed immediately left of the hamburger, right-aligned to it.
    private const float ShopButtonRightMargin =
        MenuButtonRightMargin + TopButtonWidth + TopButtonGap;
    private const float LightButtonRightMargin =
        ShopButtonRightMargin + TopButtonWidth + TopButtonGap;

    private static readonly Color ScrimColor = new Color32(19, 30, 55, 215);
    private static readonly Color FrameOrange = new Color32(255, 105, 151, 255);
    private static readonly Color FrameShadow = PremiumUiStyle.Shadow;
    private static readonly Color CreamBar = PremiumUiStyle.Ivory;
    private static readonly Color DarkBrownText = PremiumUiStyle.Ink;

    // Icon palette (procedural low-poly, no sprites).
    private static readonly Color IconInk = PremiumUiStyle.Night;
    private static readonly Color IconAccent = PremiumUiStyle.Coral;
    private static readonly Color IconLight = PremiumUiStyle.Ivory;
    private static readonly Color BadgeColor = new Color32(219, 74, 46, 255);    // red-orange badge
    private static readonly Color BadgeText = new Color32(255, 244, 232, 255);
    private static readonly Color IconShadowColor = new Color32(84, 42, 53, 90); // soft transparent shadow

    // Alternating candy cards keep the menu playful and immediately scannable.
    private static readonly Color[] RowCreams =
    {
        new Color32(217, 252, 245, 255),
        new Color32(235, 222, 255, 255),
        new Color32(255, 229, 212, 255),
        new Color32(211, 240, 255, 255)
    };

    private static readonly string[] RowIds = { "HOME STORE", "QUESTS", "CAT JOURNAL", "ROOMS", "SETTINGS" };

    [MenuItem("Tools/Cat Home/Build Main Panel (CP1)")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            string message = "No EventSystem was found in the open scene. Open GameScene and " +
                "make sure its EventSystem is present before building the main panel.";
            if (!Application.isBatchMode && !suppressDialogs)
                EditorUtility.DisplayDialog("Main Panel (CP1)", message, "OK");
            else
                Debug.LogWarning("Main panel rebuild skipped: " + message);
            return;
        }

        TMP_FontAsset font = FindFont();
        CatMovement catMovement = Object.FindAnyObjectByType<CatMovement>();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Main Panel (CP1)");

        GameObject existing = FindSceneRoot(scene, RootName);
        GameObject root = existing != null ? PrepareExistingRoot(existing) : CreateRoot(scene);

        // ----- Canvas plumbing -----
        Canvas canvas = GetOrAdd<Canvas>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = false;
        canvas.sortingOrder = PanelSortingOrder;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(root);
        MainPanelController controller = GetOrAdd<MainPanelController>(root);

        // ----- Scrim: near-invisible, full-canvas, catches outside taps only -----
        GameObject scrim = CreateRect("Scrim", root.transform);
        Stretch(scrim.GetComponent<RectTransform>());
        Image scrimImage = GetOrAdd<Image>(scrim);
        EnsureCanvasRenderer(scrim);
        scrimImage.color = ScrimColor;
        scrimImage.raycastTarget = true;
        CanvasGroup scrimGroup = GetOrAdd<CanvasGroup>(scrim);
        scrimGroup.alpha = 0f;
        scrimGroup.interactable = false;
        scrimGroup.blocksRaycasts = false;
        Button scrimButton = GetOrAdd<Button>(scrim);
        scrimButton.transition = Selectable.Transition.None;
        scrimButton.targetGraphic = scrimImage;

        // ----- Safe-area child (menu button + list) -----
        GameObject safeArea = CreateRect("SafeArea", root.transform);
        RectTransform safeAreaRect = safeArea.GetComponent<RectTransform>();
        Stretch(safeAreaRect);
        safeArea.AddComponent<SafeAreaRect>();

        // ----- Menu button (top-right, closed state) -----
        MenuButtonParts menu = CreateMenuButton(safeArea.transform);

        // ----- Shop button (immediately left of the hamburger) -----
        ShopButtonParts shop = CreateShopButton(safeArea.transform);

        // ----- Light level button (immediately left of Shop) -----
        LightButtonParts light = CreateLightButton(safeArea.transform);

        // ----- Compact drop-down list (below the button) -----
        ListParts list = CreateMenuList(safeArea.transform, font, out List<RowData> rowData);

        // ----- Wire the controller -----
        SerializedObject serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", catMovement);
        Assign(serialized, "scrimGroup", scrimGroup);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "menuButtonGroup", menu.Group);
        Assign(serialized, "menuButton", menu.Button);
        Assign(serialized, "shopButtonGroup", shop.Group);
        Assign(serialized, "shopButton", shop.Button);
        Assign(serialized, "lightButtonGroup", light.Group);
        Assign(serialized, "lightButton", light.Button);
        Assign(serialized, "lightButtonFace", light.FaceGraphic);
        Assign(serialized, "safeArea", safeAreaRect);
        Assign(serialized, "menuList", list.List);
        Assign(serialized, "menuListGroup", list.Group);
        serialized.FindProperty("widthFraction").floatValue = 0.205f;
        serialized.FindProperty("minWidth").floatValue = 390f;
        serialized.FindProperty("maxWidth").floatValue = 430f;
        AssignRows(serialized, rowData);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PremiumUiFactory.PolishHierarchy(root.transform, font);
        ValidateGraphics(root);
        EnsureFolder();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;

        string fontWarning = font == null
            ? "\n\nFredoka TMP font was not found; TMP's fallback font is used."
            : string.Empty;
        string catWarning = catMovement == null
            ? "\n\nNo CatMovement was found; the controller will resolve it at runtime."
            : string.Empty;
        if (!suppressDialogs)
            EditorUtility.DisplayDialog(
                "Main Panel (CP1)",
                (existing == null ? "Compact main menu created" : "Existing main menu rebuilt") +
                " as a separate canvas (sortingOrder 100) and prefab saved at " +
                PrefabPath + "." + fontWarning + catWarning,
                "OK"
            );
    }

    public static void BuildSilently()
    {
        suppressDialogs = true;
        try { Build(); }
        finally { suppressDialogs = false; }
    }

    /// <summary>Headless entry point used to regenerate the prefab and its GameScene instance.</summary>
    public static void BuildGameSceneBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        Build();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    // ----- Menu button -----

    private static MenuButtonParts CreateMenuButton(Transform parent)
    {
        // Right-aligned to the same margin as the list so their right edges line up.
        TopButtonParts parts = CreateTopButtonBase(parent, "MenuButton", -MenuButtonRightMargin);
        Transform face = parts.Face.transform;

        // Three cream bars = the classic "menu" glyph.
        CreateBar(face, "Bar1", new Vector2(0.26f, 0.60f), new Vector2(0.74f, 0.71f));
        CreateBar(face, "Bar2", new Vector2(0.26f, 0.445f), new Vector2(0.74f, 0.555f));
        CreateBar(face, "Bar3", new Vector2(0.26f, 0.29f), new Vector2(0.74f, 0.40f));

        // Anchor point for a future total-notification badge (empty in CP1).
        GameObject badgeAnchor = CreateRect("BadgeAnchor", parts.Root.transform);
        RectTransform badgeRect = badgeAnchor.GetComponent<RectTransform>();
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.sizeDelta = new Vector2(24f, 24f);
        badgeRect.anchoredPosition = new Vector2(-2f, -2f);

        Button button = MakeButton(parts.Root, parts.FaceGraphic, parts.Face.GetComponent<RectTransform>());
        return new MenuButtonParts(button, parts.Group);
    }

    // ----- Shop button (twin of the hamburger, placed to its left) -----

    private static ShopButtonParts CreateShopButton(Transform parent)
    {
        TopButtonParts parts = CreateTopButtonBase(parent, "ShopButton", -ShopButtonRightMargin);
        Transform face = parts.Face.transform;

        // Low-poly shopping bag glyph, cream-on-orange to match the hamburger's
        // cream bars: a rounded cream body, a dark-brown opening seam, and a
        // squared cream carry handle arching above it.
        IconOctagon(face, "BagBody", CreamBar, 30f, 26f, 0f, -5f, 7f, 0f);
        IconRect(face, "BagSeam", DarkBrownText, 30f, 2.5f, 0f, 6f);
        IconRect(face, "HandleLeft", CreamBar, 3f, 11f, -7f, 12f);
        IconRect(face, "HandleRight", CreamBar, 3f, 11f, 7f, 12f);
        IconRect(face, "HandleTop", CreamBar, 17f, 3f, 0f, 16f);

        Button button = MakeButton(parts.Root, parts.FaceGraphic, parts.Face.GetComponent<RectTransform>());
        return new ShopButtonParts(button, parts.Group);
    }

    // ----- Player brightness button (twin of Shop, placed to its left) -----

    private static LightButtonParts CreateLightButton(Transform parent)
    {
        TopButtonParts parts = CreateTopButtonBase(parent, "LightButton", -LightButtonRightMargin);
        Transform face = parts.Face.transform;

        // Procedural cream bulb: octagonal globe, narrow neck and two base bars.
        IconOctagon(face, "Bulb", CreamBar, 25f, 27f, 0f, 6f, 10f, 0f);
        IconRect(face, "BulbNeck", CreamBar, 10f, 8f, 0f, -9f);
        IconRect(face, "BulbBase1", CreamBar, 14f, 3f, 0f, -14f);
        IconRect(face, "BulbBase2", CreamBar, 10f, 3f, 0f, -18f);

        Button button = MakeButton(parts.Root, parts.FaceGraphic, parts.Face.GetComponent<RectTransform>());
        return new LightButtonParts(button, parts.Group, parts.FaceGraphic);
    }

    // Shared frame for a top-bar button: right-anchored root at the standard size,
    // a hidden CanvasGroup and orange low-poly face. Callers add their own glyph
    // and the Button afterwards.
    private static TopButtonParts CreateTopButtonBase(Transform parent, string name, float anchoredX)
    {
        GameObject buttonRoot = CreateRect(name, parent);
        RectTransform rect = buttonRoot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(TopButtonWidth, TopButtonHeight);
        rect.anchoredPosition = new Vector2(anchoredX, TopButtonTop);

        CanvasGroup group = GetOrAdd<CanvasGroup>(buttonRoot);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        GameObject face = CreateRect("Face", buttonRoot.transform);
        Stretch(face.GetComponent<RectTransform>());
        EnsureCanvasRenderer(face);
        LowPolyPanelGraphic faceGraphic = face.AddComponent<LowPolyPanelGraphic>();
        faceGraphic.color = FrameOrange;
        faceGraphic.raycastTarget = true;
        PremiumUiStyle.ConfigureAccentSurface(
            faceGraphic,
            new Color32(255, 132, 178, 255),
            FrameOrange,
            34f,
            5f);

        LowPolyPanelGraphic rim = CreatePanel("Rim", face.transform, PremiumUiStyle.Champagne, 24f, 2f, false);
        StretchWithOffsets(rim.rectTransform, 4f, 4f, -4f, -4f);
        LowPolyPanelGraphic inner = CreatePanel("Inner", face.transform, new Color32(105, 218, 242, 255), 21f, 4f, false);
        StretchWithOffsets(inner.rectTransform, 7f, 7f, -7f, -7f);

        return new TopButtonParts(buttonRoot, group, face, faceGraphic);
    }

    private static void CreateBar(Transform parent, string name, Vector2 min, Vector2 max)
    {
        Image bar = CreateImage(name, parent, CreamBar, false);
        SetAnchors(bar.rectTransform, min, max);
    }

    // ----- Compact list -----

    private static ListParts CreateMenuList(Transform parent, TMP_FontAsset font, out List<RowData> rowData)
    {
        GameObject listRoot = CreateRect("MenuList", parent);
        RectTransform rect = listRoot.GetComponent<RectTransform>();
        // Top-right anchored; grows leftward (width) and downward (height) from
        // just below the menu button.
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(FallbackListWidth, RowHeight * RowIds.Length + 28f);
        // Right-aligned to the same margin as the hamburger so their right edges
        // stay lined up, dropping in just below it (button bottom ~ -90; small gap).
        rect.anchoredPosition = new Vector2(-MenuButtonRightMargin, -98f);

        CanvasGroup group = GetOrAdd<CanvasGroup>(listRoot);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        LowPolyPanelGraphic listShadow = CreatePanel("ListShadow", listRoot.transform, FrameShadow, 18f, 0f, false);
        IgnoreLayout(listShadow.rectTransform);
        PremiumUiStyle.SetCenteredShadowStretch(listShadow.rectTransform, 4f);
        LowPolyPanelGraphic listAmbientShadow = CreatePanel("ListAmbientShadow", listRoot.transform, PremiumUiStyle.SoftShadow, 22f, 0f, false);
        IgnoreLayout(listAmbientShadow.rectTransform);
        PremiumUiStyle.SetCenteredShadowStretch(listAmbientShadow.rectTransform, 6f);
        LowPolyPanelGraphic listRim = CreatePanel("ListRim", listRoot.transform, PremiumUiStyle.Champagne, 18f, 1.5f, false);
        IgnoreLayout(listRim.rectTransform);
        Stretch(listRim.rectTransform);
        LowPolyPanelGraphic listFace = CreatePanel("ListFace", listRoot.transform, new Color32(247, 252, 255, 255), 15f, 4f, false);
        IgnoreLayout(listFace.rectTransform);
        StretchWithOffsets(listFace.rectTransform, 3f, 3f, -3f, -3f);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(listRoot);
        layout.spacing = RowSpacing;
        layout.padding = new RectOffset(12, 12, 14, 14);
        layout.childAlignment = TextAnchor.UpperRight;
        // Each row is sized to its own content, not stretched to the list width:
        // childControlWidth lets the layout honour every row's LayoutElement
        // preferredWidth (set per-title at runtime by MainPanelController), while
        // childForceExpandWidth stays off so rows keep their measured width and the
        // UpperRight alignment tucks each card against the shared right margin.
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(listRoot);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        rowData = new List<RowData>(RowIds.Length);
        for (int i = 0; i < RowIds.Length; i++)
            rowData.Add(CreateRow(listRoot.transform, RowIds[i], i, font));

        return new ListParts(rect, group);
    }

    private static RowData CreateRow(Transform parent, string id, int index, TMP_FontAsset font)
    {
        GameObject rowRoot = CreateRect("Row_" + id, parent);
        RectTransform rowRect = rowRoot.GetComponent<RectTransform>();
        rowRect.pivot = new Vector2(1f, 1f);

        LayoutElement layoutElement = GetOrAdd<LayoutElement>(rowRoot);
        layoutElement.minHeight = RowHeight;
        layoutElement.preferredHeight = RowHeight;

        CanvasGroup group = GetOrAdd<CanvasGroup>(rowRoot);
        group.alpha = 0f;

        // The menu is one tailored dropdown surface; rows are quiet navy bands
        // divided by champagne hairlines rather than five unrelated buttons.
        LowPolyPanelGraphic frame = CreatePanel("Frame", rowRoot.transform, Color.clear, 8f, 1f, true);
        Stretch(frame.rectTransform);
        Color cream = RowCreams[index % RowCreams.Length];
        LowPolyPanelGraphic face = CreatePanel("Face", rowRoot.transform, cream, 7f, 1f, false);
        StretchWithOffsets(face.rectTransform, 2f, 2f, -2f, -2f);
        if (index < RowIds.Length - 1)
        {
            Image divider = CreateImage("Divider", rowRoot.transform, new Color(PremiumUiStyle.Champagne.r, PremiumUiStyle.Champagne.g, PremiumUiStyle.Champagne.b, 0.25f), false);
            RectTransform dividerRect = divider.rectTransform;
            dividerRect.anchorMin = new Vector2(0f, 0f);
            dividerRect.anchorMax = new Vector2(1f, 0f);
            dividerRect.pivot = new Vector2(0.5f, 0f);
            dividerRect.sizeDelta = new Vector2(-18f, 1f);
            dividerRect.anchoredPosition = Vector2.zero;
        }

        // Icon + title live in one horizontal group that is centered inside the
        // card. Because every card is later widened to the longest title's width,
        // this keeps each row's icon+title block visually centred with equal
        // breathing room on both sides instead of hugging the left edge.
        RectTransform content = CreateRowContent(rowRoot.transform);

        // Procedural, section-specific low-poly icon. Small and meaningful — no big
        // empty octagon. Now the leading element of the centered content group.
        BuildRowIcon(content, id);

        // Section label: compact, bold, left-aligned inside a fixed title area so
        // every card's title begins at the same x, right after the icon. The area
        // width (the longest title) is driven at runtime by MainPanelController via
        // labelLayout; left alignment keeps shorter titles anchored to that shared
        // start x instead of re-centring per row.
        TMP_Text label = CreateText("Label", content, font, 21f, DarkBrownText, TextAlignmentOptions.MidlineLeft);
        label.text = id;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 0.5f;

        // Lets the controller pin the label to the shared title-area width (longest
        // title) so all four icons and titles align on the same x within their
        // equally centred content groups.
        LayoutElement labelLayout = GetOrAdd<LayoutElement>(label.gameObject);
        labelLayout.flexibleWidth = 0f;

        // Optional notification badge (hidden by default; UI scaffolding only).
        BadgeParts badge = CreateRowBadge(rowRoot.transform, font);

        Button button = MakeButton(rowRoot, face, face.rectTransform);
        // The label, its LayoutElement and the row LayoutElement are handed to the
        // controller so it can measure each title's preferredWidth at runtime and
        // drive both this row's width and the shared title-area width from it — no
        // fixed per-title widths, new titles size themselves.
        return new RowData(id, button, group, badge.Root, badge.Text, label, layoutElement, labelLayout);
    }

    // ----- Centered content group (icon + title) -----

    private static RectTransform CreateRowContent(Transform parent)
    {
        GameObject contentRoot = CreateRect("Content", parent);
        RectTransform rect = contentRoot.GetComponent<RectTransform>();
        // Centered on both axes inside the card; the ContentSizeFitter shrinks the
        // group to exactly icon + gap + title so the horizontal centering is tight.
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(contentRoot);
        hlg.spacing = IconLabelGap;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(contentRoot);
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    // ----- Procedural section icons (no sprites) -----

    private static void BuildRowIcon(Transform parent, string id)
    {
        // Icon container: a fixed 38x38 square, its shapes authored in a centered
        // [-19, 19] local space. Its position is now handled by the parent
        // horizontal group; a LayoutElement reserves the fixed 38x38 footprint.
        GameObject iconRoot = CreateRect("Icon_" + id, parent);
        RectTransform iconRect = iconRoot.GetComponent<RectTransform>();
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(IconSize, IconSize);

        LayoutElement iconLayout = GetOrAdd<LayoutElement>(iconRoot);
        iconLayout.minWidth = IconSize;
        iconLayout.preferredWidth = IconSize;
        iconLayout.minHeight = IconSize;
        iconLayout.preferredHeight = IconSize;
        iconLayout.flexibleWidth = 0f;
        iconLayout.flexibleHeight = 0f;

        Transform t = iconRoot.transform;
        switch (id)
        {
            case "HOME STORE": BuildRewardsIcon(t); break;
            case "QUESTS": BuildQuestsIcon(t); break;
            case "REWARDS": BuildRewardsIcon(t); break;
            case "CAT JOURNAL": BuildNotificationsIcon(t); break;
            case "INVENTORY": BuildRewardsIcon(t); break;
            case "ROOMS": BuildRoomsIcon(t); break;
            case "NOTIFICATIONS": BuildNotificationsIcon(t); break;
            case "SETTINGS": BuildSettingsIcon(t); break;
        }
    }

    // Shared orange low-poly tile every icon draws its glyph on top of, so each
    // row reads as a distinct badge: orange ground, cream + dark-brown details.
    private static void IconBase(Transform t)
    {
        IconOctagon(t, "Base", IconAccent, 36f, 36f, 0f, 0f, 9f, 0f);
    }

    // Checklist / task sheet: cream paper, dark-brown clip, checkboxes and lines.
    private static void BuildQuestsIcon(Transform t)
    {
        IconBase(t);
        IconRect(t, "Sheet", IconLight, 24f, 28f, 0f, -1f);
        IconRect(t, "Clip", IconInk, 10f, 4f, 0f, 12.5f);
        for (int i = 0; i < 3; i++)
        {
            float y = 6f - i * 7f;
            IconRect(t, "Check" + i, IconInk, 5f, 5f, -6f, y);
            IconRect(t, "Line" + i, IconInk, 10f, 2.6f, 4.5f, y);
        }
    }

    // Gift box: cream box + lid, dark-brown ribbon and bow.
    private static void BuildRewardsIcon(Transform t)
    {
        IconBase(t);
        IconRect(t, "Box", IconLight, 24f, 16f, 0f, -6f);
        IconRect(t, "Lid", IconLight, 28f, 6f, 0f, 4f);
        IconRect(t, "Seam", IconInk, 28f, 1.6f, 0f, 1f);
        IconRect(t, "RibbonV", IconInk, 4f, 28f, 0f, -2f);
        IconRect(t, "BowL", IconInk, 7f, 7f, -5f, 11f, 45f);
        IconRect(t, "BowR", IconInk, 7f, 7f, 5f, 11f, 45f);
    }

    // Small home silhouette with two bright doorways: unlike the shop bag, this
    // reads as movement between rooms rather than a purchase.
    private static void BuildRoomsIcon(Transform t)
    {
        IconBase(t);
        IconRect(t, "House", IconLight, 25f, 20f, 0f, -5f);
        IconRect(t, "RoofLeft", IconLight, 20f, 6f, -6f, 7f, 36f);
        IconRect(t, "RoofRight", IconLight, 20f, 6f, 6f, 7f, -36f);
        IconRect(t, "DoorA", PremiumUiStyle.Teal, 6f, 11f, -5f, -9f);
        IconRect(t, "DoorB", PremiumUiStyle.Coral, 6f, 11f, 5f, -9f);
    }

    // Bell: cream dome + rim, dark-brown knob and clapper.
    private static void BuildNotificationsIcon(Transform t)
    {
        IconBase(t);
        IconRect(t, "Knob", IconInk, 5f, 4f, 0f, 13f);
        IconOctagon(t, "Dome", IconLight, 22f, 20f, 0f, 0f, 9f, 0f);
        IconRect(t, "Rim", IconLight, 26f, 4f, 0f, -10f);
        IconRect(t, "Clapper", IconInk, 5f, 5f, 0f, -14f);
    }

    // Gear: cream body + teeth around an orange ground, dark-brown hub.
    private static void BuildSettingsIcon(Transform t)
    {
        IconBase(t);
        IconRect(t, "ToothN", IconLight, 8f, 8f, 0f, 13f);
        IconRect(t, "ToothS", IconLight, 8f, 8f, 0f, -13f);
        IconRect(t, "ToothE", IconLight, 8f, 8f, 13f, 0f);
        IconRect(t, "ToothW", IconLight, 8f, 8f, -13f, 0f);
        IconRect(t, "ToothNE", IconLight, 7f, 7f, 9.5f, 9.5f, 45f);
        IconRect(t, "ToothNW", IconLight, 7f, 7f, -9.5f, 9.5f, 45f);
        IconRect(t, "ToothSE", IconLight, 7f, 7f, 9.5f, -9.5f, 45f);
        IconRect(t, "ToothSW", IconLight, 7f, 7f, -9.5f, -9.5f, 45f);
        IconOctagon(t, "GearBody", IconLight, 24f, 24f, 0f, 0f, 10f, 0f);
        IconOctagon(t, "Hub", IconInk, 9f, 9f, 0f, 0f, 4f, 0f);
    }

    private static void IconRect(Transform parent, string name, Color color, float w, float h, float x, float y, float rotation = 0f)
    {
        Image image = CreateImage(name, parent, color, false);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);
        if (!Mathf.Approximately(rotation, 0f))
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    private static void IconOctagon(Transform parent, string name, Color color, float w, float h, float x, float y, float cut, float bevel)
    {
        LowPolyPanelGraphic panel = CreatePanel(name, parent, color, cut, bevel, false);
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);
    }

    // ----- Notification badge (hidden by default) -----

    private static BadgeParts CreateRowBadge(Transform parent, TMP_FontAsset font)
    {
        GameObject badgeRoot = CreateRect("Badge", parent);
        RectTransform badgeRect = badgeRoot.GetComponent<RectTransform>();
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 0.5f);
        badgeRect.sizeDelta = new Vector2(26f, 26f);
        badgeRect.anchoredPosition = new Vector2(-12f, -8f);

        // Round-ish red/orange low-poly disc.
        LowPolyPanelGraphic disc = CreatePanel("Disc", badgeRoot.transform, BadgeColor, 12f, 0f, false);
        Stretch(disc.rectTransform);

        TMP_Text count = CreateText("Count", badgeRoot.transform, font, 16f, BadgeText, TextAlignmentOptions.Center);
        count.text = "1";
        count.fontStyle = FontStyles.Bold;
        Stretch(count.rectTransform);

        // Hidden until a future feature calls MainPanelController.SetRowBadge.
        badgeRoot.SetActive(false);
        return new BadgeParts(badgeRoot, count);
    }

    // ----- Button helper -----

    private static Button MakeButton(GameObject buttonRoot, Graphic targetGraphic, RectTransform faceRect)
    {
        Button button = GetOrAdd<Button>(buttonRoot);
        button.targetGraphic = targetGraphic;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.92f, 0.86f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        LowPolyButtonPress press = GetOrAdd<LowPolyButtonPress>(buttonRoot);
        SerializedObject serializedPress = new SerializedObject(press);
        SerializedProperty faceProp = serializedPress.FindProperty("face");
        if (faceProp != null)
            faceProp.objectReferenceValue = faceRect;
        SerializedProperty offsetProp = serializedPress.FindProperty("pressedOffset");
        if (offsetProp != null)
            offsetProp.floatValue = 6f;
        serializedPress.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    // ----- Idempotent root handling -----

    private static GameObject FindSceneRoot(Scene scene, string name)
    {
        if (!scene.IsValid())
            return null;
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == name)
                return go;
        return null;
    }

    private static GameObject CreateRoot(Scene scene)
    {
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Create " + RootName);
        if (scene.IsValid())
            SceneManager.MoveGameObjectToScene(root, scene);
        return root;
    }

    private static GameObject PrepareExistingRoot(GameObject root)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(root) &&
            PrefabUtility.GetNearestPrefabInstanceRoot(root) == root)
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
        }

        // Only our own generated children are cleared and rebuilt; unrelated UI
        // never lives under this root, so nothing else is touched.
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);

        RemoveDuplicateComponents<MainPanelController>(root);
        RemoveDuplicateComponents<GraphicRaycaster>(root);
        RemoveDuplicateComponents<CanvasScaler>(root);
        RemoveDuplicateComponents<Canvas>(root);
        return root;
    }

    // ----- Primitive builders (mirrors WhileYouWereAwayPopupBuilder) -----

    private static LowPolyPanelGraphic CreatePanel(
        string name, Transform parent, Color color, float cornerCut, float bevel, bool raycastTarget)
    {
        GameObject gameObject = CreateRect(name, parent);
        EnsureCanvasRenderer(gameObject);
        LowPolyPanelGraphic graphic = gameObject.AddComponent<LowPolyPanelGraphic>();
        graphic.color = color;
        graphic.raycastTarget = raycastTarget;
        PremiumUiStyle.ConfigureSurface(graphic, color, cornerCut, bevel);
        return graphic;
    }

    private static void ConfigurePanel(LowPolyPanelGraphic graphic, float cornerCut, float bevel)
    {
        SerializedObject serialized = new SerializedObject(graphic);
        serialized.FindProperty("cornerCut").floatValue = cornerCut;
        serialized.FindProperty("bevelWidth").floatValue = bevel;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text CreateText(
        string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject gameObject = CreateRect(name, parent);
        EnsureCanvasRenderer(gameObject);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject gameObject = CreateRect(name, parent);
        EnsureCanvasRenderer(gameObject);
        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return gameObject;
    }

    private static void EnsureCanvasRenderer(GameObject gameObject)
    {
        if (gameObject.GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();
    }

    private static void ValidateGraphics(GameObject root)
    {
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
            EnsureCanvasRenderer(graphic.gameObject);

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.targetGraphic == null ||
                button.targetGraphic.GetComponent<CanvasRenderer>() == null)
            {
                throw new System.InvalidOperationException(
                    "Main Panel: a button has no valid CanvasRenderer-backed targetGraphic (" +
                    button.name + ")."
                );
            }
        }
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void RemoveDuplicateComponents<T>(GameObject gameObject) where T : Component
    {
        T[] components = gameObject.GetComponents<T>();
        for (int i = components.Length - 1; i > 0; i--)
            Undo.DestroyObjectImmediate(components[i]);
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void IgnoreLayout(RectTransform rect)
    {
        LayoutElement element = GetOrAdd<LayoutElement>(rect.gameObject);
        element.ignoreLayout = true;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, Vector2.zero, Vector2.one);
    }

    private static void StretchWithOffsets(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static TMP_FontAsset FindFont()
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text != null && text.font != null)
                return text.font;
        }
        TMP_FontAsset premium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        return premium != null
            ? premium
            : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-SemiBold SDF.asset");
    }

    private static void Assign(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new System.InvalidOperationException("Missing serialized controller property: " + propertyName);
        property.objectReferenceValue = value;
    }

    private static void AssignRows(SerializedObject serialized, List<RowData> rowData)
    {
        SerializedProperty rowsProp = serialized.FindProperty("rows");
        if (rowsProp == null)
            throw new System.InvalidOperationException("Missing serialized controller property: rows");

        rowsProp.arraySize = rowData.Count;
        for (int i = 0; i < rowData.Count; i++)
        {
            SerializedProperty element = rowsProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("id").stringValue = rowData[i].Id;
            element.FindPropertyRelative("button").objectReferenceValue = rowData[i].Button;
            element.FindPropertyRelative("group").objectReferenceValue = rowData[i].Group;
            element.FindPropertyRelative("badge").objectReferenceValue = rowData[i].Badge;
            element.FindPropertyRelative("badgeText").objectReferenceValue = rowData[i].BadgeText;
            element.FindPropertyRelative("label").objectReferenceValue = rowData[i].Label;
            element.FindPropertyRelative("layout").objectReferenceValue = rowData[i].Layout;
            element.FindPropertyRelative("labelLayout").objectReferenceValue = rowData[i].LabelLayout;
        }
    }

    private static void EnsureFolder()
    {
        if (!Directory.Exists(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "UI");
    }

    private readonly struct MenuButtonParts
    {
        public MenuButtonParts(Button button, CanvasGroup group) { Button = button; Group = group; }
        public Button Button { get; }
        public CanvasGroup Group { get; }
    }

    private readonly struct ShopButtonParts
    {
        public ShopButtonParts(Button button, CanvasGroup group) { Button = button; Group = group; }
        public Button Button { get; }
        public CanvasGroup Group { get; }
    }

    private readonly struct LightButtonParts
    {
        public LightButtonParts(Button button, CanvasGroup group, Graphic faceGraphic)
        {
            Button = button;
            Group = group;
            FaceGraphic = faceGraphic;
        }
        public Button Button { get; }
        public CanvasGroup Group { get; }
        public Graphic FaceGraphic { get; }
    }

    private readonly struct TopButtonParts
    {
        public TopButtonParts(GameObject root, CanvasGroup group, GameObject face, LowPolyPanelGraphic faceGraphic)
        {
            Root = root;
            Group = group;
            Face = face;
            FaceGraphic = faceGraphic;
        }
        public GameObject Root { get; }
        public CanvasGroup Group { get; }
        public GameObject Face { get; }
        public LowPolyPanelGraphic FaceGraphic { get; }
    }

    private readonly struct ListParts
    {
        public ListParts(RectTransform list, CanvasGroup group) { List = list; Group = group; }
        public RectTransform List { get; }
        public CanvasGroup Group { get; }
    }

    private readonly struct RowData
    {
        public RowData(string id, Button button, CanvasGroup group, GameObject badge, TMP_Text badgeText,
            TMP_Text label, LayoutElement layout, LayoutElement labelLayout)
        {
            Id = id;
            Button = button;
            Group = group;
            Badge = badge;
            BadgeText = badgeText;
            Label = label;
            Layout = layout;
            LabelLayout = labelLayout;
        }
        public string Id { get; }
        public Button Button { get; }
        public CanvasGroup Group { get; }
        public GameObject Badge { get; }
        public TMP_Text BadgeText { get; }
        public TMP_Text Label { get; }
        public LayoutElement Layout { get; }
        public LayoutElement LabelLayout { get; }
    }

    private readonly struct BadgeParts
    {
        public BadgeParts(GameObject root, TMP_Text text) { Root = root; Text = text; }
        public GameObject Root { get; }
        public TMP_Text Text { get; }
    }
}
