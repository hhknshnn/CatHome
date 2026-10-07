using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class CozyMiniGame
{
    private int fishCaught,targetFish=-1,pondCombo,bestPondCombo,pondPerfect,pondBonuses,pondBasePoints,pondComboSteps;
    private float pondPhase,pondPhaseTime,dockTarget,pondElapsed;
    private bool pondDragging,pondStrikeResolved;
    private Vector3 pondFrom,pondTo;
    private float[] fishCooldown=new float[6],fishPhaseOffset=new float[6];
    private Transform[] readyRings;
    private Texture2D[] fishPortraits=new Texture2D[6];
    private Vector3[] fishHomes;
    private const float DockZ=-2.30f,DockY=.115f;
    public int FishCaught=>fishCaught;
    public int RequestedFish=>Mathf.FloorToInt(pondElapsed/12f)%6;
    public bool FishReady{get{for(int i=0;i<6;i++)if(CanCatchFish(i))return true;return false;}}
    public bool CanCatchFish(int index)
    {
        if(kind!=CozyGameKind.Pond||!running||paused||targetFish>=0||index<0||index>=6||fishCooldown[index]>0)return false;
        Vector3 p=fishRoot.GetChild(index).localPosition;var delta=p-actor.localPosition;delta.y=0;
        return p.z<-.86f&&Mathf.Abs(delta.x)<.66f&&delta.magnitude<1.52f;
    }
    private void BuildPondFeedback()
    {
        readyRings=new Transform[6];
        for(int i=0;i<6;i++){fishPortraits[i]=Resources.Load<Texture2D>("CozyGames/Fish"+i+"Preview");readyRings[i]=Instantiate(ripple.gameObject,transform).transform;readyRings[i].name="FishReachRing"+i;readyRings[i].localScale=Vector3.one*.72f;readyRings[i].gameObject.SetActive(false);}
        ripple.gameObject.SetActive(false);
    }
    private Vector3 FishPosition(int index,float elapsed)
    {
        float pace=.74f+index*.035f+Mathf.Min(.2f,elapsed*.003f);
        float phase=elapsed*pace+index*1.07f+fishPhaseOffset[index];
        float x=(index%3-1)*1.45f+Mathf.Sin(phase*.61f+index)*.42f;
        float depth=1.72f*Mathf.Sqrt(Mathf.Clamp01(1-(x-.2f)*(x-.2f)/9));
        return new Vector3(x,.083f+Mathf.Sin(phase*2)*.012f,.04f+Mathf.Cos(phase)*depth);
    }
    private void SetupPond()
    {
        targetFish=-1;pondPhase=pondPhaseTime=pondElapsed=0;pondCombo=bestPondCombo=fishCaught=pondPerfect=pondBonuses=pondBasePoints=pondComboSteps=0;dockTarget=0;
        fishCooldown=new float[6];fishPhaseOffset=new float[6];fishHomes=new Vector3[6];
        for(int i=0;i<6;i++){var fish=fishRoot.GetChild(i);fish.gameObject.SetActive(true);fish.localPosition=FishPosition(i,0);fishHomes[i]=fish.localPosition;if(readyRings!=null)readyRings[i].gameObject.SetActive(false);}
        actor.localPosition=new Vector3(0,DockY,DockZ);actor.localRotation=Quaternion.identity;ripple.gameObject.SetActive(false);if(motion!=null)motion.Run(0);
    }
    private void TickPond(float dt)
    {
        pondElapsed+=dt;
        for(int i=0;i<6;i++)
        {
            var fish=fishRoot.GetChild(i);
            if(fishCooldown[i]>0){fishCooldown[i]=Mathf.Max(0,fishCooldown[i]-dt);fish.gameObject.SetActive(false);readyRings[i].gameObject.SetActive(false);continue;}
            fish.gameObject.SetActive(true);var point=FishPosition(i,pondElapsed);var direction=FishPosition(i,pondElapsed+.07f)-point;direction.y=0;fish.localPosition=point;if(direction.sqrMagnitude>.00001f)fish.localRotation=Quaternion.LookRotation(direction);
            bool shore=point.z<-.8f;readyRings[i].gameObject.SetActive(shore&&targetFish<0);
            if(shore){readyRings[i].localPosition=point+Vector3.up*.055f;bool ready=CanCatchFish(i);Color color=ready?new Color(.45f,1,.72f):i==RequestedFish?new Color(1,.72f,.32f):new Color(.75f,.88f,.81f);TintRing(readyRings[i],color);readyRings[i].localScale=Vector3.one*(ready?.72f:.56f);}
        }
        if(targetFish<0)
        {
            var p=actor.localPosition;float x=Mathf.MoveTowards(p.x,dockTarget,3.6f*dt);float speed=Mathf.Abs(x-p.x)/Mathf.Max(dt,.0001f);actor.localPosition=new Vector3(x,DockY,DockZ);
            var heading=speed>.02f?Quaternion.LookRotation(new Vector3(Mathf.Sign(x-p.x),0,.6f)):Quaternion.identity;
            actor.localRotation=Quaternion.RotateTowards(actor.localRotation,heading,540*dt);if(motion!=null)motion.Run(speed);
            return;
        }
        pondPhaseTime+=dt;
        if(pondPhase==1)
        {
            var fishAt=FishPosition(targetFish,pondElapsed+.4f);var d=fishAt-actor.localPosition;d.y=0;var heading=Quaternion.LookRotation(d);
            actor.localRotation=Quaternion.RotateTowards(actor.localRotation,heading,720*dt);if(motion!=null)motion.Run(0);
            if(Quaternion.Angle(actor.localRotation,heading)<8||pondPhaseTime>.28f)
            {
                pondFrom=actor.localPosition;pondTo=fishAt-d.normalized*.43f;pondTo.y=DockY;pondPhase=2;pondPhaseTime=0;
            }
        }
        else if(pondPhase==2)
        {
            float t=Mathf.Clamp01(pondPhaseTime/.36f);actor.localPosition=Vector3.Lerp(pondFrom,pondTo,Mathf.SmoothStep(0,1,t))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.26f);
            if(motion!=null)motion.Hunt(Mathf.Lerp(.12f,.82f,t));
            if(t>=1&&!pondStrikeResolved)
            {
                pondStrikeResolved=true;var fish=fishRoot.GetChild(targetFish);Vector3 paw=actor.localPosition+actor.forward*.43f;var gap=fish.localPosition-paw;gap.y=0;
                bool caught=gap.magnitude<.60f;
                if(caught)
                {
                    bool perfect=gap.magnitude<.23f;bool requested=targetFish==RequestedFish;
                    fishCaught++;pondCombo=Mathf.Min(5,pondCombo+1);bestPondCombo=Mathf.Max(bestPondCombo,pondCombo);
                    pondBasePoints+=100+targetFish*15;pondComboSteps+=pondCombo-1;if(perfect)pondPerfect++;if(requested)pondBonuses++;
                    int gained=CozyGameRules.PondScore(targetFish,pondCombo,perfect,requested);score+=gained;CozyGameProgress.RecordFish(targetFish);
                    Say(CozyGameUi.Copy((perfect?"Kusursuz! ":"Yakaladın! ")+$"+{gained} puan · Seri x{pondCombo}",(perfect?"Perfect! ":"Caught! ")+$"+{gained} points · Streak x{pondCombo}"));
                    fishCooldown[targetFish]=2.1f;GameAudio.Play(AudioCue.Catch,.75f,AudioBus.MiniGame);
                }
                else {pondCombo=0;Say(CozyGameUi.Copy("Az kaldı! Kıyıya yaklaşmasını bekle.","So close! Wait until it reaches the shore."));fishPhaseOffset[targetFish]+=.8f;}
                ripple.gameObject.SetActive(true);ripple.localPosition=fish.localPosition+Vector3.up*.07f;TintRing(ripple,caught?new Color(.65f,1,.8f):new Color(.9f,.8f,.65f));pondPhase=3;pondPhaseTime=0;
            }
        }
        else
        {
            float t=Mathf.Clamp01(pondPhaseTime/.42f);actor.localPosition=Vector3.Lerp(pondTo,pondFrom,Mathf.SmoothStep(0,1,t))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.20f);
            ripple.localScale=Vector3.one*Mathf.Lerp(.55f,1.7f,t);if(motion!=null)motion.Hunt(Mathf.Lerp(.82f,1,t));
            if(t>=1){targetFish=-1;actor.localPosition=pondFrom;dockTarget=pondFrom.x;ripple.gameObject.SetActive(false);if(motion!=null)motion.Run(0);}
        }
    }
    static void TintRing(Transform ring,Color color)
    {
        var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);block.SetColor("_Color",color);foreach(var renderer in ring.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(block);
    }
    public void SelectFish(int index)
    {
        if(!running||paused||kind!=CozyGameKind.Pond||targetFish>=0||index<0||index>=6)return;
        if(!CanCatchFish(index))
        {
            pondCombo=0;Say(CozyGameUi.Copy("İskelede hizalan; balık yeşilken dokun.","Line up on the deck; tap when the fish glows mint."));return;
        }
        targetFish=index;pondPhase=1;pondPhaseTime=0;pondStrikeResolved=false;ClearPointer();
    }
    public void Paw(){for(int i=0;i<6;i++)if(CanCatchFish(i)){SelectFish(i);return;}}
    public void MoveAlongDock(float x){if(!running||paused||kind!=CozyGameKind.Pond||targetFish>=0)return;dockTarget=Mathf.Clamp(x,-2.45f,2.45f);}
    private void PondPointerDown(PointerEventData e)
    {
        float best=Mathf.Clamp(Screen.height*.065f,36,82);int found=-1;
        for(int i=0;i<6;i++){if(fishCooldown[i]>0)continue;float d=Vector2.Distance(e.position,gameCamera.WorldToScreenPoint(fishRoot.GetChild(i).position));if(d<best){best=d;found=i;}}
        if(found>=0){SelectFish(found);return;}
        if(Point(e.position,out var point)){pondDragging=true;pointerId=e.pointerId;MoveAlongDock(point.x);}
    }
    private void PondPointerDrag(PointerEventData e){if(!pondDragging||e.pointerId!=pointerId)return;if(Point(e.position,out var point))MoveAlongDock(point.x);}
}

