using System;
using System.Reflection;
using CatHome.Economy;
using NUnit.Framework;

public sealed class CatRunnerProgressionTests
{
    private DateTime now;

    [SetUp]
    public void SetUp()
    {
        now = DateTime.UtcNow;
        EconomyService.ApplyLegacyBalances(0, 0);
        CatRunnerProgressService.ApplySavedState(FreshState(now), now);
        CatRunnerRewardedAdBridge.Register(null);
    }

    [TearDown]
    public void TearDown()
    {
        EconomyService.ApplyLegacyBalances(0, 0);
        CatRunnerRewardedAdBridge.Register(null);
    }

    [Test]
    public void SaveSchema_IncludesRunnerProgressVersion()
    {
        Assert.That(CatHomeSaveSystem.CurrentSaveVersion, Is.EqualTo(11));
    }

    [Test]
    public void VersionSevenSave_MigratesRunnerProgressWithoutLosingExistingSlices()
    {
        var legacy = new CatHomeSaveData
        {
            version = 7,
            coins = 123,
            runnerEnergy = RunnerEnergySaveState.CreateDefault(now),
            homeStore = HomeStoreSaveState.CreateDefault()
        };
        MethodInfo migrate = typeof(CatHomeSaveSystem).GetMethod(
            "MigrateSaveData",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(migrate, Is.Not.Null);

        migrate.Invoke(null, new object[] { legacy });

        Assert.That(legacy.version, Is.EqualTo(11));
        Assert.That(legacy.catchLives, Is.Not.Null);
        Assert.That(legacy.catchLives.lives, Is.EqualTo(CatchLivesService.MaximumLives));
        Assert.That(legacy.coins, Is.EqualTo(123));
        Assert.That(legacy.runnerEnergy, Is.Not.Null);
        Assert.That(legacy.homeStore, Is.Not.Null);
        Assert.That(legacy.runnerProgress, Is.Not.Null);
        Assert.That(legacy.runnerProgress.soundEnabled, Is.True);
        Assert.That(legacy.runnerProgress.hapticsEnabled, Is.True);
        Assert.That(legacy.homeProgression, Is.Not.Null);
        Assert.That(legacy.homeProgression.homeXp, Is.EqualTo(0L));
        Assert.That(legacy.dailyRetention, Is.Not.Null);
        Assert.That(legacy.achievements, Is.Not.Null);
    }

    [Test]
    public void BestScore_IsMonotonicAndCaptured()
    {
        Assert.That(CatRunnerProgressService.RecordBestScore(420), Is.True);
        Assert.That(CatRunnerProgressService.RecordBestScore(200), Is.False);

        CatRunnerProgressSaveState captured =
            CatRunnerProgressService.CaptureState(now);
        Assert.That(captured.bestScore, Is.EqualTo(420));
    }

    [Test]
    public void DailyMissions_ResetOnNextUtcDay()
    {
        CatRunnerProgressSaveState state = FreshState(now);
        state.dailyCoins = 22;
        state.dailyJumps = 4;
        state.dailyDistance = 210;
        CatRunnerProgressService.ApplySavedState(state, now);

        CatRunnerProgressSaveState nextDay =
            CatRunnerProgressService.CaptureState(now.AddDays(1));

        Assert.That(nextDay.dailyCoins, Is.Zero);
        Assert.That(nextDay.dailyJumps, Is.Zero);
        Assert.That(nextDay.dailyDistance, Is.Zero);
        Assert.That(nextDay.dailyCoinsClaimed, Is.False);
    }

    [Test]
    public void CompletedDailyMission_GrantsExactlyOnce()
    {
        CatRunnerProgressSaveState state = FreshState(now);
        state.dailyCoins = CatRunnerProgressService.DailyCoinTarget - 1;
        CatRunnerProgressService.ApplySavedState(state, now);

        CatRunnerProgressService.RecordCoinPickup();
        long firstBalance = EconomyService.Coins;
        CatRunnerProgressService.RecordCoinPickup(10);

        Assert.That(firstBalance, Is.EqualTo(CatRunnerProgressService.DailyCoinReward));
        Assert.That(EconomyService.Coins, Is.EqualTo(firstBalance));
        Assert.That(
            CatRunnerProgressService.CaptureState(now).dailyCoinsClaimed,
            Is.True);
    }

    [Test]
    public void PendingResult_SettlesIdempotentlyAndClearsRecoveryRecord()
    {
        CatRunnerResult result = CatRunnerResult.Create(
            "progress-pending-run",
            50f,
            300f,
            10,
            1,
            20);
        CatRunnerProgressService.SetPendingResult(result, 500);

        Assert.That(
            CatRunnerProgressService.TrySettlePendingResult(out EconomyTransactionResult first),
            Is.True);
        Assert.That(first.IsSettled, Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(12));
        Assert.That(CatRunnerProgressService.HasPendingResult, Is.False);

        Assert.That(
            CatRunnerProgressService.TrySettlePendingResult(out EconomyTransactionResult repeated),
            Is.True);
        Assert.That(repeated.Status, Is.EqualTo(EconomyTransactionStatus.NoChange));
        Assert.That(EconomyService.Coins, Is.EqualTo(12));
    }

    [Test]
    public void CorruptProgress_IsSanitizedWithoutRestoringPendingReward()
    {
        CatRunnerProgressSaveState corrupt = FreshState(now);
        corrupt.bestScore = -40;
        corrupt.dailyMissionDayUtc = "not-a-date";
        corrupt.dailyCoins = 999;
        corrupt.pendingResult = new CatRunnerPendingResultSaveState
        {
            hasValue = true,
            runId = "bad",
            distance = float.NaN,
            durationSeconds = 20f,
            coinsCollected = 99
        };

        CatRunnerProgressService.ApplySavedState(corrupt, now);
        CatRunnerProgressSaveState safe =
            CatRunnerProgressService.CaptureState(now);

        Assert.That(safe.bestScore, Is.Zero);
        Assert.That(safe.dailyCoins, Is.Zero);
        Assert.That(safe.pendingResult.hasValue, Is.False);
    }

    [Test]
    public void RewardedAdBridge_AcceptsOnlyOneVerifiedCompletion()
    {
        var provider = new FakeRewardedProvider();
        CatRunnerRewardedAdBridge.Register(provider);
        int verifiedCompletions = 0;

        Assert.That(
            CatRunnerRewardedAdBridge.TryShow(
                verified =>
                {
                    if (verified)
                        verifiedCompletions++;
                }),
            Is.True);
        provider.Complete(true);
        provider.Complete(true);

        Assert.That(verifiedCompletions, Is.EqualTo(1));
    }

    [TestCase(0, 1)]
    [TestCase(4, 1)]
    [TestCase(5, 2)]
    [TestCase(19, 4)]
    [TestCase(999, CatRunnerGameController.MaximumComboMultiplier)]
    public void ComboMultiplier_IsBoundedAndPredictable(int streak, int expected)
    {
        Assert.That(
            CatRunnerGameController.GetComboMultiplier(streak),
            Is.EqualTo(expected));
    }

    [Test]
    public void DoubleCoins_ChangesPayoutValueOnly()
    {
        Assert.That(CatRunnerGameController.GetCollectedCoinValue(false), Is.EqualTo(1));
        Assert.That(CatRunnerGameController.GetCollectedCoinValue(true), Is.EqualTo(2));
    }

    private static CatRunnerProgressSaveState FreshState(DateTime utcNow)
    {
        CatRunnerProgressSaveState state =
            CatRunnerProgressSaveState.CreateDefault(utcNow);
        state.bestScore = 0;
        state.tutorialCompleted = false;
        state.reducedMotion = false;
        state.soundEnabled = true;
        state.hapticsEnabled = true;
        return state;
    }

    private sealed class FakeRewardedProvider : ICatRunnerRewardedAdProvider
    {
        private Action<bool> completion;

        public bool IsRewardedAdReady(string placementId)
        {
            return placementId == CatRunnerRewardedAdBridge.EnergyPlacementId;
        }

        public void ShowRewardedAd(string placementId, Action<bool> completed)
        {
            completion = completed;
        }

        public void Complete(bool verified)
        {
            completion?.Invoke(verified);
        }
    }
}
