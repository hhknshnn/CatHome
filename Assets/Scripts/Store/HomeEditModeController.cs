using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact Home 2.0 editor. Safe drag releases save automatically, leaving only
/// item navigation, one rotate action, storage and Done on screen.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeEditModeController : MonoBehaviour
{
    private readonly struct ProductSnapshot
    {
        public ProductSnapshot(HomeProductPlacement placement)
        {
            ProductId = placement != null ? placement.ProductId : string.Empty;
            Stored = HomeStoreService.IsStored(ProductId);
            HasWorldPlacement = HomeStoreService.TryGetWorldPlacement(
                ProductId, out Vector3 position, out float yaw);
            Position = position;
            RotationY = yaw;
        }

        public string ProductId { get; }
        public bool Stored { get; }
        public bool HasWorldPlacement { get; }
        public Vector3 Position { get; }
        public float RotationY { get; }
    }

    [Header("Presentation")]
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField] private TMP_Text roomTitleText;
    [SerializeField] private TMP_Text productTitleText;
    [SerializeField] private TMP_Text placementRuleText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private LowPolyPanelGraphic placementRuleBadge;

    [Header("Actions")]
    [SerializeField] private Button previousItemButton;
    [SerializeField] private Button nextItemButton;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button storeButton;
    [SerializeField] private TMP_Text storeButtonText;
    [SerializeField] private Button doneButton;

    private readonly List<HomeProductPlacement> placements = new List<HomeProductPlacement>();
    private HomeProductPlacement activePlacement;
    private ProductSnapshot activeSnapshot;
    private CatMovement catMovement;
    private HomePlacementZoneGuide zoneGuide;
    private bool inputBlockHeld;
    private bool isOpen;
    private int selectedIndex = -1;

    public static bool IsAnyOpen { get; private set; }
    public bool IsOpen => isOpen;
    public int EditableItemCount => placements.Count;
    public string SelectedProductId =>
        activePlacement != null ? activePlacement.ProductId : string.Empty;

    private void Awake()
    {
        zoneGuide = GetComponent<HomePlacementZoneGuide>();
        if (zoneGuide == null)
            zoneGuide = gameObject.AddComponent<HomePlacementZoneGuide>();
        BindListeners();
        SetVisible(false);
    }

    private void OnDisable()
    {
        if (isOpen)
            Close();
        UnbindListeners();
    }

    public void Open()
    {
        // Retired UI may still be referenced by an older scene.
        // Fixed room composition has no drag, turn or storage mode.
        return;
    }

    public void Close()
    {
        if (!isOpen)
            return;
        CancelActivePreview(restoreSnapshot: true);
        zoneGuide?.Hide();
        isOpen = false;
        IsAnyOpen = false;
        SetVisible(false);
        ReleaseInputBlock();
        CatHomeSaveSystem.SaveNow(true);
    }

    public void HandlePointerDown(Vector2 screenPosition)
    {
        if (!isOpen)
            return;
        if (TryPickPlacement(screenPosition, out HomeProductPlacement picked) &&
            picked != activePlacement)
            SelectPlacement(picked);
        activePlacement?.PreviewScreenPosition(ResolveCamera(), screenPosition);
        UpdateUi();
    }

    public void HandlePointerDrag(Vector2 screenPosition)
    {
        if (!isOpen || activePlacement == null)
            return;
        activePlacement.PreviewScreenPosition(ResolveCamera(), screenPosition);
        UpdateUi();
    }

    public void HandlePointerUp(Vector2 screenPosition)
    {
        if (!isOpen || activePlacement == null)
            return;
        activePlacement.PreviewScreenPosition(ResolveCamera(), screenPosition);
        if (!activePlacement.IsPreviewValid)
        {
            activePlacement.RevertInvalidPreview();
            UpdateUi(GameLanguageService.Text("edit.returned_safe"));
            return;
        }
        SaveActivePlacement(GameLanguageService.Text("edit.placed_adjust"));
    }

    private void SelectPreviousItem() => CycleSelection(-1);
    private void SelectNextItem() => CycleSelection(1);

    private void CycleSelection(int direction)
    {
        if (!isOpen || placements.Count == 0)
            return;
        int start = selectedIndex < 0 ? (direction > 0 ? -1 : 0) : selectedIndex;
        int next = (start + direction + placements.Count) % placements.Count;
        SelectPlacement(placements[next]);
    }

    private void SelectPlacement(HomeProductPlacement placement)
    {
        if (placement == null || !HomeStoreService.IsOwned(placement.ProductId))
            return;
        if (!CatCollectionPolicy.CanDisplay(placement.ProductId, out var reason))
        { UpdateUi(reason); return; }
        CancelActivePreview(restoreSnapshot: true);
        selectedIndex = placements.IndexOf(placement);
        activeSnapshot = new ProductSnapshot(placement);
        if (activeSnapshot.Stored && !HomeStoreService.TrySetStored(placement.ProductId, false))
        { activePlacement=null; UpdateUi(CatCollectionPolicy.Summary); return; }
        placement.ApplySavedPlacement();

        if (!placement.BeginPreview())
        {
            RestoreSnapshot(activeSnapshot);
            activePlacement = null;
            zoneGuide?.Hide();
            UpdateUi(GetUnavailableHint(placement));
            return;
        }

        activePlacement = placement;
        zoneGuide?.Show(placement);
        UpdateUi(activeSnapshot.Stored
            ? GameLanguageService.Text("edit.from_storage")
            : GetPlacementHint(placement));
    }

    private void RotateSelected()
    {
        if (!isOpen || activePlacement == null)
            return;
        activePlacement.RotatePreview(45f);
        if (!activePlacement.IsPreviewValid)
        {
            activePlacement.RevertInvalidPreview();
            UpdateUi(GameLanguageService.Text("edit.no_turn_room"));
            return;
        }
        SaveActivePlacement(GameLanguageService.Text("edit.turned_saved"));
    }

    private void SaveActivePlacement(string confirmation)
    {
        if (activePlacement == null || !activePlacement.IsPreviewValid)
            return;
        HomeProductPlacement selected = activePlacement;
        if (!selected.CommitPreview())
        {
            UpdateUi(GameLanguageService.Text("edit.save_failed"));
            return;
        }
        selected.ApplySavedPlacement();
        activeSnapshot = new ProductSnapshot(selected);
        if (!selected.BeginPreview())
        {
            activePlacement = null;
            zoneGuide?.Hide();
            UpdateUi(GameLanguageService.Text("edit.saved"));
            return;
        }
        activePlacement = selected;
        zoneGuide?.Show(selected);
        UpdateUi(confirmation);
    }

    private void StoreSelected()
    {
        if (activePlacement == null)
        {
            UpdateUi(GameLanguageService.Text("edit.choose_to_store"));
            return;
        }
        HomeProductPlacement selected = activePlacement;
        ProductSnapshot snapshot = activeSnapshot;
        selected.CancelPreview();
        activePlacement = null;
        int attachedStored = StoreAttachedProducts(selected.ProductId);
        if (!HomeStoreService.TrySetStored(selected.ProductId, true))
        {
            RestoreSnapshot(snapshot);
            UpdateUi(GameLanguageService.Text("edit.cannot_store"));
            return;
        }
        activeSnapshot = default;
        zoneGuide?.Hide();
        UpdateUi(attachedStored > 0
            ? GameLanguageService.Text("edit.stored_attached")
            : GameLanguageService.Text("edit.stored_restore"));
    }

    private int StoreAttachedProducts(string supportProductId)
    {
        int stored = 0;
        for (int i = 0; i < placements.Count; i++)
        {
            HomeProductPlacement placement = placements[i];
            if (placement == null ||
                !string.Equals(placement.RequiredProductId, supportProductId, StringComparison.Ordinal) ||
                HomeStoreService.IsStored(placement.ProductId))
                continue;
            if (HomeStoreService.TrySetStored(placement.ProductId, true))
                stored++;
        }
        return stored;
    }

    private void CancelActivePreview(bool restoreSnapshot)
    {
        if (activePlacement == null)
            return;
        activePlacement.CancelPreview();
        if (restoreSnapshot)
            RestoreSnapshot(activeSnapshot);
        activePlacement = null;
        activeSnapshot = default;
        zoneGuide?.Hide();
    }

    private static void RestoreSnapshot(ProductSnapshot snapshot)
    {
        if (string.IsNullOrEmpty(snapshot.ProductId))
            return;
        if (snapshot.HasWorldPlacement)
            HomeStoreService.TrySetPlacement(snapshot.ProductId, snapshot.Position, snapshot.RotationY);
        else
            HomeStoreService.TryClearPlacement(snapshot.ProductId);
        HomeStoreService.TrySetStored(snapshot.ProductId, snapshot.Stored);
        HomeProductPlacement[] all =
            FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].ProductId == snapshot.ProductId)
                all[i].ApplySavedPlacement();
    }

    private void RefreshPlacements()
    {
        placements.Clear();
        HomeProductPlacement[] all =
            FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
        {
            HomeProductPlacement placement = all[i];
            if (placement == null || !HomeStoreService.IsOwned(placement.ProductId))
                continue;
            if (!HomeStoreService.TryGetProduct(placement.ProductId, out HomeStoreProduct product) ||
                !product.IsPlaceable)
                continue;
            placements.Add(placement);
        }
        placements.Sort((a, b) => string.CompareOrdinal(
            GetTitle(a != null ? a.ProductId : string.Empty),
            GetTitle(b != null ? b.ProductId : string.Empty)));
        selectedIndex = activePlacement != null ? placements.IndexOf(activePlacement) : -1;
    }

    private bool TryPickPlacement(Vector2 screenPosition, out HomeProductPlacement placement)
    {
        placement = null;
        Camera camera = ResolveCamera();
        if (camera == null)
            return false;
        RaycastHit[] hits = Physics.RaycastAll(
            camera.ScreenPointToRay(screenPosition), float.PositiveInfinity, ~0,
            QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            HomeProductPlacement candidate = hits[i].collider != null
                ? hits[i].collider.GetComponentInParent<HomeProductPlacement>()
                : null;
            if (candidate != null && placements.Contains(candidate) &&
                !HomeStoreService.IsStored(candidate.ProductId))
            {
                placement = candidate;
                return true;
            }
        }
        return false;
    }

    private Camera ResolveCamera()
    {
        Camera camera = Camera.main;
        return camera != null ? camera : FindAnyObjectByType<Camera>();
    }

    private void UpdateUi(string overrideHint = null)
    {
        if (roomTitleText != null)
            roomTitleText.text = GameLanguageService.Format("edit.room", HomeRoomService.CurrentRoom.DisplayName);
        if (productTitleText != null)
            productTitleText.text = activePlacement != null
                ? GetTitle(activePlacement.ProductId) : GameLanguageService.Text("edit.select_item");
        if (placementRuleText != null)
            placementRuleText.text = activePlacement != null
                ? GetPlacementRule(activePlacement) : GameLanguageService.Text("edit.tap_select");
        if (placementRuleBadge != null)
            placementRuleBadge.SetPremiumBaseColor(GetPlacementColor(activePlacement));

        if (hintText != null && overrideHint != null)
            hintText.text = overrideHint;
        else if (hintText != null && activePlacement == null)
            hintText.text = placements.Count == 0
                ? GameLanguageService.Text("edit.buy_hint") : GameLanguageService.Text("edit.move_hint");
        else if (hintText != null)
            hintText.text = activePlacement.IsPreviewValid
                ? GetPlacementHint(activePlacement) : GetBlockedHint(activePlacement);

        bool selected = activePlacement != null;
        if (rotateButton != null)
            rotateButton.interactable = selected && activePlacement.SupportsRotation;
        if (storeButton != null)
            storeButton.interactable = selected;
        if (storeButtonText != null)
            storeButtonText.text = GameLanguageService.Text("edit.store");
        if (previousItemButton != null)
            previousItemButton.interactable = placements.Count > 0;
        if (nextItemButton != null)
            nextItemButton.interactable = placements.Count > 0;
    }

    private static string GetPlacementRule(HomeProductPlacement placement)
    {
        if (placement == null)
            return GameLanguageService.Text("edit.tap_select");
        switch (placement.PlacementKind)
        {
            case HomeProductPlacementKind.WallEdge: return GameLanguageService.Text("edit.wall");
            case HomeProductPlacementKind.BookshelfOnly: return GameLanguageService.Text("edit.bookshelf");
            case HomeProductPlacementKind.ProductSurfaceOnly:
                return GameLanguageService.Format("edit.surface", GetTitle(placement.RequiredProductId));
            default: return GameLanguageService.Text("edit.floor");
        }
    }

    private static Color GetPlacementColor(HomeProductPlacement placement)
    {
        if (placement == null)
            return new Color32(235, 224, 246, 255);
        switch (placement.PlacementKind)
        {
            case HomeProductPlacementKind.WallEdge: return PremiumUiStyle.CandyGrape;
            case HomeProductPlacementKind.BookshelfOnly:
            case HomeProductPlacementKind.ProductSurfaceOnly: return PremiumUiStyle.Champagne;
            default: return PremiumUiStyle.CandyMint;
        }
    }

    private static string GetPlacementHint(HomeProductPlacement placement)
    {
        switch (placement.PlacementKind)
        {
            case HomeProductPlacementKind.WallEdge:
                return GameLanguageService.Text("edit.drag_wall");
            case HomeProductPlacementKind.BookshelfOnly:
            case HomeProductPlacementKind.ProductSurfaceOnly:
                return GameLanguageService.Text("edit.drag_surface");
            default:
                return GameLanguageService.Text("edit.drag_floor");
        }
    }

    private static string GetBlockedHint(HomeProductPlacement placement)
    {
        return placement.PlacementKind == HomeProductPlacementKind.BookshelfOnly ||
               placement.PlacementKind == HomeProductPlacementKind.ProductSurfaceOnly
            ? GameLanguageService.Text("edit.only_surface")
            : GameLanguageService.Text("edit.blocked");
    }

    private static string GetUnavailableHint(HomeProductPlacement placement)
    {
        if (!string.IsNullOrEmpty(placement.RequiredProductId))
            return GameLanguageService.Format("edit.restore_required", GetTitle(placement.RequiredProductId));
        return GameLanguageService.Text("edit.unavailable");
    }

    private static string GetTitle(string productId)
    {
        return HomeStoreService.TryGetProduct(productId, out HomeStoreProduct product)
            ? product.Title : GameLanguageService.Text("product.unknown");
    }

    private void SetVisible(bool visible)
    {
        if (panelGroup == null)
            return;
        panelGroup.alpha = visible ? 1f : 0f;
        panelGroup.interactable = visible;
        panelGroup.blocksRaycasts = visible;
    }

    private void AcquireInputBlock()
    {
        if (inputBlockHeld)
            return;
        if (catMovement == null)
            catMovement = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
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
        previousItemButton?.onClick.AddListener(SelectPreviousItem);
        nextItemButton?.onClick.AddListener(SelectNextItem);
        rotateButton?.onClick.AddListener(RotateSelected);
        storeButton?.onClick.AddListener(StoreSelected);
        doneButton?.onClick.AddListener(Close);
    }

    private void UnbindListeners()
    {
        previousItemButton?.onClick.RemoveAllListeners();
        nextItemButton?.onClick.RemoveAllListeners();
        rotateButton?.onClick.RemoveAllListeners();
        storeButton?.onClick.RemoveAllListeners();
        doneButton?.onClick.RemoveAllListeners();
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CanvasGroup group, TMP_Text roomTitle, TMP_Text productTitle,
        TMP_Text placementRule, TMP_Text hint, LowPolyPanelGraphic ruleBadge,
        Button previous, Button next, Button rotate, Button store,
        TMP_Text storeLabel, Button done)
    {
        panelGroup = group;
        roomTitleText = roomTitle;
        productTitleText = productTitle;
        placementRuleText = placementRule;
        hintText = hint;
        placementRuleBadge = ruleBadge;
        previousItemButton = previous;
        nextItemButton = next;
        rotateButton = rotate;
        storeButton = store;
        storeButtonText = storeLabel;
        doneButton = done;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState() => IsAnyOpen = false;
}
