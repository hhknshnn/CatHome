using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public sealed class MiniGamePresentationTests
{
    [Test]
    public void EveryRunnerHazard_UsesTheBuiltMeshHeight()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(CatRunnerLauncher.RunnerScenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            int count=0;
            foreach(var root in scene.GetRootGameObjects())foreach(var item in root.GetComponentsInChildren<CatRunnerTrackObject>(true))
            {
                if(!item.IsHazard)continue;
                Transform model=null;foreach(Transform child in item.transform)if(child.name.StartsWith("Runner"))model=child;
                Assert.That(model,Is.Not.Null,item.name);float highest=0;
                foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    var b=filter.sharedMesh.bounds;
                    for(int i=0;i<8;i++)
                    {
                        var v=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                        highest=Mathf.Max(highest,item.transform.InverseTransformPoint(filter.transform.TransformPoint(v)).y);
                    }
                }
                var so=new SerializedObject(item);
                Assert.That(so.FindProperty("collisionTopOffset").floatValue,Is.EqualTo(highest).Within(.004f),item.name);
                Assert.That(highest,Is.GreaterThan(.15f),item.name+" has an empty hazard envelope");count++;
            }
            Assert.That(count,Is.EqualTo(16));
        }
        finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
    }
    [Test]
    public void EveryBreed_FitsTheVisibleDuckGateWithoutSinking()
    {
        var catalog=CatBreedCatalog.Load();var mesh=new Mesh();
        try
        {
            foreach(var entry in catalog.Entries)
            {
                var root=CatBreedVisualFactory.Create(entry,catalog.GameplayController,null);root.transform.localScale=Vector3.one*.5f;
                try
                {
                    var a=root.GetComponentInChildren<Animator>();a.enabled=false;
                    var clip=Resources.Load<AnimationClip>("MiniGameAnimations/"+entry.Id+"_Duck");Assert.That(clip,Is.Not.Null,entry.Id);
                    for(int frame=0;frame<=240;frame++)
                    {
                        clip.SampleAnimation(a.gameObject,frame/240f);
                        float low=float.PositiveInfinity,high=float.NegativeInfinity;
                        foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                        {
                            skin.BakeMesh(mesh,true);
                            foreach(var v in mesh.vertices){float y=skin.transform.TransformPoint(v).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);}
                        }
                        Assert.That(low,Is.InRange(-.006f,.025f),entry.Id+" foot contact frame "+frame);
                        Assert.That(high,Is.LessThan(.50f),entry.Id+" body/tail hits gate at "+frame);
                    }
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
        finally{Object.DestroyImmediate(mesh);}
    }
    [Test]
    public void JumpAndPounce_UseTheRealSkeletonForAllBreeds()
    {
        foreach(var entry in CatBreedCatalog.Load().Entries)
        foreach(string kind in new[]{"Jump","Pounce"})
        {
            var clip=Resources.Load<AnimationClip>("MiniGameAnimations/"+entry.Id+"_"+kind);
            Assert.That(clip,Is.Not.Null,entry.Id);
            var curves=AnimationUtility.GetCurveBindings(clip);
            Assert.That(curves.Any(c=>c.path.EndsWith("DEF-forearm.L")&&c.propertyName=="m_LocalRotation.x"),Is.True);
            Assert.That(curves.Any(c=>c.propertyName.Contains("Scale")),Is.False,"Do not squash the cat instead of posing it.");
        }
    }
    [Test]
    public void MiniGameArt_IsAuthoredGeometryAndHdMaterial()
    {
        foreach(string name in new[]{"RunnerYarnBasket","RunnerScratchPost","RunnerFoodBowl","RunnerVacuum","RunnerCatBed","RunnerTreats","RunnerCarrier","RunnerPillows","RunnerRibbonGate","RunnerNapCanopy","CatchFeltMouse","RunnerTownhouse0","RunnerTownhouse1","RunnerTownhouse2","CatchPlayroom"})
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/MiniGames/Models/"+name+".fbx");Assert.That(asset,Is.Not.Null,name);
            Assert.That(asset.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.vertexCount),Is.GreaterThan(100),name);
        }
        foreach(string name in new[]{"OakGrain","LinenWeave"})Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MiniGames/Textures/"+name+".png").width,Is.EqualTo(2048));
    }
}
