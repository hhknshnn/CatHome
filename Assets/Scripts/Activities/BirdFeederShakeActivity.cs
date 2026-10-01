using System.Collections;
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
    public Vector3 ContactStand { get; private set; }
    public bool ContactStandBlocked { get; private set; }
    private Vector3 feederHome;
    private Quaternion feederHomeRotation;
    private Vector3 seedHome;
    private bool homeCaptured;
    private CatToyContactMotion pawContact;
    private CatPawReachMotion pawReach;
    public int ContactStrokes { get; private set; }
    public float MinimumPawDistance { get; private set; }
    public bool IsTapping { get; private set; }

    public override string ProgressLabel => IsRunning ? "REACHING..." : string.Empty;

    public Transform FeederPivot => feederPivot;
    public Transform SeedPivot => seedPivot;
    public int BatCount => Mathf.Max(1, batCount);
    public float SwingAngle => Mathf.Clamp(swingAngle, 2f, 30f);

    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        if (actor == null || reachPoint == null || feederPivot == null) return false;
        Vector3 stand = actor.transform.position;
        float distance = Vector3.ProjectOnPlane(transform.position - stand, Vector3.up).magnitude;
        // The pole must stay ahead of the chest; the old feeder-centred stand
        // allowed its shaft through the torso. Readiness never moves the cat.
        if (distance < poleStandDistance - .025f || distance > poleStandDistance + .025f ||
            !TryPoleContact(stand, out Vector3 contact)) return false;
        if (!CatActivityStartResolver.Facing(actor, transform.position, poleStandDistance + .025f,
            contact, 18f, out start) || !CatPawReachResolver.TryResolve(actor,contact,false,CatActivityPose.Paw,out var plan,0)) return false;
        start.PawPlan=plan; start.HasPawPlan=true; return true;
    }

    private bool TryPoleContact(Vector3 stand, out Vector3 contact)
    {
        contact = transform.position; contact.y = stand.y + .38f;
        Vector3 from = stand + Vector3.up * .38f;
        var hits = Physics.RaycastAll(from, (contact - from).normalized, .9f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<CatMovement>() != null) continue;
            if (!hit.transform.IsChildOf(transform)) return false;
            contact = hit.point + hit.normal * .008f;
            return true;
        }
        return false;
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (reachPoint == null || feederPivot == null)
        {
            failureReason = "NOTHING TO REACH";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        ContactStrokes=0;MinimumPawDistance=float.PositiveInfinity;
        ContactStand = AcceptedStart.Position;
        ContactStandBlocked = false;
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

        Vector3 reach = AcceptedStart.Position;
        Quaternion facing = AcceptedStart.Rotation;
        Vector3 contact = AcceptedStart.ActionTarget;
        pawContact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        pawReach=Cat.GetComponent<CatPawReachMotion>()??Cat.gameObject.AddComponent<CatPawReachMotion>();
        var hand=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-hand.R");
        if(hand==null){CancelForTransition();yield break;}
        for (int index = 0; index < BatCount; index++)
        {
            float bat = 0f;
            float strength = 0.55f + index * 0.22f;
            bool touched=false;IsTapping=true;
            while (bat < .80f)
            {
                if(Time.timeScale<=0f){yield return null;continue;}
                float t = Mathf.Clamp01(bat / .80f);
                Cat.transform.SetPositionAndRotation(reach,facing);
                pawReach.Sample(this,AcceptedStart.PawPlan,t);
                yield return new WaitForEndOfFrame();
                if(!IsRunning)yield break;
                if(Time.timeScale>0f&&t>.28f&&t<.7f)
                {
                    float distance=Vector3.Distance(hand.position,AcceptedStart.PawPlan.Target);
                    MinimumPawDistance=Mathf.Min(MinimumPawDistance,distance);
                    if(!touched&&distance<.025f){touched=true;GameAudio.Play(AudioCue.Feeder,.8f);}
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
            IsTapping=false;pawReach.Clear();pawContact.Clear();
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

    private void RestoreRig()
    {
        IsTapping=false;pawReach?.Clear();pawContact?.Clear();
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
