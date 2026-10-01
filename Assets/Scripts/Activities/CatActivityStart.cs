using UnityEngine;

public enum CatActivityStartKind { Stationary, Contact, GroundLaunch }

/// <summary>A validated player stance, shared by the prompt and the action.</summary>
public struct CatActivityStart
{
    public Vector3 Position, ZoneCentre, ActionTarget;
    public Quaternion Rotation;
    public float PromptDistance;
    public CatActivityStartKind Kind;
    public CatPawReachPlan PawPlan;
    public bool HasPawPlan;
}

public static class CatActivityStartResolver
{
    public static bool Current(CatMovement actor, Vector3 centre, float radius, out CatActivityStart start)
    {
        start = new CatActivityStart { ZoneCentre = centre, ActionTarget = centre,
            PromptDistance = float.PositiveInfinity, Kind = CatActivityStartKind.Stationary };
        if (actor == null || !actor.isActiveAndEnabled) return false;
        start.Position = actor.transform.position;
        start.Rotation = actor.transform.rotation;
        Vector3 delta = start.Position - centre; delta.y = 0f;
        start.PromptDistance = delta.magnitude;
        return start.PromptDistance <= radius && Mathf.Abs(start.Position.y - centre.y) <= .15f &&
            actor.IsInteractionPoseClear(start.Position, start.Rotation);
    }

    public static bool Facing(CatMovement actor, Vector3 centre, float radius,
        Vector3 target, float maxYaw, out CatActivityStart start)
    {
        bool ready = Current(actor, centre, radius, out start);
        start.ActionTarget = target;
        start.Kind = CatActivityStartKind.Contact;
        Vector3 direction = target - start.Position; direction.y = 0f;
        return ready && (direction.sqrMagnitude < .0001f ||
            Vector3.Angle(start.Rotation * Vector3.forward, direction) <= maxYaw);
    }

    public static bool GroundLaunch(CatActivity owner, CatMovement actor,
        Vector3 authoredFloor, Vector3 landing, out CatActivityStart start)
    {
        Vector3 centre = GroundLaunchCentre(owner, authoredFloor, landing);
        bool ready = Facing(actor, centre, .22f, landing, 35f, out start);
        start.Kind = CatActivityStartKind.GroundLaunch;
        return ready && CatJumpClearanceResolver.EndsClear(actor, start.Position, landing,
            start.Rotation, true, true, out _);
    }

    public static Vector3 GroundLaunchCentre(CatActivity owner, Vector3 authoredFloor, Vector3 landing)
    {
        if (owner == null) return authoredFloor;
        Vector3 centre = authoredFloor + owner.transform.TransformVector(
            CatFurnitureJumpClearance.LaunchOffset(owner.StoreProductId));
        if (owner is SinkSipActivity fountain && owner.StoreProductId == HomeStoreService.PatioWaterFountainId &&
            fountain.SipTarget != null)
        {
            Vector3 tangent = fountain.SipTarget.position - landing; tangent.y = 0f;
            if (tangent.sqrMagnitude > .0001f) centre = landing - tangent.normalized * .80f;
        }
        centre.y = authoredFloor.y;
        return centre;
    }

    // Final ground destinations share the accepted root height, which can be
    // above world zero. Keep the legacy rule only outside a prepared activity.
    public static bool LandsOnSupport(CatMovement actor, Vector3 destination)
    {
        var owner = CatActivity.Active;
        return owner != null && owner.TryGetPreparedStart(actor, out var start)
            ? destination.y > start.Position.y + .015f
            : destination.y > .12f;
    }

    // No route search or arbitrary rescue point. Cancellation keeps its old
    // accepted-floor behavior only while that pose still clears current solids.
    // When both stored/current poses are blocked, a vertical projection may
    // release an interrupted airborne cat at the same XZ on a real clear floor.
    // If none is safe, return false: do not teleport into a collider. Releasing
    // the controller lets ordinary gravity/body-guard escape handle that state.
    public static bool TryRecovery(CatMovement actor, CatActivityStart accepted,
        bool cancelled, out CatActivityStart recovery)
    {
        recovery = accepted;
        if (actor == null) return false;
        Vector3 current = actor.transform.position;
        Quaternion rotation = actor.transform.rotation;
        bool currentFloor = Mathf.Abs(current.y - accepted.Position.y) <= .15f &&
            actor.IsInteractionPoseClear(current, rotation);
        if (!cancelled && currentFloor)
        { recovery.Position = current; recovery.Rotation = rotation; return true; }
        if (actor.IsInteractionPoseClear(accepted.Position, accepted.Rotation)) return true;
        if (currentFloor)
        { recovery.Position = current; recovery.Rotation = rotation; return true; }
        Vector3 projected = current; projected.y = accepted.Position.y;
        if (!actor.IsInteractionPoseClear(projected, rotation)) return false;
        foreach (var hit in Physics.RaycastAll(projected + Vector3.up * .20f,
            Vector3.down, .40f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<CatMovement>() != null || hit.normal.y < .65f ||
                hit.point.y > projected.y + .01f || hit.point.y < projected.y - .20f) continue;
            recovery.Position = projected; recovery.Rotation = rotation; return true;
        }
        return false;
    }

    // Resolve the real descent endpoint before turning/takeoff, never after
    // touchdown. This bounded local clearance query does not walk or move the
    // actor and preserves an already clear authored landing exactly.
    public static bool GroundLanding(CatMovement actor, Vector3 from, Vector3 preferred,
        out Vector3 landing, out Quaternion heading, Vector3 measuredPreferredHeading = default)
    {
        landing = preferred; heading = actor.transform.rotation;
        Physics.SyncTransforms();
        Vector3 outward = preferred - from; outward.y = 0;
        float authoredDistance = outward.magnitude;
        if (authoredDistance < .0001f) outward = actor.transform.forward;
        outward.Normalize();
        var directions = new System.Collections.Generic.List<Vector3> { outward };
        measuredPreferredHeading.y = 0;
        if (measuredPreferredHeading.sqrMagnitude > .0001f) directions.Add(measuredPreferredHeading.normalized);
        foreach (float angle in new[] { -15f, 15f, -30f, 30f, -45f, 45f, -60f, 60f, -75f, 75f, -90f, 90f })
            directions.Add(Quaternion.AngleAxis(angle, Vector3.up) * outward);
        foreach (Vector3 direction in directions)
        for (int extension = 0; extension <= 6; extension++)
        {
            Vector3 candidate = from + direction * (authoredDistance + extension * .06f); candidate.y = preferred.y;
            Quaternion rotation = Quaternion.LookRotation(direction);
            if (!actor.IsInteractionPoseClear(candidate, rotation)) continue;
            var boundary = HomeRoomBoundary.FindFor(actor.gameObject.scene);
            if (boundary != null && (candidate.x < boundary.MinimumXZ.x || candidate.x > boundary.MaximumXZ.x ||
                candidate.z < boundary.MinimumXZ.y || candidate.z > boundary.MaximumXZ.y)) continue;
            if (!CatJumpClearanceResolver.EndsClear(actor, from, candidate, rotation, false, false, out var rejected))
            {
                // A fixed support preparation cannot be repaired by a longer
                // landing along the same direction. Avoid repeated queries.
                if (rejected.phase <= CatJumpMotion.Takeoff) break;
                continue;
            }
            landing = candidate; heading = rotation; return true;
        }
        return false;
    }
}
