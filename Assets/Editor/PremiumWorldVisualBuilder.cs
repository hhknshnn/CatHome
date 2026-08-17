using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Gives the home scene the same bright, polished finish as the candy UI.
/// The recipe is deliberately measured: richer colour and soft highlight bloom
/// without washing out the furniture or changing gameplay lighting contracts.
/// </summary>
public static class PremiumWorldVisualBuilder
{
    private const string ScenePath = "Assets/Scenes/Levels/LivingRoom_Level01.unity";
    private const string ProfileFolder = "Assets/Art/PremiumWorld";
    private const string ProfilePath = ProfileFolder + "/CatHomeRoom_PremiumVolume.asset";
    private const string MaterialFolder = ProfileFolder + "/Materials";
    private const string RootName = "PremiumWorldPresentation";
    private const string ArchitectureName = "CandyRoomArchitecture";

    [MenuItem("Tools/Cat Home/Visuals/Apply Premium Home Finish")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = FindInScene<Camera>(scene, "Main Camera");
        if (camera == null)
        {
            Debug.LogWarning("Premium home finish skipped: LivingRoom_Level01 has no Main Camera.");
            return;
        }

        UniversalAdditionalCameraData cameraData =
            camera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
            cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.dithering = true;
        camera.allowHDR = true;

        VolumeProfile profile = GetOrCreateProfile();
        Transform presentation = CatHomeAuthoringWorkspace.FindNamedInScene(
            scene, CatHomeAuthoringWorkspace.PresentationGroupName)?.transform;
        GameObject root = CatHomeAuthoringWorkspace.FindNamedInScene(scene, RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        if (presentation != null && root.transform.parent != presentation)
            root.transform.SetParent(presentation, false);

        Volume volume = GetOrAdd<Volume>(root);
        volume.isGlobal = true;
        volume.priority = 12f;
        volume.weight = 1f;
        volume.sharedProfile = profile;

        RebuildFillLights(root.transform);
        RebuildArchitecturalFinish(scene, root.transform);
        RecolorLivingRoomFurniture(scene);
        EditorUtility.SetDirty(cameraData);
        EditorUtility.SetDirty(volume);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log(
            "Premium home finish applied: candy colour grade, polished room trim, " +
            "ambient wall sparkles, SMAA, bloom and soft room fill lights.");
    }

    public static void BuildSilently() => Build();

    private static VolumeProfile GetOrCreateProfile()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder(ProfileFolder);
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        Bloom bloom = GetOrAddOverride<Bloom>(profile);
        bloom.active = true;
        bloom.intensity.Override(0.24f);
        bloom.threshold.Override(0.88f);
        bloom.scatter.Override(0.68f);
        bloom.highQualityFiltering.Override(true);
        bloom.tint.Override(new Color(1f, 0.93f, 0.78f, 1f));

        ColorAdjustments colour = GetOrAddOverride<ColorAdjustments>(profile);
        colour.active = true;
        colour.postExposure.Override(0f);
        colour.contrast.Override(7f);
        colour.saturation.Override(12f);
        colour.colorFilter.Override(new Color(1f, 0.975f, 0.95f, 1f));

        WhiteBalance whiteBalance = GetOrAddOverride<WhiteBalance>(profile);
        whiteBalance.active = true;
        whiteBalance.temperature.Override(3f);
        whiteBalance.tint.Override(2f);

        Tonemapping tonemapping = GetOrAddOverride<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        Vignette vignette = GetOrAddOverride<Vignette>(profile);
        vignette.active = true;
        vignette.color.Override(new Color32(83, 43, 108, 255));
        vignette.intensity.Override(0.075f);
        vignette.smoothness.Override(0.34f);
        vignette.rounded.Override(true);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T GetOrAddOverride<T>(VolumeProfile profile)
        where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
            if (!AssetDatabase.Contains(component))
                AssetDatabase.AddObjectToAsset(component, profile);
        }
        EditorUtility.SetDirty(component);
        return component;
    }

    private static void RebuildFillLights(Transform root)
    {
        RemoveChild(root, "PremiumWindowBounce");
        RemoveChild(root, "PremiumPeachFill");

        CreateFillLight(
            root,
            "PremiumWindowBounce",
            new Vector3(3.8f, 3.2f, 1.8f),
            new Color32(112, 225, 255, 255),
            0.9f,
            8.5f);
        CreateFillLight(
            root,
            "PremiumPeachFill",
            new Vector3(-3.6f, 2.4f, -0.8f),
            new Color32(255, 162, 126, 255),
            0.6f,
            7.5f);
    }

    private static void RebuildArchitecturalFinish(Scene scene, Transform root)
    {
        RemoveChild(root, ArchitectureName);

        Material wallSky = GetOrCreateLitMaterial(
            "PremiumWallSky", new Color32(214, 246, 252, 255), 0.18f);
        Material wallMint = GetOrCreateLitMaterial(
            "PremiumWallMint", new Color32(206, 248, 226, 255), 0.18f);
        Material wallPeach = GetOrCreateLitMaterial(
            "PremiumWallPeach", new Color32(255, 226, 214, 255), 0.18f);
        Material cream = GetOrCreateLitMaterial(
            "PremiumCreamTrim", new Color32(255, 244, 214, 255), 0.48f);
        Material gold = GetOrCreateLitMaterial(
            "PremiumGoldTrim", new Color32(249, 190, 78, 255), 0.62f, 0.08f);
        Material softPeach = GetOrCreateLitMaterial(
            "PremiumWainscotPeach", new Color32(255, 215, 195, 255), 0.36f);
        Material softMint = GetOrCreateLitMaterial(
            "PremiumWainscotMint", new Color32(198, 239, 224, 255), 0.36f);
        Material softLilac = GetOrCreateLitMaterial(
            "PremiumWainscotLilac", new Color32(225, 211, 246, 255), 0.36f);
        Material candyPink = GetOrCreateLitMaterial(
            "PremiumMotifPink", new Color32(255, 126, 169, 255), 0.66f, 0.34f);
        Material candyAqua = GetOrCreateLitMaterial(
            "PremiumMotifAqua", new Color32(91, 219, 213, 255), 0.66f, 0.32f);
        Material candyLemon = GetOrCreateLitMaterial(
            "PremiumMotifLemon", new Color32(255, 222, 92, 255), 0.66f, 0.3f);
        Material candyLilac = GetOrCreateLitMaterial(
            "PremiumMotifLilac", new Color32(185, 146, 238, 255), 0.66f, 0.3f);

        ApplySurfaceMaterial(scene, "BackWall", wallSky);
        ApplySurfaceMaterial(scene, "LeftWall", wallMint);
        ApplySurfaceMaterial(scene, "RightWall", wallPeach);
        ApplySurfaceMaterial(scene, "Floor", cream);
        ApplySurfaceMaterial(scene, "WalkableFloor", cream);
        ApplySurfaceMaterial(scene, "Baseboard_Back", cream);
        ApplySurfaceMaterial(scene, "Baseboard_Left", cream);
        ApplySurfaceMaterial(scene, "Baseboard_Right", cream);
        RecolorNamedSurfaces(scene, "BackWall", wallSky);
        RecolorNamedSurfaces(scene, "LeftWall", wallMint);
        RecolorNamedSurfaces(scene, "RightWall", wallPeach);
        RecolorNamedSurfaces(scene, "Floor", cream);
        HideNamedRenderer(scene, "BackWall");
        HideNamedRenderer(scene, "LeftWall");
        HideNamedRenderer(scene, "RightWall");

        GameObject architecture = new GameObject(ArchitectureName);
        architecture.transform.SetParent(root, false);

        Transform liners = CreateGroup(architecture.transform, "PastelWallLiners");
        CreateBox(liners, "BackLiner", new Vector3(0f, 1.92f, 2.788f),
            new Vector3(7.72f, 2.02f, 0.04f), wallSky);
        CreateBox(liners, "LeftLiner", new Vector3(-3.788f, 1.92f, 0f),
            new Vector3(0.04f, 2.02f, 5.72f), wallMint);
        CreateRightLinerWithWindowOpening(liners, scene, wallPeach);

        Transform wainscot = CreateGroup(architecture.transform, "RoundedWainscot");
        CreateBackWainscot(wainscot, softPeach, softMint, softLilac, cream, gold);
        CreateSideWainscot(wainscot, -1f, softMint, softLilac, cream, gold);
        CreateSideWainscot(wainscot, 1f, softLilac, softPeach, cream, gold);

        Transform crown = CreateGroup(architecture.transform, "PolishedCrownTrim");
        CreateBox(crown, "Back_Cream", new Vector3(0f, 2.82f, 2.69f),
            new Vector3(7.78f, 0.13f, 0.17f), cream);
        CreateBox(crown, "Back_GoldInset", new Vector3(0f, 2.76f, 2.59f),
            new Vector3(7.5f, 0.035f, 0.045f), gold);
        CreateBox(crown, "Left_Cream", new Vector3(-3.69f, 2.82f, 0f),
            new Vector3(0.17f, 0.13f, 5.72f), cream);
        CreateBox(crown, "Right_Cream", new Vector3(3.69f, 2.82f, 0f),
            new Vector3(0.17f, 0.13f, 5.72f), cream);
        CreateBox(crown, "Left_GoldInset", new Vector3(-3.59f, 2.76f, 0f),
            new Vector3(0.045f, 0.035f, 5.45f), gold);
        CreateBox(crown, "Right_GoldInset", new Vector3(3.59f, 2.76f, 0f),
            new Vector3(0.045f, 0.035f, 5.45f), gold);

        Transform motifs = CreateGroup(architecture.transform, "CandyWallMotifs");
        CreatePawMotif(motifs, "BackPaw_Left", new Vector3(-2.55f, 1.93f, 2.65f),
            Quaternion.identity, candyPink, candyLemon, 1f);
        CreatePawMotif(motifs, "BackPaw_Right", new Vector3(2.26f, 2.02f, 2.65f),
            Quaternion.identity, candyAqua, candyLilac, 0.88f);
        CreatePawMotif(motifs, "LeftPaw", new Vector3(-3.57f, 1.94f, 0.74f),
            Quaternion.Euler(0f, 90f, 0f), candyLilac, candyPink, 0.76f);

        var animatedSparkles = new List<Transform>();
        animatedSparkles.Add(CreateSparkle(
            motifs, "Sparkle_BackLeft", new Vector3(-3.2f, 2.4f, 2.61f),
            Quaternion.identity, candyLemon, 0.13f));
        animatedSparkles.Add(CreateSparkle(
            motifs, "Sparkle_BackMid", new Vector3(0.62f, 2.31f, 2.61f),
            Quaternion.identity, candyPink, 0.1f));
        animatedSparkles.Add(CreateSparkle(
            motifs, "Sparkle_BackRight", new Vector3(3.12f, 1.72f, 2.61f),
            Quaternion.identity, candyAqua, 0.11f));
        animatedSparkles.Add(CreateSparkle(
            motifs, "Sparkle_Left", new Vector3(-3.53f, 2.37f, -0.82f),
            Quaternion.Euler(0f, 90f, 0f), candyAqua, 0.1f));
        animatedSparkles.Add(CreateSparkle(
            motifs, "Sparkle_RightFront", new Vector3(3.53f, 1.56f, -1.74f),
            Quaternion.Euler(0f, 90f, 0f), candyLemon, 0.09f));

        PremiumWorldAmbientFx ambientFx = architecture.AddComponent<PremiumWorldAmbientFx>();
        ambientFx.EditorConfigure(animatedSparkles.ToArray(), 0.085f, 7f, 0.018f, 1.05f);
        EditorUtility.SetDirty(ambientFx);
    }

    private static void RecolorLivingRoomFurniture(Scene scene)
    {
        Material cream = GetOrCreateLitMaterial(
            "LivingRoomPastelCream", new Color32(255, 244, 214, 255), 0.42f);
        Material mint = GetOrCreateLitMaterial(
            "LivingRoomPastelMint", new Color32(126, 235, 190, 255), 0.4f);
        Material coral = GetOrCreateLitMaterial(
            "LivingRoomPastelCoral", new Color32(255, 150, 158, 255), 0.4f);
        Material lilac = GetOrCreateLitMaterial(
            "LivingRoomPastelLilac", new Color32(210, 176, 242, 255), 0.4f);
        Material peach = GetOrCreateLitMaterial(
            "LivingRoomPastelPeach", new Color32(255, 196, 150, 255), 0.4f);
        Material aqua = GetOrCreateLitMaterial(
            "LivingRoomPastelAqua", new Color32(91, 219, 213, 255), 0.42f);
        Material gold = GetOrCreateLitMaterial(
            "LivingRoomPastelGold", new Color32(255, 201, 86, 255), 0.58f, 0.12f);

        Material pack = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/LowPolyLivingRoomPack/Materials/M_LowPolyLivingRoom.mat");
        if (pack != null)
        {
            Color tint = new Color(1f, 0.92f, 0.9f, 1f);
            pack.color = tint;
            if (pack.HasProperty("_BaseColor"))
                pack.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(pack);
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer.GetComponentInParent<CatMovement>() != null ||
                    renderer.GetComponentInParent<Camera>() != null)
                    continue;
                string name = renderer.gameObject.name;
                if (name.IndexOf("Sofa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Couch", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = coral;
                else if (name.IndexOf("Carpet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         name.IndexOf("Rug", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = mint;
                else if (name.IndexOf("Table", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         name.IndexOf("Coffee", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = peach;
                else if (name.IndexOf("Armchair", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         name.IndexOf("Chair", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = lilac;
                else if (name.IndexOf("Lamp", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = gold;
                else if (name.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = mint;
                else if (name.IndexOf("TV", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         name.IndexOf("Console", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = aqua;
                else if (name.IndexOf("Bookshelf", StringComparison.OrdinalIgnoreCase) >= 0)
                    renderer.sharedMaterial = cream;
            }
        }
    }

    private static void CreateBackWainscot(
        Transform parent,
        Material baseMaterial,
        Material alternateOne,
        Material alternateTwo,
        Material cream,
        Material gold)
    {
        CreateBox(parent, "Back_Base", new Vector3(0f, 0.57f, 2.775f),
            new Vector3(7.72f, 0.82f, 0.035f), baseMaterial);
        Material[] panels = { alternateOne, alternateTwo, alternateOne, alternateTwo };
        float[] xPositions = { -2.82f, -0.94f, 0.94f, 2.82f };
        for (int i = 0; i < xPositions.Length; i++)
        {
            string prefix = "BackPanel_" + (i + 1);
            CreateBox(parent, prefix + "_Inset", new Vector3(xPositions[i], 0.57f, 2.744f),
                new Vector3(1.54f, 0.56f, 0.025f), panels[i]);
            CreatePanelFrame(parent, prefix, new Vector3(xPositions[i], 0.57f, 2.724f),
                Quaternion.identity, new Vector2(1.62f, 0.64f), cream);
        }

        CreateBox(parent, "Back_ChairRail", new Vector3(0f, 1.035f, 2.69f),
            new Vector3(7.8f, 0.105f, 0.16f), cream);
        CreateBox(parent, "Back_ChairRailGold", new Vector3(0f, 1.065f, 2.6f),
            new Vector3(7.48f, 0.032f, 0.04f), gold);
    }

    private static void CreateSideWainscot(
        Transform parent,
        float side,
        Material baseMaterial,
        Material alternate,
        Material cream,
        Material gold)
    {
        string sideName = side < 0f ? "Left" : "Right";
        float baseX = side * 3.775f;
        float detailX = side * 3.742f;
        float frameX = side * 3.719f;
        CreateBox(parent, sideName + "_Base", new Vector3(baseX, 0.57f, 0f),
            new Vector3(0.035f, 0.82f, 5.72f), baseMaterial);

        float[] zPositions = { -2.08f, -0.69f, 0.69f, 2.08f };
        for (int i = 0; i < zPositions.Length; i++)
        {
            string prefix = sideName + "Panel_" + (i + 1);
            CreateBox(parent, prefix + "_Inset", new Vector3(detailX, 0.57f, zPositions[i]),
                new Vector3(0.025f, 0.56f, 1.08f), (i & 1) == 0 ? alternate : baseMaterial);
            CreatePanelFrame(parent, prefix, new Vector3(frameX, 0.57f, zPositions[i]),
                Quaternion.Euler(0f, 90f, 0f), new Vector2(1.16f, 0.64f), cream);
        }

        float railX = side * 3.68f;
        float goldX = side * 3.59f;
        CreateBox(parent, sideName + "_ChairRail", new Vector3(railX, 1.035f, 0f),
            new Vector3(0.16f, 0.105f, 5.78f), cream);
        CreateBox(parent, sideName + "_ChairRailGold", new Vector3(goldX, 1.065f, 0f),
            new Vector3(0.04f, 0.032f, 5.46f), gold);
    }

    private static void CreatePanelFrame(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        Vector2 size,
        Material material)
    {
        Transform frame = CreateGroup(parent, name + "_Frame");
        frame.localPosition = position;
        frame.localRotation = rotation;
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        CreateBox(frame, "Top", new Vector3(0f, halfHeight, 0f),
            new Vector3(size.x, 0.035f, 0.03f), material);
        CreateBox(frame, "Bottom", new Vector3(0f, -halfHeight, 0f),
            new Vector3(size.x, 0.035f, 0.03f), material);
        CreateBox(frame, "Left", new Vector3(-halfWidth, 0f, 0f),
            new Vector3(0.035f, size.y, 0.03f), material);
        CreateBox(frame, "Right", new Vector3(halfWidth, 0f, 0f),
            new Vector3(0.035f, size.y, 0.03f), material);
    }

    private static void CreatePawMotif(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        Material padMaterial,
        Material toeMaterial,
        float scale)
    {
        Transform paw = CreateGroup(parent, name);
        paw.localPosition = position;
        paw.localRotation = rotation;
        paw.localScale = Vector3.one * scale;

        CreateSphere(paw, "Pad", new Vector3(0f, -0.055f, 0f),
            new Vector3(0.19f, 0.15f, 0.038f), padMaterial);
        CreateSphere(paw, "Toe_1", new Vector3(-0.155f, 0.085f, 0f),
            new Vector3(0.061f, 0.074f, 0.035f), toeMaterial);
        CreateSphere(paw, "Toe_2", new Vector3(-0.053f, 0.15f, 0f),
            new Vector3(0.064f, 0.078f, 0.035f), toeMaterial);
        CreateSphere(paw, "Toe_3", new Vector3(0.058f, 0.15f, 0f),
            new Vector3(0.064f, 0.078f, 0.035f), toeMaterial);
        CreateSphere(paw, "Toe_4", new Vector3(0.158f, 0.082f, 0f),
            new Vector3(0.061f, 0.074f, 0.035f), toeMaterial);
    }

    private static Transform CreateSparkle(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        Material material,
        float size)
    {
        Transform sparkle = CreateGroup(parent, name);
        sparkle.localPosition = position;
        sparkle.localRotation = rotation;
        CreateSphere(sparkle, "Vertical", Vector3.zero,
            new Vector3(size * 0.26f, size, size * 0.18f), material);
        CreateSphere(sparkle, "Horizontal", Vector3.zero,
            new Vector3(size, size * 0.26f, size * 0.18f), material);
        CreateSphere(sparkle, "Core", Vector3.zero,
            Vector3.one * size * 0.34f, material);
        return sparkle;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    // The right wall carries the WindowSystem. A solid RightLiner box would bury the
    // glass/sky panels and kill the day/night light. Build the liner as a frame around the
    // window opening instead so the pane stays visible and the sun beam still reaches the room.
    // The hole is placed in the wooden frame ring (outside the glass, inside the frame edge)
    // so the wall tucks behind the frame with no gap over the glass.
    private static void CreateRightLinerWithWindowOpening(
        Transform liners, Scene scene, Material wallPeach)
    {
        // Liner footprint (unchanged from the original solid box).
        const float x = 3.788f;
        const float thickness = 0.04f;
        const float yMin = 0.91f, yMax = 2.93f;   // center 1.92, height 2.02
        const float zMin = -2.86f, zMax = 2.86f;  // center 0,    length 5.72

        if (!TryGetRightWallWindowOpening(scene, out float hzMin, out float hzMax,
                out float hyMin, out float hyMax))
        {
            // No window found on the right wall: keep the original solid liner.
            CreateBox(liners, "RightLiner", new Vector3(x, 1.92f, 0f),
                new Vector3(thickness, 2.02f, 5.72f), wallPeach);
            return;
        }

        // Front/back jambs run the full liner height on either side of the opening.
        CreateBox(liners, "RightLiner_JambFront",
            new Vector3(x, (yMin + yMax) * 0.5f, (zMin + hzMin) * 0.5f),
            new Vector3(thickness, yMax - yMin, hzMin - zMin), wallPeach);
        CreateBox(liners, "RightLiner_JambBack",
            new Vector3(x, (yMin + yMax) * 0.5f, (hzMax + zMax) * 0.5f),
            new Vector3(thickness, yMax - yMin, zMax - hzMax), wallPeach);
        // Sill below and header above, spanning only the opening width.
        CreateBox(liners, "RightLiner_Sill",
            new Vector3(x, (yMin + hyMin) * 0.5f, (hzMin + hzMax) * 0.5f),
            new Vector3(thickness, hyMin - yMin, hzMax - hzMin), wallPeach);
        CreateBox(liners, "RightLiner_Header",
            new Vector3(x, (hyMax + yMax) * 0.5f, (hzMin + hzMax) * 0.5f),
            new Vector3(thickness, yMax - hyMax, hzMax - hzMin), wallPeach);
    }

    // Finds the WindowSystem on the right wall and returns an opening rect (z/y) that sits
    // in the wooden frame ring: midway between the glass edge and the frame edge, so the wall
    // never covers the glass yet leaves no visible gap.
    private static bool TryGetRightWallWindowOpening(
        Scene scene, out float hzMin, out float hzMax, out float hyMin, out float hyMax)
    {
        hzMin = hzMax = hyMin = hyMax = 0f;
        Transform windowSystem = null;
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            windowSystem = FindDeepChild(sceneRoot.transform, "WindowSystem");
            if (windowSystem != null)
                break;
        }
        if (windowSystem == null)
            return false;

        Transform window = windowSystem.Find("Window");
        Transform glassPanel = windowSystem.Find("GlassPanel");
        if (window == null || glassPanel == null)
            return false;

        if (!TryGetWorldBounds(window, out Bounds frame) ||
            !TryGetWorldBounds(glassPanel, out Bounds glass))
            return false;

        // Only carve when the window is on the right wall (positive X face).
        if (frame.center.x < 2.5f)
            return false;

        hzMin = (glass.min.z + frame.min.z) * 0.5f;
        hzMax = (glass.max.z + frame.max.z) * 0.5f;
        hyMin = (glass.min.y + frame.min.y) * 0.5f;
        hyMax = (glass.max.y + frame.max.y) * 0.5f;
        return true;
    }

    private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
    {
        bounds = new Bounds();
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool initialized = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;
            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return initialized;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindDeepChild(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static GameObject CreateBox(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        RemovePrimitiveCollider(gameObject);
        ConfigureRenderer(gameObject.GetComponent<Renderer>(), material, true);
        return gameObject;
    }

    private static GameObject CreateSphere(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        RemovePrimitiveCollider(gameObject);
        ConfigureRenderer(gameObject.GetComponent<Renderer>(), material, false);
        return gameObject;
    }

    private static void ConfigureRenderer(Renderer renderer, Material material, bool receiveShadows)
    {
        if (renderer == null)
            return;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = receiveShadows;
        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
        EditorUtility.SetDirty(renderer);
    }

    private static void RemovePrimitiveCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
    }

    private static void HideNamedRenderer(Scene scene, string objectName)
    {
        GameObject gameObject = CatHomeAuthoringWorkspace.FindNamedInScene(scene, objectName);
        Renderer renderer = gameObject != null ? gameObject.GetComponent<Renderer>() : null;
        if (renderer == null)
            return;
        renderer.enabled = false;
        EditorUtility.SetDirty(renderer);
    }

    private static void RecolorNamedSurfaces(Scene scene, string nameContains, Material material)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].GetComponentInParent<CatMovement>() != null)
                    continue;
                if (renderers[i].gameObject.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                renderers[i].sharedMaterial = material;
                EditorUtility.SetDirty(renderers[i]);
            }
        }
    }

    private static void ApplySurfaceMaterial(Scene scene, string objectName, Material material)
    {
        GameObject gameObject = CatHomeAuthoringWorkspace.FindNamedInScene(scene, objectName);
        Renderer renderer = gameObject != null ? gameObject.GetComponent<Renderer>() : null;
        if (renderer == null)
            return;
        renderer.sharedMaterial = material;
        EditorUtility.SetDirty(renderer);
    }

    private static Material GetOrCreateLitMaterial(
        string name,
        Color colour,
        float smoothness,
        float emissionStrength = 0f)
    {
        EnsureFolder(MaterialFolder);
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (shader != null && material.shader != shader)
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", colour);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", colour);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        if (material.HasProperty("_EmissionColor"))
        {
            if (emissionStrength > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(
                    "_EmissionColor",
                    new Color(
                        colour.r * emissionStrength,
                        colour.g * emissionStrength,
                        colour.b * emissionStrength,
                        1f));
                material.globalIlluminationFlags =
                    MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }

        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void CreateFillLight(
        Transform parent,
        string name,
        Vector3 position,
        Color colour,
        float intensity,
        float range)
    {
        GameObject lightObject = new GameObject(name, typeof(Light));
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = position;
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = colour;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
    }

    private static void RemoveChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static T FindInScene<T>(Scene scene, string objectName) where T : Component
    {
        GameObject gameObject = CatHomeAuthoringWorkspace.FindNamedInScene(scene, objectName);
        return gameObject != null ? gameObject.GetComponent<T>() : null;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        string name = path.Substring(slash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
