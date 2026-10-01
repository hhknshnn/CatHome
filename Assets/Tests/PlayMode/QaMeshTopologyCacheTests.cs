#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class QaMeshTopologyCacheTests
{
    [TestCase(IndexFormat.UInt16)]
    [TestCase(IndexFormat.UInt32)]
    public void Snapshot_MatchesOriginalVerticesAndTriangles_WithSubmeshesAndBaseVertex(IndexFormat format)
    {
        var mesh = new Mesh { name = "QA original topology", indexFormat = format };
        var cache = new QaMeshTopologyCache();
        try
        {
            mesh.vertices = new[] {
                new Vector3(-2, 0, 0), new Vector3(-1, 0, 0), new Vector3(-2, 1, 0),
                new Vector3(1, 0, 0), new Vector3(2, 0, 0), new Vector3(1, 1, 0) };
            mesh.subMeshCount = 2;
            mesh.SetTriangles(new[] { 0, 1, 2 }, 0, false, 0);
            mesh.SetTriangles(new[] { 0, 2, 1 }, 1, false, 3);
            Vector3[] expectedVertices = mesh.vertices;
            int[] expectedTriangles = mesh.triangles;
            var actual = cache.Get(mesh);
            Assert.That(actual.vertices, Is.EqualTo(expectedVertices));
            Assert.That(actual.triangles, Is.EqualTo(expectedTriangles));
            Assert.That(actual.triangles, Is.EqualTo(new[] { 0, 1, 2, 3, 5, 4 }));
            Assert.That(cache.Get(mesh), Is.SameAs(actual), "Repeated skin samples reuse only managed data.");
            cache.Clear();
            Assert.That(cache.Count, Is.Zero);
            Assert.That(cache.Get(mesh).triangles, Is.EqualTo(expectedTriangles), "Clear releases the cache without changing the source.");
            Assert.That(mesh.vertices, Is.EqualTo(expectedVertices));
            Assert.That(mesh.triangles, Is.EqualTo(expectedTriangles));
        }
        finally { cache.Clear(); Object.DestroyImmediate(mesh); }
    }

    [Test] public void ImportedMainFoodBowl_IsMeasuredWithoutChangingReadabilityOrTheAsset()
    {
        const string path = "Assets/Art/PremiumFurniture/Models/MainFoodBowl_Premium.fbx";
        var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
        Assert.That(imported, Is.Not.Empty);
        var importer = (ModelImporter)AssetImporter.GetAtPath(path);
        bool importerReadable = importer.isReadable;
        byte[] assetBytes = System.IO.File.ReadAllBytes(path);
        byte[] metadataBytes = System.IO.File.ReadAllBytes(path + ".meta");
        var cache = new QaMeshTopologyCache();
        try
        {
            int unreadable = 0;
            foreach (var mesh in imported)
            {
                bool readable = mesh.isReadable; if (!readable) unreadable++;
                var first = cache.Get(mesh);
                Assert.That(first.vertices.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(first.triangles, Is.Not.Empty);
                Assert.That(first.triangles.Length % 3, Is.Zero);
                Assert.That(first.triangles.All(i => i >= 0 && i < first.vertices.Length), Is.True);
                Assert.That(cache.Get(mesh), Is.SameAs(first));
                Assert.That(mesh.isReadable, Is.EqualTo(readable));
            }
            Assert.That(unreadable, Is.GreaterThan(0), "Exercise the exact unreadable bowl that caused the native diagnostic failure.");
        }
        finally { cache.Clear(); }
        Assert.That(importer.isReadable, Is.EqualTo(importerReadable));
        Assert.That(System.IO.File.ReadAllBytes(path), Is.EqualTo(assetBytes));
        Assert.That(System.IO.File.ReadAllBytes(path + ".meta"), Is.EqualTo(metadataBytes));
    }
}
#endif
