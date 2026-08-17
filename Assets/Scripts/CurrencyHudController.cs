using System.Globalization;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-left progression/currency HUD: three compact low-poly badges inside the
/// mobile safe area — Bond XP, Coins, Diamonds — each with its icon, its live
/// value and a small "+" button beside it. Each badge is the same 68 units tall
/// as the top-right buttons and shares their vertical centre; the arithmetic
/// that guarantees that lives in <c>CurrencyHudBuilder</c>.
///
/// This controller paints the amounts and then asks each entry's
/// <see cref="CurrencyEntryLayout"/> to re-centre its content, because the
/// position of a "+" depends on how wide its amount rendered.
///
/// This controller owns no economy and no progression. It is a pure view:
/// <see cref="Refresh"/> reads straight through to the authoritative services
/// every time it paints, so nothing here is a cached balance. Coins and diamonds
/// come from <see cref="EconomyService"/>, bond XP from
/// <see cref="ProgressionService"/> and home level from
/// <see cref="HomeProgressionService"/>. It subscribes to their change events and
/// repaints; it never writes back, never touches save data, quests or rewards.
///
/// The serialized amounts below exist only as an edit-mode layout preview and
/// for a deliberately unbound HUD (<see cref="bindToEconomyService"/> off). While
/// the game is playing and bound, they are ignored entirely — there is no code
/// path that can show a hardcoded amount in a running build.
///
/// The hierarchy is authored by <c>CurrencyHudBuilder</c> (Editor) and wired into
/// the serialized fields below. References are also resolved defensively at
/// runtime, so a partially wired object degrades to "shows nothing" instead of
/// throwing.
///
/// Coexistence: this lives on its own canvas (sortingOrder 95) below the main
/// panel canvas (100) and the shop canvas (110), so the main menu's outside-tap
/// scrim and the shop always cover it. Only the three "+" buttons take raycasts;
/// the frames, icons and labels are decoration and never steal a world tap.
/// Visibility follows the same gate as the top-right bar (hidden during
/// onboarding, the celebration, the offline popup, the shop and the quest panel).
/// </summary>
[DisallowMultipleComponent]
// After TopHudResponsiveLayout (100), so the needs group has already been placed
// and scaled for this frame when the overlap fit below measures against it.
[DefaultExecutionOrder(200)]
public sealed class CurrencyHudController : MonoBehaviour
{
    /// <summary>Which HUD entry a "+" tap came from.</summary>
    public enum HudEntry
    {
        BondXp,
        Coin,
        Diamond,
    }

    // Name of the left-most need indicator, matching TopHudResponsiveLayout's own
    // lookup. Used only to measure how much room the group has; nothing about the
    // needs HUD is read, moved or modified.
    private static readonly string[] NeedsAnchorNames = { "HungerUI", "FoodBar" };

    /// <summary>Breathing room kept between the group and the needs HUD, in canvas units.</summary>
    private const float NeedsGap = 16f;

    /// <summary>The group never shrinks below this, so values stay readable.</summary>
    private const float MinimumFitScale = 0.7f;

    // No [Min] here on purpose: Unity's MinDrawer routes an Integer property
    // through intValue, which would silently truncate a long. OnValidate clamps
    // these instead.
    [Header("Layout preview only — never shown while playing and bound")]
    [SerializeField] private long bondXp;
    [SerializeField] private long coins;
    [SerializeField] private long diamonds;

    [Header("Raycast / visibility gate (whole canvas)")]
    [SerializeField] private CanvasGroup rootGroup;

    [Header("Strip root (scaled down if it would reach the needs HUD)")]
    [SerializeField] private RectTransform groupRect;

    [Header("Labels")]
    [SerializeField] private TMP_Text bondXpText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text diamondText;
    [SerializeField] private TMP_Text homeLevelText;

    [Header("\"+\" buttons (isolated placeholders, see HandlePlusRequest)")]
    [SerializeField] private Button bondXpPlusButton;
    [SerializeField] private Button coinPlusButton;
    [SerializeField] private Button diamondPlusButton;

    [Header("Behaviour")]
    // Mirrors the top-right bar: the HUD steps aside for onboarding, the
    // celebration, the offline popup, the shop and the quest panel instead of
    // floating over them.
    [SerializeField] private bool hideWhileBlockingModalOpen = true;

    [Tooltip(
        "Shows the real values owned by EconomyService (coins, diamonds) and " +
        "ProgressionService (bond XP), repainting on their change events. Turn " +
        "off to keep the serialized preview amounts, which is only useful for a " +
        "layout mock-up."
    )]
    [SerializeField] private bool bindToEconomyService = true;

    [Tooltip(
        "Scales the group down when a narrow or portrait screen would push it " +
        "into the centered Hunger / Thirst / Energy group. The needs HUD itself " +
        "is only measured, never moved."
    )]
    [SerializeField] private bool avoidNeedsHudOverlap = true;

    private bool listenersBound;
    private bool subscribedToServices;
    private bool started;

    // Per-entry content layouts, resolved on the first repaint. See RelayoutEntries.
    private CurrencyEntryLayout[] entryLayouts;

    // Resolved lazily (same idiom as MainPanelController's shopPanel) so this HUD
    // stays an independent system with no wired reference to the popup.
    private WhileYouWereAwayPopup offlinePopup;

    // Overlap fit state. Recomputed only when the screen, the safe area or the
    // canvas scale actually changed — never per frame.
    private Canvas hudCanvas;
    private RectTransform needsAnchor;
    private bool fitDirty = true;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float lastCanvasScale = -1f;

    /// <summary>True while the labels are painted from the live services.</summary>
    private bool IsBound => bindToEconomyService && Application.isPlaying;

    // ----- Public read-through API (never a cached balance) -----

    public long BondXp => IsBound ? Clamp(ProgressionService.BondXp) : bondXp;
    public long Coins => IsBound ? Clamp(EconomyService.Coins) : coins;
    public long Diamonds => IsBound ? Clamp(EconomyService.Diamonds) : diamonds;

    private void Awake()
    {
        if (rootGroup == null)
            rootGroup = GetComponent<CanvasGroup>();
        if (hudCanvas == null)
            hudCanvas = GetComponent<Canvas>();

        BindListeners();

        // Park hidden and non-interactive before the first frame so nothing
        // flashes over onboarding; Update reveals it once the gate allows.
        SetVisible(!hideWhileBlockingModalOpen);
        Refresh();
    }

    private void Start()
    {
        started = true;
    }

    private void OnEnable()
    {
        // Re-bind in case the object was toggled off/on after Awake.
        BindListeners();
        Subscribe();

        // A repaint on enable covers the window this object was disabled for: the
        // subscriptions only report changes, so the current truth is read once.
        Refresh();
        fitDirty = true;
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!started)
            return;

        SetVisible(!hideWhileBlockingModalOpen || !IsBlockingModalActive());
        ApplyOverlapFit();
    }

    // ----- Preview amount API -----
    //
    // Display-only overrides for tooling and for an unbound HUD. While the game
    // is playing and bound, Refresh reads the services directly and these values
    // are never shown.

    public void SetBondXp(long value)
    {
        bondXp = Clamp(value);
        Refresh();
    }

    public void SetCoins(long value)
    {
        coins = Clamp(value);
        Refresh();
    }

    public void SetDiamonds(long value)
    {
        diamonds = Clamp(value);
        Refresh();
    }

    public void SetCurrencies(long diamondAmount, long coinAmount)
    {
        diamonds = Clamp(diamondAmount);
        coins = Clamp(coinAmount);
        Refresh();
    }

    /// <summary>Repaints every label from the authoritative values.</summary>
    public void Refresh()
    {
        if (bondXpText != null)
            bondXpText.text = Format(BondXp);
        if (coinText != null)
            coinText.text = Format(Coins);
        if (diamondText != null)
            diamondText.text = Format(Diamonds);
        if (homeLevelText != null)
            homeLevelText.text = FormatHomeLevel(HomeProgressionService.HomeLevel);

        RelayoutEntries();
    }

    /// <summary>
    /// Re-measures each entry so a "+" follows its digits in the same frame the
    /// amount changed, rather than a frame later. The layouts are found by search
    /// instead of being wired into new serialized fields, so every prefab and
    /// scene object already carrying this controller stays valid as-is.
    /// </summary>
    private void RelayoutEntries()
    {
        if (!EntryLayoutsResolved())
            entryLayouts = GetComponentsInChildren<CurrencyEntryLayout>(true);

        for (int i = 0; i < entryLayouts.Length; i++)
        {
            if (entryLayouts[i] != null)
                entryLayouts[i].Relayout();
        }
    }

    // A rebuilt HUD leaves this cache pointing at destroyed children, so the
    // entries are re-found rather than silently skipped.
    private bool EntryLayoutsResolved()
    {
        if (entryLayouts == null || entryLayouts.Length == 0)
            return false;

        for (int i = 0; i < entryLayouts.Length; i++)
        {
            if (entryLayouts[i] == null)
                return false;
        }

        return true;
    }

    // A balance can never be negative, but the HUD refuses to render a "-" even
    // if a future source hands it one.
    private static long Clamp(long value) => value < 0 ? 0 : value;

    // Grouped with an invariant separator so "2450" reads as "2,450" on every
    // device regardless of the player's locale. The project has no approved
    // compact ("12.5K") helper, so none is invented here; the labels auto-size
    // instead.
    private static string Format(long value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    // ----- Service binding (read-only, event driven) -----

    private void Subscribe()
    {
        if (subscribedToServices || !bindToEconomyService)
            return;

        // Guarded by the flag, so a scene reload or an enable/disable cycle can
        // never leave two handlers on either static event.
        EconomyService.BalanceChanged += OnBalanceChanged;
        ProgressionService.StateChanged += Refresh;
        HomeProgressionService.Changed += Refresh;
        subscribedToServices = true;
    }

    private void Unsubscribe()
    {
        if (!subscribedToServices)
            return;

        EconomyService.BalanceChanged -= OnBalanceChanged;
        ProgressionService.StateChanged -= Refresh;
        HomeProgressionService.Changed -= Refresh;
        subscribedToServices = false;
    }

    private static string FormatHomeLevel(int level) =>
        "HOME LV. " + level.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// One currency moved. The whole group is repainted from the services rather
    /// than patched from the delta, which keeps a missed event (this object was
    /// disabled) from leaving a stale number on screen.
    /// </summary>
    private void OnBalanceChanged(CurrencyBalanceChange change)
    {
        Refresh();
    }

    // ----- "+" buttons -----
    //
    // CP4 seam. Nothing here grants currency or bond XP, writes save data or
    // opens the shop / IAP / rewarded-ad shells, all of which are still
    // prototypes. Each button forwards its own entry to the single callback
    // below, which only logs — CP5 replaces the body of HandlePlusRequest with
    // the real routing (coin -> acquisition / rewarded ads, diamond -> IAP,
    // bond XP -> earn-information panel or quests) without touching anything
    // else in this file.

    private void OnBondXpPlusPressed() => HandlePlusRequest(HudEntry.BondXp);

    private void OnCoinPlusPressed() => HandlePlusRequest(HudEntry.Coin);

    private void OnDiamondPlusPressed() => HandlePlusRequest(HudEntry.Diamond);

    private void HandlePlusRequest(HudEntry entry)
    {
        ShopPanelController shop = FindAnyObjectByType<ShopPanelController>(
            FindObjectsInactive.Include);
        if (shop == null)
            return;

        switch (entry)
        {
            case HudEntry.Coin:
                shop.RequestOpen(HomeStoreCategory.Cat);
                break;
            case HudEntry.Diamond:
                shop.RequestDiamondStore();
                break;
            default:
                Debug.Log("Bond XP is earned by caring for your cat and completing quests.", this);
                break;
        }
    }

    // ----- Visibility -----

    private void SetVisible(bool visible)
    {
        if (rootGroup == null)
            return;

        rootGroup.alpha = visible ? 1f : 0f;
        rootGroup.interactable = visible;
        // Only the "+" buttons are raycast targets, so this gate is the
        // difference between "the + is tappable" and "the HUD is pure decoration".
        rootGroup.blocksRaycasts = visible;
    }

    // ----- Modal coordination (mirrors MainPanelController's gate) -----

    private bool IsBlockingModalActive()
    {
        if (!PetTutorialHint.IsOnboardingCompleted)
            return true;

        if (OnboardingCelebrationView.IsAnyOpen)
            return true;

        if (HomeLevelUpCelebrationView.IsAnyOpen)
            return true;

        if (ShopPanelController.IsAnyOpen)
            return true;

        // The quest panel covers this HUD and carries its own wallet readout, so
        // the group steps aside instead of duplicating the amounts underneath it.
        if (QuestPanelController.IsAnyOpen)
            return true;

        if (offlinePopup == null)
            offlinePopup = FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);

        return offlinePopup != null && offlinePopup.IsOpen;
    }

    // ----- Needs-HUD overlap fit -----

    /// <summary>
    /// Keeps the group clear of the centered needs group on narrow and portrait
    /// screens by scaling the group itself down. The needs indicators are only
    /// measured — their position, size and scale stay entirely owned by
    /// TopHudResponsiveLayout.
    /// </summary>
    private void ApplyOverlapFit()
    {
        if (!avoidNeedsHudOverlap || groupRect == null)
            return;

        if (hudCanvas == null)
        {
            hudCanvas = GetComponent<Canvas>();
            if (hudCanvas == null)
                return;
        }

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        float scaleFactor = Mathf.Max(0.0001f, hudCanvas.scaleFactor);
        if (!fitDirty && safeArea == lastSafeArea && screenSize == lastScreenSize &&
            Mathf.Abs(scaleFactor - lastCanvasScale) < 0.0001f)
        {
            return;
        }

        fitDirty = false;
        lastSafeArea = safeArea;
        lastScreenSize = screenSize;
        lastCanvasScale = scaleFactor;

        float fit = 1f;
        RectTransform needs = ResolveNeedsAnchor();
        if (needs != null)
        {
            // Both canvases are screen-space overlay, so a RectTransform's world
            // position is already in screen pixels and the two are directly
            // comparable. groupRect's pivot is its top-left corner, which is the
            // one point that does not move when the fit scale changes.
            float needsLeft = needs.position.x - needs.rect.width * 0.5f * needs.lossyScale.x;
            float available = needsLeft - NeedsGap * scaleFactor - groupRect.position.x;
            float wanted = groupRect.rect.width * scaleFactor;

            if (wanted > 0.01f && available < wanted)
                fit = Mathf.Clamp(available / wanted, MinimumFitScale, 1f);
        }

        groupRect.localScale = new Vector3(fit, fit, 1f);
    }

    /// <summary>
    /// Finds the left-most need indicator by name, the same way
    /// TopHudResponsiveLayout resolves it. The scan only runs when the reference
    /// is missing and the fit is being recomputed, so it happens at most once per
    /// resolution change — never per frame.
    /// </summary>
    private RectTransform ResolveNeedsAnchor()
    {
        if (needsAnchor != null)
            return needsAnchor;

        RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
        for (int i = 0; i < rects.Length; i++)
        {
            for (int nameIndex = 0; nameIndex < NeedsAnchorNames.Length; nameIndex++)
            {
                if (rects[i].name != NeedsAnchorNames[nameIndex])
                    continue;

                needsAnchor = rects[i];
                return needsAnchor;
            }
        }

        return needsAnchor;
    }

    // ----- Listener lifecycle -----

    private void BindListeners()
    {
        if (listenersBound)
            return;

        AddPlusListener(bondXpPlusButton, OnBondXpPlusPressed);
        AddPlusListener(coinPlusButton, OnCoinPlusPressed);
        AddPlusListener(diamondPlusButton, OnDiamondPlusPressed);
        listenersBound = true;
    }

    private static void AddPlusListener(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    private void UnbindListeners()
    {
        if (bondXpPlusButton != null)
            bondXpPlusButton.onClick.RemoveListener(OnBondXpPlusPressed);
        if (coinPlusButton != null)
            coinPlusButton.onClick.RemoveListener(OnCoinPlusPressed);
        if (diamondPlusButton != null)
            diamondPlusButton.onClick.RemoveListener(OnDiamondPlusPressed);
        listenersBound = false;
    }

    private void OnDestroy()
    {
        // OnDisable already ran for an active object; this covers a destroy that
        // never reached it, so no handler can outlive this component.
        Unsubscribe();
        UnbindListeners();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keeps the authored preview values visible in the Scene view while
        // designing, without needing to enter play mode.
        if (bondXp < 0) bondXp = 0;
        if (coins < 0) coins = 0;
        if (diamonds < 0) diamonds = 0;

        // Deferred: touching TMP text directly inside OnValidate can trip Unity's
        // "SendMessage cannot be called during OnValidate" warning.
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
                Refresh();
        };
    }
#endif
}
