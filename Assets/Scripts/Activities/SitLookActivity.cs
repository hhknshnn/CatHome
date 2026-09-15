using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SitLookReaction
{
    Sit = 0,
    PawSwat = 1,
    Pounce = 2
}

/// <summary>
/// Shared sit-and-look activity used by Bond milestone play: window watching,
/// feather-toy swats and garden bird watching.
/// </summary>
[DisallowMultipleComponent]
public sealed class SitLookActivity : CatActivity
{
    [Header("Look")]
    [SerializeField] private Transform lookPoint;
    [SerializeField] private SitLookReaction reactionKind = SitLookReaction.Sit;
    [SerializeField, Min(0.5f)] private float lookDuration = 2.4f;
    [SerializeField] private string completeMessage = "SO COZY!";
    [SerializeField] private Vector3[] visibleLookTargets = new Vector3[0];

    private CatFurnitureGaze gaze;
    private CharacterController characterController;
    private int gestureBeats;
    public int GestureBeats => gestureBeats;
    public bool ViewStandBlocked { get; private set; }
    public Vector3 ActiveLookTarget { get; private set; }
    public Vector3 ViewStand { get; private set; }
    public Quaternion ViewRotation { get; private set; }
    public int VisibleLookTargetCount => visibleLookTargets != null ? visibleLookTargets.Length : 0;
    private List<Vector3> viewApproach;
    private Vector3 plannedOrigin;
    // These floor observations can start beside their visible product. Search
    // locally once; the generic contact routes used in other rooms are unchanged.
    private bool UsesLocalViewingRoute => ReactionKind == SitLookReaction.Sit &&
        (Kind == CatActivityKind.BookshelfSniff || Kind == CatActivityKind.BookSetSniff ||
         Kind == CatActivityKind.PlantSniff || Kind == CatActivityKind.LampWatch ||
         Kind == CatActivityKind.FridgeStare || (IsMirrorGaze && HasNearbyApproach));

    public override string ProgressLabel => IsRunning ? DisplayName : string.Empty;
    public Transform LookPoint => lookPoint;
    // Existing bathroom scenes serialized PawSwat. The reflection is observed
    // calmly even before that obsolete authoring value is regenerated.
    private bool IsMirrorGaze => Kind == CatActivityKind.MirrorGaze;
    public SitLookReaction ReactionKind => IsMirrorGaze ? SitLookReaction.Sit : reactionKind;
    protected override bool UsesNearbyRoutineEntry => true;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (lookPoint == null)
        {
            failureReason = "NOT READY YET";
            return false;
        }

        ViewStandBlocked = !ResolveViewingRoute(Cat.transform.position);
        failureReason = ViewStandBlocked ? "LET'S MAKE SOME ROOM." : string.Empty;
        return !ViewStandBlocked;
    }

    protected override bool BeginActivity()
    {
        gestureBeats = 0;
        // CanBeginActivity already solved this route before reserving energy.
        // Only re-plan if the shared entrance actually moved the cat.
        if (!UsesLocalViewingRoute || viewApproach == null || (Cat.transform.position - plannedOrigin).sqrMagnitude > .0001f)
            ViewStandBlocked = !ResolveViewingRoute(Cat.transform.position);
        if (ViewStandBlocked) return false;
        PlayCatPose(IsMirrorGaze ? CatActivityPose.SitDown : CatActivityPose.Walk);
        gaze = Cat.GetComponent<CatFurnitureGaze>();
        if(gaze == null) gaze=Cat.gameObject.AddComponent<CatFurnitureGaze>();
        characterController = Cat.GetComponent<CharacterController>();
        StartCoroutine(LookRoutine());
        return true;
    }

    private IEnumerator LookRoutine()
    {
        Cat.SetMovementLocked(this, true);
        Vector3 startPosition = Cat.transform.position;
        Quaternion startRotation = Cat.transform.rotation;
        Vector3 sitPoint = ViewStand;
        Vector3 lookTarget = ActiveLookTarget;
        List<Vector3> approach = viewApproach;
        Vector3 lookDirection = lookTarget - sitPoint;
        lookDirection.y = 0f;
        Quaternion sitRotation = ReactionKind == SitLookReaction.Sit ? ViewRotation : lookDirection.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : startRotation;

        if (characterController != null)
            characterController.enabled = false;

        float elapsed = 0f;
        bool movesToSeat = (sitPoint - startPosition).sqrMagnitude > .0004f;
        if (movesToSeat)
        {
            if (approach == null && !CatActivityMotion.TryFloorPath(startPosition, sitPoint, out approach))
            {
                ViewStandBlocked = true;
                approach = new List<Vector3>();
                sitPoint = startPosition;
                Vector3 fallbackLook = lookTarget - sitPoint; fallbackLook.y = 0f;
                if (fallbackLook.sqrMagnitude > .001f) sitRotation = Quaternion.LookRotation(fallbackLook);
            }
            foreach (Vector3 waypoint in approach)
            {
                Vector3 from = Cat.transform.position;
                Vector3 to = Flatten(waypoint, startPosition.y);
                Vector3 travel = to - from; travel.y = 0f;
                if (travel.sqrMagnitude < .0001f) continue;
                Quaternion travelRotation = Quaternion.LookRotation(travel);
                PlayCatPose(CatActivityPose.Sniff);
                yield return CatActivityFacing.Turn(Cat, travelRotation);
                PlayCatPose(CatActivityPose.Walk);
                float duration = travel.magnitude / .85f;
                elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    Cat.transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                    Cat.transform.rotation = travelRotation;
                    yield return null;
                }
            }
        }
        Cat.transform.position = sitPoint;
        PlayCatPose(CatActivityPose.Sniff);
        yield return CatActivityFacing.Turn(Cat, sitRotation);

        if (characterController != null)
            characterController.enabled = true;

        if (IsMirrorGaze)
        {
            PlayCatPose(CatActivityPose.SitDown);
            yield return new WaitForSeconds(.7f);
        }

        // The entire observed beat belongs to this routine. A one-shot reaction
        // previously reset to Sit after .58s, leaving most of the interaction idle.
        // Touching props alternate paws; distant/hung objects receive a seated gaze.
        float beatDuration = lookDuration / 3f;
        for(int beat=0;beat<3;beat++)
        {
            CatActivityPose pose = ReactionKind == SitLookReaction.Sit ? CatActivityPose.Sit :
                ReactionKind == SitLookReaction.Pounce && beat == 0 ? CatActivityPose.Stalk :
                beat == 1 ? CatActivityPose.BatRight : CatActivityPose.BatLeft;
            PlayCatPose(pose); gestureBeats++;
            elapsed=0;
            while(elapsed<beatDuration)
            {
                elapsed+=Time.deltaTime;
                if(Cat!=null && gaze!=null)
                {
                    float blend=Mathf.Sin(Mathf.Clamp01(elapsed/beatDuration)*Mathf.PI);
                    gaze.LookAt(lookTarget,blend,ReactionKind == SitLookReaction.Sit ? CatFurnitureGaze.SeatedYawLimit : CatFurnitureGaze.DefaultYawLimit);
                }
                yield return null;
            }
        }

        if(gaze!=null)gaze.Clear();
        if (IsMirrorGaze)
        {
            PlayCatPose(CatActivityPose.StandUp);
            yield return new WaitForSeconds(.7f);
        }
        if (Cat != null)
            Cat.SetMovementLocked(this, false);
        CompleteActivity(
            string.IsNullOrWhiteSpace(completeMessage) ? "SO COZY!" : completeMessage);
    }

    Vector3 LookTargetAt(int index) => VisibleLookTargetCount > 0
        ? transform.TransformPoint(visibleLookTargets[index]) : lookPoint.position;

    public bool TryGetVisibleLookPoint(CatMovement cat, Vector3 sightFrom, out Vector3 target)
    {
        target = lookPoint != null ? lookPoint.position : transform.position;
        if (lookPoint == null) return false;
        float nearest = float.PositiveInfinity; bool found = false;
        for (int i = 0; i < Mathf.Max(1, VisibleLookTargetCount); i++)
        {
            Vector3 candidate = LookTargetAt(i);
            float square = (candidate - sightFrom).sqrMagnitude;
            if (square >= nearest || !CatActivityApproach.HasClearSight(this, cat, sightFrom, candidate)) continue;
            target = candidate; nearest = square; found = true;
        }
        return found;
    }

    bool ResolveViewingRoute(Vector3 origin)
    {
        if (Cat == null || lookPoint == null) return false;
        Physics.SyncTransforms();
        plannedOrigin = origin;
        if (UsesLocalViewingRoute)
        {
            if (ResolveLocalViewingRoute(origin)) return true;
            // A distant scripted start may still need the authored room path.
            // Normal nearby mirror taps use the same short search as the living room.
            if (!IsMirrorGaze && Kind != CatActivityKind.FridgeStare) return false;
        }
        Vector3 authored = Flatten(RoutineEntryPoint != null ? RoutineFloorPosition : transform.position, origin.y);
        float best = float.PositiveInfinity; viewApproach = null;
        for (int i = 0; i < Mathf.Max(1, VisibleLookTargetCount); i++)
        {
            Vector3 target = LookTargetAt(i), stand = authored;
            List<Vector3> route = null;
            bool seated = ReactionKind == SitLookReaction.Sit;
            bool readable = seated ? CatActivityFacing.TryResolveViewFacing(Cat,stand,target,out _) :
                CatActivityFacing.FacingDot(target-stand,stand,CatActivityFacing.CameraPosition(Cat)) >= CatActivityFacing.PreferredViewDot;
            bool floorClear = seated ? CatActivityMotion.IsControllerFloorClear(Cat,stand) : CatActivityMotion.IsFloorClear(stand);
            bool found = readable && floorClear && (seated ? CatActivityMotion.TryFloorPath(Cat,origin,stand,out route) :
                CatActivityMotion.TryFloorPath(origin,stand,out route));
            if (found && !CatActivityApproach.HasClearSight(this, Cat, stand + Vector3.up * .4f, target)) found = false;
            if (!found) found = ReactionKind == SitLookReaction.Sit
                ? CatActivityFacing.TryFindViewStand(Cat, target, authored, origin, out stand, out route, this)
                : CatActivityFacing.TryFindContactStand(Cat, target, authored, origin, out stand, out route, this);
            if (!found || !CatActivityApproach.HasClearSight(this, Cat, stand + Vector3.up * .4f, target)) continue;
            float length = 0f; Vector3 previous = origin;
            foreach (Vector3 point in route) { length += Vector3.Distance(previous, point); previous = point; }
            if (length >= best) continue;
            best = length; ViewStand = stand; ActiveLookTarget = target; viewApproach = route;
            Quaternion rotation;
            if (seated) CatActivityFacing.TryResolveViewFacing(Cat,stand,target,out rotation);
            else rotation = Quaternion.LookRotation(new Vector3(target.x-stand.x,0,target.z-stand.z));
            ViewRotation = rotation;
        }
        return viewApproach != null;
    }

    bool ResolveLocalViewingRoute(Vector3 origin)
    {
        viewApproach = null;
        Vector3 camera = CatActivityFacing.CameraPosition(Cat);
        float radius = CatActivityMotion.ControllerFloorRadius(Cat);
        var boundary = HomeRoomBoundary.FindFor(Cat.gameObject.scene);
        Vector3 centre = Kind == CatActivityKind.FridgeStare && !HasNearbyApproach ? RoutineEntryPoint.position : origin;
        centre.y = origin.y;
        var candidates = new List<Vector3> { origin };
        // Reuse the room's .2m floor lattice: polar samples can miss the narrow
        // open aisle beside the armchair. No occupancy grid or BFS is rebuilt.
        for (int x = 0; x < 38; x++) for (int z = 0; z < 32; z++)
        {
            Vector3 point = CatActivityMotion.GridPoint(x, z); point.y = origin.y;
            if ((point - centre).sqrMagnitude <= 1.6f * 1.6f) candidates.Add(point);
        }
        candidates.Sort((a,b) => (a-origin).sqrMagnitude.CompareTo((b-origin).sqrMagnitude));
        foreach (Vector3 stand in candidates)
        {
            if (!CatActivityMotion.IsFloorClear(stand, radius) ||
                (boundary != null && (boundary.ClampPosition(stand, radius)-stand).sqrMagnitude >= .000001f)) continue;
            for (int index = 0; index < Mathf.Max(1, VisibleLookTargetCount); index++)
            {
                Vector3 target = LookTargetAt(index), flat = target - stand; flat.y = 0;
                if (flat.sqrMagnitude < .0001f || flat.sqrMagnitude > 1.5f * 1.5f) continue;
                Quaternion preferred = Quaternion.LookRotation(flat);
                Quaternion rotation = CatActivityFacing.Resolve(stand, preferred, camera);
                if (Quaternion.Angle(rotation, preferred) > CatFurnitureGaze.SeatedYawLimit - 5f ||
                    !CatActivityApproach.HasClearSight(this, Cat, stand + Vector3.up * .4f, target)) continue;
                // The approach disables the controller and uses the same .27m
                // swept body clearance as the common floor entrance. The final
                // seat additionally clears the full controller turning envelope.
                List<Vector3> route;
                if (Kind == CatActivityKind.FridgeStare && !HasNearbyApproach)
                {
                    if (!CatActivityMotion.TryFloorPath(origin, stand, out route)) break;
                }
                else
                {
                    if (!CatActivityMotion.ClearSegment(origin, stand)) break;
                    route = new List<Vector3> { stand };
                }
                ViewStand = stand; ViewRotation = rotation; ActiveLookTarget = target;
                viewApproach = route;
                return true;
            }
        }
        return false;
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
        if(gaze!=null)gaze.Clear();
        if (characterController != null)
            characterController.enabled = true;
        if (Cat != null)
            Cat.SetMovementLocked(this, false);
        base.CancelActivity();
    }

#if UNITY_EDITOR
    // The builder supplies actual final-mesh surface points in product-local
    // space. Runtime never substitutes an empty bounding-box corner for a prop.
    public void EditorConfigureVisibleLookTargets(Vector3[] localSurfacePoints) =>
        visibleLookTargets = localSurfacePoints != null ? (Vector3[])localSurfacePoints.Clone() : new Vector3[0];

    public void EditorConfigureLook(
        Transform point,
        SitLookReaction kind,
        float duration,
        string message)
    {
        lookPoint = point;
        reactionKind = kind;
        lookDuration = Mathf.Max(0.5f, duration);
        completeMessage = string.IsNullOrWhiteSpace(message) ? "SO COZY!" : message;
    }
#endif
}
