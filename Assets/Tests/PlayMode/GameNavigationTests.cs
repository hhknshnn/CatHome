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
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class GameNavigationTests
{
    private bool hadOnboarding;
    private int savedOnboarding;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Navigation tests require an isolated QA save.");
        hadOnboarding = PlayerPrefs.HasKey(PetTutorialHint.OnboardingCompletedKey);
        savedOnboarding = PlayerPrefs.GetInt(PetTutorialHint.OnboardingCompletedKey);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, 1);
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        float deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline)
        {
            var loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader != null && loader.IsReady && !loader.IsTransitioning) break;
            yield return null;
        }
        Assert.That(Object.FindAnyObjectByType<LevelLoader>().IsReady, Is.True);
        foreach (var title in Object.FindObjectsByType<TitleScreen>(FindObjectsInactive.Include))
            title.gameObject.SetActive(false);
        foreach (var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include))
        { popup.Close(); popup.enabled = false; }
        foreach (var popup in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include))
            popup.enabled = false;
        foreach (var popup in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include))
            popup.enabled = false;
        var now = DateTime.UtcNow;
        RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState
        {
            energy = 5, regenerationAnchorUtc = now.ToString("O"),
            rewardedAdsDayUtc = now.ToString("yyyy-MM-dd"), unlimitedUntilUtc = ""
        }, now);
        var progress = CatRunnerProgressSaveState.CreateDefault(now);
        progress.tutorialCompleted = true;
        progress.soundEnabled = false;
        progress.hapticsEnabled = false;
        CatRunnerProgressService.ApplySavedState(progress, now);
        CatchLivesService.ApplySavedState(null, now);
        CatchLivesService.CompleteTutorial();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1;
        if (hadOnboarding) PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, savedOnboarding);
        else PlayerPrefs.DeleteKey(PetTutorialHint.OnboardingCompletedKey);
    }

    [UnityTest]
    public IEnumerator Leaderboard_OnlyCloseDismisses_AndReturnsToBlockedGamesHub()
    {
        var hub = Object.FindAnyObjectByType<GamesHubPanel>(FindObjectsInactive.Include);
        var board = Object.FindAnyObjectByType<LeaderboardPanel>(FindObjectsInactive.Include);
        var cat = Object.FindAnyObjectByType<CatMovement>();
        hub.Show();
        Read<Button>(hub, "leaderboardButton").onClick.Invoke();
        Assert.That(board.IsOpen, Is.True);
        Assert.That(GamesHubPanel.IsAnyOpen, Is.False);
        Assert.That(cat.IsMovementLocked, Is.True);
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.ExecuteHierarchy(board.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        var body = board.GetComponentsInChildren<TMPro.TMP_Text>(true).First(t => t.name == "CatName");
        ExecuteEvents.ExecuteHierarchy(body.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.That(board.IsOpen, Is.True, "Scrim and card body clicks must not dismiss rankings.");
        Read<Button>(board, "catchButton").onClick.Invoke();
        Read<Button>(board, "weeklyButton").onClick.Invoke();
        Assert.That(Read<CompetitionGame>(board, "game"), Is.EqualTo(CompetitionGame.CatCatch));
        Assert.That(Read<CompetitionPeriod>(board, "period"), Is.EqualTo(CompetitionPeriod.Weekly));
        pointer.scrollDelta = Vector2.down;
        board.GetComponentInChildren<ScrollRect>(true).OnScroll(pointer);
        yield return null;
        Assert.That(board.IsOpen, Is.True, "Filtering and scrolling stay inside rankings.");
        Read<Button>(board, "closeButton").onClick.Invoke();
        Assert.That(board.IsOpen, Is.False);
        Assert.That(GamesHubPanel.IsAnyOpen, Is.True);
        Assert.That(cat.IsMovementLocked && cat.AreWorldActionsBlocked, Is.True,
            "The games panel, rather than a leaked leaderboard owner, must own home input.");
        Read<Button>(board, "closeButton").onClick.Invoke();
        Assert.That(GamesHubPanel.IsAnyOpen, Is.True, "A duplicate close cannot toggle the games panel off.");
        hub.Hide();
        Assert.That(cat.IsMovementLocked || cat.AreWorldActionsBlocked, Is.False);
    }

    [UnityTest]
    public IEnumerator BothGames_AllExitScreensOfferHomeAndGames_AndFirstQueuedChoiceWins()
    {
        var hub = Object.FindAnyObjectByType<GamesHubPanel>(FindObjectsInactive.Include);
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var loader = Object.FindAnyObjectByType<LevelLoader>();
        string room = loader.CurrentRoom.Id;
        foreach (bool runner in new[] { true, false })
        foreach (string screen in new[] { "welcome", "pause", "result" })
        foreach (bool toGames in new[] { false, true })
        {
            string sceneName = runner ? CatRunnerLauncher.RunnerSceneName : CatCatchLauncher.CatchSceneName;
            hub.Show();
            Read<Button>(hub, runner ? "runnerButton" : "catchButton").onClick.Invoke();
            yield return WaitScene(sceneName, true);
            Assert.That(GamesHubPanel.IsAnyOpen, Is.False);
            Object controller = runner ? (Object)Object.FindAnyObjectByType<CatRunnerGameController>() :
                Object.FindAnyObjectByType<CatCatchGameController>();
            if (screen != "welcome")
            {
                if (runner)
                {
                    var game = (CatRunnerGameController)controller;
                    Object.FindAnyObjectByType<CatRunnerTrackManager>().enabled = false;
                    game.StartFromWelcome();
                    yield return new WaitForSeconds(2.6f);
                    if (screen == "pause") game.PauseRun();
                    else { game.RegisterCoin(); Invoke(game, "CompleteRun"); }
                }
                else
                {
                    var game = (CatCatchGameController)controller;
                    game.StartHunt();
                    if (screen == "pause") game.PauseHunt();
                    else
                    {
                        typeof(CatCatchGameController).GetField("remaining", BindingFlags.Instance | BindingFlags.NonPublic)
                            .SetValue(game, 0f);
                        Invoke(game, "Update");
                    }
                }
                yield return null;
                yield return null;
            }
            int runnerLives = RunnerEnergyService.CurrentEnergy;
            int catchLives = CatchLivesService.CurrentLives;
            long coins = EconomyService.Coins;
            string homeField = screen == "welcome" ? "welcomeExitButton" : screen == "pause" ? "pauseExitButton" : "collectButton";
            string gamesField = screen == "welcome" ? "welcomeGamesButton" : screen == "pause" ? "pauseGamesButton" : "resultGamesButton";
            var homeButton = Read<Button>(controller, homeField);
            var gamesButton = Read<Button>(controller, gamesField);
            Assert.That(homeButton != null && gamesButton != null, Is.True, sceneName + "/" + screen + " bindings");
            Assert.That(homeButton.gameObject.activeInHierarchy && gamesButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(homeButton.interactable && gamesButton.interactable, Is.True);
            (toGames ? gamesButton : homeButton).onClick.Invoke();
            (toGames ? homeButton : gamesButton).onClick.Invoke();
            if (runner) ((CatRunnerGameController)controller).StartFromWelcome();
            else ((CatCatchGameController)controller).StartHunt();
            yield return WaitScene(sceneName, false);
            yield return null;
            Assert.That(loader.CurrentRoom.Id, Is.EqualTo(room));
            Assert.That(Object.FindAnyObjectByType<CatMovement>(), Is.SameAs(cat));
            Assert.That(GamesHubPanel.IsAnyOpen, Is.EqualTo(toGames), sceneName + "/" + screen);
            Assert.That(cat.IsMovementLocked, Is.EqualTo(toGames), "Only an open games panel keeps its input owner.");
            Assert.That(cat.AreWorldActionsBlocked, Is.EqualTo(toGames));
            Assert.That(RunnerEnergyService.CurrentEnergy, Is.EqualTo(runnerLives));
            Assert.That(CatchLivesService.CurrentLives, Is.EqualTo(catchLives));
            Assert.That(EconomyService.Coins, Is.EqualTo(coins), "Changing navigation destination cannot pay the result again.");
            Assert.That(CatRunnerSessionContext.IsLaunching, Is.False);
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Count(c => c.enabled && c.targetTexture == null), Is.EqualTo(1));
            hub.Hide();
            Assert.That(cat.IsMovementLocked, Is.False);
        }
    }

    private static IEnumerator WaitScene(string sceneName, bool loaded)
    {
        float deadline = Time.realtimeSinceStartup + 30f;
        while ((SceneManager.GetSceneByName(sceneName).isLoaded != loaded || CatRunnerSessionContext.IsLaunching) &&
               Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(SceneManager.GetSceneByName(sceneName).isLoaded, Is.EqualTo(loaded));
        yield return null;
    }

    private static T Read<T>(object target, string field) =>
        (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Invoke(object target, string method) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
}
