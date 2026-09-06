using System.Collections;
using UnityEngine;

/// <summary>
/// Drink from the bathroom vanity tap.
///
/// The counter stands 0.83 above the floor and the cat has no jump, so like the
/// star tipi and the shower this is scripted: the controller is switched off,
/// the cat is lifted onto the counter, laps at the running water and hops back
/// down, ending outside the product footprint before physics resumes.
///
/// Thirst is topped up with <see cref="ThirstSystem.RestoreThirst"/> rather than
/// BeginDrinking, because BeginDrinking fires Drank, which already records a
/// Drink quest — and CatActivity records one on completion too.
/// </summary>
[DisallowMultipleComponent]
public sealed class SinkSipActivity : CatActivity
{
    [Header("Sink sip")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform perchPoint;
    [SerializeField, Min(0.5f)] private float sipDuration = 2.4f;
    [SerializeField, Min(0f)] private float thirstRestore = 45f;
    [SerializeField, Range(0f, 100f)] private float notThirstyAbove = 96f;

    private CharacterController characterController;
    private ThirstSystem thirst;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "SIPPING..." : string.Empty;

    public float SipDuration => Mathf.Max(0.5f, sipDuration);
    public float ThirstRestore => Mathf.Max(0f, thirstRestore);
    public float NotThirstyAbove => Mathf.Clamp(notThirstyAbove, 0f, 100f);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || perchPoint == null)
        {
            failureReason = "THE TAP IS NOT READY";
            return false;
        }

        if (thirst == null)
            thirst = FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (thirst != null && thirst.CurrentThirst >= NotThirstyAbove)
        {
            failureReason = "I AM NOT THIRSTY!";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(SipRoutine());
        return true;
    }

    private IEnumerator SipRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 floor = Flatten(floorPoint.position, start.y);
        Vector3 perch = perchPoint.position;

        Quaternion toFloor = LookTowards(floor - start, startRotation);
        yield return Move(start, floor, startRotation, toFloor, 0.30f);

        Quaternion inward = LookTowards(
            new Vector3(perch.x - floor.x, 0f, perch.z - floor.z), toFloor);
        yield return Move(floor, floor, toFloor, inward, 0.18f);

        // Crouch, then arc onto the counter: a straight lerp up reads as an
        // elevator, not a hop.
        Vector3 crouched = originalScale;
        crouched.y *= 0.78f;
        crouched.x *= 1.08f;
        crouched.z *= 1.08f;
        yield return Squash(originalScale, crouched, 0.16f);
        yield return Squash(crouched, originalScale, 0.10f);
        yield return Hop(floor, perch, inward, 0.42f);

        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Drink, perchPoint);
        while (elapsed < SipDuration)
        {
            elapsed += Time.deltaTime;
            // Lapping: a small nose dip towards the basin.
            float lap = Mathf.Abs(Mathf.Sin(elapsed * 6.2f));
            Vector3 position = perch;
            position.y -= lap * 0.030f;
            Cat.transform.position = position;
            Cat.transform.rotation = inward * Quaternion.Euler(lap * 9f, 0f, 0f);
            yield return null;
        }

        Cat.transform.rotation = inward;
        Quaternion outward = LookTowards(floor - perch, inward);
        yield return Move(perch, perch, inward, outward, 0.20f);
        yield return Hop(perch, floor, outward, 0.38f);

        RestoreCat();
        if (thirst != null)
            thirst.RestoreThirst(ThirstRestore);
        CompleteActivity("REFRESHING!");
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
    public void EditorConfigureSip(
        Transform floor, Transform perch, float duration, float restore, float thirstyBelow)
    {
        floorPoint = floor;
        perchPoint = perch;
        sipDuration = Mathf.Max(0.5f, duration);
        thirstRestore = Mathf.Max(0f, restore);
        notThirstyAbove = Mathf.Clamp(thirstyBelow, 0f, 100f);
    }
#endif
}
