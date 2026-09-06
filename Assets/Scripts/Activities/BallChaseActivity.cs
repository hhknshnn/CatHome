using System.Collections;
using UnityEngine;

/// <summary>A paw contact starts every roll. No proximity catch or autonomous throw.</summary>
[DisallowMultipleComponent]
public sealed class BallChaseActivity : CatActivity
{
    [SerializeField] Transform ball;
    [SerializeField] Transform[] landingPoints=new Transform[0];
    [SerializeField,Min(1)] int catchesToComplete=3;
    CatToyContactMotion contact;
    CatActivityAnimation pose;
    CharacterController controller;
    int catches;
    bool hit;
    Vector3 playDirection;
    public int CatchCount=>catches;
    public int CatchGoal=>Mathf.Max(1,catchesToComplete);
    public float LastHitDistance {get;private set;}
    public float RolledDistance {get;private set;}
    public string InterruptedReason {get;private set;}
    public Transform Ball=>ball;
    protected override bool UsesFloorApproach=>true;
    public override string ProgressLabel=>IsRunning?(GameLanguageService.Current==GameLanguage.Turkish?
        $"Pati ve takip · {catches}/{CatchGoal}":$"Bat and chase · {catches}/{CatchGoal}"):string.Empty;
    protected override void Awake(){base.Awake();if(ball!=null)ball.gameObject.SetActive(false);}
    protected override bool CanBeginActivity(out string reason)
    {
        reason="Top için biraz açık alan gerekiyor.";
        if(ball==null||RoutineEntryPoint==null)return false;
        Vector3 entry=RoutineEntryPoint.position;entry.y=0;
        Vector3 outward=entry-transform.position;outward.y=0;
        if(!ChooseDirection(entry,outward.normalized,1.30f,out playDirection))return false;
        reason=string.Empty;return true;
    }
    protected override bool BeginActivity()
    {
        controller=Cat.GetComponent<CharacterController>();
        contact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        pose=Cat.GetComponent<CatActivityAnimation>();
        catches=0;RolledDistance=0;LastHitDistance=float.PositiveInfinity;InterruptedReason=null;
        Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        StartCoroutine(PlayRoutine());return true;
    }
    static bool ChooseDirection(Vector3 origin,Vector3 preferred,float distance,out Vector3 direction)
    {
        foreach(float angle in new[]{0f,45f,-45f,90f,-90f,135f,-135f,180f})
        {
            direction=Quaternion.Euler(0,angle,0)*(preferred.sqrMagnitude>.01f?preferred:Vector3.forward);
            if(CatActivityMotion.ClearSegment(origin,origin+direction*distance))return true;
        }
        direction=Vector3.zero;return false;
    }
    IEnumerator PlayRoutine()
    {
        Cat.transform.rotation=Quaternion.LookRotation(playDirection);
        ball.position=Cat.transform.position+playDirection*.30f+Vector3.up*.09f;
        ball.gameObject.SetActive(true);
        PlayCatPose(CatActivityPose.Sniff);yield return new WaitForSeconds(.45f);
        for(int beat=0;beat<CatchGoal;beat++)
        {
            Vector3 floor=ball.position;floor.y=0;
            Vector3 direction=playDirection;
            Vector3 stand=floor-direction*.30f;
            bool found=false;
            foreach(float angle in new[]{0f,45f,-45f,90f,-90f,135f,-135f,180f})
            {
                direction=Quaternion.Euler(0,angle,0)*playDirection;
                stand=floor-direction*.30f;
                // Leave room BEYOND the stopping point. Otherwise a ball at the
                // wall cannot be approached from its other side for the next bat.
                if(CatActivityMotion.ClearSegment(stand,floor+direction*.99f) &&
                   CatActivityMotion.TryFloorPath(Cat.transform.position,stand,out _)){found=true;break;}
            }
            if(!found){InterruptedReason="No clear roll from "+floor;CancelActivity();yield break;}
            yield return WalkTo(stand);
            Cat.transform.rotation=Quaternion.LookRotation(direction);
            hit=false;contact.Clear();float elapsed=0;
            while(elapsed<.82f)
            {
                float phase=Mathf.Clamp01(elapsed/.82f);
                pose.SetTimedPose(beat%2==0?CatActivityPose.BatLeft:CatActivityPose.BatRight,phase);
                contact.Reach(ball.position,beat%2==0,phase);
                if(!hit && phase>=.34f && phase<.75f && contact.Distance<.095f)
                {
                    hit=true;LastHitDistance=contact.Distance;catches++;NotifyChanged();
                    contact.Clear();
                    yield return Roll(floor+Vector3.up*.09f,direction);
                    break;
                }
                elapsed+=Time.deltaTime;yield return null;
            }
            contact.Clear();
            if(!hit){InterruptedReason="No paw contact at "+ball.position+" from "+Cat.transform.position;CancelActivity();yield break;}
            playDirection=direction;
            yield return WalkTo(new Vector3(ball.position.x,0,ball.position.z)-direction*.30f);
            PlayCatPose(beat==CatchGoal-1?CatActivityPose.Sit:CatActivityPose.Stalk);
            yield return new WaitForSeconds(beat==CatchGoal-1?.65f:.22f);
        }
        Release();CompleteActivity(GameLanguageService.Current==GameLanguage.Turkish?"Bir pati daha mı?":"One more paw?");
    }
    IEnumerator Roll(Vector3 start,Vector3 direction)
    {
        float elapsed=0,previous=0;
        PlayCatPose(CatActivityPose.Stalk);
        while(elapsed<.65f)
        {
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/.65f);
            float distance=.65f*(1-(1-t)*(1-t));
            ball.position=start+direction*distance+Vector3.up*(Mathf.Sin(t*Mathf.PI*3)*.022f*(1-t));
            ball.Rotate(Vector3.Cross(Vector3.up,direction),(distance-previous)/.09f*Mathf.Rad2Deg,Space.World);
            RolledDistance+=distance-previous;previous=distance;
            if(t>.28f)Cat.transform.position=Vector3.MoveTowards(Cat.transform.position,
                new Vector3(ball.position.x,0,ball.position.z)-direction*.30f,1.15f*Time.deltaTime);
            yield return null;
        }
        ball.position=start+direction*.65f;
    }
    IEnumerator WalkTo(Vector3 target)
    {
        if(!CatActivityMotion.TryFloorPath(Cat.transform.position,target,out var path))yield break;
        PlayCatPose(CatActivityPose.Walk);
        foreach(var point in path)while((Cat.transform.position-point).sqrMagnitude>.0001f)
        {
            Vector3 delta=point-Cat.transform.position;delta.y=0;
            if(delta.sqrMagnitude>.001f)Cat.transform.rotation=Quaternion.RotateTowards(Cat.transform.rotation,Quaternion.LookRotation(delta),540*Time.deltaTime);
            Cat.transform.position=Vector3.MoveTowards(Cat.transform.position,point,1.5f*Time.deltaTime);yield return null;
        }
    }
    void Release(){if(contact!=null)contact.Clear();if(controller!=null)controller.enabled=true;if(Cat!=null)Cat.SetMovementLocked(this,false);}
    protected override void CancelActivity(){StopAllCoroutines();Release();if(ball!=null)ball.gameObject.SetActive(false);base.CancelActivity();}
    protected override void OnDisable(){StopAllCoroutines();Release();if(ball!=null)ball.gameObject.SetActive(false);base.OnDisable();}
#if UNITY_EDITOR
    public void EditorConfigureBall(Transform ballTransform,Transform[] points,int catchGoal)
    {ball=ballTransform;landingPoints=points??new Transform[0];catchesToComplete=Mathf.Max(1,catchGoal);}
#endif
}
