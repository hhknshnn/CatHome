using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Contact alone cannot tell a cat crouch from inverted knees and rolled paws.</summary>
public sealed class RunnerCrouchAnatomyTests
{
    [Test]
    public void AllBreeds_CrouchKeepsElbowsBackKneesForwardAndPawsAligned()
    {
        var catalog=CatBreedCatalog.Load();
        var source=AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath)
            .OfType<AnimationClip>().First(c=>c.name.EndsWith("|Walk",StringComparison.Ordinal));
        foreach(var entry in catalog.Entries)
        {
            var visual=CatBreedVisualFactory.Create(entry,catalog.GameplayController,null);
            visual.transform.localScale=Vector3.one*.5f;
            try
            {
                var animator=visual.GetComponentInChildren<Animator>();animator.enabled=false;
                var clip=Resources.Load<AnimationClip>("MiniGameAnimations/"+entry.Id+"_Duck");
                Assert.That(clip,Is.Not.Null,entry.Id);
                var joints=visual.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("DEF-"))
                    .ToDictionary(t=>t.name);
                string[] paws={"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"};
                for(int frame=0;frame<=60;frame++)
                {
                    float phase=frame/60f;
                    source.SampleAnimation(animator.gameObject,phase*source.length);
                    var pawRotations=paws.ToDictionary(n=>n,n=>joints[n].rotation);
                    Vector3 sourceTorso=joints["DEF-spine.003"].position-joints["DEF-spine"].position;
                    Vector3 sourceGaze=joints["DEF-spine.006"].position-joints["DEF-spine.004"].position;
                    clip.SampleAnimation(animator.gameObject,phase);
                    foreach(string side in new[]{"L","R"})
                    {
                        AssertBend(joints,"DEF-upper_arm."+side,"DEF-forearm."+side,"DEF-hand."+side,
                            -visual.transform.forward,entry.Id+" elbow "+side+" frame "+frame);
                        AssertBend(joints,"DEF-thigh."+side,"DEF-shin."+side,"DEF-foot."+side,
                            visual.transform.forward,entry.Id+" knee "+side+" frame "+frame);
                    }
                    foreach(string paw in paws)
                        Assert.That(Quaternion.Angle(pawRotations[paw],joints[paw].rotation),Is.LessThan(5f),
                            entry.Id+" "+paw+" must retain the authored sole angle at "+frame);
                    Vector3 torso=joints["DEF-spine.003"].position-joints["DEF-spine"].position;
                    Vector3 gaze=joints["DEF-spine.006"].position-joints["DEF-spine.004"].position;
                    Assert.That(torso.magnitude/sourceTorso.magnitude,Is.InRange(.97f,1.03f),entry.Id+" stretched torso");
                    Assert.That(Vector3.Angle(sourceTorso,torso),Is.LessThan(3f),entry.Id+" folded spine");
                    Assert.That(Vector3.Angle(sourceGaze,gaze),Is.LessThan(4f),entry.Id+" twisted neck");
                }
                Assert.That(AnimationUtility.GetCurveBindings(clip).Any(c=>c.propertyName.Contains("Scale")),Is.False);
            }
            finally{Object.DestroyImmediate(visual);}
        }
    }

    [Test]
    public void CrouchCadence_IsReadableFromRecoveryThroughMaximumRunnerSpeed()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(CatRunnerLauncher.RunnerScenePath,
            UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var game=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatRunnerGameController>(true)).Single();
            var serialized=new SerializedObject(game);
            float start=serialized.FindProperty("baseSpeed").floatValue;
            float maximum=start*serialized.FindProperty("maximumSpeedMultiplier").floatValue;
            Assert.That(maximum,Is.GreaterThan(start));
            foreach(float speed in new[]{start*.52f,start,maximum,maximum*2})
                Assert.That(MiniGameCatAnimation.CrouchCyclesPerSecond(speed),Is.InRange(1.25f,2.6f),"speed "+speed);
            Assert.That(MiniGameCatAnimation.CrouchCyclesPerSecond(start),Is.LessThan(2),"walk cannot flutter at eight cycles/sec");
        }
        finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
    }

    private static void AssertBend(Dictionary<string,Transform> bones,string upper,string middle,string tip,Vector3 expected,string message)
    {
        Vector3 chain=bones[tip].position-bones[upper].position;
        Vector3 bend=Vector3.ProjectOnPlane(bones[middle].position-bones[upper].position,chain);
        Vector3 plane=Vector3.ProjectOnPlane(expected,chain);
        Assert.That(bend.magnitude,Is.GreaterThan(.006f),message+" is locked straight");
        Assert.That(Vector3.Dot(bend.normalized,plane.normalized),Is.GreaterThan(.9f),message+" bends backwards");
    }
}
