using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class CatJumpBodyCoverageTests
{
    [Test]
    public void AllFourRegions_KeepWholeBoundaryFacesAndIsolatedVerticesInsideTheirOwnConvexParts()
    {
        var vertices=new[]{new Vector3(-1,0,0),new Vector3(1,0,0),new Vector3(0,1,0),new Vector3(0,0,1),new Vector3(0,-1,0)};
        var triangles=new[]{0,1,2,1,2,3};var groups=new[]{0,1,2,3,0};
        string[] names={"pelvis","chest","neck","head"};
        for(int region=0;region<4;region++)
        {
            var selected=CatJumpSurfaceCoverageBuilder.RegionTriangles(triangles,groups,region);
            Assert.That(selected.Length,Is.GreaterThan(0));
            foreach(int v in Enumerable.Range(0,groups.Length).Where(v=>groups[v]==region))Assert.That(selected,Does.Contain(v));
            for(int t=0;t<triangles.Length;t+=3)
                if(Enumerable.Range(0,3).Any(k=>groups[triangles[t+k]]==region))
                    for(int k=0;k<3;k++)Assert.That(selected,Does.Contain(triangles[t+k]),"Boundary vertex removed");
            var result=CatJumpSurfaceCoverageBuilder.Fit(vertices,selected,names[region]);
            Assert.That(result.parts.Length,Is.InRange(1,CatJumpClearanceCatalog.MaxRegionProbes));
            Assert.That(CatJumpSurfaceCoverageBuilder.TriangleContainment(vertices,selected,result.parts),Is.GreaterThanOrEqualTo(.00399f));
            Assert.That(CatJumpSurfaceCoverageBuilder.TriangleContainment(vertices,selected,new[]{result.envelope}),Is.GreaterThanOrEqualTo(.00399f));
            Assert.That(result.parts.All(p=>p.region.StartsWith(names[region]+".",StringComparison.Ordinal)),Is.True);
        }
    }
    [Test]
    public void InstalledTenBreeds_All63EndPhases_AllFourRegions_ContainEveryAssignedSourceTriangle()
    {
        string setup=string.Join("|",EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive));
        var guard=new[]{"Assets/Resources/Home/CatJumpClearanceCatalog.asset","Assets/Resources/Home/CatBodyGuardCatalog.asset",
            "Assets/Resources/Home/CatPawReachCatalog.asset","Assets/Scripts/Activities/CatJumpMotion.cs"}.ToDictionary(p=>p,File.ReadAllBytes);
        string report;
        try{report=CatJumpClearanceBuilder.MeasureBodyTriangleCoverage();}
        finally
        {
            foreach(var pair in guard)CollectionAssert.AreEqual(pair.Value,File.ReadAllBytes(pair.Key),pair.Key);
            Assert.That(string.Join("|",EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive)),Is.EqualTo(setup));
        }
        var rows=report.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(r=>r.Split(',')).ToArray();
        Assert.That(rows.Length,Is.EqualTo(630*4));
        Assert.That(rows.Select(r=>r[0]).Distinct().Count(),Is.EqualTo(10));
        foreach(var region in new[]{"pelvis","chest","neck","head"})Assert.That(rows.Count(r=>r[2]==region),Is.EqualTo(630));
        foreach(var r in rows)
        {
            string id=r[0]+"/"+r[1]+"/"+r[2];
            Assert.That(int.Parse(r[3]),Is.GreaterThan(0),id);
            Assert.That(int.Parse(r[4]),Is.InRange(1,CatJumpClearanceCatalog.MaxRegionProbes),id);
            Assert.That(float.Parse(r[7],CultureInfo.InvariantCulture),Is.GreaterThanOrEqualTo(.00199f),id+" recomputed convex face cover");
            Assert.That(float.Parse(r[8],CultureInfo.InvariantCulture),Is.GreaterThanOrEqualTo(.00199f),id+" installed convex face cover; bake required");
        }
        TestContext.WriteLine(report);
    }
}
