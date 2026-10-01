using System.Collections;
using UnityEngine;

/// <summary>Rest in the nearby warm spot without a scripted entrance or a body turn.</summary>
[DisallowMultipleComponent]
public sealed class OvenWarmthActivity : CatActivity
{
    [Header("Oven warmth")]
    [SerializeField] private Transform baskPoint;
    [SerializeField] private Transform doorPoint;
    [SerializeField, Min(0.5f)] private float baskDuration = 4.2f;
    [SerializeField, Min(0f)] private float energyRestore = 30f;
    [SerializeField, Range(0f, 100f)] private float wideAwakeEnergy = 92f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatGroundRestMotion groundMotion;
    private CatWarmthFeedback warmthFeedback;

    public const float UseRadius = .24f;
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        return baskPoint != null && doorPoint != null && CatActivityStartResolver.Current(actor,
            baskPoint.position, Mathf.Min(UseRadius, InteractionRadius), out start);
    }
    public override string ProgressLabel => IsRunning ? "WARMING UP..." : string.Empty;
    public float BaskDuration => Mathf.Max(0.5f, baskDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);
    public float WideAwakeEnergy => Mathf.Clamp(wideAwakeEnergy, 0f, 100f);
    public override float EnergyCost => 0f;
    public override bool SupportsContinuousRest => true;

    public override bool TryGetPromptDistance(CatMovement cat, out float distance)
    {
        distance = float.PositiveInfinity;
        return base.TryGetPromptDistance(cat, out distance);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (baskPoint == null || doorPoint == null)
        {
            failureReason = "THE OVEN IS NOT READY";
            return false;
        }
        bool ready = TryGetPromptDistance(Cat, out _);
        failureReason = ready ? string.Empty : "LET'S GET A LITTLE CLOSER!";
        return ready;
    }

    protected override bool BeginActivity()
    {
        // A click can arrive after the frame which displayed the prompt.
        if (!TryGetPromptDistance(Cat, out _)) return false;
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        groundMotion = new CatGroundRestMotion(this, Cat, baskPoint.position.y);
        StartCoroutine(BaskRoutine());
        return true;
    }

    private IEnumerator BaskRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        yield return groundMotion.Pose(CatActivityPose.SitDown, .55f);
        yield return groundMotion.Pose(CatActivityPose.TowelSettle, .90f);
        groundMotion.Play(CatActivityPose.Sleep);
        warmthFeedback = Cat.GetComponent<CatWarmthFeedback>() ?? Cat.gameObject.AddComponent<CatWarmthFeedback>();
        warmthFeedback.Begin(this, Cat);
        while (KeepResting) { groundMotion.Hold(); yield return null; }
        warmthFeedback.Stop();
        yield return groundMotion.Pose(CatActivityPose.TowelWake, .80f);
        yield return groundMotion.Pose(CatActivityPose.StandUp, .55f);
        RestoreCat();
        CompleteActivity("TOASTY!");
    }

    private void RestoreCat()
    {
        warmthFeedback?.Stop();
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
    public void EditorConfigureBask(
        Transform bask, Transform door, float duration, float restore, float awakeAbove)
    {
        baskPoint = bask;
        doorPoint = door;
        baskDuration = Mathf.Max(0.5f, duration);
        energyRestore = Mathf.Max(0f, restore);
        wideAwakeEnergy = Mathf.Clamp(awakeAbove, 0f, 100f);
    }
#endif
}
