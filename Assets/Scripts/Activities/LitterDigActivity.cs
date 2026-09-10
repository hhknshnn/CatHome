using System.Collections;
using UnityEngine;

/// <summary>
/// Enter, inspect and dig real sand, briefly crouch, cover and leave the tray.
///
/// The tray is a walk-in like the star tipi and the shower, so its product box
/// becomes a trigger — the 0.15 sill is well above the CharacterController's
/// step offset and would otherwise stop the cat dead at the mouth. The
/// controller is switched off for the routine and the cat always ends up
/// outside the footprint before physics resumes, and it is never re-parented
/// under the product.
///
/// Bathroom sand has a reversible deformed mesh and measured alternating paws.
/// Unrelated authored uses retain their previous motion.
/// </summary>
[DisallowMultipleComponent]
public sealed class LitterDigActivity : CatActivity
{
    [Header("Litter dig")]
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private Transform digPoint;
    [SerializeField] private Transform digFacingPoint;
    [SerializeField, Min(0.5f)] private float digDuration = 2.6f;
    [SerializeField, Min(1)] private int scrapeCount = 4;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatLitterRoutineMotion litterMotion;
    [SerializeField] private CatLitterSandSurface litterSurface;
    public CatLitterPhase Phase { get; private set; }
    public CatLitterSandSurface LitterSurface => litterSurface;

    public override string ProgressLabel => IsRunning ? "DIGGING..." : string.Empty;

    public float DigDuration => Mathf.Max(0.5f, digDuration);
    public int ScrapeCount => Mathf.Max(1, scrapeCount);
    public bool UsesGentleScraping => Kind == CatActivityKind.LitterDig &&
        StoreProductId == HomeStoreService.BathroomLitterBoxId;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (mouthPoint == null || digPoint == null || (UsesGentleScraping && (litterSurface == null || !litterSurface.IsConfigured)))
        {
            failureReason = "THE TRAY IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        if (UsesGentleScraping && !litterSurface.Begin(this)) return false;
        StartCoroutine(DigRoutine());
        return true;
    }

    private IEnumerator DigRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Phase = CatLitterPhase.Entering;
        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 mouth = Flatten(mouthPoint.position, start.y);
        Vector3 dig = digPoint.position;

        Quaternion toMouth = LookTowards(mouth - start, startRotation);
        yield return Move(start, mouth, startRotation, toMouth, 0.30f);

        Quaternion inward = LookTowards(
            new Vector3(dig.x - mouth.x, 0f, dig.z - mouth.z), toMouth);
        yield return Move(mouth, mouth, toMouth, inward, 0.16f);
        // Step over the sill rather than hop: the litter surface is only 0.18
        // up, so an arc would read as the cat vaulting a kerb.
        yield return Move(mouth, dig, inward, inward, 0.40f);

        if (UsesGentleScraping)
        {
            Quaternion preferred = digFacingPoint != null ? LookTowards(digFacingPoint.position - dig, inward) : inward;
            inward = CatActivityFacing.Resolve(Cat, dig, CatActivityFacing.AlongAxis(Cat, dig, preferred));
            PlayCatPose(CatActivityPose.GentleKnead, digPoint);
            yield return CatActivityFacing.Turn(Cat, inward, .4f);
            yield return new WaitForSeconds(.24f);
            litterMotion = Cat.GetComponent<CatLitterRoutineMotion>() ?? Cat.gameObject.AddComponent<CatLitterRoutineMotion>();
            yield return SandPhase(CatLitterPhase.Investigating, dig, inward, .8f);
            yield return SandPhase(CatLitterPhase.Digging, dig, inward, Mathf.Max(DigDuration, ScrapeCount * CatLitterRoutineMotion.StrokeSeconds));
            yield return SandPhase(CatLitterPhase.Squatting, dig, inward, 2.5f);
            Phase = CatLitterPhase.Covering;
            litterMotion.PrepareCovering(this);
            PlayCatPose(CatActivityPose.GentleKnead, digPoint);
            yield return new WaitForSeconds(.24f);
            yield return SandPhase(CatLitterPhase.Covering, dig, inward, 3f * CatLitterRoutineMotion.StrokeSeconds);
            litterSurface.Excavate(this, litterMotion.HoleTarget, 0f);
            litterMotion.Clear();
        }
        else
        {
            // Outdoor troughs use the same real contact patch from its visible
            // end. Turn only after entering; both axis directions fit the patch.
            inward = CatActivityFacing.AlongAxis(Cat, dig, inward);
            PlayCatPose(CatActivityPose.Sniff, digPoint);
            yield return CatActivityFacing.Turn(Cat, inward, .3f);
            float perScrape = DigDuration / ScrapeCount;
            for (int i = 0; i < ScrapeCount; i++)
            {
                float elapsed = 0f;
                PlayCatPose(CatActivityPose.Paw, digPoint);
                while (elapsed < perScrape)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / perScrape);
                    float stroke = Mathf.Sin(t * Mathf.PI);
                    Vector3 position = dig;
                    position += (inward * Vector3.forward) * (-stroke * 0.075f);
                    position.y = dig.y + stroke * 0.020f;
                    Cat.transform.position = position;
                    Cat.transform.rotation = inward * Quaternion.Euler(stroke * 13f, 0f, 0f);
                    Vector3 scale = originalScale;
                    scale.y *= 1f - stroke * 0.055f;
                    scale.z *= 1f + stroke * 0.045f;
                    Cat.transform.localScale = scale;
                    yield return null;
                }
            }
        }

        Cat.transform.localScale = originalScale;
        Cat.transform.position = dig;

        // Covering faces the same readable side as digging. Turn towards the
        // open mouth only when leaving the tray.
        Phase = CatLitterPhase.Exiting;
        Quaternion outward = LookTowards(mouth - dig, inward);
        yield return Move(dig, dig, inward, outward, UsesGentleScraping ? .4f : .26f);
        if (!UsesGentleScraping) for (int i = 0; i < 2; i++)
        {
            float elapsed = 0f;
            while (elapsed < 0.34f)
            {
                elapsed += Time.deltaTime;
                float stroke = Mathf.Sin(Mathf.Clamp01(elapsed / 0.34f) * Mathf.PI);
                Cat.transform.rotation = outward * Quaternion.Euler(stroke * 10f, 0f, 0f);
                Cat.transform.position = dig + (outward * Vector3.forward) * (stroke * 0.045f);
                yield return null;
            }
        }
        Cat.transform.rotation = outward;
        yield return Move(dig, mouth, outward, outward, 0.36f);
        yield return Move(mouth, Flatten(mouthPoint.position, start.y) +
                          (mouth - dig).normalized * 0.35f, outward, outward, 0.26f);

        RestoreCat();
        CompleteActivity("ALL COVERED UP!");
    }

    private IEnumerator SandPhase(CatLitterPhase phase, Vector3 position, Quaternion rotation, float duration)
    {
        Phase = phase;
        float elapsed = 0f;
        var pose = Cat.GetComponent<CatActivityAnimation>();
        while (elapsed < duration)
        {
            if (phase == CatLitterPhase.Squatting)
            {
                // Hold partway through the genuine sit transition: the hips
                // lower and knees flex, without scaling or compressing the cat.
                float crouch = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed / .45f, (duration - elapsed) / .45f));
                pose.SetTimedPose(CatActivityPose.SitDown, .58f * crouch, digPoint);
            }
            litterMotion.Sample(this, litterSurface, phase, elapsed, duration);
            Cat.transform.SetPositionAndRotation(position, rotation);
            Cat.transform.localScale = originalScale;
            yield return null;
            elapsed += Time.deltaTime;
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
        if (litterMotion != null) litterMotion.Clear();
        if (litterSurface != null) litterSurface.Stop(this);
        Phase = CatLitterPhase.None;
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
    public void EditorConfigureLitterFacing(Transform point) => digFacingPoint = point;

    public void EditorConfigureLitterSurface(MeshFilter surface, Vector3 localCenter, Vector2 localSize)
    {
        litterSurface = GetComponent<CatLitterSandSurface>();
        if (litterSurface == null) litterSurface = gameObject.AddComponent<CatLitterSandSurface>();
        litterSurface.EditorConfigure(surface, localCenter, localSize);
    }

    public void EditorConfigureDig(
        Transform mouth, Transform dig, float duration, int scrapes)
    {
        mouthPoint = mouth;
        digPoint = dig;
        digDuration = Mathf.Max(0.5f, duration);
        scrapeCount = Mathf.Max(1, scrapes);
    }
#endif
}
