using CatHome.Economy;
using NUnit.Framework;

public sealed class HomeProgressionServiceTests
{
    [SetUp]
    public void SetUp()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    [TearDown]
    public void TearDown()
    {
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    [Test]
    public void FreshHome_IsLevelOneWithZeroXp()
    {
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(0L));
        Assert.That(HomeProgressionService.HomeLevel, Is.EqualTo(1));
        Assert.That(HomeProgressionService.XpIntoCurrentLevel, Is.EqualTo(0L));
        Assert.That(HomeProgressionService.XpForCurrentLevel, Is.EqualTo(1000L));
    }

    [Test]
    public void LevelCurve_IsTriangularAndMonotonic()
    {
        // Cumulative floors: L1=0, L2=1000, L3=3000, L4=6000, L5=10000.
        Assert.That(HomeProgressionService.CumulativeXpForLevel(1), Is.EqualTo(0L));
        Assert.That(HomeProgressionService.CumulativeXpForLevel(2), Is.EqualTo(1000L));
        Assert.That(HomeProgressionService.CumulativeXpForLevel(3), Is.EqualTo(3000L));
        Assert.That(HomeProgressionService.CumulativeXpForLevel(4), Is.EqualTo(6000L));
        Assert.That(HomeProgressionService.CumulativeXpForLevel(5), Is.EqualTo(10000L));

        Assert.That(HomeProgressionService.LevelForXp(0L), Is.EqualTo(1));
        Assert.That(HomeProgressionService.LevelForXp(999L), Is.EqualTo(1));
        Assert.That(HomeProgressionService.LevelForXp(1000L), Is.EqualTo(2));
        Assert.That(HomeProgressionService.LevelForXp(2999L), Is.EqualTo(2));
        Assert.That(HomeProgressionService.LevelForXp(3000L), Is.EqualTo(3));
        Assert.That(HomeProgressionService.LevelForXp(9999L), Is.EqualTo(4));
        Assert.That(HomeProgressionService.LevelForXp(10000L), Is.EqualTo(5));
    }

    [Test]
    public void LevelForXp_ClampsAtMaxLevelWithoutOverflow()
    {
        Assert.That(HomeProgressionService.LevelForXp(long.MaxValue),
            Is.EqualTo(HomeProgressionService.MaxLevel));
    }

    [Test]
    public void GrantHomeXp_AccumulatesAndRaisesLevel()
    {
        int changes = 0;
        void Handler() => changes++;
        HomeProgressionService.Changed += Handler;
        try
        {
            HomeProgressionService.GrantHomeXp(600L, "test.a");
            Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(600L));
            Assert.That(HomeProgressionService.HomeLevel, Is.EqualTo(1));

            HomeProgressionService.GrantHomeXp(600L, "test.b");
            Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(1200L));
            Assert.That(HomeProgressionService.HomeLevel, Is.EqualTo(2));
            Assert.That(HomeProgressionService.XpIntoCurrentLevel, Is.EqualTo(200L));
            Assert.That(HomeProgressionService.XpForCurrentLevel, Is.EqualTo(2000L));
        }
        finally
        {
            HomeProgressionService.Changed -= Handler;
        }

        Assert.That(changes, Is.EqualTo(2));
    }

    [Test]
    public void GrantHomeXp_IgnoresNonPositiveAmounts()
    {
        HomeProgressionService.GrantHomeXp(500L);
        HomeProgressionService.GrantHomeXp(0L);
        HomeProgressionService.GrantHomeXp(-9999L);

        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(500L));
    }

    [Test]
    public void SaveState_RoundTripsHomeXp()
    {
        HomeProgressionService.GrantHomeXp(3200L);
        Assert.That(HomeProgressionService.HomeLevel, Is.EqualTo(3));

        HomeProgressionSaveState saved = HomeProgressionService.CaptureState();
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(0L));
        Assert.That(HomeProgressionService.HomeLevel, Is.EqualTo(1));

        HomeProgressionService.ApplySavedState(saved);
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(3200L));
        Assert.That(HomeProgressionService.HomeLevel, Is.EqualTo(3));
    }

    [Test]
    public void ApplySavedState_SanitizesNegativeAndNull()
    {
        HomeProgressionService.ApplySavedState(new HomeProgressionSaveState { homeXp = -42L });
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(0L));

        HomeProgressionService.GrantHomeXp(700L);
        HomeProgressionService.ApplySavedState(null);
        Assert.That(HomeProgressionService.HomeXp, Is.EqualTo(0L));
    }

    [Test]
    public void HomeStorePurchase_GrantsHomeXpEqualToCoinPrice()
    {
        EconomyService.AddCurrency(
            CurrencyType.Coin,
            HomeStoreService.BallBasketPrice,
            EconomySource.Debug);

        HomeStorePurchaseResult result =
            HomeStoreService.TryPurchase(HomeStoreService.BallBasketId);

        Assert.That(result.Status, Is.EqualTo(HomeStorePurchaseStatus.Purchased));
        Assert.That(HomeProgressionService.HomeXp,
            Is.EqualTo(HomeStoreService.BallBasketPrice));
    }

    [Test]
    public void FreeTestingAcquisition_GrantsHomeXpForItemAndPrerequisites()
    {
        // Television (2500) pulls in the TV unit (1800) as a prerequisite; both
        // acquisitions award their coin value as Home XP.
        HomeStorePurchaseResult television = HomeStoreService.TryAcquireForTesting(
            HomeStoreService.ModernTelevisionId);

        Assert.That(television.Status, Is.EqualTo(HomeStorePurchaseStatus.Purchased));
        Assert.That(HomeStoreService.TryGetProduct(
            HomeStoreService.ModernTelevisionId, out HomeStoreProduct tv), Is.True);
        Assert.That(HomeStoreService.TryGetProduct(
            HomeStoreService.TvUnitId, out HomeStoreProduct unit), Is.True);
        Assert.That(HomeProgressionService.HomeXp,
            Is.EqualTo(tv.CoinPrice + unit.CoinPrice));
    }
}
