using System;
using System.Collections.Generic;
using System.Linq;
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
    private static string[] RoomScenePaths => HomeRoomService.Rooms.Select(room => room.ScenePath).ToArray();

    private const float Tolerance = .001f;

    [Test]
    public void BalconyAndLoft_ForegroundArchitectureLeavesTheFrontViewOpen()
    {
        WithScene(HomeRoomService.BalconyScenePath, scene =>
        {
            var front = FindNamed(scene, "FrontRail");
            Assert.That(front, Is.Not.Null);
            Assert.That(front.gameObject.activeSelf, Is.True, "Only the rendering is cut away.");
            Assert.That(front.GetComponentsInChildren<Renderer>().All(r => !r.enabled), Is.True);
            foreach (string side in new[] { "LeftRail", "RightRail" })
                Assert.That(FindNamed(scene, side).GetComponentsInChildren<Renderer>().All(r => r.enabled), Is.True, side);
        });
        WithScene(HomeRoomService.SecondFloorScenePath, scene =>
        {
            var ceiling = FindNamed(scene, "Loft Ceiling");
            Assert.That(ceiling, Is.Not.Null);
            foreach (Transform part in ceiling)
                foreach (var renderer in part.GetComponentsInChildren<Renderer>())
                    Assert.That(renderer.enabled, Is.EqualTo(part.name.StartsWith("Pendant", StringComparison.Ordinal)), part.name);
        });
    }

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
    public void EveryRoomScene_KeepsItsApprovedCameraAndSharedCatScale()
    {
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
                if(catScale==null){catPosition=cat.position;catScale=cat.lossyScale;}
                AssertVector(cat.position,catPosition.Value,$"'{path}' cat position");
                AssertVector(cat.lossyScale,catScale.Value,$"'{path}' cat scale");
                Assert.That(camera.GetComponent<HomeWorldViewport>(),Is.Not.Null,"Every home camera reserves the navigation strip.");
                AssertVector(camera.transform.position,HomeRoomCameraProfile.Position,$"'{path}' front-centred camera");
                Assert.That(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(HomeRoomCameraProfile.Angles)),Is.LessThan(.05f),path);
                Assert.That(camera.fieldOfView,Is.EqualTo(HomeRoomCameraProfile.FieldOfView).Within(Tolerance),path);
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

    [Test]
    public void KitchenScene_StartsFurnitureFreeAndKeepsPremiumFixedArchitecture()
    {
        WithScene(HomeRoomService.KitchenScenePath, scene =>
        {
            Assert.That(FindNamed(scene, "Sunshine Checker Floor"), Is.Not.Null);
            Assert.That(FindNamed(scene, "Sunshine Floor Inlay"), Is.Not.Null);
            Assert.That(FindNamed(scene, "Kitchen Crown Moulding"), Is.Not.Null);
            Assert.That(FindNamed(scene, "Kitchen Sunrise Window"), Is.Not.Null);

            var displays = new List<StoreProductDisplay>();
            foreach (GameObject root in scene.GetRootGameObjects())
                displays.AddRange(root.GetComponentsInChildren<StoreProductDisplay>(true));

            Assert.That(displays.Count, Is.EqualTo(10));
            for (int i = 0; i < displays.Count; i++)
            {
                Renderer[] renderers =
                    displays[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                {
                    Assert.That(renderers[j].gameObject.activeInHierarchy, Is.False,
                        displays[i].ProductId + " must not furnish a fresh Kitchen.");
                }
            }
        });
    }

    [Test]
    public void EveryRoomScene_StartsWithoutCatalogFurnitureAndKeepsItsFixedPolish()
    {
        foreach (string path in RoomScenePaths)
        {
            WithScene(path, scene =>
            {
                Assert.That(
                    FindNamed(scene, HomeRoomShellVisualPolishBuilder.RootName),
                    Is.Not.Null,
                    $"'{path}' is missing the shared fixed-architecture polish.");

                var displays = new List<StoreProductDisplay>();
                foreach (GameObject root in scene.GetRootGameObjects())
                    displays.AddRange(root.GetComponentsInChildren<StoreProductDisplay>(true));

                Assert.That(displays.Count, Is.GreaterThanOrEqualTo(10),
                    $"'{path}' is missing its placeable catalog collection.");
                for (int i = 0; i < displays.Count; i++)
                {
                    Renderer[] renderers =
                        displays[i].GetComponentsInChildren<Renderer>(true);
                    for (int j = 0; j < renderers.Length; j++)
                    {
                        Assert.That(renderers[j].gameObject.activeInHierarchy, Is.False,
                            $"{displays[i].ProductId} must not furnish a fresh '{path}'.");
                    }
                }
            });
        }
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
