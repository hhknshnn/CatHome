using System;
using System.Collections;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen "HOME LEVEL UP!" celebration and its coin reward: a gold-framed
/// navy card with a spinning sunburst, a big level medallion and a confetti
/// spray, over an opaque backdrop that hides the in-game HUD so the moment reads
/// as a clean takeover. It self-bootstraps onto its own overlay canvas the first
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
    private RawImage blurImage;
    private RenderTexture blurRt;
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
        ReleaseBlur();
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

        // Backdrop: a frozen, heavily blurred snapshot of the game screen captured
        // the moment the celebration opens, so the world stays visible but soft and
        // the HUD reads as pushed behind. Filled with a solid navy until the first
        // capture. A translucent tint over it lifts contrast for the card.
        RectTransform blurRoot = NewRect(root, "BlurBackground", Vector2.zero, Vector2.zero);
        blurRoot.anchorMin = Vector2.zero;
        blurRoot.anchorMax = Vector2.one;
        blurRoot.offsetMin = blurRoot.offsetMax = Vector2.zero;
        blurRoot.gameObject.AddComponent<CanvasRenderer>();
        blurImage = blurRoot.gameObject.AddComponent<RawImage>();
        blurImage.color = new Color(0.05f, 0.06f, 0.13f, 1f);
        blurImage.raycastTarget = true;

        Image tint = NewRect(root, "Tint", Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        tint.rectTransform.anchorMin = Vector2.zero;
        tint.rectTransform.anchorMax = Vector2.one;
        tint.rectTransform.offsetMin = tint.rectTransform.offsetMax = Vector2.zero;
        tint.color = new Color(0.05f, 0.06f, 0.14f, 0.42f);
        tint.raycastTarget = true;

        panel = NewRect(root, "CelebrationPanel", new Vector2(880f, 580f), Vector2.zero);

        NewPanel(panel, "Depth", new Vector2(900f, 600f), new Vector2(0f, -8f), PremiumUiStyle.Shadow, 54f, 12f);
        NewPanel(panel, "ChampagneFrame", new Vector2(880f, 580f), Vector2.zero, PremiumUiStyle.Champagne, 54f, 14f);
        NewPanel(panel, "NavyFace", new Vector2(836f, 536f), new Vector2(0f, 4f), PremiumUiStyle.Navy, 45f, 9f);

        burst = NewRect(panel, "SunBurst", new Vector2(430f, 430f), new Vector2(0f, 150f));
        burst.gameObject.AddComponent<CanvasRenderer>();
        burst.gameObject.AddComponent<OnboardingCelebrationGraphic>()
            .Configure(OnboardingCelebrationGraphic.ShapeKind.Burst, new Color32(255, 196, 64, 200));

        // Gold medallion with the new level number.
        medallion = NewRect(panel, "LevelMedallion", new Vector2(180f, 180f), new Vector2(0f, 152f));
        NewPanel(medallion, "MedallionRim", new Vector2(180f, 180f), Vector2.zero, PremiumUiStyle.Champagne, 88f, 6f);
        NewPanel(medallion, "MedallionFace", new Vector2(142f, 142f), Vector2.zero, PremiumUiStyle.Ivory, 68f, 4f);
        NewText(medallion, "LvLabel", font, "LV.", 28f, FontStyles.Bold,
            new Vector2(140f, 32f), new Vector2(0f, 34f), PremiumUiStyle.Navy);
        levelNumber = NewText(medallion, "LevelNumber", font, "6", 86f, FontStyles.Bold,
            new Vector2(160f, 104f), new Vector2(0f, -16f), PremiumUiStyle.Navy);

        titleText = NewText(panel, "Title", font, "HOME LEVEL UP!", 56f, FontStyles.Bold,
            new Vector2(760f, 72f), new Vector2(0f, 8f), PremiumUiStyle.Ivory);
        subtitleText = NewText(panel, "Subtitle", font, "YOUR HOME REACHED LEVEL 6", 28f, FontStyles.Bold,
            new Vector2(780f, 44f), new Vector2(0f, -44f), PremiumUiStyle.ChampagneLight);
        rewardText = NewText(panel, "Reward", font, "REWARD  +500 COINS", 34f, FontStyles.Bold,
            new Vector2(780f, 52f), new Vector2(0f, -108f), PremiumUiStyle.Champagne);

        collectButton = BuildButton(font, "CollectButton", "COLLECT", new Vector2(320f, 96f),
            new Vector2(-172f, -196f), PremiumUiStyle.Teal, Color.white, out collectRoot, out collectFace);
        collectButton.onClick.AddListener(HandleCollect);

        adButton = BuildButton(font, "WatchAdButton", "WATCH AD  x2", new Vector2(320f, 96f),
            new Vector2(172f, -196f), PremiumUiStyle.Champagne, PremiumUiStyle.Navy, out adRoot, out adFace);
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
                new Vector2(22f + (i % 3) * 6f, 22f + (i % 2) * 8f), new Vector2(0f, 150f));
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
        root = NewRect(panel, name, size, pos);
        LowPolyPanelGraphic panelFace = NewPanel(root, "ButtonFace", new Vector2(size.x, size.y - 8f),
            new Vector2(0f, 4f), faceColor, 40f, 6f);
        face = panelFace;
        NewText(root, "Label", font, label, 30f, FontStyles.Bold,
            new Vector2(size.x - 26f, size.y - 30f), new Vector2(0f, 4f), textColor);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = panelFace;
        button.transition = Selectable.Transition.ColorTint;
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
            subtitleText.text = "YOUR HOME REACHED LEVEL " + level;
        if (rewardText != null)
            rewardText.text = "REWARD  +" + BaseCoins + " COINS";

        // The doubling button only appears when a rewarded ad is actually ready;
        // otherwise Collect takes the whole row so there is no dead button.
        bool adReady = CatRunnerRewardedAdBridge.HasReadyProvider;
        adRoot.gameObject.SetActive(adReady);
        collectRoot.anchoredPosition = new Vector2(adReady ? -172f : 0f, -196f);
        if (collectButton != null) collectButton.interactable = true;
        if (adButton != null) adButton.interactable = true;

        isOpen = true;
        IsAnyOpen = true;
        transform.SetAsLastSibling();
        // Stay hidden for one frame so the backdrop snapshot (taken at end of frame
        // inside OpenRoutine) captures the game without the celebration itself.
        rootGroup.alpha = 0f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
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

        // Capture the blurred backdrop at end of frame (a mid-script capture reads
        // a black buffer), while this canvas is still transparent, then reveal.
        yield return new WaitForEndOfFrame();
        CaptureBlur();
        rootGroup.alpha = 1f;

        float elapsed = 0f;
        const float duration = 1.05f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float pop = t < 0.55f
                ? Mathf.Lerp(0.75f, 1.08f, EaseOut(t / 0.55f))
                : Mathf.Lerp(1.08f, 1f, Smooth((t - 0.55f) / 0.45f));
            panel.localScale = Vector3.one * pop;

            float burstPop = Mathf.Clamp01(t / 0.18f);
            burst.localScale = Vector3.one * Mathf.Lerp(0.72f, 1f, EaseOut(burstPop));
            burst.localRotation = Quaternion.Euler(0f, 0f, t * 12f);

            float medPop = Mathf.Clamp01((t - 0.12f) / 0.4f);
            medallion.localScale = Vector3.one * (0.2f + EaseOut(medPop) * 0.8f) *
                (1f + 0.08f * Mathf.Sin(medPop * Mathf.PI));

            for (int i = 0; i < confetti.Length; i++)
            {
                float p = Mathf.Clamp01((t - 0.08f) / ((i % 4) * 0.025f + 0.62f));
                confetti[i].anchoredPosition = new Vector2(0f, 150f) + directions[i] * EaseOut(p);
                confetti[i].localRotation = Quaternion.Euler(0f, 0f, rotations[i] * p);
                confetti[i].localScale = Vector3.one * Mathf.Sin(p * Mathf.PI) * 1.2f;
            }
            yield return null;
        }

        panel.localScale = Vector3.one;
        medallion.localScale = Vector3.one;

        float loop = 0f;
        while (isOpen && !closing)
        {
            loop += Time.unscaledDeltaTime;
            burst.localRotation = Quaternion.Euler(0f, 0f, 12f + loop * 10f);
            burst.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(loop * 3.2f));
            float breathe = 1f + 0.04f * Mathf.Sin(loop * 2.4f);
            medallion.localScale = new Vector3(breathe, breathe, 1f);
            for (int i = 0; i < confetti.Length; i++)
            {
                float p = Mathf.Repeat(loop * 0.34f + i / (float)confetti.Length, 1f);
                float visible = Mathf.Sin(p * Mathf.PI);
                confetti[i].anchoredPosition = new Vector2(0f, 150f) + directions[i] * EaseOut(p);
                confetti[i].localRotation = Quaternion.Euler(0f, 0f, rotations[i] * p + loop * 35f);
                confetti[i].localScale = Vector3.one * (0.55f + visible * 0.65f);
                SetAlpha(confetti[i], visible);
            }
            yield return null;
        }
    }

    private IEnumerator CloseRoutine()
    {
        float elapsed = 0f;
        while (elapsed < 0.24f)
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
        ReleaseBlur();
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
            confetti[i].anchoredPosition = new Vector2(0f, 150f);
            confetti[i].localScale = Vector3.zero;
            confetti[i].localRotation = Quaternion.identity;
            SetAlpha(confetti[i], 1f);
        }
    }

    // ----- Blurred backdrop -----

    private void CaptureBlur()
    {
        Texture2D snap = ScreenCapture.CaptureScreenshotAsTexture();
        if (snap == null)
            return;

        // The captured frame has a zero alpha channel (an opaque scene never writes
        // framebuffer alpha), which would make the RawImage transparent. Force it
        // opaque so the blurred snapshot actually shows.
        Color32[] pixels = snap.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i].a = 255;
        snap.SetPixels32(pixels);
        snap.Apply(false);

        // Progressive down- then up-sampling: each bilinear step averages a 2x2
        // neighbourhood, so the blur accumulates smoothly and the final texture is
        // large enough (~1/3 screen) that stretching it to full screen stays creamy
        // instead of showing bilinear blocks.
        int w = snap.width;
        int h = snap.height;
        RenderTexture d1 = Downsample(snap, w / 4, h / 4);
        RenderTexture d2 = Downsample(d1, w / 12, h / 12);
        RenderTexture d3 = Downsample(d2, w / 28, h / 28);
        RenderTexture u1 = Downsample(d3, w / 8, h / 8);
        RenderTexture u2 = Downsample(u1, w / 3, h / 3);

        ReleaseBlur();
        blurRt = new RenderTexture(u2.width, u2.height, 0) { filterMode = FilterMode.Bilinear };
        Graphics.Blit(u2, blurRt);

        RenderTexture.ReleaseTemporary(d1);
        RenderTexture.ReleaseTemporary(d2);
        RenderTexture.ReleaseTemporary(d3);
        RenderTexture.ReleaseTemporary(u1);
        RenderTexture.ReleaseTemporary(u2);
        Destroy(snap);

        if (blurImage != null)
        {
            blurImage.texture = blurRt;
            blurImage.color = Color.white;
        }
    }

    private static RenderTexture Downsample(Texture source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(Mathf.Max(8, width), Mathf.Max(8, height), 0);
        rt.filterMode = FilterMode.Bilinear;
        Graphics.Blit(source, rt);
        return rt;
    }

    private void ReleaseBlur()
    {
        if (blurImage != null)
        {
            blurImage.texture = null;
            blurImage.color = new Color(0.05f, 0.06f, 0.13f, 1f);
        }
        if (blurRt != null)
        {
            blurRt.Release();
            Destroy(blurRt);
            blurRt = null;
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
