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
/// Authors Balcony Level 01 as a sunny elevated deck over a pastel city skyline.
/// The ten furnishings are hidden store products; the deck, railing and skyline
/// stay in the shell. Mirrors the Garden builder's room contract (seven authoring
/// groups, one camera/listener, cloned cat, care area) so the shared validator
/// and loader treat it exactly like every other home room.
/// </summary>
public static class BalconyLevelBuilder
{
    public const string ScenePath = HomeRoomService.BalconyScenePath;

    private const string LivingScenePath = HomeRoomService.LivingRoomScenePath;
    private const string MaterialFolder = "Assets/Art/Balcony/Materials";
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
    private static readonly Color Sky = new Color32(150, 220, 255, 255);
    private static readonly Color SkyDeep = new Color32(120, 196, 245, 255);
    private static readonly Color Deck = new Color32(214, 154, 92, 255);
    private static readonly Color DeckLight = new Color32(232, 182, 128, 255);
    private static readonly Color Rail = new Color32(255, 248, 232, 255);
    private static readonly Color CityWarm = new Color32(214, 176, 214, 255);
    private static readonly Color CityCool = new Color32(160, 176, 224, 255);
    private static readonly Color CityFar = new Color32(196, 200, 235, 255);

    private const float LivingCameraFieldOfView = 47f;
    private static readonly Vector3 LivingCameraPosition = new Vector3(-1f, 3f, -5.5f);
    private static readonly Quaternion LivingCameraRotation =
        Quaternion.Euler(25f, 12.995f, 0f);
    private static readonly Vector3 LivingSpawnPointPosition = new Vector3(0f, 0f, -2f);
    private static readonly Color LivingAmbientSkyColor = new Color32(54, 58, 66, 255);
    private static readonly Color LivingAmbientEquatorColor = new Color32(29, 32, 34, 255);
    private static readonly Color LivingAmbientGroundColor = new Color32(12, 11, 9, 255);
    private static readonly Color LivingCameraBackground = new Color32(120, 196, 245, 0);

    [MenuItem("Tools/Cat Home/Rooms/Build Balcony Level 01")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Balcony Level 01",
                "Create or replace Balcony_Level01 as a sunny deck over a city skyline?",
                "Build",
                "Cancel"))
        {
            return;
        }

        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Balcony", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Rebuild Balcony Level 01 (Silent)")]
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
        bool previousWasBalcony = previousActive.IsValid() &&
                                  string.Equals(
                                      previousActive.path,
                                      ScenePath,
                                      StringComparison.Ordinal);
        Scene livingScene = SceneManager.GetSceneByPath(LivingScenePath);
        bool openedLiving = !livingScene.IsValid() || !livingScene.isLoaded;
        if (openedLiving)
            livingScene = EditorSceneManager.OpenScene(LivingScenePath, OpenSceneMode.Additive);

        Scene existingBalcony = SceneManager.GetSceneByPath(ScenePath);
        if (existingBalcony.IsValid() && existingBalcony.isLoaded)
        {
            if (SceneManager.GetActiveScene() == existingBalcony)
                EditorSceneManager.SetActiveScene(livingScene);
            EditorSceneManager.CloseScene(existingBalcony, true);
        }

        Scene balcony = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(balcony);

        Transform environment = CreateSceneRoot(balcony, "01 Environment");
        Transform furniture = CreateSceneRoot(balcony, "02 Furniture");
        Transform character = CreateSceneRoot(balcony, "03 Character");
        Transform gameplay = CreateSceneRoot(balcony, "04 Gameplay");
        Transform presentation = CreateSceneRoot(balcony, "05 Presentation");
        CreateSceneRoot(balcony, "06 Local UI");
        Transform setup = CreateSceneRoot(balcony, "07 Level Setup");

        BuildRoomShell(environment, materials);
        Camera camera = BuildPresentation(presentation);
        CareSet care = BuildEmptyCareArea(gameplay);
        CatMovement cat = CloneAndConfigureCat(
            livingScene, character, camera, care, balcony);
        BuildGameTimeService(setup);
        BuildArchitecture(setup, camera, cat);
        StoreProductContentBuilder.BuildRoomSceneProducts(
            balcony,
            HomeRoomService.BalconyId,
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
            Debug.LogWarning("Balcony room lighting controller build failed: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(balcony);
        if (!EditorSceneManager.SaveScene(balcony, ScenePath))
            throw new InvalidOperationException("Balcony_Level01 could not be saved.");

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(balcony, true);

        if (openedLiving && !previousWasBalcony)
            EditorSceneManager.CloseScene(livingScene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);
        else if (livingScene.IsValid() && livingScene.isLoaded)
            EditorSceneManager.SetActiveScene(livingScene);

        return "Balcony Level 01 built as a sunny city-view deck with a ten-piece ROOM collection.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Balcony_Cream", Cream, .03f, .54f),
            ["Aqua"] = EnsureMaterial("Balcony_Aqua", Aqua, .02f, .62f),
            ["Mint"] = EnsureMaterial("Balcony_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Balcony_Coral", Coral, .02f, .58f),
            ["Peach"] = EnsureMaterial("Balcony_Peach", Peach, .02f, .56f),
            ["Lilac"] = EnsureMaterial("Balcony_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Balcony_Lemon", Lemon, .03f, .6f),
            ["Ink"] = EnsureMaterial("Balcony_Ink", Ink, .05f, .36f),
            ["Sky"] = EnsureMaterial("Balcony_Sky", Sky, .04f, .68f),
            ["SkyDeep"] = EnsureMaterial("Balcony_SkyDeep", SkyDeep, .04f, .64f),
            ["Deck"] = EnsureMaterial("Balcony_Deck", Deck, .04f, .36f),
            ["DeckLight"] = EnsureMaterial("Balcony_DeckLight", DeckLight, .04f, .4f),
            ["Rail"] = EnsureMaterial("Balcony_Rail", Rail, .06f, .5f),
            ["CityWarm"] = EnsureMaterial("Balcony_CityWarm", CityWarm, .02f, .42f),
            ["CityCool"] = EnsureMaterial("Balcony_CityCool", CityCool, .02f, .42f),
            ["CityFar"] = EnsureMaterial("Balcony_CityFar", CityFar, .02f, .38f),
            ["Gold"] = EnsureMaterial(
                "Balcony_Gold", new Color32(255, 191, 57, 255), .58f, .75f)
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
            materials["Deck"], true);
        HideRenderer(floor);

        BuildDeck(parent, materials);
        BuildInvisibleShellWalls(parent, materials);
        BuildHouseWall(parent, materials);
        BuildOpenSky(parent, materials);
        BuildCitySkyline(parent, materials);
        BuildRailing(parent, materials);
        BuildCornerGreens(parent, materials);
    }

    private static void BuildDeck(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform deck = CreateChild(parent, "Sunny Deck");
        GameObject blanket = CreateBlock(
            "DeckBlanket",
            deck,
            new Vector3(0f, HomeRoomShellMetrics.FloorTileCenterY, 0f),
            new Vector3(
                HomeRoomShellMetrics.FloorWidth - .04f,
                HomeRoomShellMetrics.FloorTileThickness,
                HomeRoomShellMetrics.FloorDepth - .04f),
            materials["Deck"],
            false);
        HideShadows(blanket);

        // Warm plank stripes give the deck a real wooden read.
        const int planks = 9;
        float span = HomeRoomShellMetrics.FloorWidth - .3f;
        float step = span / planks;
        float start = -span * .5f + step * .5f;
        for (int i = 0; i < planks; i++)
        {
            GameObject plank = CreateBlock(
                "Plank_" + (i + 1).ToString("00"),
                deck,
                new Vector3(start + step * i, HomeRoomShellMetrics.FloorTileCenterY + .01f, 0f),
                new Vector3(step * .82f, .02f, HomeRoomShellMetrics.FloorDepth - .1f),
                i % 2 == 0 ? materials["DeckLight"] : materials["Deck"],
                false);
            HideShadows(plank);
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

    private static void BuildHouseWall(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        // The balcony hangs off the home: a warm facade with glass doors sits at
        // the back so the open railing sides read as "outside".
        Transform facade = CreateChild(parent, "House Facade");
        float z = HomeRoomShellMetrics.BackWallDecorZ(.12f);
        CreateBlock("Facade", facade,
            new Vector3(0f, 1.5f, z + .12f),
            new Vector3(HomeRoomShellMetrics.FloorWidth - .2f, 3f, .18f),
            materials["Cream"], true);
        CreateBlock("DoorFrame", facade,
            new Vector3(0f, 1.1f, z),
            new Vector3(1.7f, 2.2f, .1f),
            materials["Rail"], false);
        CreateBlock("GlassDoor", facade,
            new Vector3(0f, 1.1f, z - .03f),
            new Vector3(1.5f, 2f, .06f),
            materials["Aqua"], false);
        CreateBlock("DoorMullion", facade,
            new Vector3(0f, 1.1f, z - .05f),
            new Vector3(.06f, 2f, .04f),
            materials["Rail"], false);
        CreateBlock("Sconce", facade,
            new Vector3(1.15f, 1.95f, z),
            new Vector3(.18f, .3f, .12f),
            materials["Gold"], false);
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
            materials["SkyDeep"], false);
        CreateBlock("SkyRight", sky,
            new Vector3(32f, 11f, 8f), new Vector3(1f, 44f, 64f),
            materials["SkyDeep"], false);

        // Warm morning sun with a soft pale halo.
        CreateSphere("SunGlow", sky,
            new Vector3(8.5f, 12.4f, 21f), new Vector3(4.6f, 4.6f, .6f),
            materials["Cream"], false);
        CreateSphere("SunDisc", sky,
            new Vector3(8.5f, 12.4f, 20.4f), Vector3.one * 2.1f,
            materials["Lemon"], false);

        BuildCloud(sky, "CloudA", new Vector3(-8.5f, 10.2f, 18.5f), 1.2f, materials);
        BuildCloud(sky, "CloudB", new Vector3(2.5f, 11.4f, 20f), .9f, materials);
        BuildCloud(sky, "CloudC", new Vector3(11f, 9.4f, 17f), 1f, materials);
        BuildCloud(sky, "CloudD", new Vector3(-2f, 12.8f, 22f), .7f, materials);
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

    private static void BuildCitySkyline(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        // A pastel skyline below the railing line so the deck reads as elevated.
        Transform city = CreateChild(parent, "City Skyline");

        // Far haze band.
        CreateBlock("HazeBand", city,
            new Vector3(0f, 2.2f, 13f), new Vector3(46f, 4.4f, 1f),
            materials["CityFar"], false);

        float[] xs =
        {
            -12f, -9.5f, -7f, -5f, -3f, -1f, 1f, 3f, 5f, 7f, 9.5f, 12f
        };
        for (int i = 0; i < xs.Length; i++)
        {
            float u = Frac(i * 0.6180339f + .13f);
            float height = 3.4f + u * 5.5f;
            float depth = 11.5f - (i % 3) * .6f;
            Material tone;
            if (i % 3 == 0)
                tone = materials["CityWarm"];
            else if (i % 3 == 1)
                tone = materials["CityCool"];
            else
                tone = materials["CityFar"];
            Transform tower = CreateChild(city, "Tower_" + (i + 1).ToString("00"));
            tower.localPosition = new Vector3(xs[i], 0f, depth);
            GameObject body = CreateBlock("Body", tower,
                new Vector3(0f, height * .5f, 0f),
                new Vector3(1.5f + (i % 2) * .5f, height, 1.4f),
                tone, false);
            HideShadows(body);
            // A few lit windows so the towers feel alive.
            for (int w = 0; w < 4; w++)
            {
                float wy = 1f + w * (height / 5f);
                GameObject win = CreateBlock("Win_" + w, tower,
                    new Vector3((w % 2 == 0 ? -.3f : .3f), wy, .72f),
                    new Vector3(.3f, .32f, .04f),
                    (i + w) % 2 == 0 ? materials["Lemon"] : materials["Cream"],
                    false);
                HideShadows(win);
            }
            if (i % 4 == 0)
            {
                CreateBlock("Roof", tower,
                    new Vector3(0f, height + .1f, 0f),
                    new Vector3(.2f, .6f, .2f), materials["Ink"], false);
            }
        }
    }

    private static void BuildRailing(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform railing = CreateChild(parent, "Deck Railing");
        const float postHeight = 1.02f;
        const float topRailY = 1.02f;
        float innerX = HomeRoomShellMetrics.InteriorMaxX - .1f;
        float frontZ = HomeRoomShellMetrics.InteriorMinZ + .1f;

        BuildRailRun(railing, "FrontRail",
            new Vector3(0f, 0f, frontZ),
            HomeRoomShellMetrics.FloorWidth - .35f, 0f,
            materials, topRailY, postHeight, 8);
        BuildRailRun(railing, "LeftRail",
            new Vector3(-innerX, 0f, .35f),
            HomeRoomShellMetrics.FloorDepth - 1.1f, 90f,
            materials, topRailY, postHeight, 6);
        BuildRailRun(railing, "RightRail",
            new Vector3(innerX, 0f, .35f),
            HomeRoomShellMetrics.FloorDepth - 1.1f, 90f,
            materials, topRailY, postHeight, 6);
    }

    private static void BuildRailRun(
        Transform parent, string name, Vector3 center, float length, float yaw,
        IReadOnlyDictionary<string, Material> materials,
        float topRailY, float postHeight, int posts)
    {
        Transform run = CreateChild(parent, name);
        run.localPosition = center;
        run.localRotation = Quaternion.Euler(0f, yaw, 0f);

        CreateBlock("TopRail", run,
            new Vector3(0f, topRailY, 0f),
            new Vector3(length, .08f, .1f), materials["Rail"], false);
        CreateBlock("MidRail", run,
            new Vector3(0f, topRailY - .34f, 0f),
            new Vector3(length, .05f, .07f), materials["Rail"], false);

        float start = -length * .5f;
        float step = posts <= 1 ? 0f : length / (posts - 1);
        for (int i = 0; i < posts; i++)
        {
            CreateBlock("Post_" + (i + 1).ToString("00"), run,
                new Vector3(start + step * i, postHeight * .5f, 0f),
                new Vector3(.07f, postHeight, .07f),
                materials["Rail"], false);
        }

        // Slim baluster infill between the posts keeps the railing safe-looking.
        int balusters = posts * 3;
        float bstep = length / balusters;
        for (int i = 0; i < balusters; i++)
        {
            CreateBlock("Baluster_" + (i + 1).ToString("00"), run,
                new Vector3(-length * .5f + bstep * (i + .5f), postHeight * .5f - .18f, 0f),
                new Vector3(.03f, postHeight - .36f, .03f),
                materials["Rail"], false);
        }
    }

    private static void BuildCornerGreens(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform greens = CreateChild(parent, "Deck Greens");
        Vector3[] spots =
        {
            new Vector3(-3.0f, 0f, 2.0f),
            new Vector3(3.0f, 0f, 2.0f)
        };
        Material[] leaf = { materials["Mint"], materials["Aqua"] };
        for (int i = 0; i < spots.Length; i++)
        {
            Transform pot = CreateChild(greens, "PlanterPot_" + (i + 1));
            pot.localPosition = spots[i];
            CreateBlock("Pot", pot,
                new Vector3(0f, .2f, 0f), new Vector3(.4f, .4f, .4f),
                materials["Peach"], false);
            CreateSphere("Foliage", pot,
                new Vector3(0f, .62f, 0f), new Vector3(.62f, .72f, .62f),
                leaf[i % leaf.Length], false);
            CreateSphere("FoliageHigh", pot,
                new Vector3(.1f, .95f, -.06f), new Vector3(.42f, .5f, .42f),
                materials["Mint"], false);
        }
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

        GameObject keyObject = new GameObject("Balcony Key Light", typeof(Light));
        keyObject.transform.SetParent(parent, false);
        keyObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        Light key = keyObject.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color32(255, 246, 210, 255);
        key.intensity = 1.05f;
        key.shadows = LightShadows.Soft;

        BuildFillLight(parent, "Sky Fill", new Vector3(-3.4f, 2.9f, -1.5f),
            new Color32(150, 220, 255, 255), .9f, 8.5f);
        BuildFillLight(parent, "Sun Fill", new Vector3(3.5f, 2.25f, .4f),
            new Color32(255, 210, 120, 255), .6f, 7.5f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedVolumePath);
        if (profile != null)
        {
            GameObject volumeObject = new GameObject("Balcony Premium Volume", typeof(Volume));
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
        levelMarker.EditorSetId(HomeRoomService.BalconyId);
        HomeRoomSceneMarker roomMarker = markerObject.AddComponent<HomeRoomSceneMarker>();
        roomMarker.EditorConfigure(HomeRoomService.BalconyId, camera, cat);

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
        EnsureFolder("Assets/Art/Balcony");
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
