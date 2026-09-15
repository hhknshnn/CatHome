using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class NativeJumpSourceTests
{
    [Test] public void FurnitureJump_ReferencesUntouchedVendorClip_AndReturnsToItsStandingPose()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PolyperfectCatIntegrationBuilder.ControllerPath);
        var state = controller.layers[0].stateMachine.states.Single(s=>s.state.name=="NativeJump").state;
        var clip = AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath).OfType<AnimationClip>()
            .Single(c=>!c.name.StartsWith("__",StringComparison.Ordinal)&&c.name.EndsWith("|Jump",StringComparison.Ordinal));
        Assert.That(state.motion, Is.SameAs(clip), "The original Jump must not be replaced by a reconstructed pose.");
        var catalog = CatBreedCatalog.Load();
        var visual = CatBreedVisualFactory.Create(catalog.Entries[0], catalog.GameplayController, null, "Source endpoint proof");
        try
        {
            var animator = visual.GetComponentInChildren<Animator>(); animator.enabled = false;
            var bones = animator.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("DEF-",StringComparison.Ordinal)).ToArray();
            clip.SampleAnimation(animator.gameObject,0);
            var positions = bones.Select(t=>t.localPosition).ToArray(); var rotations=bones.Select(t=>t.localRotation).ToArray();
            clip.SampleAnimation(animator.gameObject,clip.length);
            for (int i=0;i<bones.Length;i++)
            {
                Assert.That(Vector3.Distance(bones[i].localPosition,positions[i]),Is.LessThan(.0001f),bones[i].name);
                Assert.That(Quaternion.Angle(bones[i].localRotation,rotations[i]),Is.LessThan(.06f),bones[i].name);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(visual); }
    }
}
