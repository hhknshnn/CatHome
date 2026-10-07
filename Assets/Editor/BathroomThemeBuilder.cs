using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Bathroom art in the living-room palette. Product geometry and activity anchors stay authored.</summary>
public static class BathroomThemeBuilder
{
    public const string Folder = "Assets/Art/BathroomTheme";
    public const string DecorRoot = "BathroomHomeAccents";
    static Material Surface(string role, Color32 color, float gloss = .25f, float metal = 0)
    {
        string path = Folder + "/" + role + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Directory.CreateDirectory(Folder);
            material = new Material(LivingCompositionBuilder.Material("LC_Cream")) { name = "Bathroom_" + role };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        material.SetFloat("_Smoothness", gloss); material.SetFloat("_Metallic", metal);
        material.enableInstancing = true; EditorUtility.SetDirty(material);
        return material;
    }
    static Material Cream => Surface("WarmPorcelain", new Color32(237,229,207,255), .48f);
    static Material Sage => Surface("SagePlaster", new Color32(150,182,174,255));
    static Material Petrol => Surface("PetrolPanels", new Color32(62,112,119,255));
    static Material Mint => Surface("MintEnamel", new Color32(119,191,166,255), .42f);
    static Material Seafoam => Surface("SeafoamEnamel", new Color32(157,208,190,255), .40f);
    static Material Coral => Surface("CoralLinen", new Color32(219,119,102,255), .14f);
    static Material Brass => Surface("BrushedBrass", new Color32(193,159,102,255), .43f, .35f);
    static Material Sand => Surface("SandStone", new Color32(192,183,163,255), .18f);
    static Material Oat => Surface("OatStone", new Color32(206,198,178,255), .18f);
    static Material Water => Surface("SeaGlass", new Color32(104,174,177,255), .70f);

    public static string Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        CatHomeEditPreview.Clear(); ApplyProductPrefabs();
        var scene = SceneManager.GetSceneByPath(HomeRoomService.BathroomScenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(HomeRoomService.BathroomScenePath, OpenSceneMode.Additive);
        try
        {
            ApplyScene(scene);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        return "Bathroom camera, architecture, fixture finishes and home accents saved.";
    }

    public static void ApplyProductPrefabs()
    {
        foreach (var product in StoreCatalogAssets.PlaceableProducts.Where(p =>
                     HomeStoreService.IsProductInRoomCollection(HomeRoomService.BathroomId, p.ProductId)))
            ApplyProductPrefab("Assets/Art/StoreProducts/Prefabs/" + product.PrefabName + ".prefab");
    }

    public static void ApplyProductPrefab(string path)
    {
        if (!Path.GetFileName(path).StartsWith("Bathroom", StringComparison.Ordinal)) return;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var display = root.GetComponent<StoreProductDisplay>();
            if (display == null) return;
            ProductMaterials(root.transform, display.ProductId);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void ProductMaterials(Transform root, string id)
    {
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.name == "LitterSandSurface") continue;
            var materials = renderer.sharedMaterials;
            for (int i=0;i<materials.Length;i++)
            {
                var material = materials[i]; if (material == null) continue;
                if (AssetDatabase.GetAssetPath(material).StartsWith(Folder, StringComparison.Ordinal)) continue;
                var source = ModernWorldArtBuilder.ResolveSourceMaterial(material);
                if (AssetDatabase.GetAssetPath(source).StartsWith(Folder + "/", StringComparison.Ordinal) ||
                    id == HomeStoreService.BathroomLaundryHamperId && source.name == "LC_Oak")
                { materials[i] = source; continue; }
                string name = source.name.ToLowerInvariant();
                if (name.Contains("gold")) materials[i] = id == HomeStoreService.BathroomLaundryHamperId ? LivingCompositionBuilder.Material("LC_Oak") : Brass;
                else if (name.Contains("cream"))
                    materials[i] = id == HomeStoreService.BathroomTubId || id == HomeStoreService.BathroomShowerId ? Mint :
                        id == HomeStoreService.BathroomVanityId || id == HomeStoreService.BathroomTowelStorageId ? Petrol : Cream;
                else if (name.Contains("white")) materials[i] = Cream;
                else if (name.Contains("coral") || name.Contains("pink")) materials[i] = Coral;
                else if (name.Contains("aqua")) materials[i] = Water;
                else if (name.Contains("mint")) materials[i] = Mint;
                else if (name.Contains("lilac")) materials[i] = Seafoam;
                else if (name.Contains("lemon")) materials[i] = Oat;
            }
            renderer.sharedMaterials = materials;
        }
    }

    public static void ApplyScene(Scene scene)
    {
        if (scene.path != HomeRoomService.BathroomScenePath) return;
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (var camera in all.Select(t=>t.GetComponent<Camera>()).Where(c=>c!=null)) HomeRoomCameraProfile.Apply(camera);
        foreach (var product in all.Select(t=>t.GetComponent<StoreProductDisplay>()).Where(p=>p!=null)) ProductMaterials(product.transform, product.ProductId);
        foreach (var renderer in all.Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r!=null))
        {
            if (renderer.GetComponentInParent<StoreProductDisplay>() != null) continue;
            if (renderer.GetComponentInParent<CatMovement>() != null) continue;
            string name = renderer.name;
            if (name.StartsWith("RibbonTile_") || name.StartsWith("Bubble"))
            { renderer.enabled = false; continue; }
            if (name.StartsWith("Tile_"))
            {
                var parts = name.Split('_');
                renderer.sharedMaterial = (int.Parse(parts[1])+int.Parse(parts[2]))%2==0 ? Sand : Oat;
            }
            else if (name == "WalkableFloor") renderer.sharedMaterial = Sand;
            else if (name.Contains("Wall_Cream")) renderer.sharedMaterial = name.StartsWith("Right") ? Petrol : Sage;
            else if (name.Contains("Wainscot")) renderer.sharedMaterial = Petrol;
            else if (name == "RoomWallPanel_Premium") renderer.sharedMaterials = new[]{Petrol,Cream,Brass};
            else if (name == "RoomDoor_Premium") renderer.sharedMaterials = new[]{Cream,Mint,Brass};
            else if (name.Contains("Baseboard") || name.Contains("PictureRail") || name.Contains("Crown_") || name.Contains("FloorInlay"))
                renderer.sharedMaterial = name.Contains("Gold") || name.Contains("Inlay") ? Brass : Cream;
        }
        var old=all.FirstOrDefault(t=>t.name==DecorRoot);
        if (old!=null) Object.DestroyImmediate(old.gameObject);
        var root=new GameObject(DecorRoot).transform; SceneManager.MoveGameObjectToScene(root.gameObject,scene);
        var window=new GameObject("BathroomGardenWindow").transform;window.SetParent(root,false);
        window.SetPositionAndRotation(new Vector3(3.71f,1.72f,.75f),Quaternion.Euler(0,90,0));
        window.localScale=new Vector3(.90f,.75f,1);
        Box(window,"WindowBacking",Vector3.zero,new Vector3(.94f,.87f,.035f),Petrol);
        Box(window,"FrameLeft",new Vector3(-.49f,0,-.03f),new Vector3(.06f,.99f,.08f),Cream);
        Box(window,"FrameRight",new Vector3(.49f,0,-.03f),new Vector3(.06f,.99f,.08f),Cream);
        Box(window,"FrameTop",new Vector3(0,.49f,-.03f),new Vector3(1.04f,.06f,.08f),Cream);
        Box(window,"FrameBottom",new Vector3(0,-.49f,-.03f),new Vector3(1.04f,.06f,.08f),Cream);
        var landscape=GameObject.CreatePrimitive(PrimitiveType.Quad);landscape.name="GardenView";
        Object.DestroyImmediate(landscape.GetComponent<Collider>());landscape.transform.SetParent(window,false);
        landscape.transform.localPosition=new Vector3(0,0,-.025f);landscape.transform.localScale=new Vector3(.94f,.87f,1);
        landscape.GetComponent<MeshRenderer>().sharedMaterial=LivingCompositionBuilder.Material("LC_WindowLandscape");
        Box(window,"Mullion",new Vector3(0,0,-.05f),new Vector3(.035f,.90f,.025f),Cream);
        Box(window,"Sill",new Vector3(0,-.53f,-.09f),new Vector3(1.12f,.07f,.24f),Cream);
        var plant=LivingCompositionBuilder.Model("SmallCeramicPlant",root);plant.name="WindowHerb";
        plant.transform.position=new Vector3(3.53f,1.35f,.95f);plant.transform.localScale=Vector3.one*.40f;
        Box(root,"HamperShelf",new Vector3(-1.50f,1.34f,2.48f),new Vector3(.55f,.055f,.27f),Cream);
        plant=LivingCompositionBuilder.Model("SmallCeramicPlant",root);plant.name="FreshGreenery";
        plant.transform.position=new Vector3(-1.48f,1.369f,2.44f);plant.transform.localScale=Vector3.one*.62f;
        var print=LivingCompositionBuilder.Model("SmallWallFrame",root);print.name="BathBotanicalPrint";
        print.transform.SetPositionAndRotation(new Vector3(-1.45f,1.92f,2.73f),Quaternion.identity);
        print.transform.localScale=Vector3.one*.72f;
        var art=new GameObject("BotanicalPrint",typeof(MeshFilter),typeof(MeshRenderer));art.transform.SetParent(print.transform,false);
        art.transform.localPosition=new Vector3(0,0,-.02f);art.transform.localScale=new Vector3(.352f,.492f,1);
        art.GetComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(LivingCompositionBuilder.Art+"/ArtPanel1.asset");
        art.GetComponent<MeshRenderer>().sharedMaterial=LivingCompositionBuilder.Material("LC_Art");
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=position;go.transform.localScale=scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
    }
}
