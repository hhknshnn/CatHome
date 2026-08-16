using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameBalanceConfig",
    menuName = "Cat Home/Game Balance Config"
)]
public sealed class GameBalanceConfig : ScriptableObject
{
    public const float FourHourNeedDepletionPerSecond = 100f / (4f * 60f * 60f);

    public enum ProfileType
    {
        Test,
        Production
    }

    [Serializable]
    public sealed class BalanceProfile
    {
        [Header("Hunger")]
        [SerializeField, Range(0f, 100f)] private float hungerStartValue = 100f;
        [SerializeField, Min(0f)] private float hungerDecreasePerSecond = FourHourNeedDepletionPerSecond;
        [SerializeField, Range(0f, 100f)] private float hungerCriticalThreshold = 0.01f;

        [Header("Thirst")]
        [SerializeField, Range(0f, 100f)] private float thirstStartValue = 100f;
        [SerializeField, Min(0f)] private float thirstDecreasePerSecond = FourHourNeedDepletionPerSecond;
        [SerializeField, Range(0f, 100f)] private float thirstCriticalThreshold = 0.01f;

        [Header("Energy")]
        [SerializeField, Range(0f, 100f)] private float energyStartValue = 100f;
        [SerializeField, Min(0f)] private float awakeEnergyDecreasePerSecond = FourHourNeedDepletionPerSecond;
        [SerializeField, Min(0f)] private float sleepingEnergyRecoveryPerSecond = 10f;
        [SerializeField, Range(0f, 100f)] private float energyCriticalThreshold = 0.01f;

        [Header("Interactions")]
        [SerializeField, Min(0f)] private float eatingDuration = 10f;
        [SerializeField, Min(0f)] private float drinkingDuration = 10f;
        [SerializeField, Range(0f, 100f)] private float satisfiedActionThreshold = 90f;

        [Header("Critical Need Movement")]
        [SerializeField, Range(0.1f, 1f)] private float hungerThirstCriticalSpeedMultiplier = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float energyCriticalSpeedMultiplier = 0.65f;

        [Header("Offline Progress")]
        [SerializeField, Min(0f)] private float maximumOfflineProgressHours = 24f;

        public float HungerStartValue => hungerStartValue;
        public float HungerDecreasePerSecond => hungerDecreasePerSecond;
        public float HungerCriticalThreshold => hungerCriticalThreshold;
        public float ThirstStartValue => thirstStartValue;
        public float ThirstDecreasePerSecond => thirstDecreasePerSecond;
        public float ThirstCriticalThreshold => thirstCriticalThreshold;
        public float EnergyStartValue => energyStartValue;
        public float AwakeEnergyDecreasePerSecond => awakeEnergyDecreasePerSecond;
        public float SleepingEnergyRecoveryPerSecond => sleepingEnergyRecoveryPerSecond;
        public float EnergyCriticalThreshold => energyCriticalThreshold;
        public float EatingDuration => eatingDuration;
        public float DrinkingDuration => drinkingDuration;
        public float SatisfiedActionThreshold => satisfiedActionThreshold;
        public float HungerThirstCriticalSpeedMultiplier => hungerThirstCriticalSpeedMultiplier;
        public float EnergyCriticalSpeedMultiplier => energyCriticalSpeedMultiplier;
        public float MaximumOfflineProgressHours => maximumOfflineProgressHours;
    }

    private const string ResourcesPath = "GameBalanceConfig";
    private static bool missingConfigWarningLogged;

    [SerializeField] private ProfileType activeProfile = ProfileType.Test;
    [SerializeField] private BalanceProfile test = new BalanceProfile();
    [SerializeField] private BalanceProfile production = new BalanceProfile();

    public ProfileType ActiveProfile => activeProfile;
    public BalanceProfile Test => test;
    public BalanceProfile Production => production;
    public BalanceProfile ActiveBalance =>
        activeProfile == ProfileType.Production ? production : test;

    public static bool TryGetActiveBalance(out BalanceProfile balance)
    {
        GameBalanceConfig config = Resources.Load<GameBalanceConfig>(ResourcesPath);
        if (config != null)
        {
            config.EnsureProfiles();
            balance = config.ActiveBalance;
            return true;
        }

        balance = null;
        if (!missingConfigWarningLogged)
        {
            Debug.LogWarning(
                "GameBalanceConfig: Resources/GameBalanceConfig could not be loaded. " +
                "Existing serialized component values will be used as fallback."
            );
            missingConfigWarningLogged = true;
        }

        return false;
    }

    public static float GetSatisfiedActionThreshold()
    {
        return TryGetActiveBalance(out BalanceProfile balance)
            ? balance.SatisfiedActionThreshold
            : 90f;
    }

    public void EnsureProfiles()
    {
        if (test == null)
            test = new BalanceProfile();

        if (production == null)
            production = new BalanceProfile();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        missingConfigWarningLogged = false;
    }
}
