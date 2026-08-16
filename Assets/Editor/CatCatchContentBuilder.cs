using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Authors the Cat Catch mini-game scene and wires it into the home Games hub.
/// </summary>
public static class CatCatchContentBuilder
{
    public const string ScenePath = CatCatchLauncher.CatchScenePath;
    private const string CatPrefabPath =
        "Assets/PolyOne/Cartoon Dog, Cat/Prefab/SM_CartoonAnimal_Cat.prefab";
    private const string MaterialFolder = "Assets/Art/Catch/Materials";

    private static readonly Color Cream = new Color32(255, 247, 224, 255);
    private static readonly Color Aqua = new Color32(67, 220, 211, 255);
    private static readonly Color Mint = new Color32(126, 235, 190, 255);
    private static readonly Color Coral = new Color32(255, 120, 130, 255);
    private static readonly Color Lilac = new Color32(188, 143, 235, 255);
    private static readonly Color Lemon = new Color32(255, 222, 94, 255);
    private static readonly Color Ink = new Color32(63, 47, 80, 255);
    private static readonly Color Peach = new Color32(255, 177, 120, 255);

    // The welcome screen is deliberately built from the same tokens as the Cat
    // Runner welcome so the two mini-games read as one product: same card cream,
    // same gold hero frame, same grape best-score pill, same teal exit button.
    private static readonly Color CardCream = new Color32(255, 242, 211, 255);
    private static readonly Color CardDark = new Color32(64, 50, 64, 255);
    private static readonly Color TealDark = new Color32(31, 100, 108, 255);
    private static readonly Color Teal = new Color32(47, 151, 149, 255);
    private static readonly Color Grape = new Color32(100, 59, 167, 255);
    private static readonly Color Gold = new Color32(255, 196, 42, 255);
    private static readonly Color FrameGold = new Color32(255, 205, 64, 255);
    // Same pink header band as Cat Runner welcome — teal made Catch look like
    // a different product when the two screens sat next to each other.
    private static readonly Color HeaderPink = new Color32(255, 101, 154, 255);
    private static readonly Color Orange = new Color32(238, 126, 48, 255);

    private const string CatchHeroPath = "Assets/Art/Catch/UI/CatCatchHero_v1.png";

    [MenuItem("Tools/Cat Home/Games/Build Cat Catch")]
    public static void BuildFromMenu()
    {
        EditorUtility.DisplayDialog("Cat Catch", BuildSilently(), "OK");
    }

    public static string BuildSilently()
    {
        EnsureFolders();
        Dictionary<string, Material> materials = BuildMaterials();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("CatCatchRoot");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector3(40f, 1000f, 0f);

        CatCatchGameController game = root.AddComponent<CatCatchGameController>();
        Camera camera = BuildCamera(root.transform);
        AudioListener listener = camera.GetComponent<AudioListener>();
        BuildLighting(root.transform);
        BuildArena(root.transform, materials);
        Transform spawn = CreatePoint(root.transform, "CatSpawn", new Vector3(0f, 0f, -1.6f));
        CatCatchPlayer player = BuildPlayer(root.transform, spawn, camera);
        CatCatchMouse[] mice = BuildMice(root.transform, materials, player.transform);
        // Keep hand-authored / AI premium hero art when present; only bake a
        // procedural arena still as a fallback so rebuilds never wipe the card.
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(CatchHeroPath) == null)
            BakeWelcomeHero(root.transform, camera, player, mice);
        BuildUi(
            root.transform,
            game,
            out Canvas canvas,
            out GameObject welcome,
            out TMP_Text welcomeBest,
            out TMP_Text welcomeLives,
            out Button start,
            out Button exit,
            out Button rewarded,
            out GameObject hud,
            out TMP_Text score,
            out TMP_Text comboLabel,
            out TMP_Text timer,
            out TMP_Text caught,
            out GameObject results,
            out TMP_Text title,
            out TMP_Text details,
            out Button collect,
            out Button retry,
            out GameObject tutorial,
            out TMP_Text tutorialMessage,
            out Button tutorialSkip,
            out Button pause,
            out GameObject pausePanel,
            out Button resume,
            out Button pauseExit);

        game.EditorConfigure(
            player, mice, spawn, camera, listener, canvas,
            welcome, welcomeBest, welcomeLives, start, exit, rewarded,
            hud, score, comboLabel, timer, caught, results, title, details, collect, retry,
            tutorial, tutorialMessage, tutorialSkip, pause, pausePanel, resume, pauseExit);

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Cat Catch scene could not be saved.");
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        return "Cat Catch mini-game built with tap-to-pounce, pause/exit, and independent lives.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Catch_Cream", Cream, .03f, .54f),
            ["Aqua"] = EnsureMaterial("Catch_Aqua", Aqua, .02f, .62f),
            ["Mint"] = EnsureMaterial("Catch_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Catch_Coral", Coral, .02f, .58f),
            ["Lilac"] = EnsureMaterial("Catch_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Catch_Lemon", Lemon, .03f, .6f),
            ["Peach"] = EnsureMaterial("Catch_Peach", Peach, .02f, .56f),
            ["Ink"] = EnsureMaterial("Catch_Ink", Ink, .05f, .36f)
        };
    }

    private static Material EnsureMaterial(string name, Color color, float metallic, float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Camera BuildCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("CatchCamera", typeof(Camera),
            typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0f, 7.2f, -6.4f);
        cameraObject.transform.localRotation = Quaternion.Euler(48f, 0f, 0f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = 42f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(155, 232, 255, 0);
        camera.allowHDR = true;
        camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        if (camera.GetComponent<PhysicsRaycaster>() == null)
            cameraObject.AddComponent<PhysicsRaycaster>();
        return camera;
    }

    private static void BuildLighting(Transform parent)
    {
        GameObject key = new GameObject("Catch Key Light", typeof(Light));
        key.transform.SetParent(parent, false);
        key.transform.rotation = Quaternion.Euler(50f, -20f, 0f);
        Light light = key.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = Cream;
        light.intensity = 1.05f;
        light.shadows = LightShadows.Soft;
    }

    private static void BuildArena(Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform arena = CreatePoint(parent, "CatchArena", Vector3.zero);
        CreateBlock(arena, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 6f),
            materials["Cream"]);
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 6; z++)
            {
                Material tile = ((x + z) & 1) == 0 ? materials["Mint"] : materials["Lilac"];
                CreateBlock(arena, $"Tile_{x}_{z}",
                    new Vector3(-3.5f + x, 0.01f, -2.5f + z),
                    new Vector3(0.96f, 0.02f, 0.96f), tile);
            }
        }
        CreateBlock(arena, "BackWall", new Vector3(0f, 1.5f, 2.9f),
            new Vector3(8f, 3f, 0.2f), materials["Peach"]);
        CreateBlock(arena, "LeftWall", new Vector3(-3.9f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 6f), materials["Aqua"]);
        CreateBlock(arena, "RightWall", new Vector3(3.9f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 6f), materials["Coral"]);
        CreateSphere(arena, "PawPad", new Vector3(0f, 2.15f, 2.78f),
            new Vector3(0.42f, 0.32f, 0.08f), materials["Lemon"]);
    }

    private static CatCatchPlayer BuildPlayer(Transform parent, Transform spawn, Camera camera)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPrefabPath);
        if (prefab == null)
            throw new InvalidOperationException("Cat prefab missing for Cat Catch.");
        // The cat prefab carries its Animator on its own root, and the clips
        // animate that root's transform. Moving it directly means the Animator
        // rewrites the position every frame and the cat sticks in place, so the
        // hunt drives a wrapper and the animated model rides underneath it.
        GameObject cat = new GameObject("CatchCat");
        cat.transform.SetParent(parent, false);
        cat.transform.localPosition = spawn.localPosition;
        cat.transform.localRotation = Quaternion.identity;

        GameObject model = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        model.name = "CatModel";
        model.transform.SetParent(cat.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        // Left at one on purpose: the cat's Idle and Run clips drive the root
        // scale to one, so any other authored value is overwritten the moment
        // the Animator plays and only shows up as a pop mid-animation.
        model.transform.localScale = Vector3.one;
        // The hunt drives the cat directly across a flat arena, so every collider
        // and the shared prefab's CharacterController are stripped: left enabled,
        // the controller fights the hunt's movement and pins the cat in place.
        CharacterController controller = model.GetComponent<CharacterController>();
        if (controller != null)
            UnityEngine.Object.DestroyImmediate(controller);
        Collider[] catColliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < catColliders.Length; i++)
        {
            if (catColliders[i] != null)
                UnityEngine.Object.DestroyImmediate(catColliders[i]);
        }
        CatCatchPlayer player = cat.AddComponent<CatCatchPlayer>();
        player.EditorBind(model.GetComponentInChildren<Animator>(true), camera);
        return player;
    }

    private static CatCatchMouse[] BuildMice(
        Transform parent, IReadOnlyDictionary<string, Material> materials, Transform cat)
    {
        var mice = new CatCatchMouse[5];
        Vector3 minLocal = new Vector3(-2.45f, 0.12f, -1.7f);
        Vector3 maxLocal = new Vector3(2.45f, 0.12f, 1.7f);
        for (int i = 0; i < mice.Length; i++)
        {
            Transform mouseRoot = CreatePoint(parent, "CatchMouse_" + (i + 1),
                new Vector3(-2f + i, 0.12f, 0.4f));
            CreateSphere(mouseRoot, "Body", Vector3.zero, new Vector3(0.38f, 0.26f, 0.52f),
                materials["Ink"]);
            CreateSphere(mouseRoot, "Belly", new Vector3(0f, -0.02f, 0.04f),
                new Vector3(0.28f, 0.16f, 0.34f), materials["Peach"]);
            CreateSphere(mouseRoot, "EarL", new Vector3(-0.12f, 0.16f, -0.06f),
                new Vector3(0.12f, 0.16f, 0.07f), materials["Coral"]);
            CreateSphere(mouseRoot, "EarR", new Vector3(0.12f, 0.16f, -0.06f),
                new Vector3(0.12f, 0.16f, 0.07f), materials["Coral"]);
            CreateSphere(mouseRoot, "Nose", new Vector3(0f, 0.04f, 0.24f),
                Vector3.one * 0.09f, materials["Lemon"]);
            CreateSphere(mouseRoot, "Tail", new Vector3(0f, 0.06f, -0.28f),
                new Vector3(0.08f, 0.08f, 0.28f), materials["Lilac"]);
            CatCatchMouse mouse = mouseRoot.gameObject.AddComponent<CatCatchMouse>();
            mouse.Configure(parent, minLocal, maxLocal, cat);
            mouse.Hide();
            mice[i] = mouse;
        }
        return mice;
    }

    private static void BuildUi(
        Transform parent,
        CatCatchGameController game,
        out Canvas canvas,
        out GameObject welcome,
        out TMP_Text welcomeBest,
        out TMP_Text welcomeLives,
        out Button start,
        out Button exit,
        out Button rewarded,
        out GameObject hud,
        out TMP_Text score,
        out TMP_Text combo,
        out TMP_Text timer,
        out TMP_Text caught,
        out GameObject results,
        out TMP_Text title,
        out TMP_Text details,
        out Button collect,
        out Button retry,
        out GameObject tutorial,
        out TMP_Text tutorialMessage,
        out Button tutorialSkip,
        out Button pause,
        out GameObject pausePanel,
        out Button resume,
        out Button pauseExit)
    {
        GameObject canvasObject = new GameObject(
            "CatchCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject safe = CreateUi("SafeArea", canvasObject.transform);
        Stretch(safe.GetComponent<RectTransform>());
        safe.AddComponent<SafeAreaRect>();

        hud = CreateUi("HuntHud", safe.transform);
        Stretch(hud.GetComponent<RectTransform>());
        GameObject catcher = CreateUi("PounceCatcher", hud.transform);
        Stretch(catcher.GetComponent<RectTransform>());
        Image catcherImage = catcher.AddComponent<Image>();
        catcherImage.color = new Color(1f, 1f, 1f, 0f);
        catcherImage.raycastTarget = true;
        CatchPounceInput pounceInput = catcher.AddComponent<CatchPounceInput>();
        pounceInput.EditorBind(game);
        score = CreateStatPill(
            hud.transform, "ScorePill", "SCORE", "0", Lemon, Ink,
            new Vector2(0f, 1f), new Vector2(196f, -64f), new Vector2(300f, 96f), 38f);
        combo = CreateLabel(hud.transform, "ComboLabel", string.Empty, 24f, Coral);
        SetAnchored(combo.rectTransform, new Vector2(0f, 1f),
            new Vector2(196f, -176f), new Vector2(300f, 34f));
        combo.alignment = TextAlignmentOptions.Center;
        combo.raycastTarget = false;

        timer = CreateStatPill(
            hud.transform, "TimerPill", "TIME LEFT", "60", Coral, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(240f, 96f), 44f);

        caught = CreateStatPill(
            hud.transform, "CatchPill", "MICE  •  COINS", "0  •  0", Aqua, Ink,
            new Vector2(1f, 1f), new Vector2(-306f, -64f), new Vector2(340f, 96f), 32f);

        GameObject hintPill = CreatePremium(hud.transform, "HintPill", Cream, 20f);
        SetAnchored(hintPill.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0f, 56f), new Vector2(820f, 56f));
        TMP_Text hint = CreateLabel(hintPill.transform, "Hint",
            "TAP A MOUSE  •  YOUR CAT CHASES IT DOWN AND POUNCES", 20f, Ink);
        Stretch(hint.rectTransform);
        hint.alignment = TextAlignmentOptions.Center;
        hint.raycastTarget = false;
        pause = CreateButton(hud.transform, "PauseButton", "II", Lilac, new Vector2(76f, 70f));
        SetAnchored(pause.GetComponent<RectTransform>(), new Vector2(1f, 1f),
            new Vector2(-52f, -58f), new Vector2(76f, 70f));
        TMP_Text pauseLabel = pause.GetComponentInChildren<TMP_Text>(true);
        if (pauseLabel != null)
            pauseLabel.fontSize = 28f;

        // Deliberately the Cat Runner welcome geometry: same scrim, same candy
        // ribbons, same 1100x770 card, same left hero frame and right action
        // column, so switching between the two mini-games is not a style jump.
        welcome = CreateUi("WelcomePanel", canvasObject.transform);
        Stretch(welcome.GetComponent<RectTransform>());
        Image welcomeScrim = welcome.AddComponent<Image>();
        welcomeScrim.color = new Color32(52, 44, 137, 238);
        GameObject candyLeft = CreateFlatPanel(
            welcome.transform, "CandyGlowLeft", new Color32(54, 227, 216, 118));
        SetAnchored(candyLeft.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
            new Vector2(85f, 0f), new Vector2(390f, 1240f));
        candyLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -17f);
        GameObject candyRight = CreateFlatPanel(
            welcome.transform, "CandyGlowRight", new Color32(255, 100, 177, 124));
        SetAnchored(candyRight.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
            new Vector2(-70f, 0f), new Vector2(390f, 1240f));
        candyRight.transform.localRotation = Quaternion.Euler(0f, 0f, 17f);

        GameObject welcomeSafe = CreateUi("WelcomeSafeArea", welcome.transform);
        Stretch(welcomeSafe.GetComponent<RectTransform>());
        welcomeSafe.AddComponent<SafeAreaRect>();
        GameObject welcomeLayout = CreateUi("WelcomeCardLayout", welcomeSafe.transform);
        SetAnchored(welcomeLayout.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1120f, 790f));

        GameObject welcomeGlow = CreatePremium(
            welcomeLayout.transform, "WelcomeCardGlow", new Color32(255, 221, 86, 220), 48f);
        SetAnchored(welcomeGlow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1120f, 790f));
        GameObject welcomeCard = CreatePremium(
            welcomeLayout.transform, "WelcomeCard", CardCream, 42f);
        SetAnchored(welcomeCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1100f, 770f));

        GameObject cardHeader = CreatePremium(
            welcomeCard.transform, "CardHeader", HeaderPink, 34f);
        RectTransform cardHeaderRect = cardHeader.GetComponent<RectTransform>();
        cardHeaderRect.anchorMin = new Vector2(0f, 1f);
        cardHeaderRect.anchorMax = new Vector2(1f, 1f);
        cardHeaderRect.pivot = new Vector2(0.5f, 1f);
        cardHeaderRect.sizeDelta = new Vector2(0f, 170f);
        cardHeaderRect.anchoredPosition = Vector2.zero;
        TMP_Text eyebrow = CreateLabel(
            cardHeader.transform, "Eyebrow", "A SIXTY SECOND MOUSE HUNT", 20f, Cream);
        SetAnchored(eyebrow.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(600f, 30f));
        eyebrow.alignment = TextAlignmentOptions.Center;
        eyebrow.fontStyle = FontStyles.Bold;
        TMP_Text welcomeTitle = CreateLabel(
            cardHeader.transform, "WelcomeTitle", "CAT CATCH", 64f, Cream);
        SetAnchored(welcomeTitle.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(0f, -100f), new Vector2(680f, 82f));
        welcomeTitle.alignment = TextAlignmentOptions.Center;
        welcomeTitle.fontStyle = FontStyles.Bold;

        BuildWelcomeHero(welcomeCard.transform);

        // Right-column anchors mirror Cat Runner welcome exactly so the two
        // screens share one vertical rhythm (tagline → best → lives → rules →
        // start → exit → controls) with no stacked overlaps.
        TMP_Text tagline = CreateLabel(welcomeCard.transform, "Tagline",
            "CHASE THE MICE  •  POUNCE TO CATCH  •  BEAT YOUR BEST", 20f, CardDark);
        SetAnchored(tagline.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(270f, -230f), new Vector2(470f, 66f));
        tagline.alignment = TextAlignmentOptions.Center;
        tagline.fontStyle = FontStyles.Bold;

        GameObject bestPill = CreatePremium(welcomeCard.transform, "BestScorePill", Grape, 28f);
        SetAnchored(bestPill.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
            new Vector2(270f, -326f), new Vector2(430f, 82f));
        welcomeBest = CreateLabel(bestPill.transform, "BestScore", "BEST SCORE   0", 31f, Gold);
        Stretch(welcomeBest.rectTransform);
        welcomeBest.alignment = TextAlignmentOptions.Center;
        welcomeBest.fontStyle = FontStyles.Bold;

        welcomeLives = CreateLabel(welcomeCard.transform, "WelcomeLives",
            "1 LIFE PER HUNT", 18f, TealDark);
        SetAnchored(welcomeLives.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(270f, -394f), new Vector2(470f, 44f));
        welcomeLives.alignment = TextAlignmentOptions.Center;
        welcomeLives.fontStyle = FontStyles.Bold;

        GameObject rulesPill = CreatePremium(
            welcomeCard.transform, "HuntRulesPill", new Color32(91, 210, 191, 255), 24f);
        SetAnchored(rulesPill.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
            new Vector2(270f, -475f), new Vector2(470f, 80f));
        TMP_Text rules = CreateLabel(rulesPill.transform, "HuntRules",
            "60 SECONDS  •  5 MICE ON THE FLOOR  •  COMBO BONUS", 15f, CardDark);
        StretchWithOffsets(rules.rectTransform, 12f, 6f, -12f, -6f);
        rules.alignment = TextAlignmentOptions.Center;
        rules.fontStyle = FontStyles.Bold;

        start = CreateButton(welcomeCard.transform, "WelcomeStartButton", "START HUNT",
            Orange, new Vector2(430f, 94f));
        SetAnchored(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(270f, 175f), new Vector2(430f, 94f));
        start.GetComponentInChildren<TMP_Text>(true).fontSize = 36f;
        rewarded = CreateButton(welcomeCard.transform, "WelcomeRewardedButton",
            "WATCH  •  LIVES +2", Teal, new Vector2(430f, 94f));
        SetAnchored(rewarded.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(270f, 175f), new Vector2(430f, 94f));
        rewarded.GetComponentInChildren<TMP_Text>(true).fontSize = 28f;
        rewarded.gameObject.SetActive(false);
        exit = CreateButton(welcomeCard.transform, "WelcomeExitButton", "EXIT TO MAIN MENU",
            Teal, new Vector2(430f, 76f));
        SetAnchored(exit.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(270f, 80f), new Vector2(430f, 76f));
        TMP_Text controls = CreateLabel(welcomeCard.transform, "Controls",
            "TAP A MOUSE  •  YOUR CAT CHASES IT DOWN AND POUNCES", 14f, CardDark);
        SetAnchored(controls.rectTransform, new Vector2(0.5f, 0f),
            new Vector2(270f, 23f), new Vector2(480f, 34f));
        controls.alignment = TextAlignmentOptions.Center;

        results = CreateUi("ResultsPanel", canvasObject.transform);
        Stretch(results.GetComponent<RectTransform>());
        Image resultScrim = results.AddComponent<Image>();
        resultScrim.color = new Color32(70, 34, 132, 232);
        GameObject resultRibbonLeft = CreateFlatPanel(
            results.transform, "ResultCandyRibbonLeft", new Color32(47, 233, 218, 108));
        SetAnchored(resultRibbonLeft.GetComponent<RectTransform>(), new Vector2(0f, 0.5f),
            new Vector2(100f, 0f), new Vector2(430f, 1260f));
        resultRibbonLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject resultRibbonRight = CreateFlatPanel(
            results.transform, "ResultCandyRibbonRight", new Color32(255, 100, 177, 112));
        SetAnchored(resultRibbonRight.GetComponent<RectTransform>(), new Vector2(1f, 0.5f),
            new Vector2(-86f, 0f), new Vector2(430f, 1260f));
        resultRibbonRight.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        GameObject resultShadow = CreatePremium(
            results.transform, "ResultsCardShadow", new Color32(255, 221, 86, 220), 46f);
        SetAnchored(resultShadow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(736f, 676f));
        GameObject resultCard = CreatePremium(results.transform, "ResultsCard", CardCream, 46f);
        SetAnchored(resultCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(720f, 660f));

        GameObject titleBanner = CreatePremium(resultCard.transform, "ResultBanner", Lemon, 28f);
        SetAnchored(titleBanner.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
            new Vector2(0f, -70f), new Vector2(600f, 104f));
        title = CreateLabel(titleBanner.transform, "ResultTitle", "MIGHTY HUNTER!", 44f, Ink);
        Stretch(title.rectTransform);
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;

        GameObject detailPanel = CreatePremium(resultCard.transform, "ResultDetailPanel", Aqua, 26f);
        SetAnchored(detailPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
            new Vector2(0f, -196f), new Vector2(600f, 232f));
        details = CreateLabel(detailPanel.transform, "ResultDetails", string.Empty, 27f, Ink);
        SetAnchored(details.rectTransform, new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(548f, 200f));
        details.alignment = TextAlignmentOptions.Center;
        details.lineSpacing = 22f;

        collect = CreateButton(resultCard.transform, "CollectButton", "COLLECT  •  HOME", Orange,
            new Vector2(470f, 96f));
        SetAnchored(collect.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0f, 124f), new Vector2(470f, 96f));
        retry = CreateButton(resultCard.transform, "RetryButton", "HUNT AGAIN", Teal,
            new Vector2(470f, 78f));
        SetAnchored(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0f, 32f), new Vector2(470f, 78f));

        tutorial = CreateUi("CatchTutorialPanel", canvasObject.transform);
        Stretch(tutorial.GetComponent<RectTransform>());
        GameObject tutorialSafe = CreateUi("TutorialSafeArea", tutorial.transform);
        Stretch(tutorialSafe.GetComponent<RectTransform>());
        tutorialSafe.AddComponent<SafeAreaRect>();
        GameObject tutorialLayout = CreateUi("TutorialCardLayout", tutorialSafe.transform);
        SetAnchored(tutorialLayout.GetComponent<RectTransform>(), new Vector2(0.5f, 1f),
            new Vector2(0f, -228f), new Vector2(760f, 194f));
        GameObject tutorialGlow = CreatePremium(tutorialLayout.transform, "TutorialGlow",
            Lemon, 30f);
        SetAnchored(tutorialGlow.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(760f, 194f));
        GameObject tutorialCard = CreatePremium(tutorialLayout.transform, "TutorialCard",
            Cream, 28f);
        SetAnchored(tutorialCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(744f, 180f));
        tutorialMessage = CreateLabel(tutorialCard.transform, "TutorialMessage",
            "TAP THE FLOOR\nYOUR CAT POUNCES THERE", 28f, Ink);
        SetAnchored(tutorialMessage.rectTransform, new Vector2(0.44f, 0.5f),
            new Vector2(-34f, 0f), new Vector2(500f, 136f));
        tutorialMessage.alignment = TextAlignmentOptions.Center;
        tutorialMessage.raycastTarget = false;
        tutorialSkip = CreateButton(tutorialCard.transform, "TutorialSkipButton", "SKIP",
            Lilac, new Vector2(150f, 60f));
        SetAnchored(tutorialSkip.GetComponent<RectTransform>(), new Vector2(0.86f, 0.5f),
            Vector2.zero, new Vector2(150f, 60f));

        pausePanel = CreateUi("PausePanel", canvasObject.transform);
        Stretch(pausePanel.GetComponent<RectTransform>());
        Image pauseScrim = pausePanel.AddComponent<Image>();
        pauseScrim.color = new Color32(52, 44, 137, 230);
        GameObject pauseCard = CreatePremium(pausePanel.transform, "PauseCard", CardCream, 42f);
        SetAnchored(pauseCard.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(680f, 520f));
        TMP_Text pauseTitle = CreateLabel(pauseCard.transform, "PauseTitle", "HUNT PAUSED", 46f, Ink);
        SetAnchored(pauseTitle.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(0f, -90f), new Vector2(560f, 70f));
        pauseTitle.alignment = TextAlignmentOptions.Center;
        pauseTitle.fontStyle = FontStyles.Bold;
        resume = CreateButton(pauseCard.transform, "ResumeButton", "RESUME HUNT", Orange,
            new Vector2(430f, 92f));
        SetAnchored(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0f, 168f), new Vector2(430f, 92f));
        pauseExit = CreateButton(pauseCard.transform, "PauseExitButton", "EXIT TO MAIN MENU",
            Teal, new Vector2(430f, 72f));
        SetAnchored(pauseExit.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0f, 78f), new Vector2(430f, 72f));

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);
        PremiumUiFactory.PolishHierarchy(canvasObject.transform, font);
        hud.SetActive(false);
        results.SetActive(false);
        tutorial.SetActive(false);
        pausePanel.SetActive(false);
    }

    /// <summary>
    /// The left identity column. Matches Cat Runner: gold frame + masked hero
    /// art. Prefers the premium AI illustration at CatchHeroPath; falls back to
    /// a painted slab only when that asset is missing.
    /// </summary>
    private static void BuildWelcomeHero(Transform card)
    {
        GameObject heroFrame = CreatePremium(card, "WelcomeHeroFrame", FrameGold, 34f);
        SetAnchored(heroFrame.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f),
            new Vector2(-274f, -64f), new Vector2(516f, 516f));
        Mask heroMask = heroFrame.AddComponent<Mask>();
        heroMask.showMaskGraphic = true;

        ConfigureCatchHeroImporter();
        Texture2D heroArt = AssetDatabase.LoadAssetAtPath<Texture2D>(CatchHeroPath);
        if (heroArt != null)
        {
            GameObject heroObject = CreateUi("WelcomeHeroArt", heroFrame.transform);
            RawImage heroImage = heroObject.AddComponent<RawImage>();
            heroImage.texture = heroArt;
            heroImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            heroImage.color = Color.white;
            heroImage.raycastTarget = false;
            StretchWithOffsets(heroImage.rectTransform, 8f, 8f, -8f, -8f);
        }
        else
        {
            GameObject heroFill = CreatePremium(heroFrame.transform, "WelcomeHeroFill", Aqua, 28f);
            StretchWithOffsets(heroFill.GetComponent<RectTransform>(), 8f, 8f, -8f, -8f);
            TMP_Text heroMark = CreateLabel(heroFill.transform, "HeroMark",
                "CHASE\nPOUNCE\nCATCH", 54f, Cream);
            Stretch(heroMark.rectTransform);
            heroMark.alignment = TextAlignmentOptions.Center;
            heroMark.lineSpacing = 6f;
        }

        GameObject heroGlint = CreatePremium(
            heroFrame.transform, "HeroGlintSparkle", new Color32(255, 255, 255, 96), 16f);
        SetAnchored(heroGlint.GetComponent<RectTransform>(), new Vector2(0.72f, 0.82f),
            Vector2.zero, new Vector2(94f, 14f));
        heroGlint.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
    }

    /// <summary>
    /// Renders the finished arena into the welcome hero image so the card shows
    /// the real game instead of a stand-in. Every temporary change (pose, mouse
    /// placement, ambient light) is restored before the scene is saved.
    /// </summary>
    private static void BakeWelcomeHero(
        Transform root, Camera sceneCamera, CatCatchPlayer player, CatCatchMouse[] mice)
    {
        GameObject cameraObject = null;
        RenderTexture target = null;
        RenderTexture previousActive = RenderTexture.active;
        AmbientMode previousAmbientMode = RenderSettings.ambientMode;
        Color previousAmbientLight = RenderSettings.ambientLight;
        Transform cat = player.transform;
        Vector3 catPosition = cat.localPosition;
        Quaternion catRotation = cat.localRotation;
        Transform model = cat.childCount > 0 ? cat.GetChild(0) : null;
        Vector3 modelPosition = model != null ? model.localPosition : Vector3.zero;
        Quaternion modelRotation = model != null ? model.localRotation : Quaternion.identity;
        Vector3 modelScale = model != null ? model.localScale : Vector3.one;
        CatCatchMouse mouse = mice != null && mice.Length > 0 ? mice[0] : null;
        Vector3 mousePosition = mouse != null ? mouse.transform.localPosition : Vector3.zero;

        try
        {
            EnsureFolder("Assets/Art/Catch/UI");
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.76f, 0.82f, 1f);

            cat.localPosition = new Vector3(-0.32f, 0f, -1.35f);
            cat.localRotation = Quaternion.Euler(0f, 208f, 0f);
            SamplePose(model);
            if (mouse != null)
            {
                mouse.gameObject.SetActive(true);
                // Kept at roughly the cat's depth: anything nearer the camera
                // falls under the frame once the shot is tilted down.
                mouse.transform.localPosition = new Vector3(0.92f, 0.12f, -1.02f);
                mouse.transform.localRotation = Quaternion.Euler(0f, 118f, 0f);
            }

            cameraObject = new GameObject("CatchHeroBakeCamera", typeof(Camera),
                typeof(UniversalAdditionalCameraData));
            cameraObject.transform.SetParent(root, false);
            cameraObject.transform.localPosition = new Vector3(0.25f, 1.95f, -4.45f);
            cameraObject.transform.localRotation = Quaternion.Euler(23f, -3f, 0f);
            Camera heroCamera = cameraObject.GetComponent<Camera>();
            heroCamera.fieldOfView = 40f;
            heroCamera.clearFlags = CameraClearFlags.SolidColor;
            heroCamera.backgroundColor = new Color32(126, 226, 236, 255);
            heroCamera.allowHDR = sceneCamera.allowHDR;
            heroCamera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            target = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };
            heroCamera.targetTexture = target;
            heroCamera.Render();

            RenderTexture.active = target;
            var baked = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            baked.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            baked.Apply();
            System.IO.File.WriteAllBytes(CatchHeroPath, baked.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(baked);
            AssetDatabase.ImportAsset(CatchHeroPath, ImportAssetOptions.ForceUpdate);
        }
        catch (Exception error)
        {
            Debug.LogWarning("Cat Catch hero bake skipped: " + error.Message);
        }
        finally
        {
            RenderTexture.active = previousActive;
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
            if (target != null)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            RenderSettings.ambientMode = previousAmbientMode;
            RenderSettings.ambientLight = previousAmbientLight;
            cat.localPosition = catPosition;
            cat.localRotation = catRotation;
            if (model != null)
            {
                model.localPosition = modelPosition;
                model.localRotation = modelRotation;
                model.localScale = modelScale;
            }
            if (mouse != null)
            {
                mouse.transform.localPosition = mousePosition;
                mouse.Hide();
            }
        }
    }

    /// <summary>
    /// Freezes the cat in a mid-run frame for the bake. Without it the prefab
    /// renders in its bind pose, which reads as a stiff statue on the card.
    /// </summary>
    private static void SamplePose(Transform model)
    {
        if (model == null)
            return;
        Animator animator = model.GetComponentInChildren<Animator>(true);
        RuntimeAnimatorController controller = animator != null
            ? animator.runtimeAnimatorController
            : null;
        if (controller == null)
            return;
        AnimationClip[] clips = controller.animationClips;
        AnimationClip chosen = null;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
                continue;
            if (clips[i].name.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                chosen = clips[i];
                break;
            }
            if (chosen == null)
                chosen = clips[i];
        }
        if (chosen == null)
            return;
        chosen.SampleAnimation(model.gameObject, chosen.length * 0.35f);
        model.localPosition = Vector3.zero;
        model.localScale = Vector3.one;
    }

    private static void ConfigureCatchHeroImporter()
    {
        var importer = AssetImporter.GetAtPath(CatchHeroPath) as TextureImporter;
        if (importer == null)
            return;
        bool dirty = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            dirty = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }
        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            dirty = true;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }
        if (importer.maxTextureSize != 2048)
        {
            importer.maxTextureSize = 2048;
            dirty = true;
        }
        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            dirty = true;
        }
        if (importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = false;
            dirty = true;
        }
        if (dirty)
            importer.SaveAndReimport();
    }

    private static GameObject CreateBlock(
        Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = block.GetComponent<Collider>();
        if (collider != null && name != "Floor")
            UnityEngine.Object.DestroyImmediate(collider);
        return block;
    }

    private static GameObject CreateSphere(
        Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = position;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        return sphere;
    }

    private static Transform CreatePoint(Transform parent, string name, Vector3 localPosition)
    {
        GameObject point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.localPosition = localPosition;
        return point.transform;
    }

    private static GameObject CreateUi(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    /// <summary>
    /// A HUD readout with its caption baked in: a soft drop panel behind a
    /// coloured pill, a small uppercase caption and the value underneath it.
    /// </summary>
    private static TMP_Text CreateStatPill(
        Transform parent,
        string name,
        string caption,
        string value,
        Color color,
        Color valueColor,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        float valueSize)
    {
        // Soft depth under the pill — same centre, slightly larger on every side.
        // Never +X/−Y drop offset: that paints a broken right/bottom frame gap.
        GameObject shadow = CreatePremium(parent, name + "Shadow", Ink, 22f);
        SetAnchored(shadow.GetComponent<RectTransform>(), anchor, position,
            size + new Vector2(8f, 8f));
        LowPolyPanelGraphic shadowGraphic = shadow.GetComponent<LowPolyPanelGraphic>();
        if (shadowGraphic != null)
        {
            Color faded = Ink;
            faded.a = 0.16f;
            PremiumUiStyle.ConfigureShadowSurface(shadowGraphic, faded, 22f);
        }

        GameObject pill = CreatePremium(parent, name, color, 22f);
        SetAnchored(pill.GetComponent<RectTransform>(), anchor, position, size);

        // Both rows hang off the pill's top edge: anchoring the value to the
        // bottom instead let a tall value climb back into the caption band, which
        // is what printed "TIME LEFT" straight through the countdown digits.
        TMP_Text captionLabel = CreateLabel(pill.transform, "Caption", caption, 17f, valueColor);
        SetAnchored(captionLabel.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(size.x - 24f, 20f));
        captionLabel.alignment = TextAlignmentOptions.Center;
        captionLabel.alpha = 0.72f;
        captionLabel.characterSpacing = 5f;
        captionLabel.raycastTarget = false;

        TMP_Text valueLabel = CreateLabel(pill.transform, "Value", value, valueSize, valueColor);
        SetAnchored(valueLabel.rectTransform, new Vector2(0.5f, 1f),
            new Vector2(0f, -38f), new Vector2(size.x - 24f, size.y - 46f));
        valueLabel.alignment = TextAlignmentOptions.Center;
        valueLabel.raycastTarget = false;
        return valueLabel;
    }

    private static GameObject CreatePremium(Transform parent, string name, Color color, float cut)
    {
        GameObject panel = CreateUi(name, parent);
        LowPolyPanelGraphic graphic = panel.AddComponent<LowPolyPanelGraphic>();
        // Same top/bottom split as the Runner builder: without the darker bottom
        // stop the panel loses its inner frame and reads as a flat pastel slab.
        PremiumUiStyle.ConfigureAccentSurface(
            graphic,
            Color.Lerp(color, Color.white, 0.1f),
            Color.Lerp(color, PremiumUiStyle.Night, 0.08f),
            cut,
            3f);
        graphic.raycastTarget = false;
        return panel;
    }

    private static GameObject CreateFlatPanel(Transform parent, string name, Color color)
    {
        GameObject panel = CreateUi(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
    }

    private static TMP_Text CreateLabel(
        Transform parent, string name, string value, float size, Color color)
    {
        GameObject labelObject = CreateUi(name, parent);
        TMP_Text label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = size;
        label.color = color;
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 1.1f;
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);
        if (font != null)
            label.font = font;
        return label;
    }

    private static Button CreateButton(
        Transform parent, string name, string label, Color color, Vector2 size)
    {
        GameObject buttonObject = CreateUi(name, parent);
        LowPolyPanelGraphic graphic = buttonObject.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(
            graphic,
            Color.Lerp(color, Color.white, 0.2f),
            Color.Lerp(color, PremiumUiStyle.Night, 0.16f),
            34f,
            5f);
        graphic.raycastTarget = true;
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.targetGraphic = graphic;
        button.GetComponent<RectTransform>().sizeDelta = size;
        TMP_Text text = CreateLabel(
            buttonObject.transform, "Label", label, 28f, PremiumUiStyle.Ivory);
        StretchWithOffsets(text.rectTransform, 10f, 6f, -10f, -6f);
        text.alignment = TextAlignmentOptions.Center;
        text.extraPadding = true;
        text.characterSpacing = 1.15f;
        text.margin = new Vector4(4f, 2f, 4f, 2f);
        buttonObject.AddComponent<PremiumButtonFx>();
        return button;
    }

    private static void StretchWithOffsets(
        RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        // Match Cat Runner: keep a centre pivot so top- and bottom-anchored
        // stacks share the same vertical math. Anchoring the pivot to the edge
        // stretched the right column and made the rules pill sit under START.
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (!string.Equals(scenes[i].path, ScenePath, StringComparison.Ordinal))
                continue;
            scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
            found = true;
            break;
        }
        if (!found)
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Scenes/Catch");
        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/Catch");
        EnsureFolder("Assets/Art/Catch/UI");
        EnsureFolder(MaterialFolder);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
