using UnityEngine;

/// <summary>Reversible two-arm contact correction, preserving every breed's bone lengths.</summary>
[DefaultExecutionOrder(600)]
public sealed class CatToyContactMotion : MonoBehaviour
{
    sealed class Limb
    {
        public Transform arm, fore, hand;
        public Quaternion armPose, forePose;
        public bool adjusted,requested;
        public Vector3 target;
        public float weight;
        public int iterations=8;
        public float distance=float.PositiveInfinity;
    }
    readonly Limb[] limbs={new Limb(),new Limb()};
    public Vector3 LastContactPosition {get;private set;}
    public float Distance {get;private set;}=float.PositiveInfinity;
    public float LeftDistance=>limbs[0].distance;
    public float RightDistance=>limbs[1].distance;
    public void Reach(Vector3 point,bool left,float phase,int iterations=8)
    {
        Set(left?0:1,point,Mathf.SmoothStep(0,1,phase<.42f?phase/.42f:(1-phase)/.58f),iterations);
    }
    public void ReachBoth(Vector3 left,Vector3 right,float weight=1f,int iterations=8)
    {Set(0,left,weight,iterations);Set(1,right,weight,iterations);}
    void Set(int index,Vector3 point,float weight,int iterations=8)
    {
        var limb=limbs[index];string side=index==0?"L":"R";
        if(limb.arm==null)
            foreach(var t in GetComponentsInChildren<Transform>())
            {
                if(t.name=="DEF-upper_arm."+side)limb.arm=t;
                if(t.name=="DEF-forearm."+side)limb.fore=t;
                if(t.name=="DEF-hand."+side)limb.hand=t;
            }
        limb.target=point;limb.weight=Mathf.Clamp01(weight);limb.iterations=Mathf.Clamp(iterations,1,32);limb.requested=true;
    }
    void Update(){Restore();}
    void LateUpdate()
    {
        foreach(var limb in limbs)
        {
            if(!limb.requested || limb.arm==null || limb.fore==null || limb.hand==null)continue;
            limb.requested=false;limb.armPose=limb.arm.localRotation;limb.forePose=limb.fore.localRotation;limb.adjusted=true;
            Vector3 goal=Vector3.Lerp(limb.hand.position,limb.target,limb.weight);
            for(int i=0;i<limb.iterations;i++){Aim(limb.fore,limb.hand,goal);Aim(limb.arm,limb.hand,goal);}
            limb.distance=Vector3.Distance(limb.hand.position,limb.target);Distance=limb.distance;LastContactPosition=limb.hand.position;
        }
    }
    static void Aim(Transform joint,Transform hand,Vector3 goal)
    {
        Vector3 from=hand.position-joint.position,to=goal-joint.position;
        if(from.sqrMagnitude<.00001f || to.sqrMagnitude<.00001f)return;
        joint.rotation=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(from,to),20)*joint.rotation;
    }
    void Restore()
    {
        foreach(var limb in limbs)
        {
            if(!limb.adjusted)continue;limb.adjusted=false;
            if(limb.arm!=null)limb.arm.localRotation=limb.armPose;
            if(limb.fore!=null)limb.fore.localRotation=limb.forePose;
        }
    }
    public void Clear()
    {
        Restore();foreach(var limb in limbs){limb.requested=false;limb.arm=null;limb.fore=null;limb.hand=null;limb.distance=float.PositiveInfinity;}
        Distance=float.PositiveInfinity;
    }
    void OnDisable(){Clear();}
}
