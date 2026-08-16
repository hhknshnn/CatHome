using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Authors activity-specific clips against the existing Cat Home character rig
/// and adds directly addressable states to its current AnimatorController.
/// </summary>
public static class CatActivityAnimationBuilder
{
    public const string PounceState = "ActivityPounce";
    public const string PawSwatState = "ActivityPawSwat";
    public const string ScratchState = "ActivityScratch";
    public const string TunnelState = "ActivityTunnelCrawl";

    private const string CatPrefabPath =
        "Assets/PolyOne/Cartoon Dog, Cat/Prefab/SM_CartoonAnimal_Cat.prefab";
    private const string ControllerPath =
        "Assets/PolyOne/Cartoon Dog, Cat/Animation/Cat/Controller_CartoonAnimal_Cat.controller";
    private const string Folder = "Assets/Animations/CatActivities";

    private const string Root = "Root";
    private const string Spine = "Root/Spine";
    private const string Spine1 = "Root/Spine/Spine1";
    private const string Spine2 = "Root/Spine/Spine1/Spine2";
    private const string Neck = "Root/Spine/Spine1/Spine2/Neck";
    private const string Head = "Root/Spine/Spine1/Spine2/Neck/Head";
    private const string LeftShoulder = "Root/Spine/Spine1/Spine2/LeftShoulder";
    private const string LeftArm = LeftShoulder + "/LeftArm";
    private const string LeftForeArm = LeftArm + "/LeftForeArm";
    private const string LeftHand = LeftForeArm + "/LeftHand";
    private const string RightShoulder = "Root/Spine/Spine1/Spine2/RightShoulder";
    private const string RightArm = RightShoulder + "/RightArm";
    private const string RightForeArm = RightArm + "/RightForeArm";
    private const string RightHand = RightForeArm + "/RightHand";
    private const string LeftUpLeg = "Root/LeftUpLeg";
    private const string LeftLeg = LeftUpLeg + "/LeftLeg";
    private const string RightUpLeg = "Root/RightUpLeg";
    private const string RightLeg = RightUpLeg + "/RightLeg";
    private const string Tail = "Root/Tail";
    private const string Tail1 = Tail + "/Tail1";
    private const string Tail2 = Tail1 + "/Tail2";

    [MenuItem("Tools/Cat Home/Gameplay/Build Cat Activity Animations")]
    public static void BuildFromMenu()
    {
        EditorUtility.DisplayDialog("Cat Activity Animations", BuildSilently(), "OK");
    }

    public static string BuildSilently()
    {
        EnsureFolders();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPrefabPath);
        if (prefab == null)
            throw new InvalidOperationException("Cat prefab is missing at " + CatPrefabPath);
        Animator animator = prefab.GetComponentInChildren<Animator>(true);
        if (animator == null)
            throw new InvalidOperationException("The current cat prefab has no Animator.");

        AnimationClip pounce = SaveClip(BuildPounce(animator), Folder + "/Cat_Activity_Pounce.anim");
        AnimationClip pawSwat = SaveClip(BuildPawSwat(animator), Folder + "/Cat_Activity_PawSwat.anim");
        AnimationClip scratch = SaveClip(BuildScratch(animator), Folder + "/Cat_Activity_Scratch.anim");
        AnimationClip tunnel = SaveClip(BuildTunnelCrawl(animator), Folder + "/Cat_Activity_TunnelCrawl.anim");

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new InvalidOperationException("Cat AnimatorController is missing at " + ControllerPath);
        UpsertState(controller, PounceState, pounce);
        UpsertState(controller, PawSwatState, pawSwat);
        UpsertState(controller, ScratchState, scratch);
        UpsertState(controller, TunnelState, tunnel);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return "Pounce, paw-swat, scratching and tunnel-crawl clips rebuilt for the current cat rig.";
    }

    private static AnimationClip BuildPounce(Animator animator)
    {
        var clip = NewClip("Cat_Activity_Pounce");
        float[] t = { 0f, 0.11f, 0.23f, 0.39f, 0.56f, 0.72f };

        AddPosition(clip, animator, Root, t, new[]
        {
            Vector3.zero,
            new Vector3(0f, -0.045f, 0f),
            new Vector3(0f, -0.065f, 0.025f),
            new Vector3(0f, 0.14f, 0.16f),
            new Vector3(0f, 0.035f, 0.08f),
            Vector3.zero
        });
        AddRotation(clip, animator, Root, t, Deltas(0, -7, -16, 9, 4, 0, Vector3.right));
        AddRotation(clip, animator, Spine, t, Deltas(0, -10, -18, 15, -5, 0, Vector3.right));
        AddRotation(clip, animator, Spine2, t, Deltas(0, -6, -12, 12, -4, 0, Vector3.right));
        AddRotation(clip, animator, Neck, t, Deltas(0, 8, 13, -10, 5, 0, Vector3.right));
        AddRotation(clip, animator, Head, t, Deltas(0, 5, 8, -7, 3, 0, Vector3.right));

        AddRotation(clip, animator, LeftUpLeg, t, Deltas(0, -22, -34, 24, -8, 0, Vector3.forward));
        AddRotation(clip, animator, RightUpLeg, t, Deltas(0, 22, 34, -24, 8, 0, Vector3.forward));
        AddRotation(clip, animator, LeftLeg, t, Deltas(0, 18, 28, -18, 5, 0, Vector3.forward));
        AddRotation(clip, animator, RightLeg, t, Deltas(0, -18, -28, 18, -5, 0, Vector3.forward));
        AddRotation(clip, animator, LeftShoulder, t, Deltas(0, 18, 32, 62, 30, 0, Vector3.right));
        AddRotation(clip, animator, RightShoulder, t, Deltas(0, -18, -32, -62, -30, 0, Vector3.right));
        AddRotation(clip, animator, LeftArm, t, Deltas(0, 12, 25, 55, 25, 0, Vector3.right));
        AddRotation(clip, animator, RightArm, t, Deltas(0, -12, -25, -55, -25, 0, Vector3.right));
        AddRotation(clip, animator, LeftForeArm, t, Deltas(0, 5, 10, -35, -10, 0, Vector3.right));
        AddRotation(clip, animator, RightForeArm, t, Deltas(0, 5, 10, -35, -10, 0, Vector3.right));
        AddRotation(clip, animator, Tail, t, Deltas(0, 12, 24, -18, 10, 0, Vector3.right));
        AddRotation(clip, animator, Tail1, t, Deltas(0, 10, 18, -12, 8, 0, Vector3.right));
        AddRotation(clip, animator, Tail2, t, Deltas(0, -8, -15, 12, -7, 0, Vector3.up));

        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static AnimationClip BuildPawSwat(Animator animator)
    {
        var clip = NewClip("Cat_Activity_PawSwat");
        float[] t = { 0f, 0.12f, 0.23f, 0.34f, 0.46f, 0.58f };

        AddPosition(clip, animator, Root, t, new[]
        {
            Vector3.zero,
            new Vector3(0f, -0.018f, 0f),
            new Vector3(0f, 0.025f, 0.035f),
            new Vector3(0f, 0.03f, 0.055f),
            new Vector3(0f, 0.01f, 0.02f),
            Vector3.zero
        });
        AddRotation(clip, animator, Spine1, t, Combine(
            Deltas(0, -4, -9, -4, 2, 0, Vector3.right),
            Deltas(0, 5, 12, 7, -3, 0, Vector3.up)));
        AddRotation(clip, animator, Neck, t, Deltas(0, -5, -11, -7, 4, 0, Vector3.up));
        AddRotation(clip, animator, Head, t, Deltas(0, -7, -15, -8, 5, 0, Vector3.up));
        AddRotation(clip, animator, RightShoulder, t, Deltas(0, -45, -95, -25, -10, 0, Vector3.right));
        AddRotation(clip, animator, RightArm, t, Deltas(0, -30, -75, 25, -10, 0, Vector3.right));
        AddRotation(clip, animator, RightForeArm, t, Deltas(0, 12, 48, -35, -8, 0, Vector3.forward));
        AddRotation(clip, animator, RightHand, t, Deltas(0, -10, -32, 28, 8, 0, Vector3.up));
        AddRotation(clip, animator, LeftShoulder, t, Deltas(0, 8, 16, 10, 3, 0, Vector3.right));
        AddRotation(clip, animator, LeftArm, t, Deltas(0, 8, 15, 10, 3, 0, Vector3.right));
        AddRotation(clip, animator, LeftUpLeg, t, Deltas(0, -8, -14, -8, -3, 0, Vector3.forward));
        AddRotation(clip, animator, RightUpLeg, t, Deltas(0, 8, 14, 8, 3, 0, Vector3.forward));
        AddRotation(clip, animator, Tail, t, Deltas(0, 10, 22, -18, 8, 0, Vector3.up));
        AddRotation(clip, animator, Tail1, t, Deltas(0, -8, -16, 14, -6, 0, Vector3.up));

        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static AnimationClip BuildScratch(Animator animator)
    {
        var clip = NewClip("Cat_Activity_Scratch");
        float[] t = { 0f, 0.12f, 0.24f, 0.36f, 0.48f, 0.60f };

        AddPosition(clip, animator, Root, t, new[]
        {
            new Vector3(0f, 0.015f, 0f),
            new Vector3(0f, 0.04f, 0.015f),
            new Vector3(0f, 0.02f, 0.02f),
            new Vector3(0f, 0.04f, 0.015f),
            new Vector3(0f, 0.02f, 0.02f),
            new Vector3(0f, 0.015f, 0f)
        });
        AddRotation(clip, animator, Root, t, Deltas(-10, -15, -11, -15, -11, -10, Vector3.right));
        AddRotation(clip, animator, Spine, t, Deltas(18, 24, 18, 24, 18, 18, Vector3.right));
        AddRotation(clip, animator, Spine1, t, Deltas(10, 14, 9, 14, 9, 10, Vector3.right));
        AddRotation(clip, animator, Neck, t, Deltas(-8, -12, -7, -12, -7, -8, Vector3.right));
        AddRotation(clip, animator, Head, t, Deltas(-4, 4, -5, 4, -5, -4, Vector3.up));
        AddRotation(clip, animator, LeftShoulder, t, Deltas(72, 105, 78, 105, 78, 72, Vector3.right));
        AddRotation(clip, animator, RightShoulder, t, Deltas(-72, -78, -105, -78, -105, -72, Vector3.right));
        AddRotation(clip, animator, LeftArm, t, Deltas(52, 82, 56, 82, 56, 52, Vector3.right));
        AddRotation(clip, animator, RightArm, t, Deltas(-52, -56, -82, -56, -82, -52, Vector3.right));
        AddRotation(clip, animator, LeftForeArm, t, Deltas(-20, -52, -12, -52, -12, -20, Vector3.forward));
        AddRotation(clip, animator, RightForeArm, t, Deltas(20, 12, 52, 12, 52, 20, Vector3.forward));
        AddRotation(clip, animator, LeftHand, t, Deltas(0, 24, -8, 24, -8, 0, Vector3.up));
        AddRotation(clip, animator, RightHand, t, Deltas(0, -8, 24, -8, 24, 0, Vector3.up));
        AddRotation(clip, animator, LeftUpLeg, t, Deltas(-12, -16, -10, -16, -10, -12, Vector3.forward));
        AddRotation(clip, animator, RightUpLeg, t, Deltas(12, 16, 10, 16, 10, 12, Vector3.forward));
        AddRotation(clip, animator, Tail, t, Deltas(12, 20, 10, -16, 8, 12, Vector3.up));
        AddRotation(clip, animator, Tail1, t, Deltas(-8, -15, 8, 14, -7, -8, Vector3.up));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static AnimationClip BuildTunnelCrawl(Animator animator)
    {
        var clip = NewClip("Cat_Activity_TunnelCrawl");
        float[] t = { 0f, 0.14f, 0.28f, 0.42f, 0.56f, 0.70f };

        AddPosition(clip, animator, Root, t, new[]
        {
            new Vector3(0f, -0.04f, 0.02f),
            new Vector3(0f, -0.02f, 0.05f),
            new Vector3(0f, -0.05f, 0.03f),
            new Vector3(0f, -0.02f, 0.06f),
            new Vector3(0f, -0.05f, 0.03f),
            new Vector3(0f, -0.04f, 0.02f)
        });
        AddRotation(clip, animator, Root, t, Deltas(16, 20, 14, 20, 14, 16, Vector3.right));
        AddRotation(clip, animator, Spine, t, Deltas(22, 28, 18, 28, 18, 22, Vector3.right));
        AddRotation(clip, animator, Spine1, t, Deltas(10, 14, 8, 14, 8, 10, Vector3.right));
        AddRotation(clip, animator, Neck, t, Deltas(-6, -10, -4, -10, -4, -6, Vector3.right));
        AddRotation(clip, animator, Head, t, Deltas(-2, 6, -4, 6, -4, -2, Vector3.up));
        AddRotation(clip, animator, LeftShoulder, t, Deltas(38, 62, 28, 62, 28, 38, Vector3.right));
        AddRotation(clip, animator, RightShoulder, t, Deltas(-28, -38, -62, -38, -62, -28, Vector3.right));
        AddRotation(clip, animator, LeftArm, t, Deltas(28, 48, 18, 48, 18, 28, Vector3.right));
        AddRotation(clip, animator, RightArm, t, Deltas(-18, -28, -48, -28, -48, -18, Vector3.right));
        AddRotation(clip, animator, LeftUpLeg, t, Deltas(-18, -8, -24, -8, -24, -18, Vector3.right));
        AddRotation(clip, animator, RightUpLeg, t, Deltas(-24, -18, -8, -18, -8, -24, Vector3.right));
        AddRotation(clip, animator, Tail, t, Deltas(8, 16, 4, -12, 6, 8, Vector3.up));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static AnimationClip NewClip(string name)
    {
        var clip = new AnimationClip { name = name, frameRate = 30f };
        HoldRootScale(clip);
        return clip;
    }

    /// <summary>
    /// The shipped Idle and Run clips animate the root object's scale to one.
    /// A clip without that channel lets the scale drift back towards the
    /// transform's authored value mid-blend, which reads as the cat shrinking
    /// while it pounces. Every activity clip therefore pins the same scale.
    /// </summary>
    private static void HoldRootScale(AnimationClip clip)
    {
        var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.x", curve);
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.y", curve);
        clip.SetCurve(string.Empty, typeof(Transform), "m_LocalScale.z", curve);
    }

    private static AnimationClip SaveClip(AnimationClip clip, string path)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        EditorUtility.CopySerialized(clip, existing);
        UnityEngine.Object.DestroyImmediate(clip);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static void UpsertState(
        AnimatorController controller,
        string stateName,
        AnimationClip clip)
    {
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState state = null;
        ChildAnimatorState[] states = machine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != null && states[i].state.name == stateName)
            {
                state = states[i].state;
                break;
            }
        }

        if (state == null)
            state = machine.AddState(stateName);
        state.motion = clip;
        state.speed = 1f;
        state.writeDefaultValues = true;
        EditorUtility.SetDirty(state);
    }

    private static void AddPosition(
        AnimationClip clip,
        Animator animator,
        string path,
        float[] times,
        Vector3[] deltas)
    {
        Transform bone = RequireBone(animator, path);
        if (times.Length != deltas.Length)
            throw new ArgumentException("Position key counts do not match for " + path);
        Vector3 rest = bone.localPosition;
        var x = new Keyframe[times.Length];
        var y = new Keyframe[times.Length];
        var z = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
        {
            Vector3 value = rest + deltas[i];
            x[i] = new Keyframe(times[i], value.x);
            y[i] = new Keyframe(times[i], value.y);
            z[i] = new Keyframe(times[i], value.z);
        }
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", SmoothCurve(x));
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", SmoothCurve(y));
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", SmoothCurve(z));
    }

    private static void AddRotation(
        AnimationClip clip,
        Animator animator,
        string path,
        float[] times,
        Vector3[] eulerDeltas)
    {
        Transform bone = RequireBone(animator, path);
        if (times.Length != eulerDeltas.Length)
            throw new ArgumentException("Rotation key counts do not match for " + path);
        Quaternion rest = bone.localRotation;
        var x = new Keyframe[times.Length];
        var y = new Keyframe[times.Length];
        var z = new Keyframe[times.Length];
        var w = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
        {
            Quaternion value = rest * Quaternion.Euler(eulerDeltas[i]);
            x[i] = new Keyframe(times[i], value.x);
            y[i] = new Keyframe(times[i], value.y);
            z[i] = new Keyframe(times[i], value.z);
            w[i] = new Keyframe(times[i], value.w);
        }
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", SmoothCurve(x));
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", SmoothCurve(y));
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", SmoothCurve(z));
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", SmoothCurve(w));
    }

    private static AnimationCurve SmoothCurve(Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        return curve;
    }

    private static Transform RequireBone(Animator animator, string path)
    {
        Transform bone = animator.transform.Find(path);
        if (bone == null)
            throw new InvalidOperationException("Current cat rig is missing bone path: " + path);
        return bone;
    }

    private static Vector3[] Deltas(
        float a, float b, float c, float d, float e, float f, Vector3 axis)
    {
        return new[] { axis * a, axis * b, axis * c, axis * d, axis * e, axis * f };
    }

    private static Vector3[] Combine(Vector3[] first, Vector3[] second)
    {
        if (first.Length != second.Length)
            throw new ArgumentException("Animation delta arrays must match.");
        var result = new Vector3[first.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = first[i] + second[i];
        return result;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Animations", "CatActivities");
    }
}
