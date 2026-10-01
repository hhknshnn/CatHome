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
    CatSupportedFurnitureMotion supportedMotion;
    Vector3 toyStart;
    Quaternion toyRotation;
    public bool IsResting {get;private set;}
    public bool DidPush {get;private set;}
    public Transform Perch=>perch;
    public Transform Toy=>toy;
    public float ContactDistance {get;private set;}
    protected override bool UsesFloorApproach=>false;
    protected override bool UsesPreparedStart=>true;
    protected override string ApproachHint => table ? base.ApproachHint : GameContentCopy.Text(
        "Koltuğun önüne yaklaşıp ona dönelim.", "Move to the front of the sofa and face it.");
    public override bool TryGetPromptDistance(CatMovement actor,out float distance)
    {
        return base.TryGetPromptDistance(actor,out distance);
    }
    protected override bool TryPrepareStart(CatMovement actor,out CatActivityStart start)
    {
        start=default;
        return perch!=null && RoutineEntryPoint!=null && actor!=null &&
            CatActivityStartResolver.GroundLaunch(this,actor,RoutineEntryPoint.position,LandingPosition(actor,out _),out start);
    }
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
        Vector3 floor=RoutineEntryPoint.position;floor.y=AcceptedStart.Position.y;
        // The table's authored heading places the paw at the loose toy. Only
        // the free rest chooses a viewing direction before the held pose.
        Vector3 seat=LandingPosition(Cat,out Quaternion held);
        supportedMotion = new CatSupportedFurnitureMotion(this, Cat, perch);
        yield return supportedMotion.Jump(AcceptedStart.Position, seat, AcceptedStart.Rotation, held);
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
            contact.Clear();
            yield return supportedMotion.Pose(CatActivityPose.SitDown, .55f, perch.position, Cat.transform.rotation);
            PlayCatPose(CatActivityPose.Sit,perch);
            yield return new WaitForSeconds(.8f);
        }
        else
        {
            yield return supportedMotion.Pose(CatActivityPose.SitDown, .55f, seat, held);
            yield return supportedMotion.Pose(CatActivityPose.TowelSettle, .90f, seat, held);
            supportedMotion.Rest(CatActivityPose.Sleep);IsResting=true;
            while(KeepResting)yield return null;IsResting=false;
            yield return supportedMotion.Pose(CatActivityPose.TowelWake, .80f, seat, held);
        }
        yield return supportedMotion.Pose(CatActivityPose.StandUp, .55f, seat, Cat.transform.rotation);
        yield return supportedMotion.Jump(seat, floor, Cat.transform.rotation,
            Quaternion.LookRotation(Vector3.ProjectOnPlane(floor-seat,Vector3.up)));
        Release();CompleteActivity(GameLanguageService.Current==GameLanguage.Turkish?(table?"Ben bir şey yapmadım!":"Ne güzel bir köşe."):(table?"It wasn't me!":"Such a cosy spot."));
        // Reset only after the cat is back on the floor, ready for the next visit.
        ResetToy();
    }
    IEnumerator PushToy()
    {
        GameAudio.Play(AudioCue.BallTap,.7f);
        GameAudio.Play(AudioCue.BallRoll,.5f);
        PlayCatPose(CatActivityPose.Sniff,perch);float t=0;
        Vector3 edge=transform.TransformPoint(toyEdge),landing=transform.TransformPoint(toyLanding);
        Vector3 start=toy.position;
        while(t<.45f)
        {
            if(Time.timeScale<=0f){yield return null;continue;}
            t+=Time.deltaTime;toy.position=Vector3.Lerp(start,edge,Mathf.Clamp01(t/.45f));
            toy.Rotate(Vector3.forward,-240*Time.deltaTime,Space.World);yield return null;
        }
        t=0;
        while(t<.38f)
        {
            if(Time.timeScale<=0f){yield return null;continue;}
            t+=Time.deltaTime;float p=Mathf.Clamp01(t/.38f);
            Vector3 at=Vector3.Lerp(edge,landing,p);at.y=Mathf.Lerp(edge.y,landing.y,p*p);
            toy.position=at;toy.Rotate(Vector3.forward,-360*Time.deltaTime,Space.World);
            // The solid object's first floor contact is distinct from the
            // light paw tap and rolling sound on the tabletop.
            if(p>=1f)GameAudio.Play(AudioCue.PropLand,1f);
            yield return null;
        }
        t=0;
        while(t<.4f){if(Time.timeScale<=0f){yield return null;continue;}t+=Time.deltaTime;toy.position=landing+Vector3.up*(Mathf.Abs(Mathf.Sin(t/.4f*Mathf.PI*2))*.05f*(1-t/.4f));yield return null;}
        toy.position=landing;
    }
    Vector3 LandingPosition(CatMovement actor,out Quaternion held)
    {
        held=table?perch.rotation:HeldFacing(actor,perch.rotation);
        Vector3 seat=perch.position;
        if(!table)
        {
            // The seat remains authored independently of the accepted launch
            // stance, preserving the backrest and armrest clearance.
            seat+=Vector3.ProjectOnPlane(RoutineEntryPoint.position-seat,Vector3.up).normalized*.10f;
            seat+=held*Vector3.forward*.08f;
        }
        return seat;
    }
    Quaternion HeldFacing(CatMovement actor,Quaternion preferred)
    {
        var surface=perch.GetComponent<CatActivitySurface>();
        return surface!=null && surface.AlignAlongSurface?
            CatActivityFacing.AlongAxis(actor,perch.position,perch.rotation * Quaternion.Euler(0,90,0)):
            CatActivityFacing.Resolve(actor,perch.position,preferred);
    }
    IEnumerator Hop(Vector3 from,Vector3 to)
    {
        Vector3 flat=to-from;flat.y=0;
        var facing=flat.sqrMagnitude>.001f?Quaternion.LookRotation(flat):Cat.transform.rotation;
        yield return CatActivityMotion.Jump(Cat,from,to,Cat.transform.rotation,facing,.18f);
    }
    void ResetToy(){if(toy!=null && toyStart.sqrMagnitude>.001f)toy.SetPositionAndRotation(toyStart,toyRotation);}
    void Release(){supportedMotion?.End();supportedMotion=null;IsResting=false;if(contact!=null)contact.Clear();if(controller!=null)controller.enabled=true;if(Cat!=null)Cat.SetMovementLocked(this,false);}
    protected override void CancelActivity(){if(!IsRunning)return;StopAllCoroutines();if(!HasBegunActivity){base.CancelActivity();return;}Release();ResetToy();base.CancelActivity();}
#if UNITY_EDITOR
    public void EditorConfigureFurniture(Transform support,bool isTable,Transform prop,Vector3 edge,Vector3 landing)
    {perch=support;table=isTable;toy=prop;toyEdge=transform.InverseTransformPoint(edge);toyLanding=transform.InverseTransformPoint(landing);}
#endif
}
