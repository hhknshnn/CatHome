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
/// Authors Bedroom Level 01 as a bright empty shell. Its ten furnishings are
/// hidden store products, so ownership alone decides what appears at runtime.
/// </summary>
public static class BedroomLevelBuilder
{
    public const string ScenePath = HomeRoomService.BedroomScenePath;

    private const string LivingScenePath = HomeRoomService.LivingRoomScenePath;
    private const string MaterialFolder = "Assets/Art/Bedroom/Materials";
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
    private static readonly Color Sky = new Color32(155, 232, 255, 255);
    private const float LivingCameraFieldOfView = 47f;
    private static readonly Vector3 LivingCameraPosition = new Vector3(-1f, 3f, -5.5f);
    private static readonly Quaternion LivingCameraRotation =
        Quaternion.Euler(25f, 12.995f, 0f);
    private static readonly Vector3 LivingSpawnPointPosition = new Vector3(0f, 0f, -2f);
    private static readonly Color LivingAmbientSkyColor = new Color32(54, 58, 66, 255);
    private static readonly Color LivingAmbientEquatorColor = new Color32(29, 32, 34, 255);
    private static readonly Color LivingAmbientGroundColor = new Color32(12, 11, 9, 255);
    private static readonly Color LivingCameraBackground = new Color32(49, 77, 121, 0);

    // Back-wall composition for the canonical 8 x 6 shell. The doorway sits in
    // the free right-hand span, the window above the stove run and the paw
    // medallion above the sink cabinet, so no wall art hides behind a fixture.
    private const int RibbonTileCount = 18;
    private const float RibbonStepX = .42f;
    private const float RibbonStartX = -3.57f;
    private const float RibbonCenterY = 1.3f;
    private const float DoorwayCenterX = 1.36f;
    private const float WindowCenterX = -.75f;
    private const float WindowCenterY = 2.25f;
    private const float MedallionCenterX = -2.6f;
    private const float MedallionCenterY = 2.3f;

    [MenuItem("Tools/Cat Home/Rooms/Build Bedroom Level 01")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Bedroom Level 01",
                "Create or replace Bedroom_Level01 as an empty premium room?",
                "Build",
                "Cancel"))
        {
            return;
        }

        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Bedroom", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Rebuild Bedroom Level 01 (Silent)")]
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
        bool previousWasBedroom = previousActive.IsValid() &&
                                  string.Equals(
                                      previousActive.path,
                                      ScenePath,
                                      StringComparison.Ordinal);
        Scene livingScene = SceneManager.GetSceneByPath(LivingScenePath);
        bool openedLiving = !livingScene.IsValid() || !livingScene.isLoaded;
        if (openedLiving)
            livingScene = EditorSceneManager.OpenScene(LivingScenePath, OpenSceneMode.Additive);

        Scene existingBedroom = SceneManager.GetSceneByPath(ScenePath);
        if (existingBedroom.IsValid() && existingBedroom.isLoaded)
        {
            if (SceneManager.GetActiveScene() == existingBedroom)
                EditorSceneManager.SetActiveScene(livingScene);
            EditorSceneManager.CloseScene(existingBedroom, true);
        }

        Scene bedroom = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(bedroom);

        Transform environment = CreateSceneRoot(bedroom, "01 Environment");
        Transform furniture = CreateSceneRoot(bedroom, "02 Furniture");
        Transform character = CreateSceneRoot(bedroom, "03 Character");
        Transform gameplay = CreateSceneRoot(bedroom, "04 Gameplay");
        Transform presentation = CreateSceneRoot(bedroom, "05 Presentation");
        CreateSceneRoot(bedroom, "06 Local UI");
        Transform setup = CreateSceneRoot(bedroom, "07 Level Setup");

        BuildRoomShell(environment, materials);
        Camera camera = BuildPresentation(presentation);
        CareSet care = BuildEmptyCareArea(gameplay);
        CatMovement cat = CloneAndConfigureCat(
            livingScene, character, camera, care, bedroom);
        BuildGameTimeService(setup);
        BuildArchitecture(setup, camera, cat);
        StoreProductContentBuilder.BuildRoomSceneProducts(
            bedroom,
            HomeRoomService.BedroomId,
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
            Debug.LogWarning("Bedroom room lighting controller build failed: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(bedroom);
        if (!EditorSceneManager.SaveScene(bedroom, ScenePath))
            throw new InvalidOperationException("Bedroom_Level01 could not be saved.");

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(bedroom, true);

        if (openedLiving && !previousWasBedroom)
            EditorSceneManager.CloseScene(livingScene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);
        else if (livingScene.IsValid() && livingScene.isLoaded)
            EditorSceneManager.SetActiveScene(livingScene);

        return "Bedroom Level 01 built empty with a ten-piece placeable ROOM collection.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Bedroom_Cream", Cream, .03f, .54f),
            ["Aqua"] = EnsureMaterial("Bedroom_Aqua", Aqua, .02f, .62f),
            ["Mint"] = EnsureMaterial("Bedroom_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Bedroom_Coral", Coral, .02f, .58f),
            ["Peach"] = EnsureMaterial("Bedroom_Peach", Peach, .02f, .56f),
            ["Lilac"] = EnsureMaterial("Bedroom_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Bedroom_Lemon", Lemon, .03f, .6f),
            ["Ink"] = EnsureMaterial("Bedroom_Ink", Ink, .05f, .36f),
            ["Sky"] = EnsureMaterial("Bedroom_Sky", Sky, .08f, .74f),
            ["Gold"] = EnsureMaterial(
                "Bedroom_Gold", new Color32(255, 191, 57, 255), .58f, .75f),
            ["Wood"] = EnsureMaterial(
                "Bedroom_Wood", new Color32(242, 196, 148, 255), .04f, .42f),
            ["WoodWarm"] = EnsureMaterial(
                "Bedroom_WoodWarm", new Color32(255, 218, 176, 255), .03f, .46f),
            ["WoodDeep"] = EnsureMaterial(
                "Bedroom_WoodDeep", new Color32(214, 154, 108, 255), .05f, .36f)
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
            materials["WoodWarm"], true);
        floor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Bedroom uses warm wood planks, not bathroom/kitchen checker tiles.
        Transform planks = CreateChild(parent, "Dreamy Wood Floor");
        const int plankCount = 22;
        const float plankGap = .012f;
        float plankStep = HomeRoomShellMetrics.FloorWidth / plankCount;
        float plankWidth = Mathf.Max(.08f, plankStep - plankGap);
        float plankLength = HomeRoomShellMetrics.FloorDepth - .04f;
        for (int i = 0; i < plankCount; i++)
        {
            Material material = i % 3 == 0
                ? materials["WoodWarm"]
                : i % 3 == 1 ? materials["Wood"] : materials["WoodDeep"];
            float x = -(HomeRoomShellMetrics.FloorWidth * .5f) + plankStep * (i + .5f);
            GameObject plank = CreateBlock(
                $"Plank_{i + 1:00}",
                planks,
                new Vector3(x, HomeRoomShellMetrics.FloorTileCenterY * .45f, 0f),
                new Vector3(
                    plankWidth,
                    HomeRoomShellMetrics.FloorTileThickness * .45f,
                    plankLength),
                material,
                false);
            plank.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        Transform walls = CreateChild(parent, "Dreamy Bedroom Walls");
        CreateBlock("BackWall_Cream", walls,
            HomeRoomShellMetrics.BackWallPosition, HomeRoomShellMetrics.BackWallScale,
            materials["Cream"], true);
        CreateBlock("BackWall_LilacWainscot", walls,
            HomeRoomShellMetrics.BackWainscotPosition,
            HomeRoomShellMetrics.BackWainscotScale, materials["Lilac"], false);
        CreateBlock("LeftWall_Cream", walls,
            HomeRoomShellMetrics.LeftWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Cream"], true);
        CreateBlock("LeftWall_PeachWainscot", walls,
            HomeRoomShellMetrics.LeftWainscotPosition,
            HomeRoomShellMetrics.SideWainscotScale, materials["Peach"], false);
        CreateBlock("RightWall_Cream", walls,
            HomeRoomShellMetrics.RightWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Cream"], true);
        CreateBlock("RightWall_MintWainscot", walls,
            HomeRoomShellMetrics.RightWainscotPosition,
            HomeRoomShellMetrics.SideWainscotScale, materials["Coral"], false);

        CreateBlock("BackBaseboard", walls,
            HomeRoomShellMetrics.BackBaseboardPosition,
            HomeRoomShellMetrics.BackBaseboardScale, materials["Gold"], false);
        CreateBlock("LeftBaseboard", walls,
            HomeRoomShellMetrics.LeftBaseboardPosition,
            HomeRoomShellMetrics.SideBaseboardScale, materials["Gold"], false);
        CreateBlock("RightBaseboard", walls,
            HomeRoomShellMetrics.RightBaseboardPosition,
            HomeRoomShellMetrics.SideBaseboardScale, materials["Gold"], false);

        Transform ribbon = CreateChild(walls, "Starlight Wall Ribbon");
        for (int i = 0; i < RibbonTileCount; i++)
        {
            Material material = i % 4 == 0 ? materials["Lilac"]
                : i % 4 == 1 ? materials["Lemon"]
                : i % 4 == 2 ? materials["Aqua"] : materials["Mint"];
            CreateBlock("RibbonTile_" + (i + 1).ToString("00"), ribbon,
                new Vector3(
                    RibbonStartX + i * RibbonStepX,
                    RibbonCenterY,
                    HomeRoomShellMetrics.BackWallDecorZ(.15f)),
                new Vector3(.38f, .3f, .08f), material, false);
        }

        BuildDoorway(walls, materials);
        BuildSunriseWindow(walls, materials);
        BuildPawWallMedallion(walls, materials);
    }

    private static void BuildDoorway(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform door = CreateChild(parent, "Room Portal Doorway");
        const float x = DoorwayCenterX;
        CreateBlock("DoorPanel", door,
            new Vector3(x, .95f, HomeRoomShellMetrics.BackWallDecorZ(.2f)),
            new Vector3(1.45f, 1.9f, .13f), materials["Mint"], false);
        CreateBlock("DoorInset", door,
            new Vector3(x, .99f, HomeRoomShellMetrics.BackWallDecorZ(.3f)),
            new Vector3(1.15f, 1.64f, .08f), materials["Cream"], false);
        CreateBlock("DoorTop", door,
            new Vector3(x, 1.98f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(1.72f, .24f, .24f), materials["Aqua"], false);
        CreateBlock("DoorLeft", door,
            new Vector3(x - .77f, 1.03f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(.24f, 2.06f, .24f), materials["Aqua"], false);
        CreateBlock("DoorRight", door,
            new Vector3(x + .77f, 1.03f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(.24f, 2.06f, .24f), materials["Aqua"], false);
        CreateSphere("DoorKnob", door,
            new Vector3(x - .48f, .89f, HomeRoomShellMetrics.BackWallDecorZ(.43f)),
            Vector3.one * .13f, materials["Gold"], false);
    }

    private static void BuildSunriseWindow(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform window = CreateChild(parent, "Bedroom Moon Window");
        const float x = WindowCenterX;
        const float y = WindowCenterY;
        CreateBlock("WindowFrame", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.2f)),
            new Vector3(2f, 1.35f, .15f), materials["Lilac"], false);
        CreateBlock("NightGlass", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.3f)),
            new Vector3(1.68f, 1.01f, .08f), materials["Sky"], false);
        CreateBlock("WindowVertical", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.38f)),
            new Vector3(.11f, 1.03f, .08f), materials["Cream"], false);
        CreateBlock("WindowHorizontal", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.39f)),
            new Vector3(1.7f, .11f, .08f), materials["Cream"], false);
        CreateSphere("Moon", window,
            new Vector3(x + .46f, y + .29f, HomeRoomShellMetrics.BackWallDecorZ(.45f)),
            Vector3.one * .28f, materials["Lemon"], false);
        CreateSphere("MoonCheek", window,
            new Vector3(x + .56f, y + .26f, HomeRoomShellMetrics.BackWallDecorZ(.5f)),
            Vector3.one * .14f, materials["Cream"], false);
    }

    private static void BuildPawWallMedallion(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform paw = CreateChild(parent, "Bedroom Paw Medallion");
        const float x = MedallionCenterX;
        const float y = MedallionCenterY;
        float z = HomeRoomShellMetrics.BackWallDecorZ(.23f);
        CreateSphere("PawPad", paw, new Vector3(x, y, z),
            new Vector3(.28f, .23f, .08f), materials["Lilac"], false);
        Vector3[] toes =
        {
            new Vector3(x - .24f, y + .26f, z), new Vector3(x - .07f, y + .38f, z),
            new Vector3(x + .13f, y + .38f, z), new Vector3(x + .3f, y + .25f, z)
        };
        for (int i = 0; i < toes.Length; i++)
        {
            CreateSphere("PawToe_" + (i + 1), paw, toes[i],
                new Vector3(.1f, .11f, .07f), materials["Coral"], false);
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

        GameObject keyObject = new GameObject("Bedroom Key Light", typeof(Light));
        keyObject.transform.SetParent(parent, false);
        keyObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light key = keyObject.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color32(255, 246, 218, 255);
        key.intensity = 1.02f;
        key.shadows = LightShadows.Soft;

        BuildFillLight(parent, "Aqua Fill", new Vector3(-3.4f, 2.9f, -1.5f),
            new Color32(116, 244, 233, 255), .9f, 8.5f);
        BuildFillLight(parent, "Peach Fill", new Vector3(3.5f, 2.25f, .4f),
            new Color32(255, 167, 135, 255), .6f, 7.5f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedVolumePath);
        if (profile != null)
        {
            GameObject volumeObject = new GameObject("Bedroom Premium Volume", typeof(Volume));
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
        levelMarker.EditorSetId(HomeRoomService.BedroomId);
        HomeRoomSceneMarker roomMarker = markerObject.AddComponent<HomeRoomSceneMarker>();
        roomMarker.EditorConfigure(HomeRoomService.BedroomId, camera, cat);

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
        EnsureFolder("Assets/Art/Bedroom");
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
