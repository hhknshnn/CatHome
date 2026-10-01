using System.Collections;
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
    public Vector3 RecordStand { get; private set; }
    public bool RecordStandBlocked { get; private set; }
    private float paperSpeed;
    private int lastPaperStroke;
    private CatToyContactMotion recordPaw;
    private CatPawReachMotion pawReach;
    private RecordPlayerMusic recordMusic;
    public RecordPlayerMusic RecordMusic => recordMusic;
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

    protected override void OnEnable()
    {
        base.OnEnable();
        if (Kind != CatActivityKind.RecordSpin) return;
        recordMusic = GetComponent<RecordPlayerMusic>() ?? gameObject.AddComponent<RecordPlayerMusic>();
        recordMusic.Prepare(rollPivot);
    }

    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        if (actor == null || rollPivot == null || swatPoint == null) return false;
        Vector3 target = PaperContactPosition;
        Vector3 centre = swatPoint.position;
        float radius = .12f;
        if (Kind == CatActivityKind.RecordSpin)
        {
            var renderer = rollPivot.GetComponentInChildren<Renderer>();
            if (renderer == null) return false;
            Bounds disc = renderer.bounds;
            Vector3 near = actor.transform.position - disc.center; near.y = 0;
            if (near.sqrMagnitude < .001f) return false;
            target = disc.center + near.normalized * (Mathf.Min(disc.extents.x, disc.extents.z) + .012f);
            target.y = disc.max.y + .003f;
            centre = disc.center; radius = .66f;
            
        }
        centre.y = actor.transform.position.y;
        if (!CatActivityStartResolver.Facing(actor, centre, radius, target, 80f, out start)) return false;
        bool left=Vector3.Dot(target-actor.transform.position,actor.transform.right)<0;
        CatPawReachPlan plan;
        if (Kind==CatActivityKind.RecordSpin)
        {
            if (!TryRecordReach(actor,target,left,out plan)) return false;
        }
        else if (UsesPaperTears)
        {
            if (!CatPawReachResolver.TryResolve(actor,target,true,CatActivityPose.Scratch,out plan) ||
                !CatPawReachResolver.TryResolve(actor,target,false,CatActivityPose.Scratch,out _)) return false;
        }
        else return true;
        start.PawPlan=plan; start.HasPawPlan=true; return true;
    }

    private static bool TryRecordReach(CatMovement actor, Vector3 target, bool left, out CatPawReachPlan plan)
    {
        return CatPawReachResolver.TryResolve(actor,target,left,left?CatActivityPose.BatLeft:CatActivityPose.BatRight,out plan,32f,35f) ||
            CatPawReachResolver.TryResolve(actor,target,left,CatActivityPose.Scratch,out plan,32f,35f);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        failureReason = rollPivot == null || swatPoint == null ? "NO PAPER TO PLAY WITH" : string.Empty;
        if (Kind == CatActivityKind.RecordSpin &&
            (recordMusic == null || recordMusic.Source == null || recordMusic.Source.clip == null))
            failureReason = GameLanguageService.Text("record.not_ready");
        return failureReason.Length == 0;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        pivotRest = rollPivot.localRotation;
        pawReach=Cat.GetComponent<CatPawReachMotion>()??Cat.gameObject.AddComponent<CatPawReachMotion>();
        if (Kind == CatActivityKind.RecordSpin)
        {
            RecordStand = AcceptedStart.Position;
            RecordStandBlocked = false;
        }
        if (UsesPaperTears)
        {
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
        Vector3 stand = AcceptedStart.Position;
        Quaternion facing = AcceptedStart.Rotation;
        recordPaw = Cat.GetComponent<CatToyContactMotion>() ?? Cat.gameObject.AddComponent<CatToyContactMotion>();
        var hand = CatBreedVisualFactory.FindDescendant(Cat.transform,
            AcceptedStart.PawPlan.Left ? "DEF-hand.L" : "DEF-hand.R");
        if (hand == null) { CancelForTransition(); yield break; }
        IsRecordTapping = true;
        bool touched = false;
        float elapsed = 0f;
        while (elapsed < .85f)
        {
            if (Time.timeScale <= 0f || Time.deltaTime <= 0f) { yield return null; continue; }
            float phase = elapsed / .85f;
            Cat.transform.SetPositionAndRotation(stand, facing);
            pawReach.Sample(this, AcceptedStart.PawPlan, phase);
            // The shared pose and IK finish in LateUpdate. Observe this frame's
            // solved contact, and recheck a pause applied earlier this frame.
            yield return new WaitForEndOfFrame();
            if (!IsRunning) yield break;
            if (Time.timeScale > 0f && Time.deltaTime > 0f && phase > .28f && phase < .70f)
            {
                float actualDistance = Vector3.Distance(hand.position, AcceptedStart.PawPlan.Target);
                RecordContactDistance = Mathf.Min(RecordContactDistance, actualDistance);
                if (!touched && actualDistance < .025f)
                {
                    touched = true;
                    RecordContactCount++;
                    if (recordMusic == null || !recordMusic.ToggleFromContact(this, Cat))
                    { CancelForTransition(); yield break; }
                }
            }
            yield return null;
            if (Time.timeScale > 0f) elapsed += Time.deltaTime;
        }
        IsRecordTapping = false;
        pawReach.Clear(); recordPaw.Clear();
        if (!touched) { CancelForTransition(); yield break; }
        RestoreCat();
        CompleteActivity(GameLanguageService.Text(recordMusic.IsOn ? "record.on" : "record.off"));
    }

    private IEnumerator PaperRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        // This wall-mounted roll is above a floor toy. The existing scratch
        // posture raises the real chest while retaining planted hind legs;
        // an idle-height Bat pose cannot reach it by rotating the elbow alone.
        PlayCatPose(CatActivityPose.Scratch);
        yield return new WaitForSeconds(.24f);
        var pose = Cat.GetComponent<CatActivityAnimation>();
        for (int stroke = 1; stroke <= Swats; stroke++)
        {
            bool left = stroke % 2 != 0;
            if (!CatPawReachResolver.TryResolve(Cat,PaperContactPosition,left,CatActivityPose.Scratch,out var plan) ||
                !paperPaw.BeginStroke(this, stroke, left)) break;
            float elapsed = 0f;
            const float seconds = .92f;
            while (elapsed < seconds)
            {
                // A paused frame retains the last sampled stroke and source
                // pose. The elapsed clock advances only after an active yield.
                if (Time.deltaTime <= 0f) { yield return null; continue; }
                float phase = Mathf.Clamp01(elapsed / seconds);
                pawReach.Sample(this,plan,phase,false);
                paperPaw.Sample(phase);
                paperSpeed = SpinDown(paperSpeed, Time.deltaTime);
                yield return null;
                elapsed += Time.deltaTime;
            }
            pawReach.Clear(); paperPaw.Clear();
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

        Vector3 stand = AcceptedStart.Position;
        Quaternion facing = AcceptedStart.Rotation;

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

    private void RestoreCat()
    {
        IsRecordTapping = false; pawReach?.Clear(); recordPaw?.Clear();
        if (Cat == null)
            return;

        if (originalScale.sqrMagnitude > 0.0001f)
            Cat.transform.localScale = originalScale;
        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        if (rollPivot != null && Kind != CatActivityKind.RecordSpin)
            rollPivot.localRotation = pivotRest;
        if (UsesPaperTears) { paperPaw?.Clear(); paperFx?.Stop(); paperSpeed = 0f; }
        RestoreCat();
        base.CancelActivity();
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
