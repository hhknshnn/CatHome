using NUnit.Framework;

public sealed class CatchHuntRulesTests
{
    [Test]
    public void Pounce_RequiresBeingClose()
    {
        Assert.That(
            CatchHuntRules.ShouldPounce(CatchHuntRules.PounceTriggerDistance + 0.1f, 0f),
            Is.False);
        Assert.That(
            CatchHuntRules.ShouldPounce(CatchHuntRules.PounceTriggerDistance - 0.1f, 0f),
            Is.True);
    }

    [Test]
    public void Pounce_RequiresFacingThePrey()
    {
        Assert.That(CatchHuntRules.ShouldPounce(0.8f, 90f), Is.False);
        Assert.That(
            CatchHuntRules.ShouldPounce(0.8f, CatchHuntRules.PounceAlignmentDegrees - 1f),
            Is.True);
    }

    [Test]
    public void PounceLunge_IsCappedSoTheCatNeverTeleports()
    {
        Assert.That(
            CatchHuntRules.PounceDistanceFor(12f),
            Is.EqualTo(CatchHuntRules.PounceMaximumDistance));
        Assert.That(CatchHuntRules.PounceDistanceFor(0.9f), Is.EqualTo(0.9f).Within(0.0001f));
    }

    [Test]
    public void Strike_OnlyReachesTheLandingSpot()
    {
        Assert.That(CatchHuntRules.IsWithinStrike(CatchHuntRules.StrikeRadius), Is.True);
        Assert.That(CatchHuntRules.IsWithinStrike(CatchHuntRules.StrikeRadius + 0.01f), Is.False);
    }

    [Test]
    public void PounceLeadsAFleeingMouse()
    {
        var prey = new UnityEngine.Vector3(2f, 0f, 0f);
        var velocity = new UnityEngine.Vector3(CatchHuntRules.MouseFleeSpeed, 0f, 0f);
        UnityEngine.Vector3 aim = CatchHuntRules.PredictPreyPoint(prey, velocity);

        Assert.That(aim.x, Is.GreaterThan(prey.x), "The pounce aimed behind the mouse.");
        Assert.That(
            aim.x - prey.x,
            Is.EqualTo(CatchHuntRules.MouseFleeSpeed * CatchHuntRules.PounceTravelSeconds)
                .Within(0.0001f));
    }

    [Test]
    public void PounceAimIsUnchangedForAStillMouse()
    {
        var prey = new UnityEngine.Vector3(-1f, 0f, 0.5f);
        UnityEngine.Vector3 aim = CatchHuntRules.PredictPreyPoint(prey, UnityEngine.Vector3.zero);
        Assert.That(aim, Is.EqualTo(prey));
    }

    [Test]
    public void InterceptAimsAheadOfAFleeingMouse()
    {
        var cat = new UnityEngine.Vector3(0f, 0f, 0f);
        var prey = new UnityEngine.Vector3(3f, 0f, 0f);
        var velocity = new UnityEngine.Vector3(0f, 0f, CatchHuntRules.MouseFleeSpeed);

        UnityEngine.Vector3 aim = CatchHuntRules.PredictInterceptPoint(
            prey, velocity, cat, CatchHuntRules.CatRunSpeed);

        Assert.That(aim.z, Is.GreaterThan(prey.z), "The cat aimed behind the turn.");
        Assert.That(aim.x, Is.EqualTo(prey.x).Within(0.0001f));
    }

    [Test]
    public void InterceptLeadIsCappedForDistantPrey()
    {
        var cat = UnityEngine.Vector3.zero;
        var prey = new UnityEngine.Vector3(60f, 0f, 0f);
        var velocity = new UnityEngine.Vector3(0f, 0f, 2f);

        UnityEngine.Vector3 aim = CatchHuntRules.PredictInterceptPoint(
            prey, velocity, cat, CatchHuntRules.CatRunSpeed);

        Assert.That(
            aim.z,
            Is.EqualTo(2f * CatchHuntRules.MaximumInterceptLeadSeconds).Within(0.0001f));
    }

    [Test]
    public void InterceptIgnoresPreyRunningStraightAway()
    {
        var cat = UnityEngine.Vector3.zero;
        var prey = new UnityEngine.Vector3(2f, 0f, 0f);
        var fleeingDirectlyAway = new UnityEngine.Vector3(CatchHuntRules.MouseFleeSpeed, 0f, 0f);

        UnityEngine.Vector3 aim = CatchHuntRules.PredictInterceptPoint(
            prey, fleeingDirectlyAway, cat, CatchHuntRules.CatRunSpeed);

        // Chasing the mouse itself, not a point beyond it: leading a mouse that
        // runs straight away is what stalls the chase forever.
        Assert.That(aim.x, Is.EqualTo(prey.x).Within(0.0001f));
        Assert.That(aim.z, Is.EqualTo(prey.z).Within(0.0001f));
    }

    [Test]
    public void MiceCanOutrunAStandingCatButNotAPounce()
    {
        Assert.That(CatchHuntRules.MouseFleeSpeed, Is.LessThan(CatchHuntRules.CatRunSpeed));
        Assert.That(CatchHuntRules.MousePanicSpeed, Is.GreaterThan(CatchHuntRules.CatRunSpeed));
    }

    [Test]
    public void RespawnClearance_KeepsFreshMiceOutsideStrikeRange()
    {
        Assert.That(
            CatchHuntRules.MouseSpawnClearance,
            Is.GreaterThan(CatchHuntRules.PounceMaximumDistance));
        Assert.That(CatchHuntRules.MouseSpawnGrace, Is.GreaterThan(0f));
    }
}
