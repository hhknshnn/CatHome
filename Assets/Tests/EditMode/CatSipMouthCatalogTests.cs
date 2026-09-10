using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class CatSipMouthCatalogTests
{
    [Test] public void BakedMouthSkinning_MatchesActualMeshAcrossAllBreedsAndDrinkingPhases()
    {
        var catalog=CatSipMouthCatalog.Load();Assert.That(catalog,Is.Not.Null,"Bake CatSipMouthBuilder.Build before validation.");
        var breeds=CatBreedCatalog.Load();Assert.That(catalog.BreedCount,Is.EqualTo(breeds.Count));
        Assert.That(catalog.ProfileCount,Is.EqualTo(7),"The ten breeds share seven original mesh profiles.");
        var clip=breeds.GameplayController.animationClips.First(c=>c.name.EndsWith("|Eating",StringComparison.Ordinal));
        var scene=EditorSceneManager.NewPreviewScene();var baked=new Mesh();
        try
        {
            foreach(var breed in breeds.Entries)
            {
                var profile=catalog.Find(breed.Id);Assert.That(profile,Is.Not.Null,breed.Id);
                Assert.That(profile.vertices.Length,Is.InRange(1,24));
                var visual=CatBreedVisualFactory.Create(breed,breeds.GameplayController,null);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual,scene);visual.transform.localScale=Vector3.one*.5f;
                try
                {
                    var animator=visual.GetComponentInChildren<Animator>();animator.enabled=false;
                    var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    foreach(float phase in new[]{0f,.37f,.85f})
                    {
                        clip.SampleAnimation(animator.gameObject,phase*clip.length);skin.BakeMesh(baked,true);var vertices=baked.vertices;
                        foreach(var vertex in profile.vertices)
                        {
                            Assert.That(vertex.influences.Length,Is.InRange(1,4));
                            Assert.That(vertex.influences.Sum(i=>i.weight),Is.EqualTo(1f).Within(.0001f));
                            Vector3 skinned=Vector3.zero;
                            foreach(var influence in vertex.influences)
                            {
                                var bone=animator.transform.Find(influence.bonePath);Assert.That(bone,Is.Not.Null,breed.Id+" "+influence.bonePath);
                                skinned+=bone.TransformPoint(influence.bindPosition)*influence.weight;
                            }
                            Vector3 actual=skin.transform.TransformPoint(vertices[vertex.vertexIndex]);
                            Assert.That(Vector3.Distance(skinned,actual),Is.LessThan(.001f),breed.Id+" phase="+phase+" real mouth vertex="+vertex.vertexIndex);
                        }
                    }
                }
                finally{Object.DestroyImmediate(visual);}
            }
        }
        finally{Object.DestroyImmediate(baked);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
