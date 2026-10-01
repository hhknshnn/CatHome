using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Fixed imported furniture follows the catalog's exact-mesh solid policy.</summary>
public static class HomeFixedFurnitureCollisionBuilder
{
    // The caller passes an explicit authored model root. Never search the room
    // for arbitrary renderer names or turn decorations into solid geometry.
    public static int EnsureMeshGeometry(Transform modelRoot)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Author fixed furniture outside Play Mode.");
        if (modelRoot == null) throw new ArgumentNullException(nameof(modelRoot));
        var filters = modelRoot.GetComponentsInChildren<MeshFilter>(true)
            .Where(f => f.GetComponent<MeshRenderer>() != null).ToArray();
        if (filters.Length == 0) throw new InvalidOperationException(modelRoot.name + " has no authored mesh.");
        // Validate every source before making the first change. A future model
        // with procedural effects needs an explicit semantic authoring choice.
        foreach (var filter in filters)
            if (filter.sharedMesh == null || !AssetDatabase.GetAssetPath(filter.sharedMesh).EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Expected an imported fixed mesh: " + filter.name);
        if (modelRoot.GetComponentsInChildren<Rigidbody>(true).Length != 0)
            throw new InvalidOperationException("Static nonconvex geometry requires a fixed model: " + modelRoot.name);
        int changed = 0;
        foreach (var filter in filters)
        {
            var collider = filter.GetComponent<MeshCollider>();
            if (collider != null && collider.enabled && !collider.isTrigger && !collider.convex && collider.sharedMesh == filter.sharedMesh) continue;
            if (collider == null) collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
            Undo.RecordObject(collider, "Use fixed furniture's actual mesh");
            collider.isTrigger = false; collider.convex = false;
            collider.sharedMesh = filter.sharedMesh; collider.enabled = true;
            EditorUtility.SetDirty(collider); changed++;
        }
        foreach (var box in modelRoot.GetComponentsInChildren<BoxCollider>(true))
        {
            if (box.isTrigger) continue;
            Undo.RecordObject(box, "Keep furniture reservation as picking trigger");
            // Preserve the original pick volume, enabled flag and transform.
            // It is no longer a solid block across the cushions/leg openings.
            box.isTrigger = true; EditorUtility.SetDirty(box); changed++;
        }
        return changed;
    }

    public static Transform[] FixedModels(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.path != HomeRoomService.LivingRoomScenePath)
            throw new InvalidOperationException("The loaded authored Living Room scene is required.");
        var furnitureRoot = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "02 Furniture");
        var owner = furnitureRoot != null ? furnitureRoot.transform.Find("RoomFurniture") : null;
        if (owner == null) throw new InvalidOperationException("Missing known RoomFurniture authoring hierarchy.");
        var models = new[] { owner.Find(HomeRoomGameplaySafetyBuilder.LivingSofaName),
            owner.Find(HomeRoomGameplaySafetyBuilder.LivingCoffeeTableName) };
        if (models.Any(t => t == null)) throw new InvalidOperationException("The two permanent furniture models must exist.");
        return models;
    }

    // Targeted migration: no rebuild, lighting/boundary mutation, opening other
    // scenes, prefab saving, or automatic scene saving. Caller reviews/saves.
    public static int Apply(Scene scene)
    {
        int changed = 0;
        foreach (var model in FixedModels(scene)) changed += EnsureMeshGeometry(model);
        if (changed > 0) EditorSceneManager.MarkSceneDirty(scene);
        return changed;
    }
    public static int ApplyLoadedLivingRoom() => Apply(SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath));
}
