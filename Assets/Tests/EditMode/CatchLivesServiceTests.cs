using System;
using System.Globalization;
using NUnit.Framework;

public sealed class CatchLivesServiceTests
{
    private static readonly DateTime Start =
        new DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void SetUp()
    {
        CatchLivesService.ApplySavedState(State(5, Start), Start);
        RunnerEnergyService.ApplySavedState(RunnerState(5, Start), Start);
    }

    [Test]
    public void HuntSpend_DoesNotTouchRunnerEnergy()
    {
        Assert.That(CatchLivesService.TrySpendHuntLife(Start), Is.True);
        Assert.That(CatchLivesService.CaptureState(Start).lives, Is.EqualTo(4));
        Assert.That(RunnerEnergyService.CaptureState(Start).energy, Is.EqualTo(5));
    }

    [Test]
    public void RunnerSpend_DoesNotTouchCatchLives()
    {
        Assert.That(RunnerEnergyService.TrySpendRunEnergy(Start), Is.True);
        Assert.That(RunnerEnergyService.CaptureState(Start).energy, Is.EqualTo(4));
        Assert.That(CatchLivesService.CaptureState(Start).lives, Is.EqualTo(5));
    }

    [Test]
    public void HuntSpend_RegeneratesOneLifeAfterTenMinutes()
    {
        Assert.That(CatchLivesService.TrySpendHuntLife(Start), Is.True);
        CatchLivesService.Refresh(Start.AddMinutes(9).AddSeconds(59));
        Assert.That(CatchLivesService.CaptureState(
            Start.AddMinutes(9).AddSeconds(59)).lives, Is.EqualTo(4));

        CatchLivesService.Refresh(Start.AddMinutes(10));
        Assert.That(CatchLivesService.CaptureState(Start.AddMinutes(10)).lives,
            Is.EqualTo(5));
    }

    [Test]
    public void Tutorial_PersistsAfterCompleteAndReload()
    {
        Assert.That(CatchLivesService.TutorialCompleted, Is.False);
        Assert.That(CatchLivesService.CompleteTutorial(), Is.True);
        CatchLivesSaveState captured = CatchLivesService.CaptureState(Start);
        Assert.That(captured.tutorialCompleted, Is.True);

        CatchLivesService.ApplySavedState(State(5, Start), Start);
        Assert.That(CatchLivesService.TutorialCompleted, Is.False);

        CatchLivesService.ApplySavedState(captured, Start);
        Assert.That(CatchLivesService.TutorialCompleted, Is.True);
        Assert.That(CatchLivesService.CompleteTutorial(), Is.False);
    }

    private static CatchLivesSaveState State(int lives, DateTime anchor)
    {
        return new CatchLivesSaveState
        {
            lives = lives,
            regenerationAnchorUtc = anchor.ToString("O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = string.Empty,
            rewardedAdsClaimedToday = 0,
            rewardedAdsDayUtc = anchor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }

    private static RunnerEnergySaveState RunnerState(int energy, DateTime anchor)
    {
        return new RunnerEnergySaveState
        {
            energy = energy,
            regenerationAnchorUtc = anchor.ToString("O", CultureInfo.InvariantCulture),
            unlimitedUntilUtc = string.Empty,
            rewardedAdsClaimedToday = 0,
            rewardedAdsDayUtc = anchor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }
}
