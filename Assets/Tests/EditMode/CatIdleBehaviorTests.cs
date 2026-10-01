using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public sealed class CatIdleBehaviorTests
{
    [TestCase(CatRunnerLauncher.RunnerSceneName)]
    [TestCase(CatCatchLauncher.CatchSceneName)]
    public void MiniGameGate_CoversLaunchReservationUntilItsOwnerCompletes(string sceneName)
    {
        RequireHomeOnly();
        var launch = typeof(CatRunnerSessionContext).GetField("launchingScene", BindingFlags.Static | BindingFlags.NonPublic);
        object previous = launch.GetValue(null);
        try
        {
            launch.SetValue(null, null);
            Assert.That(MiniGameGate(), Is.False);
            launch.SetValue(null, sceneName);
            Assert.That(MiniGameGate(), Is.True, "Idle must remain suppressed before the additive scene appears.");
            string other = sceneName == CatRunnerLauncher.RunnerSceneName
                ? CatCatchLauncher.CatchSceneName : CatRunnerLauncher.RunnerSceneName;
            CatRunnerSessionContext.CompleteLaunch(other);
            Assert.That(MiniGameGate(), Is.True, "A different launch cannot release this reservation.");
            CatRunnerSessionContext.CompleteLaunch(sceneName);
            Assert.That(MiniGameGate(), Is.False, "Cancelled or completed launch with no scene returns to normal home idle.");
        }
        finally { launch.SetValue(null, previous); }
    }

    private static bool MiniGameGate()
    {
        var method = typeof(CatIdleBehavior).GetMethod("MiniGameIsActive", BindingFlags.Static | BindingFlags.NonPublic);
        return ((Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), method))();
    }

    private static void RequireHomeOnly()
    {
        Assert.That(SceneManager.GetSceneByName(CatRunnerLauncher.RunnerSceneName).isLoaded, Is.False,
            "This isolated fixture must start without a running mini-game.");
        Assert.That(SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName).isLoaded, Is.False,
            "This isolated fixture must start without a running mini-game.");
    }

    [Test]
    public void Mood_UsesNeedFloorAndBondThreshold()
    {
        Assert.That(CatIdlePersonality.Evaluate(20f, 90f, 90f, 250L),
            Is.EqualTo(CatIdleMood.Needy));
        Assert.That(CatIdlePersonality.Evaluate(90f, 90f, 20f, 250L),
            Is.EqualTo(CatIdleMood.Needy));
        Assert.That(CatIdlePersonality.Evaluate(50f, 50f, 30f, 250L),
            Is.EqualTo(CatIdleMood.Sleepy));
        Assert.That(CatIdlePersonality.Evaluate(90f, 90f, 90f, 80L),
            Is.EqualTo(CatIdleMood.Happy));
        Assert.That(CatIdlePersonality.Evaluate(90f, 90f, 90f, 79L),
            Is.EqualTo(CatIdleMood.Content));
        Assert.That(CatIdlePersonality.Evaluate(55f, 55f, 55f, 250L),
            Is.EqualTo(CatIdleMood.Content));
        Assert.That(CatIdlePersonality.Evaluate(40f, 90f, 90f, 250L),
            Is.EqualTo(CatIdleMood.Restless));
    }

    [Test]
    public void PickBeat_PlayOnlyWhenHappy()
    {
        Assert.That(CatIdlePersonality.PickBeat(CatIdleMood.Happy, 80),
            Is.EqualTo(CatIdleBeat.Play));
        Assert.That(CatIdlePersonality.PickBeat(CatIdleMood.Content, 99),
            Is.Not.EqualTo(CatIdleBeat.Play));
        Assert.That(CatIdlePersonality.PickBeat(CatIdleMood.Sleepy, 99),
            Is.EqualTo(CatIdleBeat.Stretch));
        Assert.That(CatIdlePersonality.PickBeat(CatIdleMood.Needy, 10),
            Is.EqualTo(CatIdleBeat.LookAround));
    }

    [Test]
    public void AttentionLine_MatchesLowestNeedAndUsesName()
    {
        Assert.That(
            CatIdlePersonality.AttentionLine(CatIdleMood.Needy, "Loki", 10f, 40f, 40f),
            Is.EqualTo(GameLanguageService.Text("idle.hungry")));
        Assert.That(
            CatIdlePersonality.AttentionLine(CatIdleMood.Needy, "Loki", 40f, 10f, 40f),
            Is.EqualTo(GameLanguageService.Text("idle.thirsty")));
        Assert.That(
            CatIdlePersonality.AttentionLine(CatIdleMood.Sleepy, "Loki", 80f, 80f, 20f),
            Is.EqualTo(GameLanguageService.Text("idle.sleepy")));
        Assert.That(
            CatIdlePersonality.AttentionLine(CatIdleMood.Happy, "loki", 90f, 90f, 90f),
            Is.EqualTo(GameLanguageService.Format("idle.pet", "loki")));
    }

    [Test]
    public void QuietDelay_IsShorterWhenNeedyThanWhenHappy()
    {
        Assert.That(
            CatIdlePersonality.QuietDelay(CatIdleMood.Needy),
            Is.LessThan(CatIdlePersonality.QuietDelay(CatIdleMood.Happy)));
        Assert.That(
            CatIdlePersonality.AttentionDelay(CatIdleMood.Needy),
            Is.LessThan(CatIdlePersonality.AttentionDelay(CatIdleMood.Happy)));
    }

    [Test]
    public void AnimatorState_UsesExistingHomeClipsAndLeavesLookOnIdle()
    {
        Assert.That(CatIdlePersonality.AnimatorState(CatIdleBeat.LookAround), Is.Empty);
        Assert.That(CatIdlePersonality.AnimatorState(CatIdleBeat.Attention), Is.Empty);
        Assert.That(CatIdlePersonality.AnimatorState(CatIdleBeat.Groom),
            Is.EqualTo("ActivityScratch"));
        Assert.That(CatIdlePersonality.AnimatorState(CatIdleBeat.ToyGlance),
            Is.EqualTo("ActivityPawSwat"));
        Assert.That(CatIdlePersonality.AnimatorState(CatIdleBeat.Stretch),
            Is.EqualTo("LieDown"));
        Assert.That(CatIdlePersonality.AnimatorState(CatIdleBeat.Play),
            Is.EqualTo("ActivityPounce"));
    }

    [Test]
    public void EnsureOn_AddsBehaviorWithoutTakingMovementLock()
    {
        var cat = new GameObject(
            "IdleTestCat",
            typeof(CharacterController),
            typeof(CatMovement));
        try
        {
            CatMovement movement = cat.GetComponent<CatMovement>();
            CatIdleBehavior.EnsureOn(movement);
            CatIdleBehavior idle = cat.GetComponent<CatIdleBehavior>();
            Assert.That(idle, Is.Not.Null);

            idle.BeginBeatForTesting(CatIdleBeat.Groom);

            Assert.That(movement.IsMovementPhysicallyLocked, Is.False);
            Assert.That(movement.IsMovementLocked, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(cat);
        }
    }
}
