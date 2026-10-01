#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CatIdleMiniGameGateTests
{
    private Scene originalActive, fixture;
    private FieldInfo launch;
    private object previousLaunch;
    private bool captured;
    private Func<bool> miniGameGate;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True,
            "Mini-game scene lifecycle checks require an isolated QA save.");
        Assert.That(SceneManager.GetSceneByName(CatRunnerLauncher.RunnerSceneName).isLoaded, Is.False);
        Assert.That(SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName).isLoaded, Is.False);
        launch = typeof(CatRunnerSessionContext).GetField("launchingScene", BindingFlags.Static | BindingFlags.NonPublic);
        previousLaunch = launch.GetValue(null);
        originalActive = SceneManager.GetActiveScene();
        captured = true;
        launch.SetValue(null, null);
        var method = typeof(CatIdleBehavior).GetMethod("MiniGameIsActive", BindingFlags.Static | BindingFlags.NonPublic);
        miniGameGate = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method);
    }

    [UnityTest]
    public IEnumerator RunnerScene_SuppressesIdleUntilAdditiveUnload()
    {
        yield return VerifySceneLifecycle(CatRunnerLauncher.RunnerSceneName);
    }

    [UnityTest]
    public IEnumerator CatchScene_SuppressesIdleUntilAdditiveUnload()
    {
        yield return VerifySceneLifecycle(CatCatchLauncher.CatchSceneName);
    }

    private IEnumerator VerifySceneLifecycle(string sceneName)
    {
        Assert.That(miniGameGate(), Is.False);
        launch.SetValue(null, sceneName);
        Assert.That(miniGameGate(), Is.True, "Launch reservation precedes the additive scene.");
        fixture = SceneManager.CreateScene(sceneName);
        CatRunnerSessionContext.CompleteLaunch(sceneName);
        Assert.That(fixture.rootCount, Is.Zero, "Visibility must not depend on scanning for controller components.");
        Assert.That(miniGameGate(), Is.True, "The loaded scene covers welcome, play, pause and result.");
        AsyncOperation unload = SceneManager.UnloadSceneAsync(fixture);
        Assert.That(unload, Is.Not.Null);
        while (!unload.isDone) yield return null;
        fixture = default;
        Assert.That(miniGameGate(), Is.False, "Unloading the mini-game releases home idle immediately.");
    }

    [UnityTearDown]
    public IEnumerator After()
    {
        if (!captured) yield break;
        try
        {
            if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
            if (fixture.IsValid() && fixture.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(fixture);
                if (unload != null) while (!unload.isDone) yield return null;
            }
            fixture = default;
        }
        finally
        {
            launch.SetValue(null, previousLaunch);
            captured = false;
        }
    }
}
#endif
