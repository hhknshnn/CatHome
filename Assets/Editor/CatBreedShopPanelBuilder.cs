using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using U = PremiumUiElements;

/// <summary>Authors the premium, live-3D Polyperfect CAT SHOP in CatHome_UI.</summary>
public static class CatBreedShopPanelBuilder
{
    public const string RootName = "CatBreedShopPanelCanvas";
    public const string PrefabPath = "Assets/UI/CatBreedShopPanel.prefab";
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";

    private static readonly Color Scrim = new Color32(31, 29, 61, 188);
    private static readonly Color Ink = PremiumUiStyle.Ink;

    [MenuItem("Tools/Cat Home/UI/Build Cat Breed Shop")]
    public static void BuildFromMenu()
    {
        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home", result, "OK");
    }

    public static string BuildSilently()
    {
        CatBreedCatalog catalog = AssetDatabase.LoadAssetAtPath<CatBreedCatalog>(
            PolyperfectCatIntegrationBuilder.CatalogPath);
        if (catalog == null || catalog.Count != 10)
            throw new InvalidOperationException(
                "Build the Polyperfect cat catalog before building CAT SHOP.");

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
        canvas.sortingOrder = 121;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        GetOrAdd<GraphicRaycaster>(root);

        CatBreedShopPanel controller = GetOrAdd<CatBreedShopPanel>(root);
        CanvasGroup rootGroup = GetOrAdd<CanvasGroup>(root);
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        RectTransform safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea);
        GetOrAdd<SafeAreaRect>(safeArea.gameObject);

        Image scrim = CreateImage("CatShopScrim", safeArea, Scrim, true);
        Stretch(scrim.rectTransform);
        Button scrimButton = GetOrAdd<Button>(scrim.gameObject);
        scrimButton.targetGraphic = scrim;
        scrimButton.transition = Selectable.Transition.None;

        RectTransform panel = CreateRect("CatBreedShopPanelVisual", safeArea);
        SetCentered(panel, new Vector2(1720f, 930f), Vector2.zero);
        CanvasGroup panelGroup = GetOrAdd<CanvasGroup>(panel.gameObject);
        var face = AddPanel(panel.gameObject, PremiumUiStyle.Ivory, 32f, 2f, true);
        PremiumUiStyle.ConfigureLightSurface(face, 32f, 2f);

        BuildHeader(panel, font);
        CatBreedTurntablePreview preview = BuildTurntable(panel, font);
        BuildDetails(panel, font, out TMP_Text breedName, out TMP_Text breedStatus,
            out Button useButton, out TMP_Text useButtonLabel);
        List<CardParts> cards = BuildBreedCards(panel, font, catalog);
        Button closeButton = BuildCloseButton(panel, font);
        var nameInput = BuildIdentityControls(panel, font, out var coatButtons, out var coatSelections);

        var serialized = new SerializedObject(controller);
        Assign(serialized, "catMovement", null);
        Assign(serialized, "rootGroup", rootGroup);
        Assign(serialized, "safeArea", safeArea);
        Assign(serialized, "scrimButton", scrimButton);
        Assign(serialized, "panelVisual", panel);
        Assign(serialized, "panelGroup", panelGroup);
        Assign(serialized, "closeButton", closeButton);
        Assign(serialized, "useButton", useButton);
        Assign(serialized, "useButtonLabel", useButtonLabel);
        Assign(serialized, "breedName", breedName);
        Assign(serialized, "breedStatus", breedStatus);
        Assign(serialized, "turntable", preview);
        Assign(serialized, "nameInput", nameInput);
        var coatArray = serialized.FindProperty("coatButtons"); coatArray.arraySize = coatButtons.Count;
        var ringArray = serialized.FindProperty("coatSelection"); ringArray.arraySize = coatSelections.Count;
        for (int i = 0; i < coatButtons.Count; i++)
        {
            coatArray.GetArrayElementAtIndex(i).objectReferenceValue = coatButtons[i];
            ringArray.GetArrayElementAtIndex(i).objectReferenceValue = coatSelections[i];
        }
        AssignCards(serialized, cards);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PremiumUiFactory.PolishHierarchy(root.transform, font);
        Validate(root, catalog);
        EnsureFolder("Assets/UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            root, PrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.MarkSceneDirty(uiScene);
        EditorSceneManager.SaveScene(uiScene);
        AssetDatabase.SaveAssets();

        if (openedForBuild)
            EditorSceneManager.CloseScene(uiScene, true);
        return "Premium CAT SHOP built with ten real portraits and a live 3D turntable.";
    }

    private static void BuildHeader(RectTransform panel, TMP_FontAsset font)
    {
        var title = U.Label("Title", panel, font, 48, Ink, -590, 393, 400, 70);
        U.Localize(title, "cat.title");
        var subtitle = U.Label("Subtitle", panel, font, 22, PremiumUiStyle.Muted, -485, 340, 610, 40);
        U.Localize(subtitle, "cat.subtitle");
        var appearance = U.Label("AppearanceTitle", panel, font, 30, Ink, 430, 326, 626, 48);
        U.Localize(appearance, "cat.appearance");
    }

    private static CatBreedTurntablePreview BuildTurntable(RectTransform panel, TMP_FontAsset font)
    {
        var well = U.Panel("TurntableWell", panel, PremiumUiStyle.Mint, -410, 15, 690, 560, 26);
        var viewport = U.Rect("LivePreviewViewport", well.transform); U.At(viewport, 0, 0, 548, 548);
        var raw = viewport.gameObject.AddComponent<RawImage>(); raw.raycastTarget = true;
        return viewport.gameObject.AddComponent<CatBreedTurntablePreview>();
    }

    private static void BuildDetails(RectTransform panel, TMP_FontAsset font,
        out TMP_Text breedName, out TMP_Text breedStatus, out Button useButton, out TMP_Text useButtonLabel)
    {
        breedName = U.Label("BreedName", panel, font, 24, Ink, -410, 260, 550, 42, TextAlignmentOptions.Center);
        breedStatus = U.Label("BreedStatus", panel, font, 20, PremiumUiStyle.Muted, 435, -325, 630, 36, TextAlignmentOptions.Center);
        useButton = U.Action("UseThisCatButton", panel, font, "cat.continue", PremiumUiStyle.Coral,
            435, -394, 626, 76, out useButtonLabel);
    }

    private static TMP_InputField BuildIdentityControls(RectTransform panel, TMP_FontAsset font,
        out List<Button> buttons, out List<GameObject> rings)
    {
        var well = U.Panel("NameInputWell", panel, PremiumUiStyle.WarmIvory, -410, -315, 600, 66, 20, true);
        var area = U.Rect("TextArea", well.transform); U.Fill(area, 14); area.gameObject.AddComponent<RectMask2D>();
        var placeholder = U.Label("Placeholder", area, font, 26, PremiumUiStyle.Muted, 0, 0, 1, 1);
        U.Fill(placeholder.rectTransform); U.Localize(placeholder, "cat.name");
        var text = U.Label("Text", area, font, 28, Ink, 0, 0, 1, 1); U.Fill(text.rectTransform);
        var input = well.gameObject.AddComponent<TMP_InputField>();
        input.targetGraphic = well; input.textViewport = area; input.textComponent = text;
        input.placeholder = placeholder; input.fontAsset = font; input.pointSize = 28;
        input.characterLimit = CatIdentityService.MaxNameLength; input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard; input.caretWidth = 2;
        input.customCaretColor = true; input.caretColor = PremiumUiStyle.Teal;
        input.selectionColor = new Color32(143, 204, 185, 140);
        var coat = U.Label("CoatLabel", panel, font, 18, PremiumUiStyle.Muted, -410, -366, 600, 26);
        U.Localize(coat, "cat.coat");
        buttons = new List<Button>(); rings = new List<GameObject>();
        for (int i = 0; i < CatIdentityService.CoatCount; i++)
        {
            float x = -667 + i * 73f;
            var ring = U.Panel("CoatSelection_" + i, panel, PremiumUiStyle.Teal, x, -414, 62, 54, 22);
            var button = U.Action("Coat_" + i, panel, font, null, CatIdentityService.Palette[i].Tint,
                x, -414, 52, 48, out var unused);
            unused.gameObject.SetActive(false); buttons.Add(button); rings.Add(ring.gameObject);
            ring.gameObject.SetActive(false);
        }
        return input;
    }

    private static List<CardParts> BuildBreedCards(RectTransform panel, TMP_FontAsset font, CatBreedCatalog catalog)
    {
        var scrollRect = U.Rect("BreedScroll", panel); U.At(scrollRect, 435, -8, 650, 572);
        var viewport = U.Rect("Viewport", scrollRect); U.Fill(viewport); viewport.gameObject.AddComponent<RectMask2D>();
        var content = U.Rect("BreedGrid", viewport); content.anchorMin = new Vector2(0,1);
        content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = new Vector2(302,176);
        grid.spacing = new Vector2(20,16); grid.padding = new RectOffset(6,6,6,6);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36;
        viewport.offsetMax = new Vector2(-22,0);
        var track = U.Panel("ScrollTrack", scrollRect, PremiumUiStyle.Mint, 318,0,10,572,5,true);
        var handle = U.Panel("Handle", track.transform, PremiumUiStyle.Teal,0,0,10,96,5,true);
        var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle.rectTransform;
        bar.targetGraphic = handle; bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        var cards = new List<CardParts>();
        for (int i = 0; i < catalog.Count; i++)
        {
            var entry = catalog.Get(i); var root = U.Rect("BreedCard_" + entry.Id, content);
            U.At(root, 0, 0, 302,176);
            var ring = U.Panel("SelectionRing", root, PremiumUiStyle.Teal, 0,0,302,176,20); U.Fill(ring.rectTransform);
            var face = U.Panel("CardVisual", root, PremiumUiStyle.Ivory,0,0,294,168,17,true);
            var portrait = U.Rect("RealCatPortrait", face.transform); U.At(portrait,0,17,112,112);
            var image = portrait.gameObject.AddComponent<Image>(); image.sprite = entry.Portrait; image.preserveAspect = true; image.raycastTarget = false;
            var label = U.Label("BreedLabel",face.transform,font,19,Ink,0,-59,268,40,TextAlignmentOptions.Center);
            label.text = entry.DisplayName;
            var badge = U.Panel("ActiveBadge",root,PremiumUiStyle.Mint,118,61,32,32,16);
            var checkShort=U.Rect("CheckShort",badge.transform);U.At(checkShort,-5,-1,4,10);
            checkShort.gameObject.AddComponent<Image>().color=Ink;checkShort.localEulerAngles=new Vector3(0,0,40);
            var checkLong=U.Rect("CheckLong",badge.transform);U.At(checkLong,2,2,4,18);
            checkLong.gameObject.AddComponent<Image>().color=Ink;checkLong.localEulerAngles=new Vector3(0,0,-40);
            var button = root.gameObject.AddComponent<Button>(); button.targetGraphic = face; button.transition = Selectable.Transition.None;
            ring.gameObject.SetActive(false); badge.gameObject.SetActive(false);
            cards.Add(new CardParts(entry.Id,button,ring.gameObject,badge.gameObject,label));
        }
        return cards;
    }

    private static Button BuildCloseButton(RectTransform panel,TMP_FontAsset font)
    {
        var close = U.Action("CloseButton",panel,font,null,PremiumUiStyle.WarmIvory,790,410,58,58,out var label);
        label.text = "×"; label.fontSize=34; return close;
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
            throw new InvalidOperationException("Missing CAT SHOP field '" + name + "'.");
        property.objectReferenceValue = value;
    }

    private static void AssignCards(SerializedObject serialized, List<CardParts> cards)
    {
        SerializedProperty property = serialized.FindProperty("cards");
        property.arraySize = cards.Count;
        for (int i = 0; i < cards.Count; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("id").stringValue = cards[i].Id;
            element.FindPropertyRelative("button").objectReferenceValue = cards[i].Button;
            element.FindPropertyRelative("selectionRing").objectReferenceValue = cards[i].Ring;
            element.FindPropertyRelative("activeBadge").objectReferenceValue = cards[i].Badge;
            element.FindPropertyRelative("label").objectReferenceValue = cards[i].Label;
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

    private static void Validate(GameObject root, CatBreedCatalog catalog)
    {
        if (root.GetComponents<CatBreedShopPanel>().Length != 1)
            throw new InvalidOperationException("CAT SHOP needs exactly one controller.");
        if (root.GetComponentsInChildren<CatBreedTurntablePreview>(true).Length != 1)
            throw new InvalidOperationException("CAT SHOP needs exactly one live turntable.");
        if (catalog.Count != 10)
            throw new InvalidOperationException("CAT SHOP must expose all ten package cats.");
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            if (button.targetGraphic == null)
                throw new InvalidOperationException("CAT SHOP button has no visual: " + button.name);
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
        public CardParts(string id, Button button, GameObject ring, GameObject badge, TMP_Text label)
        {
            Id = id;
            Button = button;
            Ring = ring;
            Badge = badge;
            Label = label;
        }

        public string Id { get; }
        public Button Button { get; }
        public GameObject Ring { get; }
        public GameObject Badge { get; }
        public TMP_Text Label { get; }
    }
}
