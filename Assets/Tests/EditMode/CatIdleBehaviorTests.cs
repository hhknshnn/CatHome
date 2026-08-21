using NUnit.Framework;
using UnityEngine;

public sealed class CatIdleBehaviorTests
{
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
            Is.EqualTo("I'M HUNGRY..."));
        Assert.That(
            CatIdlePersonality.AttentionLine(CatIdleMood.Needy, "Loki", 40f, 10f, 40f),
            Is.EqualTo("I NEED A DRINK..."));
        Assert.That(
            CatIdlePersonality.AttentionLine(CatIdleMood.Sleepy, "Loki", 80f, 80f, 20f),
            Is.EqualTo("SLEEPY..."));
        Assert.That(
            CatIdlePersonality.AttentionLine(CatIdleMood.Happy, "loki", 90f, 90f, 90f),
            Is.EqualTo("LOKI WANTS A PET!"));
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
