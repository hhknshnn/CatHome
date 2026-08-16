using CatHome.Economy;

/// <summary>
/// Settles the one-time reward for completing the first real care loop.
/// The fixed transaction id is stored by EconomyService, so replaying or
/// resetting the visual tutorial cannot pay the reward twice.
/// </summary>
public static class StarterCareRewardService
{
    public const int RewardCoins = 180;
    public const string TransactionId = "onboarding-care-complete-v1";

    public static EconomyTransactionResult Grant()
    {
        return EconomyService.AddCurrency(
            CurrencyType.Coin,
            RewardCoins,
            EconomySource.Onboarding,
            TransactionId,
            EconomyPersistence.Immediate);
    }
}
