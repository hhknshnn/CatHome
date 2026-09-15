using System.Collections;
using System.Collections.Generic;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modal home store. It presents the catalog, delegates every purchase to
/// HomeStoreService and only owns screen animation/input coordination.
/// </summary>
[DisallowMultipleComponent]
public sealed class ShopPanelController : MonoBehaviour
{
    private enum PanelState
    {
        Closed,
        Opening,
        Open,
        Closing
    }

    [System.Serializable]
    private struct ProductCard
    {
        public string productId;
        public Button button;
        public CanvasGroup group;
        public TMP_Text categoryText;
        public TMP_Text titleText;
        public TMP_Text descriptionText;
        public TMP_Text priceText;
        public GameObject currencyPriceGroup;
        public TMP_Text coinPriceText;
        public TMP_Text diamondPriceText;
        public RawImage previewImage;
        public TMP_Text actionText;
        public Graphic actionBackground;
        public GameObject ownedBadge;
    }

    [System.Serializable]
    private struct ShopTab
    {
        public HomeStoreCategory category;
        public Button button;
        public Graphic background;
        public TMP_Text label;
        public GameObject selectedMarker;
    }

    [Header("Input")]
    [SerializeField] private CatMovement catMovement;

    [Header("Canvas")]
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private CanvasGroup scrimGroup;
    [SerializeField] private Button scrimButton;

    [Header("Panel")]
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text ownedCountText;
    [SerializeField] private TMP_Text homeLevelText;
    [SerializeField] private TMP_Text sectionTitleText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private ScrollRect productScrollRect;
    [SerializeField] private ShopTab[] tabs = new ShopTab[0];
    [SerializeField] private ProductCard[] cards = new ProductCard[0];

    [Header("Purchase dialog")]
    [SerializeField] private CanvasGroup purchaseGroup;
    [SerializeField] private RawImage purchaseIcon;
    [SerializeField] private GameObject requestedProductRoot;
    [SerializeField] private RawImage requestedProductIcon;
    [SerializeField] private TMP_Text requestedProductLabel;
    [SerializeField] private TMP_Text purchasePlacementText;
    [SerializeField] private RawImage diamondConfirmationIcon;
    [SerializeField] private TMP_Text purchaseTitleText;
    [SerializeField] private TMP_Text purchaseMessageText;
    [SerializeField] private Button purchaseCoinButton;
    [SerializeField] private TMP_Text purchaseCoinButtonText;
    [SerializeField] private Button purchaseDiamondButton;
    [SerializeField] private TMP_Text purchaseDiamondButtonText;
    [SerializeField] private Button purchaseCancelButton;
    [SerializeField] private Button purchaseLaterButton;

    [Header("Diamond confirmation")]
    [SerializeField] private CanvasGroup diamondConfirmationGroup;
    [SerializeField] private TMP_Text diamondConfirmationTitle;
    [SerializeField] private TMP_Text diamondConfirmationMessage;
    [SerializeField] private TMP_Text diamondConfirmationButtonText;
    [SerializeField] private Button diamondConfirmationButton;
    [SerializeField] private Button diamondConfirmationCancelButton;
    [SerializeField] private DiamondStorePanel diamondStorePanel;

    [Header("Placement mode")]
    [SerializeField] private CanvasGroup placementGroup;
    [SerializeField] private CanvasGroup placementDragGroup;
    [SerializeField] private TMP_Text placementTitleText;
    [SerializeField] private TMP_Text placementCounterText;
    [SerializeField] private Button placementPreviousButton;
    [SerializeField] private Button placementNextButton;
    [SerializeField] private Button placementConfirmButton;
    [SerializeField] private Button placementCancelButton;

    [Header("Responsive size")]
    [SerializeField, Range(0.35f, 0.9f)] private float widthFraction = 0.9f;
    [SerializeField, Min(0f)] private float minWidth = 1460f;
    [SerializeField, Min(0f)] private float maxWidth = 1720f;
    [SerializeField, Min(0f)] private float safeAreaMargin = 34f;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float openCloseDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.14f;
    [SerializeField, Min(0f)] private float cardStagger = 0.035f;
    [SerializeField, Min(0.01f)] private float cardFadeDuration = 0.11f;
    [SerializeField, Range(0.7f, 1f)] private float revealScaleFrom = 0.94f;
    [SerializeField, Range(0f, 1f)] private float scrimTargetAlpha = 0.68f;

    private static readonly Color BuyColor = JoyfulUiArt.Ocean;
    private static readonly Color OwnedColor = JoyfulUiArt.SkyPaper;
    private static readonly Color NeedColor = JoyfulUiArt.Paper;
    private static readonly Color TabActiveColor = PremiumUiStyle.Teal;
    private static readonly Color TabIdleColor = PremiumUiStyle.Mint;
    private static readonly Color TabActiveTextColor = Color.white;
    private static readonly Color TabIdleTextColor = PremiumUiStyle.Ink;

    private static string DefaultFeedback => HomeStoreService.FreePurchaseTestingEnabled
        ? GameContentCopy.Text("Ücretsiz deneme · Edin düğmesi bakiyeni harcamaz.","Free test · Get an item without spending your balance.")
        : GameContentCopy.Text("Oyunlarda jeton kazan, kedine mutlu bir ev kur.","Earn coins in games and build a happy home for your cat.");

    private PanelState state = PanelState.Closed;
    private Coroutine animationRoutine;
    private float animTime;
    private float fitScale = 1f;
    private bool inputBlockHeld;
    private bool listenersBound;
    private WhileYouWereAwayPopup offlinePopup;
    private HomeProductPlacement activePlacement;
    private bool activePlacementWasStored;
    private bool placementMode;
    private HomeStoreCategory activeCategory = HomeStoreCategory.Cat;
    private bool purchaseDialogOpen;
    private bool diamondConfirmationOpen;
    private string pendingPurchaseProductId;
    private string pendingRequiredForProductId;

    public static bool IsAnyOpen { get; private set; }
    public bool IsOpen => state == PanelState.Open || state == PanelState.Opening;

    private void Awake()
    {
        PremiumScrollInput.Ensure(productScrollRect);
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>();

        BindListeners();
        animTime = 0f;
        ApplyVisuals();
        SetRaycasts(false);
        SetPlacementUi(false);
        SetPurchaseDialogVisible(false);
        SetDiamondConfirmationVisible(false);
        if (diamondStorePanel != null)
            diamondStorePanel.Close();
        SetFeedback(DefaultFeedback);
        RefreshStore();
        if (productScrollRect != null && productScrollRect.content != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(productScrollRect.content);
            productScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        ApplyResponsiveLayout();
        ApplyVisuals();
    }

    private void OnEnable()
    {
        BindListeners();
        EconomyService.AnyBalanceChanged += RefreshStore;
        HomeStoreService.OwnershipChanged += HandleOwnershipChanged;
        HomeStoreService.StorageChanged += HandleOwnershipChanged;
        HomeRoomService.CurrentRoomChanged += HandleCurrentRoomChanged;
        HomeProgressionService.Changed += RefreshStore;
        GameLanguageService.Changed += RefreshStore;
        RefreshStore();
    }

    private void Update()
    {
        if (IsOpen && IsBlockingModalActive())
        {
            ForceHideImmediate();
            return;
        }

        if (state != PanelState.Closed)
        {
            ApplyResponsiveLayout();
            ApplyVisuals();
        }
    }

    public void RequestOpen()
    {
        RequestOpen(HomeStoreCategory.Cat);
    }

    public void RequestOpen(HomeStoreCategory category)
    {
        if (state != PanelState.Closed && state != PanelState.Closing)
            return;
        if (IsBlockingModalActive())
            return;

        // Category selection owns the footer copy as well as the visible cards.
        // Opening directly into ROOM must show its set/budget guidance instead
        // of inheriting CAT's generic earning hint.
        SetCategory(category, true);
        placementMode = false;
        if (panel != null)
            panel.gameObject.SetActive(true);
        SetPlacementUi(false);
        IsAnyOpen = true;
        AcquireInputBlock();
        ApplyResponsiveLayout();
        SetRaycasts(true);
        state = PanelState.Opening;
        StartAnimation(true);
    }

    public void RequestDiamondStore()
    {
        if (!IsOpen)
            RequestOpen(HomeStoreCategory.Cat);
        if (diamondStorePanel != null)
            diamondStorePanel.Open();
    }

    public void RequestClose()
    {
        if (diamondConfirmationOpen)
        {
            CloseDiamondConfirmation();
            return;
        }

        if (diamondStorePanel != null && diamondStorePanel.IsOpen)
        {
            diamondStorePanel.Close();
            return;
        }

        if (purchaseDialogOpen)
        {
            ClosePurchaseDialog();
            return;
        }

        if (placementMode)
        {
            FinishPlacement(false);
            return;
        }

        BeginClosing();
    }

    private void BeginClosing()
    {
        if (state != PanelState.Open && state != PanelState.Opening)
            return;

        if (rootGroup != null)
            rootGroup.interactable = false;
        state = PanelState.Closing;
        StartAnimation(false);
    }

    public void Toggle()
    {
        if (IsOpen)
            RequestClose();
        else
            RequestOpen();
    }

    private void OnProductSelected(string productId)
    {
        if (!HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
        {
            SetFeedback("THIS PRODUCT IS NOT AVAILABLE.");
            return;
        }

        foreach(var room in HomeRoomService.Rooms)
        {
            string roomProduct=room.IsAlwaysUnlocked?HomeStoreService.HomeRoomsPreviewId:room.RequiredOwnershipId;
            if(roomProduct!=productId || !HomeRoomService.IsRoomUnlocked(room.Id))continue;
            if(HomeRoomService.CurrentRoomId==room.Id)
                SetFeedback(GameContentCopy.Text("Zaten bu odadasın.","You’re already in this room."));
            else
            {
                var loader=FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
                if(loader!=null && loader.LoadRoom(room.Id))RequestClose();
            }
            return;
        }

        if (HomeStoreService.IsOwned(productId) && product.IsPlaceable)
        {
            if (CatCollectionPolicy.IsCatItem(productId) && !HomeStoreService.IsStored(productId))
            {
                HomeStoreService.TrySetStored(productId, true);
                SetFeedback(GameContentCopy.Text("Koleksiyona kaldırıldı. İstediğinde yeniden ekleyebilirsin.", "Put away. Add it again whenever you like."));
                RefreshStore(); return;
            }
            BeginPlacement(productId);
            return;
        }

        if (!product.IsAvailable || (!product.SupportsCoins && !product.SupportsDiamonds))
        {
            SetFeedback("THIS COLLECTION IS READY FOR FUTURE CONTENT.");
            return;
        }

        if (HomeStoreService.FreePurchaseTestingEnabled)
        {
            HomeStorePurchaseResult acquired =
                HomeStoreService.TryAcquireForTesting(product.Id);
            if (acquired.Status == HomeStorePurchaseStatus.Purchased)
            {
                SetFeedback(acquired.Product.Title + " ADDED  •  FREE TEST MODE");
                RefreshStore();
                if (acquired.Product.IsPlaceable &&
                    HomeStoreService.IsProductInRoomCollection(
                        HomeRoomService.CurrentRoomId,
                        acquired.Product.Id))
                {
                    BeginPlacement(acquired.Product.Id);
                }
            }
            else if (acquired.Status == HomeStorePurchaseStatus.AlreadyOwned &&
                     acquired.Product.IsPlaceable)
            {
                BeginPlacement(acquired.Product.Id);
            }
            else
            {
                SetFeedback("THIS ITEM COULD NOT BE ADDED IN TEST MODE.");
            }
            return;
        }

        if (!HomeStoreService.MeetsHomeLevelRequirement(product))
        {
            SetFeedback("REACH HOME LEVEL " + product.RequiredLevel + " TO UNLOCK THIS ITEM.");
            return;
        }

        if (product.Id == HomeStoreService.HomeBathroomPreviewId &&
            !HomeStoreService.IsLivingRoomComplete)
        {
            SetFeedback("COMPLETE " +
                        (HomeStoreService.LivingRoomItemCount -
                         HomeStoreService.LivingRoomOwnedCount) +
                        " MORE LIVING ROOM ITEMS TO UNLOCK BATHROOM.");
            return;
        }

        if (product.Id == HomeStoreService.HomeKitchenPreviewId &&
            !HomeStoreService.IsBathroomComplete)
        {
            SetFeedback("COMPLETE " +
                        (HomeStoreService.BathroomItemCount -
                         HomeStoreService.BathroomOwnedCount) +
                        " MORE BATHROOM ITEMS TO UNLOCK KITCHEN.");
            return;
        }

        if (product.Id == HomeStoreService.HomeBedroomPreviewId &&
            !HomeStoreService.IsKitchenComplete)
        {
            SetFeedback("COMPLETE " +
                        (HomeStoreService.KitchenItemCount -
                         HomeStoreService.KitchenOwnedCount) +
                        " MORE KITCHEN ITEMS TO UNLOCK BEDROOM.");
            return;
        }

        if (product.Id == HomeStoreService.HomeGardenPreviewId &&
            !HomeStoreService.IsBedroomComplete)
        {
            SetFeedback("COMPLETE " +
                        (HomeStoreService.BedroomItemCount -
                         HomeStoreService.BedroomOwnedCount) +
                        " MORE BEDROOM ITEMS TO UNLOCK GARDEN.");
            return;
        }

        if (product.Id == HomeStoreService.HomeBalconyPreviewId &&
            !HomeStoreService.IsGardenComplete)
        {
            SetFeedback("COMPLETE " +
                        (HomeStoreService.GardenItemCount -
                         HomeStoreService.GardenOwnedCount) +
                        " MORE GARDEN ITEMS TO UNLOCK BALCONY.");
            return;
        }

        if (product.Id == HomeStoreService.HomePatioPreviewId &&
            !HomeStoreService.IsBalconyComplete)
        {
            SetFeedback("COMPLETE " +
                        (HomeStoreService.BalconyItemCount -
                         HomeStoreService.BalconyOwnedCount) +
                        " MORE BALCONY ITEMS TO UNLOCK GARDEN PATIO.");
            return;
        }

        if (product.Id == HomeStoreService.HomeSecondFloorPreviewId &&
            !HomeStoreService.IsPatioComplete)
        {
            SetFeedback("COMPLETE " +
                        (HomeStoreService.PatioItemCount -
                         HomeStoreService.PatioOwnedCount) +
                        " MORE PATIO ITEMS TO UNLOCK SECOND FLOOR.");
            return;
        }

        string requiredId = HomeStoreService.GetRequiredProductId(product.Id);
        if (!string.IsNullOrEmpty(requiredId) && !HomeStoreService.IsOwned(requiredId))
        {
            OpenPurchaseDialog(requiredId, product.Id);
            return;
        }

        OpenPurchaseDialog(product.Id, null);
    }

    private void OpenPurchaseDialog(string purchaseProductId, string requiredForProductId)
    {
        if (!HomeStoreService.TryGetProduct(purchaseProductId, out HomeStoreProduct product))
        {
            SetFeedback("THIS PRODUCT IS NOT AVAILABLE.");
            return;
        }

        pendingPurchaseProductId = product.Id;
        pendingRequiredForProductId = requiredForProductId;
        purchaseDialogOpen = true;
        SetPurchaseDialogVisible(true);
        RefreshPurchaseDialog();
    }

    private void RefreshPurchaseDialog()
    {
        if (!purchaseDialogOpen ||
            !HomeStoreService.TryGetProduct(
                pendingPurchaseProductId,
                out HomeStoreProduct product))
        {
            return;
        }

        if (purchaseTitleText != null)
        {
            purchaseTitleText.text = string.IsNullOrEmpty(pendingRequiredForProductId)
                ? product.Title
                : GameLanguageService.Format("shop.required", product.Title);
        }

        if (purchaseMessageText != null)
        {
            if (!string.IsNullOrEmpty(pendingRequiredForProductId) &&
                HomeStoreService.TryGetProduct(
                    pendingRequiredForProductId,
                    out HomeStoreProduct requestedProduct))
            {
                purchaseMessageText.text = GameLanguageService.Format("shop.required_body", requestedProduct.Title, product.Title);
            }
            else
            {
                purchaseMessageText.text =
                    product.Description;
            }
        }

        if (purchaseIcon != null)
            purchaseIcon.texture = GetProductPreviewTexture(product.Id);

        bool prerequisite=!string.IsNullOrEmpty(pendingRequiredForProductId);
        if(requestedProductRoot!=null) requestedProductRoot.SetActive(prerequisite);
        if(prerequisite && HomeStoreService.TryGetProduct(pendingRequiredForProductId,out var requested))
        {
            if(requestedProductIcon!=null) requestedProductIcon.texture=GetProductPreviewTexture(requested.Id);
            if(requestedProductLabel!=null) requestedProductLabel.text=GameLanguageService.Format("shop.requested",requested.Title);
        }
        if(purchaseIcon!=null)
        {
            purchaseIcon.rectTransform.sizeDelta=Vector2.one*(prerequisite?378:492);
            purchaseIcon.rectTransform.anchoredPosition=new Vector2(-318,prerequisite?72:22);
            purchaseIcon.uvRect=product.StoreCategory==HomeStoreCategory.Home ? RoomPreviewFit.CoverUv(1920,1080,purchaseIcon.rectTransform.rect.width,purchaseIcon.rectTransform.rect.height) : new Rect(0,0,1,1);
        }
        if(purchasePlacementText!=null) purchasePlacementText.text=GameLanguageService.Text(
            product.StoreCategory==HomeStoreCategory.Home?"shop.room_purchase":
            HomeStoreService.IsFixedRoomProduct(product.Id)?"shop.placement":"shop.cat_placement");

        PurchasePreview coin = EconomyService.PreviewPurchase(
            CurrencyType.Coin,
            product.CoinPrice);
        if (purchaseCoinButton != null)
            purchaseCoinButton.interactable = product.SupportsCoins && coin.CanAfford;
        if (purchaseDiamondButton != null)
            purchaseDiamondButton.interactable = product.SupportsDiamonds;

        if (purchaseCoinButtonText != null)
        {
            purchaseCoinButtonText.text = coin.CanAfford
                ? GameLanguageService.Format("shop.buy_coins", product.CoinPrice.ToString("N0"))
                : GameLanguageService.Format("shop.need_coins", coin.Missing.ToString("N0"));
        }
        if (purchaseDiamondButtonText != null)
            purchaseDiamondButtonText.text =
                GameLanguageService.Format("shop.buy_diamonds", product.DiamondPrice.ToString("N0"));
    }

    private void PurchaseWithCoins()
    {
        PurchasePendingProduct(CurrencyType.Coin);
    }

    private void PurchaseWithDiamonds()
    {
        if (!purchaseDialogOpen ||
            !HomeStoreService.TryGetProduct(
                pendingPurchaseProductId,
                out HomeStoreProduct product) ||
            !product.SupportsDiamonds)
        {
            return;
        }

        PurchasePreview preview = EconomyService.PreviewPurchase(
            CurrencyType.Diamond,
            product.DiamondPrice);
        if (!preview.CanAfford)
        {
            CloseDiamondConfirmation();
            if (diamondStorePanel != null)
                diamondStorePanel.Open(product.DiamondPrice);
            return;
        }

        diamondConfirmationOpen = true;
        if (diamondConfirmationTitle != null)
            diamondConfirmationTitle.text = GameLanguageService.Text("shop.confirm");
        if (diamondConfirmationMessage != null)
            diamondConfirmationMessage.text =
                GameLanguageService.Format("shop.confirm_body",product.Title,product.DiamondPrice.ToString("N0"));
        if (diamondConfirmationButtonText != null)
            diamondConfirmationButtonText.text =
                GameLanguageService.Format("shop.buy_diamonds", product.DiamondPrice.ToString("N0"));
        if(diamondConfirmationIcon!=null) diamondConfirmationIcon.texture=GetProductPreviewTexture(product.Id);
        SetDiamondConfirmationVisible(true);
    }

    private void ConfirmDiamondPurchase()
    {
        CloseDiamondConfirmation();
        PurchasePendingProduct(CurrencyType.Diamond);
    }

    private void CloseDiamondConfirmation()
    {
        diamondConfirmationOpen = false;
        SetDiamondConfirmationVisible(false);
    }

    private void SetDiamondConfirmationVisible(bool visible)
    {
        if (diamondConfirmationGroup == null)
            return;
        diamondConfirmationGroup.alpha = visible ? 1f : 0f;
        diamondConfirmationGroup.interactable = visible;
        diamondConfirmationGroup.blocksRaycasts = visible;
    }

    private void PurchasePendingProduct(CurrencyType currency)
    {
        if (!purchaseDialogOpen || string.IsNullOrEmpty(pendingPurchaseProductId))
            return;

        HomeStorePurchaseResult result = HomeStoreService.TryPurchase(
            pendingPurchaseProductId,
            currency);
        bool beginPlacement = false;
        if (result.Status != HomeStorePurchaseStatus.Purchased && result.Status != HomeStorePurchaseStatus.AlreadyOwned &&
            result.Status != HomeStorePurchaseStatus.RequiredProductMissing) GameAudio.UI(AudioCue.UIError);
        switch (result.Status)
        {
            case HomeStorePurchaseStatus.Purchased:
                SetFeedback(result.Product.Title + " ADDED TO YOUR HOME!");
                beginPlacement = result.Product.IsPlaceable;
                ClosePurchaseDialog();
                break;
            case HomeStorePurchaseStatus.AlreadyOwned:
                SetFeedback("YOU ALREADY OWN THIS ITEM.");
                ClosePurchaseDialog();
                break;
            case HomeStorePurchaseStatus.InsufficientCoins:
                SetFeedback("YOU NEED " + result.MissingAmount +
                            " MORE COINS. TRY CAT RUNNER OR USE DIAMONDS.");
                break;
            case HomeStorePurchaseStatus.InsufficientDiamonds:
                SetFeedback("YOU NEED " + result.MissingAmount +
                            " MORE DIAMONDS.");
                break;
            case HomeStorePurchaseStatus.RequiredProductMissing:
                OpenPurchaseDialog(
                    HomeStoreService.GetRequiredProductId(result.Product.Id),
                    result.Product.Id);
                break;
            default:
                SetFeedback("PURCHASE COULD NOT BE COMPLETED. PLEASE TRY AGAIN.");
                break;
        }

        RefreshStore();
        RefreshPurchaseDialog();
        if (beginPlacement)
            BeginPlacement(result.Product.Id);
    }

    private void ClosePurchaseDialog()
    {
        CloseDiamondConfirmation();
        if (diamondStorePanel != null)
            diamondStorePanel.Close();
        purchaseDialogOpen = false;
        pendingPurchaseProductId = null;
        pendingRequiredForProductId = null;
        SetPurchaseDialogVisible(false);
    }

    private void SetPurchaseDialogVisible(bool visible)
    {
        if (purchaseGroup == null)
            return;
        purchaseGroup.alpha = visible ? 1f : 0f;
        purchaseGroup.interactable = visible;
        purchaseGroup.blocksRaycasts = visible;
    }

    private Texture GetProductPreviewTexture(string productId)
    {
        if (cards == null)
            return null;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].productId == productId && cards[i].previewImage != null)
                return cards[i].previewImage.texture;
        }
        return null;
    }

    private void HandleOwnershipChanged(string productId)
    {
        RefreshStore();
    }

    private void HandleCurrentRoomChanged(string roomId)
    {
        RefreshStore();
    }

    private void RefreshStore()
    {
        if (balanceText != null)
            balanceText.text = EconomyService.Coins.ToString("N0");

        int ownedCount = 0;
        int categoryCount = 0;
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                ProductCard card = cards[i];
                if (!HomeStoreService.TryGetProduct(card.productId, out HomeStoreProduct product))
                    continue;

                bool visible = IsVisibleInActiveCollection(product);
                if (card.group != null)
                    card.group.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                categoryCount++;

                bool owned = HomeStoreService.IsOwned(product.Id) ||
                             product.Id == HomeStoreService.HomeRoomsPreviewId;
                if (owned)
                    ownedCount++;

                if (card.categoryText != null)
                    card.categoryText.text = HomeStoreService.GetCatalogEyebrow(
                        product.Id, product.Category);
                if (card.titleText != null)
                    card.titleText.text = product.Title;
                if (card.descriptionText != null)
                    card.descriptionText.text = product.Description;
                bool showsCurrencyPrice = ShowsCurrencyPrice(product);
                if (card.currencyPriceGroup != null)
                    card.currencyPriceGroup.SetActive(showsCurrencyPrice);
                if (card.priceText != null)
                {
                    card.priceText.gameObject.SetActive(!showsCurrencyPrice);
                    card.priceText.text = BuildPriceText(product);
                }
                if (card.coinPriceText != null)
                    card.coinPriceText.text = product.SupportsCoins
                        ? product.CoinPrice.ToString("N0")
                        : string.Empty;
                if (card.diamondPriceText != null)
                    card.diamondPriceText.text = product.SupportsDiamonds
                        ? product.DiamondPrice.ToString("N0")
                        : string.Empty;
                if (card.ownedBadge != null)
                {
                    card.ownedBadge.SetActive(owned);
                    if (CatCollectionPolicy.IsCatItem(product.Id))
                    {
                        var badgeLabel = card.ownedBadge.GetComponentInChildren<TMP_Text>(true);
                        if (badgeLabel != null)
                        {
                            string key = HomeStoreService.IsStored(product.Id)
                                ? "shop.in_collection" : "shop.in_room";
                            var localized = badgeLabel.GetComponent<LocalizedLabel>();
                            if (localized != null)
                                localized.EditorConfigure(badgeLabel, key);
                            else
                                badgeLabel.text = GameLanguageService.Text(key);
                        }
                    }
                }

                if (card.button != null)
                    card.button.interactable = true;

                if (product.Id == HomeStoreService.HomeRoomsPreviewId)
                {
                    SetCardAction(card, "CURRENT ROOM", OwnedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         product.Id == HomeStoreService.HomeBathroomPreviewId &&
                         !HomeStoreService.IsLivingRoomComplete)
                {
                    int remaining = HomeStoreService.LivingRoomItemCount -
                                    HomeStoreService.LivingRoomOwnedCount;
                    SetCardAction(card, remaining + " ITEMS LEFT", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         product.Id == HomeStoreService.HomeKitchenPreviewId &&
                         !HomeStoreService.IsBathroomComplete)
                {
                    int remaining = HomeStoreService.BathroomItemCount -
                                    HomeStoreService.BathroomOwnedCount;
                    SetCardAction(card, remaining + " ITEMS LEFT", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         product.Id == HomeStoreService.HomeBedroomPreviewId &&
                         !HomeStoreService.IsKitchenComplete)
                {
                    int remaining = HomeStoreService.KitchenItemCount -
                                    HomeStoreService.KitchenOwnedCount;
                    SetCardAction(card, remaining + " ITEMS LEFT", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         product.Id == HomeStoreService.HomeGardenPreviewId &&
                         !HomeStoreService.IsBedroomComplete)
                {
                    int remaining = HomeStoreService.BedroomItemCount -
                                    HomeStoreService.BedroomOwnedCount;
                    SetCardAction(card, remaining + " ITEMS LEFT", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         product.Id == HomeStoreService.HomeBalconyPreviewId &&
                         !HomeStoreService.IsGardenComplete)
                {
                    int remaining = HomeStoreService.GardenItemCount -
                                    HomeStoreService.GardenOwnedCount;
                    SetCardAction(card, remaining + " ITEMS LEFT", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         product.Id == HomeStoreService.HomePatioPreviewId &&
                         !HomeStoreService.IsBalconyComplete)
                {
                    int remaining = HomeStoreService.BalconyItemCount -
                                    HomeStoreService.BalconyOwnedCount;
                    SetCardAction(card, remaining + " ITEMS LEFT", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         product.Id == HomeStoreService.HomeSecondFloorPreviewId &&
                         !HomeStoreService.IsPatioComplete)
                {
                    int remaining = HomeStoreService.PatioItemCount -
                                    HomeStoreService.PatioOwnedCount;
                    SetCardAction(card, remaining + " ITEMS LEFT", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         !HomeStoreService.IsProductDependencyMet(product.Id))
                {
                    SetCardAction(
                        card,
                        "BUY " + GetRequiredProductShortTitle(product.Id) + " FIRST",
                        NeedColor);
                }
                else if (owned)
                {
                    SetCardAction(card, CatCollectionPolicy.IsCatItem(product.Id) ?
                        (HomeStoreService.IsStored(product.Id) ? GameContentCopy.Text("Odaya ekle", "Add to room") : GameContentCopy.Text("Kaldır", "Put away")) :
                        HomeStoreService.IsFixedRoomProduct(product.Id) ? "IN YOUR ROOM" : product.IsPlaceable ? "MOVE ITEM" : "PREVIEW READY", OwnedColor);
                }
                else if (!product.IsAvailable ||
                         (!product.SupportsCoins && !product.SupportsDiamonds))
                {
                    SetCardAction(card, "COMING SOON", NeedColor);
                }
                else if (!HomeStoreService.FreePurchaseTestingEnabled &&
                         !HomeStoreService.MeetsHomeLevelRequirement(product))
                {
                    SetCardAction(
                        card,
                        "HOME LV. " + product.RequiredLevel + " REQUIRED",
                        NeedColor);
                }
                else if (HomeStoreService.FreePurchaseTestingEnabled || CanAfford(product))
                {
                    SetCardAction(card,
                        HomeStoreService.FreePurchaseTestingEnabled
                            ? "GET"
                            : product.StoreCategory == HomeStoreCategory.Home ? "UNLOCK" : "BUY",
                        BuyColor);
                }
                else
                {
                    SetCardAction(card, "SAVE MORE", NeedColor);
                }
            }
        }

        if (activeCategory == HomeStoreCategory.Room)
        {
            IReadOnlyList<string> roomCollection = HomeStoreService.GetRoomCollection(
                HomeRoomService.CurrentRoomId);
            ownedCount = HomeStoreService.GetRoomOwnedCount(HomeRoomService.CurrentRoomId);
            categoryCount = roomCollection.Count;
        }
        else if (activeCategory == HomeStoreCategory.Home)
        {
            ownedCount = 1 +
                         (HomeStoreService.IsOwned(HomeStoreService.HomeBathroomPreviewId) ? 1 : 0) +
                         (HomeStoreService.IsOwned(HomeStoreService.HomeKitchenPreviewId) ? 1 : 0) +
                         (HomeStoreService.IsOwned(HomeStoreService.HomeBedroomPreviewId) ? 1 : 0) +
                         (HomeStoreService.IsOwned(HomeStoreService.HomeGardenPreviewId) ? 1 : 0) +
                         (HomeStoreService.IsOwned(HomeStoreService.HomeBalconyPreviewId) ? 1 : 0) +
                         (HomeStoreService.IsOwned(HomeStoreService.HomePatioPreviewId) ? 1 : 0) +
                         (HomeStoreService.IsOwned(HomeStoreService.HomeSecondFloorPreviewId) ? 1 : 0);
            categoryCount = HomeRoomService.Rooms.Count;
        }

        if (ownedCountText != null)
            ownedCountText.text = GameLanguageService.Format("shop.progress", ownedCount, categoryCount);

        if (homeLevelText != null)
            homeLevelText.text = GameLanguageService.Format("title.home_level", HomeProgressionService.HomeLevel);

        if (sectionTitleText != null)
            sectionTitleText.text = GetSectionTitle(activeCategory) + (activeCategory==HomeStoreCategory.Cat ? " · " + CatCollectionPolicy.DisplayedCount + " / 5" : "");
        RefreshTabs();
        RefreshPurchaseDialog();
    }

    private static string BuildPriceText(HomeStoreProduct product)
    {
        if (HomeStoreService.FreePurchaseTestingEnabled && product.IsAvailable)
            return GameStatusCopy.Text("FREE TEST");
        if (product.Id == HomeStoreService.HomeRoomsPreviewId)
            return HomeStoreService.LivingRoomOwnedCount + " / " +
                   HomeStoreService.LivingRoomItemCount;
        if (!product.IsAvailable)
            return "LEVEL " + product.RequiredLevel;
        if (product.SupportsCoins && product.SupportsDiamonds)
        {
            return product.CoinPrice.ToString("N0") + "  •  D " +
                   product.DiamondPrice.ToString("N0");
        }
        if (product.SupportsCoins)
            return product.CoinPrice.ToString("N0");
        if (product.SupportsDiamonds)
            return product.DiamondPrice.ToString("N0");
        return "LEVEL " + product.RequiredLevel;
    }

    private static bool ShowsCurrencyPrice(HomeStoreProduct product)
    {
        return !HomeStoreService.FreePurchaseTestingEnabled &&
               product.Id != HomeStoreService.HomeRoomsPreviewId &&
               product.IsAvailable &&
               (product.SupportsCoins || product.SupportsDiamonds);
    }

    private static bool CanAfford(HomeStoreProduct product)
    {
        return (product.SupportsCoins &&
                EconomyService.PreviewPurchase(CurrencyType.Coin, product.CoinPrice).CanAfford) ||
               (product.SupportsDiamonds &&
                EconomyService.PreviewPurchase(CurrencyType.Diamond, product.DiamondPrice).CanAfford);
    }

    private void SetCategory(HomeStoreCategory category, bool updateFeedback = true)
    {
        activeCategory = category;
        if (updateFeedback)
        {
            SetFeedback(category == HomeStoreCategory.Cat
                ? GameContentCopy.Text("En fazla 5 eşya · 1 yatak. Kaldırdıkların koleksiyonunda kalır.", "Up to 5 items · 1 bed. Removed items stay in your collection.")
                : category == HomeStoreCategory.Room
                    ? BuildRoomEconomyFeedback()
                    : GameContentCopy.Text("Odaları tamamla · Yeni odaları jeton veya elmasla aç.","Complete rooms · Unlock new rooms with coins or diamonds."));
        }
        RefreshStore();
        if (productScrollRect != null && productScrollRect.content != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(productScrollRect.content);
            productScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private static string BuildRoomEconomyFeedback()
    {
        string id=HomeRoomService.CurrentRoomId;
        int owned=HomeStoreService.GetRoomOwnedCount(id),total=HomeStoreService.GetRoomCollection(id).Count;
        return GameContentCopy.Text($"Koleksiyonun {owned}/{total} · Eşyalar tasarlanan yerine eklenir.",$"Your collection {owned}/{total} · Items go into their designed places.");
    }

    private static string BuildNextRoomGoal(string roomId)
    {
        if (!HomeStoreService.TryGetNextRoomPurchaseGoal(
                roomId,
                out HomeStorePurchaseGoal goal))
        {
            return "COLLECTION READY";
        }

        string title = GetShortProductTitle(goal.Product.Id);
        if (HomeStoreService.FreePurchaseTestingEnabled)
            return "NEXT " + title + "  •  FREE TEST";
        return goal.CanAfford
            ? "NEXT " + title + "  •  READY TO BUY"
            : "NEXT " + title + "  •  NEED " +
              goal.MissingCoins.ToString("N0") + " COINS";
    }

    private bool IsVisibleInActiveCollection(HomeStoreProduct product)
    {
        if (product.StoreCategory != activeCategory)
            return false;
        if (activeCategory == HomeStoreCategory.Room)
        {
            return HomeStoreService.IsProductInRoomCollection(
                HomeRoomService.CurrentRoomId,
                product.Id);
        }
        if (activeCategory == HomeStoreCategory.Home)
        {
            return product.Id == HomeStoreService.HomeRoomsPreviewId ||
                   product.Id == HomeStoreService.HomeBathroomPreviewId ||
                   product.Id == HomeStoreService.HomeKitchenPreviewId ||
                   product.Id == HomeStoreService.HomeBedroomPreviewId ||
                   product.Id == HomeStoreService.HomeGardenPreviewId ||
                   product.Id == HomeStoreService.HomeBalconyPreviewId ||
                   product.Id == HomeStoreService.HomePatioPreviewId ||
                   product.Id == HomeStoreService.HomeSecondFloorPreviewId;
        }
        return true;
    }

    private void RefreshTabs()
    {
        if (tabs == null)
            return;
        for (int i = 0; i < tabs.Length; i++)
        {
            bool active = tabs[i].category == activeCategory;
            if (tabs[i].background != null)
                SetGraphicColor(tabs[i].background, active ? TabActiveColor : TabIdleColor);
            if (tabs[i].label != null)
                tabs[i].label.color = active ? TabActiveTextColor : TabIdleTextColor;
            if (tabs[i].selectedMarker != null)
                tabs[i].selectedMarker.SetActive(active);
        }
    }

    private static string GetSectionTitle(HomeStoreCategory category)
    {
        switch (category)
        {
            case HomeStoreCategory.Room:
                return GameLanguageService.Format("shop.furniture_title",HomeRoomService.CurrentRoom.DisplayName);
            case HomeStoreCategory.Home:
                return GameLanguageService.Text("shop.home");
            default:
                return GameLanguageService.Text("shop.cat");
        }
    }

    private static string GetRequiredProductTitle(string productId)
    {
        string requiredId = HomeStoreService.GetRequiredProductId(productId);
        if (!string.IsNullOrEmpty(requiredId) &&
            HomeStoreService.TryGetProduct(requiredId, out HomeStoreProduct requiredProduct))
        {
            return requiredProduct.Title;
        }
        return GameContentCopy.Text("Gerekli eşya","Required item");
    }

    private static string GetRequiredProductShortTitle(string productId)
    {
        return GetShortProductTitle(HomeStoreService.GetRequiredProductId(productId));
    }

    private static string GetShortProductTitle(string productId)
    {
        if (productId == HomeStoreService.BookshelfId)
            return GameContentCopy.Text("Kitaplık","Bookshelf");
        if (productId == HomeStoreService.TvUnitId)
            return GameContentCopy.Text("TV ünitesi","TV unit");
        if (!string.IsNullOrEmpty(productId) &&
            HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product))
        {
            return product.Title;
        }
        return GameContentCopy.Text("Gerekli eşya","Required item");
    }

    private static void SetCardAction(ProductCard card, string text, Color color)
    {
        if (card.actionText != null)
        {
            card.actionText.text = GameStatusCopy.Text(text);
            card.actionText.color = color == BuyColor ? Color.white : JoyfulUiArt.Ink;
        }
        if (card.actionBackground is LowPolyPanelGraphic && card.button != null)
            ModernUiArt.Action(card.button, color != BuyColor);
        else if (card.actionBackground != null)
            SetGraphicColor(card.actionBackground, color);
    }

    private static void SetGraphicColor(Graphic graphic, Color color)
    {
        if (graphic is LowPolyPanelGraphic premium)
            premium.SetPremiumBaseColor(color);
        else
            graphic.color = color;
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = GameStatusCopy.Text(message);
    }

    private void BeginPlacement(string productId)
    {
        if (!CatCollectionPolicy.CanDisplay(productId, out var capacityReason))
        { SetFeedback(capacityReason); return; }
        if (CatCollectionPolicy.IsCatItem(productId))
        {
            bool added=HomeStoreService.TrySetStored(productId,false);
            SetPlacementUi(false);
            SetFeedback(GameLanguageService.Current==GameLanguage.Turkish ?
                (added?"Salonunda yerine yerleştirildi.":"Şu anda yerleştirilemiyor. Biraz sonra tekrar dene."):
                (added?"Placed in your living room.":"Cannot place this item right now. Try again shortly."));
            RefreshStore();return;
        }
        if (HomeStoreService.IsFixedRoomProduct(productId))
        {
            SetPlacementUi(false);
            SetFeedback("ADDED TO ITS PLACE IN YOUR ROOM!");
            RefreshStore();
            return;
        }
        string requiredProductId = HomeStoreService.GetRequiredProductId(productId);
        if (!string.IsNullOrEmpty(requiredProductId) &&
            HomeStoreService.IsStored(requiredProductId))
        {
            SetFeedback("PLACE " + GetRequiredProductTitle(productId) + " FIRST.");
            BeginPlacement(requiredProductId);
            return;
        }

        HomeProductPlacement[] placements =
            FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include);
        activePlacement = null;
        for (int i = 0; i < placements.Length; i++)
        {
            if (placements[i] != null && placements[i].ProductId == productId)
            {
                activePlacement = placements[i];
                break;
            }
        }

        activePlacementWasStored = HomeStoreService.IsStored(productId);
        if (activePlacement == null || !activePlacement.BeginPreview())
        {
            activePlacementWasStored = false;
            SetFeedback("THIS ITEM CANNOT BE MOVED RIGHT NOW.");
            return;
        }

        if (activePlacementWasStored &&
            !HomeStoreService.TrySetStored(productId, false))
        {
            activePlacement.CancelPreview();
            activePlacement = null;
            activePlacementWasStored = false;
            SetFeedback("THIS ITEM CANNOT BE MOVED RIGHT NOW.");
            return;
        }

        placementMode = true;
        if (panel != null)
            panel.gameObject.SetActive(false);
        SetPlacementUi(true);
        UpdatePlacementUi();
        ApplyVisuals();
    }

    private void SelectPreviousPlacement()
    {
        RotatePlacement(-45f);
    }

    private void SelectNextPlacement()
    {
        RotatePlacement(45f);
    }

    private void RotatePlacement(float degrees)
    {
        if (!placementMode || activePlacement == null)
            return;
        activePlacement.RotatePreview(degrees);
        UpdatePlacementUi();
    }

    public void HandlePlacementDrag(Vector2 screenPosition)
    {
        if (!placementMode || activePlacement == null)
            return;
        Camera camera = Camera.main;
        if (camera == null)
            camera = FindAnyObjectByType<Camera>();
        activePlacement.PreviewScreenPosition(camera, screenPosition);
        UpdatePlacementUi();
    }

    public void HandlePlacementRelease()
    {
        if (!placementMode || activePlacement == null)
            return;
        activePlacement.RevertInvalidPreview();
        UpdatePlacementUi();
    }

    private void ConfirmPlacement()
    {
        FinishPlacement(true);
    }

    private void CancelPlacement()
    {
        FinishPlacement(false);
    }

    private void FinishPlacement(bool save)
    {
        if (!placementMode)
            return;

        if (save && (activePlacement == null || !activePlacement.IsPreviewValid))
        {
            UpdatePlacementUi();
            return;
        }

        string productId = activePlacement != null
            ? activePlacement.ProductId
            : string.Empty;
        bool committed = false;
        if (activePlacement != null)
        {
            if (save)
                committed = activePlacement.CommitPreview();
            else
                activePlacement.CancelPreview();
        }

        if ((!save || !committed) && activePlacementWasStored &&
            !string.IsNullOrEmpty(productId))
            HomeStoreService.TrySetStored(productId, true);

        placementMode = false;
        activePlacement = null;
        activePlacementWasStored = false;
        SetPlacementUi(false);
        BeginClosing();
    }

    private void SetPlacementUi(bool visible)
    {
        if (placementGroup == null)
            return;
        placementGroup.alpha = visible ? 1f : 0f;
        placementGroup.interactable = visible;
        placementGroup.blocksRaycasts = visible;
        if (placementDragGroup != null)
        {
            placementDragGroup.alpha = visible ? 1f : 0f;
            placementDragGroup.interactable = visible;
            placementDragGroup.blocksRaycasts = visible;
        }
    }

    private void UpdatePlacementUi()
    {
        if (activePlacement == null)
            return;
        if (HomeStoreService.TryGetProduct(activePlacement.ProductId, out HomeStoreProduct product) &&
            placementTitleText != null)
        {
            placementTitleText.text = product.Title;
        }
        if (placementCounterText != null &&
            activePlacement.PlacementKind == HomeProductPlacementKind.BookshelfOnly)
        {
            placementCounterText.text = activePlacement.IsPreviewValid
                ? "BOOKSHELF FOUND - CONFIRM TO ARRANGE BOOKS"
                : "DRAG THE BOOK SET DIRECTLY ONTO YOUR BOOKSHELF";
        }
        if (placementCounterText != null &&
            activePlacement.PlacementKind == HomeProductPlacementKind.ProductSurfaceOnly)
        {
            string targetTitle = GetRequiredProductTitle(activePlacement.ProductId);
            placementCounterText.text = activePlacement.IsPreviewValid
                ? targetTitle + " FOUND - ROTATE OR CONFIRM"
                : "DRAG THIS ITEM DIRECTLY ONTO " + targetTitle;
        }
        if (placementCounterText != null &&
            activePlacement.PlacementKind != HomeProductPlacementKind.BookshelfOnly &&
            activePlacement.PlacementKind != HomeProductPlacementKind.ProductSurfaceOnly)
        {
            placementCounterText.text = activePlacement.IsPreviewValid
                ? GameContentCopy.Text("Boş alana sürükle · Oklarla döndür", "Drag to move · Use arrows to rotate")
                : GameContentCopy.Text("Bu alan ayrılmış. Başka bir yer seç.", "This space is reserved. Choose another spot.");
        }
        if (placementPreviousButton != null)
            placementPreviousButton.interactable = activePlacement.SupportsRotation;
        if (placementNextButton != null)
            placementNextButton.interactable = activePlacement.SupportsRotation;
        if (placementConfirmButton != null)
            placementConfirmButton.interactable = activePlacement.IsPreviewValid;
    }

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

    private void ApplyVisuals()
    {
        float reveal = Smooth(Mathf.Clamp01(animTime / revealDuration));
        if (scrimGroup != null)
            scrimGroup.alpha = Mathf.Lerp(0f, placementMode ? 0.12f : scrimTargetAlpha, reveal);
        if (panelGroup != null)
            panelGroup.alpha = placementMode ? 0f : reveal;
        if (panel != null)
        {
            float scale = Mathf.Lerp(revealScaleFrom, 1f, reveal) * fitScale;
            panel.localScale = new Vector3(scale, scale, 1f);
        }

        if (cards == null)
            return;
        int visibleIndex = 0;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].group == null || !cards[i].group.gameObject.activeSelf)
                continue;
            float start = Mathf.Min(
                visibleIndex * cardStagger,
                Mathf.Max(0f, openCloseDuration - cardFadeDuration));
            cards[i].group.alpha = Smooth(
                Mathf.Clamp01((animTime - start) / cardFadeDuration));
            visibleIndex++;
        }
    }

    private void ApplyResponsiveLayout()
    {
        if (panel == null)
            return;

        float availableWidth = safeArea != null ? safeArea.rect.width : panel.rect.width;
        float availableHeight = safeArea != null ? safeArea.rect.height : panel.rect.height;
        if (availableWidth <= 1f || availableHeight <= 1f)
            return;

        // Header and transaction cards use one authored composition. Shrinking
        // only the panel width strands their fixed positions outside its rim.
        float width = maxWidth;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        float widthBudget = Mathf.Min(availableWidth * widthFraction,
            Mathf.Max(1f, availableWidth - safeAreaMargin * 2f));
        float heightBudget = Mathf.Max(1f, availableHeight - safeAreaMargin * 2f);
        fitScale = Mathf.Min(1f, widthBudget / width, heightBudget / panel.rect.height);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        widthFraction = 0.9f;
        minWidth = 1460f;
        maxWidth = 1720f;
        safeAreaMargin = 34f;
    }
#endif

    private bool IsBlockingModalActive()
    {
        if (!PetTutorialHint.IsOnboardingCompleted || OnboardingCelebrationView.IsAnyOpen)
            return true;

        if (offlinePopup == null)
            offlinePopup = FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        return offlinePopup != null && offlinePopup.IsOpen;
    }

    private void SetRaycasts(bool on)
    {
        if (rootGroup == null)
            return;
        rootGroup.blocksRaycasts = on;
        rootGroup.interactable = on;
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

        if (closeButton != null)
            closeButton.onClick.AddListener(RequestClose);
        if (scrimButton != null)
            scrimButton.onClick.AddListener(RequestClose);
        if (purchaseCoinButton != null)
            purchaseCoinButton.onClick.AddListener(PurchaseWithCoins);
        if (purchaseDiamondButton != null)
            purchaseDiamondButton.onClick.AddListener(PurchaseWithDiamonds);
        if (purchaseCancelButton != null)
            purchaseCancelButton.onClick.AddListener(ClosePurchaseDialog);
        if (purchaseLaterButton != null) purchaseLaterButton.onClick.AddListener(ClosePurchaseDialog);
        if (diamondConfirmationButton != null)
            diamondConfirmationButton.onClick.AddListener(ConfirmDiamondPurchase);
        if (diamondConfirmationCancelButton != null)
            diamondConfirmationCancelButton.onClick.AddListener(CloseDiamondConfirmation);
        if (placementPreviousButton != null)
            placementPreviousButton.onClick.AddListener(SelectPreviousPlacement);
        if (placementNextButton != null)
            placementNextButton.onClick.AddListener(SelectNextPlacement);
        if (placementConfirmButton != null)
            placementConfirmButton.onClick.AddListener(ConfirmPlacement);
        if (placementCancelButton != null)
            placementCancelButton.onClick.AddListener(CancelPlacement);
        if (tabs != null)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                ShopTab tab = tabs[i];
                if (tab.button == null)
                    continue;
                HomeStoreCategory category = tab.category;
                tab.button.onClick.AddListener(() => SetCategory(category));
            }
        }
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                ProductCard card = cards[i];
                if (card.button == null)
                    continue;
                string id = card.productId;
                card.button.onClick.AddListener(() => OnProductSelected(id));
            }
        }
        listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!listenersBound)
            return;
        if (closeButton != null)
            closeButton.onClick.RemoveListener(RequestClose);
        if (scrimButton != null)
            scrimButton.onClick.RemoveListener(RequestClose);
        if (purchaseCoinButton != null)
            purchaseCoinButton.onClick.RemoveListener(PurchaseWithCoins);
        if (purchaseDiamondButton != null)
            purchaseDiamondButton.onClick.RemoveListener(PurchaseWithDiamonds);
        if (purchaseCancelButton != null)
            purchaseCancelButton.onClick.RemoveListener(ClosePurchaseDialog);
        if (purchaseLaterButton != null) purchaseLaterButton.onClick.RemoveListener(ClosePurchaseDialog);
        if (diamondConfirmationButton != null)
            diamondConfirmationButton.onClick.RemoveListener(ConfirmDiamondPurchase);
        if (diamondConfirmationCancelButton != null)
            diamondConfirmationCancelButton.onClick.RemoveListener(CloseDiamondConfirmation);
        if (placementPreviousButton != null)
            placementPreviousButton.onClick.RemoveListener(SelectPreviousPlacement);
        if (placementNextButton != null)
            placementNextButton.onClick.RemoveListener(SelectNextPlacement);
        if (placementConfirmButton != null)
            placementConfirmButton.onClick.RemoveListener(ConfirmPlacement);
        if (placementCancelButton != null)
            placementCancelButton.onClick.RemoveListener(CancelPlacement);
        if (tabs != null)
            for (int i = 0; i < tabs.Length; i++)
                if (tabs[i].button != null)
                    tabs[i].button.onClick.RemoveAllListeners();
        if (cards != null)
            for (int i = 0; i < cards.Length; i++)
                if (cards[i].button != null)
                    cards[i].button.onClick.RemoveAllListeners();
        listenersBound = false;
    }

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

    private void OnClosedComplete()
    {
        if (activePlacement != null && activePlacement.IsPreviewing)
            activePlacement.CancelPreview();
        state = PanelState.Closed;
        IsAnyOpen = false;
        placementMode = false;
        activePlacement = null;
        ClosePurchaseDialog();
        SetPlacementUi(false);
        if (panel != null)
            panel.gameObject.SetActive(true);
        SetRaycasts(false);
        ReleaseInputBlock();
    }

    private void OnDisable()
    {
        EconomyService.AnyBalanceChanged -= RefreshStore;
        HomeStoreService.OwnershipChanged -= HandleOwnershipChanged;
        HomeStoreService.StorageChanged -= HandleOwnershipChanged;
        HomeRoomService.CurrentRoomChanged -= HandleCurrentRoomChanged;
        HomeProgressionService.Changed -= RefreshStore;
        GameLanguageService.Changed -= RefreshStore;
        ForceHideImmediate();
    }

    private void OnDestroy()
    {
        IsAnyOpen = false;
        ReleaseInputBlock();
        UnbindListeners();
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
