using UnityEngine;

[DefaultExecutionOrder(575)]
public sealed class LivingTunnelCrouch : MonoBehaviour
{
    Transform neck;Quaternion source;bool applied;
    float target,current;
    public void Set(float pitch){target=Mathf.Clamp(pitch,0,32);}
    public void Clear(){Restore();target=current=0;}
    void Update(){Restore();}
    void LateUpdate()
    {
        if(neck==null||!neck.IsChildOf(transform))neck=CatBreedVisualFactory.FindDescendant(transform,"DEF-spine.004");
        if(neck==null)return;
        current=Mathf.MoveTowards(current,target,Time.deltaTime*90f);
        if(current<=0)return;
        source=neck.localRotation;applied=true;
        neck.rotation=Quaternion.AngleAxis(current,transform.right)*neck.rotation;
    }
    void Restore(){if(applied&&neck!=null)neck.localRotation=source;applied=false;}
    void OnDisable(){Clear();}
}
