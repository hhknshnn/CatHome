using UnityEngine;

/// <summary>Small alternating paw lifts over the neutral pose; no swat clip or root squash.</summary>
[DefaultExecutionOrder(550)]
[DisallowMultipleComponent]
public sealed class CatGentleKneadMotion : MonoBehaviour
{
    public const float PressSeconds = 1.05f;
    public const float MaximumLift = .024f;
    public const float ScrapeSeconds = .9f;
    public const float MaximumScrapeLift = .016f;
    public const float MaximumScrapeReach = .036f;

    private CatActivity owner;
    private CatToyContactMotion contact;
    private Transform leftHand, rightHand;
    private float pressPhase, envelope;
    private bool scraping;

    public bool IsActive => owner != null && owner.IsRunning;
    public float LeftLift { get; private set; }
    public float RightLift { get; private set; }
    public bool IsScraping => IsActive && scraping;
    public float LeftSweep { get; private set; }
    public float RightSweep { get; private set; }

    public void Sample(MatKneadActivity activity, float elapsed, float totalDuration) =>
        Sample(activity, elapsed, totalDuration, false);

    public void SampleScrape(LitterDigActivity activity, float elapsed, float totalDuration) =>
        Sample(activity, elapsed, totalDuration, true);

    private void Sample(CatActivity activity, float elapsed, float totalDuration, bool scrape)
    {
        if (owner != activity)
        {
            Clear();
            owner = activity;
            contact = GetComponent<CatToyContactMotion>() ?? gameObject.AddComponent<CatToyContactMotion>();
        }
        if (leftHand == null || rightHand == null)
        {
            foreach (Transform bone in GetComponentsInChildren<Transform>())
            {
                if (bone.name == "DEF-hand.L") leftHand = bone;
                if (bone.name == "DEF-hand.R") rightHand = bone;
            }
        }
        scraping = scrape;
        pressPhase = elapsed / (scraping ? ScrapeSeconds : PressSeconds);
        envelope = Mathf.SmoothStep(0f, 1f,
            Mathf.Min(elapsed / .35f, (totalDuration - elapsed) / .35f));
    }

    private void LateUpdate()
    {
        if (!IsActive || contact == null || leftHand == null || rightHand == null) return;
        float swing = Mathf.Sin(pressPhase * Mathf.PI);
        // At least one front paw stays planted; squaring makes each touch-down
        // stop smoothly rather than striking the pad at full speed.
        float maximumLift = scraping ? MaximumScrapeLift : MaximumLift;
        LeftLift = swing > 0f ? swing * swing * maximumLift * envelope : 0f;
        RightLift = swing < 0f ? swing * swing * maximumLift * envelope : 0f;
        LeftSweep = RightSweep = 0f;
        if (scraping)
        {
            // Lift and reach forward, then draw a small arc back through the
            // litter. Position and speed return smoothly to neutral each time;
            // the other paw stays planted and the torso never pumps or lunges.
            float stroke = Mathf.Repeat(pressPhase, 1f);
            float sweep = Mathf.Sin(stroke * Mathf.PI * 2f) * Mathf.Sin(stroke * Mathf.PI) *
                          MaximumScrapeReach * envelope;
            if (swing > 0f) LeftSweep = sweep;
            else if (swing < 0f) RightSweep = sweep;
            contact.ReachBoth(leftHand.position + transform.up * LeftLift + transform.forward * LeftSweep,
                rightHand.position + transform.up * RightLift + transform.forward * RightSweep);
            return;
        }
        // Preserve the established garden/pad knead trajectory exactly.
        Vector3 liftDirection = transform.up + transform.forward * .18f;
        contact.ReachBoth(leftHand.position + liftDirection * LeftLift,
            rightHand.position + liftDirection * RightLift);
    }

    public void Clear()
    {
        if (owner != null && contact != null) contact.Clear();
        owner = null;
        contact = null;
        leftHand = rightHand = null;
        LeftLift = RightLift = 0f;
        LeftSweep = RightSweep = 0f;
        scraping = false;
    }

    private void OnDisable() => Clear();
}
