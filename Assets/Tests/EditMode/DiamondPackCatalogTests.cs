using CatHome.Economy;
using NUnit.Framework;

public sealed class DiamondPackCatalogTests
{
    [SetUp]
    public void SetUp()
    {
        EconomyService.ApplyLegacyBalances(0L, 0L);
    }

    [TearDown]
    public void TearDown()
    {
        EconomyService.ApplyLegacyBalances(0L, 0L);
    }

    [Test]
    public void Catalog_ContainsTheSixPlannedConsumablePacks()
    {
        long[] expected = { 10L, 20L, 50L, 100L, 500L, 1000L };

        Assert.That(DiamondPackCatalog.Packs.Count, Is.EqualTo(expected.Length));
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(DiamondPackCatalog.Packs[i].DiamondAmount,
                Is.EqualTo(expected[i]));
            Assert.That(DiamondPackCatalog.Packs[i].ProductId,
                Is.EqualTo("cathome.diamonds." + expected[i]));
        }
    }

    [Test]
    public void ConfirmedPurchase_GrantsOncePerStoreTransaction()
    {
        const string transactionId = "iap:test:diamonds:20";

        EconomyTransactionResult first = DiamondPackCatalog.GrantConfirmedPurchase(
            "cathome.diamonds.20",
            transactionId);
        EconomyTransactionResult duplicate = DiamondPackCatalog.GrantConfirmedPurchase(
            "cathome.diamonds.20",
            transactionId);

        Assert.That(first.Status, Is.EqualTo(EconomyTransactionStatus.Success));
        Assert.That(duplicate.Status, Is.EqualTo(EconomyTransactionStatus.Duplicate));
        Assert.That(EconomyService.Diamonds, Is.EqualTo(20L));
    }

    [Test]
    public void UnknownPack_IsRejectedWithoutGrantingDiamonds()
    {
        EconomyTransactionResult result = DiamondPackCatalog.GrantConfirmedPurchase(
            "cathome.diamonds.unknown",
            "iap:test:unknown");

        Assert.That(result.Status, Is.EqualTo(EconomyTransactionStatus.Rejected));
        Assert.That(EconomyService.Diamonds, Is.Zero);
    }
}
