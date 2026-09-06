using System.Collections;
using UnityEngine;

/// <summary>
/// Walk the rim of the bath tub and paw at the water.
///
/// The tub already had a premium model but no beat of its own, and the shower
/// next to it owns "get wet" — so this one is balance and curiosity instead: the
/// cat hops onto the 0.89 rim, walks along it with a little wobble, stops over
/// the middle, dips a paw at the water three times, shakes it off and hops down.
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

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || rimStartPoint == null || rimEndPoint == null ||
            waterPoint == null)
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
        Vector3 water = waterPoint.position;

        Quaternion toFloor = LookTowards(floor - start, startRotation);
        yield return Move(start, floor, startRotation, toFloor, 0.32f);

        Quaternion up = LookTowards(
            new Vector3(rimStart.x - floor.x, 0f, rimStart.z - floor.z), toFloor);
        yield return Move(floor, floor, toFloor, up, 0.16f);

        Vector3 crouched = originalScale;
        crouched.y *= 0.76f;
        crouched.x *= 1.09f;
        crouched.z *= 1.09f;
        yield return Squash(originalScale, crouched, 0.17f);
        yield return Squash(crouched, originalScale, 0.09f);
        yield return Hop(floor, rimStart, up, 0.44f);

        // Walk the rim. The wobble is the whole point: a straight lerp along a
        // 0.89 ledge reads as the cat sliding on rails.
        Quaternion along = LookTowards(rimEnd - rimStart, up);
        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Walk);
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

        Vector3 perch = Vector3.Lerp(rimStart, rimEnd, 0.5f);
        yield return Move(rimEnd, perch, along, along, 0.28f);

        // Face the water and dip a paw at it.
        Quaternion toWater = LookTowards(
            new Vector3(water.x - perch.x, 0f, water.z - perch.z), along);
        yield return Move(perch, perch, along, toWater, 0.24f);
        for (int i = 0; i < DipCount; i++)
        {
            float dip = 0f;
            PlayCatPose(CatActivityPose.Paw);
            while (dip < 0.36f)
            {
                dip += Time.deltaTime;
                float reach = Mathf.Sin(Mathf.Clamp01(dip / 0.36f) * Mathf.PI);
                Vector3 position = perch;
                position += (toWater * Vector3.forward) * (reach * 0.095f);
                position.y = perch.y - reach * 0.055f;
                Cat.transform.position = position;
                Cat.transform.rotation = toWater * Quaternion.Euler(reach * 17f, 0f, 0f);
                yield return null;
            }
        }

        // Shake the paw off.
        float shake = 0f;
        while (shake < 0.40f)
        {
            shake += Time.deltaTime;
            float wobble = Mathf.Sin(shake * 34f) * (1f - shake / 0.40f);
            Cat.transform.position = perch;
            Cat.transform.rotation = toWater * Quaternion.Euler(0f, wobble * 13f, wobble * 9f);
            yield return null;
        }

        Cat.transform.position = perch;
        Quaternion down = LookTowards(floor - perch, toWater);
        yield return Move(perch, perch, toWater, down, 0.22f);
        yield return Hop(perch, floor, down, 0.40f);

        RestoreCat();
        CompleteActivity("STILL DRY!");
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        PlayCatPose(CatActivityPose.Hop);
        float peak = Mathf.Max(from.y, to.y) + 0.24f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = CatActivityMotion.JumpPosition(from, to, t, peak - Mathf.Max(from.y, to.y));
            Cat.transform.position = position;
            Cat.transform.rotation = facing;
            yield return null;
        }

        Cat.transform.position = to;
        Cat.transform.rotation = facing;
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        PlayCatPose(CatActivityPose.Walk);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Cat.transform.position = Vector3.Lerp(from, to, t);
            Cat.transform.rotation = Quaternion.Slerp(fromRotation, toRotation, t);
            yield return null;
        }

        Cat.transform.position = to;
        Cat.transform.rotation = toRotation;
    }

    private IEnumerator Squash(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Cat.transform.localScale =
                Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }

        Cat.transform.localScale = to;
    }

    private void RestoreCat()
    {
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

    protected override void OnDisable()
    {
        StopAllCoroutines();
        RestoreCat();
        base.OnDisable();
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
