using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class FixedRoomInteractionTests
{
    [Test]
    public void Garden_DaisyExitConnectsDiagonallyToTheRestOfTheLawn()
    {
        var states = new Dictionary<GameObject, bool>();
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(HomeRoomService.GardenScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        try
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                foreach (var root in UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).GetRootGameObjects())
                    if (root.scene != scene) { states[root] = root.activeSelf; root.SetActive(false); }
            if (opened) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(HomeRoomService.GardenScenePath,
                UnityEditor.SceneManagement.OpenSceneMode.Additive);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var display in root.GetComponentsInChildren<StoreProductDisplay>(true))
                {
                    var visual = new SerializedObject(display).FindProperty("visualRoot").objectReferenceValue as GameObject;
                    if (visual == null) continue;
                    states[visual] = visual.activeSelf; visual.SetActive(true);
                }
            Physics.SyncTransforms();
            var from = new Vector3(2.27f, 0f, 1.03f);
            Assert.That(CatActivityMotion.TryFloorPath(from, new Vector3(1.85f, 0f, -2.57f), out var path), Is.True,
                "The swept capsule fits the diagonal gap beside the grill; four-direction-only navigation strands the cat.");
            foreach (var point in path)
            {
                Assert.That(CatActivityMotion.ClearSegment(from, point), Is.True);
                from = point;
            }
        }
        finally
        {
            foreach (var pair in states) if (pair.Key != null) pair.Key.SetActive(pair.Value);
            if (opened && scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            if (active.IsValid() && active.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            Physics.SyncTransforms();
        }
    }

    [Test]
    public void LoftRecordPlayer_HasItsOwnFloorSpaceBesideTheDesk()
    {
        StoreCatalogAsset record = default, desk = default;
        foreach (var d in StoreCatalogAssets.PlaceableProducts)
        {
            if (d.ProductId == HomeStoreService.LoftRecordPlayerId) record = d;
            if (d.ProductId == HomeStoreService.LoftStudyDeskId) desk = d;
        }
        Assert.That(record.ProductId, Is.EqualTo(HomeStoreService.LoftRecordPlayerId));
        Assert.That(desk.ProductId, Is.EqualTo(HomeStoreService.LoftStudyDeskId));
        float gap = Mathf.Abs(record.DefaultPosition.x - desk.DefaultPosition.x) - (record.Footprint.x + desk.Footprint.x) * .5f;
        Assert.That(gap, Is.GreaterThan(.3f), "Both purchased objects must have separate reserved space.");
    }

    [Test]
    public void EveryBreed_HasBakedContactVerticesWithoutEnablingMeshReadWrite()
    {
        var catalog = CatBreedCatalog.Load();
        Assert.That(catalog.Count, Is.EqualTo(10));
        foreach (var entry in catalog.Entries)
        {
            var mesh = entry.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
            Assert.That(mesh.isReadable, Is.False, entry.Id + " must keep the vendor memory settings.");
            Assert.That(entry.ContactVertexIndices.Count, Is.GreaterThan(100), entry.Id);
            Assert.That(entry.ContactVertexIndices.Count, Is.LessThan(mesh.vertexCount), entry.Id + " must exclude the tail.");
            int previous = -1;
            foreach (int index in entry.ContactVertexIndices)
            {
                Assert.That(index, Is.GreaterThan(previous).And.LessThan(mesh.vertexCount), entry.Id);
                previous = index;
            }
        }
    }

    [Test]
    public void EveryCollectionPrefab_HasAnOwnedRoutineAndFloorEntry()
    {
        var ids = new HashSet<string>();
        foreach (var definition in StoreCatalogAssets.PlaceableProducts)
        {
            bool included = false;
            foreach (var room in HomeRoomService.Rooms)
                included |= HomeStoreService.IsProductInRoomCollection(room.Id, definition.ProductId);
            if (!included) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Art/StoreProducts/Prefabs/" + definition.PrefabName + ".prefab");
            var activity = prefab.GetComponent<CatActivity>();
            if(definition.ProductId==HomeStoreService.GameConsoleId||definition.ProductId==HomeStoreService.StereoId||definition.ProductId==HomeStoreService.TvUnitId)
            {
                Assert.That(activity,Is.Null,"This purchased product is decor, with no cat action.");
                Assert.That(prefab.GetComponent<StoreProductDisplay>(),Is.Not.Null,"Ownership must still control decor visibility.");
                ids.Add(definition.ProductId);continue;
            }
            Assert.That(activity, Is.Not.Null, definition.ProductId);
            Assert.That(prefab.GetComponent<RoomProductFeedback>(), Is.Not.Null, definition.ProductId);
            Assert.That(activity.StoreProductId, Is.EqualTo(definition.ProductId));
            Assert.That(activity.RoutineEntryPoint, Is.Not.Null, definition.ProductId);
            foreach (var box in prefab.GetComponentsInChildren<BoxCollider>(true))
                Assert.That(box.isTrigger, Is.True, definition.ProductId + " must not block its whole reserved footprint.");
            ids.Add(definition.ProductId);
        }
        Assert.That(ids.Count, Is.EqualTo(80));
    }

    [TestCase("GardenPergola", 0f, -.35f)]
    [TestCase("PatioParasol", .68f, 0f)]
    public void OpenStructures_HaveWalkableSpaceUnderTheirCanopy(string name, float x, float z)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/" + name + ".prefab");
        var instance = Object.Instantiate(prefab, new Vector3(100f, 0f, 100f), Quaternion.identity);
        try
        {
            foreach (var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.SetActive(true);
            Physics.SyncTransforms();
            Assert.That(instance.GetComponentsInChildren<MeshCollider>(true).Length, Is.GreaterThan(0));
            Assert.That(CatActivityMotion.IsFloorClear(instance.transform.position + new Vector3(x, 0f, z)), Is.True);
            if (name == "PatioParasol")
                Assert.That(CatActivityMotion.IsFloorClear(instance.transform.position), Is.False,
                    "The actual pole/base must still collide.");
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [Test]
    public void RoomBoundaryMargin_UsesPhysicsInsteadOfBreedTailBounds()
    {
        var root = new GameObject("Boundary test cat");
        try
        {
            root.transform.localScale = Vector3.one * .5f;
            var controller = root.AddComponent<CharacterController>(); controller.radius = .5f;
            var cat = root.AddComponent<CatMovement>();
            var giantTail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            giantTail.transform.SetParent(root.transform, false);
            giantTail.transform.localScale = new Vector3(4f, 1f, 8f);
            typeof(CatMovement).GetField("characterController", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(cat, controller);
            var method = typeof(CatMovement).GetMethod("ResolvePhysicalFootprintRadius", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That((float)method.Invoke(cat, null), Is.EqualTo(.25f).Within(.001f));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void SharedController_ContainsEveryFurniturePose()
    {
        var controller = CatBreedCatalog.Load().GameplayController as UnityEditor.Animations.AnimatorController;
        var states = new HashSet<string>();
        foreach (var state in controller.layers[0].stateMachine.states) states.Add(state.state.name);
        foreach (CatActivityPose pose in System.Enum.GetValues(typeof(CatActivityPose)))
            Assert.That(states.Contains(CatActivityAnimation.StateFor(pose)), Is.True, pose.ToString());
    }

    [Test]
    public void RaisedJump_ClearsTheCounterBeforeLanding()
    {
        var from = new Vector3(0f, 0f, -.8f);
        var to = new Vector3(0f, .81f, -.1f);
        Assert.That(CatActivityMotion.JumpPosition(from, to, .5f).y, Is.GreaterThan(.95f));
        Assert.That(CatActivityMotion.JumpPosition(from, to, 0f), Is.EqualTo(from));
        Assert.That(Vector3.Distance(CatActivityMotion.JumpPosition(from, to, 1f), to), Is.LessThan(.0001f));
    }
}
