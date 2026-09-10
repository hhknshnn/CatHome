using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Refresh facing authoring without replacing room hierarchies or their approved placement.</summary>
public static class CameraFacingAuthoringBuilder
{
    const string PrefabFolder = "Assets/Art/StoreProducts/Prefabs/";

    public static string ApplyAll()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        CatHomeEditPreview.Clear();
        Scene original = SceneManager.GetActiveScene();
        var report = new StringBuilder();
        var selected = new Dictionary<string, StoreCatalogAsset>(StringComparer.Ordinal);
        foreach (var definition in StoreCatalogAssets.PlaceableProducts)
        {
            bool currentRoom = HomeRoomService.Rooms.Any(r => HomeStoreService.IsProductInRoomCollection(r.Id, definition.ProductId));
            bool currentCat = HomeStoreService.TryGetProduct(definition.ProductId, out var product) && product.StoreCategory == HomeStoreCategory.Cat;
            if (!currentRoom && !currentCat) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + definition.PrefabName + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing product prefab: " + definition.ProductId);
            if (prefab.GetComponentInChildren<SitLookActivity>(true) == null && prefab.GetComponentInChildren<SinkSipActivity>(true) == null) continue;
            RoomProductInteractionBuilder.UpgradePrefab(definition, report);
            selected.Add(definition.ProductId, definition);
        }
        try
        {
            foreach (var room in HomeRoomService.Rooms)
            {
                var scene = SceneManager.GetSceneByPath(room.ScenePath);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath, OpenSceneMode.Additive);
                try
                {
                    LivingRoomGazeLayoutBuilder.Apply(scene);
                    int count = 0;
                    foreach (var sceneRoot in scene.GetRootGameObjects())
                    foreach (var activity in sceneRoot.GetComponentsInChildren<CatActivity>(true))
                    {
                        if (!(activity is SitLookActivity) && !(activity is SinkSipActivity)) continue;
                        if (!selected.TryGetValue(activity.StoreProductId ?? string.Empty, out var definition)) continue;
                        var sourceRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + definition.PrefabName + ".prefab");
                        var source = sourceRoot.GetComponentsInChildren<CatActivity>(true).FirstOrDefault(a => a.GetType() == activity.GetType());
                        if (source == null) throw new InvalidOperationException(definition.ProductId + ": activity type differs from its current prefab.");
                        CopyActivity(source, activity, sourceRoot.transform);
                        count++;
                    }
                    // Recheck floor anchors against the existing filled room;
                    // copying a prefab entry alone would discard its authored
                    // neighbour clearance. This does not rebuild architecture.
                    if (room.Id != HomeRoomService.LivingRoomId) RoomActivityLayoutBuilder.Configure(scene, room.Id);
                    int observations = SceneObservationFacingBuilder.Configure(scene);
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + room.ScenePath);
                    report.AppendLine(room.Id + ": refreshed " + count + " product activities and " + observations + " scene observations.");
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
        }
        finally { if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
        return report.ToString();
    }

    static void CopyActivity(CatActivity source, CatActivity target, Transform sourceRoot)
    {
        Transform targetRoot = target.transform;
        var placement = target.GetComponentInParent<HomeProductPlacement>();
        if (placement != null && placement.ProductId == target.StoreProductId) targetRoot = placement.transform;
        var from = new SerializedObject(source); var to = new SerializedObject(target);
        foreach (string field in new[] { "interactionAnchor", "routineEntryPoint", "lookPoint", "floorPoint", "perchPoint", "sipTarget" })
        {
            var sourceProperty = from.FindProperty(field); var targetProperty = to.FindProperty(field);
            if (sourceProperty == null || targetProperty == null) continue;
            var sourcePoint = sourceProperty.objectReferenceValue as Transform;
            if (sourcePoint == null) { targetProperty.objectReferenceValue = null; continue; }
            string path = AnimationUtility.CalculateTransformPath(sourcePoint, sourceRoot);
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException(source.name + ": a care anchor cannot replace the product root.");
            Transform point = EnsurePoint(targetRoot, path);
            point.SetPositionAndRotation(targetRoot.TransformPoint(sourceRoot.InverseTransformPoint(sourcePoint.position)),
                targetRoot.rotation * Quaternion.Inverse(sourceRoot.rotation) * sourcePoint.rotation);
            point.localScale = sourcePoint.localScale;
            var sourceSurface = sourcePoint.GetComponent<CatActivitySurface>();
            if (sourceSurface != null)
            {
                var surface = point.GetComponent<CatActivitySurface>();
                if (surface == null) surface = point.gameObject.AddComponent<CatActivitySurface>();
                EditorUtility.CopySerialized(sourceSurface, surface);
                EditorUtility.SetDirty(surface);
                if (PrefabUtility.IsPartOfPrefabInstance(surface)) PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            }
            targetProperty.objectReferenceValue = point;
            EditorUtility.SetDirty(point);
            if (PrefabUtility.IsPartOfPrefabInstance(point)) PrefabUtility.RecordPrefabInstancePropertyModifications(point);
        }
        foreach (string field in new[] { "visibleLookTargets", "reactionKind" })
        {
            var property = from.FindProperty(field);
            if (property != null && to.FindProperty(field) != null) to.CopyFromSerializedProperty(property);
        }
        to.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    static Transform EnsurePoint(Transform root, string path)
    {
        Transform current = root;
        foreach (string name in path.Split('/'))
        {
            var next = current.Find(name);
            if (next == null) { next = new GameObject(name).transform; next.SetParent(current, false); }
            current = next;
        }
        return current;
    }
}
