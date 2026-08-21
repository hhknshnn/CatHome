using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Renders consistent premium product-card thumbnails from real 3D assets.</summary>
public static class StoreCatalogPreviewBuilder
{
    private const int PreviewSize = 512;

    [MenuItem("Tools/Cat Home/Store/Rebuild Catalog Previews")]
    public static void BuildAll()
    {
        BuildPreviews(forceAll: true);
    }

    public static string BuildMissingAndBedroomPreviews()
    {
        return BuildPreviews(forceAll: false);
    }

    private static string BuildPreviews(bool forceAll)
    {
        EnsureIconFolder();
        int rendered = 0;
        for (int i = 0; i < StoreCatalogAssets.PlaceableProducts.Length; i++)
        {
            StoreCatalogAsset definition = StoreCatalogAssets.PlaceableProducts[i];
            if (!definition.GeneratePreview)
                continue;

            bool bedroom = definition.ProductId.StartsWith("bedroom.", StringComparison.Ordinal);
            bool missing = AssetDatabase.LoadAssetAtPath<Texture2D>(definition.IconPath) == null;
            if (!forceAll && !bedroom && !missing)
                continue;

            Render(definition);
            rendered++;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return rendered + " catalog previews written to " + StoreCatalogAssets.IconFolder + ".";
    }

    private static void Render(StoreCatalogAsset definition)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(definition.SourceAssetPath);
        if (asset == null)
            throw new InvalidOperationException("Catalog preview source is missing: " + definition.SourceAssetPath);

        var preview = new PreviewRenderUtility();
        Texture2D texture = null;
        try
        {
            GameObject instance = preview.InstantiatePrefabInScene(asset);
            instance.SetActive(true);
            Transform[] previewTransforms = instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < previewTransforms.Length; i++)
                previewTransforms[i].gameObject.SetActive(true);
            instance.transform.localScale = Vector3.one * definition.VisualScale;
            instance.transform.localPosition = definition.VisualOffset;
            bool generated = definition.SourceAssetPath.StartsWith(
                "Assets/Art/StoreProducts/Prefabs/", StringComparison.Ordinal);
            bool reverseWallPreview =
                definition.ProductId == HomeStoreService.LoftTallBookcaseId;
            instance.transform.localRotation = generated
                ? Quaternion.Euler(0f, reverseWallPreview ? 208f : 28f, 0f)
                : Quaternion.Euler(0f, 156f, 0f);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Catalog preview has no renderer: " + definition.SourceAssetPath);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            Camera camera = preview.camera;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(21, 55, 68, 255);
            camera.orthographic = true;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 100f;
            bool flat = bounds.size.y < .28f &&
                        bounds.size.x > bounds.size.y * 2.5f &&
                        bounds.size.z > bounds.size.y * 2.5f;
            if (flat)
            {
                camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.18f;
                camera.transform.position = bounds.center + new Vector3(.12f, 3.4f, -0.42f);
            }
            else
            {
                camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x, bounds.extents.z) * 1.28f;
                camera.transform.position = bounds.center + new Vector3(1.8f, 1.45f, -2.4f) *
                    Mathf.Max(.45f, bounds.size.magnitude * .42f);
            }
            camera.transform.LookAt(bounds.center);

            preview.ambientColor = new Color(.78f, .8f, .84f, 1f);
            if (preview.lights.Length > 0)
            {
                preview.lights[0].intensity = 1.25f;
                preview.lights[0].transform.rotation = Quaternion.Euler(35f, 35f, 0f);
            }
            if (preview.lights.Length > 1)
            {
                preview.lights[1].intensity = .75f;
                preview.lights[1].transform.rotation = Quaternion.Euler(320f, 210f, 0f);
            }

            preview.BeginStaticPreview(new Rect(0f, 0f, PreviewSize, PreviewSize));
            preview.Render(true);
            texture = preview.EndStaticPreview();
            if (texture == null)
                throw new InvalidOperationException("Unity returned no preview for " + definition.ProductId);

            File.WriteAllBytes(definition.IconPath, texture.EncodeToPNG());
        }
        finally
        {
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            preview.Cleanup();
        }
    }

    private static void EnsureIconFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/StoreProducts"))
            AssetDatabase.CreateFolder("Assets/Art", "StoreProducts");
        if (!AssetDatabase.IsValidFolder(StoreCatalogAssets.IconFolder))
            AssetDatabase.CreateFolder("Assets/Art/StoreProducts", "Icons");
    }
}
