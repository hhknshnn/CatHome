using NUnit.Framework;

public sealed class CatchScoringTests
{
    [Test]
    public void FirstCatch_StartsComboAtOne()
    {
        Assert.That(CatchScoring.ComboFor(0, 99f), Is.EqualTo(1));
        Assert.That(CatchScoring.ScoreForCatch(1), Is.EqualTo(CatchScoring.BaseCatchScore));
    }

    [Test]
    public void Combo_ResetsAfterWindowLapses()
    {
        int combo = CatchScoring.ComboFor(4, CatchScoring.ComboWindowSeconds + 0.01f);
        Assert.That(combo, Is.EqualTo(1));
    }

    [Test]
    public void Combo_GrowsInsideWindow()
    {
        Assert.That(CatchScoring.ComboFor(4, 1f), Is.EqualTo(5));
    }

    [Test]
    public void CatchScore_StaysBoundedRegardlessOfCombo()
    {
        Assert.That(
            CatchScoring.ScoreForCatch(400),
            Is.EqualTo(CatchScoring.MaximumScorePerCatch));
        Assert.That(
            CatchScoring.ScoreForCatch(CatchScoring.MaximumComboSteps + 1),
            Is.EqualTo(CatchScoring.ScoreForCatch(400)));
    }

    [Test]
    public void Score_IsLinearNotQuadratic()
    {
        long total = 0L;
        int combo = 0;
        for (int i = 0; i < 100; i++)
        {
            combo = CatchScoring.ComboFor(combo, 0.5f);
            total += CatchScoring.ScoreForCatch(combo);
        }

        Assert.That(total, Is.LessThanOrEqualTo(100L * CatchScoring.MaximumScorePerCatch));
    }

    [Test]
    public void Coins_ScaleWithCatchesUntilTheHuntCap()
    {
        Assert.That(CatchScoring.CoinsForCatches(0), Is.EqualTo(0L));
        Assert.That(CatchScoring.CoinsForCatches(-3), Is.EqualTo(0L));
        Assert.That(
            CatchScoring.CoinsForCatches(10),
            Is.EqualTo(10L * CatchScoring.CoinsPerCatch));
    }

    [Test]
    public void Coins_NeverExceedTheHuntCap()
    {
        long cap = CatchScoring.MaximumRewardedCatches * (long)CatchScoring.CoinsPerCatch;
        Assert.That(CatchScoring.CoinsForCatches(131), Is.EqualTo(cap));
        Assert.That(CatchScoring.CoinsForCatches(int.MaxValue), Is.EqualTo(cap));
        Assert.That(CatchScoring.IsCoinCapReached(131), Is.True);
        Assert.That(CatchScoring.IsCoinCapReached(1), Is.False);
    }
}
