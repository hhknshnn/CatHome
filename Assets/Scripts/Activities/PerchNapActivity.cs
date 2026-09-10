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
        StartCoroutine(PerchRoutine());
        return true;
    }

    private IEnumerator PerchRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 floor = Flatten(floorPoint.position, start.y);
        Vector3 perch = perchPoint.position;

        Quaternion toFloor = LookTowards(floor - start, startRotation);
        yield return Move(start, floor, startRotation, toFloor, 0.32f);

        Quaternion inward = LookTowards(
            new Vector3(perch.x - floor.x, 0f, perch.z - floor.z), toFloor);
        yield return Move(floor, floor, toFloor, inward, 0.18f);

        Vector3 crouched = originalScale;
        crouched.y *= 0.76f;
        crouched.x *= 1.09f;
        crouched.z *= 1.09f;
        yield return Squash(originalScale, crouched, 0.17f);
        yield return Squash(crouched, originalScale, 0.09f);
        yield return Hop(floor, perch, inward, 0.44f);

        // Turn to face the room, then loaf: front paws tucked, breathing.
        Quaternion outward = LookTowards(floor - perch, inward);
        Quaternion restingFacing = CatActivityFacing.AlongAxis(Cat, perch, outward);
        yield return Move(perch, perch, inward, restingFacing, 0.26f);
        Vector3 loafed = originalScale;
        loafed.y *= 0.68f;
        loafed.x *= 1.08f;
        loafed.z *= 1.08f;
        yield return Squash(originalScale, loafed, 0.26f);

        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Sleep, perchPoint);
        while (KeepResting)
        {
            elapsed += Time.deltaTime;
            float breath = Mathf.Sin(elapsed * 3.0f) * 0.032f;
            Vector3 breathing = loafed;
            breathing.y *= 1f + breath;
            breathing.x *= 1f - breath * 0.4f;
            breathing.z *= 1f - breath * 0.4f;
            Cat.transform.localScale = breathing;
            Cat.transform.position = perch;
            Cat.transform.rotation = restingFacing;
            yield return null;
        }

        yield return Squash(Cat.transform.localScale, originalScale, 0.22f);
        yield return Hop(perch, floor, outward, 0.40f);

        RestoreCat();
        CompleteActivity(string.IsNullOrWhiteSpace(completeMessage)
            ? "GOOD SPOT!" : completeMessage);
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        yield return CatActivityMotion.Jump(Cat,from,to,Cat.transform.rotation,facing);
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        PlayCatPose(CatActivityPose.Walk);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Cat.transform.position = Vector3.Lerp(from, to, t);
            Cat.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);
            yield return null;
        }

        Cat.transform.position = to;
        Cat.transform.rotation = toRotation;
    }

    private IEnumerator Squash(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Cat.transform.localScale =
                Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }

        Cat.transform.localScale = to;
    }

    private void RestoreCat()
    {
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
