using System.Collections.Generic;
using System.IO;
using TMPro;
using U = PremiumUiElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Idempotent Editor builder for the CP3 production Quest Panel. It owns exactly
/// one scene root — "QuestPanelCanvas" — and rebuilds only that subtree.
/// Everything else in the scene (MainPanelCanvas, ShopPanelCanvas, HUD Canvas,
/// joystick, ActionButton, need indicators, popups, tutorial, CatSpeechBubble, …)
/// is left untouched.
///
/// The panel is a centred low-poly card in the mobile safe area, opened by the
/// existing hamburger menu's QUESTS row (MainPanelController routes the tap here):
///
///   QuestPanelCanvas (Canvas overlay, sortingOrder 108 + QuestPanelController)
///     Scrim               full-screen dim + outside-tap catcher
///     SafeArea            (SafeAreaRect)
///       Panel             centred low-poly card
///         Header          orange bar with the "QUESTS" title
///           CloseButton   low-poly X, top-right of the header
///         LevelLabel      "LEVEL 1 · First Meals"
///         Message         completed / unavailable state (hidden while rows show)
///         ScrollView      viewport + content
///           Content       QuestRow_0 … QuestRow_5 (pooled, never instantiated at runtime)
///         Footer          "Coins: … Bond XP: … Diamonds: …"
///
/// Running the menu item again reuses the existing "QuestPanelCanvas" root: its
/// children are cleared and rebuilt, so no duplicate canvas, scrim, panel or rows
/// are produced. The visual recipe (colours, low-poly frames, close button,
/// button press) is the same one MainPanelBuilder and ShopPanelBuilder use, so the
/// quest panel reads as part of the same UI family.
/// </summary>
public static class QuestPanelBuilder
{
    private static bool suppressDialogs;
    private const string RootName = "QuestPanelCanvas";
    private const string PrefabFolder = "Assets/UI";
    private const string PrefabPath = PrefabFolder + "/QuestPanel.prefab";

    // Above MainPanelCanvas (100) so the panel always covers the top bar and its
    // drop-down list, and below ShopPanelCanvas (110), which the controller
    // already treats as a blocking modal.
    private const int PanelSortingOrder = 108;

    // Panel geometry (canvas units, 1920x1080 reference). The width is finalized
    // at runtime by the controller (fraction + min/max clamp); this is the
    // fallback. The height is authored here and only ever scaled down by the
    // controller when the safe area is too short for it.
    private const float FallbackPanelWidth = 1120f;
    private const float HeaderHeight = 100f;
    private const float HeaderInset = 6f;
    private const float PanelSidePadding = 38f;

    private const float LevelLabelTopGap = 14f;
    private const float LevelLabelHeight = 64f;
    private const float BodyTopGap = 12f;

    // Row pool. Every level in the current ProgressionConfig has at most two
    // quests; the pool is deliberately larger so a bigger level added later still
    // renders without a rebuild, and the list scrolls when it does not fit.
    private const int RowPoolCount = 6;
    private const float RowHeight = 150f;
    private const float RowSpacing = 16f;
    private const int VisibleRows = 3;
    private const float BodyHeight = RowHeight * VisibleRows + RowSpacing * (VisibleRows - 1);

    private const float FooterTopGap = 12f;
    private const float FooterHeight = 54f;
    private const float PanelBottomPadding = 24f;

    private const float PanelHeight =
        HeaderInset * 2f + HeaderHeight + LevelLabelTopGap + LevelLabelHeight +
        BodyTopGap + BodyHeight + FooterTopGap + FooterHeight + PanelBottomPadding;

    // Distances measured from the panel's top edge.
    private const float HeaderTop = HeaderInset * 2f;
    private const float LevelLabelTop = HeaderTop + HeaderHeight + LevelLabelTopGap;
    private const float BodyTop = LevelLabelTop + LevelLabelHeight + BodyTopGap;

    // Row internals.
    private const float RowContentPaddingX = 22f;
    private const float RowContentPaddingY = 15f;
    private const float RowColumnGap = 16f;
    private const float ActionColumnWidth = 210f;
    private const float TitleHeight = 38f;
    private const float DescriptionHeight = 40f;
    private const float MetaHeight = 30f;
    private const float InfoRowSpacing = 5f;
    private const float ProgressWidth = 90f;

    // Claim button. 190x64 canvas units keeps a comfortable touch target on the
    // 1920x1080 reference: it is wider and taller than the top-bar buttons the
    // project already ships.
    private const float ClaimButtonWidth = 190f;
    private const float ClaimButtonHeight = 64f;

    // Close button (a compact twin of the top-bar buttons), same as the shop's.
    private const float CloseButtonSize = 56f;

    private static readonly Color ScrimColor = new Color32(50, 42, 96, 178);
    private static readonly Color FrameOrange = PremiumUiStyle.CoralLift;
    private static readonly Color FrameShadow = PremiumUiStyle.Shadow;
    private static readonly Color PanelCream = new Color32(255, 247, 226, 255);
    private static readonly Color CreamBar = PremiumUiStyle.Ivory;
    private static readonly Color DarkBrownText = PremiumUiStyle.Ink;
    private static readonly Color TitleShadowColor = PremiumUiStyle.Shadow;
    private static readonly Color ClaimGreen = PremiumUiStyle.Teal;

    // Slight per-row cream variation so the stack does not read as a flat block,
    // exactly like the menu and shop cards.
    private static readonly Color[] RowCreams =
    {
        new Color32(218, 252, 243, 255),
        new Color32(237, 225, 255, 255),
        new Color32(255, 231, 216, 255)
    };

    [MenuItem("Tools/Cat Home/Build Quest Panel (CP3)")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            string message = "No EventSystem was found in the open scene. Open GameScene and " +
                "make sure its EventSystem is present before building the quest panel.";
            if (!Application.isBatchMode && !suppressDialogs)
                EditorUtility.DisplayDialog("Quest Panel (CP3)", message, "OK");
            else
                Debug.LogWarning("Quest panel rebuild skipped: " + message);
            return;
        }

        TMP_FontAsset font = FindFont();
        CatMovement catMovement = Object.FindAnyObjectByType<CatMovement>();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Quest Panel (CP3)");

        GameObject existing = FindSceneRoot(scene, RootName);
        GameObject root = existing != null ? PrepareExistingRoot(existing) : CreateRoot(scene);

        // ----- Canvas plumbing (identical settings to the shop's canvas) -----
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

        // One gate for the whole canvas: while closed it blocks nothing, so the
        // world, the HUD and the top bar behave exactly as before.
        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 1f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        QuestPanelController controller = GetOrAdd<QuestPanelController>(root);

        // ----- Scrim: full-canvas dim; a tap outside the panel closes it -----
        GameObject scrim = CreateRect("Scrim", root.transform);
        Stretch(scrim.GetComponent<RectTransform>());
        Image scrimImage = GetOrAdd<Image>(scrim);
        EnsureCanvasRenderer(scrim);
        scrimImage.color = ScrimColor;
        scrimImage.raycastTarget = true;
        CanvasGroup scrimGroup = GetOrAdd<CanvasGroup>(scrim);
        scrimGroup.alpha = 0f;
        Button scrimButton = GetOrAdd<Button>(scrim);
        scrimButton.transition = Selectable.Transition.None;
        scrimButton.targetGraphic = scrimImage;

        // ----- Safe-area child (the panel itself) -----
        GameObject safeArea = CreateRect("SafeArea", root.transform);
        RectTransform safeAreaRect = safeArea.GetComponent<RectTransform>();
        Stretch(safeAreaRect);
        safeArea.AddComponent<SafeAreaRect>();

        // ----- Panel -----
        PanelParts panelParts = CreatePanel(safeArea.transform, font, out List<RowData> rowData);

        // ----- Wire the controller -----
        SerializedObject serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", catMovement);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "scrimGroup", scrimGroup);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "safeArea", safeAreaRect);
        Assign(serialized, "panel", panelParts.Panel);
        Assign(serialized, "panelGroup", panelParts.Group);
        Assign(serialized, "closeButton", panelParts.CloseButton);
        Assign(serialized, "levelLabel", panelParts.LevelLabel);
        Assign(serialized, "walletLabel", panelParts.WalletLabel);
        Assign(serialized, "messageRoot", panelParts.MessageRoot);
        Assign(serialized, "messageLabel", panelParts.MessageLabel);
        Assign(serialized, "scrollRoot", panelParts.ScrollRoot);
        Assign(serialized, "scrollRect", panelParts.ScrollRect);
        serialized.FindProperty("widthFraction").floatValue = 0.64f;
        serialized.FindProperty("minWidth").floatValue = 1040f;
        serialized.FindProperty("maxWidth").floatValue = 1240f;
        serialized.FindProperty("safeAreaMargin").floatValue = 32f;
        AssignRows(serialized, rowData);
        serialized.FindProperty("chapterTab").objectReferenceValue = root.transform.Find("SafeArea/Panel/LevelLabel/ChapterTab").GetComponent<Button>();
        serialized.FindProperty("dailyTab").objectReferenceValue = root.transform.Find("SafeArea/Panel/LevelLabel/DailyTab").GetComponent<Button>();
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
                "Quest Panel (CP3)",
                (existing == null ? "Quest panel created" : "Existing quest panel rebuilt") +
                " as a separate canvas (sortingOrder " + PanelSortingOrder + ") and prefab saved at " +
                PrefabPath + ".\n\nThe hamburger menu's QUESTS row opens and closes it." +
                fontWarning + catWarning,
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

    // ----- Panel -----

    private static PanelParts CreatePanel(Transform parent, TMP_FontAsset font, out List<RowData> rowData)
    {
        GameObject panelRoot = CreateRect("Panel", parent);
        RectTransform rect = panelRoot.GetComponent<RectTransform>();
        // Centred in the safe area on both axes; the controller only ever changes
        // the width and the uniform fit-scale.
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(FallbackPanelWidth, PanelHeight);

        CanvasGroup group = GetOrAdd<CanvasGroup>(panelRoot);
        group.alpha = 0f;

        var face=U.Panel("Face",panelRoot.transform,PremiumUiStyle.Ivory,0,0,FallbackPanelWidth,PanelHeight,32,true);
        U.Fill(face.rectTransform);
        Button closeButton = CreateHeader(panelRoot.transform, font);
        TMP_Text levelLabel = CreateLevelLabel(panelRoot.transform, font);
        MessageParts message = CreateMessage(panelRoot.transform, font);
        ScrollParts scroll = CreateScrollView(panelRoot.transform, font, out rowData);
        TMP_Text walletLabel = CreateFooter(panelRoot.transform, font);

        return new PanelParts(
            rect,
            group,
            closeButton,
            levelLabel,
            walletLabel,
            message.Root,
            message.Label,
            scroll.Root,
            scroll.ScrollRect
        );
    }

    // ----- Header (title bar + close button), identical recipe to the shop -----

    private static Button CreateHeader(Transform parent, TMP_FontAsset font)
    {
        GameObject headerRoot = CreateRect("Header", parent);
        AnchorTop(headerRoot.GetComponent<RectTransform>(), HeaderTop, HeaderHeight, HeaderInset * 2f);

        var title=U.Label("Title",headerRoot.transform,font,44,PremiumUiStyle.Ink,0,0,1,1);
        U.Fill(title.rectTransform); title.rectTransform.offsetMin=new Vector2(42,8); title.rectTransform.offsetMax=new Vector2(-100,-8);
        U.Localize(title,"quests.title");
        var close=U.Action("CloseButton",headerRoot.transform,font,null,PremiumUiStyle.WarmIvory,0,0,58,58,out var label);
        var rect=(RectTransform)close.transform; rect.anchorMin=rect.anchorMax=new Vector2(1,.5f); rect.anchoredPosition=new Vector2(-56,0);
        label.text="×"; label.fontSize=34; return close;
    }

    private static void CreateHeaderTitle(
        Transform parent, string name, TMP_FontAsset font, Color color, Vector2 offset)
    {
        TMP_Text title = CreateText(parent, name, font, 42f, color, TextAlignmentOptions.MidlineLeft);
        title.text = "QUESTS";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 6f;
        // Inset symmetrically so the close button on the right does not push the
        // title off the header's optical centre.
        StretchWithOffsets(
            title.rectTransform,
            116f + offset.x,
            offset.y,
            -(CloseButtonSize + 24f) + offset.x,
            offset.y);
    }

    private static Button CreateCloseButton(Transform parent)
    {
        GameObject buttonRoot = CreateRect("CloseButton", parent);
        RectTransform rect = buttonRoot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);
        rect.anchoredPosition = new Vector2(-16f, 0f);

        GameObject face = CreateRect("Face", buttonRoot.transform);
        Stretch(face.GetComponent<RectTransform>());
        EnsureCanvasRenderer(face);
        LowPolyPanelGraphic faceGraphic = face.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(faceGraphic,
            PremiumUiStyle.CoralLift, PremiumUiStyle.CandyPink, 24f, 4f);
        faceGraphic.raycastTarget = true;
        ConfigurePanel(faceGraphic, 12f, 4f);

        IconRect(face.transform, "CrossA", Color.white, 28f, 5f, 0f, 0f, 45f);
        IconRect(face.transform, "CrossB", Color.white, 28f, 5f, 0f, 0f, -45f);

        return MakeButton(buttonRoot, faceGraphic, face.GetComponent<RectTransform>());
    }

    private static void BuildQuestsEmblem(Transform parent)
    {
        IconRect(parent, "Board", FrameOrange, 34f, 42f, 0f, -3f);
        IconRect(parent, "Clip", FrameOrange, 18f, 5f, 0f, 21f);
        IconRect(parent, "LineA", PremiumUiStyle.ChampagneLight, 18f, 3f, 3f, 7f);
        IconRect(parent, "LineB", PremiumUiStyle.ChampagneLight, 18f, 3f, 3f, -2f);
        IconRect(parent, "TickA", PremiumUiStyle.Teal, 7f, 3f, -9f, 7f, -42f);
        IconRect(parent, "TickB", PremiumUiStyle.Teal, 10f, 3f, -5f, 10f, 42f);
    }

    // ----- Level heading -----

    private static TMP_Text CreateLevelLabel(Transform parent, TMP_FontAsset font)
    {
        GameObject labelRoot = CreateRect("LevelLabel", parent);
        AnchorTop(labelRoot.GetComponent<RectTransform>(), LevelLabelTop, LevelLabelHeight, PanelSidePadding);

        var chapter=U.Action("ChapterTab",labelRoot.transform,font,"quests.chapter_tab",PremiumUiStyle.Teal,0,0,208,58,out var chapterText);
        chapterText.color=Color.white; chapterText.fontSize=23; chapterText.textWrappingMode=TextWrappingModes.NoWrap;
        var daily=U.Action("DailyTab",labelRoot.transform,font,"quests.daily_tab",PremiumUiStyle.Mint,0,0,208,58,out var dailyText);
        foreach(var button in new[]{chapter,daily}) { var r=(RectTransform)button.transform; r.anchorMin=r.anchorMax=new Vector2(0,.5f); }
        ((RectTransform)chapter.transform).anchoredPosition=new Vector2(104,0);
        ((RectTransform)daily.transform).anchoredPosition=new Vector2(328,0);
        var label=U.Label("ChapterLabel",labelRoot.transform,font,22,PremiumUiStyle.Muted,0,0,1,1,TextAlignmentOptions.Right);
        U.Fill(label.rectTransform); label.rectTransform.offsetMin=new Vector2(464,0);
        return label;
    }

    // ----- Empty / completed message (covers the list area) -----

    private static MessageParts CreateMessage(Transform parent, TMP_FontAsset font)
    {
        GameObject messageRoot = CreateRect("Message", parent);
        AnchorTop(messageRoot.GetComponent<RectTransform>(), BodyTop, BodyHeight, PanelSidePadding);

        TMP_Text label = AddWrappingText(
            messageRoot, font, 32f, 20f, PremiumUiStyle.Ink, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        label.text = "All levels completed";

        // Hidden by default; the controller shows exactly one of Message / ScrollView.
        messageRoot.SetActive(false);
        return new MessageParts(messageRoot, label);
    }

    // ----- Scrolling quest list -----

    private static ScrollParts CreateScrollView(
        Transform parent, TMP_FontAsset font, out List<RowData> rowData)
    {
        GameObject scrollRoot = CreateRect("ScrollView", parent);
        AnchorTop(scrollRoot.GetComponent<RectTransform>(), BodyTop, BodyHeight, PanelSidePadding);

        GameObject viewport = CreateRect("Viewport", scrollRoot.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        // RectMask2D clips without needing a Graphic, so the viewport adds no
        // extra draw call and no raycast target of its own.
        viewport.AddComponent<RectMask2D>();

        GameObject content = CreateRect("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);
        contentRect.sizeDelta = new Vector2(0f, BodyHeight);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(content);
        layout.spacing = RowSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperCenter;
        // Rows fill the list width and keep their authored height, so the stack
        // stays even whatever width the controller settles on.
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        // The single fitter in the panel: it grows the content to exactly the
        // active rows, so the list scrolls only when a level has more quests than
        // fit and sits flush at the top when it does not.
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = GetOrAdd<ScrollRect>(scrollRoot);
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.horizontalScrollbar = null;
        scrollRect.verticalScrollbar = null;

        rowData = new List<RowData>(RowPoolCount);
        for (int i = 0; i < RowPoolCount; i++)
            rowData.Add(CreateRow(content.transform, i, font));

        return new ScrollParts(scrollRoot, scrollRect);
    }

    private static RowData CreateRow(Transform parent, int index, TMP_FontAsset font)
    {
        GameObject rowRoot = CreateRect("QuestRow_" + index, parent);

        LayoutElement layoutElement = GetOrAdd<LayoutElement>(rowRoot);
        layoutElement.minHeight = RowHeight;
        layoutElement.preferredHeight = RowHeight;
        layoutElement.flexibleHeight = 0f;

        CanvasGroup group = GetOrAdd<CanvasGroup>(rowRoot);
        group.alpha = 0f;

        var face=U.Panel("Face",rowRoot.transform,PremiumUiStyle.WarmIvory,0,0,1,1,22,true);
        U.Fill(face.rectTransform);
        // Two columns: a flexible info column and a fixed-width action column, so
        // the state label and the Claim button always sit at the same x on every
        // row and the info text keeps whatever width is left.
        GameObject content = CreateRect("Content", rowRoot.transform);
        StretchWithOffsets(
            content.GetComponent<RectTransform>(),
            RowContentPaddingX,
            RowContentPaddingY,
            -RowContentPaddingX,
            -RowContentPaddingY);

        HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(content);
        hlg.spacing = RowColumnGap;
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        InfoParts info = CreateInfoColumn(content.transform, font);
        ActionParts action = CreateActionColumn(content.transform, font);

        return new RowData(
            rowRoot,
            group,
            info.Title,
            info.Description,
            info.Progress,
            info.Rewards,
            action.Status,
            action.ClaimRoot,
            action.ClaimButton
        );
    }

    private static InfoParts CreateInfoColumn(Transform parent, TMP_FontAsset font)
    {
        GameObject infoRoot = CreateRect("Info", parent);

        LayoutElement infoLayout = GetOrAdd<LayoutElement>(infoRoot);
        // Zero preferred + flexible 1 makes the info column exactly "whatever is
        // left after the fixed action column", independent of the text it holds.
        infoLayout.minWidth = 0f;
        infoLayout.preferredWidth = 0f;
        infoLayout.flexibleWidth = 1f;

        VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(infoRoot);
        vlg.spacing = InfoRowSpacing;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Every text below wraps and auto-shrinks inside a fixed box with an
        // ellipsis fallback, so an unusually long title or description can shrink
        // and finally truncate but can never push another line out of the row.
        TMP_Text title = CreateFixedHeightText(
            infoRoot.transform, "Title", font, 30f, 21f, TitleHeight,
            PremiumUiStyle.Ink, TextAlignmentOptions.TopLeft);
        title.fontStyle = FontStyles.Bold;
        title.text = "Quest title";

        TMP_Text description = CreateFixedHeightText(
            infoRoot.transform, "Description", font, 23f, 16f, DescriptionHeight,
            new Color32(79, 80, 78, 255), TextAlignmentOptions.TopLeft);
        description.text = "Quest description";

        GameObject meta = CreateRect("Meta", infoRoot.transform);
        LayoutElement metaLayout = GetOrAdd<LayoutElement>(meta);
        metaLayout.minHeight = MetaHeight;
        metaLayout.preferredHeight = MetaHeight;
        metaLayout.flexibleHeight = 0f;

        HorizontalLayoutGroup metaGroup = GetOrAdd<HorizontalLayoutGroup>(meta);
        metaGroup.spacing = 12f;
        metaGroup.padding = new RectOffset(0, 0, 0, 0);
        metaGroup.childAlignment = TextAnchor.MiddleLeft;
        metaGroup.childControlWidth = true;
        metaGroup.childControlHeight = true;
        metaGroup.childForceExpandWidth = false;
        metaGroup.childForceExpandHeight = true;

        TMP_Text progress = CreateText(
            meta.transform, "Progress", font, 26f, PremiumUiStyle.Teal, TextAlignmentOptions.MidlineLeft);
        progress.fontStyle = FontStyles.Bold;
        progress.text = "0/1";
        LayoutElement progressLayout = GetOrAdd<LayoutElement>(progress.gameObject);
        progressLayout.minWidth = ProgressWidth;
        progressLayout.preferredWidth = ProgressWidth;
        progressLayout.flexibleWidth = 0f;

        TMP_Text rewards = CreateText(
            meta.transform, "Rewards", font, 22f, PremiumUiStyle.Ink, TextAlignmentOptions.MidlineLeft);
        rewards.text = "+10 COINS   +5 BOND XP";
        ConfigureAutoSize(rewards, 22f, 14f);
        LayoutElement rewardsLayout = GetOrAdd<LayoutElement>(rewards.gameObject);
        rewardsLayout.minWidth = 0f;
        rewardsLayout.preferredWidth = 0f;
        rewardsLayout.flexibleWidth = 1f;

        return new InfoParts(title, description, progress, rewards);
    }

    private static ActionParts CreateActionColumn(Transform parent, TMP_FontAsset font)
    {
        GameObject actionRoot = CreateRect("Action", parent);

        LayoutElement actionLayout = GetOrAdd<LayoutElement>(actionRoot);
        // Fixed on both bounds so the column never shrinks below the Claim
        // button's touch target, whatever the info text does.
        actionLayout.minWidth = ActionColumnWidth;
        actionLayout.preferredWidth = ActionColumnWidth;
        actionLayout.flexibleWidth = 0f;

        // The status label and the Claim button share the same centred slot; the
        // controller shows exactly one of them, so they can never overlap.
        TMP_Text status = CreateText(
            actionRoot.transform, "Status", font, 24f, PremiumUiStyle.Ink, TextAlignmentOptions.Center);
        status.fontStyle = FontStyles.Bold;
        status.characterSpacing = 2f;
        status.text = "IN PROGRESS";
        status.textWrappingMode = TextWrappingModes.Normal;
        ConfigureAutoSize(status, 24f, 15f);
        Stretch(status.rectTransform);

        GameObject claimRoot = CreateRect("ClaimButton", actionRoot.transform);
        RectTransform claimRect = claimRoot.GetComponent<RectTransform>();
        claimRect.anchorMin = claimRect.anchorMax = new Vector2(0.5f, 0.5f);
        claimRect.pivot = new Vector2(0.5f, 0.5f);
        claimRect.sizeDelta = new Vector2(ClaimButtonWidth, ClaimButtonHeight);
        claimRect.anchoredPosition = Vector2.zero;

        GameObject claimFace = CreateRect("Face", claimRoot.transform);
        Stretch(claimFace.GetComponent<RectTransform>());
        EnsureCanvasRenderer(claimFace);
        LowPolyPanelGraphic claimFaceGraphic = claimFace.AddComponent<LowPolyPanelGraphic>();
        // Green reads as the one affirmative action in an otherwise orange/cream
        // panel, so the claimable row is unmistakable.
        claimFaceGraphic.color = PremiumUiStyle.Coral;
        claimFaceGraphic.raycastTarget = true;
        PremiumUiStyle.ConfigureSurface(claimFaceGraphic, PremiumUiStyle.Coral, 18f, 1f);

        TMP_Text claimLabel = CreateText(
            claimFace.transform, "Label", font, 24f, PremiumUiStyle.Ink, TextAlignmentOptions.Center);
        claimLabel.fontStyle = FontStyles.Bold;
        claimLabel.characterSpacing = 0.6f;
        claimLabel.text = "CLAIM";
        U.Localize(claimLabel,"quests.claim");
        Stretch(claimLabel.rectTransform);

        Button claimButton = MakeButton(claimRoot, claimFaceGraphic, claimFace.GetComponent<RectTransform>());

        // Authored hidden: only a Completed quest ever shows a Claim button, and
        // the controller decides that on every refresh.
        claimRoot.SetActive(false);

        return new ActionParts(status, claimRoot, claimButton);
    }

    // ----- Footer (wallet readout) -----

    private static TMP_Text CreateFooter(Transform parent, TMP_FontAsset font)
    {
        GameObject footerRoot = CreateRect("Footer", parent);
        RectTransform rect = footerRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(-PanelSidePadding * 2f, FooterHeight);
        rect.anchoredPosition = new Vector2(0f, PanelBottomPadding);

        LowPolyPanelGraphic bar = CreatePanelGraphic("Bar", footerRoot.transform, PremiumUiStyle.Mint, 16f, 3f, false);
        Stretch(bar.rectTransform);

        TMP_Text label = CreateText(
            footerRoot.transform, "Label", font, 18f, PremiumUiStyle.Ink, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        label.text = "Coins: 0    Bond XP: 0    Diamonds: 0";
        ConfigureAutoSize(label, 26f, 15f);
        StretchWithOffsets(label.rectTransform, 14f, 0f, -14f, 0f);
        return label;
    }

    // ----- Text helpers -----

    /// <summary>
    /// A wrapping, auto-shrinking label that fills its parent rect. Used where the
    /// text length is data-driven and the box is fixed.
    /// </summary>
    private static TMP_Text AddWrappingText(
        GameObject host, TMP_FontAsset font, float size, float minSize, Color color, TextAlignmentOptions alignment)
    {
        EnsureCanvasRenderer(host);
        TextMeshProUGUI text = host.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        ConfigureAutoSize(text, size, minSize);
        return text;
    }

    private static TMP_Text CreateFixedHeightText(
        Transform parent,
        string name,
        TMP_FontAsset font,
        float size,
        float minSize,
        float height,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject host = CreateRect(name, parent);
        TMP_Text text = AddWrappingText(host, font, size, minSize, color, alignment);

        LayoutElement layout = GetOrAdd<LayoutElement>(host);
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
        return text;
    }

    private static void ConfigureAutoSize(TMP_Text text, float max, float min)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = min;
        // Last line of defence: after shrinking to fontSizeMin, extra text is cut
        // with an ellipsis instead of spilling outside the row.
        text.overflowMode = TextOverflowModes.Truncate;
    }

    // ----- Button helper (same recipe as the menu and shop buttons) -----

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
        // A pressed Claim button is disabled for the length of the claim; the
        // dimmed disabled tint is the visible confirmation that the tap landed.
        colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.6f);
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

        RemoveDuplicateComponents<QuestPanelController>(root);
        RemoveDuplicateComponents<GraphicRaycaster>(root);
        RemoveDuplicateComponents<CanvasScaler>(root);
        RemoveDuplicateComponents<Canvas>(root);
        RemoveDuplicateComponents<CanvasGroup>(root);
        return root;
    }

    // ----- Primitive builders (mirror ShopPanelBuilder) -----

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
        return text;
    }

    private static void IconRect(
        Transform parent, string name, Color color, float w, float h, float x, float y, float rotation = 0f)
    {
        GameObject gameObject = CreateRect(name, parent);
        EnsureCanvasRenderer(gameObject);
        Image image = gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w, h);
        rect.anchoredPosition = new Vector2(x, y);
        if (!Mathf.Approximately(rotation, 0f))
            rect.localEulerAngles = new Vector3(0f, 0f, rotation);
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
                    "Quest Panel: a button has no valid CanvasRenderer-backed targetGraphic (" +
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

    /// <summary>
    /// Pins a full-width band to the panel's top edge: <paramref name="topOffset"/>
    /// is the distance from that edge down to the band's top. Sections are placed
    /// this way (instead of with a layout group) so the authored panel height and
    /// the section positions come from the same constants.
    /// </summary>
    private static void AnchorTop(RectTransform rect, float topOffset, float height, float sidePadding)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-sidePadding * 2f, height);
        rect.anchoredPosition = new Vector2(0f, -topOffset);
    }

    private static TMP_FontAsset FindFont()
    {
        TMP_FontAsset premium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        if (premium != null)
            return premium;
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text != null && text.font != null)
                return text.font;
        }
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-SemiBold SDF.asset");
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
            element.FindPropertyRelative("root").objectReferenceValue = rowData[i].Root;
            element.FindPropertyRelative("group").objectReferenceValue = rowData[i].Group;
            element.FindPropertyRelative("title").objectReferenceValue = rowData[i].Title;
            element.FindPropertyRelative("description").objectReferenceValue = rowData[i].Description;
            element.FindPropertyRelative("progress").objectReferenceValue = rowData[i].Progress;
            element.FindPropertyRelative("rewards").objectReferenceValue = rowData[i].Rewards;
            element.FindPropertyRelative("status").objectReferenceValue = rowData[i].Status;
            element.FindPropertyRelative("claimRoot").objectReferenceValue = rowData[i].ClaimRoot;
            element.FindPropertyRelative("claimButton").objectReferenceValue = rowData[i].ClaimButton;
        }
    }

    private static void EnsureFolder()
    {
        if (!Directory.Exists(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "UI");
    }

    private readonly struct PanelParts
    {
        public PanelParts(
            RectTransform panel,
            CanvasGroup group,
            Button closeButton,
            TMP_Text levelLabel,
            TMP_Text walletLabel,
            GameObject messageRoot,
            TMP_Text messageLabel,
            GameObject scrollRoot,
            ScrollRect scrollRect)
        {
            Panel = panel;
            Group = group;
            CloseButton = closeButton;
            LevelLabel = levelLabel;
            WalletLabel = walletLabel;
            MessageRoot = messageRoot;
            MessageLabel = messageLabel;
            ScrollRoot = scrollRoot;
            ScrollRect = scrollRect;
        }

        public RectTransform Panel { get; }
        public CanvasGroup Group { get; }
        public Button CloseButton { get; }
        public TMP_Text LevelLabel { get; }
        public TMP_Text WalletLabel { get; }
        public GameObject MessageRoot { get; }
        public TMP_Text MessageLabel { get; }
        public GameObject ScrollRoot { get; }
        public ScrollRect ScrollRect { get; }
    }

    private readonly struct MessageParts
    {
        public MessageParts(GameObject root, TMP_Text label)
        {
            Root = root;
            Label = label;
        }

        public GameObject Root { get; }
        public TMP_Text Label { get; }
    }

    private readonly struct ScrollParts
    {
        public ScrollParts(GameObject root, ScrollRect scrollRect)
        {
            Root = root;
            ScrollRect = scrollRect;
        }

        public GameObject Root { get; }
        public ScrollRect ScrollRect { get; }
    }

    private readonly struct InfoParts
    {
        public InfoParts(TMP_Text title, TMP_Text description, TMP_Text progress, TMP_Text rewards)
        {
            Title = title;
            Description = description;
            Progress = progress;
            Rewards = rewards;
        }

        public TMP_Text Title { get; }
        public TMP_Text Description { get; }
        public TMP_Text Progress { get; }
        public TMP_Text Rewards { get; }
    }

    private readonly struct ActionParts
    {
        public ActionParts(TMP_Text status, GameObject claimRoot, Button claimButton)
        {
            Status = status;
            ClaimRoot = claimRoot;
            ClaimButton = claimButton;
        }

        public TMP_Text Status { get; }
        public GameObject ClaimRoot { get; }
        public Button ClaimButton { get; }
    }

    private readonly struct RowData
    {
        public RowData(
            GameObject root,
            CanvasGroup group,
            TMP_Text title,
            TMP_Text description,
            TMP_Text progress,
            TMP_Text rewards,
            TMP_Text status,
            GameObject claimRoot,
            Button claimButton)
        {
            Root = root;
            Group = group;
            Title = title;
            Description = description;
            Progress = progress;
            Rewards = rewards;
            Status = status;
            ClaimRoot = claimRoot;
            ClaimButton = claimButton;
        }

        public GameObject Root { get; }
        public CanvasGroup Group { get; }
        public TMP_Text Title { get; }
        public TMP_Text Description { get; }
        public TMP_Text Progress { get; }
        public TMP_Text Rewards { get; }
        public TMP_Text Status { get; }
        public GameObject ClaimRoot { get; }
        public Button ClaimButton { get; }
    }
}
