using System.Collections.Generic;
using UnityEngine;

/// <summary>Reach real water with a reversible forequarter crouch and planted paws.</summary>
[DefaultExecutionOrder(690)]
[DisallowMultipleComponent]
public sealed class CatSipHeadMotion : MonoBehaviour
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
    sealed class Foreleg
    {
        public Transform arm,fore,hand;
        public Quaternion armPose,forePose,handPose,pawRotation;
        public Vector3 pawPosition,elbowPosition;
        public void Capture()
        {
            armPose=arm.localRotation;forePose=fore.localRotation;handPose=hand.localRotation;
            pawPosition=hand.position;pawRotation=hand.rotation;elbowPosition=fore.position;
        }
        public void Restore()
        {if(arm==null)return;arm.localRotation=armPose;fore.localRotation=forePose;hand.localRotation=handPose;}
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
    Transform torso;
    Quaternion torsoPose;
    public const float MaximumForequarterDeflection=34f;
    // Lowering to a basin requires flexion through the base of the neck; most
    // of the bend belongs there, with a smaller final head correction.
    public const float MaximumTotalDeflection=75f;
    static readonly float[] JointLimits={30f,25f,20f};
    SinkSipActivity owner;
    CatMovement actor;
    Transform water;
    bool adjusted;
    float weight;
    public bool IsActive => owner != null && owner.IsRunning && owner.IsSipping && owner.BelongsTo(actor);
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

    public bool Begin(SinkSipActivity activity,Transform target)
    {
        var nextActor=GetComponent<CatMovement>();
        if(activity==null||target==null||!activity.IsRunning||!activity.IsSipping||!activity.BelongsTo(nextActor))return false;
        if(IsActive&&owner!=activity)return false;
        if (!Prepare(nextActor, target)) return false;
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
        var animator=GetComponentInChildren<Animator>();
        var tag=GetComponentInChildren<CatBreedVisualTag>();
        var profile=CatSipMouthCatalog.Load()?.Find(tag!=null?tag.BreedId:CatBreedService.SelectedBreedId);
        if(animator==null||profile==null){Clear();return false;}
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
        water=target;MinimumDistance=float.PositiveInfinity;return true;
    }
    public void Sample(SinkSipActivity activity,float blend)
    {if(owner==activity)weight=Mathf.Clamp01(blend);}
    public void Stop(SinkSipActivity activity)
    {if(owner==activity)Clear();}
    void Update()=>Restore();
    void LateUpdate()
    {
        if(!IsActive || !owner.isActiveAndEnabled || water==null||joints[0]==null){Clear();return;}
        for(int i=0;i<joints.Length;i++)sourcePose[i]=joints[i].localRotation;
        torsoPose=torso.localRotation;
        foreach(var limb in forelegs)limb.Capture();
        adjusted=true;
        Vector3 goal=water.position;
        MouthVertex endpoint=mouth[0];float closest=float.PositiveInfinity;
        foreach(var vertex in mouth)
        {
            float square=(vertex.World()-goal).sqrMagnitude;
            if(square>=closest)continue;closest=square;endpoint=vertex;
        }
        SourceMouthPosition=endpoint.World();SourceDistance=Vector3.Distance(SourceMouthPosition,goal);
        NeckBasePosition=joints[0].position;
        PhysicalChainLength=Vector3.Distance(joints[0].position,joints[1].position)+Vector3.Distance(joints[1].position,joints[2].position)+
            Vector3.Distance(joints[2].position,SourceMouthPosition);
        RequiredReach=Vector3.Distance(NeckBasePosition,goal);
        // A neck cannot bridge a gap longer than its bones. Lower the shoulder
        // girdle through the real upper spine while preserving all four supports.
        // Pick the smallest useful pitch; ordinary shallow bowls need none.
        float bestDistance=float.PositiveInfinity,bestPitch=0;
        const int candidates=16;
        for(int step=0;step<=candidates;step++)
        {
            RestoreFrame();
            float pitch=MaximumForequarterDeflection*weight*step/candidates;
            torso.rotation=Quaternion.AngleAxis(pitch,actor.transform.right)*torso.rotation;
            bool planted=true;if(step>0)foreach(var limb in forelegs)planted&=limb.Plant();
            if(!planted)continue;
            SolveNeck(endpoint,goal);
            float distance=Vector3.Distance(endpoint.World(),goal);
            if(distance<bestDistance)
            {
                bestDistance=distance;bestPitch=pitch;
                for(int i=0;i<joints.Length;i++)bestNeckPose[i]=joints[i].localRotation;
            }
            if(distance<.009f)break;
        }
        RestoreFrame();
        torso.rotation=Quaternion.AngleAxis(bestPitch,actor.transform.right)*torso.rotation;
        if(bestPitch>0)foreach(var limb in forelegs)limb.Plant();
        for(int i=0;i<joints.Length;i++)joints[i].localRotation=bestNeckPose[i];
        ForequarterDeflection=Quaternion.Angle(torsoPose,torso.localRotation);
        ReachedNeckBasePosition=joints[0].position;
        PawPlantError=0;foreach(var limb in forelegs)PawPlantError=Mathf.Max(PawPlantError,Vector3.Distance(limb.hand.position,limb.pawPosition));
        TotalDeflection=0;
        for(int i=0;i<joints.Length;i++)TotalDeflection+=Quaternion.Angle(sourcePose[i],joints[i].localRotation);
        JointDeflections=new Vector3(Quaternion.Angle(sourcePose[0],joints[0].localRotation),Quaternion.Angle(sourcePose[1],joints[1].localRotation),Quaternion.Angle(sourcePose[2],joints[2].localRotation));
        MouthPosition=endpoint.World();Distance=Vector3.Distance(MouthPosition,water.position);
        if(weight>=.999f&&Time.deltaTime>0)MinimumDistance=Mathf.Min(MinimumDistance,Distance);
    }
    void SolveNeck(MouthVertex endpoint,Vector3 goal)
    {
        // CCD preserves each bone's local position/length. Per-joint limits are
        // measured from this frame's source clip, never accumulated across frames.
        for(int iteration=0;iteration<8;iteration++)
            for(int i=joints.Length-1;i>=0;i--)
            {
                var joint=joints[i];Vector3 from=endpoint.World()-joint.position,to=goal-joint.position;
                if(from.sqrMagnitude<.000001f||to.sqrMagnitude<.000001f)continue;
                Quaternion world=Quaternion.FromToRotation(from,to)*joint.rotation;
                Quaternion local=Quaternion.Inverse(joint.parent.rotation)*world;
                joint.localRotation=Quaternion.RotateTowards(sourcePose[i],local,JointLimits[i]*weight);
            }
    }
    void RestoreFrame()
    {
        if(torso!=null)torso.localRotation=torsoPose;
        foreach(var limb in forelegs)limb.Restore();
        for(int i=0;i<joints.Length;i++)if(joints[i]!=null)joints[i].localRotation=sourcePose[i];
    }
    void Restore()
    {
        if(!adjusted)return;adjusted=false;
        RestoreFrame();
    }
    void Clear()
    {
        Restore();owner=null;actor=null;water=null;mouth.Clear();weight=0;TotalDeflection=0;ForequarterDeflection=0;Distance=float.PositiveInfinity;
        torso=null;foreach(var limb in forelegs){limb.arm=null;limb.fore=null;limb.hand=null;}
        for(int i=0;i<joints.Length;i++)joints[i]=null;
    }
    void OnDisable()=>Clear();
    void OnDestroy()=>Clear();
}
