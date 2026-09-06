using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CP1 shell for the Cat Home main menu: a compact, modular low-poly list that
/// drops down from the top-right menu button. This controller is deliberately
/// dumb: it knows nothing about quests, economy, tabs, notifications, save data
/// or progression. Its only responsibilities are open/close state, the
/// fade + scale reveal animation, responsive width, input blocking and safe
/// coexistence with the existing modal flows.
///
/// The old right-side drawer (slide-in, ~44% width, heavy scrim) is gone. The
/// list now opens directly beneath the button, keeps most of the room and cat
/// visible, and its rows are pure visual prototypes that emit a single safe log
/// per tap and open nothing.
///
/// The hierarchy is authored by <c>MainPanelBuilder</c> (Editor) and wired into
/// the serialized fields below. References are also resolved defensively at
/// runtime, so a partially wired object degrades to "stays closed" instead of
/// throwing.
/// </summary>
[DisallowMultipleComponent]
public sealed class MainPanelController : MonoBehaviour
{
    private enum PanelState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    /// <summary>
    /// A single prototype navigation row. In CP1 a tap only logs; no row is
    /// bound to any real system.
    ///
    /// <c>badge</c>/<c>badgeText</c> are pure UI scaffolding for a future unread
    /// count: the small red/orange circular badge is authored hidden and stays
    /// hidden until <see cref="SetRowBadge"/> is called. No gameplay system feeds
    /// it in CP1.
    /// </summary>
    [System.Serializable]
    private struct MenuRow
    {
        public string id;
        public Button button;
        public CanvasGroup group;
        public GameObject badge;
        public TMP_Text badgeText;

        // Set by MainPanelBuilder. The controller measures <c>label</c>'s
        // preferredWidth and drives <c>layout</c>'s preferredWidth from it so each
        // card is exactly as wide as its icon + title + padding. <c>labelLayout</c>
        // pins every row's title area to the longest title's width so the four
        // icons and titles line up on the same x inside their centred groups.
        public TMP_Text label;
        public LayoutElement layout;
        public LayoutElement labelLayout;
    }

    [Header("Input")]
    [SerializeField] private CatMovement catMovement;

    [Header("Scrim (near-invisible outside-tap catcher)")]
    [SerializeField] private CanvasGroup scrimGroup;
    [SerializeField] private Button scrimButton;

    [Header("Menu Button (closed state)")]
    [SerializeField] private CanvasGroup menuButtonGroup;
    [SerializeField] private Button menuButton;

    [Header("Shop Button (top bar, left of the hamburger)")]
    [SerializeField] private CanvasGroup shopButtonGroup;
    [SerializeField] private Button shopButton;

    [Header("Cat Shop Button (top bar, left of Shop)")]
    [SerializeField] private CanvasGroup catShopButtonGroup;
    [SerializeField] private Button catShopButton;
    [SerializeField] private Image catShopButtonIcon;

    [Header("Compact List")]
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform menuList;
    [SerializeField] private CanvasGroup menuListGroup;
    [SerializeField] private MenuRow[] rows = new MenuRow[0];

    [Header("Responsive Width (canvas units, 1920-wide reference)")]
    [SerializeField, Range(0.1f, 0.5f)] private float widthFraction = 0.205f;
    [SerializeField, Min(0f)] private float minWidth = 390f;
    [SerializeField, Min(0f)] private float maxWidth = 430f;

    // Horizontal chrome framing a title inside its card: the icon footprint, the
    // icon->title gap, and symmetric side padding that centres the icon+title
    // group. Purely structural (never a per-title value), so every measured title
    // is framed identically. 38 icon + 4 gap + 2 * 26 side padding.
    private const float RowHorizontalChrome = 94f;

    [Header("Animation (unscaled time)")]
    [SerializeField, Min(0.05f)] private float openCloseDuration = 0.19f;
    [SerializeField, Min(0.01f)] private float containerRevealDuration = 0.13f;
    [SerializeField, Min(0f)] private float rowStagger = 0.025f;
    [SerializeField, Min(0.01f)] private float rowFadeDuration = 0.09f;
    [SerializeField, Range(0.5f, 1f)] private float revealScaleFrom = 0.92f;
    [SerializeField, Range(0f, 0.15f)] private float popScale = 0.04f;
    [SerializeField, Min(0f)] private float slideOffsetX = 18f;
    [SerializeField, Range(0f, 0.12f)] private float scrimTargetAlpha = 0.08f;

    private PanelState state = PanelState.Closed;
    private Coroutine animationRoutine;
    private bool inputBlockHeld;
    private bool started;
    private bool listenersBound;
    private WhileYouWereAwayPopup offlinePopup;

    // Resolved lazily (same idiom as offlinePopup) so the shop and the quest panel
    // can be built by their own independent builders without this panel needing a
    // wired reference.
    private ShopPanelController shopPanel;
    private QuestPanelController questPanel;
    private CatJournalPanel catJournalPanel;
    private SettingsPanel settingsPanel;
    private RoomSelectorPanel roomSelectorPanel;
    private HomeEditModeController homeEditMode;
    private CatBreedShopPanel catBreedShopPanel;

    /// <summary>Row id authored by MainPanelBuilder for the quest entry point.</summary>
    private const string QuestsRowId = "QUESTS";
    private const string StoreRowId = "HOME STORE";
    private const string RoomsRowId = "ROOMS";
    private const string EditRoomRowId = "EDIT ROOM";
    private const string LegacyRoomsRowId = "INVENTORY";
    private const string JournalRowId = "CAT JOURNAL";
    private const string SettingsRowId = "SETTINGS";
    private const string MainMenuRowId = "MAIN MENU";

    // Deterministic animation clock in [0, openCloseDuration]. Every visual is a
    // pure function of this value, so reversing direction mid-animation simply
    // walks the clock the other way with no visual jump.
    private float animTime;

    // Resting anchored position of the list, captured once. The open animation
    // slides the list from listBasePos + slideOffsetX (to the right, tucked under
    // the button) to listBasePos.
    private Vector2 listBasePos;

    public bool IsOpen => state == PanelState.Open || state == PanelState.Opening;

    private void Awake()
    {
        ResolveSceneReferences();

        EnsureRoomsRowCompatibility();
        BindListeners();

        // Remember where the list rests before any animation offsets it.
        if (menuList != null)
            listBasePos = menuList.anchoredPosition;

        // Notification badges are UI scaffolding only; keep them hidden on boot
        // regardless of how the prefab was last saved.
        HideAllBadges();

        // Park everything hidden and non-interactive before the first frame so
        // nothing flashes or catches a raycast while closed.
        animTime = 0f;
        ApplyVisuals();
        SetListRaycasts(false);
        SetScrimRaycasts(false);

        if (menuButtonGroup != null)
        {
            menuButtonGroup.alpha = 0f;
            menuButtonGroup.interactable = false;
            menuButtonGroup.blocksRaycasts = false;
        }

        if (shopButtonGroup != null)
        {
            shopButtonGroup.alpha = 0f;
            shopButtonGroup.interactable = false;
            shopButtonGroup.blocksRaycasts = false;
        }

        if (catShopButtonGroup != null)
        {
            catShopButtonGroup.alpha = 0f;
            catShopButtonGroup.interactable = false;
            catShopButtonGroup.blocksRaycasts = false;
        }
        state = PanelState.Closed;
    }

    public void ResolveSceneReferences()
    {
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (roomSelectorPanel == null)
            roomSelectorPanel = FindAnyObjectByType<RoomSelectorPanel>(FindObjectsInactive.Include);
    }

    // Existing serialized UI scenes used the unimplemented INVENTORY prototype.
    // Re-label that slot at runtime so navigation works safely before the next
    // editor rebuild; MainPanelBuilder authors ROOMS directly for new prefabs.
    private void EnsureRoomsRowCompatibility()
    {
        if (rows == null)
            return;
        var retained = new System.Collections.Generic.List<MenuRow>();
        foreach (MenuRow row in rows)
        {
            if (row.id == EditRoomRowId)
            {
                if (row.group != null) row.group.gameObject.SetActive(false);
                else if (row.button != null) row.button.gameObject.SetActive(false);
            }
            else retained.Add(row);
        }
        rows = retained.ToArray();
        for (int i = 0; i < rows.Length; i++)
        {
            if (!string.Equals(rows[i].id, LegacyRoomsRowId, StringComparison.Ordinal))
                continue;
            MenuRow row = rows[i];
            row.id = RoomsRowId;
            if (row.label != null)
                row.label.text = RoomsRowId;
            rows[i] = row;
        }
    }

    private void Start()
    {
        ResolveSceneReferences();
        Canvas.ForceUpdateCanvases();
        ApplyListWidth();
        started = true;
    }

    private void OnEnable()
    {
        // Re-bind in case the object was toggled off/on after Awake.
        BindListeners();
        CatBreedService.Changed -= RefreshCatShopIcon;
        CatBreedService.Changed += RefreshCatShopIcon;
        RefreshCatShopIcon();
    }

    private void Update()
    {
        if (!started)
            return;

        bool modalActive = IsBlockingModalActive();

        // A critical modal appearing while we are open must never leave our list
        // floating over that popup. Snap shut and release only our own block;
        // the popup keeps its own owner-based input block.
        if ((state == PanelState.Open || state == PanelState.Opening) && modalActive)
            ForceHideImmediate();
        // Keep the list width correct across rotations / aspect changes while
        // it is on screen or animating.
        if (state != PanelState.Closed)
            ApplyListWidth();

        UpdateMenuButtonVisibility(modalActive);
    }

    // ----- Public request API (wired to buttons) -----

    public void RequestOpen()
    {
        // Reopening is allowed only from a settled or closing state; taps that
        // arrive while already opening/open are ignored so rapid taps cannot
        // desync the state machine.
        if (state != PanelState.Closed && state != PanelState.Closing)
            return;

        if (IsBlockingModalActive())
        {
            Debug.Log("MainPanelController: open request rejected; a blocking modal is active.");
            return;
        }

        AcquireInputBlock();
        ApplyListWidth();

        SetScrimRaycasts(true);
        SetListRaycasts(true);
        // The menu button stays interactive so a second tap closes the list;
        // UpdateMenuButtonVisibility keeps it enabled while !modal.

        state = PanelState.Opening;
        StartAnimation(opening: true);
    }

    public void RequestClose()
    {
        if (state != PanelState.Open && state != PanelState.Opening)
            return;

        // Rows stop taking taps immediately, but the scrim keeps blocking and
        // the input block stays held until the close animation finishes, so a
        // world touch cannot leak through the closing gap.
        if (menuListGroup != null)
            menuListGroup.interactable = false;

        state = PanelState.Closing;
        StartAnimation(opening: false);
    }

    public void Toggle()
    {
        if (IsOpen)
            RequestClose();
        else
            RequestOpen();
    }

    // ----- Row prototype callback -----

    private void OnRowSelected(string id)
    {
        if (string.Equals(id, StoreRowId, System.StringComparison.Ordinal))
        {
            OnShopSelected();
            return;
        }

        // The QUESTS row is the production entry point to the Quest Panel (CP3).
        // Every other row is still the CP1 visual prototype.
        if (string.Equals(id, QuestsRowId, System.StringComparison.Ordinal))
        {
            OnQuestsSelected();
            return;
        }

        if (string.Equals(id, RoomsRowId, System.StringComparison.Ordinal) ||
            string.Equals(id, LegacyRoomsRowId, System.StringComparison.Ordinal))
        {
            OnRoomsSelected();
            return;
        }

        if (string.Equals(id, EditRoomRowId, System.StringComparison.Ordinal))
        {
            OnEditRoomSelected();
            return;
        }

        if (string.Equals(id, JournalRowId, System.StringComparison.Ordinal))
        {
            OnJournalSelected();
            return;
        }

        if (string.Equals(id, SettingsRowId, System.StringComparison.Ordinal))
        {
            OnSettingsSelected();
            return;
        }

        if (string.Equals(id, MainMenuRowId, System.StringComparison.Ordinal))
        {
            OnMainMenuSelected();
            return;
        }

        // A tap is a discrete event (not per-frame), so this single log is safe
        // and never spams the console. No navigation, no system is touched.
        Debug.Log($"MainPanelController: menu row '{id}' selected (CP1 prototype; navigation not implemented).");
    }

    /// <summary>
    /// Forwards the tap to the Quest Panel, which lives on its own canvas and owns
    /// all of its own state, input block and progression reads. This panel only
    /// routes the tap — exactly like <see cref="OnShopSelected"/> does for the
    /// shop — and never touches quests, rewards or the wallet itself.
    /// </summary>
    private void OnQuestsSelected()
    {
        if (questPanel == null)
            questPanel = FindAnyObjectByType<QuestPanelController>(FindObjectsInactive.Include);

        if (questPanel == null)
        {
            Debug.Log(
                "MainPanelController: QUESTS row tapped but no QuestPanelController is present in " +
                "the scene. Build it via Tools > Cat Home > Build Quest Panel (CP3)."
            );
            return;
        }

        // The drop-down closes itself on the next frame: an open quest panel is a
        // blocking modal for this controller, so ForceHideImmediate runs there and
        // releases this panel's own input block.
        Debug.Log("MainPanelController: QUESTS row tapped; opening the quest panel.");
        questPanel.RequestOpen();
    }

    private void OnRoomsSelected()
    {
        if (roomSelectorPanel == null)
            roomSelectorPanel = FindAnyObjectByType<RoomSelectorPanel>(FindObjectsInactive.Include);
        if (roomSelectorPanel == null)
        {
            Debug.LogWarning(
                "MainPanelController: ROOMS needs RoomSelectorPanelCanvas. " +
                "Build it via Tools > Cat Home > Rooms > Build Room Selector.",
                this);
            return;
        }

        roomSelectorPanel.RequestOpen();
    }

    private void OnJournalSelected() => OnCatShopSelected();

    private void OnSettingsSelected()
    {
        if (settingsPanel == null)
            settingsPanel = FindAnyObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (settingsPanel == null)
        {
            Debug.LogWarning(
                "MainPanelController: SETTINGS needs SettingsPanelCanvas. " +
                "Build it via Tools > Cat Home > UI > Build Settings Panel.",
                this);
            return;
        }

        settingsPanel.RequestOpen();
    }

    private void OnMainMenuSelected()
    {
        ForceHideImmediate();
        CatHomeSaveSystem.SaveNow(true);

        TitleScreen title = FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        if (title == null)
        {
            Debug.LogWarning("MainPanelController: MAIN MENU needs TitleScreenCanvas.", this);
            return;
        }

        title.RequestShow();
    }

    private void OnEditRoomSelected()
    {
        ForceHideImmediate();
    }

    // ----- Shop button callback -----

    private void OnShopSelected()
    {
        // The shop lives on its own canvas above this one and owns all of its own
        // state; this panel only forwards the tap. If no shop exists in the scene
        // the button degrades to a single safe log, exactly as it did before.
        if (shopPanel == null)
            shopPanel = FindAnyObjectByType<ShopPanelController>(FindObjectsInactive.Include);

        if (shopPanel == null)
        {
            Debug.Log("MainPanelController: shop button tapped but no ShopPanelController is present in the scene.");
            return;
        }

        Debug.Log("MainPanelController: shop button tapped; toggling the shop panel.");
        shopPanel.Toggle();
    }

    private void OnCatShopSelected()
    {
        if (catBreedShopPanel == null)
            catBreedShopPanel = FindAnyObjectByType<CatBreedShopPanel>(
                FindObjectsInactive.Include);
        if (catBreedShopPanel == null)
        {
            Debug.LogWarning("MainPanelController: CAT SHOP panel is missing.", this);
            return;
        }

        if (IsOpen)
            ForceHideImmediate();
        catBreedShopPanel.RequestOpen();
    }

    private void RefreshCatShopIcon()
    {
        if (catShopButtonIcon == null)
            return;
        CatBreedCatalog.Entry selected = CatBreedService.SelectedEntry;
        if (selected != null && selected.Portrait != null)
            catShopButtonIcon.sprite = selected.Portrait;
    }

    // ----- Notification badge UI support (no gameplay binding in CP1) -----

    /// <summary>
    /// Shows or hides a row's small notification badge and sets its number. This
    /// is pure UI plumbing: it is not wired to any system in CP1, and exists so a
    /// future feature can surface an unread count without touching layout. A
    /// <paramref name="count"/> of 0 or less hides the badge; otherwise it is
    /// clamped to 1..99.
    /// </summary>
    public void SetRowBadge(string id, int count)
    {
        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i].id != id)
                continue;

            bool show = count > 0;
            if (rows[i].badge != null)
                rows[i].badge.SetActive(show);
            if (show && rows[i].badgeText != null)
                rows[i].badgeText.text = Mathf.Clamp(count, 1, 99).ToString();
            return;
        }
    }

    /// <summary>Convenience wrapper that hides a single row's badge.</summary>
    public void ClearRowBadge(string id) => SetRowBadge(id, 0);

    private void HideAllBadges()
    {
        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
            if (rows[i].badge != null)
                rows[i].badge.SetActive(false);
    }

    // ----- Animation -----

    private void StartAnimation(bool opening)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimateRoutine(opening));
    }

    private IEnumerator AnimateRoutine(bool opening)
    {
        float target = opening ? openCloseDuration : 0f;

        while (!Mathf.Approximately(animTime, target))
        {
            animTime = Mathf.MoveTowards(animTime, target, Time.unscaledDeltaTime);
            ApplyVisuals();
            yield return null;
        }

        animTime = target;
        ApplyVisuals();
        animationRoutine = null;

        if (opening)
            state = PanelState.Open;
        else
            OnClosedComplete();
    }

    /// <summary>
    /// Maps the deterministic <see cref="animTime"/> clock onto container fade,
    /// scale, scrim alpha and per-row staggered fade. Pure function of animTime.
    /// </summary>
    private void ApplyVisuals()
    {
        float containerT = Smooth(Clamp01(animTime / containerRevealDuration));

        if (menuListGroup != null)
            menuListGroup.alpha = containerT;

        if (menuList != null)
        {
            // Uniform scale grows revealScaleFrom -> 1 with a subtle overshoot
            // hump (popScale, peaking mid-reveal) for a soft "pop".
            float scale = Mathf.Lerp(revealScaleFrom, 1f, containerT) +
                          popScale * Mathf.Sin(containerT * Mathf.PI);
            menuList.localScale = new Vector3(scale, scale, 1f);

            // Slide in from ~slideOffsetX to the right (tucked under the button)
            // to the resting position as the container reveals.
            float slide = slideOffsetX * (1f - containerT);
            menuList.anchoredPosition = new Vector2(listBasePos.x + slide, listBasePos.y);
        }

        if (scrimGroup != null)
            scrimGroup.alpha = Mathf.Lerp(0f, scrimTargetAlpha, containerT);

        if (rows != null)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                CanvasGroup group = rows[i].group;
                if (group == null)
                    continue;
                float rowStart = i * rowStagger;
                float rowT = Smooth(Clamp01((animTime - rowStart) / rowFadeDuration));
                group.alpha = rowT;
            }
        }
    }

    private void OnClosedComplete()
    {
        state = PanelState.Closed;
        SetScrimRaycasts(false);
        SetListRaycasts(false);
        ReleaseInputBlock();
    }

    /// <summary>
    /// Instantly forces the menu shut without animation. Used when a critical
    /// modal appears; releases only our own input block.
    /// </summary>
    private void ForceHideImmediate()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        animTime = 0f;
        ApplyVisuals();
        state = PanelState.Closed;
        SetScrimRaycasts(false);
        SetListRaycasts(false);
        ReleaseInputBlock();
    }

    // ----- Responsive width -----

    private void ApplyListWidth()
    {
        if (menuList == null)
            return;

        float reference = safeArea != null ? safeArea.rect.width : menuList.rect.width;
        if (reference <= 1f)
            return;

        float lo = Mathf.Min(minWidth, maxWidth);
        float hi = Mathf.Max(minWidth, maxWidth);
        float width = Mathf.Clamp(reference * widthFraction, lo, hi);

        // Never let the list exceed the safe area (matters on narrow 4:3 where
        // the min clamp would otherwise push it past the left edge).
        width = Mathf.Min(width, reference);

        menuList.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        // The list width is only the upper bound now; each card sizes to its own
        // title within it, so update the per-row widths whenever the cap changes.
        ApplyRowWidths(width);
    }

    /// <summary>
    /// Gives every card the same width — the one the longest title needs — so the
    /// cards read as a uniform stack, and gives every title the same title-area
    /// width so the four icons share one left x and the four titles share one start
    /// x. The widest title is found at runtime from each label's measured
    /// (unwrapped) TMP preferredWidth, so no title has a hard-coded width and a
    /// longer title added later automatically becomes the new maximum that every
    /// card and title area follows. The shared card width is
    /// <c>chrome + widest title</c>, capped at <paramref name="capWidth"/> — the
    /// current menu (list) width — so it never spills past the menu; the title area
    /// is <c>cardWidth - chrome</c>, which equals the widest title when uncapped.
    /// Each row's icon+title group is centred within the card by the builder's
    /// layout, so equal title areas keep the icons and titles column-aligned with
    /// balanced side padding, and the list's UpperRight alignment keeps the cards
    /// on the right edge.
    /// </summary>
    private void ApplyRowWidths(float capWidth)
    {
        if (rows == null || rows.Length == 0)
            return;

        // Longest title wins. NoWrap labels report their intrinsic width here,
        // independent of the row's current size.
        float widestTitle = 0f;
        for (int i = 0; i < rows.Length; i++)
        {
            TMP_Text label = rows[i].label;
            if (label != null)
                widestTitle = Mathf.Max(widestTitle, label.preferredWidth);
        }

        if (widestTitle <= 0f)
            return;

        float cardWidth = Mathf.Min(RowHorizontalChrome + widestTitle, capWidth);
        // The shared title area is what remains after the fixed chrome (icon +
        // gap + symmetric side padding). Pinning every label to it makes the icons
        // and titles line up; when uncapped this is exactly the widest title.
        float titleArea = Mathf.Max(0f, cardWidth - RowHorizontalChrome);

        for (int i = 0; i < rows.Length; i++)
        {
            LayoutElement layout = rows[i].layout;
            if (layout != null && !Mathf.Approximately(layout.preferredWidth, cardWidth))
                layout.preferredWidth = cardWidth;

            LayoutElement labelLayout = rows[i].labelLayout;
            if (labelLayout != null && !Mathf.Approximately(labelLayout.preferredWidth, titleArea))
            {
                labelLayout.preferredWidth = titleArea;
                labelLayout.minWidth = titleArea;
            }
        }
    }

    // ----- Raycast gating -----

    private void SetListRaycasts(bool on)
    {
        if (menuListGroup != null)
            menuListGroup.blocksRaycasts = on;
        if (menuListGroup != null)
            menuListGroup.interactable = on;
    }

    private void SetScrimRaycasts(bool on)
    {
        if (scrimGroup != null)
        {
            scrimGroup.blocksRaycasts = on;
            scrimGroup.interactable = on;
        }
    }

    // ----- Menu button -----

    private void UpdateMenuButtonVisibility(bool modalActive)
    {
        // The button stays visible and interactive whenever no blocking modal is
        // active, both closed and open: it is the drop-down's anchor and doubles
        // as the toggle affordance ("tap again to close"). It is the only element
        // that takes raycasts while the menu is closed.
        bool show = !modalActive;
        SetMenuButtonInteractive(show);
        if (menuButtonGroup != null)
            menuButtonGroup.alpha = show ? 1f : 0f;

        // The shop button shares the hamburger's visibility gate (hidden during
        // blocking modals / onboarding, which now includes an open shop) but not
        // its toggle role — tapping it opens the shop, never the drop-down list.
        if (shopButtonGroup != null)
        {
            shopButtonGroup.alpha = show ? 1f : 0f;
            shopButtonGroup.interactable = show;
            shopButtonGroup.blocksRaycasts = show;
        }

        if (catShopButtonGroup != null)
        {
            catShopButtonGroup.alpha = show ? 1f : 0f;
            catShopButtonGroup.interactable = show;
            catShopButtonGroup.blocksRaycasts = show;
        }
    }

    private void SetMenuButtonInteractive(bool interactive)
    {
        if (menuButtonGroup == null)
            return;
        menuButtonGroup.interactable = interactive;
        menuButtonGroup.blocksRaycasts = interactive;
    }

    // ----- Modal coordination -----

    private bool IsBlockingModalActive()
    {
        if (HomeUiFlow.IsHomeControlBlocked) return true;
        if (GamesHubPanel.IsAnyOpen || LeaderboardPanel.IsAnyOpen || SettingsPanel.IsAnyOpen || PrivacyDataPanel.IsAnyOpen)
            return true;
        if (TitleScreen.IsShowing)
            return true;

        // Onboarding not finished is treated as blocking: the menu button stays
        // hidden until the player has completed onboarding.
        if (!PetTutorialHint.IsOnboardingCompleted)
            return true;

        if (OnboardingCelebrationView.IsAnyOpen)
            return true;

        if (HomeLevelUpCelebrationView.IsAnyOpen)
            return true;

        if (CollectionCompleteCelebrationView.IsAnyOpen)
            return true;

        // The shop covers this canvas entirely, so while it is open (including its
        // close animation) the top bar and the drop-down list stay out of the way
        // instead of floating underneath it.
        if (ShopPanelController.IsAnyOpen)
            return true;

        // The quest panel sorts above this canvas for the same reason, and this is
        // also what closes the drop-down after the QUESTS row was tapped.
        if (QuestPanelController.IsAnyOpen)
            return true;

        if (RoomSelectorPanel.IsAnyOpen)
            return true;

        if (HomeEditModeController.IsAnyOpen)
            return true;

        if (CatBreedShopPanel.IsAnyOpen)
            return true;

        if (offlinePopup == null)
            offlinePopup = FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);

        return offlinePopup != null && offlinePopup.IsOpen;
    }

    // ----- Input block -----

    private void AcquireInputBlock()
    {
        if (inputBlockHeld)
            return;

        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>();

        if (catMovement != null)
        {
            catMovement.AcquireInputBlock(this);
            inputBlockHeld = true;
        }
    }

    private void ReleaseInputBlock()
    {
        if (!inputBlockHeld)
            return;

        if (catMovement != null)
            catMovement.ReleaseInputBlock(this);
        inputBlockHeld = false;
    }

    // ----- Listener lifecycle -----

    private void BindListeners()
    {
        if (listenersBound)
            return;

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(Toggle);
            menuButton.onClick.AddListener(Toggle);
        }

        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(OnShopSelected);
            shopButton.onClick.AddListener(OnShopSelected);
        }

        if (catShopButton != null)
        {
            catShopButton.onClick.RemoveListener(OnCatShopSelected);
            catShopButton.onClick.AddListener(OnCatShopSelected);
        }

        if (scrimButton != null)
        {
            scrimButton.onClick.RemoveListener(OnScrimSelected);
            scrimButton.onClick.AddListener(OnScrimSelected);
        }

        if (rows != null)
        {
            foreach (MenuRow row in rows)
            {
                if (row.button == null)
                    continue;
                string id = row.id;
                row.button.onClick.AddListener(() => OnRowSelected(id));
            }
        }

        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (menuButton != null)
            menuButton.onClick.RemoveListener(Toggle);
        if (shopButton != null)
            shopButton.onClick.RemoveListener(OnShopSelected);
        if (catShopButton != null)
            catShopButton.onClick.RemoveListener(OnCatShopSelected);
        if (scrimButton != null)
            scrimButton.onClick.RemoveListener(OnScrimSelected);

        // Row listeners are lambdas; RemoveAllListeners clears them without
        // touching persistent (Editor-authored) calls, of which rows have none.
        if (rows != null)
        {
            foreach (MenuRow row in rows)
                if (row.button != null)
                    row.button.onClick.RemoveAllListeners();
        }

        listenersBound = false;
    }

    private void OnDisable()
    {
        CatBreedService.Changed -= RefreshCatShopIcon;
        // Never leave the cat's input blocked because the menu was disabled
        // mid-animation.
        ReleaseInputBlock();
    }

    private void OnDestroy()
    {
        ReleaseInputBlock();
        UnbindListeners();
    }

    private static float Clamp01(float v) => Mathf.Clamp01(v);

    private void OnScrimSelected()
    {
        RequestClose();
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
