using System.Collections;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-bootstrapped overlay for a finished room (or catalog) collection.
/// COLLECT is the only grant path.
/// </summary>
[DisallowMultipleComponent]
public sealed class CollectionCompleteCelebrationView : MonoBehaviour
{
    private const int SortingOrder = 290;

    private static CollectionCompleteCelebrationView instance;

    private CanvasGroup rootGroup;
    private RectTransform panel;
    private TMP_Text titleText;
    private TMP_Text detailText;
    private TMP_Text rewardText;
    private Button collectButton;
    private Coroutine routine;
    private CollectionMilestone current;
    private bool isOpen;
    private bool claimed;

    public static bool IsAnyOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;
        var canvasObject = new GameObject("CollectionCompleteCanvas");
        DontDestroyOnLoad(canvasObject);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        instance = canvasObject.AddComponent<CollectionCompleteCelebrationView>();
        instance.Build();
        instance.HideImmediate();
        CollectionMilestoneService.Completed -= instance.HandleCompleted;
        CollectionMilestoneService.Completed += instance.HandleCompleted;
    }

    private void OnDestroy()
    {
        CollectionMilestoneService.Completed -= HandleCompleted;
        if (instance == this)
            instance = null;
        IsAnyOpen = false;
    }

    private void HandleCompleted(CollectionMilestone milestone)
    {
        if (TitleScreen.IsShowing)
            return;
        Show(milestone);
    }

    private void Update()
    {
        if (isOpen || TitleScreen.IsShowing || HomeLevelUpCelebrationView.IsAnyOpen)
            return;
        if (!CollectionMilestoneService.TryPeekPending(out CollectionMilestone pending))
            return;
        Show(pending);
    }

    private void Show(CollectionMilestone milestone)
    {
        current = milestone;
        claimed = false;
        isOpen = true;
        IsAnyOpen = true;
        if (titleText != null)
            titleText.text = milestone.Title;
        if (detailText != null)
            detailText.text = CollectionMilestoneService.FormatOwnedLabel();
        if (rewardText != null)
        {
            string reward = "+" + milestone.Coins + " COINS";
            if (milestone.Diamonds > 0)
                reward += "  •  +" + milestone.Diamonds + " DIAMOND";
            if (milestone.BondXp > 0)
                reward += "  •  +" + milestone.BondXp + " BOND";
            rewardText.text = reward;
        }

        gameObject.SetActive(true);
        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }
        if (panel != null)
            panel.localScale = CatRunnerProgressService.ReducedMotion
                ? Vector3.one
                : Vector3.one * 0.86f;
        if (routine != null)
            StopCoroutine(routine);
        if (!CatRunnerProgressService.ReducedMotion)
            routine = StartCoroutine(PopIn());
        HomeAudioController.PlayCelebration();
    }

    private IEnumerator PopIn()
    {
        float elapsed = 0f;
        while (elapsed < 0.28f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.28f);
            if (panel != null)
                panel.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, t);
            yield return null;
        }
        if (panel != null)
            panel.localScale = Vector3.one;
        routine = null;
    }

    private void OnCollect()
    {
        if (claimed)
            return;
        claimed = true;
        CollectionMilestoneService.TryClaim(current.Id);
        HideImmediate();
        isOpen = false;
        IsAnyOpen = false;
    }

    private void HideImmediate()
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(true);
    }

    private void Build()
    {
        TMP_FontAsset font = FindFont();
        RectTransform root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        rootGroup = gameObject.AddComponent<CanvasGroup>();

        RectTransform scrim = NewRect(root, "Scrim", Vector2.zero, Vector2.zero);
        Stretch(scrim);
        Image scrimImage = scrim.gameObject.AddComponent<Image>();
        scrimImage.color = new Color(0.12f, 0.08f, 0.22f, 0.45f);
        scrimImage.raycastTarget = true;

        panel = NewRect(root, "Card", new Vector2(760f, 520f), Vector2.zero);
        LowPolyPanelGraphic rim = AddPanel(panel.gameObject, new Color32(255, 205, 64, 255), 28f, 8f);
        rim.raycastTarget = true;
        RectTransform face = NewRect(panel, "Face", new Vector2(728f, 488f), Vector2.zero);
        AddPanel(face.gameObject, new Color32(255, 242, 214, 255), 24f, 7f);

        RectTransform banner = NewRect(face, "Banner", new Vector2(680f, 88f), new Vector2(0f, 170f));
        AddPanel(banner.gameObject, PremiumUiStyle.CandyPink, 22f, 6f);
        titleText = NewText(banner, "Title", font, "COLLECTION COMPLETE!", 36f, Color.white);
        Stretch(titleText.rectTransform);

        detailText = NewText(face, "Detail", font, "10 OF 10 • COLLECTED", 26f,
            PremiumUiStyle.Ink);
        SetSize(detailText.rectTransform, new Vector2(640f, 44f), new Vector2(0f, 70f));

        rewardText = NewText(face, "Reward", font, "+500 COINS", 28f,
            new Color32(70, 54, 92, 255));
        SetSize(rewardText.rectTransform, new Vector2(640f, 48f), new Vector2(0f, 10f));

        RectTransform collect = NewRect(face, "CollectButton", new Vector2(360f, 92f),
            new Vector2(0f, -150f));
        RectTransform visual = NewRect(collect, "Visual", Vector2.zero, Vector2.zero);
        Stretch(visual);
        LowPolyPanelGraphic collectFace = AddPanel(visual.gameObject, PremiumUiStyle.CandyMint, 28f, 8f);
        collectFace.raycastTarget = true;
        TMP_Text collectLabel = NewText(visual, "Label", font, "COLLECT", 36f, Color.white);
        Stretch(collectLabel.rectTransform);
        collectButton = collect.gameObject.AddComponent<Button>();
        collectButton.targetGraphic = collectFace;
        collectButton.onClick.AddListener(OnCollect);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetSize(RectTransform rect, Vector2 size, Vector2 pos)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
    }

    private static RectTransform NewRect(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetSize(rect, size, pos);
        return rect;
    }

    private static LowPolyPanelGraphic AddPanel(GameObject go, Color color, float cut, float bevel)
    {
        if (go.GetComponent<CanvasRenderer>() == null)
            go.AddComponent<CanvasRenderer>();
        LowPolyPanelGraphic panel = go.AddComponent<LowPolyPanelGraphic>();
        panel.ConfigureTutorialStyle(color, cut, bevel);
        panel.raycastTarget = false;
        return panel;
    }

    private static TMP_Text NewText(
        Transform parent, string name, TMP_FontAsset font, string value, float size, Color color)
    {
        RectTransform rect = NewRect(parent, name, new Vector2(200f, 40f), Vector2.zero);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        text.characterSpacing = 1.2f;
        return text;
    }

    private static TMP_FontAsset FindFont()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
            if (texts[i] != null && texts[i].font != null)
                return texts[i].font;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }
}
