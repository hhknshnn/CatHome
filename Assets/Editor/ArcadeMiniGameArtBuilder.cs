using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Original toy-town and garden clubhouse presentation. Apply after the base
/// mini-game builders. No player, collision, spawn, camera-count or light-count
/// changes; authored meshes and cloned materials are isolated from the home.
/// </summary>
public static class ArcadeMiniGameArtBuilder
{
    public const string AssetRoot = "Assets/Art/MiniGames/ArcadeWorlds";
    private const string Models = AssetRoot + "/Models/";
    private const string Materials = AssetRoot + "/Materials/";
    private const string PalettePath = AssetRoot + "/Textures/ArcadePalette.png";
    private static readonly Dictionary<string, Color> Colors = new Dictionary<string, Color>
    {
        ["Arcade_Coral"] = new Color(1f, .255f, .25f),
        ["Arcade_Sky"] = new Color(.11f, .58f, .89f),
        ["Arcade_Teal"] = new Color(.025f, .49f, .47f),
        ["Arcade_Mint"] = new Color(.29f, .80f, .57f),
        ["Arcade_Sun"] = new Color(1f, .69f, .115f),
        ["Arcade_Cream"] = new Color(1f, .925f, .76f),
        ["Arcade_White"] = new Color(1f, .985f, .94f),
        ["Arcade_Ink"] = new Color(.04f, .16f, .225f),
        ["Arcade_Lilac"] = new Color(.49f, .32f, .77f),
        ["Arcade_Road"] = new Color(.36f, .67f, .71f),
        ["Arcade_Sand"] = new Color(.94f, .70f, .41f),
        ["Arcade_Grass"] = new Color(.245f, .63f, .38f)
    };

    public static void ApplyRunner(Transform root)
    {
        CheckAuthoring(root);
        EnsureAssets();
        int index = 0;
        foreach (var scenery in root.GetComponentsInChildren<CatRunnerScenerySegment>(true)
                     .OrderBy(s => s.transform.localPosition.z))
        {
            var segment = scenery.transform;
            RemoveDirect(segment, t => t.name == "BoulevardStreet" || t.name == "ArcadeStreet");
            Model("ArcadeStreet", segment, Vector3.zero);
            var near = segment.Find("NearScenery");
            if (near == null)
                throw new InvalidOperationException("Runner scenery is missing NearScenery: " + segment.name);
            // Remove only the explicitly superseded visible models. Old variant IDs,
            // off-screen theme content and recycling roots are not reconstructed here.
            RemoveDirect(near, t => t.name.StartsWith("Arcade", StringComparison.Ordinal) ||
                t.gameObject.activeSelf && (t.name.StartsWith("BoulevardFacade", StringComparison.Ordinal) ||
                                            t.name == "RunnerFestivalSpan"));
            for (int side = -1; side <= 1; side += 2)
            {
                int facade = (index + (side + 1) / 2) % 3;
                Model("ArcadeFacade" + facade, near, new Vector3(side * 4.40f, .10f, -.55f), side * 65f);
                Model("ArcadeFacade" + ((facade + 1) % 3), near,
                    new Vector3(side * 7.3f, .08f, 2.15f), side * 38f, .9f);
                if ((index + (side + 1) / 2) % 2 == 0)
                    Model("ArcadePalm", near, new Vector3(side * 3.62f, .156f, -2.48f), side * 32f, .84f);
            }
            if (index % 4 == 1)
                Model("ArcadeFestivalArch", near, new Vector3(0, .156f, 2.2f));
            RecolorVisible(near);
            foreach (string variantName in CatRunnerContentBuilder.SceneryVariantNames)
            {
                var variant = segment.Find(variantName);
                if (variant != null)
                    RecolorVisible(variant);
            }
            // The surrounding ground still participates in the existing, slowly
            // recycled theme system; only its private mini-game palette changes.
            var themeSegment = segment.GetComponent<CatRunnerThemeSegment>();
            if (themeSegment != null)
            {
                var settings = new SerializedObject(themeSegment);
                SetMaterials(settings, "floorThemes", new[]
                {
                    FlatMaterial("RunnerGardenGround0", new Color(.23f, .62f, .56f)),
                    FlatMaterial("RunnerGardenGround1", new Color(.23f, .56f, .64f)),
                    FlatMaterial("RunnerGardenGround2", new Color(.34f, .64f, .49f))
                });
                settings.ApplyModifiedPropertiesWithoutUndo();
                themeSegment.ApplyTheme(themeSegment.CurrentTheme);
            }
            index++;
        }
        // Original nine landmark silhouettes and rules survive; brighter skin
        // separates obstacles from the blue-green road without changing bounds.
        var templates = root.Find("ScrollingTrack/Templates");
        if (templates != null)
            foreach (var item in templates.GetComponentsInChildren<CatRunnerTrackObject>(true))
                if (item.Kind != CatRunnerTrackObjectKind.Coin)
                    RecolorVisible(item.transform);
        RemoveDirect(root, t => t.name == "ArcadeCloudBank");
        Model("ArcadeCloudBank", root, new Vector3(0, 0, 48f));
        var camera = root.GetComponentInChildren<Camera>(true);
        if (camera != null)
        {
            var horizon = camera.transform.Find("SoftHorizon");
            if (horizon != null)
            {
                var renderer = horizon.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var sky = MaterialAsset("ArcadeSky", "CatHome/MiniGameSky");
                    sky.SetColor("_Top", new Color32(64, 174, 231, 255));
                    sky.SetColor("_Bottom", new Color32(184, 235, 221, 255));
                    EditorUtility.SetDirty(sky);
                    renderer.sharedMaterial = sky;
                }
            }
        }
        var theme = root.GetComponentInChildren<CatRunnerThemeController>(true);
        if (theme != null)
        {
            var settings = new SerializedObject(theme);
            SetColors(settings, "skyColors", new Color32(64, 174, 231, 255),
                new Color32(102, 169, 226, 255), new Color32(62, 179, 205, 255));
            SetColors(settings, "fogColors", new Color32(188, 234, 226, 255),
                new Color32(226, 216, 191, 255), new Color32(177, 222, 229, 255));
            SetColors(settings, "ambientColors", new Color32(227, 238, 229, 255),
                new Color32(239, 225, 215, 255), new Color32(218, 232, 241, 255));
            SetColors(settings, "lightColors", new Color32(255, 246, 217, 255),
                new Color32(255, 226, 201, 255), new Color32(231, 244, 255, 255));
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        ModernWorldArtBuilder.ApplyRunner(root);
        PlayfulRunnerBuilder.Apply(root);
        EditorUtility.SetDirty(root.gameObject);
        AssetDatabase.SaveAssets();
    }

    public static void ApplyCatch(Transform root)
    {
        CheckAuthoring(root);
        EnsureAssets();
        var arena = root.Find("CatchArena");
        if (arena == null)
            throw new InvalidOperationException("Catch arena not found.");
        RemoveDirect(arena, t => t.name == "CatchPlayroom" ||
            t.name == "ArcadeGardenArena" || t.name == "ArcadePalm");
        Model("ArcadeGardenArena", arena, Vector3.zero);
        for (int side = -1; side <= 1; side += 2)
            Model("ArcadePalm", arena, new Vector3(side * 5.45f, -.02f, 3.32f), -side * 18f, 1.22f);
        foreach (var mouse in root.GetComponentsInChildren<CatCatchMouse>(true))
        {
            RecolorVisible(mouse.transform);
            foreach (var ring in mouse.GetComponentsInChildren<LineRenderer>(true))
            {
                if (ring.name != "SelectedMouseRing")
                    continue;
                var ringMaterial = MaterialAsset("CatchTarget", "Universal Render Pipeline/Unlit");
                ringMaterial.color = Colors["Arcade_Coral"];
                if (ringMaterial.HasProperty("_BaseColor"))
                    ringMaterial.SetColor("_BaseColor", Colors["Arcade_Coral"]);
                EditorUtility.SetDirty(ringMaterial);
                ring.sharedMaterial = ringMaterial;
            }
        }
        var camera = root.GetComponentInChildren<Camera>(true);
        if (camera != null)
            camera.backgroundColor = new Color32(111, 200, 218, 255);
        ModernWorldArtBuilder.ApplyCatch(root);
        EditorUtility.SetDirty(root.gameObject);
        AssetDatabase.SaveAssets();
    }

    private static void CheckAuthoring(Transform root)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Arcade art authoring must run outside Play Mode.");
        if (root == null)
            throw new ArgumentNullException(nameof(root));
    }

    private static void EnsureAssets()
    {
        if (!Directory.Exists(Models))
            throw new InvalidOperationException("Run headless build_arcade_worlds.py first.");
        if (!AssetDatabase.IsValidFolder(AssetRoot + "/Materials"))
            AssetDatabase.CreateFolder(AssetRoot, "Materials");
        var importer = AssetImporter.GetAtPath(PalettePath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Import ArcadePalette.png before applying the scene pass.");
        if (importer.mipmapEnabled || importer.filterMode != FilterMode.Point ||
            importer.textureCompression != TextureImporterCompression.Uncompressed ||
            importer.wrapMode != TextureWrapMode.Clamp || !importer.sRGBTexture || importer.isReadable)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.maxTextureSize = 256;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var material = MaterialAsset("ArcadePalette", "Universal Render Pipeline/Lit");
        material.color = Color.white;
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .28f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
    }

    private static GameObject Model(string name, Transform parent, Vector3 position, float yaw = 0, float scale = 1)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + name + ".fbx");
        if (source == null)
            throw new InvalidOperationException("Missing arcade world model: " + name);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
        model.name = name;
        model.transform.SetParent(parent, false);
        model.transform.localPosition = position;
        model.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        model.transform.localScale = Vector3.one * scale;
        var palette = AssetDatabase.LoadAssetAtPath<Material>(Materials + "ArcadePalette.mat");
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials = Enumerable.Repeat(palette, renderer.sharedMaterials.Length).ToArray();
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = name == "ArcadeCloudBank" ? ShadowCastingMode.Off : ShadowCastingMode.On;
        }
        return model;
    }

    private static void RecolorVisible(Transform boundary)
    {
        foreach (var renderer in boundary.GetComponentsInChildren<Renderer>(true))
        {
            bool visible = renderer.enabled;
            for (var item = renderer.transform; item != null && item != boundary; item = item.parent)
                if (!item.gameObject.activeSelf)
                    visible = false;
            if (!visible || renderer is LineRenderer || renderer is ParticleSystemRenderer)
                continue;
            renderer.sharedMaterials = renderer.sharedMaterials.Select(AccentMaterial).ToArray();
        }
    }

    private static Material AccentMaterial(Material source)
    {
        if (source == null)
            return null;
        source = ModernWorldArtBuilder.ResolveSourceMaterial(source);
        if (source.shader != null && source.shader.name == ModernWorldArtBuilder.ShaderName)
            return source;
        // Palette meshes are already one-material assets. Never repaint their UV swatches.
        string sourcePath = AssetDatabase.GetAssetPath(source);
        if (sourcePath.StartsWith(AssetRoot + "/", StringComparison.Ordinal))
            return source;
        string name = source.name.Replace(" (Instance)", "");
        string target;
        switch (name)
        {
            case "CH_CoralBright": case "CH_Pink": case "CH_Terracotta": target = "Arcade_Coral"; break;
            case "CH_MintBright": target = "Arcade_Mint"; break;
            case "CH_TealLight": case "CH_Teal": target = "Arcade_Teal"; break;
            case "CH_LilacBright": target = "Arcade_Lilac"; break;
            case "CH_Cream": case "CH_Paving": target = "Arcade_Sand"; break;
            case "CH_White": case "CH_Porcelain": case "CH_Stucco": target = "Arcade_Cream"; break;
            case "CH_Roof": case "CH_Ink": target = "Arcade_Ink"; break;
            case "CH_Gold": target = "Arcade_Sun"; break;
            case "CH_Screen": target = "Arcade_Sky"; break;
            case "CH_Leaf": target = "Arcade_Grass"; break;
            default: return source;
        }
        return FlatMaterial(target, Colors[target]);
    }

    private static Material FlatMaterial(string name, Color color)
    {
        var material = MaterialAsset(name, "Universal Render Pipeline/Lit");
        material.color = color;
        material.mainTexture = null;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .3f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material MaterialAsset(string name, string shaderName)
    {
        string path = Materials + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;
        var shader = Shader.Find(shaderName);
        if (shader == null)
            throw new InvalidOperationException("Required mini-game shader not found: " + shaderName);
        material = new Material(shader) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void RemoveDirect(Transform parent, Func<Transform, bool> predicate)
    {
        foreach (var child in parent.Cast<Transform>().Where(predicate).ToArray())
            UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static void SetMaterials(SerializedObject settings, string name, Material[] materials)
    {
        var property = settings.FindProperty(name);
        property.arraySize = materials.Length;
        for (int i = 0; i < materials.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
    }

    private static void SetColors(SerializedObject settings, string name, params Color[] colors)
    {
        var property = settings.FindProperty(name);
        property.arraySize = colors.Length;
        for (int i = 0; i < colors.Length; i++)
            property.GetArrayElementAtIndex(i).colorValue = colors[i];
    }
}
