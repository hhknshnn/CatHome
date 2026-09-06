using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent premium room picker. It is intentionally separate from the ROOM
/// furniture tab and the HOME purchase flow: cards navigate, the store owns
/// purchases, and LevelLoader alone mutates loaded scenes.
/// </summary>
[DisallowMultipleComponent]
public sealed class RoomSelectorPanel : MonoBehaviour
{
    [Serializable]
    private struct RoomCard
    {
        public string roomId;
        public Button button;
        public CanvasGroup group;
        public LowPolyPanelGraphic face;
        public TMP_Text titleText;
        public TMP_Text statusText;
        public TMP_Text actionText;
        public GameObject lockBadge;
        public GameObject currentBadge;
    }

    private enum PanelState
    {
        Closed,
        Opening,
        Open,
        Closing,
        Travelling
    }

    [Header("Input")]
    [SerializeField] private CatMovement catMovement;
    [SerializeField] private LevelLoader levelLoader;

    [Header("Canvas")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private Button scrimButton;
    [SerializeField] private RectTransform panelVisual;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private ScrollRect roomScroll;
    [SerializeField] private RoomCard[] cards = Array.Empty<RoomCard>();

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float transitionDuration = 0.2f;
    [SerializeField, Range(0.75f, 1f)] private float revealScale = 0.88f;
    [SerializeField] private bool reducedMotion;

    private static RoomSelectorPanel activeInstance;
    private PanelState state = PanelState.Closed;
    private Coroutine animationRoutine;
    private bool inputBlockHeld;
    private bool listenersBound;
    private bool loaderEventsBound;
    private Vector3 panelBaseScale = Vector3.one;
    private Vector3 panelAuthoredScale = Vector3.one;

    public static bool IsAnyOpen =>
        activeInstance != null && activeInstance.state != PanelState.Closed;
    public bool IsOpen => state != PanelState.Closed && state != PanelState.Closing;
    public bool IsTravelling => state == PanelState.Travelling;

    private void Awake()
    {
        if (activeInstance != null && activeInstance != this)
        {
            Debug.LogWarning("Duplicate RoomSelectorPanel disabled.", this);
            gameObject.SetActive(false);
            return;
        }

        activeInstance = this;
        panelAuthoredScale = panelVisual != null ? panelVisual.localScale : Vector3.one;
        ApplyResponsiveLayout();
        ResolveSceneReferences();
        BindListeners();
        ApplyClosedVisuals();
    }

    private void OnEnable()
    {
        ApplyResponsiveLayout();
        ResolveSceneReferences();
        BindListeners();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyResponsiveLayout();
        if (panelVisual != null)
        {
            float amount = rootGroup != null ? rootGroup.alpha : 0f;
            ApplyAnimation(amount);
        }
    }

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

    public void ResolveSceneReferences()
    {
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);

        LevelLoader resolvedLoader = levelLoader;
        if (resolvedLoader == null)
            resolvedLoader = FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);

        if (resolvedLoader != levelLoader)
        {
            UnbindLoaderEvents();
            levelLoader = resolvedLoader;
        }
        BindLoaderEvents();
    }

    public void RequestOpen()
    {
        if (state != PanelState.Closed && state != PanelState.Closing)
            return;

        ResolveSceneReferences();
        if (levelLoader == null)
        {
            Debug.LogWarning("Room selector needs the GameScene LevelLoader.", this);
            return;
        }

        RefreshCards();
        ResetRoomScroll();
        AcquireInputBlock();
        state = PanelState.Opening;
        SetCanvasInteractive(true);
        StartPanelAnimation(true);
    }

    public void RequestClose()
    {
        if (state == PanelState.Travelling || state == PanelState.Closed ||
            state == PanelState.Closing)
        {
            return;
        }

        state = PanelState.Closing;
        SetCanvasInteractive(false);
        StartPanelAnimation(false);
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
    }

    private void OnRoomSelected(string roomId)
    {
        if (state != PanelState.Open || levelLoader == null)
            return;

        if (!HomeRoomService.TryGetRoom(roomId, out HomeRoomDefinition room))
        {
            SetFeedback("THIS ROOM IS NOT AVAILABLE YET.");
            return;
        }

        bool unlocked = HomeRoomService.IsRoomUnlocked(room.Id);
        if (!unlocked && HomeStoreService.FreePurchaseTestingEnabled &&
            !string.IsNullOrWhiteSpace(room.RequiredOwnershipId))
        {
            HomeStorePurchaseResult acquired =
                HomeStoreService.TryAcquireForTesting(room.RequiredOwnershipId);
            unlocked = acquired.Succeeded ||
                       acquired.Status == HomeStorePurchaseStatus.AlreadyOwned;
        }

        if (!unlocked)
        {
            SetFeedback("UNLOCK " + room.DisplayName + " IN SHOP  •  HOME");
            OpenHomeStore();
            return;
        }

        if (string.Equals(HomeRoomService.CurrentRoomId, room.Id, StringComparison.Ordinal))
        {
            SetFeedback("YOU ARE ALREADY IN " + room.DisplayName + ".");
            return;
        }

        if (!levelLoader.LoadRoom(room.Id))
        {
            SetFeedback("ROOM COULD NOT BE OPENED. TRY AGAIN.");
            return;
        }

        state = PanelState.Travelling;
        SetCardsInteractive(false);
        if (closeButton != null)
            closeButton.interactable = false;
        SetFeedback("TRAVELLING TO " + room.DisplayName + "  •  •  •");
    }

    private void OpenHomeStore()
    {
        ShopPanelController shop =
            FindAnyObjectByType<ShopPanelController>(FindObjectsInactive.Include);
        ForceCloseImmediate();
        if (shop != null)
            shop.RequestOpen(HomeStoreCategory.Home);
        else
            Debug.LogWarning("Room selector could not find the Home Store panel.", this);
    }

    private void HandleRoomLoadStarted(HomeRoomDefinition room)
    {
        if (state == PanelState.Closed)
            return;
        state = PanelState.Travelling;
        SetCardsInteractive(false);
        if (closeButton != null)
            closeButton.interactable = false;
        SetFeedback("TRAVELLING TO " + room.DisplayName + "  •  •  •");
    }

    private void HandleRoomLoaded(HomeRoomDefinition room)
    {
        // The old room owned the previous cat. Rebind before releasing the modal
        // so the newly active cat receives a balanced input block lifecycle.
        RebindInputBlockToActiveCat();
        RefreshCards();
        SetFeedback(room.DisplayName + " IS READY!");
        if (isActiveAndEnabled)
            StartCoroutine(CloseAfterArrival());
        else
            ForceCloseImmediate();
    }

    private IEnumerator CloseAfterArrival()
    {
        yield return new WaitForSecondsRealtime(reducedMotion ? 0.02f : 0.28f);
        if (state == PanelState.Travelling)
        {
            state = PanelState.Closing;
            SetCanvasInteractive(false);
            StartPanelAnimation(false);
        }
    }

    private void HandleRoomLoadFailed(string roomId, string reason)
    {
        if (state == PanelState.Closed)
            return;
        state = PanelState.Open;
        SetCardsInteractive(true);
        if (closeButton != null)
            closeButton.interactable = true;
        SetFeedback(string.IsNullOrWhiteSpace(reason) ? "ROOM COULD NOT BE OPENED." : reason);
        RefreshCards();
    }

    private void HandleOwnershipChanged(string productId)
    {
        if (state != PanelState.Closed)
            RefreshCards();
    }

    private void RefreshCards()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            RoomCard card = cards[i];
            if (!HomeRoomService.TryGetRoom(card.roomId, out HomeRoomDefinition room))
            {
                if (card.group != null)
                    card.group.gameObject.SetActive(false);
                continue;
            }

            bool current = string.Equals(
                HomeRoomService.CurrentRoomId,
                room.Id,
                StringComparison.Ordinal);
            bool unlocked = HomeRoomService.IsRoomUnlocked(room.Id);

            if (card.titleText != null)
                card.titleText.text = room.DisplayName;
            if (card.statusText != null)
            {
                card.statusText.text = current
                    ? GameLanguageService.Format("rooms.progress", HomeStoreService.GetRoomOwnedCount(room.Id), HomeStoreService.GetRoomCollection(room.Id).Count)
                    : GameLanguageService.Text(unlocked ? "rooms.ready" : "rooms.unlock_hint");
            }
            if (card.actionText != null)
                card.actionText.text = GameLanguageService.Text(current ? "rooms.current" : unlocked ? "rooms.visit" : "rooms.unlock");
            if (card.lockBadge != null)
                card.lockBadge.SetActive(!unlocked);
            if (card.currentBadge != null)
                card.currentBadge.SetActive(current);
            if (card.face != null)
            {
                Color color = current
                    ? PremiumUiStyle.Mint
                    : PremiumUiStyle.Ivory;
                card.face.SetPremiumBaseColor(color);
            }
            if (card.button != null)
                card.button.interactable = state == PanelState.Open;
        }

        if (state != PanelState.Travelling)
        {
            HomeRoomDefinition current = HomeRoomService.CurrentRoom;
            SetFeedback(GameLanguageService.Format("rooms.current_hint", current.DisplayName));
        }
    }

    private void ResetRoomScroll()
    {
        if (roomScroll == null)
            return;
        Canvas.ForceUpdateCanvases();
        if (roomScroll.content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(roomScroll.content);
        roomScroll.verticalNormalizedPosition = 1f;
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
            SetCardsInteractive(true);
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

    private void ForceCloseImmediate()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }
        state = PanelState.Closed;
        ApplyAnimation(0f);
        SetCanvasInteractive(false);
        ReleaseInputBlock();
    }

    private void SetCanvasInteractive(bool value)
    {
        if (rootGroup == null)
            return;
        rootGroup.interactable = value;
        rootGroup.blocksRaycasts = value;
        if (scrimButton != null)
            scrimButton.interactable = value;
        if (closeButton != null)
            closeButton.interactable = value;
    }

    private void SetCardsInteractive(bool value)
    {
        for (int i = 0; i < cards.Length; i++)
            if (cards[i].button != null)
                cards[i].button.interactable = value;
    }

    private void SetFeedback(string value)
    {
        if (feedbackText != null)
            feedbackText.text = value;
    }

    private void AcquireInputBlock()
    {
        if (inputBlockHeld)
            return;
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>();
        if (catMovement == null)
            return;
        catMovement.AcquireInputBlock(this);
        inputBlockHeld = true;
    }

    private void RebindInputBlockToActiveCat()
    {
        if (inputBlockHeld && catMovement != null)
            catMovement.ReleaseInputBlock(this);
        inputBlockHeld = false;
        catMovement = FindAnyObjectByType<CatMovement>();
        if (state != PanelState.Closed)
            AcquireInputBlock();
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
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].button == null)
                continue;
            string roomId = cards[i].roomId;
            cards[i].button.onClick.AddListener(() => OnRoomSelected(roomId));
        }

        HomeStoreService.OwnershipChanged += HandleOwnershipChanged;
        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!listenersBound)
            return;
        if (scrimButton != null)
            scrimButton.onClick.RemoveListener(RequestClose);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(RequestClose);
        for (int i = 0; i < cards.Length; i++)
            if (cards[i].button != null)
                cards[i].button.onClick.RemoveAllListeners();
        HomeStoreService.OwnershipChanged -= HandleOwnershipChanged;
        listenersBound = false;
    }

    private void BindLoaderEvents()
    {
        if (loaderEventsBound || levelLoader == null)
            return;
        levelLoader.RoomLoadStarted += HandleRoomLoadStarted;
        levelLoader.RoomLoaded += HandleRoomLoaded;
        levelLoader.RoomLoadFailed += HandleRoomLoadFailed;
        loaderEventsBound = true;
    }

    private void UnbindLoaderEvents()
    {
        if (!loaderEventsBound || levelLoader == null)
            return;
        levelLoader.RoomLoadStarted -= HandleRoomLoadStarted;
        levelLoader.RoomLoaded -= HandleRoomLoaded;
        levelLoader.RoomLoadFailed -= HandleRoomLoadFailed;
        loaderEventsBound = false;
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
        UnbindLoaderEvents();
        UnbindListeners();
    }
}
