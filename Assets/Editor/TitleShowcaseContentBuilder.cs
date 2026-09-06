using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>Builds a presentation-only set from the same finished furniture used in the home.</summary>
public static class TitleShowcaseContentBuilder
{
    public const string Folder = "Assets/Art/Title/LiveCats";
    public const string PrefabPath = Folder + "/TitleShowcaseStage.prefab";
    public const string PosterPath = Folder + "/CatHome_LiveCats_HD.png";

    public static GameObject BuildSilently()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var root = new GameObject("TitleShowcaseStage");
        try
        {
            var cream = Material("Porcelain", new Color32(255, 238, 211, 255), .2f);
            var mint = Material("Mint", new Color32(106, 209, 184, 255), .15f);
            var gold = Material("Champagne", new Color32(232, 174, 75, 255), .55f, .35f);
            var pink = Material("Petal", new Color32(246, 154, 168, 255), .2f);
            var sky = Material("Sky", new Color32(147, 202, 185, 255), .1f);
            var wood = Material("Oak", new Color32(231, 195, 145, 255), .2f);
            Shape(root.transform, "Warm oak floor", PrimitiveType.Cube, new Vector3(0, -.13f, 0), new Vector3(24, .25f, 22), wood);
            Shape(root.transform, "Pearl wall", PrimitiveType.Cube, new Vector3(0, 3f, 3.6f), new Vector3(24, 6, .22f), cream);
            Shape(root.transform, "Mint skirting", PrimitiveType.Cube, new Vector3(0, .17f, 3.44f), new Vector3(24, .32f, .12f), mint);
            Shape(root.transform, "Gold skirting line", PrimitiveType.Cube, new Vector3(0, .34f, 3.4f), new Vector3(24, .025f, .06f), gold);
            for (int i = -12; i <= 12; i++)
                Shape(root.transform, "Floor inlay " + i, PrimitiveType.Cube, new Vector3(i * .72f, -.001f, 0), new Vector3(.008f, .003f, 18), cream);
            // Broad arched window, with a real dimensional frame and a clear sky beyond.
            Arch(root.transform, "Window pearl surround", 2.12f, 1.7f, new Vector3(.35f, .75f, 3.2f), cream);
            Arch(root.transform, "Window gold reveal", 2f, 1.7f, new Vector3(.35f, .82f, 3.14f), gold);
            Arch(root.transform, "Window sky", 1.93f, 1.7f, new Vector3(.35f, .87f, 3.08f), sky);
            Shape(root.transform, "Window upright", PrimitiveType.Cube, new Vector3(.35f, 2.32f, 3.02f), new Vector3(.065f, 2.9f, .12f), cream);
            Shape(root.transform, "Window crossbar", PrimitiveType.Cube, new Vector3(.35f, 2.35f, 3.01f), new Vector3(3.8f, .065f, .12f), cream);
            Shape(root.transform, "Cloud A", PrimitiveType.Sphere, new Vector3(-.65f, 3.37f, 3.01f), new Vector3(.75f, .18f, .08f), cream);
            Shape(root.transform, "Cloud B", PrimitiveType.Sphere, new Vector3(1.35f, 2.93f, 3.01f), new Vector3(.85f, .17f, .08f), cream);
            Shape(root.transform, "Rug petal binding", PrimitiveType.Cylinder, new Vector3(.3f, .03f, -.05f), new Vector3(4.8f, .03f, 3.45f), pink);
            Shape(root.transform, "Rug mint weave", PrimitiveType.Cylinder, new Vector3(.3f, .065f, -.05f), new Vector3(4.58f, .016f, 3.23f), mint);
            Furniture(root.transform, "BalconyCushionBench", new Vector3(.35f, 0, 2.45f), 0f, 1.12f);
            Furniture(root.transform, "TallHouseplant", new Vector3(2.95f, 0, 2.1f), -20f, 1.15f);
            Furniture(root.transform, "FloorLamp", new Vector3(-2.25f, 0, 2.15f), 15f, 1.12f);
            Furniture(root.transform, "LoftFloorCushions", new Vector3(2.75f, 0, .4f), -20f, .9f);

            var cameraObject = new GameObject("Showcase Render Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(-1.3f, 2.2f, -7.8f);
            cameraObject.transform.LookAt(new Vector3(-1.3f, .8f, .1f));
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 34f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 40f;
            camera.cullingMask = 1 << TitleCatShowcase.StageLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(185, 232, 228, 255);
            camera.allowHDR = true; camera.allowMSAA = true; camera.allowDynamicResolution = false;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.dithering = true;
            data.volumeLayerMask = 1 << TitleCatShowcase.StageLayer;
            data.volumeTrigger = camera.transform;
            Light(root.transform, "Soft afternoon key", new Vector3(42, -30, 0), new Color(1f, .97f, .91f), 1.45f, true);
            Light(root.transform, "Sky fill", new Vector3(28, 145, 0), new Color(.72f, .94f, 1f), .65f, false);
            Light(root.transform, "Peach edge", new Vector3(15, -145, 0), new Color(1f, .78f, .7f), .45f, false);
            var volume = new GameObject("Showcase Color").AddComponent<Volume>();
            volume.transform.SetParent(root.transform, false);
            volume.isGlobal = true;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder + "/ShowcaseColor.asset");
            if (profile == null) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, Folder + "/ShowcaseColor.asset"); }
            if (!profile.TryGet<Tonemapping>(out var tone)) { tone = profile.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone, profile); }
            tone.mode.Override(TonemappingMode.ACES);
            if (!profile.TryGet<ColorAdjustments>(out var colors)) { colors = profile.Add<ColorAdjustments>(); AssetDatabase.AddObjectToAsset(colors, profile); }
            colors.postExposure.Override(0f); colors.contrast.Override(7f); colors.saturation.Override(12f);
            if (!profile.TryGet<Bloom>(out var bloom)) { bloom = profile.Add<Bloom>(); AssetDatabase.AddObjectToAsset(bloom, profile); }
            bloom.intensity.Override(.12f); bloom.threshold.Override(.95f); bloom.highQualityFiltering.Override(true);
            EditorUtility.SetDirty(profile); volume.sharedProfile = profile;
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = TitleCatShowcase.StageLayer;
            AssetDatabase.SaveAssets();
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static string CapturePoster()
    {
        var root = new GameObject("Title poster capture", typeof(RectTransform), typeof(RawImage));
        try
        {
            var showcase = root.AddComponent<TitleCatShowcase>();
            showcase.EditorConfigure(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), null, CatBreedCatalog.Load());
            var image = showcase.EditorCapture();
            File.WriteAllBytes(PosterPath, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(PosterPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PosterPath);
            importer.mipmapEnabled = false; importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp; importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return PosterPath;
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static Material Material(string name, Color color, float smoothness, float metallic = 0f)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Metallic", metallic);
        EditorUtility.SetDirty(material); return material;
    }
    private static void Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var shape = GameObject.CreatePrimitive(type); shape.name = name;
        Object.DestroyImmediate(shape.GetComponent<Collider>());
        shape.transform.SetParent(parent, false); shape.transform.localPosition = position; shape.transform.localScale = scale;
        shape.GetComponent<Renderer>().sharedMaterial = material;
    }
    private static void Arch(Transform parent, string name, float radius, float upright, Vector3 position, Material material)
    {
        var vertices = new List<Vector3> { new Vector3(-radius, 0, 0), new Vector3(radius, 0, 0) };
        for (int i = 0; i <= 64; i++) { float a = i / 64f * Mathf.PI; vertices.Add(new Vector3(Mathf.Cos(a) * radius, upright + Mathf.Sin(a) * radius, 0)); }
        var indices = new List<int>();
        for (int i = 1; i < vertices.Count - 1; i++) { indices.Add(0); indices.Add(i + 1); indices.Add(i); }
        string path = Folder + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
        mesh.Clear(); mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
        var shape = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); shape.transform.SetParent(parent, false); shape.transform.localPosition = position;
        shape.GetComponent<MeshFilter>().sharedMesh = mesh; shape.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
    private static void Furniture(Transform parent, string product, Vector3 position, float yaw, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/" + product + ".prefab");
        if (prefab == null) throw new System.InvalidOperationException("Missing real product " + product);
        var root = new GameObject(product + " (visual)").transform;
        root.SetParent(parent, false); root.localPosition = position; root.localRotation = Quaternion.Euler(0, yaw, 0); root.localScale = Vector3.one * scale;
        // Copy render data only: no purchase, collider, quest or activity components can run on the title.
        CopyVisual(prefab.transform, root);
    }
    private static void CopyVisual(Transform source, Transform target)
    {
        var filter = source.GetComponent<MeshFilter>(); var renderer = source.GetComponent<MeshRenderer>();
        if (filter != null && renderer != null) { target.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh; target.gameObject.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials; }
        foreach (Transform child in source)
        {
            var copy = new GameObject(child.name).transform; copy.SetParent(target, false);
            copy.localPosition = child.localPosition; copy.localRotation = child.localRotation; copy.localScale = child.localScale;
            CopyVisual(child, copy);
        }
    }
    private static void Light(Transform parent, string name, Vector3 angles, Color color, float intensity, bool shadows)
    {
        var light = new GameObject(name).AddComponent<Light>(); light.transform.SetParent(parent, false); light.transform.localEulerAngles = angles;
        light.type = LightType.Directional; light.color = color; light.intensity = intensity; light.cullingMask = 1 << TitleCatShowcase.StageLayer;
        light.shadows = shadows ? LightShadows.Soft : LightShadows.None; light.shadowStrength = .65f; light.shadowBias = .025f; light.shadowNormalBias = .15f;
        light.enabled = false;
    }
}
