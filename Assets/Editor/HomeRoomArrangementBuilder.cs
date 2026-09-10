using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bakes the common zoning policy into catalog, complete prefabs, contact points and scenes.</summary>
public static class HomeRoomArrangementBuilder
{
    public const string CatalogPath = "Assets/Resources/Home/RoomLayoutCatalog.asset";
    private const string PrefabFolder = "Assets/Art/StoreProducts/Prefabs/";
    public const string QaDirectory = "Docs/QA/ROOM_LAYOUT_2026-09-07";

    public static string PlanAll()
    {
        var entries = new List<HomeRoomLayoutEntry>();
        foreach (var room in HomeRoomService.Rooms)
            if (room.Id != HomeRoomService.LivingRoomId) entries.AddRange(PlanRoom(room.Id));
        SaveCatalog(entries);
        return "Planned " + entries.Count + " products across " + (HomeRoomService.Rooms.Count - 1) + " rooms.";
    }

    public static void ReplanRoom(string roomId)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Plan room arrangements in Edit Mode.");
        if (roomId == HomeRoomService.LivingRoomId) throw new InvalidOperationException("Living Room keeps its approved arrangement.");
        var catalog = AssetDatabase.LoadAssetAtPath<HomeRoomLayoutCatalog>(CatalogPath);
        var entries = catalog != null ? catalog.Entries.Where(row => row.roomId != roomId).ToList() : new List<HomeRoomLayoutEntry>();
        entries.AddRange(PlanRoom(roomId)); SaveCatalog(entries);
    }

    private static List<HomeRoomLayoutEntry> PlanRoom(string roomId)
    {
        var items = new List<HomeRoomLayoutPlanner.Item>();
        foreach (var definition in StoreCatalogAssets.PlaceableProducts)
        {
            if (!HomeStoreService.IsProductInRoomCollection(roomId, definition.ProductId)) continue;
            var raw = definition.WithoutRoomLayout();
            var root = PrefabUtility.LoadPrefabContents(PrefabFolder + raw.PrefabName + ".prefab");
            try
            {
                var stamp = root.GetComponent<RoomProductScaleStamp>(); float oldScale = stamp != null ? stamp.AppliedScale : 1;
                var activity = root.GetComponent<CatActivity>();
                Vector3 entry = activity != null && activity.RoutineEntryPoint != null ?
                    root.transform.InverseTransformPoint(activity.RoutineEntryPoint.position) / oldScale : Vector3.back * (raw.Footprint.y * .5f + .35f);
                float scale = HomeRoomLayoutPlanner.ScaleFor(raw.PrefabName);
                // New products automatically obey the same maximum silhouette budget.
                scale = Mathf.Min(scale, 2.45f / Mathf.Max(raw.Footprint.x, raw.Footprint.y), 2.12f / Mathf.Max(.12f, raw.Height));
                var item = new HomeRoomLayoutPlanner.Item { id = raw.ProductId, roomId = roomId, name = raw.PrefabName,
                    size = raw.Footprint * scale, height = raw.Height * scale, scale = scale,
                    hungHeight = raw.HungHeight, originalPosition = new Vector3(raw.DefaultPosition.x, raw.HungHeight, raw.DefaultPosition.z),
                    originalYaw = raw.DefaultYaw, entry = entry * scale, wallEdge = raw.PlacementKind == HomeProductPlacementKind.WallEdge };
                if (activity != null)
                {
                    if (activity is CartNudgeActivity cart)
                        item.requiredFacing = root.transform.InverseTransformDirection(cart.WorldRollDirection);
                    var serialized = new SerializedObject(activity);
                    foreach (string field in new[] { "perchPoint", "nestPoint", "seatPoint", "upperShelfPoint", "padPoint", "digPoint", "scratchPoint", "swatPoint" })
                    {
                        var property = serialized.FindProperty(field);
                        var point = property != null ? property.objectReferenceValue as Transform : null;
                        if (point != null) item.activityViews.Add(root.transform.InverseTransformPoint(point.position) * (scale / oldScale));
                    }
                }
                items.Add(item);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var features = new List<Bounds>(); var obstacles = new List<Bounds>();
        var views = ArchitecturalViews(roomId, features, obstacles);
        if (roomId == HomeRoomService.BathroomId)
            return BathroomArrangementProfile.Plan(items, views, features, obstacles);
        return HomeRoomLayoutPlanner.Plan(items, views, features, obstacles);
    }

    private static List<Vector3> ArchitecturalViews(string roomId, List<Bounds> features, List<Bounds> obstacles)
    {
        var views = new List<Vector3>();
        if (!HomeRoomService.TryGetRoom(roomId, out var room)) return views;
        var scene = SceneManager.GetSceneByPath(room.ScenePath); bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath, OpenSceneMode.Additive);
        try
        {
            ConfigureArchitecture(scene);
            foreach (var root in scene.GetRootGameObjects())
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                string name = node.name.ToLowerInvariant();
                if (node.GetComponent<HomeRoomLayoutObstacle>() != null)
                {
                    var fixedRenderers = node.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
                    if (fixedRenderers.Length > 0)
                    {
                        var fixedBounds = fixedRenderers[0].bounds;
                        foreach (var renderer in fixedRenderers) fixedBounds.Encapsulate(renderer.bounds);
                        obstacles.Add(fixedBounds);
                    }
                }
                if ((!name.Contains("window") && !name.Contains("door")) || node.childCount == 0 || node.GetComponentInParent<HomeProductPlacement>() != null) continue;
                var renderers = node.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                if (bounds.size.y < .6f || bounds.size.y > 3.3f || bounds.size.x > 5f || bounds.size.z > 5f) continue;
                var clearance = bounds;
                bool door = name.Contains("door");
                if (door) clearance.Expand(new Vector3(.12f, 0, .65f));
                features.Add(clearance);
                foreach (float y in new[] { Mathf.Max(door ? 1.35f : 0, bounds.center.y), door ? .85f : bounds.min.y + .18f })
                {
                    var center = bounds.center; center.y = y;
                    Vector3 along = bounds.size.x >= bounds.size.z ? Vector3.right : Vector3.forward;
                    float offset = Mathf.Min(.45f, Mathf.Max(bounds.extents.x, bounds.extents.z) * .7f);
                    views.Add(center); views.Add(center + along * offset); views.Add(center - along * offset);
                }
            }
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        return views;
    }

    public static void ConfigureArchitecture(Scene scene)
    {
        var nodes = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var tree = nodes.FirstOrDefault(node => node.name == "Courtyard Tree");
        if (tree != null)
        {
            tree.position = new Vector3(-3.02f, 0, 2.12f); tree.localScale = Vector3.one * .65f;
            var watch = nodes.FirstOrDefault(node => node.name == "BirdWatchActivity");
            var roost = tree.Find("BirdRoost");
            if (watch != null && roost != null)
            {
                watch.position = new Vector3(-2.25f, 0, 1.6f);
                var look = watch.Find("BirdLookPoint"); if (look != null) look.position = roost.position;
            }
        }
        foreach (var node in nodes)
        {
            if (node.name == "FrontLeftFence" || node.name == "FrontRightFence") HomeRoomCameraBuilder.HideForeground(node);
            if (node.name == "Courtyard Tree" || node.name == "Loft Stair Landing" || node.name.StartsWith("CornerPot_", StringComparison.Ordinal))
                if (node.GetComponent<HomeRoomLayoutObstacle>() == null) node.gameObject.AddComponent<HomeRoomLayoutObstacle>();
        }
        SceneObservationFacingBuilder.Configure(scene);
    }

    private static void SaveCatalog(IEnumerable<HomeRoomLayoutEntry> entries)
    {
        Directory.CreateDirectory("Assets/Resources/Home");
        var catalog = AssetDatabase.LoadAssetAtPath<HomeRoomLayoutCatalog>(CatalogPath);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<HomeRoomLayoutCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
        catalog.EditorReplace(entries); EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
    }

    public static void EnsureRoomPlan(string roomId)
    {
        if (roomId == HomeRoomService.LivingRoomId) return;
        var definitions = StoreCatalogAssets.PlaceableProducts.Where(d => HomeStoreService.IsProductInRoomCollection(roomId, d.ProductId)).ToArray();
        if (definitions.Length == 0 || definitions.All(d => HomeRoomLayoutCatalog.TryGet(d.ProductId, out _))) return;
        var catalog = AssetDatabase.LoadAssetAtPath<HomeRoomLayoutCatalog>(CatalogPath);
        var entries = catalog != null ? catalog.Entries.Where(row => row.roomId != roomId).ToList() : new List<HomeRoomLayoutEntry>();
        entries.AddRange(PlanRoom(roomId)); SaveCatalog(entries);
        foreach (var definition in definitions) RoomProductInteractionBuilder.UpgradePrefab(definition, new StringBuilder());
    }

    public static void ApplyProductScale(GameObject root, StoreCatalogAsset definition)
    {
        if (!HomeRoomLayoutCatalog.TryGet(definition.ProductId, out var row)) return;
        var stamp = root.GetComponent<RoomProductScaleStamp>();
        if (stamp == null) stamp = root.AddComponent<RoomProductScaleStamp>();
        float ratio = row.modelScale / Mathf.Max(.001f, stamp.AppliedScale);
        if (Mathf.Abs(ratio - 1) > .0001f)
            foreach (Transform child in root.transform)
            {
                child.localPosition *= ratio;
                if (child.GetComponentInChildren<Renderer>(true) != null || child.GetComponentInChildren<Collider>(true) != null)
                    child.localScale *= ratio;
            }
        stamp.EditorSet(row.modelScale);
        var placement = root.GetComponent<HomeProductPlacement>();
        if (placement != null)
        {
            var serialized = new SerializedObject(placement);
            serialized.FindProperty("footprintSize").vector2Value = row.footprint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static void ConfigureApproaches(Scene scene, string roomId)
    {
        if (roomId == HomeRoomService.LivingRoomId) return;
        ConfigureArchitecture(scene);
        RoomActivityLayoutBuilder.Configure(scene, roomId);
    }

    public static string ApplyAll(bool replan = true)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build room arrangements in Edit Mode.");
        var report = new StringBuilder(replan ? PlanAll() : "Applying the reviewed common room plan.").AppendLine();
        foreach (var definition in StoreCatalogAssets.PlaceableProducts)
            if (HomeRoomLayoutCatalog.TryGet(definition.ProductId, out _)) RoomProductInteractionBuilder.UpgradePrefab(definition, report);
        var original = SceneManager.GetActiveScene();
        try
        {
            foreach (var room in HomeRoomService.Rooms)
            {
                if (room.Id == HomeRoomService.LivingRoomId) continue;
                var scene = SceneManager.GetSceneByPath(room.ScenePath); bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(room.ScenePath, OpenSceneMode.Additive);
                try
                {
                    StoreProductContentBuilder.BuildRoomSceneProducts(scene, room.Id, null);
                    EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    report.AppendLine(room.Id + ": scaled products, visible approaches, fixed arrangement saved.");
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
        }
        finally { if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original); }
        AssetDatabase.SaveAssets(); Directory.CreateDirectory(QaDirectory);
        File.WriteAllText(QaDirectory + "/migration.txt", report.ToString());
        return report.ToString();
    }
}
