using System;
using UnityEngine;

public enum HomeProductPlacementKind
{
    Floor = 0,
    WallEdge = 1,
    BookshelfOnly = 2,
    ProductSurfaceOnly = 3
}

/// <summary>
/// Free-drag placement for one purchased room product. The item is constrained
/// to the usable floor rectangle and validated against real room colliders, so
/// it cannot be confirmed through walls, furniture or another placed product.
/// </summary>
[DisallowMultipleComponent]
public sealed class HomeProductPlacement : MonoBehaviour
{
    private const float WallSnapInset = 0.04f;
    // LivingRoom_Level01 wall colliders expose these inner faces:
    // X = -3.80 / +3.80, Z = -2.90 / +2.80. Keeping the placement
    // rectangle two centimetres inside them, together with the configured
    // collision clearance, leaves roughly 10-11 cm of visible wall gap for
    // an unrotated product instead of the previous 35-45 cm dead zone.
    // Decorative baseboards are ignored by overlap validation because the
    // actual footprint still remains about seven centimetres clear of them.
    public const float DefaultRoomLeft = -3.78f;
    public const float DefaultRoomRight = 3.78f;
    public const float DefaultRoomFront = -2.88f;
    public const float DefaultRoomBack = 2.78f;

    [SerializeField] private string productId;
    [SerializeField] private Transform movableRoot;
    [SerializeField] private Transform[] legacySlots = Array.Empty<Transform>();
    [SerializeField] private Vector2 footprintSize = new Vector2(0.7f, 0.6f);
    [SerializeField] private HomeProductPlacementKind placementKind;
    [SerializeField] private string requiredProductId;
    [SerializeField] private HomeBookshelfBookSet bookshelfBookSet;
    [SerializeField] private HomeRequiredProductAttachment requiredProductAttachment;
    [SerializeField] private Vector2 roomMinimum =
        new Vector2(DefaultRoomLeft, DefaultRoomFront);
    [SerializeField] private Vector2 roomMaximum =
        new Vector2(DefaultRoomRight, DefaultRoomBack);
    [SerializeField, Min(0f)] private float collisionClearance = 0.09f;
    [SerializeField, Min(0.05f)] private float validationHeight = 0.7f;
    [SerializeField, Min(0f)] private float hungHeight;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;
    private bool hasLastValidPreview;
    private bool previewing;
    private bool previewValid;
    private GameObject indicator;
    private Renderer indicatorRenderer;
    private Material indicatorMaterial;
    private Transform constrainedTarget;

    private static readonly Color ValidColor = new Color(0.15f, 0.85f, 0.58f, 0.55f);
    private static readonly Color InvalidColor = new Color(1f, 0.22f, 0.18f, 0.62f);

    public string ProductId => productId;
    public bool IsPreviewing => previewing;
    public bool IsPreviewValid => previewing && previewValid;
    public HomeProductPlacementKind PlacementKind => placementKind;
    public string RequiredProductId => requiredProductId;
    public Transform MovableRoot => movableRoot != null ? movableRoot : transform;
    public Vector3 PreviewPosition => movableRoot != null ? movableRoot.position : transform.position;
    public float PreviewRotationY => movableRoot != null ? movableRoot.eulerAngles.y : transform.eulerAngles.y;
    public bool SupportsRotation => true;

    private bool UsesRequiredProductTarget =>
        placementKind == HomeProductPlacementKind.BookshelfOnly ||
        placementKind == HomeProductPlacementKind.ProductSurfaceOnly;

    private void Awake()
    {
        ResolveRoot();
        ApplySavedPlacement();
    }

    private void OnEnable()
    {
        HomeStoreService.PlacementChanged += HandlePlacementChanged;
        ApplySavedPlacement();
    }

    private void OnDisable()
    {
        HomeStoreService.PlacementChanged -= HandlePlacementChanged;
        HideIndicator();
        previewing = false;
    }

    public bool BeginPreview()
    {
        ResolveRoot();
        if (movableRoot == null || !HomeStoreService.IsOwned(productId))
            return false;

        originalPosition = movableRoot.position;
        originalRotation = movableRoot.rotation;
        lastValidPosition = originalPosition;
        lastValidRotation = originalRotation;
        hasLastValidPreview = false;
        previewing = true;

        if (UsesRequiredProductTarget)
        {
            constrainedTarget = FindRequiredPlacementTarget();
            if (constrainedTarget == null)
            {
                previewing = false;
                return false;
            }

            previewValid = false;
            hasLastValidPreview = false;
            if (placementKind == HomeProductPlacementKind.BookshelfOnly)
            {
                ResolveBookSet();
                bookshelfBookSet?.BeginPlacementPreview(constrainedTarget);
            }
            else
            {
                ResolveRequiredAttachment();
                requiredProductAttachment?.BeginPlacementPreview(constrainedTarget);
            }
            HideIndicator();
            return true;
        }

        EnsureIndicator();
        SetIndicatorVisible(true);
        RefreshValidity();
        RememberValidPreview();
        return true;
    }

    public bool PreviewScreenPosition(Camera camera, Vector2 screenPosition)
    {
        if (!previewing || movableRoot == null || camera == null)
            return false;

        if (UsesRequiredProductTarget)
        {
            constrainedTarget = FindRequiredPlacementTarget();
            bool snapped = false;
            if (constrainedTarget != null)
            {
                Ray bookshelfRay = camera.ScreenPointToRay(screenPosition);
                RaycastHit[] hits = Physics.RaycastAll(
                    bookshelfRay,
                    float.PositiveInfinity,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits.Length; i++)
                {
                    Transform hit = hits[i].collider != null ? hits[i].collider.transform : null;
                    if (hit != null &&
                        (hit == constrainedTarget || hit.IsChildOf(constrainedTarget)))
                    {
                        snapped = true;
                        break;
                    }
                }
            }

            previewValid = snapped;
            SetRequiredTargetPreview(snapped);
            return snapped;
        }

        Ray ray = camera.ScreenPointToRay(screenPosition);
        var floor = new Plane(Vector3.up, new Vector3(0f, originalPosition.y, 0f));
        if (!floor.Raycast(ray, out float distance))
            return false;

        Vector3 candidate = ray.GetPoint(distance);
        candidate.y = originalPosition.y;
        return PreviewWorldPosition(candidate);
    }

    public bool PreviewWorldPosition(Vector3 candidate)
    {
        if (!previewing || movableRoot == null)
            return false;

        if (UsesRequiredProductTarget)
        {
            constrainedTarget = FindRequiredPlacementTarget();
            previewValid = constrainedTarget != null &&
                Vector2.Distance(
                    new Vector2(candidate.x, candidate.z),
                    new Vector2(constrainedTarget.position.x, constrainedTarget.position.z)) <= 0.9f;
            SetRequiredTargetPreview(previewValid);
            return previewValid;
        }

        candidate.y = hungHeight > 0.01f ? hungHeight : originalPosition.y;
        if (placementKind == HomeProductPlacementKind.WallEdge)
            candidate = SnapToNearestWall(candidate);
        if (hungHeight > 0.01f)
            candidate.y = hungHeight;
        movableRoot.position = ClampToRoom(candidate);
        RefreshValidity();
        RememberValidPreview();
        return previewValid;
    }

    public void RotatePreview(float degrees)
    {
        if (!previewing || movableRoot == null)
            return;
        if (placementKind == HomeProductPlacementKind.BookshelfOnly)
        {
            ResolveBookSet();
            bookshelfBookSet?.RotatePreview(Mathf.Sign(degrees) * 15f);
            return;
        }
        if (placementKind == HomeProductPlacementKind.ProductSurfaceOnly)
        {
            ResolveRequiredAttachment();
            requiredProductAttachment?.RotatePreview(degrees);
            return;
        }
        movableRoot.Rotate(0f, degrees, 0f, Space.World);
        RefreshValidity();
        RememberValidPreview();
    }

    public bool RevertInvalidPreview()
    {
        if (!previewing || movableRoot == null || previewValid)
            return false;

        if (UsesRequiredProductTarget)
        {
            SetRequiredTargetPreview(false);
            return true;
        }

        movableRoot.SetPositionAndRotation(
            hasLastValidPreview ? lastValidPosition : originalPosition,
            hasLastValidPreview ? lastValidRotation : originalRotation);
        RefreshValidity();
        return true;
    }

    public bool CommitPreview()
    {
        if (!IsPreviewValid || movableRoot == null)
            return false;

        if (UsesRequiredProductTarget)
        {
            if (constrainedTarget == null)
                return false;
            float savedYaw = 0f;
            if (placementKind == HomeProductPlacementKind.BookshelfOnly)
            {
                ResolveBookSet();
                savedYaw = bookshelfBookSet != null
                    ? bookshelfBookSet.CurrentLocalYaw
                    : 0f;
            }
            else if (placementKind == HomeProductPlacementKind.ProductSurfaceOnly)
            {
                ResolveRequiredAttachment();
                savedYaw = requiredProductAttachment != null
                    ? requiredProductAttachment.CurrentLocalYaw
                    : 0f;
            }
            bool installed = HomeStoreService.TrySetPlacement(
                productId,
                constrainedTarget.position,
                savedYaw);
            previewing = false;
            if (placementKind == HomeProductPlacementKind.BookshelfOnly)
            {
                ResolveBookSet();
                if (installed)
                    bookshelfBookSet?.CommitPlacement(constrainedTarget);
                else
                    bookshelfBookSet?.CancelPlacementPreview();
            }
            else
            {
                ResolveRequiredAttachment();
                if (installed)
                    requiredProductAttachment?.CommitPlacement(constrainedTarget);
                else
                    requiredProductAttachment?.CancelPlacementPreview();
            }
            return installed;
        }

        bool saved = HomeStoreService.TrySetPlacement(
            productId,
            movableRoot.position,
            movableRoot.eulerAngles.y);
        previewing = false;
        HideIndicator();
        return saved;
    }

    public void CancelPreview()
    {
        if (UsesRequiredProductTarget)
        {
            previewing = false;
            previewValid = false;
            if (placementKind == HomeProductPlacementKind.BookshelfOnly)
            {
                ResolveBookSet();
                bookshelfBookSet?.CancelPlacementPreview();
            }
            else
            {
                ResolveRequiredAttachment();
                requiredProductAttachment?.CancelPlacementPreview();
            }
            return;
        }

        if (movableRoot != null && previewing)
            movableRoot.SetPositionAndRotation(originalPosition, originalRotation);
        previewing = false;
        HideIndicator();
    }

    public void ApplySavedPlacement()
    {
        ResolveRoot();
        if (movableRoot == null || previewing)
            return;

        if (UsesRequiredProductTarget)
        {
            constrainedTarget = FindRequiredPlacementTarget();
            if (placementKind == HomeProductPlacementKind.BookshelfOnly)
            {
                ResolveBookSet();
                if (bookshelfBookSet != null && constrainedTarget != null &&
                    HomeStoreService.IsOwned(productId) &&
                    HomeStoreService.TryGetWorldPlacement(productId, out _, out float bookYaw))
                {
                    bookshelfBookSet.SnapToBookshelfImmediate(constrainedTarget, bookYaw);
                }
            }
            else
            {
                ResolveRequiredAttachment();
                if (requiredProductAttachment != null && constrainedTarget != null &&
                    HomeStoreService.IsOwned(productId) &&
                    HomeStoreService.TryGetWorldPlacement(productId, out _, out float localYaw))
                {
                    requiredProductAttachment.SnapToTargetImmediate(
                        constrainedTarget,
                        localYaw);
                }
            }
            return;
        }

        if (HomeStoreService.TryGetWorldPlacement(productId, out Vector3 position, out float yaw))
        {
            position.y = hungHeight > 0.01f ? hungHeight : movableRoot.position.y;
            movableRoot.SetPositionAndRotation(
                ClampToRoom(position),
                Quaternion.Euler(0f, yaw, 0f));
            return;
        }

        int legacyIndex = HomeStoreService.GetPlacementIndex(productId);
        if (legacySlots != null && legacySlots.Length > 0)
        {
            legacyIndex = Mathf.Clamp(legacyIndex, 0, legacySlots.Length - 1);
            Transform slot = legacySlots[legacyIndex];
            if (slot != null)
                movableRoot.SetPositionAndRotation(slot.position, slot.rotation);
        }
    }

    private Vector3 ClampToRoom(Vector3 candidate)
    {
        GetWorldHalfExtents(out float halfX, out float halfZ);
        candidate.x = Mathf.Clamp(candidate.x, roomMinimum.x + halfX, roomMaximum.x - halfX);
        candidate.z = Mathf.Clamp(candidate.z, roomMinimum.y + halfZ, roomMaximum.y - halfZ);
        return candidate;
    }

    private Vector3 SnapToNearestWall(Vector3 candidate)
    {
        float left = Mathf.Abs(candidate.x - roomMinimum.x);
        float right = Mathf.Abs(roomMaximum.x - candidate.x);
        float front = Mathf.Abs(candidate.z - roomMinimum.y);
        float back = Mathf.Abs(roomMaximum.y - candidate.z);
        float nearest = Mathf.Min(left, right, front, back);

        float yaw;
        if (Mathf.Approximately(nearest, left))
            yaw = 90f;
        else if (Mathf.Approximately(nearest, right))
            yaw = 270f;
        else if (Mathf.Approximately(nearest, back))
            yaw = 180f;
        else
            yaw = 0f;

        movableRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        GetWorldHalfExtents(out float halfX, out float halfZ);
        if (Mathf.Approximately(nearest, left))
            candidate.x = roomMinimum.x + halfX + WallSnapInset;
        else if (Mathf.Approximately(nearest, right))
            candidate.x = roomMaximum.x - halfX - WallSnapInset;
        else if (Mathf.Approximately(nearest, back))
            candidate.z = roomMaximum.y - halfZ - WallSnapInset;
        else
            candidate.z = roomMinimum.y + halfZ + WallSnapInset;
        return candidate;
    }

    private void GetWorldHalfExtents(out float halfX, out float halfZ)
    {
        float yaw = movableRoot != null ? movableRoot.eulerAngles.y * Mathf.Deg2Rad : 0f;
        float cosine = Mathf.Abs(Mathf.Cos(yaw));
        float sine = Mathf.Abs(Mathf.Sin(yaw));
        halfX = (footprintSize.x * cosine + footprintSize.y * sine) * 0.5f +
                collisionClearance;
        halfZ = (footprintSize.x * sine + footprintSize.y * cosine) * 0.5f +
                collisionClearance;
    }

    private void RefreshValidity()
    {
        if (movableRoot == null)
            return;

        if (UsesRequiredProductTarget)
            return;

        Physics.SyncTransforms();
        Vector3 half = new Vector3(
            footprintSize.x * 0.5f + collisionClearance,
            validationHeight * 0.5f,
            footprintSize.y * 0.5f + collisionClearance);
        Vector3 center = movableRoot.position + Vector3.up * (validationHeight * 0.5f + 0.035f);
        Collider[] overlaps = Physics.OverlapBox(
            center,
            half,
            movableRoot.rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        previewValid = true;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider collider = overlaps[i];
            if (collider == null || IsIgnoredCollider(collider))
                continue;
            previewValid = false;
            break;
        }

        UpdateIndicator();
    }

    private void RememberValidPreview()
    {
        if (!previewValid || movableRoot == null)
            return;
        lastValidPosition = movableRoot.position;
        lastValidRotation = movableRoot.rotation;
        hasLastValidPreview = true;
    }

    private bool IsIgnoredCollider(Collider collider)
    {
        if (collider is CharacterController)
            return true;
        if (movableRoot != null && collider.transform.IsChildOf(movableRoot))
            return true;
        string name = collider.name;
        if (string.Equals(name, "Floor", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "WalkableFloor", StringComparison.OrdinalIgnoreCase))
            return true;
        if (name.StartsWith("Baseboard", StringComparison.OrdinalIgnoreCase) ||
            name.IndexOf("Wainscot", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (placementKind == HomeProductPlacementKind.WallEdge &&
            (name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("Ribbon", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return true;
        }
        return false;
    }

    private void EnsureIndicator()
    {
        if (indicator != null)
            return;

        indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "PlacementValidityIndicator";
        indicator.transform.SetParent(movableRoot, false);
        indicator.transform.localPosition = new Vector3(0f, 0.018f, 0f);
        indicator.transform.localRotation = Quaternion.identity;
        indicator.transform.localScale = new Vector3(
            footprintSize.x + collisionClearance * 2f,
            0.012f,
            footprintSize.y + collisionClearance * 2f);
        Collider generatedCollider = indicator.GetComponent<Collider>();
        if (generatedCollider != null)
            DestroySafely(generatedCollider);

        indicatorRenderer = indicator.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        if (shader != null)
        {
            indicatorMaterial = new Material(shader) { name = "Runtime_PlacementIndicator" };
            indicatorMaterial.SetFloat("_Surface", 1f);
            indicatorMaterial.SetFloat("_ZWrite", 0f);
            indicatorMaterial.renderQueue = 3000;
            indicatorRenderer.sharedMaterial = indicatorMaterial;
        }
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        if (indicatorRenderer == null)
            return;
        Color color = previewValid ? ValidColor : InvalidColor;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        indicatorRenderer.SetPropertyBlock(block);
    }

    private void SetIndicatorVisible(bool visible)
    {
        if (indicator != null)
            indicator.SetActive(visible);
    }

    private void HideIndicator()
    {
        SetIndicatorVisible(false);
    }

    private void ResolveRoot()
    {
        if (movableRoot == null)
            movableRoot = transform;
    }

    private void ResolveBookSet()
    {
        if (bookshelfBookSet == null)
            bookshelfBookSet = GetComponent<HomeBookshelfBookSet>();
    }

    private void ResolveRequiredAttachment()
    {
        if (requiredProductAttachment == null)
            requiredProductAttachment = GetComponent<HomeRequiredProductAttachment>();
    }

    private void SetRequiredTargetPreview(bool snapped)
    {
        if (placementKind == HomeProductPlacementKind.BookshelfOnly)
        {
            ResolveBookSet();
            bookshelfBookSet?.SetPlacementPreview(constrainedTarget, snapped);
            return;
        }

        ResolveRequiredAttachment();
        requiredProductAttachment?.SetPlacementPreview(constrainedTarget, snapped);
    }

    private Transform FindRequiredPlacementTarget()
    {
        if (string.IsNullOrEmpty(requiredProductId) ||
            !HomeStoreService.IsOwned(requiredProductId))
        {
            return null;
        }

        HomeProductPlacement[] placements =
            FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include);
        for (int i = 0; i < placements.Length; i++)
        {
            HomeProductPlacement placement = placements[i];
            if (placement != null && placement != this &&
                string.Equals(placement.ProductId, requiredProductId, StringComparison.Ordinal))
            {
                return placement.MovableRoot;
            }
        }

        return null;
    }

    private void HandlePlacementChanged(string changedProductId)
    {
        if (string.IsNullOrEmpty(changedProductId) ||
            string.Equals(changedProductId, productId, StringComparison.Ordinal) ||
            string.Equals(changedProductId, requiredProductId, StringComparison.Ordinal))
        {
            ApplySavedPlacement();
        }
    }

    private void OnDestroy()
    {
        if (indicatorMaterial != null)
            DestroySafely(indicatorMaterial);
    }

    private static void DestroySafely(UnityEngine.Object target)
    {
        if (target == null)
            return;
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        string id,
        Transform root,
        Transform[] placementSlots,
        Vector2 footprint,
        HomeProductPlacementKind kind = HomeProductPlacementKind.Floor,
        string requiredId = null,
        float mountHeight = 0f)
    {
        productId = id;
        movableRoot = root != null ? root : transform;
        legacySlots = placementSlots ?? Array.Empty<Transform>();
        footprintSize = new Vector2(
            Mathf.Max(0.2f, footprint.x),
            Mathf.Max(0.2f, footprint.y));
        placementKind = kind;
        requiredProductId = requiredId;
        hungHeight = Mathf.Max(0f, mountHeight);
        ResolveBookSet();
        ResolveRequiredAttachment();
        roomMinimum = new Vector2(DefaultRoomLeft, DefaultRoomFront);
        roomMaximum = new Vector2(DefaultRoomRight, DefaultRoomBack);
        ApplySavedPlacement();
    }
#endif
}
