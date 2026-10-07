using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CatHome.Economy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class CozyGamesPlayTests
{
    CozyMiniGame game;
    [UnitySetUp] public IEnumerator SetUp()
    {
        Assert.That(EditorQaSession.IsActive,Is.True,"Run in an isolated copied-save QA session.");
        Application.runInBackground=true;Time.timeScale=1;
        yield return SceneManager.LoadSceneAsync("GameScene",LoadSceneMode.Single);
        float until=Time.realtimeSinceStartup+20;
        while(Time.realtimeSinceStartup<until)
        {
            var loader=Object.FindAnyObjectByType<LevelLoader>();if(loader!=null&&loader.IsReady)break;
            yield return null;
        }
        Assert.That(Object.FindAnyObjectByType<LevelLoader>().IsReady,Is.True);
        MiniGameLivesService.ApplySavedState(MiniGameLivesSaveState.CreateDefault(DateTime.UtcNow),DateTime.UtcNow);
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale=1;
        if(game!=null){game.Exit(false);yield return null;}
        foreach(string name in new[]{CozyMiniGame.YarnScene,CozyMiniGame.PondScene})
        {var scene=SceneManager.GetSceneByName(name);if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);}
    }
    IEnumerator Launch(CozyGameKind kind)
    {
        var host=new GameObject("Cozy launch test");var launcher=host.AddComponent<CozyGameLauncher>();
        launcher.Launch(kind);
        float until=Time.realtimeSinceStartup+15;
        while((game=Object.FindAnyObjectByType<CozyMiniGame>())==null&&Time.realtimeSinceStartup<until)yield return null;
        yield return null;
        Assert.That(game,Is.Not.Null);Assert.That(SceneManager.GetActiveScene(),Is.EqualTo(game.gameObject.scene));
        Assert.That(Object.FindObjectsByType<AudioListener>().Count(l=>l.enabled),Is.EqualTo(1));
        game.StartRound();Assert.That(game.IsRunning,Is.True);
    }

    static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    static T Get<T>(object target,string field)=>(T)target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    static Vector2[] RoutePulls(){var first=new[]{
            new Vector2(1.60783994f,0.715856493f),
            new Vector2(1.09163368f,1.746979f),
            new Vector2(1.9357667f,0.704561412f),
            new Vector2(0.782927871f,1.93781424f),
            new Vector2(1.73268855f,1.16871309f),
            new Vector2(0.916195691f,1.87847948f),
            new Vector2(-1.73268831f,1.16871333f),
            new Vector2(1.45183587f,-1.50342023f),
            new Vector2(-1.50342f,1.45183611f),
            new Vector2(1.52420473f,0.879999995f),
            new Vector2(1.90211308f,-0.618033886f),
            new Vector2(1.28673244f,1.64694238f)
        };return first.Concat(first.Select(p=>-p)).ToArray();}

    [UnityTest] public IEnumerator Yarn_AllTwentyFourBoards_PointerBasketCenterAndRewardOnce()
    {
        yield return Launch(CozyGameKind.Yarn);CozyGameProgress.Apply(new CozyGameSaveState());long before=EconomyService.Coins;
        var routes=RoutePulls();
        Set(game,"clock",600f); // Geometry acceptance covers every puzzle separately from the normal timed-round test.
        var pointer=game.GetComponentInChildren<CozyGamePointer>();Time.timeScale=2;
        for(int level=0;level<24;level++)
        {
            Assert.That(game.Level,Is.EqualTo(level));Assert.That(game.CanShoot,Is.True);
            var e=new PointerEventData(EventSystem.current){pointerId=-1,position=game.gameCamera.WorldToScreenPoint(game.ball.position)};
            ExecuteEvents.Execute(pointer.gameObject,e,ExecuteEvents.pointerDownHandler);yield return null;
            var destination=game.ball.position-new Vector3(routes[level].x,0,routes[level].y);e.position=game.gameCamera.WorldToScreenPoint(destination);
            ExecuteEvents.Execute(pointer.gameObject,e,ExecuteEvents.dragHandler);yield return null;
            Assert.That(game.transform.Find("DottedAimTrail").gameObject.activeSelf,Is.True);
            ExecuteEvents.Execute(pointer.gameObject,e,ExecuteEvents.pointerUpHandler);
            Assert.That(game.Shots,Is.EqualTo(1));
            float until=Time.realtimeSinceStartup+16;
            while(game.Solved<=level&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(game.Solved,Is.EqualTo(level+1),"Basket not reached on puzzle "+(level+1));
            var goal=CozyGameRules.Goal(level);Assert.That(Vector2.Distance(new Vector2(game.ball.localPosition.x,game.ball.localPosition.z),goal),Is.LessThan(.001f));
            Assert.That(game.ball.localPosition.y,Is.EqualTo(.31f).Within(.001f),"Yarn must settle inside the woven rim before scoring.");
            game.NextLevel();yield return null;
        }
        Assert.That(game.IsRunning,Is.False);long earned=EconomyService.Coins-before;Assert.That(earned,Is.EqualTo(CozyGameRules.YarnCoins(game.Score)));
        game.FinishRound();game.NextLevel();Assert.That(EconomyService.Coins-before,Is.EqualTo(earned));Assert.That(CozyGameProgress.TotalStars,Is.EqualTo(72));
        Time.timeScale=1;game.StartRound();Assert.That(game.Remaining,Is.EqualTo(180));Assert.That(game.Score,Is.Zero);
    }
    [UnityTest] public IEnumerator Yarn_TimeoutFreezeDiamondConfirmationResumeAndNoDoubleReward()
    {
        yield return Launch(CozyGameKind.Yarn);CozyGameProgress.Apply(new CozyGameSaveState());
        var wallet=EconomyService.CaptureState();
        try
        {
            var route=RoutePulls()[0];
            game.Shoot(route);float until=Time.realtimeSinceStartup+18;
            while(game.Solved==0&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(game.Solved,Is.EqualTo(1));game.NextLevel();game.Shoot(new Vector2(1.2f,.7f));yield return new WaitForSeconds(.2f);
            Set(game,"clock",.001f);yield return null;Assert.That(game.IsRunning,Is.False);
            var cat=game.actor.localPosition;var ball=game.ball.localPosition;var checkpoint=CozyGameProgress.Checkpoint;long paid=EconomyService.Coins;
            Assert.That(checkpoint,Is.Not.Null);Assert.That(checkpoint.level,Is.EqualTo(1));
            yield return new WaitForSeconds(.15f);Assert.That(game.actor.localPosition,Is.EqualTo(cat));Assert.That(game.ball.localPosition,Is.EqualTo(ball));
            EconomyService.ApplyLegacyBalances(EconomyService.Coins,0);game.RequestContinue();game.ConfirmContinue();Assert.That(game.IsRunning,Is.False);Assert.That(EconomyService.Diamonds,Is.Zero);
            EconomyService.ApplyLegacyBalances(EconomyService.Coins,5);game.RequestContinue();
            var confirm=Get<GameObject>(game,"confirm");confirm.transform.Find("Card/Cancel").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(EconomyService.Diamonds,Is.EqualTo(5));game.ConfirmContinue();Assert.That(game.IsRunning,Is.False);
            int livesBeforeContinue=MiniGameLivesService.CurrentLives;
            game.RequestContinue();game.ConfirmContinue();game.ConfirmContinue();
            Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(livesBeforeContinue));
            Assert.That(game.IsRunning,Is.True);Assert.That(EconomyService.Diamonds,Is.EqualTo(3));Assert.That(game.Remaining,Is.EqualTo(60));
            Assert.That(game.actor.localPosition,Is.EqualTo(cat));Assert.That(game.ball.localPosition,Is.EqualTo(ball));Assert.That(game.Score,Is.EqualTo(checkpoint.score));
            game.Pause();float time=game.Remaining;yield return new WaitForSecondsRealtime(.15f);Assert.That(game.Remaining,Is.EqualTo(time));
            var saved=JsonUtility.FromJson<CozyGameSaveState>(JsonUtility.ToJson(CozyGameProgress.Capture()));Assert.That(saved.checkpoint.continued,Is.True);
            game.Exit(false);while(game!=null)yield return null;CozyGameProgress.Apply(saved);
            var host=new GameObject("Resume test").AddComponent<CozyGameLauncher>();host.Launch(CozyGameKind.Yarn);
            until=Time.realtimeSinceStartup+15;while((game=Object.FindAnyObjectByType<CozyMiniGame>())==null&&Time.realtimeSinceStartup<until)yield return null;yield return null;
            game.RequestContinue();Assert.That(MiniGameLivesService.CurrentLives,Is.EqualTo(livesBeforeContinue));Assert.That(game.IsRunning,Is.True);Assert.That(EconomyService.Diamonds,Is.EqualTo(3));Assert.That(game.Remaining,Is.EqualTo(time));
            Assert.That(game.actor.localPosition,Is.EqualTo(saved.checkpoint.cat));Set(game,"clock",.001f);yield return null;
            Assert.That(CozyGameProgress.Checkpoint,Is.Null);Assert.That(EconomyService.Coins,Is.EqualTo(paid));game.FinishRound();game.RequestContinue();Assert.That(game.IsRunning,Is.False);Assert.That(EconomyService.Diamonds,Is.EqualTo(3));
            Assert.That(CozyGameProgress.Capture().completedRounds,Is.EqualTo(1));
        }
        finally{EconomyService.ApplySavedState(wallet);}
    }
    [UnityTest] public IEnumerator Pond_DockInputReachPauseStreakAlbumAndNaturalTimeout()
    {
        yield return Launch(CozyGameKind.Pond);CozyGameProgress.Apply(new CozyGameSaveState());long before=EconomyService.Coins;
        Assert.That(game.GetComponentsInChildren<UnityEngine.UI.Button>(true).Any(b=>b.name=="Paw"),Is.False);
        game.SelectFish(0);Assert.That(game.FishCaught,Is.Zero);game.Pause();float remaining=game.Remaining;var positions=game.fishRoot.Cast<Transform>().Select(t=>t.position).ToArray();
        yield return new WaitForSecondsRealtime(.15f);Assert.That(game.Remaining,Is.EqualTo(remaining));CollectionAssert.AreEqual(positions,game.fishRoot.Cast<Transform>().Select(t=>t.position));game.Resume();
        var pointer=game.GetComponentInChildren<CozyGamePointer>();Time.timeScale=2;
        for(int fish=0;fish<6;fish++)
        {
            float until=Time.realtimeSinceStartup+14;
            while(!game.CanCatchFish(fish)&&Time.realtimeSinceStartup<until&&game.IsRunning)
            {
                var target=game.fishRoot.GetChild(fish);var screen=game.gameCamera.WorldToScreenPoint(game.transform.TransformPoint(new Vector3(target.localPosition.x,.24f,-2.6f)));
                var drag=new PointerEventData(EventSystem.current){pointerId=-1,position=screen};
                ExecuteEvents.Execute(pointer.gameObject,drag,ExecuteEvents.pointerDownHandler);yield return null;
                ExecuteEvents.Execute(pointer.gameObject,drag,ExecuteEvents.dragHandler);ExecuteEvents.Execute(pointer.gameObject,drag,ExecuteEvents.pointerUpHandler);yield return null;
            }
            Assert.That(game.CanCatchFish(fish),Is.True,"No reachable shore window for fish "+fish);
            var e=new PointerEventData(EventSystem.current){pointerId=-1,position=game.gameCamera.WorldToScreenPoint(game.fishRoot.GetChild(fish).position)};
            ExecuteEvents.Execute(pointer.gameObject,e,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(pointer.gameObject,e,ExecuteEvents.pointerDownHandler);
            int expected=fish+1;until=Time.realtimeSinceStartup+2;
            while(game.FishCaught<expected&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(game.FishCaught,Is.EqualTo(expected),"A mint fish tap should produce one catch.");yield return new WaitForSeconds(.6f);
        }
        Assert.That(CozyGameProgress.FishCount,Is.EqualTo(6));Assert.That(game.Score,Is.GreaterThan(600));
        int canonical=Get<int>(game,"pondBasePoints")+Get<int>(game,"pondComboSteps")*25+Get<int>(game,"pondPerfect")*50+Get<int>(game,"pondBonuses")*75;Assert.That(game.Score,Is.EqualTo(canonical));
        float deadline=Time.realtimeSinceStartup+45;while(game.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.That(game.IsRunning,Is.False);Assert.That(game.Remaining,Is.Zero);Assert.That(EconomyService.Coins-before,Is.EqualTo(48));
        game.FinishRound();Assert.That(EconomyService.Coins-before,Is.EqualTo(48));
    }
    [UnityTest] public IEnumerator Cozy_CompetingLaunchPauseAbortAndReturnRestoreHome()
    {
        yield return Launch(CozyGameKind.Yarn);
        var other=new GameObject("Competing launcher").AddComponent<CozyGameLauncher>();other.Launch(CozyGameKind.Pond);yield return null;
        Assert.That(SceneManager.GetSceneByName(CozyMiniGame.PondScene).isLoaded,Is.False);
        game.Shoot(new Vector2(1,.7f));yield return new WaitForSeconds(.5f);
        game.Pause();var point=game.BallPoint;var cat=game.actor.position;
        yield return new WaitForSecondsRealtime(.2f);Assert.That(game.BallPoint,Is.EqualTo(point));Assert.That(game.actor.position,Is.EqualTo(cat));
        int rounds=CozyGameProgress.Capture().completedRounds;long coins=EconomyService.Coins;
        game.Exit(true);game.StartRound();game.Exit(false);
        float until=Time.realtimeSinceStartup+10;
        while(game!=null&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(game==null,Is.True);Assert.That(EconomyService.Coins,Is.EqualTo(coins));Assert.That(CozyGameProgress.Capture().completedRounds,Is.EqualTo(rounds));
        Assert.That(GamesHubPanel.IsAnyOpen,Is.True);
        Assert.That(Object.FindObjectsByType<Camera>().Count(c=>c.enabled&&c.targetTexture==null),Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<AudioListener>().Count(l=>l.enabled),Is.EqualTo(1));
    }
    [UnityTest] public IEnumerator Results_FourTabsSelectIndependentPersonalScores()
    {
        CozyGameProgress.Apply(new CozyGameSaveState());CozyGameProgress.RecordScore(CozyGameKind.Yarn,"qa-yarn",925,DateTime.UtcNow);CozyGameProgress.RecordScore(CozyGameKind.Pond,"qa-pond",1425,DateTime.UtcNow);
        var panel=Object.FindAnyObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);Assert.That(panel,Is.Not.Null);panel.Show();yield return null;
        foreach(var pair in new[]{new[]{"runnerButton","Eve Dönüş"},new[]{"catchButton","Pati Avı"},new[]{"yarnButton","Yumak Rotası"},new[]{"pondButton","Gölet Keyfi"}})
        {
            var button=Get<UnityEngine.UI.Button>(panel,pair[0]);Assert.That(button,Is.Not.Null);Assert.That(button.gameObject.activeInHierarchy,Is.True);button.onClick.Invoke();yield return null;
            Assert.That(Get<CompetitionGame>(panel,"game"),(pair[0]=="runnerButton"?Is.EqualTo(CompetitionGame.CatRunner):pair[0]=="catchButton"?Is.EqualTo(CompetitionGame.CatCatch):pair[0]=="yarnButton"?Is.EqualTo(CompetitionGame.YarnRoute):Is.EqualTo(CompetitionGame.PondPlay)));
            if(pair[0]=="yarnButton"||pair[0]=="pondButton")
            {
                string value=pair[0]=="yarnButton"?"925":"1,425";var label=Get<TMPro.TMP_Text>(panel,"emptyStateText");Assert.That(label.text.Replace(".",",").Replace(" ",""),Does.Contain(value));
                Assert.That(Get<TMPro.TMP_Text>(panel,"ownRankText").text,Does.Not.Contain("#1"));
            }
        }
        panel.Hide();
    }
    [UnityTest] public IEnumerator Yarn_FinalBasketAtTimeoutDoesNotSellAnEmptyContinuation()
    {
        yield return Launch(CozyGameKind.Yarn);
        typeof(CozyMiniGame).GetMethod("SetupLevel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{23});
        typeof(CozyMiniGame).GetMethod("EndLevel",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,new object[]{true});
        Set(game,"clock",.001f);yield return null;
        Assert.That(game.IsRunning,Is.False);Assert.That(CozyGameProgress.Checkpoint,Is.Null);
        game.RequestContinue();Assert.That(Get<GameObject>(game,"confirm").activeSelf,Is.False);
    }
    [UnityTest] public IEnumerator Homeward_FiniteFinishAndEndlessBoundaryStayDistinct()
    {
        var now=DateTime.UtcNow;RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState{energy=5,regenerationAnchorUtc=now.ToString("O")},now);
        var progress=CatRunnerProgressSaveState.CreateDefault(now);progress.tutorialCompleted=true;CatRunnerProgressService.ApplySavedState(progress,now);
        yield return SceneManager.LoadSceneAsync("CatRunner",LoadSceneMode.Additive);yield return null;
        Object.FindAnyObjectByType<CatRunnerTrackManager>().enabled=false;
        var runner=Object.FindAnyObjectByType<CatRunnerGameController>();Assert.That(runner.EndlessMode,Is.False);
        runner.StartFromWelcome();yield return new WaitForSeconds(3);
        typeof(CatRunnerGameController).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(runner,74.99f);
        yield return new WaitForSeconds(.1f);Assert.That(runner.IsRunning,Is.False);Assert.That(runner.ReachedHome,Is.True);
        runner.SetEndlessMode(true);typeof(CatRunnerGameController).GetMethod("CollectAndRetry",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(runner,null);yield return new WaitForSeconds(3);
        typeof(CatRunnerGameController).GetField("elapsed",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(runner,75.1f);
        yield return null;Assert.That(runner.IsRunning,Is.True);Assert.That(runner.ReachedHome,Is.False);
        runner.PauseRun();runner.ExitFromPause();yield return null;
    }
}


