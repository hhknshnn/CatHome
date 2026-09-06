using System.Collections;
using UnityEngine;

/// <summary>
/// Curl up on the floor in front of the oven door and soak up the heat.
///
/// The one Kitchen beat with no climbing in it: the cat walks to the door, turns
/// side-on so its flank faces the glass, folds down, and breathes there. It is
/// deliberately the slowest routine in the room — the payoff is energy, and a
/// short one would read as the cat changing its mind.
///
/// The controller is switched off because the fold tips the cat past what the
/// capsule allows, and it is stood back up before physics resumes. The cat is
/// never re-parented under the stove.
/// </summary>
[DisallowMultipleComponent]
public sealed class OvenWarmthActivity : CatActivity
{
    [Header("Oven warmth")]
    [SerializeField] private Transform baskPoint;
    [SerializeField] private Transform doorPoint;
    [SerializeField, Min(0.5f)] private float baskDuration = 4.2f;
    [SerializeField, Min(0f)] private float energyRestore = 30f;
    [SerializeField, Range(0f, 100f)] private float wideAwakeEnergy = 92f;

    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "WARMING UP..." : string.Empty;

    public float BaskDuration => Mathf.Max(0.5f, baskDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);
    public float WideAwakeEnergy => Mathf.Clamp(wideAwakeEnergy, 0f, 100f);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (baskPoint == null || doorPoint == null)
        {
            failureReason = "THE OVEN IS NOT READY";
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
        StartCoroutine(BaskRoutine());
        return true;
    }

    private IEnumerator BaskRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 bask = Flatten(baskPoint.position, start.y);
        Vector3 door = doorPoint.position;

        Quaternion toBask = LookTowards(bask - start, startRotation);
        yield return Move(start, bask, startRotation, toBask, 0.36f);

        // Side-on to the door: the flank is what gets warm, not the nose.
        Vector3 toDoor = new Vector3(door.x - bask.x, 0f, door.z - bask.z);
        Vector3 alongDoor = Vector3.Cross(Vector3.up, toDoor);
        Quaternion sideOn = LookTowards(alongDoor, toBask);
        yield return Move(bask, bask, toBask, sideOn, 0.28f);

        Vector3 folded = originalScale;
        folded.y *= 0.56f;
        folded.x *= 1.14f;
        folded.z *= 1.14f;
        yield return Squash(originalScale, folded, 0.34f);

        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Sleep, baskPoint);
        while (elapsed < BaskDuration)
        {
            elapsed += Time.deltaTime;
            float breath = Mathf.Sin(elapsed * 2.4f) * 0.038f;
            Vector3 breathing = folded;
            breathing.y *= 1f + breath;
            breathing.x *= 1f - breath * 0.4f;
            breathing.z *= 1f - breath * 0.4f;
            Cat.transform.localScale = breathing;
            Cat.transform.position = bask;
            // A slow roll towards the glass, as if turning the other side over.
            Cat.transform.rotation =
                sideOn * Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 0.9f) * 7f);
            yield return null;
        }

        Cat.transform.rotation = sideOn;
        yield return Squash(Cat.transform.localScale, originalScale, 0.28f);
        Quaternion away = LookTowards(start - bask, sideOn);
        yield return Move(bask, bask, sideOn, away, 0.24f);

        RestoreCat();
        if (Energy != null)
            Energy.RestoreEnergy(EnergyRestore);
        CompleteActivity("TOASTY!");
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
    public void EditorConfigureBask(
        Transform bask, Transform door, float duration, float restore, float awakeAbove)
    {
        baskPoint = bask;
        doorPoint = door;
        baskDuration = Mathf.Max(0.5f, duration);
        energyRestore = Mathf.Max(0f, restore);
        wideAwakeEnergy = Mathf.Clamp(awakeAbove, 0f, 100f);
    }
#endif
}
