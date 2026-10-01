using System.Collections.Generic;
using UnityEngine;

// One normalized measurement per live visual. Shared by every breed; no breed constants.
public sealed class CatSpringGeometry
{
    static readonly Dictionary<CatMovement,CatSpringGeometry> cache=new Dictionary<CatMovement,CatSpringGeometry>();
    public Transform Root,Visual,Head,Upper,Fore,Hand;
    public float HeightLocal,MuzzleLocal,ReachLocal,PawLocal;
    public Vector3 ShoulderLocal;
    public int[] HeadVertices;
    public Transform[] Bones;
    public Vector3[] BonePositions;
    public Vector3 LeftHandLocal,RightHandLocal;
    public Transform Rear;
    public float RearHeightLocal;
    public float Scale=>Root.lossyScale.y;
    public float Height=>HeightLocal*Scale;
    public float Reach=>ReachLocal*Scale;
    public float Paw=>PawLocal*Scale;
    public float StandDistance=>(MuzzleLocal+ReachLocal*.12f)*Scale;
    public static CatSpringGeometry Measure(CatMovement actor)
    {
        var animator=actor.GetComponentInChildren<Animator>();if(animator==null)return null;
        if(cache.TryGetValue(actor,out var saved)&&saved.Visual==animator.transform)return saved;
        var g=new CatSpringGeometry{Root=actor.transform,Visual=animator.transform};var nativeBones=new List<Transform>();
        foreach(var b in animator.GetComponentsInChildren<Transform>())
        {
            if(b.name.StartsWith("DEF-"))nativeBones.Add(b);
            if(b.name=="DEF-foot.L")g.Rear=b;
            if(b.name=="DEF-hand.L")g.LeftHandLocal=actor.transform.InverseTransformPoint(b.position);
            if(b.name=="DEF-hand.R")g.RightHandLocal=actor.transform.InverseTransformPoint(b.position);
            if(b.name=="DEF-spine.006")g.Head=b;
            if(b.name=="DEF-upper_arm.L")g.Upper=b;
            if(b.name=="DEF-forearm.L")g.Fore=b;
            if(b.name=="DEF-hand.L")g.Hand=b;
        }
        var skin=animator.GetComponentInChildren<SkinnedMeshRenderer>();
        if(skin==null||g.Head==null||g.Upper==null||g.Fore==null||g.Hand==null)return null;
        var root=actor.transform;g.ShoulderLocal=root.InverseTransformPoint(g.Upper.position);
        g.Bones=nativeBones.ToArray();g.BonePositions=new Vector3[g.Bones.Length];for(int i=0;i<g.Bones.Length;i++)g.BonePositions[i]=g.Bones[i].localPosition;
        g.RearHeightLocal=root.InverseTransformPoint(g.Rear.position).y;
        g.ReachLocal=(Vector3.Distance(g.Upper.position,g.Fore.position)+Vector3.Distance(g.Fore.position,g.Hand.position))/g.Scale;
        var mesh=new Mesh();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
        var head=root.InverseTransformPoint(g.Head.position);float min=float.PositiveInfinity,max=float.NegativeInfinity;
        g.MuzzleLocal=head.z;
        var tag=actor.GetComponentInChildren<CatBreedVisualTag>();var entry=CatBreedCatalog.Load().Find(tag.BreedId);
        var headVertices=new List<int>();
        foreach(int i in entry.ContactVertexIndices)
        {
            Vector3 p=root.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
            min=Mathf.Min(min,p.y);max=Mathf.Max(max,p.y);
            if(p.y>g.ShoulderLocal.y*.85f)g.MuzzleLocal=Mathf.Max(g.MuzzleLocal,p.z);
            if(p.y>g.ShoulderLocal.y*.80f&&p.z>head.z-g.ReachLocal*.25f)headVertices.Add(i);
        }
        g.HeadVertices=headVertices.ToArray();g.HeightLocal=max-min;g.PawLocal=0;
        foreach(int i in entry.SupportPawVertices(0))g.PawLocal=Mathf.Max(g.PawLocal,Vector3.Distance(skin.transform.TransformPoint(vertices[i]),g.Hand.position)/g.Scale);
        Object.Destroy(mesh);cache[actor]=g;return g;
    }
    public static bool Resolve(CatMovement actor,Transform moving,float floor,ref CatMeshContactSurface.TargetSet surfaces,
        out CatActivityStart start,out CatMeshContactSurface.Hit hit)
    {
        start=default;hit=default;var g=Measure(actor);if(g==null||moving==null||!CatMeshContactSurface.IsReady)return false;
        if(surfaces==null||surfaces.Root!=moving)surfaces=new CatMeshContactSurface.TargetSet(moving);
        Vector3 outward=Vector3.ProjectOnPlane(actor.transform.position-moving.position,Vector3.up).normalized;
        if(outward.sqrMagnitude<.5f)return false;
        Bounds bounds=new Bounds(moving.position,Vector3.zero);bool first=true;
        foreach(var r in moving.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
        // Contact near shoulder height gives short cats a reachable real spring surface.
        Vector3 query=bounds.center+outward*(bounds.extents.magnitude+g.Reach);
        query.y=Mathf.Clamp(actor.transform.position.y+g.ShoulderLocal.y*g.Scale,bounds.min.y,bounds.max.y);
        if(!surfaces.TryClosest(query,out hit))return false;
        Vector3 stand=hit.Point+outward*g.StandDistance;stand.y=floor;
        bool ready=CatActivityStartResolver.Facing(actor,stand,g.Reach*.10f,hit.Point,25f,out start);
        start.ActionTarget=hit.Point;
        Vector3 direction=Vector3.ProjectOnPlane(hit.Point-actor.transform.position,Vector3.up).normalized;
        Quaternion rotation=Quaternion.LookRotation(direction);
        Vector3 shoulder=actor.transform.position+rotation*(g.ShoulderLocal*g.Scale);
        return ready&&Vector3.Distance(shoulder,hit.Point)<g.Reach+g.Paw*.75f;
    }
    public CatPawSurfacePlan PawPlan(bool left,CatMeshContactSurface.Hit hit)
    {
        var tag=Root.GetComponentInChildren<CatBreedVisualTag>();
        var entry=CatPawReachCatalog.LoadSurface(tag.BreedId,CatActivityPose.Scratch,out _);if(entry==null)return null;
        var defs=left?entry.leftPaw:entry.rightPaw;var scores=new List<KeyValuePair<int,float>>();
        for(int i=0;i<defs.Length;i++)
        {
            if(!defs[i].distal)continue;Vector3 p=Vector3.zero;
            foreach(var w in defs[i].influences){var b=Visual.Find(w.bonePath);if(b!=null)p+=b.TransformPoint(w.bindPosition)*w.weight;}
            scores.Add(new KeyValuePair<int,float>(i,(p-hit.Point).sqrMagnitude));
        }
        scores.Sort((a,b)=>a.Value.CompareTo(b.Value));if(scores.Count<3)return null;
        return new CatPawSurfacePlan{Entry=entry,Left=left,Vertices=new[]{scores[0].Key,scores[1].Key,scores[2].Key},MeshHit=hit,
            LocalPoint=hit.LocalPoint,LocalNormal=hit.LocalNormal,LocalApproachNormal=hit.Filter.transform.InverseTransformDirection(Vector3.up),ApproachLift=Reach*.16f};
    }
}
