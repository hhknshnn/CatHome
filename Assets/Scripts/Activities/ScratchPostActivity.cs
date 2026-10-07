using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScratchPostActivity : CatActivity
{
    [SerializeField] Transform scratchPoint;
    [SerializeField,Min(.5f)] float scratchDuration=2.4f;
    [SerializeField] Transform hangingToy;
    [SerializeField] Vector3 ropeCenter=new Vector3(0,.43f,.084f);
    [SerializeField] float ropeRadius=.115f;
    CharacterController controller;
    bool wasEnabled,captured;
    Vector3 start,singleContactPoint,singleEndPoint;
    CatSpringGeometry geometry;
    CatFurnitureGaze gaze;
    float strokeHeight,strokeAmplitude,strokeSpread;
    int rhythm;
    bool alternateReady;
    CatPawReachPlan alternatePlan;
    Vector3 alternateSurface,alternateEnd;
    bool Polished => gameObject.scene.path==HomeRoomService.LivingRoomScenePath && StoreProductId==HomeStoreService.ScratchPostId;
    public int RhythmVariation {get;private set;}
    public int ActiveScratchHand {get;private set;}=-1;
    public float ScratchPhase {get;private set;}
    public float MeasuredReach => geometry!=null?geometry.Reach:0;
    Quaternion rotation,toyRotation;
    CatToyContactMotion contact;
    CatPawReachMotion reachMotion;
    Transform leftHand,rightHand;
    public const float StrokeContactTolerance=.025f;
    public int LeftStrokes {get;private set;}
    public int RightStrokes {get;private set;}
    public Vector3 LastLeftSurface {get;private set;}
    public Vector3 LastRightSurface {get;private set;}
    public float LastLeftSurfaceDistance {get;private set;}
    public float LastRightSurfaceDistance {get;private set;}
    protected override bool UsesFloorApproach=>false;
    public override string ProgressLabel=>IsRunning?"SCRATCHING...":string.Empty;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        geometry=actor!=null&&Polished?CatSpringGeometry.Measure(actor):null;
        if(Polished&&geometry!=null)
        {
            if(PrepareAlternating(actor,out start))return true;
            var body=GetComponentInChildren<MeshCollider>();
            if(body==null)return false;
            var direction=Vector3.ProjectOnPlane(body.bounds.center-actor.transform.position,Vector3.up);
            if(direction.sqrMagnitude<.0001f)return false;
            float distance=direction.magnitude;
            if(distance<geometry.StandDistance+.02f||distance>geometry.StandDistance+geometry.Reach+Mathf.Min(body.bounds.extents.x,body.bounds.extents.z)||!VisibleStance(actor,body.bounds.center))return false;
            var toward=Quaternion.LookRotation(direction);
            var headings=new[]{-14f,-10f,-5f,0f,5f,10f,14f};
            System.Array.Sort(headings,(a,b)=>Quaternion.Angle(actor.transform.rotation,toward*Quaternion.Euler(0,a,0)).CompareTo(Quaternion.Angle(actor.transform.rotation,toward*Quaternion.Euler(0,b,0))));
            foreach(float yaw in headings)
            {
                var heading=toward*Quaternion.Euler(0,yaw,0);
                if(Quaternion.Angle(actor.transform.rotation,heading)<=90f&&PrepareAlternating(actor,out start,heading)&&ClearTurn(actor,heading))return true;
            }
            return false;
        }
        if(geometry!=null)
        {
            strokeHeight=geometry.ShoulderLocal.y*geometry.Scale+geometry.Reach*.60f;
            strokeAmplitude=geometry.Reach*.28f;
            strokeSpread=Vector3.Distance(geometry.LeftHandLocal,geometry.RightHandLocal)*geometry.Scale*.45f;
        }
        if(TryPrepareSweep(actor,out start))return true;
        // A short cat cannot put both paws up the shaft from a clear standing
        // pose. It can scratch from beside it with the nearer paw, using the
        // same source-body and limb-length checks as the two-paw gesture.
        if(StoreProductId!=HomeStoreService.ScratchPostId||actor==null||scratchPoint==null||
            !actor.IsInteractionPoseClear(actor.transform.position,actor.transform.rotation))return false;
        float reach=geometry!=null?geometry.Reach:.235f;
        float shoulder=geometry!=null?geometry.ShoulderLocal.y*geometry.Scale:.286f;
        foreach(float lateral in Polished?new[]{-reach*.85f,reach*.85f,-reach*.51f,reach*.51f,-reach*1.19f,reach*1.19f}:new[]{-.20f,.20f,-.12f,.12f,-.28f,.28f})
        foreach(float height in Polished?new[]{shoulder+reach*.40f,shoulder+reach*.05f,shoulder+reach*.74f}:new[]{.38f,.30f,.46f})
        {
            if(!TryMeasureSurface(actor,height,lateral,out var point,out var hit))continue;
            var centre=point;centre.y=actor.transform.position.y;
            if(!CatActivityStartResolver.Facing(actor,centre,.70f,point,65f,out start))continue;
            bool left=lateral<0;
            Vector3 lower=point;RaycastHit lowerHit=hit;
            if(Polished&&!TryMeasureSurface(actor,height-reach*.14f,lateral,out lower,out lowerHit))continue;
            if(CatPawReachResolver.TryResolveSingleSweep(actor,point,lower,left,CatActivityPose.Scratch,out var plan,32,35))
            {start.PawPlan=plan;start.HasPawPlan=true;singleContactPoint=hit.point;singleEndPoint=lowerHit.point;return true;}
        }
        return false;
    }
    // The two curved contact lanes share one measured source-body pose.
    // Independently reachable arms do not imply a reachable two-paw stance.
    CatPawReachPlan pairedPlan;
    RaycastHit[] pairedTop=new RaycastHit[2],pairedBottom=new RaycastHit[2];
    readonly Vector3[] pairedRayOrigins=new Vector3[2];
    CatMovement pairedActor;
    Vector3 pairedPosition;
    Quaternion pairedRotation;
    Matrix4x4 pairedMatrix;
    bool pairCached;
    Transform pairedVisual;
    CatMeshContactSurface.TargetSet pairedSurface;
    readonly CatPawSurfacePlan[] pairedSkin=new CatPawSurfacePlan[2];
    Quaternion queryHeading;
    bool PrepareAlternating(CatMovement actor,out CatActivityStart prepared,Quaternion? heading=null)
    {
        prepared=default;
        queryHeading=heading??actor.transform.rotation;
        Vector3 right=queryHeading*Vector3.right;
        var body=GetComponentInChildren<MeshCollider>();
        if(body==null||!body.enabled)return false;
        var centre=body.bounds.center;
        if(!VisibleStance(actor,centre)){return false;}
        var flat=centre;flat.y=actor.transform.position.y;
        // Leave room for the muzzle's curved rise and return, not only its
        // standing endpoint. The measured chest and arm pose supplies reach.
        if(Vector3.Distance(actor.transform.position,flat)<geometry.StandDistance+.02f)return false;
        float range=geometry.StandDistance+geometry.Reach+Mathf.Min(body.bounds.extents.x,body.bounds.extents.z);
        if(!CatActivityStartResolver.Current(actor,flat,range,out prepared)&&
            !(prepared.PromptDistance<=range&&Mathf.Abs(actor.transform.position.y-flat.y)<=.15f&&StandingClear(actor,actor.transform.rotation)))return false;
        if(!heading.HasValue&&Vector3.Angle(actor.transform.forward,flat-actor.transform.position)>15f)return false;
        prepared.Rotation=queryHeading;prepared.ActionTarget=centre;prepared.Kind=CatActivityStartKind.Contact;
        using(CatPawReachResolver.BeginScratchSearch(actor))
        {
        if(pairCached&&pairedActor==actor&&pairedVisual==geometry.Visual&&pairedPosition==actor.transform.position&&pairedRotation==queryHeading&&pairedMatrix==transform.localToWorldMatrix)
        {
            bool valid=true;
            for(int hand=0;hand<2;hand++)
            {
                var top=pairedTop[hand];var bottom=pairedBottom[hand];
                valid&=FreshContact(actor,pairedRayOrigins[hand],top)&&
                    FreshContact(actor,pairedRayOrigins[hand]-Vector3.up*geometry.Reach*.22f,bottom);
            }
            if(valid&&ResolvePairedPlan(actor))
            {prepared.HasPawPlan=true;prepared.PawPlan=pairedPlan;prepared.ActionTarget=(pairedTop[0].point+pairedTop[1].point)*.5f;return true;}
        }
        pairCached=false;
        float shoulder=geometry.ShoulderLocal.y*geometry.Scale;
        float spread=Vector3.Distance(geometry.LeftHandLocal,geometry.RightHandLocal)*geometry.Scale;
        // Both hands use the same height and positive separation. Their lanes
        // stay on their own side instead of crossing to rescue a sideways cat.
        foreach(float h in new[]{.50f,.65f,.85f,1.10f,.36f,.18f,0f,-.12f,-.30f,-.50f})
        foreach(float side in new[]{.55f,.35f,.75f,1.15f})
        {
            bool valid=true;
            float height=shoulder+geometry.Reach*h;
            for(int hand=0;hand<2;hand++)
            {
                Vector3 from=actor.transform.position+Vector3.up*height+right*(hand==0?-1:1)*spread*side;
                Vector3 aim=centre+right*(hand==0?-1:1)*spread*(side-.35f)*.5f;aim.y=from.y;
                if(!MeasureRay(actor,from,aim,out var top)||
                    !MeasureRay(actor,from-Vector3.up*geometry.Reach*.22f,aim-Vector3.up*geometry.Reach*.22f,out var bottom)||
                    !VisibleContact(top)||!VisibleContact(bottom))
                {valid=false;break;}
                pairedTop[hand]=top;pairedBottom[hand]=bottom;pairedRayOrigins[hand]=from;
            }
            if(!valid||!ResolvePairedPlan(actor))continue;
            prepared.HasPawPlan=true;prepared.PawPlan=pairedPlan;
            prepared.ActionTarget=(pairedTop[0].point+pairedTop[1].point)*.5f;
            pairCached=true;pairedActor=actor;pairedVisual=geometry.Visual;pairedPosition=actor.transform.position;pairedRotation=queryHeading;pairedMatrix=transform.localToWorldMatrix;
            return true;
        }
        return false;
        }
    }
    CatMovement standingActor;
    SkinnedMeshRenderer standingRenderer;
    CatCareSkinClearance standingClearance;
    bool StandingClear(CatMovement actor,Quaternion heading)
    {
        if(actor.IsInteractionPoseClear(actor.transform.position,heading))return true;
        var renderer=actor.GetComponentInChildren<SkinnedMeshRenderer>();
        if(standingActor!=actor||standingRenderer!=renderer||standingClearance==null)
        {
            standingClearance=null;standingActor=actor;standingRenderer=renderer;
            var tag=actor.GetComponentInChildren<CatBreedVisualTag>();
            var profile=CatCareSkinCatalog.Load()?.Find(tag!=null?tag.BreedId:CatBreedService.SelectedBreedId);
            if(!CatCareWeightedSkin.TryBind(profile,actor.GetComponentInChildren<Animator>(),renderer,out var skin))return false;
            standingClearance=CatCareSkinClearance.Bind(skin,this,scratchPoint,actor);
        }
        if(standingClearance==null||!standingClearance.Available)return false;
        var delta=Matrix4x4.TRS(actor.transform.position,heading,Vector3.one)*
            Matrix4x4.TRS(actor.transform.position,actor.transform.rotation,Vector3.one).inverse;
        // Only the actual post meshes can refine its conservative body envelope.
        // Other solids and the controller retain the original physical checks.
        return standingClearance.IsClear(.0029f,delta)&&actor.IsCarePoseClear(actor.transform.position,heading,
            c=>c.transform.IsChildOf(transform)&&standingClearance.ContainsVerifiedMesh(c));
    }
    bool ClearTurn(CatMovement actor,Quaternion heading)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(Quaternion.Angle(actor.transform.rotation,heading)));
        for(int i=0;i<=steps;i++)
            if(!StandingClear(actor,Quaternion.Slerp(actor.transform.rotation,heading,(float)i/steps)))return false;
        return true;
    }
    bool FreshContact(CatMovement actor,Vector3 from,RaycastHit previous)
    {
        return previous.collider!=null&&previous.collider.enabled&&VisibleContact(previous)&&
            MeasureRay(actor,from,previous.point,out var current)&&current.collider==previous.collider&&
            Vector3.Distance(previous.point,current.point)<.001f;
    }
    bool ResolvePairedPlan(CatMovement actor)
    {
        float separation=Vector3.Distance(geometry.LeftHandLocal,geometry.RightHandLocal)*geometry.Scale*.15f;
        if(Vector3.Dot(pairedTop[1].point-pairedTop[0].point,queryHeading*Vector3.right)<=separation||
            Vector3.Dot(pairedBottom[1].point-pairedBottom[0].point,queryHeading*Vector3.right)<=separation)return false;
        float forward=geometry.ShoulderLocal.z*geometry.Scale+geometry.Reach*.02f;
        for(int hand=0;hand<2;hand++)
            if(Vector3.Dot(pairedTop[hand].point-actor.transform.position,queryHeading*Vector3.forward)<forward||
                Vector3.Dot(pairedBottom[hand].point-actor.transform.position,queryHeading*Vector3.forward)<forward)return false;
        return CatPawReachResolver.TryResolveScratchSweepAt(actor,queryHeading,
            pairedTop[0].point+pairedTop[0].normal*.005f,pairedBottom[0].point+pairedBottom[0].normal*.005f,
            pairedTop[1].point+pairedTop[1].normal*.005f,pairedBottom[1].point+pairedBottom[1].normal*.005f,
            out pairedPlan,Mathf.Max(.004f,geometry.Reach*.02f));
    }
    bool VisibleStance(CatMovement actor,Vector3 centre)
    {
        var camera=Camera.main;if(camera==null)return false;
        var towardCamera=Vector3.ProjectOnPlane(camera.transform.position-centre,Vector3.up).normalized;
        var side=Vector3.ProjectOnPlane(actor.transform.position-centre,Vector3.up).normalized;
        float viewAngle=Vector3.Dot(side,towardCamera);
        // Reject the hidden far side, but do not require a camera-specific
        // side or angle that makes the cat turn away from the board.
        if(viewAngle<.10f)return false;
        var view=camera.WorldToViewportPoint(actor.transform.position+Vector3.up*geometry.Height*.5f);
        return view.z>0&&view.x>.10f&&view.x<.90f&&view.y>.14f&&view.y<.88f;
    }
    bool VisibleContact(RaycastHit contactHit)
    {
        var camera=Camera.main;if(camera==null)return false;
        Vector3 target=contactHit.point+contactHit.normal*.003f;
        var screen=camera.WorldToViewportPoint(target);
        if(screen.z<=0||screen.x<.08f||screen.x>.92f||screen.y<.15f||screen.y>.88f)return false;
        Vector3 ray=target-camera.transform.position;
        // The actual board must not hide its own contact on the far side.
        return !contactHit.collider.Raycast(new Ray(camera.transform.position,ray.normalized),out var hit,ray.magnitude-.004f);
    }
    bool MeasureRay(CatMovement actor,Vector3 from,Vector3 aim,out RaycastHit measured)
    {
        measured=default;var hits=Physics.RaycastAll(from,(aim-from).normalized,(aim-from).magnitude+geometry.Reach*.2f,~0,QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(var hit in hits)
        {
            if(hit.collider.GetComponentInParent<CatMovement>()==actor)continue;
            if(!hit.transform.IsChildOf(transform))return false;
            measured=hit;return true;
        }
        return false;
    }
    bool TryPrepareSweep(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        if (actor == null || scratchPoint == null) return false;
        // The old scratchPoint is a standing marker, so facing it reverses
        // a cat that has already approached closer than that marker. Offer the
        // action against the actual front surface and the measured arm plan.
        if (!TrySurface(actor, geometry!=null?strokeHeight:.43f, 0f, out Vector3 surface)) return false;
        Vector3 centre = surface; centre.y = actor.transform.position.y;
        if (!CatActivityStartResolver.Facing(actor, centre, .70f,
            surface, 22f, out start)) return false;
        start.ActionTarget = surface;
        float spread = geometry!=null?strokeSpread:StrokeSpread;
        if (!TrySurface(actor, geometry!=null?strokeHeight+strokeAmplitude:.48f, -spread, out Vector3 leftA) ||
            !TrySurface(actor, geometry!=null?strokeHeight-strokeAmplitude:.34f, -spread, out Vector3 leftB) ||
            !TrySurface(actor, geometry!=null?strokeHeight+strokeAmplitude:.48f, spread, out Vector3 rightA) ||
            !TrySurface(actor, geometry!=null?strokeHeight-strokeAmplitude:.34f, spread, out Vector3 rightB)) return false;
        start.HasPawPlan = CatPawReachResolver.TryResolveSweep(actor, leftA, leftB, rightA, rightB,
            CatActivityPose.Scratch, out start.PawPlan);
        return start.HasPawPlan;
    }

    float StrokeSpread => HomeStoreService.IsFixedRoomProduct(StoreProductId) ? .025f : .045f;

    private bool TrySurface(CatMovement actor, float height, float lateral, out Vector3 surface)
        => TryMeasureSurface(actor,height,lateral,out surface,out _);

    private bool TryMeasureSurface(CatMovement actor, float height, float lateral, out Vector3 surface,out RaycastHit measured)
    {
        Vector3 from = actor.transform.position + Vector3.up * height + actor.transform.right * lateral;
        surface = from;measured=default;
        var hits = Physics.RaycastAll(from, actor.transform.forward, .70f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.GetComponentInParent<CatMovement>() == actor) continue;
            if (!hit.transform.IsChildOf(transform)) return false;
            measured=hit;
            surface = hit.point + hit.normal * .005f;
            return true;
        }
        return false;
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        failureReason = scratchPoint == null || !AcceptedStart.HasPawPlan ? "SCRATCH POST IS NOT READY" : string.Empty;
        return failureReason.Length == 0;
    }
    protected override bool BeginActivity()
    {
        leftHand=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-hand.L");
        rightHand=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-hand.R");
        if(leftHand==null||rightHand==null)return false;
        controller=Cat.GetComponent<CharacterController>();wasEnabled=controller!=null&&controller.enabled;
        start=Cat.transform.position;rotation=Cat.transform.rotation;captured=true;
        if(hangingToy!=null)toyRotation=hangingToy.localRotation;
        Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        contact=Cat.GetComponent<CatToyContactMotion>();if(contact==null)contact=Cat.gameObject.AddComponent<CatToyContactMotion>();
        contact.HoldWhenPaused=Polished;
        reachMotion=Cat.GetComponent<CatPawReachMotion>()??Cat.gameObject.AddComponent<CatPawReachMotion>();
        LeftStrokes=RightStrokes=0;
        RhythmVariation=rhythm++%2;
        if(Polished)gaze=Cat.GetComponent<CatFurnitureGaze>()??Cat.gameObject.AddComponent<CatFurnitureGaze>();
        LastLeftSurfaceDistance=LastRightSurfaceDistance=float.PositiveInfinity;
        alternateReady=false;
        if(!Polished&&!AcceptedStart.PawPlan.Both&&geometry!=null)
        {
            bool left=!AcceptedStart.PawPlan.Left;
            Vector3 pad=AcceptedStart.PawPlan.Target-singleContactPoint;
            alternateReady=CatPawReachResolver.TryResolveSingleSweep(Cat,AcceptedStart.PawPlan.Target,singleEndPoint+pad,left,CatActivityPose.Scratch,out alternatePlan,32,35);
            if(alternateReady){alternateSurface=singleContactPoint;alternateEnd=singleEndPoint;}
            float side=(left?-1:1)*geometry.Reach*.51f;
            foreach(float height in new[]{geometry.ShoulderLocal.y*geometry.Scale+geometry.Reach*.05f,geometry.ShoulderLocal.y*geometry.Scale+geometry.Reach*.40f})
            {
                if(alternateReady)break;
                if(!TryMeasureSurface(Cat,height,side,out var point,out var hit)||
                   !TryMeasureSurface(Cat,height-geometry.Reach*.14f,side,out var lower,out var lowerHit))continue;
                if(!CatPawReachResolver.TryResolveSingleSweep(Cat,point,lower,left,CatActivityPose.Scratch,out alternatePlan,32,35))continue;
                alternateReady=true;alternateSurface=hit.point;alternateEnd=lowerHit.point;break;
            }
        }
        StartCoroutine(Routine());return true;
    }
    IEnumerator Routine()
    {
        if(Polished)
        {
            var turn=CatActivityFacing.Turn(Cat,AcceptedStart.Rotation);
            while(IsRunning)
            {
                var before=Cat.transform.rotation;
                if(!turn.MoveNext())break;
                if(!ClearTurn(Cat,AcceptedStart.Rotation))
                {Cat.transform.rotation=before;(turn as System.IDisposable)?.Dispose();CancelForTransition();yield break;}
                yield return turn.Current;
            }
            // Recheck both real contact lanes after the turn, before raising paws.
            if(!PrepareAlternating(Cat,out _)){CancelForTransition();yield break;}
            yield return AlternatingRoutine();yield break;
        }
        if(Polished)
        {
            float notice=0;
            while(notice<.28f){gaze.LookAt(AcceptedStart.ActionTarget,Mathf.SmoothStep(0,1,notice/.28f),25);notice+=Time.deltaTime;yield return null;}
        }
        if(!AcceptedStart.PawPlan.Both)
        {yield return SinglePawRoutine();yield break;}
        float spread = geometry!=null?strokeSpread:StrokeSpread;
        float time=0;
        int lastLeft=-1,lastRight=-1;
        while(time<scratchDuration)
        {
            if(Time.timeScale<=0f){yield return null;continue;}
            Cat.transform.SetPositionAndRotation(AcceptedStart.Position, AcceptedStart.Rotation);
            time+=Time.deltaTime;
            float cycle=time/(Polished?(RhythmVariation==0?.70f:.83f):.65f);
            float leftPhase=Mathf.Repeat(cycle,1),rightPhase=Mathf.Repeat(cycle+.5f,1);
            // Query the actual near face at each stroke height, including
            // moulded doors. The accepted plan covers both ends of both hands.
            float middle=geometry!=null?strokeHeight:.41f,amplitude=geometry!=null?strokeAmplitude:.07f;
            float leftHeight=middle+amplitude*Mathf.Cos(leftPhase*Mathf.PI*2f);
            float rightHeight=middle+amplitude*Mathf.Cos(rightPhase*Mathf.PI*2f);
            if (!TryMeasureSurface(Cat, leftHeight, -spread, out Vector3 left,out var leftHit) ||
                !TryMeasureSurface(Cat, rightHeight, spread, out Vector3 right,out var rightHit))
            { CancelForTransition(); yield break; }
            var plan=AcceptedStart.PawPlan; plan.LeftTarget=left; plan.RightTarget=right;
            reachMotion.Sample(this,plan,leftPhase);
            if(hangingToy!=null)hangingToy.localRotation=toyRotation*Quaternion.Euler(0,0,Mathf.Sin(time*10)*9);
            // Source sampling, chest motion and both hands solve in LateUpdate.
            // Credit this frame's actual hands against the real hit points,
            // not last frame's IK distance or the 5mm padded IK targets.
            yield return new WaitForEndOfFrame();
            if(!IsRunning)yield break;
            float leftDistance=Vector3.Distance(leftHand.position,leftHit.point);
            float rightDistance=Vector3.Distance(rightHand.position,rightHit.point);
            if(Time.deltaTime>0f && leftPhase>.4f && leftDistance<StrokeContactTolerance && lastLeft!=(int)cycle)
            {LeftStrokes++;lastLeft=(int)cycle;LastLeftSurface=leftHit.point;LastLeftSurfaceDistance=leftDistance;}
            if(Time.deltaTime>0f && rightPhase>.4f && rightDistance<StrokeContactTolerance && lastRight!=(int)(cycle+.5f))
            {RightStrokes++;lastRight=(int)(cycle+.5f);LastRightSurface=rightHit.point;LastRightSurfaceDistance=rightDistance;}
            yield return null;
        }
        reachMotion.Clear();contact.Clear();
        if (LeftStrokes == 0 || RightStrokes == 0) { CancelForTransition(); yield break; }
        Restore(false);CompleteActivity("CLAWS FEEL GREAT!");
    }
    IEnumerator AlternatingRoutine()
    {
        // Root and rear support remain fixed. Source shoulder/elbow flexion,
        // measured chest bend and an outward recovery arc carry each gesture.
        Vector3 look=(pairedTop[0].point+pairedTop[1].point)*.5f;
        pairedSurface=new CatMeshContactSurface.TargetSet(transform);
        for(int hand=0;hand<2;hand++)
        {
            if(!pairedSurface.TryClosest(pairedTop[hand].point,out var hit)){CancelForTransition();yield break;}
            pairedSkin[hand]=geometry.PawPlan(hand==0,hit);
            if(pairedSkin[hand]==null){CancelForTransition();yield break;}
            // Readiness and animation solve the identical real skin patch.
            var patch=hand==0?pairedPlan.LeftPawVertices:pairedPlan.RightPawVertices;
            if(patch==null||patch.Length<3){CancelForTransition();yield break;}
            pairedSkin[hand].Vertices=patch;
            pairedSkin[hand].ReturnToStart=true;
            pairedSkin[hand].ConformScratchSupport=true;
            pairedSkin[hand].ScratchClearance=.001f;
            pairedSkin[hand].ApproachLift=geometry.Reach*.28f;
            pairedSkin[hand].Collider=pairedTop[hand].collider;
            // Rope ridges have steep up/down bevel normals. A claw retracts
            // away from the shaft, never along a single decorative ridge.
            Vector3 outward=Vector3.ProjectOnPlane(pairedTop[hand].normal,Vector3.up).normalized;
            pairedSkin[hand].LocalNormal=hit.Filter.transform.InverseTransformDirection(outward);
            pairedSkin[hand].LocalApproachNormal=pairedSkin[hand].LocalNormal;
        }
        // Raise once, scratch with both arms supported, then settle once.
        // Do not replay a full rise/fold animation independently for each paw.
        var bodyPlan=pairedPlan;bodyPlan.Surface=pairedSkin[0];
        for(float t=0;t<.8f;)
        {
            if(Time.timeScale<=0f){yield return null;continue;}
            float blend=Mathf.SmoothStep(0,1,t/.8f);
            reachMotion.Sample(this,bodyPlan,.35f*(t/.8f),false);
            float lift=Mathf.Sin(Mathf.PI*blend)*geometry.Reach*.12f;
            for(int hand=0;hand<2;hand++)ScratchTarget(hand,0,lift,blend);
            gaze.LookAt(look,blend*.65f,22);yield return new WaitForEndOfFrame();
            t+=Time.deltaTime;
        }
        for(int stroke=0;stroke<4;stroke++)
        {
            int hand=stroke%2;bool touched=false;
            float seconds=RhythmVariation==0?(hand==0?.78f:.91f):(stroke==3?.82f:.59f);
            for(float t=0;t<seconds;)
            {
                if(Time.timeScale<=0){yield return null;continue;}
                float phase=Mathf.Clamp01(t/seconds);ActiveScratchHand=hand;ScratchPhase=phase;
                float rake=(phase<=.68f?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.36f,.66f,phase)):
                    1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1,phase)))*(RhythmVariation==0?.70f:1f);
                float lift=phase>.68f?Mathf.Sin((phase-.68f)/.32f*Mathf.PI)*geometry.Reach*.05f:0;
                reachMotion.Sample(this,bodyPlan,.5f,false);
                for(int side=0;side<2;side++)ScratchTarget(side,side==hand?rake:0,side==hand?lift:0,1);
                gaze.LookAt(look,.65f,22);yield return new WaitForEndOfFrame();
                Vector3 point=hand==0?contact.LeftContactPosition:contact.RightContactPosition;
                if(pairedSurface.TryClosest(point,out var hit))
                {
                    float distance=Vector3.Distance(point,hit.Point);
                    if(phase>=.4f&&phase<=.65f&&distance<StrokeContactTolerance)
                    {touched=true;if(hand==0){LastLeftSurface=hit.Point;LastLeftSurfaceDistance=distance;}else{LastRightSurface=hit.Point;LastRightSurfaceDistance=distance;}}
                }
                t+=Time.deltaTime;
            }
            if(!touched){CancelForTransition();yield break;}
            if(hand==0)LeftStrokes++;else RightStrokes++;
        }
        ActiveScratchHand=-1;
        for(float t=0;t<.8f;)
        {
            if(Time.timeScale<=0f){yield return null;continue;}
            float blend=1-Mathf.SmoothStep(0,1,t/.8f);
            reachMotion.Sample(this,bodyPlan,.68f+.32f*t/.8f,false);
            float lift=Mathf.Sin(Mathf.PI*blend)*geometry.Reach*.12f;
            for(int hand=0;hand<2;hand++)ScratchTarget(hand,0,lift,blend);
            gaze.LookAt(look,.65f*blend,22);yield return new WaitForEndOfFrame();
            t+=Time.deltaTime;
        }
        // Render the exact standing endpoint before releasing the sampled
        // pose. A loop ending just below one must not leave a final IK offset.
        while(Time.timeScale<=0f)yield return null;
        reachMotion.Sample(this,bodyPlan,1f,false);
        for(int hand=0;hand<2;hand++)ScratchTarget(hand,0,0,0);
        gaze.LookAt(look,0,22);yield return new WaitForEndOfFrame();
        while(Time.timeScale<=0f)yield return null;
        reachMotion.Clear();contact.Clear();Restore(false);CompleteActivity("CLAWS FEEL GREAT!");
    }
    void ScratchTarget(int hand,float rake,float lift,float weight)
    {
        Vector3 surface=Vector3.Lerp(pairedTop[hand].point,pairedBottom[hand].point,rake);
        Vector3 normal=Vector3.ProjectOnPlane(pairedTop[hand].normal,Vector3.up).normalized;
        if(pairedTop[hand].collider.Raycast(new Ray(surface+normal*geometry.Reach,-normal),out var hit,geometry.Reach*2f))surface=hit.point;
        var skin=pairedSkin[hand];skin.LocalPoint=skin.TargetTransform.InverseTransformPoint(surface+normal*lift);
        skin.LocalNormal=skin.TargetTransform.InverseTransformDirection(normal);skin.ApproachLift=lift;
        contact.ReachSurface(skin,weight);
    }
    IEnumerator SinglePawRoutine()
    {
        var surface=singleContactPoint;
        for(int stroke=0;stroke<3;stroke++)
        {
            bool alternate=alternateReady&&stroke==1;
            var plan=alternate?alternatePlan:AcceptedStart.PawPlan;
            var from=alternate?alternateSurface:singleContactPoint;
            var to=alternate?alternateEnd:singleEndPoint;
            float drag=Vector3.Distance(from,to);
            float elapsed=0;bool touched=false;
            float seconds=Polished?(RhythmVariation==0?(stroke==1?.92f:.69f):(stroke==2?.96f:.76f)):.80f;
            while(elapsed<seconds)
            {
                if(Time.timeScale<=0){yield return null;continue;}
                Cat.transform.SetPositionAndRotation(AcceptedStart.Position,AcceptedStart.Rotation);
                float phase=Mathf.Clamp01(elapsed/seconds);
                // A short downward rake during the planted part of the gesture,
                // then a folded recovery. Both endpoints passed one source-body plan.
                float rake=Polished?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.65f,phase)):0;
                surface=Vector3.Lerp(from,to,rake);
                var framePlan=plan;
                Vector3 target=plan.Target+Vector3.down*(drag*rake);
                if(plan.Left)framePlan.LeftTarget=target;else framePlan.RightTarget=target;
                reachMotion.Sample(this,framePlan,phase);
                if(gaze!=null)gaze.LookAt(surface,.55f,18);
                if(hangingToy!=null)hangingToy.localRotation=toyRotation*Quaternion.Euler(0,0,Mathf.Sin((stroke*.8f+elapsed)*10)*9);
                yield return new WaitForEndOfFrame();
                float distance=Vector3.Distance((plan.Left?leftHand:rightHand).position,surface);
                if(phase>=.4f&&phase<=.68f&&distance<StrokeContactTolerance)
                {
                    touched=true;
                    if(plan.Left){LastLeftSurface=surface;LastLeftSurfaceDistance=distance;}
                    else{LastRightSurface=surface;LastRightSurfaceDistance=distance;}
                }
                elapsed+=Time.deltaTime;
            }
            if(!touched){CancelForTransition();yield break;}
            if(plan.Left)LeftStrokes++;else RightStrokes++;
        }
        reachMotion.Clear();contact.Clear();
        if(Polished){float settle=0;while(settle<.22f){gaze.LookAt(surface,1-settle/.22f,18);settle+=Time.deltaTime;yield return null;}}
        Restore(false);CompleteActivity("CLAWS FEEL GREAT!");
    }
    void Restore(bool cancel)
    {
        ActiveScratchHand=-1;ScratchPhase=0;
        if(!captured)return;captured=false;
        reachMotion?.Clear();gaze?.Clear();
        if(contact!=null)contact.Clear();
        if(hangingToy!=null)hangingToy.localRotation=toyRotation;
        if(Cat!=null)Cat.SetMovementLocked(this,false);
        if(controller!=null)controller.enabled=wasEnabled;
    }
    protected override void CancelActivity(){if(!IsRunning)return;StopAllCoroutines();if(!HasBegunActivity){base.CancelActivity();return;}Restore(true);base.CancelActivity();}
#if UNITY_EDITOR
    public void EditorConfigureScratch(Transform point,float duration){scratchPoint=point;scratchDuration=Mathf.Max(.5f,duration);}
    public void EditorConfigureToy(Transform toy){hangingToy=toy;}
    public void EditorConfigureRope(Vector3 center,float radius){ropeCenter=center;ropeRadius=radius;}
#endif
}
