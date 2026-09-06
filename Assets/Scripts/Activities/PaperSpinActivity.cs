using System.Collections;
using UnityEngine;

/// <summary>
/// Bat the toilet paper roll until it spins itself out.
///
/// The cat stays on the floor for this one — it reaches up, swats the roll, and
/// the roll keeps spinning under its own momentum and slows down. Only the
/// pivot is animated; the cat is never parented to it (see AGENTS.md: parenting
/// a scene object under a prefab instance at runtime wedges the editor).
/// </summary>
[DisallowMultipleComponent]
public sealed class PaperSpinActivity : CatActivity
{
    [Header("Paper spin")]
    [SerializeField] private Transform rollPivot;
    [SerializeField] private Transform swatPoint;
    [SerializeField, Min(1)] private int swats = 3;
    [SerializeField, Min(60f)] private float spinPerSwat = 620f;
    [SerializeField, Min(0.2f)] private float spinDamping = 1.15f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private Quaternion pivotRest;

    public override string ProgressLabel => IsRunning ? "SPINNING..." : string.Empty;

    public int Swats => Mathf.Max(1, swats);
    public float SpinPerSwat => Mathf.Max(60f, spinPerSwat);
    public float SpinDamping => Mathf.Max(0.2f, spinDamping);
    public Transform RollPivot => rollPivot;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (rollPivot == null || swatPoint == null)
        {
            failureReason = "NO PAPER TO PLAY WITH";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        pivotRest = rollPivot.localRotation;
        StartCoroutine(SpinRoutine());
        return true;
    }

    private IEnumerator SpinRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 stand = Flatten(swatPoint.position, start.y);
        Quaternion facing = LookTowards(rollPivot.position - stand, startRotation);

        yield return Move(start, stand, startRotation, facing, 0.34f);

        float spin = 0f;
        for (int i = 0; i < Swats; i++)
        {
            yield return Reach(stand, facing);
            spin += SpinPerSwat;
            // Let the roll run down a little between swats, so each hit reads as
            // a fresh push rather than one long spin.
            float settle = 0f;
            while (settle < 0.28f)
            {
                settle += Time.deltaTime;
                spin = SpinDown(spin, Time.deltaTime);
                yield return null;
            }
        }

        PlayCatPose(CatActivityPose.Sit);
        while (spin > 12f)
        {
            spin = SpinDown(spin, Time.deltaTime);
            yield return null;
        }

        RestoreCat();
        CompleteActivity("PAPER EVERYWHERE!");
    }

    private float SpinDown(float speed, float deltaTime)
    {
        rollPivot.localRotation = rollPivot.localRotation * Quaternion.Euler(
            speed * deltaTime, 0f, 0f);
        return Mathf.Max(0f, speed - speed * SpinDamping * deltaTime);
    }

    /// <summary>One paw swipe: rear up towards the roll and drop back.</summary>
    private IEnumerator Reach(Vector3 stand, Quaternion facing)
    {
        const float duration = 0.34f;
        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Paw);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float lift = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

            Vector3 position = stand;
            position.y += lift * 0.075f;
            Cat.transform.position = position;
            Cat.transform.rotation = facing * Quaternion.Euler(-lift * 18f, 0f, 0f);

            Vector3 stretched = originalScale;
            stretched.y *= 1f + lift * 0.10f;
            stretched.x *= 1f - lift * 0.04f;
            stretched.z *= 1f - lift * 0.04f;
            Cat.transform.localScale = stretched;
            yield return null;
        }

        Cat.transform.position = stand;
        Cat.transform.rotation = facing;
        Cat.transform.localScale = originalScale;
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
        if (rollPivot != null)
            rollPivot.localRotation = pivotRest;
        RestoreCat();
        base.OnDisable();
    }

#if UNITY_EDITOR
    public void EditorConfigureSpin(
        Transform pivot, Transform swat, int swatCount, float perSwat, float damping)
    {
        rollPivot = pivot;
        swatPoint = swat;
        swats = Mathf.Max(1, swatCount);
        spinPerSwat = Mathf.Max(60f, perSwat);
        spinDamping = Mathf.Max(0.2f, damping);
    }
#endif
}
