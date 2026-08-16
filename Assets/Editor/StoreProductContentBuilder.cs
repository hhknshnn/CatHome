using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Imports Blender store models and authors movable room products.</summary>
public static class StoreProductContentBuilder
{
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
            BuildPrefab(StoreCatalogAssets.PlaceableProducts[i], materials);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
        if (definition.ProductId == HomeStoreService.BookSetId)
        {
            BuildBookshelfBookSetPrefab(definition);
            return;
        }
        if (definition.ProductId == HomeStoreService.GameConsoleId)
        {
            BuildGameConsoleSetPrefab(definition);
            return;
        }
        if (definition.ProductId == HomeStoreService.StereoId)
        {
            BuildSpeakerSystemPrefab(definition);
            return;
        }

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

            BoxCollider collider = visual.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, definition.Height * 0.5f, 0f);
            collider.size = new Vector3(
                definition.Footprint.x,
                definition.Height,
                definition.Footprint.y);

            ConfigureProductComponents(root, visual, definition);

            string prefabPath = PrefabFolder + "/" + definition.PrefabName + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
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
            new Vector3(.98f, 1.82f, .22f), materials["CH_Ink"]);
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

    private static void BuildProceduralBedroomPrefab(
        StoreCatalogAsset definition,
        IReadOnlyDictionary<string, Material> materials)
    {
        var root = new GameObject("StoreProduct_" + definition.PrefabName);
        try
        {
            var visual = new GameObject("VisualContent");
            visual.transform.SetParent(root.transform, false);

            if (definition.ProductId == HomeStoreService.BedroomPawRugId)
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
            PrefabUtility.SaveAsPrefabAsset(
                root,
                PrefabFolder + "/" + definition.PrefabName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
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

            if (definition.ProductId == HomeStoreService.KitchenPawMatId)
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
        BoxCollider collider = visual.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, definition.Height * .5f, 0f);
        collider.size = new Vector3(
            definition.Footprint.x,
            definition.Height,
            definition.Footprint.y);
    }

    private static void BuildBookshelfBookSetPrefab(StoreCatalogAsset definition)
    {
        const string sourceFolder = "Assets/LowPolyLivingRoomPack/Prefabs/";
        const float bookScale = 2.35f;
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
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
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

            BoxCollider collider = visual.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, definition.Height * 0.5f, 0f);
            collider.size = new Vector3(
                definition.Footprint.x,
                definition.Height,
                definition.Footprint.y);

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
                ? new Vector3(0f, 0.62f, 0f)
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
        Dictionary<string, Material> materials)
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
