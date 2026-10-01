using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class CatCareWeightedSkinTests
{
    [Test]
    public void AllTenBreeds_SourceEatingAndBoundedJoints_ExactlyMatchNativeBakedSkin()
    {
        var breeds=CatBreedCatalog.Load();Assert.That(breeds.Count,Is.EqualTo(10));
        var clip=breeds.GameplayController.animationClips.First(c=>c.name.EndsWith("|Eating",StringComparison.Ordinal));
        var scene=EditorSceneManager.NewPreviewScene();int checkedVertices=0,checkedPoses=0;float maximum=0;
        try
        {
            foreach(var breed in breeds.Entries)
            {
                var visual=CatBreedVisualFactory.Create(breed,breeds.GameplayController,null);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual,scene);
                visual.transform.SetPositionAndRotation(new Vector3(3.14f,.05f,-1.35f),Quaternion.Euler(0,37,0));visual.transform.localScale=Vector3.one*.5f;
                var mesh=new Mesh();
                try
                {
                    var animator=visual.GetComponentInChildren<Animator>();animator.enabled=false;var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    var profile=CatCareSkinBuilder.Capture(breed,animator,skin);
                    Assert.That(profile.vertexIndices,Is.EqualTo(breed.ContactVertexIndices));
                    Assert.That(CatCareWeightedSkin.TryBind(profile,animator,skin,out var weighted),Is.True);
                    var transforms=animator.GetComponentsInChildren<Transform>(true);
                    var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();
                    foreach(float phase in new[]{0f,.25f,.5f,.75f})foreach(bool adjusted in new[]{false,true})
                    {
                        for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];}
                        clip.SampleAnimation(animator.gameObject,clip.length*phase);
                        if(adjusted)
                        {
                            // Exercise current anatomical transforms, not a stored source-frame result.
                            animator.transform.position+=visual.transform.forward*.07f;
                            foreach(string name in new[]{"DEF-spine.002","DEF-spine.004","DEF-spine.005","DEF-spine.006"})
                            {var bone=CatBreedVisualFactory.FindDescendant(visual.transform,name);Assert.That(bone,Is.Not.Null);bone.localRotation=Quaternion.AngleAxis(7,Vector3.right)*bone.localRotation;}
                        }
                        Assert.That(weighted.CaptureMatrices(),Is.True);skin.BakeMesh(mesh,true);var actual=mesh.vertices;
                        for(int v=0;v<weighted.Count;v++)
                        {
                            float error=Vector3.Distance(weighted.Point(v),skin.transform.TransformPoint(actual[weighted.SourceIndex(v)]));
                            maximum=Mathf.Max(maximum,error);checkedVertices++;
                            Assert.That(error,Is.LessThan(.000003f),breed.Id+" phase="+phase+" adjusted="+adjusted+" sourceVertex="+weighted.SourceIndex(v));
                        }
                        checkedPoses++;
                    }
                }
                finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(visual);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        Assert.That(checkedPoses,Is.EqualTo(80));Assert.That(checkedVertices,Is.GreaterThan(50000));
        TestContext.WriteLine("Exact weighted skin: "+checkedPoses+" source/current poses / "+checkedVertices+" vertices, max error="+maximum);
    }
}
