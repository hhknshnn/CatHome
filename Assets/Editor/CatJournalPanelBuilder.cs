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
/// Authors the premium CAT JOURNAL customisation panel in CatHome_UI: name the cat
/// and pick a coat colour. Opened from the hamburger's CAT JOURNAL row; the runtime
/// <see cref="CatJournalPanel"/> owns state and persists through
/// <see cref="CatIdentityService"/>.
/// </summary>
public static class CatJournalPanelBuilder
{
    public const string RootName = "CatJournalPanelCanvas";
    public const string PrefabPath = "Assets/UI/CatJournalPanel.prefab";
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";

    private static readonly Color Scrim = new Color32(44, 27, 67, 174);
    private static readonly Color Ink = PremiumUiStyle.Ink;
    private static readonly Color Cream = PremiumUiStyle.CandyCloud;
    private static readonly Color CatGrey = new Color(0.62f, 0.6f, 0.58f);

    [MenuItem("Tools/Cat Home/UI/Build Cat Journal Panel")]
    public static void BuildFromMenu()
    {
        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home", result, "OK");
    }

    public static string BuildSilently()
    {
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
        canvas.sortingOrder = 117;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAdd<GraphicRaycaster>(root);

        CatJournalPanel controller = GetOrAdd<CatJournalPanel>(root);
        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        RectTransform safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea);
        GetOrAdd<SafeAreaRect>(safeArea.gameObject);

        Image scrim = CreateImage("JournalScrim", safeArea, Scrim, true);
        Stretch(scrim.rectTransform);
        Button scrimButton = GetOrAdd<Button>(scrim.gameObject);
        scrimButton.targetGraphic = scrim;
        scrimButton.transition = Selectable.Transition.None;

        RectTransform glow = CreateRect("AquaGlow", safeArea);
        SetCentered(glow, new Vector2(1180f, 800f), new Vector2(0f, -10f));
        LowPolyPanelGraphic glowGraphic = AddPanel(glow.gameObject,
            new Color32(53, 231, 216, 88), 48f, 10f, false);
        PremiumUiStyle.ConfigureShadowSurface(glowGraphic,
            new Color32(45, 220, 209, 96), 48f);

        RectTransform depth = CreateRect("CoralDepth", safeArea);
        SetCentered(depth, new Vector2(1156f, 772f), Vector2.zero);
        AddPanel(depth.gameObject, PremiumUiStyle.CoralLift, 44f, 10f, false);

        RectTransform panel = CreateRect("CatJournalPanelVisual", safeArea);
        SetCentered(panel, new Vector2(1140f, 752f), Vector2.zero);
        CanvasGroup panelGroup = GetOrAdd<CanvasGroup>(panel.gameObject);
        LowPolyPanelGraphic panelFace = AddPanel(panel.gameObject, Cream, 44f, 14f, true);
        PremiumUiStyle.ConfigureAccentSurface(
            panelFace,
            new Color32(255, 239, 191, 255),
            new Color32(211, 249, 234, 255),
            44f, 14f);

        BuildHeader(panel, font);
        BuildCatPortrait(panel, font, out List<Graphic> previewParts,
            out List<Graphic> previewAccents);
        TMP_Text namePreview = BuildNamePreview(panel, font);
        TMP_InputField nameInput = BuildNameRow(panel, font);
        TMP_Text coatNameText = BuildCoatLabel(panel, font);
        List<Swatch> swatches = BuildSwatchRow(panel, font);
        BuildIdentityHint(panel, font);
        Button closeButton = BuildCloseButton(panel, font);
        BuildSparkle(panel, "SparkleLeft", new Vector2(-520f, 330f), 16f,
            PremiumUiStyle.CandyLemon, 18f);
        BuildSparkle(panel, "SparkleRight", new Vector2(520f, -330f), 14f,
            PremiumUiStyle.CandyPink, -12f);

        var serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", null);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "safeArea", safeArea);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "panelVisual", panel);
        Assign(serialized, "panelGroup", panelGroup);
        Assign(serialized, "closeButton", closeButton);
        Assign(serialized, "nameInput", nameInput);
        Assign(serialized, "namePreview", namePreview);
        Assign(serialized, "coatNameText", coatNameText);
        AssignGraphics(serialized, "coatPreviewParts", previewParts);
        AssignGraphics(serialized, "coatPreviewAccentParts", previewAccents);
        AssignSwatches(serialized, swatches);
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
        return "Premium Cat Journal panel built in CatHome_UI.";
    }

    private static void BuildHeader(RectTransform panel, TMP_FontAsset font)
    {
        LowPolyPanelGraphic badge = CreatePanel("JournalBadge", panel,
            PremiumUiStyle.CandyPink, 26f, 8f, false);
        SetCentered(badge.rectTransform, new Vector2(150f, 48f), new Vector2(-430f, 322f));
        TMP_Text badgeText = CreateText("BadgeText", badge.transform, font, 20f,
            Color.white, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform);
        badgeText.text = "CAT";
        badgeText.fontStyle = FontStyles.Bold;

        TMP_Text title = CreateText("Title", panel, font, 44f, Ink,
            TextAlignmentOptions.Center);
        SetCentered(title.rectTransform, new Vector2(640f, 58f), new Vector2(0f, 322f));
        title.text = "CAT JOURNAL";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 2.2f;

        TMP_Text subtitle = CreateText("Subtitle", panel, font, 20f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(subtitle.rectTransform, new Vector2(900f, 40f), new Vector2(0f, 280f));
        subtitle.text = "NAME YOUR CAT & PICK A COAT";
        subtitle.fontStyle = FontStyles.Bold;
        subtitle.characterSpacing = 1.1f;
        subtitle.overflowMode = TextOverflowModes.Truncate;
    }

    private static TMP_Text BuildNamePreview(RectTransform panel, TMP_FontAsset font)
    {
        LowPolyPanelGraphic well = CreatePanel("NamePreviewWell", panel,
            PremiumUiStyle.CandyCloud, 26f, 7f, false);
        SetCentered(well.rectTransform, new Vector2(600f, 96f), new Vector2(218f, 176f));
        PremiumUiStyle.ConfigureAccentSurface(well,
            new Color32(255, 244, 210, 255), new Color32(255, 226, 236, 255), 26f, 7f);
        TMP_Text preview = CreateText("NamePreview", well.transform, font, 52f,
            Ink, TextAlignmentOptions.Center);
        Stretch(preview.rectTransform);
        preview.text = "MELO";
        preview.fontStyle = FontStyles.Bold;
        preview.characterSpacing = 2f;
        preview.overflowMode = TextOverflowModes.Truncate;
        return preview;
    }

    private static TMP_InputField BuildNameRow(RectTransform panel, TMP_FontAsset font)
    {
        TMP_Text label = CreateText("NameLabel", panel, font, 20f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(label.rectTransform, new Vector2(400f, 30f), new Vector2(218f, 96f));
        label.text = "TAP TO RENAME";
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 1.1f;

        LowPolyPanelGraphic well = CreatePanel("NameInputWell", panel,
            Color.white, 22f, 6f, true);
        SetCentered(well.rectTransform, new Vector2(560f, 72f), new Vector2(218f, 44f));
        PremiumUiStyle.ConfigureAccentSurface(well,
            new Color32(255, 255, 255, 255), new Color32(233, 246, 255, 255), 22f, 6f);

        RectTransform textArea = CreateRect("TextArea", well.transform);
        StretchWithOffsets(textArea, 24f, 8f, -24f, -8f);
        GetOrAdd<RectMask2D>(textArea.gameObject);

        TMP_Text placeholder = CreateText("Placeholder", textArea, font, 30f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        Stretch(placeholder.rectTransform);
        placeholder.text = "NAME YOUR CAT";
        placeholder.fontStyle = FontStyles.Italic;

        TMP_Text text = CreateText("Text", textArea, font, 30f, Ink,
            TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 1.4f;

        TMP_InputField input = GetOrAdd<TMP_InputField>(well.gameObject);
        input.targetGraphic = well;
        input.textViewport = textArea;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.fontAsset = font;
        input.pointSize = 30f;
        input.characterLimit = CatIdentityService.MaxNameLength;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.caretWidth = 3;
        input.customCaretColor = true;
        input.caretColor = PremiumUiStyle.CandyBerry;
        input.selectionColor = new Color32(120, 220, 210, 150);
        input.onFocusSelectAll = true;
        input.text = CatIdentityService.CatName;
        return input;
    }

    private static TMP_Text BuildCoatLabel(RectTransform panel, TMP_FontAsset font)
    {
        TMP_Text label = CreateText("CoatLabel", panel, font, 24f, Ink,
            TextAlignmentOptions.Center);
        SetCentered(label.rectTransform, new Vector2(600f, 34f), new Vector2(218f, -58f));
        label.text = "CLASSIC COAT";
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 1.4f;
        return label;
    }

    private const float SwatchSize = 72f;
    private const float SwatchSpacing = 12f;

    private static List<Swatch> BuildSwatchRow(RectTransform panel, TMP_FontAsset font)
    {
        int count = CatIdentityService.CoatCount;
        var swatches = new List<Swatch>(count);
        float totalWidth = count * SwatchSize + (count - 1) * SwatchSpacing;
        float startX = -totalWidth * 0.5f + SwatchSize * 0.5f;
        for (int i = 0; i < count; i++)
        {
            CatIdentityService.CatCoat coat = CatIdentityService.Palette[i];
            Color display = Color.Lerp(coat.Tint, CatGrey, 0.4f);
            float x = startX + i * (SwatchSize + SwatchSpacing);

            RectTransform swatchRoot = CreateRect("Swatch_" + i, panel);
            SetCentered(swatchRoot, new Vector2(SwatchSize, SwatchSize),
                new Vector2(x + 218f, -134f));

            // Selection ring sits behind, slightly larger, and toggles on select.
            LowPolyPanelGraphic ring = CreatePanel("SelectionRing", swatchRoot.transform,
                PremiumUiStyle.CandyBerry, 30f, 7f, false);
            SetCentered(ring.rectTransform, new Vector2(SwatchSize + 16f, SwatchSize + 16f),
                Vector2.zero);

            LowPolyPanelGraphic face = CreatePanel("SwatchFace", swatchRoot.transform,
                display, 26f, 8f, true);
            Stretch(face.rectTransform);
            PremiumUiStyle.ConfigureAccentSurface(
                face, Color.Lerp(display, Color.white, 0.18f), display, 26f, 8f);

            Button button = GetOrAdd<Button>(swatchRoot.gameObject);
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;

            ring.gameObject.SetActive(i == CatIdentityService.CoatIndex);
            swatches.Add(new Swatch(button, ring.gameObject));
        }
        return swatches;
    }

    private static void BuildIdentityHint(RectTransform panel, TMP_FontAsset font)
    {
        LowPolyPanelGraphic hint = CreatePanel("IdentityHint", panel,
            new Color32(255, 226, 236, 255), 22f, 5f, false);
        SetCentered(hint.rectTransform, new Vector2(650f, 74f), new Vector2(218f, -244f));
        TMP_Text copy = CreateText("HintCopy", hint.transform, font, 18f, Ink,
            TextAlignmentOptions.Center);
        StretchWithOffsets(copy.rectTransform, 20f, 6f, -20f, -6f);
        copy.text = "YOUR NAME & COAT FOLLOW YOU THROUGH EVERY ROOM AND GAME";
        copy.fontStyle = FontStyles.Bold;
        copy.characterSpacing = .7f;
        copy.enableAutoSizing = true;
        copy.fontSizeMin = 14f;
        copy.fontSizeMax = 18f;
    }

    private static void BuildCatPortrait(RectTransform panel, TMP_FontAsset font,
        out List<Graphic> coatParts, out List<Graphic> accentParts)
    {
        coatParts = new List<Graphic>();
        accentParts = new List<Graphic>();

        LowPolyPanelGraphic glow = CreatePanel("PortraitGlow", panel,
            PremiumUiStyle.CandyLemon, 44f, 9f, false);
        SetCentered(glow.rectTransform, new Vector2(342f, 424f), new Vector2(-376f, -22f));
        LowPolyPanelGraphic frame = CreatePanel("CatPortraitFrame", panel,
            PremiumUiStyle.CandyCloud, 40f, 10f, false);
        SetCentered(frame.rectTransform, new Vector2(326f, 408f), new Vector2(-376f, -22f));
        PremiumUiStyle.ConfigureAccentSurface(frame,
            new Color32(213, 249, 239, 255), new Color32(174, 226, 244, 255), 40f, 10f);

        TMP_Text eyebrow = CreateText("PortraitEyebrow", frame.transform, font, 17f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(eyebrow.rectTransform, new Vector2(260f, 28f), new Vector2(0f, 160f));
        eyebrow.text = "YOUR COZY COMPANION";
        eyebrow.fontStyle = FontStyles.Bold;
        eyebrow.characterSpacing = 1f;

        Color coat = Color.Lerp(CatIdentityService.CurrentTint, CatGrey, 0.34f);
        Color stripe = Color.Lerp(coat, Ink, 0.34f);

        LowPolyPanelGraphic body = CreatePanel("PortraitBody", frame.transform,
            coat, 68f, 7f, false);
        SetCentered(body.rectTransform, new Vector2(174f, 182f), new Vector2(0f, -72f));
        coatParts.Add(body);

        LowPolyPanelGraphic earLeft = CreatePanel("EarLeft", frame.transform,
            coat, 16f, 5f, false);
        SetCentered(earLeft.rectTransform, new Vector2(78f, 78f), new Vector2(-67f, 67f));
        earLeft.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        coatParts.Add(earLeft);
        LowPolyPanelGraphic earRight = CreatePanel("EarRight", frame.transform,
            coat, 16f, 5f, false);
        SetCentered(earRight.rectTransform, new Vector2(78f, 78f), new Vector2(67f, 67f));
        earRight.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        coatParts.Add(earRight);

        LowPolyPanelGraphic innerLeft = CreatePanel("InnerEarLeft", earLeft.transform,
            PremiumUiStyle.CandyPink, 12f, 4f, false);
        SetCentered(innerLeft.rectTransform, new Vector2(42f, 42f), Vector2.zero);
        LowPolyPanelGraphic innerRight = CreatePanel("InnerEarRight", earRight.transform,
            PremiumUiStyle.CandyPink, 12f, 4f, false);
        SetCentered(innerRight.rectTransform, new Vector2(42f, 42f), Vector2.zero);

        LowPolyPanelGraphic head = CreatePanel("PortraitHead", frame.transform,
            coat, 72f, 8f, false);
        SetCentered(head.rectTransform, new Vector2(210f, 172f), new Vector2(0f, 45f));
        coatParts.Add(head);

        for (int i = -1; i <= 1; i++)
        {
            Image mark = CreateImage("ForeheadStripe_" + (i + 2), head.transform,
                stripe, false);
            SetCentered(mark.rectTransform, new Vector2(12f, 52f),
                new Vector2(i * 22f, 47f));
            mark.rectTransform.localEulerAngles = new Vector3(0f, 0f, i * -10f);
            accentParts.Add(mark);
        }

        BuildPortraitEye(head.transform, -44f);
        BuildPortraitEye(head.transform, 44f);
        LowPolyPanelGraphic muzzleLeft = CreatePanel("MuzzleLeft", head.transform,
            Cream, 28f, 4f, false);
        SetCentered(muzzleLeft.rectTransform, new Vector2(70f, 54f), new Vector2(-30f, -32f));
        LowPolyPanelGraphic muzzleRight = CreatePanel("MuzzleRight", head.transform,
            Cream, 28f, 4f, false);
        SetCentered(muzzleRight.rectTransform, new Vector2(70f, 54f), new Vector2(30f, -32f));
        LowPolyPanelGraphic nose = CreatePanel("Nose", head.transform,
            PremiumUiStyle.CandyPink, 14f, 3f, false);
        SetCentered(nose.rectTransform, new Vector2(34f, 25f), new Vector2(0f, -26f));

        LowPolyPanelGraphic collar = CreatePanel("Collar", frame.transform,
            PremiumUiStyle.CandyAqua, 18f, 5f, false);
        SetCentered(collar.rectTransform, new Vector2(184f, 34f), new Vector2(0f, -81f));
        LowPolyPanelGraphic tag = CreatePanel("PawTag", frame.transform,
            PremiumUiStyle.CandyLemon, 18f, 5f, false);
        SetCentered(tag.rectTransform, new Vector2(48f, 48f), new Vector2(0f, -104f));
        TMP_Text paw = CreateText("Paw", tag.transform, font, 25f, Ink,
            TextAlignmentOptions.Center);
        Stretch(paw.rectTransform);
        paw.text = "•";

        TMP_Text hint = CreateText("PortraitHint", frame.transform, font, 17f,
            Ink, TextAlignmentOptions.Center);
        SetCentered(hint.rectTransform, new Vector2(270f, 28f), new Vector2(0f, -174f));
        hint.text = "PICK A COAT  •  SEE IT LIVE";
        hint.fontStyle = FontStyles.Bold;
        hint.characterSpacing = .8f;
    }

    private static void BuildPortraitEye(Transform parent, float x)
    {
        LowPolyPanelGraphic eye = CreatePanel("Eye", parent,
            new Color32(50, 41, 63, 255), 20f, 3f, false);
        SetCentered(eye.rectTransform, new Vector2(38f, 50f), new Vector2(x, 8f));
        LowPolyPanelGraphic iris = CreatePanel("Iris", eye.transform,
            new Color32(97, 212, 189, 255), 12f, 2f, false);
        SetCentered(iris.rectTransform, new Vector2(21f, 29f), Vector2.zero);
        LowPolyPanelGraphic shine = CreatePanel("Shine", eye.transform,
            Color.white, 6f, 1f, false);
        SetCentered(shine.rectTransform, new Vector2(10f, 10f), new Vector2(-7f, 10f));
    }

    private static Button BuildCloseButton(RectTransform panel, TMP_FontAsset font)
    {
        RectTransform root = CreateRect("CloseButton", panel);
        SetCentered(root, new Vector2(62f, 62f), new Vector2(524f, 326f));
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

    // --- shared helpers (mirroring RoomSelectorPanelBuilder) --------------

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
            throw new InvalidOperationException("Missing CatJournalPanel field '" + name + "'.");
        property.objectReferenceValue = value;
    }

    private static void AssignSwatches(SerializedObject serialized, List<Swatch> swatches)
    {
        SerializedProperty property = serialized.FindProperty("swatches");
        property.arraySize = swatches.Count;
        for (int i = 0; i < swatches.Count; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("button").objectReferenceValue = swatches[i].Button;
            element.FindPropertyRelative("selectionRing").objectReferenceValue =
                swatches[i].Ring;
        }
    }

    private static void AssignGraphics(SerializedObject serialized, string name,
        List<Graphic> graphics)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
            throw new InvalidOperationException("Missing Cat Journal field '" + name + "'.");
        property.arraySize = graphics.Count;
        for (int i = 0; i < graphics.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = graphics[i];
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

    private static void Validate(GameObject root)
    {
        if (root.GetComponents<CatJournalPanel>().Length != 1)
            throw new InvalidOperationException("Cat Journal needs exactly one controller.");
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.targetGraphic == null)
                throw new InvalidOperationException("Cat Journal button has no visual: " + button.name);
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

    private readonly struct Swatch
    {
        public Swatch(Button button, GameObject ring)
        {
            Button = button;
            Ring = ring;
        }

        public Button Button { get; }
        public GameObject Ring { get; }
    }
}
