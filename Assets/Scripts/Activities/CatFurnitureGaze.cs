using UnityEngine;

/// <summary>Bounded head tracking during a locked furniture routine; never rotates CatRoot.</summary>
[DefaultExecutionOrder(650)]
public sealed class CatFurnitureGaze : MonoBehaviour
{
    public const float DefaultYawLimit = 16f;
    public const float SeatedYawLimit = 40f;
    Transform head; Quaternion original; bool adjusted, requested; Vector3 target; float weight, yawLimit = DefaultYawLimit;
    public float Deflection { get; private set; }
    public void LookAt(Vector3 point,float blend,float maximumYaw = DefaultYawLimit)
    {
        target=point;weight=Mathf.Clamp01(blend);yawLimit=Mathf.Clamp(maximumYaw,0f,SeatedYawLimit);requested=true;
        if(head==null)foreach(var bone in GetComponentsInChildren<Transform>())
            if(bone.name=="DEF-spine.006"){head=bone;break;}
    }
    void Update(){Restore();}
    void LateUpdate()
    {
        if(!requested || head==null)return; requested=false;
        Vector3 direction=target-head.position;
        if(direction.sqrMagnitude<.0001f)return;
        float yaw=Mathf.Clamp(Vector3.SignedAngle(transform.forward,Vector3.ProjectOnPlane(direction,Vector3.up),Vector3.up),-yawLimit,yawLimit);
        float pitch=Mathf.Clamp(Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg,-12,26);
        original=head.localRotation;adjusted=true;
        Quaternion offset=Quaternion.AngleAxis(yaw*weight,Vector3.up)*Quaternion.AngleAxis(-pitch*weight,transform.right);
        head.rotation=offset*head.rotation;Deflection=Quaternion.Angle(Quaternion.identity,offset);
    }
    void Restore(){if(adjusted && head!=null)head.localRotation=original;adjusted=false;}
    public void Clear(){Restore();requested=false;head=null;Deflection=0;}
    void OnDisable(){Clear();}
}
