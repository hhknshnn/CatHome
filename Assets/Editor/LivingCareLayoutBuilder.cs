using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>The requested living-room bed, bookshelf and wall care arrangement.</summary>
public static class LivingCareLayoutBuilder
{
    public static string ApplyAndSave()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Apply the living arrangement in Edit Mode.");
        CatHomeEditPreview.Clear();
        var scene = SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open the living-room scene first.");
        foreach (var id in new[] { HomeStoreService.BookshelfId, HomeStoreService.BookSetId, HomeStoreService.ModernPaintingId })
        {
            StoreProductContentBuilder.RebuildProductWithExistingMaterials(id);
            var definition = StoreCatalogAssets.PlaceableProducts.First(d => d.ProductId == id);
            var old = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HomeProductPlacement>(true)).First(p => p.ProductId == id);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/" + definition.PrefabName + ".prefab");
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            copy.name = old.name; copy.transform.SetParent(old.transform.parent, false);
            copy.transform.SetPositionAndRotation(definition.DefaultPosition, Quaternion.Euler(0, definition.DefaultYaw, 0));
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var bed = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).First(t => t.name == "Bed5 V3");
        bed.position = LivingRoomReferenceLayout.BedPosition;
        PremiumCareStationBuilder.Apply(scene);
        Physics.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return "Living bed/bookshelf and TV-wall care station updated; other rooms were not rebuilt.";
    }
}
