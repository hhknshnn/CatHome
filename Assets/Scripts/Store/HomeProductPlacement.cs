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
/// Keeps each room product at its authored location. Legacy preview entry points
/// remain for existing serialized UI references but cannot start a drag session.
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

    private bool cachedAuthoredPose;
    private bool reconcilePending;
    private Vector3 authoredPosition;
    private Quaternion authoredRotation;

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
    private static readonly Color WallValidColor = new Color(0.65f, 0.32f, 0.95f, 0.58f);
    private static readonly Color InvalidColor = new Color(1f, 0.22f, 0.18f, 0.62f);

    public string ProductId => productId;
    public bool IsPreviewing => previewing;
    public bool IsPreviewValid => previewing && previewValid;
    public HomeProductPlacementKind PlacementKind => placementKind;
    public string RequiredProductId => requiredProductId;
    public Transform MovableRoot => movableRoot != null ? movableRoot : transform;
    public Vector2 Footprint => footprintSize;
    public Vector3 PreviewPosition => movableRoot != null ? movableRoot.position : transform.position;
    public float PreviewRotationY => movableRoot != null ? movableRoot.eulerAngles.y : transform.eulerAngles.y;
    public bool SupportsRotation => !HomeStoreService.IsFixedRoomProduct(productId) && !CatCollectionPolicy.IsCatItem(productId);
    public Transform RequiredPlacementTarget =>
        UsesRequiredProductTarget ? FindRequiredPlacementTarget() : null;

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
        HomeStoreService.OwnershipChanged += HandlePlacementChanged;
        HomeStoreService.StorageChanged += HandlePlacementChanged;
        ApplySavedPlacement();
        reconcilePending = true;
    }

    private void OnDisable()
    {
        HomeStoreService.PlacementChanged -= HandlePlacementChanged;
        HomeStoreService.OwnershipChanged -= HandlePlacementChanged;
        HomeStoreService.StorageChanged -= HandlePlacementChanged;
        HideIndicator();
        previewing = false;
    }

    public bool BeginPreview()
    {
        if (HomeStoreService.IsFixedRoomProduct(productId) || CatCollectionPolicy.IsCatItem(productId))
            return false;
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

            previewValid = HomeStoreService.TryGetWorldPlacement(
                productId, out _, out _);
            hasLastValidPreview = previewValid;
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
            SetRequiredTargetPreview(previewValid);
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
        // Furniture ownership or another placement may have changed since drag.
        RefreshValidity();
        if (!CatCollectionPolicy.CanDisplay(productId, out _)) return false;
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

        if (HomeStoreService.IsFixedRoomProduct(productId))
        {
            if (!UsesRequiredProductTarget)
            {
                if (HomeRoomLayoutCatalog.TryGet(productId, out var layout) &&
                    HomeRoomService.TryGetRoom(layout.roomId, out var room) && gameObject.scene.path == room.ScenePath)
                    movableRoot.SetPositionAndRotation(layout.position, Quaternion.Euler(0, layout.yaw, 0));
                else movableRoot.SetPositionAndRotation(authoredPosition, authoredRotation);
            }
            return; // Book/TV components own their authored attachment.
        }

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
        float back = Mathf.Abs(roomMaximum.y - candidate.z);
        // Cat Home rooms have three authored walls. The open camera edge is a
        // floor boundary, never a valid wall-placement surface.
        float nearest = Mathf.Min(left, right, back);

        float yaw;
        if (Mathf.Approximately(nearest, left))
            yaw = 90f;
        else if (Mathf.Approximately(nearest, right))
            yaw = 270f;
        else if (Mathf.Approximately(nearest, back))
            yaw = 180f;
        else
            yaw = 270f;

        movableRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        GetWorldHalfExtents(out float halfX, out float halfZ);
        if (Mathf.Approximately(nearest, left))
            candidate.x = roomMinimum.x + halfX + WallSnapInset;
        else if (Mathf.Approximately(nearest, right))
            candidate.x = roomMaximum.x - halfX - WallSnapInset;
        else if (Mathf.Approximately(nearest, back))
            candidate.z = roomMaximum.y - halfZ - WallSnapInset;
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

    // The authored pad touches the rear panelling. Keep furniture, entrance
    // and front/side clearance; remove only empty padding behind this pose.
    private float AuthoredRearWallInset(Vector3 position,float yaw)
    {
        if(productId!=HomeStoreService.NapPillowId || gameObject.scene.path!=HomeRoomService.LivingRoomScenePath ||
           (position-CatRoomArrangement.RearWallNapPillowPosition).sqrMagnitude>.000001f || Mathf.Abs(Mathf.DeltaAngle(yaw,0))>.1f)return 0;
        float panel=CatRoomArrangement.RearPanelZ;
        return Mathf.Max(0,footprintSize.y*.5f+collisionClearance-(panel-position.z));
    }

    private void RefreshValidity(bool ignoreCatProducts = false)
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
        float rearInset=AuthoredRearWallInset(movableRoot.position,movableRoot.eulerAngles.y);
        center-=movableRoot.forward*(rearInset*.5f);half.z-=rearInset*.5f;
        Collider[] overlaps = Physics.OverlapBox(
            center,
            half,
            movableRoot.rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        previewValid = !OverlapsReservedProduct(ignoreCatProducts);
        var activity=GetComponent<CatActivity>();
        if(CatCollectionPolicy.IsCatItem(productId) && activity!=null && activity.RoutineEntryPoint!=null)
        {
            previewValid &= CatActivityMotion.IsFloorClear(activity.RoutineEntryPoint.position,.24f,ignoreCatProducts);
            var enrichment=activity as CatEnrichmentActivity;
            if(enrichment!=null && enrichment.Mode==CatEnrichmentMode.Tunnel && enrichment.ExitPoint!=null)
                previewValid &= CatActivityMotion.IsFloorClear(enrichment.ExitPoint.position,.24f,ignoreCatProducts);
        }
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider collider = overlaps[i];
            if (collider == null || IsIgnoredCollider(collider))
                continue;
            var other=collider.GetComponentInParent<HomeProductPlacement>();
            if(ignoreCatProducts && other!=null && CatCollectionPolicy.IsCatItem(other.ProductId)) continue;
            previewValid = false;
            break;
        }

        UpdateIndicator();
    }

    public bool IsCurrentPositionValid()
    {
        ResolveRoot(); RefreshValidity(); return previewValid;
    }

    public bool IsRoomPoseValid(Vector3 position, float yaw)
    {
        ResolveRoot();
        var oldPosition=MovableRoot.position;var oldRotation=MovableRoot.rotation;
        bool oldValid=previewValid;
        try
        {
            MovableRoot.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            GetWorldHalfExtents(out float x,out float z);
            if(position.x-x<roomMinimum.x || position.x+x>roomMaximum.x || position.z-z<roomMinimum.y || position.z+z-AuthoredRearWallInset(position,yaw)>roomMaximum.y)return false;
            RefreshValidity(true);return previewValid;
        }
        finally { MovableRoot.SetPositionAndRotation(oldPosition,oldRotation);previewValid=oldValid; }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || !reconcilePending || previewing) return;
        reconcilePending = false;
        if (!CatCollectionPolicy.IsCatItem(productId) || !HomeStoreService.IsOwned(productId) || HomeStoreService.IsStored(productId)) return;
        CatRoomArrangement.Request(gameObject.scene);
    }

    private bool OverlapsReservedProduct(bool ignoreCatProducts = false)
    {
        if (!CatCollectionPolicy.IsCatItem(productId)) return false;
        foreach (var other in FindObjectsByType<HomeProductPlacement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (other == this || other.gameObject.scene != gameObject.scene) continue;
            bool fixedRoom = HomeStoreService.IsFixedRoomProduct(other.ProductId);
            if(ignoreCatProducts && !fixedRoom)continue;
            if (!fixedRoom && (!CatCollectionPolicy.IsCatItem(other.ProductId) ||
                !HomeStoreService.IsOwned(other.ProductId) || HomeStoreService.IsStored(other.ProductId))) continue;
            if (fixedRoom && !other.ReservesFloorSpace()) continue;
            if (FootprintsOverlap(MovableRoot.position, footprintSize, MovableRoot.eulerAngles.y,
                other.MovableRoot.position, other.footprintSize, other.MovableRoot.eulerAngles.y, collisionClearance)) return true;
            var activity=GetComponent<CatActivity>();
            if(activity!=null && activity.RoutineEntryPoint!=null && FootprintsOverlap(activity.RoutineEntryPoint.position,new Vector2(.48f,.48f),0,
                other.MovableRoot.position,other.footprintSize,other.MovableRoot.eulerAngles.y,.04f))return true;
            var tunnel=activity as CatEnrichmentActivity;
            if(tunnel!=null && tunnel.Mode==CatEnrichmentMode.Tunnel && tunnel.ExitPoint!=null && FootprintsOverlap(tunnel.ExitPoint.position,new Vector2(.48f,.48f),0,
                other.MovableRoot.position,other.footprintSize,other.MovableRoot.eulerAngles.y,.04f))return true;
            if(!fixedRoom)
            {
                var otherActivity=other.GetComponent<CatActivity>();
                if(otherActivity!=null && otherActivity.RoutineEntryPoint!=null && FootprintsOverlap(MovableRoot.position,footprintSize,MovableRoot.eulerAngles.y,
                    otherActivity.RoutineEntryPoint.position,new Vector2(.48f,.48f),0,.04f))return true;
                var otherTunnel=otherActivity as CatEnrichmentActivity;
                if(otherTunnel!=null && otherTunnel.Mode==CatEnrichmentMode.Tunnel && otherTunnel.ExitPoint!=null && FootprintsOverlap(MovableRoot.position,footprintSize,MovableRoot.eulerAngles.y,
                    otherTunnel.ExitPoint.position,new Vector2(.48f,.48f),0,.04f))return true;
            }
        }
        return false;
    }

    public bool ReservesFloorSpace()
    {
        // Inspect inactive geometry too. Wall pictures and walkable rugs leave
        // the floor available; tall furniture reserves its future footprint.
        foreach (var filter in MovableRoot.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            var b=filter.sharedMesh.bounds;
            Vector3 low=filter.transform.TransformPoint(b.center-Vector3.up*b.extents.y);
            Vector3 high=filter.transform.TransformPoint(b.center+Vector3.up*b.extents.y);
            float min=Mathf.Min(low.y,high.y), max=Mathf.Max(low.y,high.y);
            if (min < .65f && max > .12f) return true;
        }
        return false;
    }

    public static bool FootprintsOverlap(Vector3 a, Vector2 aSize, float aYaw,
        Vector3 b, Vector2 bSize, float bYaw, float gap)
    {
        // Automatic CAT candidates use quarter turns. Avoid quaternion work and
        // allocations in the thousands of repeated layout/corridor comparisons.
        if(Mathf.Abs(Mathf.DeltaAngle(aYaw,Mathf.Round(aYaw/90f)*90f))<.001f &&
           Mathf.Abs(Mathf.DeltaAngle(bYaw,Mathf.Round(bYaw/90f)*90f))<.001f)
        {
            bool swapA=(Mathf.Abs(Mathf.RoundToInt(aYaw/90f))%2)==1,swapB=(Mathf.Abs(Mathf.RoundToInt(bYaw/90f))%2)==1;
            float aw=swapA?aSize.y:aSize.x,ad=swapA?aSize.x:aSize.y;
            float bw=swapB?bSize.y:bSize.x,bd=swapB?bSize.x:bSize.y;
            return Mathf.Abs(b.x-a.x)<(aw+bw)*.5f+gap && Mathf.Abs(b.z-a.z)<(ad+bd)*.5f+gap;
        }
        Vector3 ax=Quaternion.Euler(0,aYaw,0)*Vector3.right, az=Quaternion.Euler(0,aYaw,0)*Vector3.forward;
        Vector3 bx=Quaternion.Euler(0,bYaw,0)*Vector3.right, bz=Quaternion.Euler(0,bYaw,0)*Vector3.forward;
        Vector3 delta=b-a; delta.y=0;
        foreach(var axis in new[]{ax,az,bx,bz})
        {
            float ra=(Mathf.Abs(Vector3.Dot(ax,axis))*aSize.x+Mathf.Abs(Vector3.Dot(az,axis))*aSize.y)*.5f;
            float rb=(Mathf.Abs(Vector3.Dot(bx,axis))*bSize.x+Mathf.Abs(Vector3.Dot(bz,axis))*bSize.y)*.5f;
            if(Mathf.Abs(Vector3.Dot(delta,axis)) >= ra+rb+gap) return false;
        }
        return true;
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
        Color color = previewValid
            ? placementKind == HomeProductPlacementKind.WallEdge
                ? WallValidColor
                : ValidColor
            : InvalidColor;
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
        if (!cachedAuthoredPose)
        {
            authoredPosition = movableRoot.position;
            authoredRotation = movableRoot.rotation;
            cachedAuthoredPose = true;
        }
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
            !HomeStoreService.IsOwned(requiredProductId) ||
            HomeStoreService.IsStored(requiredProductId))
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
        reconcilePending = true;
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
