using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class FixedFurnitureMeshCollisionTests
{
    [Test] public void EveryPlacedSolidMesh_HasOriginalContactGeometry()
    {
        int checkedSolids=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Art/StoreProducts/Prefabs"}))
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var solid in prefab.GetComponentsInChildren<MeshCollider>(true))
            {
                if(!solid.enabled||solid.isTrigger||solid.sharedMesh==null)continue;
                Assert.That(CatMeshContactSurface.HasGeometry(solid.sharedMesh),Is.True,prefab.name+"/"+solid.name);
                checkedSolids++;
            }
        }
        Assert.That(checkedSolids,Is.GreaterThan(30));
    }

    [Test] public void Validator_AcceptsExactSolids_RejectsMissingProtectionAndSolidPickBoxes()
    {
        var original = File.ReadAllBytes(HomeRoomService.LivingRoomScenePath); var setup = Setup();
        var scene = EditorSceneManager.OpenPreviewScene(HomeRoomService.LivingRoomScenePath);
        try
        {
            var root = HomeFixedFurnitureCollisionBuilder.FixedModels(scene)[0];
            var validate = typeof(LevelContentValidator).GetMethod("RequireSolidFurniture",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(validate, Is.Not.Null);
            var valid = new LevelValidationReport();
            validate.Invoke(null, new object[] { scene, root.name, valid });
            Assert.That(valid.Errors, Is.Empty);
            var mesh = root.GetComponentsInChildren<MeshCollider>(true).First();
            mesh.enabled = false;
            var missing = new LevelValidationReport();
            validate.Invoke(null, new object[] { scene, root.name, missing });
            Assert.That(missing.Errors, Has.Some.Contains("exact-mesh solid"));
            mesh.enabled = true;
            root.GetComponentsInChildren<BoxCollider>(true).First().isTrigger = false;
            var blockedOpening = new LevelValidationReport();
            validate.Invoke(null, new object[] { scene, root.name, blockedOpening });
            Assert.That(blockedOpening.Errors, Has.Some.Contains("picking triggers"));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        CollectionAssert.AreEqual(original, File.ReadAllBytes(HomeRoomService.LivingRoomScenePath));
        CollectionAssert.AreEqual(setup, Setup());
    }

    static string[] Setup() => EditorSceneManager.GetSceneManagerSetup()
        .Select(s => s.path + "|" + s.isLoaded + "|" + s.isActive).ToArray();
    [Test] public void PersistedFixedPair_UsesExactMeshSolids_AndMigrationIsIdempotent()
    {
        var original = File.ReadAllBytes(HomeRoomService.LivingRoomScenePath); var setup = Setup();
        var scene = EditorSceneManager.OpenPreviewScene(HomeRoomService.LivingRoomScenePath);
        try
        {
            var models = HomeFixedFurnitureCollisionBuilder.FixedModels(scene);
            foreach (var root in models)
            {
                var boxes = root.GetComponentsInChildren<BoxCollider>(true);
                Assert.That(boxes, Is.Not.Empty, "Retain existing click/picking volumes");
                Assert.That(boxes.All(b => b.isTrigger), Is.True, "A reserved furniture box cannot be a solid");
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter.GetComponent<MeshCollider>();
                    Assert.That(mesh, Is.Not.Null); Assert.That(mesh.sharedMesh, Is.SameAs(filter.sharedMesh));
                    Assert.That(mesh.enabled && !mesh.convex && !mesh.isTrigger, Is.True);
                }
            }
            bool dirty = scene.isDirty;
            string[] before = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true))
                .Where(c => c != null).Select(EditorJsonUtility.ToJson).ToArray();
            Assert.That(HomeFixedFurnitureCollisionBuilder.Apply(scene), Is.Zero);
            Assert.That(HomeFixedFurnitureCollisionBuilder.Apply(scene), Is.Zero);
            CollectionAssert.AreEqual(before, scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Component>(true))
                .Where(c => c != null).Select(EditorJsonUtility.ToJson).ToArray());
            Assert.That(scene.isDirty, Is.EqualTo(dirty));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        CollectionAssert.AreEqual(original, File.ReadAllBytes(HomeRoomService.LivingRoomScenePath));
        CollectionAssert.AreEqual(setup, Setup());
    }

    [Test] public void ReauthoringFixedPair_ChangesOnlyColliderRoles_AndPreservesLocalMeshAndPickShape()
    {
        var original = File.ReadAllBytes(HomeRoomService.LivingRoomScenePath); var setup = Setup();
        var scene = EditorSceneManager.OpenPreviewScene(HomeRoomService.LivingRoomScenePath);
        try
        {
            var models = HomeFixedFurnitureCollisionBuilder.FixedModels(scene);
            var boxes = models.SelectMany(t => t.GetComponentsInChildren<BoxCollider>(true)).ToArray();
            var centres = boxes.Select(b => b.center).ToArray(); var sizes = boxes.Select(b => b.size).ToArray();
            var transforms = models.SelectMany(t => t.GetComponentsInChildren<Transform>(true)).ToArray();
            var matrices = transforms.Select(t => t.localToWorldMatrix).ToArray();
            var filters = models.SelectMany(t => t.GetComponentsInChildren<MeshFilter>(true)).ToArray();
            var meshes = filters.Select(f => f.sharedMesh).ToArray();
            foreach (var box in boxes) box.isTrigger = false; // Preview-only recreation of the old authored defect.
            Assert.That(HomeFixedFurnitureCollisionBuilder.Apply(scene), Is.EqualTo(boxes.Length));
            Assert.That(HomeFixedFurnitureCollisionBuilder.Apply(scene), Is.Zero);
            for (int i=0;i<boxes.Length;i++)
            { Assert.That(boxes[i].isTrigger, Is.True); Assert.That(boxes[i].center, Is.EqualTo(centres[i])); Assert.That(boxes[i].size, Is.EqualTo(sizes[i])); }
            for (int i=0;i<transforms.Length;i++) Assert.That(transforms[i].localToWorldMatrix, Is.EqualTo(matrices[i]));
            for (int i=0;i<filters.Length;i++) Assert.That(filters[i].sharedMesh, Is.SameAs(meshes[i]));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        CollectionAssert.AreEqual(original, File.ReadAllBytes(HomeRoomService.LivingRoomScenePath));
        CollectionAssert.AreEqual(setup, Setup());
    }
}
