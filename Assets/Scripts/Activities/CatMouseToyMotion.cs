using UnityEngine;

// Mouse-only hunting overlay. Reuses rig measurements and skin contact, not the spring animation.
[DefaultExecutionOrder(575)]
public sealed class CatMouseToyMotion : MonoBehaviour
{
    sealed class RestPose {public Quaternion[] rotations;public Vector3[] feet;public Quaternion[] footRotations;}
    static readonly System.Collections.Generic.Dictionary<Transform,RestPose> rests=new System.Collections.Generic.Dictionary<Transform,RestPose>();
    RestPose rest;
    static RestPose NativePose(CatMovement actor,CatSpringGeometry g)
    {
        if(rests.TryGetValue(g.Visual,out var saved))return saved;
        var pose=new RestPose{rotations=new Quaternion[g.Bones.Length],feet=new Vector3[4],footRotations=new Quaternion[4]};
        for(int i=0;i<g.Bones.Length;i++)pose.rotations[i]=g.Bones[i].localRotation;
        var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
        var tag=actor.GetComponentInChildren<CatBreedVisualTag>();var entry=CatBreedCatalog.Load().Find(tag.BreedId);
        for(int i=0;i<4;i++){string side=i%2==0?"L":"R";var foot=CatBreedVisualFactory.FindDescendant(actor.transform,(i<2?"DEF-hand.":"DEF-foot.")+side);float sole=float.PositiveInfinity;foreach(int v in entry.SupportPawVertices(i))sole=Mathf.Min(sole,skin.transform.TransformPoint(vertices[v]).y);var point=foot.position;point.y+=actor.transform.position.y-sole;pose.feet[i]=actor.transform.InverseTransformPoint(point);pose.footRotations[i]=Quaternion.Inverse(actor.transform.rotation)*foot.rotation;}
        Object.Destroy(mesh);rests[g.Visual]=pose;return pose;
    }
    CatSpringGeometry geometry;
    CatToyContactMotion contact;
    CatFurnitureGaze gaze;
    CatPawSurfacePlan surface;
    Transform chest,neck,tail;
    readonly Transform[] upper=new Transform[4],lower=new Transform[4],foot=new Transform[4];
    readonly Vector3[] anchors=new Vector3[4];
    Vector3[] positions;Quaternion[] rotations;Vector3 visualPosition;
    bool adjusted,requested,left;float crouch,reach,settle,contactLean;Vector3 look;
    public float SupportDrift {get;private set;}
    public float BodyTransfer {get;private set;}
    public static float ApproachDistance(CatSpringGeometry g)=>g.Reach*.48f;
    public static bool Resolve(CatMovement actor,Transform moving,float floor,ref CatMeshContactSurface.TargetSet surfaces,out CatActivityStart start,out CatMeshContactSurface.Hit hit)
    {
        start=default;hit=default;var g=CatSpringGeometry.Measure(actor);
        if(g==null||moving==null||!CatMeshContactSurface.IsReady)return false;NativePose(actor,g);
        if(surfaces==null||surfaces.Root!=moving)surfaces=new CatMeshContactSurface.TargetSet(moving);
        Vector3 away=Vector3.ProjectOnPlane(actor.transform.position-moving.position,Vector3.up).normalized;
        if(away.sqrMagnitude<.5f)return false;
        Bounds b=new Bounds(moving.position,Vector3.zero);bool first=true;
        foreach(var r in moving.GetComponentsInChildren<Renderer>()){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
        var query=b.center+away*(b.extents.magnitude+g.Reach);query.y=b.center.y+b.extents.y*.35f;
        if(!surfaces.TryClosest(query,out hit))return false;
        float vertical=Mathf.Max(0,actor.transform.position.y+g.ShoulderLocal.y*g.Scale-g.Reach*.12f-hit.Point.y);
        float horizontal=Mathf.Sqrt(Mathf.Max(0,g.Reach*g.Reach*.86f*.86f-vertical*vertical));
        float distance=Mathf.Max(g.ShoulderLocal.z*g.Scale+horizontal,g.MuzzleLocal*g.Scale+g.Reach*.10f);
        Vector3 finish=hit.Point+away*distance;finish.y=floor;
        Vector3 startPoint=finish+away*ApproachDistance(g);
        bool ready=CatActivityStartResolver.Facing(actor,startPoint,g.Reach*.18f,hit.Point,32f,out start);
        return ready&&CatActivityMotion.ClearSegment(actor.transform.position,finish,CatActivityMotion.ControllerFloorRadius(actor))&&actor.IsInteractionPoseClear(finish,Quaternion.LookRotation(-away));
    }
    public void Begin(bool useLeft,CatMeshContactSurface.Hit hit)
    {
        Clear();left=useLeft;geometry=CatSpringGeometry.Measure(GetComponent<CatMovement>());
        rest=NativePose(GetComponent<CatMovement>(),geometry);positions=new Vector3[geometry.Bones.Length];rotations=new Quaternion[positions.Length];
        contact=GetComponent<CatToyContactMotion>();gaze=GetComponent<CatFurnitureGaze>()??gameObject.AddComponent<CatFurnitureGaze>();
        chest=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine.003");neck=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine.005");tail=CatBreedVisualFactory.FindDescendant(transform,"DEF-tail.003");
        for(int i=0;i<4;i++){string side=i%2==0?"L":"R";upper[i]=CatBreedVisualFactory.FindDescendant(transform,(i<2?"DEF-upper_arm.":"DEF-thigh.")+side);lower[i]=CatBreedVisualFactory.FindDescendant(transform,(i<2?"DEF-forearm.":"DEF-shin.")+side);foot[i]=CatBreedVisualFactory.FindDescendant(transform,(i<2?"DEF-hand.":"DEF-foot.")+side);anchors[i]=transform.TransformPoint(rest.feet[i]);}

        surface=geometry.PawPlan(left,hit);if(surface!=null)surface.ApproachLift=geometry.Reach*.22f;
        float floor=transform.position.y;
        foreach(var hitFloor in Physics.RaycastAll(transform.position+Vector3.up*geometry.Height,Vector3.down,geometry.Height*2,~0,QueryTriggerInteraction.Ignore))if(hitFloor.collider.GetComponentInParent<CatMovement>()==null&&hitFloor.normal.y>.7f&&hitFloor.point.y<=transform.position.y+geometry.Paw*.1f)floor=Mathf.Min(floor,hitFloor.point.y);
        for(int i=0;i<4;i++)anchors[i].y=floor+rest.feet[i].y*geometry.Scale;
        SupportDrift=0;BodyTransfer=0;contactLean=0;
    }
    public void Sample(Vector3 target,float body,float paw,float release=1){look=target;crouch=body;reach=paw;settle=release;requested=true;}
    void Update(){Restore();}
    void LateUpdate()
    {
        if(!requested||geometry==null)return;requested=false;adjusted=true;visualPosition=geometry.Visual.position;
        for(int i=0;i<positions.Length;i++){positions[i]=geometry.Bones[i].localPosition;rotations[i]=geometry.Bones[i].localRotation;geometry.Bones[i].localPosition=geometry.BonePositions[i];
            string name=geometry.Bones[i].name;
            if(name=="DEF-spine"||name=="DEF-spine.001"||name=="DEF-spine.002"||name.Contains("pelvis")||name.Contains("thigh")||name.Contains("shin")||name.Contains("foot")||name.Contains("toe")||name.Contains("upper_arm")||name.Contains("forearm")||name.Contains("hand"))geometry.Bones[i].localRotation=rest.rotations[i];}
        geometry.Visual.position+=Vector3.up*(anchors[2].y-foot[2].position.y-geometry.Reach*.20f*crouch);
        float error=left?contact.LeftDistance:contact.RightDistance;
        if(reach>.99f&&!float.IsInfinity(error))contactLean=Mathf.Min(geometry.Reach*.24f,contactLean+Mathf.Clamp(error-geometry.Paw*.04f,0,geometry.Reach*.10f));
        Vector3 transfer=transform.forward*(geometry.Reach*.10f*crouch+contactLean*reach);
        if(chest!=null){chest.position+=transfer;chest.rotation=Quaternion.AngleAxis(3*crouch,transform.right)*chest.rotation;}
        BodyTransfer=Mathf.Max(BodyTransfer,transfer.magnitude);
        // A low prey needs neck pitch as well as bounded head tracking.
        if(neck!=null)neck.rotation=Quaternion.AngleAxis(8*settle,transform.right)*neck.rotation;
        if(tail!=null)tail.rotation=Quaternion.AngleAxis(Mathf.Sin(Time.time*5)*3*crouch,Vector3.up)*tail.rotation;
        for(int i=0;i<4;i++)
        {
            if(i==(left?0:1)&&reach>0)continue;
            for(int n=0;n<24;n++){Aim(lower[i],foot[i],anchors[i]);Aim(upper[i],foot[i],anchors[i]);}
            foot[i].rotation=transform.rotation*rest.footRotations[i];SupportDrift=Mathf.Max(SupportDrift,Vector3.Distance(foot[i].position,anchors[i]));
        }
        if(surface!=null&&reach>0)contact.ReachSurface(surface,reach);
        gaze.LookAt(look,settle,38);
    }
    public static bool Escape(Transform moving,Transform owner,Vector3 desired,float distance,out Vector3 end)
    {
        Bounds b=new Bounds(moving.position,Vector3.zero);bool first=true;foreach(var r in moving.GetComponentsInChildren<Renderer>()){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
        foreach(float angle in new[]{0f,35f,-35f,70f,-70f,100f,-100f})
        {
            var delta=Quaternion.AngleAxis(angle,Vector3.up)*desired*distance;bool clear=true;
            for(int i=1;i<=5&&clear;i++)foreach(var c in Physics.OverlapBox(b.center+delta*(i/5f),b.extents*.88f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
            {if(c.transform.IsChildOf(owner)||c.GetComponentInParent<CatMovement>()!=null||c.bounds.max.y<=b.min.y+b.size.y*.04f)continue;clear=false;break;}
            if(clear)foreach(var r in owner.GetComponentsInChildren<Renderer>())
            {
                if(r.transform.IsChildOf(moving))continue;
                float initial=OverlapVolume(b,r.bounds);
                for(int i=1;i<=5;i++){var translated=b;translated.center+=delta*(i/5f);if(OverlapVolume(translated,r.bounds)>initial+ b.size.x*b.size.y*b.size.z*.015f){clear=false;break;}}
                if(!clear)break;
            }
            if(clear){end=moving.position+delta;return true;}
        }
        end=moving.position;return false;
    }
    static float OverlapVolume(Bounds a,Bounds b){var extent=Vector3.Min(a.max,b.max)-Vector3.Max(a.min,b.min);return Mathf.Max(0,extent.x)*Mathf.Max(0,extent.y)*Mathf.Max(0,extent.z);}
    static void Aim(Transform joint,Transform end,Vector3 target){var a=end.position-joint.position;var b=target-joint.position;if(a.sqrMagnitude>.000001f&&b.sqrMagnitude>.000001f)joint.rotation=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(a,b),18)*joint.rotation;}
    void Restore(){if(!adjusted)return;adjusted=false;for(int i=0;i<positions.Length;i++)if(geometry.Bones[i]!=null){geometry.Bones[i].localPosition=positions[i];geometry.Bones[i].localRotation=rotations[i];}if(geometry.Visual!=null)geometry.Visual.position=visualPosition;}
    public void Clear(){Restore();requested=false;contact?.Clear();gaze?.Clear();}
    void OnDisable(){Clear();}
}
