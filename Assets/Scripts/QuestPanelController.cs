using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using QuestState = CatHome.Quests.QuestState;

/// <summary>
/// CP3 production Quest Panel: the first player-facing view of the active level's
/// quests and the first player-facing Claim button. It replaces the development
/// overlay as the way a player claims a reward; the overlay itself stays as a
/// development-only tool.
///
/// The controller is a pure view over <see cref="ProgressionService"/>. It reads
/// through one read-only query (<see cref="ProgressionService.CaptureActiveChapterQuests"/>)
/// and writes through exactly one call (<see cref="ProgressionService.TryClaimQuest"/>).
/// It owns no economy: it never touches the wallet, never marks a quest completed
/// or claimed, and never advances a level. Opening it can not change progression.
///
/// Open/close, the fade + scale reveal, responsive sizing inside the mobile safe
/// area and the input block mirror <see cref="ShopPanelController"/> so the panel
/// behaves exactly like the shop the player already knows. It is opened by the
/// existing hamburger menu's QUESTS row.
///
/// The hierarchy is authored by <c>QuestPanelBuilder</c> (Editor) and wired into
/// the serialized fields below. References are also resolved defensively at
/// runtime, so a partially wired object degrades to "stays closed" instead of
/// throwing.
///
/// Coexistence: this lives on its own canvas (sortingOrder 108) above the main
/// panel canvas (100) and below the shop canvas (110). While it is open,
/// <see cref="IsAnyOpen"/> tells <see cref="MainPanelController"/> to treat it as
/// a blocking modal, so the top bar and its drop-down never float over it.
/// </summary>
[DisallowMultipleComponent]
public sealed class QuestPanelController : MonoBehaviour
{
    private enum PanelState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    /// <summary>
    /// One authored quest row. The pool is fixed and built by QuestPanelBuilder;
    /// a refresh only fills the rows the active level needs and deactivates the
    /// rest, so no UI is instantiated or destroyed at runtime.
    /// </summary>
    [System.Serializable]
    private struct QuestRow
    {
        public GameObject root;
        public CanvasGroup group;
        public TMP_Text title;
        public TMP_Text description;
        public TMP_Text progress;
        public TMP_Text rewards;
        public TMP_Text status;
        public GameObject claimRoot;
        public Button claimButton;
    }

    [Header("Input")]
    [SerializeField] private CatMovement catMovement;

    [Header("Raycast gate (whole canvas)")]
    [SerializeField] private CanvasGroup rootGroup;

    [Header("Scrim (dim + outside-tap catcher)")]
    [SerializeField] private CanvasGroup scrimGroup;
    [SerializeField] private Button scrimButton;

    [Header("Panel")]
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button closeButton;

    [Header("Content")]
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private Button chapterTab;
    [SerializeField] private Button dailyTab;
    private bool showDaily;
    [SerializeField] private TMP_Text walletLabel;
    [SerializeField] private GameObject messageRoot;
    [SerializeField] private TMP_Text messageLabel;
    [SerializeField] private GameObject scrollRoot;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private QuestRow[] rows = new QuestRow[0];

    [Header("Responsive size (canvas units, 1920x1080 reference)")]
    [SerializeField, Range(0.2f, 0.95f)] private float widthFraction = 0.64f;
    [SerializeField, Min(0f)] private float minWidth = 1040f;
    [SerializeField, Min(0f)] private float maxWidth = 1240f;
    // Breathing room kept between the panel and the safe-area edges. Also the
    // budget the fit-scale uses on short screens.
    [SerializeField, Min(0f)] private float safeAreaMargin = 32f;

    [Header("Animation (unscaled time)")]
    [SerializeField, Min(0.05f)] private float openCloseDuration = 0.22f;
    [SerializeField, Min(0.01f)] private float panelRevealDuration = 0.15f;
    [SerializeField, Min(0f)] private float rowStagger = 0.035f;
    [SerializeField, Min(0.01f)] private float rowFadeDuration = 0.1f;
    [SerializeField, Range(0.5f, 1f)] private float revealScaleFrom = 0.9f;
    [SerializeField, Range(0f, 0.15f)] private float popScale = 0.035f;
    [SerializeField, Range(0f, 1f)] private float scrimTargetAlpha = 0.55f;

    // Runtime rows use the shared palette; old serialized row colours must not
    // restore the previous cream / brown treatment when the snapshot refreshes.
    private static readonly Color activeTextColor = StorybookScreenStyle.Ink;
    private static readonly Color mutedTextColor = StorybookScreenStyle.Muted;
    private static readonly Color readyTextColor = StorybookQuestPresentation.PositiveText;
    private static readonly Color claimedTextColor = StorybookQuestPresentation.PositiveText;

    // Player-facing state strings. Kept in one place so the presentation of the
    // authoritative QuestState never drifts between rows.
    private const string LockedText = "LOCKED";
    private const string ActiveText = "IN PROGRESS";
    private const string ClaimedText = "CLAIMED";
    private const string AllChaptersCompletedText = "All quest chapters completed";
    private const string UnavailableText =
        "Quests are not available right now.";
    private const string EmptyLevelText =
        "This level has no quests configured.";

    private PanelState state = PanelState.Closed;
    private Coroutine animationRoutine;
    private bool inputBlockHeld;
    private bool listenersBound;
    private bool subscribedToProgression;
    private bool started;
    private WhileYouWereAwayPopup offlinePopup;

    // Deterministic animation clock in [0, openCloseDuration]. Every visual is a
    // pure function of this value, so reversing direction mid-animation simply
    // walks the clock the other way with no visual jump.
    private float animTime;

    // Uniform down-scale applied when the authored panel height does not fit the
    // current safe area (short screens / landscape). 1 when it fits.
    private float fitScale = 1f;

    // Reused across refreshes so a repaint allocates nothing.
    private readonly List<QuestSnapshot> snapshots = new List<QuestSnapshot>(8);
    private readonly StringBuilder rewardBuilder = new StringBuilder(48);

    // Quest id currently shown by each row, parallel to <see cref="rows"/>. The
    // claim callback resolves the id from here rather than from a captured
    // lambda variable, so a row that was reused for a different quest after a
    // refresh can never claim the quest it used to show.
    private string[] rowQuestIds = new string[0];

    // True only for the duration of one TryClaimQuest call. The claim raises
    // several StateChanged events (coins, bond XP, level) from inside that call;
    // this collapses them into the single refresh the claim path runs afterwards.
    private bool claimInProgress;

    /// <summary>
    /// True from the moment an open is requested until the close animation has
    /// fully finished. <see cref="MainPanelController"/> reads this to treat the
    /// quest panel as a blocking modal, so the top bar and its drop-down stay out
    /// of the way for the whole transition instead of flashing back in mid-close.
    /// </summary>
    public static bool IsAnyOpen { get; private set; }

    public bool IsOpen => state == PanelState.Open || state == PanelState.Opening;

    private void Awake()
    {
        StorybookQuestPresentation.Apply(transform);
        PremiumScrollInput.Ensure(scrollRect);
        ResolveSceneReferences();

        if (rootGroup == null)
            rootGroup = GetComponent<CanvasGroup>();

        EnsureRowIdBuffer();
        BindListeners();

        // The authored rows carry placeholder copy; they stay hidden until the
        // first Refresh (which always runs before the panel becomes visible), so
        // no placeholder quest can ever reach the screen.
        HideAllRows();

        // Park everything hidden and non-interactive before the first frame so
        // nothing flashes or catches a raycast while closed.
        animTime = 0f;
        ApplyVisuals();
        SetRaycasts(false);
        state = PanelState.Closed;
        IsAnyOpen = false;
    }

    public void ResolveSceneReferences()
    {
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        ApplyResponsiveLayout();
        ApplyVisuals();
        started = true;
    }

    private void OnEnable()
    {
        // Re-bind in case the object was toggled off/on after Awake.
        BindListeners();
        SubscribeToProgression();
    }

    private void Update()
    {
        if (!started)
            return;

        // A critical modal appearing while we are open must never leave the panel
        // floating over that popup. Snap shut and release only our own block.
        if (IsOpen && IsBlockingModalActive())
        {
            ForceHideImmediate();
            return;
        }

        // Keep the panel correctly sized across rotations / aspect changes while
        // it is on screen or animating.
        if (state != PanelState.Closed)
        {
            ApplyResponsiveLayout();
            ApplyVisuals();
        }
    }

    // ----- Public request API (wired to buttons / MainPanelController) -----

    public void RequestOpen()
    {
        // Reopening is allowed only from a settled or closing state; taps that
        // arrive while already opening/open are ignored so rapid taps cannot
        // desync the state machine.
        if (state != PanelState.Closed && state != PanelState.Closing)
            return;

        if (IsBlockingModalActive())
        {
            Debug.Log("QuestPanelController: open request rejected; a blocking modal is active.");
            return;
        }

        IsAnyOpen = true;
        AcquireInputBlock();
        ApplyResponsiveLayout();
        SetRaycasts(true);

        // Painted before the reveal starts so the first visible frame already
        // shows the current level, progress and states.
        Refresh();
        ResetScrollToTop();

        state = PanelState.Opening;
        StartAnimation(opening: true);
    }

    public void RequestClose()
    {
        if (state != PanelState.Open && state != PanelState.Opening)
            return;

        // Buttons stop taking taps immediately, but the canvas keeps blocking
        // raycasts and the input block stays held until the close animation
        // finishes, so a world touch cannot leak through the closing gap.
        if (rootGroup != null)
            rootGroup.interactable = false;

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

    /// <summary>
    /// Puts the list back at the first quest whenever the panel is opened. The
    /// content fitter has not rebuilt yet at that point, so the canvas is flushed
    /// first; otherwise the ScrollRect would normalize against the previous
    /// level's content height.
    /// </summary>
    private void ResetScrollToTop()
    {
        if (scrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    // ----- Reading progression -----

    /// <summary>
    /// Repaints the whole panel from a fresh read-only snapshot. Called on open,
    /// on every progression change while open, and after every claim attempt —
    /// a claim can advance the player to a different level, so nothing less than
    /// a full repaint is correct.
    /// </summary>
    public void Refresh()
    {
        QuestBoardStatus status = ProgressionService.CaptureActiveChapterQuests(
            snapshots,
            out string levelName
        );

        UpdateWalletLabel();

        bool daily=showDaily || status==QuestBoardStatus.AllChaptersCompleted;
        PaintTab(chapterTab,!daily); PaintTab(dailyTab,daily);
        if(daily)
        {
            snapshots.Clear(); DailyRetentionService.CaptureDailyQuests(snapshots);
            SetLevelHeading(GameLanguageService.Text("quests.refresh_daily"));
        }
        else
        {
            if(status==QuestBoardStatus.Unavailable) { ShowMessage(GameLanguageService.Text("quests.unavailable")); return; }
            SetLevelHeading(GameLanguageService.Format("quests.chapter",ProgressionService.CurrentChapterNumber));
        }

        if (snapshots.Count == 0)
        {
            ShowMessage(EmptyLevelText);
            return;
        }

        ShowRows();
    }

    private void SelectDaily(bool value) { showDaily=value; Refresh(); ResetScrollToTop(); }
    private static void PaintTab(Button button,bool selected)
    {
        StorybookScreenStyle.Action(button, !selected, selected);
    }

    private static string FormatChapterHeading(int chapterNumber, string chapterName)
    {
        string prefix = "CHAPTER " + chapterNumber.ToString(CultureInfo.InvariantCulture);

        // Plain ASCII separator on purpose: a typographic bullet is not guaranteed
        // to exist in the Fredoka SDF atlas and would log a missing-glyph warning.
        return string.IsNullOrWhiteSpace(chapterName) ? prefix : prefix + " - " + chapterName;
    }

    private void SetLevelHeading(string text)
    {
        if (levelLabel != null)
            levelLabel.text = text;
    }

    private void UpdateWalletLabel()
    {
        if (walletLabel == null)
            return;

        walletLabel.text = GameLanguageService.Format("quests.wallet",Format(ProgressionService.Coins),Format(ProgressionService.BondXp),Format(ProgressionService.Diamonds));
    }

    // Grouped with an invariant separator so a large amount reads the same on
    // every device regardless of the player's locale (mirrors CurrencyHud).
    private static string Format(long value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>
    /// Shows a single centred message instead of the quest list, with every row
    /// (and therefore every Claim button) deactivated.
    /// </summary>
    private void ShowMessage(string text)
    {
        HideAllRows();

        if (scrollRoot != null)
            scrollRoot.SetActive(false);
        if (messageRoot != null)
            messageRoot.SetActive(true);
        if (messageLabel != null)
            messageLabel.text = text;
    }

    private void ShowRows()
    {
        if (messageRoot != null)
            messageRoot.SetActive(false);
        if (scrollRoot != null)
            scrollRoot.SetActive(true);

        EnsureRowIdBuffer();

        int rowCount = rows != null ? rows.Length : 0;
        int visible = Mathf.Min(snapshots.Count, rowCount);

        if (snapshots.Count > rowCount)
        {
            // A discrete, once-per-refresh log (never per frame). The authored
            // pool covers every level in the current config with room to spare;
            // this only fires if a much larger level is added later.
            Debug.LogWarning(
                $"QuestPanelController: level has {snapshots.Count} quests but only {rowCount} " +
                "authored rows. Rebuild the panel with a larger row pool via " +
                "Tools > Cat Home > Build Quest Panel (CP3)."
            );
        }

        for (int i = 0; i < visible; i++)
            ApplyRow(i, snapshots[i]);

        for (int i = visible; i < rowCount; i++)
            HideRow(i);
    }

    private void ApplyRow(int index, QuestSnapshot snapshot)
    {
        QuestRow row = rows[index];
        rowQuestIds[index] = snapshot.QuestId;

        if (row.root != null)
        {
            row.root.SetActive(true);
            // Stretched faces in the dormant authored pool can have zero width
            // during editor styling. Resolve the actual row face at refresh so
            // existing scenes and newly built rows receive the same finish.
            Transform face = row.root.transform.Find("Face");
            if (face != null)
                StorybookScreenStyle.Card(face.GetComponent<LowPolyPanelGraphic>(), 22f);
        }
        StorybookScreenStyle.Action(row.claimButton);

        // A quest authored without a title falls back to its description, never to
        // the raw quest id: ids are data, not player-facing copy.
        string title = !string.IsNullOrWhiteSpace(snapshot.Title)
            ? snapshot.Title
            : (!string.IsNullOrWhiteSpace(snapshot.Description) ? snapshot.Description : "Quest");

        bool locked = snapshot.State == QuestState.Locked;
        Color bodyColor = locked ? mutedTextColor : activeTextColor;

        SetText(row.title, title, bodyColor);
        SetText(row.description, snapshot.Description, bodyColor);
        SetText(
            row.progress,
            snapshot.Count.ToString(CultureInfo.InvariantCulture) + "/" +
            snapshot.RequiredCount.ToString(CultureInfo.InvariantCulture),
            bodyColor
        );
        SetText(row.rewards, BuildRewardText(snapshot), bodyColor);

        // The authoritative lifecycle state decides the row, never the legacy
        // completed mirror: it is the only thing that separates Completed (reward
        // still waiting) from Claimed (reward already paid).
        switch (snapshot.State)
        {
            case QuestState.Completed:
                // The single state in which TryClaimQuest can succeed.
                SetText(row.status, string.Empty, readyTextColor);
                SetClaimVisible(row, true, true);
                break;

            case QuestState.Claimed:
                SetText(row.status, GameLanguageService.Text("quests.claimed"), claimedTextColor);
                SetClaimVisible(row, false, false);
                break;

            case QuestState.Locked:
                SetText(row.status, GameLanguageService.Text("quests.locked"), mutedTextColor);
                SetClaimVisible(row, false, false);
                break;

            default:
                SetText(row.status, GameLanguageService.Text("quests.active"), bodyColor);
                SetClaimVisible(row, false, false);
                break;
        }
        StorybookQuestPresentation.ApplyRow(row.root == null ? null : row.root.transform,
            snapshot, row.progress, row.title, row.description, row.rewards);
    }

    private string BuildRewardText(QuestSnapshot snapshot)
    {
        if (!snapshot.HasAnyReward)
            return string.Empty;

        rewardBuilder.Length = 0;

        // Zero rewards are omitted entirely so a row never advertises "+0 coins".
        if (snapshot.RewardCoins > 0)
            AppendReward(snapshot.RewardCoins, GameLanguageService.Text("currency.coins"));
        if (snapshot.RewardBondXp > 0)
            AppendReward(snapshot.RewardBondXp, GameLanguageService.Text("currency.bond"));
        if (snapshot.RewardDiamonds > 0)
            AppendReward(snapshot.RewardDiamonds, GameLanguageService.Text("diamonds.units"));

        return rewardBuilder.ToString();
    }

    private void AppendReward(long amount, string label)
    {
        if (rewardBuilder.Length > 0)
            rewardBuilder.Append("   ");

        rewardBuilder.Append('+').Append(Format(amount)).Append(' ').Append(label);
    }

    private static void SetText(TMP_Text text, string value, Color color)
    {
        if (text == null)
            return;

        text.text = value ?? string.Empty;
        text.color = color;
    }

    private static void SetClaimVisible(QuestRow row, bool visible, bool interactable)
    {
        if (row.claimRoot != null)
            row.claimRoot.SetActive(visible);

        if (row.claimButton != null)
            row.claimButton.interactable = visible && interactable;
    }

    private void HideAllRows()
    {
        int rowCount = rows != null ? rows.Length : 0;
        for (int i = 0; i < rowCount; i++)
            HideRow(i);
    }

    private void HideRow(int index)
    {
        EnsureRowIdBuffer();
        rowQuestIds[index] = null;

        QuestRow row = rows[index];

        // The button is disabled as well as hidden: a hidden-but-enabled button
        // could still be driven by a queued event in the same frame.
        SetClaimVisible(row, false, false);

        if (row.root != null)
            row.root.SetActive(false);
    }

    private void EnsureRowIdBuffer()
    {
        int rowCount = rows != null ? rows.Length : 0;
        if (rowQuestIds == null || rowQuestIds.Length != rowCount)
            rowQuestIds = new string[rowCount];
    }

    // ----- Claim (the only write path) -----

    /// <summary>
    /// The production claim trigger. It calls the single reward path,
    /// <see cref="ProgressionService.TryClaimQuest"/>, and does nothing else: no
    /// wallet call, no state flip, no level advancement is reproduced here.
    ///
    /// Repeated taps are stopped three times over: the button is disabled before
    /// the service call (so a second queued tap in the same frame is rejected by
    /// the Button itself), <see cref="claimInProgress"/> rejects a reentrant call,
    /// and TryClaimQuest itself succeeds only for a Completed quest, so even a tap
    /// that somehow got through would pay nothing.
    /// </summary>
    private void OnClaimPressed(int rowIndex)
    {
        if (claimInProgress)
            return;

        EnsureRowIdBuffer();
        if (rowIndex < 0 || rowIndex >= rowQuestIds.Length)
            return;

        string questId = rowQuestIds[rowIndex];
        if (string.IsNullOrEmpty(questId))
            return;

        // Disabled first, before anything can be granted.
        SetClaimVisible(rows[rowIndex], true, false);

        bool granted;
        claimInProgress = true;
        try
        {
            granted = ProgressionService.TryClaimQuest(questId);
        }
        finally
        {
            claimInProgress = false;
        }

        if (!granted)
        {
            // A rejected claim is not an error: the quest was already claimed, is
            // not complete yet, or no longer exists. Nothing was changed, and the
            // refresh below restores the row's real state.
            Debug.Log(
                $"QuestPanelController: quest '{questId}' was not claimable. Nothing changed."
            );
        }

        // Always a full repaint, never a local row tweak: a successful claim may
        // have advanced the player to an entirely different level.
        Refresh();
    }

    // ----- Live refresh -----

    private void SubscribeToProgression()
    {
        if (subscribedToProgression)
            return;

        ProgressionService.StateChanged += OnProgressionStateChanged;
        subscribedToProgression = true;
    }

    private void UnsubscribeFromProgression()
    {
        if (!subscribedToProgression)
            return;

        ProgressionService.StateChanged -= OnProgressionStateChanged;
        subscribedToProgression = false;
    }

    /// <summary>
    /// Event-driven repaint: quest progress, completion, claims, level-ups and a
    /// loaded save all arrive here. Nothing is polled per frame.
    /// </summary>
    private void OnProgressionStateChanged()
    {
        // One claim raises several changes from inside TryClaimQuest; the claim
        // path repaints once when that call returns.
        if (claimInProgress)
            return;

        // Nothing is visible while closed, and RequestOpen always repaints before
        // the reveal, so a closed panel simply ignores the change.
        if (!IsOpen)
            return;

        Refresh();
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
    /// Maps the deterministic <see cref="animTime"/> clock onto scrim alpha,
    /// panel fade, panel scale and per-row staggered fade. Pure function of
    /// animTime (and the current <see cref="fitScale"/>).
    /// </summary>
    private void ApplyVisuals()
    {
        float revealT = Smooth(Clamp01(animTime / panelRevealDuration));

        if (scrimGroup != null)
            scrimGroup.alpha = Mathf.Lerp(0f, scrimTargetAlpha, revealT);

        if (panelGroup != null)
            panelGroup.alpha = revealT;

        if (panel != null)
        {
            // Uniform scale grows revealScaleFrom -> 1 with a subtle overshoot
            // hump (popScale, peaking mid-reveal) for a soft "pop", then the
            // safe-area fit-scale is folded in so short screens simply shrink.
            float scale = Mathf.Lerp(revealScaleFrom, 1f, revealT) +
                          popScale * Mathf.Sin(revealT * Mathf.PI);
            scale *= fitScale;
            panel.localScale = new Vector3(scale, scale, 1f);
        }

        if (rows == null)
            return;

        for (int i = 0; i < rows.Length; i++)
        {
            CanvasGroup group = rows[i].group;
            if (group == null)
                continue;

            float rowStart = i * rowStagger;
            group.alpha = Smooth(Clamp01((animTime - rowStart) / rowFadeDuration));
        }
    }

    private void OnClosedComplete()
    {
        state = PanelState.Closed;
        IsAnyOpen = false;
        SetRaycasts(false);
        ReleaseInputBlock();
    }

    /// <summary>
    /// Instantly forces the panel shut without animation. Used when a critical
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
        OnClosedComplete();
    }

    // ----- Responsive layout -----

    /// <summary>
    /// Sizes the panel from the safe area: the width is a fraction of the safe
    /// width clamped to [minWidth, maxWidth] and never wider than the safe area
    /// minus its margins, and the authored height is folded into
    /// <see cref="fitScale"/> so a panel taller than the safe area shrinks
    /// uniformly instead of spilling under a notch or a home indicator. Because
    /// the shrink is uniform, the close and Claim buttons stay on screen and keep
    /// their proportions on every aspect ratio.
    /// </summary>
    private void ApplyResponsiveLayout()
    {
        if (panel == null)
            return;

        float availableWidth = safeArea != null ? safeArea.rect.width : panel.rect.width;
        float availableHeight = safeArea != null ? safeArea.rect.height : panel.rect.height;
        if (availableWidth <= 1f || availableHeight <= 1f)
            return;

        float lo = Mathf.Min(minWidth, maxWidth);
        float hi = Mathf.Max(minWidth, maxWidth);
        float width = Mathf.Clamp(availableWidth * widthFraction, lo, hi);

        // Never let the panel exceed the safe area (matters on narrow phones
        // where the min clamp would otherwise push it past both edges).
        width = Mathf.Min(width, Mathf.Max(1f, availableWidth - safeAreaMargin * 2f));
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        float authoredHeight = panel.rect.height;
        float heightBudget = Mathf.Max(1f, availableHeight - safeAreaMargin * 2f);
        fitScale = authoredHeight > heightBudget ? heightBudget / authoredHeight : 1f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        widthFraction = 0.64f;
        minWidth = 1040f;
        maxWidth = 1240f;
        safeAreaMargin = 32f;
    }
#endif

    // ----- Raycast gating -----

    private void SetRaycasts(bool on)
    {
        if (rootGroup != null)
        {
            rootGroup.blocksRaycasts = on;
            rootGroup.interactable = on;
        }
    }

    // ----- Modal coordination (mirrors the shop's gate) -----

    private bool IsBlockingModalActive()
    {
        // Onboarding not finished is treated as blocking: the menu row that opens
        // this panel is hidden until the player has completed onboarding, and this
        // guard keeps the panel closed even if it is opened by script.
        if (!PetTutorialHint.IsOnboardingCompleted)
            return true;

        if (OnboardingCelebrationView.IsAnyOpen)
            return true;

        // The shop's canvas sorts above this one, so it must never be able to
        // cover an open quest panel.
        if (ShopPanelController.IsAnyOpen)
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
            // CatInputCategory.All: movement, petting and world actions are all
            // suspended while the quest panel is on screen, so the cat can not be
            // driven or petted through the panel.
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

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(RequestClose);
            closeButton.onClick.AddListener(RequestClose);
        }

        if (scrimButton != null)
        {
            scrimButton.onClick.RemoveListener(RequestClose);
            scrimButton.onClick.AddListener(RequestClose);
        }

        if (rows != null)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                Button claim = rows[i].claimButton;
                if (claim == null)
                    continue;

                // The index, not the quest id, is captured: the row's current id
                // is looked up at click time so a refreshed row always claims what
                // it currently shows.
                int index = i;
                claim.onClick.AddListener(() => OnClaimPressed(index));
            }
        }

        if(chapterTab!=null) chapterTab.onClick.AddListener(()=>SelectDaily(false));
        if(dailyTab!=null) dailyTab.onClick.AddListener(()=>SelectDaily(true));
        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(RequestClose);
        if (scrimButton != null)
            scrimButton.onClick.RemoveListener(RequestClose);

        // Claim listeners are lambdas; RemoveAllListeners clears them without
        // touching persistent (Editor-authored) calls, of which rows have none.
        if (rows != null)
        {
            for (int i = 0; i < rows.Length; i++)
                if (rows[i].claimButton != null)
                    rows[i].claimButton.onClick.RemoveAllListeners();
        }

        listenersBound = false;
    }

    private void OnDisable()
    {
        // Never leave the cat's input blocked because the panel was disabled
        // mid-animation, and never leave a subscription pointing at a disabled
        // view.
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        UnsubscribeFromProgression();

        animTime = 0f;
        ApplyVisuals();
        state = PanelState.Closed;
        IsAnyOpen = false;
        SetRaycasts(false);
        ReleaseInputBlock();
    }

    private void OnDestroy()
    {
        IsAnyOpen = false;
        UnsubscribeFromProgression();
        ReleaseInputBlock();
        UnbindListeners();
    }

    private static float Clamp01(float v) => Mathf.Clamp01(v);

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
