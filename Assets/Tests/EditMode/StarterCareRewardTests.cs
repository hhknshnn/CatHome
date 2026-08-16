using CatHome.Economy;
using NUnit.Framework;

public sealed class StarterCareRewardTests
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
    public void CompleteCareReward_PaysConfiguredCoinsOnlyOnce()
    {
        EconomyTransactionResult first = StarterCareRewardService.Grant();
        EconomyTransactionResult repeated = StarterCareRewardService.Grant();

        Assert.That(first.Succeeded, Is.True);
        Assert.That(repeated.Status, Is.EqualTo(EconomyTransactionStatus.Duplicate));
        Assert.That(EconomyService.Coins, Is.EqualTo(StarterCareRewardService.RewardCoins));
    }
}
