using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThirstSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image thirstFill;
    [SerializeField] private Image thirstFrame;
    [SerializeField] private TMP_Text percentageText;
    private int displayedPercentage = -1;
    private string displayedPercentageText;

    [Header("Cat")]
    [SerializeField] private CatMovement catMovement;
    [SerializeField] private float dehydratedSpeedMultiplier = 0.45f;

    [Header("Thirst")]
    [SerializeField] private float currentThirst = 100f;
    [SerializeField] private float decreasePerSecond = GameBalanceConfig.FourHourNeedDepletionPerSecond;
    [SerializeField, Range(0f, 100f)] private float criticalThreshold = 0.01f;

    private Coroutine drinkingCoroutine;
    private bool isDrinking;
    private UnityEngine.Object drinkingOwner;
    private Action drinkingCompleted;

    private readonly Color fullColor = new Color32(73, 169, 232, 255);
    private readonly Color thirstyColor = new Color32(246, 200, 76, 255);
    private readonly Color criticalColor = new Color32(232, 76, 76, 255);
    private readonly Color darkCriticalColor = new Color32(120, 20, 20, 255);
    private readonly Color textColor = new Color32(255, 244, 214, 255);

    public CatMovement CatMovement => catMovement;
    public bool IsDrinking => isDrinking;
    public bool IsDrinkingFor(UnityEngine.Object owner) => isDrinking && ReferenceEquals(drinkingOwner, owner);
    public bool CanDrink => !isDrinking && currentThirst < CatCareEligibility.SatisfiedThreshold;
    public float CurrentThirst => currentThirst;

    /// <summary>Raised when a drinking interaction finishes. Not raised on load.</summary>
    public event Action Drank;

    private void Awake()
    {
        ResolveSceneReferences();

        if (!GameBalanceConfig.TryGetActiveBalance(out GameBalanceConfig.BalanceProfile balance))
            return;

        currentThirst = balance.ThirstStartValue;
        decreasePerSecond = balance.ThirstDecreasePerSecond;
        criticalThreshold = balance.ThirstCriticalThreshold;
        dehydratedSpeedMultiplier = balance.HungerThirstCriticalSpeedMultiplier;
    }

    public void ResolveSceneReferences()
    {
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (!isDrinking && currentThirst > 0f)
        {
            currentThirst -= decreasePerSecond * Time.deltaTime;
            currentThirst = Mathf.Clamp(currentThirst, 0f, 100f);
        }

        UpdateUI();
    }

    public bool BeginDrinking(float duration) => BeginDrinking(duration, null);

    public bool BeginDrinking(float duration, UnityEngine.Object owner, Action onCompleted = null)
    {
        if (!isActiveAndEnabled || isDrinking) return false;
        ResolveSceneReferences();
        if (!CatCareEligibility.TryAccept(catMovement, CatCareNeed.Water)) return false;
        if (!CanDrink) return false;

        if (drinkingCoroutine != null)
        {
            StopCoroutine(drinkingCoroutine);
            drinkingCoroutine = null;
        }

        isDrinking = true;
        drinkingOwner = owner;
        drinkingCompleted = onCompleted;
        if (duration <= 0f)
        {
            CompleteDrinking();
            return true;
        }

        drinkingCoroutine = StartCoroutine(DrinkingRoutine(duration));
        return true;
    }

    private IEnumerator DrinkingRoutine(float duration)
    {
        float startingThirst = currentThirst;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            currentThirst = Mathf.Lerp(
                startingThirst,
                100f,
                Mathf.Clamp01(elapsedTime / duration)
            );

            UpdateUI();
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        CompleteDrinking();
    }

    private void CompleteDrinking()
    {
        Action completed = drinkingCompleted;
        drinkingCompleted = null;
        currentThirst = 100f;
        isDrinking = false;
        drinkingOwner = null;
        drinkingCoroutine = null;

        UpdateUI();

        try { completed?.Invoke(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
        try
        {
            Drank?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private void UpdateUI()
    {
        bool isDehydrated = currentThirst <= criticalThreshold;
        float normalizedThirst = currentThirst / 100f;

        if (percentageText != null)
        {
            int percentage = Mathf.CeilToInt(currentThirst);
            if (displayedPercentage != percentage || displayedPercentageText == null)
            {
                displayedPercentage = percentage;
                displayedPercentageText = percentage + "%";
            }
            if (percentageText.text != displayedPercentageText)
                percentageText.text = displayedPercentageText;
            Color legacyColor = isDehydrated
                ? Color.Lerp(PremiumUiStyle.Ink, new Color32(152, 53, 43, 255),
                    (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f)
                : (Color)PremiumUiStyle.Ink;
            percentageText.color = StorybookHudLayout.NeedPercentageColor(percentageText, percentage, legacyColor);
        }

        if (isDehydrated)
        {
            UpdateDehydratedUI();
        }
        else
        {
            if (thirstFill != null)
            {
                thirstFill.fillAmount = normalizedThirst;

                if (currentThirst >= 70f)
                    thirstFill.color = fullColor;
                else if (currentThirst >= 30f)
                    thirstFill.color = thirstyColor;
                else
                    thirstFill.color = criticalColor;
            }

            if (thirstFrame != null)
                thirstFrame.color = Color.white;
        }

        if (catMovement != null)
        {
            catMovement.SetThirstSpeedMultiplier(
                isDehydrated ? dehydratedSpeedMultiplier : 1f
            );
        }
    }

    /// <summary>
    /// Top the bar up without running the drinking routine. Activities that
    /// already record their own quest progress use this: BeginDrinking fires
    /// Drank, which records a Drink quest, so calling it from a CatActivity
    /// would count the same sip twice.
    /// </summary>
    public void RestoreThirst(float amount)
    {
        if (amount <= 0f)
            return;

        currentThirst = Mathf.Clamp(currentThirst + amount, 0f, 100f);
        UpdateUI();
    }

    public void ApplySavedValue(float value)
    {
        CancelDrinking(drinkingOwner);
        currentThirst = Mathf.Clamp(value, 0f, 100f);
        UpdateUI();
    }

    public void ApplyOfflineChange(float delta)
    {
        ApplySavedValue(currentThirst + delta);
    }

    private void UpdateDehydratedUI()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
        Color flashingRed = Color.Lerp(darkCriticalColor, criticalColor, pulse);

        if (thirstFill != null)
        {
            thirstFill.fillAmount = 0.08f;
            thirstFill.color = flashingRed;
        }

        if (thirstFrame != null)
            thirstFrame.color = flashingRed;

    }

    public void CancelDrinking(UnityEngine.Object owner)
    {
        if (!ReferenceEquals(drinkingOwner, owner)) return;
        if (drinkingCoroutine != null)
        {
            StopCoroutine(drinkingCoroutine);
            drinkingCoroutine = null;
        }

        isDrinking = false;
        drinkingOwner = null;
        drinkingCompleted = null;
        UpdateUI();
    }

    private void OnDisable()
    {
        CancelDrinking(drinkingOwner);
        if (catMovement != null)
            catMovement.SetThirstSpeedMultiplier(1f);
    }
}
