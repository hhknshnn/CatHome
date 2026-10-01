#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public sealed class CatJumpWeightedBodyTests
{
    const string Output="Docs/QA/INTERACTION_POLISH_2026-09-16/jump-weighted-triangles";
    [Test] public void AllTenBreeds_KeepOriginalWeightedBoundaryFaces_AndMissingDataUsesLegacyPolicy()
    {
        var catalog=CatBreedCatalog.Load();var jump=CatJumpClearanceCatalog.Load();int count=0;
        foreach(var breed in catalog.Entries)
        {
            var entry=jump.Find(breed.Id);Assert.That(entry.weightedBodyVersion,Is.EqualTo(CatJumpClearanceCatalog.WeightedBodyVersion));
            var skin=breed.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);var mesh=skin.sharedMesh;
            var weights=mesh.boneWeights;var bind=mesh.bindposes;var original=mesh.vertices;
            var groups=Enumerable.Repeat(-1,weights.Length).ToArray();foreach(int v in breed.ContactVertexIndices)groups[v]=CatJumpClearanceBuilder.ClassifyBodyRegion(weights[v],skin.bones);
            for(int r=0;r<4;r++)
            {
                var region=entry.bodySurface[r];var required=CatJumpSurfaceCoverageBuilder.RegionTriangles(mesh.triangles,groups,r);
                var actual=new HashSet<(int,int,int)>();
                for(int t=0;t<region.triangles.Length;t+=3)actual.Add((region.skin[region.triangles[t]].vertexIndex,region.skin[region.triangles[t+1]].vertexIndex,region.skin[region.triangles[t+2]].vertexIndex));
                for(int t=0;t<required.Length;t+=3)Assert.That(actual.Contains((required[t],required[t+1],required[t+2])),Is.True,breed.Id+" complete source face");
                var owned=new HashSet<(int,int,int)>();
                foreach(var part in region.parts)
                {
                    for(int t=0;t<part.triangles.Length;t+=3)
                    {
                        int a=part.triangles[t],b=part.triangles[t+1],c=part.triangles[t+2];
                        Assert.That(part.vertices.Contains(a)&&part.vertices.Contains(b)&&part.vertices.Contains(c),Is.True,"One complete face belongs to one broad part");owned.Add((a,b,c));
                    }
                }
                Assert.That(region.parts.Length,Is.InRange(1,32));
                for(int t=0;t<region.triangles.Length;t+=3)Assert.That(owned.Contains((region.triangles[t],region.triangles[t+1],region.triangles[t+2])),Is.True);
                foreach(var definition in region.skin)
                {
                    var w=weights[definition.vertexIndex];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] amounts={w.weight0,w.weight1,w.weight2,w.weight3};int at=0;
                    for(int k=0;k<4;k++)if(amounts[k]>0)
                    {var influence=definition.influences[at++];Assert.That(influence.weight,Is.EqualTo(amounts[k]));Assert.That(influence.bindPosition,Is.EqualTo(bind[ids[k]].MultiplyPoint3x4(original[definition.vertexIndex])));Assert.That(entry.bodyBones[influence.sourceBone].path,Is.EqualTo(influence.bonePath));}
                    Assert.That(at,Is.EqualTo(definition.influences.Length));
                }
            }
            foreach(var sample in entry.samples)Assert.That(CatJumpWeightedBody.HasData(entry,sample),Is.True);
            count++;
        }
        Assert.That(count,Is.EqualTo(10));
        Assert.That(CatJumpWeightedBody.HasData(new CatJumpClearanceCatalog.Entry(),new CatJumpClearanceCatalog.Sample()),Is.False);
        Assert.That(CatJumpWeightedBody.TryClear(null,new CatJumpClearanceCatalog.Entry(),new CatJumpClearanceCatalog.Sample(),Matrix4x4.identity,Vector3.zero,out bool clear),Is.False);
        Assert.That(clear,Is.False,"Missing data does not supply an approval; resolver must run the existing part gate.");
    }

    [Test,Timeout(240000)] public void TenBreeds_All63NativeEndSamples_WeightedBodyMatchesActualSourceSkin()
    {
        var catalog=CatBreedCatalog.Load();var jump=CatJumpClearanceCatalog.Load();var scene=EditorSceneManager.NewPreviewScene();
        var rows=new List<string>{"breed,phase,region,vertices,maxError,bones"};int samples=0;
        var assets=new Dictionary<string,string>();
        try
        {
            foreach(var breed in catalog.Entries)
            {
                var entry=jump.Find(breed.Id);var clip=entry.sourceClip;Assert.That(clip,Is.Not.Null);
                string source=AssetDatabase.GetAssetPath(clip);assets[source]=AssetDatabase.GetAssetDependencyHash(source).ToString();
                var root=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA native weighted body source");SceneManager.MoveGameObjectToScene(root,scene);
                root.transform.SetPositionAndRotation(new Vector3(1,.07f,-1),Quaternion.Euler(0,37,0));var baked=new Mesh();
                try
                {
                    var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
                    var transforms=root.GetComponentsInChildren<Transform>(true);var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
                    Vector3 p=animator.transform.localPosition,s=animator.transform.localScale;Quaternion q=animator.transform.localRotation;
                    Assert.That(entry.samples.Length,Is.EqualTo(63));
                    foreach(var sample in entry.samples)
                    {
                        for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
                        clip.SampleAnimation(animator.gameObject,clip.length*sample.phase);animator.transform.localPosition=p;animator.transform.localRotation=q;animator.transform.localScale=s;
                        skin.BakeMesh(baked,true);var vertices=baked.vertices;var local=new Vector3[vertices.Length];float minY=float.PositiveInfinity;
                        for(int v=0;v<vertices.Length;v++)local[v]=root.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[v]));
                        foreach(int v in breed.ContactVertexIndices)minY=Mathf.Min(minY,local[v].y);
                        foreach(var region in entry.bodySurface)
                        {
                            var frame=CatPawWeightedSkin.Build(entry.bodyBones,sample.bodySkinMatrices,root.transform.localToWorldMatrix,region.skin,Vector3.up*.008f);
                            Assert.That(frame,Is.Not.Null);var predicted=new Vector3[region.skin.Length];
                            CatPawWeightedSkin.Fill(frame,Vector3.zero,Quaternion.identity,false,true,default,default,default,predicted);float maximum=0;
                            for(int v=0;v<predicted.Length;v++)maximum=Mathf.Max(maximum,Vector3.Distance(predicted[v],root.transform.TransformPoint(local[region.skin[v].vertexIndex]-Vector3.up*minY)+Vector3.up*.008f));
                            rows.Add(FormattableString.Invariant($"{breed.Id},{sample.phase:R},{region.region},{predicted.Length},{maximum:R},{entry.bodyBones.Length}"));
                            Assert.That(maximum,Is.LessThanOrEqualTo(.0001f),breed.Id+"/"+sample.phase+"/"+region.region+" original weighted source");
                        }
                        samples++;
                    }
                }
                finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(root);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/weighted-jump-source.csv",rows);}
        Assert.That(samples,Is.EqualTo(630));foreach(var pair in assets)Assert.That(AssetDatabase.GetAssetDependencyHash(pair.Key).ToString(),Is.EqualTo(pair.Value));
    }
}
#endif
