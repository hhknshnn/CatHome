using CatHome.Economy;

/// <summary>
/// Settles Cat Runner rewards through the canonical economy. The run id becomes
/// an idempotency key, so button double taps or a retry can never pay twice.
/// </summary>
public static class CatRunnerRewardService
{
    public static EconomyTransactionResult Grant(CatRunnerResult result)
    {
        return EconomyService.GrantReward(
            RewardBundle.Coins(result.TotalCoins),
            EconomySource.CatRunner,
            BuildTransactionId(result.RunId),
            EconomyPersistence.Immediate);
    }

    /// <summary>
    /// Optional rewarded-ad double of an already settled run. Uses a distinct
    /// transaction id so the normal payout path is unchanged.
    /// </summary>
    public static EconomyTransactionResult GrantDouble(CatRunnerResult result)
    {
        return EconomyService.GrantReward(
            RewardBundle.Coins(result.TotalCoins),
            EconomySource.RewardedAd,
            BuildDoubleTransactionId(result.RunId),
            EconomyPersistence.Immediate);
    }

    public static string BuildTransactionId(string runId) =>
        "cat-runner:" + (runId ?? string.Empty);

    public static string BuildDoubleTransactionId(string runId) =>
        BuildTransactionId(runId) + ":double";
}
