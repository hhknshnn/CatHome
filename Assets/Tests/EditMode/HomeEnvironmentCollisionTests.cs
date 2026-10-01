using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class HomeEnvironmentCollisionTests
{
    // Acceptance inventory, independent of the migration's own role selection.
    // Removing an authoring call or a migration profile must not silently reduce coverage.
    static readonly Dictionary<string, int> ExpectedParts = new Dictionary<string, int>
    {
        { HomeRoomService.LivingRoomScenePath, 63 }, { HomeRoomService.BathroomScenePath, 17 },
        { HomeRoomService.KitchenScenePath, 18 }, { HomeRoomService.BedroomScenePath, 18 },
        { HomeRoomService.GardenScenePath, 42 }, { HomeRoomService.BalconyScenePath, 88 },
        { HomeRoomService.PatioScenePath, 30 }, { HomeRoomService.SecondFloorScenePath, 25 }
    };

    static readonly Dictionary<string, string> NonblockingExamples = new Dictionary<string, string>
    {
        { HomeRoomService.LivingRoomScenePath, "05 Presentation/PremiumWorldPresentation/CandyRoomArchitecture/RoundedWainscot/Back_ChairRailGold" },
        { HomeRoomService.BathroomScenePath, "01 Environment/Glossy Floor Tiles/Tile_01_01" },
        { HomeRoomService.KitchenScenePath, "01 Environment/Sunshine Checker Floor/Tile_01_01" },
        { HomeRoomService.BedroomScenePath, "01 Environment/Dreamy Wood Floor/Plank_01" },
        { HomeRoomService.GardenScenePath, "01 Environment/Sunny Garden Fence/LeftFence/Hedge" },
        { HomeRoomService.BalconyScenePath, "01 Environment/Deck Greens/PlanterPot_1/Foliage" },
        { HomeRoomService.PatioScenePath, "01 Environment/Patio Low Wall/LeftWall_Stone/Urn_1" },
        { HomeRoomService.SecondFloorScenePath, "01 Environment/Loft Ceiling/Beam_1" }
    };

    [Test]
    public void EightPersistedRooms_HaveCompleteExactCollision_AndMigrationIsANoOp()
    {
        Assert.That(HomeRoomService.Rooms.Count, Is.EqualTo(ExpectedParts.Count));
        var originalSetup = SceneSetupSignature();
        foreach (var room in HomeRoomService.Rooms)
        {
            byte[] originalBytes = File.ReadAllBytes(room.ScenePath);
            // A preview loads the saved scene even when its live editor counterpart
            // is open. Apply never touches the user's loaded scene or its dirty state.
            var scene = EditorSceneManager.OpenPreviewScene(room.ScenePath);
            try
            {
                Assert.That(scene.path, Is.EqualTo(room.ScenePath));
                bool dirty = scene.isDirty;
                var before = ColliderState(scene);
                var inspect = HomeEnvironmentCollisionBuilder.Inspect(scene);
                Assert.That(inspect.inspected, Is.EqualTo(ExpectedParts[room.ScenePath]), room.SceneName);
                Assert.That(inspect.changes, Is.Zero, inspect.ToString());
                Assert.That(inspect.entries, Is.Empty, inspect.ToString());
                Assert.That(HomeEnvironmentCollisionBuilder.Apply(scene).changes, Is.Zero, room.SceneName);
                Assert.That(HomeEnvironmentCollisionBuilder.Apply(scene).changes, Is.Zero, "Repeated apply: " + room.SceneName);
                CollectionAssert.AreEqual(before, ColliderState(scene), room.SceneName + " collider state changed");
                Assert.That(scene.isDirty, Is.EqualTo(dirty), "No-op migration dirtied " + room.SceneName);
                AssertExactArchitecturalShapes(scene);
                var decoration = Find(scene, NonblockingExamples[room.ScenePath]);
                Assert.That(decoration, Is.Not.Null, NonblockingExamples[room.ScenePath]);
                Assert.That(decoration.GetComponentsInChildren<Collider>(true), Is.Empty,
                    "Visual floor/foliage/ornament must remain nonblocking: " + decoration.name);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(room.ScenePath), "Saved scene changed: " + room.SceneName);
        }
        CollectionAssert.AreEqual(originalSetup, SceneSetupSignature(), "Loaded editor scenes/dirty flags must be preserved.");
    }

    [Test]
    public void FutureRoomAuthoring_KeepsLocalPrimitiveShape_UnderRotatedNonuniformParents()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var host = new GameObject("Future room architecture");
        SceneManager.MoveGameObjectToScene(host, scene);
        try
        {
            host.transform.SetPositionAndRotation(new Vector3(4, 2, -3), Quaternion.Euler(0, 37, 0));
            host.transform.localScale = new Vector3(1.7f, .8f, .6f);
            foreach (HomeEnvironmentCollisionBuilder.SolidRole role in Enum.GetValues(typeof(HomeEnvironmentCollisionBuilder.SolidRole)))
            {
                Vector3 localPosition = new Vector3(.6f, .25f, -.7f), localScale = new Vector3(2.1f, .5f, .13f);
                var part = HomeEnvironmentCollisionBuilder.CreateSolidBlock(role.ToString(), host.transform,
                    localPosition, localScale, null, role);
                var box = part.GetComponent<BoxCollider>();
                var mesh = part.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(part.transform.localPosition, Is.EqualTo(localPosition));
                Assert.That(part.transform.localScale, Is.EqualTo(localScale));
                Assert.That(box.center, Is.EqualTo(mesh.bounds.center));
                Assert.That(box.size, Is.EqualTo(mesh.bounds.size), "World aggregate bounds must not inflate a local collider.");
                Assert.That(box.enabled && !box.isTrigger, Is.True);
                Assert.That(HomeEnvironmentCollisionBuilder.EnsureSolidBox(part, role), Is.False);
                Assert.That(part.GetComponents<Collider>().Length, Is.EqualTo(1));
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [TestCase("GardenPergola")]
    [TestCase("PatioParasol")]
    public void EnvironmentAuthoring_CannotReplaceCatalogOpeningsWithSolidBoxes(string prefabName)
    {
        string path = "Assets/Art/StoreProducts/Prefabs/" + prefabName + ".prefab";
        byte[] originalBytes = File.ReadAllBytes(path);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Assert.That(prefab, Is.Not.Null);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            var boxes = instance.GetComponentsInChildren<BoxCollider>(true);
            Assert.That(boxes, Is.Not.Empty);
            Assert.That(boxes.All(box => box.isTrigger), Is.True, "Catalog reservations must keep real openings walkable.");
            var meshes = instance.GetComponentsInChildren<MeshCollider>(true);
            Assert.That(meshes, Is.Not.Empty);
            Assert.That(meshes.All(c => c.sharedMesh != null && c.enabled && !c.isTrigger && !c.convex), Is.True);
            var before = ColliderState(scene);
            Assert.Throws<ArgumentException>(() => HomeEnvironmentCollisionBuilder.EnsureSolidBox(
                boxes[0].gameObject, HomeEnvironmentCollisionBuilder.SolidRole.Wall));
            CollectionAssert.AreEqual(before, ColliderState(scene), "Rejected environment policy must leave the catalog intact.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(path));
    }

    static void AssertExactArchitecturalShapes(Scene scene)
    {
        var environment = scene.GetRootGameObjects().Single(root => root.name == "01 Environment").transform;
        var livingCladding = scene.path == HomeRoomService.LivingRoomScenePath
            ? Find(scene, "05 Presentation/PremiumWorldPresentation/CandyRoomArchitecture/RoundedWainscot") : null;
        foreach (var filter in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)))
        {
            if (!filter.transform.IsChildOf(environment) &&
                (livingCladding == null || !filter.transform.IsChildOf(livingCladding))) continue;
            if (filter.sharedMesh == null || filter.GetComponentInParent<HomeProductPlacement>() != null ||
                filter.GetComponentInParent<StoreProductDisplay>() != null) continue;
            var box = filter.GetComponent<BoxCollider>();
            if (box != null && box.enabled && !box.isTrigger && filter.sharedMesh.name == "Cube")
            {
                Assert.That(Vector3.Distance(box.center, filter.sharedMesh.bounds.center), Is.LessThan(.00001f), filter.name);
                Assert.That(Vector3.Distance(box.size, filter.sharedMesh.bounds.size), Is.LessThan(.00001f), filter.name);
            }
            if (AssetDatabase.GetAssetPath(filter.sharedMesh) != "Assets/Art/RoomShellPolish/Models/RoomWallPanel_Premium.fbx") continue;
            var mesh = filter.GetComponent<MeshCollider>();
            Assert.That(mesh, Is.Not.Null, "Real wall panel needs its own collider: " + filter.name);
            Assert.That(mesh.sharedMesh, Is.SameAs(filter.sharedMesh));
            Assert.That(mesh.enabled && !mesh.isTrigger && !mesh.convex, Is.True);
            Assert.That(filter.GetComponent<Rigidbody>(), Is.Null, "Cladding stays static.");
        }
    }

    static Transform Find(Scene scene, string path)
    {
        int slash = path.IndexOf('/');
        var root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == path.Substring(0, slash));
        return root != null ? root.transform.Find(path.Substring(slash + 1)) : null;
    }
    static string[] ColliderState(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<Collider>(true))
        .Select(c => c.GetEntityId() + " " + EditorJsonUtility.ToJson(c)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
    static string[] SceneSetupSignature() => EditorSceneManager.GetSceneManagerSetup()
        .Select(s => s.path + "|" + s.isLoaded + "|" + s.isActive + "|" + SceneManager.GetSceneByPath(s.path).isDirty).ToArray();
}
