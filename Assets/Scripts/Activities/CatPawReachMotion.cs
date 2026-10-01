using UnityEngine;

/// <summary>Source gesture plus a reversible chest bend; hips, rear paws, root and limb lengths stay authored.</summary>
[DefaultExecutionOrder(550)]
[DisallowMultipleComponent]
public sealed class CatPawReachMotion : MonoBehaviour
{
    CatActivity owner;
    CatActivityAnimation poseDriver;
    CatToyContactMotion contact;
    CatPawReachPlan plan;
    Transform chest, hips, leftRear, rightRear;
    Quaternion chestPose;
    bool adjusted, requested, solveHands, hasSample;
    float envelope;
    public bool IsActive => owner != null && owner.IsRunning;
    public float AppliedChestPitch { get; private set; }
    public float AppliedChestYaw { get; private set; }
    public float RearSupportDisplacement { get; private set; }

    public void Sample(CatActivity activity, CatPawReachPlan reach, float normalizedPhase, bool solveHands = true)
    {
        if (activity == null || !activity.IsRunning || !activity.BelongsTo(GetComponent<CatMovement>())) { Clear(); return; }
        // Pause retains the last accepted pose. It cannot create an owner or
        // accept a newer source phase/target while simulation time is stopped.
        if (Time.timeScale <= 0f)
        {
            if (owner == activity && hasSample) requested = true;
            return;
        }
        if (owner != activity)
        {
            Clear(); owner = activity;
            poseDriver = GetComponent<CatActivityAnimation>();
            contact = GetComponent<CatToyContactMotion>() ?? gameObject.AddComponent<CatToyContactMotion>();
            foreach (var bone in GetComponentsInChildren<Transform>())
            {
                if (bone.name == "DEF-spine.001") chest = bone;
                if (bone.name == "DEF-spine") hips = bone;
                if (bone.name == "DEF-foot.L") leftRear = bone;
                if (bone.name == "DEF-foot.R") rightRear = bone;
            }
        }
        plan = reach; this.solveHands = solveHands;
        float phase = Mathf.Clamp01(normalizedPhase);
        envelope = Mathf.SmoothStep(0, 1, phase < .35f ? phase / .35f : phase <= .68f ? 1f : (1f - phase) / .32f);
        float sourceEnd=plan.Surface!=null&&plan.Surface.ReturnToStart?0f:1f;
        float source = phase < .35f ? Mathf.Lerp(0, plan.SourcePhase, Mathf.SmoothStep(0, 1, phase / .35f)) :
            phase <= .68f ? plan.SourcePhase : Mathf.Lerp(plan.SourcePhase, sourceEnd, Mathf.SmoothStep(0, 1, (phase - .68f) / .32f));
        poseDriver.SetTimedPose(plan.Pose, source);
        requested = hasSample = true;
    }

    void Update() => Restore();
    void LateUpdate()
    {
        if (!IsActive) { Clear(); return; }
        // Update restores our joints before the source animator evaluates.
        // Even when a paused activity skips Sample, apply the same correction
        // after that evaluation so neither chest nor hand falls back to source.
        bool holdPaused = hasSample && Time.timeScale <= 0f;
        if ((!requested && !holdPaused) || chest == null || hips == null || leftRear == null || rightRear == null) return;
        requested = false;
        chestPose = chest.localRotation; adjusted = true;
        Vector3 hipsBefore = hips.position, leftBefore = leftRear.position, rightBefore = rightRear.position;
        AppliedChestPitch = Mathf.Clamp(plan.ChestPitch, plan.Surface != null && plan.Pose == CatActivityPose.Paw ? -CatPawReachResolver.MaximumChestPitch : 0, CatPawReachResolver.MaximumChestPitch) * envelope;
        AppliedChestYaw = Mathf.Clamp(plan.ChestYaw, -CatPawReachResolver.MaximumChestYaw, CatPawReachResolver.MaximumChestYaw) * envelope;
        chest.rotation = Quaternion.AngleAxis(AppliedChestYaw, Vector3.up) * Quaternion.AngleAxis(AppliedChestPitch, transform.right) * chest.rotation;
        RearSupportDisplacement = Mathf.Max(Vector3.Distance(hipsBefore, hips.position),
            Mathf.Max(Vector3.Distance(leftBefore, leftRear.position), Vector3.Distance(rightBefore, rightRear.position)));
        if (RearSupportDisplacement > .0001f)
        {
            Restore(); owner.CancelForTransition(); return;
        }
        if (!solveHands) return;
        if (plan.Surface != null) contact.ReachSurface(plan.Surface, envelope);
        else if (plan.Both) contact.ReachBoth(plan.LeftTarget, plan.RightTarget, envelope, 16);
        else
        {
            // Reach's .42 sample has unit weight; map the envelope onto its
            // existing ascent so this helper never implements a second limb IK.
            contact.Reach(plan.Target, plan.Left, .42f * envelope, 16);
        }
    }
    void Restore()
    {
        if (!adjusted) return; adjusted = false;
        if (chest != null) chest.localRotation = chestPose;
    }
    public void Clear()
    {
        Restore(); if (owner != null) contact?.Clear();
        owner = null; poseDriver = null; contact = null;
        chest = hips = leftRear = rightRear = null;
        requested = hasSample = false; AppliedChestPitch = AppliedChestYaw = RearSupportDisplacement = 0;
    }
    void OnDisable() => Clear();
}

