using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bathroom paper uses measured, real paw contacts to spin the roll and tear
/// sheets. The cat stays on a clear floor stance with its normal proportions.
/// Other authored uses, including the record player, keep their existing routine.
/// </summary>
[DisallowMultipleComponent]
public sealed class PaperSpinActivity : CatActivity
{
    [Header("Paper spin")]
    [SerializeField] private Transform rollPivot;
    [SerializeField] private Transform swatPoint;
    [SerializeField] private Transform paperContactPoint;
    [SerializeField, Min(1)] private int swats = 3;
    [SerializeField, Min(60f)] private float spinPerSwat = 620f;
    [SerializeField, Min(0.2f)] private float spinDamping = 1.15f;

    private CharacterController characterController;
    private Vector3 originalScale;
    private Quaternion pivotRest;
    private CatPaperRollPawMotion paperPaw;
    private CatPaperTearFx paperFx;
    private List<Vector3> paperApproach;
    private List<Vector3> paperExit;
    private List<Vector3> recordApproach;
    private List<Vector3> recordExit;
    public Vector3 RecordStand { get; private set; }
    public bool RecordStandBlocked { get; private set; }
    private float paperSpeed;
    private int lastPaperStroke;
    private CatToyContactMotion recordPaw;
    public bool IsRecordTapping { get; private set; }
    public int RecordContactCount { get; private set; }
    public float RecordContactDistance { get; private set; } = float.PositiveInfinity;

    public override string ProgressLabel => IsRunning ? "SPINNING..." : string.Empty;

    public int Swats => Mathf.Max(1, swats);
    public float SpinPerSwat => Mathf.Max(60f, spinPerSwat);
    public float SpinDamping => Mathf.Max(0.2f, spinDamping);
    public Transform RollPivot => rollPivot;
    public Vector3 SpinAxis => Kind == CatActivityKind.RecordSpin ? Vector3.up : Vector3.right;
    public bool UsesPaperTears => Kind == CatActivityKind.PaperSpin && StoreProductId == HomeStoreService.BathroomToiletId;
    public int PaperContactCount { get; private set; }
    public float LastPaperContactDistance { get; private set; } = float.PositiveInfinity;
    public Vector3 LastPaperContactPosition { get; private set; }
    public Vector3 PaperContactPosition
    {
        get
        {
            if (paperContactPoint != null) return paperContactPoint.position;
            if (rollPivot == null) return transform.position;
            // Actual source cylinder: R=.115, axis Y=.760. Both go through the
            // same model fit and .7 bathroom scale, so this fallback scales once.
            float radius = Mathf.Abs(transform.InverseTransformPoint(rollPivot.position).y) * (.115f / .760f);
            return rollPivot.position + transform.TransformVector(new Vector3(0f, -1f, 1f).normalized * radius);
        }
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (rollPivot == null || swatPoint == null)
        {
            failureReason = "NO PAPER TO PLAY WITH";
            return false;
        }

        if (UsesPaperTears && Cat != null && !PlanPaperApproach(Cat.transform.position))
        {
            failureReason = "LET'S GET A LITTLE CLOSER!";
            return false;
        }
        if (UsesPaperTears && !PlanPaperExit())
        { failureReason = "LET'S MAKE SOME ROOM."; return false; }

        if (Kind == CatActivityKind.RecordSpin)
        {
            RecordStandBlocked = !FindRecordStand(Flatten(RoutineFloorPosition, Cat.transform.position.y), out Vector3 stand);
            RecordStand = stand;
            if (RecordStandBlocked) { failureReason = "LET'S MAKE SOME ROOM."; return false; }
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        pivotRest = rollPivot.localRotation;
        if (Kind == CatActivityKind.RecordSpin)
        {
            RecordStandBlocked = !FindRecordStand(Cat.transform.position, out Vector3 stand);
            RecordStand = stand;
            if (RecordStandBlocked) return false;
        }
        if (UsesPaperTears)
        {
            if (!PlanPaperApproach(Cat.transform.position)) return false;
            paperPaw = Cat.GetComponent<CatPaperRollPawMotion>() ?? Cat.gameObject.AddComponent<CatPaperRollPawMotion>();
            paperFx = GetComponent<CatPaperTearFx>() ?? gameObject.AddComponent<CatPaperTearFx>();
            PaperContactCount = lastPaperStroke = 0; paperSpeed = 0; LastPaperContactDistance = float.PositiveInfinity;
            paperFx.Begin(this);
            StartCoroutine(PaperRoutine());
        }
        else if (Kind == CatActivityKind.RecordSpin)
        {
            RecordContactCount = 0; RecordContactDistance = float.PositiveInfinity;
            StartCoroutine(RecordRoutine());
        }
        else StartCoroutine(SpinRoutine());
        return true;
    }

    private IEnumerator RecordRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Vector3 stand = Flatten(RecordStand, Cat.transform.position.y);
        Quaternion facing = RecordFacing(stand);
        foreach (var point in recordApproach)
        {
            var destination = Flatten(point, stand.y);
            yield return CatActivityMotion.WalkAuthoredStep(Cat, destination,
                LookTowards(destination - Cat.transform.position, Cat.transform.rotation), .20f);
        }
        yield return CatActivityFacing.Turn(Cat, facing, .20f);
        recordPaw = Cat.GetComponent<CatToyContactMotion>() ?? Cat.gameObject.AddComponent<CatToyContactMotion>();
        var animation = Cat.GetComponent<CatActivityAnimation>();
        Bounds disc = rollPivot.GetComponentInChildren<Renderer>().bounds;
        Vector3 near = stand-disc.center;near.y=0;
        Vector3 contact = disc.center + near.normalized * (Mathf.Min(disc.extents.x, disc.extents.z)+.012f);
        contact.y = disc.max.y + .003f;
        bool left=Vector3.Dot(contact-stand,facing*Vector3.right)<0;
        float speed = 0f;
        for (int beat = 0; beat < Swats; beat++)
        {
            IsRecordTapping = true; bool touched = false; float elapsed = 0f;
            while (elapsed < .80f)
            {
                float phase = elapsed / .80f;
                Cat.transform.SetPositionAndRotation(stand, facing);
                // A side reach keeps the head outside the cabinet. Attack's
                // 30 cm body lunge formerly carried the chest through it.
                animation.SetTimedPose(left ? CatActivityPose.BatLeft : CatActivityPose.BatRight, phase);
                recordPaw.Reach(contact, left, phase);
                if (phase > .28f && phase < .70f)
                {
                    RecordContactDistance = Mathf.Min(RecordContactDistance, recordPaw.Distance);
                    if (!touched && recordPaw.Distance < .025f)
                    { touched = true; RecordContactCount++; speed += SpinPerSwat; }
                }
                speed = SpinDown(speed, Time.deltaTime);
                yield return null; elapsed += Time.deltaTime;
            }
            IsRecordTapping = false; recordPaw.Clear();
            if (!touched) { CancelForTransition(); yield break; }
        }
        PlayCatPose(CatActivityPose.GentleKnead);
        while (speed > 12f)
        {
            Cat.transform.SetPositionAndRotation(stand, facing);
            speed = SpinDown(speed, Time.deltaTime); yield return null;
        }
        foreach (var destination in recordExit)
            yield return CatActivityMotion.WalkAuthoredStep(Cat, destination,
                LookTowards(destination-Cat.transform.position, Cat.transform.rotation), .20f);
        RestoreCat(); CompleteActivity("Plak dönüyor!");
    }

    private bool FindRecordStand(Vector3 origin, out Vector3 stand)
    {
        stand=origin;recordExit=null;recordApproach=null;
        float best=float.PositiveInfinity;
        for(int attempt=0;attempt<3;attempt++)
        {
        for(int angle=0;angle<360;angle+=15)
        {
            Vector3 candidate=rollPivot.position+Quaternion.Euler(0,angle,0)*Vector3.forward*(.58f+attempt*.025f);candidate.y=origin.y;
            // Finish the approach along the working heading. Turning only
            // after reaching the near stand swept the nose through the desk.
            Vector3 staging=candidate-RecordFacing(candidate)*Vector3.forward*.18f;
            if(!CatActivityMotion.IsFloorClear(candidate)||
                !CatActivityMotion.ClearSegment(staging,candidate)||
                !CatActivityMotion.TryFloorPath(origin,staging,out var path)||!PlanExit(candidate,out var exit))continue;
            path.Add(candidate);
            // A quarter-turn side reach is anatomically reachable; a paw
            // cannot wrap behind the body to satisfy a camera preference.
            var towards=LookTowards(rollPivot.position-candidate,Quaternion.identity);
            if(Quaternion.Angle(towards,RecordFacing(candidate))>42f)continue;
            float length=0;Vector3 previous=origin;
            foreach(var p in path){length+=Vector3.Distance(previous,p);previous=p;}
            if(length>2.2f||length>=best)continue;
            best=length;stand=candidate;recordApproach=path;recordExit=exit;
        }
        if(recordApproach!=null)return true;
        }
        return recordApproach!=null;
    }

    private Quaternion RecordFacing(Vector3 stand)
    {
        var towards=LookTowards(rollPivot.position-stand,Cat.transform.rotation);
        var view=LookTowards(CatActivityFacing.CameraPosition(Cat)-stand,towards);
        return Quaternion.RotateTowards(towards,view,Mathf.Max(0,Quaternion.Angle(towards,view)-80f));
    }

    private bool PlanPaperExit()
        => PlanExit(Flatten(swatPoint.position, Cat.transform.position.y), out paperExit);

    private Quaternion PaperFacing()
        => CatActivityFacing.Resolve(Cat,Flatten(swatPoint.position,Cat.transform.position.y),
            LookTowards(PaperContactPosition-swatPoint.position,Cat.transform.rotation));

    private bool PlanPaperApproach(Vector3 origin)
    {
        Vector3 stand=Flatten(swatPoint.position,origin.y);
        Vector3 staging=stand-PaperFacing()*Vector3.forward*.18f;
        paperApproach=null;
        if(!CatActivityMotion.IsFloorClear(stand)||!CatActivityMotion.ClearSegment(staging,stand)||
            !CatActivityMotion.TryFloorPath(origin,staging,out paperApproach))return false;
        paperApproach.Add(stand);return true;
    }

    private bool PlanExit(Vector3 stand, out List<Vector3> exit)
    {
        exit = null;
        if (CatActivityMotion.IsControllerFloorClear(Cat, stand))
        { exit = new List<Vector3>(); return true; }
        var candidates = new List<Vector3>();
        for (int x = -6; x <= 6; x++) for (int z = -6; z <= 6; z++)
            candidates.Add(stand + new Vector3(x * .1f, 0, z * .1f));
        candidates.Sort((a,b) => (a-stand).sqrMagnitude.CompareTo((b-stand).sqrMagnitude));
        foreach (var candidate in candidates)
        {
            if (!CatActivityMotion.IsControllerFloorClear(Cat, candidate) ||
                !CatActivityMotion.TryFloorPath(stand, candidate, out var path)) continue;
            exit = path; return true;
        }
        return false;
    }

    private IEnumerator PaperRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Quaternion facing = PaperFacing();
        foreach (var point in paperApproach)
        {
            Vector3 destination = Flatten(point, Cat.transform.position.y);
            yield return Move(Cat.transform.position, destination, Cat.transform.rotation,
                LookTowards(destination - Cat.transform.position, Cat.transform.rotation),
                Mathf.Clamp(Vector3.Distance(Cat.transform.position, destination) / 1.2f, .2f, .8f));
        }
        PlayCatPose(CatActivityPose.GentleKnead);
        yield return CatActivityFacing.Turn(Cat, facing);
        // This wall-mounted roll is above a floor toy. The existing scratch
        // posture raises the real chest while retaining planted hind legs;
        // an idle-height Bat pose cannot reach it by rotating the elbow alone.
        PlayCatPose(CatActivityPose.Scratch);
        yield return new WaitForSeconds(.24f);
        var pose = Cat.GetComponent<CatActivityAnimation>();
        for (int stroke = 1; stroke <= Swats; stroke++)
        {
            bool left = stroke % 2 != 0;
            if (!paperPaw.BeginStroke(this, stroke, left)) break;
            float elapsed = 0f;
            const float seconds = .92f;
            while (elapsed < seconds)
            {
                // A paused frame retains the last sampled stroke and source
                // pose. The elapsed clock advances only after an active yield.
                if (Time.deltaTime <= 0f) { yield return null; continue; }
                float phase = Mathf.Clamp01(elapsed / seconds);
                pose.SetTimedPose(CatActivityPose.Scratch, phase);
                paperPaw.Sample(phase);
                paperSpeed = SpinDown(paperSpeed, Time.deltaTime);
                yield return null;
                elapsed += Time.deltaTime;
            }
            paperPaw.Clear();
        }
        if (PaperContactCount == 0)
        {
            CancelActivity();
            ShowSpeech(GameContentCopy.Text("Ruloya biraz daha yaklaşmalıyım.", "I need to get closer to the roll."));
            yield break;
        }
        PlayCatPose(CatActivityPose.SitDown);
        while (paperSpeed > 12f || paperFx.ActivePieces > 0 || Time.deltaTime <= 0f)
        {
            paperSpeed = SpinDown(paperSpeed, Time.deltaTime);
            yield return null;
        }
        paperPaw.Clear(); paperFx.Stop();
        rollPivot.localRotation = pivotRest;
        // The narrow paw stance can fit beside the roll while the complete
        // walking controller still overlaps the toilet. Walk out before release.
        foreach (var destination in paperExit)
        {
            Vector3 from = Cat.transform.position;
            yield return Move(from, destination, Cat.transform.rotation,
                LookTowards(destination - from, Cat.transform.rotation),
                Mathf.Max(.12f, Vector3.Distance(from, destination) / 1.2f));
        }
        RestoreCat();
        CompleteActivity(GameContentCopy.Text("Kâğıtlar uçuşuyor!", "Paper everywhere!"));
    }

    internal bool AcceptPaperContact(CatPaperRollPawMotion motion, int stroke, Vector3 paw, float distance)
    {
        if (!UsesPaperTears || !IsRunning || !HasBegunActivity || motion != paperPaw || Time.deltaTime <= 0f ||
            stroke <= lastPaperStroke || stroke > Swats || distance > CatPaperRollPawMotion.ContactTolerance ||
            Vector3.Distance(paw, PaperContactPosition) > CatPaperRollPawMotion.ContactTolerance) return false;
        lastPaperStroke = stroke; PaperContactCount++; LastPaperContactDistance = distance; LastPaperContactPosition = paw;
        paperSpeed += SpinPerSwat;
        paperFx.Tear(this, stroke, PaperContactPosition, Cat.transform.position - PaperContactPosition);
        return true;
    }

    private IEnumerator SpinRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 start = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 stand = Flatten(Kind == CatActivityKind.RecordSpin ? RecordStand : swatPoint.position, start.y);
        Quaternion facing = LookTowards(rollPivot.position - stand, startRotation);

        if (Kind == CatActivityKind.RecordSpin)
        {
            foreach (var point in recordApproach)
            {
                Vector3 destination = Flatten(point, start.y);
                Vector3 from = Cat.transform.position;
                yield return Move(from, destination, Cat.transform.rotation,
                    LookTowards(destination - from, Cat.transform.rotation),
                    Mathf.Max(.18f, Vector3.Distance(from, destination) / 1.1f));
            }
            PlayCatPose(CatActivityPose.GentleKnead);
            yield return CatActivityFacing.Turn(Cat, facing);
        }
        else yield return Move(start, stand, startRotation, facing, 0.34f);

        float spin = 0f;
        for (int i = 0; i < Swats; i++)
        {
            yield return Reach(stand, facing);
            spin += SpinPerSwat;
            // Let the roll run down a little between swats, so each hit reads as
            // a fresh push rather than one long spin.
            float settle = 0f;
            while (settle < 0.28f)
            {
                settle += Time.deltaTime;
                spin = SpinDown(spin, Time.deltaTime);
                yield return null;
            }
        }

        PlayCatPose(CatActivityPose.Sit);
        while (spin > 12f)
        {
            spin = SpinDown(spin, Time.deltaTime);
            yield return null;
        }

        RestoreCat();
        CompleteActivity(Kind == CatActivityKind.RecordSpin ? "Plak dönüyor!" : "PAPER EVERYWHERE!");
    }

    private float SpinDown(float speed, float deltaTime)
    {
        // A horizontal record spins on its vertical spindle. Reusing the paper
        // roll's X axis flipped the entire disc through the console tabletop.
        rollPivot.localRotation *= Quaternion.AngleAxis(speed * deltaTime, SpinAxis);
        return Mathf.Max(0f, speed - speed * SpinDamping * deltaTime);
    }

    /// <summary>One paw swipe: rear up towards the roll and drop back.</summary>
    private IEnumerator Reach(Vector3 stand, Quaternion facing)
    {
        const float duration = 0.34f;
        float elapsed = 0f;
        PlayCatPose(CatActivityPose.Paw);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float lift = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

            Vector3 position = stand;
            position.y += lift * 0.075f;
            Cat.transform.position = position;
            Cat.transform.rotation = facing * Quaternion.Euler(-lift * 18f, 0f, 0f);

            Vector3 stretched = originalScale;
            stretched.y *= 1f + lift * 0.10f;
            stretched.x *= 1f - lift * 0.04f;
            stretched.z *= 1f - lift * 0.04f;
            Cat.transform.localScale = stretched;
            yield return null;
        }

        Cat.transform.position = stand;
        Cat.transform.rotation = facing;
        Cat.transform.localScale = originalScale;
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        if (UsesPaperTears)
        {
            yield return CatActivityMotion.WalkAuthoredStep(Cat, to, toRotation, duration);
            yield break;
        }
        Quaternion arrival = toRotation;
        bool contactRoute = UsesPaperTears || Kind == CatActivityKind.RecordSpin;
        if (contactRoute)
        {
            Quaternion travel = LookTowards(to - from, Cat.transform.rotation);
            PlayCatPose(CatActivityPose.GentleKnead);
            yield return CatActivityFacing.Turn(Cat, travel, .16f);
            fromRotation = toRotation = travel;
        }
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
        if (contactRoute && Quaternion.Angle(toRotation, arrival) > .1f)
        {
            PlayCatPose(CatActivityPose.GentleKnead);
            yield return CatActivityFacing.Turn(Cat, arrival, .16f);
        }
    }

    private void RestoreCat()
    {
        IsRecordTapping = false; recordPaw?.Clear();
        if (Cat == null)
            return;

        if (originalScale.sqrMagnitude > 0.0001f)
            Cat.transform.localScale = originalScale;
        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    private void RestorePaperFloor()
    {
        if (!UsesPaperTears || Cat == null || paperExit == null || paperExit.Count == 0 ||
            CatActivityMotion.IsControllerFloorClear(Cat, Cat.transform.position)) return;
        Vector3 point = paperExit[paperExit.Count - 1];
        if (!CatActivityMotion.IsControllerFloorClear(Cat, point)) return;
        var controller = Cat.GetComponent<CharacterController>();
        bool enabled = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        Cat.transform.position = point;
        if (controller != null) controller.enabled = enabled;
        Physics.SyncTransforms();
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
        if (!HasBegunActivity) { base.CancelActivity(); RestorePaperFloor(); return; }
        if (rollPivot != null)
            rollPivot.localRotation = pivotRest;
        if (UsesPaperTears) { paperPaw?.Clear(); paperFx?.Stop(); paperSpeed = 0f; }
        RestoreCat();
        base.CancelActivity();
        RestorePaperFloor();
    }

#if UNITY_EDITOR
    public void EditorConfigurePaperContact(Transform contact) => paperContactPoint = contact;
    public void EditorConfigureSpin(
        Transform pivot, Transform swat, int swatCount, float perSwat, float damping)
    {
        rollPivot = pivot;
        swatPoint = swat;
        swats = Mathf.Max(1, swatCount);
        spinPerSwat = Mathf.Max(60f, perSwat);
        spinDamping = Mathf.Max(0.2f, damping);
    }
#endif
}
