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
    private CatToyContactMotion contact;
    private CatPawReachMotion pawReach;
    public int ContactCount { get; private set; }
    public float MinimumPawDistance { get; private set; }
    public Vector3 LastContactPosition { get; private set; }

    public override string ProgressLabel => !IsRunning ? string.Empty : Kind == CatActivityKind.FernWatch
        ? GameLanguageService.Text("interaction.fern.progress") : GameLanguageService.Text("interaction.watching");
    public Transform LookPoint => lookPoint;
    // Existing bathroom scenes serialized PawSwat. The reflection is observed
    // calmly even before that obsolete authoring value is regenerated.
    private bool IsMirrorGaze => Kind == CatActivityKind.MirrorGaze;
    public SitLookReaction ReactionKind => IsMirrorGaze ? SitLookReaction.Sit : reactionKind;
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;

    struct LookStartCandidate { public Vector3 point; public float distance; public int index; }
    readonly List<LookStartCandidate> startCandidates = new List<LookStartCandidate>(32);
    public bool IsStartSearchPending {get;private set;}
    CatMovement startSearchActor;
    Matrix4x4 startSearchActorMatrix,startSearchOwnerMatrix,startSearchLookMatrix;
    Vector3[] startSearchTargets;
    int startSearchIndex;
    int startSearchFrame=-1,startSearchFrameTarget;
    bool startSearchInitialized;
    static int CompareLookStart(LookStartCandidate a, LookStartCandidate b)
    { int distance = a.distance.CompareTo(b.distance); return distance != 0 ? distance : a.index.CompareTo(b.index); }

    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        IsStartSearchPending=false;
        Vector3 centre = RoutineEntryPoint != null ? RoutineEntryPoint.position : transform.position;
        start = new CatActivityStart { ZoneCentre = centre, ActionTarget = lookPoint != null ? lookPoint.position : centre,
            PromptDistance = float.PositiveInfinity };
        if (actor == null || lookPoint == null) return false;
        // Validate the current body once. Looking and touching never search for
        // another stand or choose a camera-facing root orientation after a click.
        if (!CatActivityStartResolver.Current(actor, actor.transform.position, 0f, out var candidate)) return false;
        candidate.ZoneCentre = centre;
        bool seated = ReactionKind == SitLookReaction.Sit;
        bool flowers = StoreProductId == HomeStoreService.BalconyRailingFlowersId;
        var actorMatrix=actor.transform.localToWorldMatrix;
        var ownerMatrix=transform.localToWorldMatrix;var lookMatrix=lookPoint.localToWorldMatrix;
        if(!flowers||!startSearchInitialized||startSearchActor!=actor||
            !startSearchActorMatrix.Equals(actorMatrix)||!startSearchOwnerMatrix.Equals(ownerMatrix)||
            !startSearchLookMatrix.Equals(lookMatrix)||!ReferenceEquals(startSearchTargets,visibleLookTargets))
        {
            startSearchInitialized=true;startSearchActor=actor;startSearchActorMatrix=actorMatrix;
            startSearchOwnerMatrix=ownerMatrix;startSearchLookMatrix=lookMatrix;
            startSearchTargets=visibleLookTargets;startSearchIndex=0;startSearchFrame=-1;
        }
        float nearest = float.PositiveInfinity;
        startCandidates.Clear();
        for (int i = 0; i < Mathf.Max(1, VisibleLookTargetCount); i++)
        {
            Vector3 point = LookTargetAt(i), direction = point - actor.transform.position;
            direction.y = 0;
            float distance = direction.magnitude;
            if (distance < (seated ? .1f : .30f) || distance > (seated ? 1.5f : .58f) ||
                Vector3.Angle(actor.transform.forward, direction) > (seated ? CatFurnitureGaze.SeatedYawLimit - 5f : flowers ? 100f : 25f)) continue;
            startCandidates.Add(new LookStartCandidate { point = point, distance = distance, index = i });
        }
        // Test nearest surfaces first. This returns exactly the previous closest
        // reachable result, without solving every farther target along the way.
        startCandidates.Sort(CompareLookStart);
        // Share the common per-frame budget across nearby surfaces, so one
        // difficult leaf cannot starve a reachable neighbour. A ready surface
        // stays first for the next fresh prompt/click validation.
        if(flowers&&startCandidates.Count>0&&startSearchFrame!=Time.frameCount)
        {
            startSearchFrame=Time.frameCount;
            startSearchFrameTarget=startSearchIndex%startCandidates.Count;
        }
        // HUD selection, hysteresis and click may all poll in the same frame.
        // They must retry this frame's current target: a zero-budget Pending
        // result cannot consume another target's turn. Every call still runs
        // the current body/surface physics, including cached accepted plans.
        int firstTarget = flowers && startCandidates.Count > 0 ? startSearchFrameTarget % startCandidates.Count : 0;
        for(int step=0;step<startCandidates.Count;step++)
        {
            int targetIndex = (firstTarget + step) % startCandidates.Count;
            if(flowers)startSearchIndex=targetIndex;
            var target=startCandidates[targetIndex];
            Vector3 point = target.point;
            float distance = target.distance;
            if (!CatActivityApproach.HasClearSight(this, actor, actor.transform.position + Vector3.up * .4f, point)) continue;
            if (!seated)
            {
                bool left = Vector3.Dot(point - actor.transform.position, actor.transform.right) <= 0;
                var pose = flowers || point.y - actor.transform.position.y > .55f ? CatActivityPose.Scratch :
                    left ? CatActivityPose.BatLeft : CatActivityPose.BatRight;
                CatPawReachPlan plan;
                if (flowers)
                {
                    if(!CatPawReachResolver.TryMeasureSurface(transform,point,out var surface))continue;
                    if(!CatPawReachResolver.TryResolveSurface(actor,surface,left,pose,out plan,
                        CatPawReachResolver.MaximumChestPitch,CatPawReachResolver.MaximumChestYaw))
                    {
                        if(CatPawReachResolver.SurfaceQueryPending)
                        {
                            startSearchFrameTarget=targetIndex;
                            startSearchIndex=(targetIndex+1)%startCandidates.Count;
                            IsStartSearchPending=true;start=candidate;return false;
                        }
                        continue;
                    }
                    point=surface.point;
                }
                else if (!CatPawReachResolver.TryResolve(actor, point, left, pose, out plan,
                    CatPawReachResolver.MaximumChestPitch, 0)) continue;
                candidate.PawPlan = plan; candidate.HasPawPlan = true;
            }
            if(flowers){startSearchFrameTarget=targetIndex;startSearchIndex=targetIndex;}
            nearest = distance; candidate.ActionTarget = point;
            break;
        }
        if(flowers&&float.IsPositiveInfinity(nearest))startSearchIndex=0;
        candidate.PromptDistance = nearest;
        candidate.Kind = seated ? CatActivityStartKind.Stationary : CatActivityStartKind.Contact;
        start = candidate;
        return !float.IsPositiveInfinity(nearest);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        ViewStandBlocked = lookPoint == null;
        failureReason = ViewStandBlocked ? "NOT READY YET" : string.Empty;
        if (ViewStandBlocked) return false;
        ViewStand = AcceptedStart.Position;
        ViewRotation = AcceptedStart.Rotation;
        ActiveLookTarget = AcceptedStart.ActionTarget;
        return true;
    }

    protected override bool BeginActivity()
    {
        gestureBeats = ContactCount = 0;
        MinimumPawDistance = float.PositiveInfinity;
        gaze = Cat.GetComponent<CatFurnitureGaze>() ?? Cat.gameObject.AddComponent<CatFurnitureGaze>();
        if (ReactionKind != SitLookReaction.Sit)
        {
            if (!AcceptedStart.HasPawPlan) return false;
            contact = Cat.GetComponent<CatToyContactMotion>() ?? Cat.gameObject.AddComponent<CatToyContactMotion>();
            pawReach = Cat.GetComponent<CatPawReachMotion>() ?? Cat.gameObject.AddComponent<CatPawReachMotion>();
        }
        characterController = Cat.GetComponent<CharacterController>();
        StartCoroutine(LookRoutine());
        return true;
    }

    private IEnumerator LookRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        if (ReactionKind == SitLookReaction.Sit)
        {
            PlayCatPose(CatActivityPose.SitDown);
            yield return new WaitForSeconds(.55f);
        }
        else if (AcceptedStart.PawPlan.Pose == CatActivityPose.Scratch)
        {
            // Enter the raised source posture through its existing crossfade.
            // The stance stays still while the chest and forelegs rise.
            PlayCatPose(CatActivityPose.Scratch);
            yield return new WaitForSeconds(.18f);
        }
        float beatDuration = lookDuration / 3f;
        for (int beat = 0; beat < 3; beat++)
        {
            bool seated = ReactionKind == SitLookReaction.Sit;
            bool left = Vector3.Dot(ActiveLookTarget - ViewStand, ViewRotation * Vector3.right) <= 0f;
            CatActivityPose pose = seated ? CatActivityPose.Sit :
                ActiveLookTarget.y - ViewStand.y > .55f ? CatActivityPose.Scratch :
                left ? CatActivityPose.BatLeft : CatActivityPose.BatRight;
            gestureBeats++;
            bool touched = false;
            float elapsed = 0f;
            while (elapsed < beatDuration)
            {
                Cat.transform.SetPositionAndRotation(ViewStand, ViewRotation);
                float phase = Mathf.Clamp01(elapsed / beatDuration);
                if (seated)
                {
                    PlayCatPose(pose);
                    gaze.LookAt(ActiveLookTarget, Mathf.Sin(phase * Mathf.PI), CatFurnitureGaze.SeatedYawLimit);
                }
                else
                {
                    pawReach.Sample(this, AcceptedStart.PawPlan, phase);
                    if (AcceptedStart.PawPlan.Surface != null)
                    {
                        // Observe the same frame's actual skinned patch after
                        // the source pose, chest and final limb solve.
                        yield return new WaitForEndOfFrame();
                        if (!IsRunning) yield break;
                    }
                    if (Time.timeScale > 0f && phase >= .30f && phase <= .68f)
                    {
                        MinimumPawDistance = Mathf.Min(MinimumPawDistance, contact.Distance);
                        if (!touched && contact.Distance <= .025f)
                        {
                            touched = true; ContactCount++;
                            LastContactPosition = contact.LastContactPosition;
                            GameAudio.Play(AudioCue.Leaves, .45f);
                        }
                    }
                }
                yield return null;
                if (Time.timeScale > 0f) elapsed += Time.deltaTime;
            }
            pawReach?.Clear(); contact?.Clear();
            if (!seated && !touched) { CancelForTransition(); yield break; }
        }
        gaze.Clear(); pawReach?.Clear(); contact?.Clear();
        if (ReactionKind == SitLookReaction.Sit)
        {
            PlayCatPose(CatActivityPose.StandUp);
            yield return new WaitForSeconds(.55f);
        }
        Cat.SetMovementLocked(this, false);
        CompleteActivity(Kind == CatActivityKind.FernWatch ? "FERN_INSPECTED" :
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

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        if(gaze!=null)gaze.Clear();
        pawReach?.Clear(); contact?.Clear();
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
