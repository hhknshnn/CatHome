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

        // Walk the rim. The wobble is the whole point: a straight lerp along a
        // 0.89 ledge reads as the cat sliding on rails.
        Quaternion along = LookTowards(rimEnd - rimStart, up);
        yield return CatActivityFacing.Turn(Cat, along, .2f);
        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Walk);
        IsRimWalking = true;
        while (elapsed < WalkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / WalkDuration);
            Vector3 position = Vector3.Lerp(rimStart, rimEnd, Mathf.SmoothStep(0f, 1f, t));
            position.y += Mathf.Abs(Mathf.Sin(elapsed * 7.4f)) * 0.020f;
            Cat.transform.position = position;
            Cat.transform.rotation = along * Quaternion.Euler(0f, 0f, Mathf.Sin(elapsed * 4.6f) * 8f);
            yield return null;
        }

        // Leave from the end reached by the balance walk. Returning to the
        // centre and reaching toward the water read as an unrelated paw attack.
        Cat.transform.SetPositionAndRotation(rimEnd, along);
        IsRimWalking = false;
        Quaternion down = LookTowards(floor - rimEnd, along);
        yield return Hop(rimEnd, floor, down, 0.40f);

        RestoreCat();
        CompleteActivity("STILL DRY!");
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        yield return CatActivityMotion.Jump(Cat,from,to,Cat.transform.rotation,facing);
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        Vector3 direction = to - from; direction.y = 0f;
        if (direction.sqrMagnitude < .000001f)
        {
            // An already reached entry is not an extra stationary work beat.
            if (Quaternion.Angle(Cat.transform.rotation, toRotation) <= .1f) yield break;
            PlayCatPose(CatActivityPose.GentleKnead);
            yield return CatActivityFacing.Turn(Cat, toRotation, duration);
            yield break;
        }

        // Turn on the spot first. Interpolating a travel position while still
        // facing the previous action made the return leg slide backwards.
        Quaternion travel = Quaternion.LookRotation(direction, Vector3.up);
        PlayCatPose(CatActivityPose.GentleKnead);
        yield return CatActivityFacing.Turn(Cat, travel, .16f);
        PlayCatPose(CatActivityPose.Walk);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Cat.transform.SetPositionAndRotation(Vector3.Lerp(from, to, t), travel);
            yield return null;
        }

        Cat.transform.SetPositionAndRotation(to, travel);
        if (Quaternion.Angle(travel, toRotation) > .1f)
        {
            PlayCatPose(CatActivityPose.GentleKnead);
            yield return CatActivityFacing.Turn(Cat, toRotation, .16f);
        }
    }

    private void RestoreCat()
    {
        IsRimWalking = false;
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
