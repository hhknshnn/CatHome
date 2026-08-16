using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procedural, sprite-free presentation for the top-right clock and brightness selector.
/// MainPanelController owns interaction coordination; this component owns only crisp visuals.
/// </summary>
[DisallowMultipleComponent]
public sealed class BrightnessPanelView : MonoBehaviour
{
    private static readonly Color Orange = PremiumUiStyle.Navy;
    private static readonly Color OrangeSelected = PremiumUiStyle.Teal;
    private static readonly Color Cream = PremiumUiStyle.Ivory;
    private static readonly Color Brown = PremiumUiStyle.Ink;
    private static readonly Color Shadow = PremiumUiStyle.Shadow;

    private RectTransform safeArea;
    private RectTransform lightButtonRect;
    private Graphic lightButtonFace;
    private GameTimeService timeService;
    private RectTransform clockRoot;
    private CanvasGroup clockGroup;
    private TMP_Text clockLabel;
    private RectTransform panelRoot;
    private CanvasGroup panelGroup;
    private readonly Button[] levelButtons = new Button[3];
    private readonly Graphic[] levelFaces = new Graphic[3];
    private Action<PlayerLightingPreference.LightLevel> onSelected;
    private double nextClockRefresh;
    private int displayedMinute = -1;
    private float lastSafeWidth = -1f;
    private bool highlighted;

    public bool IsOpen => panelGroup != null && panelGroup.alpha > 0.5f;

    public void Configure(
        RectTransform safeArea,
        RectTransform lightButtonRect,
        Graphic lightButtonFace,
        Action<PlayerLightingPreference.LightLevel> onSelected)
    {
        this.safeArea = safeArea;
        this.lightButtonRect = lightButtonRect;
        this.lightButtonFace = lightButtonFace;
        this.onSelected = onSelected;
        if (timeService == null)
            timeService = FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include);

        if (clockRoot == null || panelRoot == null)
            Build();

        SetPanelOpen(false);
        RefreshSelection();
        RefreshClock(true);
        ApplyResponsiveLayout(true);
    }

    private void Update()
    {
        RefreshClock(false);
        ApplyResponsiveLayout(false);
    }

    public void SetPanelOpen(bool open)
    {
        if (panelGroup == null)
            return;

        panelGroup.alpha = open ? 1f : 0f;
        panelGroup.interactable = open;
        panelGroup.blocksRaycasts = open;
        RefreshSelection();
    }

    public void SetTopBarVisible(bool visible)
    {
        if (clockGroup == null)
            return;
        clockGroup.alpha = visible ? 1f : 0f;
        clockGroup.interactable = false;
        clockGroup.blocksRaycasts = false;
        if (!visible)
            SetPanelOpen(false);
    }

    public void SetLightButtonHighlighted(bool value)
    {
        highlighted = value;
        RefreshLightFace();
    }

    public void RefreshSelection()
    {
        int selected = (int)PlayerLightingPreference.Current;
        for (int i = 0; i < levelFaces.Length; i++)
        {
            if (levelFaces[i] != null)
                levelFaces[i].color = i == selected ? OrangeSelected : PremiumUiStyle.NavyLift;
            TMP_Text label = levelButtons[i] != null
                ? levelButtons[i].GetComponentInChildren<TMP_Text>(true)
                : null;
            if (label != null)
                label.color = Cream;
        }
        RefreshLightFace();
    }

    private void RefreshLightFace()
    {
        if (lightButtonFace == null)
            return;

        if (highlighted)
        {
            lightButtonFace.color = PremiumUiStyle.TealLift;
            return;
        }

        switch (PlayerLightingPreference.Current)
        {
            case PlayerLightingPreference.LightLevel.Low:
                lightButtonFace.color = PremiumUiStyle.Night;
                break;
            case PlayerLightingPreference.LightLevel.High:
                lightButtonFace.color = PremiumUiStyle.Teal;
                break;
            default:
                lightButtonFace.color = Orange;
                break;
        }
    }

    private void Build()
    {
        TMP_FontAsset font = FindFont();
        Transform parent = safeArea != null ? safeArea : transform;

        clockRoot = CreateRect(parent, "ClockDisplay");
        clockRoot.anchorMin = clockRoot.anchorMax = new Vector2(1f, 1f);
        clockRoot.pivot = new Vector2(1f, 1f);
        // Same height and top offset as every other top-bar control. The old
        // 56-unit clock sat inset by six units and made the right cluster look
        // detached even though its centre happened to match.
        clockRoot.sizeDelta = new Vector2(112f, 68f);
        clockGroup = clockRoot.gameObject.AddComponent<CanvasGroup>();

        LowPolyPanelGraphic clockShadow = CreatePanel(clockRoot, "Shadow", Shadow, false);
        Stretch(clockShadow.rectTransform, -3f, -3f, 3f, 3f);
        LowPolyPanelGraphic clockFrame = CreatePanel(clockRoot, "Frame", PremiumUiStyle.Champagne, false);
        Stretch(clockFrame.rectTransform, 0f, 0f, 0f, 0f);
        LowPolyPanelGraphic clockFace = CreatePanel(clockRoot, "Face", PremiumUiStyle.Navy, false);
        Stretch(clockFace.rectTransform, 4f, 4f, -4f, -4f);

        clockLabel = CreateText(clockRoot, "Time", font, 25f, Cream, TextAlignmentOptions.Center);
        clockLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        clockLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        clockLabel.rectTransform.offsetMin = new Vector2(8f, 0f);
        clockLabel.rectTransform.offsetMax = new Vector2(-8f, 0f);
        clockLabel.fontStyle = FontStyles.Bold;

        panelRoot = CreateRect(parent, "BrightnessPanel");
        panelRoot.anchorMin = panelRoot.anchorMax = new Vector2(1f, 1f);
        panelRoot.pivot = new Vector2(0.5f, 1f);
        panelRoot.sizeDelta = new Vector2(324f, 112f);
        panelGroup = panelRoot.gameObject.AddComponent<CanvasGroup>();

        LowPolyPanelGraphic panelShadow = CreatePanel(panelRoot, "Shadow", Shadow, false);
        Stretch(panelShadow.rectTransform, -3f, -3f, 3f, 3f);
        LowPolyPanelGraphic frame = CreatePanel(panelRoot, "Frame", PremiumUiStyle.Champagne, false);
        Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);
        LowPolyPanelGraphic face = CreatePanel(panelRoot, "CreamFace", PremiumUiStyle.Navy, false);
        Stretch(face.rectTransform, 5f, 5f, -5f, -5f);

        TMP_Text title = CreateText(panelRoot, "Title", font, 16f, PremiumUiStyle.ChampagneLight, TextAlignmentOptions.Center);
        title.text = "BRIGHTNESS";
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 2f;
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.sizeDelta = new Vector2(0f, 30f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -8f);

        string[] labels = { "LOW", "MEDIUM", "HIGH" };
        for (int i = 0; i < labels.Length; i++)
            CreateLevelButton(panelRoot, font, i, labels[i]);
    }

    private void CreateLevelButton(Transform parent, TMP_FontAsset font, int index, string label)
    {
        RectTransform root = CreateRect(parent, label + "Button");
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(94f, 54f);
        root.anchoredPosition = new Vector2(14f + index * 101f, -43f);

        LowPolyPanelGraphic shadow = CreatePanel(root, "Shadow", new Color32(84, 42, 53, 100), false);
        Stretch(shadow.rectTransform, -2f, -2f, 2f, 2f);
        LowPolyPanelGraphic face = CreatePanel(root, "Face", PremiumUiStyle.NavyLift, true);
        Stretch(face.rectTransform, 0f, 0f, 0f, 0f);

        TMP_Text text = CreateText(root, "Label", font, index == 1 ? 15f : 17f, Cream, TextAlignmentOptions.Center);
        text.text = label;
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform, 4f, 2f, -4f, -2f);

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.transition = Selectable.Transition.None;
        PremiumButtonFx fx = root.gameObject.AddComponent<PremiumButtonFx>();
        fx.Configure(face.rectTransform, face, button, index == 1);
        PlayerLightingPreference.LightLevel level = (PlayerLightingPreference.LightLevel)index;
        button.onClick.AddListener(() => onSelected?.Invoke(level));
        levelButtons[index] = button;
        levelFaces[index] = face;
    }

    private void RefreshClock(bool force)
    {
        if (clockLabel == null)
            return;

        // Additive scene load order is not deterministic. The UI can awake
        // before LivingRoom_Level01 creates its GameTimeService, so resolve it
        // again instead of leaving a permanently empty clock frame.
        if (timeService == null)
            timeService = FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include);

        double realtime = Time.realtimeSinceStartupAsDouble;
        if (!force && realtime < nextClockRefresh)
            return;
        nextClockRefresh = realtime + 0.5d;

        // Device time is a presentation-only fallback, matching
        // GameTimeService.CurrentLocalTime's own documented fallback semantics.
        DateTimeOffset local = timeService != null
            ? timeService.CurrentLocalTime
            : DateTimeOffset.Now;
        int minuteKey = local.DayOfYear * 1440 + local.Hour * 60 + local.Minute;
        if (!force && minuteKey == displayedMinute)
            return;

        displayedMinute = minuteKey;
        clockLabel.SetText("{0:00}:{1:00}", local.Hour, local.Minute);
    }

    private void ApplyResponsiveLayout(bool force)
    {
        if (safeArea == null || lightButtonRect == null || clockRoot == null || panelRoot == null)
            return;

        float width = safeArea.rect.width;
        if (!force && Mathf.Abs(width - lastSafeWidth) < 0.5f)
            return;
        lastSafeWidth = width;

        float gap = width < 900f ? 8f : 14f;
        float clockWidth = width < 720f ? 104f : 112f;
        clockRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, clockWidth);
        clockRoot.anchoredPosition = new Vector2(
            lightButtonRect.anchoredPosition.x - lightButtonRect.rect.width - gap,
            -22f
        );
        float lightCenterX = lightButtonRect.anchoredPosition.x +
                             lightButtonRect.rect.width * (0.5f - lightButtonRect.pivot.x);
        float lightBottom = lightButtonRect.anchoredPosition.y -
                            lightButtonRect.rect.height * lightButtonRect.pivot.y;
        panelRoot.anchoredPosition = new Vector2(
            lightCenterX,
            lightBottom - 8f
        );
    }

    private TMP_FontAsset FindFont()
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
            if (fonts[i] != null && fonts[i].name.IndexOf("NunitoSans-Premium", StringComparison.OrdinalIgnoreCase) >= 0)
                return fonts[i];

        TMP_Text existing = GetComponentInChildren<TMP_Text>(true);
        if (existing != null && existing.font != null)
            return existing.font;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private static RectTransform CreateRect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private static LowPolyPanelGraphic CreatePanel(Transform parent, string name, Color color, bool raycast)
    {
        RectTransform rect = CreateRect(parent, name);
        LowPolyPanelGraphic panel = rect.gameObject.AddComponent<LowPolyPanelGraphic>();
        panel.color = color;
        panel.raycastTarget = raycast;
        if (Mathf.Abs(color.r - PremiumUiStyle.Navy.r) < 0.01f ||
            Mathf.Abs(color.r - PremiumUiStyle.NavyLift.r) < 0.01f ||
            Mathf.Abs(color.r - PremiumUiStyle.Night.r) < 0.01f)
            PremiumUiStyle.ConfigureDarkSurface(panel, 12f, 3f);
        return panel;
    }

    private static TMP_Text CreateText(
        Transform parent, string name, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(parent, name);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }
}
