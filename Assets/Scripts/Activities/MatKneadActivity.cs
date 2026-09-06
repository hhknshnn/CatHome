using System.Collections;
using UnityEngine;

/// <summary>
/// Knead the bath mat's raised pad, then flop over on it.
///
/// The mat is 0.08 tall, so there is nothing to climb and nothing to enter: the
/// whole beat is the cat. It walks onto the pad, alternates front paws with a
/// small weight shift on each press, then rolls onto its side and settles for a
/// moment before getting back up.
///
/// The CharacterController is switched off for the routine because the roll
/// tips the cat past what the capsule allows, and it is handed back with the
/// cat upright and clear of the pad. The cat is never re-parented under the mat.
/// </summary>
[DisallowMultipleComponent]
public sealed class MatKneadActivity : CatActivity
{
    [Header("Mat knead")]
    [SerializeField] private Transform padPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(1)] private int kneadCount = 6;
    [SerializeField, Min(0.1f)] private float kneadInterval = 0.34f;
    [SerializeField, Min(0.2f)] private float flopDuration = 1.4f;
    [SerializeField, Min(0f)] private float energyRestore = 8f;

    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "KNEADING..." : string.Empty;

    public int KneadCount => Mathf.Max(1, kneadCount);
    public float KneadInterval => Mathf.Max(0.1f, kneadInterval);
    public float FlopDuration => Mathf.Max(0.2f, flopDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (padPoint == null || exitPoint == null)
        {
            failureReason = "THE MAT IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(KneadRoutine());
        return true;
    }

    private IEnumerator KneadRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 pad = padPoint.position;
        Vector3 exit = Flatten(exitPoint.position, start.y);

        Quaternion toPad = LookTowards(
            new Vector3(pad.x - start.x, 0f, pad.z - start.z), startRotation);
        yield return Move(start, pad, startRotation, toPad, 0.36f);

        // Knead: alternate sides, so it reads as two paws and not as a bounce.
        for (int i = 0; i < KneadCount; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            float elapsed = 0f;
            PlayCatPose(CatActivityPose.Paw, padPoint);
            while (elapsed < KneadInterval)
            {
                elapsed += Time.deltaTime;
                float press = Mathf.Sin(Mathf.Clamp01(elapsed / KneadInterval) * Mathf.PI);
                Cat.transform.position = pad + new Vector3(0f, press * 0.022f, 0f);
                Cat.transform.rotation =
                    toPad * Quaternion.Euler(press * 7f, 0f, press * 6f * side);
                Vector3 scale = originalScale;
                scale.y *= 1f + press * 0.045f;
                scale.x *= 1f - press * 0.030f;
                Cat.transform.localScale = scale;
                yield return null;
            }
        }

        Cat.transform.position = pad;
        Cat.transform.localScale = originalScale;

        // Flop onto one side and settle, breathing.
        Quaternion upright = toPad;
        Quaternion onSide = toPad * Quaternion.Euler(0f, 0f, 74f);
        float roll = 0f;
        while (roll < 0.30f)
        {
            roll += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(roll / 0.30f));
            Cat.transform.rotation = Quaternion.Slerp(upright, onSide, t);
            yield return null;
        }

        float settled = 0f;
        PlayCatPose(CatActivityPose.Sleep, padPoint);
        while (settled < FlopDuration)
        {
            settled += Time.deltaTime;
            float breath = Mathf.Sin(settled * 3.4f) * 0.030f;
            Vector3 scale = originalScale;
            scale.x *= 1f + breath;
            scale.z *= 1f - breath * 0.5f;
            Cat.transform.localScale = scale;
            Cat.transform.rotation = onSide;
            yield return null;
        }

        float up = 0f;
        while (up < 0.28f)
        {
            up += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(up / 0.28f));
            Cat.transform.rotation = Quaternion.Slerp(onSide, upright, t);
            Cat.transform.localScale = Vector3.Lerp(Cat.transform.localScale, originalScale, t);
            yield return null;
        }

        Cat.transform.rotation = upright;
        Cat.transform.localScale = originalScale;
        Quaternion away = LookTowards(exit - pad, upright);
        yield return Move(pad, exit, upright, away, 0.34f);

        RestoreCat();
        if (Energy != null)
            Energy.RestoreEnergy(EnergyRestore);
        CompleteActivity("MAKING BISCUITS!");
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
        // The flop leaves the cat rolled; physics must come back upright.
        Vector3 forward = Cat.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
            Cat.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
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
    public void EditorConfigureKnead(
        Transform pad, Transform exit, int kneads, float interval, float flop, float restore)
    {
        padPoint = pad;
        exitPoint = exit;
        kneadCount = Mathf.Max(1, kneads);
        kneadInterval = Mathf.Max(0.1f, interval);
        flopDuration = Mathf.Max(0.2f, flop);
        energyRestore = Mathf.Max(0f, restore);
    }
#endif
}
