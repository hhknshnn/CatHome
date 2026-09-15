using System;
using UnityEngine;

/// <summary>
/// Keeps an authored store product in the room hierarchy while showing its
/// model and collision only after ownership is granted.
/// </summary>
[DisallowMultipleComponent]
public sealed class StoreProductDisplay : MonoBehaviour
{
    [SerializeField] private string productId;
    [SerializeField] private GameObject visualRoot;

    public string ProductId => productId;

    private void Awake()
    {
        Refresh();
    }

    private void OnEnable()
    {
        HomeStoreService.OwnershipChanged += HandleOwnershipChanged;
        HomeStoreService.StorageChanged += HandleStorageChanged;
        Refresh();
    }

    private void OnDisable()
    {
        HomeStoreService.OwnershipChanged -= HandleOwnershipChanged;
        HomeStoreService.StorageChanged -= HandleStorageChanged;
    }

    public void Refresh()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(
                HomeStoreService.IsOwned(productId) &&
                (productId!=HomeStoreService.KitchenFruitBasketId||HomeStoreService.IsProductDependencyMet(productId)) &&
                !HomeStoreService.IsStored(productId));
            // CatActivity can reveal the same content earlier in OwnershipChanged.
            // Always check the cat after visibility resolves, regardless of listener order.
            if (visualRoot.activeSelf && HomeStoreService.IsFixedRoomProduct(productId))
                CatActivityMotion.KeepCatClearAfterPurchase(gameObject.scene);
        }
    }

    private void HandleStorageChanged(string changedProductId)
    {
        if (string.IsNullOrEmpty(changedProductId) ||
            string.Equals(changedProductId, productId, StringComparison.Ordinal))
        {
            Refresh();
        }
    }

    private void HandleOwnershipChanged(string changedProductId)
    {
        if (string.IsNullOrEmpty(changedProductId) ||
            string.Equals(changedProductId, productId, StringComparison.Ordinal) ||
            (productId==HomeStoreService.KitchenFruitBasketId&&changedProductId==HomeStoreService.KitchenIslandId))
        {
            Refresh();
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(string id, GameObject content)
    {
        productId = id;
        visualRoot = content;
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }
#endif
}
