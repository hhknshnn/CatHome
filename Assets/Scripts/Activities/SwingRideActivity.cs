using System.Collections;
using UnityEngine;

/// <summary>
/// Ride the patio porch swing.
///
/// The cat cannot climb furniture on its own — CatMovement has no jump and its
/// CharacterController stepOffset is 0.005 world units — so, like the tunnel and
/// the star tipi, this activity switches physics off and scripts the whole hop.
///
/// The premium model ships as two single-object FBX files on a shared origin,
/// `PatioPorchSwing_Premium` and `PatioPorchSwingSeat_Premium`. Only the bench
/// hangs under `SwingPivot`, so rocking the pivot rocks the seat, the chains and
/// the cushions together. `SwingSeatPoint` hangs under the same pivot, and the
/// cat is pinned to it every frame — the cat is never re-parented into the
/// product, which keeps its own hierarchy untouched.
/// </summary>
[DisallowMultipleComponent]
public sealed class SwingRideActivity : CatActivity
{
    [Header("Swing ride")]
    [SerializeField] private Transform swingPivot;
    [SerializeField] private Transform mountPoint;
    [SerializeField] private Transform seatPoint;
    [SerializeField, Min(1f)] private float rideDuration = 4.2f;
    [SerializeField, Range(1f, 25f)] private float swingAngle = 9f;
    [SerializeField, Min(0.2f)] private float swingSpeed = 2.3f;
    [SerializeField, Min(0f)] private float bondReward = 4f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatSupportedFurnitureMotion supportedMotion;

    public override string ProgressLabel => IsRunning ? "SWINGING!" : string.Empty;

    public float RideDuration => Mathf.Max(1f, rideDuration);
    public override bool SupportsContinuousRest=>true;
    public override float EnergyCost=>0f;
    public float SwingAngle => Mathf.Clamp(swingAngle, 1f, 25f);
    public long BondReward => (long)Mathf.Max(0f, bondReward);
    public Transform SwingPivot => swingPivot;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (swingPivot == null || mountPoint == null || seatPoint == null)
        {
            failureReason = "THE SWING IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(RideRoutine());
        return true;
    }

    private IEnumerator RideRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;
        swingPivot.localRotation = Quaternion.identity;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 mount = Flatten(mountPoint.position, start.y);

        Quaternion toMount = LookTowards(mount - start, startRotation);
        yield return Move(start, mount, startRotation, toMount, 0.34f);

        // Hop up onto the bench, facing back out of the swing.
        Quaternion facing = CatActivityFacing.AlongAxis(Cat, seatPoint.position,
            seatPoint.rotation * Quaternion.Euler(0f, 90f, 0f));
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, seatPoint);
        yield return supportedMotion.Jump(mount, seatPoint.position, toMount, facing);
        var area = seatPoint.GetComponent<CatActivitySurface>();
        bool sleeping = area != null && area.ResolvePose(CatActivityPose.Sit) == CatActivityPose.Sleep;
        yield return supportedMotion.Pose(CatActivityPose.SitDown, .55f, seatPoint.position, facing);
        if (sleeping) yield return supportedMotion.Pose(CatActivityPose.TowelSettle, .90f, seatPoint.position, facing);

        // The seat point rides under the pivot, so pinning the cat to it each
        // frame swings the cat with the bench without touching its hierarchy.
        Quaternion pivotRest = swingPivot.rotation;
        float elapsed = 0f;
        float duration = RideDuration;
        PlayCatPose(sleeping ? CatActivityPose.Sleep : CatActivityPose.Sit, seatPoint);
        while (KeepResting)
        {
            elapsed += Time.deltaTime;
            // Ease the rocking in and out so it starts and stops gently.
            float envelope = Mathf.SmoothStep(0,1,elapsed/1.2f);
            float angle = CatRunnerProgressService.ReducedMotion?0:SwingAngle * envelope * Mathf.Sin(elapsed * swingSpeed * Mathf.PI);
            swingPivot.localRotation = Quaternion.Euler(angle, 0f, 0f);
            Cat.transform.position = seatPoint.position;
            Cat.transform.rotation = swingPivot.rotation * Quaternion.Inverse(pivotRest) * facing;
            yield return null;
        }

        var stoppingRotation=swingPivot.localRotation;float stopping=0;
        while(stopping<.5f){stopping+=Time.deltaTime;swingPivot.localRotation=Quaternion.Slerp(stoppingRotation,Quaternion.identity,Mathf.SmoothStep(0,1,stopping/.5f));Cat.transform.position=seatPoint.position;Cat.transform.rotation=swingPivot.rotation*Quaternion.Inverse(pivotRest)*facing;yield return null;}
        swingPivot.localRotation = Quaternion.identity;
        Cat.transform.position = seatPoint.position;
        Cat.transform.rotation = facing;

        if (sleeping) yield return supportedMotion.Pose(CatActivityPose.TowelWake, .80f, seatPoint.position, facing);
        yield return supportedMotion.Pose(CatActivityPose.StandUp, .55f, seatPoint.position, facing);

        Vector3 landing = Flatten(mountPoint.position, start.y);
        yield return supportedMotion.Jump(Cat.transform.position, landing, Cat.transform.rotation,
                         LookTowards(landing - seatPoint.position, Cat.transform.rotation));

        RestoreCat();
        if (BondReward > 0L)
            ProgressionService.AddBondXp(BondReward);
        CompleteActivity("WHEEE!");
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        yield return CatActivityMotion.WalkAuthoredStep(Cat, to, toRotation, duration);
    }

    private IEnumerator Hop(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation,
        float duration, float arcHeight)
    {
        yield return CatActivityMotion.Jump(Cat,from,to,fromRotation,toRotation,arcHeight);
    }

    private void RestoreCat()
    {
        supportedMotion?.End(); supportedMotion = null;
        if (swingPivot != null)
            swingPivot.localRotation = Quaternion.identity;
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
    public void EditorConfigureSwing(
        Transform pivot, Transform mount, Transform seat,
        float duration, float angle, float speed, float bond)
    {
        swingPivot = pivot;
        mountPoint = mount;
        seatPoint = seat;
        rideDuration = Mathf.Max(1f, duration);
        swingAngle = Mathf.Clamp(angle, 1f, 25f);
        swingSpeed = Mathf.Max(0.2f, speed);
        bondReward = Mathf.Max(0f, bond);
    }
#endif
}
