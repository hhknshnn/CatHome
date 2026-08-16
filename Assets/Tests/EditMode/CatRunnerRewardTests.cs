using CatHome.Economy;
using NUnit.Framework;

public sealed class CatRunnerRewardTests
{
    [SetUp]
    public void SetUp()
    {
        EconomyService.ApplyLegacyBalances(0, 0);
    }

    [TearDown]
    public void TearDown()
    {
        EconomyService.ApplyLegacyBalances(0, 0);
    }

    [Test]
    public void Result_SanitizesValuesAndCalculatesCareBonus()
    {
        CatRunnerResult result = CatRunnerResult.Create(
            "run-a",
            60f,
            412.5f,
            12,
            -4,
            20);

        Assert.That(result.RunId, Is.EqualTo("run-a"));
        Assert.That(result.CoinsCollected, Is.EqualTo(12));
        Assert.That(result.Collisions, Is.Zero);
        Assert.That(result.BonusCoins, Is.EqualTo(2));
        Assert.That(result.TotalCoins, Is.EqualTo(14));
    }

    [Test]
    public void Reward_UsesRunIdAndCanNeverPayTwice()
    {
        CatRunnerResult result = CatRunnerResult.Create(
            "same-run",
            60f,
            400f,
            10,
            1,
            20);

        EconomyTransactionResult first = CatRunnerRewardService.Grant(result);
        EconomyTransactionResult repeated = CatRunnerRewardService.Grant(result);

        Assert.That(first.Succeeded, Is.True);
        Assert.That(repeated.Status, Is.EqualTo(EconomyTransactionStatus.Duplicate));
        Assert.That(EconomyService.Coins, Is.EqualTo(12));
        Assert.That(EconomyService.Diamonds, Is.Zero);
    }
}
