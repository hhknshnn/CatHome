using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class MiniGameLivesTests
{
    static readonly DateTime Now=new DateTime(2026,10,6,12,0,0,DateTimeKind.Utc);
    MiniGameLivesSaveState previous;
    [SetUp] public void SetUp(){previous=MiniGameLivesService.CaptureState(DateTime.UtcNow);MiniGameLivesService.ApplySavedState(State(20,Now),Now);}
    [TearDown] public void TearDown()=>MiniGameLivesService.ApplySavedState(previous,DateTime.UtcNow);
    static MiniGameLivesSaveState State(int count,DateTime anchor){var s=MiniGameLivesSaveState.CreateDefault(anchor);s.lives=count;return s;}

    [Test] public void TwentyStartsConsumeThePoolAndTwentyFirstIsRejected()
    {
        for(int i=0;i<20;i++)Assert.That(MiniGameLivesService.TrySpendLife(Now),Is.True);
        Assert.That(MiniGameLivesService.TrySpendLife(Now),Is.False);
        Assert.That(MiniGameLivesService.CanStartRound(Now),Is.False);
        Assert.That(MiniGameLivesService.CaptureState(Now).lives,Is.Zero);
    }
    [Test] public void FullPoolStartsOneClockAndSpendingAgainDoesNotRestartIt()
    {
        MiniGameLivesService.TrySpendLife(Now);MiniGameLivesService.TrySpendLife(Now.AddMinutes(9));
        Assert.That(MiniGameLivesService.TimeUntilNextLife(Now.AddMinutes(9)),Is.EqualTo(TimeSpan.FromMinutes(1)));
        Assert.That(MiniGameLivesService.CaptureState(Now.AddMinutes(10)).lives,Is.EqualTo(19));
        Assert.That(MiniGameLivesService.CaptureState(Now.AddMinutes(20)).lives,Is.EqualTo(20));
    }
    [Test] public void OfflineRefillPreservesPartialIntervalAndCapsAtTwenty()
    {
        MiniGameLivesService.ApplySavedState(State(0,Now),Now);
        Assert.That(MiniGameLivesService.CaptureState(Now.AddMinutes(35)).lives,Is.EqualTo(3));
        Assert.That(MiniGameLivesService.TimeUntilNextLife(Now.AddMinutes(35)),Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(MiniGameLivesService.CaptureState(Now.AddYears(100)).lives,Is.EqualTo(20));
    }
    [Test] public void ReloadKeepsEmptyPoolAndRemainingClock()
    {
        MiniGameLivesService.ApplySavedState(State(0,Now),Now);
        var saved=JsonUtility.FromJson<MiniGameLivesSaveState>(JsonUtility.ToJson(MiniGameLivesService.CaptureState(Now.AddMinutes(7))));
        MiniGameLivesService.ApplySavedState(State(20,Now),Now);
        MiniGameLivesService.ApplySavedState(saved,Now.AddMinutes(7));
        Assert.That(MiniGameLivesService.CanStartRound(Now.AddMinutes(7)),Is.False);
        Assert.That(MiniGameLivesService.TimeUntilNextLife(Now.AddMinutes(7)),Is.EqualTo(TimeSpan.FromMinutes(3)));
    }
    [Test] public void BackwardsClockCannotCreateLives()
    {
        MiniGameLivesService.ApplySavedState(State(0,Now),Now);
        Assert.That(MiniGameLivesService.CaptureState(Now.AddHours(-1)).lives,Is.Zero);
        Assert.That(MiniGameLivesService.TimeUntilNextLife(Now.AddHours(-1)),Is.EqualTo(TimeSpan.FromMinutes(70)));
    }
    [Test] public void AdRewardsShareOneDailyCap()
    {
        MiniGameLivesService.ApplySavedState(State(0,Now),Now);
        for(int i=0;i<3;i++)Assert.That(MiniGameLivesService.TryGrantRewardedAd(Now),Is.True);
        Assert.That(MiniGameLivesService.CaptureState(Now).lives,Is.EqualTo(6));
        Assert.That(MiniGameLivesService.TryGrantRewardedAd(Now),Is.False);
        var state=MiniGameLivesService.CaptureState(Now);state.lives=0;state.regenerationAnchorUtc=Now.AddDays(1).ToString("O");
        MiniGameLivesService.ApplySavedState(state,Now.AddDays(1));Assert.That(MiniGameLivesService.TryGrantRewardedAd(Now.AddDays(1)),Is.True);
    }
    [Test] public void UnlimitedEntitlementBypassesSpendingUntilExpiry()
    {
        MiniGameLivesService.ActivateUnlimitedUntil(Now.AddMinutes(1),Now);
        MiniGameLivesService.TrySpendLife(Now);Assert.That(MiniGameLivesService.CaptureState(Now).lives,Is.EqualTo(20));
        MiniGameLivesService.TrySpendLife(Now.AddMinutes(2));Assert.That(MiniGameLivesService.CaptureState(Now.AddMinutes(2)).lives,Is.EqualTo(19));
    }
    [TestCase(6)][TestCase(12)][TestCase(13)] public void LegacyMigrationGrantsTwentyAndPreservesProgressAndEntitlements(int version)
    {
        var runner=RunnerEnergySaveState.CreateDefault(Now);runner.energy=0;runner.unlimitedUntilUtc=DateTime.UtcNow.AddDays(2).ToString("O");
        var catcher=CatchLivesSaveState.CreateDefault(Now);catcher.lives=1;catcher.tutorialCompleted=true;
        var data=new CatHomeSaveData{version=version,runnerEnergy=runner,catchLives=catcher,coins=735,diamonds=8,cozyGames=new CozyGameSaveState{yarnStars=new[]{3,2,1},pondBest=12}};
        typeof(CatHomeSaveSystem).GetMethod("MigrateSaveData",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{data});
        Assert.That(data.version,Is.EqualTo(14));Assert.That(data.miniGameLives.lives,Is.EqualTo(20));
        Assert.That(data.miniGameLives.unlimitedUntilUtc,Is.EqualTo(runner.unlimitedUntilUtc));
        Assert.That(data.coins,Is.EqualTo(735));Assert.That(data.diamonds,Is.EqualTo(8));
        if(version>=12){Assert.That(data.cozyGames.yarnStars[0],Is.EqualTo(3));Assert.That(data.cozyGames.pondBest,Is.EqualTo(12));Assert.That(data.catchLives.tutorialCompleted,Is.True);}
    }
    [Test] public void MigrationCombinesTodaysAdClaimsWithoutMutatingLegacyStates()
    {
        var a=RunnerEnergySaveState.CreateDefault(Now);var b=CatchLivesSaveState.CreateDefault(Now);
        a.rewardedAdsClaimedToday=2;b.rewardedAdsClaimedToday=2;b.unlimitedUntilUtc=Now.AddDays(3).ToString("O");
        var migrated=MiniGameLivesService.FromLegacy(a,b,Now);
        Assert.That(migrated.rewardedAdsClaimedToday,Is.EqualTo(3));Assert.That(migrated.unlimitedUntilUtc,Is.EqualTo(b.unlimitedUntilUtc));
        Assert.That(a.energy,Is.EqualTo(5));Assert.That(b.lives,Is.EqualTo(5));
    }
    [TestCase(-9,0)][TestCase(999,20)] public void InvalidCountsAreClamped(int input,int expected)
    {
        MiniGameLivesService.ApplySavedState(new MiniGameLivesSaveState{lives=input,regenerationAnchorUtc="broken"},Now);
        Assert.That(MiniGameLivesService.CaptureState(Now).lives,Is.EqualTo(expected));
    }
    [Test] public void NewHomeJourneyPreservesSharedLivesInsteadOfRefillingThem()
    {
        var now=DateTime.UtcNow;MiniGameLivesService.ApplySavedState(State(7,now),now);
        var reset=(CatHomeSaveData)typeof(CatHomeSaveSystem).GetMethod("CreateNewGameData",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{now,null,RunnerEnergySaveState.CreateDefault(now),CatRunnerProgressSaveState.CreateDefault(now),CatchLivesSaveState.CreateDefault(now)});
        Assert.That(reset.miniGameLives.lives,Is.EqualTo(7));
        Assert.That(reset.miniGameLives.regenerationAnchorUtc,Is.EqualTo(now.ToString("O")));
    }
}
