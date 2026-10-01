using System.Collections.Generic;
using UnityEngine;

/// <summary>Actual source arm seeds projected onto owned rendered triangles.</summary>
public sealed class CatMeshPawTargetSearch
{
    struct Candidate { public CatMeshContactSurface.Hit hit; public bool left; public float cost; public bool blocked; }
    readonly CatMeshContactSurface.TargetSet surfaces;
    readonly List<Candidate> candidates=new List<Candidate>(32);
    readonly RaycastHit[] rankingHits=new RaycastHit[32];
    CatMovement actor;CatBreedVisualTag visual;CatPawReachCatalog.Entry entry;
    Matrix4x4 actorMatrix,ownerMatrix;
    int cursor;float minimumPitch,maximumPitch,maximumYaw;
    public Transform Root=>surfaces.Root;
    public bool IsPending {get;private set;}
    public CatMeshPawTargetSearch(Transform root){surfaces=new CatMeshContactSurface.TargetSet(root);}

    public bool TryResolve(CatMovement current,CatActivityPose pose,out CatPawReachPlan plan,
        float minPitch=0,float maxPitch=32,float maxYaw=0)
    {
        plan=default;IsPending=false;
        if(current==null||Root==null)return false;
        var tag=current.GetComponentInChildren<CatBreedVisualTag>();if(tag==null)return false;
        var source=CatPawReachCatalog.LoadSurface(tag.BreedId,pose,out bool loading);
        if(loading){IsPending=true;return false;}
        if(source?.samples==null)return false;
        var matrix=tag.transform.localToWorldMatrix;
        if(actor!=current||visual!=tag||entry!=source||!actorMatrix.Equals(matrix)||
            !ownerMatrix.Equals(Root.localToWorldMatrix)||minimumPitch!=minPitch||maximumPitch!=maxPitch||maximumYaw!=maxYaw)
        {
            actor=current;visual=tag;entry=source;actorMatrix=matrix;ownerMatrix=Root.localToWorldMatrix;
            minimumPitch=minPitch;maximumPitch=maxPitch;maximumYaw=maxYaw;cursor=0;candidates.Clear();
            foreach(var sample in source.samples)
            {
                if(!CatPawSurfacePlan.ContactPhase(pose,sample.phase))continue;
                for(int side=0;side<2;side++)
                {
                    var definitions=side==0?source.leftPaw:source.rightPaw;
                    var points=side==0?sample.leftPaw:sample.rightPaw;
                    if(points==null||definitions==null||points.Length!=definitions.Length)continue;
                    Vector3 mean=Vector3.zero;int count=0;
                    for(int i=0;i<points.Length;i++)if(definitions[i].distal){mean+=points[i];count++;}
                    if(count==0)continue;mean/=count;
                    Vector3 pivot=matrix.MultiplyPoint3x4(sample.torsoPivot)+Vector3.up*CatPawReachCatalog.GroundClearance;
                    Vector3 endpoint=matrix.MultiplyPoint3x4(mean)+Vector3.up*CatPawReachCatalog.GroundClearance;
                    var chain=side==0?sample.left:sample.right;
                    Vector3 fore=matrix.MultiplyPoint3x4(chain.fore)+Vector3.up*CatPawReachCatalog.GroundClearance;
                    Vector3 upper=matrix.MultiplyPoint3x4(chain.upper)+Vector3.up*CatPawReachCatalog.GroundClearance;
                    for(int bend=0;bend<(minPitch<0?3:1);bend++)
                    {
                        float pitch=bend==0?0:bend==1?minPitch*.5f:minPitch;
                        Quaternion rotation=Quaternion.AngleAxis(pitch,current.transform.right);
                        // A distal mean often projects onto a prop's underside.
                        // The same measured source forearm and shoulder supply
                        // real side/top alternatives; no authored height, model
                        // offset or hypothetical stand is introduced.
                        for(int anchor=0;anchor<(minPitch<0?3:1);anchor++)
                        {
                            Vector3 sourcePoint=anchor==0?endpoint:anchor==1?fore:upper;
                            Vector3 seed=pivot+rotation*(sourcePoint-pivot);
                            if(!surfaces.TryClosest(seed,out var hit)||hit.Normal.sqrMagnitude<.00001f)continue;
                            // Rank against the source hand, not whichever shoulder
                            // seed happened to project closest to the object. A
                            // clear direct upper-arm line is a useful first try;
                            // an obstructed line only moves the candidate later.
                            Vector3 sourceHand=pivot+rotation*(endpoint-pivot);
                            Vector3 sourceUpper=pivot+rotation*(upper-pivot);
                            var candidate=new Candidate{hit=hit,left=side==0,cost=(hit.Point-sourceHand).sqrMagnitude,
                                blocked=DirectPathBlocked(current,sourceUpper,hit.Point)};
                            int duplicate=-1;
                            for(int at=0;at<candidates.Count;at++)
                                if(candidates[at].left==candidate.left&&candidates[at].hit.Filter==hit.Filter&&
                                    (candidates[at].hit.Point-hit.Point).sqrMagnitude<.000025f){duplicate=at;break;}
                            if(duplicate<0)candidates.Add(candidate);
                            else if(Compare(candidate,candidates[duplicate])<0)candidates[duplicate]=candidate;
                        }
                    }
                }
            }
            candidates.Sort(Compare);
        }
        if(candidates.Count==0)return false;
        int first=cursor%candidates.Count;
        for(int step=0;step<candidates.Count;step++)
        {
            int index=(first+step)%candidates.Count;var candidate=candidates[index];
            if(!candidate.hit.IsValid)continue;
            if(CatPawReachResolver.TryResolveSurface(current,candidate.hit,candidate.left,pose,out plan,maxPitch,maxYaw,minPitch,true))
            {cursor=index;return true;}
            if(CatPawReachResolver.SurfaceQueryPending)
            {cursor=(index+1)%candidates.Count;IsPending=true;return false;}
        }
        cursor=0;return false;
    }
    static int Compare(Candidate a,Candidate b)
    {
        int result=a.blocked.CompareTo(b.blocked);
        return result!=0?result:a.cost.CompareTo(b.cost);
    }
    bool DirectPathBlocked(CatMovement current,Vector3 from,Vector3 target)
    {
        Vector3 delta=target-from;float distance=delta.magnitude;
        if(distance<.00001f)return false;
        int count=Physics.RaycastNonAlloc(from,delta/distance,rankingHits,distance,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        if(count==rankingHits.Length)return true;
        for(int i=0;i<count;i++)
        {
            var solid=rankingHits[i].collider;if(solid==null)continue;
            if(solid.transform.IsChildOf(current.transform)||solid.transform.IsChildOf(Root))continue;
            return true;
        }
        return false;
    }

}
