using System.Collections;
using UnityEngine;

/// <summary>
/// Rub a cheek along the grooming cart's brush roller.
///
/// Nothing on the product moves: the beat is entirely the cat, which is why the
/// cart ships as one FBX rather than two. The cat walks up to the roller, turns
/// side-on, and drags itself along the brush a few times, leaning into it and
/// squashing slightly on each pass.
///
/// Like every other scripted activity the CharacterController is switched off
/// for the routine, the cat is never re-parented under the product, and it ends
/// up outside the footprint before physics resumes. The cart's product box
/// stays solid â€” the cat brushes past the front face, it does not walk in.
/// </summary>
[DisallowMultipleComponent]
public sealed class GroomBrushActivity : CatActivity
{
    [Header("Groom brush")]
    [SerializeField] private Transform approachPoint;
    [SerializeField] private Transform rubStartPoint;
    [SerializeField] private Transform rubEndPoint;
    [SerializeField, Min(1)] private int passCount = 3;
    [SerializeField, Min(0.2f)] private float passDuration = 0.8f;

    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "GROOMING..." : string.Empty;

    public int PassCount => Mathf.Max(1, passCount);
    public float PassDuration => Mathf.Max(0.2f, passDuration);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (approachPoint == null || rubStartPoint == null || rubEndPoint == null)
        {
            failureReason = "THE BRUSH IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(GroomRoutine());
        return true;
    }

    private IEnumerator GroomRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 approach = Flatten(approachPoint.position, start.y);
        Vector3 rubStart = Flatten(rubStartPoint.position, start.y);
        Vector3 rubEnd = Flatten(rubEndPoint.position, start.y);

        Quaternion toApproach = LookTowards(approach - start, startRotation);
        yield return Move(start, approach, startRotation, toApproach, 0.32f);

        // Side-on: the cat faces ALONG the roller, not at it, so the cheek is
        // what touches the bristles.
        Quaternion along = LookTowards(rubEnd - rubStart, toApproach);
        yield return Move(approach, rubStart, toApproach, along, 0.30f);

        for (int pass = 0; pass < PassCount; pass++)
        {
            bool forward = pass % 2 == 0;
            Vector3 from = forward ? rubStart : rubEnd;
            Vector3 to = forward ? rubEnd : rubStart;
            Quaternion facing = LookTowards(to - from, along);

            float elapsed = 0f;
            PlayCatPose(CatActivityPose.Groom);
            while (elapsed < PassDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PassDuration);
                float lean = Mathf.Sin(t * Mathf.PI);
                Cat.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                // Roll into the brush and squash: the lean is what sells contact.
                Cat.transform.rotation = facing * Quaternion.Euler(0f, 0f, lean * 11f);
                Vector3 scale = originalScale;
                scale.x *= 1f - lean * 0.060f;
                scale.y *= 1f + lean * 0.035f;
                Cat.transform.localScale = scale;
                yield return null;
            }

            Cat.transform.position = to;
            Cat.transform.localScale = originalScale;
            if (pass < PassCount - 1)
                yield return Move(to, to, facing, LookTowards(from - to, facing), 0.22f);
        }

        Vector3 exit = Cat.transform.position;
        Quaternion away = LookTowards(approach - exit, Cat.transform.rotation);
        yield return Move(exit, approach, Cat.transform.rotation, away, 0.30f);

        RestoreCat();
        CompleteActivity("SO FLUFFY!");
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
    public void EditorConfigureGroom(
        Transform approach, Transform rubStart, Transform rubEnd, int passes, float duration)
    {
        approachPoint = approach;
        rubStartPoint = rubStart;
        rubEndPoint = rubEnd;
        passCount = Mathf.Max(1, passes);
        passDuration = Mathf.Max(0.2f, duration);
    }
#endif
}
