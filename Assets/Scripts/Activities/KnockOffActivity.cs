using System.Collections;
using System.Collections.Generic;
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
    private List<Vector3> contactPath;
    public Vector3 ContactStand { get; private set; }
    public bool ContactStandBlocked { get; private set; }
    private Vector3 pivotHome;
    private Quaternion pivotHomeRotation;
    private bool pivotHomeCaptured;
    private CatSupportedFurnitureMotion supportedMotion;
    private CatToyContactMotion pawContact;
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

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (reachPoint == null || glassPivot == null)
        {
            failureReason = "NOTHING TO PUSH";
            return false;
        }

        Vector3 authored = reachPoint.position; authored.y = Cat.transform.position.y;
        if (perchPoint != null)
        {
            ContactStand = RoutineFloorPosition;
            ContactStandBlocked = !CatActivityMotion.IsFloorClear(ContactStand);
            failureReason = ContactStandBlocked ? "LET'S MAKE SOME ROOM." : string.Empty;
            return !ContactStandBlocked;
        }
        Vector3 origin = RoutineFloorPosition; origin.y = authored.y;
        ContactStandBlocked = !TryGroundedPawStand(origin, out Vector3 stand, out contactPath);
        ContactStand = stand;
        failureReason = ContactStandBlocked ? "LET'S MAKE SOME ROOM." : string.Empty;
        return !ContactStandBlocked;
    }

    protected override bool BeginActivity()
    {
        measuredFallDirection = Vector3.zero; LastStrikePosition = Vector3.zero;
        if (perchPoint != null)
        {
            characterController = Cat.GetComponent<CharacterController>();
            pivotHome = glassPivot.localPosition; pivotHomeRotation = glassPivot.localRotation; pivotHomeCaptured = true;
            ContactStrokes = 0; MinimumPawDistance = float.PositiveInfinity; StartCoroutine(SupportedKnockRoutine()); return true;
        }
        ContactStandBlocked = !TryGroundedPawStand(Cat.transform.position, out Vector3 stand, out contactPath);
        ContactStand = stand;
        if (ContactStandBlocked) return false;
        characterController = Cat.GetComponent<CharacterController>();
        if (!pivotHomeCaptured)
        {
            pivotHome = glassPivot.localPosition;
            pivotHomeRotation = glassPivot.localRotation;
            pivotHomeCaptured = true;
        }
        ContactStrokes=0;MinimumPawDistance=float.PositiveInfinity;
        StartCoroutine(StoreProductId==HomeStoreService.BalconySideTableId || StoreProductId==HomeStoreService.LoftBookStackId ? GroundedKnockRoutine():KnockRoutine());
        return true;
    }

    private IEnumerator GroundedKnockRoutine()
    {
        Cat.SetMovementLocked(this,true);if(characterController!=null)characterController.enabled=false;
        Vector3 approachOrigin = Cat.transform.position;
        Vector3 stand=Flatten(ContactStand,Cat.transform.position.y);
        foreach(var point in contactPath)
        {
            Vector3 to=Flatten(point,stand.y),from=Cat.transform.position;
            yield return Move(from,to,Cat.transform.rotation,LookTowards(to-from,Cat.transform.rotation),Mathf.Max(.18f,Vector3.Distance(from,to)/1.1f));
        }
        Quaternion facing=GroundedFacing(stand);
        yield return Move(stand,stand,Cat.transform.rotation,facing,.2f);
        pawContact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        var animation=Cat.GetComponent<CatActivityAnimation>();
        Vector3 direction=WorldFallDirection;
        Vector3 push=glassPivot.parent.InverseTransformDirection(direction);
        Vector3 axis=Vector3.Cross(Vector3.up,push);
        for(int beat=0;beat<=TeaseCount;beat++)
        {
            bool touched=false;float elapsed=0;IsTapping=true;
            while(elapsed<.80f)
            {
                float t=elapsed/.80f;Cat.transform.SetPositionAndRotation(stand,facing);
                // Attack translates the whole skeleton about 30 cm towards
                // the table. Keep the body planted; only the real hand reaches.
                bool planted = StoreProductId == HomeStoreService.BalconySideTableId;
                var bounds=glassPivot.GetComponentInChildren<Renderer>().bounds;
                float propRadius = Mathf.Min(bounds.extents.x,bounds.extents.z);
                Vector3 toward=bounds.center-stand;toward.y=0;
                Vector3 target=bounds.center-(planted ? toward.normalized : facing*Vector3.forward)*(planted ? propRadius + .012f : propRadius*.95f);
                target.y=bounds.min.y+Mathf.Min(planted ? .025f : .055f,bounds.size.y*.5f);
                bool left=!planted||Vector3.Dot(target-stand,facing*Vector3.right)<0;
                animation.SetTimedPose(planted ? (left?CatActivityPose.BatLeft:CatActivityPose.BatRight) : CatActivityPose.Paw,t);
                float reachPhase=planted?(t<.32f?.42f*t/.32f:t<.62f?.42f:Mathf.Lerp(.42f,1,(t-.62f)/.38f)):t;
                pawContact.Reach(target,left,reachPhase,planted?16:8);
                if(t>.28f&&t<.7f){MinimumPawDistance=Mathf.Min(MinimumPawDistance,pawContact.Distance);
                    if(!touched && pawContact.Distance<.025f){CaptureStrike();direction=WorldFallDirection;push=glassPivot.parent.InverseTransformDirection(direction);axis=Vector3.Cross(Vector3.up,push);touched=true;}}
                if(touched)glassPivot.localRotation=pivotHomeRotation*Quaternion.AngleAxis(Mathf.Sin(t*Mathf.PI*3)*7*(1-t),axis);
                yield return null;elapsed+=Time.deltaTime;
            }
            IsTapping=false;pawContact.Clear();glassPivot.localRotation=pivotHomeRotation;
            if(!touched){CancelForTransition();yield break;}
            ContactStrokes++;yield return Wait(.15f);
        }
        Bounds table = TableBounds();
        yield return FallFromTable(stand, facing, null);
        if (!CatActivityMotion.IsControllerFloorClear(Cat, Cat.transform.position))
        {
            if (StoreProductId == HomeStoreService.BalconySideTableId)
            {
                // The two 180-degree routes are not equivalent here: the
                // inward one sweeps the head through the raised table rim.
                yield return Move(stand, stand, Cat.transform.rotation,
                    LookTowards(stand-table.center, facing), .18f);
            }
            // A planted paw stance may be narrower than the controller's
            // turning footprint. Retrace only enough of the clear approach
            // to restore that footprint before handing movement back.
            var reverse = new List<Vector3>(contactPath); reverse.Insert(0, approachOrigin);
            bool clear = false;
            for (int i = reverse.Count - 2; i >= 0 && !clear; i--)
            {
                Vector3 from = Cat.transform.position, destination = Flatten(reverse[i], stand.y);
                int samples = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from,destination)/.025f));
                for (int sample = 1; sample <= samples; sample++)
                {
                    var candidate = Vector3.Lerp(from,destination,(float)sample/samples);
                    if (!CatActivityMotion.IsControllerFloorClear(Cat,candidate)) continue;
                    destination = candidate; clear = true; break;
                }
                yield return Move(from,destination,Cat.transform.rotation,
                    LookTowards(destination-from,Cat.transform.rotation),Mathf.Max(.18f,Vector3.Distance(from,destination)/1.1f));
            }
        }
        RestoreCat();CompleteActivity("OOPS.");
    }

    private bool TryGroundedPawStand(Vector3 origin, out Vector3 stand, out List<Vector3> path)
    {
        stand = reachPoint.position; path = null;
        if (glassPivot == null) return false;
        if (StoreProductId != HomeStoreService.BalconySideTableId)
        {
            var authored = reachPoint.position; authored.y = origin.y;
            return CatActivityFacing.TryFindContactStand(Cat, glassPivot.position, authored, origin, out stand, out path);
        }
        var renderer = glassPivot.GetComponentInChildren<Renderer>();
        if (renderer == null) return false;
        var bounds = renderer.bounds;
        Vector3 offset = reachPoint.position - glassPivot.position; offset.y = 0;
        // The old stand relied on Attack's forward body translation. Measure
        // from the near prop face, leaving a normal foreleg's reach instead.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            float radius = .34f + attempt * .04f + Mathf.Min(bounds.extents.x, bounds.extents.z);
            var authored = glassPivot.position + offset.normalized * radius;
            authored.y = origin.y;
            if (!CatActivityFacing.TryFindContactStand(Cat, glassPivot.position, authored, origin, out stand, out path)) continue;
            Vector3 staging=stand-GroundedFacing(stand)*Vector3.forward*.18f;
            if(!CatActivityMotion.ClearSegment(staging,stand)||!CatActivityMotion.TryFloorPath(origin,staging,out path))continue;
            path.Add(stand);return true;
        }
        return false;
    }

    private Quaternion GroundedFacing(Vector3 stand)
    {
        var facing=LookTowards(glassPivot.position-stand,Cat.transform.rotation);
        if(StoreProductId!=HomeStoreService.BalconySideTableId)return facing;
        var left=facing*Quaternion.Euler(0,-35,0);var right=facing*Quaternion.Euler(0,35,0);
        var camera=CatActivityFacing.CameraPosition(Cat);
        return CatActivityFacing.FacingDot(left*Vector3.forward,stand,camera)>
            CatActivityFacing.FacingDot(right*Vector3.forward,stand,camera)?left:right;
    }

    private IEnumerator SupportedKnockRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        Vector3 floor = ContactStand; floor.y = Cat.transform.position.y;
        Vector3 perch = perchPoint.position;
        Quaternion facing = LookTowards(glassPivot.position - perch, Cat.transform.rotation);
        Quaternion launch = LookTowards(perch - floor, facing);
        yield return CatActivityMotion.WalkAuthoredStep(Cat, floor, launch, .2f);
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, perchPoint);
        yield return supportedMotion.Jump(floor, perch, launch, facing);
        IsOnNightstand = true;
        pawContact = Cat.GetComponent<CatToyContactMotion>() ?? Cat.gameObject.AddComponent<CatToyContactMotion>();
        Vector3 direction = WorldFallDirection;
        Vector3 localPush = glassPivot.parent.InverseTransformDirection(direction);
        Vector3 axis = Vector3.Cross(Vector3.up, localPush);
        var animation = Cat.GetComponent<CatActivityAnimation>();
        for (int beat = 0; beat <= TeaseCount; beat++)
        {
            IsTapping = true;
            float elapsed = 0f; bool touched = false;
            while (elapsed < .70f)
            {
                float phase = Mathf.Clamp01(elapsed / .70f);
                Cat.transform.SetPositionAndRotation(perch, facing);
                animation.SetPose(CatActivityPose.GentleKnead, perchPoint);
                Bounds glassBounds = glassPivot.GetComponentInChildren<Renderer>().bounds;
                float radius = Mathf.Min(glassBounds.extents.x, glassBounds.extents.z);
                Vector3 target = glassBounds.center - facing * Vector3.forward * (radius * .95f);
                target.y = Mathf.Clamp(perch.y + .06f, glassBounds.min.y + .015f, glassBounds.max.y - .015f);
                pawContact.Reach(target, true, phase);
                if (phase > .28f && phase < .7f) MinimumPawDistance = Mathf.Min(MinimumPawDistance, pawContact.Distance);
                if (!touched && phase > .28f && phase < .7f && pawContact.Distance < .025f)
                {CaptureStrike();direction=WorldFallDirection;localPush=glassPivot.parent.InverseTransformDirection(direction);axis=Vector3.Cross(Vector3.up,localPush);touched=true;}
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

    private void CaptureStrike()
    {
        GameAudio.Play(CatFoley.ContactCue(this), .8f);
        LastStrikePosition = pawContact.LastContactPosition;
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

    private IEnumerator KnockRoutine()
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

        Vector3 push = glassPivot.parent != null ? glassPivot.parent.InverseTransformDirection(WorldFallDirection) : WorldFallDirection;
        // The prop's fall direction is not the direction from the cat to the
        // prop (a glass may fall back towards the near edge).
        Vector3 towardsGlass = Flatten(glassPivot.position - reach, 0f).normalized;
        Quaternion facing = LookTowards(towardsGlass, toReach);
        yield return Move(reach, reach, toReach, facing, 0.20f);

        // Tease: paw out, the glass rocks and settles. Twice, so the third one
        // reads as the cat deciding rather than as an accident.
        for (int i = 0; i < TeaseCount; i++)
        {
            float tap = 0f;
            PlayCatPose(CatActivityPose.Paw);
            while (tap < 0.42f)
            {
                tap += Time.deltaTime;
                float t = Mathf.Clamp01(tap / 0.42f);
                float paw = Mathf.Sin(t * Mathf.PI);
                float rock = Mathf.Sin(t * Mathf.PI * 2.4f) * (1f - t);
                Cat.transform.position = reach + towardsGlass * (paw * 0.060f);
                Cat.transform.rotation = facing * Quaternion.Euler(-paw * 18f, 0f, 0f);
                glassPivot.localPosition = pivotHome + push * (paw * 0.022f);
                glassPivot.localRotation = pivotHomeRotation * Quaternion.AngleAxis(
                    rock * 11f, Vector3.Cross(Vector3.up, push));
                yield return null;
            }

            glassPivot.localPosition = pivotHome;
            glassPivot.localRotation = pivotHomeRotation;
            yield return Wait(0.22f);
        }

        // The push that does it.
        float shove = 0f;
        while (shove < 0.24f)
        {
            shove += Time.deltaTime;
            float t = Mathf.Clamp01(shove / 0.24f);
            Cat.transform.position = reach + towardsGlass * (Mathf.Sin(t * Mathf.PI) * 0.085f);
            Cat.transform.rotation = facing * Quaternion.Euler(-t * 24f, 0f, 0f);
            glassPivot.localPosition = pivotHome + push * (t * 0.130f);
            yield return null;
        }

        // The fall: out and down, tumbling as it goes.
        Vector3 lipPosition = pivotHome + push * 0.130f;
        Vector3 floorPosition = lipPosition + push * 0.140f - Vector3.up * FallHeight;
        Vector3 axis = Vector3.Cross(Vector3.up, push);
        float fall = 0f;
        PlayCatPose(CatActivityPose.Sit);
        while (fall < 0.34f)
        {
            fall += Time.deltaTime;
            float t = Mathf.Clamp01(fall / 0.34f);
            // Gravity, not a lerp: a constant-speed fall reads as a lift.
            glassPivot.localPosition = Vector3.Lerp(lipPosition, floorPosition,
                                                    t * t);
            glassPivot.localRotation =
                pivotHomeRotation * Quaternion.AngleAxis(t * 82f, axis);
            // The cat leans over the edge to watch it go.
            Cat.transform.rotation = facing * Quaternion.Euler(24f + t * 12f, 0f, 0f);
            yield return null;
        }

        // One small bounce, then it lies still on its side.
        float bounce = 0f;
        while (bounce < 0.36f)
        {
            bounce += Time.deltaTime;
            float t = Mathf.Clamp01(bounce / 0.36f);
            float hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 1.6f)) * (1f - t) * 0.070f;
            glassPivot.localPosition = floorPosition + Vector3.up * hop;
            glassPivot.localRotation =
                pivotHomeRotation * Quaternion.AngleAxis(82f + t * 8f, axis);
            Cat.transform.rotation = facing * Quaternion.Euler(30f, 0f, 0f);
            yield return null;
        }

        yield return Wait(0.30f);
        Cat.transform.rotation = facing;
        Quaternion away = CatActivityFacing.Resolve(Cat, reach, LookTowards(start - reach, facing));
        yield return Move(reach, reach, facing, away, 0.24f);

        RestoreCat();
        CompleteActivity("OOPS.");
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
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
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

    private void RestoreCat()
    {
        pawContact?.Clear(); supportedMotion?.End(); supportedMotion = null;
        if (IsOnNightstand && Cat != null) Cat.transform.position = ContactStand;
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
