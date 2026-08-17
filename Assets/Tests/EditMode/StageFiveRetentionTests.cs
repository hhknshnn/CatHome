using System;
using CatHome.Economy;
using NUnit.Framework;
using QuestState = CatHome.Quests.QuestState;

public sealed class StageFiveRetentionTests
{
    [SetUp]
    public void SetUp()
    {
        EconomyService.ApplyLegacyBalances(0, 0);
        ProgressionService.ApplySavedState(0, 0, 0, 1, Array.Empty<QuestProgressEntry>());
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        CatRunnerProgressSaveState runner =
            CatRunnerProgressSaveState.CreateDefault(DateTime.UtcNow);
        runner.bestScore = 0;
        runner.tutorialCompleted = false;
        CatRunnerProgressService.ApplySavedState(runner, DateTime.UtcNow);
        DailyRetentionService.ApplySavedState(DailyRetentionSaveState.CreateDefault());
        AchievementService.ApplySavedState(AchievementSaveState.CreateDefault());
    }

    [TearDown]
    public void TearDown()
    {
        EconomyService.ApplyLegacyBalances(0, 0);
        DailyRetentionService.ApplySavedState(DailyRetentionSaveState.CreateDefault());
        AchievementService.ApplySavedState(AchievementSaveState.CreateDefault());
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        ProgressionService.ApplySavedState(0, 0, 0, 1, Array.Empty<QuestProgressEntry>());
    }

    [Test]
    public void DailyLogin_PaysOncePerUtcDayAndStacksStreak()
    {
        DateTime day = new DateTime(2026, 8, 18, 9, 0, 0, DateTimeKind.Utc);
        Assert.That(DailyRetentionService.NotifySessionStart(day), Is.True);
        Assert.That(DailyRetentionService.LoginStreak, Is.EqualTo(1));
        Assert.That(EconomyService.Coins, Is.EqualTo(15));
        Assert.That(DailyRetentionService.NotifySessionStart(day.AddHours(3)), Is.False);
        Assert.That(EconomyService.Coins, Is.EqualTo(15));

        Assert.That(DailyRetentionService.NotifySessionStart(day.AddDays(1)), Is.True);
        Assert.That(DailyRetentionService.LoginStreak, Is.EqualTo(2));
        Assert.That(EconomyService.Coins, Is.EqualTo(40));
    }

    [Test]
    public void SeventhLogin_GrantsADiamond()
    {
        var state = DailyRetentionSaveState.CreateDefault();
        state.lastLoginDayUtc = "2026-08-17";
        state.loginStreak = 6;
        DailyRetentionService.ApplySavedState(state);

        Assert.That(
            DailyRetentionService.NotifySessionStart(
                new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc)),
            Is.True);
        Assert.That(DailyRetentionService.LoginStreak, Is.EqualTo(7));
        Assert.That(EconomyService.Diamonds, Is.EqualTo(1));
    }

    [Test]
    public void DailyQuests_TrackAndPayOnce()
    {
        var snapshots = new System.Collections.Generic.List<QuestSnapshot>();
        DailyRetentionService.CaptureDailyQuests(snapshots);
        Assert.That(snapshots.Count, Is.EqualTo(3));

        QuestSnapshot first = snapshots[0];
        Assert.That(DailyRetentionService.IsDailyQuestId(first.QuestId), Is.True);
        Assert.That(first.State, Is.EqualTo(QuestState.Active));

        var type = (QuestType)Enum.Parse(
            typeof(QuestType),
            first.QuestId.Substring(first.QuestId.LastIndexOf(':') + 1));
        long coinsBefore = EconomyService.Coins;
        long bondBefore = ProgressionService.BondXp;
        Assert.That(DailyRetentionService.RecordProgress(type), Is.True);
        Assert.That(ProgressionService.TryClaimQuest(first.QuestId), Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(coinsBefore + 15));
        Assert.That(ProgressionService.BondXp, Is.EqualTo(bondBefore + 6));
        Assert.That(ProgressionService.TryClaimQuest(first.QuestId), Is.False);
        Assert.That(EconomyService.Coins, Is.EqualTo(coinsBefore + 15));
    }

    [Test]
    public void Achievements_PayOnceAndCanGrantDiamonds()
    {
        CatRunnerProgressService.CompleteTutorial();
        Assert.That(AchievementService.Evaluate(), Is.GreaterThan(0));
        Assert.That(AchievementService.IsUnlocked(AchievementService.FirstRunId), Is.True);
        long coins = EconomyService.Coins;
        Assert.That(AchievementService.Evaluate(), Is.EqualTo(0));
        Assert.That(EconomyService.Coins, Is.EqualTo(coins));

        ProgressionService.AddBondXp(BondMilestoneService.BirdWatchBond);
        Assert.That(AchievementService.IsUnlocked(AchievementService.Bond250Id), Is.True);
        Assert.That(EconomyService.Diamonds, Is.EqualTo(1));
    }

    [Test]
    public void DoubleCoinsAd_UsesASeparateTransaction()
    {
        var result = CatRunnerResult.Create("run-double-1", 60f, 120f, 10, 0, 0);
        Assert.That(CatRunnerRewardService.Grant(result).Succeeded, Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(10));
        Assert.That(CatRunnerRewardService.GrantDouble(result).Succeeded, Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(20));
        Assert.That(CatRunnerRewardService.GrantDouble(result).IsSettled, Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(20));
    }
}
