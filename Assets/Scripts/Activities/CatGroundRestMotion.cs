using System.Collections;
using UnityEngine;

/// <summary>
/// Grounded rest at the position and heading the player already chose. The
/// temporary support contributes only surface height, never horizontal centering.
/// </summary>
public sealed class CatGroundRestMotion
{
    private readonly CatMovement cat;
    private readonly CatActivityAnimation animation;
    private readonly Transform support;
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }

    public static bool TryGetPromptDistance(CatMovement actor, Transform centre,
        float radius, out float distance)
    {
        distance = float.PositiveInfinity;
        if (actor == null || centre == null || !actor.isActiveAndEnabled ||
            actor.gameObject.scene != centre.gameObject.scene) return false;
        Vector3 delta = actor.transform.position - centre.position;
        delta.y = 0f;
        distance = delta.magnitude;
        return distance <= radius && CatActivityMotion.IsControllerFloorClear(actor, actor.transform.position);
    }

    public CatGroundRestMotion(CatActivity owner, CatMovement actor, float surfaceHeight)
    {
        cat = actor;
        animation = actor.GetComponent<CatActivityAnimation>();
        Position = actor.transform.position;
        Rotation = actor.transform.rotation;
        var anchor = new GameObject("Ground rest height") { hideFlags = HideFlags.DontSave };
        support = anchor.transform;
        // Keep world scale at one. CatActivitySurface intentionally is absent:
        // its bounding-box centering would slide the visible cat before resting.
        support.SetParent(owner.transform, true);
        support.SetPositionAndRotation(new Vector3(Position.x, surfaceHeight, Position.z), Quaternion.identity);
    }

    public void Hold()
    {
        if (cat != null) cat.transform.SetPositionAndRotation(Position, Rotation);
    }

    public void Play(CatActivityPose pose)
    {
        Hold();
        if (animation != null) animation.SetPose(pose, support);
    }

    public IEnumerator Pose(CatActivityPose pose, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            Hold();
            float phase = Mathf.Clamp01(elapsed / seconds);
            if (animation != null)
                animation.SetTimedPose(pose, pose == CatActivityPose.TowelWake ? 1f - phase : phase, support);
            yield return null;
            if (Time.timeScale > 0f) elapsed += Time.deltaTime;
        }
        Hold();
        if (animation != null) animation.SetTimedPose(pose, pose == CatActivityPose.TowelWake ? 0f : 1f, support);
    }

    public void End()
    {
        if (animation != null) animation.SetPose(CatActivityPose.GentleKnead);
        if (support != null) Object.Destroy(support.gameObject);
    }
}
