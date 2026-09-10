using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Readable stationary action staging. Never changes locomotion or a contact target.</summary>
public static class CatActivityFacing
{
    public const float MaximumViewAngle = 65f;
    public const float MinimumViewDot = -.01f;
    // The rendered torso midpoint is forward of the root; a merely side-on
    // root can still leave the actual body facing away from a nearby camera.
    public const float PreferredViewDot = .30f;

    public static Vector3 CameraPosition(CatMovement cat)
    {
        Camera camera = Camera.main;
        if (camera != null && camera.isActiveAndEnabled && (cat == null || camera.gameObject.scene == cat.gameObject.scene))
            return camera.transform.position;
        foreach (Camera candidate in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (candidate.isActiveAndEnabled && candidate.targetTexture == null &&
                (cat == null || candidate.gameObject.scene == cat.gameObject.scene)) return candidate.transform.position;
        // The authoring camera profile is shared by current and future home rooms.
        return HomeRoomCameraProfile.Position;
    }

    public static float FacingDot(Vector3 forward, Vector3 position, Vector3 cameraPosition)
    {
        forward.y = 0f; Vector3 view = cameraPosition - position; view.y = 0f;
        if (forward.sqrMagnitude < .000001f || view.sqrMagnitude < .000001f) return 1f;
        return Vector3.Dot(forward.normalized, view.normalized);
    }

    public static Quaternion Resolve(CatMovement cat, Vector3 position, Quaternion preferred) =>
        Resolve(position, preferred, CameraPosition(cat));

    public static Quaternion Resolve(Vector3 position, Quaternion preferred, Vector3 cameraPosition)
    {
        Vector3 view = cameraPosition - position; view.y = 0f;
        if (view.sqrMagnitude < .000001f) return preferred;
        Vector3 forward = preferred * Vector3.forward; forward.y = 0f;
        if (forward.sqrMagnitude < .000001f) forward = view;
        float angle = Vector3.SignedAngle(view, forward, Vector3.up);
        // At exactly opposite headings both turns tie. Float noise after a room
        // transform must not randomly switch between the two visible limits.
        if (Vector3.Dot(view.normalized, forward.normalized) < -.999999f) angle = 180f;
        return Quaternion.LookRotation(Quaternion.AngleAxis(Mathf.Clamp(angle, -MaximumViewAngle, MaximumViewAngle), Vector3.up) * view, Vector3.up);
    }

    public static Quaternion AlongAxis(CatMovement cat, Vector3 position, Quaternion preferred) =>
        AlongAxis(position, preferred, CameraPosition(cat));

    public static Quaternion AlongAxis(Vector3 position, Quaternion preferred, Vector3 cameraPosition)
    {
        Quaternion opposite = preferred * Quaternion.Euler(0f, 180f, 0f);
        return FacingDot(preferred * Vector3.forward, position, cameraPosition) >=
            FacingDot(opposite * Vector3.forward, position, cameraPosition) ? preferred : opposite;
    }

    public static IEnumerator Turn(CatMovement cat, Quaternion target, float duration = .24f)
    {
        if (cat == null) yield break;
        Quaternion start = cat.transform.rotation; float elapsed = 0f;
        while (elapsed < duration && cat != null)
        {
            elapsed += Time.deltaTime;
            cat.transform.rotation = Quaternion.Slerp(start, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.001f, duration))));
            yield return null;
        }
        if (cat != null) cat.transform.rotation = target;
    }

    /// <summary>Keep the authored muzzle/paw radius while finding a clear, camera-readable contact side.</summary>
    public static bool TryFindContactStand(CatMovement cat, Vector3 target, Vector3 authored, Vector3 origin,
        out Vector3 stand, out List<Vector3> path, CatActivity sightOwner = null)
        => TryFindStand(cat, target, authored, origin, false, out stand, out path, sightOwner);

    public static bool TryFindViewStand(CatMovement cat, Vector3 target, Vector3 authored, out Vector3 stand)
        => TryFindViewStand(cat, target, authored, authored, out stand, out _);

    public static bool TryFindViewStand(CatMovement cat, Vector3 target, Vector3 authored, Vector3 origin,
        out Vector3 stand, out List<Vector3> path, CatActivity sightOwner = null)
        => TryFindStand(cat, target, authored, origin, true, out stand, out path, sightOwner);

    // A seated cat may show its body in profile while its real head looks at
    // a wall object. The target cannot require more neck yaw than the gaze rig
    // can produce. Contact gestures still face the target with their body.
    public static bool TryResolveViewFacing(CatMovement cat, Vector3 position, Vector3 target, out Quaternion rotation)
    {
        Vector3 towards = target-position; towards.y=0f;
        rotation = Quaternion.identity;
        if (towards.sqrMagnitude < .0001f) return false;
        Quaternion preferred = Quaternion.LookRotation(towards);
        rotation = Resolve(cat,position,preferred);
        return Quaternion.Angle(rotation,preferred) <= CatFurnitureGaze.SeatedYawLimit-5f;
    }

    static bool TryFindStand(CatMovement cat, Vector3 target, Vector3 authored, Vector3 origin,
        bool freeGaze, out Vector3 stand, out List<Vector3> path, CatActivity sightOwner)
    {
        stand = authored; path = null;
        Vector3 offset = authored - target; offset.y = 0f;
        if (offset.sqrMagnitude < .0001f) return false;
        Vector3 camera = CameraPosition(cat);
        float best = float.PositiveInfinity;
        float authoredRadius = offset.magnitude;
        int radiusCount = freeGaze ? 3 : 1;
        for (int radiusIndex = 0; radiusIndex < radiusCount; radiusIndex++)
        for (int i = 0; i < 48; i++)
        {
            float radius = freeGaze ? Mathf.Clamp(authoredRadius + (radiusIndex == 1 ? -.2f : radiusIndex == 2 ? .2f : 0f), .48f, 1.25f) : authoredRadius;
            Vector3 candidate = target + Quaternion.Euler(0f, i * 7.5f, 0f) * offset.normalized * radius;
            candidate.y = authored.y;
            float direct = Vector3.Distance(origin, candidate);
            bool readable = freeGaze ? TryResolveViewFacing(cat,candidate,target,out _) :
                FacingDot(target-candidate,candidate,camera) >= PreferredViewDot;
            bool floorClear = freeGaze ? CatActivityMotion.IsControllerFloorClear(cat,candidate) : CatActivityMotion.IsFloorClear(candidate);
            List<Vector3> route;
            if (direct > 2.2f || direct >= best ||
                !readable || !floorClear ||
                (sightOwner != null && !CatActivityApproach.HasClearSight(sightOwner, cat, candidate + Vector3.up * .4f, target))) continue;
            if (!(freeGaze ? CatActivityMotion.TryFloorPath(cat,origin,candidate,out route) :
                CatActivityMotion.TryFloorPath(origin,candidate,out route))) continue;
            float length = 0f; Vector3 previous = origin;
            bool clear = true;
            foreach (Vector3 rawPoint in route)
            {
                Vector3 point = rawPoint; point.y = origin.y;
                if (!CatActivityMotion.ClearSegment(previous, point,freeGaze ? CatActivityMotion.ControllerFloorRadius(cat) : .27f)) { clear = false; break; }
                length += Vector3.Distance(previous, point); previous = point;
            }
            if (!clear) continue;
            if (length > 2.2f || length >= best) continue;
            best = length; stand = candidate; path = route;
        }
        return path != null;
    }
}
