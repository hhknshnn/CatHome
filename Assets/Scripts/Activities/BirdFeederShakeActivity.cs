using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tap the reachable feeder pole from the deck. Actual paw contact starts the
/// suspended feeder and seeds moving; cancellation restores both pivots.
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
    [SerializeField, Min(.5f)] private float poleStandDistance = .66f;

    private CharacterController characterController;
    private List<Vector3> contactPath;
    public Vector3 ContactStand { get; private set; }
    public bool ContactStandBlocked { get; private set; }
    private Vector3 feederHome;
    private Quaternion feederHomeRotation;
    private Vector3 seedHome;
    private bool homeCaptured;
    private CatToyContactMotion pawContact;
    public int ContactStrokes { get; private set; }
    public float MinimumPawDistance { get; private set; }
    public bool IsTapping { get; private set; }

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

        Vector3 origin = RoutineFloorPosition; origin.y = Cat.transform.position.y;
        ContactStandBlocked = !FindPoleStand(origin, out Vector3 stand);
        ContactStand = stand;
        failureReason = ContactStandBlocked ? "LET'S MAKE SOME ROOM." : string.Empty;
        return !ContactStandBlocked;
    }

    protected override bool BeginActivity()
    {
        ContactStrokes=0;MinimumPawDistance=float.PositiveInfinity;
        ContactStandBlocked = !FindPoleStand(Cat.transform.position, out Vector3 stand);
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

    private bool FindPoleStand(Vector3 origin, out Vector3 stand)
    {
        Vector3 pole = transform.position; pole.y = Cat.transform.position.y;
        Vector3 offset = reachPoint.position - pole; offset.y = 0f;
        if (offset.sqrMagnitude < .0001f) offset = -transform.forward;
        // The old radius was measured from the hanging feeder: only .184 m.
        // That put the pole through the torso and sent a paw behind its shoulder.
        Vector3 authored = pole + offset.normalized * poleStandDistance;
        return CatActivityFacing.TryFindContactStand(Cat, pole, authored, origin,
            out stand, out contactPath, null, true);
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

        Vector3 up = (transform.position - reach);
        up.y = 0f;
        Vector3 push = up.sqrMagnitude > 0.0001f ? up.normalized : Cat.transform.forward;
        // A slight diagonal leaves the head beside the pole while the nearer
        // right foreleg taps it. Reaching straight through the head looked clipped.
        Quaternion facing = LookTowards(push, toReach) * Quaternion.Euler(0f, -5f, 0f);
        PlayCatPose(CatActivityPose.GentleKnead);
        yield return CatActivityFacing.Turn(Cat, facing, .2f);

        // Tap the reachable pole. The suspended feeder responds to that
        // contact; the cat's root and supporting feet stay on the deck.
        Vector3 contact=transform.position;contact.y=reach.y+.38f;
        Vector3 rayFrom=reach+Vector3.up*.38f;
        var hits=Physics.RaycastAll(rayFrom,(contact-rayFrom).normalized,1.5f,~0,QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        bool found=false;
        foreach(var hit in hits)if(hit.transform.IsChildOf(transform))
        {contact=hit.point+hit.normal*.008f;found=true;break;}
        if(!found){CancelForTransition();yield break;}
        pawContact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        var animation=Cat.GetComponent<CatActivityAnimation>();
        for (int index = 0; index < BatCount; index++)
        {
            float bat = 0f;
            float strength = 0.55f + index * 0.22f;
            bool touched=false;IsTapping=true;
            while (bat < .80f)
            {
                float t = Mathf.Clamp01(bat / .80f);
                Cat.transform.SetPositionAndRotation(reach,facing);
                animation.SetTimedPose(CatActivityPose.Paw,t);
                pawContact.Reach(contact,false,t);
                if(t>.28f&&t<.7f)
                {
                    MinimumPawDistance=Mathf.Min(MinimumPawDistance,pawContact.Distance);
                    if(!touched&&pawContact.Distance<.025f){touched=true;GameAudio.Play(AudioCue.Feeder,.8f);}
                }

                float swing = Mathf.Sin(t * Mathf.PI * 2.2f) * (1f - t * 0.35f)
                              * SwingAngle * strength;
                if(touched)feederPivot.localRotation =
                    feederHomeRotation * Quaternion.AngleAxis(swing, Vector3.right);
                if (touched && seedPivot != null)
                {
                    float scatter = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3.0f))
                                    * 0.055f * strength;
                    seedPivot.localPosition = seedHome - Vector3.up * scatter;
                }

                yield return null;bat+=Time.deltaTime;
            }
            IsTapping=false;pawContact.Clear();
            if(!touched){CancelForTransition();yield break;}
            ContactStrokes++;
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
        IsTapping=false;pawContact?.Clear();
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
