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
/// Authors the premium SETTINGS panel in CatHome_UI: ON/OFF toggles for music,
/// sound, game sound, vibration and reduced motion. Opened from the hamburger's
/// SETTINGS row; <see cref="SettingsPanel"/> binds each row to the existing
/// preference services. No modal dialog on build (safe for batch/automation).
/// </summary>
public static class SettingsPanelBuilder
{
    public const string RootName = "SettingsPanelCanvas";
    public const string PrefabPath = "Assets/UI/SettingsPanel.prefab";
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";

    private static readonly Color Scrim = new Color32(44, 27, 67, 174);
    private static readonly Color Ink = PremiumUiStyle.Ink;
    private static readonly Color Cream = PremiumUiStyle.CandyCloud;

    private static readonly (string key, string label)[] Rows =
    {
        ("music", "MUSIC"),
        ("sound", "SOUND EFFECTS"),
        ("game_sound", "GAME SOUND"),
        ("haptics", "VIBRATION"),
        ("reduced_motion", "REDUCED MOTION"),
        ("language", "LANGUAGE")
    };

    [MenuItem("Tools/Cat Home/UI/Build Settings Panel")]
    public static void BuildBatch()
    {
        Debug.Log(BuildSilently());
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
        // Above the title overlay (200) so SETTINGS opens on top of the launch menu.
        canvas.sortingOrder = 230;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        GetOrAdd<GraphicRaycaster>(root);

        SettingsPanel controller = GetOrAdd<SettingsPanel>(root);
        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        RectTransform safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea);
        GetOrAdd<SafeAreaRect>(safeArea.gameObject);

        Image scrim = CreateImage("SettingsScrim", safeArea, Scrim, true);
        Stretch(scrim.rectTransform);
        Button scrimButton = GetOrAdd<Button>(scrim.gameObject);
        scrimButton.targetGraphic = scrim;
        scrimButton.transition = Selectable.Transition.None;

        RectTransform glow = CreateRect("AquaGlow", safeArea);
        SetCentered(glow, new Vector2(1020f, 800f), new Vector2(0f, -10f));
        LowPolyPanelGraphic glowGraphic = AddPanel(glow.gameObject,
            new Color32(53, 231, 216, 88), 48f, 10f, false);
        PremiumUiStyle.ConfigureShadowSurface(glowGraphic,
            new Color32(45, 220, 209, 96), 48f);

        RectTransform depth = CreateRect("CoralDepth", safeArea);
        SetCentered(depth, new Vector2(996f, 772f), Vector2.zero);
        AddPanel(depth.gameObject, PremiumUiStyle.CoralLift, 44f, 10f, false);

        RectTransform panel = CreateRect("SettingsPanelVisual", safeArea);
        SetCentered(panel, new Vector2(980f, 752f), Vector2.zero);
        CanvasGroup panelGroup = GetOrAdd<CanvasGroup>(panel.gameObject);
        LowPolyPanelGraphic panelFace = AddPanel(panel.gameObject, Cream, 44f, 14f, true);
        PremiumUiStyle.ConfigureAccentSurface(
            panelFace,
            new Color32(255, 239, 191, 255),
            new Color32(211, 249, 234, 255),
            44f, 14f);

        BuildHeader(panel, font);
        List<Row> builtRows = BuildRows(panel, font);
        Button closeButton = BuildCloseButton(panel, font);

        var serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", null);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "safeArea", safeArea);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "panelVisual", panel);
        Assign(serialized, "panelGroup", panelGroup);
        Assign(serialized, "closeButton", closeButton);
        AssignRows(serialized, builtRows);
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
        return "Premium Settings panel built in CatHome_UI.";
    }

    private static void BuildHeader(RectTransform panel, TMP_FontAsset font)
    {
        LowPolyPanelGraphic badge = CreatePanel("SettingsBadge", panel,
            PremiumUiStyle.CandyGrape, 26f, 8f, false);
        SetCentered(badge.rectTransform, new Vector2(150f, 48f), new Vector2(-360f, 322f));
        TMP_Text badgeText = CreateText("BadgeText", badge.transform, font, 20f,
            Color.white, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform);
        badgeText.text = "GEAR";
        badgeText.fontStyle = FontStyles.Bold;
        Localize(badgeText, "settings.badge");

        TMP_Text title = CreateText("Title", panel, font, 44f, Ink,
            TextAlignmentOptions.Center);
        SetCentered(title.rectTransform, new Vector2(560f, 58f), new Vector2(0f, 322f));
        title.text = "SETTINGS";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 2.4f;
        Localize(title, "settings.title");

        TMP_Text subtitle = CreateText("Subtitle", panel, font, 20f,
            PremiumUiStyle.Muted, TextAlignmentOptions.Center);
        SetCentered(subtitle.rectTransform, new Vector2(820f, 40f), new Vector2(0f, 280f));
        subtitle.text = "SOUND, MUSIC & MOTION";
        subtitle.fontStyle = FontStyles.Bold;
        subtitle.characterSpacing = 1.1f;
        Localize(subtitle, "settings.subtitle");
    }

    private const float RowWidth = 800f;
    private const float RowHeight = 78f;
    private const float RowSpacing = 12f;

    private static List<Row> BuildRows(RectTransform panel, TMP_FontAsset font)
    {
        var rows = new List<Row>(Rows.Length);
        float total = Rows.Length * RowHeight + (Rows.Length - 1) * RowSpacing;
        float startY = total * 0.5f - RowHeight * 0.5f - 40f;
        for (int i = 0; i < Rows.Length; i++)
        {
            float y = startY - i * (RowHeight + RowSpacing);
            LowPolyPanelGraphic well = CreatePanel("Row_" + Rows[i].key, panel,
                PremiumUiStyle.CandyCloud, 22f, 6f, false);
            SetCentered(well.rectTransform, new Vector2(RowWidth, RowHeight), new Vector2(0f, y));
            PremiumUiStyle.ConfigureAccentSurface(well,
                new Color32(255, 250, 232, 255), new Color32(232, 246, 255, 255), 22f, 6f);

            TMP_Text label = CreateText("Label", well.transform, font, 26f, Ink,
                TextAlignmentOptions.Left);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0.5f);
            labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.sizeDelta = new Vector2(520f, 40f);
            labelRect.anchoredPosition = new Vector2(36f, 0f);
            label.text = Rows[i].label;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 1.2f;
            label.overflowMode = TextOverflowModes.Truncate;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 26f;
            Localize(label, "settings.row." + Rows[i].key);

            RectTransform toggleRoot = CreateRect("Toggle", well.transform);
            toggleRoot.anchorMin = toggleRoot.anchorMax = toggleRoot.pivot = new Vector2(1f, 0.5f);
            toggleRoot.sizeDelta = new Vector2(166f, 54f);
            toggleRoot.anchoredPosition = new Vector2(-30f, 0f);
            LowPolyPanelGraphic face = AddPanel(toggleRoot.gameObject,
                new Color(0.42f, 0.84f, 0.55f), 24f, 7f, true);
            TMP_Text stateText = CreateText("State", toggleRoot, font, 24f,
                Color.white, TextAlignmentOptions.Center);
            Stretch(stateText.rectTransform);
            stateText.text = "ON";
            stateText.fontStyle = FontStyles.Bold;
            stateText.characterSpacing = 1.4f;
            stateText.enableAutoSizing = true;
            stateText.fontSizeMin = 16f;
            stateText.fontSizeMax = 24f;

            Button button = GetOrAdd<Button>(toggleRoot.gameObject);
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;

            rows.Add(new Row(Rows[i].key, button, face, stateText));
        }
        return rows;
    }

    private static Button BuildCloseButton(RectTransform panel, TMP_FontAsset font)
    {
        RectTransform root = CreateRect("CloseButton", panel);
        SetCentered(root, new Vector2(62f, 62f), new Vector2(444f, 326f));
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

    // --- shared helpers ---------------------------------------------------

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
            throw new InvalidOperationException("Missing SettingsPanel field '" + name + "'.");
        property.objectReferenceValue = value;
    }

    private static void AssignRows(SerializedObject serialized, List<Row> rows)
    {
        SerializedProperty property = serialized.FindProperty("rows");
        property.arraySize = rows.Count;
        for (int i = 0; i < rows.Count; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("key").stringValue = rows[i].Key;
            element.FindPropertyRelative("button").objectReferenceValue = rows[i].Button;
            element.FindPropertyRelative("face").objectReferenceValue = rows[i].Face;
            element.FindPropertyRelative("stateText").objectReferenceValue = rows[i].State;
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

    private static void Validate(GameObject root)
    {
        if (root.GetComponents<SettingsPanel>().Length != 1)
            throw new InvalidOperationException("Settings needs exactly one controller.");
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.targetGraphic == null)
                throw new InvalidOperationException("Settings button has no visual: " + button.name);
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

    private readonly struct Row
    {
        public Row(string key, Button button, LowPolyPanelGraphic face, TMP_Text state)
        {
            Key = key;
            Button = button;
            Face = face;
            State = state;
        }

        public string Key { get; }
        public Button Button { get; }
        public LowPolyPanelGraphic Face { get; }
        public TMP_Text State { get; }
    }
}
