using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Kitchen finishes share the living/bathroom palette without changing furniture or action geometry.</summary>
public static class KitchenThemeBuilder
{
    public const string DecorRoot = "KitchenHomeAccents";
    static Material Surface(string role) => AssetDatabase.LoadAssetAtPath<Material>(BathroomThemeBuilder.Folder + "/" + role + ".mat")
        ?? throw new InvalidOperationException("Missing shared home finish: " + role);
    static Material Cream => Surface("WarmPorcelain");
    static Material Sage => Surface("SagePlaster");
    static Material Petrol => Surface("PetrolPanels");
    static Material Mint => Surface("MintEnamel");
    static Material Seafoam => Surface("SeafoamEnamel");
    static Material Coral => Surface("CoralLinen");
    static Material Brass => Surface("BrushedBrass");
    static Material Sand => Surface("SandStone");
    static Material Oat => Surface("OatStone");
    static Material Water => Surface("SeaGlass");
    static Material Oak => LivingCompositionBuilder.Material("LC_Oak");

    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        CatHomeEditPreview.Clear(); ApplyProductPrefabs();
        var scene = SceneManager.GetSceneByPath(HomeRoomService.KitchenScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(HomeRoomService.KitchenScenePath, OpenSceneMode.Additive);
        try
        {
            ApplyScene(scene);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        return "Kitchen finishes, camera and home accents saved; action geometry preserved.";
    }

    public static void ApplyProductPrefabs()
    {
        foreach (var product in StoreCatalogAssets.PlaceableProducts.Where(p =>
                     HomeStoreService.IsProductInRoomCollection(HomeRoomService.KitchenId, p.ProductId)))
            ApplyProductPrefab("Assets/Art/StoreProducts/Prefabs/" + product.PrefabName + ".prefab");
    }

    public static void ApplyProductPrefab(string path)
    {
        if (!Path.GetFileName(path).StartsWith("Kitchen", StringComparison.Ordinal)) return;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var display = root.GetComponent<StoreProductDisplay>();
            if (display == null) return;
            ApplyFurniture(root.transform, display.ProductId); PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void ApplyFurniture(Transform root, string id)
    {
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            // Fruit remains recognizable and keeps the existing loose-prop materials.
            if (renderer.name.StartsWith("Fruit_", StringComparison.Ordinal)) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i]; if (material == null) continue;
                var source = ModernWorldArtBuilder.ResolveSourceMaterial(material);
                if (AssetDatabase.GetAssetPath(source).StartsWith(BathroomThemeBuilder.Folder + "/", StringComparison.Ordinal) || source.name == "LC_Oak")
                { materials[i] = source; continue; }
                string name = source.name.ToLowerInvariant();
                if (name.Contains("gold")) materials[i] = Brass;
                else if (name.Contains("cream"))
                    materials[i] = id == HomeStoreService.KitchenRefrigeratorId || id == HomeStoreService.KitchenStoveOvenId ? Mint :
                        id == HomeStoreService.KitchenPantryShelfId || id == HomeStoreService.KitchenSinkCabinetId || id == HomeStoreService.KitchenIslandId ? Petrol :
                        id == KitchenDiningSetBuilder.ActivityId || id == HomeStoreService.KitchenCounterStoolId || id == HomeStoreService.KitchenFruitBasketId ? Oak : Cream;
                else if (name.Contains("white") || name.Contains("pearl")) materials[i] = Cream;
                else if (name.Contains("coral") || name.Contains("pink")) materials[i] = Coral;
                else if (name.Contains("aqua") || name.Contains("teal")) materials[i] = Water;
                else if (name.Contains("mint")) materials[i] = Mint;
                else if (name.Contains("lilac")) materials[i] = Seafoam;
                else if (name.Contains("lemon")) materials[i] = Oat;
            }
            renderer.sharedMaterials = materials;
        }
    }

    public static void ApplyScene(Scene scene)
    {
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        if (scene.path != HomeRoomService.KitchenScenePath && !all.Any(t => t.GetComponent<HomeRoomSceneMarker>()?.RoomId == HomeRoomService.KitchenId)) return;
        foreach (var camera in all.Select(t => t.GetComponent<Camera>()).Where(c => c != null)) HomeRoomCameraProfile.Apply(camera);
        foreach (var product in all.Select(t => t.GetComponent<StoreProductDisplay>()).Where(p => p != null)) ApplyFurniture(product.transform, product.ProductId);
        var dining = all.FirstOrDefault(t => t.name == "KitchenDiningSet");
        if (dining != null) ApplyFurniture(dining, KitchenDiningSetBuilder.ActivityId);
        foreach (var renderer in all.Select(t => t.GetComponent<MeshRenderer>()).Where(r => r != null))
        {
            if (renderer.GetComponentInParent<StoreProductDisplay>() != null || renderer.GetComponentInParent<CatMovement>() != null ||
                dining != null && renderer.transform.IsChildOf(dining)) continue;
            string name = renderer.name;
            if (renderer.GetComponentsInParent<Transform>(true).Any(t => t.name == "KitchenPearlThreshold" || t.name == "Candy Corner Jewels"))
            { renderer.enabled = false; continue; }
            if (name.StartsWith("RibbonTile_") || name == "SkyGlass" || name == "Sun" || name.StartsWith("Cloud") || name.EndsWith("Hill"))
            { renderer.enabled = false; continue; }
            if (name.StartsWith("Tile_"))
            { var parts = name.Split('_'); renderer.sharedMaterial = (int.Parse(parts[1]) + int.Parse(parts[2])) % 2 == 0 ? Sand : Oat; }
            else if (name == "WalkableFloor") renderer.sharedMaterial = Sand;
            else if (name.Contains("Wall_Cream")) renderer.sharedMaterial = name.StartsWith("Right") ? Petrol : Sage;
            else if (name.Contains("Wainscot") || name.Contains("Backsplash")) renderer.sharedMaterial = Petrol;
            else if (name == "RoomWallPanel_Premium") renderer.sharedMaterials = new[] { Petrol, Cream, Brass };
            else if (name == "RoomDoor_Premium") renderer.sharedMaterials = new[] { Cream, Mint, Brass };
            else if (name.Contains("Gold") || name.StartsWith("Paw")) renderer.sharedMaterial = Brass;
            else if (name.Contains("Baseboard") || name.Contains("Rail") || name.Contains("Crown") || name.Contains("Pearl") || name.StartsWith("Window")) renderer.sharedMaterial = Cream;
            else if (name.Contains("Candy")) renderer.sharedMaterial = Mint;
        }
        var window = all.FirstOrDefault(t => t.name == "Kitchen Sunrise Window");
        if (window != null)
        {
            window.localPosition = new Vector3(-.1875f, .40f, 0);
            window.localScale = new Vector3(.75f, .65f, 1);
            var oldView = window.Find("KitchenGardenView");
            if (oldView != null) Object.DestroyImmediate(oldView.gameObject);
            var view = GameObject.CreatePrimitive(PrimitiveType.Quad); view.name = "KitchenGardenView";
            Object.DestroyImmediate(view.GetComponent<Collider>()); view.transform.SetParent(window, false);
            view.transform.localPosition = new Vector3(-.75f, 2.25f, HomeRoomShellMetrics.BackWallDecorZ(.40f));
            view.transform.localScale = new Vector3(1.82f, 1.16f, 1);
            view.GetComponent<MeshRenderer>().sharedMaterial = LivingCompositionBuilder.Material("LC_WindowLandscape");
            view.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var badge = all.FirstOrDefault(t => t != null && t.name == "Kitchen Paw Medallion");
        if (badge != null) { badge.localPosition = new Vector3(-.91f, .485f, 0); badge.localScale = new Vector3(.65f, .65f, 1); }
        var old = all.FirstOrDefault(t => t != null && t.name == DecorRoot);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject(DecorRoot).transform; SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        var plant = LivingCompositionBuilder.Model("SmallCeramicPlant", root); plant.name = "KitchenWindowHerb";
        plant.transform.position = new Vector3(-.14f, 1.385f, 2.32f); plant.transform.localScale = Vector3.one * .48f;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
