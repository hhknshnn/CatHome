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

public sealed class CatPawWeightedSkinTests
{
    const string Output="Docs/QA/INTERACTION_POLISH_2026-09-16/paw-surface-stage/full-front-arm";
    const float Epsilon=.0001f;
    [Test] public void FrontArmTopology_CoversEveryInfluencedVertexAndTouchingSourceFace()
    {
        var lines=new List<string>{"breed,side,vertices,faces,regions,bones,samples"};
        foreach(var breed in CatBreedCatalog.Load().Entries)
        {
            var entry=Load(breed.Id);var skin=breed.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;var triangles=skin.sharedMesh.triangles;
            var originalVertices=skin.sharedMesh.vertices;var bindPoses=skin.sharedMesh.bindposes;
            Assert.That(entry.surfaceGeometryVersion,Is.EqualTo(CatPawReachCatalog.SurfaceGeometryVersion));
            foreach(var sample in entry.samples)Assert.That(sample.skinMatrices.Length,Is.EqualTo(entry.skinBones.Length));
            for(int side=0;side<2;side++)
            {
                var upper=bones.Single(b=>b.name=="DEF-upper_arm."+(side==0?"L":"R"));
                var defs=side==0?entry.leftArmSkin:entry.rightArmSkin;
                var topology=side==0?entry.leftArmTriangles:entry.rightArmTriangles;
                var map=defs.Select((v,i)=>new{v.vertexIndex,index=i}).ToDictionary(v=>v.vertexIndex,v=>v.index);
                var required=new HashSet<int>();
                for(int v=0;v<weights.Length;v++)
                {var w=weights[v];if(Under(w.boneIndex0,w.weight0)||Under(w.boneIndex1,w.weight1)||Under(w.boneIndex2,w.weight2)||Under(w.boneIndex3,w.weight3))required.Add(v);}
                foreach(int v in required)Assert.That(map.ContainsKey(v),Is.True,breed.Id+" missing weighted arm vertex "+v);
                var regions=CatPawSurfaceRegions.Build(defs,topology,CatPawWeightedSkin.MaximumBoneGroups);
                Assert.That(regions.Length,Is.InRange(1,CatPawWeightedSkin.MaximumBoneGroups*4));
                for(int t=0;t<triangles.Length;t+=3)
                {
                    int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                    if(!required.Contains(a)&&!required.Contains(b)&&!required.Contains(c))continue;
                    Assert.That(map.ContainsKey(a)&&map.ContainsKey(b)&&map.ContainsKey(c),Is.True,"All source face corners, including torso seams, must be included");
                    Assert.That(regions.Any(r=>Array.IndexOf(r,map[a])>=0&&Array.IndexOf(r,map[b])>=0&&Array.IndexOf(r,map[c])>=0),Is.True,"Entire influenced face must fit one convex envelope");
                }
                foreach(var d in defs)
                {
                    var w=weights[d.vertexIndex];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] amounts={w.weight0,w.weight1,w.weight2,w.weight3};
                    int i=0;float sum=0;
                    for(int j=0;j<4;j++)if(amounts[j]>0)
                    {
                        var influence=d.influences[i++];sum+=influence.weight;
                        Assert.That(influence.weight,Is.EqualTo(amounts[j]));
                        Assert.That(entry.skinBones[influence.sourceBone].path,Is.EqualTo(influence.bonePath));
                        Assert.That(influence.bindPosition,Is.EqualTo(bindPoses[ids[j]].MultiplyPoint3x4(originalVertices[d.vertexIndex])));
                    }
                    Assert.That(i,Is.EqualTo(d.influences.Length));Assert.That(sum,Is.EqualTo(1).Within(.0001));
                }
                lines.Add(string.Join(",",breed.Id,side,defs.Length,topology.Length/3,regions.Length,entry.skinBones.Length,entry.samples.Length));
                bool Under(int index,float weight)=>weight>0&&(bones[index]==upper||bones[index].IsChildOf(upper));
            }
            if(breed.Id=="maine-coon")Assert.That(entry.leftArmSkin.Any(v=>v.vertexIndex==1473),Is.True,"Native .520 collision witness must be guarded");
            if(breed.Id=="british-shorthair")Assert.That(entry.rightArmSkin.Any(v=>v.vertexIndex==2178),Is.True,"Native .412 collision witness must be guarded");
        }
        Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/front-arm-coverage.csv",lines);
    }

    [Test,Timeout(240000)] public void TenBreeds_WeightedArmPrediction_MatchesLiveJointSkinning_AllSurfaceSamples()
    {
        var catalog=CatBreedCatalog.Load();var scene=EditorSceneManager.NewPreviewScene();
        var lines=new List<string>{"breed,phase,activeLeft,leftError,rightError,sourceError,witnessError,legacyRigidWitnessError"};
        var clipMethod=typeof(CatPawReachBuilder).GetMethod("StateClip",BindingFlags.NonPublic|BindingFlags.Static);
        try
        {
            foreach(var breed in catalog.Entries)
            {
                var entry=Load(breed.Id);var root=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA weighted front-arm skin");
                SceneManager.MoveGameObjectToScene(root,scene);root.transform.SetPositionAndRotation(new Vector3(1,.07f,-1),Quaternion.Euler(0,37,0));root.transform.localScale=Vector3.one*.5f;
                var baked=new Mesh();
                try
                {
                    var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
                    var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
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
                        var frames=new[]{CatPawWeightedSkin.Build(entry,sample,root.transform.localToWorldMatrix,entry.leftArmSkin),CatPawWeightedSkin.Build(entry,sample,root.transform.localToWorldMatrix,entry.rightArmSkin)};
                        Assert.That(frames.All(f=>f!=null),Is.True);
                        var results=new[]{new Vector3[entry.leftArmSkin.Length],new Vector3[entry.rightArmSkin.Length]};
                        Vector3 pivot=CatPawReachCatalog.WorldPoint(root.transform,sample.torsoPivot);
                        skin.BakeMesh(baked,true);var sourceVertices=baked.vertices;float sourceError=0;
                        for(int side=0;side<2;side++)
                        {
                            CatPawWeightedSkin.Fill(frames[side],pivot,Quaternion.identity,false,active==0,default,default,default,results[side]);
                            var defs=side==0?entry.leftArmSkin:entry.rightArmSkin;
                            for(int v=0;v<defs.Length;v++)sourceError=Mathf.Max(sourceError,Vector3.Distance(results[side][v],skin.transform.TransformPoint(sourceVertices[defs[v].vertexIndex])));
                        }
                        float envelope=Mathf.Sin(sample.phase*Mathf.PI);Quaternion bend=Quaternion.AngleAxis(23*envelope,Vector3.up)*Quaternion.AngleAxis(13*envelope,root.transform.right);
                        chest.rotation=bend*chest.rotation;
                        string suffix=active==0?"L":"R";var upper=CatBreedVisualFactory.FindDescendant(animator.transform,"DEF-upper_arm."+suffix);
                        var fore=CatBreedVisualFactory.FindDescendant(animator.transform,"DEF-forearm."+suffix);var hand=CatBreedVisualFactory.FindDescendant(animator.transform,"DEF-hand."+suffix);
                        var paw=active==0?entry.leftPaw:entry.rightPaw;var patch=paw.Where(v=>v.distal).Take(6).ToArray();
                        Vector3 endpoint=PatchPoint();Vector3 sourceUpper=upper.position,sourceFore=fore.position,sourceHand=hand.position;
                        Vector3 target=endpoint+root.transform.forward*.025f+Vector3.up*.015f;
                        var solve=CatPawSurfaceCcd.Solve(sourceUpper,sourceFore,sourceHand,endpoint,target,envelope,Vector3.up,.012f);
                        for(int side=0;side<2;side++)CatPawWeightedSkin.Fill(frames[side],pivot,bend,true,active==0,solve,sourceUpper,sourceFore,results[side]);
                        Vector3 goal=CatPawSurfaceCcd.Goal(endpoint,target,envelope,Vector3.up,.012f);
                        for(int iteration=0;iteration<16;iteration++){Turn(fore);Turn(upper);}
                        skin.BakeMesh(baked,true);var vertices=baked.vertices;var errors=new float[2];float witnessError=0,legacyError=0;
                        for(int side=0;side<2;side++)
                        {
                            var defs=side==0?entry.leftArmSkin:entry.rightArmSkin;
                            for(int v=0;v<defs.Length;v++)
                            {
                                Vector3 actual=skin.transform.TransformPoint(vertices[defs[v].vertexIndex]);
                                errors[side]=Mathf.Max(errors[side],Vector3.Distance(results[side][v],actual));
                                bool witness=breed.Id=="maine-coon"&&defs[v].vertexIndex==1473||breed.Id=="british-shorthair"&&defs[v].vertexIndex==2178;
                                if(witness)
                                {
                                    witnessError=Mathf.Max(witnessError,Vector3.Distance(results[side][v],actual));
                                    Vector3 sourcePoint=skin.transform.TransformPoint(sourceVertices[defs[v].vertexIndex]);sourcePoint=pivot+bend*(sourcePoint-pivot);
                                    if(side==active)legacyError=Mathf.Max(legacyError,Vector3.Distance(CatPawSurfaceCcd.TransformPoint(solve,sourceHand,sourcePoint),actual));
                                }
                            }
                        }
                        lines.Add(FormattableString.Invariant($"{breed.Id},{sample.phase:F3},{active==0},{errors[0]:F8},{errors[1]:F8},{sourceError:F8},{witnessError:F8},{legacyError:F8}"));
                        Assert.That(Mathf.Max(sourceError,Mathf.Max(errors[0],errors[1])),Is.LessThanOrEqualTo(Epsilon),breed.Id+" source phase "+sample.phase+" weighted full-arm prediction");
                        Vector3 PatchPoint()
                        {
                            Vector3 point=Vector3.zero;foreach(var v in patch)foreach(var influence in v.influences)point+=animator.transform.Find(influence.bonePath).TransformPoint(influence.bindPosition)*influence.weight;
                            return point/patch.Length;
                        }
                        void Turn(Transform joint)
                        {
                            Vector3 from=PatchPoint()-joint.position,to=goal-joint.position;if(from.sqrMagnitude<.00001f||to.sqrMagnitude<.00001f)return;
                            joint.rotation=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(from,to),20)*joint.rotation;
                        }
                    }
                }
                finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(root);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);Directory.CreateDirectory(Output);File.WriteAllLines(Output+"/weighted-arm-source-proof.csv",lines);}
    }
    static CatPawReachCatalog.Entry Load(string breed)
    {
        var catalog=AssetDatabase.LoadAssetAtPath<CatPawReachCatalog>("Assets/Resources/"+CatPawReachCatalog.SurfaceResourceName(breed,CatActivityPose.Scratch)+".asset");
        Assert.That(catalog,Is.Not.Null);return catalog.Find(breed,CatActivityPose.Scratch);
    }
}
#endif
