using System.Collections;
using UnityEngine;

/// <summary>
/// Nap inside the bedroom star tipi.
///
/// The tent interior is far too tight for the cat's CharacterController
/// (0.50 wide capsule against a 0.46 wide tent at head height, and the mat lip
/// is above stepOffset), so this activity follows the PlayTunnel pattern: the
/// controller is switched off and the cat is walked in, curled up and walked
/// back out under script control. The cat always ends the routine outside the
/// product footprint before physics is handed back.
/// </summary>
[DisallowMultipleComponent]
public sealed class CanopyNapActivity : CatActivity
{
    [Header("Canopy nap")]
    [SerializeField] private Transform doorPoint;
    [SerializeField] private Transform nestPoint;
    [SerializeField, Min(0.5f)] private float napDuration = 3.2f;
    [SerializeField, Min(0f)] private float energyRestore = 22f;
    [SerializeField, Range(0f, 100f)] private float wideAwakeEnergy = 92f;

    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "NAPPING..." : string.Empty;

    public float EnergyRestore => Mathf.Max(0f, energyRestore);
    public float NapDuration => Mathf.Max(0.5f, napDuration);
    public float WideAwakeEnergy => Mathf.Clamp(wideAwakeEnergy, 0f, 100f);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (doorPoint == null || nestPoint == null)
        {
            failureReason = "THE TENT IS NOT READY";
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
        StartCoroutine(NapRoutine());
        return true;
    }

    private IEnumerator NapRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 door = Flatten(doorPoint.position, start.y);
        Vector3 nest = nestPoint.position;

        Quaternion toDoor = LookTowards(door - start, startRotation);
        yield return Move(start, door, startRotation, toDoor, 0.34f);

        Quaternion inward = LookTowards(nest - door, toDoor);
        yield return EnterNest(door, nest, toDoor, inward, 0.55f);

        // Turn around so the cat sleeps facing the open door.
        Quaternion outward = LookTowards(door - nest, inward);
        yield return Move(nest, nest, inward, outward, 0.26f);

        Vector3 curled = originalScale;
        curled.y *= 0.62f;
        curled.x *= 1.10f;
        curled.z *= 1.10f;
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
            yield return null;
        }

        yield return Squash(Cat.transform.localScale, originalScale, 0.24f);
        yield return EnterNest(nest, door, outward, outward, 0.50f);

        RestoreCat();
        if (Energy != null)
            Energy.RestoreEnergy(EnergyRestore);
        CompleteActivity("SWEET DREAMS!");
    }

    private IEnumerator EnterNest(Vector3 from, Vector3 to, Quaternion start, Quaternion end, float duration)
    {
        bool raised = Mathf.Abs(from.y - to.y) > .12f;
        PlayCatPose(raised ? CatActivityPose.Hop : CatActivityPose.Crawl);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Cat.transform.position = raised ? CatActivityMotion.JumpPosition(from, to, t) :
                Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            Cat.transform.rotation = Quaternion.Slerp(start, end, t);
            yield return null;
        }
        Cat.transform.position = to;
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
    public void EditorConfigureNap(
        Transform door, Transform nest, float duration, float restore, float awakeThreshold)
    {
        doorPoint = door;
        nestPoint = nest;
        napDuration = Mathf.Max(0.5f, duration);
        energyRestore = Mathf.Max(0f, restore);
        wideAwakeEnergy = Mathf.Clamp(awakeThreshold, 0f, 100f);
    }
#endif
}
