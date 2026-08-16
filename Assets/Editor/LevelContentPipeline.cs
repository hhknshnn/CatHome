using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelContentPipeline
{
    [MenuItem("Tools/Cat Home/Architecture/Create Level Scene From Living Room Template")]
    public static void CreateFromMenu()
    {
        string destination = EditorUtility.SaveFilePanelInProject(
            "Create Cat Home Level Scene",
            "NewLevel",
            "unity",
            "Choose a scene path under Assets/Scenes/Levels.",
            "Assets/Scenes/Levels");

        if (string.IsNullOrWhiteSpace(destination))
            return;

        string sceneId = System.IO.Path.GetFileNameWithoutExtension(destination)
            .ToLowerInvariant()
            .Replace('_', '-');
        CreateFromLivingRoomTemplate(sceneId, destination);
    }

    public static bool CreateFromLivingRoomTemplate(string sceneId, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(sceneId))
            throw new ArgumentException("A stable scene id is required.", nameof(sceneId));
        if (string.IsNullOrWhiteSpace(destinationPath) ||
            !destinationPath.StartsWith("Assets/Scenes/Levels/", StringComparison.Ordinal) ||
            !destinationPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Level scenes must be created under Assets/Scenes/Levels as a .unity file.",
                nameof(destinationPath));
        }
        if (AssetDatabase.LoadMainAssetAtPath(destinationPath) != null)
            throw new InvalidOperationException($"An asset already exists at '{destinationPath}'.");

        if (!AssetDatabase.CopyAsset(SceneArchitectureBuilder.LevelScenePath, destinationPath))
            return false;

        Scene scene = EditorSceneManager.OpenScene(destinationPath, OpenSceneMode.Additive);
        try
        {
            LevelSceneMarker marker = FindInScene<LevelSceneMarker>(scene);
            if (marker == null)
            {
                var markerObject = new GameObject("LevelArchitecture");
                SceneManager.MoveGameObjectToScene(markerObject, scene);
                GameObject setupGroup = CatHomeAuthoringWorkspace.FindNamedInScene(
                    scene,
                    CatHomeAuthoringWorkspace.LevelSetupGroupName);
                if (setupGroup != null)
                    markerObject.transform.SetParent(setupGroup.transform, false);
                marker = markerObject.AddComponent<LevelSceneMarker>();
            }

            marker.EditorSetId(sceneId);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
