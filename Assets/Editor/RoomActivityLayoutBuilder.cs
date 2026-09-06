using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bakes reachable approach/exit points against the complete fixed collection.</summary>
public static class RoomActivityLayoutBuilder
{
    public static string Configure(Scene scene, string roomId)
    {
        var states = new Dictionary<GameObject, bool>();
        var report = new StringBuilder();
        var activities = new List<CatActivity>();
        try
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i) != scene)
                    foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    { states[root] = root.activeSelf; root.SetActive(false); }
            foreach (var root in scene.GetRootGameObjects())
            {
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
            var floor = CatActivityMotion.ReachableFloor(new Vector3(0f, 0f, -2f));
            var changed = new HashSet<Transform>();
            var claimedAnchors = new List<Vector3>();
            foreach (var activity in activities)
            {
                var data = new SerializedObject(activity);
                foreach (string name in new[] { "interactionAnchor", "routineEntryPoint", "floorPoint", "mountPoint",
                             "mouthPoint", "exitPoint", "approachPoint", "scratchPoint", "swatPoint", "shovePoint", "baskPoint" })
                {
                    var property = data.FindProperty(name);
                    var point = property != null ? property.objectReferenceValue as Transform : null;
                    if (point == null || changed.Contains(point)) continue;
                    Vector3 original = point.position;
                    Vector3 candidate = original; candidate.y = 0f;
                    bool isAnchor = name == "interactionAnchor";
                    float nearest = float.PositiveInfinity;
                    foreach (Vector3 p in floor)
                        if ((candidate - p).sqrMagnitude < .063f && CatActivityMotion.ClearSegment(candidate, p))
                            nearest = Mathf.Min(nearest, Vector3.Distance(candidate, p));
                    if (!CatActivityMotion.IsFloorClear(candidate, .30f) || nearest > .25f ||
                        (isAnchor && IsClaimed(claimedAnchors, candidate)))
                    {
                        Vector3 reference = original; reference.y = 0f;
                        float best = float.PositiveInfinity;
                        foreach (Vector3 p in floor)
                        {
                            float distance = (p - reference).sqrMagnitude;
                            if (distance >= best || !CatActivityMotion.IsFloorClear(p, .30f) ||
                                (isAnchor && IsClaimed(claimedAnchors, p))) continue;
                            best = distance; candidate = p;
                        }
                        if (best > 2.25f) throw new System.InvalidOperationException(activity.Kind + ": no nearby clear approach for " + name);
                    }
                    if (isAnchor) claimedAnchors.Add(candidate);
                    if ((candidate - original).sqrMagnitude > .00001f)
                    {
                        report.AppendLine(activity.StoreProductId + "/" + name + ": " + original.ToString("F3") + " -> " + candidate.ToString("F3"));
                        point.position = candidate;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(point);
                        changed.Add(point);
                    }
                }
            }
        }
        finally { foreach (var state in states) if (state.Key != null) state.Key.SetActive(state.Value); Physics.SyncTransforms(); }
        System.IO.Directory.CreateDirectory("Temp/FixedRoomAudit");
        System.IO.File.WriteAllText("Temp/FixedRoomAudit/" + roomId + "-approach-changes.txt", report.ToString());
        return report.ToString();
    }

    private static bool IsClaimed(List<Vector3> points, Vector3 candidate)
    {
        foreach (var point in points) if ((candidate - point).sqrMagnitude < .045f) return true;
        return false;
    }
}
