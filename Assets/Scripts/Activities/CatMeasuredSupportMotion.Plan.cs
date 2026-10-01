using System;
using UnityEngine;

public sealed partial class CatMeasuredSupportMotion
{
    readonly Vector3[] planTargets=new Vector3[4],planLower=new Vector3[4],planSoles=new Vector3[4];
    readonly bool[] planPlanted=new bool[4],planSolve=new bool[4];
    readonly Vector3[] planBaseTargets=new Vector3[4];
    readonly float[] planPawRaises=new float[4];
    readonly float[] planStepLimits=new float[4];
    readonly Vector4[] pawSettleDiagnostics=new Vector4[4];
    public Vector4 NumericPawSettleAt(int leg)=>pawSettleDiagnostics[leg];
    public int NumericPlanLimbTrials { get; private set; }
    public int NumericPlanLimbAccepted { get; private set; }
    readonly CatSupportedLimbSkin.Pose[] planPoses=new CatSupportedLimbSkin.Pose[4];
    readonly Quaternion[] planFootOrientation=new Quaternion[4];
    Vector3[] planPoints=Array.Empty<Vector3>();
    Quaternion planRotation=Quaternion.identity;
    Vector3 planPivot;
    bool previousRestPlan;
    float previousRestLift;
    Quaternion previousRestRotation;
    Vector3 previousRestPivot;
    readonly Vector3[] previousRestTargetOffsets=new Vector3[4];
    readonly bool[] previousRestPlanted=new bool[4];
    float planLift,planOwnerDepth;
    int planWitness=-1;
    Bounds planBounds;
    public struct NumericSupportAttempt
    {
        public float lift,tiltDegrees,depth,reachMargin;
        public int leg,sourceVertex;
        public Vector3 worldPoint;
        public string diagnosticCode;
        public Collider blocker;
    }
    readonly NumericSupportAttempt[] numericAttempts=new NumericSupportAttempt[32];
    public int NumericPlanAttemptCount { get; private set; }
    public NumericSupportAttempt NumericPlanAttemptAt(int index)=>numericAttempts[index];
    public string NumericPlanReason { get; private set; }
    public float NumericPlanRawDepth { get; private set; }
    public int NumericPlanRawWitness { get; private set; }
    public int NumericPlanPlantedCount { get; private set; }
    public float NumericPlanLegacyLiftCap { get; private set; }
    public float NumericPlanReachLiftCap { get; private set; }
    public Vector3 NumericPlanTiltPivot { get; private set; }
    public Vector3 NumericPlanTiltAxis { get; private set; }
    public Vector3 NumericPlanFootTarget(int leg)=>planTargets[leg];
    public bool NumericPlanFootPlanted(int leg)=>planPlanted[leg];

    // The gate and live support frame use this exact numeric selection. False
    // means missing source/owner geometry; a supplied but blocked source
    // returns true with clear=false. No actor, visual or bone is assigned.
    public bool TryPredictNativePreparation(CatSupportedLimbSkin source,Vector3 supportPosition,Quaternion heading,out bool clear)
    {
        clear=false;
        if(source==null||owner==null||!owner.IsRunning||movement==null||!HasMeasuredOwnerGeometry())return false;
        Vector3 oldUp=up;float oldReach=reach,oldReference=reference;
        bool oldBackfaces=Physics.queriesHitBackfaces;
        try
        {
            Physics.queriesHitBackfaces=true;
            topCache.Clear();ownerDepthCache.Clear();environmentSynchronized=false;
            // The owner can remain still while an unrelated scene obstacle
            // moves. A pure query owns its fresh physics synchronization.
            Physics.SyncTransforms();environmentSynchronized=true;
            up=authored!=null?authored.up.normalized:Vector3.up;
            reference=Vector3.Dot(supportPosition,up);reach=0;
            for(int i=0;i<4;i++)reach=Mathf.Max(reach,Vector3.Distance(source.Upper(i),source.Lower(i))+Vector3.Distance(source.Lower(i),source.Foot(i)));
            CaptureOwnerGeometry();
            clear=TryNumericSupportPlan(source,supportPosition,heading,owner is TubEdgeWalkActivity,false,
                !(owner is TubEdgeWalkActivity)&&CatActivityStartResolver.LandsOnSupport(movement,supportPosition));return true;
        }
        finally{up=oldUp;reach=oldReach;reference=oldReference;Physics.queriesHitBackfaces=oldBackfaces;}
    }

    bool TryNumericSupportPlan(CatSupportedLimbSkin source,Vector3 supportPosition,Quaternion heading,bool allowTilt,bool fromLive=false,bool conformRest=false)
    {
        NumericPlanAttemptCount=NumericPlanPlantedCount=0;NumericPlanReason="Evaluating";
        NumericPlanLimbTrials=NumericPlanLimbAccepted=0;
        NumericPlanRawDepth=NumericPlanLegacyLiftCap=NumericPlanReachLiftCap=0;NumericPlanRawWitness=-1;
        NumericPlanTiltPivot=NumericPlanTiltAxis=Vector3.zero;
        if(source==null||!HasMeasuredOwnerGeometry()){NumericPlanReason="MissingSourceOrOwnerGeometry";return false;}
        float minimumLift=fromLive?VisualLift:0;
        if(fromLive)
            for(int i=0;i<4;i++)
            {
                if(legs[i].solve)
                {if(!source.TryPose(i,legs[i].target,legs[i].desiredLower,up*minimumLift,out planPoses[i]))return false;}
                else planPoses[i]=source.SourcePose(i,up*minimumLift);
            }
        topCache.Clear();
        if(planPoints.Length!=source.VertexCount)planPoints=new Vector3[source.VertexCount];
        for(int i=0;i<4;i++)
        {
            planTargets[i]=source.Foot(i);planSoles[i]=source.Foot(i)+up*100f;planPlanted[i]=false;planPawRaises[i]=0;
            pawSettleDiagnostics[i]=Vector4.zero;
            planStepLimits[i]=Vector3.Distance(source.Lower(i),source.Foot(i))*.35f;
        }
        planWitness=-1;float rawDepth=0,maxLift=float.PositiveInfinity;
        for(int i=0;i<4;i++)maxLift=Mathf.Min(maxLift,Vector3.Distance(source.Lower(i),source.Foot(i))*.35f);
        NumericPlanLegacyLiftCap=maxLift;
        for(int slot=0;slot<source.VertexCount;slot++)
        {
            Vector3 point=source.SourcePoint(slot);planPoints[slot]=point;
            int index=source.SourceIndex(slot);if(index<0||index>=paw.Length){NumericPlanReason="InvalidSourceVertex";return false;}
            int leg=paw[index];if(leg>=0&&Vector3.Dot(point-planSoles[leg],up)<0)planSoles[leg]=point;
            if(conformRest&&leg>=0)
                planStepLimits[leg]=Mathf.Max(planStepLimits[leg],Mathf.Min(reach*.35f,
                    Vector3.ProjectOnPlane(point-source.Foot(leg),up).magnitude));
            Vector3 measured=fromLive?source.CombinedPoint(slot,planPoses,up*minimumLift):point;
            float depth=OwnerDepth(measured);if(float.IsInfinity(depth)){NumericPlanReason="MissingOriginalOwnerTriangle";return false;}
            if(leg>=0&&depth>.002f&&TryTop(point,out var pawTop))
                planPawRaises[leg]=Mathf.Max(planPawRaises[leg],Vector3.Dot(pawTop.point-point,up)+.008f);
            if(depth>rawDepth){rawDepth=depth;planWitness=slot;}
        }
        NumericPlanRawDepth=rawDepth;NumericPlanRawWitness=planWitness>=0?source.SourceIndex(planWitness):-1;
        if(planWitness<0||rawDepth<=.002f){NumericPlanReason="SourceAlreadyClear";return false;} // unchanged refusal for an already clear source
        int planted=0;
        if(!fromLive)
        {
        for(int i=0;i<4;i++)
        {
            if(conformRest&&TryRestSupport(planSoles[i],out var restHit))
            {
                float raise=Vector3.Dot(restHit.point-planSoles[i],up)+.008f;
                if(raise>=-.007f&&raise<=reach)
                {planTargets[i]+=up*raise;planPlanted[i]=true;planted++;continue;}
            }
            if(!TrySupportNear(planSoles[i],out var hit))continue;
            float delta=Vector3.Dot(hit.point-planSoles[i],up)+.008f;
            // An authored swing remains free; only actual nearby contact or
            // occupied paw skin establishes a new planted constraint.
            if(Mathf.Abs(delta-.008f)>.018f&&OwnerDepth(planSoles[i])<=.002f)continue;
            if(Mathf.Abs(delta)>Vector3.Distance(source.Lower(i),source.Foot(i))*.35f)continue;
            planTargets[i]+=up*delta;planPlanted[i]=true;planted++;
        }
        // Match the live support classifier: a distal corner may be planted
        // even when the lowest point hangs over an edge.
        for(int slot=0;slot<source.VertexCount;slot++)
        {
            int leg=paw[source.SourceIndex(slot)];if(leg<0||planPlanted[leg])continue;
            Vector3 point=planPoints[slot];
            if(!TrySupportNear(point,out var hit)||Mathf.Abs(Vector3.Dot(point-hit.point,up))>.018f)continue;
            float delta=Vector3.Dot(hit.point-point,up)+.008f;
            if(Mathf.Abs(delta)>Vector3.Distance(source.Lower(leg),source.Foot(leg))*.35f)continue;
            planTargets[leg]+=up*delta;planPlanted[leg]=true;planted++;
        }
        // A curved support must clear the whole visible paw footprint. Its
        // lowest corner alone can sit beyond the rim while another toe is inside.
        for(int i=0;i<4;i++)
        {
            float raise=planPawRaises[i];
            if(raise>.0001f&&raise<=reach)
            {
                float current=Vector3.Dot(planTargets[i]-source.Foot(i),up);
                planTargets[i]+=up*Mathf.Max(0,raise-current);
                if(!planPlanted[i]){planPlanted[i]=true;planted++;}
            }
            planBaseTargets[i]=planTargets[i];
        }
        }
        else for(int i=0;i<4;i++)
        {
            planBaseTargets[i]=planTargets[i]=legs[i].target;
            planPlanted[i]=legs[i].planted;
            if(planPlanted[i])planted++;
        }
        NumericPlanPlantedCount=planted;
        if(planted==0){NumericPlanReason="NoPlantedContact";return false;}
        // Previous contact is only a search seed. Reproject it onto today's
        // surface and recheck the current source-sized step and two-link reach.
        // This keeps a curved-seat solution from switching between opposite
        // contacts merely because the original clip advanced by one frame.
        if(conformRest&&allowTilt&&!fromLive&&previousRestPlan)
        for(int leg=0;leg<4;leg++)if(planPlanted[leg]&&previousRestPlanted[leg])
        {
            Vector3 target=source.Foot(leg)+transform.TransformVector(previousRestTargetOffsets[leg]);
            Vector3 shoulder=CatSupportedLimbSkin.BodyPoint(source.Upper(leg),up*previousRestLift,previousRestRotation,transform.TransformPoint(previousRestPivot));
            if(TryNumericFootTarget(source,leg,shoulder,ref target))planBaseTargets[leg]=planTargets[leg]=target;
        }
        // Diagnostic only: maximum positive translation allowed by exactly
        // these fixed targets and original two-link lengths. This value does
        // not replace the existing candidate cap or change any permission.
        float reachLift=MeasureReachLift(true);
        NumericPlanReachLiftCap=reachLift;
        // Vertical conformation on a curved seat is bounded by the measured
        // two-link reach at the actual paw heights. A fraction of the lower
        // bone is a lateral step limit, not the available body height.
        maxLift=Mathf.Max(0,Mathf.Min(reach,reachLift-.0003f));
        planPivot=Vector3.zero;
        // Reuse only a candidate shape, never a collision permission. Source
        // skin, support targets, reach and current scene are checked again.
        if(conformRest&&allowTilt&&!fromLive&&previousRestPlan)
        {
            float warmLift=Mathf.Min(previousRestLift,Mathf.Max(0,MeasureReachLift(false)-.0003f));
            // A still-valid previous correction must not hold a large lean
            // until it suddenly fails. Revalidate a small return toward the
            // original pose first; no blend bypasses skin or contact checks.
            if(Quaternion.Angle(Quaternion.identity,previousRestRotation)>3f&&
                Candidate(warmLift,Quaternion.Slerp(previousRestRotation,Quaternion.identity,Mathf.Clamp01(Time.deltaTime*6f)),transform.TransformPoint(previousRestPivot)))return true;
            if(RefinedCandidate(warmLift,previousRestRotation,transform.TransformPoint(previousRestPivot),6))return true;
        }
        if(Candidate(minimumLift,Quaternion.identity,Vector3.zero))return true;
        if(conformRest)
        {
            float currentReach=Mathf.Max(0,Mathf.Min(reach,MeasureReachLift(true)-.0003f));
            // On the next settling frame the supported reach often changes by
            // millimetres. Try that measured translation before tilt families.
            if(currentReach>minimumLift+.0001f&&
                Candidate(currentReach,Quaternion.identity,Vector3.zero))return true;
            if(allowTilt&&TryResidualTilt(currentReach))return true;
        }
        if(conformRest)
        {
            // A resting body belongs above the upper envelope, including
            // cushions. An interior-only ray misses skin below a thin cloth.
            float restLift=0;int restWitness=-1;
            for(int slot=0;slot<source.VertexCount;slot++)
            {
                int index=source.SourceIndex(slot);
                if(paw[index]>=0||limb[index]>=0)continue;
                Vector3 point=planPoints[slot];
                if(TryRestSupport(point,out var supportHit))
                {
                    float required=Vector3.Dot(supportHit.point-point,up)+.008f;
                    if(required>restLift){restLift=required;restWitness=slot;}
                }
            }
            if(restLift>0&&restLift<=reach&&Candidate(restLift,Quaternion.identity,Vector3.zero))return true;
            if(allowTilt&&restWitness>=0)
            {
                Vector3 point=planPoints[restWitness];int restFirst=-1,restSecond=-1;float restFar=-1,restNext=-1;
                for(int leg=0;leg<4;leg++)if(planPlanted[leg])
                {
                    float distance=Vector3.ProjectOnPlane(planBaseTargets[leg]-point,up).sqrMagnitude;
                    if(distance>restFar){restSecond=restFirst;restNext=restFar;restFirst=leg;restFar=distance;}else if(distance>restNext){restSecond=leg;restNext=distance;}
                }
                if(restFirst>=0)
                {
                    Vector3 restPivot=restSecond>=0?(planBaseTargets[restFirst]+planBaseTargets[restSecond])*.5f:planBaseTargets[restFirst];
                    Vector3 restRadial=Vector3.ProjectOnPlane(point-restPivot,up);
                    if(restRadial.magnitude>.08f)
                    {
                        Vector3 restAxis=Vector3.Cross(restRadial,up).normalized;
                        float angle=Mathf.Asin(Mathf.Clamp01(restLift/restRadial.magnitude))*Mathf.Rad2Deg;
                        angle=Mathf.Min(angle,35f);
                        if(RefinedCandidate(0,Quaternion.AngleAxis(angle*.5f,restAxis),restPivot))return true;
                        if(RefinedCandidate(0,Quaternion.AngleAxis(angle,restAxis),restPivot))return true;
                        if(RefinedCandidate(restLift*.5f,Quaternion.AngleAxis(angle*.5f,restAxis),restPivot))return true;
                    }
                }
            }
        }
        // The target-only pass can resolve a real rim/paw intersection before
        // another body region requires lift or tilt. Its supported goals are
        // the shared geometric constraints for those independent candidates.
        NumericPlanReachLiftCap=reachLift=MeasureReachLift(true);
        maxLift=Mathf.Max(0,Mathf.Min(reach,reachLift-.0003f));
        // The source-size limit can exceed the reach of an already planted
        // leg. Evaluate its measured remaining reach before larger candidates.
        float reachableLift=Mathf.Min(maxLift,reachLift-.0001f);
        if(reachableLift>minimumLift+.0001f&&Candidate(Mathf.Min(reachableLift,minimumLift+rawDepth+.008f),Quaternion.identity,Vector3.zero))return true;
        float requested=Mathf.Min(maxLift,minimumLift+rawDepth+.008f);
        if(TryTop(planPoints[planWitness]+up*minimumLift,out var top))requested=Mathf.Min(maxLift,minimumLift+top.distance+.008f);
        float previous=minimumLift;
        for(int choice=0;choice<3;choice++)
        {float value=choice==0?(minimumLift+requested)*.5f:choice==1?requested:maxLift;if(value<=previous+.0001f)continue;previous=value;if(Candidate(value,Quaternion.identity,Vector3.zero))return true;}

        if(!allowTilt){NumericPlanReason="TranslationCandidatesRejected";return false;}

        // Select the real planted support farthest from the measured failing
        // skin; this derives the tilt pivot without an item or front-leg rule.
        // A lifted pose may have cleared the original head/chest witness and
        // exposed the opposite hip. Derive tilt from that remaining skin,
        // retaining the measured lift, instead of tilting the raw pose again.
        float tiltLift=minimumLift;int tiltWitness=planWitness;
        for(int i=0;i<NumericPlanAttemptCount;i++)
        {
            var a=numericAttempts[i];
            if(a.tiltDegrees>.001f||a.lift<tiltLift||a.sourceVertex<0||a.diagnosticCode!="OtherOwnerSkin")continue;
            int slot=source.Slot(a.sourceVertex);if(slot<0)continue;
            tiltLift=a.lift;tiltWitness=slot;
        }
        Vector3 witness=planPoints[tiltWitness];int first=-1,second=-1;float far=-1,next=-1;
        for(int i=0;i<4;i++)if(planPlanted[i])
        {
            float distance=Vector3.ProjectOnPlane(planBaseTargets[i]-witness,up).sqrMagnitude;
            if(distance>far){second=first;next=far;first=i;far=distance;}else if(distance>next){second=i;next=distance;}
        }
        if(first<0){NumericPlanReason="NoTiltPivot";return false;}
        Vector3 pivot=second>=0?(planBaseTargets[first]+planBaseTargets[second])*.5f:planBaseTargets[first];
        Vector3 radial=Vector3.ProjectOnPlane(witness-pivot,up);NumericPlanTiltPivot=pivot;
        if(radial.magnitude<.08f){NumericPlanReason="TiltLeverBelowEightCentimetres";return false;}
        Vector3 axis=Vector3.Cross(radial,up).normalized;NumericPlanTiltAxis=axis;
        float degrees=Mathf.Min(6f,Mathf.Asin(Mathf.Clamp01((rawDepth+.008f)/radial.magnitude))*Mathf.Rad2Deg);
        previous=0;
        for(int choice=0;choice<3;choice++)
        {float angle=choice==0?degrees*.5f:choice==1?degrees:6f;if(angle<=previous+.01f)continue;previous=angle;if(Candidate(tiltLift,Quaternion.AngleAxis(angle,axis),pivot))return true;}
        if(tiltLift>.0001f&&Candidate(tiltLift,Quaternion.AngleAxis(12f,axis),pivot))return true;
        NumericPlanReason="AllCandidatesRejected";return false;

        bool TryResidualTilt(float lift)
        {
            if(NumericPlanAttemptCount==0)return false;
            var failed=numericAttempts[NumericPlanAttemptCount-1];
            int slot=failed.sourceVertex>=0?source.Slot(failed.sourceVertex):-1;
            if(slot<0)return false;
            Vector3 point=planPoints[slot];int first=-1,second=-1;float far=-1,next=-1;
            for(int leg=0;leg<4;leg++)if(planPlanted[leg])
            {
                float distance=Vector3.ProjectOnPlane(planBaseTargets[leg]-point,up).sqrMagnitude;
                if(distance>far){second=first;next=far;first=leg;far=distance;}else if(distance>next){second=leg;next=distance;}
            }
            if(first<0)return false;
            Vector3 pivot=second>=0?(planBaseTargets[first]+planBaseTargets[second])*.5f:planBaseTargets[first];
            Vector3 radial=Vector3.ProjectOnPlane(point-pivot,up);
            if(radial.magnitude<.08f)return false;
            Vector3 axis=Vector3.Cross(radial,up).normalized;
            float angle=Mathf.Min(6f,Mathf.Asin(Mathf.Clamp01((rawDepth+.008f)/radial.magnitude))*Mathf.Rad2Deg);
            return Candidate(lift,Quaternion.AngleAxis(angle*.5f,axis),pivot)||
                Candidate(lift,Quaternion.AngleAxis(angle,axis),pivot)||
                Candidate(lift,Quaternion.AngleAxis(12f,axis),pivot);
        }

        bool RefinedCandidate(float lift,Quaternion rotation,Vector3 pivot,int maximumSteps=4)
        {
            if(pivot==Vector3.zero)
            {int count=0;for(int leg=0;leg<4;leg++)if(planPlanted[leg]){pivot+=planBaseTargets[leg];count++;}if(count>0)pivot/=count;}
            // Refine the actual remaining witness along the support normal.
            // Continue a near-clear warm pose instead of jumping to a distant
            // tilt family. Every step is rechecked against the exact skin.
            for(int pass=0;pass<maximumSteps;pass++)
            {
                int before=NumericPlanAttemptCount;
                if(Candidate(lift,rotation,pivot))return true;
                if(NumericPlanAttemptCount==before)return false;
                var failed=numericAttempts[NumericPlanAttemptCount-1];
                if(failed.sourceVertex<0||failed.depth<=.002f||failed.depth>.025f||
                    (failed.diagnosticCode!="OtherOwnerSkin"&&failed.diagnosticCode!="DeepestOwnerSkin"))return false;
                if(!TryTop(failed.worldPoint,out var hit))return false;
                float step=Vector3.Dot(hit.point-failed.worldPoint,up)+.003f;
                if(step>.0001f&&step<=.03f&&step<=failed.depth*3f&&failed.lift>=lift-.00001f)
                {lift=failed.lift+step;continue;}
                // A nearly vertical decorative face needs a small supported
                // lean along its closest normal, not a large upward escape.
                if(!(hit.collider is MeshCollider mesh)||!TryOwnerMetric(mesh,failed.worldPoint,out float depth,out var normal))return false;
                Vector3 radial=failed.worldPoint-pivot-up*failed.lift;
                Vector3 axis=Vector3.Cross(radial,normal);
                if(axis.sqrMagnitude<1e-10f||radial.magnitude<.05f)return false;
                float angle=Mathf.Min(6f,Mathf.Asin(Mathf.Clamp01((depth+.003f)/radial.magnitude))*Mathf.Rad2Deg);
                rotation=Quaternion.AngleAxis(angle,axis.normalized)*rotation;
                if(Quaternion.Angle(Quaternion.identity,rotation)>35.01f)return false;
                lift=failed.lift;
            }
            return false;
        }

        float MeasureReachLift(bool allowSupportedStep)
        {
            float value=float.PositiveInfinity;
            for(int i=0;i<4;i++)if(planPlanted[i])
            {
                Vector3 delta=source.Upper(i)-planBaseTargets[i];
                float length=Vector3.Distance(source.Upper(i),source.Lower(i))+Vector3.Distance(source.Lower(i),source.Foot(i))-.0001f;
                float horizontal=Vector3.ProjectOnPlane(delta,up).sqrMagnitude;
                if(allowSupportedStep)
                {
                    float allowance=planStepLimits[i];
                    float nearest=Mathf.Max(0,Vector3.ProjectOnPlane(source.Upper(i)-source.Foot(i),up).magnitude-allowance);
                    horizontal=nearest*nearest;
                }
                value=Mathf.Min(value,horizontal>length*length?float.NegativeInfinity:
                    Mathf.Sqrt(Mathf.Max(0,length*length-horizontal))-Vector3.Dot(delta,up));
            }
            return value;
        }

        bool Candidate(float lift,Quaternion rotation,Vector3 pivot)
        {
            if(NumericPlanAttemptCount>=numericAttempts.Length)return false;
            if(Quaternion.Angle(Quaternion.identity,rotation)>.001f)
            {
                float tiltedReach=float.PositiveInfinity;
                for(int i=0;i<4;i++)if(planPlanted[i])
                {
                    Vector3 upper=CatSupportedLimbSkin.BodyPoint(source.Upper(i),Vector3.zero,rotation,pivot);
                    Vector3 delta=upper-planBaseTargets[i];
                    float length=Vector3.Distance(source.Upper(i),source.Lower(i))+Vector3.Distance(source.Lower(i),source.Foot(i))-.0003f;
                    float allowance=planStepLimits[i];
                    float nearest=Mathf.Max(0,Vector3.ProjectOnPlane(upper-source.Foot(i),up).magnitude-allowance);
                    float horizontal=nearest*nearest;
                    if(horizontal>=length*length)return false;
                    tiltedReach=Mathf.Min(tiltedReach,Mathf.Sqrt(length*length-horizontal)-Vector3.Dot(delta,up));
                }
                lift=Mathf.Min(lift,Mathf.Max(0,tiltedReach-.0001f));
            }
            // Fixed source and foot goals are unchanged after candidate zero.
            // A reach-capped lift may equal the later source-sized candidate.
            // Do not evaluate that identical rejected translation twice.
            if(Quaternion.Angle(Quaternion.identity,rotation)<.001f)
                for(int i=1;i<NumericPlanAttemptCount;i++)
                    if(numericAttempts[i].tiltDegrees<.001f&&Mathf.Abs(numericAttempts[i].lift-lift)<.000001f)return false;
            int at=NumericPlanAttemptCount++;
            numericAttempts[at]=new NumericSupportAttempt{lift=lift,tiltDegrees=Quaternion.Angle(Quaternion.identity,rotation),
                leg=-1,sourceVertex=-1,reachMargin=float.PositiveInfinity,diagnosticCode="Evaluating"};
            Vector3 translation=up*lift;
            for(int i=0;i<4;i++)
            {
                // A failed body candidate must not leave modified foot goals
                // for the next lift/tilt candidate.
                planTargets[i]=planPlanted[i]?planBaseTargets[i]:CatSupportedLimbSkin.BodyPoint(source.Foot(i),translation,rotation,pivot);
                planSolve[i]=planPlanted[i];
                planFootOrientation[i]=Quaternion.identity;
                planLower[i]=fromLive?CatSupportedLimbSkin.BodyPoint(legs[i].desiredLower-up*minimumLift,translation,rotation,pivot):
                    CatSupportedLimbSkin.BodyPoint(source.Lower(i),translation,rotation,pivot);
                if(!planPlanted[i]){planPoses[i]=source.SourcePose(i,translation,rotation,pivot);continue;}
                float a=Vector3.Distance(source.Upper(i),source.Lower(i)),b=Vector3.Distance(source.Lower(i),source.Foot(i));
                float distance=Vector3.Distance(CatSupportedLimbSkin.BodyPoint(source.Upper(i),translation,rotation,pivot),planTargets[i]);
                float margin=Mathf.Min(a+b-.0001f-distance,distance-Mathf.Abs(a-b)-.0001f);
                numericAttempts[at].reachMargin=Mathf.Min(numericAttempts[at].reachMargin,margin);
                if(distance>=a+b-.0001f||distance<=Mathf.Abs(a-b)+.0001f)
                {
                    // Preserve the existing supported-foot allowance instead
                    // of lengthening a link when a small lift changes reach.
                    Vector3 shoulder=CatSupportedLimbSkin.BodyPoint(source.Upper(i),translation,rotation,pivot);
                    Vector3 delta=shoulder-planTargets[i],horizontal=Vector3.ProjectOnPlane(delta,up);
                    float length=a+b-.0003f,height=Vector3.Dot(delta,up);
                    if(height*height>=length*length||horizontal.sqrMagnitude<1e-12f)return Reject("Reach",i);
                    float radial=Mathf.Sqrt(length*length-height*height);
                    Vector3 target=planTargets[i]+horizontal.normalized*Mathf.Max(0,horizontal.magnitude-radial);
                    if(!TryNumericFootTarget(source,i,shoulder,ref target)&&
                        !(conformRest&&TryRestFootTarget(source,i,shoulder,out target)))return Reject("Reach",i);
                    planTargets[i]=target;
                }
                if(!source.TryPose(i,planTargets[i],planLower[i],translation,rotation,pivot,planFootOrientation[i],out planPoses[i]))return Reject("TwoLinkPose",i);
            }
            // Legacy support also changes the bending plane of a colliding
            // unplanted limb. Use the exact same numeric source pose here, so
            // preflight and live application receive identical joint goals.
            // A supported paw escape can expose a different forearm witness.
            // Re-evaluate that actual remaining skin, with the original source
            // limits on every trial; stop as soon as no target improves.
            for(int pass=0;pass<3;pass++)
            {
                int acceptedBefore=NumericPlanLimbAccepted;
                for(int i=0;i<4;i++)RefineNumericLimb(source,i,translation,rotation,pivot,heading);
                if(NumericPlanLimbAccepted==acceptedBefore)break;
            }
            // Only the initial target-only pass establishes the foot support
            // basis. Later failed lift/tilt trials cannot contaminate it. Every
            // moved goal has already passed real support and two-link reach;
            // all weighted skin and the fresh scene still gate acceptance.
            if(Mathf.Abs(lift-minimumLift)<.000001f&&Quaternion.Angle(Quaternion.identity,rotation)<.001f)
                for(int i=0;i<4;i++)if(planPlanted[i])planBaseTargets[i]=planTargets[i];
            planOwnerDepth=0;
            if(planWitness>=0)
            {
                float witnessDepth=OwnerDepth(source.CombinedPoint(planWitness,planPoses,translation,rotation,pivot));
                if(witnessDepth>.002f)return Reject("DeepestOwnerSkin",-1,source.SourceIndex(planWitness),witnessDepth);
            }
            bool firstPoint=true;
            for(int slot=0;slot<source.VertexCount;slot++)
            {
                Vector3 point=source.CombinedPoint(slot,planPoses,translation,rotation,pivot);
                if(firstPoint){planBounds=new Bounds(point,Vector3.zero);firstPoint=false;}else planBounds.Encapsulate(point);
                float depth=OwnerDepth(point);if(depth>.002f)return Reject("OtherOwnerSkin",-1,source.SourceIndex(slot),depth);planOwnerDepth=Mathf.Max(planOwnerDepth,depth);
            }
            // Settle only a fully clear candidate; rejected candidates cannot
            // pay for a contact search they will never use.
            bool settled=false;
            for(int i=0;i<4;i++)if(planPlanted[i])settled|=SettleNumericPaw(source,i,translation,rotation,pivot);
            if(settled)
            for(int slot=0;slot<source.VertexCount;slot++)
            {
                Vector3 point=source.CombinedPoint(slot,planPoses,translation,rotation,pivot);
                planBounds.Encapsulate(point);float depth=OwnerDepth(point);
                if(depth>.002f)return Reject("SettledOwnerSkin",-1,source.SourceIndex(slot),depth);
                planOwnerDepth=Mathf.Max(planOwnerDepth,depth);
            }
            if(!environmentSynchronized){Physics.SyncTransforms();environmentSynchronized=true;}
            var box=planBounds;box.Expand(.002f);combinedOwnerDepth=planOwnerDepth;
            combinedQuery[0]=new CatBodyGuardBox{centre=box.center,halfExtents=box.extents,rotation=Quaternion.identity};
            CommonLiftBlocker=null;
            if(movement==null||!movement.IsInteractionBoxesClear(combinedQuery,.002f,measuredOwnerRefinement))
            {numericAttempts[at].blocker=CommonLiftBlocker;return Reject("SceneBox");}
            numericAttempts[at].diagnosticCode="Accepted";numericAttempts[at].depth=planOwnerDepth;NumericPlanReason="Accepted";
            planLift=lift;planRotation=rotation;planPivot=pivot;return true;
            bool Reject(string reason,int leg=-1,int sourceVertex=-1,float depth=0)
            {
                numericAttempts[at].diagnosticCode=reason;numericAttempts[at].leg=leg;numericAttempts[at].sourceVertex=sourceVertex;numericAttempts[at].depth=depth;
                if(sourceVertex>=0)
                {int slot=source.Slot(sourceVertex);if(slot>=0)numericAttempts[at].worldPoint=source.CombinedPoint(slot,planPoses,translation,rotation,pivot);}
                return false;
            }
        }
    }

    bool TryRestSupport(Vector3 sole,out RaycastHit result)
    {
        result=default;bool found=false;float nearest=float.PositiveInfinity;
        Vector3 origin=sole+up*(reference+reach-Vector3.Dot(sole,up));
        for(int i=0;i<solids.Count;i++)
        {
            if(!solidActive[i])continue;RayQueries++;
            if(!solids[i].Raycast(new Ray(origin,-up),out var hit,reach+.09f))continue;
            if(!CatMeshContactSurface.TryOriginalTriangleNormal(solidMeshes[i],solidMatrices[i],hit.triangleIndex,out var normal)||Vector3.Dot(normal,up)<.35f)continue;
            float distance=Mathf.Abs(Vector3.Dot(hit.point-sole,up));
            if(distance>=nearest)continue;nearest=distance;result=hit;found=true;
        }
        return found;
    }

    bool TryRestFootTarget(CatSupportedLimbSkin source,int leg,Vector3 shoulder,out Vector3 target)
    {
        target=planBaseTargets[leg];
        float a=Vector3.Distance(source.Upper(leg),source.Lower(leg));
        float b=Vector3.Distance(source.Lower(leg),source.Foot(leg));
        float radius=planStepLimits[leg],best=float.PositiveInfinity;bool found=false;
        Vector3 forward=Vector3.ProjectOnPlane(transform.forward,up).normalized;
        Vector3 right=Vector3.Cross(up,forward);
        for(int step=0;step<17;step++)
        {
            float angle=(step-1)%8*Mathf.PI*.25f;
            Vector3 offset=step==0?Vector3.zero:(right*Mathf.Cos(angle)+forward*Mathf.Sin(angle))*radius*(step<=8?.5f:1f);
            Vector3 sole=planSoles[leg]+offset;
            if(!TryRestSupport(sole,out var hit))continue;
            Vector3 candidate=source.Foot(leg)+offset+up*(Vector3.Dot(hit.point-sole,up)+.008f);
            float distance=Vector3.Distance(shoulder,candidate);
            if(distance>=a+b-.0003f||distance<=Mathf.Abs(a-b)+.0003f)continue;
            float cost=offset.sqrMagnitude;
            if(cost>=best)continue;
            best=cost;target=candidate;found=true;
        }
        return found;
    }

    // Only actual geometry supplies a correction direction. There is no
    // item-specific offset or angular candidate grid. Intermediate trials are
    // numeric; the original full-skin/scene acceptance below remains mandatory.
    void RefineNumericLimb(CatSupportedLimbSkin source,int leg,Vector3 translation,Quaternion rotation,Vector3 pivot,Quaternion heading)
    {
        float score=NumericLimbScore(source,leg,translation,rotation,pivot,out int witness);
        if(score<=.002f||float.IsInfinity(score)||witness<0)return;
        Vector3 point=source.CombinedPoint(witness,planPoses,translation,rotation,pivot);
        if(!TryMeasuredInterior(point,out var interior)||!(interior.collider is MeshCollider mesh)||
            !TryOwnerMetric(mesh,point,out float depth,out var normal)||depth<=.002f)return;
        TryFootOrientation();
        if(score<=.002f)return;
        int sourceVertex=source.SourceIndex(witness);
        if(sourceVertex>=0&&sourceVertex<paw.Length&&paw[sourceVertex]==leg)
        {
            // Distal skin follows the held wrist, so changing the elbow plane
            // cannot remove a paw-tip intersection. The measured escape must
            // move the foot target, as in the existing paw solver. An authored
            // swing remains unplanted and may only escape the measured solid
            // within its original source-sized correction allowance.
            Vector3 firstTarget=planTargets[leg],firstLower=planLower[leg];
            Try(firstTarget+normal*(depth+.008f),firstLower,true);
            if(score<=.002f)return;
            for(int direction=0;direction<6;direction++)
            {
                Vector3 axis=direction==0?up:direction==1?-up:direction==2?heading*Vector3.right:
                    direction==3?heading*Vector3.left:direction==4?heading*Vector3.forward:heading*Vector3.back;
                RayQueries++;
                if(!interior.collider.Raycast(new Ray(point,axis),out var exit,reach+.01f))continue;
                Vector3 exitNormal=exit.normal;
                if(!CatMeshContactSurface.TryOriginalTriangleNormal(mesh.sharedMesh,mesh.transform.localToWorldMatrix,exit.triangleIndex,out exitNormal)||
                    Vector3.Dot(exitNormal,axis)<=.0001f)continue;
                // Candidates share the same original target. Accepted trials
                // are compared by fresh actual weighted skin; no cumulative
                // escape displacement can exceed the original 35% limit.
                Try(firstTarget+axis*(exit.distance+.008f),firstLower,true);
                if(score<=.002f)return;
            }
        }
        Vector3 desired=planPoses[leg].elbow+normal*(depth+.008f);
        Try( planTargets[leg],desired,false );
        if(score<=.002f||!planPlanted[leg])return;

        // The desired elbow must lie on its real upper-link sphere. At the
        // present supported foot height, the lower link defines a circle.
        // Its nearest point is the minimum lateral target change needed by
        // that measured escape; all source-sized support limits still apply.
        Vector3 shoulder=CatSupportedLimbSkin.BodyPoint(source.Upper(leg),translation,rotation,pivot);
        float upper=Vector3.Distance(source.Upper(leg),source.Lower(leg));
        float lower=Vector3.Distance(source.Lower(leg),source.Foot(leg));
        Vector3 wanted=desired-shoulder;if(wanted.sqrMagnitude<1e-12f)return;
        Vector3 elbow=shoulder+wanted.normalized*upper;
        float height=Vector3.Dot(planTargets[leg]-elbow,up);
        if(Mathf.Abs(height)<lower)
        {
            Vector3 circle=elbow+up*height,radial=Vector3.ProjectOnPlane(planTargets[leg]-circle,up);
            if(radial.sqrMagnitude>1e-12f)
                Try(circle+radial.normalized*Mathf.Sqrt(lower*lower-height*height),desired,true);
        }
        // A second direction follows the same measured joint displacement;
        // there is one trial, not an expanding blind radial search.
        if(score>.002f)Try(planTargets[leg]+Vector3.ProjectOnPlane(desired-planPoses[leg].elbow,up),desired,true);
        if(score>.002f)
        {
            // A concave support can require a different real contact beside
            // the blocked elbow. Inspect a bounded neighbourhood once; each
            // goal still passes the actual surface, paw, reach and knee limits.
            Vector3 outward=Vector3.ProjectOnPlane(normal,up).normalized;
            if(outward.sqrMagnitude<.001f)outward=Vector3.ProjectOnPlane(source.Foot(leg)-source.Upper(leg),up).normalized;
            Vector3 tangent=Vector3.Cross(up,outward);
            float radius=planStepLimits[leg];
            for(int candidate=0;candidate<8&&score>.002f;candidate++)
            {
                float angle=candidate*Mathf.PI*.25f;
                Vector3 delta=(outward*Mathf.Cos(angle)+tangent*Mathf.Sin(angle))*radius;
                Try(source.Foot(leg)+delta,desired,true);
            }
        }

        void Try(Vector3 target,Vector3 lowerTarget,bool moveFoot)
        {
            NumericPlanLimbTrials++;
            Vector3 origin=CatSupportedLimbSkin.BodyPoint(source.Upper(leg),translation,rotation,pivot);
            if(moveFoot&&!TryNumericFootTarget(source,leg,origin,ref target))return;
            float a=Vector3.Distance(source.Upper(leg),source.Lower(leg)),b=Vector3.Distance(source.Lower(leg),source.Foot(leg));
            float distance=Vector3.Distance(origin,target);
            if(distance>=a+b-.0001f||distance<=Mathf.Abs(a-b)+.0001f)return;
            if(!source.TryPose(leg,target,lowerTarget,translation,rotation,pivot,planFootOrientation[leg],out var pose))return;
            var saved=planPoses[leg];planPoses[leg]=pose;
            float next=NumericLimbScore(source,leg,translation,rotation,pivot,out _,score-.0001f);
            if(next+.0001f>=score){planPoses[leg]=saved;return;}
            score=next;planTargets[leg]=target;planLower[leg]=lowerTarget;planSolve[leg]=true;NumericPlanLimbAccepted++;
        }
        void TryFootOrientation()
        {
            if(!planPlanted[leg]||ownerSurface==null)return;
            // One actual upper-triangle ray supplies an orientation hint.
            // The following exact weighted skin still decides acceptance.
            Vector3 sole=planTargets[leg]+planFootOrientation[leg]*(planSoles[leg]-source.Foot(leg));
            if(!TrySupportNear(sole,out var support)||Vector3.Dot(support.normal,up)<.5f)return;
            Vector3 contact=support.point,surfaceNormal=support.normal;
            if(Vector3.Distance(sole,contact)>.06f)return;
            Quaternion goal=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(up,surfaceNormal),25f);
            Quaternion initialOrientation=planFootOrientation[leg];Vector3 initialTarget=planTargets[leg];
            for(int choice=0;choice<2;choice++)
            {
                Quaternion desired=Quaternion.Slerp(initialOrientation,goal,choice==0?.5f:1f);
                Quaternion delta=desired*Quaternion.Inverse(initialOrientation);
                Vector3 target=contact+delta*(initialTarget-contact);
                float a=Vector3.Distance(source.Upper(leg),source.Lower(leg)),b=Vector3.Distance(source.Lower(leg),source.Foot(leg));
                if(Vector3.ProjectOnPlane(target-source.Foot(leg),up).magnitude>planStepLimits[leg])continue;
                Vector3 shoulder=CatSupportedLimbSkin.BodyPoint(source.Upper(leg),translation,rotation,pivot);
                float distance=Vector3.Distance(shoulder,target);
                if(distance>=a+b-.0001f||distance<=Mathf.Abs(a-b)+.0001f)continue;
                if(!source.TryPose(leg,target,planLower[leg],translation,rotation,pivot,desired,out var pose))continue;
                var saved=planPoses[leg];planPoses[leg]=pose;
                float next=NumericLimbScore(source,leg,translation,rotation,pivot,out _,score-.0001f);
                if(next+.0001f>=score){planPoses[leg]=saved;continue;}
                score=next;planFootOrientation[leg]=desired;planTargets[leg]=target;planSolve[leg]=true;NumericPlanLimbAccepted++;
                if(score<=.002f)return;
            }
        }
    }
    float NumericLimbScore(CatSupportedLimbSkin source,int leg,Vector3 translation,Quaternion rotation,Vector3 pivot,out int witness,float stopAt=float.PositiveInfinity)
    {
        witness=-1;float maximum=0;
        var affected=source.Vertices(leg);
        for(int i=0;i<affected.Count;i++)
        {
            int slot=affected[i],index=source.SourceIndex(slot);
            // Rank a limb correction by its existing measured majority-weight
            // limb/paw mask. A small blend into the torso must not choose the
            // body's witness as an elbow goal. Final all-skin coverage below
            // remains unchanged and still checks every mixed-weight vertex.
            if(index<0||index>=limb.Length||(limb[index]!=leg&&paw[index]!=leg))continue;
            float depth=OwnerDepth(source.CombinedPoint(slot,planPoses,translation,rotation,pivot));
            if(depth>maximum){maximum=depth;witness=slot;}
            if(maximum>=stopAt)return maximum;
        }
        return maximum;
    }
    bool SettleNumericPaw(CatSupportedLimbSkin source,int leg,Vector3 translation,Quaternion rotation,Vector3 pivot)
    {
        if(ownerSurface==null)return false;
        float nearest=float.PositiveInfinity;Vector3 correction=Vector3.zero;
        foreach(int slot in source.Vertices(leg))
        {
            if(paw[source.SourceIndex(slot)]!=leg)continue;
            Vector3 point=source.CombinedPoint(slot,planPoses,translation,rotation,pivot);
            if(!ownerSurface.TryClosest(point,out var hit)||Vector3.Dot(hit.Normal,up)<.5f)continue;
            float gap=Vector3.Distance(point,hit.Point);
            if(gap<=.012f){pawSettleDiagnostics[leg]=new Vector4(1,gap,0,0);return false;}
            if(gap>=nearest)continue;
            nearest=gap;correction=hit.Point+hit.Normal*.008f-point;
        }
        pawSettleDiagnostics[leg]=new Vector4(2,nearest,correction.magnitude,0);
        if(nearest<=.012f||nearest>.06f)return false;
        Vector3 target=planTargets[leg]+correction;
        float length=Vector3.Distance(source.Lower(leg),source.Foot(leg));
        if(Vector3.ProjectOnPlane(target-source.Foot(leg),up).magnitude>planStepLimits[leg]){pawSettleDiagnostics[leg].x=3;return false;}
        float a=Vector3.Distance(source.Upper(leg),source.Lower(leg));
        float distance=Vector3.Distance(CatSupportedLimbSkin.BodyPoint(source.Upper(leg),translation,rotation,pivot),target);
        if(distance>=a+length-.0001f||distance<=Mathf.Abs(a-length)+.0001f){pawSettleDiagnostics[leg].x=4;return false;}
        if(!source.TryPose(leg,target,planLower[leg],translation,rotation,pivot,planFootOrientation[leg],out var pose))return false;
        var saved=planPoses[leg];var savedTarget=planTargets[leg];var savedLower=planLower[leg];var savedOrientation=planFootOrientation[leg];
        planPoses[leg]=pose;planTargets[leg]=target;
        float settleDepth=NumericLimbScore(source,leg,translation,rotation,pivot,out _, .00201f);
        if(settleDepth>.002f)
        {
            for(int pass=0;pass<2;pass++)RefineNumericLimb(source,leg,translation,rotation,pivot,transform.rotation);
            settleDepth=NumericLimbScore(source,leg,translation,rotation,pivot,out _, .00201f);
        }
        pawSettleDiagnostics[leg].w=settleDepth;
        bool contact=false;
        if(settleDepth<=.002f)
        foreach(int slot in source.Vertices(leg))
        {
            if(paw[source.SourceIndex(slot)]!=leg)continue;
            Vector3 point=source.CombinedPoint(slot,planPoses,translation,rotation,pivot);
            if(ownerSurface.TryClosest(point,out var hit)&&Vector3.Dot(hit.Normal,up)>=.5f&&Vector3.Distance(point,hit.Point)<=.015f){contact=true;break;}
        }
        if(settleDepth>.002f||!contact)
        {
            pawSettleDiagnostics[leg].x=5;planPoses[leg]=saved;planTargets[leg]=savedTarget;planLower[leg]=savedLower;planFootOrientation[leg]=savedOrientation;
            return false;
        }
        pawSettleDiagnostics[leg].x=6;
        planSolve[leg]=true;return true;
    }
    bool TryNumericFootTarget(CatSupportedLimbSkin source,int leg,Vector3 shoulder,ref Vector3 target)
    {
        float a=Vector3.Distance(source.Upper(leg),source.Lower(leg)),b=Vector3.Distance(source.Lower(leg),source.Foot(leg));
        if(!planPlanted[leg])
            return Vector3.Distance(target,source.Foot(leg))<=b*.35f;
        Vector3 lateral=Vector3.ProjectOnPlane(target-source.Foot(leg),up);float maximum=planStepLimits[leg];
        if(lateral.magnitude>maximum)target+=lateral.normalized*maximum-lateral;
        // The source paw keeps its original world orientation after IK. Its
        // measured sole therefore translates by exactly the foot displacement.
        Vector3 sole=planSoles[leg]+target-source.Foot(leg);
        if(!TrySupportNear(sole,out var hit))return false;
        target+=up*(Vector3.Dot(hit.point-sole,up)+.008f);
        // Preserve all distal support points after a lateral target change.
        float raise=0;
        foreach(int slot in source.Vertices(leg))
        {
            int index=source.SourceIndex(slot);if(index<0||index>=paw.Length||paw[index]!=leg)continue;
            Vector3 point=planPoints[slot]+target-source.Foot(leg);
            if(TryTop(point,out var top))raise=Mathf.Max(raise,Vector3.Dot(top.point-point,up)+.008f);
        }
        if(raise>maximum)return false;
        target+=up*raise;
        float distance=Vector3.Distance(shoulder,target);
        if(distance>=a+b-.0001f||distance<=Mathf.Abs(a-b)+.0001f)return false;
        return Vector3.ProjectOnPlane(target-source.Foot(leg),up).magnitude<=maximum+.000001f;
    }

    void ApplyNumericSupportPlan(Vector3 rootBefore,Quaternion headingBefore)
    {
        NumericPlanApplied=true;
        if(!activityAnimation.IsNativeJump)
        {
            previousRestPlan=true;previousRestLift=planLift;previousRestRotation=planRotation;previousRestPivot=transform.InverseTransformPoint(planPivot);
            for(int leg=0;leg<4;leg++)
            {previousRestPlanted[leg]=planPlanted[leg];previousRestTargetOffsets[leg]=transform.InverseTransformVector(planTargets[leg]-supportedSkin.Foot(leg));}
        }
        adjusted=true;VisualLift=RequiredLift=planLift;BodyRotation=planRotation;BodyPivot=planPivot;
        BodyTiltDegrees=Quaternion.Angle(Quaternion.identity,planRotation);
        visual.position=CatSupportedLimbSkin.BodyPoint(visualWorldBefore,up*planLift,planRotation,planPivot);
        visual.rotation=planRotation*visualWorldRotationBefore;
        PlantedLegCount=0;
        for(int i=0;i<4;i++)
        {
            var leg=legs[i];leg.planted=planPlanted[i];leg.solve=planSolve[i];leg.target=planTargets[i];leg.desiredLower=planLower[i];
            leg.worldFoot=planFootOrientation[i]*leg.worldFoot;
            if(leg.planted)PlantedLegCount++;
            if(!leg.solve)continue;Solve(leg);
            MaximumLegResidual=Mathf.Max(MaximumLegResidual,Vector3.Distance(leg.foot.position,leg.target));
            MaximumFootLateralShift=Mathf.Max(MaximumFootLateralShift,Vector3.ProjectOnPlane(leg.target-leg.sourceFoot,up).magnitude);
        }
        Bake();MaximumTopPenetration=planOwnerDepth;MeasuredVertices=mask.Count;
        MaximumRootShift=Mathf.Max(MaximumRootShift,Vector3.Distance(rootBefore,transform.position));
        Debug.Assert(Quaternion.Angle(headingBefore,transform.rotation)<.001f);
    }
}
