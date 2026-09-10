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
    private const string CatPrefabPath = PolyperfectCatIntegrationBuilder.PrefabPath;
    private const string MaterialFolder = "Assets/Art/Catch/Materials";

    private static readonly Color Cream = new Color32(255, 247, 224, 255);
    private static readonly Color Aqua = new Color32(67, 220, 211, 255);
    private static readonly Color Mint = new Color32(126, 235, 190, 255);
    private static readonly Color Coral = new Color32(255, 120, 130, 255);
    private static readonly Color Lilac = new Color32(188, 143, 235, 255);
    private static readonly Color Lemon = new Color32(255, 222, 94, 255);
    private static readonly Color Ink = new Color32(63, 47, 80, 255);
    private static readonly Color Peach = new Color32(255, 177, 120, 255);

    // Premium arena palette: soft, harmonious pastels that read as the same
    // candy world as the home room, replacing the old clashing teal / orange /
    // maroon blocks. Walls stay light so the cat and mice pop against them.
    private static readonly Color WallPeach = new Color32(250, 195, 173, 255);
    private static readonly Color WallMint = new Color32(166, 224, 198, 255);
    private static readonly Color WallSky = new Color32(159, 211, 235, 255);
    private static readonly Color TrimCream = new Color32(255, 244, 214, 255);
    private static readonly Color TrimGold = new Color32(249, 190, 78, 255);
    private static readonly Color TileMint = new Color32(183, 232, 208, 255);
    private static readonly Color TileLilac = new Color32(215, 193, 239, 255);
    private static readonly Color RugPink = new Color32(250, 159, 190, 255);
    private static readonly Color RugCream = new Color32(255, 221, 190, 255);
    // Cute premium mouse: soft lilac body, cream belly, pink ears/nose/tail and
    // a dark eye. The old near-black Ink body read as a flat blob on the floor.
    private static readonly Color MouseBody = new Color32(183, 124, 227, 255);
    private static readonly Color MouseBelly = new Color32(255, 240, 224, 255);
    private static readonly Color MousePink = new Color32(255, 150, 180, 255);
    private static readonly Color EyeInk = new Color32(46, 35, 60, 255);

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
        BuildCatchBursts(root.transform, materials);
        // Keep hand-authored / AI premium hero art when present; only bake a
        // procedural arena still as a fallback so rebuilds never wipe the card.
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

        MiniGameArtBuilder.Catch(root.transform);
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
            ["Ink"] = EnsureMaterial("Catch_Ink", Ink, .05f, .36f),
            ["WallPeach"] = EnsureMaterial("Catch_WallPeach", WallPeach, .0f, .34f),
            ["WallMint"] = EnsureMaterial("Catch_WallMint", WallMint, .0f, .34f),
            ["WallSky"] = EnsureMaterial("Catch_WallSky", WallSky, .0f, .34f),
            ["TrimCream"] = EnsureMaterial("Catch_TrimCream", TrimCream, .0f, .5f),
            ["TrimGold"] = EnsureMaterial("Catch_TrimGold", TrimGold, .08f, .62f),
            ["TileMint"] = EnsureMaterial("Catch_TileMint", TileMint, .0f, .5f),
            ["TileLilac"] = EnsureMaterial("Catch_TileLilac", TileLilac, .0f, .5f),
            ["RugPink"] = EnsureMaterial("Catch_RugPink", RugPink, .0f, .46f),
            ["RugCream"] = EnsureMaterial("Catch_RugCream", RugCream, .0f, .46f),
            ["MouseBody"] = EnsureMaterial("Catch_MouseBody", MouseBody, .02f, .5f),
            ["MouseBelly"] = EnsureMaterial("Catch_MouseBelly", MouseBelly, .0f, .5f),
            ["MousePink"] = EnsureMaterial("Catch_MousePink", MousePink, .02f, .54f),
            ["EyeInk"] = EnsureMaterial("Catch_EyeInk", EyeInk, .0f, .3f)
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
        light.intensity = 0.72f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.55f;

        // Soft cyan fill from the opposite side removes the flat, single-shade
        // look and gives the pastel walls gentle dimension.
        GameObject fill = new GameObject("Catch Fill Light", typeof(Light));
        fill.transform.SetParent(parent, false);
        fill.transform.rotation = Quaternion.Euler(38f, 150f, 0f);
        Light fillLight = fill.GetComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color32(150, 226, 255, 255);
        fillLight.intensity = 0.18f;
        fillLight.shadows = LightShadows.None;

        // Keep the shadow side colourful without bleaching the tile, rug and
        // mouse silhouettes into one nearly-white value range.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.52f, 0.57f, 0.65f, 1f);
    }

    private static void BuildArena(Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform arena = CreatePoint(parent, "CatchArena", Vector3.zero);
        // Floor keeps its collider and 8x6 footprint — the hunt clamps the cat to
        // it and taps raycast against it, so geometry is locked; only the finish
        // is lifted to the premium candy palette.
        CreateBlock(arena, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(8f, 0.2f, 6f),
            materials["Cream"]);
        for (int x = 0; x < 8; x++)
        {
            for (int z = 0; z < 6; z++)
            {
                Material tile = ((x + z) & 1) == 0 ? materials["TileMint"] : materials["TileLilac"];
                CreateBlock(arena, $"Tile_{x}_{z}",
                    new Vector3(-3.5f + x, 0.01f, -2.5f + z),
                    new Vector3(0.96f, 0.02f, 0.96f), tile);
            }
        }

        // Soft round play rug in the middle so the arena reads as a cosy room,
        // not a bare checker grid. Thin discs, no collider.
        CreateDisc(arena, "PlayRugBase", new Vector3(0f, 0.02f, -0.1f),
            new Vector3(4.6f, 0.02f, 3.5f), materials["RugPink"]);
        CreateDisc(arena, "PlayRugInner", new Vector3(0f, 0.025f, -0.1f),
            new Vector3(3.7f, 0.02f, 2.8f), materials["RugCream"]);

        // Harmonious pastel walls with cream + gold trim and a wainscot line,
        // matching the home room's premium finish.
        CreateBlock(arena, "BackWall", new Vector3(0f, 1.5f, 2.9f),
            new Vector3(8f, 3f, 0.2f), materials["WallPeach"]);
        CreateBlock(arena, "LeftWall", new Vector3(-3.9f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 6f), materials["WallMint"]);
        CreateBlock(arena, "RightWall", new Vector3(3.9f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 6f), materials["WallSky"]);

        BuildWallTrim(arena, materials);
        BuildWallMotifs(arena, materials);
    }

    /// <summary>
    /// Cream crown + baseboard + wainscot line on the three walls, plus a thin
    /// gold inset, so the arena shell reads as finished trim like the home room.
    /// All trim is inside the wall faces (x = +-3.79, z = 2.79) and collider-free.
    /// </summary>
    private static void BuildWallTrim(Transform arena, IReadOnlyDictionary<string, Material> materials)
    {
        Material cream = materials["TrimCream"];
        Material gold = materials["TrimGold"];
        const float inner = 3.79f;
        const float backZ = 2.79f;

        // Crown (top) trim.
        CreateBlock(arena, "Crown_Back", new Vector3(0f, 2.82f, backZ),
            new Vector3(7.9f, 0.16f, 0.12f), cream);
        CreateBlock(arena, "Crown_Left", new Vector3(-inner, 2.82f, 0f),
            new Vector3(0.12f, 0.16f, 5.9f), cream);
        CreateBlock(arena, "Crown_Right", new Vector3(inner, 2.82f, 0f),
            new Vector3(0.12f, 0.16f, 5.9f), cream);
        // Baseboard (bottom) trim.
        CreateBlock(arena, "Base_Back", new Vector3(0f, 0.16f, backZ),
            new Vector3(7.9f, 0.3f, 0.14f), cream);
        CreateBlock(arena, "Base_Left", new Vector3(-inner, 0.16f, 0f),
            new Vector3(0.14f, 0.3f, 5.9f), cream);
        CreateBlock(arena, "Base_Right", new Vector3(inner, 0.16f, 0f),
            new Vector3(0.14f, 0.3f, 5.9f), cream);
        // Gold wainscot line ~ home 0.57 band.
        CreateBlock(arena, "Wainscot_Back", new Vector3(0f, 0.9f, backZ),
            new Vector3(7.6f, 0.05f, 0.06f), gold);
        CreateBlock(arena, "Wainscot_Left", new Vector3(-inner, 0.9f, 0f),
            new Vector3(0.06f, 0.05f, 5.6f), gold);
        CreateBlock(arena, "Wainscot_Right", new Vector3(inner, 0.9f, 0f),
            new Vector3(0.06f, 0.05f, 5.6f), gold);
    }

    /// <summary>Flat candy paw pads on the walls, matching the home motifs.</summary>
    private static void BuildWallMotifs(Transform arena, IReadOnlyDictionary<string, Material> materials)
    {
        CreatePawMotif(arena, "PawBackLeft", new Vector3(-2.2f, 2.0f, 2.77f),
            Quaternion.identity, materials["MousePink"], materials["Lemon"], 1f);
        CreatePawMotif(arena, "PawBackRight", new Vector3(2.2f, 1.85f, 2.77f),
            Quaternion.identity, materials["Mint"], materials["MousePink"], 0.85f);
        CreatePawMotif(arena, "PawLeft", new Vector3(-3.77f, 2.05f, -0.7f),
            Quaternion.Euler(0f, 90f, 0f), materials["Lemon"], materials["Aqua"], 0.8f);
        CreatePawMotif(arena, "PawRight", new Vector3(3.77f, 1.7f, 0.8f),
            Quaternion.Euler(0f, -90f, 0f), materials["MousePink"], materials["Mint"], 0.8f);
    }

    /// <summary>A round paw: one pad plus four toe beans, flattened onto a wall.</summary>
    private static void CreatePawMotif(
        Transform parent, string name, Vector3 position, Quaternion rotation,
        Material pad, Material toes, float scale)
    {
        GameObject paw = new GameObject(name);
        paw.transform.SetParent(parent, false);
        paw.transform.localPosition = position;
        paw.transform.localRotation = rotation;
        paw.transform.localScale = Vector3.one * scale;
        CreateSphere(paw.transform, "Pad", new Vector3(0f, -0.05f, 0f),
            new Vector3(0.34f, 0.28f, 0.06f), pad);
        CreateSphere(paw.transform, "Toe0", new Vector3(-0.17f, 0.2f, 0f),
            new Vector3(0.12f, 0.14f, 0.05f), toes);
        CreateSphere(paw.transform, "Toe1", new Vector3(-0.055f, 0.27f, 0f),
            new Vector3(0.12f, 0.15f, 0.05f), toes);
        CreateSphere(paw.transform, "Toe2", new Vector3(0.065f, 0.27f, 0f),
            new Vector3(0.12f, 0.15f, 0.05f), toes);
        CreateSphere(paw.transform, "Toe3", new Vector3(0.18f, 0.2f, 0f),
            new Vector3(0.12f, 0.14f, 0.05f), toes);
    }

    private static CatCatchPlayer BuildPlayer(Transform parent, Transform spawn, Camera camera)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPrefabPath);
        if (prefab == null)
            throw new InvalidOperationException("Cat prefab missing for Cat Catch.");
        // The hunt drives a wrapper while the project-owned visual prefab keeps
        // the imported Animator isolated underneath it.
        GameObject cat = new GameObject("CatchCat");
        cat.transform.SetParent(parent, false);
        cat.transform.localPosition = spawn.localPosition;
        cat.transform.localRotation = Quaternion.identity;

        GameObject model = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        model.name = "CatModel";
        model.transform.SetParent(cat.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        // The integrated prefab owns the model-fit scale internally.
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
                materials["MouseBody"]);
            CreateSphere(mouseRoot, "Belly", new Vector3(0f, -0.04f, 0.05f),
                new Vector3(0.3f, 0.17f, 0.36f), materials["MouseBelly"]);
            CreateSphere(mouseRoot, "EarL", new Vector3(-0.13f, 0.17f, -0.04f),
                new Vector3(0.15f, 0.18f, 0.08f), materials["MousePink"]);
            CreateSphere(mouseRoot, "EarR", new Vector3(0.13f, 0.17f, -0.04f),
                new Vector3(0.15f, 0.18f, 0.08f), materials["MousePink"]);
            // Ear inner beans + eyes for a cute, readable face.
            CreateSphere(mouseRoot, "EarInnerL", new Vector3(-0.13f, 0.18f, -0.005f),
                new Vector3(0.08f, 0.1f, 0.05f), materials["MouseBelly"]);
            CreateSphere(mouseRoot, "EarInnerR", new Vector3(0.13f, 0.18f, -0.005f),
                new Vector3(0.08f, 0.1f, 0.05f), materials["MouseBelly"]);
            CreateSphere(mouseRoot, "EyeL", new Vector3(-0.1f, 0.06f, 0.2f),
                Vector3.one * 0.07f, materials["EyeInk"]);
            CreateSphere(mouseRoot, "EyeR", new Vector3(0.1f, 0.06f, 0.2f),
                Vector3.one * 0.07f, materials["EyeInk"]);
            CreateSphere(mouseRoot, "Nose", new Vector3(0f, 0.02f, 0.26f),
                Vector3.one * 0.08f, materials["MousePink"]);
            CreateSphere(mouseRoot, "Tail", new Vector3(0f, 0.06f, -0.28f),
                new Vector3(0.08f, 0.08f, 0.28f), materials["MousePink"]);
            CatCatchMouse mouse = mouseRoot.gameObject.AddComponent<CatCatchMouse>();
            mouse.Configure(parent, minLocal, maxLocal, cat);
            mouse.Hide();
            mice[i] = mouse;
        }
        return mice;
    }

    /// <summary>
    /// A small pool of scale-only catch celebrations. The controller finds these
    /// via GetComponentsInChildren and round-robins one on every catch.
    /// </summary>
    private static void BuildCatchBursts(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform pool = CreatePoint(parent, "CatchBursts", Vector3.zero);
        Material[] confettiMats =
        {
            materials["Lemon"], materials["MousePink"], materials["Aqua"],
            materials["Mint"], materials["Coral"], materials["Lilac"]
        };
        for (int b = 0; b < 4; b++)
        {
            GameObject burst = new GameObject("CatchBurst_" + (b + 1));
            burst.transform.SetParent(pool, false);
            Transform ring = CreateDisc(burst.transform, "Ring", new Vector3(0f, 0.09f, 0f),
                new Vector3(0.55f, 0.02f, 0.55f), materials["Cream"]).transform;
            var confetti = new Transform[6];
            for (int i = 0; i < confetti.Length; i++)
            {
                confetti[i] = CreateSphere(burst.transform, "Bit_" + i,
                    new Vector3(0f, 0.15f, 0f), Vector3.one * 0.14f,
                    confettiMats[i % confettiMats.Length]).transform;
            }
            CatchBurstFx fx = burst.AddComponent<CatchBurstFx>();
            fx.EditorBind(ring, confetti);
        }
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
            new Vector2(0f, 1f), new Vector2(184f, -60f), new Vector2(292f, 74f), 36f);
        combo = CreateLabel(hud.transform, "ComboLabel", string.Empty, 24f, Coral);
        SetAnchored(combo.rectTransform, new Vector2(0f, 1f),
            new Vector2(184f, -128f), new Vector2(292f, 32f));
        combo.alignment = TextAlignmentOptions.Center;
        combo.raycastTarget = false;

        timer = CreateStatPill(
            hud.transform, "TimerPill", "TIME LEFT", "60", Coral, Color.white,
            new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(232f, 74f), 40f);

        caught = CreateStatPill(
            hud.transform, "CatchPill", "MICE  •  COINS", "0  •  0", Aqua, Ink,
            new Vector2(1f, 1f), new Vector2(-298f, -60f), new Vector2(332f, 74f), 32f);

        GameObject hintPill = CreatePremium(hud.transform, "HintPill", Cream, 20f);
        SetAnchored(hintPill.GetComponent<RectTransform>(), new Vector2(0.5f, 0f),
            new Vector2(0f, 56f), new Vector2(820f, 56f));
        TMP_Text hint = CreateLabel(hintPill.transform, "Hint",
            "TAP A MOUSE  •  YOUR CAT CHASES IT DOWN AND POUNCES", 20f, Ink);
        Stretch(hint.rectTransform);
        hint.alignment = TextAlignmentOptions.Center;
        hint.raycastTarget = false;
        pause = CreateButton(hud.transform, "PauseButton", "II", Lilac, new Vector2(72f, 66f));
        SetAnchored(pause.GetComponent<RectTransform>(), new Vector2(1f, 1f),
            new Vector2(-56f, -60f), new Vector2(72f, 66f));
        TMP_Text pauseLabel = pause.GetComponentInChildren<TMP_Text>(true);
        if (pauseLabel != null)
            pauseLabel.fontSize = 26f;

        var welcomeParts=PremiumMiniGameUiBuilder.Welcome(canvasObject.transform,false);
        welcome=welcomeParts.Root;welcomeBest=welcomeParts.Best;welcomeLives=welcomeParts.Energy;
        start=welcomeParts.Start;exit=welcomeParts.Exit;rewarded=welcomeParts.Rewarded;
        var resultParts=PremiumMiniGameUiBuilder.Results(canvasObject.transform,false);
        results=resultParts.Root;title=resultParts.Title;details=resultParts.Details;collect=resultParts.Home;retry=resultParts.Retry;
        resultParts.NewBest.SetActive(false);resultParts.Missions.gameObject.SetActive(false);
        var pauseParts=PremiumMiniGameUiBuilder.Pause(canvasObject.transform,false);
        pausePanel=pauseParts.Root;resume=pauseParts.Resume;pauseExit=pauseParts.Exit;
        var tutorialParts=PremiumMiniGameUiBuilder.Tutorial(canvasObject.transform);
        tutorial=tutorialParts.Root;tutorialMessage=tutorialParts.Message;tutorialSkip=tutorialParts.Skip;

        var fitSafe=canvasObject.transform.Find("WelcomePanel/WelcomeSafeArea") as RectTransform;
        canvasObject.AddComponent<CatRunnerResponsiveLayout>().EditorConfigure(fitSafe,null,welcomeParts.Card,resultParts.Card,pauseParts.Card,tutorialParts.Card);

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            PremiumUiStyle.PremiumFontAssetPath);
        PremiumUiFactory.PolishHierarchy(canvasObject.transform, font);
        PremiumMiniGameUiBuilder.PolishHud(hud.transform);
        PremiumUiElements.Localize(hint,"catch.hint");
        hint.gameObject.AddComponent<CatchHuntHint>();
        PremiumUiElements.Localize(score.transform.parent.Find("Caption").GetComponent<TMP_Text>(),"games.score");
        PremiumUiElements.Localize(timer.transform.parent.Find("Caption").GetComponent<TMP_Text>(),"games.time");
        PremiumUiElements.Localize(caught.transform.parent.Find("Caption").GetComponent<TMP_Text>(),"catch.mice");
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
        // A completed game's live capture is authoritative. This early builder
        // still has the temporary arena, before the final art pass runs.
        if (System.IO.File.Exists("Assets/Art/Games/CatchPreview.png")) return;
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
            EnsureFolder("Assets/Art/Games");
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

            target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };
            heroCamera.targetTexture = target;
            heroCamera.Render();

            RenderTexture.active = target;
            var baked = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            baked.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            baked.Apply();
            System.IO.File.WriteAllBytes("Assets/Art/Games/CatchPreview.png", baked.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(baked);
            AssetDatabase.ImportAsset("Assets/Art/Games/CatchPreview.png", ImportAssetOptions.ForceUpdate);
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

    private static GameObject CreateDisc(
        Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;
        disc.transform.SetParent(parent, false);
        disc.transform.localPosition = position;
        // Unity's cylinder is 2 units tall; scale.y here is the full thickness.
        disc.transform.localScale = new Vector3(scale.x, scale.y * 0.5f, scale.z);
        disc.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(disc.GetComponent<Collider>());
        return disc;
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

        // Caption + value are one tightly-stacked block centred on the pill so
        // there is no top-heavy caption with a dead gap under the value. Both use
        // the centre anchor and mirror each other around it.
        const float captionHeight = 20f;
        const float rowGap = 3f;
        float captionCenterY = valueSize * 0.5f + rowGap * 0.5f;
        float valueCenterY = -(captionHeight * 0.5f + rowGap * 0.5f);

        TMP_Text captionLabel = CreateLabel(pill.transform, "Caption", caption, 16f, valueColor);
        SetAnchored(captionLabel.rectTransform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, captionCenterY), new Vector2(size.x - 24f, captionHeight));
        captionLabel.alignment = TextAlignmentOptions.Center;
        captionLabel.alpha = 0.72f;
        captionLabel.characterSpacing = 5f;
        captionLabel.raycastTarget = false;

        TMP_Text valueLabel = CreateLabel(pill.transform, "Value", value, valueSize, valueColor);
        SetAnchored(valueLabel.rectTransform, new Vector2(0.5f, 0.5f),
            new Vector2(0f, valueCenterY), new Vector2(size.x - 24f, valueSize + 8f));
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
