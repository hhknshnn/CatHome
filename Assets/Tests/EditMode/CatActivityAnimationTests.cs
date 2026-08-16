using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class CatActivityAnimationTests
{
    private const string ControllerPath =
        "Assets/PolyOne/Cartoon Dog, Cat/Animation/Cat/Controller_CartoonAnimal_Cat.controller";
    private const string ClipFolder = "Assets/Animations/CatActivities/";

    [TestCase("ActivityPounce", "Cat_Activity_Pounce.anim")]
    [TestCase("ActivityPawSwat", "Cat_Activity_PawSwat.anim")]
    [TestCase("ActivityScratch", "Cat_Activity_Scratch.anim")]
    public void ActivityState_UsesAuthoredClipWithRigCurves(string stateName, string clipFile)
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipFolder + clipFile);

        Assert.That(controller, Is.Not.Null);
        Assert.That(clip, Is.Not.Null);
        Assert.That(AnimationUtility.GetCurveBindings(clip).Length, Is.GreaterThan(20));

        AnimatorState state = controller.layers[0].stateMachine.states
            .Select(child => child.state)
            .FirstOrDefault(candidate => candidate != null && candidate.name == stateName);
        Assert.That(state, Is.Not.Null, stateName + " is missing from the current cat controller.");
        Assert.That(state.motion, Is.SameAs(clip));
    }

    [Test]
    public void ScratchClip_LoopsForContinuousScratching()
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            ClipFolder + "Cat_Activity_Scratch.anim");
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);

        Assert.That(settings.loopTime, Is.True);
    }
}
