using System.Collections;
using UnityEngine;

/// <summary>
/// Hop onto the bath rim, balance along the edge, then land on the open floor.
///
/// Like every scripted activity here the CharacterController is switched off for
/// the routine, the cat is never re-parented under the product, and it lands
/// clear of the footprint before physics resumes. The tub's product box stays
/// solid: the cat goes over the rim, not through the side.
/// </summary>
[DisallowMultipleComponent]
public sealed class TubEdgeWalkActivity : CatActivity
{
    [Header("Tub edge walk")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform rimStartPoint;
    [SerializeField] private Transform rimEndPoint;
    [SerializeField] private Transform waterPoint;
    [SerializeField, Min(0.3f)] private float walkDuration = 1.8f;
    [SerializeField, Min(1)] private int dipCount = 3;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatTubRimMotion rimMotion;
    private float nextBalanceSpeech;

    public override string ProgressLabel => IsRunning ? "BALANCING..." : string.Empty;

    public float WalkDuration => Mathf.Max(0.3f, walkDuration);
    public int DipCount => Mathf.Max(1, dipCount);
    public bool IsRimWalking { get; private set; }
    public Vector3 SelectedRimStart { get; private set; }
    public Vector3 SelectedRimEnd { get; private set; }
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        if (actor == null || floorPoint == null || rimStartPoint == null || rimEndPoint == null) return false;
        ResolveRimPath(actor, out Vector3 first, out _);
        return CatActivityStartResolver.GroundLaunch(this, actor, floorPoint.position, first, out start);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || rimStartPoint == null || rimEndPoint == null)
        {
            failureReason = "THE TUB IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        StartCoroutine(EdgeRoutine());
        return true;
    }

    private IEnumerator EdgeRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 floor = Flatten(floorPoint.position, AcceptedStart.Position.y);
        ResolveRimPath(Cat, out Vector3 rimStart, out Vector3 rimEnd);
        // A camera transition after acceptance must not exchange the ends of
        // the already prepared jump. Its actual rim height is measured below.
        if (Vector3.ProjectOnPlane(rimStart - AcceptedStart.ActionTarget, Vector3.up).sqrMagnitude >
            Vector3.ProjectOnPlane(rimEnd - AcceptedStart.ActionTarget, Vector3.up).sqrMagnitude)
        { Vector3 swap = rimStart; rimStart = rimEnd; rimEnd = swap; }
        rimMotion = Cat.GetComponent<CatTubRimMotion>();
        if (rimMotion == null) rimMotion = Cat.gameObject.AddComponent<CatTubRimMotion>();
        rimMotion.Prepare(this, GetComponentInChildren<MeshCollider>(), rimStartPoint.position, rimEndPoint.position);
        rimStart.y = rimMotion.HeightAt(rimStart) - .045f;
        rimEnd.y = rimMotion.HeightAt(rimEnd) - .045f;
        SelectedRimStart = rimStart; SelectedRimEnd = rimEnd;
        Quaternion up = LookTowards(rimStart - floor, AcceptedStart.Rotation);
        yield return Hop(AcceptedStart.Position, rimStart, up, 0.44f);

        // The landing pivot is already supported by the rim, before walking.
        IsRimWalking = true;
        Quaternion along = LookTowards(rimEnd - rimStart, up);
        yield return CatActivityMotion.TurnForStep(Cat, along);
        float elapsed = 0f;
        var animation = Cat.GetComponent<CatActivityAnimation>();
        animation.SetWalkSpeed(Vector3.Distance(rimStart, rimEnd) / WalkDuration, null);
        if (Time.time >= nextBalanceSpeech)
        {
            ShowSpeech("TUB_BALANCE");
            nextBalanceSpeech = Time.time + 30f;
        }
        while (elapsed < WalkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / WalkDuration);
            Vector3 position = Vector3.Lerp(rimStart, rimEnd, t);
            // A slight crouch leaves reach for narrow, tucked paw placements.
            position.y = rimMotion.HeightAt(position) - .045f;
            Cat.transform.position = position;
            Cat.transform.rotation = along;
            rimMotion.BalanceSeconds = elapsed;
            yield return null;
        }

        // Leave from the end reached by the balance walk. Returning to the
        // centre and reaching toward the water read as an unrelated paw attack.
        Cat.transform.SetPositionAndRotation(rimEnd, along);
        // The departure pivot still stands on this rim. Keep its measured
        // support bound until the jump finishes; native flight owns its bones.
        Quaternion down = LookTowards(floor - rimEnd, along);
        yield return Hop(rimEnd, floor, down, 0.40f);
        IsRimWalking = false;
        rimMotion.Clear();

        RestoreCat();
        CompleteActivity("STILL DRY!");
    }

    private void ResolveRimPath(CatMovement actor, out Vector3 first, out Vector3 last)
    {
        first = rimStartPoint.position;
        last = rimEndPoint.position;
        // Share the inset and direction choice with the prompt without adding
        // a rig component, playing a pose or changing the actor's transform.
        Vector3 axis = (last - first).normalized;
        Vector3 inward = waterPoint != null ? waterPoint.position - (first + last) * .5f : transform.forward;
        inward = Vector3.ProjectOnPlane(inward, Vector3.up).normalized;
        float inset = Mathf.Min(.40f, Vector3.Distance(first, last) * .30f);
        first += axis * inset + inward * .045f;
        last -= axis * inset - inward * .045f;
        Quaternion authoredAlong = LookTowards(last - first, actor.transform.rotation);
        Quaternion viewAlong = CatActivityFacing.AlongAxis(actor, (first + last) * .5f, authoredAlong);
        if (Vector3.Dot(viewAlong * Vector3.forward, last - first) < 0f)
        { Vector3 swap = first; first = last; last = swap; }
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        yield return CatActivityMotion.Jump(Cat,from,to,Cat.transform.rotation,facing,.20f,false);
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        yield return CatActivityMotion.WalkAuthoredStep(Cat, to, toRotation, duration);
    }

    private void RestoreCat()
    {
        IsRimWalking = false;
        if (rimMotion != null) rimMotion.Clear();
        if (Cat == null)
            return;

        if (originalScale.sqrMagnitude > 0.0001f)
            Cat.transform.localScale = originalScale;
        Vector3 forward = Cat.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
            Cat.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
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
    public void EditorConfigureEdge(
        Transform floor, Transform rimStart, Transform rimEnd, Transform water,
        float duration, int dips)
    {
        floorPoint = floor;
        rimStartPoint = rimStart;
        rimEndPoint = rimEnd;
        waterPoint = water;
        walkDuration = Mathf.Max(0.3f, duration);
        dipCount = Mathf.Max(1, dips);
    }
#endif
}
