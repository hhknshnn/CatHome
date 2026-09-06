using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HomeRoomGameplaySafetyTests
{
    private static readonly string[] RoomScenePaths =
    {
        HomeRoomService.LivingRoomScenePath,
        HomeRoomService.BathroomScenePath,
        HomeRoomService.KitchenScenePath,
        HomeRoomService.BedroomScenePath,
        HomeRoomService.GardenScenePath,
        HomeRoomService.BalconyScenePath,
        HomeRoomService.PatioScenePath,
        HomeRoomService.SecondFloorScenePath
    };

    [Test]
    public void EveryRoom_HasCanonicalMovementBoundary()
    {
        foreach (string path in RoomScenePaths)
        {
            WithScene(path, scene =>
            {
                HomeRoomBoundary boundary = FindComponent<HomeRoomBoundary>(scene);
                Assert.That(boundary, Is.Not.Null, path);
                Assert.That(boundary.MinimumXZ.x,
                    Is.EqualTo(HomeRoomShellMetrics.InteriorMinX).Within(.001f), path);
                Assert.That(boundary.MinimumXZ.y,
                    Is.EqualTo(HomeRoomShellMetrics.InteriorMinZ).Within(.001f), path);
                Assert.That(boundary.MaximumXZ.x,
                    Is.EqualTo(HomeRoomShellMetrics.InteriorMaxX).Within(.001f), path);
                Assert.That(boundary.MaximumXZ.y,
                    Is.EqualTo(HomeRoomShellMetrics.InteriorMaxZ).Within(.001f), path);
            });
        }
    }

    [Test]
    public void Boundary_ClampsTheWholeCatFootprintInsideAllFourEdges()
    {
        GameObject root = new GameObject("BoundaryTest");
        try
        {
            HomeRoomBoundary boundary = root.AddComponent<HomeRoomBoundary>();
            boundary.Configure(new Vector2(-3.8f, -3f), new Vector2(3.8f, 2.8f), .05f);
            Vector3 clamped = boundary.ClampPosition(new Vector3(99f, 2f, -99f), .55f);
            Assert.That(clamped.x, Is.EqualTo(3.2f).Within(.001f));
            Assert.That(clamped.z, Is.EqualTo(-2.4f).Within(.001f));
            Assert.That(clamped.y, Is.EqualTo(2f).Within(.001f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void LivingRoom_UsesFixedDaylightAndSolidBaseFurniture()
    {
        WithScene(HomeRoomService.LivingRoomScenePath, scene =>
        {
            GameTimeService time = FindComponent<GameTimeService>(scene);
            Assert.That(time, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(time);
            Assert.That(serialized.FindProperty("useTestTime").boolValue, Is.True);
            Assert.That(serialized.FindProperty("testHour").floatValue,
                Is.EqualTo(HomeRoomGameplaySafetyBuilder.LivingRoomDaylightHour).Within(.001f));

            AssertSolidFurniture(scene, HomeRoomGameplaySafetyBuilder.LivingSofaName);
            AssertSolidFurniture(scene, HomeRoomGameplaySafetyBuilder.LivingCoffeeTableName);
            Assert.That(PlayerLightingPreference.Multiplier,
                Is.EqualTo(PlayerLightingPreference.CanonicalMultiplier).Within(.001f));
        });
    }

    [Test]
    public void MainPanel_HasNoClockOrBrightnessControls()
    {
        const string path = "Assets/UI/MainPanel.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Assert.That(FindNamed(root.transform, "LightButton"), Is.Null);
            Assert.That(FindNamed(root.transform, "ClockDisplay"), Is.Null);
            Assert.That(FindNamed(root.transform, "BrightnessPanel"), Is.Null);
            Assert.That(root.GetComponent<BrightnessPanelView>(), Is.Null);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AssertSolidFurniture(Scene scene, string objectName)
    {
        Transform furniture = FindNamed(scene, objectName);
        Assert.That(furniture, Is.Not.Null, objectName);
        BoxCollider collider = furniture.GetComponent<BoxCollider>();
        Assert.That(collider, Is.Not.Null, objectName);
        Assert.That(collider.enabled, Is.True, objectName);
        Assert.That(collider.isTrigger, Is.False, objectName);

        Renderer[] renderers = furniture.GetComponentsInChildren<Renderer>(true);
        Assert.That(renderers.Length, Is.GreaterThan(0), objectName);
        Bounds visible = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            visible.Encapsulate(renderers[i].bounds);
        Bounds solid = collider.bounds;
        const float tolerance = .002f;
        Assert.That(solid.min.x, Is.LessThanOrEqualTo(visible.min.x + tolerance), objectName);
        Assert.That(solid.min.y, Is.LessThanOrEqualTo(visible.min.y + tolerance), objectName);
        Assert.That(solid.min.z, Is.LessThanOrEqualTo(visible.min.z + tolerance), objectName);
        Assert.That(solid.max.x, Is.GreaterThanOrEqualTo(visible.max.x - tolerance), objectName);
        Assert.That(solid.max.y, Is.GreaterThanOrEqualTo(visible.max.y - tolerance), objectName);
        Assert.That(solid.max.z, Is.GreaterThanOrEqualTo(visible.max.z - tolerance), objectName);
    }

    private static Transform FindNamed(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindNamed(roots[i].transform, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (string.Equals(all[i].name, name, StringComparison.Ordinal))
                return all[i];
        return null;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T found = roots[i].GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void WithScene(string path, Action<Scene> body)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            body(scene);
        }
        finally
        {
            if (opened && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
