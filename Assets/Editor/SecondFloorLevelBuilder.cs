using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Authors Second Floor Level 01 as a cozy upstairs loft: solid warm walls, a
/// wood-plank floor, a big picture window with a rooftop view, sloped ceiling
/// beams with a skylight and a stair-landing rail. The ten furnishings are hidden
/// store products; the shell stays. Mirrors the shared room contract (seven
/// authoring groups, one camera/listener, cloned cat, care area).
/// </summary>
public static class SecondFloorLevelBuilder
{
    public const string ScenePath = HomeRoomService.SecondFloorScenePath;

    private const string LivingScenePath = HomeRoomService.LivingRoomScenePath;
    private const string MaterialFolder = "Assets/Art/SecondFloor/Materials";
    private const string SharedVolumePath =
        "Assets/Art/PremiumWorld/CatHomeRoom_PremiumVolume.asset";

    private static readonly Color Cream = new Color32(255, 250, 236, 255);
    private static readonly Color Aqua = new Color32(145, 231, 222, 255);
    private static readonly Color Mint = new Color32(184, 242, 211, 255);
    private static readonly Color Coral = new Color32(255, 181, 176, 255);
    private static readonly Color Lilac = new Color32(218, 195, 243, 255);
    private static readonly Color Lemon = new Color32(255, 232, 139, 255);
    private static readonly Color Ink = new Color32(63, 47, 80, 255);
    private static readonly Color Sky = new Color32(150, 220, 255, 255);
    private static readonly Color Wall = new Color32(255, 244, 226, 255);
    private static readonly Color WallDeep = new Color32(232, 218, 242, 255);
    private static readonly Color Wood = new Color32(218, 166, 110, 255);
    private static readonly Color WoodLight = new Color32(240, 198, 151, 255);
    private static readonly Color Roof = new Color32(211, 190, 226, 255);

    private const float LivingCameraFieldOfView = 47f;
    private static readonly Vector3 LivingCameraPosition = new Vector3(-1f, 3f, -5.5f);
    private static readonly Quaternion LivingCameraRotation =
        Quaternion.Euler(25f, 12.995f, 0f);
    private static readonly Vector3 LivingSpawnPointPosition = new Vector3(0f, 0f, -2f);
    private static readonly Color LivingAmbientSkyColor = new Color32(58, 56, 62, 255);
    private static readonly Color LivingAmbientEquatorColor = new Color32(34, 30, 34, 255);
    private static readonly Color LivingAmbientGroundColor = new Color32(14, 12, 12, 255);
    private static readonly Color LivingCameraBackground = new Color32(255, 238, 214, 0);

    [MenuItem("Tools/Cat Home/Rooms/Build Second Floor Level 01")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Second Floor Level 01",
                "Create or replace SecondFloor_Level01 as a cozy upstairs loft?",
                "Build",
                "Cancel"))
        {
            return;
        }

        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Second Floor", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Rebuild Second Floor Level 01 (Silent)")]
    public static void BuildBatch()
    {
        Debug.Log(BuildSilently());
    }

    public static string BuildSilently()
    {
        EnsureFolders();
        Dictionary<string, Material> materials = BuildMaterials();
        StoreProductContentBuilder.BuildProductAssetsSilently();

        Scene previousActive = SceneManager.GetActiveScene();
        bool previousWasLoft = previousActive.IsValid() &&
                               string.Equals(
                                   previousActive.path,
                                   ScenePath,
                                   StringComparison.Ordinal);
        Scene livingScene = SceneManager.GetSceneByPath(LivingScenePath);
        bool openedLiving = !livingScene.IsValid() || !livingScene.isLoaded;
        if (openedLiving)
            livingScene = EditorSceneManager.OpenScene(LivingScenePath, OpenSceneMode.Additive);

        Scene existingLoft = SceneManager.GetSceneByPath(ScenePath);
        if (existingLoft.IsValid() && existingLoft.isLoaded)
        {
            if (SceneManager.GetActiveScene() == existingLoft)
                EditorSceneManager.SetActiveScene(livingScene);
            EditorSceneManager.CloseScene(existingLoft, true);
        }

        Scene loft = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(loft);

        Transform environment = CreateSceneRoot(loft, "01 Environment");
        Transform furniture = CreateSceneRoot(loft, "02 Furniture");
        Transform character = CreateSceneRoot(loft, "03 Character");
        Transform gameplay = CreateSceneRoot(loft, "04 Gameplay");
        Transform presentation = CreateSceneRoot(loft, "05 Presentation");
        CreateSceneRoot(loft, "06 Local UI");
        Transform setup = CreateSceneRoot(loft, "07 Level Setup");

        BuildRoomShell(environment, materials);
        HomeRoomShellVisualPolishBuilder.Apply(loft, HomeRoomService.SecondFloorId, environment);
        Camera camera = BuildPresentation(presentation);
        CareSet care = BuildEmptyCareArea(gameplay);
        CatMovement cat = CloneAndConfigureCat(
            livingScene, character, camera, care, loft);
        BuildGameTimeService(setup);
        BuildArchitecture(setup, camera, cat);
        StoreProductContentBuilder.BuildRoomSceneProducts(
            loft,
            HomeRoomService.SecondFloorId,
            furniture);

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientSkyColor = LivingAmbientSkyColor;
        RenderSettings.ambientEquatorColor = LivingAmbientEquatorColor;
        RenderSettings.ambientGroundColor = LivingAmbientGroundColor;
        RenderSettings.fog = false;

        try
        {
            RoomLightingControllerBuilder.Build();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Second Floor room lighting controller build failed: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(loft);
        if (!EditorSceneManager.SaveScene(loft, ScenePath))
            throw new InvalidOperationException("SecondFloor_Level01 could not be saved.");

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(loft, true);

        if (openedLiving && !previousWasLoft)
            EditorSceneManager.CloseScene(livingScene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);
        else if (livingScene.IsValid() && livingScene.isLoaded)
            EditorSceneManager.SetActiveScene(livingScene);

        return "Second Floor Level 01 built as a cozy loft with a ten-piece ROOM collection.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Loft_Cream", Cream, .03f, .5f),
            ["Aqua"] = EnsureMaterial("Loft_Aqua", Aqua, .04f, .66f),
            ["Mint"] = EnsureMaterial("Loft_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Loft_Coral", Coral, .02f, .58f),
            ["Lilac"] = EnsureMaterial("Loft_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Loft_Lemon", Lemon, .03f, .6f),
            ["Ink"] = EnsureMaterial("Loft_Ink", Ink, .05f, .36f),
            ["Sky"] = EnsureMaterial("Loft_Sky", Sky, .04f, .68f),
            ["Wall"] = EnsureMaterial("Loft_Wall", Wall, .02f, .32f),
            ["WallDeep"] = EnsureMaterial("Loft_WallDeep", WallDeep, .02f, .34f),
            ["Wood"] = EnsureMaterial("Loft_Wood", Wood, .04f, .36f),
            ["WoodLight"] = EnsureMaterial("Loft_WoodLight", WoodLight, .04f, .4f),
            ["Roof"] = EnsureMaterial("Loft_Roof", Roof, .03f, .4f),
            ["Gold"] = EnsureMaterial(
                "Loft_Gold", new Color32(255, 191, 57, 255), .58f, .75f)
        };
    }

    private static Material EnsureMaterial(
        string name, Color color, float metallic, float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                        Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("No compatible lit shader was found.");
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
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void BuildRoomShell(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        GameObject floor = CreateBlock("WalkableFloor", parent,
            HomeRoomShellMetrics.FloorPosition, HomeRoomShellMetrics.FloorScale,
            materials["Wood"], true);
        HideRenderer(floor);

        BuildWoodFloor(parent, materials);
        BuildSolidWalls(parent, materials);
        BuildBackWindow(parent, materials);
        BuildCeiling(parent, materials);
        BuildStairLanding(parent, materials);
        BuildLoftAccents(parent, materials);
    }

    private static void BuildWoodFloor(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform floor = CreateChild(parent, "Loft Wood Floor");
        GameObject blanket = CreateBlock(
            "FloorBlanket",
            floor,
            new Vector3(0f, HomeRoomShellMetrics.FloorTileCenterY, 0f),
            new Vector3(
                HomeRoomShellMetrics.FloorWidth - .04f,
                HomeRoomShellMetrics.FloorTileThickness,
                HomeRoomShellMetrics.FloorDepth - .04f),
            materials["Wood"],
            false);
        HideShadows(blanket);

        const int planks = 10;
        float span = HomeRoomShellMetrics.FloorDepth - .2f;
        float step = span / planks;
        float start = -span * .5f + step * .5f;
        for (int i = 0; i < planks; i++)
        {
            GameObject plank = CreateBlock(
                "Plank_" + (i + 1).ToString("00"),
                floor,
                new Vector3(0f, HomeRoomShellMetrics.FloorTileCenterY + .01f, start + step * i),
                new Vector3(HomeRoomShellMetrics.FloorWidth - .12f, .02f, step * .82f),
                i % 2 == 0 ? materials["WoodLight"] : materials["Wood"],
                false);
            HideShadows(plank);
        }
    }

    private static void BuildSolidWalls(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform walls = CreateChild(parent, "Loft Walls");
        // Visible solid walls at the canonical shell transforms.
        CreateBlock("BackWall_Loft", walls,
            HomeRoomShellMetrics.BackWallPosition, HomeRoomShellMetrics.BackWallScale,
            materials["Wall"], true);
        CreateBlock("LeftWall_Loft", walls,
            HomeRoomShellMetrics.LeftWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["WallDeep"], true);
        CreateBlock("RightWall_Loft", walls,
            HomeRoomShellMetrics.RightWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["WallDeep"], true);

        // Baseboard + a warm wainscot band so the walls do not read flat.
        float backZ = HomeRoomShellMetrics.InteriorMaxZ - .04f;
        CreateBlock("BaseboardBack", walls,
            new Vector3(0f, .1f, backZ), new Vector3(HomeRoomShellMetrics.FloorWidth - .2f, .2f, .06f),
            materials["Cream"], false);
        CreateBlock("WainscotBack", walls,
            new Vector3(0f, .7f, backZ), new Vector3(HomeRoomShellMetrics.FloorWidth - .2f, .06f, .05f),
            materials["Gold"], false);
        float leftX = HomeRoomShellMetrics.InteriorMinX + .04f;
        float rightX = HomeRoomShellMetrics.InteriorMaxX - .04f;
        CreateBlock("BaseboardLeft", walls,
            new Vector3(leftX, .1f, 0f), new Vector3(.06f, .2f, HomeRoomShellMetrics.FloorDepth - .2f),
            materials["Cream"], false);
        CreateBlock("BaseboardRight", walls,
            new Vector3(rightX, .1f, 0f), new Vector3(.06f, .2f, HomeRoomShellMetrics.FloorDepth - .2f),
            materials["Cream"], false);
    }

    private static void BuildBackWindow(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform window = CreateChild(parent, "Loft Picture Window");
        float z = HomeRoomShellMetrics.BackWallDecorZ(.06f);

        // A bright sky pane, glass tint, frame and muntins read as a big window.
        CreateBlock("SkyView", window,
            new Vector3(0f, 1.85f, z + .02f), new Vector3(3.0f, 1.9f, .04f),
            materials["Sky"], false);
        // A hint of distant rooftops so the loft feels high up.
        CreateBlock("RooflineA", window,
            new Vector3(-.9f, 1.15f, z + .015f), new Vector3(1.0f, .5f, .03f),
            materials["Roof"], false);
        CreateBlock("RooflineB", window,
            new Vector3(.7f, 1.05f, z + .015f), new Vector3(1.2f, .4f, .03f),
            materials["Lilac"], false);
        CreateSphere("WindowSun", window,
            new Vector3(1.1f, 2.35f, z + .01f), Vector3.one * .4f, materials["Lemon"], false);
        CreateBlock("Glass", window,
            new Vector3(0f, 1.85f, z), new Vector3(3.0f, 1.9f, .03f),
            materials["Aqua"], false);
        // Frame.
        CreateBlock("FrameTop", window,
            new Vector3(0f, 2.84f, z - .01f), new Vector3(3.24f, .16f, .1f), materials["Cream"], false);
        CreateBlock("FrameBottom", window,
            new Vector3(0f, .86f, z - .01f), new Vector3(3.24f, .16f, .1f), materials["Cream"], false);
        CreateBlock("FrameLeft", window,
            new Vector3(-1.54f, 1.85f, z - .01f), new Vector3(.16f, 2.14f, .1f), materials["Cream"], false);
        CreateBlock("FrameRight", window,
            new Vector3(1.54f, 1.85f, z - .01f), new Vector3(.16f, 2.14f, .1f), materials["Cream"], false);
        CreateBlock("MuntinV", window,
            new Vector3(0f, 1.85f, z - .02f), new Vector3(.07f, 1.9f, .06f), materials["Cream"], false);
        CreateBlock("MuntinH", window,
            new Vector3(0f, 1.85f, z - .02f), new Vector3(3.0f, .07f, .06f), materials["Cream"], false);
    }

    private static void BuildCeiling(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform ceiling = CreateChild(parent, "Loft Ceiling");
        float y = HomeRoomShellMetrics.WallHeight - .06f;

        GameObject panel = CreateBlock("CeilingPanel", ceiling,
            new Vector3(0f, y, .2f),
            new Vector3(HomeRoomShellMetrics.FloorWidth - .1f, .08f, HomeRoomShellMetrics.FloorDepth - .1f),
            materials["Cream"], false);
        HideShadows(panel);

        // Exposed beams across the loft.
        for (int i = 0; i < 4; i++)
        {
            float x = -2.4f + i * 1.6f;
            CreateBlock("Beam_" + (i + 1), ceiling,
                new Vector3(x, y - .1f, .2f),
                new Vector3(.16f, .18f, HomeRoomShellMetrics.FloorDepth - .3f),
                materials["Wood"], false);
        }

        // A skylight strip that glows.
        CreateBlock("SkylightFrame", ceiling,
            new Vector3(.6f, y - .02f, -.6f), new Vector3(1.9f, .06f, 1.1f), materials["Gold"], false);
        GameObject glow = CreateBlock("SkylightGlow", ceiling,
            new Vector3(.6f, y - .08f, -.6f), new Vector3(1.7f, .04f, .9f), materials["Sky"], false);
        HideShadows(glow);
        CreateBlock("SkylightBar", ceiling,
            new Vector3(.6f, y - .09f, -.6f), new Vector3(.05f, .03f, .9f), materials["Cream"], false);

        // A pendant light hanging over the room center.
        CreateBlock("PendantCord", ceiling,
            new Vector3(-1.0f, y - .35f, -.3f), new Vector3(.03f, .6f, .03f), materials["Ink"], false);
        CreateSphere("PendantShade", ceiling,
            new Vector3(-1.0f, y - .66f, -.3f), new Vector3(.42f, .34f, .42f), materials["Gold"], false);
        CreateSphere("PendantBulb", ceiling,
            new Vector3(-1.0f, y - .74f, -.3f), Vector3.one * .16f, materials["Lemon"], false);
    }

    private static void BuildStairLanding(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        // A railing and newel at the front-right corner suggest the stairs coming
        // up into the loft, without cutting a hole in the walkable floor.
        Transform landing = CreateChild(parent, "Loft Stair Landing");
        float x = HomeRoomShellMetrics.InteriorMaxX - .5f;
        float z = HomeRoomShellMetrics.InteriorMinZ + .5f;

        CreateBlock("Newel", landing,
            new Vector3(x, .5f, z), new Vector3(.14f, 1.0f, .14f), materials["Wood"], false);
        CreateSphere("NewelCap", landing,
            new Vector3(x, 1.04f, z), Vector3.one * .16f, materials["Gold"], false);
        CreateBlock("HandRail", landing,
            new Vector3(x - .8f, .9f, z), new Vector3(1.6f, .08f, .08f), materials["Wood"], false);
        for (int i = 0; i < 5; i++)
        {
            CreateBlock("Baluster_" + (i + 1), landing,
                new Vector3(x - .2f - i * .34f, .55f, z),
                new Vector3(.05f, .7f, .05f), materials["Cream"], false);
        }
        // A low potted plant on the landing post.
        CreateSphere("LandingPlant", landing,
            new Vector3(x, 1.2f, z), new Vector3(.3f, .28f, .3f), materials["Mint"], false);
    }

    private static void BuildLoftAccents(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform accents = CreateChild(parent, "Loft Accents");
        // Wall sconces flanking the window.
        float z = HomeRoomShellMetrics.BackWallDecorZ(.05f);
        CreateBlock("SconceL", accents,
            new Vector3(-2.4f, 1.9f, z), new Vector3(.16f, .3f, .12f), materials["Gold"], false);
        CreateBlock("SconceR", accents,
            new Vector3(2.4f, 1.9f, z), new Vector3(.16f, .3f, .12f), materials["Gold"], false);
        CreateSphere("SconceGlowL", accents,
            new Vector3(-2.4f, 2.05f, z - .05f), Vector3.one * .14f, materials["Lemon"], false);
        CreateSphere("SconceGlowR", accents,
            new Vector3(2.4f, 2.05f, z - .05f), Vector3.one * .14f, materials["Lemon"], false);
    }

    private static Camera BuildPresentation(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera),
            typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = LivingCameraPosition;
        cameraObject.transform.localRotation = LivingCameraRotation;
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = LivingCameraFieldOfView;
        camera.nearClipPlane = .3f;
        camera.farClipPlane = 1000f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = LivingCameraBackground;
        camera.allowHDR = true;
        camera.allowMSAA = true;
        camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

        GameObject keyObject = new GameObject("Loft Key Light", typeof(Light));
        keyObject.transform.SetParent(parent, false);
        keyObject.transform.rotation = Quaternion.Euler(50f, -24f, 0f);
        Light key = keyObject.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color32(255, 244, 214, 255);
        key.intensity = 1.0f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = .50f;

        BuildFillLight(parent, "Window Fill", new Vector3(0f, 2.2f, 2.2f),
            new Color32(150, 220, 255, 255), .8f, 8.5f);
        BuildFillLight(parent, "Warm Fill", new Vector3(-1.5f, 1.6f, -1f),
            new Color32(255, 210, 150, 255), .7f, 7.5f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedVolumePath);
        if (profile != null)
        {
            GameObject volumeObject = new GameObject("Loft Premium Volume", typeof(Volume));
            volumeObject.transform.SetParent(parent, false);
            Volume volume = volumeObject.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 5f;
            volume.sharedProfile = profile;
        }
        return camera;
    }

    private static void BuildFillLight(Transform parent, string name, Vector3 position,
        Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name, typeof(Light));
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static CareSet BuildEmptyCareArea(Transform gameplay)
    {
        Transform care = CreateChild(gameplay, "Cat Care Anchors");

        GameObject foodBowl = new GameObject("FoodBowlAnchor");
        foodBowl.transform.SetParent(care, false);
        foodBowl.transform.position = new Vector3(-3.25f, 0f, -1.58f);
        Transform foodPoint = CreateWorldAnchor(foodBowl.transform, "InteractionPoint",
            new Vector3(-3.25f, .03f, -2.02f), Quaternion.identity);
        GameObject foodContent = new GameObject("FoodContent");
        foodContent.transform.SetParent(foodBowl.transform, false);

        GameObject waterBowl = new GameObject("WaterBowlAnchor");
        waterBowl.transform.SetParent(care, false);
        waterBowl.transform.position = new Vector3(-2.55f, 0f, -1.58f);
        Transform waterPoint = CreateWorldAnchor(waterBowl.transform, "InteractionPoint",
            new Vector3(-2.55f, .03f, -2.02f), Quaternion.identity);
        GameObject waterContent = new GameObject("WaterContent");
        waterContent.transform.SetParent(waterBowl.transform, false);

        GameObject rest = new GameObject("RestAnchor");
        rest.transform.SetParent(care, false);
        rest.transform.position = new Vector3(-3.15f, 0f, -2.62f);
        Transform bedPoint = CreateWorldAnchor(rest.transform, "BedInteractionPoint",
            new Vector3(-3.15f, .03f, -2.08f), Quaternion.identity);
        Transform sleepPoint = CreateWorldAnchor(rest.transform, "SleepPoint",
            new Vector3(-3.15f, .18f, -2.62f), Quaternion.identity);

        return new CareSet(
            foodBowl.transform, foodPoint, foodContent,
            waterBowl.transform, waterPoint, waterContent,
            bedPoint, sleepPoint);
    }

    private static CatMovement CloneAndConfigureCat(
        Scene livingScene, Transform parent, Camera camera, CareSet care, Scene targetScene)
    {
        CatMovement sourceCat = FindInScene<CatMovement>(livingScene);
        if (sourceCat == null)
            throw new InvalidOperationException("Living Room CatRoot was not found.");

        GameObject catObject = UnityEngine.Object.Instantiate(sourceCat.gameObject);
        catObject.name = "CatRoot";
        SceneManager.MoveGameObjectToScene(catObject, targetScene);
        catObject.transform.SetParent(parent, false);
        catObject.transform.localPosition = new Vector3(0f, 0f, -2f);
        catObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        catObject.transform.localScale = new Vector3(.5f, .5f, .5f);

        CatMovement cat = catObject.GetComponent<CatMovement>();
        BowlInteraction bowls = catObject.GetComponent<BowlInteraction>();
        SleepInteraction sleep = catObject.GetComponent<SleepInteraction>();
        PetInteraction pet = catObject.GetComponent<PetInteraction>();
        CharacterController characterController = catObject.GetComponent<CharacterController>();
        Animator animator = catObject.GetComponentInChildren<Animator>(true);

        SerializedObject movementSerialized = new SerializedObject(cat);
        SetObject(movementSerialized, "mobileJoystick", null);
        movementSerialized.ApplyModifiedPropertiesWithoutUndo();

        if (bowls != null)
        {
            SerializedObject serialized = new SerializedObject(bowls);
            SetObject(serialized, "catTransform", cat.transform);
            SetObject(serialized, "catMovement", cat);
            SetObject(serialized, "hungerSystem", null);
            SetObject(serialized, "thirstSystem", null);
            SetObject(serialized, "sleepInteraction", sleep);
            SetObject(serialized, "animator", animator);
            SetObject(serialized, "characterController", characterController);
            ConfigureBowl(serialized.FindProperty("food"), care.FoodBowl,
                care.FoodPoint, care.FoodContent, "EAT", "Eat");
            ConfigureBowl(serialized.FindProperty("water"), care.WaterBowl,
                care.WaterPoint, care.WaterContent, "DRINK", "Drink");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (sleep != null)
        {
            SerializedObject serialized = new SerializedObject(sleep);
            SetObject(serialized, "bedInteractionPoint", care.BedPoint);
            SetObject(serialized, "sleepPoint", care.SleepPoint);
            SetObject(serialized, "catTransform", cat.transform);
            SetObject(serialized, "catMovement", cat);
            SetObject(serialized, "animator", animator);
            SetObject(serialized, "characterController", characterController);
            SetObject(serialized, "energySystem", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        if (pet != null)
        {
            SerializedObject serialized = new SerializedObject(pet);
            SetObject(serialized, "catMovement", cat);
            SetObject(serialized, "bowlInteraction", bowls);
            SetObject(serialized, "sleepInteraction", sleep);
            SetObject(serialized, "hungerSystem", null);
            SetObject(serialized, "thirstSystem", null);
            SetObject(serialized, "animator", animator);
            SetObject(serialized, "tutorialHint", null);
            SetObject(serialized, "gameplayCamera", camera);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        return cat;
    }

    private static void ConfigureBowl(SerializedProperty property, Transform bowl,
        Transform point, GameObject content, string label, string animation)
    {
        if (property == null)
            return;
        property.FindPropertyRelative("bowl").objectReferenceValue = bowl;
        property.FindPropertyRelative("interactionPoint").objectReferenceValue = point;
        property.FindPropertyRelative("content").objectReferenceValue = content;
        property.FindPropertyRelative("startsFull").boolValue = true;
        property.FindPropertyRelative("buttonText").stringValue = label;
        property.FindPropertyRelative("animationState").stringValue = animation;
    }

    private static void BuildArchitecture(Transform parent, Camera camera, CatMovement cat)
    {
        GameObject markerObject = new GameObject("LevelArchitecture");
        markerObject.transform.SetParent(parent, false);
        LevelSceneMarker levelMarker = markerObject.AddComponent<LevelSceneMarker>();
        levelMarker.EditorSetId(HomeRoomService.SecondFloorId);
        HomeRoomSceneMarker roomMarker = markerObject.AddComponent<HomeRoomSceneMarker>();
        roomMarker.EditorConfigure(HomeRoomService.SecondFloorId, camera, cat);

        GameObject spawnObject = new GameObject("SpawnPoint_default");
        spawnObject.transform.SetParent(parent, false);
        spawnObject.transform.localPosition = LivingSpawnPointPosition;
        spawnObject.transform.localRotation = Quaternion.identity;
        LevelSpawnPoint spawn = spawnObject.AddComponent<LevelSpawnPoint>();
        spawn.EditorSetId("default");
    }

    private static void BuildGameTimeService(Transform parent)
    {
        GameObject timeObject = new GameObject("GameTimeService");
        timeObject.transform.SetParent(parent, false);
        timeObject.AddComponent<GameTimeService>();
    }

    private static GameObject CreateBlock(string name, Transform parent, Vector3 position,
        Vector3 scale, Material material, bool collider)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
            UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
        return block;
    }

    private static GameObject CreateSphere(string name, Transform parent, Vector3 position,
        Vector3 scale, Material material, bool collider)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = position;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
        return sphere;
    }

    private static void HideRenderer(GameObject target)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;
    }

    private static void HideShadows(GameObject target)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
            renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static Transform CreateSceneRoot(Scene scene, string name)
    {
        GameObject root = new GameObject(name);
        SceneManager.MoveGameObjectToScene(root, scene);
        return root.transform;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static Transform CreateWorldAnchor(Transform parent, string name,
        Vector3 position, Quaternion rotation)
    {
        Transform anchor = CreateChild(parent, name);
        anchor.position = position;
        anchor.rotation = rotation;
        return anchor;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }

    private static void SetObject(
        SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
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
        EnsureFolder("Assets/Scenes/Levels");
        EnsureFolder("Assets/Art/SecondFloor");
        EnsureFolder(MaterialFolder);
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

    private readonly struct CareSet
    {
        public CareSet(Transform foodBowl, Transform foodPoint, GameObject foodContent,
            Transform waterBowl, Transform waterPoint, GameObject waterContent,
            Transform bedPoint, Transform sleepPoint)
        {
            FoodBowl = foodBowl;
            FoodPoint = foodPoint;
            FoodContent = foodContent;
            WaterBowl = waterBowl;
            WaterPoint = waterPoint;
            WaterContent = waterContent;
            BedPoint = bedPoint;
            SleepPoint = sleepPoint;
        }

        public Transform FoodBowl { get; }
        public Transform FoodPoint { get; }
        public GameObject FoodContent { get; }
        public Transform WaterBowl { get; }
        public Transform WaterPoint { get; }
        public GameObject WaterContent { get; }
        public Transform BedPoint { get; }
        public Transform SleepPoint { get; }
    }
}
