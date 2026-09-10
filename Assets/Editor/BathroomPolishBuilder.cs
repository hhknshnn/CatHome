using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BathroomPolishBuilder
{
    public static string Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        CatHomeEditPreview.Clear();
        var report = new StringBuilder();
        var products = StoreCatalogAssets.PlaceableProducts.Where(d => HomeStoreService.IsProductInRoomCollection(HomeRoomService.BathroomId, d.ProductId)).ToArray();
        foreach (var product in products) RoomProductInteractionBuilder.UpgradePrefab(product, report);
        HomeRoomArrangementBuilder.ReplanRoom(HomeRoomService.BathroomId);
        var scene = SceneManager.GetSceneByPath(HomeRoomService.BathroomScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(HomeRoomService.BathroomScenePath, OpenSceneMode.Additive);
        try
        {
            StoreProductContentBuilder.BuildRoomSceneProducts(scene, HomeRoomService.BathroomId, null);
            var errors = HomeRoomArrangementValidation.Validate(scene, HomeRoomService.BathroomId);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
        return "Bathroom only: aligned collection and reachable activities saved.\n" + report;
    }
}
