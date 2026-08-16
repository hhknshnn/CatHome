using System;
using System.Globalization;
using NUnit.Framework;

public sealed class RunnerEnergyServiceTests
{
    private static readonly DateTime Start =
        new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void SetUp()
    {
        RunnerEnergyService.ApplySavedState(State(5, Start), Start);
    }

    [Test]
    public void StartingRun_SpendsOneEnergyAndStartsTenMinuteTimer()
    {
        Assert.That(RunnerEnergyService.TrySpendRunEnergy(Start), Is.True);
        Assert.That(RunnerEnergyService.CaptureState(Start).energy, Is.EqualTo(4));

        RunnerEnergyService.Refresh(Start.AddMinutes(9).AddSeconds(59));
        Assert.That(RunnerEnergyService.CaptureState(
            Start.AddMinutes(9).AddSeconds(59)).energy, Is.EqualTo(4));

        RunnerEnergyService.Refresh(Start.AddMinutes(10));
        Assert.That(RunnerEnergyService.CaptureState(Start.AddMinutes(10)).energy,
            Is.EqualTo(5));
    }

    [Test]
    public void OfflineRegeneration_UsesWholeIntervalsAndCapsAtFive()
    {
        RunnerEnergyService.ApplySavedState(State(1, Start), Start);
        DateTime afterThirtyFiveMinutes = Start.AddMinutes(35);

        RunnerEnergyService.Refresh(afterThirtyFiveMinutes);

        Assert.That(RunnerEnergyService.CaptureState(afterThirtyFiveMinutes).energy,
            Is.EqualTo(4));
        Assert.That(RunnerEnergyService.TimeUntilNextEnergy(afterThirtyFiveMinutes),
            Is.EqualTo(TimeSpan.FromMinutes(5)));

        RunnerEnergyService.Refresh(Start.AddHours(5));
        Assert.That(RunnerEnergyService.CaptureState(Start.AddHours(5)).energy,
            Is.EqualTo(5));
    }

    [Test]
    public void ClockMovingBackwards_NeverCreatesEnergy()
    {
        RunnerEnergyService.ApplySavedState(State(2, Start), Start);

        RunnerEnergyService.Refresh(Start.AddHours(-2));

        Assert.That(RunnerEnergyService.CaptureState(Start.AddHours(-2)).energy,
            Is.EqualTo(2));
        Assert.That(RunnerEnergyService.TimeUntilNextEnergy(Start.AddHours(-2)),
            Is.EqualTo(TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(10))));
    }

    [Test]
    public void RewardedAd_GrantsTwoAndHonorsDailyLimit()
    {
        RunnerEnergyService.ApplySavedState(State(0, Start), Start);

        for (int claim = 0; claim < 3; claim++)
        {
            Assert.That(RunnerEnergyService.TryGrantRewardedAd(Start), Is.True);
            Assert.That(RunnerEnergyService.TrySpendRunEnergy(Start), Is.True);
            Assert.That(RunnerEnergyService.TrySpendRunEnergy(Start), Is.True);
        }

        Assert.That(RunnerEnergyService.TryGrantRewardedAd(Start), Is.False);
        Assert.That(RunnerEnergyService.CaptureState(Start).rewardedAdsClaimedToday,
            Is.EqualTo(3));

        DateTime nextDay = Start.AddDays(1);
        RunnerEnergySaveState nextDayEmpty = State(0, nextDay);
        nextDayEmpty.rewardedAdsClaimedToday = 3;
        nextDayEmpty.rewardedAdsDayUtc = Start.ToString(
            "yyyy-MM-dd", CultureInfo.InvariantCulture);
        RunnerEnergyService.ApplySavedState(nextDayEmpty, nextDay);
        Assert.That(RunnerEnergyService.TryGrantRewardedAd(nextDay), Is.True);
    }

    [Test]
    public void RewardedAd_IsHiddenWhenTwoEnergyWouldOverflow()
    {
        RunnerEnergyService.ApplySavedState(State(4, Start), Start);

        Assert.That(RunnerEnergyService.CanClaimRewardedAd(Start), Is.False);
        Assert.That(RunnerEnergyService.TryGrantRewardedAd(Start), Is.False);
    }

    [Test]
    public void UnlimitedPass_BypassesEntrySpendButNotAfterExpiry()
    {
        RunnerEnergyService.ApplySavedState(State(2, Start), Start);
        Assert.That(RunnerEnergyService.ActivateUnlimitedUntil(
            Start.AddDays(7), Start), Is.True);

        Assert.That(RunnerEnergyService.TrySpendRunEnergy(Start.AddDays(1)), Is.True);
        Assert.That(RunnerEnergyService.CaptureState(Start.AddDays(1)).energy,
            Is.EqualTo(5), "Normal regeneration continues behind the entitlement.");

        DateTime expired = Start.AddDays(8);
        Assert.That(RunnerEnergyService.IsUnlimitedAt(expired), Is.False);
        Assert.That(RunnerEnergyService.TrySpendRunEnergy(expired), Is.True);
        Assert.That(RunnerEnergyService.CaptureState(expired).energy, Is.EqualTo(4));
    }

    private static RunnerEnergySaveState State(int energy, DateTime anchor)
    {
        return new RunnerEnergySaveState
        {
            energy = energy,
            regenerationAnchorUtc = anchor.ToString("O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = string.Empty,
            rewardedAdsClaimedToday = 0,
            rewardedAdsDayUtc = anchor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }
}
