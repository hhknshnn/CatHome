using System.Collections;
using UnityEngine;

/// <summary>
/// Pat the glass on the nightstand until it goes over the edge.
///
/// The most cat thing in the whole house, and the only routine where the product
/// loses. The glass is its own FBX on the nightstand's origin — like the toilet
/// paper roll — so it can hang under a pivot the routine drives: two exploratory
/// taps that only rock it, then a third that pushes it off, a fall, a bounce and
/// a settle on its side.
///
/// The glass is put back exactly where it started when the routine ends. A prop
/// that stayed on the floor would be somewhere the catalog never placed it, and
/// the next cat to walk past would knock a glass that was already down.
///
/// The cat itself stays on its own feet; the controller is switched off only so
/// the reach can push it through the nightstand's collider.
/// </summary>
[DisallowMultipleComponent]
public sealed class KnockOffActivity : CatActivity
{
    [Header("Knock off")]
    [SerializeField] private Transform reachPoint;
    [SerializeField] private Transform glassPivot;
    [SerializeField] private Vector3 fallDirection = Vector3.forward;
    [SerializeField, Min(0.05f)] private float fallHeight = 0.55f;
    [SerializeField, Min(1)] private int teaseCount = 2;

    private CharacterController characterController;
    private Vector3 pivotHome;
    private Quaternion pivotHomeRotation;
    private bool pivotHomeCaptured;

    public override string ProgressLabel => IsRunning ? "PATTING..." : string.Empty;

    public float FallHeight => Mathf.Max(0.05f, fallHeight);
    public int TeaseCount => Mathf.Max(1, teaseCount);
    public Transform GlassPivot => glassPivot;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (reachPoint == null || glassPivot == null)
        {
            failureReason = "NOTHING TO PUSH";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        if (!pivotHomeCaptured)
        {
            pivotHome = glassPivot.localPosition;
            pivotHomeRotation = glassPivot.localRotation;
            pivotHomeCaptured = true;
        }
        StartCoroutine(KnockRoutine());
        return true;
    }

    private IEnumerator KnockRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 reach = Flatten(reachPoint.position, start.y);

        Quaternion toReach = LookTowards(reach - start, startRotation);
        yield return Move(start, reach, startRotation, toReach, 0.34f);

        Vector3 push = fallDirection.sqrMagnitude > 0.0001f
            ? fallDirection.normalized
            : Vector3.forward;
        Quaternion facing = LookTowards(push, toReach);
        yield return Move(reach, reach, toReach, facing, 0.20f);

        // Tease: paw out, the glass rocks and settles. Twice, so the third one
        // reads as the cat deciding rather than as an accident.
        for (int i = 0; i < TeaseCount; i++)
        {
            float tap = 0f;
            PlayCatPose(CatActivityPose.Paw);
            while (tap < 0.42f)
            {
                tap += Time.deltaTime;
                float t = Mathf.Clamp01(tap / 0.42f);
                float paw = Mathf.Sin(t * Mathf.PI);
                float rock = Mathf.Sin(t * Mathf.PI * 2.4f) * (1f - t);
                Cat.transform.position = reach + push * (paw * 0.060f);
                Cat.transform.rotation = facing * Quaternion.Euler(-paw * 18f, 0f, 0f);
                glassPivot.localPosition = pivotHome + push * (paw * 0.022f);
                glassPivot.localRotation = pivotHomeRotation * Quaternion.AngleAxis(
                    rock * 11f, Vector3.Cross(Vector3.up, push));
                yield return null;
            }

            glassPivot.localPosition = pivotHome;
            glassPivot.localRotation = pivotHomeRotation;
            yield return Wait(0.22f);
        }

        // The push that does it.
        float shove = 0f;
        while (shove < 0.24f)
        {
            shove += Time.deltaTime;
            float t = Mathf.Clamp01(shove / 0.24f);
            Cat.transform.position = reach + push * (Mathf.Sin(t * Mathf.PI) * 0.085f);
            Cat.transform.rotation = facing * Quaternion.Euler(-t * 24f, 0f, 0f);
            glassPivot.localPosition = pivotHome + push * (t * 0.130f);
            yield return null;
        }

        // The fall: out and down, tumbling as it goes.
        Vector3 lipPosition = pivotHome + push * 0.130f;
        Vector3 floorPosition = lipPosition + push * 0.140f - Vector3.up * FallHeight;
        Vector3 axis = Vector3.Cross(Vector3.up, push);
        float fall = 0f;
        PlayCatPose(CatActivityPose.Sit);
        while (fall < 0.34f)
        {
            fall += Time.deltaTime;
            float t = Mathf.Clamp01(fall / 0.34f);
            // Gravity, not a lerp: a constant-speed fall reads as a lift.
            glassPivot.localPosition = Vector3.Lerp(lipPosition, floorPosition,
                                                    t * t);
            glassPivot.localRotation =
                pivotHomeRotation * Quaternion.AngleAxis(t * 82f, axis);
            // The cat leans over the edge to watch it go.
            Cat.transform.rotation = facing * Quaternion.Euler(24f + t * 12f, 0f, 0f);
            yield return null;
        }

        // One small bounce, then it lies still on its side.
        float bounce = 0f;
        while (bounce < 0.36f)
        {
            bounce += Time.deltaTime;
            float t = Mathf.Clamp01(bounce / 0.36f);
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.6f)) * (1f - t) * 0.070f;
            glassPivot.localPosition = floorPosition + Vector3.up * hop;
            glassPivot.localRotation =
                pivotHomeRotation * Quaternion.AngleAxis(82f + t * 8f, axis);
            Cat.transform.rotation = facing * Quaternion.Euler(30f, 0f, 0f);
            yield return null;
        }

        yield return Wait(0.30f);
        Cat.transform.rotation = facing;
        Quaternion away = LookTowards(start - reach, facing);
        yield return Move(reach, reach, facing, away, 0.24f);

        RestoreCat();
        CompleteActivity("OOPS.");
    }

    private static IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
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

    private void RestoreCat()
    {
        // The glass goes back on the nightstand. It has to: the catalog placed
        // it there, and a glass left on the floor would be knocked twice.
        if (glassPivot != null && pivotHomeCaptured)
        {
            glassPivot.localPosition = pivotHome;
            glassPivot.localRotation = pivotHomeRotation;
        }

        if (Cat == null)
            return;

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
    public void EditorConfigureKnock(
        Transform reach, Transform pivot, Vector3 direction, float drop, int teases)
    {
        reachPoint = reach;
        glassPivot = pivot;
        fallDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized : Vector3.forward;
        fallHeight = Mathf.Max(0.05f, drop);
        teaseCount = Mathf.Max(1, teases);
    }
#endif
}
