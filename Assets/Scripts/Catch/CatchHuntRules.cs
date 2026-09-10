using UnityEngine;

/// <summary>
/// Pure hunt tuning shared by the cat, the mice and the hunt controller. Kept
/// side-effect free so the chase rules can be asserted in EditMode tests.
/// </summary>
public static class CatchHuntRules
{
    public const float CatRunSpeed = 3.7f;
    public const float PounceTriggerDistance = 1.5f;
    public const float PounceMaximumDistance = 1.9f;
    public const float PounceAlignmentDegrees = 28f;
    public const float TurnDegreesPerSecond = 300f;

    /// <summary>
    /// A chase that drags on this long lunges anyway. Guarantees the hunt always
    /// resolves instead of the cat trailing a mouse forever.
    /// </summary>
    public const float ForcedPounceAfterSeconds = 2.5f;
    public const float PounceTravelSeconds = 0.38f;
    public const float PouncePrepareSeconds = 0.10f;
    public const float PounceRecoverSeconds = 0.38f;
    public const float StrikeRadius = 0.32f;
    public const float TapTargetRadius = 0.9f;

    public const float MouseWanderSpeed = 1.5f;
    public const float MouseFleeSpeed = 2.85f;
    public const float MaximumInterceptLeadSeconds = 0.5f;
    public const float MousePanicSpeed = 4.2f;
    public const float MousePanicSeconds = 0.8f;
    public const float MouseFleeDistance = 2f;

    /// <summary>
    /// A mouse can only sprint for so long. Without this a tangent-fleeing mouse
    /// is mathematically uncatchable: the cat loses more to turning than it gains
    /// in speed, and the chase never ends.
    /// </summary>
    public const float MouseFleeStaminaSeconds = 2f;
    public const float MouseFleeRecoverSeconds = 0.7f;
    public const float MouseSpawnGrace = 0.5f;
    public const float MouseSpawnClearance = 2.15f;

    /// <summary>
    /// The cat may only launch when it is close enough and already facing its
    /// prey, so a pounce reads as a committed lunge instead of a snap turn.
    /// </summary>
    public static bool ShouldPounce(float planarDistance, float facingAngleDegrees)
    {
        if (planarDistance > PounceTriggerDistance)
            return false;
        return facingAngleDegrees <= PounceAlignmentDegrees;
    }

    public static bool ShouldForcePounce(float planarDistance, float chaseSeconds, float facingAngleDegrees = 0f)
    {
        return chaseSeconds >= ForcedPounceAfterSeconds && planarDistance <= PounceMaximumDistance &&
               facingAngleDegrees <= PounceAlignmentDegrees;
    }

    public static float PounceDistanceFor(float planarDistance)
    {
        return Mathf.Clamp(planarDistance, 0f, PounceMaximumDistance);
    }

    /// <summary>
    /// A cat lunges where the mouse will be, not where it was. Without this lead
    /// the pounce always lands behind a fleeing mouse by roughly its own travel
    /// distance, and no amount of tuning the radius fixes that.
    /// </summary>
    public static Vector3 PredictPreyPoint(Vector3 preyPosition, Vector3 preyVelocity)
    {
        Vector3 lead = preyVelocity * PounceTravelSeconds;
        lead.y = 0f;
        preyPosition.y = 0f;
        return preyPosition + lead;
    }

    /// <summary>
    /// Where the cat should run to cut a fleeing mouse off. Chasing the mouse's
    /// current position means trailing it forever once it turns; aiming at where
    /// it will be by the time the cat arrives closes the gap.
    /// </summary>
    public static Vector3 PredictInterceptPoint(
        Vector3 preyPosition, Vector3 preyVelocity, Vector3 catPosition, float catSpeed)
    {
        preyPosition.y = 0f;
        catPosition.y = 0f;
        preyVelocity.y = 0f;
        Vector3 toPrey = preyPosition - catPosition;
        float distance = toPrey.magnitude;
        if (catSpeed <= 0.01f || distance <= 0.01f)
            return preyPosition;

        // Only lead the sideways part of the mouse's run. Leading the part that
        // moves straight away from the cat pushes the aim point further out and
        // the chase settles into a stalemate the cat can never close.
        Vector3 radial = toPrey / distance;
        Vector3 tangentVelocity = preyVelocity - radial * Vector3.Dot(preyVelocity, radial);
        float leadSeconds = Mathf.Min(distance / catSpeed, MaximumInterceptLeadSeconds);
        return preyPosition + tangentVelocity * leadSeconds;
    }

    public static bool IsWithinStrike(float planarDistance) => planarDistance <= StrikeRadius;
}
