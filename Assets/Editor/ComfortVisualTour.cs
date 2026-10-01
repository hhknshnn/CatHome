using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Real screenshots only, on the isolated QA copy. Does not provision products or build content.</summary>
public static class ComfortVisualTour
{
    public const string OutputDirectory = "Docs/QA/CARE_MOTION_2026-09-09/screens";
    public static string Status { get; private set; } = "Idle";
    public static string Error { get; private set; } = "";
    public static bool IsRunning { get; private set; }
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly List<string> evidence = new List<string>();
    static readonly List<Preference> preferences = new List<Preference>();
    static readonly Dictionary<Behaviour, bool> enabledStates = new Dictionary<Behaviour, bool>();
    static LevelLoader host;
    static bool stopRequested, snapshotHeld;
    static string previousOutput;
    static float previousTimeScale;
    static int previousCaptureRate, previousSizeIndex;
    static EditorWindow gameView;
    sealed class Preference { public string key, text; public int number; public bool existed, isString; }

    public static void Start()
    {
        if (!Application.isPlaying || !EditorQaSession.IsActive)
            throw new InvalidOperationException("ComfortVisualTour requires Play Mode and an isolated QA save.");
        if (IsRunning) throw new InvalidOperationException("The current visual tour is still running.");
        host = Object.FindAnyObjectByType<LevelLoader>();
        if (host == null || !host.IsReady || host.IsTransitioning || HomeUiFlow.IsMiniGameVisible)
            throw new InvalidOperationException("Start from a ready home room.");
        stopRequested = false; Error = ""; evidence.Clear();
        SaveSettings();
        IsRunning = true; Status = "Preparing";
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        try
        {
            Directory.CreateDirectory(OutputDirectory);
            UiQaVisualTour.OutputDirectory = OutputDirectory;
            PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, 1);
            Time.timeScale = 1f; Time.captureFramerate = 0;
            Quiet<PetTutorialHint>(); Quiet<WhileYouWereAwayPopup>();
            Quiet<CollectionCompleteCelebrationView>(); Quiet<HomeLevelUpCelebrationView>();
            host.StartCoroutine(Drive());
        }
        catch (Exception exception)
        {
            Error = exception.ToString(); RestoreSettings(); IsRunning = false;
            Status = "Failed to start: " + exception.Message;
            throw;
        }
    }

    /// <summary>Cooperative stop: finish cleanup and return to the living room before restoring settings.</summary>
    public static void Stop() { if (IsRunning) { stopRequested = true; Status = "Stopping; returning home"; } }

    static IEnumerator Drive()
    {
        try
        {
            yield return ExecuteSafely(Tour(), true);
            yield return ExecuteSafely(Cleanup(), false);
        }
        finally
        {
            RestoreSettings(); IsRunning = false;
            Status = !string.IsNullOrEmpty(Error) ? "Failed: " + Error.Split('\n')[0] :
                stopRequested ? "Stopped; settings restored" : "Complete; " + evidence.Count + " screenshots";
            File.WriteAllText(Path.Combine(OutputDirectory, "comfort-tour-status.txt"), Status + "\n" + Error);
            File.WriteAllLines(Path.Combine(OutputDirectory, "comfort-tour-files.txt"), evidence);
        }
    }

    // Unity does not propagate exceptions from nested enumerators to the parent.
    // Advance the complete nested stack here so failures still run our cleanup.
    static IEnumerator ExecuteSafely(IEnumerator routine, bool honourStop)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        try
        {
            while (stack.Count > 0 && !(honourStop && stopRequested))
            {
                object current = null; bool advanced;
                try { advanced = stack.Peek().MoveNext(); if (advanced) current = stack.Peek().Current; }
                catch (Exception exception)
                {
                    Error += (Error.Length == 0 ? "" : "\n") + Status + ": " + exception;
                    break;
                }
                if (!advanced) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
        }
        finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
    }

    static IEnumerator Tour()
    {
        ClearPanels(); yield return new WaitForSecondsRealtime(.6f);
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return ChangeRoom(room.Id);
            AssertHome(false);
            string[] items = Object.FindObjectsByType<StoreProductDisplay>()
                .Where(p => p.gameObject.activeInHierarchy && p.gameObject.scene.name == room.SceneName)
                .Select(p => p.ProductId).Where(id => !string.IsNullOrEmpty(id)).Distinct().OrderBy(id => id).ToArray();
            File.WriteAllLines(Path.Combine(OutputDirectory, "Room_" + room.SceneName + ".products.txt"), items);
            yield return Pair("Room_" + room.SceneName);
        }
        yield return ChangeRoom(HomeRoomService.LivingRoomId);
        var companion = Need<CatCompanionPanel>();
        companion.Show(false); yield return Pair("Companion_Commands"); AssertHome(true);
        companion.Show(true); yield return Pair("Companion_Guide");
        companion.Show(false);
        Get<Button[]>(companion, "commandButtons")[1].onClick.Invoke();
        yield return Wait(() => CatActivity.Active != null && CatActivity.Active.IsWaitingForRestStop,
            "real Sit rest", 6);
        companion.Show(false); yield return new WaitForSecondsRealtime(.15f);
        Require(Get<Button[]>(companion, "commandButtons").All(b => !b.interactable), "Busy commands must be disabled.");
        Require(Get<Button>(companion, "stop").gameObject.activeInHierarchy, "Rest must retain its stop action.");
        yield return Pair("Companion_SitRest_CommandsDisabled");
        companion.Close(); CatActionState.CancelForTransition(Need<CatMovement>());
        yield return new WaitForSecondsRealtime(.3f);

        var hub = Need<GamesHubPanel>(); var board = Need<LeaderboardPanel>();
        hub.Show(); Get<Button>(hub, "leaderboardButton").onClick.Invoke();
        Require(board.IsOpen, "Leaderboard did not open from Games.");
        yield return Pair("Leaderboard_RealState"); AssertHome(true);
        Get<Button>(board, "closeButton").onClick.Invoke();
        Require(!board.IsOpen && GamesHubPanel.IsAnyOpen, "Kapat did not return to Games.");
        yield return Pair("Leaderboard_Close_ReturnsToGames");
        hub.Hide(); AssertHome(false);

        foreach (bool runner in new[] { true, false })
        foreach (string screen in new[] { "welcome", "pause", "result" })
        foreach (bool toGames in new[] { false, true })
            yield return GameCase(runner, screen, toGames);
    }

    static IEnumerator GameCase(bool runner, string screen, bool toGames)
    {
        string name = runner ? CatRunnerLauncher.RunnerSceneName : CatCatchLauncher.CatchSceneName;
        string shot = (runner ? "Runner" : "Catch") + "_" + screen + "_" + (toGames ? "ToGames" : "ToHome");
        Status = shot;
        var now = DateTime.UtcNow;
        var runnerLives = RunnerEnergySaveState.CreateDefault(now); runnerLives.energy = 5;
        RunnerEnergyService.ApplySavedState(runnerLives, now);
        var progress = CatRunnerProgressService.CaptureState(now); progress.tutorialCompleted = true;
        CatRunnerProgressService.ApplySavedState(progress, now);
        var catchLives = CatchLivesSaveState.CreateDefault(now); catchLives.tutorialCompleted = true;
        CatchLivesService.ApplySavedState(catchLives, now);
        string roomId = host.CurrentRoom.Id; var cat = Need<CatMovement>();
        var hub = Need<GamesHubPanel>(); hub.Show();
        Get<Button>(hub, runner ? "runnerButton" : "catchButton").onClick.Invoke();
        yield return Wait(() => SceneManager.GetSceneByName(name).isLoaded && !CatRunnerSessionContext.IsLaunching, name, 30);
        yield return null;
        Component game = runner ? (Component)Need<CatRunnerGameController>() : Need<CatCatchGameController>();
        if (screen != "welcome")
        {
            if (runner)
            {
                var run = (CatRunnerGameController)game;
                Get<Button>(run, "welcomeStartButton").onClick.Invoke();
                yield return Wait(() => run.IsRunning, "Runner countdown", 8);
                if (screen == "pause") run.PauseRun();
                else { run.RegisterCoin(); Call(run, "CompleteRun"); yield return Wait(() => Get<bool>(run, "resultSettled"), "Runner settlement", 8); }
            }
            else
            {
                var hunt = (CatCatchGameController)game;
                Get<Button>(hunt, "welcomeStartButton").onClick.Invoke();
                if (screen == "pause") hunt.PauseHunt();
                else { Set(hunt, "remaining", 0f); Call(hunt, "Update"); }
            }
        }
        string homeField = screen == "welcome" ? "welcomeExitButton" : screen == "pause" ? "pauseExitButton" : "collectButton";
        string gamesField = screen == "welcome" ? "welcomeGamesButton" : screen == "pause" ? "pauseGamesButton" : "resultGamesButton";
        var home = Get<Button>(game, homeField); var games = Get<Button>(game, gamesField);
        Require(home != null && games != null && home.gameObject.activeInHierarchy && games.gameObject.activeInHierarchy,
            shot + " must show both exit choices.");
        yield return Pair(shot);
        int beforeRunner = RunnerEnergyService.CurrentEnergy, beforeCatch = CatchLivesService.CurrentLives;
        (toGames ? games : home).onClick.Invoke();
        yield return Wait(() => !SceneManager.GetSceneByName(name).isLoaded, name + " unload", 30);
        yield return new WaitForSecondsRealtime(.3f);
        Require(host.CurrentRoom.Id == roomId && Need<CatMovement>() == cat, "Game return changed room or home cat.");
        Require(GamesHubPanel.IsAnyOpen == toGames, "Game return selected the wrong destination.");
        Require(RunnerEnergyService.CurrentEnergy == beforeRunner && CatchLivesService.CurrentLives == beforeCatch,
            "Returning from a mini-game spent another life.");
        AssertHome(toGames);
        yield return Shot(shot + "_Returned");
        hub.Hide(); AssertHome(false);
    }

    static IEnumerator Pair(string name)
    {
        foreach (int width in new[] { 1920, 1440 })
        {
            UiQaVisualTour.Resolution(width, 1080);
            yield return Wait(() => Screen.width == width && Screen.height == 1080, "resolution " + width, 10);
            yield return new WaitForSecondsRealtime(.7f);
            yield return Shot(name + "_" + width + "x1080");
        }
    }
    static IEnumerator Shot(string name)
    {
        Status = "Capturing " + name;
        string path = Path.Combine(OutputDirectory, name + ".png");
        DateTime previousWrite = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        UiQaVisualTour.Capture(name);
        yield return Wait(() => File.Exists(path) && File.GetLastWriteTimeUtc(path) != previousWrite,
            "screenshot " + name, 15);
        evidence.Add(name + ".png");
    }
    static IEnumerator ChangeRoom(string roomId)
    {
        Status = "Room " + roomId; ClearPanels();
        CatActionState.CancelForTransition(Object.FindAnyObjectByType<CatMovement>());
        Require(host.LoadRoom(roomId), "Cannot load " + roomId + "; provision the isolated QA collection first.");
        yield return Wait(() => host.IsReady && !host.IsTransitioning && host.CurrentRoom.Id == roomId, roomId, 30);
        var cat = Need<CatMovement>();
        var spawn = Object.FindObjectsByType<LevelSpawnPoint>()
            .FirstOrDefault(p => p.gameObject.scene == cat.gameObject.scene && p.SpawnPointId == host.CurrentRoom.SpawnPointId);
        Require(spawn != null, "Missing room spawn for " + roomId);
        cat.ApplySavedWorldPose(spawn.transform.position, spawn.transform.rotation);
        yield return new WaitForSecondsRealtime(.8f);
    }
    static IEnumerator Cleanup()
    {
        if (!Application.isPlaying) yield break;
        Status = "Restoring home";
        Time.timeScale = 1;
        var runner = Object.FindAnyObjectByType<CatRunnerGameController>();
        if (runner != null)
        {
            if (Get<bool>(runner, "showingWelcome")) runner.ExitFromWelcome();
            else if (runner.IsRunning || Get<bool>(runner, "countdownActive") || runner.IsPaused)
            { if (!runner.IsPaused) runner.PauseRun(); runner.ExitFromPause(); }
            else Call(runner, "CollectAndReturnHome");
        }
        Object.FindAnyObjectByType<CatCatchGameController>()?.ExitToHome();
        yield return Wait(() => !HomeUiFlow.IsMiniGameVisible, "final game unload", 30);
        ClearPanels();
        if (host != null) yield return ChangeRoom(HomeRoomService.LivingRoomId);
        AssertHome(false);
    }
    static IEnumerator Wait(Func<bool> ready, string label, float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
        Require(ready(), "Timed out waiting for " + label);
    }
    static void AssertHome(bool blocked)
    {
        var cat = Need<CatMovement>();
        Require(cat.IsMovementLocked == blocked && cat.AreWorldActionsBlocked == blocked, "Unexpected home input owner.");
        Require(Object.FindObjectsByType<CatMovement>().Length == 1, "Expected one home cat.");
        Require(Object.FindObjectsByType<Camera>().Count(c => c.enabled && c.targetTexture == null) == 1,
            "Expected one rendering world camera.");
    }
    static void ClearPanels() { Object.FindAnyObjectByType<CatCompanionPanel>()?.Close(); UiQaVisualTour.Clear(); }
    static T Need<T>() where T : Component => UiQaVisualTour.Find<T>() ?? throw new InvalidOperationException("Missing " + typeof(T).Name);
    static T Get<T>(object target, string field) => UiQaVisualTour.Get<T>(target, field);
    static void Set(object target, string field, object value) => UiQaVisualTour.Set(target, field, value);
    static void Call(object target, string method) => UiQaVisualTour.Call(target, method);
    static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
    static void Quiet<T>() where T : Behaviour
    {
        foreach (var component in Object.FindObjectsByType<T>(FindObjectsInactive.Include))
        { enabledStates[component] = component.enabled; component.enabled = false; }
    }

    static void SaveSettings()
    {
        preferences.Clear(); enabledStates.Clear();
        foreach (string key in new[] { "cat.identity.breed.v1", "CatHome_CatName", "CatHome_CollectionMilestones", "cat-home.local-guest-id" })
            preferences.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), isString = true, text = PlayerPrefs.GetString(key, "") });
        foreach (string key in new[] { "cat.identity.coat", "home.audio.sound", "home.audio.music", "cat-home.language", "Player_LightLevel", "CatHome_PetTutorialCompleted", "CatHome_IntroductionCompleted", "CatHome_IntroductionStep", "CatHome_OnboardingStep", "CatHome_OnboardingCompleted", "cat-home.account-kind", "CatRunner_BestScore_v1" })
            preferences.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), number = PlayerPrefs.GetInt(key, 0) });
        previousOutput = UiQaVisualTour.OutputDirectory; previousTimeScale = Time.timeScale; previousCaptureRate = Time.captureFramerate;
        gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        previousSizeIndex = (int)gameView.GetType().GetProperty("selectedSizeIndex", Flags).GetValue(gameView);
        snapshotHeld = true;
    }
    static void RestoreSettings()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        if (!snapshotHeld) return;
        snapshotHeld = false;
        foreach (var item in preferences)
        {
            if (!item.existed) PlayerPrefs.DeleteKey(item.key);
            else if (item.isString) PlayerPrefs.SetString(item.key, item.text);
            else PlayerPrefs.SetInt(item.key, item.number);
        }
        foreach (var pair in enabledStates) if (pair.Key != null) pair.Key.enabled = pair.Value;
        PlayerPrefs.Save(); Time.timeScale = previousTimeScale; Time.captureFramerate = previousCaptureRate;
        UiQaVisualTour.OutputDirectory = previousOutput;
        if (gameView != null) { gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(gameView, previousSizeIndex); gameView.Repaint(); }
    }
    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.ExitingPlayMode || !IsRunning) return;
        stopRequested = true; RestoreSettings(); IsRunning = false; Status = "Stopped because Play Mode ended; settings restored";
    }
}
