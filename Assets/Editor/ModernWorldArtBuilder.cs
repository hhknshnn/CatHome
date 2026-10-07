using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reproducible final material pass. Preserves every mesh, collider, product
/// transform, renderer enable state, activity anchor, camera and light.
/// Blender-authored packed micro-surfaces use one object-metric texture sample.
/// </summary>
public static class ModernWorldArtBuilder
{
    public const string AssetRoot = "Assets/Art/ModernPolish";
    public const string ShaderName = "CatHome/Modern Surface";
    const string SourceTag = "ModernSourcePath";
    static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>(StringComparer.Ordinal);
    static readonly string[] Families = { "Oak", "Linen", "Stone", "Ceramic", "Plaster", "Leaf", "Rattan", "Suede" };

    public static string ApplyAllRooms()
    {
        CheckEdit(); EnsureAssets(); int renderers = 0;
        foreach (var room in HomeRoomService.Rooms)
        {
            var scene = SceneManager.GetSceneByPath(room.ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath, OpenSceneMode.Additive);
            try
            {
                renderers += Apply(scene, room.Id);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
        return "Modern materials: " + HomeRoomService.Rooms.Count + " rooms / " + renderers + " renderers. Geometry, transforms, physics, lights and cameras unchanged.";
    }

    public static int Apply(Scene scene, string roomId)
    {
        CheckEdit(); EnsureAssets(); int count = 0;
        if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A loaded scene is required.");
        foreach (var root in scene.GetRootGameObjects()) count += ApplyRoot(root.transform, roomId);
        return count;
    }

    public static int ApplyRunner(Transform root) => ApplyRoot(root, "runner");
    public static int ApplyCatch(Transform root) => ApplyRoot(root, "catch");

    /// <summary>Finish the existing title stage without rebuilding its set or lighting.</summary>
    public static string ApplyTitleStage()
    {
        CheckEdit(); EnsureAssets();
        string path = TitleShowcaseContentBuilder.PrefabPath;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            int count = ApplyRoot(root.transform, HomeRoomService.LivingRoomId);
            if (count > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            // Existing derived assets can receive parameter updates even when
            // their renderer references already match and count is zero.
            AssetDatabase.SaveAssets();
            return "Title stage finished: " + count + " renderer references updated. Existing geometry, transforms, cameras and lights preserved.";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// <summary>Use after a product or world builder, before previews are captured.</summary>
    public static int ApplyRoot(Transform root, string roomId = "livingroom")
    {
        CheckEdit(); EnsureAssets();
        if (root == null) throw new ArgumentNullException(nameof(root));
        roomId = Profile(roomId);
        Cache.Clear();
        int count = 0;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.GetComponentInParent<CatMovement>() != null ||
                renderer.GetComponentInParent<Canvas>() != null ||
                renderer.GetComponentInParent<ParticleSystem>() != null) continue;
            string context = Context(renderer.transform);
            if (Contains(context, "televisionscreen", "soft horizon", "softhorizon", "skybackdrop", "cloudbank", "sunbeam", "windowglass", "selectedmousering")) continue;
            var materials = renderer.sharedMaterials; bool changed = false;
            for (int index = 0; index < materials.Length; index++)
            {
                var target = Derive(materials[index], context, roomId);
                if (target == materials[index]) continue;
                materials[index] = target; changed = true;
            }
            if (!changed) continue;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer); count++;
        }

        // Recycling must retain the finish: runtime ApplyTheme uses these arrays.
        foreach (var segment in root.GetComponentsInChildren<CatRunnerThemeSegment>(true))
        {
            var serialized = new SerializedObject(segment);
            foreach (var propertyName in new[] { "floorThemes", "detailThemes", "laneThemes", "edgeThemes", "accentThemes" })
            {
                var array = serialized.FindProperty(propertyName);
                if (array == null || !array.isArray) continue;
                for (int i = 0; i < array.arraySize; i++)
                {
                    var slot = array.GetArrayElementAtIndex(i);
                    if (slot.objectReferenceValue is Material material)
                        slot.objectReferenceValue = Derive(material, propertyName.ToLowerInvariant(), "runner");
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        return count;
    }

    /// <summary>Catalog templates must match the room and its future purchases.</summary>
    public static string ApplyCatalogPrefabs()
    {
        CheckEdit(); EnsureAssets(); int count = 0;
        const string folder = "Assets/Art/StoreProducts/Prefabs";
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }).OrderBy(g => g, StringComparer.Ordinal))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (ApplyCatalogPrefab(path)) count++;
        }
        AssetDatabase.SaveAssets();
        return count + " catalog prefabs finished, without transform or geometry edits.";
    }

    public static bool ApplyCatalogPrefab(string path)
    {
        CheckEdit();
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/Art/StoreProducts/Prefabs/", StringComparison.Ordinal) ||
            !path.EndsWith(".prefab", StringComparison.Ordinal))
            throw new ArgumentException("A catalog prefab path is required.", nameof(path));
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (ApplyRoot(root.transform, RoomFromName(Path.GetFileNameWithoutExtension(path))) == 0) return false;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return true;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Earlier generators use canonical names, never the derived display name.
    public static Material ResolveSourceMaterial(Material material) => material == null ? null : Original(material);

    static Material Derive(Material current, string context, string roomId)
    {
        if (current == null) return null;
        var source = Original(current);
        string sourcePath = AssetDatabase.GetAssetPath(source);
        // Reviewed bathroom finishes are shared by placed fixtures and catalog photos.
        if (sourcePath.StartsWith(BathroomThemeBuilder.Folder + "/", StringComparison.Ordinal) ||
            sourcePath.StartsWith(RemainingRoomsThemeBuilder.Folder + "/", StringComparison.Ordinal)) return source;
        string n = source.name.ToLowerInvariant();
        // Missing/deleted provenance must never create a Modern-from-Modern
        // chain or compound a previously approved tint on a repeated pass.
        if (source.shader != null && source.shader.name == ShaderName) return current;
        if (source.shader == null || (source.shader.name != "Universal Render Pipeline/Lit" && source.shader.name != ShaderName)) return current;
        if (sourcePath.IndexOf("polyperfect", StringComparison.OrdinalIgnoreCase) >= 0 ||
            Contains(n, "cat_game", "m_cat_", "coin", "pawcoin", "water", "mirror", "screen", "glass", "cloud", "sunbeam", "lockplate", "hazardwarning", "power", "fx", "food") ||
            n.Contains("sky") && !n.Contains("wall")) return current;
        if (source.HasProperty("_Surface") && source.GetFloat("_Surface") > .5f) return current;
        if (string.IsNullOrEmpty(sourcePath)) return current;

        string family = Family(n, context);
        string key = sourcePath + "|" + source.name + "|" + family + "|" + roomId;
        // Architecture gets room identity; the same catalog item keeps one finish
        // in its photograph and its placed instance.
        bool architecture = Contains(context, "wall", "wainscot", "floor", "plank", "paving", "ceiling", "rail", "roof");
        string colorRole = architecture ? "architecture" : "product";
        key += "|" + colorRole;
        if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
        string safeName = string.Concat(source.name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
        string path = AssetRoot + "/Materials/Modern_" + safeName + "_" + family + "_" + Hash128.Compute(key).ToString().Substring(0, 12) + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.CopyPropertiesFromMaterial(source);
        material.shader = Shader.Find(ShaderName);
        material.SetOverrideTag(SourceTag, sourcePath);
        material.SetOverrideTag("ModernSourceName", source.name);
        material.SetOverrideTag("ModernSurfaceFamily", family);
        // Broad lawn and foliage volumes need organic variation. The authored
        // leaf-vein sample is unsuitable when projected over entire hills.
        string detailFamily = family == "Leaf" ? "Plaster" : family;
        material.SetTexture("_DetailAlbedoMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AssetRoot + "/Textures/Modern_" + detailFamily + "_Surface.png"));
        material.SetTexture("_DetailNormalMap", null);
        material.DisableKeyword("_DETAIL_MULX2"); material.DisableKeyword("_DETAIL_SCALED");
        float tile = family == "Oak" ? 1.15f : family == "Linen" ? 2.0f : family == "Rattan" ? 2.1f : family == "Leaf" ? .7f : family == "Suede" ? 2.4f : .90f;
        material.SetTextureScale("_DetailAlbedoMap", new Vector2(tile, tile));
        material.SetTextureOffset("_DetailAlbedoMap", Vector2.zero);
        // Room-scale captures set the budget: grain supports the silhouette
        // and color instead of becoming high-contrast waves across the floor.
        material.SetFloat("_DetailAlbedoMapScale", DetailContrast(family, architecture));
        material.SetFloat("_DetailNormalMapScale", DetailRelief(family, architecture));
        material.SetFloat("_Smoothness", Smoothness(family));
        if (Contains(n, "gold", "brass", "champagne"))
        {
            material.SetFloat("_Metallic", .64f); material.SetFloat("_Smoothness", .57f);
            material.SetFloat("_DetailNormalMapScale", .00003f);
        }
        else if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        Color original = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
        var albedo = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
        bool authoredAlbedo = albedo != null && albedo.width > 32 && albedo.height > 32;
        Color color = authoredAlbedo ? original : Palette(original, n, context, family, roomId, architecture);
        material.SetColor("_BaseColor", color); material.SetColor("_Color", color);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material); Cache.Add(key, material);
        return material;
    }

    static Material Original(Material material)
    {
        if (material.shader == null || material.shader.name != ShaderName) return material;
        string path = material.GetTag(SourceTag, false, "");
        string name = material.GetTag("ModernSourceName", false, "");
        if (string.IsNullOrEmpty(path)) return material;
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().FirstOrDefault(m => m.name == name) ??
               AssetDatabase.LoadAssetAtPath<Material>(path) ?? material;
    }

    static string Family(string material, string context)
    {
        // Verified single-mesh FBX slots need a few exact substance overrides;
        // a room or furniture ancestor must not turn appliances into oak or
        // the hammock's separate rigid stand into upholstered fabric.
        if (material == "ch_cream")
        {
            if (ContextNode(context, "kitchenrefrigerator", "kitchenrefrigerator_premiummodel", "kitchenstoveoven", "kitchenstoveoven_premiummodel"))
                return "Ceramic";
            if (ContextNode(context, "patioparasol", "patioparasol_premiummodel", "gardenhammockbed", "hammockbed"))
                return "Linen";
        }
        if ((material == "ch_white" || material == "ch_mintbright") &&
            ContextNode(context, "gardenhammock_premiummodel"))
            return "Plaster";
        if (Contains(material, "leaf", "grass", "foliage") || Contains(context, "arcadepalm", "roomfoliage", "gardenbush")) return "Leaf";
        if (Contains(material, "wood", "oak", "parquet", "deck") || Contains(context, "plank")) return "Oak";
        if (Contains(material, "stone", "paving", "pavement") || Contains(context, "arcadestreet", "arcadegardenarena", "floorthemes", "terracefloor")) return "Stone";
        if (Contains(material, "fabric", "rug", "carpet", "cloth")) return "Linen";
        bool soft = Contains(context, "sofa", "armchair", "cushion", "pillow", "blanket", "tunnel", "bed", "beanbag", "hammock", "canopy", "awning", "curtain", "mat_", "bathmat", "sunmat", "rug", "towel", "yarn");
        if (Contains(context, "basket", "hamper", "rattan") && Contains(material, "cream", "gold", "wood")) return "Rattan";
        if (Contains(material, "cream", "sand") && !Contains(context, "wall", "tile", "ceramic", "bowl", "cloud", "stone", "porcelain")) return "Oak";
        if (soft && !Contains(material, "gold", "ink", "teal", "screen")) return "Linen";
        if (Contains(material, "porcelain", "ceramic", "gold", "brass", "champagne") || Contains(context, "bowl", "vase", "pottery", "washbasin")) return "Ceramic";
        if (Contains(context, "mouse") && !Contains(material, "eye", "ink")) return "Suede";
        return "Plaster";
    }

    static float Smoothness(string family)
    {
        switch (family)
        {
            case "Oak": return .35f; case "Linen": return .12f; case "Stone": return .19f;
            case "Ceramic": return .66f; case "Leaf": return .16f; case "Rattan": return .16f;
            case "Suede": return .10f; default: return .22f;
        }
    }

    static float DetailContrast(string family, bool architecture)
    {
        switch (family)
        {
            case "Oak": return architecture ? .05f : .14f;
            case "Leaf": return .08f;
            case "Linen": return .28f;
            case "Rattan": return .35f;
            case "Ceramic": return .30f;
            case "Stone": return .20f;
            case "Suede": return .24f;
            default: return .22f;
        }
    }

    static float DetailRelief(string family, bool architecture)
    {
        switch (family)
        {
            case "Oak": return architecture ? .000012f : .000035f;
            case "Leaf": return .000015f;
            case "Linen": return .00030f;
            case "Rattan": return .00050f;
            case "Stone": return .00010f;
            case "Ceramic": return .00004f;
            case "Suede": return .00010f;
            default: return .00008f;
        }
    }

    static Color Palette(Color source, string material, string context, string family, string roomId, bool architecture)
    {
        // Atlas colors remain authored and canonical; never multiply a palette
        // texture by a product tint or a wall identity.
        if (material.Contains("palette")) return source;
        if (family == "Oak" || family == "Rattan")
        {
            if (source.r < .22f && source.g < .22f && source.b < .22f) return source;
            return Contains(material, "light", "cream", "sand") ? C(205, 174, 133) : C(177, 137, 94);
        }
        if (family == "Leaf") return Contains(material, "shade", "deep", "dark") ? C(45,112,69) : C(88,151,77);
        if (Contains(material, "gold", "brass", "champagne")) return C(218,171,86);
        if (architecture && Contains(material, "wall") && !Contains(material, "wainscot"))
        {
            switch (roomId)
            {
                case "livingroom": return Contains(material, "left", "deep") ? C(35,101,112) : C(169,196,205);
                case "bathroom": return C(157,210,211);
                case "kitchen": return C(247,215,148);
                case "bedroom": return Contains(material,"deep") ? C(119,131,172) : C(181,189,217);
                case "secondfloor": return Contains(material,"deep") ? C(60,83,110) : C(196,205,210);
            }
        }
        if (family == "Linen")
        {
            if (Contains(material, "coral", "pink", "peach")) return roomId == "bedroom" ? C(213,143,159) : C(236,121,104);
            if (Contains(material, "mint", "aqua")) return C(100,191,175);
            if (Contains(material, "lilac")) return C(157,162,204);
            if (Contains(material, "white", "pearl", "ivory")) return C(245,236,216);
        }
        if (Contains(material, "white", "pearl", "ivory") || architecture && Contains(material,"cream")) return C(245,239,222);
        return source;
    }

    static Color C(byte red, byte green, byte blue) => new Color32(red, green, blue, 255);
    static bool Contains(string value, params string[] fragments) => fragments.Any(value.Contains);
    static bool ContextNode(string context, params string[] names) => context.Split('/').Any(node => names.Contains(node));
    static string Context(Transform transform)
    {
        string context = "";
        for (int i = 0; transform != null && i < 5; i++, transform = transform.parent)
        {
            string name = transform.name.ToLowerInvariant();
            // Room containers describe location, not the object's substance.
            // BedroomNightstand is a nightstand; it is not upholstered because
            // the room prefix happens to contain "bed". The same applies to
            // SecondFloor product roots and the "floor" architecture role.
            if (name == "01 environment" || name == "roomfurniture" || name.EndsWith("_level01", StringComparison.Ordinal)) break;
            foreach (var room in new[] { "livingroom", "living room", "living-room", "bedroom", "bathroom", "secondfloor", "second floor", "second-floor" })
                name = name.Replace(room, "");
            context += "/" + name;
            if (transform.GetComponent<StoreProductDisplay>() != null) break;
        }
        return context;
    }
    static string RoomFromName(string name)
    {
        string n = name.ToLowerInvariant();
        foreach (var id in new[] { "bathroom", "kitchen", "bedroom", "garden", "balcony", "patio", "secondfloor" })
            if (n.StartsWith(id, StringComparison.Ordinal)) return id;
        return n.StartsWith("loft", StringComparison.Ordinal) ? "secondfloor" : "livingroom";
    }
    static string Profile(string id)
    {
        string compact = (id ?? "livingroom").Replace("-", "").ToLowerInvariant();
        foreach (var profile in new[] { "livingroom", "bathroom", "kitchen", "bedroom", "garden", "balcony", "patio", "secondfloor", "runner", "catch" })
            if (compact.StartsWith(profile, StringComparison.Ordinal)) return profile;
        return "livingroom";
    }
    static void CheckEdit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Modern world art is an Edit Mode authoring operation.");
    }
    public static void EnsureAssets()
    {
        if (!AssetDatabase.IsValidFolder(AssetRoot + "/Materials"))
        {
            Directory.CreateDirectory(AssetRoot + "/Materials"); AssetDatabase.Refresh();
        }
        if (Shader.Find(ShaderName) == null) throw new InvalidOperationException("Import ModernSurface.shader before applying the world finish.");
        foreach (var family in Families)
        {
            string path = AssetRoot + "/Textures/Modern_" + family + "_Surface.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Bake and import modern Blender surfaces first: " + path);
            var android = importer.GetPlatformTextureSettings("Android");
            bool changed = importer.sRGBTexture || !importer.mipmapEnabled || importer.isReadable ||
                importer.wrapMode != TextureWrapMode.Repeat || importer.filterMode != FilterMode.Trilinear ||
                importer.maxTextureSize != 512 || importer.anisoLevel != 2 || !android.overridden ||
                android.format != TextureImporterFormat.ETC2_RGB4 || android.maxTextureSize != 512;
            if (!changed) continue;
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = false;
            importer.mipmapEnabled = true; importer.isReadable = false; importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear; importer.maxTextureSize = 512; importer.anisoLevel = 2;
            importer.textureCompression = TextureImporterCompression.Compressed;
            android.name = "Android"; android.overridden = true; android.maxTextureSize = 512;
            android.format = TextureImporterFormat.ETC2_RGB4; android.compressionQuality = 70;
            importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
        }
    }
}
