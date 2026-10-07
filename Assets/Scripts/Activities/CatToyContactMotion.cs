using UnityEngine;

/// <summary>Reversible two-arm contact correction, preserving every breed's bone lengths.</summary>
[DefaultExecutionOrder(600)]
public sealed class CatToyContactMotion : MonoBehaviour
{
    sealed class Limb
    {
        public Transform arm, fore, hand;
        public Quaternion armPose, forePose;
        public Quaternion heldArm,heldFore;
        public Vector3 sourceElbowPole,sourceWristAxis;public float sourceElbowAngle;
        public bool scratchSourceCaptured,hasScratchPoleTurn;
        public float scratchPoleTurn;
        public bool adjusted,requested;
        public Vector3 target,contactPosition;
        public float weight;
        public int iterations=8;
        public float distance=float.PositiveInfinity;
        public CatPawSurfacePlan surface;
        public CatPawSurfaceBinding skin;
    }
    readonly Limb[] limbs={new Limb(),new Limb()};
    public Vector3 LastContactPosition {get;private set;}
    public float Distance {get;private set;}=float.PositiveInfinity;
    public bool HoldWhenPaused {get;set;}
    public float LeftDistance=>limbs[0].distance;
    public float RightDistance=>limbs[1].distance;
    public Vector3 LeftContactPosition=>limbs[0].contactPosition;
    public Vector3 RightContactPosition=>limbs[1].contactPosition;
    public void Reach(Vector3 point,bool left,float phase,int iterations=8)
    {
        Set(left?0:1,point,Mathf.SmoothStep(0,1,phase<.42f?phase/.42f:(1-phase)/.58f),iterations);
    }
    public void ReachBoth(Vector3 left,Vector3 right,float weight=1f,int iterations=8)
    {Set(0,left,weight,iterations);Set(1,right,weight,iterations);}
    public void ReachWeighted(Vector3 point,bool left,float weight)
    {Set(left?0:1,point,weight);}
    // Opt-in actual-skin endpoint. Existing Reach/ReachBoth remain wrist-pivot APIs.
    public void ReachSurface(CatPawSurfacePlan surface,float weight)
    {
        if(surface==null||!surface.IsValid)return;
        int index=surface.Left?0:1;var limb=limbs[index];
        bool bind=limb.surface!=surface||limb.skin==null;
        Set(index,surface.Point,weight,CatPawSurfaceCcd.Iterations);
        limb.surface=surface;
        if(bind)
        {
            var animator=GetComponentInChildren<Animator>();
            limb.skin=new CatPawSurfaceBinding(surface,animator!=null?animator.transform:null);
            limb.scratchSourceCaptured=limb.hasScratchPoleTurn=false;
        }
        if(limb.skin==null||!limb.skin.IsValid)limb.requested=false;
    }
    void Set(int index,Vector3 point,float weight,int iterations=8)
    {
        var limb=limbs[index];limb.surface=null;string side=index==0?"L":"R";
        if(limb.arm==null)
            foreach(var t in GetComponentsInChildren<Transform>())
            {
                if(t.name=="DEF-upper_arm."+side)limb.arm=t;
                if(t.name=="DEF-forearm."+side)limb.fore=t;
                if(t.name=="DEF-hand."+side)limb.hand=t;
            }
        limb.target=point;limb.weight=Mathf.Clamp01(weight);limb.iterations=Mathf.Clamp(iterations,1,32);limb.requested=true;
    }
    void Update(){Restore();}
    void LateUpdate()
    {
        foreach(var limb in limbs)
        {
            bool holdPaused=HoldWhenPaused&&Time.timeScale<=0f;
            if((!limb.requested&&!holdPaused) || limb.arm==null || limb.fore==null || limb.hand==null)continue;
            limb.requested=false;limb.armPose=limb.arm.localRotation;limb.forePose=limb.fore.localRotation;limb.adjusted=true;
            if(limb.surface!=null)
            {
                if(!limb.surface.IsValid||limb.skin==null||!limb.skin.IsValid){limb.distance=float.PositiveInfinity;Distance=limb.distance;continue;}
                limb.target=limb.surface.Point;
                if(limb.surface.ConformScratchSupport)
                {
                    if(holdPaused){limb.arm.localRotation=limb.heldArm;limb.fore.localRotation=limb.heldFore;continue;}
                    SolveScratch(limb);
                    continue;
                }
                Vector3 skinGoal=CatPawSurfaceCcd.Goal(limb.skin.Point(),limb.target,limb.weight,
                    limb.surface.ApproachNormal,limb.surface.ApproachLift,limb.surface.LimitLiftToRemainingRise);
                for(int i=0;i<CatPawSurfaceCcd.Iterations;i++)
                {
                    limb.fore.rotation=CatPawSurfaceCcd.Delta(limb.fore.position,limb.skin.Point(),skinGoal)*limb.fore.rotation;
                    limb.arm.rotation=CatPawSurfaceCcd.Delta(limb.arm.position,limb.skin.Point(),skinGoal)*limb.arm.rotation;
                }
                LastContactPosition=limb.skin.Point();
                if(limb.surface.ConformScratchSupport)
                {
                    // Correct the skin's leading edge, not an averaged patch hidden
                    // inside the paw. Blend the correction with the source reach.
                    Vector3 normal=limb.surface.Normal;
                    float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.70f,1f,limb.weight));
                    float rayLength=Vector3.Distance(limb.arm.position,limb.fore.position)+Vector3.Distance(limb.fore.position,limb.hand.position);
                    bool seated=blend>=.999f;
                    // Select the leading skin once per frame, then keep that
                    // endpoint fixed throughout the joint iterations.
                    limb.skin.ResetBoardSupport();
                    for(int pass=0;pass<24;pass++)
                    {
                        Vector3 edge=seated?limb.skin.BoardSupportPoint(normal,rayLength):limb.skin.SupportPoint(limb.target,normal);
                        if(limb.surface.Collider==null||!limb.surface.Collider.Raycast(new Ray(edge+normal*rayLength,-normal),out var board,rayLength*2))break;
                        Vector3 supportGoal=Vector3.Lerp(edge,limb.target+normal*limb.surface.ScratchClearance,blend);
                        limb.fore.rotation=CatPawSurfaceCcd.Delta(limb.fore.position,edge,supportGoal)*limb.fore.rotation;
                        edge=seated?limb.skin.BoardSupportPoint(normal,rayLength):limb.skin.SupportPoint(limb.target,normal);
                        if(!limb.surface.Collider.Raycast(new Ray(edge+normal*rayLength,-normal),out board,rayLength*2))break;
                        supportGoal=Vector3.Lerp(edge,limb.target+normal*limb.surface.ScratchClearance,blend);
                        limb.arm.rotation=CatPawSurfaceCcd.Delta(limb.arm.position,edge,supportGoal)*limb.arm.rotation;
                    }
                    LastContactPosition=seated?limb.skin.BoardSupportPoint(normal,rayLength):limb.skin.SupportPoint(limb.target,normal);
                }
                limb.contactPosition=LastContactPosition;
                limb.distance=Vector3.Distance(LastContactPosition,limb.target);Distance=limb.distance;
                continue;
            }
            Vector3 goal=Vector3.Lerp(limb.hand.position,limb.target,limb.weight);
            for(int i=0;i<limb.iterations;i++){Aim(limb.fore,limb.hand,goal);Aim(limb.arm,limb.hand,goal);}
            limb.distance=Vector3.Distance(limb.hand.position,limb.target);Distance=limb.distance;LastContactPosition=limb.hand.position;limb.contactPosition=LastContactPosition;
        }
    }
    static void Aim(Transform joint,Transform hand,Vector3 goal)
    {
        Vector3 from=hand.position-joint.position,to=goal-joint.position;
        if(from.sqrMagnitude<.00001f || to.sqrMagnitude<.00001f)return;
        joint.rotation=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(from,to),20)*joint.rotation;
    }
    // Scratch uses one anatomical elbow plane, not successive unconstrained
    // forearm/shoulder rotations. The source wrist's local pose stays intact.
    void SolveScratch(Limb limb)
    {
        Vector3 normal=limb.surface.Normal;
        float length=Vector3.Distance(limb.arm.position,limb.fore.position)+Vector3.Distance(limb.fore.position,limb.hand.position);
        if(!limb.scratchSourceCaptured)
        {
            limb.sourceWristAxis=(limb.hand.position-limb.arm.position).normalized;
            limb.sourceElbowPole=Vector3.ProjectOnPlane(limb.fore.position-limb.arm.position,limb.sourceWristAxis);
            if(limb.sourceElbowPole.sqrMagnitude<.00000001f)
                limb.sourceElbowPole=Vector3.ProjectOnPlane(-transform.forward*.65f-Vector3.up,limb.sourceWristAxis);
            limb.sourceElbowPole.Normalize();limb.scratchSourceCaptured=true;
        }
        limb.sourceElbowAngle=Vector3.Angle(limb.arm.position-limb.fore.position,limb.hand.position-limb.fore.position);
        limb.skin.ResetBoardSupport();
        // Follow one anatomical patch throughout the gesture. Re-selecting a
        // different leading toe each frame also changes its tangential target.
        bool seated=limb.weight>0f;
        Vector3 edge=limb.skin.Point();
        Vector3 goal=Vector3.Lerp(edge,limb.target+normal*limb.surface.ScratchClearance,limb.weight);
        for(int pass=0;seated&&pass<CatPawReachResolver.ScratchSolveIterations;pass++)
        {
            edge=limb.skin.Point();
            Vector3 correction=goal-edge;
            // Long distal toes can overshoot their surface point when every
            // correction is applied at once. Match the measured reach plan.
            SolveElbow(limb,limb.hand.position+correction*CatPawReachResolver.ScratchCorrectionDamping);
        }
        if(seated)
            for(int pass=0;pass<8;pass++)
            {
                // Rank again in the solved pose. Rope grooves can change which
                // toe is leading; correct only normal distance, never drag a
                // newly selected toe sideways to the authored point.
                limb.skin.ResetBoardSupport();edge=limb.skin.BoardSupportPoint(normal,length);
                if(!limb.surface.Collider.Raycast(new Ray(edge+normal*length,-normal),out var board,length*2))break;
                float gap=Vector3.Dot(edge-board.point,normal);
                float correction=(limb.surface.ScratchClearance+limb.surface.ApproachLift-gap)*limb.weight;
                // At full contact also close a remaining air gap. Damping
                // avoids overshooting when a rope ridge changes the leading
                // toe; the approach still only corrects outward penetration.
                if(correction<0)correction*=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.85f,1f,limb.weight));
                if(Mathf.Abs(correction)<.00002f)break;
                SolveElbow(limb,limb.hand.position+normal*(correction*.7f));
            }
        // Weight the endpoint and elbow plane, rather than blending the solved
        // rotations back through the shaft after checking skin clearance.
        limb.heldArm=limb.arm.localRotation;limb.heldFore=limb.fore.localRotation;
        limb.skin.ResetBoardSupport();
        LastContactPosition=seated?limb.skin.BoardSupportPoint(normal,length):limb.skin.SupportPoint(limb.target,normal);
        limb.contactPosition=LastContactPosition;
        limb.distance=Vector3.Distance(LastContactPosition,limb.target);Distance=limb.distance;
    }
    void SolveElbow(Limb limb,Vector3 wristGoal)
    {
        Vector3 shoulder=limb.arm.position;
        float upper=Vector3.Distance(shoulder,limb.fore.position),lower=Vector3.Distance(limb.fore.position,limb.hand.position);
        Vector3 delta=wristGoal-shoulder;float distance=delta.magnitude;
        if(distance<.0001f||upper<.0001f||lower<.0001f)return;
        Vector3 axis=delta/distance;
        // Internal elbow angle remains 40..165 degrees and always bends down
        // and toward the body. Neither a straight singularity nor a pole flip
        // is accepted merely to hit the surface more closely.
        float low=Mathf.Lerp(Mathf.Min(40,limb.sourceElbowAngle),40,limb.weight);
        float high=Mathf.Lerp(Mathf.Max(165,limb.sourceElbowAngle),165,limb.weight);
        float min=Mathf.Sqrt(upper*upper+lower*lower-2*upper*lower*Mathf.Cos(low*Mathf.Deg2Rad));
        float max=Mathf.Sqrt(upper*upper+lower*lower-2*upper*lower*Mathf.Cos(high*Mathf.Deg2Rad));
        distance=Mathf.Clamp(distance,min,max);wristGoal=shoulder+axis*distance;
        Vector3 pole=Vector3.ProjectOnPlane(-transform.forward*.65f-Vector3.up,axis);
        if(pole.sqrMagnitude<.0001f)pole=Vector3.ProjectOnPlane(-transform.forward,axis);
        pole.Normalize();
        // Transport the captured neutral bend plane; a newly normalized pole
        // from a nearly straight animated arm can flip during rise or settle.
        Vector3 sourcePole=Quaternion.FromToRotation(limb.sourceWristAxis,axis)*limb.sourceElbowPole;
        sourcePole=Vector3.ProjectOnPlane(sourcePole,axis).normalized;
        if(sourcePole.sqrMagnitude>.001f)
        {
            float turn=Vector3.SignedAngle(sourcePole,pole,axis);
            if(limb.hasScratchPoleTurn)turn=limb.scratchPoleTurn+Mathf.DeltaAngle(limb.scratchPoleTurn,turn);
            limb.scratchPoleTurn=turn;limb.hasScratchPoleTurn=true;
            pole=Quaternion.AngleAxis(turn*limb.weight,axis)*sourcePole;
        }
        float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
        Vector3 elbow=shoulder+axis*along+pole*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        limb.arm.rotation=Quaternion.FromToRotation(limb.fore.position-shoulder,elbow-shoulder)*limb.arm.rotation;
        limb.fore.rotation=Quaternion.FromToRotation(limb.hand.position-limb.fore.position,wristGoal-limb.fore.position)*limb.fore.rotation;
    }
    void Restore()
    {
        foreach(var limb in limbs)
        {
            if(!limb.adjusted)continue;limb.adjusted=false;
            if(limb.arm!=null)limb.arm.localRotation=limb.armPose;
            if(limb.fore!=null)limb.fore.localRotation=limb.forePose;
        }
    }
    public void Clear()
    {
        HoldWhenPaused=false;
        Restore();foreach(var limb in limbs){limb.requested=false;limb.arm=null;limb.fore=null;limb.hand=null;limb.surface=null;limb.skin=null;limb.scratchSourceCaptured=limb.hasScratchPoleTurn=false;limb.distance=float.PositiveInfinity;}
        Distance=float.PositiveInfinity;
    }
    void OnDisable(){Clear();}
}

