using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KitchenBedroomPolishBuilder
{
    public static string Apply(string roomId)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
        if (!KitchenBedroomArrangementProfile.IsReviewedRoom(roomId)) throw new InvalidOperationException("Unreviewed room.");
        CatHomeEditPreview.Clear();
        var report = new StringBuilder();
        // Read the unmodified prefab entrance before planning. Reapply the new
        // plan to targets only after it has passed the common room constraints.
        HomeRoomArrangementBuilder.ReplanRoom(roomId);
        foreach (var product in StoreCatalogAssets.PlaceableProducts.Where(d => HomeStoreService.IsProductInRoomCollection(roomId, d.ProductId)))
            RoomProductInteractionBuilder.UpgradePrefab(product, report);
        HomeRoomService.TryGetRoom(roomId, out var room);
        var scene = SceneManager.GetSceneByPath(room.ScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath, OpenSceneMode.Additive);
        try
        {
            StoreProductContentBuilder.BuildRoomSceneProducts(scene, roomId, null);
            var errors = HomeRoomArrangementValidation.Validate(scene, roomId);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
        return roomId + ": perimeter layout and contacts saved.\n" + report;
    }
}
