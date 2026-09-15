using System.Collections;
using UnityEngine;

/// <summary>Authored jump clips and supported transitions without changing the cat's proportions.</summary>
public sealed class CatSupportedFurnitureMotion
{
    readonly CatMovement cat;
    readonly CatActivityAnimation animation;
    readonly Transform support;
    public bool InFlight { get; private set; }
    public float FlightProgress { get; private set; }

    public CatSupportedFurnitureMotion(CatActivity owner, CatMovement actor, Transform surface)
    {
        cat = actor; animation = cat.GetComponent<CatActivityAnimation>();
        var anchor = new GameObject("Furniture pose support") { hideFlags = HideFlags.DontSave };
        support = anchor.transform; support.SetParent(owner.transform, true);
        support.SetPositionAndRotation(surface.position, surface.rotation);
        anchor.AddComponent<CatActivitySurface>();
    }

    public IEnumerator Pose(CatActivityPose pose, float seconds, Vector3 position, Quaternion rotation,
        float first = 0f, float last = 1f, float firstBlend = 1f, float lastBlend = 1f)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            cat.transform.SetPositionAndRotation(position, rotation); support.position = position;
            float sample = Mathf.Lerp(first, last, t);
            animation.SetTimedPose(pose, pose == CatActivityPose.TowelWake ? 1f - sample : sample, support);
            animation.SetHorizontalSupportBlend(Mathf.SmoothStep(firstBlend, lastBlend, t));
            yield return null; elapsed += Time.deltaTime;
        }
        animation.SetTimedPose(pose, pose == CatActivityPose.TowelWake ? 1f - last : last, support);
        animation.SetHorizontalSupportBlend(lastBlend);
    }

    public IEnumerator Jump(Vector3 from, Vector3 to, Quaternion launch, Quaternion arrival, bool preserveLaunchHeading = false)
    {
        yield return CatJumpMotion.Play(cat, from, to, launch, arrival, to.y > .12f, .20f,
            (stage, progress) => { InFlight = stage == 1; FlightProgress = progress; }, preserveLaunchHeading);
        InFlight = false; FlightProgress = 1f;
        support.position = to;
    }

    public void Rest(CatActivityPose pose) => animation.SetPose(pose, support);

    public void End()
    {
        InFlight = false;
        if (animation != null) animation.SetPose(CatActivityPose.GentleKnead);
        if (support != null) Object.Destroy(support.gameObject);
    }
}
