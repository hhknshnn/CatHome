using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Every room must present the same box to the shared camera. Living Room is the
/// reference; Bathroom and Kitchen have to match its floor extents, wall
/// placement, wall height, camera transform and cat scale exactly, otherwise the
/// same camera frames each room differently and the cat reads at another size.
/// </summary>
public sealed class HomeRoomShellTests
{
    private static readonly string[] RoomScenePaths =
    {
        HomeRoomService.LivingRoomScenePath,
        HomeRoomService.BathroomScenePath,
        HomeRoomService.KitchenScenePath,
        HomeRoomService.BedroomScenePath
    };

    private const float Tolerance = .001f;

    [Test]
    public void EveryRoomScene_UsesTheCanonicalShellBox()
    {
        foreach (string path in RoomScenePaths)
        {
            WithScene(path, scene =>
            {
                Transform floor = FindFloor(scene);
                Assert.That(floor, Is.Not.Null, $"'{path}' has no floor block.");
                AssertVector(floor.position, HomeRoomShellMetrics.FloorPosition,
                    $"'{path}' floor position");
                AssertVector(floor.lossyScale, HomeRoomShellMetrics.FloorScale,
                    $"'{path}' floor scale");

                AssertWall(scene, path, "back",
                    HomeRoomShellMetrics.BackWallPosition,
                    HomeRoomShellMetrics.BackWallScale);
                AssertWall(scene, path, "left",
                    HomeRoomShellMetrics.LeftWallPosition,
                    HomeRoomShellMetrics.SideWallScale);
                AssertWall(scene, path, "right",
                    HomeRoomShellMetrics.RightWallPosition,
                    HomeRoomShellMetrics.SideWallScale);
            });
        }
    }

    [Test]
    public void EveryRoomScene_SharesTheLivingRoomCameraAndCatScale()
    {
        Vector3? cameraPosition = null;
        Quaternion? cameraRotation = null;
        float fieldOfView = 0f;
        Vector3? catPosition = null;
        Vector3? catScale = null;

        foreach (string path in RoomScenePaths)
        {
            WithScene(path, scene =>
            {
                Camera camera = FindComponent<Camera>(scene);
                Assert.That(camera, Is.Not.Null, $"'{path}' has no camera.");
                Transform cat = FindNamed(scene, "CatRoot");
                Assert.That(cat, Is.Not.Null, $"'{path}' has no CatRoot.");

                if (cameraPosition == null)
                {
                    cameraPosition = camera.transform.position;
                    cameraRotation = camera.transform.rotation;
                    fieldOfView = camera.fieldOfView;
                    catPosition = cat.position;
                    catScale = cat.lossyScale;
                    return;
                }

                AssertVector(camera.transform.position, cameraPosition.Value,
                    $"'{path}' camera position");
                Assert.That(
                    Quaternion.Angle(camera.transform.rotation, cameraRotation.Value),
                    Is.LessThan(.05f), $"'{path}' camera rotation differs.");
                Assert.That(camera.fieldOfView, Is.EqualTo(fieldOfView).Within(Tolerance),
                    $"'{path}' camera field of view differs.");
                AssertVector(cat.position, catPosition.Value, $"'{path}' cat position");
                AssertVector(cat.lossyScale, catScale.Value, $"'{path}' cat scale");
            });
        }
    }

    [Test]
    public void EveryRoomProduct_FitsInsideTheCanonicalShell()
    {
        var failures = new List<string>();
        for (int i = 0; i < StoreCatalogAssets.PlaceableProducts.Length; i++)
        {
            StoreCatalogAsset definition = StoreCatalogAssets.PlaceableProducts[i];
            Vector3 position = definition.DefaultPosition;
            if (position == Vector3.zero)
                continue;

            float radians = definition.DefaultYaw * Mathf.Deg2Rad;
            float cos = Mathf.Abs(Mathf.Cos(radians));
            float sin = Mathf.Abs(Mathf.Sin(radians));
            float halfX = (definition.Footprint.x * cos + definition.Footprint.y * sin) * .5f;
            float halfZ = (definition.Footprint.x * sin + definition.Footprint.y * cos) * .5f;

            if (position.x - halfX < HomeRoomShellMetrics.InteriorMinX - Tolerance ||
                position.x + halfX > HomeRoomShellMetrics.InteriorMaxX + Tolerance ||
                position.z - halfZ < HomeRoomShellMetrics.InteriorMinZ - Tolerance ||
                position.z + halfZ > HomeRoomShellMetrics.InteriorMaxZ + Tolerance)
            {
                failures.Add(
                    $"{definition.ProductId} at {position} spans " +
                    $"x[{position.x - halfX:0.###}, {position.x + halfX:0.###}] " +
                    $"z[{position.z - halfZ:0.###}, {position.z + halfZ:0.###}]");
            }

            Assert.That(definition.Height,
                Is.LessThanOrEqualTo(HomeRoomShellMetrics.WallHeight),
                $"{definition.ProductId} is taller than the canonical wall.");
        }

        Assert.That(failures, Is.Empty,
            "Products reach through the canonical shell:\n" + string.Join("\n", failures));
    }

    private static void AssertWall(Scene scene, string path, string side,
        Vector3 expectedPosition, Vector3 expectedScale)
    {
        Transform wall = FindWall(scene, side);
        Assert.That(wall, Is.Not.Null, $"'{path}' has no {side} wall.");
        AssertVector(wall.position, expectedPosition, $"'{path}' {side} wall position");
        AssertVector(wall.lossyScale, expectedScale, $"'{path}' {side} wall scale");
    }

    private static void AssertVector(Vector3 actual, Vector3 expected, string label)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance), label + " x");
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance), label + " y");
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(Tolerance), label + " z");
    }

    private static Transform FindFloor(Scene scene)
    {
        return FindNamed(scene, "WalkableFloor") ?? FindNamed(scene, "Floor");
    }

    private static Transform FindWall(Scene scene, string side)
    {
        string prefix = char.ToUpperInvariant(side[0]) + side.Substring(1) + "Wall";
        return FindWhere(scene, t =>
            t.name.StartsWith(prefix, StringComparison.Ordinal) &&
            t.GetComponent<MeshRenderer>() != null &&
            t.GetComponent<BoxCollider>() != null);
    }

    private static Transform FindNamed(Scene scene, string name)
    {
        return FindWhere(scene, t => string.Equals(t.name, name, StringComparison.Ordinal));
    }

    private static Transform FindWhere(Scene scene, Func<Transform, bool> predicate)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (predicate(all[i]))
                    return all[i];
        }
        return null;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void WithScene(string path, Action<Scene> body)
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null,
            $"Scene asset '{path}' is missing.");

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
