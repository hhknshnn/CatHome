using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Imports Blender store models and authors movable room products.</summary>
public static class StoreProductContentBuilder
{
    private const string PremiumFurnitureModelFolder =
        "Assets/Art/PremiumFurniture/Models/";
    private const string MaterialFolder = "Assets/Art/StoreProducts/Materials";
    private const string PrefabFolder = "Assets/Art/StoreProducts/Prefabs";
    private const string SceneRootName = "StoreProducts";

    private static readonly Dictionary<string, Color> Palette =
        new Dictionary<string, Color>(StringComparer.Ordinal)
        {
            { "CH_Orange", new Color32(227, 79, 20, 255) },
            { "CH_OrangeLight", new Color32(255, 143, 61, 255) },
            { "CH_Teal", new Color32(15, 122, 117, 255) },
            { "CH_TealLight", new Color32(48, 173, 166, 255) },
            { "CH_Cream", new Color32(255, 212, 143, 255) },
            { "CH_Gold", new Color32(225, 133, 23, 255) },
            { "CH_Purple", new Color32(89, 51, 140, 255) },
            { "CH_Pink", new Color32(235, 107, 122, 255) },
            { "CH_Ink", new Color32(24, 20, 30, 255) },
            { "CH_Screen", new Color32(15, 48, 64, 255) },
            { "CH_AquaBright", new Color32(67, 220, 211, 255) },
            { "CH_MintBright", new Color32(126, 235, 190, 255) },
            { "CH_CoralBright", new Color32(255, 120, 130, 255) },
            { "CH_LilacBright", new Color32(188, 143, 235, 255) },
            { "CH_LemonBright", new Color32(255, 222, 94, 255) },
            { "CH_White", new Color32(255, 248, 230, 255) }
        };

    [MenuItem("Tools/Cat Home/Store/Build Real Product Content")]
    public static void BuildFromMenu()
    {
        string result = BuildSilently();
        EditorUtility.DisplayDialog("Cat Home Store Products", result, "OK");
    }

    public static string BuildSilently()
    {
        BuildProductAssetsSilently();
        BuildRoomSceneProducts(
            HomeRoomService.LivingRoomScenePath,
            HomeRoomService.LivingRoomId);
        BuildRoomSceneProducts(
            HomeRoomService.BathroomScenePath,
            HomeRoomService.BathroomId);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.KitchenScenePath) != null)
        {
            BuildRoomSceneProducts(
                HomeRoomService.KitchenScenePath,
                HomeRoomService.KitchenId);
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.BedroomScenePath) != null)
        {
            BuildRoomSceneProducts(
                HomeRoomService.BedroomScenePath,
                HomeRoomService.BedroomId);
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.GardenScenePath) != null)
        {
            BuildRoomSceneProducts(
                HomeRoomService.GardenScenePath,
                HomeRoomService.GardenId);
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.BalconyScenePath) != null)
        {
            BuildRoomSceneProducts(
                HomeRoomService.BalconyScenePath,
                HomeRoomService.BalconyId);
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.PatioScenePath) != null)
        {
            BuildRoomSceneProducts(
                HomeRoomService.PatioScenePath,
                HomeRoomService.PatioId);
        }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.SecondFloorScenePath) != null)
        {
            BuildRoomSceneProducts(
                HomeRoomService.SecondFloorScenePath,
                HomeRoomService.SecondFloorId);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return StoreCatalogAssets.PlaceableProducts.Length +
               " real products prefabbed and connected to room placement.";
    }

    public static void BuildProductAssetsSilently()
    {
        EnsureFolders();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Dictionary<string, Material> materials = BuildMaterials();
        for (int i = 0; i < StoreCatalogAssets.PlaceableProducts.Length; i++)
            BuildPrefab(StoreCatalogAssets.PlaceableProducts[i].WithoutRoomLayout(), materials);
        CatProductContentBuilder.BuildLegacyAssets(materials);
        RoomProductInteractionBuilder.UpgradePrefabs();
        ModernWorldArtBuilder.ApplyCatalogPrefabs();
        LivingProgressionComposition.ApplyProductPrefabs();
        BathroomThemeBuilder.ApplyProductPrefabs();
        KitchenThemeBuilder.ApplyProductPrefabs();
        RemainingRoomsThemeBuilder.ApplyProductPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void RebuildProductWithExistingMaterials(string productId)
    {
        if(!StoreCatalogAssets.TryGet(productId,out var definition))throw new ArgumentException(productId);
        var materials=new Dictionary<string,Material>(StringComparer.Ordinal);
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{MaterialFolder}))
        {var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));materials[material.name]=material;}
        BuildPrefab(definition.WithoutRoomLayout(),materials);
        RoomProductInteractionBuilder.UpgradePrefab(definition,new System.Text.StringBuilder());
        ModernWorldArtBuilder.ApplyCatalogPrefab(PrefabFolder + "/" + definition.PrefabName + ".prefab");
        LivingProgressionComposition.ApplyProductPrefab(PrefabFolder + "/" + definition.PrefabName + ".prefab");
        BathroomThemeBuilder.ApplyProductPrefab(PrefabFolder + "/" + definition.PrefabName + ".prefab");
        KitchenThemeBuilder.ApplyProductPrefab(PrefabFolder + "/" + definition.PrefabName + ".prefab");
        RemainingRoomsThemeBuilder.ApplyProductPrefab(PrefabFolder + "/" + definition.PrefabName + ".prefab");
        AssetDatabase.SaveAssets();
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("No compatible lit shader was found.");

        var result = new Dictionary<string, Material>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, Color> pair in Palette)
        {
            string path = MaterialFolder + "/" + pair.Key + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = pair.Key };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = pair.Value;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", pair.Value);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.12f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            result[pair.Key] = material;
        }
        return result;
    }

    private static void BuildPrefab(
        StoreCatalogAsset definition,
        Dictionary<string, Material> materials)
    {
        if(CatProductContentBuilder.TryBuild(definition,materials))return;
        if (TryBuildPremiumBathroomHeroPrefab(definition, materials))
            return;
        if (TryBuildPremiumKitchenHeroPrefab(definition, materials))
            return;
        if (TryBuildPremiumBedroomHeroPrefab(definition, materials))
            return;
        if (TryBuildPremiumGardenHeroPrefab(definition, materials))
            return;
        if (TryGetBathroomFixtureSource(
                definition.ProductId,
                out string bathroomSource))
        {
            BuildBathroomFixturePrefab(definition, bathroomSource);
            return;
        }
        if (definition.ProductId == HomeStoreService.PlayTunnelId)
        {
            BuildProceduralTunnelPrefab(definition, materials);
            return;
        }
        if (HomeStoreService.IsBathroomCollectionProduct(definition.ProductId))
        {
            BuildProceduralBathroomPrefab(definition, materials);
            return;
        }
        if (HomeStoreService.IsKitchenCollectionProduct(definition.ProductId))
        {
            BuildProceduralKitchenPrefab(definition, materials);
            return;
        }
        if (HomeStoreService.IsBedroomCollectionProduct(definition.ProductId))
        {
            BuildProceduralBedroomPrefab(definition, materials);
            return;
        }
        if (HomeStoreService.IsGardenCollectionProduct(definition.ProductId))
        {
            BuildProceduralGardenPrefab(definition, materials);
            return;
        }
        if (HomeStoreService.IsBalconyCollectionProduct(definition.ProductId))
        {
            BuildProceduralBalconyPrefab(definition, materials);
            return;
        }
        if (HomeStoreService.IsPatioCollectionProduct(definition.ProductId))
        {
            BuildProceduralPatioPrefab(definition, materials);
            return;
        }
        if (HomeStoreService.IsSecondFloorCollectionProduct(definition.ProductId))
        {
            BuildProceduralLoftPrefab(definition, materials);
            return;
        }
        if (definition.ProductId == HomeStoreService.BookSetId)
        {
            BuildBookshelfBookSetPrefab(definition, materials);
            return;
        }
        if (definition.ProductId == HomeStoreService.GameConsoleId)
        {
            if (!TryBuildPremiumRoomProductPrefab(definition, materials))
                BuildGameConsoleSetPrefab(definition);
            return;
        }
        if (definition.ProductId == HomeStoreService.StereoId)
        {
            if (!TryBuildPremiumRoomProductPrefab(definition, materials))
                BuildSpeakerSystemPrefab(definition);
            return;
        }

        // Living Room products still point at LowPolyLivingRoomPack sources in the
        // catalog. As each one is reauthored in the premium furniture language,
        // its FBX takes over here and the pack asset stays as the fallback, so
        // footprint, scale, price and save ids never move.
        if (TryBuildPremiumRoomProductPrefab(definition, materials))
            return;

        string modelPath = definition.SourceAssetPath;
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
            throw new InvalidOperationException("Store product model is missing: " + modelPath);

        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (model == null)
                throw new InvalidOperationException("Could not instantiate " + modelPath);
            model.name = definition.PrefabName + "_Model";
            model.transform.SetParent(visual.transform, false);
            model.transform.localPosition = definition.VisualOffset;
            model.transform.localScale = Vector3.one * definition.VisualScale;
            ReplaceMaterials(model, materials);

            ConfigureProductCollider(visual.AddComponent<BoxCollider>(), definition);

            ConfigureProductComponents(root, visual, definition);
            if (definition.ProductId == HomeStoreService.FeatherToyId)
                AttachFeatherPlayActivity(root, visual.transform);

            string prefabPath = PrefabFolder + "/" + definition.PrefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// The Kitchen's ten products all ship as reauthored premium models with a
    /// cat routine, and all ten share one orientation rule: front authored at
    /// -Z and absent from `facesBackward`, so an authored (x, y, z) lands at
    /// (-x, y, z). The four that predate this wave were in `facesBackward`
    /// because their fronts were authored at +Z; reauthoring them took them out.
    /// </summary>
    /// <summary>
    /// Bedroom is the first room that needs BOTH orientation mappings at once.
    /// Its floor products (bed, rug, night light, yarn basket, vanity stool,
    /// star canopy) are authored front-at--Z and stay OUT of `facesBackward`, so
    /// an authored (x, y, z) lands at (-x, y, z). Its wall products (wardrobe at
    /// yaw 270, daybed at 180, nightstand and art at 90) need the 180, so for
    /// those an authored point lands at (x, y, -z) instead.
    ///
    /// The wall alone does not decide it. `BathroomWallMirror` hangs on a left
    /// wall at yaw 270 and needs no 180; `BedroomDreamArt` hangs on a left wall
    /// at yaw 90 and does. Work the mapping out per product, then measure it.
    /// </summary>
    private static bool TryBuildPremiumBedroomHeroPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        bool isHero =
            definition.ProductId == HomeStoreService.BedroomQueenBedId ||
            definition.ProductId == HomeStoreService.BedroomWardrobeId ||
            definition.ProductId == HomeStoreService.BedroomWindowDaybedId ||
            definition.ProductId == HomeStoreService.BedroomNightstandId ||
            definition.ProductId == HomeStoreService.BedroomDreamArtId ||
            definition.ProductId == HomeStoreService.BedroomNightLightId ||
            definition.ProductId == HomeStoreService.BedroomYarnBasketId ||
            definition.ProductId == HomeStoreService.BedroomVanityStoolId ||
            definition.ProductId == HomeStoreService.BedroomPawRugId;
        if (!isHero)
            return false;

        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            if (!TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
                return false;
            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            AttachBedroomActivity(root, visual, definition, materials);
            PrefabUtility.SaveAsPrefabAsset(
                root, PrefabFolder + "/" + definition.PrefabName + ".prefab");
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AttachBedroomActivity(
        GameObject root, GameObject visual, StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        string id = definition.ProductId;
        if (id == HomeStoreService.BedroomQueenBedId)
            AttachBedNapActivity(root, visual);
        else if (id == HomeStoreService.BedroomVanityStoolId)
            AttachVanityStoolNapActivity(root, visual);
        else if (id == HomeStoreService.BedroomWardrobeId)
            AttachWardrobeScratchActivity(root, visual);
        else if (id == HomeStoreService.BedroomWindowDaybedId)
            AttachDaybedWatchActivity(root, visual);
        else if (id == HomeStoreService.BedroomNightstandId)
            AttachKnockOffActivity(root, visual, materials);
        else if (id == HomeStoreService.BedroomYarnBasketId)
            AttachYarnSwatActivity(root, visual);
        else if (id == HomeStoreService.BedroomPawRugId)
            AttachBedroomMatKneadActivity(root, visual);
    }

    /// <summary>Mattress at an authored 0.520, clear half at +X.</summary>
    private static void AttachBedNapActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredMattressTop = .520f;
        const float authoredPerchX = .480f;

        float perchX = -authoredPerchX * scale;
        Transform floor = MakePoint(root, "PerchFloorPoint",
            new Vector3(perchX, 0f, -1.06f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(perchX, authoredMattressTop * scale, -.120f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(perchX, 0f, -1.22f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "bed-nap", "QUEEN BED", CatActivityKind.BedNap, QuestType.Sleep,
            0, "SLEEP", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomQueenBedId);
        activity.EditorConfigurePerch(floor, perch, 4.4f, 34f, 94f, "SO SOFT!");
    }

    /// <summary>Pouffe seat at an authored 0.480.</summary>
    private static void AttachVanityStoolNapActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredSeatTop = .480f;

        Transform floor = MakePoint(root, "PerchFloorPoint", new Vector3(0f, 0f, -.60f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(0f, authoredSeatTop * scale, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.74f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "vanity-stool-nap", "VANITY STOOL", CatActivityKind.VanityStoolNap,
            QuestType.Sleep, 0, "CURL UP", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomVanityStoolId);
        activity.EditorConfigurePerch(floor, perch, 2.8f, 16f, 94f, "MY POUFFE!");
    }

    /// <summary>
    /// A 2.15 wardrobe with two tall doors is a scratching post as far as the
    /// cat is concerned, so this reuses <see cref="ScratchPostActivity"/> rather
    /// than adding a class.
    ///
    /// The wardrobe IS in `facesBackward`, so root space here flips Z, not X.
    /// The scratch point is on the centre line, which makes that moot — but the
    /// approach offset is not, and -Z is still the room.
    /// </summary>
    private static void AttachWardrobeScratchActivity(GameObject root, GameObject visual)
    {
        // Root +Z, not -Z. Measured: with `facesBackward` on, the authored front
        // at -Z becomes root +Z, and at yaw 270 root +Z maps to world -X, which
        // is the room. Every approach point on a Bedroom wall product is +Z.
        Transform scratch = MakePoint(root, "ScratchPoint", new Vector3(0f, 0f, .62f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .78f));

        ScratchPostActivity activity = root.AddComponent<ScratchPostActivity>();
        activity.EditorConfigure(
            "wardrobe-scratch", "WARDROBE", CatActivityKind.WardrobeScratch,
            QuestType.Scratch, 0, "SCRATCH", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomWardrobeId);
        activity.EditorConfigureScratch(scratch, 2.8f);
    }

    /// <summary>
    /// A supported nap on the window cushion; saved activity identity is retained.
    /// </summary>
    private static void AttachDaybedWatchActivity(GameObject root, GameObject visual)
    {
        BedroomPlayRestBuilder.ConfigureDaybed(root, visual);
    }

    /// <summary>
    /// The nightstand's glass ships as its own single-object FBX on the
    /// nightstand's origin, so it can hang under a pivot the routine drops.
    ///
    /// The nightstand IS in `facesBackward`, so root space flips **Z** and not
    /// X — the opposite of the toilet's neighbours but the same as the toilet
    /// itself. The glass is authored at (.170, .568, -.060) and therefore lands
    /// at (.170, .568, .060) times the model scale.
    /// </summary>
    private static void AttachKnockOffActivity(
        GameObject root, GameObject visual, IReadOnlyDictionary<string, Material> materials)
    {
        GameObject glassAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "BedroomNightstandGlass_Premium.fbx");
        if (glassAsset == null)
            return;

        float scale = ReadModelScale(visual);
        var authoredGlass = new Vector3(.170f, .568f, -.060f);
        var glassPosition = new Vector3(
            authoredGlass.x * scale, authoredGlass.y * scale, -authoredGlass.z * scale);

        Transform pivot = MakePoint(visual, "GlassPivot", glassPosition);

        GameObject glass = PrefabUtility.InstantiatePrefab(glassAsset) as GameObject;
        if (glass == null)
            return;
        glass.name = "NightstandGlass";
        glass.transform.SetParent(pivot, false);
        // The glass mesh is authored on the nightstand's origin, so it has to be
        // pushed back by the pivot offset: leaving it at zero would put the
        // glass at twice the offset.
        glass.transform.localPosition = -glassPosition;
        glass.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        glass.transform.localScale = Vector3.one * scale;
        RemoveChildColliders(glass);
        ReplaceMaterials(glass, materials);

        // Root +Z is the room here: yaw 90 maps root +Z to world +X, and the
        // nightstand is on the left wall. The glass therefore falls towards +Z
        // as well — a glass pushed the other way would go into the plaster.
        Transform reach = MakePoint(root, "KnockReachPoint",
            new Vector3(glassPosition.x, 0f, glassPosition.z + .62f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(glassPosition.x, 0f, glassPosition.z + .78f));

        KnockOffActivity activity = root.AddComponent<KnockOffActivity>();
        activity.EditorConfigure(
            "knock-off", "NIGHTSTAND", CatActivityKind.KnockOff, QuestType.KnockOff,
            0, "PUSH", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomNightstandId);
        activity.EditorConfigureKnock(reach, pivot, Vector3.forward, .55f, 2);
    }

    /// <summary>Three real paw contacts, short yarn rolls and a floor chase.</summary>
    private static void AttachYarnSwatActivity(GameObject root, GameObject visual)
    {
        BedroomPlayRestBuilder.ConfigureYarn(root, visual);
    }

    /// <summary>Cat sits in the lamp's glow and stares up at the moon crest.</summary>
    private static void AttachNightLightGazeActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);

        Transform look = MakePoint(root, "GazeLookPoint",
            new Vector3(0f, .758f * scale, -.030f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.62f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "night-light-gaze", "NIGHT LIGHT", CatActivityKind.NightLightGaze,
            QuestType.BedroomWatch, 0, "GAZE", 1.1f, 2f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomNightLightId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.8f, "SO WARM.");
    }

    /// <summary>
    /// The art hangs at 1.62, well above the cat, so this is the mirror's beat.
    /// Every point under this root is lifted with the product, which is harmless:
    /// SitLookActivity flattens the sit point to the cat's own y.
    /// </summary>
    private static void AttachArtGazeActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);

        // Root +Z is the room: yaw 90 with `facesBackward` on. Same call as the
        // nightstand two metres along the same wall.
        Transform look = MakePoint(root, "GazeLookPoint",
            new Vector3(0f, .378f * scale, .070f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .85f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "art-gaze", "DREAM ART", CatActivityKind.ArtGaze, QuestType.BedroomWatch,
            0, "LOOK", 1.1f, 2f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomDreamArtId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.6f, "SWEET DREAMS.");
    }

    /// <summary>Rug pad authored at +X, landing at -X.</summary>
    private static void AttachBedroomMatKneadActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredPadCentreX = .545f;
        const float authoredPadTop = .080f;

        float padX = -authoredPadCentreX * scale;
        Transform pad = MakePoint(root, "KneadPadPoint",
            new Vector3(padX, authoredPadTop * scale, 0f));
        Transform exit = MakePoint(root, "KneadExitPoint",
            new Vector3(padX, 0f, -.98f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(padX, 0f, -.86f));

        MatKneadActivity activity = root.AddComponent<MatKneadActivity>();
        activity.EditorConfigure(
            "bedroom-mat-knead", "BEDSIDE RUG", CatActivityKind.BedroomMatKnead,
            QuestType.MatKnead, 0, "KNEAD", 1.1f, 3f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomPawRugId);
        activity.EditorConfigureKnead(pad, exit, 6, .34f, 1.4f, 8f);
    }

    /// <summary>
    /// Garden is the first room whose ten products are ALL floor products, so no
    /// wall mapping is in play — but the yaws are not all 0. Yaw 90 sends an
    /// authored -Z front to world -X and yaw 270 sends it to +X, so the hammock
    /// at x -2.75 and the grill at x +2.65 will both end up facing out of the
    /// courtyard and will need `facesBackward`; the pergola at yaw 0 does not.
    /// Work each one out against where it stands, then measure it.
    /// </summary>
    private static bool TryBuildPremiumGardenHeroPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        bool isHero =
            definition.ProductId == HomeStoreService.GardenPergolaId ||
            definition.ProductId == HomeStoreService.GardenSaplingId ||
            definition.ProductId == HomeStoreService.GardenBistroSetId ||
            definition.ProductId == HomeStoreService.GardenHammockId ||
            definition.ProductId == HomeStoreService.GardenSunLoungerId ||
            definition.ProductId == HomeStoreService.GardenGrillId ||
            definition.ProductId == HomeStoreService.GardenBirdBathId ||
            definition.ProductId == HomeStoreService.GardenFlowerPotsId ||
            definition.ProductId == HomeStoreService.GardenDaisyBedId ||
            definition.ProductId == HomeStoreService.GardenYarnBallId;
        if (!isHero)
            return false;

        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            if (!TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
                return false;
            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            AttachGardenActivity(root, visual, definition, materials);
            PrefabUtility.SaveAsPrefabAsset(
                root, PrefabFolder + "/" + definition.PrefabName + ".prefab");
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AttachGardenActivity(
        GameObject root, GameObject visual, StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        string id = definition.ProductId;
        if (id == HomeStoreService.GardenPergolaId)
            AttachPergolaClimbActivity(root, visual);
        else if (id == HomeStoreService.GardenSaplingId)
            AttachTreeScratchActivity(root, visual);
        else if (id == HomeStoreService.GardenBistroSetId)
            AttachBistroPerchActivity(root, visual);
        else if (id == HomeStoreService.GardenHammockId)
            AttachHammockSwayActivity(root, visual, materials);
        else if (id == HomeStoreService.GardenSunLoungerId)
            AttachSunBaskActivity(root, visual);
        else if (id == HomeStoreService.GardenBirdBathId)
            AttachBirdBathSipActivity(root, visual);
        else if (id == HomeStoreService.GardenFlowerPotsId)
            AttachPotDigActivity(root, visual);
        else if (id == HomeStoreService.GardenDaisyBedId)
            AttachDaisyRollActivity(root, visual);
        else if (id == HomeStoreService.GardenYarnBallId)
            AttachYarnBallChaseActivity(root, visual, materials);
    }

    /// <summary>
    /// Bench at an authored 0.480, lattice perch rail on top at 1.052.
    ///
    /// The pergola's roof is at 1.44 and out of reach on purpose:
    /// <see cref="PantryClimbActivity"/> stages exactly two hops and neither may
    /// clear much more than half a metre, so the bench and the rail were sized
    /// against that limit rather than the other way round (0.480 up, then 0.572).
    /// The cat starts INSIDE the shelter in front of the bench, not out on the
    /// lawn, because the bench sits against the back lattice.
    /// </summary>
    private static void AttachPergolaClimbActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredBenchTop = .480f;
        const float authoredRailTop = 1.052f;

        Transform floor = MakePoint(root, "ClimbFloorPoint",
            new Vector3(0f, 0f, .060f * scale));
        Transform lower = MakePoint(root, "ClimbLowerPoint",
            new Vector3(0f, authoredBenchTop * scale, .400f * scale));
        Transform upper = MakePoint(root, "ClimbUpperPoint",
            new Vector3(0f, authoredRailTop * scale, .618f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.98f));

        PantryClimbActivity activity = root.AddComponent<PantryClimbActivity>();
        activity.EditorConfigure(
            "pergola-climb", "PERGOLA", CatActivityKind.PergolaClimb,
            QuestType.GardenClimb, 0, "CLIMB", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenPergolaId);
        activity.EditorConfigureClimb(floor, lower, upper, 2.4f);
    }

    /// <summary>
    /// A nursery sapling in a planter is a scratching post as far as the cat is
    /// concerned, so this reuses <see cref="ScratchPostActivity"/> the way the
    /// Bedroom wardrobe does rather than adding a class.
    ///
    /// The trunk carries real bark ribs between the soil and 1.05, which is the
    /// band the model was authored around; the scratch point stands the cat just
    /// clear of the planter rim so it reaches ribs and not basket weave. The
    /// sapling is a yaw 0 floor product and absent from `facesBackward`, so the
    /// courtyard is at root -Z and both points are negative.
    /// </summary>
    private static void AttachTreeScratchActivity(GameObject root, GameObject visual)
    {
        Transform scratch = MakePoint(root, "ScratchPoint", new Vector3(0f, 0f, -.46f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.62f));

        ScratchPostActivity activity = root.AddComponent<ScratchPostActivity>();
        activity.EditorConfigure(
            "tree-scratch", "SAPLING", CatActivityKind.TreeScratch,
            QuestType.Scratch, 0, "SCRATCH", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenSaplingId);
        activity.EditorConfigureScratch(scratch, 2.8f);
    }

    /// <summary>
    /// The café table top at an authored 0.660, kept clear on purpose.
    ///
    /// Everything that would normally dress a bistro table — cup, saucer, folded
    /// napkin — was authored onto the LEFT CHAIR instead, because a prop in the
    /// middle of a 0.66 round top ends up underneath the sleeping cat. The two
    /// chairs flank the table at +/-X so the courtyard side stays open, which is
    /// where the jump comes from: this is a yaw 0 floor product absent from
    /// `facesBackward`, so the room is at root -Z.
    /// </summary>
    private static void AttachBistroPerchActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredTableTop = .660f;
        const float authoredTableZ = .048f;

        Transform floor = MakePoint(root, "PerchFloorPoint", new Vector3(0f, 0f, -.52f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(0f, authoredTableTop * scale, authoredTableZ * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.72f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "bistro-perch", "BISTRO TABLE", CatActivityKind.BistroPerch,
            QuestType.Sleep, 0, "NAP", 1.1f, 8f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenBistroSetId);
        activity.EditorConfigurePerch(floor, perch, 3.2f, 18f, 92f, "CAFE NAP!");
    }

    /// <summary>
    /// The hammock bed hangs under a pivot and swings, the way the porch swing's
    /// bench does. It is a second single-object FBX on the stand's own origin,
    /// so the pivot offset is undone on the child.
    ///
    /// This is the first Garden product that needed `facesBackward`. Yaw 90
    /// sends an authored -Z front to world -X, and the hammock stands at
    /// x -2.75 where the courtyard is at +X, so without the 180 it faces the
    /// fence. With the 180 on, Z flips instead of X and the room is at root +Z:
    /// both the mount and the anchor below are positive.
    /// </summary>
    private static void AttachHammockSwayActivity(
        GameObject root, GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject bedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "GardenHammockBed_Premium.fbx");
        if (bedAsset == null)
            return;

        float scale = ReadModelScale(visual);
        const float authoredRing = .720f;
        const float authoredBedTop = .326f;

        BoxCollider hammockCollider = visual.GetComponent<BoxCollider>();
        if (hammockCollider != null)
            hammockCollider.isTrigger = true;

        float pivotY = authoredRing * scale;
        Transform pivot = new GameObject("SwingPivot").transform;
        pivot.SetParent(visual.transform, false);
        pivot.localPosition = new Vector3(0f, pivotY, 0f);

        GameObject bed = PrefabUtility.InstantiatePrefab(bedAsset) as GameObject;
        if (bed == null)
            return;
        bed.name = "HammockBed";
        bed.transform.SetParent(pivot, false);
        bed.transform.localPosition = new Vector3(0f, -pivotY, 0f);
        bed.transform.localRotation = Quaternion.identity;
        bed.transform.localScale = Vector3.one * scale;
        RemoveChildColliders(bed);
        ReplaceMaterials(bed, materials);

        Transform seatPoint = new GameObject("SwingSeatPoint").transform;
        seatPoint.SetParent(visual.transform, false);
        seatPoint.localPosition = new Vector3(0f, authoredBedTop * scale, 0f);
        seatPoint.SetParent(pivot, true);

        Transform mount = new GameObject("SwingMountPoint").transform;
        mount.SetParent(root.transform, false);
        mount.localPosition = new Vector3(0f, 0f, .52f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(0f, 0f, .76f);

        SwingRideActivity activity = root.AddComponent<SwingRideActivity>();
        activity.EditorConfigure(
            "hammock-sway", "HAMMOCK", CatActivityKind.HammockSway,
            QuestType.SwingRide, 0, "SWAY", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenHammockId);
        activity.EditorConfigureSwing(pivot, mount, seatPoint, 4.6f, 7f, 1.9f, 4f);
    }

    /// <summary>
    /// The towel folded across the lounger deck at an authored 0.300.
    ///
    /// Reuses <see cref="TowelNestActivity"/>, the routine written for the
    /// bathroom cabinet's niche: the cat climbs on and settles into the only
    /// soft thing on the product. The deck is deliberately bare apart from the
    /// towel, so the nest point is unambiguous.
    ///
    /// This lounger carries yaw 90 like the hammock but stands at x +2.55 where
    /// the courtyard is at -X, so its authored -Z front already faces the room
    /// and it stays OUT of `facesBackward`. Both points are negative.
    /// </summary>
    private static void AttachSunBaskActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredNest = .300f;
        const float authoredNestX = .210f;

        Transform floor = MakePoint(root, "NestFloorPoint", new Vector3(0f, 0f, -.44f));
        Transform nest = MakePoint(root, "NestPoint",
            new Vector3(-authoredNestX * scale, authoredNest * scale, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.60f));

        TowelNestActivity activity = root.AddComponent<TowelNestActivity>();
        activity.EditorConfigure(
            "sun-bask", "SUN LOUNGER", CatActivityKind.SunBask,
            QuestType.Sleep, 0, "BASK", 1.1f, 8f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenSunLoungerId);
        activity.EditorConfigureNest(floor, nest, 3.8f, 24f, 92f);
    }

    /// <summary>
    /// The lid seam at an authored 0.640, which is a cat's eyeline.
    ///
    /// Same routine as the refrigerator: the cat sits in front and stares at the
    /// thing it is not allowed to have. The look point is the seam and the
    /// thermometer beside it, not the vent at 1.05 — a cat that stares at the
    /// chimney reads as looking past the product.
    ///
    /// The grill IS in `facesBackward` (yaw 270 at x +2.65 would otherwise face
    /// the fence), so root space flips Z and not X: the authored kettle offset
    /// keeps its sign, the approach points do not.
    /// </summary>
    private static void AttachGrillWatchActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredKettleX = -.075f;
        const float authoredSeam = .640f;

        Transform look = MakePoint(root, "StareLookPoint",
            new Vector3(authoredKettleX * scale, authoredSeam * scale, .280f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .62f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "grill-watch", "GRILL", CatActivityKind.GrillWatch,
            QuestType.GardenWatch, 0, "STARE", 1.1f, 3f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenGrillId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.8f, "SMELLS GOOD!");
    }

    /// <summary>Bath rim at an authored 0.700, water just under it.</summary>
    private static void AttachBirdBathSipActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredRim = .700f;

        Transform floor = MakePoint(root, "SipFloorPoint", new Vector3(0f, 0f, -.46f));
        Transform perch = MakePoint(root, "SipPerchPoint",
            new Vector3(0f, authoredRim * scale, -.300f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.62f));

        SinkSipActivity activity = root.AddComponent<SinkSipActivity>();
        activity.EditorConfigure(
            "bird-bath-sip", "BIRD BATH", CatActivityKind.BirdBathSip,
            QuestType.Drink, 0, "DRINK", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenBirdBathId);
        activity.EditorConfigureSip(floor, perch, 2.4f, 45f, 96f);
    }

    /// <summary>
    /// The one pot that was left unplanted, authored at +X and landing at -X.
    ///
    /// <see cref="LitterDigActivity"/> needs somewhere to dig, which is why the
    /// group is two planted pots and one bare: plant all three and the product
    /// has a model but no routine.
    /// </summary>
    private static void AttachPotDigActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredPotX = .150f;
        const float authoredPotZ = -.140f;
        const float authoredSoil = .300f;

        Transform mouth = MakePoint(root, "DigMouthPoint",
            new Vector3(-authoredPotX * scale, 0f, -.44f));
        Transform dig = MakePoint(root, "DigPoint",
            new Vector3(-authoredPotX * scale, authoredSoil * scale,
                        authoredPotZ * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.60f));

        LitterDigActivity activity = root.AddComponent<LitterDigActivity>();
        activity.EditorConfigure(
            "pot-dig", "FLOWER POTS", CatActivityKind.PotDig,
            QuestType.LitterDig, 0, "DIG", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenFlowerPotsId);
        activity.EditorConfigureDig(mouth, dig, 2.6f, 4);
        BathroomActionPartsBuilder.Configure(root);
    }

    /// <summary>
    /// The flattened moss patch, authored at +X and landing at -X.
    ///
    /// Third reuse of <see cref="MatKneadActivity"/> after the bath mat and the
    /// kitchen runner. Those are flat textiles; here the pad is a patch of moss
    /// inside a raised bed, so the pad point sits at the bed's soil height and
    /// not on the floor.
    /// </summary>
    private static void AttachDaisyRollActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredPadX = .310f;
        const float authoredPadTop = .196f;

        Transform pad = MakePoint(root, "KneadPadPoint",
            new Vector3(-authoredPadX * scale, authoredPadTop * scale, 0f));
        Transform exit = MakePoint(root, "KneadExitPoint",
            new Vector3(-authoredPadX * scale, 0f, -.62f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-authoredPadX * scale, 0f, -.54f));

        MatKneadActivity activity = root.AddComponent<MatKneadActivity>();
        activity.EditorConfigure(
            "daisy-roll", "DAISY BED", CatActivityKind.DaisyRoll,
            QuestType.MatKnead, 0, "KNEAD", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenDaisyBedId);
        activity.EditorConfigureKnead(pad, exit, 6, .34f, 1.4f, 8f);
    }

    /// <summary>
    /// The bought yarn toy. The ball is a second single-object FBX because the
    /// routine hops it between points and the cat pounces on it — a ball baked
    /// into the nest mesh could not move.
    ///
    /// The free lawn toy in the Garden shell uses the same class with
    /// <see cref="CatActivityKind.BallChase"/>; this one gets its own kind, or a
    /// by-kind lookup would find whichever loaded first.
    /// </summary>
    private static void AttachYarnBallChaseActivity(
        GameObject root, GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject ballAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "GardenYarnBallBall_Premium.fbx");
        if (ballAsset == null)
            return;

        float scale = ReadModelScale(visual);

        GameObject ball = PrefabUtility.InstantiatePrefab(ballAsset) as GameObject;
        if (ball == null)
            return;
        ball.name = "YarnBall";
        ball.transform.SetParent(visual.transform, false);
        ball.transform.localPosition = Vector3.zero;
        ball.transform.localRotation = Quaternion.identity;
        ball.transform.localScale = Vector3.one * scale;
        RemoveChildColliders(ball);
        ReplaceMaterials(ball, materials);

        var hops = new Transform[3];
        hops[0] = MakePoint(root, "YarnHopPoint_1", new Vector3(.52f, .10f, -.46f));
        hops[1] = MakePoint(root, "YarnHopPoint_2", new Vector3(-.46f, .10f, -.58f));
        hops[2] = MakePoint(root, "YarnHopPoint_3", new Vector3(.06f, .10f, -.84f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.52f));

        GardenYarnChaseActivity activity = root.AddComponent<GardenYarnChaseActivity>();
        activity.EditorConfigure(
            "yarn-ball-chase", "YARN BALL", CatActivityKind.YarnBallChase,
            QuestType.PlayBall, 0, "PLAY YARN", 1.7f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.GardenYarnBallId);
        activity.EditorConfigureYarn(ball.transform, hops, 4);
    }


    private static bool TryBuildPremiumKitchenHeroPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        bool isHero =
            definition.ProductId == HomeStoreService.KitchenIslandId ||
            definition.ProductId == HomeStoreService.KitchenRefrigeratorId ||
            definition.ProductId == HomeStoreService.KitchenStoveOvenId ||
            definition.ProductId == HomeStoreService.KitchenPantryShelfId ||
            definition.ProductId == HomeStoreService.KitchenSinkCabinetId ||
            definition.ProductId == HomeStoreService.KitchenDishCartId ||
            definition.ProductId == HomeStoreService.KitchenCounterStoolId ||
            definition.ProductId == HomeStoreService.KitchenFruitBasketId ||
            definition.ProductId == HomeStoreService.KitchenFeedingStationId ||
            definition.ProductId == HomeStoreService.KitchenPawMatId;
        if (!isHero)
            return false;

        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            if (!TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
                return false;
            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            AttachKitchenActivity(root, visual, definition);
            PrefabUtility.SaveAsPrefabAsset(
                root, PrefabFolder + "/" + definition.PrefabName + ".prefab");
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AttachKitchenActivity(
        GameObject root, GameObject visual, StoreCatalogAsset definition)
    {
        string id = definition.ProductId;
        if (id == HomeStoreService.KitchenIslandId)
            AttachIslandPerchActivity(root, visual);
        else if (id == HomeStoreService.KitchenCounterStoolId)
            AttachStoolPerchActivity(root, visual);
        else if (id == HomeStoreService.KitchenRefrigeratorId)
            AttachFridgeStareActivity(root, visual);
        else if (id == HomeStoreService.KitchenFruitBasketId)
            AttachFruitSwatActivity(root, visual);
        else if (id == HomeStoreService.KitchenPantryShelfId)
            AttachPantryClimbActivity(root, visual);
        else if (id == HomeStoreService.KitchenStoveOvenId)
            AttachOvenWarmthActivity(root, visual);
        else if (id == HomeStoreService.KitchenDishCartId)
            AttachCartNudgeActivity(root, visual);
        else if (id == HomeStoreService.KitchenFeedingStationId)
            AttachMealTimeActivity(root, visual);
        else if (id == HomeStoreService.KitchenSinkCabinetId)
            AttachKitchenSipActivity(root, visual);
        else if (id == HomeStoreService.KitchenPawMatId)
            AttachKitchenMatKneadActivity(root, visual);
    }

    /// <summary>Island counter at an authored 0.86, clear stretch at +X.</summary>
    private static void AttachIslandPerchActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredCounterTop = .860f;
        const float authoredPerchX = .640f;

        float perchX = -authoredPerchX * scale;
        Transform floor = MakePoint(root, "PerchFloorPoint",
            new Vector3(perchX, 0f, -.92f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(perchX, authoredCounterTop * scale, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(perchX, 0f, -1.08f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "island-perch", "KITCHEN ISLAND", CatActivityKind.IslandPerch,
            QuestType.Sleep, 0, "PERCH", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenIslandId);
        activity.EditorConfigurePerch(floor, perch, 3.0f, 20f, 94f, "BEST SEAT!");
    }

    /// <summary>Stool seat at an authored 0.56.</summary>
    private static void AttachStoolPerchActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredSeatTop = .560f;

        Transform floor = MakePoint(root, "PerchFloorPoint", new Vector3(0f, 0f, -.66f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(0f, authoredSeatTop * scale, -.030f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.80f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "stool-perch", "COUNTER STOOL", CatActivityKind.StoolPerch,
            QuestType.Sleep, 0, "PERCH", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenCounterStoolId);
        activity.EditorConfigurePerch(floor, perch, 2.6f, 14f, 94f, "MY STOOL!");
    }

    /// <summary>
    /// The fridge door is a flat 2.25 face far above the cat, which is the same
    /// beat as the bathroom mirror: sit, look up, paw at it. Reuses the shared
    /// <see cref="SitLookActivity"/> rather than adding a class.
    /// </summary>
    private static void AttachFridgeStareActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);

        Transform look = MakePoint(root, "StareLookPoint",
            new Vector3(0f, 1.050f * scale, -.360f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.90f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "fridge-stare", "REFRIGERATOR", CatActivityKind.FridgeStare,
            QuestType.KitchenWatch, 0, "STARE", 1.1f, 3f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenRefrigeratorId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.8f, "OPEN IT!");
    }

    /// <summary>Fruit heaped proud of the upper tier: a swat, not a climb.</summary>
    private static void AttachFruitSwatActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredFruitTop = .720f;

        Transform look = MakePoint(root, "SwatLookPoint",
            new Vector3(0f, authoredFruitTop * scale, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.72f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "fruit-swat", "FRUIT BASKET", CatActivityKind.FruitSwat,
            QuestType.KitchenWatch, 0, "SWAT", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenFruitBasketId);
        activity.EditorConfigureLook(look, SitLookReaction.PawSwat, 2.4f, "GOT IT!");
    }

    /// <summary>Two staged hops, onto the authored 0.520 and 0.930 shelves.</summary>
    private static void AttachPantryClimbActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredLowerShelf = .558f;
        const float authoredUpperShelf = .968f;

        Transform floor = MakePoint(root, "ClimbFloorPoint", new Vector3(0f, 0f, -.86f));
        Transform lower = MakePoint(root, "ClimbLowerPoint",
            new Vector3(-.220f * scale, authoredLowerShelf * scale, -.020f * scale));
        Transform upper = MakePoint(root, "ClimbUpperPoint",
            new Vector3(.180f * scale, authoredUpperShelf * scale, -.020f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -1.00f));

        PantryClimbActivity activity = root.AddComponent<PantryClimbActivity>();
        activity.EditorConfigure(
            "pantry-climb", "PANTRY SHELF", CatActivityKind.PantryClimb,
            QuestType.PantryClimb, 0, "CLIMB", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenPantryShelfId);
        activity.EditorConfigureClimb(floor, lower, upper, 2.4f);
    }

    /// <summary>Basking spot on the floor in front of the oven glass.</summary>
    private static void AttachOvenWarmthActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredDoorMid = .425f;

        Transform bask = MakePoint(root, "BaskPoint", new Vector3(0f, 0f, -.70f));
        Transform door = MakePoint(root, "BaskDoorPoint",
            new Vector3(0f, authoredDoorMid * scale, -.335f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.86f));

        OvenWarmthActivity activity = root.AddComponent<OvenWarmthActivity>();
        activity.EditorConfigure(
            "oven-warmth", "STOVE OVEN", CatActivityKind.OvenWarmth,
            QuestType.Sleep, 0, "WARM UP", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenStoveOvenId);
        activity.EditorConfigureBask(bask, door, 4.2f, 30f, 92f);
    }

    /// <summary>
    /// The cart is the only Kitchen product that moves, and the whole thing
    /// rolls, so the routine drives VisualContent itself — no second FBX and no
    /// pivot, unlike the toilet paper roll.
    /// </summary>
    private static void AttachCartNudgeActivity(GameObject root, GameObject visual)
    {
        Transform shove = MakePoint(root, "NudgeShovePoint", new Vector3(-.62f, 0f, -.36f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-.76f, 0f, -.46f));

        CartNudgeActivity activity = root.AddComponent<CartNudgeActivity>();
        activity.EditorConfigure(
            "cart-nudge", "DISH CART", CatActivityKind.CartNudge,
            QuestType.CartNudge, 0, "PUSH", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenDishCartId);
        activity.EditorConfigureNudge(shove, visual.transform, Vector3.right, .30f, 2);
    }

    /// <summary>Food bowl authored at -X, so it lands at +X.</summary>
    private static void AttachMealTimeActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredFoodX = -.230f;
        const float authoredBowlRim = .250f;

        float bowlX = -authoredFoodX * scale;
        Transform stand = MakePoint(root, "MealStandPoint",
            new Vector3(bowlX, 0f, -.58f));
        Transform bowl = MakePoint(root, "MealBowlPoint",
            new Vector3(bowlX, authoredBowlRim * scale, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(bowlX, 0f, -.72f));

        MealTimeActivity activity = root.AddComponent<MealTimeActivity>();
        activity.EditorConfigure(
            "meal-time", "FEEDING STATION", CatActivityKind.MealTime,
            QuestType.Eat, 0, "EAT", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenFeedingStationId);
        activity.EditorConfigureMeal(stand, bowl, 3.0f, 40f, 96f);
    }

    /// <summary>
    /// The kitchen sink is the room's second water source, and the bathroom
    /// vanity already owns that beat exactly — reuses
    /// <see cref="SinkSipActivity"/> rather than adding a class.
    /// </summary>
    private static void AttachKitchenSipActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredCounterTop = 1.010f;

        Transform floor = MakePoint(root, "SipFloorPoint", new Vector3(0f, 0f, -.80f));
        Transform perch = MakePoint(root, "SipPerchPoint",
            new Vector3(0f, authoredCounterTop * scale, -.150f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.94f));

        SinkSipActivity activity = root.AddComponent<SinkSipActivity>();
        activity.EditorConfigure(
            "kitchen-sip", "KITCHEN SINK", CatActivityKind.KitchenSip,
            QuestType.Drink, 0, "DRINK", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenSinkCabinetId);
        activity.EditorConfigureSip(floor, perch, 2.4f, 42f, 96f);
    }

    /// <summary>Runner pad authored at +X, landing at -X.</summary>
    private static void AttachKitchenMatKneadActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredPadCentreX = .560f;
        const float authoredPadTop = .080f;

        float padX = -authoredPadCentreX * scale;
        Transform pad = MakePoint(root, "KneadPadPoint",
            new Vector3(padX, authoredPadTop * scale, 0f));
        Transform exit = MakePoint(root, "KneadExitPoint",
            new Vector3(padX, 0f, -.95f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(padX, 0f, -.82f));

        MatKneadActivity activity = root.AddComponent<MatKneadActivity>();
        activity.EditorConfigure(
            "kitchen-mat-knead", "KITCHEN RUNNER", CatActivityKind.KitchenMatKnead,
            QuestType.MatKnead, 0, "KNEAD", 1.1f, 3f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.KitchenPawMatId);
        activity.EditorConfigureKnead(pad, exit, 6, .34f, 1.4f, 8f);
    }

    private static bool TryBuildPremiumBathroomHeroPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        bool isHero =
            definition.ProductId == HomeStoreService.BathroomTubId ||
            definition.ProductId == HomeStoreService.BathroomVanityId ||
            definition.ProductId == HomeStoreService.BathroomToiletId ||
            definition.ProductId == HomeStoreService.BathroomShowerId ||
            definition.ProductId == HomeStoreService.BathroomTowelStorageId ||
            definition.ProductId == HomeStoreService.BathroomLitterBoxId ||
            definition.ProductId == HomeStoreService.BathroomGroomingCartId ||
            definition.ProductId == HomeStoreService.BathroomLaundryHamperId ||
            definition.ProductId == HomeStoreService.BathroomMirrorId ||
            definition.ProductId == HomeStoreService.BathroomBathMatId;
        if (!isHero)
            return false;

        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            if (!TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
                return false;
            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            if (definition.ProductId == HomeStoreService.BathroomShowerId)
                AttachShowerRinseActivity(root, visual);
            if (definition.ProductId == HomeStoreService.BathroomVanityId)
                AttachSinkSipActivity(root, visual);
            if (definition.ProductId == HomeStoreService.BathroomToiletId)
                AttachPaperSpinActivity(root, visual, materials);
            if (definition.ProductId == HomeStoreService.BathroomTowelStorageId)
                AttachTowelNestActivity(root, visual);
            if (definition.ProductId == HomeStoreService.BathroomLitterBoxId)
                AttachLitterDigActivity(root, visual);
            if (definition.ProductId == HomeStoreService.BathroomGroomingCartId)
                AttachGroomBrushActivity(root, visual);
            if (definition.ProductId == HomeStoreService.BathroomLaundryHamperId)
                AttachHamperDiveActivity(root, visual);
            // The bathroom wall mirror is decoration only.
            if (definition.ProductId == HomeStoreService.BathroomBathMatId)
                AttachMatKneadActivity(root, visual);
            if (definition.ProductId == HomeStoreService.BathroomTubId)
                AttachTubEdgeWalkActivity(root, visual);
            PrefabUtility.SaveAsPrefabAsset(
                root, PrefabFolder + "/" + definition.PrefabName + ".prefab");
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// The shower cabin is open at the front, so like the star tipi its product
    /// box turns into a trigger and a scripted rinse walks the cat inside. The
    /// tray lip alone (0.12 against a 0.005 step offset) would stop the cat.
    /// </summary>
    private static void AttachShowerRinseActivity(GameObject root, GameObject visual)
    {
        BoxCollider showerCollider = visual.GetComponent<BoxCollider>();
        if (showerCollider != null)
            showerCollider.isTrigger = true;

        // Root local space is the mesh mirrored: the model is authored with its
        // open front at -Z and the model child carries the 180 turn, so the way
        // in is +Z here, offset to the walk-in half of the front.
        Transform door = new GameObject("RinseDoorPoint").transform;
        door.SetParent(root.transform, false);
        door.localPosition = new Vector3(-.33f, 0f, .95f);
        Transform stand = new GameObject("RinseStandPoint").transform;
        stand.SetParent(root.transform, false);
        stand.localPosition = new Vector3(-.30f, .125f, .05f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(-.33f, 0f, 1.05f);

        ShowerRinseActivity activity = root.AddComponent<ShowerRinseActivity>();
        activity.EditorConfigure(
            "shower-rinse",
            "RAINBOW SHOWER",
            CatActivityKind.ShowerRinse,
            QuestType.ShowerRinse,
            0,
            "RINSE",
            1.1f,
            6f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomShowerId);
        activity.EditorConfigureRinse(door, stand, 2.6f, .65f, 6f);
        Transform outlet = MakePoint(root, "RinseWaterOutlet", new Vector3(0, 1.6975694f, .0262041f));
        activity.EditorConfigureWaterOutlet(outlet);
    }

    /// <summary>
    /// The vanity counter is a perch, not a room the cat walks into, so the
    /// product box stays solid: the sip lifts the cat over it. Anchors are in
    /// root space, which is the mesh mirrored (the model is authored front at
    /// -Z and the model child carries the 180 turn) and scaled by the 0.96
    /// FitFixtureModel factor, so the counter at model y 0.86 is root y 0.826.
    /// </summary>
    private static void AttachSinkSipActivity(GameObject root, GameObject visual)
    {
        // FitFixtureModel's factor is read off the built model rather than
        // hardcoded, so the perch keeps sitting exactly on the counter if the
        // mesh is re-exported at a slightly different size.
        Renderer modelRenderer = visual.GetComponentInChildren<Renderer>(true);
        float modelScale = modelRenderer != null
            ? modelRenderer.transform.localScale.x
            : 1f;
        const float authoredCounterTop = .860f;

        Transform floor = new GameObject("SipFloorPoint").transform;
        floor.SetParent(root.transform, false);
        floor.localPosition = new Vector3(-.058f, 0f, .720f);
        Transform perch = new GameObject("SipPerchPoint").transform;
        perch.SetParent(root.transform, false);
        perch.localPosition = new Vector3(
            -.058f, authoredCounterTop * modelScale, .317f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(-.058f, 0f, .860f);

        SinkSipActivity activity = root.AddComponent<SinkSipActivity>();
        activity.EditorConfigure(
            "sink-sip",
            "VANITY TAP",
            CatActivityKind.SinkSip,
            QuestType.Drink,
            0,
            "DRINK",
            1.1f,
            4f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomVanityId);
        activity.EditorConfigureSip(floor, perch, 2.4f, 45f, 96f);
    }

    /// <summary>
    /// The tub had a premium model but no beat of its own, and the shower next
    /// to it already owns getting wet — so this one is balance: the cat walks
    /// the rim and paws at the water.
    ///
    /// The tub IS in `facesBackward`, unlike the five props added alongside it,
    /// so the mapping here is the other one: the model child's 180 cancels the
    /// FBX import's X mirror and only **Z** flips. Every number below is written
    /// straight in root space and measured off the built prefab, which sidesteps
    /// the question entirely.
    /// </summary>
    private static void AttachTubEdgeWalkActivity(GameObject root, GameObject visual)
    {
        Bounds bounds = CalculateRendererBounds(visual);
        // Profiled off the built prefab in root space, band by band, rather than
        // derived from bounds. The tub is NOT symmetric front to back: the tall
        // backrest is at root -Z and tops out at 1.08, the water sits at 0.64,
        // and the low rim the cat can actually walk is at root +Z, 0.744 up and
        // only 0.22 wide. Taking half the depth off the max corner, which is
        // what the first pass did, put the cat on the backrest with its face in
        // the wall.
        const float rimY = .744f;
        const float rimZ = .550f;
        const float waterY = .640f;
        float halfRun = bounds.size.x * .5f - .300f;

        Transform floor = MakePoint(root, "EdgeFloorPoint", new Vector3(0f, 0f, rimZ + .55f));
        Transform rimStart = MakePoint(root, "EdgeRimStartPoint",
            new Vector3(-halfRun, rimY, rimZ));
        Transform rimEnd = MakePoint(root, "EdgeRimEndPoint",
            new Vector3(halfRun, rimY, rimZ));
        Transform water = MakePoint(root, "EdgeWaterPoint", new Vector3(0f, waterY, .050f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, rimZ + .70f));

        TubEdgeWalkActivity activity = root.AddComponent<TubEdgeWalkActivity>();
        activity.EditorConfigure(
            "tub-edge-walk",
            "BATH TUB",
            CatActivityKind.TubEdgeWalk,
            QuestType.TubEdgeWalk,
            0,
            "BALANCE",
            1.1f,
            5f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomTubId);
        activity.EditorConfigureEdge(floor, rimStart, rimEnd, water, 1.8f, 3);
    }

    /// <summary>
    /// Reads FitFixtureModel's factor off the built model rather than assuming
    /// 0.96, because the factor is 0.96 of the TIGHTEST catalog ratio and the
    /// bathroom's five props do not all author to an exact contract.
    /// </summary>
    private static float ReadModelScale(GameObject visual)
    {
        Renderer modelRenderer = visual.GetComponentInChildren<Renderer>(true);
        return modelRenderer != null ? modelRenderer.transform.localScale.x : 1f;
    }

    /// <summary>
    /// Creates a point under `root`. Callers mirror authored X themselves,
    /// because none of these products is in `facesBackward` and nothing cancels
    /// the FBX import's X mirror: an authored (x, y, z) lands at (-x, y, z).
    /// </summary>
    private static Transform MakePoint(GameObject root, string name, Vector3 local)
    {
        Transform point = new GameObject(name).transform;
        point.SetParent(root.transform, false);
        point.localPosition = local;
        return point;
    }

    /// <summary>
    /// The litter tray is a walk-in: its 0.15 sill sits well above the
    /// CharacterController's step offset, so like the shower and the star tipi
    /// the product box becomes a trigger and the routine walks the cat in.
    /// </summary>
    private static void AttachLitterDigActivity(GameObject root, GameObject visual)
    {
        BoxCollider box = visual.GetComponent<BoxCollider>();
        if (box != null)
            box.isTrigger = true;

        float scale = ReadModelScale(visual);
        const float authoredLitterTop = .190f;

        Transform mouth = MakePoint(root, "DigMouthPoint", new Vector3(0f, 0f, -.78f));
        Transform dig = MakePoint(root, "DigPoint",
            new Vector3(0f, authoredLitterTop * scale, .050f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.95f));

        LitterDigActivity activity = root.AddComponent<LitterDigActivity>();
        activity.EditorConfigure(
            "litter-dig",
            "LITTER TRAY",
            CatActivityKind.LitterDig,
            QuestType.LitterDig,
            0,
            "DIG",
            1.1f,
            3f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomLitterBoxId);
        activity.EditorConfigureDig(mouth, dig, 2.6f, 4);
    }

    /// <summary>
    /// Nothing on the grooming cart moves, so the whole beat is the cat: it
    /// stands side-on to the front roller and drags along it. The rub line runs
    /// in X, which is the axis the FBX import mirrors — the two ends swap sides,
    /// and since the routine only needs the two ends of one line that is free.
    /// </summary>
    private static void AttachGroomBrushActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);

        Transform approach = MakePoint(root, "GroomApproachPoint",
            new Vector3(0f, 0f, -.80f));
        Transform rubStart = MakePoint(root, "GroomRubStartPoint",
            new Vector3(.200f * scale, 0f, -.500f * scale));
        Transform rubEnd = MakePoint(root, "GroomRubEndPoint",
            new Vector3(-.200f * scale, 0f, -.500f * scale));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.78f));

        GroomBrushActivity activity = root.AddComponent<GroomBrushActivity>();
        activity.EditorConfigure(
            "groom-brush",
            "GROOMING CART",
            CatActivityKind.GroomBrush,
            QuestType.Groom,
            0,
            "GROOM",
            1.1f,
            4f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomGroomingCartId);
        activity.EditorConfigureGroom(approach, rubStart, rubEnd, 3, 0.8f);
    }

    /// <summary>
    /// The hamper rim is 0.78 up once the catalog has scaled the basket, so the
    /// cat is hopped in like the vanity counter rather than walked in, and the
    /// product box stays solid.
    /// </summary>
    private static void AttachHamperDiveActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredLaundryTop = .900f;

        Transform floor = MakePoint(root, "DiveFloorPoint", new Vector3(0f, 0f, -.72f));
        Transform pile = MakePoint(root, "DivePilePoint",
            new Vector3(0f, authoredLaundryTop * scale, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.88f));

        HamperDiveActivity activity = root.AddComponent<HamperDiveActivity>();
        activity.EditorConfigure(
            "hamper-dive",
            "LAUNDRY HAMPER",
            CatActivityKind.HamperDive,
            QuestType.HamperDive,
            0,
            "DIVE",
            1.1f,
            5f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomLaundryHamperId);
        activity.EditorConfigureDive(floor, pile, .26f, 1.5f, 12f);
    }

    /// <summary>
    /// The mirror hangs at 1.58, far above the cat, so this is a sit-and-look
    /// like the window and the garden birds rather than a new routine — the
    /// shared <see cref="SitLookActivity"/> already does exactly this beat, with
    /// a paw swat at the reflection.
    ///
    /// Every point under this root is lifted to hungHeight along with the
    /// product. That is harmless: SitLookActivity flattens the sit point to the
    /// cat's own y, and CatActivity.DistanceTo ignores y as well.
    /// </summary>
    private static void AttachMirrorGazeActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredGlassMid = .458f;

        Transform look = MakePoint(root, "GazeLookPoint",
            new Vector3(0f, authoredGlassMid * scale, -.060f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.85f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "mirror-gaze",
            "WALL MIRROR",
            CatActivityKind.MirrorGaze,
            QuestType.MirrorGaze,
            0,
            "LOOK",
            1.1f,
            3f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomMirrorId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.6f, "WHO IS THAT?");
    }

    /// <summary>
    /// The mat is 0.08 tall, so there is nothing to climb or enter and the beat
    /// is entirely the cat. The knead pad is authored at +X and lands at -X.
    /// </summary>
    private static void AttachMatKneadActivity(GameObject root, GameObject visual)
    {
        float scale = ReadModelScale(visual);
        const float authoredPadCentreX = .330f;
        const float authoredPadTop = .080f;

        float padX = -authoredPadCentreX * scale;
        Transform pad = MakePoint(root, "KneadPadPoint",
            new Vector3(padX, authoredPadTop * scale, 0f));
        Transform exit = MakePoint(root, "KneadExitPoint",
            new Vector3(padX, 0f, -.95f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(padX, 0f, -.82f));

        MatKneadActivity activity = root.AddComponent<MatKneadActivity>();
        activity.EditorConfigure(
            "mat-knead",
            "BATH MAT",
            CatActivityKind.MatKnead,
            QuestType.MatKnead,
            0,
            "KNEAD",
            1.1f,
            3f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomBathMatId);
        activity.EditorConfigureKnead(pad, exit, 6, .34f, 1.4f, 8f);
    }

    /// <summary>
    /// The towel cabinet's niche is an open bay with a folded towel stack at
    /// 0.900 in authoring space, so the cat is hopped onto it like the vanity
    /// counter rather than walked in like the tipi.
    ///
    /// Root space here is NOT the toilet's. This product stands on the left
    /// wall, so it is absent from `facesBackward` and the model child carries no
    /// 180 to cancel the FBX import's X mirror: an authored point (x, y, z)
    /// lands at (-x, y, z) * scale. Z is not flipped, so the room side — root -Z
    /// — is still where the niche opening faces.
    /// </summary>
    private static void AttachTowelNestActivity(GameObject root, GameObject visual)
    {
        // FitFixtureModel's factor is read off the built model rather than
        // hardcoded, so the nest keeps sitting on the stack if the mesh is
        // re-exported at a slightly different size.
        Renderer modelRenderer = visual.GetComponentInChildren<Renderer>(true);
        float modelScale = modelRenderer != null
            ? modelRenderer.transform.localScale.x
            : 1f;
        const float authoredBedCentreX = .150f;
        const float authoredBedTop = .900f;

        float nestX = -authoredBedCentreX * modelScale;

        Transform floor = new GameObject("NestFloorPoint").transform;
        floor.SetParent(root.transform, false);
        floor.localPosition = new Vector3(nestX, 0f, -.78f);
        Transform nest = new GameObject("NestPoint").transform;
        nest.SetParent(root.transform, false);
        nest.localPosition = new Vector3(
            nestX, authoredBedTop * modelScale, -.020f * modelScale);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(nestX, 0f, -.94f);

        TowelNestActivity activity = root.AddComponent<TowelNestActivity>();
        activity.EditorConfigure(
            "towel-nest",
            "TOWEL NICHE",
            CatActivityKind.TowelNest,
            QuestType.Sleep,
            0,
            "NAP",
            1.1f,
            0f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomTowelStorageId);
        activity.EditorConfigureNest(floor, nest, 3.4f, 26f, 92f);
    }

    /// <summary>
    /// The paper roll is its own single-object FBX on the toilet's origin, so it
    /// can hang under a spin pivot. Root space is the mesh mirrored (front
    /// authored at -Z, 180 on the model child) and scaled by FitFixtureModel, so
    /// a model point (x, y, z) lands at (-x, y, -z) * scale.
    /// </summary>
    private static void AttachPaperSpinActivity(
        GameObject root,
        GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject rollAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "BathroomToiletRoll_Premium.fbx");
        if (rollAsset == null)
            return;

        Renderer modelRenderer = visual.GetComponentInChildren<Renderer>(true);
        if (modelRenderer == null)
            return;
        float modelScale = modelRenderer.transform.localScale.x;

        // The roll axis as authored in build_bathroom_toilet.py. Measured, not
        // assumed: the FBX import mirrors X and the model child's 180 mirrors it
        // back, so only Z flips between authoring space and root space.
        var authoredAxis = new Vector3(-.375f, .760f, -.245f);
        var axis = new Vector3(
            authoredAxis.x * modelScale,
            authoredAxis.y * modelScale,
            -authoredAxis.z * modelScale);

        Transform pivot = new GameObject("RollPivot").transform;
        pivot.SetParent(visual.transform, false);
        pivot.localPosition = axis;

        GameObject roll = PrefabUtility.InstantiatePrefab(rollAsset) as GameObject;
        if (roll == null)
            return;
        roll.name = "ToiletPaperRoll";
        roll.transform.SetParent(pivot, false);
        // The roll mesh is authored on the toilet's origin, so it has to be
        // pushed back by the pivot offset: leaving it at zero would place the
        // roll axis at twice the offset.
        roll.transform.localPosition = -axis;
        roll.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        roll.transform.localScale = Vector3.one * modelScale;
        RemoveChildColliders(roll);
        ReplaceMaterials(roll, materials);

        // The room side is root +Z: the front is authored at -Z and the model
        // child carries the 180.
        Transform swat = new GameObject("SwatPoint").transform;
        swat.SetParent(root.transform, false);
        swat.localPosition = new Vector3(axis.x - .10f, 0f, axis.z + .50f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(axis.x - .14f, 0f, axis.z + .68f);

        PaperSpinActivity activity = root.AddComponent<PaperSpinActivity>();
        activity.EditorConfigure(
            "paper-spin",
            "PAPER ROLL",
            CatActivityKind.PaperSpin,
            QuestType.PaperSpin,
            0,
            "SWAT",
            1.1f,
            5f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BathroomToiletId);
        activity.EditorConfigureSpin(pivot, swat, 3, 620f, 1.15f);
        BathroomActionPartsBuilder.Configure(root);
    }

    private static bool TryBuildPremiumRoomProductPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            if (!TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
                return false;

            // Wall art hangs through VisualOffset, so the premium mesh needs the
            // same lift the pack path applies. The collider stays on VisualContent.
            if (definition.VisualOffset != Vector3.zero)
            {
                for (int i = 0; i < visual.transform.childCount; i++)
                    visual.transform.GetChild(i).localPosition = definition.VisualOffset;
            }

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            PrefabUtility.SaveAsPrefabAsset(
                root, PrefabFolder + "/" + definition.PrefabName + ".prefab");
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool TryGetBathroomFixtureSource(string productId, out string path)
    {
        if (productId == HomeStoreService.BathroomTubId)
            path = "Assets/Art/Bathroom/Models/BathroomTub.fbx";
        else if (productId == HomeStoreService.BathroomVanityId)
            path = "Assets/Art/Bathroom/Models/BathroomVanitySink.fbx";
        else if (productId == HomeStoreService.BathroomToiletId)
            path = "Assets/Art/Bathroom/Models/BathroomToilet.fbx";
        else if (productId == HomeStoreService.BathroomShowerId)
            path = "Assets/Art/Bathroom/Models/BathroomShower.fbx";
        else
        {
            path = null;
            return false;
        }
        return true;
    }

    private static void BuildBathroomFixturePrefab(
        StoreCatalogAsset definition,
        string sourcePath)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
        if (source == null)
            throw new InvalidOperationException("Bathroom fixture model is missing: " + sourcePath);

        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (model == null)
                throw new InvalidOperationException("Could not instantiate " + sourcePath);
            model.name = definition.PrefabName + "_Model";
            model.transform.SetParent(visual.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            model.transform.localScale = Vector3.one;
            RemoveChildColliders(model);
            FitFixtureModel(model, definition);

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void FitFixtureModel(GameObject model, StoreCatalogAsset definition)
    {
        Bounds bounds = CalculateRendererBounds(model);
        Vector3 size = bounds.size;
        float scale = Mathf.Min(
            definition.Footprint.x / Mathf.Max(.001f, size.x),
            Mathf.Min(
                definition.Height / Mathf.Max(.001f, size.y),
                definition.Footprint.y / Mathf.Max(.001f, size.z)));
        model.transform.localScale = Vector3.one * scale * .96f;
        bounds = CalculateRendererBounds(model);
        model.transform.localPosition += new Vector3(
            -bounds.center.x,
            -bounds.min.y,
            -bounds.center.z);
    }

    private static Bounds CalculateRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException(root.name + " has no renderer.");
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void RemoveChildColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            UnityEngine.Object.DestroyImmediate(colliders[i]);
    }

    private static void BuildProceduralBathroomPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (definition.ProductId == HomeStoreService.BathroomBathMatId)
                BuildBathMatVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BathroomLaundryHamperId)
                BuildLaundryHamperVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BathroomLitterBoxId)
                BuildLitterBoxVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BathroomGroomingCartId)
                BuildGroomingCartVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BathroomTowelStorageId)
                BuildTowelStorageVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BathroomMirrorId)
                BuildBathroomMirrorVisual(visual.transform, materials);
            else
                throw new InvalidOperationException(
                    "No Bathroom procedural visual for " + definition.ProductId);

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildBathMatVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "AquaMat", PrimitiveType.Cube,
            new Vector3(0f, .035f, 0f), Quaternion.identity,
            new Vector3(1.9f, .07f, 1.15f), materials["CH_TealLight"]);
        AddPrimitivePart(parent, "PawPad", PrimitiveType.Sphere,
            new Vector3(0f, .082f, -.08f), Quaternion.identity,
            new Vector3(.48f, .045f, .38f), materials["CH_Cream"]);
        for (int i = 0; i < 3; i++)
        {
            AddPrimitivePart(parent, "PawToe_" + (i + 1), PrimitiveType.Sphere,
                new Vector3((i - 1) * .25f, .084f, .28f + (i == 1 ? .06f : 0f)),
                Quaternion.identity, new Vector3(.22f, .045f, .25f),
                materials["CH_Cream"]);
        }
    }

    private static void BuildLaundryHamperVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "HamperBody", PrimitiveType.Cylinder,
            new Vector3(0f, .38f, 0f), Quaternion.identity,
            new Vector3(.78f, .76f, .78f), materials["CH_Pink"]);
        AddPrimitivePart(parent, "HamperInset", PrimitiveType.Cylinder,
            new Vector3(0f, .77f, 0f), Quaternion.identity,
            new Vector3(.66f, .035f, .66f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "MintTowel", PrimitiveType.Cylinder,
            new Vector3(-.15f, .85f, 0f), Quaternion.Euler(0f, 0f, 90f),
            new Vector3(.24f, .42f, .24f), materials["CH_TealLight"]);
        AddPrimitivePart(parent, "CreamTowel", PrimitiveType.Cylinder,
            new Vector3(.14f, .83f, .03f), Quaternion.Euler(0f, 0f, 90f),
            new Vector3(.22f, .38f, .22f), materials["CH_Cream"]);
    }

    private static void BuildLitterBoxVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Tray", PrimitiveType.Cube,
            new Vector3(0f, .12f, 0f), Quaternion.identity,
            new Vector3(1.3f, .24f, 1.02f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "CleanLitter", PrimitiveType.Cube,
            new Vector3(0f, .255f, -.03f), Quaternion.identity,
            new Vector3(1.02f, .07f, .72f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "BackLip", PrimitiveType.Cube,
            new Vector3(0f, .34f, .45f), Quaternion.identity,
            new Vector3(1.3f, .44f, .12f), materials["CH_Purple"]);
        AddPrimitivePart(parent, "FrontPaw", PrimitiveType.Sphere,
            new Vector3(0f, .27f, -.53f), Quaternion.identity,
            new Vector3(.28f, .08f, .12f), materials["CH_Cream"]);
    }

    private static void BuildGroomingCartVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "CartBody", PrimitiveType.Cube,
            new Vector3(0f, .46f, 0f), Quaternion.identity,
            new Vector3(.88f, .88f, .6f), materials["CH_Purple"]);
        AddPrimitivePart(parent, "GoldTop", PrimitiveType.Cube,
            new Vector3(0f, .94f, 0f), Quaternion.identity,
            new Vector3(.94f, .09f, .66f), materials["CH_Gold"]);
        for (int i = 0; i < 2; i++)
        {
            AddPrimitivePart(parent, "Drawer_" + (i + 1), PrimitiveType.Cube,
                new Vector3(0f, .3f + i * .31f, -.315f), Quaternion.identity,
                new Vector3(.7f, .23f, .04f), materials["CH_Cream"]);
        }
        AddPrimitivePart(parent, "AquaBottle", PrimitiveType.Cylinder,
            new Vector3(-.2f, 1.09f, 0f), Quaternion.identity,
            new Vector3(.16f, .28f, .16f), materials["CH_TealLight"]);
        AddPrimitivePart(parent, "CoralBottle", PrimitiveType.Cylinder,
            new Vector3(.08f, 1.06f, .03f), Quaternion.identity,
            new Vector3(.14f, .22f, .14f), materials["CH_OrangeLight"]);
    }

    private static void BuildTowelStorageVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "StorageBack", PrimitiveType.Cube,
            new Vector3(0f, .91f, .12f), Quaternion.identity,
            new Vector3(.98f, 1.82f, .22f), materials["CH_White"]);
        AddPrimitivePart(parent, "LilacFrameLeft", PrimitiveType.Cube,
            new Vector3(-.46f, .91f, -.04f), Quaternion.identity,
            new Vector3(.08f, 1.82f, .24f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "LilacFrameRight", PrimitiveType.Cube,
            new Vector3(.46f, .91f, -.04f), Quaternion.identity,
            new Vector3(.08f, 1.82f, .24f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "CoralTop", PrimitiveType.Cube,
            new Vector3(0f, 1.78f, -.04f), Quaternion.identity,
            new Vector3(.98f, .08f, .24f), materials["CH_CoralBright"]);
        Material[] towels =
        {
            materials["CH_OrangeLight"],
            materials["CH_TealLight"],
            materials["CH_Purple"]
        };
        for (int i = 0; i < 3; i++)
        {
            float y = .35f + i * .55f;
            AddPrimitivePart(parent, "GoldShelf_" + (i + 1), PrimitiveType.Cube,
                new Vector3(0f, y - .12f, -.08f), Quaternion.identity,
                new Vector3(1.02f, .08f, .48f), materials["CH_Gold"]);
            AddPrimitivePart(parent, "RolledTowel_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(0f, y + .08f, -.16f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(.3f, .55f, .3f), towels[i]);
        }
    }

    private static void BuildBathroomMirrorVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "GoldFrame", PrimitiveType.Cube,
            new Vector3(0f, .39f, 0f), Quaternion.identity,
            new Vector3(.9f, .78f, .08f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "AquaGlass", PrimitiveType.Cube,
            new Vector3(0f, .39f, -.03f), Quaternion.identity,
            new Vector3(.72f, .6f, .05f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "CreamInset", PrimitiveType.Cube,
            new Vector3(0f, .39f, -.045f), Quaternion.identity,
            new Vector3(.58f, .46f, .03f), materials["CH_White"]);
        AddPrimitivePart(parent, "PawCrest", PrimitiveType.Sphere,
            new Vector3(0f, .78f, -.02f), Quaternion.identity,
            new Vector3(.16f, .12f, .06f), materials["CH_CoralBright"]);
    }

    private static void BuildProceduralTunnelPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            BuildPlayTunnelVisual(visual.transform, materials);
            AddProductCollider(visual, definition);
            BoxCollider tunnelCollider = visual.GetComponent<BoxCollider>();
            if (tunnelCollider != null)
                tunnelCollider.isTrigger = true;
            ConfigureProductComponents(root, visual, definition);
            AttachTunnelActivity(root, visual.transform);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildPlayTunnelVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        Material[] rings =
        {
            materials["CH_CoralBright"], materials["CH_LemonBright"],
            materials["CH_MintBright"], materials["CH_AquaBright"],
            materials["CH_LilacBright"]
        };
        const int segments = 10;
        const float radius = 0.2f;
        for (int i = 0; i < rings.Length; i++)
        {
            float z = -0.38f + i * 0.18f;
            for (int s = 0; s < segments; s++)
            {
                float angle = s * (Mathf.PI * 2f / segments);
                AddPrimitivePart(
                    parent,
                    "TunnelRing_" + (i + 1) + "_" + (s + 1),
                    PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * radius, 0.22f + Mathf.Sin(angle) * radius, z),
                    Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg),
                    new Vector3(0.09f, 0.07f, 0.08f),
                    rings[i]);
            }
        }
        AddPrimitivePart(parent, "HangingToy", PrimitiveType.Sphere,
            new Vector3(0f, .48f, -.5f), Quaternion.identity,
            Vector3.one * .08f, materials["CH_LemonBright"]);
        AddPrimitivePart(parent, "ToyString", PrimitiveType.Cube,
            new Vector3(0f, .4f, -.5f), Quaternion.identity,
            new Vector3(.025f, .12f, .025f), materials["CH_Gold"]);
    }

    private static void AttachTunnelActivity(GameObject root, Transform visual)
    {
        Transform entrance = new GameObject("TunnelEntrance").transform;
        entrance.SetParent(root.transform, false);
        entrance.localPosition = new Vector3(0f, 0f, -.48f);
        Transform exit = new GameObject("TunnelExit").transform;
        exit.SetParent(root.transform, false);
        exit.localPosition = new Vector3(0f, 0f, .48f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(0f, 0f, -.62f);

        TunnelPlayActivity activity = root.AddComponent<TunnelPlayActivity>();
        activity.EditorConfigure(
            "play-tunnel",
            "PLAY TUNNEL",
            CatActivityKind.TunnelPlay,
            QuestType.TunnelPlay,
            0,
            "ZOOM",
            1.05f,
            6f,
            anchor,
            null,
            visual.gameObject);
        activity.EditorConfigureStoreProduct(HomeStoreService.PlayTunnelId);
        activity.EditorConfigureTunnel(entrance, exit, 1.28f);
    }

    public static string EnsureFeatherPlayActivity()
    {
        string prefabPath = PrefabFolder + "/FeatherToy.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            BuildProductAssetsSilently();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        if (prefab == null)
            return "feather-play-missing-prefab";

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.GetComponent<CatActivity>() == null)
            {
                Transform visual = root.transform.Find("VisualContent");
                AttachFeatherPlayActivity(root, visual != null ? visual : root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return "feather-play-added";
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return "feather-play-present";
    }

    private static void AttachFeatherPlayActivity(GameObject root, Transform visual)
    {
        if (root.GetComponent<SitLookActivity>() != null)
            return;

        Transform look = new GameObject("FeatherLookPoint").transform;
        look.SetParent(root.transform, false);
        look.localPosition = new Vector3(0f, 0.45f, 0f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(0f, 0f, -0.45f);

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "feather-play",
            "FEATHER TOY",
            CatActivityKind.FeatherPlay,
            QuestType.FeatherPlay,
            BondMilestoneService.FeatherPlayBond,
            "PLAY",
            1.05f,
            7f,
            anchor,
            null,
            null);
        activity.EditorConfigureStoreProduct(HomeStoreService.FeatherToyId);
        activity.EditorConfigureLook(
            look,
            SitLookReaction.PawSwat,
            2.1f,
            "BOING!");
    }

    private static void BuildProceduralBedroomPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
            {
                // Premium Blender hero asset connected; placement/economy stay shared.
            }
            else if (definition.ProductId == HomeStoreService.BedroomPawRugId)
                BuildBedroomPawRugVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomNightLightId)
                BuildBedroomNightLightVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomDreamArtId)
                BuildBedroomDreamArtVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomYarnBasketId)
                BuildBedroomYarnBasketVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomNightstandId)
                BuildBedroomNightstandVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomVanityStoolId)
                BuildBedroomVanityStoolVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomWardrobeId)
                BuildBedroomWardrobeVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomWindowDaybedId)
                BuildBedroomWindowDaybedVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomStarCanopyId)
                BuildBedroomStarCanopyVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BedroomQueenBedId)
                BuildBedroomQueenBedVisual(visual.transform, materials);
            else
                throw new InvalidOperationException(
                    "No Bedroom procedural visual for " + definition.ProductId);

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            if (definition.ProductId == HomeStoreService.BedroomStarCanopyId)
                AttachCanopyNapActivity(root, visual);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// The star tipi is enterable, so like the play tunnel its product box turns
    /// into a trigger and the cat is walked inside by a scripted nap activity.
    /// </summary>
    private static void AttachCanopyNapActivity(GameObject root, GameObject visual)
    {
        BoxCollider canopyCollider = visual.GetComponent<BoxCollider>();
        if (canopyCollider != null)
            canopyCollider.isTrigger = true;

        Transform door = new GameObject("NapDoorPoint").transform;
        door.SetParent(root.transform, false);
        door.localPosition = new Vector3(0f, 0f, -.72f);
        Transform nest = new GameObject("NapNestPoint").transform;
        nest.SetParent(root.transform, false);
        nest.localPosition = new Vector3(0f, 0f, .02f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(0f, 0f, -.95f);

        CanopyNapActivity activity = root.AddComponent<CanopyNapActivity>();
        activity.EditorConfigure(
            "canopy-nap",
            "STAR TIPI",
            CatActivityKind.CanopyNap,
            QuestType.Sleep,
            0,
            "NAP",
            1.1f,
            0f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomStarCanopyId);
        activity.EditorConfigureNap(door, nest, 3.2f, 22f, 92f);
    }

    private static void BuildBedroomPawRugVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "LilacRug", PrimitiveType.Cube,
            new Vector3(0f, .035f, 0f), Quaternion.identity,
            new Vector3(2.2f, .07f, 1.28f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "CreamInset", PrimitiveType.Cube,
            new Vector3(0f, .072f, 0f), Quaternion.identity,
            new Vector3(1.92f, .02f, 1.04f), materials["CH_White"]);
        AddHorizontalPaw(parent, new Vector3(0f, .09f, 0f), materials["CH_MintBright"]);
    }

    private static void BuildBedroomNightLightVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "MintBase", PrimitiveType.Cylinder,
            new Vector3(0f, .08f, 0f), Quaternion.identity,
            new Vector3(.28f, .16f, .28f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "Moon", PrimitiveType.Sphere,
            new Vector3(0f, .48f, 0f), Quaternion.identity,
            new Vector3(.42f, .42f, .16f), materials["CH_LemonBright"]);
        AddPrimitivePart(parent, "MoonCheek", PrimitiveType.Sphere,
            new Vector3(.12f, .46f, -.05f), Quaternion.identity,
            new Vector3(.16f, .18f, .08f), materials["CH_White"]);
    }

    private static void BuildBedroomDreamArtVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "GoldFrame", PrimitiveType.Cube,
            new Vector3(0f, .39f, 0f), Quaternion.identity,
            new Vector3(1.02f, .78f, .07f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "SkyPanel", PrimitiveType.Cube,
            new Vector3(0f, .39f, -.03f), Quaternion.identity,
            new Vector3(.86f, .62f, .04f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "Cloud", PrimitiveType.Sphere,
            new Vector3(-.16f, .28f, -.05f), Quaternion.identity,
            new Vector3(.34f, .18f, .06f), materials["CH_White"]);
        AddPrimitivePart(parent, "Star", PrimitiveType.Sphere,
            new Vector3(.2f, .52f, -.05f), Quaternion.identity,
            Vector3.one * .14f, materials["CH_LemonBright"]);
    }

    private static void BuildBedroomYarnBasketVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Basket", PrimitiveType.Cylinder,
            new Vector3(0f, .22f, 0f), Quaternion.identity,
            new Vector3(.36f, .44f, .36f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "YarnMint", PrimitiveType.Sphere,
            new Vector3(-.1f, .46f, -.04f), Quaternion.identity,
            Vector3.one * .22f, materials["CH_MintBright"]);
        AddPrimitivePart(parent, "YarnLilac", PrimitiveType.Sphere,
            new Vector3(.12f, .48f, .06f), Quaternion.identity,
            Vector3.one * .2f, materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "YarnLemon", PrimitiveType.Sphere,
            new Vector3(0f, .58f, -.02f), Quaternion.identity,
            Vector3.one * .16f, materials["CH_LemonBright"]);
    }

    private static void BuildBedroomNightstandVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "PeachBody", PrimitiveType.Cube,
            new Vector3(0f, .36f, 0f), Quaternion.identity,
            new Vector3(.68f, .72f, .5f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "GoldTop", PrimitiveType.Cube,
            new Vector3(0f, .74f, 0f), Quaternion.identity,
            new Vector3(.74f, .08f, .56f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Drawer", PrimitiveType.Cube,
            new Vector3(0f, .34f, -.26f), Quaternion.identity,
            new Vector3(.5f, .22f, .04f), materials["CH_White"]);
        AddPrimitivePart(parent, "PawKnob", PrimitiveType.Sphere,
            new Vector3(0f, .34f, -.29f), Quaternion.identity,
            Vector3.one * .08f, materials["CH_LilacBright"]);
    }

    private static void BuildBedroomVanityStoolVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "AquaSeat", PrimitiveType.Cylinder,
            new Vector3(0f, .4f, 0f), Quaternion.identity,
            new Vector3(.4f, .12f, .4f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "CreamCushion", PrimitiveType.Cylinder,
            new Vector3(0f, .48f, 0f), Quaternion.identity,
            new Vector3(.34f, .08f, .34f), materials["CH_White"]);
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad;
            AddPrimitivePart(parent, "GoldLeg_" + (i + 1), PrimitiveType.Cube,
                new Vector3(Mathf.Cos(angle) * .18f, .2f, Mathf.Sin(angle) * .18f),
                Quaternion.identity, new Vector3(.07f, .4f, .07f), materials["CH_Gold"]);
        }
    }

    private static void BuildBedroomWardrobeVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "MintBody", PrimitiveType.Cube,
            new Vector3(0f, 1.07f, 0f), Quaternion.identity,
            new Vector3(1.12f, 2.14f, .54f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "CoralDoorLeft", PrimitiveType.Cube,
            new Vector3(-.26f, 1.07f, -.28f), Quaternion.identity,
            new Vector3(.5f, 1.92f, .05f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "CoralDoorRight", PrimitiveType.Cube,
            new Vector3(.26f, 1.07f, -.28f), Quaternion.identity,
            new Vector3(.5f, 1.92f, .05f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "GoldHandleL", PrimitiveType.Cube,
            new Vector3(-.08f, 1.07f, -.32f), Quaternion.identity,
            new Vector3(.04f, .22f, .04f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "GoldHandleR", PrimitiveType.Cube,
            new Vector3(.08f, 1.07f, -.32f), Quaternion.identity,
            new Vector3(.04f, .22f, .04f), materials["CH_Gold"]);
    }

    private static void BuildBedroomWindowDaybedVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "PeachBase", PrimitiveType.Cube,
            new Vector3(0f, .28f, 0f), Quaternion.identity,
            new Vector3(1.96f, .56f, .8f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "MintMattress", PrimitiveType.Cube,
            new Vector3(0f, .58f, 0f), Quaternion.identity,
            new Vector3(1.88f, .12f, .72f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "PillowLeft", PrimitiveType.Cube,
            new Vector3(-.62f, .7f, .08f), Quaternion.identity,
            new Vector3(.42f, .16f, .36f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "PillowRight", PrimitiveType.Cube,
            new Vector3(.62f, .7f, .08f), Quaternion.identity,
            new Vector3(.42f, .16f, .36f), materials["CH_CoralBright"]);
    }

    private static void BuildBedroomStarCanopyVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "LilacRing", PrimitiveType.Cylinder,
            new Vector3(0f, 1.42f, 0f), Quaternion.identity,
            new Vector3(.7f, .05f, .7f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "DrapeLeft", PrimitiveType.Cube,
            new Vector3(-.38f, .78f, 0f), Quaternion.Euler(0f, 0f, 8f),
            new Vector3(.08f, 1.28f, .7f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "DrapeRight", PrimitiveType.Cube,
            new Vector3(.38f, .78f, 0f), Quaternion.Euler(0f, 0f, -8f),
            new Vector3(.08f, 1.28f, .7f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "Cushion", PrimitiveType.Cylinder,
            new Vector3(0f, .12f, 0f), Quaternion.identity,
            new Vector3(.7f, .24f, .7f), materials["CH_White"]);
        AddPrimitivePart(parent, "StarDrop", PrimitiveType.Sphere,
            new Vector3(0f, 1.18f, 0f), Quaternion.identity,
            Vector3.one * .12f, materials["CH_LemonBright"]);
    }

    private static void BuildBedroomQueenBedVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "MintHeadboard", PrimitiveType.Cube,
            new Vector3(0f, .72f, .62f), Quaternion.identity,
            new Vector3(2.28f, 1.05f, .14f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "CreamFrame", PrimitiveType.Cube,
            new Vector3(0f, .28f, -.05f), Quaternion.identity,
            new Vector3(2.28f, .42f, 1.28f), materials["CH_White"]);
        AddPrimitivePart(parent, "CoralMattress", PrimitiveType.Cube,
            new Vector3(0f, .52f, -.08f), Quaternion.identity,
            new Vector3(2.12f, .16f, 1.16f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "PillowLeft", PrimitiveType.Cube,
            new Vector3(-.46f, .68f, .38f), Quaternion.identity,
            new Vector3(.7f, .18f, .32f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "PillowRight", PrimitiveType.Cube,
            new Vector3(.46f, .68f, .38f), Quaternion.identity,
            new Vector3(.7f, .18f, .32f), materials["CH_LemonBright"]);
    }

    private static void BuildProceduralKitchenPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
            {
                // Premium Blender hero asset connected; placement/economy stay shared.
            }
            else if (definition.ProductId == HomeStoreService.KitchenPawMatId)
                BuildKitchenPawMatVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenFruitBasketId)
                BuildKitchenFruitBasketVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenFeedingStationId)
                BuildKitchenFeedingStationVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenCounterStoolId)
                BuildKitchenCounterStoolVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenPantryShelfId)
                BuildKitchenPantryShelfVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenDishCartId)
                BuildKitchenDishCartVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenSinkCabinetId)
                BuildKitchenSinkCabinetVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenRefrigeratorId)
                BuildKitchenRefrigeratorVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenStoveOvenId)
                BuildKitchenStoveOvenVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.KitchenIslandId)
                BuildKitchenIslandVisual(visual.transform, materials);
            else
                throw new InvalidOperationException(
                    "No Kitchen procedural visual for " + definition.ProductId);

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildKitchenPawMatVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "CoralMat", PrimitiveType.Cube,
            new Vector3(0f, .035f, 0f), Quaternion.identity,
            new Vector3(2.2f, .07f, 1.15f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "CreamInset", PrimitiveType.Cube,
            new Vector3(0f, .073f, 0f), Quaternion.identity,
            new Vector3(1.98f, .018f, .94f), materials["CH_White"]);
        AddHorizontalPaw(parent, new Vector3(0f, .09f, -.05f),
            materials["CH_AquaBright"]);
    }

    private static void BuildKitchenFruitBasketVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "BasketBody", PrimitiveType.Cylinder,
            new Vector3(0f, .24f, 0f), Quaternion.identity,
            new Vector3(.43f, .48f, .43f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "BasketInset", PrimitiveType.Cylinder,
            new Vector3(0f, .49f, 0f), Quaternion.identity,
            new Vector3(.36f, .035f, .36f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "HandleLeft", PrimitiveType.Cube,
            new Vector3(-.34f, .63f, 0f), Quaternion.Euler(0f, 0f, -12f),
            new Vector3(.055f, .48f, .055f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "HandleRight", PrimitiveType.Cube,
            new Vector3(.34f, .63f, 0f), Quaternion.Euler(0f, 0f, 12f),
            new Vector3(.055f, .48f, .055f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "HandleTop", PrimitiveType.Cube,
            new Vector3(0f, .85f, 0f), Quaternion.identity,
            new Vector3(.61f, .055f, .055f), materials["CH_Gold"]);
        Material[] fruit =
        {
            materials["CH_CoralBright"], materials["CH_LemonBright"],
            materials["CH_MintBright"], materials["CH_LilacBright"],
            materials["CH_OrangeLight"]
        };
        Vector3[] positions =
        {
            new Vector3(-.2f, .56f, -.08f), new Vector3(.03f, .59f, -.12f),
            new Vector3(.23f, .55f, -.02f), new Vector3(-.09f, .61f, .12f),
            new Vector3(.14f, .6f, .14f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            AddPrimitivePart(parent, "Fruit_" + (i + 1), PrimitiveType.Sphere,
                positions[i], Quaternion.identity, Vector3.one * .22f, fruit[i]);
        }
    }

    private static void BuildKitchenFeedingStationVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "MintBase", PrimitiveType.Cube,
            new Vector3(0f, .16f, 0f), Quaternion.identity,
            new Vector3(1.35f, .32f, .72f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "CreamTop", PrimitiveType.Cube,
            new Vector3(0f, .35f, 0f), Quaternion.identity,
            new Vector3(1.41f, .09f, .77f), materials["CH_White"]);
        for (int i = 0; i < 2; i++)
        {
            Material bowlColor = i == 0
                ? materials["CH_CoralBright"] : materials["CH_AquaBright"];
            float x = i == 0 ? -.36f : .36f;
            AddPrimitivePart(parent, "Bowl_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(x, .43f, 0f), Quaternion.identity,
                new Vector3(.27f, .16f, .27f), bowlColor);
            AddPrimitivePart(parent, "BowlInset_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(x, .52f, 0f), Quaternion.identity,
                new Vector3(.21f, .025f, .21f), materials["CH_Ink"]);
        }
        AddVerticalPaw(parent, new Vector3(0f, .18f, -.372f),
            materials["CH_LemonBright"]);
    }

    private static void BuildKitchenCounterStoolVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "LilacSeat", PrimitiveType.Cylinder,
            new Vector3(0f, .76f, 0f), Quaternion.identity,
            new Vector3(.42f, .14f, .42f), materials["CH_LilacBright"]);
        Vector3[] legs =
        {
            new Vector3(-.27f, .36f, -.22f), new Vector3(.27f, .36f, -.22f),
            new Vector3(-.27f, .36f, .22f), new Vector3(.27f, .36f, .22f)
        };
        for (int i = 0; i < legs.Length; i++)
        {
            AddPrimitivePart(parent, "GoldLeg_" + (i + 1), PrimitiveType.Cube,
                legs[i], Quaternion.identity, new Vector3(.07f, .72f, .07f),
                materials["CH_Gold"]);
        }
        AddPrimitivePart(parent, "CoralFootRail", PrimitiveType.Cube,
            new Vector3(0f, .31f, -.27f), Quaternion.identity,
            new Vector3(.58f, .055f, .055f), materials["CH_CoralBright"]);
    }

    private static void BuildKitchenPantryShelfVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "LilacBack", PrimitiveType.Cube,
            new Vector3(0f, .925f, .21f), Quaternion.identity,
            new Vector3(1.15f, 1.85f, .13f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "LeftPost", PrimitiveType.Cube,
            new Vector3(-.53f, .925f, 0f), Quaternion.identity,
            new Vector3(.09f, 1.85f, .55f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "RightPost", PrimitiveType.Cube,
            new Vector3(.53f, .925f, 0f), Quaternion.identity,
            new Vector3(.09f, 1.85f, .55f), materials["CH_AquaBright"]);
        for (int row = 0; row < 4; row++)
        {
            float y = .17f + row * .5f;
            AddPrimitivePart(parent, "GoldShelf_" + (row + 1), PrimitiveType.Cube,
                new Vector3(0f, y, -.02f), Quaternion.identity,
                new Vector3(1.08f, .075f, .55f), materials["CH_Gold"]);
            if (row < 3)
            {
                AddPrimitivePart(parent, "PantryJar_" + (row + 1), PrimitiveType.Cylinder,
                    new Vector3(-.26f, y + .22f, -.13f), Quaternion.identity,
                    new Vector3(.14f, .34f, .14f),
                    row == 0 ? materials["CH_CoralBright"]
                    : row == 1 ? materials["CH_MintBright"]
                    : materials["CH_LemonBright"]);
                AddPrimitivePart(parent, "PantryBox_" + (row + 1), PrimitiveType.Cube,
                    new Vector3(.23f, y + .2f, -.12f), Quaternion.identity,
                    new Vector3(.28f, .38f, .22f),
                    row == 1 ? materials["CH_CoralBright"] : materials["CH_White"]);
            }
        }
    }

    private static void BuildKitchenDishCartVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "CartBack", PrimitiveType.Cube,
            new Vector3(0f, .61f, .23f), Quaternion.identity,
            new Vector3(.92f, 1.08f, .12f), materials["CH_AquaBright"]);
        for (int row = 0; row < 3; row++)
        {
            float y = .22f + row * .4f;
            AddPrimitivePart(parent, "CreamShelf_" + (row + 1), PrimitiveType.Cube,
                new Vector3(0f, y, 0f), Quaternion.identity,
                new Vector3(.92f, .07f, .62f), materials["CH_White"]);
            AddPrimitivePart(parent, "Plate_" + (row + 1), PrimitiveType.Cylinder,
                new Vector3(-.21f, y + .14f, -.18f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(.2f, .045f, .2f),
                row == 0 ? materials["CH_CoralBright"]
                : row == 1 ? materials["CH_LilacBright"]
                : materials["CH_LemonBright"]);
            AddPrimitivePart(parent, "Cup_" + (row + 1), PrimitiveType.Cylinder,
                new Vector3(.23f, y + .13f, -.12f), Quaternion.identity,
                new Vector3(.12f, .24f, .12f), materials["CH_MintBright"]);
        }
        for (int i = 0; i < 2; i++)
        {
            AddPrimitivePart(parent, "Wheel_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(i == 0 ? -.3f : .3f, .08f, -.18f),
                Quaternion.Euler(0f, 0f, 90f), new Vector3(.1f, .08f, .1f),
                materials["CH_Ink"]);
        }
    }

    private static void BuildKitchenSinkCabinetVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "CoralCabinet", PrimitiveType.Cube,
            new Vector3(0f, .45f, 0f), Quaternion.identity,
            new Vector3(1.65f, .9f, .75f), materials["CH_CoralBright"]);
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -.39f : .39f;
            AddPrimitivePart(parent, "CreamDoor_" + (i + 1), PrimitiveType.Cube,
                new Vector3(x, .43f, -.385f), Quaternion.identity,
                new Vector3(.67f, .68f, .045f), materials["CH_White"]);
            AddPrimitivePart(parent, "GoldHandle_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(i == 0 ? -.12f : .12f, .48f, -.425f), Quaternion.identity,
                Vector3.one * .08f, materials["CH_Gold"]);
        }
        AddPrimitivePart(parent, "CreamCounter", PrimitiveType.Cube,
            new Vector3(0f, .95f, 0f), Quaternion.identity,
            new Vector3(1.75f, .12f, .83f), materials["CH_White"]);
        AddPrimitivePart(parent, "AquaBasin", PrimitiveType.Cylinder,
            new Vector3(0f, 1.04f, -.03f), Quaternion.identity,
            new Vector3(.45f, .08f, .3f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "FaucetStem", PrimitiveType.Cylinder,
            new Vector3(0f, 1.25f, .19f), Quaternion.identity,
            new Vector3(.055f, .36f, .055f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "FaucetSpout", PrimitiveType.Cube,
            new Vector3(0f, 1.41f, .06f), Quaternion.identity,
            new Vector3(.08f, .07f, .28f), materials["CH_Gold"]);
    }

    private static void BuildKitchenRefrigeratorVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "MintFridge", PrimitiveType.Cube,
            new Vector3(0f, 1.125f, 0f), Quaternion.identity,
            new Vector3(1.25f, 2.25f, .78f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "CreamDoorTop", PrimitiveType.Cube,
            new Vector3(0f, 1.55f, -.402f), Quaternion.identity,
            new Vector3(1.09f, 1.15f, .045f), materials["CH_White"]);
        AddPrimitivePart(parent, "LilacDoorBottom", PrimitiveType.Cube,
            new Vector3(0f, .54f, -.402f), Quaternion.identity,
            new Vector3(1.09f, .7f, .045f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "GoldTopHandle", PrimitiveType.Cube,
            new Vector3(.43f, 1.48f, -.455f), Quaternion.identity,
            new Vector3(.07f, .58f, .07f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "GoldBottomHandle", PrimitiveType.Cube,
            new Vector3(.43f, .58f, -.455f), Quaternion.identity,
            new Vector3(.07f, .36f, .07f), materials["CH_Gold"]);
        AddVerticalPaw(parent, new Vector3(-.28f, 1.55f, -.46f),
            materials["CH_CoralBright"]);
    }

    private static void BuildKitchenStoveOvenVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "PeachOven", PrimitiveType.Cube,
            new Vector3(0f, .65f, 0f), Quaternion.identity,
            new Vector3(1.18f, 1.3f, .74f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "OvenWindow", PrimitiveType.Cube,
            new Vector3(0f, .47f, -.382f), Quaternion.identity,
            new Vector3(.88f, .56f, .04f), materials["CH_Screen"]);
        AddPrimitivePart(parent, "CreamTrim", PrimitiveType.Cube,
            new Vector3(0f, .47f, -.407f), Quaternion.identity,
            new Vector3(1.01f, .69f, .018f), materials["CH_White"]);
        AddPrimitivePart(parent, "WindowFace", PrimitiveType.Cube,
            new Vector3(0f, .47f, -.421f), Quaternion.identity,
            new Vector3(.86f, .53f, .018f), materials["CH_Screen"]);
        AddPrimitivePart(parent, "DarkCooktop", PrimitiveType.Cube,
            new Vector3(0f, 1.325f, 0f), Quaternion.identity,
            new Vector3(1.18f, .05f, .74f), materials["CH_Ink"]);
        Vector3[] burners =
        {
            new Vector3(-.3f, 1.37f, -.18f), new Vector3(.3f, 1.37f, -.18f),
            new Vector3(-.3f, 1.37f, .18f), new Vector3(.3f, 1.37f, .18f)
        };
        for (int i = 0; i < burners.Length; i++)
        {
            AddPrimitivePart(parent, "Burner_" + (i + 1), PrimitiveType.Cylinder,
                burners[i], Quaternion.identity, new Vector3(.18f, .025f, .18f),
                i % 2 == 0 ? materials["CH_CoralBright"] : materials["CH_AquaBright"]);
        }
        for (int i = 0; i < 3; i++)
        {
            AddPrimitivePart(parent, "GoldKnob_" + (i + 1), PrimitiveType.Sphere,
                new Vector3((i - 1) * .27f, 1.02f, -.405f), Quaternion.identity,
                Vector3.one * .09f, materials["CH_Gold"]);
        }
    }

    private static void BuildKitchenIslandVisual(
        Transform parent,
        IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "AquaIsland", PrimitiveType.Cube,
            new Vector3(0f, .47f, 0f), Quaternion.identity,
            new Vector3(2.08f, .94f, .9f), materials["CH_AquaBright"]);
        for (int i = 0; i < 3; i++)
        {
            AddPrimitivePart(parent, "CoralPanel_" + (i + 1), PrimitiveType.Cube,
                new Vector3((i - 1) * .65f, .5f, -.462f), Quaternion.identity,
                new Vector3(.53f, .66f, .04f),
                i == 1 ? materials["CH_LemonBright"] : materials["CH_CoralBright"]);
        }
        AddPrimitivePart(parent, "CreamCounter", PrimitiveType.Cube,
            new Vector3(0f, 1.01f, 0f), Quaternion.identity,
            new Vector3(2.25f, .14f, 1.05f), materials["CH_White"]);
        AddPrimitivePart(parent, "GoldFootRail", PrimitiveType.Cube,
            new Vector3(0f, .16f, -.54f), Quaternion.identity,
            new Vector3(1.65f, .065f, .065f), materials["CH_Gold"]);
        AddVerticalPaw(parent, new Vector3(0f, .53f, -.51f),
            materials["CH_White"]);
    }

    private static void AddHorizontalPaw(
        Transform parent, Vector3 center, Material material)
    {
        AddPrimitivePart(parent, "PawPad", PrimitiveType.Sphere,
            center, Quaternion.identity, new Vector3(.44f, .035f, .33f), material);
        Vector3[] toes =
        {
            new Vector3(-.28f, 0f, .25f), new Vector3(-.09f, 0f, .34f),
            new Vector3(.12f, 0f, .34f), new Vector3(.3f, 0f, .24f)
        };
        for (int i = 0; i < toes.Length; i++)
        {
            AddPrimitivePart(parent, "PawToe_" + (i + 1), PrimitiveType.Sphere,
                center + toes[i], Quaternion.identity,
                new Vector3(.15f, .035f, .17f), material);
        }
    }

    private static void AddVerticalPaw(
        Transform parent, Vector3 center, Material material)
    {
        AddPrimitivePart(parent, "PawPad", PrimitiveType.Sphere,
            center, Quaternion.identity, new Vector3(.23f, .18f, .035f), material);
        Vector3[] toes =
        {
            new Vector3(-.16f, .19f, 0f), new Vector3(-.055f, .25f, 0f),
            new Vector3(.065f, .25f, 0f), new Vector3(.17f, .18f, 0f)
        };
        for (int i = 0; i < toes.Length; i++)
        {
            AddPrimitivePart(parent, "PawToe_" + (i + 1), PrimitiveType.Sphere,
                center + toes[i], Quaternion.identity,
                new Vector3(.075f, .08f, .035f), material);
        }
    }

    private static void BuildProceduralGardenPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
            {
                // Premium Blender hero asset connected; open pergola colliders remain below.
            }
            else if (definition.ProductId == HomeStoreService.GardenYarnBallId)
                BuildGardenYarnBallVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenFlowerPotsId)
                BuildGardenFlowerPotsVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenDaisyBedId)
                BuildGardenDaisyBedVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenSaplingId)
                BuildGardenSaplingVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenBirdBathId)
                BuildGardenBirdBathVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenSunLoungerId)
                BuildGardenSunLoungerVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenBistroSetId)
                BuildGardenBistroSetVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenGrillId)
                BuildGardenGrillVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenHammockId)
                BuildGardenHammockVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.GardenPergolaId)
                BuildGardenPergolaVisual(visual.transform, materials);
            else
                throw new InvalidOperationException(
                    "No Garden procedural visual for " + definition.ProductId);

            if (definition.ProductId == HomeStoreService.GardenPergolaId)
                AddGardenPergolaColliders(visual);
            else
                AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildGardenYarnBallVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "YarnCore", PrimitiveType.Sphere,
            new Vector3(0f, .14f, 0f), Quaternion.identity,
            Vector3.one * .28f, materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "YarnBand", PrimitiveType.Cylinder,
            new Vector3(0f, .14f, 0f), Quaternion.Euler(0f, 0f, 90f),
            new Vector3(.3f, .04f, .3f), materials["CH_LemonBright"]);
    }

    private static void BuildGardenFlowerPotsVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "PotCoral", PrimitiveType.Cylinder,
            new Vector3(-.22f, .14f, .04f), Quaternion.identity,
            new Vector3(.22f, .28f, .22f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "BloomCoral", PrimitiveType.Sphere,
            new Vector3(-.22f, .36f, .04f), Quaternion.identity,
            Vector3.one * .2f, materials["CH_Pink"]);
        AddPrimitivePart(parent, "PotLemon", PrimitiveType.Cylinder,
            new Vector3(.18f, .12f, -.08f), Quaternion.identity,
            new Vector3(.2f, .24f, .2f), materials["CH_LemonBright"]);
        AddPrimitivePart(parent, "BloomLemon", PrimitiveType.Sphere,
            new Vector3(.18f, .32f, -.08f), Quaternion.identity,
            Vector3.one * .16f, materials["CH_MintBright"]);
        AddPrimitivePart(parent, "PotLilac", PrimitiveType.Cylinder,
            new Vector3(.04f, .1f, .18f), Quaternion.identity,
            new Vector3(.18f, .2f, .18f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "BloomLilac", PrimitiveType.Sphere,
            new Vector3(.04f, .28f, .18f), Quaternion.identity,
            Vector3.one * .14f, materials["CH_White"]);
    }

    private static void BuildGardenDaisyBedVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "SoilBed", PrimitiveType.Cube,
            new Vector3(0f, .08f, 0f), Quaternion.identity,
            new Vector3(1.08f, .16f, .68f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "MintRim", PrimitiveType.Cube,
            new Vector3(0f, .16f, 0f), Quaternion.identity,
            new Vector3(1.12f, .05f, .72f), materials["CH_MintBright"]);
        for (int i = 0; i < 5; i++)
        {
            float x = -0.36f + i * .18f;
            AddPrimitivePart(parent, "Daisy_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(x, .28f, (i % 2 == 0 ? .08f : -.1f)),
                Quaternion.identity,
                Vector3.one * .14f,
                i % 2 == 0 ? materials["CH_White"] : materials["CH_LemonBright"]);
        }
    }

    private static void BuildGardenSaplingVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Trunk", PrimitiveType.Cylinder,
            new Vector3(0f, .55f, 0f), Quaternion.identity,
            new Vector3(.16f, 1.1f, .16f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "Crown", PrimitiveType.Sphere,
            new Vector3(0f, 1.35f, 0f), Quaternion.identity,
            new Vector3(.9f, .72f, .9f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "CrownHigh", PrimitiveType.Sphere,
            new Vector3(.12f, 1.62f, -.08f), Quaternion.identity,
            new Vector3(.55f, .42f, .55f), materials["CH_TealLight"]);
    }

    private static void BuildGardenBirdBathVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Pedestal", PrimitiveType.Cylinder,
            new Vector3(0f, .28f, 0f), Quaternion.identity,
            new Vector3(.18f, .56f, .18f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "Bowl", PrimitiveType.Cylinder,
            new Vector3(0f, .62f, 0f), Quaternion.identity,
            new Vector3(.62f, .08f, .62f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "Water", PrimitiveType.Cylinder,
            new Vector3(0f, .67f, 0f), Quaternion.identity,
            new Vector3(.5f, .03f, .5f), materials["CH_AquaBright"]);
    }

    private static void BuildGardenSunLoungerVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Frame", PrimitiveType.Cube,
            new Vector3(0f, .16f, 0f), Quaternion.identity,
            new Vector3(1.32f, .08f, .48f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Cushion", PrimitiveType.Cube,
            new Vector3(0f, .24f, 0f), Quaternion.identity,
            new Vector3(1.22f, .1f, .42f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "Backrest", PrimitiveType.Cube,
            new Vector3(-.48f, .4f, 0f), Quaternion.Euler(0f, 0f, 28f),
            new Vector3(.5f, .08f, .42f), materials["CH_CoralBright"]);
    }

    private static void BuildGardenBistroSetVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "TableTop", PrimitiveType.Cylinder,
            new Vector3(0f, .48f, 0f), Quaternion.identity,
            new Vector3(.72f, .06f, .72f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "TableStem", PrimitiveType.Cylinder,
            new Vector3(0f, .24f, 0f), Quaternion.identity,
            new Vector3(.1f, .48f, .1f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "ChairSeatL", PrimitiveType.Cube,
            new Vector3(-.48f, .28f, .18f), Quaternion.identity,
            new Vector3(.32f, .08f, .32f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "ChairBackL", PrimitiveType.Cube,
            new Vector3(-.6f, .46f, .18f), Quaternion.identity,
            new Vector3(.06f, .32f, .32f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "ChairSeatR", PrimitiveType.Cube,
            new Vector3(.48f, .28f, -.16f), Quaternion.identity,
            new Vector3(.32f, .08f, .32f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "ChairBackR", PrimitiveType.Cube,
            new Vector3(.6f, .46f, -.16f), Quaternion.identity,
            new Vector3(.06f, .32f, .32f), materials["CH_AquaBright"]);
    }

    private static void BuildGardenGrillVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Body", PrimitiveType.Cube,
            new Vector3(0f, .42f, 0f), Quaternion.identity,
            new Vector3(.72f, .52f, .48f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "Lid", PrimitiveType.Cube,
            new Vector3(0f, .74f, 0f), Quaternion.identity,
            new Vector3(.76f, .12f, .52f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "Knob", PrimitiveType.Sphere,
            new Vector3(0f, .56f, -.26f), Quaternion.identity,
            Vector3.one * .08f, materials["CH_Gold"]);
        AddPrimitivePart(parent, "LegL", PrimitiveType.Cube,
            new Vector3(-.26f, .12f, 0f), Quaternion.identity,
            new Vector3(.08f, .24f, .08f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "LegR", PrimitiveType.Cube,
            new Vector3(.26f, .12f, 0f), Quaternion.identity,
            new Vector3(.08f, .24f, .08f), materials["CH_Gold"]);
    }

    private static void BuildGardenHammockVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "PostL", PrimitiveType.Cylinder,
            new Vector3(-.48f, .42f, 0f), Quaternion.identity,
            new Vector3(.1f, .84f, .1f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "PostR", PrimitiveType.Cylinder,
            new Vector3(.48f, .42f, 0f), Quaternion.identity,
            new Vector3(.1f, .84f, .1f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Sling", PrimitiveType.Cube,
            new Vector3(0f, .42f, 0f), Quaternion.identity,
            new Vector3(.92f, .08f, .38f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "Pillow", PrimitiveType.Cube,
            new Vector3(-.22f, .5f, 0f), Quaternion.identity,
            new Vector3(.28f, .08f, .28f), materials["CH_White"]);
    }

    // Post top at y = 1.68 so the arbor stays low enough for the camera and, more
    // importantly, so the canopy clears anything the player parks underneath it.
    private const float GardenPergolaPostTop = 1.68f;

    private static void BuildGardenPergolaVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        const float postHeight = 1.68f;
        float postCenterY = postHeight * .5f;
        float beamY = postHeight - .06f;
        float slatY = postHeight + .02f;

        AddPrimitivePart(parent, "PostFL", PrimitiveType.Cube,
            new Vector3(-.85f, postCenterY, -.58f), Quaternion.identity,
            new Vector3(.12f, postHeight, .12f), materials["CH_White"]);
        AddPrimitivePart(parent, "PostFR", PrimitiveType.Cube,
            new Vector3(.85f, postCenterY, -.58f), Quaternion.identity,
            new Vector3(.12f, postHeight, .12f), materials["CH_White"]);
        AddPrimitivePart(parent, "PostBL", PrimitiveType.Cube,
            new Vector3(-.85f, postCenterY, .58f), Quaternion.identity,
            new Vector3(.12f, postHeight, .12f), materials["CH_White"]);
        AddPrimitivePart(parent, "PostBR", PrimitiveType.Cube,
            new Vector3(.85f, postCenterY, .58f), Quaternion.identity,
            new Vector3(.12f, postHeight, .12f), materials["CH_White"]);
        AddPrimitivePart(parent, "BeamFront", PrimitiveType.Cube,
            new Vector3(0f, beamY, -.58f), Quaternion.identity,
            new Vector3(1.9f, .1f, .12f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "BeamBack", PrimitiveType.Cube,
            new Vector3(0f, beamY, .58f), Quaternion.identity,
            new Vector3(1.9f, .1f, .12f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "SlatA", PrimitiveType.Cube,
            new Vector3(-.4f, slatY, 0f), Quaternion.identity,
            new Vector3(.1f, .06f, 1.28f), materials["CH_LemonBright"]);
        AddPrimitivePart(parent, "SlatB", PrimitiveType.Cube,
            new Vector3(.4f, slatY, 0f), Quaternion.identity,
            new Vector3(.1f, .06f, 1.28f), materials["CH_LemonBright"]);
        // A leafy climbing vine over the canopy so the lower arbor still reads lush.
        AddPrimitivePart(parent, "VineA", PrimitiveType.Sphere,
            new Vector3(-.55f, slatY + .12f, .18f), Quaternion.identity,
            new Vector3(.5f, .3f, .55f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "VineB", PrimitiveType.Sphere,
            new Vector3(.5f, slatY + .12f, -.2f), Quaternion.identity,
            new Vector3(.55f, .28f, .5f), materials["CH_MintBright"]);
    }

    // The pergola is an open arbor: the player must be able to park furniture under
    // its canopy. A single footprint-sized box (AddProductCollider) makes the whole
    // interior an obstacle, so instead the four corner posts are the only blockers.
    private static void AddGardenPergolaColliders(GameObject visual)
    {
        var posts = new[]
        {
            new Vector3(-.85f, 0f, -.58f),
            new Vector3(.85f, 0f, -.58f),
            new Vector3(-.85f, 0f, .58f),
            new Vector3(.85f, 0f, .58f)
        };
        for (int i = 0; i < posts.Length; i++)
        {
            BoxCollider post = visual.AddComponent<BoxCollider>();
            post.center = new Vector3(
                posts[i].x, GardenPergolaPostTop * .5f, posts[i].z);
            post.size = new Vector3(.2f, GardenPergolaPostTop, .2f);
        }
    }


    /// <summary>
    /// Balcony v1 (20 August) shipped deliberately without cat routines. The
    /// wave-3 decision of 2 September covers all 80 products, so the room is
    /// being brought up to the same contract as the Bathroom, Kitchen, Bedroom
    /// and Garden: every product carries a routine, a validator entry and a test.
    /// </summary>
    private static void AttachBalconyActivity(
        GameObject root, GameObject visual, StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        string id = definition.ProductId;
        if (OutdoorArrangementProfile.IsDecoration(id)) return;
        if (id == HomeStoreService.BalconySunAwningId)
            AttachAwningGazeActivity(root, visual);
        else if (id == HomeStoreService.BalconyHerbShelfId)
            AttachHerbShelfClimbActivity(root, visual);
        else if (id == HomeStoreService.BalconyBirdFeederId)
            AttachFeederShakeActivity(root, visual, materials);
        else if (id == HomeStoreService.BalconyHangingChairId)
            AttachEggChairNapActivity(root, visual);
        else if (id == HomeStoreService.BalconyCushionBenchId)
            AttachBenchNapActivity(root, visual);
        else if (id == HomeStoreService.BalconySideTableId)
            AttachTableKnockOffActivity(root, visual, materials);
        else if (id == HomeStoreService.BalconySunMatId)
            AttachSunMatBaskActivity(root, visual);
        else if (id == HomeStoreService.BalconyPlanterBoxId)
            AttachPlanterDigActivity(root, visual);
        else if (id == HomeStoreService.BalconyRailingFlowersId)
            AttachRailingSwatActivity(root, visual);
        else if (id == HomeStoreService.BalconyLanternStringId)
            AttachLanternGazeActivity(root, visual);
    }

    /// <summary>
    /// The valance at an authored 0.10, looked at from the deck below.
    ///
    /// This product hangs at HungHeight 2.24 and its own box is capped at 0.71,
    /// so nothing on it can ever be within a cat's reach: a pull cord long
    /// enough to bat would have to hang 1.2 below the housing. The honest beat
    /// is therefore the look-up, the same one the Bedroom's dream art uses.
    ///
    /// The awning is in `facesBackward` — yaw 180 would otherwise point the
    /// fabric at the wall — so root space flips Z and both points are positive.
    /// </summary>
    private static void AttachAwningGazeActivity(GameObject root, GameObject visual)
    {
        Transform look = MakePoint(root, "GazeLookPoint", new Vector3(0f, .120f, .440f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .98f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "awning-gaze", "SUN AWNING", CatActivityKind.AwningGaze,
            QuestType.BalconyWatch, 0, "GAZE", 1.1f, 2f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconySunAwningId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.6f, "SO SHADY!");
    }

    /// <summary>
    /// Boards at an authored 0.44 and 0.90, sized against the routine.
    ///
    /// <see cref="PantryClimbActivity"/> stages exactly two hops and refuses
    /// much more than half a metre each, so the boards were placed to that
    /// limit rather than the other way round. The middle board is left clear
    /// across its centre — its pots are pushed to the ends — because that is
    /// where the cat lands.
    ///
    /// The shelf IS in `facesBackward`: yaw 90 at x -3.22 would face the rack
    /// into the left wall. Root space therefore flips Z, so the deck is at
    /// root +Z and every approach point below is positive.
    /// </summary>
    private static void AttachHerbShelfClimbActivity(GameObject root, GameObject visual)
    {
        const float authoredLowBoard = .440f;
        const float authoredMidBoard = .900f;

        Transform floor = MakePoint(root, "ClimbFloorPoint", new Vector3(0f, 0f, .46f));
        Transform lower = MakePoint(root, "ClimbLowerPoint",
            new Vector3(-.140f, authoredLowBoard, .060f));
        Transform upper = MakePoint(root, "ClimbUpperPoint",
            new Vector3(0f, authoredMidBoard, .040f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .68f));

        PantryClimbActivity activity = root.AddComponent<PantryClimbActivity>();
        activity.EditorConfigure(
            "herb-shelf-climb", "HERB SHELF", CatActivityKind.HerbShelfClimb,
            QuestType.PantryClimb, 0, "CLIMB", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconyHerbShelfId);
        activity.EditorConfigureClimb(floor, lower, upper, 2.4f);
    }

    /// <summary>
    /// The feeder hanging at an authored 0.76-1.06, worked from the deck below.
    ///
    /// `BirdFeederShakeActivity` is the Balcony's own routine and the only one
    /// in the project where the cat works a product from underneath: it rears up
    /// on the spot instead of climbing on. The feeder and the seed each hang
    /// under their own pivot so the routine can swing one and drop the other,
    /// and both are separate single-object FBX files for the same reason the
    /// porch swing's bench is.
    /// </summary>
    private static void AttachFeederShakeActivity(
        GameObject root, GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject feederAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "BalconyBirdFeederFeeder_Premium.fbx");
        GameObject seedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "BalconyBirdFeederSeed_Premium.fbx");
        if (feederAsset == null || seedAsset == null)
            return;

        const float authoredHang = 1.240f;
        const float authoredHookZ = -.195f;

        Transform feederPivot = new GameObject("FeederPivot").transform;
        feederPivot.SetParent(visual.transform, false);
        feederPivot.localPosition = new Vector3(0f, authoredHang, authoredHookZ);

        GameObject feeder = PrefabUtility.InstantiatePrefab(feederAsset) as GameObject;
        if (feeder == null)
            return;
        feeder.name = "Feeder";
        feeder.transform.SetParent(feederPivot, false);
        feeder.transform.localPosition =
            new Vector3(0f, -authoredHang, -authoredHookZ);
        feeder.transform.localRotation = Quaternion.identity;
        RemoveChildColliders(feeder);
        ReplaceMaterials(feeder, materials);

        Transform seedPivot = new GameObject("SeedPivot").transform;
        seedPivot.SetParent(feederPivot, false);
        seedPivot.localPosition = Vector3.zero;

        GameObject seed = PrefabUtility.InstantiatePrefab(seedAsset) as GameObject;
        if (seed == null)
            return;
        seed.name = "Seed";
        seed.transform.SetParent(seedPivot, false);
        seed.transform.localPosition =
            new Vector3(0f, -authoredHang, -authoredHookZ);
        seed.transform.localRotation = Quaternion.identity;
        RemoveChildColliders(seed);
        ReplaceMaterials(seed, materials);

        Transform reach = MakePoint(root, "ShakeReachPoint",
            new Vector3(0f, 0f, -.42f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.60f));

        BirdFeederShakeActivity activity =
            root.AddComponent<BirdFeederShakeActivity>();
        activity.EditorConfigure(
            "feeder-shake", "BIRD FEEDER", CatActivityKind.FeederShake,
            QuestType.FeederShake, 0, "SHAKE", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconyBirdFeederId);
        activity.EditorConfigureShake(reach, feederPivot, seedPivot, 3, 14f, 1.1f);
    }

    /// <summary>Egg chair cushion at an authored 0.560, mouth at -Z.</summary>
    private static void AttachEggChairNapActivity(GameObject root, GameObject visual)
    {
        Transform door = MakePoint(root, "NapDoorPoint", new Vector3(0f, 0f, -.56f));
        Transform nest = MakePoint(root, "NapNestPoint", new Vector3(0f, .560f, -.53f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.74f));

        CanopyNapActivity activity = root.AddComponent<CanopyNapActivity>();
        activity.EditorConfigure(
            "egg-chair-nap", "HANGING CHAIR", CatActivityKind.EggChairNap,
            QuestType.Sleep, 0, "CURL UP", 1.1f, 8f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconyHangingChairId);
        activity.EditorConfigureNap(door, nest, 3.6f, 26f, 92f);
    }

    /// <summary>Bench seat at an authored 0.400, clear half authored at +X.</summary>
    private static void AttachBenchNapActivity(GameObject root, GameObject visual)
    {
        const float authoredSeat = .400f;
        const float authoredPerchX = .420f;

        Transform floor = MakePoint(root, "PerchFloorPoint",
            new Vector3(-authoredPerchX, 0f, -.52f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(-authoredPerchX, authoredSeat, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-authoredPerchX, 0f, -.70f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "bench-nap", "CUSHION BENCH", CatActivityKind.BenchNap,
            QuestType.Sleep, 0, "NAP", 1.1f, 8f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconyCushionBenchId);
        activity.EditorConfigurePerch(floor, perch, 3.4f, 22f, 92f, "SUN NAP!");
    }

    /// <summary>
    /// The drink on the tray at an authored 0.430 — the nightstand's routine in
    /// the one other place a glass is left standing at cat height.
    /// </summary>
    private static void AttachTableKnockOffActivity(
        GameObject root, GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject glassAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "BalconySideTableGlass_Premium.fbx");
        if (glassAsset == null)
            return;

        const float authoredGlassX = .115f;
        const float authoredGlassZ = -.075f;

        Transform pivot = new GameObject("GlassPivot").transform;
        pivot.SetParent(visual.transform, false);
        pivot.localPosition = new Vector3(-authoredGlassX, .345f, authoredGlassZ);

        GameObject glass = PrefabUtility.InstantiatePrefab(glassAsset) as GameObject;
        if (glass == null)
            return;
        glass.name = "Glass";
        glass.transform.SetParent(pivot, false);
        glass.transform.localPosition =
            new Vector3(authoredGlassX, -.345f, -authoredGlassZ);
        glass.transform.localRotation = Quaternion.identity;
        RemoveChildColliders(glass);
        ReplaceMaterials(glass, materials);

        Transform reach = MakePoint(root, "KnockReachPoint",
            new Vector3(-authoredGlassX, 0f, -.46f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.62f));

        KnockOffActivity activity = root.AddComponent<KnockOffActivity>();
        activity.EditorConfigure(
            "table-knock-off", "SIDE TABLE", CatActivityKind.TableKnockOff,
            QuestType.KnockOff, 0, "PUSH", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconySideTableId);
        activity.EditorConfigureKnock(reach, pivot, Vector3.forward, .48f, 2);
    }

    /// <summary>The clear half of the sun mat, authored at -X so it lands at +X.</summary>
    private static void AttachSunMatBaskActivity(GameObject root, GameObject visual)
    {
        const float authoredBaskX = -.420f;

        Transform bask = MakePoint(root, "BaskPoint",
            new Vector3(-authoredBaskX, .044f, 0f));
        Transform door = MakePoint(root, "BaskDoorPoint",
            new Vector3(-authoredBaskX, 0f, -.72f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-authoredBaskX, 0f, -.88f));

        OvenWarmthActivity activity = root.AddComponent<OvenWarmthActivity>();
        activity.EditorConfigure(
            "sun-mat-bask", "SUN MAT", CatActivityKind.SunMatBask,
            QuestType.Sleep, 0, "SUNBATHE", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconySunMatId);
        activity.EditorConfigureBask(bask, door, 4.0f, 20f, 92f);
    }

    /// <summary>The bare third of the planter, authored at +X so it lands at -X.</summary>
    private static void AttachPlanterDigActivity(GameObject root, GameObject visual)
    {
        const float authoredDigX = .250f;
        const float authoredSoil = .320f;

        Transform mouth = MakePoint(root, "DigMouthPoint",
            new Vector3(-authoredDigX, 0f, -.44f));
        Transform dig = MakePoint(root, "DigPoint",
            new Vector3(-authoredDigX, authoredSoil, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-authoredDigX, 0f, -.58f));

        LitterDigActivity activity = root.AddComponent<LitterDigActivity>();
        activity.EditorConfigure(
            "planter-dig", "PLANTER BOX", CatActivityKind.PlanterDig,
            QuestType.LitterDig, 0, "DIG", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconyPlanterBoxId);
        activity.EditorConfigureDig(mouth, dig, 2.6f, 4);
    }

    /// <summary>
    /// Blooms at an authored 0.760, which is where a sitting cat's paw reaches.
    ///
    /// The railing box IS in `facesBackward` (yaw 180 would face the rail), so
    /// root space flips Z and the deck is at root +Z. This is the one Balcony
    /// look that is a SWAT rather than a gaze: the awning and the lantern string
    /// hang at 2.24 and 1.92 and are genuinely out of reach, this is not.
    /// </summary>
    private static void AttachRailingSwatActivity(GameObject root, GameObject visual)
    {
        Transform look = MakePoint(root, "SwatLookPoint", new Vector3(0f, .760f, .120f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .62f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "railing-swat", "RAILING FLOWERS", CatActivityKind.RailingSwat,
            QuestType.BalconyWatch, 0, "SWAT", 1.1f, 3f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconyRailingFlowersId);
        activity.EditorConfigureLook(look, SitLookReaction.PawSwat, 2.8f, "GOT IT!");
    }

    /// <summary>
    /// The lantern run, looked up at from the deck.
    ///
    /// Hung at 1.92 with a 0.30 box, so the lowest lantern is 1.92 above the
    /// deck and nothing here is reachable — the same honest limit the awning
    /// has. A spin or a swat would be a lie about what the cat can touch.
    /// </summary>
    private static void AttachLanternGazeActivity(GameObject root, GameObject visual)
    {
        Transform look = MakePoint(root, "GazeLookPoint", new Vector3(0f, .120f, .020f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .74f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "lantern-gaze", "LANTERN STRING", CatActivityKind.LanternGaze,
            QuestType.BalconyWatch, 0, "GAZE", 1.1f, 2f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BalconyLanternStringId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.6f, "PRETTY!");
    }
    private static void BuildProceduralBalconyPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
            {
            }
            else if (definition.ProductId == HomeStoreService.BalconySunMatId)
                BuildBalconySunMatVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconyPlanterBoxId)
                BuildBalconyPlanterBoxVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconyHerbShelfId)
                BuildBalconyHerbShelfVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconyRailingFlowersId)
                BuildBalconyRailingFlowersVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconyBirdFeederId)
                BuildBalconyBirdFeederVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconyLanternStringId)
                BuildBalconyLanternStringVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconyCushionBenchId)
                BuildBalconyCushionBenchVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconySideTableId)
                BuildBalconySideTableVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconyHangingChairId)
                BuildBalconyHangingChairVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.BalconySunAwningId)
                BuildBalconySunAwningVisual(visual.transform, materials);
            else
                throw new InvalidOperationException(
                    "No Balcony procedural visual for " + definition.ProductId);

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            AttachBalconyActivity(root, visual, definition, materials);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildBalconySunMatVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "MatBase", PrimitiveType.Cube,
            new Vector3(0f, .04f, 0f), Quaternion.identity,
            new Vector3(1.82f, .06f, 1.08f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "StripeA", PrimitiveType.Cube,
            new Vector3(-.42f, .075f, 0f), Quaternion.identity,
            new Vector3(.34f, .03f, 1.02f), materials["CH_White"]);
        AddPrimitivePart(parent, "StripeB", PrimitiveType.Cube,
            new Vector3(.42f, .075f, 0f), Quaternion.identity,
            new Vector3(.34f, .03f, 1.02f), materials["CH_CoralBright"]);
    }

    private static void BuildBalconyPlanterBoxVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Box", PrimitiveType.Cube,
            new Vector3(0f, .16f, 0f), Quaternion.identity,
            new Vector3(.78f, .32f, .34f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Soil", PrimitiveType.Cube,
            new Vector3(0f, .32f, 0f), Quaternion.identity,
            new Vector3(.72f, .06f, .28f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "BloomCoral", PrimitiveType.Sphere,
            new Vector3(-.24f, .44f, 0f), Quaternion.identity,
            Vector3.one * .2f, materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "BloomLemon", PrimitiveType.Sphere,
            new Vector3(0f, .46f, .02f), Quaternion.identity,
            Vector3.one * .2f, materials["CH_LemonBright"]);
        AddPrimitivePart(parent, "BloomLilac", PrimitiveType.Sphere,
            new Vector3(.24f, .44f, -.02f), Quaternion.identity,
            Vector3.one * .2f, materials["CH_LilacBright"]);
    }

    private static void BuildBalconyHerbShelfVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Back", PrimitiveType.Cube,
            new Vector3(0f, .7f, -.18f), Quaternion.identity,
            new Vector3(.92f, 1.4f, .06f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "ShelfLow", PrimitiveType.Cube,
            new Vector3(0f, .5f, 0f), Quaternion.identity,
            new Vector3(.92f, .06f, .32f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "ShelfHigh", PrimitiveType.Cube,
            new Vector3(0f, 1f, 0f), Quaternion.identity,
            new Vector3(.92f, .06f, .32f), materials["CH_Gold"]);
        for (int i = 0; i < 3; i++)
        {
            float x = -.3f + i * .3f;
            AddPrimitivePart(parent, "PotLow_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(x, .6f, 0f), Quaternion.identity,
                new Vector3(.18f, .16f, .18f), materials["CH_OrangeLight"]);
            AddPrimitivePart(parent, "HerbLow_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(x, .74f, 0f), Quaternion.identity,
                Vector3.one * .16f, materials["CH_MintBright"]);
            AddPrimitivePart(parent, "PotHigh_" + (i + 1), PrimitiveType.Cylinder,
                new Vector3(x, 1.1f, 0f), Quaternion.identity,
                new Vector3(.18f, .16f, .18f), materials["CH_OrangeLight"]);
            AddPrimitivePart(parent, "HerbHigh_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(x, 1.24f, 0f), Quaternion.identity,
                Vector3.one * .16f, materials["CH_TealLight"]);
        }
    }

    private static void BuildBalconyRailingFlowersVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Trough", PrimitiveType.Cube,
            new Vector3(0f, .5f, 0f), Quaternion.identity,
            new Vector3(1.52f, .22f, .24f), materials["CH_White"]);
        AddPrimitivePart(parent, "Soil", PrimitiveType.Cube,
            new Vector3(0f, .62f, 0f), Quaternion.identity,
            new Vector3(1.46f, .05f, .18f), materials["CH_Ink"]);
        Material[] blooms =
        {
            materials["CH_CoralBright"], materials["CH_LemonBright"],
            materials["CH_LilacBright"], materials["CH_Pink"],
            materials["CH_MintBright"]
        };
        for (int i = 0; i < 6; i++)
        {
            float x = -.6f + i * .24f;
            AddPrimitivePart(parent, "Bloom_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(x, .74f, (i % 2 == 0 ? .03f : -.03f)),
                Quaternion.identity, Vector3.one * .17f, blooms[i % blooms.Length]);
        }
    }

    private static void BuildBalconyBirdFeederVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Pole", PrimitiveType.Cylinder,
            new Vector3(0f, .6f, 0f), Quaternion.identity,
            new Vector3(.09f, 1.2f, .09f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Arm", PrimitiveType.Cube,
            new Vector3(.12f, 1.14f, 0f), Quaternion.identity,
            new Vector3(.34f, .05f, .05f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "House", PrimitiveType.Cube,
            new Vector3(.26f, .96f, 0f), Quaternion.identity,
            new Vector3(.3f, .26f, .3f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "Roof", PrimitiveType.Cube,
            new Vector3(.26f, 1.12f, 0f), Quaternion.Euler(0f, 45f, 0f),
            new Vector3(.28f, .1f, .28f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "Base", PrimitiveType.Cylinder,
            new Vector3(0f, .04f, 0f), Quaternion.identity,
            new Vector3(.4f, .08f, .4f), materials["CH_TealLight"]);
    }

    private static void BuildBalconyLanternStringVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Cord", PrimitiveType.Cube,
            new Vector3(0f, .2f, 0f), Quaternion.identity,
            new Vector3(1.5f, .03f, .03f), materials["CH_Ink"]);
        Material[] glows =
        {
            materials["CH_LemonBright"], materials["CH_CoralBright"],
            materials["CH_AquaBright"], materials["CH_LilacBright"],
            materials["CH_MintBright"]
        };
        for (int i = 0; i < 5; i++)
        {
            float x = -.6f + i * .3f;
            AddPrimitivePart(parent, "Cap_" + (i + 1), PrimitiveType.Cube,
                new Vector3(x, .19f, 0f), Quaternion.identity,
                new Vector3(.06f, .05f, .06f), materials["CH_Gold"]);
            AddPrimitivePart(parent, "Lantern_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(x, .08f, 0f), Quaternion.identity,
                new Vector3(.16f, .2f, .16f), glows[i % glows.Length]);
        }
    }

    private static void BuildBalconyCushionBenchVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Frame", PrimitiveType.Cube,
            new Vector3(0f, .2f, 0f), Quaternion.identity,
            new Vector3(1.5f, .12f, .5f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "LegL", PrimitiveType.Cube,
            new Vector3(-.66f, .1f, 0f), Quaternion.identity,
            new Vector3(.08f, .2f, .44f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "LegR", PrimitiveType.Cube,
            new Vector3(.66f, .1f, 0f), Quaternion.identity,
            new Vector3(.08f, .2f, .44f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "SeatCushion", PrimitiveType.Cube,
            new Vector3(0f, .32f, 0f), Quaternion.identity,
            new Vector3(1.42f, .12f, .46f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "PillowL", PrimitiveType.Cube,
            new Vector3(-.44f, .46f, -.06f), Quaternion.identity,
            new Vector3(.34f, .22f, .3f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "PillowR", PrimitiveType.Cube,
            new Vector3(.44f, .46f, -.06f), Quaternion.identity,
            new Vector3(.34f, .22f, .3f), materials["CH_LemonBright"]);
    }

    private static void BuildBalconySideTableVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Top", PrimitiveType.Cylinder,
            new Vector3(0f, .46f, 0f), Quaternion.identity,
            new Vector3(.6f, .06f, .6f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "Stem", PrimitiveType.Cylinder,
            new Vector3(0f, .23f, 0f), Quaternion.identity,
            new Vector3(.1f, .46f, .1f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Foot", PrimitiveType.Cylinder,
            new Vector3(0f, .03f, 0f), Quaternion.identity,
            new Vector3(.36f, .06f, .36f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Cup", PrimitiveType.Cylinder,
            new Vector3(.14f, .53f, .06f), Quaternion.identity,
            new Vector3(.14f, .1f, .14f), materials["CH_White"]);
    }

    private static void BuildBalconyHangingChairVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Stand", PrimitiveType.Cube,
            new Vector3(.34f, .77f, 0f), Quaternion.identity,
            new Vector3(.1f, 1.54f, .1f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Foot", PrimitiveType.Cube,
            new Vector3(.16f, .04f, 0f), Quaternion.identity,
            new Vector3(.7f, .08f, .5f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Arm", PrimitiveType.Cube,
            new Vector3(0f, 1.48f, 0f), Quaternion.identity,
            new Vector3(.78f, .08f, .08f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Rope", PrimitiveType.Cylinder,
            new Vector3(-.3f, 1.2f, 0f), Quaternion.identity,
            new Vector3(.03f, .5f, .03f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "Pod", PrimitiveType.Sphere,
            new Vector3(-.3f, .78f, 0f), Quaternion.identity,
            new Vector3(.72f, .78f, .62f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "Cushion", PrimitiveType.Sphere,
            new Vector3(-.3f, .66f, .1f), Quaternion.identity,
            new Vector3(.5f, .3f, .42f), materials["CH_White"]);
    }

    private static void BuildBalconySunAwningVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "BackBar", PrimitiveType.Cube,
            new Vector3(0f, 1.72f, .5f), Quaternion.identity,
            new Vector3(2.3f, .1f, .1f), materials["CH_White"]);
        AddPrimitivePart(parent, "Canopy", PrimitiveType.Cube,
            new Vector3(0f, 1.5f, -.1f), Quaternion.Euler(-22f, 0f, 0f),
            new Vector3(2.3f, .06f, 1.2f), materials["CH_CoralBright"]);
        for (int i = 0; i < 4; i++)
        {
            float x = -.85f + i * .57f;
            AddPrimitivePart(parent, "Stripe_" + (i + 1), PrimitiveType.Cube,
                new Vector3(x, 1.51f, -.1f), Quaternion.Euler(-22f, 0f, 0f),
                new Vector3(.28f, .07f, 1.22f), materials["CH_White"]);
        }
        AddPrimitivePart(parent, "Valance", PrimitiveType.Cube,
            new Vector3(0f, 1.24f, -.66f), Quaternion.identity,
            new Vector3(2.3f, .16f, .05f), materials["CH_LemonBright"]);
    }

    /// <summary>
    /// Routes each Patio product to its own routine.
    ///
    /// Patio v1 shipped as models with no cat behaviour at all except the porch
    /// swing; wave 3 brought the room up to the same contract as the rest of the
    /// house. The swing keeps its ride, wired separately in the prefab builder
    /// because it needs a second FBX under a pivot.
    ///
    /// Orientation is decided per product, never per room. Four of these ten
    /// stand against the back wall or in a corner and three of those are in
    /// `facesBackward`; the rest are yaw 0 floor products where root space
    /// mirrors X and leaves Z alone. The comment on each method says which.
    /// </summary>
    private static void AttachPatioActivity(
        GameObject root, GameObject visual, StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        string id = definition.ProductId;
        if (OutdoorArrangementProfile.IsDecoration(id)) return;
        if (id == HomeStoreService.PatioPergolaArchId)
            AttachArchClimbActivity(root, visual);
        else if (id == HomeStoreService.PatioParasolId)
            AttachParasolScratchActivity(root, visual);
        else if (id == HomeStoreService.PatioDiningSetId)
            AttachDiningPerchActivity(root, visual);
        else if (id == HomeStoreService.PatioFirePitId)
            AttachFirePitBaskActivity(root, visual);
        else if (id == HomeStoreService.PatioWaterFountainId)
            AttachFountainSipActivity(root, visual);
        else if (id == HomeStoreService.PatioPottedFernsId)
            AttachFernWatchActivity(root, visual);
        else if (id == HomeStoreService.PatioHerbTroughId)
            AttachHerbTroughDigActivity(root, visual);
        else if (id == HomeStoreService.PatioStringLightsId)
            AttachFestoonGazeActivity(root, visual);
        else if (id == HomeStoreService.PatioStoneRugId)
            AttachStoneRugKneadActivity(root, visual);
    }

    /// <summary>
    /// The arch crossbeam at an authored 1.62, climbed in two hops.
    ///
    /// The arch IS in `facesBackward` — at yaw 180 against the back wall the
    /// authored front would otherwise point into the wall — so root space flips
    /// Z and the courtyard is at root +Z. Every point below is positive.
    ///
    /// <see cref="PantryClimbActivity"/> refuses hops much over half a metre,
    /// so the lower rail was placed at 0.78 to make the 1.62 beam reachable in
    /// two, rather than the beam being lowered to suit the routine.
    /// </summary>
    private static void AttachArchClimbActivity(GameObject root, GameObject visual)
    {
        const float authoredRail = .780f;
        const float authoredBeam = 1.620f;

        Transform floor = MakePoint(root, "ClimbFloorPoint", new Vector3(0f, 0f, .74f));
        Transform lower = MakePoint(root, "ClimbLowerPoint",
            new Vector3(-.820f, authoredRail, .180f));
        Transform upper = MakePoint(root, "ClimbUpperPoint",
            new Vector3(-.820f, authoredBeam, .060f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .92f));

        PantryClimbActivity activity = root.AddComponent<PantryClimbActivity>();
        activity.EditorConfigure(
            "arch-climb", "PERGOLA ARCH", CatActivityKind.ArchClimb,
            QuestType.PatioClimb, 0, "CLIMB", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioPergolaArchId);
        activity.EditorConfigureClimb(floor, lower, upper, 2.4f);
    }

    /// <summary>
    /// The corded pole band, authored 0.100 to 0.720.
    ///
    /// A parasol pole wrapped in cord IS a scratching post to a cat, which is
    /// why the wrap is modelled over exactly the band a standing cat reaches
    /// instead of running the full height of the pole. Yaw 0 floor product,
    /// absent from `facesBackward`, so root space mirrors X and leaves Z alone;
    /// the courtyard opens at root -Z.
    /// </summary>
    private static void AttachParasolScratchActivity(GameObject root, GameObject visual)
    {
        Transform scratch = MakePoint(root, "ScratchPoint", new Vector3(0f, 0f, -.44f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.62f));

        ScratchPostActivity activity = root.AddComponent<ScratchPostActivity>();
        activity.EditorConfigure(
            "parasol-scratch", "PARASOL", CatActivityKind.ParasolScratch,
            QuestType.Scratch, 0, "SCRATCH", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioParasolId);
        activity.EditorConfigureScratch(scratch, 2.8f);
    }

    /// <summary>
    /// The table top at an authored 0.440, on the half that is kept clear.
    ///
    /// The top carries a parasol hole through its centre and a runner at one
    /// end, so the perch is offset to the clear half — a cat cannot lie on a
    /// hole. The clear half is authored at +X and this product is absent from
    /// `facesBackward`, so root space mirrors it to -X.
    /// </summary>
    private static void AttachDiningPerchActivity(GameObject root, GameObject visual)
    {
        const float authoredTop = .440f;
        const float authoredClearHalf = .440f;

        Transform floor = MakePoint(root, "PerchFloorPoint", new Vector3(0f, 0f, -.94f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(-authoredClearHalf, authoredTop, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -1.06f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "dining-perch", "DINING SET", CatActivityKind.DiningPerch,
            QuestType.Sleep, 0, "NAP", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioDiningSetId);
        activity.EditorConfigurePerch(floor, perch, 3.4f, 20f, 92f, "TABLE NAP!");
    }

    /// <summary>
    /// The open face of the pit, authored at -Z.
    ///
    /// Same beat as the kitchen oven: the cat never touches the product, it
    /// curls up beside the warm side. The spark screen deliberately wraps only
    /// three quarters of the ledge and the gap is authored at -Z, so the bask
    /// point and the gap agree. The pit sits at the left of the courtyard, and
    /// root -Z points back into it, so the cat is not asked to stand in a wall.
    /// </summary>
    private static void AttachFirePitBaskActivity(GameObject root, GameObject visual)
    {
        const float authoredLedge = .400f;

        Transform bask = MakePoint(root, "BaskPoint", new Vector3(0f, 0f, -.72f));
        Transform door = MakePoint(root, "BaskDoorPoint",
            new Vector3(0f, authoredLedge, -.300f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.88f));

        OvenWarmthActivity activity = root.AddComponent<OvenWarmthActivity>();
        activity.EditorConfigure(
            "fire-pit-bask", "FIRE PIT", CatActivityKind.FirePitBask,
            QuestType.Sleep, 0, "WARM UP", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioFirePitId);
        activity.EditorConfigureBask(bask, door, 4.2f, 28f, 92f);
    }

    /// <summary>
    /// The LOW basin rim at an authored 0.300, which is the one a cat drinks
    /// from — never the upper bowl at 0.640.
    ///
    /// The fountain stands at x +2.9, hard against the right of the courtyard,
    /// so the room is at root -X and not at root -Z where a yaw 0 product's
    /// approach usually goes. Yaw alone does not decide this; the position does.
    /// </summary>
    private static void AttachFountainSipActivity(GameObject root, GameObject visual)
    {
        const float authoredBasinRim = .300f;

        Transform floor = MakePoint(root, "SipFloorPoint", new Vector3(-.64f, 0f, 0f));
        Transform perch = MakePoint(root, "SipPerchPoint",
            new Vector3(-.300f, authoredBasinRim, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-.80f, 0f, 0f));

        SinkSipActivity activity = root.AddComponent<SinkSipActivity>();
        activity.EditorConfigure(
            "fountain-sip", "WATER FOUNTAIN", CatActivityKind.FountainSip,
            QuestType.Drink, 0, "DRINK", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioWaterFountainId);
        activity.EditorConfigureSip(floor, perch, 2.4f, 45f, 96f);
    }

    /// <summary>
    /// The fronds at an authored 0.78, watched from the courtyard side.
    ///
    /// The pot has no flat top and no perchable rim on purpose: it is a plant,
    /// not a disguised platform, so the honest beat is a watch. It stands at
    /// x +2.95 against the right of the courtyard, so the approach is at root
    /// -X — the same lesson the fountain teaches two metres away.
    /// </summary>
    private static void AttachFernWatchActivity(GameObject root, GameObject visual)
    {
        Transform look = MakePoint(root, "WatchLookPoint", new Vector3(0f, .780f, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-.66f, 0f, 0f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "fern-watch", "POTTED FERNS", CatActivityKind.FernWatch,
            QuestType.PatioWatch, 0, "WATCH", 1.1f, 3f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioPottedFernsId);
        activity.EditorConfigureLook(look, SitLookReaction.PawSwat, 2.8f, "WIGGLY!");
    }

    /// <summary>
    /// The dug hollow at an authored (-0.430, 0.358).
    ///
    /// The hollow is real geometry — a thrown rim, a dished floor and loose
    /// crumbs — and the soil is a different tone from it, because a dig that
    /// points at a flat matching slab reads as the cat pawing at nothing. The
    /// box lid was lowered to 0.372 so the hollow clears the front rim and can
    /// actually be seen from the room camera.
    ///
    /// The trough IS in `facesBackward`, so root space flips Z and not X: the
    /// hollow keeps its authored -X and the courtyard is at root +Z.
    /// </summary>
    private static void AttachHerbTroughDigActivity(GameObject root, GameObject visual)
    {
        const float authoredSoil = .358f;
        const float authoredHollowX = -.430f;

        Transform mouth = MakePoint(root, "DigMouthPoint",
            new Vector3(authoredHollowX, authoredSoil + .120f, .240f));
        Transform dig = MakePoint(root, "DigPoint",
            new Vector3(authoredHollowX, authoredSoil, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(authoredHollowX, 0f, .58f));

        LitterDigActivity activity = root.AddComponent<LitterDigActivity>();
        activity.EditorConfigure(
            "herb-trough-dig", "HERB TROUGH", CatActivityKind.HerbTroughDig,
            QuestType.LitterDig, 0, "DIG", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioHerbTroughId);
        activity.EditorConfigureDig(mouth, dig, 2.6f, 4);
    }

    /// <summary>
    /// The festoon, looked up at from the courtyard.
    ///
    /// The product now stands on its own posts instead of hanging at 1.95, so
    /// the look point moved with the swag: the wire ties off at 1.900, sags to
    /// 1.660 mid-span and the mid bulb centres at 1.574. Nothing on it is within
    /// a paw's reach — the lowest bulb bottoms out at 1.434 — so the beat stays
    /// a gaze, the same honest limit the balcony awning and lantern string have.
    /// A swat here would be a lie about what the cat can touch.
    ///
    /// It IS in `facesBackward`, so root space flips Z and the courtyard is at
    /// root +Z. The bulbs are authored just off zero at -Z, so they land at
    /// root +Z too — on the courtyard face of the posts, which is the point.
    /// </summary>
    private static void AttachFestoonGazeActivity(GameObject root, GameObject visual)
    {
        Transform look = MakePoint(root, "GazeLookPoint", new Vector3(0f, 1.574f, .010f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .76f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "festoon-gaze", "STRING LIGHTS", CatActivityKind.FestoonGaze,
            QuestType.PatioWatch, 0, "GAZE", 1.1f, 2f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioStringLightsId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.6f, "SO WARM!");
    }

    /// <summary>
    /// The woven runner at an authored 0.068 in the middle of the flagstones.
    ///
    /// The whole product is 0.08 high, so the runner is the only part of it a
    /// cat can register: the routine kneads that panel and not the stone. It
    /// lies in the middle of the courtyard at yaw 0, so root -Z is open floor.
    /// </summary>
    private static void AttachStoneRugKneadActivity(GameObject root, GameObject visual)
    {
        const float authoredRunnerTop = .068f;

        Transform pad = MakePoint(root, "KneadPadPoint",
            new Vector3(0f, authoredRunnerTop, 0f));
        Transform exit = MakePoint(root, "KneadExitPoint", new Vector3(0f, 0f, -.86f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.98f));

        MatKneadActivity activity = root.AddComponent<MatKneadActivity>();
        activity.EditorConfigure(
            "stone-rug-knead", "STONE RUG", CatActivityKind.StoneRugKnead,
            QuestType.MatKnead, 0, "KNEAD", 1.1f, 5f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioStoneRugId);
        activity.EditorConfigureKnead(pad, exit, 6, .34f, 1.4f, 8f);
    }

    private static void BuildProceduralPatioPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
            {
            }
            else if (definition.ProductId == HomeStoreService.PatioStoneRugId)
                BuildPatioStoneRugVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioPottedFernsId)
                BuildPatioPottedFernsVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioHerbTroughId)
                BuildPatioHerbTroughVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioStringLightsId)
                BuildPatioStringLightsVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioWaterFountainId)
                BuildPatioWaterFountainVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioFirePitId)
                BuildPatioFirePitVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioDiningSetId)
                BuildPatioDiningSetVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioParasolId)
                BuildPatioParasolVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioPorchSwingId)
                BuildPatioPorchSwingVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.PatioPergolaArchId)
                BuildPatioPergolaArchVisual(visual.transform, materials);
            else
                throw new InvalidOperationException(
                    "No Patio procedural visual for " + definition.ProductId);

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            if (definition.ProductId == HomeStoreService.PatioPorchSwingId)
                AttachSwingRideActivity(root, visual, materials);
            else
                AttachPatioActivity(root, visual, definition, materials);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// The porch swing is rideable: its product box becomes a trigger, the
    /// premium bench mesh is re-parented under a pivot beneath the top beam so
    /// the seat, chains and cushions rock together, and a scripted activity
    /// hops the cat on and off. The procedural fallback has a single merged
    /// visual and therefore gets no ride.
    /// </summary>
    private static void AttachSwingRideActivity(
        GameObject root,
        GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        // The bench is its own single-object FBX so it can hang under a pivot.
        // A child of the already-instantiated frame prefab could not be
        // re-parented here, and one FBX holding both meshes imports rotated.
        GameObject benchAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "PatioPorchSwingSeat_Premium.fbx");
        if (benchAsset == null)
            return;

        BoxCollider swingCollider = visual.GetComponent<BoxCollider>();
        if (swingCollider != null)
            swingCollider.isTrigger = true;

        Transform pivot = new GameObject("SwingPivot").transform;
        pivot.SetParent(visual.transform, false);
        pivot.localPosition = new Vector3(0f, 1.39f, 0f);

        GameObject bench = PrefabUtility.InstantiatePrefab(benchAsset) as GameObject;
        if (bench == null)
            return;
        bench.name = "PorchSwingSeat";
        bench.transform.SetParent(pivot, false);
        // Authored on the same origin as the frame, so undo the pivot offset.
        bench.transform.localPosition = new Vector3(0f, -1.39f, 0f);
        bench.transform.localRotation = Quaternion.identity;
        bench.transform.localScale = Vector3.one;
        RemoveChildColliders(bench);
        ReplaceMaterials(bench, materials);

        Transform seatPoint = new GameObject("SwingSeatPoint").transform;
        seatPoint.SetParent(visual.transform, false);
        seatPoint.localPosition = new Vector3(0f, .74f, -.02f);
        seatPoint.SetParent(pivot, true);

        Transform mount = new GameObject("SwingMountPoint").transform;
        mount.SetParent(root.transform, false);
        mount.localPosition = new Vector3(0f, 0f, -.62f);
        Transform anchor = new GameObject("InteractionAnchor").transform;
        anchor.SetParent(root.transform, false);
        anchor.localPosition = new Vector3(0f, 0f, -.85f);

        SwingRideActivity activity = root.AddComponent<SwingRideActivity>();
        activity.EditorConfigure(
            "swing-ride",
            "PORCH SWING",
            CatActivityKind.SwingRide,
            QuestType.SwingRide,
            0,
            "SWING",
            1.1f,
            6f,
            anchor,
            null,
            visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.PatioPorchSwingId);
        activity.EditorConfigureSwing(pivot, mount, seatPoint, 4.2f, 9f, 2.3f, 4f);
    }

    private static void BuildPatioStoneRugVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "RugBase", PrimitiveType.Cube,
            new Vector3(0f, .04f, 0f), Quaternion.identity,
            new Vector3(1.92f, .06f, 1.12f), materials["CH_TealLight"]);
        AddPrimitivePart(parent, "RugBorder", PrimitiveType.Cube,
            new Vector3(0f, .05f, 0f), Quaternion.identity,
            new Vector3(1.6f, .05f, .84f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "RugDiamond", PrimitiveType.Cube,
            new Vector3(0f, .07f, 0f), Quaternion.Euler(0f, 45f, 0f),
            new Vector3(.5f, .03f, .5f), materials["CH_CoralBright"]);
    }

    private static void BuildPatioPottedFernsVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -.24f : .24f;
            AddPrimitivePart(parent, "Planter_" + i, PrimitiveType.Cylinder,
                new Vector3(x, .26f, 0f), Quaternion.identity,
                new Vector3(.28f, .52f, .28f), materials["CH_Cream"]);
            AddPrimitivePart(parent, "FernLow_" + i, PrimitiveType.Sphere,
                new Vector3(x, .66f, 0f), Quaternion.identity,
                new Vector3(.52f, .5f, .52f), materials["CH_MintBright"]);
            AddPrimitivePart(parent, "FernHigh_" + i, PrimitiveType.Sphere,
                new Vector3(x + .06f, .95f, -.05f), Quaternion.identity,
                new Vector3(.34f, .42f, .34f), materials["CH_TealLight"]);
        }
    }

    private static void BuildPatioHerbTroughVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Trough", PrimitiveType.Cube,
            new Vector3(0f, .2f, 0f), Quaternion.identity,
            new Vector3(1.5f, .3f, .3f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "Soil", PrimitiveType.Cube,
            new Vector3(0f, .34f, 0f), Quaternion.identity,
            new Vector3(1.44f, .06f, .24f), materials["CH_Ink"]);
        Material[] herbs = { materials["CH_MintBright"], materials["CH_LilacBright"] };
        for (int i = 0; i < 7; i++)
        {
            float x = -.6f + i * .2f;
            AddPrimitivePart(parent, "Herb_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(x, .46f, (i % 2 == 0 ? .03f : -.03f)),
                Quaternion.identity, Vector3.one * .16f, herbs[i % herbs.Length]);
        }
    }

    private static void BuildPatioStringLightsVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        // Stands on its own posts, like the premium model it stands in for.
        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? -.6f : .6f;
            AddPrimitivePart(parent, "Post_" + (side + 1), PrimitiveType.Cube,
                new Vector3(x, 1f, 0f), Quaternion.identity,
                new Vector3(.11f, 2f, .11f), materials["CH_Cream"]);
            AddPrimitivePart(parent, "Finial_" + (side + 1), PrimitiveType.Sphere,
                new Vector3(x, 2.05f, 0f), Quaternion.identity,
                Vector3.one * .1f, materials["CH_Gold"]);
        }

        AddPrimitivePart(parent, "Cord", PrimitiveType.Cube,
            new Vector3(0f, 1.78f, -.01f), Quaternion.identity,
            new Vector3(1.2f, .03f, .03f), materials["CH_Ink"]);
        for (int i = 0; i < 6; i++)
        {
            float x = -.45f + i * .18f;
            AddPrimitivePart(parent, "Bulb_" + (i + 1), PrimitiveType.Sphere,
                new Vector3(x, 1.68f, -.01f), Quaternion.identity,
                Vector3.one * .14f, materials["CH_LemonBright"]);
        }
    }

    private static void BuildPatioWaterFountainVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "BasinLow", PrimitiveType.Cylinder,
            new Vector3(0f, .12f, 0f), Quaternion.identity,
            new Vector3(.78f, .24f, .78f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "WaterLow", PrimitiveType.Cylinder,
            new Vector3(0f, .22f, 0f), Quaternion.identity,
            new Vector3(.64f, .04f, .64f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "Stem", PrimitiveType.Cylinder,
            new Vector3(0f, .42f, 0f), Quaternion.identity,
            new Vector3(.14f, .4f, .14f), materials["CH_White"]);
        AddPrimitivePart(parent, "BasinTop", PrimitiveType.Cylinder,
            new Vector3(0f, .6f, 0f), Quaternion.identity,
            new Vector3(.42f, .12f, .42f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "WaterTop", PrimitiveType.Cylinder,
            new Vector3(0f, .66f, 0f), Quaternion.identity,
            new Vector3(.3f, .03f, .3f), materials["CH_AquaBright"]);
        AddPrimitivePart(parent, "Spout", PrimitiveType.Sphere,
            new Vector3(0f, .78f, 0f), Quaternion.identity,
            Vector3.one * .1f, materials["CH_AquaBright"]);
    }

    private static void BuildPatioFirePitVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Ring", PrimitiveType.Cylinder,
            new Vector3(0f, .2f, 0f), Quaternion.identity,
            new Vector3(.82f, .4f, .82f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "Bowl", PrimitiveType.Cylinder,
            new Vector3(0f, .32f, 0f), Quaternion.identity,
            new Vector3(.64f, .1f, .64f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "Ember", PrimitiveType.Cylinder,
            new Vector3(0f, .38f, 0f), Quaternion.identity,
            new Vector3(.5f, .04f, .5f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "FlameLow", PrimitiveType.Sphere,
            new Vector3(0f, .5f, 0f), Quaternion.identity,
            new Vector3(.34f, .42f, .34f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "FlameHigh", PrimitiveType.Sphere,
            new Vector3(.04f, .66f, -.02f), Quaternion.identity,
            new Vector3(.2f, .28f, .2f), materials["CH_LemonBright"]);
    }

    private static void BuildPatioDiningSetVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "TableTop", PrimitiveType.Cylinder,
            new Vector3(0f, .52f, 0f), Quaternion.identity,
            new Vector3(.86f, .06f, .86f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "TableStem", PrimitiveType.Cylinder,
            new Vector3(0f, .26f, 0f), Quaternion.identity,
            new Vector3(.12f, .52f, .12f), materials["CH_White"]);
        Vector3[] chairs =
        {
            new Vector3(0f, 0f, .62f), new Vector3(0f, 0f, -.62f),
            new Vector3(.62f, 0f, 0f), new Vector3(-.62f, 0f, 0f)
        };
        Material[] seatColors =
        {
            materials["CH_CoralBright"], materials["CH_MintBright"],
            materials["CH_LemonBright"], materials["CH_LilacBright"]
        };
        for (int i = 0; i < chairs.Length; i++)
        {
            AddPrimitivePart(parent, "Seat_" + (i + 1), PrimitiveType.Cube,
                chairs[i] + new Vector3(0f, .3f, 0f), Quaternion.identity,
                new Vector3(.3f, .08f, .3f), seatColors[i]);
            Vector3 backOffset = chairs[i].normalized * .13f;
            AddPrimitivePart(parent, "Back_" + (i + 1), PrimitiveType.Cube,
                chairs[i] + backOffset + new Vector3(0f, .48f, 0f),
                Quaternion.LookRotation(chairs[i].normalized == Vector3.zero
                    ? Vector3.forward : chairs[i].normalized),
                new Vector3(.3f, .3f, .06f), seatColors[i]);
        }
    }

    private static void BuildPatioParasolVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Base", PrimitiveType.Cylinder,
            new Vector3(0f, .06f, 0f), Quaternion.identity,
            new Vector3(.44f, .12f, .44f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "Pole", PrimitiveType.Cylinder,
            new Vector3(0f, .9f, 0f), Quaternion.identity,
            new Vector3(.08f, 1.8f, .08f), materials["CH_White"]);
        AddPrimitivePart(parent, "CanopyCore", PrimitiveType.Cylinder,
            new Vector3(0f, 1.66f, 0f), Quaternion.identity,
            new Vector3(2.1f, .12f, 2.1f), materials["CH_MintBright"]);
        for (int i = 0; i < 6; i++)
        {
            float ang = i / 6f * Mathf.PI * 2f;
            AddPrimitivePart(parent, "Panel_" + (i + 1), PrimitiveType.Cube,
                new Vector3(Mathf.Cos(ang) * .52f, 1.62f, Mathf.Sin(ang) * .52f),
                Quaternion.Euler(0f, -ang * Mathf.Rad2Deg, 0f),
                new Vector3(.5f, .05f, 1.02f),
                i % 2 == 0 ? materials["CH_White"] : materials["CH_TealLight"]);
        }
        AddPrimitivePart(parent, "Finial", PrimitiveType.Sphere,
            new Vector3(0f, 1.82f, 0f), Quaternion.identity,
            Vector3.one * .12f, materials["CH_Gold"]);
    }

    private static void BuildPatioPorchSwingVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "FrameL", PrimitiveType.Cube,
            new Vector3(-.72f, .74f, 0f), Quaternion.Euler(0f, 0f, 10f),
            new Vector3(.1f, 1.48f, .1f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "FrameR", PrimitiveType.Cube,
            new Vector3(.72f, .74f, 0f), Quaternion.Euler(0f, 0f, -10f),
            new Vector3(.1f, 1.48f, .1f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "TopBar", PrimitiveType.Cube,
            new Vector3(0f, 1.46f, 0f), Quaternion.identity,
            new Vector3(1.6f, .1f, .12f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "ChainL", PrimitiveType.Cylinder,
            new Vector3(-.5f, 1.1f, 0f), Quaternion.identity,
            new Vector3(.03f, .5f, .03f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "ChainR", PrimitiveType.Cylinder,
            new Vector3(.5f, 1.1f, 0f), Quaternion.identity,
            new Vector3(.03f, .5f, .03f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "Seat", PrimitiveType.Cube,
            new Vector3(0f, .82f, 0f), Quaternion.identity,
            new Vector3(1.2f, .1f, .46f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "SeatCushion", PrimitiveType.Cube,
            new Vector3(0f, .9f, 0f), Quaternion.identity,
            new Vector3(1.12f, .1f, .42f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "BackCushion", PrimitiveType.Cube,
            new Vector3(0f, 1.06f, -.2f), Quaternion.identity,
            new Vector3(1.12f, .3f, .1f), materials["CH_MintBright"]);
    }

    private static void BuildPatioPergolaArchVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "PostL", PrimitiveType.Cube,
            new Vector3(-1.0f, .85f, 0f), Quaternion.identity,
            new Vector3(.14f, 1.7f, .14f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "PostR", PrimitiveType.Cube,
            new Vector3(1.0f, .85f, 0f), Quaternion.identity,
            new Vector3(.14f, 1.7f, .14f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "ArchTop", PrimitiveType.Cube,
            new Vector3(0f, 1.74f, 0f), Quaternion.identity,
            new Vector3(2.2f, .14f, .3f), materials["CH_Cream"]);
        for (int i = 0; i < 4; i++)
        {
            float x = -.75f + i * .5f;
            AddPrimitivePart(parent, "Slat_" + (i + 1), PrimitiveType.Cube,
                new Vector3(x, 1.82f, 0f), Quaternion.identity,
                new Vector3(.08f, .05f, .5f), materials["CH_MintBright"]);
        }
        AddPrimitivePart(parent, "VineL", PrimitiveType.Sphere,
            new Vector3(-1.0f, 1.5f, .1f), Quaternion.identity,
            new Vector3(.4f, .6f, .4f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "VineR", PrimitiveType.Sphere,
            new Vector3(1.0f, 1.3f, -.1f), Quaternion.identity,
            new Vector3(.42f, .7f, .42f), materials["CH_TealLight"]);
        AddPrimitivePart(parent, "BloomL", PrimitiveType.Sphere,
            new Vector3(-.9f, 1.7f, .16f), Quaternion.identity,
            Vector3.one * .16f, materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "BloomR", PrimitiveType.Sphere,
            new Vector3(.92f, 1.5f, -.16f), Quaternion.identity,
            Vector3.one * .16f, materials["CH_CoralBright"]);
    }

    /// <summary>
    /// Routes each Second Floor product to its own routine.
    ///
    /// The loft shipped as ten pieces of furniture with no cat behaviour at
    /// all; wave 3 gives every one of them a beat. Nine reuse a shared activity
    /// class with their own <see cref="CatActivityKind"/>, which is why the
    /// validator and the tests look them up by KIND — a `GetComponent` would
    /// return whichever instance the room happened to build first.
    ///
    /// Orientation is decided per product. Two stand against the back wall and
    /// are in `facesBackward`; three stand hard against a side of the loft and
    /// are approached across X even though their yaw is 0; the rest are open
    /// floor products approached from root -Z.
    /// </summary>
    private static void AttachLoftActivity(
        GameObject root, GameObject visual, StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        string id = definition.ProductId;
        if (id == HomeStoreService.LoftFloorRunnerId)
            AttachRunnerKneadActivity(root, visual);
        else if (id == HomeStoreService.LoftFloorCushionsId)
            AttachCushionNestActivity(root, visual);
        else if (id == HomeStoreService.LoftBookStackId)
            AttachBookKnockOffActivity(root, visual, materials);
        else if (id == HomeStoreService.LoftArcLampId)
            AttachLampGlowBaskActivity(root, visual);
        else if (id == HomeStoreService.LoftBeanBagId)
            AttachBeanBagNapActivity(root, visual);
        else if (id == HomeStoreService.LoftRecordPlayerId)
            AttachRecordSpinActivity(root, visual, materials);
        else if (id == HomeStoreService.LoftStudyDeskId)
            AttachDeskPerchActivity(root, visual);
        else if (id == HomeStoreService.LoftWallGalleryId)
            AttachGalleryGazeActivity(root, visual);
        else if (id == HomeStoreService.LoftTallBookcaseId)
            AttachBookcaseClimbActivity(root, visual);
        else if (id == HomeStoreService.LoftChaiseLoungeId)
            AttachChaiseNapActivity(root, visual);
    }

    /// <summary>
    /// The raised centre pad at an authored 0.072.
    ///
    /// The whole runner is 0.08 high, so the pad is the only part of it a cat
    /// can register at all — the routine kneads that panel, not the weave
    /// around it. Yaw 0 floor product in the middle of the loft, so root -Z is
    /// open floor and the cat leaves that way.
    /// </summary>
    private static void AttachRunnerKneadActivity(GameObject root, GameObject visual)
    {
        const float authoredPadTop = .072f;

        Transform pad = MakePoint(root, "KneadPadPoint",
            new Vector3(0f, authoredPadTop, 0f));
        Transform exit = MakePoint(root, "KneadExitPoint", new Vector3(0f, 0f, -.88f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -1.00f));

        MatKneadActivity activity = root.AddComponent<MatKneadActivity>();
        activity.EditorConfigure(
            "runner-knead", "FLOOR RUNNER", CatActivityKind.RunnerKnead,
            QuestType.MatKnead, 0, "KNEAD", 1.1f, 5f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftFloorRunnerId);
        activity.EditorConfigureKnead(pad, exit, 6, .34f, 1.4f, 8f);
    }

    /// <summary>
    /// The dished top cushion at an authored 0.330.
    ///
    /// <see cref="TowelNestActivity"/> drops the cat INTO a hollow rather than
    /// onto a flat top, so the top cushion is dished with a real rim standing
    /// proud of it. A flat stack would have made the nest a lie.
    ///
    /// The stack sits at x +2.9, hard against the right of the loft, so the cat
    /// approaches across root -X. Yaw is 0 and that decides nothing here — the
    /// position does.
    /// </summary>
    private static void AttachCushionNestActivity(GameObject root, GameObject visual)
    {
        const float authoredNestFloor = .330f;

        Transform floor = MakePoint(root, "NestFloorPoint", new Vector3(-.62f, 0f, 0f));
        Transform nest = MakePoint(root, "NestPoint",
            new Vector3(0f, authoredNestFloor, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-.78f, 0f, 0f));

        TowelNestActivity activity = root.AddComponent<TowelNestActivity>();
        activity.EditorConfigure(
            "cushion-nest", "FLOOR CUSHIONS", CatActivityKind.CushionNest,
            QuestType.Sleep, 0, "NEST", 1.1f, 8f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftFloorCushionsId);
        activity.EditorConfigureNest(floor, nest, 3.6f, 24f, 92f);
    }

    /// <summary>
    /// The loose volume on top of the stack, at an authored 0.490.
    ///
    /// The stack's own board tops out at 0.454 and is left clear on purpose:
    /// the book that falls is a second single-object FBX on the same origin,
    /// hung under its own pivot here. Its mesh is authored on the PRODUCT
    /// origin, so the child has to be pushed back by the pivot offset — left at
    /// zero the book would sit at twice the offset.
    ///
    /// The stack is at x +2.9 against the right of the loft, so the cat reaches
    /// from root -X and the book is pushed that way too.
    /// </summary>
    private static void AttachBookKnockOffActivity(
        GameObject root, GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject bookAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "LoftBookStackBook_Premium.fbx");
        if (bookAsset == null)
            return;

        var axis = new Vector3(0f, .490f, 0f);

        Transform pivot = new GameObject("BookPivot").transform;
        pivot.SetParent(visual.transform, false);
        pivot.localPosition = axis;

        GameObject book = PrefabUtility.InstantiatePrefab(bookAsset) as GameObject;
        if (book == null)
            return;
        book.name = "Book";
        book.transform.SetParent(pivot, false);
        book.transform.localPosition = -axis;
        book.transform.localRotation = Quaternion.identity;
        RemoveChildColliders(book);
        ReplaceMaterials(book, materials);

        Transform reach = MakePoint(root, "KnockReachPoint", new Vector3(-.44f, 0f, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-.60f, 0f, 0f));

        KnockOffActivity activity = root.AddComponent<KnockOffActivity>();
        activity.EditorConfigure(
            "book-knock-off", "BOOK STACK", CatActivityKind.BookKnockOff,
            QuestType.KnockOff, 0, "PUSH", 1.1f, 4f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftBookStackId);
        activity.EditorConfigureKnock(reach, pivot, Vector3.left, .44f, 2);
    }

    /// <summary>
    /// The pool of light beside the lamp.
    ///
    /// Same beat as the kitchen oven and the patio fire pit: the cat never
    /// touches the product, it settles in the warm spot next to it. The pool is
    /// deliberately not modelled — with a 0.35 half-footprint any pool wide
    /// enough to lie in would be inside the lamp's own base — so the bask point
    /// sits on the floor outside the product box and the shade leans over it.
    ///
    /// The lamp stands at x -3.2 against the LEFT of the loft, so the room is
    /// at root +X. The shade is authored leaning to -X and this product is not
    /// in `facesBackward`, so the overhang lands at root +X, over the cat.
    /// </summary>
    private static void AttachLampGlowBaskActivity(GameObject root, GameObject visual)
    {
        const float authoredShadeX = -.196f;
        const float authoredShadeY = 1.560f;

        Transform bask = MakePoint(root, "BaskPoint", new Vector3(.62f, 0f, 0f));
        Transform door = MakePoint(root, "BaskDoorPoint",
            new Vector3(-authoredShadeX, authoredShadeY - .080f, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(.78f, 0f, 0f));

        OvenWarmthActivity activity = root.AddComponent<OvenWarmthActivity>();
        activity.EditorConfigure(
            "lamp-glow-bask", "ARC FLOOR LAMP", CatActivityKind.LampGlowBask,
            QuestType.Sleep, 0, "WARM UP", 1.1f, 0f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftArcLampId);
        activity.EditorConfigureBask(bask, door, 4.2f, 26f, 92f);
    }

    /// <summary>
    /// The seat dish at an authored 0.330.
    ///
    /// The bag is modelled with a real hollow and a back that slumps up on the
    /// FAR side only, so the cat's approach at root -Z is not fenced off. A
    /// bean bag modelled as one smooth blob would give the nap nowhere to
    /// happen.
    /// </summary>
    private static void AttachBeanBagNapActivity(GameObject root, GameObject visual)
    {
        const float authoredSeat = .330f;

        Transform floor = MakePoint(root, "PerchFloorPoint", new Vector3(0f, 0f, -.70f));
        Transform perch = MakePoint(root, "PerchPoint", new Vector3(0f, authoredSeat, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, -.86f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "bean-bag-nap", "BEAN BAG CHAIR", CatActivityKind.BeanBagNap,
            QuestType.Sleep, 0, "NAP", 1.1f, 8f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftBeanBagId);
        activity.EditorConfigurePerch(floor, perch, 4.0f, 30f, 92f, "SO SINKY!");
    }

    /// <summary>
    /// The record on the platter, at an authored (-0.130, 0.400).
    ///
    /// The loft's second moving part and the second product here that ships as
    /// two FBX files. The platter well is left clear so the record can spin
    /// without intersecting anything, the tonearm is swung off to the far
    /// corner for the same reason, and the dust lid is propped OPEN because a
    /// closed lid would hide the one part the player watches move.
    ///
    /// Yaw 0 floor product absent from `facesBackward`, so the authored -X
    /// platter lands at root +X and every point below follows it.
    /// </summary>
    private static void AttachRecordSpinActivity(
        GameObject root, GameObject visual,
        IReadOnlyDictionary<string, Material> materials)
    {
        GameObject discAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "LoftRecordPlayerDisc_Premium.fbx");
        if (discAsset == null)
            return;

        const float authoredDiscX = -.130f;
        var axis = new Vector3(-authoredDiscX, .400f, 0f);

        Transform pivot = new GameObject("RecordPivot").transform;
        pivot.SetParent(visual.transform, false);
        pivot.localPosition = axis;

        GameObject disc = PrefabUtility.InstantiatePrefab(discAsset) as GameObject;
        if (disc == null)
            return;
        disc.name = "Record";
        disc.transform.SetParent(pivot, false);
        // The disc mesh is authored on the console's origin, so the child has to
        // cancel the pivot offset or it lands at twice it.
        disc.transform.localPosition = -axis;
        disc.transform.localRotation = Quaternion.identity;
        RemoveChildColliders(disc);
        ReplaceMaterials(disc, materials);

        Transform swat = MakePoint(root, "SwatPoint",
            new Vector3(-authoredDiscX, 0f, -.52f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-authoredDiscX, 0f, -.68f));

        PaperSpinActivity activity = root.AddComponent<PaperSpinActivity>();
        activity.EditorConfigure(
            "record-spin", "RECORD PLAYER", CatActivityKind.RecordSpin,
            QuestType.PaperSpin, 0, "SWAT", 1.1f, 5f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftRecordPlayerId);
        activity.EditorConfigureSpin(pivot, swat, 3, 540f, 1.2f);
    }

    /// <summary>
    /// The clear half of the desk top, at an authored 0.560.
    ///
    /// A cat on a desk is the canonical version of this beat, so one half of
    /// the top is kept empty: the lamp, the books, the mug and the paper tray
    /// all live on the authored +X half, which root space mirrors to -X, and
    /// the perch sits on the other one. The top is at 0.560 rather than a
    /// realistic 0.72 because the contract is 0.80 tall and the clutter has to
    /// fit under it — the bedroom nightstand paid the same bill.
    /// </summary>
    private static void AttachDeskPerchActivity(GameObject root, GameObject visual)
    {
        const float authoredTop = .560f;
        const float authoredClearHalf = -.400f;

        Transform floor = MakePoint(root, "PerchFloorPoint", new Vector3(0f, 0f, -.86f));
        Transform perch = MakePoint(root, "PerchPoint",
            new Vector3(-authoredClearHalf, authoredTop, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(-authoredClearHalf, 0f, -1.00f));

        PerchNapActivity activity = root.AddComponent<PerchNapActivity>();
        activity.EditorConfigure(
            "desk-perch", "STUDY DESK", CatActivityKind.DeskPerch,
            QuestType.Sleep, 0, "NAP", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftStudyDeskId);
        activity.EditorConfigurePerch(floor, perch, 3.2f, 18f, 92f, "MY DESK!");
    }

    /// <summary>
    /// The picture wall, looked up at from the floor.
    ///
    /// Hung at 1.55 with a 0.90 box, so the lowest frame sits near 1.60 and
    /// nothing here is within a paw's reach. The beat is a gaze — the same
    /// honest limit the balcony awning, the balcony lantern string and the
    /// patio festoon have. A swat would be a lie about what the cat can touch.
    ///
    /// It IS in `facesBackward`, so root space flips Z and the room is at root
    /// +Z while the art plane, authored at -0.060, lands at root +0.060.
    /// </summary>
    private static void AttachGalleryGazeActivity(GameObject root, GameObject visual)
    {
        Transform look = MakePoint(root, "GazeLookPoint", new Vector3(0f, .300f, .060f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .72f));

        SitLookActivity activity = root.AddComponent<SitLookActivity>();
        activity.EditorConfigure(
            "gallery-gaze", "WALL GALLERY", CatActivityKind.GalleryGaze,
            QuestType.LoftWatch, 0, "GAZE", 1.1f, 2f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftWallGalleryId);
        activity.EditorConfigureLook(look, SitLookReaction.Sit, 2.6f, "SO ARTY!");
    }

    /// <summary>
    /// Shelves at an authored 0.520 and 1.040, sized against the routine.
    ///
    /// <see cref="PantryClimbActivity"/> stages exactly two hops and refuses
    /// much more than half a metre each, so the two lowest shelves were placed
    /// to that limit rather than the other way round. Both landings are kept
    /// CLEAR across their middle — the books on those shelves are pushed to the
    /// ends — because that is where the cat puts its feet.
    ///
    /// The bookcase IS in `facesBackward`, so root space flips Z and not X: the
    /// clear gaps keep their authored X of 0 and the room is at root +Z.
    /// </summary>
    private static void AttachBookcaseClimbActivity(GameObject root, GameObject visual)
    {
        const float authoredLowShelf = .520f;
        const float authoredMidShelf = 1.040f;

        Transform floor = MakePoint(root, "ClimbFloorPoint", new Vector3(0f, 0f, .62f));
        Transform lower = MakePoint(root, "ClimbLowerPoint",
            new Vector3(0f, authoredLowShelf, .100f));
        Transform upper = MakePoint(root, "ClimbUpperPoint",
            new Vector3(0f, authoredMidShelf, .060f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(0f, 0f, .80f));

        PantryClimbActivity activity = root.AddComponent<PantryClimbActivity>();
        activity.EditorConfigure(
            "bookcase-climb", "TALL BOOKCASE", CatActivityKind.BookcaseClimb,
            QuestType.PantryClimb, 0, "CLIMB", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftTallBookcaseId);
        activity.EditorConfigureClimb(floor, lower, upper, 2.4f);
    }

    /// <summary>
    /// The seat pocket between the bolster and the open foot end.
    ///
    /// <see cref="CanopyNapActivity"/> walks the cat in at an opening and
    /// settles it in a nest, so the product box becomes a trigger. The chaise
    /// gives that honestly: the scrolled head is the enclosure, the bolster in
    /// front of it is the pillow, and the foot end is deliberately left clear as
    /// the way in — a bolster laid across the foot would barricade the only
    /// opening the routine has.
    ///
    /// The head is authored at +X and this product is not in `facesBackward`,
    /// so the head lands at root -X and the way in is at root +X.
    /// </summary>
    private static void AttachChaiseNapActivity(GameObject root, GameObject visual)
    {
        BoxCollider chaiseCollider = visual.GetComponent<BoxCollider>();
        if (chaiseCollider != null)
            chaiseCollider.isTrigger = true;

        const float authoredSeat = .320f;

        Transform door = MakePoint(root, "NapDoorPoint", new Vector3(.740f, 0f, -.44f));
        Transform nest = MakePoint(root, "NapNestPoint",
            new Vector3(-.250f, authoredSeat, 0f));
        Transform anchor = MakePoint(root, "InteractionAnchor",
            new Vector3(.620f, 0f, -.66f));

        CanopyNapActivity activity = root.AddComponent<CanopyNapActivity>();
        activity.EditorConfigure(
            "chaise-nap", "CHAISE LOUNGE", CatActivityKind.ChaiseNap,
            QuestType.Sleep, 0, "NAP", 1.1f, 8f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.LoftChaiseLoungeId);
        activity.EditorConfigureNap(door, nest, 4.4f, 34f, 94f);
    }

    private static void BuildProceduralLoftPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (TryBuildPremiumFurnitureVisual(visual.transform, definition, materials))
            {
                // Blender-authored hero furniture uses the same footprint,
                // placement and economy contract as the procedural fallback.
            }
            else if (definition.ProductId == HomeStoreService.LoftFloorRunnerId)
                BuildLoftFloorRunnerVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftFloorCushionsId)
                BuildLoftFloorCushionsVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftBookStackId)
                BuildLoftBookStackVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftArcLampId)
                BuildLoftArcLampVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftBeanBagId)
                BuildLoftBeanBagVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftRecordPlayerId)
                BuildLoftRecordPlayerVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftStudyDeskId)
                BuildLoftStudyDeskVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftWallGalleryId)
                BuildLoftWallGalleryVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftTallBookcaseId)
                BuildLoftTallBookcaseVisual(visual.transform, materials);
            else if (definition.ProductId == HomeStoreService.LoftChaiseLoungeId)
                BuildLoftChaiseLoungeVisual(visual.transform, materials);
            else
                throw new InvalidOperationException(
                    "No Loft procedural visual for " + definition.ProductId);

            AddProductCollider(visual, definition);
            ConfigureProductComponents(root, visual, definition);
            AttachLoftActivity(root, visual, definition, materials);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static bool TryBuildPremiumFurnitureVisual(
        Transform parent,
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        string fileName = null;
        if (definition.ProductId == HomeStoreService.LoftFloorRunnerId)
            fileName = "LoftFloorRunner_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftFloorCushionsId)
            fileName = "LoftFloorCushions_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftBookStackId)
            fileName = "LoftBookStack_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftWallGalleryId)
            fileName = "LoftWallGallery_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftArcLampId)
            fileName = "LoftArcLamp_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftBeanBagId)
            fileName = "LoftBeanBag_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftRecordPlayerId)
            fileName = "LoftRecordPlayer_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftStudyDeskId)
            fileName = "LoftStudyDesk_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftTallBookcaseId)
            fileName = "LoftTallBookcase_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.LoftChaiseLoungeId)
            fileName = "LoftChaiseLounge_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconyCushionBenchId)
            fileName = "BalconyCushionBench_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconyHangingChairId)
            fileName = "BalconyHangingChair_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconySunAwningId)
            fileName = "BalconySunAwning_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconyHerbShelfId)
            fileName = "BalconyHerbShelf_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconyBirdFeederId)
            fileName = "BalconyBirdFeeder_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconySideTableId)
            fileName = "BalconySideTable_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconySunMatId)
            fileName = "BalconySunMat_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconyPlanterBoxId)
            fileName = "BalconyPlanterBox_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconyRailingFlowersId)
            fileName = "BalconyRailingFlowers_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BalconyLanternStringId)
            fileName = "BalconyLanternString_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioWaterFountainId)
            fileName = "PatioWaterFountain_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioFirePitId)
            fileName = "PatioFirePit_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioParasolId)
            fileName = "PatioParasol_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioDiningSetId)
            fileName = "PatioDiningSet_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioStoneRugId)
            fileName = "PatioStoneRug_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioPottedFernsId)
            fileName = "PatioPottedFerns_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioHerbTroughId)
            fileName = "PatioHerbTrough_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioStringLightsId)
            fileName = "PatioStringLights_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioPergolaArchId)
            fileName = "PatioPergolaArch_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.PatioPorchSwingId)
            fileName = "PatioPorchSwing_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomTubId)
            fileName = "BathroomTub_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomVanityId)
            fileName = "BathroomVanitySink_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomToiletId)
            fileName = "BathroomToilet_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomShowerId)
            fileName = "BathroomShower_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomTowelStorageId)
            fileName = "BathroomTowelStorage_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomLitterBoxId)
            fileName = "BathroomLitterBox_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomGroomingCartId)
            fileName = "BathroomGroomingCart_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomLaundryHamperId)
            fileName = "BathroomLaundryHamper_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomMirrorId)
            fileName = "BathroomWallMirror_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BathroomBathMatId)
            fileName = "BathroomBathMat_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenDishCartId)
            fileName = "KitchenDishCart_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenCounterStoolId)
            fileName = "KitchenCounterStool_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenFruitBasketId)
            fileName = "KitchenFruitBasket_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenFeedingStationId)
            fileName = "KitchenFeedingStation_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenPawMatId)
            fileName = "KitchenPawMat_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomNightstandId)
            fileName = "BedroomNightstand_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomDreamArtId)
            fileName = "BedroomDreamArt_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomNightLightId)
            fileName = "BedroomNightLight_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomYarnBasketId)
            fileName = "BedroomYarnBasket_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomVanityStoolId)
            fileName = "BedroomVanityStool_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomPawRugId)
            fileName = "BedroomPawRug_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenIslandId)
            fileName = "KitchenIsland_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenRefrigeratorId)
            fileName = "KitchenRefrigerator_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenStoveOvenId)
            fileName = "KitchenStoveOven_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenPantryShelfId)
            fileName = "KitchenPantryShelf_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.KitchenSinkCabinetId)
            fileName = "KitchenSinkCabinet_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BookshelfId)
            fileName = "TallBookshelf_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.ArmchairId)
            fileName = "ClassicArmchair_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.FloorLampId)
            fileName = "FloorLamp_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.TallPlantId)
            fileName = "TallHouseplant_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.TvUnitId)
            fileName = "TvUnit_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.ModernTelevisionId)
            fileName = "ModernTelevision_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.StereoId)
            fileName = "SpeakerSystem_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.ModernPaintingId)
            fileName = "ModernPainting_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GameConsoleId)
            fileName = "GameConsoleSet_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomQueenBedId)
            fileName = "BedroomQueenBed_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomWardrobeId)
            fileName = "BedroomWardrobe_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomWindowDaybedId)
            fileName = "BedroomWindowDaybed_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.BedroomStarCanopyId)
            fileName = "BedroomStarCanopy_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenPergolaId)
            fileName = "GardenPergola_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenSaplingId)
            fileName = "GardenSapling_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenBistroSetId)
            fileName = "GardenBistroSet_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenSunLoungerId)
            fileName = "GardenSunLounger_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenHammockId)
            fileName = "GardenHammock_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenGrillId)
            fileName = "GardenGrill_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenBirdBathId)
            fileName = "GardenBirdBath_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenFlowerPotsId)
            fileName = "GardenFlowerPots_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenDaisyBedId)
            fileName = "GardenDaisyBed_Premium.fbx";
        else if (definition.ProductId == HomeStoreService.GardenYarnBallId)
            fileName = "GardenYarnBall_Premium.fbx";

        if (string.IsNullOrEmpty(fileName))
            return false;

        string path = PremiumFurnitureModelFolder + fileName;
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            return false;

        GameObject model = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (model == null)
            return false;

        model.name = definition.PrefabName + "_PremiumModel";
        model.transform.SetParent(parent, false);
        model.transform.localPosition = Vector3.zero;
        bool facesBackward =
            definition.ProductId == HomeStoreService.BalconySunAwningId ||
            definition.ProductId == HomeStoreService.BalconyRailingFlowersId ||
            definition.ProductId == HomeStoreService.BalconyLanternStringId ||
            definition.ProductId == HomeStoreService.PatioPergolaArchId ||
            definition.ProductId == HomeStoreService.PatioHerbTroughId ||
            definition.ProductId == HomeStoreService.PatioStringLightsId ||
            definition.ProductId == HomeStoreService.LoftWallGalleryId ||
            definition.ProductId == HomeStoreService.LoftTallBookcaseId ||
            definition.ProductId == HomeStoreService.BathroomTubId ||
            definition.ProductId == HomeStoreService.BathroomVanityId ||
            definition.ProductId == HomeStoreService.BathroomToiletId ||
            definition.ProductId == HomeStoreService.BathroomShowerId ||
            definition.ProductId == HomeStoreService.BedroomWardrobeId ||
            definition.ProductId == HomeStoreService.BedroomWindowDaybedId ||
            definition.ProductId == HomeStoreService.BedroomNightstandId ||
            definition.ProductId == HomeStoreService.BedroomDreamArtId ||
            definition.ProductId == HomeStoreService.GardenHammockId ||
            definition.ProductId == HomeStoreService.GardenGrillId ||
            definition.ProductId == HomeStoreService.BalconyHerbShelfId;
        model.transform.localRotation = facesBackward
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
        model.transform.localScale = Vector3.one;

        // Embedded FBX slots deliberately use the canonical CH_* names, so
        // existing URP materials remain the single source of truth.
        ReplaceMaterials(model, materials);
        if(definition.ProductId==HomeStoreService.BookshelfId)model.transform.localScale=Vector3.one*.8f;
        if(definition.ProductId==HomeStoreService.ModernTelevisionId)model.transform.localScale=Vector3.one*.88f;
        if(definition.ProductId==HomeStoreService.FloorLampId)model.transform.localScale=Vector3.one*.74f;
        if(definition.ProductId==HomeStoreService.TallPlantId)model.transform.localScale=Vector3.one*.72f;
        bool authoredForCatalogFit =
            definition.ProductId == HomeStoreService.ArmchairId ||
            fileName.StartsWith("Bathroom", StringComparison.Ordinal) ||
            fileName.StartsWith("Kitchen", StringComparison.Ordinal) ||
            fileName.StartsWith("Bedroom", StringComparison.Ordinal) ||
            fileName.StartsWith("Garden", StringComparison.Ordinal);
        if (authoredForCatalogFit)
            FitFixtureModel(model, definition);
        if(definition.ProductId==HomeStoreService.ModernTelevisionId)CatTelevisionScreenBuilder.Apply(parent.gameObject);
        return true;
    }

    private static void BuildLoftFloorRunnerVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "RunnerBase", PrimitiveType.Cube,
            new Vector3(0f, .04f, 0f), Quaternion.identity,
            new Vector3(1.9f, .06f, 1.12f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "RunnerBand", PrimitiveType.Cube,
            new Vector3(0f, .05f, 0f), Quaternion.identity,
            new Vector3(1.6f, .04f, .84f), materials["CH_Cream"]);
        for (int i = 0; i < 3; i++)
        {
            AddPrimitivePart(parent, "Chevron_" + i, PrimitiveType.Cube,
                new Vector3(-.5f + i * .5f, .07f, 0f), Quaternion.Euler(0f, 45f, 0f),
                new Vector3(.3f, .03f, .3f),
                i % 2 == 0 ? materials["CH_CoralBright"] : materials["CH_MintBright"]);
        }
    }

    private static void BuildLoftFloorCushionsVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "CushionA", PrimitiveType.Cube,
            new Vector3(-.22f, .14f, .04f), Quaternion.Euler(0f, 8f, 0f),
            new Vector3(.6f, .24f, .6f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "CushionB", PrimitiveType.Cube,
            new Vector3(.24f, .13f, -.06f), Quaternion.Euler(0f, -12f, 0f),
            new Vector3(.56f, .22f, .56f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "CushionTop", PrimitiveType.Cube,
            new Vector3(.02f, .34f, .0f), Quaternion.Euler(0f, 20f, 0f),
            new Vector3(.5f, .2f, .5f), materials["CH_LemonBright"]);
    }

    private static void BuildLoftBookStackVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Material[] covers =
        {
            materials["CH_CoralBright"], materials["CH_MintBright"],
            materials["CH_LilacBright"], materials["CH_LemonBright"],
            materials["CH_AquaBright"]
        };
        float y = .05f;
        for (int i = 0; i < 5; i++)
        {
            float w = .42f - i * .03f;
            AddPrimitivePart(parent, "Book_" + (i + 1), PrimitiveType.Cube,
                new Vector3((i % 2 == 0 ? -.03f : .03f), y, 0f),
                Quaternion.Euler(0f, (i % 2 == 0 ? -6f : 6f), 0f),
                new Vector3(w, .1f, .32f), covers[i % covers.Length]);
            y += .11f;
        }
        // A small side stack.
        AddPrimitivePart(parent, "SideBookA", PrimitiveType.Cube,
            new Vector3(.34f, .06f, .1f), Quaternion.Euler(0f, 24f, 0f),
            new Vector3(.36f, .1f, .28f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "SideBookB", PrimitiveType.Cube,
            new Vector3(.34f, .17f, .1f), Quaternion.Euler(0f, 24f, 0f),
            new Vector3(.34f, .1f, .26f), materials["CH_Cream"]);
    }

    private static void BuildLoftArcLampVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Base", PrimitiveType.Cylinder,
            new Vector3(.28f, .05f, 0f), Quaternion.identity,
            new Vector3(.4f, .1f, .4f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "PoleLow", PrimitiveType.Cylinder,
            new Vector3(.28f, .7f, 0f), Quaternion.identity,
            new Vector3(.06f, 1.4f, .06f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "ArcMid", PrimitiveType.Cylinder,
            new Vector3(.12f, 1.5f, 0f), Quaternion.Euler(0f, 0f, 55f),
            new Vector3(.06f, .5f, .06f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "ArcTop", PrimitiveType.Cylinder,
            new Vector3(-.16f, 1.72f, 0f), Quaternion.Euler(0f, 0f, 80f),
            new Vector3(.06f, .5f, .06f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "Shade", PrimitiveType.Cylinder,
            new Vector3(-.3f, 1.62f, 0f), Quaternion.identity,
            new Vector3(.34f, .2f, .34f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "Bulb", PrimitiveType.Sphere,
            new Vector3(-.3f, 1.5f, 0f), Quaternion.identity,
            Vector3.one * .14f, materials["CH_LemonBright"]);
    }

    private static void BuildLoftBeanBagVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Base", PrimitiveType.Sphere,
            new Vector3(0f, .22f, 0f), Quaternion.identity,
            new Vector3(.92f, .5f, .92f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "Seat", PrimitiveType.Sphere,
            new Vector3(0f, .42f, -.06f), Quaternion.identity,
            new Vector3(.66f, .3f, .66f), materials["CH_Pink"]);
        AddPrimitivePart(parent, "Seam", PrimitiveType.Cube,
            new Vector3(0f, .3f, .0f), Quaternion.identity,
            new Vector3(.94f, .04f, .1f), materials["CH_Cream"]);
    }

    private static void BuildLoftRecordPlayerVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "StandTop", PrimitiveType.Cube,
            new Vector3(0f, .5f, 0f), Quaternion.identity,
            new Vector3(.7f, .06f, .5f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "LegL", PrimitiveType.Cylinder,
            new Vector3(-.28f, .25f, .18f), Quaternion.identity,
            new Vector3(.06f, .5f, .06f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "LegR", PrimitiveType.Cylinder,
            new Vector3(.28f, .25f, .18f), Quaternion.identity,
            new Vector3(.06f, .5f, .06f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "LegLB", PrimitiveType.Cylinder,
            new Vector3(-.28f, .25f, -.18f), Quaternion.identity,
            new Vector3(.06f, .5f, .06f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "LegRB", PrimitiveType.Cylinder,
            new Vector3(.28f, .25f, -.18f), Quaternion.identity,
            new Vector3(.06f, .5f, .06f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "Box", PrimitiveType.Cube,
            new Vector3(0f, .58f, 0f), Quaternion.identity,
            new Vector3(.64f, .1f, .46f), materials["CH_Ink"]);
        AddPrimitivePart(parent, "Platter", PrimitiveType.Cylinder,
            new Vector3(-.06f, .64f, 0f), Quaternion.identity,
            new Vector3(.34f, .03f, .34f), materials["CH_Screen"]);
        AddPrimitivePart(parent, "Label", PrimitiveType.Cylinder,
            new Vector3(-.06f, .66f, 0f), Quaternion.identity,
            new Vector3(.12f, .02f, .12f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "Tonearm", PrimitiveType.Cube,
            new Vector3(.2f, .66f, .12f), Quaternion.Euler(0f, 35f, 0f),
            new Vector3(.28f, .03f, .04f), materials["CH_Gold"]);
    }

    private static void BuildLoftStudyDeskVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "DeskTop", PrimitiveType.Cube,
            new Vector3(0f, .72f, .1f), Quaternion.identity,
            new Vector3(1.3f, .08f, .56f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "LegL", PrimitiveType.Cube,
            new Vector3(-.58f, .36f, .1f), Quaternion.identity,
            new Vector3(.08f, .72f, .5f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "LegR", PrimitiveType.Cube,
            new Vector3(.58f, .36f, .1f), Quaternion.identity,
            new Vector3(.08f, .72f, .5f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "Drawer", PrimitiveType.Cube,
            new Vector3(.42f, .6f, .1f), Quaternion.identity,
            new Vector3(.4f, .18f, .5f), materials["CH_Cream"]);
        AddPrimitivePart(parent, "ChairSeat", PrimitiveType.Cube,
            new Vector3(0f, .42f, -.4f), Quaternion.identity,
            new Vector3(.42f, .08f, .42f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "ChairBack", PrimitiveType.Cube,
            new Vector3(0f, .62f, -.6f), Quaternion.identity,
            new Vector3(.42f, .4f, .06f), materials["CH_MintBright"]);
        AddPrimitivePart(parent, "DeskLampArm", PrimitiveType.Cylinder,
            new Vector3(-.5f, .92f, .2f), Quaternion.Euler(0f, 0f, 30f),
            new Vector3(.04f, .3f, .04f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "DeskLampHead", PrimitiveType.Sphere,
            new Vector3(-.42f, 1.04f, .2f), Quaternion.identity,
            Vector3.one * .12f, materials["CH_LemonBright"]);
        AddPrimitivePart(parent, "Books", PrimitiveType.Cube,
            new Vector3(.36f, .82f, .1f), Quaternion.identity,
            new Vector3(.3f, .12f, .24f), materials["CH_LilacBright"]);
    }

    private static void BuildLoftWallGalleryVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        Material[] arts =
        {
            materials["CH_CoralBright"], materials["CH_MintBright"],
            materials["CH_LilacBright"], materials["CH_LemonBright"],
            materials["CH_AquaBright"], materials["CH_Pink"]
        };
        int idx = 0;
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = -.5f + col * .5f;
                float y = .3f + row * .5f;
                AddPrimitivePart(parent, "Frame_" + idx, PrimitiveType.Cube,
                    new Vector3(x, y, .02f), Quaternion.identity,
                    new Vector3(.4f, .4f, .04f), materials["CH_Cream"]);
                AddPrimitivePart(parent, "Art_" + idx, PrimitiveType.Cube,
                    new Vector3(x, y, -.01f), Quaternion.identity,
                    new Vector3(.3f, .3f, .04f), arts[idx % arts.Length]);
                idx++;
            }
        }
    }

    private static void BuildLoftTallBookcaseVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Back", PrimitiveType.Cube,
            new Vector3(0f, 1.0f, -.16f), Quaternion.identity,
            new Vector3(1.3f, 2.0f, .06f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "SideL", PrimitiveType.Cube,
            new Vector3(-.64f, 1.0f, 0f), Quaternion.identity,
            new Vector3(.08f, 2.0f, .36f), materials["CH_OrangeLight"]);
        AddPrimitivePart(parent, "SideR", PrimitiveType.Cube,
            new Vector3(.64f, 1.0f, 0f), Quaternion.identity,
            new Vector3(.08f, 2.0f, .36f), materials["CH_OrangeLight"]);
        Material[] books =
        {
            materials["CH_CoralBright"], materials["CH_MintBright"],
            materials["CH_LilacBright"], materials["CH_LemonBright"],
            materials["CH_AquaBright"]
        };
        for (int shelf = 0; shelf < 5; shelf++)
        {
            float y = .28f + shelf * .42f;
            AddPrimitivePart(parent, "Shelf_" + shelf, PrimitiveType.Cube,
                new Vector3(0f, y, 0f), Quaternion.identity,
                new Vector3(1.24f, .05f, .34f), materials["CH_OrangeLight"]);
            for (int b = 0; b < 5; b++)
            {
                AddPrimitivePart(parent, "Book_" + shelf + "_" + b, PrimitiveType.Cube,
                    new Vector3(-.44f + b * .22f, y + .19f, 0f),
                    Quaternion.Euler(0f, 0f, (b == 4 ? 12f : 0f)),
                    new Vector3(.14f, .3f, .24f), books[(shelf + b) % books.Length]);
            }
        }
        AddPrimitivePart(parent, "TopPlant", PrimitiveType.Sphere,
            new Vector3(.4f, 2.12f, 0f), Quaternion.identity,
            new Vector3(.34f, .3f, .34f), materials["CH_MintBright"]);
    }

    private static void BuildLoftChaiseLoungeVisual(
        Transform parent, IReadOnlyDictionary<string, Material> materials)
    {
        AddPrimitivePart(parent, "Base", PrimitiveType.Cube,
            new Vector3(0f, .22f, 0f), Quaternion.identity,
            new Vector3(1.5f, .16f, .58f), materials["CH_Orange"]);
        AddPrimitivePart(parent, "Seat", PrimitiveType.Cube,
            new Vector3(0f, .36f, 0f), Quaternion.identity,
            new Vector3(1.42f, .16f, .52f), materials["CH_LilacBright"]);
        AddPrimitivePart(parent, "Backrest", PrimitiveType.Cube,
            new Vector3(-.62f, .58f, 0f), Quaternion.Euler(0f, 0f, 24f),
            new Vector3(.5f, .16f, .52f), materials["CH_Purple"]);
        AddPrimitivePart(parent, "Bolster", PrimitiveType.Cylinder,
            new Vector3(-.5f, .5f, 0f), Quaternion.Euler(90f, 0f, 0f),
            new Vector3(.2f, .5f, .2f), materials["CH_CoralBright"]);
        AddPrimitivePart(parent, "LegL", PrimitiveType.Cylinder,
            new Vector3(-.6f, .08f, .2f), Quaternion.identity,
            new Vector3(.08f, .16f, .08f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "LegR", PrimitiveType.Cylinder,
            new Vector3(.6f, .08f, .2f), Quaternion.identity,
            new Vector3(.08f, .16f, .08f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "LegLB", PrimitiveType.Cylinder,
            new Vector3(-.6f, .08f, -.2f), Quaternion.identity,
            new Vector3(.08f, .16f, .08f), materials["CH_Gold"]);
        AddPrimitivePart(parent, "LegRB", PrimitiveType.Cylinder,
            new Vector3(.6f, .08f, -.2f), Quaternion.identity,
            new Vector3(.08f, .16f, .08f), materials["CH_Gold"]);
    }

    private static GameObject AddPrimitivePart(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 size,
        Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = type == PrimitiveType.Cylinder
            ? new Vector3(size.x, size.y * .5f, size.z)
            : size;
        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
        return part;
    }

    private static void AddProductCollider(GameObject visual, StoreCatalogAsset definition)
    {
        ConfigureProductCollider(visual.AddComponent<BoxCollider>(), definition);
    }

    /// <summary>
    /// Sizes a product's box to its catalog contract and decides whether it is
    /// solid or a trigger.
    ///
    /// The cat's <see cref="CharacterController"/> is authored with a step
    /// offset of 0.01, which is deliberate: it stops the cat from strolling up
    /// onto furniture that has no climb routine. The cost is that ANY solid
    /// collider taller than a centimetre is a wall. The six floor mats are
    /// 0.080 tall, so every rug in the game was an eight-centimetre kerb the cat
    /// bounced off, and the room read as if the rug were a pit. A rug is a
    /// surface, not an obstacle: it becomes a trigger and the cat walks over it.
    ///
    /// This is the same call the litter tray, shower, tunnel, star tipi, swing
    /// and chaise already make by hand for their own routines
    /// (<see cref="AttachLitterDigActivity"/> spells the reasoning out); the
    /// mats simply never got it, because their `MatKneadActivity` drives the
    /// cat from an anchor beside the mat and nothing failed loudly.
    ///
    /// Keep the threshold BELOW `WalkingLeash` at 0.150, which is a floor
    /// product the cat should still bump into.
    /// </summary>
    private static void ConfigureProductCollider(
        BoxCollider collider, StoreCatalogAsset definition)
    {
        collider.center = new Vector3(0f, definition.Height * .5f, 0f);
        collider.size = new Vector3(
            definition.Footprint.x,
            definition.Height,
            definition.Footprint.y);
        collider.isTrigger = IsWalkOverSurface(definition);
    }

    private const float WalkOverSurfaceHeight = .12f;

    private static bool IsWalkOverSurface(StoreCatalogAsset definition)
    {
        return definition.PlacementKind == HomeProductPlacementKind.Floor &&
               definition.HungHeight <= .01f &&
               definition.Height <= WalkOverSurfaceHeight;
    }

    private static readonly string[] ColorfulBookSpines =
    {
        "CH_CoralBright", "CH_AquaBright", "CH_MintBright", "CH_LilacBright",
        "CH_LemonBright", "CH_Pink", "CH_TealLight", "CH_Teal",
        "CH_Orange", "CH_Purple"
    };

    private static void BuildBookshelfBookSetPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        const string sourceFolder = "Assets/LowPolyLivingRoomPack/Prefabs/";
        // The premium book is authored at the size the pack book reached after
        // its 2.35 scale, so it is instanced at 1.
        GameObject premiumBook = AssetDatabase.LoadAssetAtPath<GameObject>(
            PremiumFurnitureModelFolder + "ColorfulBook_Premium.fbx");
        float bookScale = premiumBook != null ? 1f : 2.35f;
        const float usableShelfWidth = 1.2f;
        const float gap = 0.025f;

        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            var books = new List<Transform>(10);
            var widths = new List<float>(10);
            for (int i = 1; i <= 10; i++)
            {
                string sourcePath = sourceFolder + "Book_" + i + ".prefab";
                GameObject source = premiumBook != null
                    ? premiumBook
                    : AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null)
                    throw new InvalidOperationException("Book source is missing: " + sourcePath);

                GameObject book = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (book == null)
                    throw new InvalidOperationException("Could not instantiate " + sourcePath);
                book.name = "ColorBook_" + i;
                book.transform.SetParent(visual.transform, false);
                book.transform.localPosition = Vector3.zero;
                book.transform.localRotation = Quaternion.identity;
                book.transform.localScale = Vector3.one * bookScale;

                if (premiumBook != null)
                {
                    ReplaceMaterials(book, materials);
                    // One authored book, ten spine colours.
                    string spineName = ColorfulBookSpines[(i - 1) % ColorfulBookSpines.Length];
                    if (materials.TryGetValue(spineName, out Material spine))
                    {
                        Renderer[] bookRenderers = book.GetComponentsInChildren<Renderer>(true);
                        for (int r = 0; r < bookRenderers.Length; r++)
                        {
                            Material[] slots = bookRenderers[r].sharedMaterials;
                            for (int slot = 0; slot < slots.Length; slot++)
                            {
                                if (slots[slot] != null &&
                                    slots[slot].name == "CH_CoralBright")
                                    slots[slot] = spine;
                            }
                            bookRenderers[r].sharedMaterials = slots;
                        }
                    }
                }

                Renderer[] renderers = book.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                    throw new InvalidOperationException("Book has no renderer: " + sourcePath);
                Bounds bounds = renderers[0].bounds;
                for (int r = 1; r < renderers.Length; r++)
                    bounds.Encapsulate(renderers[r].bounds);
                books.Add(book.transform);
                widths.Add(Mathf.Max(0.03f, bounds.size.x));
            }

            var positions = new Vector3[books.Count];
            var rotations = new Vector3[books.Count];
            var scales = new Vector3[books.Count];
            int rowStart = 0;
            const int shelfRowCount = 3;
            int baseRowCount = books.Count / shelfRowCount;
            int extraBooks = books.Count % shelfRowCount;
            for (int row = 0; row < shelfRowCount; row++)
            {
                int rowCount = baseRowCount + (row < extraBooks ? 1 : 0);
                float rowWidth = 0f;
                for (int i = 0; i < rowCount; i++)
                    rowWidth += widths[rowStart + i] + (i > 0 ? gap : 0f);
                if (rowWidth > usableShelfWidth)
                {
                    throw new InvalidOperationException(
                        "Colorful book row exceeds the tall bookshelf width.");
                }

                float cursor = -rowWidth * 0.5f;
                for (int i = 0; i < rowCount; i++)
                {
                    int index = rowStart + i;
                    float width = widths[index];
                    positions[index] = new Vector3(
                        cursor + width * 0.5f,
                        0.65f + row * 0.4f,
                        0.2f);
                    rotations[index] = Vector3.zero;
                    scales[index] = books[index].localScale;
                    cursor += width + gap;
                }

                rowStart += rowCount;
            }

            // Keep the authored prefab in its final arranged pose as well. Runtime
            // placement still animates from the staged offsets, while catalog
            // previews can show the complete ten-book set instead of an overlap.
            for (int i = 0; i < books.Count; i++)
            {
                positions[i]*=.8f;
                scales[i]*=.8f;
                books[i].localPosition = positions[i];
                books[i].localRotation = Quaternion.Euler(rotations[i]);
                books[i].localScale = scales[i];
            }

            StoreProductDisplay display = root.AddComponent<StoreProductDisplay>();
            display.EditorConfigure(definition.ProductId, visual);
            HomeBookshelfBookSet bookSet = root.AddComponent<HomeBookshelfBookSet>();
            bookSet.EditorConfigure(
                definition.ProductId,
                HomeStoreService.BookshelfId,
                visual,
                books.ToArray(),
                positions,
                rotations,
                scales);
            HomeProductPlacement placement = root.AddComponent<HomeProductPlacement>();
            placement.EditorConfigure(
                definition.ProductId,
                root.transform,
                Array.Empty<Transform>(),
                definition.Footprint,
                HomeProductPlacementKind.BookshelfOnly,
                HomeStoreService.BookshelfId);

            string prefabPath = PrefabFolder + "/" + definition.PrefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void BuildGameConsoleSetPrefab(StoreCatalogAsset definition)
    {
        BuildCompositeProductPrefab(
            definition,
            new[]
            {
                new CompositePart(
                    "Assets/LowPolyLivingRoomPack/Prefabs/Console_Modern.prefab",
                    "ModernGameConsole",
                    new Vector3(-0.13f, 0f, 0f),
                    3.2f),
                new CompositePart(
                    "Assets/LowPolyLivingRoomPack/Prefabs/Gamepad_Classic.prefab",
                    "ClassicGamepad",
                    new Vector3(0.18f, 0f, -0.06f),
                    1.4f)
            });
    }

    private static void BuildSpeakerSystemPrefab(StoreCatalogAsset definition)
    {
        BuildCompositeProductPrefab(
            definition,
            new[]
            {
                new CompositePart(
                    "Assets/LowPolyLivingRoomPack/Prefabs/Stereo_MainUnit.prefab",
                    "StereoMainUnit",
                    Vector3.zero,
                    3f),
                new CompositePart(
                    "Assets/LowPolyLivingRoomPack/Prefabs/Stereo_Speaker.prefab",
                    "LeftSpeaker",
                    new Vector3(-0.43f, 0f, 0f),
                    3f),
                new CompositePart(
                    "Assets/LowPolyLivingRoomPack/Prefabs/Stereo_Speaker.prefab",
                    "RightSpeaker",
                    new Vector3(0.43f, 0f, 0f),
                    3f)
            });
    }

    private static void BuildCompositeProductPrefab(
        StoreCatalogAsset definition,
        CompositePart[] parts)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);
            for (int i = 0; i < parts.Length; i++)
                AddCompositePart(visual.transform, parts[i]);

            ConfigureProductCollider(visual.AddComponent<BoxCollider>(), definition);

            ConfigureProductComponents(root, visual, definition);
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void AddCompositePart(Transform parent, CompositePart part)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(part.AssetPath);
        if (asset == null)
            throw new InvalidOperationException("Composite product part is missing: " + part.AssetPath);

        GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null)
            throw new InvalidOperationException("Could not instantiate " + part.AssetPath);
        instance.name = part.Name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = part.LocalPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * part.Scale;
    }

    private static void ConfigureProductComponents(
        GameObject root,
        GameObject visual,
        StoreCatalogAsset definition)
    {
        root.AddComponent<StoreProductDisplay>().EditorConfigure(
            definition.ProductId,
            visual);

        string requiredId = HomeStoreService.GetRequiredProductId(definition.ProductId);
        if (definition.PlacementKind == HomeProductPlacementKind.ProductSurfaceOnly)
        {
            if (string.IsNullOrEmpty(requiredId))
            {
                throw new InvalidOperationException(
                    "A product-surface placement needs a required product id: " +
                    definition.ProductId);
            }

            Vector3 localPosition = definition.ProductId == HomeStoreService.ModernTelevisionId
                ? new Vector3(0f, 0.60f, 0f)
                : Vector3.zero;
            root.AddComponent<HomeRequiredProductAttachment>().EditorConfigure(
                definition.ProductId,
                requiredId,
                visual,
                localPosition,
                Vector3.zero);
        }

        root.AddComponent<HomeProductPlacement>().EditorConfigure(
            definition.ProductId,
            root.transform,
            Array.Empty<Transform>(),
            definition.Footprint,
            definition.PlacementKind,
            requiredId,
            definition.HungHeight);
    }

    private readonly struct CompositePart
    {
        public CompositePart(string assetPath, string name, Vector3 localPosition, float scale)
        {
            AssetPath = assetPath;
            Name = name;
            LocalPosition = localPosition;
            Scale = scale;
        }

        public string AssetPath { get; }
        public string Name { get; }
        public Vector3 LocalPosition { get; }
        public float Scale { get; }
    }

    private static void ReplaceMaterials(
        GameObject model,
        IReadOnlyDictionary<string, Material> materials)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] current = renderers[i].sharedMaterials;
            for (int slot = 0; slot < current.Length; slot++)
            {
                string name = current[slot] != null ? current[slot].name : string.Empty;
                if (materials.TryGetValue(name, out Material replacement))
                    current[slot] = replacement;
            }
            renderers[i].sharedMaterials = current;
        }
    }

    private static void BuildRoomSceneProducts(string scenePath, string roomId)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedForBuild = !scene.IsValid() || !scene.isLoaded;
        if (openedForBuild)
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

        BuildRoomSceneProducts(scene, roomId, null);
        if(roomId==HomeRoomService.LivingRoomId)CatProductContentBuilder.UpgradeLegacyStations(scene);
        RoomActivityLayoutBuilder.Configure(scene, roomId);
        ModernWorldArtBuilder.Apply(scene, roomId);
        if(roomId==HomeRoomService.BathroomId)BathroomThemeBuilder.ApplyScene(scene);
        if(roomId==HomeRoomService.KitchenId)KitchenThemeBuilder.ApplyScene(scene);
        if(RemainingRoomsThemeBuilder.Handles(roomId))RemainingRoomsThemeBuilder.ApplyScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedForBuild)
            EditorSceneManager.CloseScene(scene, true);
    }

    public static void BuildRoomSceneProducts(
        Scene scene,
        string roomId,
        Transform preferredParent)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("Room scene must be loaded before products are authored.");

        HomeRoomArrangementBuilder.EnsureRoomPlan(roomId);

        GameObject existing = FindNamedInScene(scene, SceneRootName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        var root = new GameObject(SceneRootName);
        SceneManager.MoveGameObjectToScene(root, scene);
        Transform furniture = preferredParent;
        if (furniture == null)
        {
            GameObject livingFurniture = FindNamedInScene(scene, "RoomFurniture");
            GameObject authoringFurniture = FindNamedInScene(scene, "02 Furniture");
            furniture = livingFurniture != null
                ? livingFurniture.transform
                : authoringFurniture != null ? authoringFurniture.transform : null;
        }
        if (furniture != null)
        {
            root.transform.SetParent(furniture, false);
            Vector3 inherited = furniture.lossyScale;
            root.transform.localScale = new Vector3(
                SafeInverse(inherited.x),
                SafeInverse(inherited.y),
                SafeInverse(inherited.z));
        }

        GameObject catalogGroup = CreateHierarchyGroup("Catalog", root.transform);
        GameObject catGroup = CreateHierarchyGroup("CAT Products", catalogGroup.transform);
        GameObject roomGroup = CreateHierarchyGroup("ROOM Products", catalogGroup.transform);

        for (int i = 0; i < StoreCatalogAssets.PlaceableProducts.Length; i++)
        {
            StoreCatalogAsset definition = StoreCatalogAssets.PlaceableProducts[i];
            if (!HomeStoreService.TryGetProduct(
                    definition.ProductId,
                    out HomeStoreProduct product))
            {
                continue;
            }

            bool roomProduct = product.StoreCategory == HomeStoreCategory.Room &&
                               HomeStoreService.IsProductInRoomCollection(
                                   roomId,
                                   definition.ProductId);
            bool catProduct = product.StoreCategory == HomeStoreCategory.Cat &&
                              string.Equals(
                                  roomId,
                                  HomeRoomService.LivingRoomId,
                                  StringComparison.Ordinal);
            if (!roomProduct && !catProduct)
                continue;

            string prefabPath = PrefabFolder + "/" + definition.PrefabName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new InvalidOperationException("Store prefab is missing: " + prefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Could not place store prefab " + prefabPath);
            instance.name = definition.PrefabName;
            Transform categoryParent = roomProduct
                ? roomGroup.transform
                : catGroup.transform;
            instance.transform.SetParent(categoryParent, false);
            Vector3 placed = definition.DefaultPosition;
            if (definition.HungHeight > 0.01f)
                placed.y = definition.HungHeight;
            instance.transform.SetPositionAndRotation(
                placed,
                Quaternion.Euler(0f, definition.DefaultYaw, 0f));
        }
        OutdoorPolishBuilder.ConfigureScene(scene, roomId);
        HomeRoomArrangementBuilder.ConfigureApproaches(scene, roomId);
        ModernWorldArtBuilder.ApplyRoot(root.transform, roomId);
        if(roomId==HomeRoomService.BathroomId)BathroomThemeBuilder.ApplyScene(scene);
        if(roomId==HomeRoomService.KitchenId)KitchenDiningSetBuilder.Apply(scene);
        if(roomId==HomeRoomService.KitchenId)KitchenThemeBuilder.ApplyScene(scene);
        if(RemainingRoomsThemeBuilder.Handles(roomId))RemainingRoomsThemeBuilder.ApplyScene(scene);
    }

    private static GameObject CreateHierarchyGroup(string name, Transform parent)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group;
    }

    private static float SafeInverse(float value)
    {
        return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
    }

    private static GameObject FindNamedInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i].name == name)
                    return transforms[i].gameObject;
        }
        return null;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Art", "StoreProducts");
        EnsureFolder("Assets/Art/StoreProducts", "Models");
        EnsureFolder("Assets/Art/StoreProducts", "Materials");
        EnsureFolder("Assets/Art/StoreProducts", "Prefabs");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

}
