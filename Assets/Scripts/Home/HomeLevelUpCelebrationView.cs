using System;
using System.Collections;
using CatHome.Economy;
using TMPro;
using U = PremiumUiElements;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen level milestone with a photographic stage, level medallion and
/// one short confetti reveal over a world-only backdrop. It self-bootstraps
/// onto its own overlay canvas the first
/// time a scene loads and listens to <see cref="HomeProgressionService.LeveledUp"/>,
/// so it needs no scene wiring and never touches the authored scenes.
///
/// It grants the level-up coin reward on the player's choice: <see cref="BaseCoins"/>
/// on Collect, or <see cref="DoubledCoins"/> after a verified rewarded ad. The
/// grant is guarded so it lands exactly once per celebration.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeLevelUpCelebrationView : MonoBehaviour
{
    private const int ConfettiCount = 22;
    private const int SortingOrder = 300;
    private const long BaseCoins = 500;
    private const long DoubledCoins = 1000;

    private static HomeLevelUpCelebrationView instance;

    private CanvasGroup rootGroup;
    private PremiumModalBackdrop backdrop;
    private RectTransform panel;
    private RectTransform burst;
    private RectTransform medallion;
    private TMP_Text levelNumber;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text rewardText;
    private RectTransform collectRoot;
    private RectTransform adRoot;
    private Button collectButton;
    private Button adButton;
    private Graphic collectFace;
    private Graphic adFace;
    private RectTransform[] confetti;
    private Vector2[] directions;
    private float[] rotations;
    private Coroutine routine;
    private bool isOpen;
    private bool closing;
    private bool rewardClaimed;
    private int shownLevel;

    public static bool IsAnyOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject canvasObject = new GameObject("HomeLevelUpCelebrationCanvas");
        UnityEngine.Object.DontDestroyOnLoad(canvasObject);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        instance = canvasObject.AddComponent<HomeLevelUpCelebrationView>();
        instance.Build();
        instance.SetHiddenImmediate();

        HomeProgressionService.LeveledUp -= instance.HandleLeveledUp;
        HomeProgressionService.LeveledUp += instance.HandleLeveledUp;
    }

    private void OnDestroy()
    {
        HomeProgressionService.LeveledUp -= HandleLeveledUp;
        if (instance == this)
            instance = null;
        IsAnyOpen = false;
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        isOpen = false;
        closing = false;
        IsAnyOpen = false;
        SetHiddenImmediate();
    }

    private void HandleLeveledUp(int newLevel)
    {
        Show(newLevel);
    }

    // ----- Build -----

    private void Build()
    {
        TMP_FontAsset font = FindFont();
        RectTransform root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        rootGroup = gameObject.AddComponent<CanvasGroup>();

        // The shared world-only capture temporarily fills the camera viewport,
        // so the reserved home dock and HUD never enter the modal backdrop.
        RectTransform blurRoot = NewRect(root, "BlurBackground", Vector2.zero, Vector2.zero);
        blurRoot.anchorMin = Vector2.zero;
        blurRoot.anchorMax = Vector2.one;
        blurRoot.offsetMin = blurRoot.offsetMax = Vector2.zero;
        blurRoot.gameObject.AddComponent<CanvasRenderer>();
        Image blurImage = blurRoot.gameObject.AddComponent<Image>();
        blurImage.color = new Color(0.05f, 0.06f, 0.13f, 1f);
        blurImage.raycastTarget = true;
        backdrop = blurRoot.gameObject.AddComponent<PremiumModalBackdrop>();

        Image tint = NewRect(root, "Tint", Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        tint.rectTransform.anchorMin = Vector2.zero;
        tint.rectTransform.anchorMax = Vector2.one;
        tint.rectTransform.offsetMin = tint.rectTransform.offsetMax = Vector2.zero;
        tint.color = new Color(0.05f, 0.06f, 0.14f, 0.42f);
        tint.raycastTarget = true;

        panel = NewRect(root, "CelebrationPanel", new Vector2(1060f, 620f), Vector2.zero);
        PremiumMomentArt.FitParent(panel,1060,620);

        NewPanel(panel,"IvoryFace",new Vector2(1060,620),Vector2.zero,PremiumUiStyle.Ivory,32,2);
        PremiumMomentArt.Stage(panel,-326,0,338,552);
        PremiumMomentArt.Caption(panel,"Her gün biraz\ndaha evimiz.","More like home,\nevery day.",-326,-174,286,94,27);
        PremiumMomentArt.Caption(panel,"YENİ BİR DÖNÜM NOKTASI","A NEW MILESTONE",176,240,574,40,19);
        PremiumMomentArt.RewardTray(panel,176,-76,574,100);
        burst = NewRect(panel, "SunBurst", new Vector2(236f, 236f), new Vector2(-326f, 62f));
        burst.gameObject.AddComponent<CanvasRenderer>();
        burst.gameObject.AddComponent<OnboardingCelebrationGraphic>()
            .Configure(OnboardingCelebrationGraphic.ShapeKind.Burst, new Color32(255, 216, 147, 70));

        // Gold medallion with the new level number.
        medallion = NewRect(panel, "LevelMedallion", new Vector2(180f, 180f), new Vector2(-326f, 62f));
        NewPanel(medallion, "MedallionRim", new Vector2(180f, 180f), Vector2.zero, PremiumUiStyle.Champagne, 88f, 6f);
        NewPanel(medallion, "MedallionFace", new Vector2(142f, 142f), Vector2.zero, PremiumUiStyle.Ivory, 68f, 4f);
        NewText(medallion, "LvLabel", font, "LV.", 28f, FontStyles.Bold,
            new Vector2(140f, 32f), new Vector2(0f, 34f), PremiumUiStyle.Navy);
        levelNumber = NewText(medallion, "LevelNumber", font, "6", 86f, FontStyles.Bold,
            new Vector2(160f, 104f), new Vector2(0f, -16f), PremiumUiStyle.Navy);

        titleText = NewText(panel, "Title", font, GameLanguageService.Text("celebration.level"), 44f, FontStyles.Bold,
            new Vector2(574f, 116f), new Vector2(176f, 152f), PremiumUiStyle.Ink);
        subtitleText = NewText(panel, "Subtitle", font, "YOUR HOME REACHED LEVEL 6", 28f, FontStyles.Bold,
            new Vector2(574f, 64f), new Vector2(176f, 46f), PremiumUiStyle.Muted);
        rewardText = NewText(panel, "Reward", font, "REWARD  +500 COINS", 34f, FontStyles.Bold,
            new Vector2(466f, 78f), new Vector2(218f, -76f), PremiumUiStyle.Teal);

        collectButton = BuildButton(font, "CollectButton", "COLLECT", new Vector2(274f, 80f),
            new Vector2(26f, -218f), PremiumUiStyle.Coral, PremiumUiStyle.Ink, out collectRoot, out collectFace);
        collectButton.onClick.AddListener(HandleCollect);

        adButton = BuildButton(font, "WatchAdButton", "WATCH AD  x2", new Vector2(274f, 80f),
            new Vector2(326f, -218f), PremiumUiStyle.Mint, PremiumUiStyle.Ink, out adRoot, out adFace);
        adButton.onClick.AddListener(HandleWatchAd);

        BuildConfetti();

        // Draw order: burst behind, then confetti, then medallion and text on top.
        burst.SetSiblingIndex(3);
        medallion.SetAsLastSibling();
        titleText.rectTransform.SetAsLastSibling();
        subtitleText.rectTransform.SetAsLastSibling();
        rewardText.rectTransform.SetAsLastSibling();
        collectRoot.SetAsLastSibling();
        adRoot.SetAsLastSibling();

        // Everything except the two button faces and the dim is decoration.
        Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
        if (collectFace != null) collectFace.raycastTarget = true;
        if (adFace != null) adFace.raycastTarget = true;
    }

    private void BuildConfetti()
    {
        confetti = new RectTransform[ConfettiCount];
        directions = new Vector2[ConfettiCount];
        rotations = new float[ConfettiCount];
        for (int i = 0; i < ConfettiCount; i++)
        {
            float angle = (i + 0.35f * (i % 3)) * Mathf.PI * 2f / ConfettiCount;
            float distance = 190f + (i % 5) * 26f;
            directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            rotations[i] = (i % 2 == 0 ? 1f : -1f) * (150f + (i % 7) * 35f);

            RectTransform piece = NewRect(panel, "Confetti_" + i,
                new Vector2(22f + (i % 3) * 6f, 22f + (i % 2) * 8f), new Vector2(-326f, 62f));
            piece.gameObject.AddComponent<CanvasRenderer>();
            var graphic = piece.gameObject.AddComponent<OnboardingCelebrationGraphic>();
            var kind = i % 5 == 0 ? OnboardingCelebrationGraphic.ShapeKind.Heart
                : i % 3 == 0 ? OnboardingCelebrationGraphic.ShapeKind.Star
                : OnboardingCelebrationGraphic.ShapeKind.Diamond;
            graphic.Configure(kind, i % 2 == 0
                ? new Color32(255, 196, 64, 255)
                : new Color32(48, 213, 211, 255));
            confetti[i] = piece;
        }
    }

    private Button BuildButton(TMP_FontAsset font, string name, string label, Vector2 size, Vector2 pos,
        Color faceColor, Color textColor, out RectTransform root, out Graphic face)
    {
        var button=U.Action(name,panel,font,name=="CollectButton"?"quests.claim":"celebration.double",name=="CollectButton"?PremiumUiStyle.Coral:PremiumUiStyle.Mint,pos.x,pos.y,size.x,size.y,out var text);
        root=(RectTransform)button.transform; face=button.targetGraphic;
        return button;
    }

    // ----- Show / hide -----

    public void Show(int level)
    {
        if (closing)
            return;

        shownLevel = level;
        rewardClaimed = false;
        if (levelNumber != null)
            levelNumber.text = level.ToString();
        if (subtitleText != null)
            subtitleText.text = GameLanguageService.Format("celebration.level_body",level);
        if (rewardText != null)
            rewardText.text = GameLanguageService.Format("celebration.coins",BaseCoins);

        // The doubling button only appears when a rewarded ad is actually ready;
        // otherwise Collect takes the whole row so there is no dead button.
        bool adReady = CatRunnerRewardedAdBridge.HasReadyProvider;
        adRoot.gameObject.SetActive(adReady);
        collectRoot.anchoredPosition = new Vector2(adReady ? 26f : 176f, -218f);
        collectRoot.sizeDelta = new Vector2(adReady ? 274f : 574f,80f);
        if (collectButton != null) collectButton.interactable = true;
        if (adButton != null) adButton.interactable = true;

        isOpen = true;
        IsAnyOpen = true;
        transform.SetAsLastSibling();
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
        if (backdrop != null)
            backdrop.enabled = true;
        StartRoutine(OpenRoutine());
    }

    private void HandleCollect()
    {
        ClaimReward(BaseCoins, EconomySource.Achievement);
    }

    private void HandleWatchAd()
    {
        if (!isOpen || closing || rewardClaimed)
            return;

        if (collectButton != null) collectButton.interactable = false;
        if (adButton != null) adButton.interactable = false;

        bool started = CatRunnerRewardedAdBridge.TryShow(verified =>
        {
            if (verified)
                ClaimReward(DoubledCoins, EconomySource.RewardedAd);
            else
                ClaimReward(BaseCoins, EconomySource.Achievement);
        });

        // No ad available after all: never punish the player, grant the base.
        if (!started)
            ClaimReward(BaseCoins, EconomySource.Achievement);
    }

    private void ClaimReward(long amount, EconomySource source)
    {
        if (rewardClaimed)
            return;
        rewardClaimed = true;

        EconomyService.AddCurrency(
            CurrencyType.Coin,
            amount,
            source,
            "home-levelup-" + shownLevel + "-" + Guid.NewGuid().ToString("N"));

        BeginClose();
    }

    private void BeginClose()
    {
        if (closing)
            return;
        closing = true;
        if (collectButton != null) collectButton.interactable = false;
        if (adButton != null) adButton.interactable = false;
        StartRoutine(CloseRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        ResetPieces();

        float elapsed = 0f;
        const float duration = 1.05f;
        while (elapsed < duration && !CatRunnerProgressService.ReducedMotion)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float pop = Mathf.Lerp(.96f,1f,EaseOut(Mathf.Clamp01(elapsed/.28f)));
            panel.localScale = Vector3.one * pop;

            float burstPop = Mathf.Clamp01(t / 0.18f);
            burst.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, EaseOut(burstPop));
            burst.localRotation = Quaternion.Euler(0f, 0f, t * 12f);

            float medPop = Mathf.Clamp01((t - 0.12f) / 0.4f);
            medallion.localScale = Vector3.one * Mathf.Lerp(.9f,1f,EaseOut(medPop));

            for (int i = 0; i < confetti.Length; i++)
            {
                float p = Mathf.Clamp01((t - 0.08f) / ((i % 4) * 0.025f + 0.62f));
                confetti[i].anchoredPosition = new Vector2(-326f, 62f) + directions[i] * EaseOut(p);
                confetti[i].localRotation = Quaternion.Euler(0f, 0f, rotations[i] * p);
                confetti[i].localScale = Vector3.one * Mathf.Sin(p * Mathf.PI) * 1.2f;
                SetAlpha(confetti[i],1f-Mathf.Clamp01((p-.68f)/.32f));
            }
            yield return null;
        }

        panel.localScale = Vector3.one;
        medallion.localScale = Vector3.one;

        // Keep the reward choice visible after a bounded reveal. The service
        // still owns collection; settling this animation never claims it.
        ResetPieces();
        routine = null;
    }

    private IEnumerator CloseRoutine()
    {
        float elapsed = 0f;
        while (elapsed < 0.24f && !CatRunnerProgressService.ReducedMotion)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(elapsed / 0.24f);
            rootGroup.alpha = 1f - t;
            panel.localScale = Vector3.one * Mathf.Lerp(0.98f, 0.86f, t);
            yield return null;
        }
        isOpen = false;
        closing = false;
        IsAnyOpen = false;
        SetHiddenImmediate();
        routine = null;
    }

    private void StartRoutine(IEnumerator next)
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(next);
    }

    private void SetHiddenImmediate()
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
        if (panel != null)
            panel.localScale = Vector3.one;
        if (backdrop != null)
            backdrop.enabled = false;
        ResetPieces();
    }

    private void ResetPieces()
    {
        if (burst != null)
        {
            burst.localScale = Vector3.one;
            burst.localRotation = Quaternion.identity;
        }
        if (medallion != null)
            medallion.localScale = Vector3.one;
        if (confetti == null)
            return;
        for (int i = 0; i < confetti.Length; i++)
        {
            if (confetti[i] == null)
                continue;
            confetti[i].anchoredPosition = new Vector2(-326f, 62f);
            confetti[i].localScale = Vector3.zero;
            confetti[i].localRotation = Quaternion.identity;
            SetAlpha(confetti[i], 0f);
        }
    }

    // ----- Helpers -----

    private TMP_FontAsset FindFont()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].font != null)
                return texts[i].font;
        }
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private static RectTransform NewRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        r.localScale = Vector3.one;
        return r;
    }

    private static LowPolyPanelGraphic NewPanel(Transform parent, string name, Vector2 size, Vector2 pos,
        Color color, float cut, float bevel)
    {
        RectTransform r = NewRect(parent, name, size, pos);
        r.gameObject.AddComponent<CanvasRenderer>();
        var g = r.gameObject.AddComponent<LowPolyPanelGraphic>();
        g.ConfigureTutorialStyle(color, cut, bevel);
        g.raycastTarget = false;
        return g;
    }

    private static TMP_Text NewText(Transform parent, string name, TMP_FontAsset font, string value,
        float size, FontStyles style, Vector2 rect, Vector2 pos, Color color)
    {
        RectTransform r = NewRect(parent, name, rect, pos);
        r.gameObject.AddComponent<CanvasRenderer>();
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.text = value;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.color = color;
        t.raycastTarget = false;
        t.richText = false;
        PremiumTypography.Apply(t);
        return t;
    }

    private static void SetAlpha(RectTransform rect, float alpha)
    {
        if (rect == null)
            return;
        Graphic g = rect.GetComponent<Graphic>();
        if (g == null)
            return;
        Color c = g.color;
        c.a = alpha;
        g.color = c;
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float EaseOut(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - (1f - t) * (1f - t) * (1f - t);
    }
}
