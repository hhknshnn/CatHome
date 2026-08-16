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
        Refresh();
    }

    private void OnDisable()
    {
        HomeStoreService.OwnershipChanged -= HandleOwnershipChanged;
    }

    public void Refresh()
    {
        if (visualRoot != null)
            visualRoot.SetActive(HomeStoreService.IsOwned(productId));
    }

    private void HandleOwnershipChanged(string changedProductId)
    {
        if (string.IsNullOrEmpty(changedProductId) ||
            string.Equals(changedProductId, productId, StringComparison.Ordinal))
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
