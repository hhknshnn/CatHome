using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Premium settings panel opened from the hamburger's SETTINGS row. Each row is a
/// simple ON/OFF toggle bound by key to an existing preference service: the home
/// soundscape (<see cref="HomeAudioService"/>) and the shared mini-game switches
/// (<see cref="CatRunnerProgressService"/>). No new persistence is introduced.
/// </summary>
[DisallowMultipleComponent]
public sealed class SettingsPanel : MonoBehaviour
{
    [Serializable]
    private struct ToggleRow
    {
        public string key;
        public Button button;
        public LowPolyPanelGraphic face;
        public TMP_Text stateText;
    }

    private enum PanelState { Closed, Opening, Open, Closing }

    [Header("Input")]
    [SerializeField] private CatMovement catMovement;

    [Header("Canvas")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private Button scrimButton;
    [SerializeField] private RectTransform panelVisual;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private ToggleRow[] rows = Array.Empty<ToggleRow>();

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float transitionDuration = 0.2f;
    [SerializeField, Range(0.75f, 1f)] private float revealScale = 0.88f;
    [SerializeField] private bool reducedMotion;

    private static readonly Color OnColor = new Color(0.42f, 0.84f, 0.55f);
    private static readonly Color OffColor = new Color(0.74f, 0.72f, 0.78f);

    private static SettingsPanel activeInstance;
    private PanelState state = PanelState.Closed;
    private Coroutine animationRoutine;
    private bool inputBlockHeld;
    private bool listenersBound;
    private bool prefsBound;
    private Vector3 panelBaseScale = Vector3.one;
    private Vector3 panelAuthoredScale = Vector3.one;

    public static bool IsAnyOpen =>
        activeInstance != null && activeInstance.state != PanelState.Closed;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            gameObject.SetActive(false);
            return;
        }
        activeInstance = this;
        panelAuthoredScale = panelVisual != null ? panelVisual.localScale : Vector3.one;
        ApplyResponsiveLayout();
        BindListeners();
        ApplyClosedVisuals();
    }

    private void OnEnable()
    {
        ApplyResponsiveLayout();
        BindListeners();
        RefreshRows();
    }

    public void RequestOpen()
    {
        if (state != PanelState.Closed && state != PanelState.Closing)
            return;
        reducedMotion = CatRunnerProgressService.ReducedMotion;
        RefreshRows();
        AcquireInputBlock();
        state = PanelState.Opening;
        SetCanvasInteractive(true);
        StartPanelAnimation(true);
    }

    public void RequestClose()
    {
        if (state == PanelState.Closed || state == PanelState.Closing)
            return;
        state = PanelState.Closing;
        SetCanvasInteractive(false);
        StartPanelAnimation(false);
    }

    public void SetReducedMotion(bool value) => reducedMotion = value;

    private void OnRowTapped(int index)
    {
        if (index < 0 || index >= rows.Length)
            return;
        string key = rows[index].key;
        if (key == "language")
        {
            GameLanguageService.Toggle();
            RefreshRows();
            return;
        }
        SetPref(key, !GetPref(key));
        RefreshRows();
    }

    private void RefreshRows()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].key == "language")
            {
                if (rows[i].stateText != null)
                {
                    rows[i].stateText.text = GameLanguageService.Text(
                        GameLanguageService.Current == GameLanguage.English
                            ? "language.english"
                            : "language.turkish");
                }
                if (rows[i].face != null)
                    rows[i].face.SetPremiumBaseColor(PremiumUiStyle.CandyAqua);
                continue;
            }
            bool on = GetPref(rows[i].key);
            if (rows[i].stateText != null)
                rows[i].stateText.text = GameLanguageService.Text(
                    on ? "settings.on" : "settings.off");
            if (rows[i].face != null)
                rows[i].face.SetPremiumBaseColor(on ? OnColor : OffColor);
        }
    }

    private static bool GetPref(string key)
    {
        switch (key)
        {
            case "music": return HomeAudioService.MusicEnabled;
            case "sound": return HomeAudioService.SoundEnabled;
            case "game_sound": return CatRunnerProgressService.SoundEnabled;
            case "haptics": return CatRunnerProgressService.HapticsEnabled;
            case "reduced_motion": return CatRunnerProgressService.ReducedMotion;
            default: return false;
        }
    }

    private static void SetPref(string key, bool value)
    {
        switch (key)
        {
            case "music": HomeAudioService.MusicEnabled = value; break;
            case "sound": HomeAudioService.SoundEnabled = value; break;
            case "game_sound": CatRunnerProgressService.SetSoundEnabled(value); break;
            case "haptics": CatRunnerProgressService.SetHapticsEnabled(value); break;
            case "reduced_motion": CatRunnerProgressService.SetReducedMotion(value); break;
        }
    }

    // --- lifecycle (mirrors the other home panels) ------------------------

    private void ApplyResponsiveLayout()
    {
        if (panelVisual == null)
            return;
        if (panelAuthoredScale == Vector3.zero)
            panelAuthoredScale = Vector3.one;
        float availableWidth = safeArea != null ? safeArea.rect.width : Screen.width;
        float availableHeight = safeArea != null ? safeArea.rect.height : Screen.height;
        float panelWidth = Mathf.Max(1f, panelVisual.rect.width);
        float panelHeight = Mathf.Max(1f, panelVisual.rect.height);
        float fit = Mathf.Min(1f,
            Mathf.Min((availableWidth - 36f) / panelWidth,
                (availableHeight - 36f) / panelHeight));
        fit = Mathf.Clamp(fit, .72f, 1f);
        panelBaseScale = panelAuthoredScale * fit;
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyResponsiveLayout();
        if (panelVisual != null)
            ApplyAnimation(rootGroup != null ? rootGroup.alpha : 0f);
    }

    private void StartPanelAnimation(bool opening)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimatePanel(opening));
    }

    private IEnumerator AnimatePanel(bool opening)
    {
        float duration = reducedMotion ? 0.01f : Mathf.Max(0.05f, transitionDuration);
        float elapsed = 0f;
        float from = opening ? 0f : 1f;
        float to = opening ? 1f : 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            ApplyAnimation(Mathf.Lerp(from, to, t));
            yield return null;
        }
        ApplyAnimation(to);
        animationRoutine = null;
        if (opening)
        {
            state = PanelState.Open;
        }
        else
        {
            state = PanelState.Closed;
            SetCanvasInteractive(false);
            ReleaseInputBlock();
        }
    }

    private void ApplyAnimation(float amount)
    {
        if (rootGroup != null)
            rootGroup.alpha = amount;
        if (panelGroup != null)
            panelGroup.alpha = amount;
        if (panelVisual != null)
        {
            float scale = reducedMotion ? 1f : Mathf.Lerp(revealScale, 1f, amount);
            panelVisual.localScale = panelBaseScale * scale;
        }
    }

    private void ApplyClosedVisuals()
    {
        state = PanelState.Closed;
        ApplyAnimation(0f);
        SetCanvasInteractive(false);
    }

    private void SetCanvasInteractive(bool value)
    {
        if (rootGroup == null)
            return;
        rootGroup.interactable = value;
        rootGroup.blocksRaycasts = value;
    }

    private void AcquireInputBlock()
    {
        if (inputBlockHeld)
            return;
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (catMovement == null)
            return;
        catMovement.AcquireInputBlock(this);
        inputBlockHeld = true;
    }

    private void ReleaseInputBlock()
    {
        if (!inputBlockHeld)
            return;
        if (catMovement != null)
            catMovement.ReleaseInputBlock(this);
        inputBlockHeld = false;
    }

    private void BindListeners()
    {
        if (listenersBound)
            return;
        if (scrimButton != null)
        {
            scrimButton.onClick.RemoveListener(RequestClose);
            scrimButton.onClick.AddListener(RequestClose);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(RequestClose);
            closeButton.onClick.AddListener(RequestClose);
        }
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].button == null)
                continue;
            int index = i;
            rows[i].button.onClick.AddListener(() => OnRowTapped(index));
        }
        if (!prefsBound)
        {
            HomeAudioService.Changed += RefreshRows;
            CatRunnerProgressService.PreferencesChanged += RefreshRows;
            GameLanguageService.Changed += RefreshRows;
            prefsBound = true;
        }
        listenersBound = true;
    }

    private void OnDisable()
    {
        ReleaseInputBlock();
    }

    private void OnDestroy()
    {
        if (prefsBound)
        {
            HomeAudioService.Changed -= RefreshRows;
            CatRunnerProgressService.PreferencesChanged -= RefreshRows;
            GameLanguageService.Changed -= RefreshRows;
            prefsBound = false;
        }
        if (activeInstance == this)
            activeInstance = null;
        ReleaseInputBlock();
    }
}
