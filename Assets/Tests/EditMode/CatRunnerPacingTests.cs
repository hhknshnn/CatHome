using NUnit.Framework;
using UnityEngine;

public sealed class CatRunnerPacingTests
{
    [TestCase(0f, 1)]
    [TestCase(29.99f, 1)]
    [TestCase(30f, 2)]
    [TestCase(89.99f, 3)]
    [TestCase(90f, 4)]
    public void CurtainNumber_AdvancesEveryThirtySeconds(float elapsed, int expected)
    {
        Assert.That(CatRunnerGameController.GetCurtainNumber(elapsed), Is.EqualTo(expected));
    }

    [TestCase(0f, 0)]
    [TestCase(30f, 90)]
    [TestCase(60f, 180)]
    [TestCase(90f, 270)]
    [TestCase(120f, 360)]
    public void CoinSchedule_KeepsExactlyNinetyPerCurtain(float elapsed, int expected)
    {
        Assert.That(CatRunnerGameController.GetScheduledCoinCount(elapsed), Is.EqualTo(expected));
    }

    [Test]
    public void Difficulty_IncreasesWithoutIncreasingCoinDensity()
    {
        Assert.That(CatRunnerGameController.GetSpeedMultiplier(1), Is.EqualTo(1f));
        Assert.That(CatRunnerGameController.GetSpeedMultiplier(2), Is.EqualTo(1.1f).Within(0.001f));
        Assert.That(CatRunnerGameController.GetSpeedMultiplier(8), Is.EqualTo(1.7f).Within(0.001f));
        Assert.That(CatRunnerTrackManager.GetObstacleInterval(3),
            Is.LessThan(CatRunnerTrackManager.GetObstacleInterval(1)));
        Assert.That(CatRunnerTrackManager.GetDoubleObstacleChance(3),
            Is.GreaterThan(CatRunnerTrackManager.GetDoubleObstacleChance(1)));
    }

    [Test]
    public void JumpFormation_RisesOverObstacleAndReturnsToFloor()
    {
        Assert.That(CatRunnerTrackManager.GetFormationCoinHeight(0, true), Is.EqualTo(0.42f));
        Assert.That(CatRunnerTrackManager.GetFormationCoinHeight(1, true), Is.EqualTo(0.78f));
        Assert.That(CatRunnerTrackManager.GetFormationCoinHeight(2, true), Is.EqualTo(1.18f));
        Assert.That(CatRunnerTrackManager.GetFormationCoinHeight(3, true), Is.EqualTo(0.78f));
        Assert.That(CatRunnerTrackManager.GetFormationCoinHeight(4, true), Is.EqualTo(0.42f));
    }

    [TestCase(0f, 50f, true)]
    [TestCase(35f, 50f, true)]
    [TestCase(60f, 30f, false)]
    [TestCase(0f, 40f, false)]
    [TestCase(0f, -80f, false)]
    public void SwipeClassification_PrioritizesIntentionalUpwardMotion(
        float x,
        float y,
        bool expectedJump)
    {
        Assert.That(CatRunnerPlayer.IsJumpSwipe(new Vector2(x, y)), Is.EqualTo(expectedJump));
    }
}
