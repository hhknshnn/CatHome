using System.Collections.Generic;
using UnityEngine;

/// <summary>Small, reversible reach correction after the breed animation and floor alignment.</summary>
[DefaultExecutionOrder(600)]
public sealed class CatToyContactMotion : MonoBehaviour
{
    Transform arm, forearm, hand;
    Quaternion armPose, forearmPose;
    bool adjusted, requested;
    Vector3 target;
    float weight;
    public float Distance { get; private set; } = float.PositiveInfinity;
    public void Reach(Vector3 point, bool left, float phase)
    {
        string side=left?"L":"R";
        if(arm==null || arm.name!="DEF-upper_arm."+side)
        {
            Clear();
            foreach(var t in GetComponentsInChildren<Transform>())
            {
                if(t.name=="DEF-upper_arm."+side)arm=t;
                if(t.name=="DEF-forearm."+side)forearm=t;
                if(t.name=="DEF-hand."+side)hand=t;
            }
        }
        target=point; requested=true;
        weight=Mathf.SmoothStep(0,1,phase<.42f?phase/.42f:(1-phase)/.58f);
    }
    void Update(){Restore();}
    void LateUpdate()
    {
        if(!requested || arm==null || forearm==null || hand==null) return;
        requested=false;armPose=arm.localRotation;forearmPose=forearm.localRotation;adjusted=true;
        Vector3 goal=Vector3.Lerp(hand.position,target,weight);
        // CCD rotates the real joints, preserving segment lengths and breed scale.
        for(int i=0;i<8;i++)
        {
            Aim(forearm,goal); Aim(arm,goal);
        }
        Distance=Vector3.Distance(hand.position,target);
    }
    void Aim(Transform joint,Vector3 goal)
    {
        Vector3 from=hand.position-joint.position, to=goal-joint.position;
        if(from.sqrMagnitude<.00001f||to.sqrMagnitude<.00001f)return;
        joint.rotation=Quaternion.RotateTowards(Quaternion.identity,Quaternion.FromToRotation(from,to),20)*joint.rotation;
    }
    void Restore()
    {
        if(!adjusted)return;adjusted=false;
        if(arm!=null)arm.localRotation=armPose;
        if(forearm!=null)forearm.localRotation=forearmPose;
    }
    public void Clear(){Restore();requested=false;arm=null;forearm=null;hand=null;Distance=float.PositiveInfinity;}
    void OnDisable(){Clear();}
}
