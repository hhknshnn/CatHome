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
    Vector3 start,singleContactPoint;
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
        if(TryPrepareSweep(actor,out start))return true;
        // A short cat cannot put both paws up the shaft from a clear standing
        // pose. It can scratch from beside it with the nearer paw, using the
        // same source-body and limb-length checks as the two-paw gesture.
        if(StoreProductId!=HomeStoreService.ScratchPostId||actor==null||scratchPoint==null||
            !actor.IsInteractionPoseClear(actor.transform.position,actor.transform.rotation))return false;
        foreach(float lateral in new[]{-.20f,.20f,-.12f,.12f,-.28f,.28f})
        foreach(float height in new[]{.38f,.30f,.46f})
        {
            if(!TryMeasureSurface(actor,height,lateral,out var point,out var hit))continue;
            var centre=point;centre.y=actor.transform.position.y;
            if(!CatActivityStartResolver.Facing(actor,centre,.70f,point,65f,out start))continue;
            bool left=lateral<0;
            if(CatPawReachResolver.TryResolve(actor,point,left,CatActivityPose.Scratch,out var plan,32,35))
            {start.PawPlan=plan;start.HasPawPlan=true;singleContactPoint=hit.point;return true;}
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
        if (!TrySurface(actor, .43f, 0f, out Vector3 surface)) return false;
        Vector3 centre = surface; centre.y = actor.transform.position.y;
        if (!CatActivityStartResolver.Facing(actor, centre, .70f,
            surface, 22f, out start)) return false;
        start.ActionTarget = surface;
        float spread = StrokeSpread;
        if (!TrySurface(actor, .48f, -spread, out Vector3 leftA) ||
            !TrySurface(actor, .34f, -spread, out Vector3 leftB) ||
            !TrySurface(actor, .48f, spread, out Vector3 rightA) ||
            !TrySurface(actor, .34f, spread, out Vector3 rightB)) return false;
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
        reachMotion=Cat.GetComponent<CatPawReachMotion>()??Cat.gameObject.AddComponent<CatPawReachMotion>();
        LeftStrokes=RightStrokes=0;
        LastLeftSurfaceDistance=LastRightSurfaceDistance=float.PositiveInfinity;
        StartCoroutine(Routine());return true;
    }
    IEnumerator Routine()
    {
        if(!AcceptedStart.PawPlan.Both)
        {yield return SinglePawRoutine();yield break;}
        float spread = StrokeSpread;
        float time=0;
        int lastLeft=-1,lastRight=-1;
        while(time<scratchDuration)
        {
            if(Time.timeScale<=0f){yield return null;continue;}
            Cat.transform.SetPositionAndRotation(AcceptedStart.Position, AcceptedStart.Rotation);
            time+=Time.deltaTime;
            float cycle=time/.65f;
            float leftPhase=Mathf.Repeat(cycle,1),rightPhase=Mathf.Repeat(cycle+.5f,1);
            // Query the actual near face at each stroke height, including
            // moulded doors. The accepted plan covers both ends of both hands.
            float leftHeight=.41f+.07f*Mathf.Cos(leftPhase*Mathf.PI*2f);
            float rightHeight=.41f+.07f*Mathf.Cos(rightPhase*Mathf.PI*2f);
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
    IEnumerator SinglePawRoutine()
    {
        var plan=AcceptedStart.PawPlan;
        var surface=singleContactPoint;
        for(int stroke=0;stroke<3;stroke++)
        {
            float elapsed=0;bool touched=false;
            while(elapsed<.80f)
            {
                if(Time.timeScale<=0){yield return null;continue;}
                Cat.transform.SetPositionAndRotation(AcceptedStart.Position,AcceptedStart.Rotation);
                float phase=Mathf.Clamp01(elapsed/.80f);reachMotion.Sample(this,plan,phase);
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
        reachMotion.Clear();contact.Clear();Restore(false);CompleteActivity("CLAWS FEEL GREAT!");
    }
    void Restore(bool cancel)
    {
        if(!captured)return;captured=false;
        reachMotion?.Clear();
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
