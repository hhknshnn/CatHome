using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Reach a real care target with a reversible head adjustment and planted paws.</summary>
[DefaultExecutionOrder(690)]
[DisallowMultipleComponent]
public sealed class CatMealHeadMotion : MonoBehaviour
{
    sealed class MouthVertex
    {
        public readonly Transform[] bones=new Transform[4];
        public readonly Vector3[] points=new Vector3[4];
        public readonly float[] weights=new float[4];
        public Vector3 World()
        {
            Vector3 result=Vector3.zero;
            for(int i=0;i<4;i++)if(weights[i]>0&&bones[i]!=null)result+=bones[i].TransformPoint(points[i])*weights[i];
            return result;
        }
    }
    readonly List<MouthVertex> mouth=new List<MouthVertex>(24);
    readonly Transform[] joints=new Transform[3];
    readonly Quaternion[] sourcePose=new Quaternion[3];
    readonly Quaternion[] bestNeckPose=new Quaternion[3];
    readonly Quaternion[] previousNeckOffset=new Quaternion[3];
    bool hasPreviousNeckPose;
    readonly Quaternion[] heldNeckPose=new Quaternion[3];
    Quaternion heldTorsoPose;
    Vector3 heldVisualPosition;
    bool hasHeldCarePose;
    sealed class Foreleg
    {
        public Transform arm,fore,hand;
        public Quaternion armPose,forePose,handPose,pawRotation;
        public Quaternion heldArmPose,heldForePose,heldHandPose;
        public Vector3 pawPosition,sourcePawPosition,elbowPosition,sourceElbowPosition;
        public float sourceSoleHeight,sourceSupportHeight,sourceSupportSurfaceHeight;
        public Vector3 sourceSupportPoint;
        public int sourceSupportSamples;
        public Vector3 soleLocal;
        public int[] soleVertices;
        public bool measuredSole;
        public void Capture()
        {
            armPose=arm.localRotation;forePose=fore.localRotation;handPose=hand.localRotation;
            pawPosition=sourcePawPosition=hand.position;pawRotation=hand.rotation;elbowPosition=sourceElbowPosition=fore.position;
            sourceSoleHeight=measuredSole?hand.TransformPoint(soleLocal).y:.005f;
        }
        public void Restore()
        {if(arm==null)return;arm.localRotation=armPose;fore.localRotation=forePose;hand.localRotation=handPose;elbowPosition=sourceElbowPosition;}
        public void CaptureHeld(){heldArmPose=arm.localRotation;heldForePose=fore.localRotation;heldHandPose=hand.localRotation;}
        public void ApplyHeld(){arm.localRotation=heldArmPose;fore.localRotation=heldForePose;hand.localRotation=heldHandPose;}
        public float ReachReserve => Vector3.Distance(arm.position,fore.position)+Vector3.Distance(fore.position,hand.position)-Vector3.Distance(arm.position,pawPosition);
        public bool Plant()
        {
            Vector3 delta=pawPosition-arm.position;float distance=delta.magnitude;
            float upper=Vector3.Distance(arm.position,fore.position),lower=Vector3.Distance(fore.position,hand.position);
            // Never stretch a leg, move its attachment or silently accept an unreachable paw.
            if(distance<Mathf.Abs(upper-lower)+.00001f||distance>upper+lower-.00001f)return false;
            Vector3 direction=delta/distance;
            Vector3 bend=Vector3.ProjectOnPlane(elbowPosition-arm.position,direction);
            if(bend.sqrMagnitude<.00000001f)bend=Vector3.ProjectOnPlane(fore.position-arm.position,direction);
            if(bend.sqrMagnitude<.00000001f)return false;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            Vector3 elbow=arm.position+direction*along+bend.normalized*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            arm.rotation=Quaternion.FromToRotation(fore.position-arm.position,elbow-arm.position)*arm.rotation;
            fore.rotation=Quaternion.FromToRotation(hand.position-fore.position,pawPosition-fore.position)*fore.rotation;
            hand.rotation=pawRotation;
            return Vector3.Distance(hand.position,pawPosition)<.0001f;
        }
    }
    readonly Foreleg[] forelegs={new Foreleg(),new Foreleg()};
    readonly Foreleg[] hindlegs={new Foreleg(),new Foreleg()};
    Transform visual;
    Vector3 visualSourcePosition;
    float bodyLean;
    float sourceFloorLift;
    CatCareWeightedSkin careSkin;
    // Read-only current-pose readiness uses the same exact skin as the routine.
    // Four target slots cover the room's care offers without binding every poll.
    sealed class StandingSkinCheck
    {
        public Transform target;
        public Animator animator;
        public SkinnedMeshRenderer renderer;
        public Mesh mesh;
        public CatCareSkinClearance clearance;
    }
    readonly StandingSkinCheck[] standingChecks=new StandingSkinCheck[4];
    int nextStandingCheck;
    CatCareSkinClearance careClearance;
    bool careFrameClear=true;
    public bool HasCareSkinClearance=>careClearance!=null;
    public bool CareFrameClear=>careFrameClear;
    public int ClearanceCandidates{get;private set;}
    public int ClearanceVertices{get;private set;}
    public int ClearanceRays{get;private set;}
    public int ClearanceTriangleTests{get;private set;}
    public int ClearanceMetricQueries{get;private set;}
    public double ClearanceMilliseconds{get;private set;}
    bool leanRequested,settlingOut;
    public bool IsSettlingOut=>settlingOut;
    public int KinematicCandidates{get;private set;}
    // Only a pitch index is retained. Every candidate is rebuilt from this
    // frame's native pose and must pass this frame's complete skin test.
    int preferredShoulderStep;
    struct ReachCandidate
    {
        public int step,canonicalOrder;
        public float pitch,distance;
        public Quaternion neck0,neck1,neck2;
        public bool skinTested,skinClear;
    }
    readonly ReachCandidate[] reachCandidates=new ReachCandidate[33];
    readonly int[] shoulderOrder=new int[33];
    // Native source measurement: .07m closes the Persian gap with four planted
    // supports. A source-relative reserve keeps the limb away from full extension.
    const float MaximumBodyLean=.07f;
    const float LeanSeconds=.25f;
    public float BodyLeanDistance=>bodyLean;
    public bool BodyLeanRequested=>leanRequested;
    public float MinimumLimbReachReserve {get;private set;}=float.PositiveInfinity;
    Transform torso;
    Quaternion torsoPose;
    public const float MaximumForequarterDeflection=34f;
    // Lowering to a basin requires flexion through the base of the neck; most
    // of the bend belongs there, with a smaller final head correction.
    public const float MaximumTotalDeflection=75f;
    static readonly float[] JointLimits={30f,25f,20f};
    MonoBehaviour owner;
    CatMovement actor;
    Transform food;
    bool adjusted;
    float weight;
    public bool IsActive => OwnsCare(owner, actor);
    public float Distance {get;private set;}=float.PositiveInfinity;
    public float MinimumDistance {get;private set;}=float.PositiveInfinity;
    public float TotalDeflection {get;private set;}
    public Vector3 MouthPosition {get;private set;}
    public int MouthVertexCount=>mouth.Count;
    public Vector3 SourceMouthPosition {get;private set;}
    public Vector3 NeckBasePosition {get;private set;}
    public Vector3 JointDeflections {get;private set;}
    public float SourceDistance {get;private set;}
    public float PhysicalChainLength {get;private set;}
    public float RequiredReach {get;private set;}
    public float ReachDeficit=>Mathf.Max(0,RequiredReach-PhysicalChainLength);
    public float ForequarterDeflection {get;private set;}
    public float PawPlantError {get;private set;}
    public Vector3 ReachedNeckBasePosition {get;private set;}

    // The authored Eating mouth is a useful use-zone centre, not a root snap.
    // A bounded skeletal reach closes the small remaining gap while the player
    // keeps the position and heading that made the prompt available.
    public static bool TryPrepareCareStart(CatMovement actor, Transform target, out CatActivityStart start)
    {
        if(!TryPrepareCarePose(actor,target,.16f,out start))return false;
        // Fresh physical validation stays on the click, never on each HUD poll.
        var head=actor.GetComponent<CatMealHeadMotion>();
        if(head==null)head=actor.gameObject.AddComponent<CatMealHeadMotion>();
        return head.IsStandingSkinClear(actor,target,true);
    }

    // Shared inexpensive stance check for the approach label and click admission.
    // Does not sample clips, scan skin, create components or move the actor.
    public static bool TryPrepareBowlPose(CatMovement actor, Transform target, out CatActivityStart start)
        => TryPrepareCarePose(actor,target,.025f,out start);

    static bool TryPrepareCarePose(CatMovement actor, Transform target, float rearReach, out CatActivityStart start)
    {
        start = default;
        if (actor == null || target == null) return false;
        var tag = actor.GetComponentInChildren<CatBreedVisualTag>();
        var profile = CatFeedingAlignmentCatalog.Load()?.Find(tag != null ? tag.BreedId : CatBreedService.SelectedBreedId);
        if (profile == null) return false;
        float scale = actor.transform.lossyScale.y / .5f;
        Vector3 mouth = profile.mouthOffset * scale;
        Vector3 centre = target.position - actor.transform.rotation * mouth;
        centre.y = 0f;
        Vector3 predictedMouth = actor.transform.position + actor.transform.rotation * mouth;
        if (Mathf.Abs(predictedMouth.y - target.position.y) > .26f * scale) return false;
        Vector3 delta=actor.transform.position-centre;delta.y=0;
        // The planted limbs can support more reach along the body than across
        // its side. A circular zone admitted far diagonal stances that the
        // bounded neck could not reach. Keep the nearby prompt, but require a
        // physically useful oval around the authored eating mouth on click.
        Vector3 localGap=Quaternion.Inverse(actor.transform.rotation)*delta/scale;
        float reachZone=localGap.z*localGap.z/(.16f*.16f)+localGap.x*localGap.x/(.10f*.10f);
        Vector3 direction=target.position-actor.transform.position;direction.y=0;
        start=new CatActivityStart{Position=actor.transform.position,Rotation=actor.transform.rotation,
            ZoneCentre=centre,ActionTarget=target.position,PromptDistance=delta.magnitude,Kind=CatActivityStartKind.Contact};
        // The old symmetric oval admitted starts 16 cm behind the measured
        // Eating mouth. Native walking could reach that edge while the neck
        // still could not bridge the gap. Require the source mouth to reach
        // the same 2.5 cm outer contact shell used by the actual care solver
        // for floor bowls. Other activities retain their own authored reach.
        if(!actor.isActiveAndEnabled||reachZone>1f||localGap.z < -rearReach||Mathf.Abs(start.Position.y-centre.y)>.15f||
            (direction.sqrMagnitude>=.0001f&&Vector3.Angle(actor.transform.forward,direction)>25f))return false;
        return true;
    }

    bool IsStandingSkinClear(CatMovement cat,Transform target,bool validateEnvelope=false)
    {
        var animator=cat.GetComponentInChildren<Animator>();
        var renderer=cat.GetComponentInChildren<SkinnedMeshRenderer>();
        if(animator==null||renderer==null||renderer.sharedMesh==null)return false;
        StandingSkinCheck check=null;
        for(int i=0;i<standingChecks.Length;i++)
        {
            var cached=standingChecks[i];
            if(cached!=null&&cached.target==target&&cached.animator==animator&&
                cached.renderer==renderer&&cached.mesh==renderer.sharedMesh){check=cached;break;}
        }
        if(check==null)
        {
            var tag=cat.GetComponentInChildren<CatBreedVisualTag>();
            var profile=CatCareSkinCatalog.Load()?.Find(tag!=null?tag.BreedId:CatBreedService.SelectedBreedId);
            if(!CatCareWeightedSkin.TryBind(profile,animator,renderer,out var currentSkin))return false;
            var clearance=CatCareSkinClearance.Bind(currentSkin,this,target,cat);
            if(clearance==null||!clearance.Available)return false;
            check=new StandingSkinCheck{target=target,animator=animator,renderer=renderer,mesh=renderer.sharedMesh,clearance=clearance};
            standingChecks[nextStandingCheck]=check;nextStandingCheck=(nextStandingCheck+1)%standingChecks.Length;
        }
        // Retain only bindings and reject-index order. Fresh bone matrices,
        // current collider enablement/transforms and every vertex are checked.
        return check.clearance.IsClear() && (!validateEnvelope || cat.IsCarePoseClear(
            cat.transform.position,cat.transform.rotation,check.clearance.ContainsVerifiedMesh));
    }

    static bool OwnsCare(MonoBehaviour activity, CatMovement cat)
    {
        if (activity == null || !activity.isActiveAndEnabled || cat == null) return false;
        if (activity is MealTimeActivity meal)
            return meal.IsRunning && (meal.IsEating || meal.IsLeavingMeal) && meal.BelongsTo(cat);
        return activity is BowlInteraction bowl && bowl.IsInteracting && bowl.gameObject == cat.gameObject;
    }

    public bool Begin(MonoBehaviour activity,Transform target)
    {
        var nextActor=GetComponent<CatMovement>();
        if(target==null||!OwnsCare(activity,nextActor))return false;
        if(IsActive&&owner!=activity)return false;
        if (!Prepare(nextActor, target)) return false;
        careClearance=CatCareSkinClearance.Bind(careSkin,activity,target,nextActor);
        owner=activity; return true;
    }
    bool Prepare(CatMovement nextActor, Transform target)
    {
        Clear();actor=nextActor;
        string[] names={"DEF-spine.004","DEF-spine.005","DEF-spine.006"};
        for(int i=0;i<joints.Length;i++)
        {
            joints[i]=CatBreedVisualFactory.FindDescendant(transform,names[i]);
            if(joints[i]==null){Clear();return false;}
            foreach(string limb in new[]{"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"})
            {
                var paw=CatBreedVisualFactory.FindDescendant(transform,limb);
                if(paw==null||paw.IsChildOf(joints[i])){Clear();return false;}
            }
        }
        torso=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine.002");
        if(torso==null||!joints[0].IsChildOf(torso)){Clear();return false;}
        for(int i=0;i<forelegs.Length;i++)
        {
            string side=i==0?"L":"R";var limb=forelegs[i];
            limb.arm=CatBreedVisualFactory.FindDescendant(transform,"DEF-upper_arm."+side);
            limb.fore=CatBreedVisualFactory.FindDescendant(transform,"DEF-forearm."+side);
            limb.hand=CatBreedVisualFactory.FindDescendant(transform,"DEF-hand."+side);
            var rear=CatBreedVisualFactory.FindDescendant(transform,"DEF-foot."+side);
            if(limb.arm==null||limb.fore==null||limb.hand==null||rear==null||rear.IsChildOf(torso)||
                !limb.arm.IsChildOf(torso)||limb.fore.parent!=limb.arm||limb.hand.parent!=limb.fore)
            {Clear();return false;}
        }
        for(int i=0;i<hindlegs.Length;i++)
        {
            string side=i==0?"L":"R";var limb=hindlegs[i];
            limb.arm=CatBreedVisualFactory.FindDescendant(transform,"DEF-thigh."+side);
            limb.fore=CatBreedVisualFactory.FindDescendant(transform,"DEF-shin."+side);
            limb.hand=CatBreedVisualFactory.FindDescendant(transform,"DEF-foot."+side);
            if(limb.arm==null||limb.fore==null||limb.hand==null||limb.arm.IsChildOf(torso)||
                limb.fore.parent!=limb.arm||limb.hand.parent!=limb.fore){Clear();return false;}
        }
        var animator=GetComponentInChildren<Animator>();
        var tag=GetComponentInChildren<CatBreedVisualTag>();
        var profile=CatSipMouthCatalog.Load()?.Find(tag!=null?tag.BreedId:CatBreedService.SelectedBreedId);
        if(animator==null||profile==null){Clear();return false;}
        visual=animator.transform;
        foreach(var vertex in profile.vertices)
        {
            var candidate=new MouthVertex();
            if(vertex.influences.Length>4){Clear();return false;}
            for(int i=0;i<vertex.influences.Length;i++)
            {
                var influence=vertex.influences[i];candidate.bones[i]=animator.transform.Find(influence.bonePath);
                if(candidate.bones[i]==null){Clear();return false;}
                candidate.weights[i]=influence.weight;candidate.points[i]=influence.bindPosition;
            }
            mouth.Add(candidate);
        }
        if(mouth.Count==0||mouth.Count>24){Clear();return false;}
        var skin=GetComponentInChildren<SkinnedMeshRenderer>();
        if(skin==null){Clear();return false;}
        var skinProfile=CatCareSkinCatalog.Load()?.Find(tag!=null?tag.BreedId:CatBreedService.SelectedBreedId);
        if(!CatCareWeightedSkin.TryBind(skinProfile,animator,skin,out careSkin)){Clear();return false;}
        // Use actual weighted paw/toe skin, not an arbitrary nearby vertex
        // frozen into hand space. Toe animation and mixed skin weights can
        // put that old point millimetres above the real sole and over-lower it.
        var skinBones=new Transform[skinProfile.bonePaths.Length];
        for(int i=0;i<skinBones.Length;i++)skinBones[i]=animator.transform.Find(skinProfile.bonePaths[i]);
        foreach(var limb in AllLegs())
        {
            var vertices=new List<int>();
            for(int vertex=0;vertex<skinProfile.vertexIndices.Length;vertex++)
            {
                float pawWeight=0;
                for(int i=skinProfile.starts[vertex];i<skinProfile.starts[vertex+1];i++)
                {
                    var bone=skinBones[skinProfile.bones[i]];
                    if(bone==limb.hand||bone.IsChildOf(limb.hand))pawWeight+=skinProfile.weights[i];
                }
                if(pawWeight>=.5f)vertices.Add(vertex);
            }
            if(vertices.Count==0){Clear();return false;}
            limb.soleVertices=vertices.ToArray();limb.measuredSole=true;
        }
        food=target;MinimumDistance=float.PositiveInfinity;return true;
    }
    public void Sample(MonoBehaviour activity,float blend)
    {if(owner==activity)weight=Mathf.Clamp01(blend);}
    public void Stop(MonoBehaviour activity)
    {if(owner==activity)Clear();}
    // Normal completion unfolds the same bounded adjustment before releasing
    // care ownership. Cancellation still restores immediately and never moves root.
    public IEnumerator SettleOut(MonoBehaviour activity,System.Action beginSourceExit,float sourceTransitionSeconds=0f)
    {
        if(owner!=activity)yield break;
        settlingOut=true;float from=weight,elapsed=0;
        // Retract the visual weight shift before the source stands up. Keep
        // full joint correction against the still-advancing Eating clip: its
        // uncorrected low head is not a safe intermediate. The faster Sniff
        // blend otherwise narrows leg reach before the shift can return.
        while(owner==activity&&bodyLean>.00001f)
        {
            Sample(activity,from);
            yield return null;
        }
        if(owner!=activity)yield break;
        beginSourceExit?.Invoke();
        // One frame lets a deferred CatActivityAnimation.SetPose take effect.
        // Source time is never stopped in either part of this transition.
        yield return null;
        while(owner==activity&&elapsed<sourceTransitionSeconds)
        {
            Sample(activity,from);
            yield return null;elapsed+=Time.deltaTime;
        }
        elapsed=0;
        while(owner==activity&&elapsed<LeanSeconds)
        {
            Sample(activity,from*(1f-Mathf.Clamp01(elapsed/LeanSeconds)));
            yield return null;elapsed+=Time.deltaTime;
        }
        if(owner==activity)Sample(activity,0);
        yield return null;
    }
    IEnumerable<Foreleg> AllLegs()
    {foreach(var leg in forelegs)yield return leg;foreach(var leg in hindlegs)yield return leg;}
    void CaptureLeg(Foreleg limb)
    {
        // Matrices were captured once from this frame's untouched native pose.
        // Every source frame gets its own real sole; no previous correction or
        // cached clearance is admitted as source data.
        if(limb.measuredSole&&limb.soleVertices!=null)
        {
            Vector3 lowest=careSkin.Point(limb.soleVertices[0]);
            for(int i=1;i<limb.soleVertices.Length;i++)
            {Vector3 point=careSkin.Point(limb.soleVertices[i]);if(point.y<lowest.y)lowest=point;}
            limb.soleLocal=limb.hand.InverseTransformPoint(lowest);
            limb.sourceSupportHeight=careClearance!=null?careClearance.SupportHeight(lowest):0f;
            limb.sourceSupportSurfaceHeight=limb.sourceSupportHeight;
            limb.sourceSupportPoint=lowest;limb.sourceSupportSamples=0;
            if(careClearance!=null)
            {
                // First vertical contact of the whole curved paw. The lowest toe
                // may lie outside a tray while its heel is still over the solid.
                // Preserve each vertex's height above the sole: a dorsal vertex
                // over a platform must not lift the entire paw to that platform.
                // This is sampled once from the native pose, never per candidate.
                for(int i=0;i<limb.soleVertices.Length;i++)
                {
                    Vector3 point=careSkin.Point(limb.soleVertices[i]);
                    float surface=careClearance.SupportHeight(point);limb.sourceSupportSamples++;
                    float equivalentSole=surface-(point.y-lowest.y);
                    if(equivalentSole<=limb.sourceSupportHeight)continue;
                    limb.sourceSupportHeight=equivalentSole;
                    limb.sourceSupportSurfaceHeight=surface;limb.sourceSupportPoint=point;
                }
            }
        }
        limb.Capture();
    }
    void Update()=>Restore();
    void LateUpdate()
    {
        if(!IsActive || !owner.isActiveAndEnabled || food==null||joints[0]==null){Clear();return;}
        long clearanceStarted=System.Diagnostics.Stopwatch.GetTimestamp();
        careFrameClear=true;ClearanceCandidates=KinematicCandidates=ClearanceVertices=ClearanceRays=ClearanceTriangleTests=ClearanceMetricQueries=0;ClearanceMilliseconds=0;
        // A paused native frame must not run another warm-start convergence
        // step. Reapply its exact displayed correction after Animator sampling,
        // but still reject it against fresh skin and current physical blockers.
        if(Time.deltaTime<=0f&&hasHeldCarePose&&careClearance!=null)
        {
            visual.localPosition=heldVisualPosition;torso.localRotation=heldTorsoPose;
            for(int i=0;i<joints.Length;i++)joints[i].localRotation=heldNeckPose[i];
            foreach(var limb in forelegs)limb.ApplyHeld();foreach(var limb in hindlegs)limb.ApplyHeld();adjusted=true;
            careFrameClear=CheckCareSkin()&&actor.IsCarePoseClear(actor.transform.position,actor.transform.rotation,careClearance.ContainsVerifiedMesh);
            if(careFrameClear)
            {ClearanceMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-clearanceStarted)*1000.0/System.Diagnostics.Stopwatch.Frequency;return;}
            RestoreFrame();
        }
        for(int i=0;i<joints.Length;i++)bestNeckPose[i]=sourcePose[i]=joints[i].localRotation;
        torsoPose=torso.localRotation;visualSourcePosition=visual.localPosition;
        if(!careSkin.CaptureMatrices()){Clear();return;}
        // Some retargeted Eating frames put the pelvis a few millimetres
        // below the floor. Lift only the visual rig by that measured amount,
        // keeping the root and all four paw targets fixed. Never relax skin
        // clearance or reuse a previous frame's permission.
        float lowestSource=float.PositiveInfinity;
        for(int i=0;i<careSkin.Count;i++)lowestSource=Mathf.Min(lowestSource,careSkin.Point(i).y);
        sourceFloorLift=careClearance!=null?Mathf.Clamp(careClearance.FloorHeight+.001f-lowestSource,0,.006f)*weight:0;
        foreach(var limb in forelegs)
        {
            CaptureLeg(limb);
            // Retain the real sole-to-bone offset. A long-haired forepaw can
            // sit above the floor in the source clip; plant its sole smoothly.
            if(limb.measuredSole)limb.pawPosition.y-=Mathf.Clamp(limb.sourceSoleHeight-(limb.sourceSupportHeight+.005f),0f,.065f)*weight;
        }
        foreach(var limb in hindlegs)CaptureLeg(limb);
        adjusted=true;
        // Contact is above the kibble, not inside it. A mouth vertex touching
        // the grain exactly can leave the lower jaw and cheeks buried in food.
        Vector3 outside=actor.transform.position-food.position;outside.y=0f;
        Vector3 goal=food.position + Vector3.up * .025f + outside.normalized * .025f;
        MouthVertex endpoint=LowestMouth();
        SourceMouthPosition=endpoint.World();SourceDistance=Vector3.Distance(SourceMouthPosition,goal);
        NeckBasePosition=joints[0].position;
        PhysicalChainLength=Vector3.Distance(joints[0].position,joints[1].position)+Vector3.Distance(joints[1].position,joints[2].position)+
            Vector3.Distance(joints[2].position,SourceMouthPosition);
        RequiredReach=Vector3.Distance(NeckBasePosition,goal);
        // A neck cannot bridge a gap longer than its bones. Lower the shoulder
        // girdle through the real upper spine while preserving all four supports.
        // Pick the smallest useful pitch; ordinary shallow bowls need none.
        // Ordinary reachable care retains its original two-front-paw solve.
        // An unreachable full-weight frame requests a bounded, reversible body
        // weight shift. Both rear paw supports are then retained as well.
        float bestPitch=0;
        // Once the physical path has requested lean, the zero-lean scan cannot
        // change that latch or the selected pose. Do not solve those33 again.
        if(careClearance==null||(!leanRequested&&!settlingOut&&weight>=.999f))
        {
            SolveReachFrame(goal,0,out bestPitch);
            float baseFood=Vector3.Distance(LowestMouth().World(),food.position);
            if(!settlingOut&&weight>=.999f&&baseFood>CatCareReachGeometry.ContactDistance)leanRequested=true;
        }
        float scale=actor.transform.lossyScale.y/.5f;
        float requested=leanRequested&&!settlingOut?MaximumBodyLean*scale*weight:0f;
        float desiredLean=Mathf.MoveTowards(bodyLean,requested,MaximumBodyLean*scale/LeanSeconds*Time.deltaTime);
        if(careClearance!=null)
        {
            if(SolveClearReachFrame(goal,desiredLean,out float acceptedLean,out bestPitch))bodyLean=acceptedLean;
            else
            {
                // Preserve the current source clip when no adjusted pose is safe.
                // An unsafe fallback cannot add an eating contact or reward.
                RestoreFrame();bodyLean=0;bestPitch=0;careFrameClear=CheckCareSkin();
            }
        }
        else
        {
            bodyLean=desiredLean;
            if(bodyLean>.00001f&&!SolveReachFrame(goal,bodyLean,out bestPitch))
            {bodyLean=0;SolveReachFrame(goal,0,out bestPitch);}
        }
        MinimumLimbReachReserve=float.PositiveInfinity;
        foreach(var limb in forelegs)MinimumLimbReachReserve=Mathf.Min(MinimumLimbReachReserve,limb.ReachReserve);
        if(bodyLean>.00001f)foreach(var limb in hindlegs)MinimumLimbReachReserve=Mathf.Min(MinimumLimbReachReserve,limb.ReachReserve);
        ForequarterDeflection=Quaternion.Angle(torsoPose,torso.localRotation);
        ReachedNeckBasePosition=joints[0].position;
        PawPlantError=0;foreach(var limb in forelegs)PawPlantError=Mathf.Max(PawPlantError,Vector3.Distance(limb.hand.position,limb.pawPosition));
        if(bodyLean>.00001f||sourceFloorLift>.00001f)foreach(var limb in hindlegs)PawPlantError=Mathf.Max(PawPlantError,Vector3.Distance(limb.hand.position,limb.pawPosition));
        TotalDeflection=0;
        for(int i=0;i<joints.Length;i++)TotalDeflection+=Quaternion.Angle(sourcePose[i],joints[i].localRotation);
        JointDeflections=new Vector3(Quaternion.Angle(sourcePose[0],joints[0].localRotation),Quaternion.Angle(sourcePose[1],joints[1].localRotation),Quaternion.Angle(sourcePose[2],joints[2].localRotation));
        for(int i=0;i<joints.Length;i++)previousNeckOffset[i]=Quaternion.Inverse(sourcePose[i])*joints[i].localRotation;
        hasPreviousNeckPose=careFrameClear;
        heldVisualPosition=visual.localPosition;heldTorsoPose=torso.localRotation;
        for(int i=0;i<joints.Length;i++)heldNeckPose[i]=joints[i].localRotation;
        foreach(var limb in forelegs)limb.CaptureHeld();foreach(var limb in hindlegs)limb.CaptureHeld();
        hasHeldCarePose=careFrameClear;
        // Success belongs to any of the actual baked mouth vertices touching
        // the real food. LowestMouth remains the solver's lower-jaw aim point;
        // its height ordering is not a nearest-contact distance measurement.
        MouthPosition=NearestMouth(food.position).World();Distance=Vector3.Distance(MouthPosition,food.position);
        if(!settlingOut&&weight>=.999f&&Time.deltaTime>0&&careFrameClear)MinimumDistance=Mathf.Min(MinimumDistance,Distance);
        // Includes the initial reach scan, candidate preparation and final pose,
        // rather than reporting only the expensive skin subsection.
        ClearanceMilliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-clearanceStarted)*1000.0/System.Diagnostics.Stopwatch.Frequency;
    }
    bool CheckCareSkin()
    {
        bool clear=careClearance.IsClear();
        ClearanceVertices+=careClearance.LastVertices;ClearanceRays+=careClearance.LastRays;
        ClearanceMetricQueries+=careClearance.LastMetricQueries;
        if(careClearance.LastTriangleTests<0)ClearanceTriangleTests=-1;
        else if(ClearanceTriangleTests>=0)ClearanceTriangleTests+=careClearance.LastTriangleTests;
        return clear;
    }
    bool SolveClearReachFrame(Vector3 goal,float desiredLean,out float chosenLean,out float bestPitch)
    {
        chosenLean=0;bestPitch=0;float scale=actor.transform.lossyScale.y/.5f;
        float stepSize=.005f*scale;
        int orderCount=BuildShoulderOrder();
        for(int retreat=0;retreat<=14;retreat++)
        {
            float lean=Mathf.Max(0,desiredLean-retreat*stepSize);
            float rearWeight=Mathf.Clamp01(lean/(MaximumBodyLean*scale));
            foreach(var limb in hindlegs)
            {
                limb.pawPosition=limb.sourcePawPosition;
                if(limb.measuredSole)limb.pawPosition.y-=Mathf.Clamp(limb.sourceSoleHeight-(limb.sourceSupportHeight+.005f),0,.065f)*rearWeight;
            }
            int count=0;
            for(int order=0;order<orderCount;order++)
            {
                int step=shoulderOrder[order];
                float pitch=MaximumForequarterDeflection*weight*step/16;
                ClearanceCandidates++;KinematicCandidates++;
                if(!ApplyShoulderCandidate(lean,pitch,scale))continue;
                SolveNeck(LowestMouth(),goal);
                var candidate=new ReachCandidate{step=step,pitch=pitch,canonicalOrder=CanonicalOrder(step),
                    distance=Vector3.Distance(LowestMouth().World(),goal),neck0=joints[0].localRotation,
                    neck1=joints[1].localRotation,neck2=joints[2].localRotation};
                // Try the preceding pitch and its neighbours first for a real
                // bite. No previous clearance/pose is reused: all joints, paws
                // and exact weighted skin are evaluated on this native frame.
                if(order<3&&!settlingOut&&weight>=.999f&&candidate.distance<=CatCareReachGeometry.ContactDistance&&
                    Vector3.Distance(LowestMouth().World(),food.position)<=CatCareReachGeometry.ContactDistance)
                {
                    candidate.skinTested=true;candidate.skinClear=CheckCareSkin();
                    if(candidate.skinClear)
                    {chosenLean=lean;bestPitch=pitch;preferredShoulderStep=step;return true;}
                }
                // Stable insertion sort in a bounded reused buffer. For a
                // withdrawing phase, check closest candidates first; the first
                // clear result is exactly the old minimum-distance selection.
                int at=count++;
                while(at>0&&(candidate.distance<reachCandidates[at-1].distance||
                    (candidate.distance==reachCandidates[at-1].distance&&candidate.canonicalOrder<reachCandidates[at-1].canonicalOrder)))
                {reachCandidates[at]=reachCandidates[at-1];at--;}
                reachCandidates[at]=candidate;
            }
            for(int index=0;index<count;index++)
            {
                var candidate=reachCandidates[index];
                if(candidate.skinTested&&!candidate.skinClear)continue;
                if(!ApplyShoulderCandidate(lean,candidate.pitch,scale))continue;
                joints[0].localRotation=candidate.neck0;joints[1].localRotation=candidate.neck1;joints[2].localRotation=candidate.neck2;
                if(!CheckCareSkin())continue;
                chosenLean=lean;bestPitch=candidate.pitch;preferredShoulderStep=candidate.step;return true;
            }
            if(lean<=0)break;
        }
        RestoreFrame();return false;
    }
    bool ApplyShoulderCandidate(float lean,float pitch,float scale)
    {
        RestoreFrame();ApplyLean(lean);
        torso.rotation=Quaternion.AngleAxis(pitch,actor.transform.right)*torso.rotation;
        bool planted=true;foreach(var limb in forelegs)planted&=limb.Plant();
        if(lean>.00001f||sourceFloorLift>.00001f)
        {
            foreach(var limb in hindlegs)planted&=limb.Plant();
            float reserve=.003f*scale;
            foreach(var limb in forelegs)planted&=limb.ReachReserve>=reserve;
            foreach(var limb in hindlegs)planted&=limb.ReachReserve>=reserve;
        }
        return planted;
    }
    static int CanonicalOrder(int step)=>step==0?0:step>0?step*2-1:-step*2;
    int BuildShoulderOrder()
    {
        if(weight<=0){shoulderOrder[0]=0;return 1;}
        int count=0;ulong seen=0;
        AddShoulderStep(preferredShoulderStep,ref count,ref seen);
        AddShoulderStep(preferredShoulderStep-1,ref count,ref seen);
        AddShoulderStep(preferredShoulderStep+1,ref count,ref seen);
        for(int choice=0;choice<=32;choice++)AddShoulderStep((choice+1)/2*(choice%2==0?-1:1),ref count,ref seen);
        return count;
    }
    void AddShoulderStep(int step,ref int count,ref ulong seen)
    {
        if(step< -16||step>16)return;ulong flag=1UL<<(step+16);
        if((seen&flag)!=0)return;seen|=flag;shoulderOrder[count++]=step;
    }
    bool SolveReachFrame(Vector3 goal,float lean,out float bestPitch)
    {
        bestPitch=0;float bestDistance=float.PositiveInfinity;const int candidates=16;
        // Rear targets derive from this native source frame; the measured sole
        // offset blends in with the lean instead of snapping feet at its onset.
        float rearWeight=MaximumBodyLean>0?Mathf.Clamp01(lean/(MaximumBodyLean*actor.transform.lossyScale.y/.5f)):0;
        foreach(var limb in hindlegs)
        {
            // Never read a previous head/lean candidate as the next source.
            // Both values were captured once before any solve in LateUpdate.
            limb.pawPosition=limb.sourcePawPosition;
            if(limb.measuredSole)limb.pawPosition.y-=Mathf.Clamp(limb.sourceSoleHeight-(limb.sourceSupportHeight+.005f),0f,.065f)*rearWeight;
        }
        for(int choice=0;choice<=candidates*2;choice++)
        {
            KinematicCandidates++;
            RestoreFrame();ApplyLean(lean);
            int step=(choice+1)/2*(choice%2==0?-1:1);
            float pitch=MaximumForequarterDeflection*weight*step/candidates;
            torso.rotation=Quaternion.AngleAxis(pitch,actor.transform.right)*torso.rotation;
            bool planted=true;foreach(var limb in forelegs)planted&=limb.Plant();
            if(lean>.00001f||sourceFloorLift>.00001f)
            {
                float reserve=.003f*actor.transform.lossyScale.y/.5f;
                foreach(var limb in hindlegs)planted&=limb.Plant();
                foreach(var limb in forelegs)planted&=limb.ReachReserve>=reserve;
                foreach(var limb in hindlegs)planted&=limb.ReachReserve>=reserve;
            }
            if(!planted)continue;
            SolveNeck(LowestMouth(),goal);
            float distance=Vector3.Distance(LowestMouth().World(),goal);
            if(distance<bestDistance)
            {bestDistance=distance;bestPitch=pitch;for(int i=0;i<joints.Length;i++)bestNeckPose[i]=joints[i].localRotation;}
            if(distance<.009f)break;
        }
        RestoreFrame();
        if(float.IsPositiveInfinity(bestDistance))return false;
        ApplyLean(lean);torso.rotation=Quaternion.AngleAxis(bestPitch,actor.transform.right)*torso.rotation;
        foreach(var limb in forelegs)limb.Plant();
        if(lean>.00001f||sourceFloorLift>.00001f)foreach(var limb in hindlegs)limb.Plant();
        for(int i=0;i<joints.Length;i++)joints[i].localRotation=bestNeckPose[i];
        return true;
    }
    void ApplyLean(float lean)
    {
        // Every caller first RestoreFrame()s: each limb resets elbowPosition to
        // sourceElbowPosition, so the 33 candidate shifts never accumulate.
        // Shift toward the bowl, not along a slightly misaligned body heading.
        // The root stays fixed and every paw/skin candidate is still validated.
        // A modest accepted yaw must not make the neck bridge the lateral gap.
        Vector3 direction=food.position-actor.transform.position;direction.y=0;
        direction=direction.sqrMagnitude>.0001f?direction.normalized:actor.transform.forward;
        Vector3 shift=direction*lean+Vector3.up*sourceFloorLift;visual.position+=shift;
        foreach(var limb in forelegs)limb.elbowPosition+=shift;
        foreach(var limb in hindlegs)limb.elbowPosition+=shift;
    }
    void SolveNeck(MouthVertex endpoint,Vector3 goal)
    {
        // Start from the preceding correction applied to this frame's native
        // pose. CCD otherwise alternates between equally close neck folds.
        // This is only a search seed: bounds and fresh skin validation still
        // decide whether the newly solved pose can be displayed.
        if(hasPreviousNeckPose)
            for(int i=0;i<joints.Length;i++)
                joints[i].localRotation=CatCareJointLimit.Clamp(sourcePose[i],sourcePose[i]*previousNeckOffset[i],JointLimits[i]*weight);
        // CCD preserves each bone's local position/length. Per-joint limits are
        // measured from this frame's source clip, never accumulated across frames.
        for(int iteration=0;iteration<8;iteration++)
        {
            endpoint=LowestMouth();
            for(int i=joints.Length-1;i>=0;i--)
            {
                var joint=joints[i];Vector3 from=endpoint.World()-joint.position,to=goal-joint.position;
                if(from.sqrMagnitude<.000001f||to.sqrMagnitude<.000001f)continue;
                Quaternion world=Quaternion.FromToRotation(from,to)*joint.rotation;
                Quaternion local=Quaternion.Inverse(joint.parent.rotation)*world;
                joint.localRotation=CatCareJointLimit.Clamp(sourcePose[i],local,JointLimits[i]*weight);
            }
        }
    }
    MouthVertex NearestMouth(Vector3 point)
    {
        MouthVertex result=mouth[0];float square=(result.World()-point).sqrMagnitude;
        foreach(var vertex in mouth)
        {float candidate=(vertex.World()-point).sqrMagnitude;if(candidate<square){square=candidate;result=vertex;}}
        return result;
    }
    MouthVertex LowestMouth()
    {
        MouthVertex result=mouth[0];float lowest=result.World().y;
        foreach(var vertex in mouth){float y=vertex.World().y;if(y<lowest){lowest=y;result=vertex;}}
        return result;
    }
    void RestoreFrame()
    {
        if(visual!=null&&adjusted)visual.localPosition=visualSourcePosition;
        if(torso!=null)torso.localRotation=torsoPose;
        foreach(var limb in forelegs)limb.Restore();
        foreach(var limb in hindlegs)limb.Restore();
        for(int i=0;i<joints.Length;i++)if(joints[i]!=null)joints[i].localRotation=sourcePose[i];
    }
    void Restore()
    {
        if(!adjusted)return;
        RestoreFrame();adjusted=false;
    }
    void Clear()
    {
        Restore();owner=null;actor=null;food=null;mouth.Clear();weight=0;TotalDeflection=0;ForequarterDeflection=0;Distance=float.PositiveInfinity;
        torso=null;visual=null;bodyLean=sourceFloorLift=0;leanRequested=false;settlingOut=false;MinimumLimbReachReserve=float.PositiveInfinity;
        careSkin=null;careClearance=null;careFrameClear=true;preferredShoulderStep=0;hasPreviousNeckPose=hasHeldCarePose=false;
        ClearanceCandidates=KinematicCandidates=ClearanceVertices=ClearanceRays=ClearanceTriangleTests=ClearanceMetricQueries=0;ClearanceMilliseconds=0;
        foreach(var limb in AllLegs()){limb.arm=null;limb.fore=null;limb.hand=null;limb.measuredSole=false;limb.soleVertices=null;}
        for(int i=0;i<joints.Length;i++)joints[i]=null;
    }
    void OnDisable()=>Clear();
    void OnDestroy()=>Clear();
}
