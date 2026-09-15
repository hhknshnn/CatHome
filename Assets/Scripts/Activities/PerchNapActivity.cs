using System.Collections;
using UnityEngine;

/// <summary>
/// Hop onto a raised surface, settle, and hop back down.
///
/// Shared by the kitchen island counter and the counter stool, which differ only
/// in how high the perch is and how long the cat stays — the same reason the
/// window, the feather toy and the garden birds all share
/// <see cref="SitLookActivity"/>. Each product still gets its own
/// <see cref="CatActivityKind"/> so the scene validator can tell them apart.
///
/// The controller is switched off for the routine, the cat is never re-parented
/// under the product, and it lands clear of the footprint before physics
/// resumes.
/// </summary>
[DisallowMultipleComponent]
public sealed class PerchNapActivity : CatActivity
{
    [Header("Perch nap")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform perchPoint;
    [SerializeField, Min(0.5f)] private float settleDuration = 2.8f;
    [SerializeField, Min(0f)] private float energyRestore = 18f;
    [SerializeField, Range(0f, 100f)] private float wideAwakeEnergy = 94f;
    [SerializeField] private string completeMessage = "GOOD SPOT!";

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatSupportedFurnitureMotion supportedMotion;
    public Transform PerchPoint => perchPoint;
    public Transform FloorPoint => floorPoint;

    public override string ProgressLabel => IsRunning ? "SETTLING..." : string.Empty;

    public float SettleDuration => Mathf.Max(0.5f, settleDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);
    public float WideAwakeEnergy => Mathf.Clamp(wideAwakeEnergy, 0f, 100f);
    public override float EnergyCost => 0f;
    public override bool SupportsContinuousRest=>true;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || perchPoint == null)
        {
            failureReason = "NO ROOM UP THERE";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(SupportedRoutine());
        return true;
    }

    private IEnumerator SupportedRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Vector3 floor = Flatten(floorPoint.position, Cat.transform.position.y), perch = perchPoint.position;
        bool directChair = StoreProductId == "room.armchair";
        // Keep one seat position throughout the chair routine. Its front half
        // leaves room for the original jump pose's head and the short yaw.
        if (directChair) perch += transform.TransformDirection(Vector3.back) * .10f;
        Quaternion inward = LookTowards(perch - floor, Cat.transform.rotation);
        yield return CatActivityMotion.WalkAuthoredStep(Cat, floor, inward, .26f);
        var area = perchPoint.GetComponent<CatActivitySurface>();
        bool sitOnly = area != null && area.ResolvePose(CatActivityPose.Sleep) == CatActivityPose.Sit;
        Quaternion preferred = perchPoint.rotation * Quaternion.Euler(0, 90, 0);
        Quaternion facing = sitOnly ? CatActivityFacing.Resolve(Cat, perch, preferred) : CatActivityFacing.AlongAxis(Cat, perch, preferred);
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, perchPoint);
        yield return supportedMotion.Jump(floor, perch, inward, facing);
        yield return supportedMotion.Pose(CatActivityPose.SitDown, .60f, perch, facing);
        if (!sitOnly) yield return supportedMotion.Pose(CatActivityPose.TowelSettle, .95f, perch, facing);
        if (directChair) supportedMotion.Rest(sitOnly ? CatActivityPose.Sit : CatActivityPose.Sleep);
        else PlayCatPose(sitOnly ? CatActivityPose.Sit : CatActivityPose.Sleep, perchPoint);
        while (KeepResting)
        {
            Cat.transform.SetPositionAndRotation(perch, facing); Cat.transform.localScale = originalScale;
            yield return null;
        }
        if (!sitOnly) yield return supportedMotion.Pose(CatActivityPose.TowelWake, .80f, perch, facing);
        yield return supportedMotion.Pose(CatActivityPose.StandUp, .55f, perch, facing);
        yield return supportedMotion.Jump(perch, floor, facing, LookTowards(floor - perch, facing));
        RestoreCat(); CompleteActivity(string.IsNullOrWhiteSpace(completeMessage) ? "GOOD SPOT!" : completeMessage);
    }

    private void RestoreCat()
    {
        if (supportedMotion != null) { supportedMotion.End(); supportedMotion = null; }
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
    public void EditorConfigurePerch(
        Transform floor, Transform perch, float settle, float restore, float awakeAbove,
        string message)
    {
        floorPoint = floor;
        perchPoint = perch;
        settleDuration = Mathf.Max(0.5f, settle);
        energyRestore = Mathf.Max(0f, restore);
        wideAwakeEnergy = Mathf.Clamp(awakeAbove, 0f, 100f);
        completeMessage = string.IsNullOrWhiteSpace(message) ? "GOOD SPOT!" : message;
    }
#endif
}
