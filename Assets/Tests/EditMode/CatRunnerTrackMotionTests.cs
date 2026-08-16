using NUnit.Framework;
using UnityEngine;

public sealed class CatRunnerTrackMotionTests
{
    [Test]
    public void TrackObjects_ApproachFasterThanVisualFloor()
    {
        var gameObject = new GameObject("TrackMotionTest");
        try
        {
            CatRunnerTrackManager track = gameObject.AddComponent<CatRunnerTrackManager>();

            Assert.That(track.ObjectApproachSpeedMultiplier, Is.GreaterThan(1f));
            Assert.That(7f * track.ObjectApproachSpeedMultiplier, Is.GreaterThan(7f));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void RoadProfile_IsRollingButRemainsGentle()
    {
        float start = CatRunnerTrackManager.GetRoadHeight(0f);
        float later = CatRunnerTrackManager.GetRoadHeight(40f);

        Assert.That(later, Is.Not.EqualTo(start).Within(0.01f));
        Assert.That(Mathf.Abs(CatRunnerTrackManager.GetRoadSlope(40f)), Is.LessThan(0.08f));
    }

    [Test]
    public void RunnerScore_CombinesDistanceAndPawCoins()
    {
        Assert.That(CatRunnerGameController.CalculateScore(125.4f, 7), Is.EqualTo(195));
        Assert.That(CatRunnerGameController.CalculateScore(-10f, -2), Is.Zero);
    }
}
