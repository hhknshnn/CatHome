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

    public static string BuildTransactionId(string runId) =>
        "cat-runner:" + (runId ?? string.Empty);
}
