#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CatPawSurfaceTopologyTests
{
    const string Output="Docs/QA/INTERACTION_POLISH_2026-09-16/paw-surface-stage/triangle-seams";
    [Serializable] sealed class Bindings { public CatPawReachCatalog.PawVertex[] left,right; }
    [Serializable] sealed class Baseline { public FileHash[] bodies; public BindingHash[] bindings; }
    [Serializable] sealed class FileHash { public string path,hash; }
    [Serializable] sealed class BindingHash { public string breed,hash; }
    static string Hash(byte[] bytes){using(var h=SHA256.Create())return string.Concat(h.ComputeHash(bytes).Select(b=>b.ToString("x2")));}
    static string BindingDigest(CatPawReachCatalog.Entry e)=>Hash(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Bindings{left=e.leftPaw,right=e.rightPaw})));
    static CatPawReachCatalog.Entry Load(string breed,bool surface)
    {
        string resource=surface?CatPawReachCatalog.SurfaceResourceName(breed,CatActivityPose.Scratch):CatPawReachCatalog.BodyResourceName(breed);
        var catalog=AssetDatabase.LoadAssetAtPath<CatPawReachCatalog>("Assets/Resources/"+resource+".asset");
        Assert.That(catalog,Is.Not.Null,resource);return catalog.Find(breed,CatActivityPose.Scratch);
    }
    // Invoke once AFTER importing this source, BEFORE BuildSurfaceGeometry().
    // This writes a QA proof only; no project asset is changed.
    public static string CapturePreBakeBaseline()
    {
        var breeds=CatBreedCatalog.Load().Entries.ToArray();
        var paths=breeds.Select(b=>"Assets/Resources/"+CatPawReachCatalog.BodyResourceName(b.Id)+".asset")
            .Concat(new[]{"Assets/Resources/Home/CatPawReachCatalog.asset"}).ToArray();
        var data=new Baseline{bodies=paths.Select(p=>new FileHash{path=p,hash=Hash(File.ReadAllBytes(p))}).ToArray(),
            bindings=breeds.Select(b=>new BindingHash{breed=b.Id,hash=BindingDigest(Load(b.Id,true))}).ToArray()};
        Directory.CreateDirectory(Output);string path=Output+"/pre-bake-baseline.json";
        File.WriteAllText(path,JsonUtility.ToJson(data,true));return path;
    }
    [Test] public void SurfaceRefinement_PreservesBodyBytesAndOriginalSkinBindings()
    {
        string path=Output+"/pre-bake-baseline.json";Assert.That(File.Exists(path),Is.True,"Capture pre-bake baseline before the reviewed surface-only bake");
        var baseline=JsonUtility.FromJson<Baseline>(File.ReadAllText(path));
        Assert.That(baseline.bodies.Length,Is.EqualTo(11));Assert.That(baseline.bindings.Length,Is.EqualTo(10));
        foreach(var file in baseline.bodies)Assert.That(Hash(File.ReadAllBytes(file.path)),Is.EqualTo(file.hash),file.path);
        foreach(var item in baseline.bindings)Assert.That(BindingDigest(Load(item.breed,true)),Is.EqualTo(item.hash),item.breed+" source/bind weights changed");
    }
    [Test] public void SplitRegions_EncloseCompleteTriangles_AndMayShareBoundaryVertices()
    {
        var defs=Enumerable.Range(0,24).Select(i=>new CatPawReachCatalog.PawVertex{vertexIndex=i,
            influences=new[]{new CatPawReachCatalog.PawInfluence{bonePath="paw",weight=1,bindPosition=new Vector3(i*.01f,(i%3)*.003f,(i%2)*.005f)}}}).ToArray();
        var triangles=new[]{0,12,23,3,14,21,6,7,8};
        var regions=CatPawSurfaceRegions.Build(defs,triangles);
        Assert.That(regions.Length,Is.InRange(2,32));
        Assert.That(regions.Sum(r=>r.Length),Is.GreaterThan(defs.Length),"A crossing face must bring neighbours into one region");
        AssertCoverage(defs,triangles,regions,defs.Select(d=>d.influences[0].bindPosition).ToArray());
        Assert.That(CatPawSurfaceRegions.Build(defs,new[]{0,1,24}),Is.Empty,"Invalid topology must fail closed");
        Assert.That(CatPawSurfaceRegions.Build(defs,Array.Empty<int>()),Is.Empty,"Missing topology must fail closed");
    }
    [Test] public void TenSurfaceShards_KeepLegacyBodyPhases_AndCoverAllInternalSourceFaces()
    {
        var extra=new[]{.31f,.33f,.41f,.43f,.51f,.53f,.61f,.63f};
        var report=new List<string>{"breed,side,vertices,triangles,outerBoundaryTriangles,regions,regionMemberships,surfaceSamples,bodySamples"};
        foreach(var breed in CatBreedCatalog.Load().Entries)
        {
            var body=Load(breed.Id,false);var surface=Load(breed.Id,true);
            Assert.That(body.samples.Length,Is.EqualTo(25),breed.Id+" legacy Body phases");
            Assert.That(body.leftPaw,Is.Empty);Assert.That(body.rightPaw,Is.Empty);
            Assert.That(surface.surfaceGeometryVersion,Is.EqualTo(CatPawReachCatalog.SurfaceGeometryVersion));
            Assert.That(surface.samples.Length,Is.EqualTo(CatPawReachCatalog.SurfaceSampleCount));
            foreach(float phase in extra)Assert.That(surface.samples.Any(s=>Mathf.Abs(s.phase-phase)<.000001f),Is.True,breed.Id+" added phase "+phase);
            for(int i=1;i<surface.samples.Length;i++)Assert.That(surface.samples[i].phase,Is.GreaterThan(surface.samples[i-1].phase));
            var source=breed.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
            var original=source.triangles;
            for(int side=0;side<2;side++)
            {
                var definitions=side==0?surface.leftPaw:surface.rightPaw;
                var triangles=side==0?surface.leftPawTriangles:surface.rightPawTriangles;
                var map=definitions.Select((d,i)=>new{d.vertexIndex,index=i}).ToDictionary(p=>p.vertexIndex,p=>p.index);
                var expected=new List<int>();int boundary=0;
                for(int t=0;t<original.Length;t+=3)
                {
                    bool a=map.TryGetValue(original[t],out int x),b=map.TryGetValue(original[t+1],out int y),c=map.TryGetValue(original[t+2],out int z);
                    if(a&&b&&c){expected.Add(x);expected.Add(y);expected.Add(z);}else if(a||b||c)boundary++;
                }
                Assert.That(triangles,Is.EqualTo(expected),breed.Id+" complete original paw faces");
                var regions=CatPawSurfaceRegions.Build(definitions,triangles);Assert.That(regions.Length,Is.InRange(1,32));
                foreach(var sample in surface.samples)AssertCoverage(definitions,triangles,regions,side==0?sample.leftPaw:sample.rightPaw);
                report.Add(string.Join(",",breed.Id,side,definitions.Length,triangles.Length/3,boundary,regions.Length,regions.Sum(r=>r.Length),surface.samples.Length,body.samples.Length));
            }
        }
        Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/triangle-coverage.csv",report);
    }
    static void AssertCoverage(CatPawReachCatalog.PawVertex[] definitions,int[] triangles,int[][] regions,Vector3[] points)
    {
        var membership=regions.Select(r=>new HashSet<int>(r)).ToArray();
        for(int v=0;v<definitions.Length;v++)Assert.That(membership.Any(r=>r.Contains(v)),Is.True,"Missing measured vertex "+v);
        Vector3 normal=new Vector3(.986856f,-.098621f,.128020f).normalized;
        var boxes=regions.Select(r=>CatPawSurfaceRegions.FitBox(points,r,normal)).ToArray();
        for(int t=0;t<triangles.Length;t+=3)
        {
            int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
            int region=Array.FindIndex(membership,r=>r.Contains(a)&&r.Contains(b)&&r.Contains(c));
            Assert.That(region,Is.GreaterThanOrEqualTo(0),"The complete triangle must belong to one convex envelope");
            var box=boxes[region];Quaternion inverse=Quaternion.Inverse(box.rotation);
            foreach(int v in new[]{a,b,c})
            {
                Vector3 p=inverse*(points[v]-box.centre);
                Assert.That(Mathf.Max(Mathf.Abs(p.x)-box.halfExtents.x,Mathf.Max(Mathf.Abs(p.y)-box.halfExtents.y,Mathf.Abs(p.z)-box.halfExtents.z)),Is.LessThanOrEqualTo(.000002f));
            }
        }
    }
}
#endif
