#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CatHomeAnimationGenerator
{
    private const string EatDrinkMenuPath = "Tools/Cat Home/Create Eat And Drink Animations";
    private const string SleepMenuPath = "Tools/Cat Home/Create Sleep Animations";
    private const string OutputFolder = "Assets/Animations/Cat";
    private const string EatClipPath = OutputFolder + "/Cat_Eat.anim";
    private const string DrinkClipPath = OutputFolder + "/Cat_Drink.anim";
    private const string LieDownClipPath = OutputFolder + "/Cat_LieDown.anim";
    private const string SleepClipPath = OutputFolder + "/Cat_Sleep.anim";
    private const float ClipLength = 1f;
    private const float LieDownClipLength = 1.35f;
    private const float SleepClipLength = 2.5f;
    private const float FrameRate = 30f;

    [MenuItem(EatDrinkMenuPath)]
    private static void CreateAnimations()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || EditorUtility.IsPersistent(selected))
        {
            Debug.LogError("Cat Home Animation Generator: Hierarchy'de bir GameObject seçin.");
            return;
        }

        Animator animator = selected.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogError($"Cat Home Animation Generator: '{selected.name}' üzerinde veya çocuklarında Animator bulunamadı.", selected);
            return;
        }

        Transform neck = FindDescendant(animator.transform, "Neck");
        Transform head = FindDescendant(animator.transform, "Head");
        if (neck == null || head == null)
        {
            string missing = neck == null && head == null ? "Neck ve Head" : neck == null ? "Neck" : "Head";
            Debug.LogError($"Cat Home Animation Generator: Animator '{animator.name}' iskeletinde {missing} isimli kemik bulunamadı. Kemik adlarının tam olarak 'Neck' ve 'Head' olduğundan emin olun.", animator);
            return;
        }

        AnimatorController controller = GetAnimatorController(animator.runtimeAnimatorController);
        if (controller == null)
        {
            Debug.LogError($"Cat Home Animation Generator: '{animator.name}' Animator bileşenine düzenlenebilir bir Animator Controller atanmamış.", animator);
            return;
        }

        if (controller.layers == null || controller.layers.Length == 0)
        {
            Debug.LogError($"Cat Home Animation Generator: '{controller.name}' Animator Controller içinde state eklenebilecek bir layer yok.", controller);
            return;
        }

        string neckPath = AnimationUtility.CalculateTransformPath(neck, animator.transform);
        string headPath = AnimationUtility.CalculateTransformPath(head, animator.transform);

        EnsureOutputFolders();

        AnimationClip eatClip = BuildClip(
            "Cat_Eat",
            neckPath, neck.localRotation,
            headPath, head.localRotation,
            new[] { 0f, 0.25f, 0.5f, 0.75f, 1f },
            new[] { 36f, 44f, 36f, 43f, 36f },
            new[] { 21f, 28f, 21f, 27f, 21f });

        AnimationClip drinkClip = BuildClip(
            "Cat_Drink",
            neckPath, neck.localRotation,
            headPath, head.localRotation,
            new[] { 0f, 0.5f, 1f },
            new[] { 46f, 50f, 46f },
            new[] { 30f, 34f, 30f });

        eatClip = SaveOrUpdateClip(eatClip, EatClipPath);
        drinkClip = SaveOrUpdateClip(drinkClip, DrinkClipPath);

        Undo.RecordObject(controller, "Add Cat Eat and Drink States");
        SetStateMotion(controller, "Eat", eatClip);
        SetStateMotion(controller, "Drink", drinkClip);
        EditorUtility.SetDirty(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Cat Home Animation Generator tamamlandı.\n" +
            $"Oluşturulan klipler: {EatClipPath}, {DrinkClipPath}\n" +
            $"Neck yolu: {neckPath}\n" +
            $"Head yolu: {headPath}\n" +
            $"Animator Controller: {AssetDatabase.GetAssetPath(controller)}",
            animator);
    }

    [MenuItem(SleepMenuPath)]
    private static void CreateSleepAnimations()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || EditorUtility.IsPersistent(selected))
        {
            Debug.LogError("Cat Home Sleep Animation Generator: Select the cat GameObject in the Hierarchy.");
            return;
        }

        Animator animator = selected.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogError(
                $"Cat Home Sleep Animation Generator: No Animator was found on '{selected.name}' or its children.",
                selected
            );
            return;
        }

        AnimatorController controller = GetAnimatorController(animator.runtimeAnimatorController);
        if (controller == null || controller.layers == null || controller.layers.Length == 0)
        {
            Debug.LogError(
                $"Cat Home Sleep Animation Generator: '{animator.name}' needs an editable Animator Controller with a Base Layer.",
                animator
            );
            return;
        }

        Transform root = FindDescendant(animator.transform, "Root");
        Transform spine1 = FindDescendant(animator.transform, "Spine1");
        Transform spine2 = FindDescendant(animator.transform, "Spine2");
        Transform neck = FindDescendant(animator.transform, "Neck");
        Transform head = FindDescendant(animator.transform, "Head");

        var missingBones = new List<string>();
        AddMissingBone(missingBones, root, "Root");
        AddMissingBone(missingBones, spine1, "Spine1");
        AddMissingBone(missingBones, spine2, "Spine2");
        AddMissingBone(missingBones, neck, "Neck");
        AddMissingBone(missingBones, head, "Head");

        if (missingBones.Count > 0)
        {
            Debug.LogError(
                "Cat Home Sleep Animation Generator: The selected Animator rig is missing required bones: " +
                string.Join(", ", missingBones) + ". No assets were changed.",
                animator
            );
            return;
        }

        string rootPath = AnimationUtility.CalculateTransformPath(root, animator.transform);
        string spine1Path = AnimationUtility.CalculateTransformPath(spine1, animator.transform);
        string spine2Path = AnimationUtility.CalculateTransformPath(spine2, animator.transform);
        string neckPath = AnimationUtility.CalculateTransformPath(neck, animator.transform);
        string headPath = AnimationUtility.CalculateTransformPath(head, animator.transform);

        Vector3 lyingRootPosition = root.localPosition + Vector3.down * 0.18f;
        Quaternion lyingRootRotation =
            root.localRotation * Quaternion.AngleAxis(82f, Vector3.up);
        Quaternion lyingSpine1Rotation =
            spine1.localRotation * Quaternion.AngleAxis(-6f, Vector3.right);
        Quaternion lyingSpine2Rotation =
            spine2.localRotation * Quaternion.AngleAxis(8f, Vector3.right);
        Quaternion lyingNeckRotation =
            neck.localRotation * Quaternion.AngleAxis(16f, Vector3.right);
        Quaternion lyingHeadRotation =
            head.localRotation * Quaternion.AngleAxis(12f, Vector3.right);

        EnsureOutputFolders();

        AnimationClip lieDownClip = BuildLieDownClip(
            rootPath, root.localPosition, root.localRotation,
            lyingRootPosition, lyingRootRotation,
            spine1Path, spine1.localRotation, lyingSpine1Rotation,
            spine2Path, spine2.localRotation, lyingSpine2Rotation,
            neckPath, neck.localRotation, lyingNeckRotation,
            headPath, head.localRotation, lyingHeadRotation
        );

        AnimationClip sleepClip = BuildSleepClip(
            rootPath, lyingRootPosition, lyingRootRotation,
            spine1Path, lyingSpine1Rotation,
            spine2Path, lyingSpine2Rotation, spine2.localScale,
            neckPath, lyingNeckRotation,
            headPath, lyingHeadRotation
        );

        lieDownClip = SaveOrUpdateClip(lieDownClip, LieDownClipPath);
        sleepClip = SaveOrUpdateClip(sleepClip, SleepClipPath);

        Undo.RecordObject(controller, "Add Cat Sleep States");
        AnimatorState lieDownState = SetStateMotionOnBaseLayer(controller, "LieDown", lieDownClip);
        AnimatorState sleepState = SetStateMotionOnBaseLayer(controller, "Sleep", sleepClip);
        EnsureAutomaticTransition(lieDownState, sleepState);
        EditorUtility.SetDirty(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "Cat Home sleep animations created or updated.\n" +
            $"Clips: {LieDownClipPath}, {SleepClipPath}\n" +
            $"Rig paths: {rootPath}, {spine1Path}, {spine2Path}, {neckPath}, {headPath}\n" +
            $"Animator Controller: {AssetDatabase.GetAssetPath(controller)}",
            animator
        );
    }

    private static void AddMissingBone(List<string> missingBones, Transform bone, string boneName)
    {
        if (bone == null)
            missingBones.Add(boneName);
    }

    private static Transform FindDescendant(Transform root, string targetName)
    {
        if (string.Equals(root.name, targetName, StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDescendant(root.GetChild(i), targetName);
            if (result != null)
                return result;
        }

        return null;
    }

    private static AnimatorController GetAnimatorController(RuntimeAnimatorController runtimeController)
    {
        while (runtimeController is AnimatorOverrideController overrideController)
            runtimeController = overrideController.runtimeAnimatorController;

        return runtimeController as AnimatorController;
    }

    private static AnimationClip BuildLieDownClip(
        string rootPath,
        Vector3 rootBasePosition,
        Quaternion rootBaseRotation,
        Vector3 rootLyingPosition,
        Quaternion rootLyingRotation,
        string spine1Path,
        Quaternion spine1BaseRotation,
        Quaternion spine1LyingRotation,
        string spine2Path,
        Quaternion spine2BaseRotation,
        Quaternion spine2LyingRotation,
        string neckPath,
        Quaternion neckBaseRotation,
        Quaternion neckLyingRotation,
        string headPath,
        Quaternion headBaseRotation,
        Quaternion headLyingRotation)
    {
        var clip = new AnimationClip
        {
            name = "Cat_LieDown",
            frameRate = FrameRate
        };

        float[] times = { 0f, 0.25f, 0.75f, 1.1f, LieDownClipLength };
        float[] weights = { 0f, 0.08f, 0.55f, 0.88f, 1f };

        AddVector3Curves(
            clip,
            rootPath,
            "localPosition",
            times,
            LerpVectors(rootBasePosition, rootLyingPosition, weights),
            true
        );
        AddQuaternionValueCurves(
            clip,
            rootPath,
            times,
            SlerpRotations(rootBaseRotation, rootLyingRotation, weights),
            true
        );
        AddQuaternionValueCurves(
            clip,
            spine1Path,
            times,
            SlerpRotations(spine1BaseRotation, spine1LyingRotation, weights),
            true
        );
        AddQuaternionValueCurves(
            clip,
            spine2Path,
            times,
            SlerpRotations(spine2BaseRotation, spine2LyingRotation, weights),
            true
        );
        AddQuaternionValueCurves(
            clip,
            neckPath,
            times,
            SlerpRotations(neckBaseRotation, neckLyingRotation, weights),
            true
        );
        AddQuaternionValueCurves(
            clip,
            headPath,
            times,
            SlerpRotations(headBaseRotation, headLyingRotation, weights),
            true
        );

        SetClipSettings(clip, false, LieDownClipLength);
        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static AnimationClip BuildSleepClip(
        string rootPath,
        Vector3 rootLyingPosition,
        Quaternion rootLyingRotation,
        string spine1Path,
        Quaternion spine1LyingRotation,
        string spine2Path,
        Quaternion spine2LyingRotation,
        Vector3 spine2BaseScale,
        string neckPath,
        Quaternion neckLyingRotation,
        string headPath,
        Quaternion headLyingRotation)
    {
        var clip = new AnimationClip
        {
            name = "Cat_Sleep",
            frameRate = FrameRate
        };

        float quarter = SleepClipLength * 0.25f;
        float[] times = { 0f, quarter, quarter * 2f, quarter * 3f, SleepClipLength };

        Vector3 breathUpPosition = rootLyingPosition + Vector3.up * 0.006f;
        Vector3[] rootPositions =
        {
            rootLyingPosition,
            breathUpPosition,
            rootLyingPosition,
            rootLyingPosition + Vector3.down * 0.003f,
            rootLyingPosition
        };

        Quaternion[] rootRotations = RepeatRotation(rootLyingRotation, times.Length);
        Quaternion[] spine1Rotations = OffsetRotations(
            spine1LyingRotation,
            new[] { 0f, 1.2f, 0f, -0.8f, 0f },
            Vector3.right
        );
        Quaternion[] spine2Rotations = OffsetRotations(
            spine2LyingRotation,
            new[] { 0f, 1.5f, 0f, -1f, 0f },
            Vector3.right
        );
        Quaternion[] neckRotations = OffsetRotations(
            neckLyingRotation,
            new[] { 0f, -0.8f, 0f, 0.5f, 0f },
            Vector3.right
        );
        Quaternion[] headRotations = OffsetRotations(
            headLyingRotation,
            new[] { 0f, -0.5f, 0f, 0.35f, 0f },
            Vector3.right
        );

        Vector3 expandedScale = new Vector3(
            spine2BaseScale.x * 1.012f,
            spine2BaseScale.y,
            spine2BaseScale.z * 1.012f
        );
        Vector3[] spine2Scales =
        {
            spine2BaseScale,
            expandedScale,
            spine2BaseScale,
            Vector3.Lerp(spine2BaseScale, expandedScale, 0.25f),
            spine2BaseScale
        };

        AddVector3Curves(clip, rootPath, "localPosition", times, rootPositions, true);
        AddQuaternionValueCurves(clip, rootPath, times, rootRotations, true);
        AddQuaternionValueCurves(clip, spine1Path, times, spine1Rotations, true);
        AddQuaternionValueCurves(clip, spine2Path, times, spine2Rotations, true);
        AddVector3Curves(clip, spine2Path, "localScale", times, spine2Scales, true);
        AddQuaternionValueCurves(clip, neckPath, times, neckRotations, true);
        AddQuaternionValueCurves(clip, headPath, times, headRotations, true);

        SetClipSettings(clip, true, SleepClipLength);
        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static AnimationClip BuildClip(
        string clipName,
        string neckPath,
        Quaternion neckBaseRotation,
        string headPath,
        Quaternion headBaseRotation,
        float[] times,
        float[] neckAngles,
        float[] headAngles)
    {
        var clip = new AnimationClip
        {
            name = clipName,
            frameRate = FrameRate
        };

        AddQuaternionCurves(clip, neckPath, neckBaseRotation, times, neckAngles);
        AddQuaternionCurves(clip, headPath, headBaseRotation, times, headAngles);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.startTime = 0f;
        settings.stopTime = ClipLength;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static void AddQuaternionCurves(
        AnimationClip clip,
        string path,
        Quaternion baseRotation,
        float[] times,
        float[] xAngles)
    {
        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();
        var w = new AnimationCurve();
        Quaternion previous = baseRotation;

        for (int i = 0; i < times.Length; i++)
        {
            // Post-multiplication rotates around the bone's existing local X axis.
            Quaternion rotation = baseRotation * Quaternion.AngleAxis(xAngles[i], Vector3.right);
            if (i > 0 && Quaternion.Dot(previous, rotation) < 0f)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);

            x.AddKey(times[i], rotation.x);
            y.AddKey(times[i], rotation.y);
            z.AddKey(times[i], rotation.z);
            w.AddKey(times[i], rotation.w);
            previous = rotation;
        }

        SetLinearTangents(x);
        SetLinearTangents(y);
        SetLinearTangents(z);
        SetLinearTangents(w);
        clip.SetCurve(path, typeof(Transform), "localRotation.x", x);
        clip.SetCurve(path, typeof(Transform), "localRotation.y", y);
        clip.SetCurve(path, typeof(Transform), "localRotation.z", z);
        clip.SetCurve(path, typeof(Transform), "localRotation.w", w);
    }

    private static void AddQuaternionValueCurves(
        AnimationClip clip,
        string path,
        float[] times,
        Quaternion[] rotations,
        bool smooth)
    {
        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();
        var w = new AnimationCurve();
        Quaternion previous = rotations[0];

        for (int i = 0; i < times.Length; i++)
        {
            Quaternion rotation = rotations[i];
            if (i > 0 && Quaternion.Dot(previous, rotation) < 0f)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);

            x.AddKey(times[i], rotation.x);
            y.AddKey(times[i], rotation.y);
            z.AddKey(times[i], rotation.z);
            w.AddKey(times[i], rotation.w);
            previous = rotation;
        }

        SetTangents(x, smooth);
        SetTangents(y, smooth);
        SetTangents(z, smooth);
        SetTangents(w, smooth);
        clip.SetCurve(path, typeof(Transform), "localRotation.x", x);
        clip.SetCurve(path, typeof(Transform), "localRotation.y", y);
        clip.SetCurve(path, typeof(Transform), "localRotation.z", z);
        clip.SetCurve(path, typeof(Transform), "localRotation.w", w);
    }

    private static void AddVector3Curves(
        AnimationClip clip,
        string path,
        string propertyPrefix,
        float[] times,
        Vector3[] values,
        bool smooth)
    {
        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();

        for (int i = 0; i < times.Length; i++)
        {
            x.AddKey(times[i], values[i].x);
            y.AddKey(times[i], values[i].y);
            z.AddKey(times[i], values[i].z);
        }

        SetTangents(x, smooth);
        SetTangents(y, smooth);
        SetTangents(z, smooth);
        clip.SetCurve(path, typeof(Transform), propertyPrefix + ".x", x);
        clip.SetCurve(path, typeof(Transform), propertyPrefix + ".y", y);
        clip.SetCurve(path, typeof(Transform), propertyPrefix + ".z", z);
    }

    private static Vector3[] LerpVectors(Vector3 from, Vector3 to, float[] weights)
    {
        var values = new Vector3[weights.Length];
        for (int i = 0; i < weights.Length; i++)
            values[i] = Vector3.LerpUnclamped(from, to, weights[i]);

        return values;
    }

    private static Quaternion[] SlerpRotations(Quaternion from, Quaternion to, float[] weights)
    {
        var values = new Quaternion[weights.Length];
        for (int i = 0; i < weights.Length; i++)
            values[i] = Quaternion.SlerpUnclamped(from, to, weights[i]);

        return values;
    }

    private static Quaternion[] RepeatRotation(Quaternion rotation, int count)
    {
        var values = new Quaternion[count];
        for (int i = 0; i < count; i++)
            values[i] = rotation;

        return values;
    }

    private static Quaternion[] OffsetRotations(
        Quaternion baseRotation,
        float[] angles,
        Vector3 localAxis)
    {
        var values = new Quaternion[angles.Length];
        for (int i = 0; i < angles.Length; i++)
            values[i] = baseRotation * Quaternion.AngleAxis(angles[i], localAxis);

        return values;
    }

    private static void SetClipSettings(AnimationClip clip, bool loop, float length)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.startTime = 0f;
        settings.stopTime = length;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    private static void SetTangents(AnimationCurve curve, bool smooth)
    {
        if (!smooth)
        {
            SetLinearTangents(curve);
            return;
        }

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.ClampedAuto
            );
            AnimationUtility.SetKeyRightTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.ClampedAuto
            );
        }
    }

    private static void SetLinearTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
    }

    private static AnimationClip SaveOrUpdateClip(AnimationClip generatedClip, string assetPath)
    {
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existingClip == null)
        {
            AssetDatabase.CreateAsset(generatedClip, assetPath);
            return generatedClip;
        }

        EditorUtility.CopySerialized(generatedClip, existingClip);
        existingClip.name = generatedClip.name;
        EditorUtility.SetDirty(existingClip);
        UnityEngine.Object.DestroyImmediate(generatedClip);
        return existingClip;
    }

    private static void SetStateMotion(AnimatorController controller, string stateName, Motion motion)
    {
        AnimatorState state = FindState(controller, stateName);
        if (state == null)
            state = controller.layers[0].stateMachine.AddState(stateName);

        state.motion = motion;
        EditorUtility.SetDirty(state);
    }

    private static AnimatorState SetStateMotionOnBaseLayer(
        AnimatorController controller,
        string stateName,
        Motion motion)
    {
        AnimatorStateMachine baseStateMachine = controller.layers[0].stateMachine;
        AnimatorState state = FindStateRecursive(baseStateMachine, stateName);
        if (state == null)
            state = baseStateMachine.AddState(stateName);

        state.motion = motion;
        state.writeDefaultValues = true;
        EditorUtility.SetDirty(state);
        return state;
    }

    private static void EnsureAutomaticTransition(
        AnimatorState lieDownState,
        AnimatorState sleepState)
    {
        AnimatorStateTransition transition = null;
        var duplicateTransitions = new List<AnimatorStateTransition>();

        foreach (AnimatorStateTransition candidate in lieDownState.transitions)
        {
            if (candidate.destinationState != sleepState)
                continue;

            if (transition == null)
                transition = candidate;
            else
                duplicateTransitions.Add(candidate);
        }

        foreach (AnimatorStateTransition duplicate in duplicateTransitions)
            lieDownState.RemoveTransition(duplicate);

        if (transition == null)
            transition = lieDownState.AddTransition(sleepState);

        foreach (AnimatorCondition condition in transition.conditions)
            transition.RemoveCondition(condition);

        transition.hasExitTime = true;
        transition.exitTime = 1f;
        transition.hasFixedDuration = true;
        transition.duration = 0.15f;
        transition.offset = 0f;
        transition.canTransitionToSelf = false;
        EditorUtility.SetDirty(transition);
    }

    private static AnimatorState FindState(AnimatorController controller, string stateName)
    {
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            AnimatorState state = FindStateRecursive(layer.stateMachine, stateName);
            if (state != null)
                return state;
        }

        return null;
    }

    private static AnimatorState FindStateRecursive(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            if (childState.state.name == stateName)
                return childState.state;
        }

        foreach (ChildAnimatorStateMachine childMachine in stateMachine.stateMachines)
        {
            AnimatorState state = FindStateRecursive(childMachine.stateMachine, stateName);
            if (state != null)
                return state;
        }

        return null;
    }

    private static void EnsureOutputFolders()
    {
        EnsureFolder("Assets", "Animations");
        EnsureFolder("Assets/Animations", "Cat");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
