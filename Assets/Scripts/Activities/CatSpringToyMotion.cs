using UnityEngine;

// Spring-only overlay on the existing sampled ToyBat clips. No root translation,
// new skeleton, or persistent bone edits; the opposite paw stays at its support.
[DefaultExecutionOrder(575)]
public sealed class CatSpringToyMotion : MonoBehaviour
{
    Transform chest, shoulder, supportHand, hand;
    Quaternion chestPose, shoulderPose, handPose;
    bool adjusted, requested, left;
    Vector3 target, supportPoint;
    float phase, blend;
    CatToyContactMotion contact;
    CatFurnitureGaze gaze;
    CatSpringGeometry geometry;
    CatPawSurfacePlan surface;
    Vector3[] sampledPositions;
    Vector3 sampledVisualPosition;
    float contactLean;
    public float ContactTolerance=>geometry!=null?geometry.Paw*.08f:0;
    public void Begin(bool useLeft,CatMeshContactSurface.Hit hit)
    {
        Clear();left=useLeft;
        foreach(var t in GetComponentsInChildren<Transform>())
        {
            if(t.name=="DEF-spine.003")chest=t;
            if(t.name=="DEF-shoulder."+(left?"L":"R"))shoulder=t;
            if(t.name=="DEF-hand."+(left?"R":"L"))supportHand=t;
            if(t.name=="DEF-hand."+(left?"L":"R"))hand=t;
        }
        if(supportHand!=null)supportPoint=supportHand.position;
        contact=GetComponent<CatToyContactMotion>();
        geometry=CatSpringGeometry.Measure(GetComponent<CatMovement>());
        sampledPositions=new Vector3[geometry.Bones.Length];
        supportPoint=transform.TransformPoint(left?geometry.RightHandLocal:geometry.LeftHandLocal);
        surface=geometry?.PawPlan(left,hit);
        gaze=GetComponent<CatFurnitureGaze>()??gameObject.AddComponent<CatFurnitureGaze>();
    }
    public void Sample(Vector3 point,float p,float amount)
    {target=point;phase=p;blend=amount;requested=true;if(p<.1f||amount<=0)contactLean=0;}
    void Update(){Restore();}
    void LateUpdate()
    {
        if(!requested||chest==null||hand==null)return;requested=false;
        chestPose=chest.localRotation;
        handPose=hand.localRotation;
        if(shoulder!=null)shoulderPose=shoulder.localRotation;
        adjusted=true;
        // The shared clips contain small-source translations. Keep this live rig's
        // native translations, while retaining every sampled clip rotation.
        sampledVisualPosition=geometry.Visual.position;
        for(int i=0;i<geometry.Bones.Length;i++){sampledPositions[i]=geometry.Bones[i].localPosition;geometry.Bones[i].localPosition=geometry.BonePositions[i];}
        geometry.Visual.position+=Vector3.up*(transform.TransformPoint(new Vector3(0,geometry.RearHeightLocal,0)).y-geometry.Rear.position.y);
        // Finish a near-full extension with a small measured shoulder transfer,
        // not a longer limb or a wrist offset. Only residual real-skin error drives it.
        float error=left?contact.LeftDistance:contact.RightDistance;
        if(phase>=.49f&&phase<.59f&&!float.IsInfinity(error))
            contactLean=Mathf.Min(geometry.Reach*.10f,contactLean+Mathf.Min(error,geometry.Reach*.04f));
        chest.position+=transform.forward*(contactLean*blend);
        // Chest rolls toward the supporting foreleg, with a small rearward lean.
        chest.rotation=Quaternion.AngleAxis((left?3.5f:-3.5f)*blend,transform.forward)*
            Quaternion.AngleAxis(2.5f*blend,transform.right)*chest.rotation;
        if(shoulder!=null)shoulder.rotation=Quaternion.AngleAxis(-5f*blend,transform.right)*shoulder.rotation;
        hand.rotation=Quaternion.AngleAxis(12f*blend,transform.right)*hand.rotation;
        gaze.LookAt(target,.85f,28f);
        if(blend<=0)return;
        // Lift and fold first, then extend to the near face; return on a lower arc.
        float reach=phase<.45f?Mathf.SmoothStep(0,1,phase/.45f):
            phase<.56f?1:Mathf.SmoothStep(1,0,(phase-.56f)/.44f);
        if(surface!=null)contact.ReachSurface(surface,reach);
        if(supportHand!=null)contact.ReachWeighted(supportPoint,!left,blend);
    }
    void Restore()
    {
        if(!adjusted)return;adjusted=false;
        if(chest!=null)chest.localRotation=chestPose;
        if(shoulder!=null)shoulder.localRotation=shoulderPose;
        if(hand!=null)hand.localRotation=handPose;
        if(geometry!=null&&sampledPositions!=null){for(int i=0;i<geometry.Bones.Length;i++)if(geometry.Bones[i]!=null)geometry.Bones[i].localPosition=sampledPositions[i];if(geometry.Visual!=null)geometry.Visual.position=sampledVisualPosition;}
    }
    public void Clear(){Restore();requested=false;gaze?.Clear();contact?.Clear();}
    void OnDisable(){Clear();}
}
