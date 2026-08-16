using NUnit.Framework;
using UnityEngine;

public sealed class CatRunnerPremiumMechanicsTests
{
    [TestCase(-80f, 5f, CatRunnerGestureDirection.Left)]
    [TestCase(80f, 5f, CatRunnerGestureDirection.Right)]
    [TestCase(8f, 80f, CatRunnerGestureDirection.Up)]
    [TestCase(8f, -80f, CatRunnerGestureDirection.Down)]
    [TestCase(30f, 30f, CatRunnerGestureDirection.None)]
    public void GestureClassification_UsesDominantAxisInEveryDirection(
        float x,
        float y,
        CatRunnerGestureDirection expected)
    {
        Assert.That(
            CatRunnerPlayer.ClassifySwipe(new Vector2(x, y)),
            Is.EqualTo(expected));
    }

    [Test]
    public void DragLaneTarget_IsResolutionRelativeAndClamped()
    {
        float oneLaneOnPhone = CatRunnerPlayer.GetDraggedLaneTarget(0f, 238f, 1080f, 1f);
        float oneLaneOnTablet = CatRunnerPlayer.GetDraggedLaneTarget(0f, 451f, 2048f, 1f);

        Assert.That(oneLaneOnPhone, Is.EqualTo(1f).Within(.01f));
        Assert.That(oneLaneOnTablet, Is.EqualTo(1f).Within(.01f));
        Assert.That(CatRunnerPlayer.GetDraggedLaneTarget(0f, -900f, 1080f), Is.EqualTo(-1f));
        Assert.That(CatRunnerPlayer.GetDraggedLaneTarget(0f, 900f, 1080f), Is.EqualTo(1f));
    }

    [Test]
    public void SwipeThreshold_UsesDpiWhenAvailableAndResolutionAsFallback()
    {
        float mdpi = CatRunnerPlayer.GetScaledSwipeThreshold(45f, 1920f, 1080f, 160f);
        float highDpi = CatRunnerPlayer.GetScaledSwipeThreshold(45f, 2400f, 1080f, 320f);
        float resolutionFallback = CatRunnerPlayer.GetScaledSwipeThreshold(45f, 1280f, 720f, 0f);

        Assert.That(mdpi, Is.EqualTo(45f).Within(.001f));
        Assert.That(highDpi, Is.EqualTo(90f).Within(.001f));
        Assert.That(resolutionFallback, Is.EqualTo(30f).Within(.01f));
    }

    [Test]
    public void ElevatedRoute_RisesToAStableDeckAndReturnsSafely()
    {
        const float platformHeight = .82f;
        Assert.That(
            CatRunnerTrackObject.SamplePlatformHeight(6.5f, platformHeight, 13f, 3f),
            Is.Zero);
        Assert.That(
            CatRunnerTrackObject.SamplePlatformHeight(5f, platformHeight, 13f, 3f),
            Is.InRange(.35f, .47f));
        Assert.That(
            CatRunnerTrackObject.SamplePlatformHeight(0f, platformHeight, 13f, 3f),
            Is.EqualTo(platformHeight).Within(.001f));
        Assert.That(
            CatRunnerTrackObject.SamplePlatformHeight(-5f, platformHeight, 13f, 3f),
            Is.InRange(.35f, .47f));
        Assert.That(
            CatRunnerTrackObject.SamplePlatformHeight(-6.5f, platformHeight, 13f, 3f),
            Is.Zero);
    }

    [Test]
    public void ElevatedRoute_OnlySupportsItsOwnLane()
    {
        GameObject root = new GameObject("PremiumPlatformTest");
        try
        {
            CatRunnerTrackObject platform = root.AddComponent<CatRunnerTrackObject>();
            platform.EditorConfigure(CatRunnerTrackObjectKind.Platform, .82f, 13f, 3f, .65f);
            platform.InitializeRuntime(0f);

            Assert.That(platform.TrySamplePlatformHeight(0f, out float centerHeight), Is.True);
            Assert.That(centerHeight, Is.EqualTo(.82f).Within(.001f));
            Assert.That(platform.TrySamplePlatformHeight(1.35f, out _), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PlatformSurface_IncludesRoadOffsetButSpawnLiftRemainsRelative()
    {
        GameObject root = new GameObject("RoadOffsetPlatformTest");
        try
        {
            root.transform.localPosition = new Vector3(0f, .27f, 0f);
            CatRunnerTrackObject platform = root.AddComponent<CatRunnerTrackObject>();
            platform.EditorConfigure(CatRunnerTrackObjectKind.Platform, .82f, 13f, 3f, .65f);
            platform.InitializeRuntime(0f);

            Assert.That(platform.TrySamplePlatformHeight(0f, out float playerSurface), Is.True);
            Assert.That(playerSurface, Is.EqualTo(1.09f).Within(.001f));
            Assert.That(
                platform.TrySamplePlatformRelativeHeightAt(0f, 0f, out float spawnLift),
                Is.True);
            Assert.That(spawnLift, Is.EqualTo(.82f).Within(.001f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void HazardCollision_RequiresRealVerticalIntervalOverlap()
    {
        Assert.That(
            CatRunnerTrackObject.HasVerticalOverlap(0f, .68f, 0f, .48f),
            Is.True);
        Assert.That(
            CatRunnerTrackObject.HasVerticalOverlap(.5f, 1.18f, 0f, .48f),
            Is.False,
            "A cat whose feet cleared the obstacle top must not be hit.");
        Assert.That(
            CatRunnerTrackObject.HasVerticalOverlap(0f, .68f, .82f, 1.3f),
            Is.False,
            "An obstacle entirely above the cat must not be treated as a hit.");
        Assert.That(
            CatRunnerTrackObject.HasVerticalOverlap(1.2f, 1.88f, 0f, .48f),
            Is.False,
            "An obstacle entirely below the cat must not be treated as a hit.");
    }

    [Test]
    public void OverheadHazard_OverlapsStandingCatButClearsSlidingCat()
    {
        GameObject root = new GameObject("OverheadHazardTest");
        try
        {
            CatRunnerTrackObject hazard = root.AddComponent<CatRunnerTrackObject>();
            hazard.EditorConfigure(CatRunnerTrackObjectKind.OverheadObstacle);
            hazard.InitializeRuntime(0f);

            Assert.That(hazard.OverlapsVerticalInterval(0f, .68f), Is.True);
            Assert.That(hazard.OverlapsVerticalInterval(0f, .34f), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void PremiumPatternCadence_RemainsReadableAsDifficultyRises()
    {
        Assert.That(CatRunnerTrackManager.GetElevationInterval(0f), Is.EqualTo(8.5f));
        Assert.That(CatRunnerTrackManager.GetElevationInterval(1f), Is.EqualTo(12.5f));
        Assert.That(
            CatRunnerTrackManager.GetOverheadObstacleChance(6),
            Is.GreaterThan(CatRunnerTrackManager.GetOverheadObstacleChance(1)));
        Assert.That(CatRunnerTrackManager.GetOverheadObstacleChance(99), Is.LessThanOrEqualTo(.38f));
        Assert.That(
            CatRunnerTrackManager.MaximumHazardLanesPerRow,
            Is.LessThan(3),
            "Every obstacle row must retain at least one readable route.");
    }

    [TestCase(2f, -2f, true)]
    [TestCase(.8f, .2f, true)]
    [TestCase(2f, .8f, false)]
    [TestCase(-.8f, -2f, false)]
    public void SweptCollision_DetectsFastObjectsCrossingPlayerBand(
        float previousZ,
        float currentZ,
        bool expected)
    {
        Assert.That(
            CatRunnerTrackManager.CrossesInteractionBand(previousZ, currentZ, .55f),
            Is.EqualTo(expected));
    }

    [Test]
    public void PowerUpTemplate_RetainsItsSpecificKind()
    {
        GameObject root = new GameObject("PowerUpKindTest");
        try
        {
            CatRunnerTrackObject powerUp = root.AddComponent<CatRunnerTrackObject>();
            powerUp.EditorConfigure(
                CatRunnerTrackObjectKind.PowerUp,
                pickupKind: CatRunnerPowerUpKind.DoubleCoins);

            Assert.That(powerUp.Kind, Is.EqualTo(CatRunnerTrackObjectKind.PowerUp));
            Assert.That(powerUp.PowerUpKind, Is.EqualTo(CatRunnerPowerUpKind.DoubleCoins));
            Assert.That(powerUp.IsPickup, Is.True);
            Assert.That(powerUp.IsHazard, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
