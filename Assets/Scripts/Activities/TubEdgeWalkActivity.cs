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

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 floor = Flatten(floorPoint.position, start.y);
        Vector3 rimStart = rimStartPoint.position;
        Vector3 rimEnd = rimEndPoint.position;
        // The old endpoints were beyond the curved lip. Leave room for the
        // entire animal, then measure the actual low rim of this built model.
        Vector3 axis = (rimEnd - rimStart).normalized;
        Vector3 inward = waterPoint != null ? waterPoint.position - (rimStart + rimEnd) * .5f : transform.forward;
        inward = Vector3.ProjectOnPlane(inward, Vector3.up).normalized;
        float inset = Mathf.Min(.40f, Vector3.Distance(rimStart, rimEnd) * .30f);
        rimStart += axis * inset + inward * .045f;
        rimEnd -= axis * inset - inward * .045f;
        rimMotion = Cat.GetComponent<CatTubRimMotion>();
        if (rimMotion == null) rimMotion = Cat.gameObject.AddComponent<CatTubRimMotion>();
        rimMotion.Prepare(this, GetComponentInChildren<MeshCollider>(), rimStartPoint.position, rimEndPoint.position);
        rimStart.y = rimMotion.HeightAt(rimStart) - .045f;
        rimEnd.y = rimMotion.HeightAt(rimEnd) - .045f;
        Quaternion authoredAlong = LookTowards(rimEnd - rimStart, startRotation);
        Quaternion viewAlong = CatActivityFacing.AlongAxis(Cat, (rimStart + rimEnd) * .5f, authoredAlong);
        if (Vector3.Dot(viewAlong * Vector3.forward, rimEnd - rimStart) < 0f)
        { Vector3 swap = rimStart; rimStart = rimEnd; rimEnd = swap; }
        SelectedRimStart = rimStart; SelectedRimEnd = rimEnd;

        Quaternion toFloor = LookTowards(floor - start, startRotation);
        yield return Move(start, floor, startRotation, toFloor, 0.32f);

        Quaternion up = LookTowards(
            new Vector3(rimStart.x - floor.x, 0f, rimStart.z - floor.z), toFloor);
        yield return Move(floor, floor, toFloor, up, 0.16f);

        yield return Hop(floor, rimStart, up, 0.44f);

        Quaternion along = LookTowards(rimEnd - rimStart, up);
        yield return CatActivityMotion.TurnForStep(Cat, along);
        float elapsed = 0f;
        var animation = Cat.GetComponent<CatActivityAnimation>();
        animation.SetWalkSpeed(Vector3.Distance(rimStart, rimEnd) / WalkDuration, null);
        IsRimWalking = true;
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
        IsRimWalking = false;
        rimMotion.Clear();
        Quaternion down = LookTowards(floor - rimEnd, along);
        yield return Hop(rimEnd, floor, down, 0.40f);

        RestoreCat();
        CompleteActivity("STILL DRY!");
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
