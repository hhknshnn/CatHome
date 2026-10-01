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
/// Authors Bathroom Level 01 from the dedicated fixture kit, existing low-poly
/// props and a polished procedural room shell. Fixture resolution is always
/// prefab, then FBX, then a safe procedural fallback.
/// </summary>
public static class BathroomLevelBuilder
{
    public const string ScenePath = HomeRoomService.BathroomScenePath;

    private const string LivingScenePath = HomeRoomService.LivingRoomScenePath;
    private const string MaterialFolder = "Assets/Art/Bathroom/Materials";
    private const string PrefabFolder = "Assets/Art/Bathroom/Prefabs";
    private const string ModelFolder = "Assets/Art/Bathroom/Models";
    private const string SharedVolumePath =
        "Assets/Art/PremiumWorld/CatHomeRoom_PremiumVolume.asset";

    private const string TubModel = ModelFolder + "/BathroomTub.fbx";
    private const string VanityModel = ModelFolder + "/BathroomVanitySink.fbx";
    private const string ToiletModel = ModelFolder + "/BathroomToilet.fbx";
    private const string ShowerModel = ModelFolder + "/BathroomShower.fbx";

    private static readonly Color Cream = new Color32(255, 250, 236, 255);
    private static readonly Color Aqua = new Color32(145, 231, 222, 255);
    private static readonly Color Mint = new Color32(184, 242, 211, 255);
    private static readonly Color Coral = new Color32(255, 181, 176, 255);
    private static readonly Color Lilac = new Color32(218, 195, 243, 255);
    private static readonly Color Lemon = new Color32(255, 232, 139, 255);
    private static readonly Color Ink = new Color32(63, 47, 80, 255);
    private static readonly Color Mirror = new Color32(174, 240, 246, 255);
    private static readonly Color Water = new Color32(83, 211, 244, 255);
    private static readonly Vector3 LivingSpawnPointPosition = new Vector3(0f, 0f, -2f);
    private static readonly Color LivingAmbientSkyColor = new Color32(54, 58, 66, 255);
    private static readonly Color LivingAmbientEquatorColor = new Color32(29, 32, 34, 255);
    private static readonly Color LivingAmbientGroundColor = new Color32(12, 11, 9, 255);
    private static readonly Color LivingCameraBackground = new Color32(49, 77, 121, 0);

    // Back-wall composition for the canonical 8 x 6 shell. The doorway takes the
    // centre span above the tub, which is the lowest back-wall fixture, and the
    // bubble art sits high above the vanity so neither hides behind a product.
    private const int RibbonTileCount = 18;
    private const float RibbonStepX = .42f;
    private const float RibbonStartX = -3.57f;
    private const float RibbonCenterY = 1.3f;
    private const float DoorwayCenterX = .9f;

    [MenuItem("Tools/Cat Home/Rooms/Build Bathroom Level 01")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Bathroom Level 01",
                "Create or replace Bathroom_Level01 using the premium fixture kit?",
                "Build",
                "Cancel"))
        {
            return;
        }

        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Bathroom", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Rebuild Bathroom Level 01 (Silent)")]
    public static void BuildBatch()
    {
        Debug.Log(BuildSilently());
    }

    [MenuItem("Tools/Cat Home/Rooms/Optimize Bathroom Fixture Imports")]
    public static void OptimizeFixtureImports()
    {
        ConfigureFixtureImporter(TubModel);
        ConfigureFixtureImporter(VanityModel);
        ConfigureFixtureImporter(ToiletModel);
        ConfigureFixtureImporter(ShowerModel);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Bathroom fixture imports optimized for runtime use.");
    }

    public static string BuildSilently()
    {
        EnsureFolders();
        ConfigureFixtureImporter(TubModel);
        ConfigureFixtureImporter(VanityModel);
        ConfigureFixtureImporter(ToiletModel);
        ConfigureFixtureImporter(ShowerModel);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Dictionary<string, Material> materials = BuildMaterials();
        StoreProductContentBuilder.BuildProductAssetsSilently();
        Scene previousActive = SceneManager.GetActiveScene();
        Scene livingScene = SceneManager.GetSceneByPath(LivingScenePath);
        bool openedLiving = !livingScene.IsValid() || !livingScene.isLoaded;
        if (openedLiving)
            livingScene = EditorSceneManager.OpenScene(LivingScenePath, OpenSceneMode.Additive);

        Scene existingBathroom = SceneManager.GetSceneByPath(ScenePath);
        if (existingBathroom.IsValid() && existingBathroom.isLoaded)
        {
            if (SceneManager.GetActiveScene() == existingBathroom)
                EditorSceneManager.SetActiveScene(livingScene);
            EditorSceneManager.CloseScene(existingBathroom, true);
        }

        Scene bathroom = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(bathroom);

        Transform environment = CreateSceneRoot(bathroom, "01 Environment");
        Transform furniture = CreateSceneRoot(bathroom, "02 Furniture");
        Transform character = CreateSceneRoot(bathroom, "03 Character");
        Transform gameplay = CreateSceneRoot(bathroom, "04 Gameplay");
        Transform presentation = CreateSceneRoot(bathroom, "05 Presentation");
        CreateSceneRoot(bathroom, "06 Local UI");
        Transform setup = CreateSceneRoot(bathroom, "07 Level Setup");

        BuildRoomShell(environment, materials);
        HomeRoomShellVisualPolishBuilder.Apply(bathroom, HomeRoomService.BathroomId, environment);
        Camera camera = BuildPresentation(presentation, materials);
        BuildEmptyBathroomUsables(gameplay);
        CareSet care = BuildEmptyCareArea(gameplay);
        CatMovement cat = CloneAndConfigureCat(
            livingScene, character, camera, care, bathroom);
        BuildGameTimeService(setup);
        BuildArchitecture(setup, camera, cat);
        StoreProductContentBuilder.BuildRoomSceneProducts(
            bathroom,
            HomeRoomService.BathroomId,
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
            Debug.LogWarning("Bathroom room lighting controller build failed: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(bathroom);
        if (!EditorSceneManager.SaveScene(bathroom, ScenePath))
            throw new InvalidOperationException("Bathroom_Level01 could not be saved.");

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(bathroom, true);

        if (openedLiving)
            EditorSceneManager.CloseScene(livingScene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return "Bathroom Level 01 built empty with a ten-piece placeable ROOM collection.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Bathroom_Cream", Cream, .08f, .54f),
            ["Aqua"] = EnsureMaterial("Bathroom_Aqua", Aqua, .03f, .68f),
            ["Mint"] = EnsureMaterial("Bathroom_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Bathroom_Coral", Coral, .02f, .61f),
            ["Lilac"] = EnsureMaterial("Bathroom_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Bathroom_Lemon", Lemon, .04f, .62f),
            ["Ink"] = EnsureMaterial("Bathroom_Ink", Ink, .08f, .38f),
            ["Mirror"] = EnsureMaterial("Bathroom_Mirror", Mirror, .42f, .9f),
            ["Water"] = EnsureMaterial("Bathroom_Water", Water, .08f, .86f),
            ["Food"] = EnsureMaterial("Bathroom_CatFood", new Color32(218, 145, 73, 255), .01f, .35f),
            ["White"] = EnsureMaterial("Bathroom_Porcelain", Color.white, .04f, .82f),
            ["Gold"] = EnsureMaterial("Bathroom_Gold", new Color32(255, 192, 67, 255), .63f, .75f)
        };
    }

    private static Material EnsureMaterial(
        string name, Color color, float metallic, float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
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
            materials["Cream"], true);
        floor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        Transform tiles = CreateChild(parent, "Glossy Floor Tiles");
        for (int x = 0; x < HomeRoomShellMetrics.FloorTileColumns; x++)
        {
            for (int z = 0; z < HomeRoomShellMetrics.FloorTileRows; z++)
            {
                Material material = ((x + z) & 1) == 0 ? materials["Cream"] : materials["Mint"];
                GameObject tile = CreateBlock(
                    $"Tile_{x + 1:00}_{z + 1:00}",
                    tiles,
                    HomeRoomShellMetrics.FloorTilePosition(x, z),
                    new Vector3(
                        HomeRoomShellMetrics.FloorTileSize,
                        HomeRoomShellMetrics.FloorTileThickness,
                        HomeRoomShellMetrics.FloorTileSize),
                    material,
                    false);
                tile.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        Transform walls = CreateChild(parent, "Glossy Walls");
        CreateBlock("BackWall_Cream", walls,
            HomeRoomShellMetrics.BackWallPosition, HomeRoomShellMetrics.BackWallScale,
            materials["Cream"], true);
        HomeEnvironmentCollisionBuilder.CreateSolidBlock("BackWall_AquaWainscot", walls,
            HomeRoomShellMetrics.BackWainscotPosition,
            HomeRoomShellMetrics.BackWainscotScale, materials["Aqua"], HomeEnvironmentCollisionBuilder.SolidRole.Wall);
        CreateBlock("LeftWall_Cream", walls,
            HomeRoomShellMetrics.LeftWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Cream"], true);
        HomeEnvironmentCollisionBuilder.CreateSolidBlock("LeftWall_MintWainscot", walls,
            HomeRoomShellMetrics.LeftWainscotPosition,
            HomeRoomShellMetrics.SideWainscotScale, materials["Mint"], HomeEnvironmentCollisionBuilder.SolidRole.Wall);
        CreateBlock("RightWall_Cream", walls,
            HomeRoomShellMetrics.RightWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Cream"], true);
        HomeEnvironmentCollisionBuilder.CreateSolidBlock("RightWall_LilacWainscot", walls,
            HomeRoomShellMetrics.RightWainscotPosition,
            HomeRoomShellMetrics.SideWainscotScale, materials["Lilac"], HomeEnvironmentCollisionBuilder.SolidRole.Wall);

        CreateBlock("BackBaseboard", walls,
            HomeRoomShellMetrics.BackBaseboardPosition,
            HomeRoomShellMetrics.BackBaseboardScale, materials["Gold"], false);
        CreateBlock("LeftBaseboard", walls,
            HomeRoomShellMetrics.LeftBaseboardPosition,
            HomeRoomShellMetrics.SideBaseboardScale, materials["Gold"], false);
        CreateBlock("RightBaseboard", walls,
            HomeRoomShellMetrics.RightBaseboardPosition,
            HomeRoomShellMetrics.SideBaseboardScale, materials["Gold"], false);

        Transform ribbon = CreateChild(walls, "Coral Lilac Tile Ribbon");
        for (int i = 0; i < RibbonTileCount; i++)
        {
            Material material = i % 3 == 0 ? materials["Coral"]
                : i % 3 == 1 ? materials["Lilac"] : materials["Lemon"];
            CreateBlock("RibbonTile_" + (i + 1).ToString("00"), ribbon,
                new Vector3(
                    RibbonStartX + i * RibbonStepX,
                    RibbonCenterY,
                    HomeRoomShellMetrics.BackWallDecorZ(.15f)),
                new Vector3(.38f, .3f, .08f), material, false);
        }

        BuildDoorway(walls, materials);
        BuildBubbleWallArt(walls, materials);
    }

    private static void BuildDoorway(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform door = CreateChild(parent, "Room Portal Doorway");
        const float x = DoorwayCenterX;
        CreateBlock("DoorPanel", door,
            new Vector3(x, .95f, HomeRoomShellMetrics.BackWallDecorZ(.2f)),
            new Vector3(1.45f, 1.9f, .13f), materials["Lilac"], false);
        CreateBlock("DoorInset", door,
            new Vector3(x, .99f, HomeRoomShellMetrics.BackWallDecorZ(.3f)),
            new Vector3(1.15f, 1.64f, .08f), materials["Cream"], false);
        CreateBlock("DoorTop", door,
            new Vector3(x, 1.98f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(1.72f, .24f, .24f), materials["Coral"], false);
        CreateBlock("DoorLeft", door,
            new Vector3(x - .77f, 1.03f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(.24f, 2.06f, .24f), materials["Coral"], false);
        CreateBlock("DoorRight", door,
            new Vector3(x + .77f, 1.03f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(.24f, 2.06f, .24f), materials["Coral"], false);
        CreateSphere("DoorKnob", door,
            new Vector3(x - .48f, .89f, HomeRoomShellMetrics.BackWallDecorZ(.43f)),
            new Vector3(.13f, .13f, .13f), materials["Gold"], false);
    }

    private static void BuildBubbleWallArt(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform art = CreateChild(parent, "Bubble Wall Art");
        float near = HomeRoomShellMetrics.BackWallDecorZ(.21f);
        float far = HomeRoomShellMetrics.BackWallDecorZ(.23f);
        Vector3[] positions =
        {
            new Vector3(-3.05f, 2.38f, far), new Vector3(-2.72f, 2.63f, near),
            new Vector3(-2.45f, 2.35f, far), new Vector3(-2.2f, 2.64f, near)
        };
        float[] sizes = { .28f, .18f, .22f, .12f };
        for (int i = 0; i < positions.Length; i++)
        {
            Material material = (i & 1) == 0 ? materials["Mirror"] : materials["Lilac"];
            CreateSphere("Bubble_" + (i + 1), art, positions[i],
                Vector3.one * sizes[i], material, false);
        }
    }

    private static Camera BuildPresentation(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
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

        GameObject sunObject = new GameObject("Bathroom Key Light", typeof(Light));
        sunObject.transform.SetParent(parent, false);
        sunObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light sun = sunObject.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color32(255, 245, 220, 255);
        sun.intensity = 1.08f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .55f;

        BuildFillLight(parent, "Aqua Fill", new Vector3(-3.4f, 2.9f, -1.5f),
            new Color32(116, 244, 233, 255), .82f, 8.5f);
        BuildFillLight(parent, "Peach Fill", new Vector3(3.5f, 2.25f, .4f),
            new Color32(255, 167, 135, 255), .52f, 7.5f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedVolumePath);
        if (profile != null)
        {
            GameObject volumeObject = new GameObject("Bathroom Premium Volume", typeof(Volume));
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

    private static FixtureSet BuildFixtures(Transform furniture, Transform gameplay,
        IReadOnlyDictionary<string, Material> materials, Scene scene)
    {
        Transform fixtures = CreateChild(furniture, "Premium Bathroom Fixtures");
        GameObject tub = CreateFixture("BathroomTub",
            PrefabFolder + "/BathroomTub.prefab", TubModel, fixtures, scene,
            new Vector3(2.28f, 0f, 2.62f), Quaternion.identity,
            new Vector3(2.45f, 1.36f, 1.303f),
            () => BuildTubFallback(fixtures, materials));
        GameObject vanity = CreateFixture("BathroomVanitySink",
            PrefabFolder + "/BathroomVanitySink.prefab", VanityModel, fixtures, scene,
            new Vector3(-2.55f, 0f, 2.78f), Quaternion.identity,
            new Vector3(1.96f, 1.69f, .855f),
            () => BuildVanityFallback(fixtures, materials));
        GameObject toilet = CreateFixture("BathroomToilet",
            PrefabFolder + "/BathroomToilet.prefab", ToiletModel, fixtures, scene,
            new Vector3(3.78f, 0f, -.2f), Quaternion.identity,
            new Vector3(.94f, 1.56f, 1.181f),
            () => BuildToiletFallback(fixtures, materials));
        GameObject shower = CreateFixture("BathroomShower",
            PrefabFolder + "/BathroomShower.prefab", ShowerModel, fixtures, scene,
            new Vector3(4.08f, 0f, 2.73f), Quaternion.identity,
            new Vector3(1.65f, 2.205f, 1.28f),
            () => BuildShowerFallback(fixtures, materials));

        ConfigureUsable(tub, gameplay, "bathroom-bath", BathroomUsableKind.Bath,
            "SPLASH", new Vector3(2.28f, .05f, 1.63f), Quaternion.identity);
        ConfigureUsable(vanity, gameplay, "bathroom-sink", BathroomUsableKind.Sink,
            "WASH", new Vector3(-2.55f, .05f, 1.85f), Quaternion.identity);
        return new FixtureSet(tub, vanity, toilet, shower);
    }

    private static GameObject CreateFixture(string name, string prefabPath, string modelPath,
        Transform parent, Scene scene, Vector3 position, Quaternion rotation,
        Vector3 targetBounds, Func<GameObject> fallback)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        bool usingImportedModel = false;
        if (source == null)
        {
            source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            usingImportedModel = source != null;
        }

        GameObject instance = source != null
            ? PrefabUtility.InstantiatePrefab(source, scene) as GameObject
            : fallback();
        if (instance == null)
            throw new InvalidOperationException("Could not build fixture '" + name + "'.");

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        // Blender's FBX root carries the required Z-up -> Y-up correction. A
        // model-prefab instance loses that correction when its scene transform
        // is authored, so apply it explicitly. A dedicated Unity prefab is
        // considered already authored and keeps the requested room rotation.
        instance.transform.localRotation = usingImportedModel
            ? rotation * Quaternion.Euler(-90f, 0f, 0f)
            : rotation;
        instance.transform.localScale = Vector3.one;
        FitUniformToBounds(instance, targetBounds);
        AlignRendererBottom(instance, 0f);
        AddFittedCollider(instance);
        return instance;
    }

    private static void ConfigureUsable(GameObject target, Transform gameplay, string id,
        BathroomUsableKind kind, string prompt, Vector3 position, Quaternion rotation)
    {
        Transform anchors = FindOrCreateChild(gameplay, "Bathroom Usable Anchors");
        GameObject anchorObject = new GameObject(id + "_Anchor");
        anchorObject.transform.SetParent(anchors, false);
        anchorObject.transform.position = position;
        anchorObject.transform.rotation = rotation;
        BathroomUsablePlaceholder usable = target.GetComponent<BathroomUsablePlaceholder>();
        if (usable == null)
            usable = target.AddComponent<BathroomUsablePlaceholder>();
        usable.EditorConfigure(id, kind, prompt, anchorObject.transform);
    }

    private static void BuildEmptyBathroomUsables(Transform gameplay)
    {
        GameObject bath = new GameObject("BathroomBathUsable");
        bath.transform.SetParent(gameplay, false);
        ConfigureUsable(bath, gameplay, "bathroom-bath", BathroomUsableKind.Bath,
            "SPLASH", new Vector3(1.2f, .05f, 1.15f), Quaternion.identity);

        GameObject sink = new GameObject("BathroomSinkUsable");
        sink.transform.SetParent(gameplay, false);
        ConfigureUsable(sink, gameplay, "bathroom-sink", BathroomUsableKind.Sink,
            "WASH", new Vector3(-2.5f, .05f, 1.35f), Quaternion.identity);

        GameObject grooming = new GameObject("BathroomGroomingUsable");
        grooming.transform.SetParent(gameplay, false);
        ConfigureUsable(grooming, gameplay, "bathroom-grooming",
            BathroomUsableKind.Grooming, "GROOM",
            new Vector3(-1.4f, .05f, -1.45f), Quaternion.identity);

        GameObject litter = new GameObject("BathroomLitterUsable");
        litter.transform.SetParent(gameplay, false);
        ConfigureUsable(litter, gameplay, "bathroom-litter",
            BathroomUsableKind.LitterBox, "TIDY",
            new Vector3(2.75f, .05f, -1.35f), Quaternion.identity);
    }

    private static CareSet BuildEmptyCareArea(Transform gameplay)
    {
        Transform care = CreateChild(gameplay, "Cat Care Anchors");

        GameObject foodBowl = new GameObject("FoodBowlAnchor");
        foodBowl.transform.SetParent(care, false);
        foodBowl.transform.position = new Vector3(-3.25f, 0f, -1.62f);
        Transform foodPoint = CreateWorldAnchor(foodBowl.transform, "InteractionPoint",
            new Vector3(-3.25f, .03f, -2.05f), Quaternion.identity);
        GameObject foodContent = new GameObject("FoodContent");
        foodContent.transform.SetParent(foodBowl.transform, false);

        GameObject waterBowl = new GameObject("WaterBowlAnchor");
        waterBowl.transform.SetParent(care, false);
        waterBowl.transform.position = new Vector3(-2.55f, 0f, -1.62f);
        Transform waterPoint = CreateWorldAnchor(waterBowl.transform, "InteractionPoint",
            new Vector3(-2.55f, .03f, -2.05f), Quaternion.identity);
        GameObject waterContent = new GameObject("WaterContent");
        waterContent.transform.SetParent(waterBowl.transform, false);

        GameObject rest = new GameObject("RestAnchor");
        rest.transform.SetParent(care, false);
        rest.transform.position = new Vector3(-3.15f, 0f, -2.6f);
        Transform bedPoint = CreateWorldAnchor(rest.transform, "BedInteractionPoint",
            new Vector3(-3.15f, .03f, -2.05f), Quaternion.identity);
        Transform sleepPoint = CreateWorldAnchor(rest.transform, "SleepPoint",
            new Vector3(-3.15f, .18f, -2.6f), Quaternion.identity);

        return new CareSet(
            foodBowl.transform,
            foodPoint,
            foodContent,
            waterBowl.transform,
            waterPoint,
            waterContent,
            bedPoint,
            sleepPoint);
    }

    private static void BuildSupportingDecor(Transform furniture, Transform gameplay,
        FixtureSet fixtures, IReadOnlyDictionary<string, Material> materials, Scene scene)
    {
        Transform decor = CreateChild(furniture, "Storage Towels Plants and Cat Props");
        BuildMirror(decor, materials);
        BuildTowelStorage(decor, materials);

        GameObject bathMat = InstantiateDecor(
            "BathMat", "Assets/LowPolyLivingRoomPack/Prefabs/Carpet_2.prefab",
            decor, scene, new Vector3(1.65f, .025f, .78f), Quaternion.identity);
        if (bathMat != null)
        {
            FitFootprint(bathMat, 2.35f, 1.38f);
            RemoveAllColliders(bathMat);
        }
        else
        {
            CreateBlock("BathMat", decor, new Vector3(1.65f, .035f, .78f),
                new Vector3(2.35f, .07f, 1.38f), materials["Coral"], false);
        }

        GameObject plantA = InstantiateDecor("MintPlant",
            "Assets/LowPolyLivingRoomPack/Prefabs/PottedPlant_Small_2.prefab",
            decor, scene, new Vector3(-4.35f, 0f, 2.55f), Quaternion.identity);
        if (plantA != null) FitHeight(plantA, 1.0f);
        GameObject plantB = InstantiateDecor("LilacPlant",
            "Assets/LowPolyLivingRoomPack/Prefabs/PottedPlant_Tall_2.prefab",
            decor, scene, new Vector3(4.35f, 0f, -2.62f), Quaternion.Euler(0f, -25f, 0f));
        if (plantB != null) FitHeight(plantB, 1.42f);

        GameObject stool = InstantiateDecor("BathStool",
            "Assets/LowPolyLivingRoomPack/Prefabs/Stool_2.prefab", decor, scene,
            new Vector3(.05f, 0f, 2.72f), Quaternion.Euler(0f, 14f, 0f));
        if (stool != null) FitHeight(stool, .68f);

        GameObject litter = BuildLitterBox(decor, materials);
        ConfigureUsable(litter, gameplay, "bathroom-litter", BathroomUsableKind.LitterBox,
            "TIDY", new Vector3(3.82f, .05f, -2.05f), Quaternion.identity);

        GameObject grooming = CreateBlock("GroomingBasket", decor,
            new Vector3(-4.18f, .35f, -.82f), new Vector3(.82f, .7f, .64f),
            materials["Lilac"], true);
        CreateBlock("GroomingBasketInset", grooming.transform,
            new Vector3(0f, .38f, 0f), new Vector3(.66f, .12f, .49f),
            materials["Cream"], false);
        ConfigureUsable(grooming, gameplay, "bathroom-grooming",
            BathroomUsableKind.Grooming, "GROOM",
            new Vector3(-3.55f, .05f, -.82f), Quaternion.Euler(0f, -90f, 0f));
    }

    private static void BuildMirror(Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject rim = CreateCylinder("VanityMirrorGoldRim", parent,
            new Vector3(-2.55f, 2.45f, 3.49f), Quaternion.Euler(90f, 0f, 0f),
            new Vector3(.91f, .075f, 1.14f), materials["Gold"], false);
        GameObject glass = CreateCylinder("VanityMirrorGlass", rim.transform,
            new Vector3(0f, -.078f, 0f), Quaternion.identity,
            new Vector3(.88f, .08f, 1.1f), materials["Mirror"], false);
        glass.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    private static void BuildTowelStorage(Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        Transform storage = CreateChild(parent, "Towel Storage");
        CreateBlock("StorageBack", storage, new Vector3(-4.45f, 1.08f, .72f),
            new Vector3(.82f, 2.12f, .45f), materials["Ink"], true);
        for (int i = 0; i < 3; i++)
        {
            float y = .42f + i * .63f;
            CreateBlock("Shelf_" + (i + 1), storage,
                new Vector3(-4.45f, y, .42f), new Vector3(.82f, .09f, .62f),
                materials["Gold"], false);
            Material towelMaterial = i == 0 ? materials["Coral"]
                : i == 1 ? materials["Mint"] : materials["Lilac"];
            CreateCylinder("RolledTowel_" + (i + 1), storage,
                new Vector3(-4.45f, y + .18f, .31f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(.26f, .33f, .26f), towelMaterial, false);
        }
    }

    private static GameObject BuildLitterBox(Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject root = new GameObject("CatLitterBox");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(3.82f, .12f, -2.68f);
        CreateBlock("Base", root.transform, Vector3.zero, new Vector3(1.3f, .24f, 1.02f),
            materials["Coral"], true);
        CreateBlock("Litter", root.transform, new Vector3(0f, .14f, 0f),
            new Vector3(1.04f, .08f, .77f), materials["Cream"], false);
        CreateBlock("BackLip", root.transform, new Vector3(0f, .29f, .45f),
            new Vector3(1.3f, .42f, .12f), materials["Lilac"], false);
        return root;
    }

    private static CareSet BuildCareArea(Transform gameplay,
        IReadOnlyDictionary<string, Material> materials, Scene scene)
    {
        Transform care = CreateChild(gameplay, "Cat Care Props");
        GameObject foodBowl = InstantiateDecor("FoodBowl",
            "Assets/Art/StoreProducts/Prefabs/CeramicBowl.prefab", care, scene,
            new Vector3(-3.7f, 0f, -1.62f), Quaternion.identity);
        GameObject waterBowl = InstantiateDecor("WaterBowl",
            "Assets/Art/StoreProducts/Prefabs/CeramicBowl.prefab", care, scene,
            new Vector3(-2.96f, 0f, -1.62f), Quaternion.identity);
        if (foodBowl == null)
            foodBowl = BuildBowlFallback("FoodBowl", care,
                new Vector3(-3.7f, .12f, -1.62f), materials["Coral"]);
        if (waterBowl == null)
            waterBowl = BuildBowlFallback("WaterBowl", care,
                new Vector3(-2.96f, .12f, -1.62f), materials["Aqua"]);
        FitFootprint(foodBowl, .58f, .58f);
        FitFootprint(waterBowl, .58f, .58f);
        AlignRendererBottom(foodBowl, 0f);
        AlignRendererBottom(waterBowl, 0f);

        Transform foodPoint = CreateWorldAnchor(foodBowl.transform, "InteractionPoint",
            new Vector3(-3.7f, .03f, -2.15f), Quaternion.identity);
        Transform waterPoint = CreateWorldAnchor(waterBowl.transform, "InteractionPoint",
            new Vector3(-2.96f, .03f, -2.15f), Quaternion.identity);
        GameObject foodContent = BuildBowlContent(foodBowl.transform, "FoodContent",
            new Color32(218, 145, 73, 255), materials);
        GameObject waterContent = BuildBowlContent(waterBowl.transform, "WaterContent",
            Water, materials);

        GameObject bed = InstantiateDecor("BathroomCloudBed",
            "Assets/Art/StoreProducts/Prefabs/CloudBed.prefab", care, scene,
            new Vector3(-3.65f, 0f, -2.72f), Quaternion.Euler(0f, 8f, 0f));
        if (bed == null)
        {
            bed = CreateBlock("BathroomCloudBed", care, new Vector3(-3.65f, .22f, -2.72f),
                new Vector3(1.75f, .44f, 1.18f), materials["Lilac"], true);
        }
        else
        {
            FitFootprint(bed, 1.72f, 1.18f);
            AlignRendererBottom(bed, 0f);
        }

        Bounds bedBounds = CalculateWorldBounds(bed);
        Transform sleepPoint = CreateWorldAnchor(bed.transform, "SleepPoint",
            new Vector3(bedBounds.center.x, bedBounds.max.y + .04f, bedBounds.center.z),
            Quaternion.identity);
        Transform bedPoint = CreateWorldAnchor(bed.transform, "BedInteractionPoint",
            new Vector3(bedBounds.center.x, .03f, bedBounds.min.z - .48f),
            Quaternion.identity);

        return new CareSet(foodBowl.transform, foodPoint, foodContent,
            waterBowl.transform, waterPoint, waterContent, bedPoint, sleepPoint);
    }

    private static GameObject BuildBowlFallback(
        string name, Transform parent, Vector3 position, Material material)
    {
        GameObject bowl = CreateCylinder(name, parent, position, Quaternion.identity,
            new Vector3(.34f, .12f, .34f), material, true);
        return bowl;
    }

    private static GameObject BuildBowlContent(Transform bowl, string name, Color color,
        IReadOnlyDictionary<string, Material> materials)
    {
        Material material = name.StartsWith("Water", StringComparison.Ordinal)
            ? materials["Water"] : materials["Food"];
        GameObject content = CreateCylinder(name, bowl, Vector3.zero, Quaternion.identity,
            new Vector3(.27f, .035f, .27f), material, false);
        Bounds bounds = CalculateWorldBounds(bowl.gameObject);
        content.transform.position = new Vector3(bounds.center.x, bounds.max.y + .015f,
            bounds.center.z);
        return content;
    }

    private static CatMovement CloneAndConfigureCat(Scene livingScene, Transform parent,
        Camera camera, CareSet care, Scene targetScene)
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
        CharacterController controller = catObject.GetComponent<CharacterController>();
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
            SetObject(serialized, "characterController", controller);
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
            SetObject(serialized, "characterController", controller);
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
        levelMarker.EditorSetId(HomeRoomService.BathroomId);
        HomeRoomSceneMarker roomMarker = markerObject.AddComponent<HomeRoomSceneMarker>();
        roomMarker.EditorConfigure(HomeRoomService.BathroomId, camera, cat);

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

    private static GameObject BuildTubFallback(Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject root = new GameObject("BathroomTub_Fallback");
        root.transform.SetParent(parent, false);
        CreateBlock("TubBody", root.transform, Vector3.zero,
            new Vector3(2.45f, .72f, 1.3f), materials["White"], true);
        CreateBlock("TubWater", root.transform, new Vector3(0f, .39f, 0f),
            new Vector3(2.06f, .08f, .93f), materials["Water"], false);
        CreateCylinder("TubEndA", root.transform, new Vector3(-1.12f, .15f, 0f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(.65f, .18f, .65f),
            materials["White"], false);
        CreateCylinder("TubEndB", root.transform, new Vector3(1.12f, .15f, 0f),
            Quaternion.Euler(0f, 0f, 90f), new Vector3(.65f, .18f, .65f),
            materials["White"], false);
        return root;
    }

    private static GameObject BuildVanityFallback(Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject root = new GameObject("BathroomVanitySink_Fallback");
        root.transform.SetParent(parent, false);
        CreateBlock("Cabinet", root.transform, new Vector3(0f, .58f, 0f),
            new Vector3(1.9f, 1.16f, .82f), materials["Coral"], true);
        CreateBlock("Counter", root.transform, new Vector3(0f, 1.2f, 0f),
            new Vector3(2.0f, .14f, .9f), materials["Cream"], false);
        CreateCylinder("Sink", root.transform, new Vector3(0f, 1.3f, -.03f),
            Quaternion.identity, new Vector3(.58f, .12f, .42f), materials["White"], false);
        return root;
    }

    private static GameObject BuildToiletFallback(Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject root = new GameObject("BathroomToilet_Fallback");
        root.transform.SetParent(parent, false);
        CreateBlock("Tank", root.transform, new Vector3(0f, .98f, .32f),
            new Vector3(.78f, 1.1f, .48f), materials["White"], true);
        CreateCylinder("Seat", root.transform, new Vector3(0f, .52f, -.23f),
            Quaternion.identity, new Vector3(.48f, .16f, .62f), materials["White"], true);
        return root;
    }

    private static GameObject BuildShowerFallback(Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject root = new GameObject("BathroomShower_Fallback");
        root.transform.SetParent(parent, false);
        CreateBlock("ShowerTray", root.transform, new Vector3(0f, .08f, 0f),
            new Vector3(1.62f, .16f, 1.25f), materials["White"], true);
        CreateBlock("GlassBack", root.transform, new Vector3(0f, 1.15f, .58f),
            new Vector3(1.62f, 2.2f, .07f), materials["Mirror"], false);
        CreateBlock("GlassSide", root.transform, new Vector3(.77f, 1.15f, 0f),
            new Vector3(.07f, 2.2f, 1.25f), materials["Mirror"], false);
        return root;
    }

    private static GameObject InstantiateDecor(string name, string path, Transform parent,
        Scene scene, Vector3 position, Quaternion rotation)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            return null;
        GameObject instance = PrefabUtility.InstantiatePrefab(asset, scene) as GameObject;
        if (instance == null)
            return null;
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
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

    private static GameObject CreateCylinder(string name, Transform parent, Vector3 position,
        Quaternion rotation, Vector3 scale, Material material, bool collider)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localRotation = rotation;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
            UnityEngine.Object.DestroyImmediate(cylinder.GetComponent<Collider>());
        return cylinder;
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

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        return child != null ? child : CreateChild(parent, name);
    }

    private static Transform CreateWorldAnchor(Transform parent, string name,
        Vector3 position, Quaternion rotation)
    {
        Transform anchor = CreateChild(parent, name);
        anchor.position = position;
        anchor.rotation = rotation;
        return anchor;
    }

    private static void FitUniformToBounds(GameObject target, Vector3 desired)
    {
        Bounds bounds = CalculateWorldBounds(target);
        if (bounds.size.sqrMagnitude < .0001f)
            return;
        float x = desired.x > 0f ? desired.x / bounds.size.x : float.PositiveInfinity;
        float y = desired.y > 0f ? desired.y / bounds.size.y : float.PositiveInfinity;
        float z = desired.z > 0f ? desired.z / bounds.size.z : float.PositiveInfinity;
        float scale = Mathf.Min(x, Mathf.Min(y, z));
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            return;
        target.transform.localScale *= scale;
    }

    private static void FitFootprint(GameObject target, float width, float depth)
    {
        Bounds bounds = CalculateWorldBounds(target);
        if (bounds.size.x <= .001f || bounds.size.z <= .001f)
            return;
        float scale = Mathf.Min(width / bounds.size.x, depth / bounds.size.z);
        target.transform.localScale *= scale;
    }

    private static void FitHeight(GameObject target, float height)
    {
        Bounds bounds = CalculateWorldBounds(target);
        if (bounds.size.y <= .001f)
            return;
        target.transform.localScale *= height / bounds.size.y;
        AlignRendererBottom(target, 0f);
    }

    private static void AlignRendererBottom(GameObject target, float floorY)
    {
        Bounds bounds = CalculateWorldBounds(target);
        if (bounds.size.sqrMagnitude <= .0001f)
            return;
        target.transform.position += Vector3.up * (floorY - bounds.min.y);
    }

    private static Bounds CalculateWorldBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(target.transform.position, Vector3.zero);
        Bounds result = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            result.Encapsulate(renderers[i].bounds);
        return result;
    }

    private static void AddFittedCollider(GameObject target)
    {
        if (target.GetComponent<Collider>() != null)
            return;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;
        Bounds world = CalculateWorldBounds(target);
        Vector3 center = target.transform.InverseTransformPoint(world.center);
        Vector3 scale = target.transform.lossyScale;
        Vector3 size = new Vector3(
            world.size.x / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
            world.size.y / Mathf.Max(.0001f, Mathf.Abs(scale.y)),
            world.size.z / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
        BoxCollider collider = target.AddComponent<BoxCollider>();
        collider.center = center;
        collider.size = size;
    }

    private static void RemoveAllColliders(GameObject target)
    {
        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
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

    private static void SetObject(SerializedObject serialized, string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void ConfigureFixtureImporter(string path)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
            return;
        bool changed = false;
        if (importer.importAnimation) { importer.importAnimation = false; changed = true; }
        if (importer.importCameras) { importer.importCameras = false; changed = true; }
        if (importer.importLights) { importer.importLights = false; changed = true; }
        if (importer.isReadable) { importer.isReadable = false; changed = true; }
        if (changed)
            importer.SaveAndReimport();
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
        EnsureFolder("Assets/Art/Bathroom");
        EnsureFolder(MaterialFolder);
        EnsureFolder(PrefabFolder);
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

    private readonly struct FixtureSet
    {
        public FixtureSet(GameObject tub, GameObject vanity, GameObject toilet,
            GameObject shower)
        {
            Tub = tub;
            Vanity = vanity;
            Toilet = toilet;
            Shower = shower;
        }
        public GameObject Tub { get; }
        public GameObject Vanity { get; }
        public GameObject Toilet { get; }
        public GameObject Shower { get; }
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
