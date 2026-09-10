using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ArcadeMiniGameArtTests
{
    [TestCase("ArcadeStreet", 2000)]
    [TestCase("ArcadeFacade0", 11000)]
    [TestCase("ArcadeFacade1", 11000)]
    [TestCase("ArcadeFacade2", 11000)]
    [TestCase("ArcadeFestivalArch", 4000)]
    [TestCase("ArcadePalm", 800)]
    [TestCase("ArcadeCloudBank", 3500)]
    [TestCase("ArcadeGardenArena", 35000)]
    public void WorldMeshes_StayWithinMobileBudgetWithOneMaterial(string name, int maximumTriangles)
    {
        var model = Load(name);
        long triangles = 0;
        foreach (var mesh in model.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh))
        {
            Assert.That(mesh.subMeshCount, Is.EqualTo(1), name + " must share the tiny palette");
            triangles += mesh.GetIndexCount(0) / 3;
        }
        Assert.That(triangles, Is.GreaterThan(100));
        Assert.That(triangles, Is.LessThanOrEqualTo(maximumTriangles), name);
    }

    [Test]
    public void RunnerStreet_KeepsTheExistingContactPlaneAcrossAllThreeLanes()
    {
        var allPoints = Points(Load("ArcadeStreet")).ToArray();
        var points = allPoints.Where(v => Mathf.Abs(v.x) < 2.30f).ToArray();
        Assert.That(points.Length, Is.GreaterThan(0));
        Assert.That(points.Max(v => v.y), Is.EqualTo(.033f).Within(.0006f));
        Assert.That(allPoints.Min(v => v.z), Is.EqualTo(-4f).Within(.001f));
        Assert.That(allPoints.Max(v => v.z), Is.EqualTo(4f).Within(.001f));
    }

    [Test]
    public void CatchCourt_HasNoDecorationInsideTheEightBySixMetrePlayArea()
    {
        var points = Points(Load("ArcadeGardenArena"))
            .Where(v => Mathf.Abs(v.x) < 3.99f && Mathf.Abs(v.z) < 2.99f).ToArray();
        Assert.That(points.Length, Is.GreaterThan(0));
        Assert.That(points.Max(v => v.y), Is.LessThan(.04f),
            "Printed court graphics may be flush; seats, planters and toys must remain outside the playable field.");
    }

    private static GameObject Load(string name)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(
            ArcadeMiniGameArtBuilder.AssetRoot + "/Models/" + name + ".fbx");
        Assert.That(model, Is.Not.Null, name);
        return model;
    }

    private static IEnumerable<Vector3> Points(GameObject model)
    {
        foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            foreach (var vertex in filter.sharedMesh.vertices)
                yield return model.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
    }
}
