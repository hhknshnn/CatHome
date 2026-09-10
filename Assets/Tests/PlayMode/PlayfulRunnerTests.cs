using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class PlayfulRunnerTests
{
    [UnityTest] public IEnumerator ScoreStar_BanksOnlyNewPoints_Pauses_Expires_AndRetryResets()
    {
        var now=DateTime.UtcNow;
        RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState{energy=5,regenerationAnchorUtc=now.ToString("O"),unlimitedUntilUtc="",rewardedAdsDayUtc=now.ToString("yyyy-MM-dd")},now);
        var progress=CatRunnerProgressSaveState.CreateDefault(now);progress.tutorialCompleted=true;progress.soundEnabled=false;progress.hapticsEnabled=false;
        CatRunnerProgressService.ApplySavedState(progress,now);
        yield return SceneManager.LoadSceneAsync("CatRunner",LoadSceneMode.Single);
        yield return null;yield return null;
        var game=Object.FindAnyObjectByType<CatRunnerGameController>();
        var track=Object.FindAnyObjectByType<CatRunnerTrackManager>();
        track.enabled=false;game.StartFromWelcome();yield return new WaitForSeconds(2.6f);
        Assert.That(game.IsGameplayActive,Is.True);
        int old=game.CurrentScore;game.RegisterPowerUp(CatRunnerPowerUpKind.ScoreStar,Vector3.zero);
        Assert.That(game.CurrentScore,Is.EqualTo(old),"A star must not duplicate previously earned points.");
        game.RegisterCoin();Assert.That(game.CurrentScore-old,Is.EqualTo(20));
        game.RegisterPowerUp(CatRunnerPowerUpKind.DoubleCoins,Vector3.zero);old=game.CurrentScore;
        game.RegisterCoin();Assert.That(game.CurrentScore-old,Is.EqualTo(40),"Score and coin bonuses compose without duplicating currency twice.");
        float remaining=game.ScoreStarSecondsRemaining;int earned=game.EarnedScoreBonus;
        game.PauseRun();yield return new WaitForSecondsRealtime(.15f);
        Assert.That(game.ScoreStarSecondsRemaining,Is.EqualTo(remaining));Assert.That(game.EarnedScoreBonus,Is.EqualTo(earned));
        game.ResumeRun();typeof(CatRunnerGameController).GetField("scoreStarRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,.12f);
        old=game.CurrentScore;yield return new WaitForSeconds(.3f);
        Assert.That(game.IsScoreStarActive,Is.False);Assert.That(game.CurrentScore,Is.GreaterThanOrEqualTo(old));
        Assert.That(game.EarnedScoreBonus,Is.GreaterThanOrEqualTo(earned));
        old=game.CurrentScore;game.RegisterCoin();Assert.That(game.CurrentScore-old,Is.EqualTo(20),"Expired score star must not affect the next coin.");
        game.RegisterPowerUp(CatRunnerPowerUpKind.MysteryGift,Vector3.zero);
        Assert.That((int)game.LastGrantedPowerUp,Is.InRange(0,3));
        typeof(CatRunnerGameController).GetMethod("ShowWelcome",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        Assert.That(game.IsScoreStarActive,Is.False);Assert.That(game.EarnedScoreBonus,Is.Zero);Assert.That(game.CurrentScore,Is.Zero);
    }
    [TearDown] public void RestoreTime(){Time.timeScale=1;}
}
