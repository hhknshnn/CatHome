using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Persistent bedroom play and rest authoring; preserves product identities.</summary>
public static class BedroomPlayRestBuilder
{
    public static void ConfigureDaybed(GameObject root, GameObject visual)
    {
        foreach (var old in root.GetComponents<CatActivity>()) Object.DestroyImmediate(old);
        var look = root.transform.Find("WatchLookPoint");
        if (look != null) Object.DestroyImmediate(look.gameObject);
        var floor = Point(root, "InteractionAnchor", new Vector3(.52f, 0, .86f));
        var perch = Point(root, "DaybedSleepPoint", new Vector3(.30f, .5094f, .07f));
        var surface = perch.GetComponent<CatActivitySurface>() ?? perch.gameObject.AddComponent<CatActivitySurface>();
        surface.EditorConfigurePose(CatActivityPose.Sleep, true);
        var activity = root.AddComponent<PerchNapActivity>();
        // Keep saved product/activity and bedroom quest identities compatible.
        activity.EditorConfigure("daybed-watch", "WINDOW DAYBED", CatActivityKind.DaybedWatch,
            QuestType.BedroomWatch, 0, "NAP", 1.1f, 0f, floor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomWindowDaybedId);
        activity.EditorConfigurePerch(floor, perch, 3.5f, 18f, 94f, "COZY BY THE WINDOW!");
    }

    public static void ConfigureYarn(GameObject root, GameObject visual)
    {
        foreach (var old in root.GetComponents<CatActivity>()) Object.DestroyImmediate(old);
        Transform anchor = Point(root, "InteractionAnchor", new Vector3(.12f, 0, -.78f));
        Transform ball = root.transform.Find("BedroomPlayBall");
        if (ball != null) Object.DestroyImmediate(ball.gameObject);
        {
            ball = new GameObject("BedroomPlayBall").transform;
            ball.SetParent(root.transform, false);
            var basket = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/GardenYarnBall.prefab");
            var source = basket.GetComponentsInChildren<MeshFilter>(true).Single(f =>
                AssetDatabase.GetAssetPath(f.sharedMesh).EndsWith("GardenYarnBallBall_Premium.fbx")).gameObject;
            var model = Object.Instantiate(source, ball, false);
            model.name = "YarnBall";
            foreach (var collider in model.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            var renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.localScale *= .18f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            model.transform.position += ball.position - bounds.center;
        }
        ball.localPosition = new Vector3(0, .4f, 0);
        ball.gameObject.SetActive(false);
        var activity = root.AddComponent<BallChaseActivity>();
        activity.EditorConfigure("yarn-swat", "YARN BASKET", CatActivityKind.YarnSwat,
            QuestType.PlayBall, 0, "PLAY BALL", 1.1f, 6f, anchor, null, visual);
        activity.EditorConfigureStoreProduct(HomeStoreService.BedroomYarnBasketId);
        activity.EditorConfigureBall(ball, new Transform[0], 3);
    }

    static Transform Point(GameObject root, string name, Vector3 position)
    {
        var point = root.transform.Find(name);
        if (point == null) { point = new GameObject(name).transform; point.SetParent(root.transform, false); }
        point.localPosition = position;
        return point;
    }
}
