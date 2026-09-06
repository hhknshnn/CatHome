using System.Collections;
using UnityEngine;

/// <summary>
/// Nap in the bathroom towel cabinet's open niche.
///
/// This is the bathroom's sleep beat: the room already owns two water beats
/// (<see cref="ShowerRinseActivity"/>, <see cref="SinkSipActivity"/>) and one
/// play beat (<see cref="PaperSpinActivity"/>).
///
/// The folded towel bed is 0.90 above the cabinet floor and the cat has no
/// jump, so like the vanity sip this is scripted: the controller is switched
/// off, the cat is hopped onto the stack, curled up while it breathes, and
/// hopped back down, ending outside the product footprint before physics
/// resumes. The cat is never re-parented under the cabinet.
/// </summary>
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

    public override string ProgressLabel => IsRunning ? "NAPPING..." : string.Empty;

    public float NapDuration => Mathf.Max(0.5f, napDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);
    public float WideAwakeEnergy => Mathf.Clamp(wideAwakeEnergy, 0f, 100f);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || nestPoint == null)
        {
            failureReason = "THE NICHE IS NOT READY";
            return false;
        }

        if (Energy != null && Energy.CurrentEnergy >= WideAwakeEnergy)
        {
            failureReason = "I AM WIDE AWAKE!";
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
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 floor = Flatten(floorPoint.position, start.y);
        Vector3 nest = nestPoint.position;

        Quaternion toFloor = LookTowards(floor - start, startRotation);
        yield return Move(start, floor, startRotation, toFloor, 0.32f);

        Quaternion inward = LookTowards(
            new Vector3(nest.x - floor.x, 0f, nest.z - floor.z), toFloor);
        yield return Move(floor, floor, toFloor, inward, 0.18f);

        // Crouch, then arc into the niche: a straight lerp up reads as an
        // elevator, not a hop.
        Vector3 crouched = originalScale;
        crouched.y *= 0.78f;
        crouched.x *= 1.08f;
        crouched.z *= 1.08f;
        yield return Squash(originalScale, crouched, 0.16f);
        yield return Squash(crouched, originalScale, 0.10f);
        yield return Hop(floor, nest, inward, 0.44f);

        // Turn around so the cat sleeps facing out of the niche.
        Quaternion outward = LookTowards(floor - nest, inward);
        yield return Move(nest, nest, inward, outward, 0.26f);

        Vector3 curled = originalScale;
        curled.y *= 0.60f;
        curled.x *= 1.12f;
        curled.z *= 1.12f;
        yield return Squash(originalScale, curled, 0.28f);

        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Sleep, nestPoint);
        while (elapsed < NapDuration)
        {
            elapsed += Time.deltaTime;
            float breath = Mathf.Sin(elapsed * 3.1f) * 0.035f;
            Vector3 breathing = curled;
            breathing.y *= 1f + breath;
            breathing.x *= 1f - breath * 0.4f;
            breathing.z *= 1f - breath * 0.4f;
            Cat.transform.localScale = breathing;
            Cat.transform.position = nest;
            Cat.transform.rotation = outward;
            yield return null;
        }

        yield return Squash(Cat.transform.localScale, originalScale, 0.24f);
        yield return Hop(nest, floor, outward, 0.40f);

        RestoreCat();
        if (Energy != null)
            Energy.RestoreEnergy(EnergyRestore);
        CompleteActivity("SWEET DREAMS!");
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        PlayCatPose(CatActivityPose.Hop);
        float peak = Mathf.Max(from.y, to.y) + 0.22f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = CatActivityMotion.JumpPosition(from, to, t, peak - Mathf.Max(from.y, to.y));
            Cat.transform.position = position;
            Cat.transform.rotation = facing;
            yield return null;
        }

        Cat.transform.position = to;
        Cat.transform.rotation = facing;
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

    protected override void OnDisable()
    {
        StopAllCoroutines();
        RestoreCat();
        base.OnDisable();
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
