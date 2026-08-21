using System;
using System.Globalization;
using NUnit.Framework;

public sealed class LocalNotificationTests
{
    private static readonly DateTime Noon =
        new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void SetUp()
    {
        LocalNotificationService.Clear();
        RunnerEnergyService.ApplySavedState(EnergyState(2, Noon), Noon);
    }

    [TearDown]
    public void TearDown()
    {
        LocalNotificationService.Clear();
        RunnerEnergyService.ApplySavedState(EnergyState(5, Noon), Noon);
    }

    [Test]
    public void RefreshSchedules_QueuesEnergyDailyAndOfflineWhenEnergyIsLow()
    {
        LocalNotificationService.RefreshSchedules(Noon);

        Assert.That(LocalNotificationService.Scheduled.Count, Is.EqualTo(3));
        Assert.That(Find(LocalNotificationService.EnergyFullId).FireUtc, Is.GreaterThan(Noon));
        Assert.That(Find(LocalNotificationService.DailyReadyId).FireUtc,
            Is.EqualTo(Noon.Date.AddDays(1)));
        Assert.That(Find(LocalNotificationService.OfflineRewardId).FireUtc,
            Is.EqualTo(Noon.AddHours(4)));
    }

    [Test]
    public void FullEnergy_SkipsTheEnergyNotice()
    {
        RunnerEnergyService.ApplySavedState(EnergyState(5, Noon), Noon);
        LocalNotificationService.RefreshSchedules(Noon);

        Assert.That(LocalNotificationService.Scheduled.Count, Is.EqualTo(2));
        for (int i = 0; i < LocalNotificationService.Scheduled.Count; i++)
            Assert.That(LocalNotificationService.Scheduled[i].Id,
                Is.Not.EqualTo(LocalNotificationService.EnergyFullId));
    }

    [Test]
    public void Clear_DropsPendingNotices()
    {
        LocalNotificationService.RefreshSchedules(Noon);
        LocalNotificationService.Clear();
        Assert.That(LocalNotificationService.Scheduled.Count, Is.Zero);
    }

    private static LocalNotificationService.ScheduledNotice Find(string id)
    {
        for (int i = 0; i < LocalNotificationService.Scheduled.Count; i++)
        {
            if (string.Equals(LocalNotificationService.Scheduled[i].Id, id, StringComparison.Ordinal))
                return LocalNotificationService.Scheduled[i];
        }

        Assert.Fail("Missing notice " + id);
        return default;
    }

    private static RunnerEnergySaveState EnergyState(int energy, DateTime anchor)
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
