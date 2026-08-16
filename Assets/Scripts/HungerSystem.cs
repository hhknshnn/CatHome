using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HungerSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image hungerFill;
    [SerializeField] private Image hungerFrame;
    [SerializeField] private TMP_Text percentageText;

    [Header("Cat")]
    [SerializeField] private CatMovement catMovement;
    [SerializeField] private float starvingSpeedMultiplier = 0.45f;

    [Header("Hunger")]
    [SerializeField] private float currentHunger = 100f;
    [SerializeField] private float decreasePerSecond = GameBalanceConfig.FourHourNeedDepletionPerSecond;
    [SerializeField, Range(0f, 100f)] private float criticalThreshold = 0.01f;

    private Coroutine eatingCoroutine;
    private bool isEating;

    private readonly Color fullColor = new Color32(242, 154, 56, 255);
    private readonly Color hungryColor = new Color32(246, 200, 76, 255);
    private readonly Color criticalColor = new Color32(232, 76, 76, 255);
    private readonly Color darkCriticalColor = new Color32(120, 20, 20, 255);
    private readonly Color textColor = new Color32(255, 244, 214, 255);

    public bool IsEating => isEating;
    public bool CanEat => !isEating && currentHunger < 99.9f;
    public float CurrentHunger => currentHunger;

    /// <summary>Raised when an eating interaction finishes. Not raised on load.</summary>
    public event Action Ate;

    private void Awake()
    {
        ResolveSceneReferences();

        if (!GameBalanceConfig.TryGetActiveBalance(out GameBalanceConfig.BalanceProfile balance))
            return;

        currentHunger = balance.HungerStartValue;
        decreasePerSecond = balance.HungerDecreasePerSecond;
        criticalThreshold = balance.HungerCriticalThreshold;
        starvingSpeedMultiplier = balance.HungerThirstCriticalSpeedMultiplier;
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
        if (!isEating && currentHunger > 0f)
        {
            currentHunger -= decreasePerSecond * Time.deltaTime;
            currentHunger = Mathf.Clamp(currentHunger, 0f, 100f);
        }

        UpdateUI();
    }

    public bool BeginEating(float duration)
    {
        if (!isActiveAndEnabled || !CanEat)
            return false;

        if (eatingCoroutine != null)
        {
            StopCoroutine(eatingCoroutine);
            eatingCoroutine = null;
        }

        isEating = true;
        if (duration <= 0f)
        {
            CompleteEating();
            return true;
        }

        eatingCoroutine = StartCoroutine(EatingRoutine(duration));
        return true;
    }

    private IEnumerator EatingRoutine(float duration)
    {
        float startingHunger = currentHunger;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            currentHunger = Mathf.Lerp(
                startingHunger,
                100f,
                Mathf.Clamp01(elapsedTime / duration)
            );

            UpdateUI();
            yield return null;
            elapsedTime += Time.deltaTime;
        }

        CompleteEating();
    }

    private void CompleteEating()
    {
        currentHunger = 100f;
        isEating = false;
        eatingCoroutine = null;

        UpdateUI();

        try
        {
            Ate?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    public void Feed(float amount)
    {
        if (!CanEat)
            return;

        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, 100f);
        UpdateUI();
    }

    public void ApplySavedValue(float value)
    {
        currentHunger = Mathf.Clamp(value, 0f, 100f);
        UpdateUI();
    }

    public void ApplyOfflineChange(float delta)
    {
        ApplySavedValue(currentHunger + delta);
    }

    private void UpdateUI()
    {
        bool isStarving = currentHunger <= criticalThreshold;
        float normalizedHunger = currentHunger / 100f;

        if (percentageText != null)
        {
            percentageText.text = Mathf.CeilToInt(currentHunger) + "%";
            percentageText.color = textColor;
        }

        if (isStarving)
        {
            UpdateStarvingUI();
        }
        else
        {
            if (hungerFill != null)
            {
                hungerFill.fillAmount = normalizedHunger;

                if (currentHunger >= 70f)
                    hungerFill.color = fullColor;
                else if (currentHunger >= 30f)
                    hungerFill.color = hungryColor;
                else
                    hungerFill.color = criticalColor;
            }

            if (hungerFrame != null)
                hungerFrame.color = Color.white;
        }

        if (catMovement != null)
        {
            catMovement.SetHungerSpeedMultiplier(
                isStarving ? starvingSpeedMultiplier : 1f
            );
        }
    }

    private void UpdateStarvingUI()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f;
        Color flashingRed = Color.Lerp(darkCriticalColor, criticalColor, pulse);

        if (hungerFill != null)
        {
            hungerFill.fillAmount = 0.08f;
            hungerFill.color = flashingRed;
        }

        if (hungerFrame != null)
            hungerFrame.color = flashingRed;

        if (percentageText != null)
            percentageText.color = Color.Lerp(textColor, Color.white, pulse);
    }

    private void OnDisable()
    {
        if (eatingCoroutine != null)
        {
            StopCoroutine(eatingCoroutine);
            eatingCoroutine = null;
        }

        isEating = false;

        if (catMovement != null)
            catMovement.SetHungerSpeedMultiplier(1f);
    }
}
