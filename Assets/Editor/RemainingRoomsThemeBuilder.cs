using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Shared home finishes for the five remaining rooms. Only presentation changes.</summary>
public static class RemainingRoomsThemeBuilder
{
    public const string Folder = "Assets/Art/HomeRoomTheme";
    public static readonly string[] RoomIds = { HomeRoomService.BedroomId, HomeRoomService.GardenId,
        HomeRoomService.BalconyId, HomeRoomService.PatioId, HomeRoomService.SecondFloorId };
    static Material Shared(string role) => AssetDatabase.LoadAssetAtPath<Material>(BathroomThemeBuilder.Folder + "/" + role + ".mat")
        ?? throw new InvalidOperationException("Missing shared home finish " + role);
    static Material Cream => Shared("WarmPorcelain");
    static Material Sage => Shared("SagePlaster");
    static Material Petrol => Shared("PetrolPanels");
    static Material Mint => Shared("MintEnamel");
    static Material Seafoam => Shared("SeafoamEnamel");
    static Material Coral => Shared("CoralLinen");
    static Material Brass => Shared("BrushedBrass");
    static Material Sand => Shared("SandStone");
    static Material Oat => Shared("OatStone");
    static Material Water => Shared("SeaGlass");
    static Material Oak => LivingCompositionBuilder.Material("LC_Oak");
    static Material Local(string name) => AssetDatabase.LoadAssetAtPath<Material>(Folder + "/" + name + ".mat")
        ?? throw new InvalidOperationException("Missing home finish " + name);
    public static bool Handles(string roomId) => RoomIds.Contains(roomId);

    public static void EnsureMaterials()
    {
        Directory.CreateDirectory(Folder);
        Make("Lawn", new Color32(117,151,111,255), .12f);
        Make("Leaf", new Color32(95,142,113,255), .16f);
        Make("LeafShade", new Color32(52,99,78,255), .12f);
        Make("WeatheredOak", new Color32(164,141,114,255), .24f);
        Make("OakLight", new Color32(176,155,128,255), .24f);
        Make("DuskBlue", new Color32(94,134,148,255), .24f);
        Make("SkyHaze", new Color32(176,207,204,255), .10f);
        Make("Terracotta", new Color32(182,114,91,255), .22f);
    }
    static void Make(string name, Color color, float smoothness)
    {
        string path = Folder + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var material = new Material(Cream) { name = name };
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0); material.SetTexture("_BaseMap", null);
        AssetDatabase.CreateAsset(material, path);
    }
    static bool Reviewed(Material material)
    {
        string path = AssetDatabase.GetAssetPath(material);
        return path.StartsWith(Folder + "/", StringComparison.Ordinal) ||
            path.StartsWith(BathroomThemeBuilder.Folder + "/", StringComparison.Ordinal) || material.name.StartsWith("LC_");
    }
    public static void ApplyProductPrefabs()
    {
        EnsureMaterials();
        foreach (var product in StoreCatalogAssets.PlaceableProducts.Where(p => RoomIds.Any(r => HomeStoreService.IsProductInRoomCollection(r,p.ProductId))))
            ApplyProductPrefab("Assets/Art/StoreProducts/Prefabs/" + product.PrefabName + ".prefab");
    }
    public static void ApplyProductPrefab(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        if (!new[] {"Bedroom", "Garden", "Balcony", "Patio", "Loft"}.Any(p => name.StartsWith(p,StringComparison.Ordinal))) return;
        EnsureMaterials();
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var display = root.GetComponent<StoreProductDisplay>();
            if (display == null) return;
            ApplyFurniture(root.transform, display.ProductId);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    public static void ApplyFurniture(Transform root, string id)
    {
        string product = root.name.ToLowerInvariant();
        bool plants = product.Contains("fern") || product.Contains("herb") || product.Contains("sapling") || product.Contains("flower") || product.Contains("planter");
        bool lights = product.Contains("stringlight");
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterials = renderer.sharedMaterials.Select(material =>
            {
                if (material == null) return material;
                var source = ModernWorldArtBuilder.ResolveSourceMaterial(material);
                if (id == HomeStoreService.BalconySunAwningId && (source.name == "LC_Oak" || source.name == "CH_Cream")) return Oat;
                if (Reviewed(source)) return source;
                string n = source.name.ToLowerInvariant();
                if (n.Contains("gold")) return Brass;
                if (n.Contains("white")) return Cream;
                if (n.Contains("cream"))
                    return product.Contains("wardrobe") || product.Contains("bookcase") ? Petrol :
                        product.Contains("grill") ? Petrol : product.Contains("rug") || product.Contains("runner") || product.Contains("parasol") ? Oat : Oak;
                if (lights && (n.Contains("aqua") || n.Contains("mint") || n.Contains("coral") || n.Contains("lemon") || n.Contains("lilac"))) return Cream;
                if (n.Contains("mint")) return plants ? Local("Leaf") : Mint;
                if (n.Contains("aqua") || n.Contains("teal")) return plants ? Local("LeafShade") : Water;
                if (n.Contains("lilac") || n.Contains("purple")) return product.Contains("beanbag") || product.Contains("rug") || product.Contains("runner") ? Petrol : Seafoam;
                if (n.Contains("coral") || n.Contains("pink")) return Coral;
                if (n.Contains("lemon")) return Oat;
                return source;
            }).ToArray();
        }
    }
    public static string ApplyRoom(string roomId)
    {
        if (!Handles(roomId) || EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Reviewed room in Edit Mode required.");
        EnsureMaterials(); CatHomeEditPreview.Clear();
        HomeRoomService.TryGetRoom(roomId,out var room);
        var scene = SceneManager.GetSceneByPath(room.ScenePath); bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath,OpenSceneMode.Additive);
        try
        {
            ApplyScene(scene);
            var errors = HomeRoomArrangementValidation.Validate(scene,roomId);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n",errors));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
        return roomId + " theme and framing saved.";
    }
    public static void ApplyScene(Scene scene)
    {
        var all = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var marker = all.Select(t=>t.GetComponent<HomeRoomSceneMarker>()).FirstOrDefault(m=>m!=null);
        string room = marker == null ? null : marker.RoomId;
        if (!Handles(room)) return;
        EnsureMaterials();
        bool indoor = room == HomeRoomService.BedroomId || room == HomeRoomService.SecondFloorId;
        foreach (var c in all.Select(t=>t.GetComponent<Camera>()).Where(c=>c!=null)) HomeRoomCameraProfile.Apply(c);
        foreach (var p in all.Select(t=>t.GetComponent<StoreProductDisplay>()).Where(p=>p!=null)) ApplyFurniture(p.transform,p.ProductId);
        foreach (var r in all.Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r!=null))
        {
            if (r.GetComponentInParent<StoreProductDisplay>()!=null || r.GetComponentInParent<CatMovement>()!=null) continue;
            string n=r.name;
            var parents=r.GetComponentsInParent<Transform>(true);
            if (n.StartsWith("RibbonTile_") || parents.Any(t=>t.name.Contains("Threshold") || t.name=="Candy Corner Jewels" || t.name=="Garden Path Jewelry"))
            { r.enabled=false; continue; }
            // Remove the toy-like sky panes; replace with the living room's garden artwork below.
            if (indoor && new[]{"NightGlass","Moon","MoonCheek","SkyView","Glass","RooflineA","RooflineB","WindowSun"}.Contains(n))
            { r.enabled=false; continue; }
            if (n=="RoomWallPanel_Premium") {r.sharedMaterials=new[]{Petrol,Cream,Brass};continue;}
            if (n=="RoomDoor_Premium") {r.sharedMaterials=new[]{Cream,Mint,Brass};continue;}
            if (indoor && (n.Contains("Wall_Cream") || n.EndsWith("Wall_Loft"))) {r.sharedMaterial=n.StartsWith("Right")?Petrol:Sage;continue;}
            if (n.Contains("Wainscot")) {r.sharedMaterial=Petrol;continue;}
            if (n.StartsWith("Plank_")) {r.sharedMaterial=int.Parse(n.Substring(6))%3==0?Local("OakLight"):Local("WeatheredOak");continue;}
            if (n=="Facade") {r.sharedMaterial=Sage;continue;}
            if (n=="GlassDoor") {r.sharedMaterial=Petrol;continue;}
            if (n=="WindowFrame" || n.Contains("PictureRail") && !n.Contains("Gold")) {r.sharedMaterial=Cream;continue;}
            if (n=="WallBody") {r.sharedMaterial=Sage;continue;}
            if (n=="PremiumFoliage") {r.sharedMaterials=new[]{Local("LeafShade"),Local("Leaf")};continue;}
            if (n.StartsWith("GrassBlade_"))
            {
                // Keep low tufts at the edges, clear visual clutter from the walking area.
                if (Mathf.Abs(r.transform.position.x)<2.75f && r.transform.position.z<1.6f) r.enabled=false;
                r.sharedMaterial=Local("Lawn");continue;
            }
            r.sharedMaterials=r.sharedMaterials.Select(m=>ShellMaterial(m,n,indoor)).ToArray();
        }
        var window=all.FirstOrDefault(t=>t!=null && t.name==(room==HomeRoomService.BedroomId?"Bedroom Moon Window":"Loft Picture Window"));
        if (indoor && window!=null)
        {
            bool bedroom=room==HomeRoomService.BedroomId;
            window.localPosition=bedroom?new Vector3(-.1875f,.40f,0):new Vector3(0,.85f,0);
            window.localScale=bedroom?new Vector3(.75f,.65f,1):new Vector3(.80f,.60f,1);
            var view=window.Find("HomeGardenView"); if(view!=null)Object.DestroyImmediate(view.gameObject);
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="HomeGardenView";
            Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(window,false);
            go.transform.localPosition=bedroom?new Vector3(-.75f,2.25f,HomeRoomShellMetrics.BackWallDecorZ(.40f)):new Vector3(0,1.85f,HomeRoomShellMetrics.BackWallDecorZ(.07f));
            go.transform.localScale=bedroom?new Vector3(1.82f,1.16f,1):new Vector3(3f,1.9f,1);
            go.GetComponent<MeshRenderer>().sharedMaterial=LivingCompositionBuilder.Material("LC_WindowLandscape");
            go.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var accent=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="HomeWindowAccents");
            if(accent==null){accent=new GameObject("HomeWindowAccents");SceneManager.MoveGameObjectToScene(accent,scene);}
            if(accent.transform.childCount==0)
            {
                var plant=LivingCompositionBuilder.Model("SmallCeramicPlant",accent.transform);
                plant.transform.position=bedroom?new Vector3(-.14f,1.385f,2.32f):new Vector3(1.01f,1.40f,2.64f);
                plant.transform.localScale=Vector3.one*.48f;
            }
        }
        if (room==HomeRoomService.BedroomId)
        {
            foreach(string name in new[]{"Bedroom Paw Medallion","Dream Moon Wall Signature"})
            {
                var badge=all.FirstOrDefault(t=>t!=null&&t.name==name);
                if(badge!=null){badge.localPosition=new Vector3(-.91f,.485f,0);badge.localScale=Vector3.one*.65f;}
            }
        }
    }
    static Material ShellMaterial(Material material,string objectName,bool indoor)
    {
        if(material==null)return null;
        var source=ModernWorldArtBuilder.ResolveSourceMaterial(material);
        if(Reviewed(source))return source;
        string n=source.name.ToLowerInvariant();
        if(n.Contains("sky")||n.Contains("cityfar"))return Local("SkyHaze");
        if(n.Contains("citycool"))return Local("DuskBlue");
        if(n.Contains("citywarm"))return Sage;
        if(n.Contains("grassdeep")||n.Contains("leafshade"))return Local("LeafShade");
        if(n.Contains("grass")||n.Contains("leaf"))return Local("Lawn");
        if(n.Contains("gold")||objectName.Contains("Gold"))return Brass;
        if(n.Contains("wood")||n.Contains("deck"))return Local("WeatheredOak");
        if(n.Contains("stone"))return n.Contains("light")?Oat:Sand;
        if(n.Contains("cream")||n.Contains("pearl")||n.Contains("rail"))return Cream;
        if(n.Contains("coral")||n.Contains("pink")||n.Contains("peach"))return Coral;
        if(n.Contains("mint"))return indoor?Mint:Local("Leaf");
        if(n.Contains("aqua")||n.Contains("teal"))return Water;
        if(n.Contains("lilac"))return Seafoam;
        if(n.Contains("lemon"))return Oat;
        return source;
    }
}
