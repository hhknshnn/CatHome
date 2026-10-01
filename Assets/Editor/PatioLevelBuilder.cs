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
/// Authors Patio Level 01 as a paved garden patio ringed by hedges under an open
/// sky. The ten furnishings are hidden store products; the paved floor, low wall,
/// hedges and garden backdrop stay in the shell. Mirrors the Garden/Balcony room
/// contract (seven authoring groups, one camera/listener, cloned cat, care area)
/// so the shared validator and loader treat it like every other home room.
/// </summary>
public static class PatioLevelBuilder
{
    public const string ScenePath = HomeRoomService.PatioScenePath;

    private const string LivingScenePath = HomeRoomService.LivingRoomScenePath;
    private const string MaterialFolder = "Assets/Art/Patio/Materials";
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
    private static readonly Color Sky = new Color32(140, 216, 255, 255);
    private static readonly Color Grass = new Color32(138, 211, 110, 255);
    private static readonly Color GrassDeep = new Color32(91, 176, 91, 255);
    private static readonly Color Stone = new Color32(235, 225, 208, 255);
    private static readonly Color StoneLight = new Color32(252, 244, 230, 255);
    private static readonly Color Wood = new Color32(228, 177, 112, 255);

    private static readonly Vector3 LivingSpawnPointPosition = new Vector3(0f, 0f, -2f);
    private static readonly Color LivingAmbientSkyColor = new Color32(54, 58, 66, 255);
    private static readonly Color LivingAmbientEquatorColor = new Color32(29, 32, 34, 255);
    private static readonly Color LivingAmbientGroundColor = new Color32(12, 11, 9, 255);
    private static readonly Color LivingCameraBackground = new Color32(120, 196, 245, 0);

    [MenuItem("Tools/Cat Home/Rooms/Build Patio Level 01")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Patio Level 01",
                "Create or replace Patio_Level01 as a paved garden patio?",
                "Build",
                "Cancel"))
        {
            return;
        }

        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Patio", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Rebuild Patio Level 01 (Silent)")]
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
        bool previousWasPatio = previousActive.IsValid() &&
                                string.Equals(
                                    previousActive.path,
                                    ScenePath,
                                    StringComparison.Ordinal);
        Scene livingScene = SceneManager.GetSceneByPath(LivingScenePath);
        bool openedLiving = !livingScene.IsValid() || !livingScene.isLoaded;
        if (openedLiving)
            livingScene = EditorSceneManager.OpenScene(LivingScenePath, OpenSceneMode.Additive);

        Scene existingPatio = SceneManager.GetSceneByPath(ScenePath);
        if (existingPatio.IsValid() && existingPatio.isLoaded)
        {
            if (SceneManager.GetActiveScene() == existingPatio)
                EditorSceneManager.SetActiveScene(livingScene);
            EditorSceneManager.CloseScene(existingPatio, true);
        }

        Scene patio = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(patio);

        Transform environment = CreateSceneRoot(patio, "01 Environment");
        Transform furniture = CreateSceneRoot(patio, "02 Furniture");
        Transform character = CreateSceneRoot(patio, "03 Character");
        Transform gameplay = CreateSceneRoot(patio, "04 Gameplay");
        Transform presentation = CreateSceneRoot(patio, "05 Presentation");
        CreateSceneRoot(patio, "06 Local UI");
        Transform setup = CreateSceneRoot(patio, "07 Level Setup");

        BuildRoomShell(environment, materials);
        HomeRoomShellVisualPolishBuilder.Apply(patio, HomeRoomService.PatioId, environment);
        Camera camera = BuildPresentation(presentation);
        CareSet care = BuildEmptyCareArea(gameplay);
        CatMovement cat = CloneAndConfigureCat(
            livingScene, character, camera, care, patio);
        BuildGameTimeService(setup);
        BuildArchitecture(setup, camera, cat);
        StoreProductContentBuilder.BuildRoomSceneProducts(
            patio,
            HomeRoomService.PatioId,
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
            Debug.LogWarning("Patio room lighting controller build failed: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(patio);
        if (!EditorSceneManager.SaveScene(patio, ScenePath))
            throw new InvalidOperationException("Patio_Level01 could not be saved.");

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(patio, true);

        if (openedLiving && !previousWasPatio)
            EditorSceneManager.CloseScene(livingScene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);
        else if (livingScene.IsValid() && livingScene.isLoaded)
            EditorSceneManager.SetActiveScene(livingScene);

        return "Patio Level 01 built as a paved garden patio with a ten-piece ROOM collection.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Patio_Cream", Cream, .03f, .54f),
            ["Aqua"] = EnsureMaterial("Patio_Aqua", Aqua, .02f, .62f),
            ["Mint"] = EnsureMaterial("Patio_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Patio_Coral", Coral, .02f, .58f),
            ["Peach"] = EnsureMaterial("Patio_Peach", Peach, .02f, .56f),
            ["Lilac"] = EnsureMaterial("Patio_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Patio_Lemon", Lemon, .03f, .6f),
            ["Ink"] = EnsureMaterial("Patio_Ink", Ink, .05f, .36f),
            ["Sky"] = EnsureMaterial("Patio_Sky", Sky, .04f, .68f),
            ["Grass"] = EnsureMaterial("Patio_Grass", Grass, .02f, .28f),
            ["GrassDeep"] = EnsureMaterial("Patio_GrassDeep", GrassDeep, .02f, .24f),
            ["Stone"] = EnsureMaterial("Patio_Stone", Stone, .03f, .32f),
            ["StoneLight"] = EnsureMaterial("Patio_StoneLight", StoneLight, .03f, .36f),
            ["Wood"] = EnsureMaterial("Patio_Wood", Wood, .04f, .36f),
            ["Gold"] = EnsureMaterial(
                "Patio_Gold", new Color32(255, 191, 57, 255), .58f, .75f)
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
            materials["Stone"], true);
        HideRenderer(floor);

        BuildPavedFloor(parent, materials);
        BuildInvisibleShellWalls(parent, materials);
        BuildLowWall(parent, materials);
        BuildHedgeBorder(parent, materials);
        BuildOpenSky(parent, materials);
        BuildGardenBackdrop(parent, materials);
        BuildCornerPlanters(parent, materials);
    }

    private static void BuildPavedFloor(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform paving = CreateChild(parent, "Paved Patio");
        GameObject blanket = CreateBlock(
            "PaveBlanket",
            paving,
            new Vector3(0f, HomeRoomShellMetrics.FloorTileCenterY, 0f),
            new Vector3(
                HomeRoomShellMetrics.FloorWidth - .04f,
                HomeRoomShellMetrics.FloorTileThickness,
                HomeRoomShellMetrics.FloorDepth - .04f),
            materials["Stone"],
            false);
        HideShadows(blanket);

        // Checkered flagstones read as a real paved patio.
        const int cols = 7;
        const int rows = 5;
        float w = (HomeRoomShellMetrics.FloorWidth - .3f) / cols;
        float d = (HomeRoomShellMetrics.FloorDepth - .3f) / rows;
        float sx = -(HomeRoomShellMetrics.FloorWidth - .3f) * .5f + w * .5f;
        float sz = -(HomeRoomShellMetrics.FloorDepth - .3f) * .5f + d * .5f;
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                GameObject tile = CreateBlock(
                    "Flagstone_" + c + "_" + r,
                    paving,
                    new Vector3(sx + c * w, HomeRoomShellMetrics.FloorTileCenterY + .01f, sz + r * d),
                    new Vector3(w * .9f, .02f, d * .9f),
                    (c + r) % 2 == 0 ? materials["StoneLight"] : materials["Stone"],
                    false);
                HideShadows(tile);
            }
        }
    }

    private static void BuildInvisibleShellWalls(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform walls = CreateChild(parent, "Open Air Walls");
        HideRenderer(CreateBlock("BackWall_Air", walls,
            HomeRoomShellMetrics.BackWallPosition, HomeRoomShellMetrics.BackWallScale,
            materials["Sky"], true));
        HideRenderer(CreateBlock("LeftWall_Air", walls,
            HomeRoomShellMetrics.LeftWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Sky"], true));
        HideRenderer(CreateBlock("RightWall_Air", walls,
            HomeRoomShellMetrics.RightWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Sky"], true));
    }

    private static void BuildLowWall(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform wall = CreateChild(parent, "Patio Low Wall");
        float innerX = HomeRoomShellMetrics.InteriorMaxX - .1f;
        float backZ = HomeRoomShellMetrics.InteriorMaxZ - .1f;
        float frontZ = HomeRoomShellMetrics.InteriorMinZ + .1f;

        BuildWallRun(wall, "FrontLeftWall",
            new Vector3(-2.2f, 0f, frontZ), 3.0f, 0f, materials);
        BuildWallRun(wall, "FrontRightWall",
            new Vector3(2.2f, 0f, frontZ), 3.0f, 0f, materials);
        BuildWallRun(wall, "BackLeftWall",
            new Vector3(-2.4f, 0f, backZ), 2.4f, 0f, materials);
        BuildWallRun(wall, "BackRightWall",
            new Vector3(2.4f, 0f, backZ), 2.4f, 0f, materials);
        BuildWallRun(wall, "LeftWall_Stone",
            new Vector3(-innerX, 0f, 0f),
            HomeRoomShellMetrics.FloorDepth - .35f, 90f, materials);
        BuildWallRun(wall, "RightWall_Stone",
            new Vector3(innerX, 0f, 0f),
            HomeRoomShellMetrics.FloorDepth - .35f, 90f, materials);
    }

    private static void BuildWallRun(
        Transform parent, string name, Vector3 center, float length, float yaw,
        IReadOnlyDictionary<string, Material> materials)
    {
        Transform run = CreateChild(parent, name);
        run.localPosition = center;
        run.localRotation = Quaternion.Euler(0f, yaw, 0f);

        HomeEnvironmentCollisionBuilder.CreateSolidBlock("WallBody", run,
            new Vector3(0f, .28f, 0f),
            new Vector3(length, .56f, .18f), materials["Cream"], HomeEnvironmentCollisionBuilder.SolidRole.Wall);
        HomeEnvironmentCollisionBuilder.CreateSolidBlock("WallCap", run,
            new Vector3(0f, .58f, 0f),
            new Vector3(length, .07f, .24f), materials["StoneLight"], HomeEnvironmentCollisionBuilder.SolidRole.Wall);

        int urns = Mathf.Max(2, Mathf.RoundToInt(length / 1.6f));
        float step = urns <= 1 ? 0f : length / (urns - 1);
        for (int i = 0; i < urns; i++)
        {
            float x = -length * .5f + step * i;
            HomeEnvironmentCollisionBuilder.CreateSolidBlock("Pilaster_" + (i + 1), run,
                new Vector3(x, .32f, 0f),
                new Vector3(.18f, .64f, .26f), materials["StoneLight"], HomeEnvironmentCollisionBuilder.SolidRole.Post);
            CreateSphere("Urn_" + (i + 1), run,
                new Vector3(x, .72f, 0f), Vector3.one * .18f, materials["Gold"], false);
        }
    }

    private static void BuildHedgeBorder(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform hedge = CreateChild(parent, "Patio Hedges");
        float outerX = HomeRoomShellMetrics.InteriorMaxX + .35f;
        float backZ = HomeRoomShellMetrics.InteriorMaxZ + .35f;

        CreateBlock("HedgeBack", hedge,
            new Vector3(0f, .5f, backZ),
            new Vector3(HomeRoomShellMetrics.FloorWidth + .6f, 1f, .6f),
            materials["GrassDeep"], false);
        CreateBlock("HedgeLeft", hedge,
            new Vector3(-outerX, .5f, .4f),
            new Vector3(.6f, 1f, HomeRoomShellMetrics.FloorDepth + .2f),
            materials["GrassDeep"], false);
        CreateBlock("HedgeRight", hedge,
            new Vector3(outerX, .5f, .4f),
            new Vector3(.6f, 1f, HomeRoomShellMetrics.FloorDepth + .2f),
            materials["GrassDeep"], false);

        // Rounded topiary bumps break up the hedge silhouette.
        for (int i = 0; i < 9; i++)
        {
            float x = -3.6f + i * .9f;
            CreateSphere("TopiaryBack_" + (i + 1), hedge,
                new Vector3(x, 1.0f, backZ), new Vector3(.7f, .55f, .55f),
                i % 2 == 0 ? materials["Grass"] : materials["Mint"], false);
        }
    }

    private static void BuildOpenSky(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform sky = CreateChild(parent, "Open Sky");

        CreateBlock("SkyBack", sky,
            new Vector3(0f, 11f, 27f), new Vector3(80f, 44f, 1f),
            materials["Sky"], false);
        CreateBlock("SkyTop", sky,
            new Vector3(0f, 24f, 6f), new Vector3(80f, 1f, 62f),
            materials["Sky"], false);
        CreateBlock("SkyLeft", sky,
            new Vector3(-32f, 11f, 8f), new Vector3(1f, 44f, 64f),
            materials["Sky"], false);
        CreateBlock("SkyRight", sky,
            new Vector3(32f, 11f, 8f), new Vector3(1f, 44f, 64f),
            materials["Sky"], false);

        CreateSphere("SunGlow", sky,
            new Vector3(8.5f, 12.4f, 21f), new Vector3(4.6f, 4.6f, .6f),
            materials["Cream"], false);
        CreateSphere("SunDisc", sky,
            new Vector3(8.5f, 12.4f, 20.4f), Vector3.one * 2.1f,
            materials["Lemon"], false);

        BuildCloud(sky, "CloudA", new Vector3(-8.5f, 10.2f, 18.5f), 1.2f, materials);
        BuildCloud(sky, "CloudB", new Vector3(2.5f, 11.4f, 20f), .9f, materials);
        BuildCloud(sky, "CloudC", new Vector3(11f, 9.4f, 17f), 1f, materials);
    }

    private static void BuildCloud(
        Transform parent, string name, Vector3 center, float scale,
        IReadOnlyDictionary<string, Material> materials)
    {
        Transform cloud = CreateChild(parent, name);
        cloud.localPosition = center;
        CreateSphere("Puff_1", cloud, Vector3.zero,
            new Vector3(2.6f * scale, 1.2f * scale, 1.1f * scale),
            materials["Cream"], false);
        CreateSphere("Puff_2", cloud, new Vector3(-1.3f * scale, -.25f * scale, .1f),
            new Vector3(1.9f * scale, .95f * scale, 1f * scale),
            materials["Cream"], false);
        CreateSphere("Puff_3", cloud, new Vector3(1.35f * scale, -.2f * scale, -.1f),
            new Vector3(2f * scale, 1f * scale, 1f * scale),
            materials["Cream"], false);
    }

    private static void BuildGardenBackdrop(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform vista = CreateChild(parent, "Garden Backdrop");

        CreateBlock("FarLawn", vista,
            new Vector3(0f, .01f, 13f), new Vector3(40f, .03f, 20f),
            materials["Grass"], false);
        CreateSphere("HillA", vista,
            new Vector3(-10f, .2f, 18f), new Vector3(12f, 3.4f, 5f),
            materials["GrassDeep"], false);
        CreateSphere("HillB", vista,
            new Vector3(8f, .1f, 19f), new Vector3(13f, 3.8f, 5.4f),
            materials["Mint"], false);

        float[] xs = { -12f, -8.5f, -5f, 5f, 8.5f, 12f };
        float[] zs = { 15f, 14f, 15.5f, 14f, 15f, 14.5f };
        for (int i = 0; i < xs.Length; i++)
        {
            Transform tree = CreateChild(vista, "FarTree_" + (i + 1).ToString("00"));
            tree.localPosition = new Vector3(xs[i], 0f, zs[i]);
            float height = 2.6f + (i % 3) * .8f;
            CreateBlock("Trunk", tree,
                new Vector3(0f, height * .35f, 0f),
                new Vector3(.4f, height * .7f, .4f), materials["Wood"], false);
            CreateSphere("CrownLow", tree,
                new Vector3(0f, height * .85f, 0f), new Vector3(2.2f, 1.9f, 2.2f),
                i % 2 == 0 ? materials["GrassDeep"] : materials["Mint"], false);
            CreateSphere("CrownHigh", tree,
                new Vector3(.15f, height * 1.15f, -.1f), new Vector3(1.5f, 1.4f, 1.5f),
                i % 2 == 0 ? materials["Mint"] : materials["GrassDeep"], false);
        }
    }

    private static void BuildCornerPlanters(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform planters = CreateChild(parent, "Patio Planters");
        Vector3[] spots =
        {
            new Vector3(-3.0f, 0f, 2.0f),
            new Vector3(3.0f, 0f, 2.0f)
        };
        Material[] blooms = { materials["Coral"], materials["Lilac"] };
        for (int i = 0; i < spots.Length; i++)
        {
            Transform pot = CreateChild(planters, "CornerPot_" + (i + 1));
            pot.localPosition = spots[i];
            HomeEnvironmentCollisionBuilder.CreateSolidBlock("Pot", pot,
                new Vector3(0f, .24f, 0f), new Vector3(.46f, .48f, .46f),
                materials["StoneLight"], HomeEnvironmentCollisionBuilder.SolidRole.FixedPlanter);
            CreateSphere("Bush", pot,
                new Vector3(0f, .66f, 0f), new Vector3(.6f, .6f, .6f),
                materials["Mint"], false);
            CreateSphere("Bloom", pot,
                new Vector3(.12f, .8f, .05f), Vector3.one * .18f,
                blooms[i % blooms.Length], false);
        }
    }

    private static Camera BuildPresentation(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera),
            typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        Camera camera = cameraObject.GetComponent<Camera>();
        HomeRoomCameraProfile.Apply(camera);
        camera.nearClipPlane = .3f;
        camera.farClipPlane = 1000f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = LivingCameraBackground;
        camera.allowHDR = true;
        camera.allowMSAA = true;
        camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

        GameObject keyObject = new GameObject("Patio Key Light", typeof(Light));
        keyObject.transform.SetParent(parent, false);
        keyObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        Light key = keyObject.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color32(255, 246, 210, 255);
        key.intensity = 1.05f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = .46f;

        BuildFillLight(parent, "Sky Fill", new Vector3(-3.4f, 2.9f, -1.5f),
            new Color32(140, 216, 255, 255), .9f, 8.5f);
        BuildFillLight(parent, "Sun Fill", new Vector3(3.5f, 2.25f, .4f),
            new Color32(255, 210, 120, 255), .6f, 7.5f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedVolumePath);
        if (profile != null)
        {
            GameObject volumeObject = new GameObject("Patio Premium Volume", typeof(Volume));
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
        levelMarker.EditorSetId(HomeRoomService.PatioId);
        HomeRoomSceneMarker roomMarker = markerObject.AddComponent<HomeRoomSceneMarker>();
        roomMarker.EditorConfigure(HomeRoomService.PatioId, camera, cat);

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
        EnsureFolder("Assets/Art/Patio");
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
