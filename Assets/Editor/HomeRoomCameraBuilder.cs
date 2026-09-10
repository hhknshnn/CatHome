using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Migrates authored room cameras without rebuilding their furniture or lighting.</summary>
public static class HomeRoomCameraBuilder
{
    public static string ApplyAll()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Apply room cameras in Edit Mode.");
        var original = SceneManager.GetActiveScene();
        var report = new StringBuilder();
        try
        {
            foreach (var definition in HomeRoomService.Rooms)
            {
                var scene = SceneManager.GetSceneByPath(definition.ScenePath);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(definition.ScenePath, OpenSceneMode.Additive);
                try
                {
                    int count = 0;
                    foreach (var root in scene.GetRootGameObjects())
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    {
                        HomeRoomCameraProfile.Apply(camera);
                        EditorUtility.SetDirty(camera);
                        EditorUtility.SetDirty(camera.transform);
                        EditorUtility.SetDirty(camera.GetComponent<HomeWorldViewport>());
                        count++;
                    }
                    if (count != 1) throw new System.InvalidOperationException(definition.SceneName + " needs one room camera.");
                    ApplyForegroundCutaways(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.AppendLine(definition.SceneName + ": front-centred camera applied.");
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
        return report.ToString();
    }

    // Only authored foreground architecture is cut away; colliders and room bounds remain intact.
    public static void HideForeground(Transform structure)
    {
        if (structure == null) return;
        foreach (var renderer in structure.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void ApplyForegroundCutaways(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
        {
            if (scene.path == HomeRoomService.BalconyScenePath && node.name == "FrontRail" &&
                node.parent != null && node.parent.name == "Deck Railing") HideForeground(node);
            if (scene.path == HomeRoomService.SecondFloorScenePath && node.name == "Loft Ceiling")
                foreach (Transform part in node)
                    if (!part.name.StartsWith("Pendant", System.StringComparison.Ordinal)) HideForeground(part);
        }
    }
}
