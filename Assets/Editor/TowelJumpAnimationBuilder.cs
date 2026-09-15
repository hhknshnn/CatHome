using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Use the existing native clips without copying or changing their curves.</summary>
public static class TowelJumpAnimationBuilder
{
    public static void Build()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PolyperfectCatIntegrationBuilder.ControllerPath);
        var clips = AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath).OfType<AnimationClip>().ToArray();
        foreach (string name in new[] { "NativeJump", "TowelJumpUp", "TowelJumpDown", "TowelSettle", "TowelWake" })
        {
            string source = name == "NativeJump" ? "Jump" : name == "TowelJumpUp" ? "JumpUp" : name == "TowelJumpDown" ? "JumpDown" : "Sitting_to_Sleep";
            var clip = clips.First(c => c.name.EndsWith("|" + source, StringComparison.Ordinal));
            var machine = controller.layers[0].stateMachine;
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
            state.motion = clip; state.speed = 1; state.writeDefaultValues = true;
        }
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
    }
}
