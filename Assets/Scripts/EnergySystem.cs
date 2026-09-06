using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergySystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image energyFill;
    [SerializeField] private Image energyFrame;
    [SerializeField] private TMP_Text percentageText;

    [Header("Cat")]
    [SerializeField] private CatMovement catMovement;
    [SerializeField] private SleepInteraction sleepInteraction;
    [SerializeField] private float exhaustedSpeedMultiplier = 0.65f;

    [Header("Energy")]
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float decreasePerSecond = GameBalanceConfig.FourHourNeedDepletionPerSecond;
    [SerializeField] private float recoveryPerSecond = 10f;
    [SerializeField, Range(0f, 100f)] private float criticalThreshold = 0.01f;

    private readonly Color fullColor = new Color32(166, 103, 232, 255);
    private readonly Color tiredColor = new Color32(246, 200, 76, 255);
    private readonly Color criticalColor = new Color32(232, 76, 76, 255);
    private readonly Color darkCriticalColor = new Color32(120, 20, 20, 255);
    private readonly Color textColor = new Color32(255, 244, 214, 255);

    public float CurrentEnergy => currentEnergy;
    public bool IsExhausted => currentEnergy <= criticalThreshold;
    public CatMovement CatMovement => catMovement;

    private void Awake()
    {
        ResolveSceneReferences();

        if (!GameBalanceConfig.TryGetActiveBalance(out GameBalanceConfig.BalanceProfile balance))
            return;

        currentEnergy = balance.EnergyStartValue;
        decreasePerSecond = balance.AwakeEnergyDecreasePerSecond;
        recoveryPerSecond = balance.SleepingEnergyRecoveryPerSecond;
        criticalThreshold = balance.EnergyCriticalThreshold;
        exhaustedSpeedMultiplier = balance.EnergyCriticalSpeedMultiplier;
    }

    public void ResolveSceneReferences()
    {
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);

        if (sleepInteraction == null)
            sleepInteraction = FindAnyObjectByType<SleepInteraction>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);
        UpdateUI();
    }

    private void Update()
    {
        bool isSleeping = sleepInteraction != null && sleepInteraction.IsSleeping;
        var rest = CatActivity.Active as CatEnrichmentActivity;
        var furnitureRest = CatActivity.Active as LivingFurnitureActivity;
        bool isResting = (rest != null && rest.IsResting) || (furnitureRest != null && furnitureRest.IsResting);
        if (isResting)
        {
            currentEnergy += CatEnrichmentActivity.RestEnergyPerSecond * Time.deltaTime;
        }
        else if (isSleeping)
        {
            currentEnergy += recoveryPerSecond * Time.deltaTime;
        }
        else
        {
            currentEnergy -= decreasePerSecond * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0f, 100f);
        UpdateUI(isSleeping);
    }

    private void UpdateUI()
    {
        bool isSleeping = sleepInteraction != null && sleepInteraction.IsSleeping;
        UpdateUI(isSleeping);
    }

    public void ApplySavedValue(float value)
    {
        currentEnergy = Mathf.Clamp(value, 0f, 100f);
        UpdateUI();
    }

    public void ApplyOfflineChange(float delta)
    {
        ApplySavedValue(currentEnergy + delta);
    }

    /// <summary>
    /// Adds energy back after a resting activity. Sleeping in bed uses the
    /// per-second recovery in Update; short naps land here instead.
    /// </summary>
    public void RestoreEnergy(float amount)
    {
        if (amount <= 0f)
            return;

        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0f, 100f);
        UpdateUI();
    }

    public bool CanSpendEnergy(float amount)
    {
        return amount <= 0f || currentEnergy >= amount;
    }

    public bool TrySpendEnergy(float amount)
    {
        amount = Mathf.Max(0f, amount);
        if (!CanSpendEnergy(amount))
            return false;

        currentEnergy = Mathf.Clamp(currentEnergy - amount, 0f, 100f);
        UpdateUI();
        return true;
    }

    private void UpdateUI(bool isSleeping)
    {
        bool isExhausted = IsExhausted;
        float normalizedEnergy = currentEnergy / 100f;

        if (percentageText != null)
        {
            percentageText.text = Mathf.CeilToInt(currentEnergy) + "%";
            percentageText.color = PremiumUiStyle.Ink;
        }

        if (isExhausted)
        {
            UpdateExhaustedUI();
        }
        else
        {
            if (energyFill != null)
            {
                energyFill.fillAmount = normalizedEnergy;

                if (currentEnergy >= 70f)
                    energyFill.color = fullColor;
                else if (currentEnergy >= 30f)
                    energyFill.color = tiredColor;
                else
                    energyFill.color = criticalColor;
            }

            if (energyFrame != null)
                energyFrame.color = Color.white;
        }

        if (catMovement != null)
        {
            catMovement.SetEnergySpeedMultiplier(
                isExhausted ? exhaustedSpeedMultiplier : 1f
            );
        }
    }

    private void UpdateExhaustedUI()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
        Color flashingRed = Color.Lerp(darkCriticalColor, criticalColor, pulse);

        if (energyFill != null)
        {
            energyFill.fillAmount = 0.08f;
            energyFill.color = flashingRed;
        }

        if (energyFrame != null)
            energyFrame.color = flashingRed;

        if (percentageText != null)
            percentageText.color = Color.Lerp(PremiumUiStyle.Ink, new Color32(152, 53, 43, 255), pulse);
    }

    private void OnDisable()
    {
        if (catMovement != null)
            catMovement.SetEnergySpeedMultiplier(1f);
    }
}
