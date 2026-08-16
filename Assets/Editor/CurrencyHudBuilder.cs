using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Idempotent Editor builder for the top-left progression/currency HUD. It owns
/// exactly one scene root — "CurrencyHudCanvas" — and rebuilds only that subtree.
/// Everything else in the scene (MainPanelCanvas, ShopPanelCanvas,
/// QuestPanelCanvas, HUD Canvas, joystick, ActionButton, need indicators, popups,
/// tutorial, …) is left untouched.
///
/// Three separately framed badges in the top-left corner of the mobile safe
/// area, drawn from the exact same palette and octagon recipe as the top-bar
/// buttons, the main panel and the shop cards:
///
///   CurrencyHudCanvas (Canvas overlay, sortingOrder 95 + CurrencyHudController)
///     SafeArea            (SafeAreaRect)
///       Group             top-left anchored group, 376 x 68
///         BondXpEntry     116 x 68 frame: heart icon + value + small green "+"
///         CoinEntry       116 x 68 frame: coin icon  + value + small green "+"
///         DiamondEntry    116 x 68 frame: gem icon   + value + small green "+"
///
/// Each entry carries its own shadow / orange frame / cream face rather than
/// sharing one background strip, because the gaps between them have to read the
/// same way the gaps between the top-right buttons do.
///
/// Alignment with the rest of the top bar is arithmetic, not eyeballing. The
/// entries are the same 68 units tall as the top-right buttons and hang from the
/// same -22 top offset, which puts their vertical centre at -22 - 34 = -56 — the
/// same centre the clock reaches from its own 56-unit height at -28, and the
/// same centre the top-right buttons sit on. The group pivots around that centre,
/// so responsive scale-down cannot move it a pixel above or below the other HUD.
///
/// Inside an entry the icon, the amount and the "+" are one content group,
/// centred as a unit and sized by CurrencyEntryLayout from the amount's measured
/// width, so the "+" always sits a fixed 7 units from the last digit instead of
/// being pinned to the frame's right edge.
///
/// The canvas sits *below* MainPanelCanvas (100) and ShopPanelCanvas (110) so the
/// main menu's outside-tap scrim and the shop reliably cover it. Only the three
/// "+" faces are raycast targets; the frames, icons and labels are pure
/// decoration and never steal a world tap.
///
/// The group is narrow enough to clear the centered Hunger / Thirst / Energy
/// group; on a portrait or otherwise narrow screen CurrencyHudController scales
/// the whole group down rather than letting it reach them. The needs HUD itself
/// is never touched.
///
/// Running the menu item again reuses the existing "CurrencyHudCanvas" root: its
/// children are cleared and rebuilt, so no duplicate canvas, entry, icon or
/// button is produced.
/// </summary>
public static class CurrencyHudBuilder
{
    private static bool suppressDialogs;
    private const string RootName = "CurrencyHudCanvas";
    private const string PrefabFolder = "Assets/UI";
    private const string PrefabPath = PrefabFolder + "/CurrencyHud.prefab";

    // Below MainPanelCanvas (100) and ShopPanelCanvas (110): the drop-down's
    // outside-tap scrim and the shop scrim must always cover this HUD.
    private const int HudSortingOrder = 95;

    // Layout (canvas units, 1920x1080 reference). The left margin and the top
    // offset mirror the top-bar buttons on the opposite corner so both corners
    // read as one bar.
    private const float LeftMargin = 44f;
    private const float TopOffset = -22f;

    // MainPanelBuilder's TopButtonHeight. Duplicated rather than referenced
    // because the two builders own different canvases and must stay independently
    // buildable; the doc comment above records why the number has to match.
    private const float TopBarButtonHeight = 68f;
    private const float TopBarCenterY = TopOffset - TopBarButtonHeight * 0.5f;

    // Same height and same top offset as the top-right buttons, so both corners
    // land on vertical centre -56. Width stays compact on purpose: at 116 the
    // three entries plus their gaps span 376 units, which is the same total the
    // previous strip occupied and keeps the existing clearance to the centered
    // needs group.
    private const float GroupHeight = TopBarButtonHeight;
    private const float EntryWidth = 142f;
    // Matches MainPanelBuilder's TopButtonGap so the rhythm across the whole top
    // bar is one value.
    private const float EntryGap = 14f;
    private const float GroupWidth = EntryWidth * 3f + EntryGap * 2f;

    // Entry internals. The fixed parts sum to an even 68 (32 + 4 + 6 + 26) so
    // CurrencyEntryLayout's half-width offsets stay on whole units.
    private const float IconSize = 32f;
    private const float IconValueGap = 4f;
    private const float ValuePlusGap = 6f;
    private const float PlusButtonSize = 26f;
    private const float ContentPadX = 7f;
    private const float FixedContentWidth = IconSize + IconValueGap + ValuePlusGap + PlusButtonSize;

    // What is left over for the digits once the icon, the "+" and their gaps have
    // taken their share of the 116-unit frame: 116 - 14 - 68 = 34. Amounts are
    // fitted into this box by CurrencyEntryLayout, so a short amount renders at
    // the full 26pt and only long ones step down.
    private const float MaxValueWidth = EntryWidth - ContentPadX * 2f - FixedContentWidth;
    // Keeps a single "0" from collapsing into a sliver against the "+".
    private const float MinValueWidth = 16f;
    private const float ValueRectHeight = 40f;
    // 25 exactly matches the clock label; 11 is the floor a six-digit
    // amount lands on, which still leaves the "+" clear of the digits.
    private const float ValueFontMax = 25f;
    private const float ValueFontMin = 11f;

    private static readonly Color FrameOrange = new Color32(255, 221, 79, 255);
    private static readonly Color FrameShadow = PremiumUiStyle.Shadow;
    private static readonly Color CreamBar = new Color32(255, 250, 231, 255);
    private static readonly Color DarkBrownText = new Color32(55, 41, 78, 255);

    // Per-entry identity colours. The common orange frame keeps the top-bar
    // family intact while the inset face and icon identify the currency.
    private static readonly Color BondFace = new Color32(255, 105, 165, 255);
    private static readonly Color BondRose = new Color32(242, 85, 108, 255);        // #F2556C
    private static readonly Color BondRoseDark = new Color32(183, 47, 75, 255);     // #B72F4B
    private static readonly Color BondWhite = new Color32(255, 248, 249, 255);      // #FFF8F9

    private static readonly Color CoinFace = new Color32(255, 213, 73, 255);
    private static readonly Color CoinGold = new Color32(255, 201, 40, 255);        // #FFC928
    private static readonly Color CoinGoldDeep = new Color32(246, 168, 0, 255);     // #F6A800
    private static readonly Color CoinGoldDark = new Color32(168, 90, 0, 255);      // #A85A00
    private static readonly Color CoinHighlight = new Color32(255, 242, 166, 255);  // #FFF2A6

    private static readonly Color DiamondFace = new Color32(72, 224, 236, 255);
    private static readonly Color DiamondBlue = new Color32(57, 191, 255, 255);     // #39BFFF
    private static readonly Color DiamondBlueDeep = new Color32(8, 127, 221, 255);  // #087FDD
    private static readonly Color DiamondBlueDark = new Color32(18, 102, 186, 255); // #1266BA
    private static readonly Color DiamondHighlight = new Color32(191, 243, 255, 255); // #BFF3FF

    // The only new colour in the family: a warm leaf green for the "+" affordance.
    private static readonly Color PlusGreen = new Color32(70, 225, 190, 255);

    [MenuItem("Tools/Cat Home/Build Currency HUD (Top-Left)")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            string message =
                "No EventSystem was found in the open scene. Open GameScene and make " +
                "sure its EventSystem is present before building the currency HUD.";
            if (!Application.isBatchMode && !suppressDialogs)
                EditorUtility.DisplayDialog("Currency HUD", message, "OK");
            else
                Debug.LogWarning("Currency HUD rebuild skipped: " + message);
            return;
        }

        PremiumUiFactory.ConfigureCurrencyImporters();
        TMP_FontAsset font = FindFont();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Currency HUD");

        GameObject existing = FindSceneRoot(scene, RootName);
        GameObject root = existing != null ? PrepareExistingRoot(existing) : CreateRoot(scene);

        // ----- Canvas plumbing -----
        Canvas canvas = GetOrAdd<Canvas>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = false;
        canvas.sortingOrder = HudSortingOrder;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(root);

        // One gate for the whole HUD. Parked hidden; the controller reveals it
        // once onboarding is complete and no blocking modal is up.
        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        CurrencyHudController controller = GetOrAdd<CurrencyHudController>(root);

        // ----- Safe-area child (mandatory: the HUD hugs the notch-safe corner) -----
        GameObject safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea.GetComponent<RectTransform>());
        safeArea.AddComponent<SafeAreaRect>();

        // ----- Top-left group -----
        GameObject group = CreateRect("Group", safeArea.transform);
        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.anchorMin = groupRect.anchorMax = new Vector2(0f, 1f);
        // Left-centre pivot: responsive scale-down keeps both the left edge and
        // exact top-bar centre fixed. At scale 1, centre -56 plus half-height 34
        // still yields the authored -22 top edge.
        groupRect.pivot = new Vector2(0f, 0.5f);
        groupRect.anchoredPosition = new Vector2(LeftMargin, TopBarCenterY);
        groupRect.sizeDelta = new Vector2(GroupWidth, GroupHeight);

        // Left to right: Bond XP -> Coins -> Diamonds.
        EntryParts bondXpEntry =
            CreateEntry(group.transform, "BondXpEntry", 0, font, BondFace, BuildBondXpIcon);
        EntryParts coinEntry =
            CreateEntry(group.transform, "CoinEntry", 1, font, CoinFace, BuildCoinIcon);
        EntryParts diamondEntry =
            CreateEntry(group.transform, "DiamondEntry", 2, font, DiamondFace, BuildDiamondIcon);

        // ----- Wire the controller -----
        SerializedObject serialized = new SerializedObject(controller);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "groupRect", groupRect);
        Assign(serialized, "bondXpText", bondXpEntry.Value);
        Assign(serialized, "coinText", coinEntry.Value);
        Assign(serialized, "diamondText", diamondEntry.Value);
        Assign(serialized, "bondXpPlusButton", bondXpEntry.PlusButton);
        Assign(serialized, "coinPlusButton", coinEntry.PlusButton);
        Assign(serialized, "diamondPlusButton", diamondEntry.PlusButton);
        // The HUD carries no economy of its own: the serialized amounts are a
        // layout preview only, so a rebuild leaves them at zero rather than
        // baking a hardcoded balance into the prefab.
        AssignLong(serialized, "bondXp", 0L);
        AssignLong(serialized, "coins", 0L);
        AssignLong(serialized, "diamonds", 0L);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Paint the labels through the controller so the Scene view preview uses
        // exactly the same formatting the game will.
        controller.Refresh();
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
        if (!Application.isBatchMode && !suppressDialogs)
        {
            Debug.Log(
                (existing == null ? "Currency HUD created" : "Existing currency HUD rebuilt") +
                " as a separate canvas (sortingOrder " + HudSortingOrder + ") and prefab saved at " +
                PrefabPath + ".\n\nBond XP, Coins and Diamonds read live from ProgressionService " +
                "and EconomyService at runtime — no placeholder amounts are baked in. The three " +
                "'+' buttons only log which entry was pressed; nothing is granted, spent or saved." +
                fontWarning
            );
        }
    }

    public static void BuildSilently()
    {
        suppressDialogs = true;
        try { Build(); }
        finally { suppressDialogs = false; }
    }

    /// <summary>Headless entry point for regenerating the prefab and GameScene instance.</summary>
    public static void BuildGameSceneBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        Build();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    // ----- Entry frame -----

    /// <summary>
    /// Soft depth panel, orange frame and cream face for one currency entry.
    /// Depth must share the same rect as the frame (no +X/−Y drop offset) —
    /// a right/bottom-shifted shadow reads as a broken frame on every HUD pill.
    /// </summary>
    private static void CreateEntryFrame(Transform parent, Color faceColor)
    {
        LowPolyPanelGraphic shadow = CreatePanelGraphic("Shadow", parent, PremiumUiStyle.SoftShadow, 28f, 0f, false);
        PremiumUiStyle.SetCenteredShadowStretch(shadow.rectTransform, 3f);

        LowPolyPanelGraphic frame = CreatePanelGraphic("Frame", parent, FrameOrange, 30f, 2f, false);
        Stretch(frame.rectTransform);

        // The identity face distinguishes the currency at a glance. Its 5-unit
        // inset leaves the shared orange reading as a frame rather than a halo.
        LowPolyPanelGraphic face = CreatePanelGraphic(
            "Face",
            parent,
            Color.Lerp(CreamBar, faceColor, 0.16f),
            27f,
            2f,
            false);
        StretchWithOffsets(face.rectTransform, 5f, 5f, -5f, -5f);

        LowPolyPanelGraphic identity = CreatePanelGraphic("Identity", parent, faceColor, 6f, 1.5f, false);
        SetRect(identity.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(5f, -18f));
    }

    // ----- Entries -----

    private readonly struct EntryParts
    {
        public EntryParts(TMP_Text value, Button plusButton)
        {
            Value = value;
            PlusButton = plusButton;
        }

        public TMP_Text Value { get; }
        public Button PlusButton { get; }
    }

    /// <summary>
    /// One entry: its own low-poly frame, a procedural badge icon, the amount and
    /// a small green "+". The three pieces of content are laid out at runtime by
    /// <see cref="CurrencyEntryLayout"/> as one centred group whose width follows
    /// the amount, so nothing here pins the "+" to an edge. Returns the label and
    /// the button so the caller can wire them into the controller.
    /// </summary>
    private static EntryParts CreateEntry(
        Transform parent, string name, int index, TMP_FontAsset font, Color faceColor,
        System.Action<Transform> buildIcon)
    {
        GameObject entryRoot = CreateRect(name, parent);
        RectTransform rect = entryRoot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(index * (EntryWidth + EntryGap), 0f);
        rect.sizeDelta = new Vector2(EntryWidth, GroupHeight);

        CreateEntryFrame(entryRoot.transform, faceColor);

        // Icon badge. Every child is authored directly at its final integer size;
        // localScale remains 1 so mesh and canvas pixel adjustment agree.
        GameObject iconRoot = CreateRect("Icon", entryRoot.transform);
        RectTransform iconRect = iconRoot.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(IconSize, IconSize);
        buildIcon(iconRoot.transform);

        // The amount's box is sized from its measured width every repaint; the
        // size set here is only a starting point for the first pass. Auto-sizing
        // is deliberately off — CurrencyEntryLayout owns the fit so the width it
        // measures always describes the size actually being rendered.
        TMP_Text value = CreateText(
            entryRoot.transform, "Value", font, ValueFontMax, DarkBrownText, TextAlignmentOptions.Midline);
        value.fontStyle = FontStyles.Bold;
        value.fontWeight = FontWeight.Bold;
        // Tracking costs width the 116-unit frame cannot spare, and it is the
        // main source of soft edges on small digits.
        value.characterSpacing = 0f;
        value.enableAutoSizing = false;
        value.extraPadding = true;
        value.isTextObjectScaleStatic = true;
        value.rectTransform.anchorMin = value.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        value.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        value.rectTransform.sizeDelta = new Vector2(MaxValueWidth, ValueRectHeight);

        Button plusButton = CreatePlusButton(entryRoot.transform);

        CurrencyEntryLayout layout = entryRoot.AddComponent<CurrencyEntryLayout>();
        layout.Configure(
            iconRect,
            value,
            plusButton.GetComponent<RectTransform>(),
            IconSize,
            IconValueGap,
            ValuePlusGap,
            PlusButtonSize,
            MinValueWidth,
            MaxValueWidth,
            ValueFontMin,
            ValueFontMax);

        return new EntryParts(value, plusButton);
    }

    // ----- Small green "+" button (one per entry) -----

    /// <summary>
    /// The "+" is centre-anchored and carries no anchored position of its own:
    /// CurrencyEntryLayout places it a fixed <see cref="ValuePlusGap"/> to the
    /// right of the last digit, wherever that falls.
    /// </summary>
    private static Button CreatePlusButton(Transform parent)
    {
        GameObject buttonRoot = CreateRect("PlusButton", parent);
        RectTransform rect = buttonRoot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(PlusButtonSize, PlusButtonSize);

        GameObject face = CreateRect("Face", buttonRoot.transform);
        Stretch(face.GetComponent<RectTransform>());
        EnsureCanvasRenderer(face);
        LowPolyPanelGraphic faceGraphic = face.AddComponent<LowPolyPanelGraphic>();
        faceGraphic.color = PlusGreen;
        // The only raycast targets in this HUD are these three faces.
        faceGraphic.raycastTarget = true;
        ConfigurePanel(faceGraphic, 7f, 3f);

        // Cream plus glyph, matching the cream-on-colour glyphs of the top bar.
        // Even arm sizes keep the bars centred on whole units inside the 26 box.
        IconRect(face.transform, "PlusH", CreamBar, 14f, 4f, 0f, 0f);
        IconRect(face.transform, "PlusV", CreamBar, 4f, 14f, 0f, 0f);

        return MakeButton(buttonRoot, faceGraphic, face.GetComponent<RectTransform>());
    }

    // ----- Crisp procedural icons (authored directly at final size) -----

    // Bond XP: a high-contrast white paw on a rose badge. All parts use integer
    // dimensions and positions; no transform scaling or grayscale tinting.
    private static void BuildBondXpIcon(Transform t)
    {
        IconOctagon(t, "Shadow", BondRoseDark, 30f, 30f, 1f, -1f, 8f, 2f);
        IconOctagon(t, "RoseBadge", BondRose, 30f, 30f, 0f, 0f, 8f, 2f);
        IconOctagon(t, "Pad", BondWhite, 12f, 10f, 0f, -5f, 4f, 0f);
        IconOctagon(t, "ToeLeft", BondWhite, 6f, 7f, -8f, 4f, 3f, 0f);
        IconOctagon(t, "ToeMidLeft", BondWhite, 6f, 8f, -3f, 7f, 3f, 0f);
        IconOctagon(t, "ToeMidRight", BondWhite, 6f, 8f, 3f, 7f, 3f, 0f);
        IconOctagon(t, "ToeRight", BondWhite, 6f, 7f, 8f, 4f, 3f, 0f);
    }

    // Layered gold coin with a dark edge, inset face, embossed cat paw and glint.
    private static void BuildCoinIcon(Transform t)
    {
        if (PremiumUiFactory.BuildCurrencyIcon(
                t,
                PremiumUiFactory.CurrencyVisual.Coin))
        {
            return;
        }

        IconOctagon(t, "CoinShadow", CoinGoldDark, 32f, 32f, 1f, -2f, 12f, 1.5f);
        IconOctagon(t, "CoinOuterEdge", CoinGoldDark, 32f, 32f, 0f, 0f, 12f, 1.5f);
        IconOctagon(t, "CoinGoldRim", CoinGold, 28f, 28f, 0f, 0f, 11f, 1.5f);
        IconOctagon(t, "CoinInsetFace", CoinGoldDeep, 21f, 21f, 0f, 0f, 8f, 1.2f);
        IconOctagon(t, "PawPad", CoinHighlight, 8f, 7f, 0f, -3f, 3f, 0.5f);
        IconOctagon(t, "PawToeLeft", CoinHighlight, 4f, 5f, -6f, 4f, 2f, 0.5f);
        IconOctagon(t, "PawToeMiddle", CoinHighlight, 4f, 5f, 0f, 6f, 2f, 0.5f);
        IconOctagon(t, "PawToeRight", CoinHighlight, 4f, 5f, 6f, 4f, 2f, 0.5f);
        IconRect(t, "CoinShine", Color.white, 3f, 8f, -9f, 7f, -28f);
    }

    // High-contrast faceted gem. A dark silhouette keeps it legible on cyan UI.
    private static void BuildDiamondIcon(Transform t)
    {
        if (PremiumUiFactory.BuildCurrencyIcon(
                t,
                PremiumUiFactory.CurrencyVisual.Diamond))
        {
            return;
        }

        IconRect(t, "GemShadow", new Color32(2, 42, 91, 210), 27f, 27f, 1f, -3f, 45f);
        IconRect(t, "GemOutline", DiamondBlueDark, 27f, 27f, 0f, -1f, 45f);
        IconRect(t, "GemBody", DiamondBlue, 22f, 22f, 0f, -1f, 45f);
        IconRect(t, "GemInnerFacet", DiamondBlueDeep, 14f, 14f, 1f, -1f, 45f);
        IconRect(t, "FacetHighlight", DiamondHighlight, 4f, 14f, -5f, 0f, -35f);
        IconRect(t, "GemGlintV", Color.white, 3f, 9f, 8f, 9f, 45f);
        IconRect(t, "GemGlintH", Color.white, 9f, 3f, 8f, 9f, 45f);
    }

    private static void IconRect(
        Transform parent, string name, Color color, float w, float h, float x, float y,
        float rotation = 0f, float scale = 1f)
    {
        Image image = CreateImage(name, parent, color, false);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w * scale, h * scale);
        rect.anchoredPosition = new Vector2(x * scale, y * scale);
        if (!Mathf.Approximately(rotation, 0f))
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
    }

    private static void IconOctagon(
        Transform parent, string name, Color color, float w, float h, float x, float y,
        float cut, float bevel, float scale = 1f)
    {
        LowPolyPanelGraphic panel =
            CreatePanelGraphic(name, parent, color, cut * scale, bevel * scale, false);
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w * scale, h * scale);
        rect.anchoredPosition = new Vector2(x * scale, y * scale);
    }

    // ----- Button helper (same press feel as the top bar / shop) -----

    private static Button MakeButton(GameObject buttonRoot, Graphic targetGraphic, RectTransform faceRect)
    {
        Button button = GetOrAdd<Button>(buttonRoot);
        button.targetGraphic = targetGraphic;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        // ColorTint multiplies the face colour, so these are the multipliers that
        // turn the leaf green into the mockup's three states.
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        // 0.8 on the green face gives the mockup's darker pressed green.
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        // Future option only — nothing disables these buttons today. A multiply
        // cannot fully desaturate green to the mockup's grey, so this is as close
        // as ColorTint reaches; a real grey state would need a swapped face
        // colour, which is left for whoever actually turns the state on.
        colors.disabledColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        LowPolyButtonPress press = GetOrAdd<LowPolyButtonPress>(buttonRoot);
        SerializedObject serializedPress = new SerializedObject(press);
        SerializedProperty faceProp = serializedPress.FindProperty("face");
        if (faceProp != null)
            faceProp.objectReferenceValue = faceRect;
        SerializedProperty offsetProp = serializedPress.FindProperty("pressedOffset");
        if (offsetProp != null)
            offsetProp.floatValue = 3f;
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

        RemoveDuplicateComponents<CurrencyHudController>(root);
        RemoveDuplicateComponents<GraphicRaycaster>(root);
        RemoveDuplicateComponents<CanvasScaler>(root);
        RemoveDuplicateComponents<Canvas>(root);
        RemoveDuplicateComponents<CanvasGroup>(root);
        return root;
    }

    // ----- Primitive builders (mirrors ShopPanelBuilder) -----

    private static LowPolyPanelGraphic CreatePanelGraphic(
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
        Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
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
        text.extraPadding = true;
        text.isTextObjectScaleStatic = true;
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
                    "Currency HUD: a button has no valid CanvasRenderer-backed targetGraphic (" +
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

    private static void AssignLong(SerializedObject serialized, string propertyName, long value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new System.InvalidOperationException("Missing serialized controller property: " + propertyName);
        property.longValue = value;
    }

    private static void EnsureFolder()
    {
        if (!Directory.Exists(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "UI");
    }
}
