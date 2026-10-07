using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Renders consistent premium product-card thumbnails from real 3D assets.</summary>
public static class StoreCatalogPreviewBuilder
{
    private const int PreviewSize = 1024;
    private const string GeneratedPrefabFolder = "Assets/Art/StoreProducts/Prefabs/";

    [MenuItem("Tools/Cat Home/Store/Rebuild Catalog Previews")]
    public static void BuildAll()
    {
        BuildPreviews(forceAll: true);
    }

    public static string BuildMissingAndBedroomPreviews()
    {
        return BuildPreviews(forceAll: false);
    }

    public static string BuildRoomPreviews(string roomId)
    {
        EnsureIconFolder(); int count = 0;
        foreach (var definition in StoreCatalogAssets.PlaceableProducts)
        {
            if (!definition.GeneratePreview || !HomeStoreService.IsProductInRoomCollection(roomId, definition.ProductId)) continue;
            Render(definition); if (count == 0) Render(definition); count++;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        return count + " room product previews updated.";
    }

    private static string BuildPreviews(bool forceAll)
    {
        EnsureIconFolder();
        int rendered = 0;
        foreach(var definition in CatProductContentBuilder.LegacyDefinitions)
            if(forceAll||AssetDatabase.LoadAssetAtPath<Texture2D>(definition.IconPath)==null)
            {Render(definition);if(rendered==0)Render(definition);rendered++;}
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
        // Always photograph the BUILT PRODUCT PREFAB when there is one, never the
        // raw source. Twenty-eight products (`Custom`, `Pet`, `Room` entries)
        // point `SourceAssetPath` at a bare FBX or a third-party pack prefab,
        // and `StoreProductContentBuilder` applies scale, offset and orientation
        // on top of that when it bakes the real product. Shooting the source
        // therefore photographed something the player never receives:
        // `RoundWallClock.fbx` is authored face-up, so its card was a blank gold
        // disc, and `SideTable.fbx` came out as a bare top. The prefab is what
        // the player gets, so the prefab is what the card must show.
        string builtPrefabPath = GeneratedPrefabFolder + definition.PrefabName + ".prefab";
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(builtPrefabPath);
        bool fromBuiltPrefab = asset != null;
        if (asset == null)
            asset = AssetDatabase.LoadAssetAtPath<GameObject>(definition.SourceAssetPath);
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
            // The built prefab already carries the catalog scale and offset the
            // builder baked in; applying them again would double them, and the
            // pack products scale up to 7x.
            if (!fromBuiltPrefab)
            {
                instance.transform.localScale = Vector3.one * definition.VisualScale;
                instance.transform.localPosition = definition.VisualOffset;
            }
            bool generated = fromBuiltPrefab || definition.SourceAssetPath.StartsWith(
                GeneratedPrefabFolder, StringComparison.Ordinal);
            // The preview camera always stands on the prefab's -Z side, so a
            // product whose decorated face ends up at prefab +Z shows the card
            // its blank back. This is NOT the same set as `facesBackward` in
            // StoreProductContentBuilder: a product lands in that list either
            // because its mesh front was authored at +Z (BathroomToilet and the
            // other Bathroom fixtures, which photograph correctly at 28) or
            // because it stands at a yaw that puts the room behind it (the ten
            // below). Only the second kind needs the flip, so the list is
            // explicit and every entry was checked on the rendered card.
            bool reverseWallPreview =
                definition.ProductId == HomeStoreService.LoftTallBookcaseId ||
                definition.ProductId == HomeStoreService.LoftWallGalleryId ||
                definition.ProductId == HomeStoreService.BedroomDreamArtId ||
                definition.ProductId == HomeStoreService.BedroomWardrobeId ||
                definition.ProductId == HomeStoreService.BedroomWindowDaybedId ||
                definition.ProductId == HomeStoreService.BedroomNightstandId ||
                definition.ProductId == HomeStoreService.BathroomVanityId ||
                definition.ProductId == HomeStoreService.BathroomShowerId ||
                definition.ProductId == HomeStoreService.BookshelfId ||
                definition.ProductId == HomeStoreService.ModernPaintingId ||
                definition.ProductId == HomeStoreService.ModernTelevisionId ||
                definition.ProductId == HomeStoreService.TvUnitId ||
                definition.ProductId == HomeStoreService.GameConsoleId ||
                definition.ProductId == HomeStoreService.StereoId ||
                definition.ProductId == HomeStoreService.MirrorId ||
                definition.ProductId == HomeStoreService.RetroTvId ||
                definition.ProductId == HomeStoreService.WallClockId;
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
            camera.backgroundColor = Color.clear;
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
            // These two deep silhouettes extend past the projected vertical bounds at this
            // oblique angle. Keep the original view, with enough breathing room for the whole prop.
            if (definition.PrefabName == "BathroomShower" || definition.PrefabName == "RetroTelevision")
                camera.orthographicSize *= 1.18f;

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

            preview.BeginPreview(new Rect(0f, 0f, PreviewSize, PreviewSize), GUIStyle.none);
            preview.Render(true);
            var rendered = preview.EndPreview() as RenderTexture;
            if (rendered == null)
                throw new InvalidOperationException("Unity returned no preview for " + definition.ProductId);
            var previous = RenderTexture.active;
            var resolved = RenderTexture.GetTemporary(PreviewSize, PreviewSize, 0, RenderTextureFormat.ARGB32);
            try
            {
                Graphics.Blit(rendered, resolved);
                RenderTexture.active = resolved;
                texture = new Texture2D(PreviewSize, PreviewSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, PreviewSize, PreviewSize), 0, 0);
                texture.Apply();
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(resolved); }

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
