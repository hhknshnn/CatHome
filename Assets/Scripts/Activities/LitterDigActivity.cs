using System.Collections;
using UnityEngine;

/// <summary>
/// Use the litter box: step in over the sill, scrape, turn, bury, step out.
///
/// The tray is a walk-in like the star tipi and the shower, so its product box
/// becomes a trigger — the 0.15 sill is well above the CharacterController's
/// step offset and would otherwise stop the cat dead at the mouth. The
/// controller is switched off for the routine and the cat always ends up
/// outside the footprint before physics resumes, and it is never re-parented
/// under the product.
///
/// The dig is authored as two beats so it reads as a cat and not as a shuffle:
/// front paws scrape backwards, then the cat turns 180 and buries.
/// </summary>
[DisallowMultipleComponent]
public sealed class LitterDigActivity : CatActivity
{
    [Header("Litter dig")]
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private Transform digPoint;
    [SerializeField, Min(0.5f)] private float digDuration = 2.6f;
    [SerializeField, Min(1)] private int scrapeCount = 4;

    private CharacterController characterController;
    private Vector3 originalScale;

    public override string ProgressLabel => IsRunning ? "DIGGING..." : string.Empty;

    public float DigDuration => Mathf.Max(0.5f, digDuration);
    public int ScrapeCount => Mathf.Max(1, scrapeCount);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (mouthPoint == null || digPoint == null)
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
        StartCoroutine(DigRoutine());
        return true;
    }

    private IEnumerator DigRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

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

        // Scrape: rock back on each stroke, and drift a little each time so the
        // cat is working a patch rather than pumping in place.
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

        Cat.transform.localScale = originalScale;
        Cat.transform.position = dig;

        // Turn around and bury with two short front-paw sweeps.
        Quaternion outward = LookTowards(mouth - dig, inward);
        yield return Move(dig, dig, inward, outward, 0.26f);
        for (int i = 0; i < 2; i++)
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
