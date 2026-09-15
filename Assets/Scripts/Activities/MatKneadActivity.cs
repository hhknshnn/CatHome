using System.Collections;
using UnityEngine;

/// <summary>
/// Knead the pad, then settle into continuous rest.
///
/// The mat is 0.08 tall, so there is nothing to climb and nothing to enter: the
/// whole beat is the cat. The bathroom mat and garden flowers use shallow,
/// alternating paws and a seated transition. Other authored mat kinds retain
/// their existing side-flop motion. Rest ends when the player asks to get up.
///
/// The CharacterController is switched off for the routine because the roll
/// tips the cat past what the capsule allows, and it is handed back with the
/// cat upright and clear of the pad. The cat is never re-parented under the mat.
/// </summary>
[DisallowMultipleComponent]
public sealed class MatKneadActivity : CatActivity
{
    public override bool SupportsContinuousRest=>true;
    [Header("Mat knead")]
    [SerializeField] private Transform padPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(1)] private int kneadCount = 6;
    [SerializeField, Min(0.1f)] private float kneadInterval = 0.34f;
    [SerializeField, Min(0.2f)] private float flopDuration = 1.4f;
    [SerializeField, Min(0f)] private float energyRestore = 8f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatGentleKneadMotion gentleKnead;
    private CatSupportedFurnitureMotion supportedMotion;
    private bool UsesSupportedRest => StoreProductId == HomeStoreService.GardenDaisyBedId ||
        StoreProductId == HomeStoreService.BalconySunMatId || StoreProductId == HomeStoreService.PatioStoneRugId ||
        StoreProductId == HomeStoreService.LoftFloorRunnerId;

    public bool UsesGentleKneading => Kind == CatActivityKind.DaisyRoll ||
        (Kind == CatActivityKind.MatKnead && StoreProductId == HomeStoreService.BathroomBathMatId);

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
        StartCoroutine(UsesSupportedRest ? SupportedKneadRoutine() : KneadRoutine());
        return true;
    }

    private IEnumerator SupportedKneadRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Vector3 pad = padPoint.position, exit = Flatten(exitPoint.position, Cat.transform.position.y);
        Quaternion facing = CatActivityFacing.AlongAxis(Cat, pad,
            LookTowards(pad - Cat.transform.position, Cat.transform.rotation));
        yield return CatActivityMotion.WalkAuthoredStep(Cat, pad, facing, .36f);
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, padPoint);
        yield return supportedMotion.Pose(CatActivityPose.GentleKnead, .24f, pad, facing);
        gentleKnead = Cat.GetComponent<CatGentleKneadMotion>() ?? Cat.gameObject.AddComponent<CatGentleKneadMotion>();
        float elapsed = 0, duration = KneadCount * CatGentleKneadMotion.PressSeconds;
        while (elapsed < duration)
        {
            PlayCatPose(CatActivityPose.GentleKnead, padPoint);
            Cat.transform.SetPositionAndRotation(pad, facing);
            gentleKnead.Sample(this, elapsed, duration);
            yield return null; elapsed += Time.deltaTime;
        }
        gentleKnead.Clear();
        yield return supportedMotion.Pose(CatActivityPose.SitDown, .55f, pad, facing);
        yield return supportedMotion.Pose(CatActivityPose.TowelSettle, .90f, pad, facing);
        PlayCatPose(CatActivityPose.Sleep, padPoint);
        while (KeepResting) { Cat.transform.SetPositionAndRotation(pad, facing); yield return null; }
        yield return supportedMotion.Pose(CatActivityPose.TowelWake, .80f, pad, facing);
        yield return supportedMotion.Pose(CatActivityPose.StandUp, .55f, pad, facing);
        yield return CatActivityMotion.WalkAuthoredStep(Cat, exit, LookTowards(exit-pad, facing), .34f);
        RestoreCat(); CompleteActivity("MAKING BISCUITS!");
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

        // Keep the same mat axis and footprint while showing the working paws.
        toPad = CatActivityFacing.AlongAxis(Cat, pad, toPad);
        yield return Turn(toPad);

        if (UsesGentleKneading)
        {
            // Flowers and the bathroom mat receive slow, shallow paw presses
            // from a neutral stance. The generic Paw pose is a hard swat.
            PlayCatPose(CatActivityPose.GentleKnead, padPoint);
            yield return new WaitForSeconds(.24f);
            gentleKnead = Cat.GetComponent<CatGentleKneadMotion>() ??
                Cat.gameObject.AddComponent<CatGentleKneadMotion>();
            float elapsed = 0f;
            float duration = KneadCount * CatGentleKneadMotion.PressSeconds;
            while (elapsed < duration)
            {
                gentleKnead.Sample(this, elapsed, duration);
                Cat.transform.position = pad;
                Cat.transform.rotation = toPad;
                Cat.transform.localScale = originalScale;
                yield return null;
                elapsed += Time.deltaTime;
            }
            gentleKnead.Clear();
        }
        else
        {
            // Existing textile routines retain their established motion.
            for (int i = 0; i < KneadCount; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                float elapsed = 0f;
                bool sounded = false;
                PlayCatPose(CatActivityPose.Paw, padPoint);
                while (elapsed < KneadInterval)
                {
                    elapsed += Time.deltaTime;
                    float press = Mathf.Sin(Mathf.Clamp01(elapsed / KneadInterval) * Mathf.PI);
                    if (!sounded && elapsed >= KneadInterval * .5f)
                    { sounded = true; GameAudio.Play(AudioCue.Cloth, .6f); }
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
        }

        Cat.transform.position = pad;
        Cat.transform.localScale = originalScale;

        // Flop onto one side and settle, breathing.
        Quaternion upright = toPad;
        Quaternion onSide = UsesGentleKneading ? toPad : toPad * Quaternion.Euler(0f, 0f, 74f);
        if (UsesGentleKneading)
        {
            PlayCatPose(CatActivityPose.SitDown, padPoint);
            yield return new WaitForSeconds(.7f);
        }
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
        while (KeepResting)
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
        CompleteActivity("MAKING BISCUITS!");
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        Vector3 direction = to - from; direction.y = 0f;
        if (direction.sqrMagnitude < .0001f)
        {
            Cat.transform.position = to;
            yield return Turn(toRotation);
            yield break;
        }
        Quaternion travel = Quaternion.LookRotation(direction);
        yield return Turn(travel);
        duration = Mathf.Max(duration, direction.magnitude / 1.5f);
        PlayCatPose(CatActivityPose.Walk);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Cat.transform.position = Vector3.Lerp(from, to, t);
            Cat.transform.rotation = travel;
            yield return null;
        }

        Cat.transform.position = to;
        yield return Turn(toRotation);
    }

    private IEnumerator Turn(Quaternion target)
    {
        float angle = Quaternion.Angle(Cat.transform.rotation, target);
        if (angle < .1f) yield break;
        PlayCatPose(CatActivityPose.GentleKnead);
        yield return CatActivityFacing.Turn(Cat, target, Mathf.Max(.18f, angle / 300f));
    }

    private void RestoreCat()
    {
        if (gentleKnead != null) gentleKnead.Clear();
        if (supportedMotion != null) { supportedMotion.End(); supportedMotion = null; }
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

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreCat();
        base.CancelActivity();
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
