using System.Collections;
using UnityEngine;

/// <summary>
/// Climb the pantry: hop to the low shelf, then the middle one, sniff along the
/// jars, and come back down in two hops.
///
/// The only Kitchen routine that stages more than one height, which is what
/// makes a 1.85 shelf unit worth entering at all. Each hop is its own arc so the
/// cat visibly steps up rather than levitating the whole way.
///
/// The controller is switched off for the routine, the cat is never re-parented
/// under the product, and it lands clear of the footprint before physics
/// resumes.
/// </summary>
[DisallowMultipleComponent]
public sealed class PantryClimbActivity : CatActivity
{
    [Header("Pantry climb")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform lowerShelfPoint;
    [SerializeField] private Transform upperShelfPoint;
    [SerializeField, Min(0.5f)] private float sniffDuration = 2.4f;
    [SerializeField] private bool directClimb;

    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "EXPLORING..." : string.Empty;

    public float SniffDuration => Mathf.Max(0.5f, sniffDuration);
    public bool UsesIntermediatePerch => !directClimb;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || lowerShelfPoint == null || upperShelfPoint == null)
        {
            failureReason = "THE PANTRY IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(ClimbRoutine());
        return true;
    }

    private IEnumerator ClimbRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 floor = Flatten(floorPoint.position, start.y);
        Vector3 lower = lowerShelfPoint.position;
        Vector3 upper = upperShelfPoint.position;

        Quaternion toFloor = LookTowards(floor - start, startRotation);
        yield return Move(start, floor, startRotation, toFloor, 0.32f);

        Vector3 firstLanding = directClimb ? upper : lower;
        Quaternion inward = LookTowards(
            new Vector3(firstLanding.x - floor.x, 0f, firstLanding.z - floor.z), toFloor);
        yield return Move(floor, floor, toFloor, inward, 0.18f);

        Vector3 crouched = originalScale;
        crouched.y *= 0.76f;
        crouched.x *= 1.09f;
        crouched.z *= 1.09f;
        yield return Squash(originalScale, crouched, 0.16f);
        yield return Squash(crouched, originalScale, 0.09f);
        if (directClimb)
            yield return Hop(floor, upper, inward, .75f);
        else
        {
            yield return Hop(floor, lower, inward, 0.40f);
            yield return Squash(originalScale, crouched, 0.13f);
            yield return Squash(crouched, originalScale, 0.08f);
            yield return Hop(lower, upper, inward, 0.38f);
        }

        // Sniff along the shelf: nose left, nose right, one lean in.
        float elapsed = 0f;
        Quaternion perchFacing = LookTowards(floor - upper, inward);
        PlayCatPose(CatActivityPose.Sit, upperShelfPoint);
        while (elapsed < SniffDuration)
        {
            elapsed += Time.deltaTime;
            float sweep = Mathf.Sin(elapsed * 2.3f);
            float lean = Mathf.Abs(Mathf.Sin(elapsed * 1.1f));
            Vector3 position = upper;
            position += (perchFacing * Vector3.forward) * (lean * 0.045f);
            Cat.transform.position = position;
            Cat.transform.rotation =
                perchFacing * Quaternion.Euler(lean * 9f, sweep * 26f, 0f);
            yield return null;
        }

        Cat.transform.position = upper;
        Cat.transform.rotation = inward;
        Quaternion outward = LookTowards(floor - upper, inward);
        yield return Move(upper, upper, inward, outward, 0.24f);
        if (directClimb)
            yield return Hop(upper, floor, outward, .65f);
        else
        {
            yield return Hop(upper, lower, outward, 0.34f);
            yield return Hop(lower, floor, outward, 0.36f);
        }

        RestoreCat();
        CompleteActivity("NOTHING UP HERE!");
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        PlayCatPose(CatActivityPose.Hop);
        float peak = Mathf.Max(from.y, to.y) + 0.20f;
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
    public void EditorConfigureDirectClimb(bool value) => directClimb = value;

    public void EditorConfigureClimb(
        Transform floor, Transform lower, Transform upper, float sniff)
    {
        floorPoint = floor;
        lowerShelfPoint = lower;
        upperShelfPoint = upper;
        sniffDuration = Mathf.Max(0.5f, sniff);
    }
#endif
}
