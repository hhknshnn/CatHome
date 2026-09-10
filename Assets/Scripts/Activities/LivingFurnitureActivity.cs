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
    [SerializeField] Transform selectionVisual;
    public Transform SelectionVisual=>selectionVisual;
#if UNITY_EDITOR
    public void EditorConfigureSelection(Transform visual){selectionVisual=visual;}
#endif
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
    public override bool SupportsContinuousRest=>!table;
    public override float EnergyCost=>table?base.EnergyCost:0f;
    public override string ProgressLabel=>!IsRunning?string.Empty:GameLanguageService.Current==GameLanguage.Turkish?
        (table?"Acaba düşer mi?":"Koltuk keyfi"):(table?"Will it fall?":"Sofa time");
    protected override bool CanBeginActivity(out string reason)
    {reason="Burada zıplayacak yer yok.";if(perch==null||RoutineEntryPoint==null)return false;reason=string.Empty;return true;}
    protected override bool BeginActivity()
    {
        pose=Cat.GetComponent<CatActivityAnimation>();
        contact=Cat.GetComponent<CatToyContactMotion>();if(contact==null)contact=Cat.gameObject.AddComponent<CatToyContactMotion>();
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
        // The table's authored heading places the paw at the loose toy. Only
        // the free rest chooses a viewing direction before the held pose.
        Quaternion held = perch.rotation;
        if(!table)
        {
            held=HeldFacing(perch.rotation);
        }
        yield return CatActivityFacing.Turn(Cat,held);
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
            contact.Clear();PlayCatPose(CatActivityPose.Sit,perch);
            yield return CatActivityFacing.Turn(Cat,HeldFacing(Cat.transform.rotation));
            yield return new WaitForSeconds(.8f);
        }
        else
        {
            PlayCatPose(CatActivityPose.Sleep,perch);IsResting=true;
            while(KeepResting)yield return null;IsResting=false;
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
    Quaternion HeldFacing(Quaternion preferred)
    {
        var surface=perch.GetComponent<CatActivitySurface>();
        return surface!=null && surface.AlignAlongSurface?
            CatActivityFacing.AlongAxis(Cat,perch.position,perch.rotation):
            CatActivityFacing.Resolve(Cat,perch.position,preferred);
    }
    IEnumerator Hop(Vector3 from,Vector3 to)
    {
        Vector3 flat=to-from;flat.y=0;
        var facing=flat.sqrMagnitude>.001f?Quaternion.LookRotation(flat):Cat.transform.rotation;
        yield return CatActivityMotion.Jump(Cat,from,to,Cat.transform.rotation,facing,.18f);
    }
    void ResetToy(){if(toy!=null && toyStart.sqrMagnitude>.001f)toy.SetPositionAndRotation(toyStart,toyRotation);}
    void Release(){IsResting=false;if(contact!=null)contact.Clear();if(controller!=null)controller.enabled=true;if(Cat!=null)Cat.SetMovementLocked(this,false);}
    protected override void CancelActivity(){if(!IsRunning)return;StopAllCoroutines();if(!HasBegunActivity){base.CancelActivity();return;}Release();ResetToy();base.CancelActivity();}
#if UNITY_EDITOR
    public void EditorConfigureFurniture(Transform support,bool isTable,Transform prop,Vector3 edge,Vector3 landing)
    {perch=support;table=isTable;toy=prop;toyEdge=transform.InverseTransformPoint(edge);toyLanding=transform.InverseTransformPoint(landing);}
#endif
}
