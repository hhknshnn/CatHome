using System;
using UnityEngine;

/// <summary>
/// Keeps a purchased product attached to another purchased room product.
/// The saved rotation is a local yaw offset, so the attachment follows its
/// supporting furniture while still allowing the player to rotate it.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeRequiredProductAttachment : MonoBehaviour
{
    [SerializeField] private string productId;
    [SerializeField] private string requiredProductId;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private Vector3 targetLocalPosition;
    [SerializeField] private Vector3 targetLocalEulerAngles;

    private Transform authoredParent;
    private Vector3 authoredLocalPosition;
    private Quaternion authoredLocalRotation;
    private Vector3 authoredLocalScale;
    private bool cachedAuthoringTransform;
    private bool previewing;
    private bool snapped;
    private float previewLocalYaw;
    private Transform targetRoot;

    public string RequiredProductId => requiredProductId;
    public float CurrentLocalYaw => previewLocalYaw;
    public bool IsInstalled =>
        HomeStoreService.IsOwned(productId) &&
        HomeStoreService.TryGetWorldPlacement(productId, out _, out _);

    private void Awake()
    {
        CacheAuthoringTransform();
        RefreshState();
    }

    private void OnEnable()
    {
        CacheAuthoringTransform();
        HomeStoreService.OwnershipChanged += HandleStoreChanged;
        HomeStoreService.PlacementChanged += HandleStoreChanged;
        RefreshState();
    }

    private void OnDisable()
    {
        HomeStoreService.OwnershipChanged -= HandleStoreChanged;
        HomeStoreService.PlacementChanged -= HandleStoreChanged;
        previewing = false;
        snapped = false;
    }

    public void BeginPlacementPreview(Transform target)
    {
        previewing = true;
        targetRoot = target;
        previewLocalYaw = transform.parent == target && target != null
            ? Mathf.Repeat(transform.localEulerAngles.y - targetLocalEulerAngles.y, 360f)
            : 0f;
        SetPlacementPreview(target, false);
    }

    public void SetPlacementPreview(Transform target, bool isSnapped)
    {
        targetRoot = target;
        snapped = isSnapped && targetRoot != null;
        if (!snapped)
        {
            RestoreAuthoringTransform();
            SetVisualActive(false);
            return;
        }

        AttachToTarget(targetRoot, previewLocalYaw);
        SetVisualActive(true);
    }

    public void RotatePreview(float degrees)
    {
        if (!previewing)
            return;
        previewLocalYaw = Mathf.Repeat(previewLocalYaw + degrees, 360f);
        if (snapped && targetRoot != null)
            AttachToTarget(targetRoot, previewLocalYaw);
    }

    public void CommitPlacement(Transform target)
    {
        previewing = false;
        targetRoot = target;
        snapped = targetRoot != null;
        if (!snapped)
        {
            RefreshState();
            return;
        }

        AttachToTarget(targetRoot, previewLocalYaw);
        SetVisualActive(true);
    }

    public void CancelPlacementPreview()
    {
        previewing = false;
        snapped = false;
        RefreshState();
    }

    public void SnapToTargetImmediate(Transform target, float localYaw)
    {
        previewing = false;
        targetRoot = target;
        previewLocalYaw = Mathf.Repeat(localYaw, 360f);
        snapped = targetRoot != null;
        if (!snapped)
        {
            HideUninstalled();
            return;
        }

        AttachToTarget(targetRoot, previewLocalYaw);
        SetVisualActive(true);
    }

    private void RefreshState()
    {
        if (previewing)
            return;

        if (!HomeStoreService.IsOwned(productId) ||
            !HomeStoreService.TryGetWorldPlacement(productId, out _, out float localYaw) ||
            !TryFindRequiredProduct(out Transform target))
        {
            HideUninstalled();
            return;
        }

        SnapToTargetImmediate(target, localYaw);
    }

    private bool TryFindRequiredProduct(out Transform target)
    {
        HomeProductPlacement[] placements =
            FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include);
        for (int i = 0; i < placements.Length; i++)
        {
            HomeProductPlacement placement = placements[i];
            if (placement != null &&
                string.Equals(placement.ProductId, requiredProductId, StringComparison.Ordinal))
            {
                target = placement.MovableRoot;
                return target != null;
            }
        }

        target = null;
        return false;
    }

    private void AttachToTarget(Transform target, float localYaw)
    {
        transform.SetParent(target, false);
        transform.localPosition = targetLocalPosition;
        transform.localRotation = Quaternion.Euler(
            targetLocalEulerAngles.x,
            targetLocalEulerAngles.y + localYaw,
            targetLocalEulerAngles.z);
        transform.localScale = Vector3.one;
    }

    private void HideUninstalled()
    {
        snapped = false;
        targetRoot = null;
        RestoreAuthoringTransform();
        SetVisualActive(false);
    }

    private void SetVisualActive(bool active)
    {
        if (visualRoot != null)
            visualRoot.SetActive(active);
    }

    private void CacheAuthoringTransform()
    {
        if (cachedAuthoringTransform)
            return;
        authoredParent = transform.parent;
        authoredLocalPosition = transform.localPosition;
        authoredLocalRotation = transform.localRotation;
        authoredLocalScale = transform.localScale;
        cachedAuthoringTransform = true;
    }

    private void RestoreAuthoringTransform()
    {
        if (!cachedAuthoringTransform)
            return;
        transform.SetParent(authoredParent, false);
        transform.localPosition = authoredLocalPosition;
        transform.localRotation = authoredLocalRotation;
        transform.localScale = authoredLocalScale;
    }

    private void HandleStoreChanged(string changedProductId)
    {
        if (string.IsNullOrEmpty(changedProductId) ||
            string.Equals(changedProductId, productId, StringComparison.Ordinal) ||
            string.Equals(changedProductId, requiredProductId, StringComparison.Ordinal))
        {
            RefreshState();
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        string id,
        string requiredId,
        GameObject content,
        Vector3 localPosition,
        Vector3 localEulerAngles)
    {
        productId = id;
        requiredProductId = requiredId;
        visualRoot = content;
        targetLocalPosition = localPosition;
        targetLocalEulerAngles = localEulerAngles;
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }
#endif
}
