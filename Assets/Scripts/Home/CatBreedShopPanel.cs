using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Premium modal for browsing and equipping the imported cat breeds.</summary>
[DisallowMultipleComponent]
public sealed class CatBreedShopPanel : MonoBehaviour
{
    [Serializable]
    private struct BreedCard
    {
        public string id;
        public Button button;
        public GameObject selectionRing;
        public GameObject activeBadge;
        public TMP_Text label;
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
    [SerializeField] private Button useButton;
    [SerializeField] private TMP_Text useButtonLabel;
    [SerializeField] private TMP_Text breedName;
    [SerializeField] private TMP_Text breedStatus;
    [SerializeField] private CatBreedTurntablePreview turntable;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button[] coatButtons = Array.Empty<Button>();
    [SerializeField] private GameObject[] coatSelection = Array.Empty<GameObject>();
    private int draftCoat;
    [SerializeField] private BreedCard[] cards = Array.Empty<BreedCard>();

    [Header("Animation")]
    [SerializeField, Min(.05f)] private float transitionDuration = .2f;
    [SerializeField, Range(.75f, 1f)] private float revealScale = .9f;
    [SerializeField] private bool reducedMotion;

    private static CatBreedShopPanel activeInstance;
    private PanelState state = PanelState.Closed;
    private Coroutine animationRoutine;
    private bool inputBlockHeld;
    private bool listenersBound;
    private Vector3 panelAuthoredScale = Vector3.one;
    private Vector3 panelBaseScale = Vector3.one;
    private int draftIndex;

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
        StorybookCatPresentation.Apply(transform);
        PremiumTypography.ApplyNameInput(nameInput);
        PremiumScrollInput.Ensure(GetComponentInChildren<ScrollRect>(true));
        panelAuthoredScale = panelVisual != null ? panelVisual.localScale : Vector3.one;
        ApplyResponsiveLayout();
        BindListeners();
        ApplyClosedVisuals();
    }

    private void OnEnable()
    {
        GameLanguageService.Changed += OnLanguageChanged;
        ApplyResponsiveLayout();
        BindListeners();
    }

    public void RequestOpen()
    {
        if (state != PanelState.Closed && state != PanelState.Closing)
            return;

        CatBreedCatalog catalog = CatBreedCatalog.Load();
        if (catalog == null || catalog.Count == 0)
        {
            Debug.LogWarning("CAT SHOP needs a generated CatBreedCatalog asset.", this);
            return;
        }

        draftIndex = Mathf.Max(0, catalog.IndexOf(CatBreedService.SelectedBreedId));
        draftCoat = CatIdentityService.CoatIndex;
        if (nameInput != null) nameInput.SetTextWithoutNotify(CatIdentityService.CatName);
        RefreshSelection(true);
        Canvas.ForceUpdateCanvases();
        var scroll=GetComponentInChildren<ScrollRect>(true);
        if(scroll!=null && scroll.content!=null && scroll.viewport!=null)
        {
            float range=scroll.content.rect.height-scroll.viewport.rect.height;
            if(range>0)scroll.verticalNormalizedPosition=1f-Mathf.Clamp01((draftIndex/2)*194f/range);
        }
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

    public void SetReducedMotion(bool value) { reducedMotion = value; turntable?.SetReducedMotion(value); }

    private void SelectCard(int index)
    {
        CatBreedCatalog catalog = CatBreedCatalog.Load();
        if (catalog == null || catalog.Get(index) == null)
            return;
        draftIndex = index;
        RefreshSelection(true);
    }

    private void EquipDraft()
    {
        CatBreedCatalog catalog = CatBreedCatalog.Load();
        CatBreedCatalog.Entry entry = catalog?.Get(draftIndex);
        if (entry == null || !CatBreedService.Select(entry.Id))
            return;
        if (nameInput != null) CatIdentityService.CatName = nameInput.text;
        CatIdentityService.CoatIndex = draftCoat;
        RefreshSelection(false);
        RequestClose();
    }

    private void RefreshSelection(bool rebuildPreview)
    {
        CatBreedCatalog catalog = CatBreedCatalog.Load();
        CatBreedCatalog.Entry draft = catalog?.Get(draftIndex);
        if (draft == null)
            return;

        if (breedName != null)
            breedName.text = draft.DisplayName;

        bool equipped = string.Equals(
            CatBreedService.SelectedBreedId, draft.Id, StringComparison.Ordinal);
        if (breedStatus != null)
            breedStatus.text = equipped
                ? GameLanguageService.Text("cat.current")
                : GameLanguageService.Text("cat.preview");
        if (useButtonLabel != null)
            useButtonLabel.text = GameLanguageService.Text("cat.continue");
        if (useButton != null)
            useButton.interactable = true;

        for (int i = 0; i < cards.Length; i++)
        {
            bool previewed = i == draftIndex;
            bool active = string.Equals(cards[i].id,
                CatBreedService.SelectedBreedId, StringComparison.Ordinal);
            if (cards[i].selectionRing != null)
                cards[i].selectionRing.SetActive(previewed);
            if (cards[i].activeBadge != null)
                cards[i].activeBadge.SetActive(active);
            if (cards[i].button != null)
                cards[i].button.interactable = true; // Test phase: every breed is open.
            StorybookCatPresentation.Selection(cards[i].button, previewed);
        }

        if (rebuildPreview && turntable != null)
            turntable.Show(draft);
        else if (turntable != null)
            turntable.SetPreviewActive(true);
        RefreshCoat();
    }

    private void SelectCoat(int index) { draftCoat = index; RefreshCoat(); }
    private void RefreshCoat()
    {
        for (int i = 0; i < coatButtons.Length; i++)
            ModernUiArt.Action(coatButtons[i]);
        for (int i = 0; i < coatButtons.Length; i++)
            StorybookCatPresentation.Coat(coatButtons[i], i);
        for (int i = 0; i < coatSelection.Length; i++)
            if (coatSelection[i] != null) coatSelection[i].SetActive(i == draftCoat);
        if (turntable != null && draftCoat >= 0 && draftCoat < CatIdentityService.CoatCount)
            turntable.SetTint(CatIdentityService.Palette[draftCoat].Tint);
    }

    private void ApplyResponsiveLayout()
    {
        if (panelVisual == null)
            return;
        if (panelAuthoredScale == Vector3.zero)
            panelAuthoredScale = Vector3.one;
        float availableWidth = safeArea != null ? safeArea.rect.width : Screen.width;
        float availableHeight = safeArea != null ? safeArea.rect.height : Screen.height;
        float fit = Mathf.Min(1f,
            Mathf.Min((availableWidth - 36f) / Mathf.Max(1f, panelVisual.rect.width),
                (availableHeight - 36f) / Mathf.Max(1f, panelVisual.rect.height)));
        panelBaseScale = panelAuthoredScale * Mathf.Clamp(fit, .68f, 1f);
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
        float duration = reducedMotion ? .01f : Mathf.Max(.05f, transitionDuration);
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
            turntable?.SetPreviewActive(false);
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
        turntable?.SetPreviewActive(false);
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
            scrimButton.onClick.AddListener(RequestClose);
        if (closeButton != null)
            closeButton.onClick.AddListener(RequestClose);
        if (useButton != null)
            useButton.onClick.AddListener(EquipDraft);
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].button == null)
                continue;
            int index = i;
            cards[i].button.onClick.AddListener(() => SelectCard(index));
        }
        for (int i = 0; i < coatButtons.Length; i++)
        {
            int index = i;
            if (coatButtons[i] != null) coatButtons[i].onClick.AddListener(() => SelectCoat(index));
        }
        listenersBound = true;
    }

    private void OnDisable()
    {
        GameLanguageService.Changed -= OnLanguageChanged;
        turntable?.SetPreviewActive(false);
        ReleaseInputBlock();
    }

    private void OnLanguageChanged()
    {
        if(state!=PanelState.Closed)RefreshSelection(false);
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
        ReleaseInputBlock();
    }
}
