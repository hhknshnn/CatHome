using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rear up against the feeder pole until the feeder swings and the seed rains
/// down — the Balcony's own routine.
///
/// Every other beat in the room is a reuse. This one is not, because nothing
/// else in the project has a cat working a product from BELOW: the perches,
/// climbs and naps all put the cat on top of something. Here the cat stays on
/// the deck, stretches up on its back legs and bats a thing it cannot reach,
/// which is the most cat-like use of a bird feeder there is.
///
/// The feeder hangs under a pivot the routine drives, the way the nightstand's
/// glass does, and it is returned to rest at the end: a feeder left swinging
/// would still be swinging when the next cat walked past. The seed is a second
/// pivot that drops and resets with it.
///
/// The cat itself stays on its own feet; the controller is switched off only so
/// the reach can push it through the pole's collider.
/// </summary>
[DisallowMultipleComponent]
public sealed class BirdFeederShakeActivity : CatActivity
{
    [Header("Bird feeder")]
    [SerializeField] private Transform reachPoint;
    [SerializeField] private Transform feederPivot;
    [SerializeField] private Transform seedPivot;
    [SerializeField, Min(1)] private int batCount = 3;
    [SerializeField, Range(2f, 30f)] private float swingAngle = 14f;
    [SerializeField, Min(0.1f)] private float settleDuration = 1.1f;

    private CharacterController characterController;
    private List<Vector3> contactPath;
    public Vector3 ContactStand { get; private set; }
    public bool ContactStandBlocked { get; private set; }
    private Vector3 feederHome;
    private Quaternion feederHomeRotation;
    private Vector3 seedHome;
    private bool homeCaptured;

    public override string ProgressLabel => IsRunning ? "REACHING..." : string.Empty;

    public Transform FeederPivot => feederPivot;
    public Transform SeedPivot => seedPivot;
    public int BatCount => Mathf.Max(1, batCount);
    public float SwingAngle => Mathf.Clamp(swingAngle, 2f, 30f);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (reachPoint == null || feederPivot == null)
        {
            failureReason = "NOTHING TO REACH";
            return false;
        }

        Vector3 authored = reachPoint.position; authored.y = Cat.transform.position.y;
        Vector3 origin = RoutineFloorPosition; origin.y = authored.y;
        ContactStandBlocked = !CatActivityFacing.TryFindContactStand(Cat, feederPivot.position,
            authored, origin, out Vector3 stand, out contactPath);
        ContactStand = stand;
        failureReason = ContactStandBlocked ? "LET'S MAKE SOME ROOM." : string.Empty;
        return !ContactStandBlocked;
    }

    protected override bool BeginActivity()
    {
        Vector3 authored = reachPoint.position; authored.y = Cat.transform.position.y;
        ContactStandBlocked = !CatActivityFacing.TryFindContactStand(Cat, feederPivot.position,
            authored, Cat.transform.position, out Vector3 stand, out contactPath);
        ContactStand = stand;
        if (ContactStandBlocked) return false;
        characterController = Cat.GetComponent<CharacterController>();
        if (!homeCaptured)
        {
            feederHome = feederPivot.localPosition;
            feederHomeRotation = feederPivot.localRotation;
            seedHome = seedPivot != null ? seedPivot.localPosition : Vector3.zero;
            homeCaptured = true;
        }

        StartCoroutine(ShakeRoutine());
        return true;
    }

    private IEnumerator ShakeRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 reach = Flatten(ContactStand, start.y);

        Quaternion toReach = startRotation;
        foreach (Vector3 waypoint in contactPath)
        {
            Vector3 destination = Flatten(waypoint, start.y);
            Vector3 from = Cat.transform.position;
            toReach = LookTowards(destination - from, Cat.transform.rotation);
            yield return Move(from, destination, Cat.transform.rotation, toReach,
                Mathf.Max(.18f, Vector3.Distance(from, destination) / 1.1f));
        }

        Vector3 up = (feederPivot.position - reach);
        up.y = 0f;
        Vector3 push = up.sqrMagnitude > 0.0001f ? up.normalized : Cat.transform.forward;
        Quaternion facing = LookTowards(push, toReach);
        PlayCatPose(CatActivityPose.GentleKnead);
        yield return CatActivityFacing.Turn(Cat, facing, .2f);

        // Each bat: the cat rears, the feeder swings away and comes back a
        // little short of where it started, so the swing reads as building up.
        for (int index = 0; index < BatCount; index++)
        {
            float bat = 0f;
            float strength = 0.55f + index * 0.22f;
            PlayCatPose(CatActivityPose.Paw);
            while (bat < 0.46f)
            {
                bat += Time.deltaTime;
                float t = Mathf.Clamp01(bat / 0.46f);
                float rear = Mathf.Sin(t * Mathf.PI);
                // The cat stands up rather than stepping forward: this is the
                // only routine in the game where it works a product from below.
                Cat.transform.position = reach + Vector3.up * (rear * 0.085f)
                                        + push * (rear * 0.040f);
                Cat.transform.rotation = facing * Quaternion.Euler(-rear * 26f, 0f, 0f);

                float swing = Mathf.Sin(t * Mathf.PI * 2.2f) * (1f - t * 0.35f)
                              * SwingAngle * strength;
                feederPivot.localRotation =
                    feederHomeRotation * Quaternion.AngleAxis(swing, Vector3.right);
                if (seedPivot != null)
                {
                    float scatter = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3.0f))
                                    * 0.055f * strength;
                    seedPivot.localPosition = seedHome - Vector3.up * scatter;
                }

                yield return null;
            }

            Cat.transform.position = reach;
            Cat.transform.rotation = facing;
            yield return Wait(0.16f);
        }

        // Settle: the feeder rocks itself back to rest before the cat leaves.
        float settle = 0f;
        float settleTime = Mathf.Max(0.1f, settleDuration);
        PlayCatPose(CatActivityPose.Sit);
        while (settle < settleTime)
        {
            settle += Time.deltaTime;
            float t = Mathf.Clamp01(settle / settleTime);
            float swing = Mathf.Sin(t * Mathf.PI * 5.0f) * (1f - t) * SwingAngle * 0.5f;
            feederPivot.localRotation =
                feederHomeRotation * Quaternion.AngleAxis(swing, Vector3.right);
            if (seedPivot != null)
                seedPivot.localPosition = Vector3.Lerp(
                    seedPivot.localPosition, seedHome, t);
            yield return null;
        }

        Quaternion away = CatActivityFacing.Resolve(Cat, reach, LookTowards(start - reach, facing));
        yield return Move(reach, reach, facing, away, 0.22f);

        RestoreRig();
        CompleteActivity("SEED RAIN!");
    }

    private static IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation,
        float duration)
    {
        Vector3 direction = to - from; direction.y = 0f;
        if (direction.sqrMagnitude < .000001f)
        {
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

    private void RestoreRig()
    {
        // The feeder goes back to rest. A product left mid-swing would still be
        // swinging the next time the cat walked past it.
        if (homeCaptured)
        {
            if (feederPivot != null)
            {
                feederPivot.localPosition = feederHome;
                feederPivot.localRotation = feederHomeRotation;
            }

            if (seedPivot != null)
                seedPivot.localPosition = seedHome;
        }

        if (Cat == null)
            return;

        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreRig();
        base.CancelActivity();
    }

    private static Vector3 Flatten(Vector3 point, float y)
    {
        point.y = y;
        return point;
    }

    private static Quaternion LookTowards(Vector3 direction, Quaternion fallback)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }

#if UNITY_EDITOR
    public void EditorConfigureShake(
        Transform reach, Transform feeder, Transform seed, int bats, float angle,
        float settle)
    {
        reachPoint = reach;
        feederPivot = feeder;
        seedPivot = seed;
        batCount = Mathf.Max(1, bats);
        swingAngle = Mathf.Clamp(angle, 2f, 30f);
        settleDuration = Mathf.Max(0.1f, settle);
    }
#endif
}
