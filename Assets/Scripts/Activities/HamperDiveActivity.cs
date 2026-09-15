using System.Collections;
using UnityEngine;

/// <summary>
/// Dive into the laundry hamper, sink into the pile, pop back up and hop out.
///
/// The hamper rim stands 0.78 above the floor once the catalog has scaled it, so
/// this is a hop like the vanity sip rather than a walk-in: the controller is
/// switched off, the cat arcs over the rim, sinks below the laundry line for a
/// beat, pops its head back up, and hops out. It is never re-parented under the
/// product, and it ends outside the footprint before physics resumes.
///
/// The sink is done by lowering the cat and squashing it, not by hiding the
/// renderer: a cat that vanishes reads as a bug, a cat that sinks reads as a
/// cat in a laundry basket.
/// </summary>
[DisallowMultipleComponent]
public sealed class HamperDiveActivity : CatActivity
{
    [Header("Hamper dive")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform pilePoint;
    [SerializeField, Min(0.2f)] private float sinkDepth = 0.26f;
    [SerializeField, Min(0.5f)] private float hideDuration = 1.5f;
    [SerializeField, Min(0f)] private float energyRestore = 12f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatSupportedFurnitureMotion supportedMotion;

    public override string ProgressLabel => IsRunning ? "DIVING..." : string.Empty;

    public float SinkDepth => Mathf.Max(0.2f, sinkDepth);
    public float HideDuration => Mathf.Max(0.5f, hideDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);
    public bool IsHiding { get; private set; }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || pilePoint == null)
        {
            failureReason = "THE HAMPER IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(DiveRoutine());
        return true;
    }

    private IEnumerator DiveRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 floor = Flatten(floorPoint.position, start.y);
        Vector3 pile = pilePoint.position;

        Quaternion toFloor = LookTowards(floor - start, startRotation);
        yield return Move(start, floor, startRotation, toFloor, 0.30f);

        Quaternion inward = LookTowards(
            new Vector3(pile.x - floor.x, 0f, pile.z - floor.z), toFloor);
        yield return Move(floor, floor, toFloor, inward, 0.18f);

        // Jump already supplies the real skeletal crouch. The old root-scale
        // squash was restored by CatActivityAnimation each frame and left a
        // motionless, rear-facing knead between the turn and take-off.
        var support = pilePoint.GetComponent<CatActivitySurface>();
        inward = support != null && support.AlignAlongSurface ?
            CatActivityFacing.AlongAxis(Cat, pile, pilePoint.rotation * Quaternion.Euler(0,90,0)) :
            CatActivityFacing.Resolve(Cat, pile, inward);
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, pilePoint);
        yield return supportedMotion.Jump(floor, pile, Cat.transform.rotation, inward);
        yield return supportedMotion.Pose(CatActivityPose.SitDown, .55f, pile, inward);
        yield return supportedMotion.Pose(CatActivityPose.TowelSettle, .85f, pile, inward);

        // Sink: down into the pile and squashed wide, so the laundry looks like
        // it swallowed the cat.
        Vector3 sunk = pile;
        sunk.y -= SinkDepth;
        Vector3 buried = originalScale;
        buried.y *= 0.55f;
        buried.x *= 1.14f;
        buried.z *= 1.14f;
        float dive = 0f;
        while (dive < 0.26f)
        {
            dive += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(dive / 0.26f));
            Cat.transform.position = Vector3.Lerp(pile, sunk, t);
            Cat.transform.localScale = Vector3.Lerp(originalScale, buried, t);
            yield return null;
        }

        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Sleep, pilePoint);
        IsHiding = true;
        while (elapsed < HideDuration)
        {
            elapsed += Time.deltaTime;
            // Rummaging: a small shuffle, and one peek near the end.
            float peek = elapsed > HideDuration * 0.60f
                ? Mathf.Sin((elapsed - HideDuration * 0.60f) * 5.4f)
                : 0f;
            Vector3 position = sunk;
            position.y += Mathf.Max(0f, peek) * SinkDepth * 0.55f;
            position.x += Mathf.Sin(elapsed * 7.3f) * 0.018f;
            Cat.transform.position = position;
            Cat.transform.rotation = inward * Quaternion.Euler(0f, Mathf.Sin(elapsed * 4.1f) * 9f, 0f);
            yield return null;
        }

        IsHiding = false;
        Cat.transform.rotation = inward;
        float rise = 0f;
        while (rise < 0.24f)
        {
            rise += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rise / 0.24f));
            Cat.transform.position = Vector3.Lerp(sunk, pile, t);
            Cat.transform.localScale = Vector3.Lerp(buried, originalScale, t);
            yield return null;
        }

        Quaternion outward = LookTowards(floor - pile, inward);
        yield return supportedMotion.Pose(CatActivityPose.TowelWake, .80f, pile, inward);
        yield return supportedMotion.Pose(CatActivityPose.StandUp, .55f, pile, inward);
        yield return supportedMotion.Jump(pile, floor, inward, outward);

        RestoreCat();
        if (Energy != null)
            Energy.RestoreEnergy(EnergyRestore);
        CompleteActivity("FOUND ME!");
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        yield return CatActivityMotion.Jump(Cat,from,to,Cat.transform.rotation,facing);
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        yield return CatActivityMotion.WalkAuthoredStep(Cat, to, toRotation, duration);
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
        supportedMotion?.End(); supportedMotion = null;
        IsHiding = false;
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
    public void EditorConfigureDive(
        Transform floor, Transform pile, float depth, float hide, float restore)
    {
        floorPoint = floor;
        pilePoint = pile;
        sinkDepth = Mathf.Max(0.2f, depth);
        hideDuration = Mathf.Max(0.5f, hide);
        energyRestore = Mathf.Max(0f, restore);
    }
#endif
}
