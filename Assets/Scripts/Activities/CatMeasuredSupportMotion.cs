using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Conforms an existing supported source pose to the owner's real upper surface.
/// The actor/root, clip, scale and bone lengths are never changed. Only a point inside a solid
/// asks for correction; the empty volume below an underside is not support.
/// </summary>
[DefaultExecutionOrder(680)]
[DisallowMultipleComponent]
public sealed partial class CatMeasuredSupportMotion : MonoBehaviour
{
    sealed class Leg
    {
        public Transform upper, lower, foot;
        public int index;
        public Quaternion upperLocal, lowerLocal, footLocal, worldFoot;
        public Vector3 sourceUpper, sourceLower, sourceFoot, target;
        public float upperLength, lowerLength;
        public Vector3 sourceSole, supportPoint, desiredLower;
        public float soleOffset;
        public bool planted, solve, hasPawBounds;
        public Bounds sourcePawBounds;
    }
    readonly RaycastHit[] lowerSupportHits = new RaycastHit[32];
    readonly CatBodyGuardBox[] lowerTargetBox = new CatBodyGuardBox[1];
    bool environmentSynchronized;
    CatMovement movement;
    CatSupportedLimbSkin supportedSkin;
    CatMeshContactSurface.TargetSet ownerSurface;
    bool predictionReady;
    int weightedWitnessSlot=-1;
    readonly int[] weightedFirst={-1,-1,-1,-1};
    readonly CatSupportedLimbSkin.Pose[] combinedPoses=new CatSupportedLimbSkin.Pose[4];
    readonly CatBodyGuardBox[] combinedQuery=new CatBodyGuardBox[1];
    Func<int,Collider,bool> measuredOwnerRefinement;
    Bounds combinedBounds;
    float combinedOwnerDepth;
    public int CommonLiftCandidates { get; private set; }
    public int AcceptedCommonLifts { get; private set; }
    public string CommonLiftReason { get; private set; }
    public int CommonLiftCalls { get; private set; }
    public float CommonLiftResidual { get; private set; }
    public float CommonLiftAllowedByReach { get; private set; }
    public float CommonLiftSourceLimit { get; private set; }
    public int CommonLiftRejectLeg { get; private set; }
    public Collider CommonLiftBlocker { get; private set; }
    public int CommonLiftRecordedCandidates { get; private set; }
    readonly float[] commonLiftHeights=new float[9], commonLiftDepths=new float[9];
    readonly Leg[] legs = { new Leg(), new Leg(), new Leg(), new Leg() };
    readonly Quaternion[] iterationRotations=new Quaternion[12];
    readonly Vector3[] iterationGoals=new Vector3[8];
    readonly bool[] iterationStates=new bool[8];
    readonly int[] iterationWitnesses=new int[4];
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<int> mask = new List<int>();
    readonly List<Collider> solids = new List<Collider>();
    readonly float[] pawCorrection = new float[4], limbCorrection = new float[4];
    readonly int[] pawWitness = new int[4], limbWitness = new int[4];
    struct TopResult { public bool found; public RaycastHit hit; }
    readonly Dictionary<Vector3,TopResult> topCache = new Dictionary<Vector3,TopResult>(4096);
    // Exact points are reused only within one synchronous solve, while the
    // captured owner geometry and query direction are unchanged.
    readonly Dictionary<Vector3,float> ownerDepthCache = new Dictionary<Vector3,float>(4096);
    Vector3[] world = Array.Empty<Vector3>();
    int[] paw = Array.Empty<int>(), limb = Array.Empty<int>();
    Matrix4x4[] solidMatrices = Array.Empty<Matrix4x4>();
    Bounds[] solidBounds = Array.Empty<Bounds>();
    bool[] solidActive=Array.Empty<bool>();
    Mesh[] solidMeshes=Array.Empty<Mesh>();
    Transform[] solidTransforms=Array.Empty<Transform>();
    CatActivity owner;
    CatActivityAnimation activityAnimation;
    Transform authored, visual;
    SkinnedMeshRenderer skin;
    Mesh sample;
    bool adjusted;
    Vector3 visualBefore,visualWorldBefore;
    Matrix4x4 sourceVisualMatrix;
    Quaternion visualRotationBefore,visualWorldRotationBefore;
    public float BodyTiltDegrees { get; private set; }
    public Quaternion BodyRotation { get; private set; }=Quaternion.identity;
    public Vector3 BodyPivot { get; private set; }
    public bool TryReadSourceVisualMatrix(Transform expected,out Matrix4x4 matrix)
    {matrix=sourceVisualMatrix;return adjusted&&visual!=null&&visual==expected;}
    Vector3 up;
    float reference, reach;
    public bool IsActive => owner != null && owner.IsRunning && activityAnimation != null &&
        activityAnimation.IsActive &&
        (activityAnimation.IsNativeJump ? activityAnimation.NativeJumpPhase <= CatJumpMotion.Takeoff || activityAnimation.NativeJumpPhase >= CatJumpMotion.Touchdown :
        activityAnimation.ContactSurface != null || owner is TubEdgeWalkActivity tub && tub.IsRimWalking);
    public float VisualLift { get; private set; }
    public float RequiredLift { get; private set; }
    public float MaximumTopPenetration { get; private set; }
    public float MaximumLegResidual { get; private set; }
    public float MaximumRootShift { get; private set; }
    public int MeasuredVertices { get; private set; }
    public int RayQueries { get; private set; }
    public bool ReachLimited { get; private set; }
    public CatActivity Owner => owner;
    public double LastSolveMs { get; private set; }
    public double MaximumSolveMs { get; private set; }
    public int SolveFrame { get; private set; }
    public int SolveCount { get; private set; }
    public bool NumericPlanApplied { get; private set; }
    public int PlantedLegCount { get; private set; }
    public int BakeCount { get; private set; }
    public int SupportCandidateQueries { get; private set; }
    public int AcceptedSupportTargets { get; private set; }
    public int WeightedCandidateQueries { get; private set; }
    public int WeightedCandidateVertices { get; private set; }
    public float MaximumFootLateralShift { get; private set; }
    public float MaximumBendDegrees { get; private set; }

    public void Bind(CatActivity activity, Transform source)
    {
        Clear(); owner = activity; authored = source;movement=GetComponent<CatMovement>();
        ownerSurface=activity!=null?new CatMeshContactSurface.TargetSet(activity.transform):null;
        measuredOwnerRefinement=MeasuredOwnerClear;
        activityAnimation = GetComponent<CatActivityAnimation>();
        var animator = GetComponentInChildren<Animator>();
        visual = animator != null ? animator.transform : null;
        skin = animator != null ? animator.GetComponentInChildren<SkinnedMeshRenderer>() : null;
        if (owner == null || visual == null || skin == null) return;
        foreach (var collider in owner.GetComponentsInChildren<MeshCollider>(true))
            if (collider != null && collider.sharedMesh != null && !collider.isTrigger) solids.Add(collider);
        solidMatrices = new Matrix4x4[solids.Count];
        solidBounds = new Bounds[solids.Count];
        solidActive=new bool[solids.Count];solidMeshes=new Mesh[solids.Count];solidTransforms=new Transform[solids.Count];
        var tag = skin.GetComponentInParent<CatBreedVisualTag>();
        var profile = CatBreedCatalog.Load()?.Find(tag != null ? tag.BreedId : CatBreedCatalog.DefaultBreedId);
        if (profile?.ContactVertexIndices != null)
            foreach (int index in profile.ContactVertexIndices)
                if (index >= 0 && index < skin.sharedMesh.vertexCount) mask.Add(index);
        if (mask.Count == 0) { owner = null; return; }
        world = new Vector3[skin.sharedMesh.vertexCount]; paw = new int[world.Length]; limb = new int[world.Length];
        for (int i = 0; i < paw.Length; i++) paw[i] = limb[i] = -1;
        for (int i = 0; i < legs.Length; i++)
        {
            var indices = profile.SupportPawVertices(i);
            if (indices == null || indices.Count == 0) { owner = null; return; }
            foreach (int index in indices) if (index >= 0 && index < paw.Length) paw[index] = i;
            var limbIndices = profile.SupportLimbVertices(i);
            if(limbIndices==null||limbIndices.Count==0){owner=null;return;}
            foreach(int index in limbIndices)if(index>=0&&index<limb.Length)limb[index]=i;
            legs[i].index=i;weightedFirst[i]=-1;
            string side = (i & 1) == 0 ? "L" : "R";
            legs[i].upper = CatBreedVisualFactory.FindDescendant(transform, (i < 2 ? "DEF-upper_arm." : "DEF-thigh.") + side);
            legs[i].lower = CatBreedVisualFactory.FindDescendant(transform, (i < 2 ? "DEF-forearm." : "DEF-shin.") + side);
            legs[i].foot = CatBreedVisualFactory.FindDescendant(transform, (i < 2 ? "DEF-hand." : "DEF-foot.") + side);
            if (legs[i].upper == null || legs[i].lower == null || legs[i].foot == null) owner = null;
        }
        if(owner!=null) supportedSkin=CatSupportedLimbSkin.Bind(CatCareSkinCatalog.Load()?.Find(profile.Id),animator,skin);
    }

    void Update() => Restore();
    void LateUpdate()
    {
        if (!IsActive || skin == null || solids.Count == 0) { LastSolveMs = 0; return; }
        long began = System.Diagnostics.Stopwatch.GetTimestamp();
        bool previousBackfaces = Physics.queriesHitBackfaces;
        try { Physics.queriesHitBackfaces = true; SolveSupportedFrame(); }
        finally
        {
            Physics.queriesHitBackfaces = previousBackfaces;
            LastSolveMs = (System.Diagnostics.Stopwatch.GetTimestamp() - began) * 1000d / System.Diagnostics.Stopwatch.Frequency;
            MaximumSolveMs = Math.Max(MaximumSolveMs, LastSolveMs);
            SolveFrame = Time.frameCount; SolveCount++;
        }
    }
    void SolveSupportedFrame()
    {
        NumericPlanApplied=false;
        ownerDepthCache.Clear();
        CaptureOwnerGeometry();
        Vector3 rootBefore = transform.position;
        Quaternion headingBefore = transform.rotation;
        visualBefore = visual.localPosition;visualWorldBefore=visual.position;
        sourceVisualMatrix=visual.localToWorldMatrix;
        visualRotationBefore=visual.localRotation;visualWorldRotationBefore=visual.rotation;
        BodyRotation=Quaternion.identity;BodyPivot=Vector3.zero;BodyTiltDegrees=0;
        up = authored != null ? authored.up.normalized : Vector3.up;
        Vector3 support = activityAnimation.ContactSurface != null ? activityAnimation.ContactSurface.position : transform.position;
        if (owner is TubEdgeWalkActivity)
        {
            var rim = GetComponent<CatTubRimMotion>();
            if (rim != null) support.y = rim.HeightAt(transform.position);
        }
        reference = Vector3.Dot(support, up);
        reach = 0f; RayQueries = BakeCount = PlantedLegCount = 0;
        SupportCandidateQueries=AcceptedSupportTargets=WeightedCandidateQueries=WeightedCandidateVertices=0;
        CommonLiftCandidates=AcceptedCommonLifts=CommonLiftCalls=CommonLiftRecordedCandidates=0;
        CommonLiftReason="NotConsidered";CommonLiftResidual=CommonLiftAllowedByReach=CommonLiftSourceLimit=0;
        CommonLiftRejectLeg=-1;CommonLiftBlocker=null;
        VisualLift = RequiredLift = MaximumTopPenetration = MaximumLegResidual = MaximumFootLateralShift = MaximumBendDegrees = 0f;
        ReachLimited = false;environmentSynchronized=false;
        foreach (var leg in legs)
        {
            leg.upperLocal = leg.upper.localRotation; leg.lowerLocal = leg.lower.localRotation; leg.footLocal = leg.foot.localRotation;
            leg.worldFoot = leg.foot.rotation;
            leg.sourceUpper = leg.upper.position; leg.sourceLower = leg.lower.position; leg.sourceFoot = leg.foot.position;
            leg.upperLength = Vector3.Distance(leg.sourceUpper, leg.sourceLower);
            leg.lowerLength = Vector3.Distance(leg.sourceLower, leg.sourceFoot);
            leg.target = leg.sourceFoot;
            leg.sourceSole=leg.sourceFoot;leg.soleOffset=float.NegativeInfinity;
            leg.desiredLower=leg.sourceLower;leg.planted=leg.solve=leg.hasPawBounds=false;
            reach = Mathf.Max(reach, leg.upperLength + leg.lowerLength);
        }
        // A bind-skin profile alone does not make an unknown owner mesh
        // measurable. Retain the existing geometry-only foot/elbow fallback
        // until every enabled support solid has original triangle data.
        predictionReady=supportedSkin!=null&&HasMeasuredOwnerGeometry()&&supportedSkin.Capture();
        if(!predictionReady)CommonLiftReason="MissingPredictionGeometry";
        // The native gate and the supported refinement share one numeric plan.
        bool supportedRest=!activityAnimation.IsNativeJump&&activityAnimation.ContactSurface!=null&&
            (activityAnimation.CurrentPose==CatActivityPose.Sit||activityAnimation.CurrentPose==CatActivityPose.Sleep||
             activityAnimation.CurrentPose==CatActivityPose.SitDown||activityAnimation.CurrentPose==CatActivityPose.StandUp||
             activityAnimation.CurrentPose==CatActivityPose.TowelSettle||activityAnimation.CurrentPose==CatActivityPose.TowelWake);
        bool nativeSupport=activityAnimation.NativeHasOwnerSupport&&!(owner is TubEdgeWalkActivity);
        if(predictionReady&&(supportedRest||nativeSupport||owner is TubEdgeWalkActivity||activityAnimation.IsNativeJump&&activityAnimation.NativeJumpPhase<=CatJumpMotion.Takeoff)&&
            TryNumericSupportPlan(supportedSkin,support,transform.rotation,!activityAnimation.IsNativeJump||owner is TubEdgeWalkActivity,false,supportedRest||nativeSupport))
        {ApplyNumericSupportPlan(rootBefore,headingBefore);return;}
        Bake();
        // Establish contacts from this frame's actual distal skin. A source
        // swing paw has no floor constraint merely because its bone exists.
        foreach (int index in mask)
        {
            int p=paw[index];if(p<0)continue;var leg=legs[p];
            if(!leg.hasPawBounds){leg.sourcePawBounds=new Bounds(world[index],Vector3.zero);leg.hasPawBounds=true;}
            else leg.sourcePawBounds.Encapsulate(world[index]);
            float offset=Vector3.Dot(leg.sourceFoot-world[index],up);
            if(offset>leg.soleOffset){leg.soleOffset=offset;leg.sourceSole=world[index];}
        }
        foreach (var leg in legs)
        {
            if(TrySupportNear(leg.sourceSole,out var supportHit)&&Mathf.Abs(Vector3.Dot(leg.sourceSole-supportHit.point,up))<=.018f)
            {leg.planted=leg.solve=true;leg.supportPoint=supportHit.point;PlantedLegCount++;}
        }
        // A tilted paw can touch with another distal point while its lowest
        // corner hangs over an edge. Confirm that actual contact too; a single
        // sole-centre miss must not release a visibly planted source paw.
        foreach(int index in mask)
        {
            int p=paw[index];if(p<0||legs[p].planted)continue;
            if(TrySupportNear(world[index],out var contact)&&Mathf.Abs(Vector3.Dot(world[index]-contact.point,up))<=.018f)
            {legs[p].planted=legs[p].solve=true;legs[p].supportPoint=contact.point;PlantedLegCount++;}
        }
        adjusted = true;
        // Three complete measured passes, not seven all-vertex upward pushes.
        // Limbs have their own rotational constraint. Their folded skin must
        // not force an unrelated whole-body lift while a paw remains planted.
        for (int iteration = 0; iteration < 3; iteration++)
        {
            float previousLift=VisualLift;
            for(int i=0;i<4;i++)
            {
                iterationRotations[i*3]=legs[i].upper.localRotation;iterationRotations[i*3+1]=legs[i].lower.localRotation;iterationRotations[i*3+2]=legs[i].foot.localRotation;
                iterationGoals[i*2]=legs[i].target;iterationGoals[i*2+1]=legs[i].desiredLower;
                iterationStates[i*2]=legs[i].planted;iterationStates[i*2+1]=legs[i].solve;
                iterationWitnesses[i]=weightedFirst[i];
            }
            float bodyCorrection=0;Array.Clear(pawCorrection,0,4);Array.Clear(limbCorrection,0,4);
            for(int i=0;i<4;i++)pawWitness[i]=limbWitness[i]=-1;
            foreach (int index in mask)
            {
                if (!TryTop(world[index], out var hit)) continue;
                float gap = Vector3.Dot(hit.point - world[index], up) + .008f;
                int p=paw[index],l=limb[index];
                // A long upward ray through a sloped rim is not its penetration
                // depth. Rank real distal/limb witnesses by the original mesh.
                float depth=(p>=0||l>=0)?MeasuredDepth(world[index],hit):gap;
                if(depth<=.002f)continue;
                if(p>=0){if(depth>pawCorrection[p]){pawCorrection[p]=depth;pawWitness[p]=index;}}
                else if(l>=0){if(depth>limbCorrection[l]){limbCorrection[l]=depth;limbWitness[l]=index;}}
                else bodyCorrection=Mathf.Max(bodyCorrection,gap);
            }
            bool changed=bodyCorrection>.0005f;
            for(int i=0;i<4;i++)
            {
                var leg=legs[i];
                Vector3 supportedTarget=leg.target;
                bool supportedEscape=pawWitness[i]>=0&&TrySupportedPawEscape(leg,world[pawWitness[i]],out supportedTarget);
                if(supportedEscape)
                {
                    leg.target=supportedTarget;leg.solve=true;changed=true;AcceptedSupportTargets++;
                    if(!leg.planted){leg.planted=true;PlantedLegCount++;}
                }
                if(!supportedEscape&&pawWitness[i]>=0&&TryEscape(world[pawWitness[i]],out var escape))
                {
                    Vector3 requested=leg.target+escape;
                    // A lateral avoidance keeps a real upper support below the
                    // paw. The underside of a raised ornament is never chosen
                    // as a new support height or a reason for a 10 cm step.
                    if(TrySupportNear(requested-up*leg.soleOffset,out var contact))
                    {
                        requested+=up*(Vector3.Dot(contact.point-requested,up)+leg.soleOffset+.008f);
                        if(!leg.planted){leg.planted=true;PlantedLegCount++;}leg.supportPoint=contact.point;
                    }
                    else if(leg.planted)
                    {
                        Vector3 lateral=Vector3.ProjectOnPlane(escape,up);
                        if(TrySupportNear(leg.target+lateral-up*leg.soleOffset,out contact))
                        {requested=leg.target+lateral;requested+=up*(Vector3.Dot(contact.point-requested,up)+leg.soleOffset+.008f);leg.supportPoint=contact.point;}
                        else requested=leg.target;
                    }
                    Vector3 lateralShift=Vector3.ProjectOnPlane(requested-leg.sourceFoot,up);
                    if(lateralShift.magnitude>leg.lowerLength*.35f)
                    {
                        requested-=lateralShift;
                        requested+=lateralShift.normalized*(leg.lowerLength*.35f);
                        if(leg.planted)
                        {
                            if(TrySupportNear(requested-up*leg.soleOffset,out var boundedSupport))
                                requested+=up*(Vector3.Dot(boundedSupport.point-requested,up)+leg.soleOffset+.008f);
                            else requested=leg.target;
                        }
                    }
                    if(!ValidateLowerTarget(leg,ref requested))requested=leg.target;
                    leg.target=requested;leg.solve=true;changed=true;
                }
                if(limbWitness[i]>=0&&TryEscape(world[limbWitness[i]],out var limbEscape))
                {
                    leg.desiredLower=leg.lower.position+limbEscape;leg.solve=true;changed=true;
                    // A fixed foot fixes the elbow-circle radius and axial
                    // position. A real nearby support can provide the missing
                    // degree of freedom without lengthening either bone.
                    // The penetrating point can belong mainly to the shin or
                    // forearm rather than the distal mask. Its measured side
                    // exits are equally valid bounded foot-target candidates.
                    if(leg.planted&&predictionReady&&TrySupportedPawEscape(leg,world[limbWitness[i]],out var limbTarget))
                    {leg.target=limbTarget;AcceptedSupportTargets++;}
                    if(leg.planted&&TrySupportedElbowTarget(leg,out var elbowTarget))
                    {leg.target=elbowTarget;AcceptedSupportTargets++;}
                }
            }
            // A limb-only intersection previously requested no body lift even
            // when every fixed-foot elbow candidate remained inside a rim.
            // Try the missing common degree of freedom using exact weighted
            // source skin, real planted targets, and the same anatomical limits.
            if(bodyCorrection<=.0005f&&predictionReady&&TryCommonLift(out float commonLift))
            {bodyCorrection=commonLift;changed=true;AcceptedCommonLifts++;}
            if(!changed)break;
            RequiredLift=Mathf.Max(RequiredLift,VisualLift+bodyCorrection);
            foreach(var leg in legs)if(leg.planted)AdaptSupportForLift(leg,bodyCorrection);
            float allowed=float.PositiveInfinity;
            foreach(var leg in legs)if(leg.planted)
            {
                Vector3 delta=leg.upper.position-leg.target;float length=leg.upperLength+leg.lowerLength-.001f;
                allowed=Mathf.Min(allowed,Mathf.Sqrt(Mathf.Max(0,length*length-Vector3.ProjectOnPlane(delta,up).sqrMagnitude))-Vector3.Dot(delta,up));
            }
            float correction=Mathf.Min(bodyCorrection,Mathf.Max(0,allowed));
            VisualLift+=correction;visual.position+=up*correction;
            foreach(var leg in legs)
            {
                if(!leg.planted)leg.target+=up*correction;
                leg.desiredLower+=up*correction;
                if(leg.solve)Solve(leg);
                MaximumFootLateralShift=Mathf.Max(MaximumFootLateralShift,Vector3.ProjectOnPlane(leg.target-leg.sourceFoot,up).magnitude);
            }
            ReachLimited |= correction + .001f < bodyCorrection;
            bool poseChanged=Mathf.Abs(VisualLift-previousLift)>.000001f;
            for(int i=0;i<4&&!poseChanged;i++)poseChanged=
                RotationChanged(iterationRotations[i*3],legs[i].upper.localRotation)||
                RotationChanged(iterationRotations[i*3+1],legs[i].lower.localRotation)||
                RotationChanged(iterationRotations[i*3+2],legs[i].foot.localRotation)||
                (iterationGoals[i*2]-legs[i].target).sqrMagnitude>1e-12f||
                (iterationGoals[i*2+1]-legs[i].desiredLower).sqrMagnitude>1e-12f||
                iterationStates[i*2]!=legs[i].planted||iterationStates[i*2+1]!=legs[i].solve||
                iterationWitnesses[i]!=weightedFirst[i];
            // Stop only when pose AND goals/witnesses are unchanged. A newly
            // chosen goal can still produce movement on the following pass.
            if(!poseChanged)break;
            Bake();
            if(iteration==1&&!supportedRest&&!activityAnimation.IsNativeJump&&predictionReady&&TryNumericSupportPlan(supportedSkin,support,transform.rotation,true,true))
            {ApplyNumericSupportPlan(rootBefore,headingBefore);return;}
        }
        MaximumTopPenetration = 0f; MeasuredVertices = 0;
        foreach (int index in mask)
            if (TryTop(world[index], out var hit))
            { MeasuredVertices++; MaximumTopPenetration = Mathf.Max(MaximumTopPenetration, Vector3.Dot(hit.point - world[index], up)); }
        foreach (var leg in legs)if(leg.solve)
            MaximumLegResidual = Mathf.Max(MaximumLegResidual, Vector3.Distance(leg.foot.position, leg.target));
        MaximumRootShift = Mathf.Max(MaximumRootShift, Vector3.Distance(rootBefore, transform.position));
        // Deliberately do not modify root/heading to make an impossible pose
        // appear valid. Native QA reports reach limits and the actual residual.
        Debug.Assert(Quaternion.Angle(headingBefore, transform.rotation) < .001f);
    }

    bool HasMeasuredOwnerGeometry()
    {
        bool any=false;
        foreach(var solid in solids)
        {
            if(solid==null||!solid.enabled||!solid.gameObject.activeInHierarchy)continue;
            if(!(solid is MeshCollider mesh)||mesh.sharedMesh==null||
                !CatMeshContactSurface.HasGeometry(mesh.sharedMesh))return false;
            any=true;
        }
        return any;
    }
    static bool RotationChanged(Quaternion first,Quaternion second)
    {
        float x=first.x-second.x,y=first.y-second.y,z=first.z-second.z,w=first.w-second.w;
        return x*x+y*y+z*z+w*w>1e-12f;
    }

    // One synchronous solve cannot yield to a collider edit. Capture native
    // properties once; thousands of skin points then share this exact current
    // geometry. Neither inside results nor scene permissions survive a solve.
    void CaptureOwnerGeometry()
    {
        bool moved=false;
        for(int i=0;i<solids.Count;i++)
        {
            var solid=solids[i];
            solidActive[i]=solid!=null&&solid.enabled&&solid.gameObject.activeInHierarchy;
            if(!solidActive[i])continue;
            var target=solid.transform;var matrix=target.localToWorldMatrix;
            moved|=!solidMatrices[i].Equals(matrix);
            solidMatrices[i]=matrix;solidTransforms[i]=target;
            solidMeshes[i]=(solid as MeshCollider)?.sharedMesh;
        }
        if(moved)Physics.SyncTransforms();
        for(int i=0;i<solids.Count;i++)if(solidActive[i])solidBounds[i]=solids[i].bounds;
    }

    void Bake()
    {
        if (sample == null) sample = new Mesh { name = "Measured supported cat skin" };
        BakeCount++;topCache.Clear();
        skin.BakeMesh(sample, true); sample.GetVertices(vertices);
        foreach (int index in mask) world[index] = skin.transform.TransformPoint(vertices[index]);
    }
    bool TryTop(Vector3 point, out RaycastHit result)
    {
        if(topCache.TryGetValue(point,out var cached)){result=cached.hit;return cached.found;}
        result = default; bool found = false; float best = 0f;
        for (int i = 0; i < solids.Count; i++)
        {
            var solid = solids[i];
            if (!solidActive[i] || !solidBounds[i].Contains(point)) continue;
            // With backfaces enabled an interior point first exits through the
            // upward face. A point outside/below a closed solid first meets a
            // downward underside; projecting the distant top would falsely
            // lift it by the entire thickness of the furniture.
            RayQueries++;
            if (!solid.Raycast(new Ray(point, up), out var hit, reach + .01f)) continue;
            Vector3 normal=hit.normal;
            if(solid is MeshCollider&&!CatMeshContactSurface.TryOriginalTriangleNormal(solidMeshes[i],solidMatrices[i],hit.triangleIndex,out normal))continue;
            if (Vector3.Dot(normal, up) <= .0001f || hit.distance <= best) continue;
            result = hit; best = hit.distance; found = true;
        }
        topCache[point]=new TopResult{found=found,hit=result};return found;
    }
    bool TrySupportNear(Vector3 sole,out RaycastHit result)
    {
        result=default;bool found=false;float nearest=float.PositiveInfinity;
        for(int i=0;i<solids.Count;i++)
        {
            var solid=solids[i];if(!solidActive[i])continue;
            if((solidBounds[i].ClosestPoint(sole)-sole).sqrMagnitude>.09f*.09f)continue;
            RayQueries++;
            if(!solid.Raycast(new Ray(sole+up*.08f,-up),out var hit,.14f)||Vector3.Dot(hit.normal,up)<.35f)continue;
            float distance=Mathf.Abs(Vector3.Dot(hit.point-sole,up));
            if(distance>=nearest)continue;nearest=distance;result=hit;found=true;
        }
        return found;
    }
    bool TryEscape(Vector3 point,out Vector3 correction)
    {
        correction=Vector3.zero;if(!TryTop(point,out var interior))return false;
        float best=interior.distance;Vector3 bestDirection=up;
        // An interior limb may be under a curved rim. Its closest legitimate
        // exit can be lateral/downward; only body lift uses the upper exit.
        // Six bounded tests are done for four deepest limb/paw witnesses, not
        // for every vertex on every iteration.
        Test(-up);Test(transform.right);Test(-transform.right);Test(transform.forward);Test(-transform.forward);
        correction=bestDirection*(best+.008f);return true;
        void Test(Vector3 direction)
        {
            RayQueries++;
            if(interior.collider.Raycast(new Ray(point,direction),out var hit,best+.0001f)&&
                IsOriginalExit(interior.collider,hit,direction)&&hit.distance<best)
            {best=hit.distance;bestDirection=direction;}
        }
    }
    void AdaptSupportForLift(Leg leg,float lift)
    {
        if(lift<=.0005f)return;
        Vector3 future=leg.upper.position+up*lift,delta=future-leg.target;
        float length=leg.upperLength+leg.lowerLength-.001f;
        if(delta.sqrMagnitude<=length*length)return;
        float height=Vector3.Dot(delta,up);Vector3 horizontal=Vector3.ProjectOnPlane(delta,up);
        float maximumHorizontal=Mathf.Sqrt(Mathf.Max(0,length*length-height*height));
        float shift=Mathf.Min(Mathf.Max(0,horizontal.magnitude-maximumHorizontal),leg.lowerLength*.35f);
        if(shift<=.0001f||horizontal.sqrMagnitude<1e-10f)return;
        Vector3 target=leg.target+horizontal.normalized*shift;
        if(!TrySupportNear(target-up*leg.soleOffset,out var supportHit))return;
        target+=up*(Vector3.Dot(supportHit.point-target,up)+leg.soleOffset+.008f);
        // Small source-sized adjustment on a measured upper surface; reject a
        // remote ornament or a step whose footprint lies outside that surface.
        if(Vector3.ProjectOnPlane(target-leg.sourceFoot,up).magnitude>leg.lowerLength*.35f+.0001f)return;
        if(!ValidateLowerTarget(leg,ref target))return;
        leg.target=target;leg.supportPoint=supportHit.point;leg.solve=true;
    }
    bool TrySupportedPawEscape(Leg leg,Vector3 witness,out Vector3 target)
    {
        target=leg.target;if(!TryTop(witness,out var interior))return false;
        bool found=false;float closest=float.PositiveInfinity;
        float skinScore=predictionReady?SkinScore(leg,leg.target):float.PositiveInfinity;
        // Every direction comes from this actual enclosing collider. A blocked
        // shortest/downward exit does not discard valid supported side exits.
        for(int direction=0;direction<6;direction++)
        {
            Vector3 axis=direction==0?up:direction==1?-up:direction==2?transform.right:
                direction==3?-transform.right:direction==4?transform.forward:-transform.forward;
            RayQueries++;
            if(!interior.collider.Raycast(new Ray(witness,axis),out var exit,reach+.01f)||
                !IsOriginalExit(interior.collider,exit,axis))continue;
            Vector3 candidate=leg.target+axis*(exit.distance+.008f);
            if(!TrySupportCandidate(leg,ref candidate))continue;
            float distance=(candidate-leg.target).sqrMagnitude;
            if(distance<.00000001f)continue;
            if(predictionReady)
            {
                float next=SkinScore(leg,candidate,skinScore);
                // Choose improvement in actual weighted skin, not merely the
                // shortest foot displacement through a curved solid.
                if(next+.0001f>=skinScore)continue;
                skinScore=next;
            }
            else if(distance>=closest)continue;
            closest=distance;target=candidate;found=true;
        }
        return found;
    }
    bool TrySupportedElbowTarget(Leg leg,out Vector3 target)
    {
        target=leg.target;
        Vector3 shoulder=leg.upper.position;
        Vector3 desired=leg.desiredLower-shoulder;
        if(desired.sqrMagnitude<1e-12f)return false;
        // Project onto the real upper-link sphere, then choose the nearest
        // point of the lower-link circle at the current supported foot height.
        Vector3 elbow=shoulder+desired.normalized*leg.upperLength;
        float height=Vector3.Dot(leg.target-elbow,up);
        Vector3 circle=elbow+up*height;
        Vector3 radial=Vector3.ProjectOnPlane(leg.target-circle,up);
        if(radial.sqrMagnitude<1e-12f)radial=Vector3.ProjectOnPlane(leg.sourceFoot-leg.sourceUpper,up);
        if(radial.sqrMagnitude<1e-12f)return false;
        float radius=Mathf.Sqrt(Mathf.Max(0,leg.lowerLength*leg.lowerLength-height*height));
        Vector3 minimum=circle+radial.normalized*radius;
        Vector3 first=Vector3.ProjectOnPlane(minimum-leg.target,up);
        Vector3 second=Vector3.ProjectOnPlane(leg.desiredLower-leg.lower.position,up);
        float score=predictionReady?SkinScore(leg,leg.target):(PredictLower(leg,leg.target)-leg.desiredLower).sqrMagnitude;
        bool found=false;
        if(predictionReady&&score<=.002f)return false;
        if(predictionReady&&TryWeightedSurfaceStep(leg,score,weightedWitnessSlot,out target))return true;
        // At most eight pure candidates; no trial Animator sampling or Bake.
        for(int ray=0;ray<2;ray++)
        for(int step=0;step<4;step++)
        {
            Vector3 delta=(ray==0?first:second)*(step==0?.5f:step==1?1f:step==2?1.5f:2f);
            if(delta.sqrMagnitude<.00000001f)continue;
            Vector3 candidate=leg.target+delta;
            if(!TrySupportCandidate(leg,ref candidate,false)||!CandidateSceneClear(leg,candidate))continue;
            float next=predictionReady?SkinScore(leg,candidate,score):(PredictLower(leg,candidate)-leg.desiredLower).sqrMagnitude;
            if(next+(predictionReady?.0001f:1e-8f)>=score)continue;
            score=next;target=candidate;found=true;
        }
        return found;
    }
    bool TryWeightedSurfaceStep(Leg leg,float baseline,int witnessSlot,out Vector3 target)
    {
        target=leg.target;
        if(witnessSlot<0||float.IsInfinity(baseline)||baseline<=.002f||
            !supportedSkin.TryPose(leg.index,leg.target,leg.desiredLower,up*VisualLift,out var initial))return false;
        Vector3 point=supportedSkin.Point(leg.index,witnessSlot,initial);
        if(!TryTop(point,out var interior)||!(interior.collider is MeshCollider mesh)||
            !TryOwnerMetric(mesh,point,out float depth,out var normal)||depth<=.002f)return false;
        // Moving a folded elbow's foot in the exit direction does not move
        // its skin in that direction. Measure the actual weighted response
        // along two real support tangents before choosing a bounded step.
        float step=Mathf.Min(.01f,leg.lowerLength*.1f);
        Vector3 right=Vector3.ProjectOnPlane(transform.right,up).normalized;
        Vector3 forward=Vector3.Cross(right,up).normalized;
        float x=Response(right),z=Response(forward),square=x*x+z*z;
        if(square<.0001f)return false;
        Vector3 direction=(right*x+forward*z)*((depth+.002f)/square);
        float limit=leg.lowerLength*.35f;
        if(direction.magnitude>limit)direction=direction.normalized*limit;
        bool found=false;float best=baseline;
        for(int choice=0;choice<3;choice++)
        {
            Vector3 candidate=leg.target+direction*(choice==0?.5f:choice==1?1f:1.5f);
            if(!TrySupportCandidate(leg,ref candidate,false)||!CandidateSceneClear(leg,candidate))continue;
            float score=SkinScore(leg,candidate,best);
            if(score+.0001f>=best)continue;
            target=candidate;best=score;found=true;
            if(best<=.002f)break;
        }
        return found;

        float Response(Vector3 axis)
        {
            bool positive=Sample(axis,1,out float plus),negative=Sample(axis,-1,out float minus);
            return positive&&negative?(plus-minus)/(2*step):positive?plus/step:negative?-minus/step:0;
        }
        bool Sample(Vector3 axis,float sign,out float displacement)
        {
            displacement=0;Vector3 candidate=leg.target+axis*(sign*step);
            if(!TrySupportCandidate(leg,ref candidate,false)||
                !supportedSkin.TryPose(leg.index,candidate,leg.desiredLower,up*VisualLift,out var pose))return false;
            displacement=Vector3.Dot(supportedSkin.Point(leg.index,witnessSlot,pose)-point,normal);return true;
        }
    }

    bool TryOwnerMetric(MeshCollider mesh,Vector3 point,out float depth,out Vector3 normal)
    {
        int index=solids.IndexOf(mesh);
        return index>=0
            ?CatMeshContactSurface.TryMetric(solidMeshes[index],solidTransforms[index],solidMatrices[index],point,out depth,out normal)
            :CatMeshContactSurface.TryMetric(mesh.sharedMesh,mesh.transform,point,out depth,out normal);
    }
    bool IsOriginalExit(Collider solid,RaycastHit hit,Vector3 direction)
    {
        Vector3 normal=hit.normal;int index=solids.IndexOf(solid);
        if(solid is MeshCollider&&(index<0||!CatMeshContactSurface.TryOriginalTriangleNormal(solidMeshes[index],solidMatrices[index],hit.triangleIndex,out normal)))return false;
        return Vector3.Dot(normal,direction)>.0001f;
    }
    float MeasuredDepth(Vector3 point,RaycastHit exit)
    {
        if(exit.collider is MeshCollider mesh&&TryOwnerMetric(mesh,point,out float depth,out _))return depth;
        // Unknown geometry cannot manufacture a smaller penetration.
        return exit.distance;
    }
    float SkinScore(Leg leg,Vector3 target,float stopAt=float.PositiveInfinity)
    {
        WeightedCandidateQueries++;weightedWitnessSlot=-1;
        if(!predictionReady||!supportedSkin.TryPose(leg.index,target,leg.desiredLower,up*VisualLift,out var pose))return float.PositiveInfinity;
        float maximum=0;
        // Reject a bad candidate at its real failing skin point before scanning
        // hundreds of already-clear vertices. This does not cache permission.
        int witness=pawWitness[leg.index]>=0?pawWitness[leg.index]:limbWitness[leg.index];
        // Reorder only: previously deepest affected skin is checked first.
        // Every point and collider query remains current; no clear result is cached.
        int first=weightedFirst[leg.index]>=0?weightedFirst[leg.index]:witness>=0?supportedSkin.Slot(witness):-1;
        if(first>=0)
        {WeightedCandidateVertices++;maximum=OwnerDepth(supportedSkin.Point(leg.index,first,pose));weightedWitnessSlot=weightedFirst[leg.index]=first;if(maximum>=stopAt)return maximum;}
        var affected=supportedSkin.Vertices(leg.index);
        for(int slot=0;slot<affected.Count;slot++)
        {
            int vertex=affected[slot];if(vertex==first)continue;WeightedCandidateVertices++;
            float depth=OwnerDepth(supportedSkin.Point(leg.index,vertex,pose));
            if(depth>maximum){maximum=depth;weightedWitnessSlot=weightedFirst[leg.index]=vertex;}
            if(maximum>=stopAt)return maximum;
        }
        return maximum;
    }
    float OwnerDepth(Vector3 point)
    {
        if(ownerDepthCache.TryGetValue(point,out float cachedDepth))return cachedDepth;
        float maximum=0;
        for(int i=0;i<solids.Count;i++)
        {
            var solid=solids[i];if(!solidActive[i]||!solidBounds[i].Contains(point))continue;
            RayQueries++;
            bool above=solid.Raycast(new Ray(point,up),out var hit,reach+.01f)&&IsOriginalExit(solid,hit,up);
            // At a folded cloth seam the upward ray can meet a different
            // underside first. Two independent outward side exits establish
            // that the point is still inside that same closed support.
            if(!above&&!TrySideInterior(i,point,out hit))continue;
            maximum=Mathf.Max(maximum,MeasuredDepth(point,hit));
        }
        ownerDepthCache[point]=maximum;
        return maximum;
    }
    bool TrySideInterior(int index,Vector3 point,out RaycastHit hit)
    {
        hit=default;var solid=solids[index];
        if(!solidActive[index]||!solidBounds[index].Contains(point))return false;
        float distance=solidBounds[index].size.magnitude+.01f;
        RayQueries++;
        if(!solid.Raycast(new Ray(point,Vector3.right),out var first,distance)||!IsOriginalExit(solid,first,Vector3.right))return false;
        RayQueries++;
        if(!solid.Raycast(new Ray(point,Vector3.forward),out var second,distance)||!IsOriginalExit(solid,second,Vector3.forward))return false;
        hit=first.distance<second.distance?first:second;return true;
    }
    bool TryMeasuredInterior(Vector3 point,out RaycastHit hit)
    {
        if(TryTop(point,out hit))return true;
        for(int i=0;i<solids.Count;i++)if(TrySideInterior(i,point,out hit))return true;
        hit=default;return false;
    }
    bool TryCommonLift(out float lift)
    {
        CommonLiftCalls++;CommonLiftReason="Evaluating";
        lift=0;float residual=0,allowed=float.PositiveInfinity,sourceLimit=float.PositiveInfinity;bool planted=false;
        for(int i=0;i<4;i++)
        {
            residual=Mathf.Max(residual,Mathf.Max(pawCorrection[i],limbCorrection[i]));
            var leg=legs[i];sourceLimit=Mathf.Min(sourceLimit,leg.lowerLength*.35f);
            if(!leg.planted)continue;planted=true;
            Vector3 delta=leg.upper.position-leg.target;float length=leg.upperLength+leg.lowerLength-.001f;
            allowed=Mathf.Min(allowed,Mathf.Sqrt(Mathf.Max(0,length*length-Vector3.ProjectOnPlane(delta,up).sqrMagnitude))-Vector3.Dot(delta,up));
        }
        CommonLiftResidual=residual;CommonLiftAllowedByReach=allowed;CommonLiftSourceLimit=sourceLimit-VisualLift;
        if(!planted){CommonLiftReason="NoPlantedPaw";return false;}
        if(residual<=.002f){CommonLiftReason="NoMeasuredResidual";return false;}
        allowed=Mathf.Min(allowed,sourceLimit-VisualLift);
        if(allowed<=.0005f){CommonLiftReason="ReachOrSourceLimit";return false;}
        // The new target positions were already selected above. If their
        // combined exact skin is clear, do not add an unnecessary body lift.
        if(CombinedScore(0,.002f)<=.002f){CommonLiftReason="TargetsAlreadyClear";return false;}
        float requested=Mathf.Min(allowed,residual+.008f),previous=0;
        for(int choice=0;choice<3;choice++)
        {
            float candidate=choice==0?requested*.5f:choice==1?requested:allowed;
            if(candidate<=previous+.0001f)continue;previous=candidate;CommonLiftCandidates++;
            float depth=CombinedScore(candidate,.002f);
            if(CommonLiftRecordedCandidates<commonLiftHeights.Length)
            {commonLiftHeights[CommonLiftRecordedCandidates]=candidate;commonLiftDepths[CommonLiftRecordedCandidates++]=depth;}
            if(depth>.002f){if(!float.IsInfinity(depth))CommonLiftReason="OwnerSkinBlocked";continue;}
            // Exact weighted owner skin is clear. All other scene geometry
            // receives the conservative full-skin box without an exclusion.
            if(!environmentSynchronized){Physics.SyncTransforms();environmentSynchronized=true;}
            var box=combinedBounds;box.Expand(.002f);combinedOwnerDepth=depth;
            combinedQuery[0]=new CatBodyGuardBox{centre=box.center,halfExtents=box.extents,rotation=Quaternion.identity};
            if(movement==null||!movement.IsInteractionBoxesClear(combinedQuery,.002f,measuredOwnerRefinement))
            {CommonLiftReason="SceneBoxBlocked";continue;}
            CommonLiftReason="Accepted";lift=candidate;return true;
        }
        return false;
    }
    bool MeasuredOwnerClear(int index,Collider solid)
    {
        bool allowed=combinedOwnerDepth<=.002f&&solids.Contains(solid);
        if(!allowed)CommonLiftBlocker=solid;
        return allowed;
    }
    float CombinedScore(float extra,float stopAt)
    {
        Vector3 translation=up*(VisualLift+extra);
        for(int i=0;i<4;i++)
        {
            var leg=legs[i];Vector3 target=leg.target+(leg.planted?Vector3.zero:up*extra);
            if(leg.solve||leg.planted)
            {
                float distance=Vector3.Distance(leg.sourceUpper+translation,target);
                if(distance>=leg.upperLength+leg.lowerLength-.0001f||distance<=Mathf.Abs(leg.upperLength-leg.lowerLength)+.0001f)
                {CommonLiftReason="CandidateLimbReach";CommonLiftRejectLeg=i;return float.PositiveInfinity;}
                if(!supportedSkin.TryPose(i,target,leg.desiredLower+up*extra,translation,out combinedPoses[i]))
                {CommonLiftReason="CandidatePoseUnavailable";CommonLiftRejectLeg=i;return float.PositiveInfinity;}
            }
            else combinedPoses[i]=supportedSkin.SourcePose(i,translation);
        }
        float maximum=0;bool first=true;
        // Test the known bad points first; no permission is retained between
        // candidates, phases, frames, transforms or moving scene obstacles.
        for(int i=0;i<4;i++)
        {
            int witness=limbWitness[i]>=0?limbWitness[i]:pawWitness[i];int slot=witness>=0?supportedSkin.Slot(witness):-1;
            if(slot<0)continue;WeightedCandidateVertices++;
            maximum=Mathf.Max(maximum,OwnerDepth(supportedSkin.CombinedPoint(slot,combinedPoses,translation)));
            if(maximum>stopAt)return maximum;
        }
        for(int slot=0;slot<supportedSkin.VertexCount;slot++)
        {
            Vector3 point=supportedSkin.CombinedPoint(slot,combinedPoses,translation);WeightedCandidateVertices++;
            if(first){combinedBounds=new Bounds(point,Vector3.zero);first=false;}else combinedBounds.Encapsulate(point);
            maximum=Mathf.Max(maximum,OwnerDepth(point));if(maximum>stopAt)return maximum;
        }
        return maximum;
    }
    Vector3 PredictLower(Leg leg,Vector3 target)
    {
        Vector3 origin=leg.upper.position,delta=target-origin;
        float a=leg.upperLength,b=leg.lowerLength;
        float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);
        Vector3 axis=delta.normalized;
        Vector3 sourceAxis=(leg.sourceFoot-leg.sourceUpper).normalized;
        Vector3 sourceBend=Vector3.ProjectOnPlane(leg.sourceLower-leg.sourceUpper,sourceAxis);
        if(sourceBend.sqrMagnitude<.000025f)
        {
            Quaternion sourceRotation=leg.upper.parent!=null?leg.upper.parent.rotation*leg.upperLocal:leg.upperLocal;
            sourceBend=Vector3.ProjectOnPlane(sourceRotation*Vector3.forward,sourceAxis);
        }
        Vector3 bend=Quaternion.FromToRotation(sourceAxis,axis)*sourceBend.normalized;
        Vector3 wanted=Vector3.ProjectOnPlane(leg.desiredLower-origin,axis);
        if(wanted.sqrMagnitude>.0000001f)bend=Vector3.RotateTowards(bend,wanted.normalized,25f*Mathf.Deg2Rad,0f).normalized;
        float along=(a*a-b*b+distance*distance)/(2f*distance);
        return origin+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
    }
    bool TrySupportCandidate(Leg leg,ref Vector3 target,bool checkScene=true)
    {
        SupportCandidateQueries++;
        Vector3 lateral=Vector3.ProjectOnPlane(target-leg.sourceFoot,up);
        float maximum=leg.lowerLength*.35f;
        if(lateral.magnitude>maximum)target+=lateral.normalized*maximum-lateral;
        if(!TrySupportNear(leg.sourceSole+target-leg.sourceFoot,out var contact))return false;
        target+=up*(Vector3.Dot(contact.point-(leg.sourceSole+target-leg.sourceFoot),up)+.008f);
        if(predictionReady)
        {
            float raise=0;
            foreach(int slot in supportedSkin.Vertices(leg.index))
            {
                int index=supportedSkin.SourceIndex(slot);if(paw[index]!=leg.index)continue;
                Vector3 point=supportedSkin.SourcePoint(slot)+target-leg.sourceFoot;
                if(TryTop(point,out var top))raise=Mathf.Max(raise,Vector3.Dot(top.point-point,up)+.008f);
            }
            if(raise>maximum)return false;
            target+=up*raise;
        }
        if(!ValidateLowerTarget(leg,ref target))return false;
        float distance=Vector3.Distance(leg.upper.position,target);
        if(distance>=leg.upperLength+leg.lowerLength-.0001f||distance<=Mathf.Abs(leg.upperLength-leg.lowerLength)+.0001f)return false;
        if(Vector3.ProjectOnPlane(target-leg.sourceFoot,up).magnitude>maximum+.000001f)return false;
        return !checkScene||CandidateSceneClear(leg,target);
    }
    bool CandidateSceneClear(Leg leg,Vector3 target)
    {
        if(movement==null||!leg.hasPawBounds)return false;
        if(!environmentSynchronized){Physics.SyncTransforms();environmentSynchronized=true;}
        // Every newly selected support target also clears the current scene.
        // This conservative translated source-skin envelope is a refusal gate,
        // not a substitute for the following actual mixed-weight skin bake.
        Bounds envelope=leg.sourcePawBounds;envelope.center+=target-leg.sourceFoot;envelope.Expand(.002f);
        lowerTargetBox[0]=new CatBodyGuardBox{centre=envelope.center,halfExtents=envelope.extents,rotation=Quaternion.identity};
        // Refine only this owner's conservative box with the current exact
        // weighted skin. Unrelated obstacles retain the complete box gate.
        combinedOwnerDepth=float.PositiveInfinity;
        if(predictionReady&&supportedSkin.TryPose(leg.index,target,leg.desiredLower,up*VisualLift,out var pose))
        {
            combinedOwnerDepth=0;
            foreach(int slot in supportedSkin.Vertices(leg.index))
            {
                if(paw[supportedSkin.SourceIndex(slot)]!=leg.index)continue;
                combinedOwnerDepth=Mathf.Max(combinedOwnerDepth,OwnerDepth(supportedSkin.Point(leg.index,slot,pose)));
                if(combinedOwnerDepth>.002f)break;
            }
        }
        return movement.IsInteractionBoxesClear(lowerTargetBox,.002f,measuredOwnerRefinement);
    }
    bool ValidateLowerTarget(Leg leg,ref Vector3 target)
    {
        if(Vector3.Dot(target-leg.sourceFoot,up)>=-.00001f)return true;
        if(movement==null||!leg.hasPawBounds)return false;
        if(!environmentSynchronized){Physics.SyncTransforms();environmentSynchronized=true;}
        // This is the source's visible sole, not a foot-bone height or the
        // activity's raised support plane. Include the real scene floor and
        // other furniture, not only this activity owner's mesh colliders.
        Vector3 candidateSole=leg.sourceSole+(target-leg.sourceFoot);
        float drop=Mathf.Max(0,Vector3.Dot(leg.sourceSole-candidateSole,up));
        int count=Physics.RaycastNonAlloc(new Ray(candidateSole+up*(drop+.025f),-up),lowerSupportHits,
            drop+.085f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        RayQueries++;
        if(count==lowerSupportHits.Length)return false; // unordered overflow cannot prove support
        float nearest=float.PositiveInfinity;RaycastHit supportHit=default;bool found=false;
        for(int i=0;i<count;i++)
        {
            var hit=lowerSupportHits[i];var collider=hit.collider;
            if(collider==null||collider.transform.IsChildOf(transform)||Vector3.Dot(hit.normal,up)<.35f)continue;
            if(hit.distance>=nearest)continue;nearest=hit.distance;supportHit=hit;found=true;
        }
        if(!found)return false;
        // Never place any source sole point below a real upward support.
        target+=up*Mathf.Max(0,Vector3.Dot(supportHit.point-candidateSole,up)+.008f);
        Bounds envelope=leg.sourcePawBounds;envelope.center+=target-leg.sourceFoot;envelope.Expand(.002f);
        lowerTargetBox[0]=new CatBodyGuardBox{centre=envelope.center,halfExtents=envelope.extents,rotation=Quaternion.identity};
        // A support ray alone says nothing about a neighbouring wall. Reuse
        // current scene filtering, with exact weighted paw skin for the owner
        // and the unchanged conservative box for every other obstacle.
        return CandidateSceneClear(leg,target);
    }
    void Solve(Leg leg)
    {
        // Restore source angles before every refinement. The source knee plane
        // is transported to the requested axis instead of turning sideways.
        leg.upper.localRotation = leg.upperLocal; leg.lower.localRotation = leg.lowerLocal; leg.foot.localRotation = leg.footLocal;
        Vector3 origin = leg.upper.position, delta = leg.target - origin;
        if (delta.sqrMagnitude < 1e-12f) return;
        float a = leg.upperLength, b = leg.lowerLength;
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
        Vector3 axis = delta.normalized, sourceAxis = (leg.foot.position - origin).normalized;
        Vector3 sourceBend = Vector3.ProjectOnPlane(leg.lower.position - origin, sourceAxis);
        if (sourceBend.sqrMagnitude < .000025f) sourceBend = Vector3.ProjectOnPlane(leg.upper.forward, sourceAxis);
        Vector3 bend = Quaternion.FromToRotation(sourceAxis, axis) * sourceBend.normalized;
        Vector3 wanted=Vector3.ProjectOnPlane(leg.desiredLower-origin,axis);
        if(wanted.sqrMagnitude>.0000001f)
        {
            Vector3 corrected=Vector3.RotateTowards(bend,wanted.normalized,25f*Mathf.Deg2Rad,0f).normalized;
            MaximumBendDegrees=Mathf.Max(MaximumBendDegrees,Vector3.Angle(bend,corrected));bend=corrected;
        }
        float along = (a * a - b * b + distance * distance) / (2f * distance);
        Vector3 elbow = origin + axis * along + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
        leg.upper.rotation = Quaternion.FromToRotation(leg.lower.position - origin, elbow - origin) * leg.upper.rotation;
        leg.lower.rotation = Quaternion.FromToRotation(leg.foot.position - leg.lower.position, leg.target - leg.lower.position) * leg.lower.rotation;
        leg.foot.rotation = leg.worldFoot;
    }
    public void Restore()
    {
        if (!adjusted) return; adjusted = false;
        if (visual != null){visual.localPosition = visualBefore;visual.localRotation=visualRotationBefore;}
        foreach (var leg in legs)
        {
            if (leg.upper != null) leg.upper.localRotation = leg.upperLocal;
            if (leg.lower != null) leg.lower.localRotation = leg.lowerLocal;
            if (leg.foot != null) leg.foot.localRotation = leg.footLocal;
        }
    }
    public void Clear(CatActivity expected = null)
    {
        if (expected != null && owner != expected) return;
        previousRestPlan=false;
        Restore(); owner = null; authored = visual = null; skin = null; supportedSkin=null;ownerSurface=null;predictionReady=false;solids.Clear(); mask.Clear();
        VisualLift = RequiredLift = MaximumTopPenetration = MaximumLegResidual = MaximumRootShift = 0f;
        MaximumFootLateralShift=MaximumBendDegrees=0;PlantedLegCount=BakeCount=0;topCache.Clear();
        LastSolveMs = MaximumSolveMs = 0d; SolveFrame = SolveCount = 0;
    }
    void OnDisable() => Clear();
    void OnDestroy() { if (sample != null) Destroy(sample); }
}
