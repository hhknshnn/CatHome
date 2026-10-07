using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class SharedLivesPlayTests
{
    GamesHubPanel hub;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Flags).Invoke(target,args);
    [UnitySetUp] public IEnumerator SetUp()
    {
        Assert.That(EditorQaSession.IsActive,Is.True,"Copied-save session required.");
        Application.runInBackground=true;Time.timeScale=1;yield return LoadHome();
        MiniGameLivesService.ApplySavedState(MiniGameLivesSaveState.CreateDefault(DateTime.UtcNow),DateTime.UtcNow);
        var r=CatRunnerProgressSaveState.CreateDefault(DateTime.UtcNow);r.tutorialCompleted=true;CatRunnerProgressService.ApplySavedState(r,DateTime.UtcNow);
        CatchLivesService.CompleteTutorial();
        // These unused legacy pools must not gate any of the four games.
        var empty=RunnerEnergySaveState.CreateDefault(DateTime.UtcNow);empty.energy=0;RunnerEnergyService.ApplySavedState(empty,DateTime.UtcNow);
        var c=CatchLivesSaveState.CreateDefault(DateTime.UtcNow);c.lives=0;c.tutorialCompleted=true;CatchLivesService.ApplySavedState(c,DateTime.UtcNow);
    }
    IEnumerator LoadHome()
    {
        yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
        float end=Time.realtimeSinceStartup+20;
        while((Object.FindAnyObjectByType<LevelLoader>()==null||!Object.FindAnyObjectByType<LevelLoader>().IsReady)&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(Object.FindAnyObjectByType<LevelLoader>().IsReady,Is.True);
        hub=Object.FindAnyObjectByType<GamesHubPanel>(FindObjectsInactive.Include);
        var title=Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);if(title!=null)Call(title,"HideImmediate");
        Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include)?.Close();
    }
    IEnumerator Launch(int index)
    {
        hub.Show();yield return null;
        if(index==0)Call(hub,"PlayRunner");else if(index==1)Call(hub,"PlayCatch");else hub.PlayCozy(index==2?CozyGameKind.Yarn:CozyGameKind.Pond);
        float end=Time.realtimeSinceStartup+15;
        while((index==0?Object.FindAnyObjectByType<CatRunnerGameController>()==null:index==1?Object.FindAnyObjectByType<CatCatchGameController>()==null:Object.FindAnyObjectByType<CozyMiniGame>()==null)&&Time.realtimeSinceStartup<end)yield return null;
        yield return null;
    }
    IEnumerator Returned()
    {
        float end=Time.realtimeSinceStartup+15;
        while(HomeUiFlow.IsMiniGameVisible&&Time.realtimeSinceStartup<end)yield return null;
        Assert.That(HomeUiFlow.IsMiniGameVisible,Is.False);yield return null;
    }
    [UnityTest] public IEnumerator FourGamesUseSamePool_MenuFree_DoubleStartSafe_PauseFree_RetryCostsOne_ReloadPersists()
    {
        int expected=20;
        for(int index=0;index<4;index++)
        {
            yield return Launch(index);Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(expected));
            if(index==0)
            {
                var g=Object.FindAnyObjectByType<CatRunnerGameController>();Assert.That(g,Is.Not.Null);
                Object.FindAnyObjectByType<CatRunnerTrackManager>().enabled=false;
                g.StartFromWelcome();g.StartFromWelcome();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(--expected));
                yield return new WaitForSeconds(3);g.PauseRun();g.ResumeRun();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(expected));
                Call(g,"CompleteRun");yield return null;Call(g,"CollectAndRetry");Call(g,"CollectAndRetry");
                Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(--expected));yield return new WaitForSeconds(3);g.PauseRun();g.ReturnToGamesFromPause();
            }
            else if(index==1)
            {
                var g=Object.FindAnyObjectByType<CatCatchGameController>();Assert.That(g,Is.Not.Null);
                g.StartHunt();g.StartHunt();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(--expected));
                g.PauseHunt();g.ResumeHunt();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(expected));
                Call(g,"FinishHunt",true);yield return null;g.StartHunt();g.StartHunt();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(--expected));g.PauseHunt();g.ReturnToGamesFromPause();
            }
            else
            {
                var g=Object.FindAnyObjectByType<CozyMiniGame>();Assert.That(g,Is.Not.Null);
                g.StartRound();g.StartRound();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(--expected));
                g.Pause();g.Resume();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(expected));
                g.FinishRound();yield return null;g.StartRound();g.StartRound();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(--expected));g.Exit(true);
            }
            yield return Returned();hub.Show();yield return null;
            Assert.That(hub.transform.Find("SafeArea/GamesHubCard/SharedLives/Count").GetComponent<TMPro.TMP_Text>().text,Does.Contain(expected+"/20"));
        }
        Assert.That(expected,Is.EqualTo(12));yield return null;CatHomeSaveSystem.SaveNow();yield return null;
        yield return LoadHome();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(12));
    }
    [UnityTest] public IEnumerator EmptySharedPoolBlocksAllFourGames_AndNewGamesReenableAfterRefill()
    {
        for(int index=0;index<4;index++)
        {
            var s=MiniGameLivesSaveState.CreateDefault(DateTime.UtcNow);s.lives=0;MiniGameLivesService.ApplySavedState(s,DateTime.UtcNow);
            yield return Launch(index);
            if(index==0){var g=Object.FindAnyObjectByType<CatRunnerGameController>();g.StartFromWelcome();Assert.That(g.IsRunning,Is.False);g.ReturnToGamesFromWelcome();}
            else if(index==1){var g=Object.FindAnyObjectByType<CatCatchGameController>();g.StartHunt();Assert.That(g.IsHunting,Is.False);g.ReturnToGamesFromWelcome();}
            else
            {
                var g=Object.FindAnyObjectByType<CozyMiniGame>();var start=g.GetComponentsInChildren<Button>(true).First(b=>b.name=="Start");
                g.StartRound();Assert.That(g.IsRunning,Is.False);Assert.That(start.interactable,Is.False);
                Assert.That(MiniGameLivesService.CurrentLives,Is.Zero);
                s.regenerationAnchorUtc=DateTime.UtcNow.AddMinutes(-10).ToString("O");MiniGameLivesService.ApplySavedState(s,DateTime.UtcNow);yield return null;
                Assert.That(start.interactable,Is.True);start.onClick.Invoke();Assert.That(g.IsRunning,Is.True);Assert.That(MiniGameLivesService.CurrentLives,Is.Zero);g.Exit(true);
            }
            yield return Returned();Assert.That(MiniGameLivesService.CurrentLives,Is.Zero);
        }
    }
    [UnityTearDown] public IEnumerator TearDown(){Time.timeScale=1;yield return LoadHome();}
}
