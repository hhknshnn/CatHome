using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Premium cat customisation panel opened from the hamburger's CAT JOURNAL row.
/// The player names the cat and picks a coat colour; both persist through
/// <see cref="CatIdentityService"/> and the live cat re-tints instantly via
/// <see cref="CatAppearanceController"/>. Mirrors the show/hide, input-block and
/// reduced-motion lifecycle of the other home panels.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatJournalPanel : MonoBehaviour
{
    [Serializable]
    private struct Swatch
    {
        public Button button;
        public GameObject selectionRing;
    }

    private enum PanelState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    [Header("Input")]
    [SerializeField] private CatMovement catMovement;

    [Header("Canvas")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private Button scrimButton;
    [SerializeField] private RectTransform panelVisual;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text namePreview;
    [SerializeField] private TMP_Text coatNameText;
    [SerializeField] private Graphic[] coatPreviewParts = Array.Empty<Graphic>();
    [SerializeField] private Graphic[] coatPreviewAccentParts = Array.Empty<Graphic>();
    [SerializeField] private Swatch[] swatches = Array.Empty<Swatch>();

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float transitionDuration = 0.2f;
    [SerializeField, Range(0.75f, 1f)] private float revealScale = 0.88f;
    [SerializeField] private bool reducedMotion;

    private static CatJournalPanel activeInstance;
    private PanelState state = PanelState.Closed;
    private Coroutine animationRoutine;
    private bool inputBlockHeld;
    private bool listenersBound;
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
        PremiumTypography.ApplyNameInput(nameInput);
        panelAuthoredScale = panelVisual != null ? panelVisual.localScale : Vector3.one;
        ApplyResponsiveLayout();
        BindListeners();
        ApplyClosedVisuals();
    }

    private void OnEnable()
    {
        ApplyResponsiveLayout();
        BindListeners();
    }

    public void RequestOpen()
    {
        if (state != PanelState.Closed && state != PanelState.Closing)
            return;

        RefreshFromIdentity();
        AcquireInputBlock();
        state = PanelState.Opening;
        SetCanvasInteractive(true);
        StartPanelAnimation(true);
    }

    public void RequestClose()
    {
        if (state == PanelState.Closed || state == PanelState.Closing)
            return;
        CommitName();
        state = PanelState.Closing;
        SetCanvasInteractive(false);
        StartPanelAnimation(false);
    }

    public void SetReducedMotion(bool value) => reducedMotion = value;

    private void RefreshFromIdentity()
    {
        if (nameInput != null)
            nameInput.SetTextWithoutNotify(CatIdentityService.CatName);
        UpdateNamePreview(CatIdentityService.CatName);
        RefreshSwatchSelection();
    }

    private void OnNameChanged(string value)
    {
        UpdateNamePreview(value);
    }

    private void OnNameEndEdit(string value)
    {
        CatIdentityService.CatName = value;
        if (nameInput != null)
            nameInput.SetTextWithoutNotify(CatIdentityService.CatName);
        UpdateNamePreview(CatIdentityService.CatName);
    }

    private void CommitName()
    {
        if (nameInput != null)
            CatIdentityService.CatName = nameInput.text;
    }

    private void UpdateNamePreview(string value)
    {
        if (namePreview != null)
            namePreview.text = string.IsNullOrWhiteSpace(value)
                ? "MELO"
                : value.Trim().ToUpperInvariant();
    }

    private void OnSwatchSelected(int index)
    {
        CatIdentityService.CoatIndex = index;
        RefreshSwatchSelection();
    }

    private void RefreshSwatchSelection()
    {
        int selected = CatIdentityService.CoatIndex;
        for (int i = 0; i < swatches.Length; i++)
        {
            if (swatches[i].selectionRing != null)
                swatches[i].selectionRing.SetActive(i == selected);
        }
        if (coatNameText != null && selected >= 0 && selected < CatIdentityService.CoatCount)
            coatNameText.text = CatIdentityService.Palette[selected].Name + " COAT";

        if (selected < 0 || selected >= CatIdentityService.CoatCount)
            return;
        Color coat = Color.Lerp(CatIdentityService.Palette[selected].Tint,
            new Color(0.52f, 0.48f, 0.46f), 0.34f);
        Color accent = Color.Lerp(coat, PremiumUiStyle.Ink, 0.34f);
        for (int i = 0; i < coatPreviewParts.Length; i++)
            if (coatPreviewParts[i] != null)
                coatPreviewParts[i].color = coat;
        for (int i = 0; i < coatPreviewAccentParts.Length; i++)
            if (coatPreviewAccentParts[i] != null)
                coatPreviewAccentParts[i].color = accent;
    }

    // --- Animation / lifecycle (mirrors the other home panels) ------------

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
        if (nameInput != null)
        {
            nameInput.characterLimit = CatIdentityService.MaxNameLength;
            nameInput.onValueChanged.RemoveListener(OnNameChanged);
            nameInput.onValueChanged.AddListener(OnNameChanged);
            nameInput.onEndEdit.RemoveListener(OnNameEndEdit);
            nameInput.onEndEdit.AddListener(OnNameEndEdit);
        }
        for (int i = 0; i < swatches.Length; i++)
        {
            if (swatches[i].button == null)
                continue;
            int index = i;
            swatches[i].button.onClick.AddListener(() => OnSwatchSelected(index));
        }
        listenersBound = true;
    }

    private void OnDisable()
    {
        ReleaseInputBlock();
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
        ReleaseInputBlock();
    }
}
