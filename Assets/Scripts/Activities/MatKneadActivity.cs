using System.Collections;
using UnityEngine;

/// <summary>Gently knead and rest at the player's position on a shallow mat.</summary>
[DisallowMultipleComponent]
public sealed class MatKneadActivity : CatActivity
{
    [Header("Mat knead")]
    [SerializeField] private Transform padPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(1)] private int kneadCount = 6;
    [SerializeField, Min(0.1f)] private float kneadInterval = 0.34f;
    [SerializeField, Min(0.2f)] private float flopDuration = 1.4f;
    [SerializeField, Min(0f)] private float energyRestore = 8f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatGentleKneadMotion gentleKnead;
    private CatGroundRestMotion groundMotion;

    public const float UseRadius = .16f;
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        return padPoint != null && exitPoint != null && CatActivityStartResolver.Current(actor,
            padPoint.position, Mathf.Min(UseRadius, InteractionRadius), out start);
    }
    public override bool SupportsContinuousRest => true;
    public bool UsesGentleKneading => true;
    public override string ProgressLabel => IsRunning ? "KNEADING..." : string.Empty;
    public int KneadCount => Mathf.Max(1, kneadCount);
    public float KneadInterval => Mathf.Max(0.1f, kneadInterval);
    public float FlopDuration => Mathf.Max(0.2f, flopDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);

    public override bool TryGetPromptDistance(CatMovement cat, out float distance)
    {
        distance = float.PositiveInfinity;
        return base.TryGetPromptDistance(cat, out distance);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (padPoint == null || exitPoint == null)
        {
            failureReason = "THE MAT IS NOT READY";
            return false;
        }
        bool ready = TryGetPromptDistance(Cat, out _);
        failureReason = ready ? string.Empty : "LET'S GET A LITTLE CLOSER!";
        return ready;
    }

    protected override bool BeginActivity()
    {
        if (!TryGetPromptDistance(Cat, out _)) return false;
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        groundMotion = new CatGroundRestMotion(this, Cat, padPoint.position.y);
        StartCoroutine(KneadRoutine());
        return true;
    }

    private IEnumerator KneadRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        // The source neutral pose keeps every breed's dimensions. Its front
        // paws receive alternating shallow lifts without moving the root.
        yield return groundMotion.Pose(CatActivityPose.GentleKnead, .24f);
        gentleKnead = Cat.GetComponent<CatGentleKneadMotion>() ?? Cat.gameObject.AddComponent<CatGentleKneadMotion>();
        float elapsed = 0f, duration = KneadCount * CatGentleKneadMotion.PressSeconds;
        groundMotion.Play(CatActivityPose.GentleKnead);
        while (elapsed < duration)
        {
            groundMotion.Hold();
            gentleKnead.Sample(this, elapsed, duration);
            yield return null;
            if (Time.timeScale > 0f) elapsed += Time.deltaTime;
        }
        gentleKnead.Clear();
        yield return groundMotion.Pose(CatActivityPose.SitDown, .55f);
        yield return groundMotion.Pose(CatActivityPose.TowelSettle, .90f);
        groundMotion.Play(CatActivityPose.Sleep);
        while (KeepResting) { groundMotion.Hold(); yield return null; }
        yield return groundMotion.Pose(CatActivityPose.TowelWake, .80f);
        yield return groundMotion.Pose(CatActivityPose.StandUp, .55f);

        Vector3 exit = exitPoint.position;
        exit.y = groundMotion.Position.y;
        groundMotion.End();
        groundMotion = null;
        // Leaving after the player asks to get up is part of the routine. A
        // newly blocked exit never forces the cat out of its safe current spot.
        if (CatActivityMotion.IsControllerFloorClear(Cat, exit) &&
            CatActivityMotion.TryFloorPath(Cat, Cat.transform.position, exit, out var path))
        {
            foreach (Vector3 point in path)
            {
                Vector3 destination = point;
                destination.y = exit.y;
                Vector3 direction = destination - Cat.transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude < .0001f) continue;
                yield return CatActivityMotion.WalkAuthoredStep(Cat, destination,
                    Quaternion.LookRotation(direction.normalized), .18f);
            }
        }
        RestoreCat();
        CompleteActivity("MAKING BISCUITS!");
    }

    private void RestoreCat()
    {
        if (gentleKnead != null) gentleKnead.Clear();
        if (groundMotion != null) { groundMotion.End(); groundMotion = null; }
        if (Cat == null) return;
        if (originalScale.sqrMagnitude > .0001f) Cat.transform.localScale = originalScale;
        if (characterController != null) characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (HasBegunActivity) RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureKnead(
        Transform pad, Transform exit, int kneads, float interval, float flop, float restore)
    {
        padPoint = pad;
        exitPoint = exit;
        kneadCount = Mathf.Max(1, kneads);
        kneadInterval = Mathf.Max(0.1f, interval);
        flopDuration = Mathf.Max(0.2f, flop);
        energyRestore = Mathf.Max(0f, restore);
    }
#endif
}
