using System.Collections;
using UnityEngine;

/// <summary>
/// Pat the glass on the nightstand until it goes over the edge.
///
/// The most cat thing in the whole house, and the only routine where the product
/// loses. The glass is its own FBX on the nightstand's origin — like the toilet
/// paper roll — so it can hang under a pivot the routine drives: two exploratory
/// taps that only rock it, then a third that pushes it off, a fall, a bounce and
/// a settle on its side.
///
/// The glass is put back exactly where it started when the routine ends. A prop
/// that stayed on the floor would be somewhere the catalog never placed it, and
/// the next cat to walk past would knock a glass that was already down.
///
/// The cat stands on a clear floor or measured perch and reaches with its paw.
/// </summary>
[DisallowMultipleComponent]
public sealed class KnockOffActivity : CatActivity
{
    [Header("Knock off")]
    [SerializeField] private Transform reachPoint;
    [SerializeField] private Transform perchPoint;
    [SerializeField] private Transform glassPivot;
    [SerializeField] private Vector3 fallDirection = Vector3.forward;
    [SerializeField, Min(0.05f)] private float fallHeight = 0.55f;
    [SerializeField, Min(1)] private int teaseCount = 2;

    private CharacterController characterController;
    public Vector3 ContactStand { get; private set; }
    public bool ContactStandBlocked { get; private set; }
    private Vector3 pivotHome;
    private Quaternion pivotHomeRotation;
    private bool pivotHomeCaptured;
    private CatSupportedFurnitureMotion supportedMotion;
    private CatToyContactMotion pawContact;
    private CatPawReachMotion groundReachMotion;
    private CatMeshPawTargetSearch groundTargets;
    public bool IsStartSearchPending {get;private set;}
    private Vector3 measuredFallDirection;
    public Vector3 LastStrikePosition { get; private set; }
    public int ContactStrokes { get; private set; }
    public bool IsOnNightstand { get; private set; }
    public bool IsTapping { get; private set; }
    public float MinimumPawDistance { get; private set; }
    public Transform PerchPoint => perchPoint;

    public override string ProgressLabel => IsRunning ? "PATTING..." : string.Empty;

    public float FallHeight => Mathf.Max(0.05f, fallHeight);
    public int TeaseCount => Mathf.Max(1, teaseCount);
    public Transform GlassPivot => glassPivot;
    public Vector3 WorldFallDirection => measuredFallDirection.sqrMagnitude > .001f ? measuredFallDirection : transform.TransformDirection(fallDirection.sqrMagnitude > .0001f ? fallDirection.normalized : Vector3.forward).normalized;

    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default; IsStartSearchPending=false;
        if (actor == null || reachPoint == null || glassPivot == null) return false;
        if (perchPoint != null)
            return CatActivityStartResolver.GroundLaunch(this, actor, RoutineFloorPosition, perchPoint.position, out start);
        Vector3 centre = reachPoint.position; centre.y = actor.transform.position.y;
        bool ready = CatActivityStartResolver.Facing(actor, centre, .42f, glassPivot.position, 45f, out start);
        if (!ready) return false;
        if(groundTargets==null||groundTargets.Root!=glassPivot)groundTargets=new CatMeshPawTargetSearch(glassPivot);
        start.HasPawPlan=groundTargets.TryResolve(actor,CatActivityPose.Paw,out start.PawPlan,
            -CatPawReachResolver.MaximumChestPitch,0,CatPawReachResolver.MaximumChestYaw);
        IsStartSearchPending=groundTargets.IsPending;
        if(start.HasPawPlan)start.ActionTarget=start.PawPlan.Surface.Point;
        return start.HasPawPlan;
    }

    private bool TryGroundedContact(Vector3 stand, Quaternion facing, out Vector3 target, out bool left)
    {
        target = Vector3.zero; left = false;
        var renderer = glassPivot.GetComponentInChildren<Renderer>();
        if (renderer == null) return false;
        var bounds = renderer.bounds;
        bool planted = StoreProductId == HomeStoreService.BalconySideTableId;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        Vector3 toward = bounds.center - stand; toward.y = 0;
        target = bounds.center - (planted ? toward.normalized : facing * Vector3.forward) *
            (planted ? radius + .012f : radius * .95f);
        target.y = bounds.min.y + Mathf.Min(planted ? .025f : .055f, bounds.size.y * .5f);
        left = Vector3.Dot(target - stand, facing * Vector3.right) < 0;
        return true;
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        failureReason = reachPoint == null || glassPivot == null || perchPoint == null && !AcceptedStart.HasPawPlan ?
            "NOTHING TO PUSH" : string.Empty;
        return failureReason.Length == 0;
    }

    protected override bool BeginActivity()
    {
        measuredFallDirection = Vector3.zero; LastStrikePosition = Vector3.zero;
        ContactStand = AcceptedStart.Position; ContactStandBlocked = false;
        if (perchPoint != null)
        {
            characterController = Cat.GetComponent<CharacterController>();
            pivotHome = glassPivot.localPosition; pivotHomeRotation = glassPivot.localRotation; pivotHomeCaptured = true;
            ContactStrokes = 0; MinimumPawDistance = float.PositiveInfinity; StartCoroutine(SupportedKnockRoutine()); return true;
        }
        characterController = Cat.GetComponent<CharacterController>();
        if (!pivotHomeCaptured)
        {
            pivotHome = glassPivot.localPosition;
            pivotHomeRotation = glassPivot.localRotation;
            pivotHomeCaptured = true;
        }
        ContactStrokes=0;MinimumPawDistance=float.PositiveInfinity;
        StartCoroutine(GroundedKnockRoutine());
        return true;
    }

    private IEnumerator GroundedKnockRoutine()
    {
        Cat.SetMovementLocked(this,true);if(characterController!=null)characterController.enabled=false;
        Vector3 stand = AcceptedStart.Position;
        Quaternion facing = AcceptedStart.Rotation;
        pawContact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        groundReachMotion=Cat.GetComponent<CatPawReachMotion>()??Cat.gameObject.AddComponent<CatPawReachMotion>();
        var surface=AcceptedStart.PawPlan.Surface;
        var animator=Cat.GetComponentInChildren<Animator>();
        var binding=new CatPawSurfaceBinding(surface,animator!=null?animator.transform:null);
        if(!binding.IsValid){CancelForTransition();yield break;}
        Vector3 direction=WorldFallDirection;
        Vector3 push=glassPivot.parent.InverseTransformDirection(direction);
        Vector3 axis=Vector3.Cross(Vector3.up,push);
        for(int beat=0;beat<=TeaseCount;beat++)
        {
            bool touched=false;float elapsed=0;IsTapping=true;
            while(elapsed<.80f)
            {
                if(Time.timeScale<=0f){yield return null;continue;}
                float t=elapsed/.80f;Cat.transform.SetPositionAndRotation(stand,facing);
                groundReachMotion.Sample(this,AcceptedStart.PawPlan,t);
                yield return new WaitForEndOfFrame();
                if(!IsRunning)yield break;
                if(Time.timeScale>0f&&t>.28f&&t<.7f)
                {
                    Vector3 actual=binding.Point();
                    float distance=Vector3.Distance(actual,surface.Point);
                    MinimumPawDistance=Mathf.Min(MinimumPawDistance,distance);
                    if(!touched && distance<.025f){CaptureStrike(actual);direction=WorldFallDirection;push=glassPivot.parent.InverseTransformDirection(direction);axis=Vector3.Cross(Vector3.up,push);touched=true;}
                }
                if(touched)glassPivot.localRotation=pivotHomeRotation*Quaternion.AngleAxis(Mathf.Sin(t*Mathf.PI*3)*7*(1-t),axis);
                yield return null;elapsed+=Time.deltaTime;
            }
            IsTapping=false;groundReachMotion.Clear();pawContact.Clear();glassPivot.localRotation=pivotHomeRotation;
            if(!touched){CancelForTransition();yield break;}
            ContactStrokes++;yield return Wait(.15f);
        }
        yield return FallFromTable(stand, facing, null);
        RestoreCat();CompleteActivity("OOPS.");
    }

    private IEnumerator SupportedKnockRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Vector3 floor = RoutineFloorPosition; floor.y = AcceptedStart.Position.y;
        Vector3 perch = perchPoint.position;
        Quaternion facing = LookTowards(glassPivot.position - perch, Cat.transform.rotation);
        Quaternion launch = AcceptedStart.Rotation;
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, perchPoint);
        yield return supportedMotion.Jump(AcceptedStart.Position, perch, launch, facing);
        IsOnNightstand = true;
        pawContact = Cat.GetComponent<CatToyContactMotion>() ?? Cat.gameObject.AddComponent<CatToyContactMotion>();
        Vector3 direction = WorldFallDirection;
        Vector3 localPush = glassPivot.parent.InverseTransformDirection(direction);
        Vector3 axis = Vector3.Cross(Vector3.up, localPush);
        var animation = Cat.GetComponent<CatActivityAnimation>();
        var hand=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-hand.L");
        if(hand==null){CancelForTransition();yield break;}
        for (int beat = 0; beat <= TeaseCount; beat++)
        {
            IsTapping = true;
            float elapsed = 0f; bool touched = false;
            while (elapsed < .70f)
            {
                if(Time.timeScale<=0f){yield return null;continue;}
                float phase = Mathf.Clamp01(elapsed / .70f);
                Cat.transform.SetPositionAndRotation(perch, facing);
                animation.SetPose(CatActivityPose.GentleKnead, perchPoint);
                Bounds glassBounds = glassPivot.GetComponentInChildren<Renderer>().bounds;
                float radius = Mathf.Min(glassBounds.extents.x, glassBounds.extents.z);
                Vector3 target = glassBounds.center - facing * Vector3.forward * (radius * .95f);
                target.y = Mathf.Clamp(perch.y + .06f, glassBounds.min.y + .015f, glassBounds.max.y - .015f);
                pawContact.Reach(target, true, phase);
                yield return new WaitForEndOfFrame();
                if(!IsRunning)yield break;
                float distance=Vector3.Distance(hand.position,target);
                if (Time.timeScale>0f && phase > .28f && phase < .7f) MinimumPawDistance = Mathf.Min(MinimumPawDistance, distance);
                if (Time.timeScale>0f && !touched && phase > .28f && phase < .7f && distance < .025f)
                {CaptureStrike(hand.position);direction=WorldFallDirection;localPush=glassPivot.parent.InverseTransformDirection(direction);axis=Vector3.Cross(Vector3.up,localPush);touched=true;}
                if (touched) glassPivot.localRotation = pivotHomeRotation * Quaternion.AngleAxis(Mathf.Sin(phase * Mathf.PI * 3f) * 7f * (1f-phase), axis);
                yield return null; elapsed += Time.deltaTime;
            }
            IsTapping = false; pawContact.Clear(); glassPivot.localRotation = pivotHomeRotation;
            if (!touched) { CancelForTransition(); yield break; }
            ContactStrokes++;
            yield return supportedMotion.Pose(CatActivityPose.GentleKnead, .18f, perch, facing);
        }
        yield return FallFromTable(perch, facing, perchPoint);
        // The back of this small top holds books. Sitting shifted the hips
        // into them; stay on the four clear paw spots before jumping down.
        yield return supportedMotion.Pose(CatActivityPose.GentleKnead, .25f, perch, facing);
        yield return supportedMotion.Jump(perch, floor, facing, LookTowards(floor-perch, facing));
        IsOnNightstand = false; RestoreCat(); CompleteActivity("OOPS.");
    }

    private void CaptureStrike(Vector3 actualPaw)
    {
        GameAudio.Play(CatFoley.ContactCue(this), .8f);
        LastStrikePosition = actualPaw;
        Vector3 away = glassPivot.GetComponentInChildren<Renderer>().bounds.center - LastStrikePosition;
        away.y = 0;
        if (away.sqrMagnitude > .000001f) measuredFallDirection = away.normalized;
    }

    private Bounds TableBounds()
    {
        Bounds bounds = new Bounds(transform.position, Vector3.zero); bool found = false;
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.transform.IsChildOf(glassPivot)) continue;
            if (!found) { bounds=renderer.bounds; found=true; } else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    private IEnumerator FallFromTable(Vector3 stand, Quaternion facing, Transform support)
    {
        Vector3 direction = WorldFallDirection, start = glassPivot.position;
        Quaternion homeRotation = glassPivot.rotation;
        Bounds table = TableBounds(), prop = glassPivot.GetComponentInChildren<Renderer>().bounds;
        // First ray exit, in metres. A projected diagonal extent overshoots
        // the edge; inverse-transforming a direction also loses parent scale.
        float x = Mathf.Abs(direction.x) < .00001f ? float.PositiveInfinity :
            ((direction.x > 0 ? table.max.x : table.min.x) - prop.center.x) / direction.x;
        float z = Mathf.Abs(direction.z) < .00001f ? float.PositiveInfinity :
            ((direction.z > 0 ? table.max.z : table.min.z) - prop.center.z) / direction.z;
        float edge = Mathf.Max(0, Mathf.Min(x,z));
        float radius = Mathf.Abs(direction.x)*prop.extents.x + Mathf.Abs(direction.z)*prop.extents.z;
        Vector3 lip = start + direction * (edge + radius + .025f);
        Quaternion fallen = Quaternion.AngleAxis(90,Vector3.Cross(Vector3.up,direction))*homeRotation;
        glassPivot.rotation=fallen;float bottom=float.PositiveInfinity;
        foreach(var renderer in glassPivot.GetComponentsInChildren<Renderer>())bottom=Mathf.Min(bottom,renderer.bounds.min.y);
        float clearance=glassPivot.position.y-bottom;glassPivot.rotation=homeRotation;
        Vector3 end=lip+direction*.12f;end.y=clearance+.008f;
        float fallSeconds=Mathf.Sqrt(2*Mathf.Max(.01f,lip.y-end.y)/9.81f);
        float elapsed=0,slideSeconds=.38f;
        while(elapsed<slideSeconds+fallSeconds)
        {
            Cat.transform.SetPositionAndRotation(stand,facing);
            PlayCatPose(CatActivityPose.GentleKnead,support);
            if(elapsed<slideSeconds)glassPivot.position=Vector3.Lerp(start,lip,elapsed/slideSeconds);
            else
            {
                float t=Mathf.Clamp01((elapsed-slideSeconds)/fallSeconds);
                Vector3 at=Vector3.Lerp(lip,end,t);at.y=Mathf.Lerp(lip.y,end.y,t*t);
                glassPivot.SetPositionAndRotation(at,Quaternion.Slerp(homeRotation,fallen,t));
            }
            yield return null;elapsed+=Time.deltaTime;
        }
        glassPivot.SetPositionAndRotation(end,fallen);
        GameAudio.Play(Kind==CatActivityKind.BookKnockOff?AudioCue.BookLand:
            Kind==CatActivityKind.TableKnockOff?AudioCue.CeramicLand:AudioCue.GlassLand, .85f);
        yield return Wait(.45f);
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

    private void RestoreCat()
    {
        groundReachMotion?.Clear(); pawContact?.Clear(); supportedMotion?.End(); supportedMotion = null;
        // Base prepared recovery selects a currently clear floor pose.
        IsOnNightstand = false; IsTapping = false;
        // The glass goes back on the nightstand. It has to: the catalog placed
        // it there, and a glass left on the floor would be knocked twice.
        if (glassPivot != null && pivotHomeCaptured)
        {
            glassPivot.localPosition = pivotHome;
            glassPivot.localRotation = pivotHomeRotation;
        }

        if (Cat == null)
            return;

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

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigurePerch(Transform point) => perchPoint = point;
    public void EditorConfigureKnock(
        Transform reach, Transform pivot, Vector3 direction, float drop, int teases)
    {
        reachPoint = reach;
        glassPivot = pivot;
        fallDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized : Vector3.forward;
        fallHeight = Mathf.Max(0.05f, drop);
        teaseCount = Mathf.Max(1, teases);
    }
#endif
}
