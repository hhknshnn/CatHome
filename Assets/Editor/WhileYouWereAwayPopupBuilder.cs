using System.IO;
using TMPro;
using U = PremiumUiElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WhileYouWereAwayPopupBuilder
{
    private static bool suppressDialogs;
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private const string CanvasRootName = "Canvas";
    private const string RootName = "WhileYouWereAwayPopup";
    private const string PrefabFolder = "Assets/UI";
    private const string PrefabPath = PrefabFolder + "/WhileYouWereAwayPopup.prefab";

    private static readonly Color Overlay = new Color32(36, 24, 69, 184);
    private static readonly Color FrameShadow = PremiumUiStyle.Shadow;
    private static readonly Color FaceTop = new Color32(255, 229, 166, 255);
    private static readonly Color FaceBottom = new Color32(255, 188, 216, 255);
    private static readonly Color HungerTop = new Color32(255, 220, 91, 255);
    private static readonly Color HungerBottom = new Color32(255, 139, 91, 255);
    private static readonly Color ThirstTop = new Color32(100, 236, 232, 255);
    private static readonly Color ThirstBottom = new Color32(67, 164, 242, 255);
    private static readonly Color EnergyTop = new Color32(231, 139, 247, 255);
    private static readonly Color EnergyBottom = new Color32(166, 88, 226, 255);
    private static readonly Color DarkText = PremiumUiStyle.Night;
    private static readonly Color LightText = PremiumUiStyle.Ivory;
    private static readonly Color ContentText = PremiumUiStyle.Ink;

    [MenuItem("Tools/Cat Home/Build While You Were Away Popup")]
    public static void Build()
    {
        Scene uiScene = SceneManager.GetSceneByPath(UiScenePath);
        bool openedForBuild = !uiScene.IsValid() || !uiScene.isLoaded;
        if (openedForBuild)
            uiScene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);

        // The shared UI scene contains several overlay canvases. Selecting an
        // arbitrary Canvas makes the result depend on root/sibling ordering and
        // can parent the popup under CurrencyHudCanvas (or another modal canvas).
        // The popup then hides its own parent while it is open, becomes invisible,
        // and leaves the rest of the HUD permanently gated. Always author under
        // the one canonical, root-level gameplay Canvas instead.
        Canvas canvas = FindRootComponent<Canvas>(uiScene, CanvasRootName);
        EventSystem eventSystem = FindInScene<EventSystem>(uiScene);
        if (canvas == null || eventSystem == null)
        {
            string message = canvas == null
                ? $"Shared UI scene has no root-level '{CanvasRootName}' Canvas."
                : "Shared UI scene has no EventSystem.";
            if (!Application.isBatchMode && !suppressDialogs)
                EditorUtility.DisplayDialog("While You Were Away Popup", message, "OK");
            else
                Debug.LogWarning("Return popup rebuild skipped: " + message);

            if (openedForBuild)
                EditorSceneManager.CloseScene(uiScene, true);
            return;
        }

        TMP_FontAsset font = FindFont(canvas);
        Sprite hungerIcon = FindSceneSprite(canvas, "HungerIcon");
        Sprite thirstIcon = FindSceneSprite(canvas, "ThirstIcon");
        Sprite energyIcon = FindSceneSprite(canvas, "EnergyIcon");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build While You Were Away Popup");

        Transform existing = canvas.transform.Find(RootName);
        RemoveRoguePopupRoots(uiScene, existing != null ? existing.gameObject : null);
        GameObject root = existing != null
            ? PrepareExistingRoot(existing.gameObject)
            : CreateRect(RootName, canvas.transform);

        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup popupGroup = GetOrAdd<CanvasGroup>(root);
        popupGroup.alpha = 0f;
        popupGroup.interactable = false;
        popupGroup.blocksRaycasts = false;
        WhileYouWereAwayPopup controller = GetOrAdd<WhileYouWereAwayPopup>(root);
        WhileYouWereAwayPopupFx premiumFx = GetOrAdd<WhileYouWereAwayPopupFx>(root);

        Image overlay = CreateImage("WarmOverlay", root.transform, Overlay, true);
        Stretch(overlay.rectTransform);

        GameObject safeArea = CreateRect("SafeArea", root.transform);
        Stretch(safeArea.GetComponent<RectTransform>());
        safeArea.AddComponent<SafeAreaRect>();

        // The layout root is always centred on the actual device safe area. The
        // popup-local FX component only scales this root when the safe rectangle
        // is smaller than the 1040x760 design, so 1920x1080 remains pixel-stable.
        GameObject layoutRoot = CreateRect("CenteredSafeLayout", safeArea.transform);
        SetFixedRect(
            layoutRoot.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f),
            new Vector2(980f, 680f));

        GameObject animationContainer = CreateRect("AnimationContainer", layoutRoot.transform);
        Stretch(animationContainer.GetComponent<RectTransform>());
        CanvasGroup panelGroup = animationContainer.AddComponent<CanvasGroup>();

        var face=JoyfulUiArt.Panel("ReturnFace",animationContainer.transform,JoyfulUiArt.Paper,0,0,980,680,32,true);
        var banner=JoyfulUiArt.Panel("ReturnBanner",animationContainer.transform,JoyfulUiArt.Ocean,0,218,928,196,26);
        JoyfulUiArt.Motif(banner.transform,"WelcomeStars",0,0,874,166,new Color(1,1,1,.24f));
        var title=U.Label("Title",animationContainer.transform,font,43,Color.white,-84,251,650,70);
        TMP_Text titleShadow=null;
        JoyfulUiArt.Panel("PortraitMedallion",animationContainer.transform,JoyfulUiArt.Gold,366,228,140,140,48);
        var portrait=U.Rect("CatPortrait",animationContainer.transform); U.At(portrait,366,228,118,118);
        portrait.gameObject.AddComponent<Image>().raycastTarget=false; portrait.gameObject.AddComponent<SelectedCatPortrait>();
        var durationBadge=U.Rect("AwayDurationBadge",animationContainer.transform).gameObject;
        U.At((RectTransform)durationBadge.transform,-84,187,650,42);
        var duration=U.Label("Duration",durationBadge.transform,font,23,Color.white,0,0,650,42);
        var summaryBadge=U.Rect("ReturnSummaryBadge",animationContainer.transform).gameObject;
        U.At((RectTransform)summaryBadge.transform,0,43,872,116);
        var summaryFace=JoyfulUiArt.Panel("SummaryFace",summaryBadge.transform,JoyfulUiArt.SkyPaper,0,0,872,116,24);
        JoyfulUiArt.Sticker(summaryBadge.transform,"FriendBadge","Paw",JoyfulUiArt.Gold,-370,0,76,-8);
        var summary=U.Label("ReturnSummary",summaryBadge.transform,font,28,ContentText,42,0,702,94,TextAlignmentOptions.Center);
        Row hunger=CreateNeedSummary(animationContainer.transform,"HungerRewardCard","home.hunger",-294,hungerIcon,font);
        Row thirst=CreateNeedSummary(animationContainer.transform,"ThirstRewardCard","home.thirst",0,thirstIcon,font);
        Row energy=CreateNeedSummary(animationContainer.transform,"EnergyRewardCard","home.energy",294,energyIcon,font);
        var action=U.Action("WelcomeBackButton",animationContainer.transform,font,"return.continue",JoyfulUiArt.Coral,0,-254,480,80,out var actionLabel);
        actionLabel.fontSize=26;
        var button=new ButtonParts(action,actionLabel,(RectTransform)action.transform);

        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("openingDuration").floatValue = 0.28f;
        serialized.FindProperty("closingDuration").floatValue = 0.18f;
        serialized.FindProperty("openingScale").floatValue = 0.96f;
        Assign(serialized, "popupGroup", popupGroup);
        Assign(serialized, "panelGroup", panelGroup);
        Assign(serialized, "animationContainer", animationContainer.GetComponent<RectTransform>());
        Assign(serialized, "welcomeBackButton", button.Button);
        Assign(serialized, "premiumFx", premiumFx);
        Assign(serialized, "titleText", title);
        Assign(serialized, "titleShadowText", titleShadow);
        Assign(serialized, "titleHighlightText", null);
        Assign(serialized, "durationText", duration);
        Assign(serialized, "hungerText", hunger.Text);
        Assign(serialized, "thirstText", thirst.Text);
        Assign(serialized, "energyText", energy.Text);
        Assign(serialized, "summaryText", summary);
        Assign(serialized, "buttonText", button.Text);
        Assign(serialized, "buttonShadowText", null);
        Assign(serialized, "hungerRow", hunger.Group);
        Assign(serialized, "thirstRow", thirst.Group);
        Assign(serialized, "energyRow", energy.Group);
        serialized.FindProperty("needLabelsAreSeparate").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        premiumFx.EditorConfigure(
            safeArea.GetComponent<RectTransform>(),
            layoutRoot.GetComponent<RectTransform>(),
            animationContainer.GetComponent<RectTransform>(),
            durationBadge.GetComponent<RectTransform>(),
            summaryBadge.GetComponent<RectTransform>(),
            button.RevealRoot,
            new[] { hunger.Rect, thirst.Rect, energy.Rect },
            new[] { hunger.Text.rectTransform, thirst.Text.rectTransform, energy.Text.rectTransform },
            new[] { face, summaryFace },
            new RectTransform[0]);

        PremiumUiFactory.PolishHierarchy(root.transform, font);
        title.color=duration.color=Color.white;
        JoyfulUiArt.ActionStyle(action,JoyfulUiArt.Coral);
        ValidateGraphics(root);
        EnsureFolder();
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(uiScene);

        if (openedForBuild)
        {
            EditorSceneManager.SaveScene(uiScene);
            EditorSceneManager.CloseScene(uiScene, true);
        }
        else
        {
            Selection.activeGameObject = root;
        }

        string optionalWarning = font == null
            ? "\n\nFredoka TMP font was not found; TMP's fallback font will be used."
            : string.Empty;
        if (!suppressDialogs)
            EditorUtility.DisplayDialog(
                "While You Were Away Popup",
                (existing == null ? "Popup created" : "Existing popup repaired") +
                " under Canvas and prefab updated at Assets/UI/WhileYouWereAwayPopup.prefab." + optionalWarning,
                "OK"
            );
    }

    public static void BuildSilently()
    {
        suppressDialogs = true;
        try { Build(); }
        finally { suppressDialogs = false; }
    }

    private static Row CreateNeedSummary(Transform parent,string name,string key,float x,Sprite icon,TMP_FontAsset font)
    {
        Color paper=key=="home.hunger"?new Color32(255,232,196,255):key=="home.thirst"?JoyfulUiArt.SkyPaper:new Color32(237,225,251,255);
        var face=JoyfulUiArt.Panel(name,parent,paper,x,-122,272,130,22);
        var group=face.gameObject.AddComponent<CanvasGroup>();
        JoyfulUiArt.Icon("Icon",face.transform,key=="home.hunger"?"Food":key=="home.thirst"?"Water":"Energy",-96,23,58);
        U.Localize(U.Label("NeedLabel",face.transform,font,23,JoyfulUiArt.Ink,25,27,164,36),key);
        var value=U.Label("Value",face.transform,font,27,ContentText,0,-29,246,42,TextAlignmentOptions.Center);
        value.textWrappingMode=TextWrappingModes.NoWrap;
        value.enableAutoSizing=true;value.fontSizeMin=20;value.fontSizeMax=26;
        return new Row(group,value,face.rectTransform,face);
    }

    private static GameObject PrepareExistingRoot(GameObject root)
    {
        if (PrefabUtility.IsPartOfPrefabInstance(root) &&
            PrefabUtility.GetNearestPrefabInstanceRoot(root) == root)
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);

        RemoveDuplicateComponents<CanvasGroup>(root);
        RemoveDuplicateComponents<WhileYouWereAwayPopup>(root);
        RemoveDuplicateComponents<WhileYouWereAwayPopupFx>(root);
        return root;
    }

    private static Row CreateRewardCard(
        Transform parent,
        string name,
        string labelCopy,
        Vector2 min,
        Vector2 max,
        Color top,
        Color bottom,
        Sprite icon,
        string fallbackCopy,
        TMP_FontAsset font)
    {
        GameObject card = CreateRect(name, parent);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        SetAnchors(cardRect, min, max);
        CanvasGroup group = card.AddComponent<CanvasGroup>();

        LowPolyPanelGraphic depth = CreatePanel("CardDepth", card.transform, PremiumUiStyle.Shadow, 25f, 0f);
        PremiumUiStyle.SetCenteredShadowStretch(depth.rectTransform, 5f);
        LowPolyPanelGraphic rim = CreateGradientPanel(
            "GoldRewardRim", card.transform,
            PremiumUiStyle.ChampagneLight, PremiumUiStyle.Champagne, 25f, 3f);
        Stretch(rim.rectTransform);
        LowPolyPanelGraphic face = CreateGradientPanel(
            "RewardFace", card.transform, top, bottom, 22f, 4f);
        StretchWithOffsets(face.rectTransform, 5f, 5f, -5f, -5f);

        Image glossBand = CreateImage(
            "TopGlossBand", card.transform, new Color32(255, 255, 255, 62), false);
        SetAnchors(glossBand.rectTransform, new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.91f));

        LowPolyPanelGraphic iconWell = CreateGradientPanel(
            "IconWell", card.transform,
            new Color32(255, 255, 255, 232), new Color32(255, 237, 211, 242), 31f, 3f);
        SetAnchors(iconWell.rectTransform, new Vector2(0.35f, 0.54f), new Vector2(0.65f, 0.91f));

        if (icon != null)
        {
            Image image = CreateImage("NeedIcon", card.transform, Color.white, false);
            image.sprite = icon;
            image.preserveAspect = true;
            SetAnchors(image.rectTransform, new Vector2(0.39f, 0.59f), new Vector2(0.61f, 0.86f));
        }
        else
        {
            TMP_Text fallback = CreateText(
                "IconFallback", card.transform, font, 34f, DarkText, TextAlignmentOptions.Center);
            fallback.text = fallbackCopy;
            SetAnchors(fallback.rectTransform, new Vector2(0.39f, 0.59f), new Vector2(0.61f, 0.86f));
        }

        TMP_Text label = CreateText(
            "NeedLabel", card.transform, font, 23f, LightText, TextAlignmentOptions.Center);
        label.text = labelCopy;
        SetAnchors(label.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.55f));

        TMP_Text text = CreateText(
            "NeedValues", card.transform, font, 24f, LightText, TextAlignmentOptions.Center);
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = 26f;
        SetAnchors(text.rectTransform, new Vector2(0.055f, 0.08f), new Vector2(0.945f, 0.36f));

        CreateSparkle(
            card.transform, "CardGlint", new Vector2(0.84f, 0.72f),
            new Vector2(22f, 22f), Color.white, 1.15f);
        return new Row(group, text, cardRect, face);
    }

    private static ButtonParts CreateButton(Transform parent, TMP_FontAsset font)
    {
        GameObject reveal = CreateRect("ActionReveal", parent);
        SetAnchors(reveal.GetComponent<RectTransform>(), new Vector2(0.24f, 0.035f), new Vector2(0.76f, 0.19f));

        GameObject root = CreateRect("WelcomeBackButton", reveal.transform);
        Stretch(root.GetComponent<RectTransform>());

        GameObject face = CreateRect("ButtonFace", root.transform);
        StretchWithOffsets(face.GetComponent<RectTransform>(), 0f, 5f, 0f, 5f);
        EnsureCanvasRenderer(face);
        LowPolyPanelGraphic faceGraphic = face.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(
            faceGraphic, new Color32(255, 156, 91, 255), PremiumUiStyle.CandyPink, 42f, 5f);
        faceGraphic.raycastTarget = true;

        TMP_Text text = CreateText("ButtonText", face.transform, font, 35f, LightText, TextAlignmentOptions.Center);
        text.text = "WELCOME BACK!";
        Stretch(text.rectTransform);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = faceGraphic;
        button.transition = Selectable.Transition.None;

        LowPolyButtonPress press = root.AddComponent<LowPolyButtonPress>();
        SerializedObject serializedPress = new SerializedObject(press);
        Assign(serializedPress, "face", face.GetComponent<RectTransform>());
        serializedPress.FindProperty("pressedOffset").floatValue = 6f;
        serializedPress.ApplyModifiedPropertiesWithoutUndo();
        return new ButtonParts(button, text, reveal.GetComponent<RectTransform>());
    }

    private static LowPolyPanelGraphic CreatePanel(string name, Transform parent, Color color, float cornerCut, float bevel)
    {
        GameObject gameObject = CreateRect(name, parent);
        EnsureCanvasRenderer(gameObject);
        LowPolyPanelGraphic graphic = gameObject.AddComponent<LowPolyPanelGraphic>();
        graphic.color = color;
        graphic.raycastTarget = false;
        PremiumUiStyle.ConfigureSurface(graphic, color, cornerCut, bevel);
        return graphic;
    }

    private static LowPolyPanelGraphic CreateGradientPanel(
        string name,
        Transform parent,
        Color top,
        Color bottom,
        float cornerCut,
        float bevel)
    {
        GameObject gameObject = CreateRect(name, parent);
        EnsureCanvasRenderer(gameObject);
        LowPolyPanelGraphic graphic = gameObject.AddComponent<LowPolyPanelGraphic>();
        graphic.raycastTarget = false;
        PremiumUiStyle.ConfigureAccentSurface(graphic, top, bottom, cornerCut, bevel);
        return graphic;
    }

    private static RectTransform CreatePawSeal(Transform parent)
    {
        GameObject root = CreateRect("PawWelcomeSeal", parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetFixedRect(rect, new Vector2(0.075f, 0.895f), new Vector2(106f, 106f));

        LowPolyPanelGraphic depth = CreatePanel(
            "SealDepth", root.transform, PremiumUiStyle.Shadow, 53f, 0f);
        PremiumUiStyle.SetCenteredShadowStretch(depth.rectTransform, 5f);
        LowPolyPanelGraphic rim = CreateGradientPanel(
            "SealGoldRim", root.transform,
            PremiumUiStyle.ChampagneLight, PremiumUiStyle.Champagne, 53f, 3f);
        Stretch(rim.rectTransform);
        LowPolyPanelGraphic well = CreateGradientPanel(
            "SealBerryWell", root.transform,
            PremiumUiStyle.CandyPink, PremiumUiStyle.CandyGrape, 47f, 3f);
        StretchWithOffsets(well.rectTransform, 7f, 7f, -7f, -7f);

        LowPolyPanelGraphic toeA = CreateGradientPanel(
            "PawToeA", root.transform, Color.white, PremiumUiStyle.CandyCloud, 14f, 2f);
        SetAnchors(toeA.rectTransform, new Vector2(0.18f, 0.57f), new Vector2(0.39f, 0.81f));
        toeA.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 15f);
        LowPolyPanelGraphic toeB = CreateGradientPanel(
            "PawToeB", root.transform, Color.white, PremiumUiStyle.CandyCloud, 14f, 2f);
        SetAnchors(toeB.rectTransform, new Vector2(0.4f, 0.65f), new Vector2(0.6f, 0.89f));
        LowPolyPanelGraphic toeC = CreateGradientPanel(
            "PawToeC", root.transform, Color.white, PremiumUiStyle.CandyCloud, 14f, 2f);
        SetAnchors(toeC.rectTransform, new Vector2(0.61f, 0.57f), new Vector2(0.82f, 0.81f));
        toeC.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -15f);
        LowPolyPanelGraphic pad = CreateGradientPanel(
            "PawPad", root.transform, Color.white, PremiumUiStyle.CandyCloud, 23f, 2f);
        SetAnchors(pad.rectTransform, new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.61f));

        PremiumAmbientSparkle motion = root.AddComponent<PremiumAmbientSparkle>();
        motion.EditorConfigure(1.05f, 0.025f, 2.5f, 0.7f);
        return rect;
    }

    private static RectTransform CreateSparkle(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 size,
        Color color,
        float speed)
    {
        GameObject root = CreateRect(name, parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        SetFixedRect(rect, anchor, size);

        Image vertical = CreateImage("VerticalRay", root.transform, color, false);
        SetAnchors(vertical.rectTransform, new Vector2(0.43f, 0.02f), new Vector2(0.57f, 0.98f));
        Image horizontal = CreateImage("HorizontalRay", root.transform, color, false);
        SetAnchors(horizontal.rectTransform, new Vector2(0.02f, 0.43f), new Vector2(0.98f, 0.57f));
        LowPolyPanelGraphic centre = CreateGradientPanel(
            "GlintCentre", root.transform, Color.white, color, 8f, 1f);
        SetAnchors(centre.rectTransform, new Vector2(0.31f, 0.31f), new Vector2(0.69f, 0.69f));
        centre.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        PremiumAmbientSparkle motion = root.AddComponent<PremiumAmbientSparkle>();
        motion.EditorConfigure(speed, 0.12f, 10f, 1.4f);
        return rect;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
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
        {
            EnsureCanvasRenderer(graphic.gameObject);
            Button owner = graphic.GetComponentInParent<Button>();
            graphic.raycastTarget = graphic.name == "WarmOverlay" ||
                                    (owner != null && owner.targetGraphic == graphic);
        }

        Button button = root.GetComponentInChildren<Button>(true);
        if (button == null || button.targetGraphic == null || !button.targetGraphic.raycastTarget ||
            button.targetGraphic.GetComponent<CanvasRenderer>() == null)
        {
            throw new System.InvalidOperationException(
                "While You Were Away Popup: the welcome button has no valid CanvasRenderer-backed targetGraphic."
            );
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

    private static T FindRootComponent<T>(Scene scene, string rootName) where T : Component
    {
        if (!scene.IsValid())
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
                return root.GetComponent<T>();
        }

        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid())
            return null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void RemoveRoguePopupRoots(Scene scene, GameObject canonicalRoot)
    {
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            WhileYouWereAwayPopup[] popups =
                sceneRoot.GetComponentsInChildren<WhileYouWereAwayPopup>(true);
            for (int i = 0; i < popups.Length; i++)
            {
                WhileYouWereAwayPopup popup = popups[i];
                if (popup == null || popup.gameObject == canonicalRoot)
                    continue;

                // Delete only the rogue popup object. Never delete its containing
                // canvas/prefab root: the bad object may be nested inside the
                // currency, quest or menu canvas.
                Undo.DestroyObjectImmediate(popup.gameObject);
            }
        }
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2? position = null)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = position ?? Vector2.zero;
    }

    private static void SetFixedRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 size,
        Vector2? position = null)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position ?? Vector2.zero;
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

    private static TMP_FontAsset FindFont(Canvas canvas)
    {
        TMP_FontAsset premium = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        if (premium != null)
            return premium;
        TMP_Text existing = canvas.GetComponentInChildren<TMP_Text>(true);
        if (existing != null && existing.font != null)
            return existing.font;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Fredoka-SemiBold SDF.asset");
    }

    private static Sprite FindSceneSprite(Canvas canvas, string objectName)
    {
        foreach (Image image in canvas.GetComponentsInChildren<Image>(true))
        {
            if (image.name == objectName && image.sprite != null)
                return image.sprite;
        }
        Debug.LogWarning($"While You Were Away Popup: {objectName} sprite was not found; a text facet will be used.");
        return null;
    }

    private static void Assign(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new System.InvalidOperationException("Missing serialized popup property: " + propertyName);
        property.objectReferenceValue = value;
    }

    private static void EnsureFolder()
    {
        if (!Directory.Exists(PrefabFolder))
            AssetDatabase.CreateFolder("Assets", "UI");
    }

    private readonly struct Row
    {
        public Row(
            CanvasGroup group,
            TMP_Text text,
            RectTransform rect,
            LowPolyPanelGraphic face)
        {
            Group = group;
            Text = text;
            Rect = rect;
            Face = face;
        }

        public CanvasGroup Group { get; }
        public TMP_Text Text { get; }
        public RectTransform Rect { get; }
        public LowPolyPanelGraphic Face { get; }
    }

    private readonly struct ButtonParts
    {
        public ButtonParts(Button button, TMP_Text text, RectTransform revealRoot)
        {
            Button = button;
            Text = text;
            RevealRoot = revealRoot;
        }

        public Button Button { get; }
        public TMP_Text Text { get; }
        public RectTransform RevealRoot { get; }
    }
}
