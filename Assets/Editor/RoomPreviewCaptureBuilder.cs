using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes HUD-free camera stills of each canonical room into
/// <c>Assets/Art/RoomPreviews</c> so the room selector and HOME shop share
/// the real world visuals instead of stock or UI screenshots.
/// </summary>
public static class RoomPreviewCaptureBuilder
{
    public const string PreviewFolder = "Assets/Art/RoomPreviews";
    public const int PreviewWidth = 1920;
    public const int PreviewHeight = 1080;

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
    private static readonly string BalconyPreviewPath =
        PreviewFolder + "/BalconyPreview.png";
    private static readonly string PatioPreviewPath =
        PreviewFolder + "/PatioPreview.png";
    private static readonly string SecondFloorPreviewPath =
        PreviewFolder + "/SecondFloorPreview.png";

    [MenuItem("Tools/Cat Home/Rooms/Capture Room Previews")]
    public static void CaptureFromMenu()
    {
        string result = CaptureSilently();
        EditorUtility.DisplayDialog("Cat Home Room Previews", result, "OK");
    }

    [MenuItem("Tools/Cat Home/Rooms/Capture Second Floor Preview (Silent)")]
    public static void CaptureSecondFloorFromMenu()
    {
        Debug.Log(CaptureSecondFloorSilently());
    }

    public static string CaptureSecondFloorSilently()
    {
        EnsureFolder(PreviewFolder);
        Scene previousActive = SceneManager.GetActiveScene();

        CaptureRoom(HomeRoomService.SecondFloorScenePath, SecondFloorPreviewPath);
        CopyShopIcon(SecondFloorPreviewPath,
            StoreCatalogAssets.IconFolder + "/SecondFloorRoomPreview.png");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(SecondFloorPreviewPath);
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/SecondFloorRoomPreview.png");

        if (previousActive.IsValid() && previousActive.isLoaded)
            EditorSceneManager.SetActiveScene(previousActive);

        return "Second Floor room selector and HOME shop previews were captured.";
    }

    public static string CaptureBathroomSilently()
    {
        EnsureFolder(PreviewFolder);
        var active = SceneManager.GetActiveScene();
        try
        {
            CaptureRoom(HomeRoomService.BathroomScenePath, BathroomPreviewPath);
            CopyShopIcon(BathroomPreviewPath, StoreCatalogAssets.IconFolder + "/BathroomRoomPreview.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(BathroomPreviewPath);
            ConfigureImporter(StoreCatalogAssets.IconFolder + "/BathroomRoomPreview.png");
            return "Bathroom selector and store photography updated.";
        }
        finally { if (active.IsValid() && active.isLoaded) EditorSceneManager.SetActiveScene(active); }
    }

    public static string CaptureKitchenSilently()
    {
        EnsureFolder(PreviewFolder);
        var active = SceneManager.GetActiveScene();
        try
        {
            CaptureRoom(HomeRoomService.KitchenScenePath, KitchenPreviewPath);
            CopyShopIcon(KitchenPreviewPath, StoreCatalogAssets.IconFolder + "/KitchenRoomPreview.png");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(KitchenPreviewPath);
            ConfigureImporter(StoreCatalogAssets.IconFolder + "/KitchenRoomPreview.png");
            return "Kitchen selector and store photography updated.";
        }
        finally { if (active.IsValid() && active.isLoaded) EditorSceneManager.SetActiveScene(active); }
    }

    public static string CaptureRemainingRoomSilently(string roomId)
    {
        if (!RemainingRoomsThemeBuilder.Handles(roomId)) throw new ArgumentException("Reviewed room required", nameof(roomId));
        HomeRoomService.TryGetRoom(roomId, out var room);
        string stem = roomId == HomeRoomService.SecondFloorId ? "SecondFloor" :
            Path.GetFileNameWithoutExtension(room.ScenePath).Replace("_Level01", "");
        string photo = PreviewFolder + "/" + stem + "Preview.png";
        string icon = StoreCatalogAssets.IconFolder + "/" + stem + "RoomPreview.png";
        var active = SceneManager.GetActiveScene();
        try
        {
            CaptureRoom(room.ScenePath, photo); CopyShopIcon(photo, icon);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(photo); ConfigureImporter(icon);
            return roomId + " room and store photos updated.";
        }
        finally { if (active.IsValid() && active.isLoaded) EditorSceneManager.SetActiveScene(active); }
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
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.BalconyScenePath) != null)
            CaptureRoom(HomeRoomService.BalconyScenePath, BalconyPreviewPath);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.PatioScenePath) != null)
            CaptureRoom(HomeRoomService.PatioScenePath, PatioPreviewPath);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeRoomService.SecondFloorScenePath) != null)
            CaptureRoom(HomeRoomService.SecondFloorScenePath, SecondFloorPreviewPath);

        CopyShopIcon(LivingPreviewPath, StoreCatalogAssets.IconFolder + "/LivingRoomPreview.png");
        CopyShopIcon(BathroomPreviewPath, StoreCatalogAssets.IconFolder + "/BathroomRoomPreview.png");
        CopyShopIcon(KitchenPreviewPath, StoreCatalogAssets.IconFolder + "/KitchenRoomPreview.png");
        CopyShopIcon(BedroomPreviewPath, StoreCatalogAssets.IconFolder + "/BedroomRoomPreview.png");
        CopyShopIcon(GardenPreviewPath, StoreCatalogAssets.IconFolder + "/GardenRoomPreview.png");
        if (File.Exists(Path.GetFullPath(BalconyPreviewPath)))
            CopyShopIcon(BalconyPreviewPath, StoreCatalogAssets.IconFolder + "/BalconyRoomPreview.png");
        if (File.Exists(Path.GetFullPath(PatioPreviewPath)))
            CopyShopIcon(PatioPreviewPath, StoreCatalogAssets.IconFolder + "/PatioRoomPreview.png");
        if (File.Exists(Path.GetFullPath(SecondFloorPreviewPath)))
            CopyShopIcon(SecondFloorPreviewPath, StoreCatalogAssets.IconFolder + "/SecondFloorRoomPreview.png");

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(LivingPreviewPath);
        ConfigureImporter(BathroomPreviewPath);
        ConfigureImporter(KitchenPreviewPath);
        ConfigureImporter(BedroomPreviewPath);
        ConfigureImporter(GardenPreviewPath);
        if (File.Exists(Path.GetFullPath(BalconyPreviewPath)))
            ConfigureImporter(BalconyPreviewPath);
        if (File.Exists(Path.GetFullPath(PatioPreviewPath)))
            ConfigureImporter(PatioPreviewPath);
        if (File.Exists(Path.GetFullPath(SecondFloorPreviewPath)))
            ConfigureImporter(SecondFloorPreviewPath);
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/LivingRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/BathroomRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/KitchenRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/BedroomRoomPreview.png");
        ConfigureImporter(StoreCatalogAssets.IconFolder + "/GardenRoomPreview.png");
        if (File.Exists(Path.GetFullPath(
                StoreCatalogAssets.IconFolder + "/BalconyRoomPreview.png")))
            ConfigureImporter(StoreCatalogAssets.IconFolder + "/BalconyRoomPreview.png");
        if (File.Exists(Path.GetFullPath(
                StoreCatalogAssets.IconFolder + "/PatioRoomPreview.png")))
            ConfigureImporter(StoreCatalogAssets.IconFolder + "/PatioRoomPreview.png");
        if (File.Exists(Path.GetFullPath(
                StoreCatalogAssets.IconFolder + "/SecondFloorRoomPreview.png")))
            ConfigureImporter(StoreCatalogAssets.IconFolder + "/SecondFloorRoomPreview.png");

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
        Action restoreCatPhoto = null;
        Action restoreCatFinish = null;
        var hiddenForeign = HideForeignRoomScenes(scene);
        AmbientMode previousAmbientMode = RenderSettings.ambientMode;
        float previousAmbientIntensity = RenderSettings.ambientIntensity;
        Color previousAmbientSky = RenderSettings.ambientSkyColor;
        Color previousAmbientEquator = RenderSettings.ambientEquatorColor;
        Color previousAmbientGround = RenderSettings.ambientGroundColor;
        var sceneLights = new List<Light>();
        foreach (GameObject root in scene.GetRootGameObjects())
            sceneLights.AddRange(root.GetComponentsInChildren<Light>(true));
        var previousLightIntensities = new float[sceneLights.Count];
        var previousLightColors = new Color[sceneLights.Count];
        GameObject previewKeyObject = null;
        GameObject previewFillObject = null;
        bool outdoorShowroom =
            string.Equals(scenePath, HomeRoomService.GardenScenePath, StringComparison.Ordinal) ||
            string.Equals(scenePath, HomeRoomService.BalconyScenePath, StringComparison.Ordinal) ||
            string.Equals(scenePath, HomeRoomService.PatioScenePath, StringComparison.Ordinal);
        for (int i = 0; i < sceneLights.Count; i++)
        {
            previousLightIntensities[i] = sceneLights[i].intensity;
            previousLightColors[i] = sceneLights[i].color;
        }
        try
        {
            restoreCatFinish = ApplyModernCatPhotoFinish(scene);
            RevealOwnedLooks(scene, revealed);
            restoreCatPhoto = ArrangeCatPhoto(scene);
            // Room cards are showroom photography, not a snapshot of the
            // developer machine's current evening hour.  Keep the runtime
            // day/night system intact while baking every card at the same warm
            // late-morning exposure so outdoor rooms remain inviting and the
            // catalogue never alternates between bright indoor and dark outdoor.
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientIntensity = 1.08f;
            RenderSettings.ambientSkyColor = new Color32(206, 232, 244, 255);
            RenderSettings.ambientEquatorColor = new Color32(164, 199, 214, 255);
            RenderSettings.ambientGroundColor = new Color32(112, 106, 130, 255);
            for (int i = 0; i < sceneLights.Count; i++)
            {
                Light light = sceneLights[i];
                if (light != null && light.type == LightType.Directional && light.name != "ReferenceSoftFill")
                {
                    light.intensity = outdoorShowroom ? 1.05f : 1.24f;
                    light.color = new Color32(255, 246, 216, 255);
                }
            }
            previewKeyObject = new GameObject("Room Preview Studio Key", typeof(Light));
            SceneManager.MoveGameObjectToScene(previewKeyObject, scene);
            previewKeyObject.transform.position = new Vector3(-1.6f, 4.8f, -2.6f);
            Light previewKey = previewKeyObject.GetComponent<Light>();
            previewKey.type = LightType.Point;
            previewKey.color = new Color32(255, 238, 201, 255);
            previewKey.intensity = outdoorShowroom ? .7f : 3.2f;
            previewKey.range = 11f;
            previewKey.shadows = LightShadows.None;

            previewFillObject = new GameObject("Room Preview Studio Fill", typeof(Light));
            SceneManager.MoveGameObjectToScene(previewFillObject, scene);
            previewFillObject.transform.position = new Vector3(3.2f, 3.2f, -0.5f);
            Light previewFill = previewFillObject.GetComponent<Light>();
            previewFill.type = LightType.Point;
            previewFill.color = new Color32(151, 226, 255, 255);
            previewFill.intensity = outdoorShowroom ? .35f : 1.45f;
            previewFill.range = 9f;
            previewFill.shadows = LightShadows.None;
            // Indoor rooms already carry the measured gameplay light rig.
            // Stacking the outdoor showroom treatment onto it bleaches the
            // cream upholstery and turns the aqua/lilac walls almost white.
            if (!outdoorShowroom)
            {
                previewKey.intensity = 0f;
                previewFill.intensity = 0f;
                RenderSettings.ambientMode = previousAmbientMode;
                RenderSettings.ambientIntensity = previousAmbientIntensity;
                RenderSettings.ambientSkyColor = previousAmbientSky;
                RenderSettings.ambientEquatorColor = previousAmbientEquator;
                RenderSettings.ambientGroundColor = previousAmbientGround;
                for (int i = 0; i < sceneLights.Count; i++)
                {
                    sceneLights[i].intensity = previousLightIntensities[i];
                    sceneLights[i].color = previousLightColors[i];
                }
            }
            Camera camera = FindCamera(scene);
            if (camera == null)
                throw new InvalidOperationException("No camera in " + scenePath);

            RenderTexture previousTarget = camera.targetTexture;
            Rect previousRect = camera.rect;
            float previousFov = camera.fieldOfView;
            bool previousEnabled = camera.enabled;
            camera.enabled = true;
            var render = new RenderTexture(PreviewWidth, PreviewHeight, 24)
            {
                antiAliasing = 4
            };
            Texture2D texture = null;
            try
            {
                camera.targetTexture = render;
                camera.rect = new Rect(0,0,1,1);
                camera.fieldOfView=HomeRoomCameraProfile.PreviewFieldOfView;
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
                camera.rect = previousRect;
                camera.fieldOfView = previousFov;
                camera.enabled = previousEnabled;
                if (texture != null)
                    UnityEngine.Object.DestroyImmediate(texture);
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
            }
        }
        finally
        {
            restoreCatFinish?.Invoke();
            restoreCatPhoto?.Invoke();
            for (int i = 0; i < revealed.Count; i++)
            {
                if (revealed[i] != null)
                    revealed[i].SetActive(false);
            }

            RestoreForeignRoomScenes(hiddenForeign);

            if (previewKeyObject != null)
                UnityEngine.Object.DestroyImmediate(previewKeyObject);
            if (previewFillObject != null)
                UnityEngine.Object.DestroyImmediate(previewFillObject);

            RenderSettings.ambientMode = previousAmbientMode;
            RenderSettings.ambientIntensity = previousAmbientIntensity;
            RenderSettings.ambientSkyColor = previousAmbientSky;
            RenderSettings.ambientEquatorColor = previousAmbientEquator;
            RenderSettings.ambientGroundColor = previousAmbientGround;
            for (int i = 0; i < sceneLights.Count; i++)
            {
                if (sceneLights[i] == null)
                    continue;
                sceneLights[i].intensity = previousLightIntensities[i];
                sceneLights[i].color = previousLightColors[i];
            }

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

    private static Action ApplyModernCatPhotoFinish(Scene scene)
    {
        var catalog = CatModernVisualCatalog.Load();
        if (catalog == null) return null;
        // Editor scene actors have not run the gameplay factory. Resolve the
        // same shared assets just for this photo, without adding runtime stamps
        // or persisting overrides into the authored room scene.
        var states = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            .Select(skin => Tuple.Create(skin, skin.sharedMesh, skin.sharedMaterials))
            .ToArray();
        Action restore = () =>
        {
            foreach (var state in states)
            {
                if (state.Item1 == null) continue;
                state.Item1.sharedMesh = state.Item2;
                state.Item1.sharedMaterials = state.Item3;
            }
        };
        try
        {
            foreach (var state in states)
            {
                var modernMesh = catalog.Resolve(state.Item2);
                if (modernMesh != state.Item2) state.Item1.sharedMesh = modernMesh;
                var modernMaterials = state.Item3.Select(material => catalog.Resolve(material)).ToArray();
                if (!modernMaterials.SequenceEqual(state.Item3)) state.Item1.sharedMaterials = modernMaterials;
            }
            return restore;
        }
        catch
        {
            restore();
            throw;
        }
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
               string.Equals(path, HomeRoomService.GardenScenePath, StringComparison.Ordinal) ||
               string.Equals(path, HomeRoomService.BalconyScenePath, StringComparison.Ordinal) ||
               string.Equals(path, HomeRoomService.PatioScenePath, StringComparison.Ordinal) ||
               string.Equals(path, HomeRoomService.SecondFloorScenePath, StringComparison.Ordinal);
    }

    private static void RevealOwnedLooks(Scene scene, List<GameObject> revealed)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            StoreProductDisplay[] displays =
                root.GetComponentsInChildren<StoreProductDisplay>(true);
            for (int i = 0; i < displays.Length; i++)
            {
                if(CatCollectionPolicy.IsCatItem(displays[i].ProductId))continue;
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

    private static Action ArrangeCatPhoto(Scene scene)
    {
        if(scene.path!=HomeRoomService.LivingRoomScenePath)return null;
        var products=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HomeProductPlacement>(true))
            .Where(p=>CatCollectionPolicy.IsCatItem(p.ProductId)).ToArray();
        var states=new Dictionary<GameObject,bool>();var poses=new Dictionary<Transform,Tuple<Vector3,Quaternion>>();
        foreach(var p in products)
        {
            poses[p.MovableRoot]=Tuple.Create(p.MovableRoot.position,p.MovableRoot.rotation);
            foreach(var t in p.MovableRoot.GetComponentsInChildren<Transform>(true))states[t.gameObject]=t.gameObject.activeSelf;
        }
        Action restore=()=>{
            foreach(var pair in poses)if(pair.Key!=null)pair.Key.SetPositionAndRotation(pair.Value.Item1,pair.Value.Item2);
            foreach(var pair in states)if(pair.Key!=null)pair.Key.SetActive(pair.Value);
            var layout=CatRoomArrangement.Request(scene);if(layout!=null)layout.Invalidate();
        };
        try
        {
            foreach(var p in products)p.MovableRoot.gameObject.SetActive(false);
            string[] ids={HomeStoreService.ScratchPostId,HomeStoreService.PlayTunnelId,HomeStoreService.BallBasketId,HomeStoreService.ToyMouseId,HomeStoreService.NapPillowId};
            var layout=CatRoomArrangement.Request(scene);layout.Invalidate();
            if(!layout.TryPlan(ids,out var plan))throw new InvalidOperationException("No valid five-item room photo layout.");
            foreach(var p in products)if(plan.TryGetValue(p.ProductId,out var pose))
            {
                p.MovableRoot.SetPositionAndRotation(pose.position,Quaternion.Euler(0,pose.yaw,0));p.MovableRoot.gameObject.SetActive(true);
                var activity=p.GetComponent<CatActivity>();
                var visual=new SerializedObject(activity).FindProperty("unlockedContent").objectReferenceValue as GameObject;
                if(visual!=null)visual.SetActive(true);
            }
            return restore;
        }
        catch{restore();throw;}
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

        if (importer.maxTextureSize < 2048)
        {
            importer.maxTextureSize = 2048;
            changed = true;
        }
        if (importer.npotScale != TextureImporterNPOTScale.None)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
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
