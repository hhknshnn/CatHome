using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Authors the Home 2.0 room-edit overlay under the named shared Canvas.</summary>
public static class HomeEditPanelBuilder
{
    private const string RootName = "HomeEditPanel";
    private const string PrefabPath = "Assets/UI/HomeEditPanel.prefab";
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private static bool suppressDialogs;

    [MenuItem("Tools/Cat Home/UI/Build Home 2.0 Edit Room Panel")]
    public static void Build()
    {
        Scene scene = SceneManager.GetSceneByName("CatHome_UI");
        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
        Canvas mainCanvas = FindNamedMainCanvas(scene);
        if (mainCanvas == null)
        {
            Debug.LogError("Home Edit builder needs the named CatHome_UI Canvas root.");
            return;
        }

        Transform existing = mainCanvas.transform.Find(RootName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Fredoka-SemiBold SDF.asset");

        GameObject root = CreateRect(RootName, mainCanvas.transform);
        Stretch(root.GetComponent<RectTransform>());
        Canvas nestedCanvas = root.AddComponent<Canvas>();
        nestedCanvas.overrideSorting = true;
        nestedCanvas.sortingOrder = 245;
        root.AddComponent<GraphicRaycaster>();
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        root.AddComponent<HomePlacementZoneGuide>();
        HomeEditModeController controller = root.AddComponent<HomeEditModeController>();

        Image scrim = CreateImage("Scrim", root.transform, new Color(0.04f, 0.09f, 0.16f, 0.13f), false);
        Stretch(scrim.rectTransform);

        Image input = CreateImage("EditDragSurface", root.transform, new Color(1f, 1f, 1f, 0.001f), true);
        Stretch(input.rectTransform);
        HomeEditInputSurface inputSurface = input.gameObject.AddComponent<HomeEditInputSurface>();
        inputSurface.EditorConfigure(controller);

        GameObject safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea.GetComponent<RectTransform>());
        safeArea.AddComponent<SafeAreaRect>();

        GameObject header = CreatePanel("HeaderCard", safeArea.transform,
            new Color32(255, 244, 221, 255), new Vector2(760f, 112f),
            new Vector2(0f, -104f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        CreatePanel("HeaderRibbon", header.transform,
            new Color32(72, 210, 204, 255), new Vector2(740f, 42f),
            new Vector2(0f, -8f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        TMP_Text roomTitle = CreateLabel("RoomTitle", header.transform, font, 25f,
            PremiumUiStyle.Ivory, TextAlignmentOptions.Center, "EDIT LIVING ROOM");
        SetRect(roomTitle.rectTransform, new Vector2(0f, -10f), new Vector2(710f, 38f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        Button previous = CreateButton("PreviousItemButton", header.transform, font,
            "<", new Vector2(48f, 48f), new Vector2(-328f, -53f), PremiumUiStyle.CandyGrape);
        Button next = CreateButton("NextItemButton", header.transform, font,
            ">", new Vector2(48f, 48f), new Vector2(328f, -53f), PremiumUiStyle.CandyGrape);
        TMP_Text productTitle = CreateLabel("ProductTitle", header.transform, font, 23f,
            PremiumUiStyle.Ink, TextAlignmentOptions.Center, "SELECT AN ITEM");
        SetRect(productTitle.rectTransform, new Vector2(-82f, -59f), new Vector2(400f, 34f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        GameObject ruleBadgeObject = CreatePanel("PlacementRuleBadge", header.transform,
            new Color32(205, 241, 224, 255), new Vector2(178f, 38f),
            new Vector2(205f, -58f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        LowPolyPanelGraphic ruleBadge = ruleBadgeObject.GetComponent<LowPolyPanelGraphic>();
        TMP_Text placementRule = CreateLabel("PlacementRule", ruleBadgeObject.transform, font, 15f,
            PremiumUiStyle.Ink, TextAlignmentOptions.Center, "TAP TO SELECT");
        SetRect(placementRule.rectTransform, Vector2.zero, new Vector2(162f, 32f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        GameObject toolbar = CreatePanel("Toolbar", safeArea.transform,
            new Color32(255, 248, 226, 255), new Vector2(720f, 126f),
            // Keep a real visual gutter above the persistent HomeDock. The
            // buttons extend eight pixels below this card by design, so 154
            // leaves 17 px between their faces and the dock's upper rim.
            new Vector2(0f, 154f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
        TMP_Text hint = CreateLabel("Hint", toolbar.transform, font, 17f,
            PremiumUiStyle.Ink, TextAlignmentOptions.Center, "TAP AN ITEM TO MOVE IT");
        SetRect(hint.rectTransform, new Vector2(0f, -14f), new Vector2(650f, 26f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));

        // Keep every action face fully inside the cream toolbar. The previous
        // 64 px faces started at -70 and protruded eight pixels through the
        // card's lower rim.
        float y = -52f;
        Button rotate = CreateButton("RotateButton", toolbar.transform, font,
            "TURN", new Vector2(190f, 56f), new Vector2(-210f, y), PremiumUiStyle.CandyAqua);
        Button store = CreateButton("StoreItemButton", toolbar.transform, font,
            "STORE", new Vector2(190f, 56f), new Vector2(0f, y), PremiumUiStyle.CandyPeach);
        TMP_Text storeLabel = store.GetComponentInChildren<TMP_Text>(true);
        Button done = CreateButton("DoneButton", toolbar.transform, font,
            "DONE", new Vector2(190f, 56f), new Vector2(210f, y), PremiumUiStyle.CandyBerry);

        controller.EditorConfigure(group, roomTitle, productTitle, placementRule, hint,
            ruleBadge, previous, next, rotate, store, storeLabel, done);

        PremiumUiFactory.PolishHierarchy(root.transform, font);
        EnsureFolder("Assets/UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();

        if (!suppressDialogs)
            EditorUtility.DisplayDialog("Home 2.0", "EDIT ROOM panel rebuilt.", "OK");
    }

    public static void BuildSilently()
    {
        suppressDialogs = true;
        try { Build(); }
        finally { suppressDialogs = false; }
    }

    private static Canvas FindNamedMainCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
                if (canvases[i] != null && canvases[i].name == "Canvas")
                    return canvases[i];
        }
        return null;
    }

    private static GameObject CreatePanel(
        string name, Transform parent, Color color, Vector2 size,
        Vector2 position, Vector2 anchor, Vector2 pivot)
    {
        GameObject root = CreateRect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, size, anchor, pivot);
        root.AddComponent<CanvasRenderer>();
        LowPolyPanelGraphic panel = root.AddComponent<LowPolyPanelGraphic>();
        panel.SetPremiumBaseColor(color);
        panel.raycastTarget = false;
        return root;
    }

    private static void AddInset(
        Transform parent, string name, Color color, Vector2 min, Vector2 max)
    {
        GameObject inset = CreateRect(name, parent);
        RectTransform rect = inset.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(min.x, max.y);
        rect.offsetMax = new Vector2(max.x, min.y);
        inset.AddComponent<CanvasRenderer>();
        LowPolyPanelGraphic graphic = inset.AddComponent<LowPolyPanelGraphic>();
        graphic.SetPremiumBaseColor(color);
        graphic.raycastTarget = false;
    }

    private static Button CreateButton(
        string name, Transform parent, TMP_FontAsset font, string text,
        Vector2 size, Vector2 position, Color color)
    {
        GameObject root = CreateRect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, size,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        Button button = root.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        GameObject face = CreateRect("Face", root.transform);
        Stretch(face.GetComponent<RectTransform>());
        face.AddComponent<CanvasRenderer>();
        LowPolyPanelGraphic graphic = face.AddComponent<LowPolyPanelGraphic>();
        graphic.SetPremiumBaseColor(color);
        graphic.raycastTarget = true;
        button.targetGraphic = graphic;

        TMP_Text label = CreateLabel("Label", face.transform, font, 22f,
            PremiumUiStyle.Ivory, TextAlignmentOptions.Center, text);
        Stretch(label.rectTransform);
        label.margin = new Vector4(8f, 3f, 8f, 3f);
        label.raycastTarget = false;
        return button;
    }

    private static TMP_Text CreateLabel(
        string name, Transform parent, TMP_FontAsset font, float size,
        Color color, TextAlignmentOptions alignment, string text)
    {
        GameObject root = CreateRect(name, parent);
        root.AddComponent<CanvasRenderer>();
        TextMeshProUGUI label = root.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = alignment;
        label.text = text;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;
        return label;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycast)
    {
        GameObject root = CreateRect(name, parent);
        root.AddComponent<CanvasRenderer>();
        Image image = root.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root;
    }

    private static void SetRect(
        RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor, Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string child = path.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, child);
    }
}
