using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CatJumpHeadCoverageTests
{
    [Test]
    public void SpatialPartition_CoversEntireCrossBoundaryTriangles_NotOnlyTheirVertices()
    {
        // Separate small point capsules could cover these vertices while leaving
        // the face centres uncovered. The production splitter must keep each
        // long face wholly inside at least one capsule.
        var vertices = new[] { new Vector3(-1,0,0), new Vector3(1,0,0), new Vector3(0,1,0),
            new Vector3(-1,0,.1f),new Vector3(1,0,.1f),new Vector3(0,1,.1f),new Vector3(0,0,-.1f) };
        var indices = new[] { 0,1,2,3,4,5,0,2,6,2,1,6 };
        var result = CatJumpHeadCoverageBuilder.Fit(vertices, indices);
        Assert.That(result.parts.Length, Is.InRange(2,CatJumpClearanceCatalog.MaxHeadProbes));
        Assert.That(CatJumpHeadCoverageBuilder.TriangleContainment(vertices,indices,result.parts),Is.GreaterThanOrEqualTo(.00399f));
        Assert.That(CatJumpHeadCoverageBuilder.TriangleContainment(vertices,indices,new[]{result.envelope}),Is.GreaterThanOrEqualTo(.00399f));
        // Any triangle touching an assigned head vertex remains in coverage,
        // including its boundary vertices assigned to another anatomical region.
        var selected=CatJumpHeadCoverageBuilder.HeadTriangles(indices,new[]{3,2,-1,2,2,2,3});
        Assert.That(selected.Length,Is.EqualTo(9));
        CollectionAssert.IsSubsetOf(new[]{0,1,2,6},selected);
    }
    [Test]
    public void InstalledTenBreedCatalog_All63SourceEndPhases_ContainsEveryHeadTriangleWithOriginalShell()
    {
        string setup=string.Join("|",EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive));
        var guarded=new[]{"Assets/Resources/Home/CatJumpClearanceCatalog.asset","Assets/Resources/Home/CatBodyGuardCatalog.asset",
            "Assets/Resources/Home/CatPawReachCatalog.asset","Assets/Scripts/Activities/CatJumpMotion.cs"}.ToDictionary(p=>p,File.ReadAllBytes);
        string report;
        try { report=CatJumpClearanceBuilder.MeasureHeadCoverage(); }
        finally
        {
            foreach(var pair in guarded)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),pair.Key);
            Assert.That(string.Join("|",EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive)),Is.EqualTo(setup));
        }
        var rows=report.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(r=>r.Split(',')).ToArray();
        Assert.That(rows.Length,Is.EqualTo(630));
        Assert.That(rows.Select(r=>r[0]).Distinct().Count(),Is.EqualTo(10));
        float N(string value)=>float.Parse(value,CultureInfo.InvariantCulture);
        foreach(var r in rows)
        {
            string id=r[0]+" phase="+r[1];
            Assert.That(int.Parse(r[2]),Is.GreaterThan(0),id);
            Assert.That(int.Parse(r[3]),Is.InRange(2,CatJumpClearanceCatalog.MaxHeadProbes),id);
            Assert.That(N(r[6]),Is.GreaterThanOrEqualTo(.00199f),id+" recomputed full-triangle shell");
            Assert.That(N(r[7]),Is.GreaterThanOrEqualTo(.00199f),id+" installed full-triangle shell; rebake jump-only catalog");
        }
        TestContext.WriteLine(report);
    }
}
