using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScratchPostActivity : CatActivity
{
    [SerializeField] Transform scratchPoint;
    [SerializeField,Min(.5f)] float scratchDuration=2.4f;
    [SerializeField] Transform hangingToy;
    [SerializeField] Vector3 ropeCenter=new Vector3(0,.43f,.084f);
    [SerializeField] float ropeRadius=.115f;
    CharacterController controller;
    bool wasEnabled,captured;
    Vector3 start;
    Quaternion rotation,toyRotation;
    List<Vector3> approach;
    Vector3 workingPoint;
    protected override bool UsesNearbyRoutineEntry=>true;
    CatToyContactMotion contact;
    CatActivityAnimation poses;
    MeshCollider[] measuredSurfaces;
    public int LeftStrokes {get;private set;}
    public int RightStrokes {get;private set;}
    // This routine already owns its complete floor approach and controller state.
    protected override bool UsesFloorApproach=>false;
    public override string ProgressLabel=>IsRunning?"SCRATCHING...":string.Empty;
    protected override bool CanBeginActivity(out string failureReason)
    {
        failureReason="";
        if(scratchPoint==null){failureReason="SCRATCH POST IS NOT READY";return false;}
        workingPoint=scratchPoint.position;
        if(Cat!=null)
        {
            Vector3 center=transform.TransformPoint(ropeCenter);center.y=0;
            Vector3 toward=RoutineFloorPosition-center;toward.y=0;
            if(toward.sqrMagnitude<.0001f)toward=-transform.forward;
            Vector3 camera=CatActivityFacing.CameraPosition(Cat);
            bool clear=false;approach=null;
            // Include the post's base and a small contact tolerance in the
            // capsule clearance. The rope radius alone is not the floor shape.
            for(int step=0;step<=8;step++)
            {
                float bestLength=float.PositiveInfinity;
                // A planted tree has a narrow open arc between its trunk base
                // and the boundary. A 15-degree ring can skip that whole arc.
                int directions=StoreProductId==HomeStoreService.GardenSaplingId?180:24;
                for(int side=0;side<directions;side++)
                {
                    int stepAngle=(side+1)/2*(side%2==0?-1:1);
                    float angle=stepAngle*(360f/directions);
                    float padding=.29f+step*.02f;
                    Vector3 candidate=center+(Quaternion.Euler(0,angle,0)*toward.normalized)*(ropeRadius+padding);
                    float view=CatActivityFacing.FacingDot(center-candidate,candidate,camera);
                    if(StoreProductId==HomeStoreService.ScratchPostId)
                    {
                        // The cat belongs beside the pillar in the player's
                        // view; a front-facing cat behind it is concealed.
                        if(view<.05f||view>.35f)continue;
                    }
                    else if(view<CatActivityFacing.MinimumViewDot)continue;
                    if(!CatActivityMotion.IsFloorClear(candidate)||!CatActivityMotion.TryFloorPath(RoutineFloorPosition,candidate,out var path))continue;
                    float length=0;var previous=RoutineFloorPosition;
                    foreach(var point in path){length+=Vector3.Distance(previous,point);previous=point;}
                    if(length>2.2f||length>=bestLength)continue;
                    bestLength=length;workingPoint=candidate;approach=path;clear=true;
                }
                if(clear)break;
            }
            if(!clear){failureReason="LET'S GET A LITTLE CLOSER!";return false;}
            if(!HasNearbyApproach)
            {
                if(!CatActivityMotion.TryFloorPath(Cat.transform.position,RoutineFloorPosition,out var entry))
                {failureReason="LET'S GET A LITTLE CLOSER!";return false;}
                entry.AddRange(approach);approach=entry;
            }
            return true;
        }
        if(!CatActivityMotion.TryFloorPath(Cat.transform.position,RoutineFloorPosition,out approach))
        {failureReason="LET'S GET A LITTLE CLOSER!";return false;}
        return true;
    }
    protected override bool BeginActivity()
    {
        controller=Cat.GetComponent<CharacterController>();wasEnabled=controller!=null&&controller.enabled;
        start=Cat.transform.position;rotation=Cat.transform.rotation;captured=true;
        if(hangingToy!=null)toyRotation=hangingToy.localRotation;
        Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        contact=Cat.GetComponent<CatToyContactMotion>();if(contact==null)contact=Cat.gameObject.AddComponent<CatToyContactMotion>();
        poses=Cat.GetComponent<CatActivityAnimation>();LeftStrokes=RightStrokes=0;
        measuredSurfaces=StoreProductId==HomeStoreService.BedroomWardrobeId?GetComponentsInChildren<MeshCollider>():null;
        StartCoroutine(Routine());return true;
    }
    IEnumerator Routine()
    {
        foreach(var point in approach)yield return Walk(point);
        yield return Walk(workingPoint);
        Vector3 facing=transform.TransformPoint(ropeCenter)-Cat.transform.position;facing.y=0;
        if(facing.sqrMagnitude>.001f)yield return Face(facing);
        Vector3 surface=transform.TransformPoint(ropeCenter)-Cat.transform.forward*ropeRadius;
        float spread=.045f;
        if(HomeStoreService.IsFixedRoomProduct(StoreProductId))
        {
            var hits=Physics.RaycastAll(Cat.transform.position+Vector3.up*.43f,Cat.transform.forward,1.8f,~0,QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)if(hit.transform.IsChildOf(transform)){surface=hit.point+hit.normal*.005f;spread=.025f;break;}
        }
        surface.y=0;
        float time=0;
        int lastLeft=-1,lastRight=-1;
        while(time<scratchDuration)
        {
            time+=Time.deltaTime;
            float cycle=time/.65f;
            poses.SetTimedPose(CatActivityPose.Scratch,Mathf.Repeat(cycle,1));
            // Contact follows the near side of the rope, not the hanging ball.
            Vector3 center=surface;
            float leftPhase=Mathf.Repeat(cycle,1),rightPhase=Mathf.Repeat(cycle+.5f,1);
            Vector3 left=center-Cat.transform.right*spread+Vector3.up*Mathf.Lerp(.48f,.34f,leftPhase);
            Vector3 right=center+Cat.transform.right*spread+Vector3.up*Mathf.Lerp(.48f,.34f,rightPhase);
            left=MeasuredDoorContact(left);right=MeasuredDoorContact(right);
            contact.ReachBoth(left,right,Mathf.Clamp01(time/.20f));
            if(leftPhase>.4f && contact.LeftDistance<.075f && lastLeft!=(int)cycle){LeftStrokes++;lastLeft=(int)cycle;}
            if(rightPhase>.4f && contact.RightDistance<.075f && lastRight!=(int)(cycle+.5f)){RightStrokes++;lastRight=(int)(cycle+.5f);}
            if(hangingToy!=null)hangingToy.localRotation=toyRotation*Quaternion.Euler(0,0,Mathf.Sin(time*10)*9);
            yield return null;
        }
        contact.Clear();
        if(CatActivityMotion.TryFloorPath(Cat.transform.position,RoutineFloorPosition,out var exit))
            foreach(var point in exit)yield return Walk(point);
        Restore(false);CompleteActivity("CLAWS FEEL GREAT!");
    }
    Vector3 MeasuredDoorContact(Vector3 target)
    {
        if(measuredSurfaces==null)return target;
        // Door trim varies with stroke height. A single mid-height plane put
        // the lower hand inside the moulding; measure each real near face.
        Vector3 origin=Cat.transform.position;origin.y=target.y;
        origin+=Cat.transform.right*Vector3.Dot(target-origin,Cat.transform.right);
        var ray=new Ray(origin,Cat.transform.forward);float nearest=1.8f;
        foreach(var mesh in measuredSurfaces)
        {
            RaycastHit hit;
            if(mesh.enabled&&!mesh.isTrigger&&mesh.Raycast(ray,out hit,nearest))
            {nearest=hit.distance;target=hit.point+hit.normal*.018f;}
        }
        return target;
    }
    IEnumerator Walk(Vector3 point)
    {
        if(StoreProductId==HomeStoreService.ScratchPostId && Vector3.Distance(Cat.transform.position,point)<=.005f)yield break;
        Vector3 direction=point-Cat.transform.position;direction.y=0;
        if(direction.sqrMagnitude>.001f)yield return Face(direction);
        PlayCatPose(CatActivityPose.Walk);
        while(Vector3.Distance(Cat.transform.position,point)>.005f)
        {Cat.transform.position=Vector3.MoveTowards(Cat.transform.position,point,1.5f*Time.deltaTime);yield return null;}
        Cat.transform.position=point;
    }
    IEnumerator Face(Vector3 direction)
    {
        var target=Quaternion.LookRotation(direction);
        if(StoreProductId==HomeStoreService.ScratchPostId)
        {
            float angle=Quaternion.Angle(Cat.transform.rotation,target);
            if(angle>1f)
            {
                poses.SetTimedPose(CatActivityPose.Sniff,0);
                yield return CatActivityFacing.Turn(Cat,target,Mathf.Max(.08f,angle/540f));
            }
        }
        Cat.transform.rotation=target;
    }
    void Restore(bool cancel)
    {
        if(!captured)return;captured=false;
        if(contact!=null)contact.Clear();
        if(hangingToy!=null)hangingToy.localRotation=toyRotation;
        if(Cat!=null){if(cancel){Cat.transform.position=start;Cat.transform.rotation=rotation;}Cat.SetMovementLocked(this,false);}
        if(controller!=null)controller.enabled=wasEnabled;
    }
    protected override void CancelActivity(){if(!IsRunning)return;StopAllCoroutines();if(!HasBegunActivity){base.CancelActivity();return;}Restore(true);base.CancelActivity();}
#if UNITY_EDITOR
    public void EditorConfigureScratch(Transform point,float duration){scratchPoint=point;scratchDuration=Mathf.Max(.5f,duration);}
    public void EditorConfigureToy(Transform toy){hangingToy=toy;}
    public void EditorConfigureRope(Vector3 center,float radius){ropeCenter=center;ropeRadius=radius;}
#endif
}
