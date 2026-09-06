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
/// Authors Kitchen Level 01 as a bright empty shell. Its ten furnishings are
/// hidden store products, so ownership alone decides what appears at runtime.
/// </summary>
public static class KitchenLevelBuilder
{
    public const string ScenePath = HomeRoomService.KitchenScenePath;

    private const string LivingScenePath = HomeRoomService.LivingRoomScenePath;
    private const string MaterialFolder = "Assets/Art/Kitchen/Materials";
    private const string SharedVolumePath =
        "Assets/Art/PremiumWorld/CatHomeRoom_PremiumVolume.asset";

    private static readonly Color Cream = new Color32(255, 250, 236, 255);
    private static readonly Color Pearl = new Color32(255, 253, 246, 255);
    private static readonly Color Aqua = new Color32(145, 231, 222, 255);
    private static readonly Color Mint = new Color32(184, 242, 211, 255);
    private static readonly Color Coral = new Color32(255, 181, 176, 255);
    private static readonly Color Peach = new Color32(255, 214, 184, 255);
    private static readonly Color Lilac = new Color32(218, 195, 243, 255);
    private static readonly Color Lemon = new Color32(255, 232, 139, 255);
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

    [MenuItem("Tools/Cat Home/Rooms/Build Kitchen Level 01")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Kitchen Level 01",
                "Create or replace Kitchen_Level01 as an empty premium room?",
                "Build",
                "Cancel"))
        {
            return;
        }

        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Kitchen", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Rebuild Kitchen Level 01 (Silent)")]
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
        bool previousWasKitchen = previousActive.IsValid() &&
                                  string.Equals(
                                      previousActive.path,
                                      ScenePath,
                                      StringComparison.Ordinal);
        Scene livingScene = SceneManager.GetSceneByPath(LivingScenePath);
        bool openedLiving = !livingScene.IsValid() || !livingScene.isLoaded;
        if (openedLiving)
            livingScene = EditorSceneManager.OpenScene(LivingScenePath, OpenSceneMode.Additive);

        Scene existingKitchen = SceneManager.GetSceneByPath(ScenePath);
        if (existingKitchen.IsValid() && existingKitchen.isLoaded)
        {
            if (SceneManager.GetActiveScene() == existingKitchen)
                EditorSceneManager.SetActiveScene(livingScene);
            EditorSceneManager.CloseScene(existingKitchen, true);
        }

        Scene kitchen = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(kitchen);

        Transform environment = CreateSceneRoot(kitchen, "01 Environment");
        Transform furniture = CreateSceneRoot(kitchen, "02 Furniture");
        Transform character = CreateSceneRoot(kitchen, "03 Character");
        Transform gameplay = CreateSceneRoot(kitchen, "04 Gameplay");
        Transform presentation = CreateSceneRoot(kitchen, "05 Presentation");
        CreateSceneRoot(kitchen, "06 Local UI");
        Transform setup = CreateSceneRoot(kitchen, "07 Level Setup");

        BuildRoomShell(environment, materials);
        HomeRoomShellVisualPolishBuilder.Apply(kitchen, HomeRoomService.KitchenId, environment);
        Camera camera = BuildPresentation(presentation);
        CareSet care = BuildEmptyCareArea(gameplay);
        CatMovement cat = CloneAndConfigureCat(
            livingScene, character, camera, care, kitchen);
        BuildGameTimeService(setup);
        BuildArchitecture(setup, camera, cat);
        StoreProductContentBuilder.BuildRoomSceneProducts(
            kitchen,
            HomeRoomService.KitchenId,
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
            Debug.LogWarning("Kitchen room lighting controller build failed: " + ex.Message);
        }

        EditorSceneManager.MarkSceneDirty(kitchen);
        if (!EditorSceneManager.SaveScene(kitchen, ScenePath))
            throw new InvalidOperationException("Kitchen_Level01 could not be saved.");

        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(kitchen, true);

        if (openedLiving && !previousWasKitchen)
            EditorSceneManager.CloseScene(livingScene, true);
        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);
        else if (livingScene.IsValid() && livingScene.isLoaded)
            EditorSceneManager.SetActiveScene(livingScene);

        return "Kitchen Level 01 built empty with a ten-piece placeable ROOM collection.";
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        return new Dictionary<string, Material>(StringComparer.Ordinal)
        {
            ["Cream"] = EnsureMaterial("Kitchen_Cream", Cream, .03f, .54f),
            ["Pearl"] = EnsureMaterial("Kitchen_Pearl", Pearl, .04f, .72f),
            ["Aqua"] = EnsureMaterial("Kitchen_Aqua", Aqua, .02f, .62f),
            ["Mint"] = EnsureMaterial("Kitchen_Mint", Mint, .02f, .58f),
            ["Coral"] = EnsureMaterial("Kitchen_Coral", Coral, .02f, .58f),
            ["Peach"] = EnsureMaterial("Kitchen_Peach", Peach, .02f, .56f),
            ["Lilac"] = EnsureMaterial("Kitchen_Lilac", Lilac, .02f, .57f),
            ["Lemon"] = EnsureMaterial("Kitchen_Lemon", Lemon, .03f, .6f),
            ["Ink"] = EnsureMaterial("Kitchen_Ink", Ink, .05f, .36f),
            ["Sky"] = EnsureMaterial("Kitchen_Sky", Sky, .08f, .74f),
            ["Gold"] = EnsureMaterial(
                "Kitchen_Gold", new Color32(244, 190, 73, 255), .52f, .76f),
            ["AquaAccent"] = EnsureMaterial(
                "Kitchen_AquaAccent", new Color32(49, 205, 195, 255), .04f, .67f),
            ["CoralAccent"] = EnsureMaterial(
                "Kitchen_CoralAccent", new Color32(255, 121, 143, 255), .03f, .64f)
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
            materials["Cream"], true);
        floor.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

        Transform tiles = CreateChild(parent, "Sunshine Checker Floor");
        for (int x = 0; x < HomeRoomShellMetrics.FloorTileColumns; x++)
        {
            for (int z = 0; z < HomeRoomShellMetrics.FloorTileRows; z++)
            {
                Material material = ((x + z) & 1) == 0
                    ? materials["Cream"] : materials["Lemon"];
                GameObject tile = CreateBlock(
                    $"Tile_{x + 1:00}_{z + 1:00}",
                    tiles,
                    HomeRoomShellMetrics.FloorTilePosition(x, z),
                    new Vector3(
                        HomeRoomShellMetrics.FloorTileSize - .04f,
                        HomeRoomShellMetrics.FloorTileThickness,
                        HomeRoomShellMetrics.FloorTileSize - .04f),
                    material,
                    false);
                tile.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        Transform walls = CreateChild(parent, "Candy Kitchen Walls");
        CreateBlock("BackWall_Cream", walls,
            HomeRoomShellMetrics.BackWallPosition, HomeRoomShellMetrics.BackWallScale,
            materials["Cream"], true);
        CreateBlock("BackWall_AquaBacksplash", walls,
            HomeRoomShellMetrics.BackWainscotPosition,
            HomeRoomShellMetrics.BackWainscotScale, materials["Aqua"], false);
        CreateBlock("LeftWall_Cream", walls,
            HomeRoomShellMetrics.LeftWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Cream"], true);
        CreateBlock("LeftWall_MintWainscot", walls,
            HomeRoomShellMetrics.LeftWainscotPosition,
            HomeRoomShellMetrics.SideWainscotScale, materials["Mint"], false);
        CreateBlock("RightWall_Cream", walls,
            HomeRoomShellMetrics.RightWallPosition, HomeRoomShellMetrics.SideWallScale,
            materials["Cream"], true);
        CreateBlock("RightWall_CoralWainscot", walls,
            HomeRoomShellMetrics.RightWainscotPosition,
            HomeRoomShellMetrics.SideWainscotScale, materials["Peach"], false);

        CreateBlock("BackBaseboard", walls,
            HomeRoomShellMetrics.BackBaseboardPosition,
            HomeRoomShellMetrics.BackBaseboardScale, materials["Pearl"], false);
        CreateBlock("LeftBaseboard", walls,
            HomeRoomShellMetrics.LeftBaseboardPosition,
            HomeRoomShellMetrics.SideBaseboardScale, materials["Pearl"], false);
        CreateBlock("RightBaseboard", walls,
            HomeRoomShellMetrics.RightBaseboardPosition,
            HomeRoomShellMetrics.SideBaseboardScale, materials["Pearl"], false);

        Transform ribbon = CreateChild(walls, "Candy Backsplash Ribbon");
        CreateBlock("PearlPictureRail", ribbon,
            new Vector3(0f, RibbonCenterY,
                HomeRoomShellMetrics.BackWallDecorZ(.13f)),
            new Vector3(7.66f, .16f, .09f), materials["Pearl"], false);
        CreateBlock("GoldRailInset", ribbon,
            new Vector3(0f, RibbonCenterY - .075f,
                HomeRoomShellMetrics.BackWallDecorZ(.19f)),
            new Vector3(7.48f, .035f, .035f), materials["Gold"], false);
        for (int i = 0; i < RibbonTileCount; i++)
        {
            Material material = i % 4 == 0 ? materials["CoralAccent"]
                : i % 4 == 1 ? materials["Gold"]
                : i % 4 == 2 ? materials["Lilac"] : materials["AquaAccent"];
            CreateBlock("RibbonTile_" + (i + 1).ToString("00"), ribbon,
                new Vector3(
                    RibbonStartX + i * RibbonStepX,
                    RibbonCenterY,
                    HomeRoomShellMetrics.BackWallDecorZ(.2f)),
                new Vector3(.25f, .1f, .035f), material, false);
        }

        BuildFloorInlay(parent, materials);
        BuildCrownMoulding(walls, materials);
        BuildDoorway(walls, materials);
        BuildSunriseWindow(walls, materials);
        BuildPawWallMedallion(walls, materials);
    }

    private static void BuildFloorInlay(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform inlay = CreateChild(parent, "Sunshine Floor Inlay");
        const float y = .036f;
        CreateBlock("BackGoldInlay", inlay, new Vector3(0f, y, 2.68f),
            new Vector3(7.48f, .018f, .045f), materials["Gold"], false);
        CreateBlock("LeftGoldInlay", inlay, new Vector3(-3.68f, y, 0f),
            new Vector3(.045f, .018f, 5.42f), materials["Gold"], false);
        CreateBlock("RightGoldInlay", inlay, new Vector3(3.68f, y, 0f),
            new Vector3(.045f, .018f, 5.42f), materials["Gold"], false);
    }

    private static void BuildCrownMoulding(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform crown = CreateChild(parent, "Kitchen Crown Moulding");
        CreateBlock("BackPearlCrown", crown, new Vector3(0f, 2.82f, 2.68f),
            new Vector3(7.72f, .16f, .18f), materials["Pearl"], false);
        CreateBlock("BackGoldInset", crown, new Vector3(0f, 2.73f, 2.57f),
            new Vector3(7.46f, .035f, .04f), materials["Gold"], false);
        CreateBlock("LeftPearlCrown", crown, new Vector3(-3.68f, 2.82f, 0f),
            new Vector3(.18f, .16f, 5.7f), materials["Pearl"], false);
        CreateBlock("RightPearlCrown", crown, new Vector3(3.68f, 2.82f, 0f),
            new Vector3(.18f, .16f, 5.7f), materials["Pearl"], false);
        CreateBlock("LeftGoldInset", crown, new Vector3(-3.57f, 2.73f, 0f),
            new Vector3(.04f, .035f, 5.45f), materials["Gold"], false);
        CreateBlock("RightGoldInset", crown, new Vector3(3.57f, 2.73f, 0f),
            new Vector3(.04f, .035f, 5.45f), materials["Gold"], false);
    }

    private static void BuildDoorway(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform door = CreateChild(parent, "Room Portal Doorway");
        const float x = DoorwayCenterX;
        CreateBlock("DoorPanel", door,
            new Vector3(x, .95f, HomeRoomShellMetrics.BackWallDecorZ(.2f)),
            new Vector3(1.48f, 1.92f, .13f), materials["Pearl"], false);
        CreateBlock("DoorInset", door,
            new Vector3(x, .99f, HomeRoomShellMetrics.BackWallDecorZ(.3f)),
            new Vector3(1.15f, 1.64f, .08f), materials["Peach"], false);
        CreateBlock("DoorInnerPearl", door,
            new Vector3(x, 1f, HomeRoomShellMetrics.BackWallDecorZ(.37f)),
            new Vector3(.93f, 1.42f, .035f), materials["Pearl"], false);
        CreateBlock("DoorTop", door,
            new Vector3(x, 1.98f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(1.72f, .24f, .24f), materials["AquaAccent"], false);
        CreateBlock("DoorLeft", door,
            new Vector3(x - .77f, 1.03f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(.24f, 2.06f, .24f), materials["AquaAccent"], false);
        CreateBlock("DoorRight", door,
            new Vector3(x + .77f, 1.03f, HomeRoomShellMetrics.BackWallDecorZ(.27f)),
            new Vector3(.24f, 2.06f, .24f), materials["AquaAccent"], false);
        CreateBlock("DoorGoldHeader", door,
            new Vector3(x, 2.06f, HomeRoomShellMetrics.BackWallDecorZ(.41f)),
            new Vector3(.58f, .055f, .035f), materials["Gold"], false);
        CreateSphere("DoorKnob", door,
            new Vector3(x - .48f, .89f, HomeRoomShellMetrics.BackWallDecorZ(.43f)),
            Vector3.one * .13f, materials["Gold"], false);
    }

    private static void BuildSunriseWindow(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform window = CreateChild(parent, "Kitchen Sunrise Window");
        const float x = WindowCenterX;
        const float y = WindowCenterY;
        CreateBlock("WindowFrame", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.2f)),
            new Vector3(2.18f, 1.52f, .16f), materials["Pearl"], false);
        CreateBlock("WindowGoldInset", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.28f)),
            new Vector3(2.02f, 1.36f, .08f), materials["Gold"], false);
        CreateBlock("SkyGlass", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.36f)),
            new Vector3(1.82f, 1.16f, .06f), materials["Sky"], false);
        CreateBlock("WindowVertical", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.43f)),
            new Vector3(.09f, 1.18f, .055f), materials["Pearl"], false);
        CreateBlock("WindowHorizontal", window,
            new Vector3(x, y, HomeRoomShellMetrics.BackWallDecorZ(.44f)),
            new Vector3(1.84f, .09f, .055f), materials["Pearl"], false);
        CreateSphere("Sun", window,
            new Vector3(x + .48f, y + .3f, HomeRoomShellMetrics.BackWallDecorZ(.49f)),
            Vector3.one * .19f, materials["Lemon"], false);
        CreateSphere("CloudLeft", window,
            new Vector3(x - .48f, y + .22f, HomeRoomShellMetrics.BackWallDecorZ(.5f)),
            new Vector3(.28f, .12f, .05f), materials["Pearl"], false);
        CreateSphere("CloudRight", window,
            new Vector3(x - .22f, y + .2f, HomeRoomShellMetrics.BackWallDecorZ(.5f)),
            new Vector3(.22f, .1f, .05f), materials["Pearl"], false);
        CreateSphere("MintHill", window,
            new Vector3(x - .42f, y - .48f, HomeRoomShellMetrics.BackWallDecorZ(.49f)),
            new Vector3(.72f, .23f, .05f), materials["Mint"], false);
        CreateSphere("PeachHill", window,
            new Vector3(x + .42f, y - .5f, HomeRoomShellMetrics.BackWallDecorZ(.5f)),
            new Vector3(.72f, .2f, .05f), materials["Peach"], false);
        CreateBlock("PearlSill", window,
            new Vector3(x, y - .8f, HomeRoomShellMetrics.BackWallDecorZ(.34f)),
            new Vector3(2.35f, .13f, .28f), materials["Pearl"], false);
        CreateBlock("GoldSillInset", window,
            new Vector3(x, y - .72f, HomeRoomShellMetrics.BackWallDecorZ(.48f)),
            new Vector3(2.02f, .035f, .035f), materials["Gold"], false);
    }

    private static void BuildPawWallMedallion(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Transform paw = CreateChild(parent, "Kitchen Paw Medallion");
        const float x = MedallionCenterX;
        const float y = MedallionCenterY;
        float z = HomeRoomShellMetrics.BackWallDecorZ(.45f);
        CreateCylinder("MedallionGoldRim", paw, new Vector3(x, y, z + .14f),
            Quaternion.Euler(90f, 0f, 0f), new Vector3(.95f, .035f, .95f),
            materials["Gold"], false);
        CreateCylinder("MedallionPearlFace", paw, new Vector3(x, y, z + .08f),
            Quaternion.Euler(90f, 0f, 0f), new Vector3(.79f, .03f, .79f),
            materials["Pearl"], false);
        CreateSphere("PawPad", paw, new Vector3(x, y, z),
            new Vector3(.28f, .23f, .06f), materials["Lilac"], false);
        Vector3[] toes =
        {
            new Vector3(x - .24f, y + .26f, z), new Vector3(x - .07f, y + .38f, z),
            new Vector3(x + .13f, y + .38f, z), new Vector3(x + .3f, y + .25f, z)
        };
        for (int i = 0; i < toes.Length; i++)
        {
            CreateSphere("PawToe_" + (i + 1), paw, toes[i],
                new Vector3(.1f, .11f, .055f), materials["CoralAccent"], false);
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

        GameObject keyObject = new GameObject("Kitchen Key Light", typeof(Light));
        keyObject.transform.SetParent(parent, false);
        keyObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        Light key = keyObject.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color32(255, 246, 218, 255);
        key.intensity = .9f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = .52f;
        key.shadowStrength = .58f;

        BuildFillLight(parent, "Aqua Fill", new Vector3(-3.4f, 2.9f, -1.5f),
            new Color32(116, 244, 233, 255), .72f, 8.5f);
        BuildFillLight(parent, "Peach Fill", new Vector3(3.7f, 3.1f, -.8f),
            new Color32(255, 179, 151, 255), .4f, 7f);

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SharedVolumePath);
        if (profile != null)
        {
            GameObject volumeObject = new GameObject("Kitchen Premium Volume", typeof(Volume));
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
        levelMarker.EditorSetId(HomeRoomService.KitchenId);
        HomeRoomSceneMarker roomMarker = markerObject.AddComponent<HomeRoomSceneMarker>();
        roomMarker.EditorConfigure(HomeRoomService.KitchenId, camera, cat);

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
        EnsureFolder("Assets/Art/Kitchen");
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
