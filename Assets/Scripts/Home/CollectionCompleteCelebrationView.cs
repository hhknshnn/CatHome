using System.Collections;
using CatHome.Economy;
using TMPro;
using U = PremiumUiElements;
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
    private PremiumModalBackdrop backdrop;
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

    private void OnDisable() => HideImmediate();

    private void HandleCompleted(CollectionMilestone milestone)
    {
        if (!CanPresent)
            return;
        Show(milestone);
    }

    public static bool CanPresent => !TitleScreen.IsShowing && !HomeUiFlow.IsHomeControlBlocked &&
        !ShopPanelController.IsAnyOpen && !CatBreedShopPanel.IsAnyOpen && !RoomSelectorPanel.IsAnyOpen &&
        !QuestPanelController.IsAnyOpen && !SettingsPanel.IsAnyOpen && !PrivacyDataPanel.IsAnyOpen &&
        !GamesHubPanel.IsAnyOpen && !LeaderboardPanel.IsAnyOpen && !CatCompanionPanel.IsAnyOpen && CatActivity.Active==null &&
        !(FindFirstObjectByType<MainPanelController>() is MainPanelController menu && menu.IsOpen);

    private void Update()
    {
        if (isOpen)
            return;
        if (!CollectionMilestoneService.TryPeekPending(out CollectionMilestone pending))
            return;
        if(!CanPresent)return;
        Show(pending);
    }

    private void Show(CollectionMilestone milestone)
    {
        current = milestone;
        claimed = false;
        isOpen = true;
        IsAnyOpen = true;
        if (titleText != null)
            titleText.text = GameLanguageService.Text("celebration.collection");
        if (detailText != null)
            detailText.text = milestone.IsCatalogWide
                ? GameContentCopy.Text($"{CollectionMilestoneService.CatalogSize} eşyanın hepsi senin!",$"All {CollectionMilestoneService.CatalogSize} items collected!")
                : HomeRoomService.GetOrLivingRoom(milestone.RoomId).DisplayName + " · " + GameContentCopy.Text("10 / 10 eşya", "10 / 10 items");
        if (rewardText != null)
        {
            string reward = GameLanguageService.Format("celebration.coins",milestone.Coins);
            if (milestone.Diamonds > 0)
                reward += "  •  +" + milestone.Diamonds + " " + GameLanguageService.Text("diamonds.units");
            if (milestone.BondXp > 0)
                reward += "  •  +" + milestone.BondXp + " " + GameLanguageService.Text("currency.bond");
            rewardText.text = reward;
        }

        gameObject.SetActive(true);
        if (backdrop != null)
            backdrop.enabled = true;
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
        JoyfulUiArt.Celebrate(panel);
    }

    private IEnumerator PopIn()
    {
        float elapsed = 0f;
        while (elapsed < 0.28f && !CatRunnerProgressService.ReducedMotion)
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
    }

    private void HideImmediate()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        isOpen = false;
        IsAnyOpen = false;
        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }
        if (backdrop != null)
            backdrop.enabled = false;
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
        backdrop = scrim.gameObject.AddComponent<PremiumModalBackdrop>();

        panel=NewRect(root,"Card",new Vector2(1060,620),Vector2.zero);
        PremiumMomentArt.FitParent(panel,1060,620);
        var face=U.Panel("Face",panel,PremiumUiStyle.Ivory,0,0,1060,620,32,true);
        var stage=PremiumMomentArt.Stage(face.transform,-326,0,338,552);
        var portrait=U.Rect("CatPortrait",stage); U.At(portrait,0,36,244,244);
        portrait.gameObject.AddComponent<Image>().raycastTarget=false; portrait.gameObject.AddComponent<SelectedCatPortrait>();
        PremiumMomentArt.Caption(stage,"Birlikte büyüyen\nbir yuva.","A home we build\ntogether.",0,-169,284,92,27);
        PremiumMomentArt.Caption(face.transform,"KOLEKSİYON ANI","COLLECTION MOMENT",176,242,574,40,19);
        titleText=U.Label("Title",face.transform,font,43,PremiumUiStyle.Ink,176,159,574,122,TextAlignmentOptions.Center);
        detailText=U.Label("Detail",face.transform,font,24,PremiumUiStyle.Muted,176,42,574,72,TextAlignmentOptions.Center);
        PremiumMomentArt.RewardTray(face.transform,176,-75,574,112);
        rewardText=U.Label("Reward",face.transform,font,27,PremiumUiStyle.Teal,212,-75,464,100,TextAlignmentOptions.Center);
        collectButton=U.Action("CollectButton",face.transform,font,"quests.claim",PremiumUiStyle.Coral,176,-222,574,80,out var collectLabel);
        collectButton.onClick.AddListener(OnCollect);
        JoyfulUiArt.ActionStyle(collectButton,JoyfulUiArt.Coral);
        JoyfulUiArt.Surface(face,JoyfulUiArt.Paper,32);
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
