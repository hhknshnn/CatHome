using System;
using System.IO;
using TMPro;
using U = PremiumUiElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OnlineServicesPanelBuilder
{
    public const string PrivacyRootName = "PrivacyDataPanelCanvas";
    public const string ConflictRootName = "CloudSaveConflictPanelCanvas";
    public const string PrivacyPrefabPath = "Assets/UI/PrivacyDataPanel.prefab";
    public const string ConflictPrefabPath = "Assets/UI/CloudSaveConflictPanel.prefab";
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";

    [MenuItem("Tools/Cat Home/UI/Build Online Services Panels")]
    public static void BuildBatch() => Debug.Log(BuildSilently());

    public static string BuildSilently()
    {
        Scene scene = SceneManager.GetSceneByPath(UiScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);

        GameObject privacy = PrepareRoot(scene, PrivacyRootName, 240);
        BuildPrivacy(privacy, font);
        RemoveObsoleteConflictSurface(scene);

        PremiumUiFactory.PolishHierarchy(privacy.transform, font);
        EnsureFolder("Assets/UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            privacy, PrivacyPrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        if (opened)
            EditorSceneManager.CloseScene(scene, true);
        return "Privacy/data panel built; Cloud Save remains seamless.";
    }

    private static void RemoveObsoleteConflictSurface(Scene scene)
    {
        foreach (GameObject candidate in scene.GetRootGameObjects())
            if (candidate.name == ConflictRootName)
                UnityEngine.Object.DestroyImmediate(candidate);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ConflictPrefabPath) != null)
            AssetDatabase.DeleteAsset(ConflictPrefabPath);
    }

    private static void BuildPrivacy(GameObject root, TMP_FontAsset font)
    {
        PrivacyDataPanel controller = GetOrAdd<PrivacyDataPanel>(root);
        CanvasGroup group = GetOrAdd<CanvasGroup>(root);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        RectTransform safe = CreateSafeArea(root.transform);
        Button scrim = CreateScrim(safe, new Color32(37, 28, 63, 190));
        var card=U.Panel("PrivacyDataCard",safe,PremiumUiStyle.Ivory,0,0,1080,800,32,true);
        RectTransform panel=card.rectTransform;
        U.Localize(U.Label("Title",panel,font,42,PremiumUiStyle.Ink,-60,325,830,70),"privacy.title");
        var close=U.Action("CloseButton",panel,font,null,PremiumUiStyle.WarmIvory,471,333,60,60,out var closeText);
        closeText.text="×"; closeText.fontSize=34;
        var statusWell=U.Panel("CloudStatusWell",panel,PremiumUiStyle.Mint,0,205,952,106,22);
        var status=U.Label("Status",statusWell.transform,font,23,PremiumUiStyle.Ink,0,0,878,78);
        var privacy=U.Action("PrivacyPolicyButton",panel,font,"privacy.policy",PremiumUiStyle.WarmIvory,-244,79,464,66,out var ignored);
        var data=U.Action("DataRequestButton",panel,font,"privacy.data_requests",PremiumUiStyle.WarmIvory,244,79,464,66,out ignored);
        var deleteInfo=U.Action("DeleteInfoButton",panel,font,"privacy.delete_info",PremiumUiStyle.WarmIvory,0,-7,952,66,out ignored);
        var sync=U.Action("SyncNowButton",panel,font,"privacy.sync_now",PremiumUiStyle.Teal,-244,-122,464,72,out var syncLabel);
        syncLabel.color=Color.white;
        var deleteCloud=U.Action("DeleteCloudAccountButton",panel,font,"privacy.delete_cloud",PremiumUiStyle.WarmIvory,244,-122,464,72,out var deleteLabel);
        deleteLabel.color=new Color32(152,53,43,255);
        U.Localize(U.Label("DeleteNote",panel,font,20,PremiumUiStyle.Muted,0,-228,930,80),"privacy.delete_note");
        U.Localize(U.Label("UnityPortal",panel,font,17,PremiumUiStyle.Muted,0,-331,930,40),"privacy.unity_portal");

        var confirmation=U.Panel("DeleteConfirmation",safe,PremiumUiStyle.Ivory,0,0,900,560,32,true);
        U.Localize(U.Label("Title",confirmation.transform,font,38,PremiumUiStyle.Ink,0,190,776,90),"privacy.confirm.title");
        U.Localize(U.Label("Consequences",confirmation.transform,font,23,PremiumUiStyle.Ink,0,52,776,160),"privacy.confirm.body");
        var cancel=U.Action("CancelDelete",confirmation.transform,font,"common.cancel",PremiumUiStyle.Mint,-202,-157,378,80,out ignored);
        var confirm=U.Action("ConfirmDelete",confirmation.transform,font,"privacy.confirm.action",new Color32(163,56,48,255),202,-157,378,80,out var dangerLabel);
        dangerLabel.color=Color.white;
        confirmation.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(controller);
        Assign(so, "rootGroup", group);
        Assign(so, "panelVisual", panel);
        Assign(so, "scrimButton", scrim);
        Assign(so, "closeButton", close);
        Assign(so, "privacyButton", privacy);
        Assign(so, "deleteInfoButton", deleteInfo);
        Assign(so, "dataRequestButton", data);
        Assign(so, "syncButton", sync);
        Assign(so, "deleteCloudAccountButton", deleteCloud);
        Assign(so, "deleteCloudAccountLabel", deleteLabel);
        Assign(so, "statusText", status);
        Assign(so, "confirmationRoot", confirmation.gameObject);
        Assign(so, "confirmDeleteButton", confirm);
        Assign(so, "cancelDeleteButton", cancel);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button RowButton(string name, RectTransform parent, TMP_FontAsset font,
        string label, string key, Vector2 position, Color color)
    {
        Button button = Button(name, parent, font, label, color, PremiumUiStyle.Ink,
            new Vector2(820f, 66f), position, 22f);
        Localize(Label(button), key);
        return button;
    }

    private static GameObject PrepareRoot(Scene scene, string name, int sortingOrder)
    {
        GameObject root = null;
        foreach (GameObject candidate in scene.GetRootGameObjects())
            if (candidate.name == name)
                root = candidate;
        if (root == null)
        {
            root = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        else
        {
            if (PrefabUtility.IsPartOfPrefabInstance(root) &&
                PrefabUtility.GetNearestPrefabInstanceRoot(root) == root)
                PrefabUtility.UnpackPrefabInstance(root,
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
        Canvas canvas = GetOrAdd<Canvas>(root);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        GetOrAdd<GraphicRaycaster>(root);
        return root;
    }

    private static RectTransform CreateSafeArea(Transform parent)
    {
        RectTransform safe = Rect("SafeArea", parent);
        Stretch(safe);
        GetOrAdd<SafeAreaRect>(safe.gameObject);
        return safe;
    }

    private static Button CreateScrim(RectTransform parent, Color color)
    {
        RectTransform rect = Rect("Scrim", parent);
        Stretch(rect);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        return button;
    }

    private static LowPolyPanelGraphic CreatePanel(string name, Transform parent,
        Vector2 size, Vector2 position, Color color, float cut, float bevel)
    {
        RectTransform rect = Rect(name, parent);
        Set(rect, size, position);
        GetOrAdd<CanvasRenderer>(rect.gameObject);
        LowPolyPanelGraphic panel = GetOrAdd<LowPolyPanelGraphic>(rect.gameObject);
        PremiumUiStyle.ConfigureSurface(panel, color, cut, bevel);
        panel.raycastTarget = true;
        return panel;
    }

    private static Button Button(string name, Transform parent, TMP_FontAsset font,
        string value, Color color, Color textColor, Vector2 size, Vector2 position,
        float fontSize)
    {
        RectTransform root = Rect(name, parent);
        Set(root, size, position);
        RectTransform visual = Rect("Visual", root);
        Stretch(visual);
        GetOrAdd<CanvasRenderer>(visual.gameObject);
        LowPolyPanelGraphic surface = GetOrAdd<LowPolyPanelGraphic>(visual.gameObject);
        PremiumUiStyle.ConfigureSurface(surface, color, Mathf.Min(30f, size.y * .45f), 8f);
        surface.raycastTarget = true;
        TMP_Text label = Text("Label", visual, font, value, fontSize, textColor,
            TextAlignmentOptions.Center, size - new Vector2(24f, 12f), Vector2.zero);
        Button button = GetOrAdd<Button>(root.gameObject);
        button.targetGraphic = surface;
        return button;
    }

    private static TMP_Text Label(Button button) =>
        button.transform.Find("Visual/Label").GetComponent<TMP_Text>();

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font,
        string value, float size, Color color, TextAlignmentOptions alignment,
        Vector2 rectSize, Vector2 position)
    {
        RectTransform rect = Rect(name, parent);
        Set(rect, rectSize, position);
        GetOrAdd<CanvasRenderer>(rect.gameObject);
        TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.extraPadding = true;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void Localize(TMP_Text text, string key)
    {
        LocalizedLabel localized = GetOrAdd<LocalizedLabel>(text.gameObject);
        localized.EditorConfigure(text, key);
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        return rect;
    }

    private static void Set(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Assign(SerializedObject so, string field, UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property == null)
            throw new InvalidOperationException("Missing field: " + field);
        property.objectReferenceValue = value;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
