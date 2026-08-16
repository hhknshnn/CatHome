using UnityEngine;

/// <summary>
/// Pure Cat Catch economy rules. Kept free of scene state so the hunt payout can
/// be verified in EditMode tests instead of only on device.
/// </summary>
public static class CatchScoring
{
    public const int BaseCatchScore = 120;
    public const int ComboStepScore = 20;
    public const int MaximumComboSteps = 5;
    public const float ComboWindowSeconds = 3f;
    public const int CoinsPerCatch = 8;
    public const int MaximumRewardedCatches = 45;

    public static int ComboFor(int previousCombo, float secondsSinceLastCatch)
    {
        if (previousCombo <= 0 || secondsSinceLastCatch > ComboWindowSeconds)
            return 1;
        return previousCombo + 1;
    }

    public static int ScoreForCatch(int combo)
    {
        int steps = Mathf.Clamp(combo - 1, 0, MaximumComboSteps);
        return BaseCatchScore + steps * ComboStepScore;
    }

    public static int MaximumScorePerCatch => BaseCatchScore + MaximumComboSteps * ComboStepScore;

    public static long CoinsForCatches(int catches)
    {
        if (catches <= 0)
            return 0L;
        return Mathf.Min(catches, MaximumRewardedCatches) * (long)CoinsPerCatch;
    }

    public static bool IsCoinCapReached(int catches) => catches >= MaximumRewardedCatches;
}
