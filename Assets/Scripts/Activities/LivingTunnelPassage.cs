using System.Collections;
using UnityEngine;

// The passage belongs only to the salon CAT tunnel. Ordinary locomotion and
// the spring/mouse routines retain their existing owners and clips.
public static class LivingTunnelPassage
{
    public struct Plan
    {
        public Vector3 entry, exit, axis;
        public float speed, cycleDistance, bodyHeight, bodyWidth, roof,bodyFront,bodyRear,neckPitch;
    }
    sealed class Body {public Transform visual;public float width,height,front,rear,pitch;public readonly Vector2[] silhouette=new Vector2[96];}
    static readonly System.Collections.Generic.Dictionary<CatMovement,Body[]> bodies=new System.Collections.Generic.Dictionary<CatMovement,Body[]>();
    static readonly Collider[] obstacles=new Collider[64];
    public static string LastRejection {get;private set;}
    static Body[] Measure(CatMovement cat)
    {
        var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();if(skin==null)return null;
        if(bodies.TryGetValue(cat,out var old)&&old[0].visual==skin.transform)return old;
        var neck=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.004");if(neck==null)return null;
        Quaternion original=neck.localRotation;var result=new Body[5];
        try{for(int i=0;i<5;i++){neck.localRotation=original;neck.rotation=Quaternion.AngleAxis(i*8f,cat.transform.right)*neck.rotation;result[i]=MeasurePose(cat,skin);result[i].pitch=i*8f;}}
        finally{neck.localRotation=original;}
        bodies[cat]=result;return result;
    }
    static Body MeasurePose(CatMovement cat,SkinnedMeshRenderer skin)
    {
        var mesh=new Mesh();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
        var entry=CatBreedCatalog.Load().Find(cat.GetComponentInChildren<CatBreedVisualTag>().BreedId);
        var bounds=new Bounds();bool first=true;
        foreach(int i in entry.ContactVertexIndices)
        {var p=cat.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
        Object.Destroy(mesh);float scale=cat.transform.lossyScale.y;
        var result=new Body{visual=skin.transform,width=bounds.size.x*scale,height=bounds.size.y*scale,front=Mathf.Max(0,bounds.max.z)*scale,rear=Mathf.Max(0,-bounds.min.z)*scale};
        foreach(int i in entry.ContactVertexIndices)
        {
            var p=cat.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
            Vector2 point=new Vector2(p.x*scale,(p.y-bounds.min.y)*scale);
            int angle=Mathf.Clamp(Mathf.FloorToInt((Mathf.Atan2(point.x,point.y)*Mathf.Rad2Deg+90f)/180f*96),0,95);
            if(point.sqrMagnitude>result.silhouette[angle].sqrMagnitude)result.silhouette[angle]=point;
        }
        return result;
    }
    public static bool Resolve(CatEnrichmentActivity tunnel,CatMovement cat,out CatActivityStart start,out Plan plan)
    {
        start=default;plan=default;
        LastRejection="initial";
        var geometry=cat!=null?CatSpringGeometry.Measure(cat):null;
        if(geometry==null||tunnel.RoutineEntryPoint==null||tunnel.ExitPoint==null)return false;
        Vector3 a=tunnel.RoutineEntryPoint.position,b=tunnel.ExitPoint.position;
        bool reverse=(cat.transform.position-b).sqrMagnitude<(cat.transform.position-a).sqrMagnitude;
        plan.entry=reverse?b:a;plan.exit=reverse?a:b;
        plan.axis=Vector3.ProjectOnPlane(plan.exit-plan.entry,Vector3.up).normalized;
        var candidates=Measure(cat);if(candidates==null)return false;var body=candidates[0];
        plan.bodyHeight=body.height;plan.bodyWidth=body.width;plan.bodyFront=body.front;plan.bodyRear=body.rear;
        // Actual mouth planes plus this cat's body envelope determine how far
        // the centre must stand outside each opening. No breed offsets.
        Vector3 middle=(a+b)*.5f;float near=float.PositiveInfinity,far=float.NegativeInfinity;
        foreach(var mesh in tunnel.GetComponentsInChildren<MeshCollider>())
        {
            var bounds=mesh.bounds;
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            {float d=Vector3.Dot(bounds.center+new Vector3(bounds.extents.x*x,0,bounds.extents.z*z)-middle,plan.axis);near=Mathf.Min(near,d);far=Mathf.Max(far,d);}
        }
        if(float.IsInfinity(near))return false;
        float entryDistance=Mathf.Min(near-body.front-Mathf.Max(.04f,body.width*.5f),Vector3.Dot(plan.entry-middle,plan.axis));
        float exitDistance=Mathf.Max(far+body.rear+.06f,Vector3.Dot(plan.exit-middle,plan.axis));
        plan.entry=middle+plan.axis*entryDistance;plan.exit=middle+plan.axis*exitDistance;
        plan.entry.y=plan.exit.y=cat.transform.position.y;
        float radius=Mathf.Min(.18f,geometry.Reach*.65f);
        LastRejection="stance "+plan.entry+" h="+plan.bodyHeight+" w="+plan.bodyWidth+" front="+plan.bodyFront+" rear="+plan.bodyRear;
        if(!CatActivityStartResolver.Facing(cat,plan.entry,radius,plan.exit,20,out start))return false;
        // A nearly aligned player stance is followed by a real short step onto
        // the centre line, outside the mouth. Never interpolate diagonally inside.
        LastRejection="approach";if(!CatActivityMotion.ClearSegment(cat.transform.position,plan.entry,CatActivityMotion.ControllerFloorRadius(cat)))return false;
        var tag=cat.GetComponentInChildren<CatBreedVisualTag>();
        var catalog=Resources.Load<CatHomeLocomotionCatalog>(CatHomeLocomotionCatalog.ResourceName);
        var gait=catalog!=null?catalog.Find(tag.BreedId):null;
        if(gait==null)return false;
        plan.cycleDistance=gait.CycleDistance(0,cat.transform.lossyScale.x);
        plan.speed=plan.cycleDistance/gait.walkClip.length*.72f;
        Vector3 centre=(a+b)*.5f;centre.y=Mathf.Min(a.y,b.y)+.02f;
        plan.roof=float.PositiveInfinity;
        foreach(var mesh in tunnel.GetComponentsInChildren<MeshCollider>())
            if(mesh.enabled&&!mesh.isTrigger&&mesh.Raycast(new Ray(centre,Vector3.up),out var hit,2f))
                plan.roof=Mathf.Min(plan.roof,hit.point.y-centre.y);
        if(float.IsInfinity(plan.roof)){LastRejection="missing cloth roof";return false;}
        bool fits=false;LastRejection="height";
        foreach(var candidate in candidates)
        {
            if(candidate.height>plan.roof+.015f)continue;
            if(!Fits(tunnel.GetComponentsInChildren<MeshCollider>(),cat,candidate,middle,plan.axis,near,far))continue;
            body=candidate;fits=true;break;
        }
        if(!fits)return false;
        plan.neckPitch=body.pitch;plan.bodyHeight=body.height;plan.bodyWidth=body.width;
        LastRejection="corridor";bool clear=CorridorClear(tunnel,cat,plan.entry,plan.exit,plan.bodyWidth*.5f,plan.bodyHeight);if(clear)LastRejection="clear";return clear;
    }
    static bool Fits(MeshCollider[] meshes,CatMovement cat,Body body,Vector3 middle,Vector3 axis,float near,float far)
    {
        Vector3 side=Vector3.Cross(Vector3.up,axis);
        for(int section=1;section<=3;section++)
        {
            Vector3 origin=Vector3.Lerp(middle+axis*near,middle+axis*far,section*.25f);origin.y=cat.transform.position.y+.02f;
            foreach(var point in body.silhouette)
            {
                if(point.y<.03f)continue;
                Vector3 delta=side*point.x+Vector3.up*(point.y-.012f);
                foreach(var mesh in meshes)
                    if(mesh.Raycast(new Ray(origin,delta.normalized),out var hit,delta.magnitude-.005f))
                    {LastRejection="envelope pitch="+body.pitch+" point="+point;return false;}
            }
        }
        return true;
    }
    public static bool CorridorClear(CatEnrichmentActivity tunnel,CatMovement cat,Vector3 from,Vector3 to,float radius,float height)
    {
        radius=Mathf.Max(.06f,radius);
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,to)/.10f));
        for(int i=0;i<=steps;i++)
        {
            Vector3 p=Vector3.Lerp(from,to,i/(float)steps);
            int count=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*(radius+.015f),p+Vector3.up*Mathf.Max(radius+.015f,height-radius),radius,obstacles,~0,QueryTriggerInteraction.Ignore);
            if(count==obstacles.Length)return false;
            for(int j=0;j<count;j++)
                if(!obstacles[j].transform.IsChildOf(tunnel.transform)&&obstacles[j].GetComponentInParent<CatMovement>()!=cat)return false;
        }
        return true;
    }
    public static IEnumerator Play(CatEnrichmentActivity owner,CatMovement cat,Plan plan)
    {
        var pose=cat.GetComponent<CatActivityAnimation>();
        var gaze=cat.GetComponent<CatFurnitureGaze>()??cat.gameObject.AddComponent<CatFurnitureGaze>();
        float time=0;
        while(time<.28f){pose.SetWalkSpeed(0,null);gaze.LookAt(owner.ContactPoint.position+Vector3.up*plan.bodyHeight*.6f,time/.28f,25);time+=Time.deltaTime;yield return null;}
        if(Vector3.Distance(cat.transform.position,plan.entry)>.004f)
        {
            Vector3 direction=plan.entry-cat.transform.position;direction.y=0;
            yield return CatActivityFacing.Turn(cat,Quaternion.LookRotation(direction));
            while(Vector3.Distance(cat.transform.position,plan.entry)>.004f)
            {pose.SetWalkSpeed(plan.speed,null);cat.transform.position=Vector3.MoveTowards(cat.transform.position,plan.entry,plan.speed*Time.deltaTime);yield return null;}
        }
        yield return CatActivityFacing.Turn(cat,Quaternion.LookRotation(plan.axis));
        gaze.Clear();
        var crouch=cat.GetComponent<LivingTunnelCrouch>();if(crouch==null)crouch=cat.gameObject.AddComponent<LivingTunnelCrouch>();
        crouch.Set(plan.neckPitch);
        float lower=0;while(lower<.30f){lower+=Time.deltaTime;yield return null;}
        float distance=Vector3.Distance(plan.entry,plan.exit),travelled=0;
        while(travelled<distance)
        {
            if(Time.timeScale<=0){yield return null;continue;}
            float step=Mathf.Min(plan.speed*Time.deltaTime,distance-travelled);
            Vector3 next=plan.entry+plan.axis*(travelled+step);
            if(!CorridorClear(owner,cat,cat.transform.position,next,plan.bodyWidth*.5f,plan.bodyHeight))
            {owner.CancelForTransition();yield break;}
            travelled+=step;cat.transform.position=next;
            // Same original Walk source as ActivityTunnelCrawl, with a phase
            // derived from real metres rather than a fixed playback timer.
            pose.SetTimedPose(CatActivityPose.Crawl,Mathf.Repeat(travelled/plan.cycleDistance,1));
            yield return null;
        }
        crouch.Set(0);float rise=0;while(rise<.30f){rise+=Time.deltaTime;pose.SetWalkSpeed(0,null);yield return null;}
    }
}
