using System.Collections.Generic;
using CatHome.Economy;
using NUnit.Framework;

/// <summary>
/// EditMode coverage for the CP4 economy guarantees: atomic transactions, no
/// negative balance, overflow clamping, duplicate prevention, graceful handling
/// of unknown currencies and a lossless save round-trip.
///
/// These tests never touch the local save. CatHomeSaveSystem.SaveNow is a no-op
/// until CatHomeSaveSystem.Initialize has run, which only happens in Play Mode,
/// so the economy's persistence call resolves to nothing here. They only move
/// EconomyService's in-memory static state, which its SubsystemRegistration hook
/// wipes when Play Mode starts.
/// </summary>
public sealed class EconomyServiceTests
{
    private const string TestTransactionId = "test:transaction";

    [SetUp]
    public void SetUp()
    {
        if (UnityEngine.Application.isPlaying)
            Assert.Ignore("EconomyServiceTests run in EditMode only; the local save is never written.");

        ResetEconomy();
    }

    [TearDown]
    public void TearDown()
    {
        ResetEconomy();
    }

    /// <summary>
    /// Clears balances and the settled-transaction window through the same
    /// restore path the save system uses, so no test leaks state into the next
    /// one (or into the quest fixture).
    /// </summary>
    private static void ResetEconomy()
    {
        EconomyService.ApplyLegacyBalances(0, 0);
    }

    // ----- Granting -----

    [Test]
    public void AddCurrency_IncreasesTheBalance()
    {
        Assert.IsTrue(EconomyService.AddCurrency(CurrencyType.Coin, 50, EconomySource.Debug).Succeeded);
        Assert.AreEqual(50, EconomyService.Coins);
        Assert.AreEqual(0, EconomyService.Diamonds, "Only the targeted currency may move.");
    }

    [Test]
    public void AddCurrency_RejectsNegativeAmountsWithoutChangingAnything()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 25, EconomySource.Debug);

        IgnoreExpectedLogs();
        EconomyTransactionResult result =
            EconomyService.AddCurrency(CurrencyType.Coin, -10, EconomySource.Debug);

        Assert.AreEqual(EconomyTransactionStatus.InvalidAmount, result.Status);
        Assert.AreEqual(25, EconomyService.Coins);
    }

    [Test]
    public void AddCurrency_ClampsInsteadOfOverflowing()
    {
        EconomyService.SetBalance(CurrencyType.Coin, long.MaxValue - 5, EconomySource.Debug);

        IgnoreExpectedLogs();
        EconomyTransactionResult result =
            EconomyService.AddCurrency(CurrencyType.Coin, 1000, EconomySource.Debug);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(result.Clamped, "An overflowing grant must report that it was capped.");
        Assert.AreEqual(long.MaxValue, EconomyService.Coins);
        Assert.Greater(EconomyService.Coins, 0, "A clamped balance must never wrap negative.");
    }

    [Test]
    public void UnknownCurrency_IsRejectedGracefully()
    {
        IgnoreExpectedLogs();
        EconomyTransactionResult result =
            EconomyService.AddCurrency(CurrencyType.None, 100, EconomySource.Debug);

        Assert.AreEqual(EconomyTransactionStatus.UnknownCurrency, result.Status);
        Assert.AreEqual(0, EconomyService.Coins);
        Assert.AreEqual(0, EconomyService.GetBalance(CurrencyType.None));
    }

    // ----- Spending -----

    [Test]
    public void TrySpend_DeductsWhenAffordable()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 100, EconomySource.Debug);

        Assert.IsTrue(EconomyService.TrySpendCoins(30));
        Assert.AreEqual(70, EconomyService.Coins);
    }

    [Test]
    public void TrySpend_NeverDrivesABalanceNegative()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 10, EconomySource.Debug);

        EconomyTransactionResult result =
            EconomyService.TrySpend(CurrencyType.Coin, 11, EconomySource.Shop);

        Assert.AreEqual(EconomyTransactionStatus.InsufficientFunds, result.Status);
        Assert.AreEqual(10, EconomyService.Coins, "A rejected spend must change nothing.");
    }

    [Test]
    public void TrySpend_IsAtomicAcrossCurrencies()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 100, EconomySource.Debug);
        EconomyService.AddCurrency(CurrencyType.Diamond, 1, EconomySource.Debug);

        // The coins are covered, the diamonds are not: neither may be deducted.
        RewardBundle price = RewardBundle.Create(
            RewardItem.Coins(50),
            RewardItem.Diamonds(5)
        );

        EconomyTransactionResult result = EconomyService.TrySpend(price, EconomySource.Shop);

        Assert.AreEqual(EconomyTransactionStatus.InsufficientFunds, result.Status);
        Assert.AreEqual(100, EconomyService.Coins, "A partially affordable price must deduct nothing.");
        Assert.AreEqual(1, EconomyService.Diamonds);
    }

    [Test]
    public void CanAffordAndPreview_ReportWithoutMutating()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 40, EconomySource.Debug);

        Assert.IsTrue(EconomyService.CanAfford(CurrencyType.Coin, 40));
        Assert.IsFalse(EconomyService.CanAfford(CurrencyType.Coin, 41));

        PurchasePreview preview = EconomyService.PreviewPurchase(CurrencyType.Coin, 60);
        Assert.IsTrue(preview.IsValidPrice);
        Assert.IsFalse(preview.CanAfford);
        Assert.AreEqual(20, preview.Missing);
        Assert.AreEqual(40, preview.BalanceAfter, "An unaffordable preview leaves the balance as it is.");
        Assert.AreEqual(40, EconomyService.Coins, "A preview must never move a balance.");
    }

    // ----- Reward bundles -----

    [Test]
    public void GrantReward_PaysEveryCurrencyOfABundle()
    {
        RewardBundle reward = RewardBundle.Create(
            RewardItem.Coins(50),
            RewardItem.Diamonds(2)
        );

        Assert.IsTrue(EconomyService.GrantReward(reward, EconomySource.Quest).Succeeded);
        Assert.AreEqual(50, EconomyService.Coins);
        Assert.AreEqual(2, EconomyService.Diamonds);
    }

    [Test]
    public void RewardBundle_MergesRepeatedCurrenciesAndDropsZeroLines()
    {
        RewardBundle reward = RewardBundle.Create(
            RewardItem.Coins(10),
            RewardItem.Coins(15),
            RewardItem.Diamonds(0)
        );

        Assert.AreEqual(1, reward.Count, "Repeated currencies merge and zero lines are dropped.");
        Assert.AreEqual(25, reward.GetCurrencyAmount(CurrencyType.Coin));

        EconomyService.GrantReward(reward, EconomySource.Quest);
        Assert.AreEqual(25, EconomyService.Coins);
    }

    [Test]
    public void GrantMultipleRewards_SettlesAsOneTransaction()
    {
        var bundles = new List<RewardBundle>
        {
            RewardBundle.Coins(30),
            RewardBundle.Create(RewardItem.Coins(20), RewardItem.Diamonds(3)),
        };

        Assert.IsTrue(EconomyService.GrantMultipleRewards(bundles, EconomySource.Achievement).Succeeded);
        Assert.AreEqual(50, EconomyService.Coins);
        Assert.AreEqual(3, EconomyService.Diamonds);
    }

    [Test]
    public void UnhandledPayloadKind_RejectsTheWholeBundle()
    {
        // Furniture has no registered handler in this phase, so the currency part
        // must not be paid either: a reward is all or nothing.
        Assume.That(
            EconomyService.IsPayloadSupported(RewardPayloadKind.Furniture),
            Is.False,
            "This test describes the phase in which furniture rewards have no handler yet."
        );

        RewardBundle reward = RewardBundle.Create(
            RewardItem.Coins(500),
            RewardItem.Furniture("sofa_blue")
        );

        IgnoreExpectedLogs();
        EconomyTransactionResult result = EconomyService.GrantReward(reward, EconomySource.Quest);

        Assert.AreEqual(EconomyTransactionStatus.UnsupportedPayload, result.Status);
        Assert.AreEqual(0, EconomyService.Coins, "No currency may be paid when a payload is unsupported.");
    }

    // ----- Duplicate prevention -----

    [Test]
    public void RepeatedTransactionId_PaysExactlyOnce()
    {
        EconomyTransactionResult first = EconomyService.GrantReward(
            RewardBundle.Coins(100),
            EconomySource.RewardedAd,
            TestTransactionId
        );

        Assert.IsTrue(first.Succeeded);
        Assert.AreEqual(100, EconomyService.Coins);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            EconomyTransactionResult repeat = EconomyService.GrantReward(
                RewardBundle.Coins(100),
                EconomySource.RewardedAd,
                TestTransactionId
            );

            Assert.AreEqual(EconomyTransactionStatus.Duplicate, repeat.Status);
            Assert.IsTrue(repeat.IsSettled, "A duplicate is settled: the caller may proceed.");
        }

        Assert.AreEqual(100, EconomyService.Coins, "A repeated transaction id must never pay twice.");
    }

    [Test]
    public void RestorePurchaseRewards_SkipsAlreadyGrantedProducts()
    {
        var records = new List<PurchaseRecord>
        {
            new PurchaseRecord("coin_pack_small", "purchase:1", RewardBundle.Coins(500)),
            new PurchaseRecord("gem_pack_small", "purchase:2", RewardBundle.Diamonds(20)),
        };

        Assert.AreEqual(2, EconomyService.RestorePurchaseRewards(records));
        Assert.AreEqual(500, EconomyService.Coins);
        Assert.AreEqual(20, EconomyService.Diamonds);

        Assert.AreEqual(0, EconomyService.RestorePurchaseRewards(records), "A second restore grants nothing.");
        Assert.AreEqual(500, EconomyService.Coins);
        Assert.AreEqual(20, EconomyService.Diamonds);
    }

    // ----- Save round-trip and migration -----

    [Test]
    public void CaptureAndApplyState_RoundTripsBalancesAndTransactionWindow()
    {
        EconomyService.AddCurrency(CurrencyType.Coin, 1234, EconomySource.Debug);
        EconomyService.AddCurrency(CurrencyType.Diamond, 7, EconomySource.Debug, TestTransactionId);

        EconomySaveState captured = EconomyService.CaptureState();

        EconomyService.ApplyLegacyBalances(0, 0);
        Assert.AreEqual(0, EconomyService.Coins, "The reset must actually clear the wallet first.");

        EconomyService.ApplySavedState(captured);

        Assert.AreEqual(1234, EconomyService.Coins);
        Assert.AreEqual(7, EconomyService.Diamonds);

        // The settled-transaction window survived the round-trip, so a payout
        // already granted before the save can not be granted again after it.
        EconomyTransactionResult repeat = EconomyService.GrantReward(
            RewardBundle.Diamonds(7),
            EconomySource.Purchase,
            TestTransactionId
        );
        Assert.AreEqual(EconomyTransactionStatus.Duplicate, repeat.Status);
        Assert.AreEqual(7, EconomyService.Diamonds);
    }

    [Test]
    public void ApplySavedState_WithoutAnEconomySection_KeepsTheCurrentBalances()
    {
        // The v1-v4 shape: the wallet arrives through the legacy fields and the
        // economy section is absent. Nothing may be reset.
        EconomyService.ApplyLegacyBalances(880, 12);

        EconomyService.ApplySavedState(null);
        Assert.AreEqual(880, EconomyService.Coins);
        Assert.AreEqual(12, EconomyService.Diamonds);

        EconomyService.ApplySavedState(new EconomySaveState());
        Assert.AreEqual(880, EconomyService.Coins, "An empty section must not wipe a wallet.");
        Assert.AreEqual(12, EconomyService.Diamonds);
    }

    [Test]
    public void ApplySavedState_RepairsCorruptEntries()
    {
        var corrupt = new EconomySaveState
        {
            economyVersion = EconomyService.SaveVersion,
            balances = new[]
            {
                new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Coin), -500),
                new CurrencyBalanceEntry("not_a_currency", 999),
                new CurrencyBalanceEntry(null, 999),
                new CurrencyBalanceEntry(CurrencyCatalog.GetSaveKey(CurrencyType.Diamond), 4),
            },
            processedTransactionIds = new[] { null, string.Empty, "kept:1" },
        };

        IgnoreExpectedLogs();
        EconomyService.ApplySavedState(corrupt);

        Assert.AreEqual(0, EconomyService.Coins, "A negative saved balance is repaired to zero.");
        Assert.AreEqual(4, EconomyService.Diamonds, "Valid entries still load.");
    }

    [Test]
    public void ApplyLegacyBalances_RepairsNegativeValues()
    {
        EconomyService.ApplyLegacyBalances(-100, -1);

        Assert.AreEqual(0, EconomyService.Coins);
        Assert.AreEqual(0, EconomyService.Diamonds);
    }

    // ----- Events -----

    [Test]
    public void BalanceChanged_ReportsTheDeltaAndSurvivesAThrowingSubscriber()
    {
        var received = new List<CurrencyBalanceChange>();

        void Throwing(CurrencyBalanceChange change) =>
            throw new System.InvalidOperationException("A broken view must not break the economy.");

        void Recording(CurrencyBalanceChange change) => received.Add(change);

        EconomyService.BalanceChanged += Throwing;
        EconomyService.BalanceChanged += Recording;
        try
        {
            IgnoreExpectedLogs();
            Assert.IsTrue(
                EconomyService.AddCurrency(CurrencyType.Coin, 60, EconomySource.Quest).Succeeded
            );
        }
        finally
        {
            EconomyService.BalanceChanged -= Throwing;
            EconomyService.BalanceChanged -= Recording;
        }

        Assert.AreEqual(1, received.Count, "A subscriber that threw must not block the others.");
        Assert.AreEqual(CurrencyType.Coin, received[0].Currency);
        Assert.AreEqual(0, received[0].PreviousBalance);
        Assert.AreEqual(60, received[0].NewBalance);
        Assert.AreEqual(60, received[0].Delta);
        Assert.AreEqual(EconomySource.Quest, received[0].Source);
        Assert.AreEqual(60, EconomyService.Coins);
    }

    [Test]
    public void RejectedTransactions_RaiseNoEvent()
    {
        int raised = 0;
        void Count(CurrencyBalanceChange change) => raised++;

        EconomyService.BalanceChanged += Count;
        try
        {
            EconomyService.TrySpend(CurrencyType.Coin, 10, EconomySource.Shop);
            EconomyService.GrantReward(RewardBundle.Empty, EconomySource.Quest);
        }
        finally
        {
            EconomyService.BalanceChanged -= Count;
        }

        Assert.AreEqual(0, raised, "Nothing changed, so nothing may be broadcast.");
    }

    /// <summary>
    /// Several tests deliberately provoke a rejection, and a rejection logs. The
    /// test framework treats an unexpected warning or error as a failure, so the
    /// expectation is declared instead of asserted on wording. The flag is reset
    /// by the framework before every test.
    /// </summary>
    private static void IgnoreExpectedLogs()
    {
        UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
    }
}
