using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneArchitectureBuilder
{
    public const string BootstrapScenePath = "Assets/Scenes/GameScene.unity";
    public const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    public const string LevelScenePath = "Assets/Scenes/Levels/LivingRoom_Level01.unity";
    public const string LegacyScenePath =
        "Assets/Scenes/Legacy/GameScene_PreLevelArchitecture.unity";

    private static readonly HashSet<string> UiRootNames =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Canvas",
            "EventSystem",
            "MainPanelCanvas",
            "QuestPanelCanvas",
            "CurrencyHudCanvas",
            "ShopPanelCanvas",
            RoomSelectorPanelBuilder.RootName
        };

    [MenuItem("Tools/Cat Home/Architecture/Build Bootstrap + UI + Level Scenes")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Cat Home Scene Architecture",
                "Split GameScene into Bootstrap, shared UI and Living Room Level 01? " +
                "A legacy scene copy is kept before the split.",
                "Build",
                "Cancel"))
        {
            return;
        }

        BuildSilently();
    }

    public static string BuildSilently()
    {
        EnsureFolders();

        Scene bootstrap = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
        if (bootstrap.GetRootGameObjects().Length == 1 &&
            bootstrap.GetRootGameObjects()[0].GetComponent<LevelLoader>() != null &&
            File.Exists(UiScenePath) && File.Exists(LevelScenePath))
        {
            ConfigureBuildSettings();
            ProgressionConfigBuilder.CreateOrUpdateSilently();
            return "already-built";
        }

        if (!File.Exists(LegacyScenePath))
        {
            if (!EditorSceneManager.SaveScene(bootstrap, LegacyScenePath, true))
                throw new InvalidOperationException("Could not create the legacy GameScene copy.");
        }

        Scene uiScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        MoveRoots(bootstrap, uiScene, true);
        if (!EditorSceneManager.SaveScene(uiScene, UiScenePath))
            throw new InvalidOperationException("Could not save the shared UI scene.");

        Scene levelScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        MoveRoots(bootstrap, levelScene, false);
        EnsureLevelMarkers(levelScene);
        if (!EditorSceneManager.SaveScene(levelScene, LevelScenePath))
            throw new InvalidOperationException("Could not save Living Room Level 01.");

        var bootstrapObject = new GameObject("GameBootstrap");
        SceneManager.MoveGameObjectToScene(bootstrapObject, bootstrap);
        bootstrapObject.AddComponent<LevelLoader>();

        EditorSceneManager.SetActiveScene(bootstrap);
        if (!EditorSceneManager.SaveScene(bootstrap, BootstrapScenePath))
            throw new InvalidOperationException("Could not save the bootstrap scene.");

        EditorSceneManager.CloseScene(uiScene, true);
        EditorSceneManager.CloseScene(levelScene, true);

        ConfigureBuildSettings();
        ProgressionConfigBuilder.CreateOrUpdateSilently();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "built";
    }

    private static void MoveRoots(Scene source, Scene destination, bool moveUiRoots)
    {
        GameObject[] roots = source.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (UiRootNames.Contains(root.name) == moveUiRoots)
                SceneManager.MoveGameObjectToScene(root, destination);
        }
    }

    private static void EnsureLevelMarkers(Scene levelScene)
    {
        GameObject architectureRoot = new GameObject("LevelArchitecture");
        SceneManager.MoveGameObjectToScene(architectureRoot, levelScene);
        LevelSceneMarker marker = architectureRoot.AddComponent<LevelSceneMarker>();
        marker.EditorSetId("living-room-01");

        CatMovement cat = FindInScene<CatMovement>(levelScene);
        var spawnObject = new GameObject("SpawnPoint_default");
        SceneManager.MoveGameObjectToScene(spawnObject, levelScene);
        if (cat != null)
            spawnObject.transform.SetPositionAndRotation(cat.transform.position, cat.transform.rotation);

        LevelSpawnPoint spawn = spawnObject.AddComponent<LevelSpawnPoint>();
        spawn.EditorSetId("default");

        CatHomeAuthoringWorkspace.OrganizeLivingRoomHierarchy(levelScene, false);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }

    private static void ConfigureBuildSettings()
    {
        string[] required =
        {
            BootstrapScenePath,
            UiScenePath,
            LevelScenePath,
            HomeRoomService.BathroomScenePath,
            HomeRoomService.KitchenScenePath,
            HomeRoomService.BedroomScenePath
        };

        var result = new List<EditorBuildSettingsScene>();
        var claimed = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < required.Length; i++)
        {
            // Optional room scenes are authored by their dedicated builders. Do
            // not add dangling Build Settings entries before those assets exist.
            if (required[i] != LevelScenePath &&
                HomeRoomService.IsKnownScenePath(required[i]) &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(required[i]) == null)
            {
                continue;
            }
            result.Add(new EditorBuildSettingsScene(required[i], true));
            claimed.Add(required[i]);
        }

        // Preserve Runner and any future auxiliary scenes. Architecture rebuilds
        // own the three core scenes, not the rest of the project's build list.
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        for (int i = 0; i < existing.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(existing[i].path) ||
                !claimed.Add(existing[i].path))
            {
                continue;
            }
            result.Add(existing[i]);
        }
        EditorBuildSettings.scenes = result.ToArray();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Scenes/UI");
        EnsureFolder("Assets/Scenes/Levels");
        EnsureFolder("Assets/Scenes/Legacy");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
