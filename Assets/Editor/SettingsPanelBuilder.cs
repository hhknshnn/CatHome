using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using U = PremiumUiElements;
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
        ("language", "LANGUAGE"),
        ("account", "ACCOUNT")
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

        RectTransform panel = U.Rect("SettingsPanelVisual", safeArea);
        U.At(panel, 0, 0, 1240, 860);
        CanvasGroup panelGroup = GetOrAdd<CanvasGroup>(panel.gameObject);
        AddPanel(panel.gameObject, PremiumUiStyle.Ivory, 32, 2, true);

        BuildHeader(panel, font);
        List<Row> builtRows = BuildRows(panel, font);
        Button privacyDataButton = BuildPrivacyDataButton(panel, font);
        Button closeButton = BuildCloseButton(panel, font);

        var serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", null);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "safeArea", safeArea);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "panelVisual", panel);
        Assign(serialized, "panelGroup", panelGroup);
        Assign(serialized, "closeButton", closeButton);
        Assign(serialized, "privacyDataButton", privacyDataButton);
        AssignRows(serialized, builtRows);
        Assign(serialized, "accountStatus", panel.Find("Row_account/AccountStatus").GetComponent<TMP_Text>());
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
        U.Localize(U.Label("Title",panel,font,48,Ink,-250,353,620,70),"settings.title");
        U.Localize(U.Label("Subtitle",panel,font,22,PremiumUiStyle.Muted,-100,293,920,42),"settings.subtitle");
        U.Localize(U.Label("SoundSection",panel,font,24,Ink,-300,222,512,36),"settings.group.sound");
        U.Localize(U.Label("AccessSection",panel,font,24,Ink,300,222,512,36),"settings.group.access");
    }

    private static List<Row> BuildRows(RectTransform panel, TMP_FontAsset font)
    {
        var rows = new List<Row>(Rows.Length);
        for (int i=0;i<Rows.Length;i++)
        {
            bool account=i==6;
            float x=i<3?-300:300;
            float y=148-(i<3?i:i-3)*92;
            if(account){x=0;y=-224;}
            var well=U.Panel("Row_"+Rows[i].key,panel,PremiumUiStyle.WarmIvory,x,y,account?1136:536,76,20);
            var label=U.Label("Label",well.transform,font,23,Ink,account?-300:-82,account?14:0,account?470:334,38);
            U.Localize(label,"settings.row."+Rows[i].key);
            if(account)
            {
                var status=U.Label("AccountStatus",well.transform,font,19,PremiumUiStyle.Muted,-200,-18,670,30);
                status.text=GameLanguageService.Text("account.status.guest");
            }
            var button=U.Action("Toggle",well.transform,font,null,PremiumUiStyle.Mint,
                account?406:192,0,account?248:124,54,out var state);
            state.fontSize=20;
            var face=(LowPolyPanelGraphic)button.targetGraphic;
            rows.Add(new Row(Rows[i].key,button,face,state));
        }
        U.Localize(U.Label("AccountSection",panel,font,24,Ink,0,-154,1136,36),"settings.row.account");
        return rows;
    }
    private static Button BuildCloseButton(RectTransform panel,TMP_FontAsset font)
    {
        var button=U.Action("CloseButton",panel,font,null,PremiumUiStyle.WarmIvory,552,359,60,60,out var label);
        label.text="×"; label.fontSize=34; return button;
    }
    private static Button BuildPrivacyDataButton(RectTransform panel,TMP_FontAsset font)
    {
        return U.Action("PrivacyDataButton",panel,font,"settings.privacy_data",PremiumUiStyle.Mint,
            0,-343,380,60,out var label);
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
