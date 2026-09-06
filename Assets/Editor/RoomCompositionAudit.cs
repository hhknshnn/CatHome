using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Checks complete collections, including actual walking clearance.</summary>
public static class RoomCompositionAudit
{
    public static string Capture(string roomId)
    {
        string path = null;
        foreach (var room in HomeRoomService.Rooms)
            if (room.Id == roomId) path = room.ScenePath;
        if (path == null) return "Unknown room: " + roomId;
        Scene original = SceneManager.GetActiveScene();
        Scene roomScene = SceneManager.GetSceneByPath(path);
        bool opened = !roomScene.IsValid() || !roomScene.isLoaded;
        var states = new Dictionary<GameObject, bool>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
            foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                states[root] = root.activeSelf;
        if (opened) roomScene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        GameObject cameraHost = null;
        var report = new StringBuilder();
        try
        {
            foreach (var pair in states)
                if (pair.Key.scene != roomScene) pair.Key.SetActive(false);
            var activities = new List<CatActivity>();
            foreach (var root in roomScene.GetRootGameObjects())
            {
                foreach (var part in root.GetComponentsInChildren<Transform>(true))
                    if (part.name == "Loft Ceiling") { states[part.gameObject] = part.gameObject.activeSelf; part.gameObject.SetActive(false); }
                foreach (var display in root.GetComponentsInChildren<StoreProductDisplay>(true))
                {
                    var visual = new SerializedObject(display).FindProperty("visualRoot").objectReferenceValue as GameObject;
                    if (visual == null) continue;
                    states[visual] = visual.activeSelf;
                    visual.SetActive(HomeStoreService.IsProductInRoomCollection(roomId, display.ProductId));
                }
                foreach (var activity in root.GetComponentsInChildren<CatActivity>(true))
                    if (HomeStoreService.IsProductInRoomCollection(roomId, activity.StoreProductId)) activities.Add(activity);
            }
            Physics.SyncTransforms();
            report.AppendLine(roomId + ": activities=" + activities.Count);
            const int width = 38, depth = 28;
            const float cell = .2f;
            var open = new bool[width, depth];
            var reachable = new bool[width, depth];
            var queue = new Queue<Vector2Int>();
            Vector2Int seed = new Vector2Int(18, 5);
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    open[x, z] = IsClear(new Vector3(-3.7f + x * cell, 0f, -2.7f + z * cell));
            if (!open[seed.x, seed.y])
            {
                float best = float.PositiveInfinity;
                Vector2Int requested = seed;
                for (int x = 0; x < width; x++) for (int z = 0; z < depth; z++)
                    if (open[x, z] && Vector2.Distance(new Vector2(x, z), requested) < best)
                    { best = Vector2.Distance(new Vector2(x, z), requested); seed = new Vector2Int(x, z); }
            }
            queue.Enqueue(seed); reachable[seed.x, seed.y] = true;
            Vector2Int[] neighbours = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (var delta in neighbours)
                {
                    Vector2Int next = current + delta;
                    if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= depth ||
                        !open[next.x, next.y] || reachable[next.x, next.y]) continue;
                    reachable[next.x, next.y] = true; queue.Enqueue(next);
                }
            }
            foreach (var activity in activities)
            {
                var anchor = new SerializedObject(activity).FindProperty("interactionAnchor").objectReferenceValue as Transform;
                if (anchor == null) { report.AppendLine(activity.StoreProductId + ": MISSING ANCHOR"); continue; }
                float nearest = float.PositiveInfinity;
                for (int x = 0; x < width; x++) for (int z = 0; z < depth; z++)
                    if (reachable[x, z]) nearest = Mathf.Min(nearest, Vector2.Distance(
                        new Vector2(-3.7f + x * cell, -2.7f + z * cell), new Vector2(anchor.position.x, anchor.position.z)));
                report.AppendLine(activity.StoreProductId + " " + activity.Kind +
                    ": anchor=" + anchor.position.ToString("F3") + " clear=" + IsClear(anchor.position) +
                    " reachableDistance=" + nearest.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            }
            Directory.CreateDirectory("Temp/FixedRoomAudit");
            File.WriteAllText("Temp/FixedRoomAudit/" + roomId + "-clearance.txt", report.ToString());
            cameraHost = new GameObject("RoomCompositionCaptureCamera");
            var camera = cameraHost.AddComponent<Camera>();
            camera.transform.position = new Vector3(-1f, 6.3f, -7.8f);
            camera.transform.LookAt(new Vector3(0f, .35f, .1f));
            camera.fieldOfView = 48f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.76f, .9f, .9f);
            Render(camera, "Temp/FixedRoomAudit/" + roomId + "-full.png");
            camera.transform.position = new Vector3(0f, 10f, 0f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 3.7f;
            Render(camera, "Temp/FixedRoomAudit/" + roomId + "-top.png");
            return report.ToString();
        }
        finally
        {
            if (cameraHost != null) Object.DestroyImmediate(cameraHost);
            foreach (var pair in states) if (pair.Key != null) pair.Key.SetActive(pair.Value);
            if (opened) EditorSceneManager.CloseScene(roomScene, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            Physics.SyncTransforms();
        }
    }

    private static bool IsClear(Vector3 point)
    {
        point.y = 0f;
        foreach (var collider in Physics.OverlapCapsule(point + Vector3.up * .265f,
                     point + Vector3.up * .385f, .24f, ~0, QueryTriggerInteraction.Ignore))
            if (collider.GetComponentInParent<CatMovement>() == null) return false;
        return true;
    }

    private static void Render(Camera camera, string path)
    {
        var target = new RenderTexture(1280, 900, 24);
        var previous = RenderTexture.active;
        var texture = new Texture2D(1280, 900, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 1280, 900), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(texture);
        }
    }
}
