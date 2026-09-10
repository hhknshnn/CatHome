using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Two physical viewing bays beside the living-room sofa; all product geometry stays together.</summary>
public static class LivingRoomGazeLayoutBuilder
{
    public static readonly Vector3 TallPlantPosition = new Vector3(3.30f, 0f, 1.15f);
    public static readonly Vector3 TallPlantEntryLocal = new Vector3(0f, 0f, .75f);
    public static readonly Vector3 WindowEntry = new Vector3(2.45f, 0f, .85f);

    public static void ConfigureWindowEntry(SitLookActivity activity)
    {
        if (activity == null || activity.Kind != CatActivityKind.WindowWatch ||
            activity.gameObject.scene.path != HomeRoomService.LivingRoomScenePath) return;
        var anchor = activity.RoutineEntryPoint;
        if (anchor == null) throw new InvalidOperationException("Window observation has no entry anchor.");
        anchor.position = WindowEntry;
        activity.EditorConfigureEntry(anchor);
        Dirty(anchor); Dirty(activity);
    }

    public static void Apply(Scene scene)
    {
        if (scene.path != HomeRoomService.LivingRoomScenePath) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var placement in root.GetComponentsInChildren<HomeProductPlacement>(true))
            {
                if (placement.ProductId != HomeStoreService.TallPlantId) continue;
                // Move the authored assembly, including visible mesh, collider and points.
                placement.transform.position = TallPlantPosition;
                var activity = placement.GetComponent<SitLookActivity>();
                if (activity == null || activity.RoutineEntryPoint == null)
                    throw new InvalidOperationException("Tall plant must retain its real gaze activity and entry.");
                activity.RoutineEntryPoint.position = placement.transform.TransformPoint(TallPlantEntryLocal);
                activity.EditorConfigureEntry(activity.RoutineEntryPoint);
                Dirty(placement.transform); Dirty(activity.RoutineEntryPoint); Dirty(activity);
            }
            foreach (var activity in root.GetComponentsInChildren<SitLookActivity>(true)) ConfigureWindowEntry(activity);
        }
    }

    public static string ApplyAndSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
        CatHomeEditPreview.Clear();
        var definition = StoreCatalogAssets.PlaceableProducts.Single(d => d.ProductId == HomeStoreService.TallPlantId);
        var report = new StringBuilder();
        RoomProductInteractionBuilder.UpgradePrefab(definition, report);
        Scene original = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(HomeRoomService.LivingRoomScenePath, OpenSceneMode.Additive);
        try
        {
            Apply(scene);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var placement in root.GetComponentsInChildren<HomeProductPlacement>(true))
                if (placement.ProductId == HomeStoreService.TallPlantId)
                    SitLookFacingBuilder.Configure(placement.gameObject, definition);
            SceneObservationFacingBuilder.Configure(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save living-room viewing bays.");
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
        return "Tall plant assembly and window observation entry updated; other product poses and all materials unchanged.";
    }

    static void Dirty(UnityEngine.Object value)
    {
        EditorUtility.SetDirty(value);
        if (PrefabUtility.IsPartOfPrefabInstance(value)) PrefabUtility.RecordPrefabInstancePropertyModifications(value);
    }
}
