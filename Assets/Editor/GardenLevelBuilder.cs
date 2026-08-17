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
/// Authors Garden Level 01 as a sunny fenced courtyard. The ten furnishings
/// are hidden store products; the lawn, fence and flying birds stay in the shell.
/// </summary>
public static class GardenLevelBuilder
{
    public const string ScenePath = HomeRoomService.GardenScenePath;

    private const string LivingScenePath = HomeRoomService.LivingRoomScenePath;
    private const string MaterialFolder = "Assets/Art/Garden/Materials";
    private const string SharedVolumePath =
        "Assets/Art/PremiumWorld/CatHomeRoom_PremiumVolume.asset";

    private static readonly Color Cream = new Color32(255, 247, 224, 255);
    private static readonly Color Aqua = new Color32(67, 220, 211, 255);
    private static readonly Color Mint = new Color32(126, 235, 190, 255);
    private static readonly Color Coral = new Color32(255, 120, 130, 255);
    private static readonly Color Peach = new Color32(255, 177, 120, 255);
    private static readonly Color Lilac = new Color32(188, 143, 235, 255);
    private static readonly Color Lemon = new Color32(255, 222, 94, 255);
    private static readonly Color Ink = new Color32(63, 47, 80, 255);
    private static readonly Color Sky = new Color32(132, 214, 255, 255);
    private static readonly Color Grass = new Color32(104, 196, 92, 255);
    private static readonly Color GrassDeep = new Color32(72, 164, 78, 255);
    private static readonly Color GrassLight = new Color32(156, 220, 112, 255);
    private static readonly Color WoodFence = new Color32(214, 154, 92, 255);
    private const float LivingCameraFieldOfView = 47f;
    private static readonly Vector3 LivingCameraPosition = new Vector3(-1f, 3f, -5.5f);
    private static readonly Quaternion LivingCameraRotation =
        Quaternion.Euler(25f, 12.995f, 0f);
    private static readonly Vector3 LivingSpawnPointPosition = new Vector3(0f, 0f, -2f);
    private static readonly Color LivingAmbientSkyColor = new Color32(54, 58, 66, 255);
    private static readonly Color LivingAmbientEquatorColor = new Color32(29, 32, 34, 255);
    private static readonly Color LivingAmbientGroundColor = new Color32(12, 11, 9, 255);
    private static readonly Color LivingCameraBackground = new Color32(49, 77, 121, 0);

    [MenuItem("Tools/Cat Home/Rooms/Build Garden Level 01")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Garden Level 01",
                "Create or replace Garden_Level01 as a sunny fenced courtyard?",
                "Build",
                "Cancel"))
        {
            return;
        }

        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Garden", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Rebuild Garden Level 01 (Silent)")]
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
        bool previousWasGarden = previousActive.IsValid() &&
                                 string.Equals(
                                     previousActive.path,
                                     ScenePath,
                                     StringComparison.Ordinal);
        Scene livingScene = SceneManager.GetSceneByPath(LivingScenePath);
        bool openedLiving = !livingScene.IsValid() || !livingScene.isLoaded;
        if (openedLiving)
            livingScene = EditorSceneManager.OpenScene(LivingScenePath, OpenSceneMode.Additive);

        Scene existingGarden = SceneManager.GetSceneByPath(ScenePath);
        if (existingGarden.IsValid() && existingGarden.isLoaded)
        {
            if (SceneManager.GetActiveScene() == existingGarden)
                EditorSceneManager.SetActiveScene(livingScene);
            EditorSceneManager.CloseScene(existingGarden, true);
        }

        Scene garden = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(garden);

        Transform environment = CreateSceneRoot(garden, "01 Environment");
        Transform furniture = CreateSceneRoot(garden, "02 Furniture");
        Transform character = CreateSceneRoot(garden, "03 Character");
        Transform gameplay = CreateSceneRoot(garden, "04 Gameplay");
        Transform presentation = CreateSceneRoot(garden, "05 Presentation");
        CreateSceneRoot(garden, "06 Local UI");
        Transform setup = CreateSceneRoot(garden, "07 Level Setup");

        Transform treeRoost;
        Transform[] flowerAnchors;
        BuildRoomShell(environment, materials, out treeRoost, out flowerAnchors);
        Camera camera = BuildPresentation(presentation);
        CareSet care = BuildEmptyCareArea(gameplay);
        CatMovement cat = CloneAndConfigureCat(
            livingScene, character, camera, care, garden);
        GardenBirdFlock flock = BuildBirdFlock(environment, materials, treeRoost);
        BuildBirdAttention(setup, cat, flock);
        BuildBirdWatchActivity(gameplay, treeRoost);
        BuildAmbientCritters(environment, materials, flowerAnchors);
        BuildGameTimeService(setup);
        BuildArchitecture(setup, camera, cat);
        StoreProductContentBuilder.BuildRoomSceneProducts(
            garden,
            HomeRoomService.GardenId,
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
            Debug.LogWarning("Garden room lighting controller build failed: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(garden);
        if (!EditorSceneManager.SaveScene(garden, ScenePath))
            throw new InvalidOperationException("Garden_Level01 could not be saved.");

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(garden, true);

        if (openedLiving && !previousWasGarden)
            EditorSceneManager.CloseScene(livingScene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);
        else if (livingScene.IsValid() && livingScene.isLoaded)
            EditorSceneManager.SetActiveScene(livingScene);

        return "Garden Level 01 built as a fenced courtyard with a ten-piece ROOM collection.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Garden_Cream", Cream, .03f, .54f),
            ["Aqua"] = EnsureMaterial("Garden_Aqua", Aqua, .02f, .62f),
            ["Mint"] = EnsureMaterial("Garden_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Garden_Coral", Coral, .02f, .58f),
            ["Peach"] = EnsureMaterial("Garden_Peach", Peach, .02f, .56f),
            ["Lilac"] = EnsureMaterial("Garden_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Garden_Lemon", Lemon, .03f, .6f),
            ["Ink"] = EnsureMaterial("Garden_Ink", Ink, .05f, .36f),
            ["Sky"] = EnsureMaterial("Garden_Sky", Sky, .08f, .74f),
            ["Grass"] = EnsureMaterial("Garden_Grass", Grass, .02f, .28f),
            ["GrassDeep"] = EnsureMaterial("Garden_GrassDeep", GrassDeep, .02f, .24f),
            ["GrassLight"] = EnsureMaterial("Garden_GrassLight", GrassLight, .02f, .32f),
            ["Wood"] = EnsureMaterial("Garden_Wood", WoodFence, .04f, .36f),
            ["Gold"] = EnsureMaterial(
                "Garden_Gold", new Color32(255, 191, 57, 255), .58f, .75f),
            ["Bird"] = EnsureMaterial(
                "Garden_Bird", new Color32(92, 118, 168, 255), .04f, .42f),
            ["BirdWing"] = EnsureMaterial(
                "Garden_BirdWing", new Color32(248, 244, 232, 255), .03f, .5f)
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
        Transform parent,
        IReadOnlyDictionary<string, Material> materials,
        out Transform treeRoost,
        out Transform[] flowerAnchors)
    {
        GameObject floor = CreateBlock("WalkableFloor", parent,
            HomeRoomShellMetrics.FloorPosition, HomeRoomShellMetrics.FloorScale,
            materials["Grass"], true);
        HideRenderer(floor);

        BuildGrassLawn(parent, materials);
        BuildInvisibleShellWalls(parent, materials);
        BuildOpenSky(parent, materials);
        BuildDistantPath(parent, materials);
        treeRoost = BuildCourtyardTree(parent, materials);
        flowerAnchors = BuildBorderFlowers(parent, materials);
        BuildFence(parent, materials);
        BuildGardenGate(parent, materials);
    }

    private static void BuildGrassLawn(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform lawn = CreateChild(parent, "Sunny Lawn");
        GameObject blanket = CreateBlock(
            "GrassBlanket",
            lawn,
            new Vector3(0f, HomeRoomShellMetrics.FloorTileCenterY, 0f),
            new Vector3(
                HomeRoomShellMetrics.FloorWidth - .04f,
                HomeRoomShellMetrics.FloorTileThickness,
                HomeRoomShellMetrics.FloorDepth - .04f),
            materials["GrassLight"],
            false);
        HideShadows(blanket);
        GameObject apron = CreateBlock(
            "GrassApron",
            lawn,
            new Vector3(0f, .005f, 3.2f),
            new Vector3(18f, .02f, 16f),
            materials["GrassLight"],
            false);
        HideShadows(apron);

        Material[] bladeMaterials =
        {
            materials["Grass"],
            materials["GrassLight"],
            materials["GrassDeep"],
            materials["Mint"]
        };
        const int clumps = 42;
        const int bladesPerClump = 7;
        int bladeIndex = 0;
        for (int i = 0; i < clumps; i++)
        {
            float u = Frac(i * 0.6180339f);
            float v = Frac(i * 0.4142135f + .13f);
            float cx = (u - .5f) * 7.1f;
            float cz = (v - .5f) * 5.2f;
            for (int b = 0; b < bladesPerClump; b++)
            {
                bladeIndex++;
                float angle = (b / (float)bladesPerClump) * Mathf.PI * 2f + i * .21f;
                float radius = .04f + (b % 3) * .025f;
                float height = .09f + ((i + b) % 5) * .018f;
                GameObject blade = CreateBlock(
                    "GrassBlade_" + bladeIndex.ToString("000"),
                    lawn,
                    new Vector3(
                        cx + Mathf.Cos(angle) * radius,
                        HomeRoomShellMetrics.FloorTileCenterY + height * .5f,
                        cz + Mathf.Sin(angle) * radius),
                    new Vector3(.028f, height, .016f),
                    bladeMaterials[(i + b) % bladeMaterials.Length],
                    false);
                blade.transform.localRotation = Quaternion.Euler(
                    (b % 3 - 1) * 8f,
                    angle * Mathf.Rad2Deg,
                    (i % 3 - 1) * 7f);
                HideShadows(blade);
            }
        }
    }

    private static void BuildInvisibleShellWalls(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform walls = CreateChild(parent, "Sunny Courtyard Walls");
        HideRenderer(CreateBlock("BackWall_Sky", walls,
            HomeRoomShellMetrics.BackWallPosition, HomeRoomShellMetrics.BackWallScale,
            materials["Sky"], true));
        HideRenderer(CreateBlock("LeftWall_Sky", walls,
            HomeRoomShellMetrics.LeftWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Sky"], true));
        HideRenderer(CreateBlock("RightWall_Sky", walls,
            HomeRoomShellMetrics.RightWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Sky"], true));
    }

    private static void BuildOpenSky(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform sky = CreateChild(parent, "Open Sky");
        CreateBlock("SkyBack", sky,
            new Vector3(0f, 10f, 24f), new Vector3(56f, 28f, 1f),
            materials["Sky"], false);
        CreateBlock("SkyTop", sky,
            new Vector3(0f, 18f, 4f), new Vector3(56f, 1f, 48f),
            materials["Sky"], false);
        CreateBlock("SkyOverhead", sky,
            new Vector3(0f, 14f, -4f), new Vector3(48f, 1f, 20f),
            materials["Sky"], false);
        CreateBlock("SkyHighFill", sky,
            new Vector3(6f, 17f, 12f), new Vector3(40f, 1f, 28f),
            materials["Sky"], false);
        CreateBlock("SkyLeft", sky,
            new Vector3(-24f, 10f, 6f), new Vector3(1f, 28f, 48f),
            materials["Sky"], false);
        CreateBlock("SkyRight", sky,
            new Vector3(24f, 10f, 6f), new Vector3(1f, 28f, 48f),
            materials["Sky"], false);
        CreateSphere("SunDisc", sky,
            new Vector3(6.4f, 10.5f, 16.5f), Vector3.one * 1.8f,
            materials["Lemon"], false);
        CreateSphere("CloudFarA", sky,
            new Vector3(-6.2f, 8.4f, 15.5f), new Vector3(3.2f, 1.1f, 1.4f),
            materials["Cream"], false);
        CreateSphere("CloudFarB", sky,
            new Vector3(1.8f, 9.2f, 17.2f), new Vector3(2.4f, .85f, 1.1f),
            materials["Cream"], false);
        CreateSphere("CloudFarC", sky,
            new Vector3(7.1f, 7.6f, 14.2f), new Vector3(2.8f, .9f, 1.2f),
            materials["Cream"], false);
        CreateSphere("HillLeft", sky,
            new Vector3(-7.5f, .6f, 16f), new Vector3(6.5f, 2.4f, 3.2f),
            materials["Mint"], false);
        CreateSphere("HillRight", sky,
            new Vector3(8.2f, .45f, 17.5f), new Vector3(5.8f, 1.9f, 2.8f),
            materials["GrassDeep"], false);
    }

    private static void BuildDistantPath(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform vista = CreateChild(parent, "Distant Path");
        CreateBlock("FarLawn", vista,
            new Vector3(0f, .01f, 11f), new Vector3(22f, .03f, 18f),
            materials["GrassLight"], false);
        CreateBlock("PathBed", vista,
            new Vector3(0f, .03f, 9.4f), new Vector3(1.35f, .04f, 14f),
            materials["Peach"], false);
        CreateBlock("PathCenter", vista,
            new Vector3(0f, .045f, 9.4f), new Vector3(.72f, .03f, 14f),
            materials["Cream"], false);
        for (int i = 0; i < 5; i++)
        {
            CreateBlock("PathStone_" + (i + 1), vista,
                new Vector3((i % 2 == 0 ? -.18f : .16f), .055f, 4.2f + i * 2.1f),
                new Vector3(.42f, .03f, .38f),
                i % 2 == 0 ? materials["Peach"] : materials["Lemon"],
                false);
        }
    }

    private static Transform BuildCourtyardTree(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform tree = CreateChild(parent, "Courtyard Tree");
        tree.localPosition = new Vector3(-2.45f, 0f, .55f);
        CreateBlock("Trunk", tree,
            new Vector3(0f, .7f, 0f), new Vector3(.22f, 1.4f, .22f),
            materials["Wood"], false);
        CreateSphere("CrownLow", tree,
            new Vector3(-.08f, 1.55f, .05f), new Vector3(1.35f, 1.05f, 1.25f),
            materials["GrassDeep"], false);
        CreateSphere("CrownHigh", tree,
            new Vector3(.18f, 2.05f, -.12f), new Vector3(.95f, .78f, .9f),
            materials["Mint"], false);
        Transform roost = CreateChild(tree, "BirdRoost");
        roost.localPosition = new Vector3(0f, 1.82f, 0f);
        return roost;
    }

    private static Transform[] BuildBorderFlowers(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform border = CreateChild(parent, "Garden Flower Border");
        Material[] blooms =
        {
            materials["Coral"], materials["Lemon"], materials["Lilac"],
            materials["Peach"], materials["Aqua"], materials["Mint"]
        };
        Vector3[] spots =
        {
            new Vector3(-3.15f, 0f, -1.85f),
            new Vector3(-3.2f, 0f, -.55f),
            new Vector3(-3.15f, 0f, .75f),
            new Vector3(-3.05f, 0f, 1.85f),
            new Vector3(3.15f, 0f, -1.75f),
            new Vector3(3.2f, 0f, -.35f),
            new Vector3(3.12f, 0f, .95f),
            new Vector3(3.05f, 0f, 1.9f),
            new Vector3(-1.85f, 0f, -2.55f),
            new Vector3(1.95f, 0f, -2.55f)
        };
        var anchors = new Transform[spots.Length];
        for (int i = 0; i < spots.Length; i++)
        {
            Transform clump = CreateChild(border, "FlowerClump_" + (i + 1).ToString("00"));
            clump.localPosition = spots[i];
            CreateBlock("Stem", clump,
                new Vector3(0f, .12f, 0f), new Vector3(.05f, .24f, .05f),
                materials["GrassDeep"], false);
            CreateSphere("Bloom", clump,
                new Vector3(0f, .28f, 0f), Vector3.one * .16f,
                blooms[i % blooms.Length], false);
            CreateSphere("SideBloom", clump,
                new Vector3(.11f, .22f, .05f), Vector3.one * .1f,
                blooms[(i + 2) % blooms.Length], false);
            anchors[i] = clump;
        }

        return anchors;
    }

    private static void BuildFence(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform fence = CreateChild(parent, "Sunny Garden Fence");
        const float railY = .62f;
        const float postHeight = 1.05f;
        float innerX = HomeRoomShellMetrics.InteriorMaxX - .12f;
        float backZ = HomeRoomShellMetrics.InteriorMaxZ - .12f;
        float frontZ = HomeRoomShellMetrics.InteriorMinZ + .12f;

        BuildFenceRun(
            fence, "BackLeftFence",
            new Vector3(-2.55f, 0f, backZ),
            2.2f,
            0f,
            materials,
            railY,
            postHeight,
            3);
        BuildFenceRun(
            fence, "BackRightFence",
            new Vector3(2.55f, 0f, backZ),
            2.2f,
            0f,
            materials,
            railY,
            postHeight,
            3);
        BuildFenceRun(
            fence, "LeftFence",
            new Vector3(-innerX, 0f, 0f),
            HomeRoomShellMetrics.FloorDepth - .35f,
            90f,
            materials,
            railY,
            postHeight,
            7);
        BuildFenceRun(
            fence, "RightFence",
            new Vector3(innerX, 0f, 0f),
            HomeRoomShellMetrics.FloorDepth - .35f,
            90f,
            materials,
            railY,
            postHeight,
            7);
        BuildFenceRun(
            fence, "FrontLeftFence",
            new Vector3(-2.15f, 0f, frontZ),
            2.9f,
            0f,
            materials,
            railY,
            postHeight,
            4);
        BuildFenceRun(
            fence, "FrontRightFence",
            new Vector3(2.15f, 0f, frontZ),
            2.9f,
            0f,
            materials,
            railY,
            postHeight,
            4);
    }

    private static void BuildFenceRun(
        Transform parent,
        string name,
        Vector3 center,
        float length,
        float yaw,
        IReadOnlyDictionary<string, Material> materials,
        float railY,
        float postHeight,
        int posts)
    {
        Transform run = CreateChild(parent, name);
        run.localPosition = center;
        run.localRotation = Quaternion.Euler(0f, yaw, 0f);

        CreateBlock("Hedge", run,
            new Vector3(0f, .28f, 0f),
            new Vector3(length, .46f, .16f),
            materials["GrassDeep"], false);
        CreateBlock("RailLow", run,
            new Vector3(0f, railY - .18f, 0f),
            new Vector3(length, .07f, .08f),
            materials["Wood"], false);
        CreateBlock("RailHigh", run,
            new Vector3(0f, railY + .18f, 0f),
            new Vector3(length, .07f, .08f),
            materials["Wood"], false);

        float start = -length * .5f;
        float step = posts <= 1 ? 0f : length / (posts - 1);
        for (int i = 0; i < posts; i++)
        {
            CreateBlock("Post_" + (i + 1).ToString("00"), run,
                new Vector3(start + step * i, postHeight * .5f, 0f),
                new Vector3(.1f, postHeight, .1f),
                materials["Wood"], false);
            CreateSphere("PostCap_" + (i + 1).ToString("00"), run,
                new Vector3(start + step * i, postHeight + .04f, 0f),
                Vector3.one * .12f,
                materials["Gold"], false);
        }
    }

    private static void BuildGardenGate(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform gate = CreateChild(parent, "Garden Gate");
        float z = HomeRoomShellMetrics.BackWallDecorZ(.28f);
        CreateBlock("GateLeft", gate,
            new Vector3(-.52f, .7f, z),
            new Vector3(.12f, 1.4f, .12f), materials["Wood"], false);
        CreateBlock("GateRight", gate,
            new Vector3(.52f, .7f, z),
            new Vector3(.12f, 1.4f, .12f), materials["Wood"], false);
        CreateBlock("GateArch", gate,
            new Vector3(0f, 1.42f, z),
            new Vector3(1.28f, .14f, .14f), materials["Gold"], false);
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

        GameObject keyObject = new GameObject("Garden Key Light", typeof(Light));
        keyObject.transform.SetParent(parent, false);
        keyObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        Light key = keyObject.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color32(255, 246, 210, 255);
        key.intensity = 1.05f;
        key.shadows = LightShadows.Soft;

        BuildFillLight(parent, "Sky Fill", new Vector3(-3.4f, 2.9f, -1.5f),
            new Color32(116, 244, 233, 255), .9f, 8.5f);
        BuildFillLight(parent, "Sun Fill", new Vector3(3.5f, 2.25f, .4f),
            new Color32(255, 210, 120, 255), .6f, 7.5f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedVolumePath);
        if (profile != null)
        {
            GameObject volumeObject = new GameObject("Garden Premium Volume", typeof(Volume));
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

    private static GardenBirdFlock BuildBirdFlock(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials,
        Transform roost)
    {
        Transform root = CreateChild(parent, "GardenBirdFlock");
        var birds = new Transform[2];
        for (int i = 0; i < birds.Length; i++)
        {
            Transform bird = CreateChild(root, "CourtyardBird_" + (i + 1));
            CreateSphere("Body", bird, Vector3.zero, new Vector3(.13f, .08f, .18f),
                materials["Bird"], false);
            CreateSphere("Head", bird, new Vector3(0f, .03f, .1f),
                Vector3.one * .08f, materials["Bird"], false);
            CreateBlock("Beak", bird, new Vector3(0f, .02f, .16f),
                new Vector3(.03f, .025f, .06f), materials["Lemon"], false);
            CreateBlock("WingL", bird, new Vector3(-.09f, .02f, 0f),
                new Vector3(.13f, .025f, .08f), materials["BirdWing"], false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            CreateBlock("WingR", bird, new Vector3(.09f, .02f, 0f),
                new Vector3(.13f, .025f, .08f), materials["BirdWing"], false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            bird.gameObject.SetActive(false);
            birds[i] = bird;
        }

        GardenBirdFlock flock = root.gameObject.AddComponent<GardenBirdFlock>();
        flock.EditorConfigure(birds, roost);
        return flock;
    }

    private static void BuildAmbientCritters(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials,
        Transform[] flowerAnchors)
    {
        Transform root = CreateChild(parent, "Garden Critters");
        var critters = new Transform[6];
        for (int i = 0; i < 3; i++)
        {
            Transform bee = CreateChild(root, "Bee_" + (i + 1));
            CreateSphere("BeeBody", bee, Vector3.zero, new Vector3(.055f, .04f, .07f),
                materials["Lemon"], false);
            CreateBlock("BeeStripe", bee, Vector3.zero, new Vector3(.06f, .03f, .03f),
                materials["Ink"], false);
            CreateBlock("BeeWing", bee, new Vector3(.02f, .03f, 0f),
                new Vector3(.05f, .01f, .03f), materials["Cream"], false);
            critters[i] = bee;
        }

        for (int i = 0; i < 3; i++)
        {
            Transform butterfly = CreateChild(root, "Butterfly_" + (i + 1));
            CreateSphere("Body", butterfly, Vector3.zero, new Vector3(.02f, .02f, .06f),
                materials["Ink"], false);
            CreateBlock("WingL", butterfly, new Vector3(-.045f, .01f, 0f),
                new Vector3(.07f, .01f, .05f),
                i % 2 == 0 ? materials["Coral"] : materials["Lilac"], false);
            CreateBlock("WingR", butterfly, new Vector3(.045f, .01f, 0f),
                new Vector3(.07f, .01f, .05f),
                i % 2 == 0 ? materials["Lemon"] : materials["Aqua"], false);
            critters[i + 3] = butterfly;
        }

        if (flowerAnchors != null)
        {
            for (int i = 0; i < critters.Length; i++)
            {
                Transform flower = flowerAnchors[i % flowerAnchors.Length];
                if (critters[i] == null || flower == null)
                    continue;
                critters[i].position = flower.position + Vector3.up * .22f;
            }
        }

        root.gameObject.AddComponent<GardenAmbientCritters>()
            .EditorConfigure(critters, flowerAnchors);
    }

    private static void BuildBirdAttention(
        Transform parent, CatMovement cat, GardenBirdFlock flock)
    {
        GameObject attention = new GameObject("GardenBirdAttention");
        attention.transform.SetParent(parent, false);
        attention.AddComponent<GardenBirdAttention>().EditorConfigure(cat, flock);
    }

    public static string EnsureBirdWatchActivity()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject existing = FindNamed(scene, "BirdWatchActivity");
        if (existing == null)
        {
            Transform gameplay = FindNamed(scene, "04 Gameplay")?.transform;
            Transform roost = FindNamed(scene, "BirdRoost")?.transform;
            if (gameplay == null)
            {
                GameObject root = new GameObject("04 Gameplay");
                SceneManager.MoveGameObjectToScene(root, scene);
                gameplay = root.transform;
            }

            BuildBirdWatchActivity(gameplay, roost);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (opened && scene.IsValid())
            EditorSceneManager.CloseScene(scene, true);
        return existing == null ? "bird-watch-added" : "bird-watch-present";
    }

    private static void BuildBirdWatchActivity(Transform parent, Transform roost)
    {
        if (parent != null && parent.Find("BirdWatchActivity") != null)
            return;

        Vector3 sit = roost != null
            ? roost.position + new Vector3(0.7f, -1.82f, 0.05f)
            : new Vector3(-1.75f, 0f, 0.55f);
        GameObject station = new GameObject("BirdWatchActivity");
        station.transform.SetParent(parent, false);
        station.transform.position = sit;

        GameObject content = new GameObject("UnlockedContent");
        content.transform.SetParent(station.transform, false);
        content.SetActive(false);

        Transform look = new GameObject("BirdLookPoint").transform;
        look.SetParent(station.transform, false);
        look.position = roost != null
            ? roost.position
            : sit + new Vector3(-0.7f, 1.82f, 0f);

        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(station.transform, false);
        anchor.localPosition = new Vector3(0.15f, 0f, -0.15f);

        SitLookActivity activity = station.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "bird-watch",
            "BIRD WATCH",
            CatActivityKind.BirdWatch,
            QuestType.BirdWatch,
            BondMilestoneService.BirdWatchBond,
            "WATCH",
            1.35f,
            6f,
            anchor,
            null,
            content);
        activity.EditorConfigureLook(
            look,
            SitLookReaction.Sit,
            2.8f,
            "HELLO BIRDS!");
    }

    private static GameObject FindNamed(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindDeepChild(roots[i].transform, name);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeepChild(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
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
        levelMarker.EditorSetId(HomeRoomService.GardenId);
        HomeRoomSceneMarker roomMarker = markerObject.AddComponent<HomeRoomSceneMarker>();
        roomMarker.EditorConfigure(HomeRoomService.GardenId, camera, cat);

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

    private static float Frac(float value)
    {
        return value - Mathf.Floor(value);
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
        EnsureFolder("Assets/Art/Garden");
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
