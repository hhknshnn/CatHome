using System.Collections;
using UnityEngine;

/// <summary>
/// Rub a cheek along the grooming cart's brush roller.
///
/// Nothing on the product moves: the beat is entirely the cat, which is why the
/// cart ships as one FBX rather than two. The cat walks up to the roller, turns
/// side-on, and walks its cheek along the same real brush line on each pass.
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
    public bool IsRubbing { get; private set; }
    public Vector3 SelectedRubStart { get; private set; }
    public Vector3 SelectedRubEnd { get; private set; }

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
        Quaternion authoredAlong = LookTowards(rubEnd - rubStart, startRotation);
        Quaternion viewAlong = CatActivityFacing.AlongAxis(Cat, (rubStart + rubEnd) * .5f, authoredAlong);
        if (Vector3.Dot(viewAlong * Vector3.forward, rubEnd - rubStart) < 0f)
        { Vector3 swap = rubStart; rubStart = rubEnd; rubEnd = swap; }
        SelectedRubStart = rubStart; SelectedRubEnd = rubEnd;

        Quaternion toApproach = LookTowards(approach - start, startRotation);
        yield return Move(start, approach, startRotation, toApproach, 0.32f);

        // Side-on: the cat faces ALONG the roller, not at it, so the cheek is
        // what touches the bristles.
        Quaternion along = LookTowards(rubEnd - rubStart, toApproach);
        yield return Move(approach, rubStart, toApproach, along, 0.30f);

        for (int pass = 0; pass < PassCount; pass++)
        {
            Vector3 from = rubStart;
            Vector3 to = rubEnd;
            Quaternion facing = LookTowards(to - from, along);

            float elapsed = 0f;
            PlayCatPose(CatActivityPose.Walk);
            IsRubbing = true;
            while (elapsed < PassDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PassDuration);
                Cat.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                Cat.transform.rotation = facing;
                Cat.transform.localScale = originalScale;
                yield return null;
            }

            Cat.transform.position = to;
            Cat.transform.localScale = originalScale;
            IsRubbing = false;
            if (pass < PassCount - 1)
            {
                // Walk back through the same open approach bay; the next rub
                // follows the real roller in the readable direction again.
                yield return Move(to, approach, facing, LookTowards(approach - to, facing), .3f);
                yield return Move(approach, from, Cat.transform.rotation, facing, .3f);
            }
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
        Vector3 direction = to - from; direction.y = 0f;
        if (direction.sqrMagnitude < .000001f)
        {
            // An already reached entry is not an extra stationary work beat.
            if (Quaternion.Angle(Cat.transform.rotation, toRotation) <= .1f) yield break;
            PlayCatPose(CatActivityPose.GentleKnead);
            yield return CatActivityFacing.Turn(Cat, toRotation, duration);
            yield break;
        }

        // Turn on the spot first. Interpolating a travel position while still
        // facing the previous action made the return leg slide backwards.
        Quaternion travel = Quaternion.LookRotation(direction, Vector3.up);
        PlayCatPose(CatActivityPose.GentleKnead);
        yield return CatActivityFacing.Turn(Cat, travel, .16f);
        PlayCatPose(CatActivityPose.Walk);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Cat.transform.SetPositionAndRotation(Vector3.Lerp(from, to, t), travel);
            yield return null;
        }

        Cat.transform.SetPositionAndRotation(to, travel);
        if (Quaternion.Angle(travel, toRotation) > .1f)
        {
            PlayCatPose(CatActivityPose.GentleKnead);
            yield return CatActivityFacing.Turn(Cat, toRotation, .16f);
        }
    }

    private void RestoreCat()
    {
        IsRubbing = false;
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
