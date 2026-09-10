using UnityEngine;

/// <summary>One reversible, bone-length-preserving reach; a real hand arrival authorizes each tear.</summary>
[DefaultExecutionOrder(680)]
[DisallowMultipleComponent]
public sealed class CatPaperRollPawMotion : MonoBehaviour
{
    public const float ContactTolerance = .035f;
    PaperSpinActivity owner;
    Transform arm, forearm, hand;
    Quaternion armPose, forearmPose;
    bool adjusted, contacted;
    int stroke;
    float phase;

    public bool IsActive => owner != null && owner.IsRunning;
    public int StrokeIndex => stroke;
    public float NormalizedPhase => phase;
    public float Distance { get; private set; } = float.PositiveInfinity;
    public Vector3 PawPosition => hand != null ? hand.position : transform.position;
    public float UpperLength { get; private set; }
    public float ForeLength { get; private set; }
    public float MinimumDistance { get; private set; } = float.PositiveInfinity;
    public float MinimumArmTargetDistance { get; private set; } = float.PositiveInfinity;
    public float MinimumReachDeficit { get; private set; } = float.PositiveInfinity;
    public Vector3 ArmPosition => arm != null ? arm.position : transform.position;
    public Vector3 ClosestShoulderPosition { get; private set; }
    public Vector3 ClosestPawPosition { get; private set; }
    public Vector3 ClosestRootPosition { get; private set; }
    public float ClosestPhase { get; private set; }

    public bool BeginStroke(PaperSpinActivity activity, int index, bool left)
    {
        Clear();
        if (activity == null || !activity.IsRunning || !activity.BelongsTo(GetComponent<CatMovement>())) return false;
        string side = left ? "L" : "R";
        foreach (var bone in GetComponentsInChildren<Transform>())
        {
            if (bone.name == "DEF-upper_arm." + side) arm = bone;
            if (bone.name == "DEF-forearm." + side) forearm = bone;
            if (bone.name == "DEF-hand." + side) hand = bone;
        }
        if (arm == null || forearm == null || hand == null) { Clear(); return false; }
        owner = activity; stroke = index; phase = 0;
        MinimumDistance = MinimumArmTargetDistance = MinimumReachDeficit = float.PositiveInfinity;
        UpperLength = Vector3.Distance(arm.position, forearm.position);
        ForeLength = Vector3.Distance(forearm.position, hand.position);
        return true;
    }

    public void Sample(float normalizedPhase) => phase = Mathf.Clamp01(normalizedPhase);
    void Update() => Restore();
    void LateUpdate()
    {
        if (!IsActive || !owner.isActiveAndEnabled || !owner.BelongsTo(GetComponent<CatMovement>()) ||
            arm == null || forearm == null || hand == null) { Clear(); return; }
        armPose = arm.localRotation; forearmPose = forearm.localRotation; adjusted = true;
        float weight = Mathf.SmoothStep(0f, 1f, phase < .36f ? phase / .36f : (1f - phase) / .32f);
        // Hold the actual paper surface briefly so a touch reads before the paw
        // withdraws. There is no transform translation or root/body stretching.
        if (phase >= .36f && phase <= .68f) weight = 1f;
        Vector3 surface = owner.PaperContactPosition;
        float armTarget = Vector3.Distance(arm.position, surface);
        MinimumArmTargetDistance = Mathf.Min(MinimumArmTargetDistance, armTarget);
        float deficit = armTarget - UpperLength - ForeLength;
        if (deficit < MinimumReachDeficit)
        {
            MinimumReachDeficit = deficit; ClosestShoulderPosition = arm.position;
            ClosestPawPosition = hand.position; ClosestRootPosition = transform.position; ClosestPhase = phase;
        }
        Vector3 goal = Vector3.Lerp(hand.position, surface, weight);
        for (int iteration = 0; iteration < 14; iteration++)
        {
            Aim(forearm, goal);
            Aim(arm, goal);
        }
        Distance = Vector3.Distance(hand.position, surface);
        MinimumDistance = Mathf.Min(MinimumDistance, Distance);
        // Time selects a contact window; it can never substitute for the actual
        // breed-scaled hand reaching the measured roll surface after the solve.
        if (!contacted && Time.deltaTime > 0f && phase >= .32f && phase <= .72f && Distance <= ContactTolerance)
        {
            contacted = owner.AcceptPaperContact(this, stroke, hand.position, Distance);
        }
    }

    void Aim(Transform joint, Vector3 goal)
    {
        Vector3 from = hand.position - joint.position, to = goal - joint.position;
        if (from.sqrMagnitude < .000001f || to.sqrMagnitude < .000001f) return;
        joint.rotation = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(from, to), 18f) * joint.rotation;
    }
    void Restore()
    {
        if (!adjusted) return;
        adjusted = false;
        if (arm != null) arm.localRotation = armPose;
        if (forearm != null) forearm.localRotation = forearmPose;
    }
    public void Clear()
    {
        Restore(); owner = null; arm = forearm = hand = null;
        contacted = false; phase = 0; stroke = 0; Distance = float.PositiveInfinity;
    }
    void OnDisable() => Clear();
}
