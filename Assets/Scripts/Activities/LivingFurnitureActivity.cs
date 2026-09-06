using System.Collections;
using UnityEngine;

/// <summary>Jump onto the permanent sofa/table, interact, and land on the reserved entry.</summary>
[DisallowMultipleComponent]
public sealed class LivingFurnitureActivity : CatActivity
{
    [SerializeField] Transform perch;
    [SerializeField] Transform toy;
    [SerializeField] Vector3 toyEdge, toyLanding;
    [SerializeField] bool table;
    CatActivityAnimation pose;
    CatToyContactMotion contact;
    CharacterController controller;
    Vector3 toyStart;
    Quaternion toyRotation;
    public bool IsResting {get;private set;}
    public bool DidPush {get;private set;}
    public Transform Perch=>perch;
    public Transform Toy=>toy;
    public float ContactDistance {get;private set;}
    protected override bool UsesFloorApproach=>true;
    public override string ProgressLabel=>!IsRunning?string.Empty:GameLanguageService.Current==GameLanguage.Turkish?
        (table?"Acaba düşer mi?":"Koltuk keyfi"):(table?"Will it fall?":"Sofa time");
    protected override bool CanBeginActivity(out string reason)
    {reason="Burada zıplayacak yer yok.";if(perch==null||RoutineEntryPoint==null)return false;reason=string.Empty;return true;}
    protected override bool BeginActivity()
    {
        pose=Cat.GetComponent<CatActivityAnimation>();
        contact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        controller=Cat.GetComponent<CharacterController>();
        Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        if(toy!=null){toyStart=toy.position;toyRotation=toy.rotation;}
        DidPush=false;ContactDistance=float.PositiveInfinity;
        StartCoroutine(Routine());return true;
    }
    IEnumerator Routine()
    {
        Vector3 floor=RoutineEntryPoint.position;floor.y=0;
        yield return Hop(floor,perch.position);
        Cat.transform.rotation=perch.rotation;
        if(table && toy!=null)
        {
            PlayCatPose(CatActivityPose.Sniff,perch);yield return new WaitForSeconds(.5f);
            // First investigate, then give the loose toy a deliberate nudge.
            for(int attempt=0;attempt<2 && !DidPush;attempt++)
            {
                contact.Clear();float t=0;
                while(t<1f)
                {
                    pose.SetTimedPose(attempt==0?CatActivityPose.BatLeft:CatActivityPose.Push,t,perch);
                    contact.Reach(toy.position,true,t);
                    if(t>=.34f && t<.75f && contact.Distance<.10f)
                    {
                        ContactDistance=contact.Distance;DidPush=true;contact.Clear();
                        yield return PushToy();break;
                    }
                    t+=Time.deltaTime;yield return null;
                }
            }
            contact.Clear();PlayCatPose(CatActivityPose.Sit,perch);yield return new WaitForSeconds(.8f);
        }
        else
        {
            PlayCatPose(CatActivityPose.Sleep,perch);IsResting=true;
            yield return new WaitForSeconds(3.2f);IsResting=false;
        }
        yield return Hop(perch.position,floor);
        Release();CompleteActivity(GameLanguageService.Current==GameLanguage.Turkish?(table?"Ben bir şey yapmadım!":"Ne güzel bir köşe."):(table?"It wasn't me!":"Such a cosy spot."));
        // Reset only after the cat is back on the floor, ready for the next visit.
        ResetToy();
    }
    IEnumerator PushToy()
    {
        PlayCatPose(CatActivityPose.Sniff,perch);float t=0;
        Vector3 edge=transform.TransformPoint(toyEdge),landing=transform.TransformPoint(toyLanding);
        Vector3 start=toy.position;
        while(t<.45f)
        {
            t+=Time.deltaTime;toy.position=Vector3.Lerp(start,edge,Mathf.Clamp01(t/.45f));
            toy.Rotate(Vector3.forward,-240*Time.deltaTime,Space.World);yield return null;
        }
        t=0;
        while(t<.38f)
        {
            t+=Time.deltaTime;float p=Mathf.Clamp01(t/.38f);
            Vector3 at=Vector3.Lerp(edge,landing,p);at.y=Mathf.Lerp(edge.y,landing.y,p*p);
            toy.position=at;toy.Rotate(Vector3.forward,-360*Time.deltaTime,Space.World);yield return null;
        }
        t=0;
        while(t<.4f){t+=Time.deltaTime;toy.position=landing+Vector3.up*(Mathf.Abs(Mathf.Sin(t/.4f*Mathf.PI*2))*.05f*(1-t/.4f));yield return null;}
        toy.position=landing;
    }
    IEnumerator Hop(Vector3 from,Vector3 to)
    {
        PlayCatPose(CatActivityPose.Hop);Vector3 direction=to-from;direction.y=0;
        if(direction.sqrMagnitude>.001f)Cat.transform.rotation=Quaternion.LookRotation(direction);
        float t=0;
        while(t<.55f){t+=Time.deltaTime;Cat.transform.position=CatActivityMotion.JumpPosition(from,to,Mathf.Clamp01(t/.55f),.28f);yield return null;}
        Cat.transform.position=to;
    }
    void ResetToy(){if(toy!=null && toyStart.sqrMagnitude>.001f)toy.SetPositionAndRotation(toyStart,toyRotation);}
    void Release(){IsResting=false;if(contact!=null)contact.Clear();if(controller!=null)controller.enabled=true;if(Cat!=null)Cat.SetMovementLocked(this,false);}
    protected override void CancelActivity(){StopAllCoroutines();Release();ResetToy();base.CancelActivity();}
    protected override void OnDisable(){StopAllCoroutines();Release();ResetToy();base.OnDisable();}
#if UNITY_EDITOR
    public void EditorConfigureFurniture(Transform support,bool isTable,Transform prop,Vector3 edge,Vector3 landing)
    {perch=support;table=isTable;toy=prop;toyEdge=transform.InverseTransformPoint(edge);toyLanding=transform.InverseTransformPoint(landing);}
#endif
}
