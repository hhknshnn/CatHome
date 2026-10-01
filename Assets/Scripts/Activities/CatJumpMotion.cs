using System;
using System.Collections;
using UnityEngine;

/// <summary>The original stationary Jump skeleton, synchronized to one furniture trajectory.</summary>
public static class CatJumpMotion
{
    public const float Takeoff = .23f;
    public const float Touchdown = .61f;
    public const float ClipSeconds = 1.733333f;

    public static IEnumerator Play(CatMovement cat, Vector3 from, Vector3 to,
        Quaternion launch, Quaternion arrival, bool landOnSupport, float clearance = .20f,
        Action<int, float> stage = null, bool preserveLaunchHeading = false)
    {
        var animation = cat.GetComponent<CatActivityAnimation>();
        var pose = to.y > from.y + .01f ? CatActivityPose.TowelJumpUp :
            to.y < from.y - .01f ? CatActivityPose.TowelJumpDown : CatActivityPose.Hop;
        bool ascending = to.y > from.y + .01f;
        var owner = CatActivity.Active;
        bool preparedLaunch = owner != null && ascending &&
            owner.TryConsumeGroundLaunch(cat, out _);
        if (preparedLaunch)
        {
            // Prompt and action accepted this exact stance. No hidden approach,
            // offset walk or pre-jump pivot is permitted after the click.
            from = cat.transform.position;
            launch = cat.transform.rotation;
            preserveLaunchHeading = true;
            if (owner.StoreProductId == "balcony.hanging-chair") clearance = .15f;
        }
        if (!preparedLaunch && owner != null && ascending && from.y < .12f)
        {
            Vector3 offset = owner.transform.TransformVector(CatFurnitureJumpClearance.LaunchOffset(owner.StoreProductId));
            if (owner is SinkSipActivity fountain && owner.StoreProductId == HomeStoreService.PatioWaterFountainId && fountain.SipTarget != null)
            {
                // Approach along the drinking tangent. Turning across the
                // centre of the narrow basin would sweep through its stem.
                var tangent = fountain.SipTarget.position-to; tangent.y=0;
                offset = to-tangent.normalized*.80f-from; offset.y=0;
            }
            if (offset.sqrMagnitude > .0001f)
            {
                Vector3 candidate = from + offset; candidate.y = from.y;
                if (!CatActivityMotion.IsFloorClear(candidate, .31f) ||
                    !CatActivityMotion.TryFloorPath(from, candidate, out var path))
                { owner.CancelForTransition(); yield break; }
                for (int i = 0; i < path.Count; i++)
                {
                    var point = path[i]; point.y = from.y;
                    var travel = point-cat.transform.position; travel.y=0;
                    yield return CatActivityMotion.WalkAuthoredStep(cat, point,
                        travel.sqrMagnitude>.001f?Quaternion.LookRotation(travel):cat.transform.rotation, .12f);
                }
                from = candidate;
            }
            if (owner.StoreProductId == "balcony.hanging-chair") clearance = .15f;
        }
        if (owner != null && !ascending && !landOnSupport && owner.StoreProductId == "balcony.hanging-chair")
            clearance = .15f;
        if (owner != null && owner.HasPreparedStart && !ascending && !landOnSupport)
        {
            Vector3 landing; Quaternion facing;
            Vector3 measuredHeading = owner.transform.TransformDirection(CatFurnitureJumpClearance.DownHeading(owner.StoreProductId));
            if (!CatActivityStartResolver.GroundLanding(cat, from, to, out landing, out facing, measuredHeading))
            { owner.CancelForTransition(); yield break; }
            to = landing; launch = arrival = facing;
        }
        Vector3 direction = to - from; direction.y = 0f;
        if (!preserveLaunchHeading && direction.sqrMagnitude > .001f) launch = Quaternion.LookRotation(direction);
        // Finish facing the actual route before jumping. A descent carries
        // this heading through touchdown rather than turning in mid-air.
        if (!ascending) arrival = launch;
        if (!preparedLaunch) yield return TurnDirectly(cat, from, launch);
        // Recheck against the actual supported visual offset AFTER the allowed
        // direct pivot. Source curves, jump timing and flight equations stay
        // unchanged. An unexpected obstacle cannot inherit a cached approval.
        if (owner != null && owner.HasPreparedStart &&
            !CatJumpClearanceResolver.EndsClear(cat, from, to, launch, landOnSupport, preparedLaunch, out _))
        { owner.CancelForTransition(); yield break; }
        animation.BeginNativeJump(landOnSupport,CatActivityStartResolver.LandsOnSupport(cat,to));
        float seconds = Takeoff * ClipSeconds, elapsed = 0f;
        stage?.Invoke(0, 0f);
        while (elapsed < seconds)
        {
            cat.transform.SetPositionAndRotation(from, launch);
            animation.SetNativeJumpSample(pose, Mathf.Lerp(0f, Takeoff, elapsed / seconds), 0f);
            yield return null; elapsed += Time.deltaTime;
        }

        const float gravity = 9.81f;
        float apex = Mathf.Max(from.y, to.y) + (ascending ? CatFurnitureJumpClearance.AscentRise(owner!=null?owner.StoreProductId:null,clearance) :
            CatFurnitureJumpClearance.DescentRise(owner != null ? owner.StoreProductId : null,clearance));
        float rise = Mathf.Sqrt(2f * (apex - from.y) / gravity);
        float fall = Mathf.Sqrt(2f * (apex - to.y) / gravity);
        seconds = rise + fall; elapsed = 0f;
        while (elapsed < seconds)
        {
            float t = Mathf.Clamp01(elapsed / seconds);
            // A launch carries horizontal momentum immediately. The old t²
            // easing climbed almost vertically, then accelerated in mid-air.
            // Keep the accepted downward trajectory and landing timing.
            // Clear the hanging chair's opening before rising into its rim.
            // The source clip, launch/landing poses and vertical arc stay fixed.
            float travelPower = CatFurnitureJumpClearance.DescentTravelPower(owner != null ? owner.StoreProductId : null);
            float horizontal = ascending ? t : 1f - Mathf.Pow(1f - t, travelPower);
            Vector3 position = Vector3.Lerp(from, to, horizontal);
            position.y = from.y + gravity * rise * elapsed - .5f * gravity * elapsed * elapsed;
            float turn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.15f, .85f, t));
            cat.transform.SetPositionAndRotation(position, ascending ? launch : Quaternion.Slerp(launch, arrival, turn));
            animation.SetNativeJumpSample(pose, Mathf.Lerp(Takeoff, Touchdown, t), t, ascending);
            stage?.Invoke(1, t);
            yield return null; elapsed += Time.deltaTime;
        }

        seconds = (1f - Touchdown) * ClipSeconds; elapsed = 0f;
        stage?.Invoke(2, 1f);
        while (elapsed < seconds)
        {
            cat.transform.SetPositionAndRotation(to, ascending ? launch : arrival);
            animation.SetNativeJumpSample(pose, Mathf.Lerp(Touchdown, 1f, elapsed / seconds), 1f);
            yield return null; elapsed += Time.deltaTime;
        }
        cat.transform.SetPositionAndRotation(to, ascending ? launch : arrival);
        animation.SetNativeJumpSample(pose, 1f, 1f);
        yield return null;
        // Let the original landing recover fully, then use one short pivot.
        // No repeated paw lifts, opening arc or backwards/forwards settling.
        if (ascending && Quaternion.Angle(launch, arrival) > .5f)
            yield return TurnDirectly(cat, to, arrival);
    }
    // One short body pivot using the same existing gait/contact overlay as
    // ground turns. Flight, touchdown, root centre and route stay authored.
    private static IEnumerator TurnDirectly(CatMovement cat, Vector3 centre, Quaternion target)
    {
        Quaternion first = cat.transform.rotation;
        float angle = Quaternion.Angle(first, target);
        if (angle < .5f) yield break;
        var animation = cat.GetComponent<CatActivityAnimation>();
        var contact = new GameObject("Natural pivot support") { hideFlags = HideFlags.DontSave };
        contact.transform.position = centre;
        var natural = CatNaturalTurnMotion.For(cat);
        animation.BeginSupportedPivot(contact.transform);
        float duration = Mathf.Clamp(angle / 300f, .18f, .50f), elapsed = 0f;
        try
        {
            while (elapsed < duration)
            {
                if (Time.timeScale <= 0f) { yield return null; continue; }
                Quaternion before = cat.transform.rotation;
                elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
                cat.transform.SetPositionAndRotation(centre,
                    Quaternion.Slerp(first, target, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
                float rate = Vector3.SignedAngle(before * Vector3.forward, cat.transform.forward, Vector3.up) / Mathf.Max(.0001f, Time.deltaTime);
                animation.SetSupportedPivotSpeed(CatNaturalTurnMotion.GaitSpeed(rate));
                natural.Signal(rate);
                yield return null;
            }
        }
        finally
        {
            natural.Signal(0f);
            if (contact != null) UnityEngine.Object.Destroy(contact);
        }
    }
}

