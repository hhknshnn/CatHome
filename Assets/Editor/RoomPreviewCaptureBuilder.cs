using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes HUD-free camera stills of each canonical room into
/// <c>Assets/Art/RoomPreviews</c> so the room selector and HOME shop share
/// the real world visuals instead of stock or UI screenshots.
/// </summary>
public static class RoomPreviewCaptureBuilder
{
    public const string PreviewFolder = "Assets/Art/RoomPreviews";
    public const int PreviewWidth = 1280;
    public const int PreviewHeight = 720;

    private static readonly string LivingPreviewPath =
        PreviewFolder + "/LivingRoomPreview.png";
    private static readonly string BathroomPreviewPath =
        PreviewFolder + "/BathroomPreview.png";
    private static readonly string KitchenPreviewPath =
        PreviewFolder + "/KitchenPreview.png";
    private static readonly string BedroomPreviewPath =
        PreviewFolder + "/BedroomPreview.png";
    private static readonly string GardenPreviewPath =
        PreviewFolder + "/GardenPreview.png";

    [MenuItem("Tools/Cat Home/Rooms/Capture Room Previews")]
    public static void CaptureFromMenu()
    {
        string result = CaptureSilently();
        EditorUtility.DisplayDialog("Cat Home Room Previews", result, "OK");
    }

    public static string CaptureSilently()
    {
        EnsureFolder(PreviewFolder);
        Scene previousActive = SceneManager.GetActiveScene();
        var previouslyLoaded = new List<string>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loaded = SceneManager.GetSceneAt(i);
            if (loaded.IsValid() && loaded.isLoaded && !string.IsNullOrEmpty(loaded.path))
                previouslyLoaded.Add(loaded.path);
        }

        CaptureRoom(HomeRoomService.LivingRoomScenePath, LivingPreviewPath);
        CaptureRoom(HomeRoomService.BathroomScenePath, BathroomPreviewPath);
        CaptureRoom(HomeRoomService.KitchenScenePath, KitchenPreviewPath);
        CaptureRoom(HomeRoomService.BedroomScenePath, BedroomPreviewPath);
        CaptureRoom(HomeRoomService.GardenScenePath, GardenPreviewPath);

        CopyShopIcon(LivingPreviewPath, StoreCatalogAssets.IconFolder + "/LivingRoomPreview.png");
        CopyShopIcon(BathroomPreviewPath, StoreCatalogAssets.IconFolder + "/BathroomRoomPreview.png");
        CopyShopIcon(KitchenPreviewPath, StoreCatalogAssets.IconFolder + "/KitchenRoomPreview.png");
        CopyShopIcon(BedroomPreviewPath, StoreCatalogAssets.IconFolder + "/BedroomRoomPreview.png");
        CopyShopIcon(GardenPreviewPath, StoreCatalogAssets.IconFolder + "/GardenRoomPreview.png");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(LivingPreviewPath);
        ConfigureImporter(BathroomPreviewPath);
        ConfigureImporter(KitchenPreviewPath);
        ConfigureImporter(BedroomPreviewPath);
        ConfigureImporter(GardenPreviewPath);
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/LivingRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/BathroomRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/KitchenRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/BedroomRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/GardenRoomPreview.png");

        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene loaded = SceneManager.GetSceneAt(i);
            if (!loaded.IsValid() || !loaded.isLoaded || string.IsNullOrEmpty(loaded.path))
                continue;
            if (previouslyLoaded.Contains(loaded.path))
                continue;
            EditorSceneManager.CloseScene(loaded, true);
        }

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return "Room selector and HOME shop now use camera stills of the real rooms.";
    }

    private static void CaptureRoom(string scenePath, string outputPath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            throw new InvalidOperationException("Room scene is missing: " + scenePath);

        Scene existing = SceneManager.GetSceneByPath(scenePath);
        bool opened = !existing.IsValid() || !existing.isLoaded;
        Scene scene = opened
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive)
            : existing;
        EditorSceneManager.SetActiveScene(scene);

        var revealed = new List<GameObject>();
        var hiddenForeign = HideForeignRoomScenes(scene);
        try
        {
            RevealOwnedLooks(scene, revealed);
            Camera camera = FindCamera(scene);
            if (camera == null)
                throw new InvalidOperationException("No camera in " + scenePath);

            RenderTexture previousTarget = camera.targetTexture;
            bool previousEnabled = camera.enabled;
            camera.enabled = true;
            var render = new RenderTexture(PreviewWidth, PreviewHeight, 24)
            {
                antiAliasing = 2
            };
            Texture2D texture = null;
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = render;
                texture = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0f, 0f, PreviewWidth, PreviewHeight), 0, 0);
                texture.Apply(false, false);
                RenderTexture.active = previousActive;
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.enabled = previousEnabled;
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
            }
        }
        finally
        {
            for (int i = 0; i < revealed.Count; i++)
            {
                if (revealed[i] != null)
                    revealed[i].SetActive(false);
            }

            RestoreForeignRoomScenes(hiddenForeign);

            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static List<GameObject> HideForeignRoomScenes(Scene keep)
    {
        var hidden = new List<GameObject>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded || scene == keep)
                continue;
            if (!IsRoomScene(scene.path))
                continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null || !root.activeSelf)
                    continue;
                root.SetActive(false);
                hidden.Add(root);
            }
        }

        return hidden;
    }

    private static void RestoreForeignRoomScenes(List<GameObject> hidden)
    {
        for (int i = 0; i < hidden.Count; i++)
        {
            if (hidden[i] != null)
                hidden[i].SetActive(true);
        }
    }

    private static bool IsRoomScene(string path)
    {
        return string.Equals(path, HomeRoomService.LivingRoomScenePath, StringComparison.Ordinal) ||
               string.Equals(path, HomeRoomService.BathroomScenePath, StringComparison.Ordinal) ||
               string.Equals(path, HomeRoomService.KitchenScenePath, StringComparison.Ordinal) ||
               string.Equals(path, HomeRoomService.BedroomScenePath, StringComparison.Ordinal) ||
               string.Equals(path, HomeRoomService.GardenScenePath, StringComparison.Ordinal);
    }

    private static void RevealOwnedLooks(Scene scene, List<GameObject> revealed)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            StoreProductDisplay[] displays =
                root.GetComponentsInChildren<StoreProductDisplay>(true);
            for (int i = 0; i < displays.Length; i++)
            {
                var serialized = new SerializedObject(displays[i]);
                var visual = serialized.FindProperty("visualRoot").objectReferenceValue
                    as GameObject;
                if (visual == null || visual.activeSelf)
                    continue;
                visual.SetActive(true);
                revealed.Add(visual);
            }
        }
    }

    private static Camera FindCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].enabled)
                    return cameras[i];
            }

            if (cameras.Length > 0)
                return cameras[0];
        }

        return null;
    }

    private static void CopyShopIcon(string sourcePath, string destinationPath)
    {
        string sourceFull = Path.GetFullPath(sourcePath);
        string destinationFull = Path.GetFullPath(destinationPath);
        string folder = Path.GetDirectoryName(destinationFull);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);
        File.Copy(sourceFull, destinationFull, true);
    }

    private static void ConfigureImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            changed = true;
        }

        if (!importer.sRGBTexture)
        {
            importer.sRGBTexture = true;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }

        if (importer.maxTextureSize < 1024)
        {
            importer.maxTextureSize = 1024;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
