using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A real distal skin patch, separate from the legacy wrist-pivot contact API.</summary>
public sealed class CatPawSurfacePlan
{
    public CatPawReachCatalog.Entry Entry;
    public bool Left;
    public int[] Vertices;
    public Collider Collider;
    public RaycastHit SourceRequest;
    public CatMeshContactSurface.Hit MeshHit;
    public Transform TargetTransform => MeshHit.Filter != null ? MeshHit.Filter.transform : Collider != null ? Collider.transform : null;
    public Vector3 LocalPoint, LocalNormal, LocalApproachNormal;
    public float ApproachLift;
    public bool LimitLiftToRemainingRise;
    // A compact source gesture goes out to the measured contact and retraces
    // that same complete source interval. It never freezes a clipped long tail.
    public bool ReturnToStart;
    public static bool ContactPhase(CatActivityPose pose,float phase) => pose==CatActivityPose.Paw ?
        Mathf.Abs(phase-.15f)<.00001f||Mathf.Abs(phase-.20f)<.00001f||Mathf.Abs(phase-.25f)<.00001f :
        Mathf.Abs(phase-.32f)<.00001f||Mathf.Abs(phase-.42f)<.00001f||Mathf.Abs(phase-.52f)<.00001f||Mathf.Abs(phase-.62f)<.00001f;
    public Vector3 Point => TargetTransform != null ? TargetTransform.TransformPoint(LocalPoint) : Vector3.zero;
    public Vector3 Normal => TargetTransform != null ? TargetTransform.localToWorldMatrix.inverse.transpose.MultiplyVector(LocalNormal).normalized : Vector3.up;
    public Vector3 ApproachNormal => TargetTransform != null ? TargetTransform.localToWorldMatrix.inverse.transpose.MultiplyVector(LocalApproachNormal).normalized : Vector3.zero;
    public bool IsValid => Entry != null && Vertices != null && Vertices.Length >= 3 && Vertices.Length <= 8 &&
        (MeshHit.Filter != null ? MeshHit.IsValid && MeshHit.Owner != null : Collider != null && Collider.enabled && Collider.gameObject.activeInHierarchy);
    public bool TargetBoxesClear(CatBodyGuardBox[] boxes,float tolerance) =>
        MeshHit.Filter == null || MeshHit.Owner != null && MeshHit.Owner.BoxesClear(boxes,tolerance);
    public CatPawReachCatalog.PawVertex[] Definitions => Left ? Entry.leftPaw : Entry.rightPaw;
}

/// <summary>The same 16 iterations and 20 degree step for predicted and live skin endpoints.</summary>
public static class CatPawSurfaceCcd
{
    public const int Iterations = 16;
    public const float StepDegrees = 20f;
    public struct State
    {
        public Vector3 Upper, Fore, Hand, Endpoint;
        public Quaternion PawRotation, UpperRotation;
    }
    public static Quaternion Delta(Vector3 joint, Vector3 endpoint, Vector3 goal)
    {
        Vector3 from=endpoint-joint,to=goal-joint;
        if(from.sqrMagnitude<.00001f||to.sqrMagnitude<.00001f)return Quaternion.identity;
        return Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(from,to),StepDegrees);
    }
    public static Vector3 Goal(Vector3 sourceEndpoint,Vector3 target,float weight,Vector3 outwardNormal,float lift,bool limitToRemainingRise=false)
    {
        float w=Mathf.Clamp01(weight);
        if(w<=0)return sourceEndpoint;if(w>=1)return target;
        Vector3 normal=outwardNormal.normalized;
        float effectiveLift=Mathf.Max(0,lift);
        // The source paw is already rising. Add only the remaining geometric
        // rise, rather than repeating the source-zero height at every phase.
        if(limitToRemainingRise&&normal.y>.00001f)
            effectiveLift=Mathf.Min(effectiveLift,Mathf.Max(0,target.y-sourceEndpoint.y)*.5f/normal.y);
        return Vector3.Lerp(sourceEndpoint,target,w)+normal*(effectiveLift*Mathf.Sin(Mathf.PI*w));
    }
    public static State Solve(Vector3 upper,Vector3 fore,Vector3 hand,Vector3 endpoint,Vector3 target,float weight,
        Vector3 outwardNormal=default,float lift=0,bool limitToRemainingRise=false)
    {
        var state=new State{Upper=upper,Fore=fore,Hand=hand,Endpoint=endpoint,PawRotation=Quaternion.identity,UpperRotation=Quaternion.identity};
        Vector3 goal=Goal(endpoint,target,weight,outwardNormal,lift,limitToRemainingRise);
        for(int i=0;i<Iterations;i++)
        {
            Quaternion delta=Delta(state.Fore,state.Endpoint,goal);
            state.Hand=state.Fore+delta*(state.Hand-state.Fore);
            state.Endpoint=state.Fore+delta*(state.Endpoint-state.Fore);
            state.PawRotation=delta*state.PawRotation;
            delta=Delta(state.Upper,state.Endpoint,goal);
            state.UpperRotation=delta*state.UpperRotation;
            state.Fore=state.Upper+delta*(state.Fore-state.Upper);
            state.Hand=state.Upper+delta*(state.Hand-state.Upper);
            state.Endpoint=state.Upper+delta*(state.Endpoint-state.Upper);
            state.PawRotation=delta*state.PawRotation;
        }
        return state;
    }
    public static Vector3 TransformPoint(State state,Vector3 oldHand,Vector3 point) =>
        state.Hand+state.PawRotation*(point-oldHand);
}

/// <summary>Only a handful of baked bind-weight vertices are skinned during the live solve.</summary>
public sealed class CatPawSurfaceBinding
{
    readonly CatPawSurfacePlan plan;
    readonly Transform[][] bones;
    public bool IsValid { get; private set; }
    public CatPawSurfaceBinding(CatPawSurfacePlan plan,Transform animationRoot)
    {
        this.plan=plan;IsValid=plan!=null&&plan.IsValid&&animationRoot!=null;
        if(!IsValid){bones=Array.Empty<Transform[]>();return;}
        bones=new Transform[plan.Vertices.Length][];
        for(int v=0;v<bones.Length;v++)
        {
            int index=plan.Vertices[v];
            if(index<0||index>=plan.Definitions.Length){IsValid=false;continue;}
            var vertex=plan.Definitions[index];bones[v]=new Transform[vertex.influences.Length];
            for(int i=0;i<bones[v].Length;i++)
            {bones[v][i]=animationRoot.Find(vertex.influences[i].bonePath);IsValid&=bones[v][i]!=null;}
        }
    }
    public Vector3 Point()
    {
        if(!IsValid||!plan.IsValid)return new Vector3(float.PositiveInfinity,0,0);
        Vector3 sum=Vector3.zero;
        for(int v=0;v<bones.Length;v++)
        {
            var influences=plan.Definitions[plan.Vertices[v]].influences;
            for(int i=0;i<influences.Length;i++)sum+=bones[v][i].TransformPoint(influences[i].bindPosition)*influences[i].weight;
        }
        return sum/bones.Length;
    }
}

public static partial class CatPawReachResolver
{
    static readonly Vector3[] surfaceDirections={Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back};
    static readonly CatBodyGuardCatalog.Probe[] pawChecks=new CatBodyGuardCatalog.Probe[2];
    static readonly List<Collider> surfaceColliderScratch=new List<Collider>(8);

    /// <summary>Measure the authored leaf point on its actual collider, with a surface normal; never changes it by a height offset.</summary>
    public static bool TryMeasureSurface(Transform owner,Vector3 authoredPoint,out RaycastHit surface)
    {
        surface=default;float nearest=.012f*.012f;bool found=false;
        owner.GetComponentsInChildren(false,surfaceColliderScratch);
        foreach(var collider in surfaceColliderScratch)
        {
            if(!collider.enabled||collider.isTrigger)continue;
            foreach(var direction in surfaceDirections)
            {
                if(!collider.Raycast(new Ray(authoredPoint+direction*.035f,-direction),out var hit,.07f))continue;
                float distance=(hit.point-authoredPoint).sqrMagnitude;
                if(distance>=nearest)continue;
                nearest=distance;surface=hit;found=true;
            }
        }
        return found;
    }

    sealed class SurfaceArmGeometry
    {
        public Vector3 upper,fore,hand;
        public float maximumReach;
        public Vector3[] points,patchMeans;
        public int[][] patches;
        public int[] distal;
    }
    sealed class SurfaceSampleGeometry
    {
        public CatPawReachCatalog.Sample sample;
        public Vector3 pivot;
        public SurfaceArmGeometry left,right;
        public CatPawWeightedSkin.Frame leftSkin,rightSkin;
        public CatPawWeightedSkin.Frame[] bodySkin;
    }
    sealed class SurfaceGeometry
    {
        public CatPawReachCatalog.Entry entry;
        public SourcePose body;
        public SurfaceSampleGeometry[] all,contacts;
        public Vector3[] leftScratch,rightScratch,leftArmScratch,rightArmScratch;
        public int[][] leftRegions,rightRegions,leftFaces,rightFaces;
        public CatBodyGuardBox[] endpointScratch;
        public int PawStride=>leftRegions.Length+rightRegions.Length;
        public SurfaceBodyGeometry bodySurface;
        public int buildCursor,contactCount;
        public bool ready,invalid;
        public CatBodyGuardCatalog.Probe[] upperScratch;
        public readonly Dictionary<int,CatBodyGuardCatalog.Probe[]> upperProfiles=new Dictionary<int,CatBodyGuardCatalog.Probe[]>();
        public readonly Dictionary<SurfaceRequest,SurfaceRequestGeometry> requests=new Dictionary<SurfaceRequest,SurfaceRequestGeometry>();
        public CatMovement checkingActor;
        public CatPawReachPlan checkingPlan;
        public int checkingFixedSample, checkingFilled;
        public Func<int,Collider,bool> refinement;
        public bool Refine(int index,Collider solid)
        {
            var actor=checkingActor;var plan=checkingPlan;int fixedSample=checkingFixedSample,filled=checkingFilled;
            int sample=fixedSample>=0?fixedSample:fixedSample==-2?
                (index/PawStride==0||SurfaceReturnsToStart(entry.pose)?0:all.Length-1):index/PawStride;
            int region=index%PawStride;
            if(filled!=sample)
            {
                if(fixedSample==-2)
                {
                    var source=all[sample];
                    CatPawWeightedSkin.Fill(source.leftSkin,source.pivot,Quaternion.identity,false,true,default,default,default,leftArmScratch);
                    CatPawWeightedSkin.Fill(source.rightSkin,source.pivot,Quaternion.identity,false,true,default,default,default,rightArmScratch);
                }
                else if(!FillSurfacePawPoints(this,plan,actor.transform.right,sample,true))return false;
                filled=sample;checkingFilled=sample;
            }
            bool left=region<leftRegions.Length;
            var points=left?leftArmScratch:rightArmScratch;
            var faces=left?leftFaces[region]:rightFaces[region-leftRegions.Length];
            for(int t=0;t<faces.Length;t+=3)
                if(!actor.IsInteractionTriangleClear(points[faces[t]],points[faces[t+1]],points[faces[t+2]],solid,.002f))return false;
            return true;
        }

    }
    struct SurfaceRequest : IEquatable<SurfaceRequest>
    {
        public Collider collider;
        public CatMeshContactSurface.Hit mesh;
        public Transform target;
        public float minPitch;
        public bool bothYawSigns;
        public RaycastHit source;
        public Vector3 point,normal;
        public Matrix4x4 targetMatrix;
        public bool left;
        public float pitch,yaw;
        public bool Equals(SurfaceRequest other)=>collider==other.collider&&point.Equals(other.point)&&normal.Equals(other.normal)&&
            targetMatrix.Equals(other.targetMatrix)&&left==other.left&&pitch==other.pitch&&yaw==other.yaw&&minPitch==other.minPitch&&bothYawSigns==other.bothYawSigns&&
            mesh.Filter==other.mesh.Filter&&mesh.Mesh==other.mesh.Mesh;
        public override bool Equals(object value)=>value is SurfaceRequest other&&Equals(other);
        public override int GetHashCode()=>point.GetHashCode()^normal.GetHashCode()*17^targetMatrix.GetHashCode()*31^
            (collider!=null?collider.GetEntityId().GetHashCode():0)^(left?101:0)^pitch.GetHashCode()^yaw.GetHashCode()^minPitch.GetHashCode()^(bothYawSigns?719:0)^
            (mesh.Filter!=null?mesh.Filter.GetEntityId().GetHashCode():0)^(mesh.Mesh!=null?mesh.Mesh.GetEntityId().GetHashCode():0);
    }
    sealed class SurfaceCandidate
    {
        public CatPawReachPlan plan;
        public int bodyProfileKey;
        public CatBodyGuardBox[] paws;
        public int pawBuildCursor;
        public CatBodyGuardBox[] bodyBoxes;
        public int bodyBuildCursor;
        public bool upperScreened,endpointScreened,transientPaws,unreachable;
    }
    sealed class SurfaceRequestGeometry
    {
        // Parameters stay in bend/sample order even when a moving collider
        // changes which plan is physically possible. Endpoint refusals below
        // last only for one search pass, never as an approval to start.
        public readonly List<SurfaceCandidate> candidates=new List<SurfaceCandidate>(8);
        public int cursor,profiledPaws,scanIndex;
        public bool scanning,fixedBodyScreened,fixedArmsScreened;
        public readonly HashSet<int> blockedEndpoints=new HashSet<int>();
        public SurfaceCandidate accepted;
        public CatBodyGuardBox[] fixedArms;
    }
    static readonly Dictionary<CatPawReachCatalog.Entry,SurfaceGeometry> surfaceGeometryCache=new Dictionary<CatPawReachCatalog.Entry,SurfaceGeometry>();
    static CatMovement surfaceActor;
    static CatBreedVisualTag surfaceVisual;
    static Matrix4x4 surfaceMatrix;
    // Diagnostic counters are work counts, not a claim about device timings.
    public static int SurfaceMathCandidatesLastQuery {get;private set;}
    public static float SurfaceReachError {get;private set;}
    public static float SurfaceReachDistance {get;private set;}
    public static float SurfaceReachLimit {get;private set;}
    public static int SurfacePhysicsCandidatesLastQuery {get;private set;}

    // A query can be geometrically undecided across frames. A false result with
    // Pending set must not be treated as a completed refusal by target iterators.
    public static bool SurfaceQueryPending {get;private set;}
    public const double SurfaceSearchBudgetMilliseconds=2.0;
    static int surfaceBudgetFrame=-1;
    static long surfaceBudgetDeadline;
    static bool SurfaceTimeAvailable()
    {
        int frame=Time.frameCount;
        if(surfaceBudgetFrame!=frame)
        {
            surfaceBudgetFrame=frame;
            surfaceBudgetDeadline=System.Diagnostics.Stopwatch.GetTimestamp()+
                (long)(System.Diagnostics.Stopwatch.Frequency*SurfaceSearchBudgetMilliseconds/1000.0);
        }
        return System.Diagnostics.Stopwatch.GetTimestamp()<surfaceBudgetDeadline;
    }
    static bool SurfaceDeferred(){SurfaceQueryPending=true;return false;}

    public static bool TryResolveSurface(CatMovement actor,RaycastHit surface,bool left,CatActivityPose pose,
        out CatPawReachPlan plan,float maxPitch=MaximumChestPitch,float maxYaw=0f)
        => ResolveSurface(actor,surface,default,left,pose,out plan,maxPitch,maxYaw,0f,false);

    /// <summary>Real rendered prop triangles; a collider is not required or created.</summary>
    public static bool TryResolveSurface(CatMovement actor,CatMeshContactSurface.Hit surface,bool left,CatActivityPose pose,
        out CatPawReachPlan plan,float maxPitch=MaximumChestPitch,float maxYaw=0f,float minPitch=0f,bool bothYawSigns=false)
        => ResolveSurface(actor,default,surface,left,pose,out plan,maxPitch,maxYaw,minPitch,bothYawSigns);

    static bool ResolveSurface(CatMovement actor,RaycastHit surface,CatMeshContactSurface.Hit mesh,bool left,CatActivityPose pose,
        out CatPawReachPlan plan,float maxPitch,float maxYaw,float minPitch,bool bothYawSigns)
    {
        plan=default;SurfaceQueryPending=false;SurfaceMathCandidatesLastQuery=SurfacePhysicsCandidatesLastQuery=0;
        SurfaceTimeAvailable();
        bool measuredMesh=mesh.Filter!=null;
        if(actor==null||(measuredMesh?(!mesh.IsValid||mesh.Owner==null):surface.collider==null))return false;
        Transform target=measuredMesh?mesh.Filter.transform:surface.collider.transform;
        Vector3 targetPoint=measuredMesh?mesh.Point:surface.point,targetNormal=measuredMesh?mesh.Normal:surface.normal;
        if(targetNormal.sqrMagnitude<.000001f)return false;
        var visual=actor.GetComponentInChildren<CatBreedVisualTag>();
        if(visual==null)return false;
        var entry=CatPawReachCatalog.LoadSurface(visual.BreedId,pose,out bool loading);
        if(loading)return SurfaceDeferred();
        if(entry?.samples==null||entry.samples.Length<4)return false;
        var matrix=visual.transform.localToWorldMatrix;
        if(surfaceActor!=actor||surfaceVisual!=visual||!surfaceMatrix.Equals(matrix))
        {surfaceGeometryCache.Clear();surfaceActor=actor;surfaceVisual=visual;surfaceMatrix=matrix;}
        if(!surfaceGeometryCache.TryGetValue(entry,out var geometry))
        {
            if(!SurfaceTimeAvailable())return SurfaceDeferred();
            geometry=BuildSurfaceGeometry(entry,matrix,visual.transform.lossyScale);
            if(geometry==null)return false;surfaceGeometryCache.Add(entry,geometry);
        }
        if(!geometry.ready)
        {
            if(geometry.invalid)return false;
            if(!ContinueSurfaceGeometry(geometry,matrix))return false;
        }
        var request=new SurfaceRequest{source=surface,collider=surface.collider,mesh=mesh,target=target,point=targetPoint,normal=targetNormal.normalized,
            targetMatrix=target.localToWorldMatrix,left=left,
            pitch=Mathf.Clamp(maxPitch,0,MaximumChestPitch),yaw=Mathf.Clamp(maxYaw,0,MaximumChestYaw),
            minPitch=pose==CatActivityPose.Paw?Mathf.Clamp(minPitch,-MaximumChestPitch,0):0,bothYawSigns=bothYawSigns};
        if(!geometry.requests.TryGetValue(request,out var cached))
        {
            if(!SurfaceTimeAvailable())return SurfaceDeferred();
            if(geometry.requests.Count>=64)geometry.requests.Clear();
            cached=new SurfaceRequestGeometry();geometry.requests.Add(request,cached);
        }
        // Every bend/arc starts and ends on exactly the same unmodified source
        // limbs. Reject that shared obstruction before searching thousands of
        // intermediate bends. Geometry is cached; permission stays fresh.
        if (cached.fixedArms == null)
        {
            if (!SurfaceTimeAvailable()) return SurfaceDeferred();
            cached.fixedArms = new CatBodyGuardBox[geometry.PawStride * 2];
            for (int end = 0; end < 2; end++)
            {
                var frame = geometry.all[end == 0 || SurfaceReturnsToStart(geometry.entry.pose) ? 0 : geometry.all.Length - 1];
                CatPawWeightedSkin.Fill(frame.leftSkin, frame.pivot, Quaternion.identity, false, true,
                    default, default, default, geometry.leftArmScratch);
                CatPawWeightedSkin.Fill(frame.rightSkin, frame.pivot, Quaternion.identity, false, true,
                    default, default, default, geometry.rightArmScratch);
                int at = end * geometry.PawStride;
                foreach (var indices in geometry.leftRegions)
                    cached.fixedArms[at++] = CatPawSurfaceRegions.FitBox(geometry.leftArmScratch, indices, request.normal);
                foreach (var indices in geometry.rightRegions)
                    cached.fixedArms[at++] = CatPawSurfaceRegions.FitBox(geometry.rightArmScratch, indices, request.normal);
            }
        }
        if(!cached.scanning||!cached.fixedArmsScreened||cached.accepted!=null)
        {
            long fixedTime=BeginSurfaceMeasure();
            bool fixedArmsClear=SurfaceBoxesClear(actor,geometry,default,cached.fixedArms,-2)&&(!measuredMesh||mesh.Owner.BoxesClear(cached.fixedArms,.002f));
            EndSurfaceMeasure(fixedTime,"fixed-arms",-1,fixedArmsClear);
            if (!fixedArmsClear)
            { cached.scanning = false;cached.fixedArmsScreened=false; return false; }
            cached.fixedArmsScreened=true;
        }
        SurfaceCandidate rejectedAccepted=null;
        // Returning a previously accepted plan always performs the complete
        // CURRENT physics check. This bounded validation can exceed the search
        // slice; permission is never based on checks from an earlier frame.
        if(cached.accepted!=null)
        {
            long warmTime=BeginSurfaceMeasure();
            bool warmClear=SurfaceFixedBodyClear(actor,geometry)&&SurfaceCandidateClear(actor,geometry,cached.accepted);
            EndSurfaceMeasure(warmTime,"warm-full-fresh",-1,warmClear);
            if(warmClear)
            {plan=cached.accepted.plan;return true;}
            // Retain geometry as the first retry even after physics rejects it.
            // Removing an obstruction must recover immediately, including when
            // the same frame has already spent its cold-search budget.
            rejectedAccepted=cached.accepted;
        }
        if(!cached.scanning)
        {cached.scanning=true;cached.scanIndex=0;cached.fixedBodyScreened=false;cached.blockedEndpoints.Clear();}
        while(true)
        {
            if(!SurfaceTimeAvailable())return SurfaceDeferred();
            SurfaceCandidate candidate;
            if(cached.scanIndex<cached.candidates.Count)candidate=cached.candidates[cached.scanIndex];
            else
            {
                candidate=NextSurfaceCandidate(actor,geometry,request,cached,pose);
                if(candidate==null)
                {
                    if(!SurfaceQueryPending){cached.scanning=false;cached.scanIndex=0;}
                    return false;
                }
                cached.candidates.Add(candidate);
            }
            if(candidate==rejectedAccepted){cached.scanIndex++;continue;}
            if(!cached.fixedBodyScreened)
            {
                // Early rejection only. Repeating this complete source check
                // after every yield can exhaust the entire search slice before
                // a single candidate advances. Final and warm acceptance still
                // recheck this body and every candidate against CURRENT physics.
                long fixedBodyTime=BeginSurfaceMeasure();
                bool fixedBodyClear=SurfaceFixedBodyClear(actor,geometry);
                EndSurfaceMeasure(fixedBodyTime,"fixed-body-cold",-1,fixedBodyClear);
                if(!fixedBodyClear){cached.scanning=false;return false;}
                cached.fixedBodyScreened=true;
            }
            int result=ContinueSurfaceCandidate(actor,geometry,cached,candidate);
            if(result==0)return SurfaceDeferred();
            if(result<0){candidate.bodyBoxes=null;candidate.bodyBuildCursor=0;cached.scanIndex++;continue;}
            if(cached.accepted!=null&&cached.accepted!=candidate){cached.accepted.bodyBoxes=null;cached.accepted.bodyBuildCursor=0;}
            cached.accepted=candidate;candidate.transientPaws=false;
            cached.scanning=false;plan=candidate.plan;return true;
        }
    }
    static SurfaceGeometry BuildSurfaceGeometry(CatPawReachCatalog.Entry entry,Matrix4x4 matrix,Vector3 scale)
    {
        if(entry.surfaceGeometryVersion!=CatPawReachCatalog.SurfaceGeometryVersion||
            entry.leftPawTriangles==null||entry.rightPawTriangles==null||
            entry.leftPawTriangles.Length==0||entry.rightPawTriangles.Length==0||
            entry.skinBones==null||entry.skinBones.Length==0||entry.leftArmSkin==null||entry.rightArmSkin==null||
            entry.leftArmSkin.Length==0||entry.rightArmSkin.Length==0||entry.leftArmTriangles==null||entry.rightArmTriangles==null)return null;
        var body=TransformSource(entry,matrix,scale);if(body==null)return null;
        var bodySurface=BuildSurfaceBodyGeometry(entry);
        if(entry.pose==CatActivityPose.Paw&&(bodySurface==null||SurfaceSourceCount(entry)<4))return null;
        var leftRegions=CatPawSurfaceRegions.Build(entry.leftArmSkin,entry.leftArmTriangles,CatPawWeightedSkin.MaximumBoneGroups);
        var rightRegions=CatPawSurfaceRegions.Build(entry.rightArmSkin,entry.rightArmTriangles,CatPawWeightedSkin.MaximumBoneGroups);
        if(leftRegions.Length==0||rightRegions.Length==0)return null;
        var leftFaces=CatPawSurfaceRegions.CoveredFaces(leftRegions,entry.leftArmTriangles);
        var rightFaces=CatPawSurfaceRegions.CoveredFaces(rightRegions,entry.rightArmTriangles);
        if(leftFaces==null||rightFaces==null)return null;
        return new SurfaceGeometry{entry=entry,body=body,bodySurface=bodySurface,all=new SurfaceSampleGeometry[SurfaceSourceCount(entry)],
            contacts=new SurfaceSampleGeometry[entry.pose==CatActivityPose.Paw?3:4],leftScratch=new Vector3[entry.leftPaw.Length],rightScratch=new Vector3[entry.rightPaw.Length],
            leftArmScratch=new Vector3[entry.leftArmSkin.Length],rightArmScratch=new Vector3[entry.rightArmSkin.Length],
            leftRegions=leftRegions,rightRegions=rightRegions,leftFaces=leftFaces,rightFaces=rightFaces,endpointScratch=new CatBodyGuardBox[leftRegions.Length+rightRegions.Length],
            upperScratch=new CatBodyGuardCatalog.Probe[entry.samples.Length*3]};
    }
    static bool ContinueSurfaceGeometry(SurfaceGeometry geometry,Matrix4x4 matrix)
    {
        while(geometry.buildCursor<geometry.all.Length)
        {
            if(!SurfaceTimeAvailable())return SurfaceDeferred();
            int i=geometry.buildCursor;var sample=geometry.entry.samples[i];
            // Refuse an old sparse catalog: verified trajectory gaps must be at
            // most .05, with explicit start/end and the four contact phases.
            if(sample==null||(i==0?Mathf.Abs(sample.phase)>.00001f:
                sample.phase<=geometry.entry.samples[i-1].phase||sample.phase-geometry.entry.samples[i-1].phase>.05001f))
            {geometry.invalid=true;return false;}
            var left=BuildSurfaceArm(sample.left,sample.leftPaw,geometry.entry.leftPaw,matrix);
            var right=BuildSurfaceArm(sample.right,sample.rightPaw,geometry.entry.rightPaw,matrix);
            if(left==null||right==null){geometry.invalid=true;return false;}
            var value=new SurfaceSampleGeometry{sample=sample,pivot=World(matrix,sample.torsoPivot),left=left,right=right};
            value.leftSkin=CatPawWeightedSkin.Build(geometry.entry,sample,matrix,geometry.entry.leftArmSkin);
            value.rightSkin=CatPawWeightedSkin.Build(geometry.entry,sample,matrix,geometry.entry.rightArmSkin);
            if(value.leftSkin==null||value.rightSkin==null){geometry.invalid=true;return false;}
            if(geometry.bodySurface!=null)
            {
                value.bodySkin=new CatPawWeightedSkin.Frame[4];
                for(int region=0;region<4;region++)
                {value.bodySkin[region]=CatPawWeightedSkin.Build(geometry.entry,sample,matrix,geometry.entry.bodySurface[region].skin);
                    if(value.bodySkin[region]==null){geometry.invalid=true;return false;}}
            }
            geometry.all[i]=value;geometry.buildCursor++;
            if(CatPawSurfacePlan.ContactPhase(geometry.entry.pose,sample.phase))
            {
                if(geometry.contactCount==geometry.contacts.Length){geometry.invalid=true;return false;}
                geometry.contacts[geometry.contactCount++]=value;
            }
        }
        float expectedEnd=geometry.entry.pose==CatActivityPose.Paw?.25f:geometry.entry.pose==CatActivityPose.Scratch?.62f:1f;
        geometry.ready=geometry.contactCount==geometry.contacts.Length&&
            Mathf.Abs(geometry.entry.samples[geometry.all.Length-1].phase-expectedEnd)<.00001f;
        geometry.invalid=!geometry.ready;return geometry.ready;
    }
    static bool SurfaceReturnsToStart(CatActivityPose pose)=>pose==CatActivityPose.Paw||pose==CatActivityPose.Scratch;
    static int SurfaceSourceCount(CatPawReachCatalog.Entry entry)
    {
        if(!SurfaceReturnsToStart(entry.pose))return entry.samples.Length;
        float end=entry.pose==CatActivityPose.Paw?.25f:.62f;
        int count=0;while(count<entry.samples.Length&&entry.samples[count].phase<=end+.00001f)count++;
        return count;
    }
    // The return traverses the same source samples in reverse. Each prefix
    // vertex/face is checked once geometrically and again in fresh final physics.
    static int SurfacePathCount(SurfaceGeometry geometry,CatPawReachPlan plan)
    {
        if(plan.Surface==null||!plan.Surface.ReturnToStart)return geometry.all.Length;
        int count=0;while(count<geometry.all.Length&&geometry.all[count].sample.phase<=plan.SourcePhase+.00001f)count++;
        return count;
    }
    static SurfaceArmGeometry BuildSurfaceArm(CatPawReachCatalog.ArmChain chain,Vector3[] source,
        CatPawReachCatalog.PawVertex[] definitions,Matrix4x4 matrix)
    {
        if(source==null||definitions==null||source.Length!=definitions.Length||source.Length<3)return null;
        var arm=new SurfaceArmGeometry{upper=World(matrix,chain.upper),fore=World(matrix,chain.fore),hand=World(matrix,chain.hand),
            points=new Vector3[source.Length],patches=new int[source.Length][],patchMeans=new Vector3[source.Length]};
        var distal=new List<int>();float offset=0;
        for(int i=0;i<source.Length;i++)
        {
            arm.points[i]=World(matrix,source[i]);if(!definitions[i].distal)continue;
            distal.Add(i);offset=Mathf.Max(offset,Vector3.Distance(arm.points[i],arm.hand));
        }
        arm.distal=distal.ToArray();if(arm.distal.Length<3)return null;
        arm.maximumReach=Vector3.Distance(arm.upper,arm.fore)+Vector3.Distance(arm.fore,arm.hand)+offset+.012f;
        // Build no neighbourhoods yet. Only a selected support tip needs one;
        // the complete source path still retains every measured paw vertex.
        return arm;
    }
    static bool EnsureSurfacePatch(SurfaceArmGeometry arm,int tip)
    {
        if(arm.patches[tip]!=null)return arm.patches[tip].Length>=3;
        // A fixed six-slot insertion list finds exactly the nearest neighbours
        // without allocating or sorting a list of the complete paw mesh.
        Span<int> nearest=stackalloc int[6];Span<float> distances=stackalloc float[6];int count=0;
        foreach(int vertex in arm.distal)
        {
            float square=(arm.points[vertex]-arm.points[tip]).sqrMagnitude;
            if(square>.025f*.025f)continue;
            if(count==6&&square>=distances[5])continue;
            int at=Mathf.Min(count,5);
            while(at>0&&square<distances[at-1])at--;
            if(at>=6)continue;
            int last=Mathf.Min(count,5);
            for(int i=last;i>at;i--){nearest[i]=nearest[i-1];distances[i]=distances[i-1];}
            nearest[at]=vertex;distances[at]=square;count=Mathf.Min(6,count+1);
        }
        if(count<3){arm.patches[tip]=Array.Empty<int>();return false;}
        var patch=new int[count];Vector3 sum=Vector3.zero;
        for(int i=0;i<count;i++){patch[i]=nearest[i];sum+=arm.points[patch[i]];}
        arm.patches[tip]=patch;arm.patchMeans[tip]=sum/count;return true;
    }
    static Bend[] surfaceHeadUpBends;
    static Bend[] SurfaceHeadUpBends()
    {
        if(surfaceHeadUpBends!=null)return surfaceHeadUpBends;
        // Search a coarse subset of the existing allowed grid first. Keep
        // every original 1-degree pitch / 5-degree yaw candidate afterwards.
        // This changes discovery order only, never motion or physical limits.
        var ordered=new List<Bend>(bends.Length);
        bool Coarse(Bend bend)=>Mathf.RoundToInt(bend.pitch)%8==0&&
            (Mathf.RoundToInt(bend.yaw)%15==0||bend.yaw==MaximumChestYaw);
        // The compact source interval removes the later forward step; raise
        // its shoulders before spending all target slices on low elbows.
        // Every allowed source angle is retained, with fresh full-path proof.
        bool Raised(Bend bend)=>(bend.pitch==24f||bend.pitch==32f)&&(bend.yaw==0f||bend.yaw==15f);
        foreach(var bend in bends)if(Raised(bend))ordered.Add(bend);
        foreach(var bend in bends)if(Coarse(bend)&&!Raised(bend))ordered.Add(bend);
        foreach(var bend in bends)if(!Coarse(bend))ordered.Add(bend);
        surfaceHeadUpBends=ordered.ToArray();return surfaceHeadUpBends;
    }
    static SurfaceCandidate NextSurfaceCandidate(CatMovement actor,SurfaceGeometry geometry,SurfaceRequest request,
        SurfaceRequestGeometry cached,CatActivityPose pose)
    {
        int count=geometry.contacts.Length,signCount=request.bothYawSigns?2:1,pitchCount=request.minPitch<0?2:1;
        var orderedBends=request.minPitch<0?SurfaceHeadUpBends():bends;
        int total=orderedBends.Length*count*3*signCount*pitchCount;
        float sign=Mathf.Sign(Vector3.Dot(request.point-actor.transform.position,actor.transform.right));
        while(cached.cursor<total)
        {
            if(!SurfaceTimeAvailable()){SurfaceQueryPending=true;return null;}
            int variantIndex=cached.cursor++,arcIndex=variantIndex%3,variant=variantIndex/3;
            if(pose==CatActivityPose.Paw)arcIndex=arcIndex==0?2:arcIndex-1; // raised, direct, original outward; same3choices
            int signIndex=variant%signCount;variant/=signCount;
            int pitchIndex=variant%pitchCount;variant/=pitchCount;
            int bendIndex=variant/count,sampleIndex=variant%count;
            var bend=orderedBends[bendIndex];float pitch=pitchIndex==0?bend.pitch:-bend.pitch;
            float yaw=bend.yaw*(signIndex==0?sign:-sign);
            if(pitch<request.minPitch||pitch>request.pitch||bend.yaw>request.yaw||pitchIndex==1&&bend.pitch==0||signIndex==1&&bend.yaw==0)continue;
            // Include signed angles and sample in the shared upper-body cache.
            // Request-local indices cannot identify profiles across range changes.
            int bodyKey=((Mathf.RoundToInt(pitch)+32)*71+Mathf.RoundToInt(yaw)+35)*count+sampleIndex;
            SurfaceMathCandidatesLastQuery++;
            var sample=geometry.contacts[sampleIndex];var arm=request.left?sample.left:sample.right;
            Quaternion rotation=Quaternion.AngleAxis(yaw,Vector3.up)*Quaternion.AngleAxis(pitch,actor.transform.right);
            Vector3 upper=sample.pivot+rotation*(arm.upper-sample.pivot);
            // Conservative triangle-inequality rejection includes the actual
            // distal skin offset. It never reintroduces the wrist reach limit.
            if((upper-request.point).sqrMagnitude>arm.maximumReach*arm.maximumReach)continue;
            Vector3 hand=sample.pivot+rotation*(arm.hand-sample.pivot),normal=request.normal;
            if(Vector3.Dot(normal,hand-request.point)<0)normal=-normal;
            Vector3 sourceNormal=Quaternion.Inverse(rotation)*normal;
            int tip=-1;float front=float.PositiveInfinity;
            foreach(int v in arm.distal)
            {float dot=Vector3.Dot(arm.points[v],sourceNormal);if(dot<front){front=dot;tip=v;}}
            if(tip<0)continue;
            // CCD changes which visible skin patch faces the surface. Select
            // the support tip after that rotation, then solve again from the
            // same authored joints. Never rotate/stretch the wrist separately.
            CatPawSurfaceCcd.State solved=default;bool stable=false;
            for(int refinement=0;refinement<4;refinement++)
            {
                if(!EnsureSurfacePatch(arm,tip))break;
                Vector3 endpoint=sample.pivot+rotation*(arm.patchMeans[tip]-sample.pivot);
                solved=CatPawSurfaceCcd.Solve(upper,sample.pivot+rotation*(arm.fore-sample.pivot),hand,endpoint,request.point,1);
                Vector3 solvedNormal=Quaternion.Inverse(rotation)*Quaternion.Inverse(solved.PawRotation)*normal;
                int next=tip;float closest=float.PositiveInfinity;
                foreach(int v in arm.distal)
                {float dot=Vector3.Dot(arm.points[v],solvedNormal);if(dot<closest){closest=dot;next=v;}}
                if(next==tip){stable=true;break;}tip=next;
            }
            if(!stable)continue;
            float distance=Vector3.Distance(solved.Endpoint,request.point);if(distance>.012f)continue;
            float minimum=float.PositiveInfinity,maximum=float.NegativeInfinity;
            Vector3 approachInSource=Quaternion.Inverse(rotation)*request.normal;
            foreach(var vertex in arm.points)
            {float projection=Vector3.Dot(vertex,approachInSource);minimum=Mathf.Min(minimum,projection);maximum=Mathf.Max(maximum,projection);}
            float lift=arcIndex*(maximum-minimum+.002f);
            Vector3 approach=request.normal;
            if(pose==CatActivityPose.Paw&&arcIndex==2)
            {
                // Reuse the third variant for a raised paw path. Its height is
                // measured from the actual selected source-start skin patch,
                // not a furniture ID/height. The contact and source endpoints
                // remain exact because Goal's arc is zero at both ends.
                var firstArm=request.left?geometry.all[0].left:geometry.all[0].right;
                Vector3 sourceStart=Mean(firstArm.points,arm.patches[tip]);
                float rise=Mathf.Max(0,request.point.y-sourceStart.y);
                Vector3 outward=request.normal;
                if(outward.y<0)outward=Vector3.ProjectOnPlane(outward,Vector3.up).normalized;
                Vector3 upwardTangent=Vector3.ProjectOnPlane(Vector3.up,outward);
                approach=(outward+upwardTangent).normalized;
                if(approach.y<=.00001f)approach=Vector3.up;
                // At half weight the added vertical component supplies half
                // the measured rise, raising the hand before it reaches over
                // the tabletop. All intermediate real skin still must clear.
                lift=Mathf.Max(maximum-minimum+.002f,rise*.5f/approach.y);
            }
            var measured=new CatPawSurfacePlan{Entry=geometry.entry,ReturnToStart=SurfaceReturnsToStart(pose),Left=request.left,Vertices=arm.patches[tip],Collider=request.collider,SourceRequest=request.source,MeshHit=request.mesh,
                LocalPoint=request.target.InverseTransformPoint(request.point),
                LocalNormal=request.targetMatrix.transpose.MultiplyVector(normal).normalized,
                LocalApproachNormal=request.targetMatrix.transpose.MultiplyVector(approach).normalized,ApproachLift=lift,LimitLiftToRemainingRise=pose==CatActivityPose.Paw&&arcIndex==2};
            return new SurfaceCandidate{bodyProfileKey=bodyKey,plan=new CatPawReachPlan{
                Pose=pose,SourcePhase=sample.sample.phase,ChestPitch=pitch,ChestYaw=yaw,Left=request.left,Both=false,
                LeftTarget=request.point,RightTarget=request.point,Surface=measured,ReachMargin=.012f-distance}};
        }
        return null;
    }
    static CatBodyGuardCatalog.Probe[] SurfaceUpper(SurfaceGeometry geometry,SurfaceCandidate candidate,Vector3 right)
    {
        if(!geometry.upperProfiles.TryGetValue(candidate.bodyProfileKey,out var upper))
        {
            int length=SurfacePathCount(geometry,candidate.plan)*3;
            if(geometry.upperScratch.Length!=length)geometry.upperScratch=new CatBodyGuardCatalog.Probe[length];
            upper=geometry.upperProfiles.Count<128?new CatBodyGuardCatalog.Probe[length]:geometry.upperScratch;
            FillSurfaceBody(geometry,candidate.plan,right,upper);
            if(upper!=geometry.upperScratch)geometry.upperProfiles.Add(candidate.bodyProfileKey,upper);
        }
        return upper;
    }
    // 0 = incomplete work; -1 = rejected now; 1 = fresh full approval.
    static int ContinueSurfaceCandidate(CatMovement actor,SurfaceGeometry geometry,SurfaceRequestGeometry request,SurfaceCandidate candidate)
    {
        if(candidate.unreachable||!candidate.plan.Surface.IsValid)return -1;
        // Reject a physically blocked actual contact before constructing the
        // complete bent-body trajectory. The eventual full fresh proof below
        // remains mandatory for every body and arm source phase.
        if(!candidate.endpointScreened)
        {
            // Arc height vanishes at the exact contact. All three arcs share
            // this endpoint, including its weighted IK and skin patch. A
            // refusal eliminates sibling arcs for this search pass only.
            // Restarting a pass rechecks it; final/warm permission always
            // checks the entire current body and both arms against physics.
            if(request.blockedEndpoints.Contains(candidate.bodyProfileKey))return -1;
            if(!SurfaceTimeAvailable())return 0;
            int contactIndex=-1;
            for(int i=0;i<geometry.all.Length;i++)
                if(Mathf.Abs(geometry.all[i].sample.phase-candidate.plan.SourcePhase)<.00001f){contactIndex=i;break;}
            if(contactIndex<0||!FillSurfacePawRegions(geometry,candidate.plan,actor.transform.right,geometry.endpointScratch,contactIndex,0))
            {candidate.unreachable=true;candidate.upperScreened=false;return -1;}
            // All arc variants end at the same true contact. Reject a blocked
            // endpoint before allocating/evaluating the rest of its trajectory.
            long endpointTime=BeginSurfaceMeasure();
            bool endpointWorld=SurfaceBoxesClear(actor,geometry,candidate.plan,geometry.endpointScratch,contactIndex);
            EndSurfaceMeasure(endpointTime,"endpoint-world",contactIndex,endpointWorld);
            long targetTime=BeginSurfaceMeasure();
            bool endpointTarget=candidate.plan.Surface.TargetBoxesClear(geometry.endpointScratch,.002f);
            EndSurfaceMeasure(targetTime,"endpoint-target",contactIndex,endpointTarget);
            bool endpointClear=endpointWorld&&endpointTarget;
            EndSurfaceMeasure(endpointTime,"endpoint",contactIndex,endpointClear);
            if(!endpointClear)
            {request.blockedEndpoints.Add(candidate.bodyProfileKey);candidate.upperScreened=false;return -1;}
            candidate.endpointScreened=true;
        }
        if(!candidate.upperScreened)
        {
            if(!SurfaceTimeAvailable())return 0;
            SurfacePhysicsCandidatesLastQuery++;
            long upperTime=BeginSurfaceMeasure();
            if(geometry.bodySurface!=null)
            {
                int bodyResult=ContinueSurfaceBodyCandidate(actor,geometry,candidate);
                EndSurfaceMeasure(upperTime,"body-candidate",candidate.bodyBuildCursor,bodyResult==1);
                if(bodyResult<=0)return bodyResult;
            }
            else
            {
                bool upperClear=actor.IsInteractionBodyClear(SurfaceUpper(geometry,candidate,actor.transform.right));
                EndSurfaceMeasure(upperTime,"upper-capsules",-1,upperClear);
                if(!upperClear)return -1;
            }
            candidate.upperScreened=true;
        }
        if(candidate.paws==null)
        {
            candidate.paws=new CatBodyGuardBox[SurfacePathCount(geometry,candidate.plan)*geometry.PawStride];candidate.pawBuildCursor=0;
            candidate.transientPaws=request.profiledPaws>=32;
            if(!candidate.transientPaws)request.profiledPaws++;
        }
        while(candidate.pawBuildCursor<SurfacePathCount(geometry,candidate.plan))
        {
            if(!SurfaceTimeAvailable())return 0;
            int sample=candidate.pawBuildCursor;
            long reachTime=BeginSurfaceMeasure();
            bool reachable=FillSurfacePawRegions(geometry,candidate.plan,actor.transform.right,candidate.paws,sample,sample*geometry.PawStride);
            EndSurfaceMeasure(reachTime,"arm-reach",sample,reachable);
            if(!reachable)
            {candidate.unreachable=true;candidate.upperScreened=candidate.endpointScreened=false;candidate.paws=null;return -1;}
            // An early rejection only: do not finish all source phases of a
            // path already blocked here. Complete fresh approval below remains.
            Array.Copy(candidate.paws,sample*geometry.PawStride,geometry.endpointScratch,0,geometry.PawStride);
            long sampleTime=BeginSurfaceMeasure();
            bool sampleClear=SurfaceBoxesClear(actor,geometry,candidate.plan,geometry.endpointScratch,sample)&&candidate.plan.Surface.TargetBoxesClear(geometry.endpointScratch,.002f);
            EndSurfaceMeasure(sampleTime,"arm-phase",sample,sampleClear);
            if(!sampleClear)
            {
                candidate.upperScreened=candidate.endpointScreened=false;
                if(candidate.transientPaws)candidate.paws=null;
                candidate.pawBuildCursor=0;return -1;
            }
            candidate.pawBuildCursor++;
        }
        if(!SurfaceTimeAvailable())return 0;
        // Earlier screens only save work. Recheck EVERY body and paw probe now,
        // so an obstruction introduced while this search yielded cannot pass.
        long finalTime=BeginSurfaceMeasure();
        bool clear=SurfaceFixedBodyClear(actor,geometry)&&SurfaceCandidateClear(actor,geometry,candidate);
        EndSurfaceMeasure(finalTime,"final-full-fresh",-1,clear);
        candidate.upperScreened=candidate.endpointScreened=false;
        if(!clear&&candidate.transientPaws){candidate.paws=null;candidate.pawBuildCursor=0;}
        return clear?1:-1;
    }
    static bool SurfaceCandidateClear(CatMovement actor,SurfaceGeometry geometry,SurfaceCandidate candidate)
    {
        SurfacePhysicsCandidatesLastQuery++;
        if(!candidate.plan.Surface.IsValid||candidate.paws==null||candidate.pawBuildCursor<SurfacePathCount(geometry,candidate.plan))return false;
        return SurfaceCandidateBodyClear(actor,geometry,candidate)&&
            SurfaceBoxesClear(actor,geometry,candidate.plan,candidate.paws,-1)&&candidate.plan.Surface.TargetBoxesClear(candidate.paws,.002f);
    }
    static bool SurfaceBoxesClear(CatMovement actor,SurfaceGeometry geometry,CatPawReachPlan plan,
        CatBodyGuardBox[] boxes,int fixedSample)
    {
        if(geometry.refinement==null)geometry.refinement=geometry.Refine;
        geometry.checkingActor=actor;geometry.checkingPlan=plan;geometry.checkingFixedSample=fixedSample;geometry.checkingFilled=-1;
        return actor.IsInteractionBoxesClear(boxes,.002f,geometry.refinement);
    }
    static void FillSurfaceBody(SurfaceGeometry geometry,CatPawReachPlan plan,Vector3 right,CatBodyGuardCatalog.Probe[] result)
    {
        int at=0;
        foreach(var sample in geometry.body.all)
        {
            if(plan.Surface.ReturnToStart&&sample.phase>plan.SourcePhase+.00001f)break;
            float envelope=Mathf.Clamp01(sample.phase<=plan.SourcePhase?sample.phase/plan.SourcePhase:(1-sample.phase)/(1-plan.SourcePhase));
            Quaternion bend=Quaternion.AngleAxis(plan.ChestYaw*envelope,Vector3.up)*Quaternion.AngleAxis(plan.ChestPitch*envelope,right);
            foreach(var source in sample.upper)
            {var probe=source;probe.start=sample.pivot+bend*(probe.start-sample.pivot);probe.end=sample.pivot+bend*(probe.end-sample.pivot);result[at++]=probe;}
        }
    }
    static bool FillSurfacePawSample(SurfaceGeometry geometry,CatPawReachPlan plan,Vector3 right,CatBodyGuardCatalog.Probe[] result,int sampleIndex)
    {
        if(!FillSurfacePawPoints(geometry,plan,right,sampleIndex))return false;
        result[sampleIndex*2]=FitPaw(geometry.leftScratch,"left-paw");
        result[sampleIndex*2+1]=FitPaw(geometry.rightScratch,"right-paw");
        return true;
    }
    static bool FillSurfacePawRegions(SurfaceGeometry geometry,CatPawReachPlan plan,Vector3 right,
        CatBodyGuardBox[] result,int sampleIndex,int at)
    {
        if(!FillSurfacePawPoints(geometry,plan,right,sampleIndex,true))return false;
        // Envelopes follow the measured contact plane. The path's lift
        // direction must not change clearance for an identical endpoint pose.
        foreach(var indices in geometry.leftRegions)result[at++]=CatPawSurfaceRegions.FitBox(geometry.leftArmScratch,indices,plan.Surface.Normal);
        foreach(var indices in geometry.rightRegions)result[at++]=CatPawSurfaceRegions.FitBox(geometry.rightArmScratch,indices,plan.Surface.Normal);
        return true;
    }
    static bool FillSurfacePawPoints(SurfaceGeometry geometry,CatPawReachPlan plan,Vector3 right,int sampleIndex,bool includeArms=false,bool includeBody=false)
    {
        var sample=geometry.all[sampleIndex];
        float envelope=Mathf.Clamp01(sample.sample.phase<=plan.SourcePhase?sample.sample.phase/plan.SourcePhase:(1-sample.sample.phase)/(1-plan.SourcePhase));
        Quaternion bend=Quaternion.AngleAxis(plan.ChestYaw*envelope,Vector3.up)*Quaternion.AngleAxis(plan.ChestPitch*envelope,right);
        var activeArm=plan.Left?sample.left:sample.right;
        Vector3 oldUpper=sample.pivot+bend*(activeArm.upper-sample.pivot),oldFore=sample.pivot+bend*(activeArm.fore-sample.pivot),
            oldHand=sample.pivot+bend*(activeArm.hand-sample.pivot),endpoint=Vector3.zero;
        foreach(int v in plan.Surface.Vertices)endpoint+=sample.pivot+bend*(activeArm.points[v]-sample.pivot);
        endpoint/=plan.Surface.Vertices.Length;
        var solved=CatPawSurfaceCcd.Solve(oldUpper,oldFore,oldHand,endpoint,plan.Surface.Point,envelope,plan.Surface.ApproachNormal,plan.Surface.ApproachLift,plan.Surface.LimitLiftToRemainingRise);
        Vector3 goal=CatPawSurfaceCcd.Goal(endpoint,plan.Surface.Point,envelope,plan.Surface.ApproachNormal,plan.Surface.ApproachLift,plan.Surface.LimitLiftToRemainingRise);
        float error=Vector3.Distance(solved.Endpoint,goal);
#if UNITY_EDITOR
        if(SurfaceQueryMeasurement!=null)
        {
            SurfaceReachError=error;SurfaceReachDistance=Vector3.Distance(oldUpper,goal);
            SurfaceReachLimit=Vector3.Distance(oldUpper,oldFore)+Vector3.Distance(oldFore,oldHand)+Vector3.Distance(oldHand,endpoint);
        }
#endif
        if(error>.012f)return false;
        for(int side=0;side<2;side++)
        {
            var arm=side==0?sample.left:sample.right;var points=side==0?geometry.leftScratch:geometry.rightScratch;
            for(int i=0;i<points.Length;i++)
            {
                points[i]=sample.pivot+bend*(arm.points[i]-sample.pivot);
                if((side==0)==plan.Left)points[i]=CatPawSurfaceCcd.TransformPoint(solved,oldHand,points[i]);
            }
            if(includeArms)CatPawWeightedSkin.Fill(side==0?sample.leftSkin:sample.rightSkin,sample.pivot,bend,
                true,plan.Left,solved,oldUpper,oldFore,side==0?geometry.leftArmScratch:geometry.rightArmScratch);
        }
        if(includeBody&&geometry.bodySurface!=null)
            for(int region=0;region<4;region++)CatPawWeightedSkin.Fill(sample.bodySkin[region],sample.pivot,bend,
                true,plan.Left,solved,oldUpper,oldFore,geometry.bodySurface.points[region]);
        return true;
    }
    static Vector3 World(Matrix4x4 matrix,Vector3 value)=>matrix.MultiplyPoint3x4(value)+Vector3.up*CatPawReachCatalog.GroundClearance;
    static Vector3 Mean(Vector3[] points,int[] indices)
    {Vector3 result=Vector3.zero;foreach(int index in indices)result+=points[index];return result/indices.Length;}
    static int[] SelectPatch(CatPawReachCatalog.PawVertex[] definitions,Vector3[] points,Vector3 normal)
    {
        int tip=-1;float front=float.PositiveInfinity;
        for(int i=0;i<points.Length;i++)if(definitions[i].distal)
        {float projection=Vector3.Dot(points[i],normal);if(projection<front){front=projection;tip=i;}}
        if(tip<0)return null;
        var order=new List<int>();
        for(int i=0;i<points.Length;i++)if(definitions[i].distal&&Vector3.Distance(points[i],points[tip])<=.025f)order.Add(i);
        order.Sort((a,b)=>(points[a]-points[tip]).sqrMagnitude.CompareTo((points[b]-points[tip]).sqrMagnitude));
        if(order.Count<3)return null;
        if(order.Count>6)order.RemoveRange(6,order.Count-6);
        return order.ToArray();
    }
    // Also exposes the exact prediction for native source-vs-rendered validation.
    // It returns geometry only; neither transforms nor animator state are changed.
    public static bool TryPredictSurfaceSample(CatMovement actor,CatPawReachPlan reach,CatPawReachCatalog.Sample sample,
        out Vector3[] left,out Vector3[] right,out Vector3 endpoint)
    {
        left=right=null;endpoint=Vector3.zero;
        var visual=actor!=null?actor.GetComponentInChildren<CatBreedVisualTag>():null;
        if(visual==null||reach.Surface==null||!reach.Surface.IsValid||
            reach.Surface.ReturnToStart&&sample.phase>reach.SourcePhase+.00001f)return false;
        return PredictPaws(actor,reach.Surface.Entry,visual.transform.localToWorldMatrix,reach.Surface,sample,
            reach.SourcePhase,reach.ChestPitch,reach.ChestYaw,out left,out right,out endpoint);
    }
    // Whole front-limb proof, including mixed elbow weights and seam corners.
    // Diagnostic output allocates; production uses the cached Frame and arrays.
    public static bool TryPredictSurfaceArmSample(CatMovement actor,CatPawReachPlan reach,CatPawReachCatalog.Sample sample,
        out Vector3[] left,out Vector3[] right)
    {
        left=right=null;var visual=actor!=null?actor.GetComponentInChildren<CatBreedVisualTag>():null;
        if(visual==null||reach.Surface==null||!reach.Surface.IsValid||
            reach.Surface.ReturnToStart&&sample.phase>reach.SourcePhase+.00001f)return false;
        var entry=reach.Surface.Entry;var matrix=visual.transform.localToWorldMatrix;
        float envelope=Mathf.Clamp01(sample.phase<=reach.SourcePhase?sample.phase/reach.SourcePhase:(1-sample.phase)/(1-reach.SourcePhase));
        Quaternion bend=Quaternion.AngleAxis(reach.ChestYaw*envelope,Vector3.up)*Quaternion.AngleAxis(reach.ChestPitch*envelope,actor.transform.right);
        Vector3 pivot=World(matrix,sample.torsoPivot);
        var chain=reach.Left?sample.left:sample.right;
        Vector3 upper=pivot+bend*(World(matrix,chain.upper)-pivot),fore=pivot+bend*(World(matrix,chain.fore)-pivot),
            hand=pivot+bend*(World(matrix,chain.hand)-pivot),endpoint=Vector3.zero;
        var old=reach.Left?sample.leftPaw:sample.rightPaw;
        foreach(int v in reach.Surface.Vertices)endpoint+=pivot+bend*(World(matrix,old[v])-pivot);endpoint/=reach.Surface.Vertices.Length;
        var solved=CatPawSurfaceCcd.Solve(upper,fore,hand,endpoint,reach.Surface.Point,envelope,reach.Surface.ApproachNormal,reach.Surface.ApproachLift,reach.Surface.LimitLiftToRemainingRise);
        if(Vector3.Distance(solved.Endpoint,CatPawSurfaceCcd.Goal(endpoint,reach.Surface.Point,envelope,reach.Surface.ApproachNormal,reach.Surface.ApproachLift,reach.Surface.LimitLiftToRemainingRise))>.012f)return false;
        for(int side=0;side<2;side++)
        {
            var definitions=side==0?entry.leftArmSkin:entry.rightArmSkin;
            var frame=CatPawWeightedSkin.Build(entry,sample,matrix,definitions);if(frame==null)return false;
            var points=new Vector3[definitions.Length];
            CatPawWeightedSkin.Fill(frame,pivot,bend,true,reach.Left,solved,upper,fore,points);
            if(side==0)left=points;else right=points;
        }
        return true;
    }
    static bool PawTrajectoryClear(CatMovement actor,CatPawReachCatalog.Entry entry,Matrix4x4 matrix,
        CatPawSurfacePlan plan,float contactPhase,float pitch,float yaw)
    {
        foreach(var sample in entry.samples)
        {
            if(!PredictPaws(actor,entry,matrix,plan,sample,contactPhase,pitch,yaw,out var left,out var right,out _))return false;
            pawChecks[0]=FitPaw(left,"left-paw");pawChecks[1]=FitPaw(right,"right-paw");
            if(!actor.IsInteractionBodyClear(pawChecks, .002f))return false;
        }
        return true;
    }
    static bool PredictPaws(CatMovement actor,CatPawReachCatalog.Entry entry,Matrix4x4 matrix,CatPawSurfacePlan plan,
        CatPawReachCatalog.Sample sample,float contactPhase,float pitch,float yaw,
        out Vector3[] left,out Vector3[] right,out Vector3 endpoint)
    {
        left=right=null;endpoint=Vector3.zero;
        if(plan.ReturnToStart&&sample.phase>contactPhase+.00001f)return false;
        float envelope=Mathf.Clamp01(sample.phase<=contactPhase?sample.phase/contactPhase:(1-sample.phase)/(1-contactPhase));
        Quaternion bend=Quaternion.AngleAxis(yaw*envelope,Vector3.up)*Quaternion.AngleAxis(pitch*envelope,actor.transform.right);
        Vector3 pivot=World(matrix,sample.torsoPivot);
        for(int side=0;side<2;side++)
        {
            var arm=side==0?sample.left:sample.right;var sourcePoints=side==0?sample.leftPaw:sample.rightPaw;
            var definitions=side==0?entry.leftPaw:entry.rightPaw;
            if(sourcePoints==null||sourcePoints.Length<3||sourcePoints.Length!=definitions.Length)return false;
            var points=new Vector3[sourcePoints.Length];
            for(int i=0;i<points.Length;i++)points[i]=pivot+bend*(World(matrix,sourcePoints[i])-pivot);
            if((side==0)==plan.Left)
            {
                Vector3 upper=pivot+bend*(World(matrix,arm.upper)-pivot),fore=pivot+bend*(World(matrix,arm.fore)-pivot),hand=pivot+bend*(World(matrix,arm.hand)-pivot);
                Vector3 sourceEndpoint=Mean(points,plan.Vertices);
                var solved=CatPawSurfaceCcd.Solve(upper,fore,hand,sourceEndpoint,plan.Point,envelope,plan.ApproachNormal,plan.ApproachLift,plan.LimitLiftToRemainingRise);
                if(Vector3.Distance(solved.Endpoint,CatPawSurfaceCcd.Goal(sourceEndpoint,plan.Point,envelope,plan.ApproachNormal,plan.ApproachLift,plan.LimitLiftToRemainingRise))>.012f)return false;
                for(int i=0;i<points.Length;i++)points[i]=CatPawSurfaceCcd.TransformPoint(solved,hand,points[i]);
                endpoint=solved.Endpoint;
            }
            if(side==0)left=points;else right=points;
        }
        return true;
    }
    static CatBodyGuardCatalog.Probe FitPaw(Vector3[] points,string region)
    {
        var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);
        var best=default(CatBodyGuardCatalog.Probe);float volume=float.PositiveInfinity;
        for(int axis=0;axis<3;axis++)
        {
            Vector3 centre=bounds.center;float radial=0;
            foreach(var point in points){Vector3 p=point-centre;p[axis]=0;radial=Mathf.Max(radial,p.sqrMagnitude);}
            float first=float.PositiveInfinity,last=float.NegativeInfinity;
            foreach(var point in points)
            {Vector3 p=point-centre;p[axis]=0;float cap=Mathf.Sqrt(Mathf.Max(0,radial-p.sqrMagnitude));first=Mathf.Min(first,point[axis]+cap);last=Mathf.Max(last,point[axis]-cap);}
            if(first>last)first=last=(first+last)*.5f;
            Vector3 a=centre,b=centre;a[axis]=first;b[axis]=last;float radius=Mathf.Sqrt(radial)+.001f;
            float candidate=Mathf.PI*radius*radius*(Vector3.Distance(a,b)+4f*radius/3f);
            if(candidate>=volume)continue;volume=candidate;best=new CatBodyGuardCatalog.Probe{region=region,start=a,end=b,radius=radius};
        }
        return best;
    }
}
