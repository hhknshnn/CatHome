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
    CatToyBallCollision ballCollision;
    public int CatchCount=>catches;
    public int CatchGoal=>Mathf.Max(1,catchesToComplete);
    public float LastHitDistance {get;private set;}
    public float RolledDistance {get;private set;}
    public string InterruptedReason {get;private set;}
    public Transform Ball=>ball;
    protected override bool UsesFloorApproach=>true;
    protected override bool UsesNearbyRoutineEntry=>true;
    public override string ProgressLabel=>IsRunning?(GameLanguageService.Current==GameLanguage.Turkish?
        $"Pati ve takip · {catches}/{CatchGoal}":$"Bat and chase · {catches}/{CatchGoal}"):string.Empty;
    protected override void Awake(){base.Awake();if(ball!=null)ball.gameObject.SetActive(false);}
    protected override bool CanBeginActivity(out string reason)
    {
        reason="Top için biraz açık alan gerekiyor.";
        if(ball==null||RoutineEntryPoint==null)return false;
        ballCollision?.Dispose();ballCollision=new CatToyBallCollision(gameObject.scene,ball);
        try
        {
            Vector3 entry=RoutineFloorPosition;entry.y=0;
            Vector3 outward=entry-transform.position;outward.y=0;
            if(!ChooseDirection(entry,outward.normalized,1.30f,out playDirection))return false;
            reason=string.Empty;return true;
        }
        finally
        {
            // Validation can be followed by an energy/path refusal. It must
            // never leave a private physics scene owned by an unstarted game.
            ballCollision.Dispose();ballCollision=null;
        }
    }
    protected override bool BeginActivity()
    {
        ballCollision=new CatToyBallCollision(gameObject.scene,ball);
        controller=Cat.GetComponent<CharacterController>();
        contact=Cat.GetComponent<CatToyContactMotion>()??Cat.gameObject.AddComponent<CatToyContactMotion>();
        pose=Cat.GetComponent<CatActivityAnimation>();
        catches=0;RolledDistance=0;LastHitDistance=float.PositiveInfinity;InterruptedReason=null;
        Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        StartCoroutine(PlayRoutine());return true;
    }
    bool ChooseDirection(Vector3 origin,Vector3 preferred,float distance,out Vector3 direction)
    {
        for(int side=0;side<24;side++)
        {
            float angle=(side+1)/2*(side%2==0?-15f:15f);
            direction=Quaternion.Euler(0,angle,0)*(preferred.sqrMagnitude>.01f?preferred:Vector3.forward);
            if(CatActivityFacing.FacingDot(direction,origin,CatActivityFacing.CameraPosition(Cat))<.30f)continue;
            if(CatActivityMotion.ClearSegment(origin,origin+direction*distance)&&
               ballCollision.ClearRoll(origin+direction*.30f,origin+direction*distance))return true;
        }
        direction=Vector3.zero;return false;
    }
    IEnumerator PlayRoutine()
    {
        Cat.transform.rotation=Quaternion.LookRotation(playDirection);
        ball.position=ballCollision.OnFloor(Cat.transform.position+playDirection*.30f);
        ball.gameObject.SetActive(true);
        PlayCatPose(CatActivityPose.Sniff);yield return new WaitForSeconds(.45f);
        for(int beat=0;beat<CatchGoal;beat++)
        {
            Vector3 floor=ball.position;floor.y=0;
            Vector3 direction=playDirection;
            Vector3 stand=floor-direction*.30f;
            bool found=false;
            for(int side=0;side<24;side++)
            {
                float angle=(side+1)/2*(side%2==0?-15f:15f);
                direction=Quaternion.Euler(0,angle,0)*playDirection;
                stand=floor-direction*.30f;
                if(CatActivityFacing.FacingDot(direction,stand,CatActivityFacing.CameraPosition(Cat))<.30f)continue;
                // Leave room BEYOND the stopping point. Otherwise a ball at the
                // wall cannot be approached from its other side for the next bat.
                if(CatActivityMotion.ClearSegment(stand,floor+direction*.99f) && ballCollision.ClearRoll(floor,floor+direction*.99f) &&
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
                if(Time.deltaTime>0f && !hit && phase>=.34f && phase<.75f && contact.Distance<.095f)
                {
                    hit=true;LastHitDistance=contact.Distance;catches++;NotifyChanged();
                    contact.Clear();
                    yield return Roll(ball.position,direction);
                    break;
                }
                elapsed+=Time.deltaTime;yield return null;
            }
            contact.Clear();
            if(!hit){InterruptedReason="No paw contact at "+ball.position+" from "+Cat.transform.position;CancelActivity();yield break;}
            playDirection=direction;
            yield return WalkTo(new Vector3(ball.position.x,0,ball.position.z)-direction*.30f);
            PlayCatPose(CatActivityPose.Sniff);
            yield return CatActivityFacing.Turn(Cat,CatActivityFacing.Resolve(Cat,Cat.transform.position,Quaternion.LookRotation(direction)));
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
            Vector3 before=ball.position;
            ball.position=ballCollision.Sweep(before,direction*(distance-previous),out bool blocked);
            float moved=Vector3.Distance(before,ball.position);
            ball.Rotate(Vector3.Cross(Vector3.up,direction),moved/.09f*Mathf.Rad2Deg,Space.World);
            RolledDistance+=moved;previous=distance;
            if(t>.28f)
            {
                Vector3 next=Vector3.MoveTowards(Cat.transform.position,new Vector3(ball.position.x,0,ball.position.z)-direction*.30f,1.15f*Time.deltaTime);
                if(CatActivityMotion.ClearSegment(Cat.transform.position,next))Cat.transform.position=next;
            }
            if(blocked)break;
            yield return null;
        }
        // No final teleport: a swept contact is the actual stopping point.
    }
    IEnumerator WalkTo(Vector3 target)
    {
        if(!CatActivityMotion.TryFloorPath(Cat.transform.position,target,out var path))yield break;
        foreach(var point in path)while((Cat.transform.position-point).sqrMagnitude>.0001f)
        {
            Vector3 delta=point-Cat.transform.position;delta.y=0;
            if(delta.sqrMagnitude>.001f)
            {
                Quaternion travel=Quaternion.LookRotation(delta);
                if(Quaternion.Angle(Cat.transform.rotation,travel)>=60f)
                {PlayCatPose(CatActivityPose.Sniff);yield return CatActivityFacing.Turn(Cat,travel,.22f);}
                Cat.transform.rotation=Quaternion.RotateTowards(Cat.transform.rotation,travel,540*Time.deltaTime);
            }
            PlayCatPose(CatActivityPose.Walk);
            Cat.transform.position=Vector3.MoveTowards(Cat.transform.position,point,1.5f*Time.deltaTime);yield return null;
        }
    }
    void Release(){ballCollision?.Dispose();ballCollision=null;if(contact!=null)contact.Clear();if(controller!=null)controller.enabled=true;if(Cat!=null)Cat.SetMovementLocked(this,false);}
    protected override void CancelActivity(){if(!IsRunning)return;StopAllCoroutines();if(!HasBegunActivity){base.CancelActivity();return;}Release();if(ball!=null)ball.gameObject.SetActive(false);base.CancelActivity();}
#if UNITY_EDITOR
    public void EditorConfigureBall(Transform ballTransform,Transform[] points,int catchGoal)
    {ball=ballTransform;landingPoints=points??new Transform[0];catchesToComplete=Mathf.Max(1,catchGoal);}
#endif
}
