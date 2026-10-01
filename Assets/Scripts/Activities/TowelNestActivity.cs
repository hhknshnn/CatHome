using System.Collections;
using UnityEngine;

public enum CatTowelPhase { None, Approach, CrouchUp, Rising, LandOnShelf, Settling, Resting, Waking, CrouchDown, Descending, LandOnFloor }

/// <summary>Native high jumps, supported settling and a soft return to the bathroom floor.</summary>
[DisallowMultipleComponent]
public sealed class TowelNestActivity : CatActivity
{
    [Header("Towel nest")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform nestPoint;
    [SerializeField, Min(0.5f)] private float napDuration = 3.4f;
    [SerializeField, Min(0f)] private float energyRestore = 26f;
    [SerializeField, Range(0f, 100f)] private float wideAwakeEnergy = 92f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private Transform jumpSupport;
    public CatTowelPhase Phase { get; private set; }
    public Vector3 JumpStart { get; private set; }
    public Vector3 JumpEnd { get; private set; }
    public float FlightProgress { get; private set; }
    public Transform NestPoint => nestPoint;
    public Transform FloorPoint => floorPoint;

    public override string ProgressLabel => IsRunning ? "NAPPING..." : string.Empty;

    public float NapDuration => Mathf.Max(0.5f, napDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);
    public float WideAwakeEnergy => Mathf.Clamp(wideAwakeEnergy, 0f, 100f);
    public override float EnergyCost => 0f;
    public override bool SupportsContinuousRest=>true;
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        return floorPoint != null && nestPoint != null &&
            CatActivityStartResolver.GroundLaunch(this, actor, floorPoint.position, nestPoint.position, out start);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || nestPoint == null)
        {
            failureReason = "THE NICHE IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(NestRoutine());
        return true;
    }

    private IEnumerator NestRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Phase = CatTowelPhase.Approach;
        Vector3 floor = Flatten(floorPoint.position, AcceptedStart.Position.y), nest = nestPoint.position;
        Quaternion inward = AcceptedStart.Rotation;

        var anchor = new GameObject("Towel jump support") { hideFlags = HideFlags.DontSave };
        jumpSupport = anchor.transform; jumpSupport.SetParent(transform, true);
        jumpSupport.SetPositionAndRotation(AcceptedStart.Position, nestPoint.rotation);
        anchor.AddComponent<CatActivitySurface>();
        var measured = Cat.GetComponent<CatMeasuredSupportMotion>() ?? Cat.gameObject.AddComponent<CatMeasuredSupportMotion>();
        measured.Bind(this, nestPoint);
        Quaternion restingFacing = CatActivityFacing.AlongAxis(Cat, nest,
            CatActivityFacing.SupportedAxis(nestPoint));
        yield return Jump(AcceptedStart.Position, nest, inward, restingFacing, true);
        yield return PoseSegment(CatTowelPhase.Settling, CatActivityPose.SitDown, 0f, 1f, .65f, nest, restingFacing);
        yield return PoseSegment(CatTowelPhase.Settling, CatActivityPose.TowelSettle, 0f, 1f, 1.1f, nest, restingFacing);
        PlayCatPose(CatActivityPose.Sleep, nestPoint);
        Phase = CatTowelPhase.Resting;
        while (KeepResting)
        {
            Cat.transform.SetPositionAndRotation(nest, restingFacing);
            Cat.transform.localScale = originalScale;
            yield return null;
        }
        // Stand up on the same support before preparing the downward jump.
        yield return PoseSegment(CatTowelPhase.Waking, CatActivityPose.TowelWake, 0f, 1f, .85f, nest, restingFacing);
        yield return PoseSegment(CatTowelPhase.Waking, CatActivityPose.StandUp, 0f, 1f, .60f, nest, restingFacing);
        Quaternion outward = LookTowards(floor - nest, restingFacing);
        yield return Jump(nest, floor, restingFacing, outward, false);
        RestoreCat();
        CompleteActivity("SWEET DREAMS!");
    }

    private IEnumerator PoseSegment(CatTowelPhase stage, CatActivityPose pose, float first, float last,
        float seconds, Vector3 position, Quaternion rotation, float firstBlend = 1f, float lastBlend = 1f)
    {
        Phase = stage;
        var animation = Cat.GetComponent<CatActivityAnimation>();
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            Cat.transform.SetPositionAndRotation(position, rotation);
            jumpSupport.position = position;
            float sample = Mathf.Lerp(first, last, t);
            animation.SetTimedPose(pose, pose == CatActivityPose.TowelWake ? 1f - sample : sample, jumpSupport);
            animation.SetHorizontalSupportBlend(Mathf.SmoothStep(firstBlend, lastBlend, t));
            yield return null;
            elapsed += Time.deltaTime;
        }
        animation.SetTimedPose(pose, pose == CatActivityPose.TowelWake ? 1f - last : last, jumpSupport);
        animation.SetHorizontalSupportBlend(lastBlend);
    }

    private IEnumerator Jump(Vector3 from, Vector3 to, Quaternion startRotation, Quaternion arrival, bool up)
    {
        JumpStart = from; JumpEnd = to; FlightProgress = 0f;
        yield return CatJumpMotion.Play(Cat, from, to, startRotation, arrival, up, .20f,
            (stage, progress) =>
            {
                FlightProgress = progress;
                Phase = stage == 0 ? (up ? CatTowelPhase.CrouchUp : CatTowelPhase.CrouchDown) :
                    stage == 1 ? (up ? CatTowelPhase.Rising : CatTowelPhase.Descending) :
                    (up ? CatTowelPhase.LandOnShelf : CatTowelPhase.LandOnFloor);
            });
        jumpSupport.position = to;
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        yield return CatActivityMotion.WalkAuthoredStep(Cat, to, toRotation, duration);
    }

    private void RestoreCat()
    {
        Phase = CatTowelPhase.None;
        if (Cat != null) Cat.GetComponent<CatMeasuredSupportMotion>()?.Clear(this);
        if (jumpSupport != null)
        {
            if (Cat != null) Cat.GetComponent<CatActivityAnimation>()?.SetPose(CatActivityPose.GentleKnead);
            Destroy(jumpSupport.gameObject); jumpSupport = null;
        }
        if (Cat == null)
            return;

        if (originalScale.sqrMagnitude > 0.0001f)
            Cat.transform.localScale = originalScale;
        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    private static Quaternion LookTowards(Vector3 direction, Quaternion fallback)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }

    private static Vector3 Flatten(Vector3 point, float y)
    {
        point.y = y;
        return point;
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureNest(
        Transform floor, Transform nest, float duration, float restore, float awakeAbove)
    {
        floorPoint = floor;
        nestPoint = nest;
        napDuration = Mathf.Max(0.5f, duration);
        energyRestore = Mathf.Max(0f, restore);
        wideAwakeEnergy = Mathf.Clamp(awakeAbove, 0f, 100f);
    }
#endif
}
