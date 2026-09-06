using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WhileYouWereAwayPopup : MonoBehaviour
{
    [Header("Display Rule")]
    [SerializeField, Min(0f)] private float minimumOfflineMinutes = 5f;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float openingDuration = 0.56f;
    [SerializeField, Min(0.01f)] private float closingDuration = 0.28f;
    [SerializeField, Range(0.5f, 1f)] private float openingScale = 0.76f;

    [Header("Structure")]
    [SerializeField] private CanvasGroup popupGroup;
    [SerializeField] private CanvasGroup panelGroup;
    [FormerlySerializedAs("panel")]
    [SerializeField] private RectTransform animationContainer;
    [SerializeField] private Button welcomeBackButton;
    [SerializeField] private WhileYouWereAwayPopupFx premiumFx;

    [Header("Copy")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text titleShadowText;
    [SerializeField] private TMP_Text titleHighlightText;
    [SerializeField] private TMP_Text durationText;
    [SerializeField] private TMP_Text hungerText;
    [SerializeField] private TMP_Text thirstText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text buttonShadowText;

    [Header("Need Rows")]
    [SerializeField] private CanvasGroup hungerRow;
    [SerializeField] private CanvasGroup thirstRow;
    [SerializeField] private CanvasGroup energyRow;
    [SerializeField] private bool needLabelsAreSeparate;

    [Header("Optional Input Target")]
    [SerializeField] private CatMovement catMovement;

    private static string Title => GameLanguageService.Text("return.title");
    private static string ButtonCopy => GameLanguageService.Text("return.continue");
    private const float ChangeTolerance = 0.005f;

    private Coroutine animationCoroutine;
    private long lastPresentedSummaryId;
    private bool isOpen;
    private bool referencesValid;
    private bool warningLogged;
    private bool hasPendingSummary;
    private CatHomeSaveSystem.OfflineReturnSummary pendingSummary;

    public bool IsOpen => isOpen;
    public static bool IsAnyOpen
    {
        get
        {
            WhileYouWereAwayPopup[] popups =
                Resources.FindObjectsOfTypeAll<WhileYouWereAwayPopup>();
            for (int i = 0; i < popups.Length; i++)
                if (popups[i] != null && popups[i].isOpen)
                    return true;
            return false;
        }
    }

    private void Awake()
    {
        referencesValid = ValidateReferences();
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>();

        if (welcomeBackButton != null)
        {
            welcomeBackButton.onClick.RemoveListener(Close);
            welcomeBackButton.onClick.AddListener(Close);
        }

        SetStaticCopy();
        SetHiddenImmediately();
    }

    private void OnEnable()
    {
        CatHomeSaveSystem.OfflineSummaryReady -= HandleOfflineSummary;
        CatHomeSaveSystem.OfflineSummaryReady += HandleOfflineSummary;

        if (CatHomeSaveSystem.TryGetLatestOfflineSummary(out var summary))
            HandleOfflineSummary(summary);
    }

    private void OnDisable()
    {
        CatHomeSaveSystem.OfflineSummaryReady -= HandleOfflineSummary;
        ReleaseInputBlock();
    }

    private void OnDestroy()
    {
        CatHomeSaveSystem.OfflineSummaryReady -= HandleOfflineSummary;
        if (welcomeBackButton != null)
            welcomeBackButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        if(hasPendingSummary&&!isOpen&&!OnboardingCelebrationView.IsAnyOpen)
        {
            CatHomeSaveSystem.OfflineReturnSummary summary=pendingSummary;
            hasPendingSummary=false;
            HandleOfflineSummary(summary);
        }
    }

    private void HandleOfflineSummary(CatHomeSaveSystem.OfflineReturnSummary summary)
    {
        if (!referencesValid || summary.Id <= lastPresentedSummaryId ||
            !MeetsDisplayRequirements(summary, minimumOfflineMinutes))
        {
            return;
        }

        if(OnboardingCelebrationView.IsAnyOpen)
        {
            pendingSummary=summary;
            hasPendingSummary=true;
            return;
        }

        if (!CatHomeSaveSystem.TryMarkOfflineSummaryPresented(summary.Id))
            return;

        lastPresentedSummaryId = summary.Id;
        if (isOpen)
            return;

        Populate(summary);
        Show();
    }

    private bool ValidateReferences()
    {
        bool valid = popupGroup != null && panelGroup != null && animationContainer != null &&
                     welcomeBackButton != null && durationText != null &&
                     hungerText != null && thirstText != null && energyText != null &&
                     summaryText != null;
        if (!valid && !warningLogged)
        {
            Debug.LogWarning(
                "WhileYouWereAwayPopup is missing a critical UI reference and will stay hidden.",
                this
            );
            warningLogged = true;
        }

        return valid;
    }

    private void SetStaticCopy()
    {
        SetText(titleText, Title);
        SetText(titleShadowText, Title);
        SetText(titleHighlightText, Title);
        SetText(buttonText, ButtonCopy);
        SetText(buttonShadowText, ButtonCopy);
    }

    private void Populate(CatHomeSaveSystem.OfflineReturnSummary summary)
    {
        durationText.text = GameLanguageService.Format("return.away", FormatDuration(summary.AppliedDuration));
        hungerText.text = needLabelsAreSeparate
            ? FormatNeedValues(summary.HungerBefore, summary.HungerAfter)
            : FormatNeed("HUNGER", summary.HungerBefore, summary.HungerAfter);
        thirstText.text = needLabelsAreSeparate
            ? FormatNeedValues(summary.ThirstBefore, summary.ThirstAfter)
            : FormatNeed("THIRST", summary.ThirstBefore, summary.ThirstAfter);
        energyText.text = needLabelsAreSeparate
            ? FormatNeedValues(summary.EnergyBefore, summary.EnergyAfter)
            : FormatNeed("ENERGY", summary.EnergyBefore, summary.EnergyAfter);
        summaryText.text = SelectSummaryCopy(summary);
    }

    private void Show()
    {
        if (catMovement == null)
        {
            catMovement = FindAnyObjectByType<CatMovement>(
                FindObjectsInactive.Include);
        }

        isOpen = true;
        popupGroup.alpha = 0f;
        popupGroup.blocksRaycasts = true;
        popupGroup.interactable = true;
        animationContainer.localScale = Vector3.one * openingScale;
        panelGroup.alpha = 0f;
        SetRowAlpha(0f);
        if (premiumFx != null)
            premiumFx.PrepareForOpen();
        if (catMovement != null)
            catMovement.AcquireInputBlock(this);

        StartAnimation(OpenRoutine());
    }

    public void Close()
    {
        if (!isOpen)
            return;

        popupGroup.interactable = false;
        if (premiumFx != null)
            premiumFx.BeginClose();
        StartAnimation(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        float elapsed = 0f;
        while (elapsed < openingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / openingDuration);
            popupGroup.alpha = Mathf.Clamp01(progress * 2.8f);
            panelGroup.alpha = Mathf.Clamp01((progress - 0.05f) / 0.55f);
            float overshoot = EaseOutBack(progress);
            animationContainer.localScale = Vector3.one * Mathf.LerpUnclamped(openingScale, 1f, overshoot);
            SetRowProgress(hungerRow, progress, 0.28f);
            SetRowProgress(thirstRow, progress, 0.38f);
            SetRowProgress(energyRow, progress, 0.48f);
            if (premiumFx != null)
                premiumFx.ApplyOpening(progress);
            yield return null;
        }

        popupGroup.alpha = 1f;
        panelGroup.alpha = 1f;
        animationContainer.localScale = Vector3.one;
        SetRowAlpha(1f);
        if (premiumFx != null)
            premiumFx.CompleteOpen();
        animationCoroutine = null;
    }

    private IEnumerator CloseRoutine()
    {
        float elapsed = 0f;
        float startAlpha = popupGroup.alpha;
        Vector3 startScale = animationContainer.localScale;
        while (elapsed < closingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / closingDuration);
            float smooth = progress * progress * (3f - 2f * progress);
            popupGroup.alpha = Mathf.Lerp(startAlpha, 0f, smooth);
            animationContainer.localScale = Vector3.Lerp(startScale, Vector3.one * 0.94f, smooth);
            if (premiumFx != null)
                premiumFx.ApplyClosing(progress);
            yield return null;
        }

        isOpen = false;
        SetHiddenImmediately();
        ReleaseInputBlock();
        animationCoroutine = null;
    }

    private void StartAnimation(IEnumerator routine)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(routine);
    }

    private void SetHiddenImmediately()
    {
        if (popupGroup != null)
        {
            popupGroup.alpha = 0f;
            popupGroup.interactable = false;
            popupGroup.blocksRaycasts = false;
        }

        if (panelGroup != null)
            panelGroup.alpha = 0f;

        if (animationContainer != null)
            animationContainer.localScale = Vector3.one;

        if (premiumFx != null)
            premiumFx.ResetVisualState();

        SetRowAlpha(1f);
    }

    private void ReleaseInputBlock()
    {
        if (catMovement != null)
            catMovement.ReleaseInputBlock(this);
    }

    private void SetRowAlpha(float alpha)
    {
        if (hungerRow != null) hungerRow.alpha = alpha;
        if (thirstRow != null) thirstRow.alpha = alpha;
        if (energyRow != null) energyRow.alpha = alpha;
    }

    private static void SetRowProgress(CanvasGroup row, float progress, float delay)
    {
        if (row != null)
            row.alpha = Mathf.Clamp01((progress - delay) / 0.22f);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    public static string FormatDuration(TimeSpan duration)
    {
        int totalMinutes = Math.Max(0, (int)Math.Floor(duration.TotalMinutes));
        int days = totalMinutes / 1440;
        int hours = totalMinutes % 1440 / 60;
        int minutes = totalMinutes % 60;

        if(GameLanguageService.Current==GameLanguage.Turkish)
        {
            if(days>0)return hours>0?$"{days} gün {hours} saat":$"{days} gün";
            if(hours>0)return minutes>0?$"{hours} saat {minutes} dakika":$"{hours} saat";
            return $"{minutes} dakika";
        }
        if (days > 0)
            return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
        if (hours > 0)
            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        return $"{minutes}m";
    }

    public static bool MeetsDisplayRequirements(
        CatHomeSaveSystem.OfflineReturnSummary summary,
        float minimumMinutes)
    {
        return summary.OfflineProgressApplied &&
               summary.AppliedDuration.TotalMinutes >= Math.Max(0f, minimumMinutes);
    }

    public static string FormatNeed(string label, float before, float after)
    {
        string direction = after > before + ChangeTolerance
            ? "  \u2191"
            : after < before - ChangeTolerance ? "  \u2193" : string.Empty;
        return $"{label}     {Mathf.RoundToInt(before)}%  \u2192  {Mathf.RoundToInt(after)}%{direction}";
    }

    public static string FormatNeedValues(float before, float after)
    {
        string direction = after > before + ChangeTolerance
            ? "  \u2191"
            : after < before - ChangeTolerance ? "  \u2193" : string.Empty;
        return $"{Mathf.RoundToInt(before)}%  \u2192  {Mathf.RoundToInt(after)}%{direction}";
    }

    public static string SelectSummaryCopy(CatHomeSaveSystem.OfflineReturnSummary summary)
    {
        if(summary.ThirstAfter<25f) return GameLanguageService.Text("return.water");
        if(summary.HungerAfter<25f) return GameLanguageService.Text("return.food");
        if(summary.EnergyAfter<25f) return GameLanguageService.Text("return.rest");
        float energyChange=summary.EnergyAfter-summary.EnergyBefore;
        if(summary.WasSleeping && energyChange>ChangeTolerance)
            return GameLanguageService.Format("return.nap",Mathf.RoundToInt(energyChange));
        return GameLanguageService.Text("return.missed");
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.35f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted +
               overshoot * shifted * shifted;
    }
}
