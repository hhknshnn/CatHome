using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class CatRunnerLifecycleTests
{
    [UnityTest]
    public IEnumerator RunnerScene_PausesDeterministicallyAndReusesPool()
    {
        PrepareRunnerServices(5, tutorialCompleted: false);
        AsyncOperation load = SceneManager.LoadSceneAsync("CatRunner", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;

        CatRunnerGameController game =
            Object.FindAnyObjectByType<CatRunnerGameController>(FindObjectsInactive.Include);
        CatRunnerTrackManager track =
            Object.FindAnyObjectByType<CatRunnerTrackManager>(FindObjectsInactive.Include);
        Assert.That(game, Is.Not.Null);
        Assert.That(track, Is.Not.Null);
        Assert.That(
            Object.FindAnyObjectByType<CatRunnerAudioController>(FindObjectsInactive.Include),
            Is.Not.Null);
        Assert.That(
            Object.FindAnyObjectByType<CatRunnerResponsiveLayout>(FindObjectsInactive.Include),
            Is.Not.Null);
        Assert.That(
            Object.FindObjectsByType<SafeAreaRect>(FindObjectsInactive.Include).Length,
            Is.GreaterThanOrEqualTo(4));

        int pooledTotal = track.PooledObjectCount + track.ActiveObjectCount;
        Assert.That(pooledTotal, Is.GreaterThan(30));

        game.StartFromWelcome();
        yield return new WaitForSeconds(2.6f);
        Assert.That(game.IsGameplayActive, Is.True);
        FieldInfo pausePanelField = typeof(CatRunnerGameController).GetField(
            "pausePanel",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo tutorialPanelField = typeof(CatRunnerGameController).GetField(
            "tutorialPanel",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(pausePanelField, Is.Not.Null);
        Assert.That(tutorialPanelField, Is.Not.Null);
        var pausePanel = pausePanelField.GetValue(game) as GameObject;
        var tutorialPanel = tutorialPanelField.GetValue(game) as GameObject;
        Assert.That(pausePanel, Is.Not.Null);
        Assert.That(tutorialPanel, Is.Not.Null);
        Assert.That(tutorialPanel.activeInHierarchy, Is.True);

        game.PauseRun();
        Assert.That(game.IsPaused, Is.True);
        Assert.That(Time.timeScale, Is.Zero);
        Assert.That(pausePanel.activeInHierarchy, Is.True);
        Assert.That(tutorialPanel.activeInHierarchy, Is.False);
        float pausedElapsed = game.ElapsedSeconds;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(game.ElapsedSeconds, Is.EqualTo(pausedElapsed).Within(.001f));

        game.ResumeRun();
        Assert.That(game.IsPaused, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(pausePanel.activeInHierarchy, Is.False);
        Assert.That(tutorialPanel.activeInHierarchy, Is.True);

        Time.timeScale = 20f;
        try
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (game.ElapsedSeconds < 8f && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(game.ElapsedSeconds, Is.GreaterThanOrEqualTo(8f));
            Assert.That(
                track.PooledObjectCount + track.ActiveObjectCount,
                Is.EqualTo(pooledTotal),
                "The warmed Runner pool must recycle objects without runtime churn.");
        }
        finally
        {
            Time.timeScale = 1f;
        }
    }

    [UnityTest]
    public IEnumerator HomeRunnerHome_AdditiveLifecycleKeepsSingletonPresentation()
    {
        AsyncOperation homeLoad =
            SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        Assert.That(homeLoad, Is.Not.Null);
        while (!homeLoad.isDone)
            yield return null;

        LevelLoader loader = null;
        for (int frame = 0; frame < 360; frame++)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
            if (loader != null && loader.IsReady)
                break;
            yield return null;
        }
        Assert.That(loader, Is.Not.Null);
        Assert.That(loader.IsReady, Is.True);

        string returnScene = SceneManager.GetActiveScene().name;
        bool fog = RenderSettings.fog;
        FogMode fogMode = RenderSettings.fogMode;
        Color ambientSky = RenderSettings.ambientSkyColor;
        PrepareRunnerServices(0, tutorialCompleted: true);

        CatRunnerLauncher launcher =
            Object.FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include);
        Assert.That(launcher, Is.Not.Null);
        launcher.Launch();

        CatRunnerGameController game = null;
        for (int frame = 0; frame < 360; frame++)
        {
            game = Object.FindAnyObjectByType<CatRunnerGameController>(
                FindObjectsInactive.Include);
            if (game != null && SceneManager.GetSceneByName("CatRunner").isLoaded)
                break;
            yield return null;
        }
        Assert.That(game, Is.Not.Null);
        Assert.That(SceneManager.GetSceneByName("CatRunner").isLoaded, Is.True);
        Assert.That(game.IsRunning, Is.False);

        FieldInfo startField = typeof(CatRunnerGameController).GetField(
            "welcomeStartButton",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo rewardedField = typeof(CatRunnerGameController).GetField(
            "welcomeRewardedEnergyButton",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(startField, Is.Not.Null);
        Assert.That(rewardedField, Is.Not.Null);
        var start = startField.GetValue(game) as UnityEngine.UI.Button;
        var rewarded = rewardedField.GetValue(game) as UnityEngine.UI.Button;
        yield return null;
        Assert.That(start, Is.Not.Null);
        Assert.That(start.interactable, Is.False);
        Assert.That(rewarded, Is.Not.Null);
        Assert.That(rewarded.gameObject.activeInHierarchy, Is.True,
            "Zero-energy players must receive the verified rewarded-energy route.");

        AssertPresentationSingletons();
        game.ExitFromWelcome();

        for (int frame = 0; frame < 360; frame++)
        {
            if (!SceneManager.GetSceneByName("CatRunner").isLoaded)
                break;
            yield return null;
        }
        Assert.That(SceneManager.GetSceneByName("CatRunner").isLoaded, Is.False);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(returnScene));
        AssertPresentationSingletons();
        Assert.That(RenderSettings.fog, Is.EqualTo(fog));
        Assert.That(RenderSettings.fogMode, Is.EqualTo(fogMode));
        Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(ambientSky));

        PrepareRunnerServices(5, tutorialCompleted: true);
    }

    private static void AssertPresentationSingletons()
    {
        int cameras = 0;
        foreach (Camera camera in
                 Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            if (camera.enabled)
                cameras++;
        int listeners = 0;
        foreach (AudioListener listener in
                 Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
            if (listener.enabled)
                listeners++;
        int eventSystems = 0;
        foreach (UnityEngine.EventSystems.EventSystem eventSystem in
                 Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
                     FindObjectsInactive.Exclude))
            if (eventSystem.enabled)
                eventSystems++;

        Assert.That(cameras, Is.EqualTo(1));
        Assert.That(listeners, Is.EqualTo(1));
        Assert.That(eventSystems, Is.EqualTo(1));
    }

    private static void PrepareRunnerServices(int energy, bool tutorialCompleted)
    {
        DateTime now = DateTime.UtcNow;
        RunnerEnergyService.ApplySavedState(
            new RunnerEnergySaveState
            {
                energy = energy,
                regenerationAnchorUtc = now.ToString("O", CultureInfo.InvariantCulture),
                unlimitedUntilUtc = string.Empty,
                rewardedAdsClaimedToday = 0,
                rewardedAdsDayUtc = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            },
            now);
        CatRunnerProgressSaveState progress =
            CatRunnerProgressSaveState.CreateDefault(now);
        progress.tutorialCompleted = tutorialCompleted;
        progress.bestScore = 0;
        progress.soundEnabled = false;
        progress.hapticsEnabled = false;
        CatRunnerProgressService.ApplySavedState(progress, now);
    }
}
