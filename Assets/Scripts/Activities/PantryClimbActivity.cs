using System.Collections;
using UnityEngine;

/// <summary>
/// Climb the pantry: hop to the low shelf, then the middle one, sniff along the
/// jars, and come back down in two hops.
///
/// The only Kitchen routine that stages more than one height, which is what
/// makes a 1.85 shelf unit worth entering at all. Each hop is its own arc so the
/// cat visibly steps up rather than levitating the whole way.
///
/// The controller is switched off for the routine, the cat is never re-parented
/// under the product, and it lands clear of the footprint before physics
/// resumes.
/// </summary>
[DisallowMultipleComponent]
public sealed class PantryClimbActivity : CatActivity
{
    [Header("Pantry climb")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform lowerShelfPoint;
    [SerializeField] private Transform upperShelfPoint;
    [SerializeField, Min(0.5f)] private float sniffDuration = 2.4f;
    [SerializeField] private bool directClimb;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatSupportedFurnitureMotion supportedMotion;
    public Transform UpperShelfPoint => upperShelfPoint;

    public override string ProgressLabel => IsRunning ? "EXPLORING..." : string.Empty;

    public float SniffDuration => Mathf.Max(0.5f, sniffDuration);
    public bool UsesIntermediatePerch => !directClimb;
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        return floorPoint != null && lowerShelfPoint != null && upperShelfPoint != null &&
            CatActivityStartResolver.GroundLaunch(this, actor, floorPoint.position,
                directClimb ? UpperLandingPosition : lowerShelfPoint.position, out start);
    }
    private Vector3 UpperLandingPosition => upperShelfPoint.position + (StoreProductId == "loft.tall-bookcase"
        ? transform.TransformDirection(Vector3.forward) * .08f : Vector3.zero);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || lowerShelfPoint == null || upperShelfPoint == null)
        {
            failureReason = "THE PANTRY IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(SupportedClimb());
        return true;
    }

    private IEnumerator SupportedClimb()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Vector3 floor = Flatten(floorPoint.position, AcceptedStart.Position.y), upper = UpperLandingPosition;
        bool bookcase=StoreProductId=="loft.tall-bookcase";
        Vector3 exit=floor;
        Vector3 exitDirection=StoreProductId==HomeStoreService.BalconyHerbShelfId?new Vector3(.94f,0,.342f):
            bookcase?new Vector3(-.7071f,0,.7071f):Vector3.zero;
        if(exitDirection.sqrMagnitude>.1f)
        {
            // Leave the narrow shelf diagonally from its long axis. Facing
            // squarely out would put the native crouch against the wall.
            // Choose the actual clear landing first and keep that heading
            // throughout the jump, with no shuffle along the shelf.
            bool found=false;exitDirection=transform.TransformDirection(exitDirection).normalized;
            foreach(float distance in new[]{.90f,1.05f,1.20f})
            {
                var candidate=Flatten(upper+exitDirection*distance,floor.y);
                if(!CatActivityMotion.IsControllerFloorClear(Cat,candidate)||!CatActivityMotion.TryFloorPath(candidate,floor,out _))continue;
                exit=candidate;found=true;break;
            }
            if(!found){CancelForTransition();yield break;}
        }
        Quaternion inward = LookTowards(upper - floor, AcceptedStart.Rotation);
        Quaternion facing = CatActivityFacing.AlongAxis(Cat, upper, upperShelfPoint.rotation * Quaternion.Euler(0, 90, 0));
        bool herbShelf=StoreProductId==HomeStoreService.BalconyHerbShelfId;
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, upperShelfPoint);
        if (!directClimb)
        {
            Vector3 lower = lowerShelfPoint.position;
            yield return supportedMotion.Jump(AcceptedStart.Position, lower, AcceptedStart.Rotation, inward);
            yield return supportedMotion.Jump(lower, upper, inward, facing);
        }
        else yield return supportedMotion.Jump(AcceptedStart.Position, upper, AcceptedStart.Rotation, facing);
        var area = upperShelfPoint.GetComponent<CatActivitySurface>();
        bool sleeping = area != null && area.ResolvePose(CatActivityPose.Sit) == CatActivityPose.Sleep;
        yield return supportedMotion.Pose(herbShelf?CatActivityPose.GentleKnead:CatActivityPose.SitDown, .55f, upper, facing);
        if (sleeping) yield return supportedMotion.Pose(CatActivityPose.TowelSettle, .90f, upper, facing);
        if(bookcase)supportedMotion.Rest(sleeping?CatActivityPose.Sleep:CatActivityPose.Sit);
        else PlayCatPose(herbShelf?CatActivityPose.GentleKnead:sleeping ? CatActivityPose.Sleep : CatActivityPose.Sit, upperShelfPoint);
        yield return new WaitForSeconds(SniffDuration);
        if (sleeping) yield return supportedMotion.Pose(CatActivityPose.TowelWake, .80f, upper, facing);
        yield return supportedMotion.Pose(herbShelf?CatActivityPose.GentleKnead:CatActivityPose.StandUp, .55f, upper, facing);
        if (!directClimb)
        {
            Vector3 lower = lowerShelfPoint.position;
            Quaternion outward = LookTowards(floor - lower, facing);
            yield return supportedMotion.Jump(upper, lower, facing, outward);
            yield return supportedMotion.Jump(lower, floor, outward, outward);
        }
        else yield return supportedMotion.Jump(upper, exit, facing, LookTowards(exit - upper, facing));
        RestoreCat(); CompleteActivity("NOTHING UP HERE!");
    }

    private void RestoreCat()
    {
        if (supportedMotion != null) { supportedMotion.End(); supportedMotion = null; }
        if (Cat == null)
            return;

        if (originalScale.sqrMagnitude > 0.0001f)
            Cat.transform.localScale = originalScale;
        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    private static Quaternion LookTowards(Vector3 direction, Quaternion fallback)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }

    private static Vector3 Flatten(Vector3 point, float y)
    {
        point.y = y;
        return point;
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureDirectClimb(bool value) => directClimb = value;

    public void EditorConfigureClimb(
        Transform floor, Transform lower, Transform upper, float sniff)
    {
        floorPoint = floor;
        lowerShelfPoint = lower;
        upperShelfPoint = upper;
        sniffDuration = Mathf.Max(0.5f, sniff);
    }
#endif
}
