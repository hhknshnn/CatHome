#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public sealed class CatPawBodyCoverageTests
{
    const string Output="Docs/QA/INTERACTION_POLISH_2026-09-16/paw-body-coverage-stage";
    static CatPawReachCatalog.Entry Load(string breed)=>AssetDatabase.LoadAssetAtPath<CatPawReachCatalog>(
        "Assets/Resources/"+CatPawReachCatalog.SurfaceResourceName(breed,CatActivityPose.Paw)+".asset").Find(breed,CatActivityPose.Paw);

    [Test] public void TenPawShards_KeepEveryBodyFaceAndLegacyFourProbeMeasurements()
    {
        int breeds=0,seamVertices=0;
        foreach(var breed in CatBreedCatalog.Load().Entries)
        {
            breeds++;var entry=Load(breed.Id);Assert.That(entry.bodySurfaceVersion,Is.EqualTo(CatPawReachCatalog.BodySurfaceVersion));
            Assert.That(entry.bodySurface.Length,Is.EqualTo(4));Assert.That(entry.samples.Length,Is.EqualTo(33));
            var mesh=breed.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var weights=mesh.sharedMesh.boneWeights;var triangles=mesh.sharedMesh.triangles;
            var groups=Enumerable.Repeat(-1,weights.Length).ToArray();
            foreach(int v in breed.ContactVertexIndices)groups[v]=CatJumpClearanceBuilder.ClassifyBodyRegion(weights[v],mesh.bones);
            for(int r=0;r<4;r++)
            {
                var region=entry.bodySurface[r];Assert.That(region.parts.Length,Is.InRange(1,32));
                var map=region.skin.Select((v,i)=>(v.vertexIndex,i)).ToDictionary(v=>v.vertexIndex,v=>v.i);
                var owned=new HashSet<(int,int,int)>();
                foreach(var part in region.parts)
                {
                    var ids=new HashSet<int>(part.vertices);
                    for(int t=0;t<part.triangles.Length;t+=3)
                    {
                        var face=(part.triangles[t],part.triangles[t+1],part.triangles[t+2]);
                        Assert.That(ids.Contains(face.Item1)&&ids.Contains(face.Item2)&&ids.Contains(face.Item3),Is.True);
                        owned.Add(face);
                    }
                }
                var required=CatJumpSurfaceCoverageBuilder.RegionTriangles(triangles,groups,r);
                Assert.That(region.triangles.Length,Is.EqualTo(required.Length));
                for(int t=0;t<required.Length;t+=3)
                {
                    Assert.That(map.ContainsKey(required[t])&&map.ContainsKey(required[t+1])&&map.ContainsKey(required[t+2]),Is.True);
                    Assert.That(owned.Contains((map[required[t]],map[required[t+1]],map[required[t+2]])),Is.True,breed.Id+" entire source face "+t);
                }
                foreach(var vertex in region.skin)
                {
                    Assert.That(vertex.influences.Sum(v=>v.weight),Is.EqualTo(1).Within(.0001));
                    if(vertex.influences.Any(v=>entry.skinBones[v.sourceBone].motion==CatPawReachCatalog.SkinMotion.Fixed)&&
                        vertex.influences.Any(v=>entry.skinBones[v.sourceBone].motion!=CatPawReachCatalog.SkinMotion.Fixed))seamVertices++;
                }
            }
            var body=AssetDatabase.LoadAssetAtPath<CatPawReachCatalog>("Assets/Resources/"+CatPawReachCatalog.BodyResourceName(breed.Id)+".asset").Find(breed.Id,CatActivityPose.Paw);
            foreach(var source in body.samples)
            {
                var dense=entry.samples.Single(s=>Mathf.Abs(s.phase-source.phase)<.000001f);
                Assert.That(dense.bodyProbes.Length,Is.EqualTo(4));
                for(int r=0;r<4;r++)
                {
                    Assert.That(dense.bodyProbes[r].region,Is.EqualTo(source.bodyProbes[r].region));
                    Assert.That(Vector3.Distance(dense.bodyProbes[r].start,source.bodyProbes[r].start),Is.LessThan(.00001f));
                    Assert.That(Vector3.Distance(dense.bodyProbes[r].end,source.bodyProbes[r].end),Is.LessThan(.00001f));
                    Assert.That(dense.bodyProbes[r].radius,Is.EqualTo(source.bodyProbes[r].radius).Within(.00001f));
                }
            }
        }
        Assert.That(breeds,Is.EqualTo(10));Assert.That(seamVertices,Is.GreaterThan(0),"Mixed pelvis/chest ownership must be tested, not rigidly approximated");
    }

    [Test,Timeout(240000)] public void TenBreeds_All33PawPhases_WeightedBodyMatchesRenderedNegativePitchAndBothArms()
    {
        var catalog=CatBreedCatalog.Load();var scene=EditorSceneManager.NewPreviewScene();
        var setup=EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive).ToArray();
        var lines=new List<string>{"breed,phase,activeLeft,pitch,yaw,sourceError,pelvisError,chestError,neckError,headError"};
        var clipMethod=typeof(CatPawReachBuilder).GetMethod("StateClip",BindingFlags.NonPublic|BindingFlags.Static);
        try
        {
            foreach(var breed in catalog.Entries)
            {
                var entry=Load(breed.Id);var root=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA weighted body skin");
                SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetPositionAndRotation(new Vector3(1,.07f,-1),Quaternion.Euler(0,37,0));root.transform.localScale=Vector3.one*.5f;
                var baked=new Mesh();
                try
                {
                    var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
                    var transforms=root.GetComponentsInChildren<Transform>(true);var positions=transforms.Select(t=>t.localPosition).ToArray();
                    var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
                    var clip=(AnimationClip)clipMethod.Invoke(null,new object[]{catalog.GameplayController,entry.stateName});
                    var chest=CatBreedVisualFactory.FindDescendant(animator.transform,"DEF-spine.001");
                    foreach(var sample in entry.samples)for(int active=0;active<2;active++)
                    {
                        for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
                        Vector3 visualPosition=animator.transform.localPosition,visualScale=animator.transform.localScale;Quaternion visualRotation=animator.transform.localRotation;
                        clip.SampleAnimation(animator.gameObject,(clip.isLooping?Mathf.Repeat(sample.phase,1f):sample.phase)*clip.length);
                        animator.transform.localPosition=visualPosition;animator.transform.localRotation=visualRotation;animator.transform.localScale=visualScale;
                        animator.transform.position+=root.transform.TransformVector(Vector3.up*-sample.sourceMinimumY)+Vector3.up*CatPawReachCatalog.GroundClearance;
                        var frames=entry.bodySurface.Select(r=>CatPawWeightedSkin.Build(entry,sample,root.transform.localToWorldMatrix,r.skin)).ToArray();
                        Assert.That(frames.All(f=>f!=null),Is.True);var results=entry.bodySurface.Select(r=>new Vector3[r.skin.Length]).ToArray();
                        Vector3 pivot=CatPawReachCatalog.WorldPoint(root.transform,sample.torsoPivot);
                        skin.BakeMesh(baked,true);var sourceVertices=baked.vertices;float sourceError=0;
                        for(int r=0;r<4;r++)
                        {
                            CatPawWeightedSkin.Fill(frames[r],pivot,Quaternion.identity,false,active==0,default,default,default,results[r]);
                            var defs=entry.bodySurface[r].skin;
                            for(int v=0;v<defs.Length;v++)sourceError=Mathf.Max(sourceError,Vector3.Distance(results[r][v],skin.transform.TransformPoint(sourceVertices[defs[v].vertexIndex])));
                        }
                        float envelope=Mathf.Clamp01(sample.phase<=.32f?sample.phase/.32f:(1-sample.phase)/.68f);
                        float pitch=active==0?-24:-32,yaw=active==0?0:15;
                        Quaternion bend=Quaternion.AngleAxis(yaw*envelope,Vector3.up)*Quaternion.AngleAxis(pitch*envelope,root.transform.right);chest.rotation=bend*chest.rotation;
                        string suffix=active==0?"L":"R";var upper=CatBreedVisualFactory.FindDescendant(animator.transform,"DEF-upper_arm."+suffix);
                        var fore=CatBreedVisualFactory.FindDescendant(animator.transform,"DEF-forearm."+suffix);var hand=CatBreedVisualFactory.FindDescendant(animator.transform,"DEF-hand."+suffix);
                        var patch=(active==0?entry.leftPaw:entry.rightPaw).Where(v=>v.distal).Take(6).ToArray();
                        Vector3 endpoint=PatchPoint(),sourceUpper=upper.position,sourceFore=fore.position;
                        Vector3 target=endpoint+root.transform.forward*.025f+Vector3.up*.015f;
                        var solve=CatPawSurfaceCcd.Solve(sourceUpper,sourceFore,hand.position,endpoint,target,envelope,Vector3.up,.012f);
                        for(int r=0;r<4;r++)CatPawWeightedSkin.Fill(frames[r],pivot,bend,true,active==0,solve,sourceUpper,sourceFore,results[r]);
                        Vector3 goal=CatPawSurfaceCcd.Goal(endpoint,target,envelope,Vector3.up,.012f);
                        for(int iteration=0;iteration<16;iteration++){Turn(fore);Turn(upper);}
                        skin.BakeMesh(baked,true);var vertices=baked.vertices;var errors=new float[4];
                        for(int r=0;r<4;r++)for(int v=0;v<entry.bodySurface[r].skin.Length;v++)errors[r]=Mathf.Max(errors[r],
                            Vector3.Distance(results[r][v],skin.transform.TransformPoint(vertices[entry.bodySurface[r].skin[v].vertexIndex])));
                        lines.Add(FormattableString.Invariant($"{breed.Id},{sample.phase:F3},{active==0},{pitch},{yaw},{sourceError:F8},{errors[0]:F8},{errors[1]:F8},{errors[2]:F8},{errors[3]:F8}"));
                        Assert.That(Mathf.Max(sourceError,errors.Max()),Is.LessThanOrEqualTo(.0001f),breed.Id+"/"+sample.phase+" weighted body and moving seam");
                        Vector3 PatchPoint(){Vector3 p=Vector3.zero;foreach(var v in patch)foreach(var w in v.influences)p+=animator.transform.Find(w.bonePath).TransformPoint(w.bindPosition)*w.weight;return p/patch.Length;}
                        void Turn(Transform joint){Vector3 a=PatchPoint()-joint.position,b=goal-joint.position;if(a.sqrMagnitude<.00001f||b.sqrMagnitude<.00001f)return;joint.rotation=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(a,b),20)*joint.rotation;}
                    }
                }
                finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(root);}
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/weighted-body-source-proof.csv",lines);
            CollectionAssert.AreEqual(setup,EditorSceneManager.GetSceneManagerSetup().Select(s=>s.path+":"+s.isLoaded+":"+s.isActive).ToArray());
        }
    }
}
#endif
