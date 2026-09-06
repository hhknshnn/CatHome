using System.Collections;
using UnityEngine;

/// <summary>
/// Rinse under the bathroom rain shower.
///
/// The cabin is open at the front, but the cat could never walk in on its own:
/// the product box is one solid collider, and the tray lip stands 0.12 above the
/// floor while the cat's CharacterController allows a 0.005 step. So this
/// follows the CanopyNap/PlayTunnel pattern — the controller is switched off and
/// the cat is walked in, rinsed, shaken dry and walked back out under script
/// control, ending outside the product footprint before physics resumes.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShowerRinseActivity : CatActivity
{
    [Header("Shower rinse")]
    [SerializeField] private Transform doorPoint;
    [SerializeField] private Transform standPoint;
    [SerializeField, Min(0.5f)] private float rinseDuration = 2.6f;
    [SerializeField, Min(0.2f)] private float shakeDuration = 0.65f;
    [SerializeField, Min(0f)] private float energyRestore = 6f;

    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "RINSING..." : string.Empty;

    public float RinseDuration => Mathf.Max(0.5f, rinseDuration);
    public float ShakeDuration => Mathf.Max(0.2f, shakeDuration);
    public float EnergyRestore => Mathf.Max(0f, energyRestore);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (doorPoint == null || standPoint == null)
        {
            failureReason = "THE SHOWER IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(RinseRoutine());
        return true;
    }

    private IEnumerator RinseRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 door = Flatten(doorPoint.position, start.y);
        // The stand point keeps its own height: stepping onto the tray is the
        // whole reason the cat cannot do this by itself.
        Vector3 stand = standPoint.position;

        Quaternion toDoor = LookTowards(door - start, startRotation);
        yield return Move(start, door, startRotation, toDoor, 0.32f);

        Quaternion inward = LookTowards(stand - door, toDoor);
        yield return Move(door, stand, toDoor, inward, 0.46f);

        // Turn back towards the open front so the rinse plays to the camera.
        Quaternion outward = LookTowards(door - stand, inward);
        yield return Move(stand, stand, inward, outward, 0.24f);

        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Sit, standPoint);
        while (elapsed < RinseDuration)
        {
            elapsed += Time.deltaTime;
            float bob = Mathf.Sin(elapsed * 5.4f);
            Vector3 position = stand;
            position.y += bob * 0.018f;
            Cat.transform.position = position;

            Vector3 wet = originalScale;
            wet.y *= 1f - bob * 0.045f;
            wet.x *= 1f + bob * 0.020f;
            wet.z *= 1f + bob * 0.020f;
            Cat.transform.localScale = wet;
            yield return null;
        }

        yield return Shake(stand, outward);

        Cat.transform.localScale = originalScale;
        yield return Move(stand, door, outward, outward, 0.42f);

        RestoreCat();
        if (Energy != null)
            Energy.RestoreEnergy(EnergyRestore);
        CompleteActivity("SQUEAKY CLEAN!");
    }

    /// <summary>Shake-off: a fast yaw wobble that damps out, with a wet squash.</summary>
    private IEnumerator Shake(Vector3 stand, Quaternion facing)
    {
        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Scratch);
        while (elapsed < ShakeDuration)
        {
            elapsed += Time.deltaTime;
            float damp = 1f - elapsed / ShakeDuration;
            float wobble = Mathf.Sin(elapsed * 34f) * 24f * damp;
            Cat.transform.rotation = facing * Quaternion.Euler(0f, wobble, 0f);

            Vector3 shaken = originalScale;
            shaken.x *= 1f + Mathf.Abs(wobble) * 0.004f;
            shaken.y *= 1f - Mathf.Abs(wobble) * 0.003f;
            Cat.transform.localScale = shaken;
            Cat.transform.position = stand;
            yield return null;
        }

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
    public void EditorConfigureRinse(
        Transform door, Transform stand, float rinse, float shake, float restore)
    {
        doorPoint = door;
        standPoint = stand;
        rinseDuration = Mathf.Max(0.5f, rinse);
        shakeDuration = Mathf.Max(0.2f, shake);
        energyRestore = Mathf.Max(0f, restore);
    }
#endif
}
