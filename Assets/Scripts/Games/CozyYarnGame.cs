using System;
using System.Collections.Generic;
using CatHome.Economy;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class CozyMiniGame
{
    private readonly List<Rect> cushions=new List<Rect>();
    private readonly List<GameObject> cushionObjects=new List<GameObject>();
    private readonly List<Vector3> approachPath=new List<Vector3>();
    private readonly List<Transform> aimDots=new List<Transform>();
    private Transform aimRoot,aimEnd;
    private Vector2 ballPoint,velocity,dragOrigin;
    private Vector3 approachTarget,captureFrom;
    private int level,shots,solved,stars,pointerId=int.MinValue;
    private float phaseTime,shotTime,levelEndTime,physicsRemainder;
    private bool moving,dragging,striking,approaching,capturing,levelEnded,bonusTaken;
    private int[] roundStars=new int[CozyGameRules.LevelCount];
    private bool[] roundPearls=new bool[CozyGameRules.LevelCount];
    public int Level=>level;
    public int Shots=>shots;
    public int Solved=>solved;
    public Vector2 BallPoint=>ballPoint;
    public bool CanShoot=>running&&!paused&&!moving&&!striking&&!approaching&&!capturing&&!levelEnded;
    private void BuildAim()
    {
        aimRoot=new GameObject("DottedAimTrail").transform;aimRoot.SetParent(transform,false);
        var material=Resources.Load<Material>("CozyGames/Aim");
        for(int i=0;i<36;i++)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="TrailDot"+i;go.transform.SetParent(aimRoot,false);Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.65f,.96f,.82f));block.SetColor("_Color",new Color(.65f,.96f,.82f));go.GetComponent<Renderer>().SetPropertyBlock(block);aimDots.Add(go.transform);
        }
        if(ripple!=null)aimEnd=Instantiate(ripple.gameObject,transform).transform;
        if(aimEnd!=null)aimEnd.gameObject.SetActive(false);
        HideAim();
    }
    private void HideAim(){if(aimRoot!=null)aimRoot.gameObject.SetActive(false);if(aimEnd!=null)aimEnd.gameObject.SetActive(false);}
    private void DrawAim(Vector2 pull)
    {
        if(aimRoot==null)return;aimRoot.gameObject.SetActive(true);
        Vector2 point=ballPoint,vel=CozyGameRules.Launch(pull);int visible=0;float distance=0;Vector2 previous=point;
        for(int step=0;step<500&&visible<aimDots.Count;step++)
        {
            CozyGameRules.StepBall(ref point,ref vel,cushions,level,.012f);distance+=Vector2.Distance(previous,point);previous=point;
            if(distance>=.16f){var dot=aimDots[visible];dot.gameObject.SetActive(true);dot.localPosition=World(point,.13f);float size=Mathf.Lerp(.088f,.038f,visible/(float)aimDots.Count);dot.localScale=new Vector3(size,.025f,size);distance=0;visible++;}
            if(CozyGameRules.IsInBasket(point,CozyGameRules.Goal(level),vel.magnitude)||vel.magnitude<.06f)break;
        }
        for(int i=visible;i<aimDots.Count;i++)aimDots[i].gameObject.SetActive(false);
    }
    private void SetupLevel(int index)
    {
        level=index;shots=0;moving=dragging=striking=approaching=capturing=levelEnded=bonusTaken=false;velocity=Vector2.zero;shotTime=levelEndTime=physicsRemainder=0;
        ballPoint=CozyGameRules.Start(level);ball.localPosition=World(ballPoint,.24f);basket.localPosition=World(CozyGameRules.Goal(level),.035f);
        var behind=CozyGameRules.Mirror(new Vector2(0,-.9f),level);actor.localPosition=World(ballPoint+behind,.03f);actor.localRotation=Quaternion.LookRotation(World(-behind,0));
        if(motion!=null)motion.Run(0);next.gameObject.SetActive(false);HideAim();
        foreach(var item in cushionObjects)Destroy(item);cushionObjects.Clear();cushions.Clear();
        foreach(var rect in CozyGameRules.Cushions(level))
        {
            cushions.Add(rect);var go=Instantiate(cushionPrefab,obstacleRoot,false);go.transform.localPosition=World(rect.center,.02f);go.transform.localScale=new Vector3(rect.width,.65f,rect.height);cushionObjects.Add(go);
        }
        bonus.localPosition=World(CozyGameRules.Mirror(new Vector2(.1f,1.3f),level),.12f);bonus.gameObject.SetActive(true);RefreshHud();
    }
    private void TickYarn(float dt)
    {
        if(levelEnded){levelEndTime+=dt;if(levelEndTime>=1.05f)NextLevel();return;}
        if(capturing)
        {
            phaseTime+=dt;float t=Mathf.Clamp01(phaseTime/.42f);
            ball.localPosition=Vector3.Lerp(captureFrom,World(CozyGameRules.Goal(level),.31f),Mathf.SmoothStep(0,1,t))+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.35f);
            ball.Rotate(Vector3.right,140*dt,Space.Self);
            if(t>=1){ballPoint=CozyGameRules.Goal(level);ball.localPosition=World(ballPoint,.31f);capturing=false;EndLevel(true);}
            return;
        }
        if(approaching)
        {
            if(approachPath.Count>0)
            {
                var to=approachPath[0]-actor.localPosition;to.y=0;
                if(to.magnitude<.04f)approachPath.RemoveAt(0);
                else {var heading=Quaternion.LookRotation(to);actor.localRotation=Quaternion.RotateTowards(actor.localRotation,heading,400*dt);float speed=Quaternion.Angle(actor.localRotation,heading)<35?2.2f:0;actor.localPosition=Vector3.MoveTowards(actor.localPosition,approachPath[0],speed*dt);if(motion!=null)motion.Run(speed);return;}
            }
            if(approachPath.Count==0){var heading=Quaternion.LookRotation(World(velocity.normalized,0));actor.localRotation=Quaternion.RotateTowards(actor.localRotation,heading,400*dt);if(motion!=null)motion.Run(0);if(Quaternion.Angle(actor.localRotation,heading)<2){approaching=false;striking=true;phaseTime=0;}}
            return;
        }
        if(striking){phaseTime+=dt;if(motion!=null)motion.Hunt(Mathf.Lerp(.1f,.82f,phaseTime/.32f));if(phaseTime>=.32f){striking=false;moving=true;shotTime=0;GameAudio.Play(AudioCue.BallRoll,.65f,AudioBus.MiniGame);}return;}
        if(!moving)return;shotTime+=dt;if(motion!=null)motion.Run(0);
        // Preview and play share an identical fixed step, independent of frame rate.
        physicsRemainder+=dt;const float step=.012f;int steps=Mathf.FloorToInt(physicsRemainder/step);physicsRemainder-=steps*step;
        for(int i=0;i<steps;i++)
        {
            CozyGameRules.StepBall(ref ballPoint,ref velocity,cushions,level,step);
            if(!bonusTaken&&Vector2.Distance(ballPoint,CozyGameRules.Mirror(new Vector2(.1f,1.3f),level))<.42f){bonusTaken=true;bonus.gameObject.SetActive(false);Say(CozyGameUi.Copy("İnci bonusu!","Pearl bonus!"));GameAudio.Play(AudioCue.Coin,.6f,AudioBus.MiniGame);}
            if(CozyGameRules.IsInBasket(ballPoint,CozyGameRules.Goal(level),velocity.magnitude)){moving=false;capturing=true;phaseTime=0;captureFrom=World(ballPoint,.24f);velocity=Vector2.zero;return;}
        }
        Vector3 displacement=World(velocity*dt,0);ball.Rotate(Vector3.Cross(Vector3.up,displacement),displacement.magnitude/CozyGameRules.BallRadius*Mathf.Rad2Deg,Space.World);ball.localPosition=World(ballPoint,.24f);
        if(moving&&(velocity.magnitude<.04f||shotTime>14)){moving=false;velocity=Vector2.zero;if(shots>=3)EndLevel(false);else {if(motion!=null)motion.Run(0);SaveContinuedCheckpoint();}}
    }
    public void Shoot(Vector2 pull)
    {
        if(!CanShoot||pull.magnitude<.12f)return;velocity=CozyGameRules.Launch(pull);shots++;approaching=true;phaseTime=0;FindYarnApproach();ClearPointer();HideAim();RefreshHud();SaveContinuedCheckpoint();
    }
    private void FindYarnApproach(){approachTarget=World(ballPoint-velocity.normalized*.82f,.03f);approachPath.Clear();approachPath.AddRange(CozyCatPath.Find(actor.localPosition,approachTarget,cushions,ballPoint));}
    private void EndLevel(bool success)
    {
        moving=striking=approaching=capturing=dragging=false;velocity=Vector2.zero;levelEnded=true;levelEndTime=0;HideAim();
        if(success)
        {
            solved++;int earned=CozyGameRules.Stars(shots,bonusTaken);stars+=earned;roundStars[level]=earned;roundPearls[level]=bonusTaken;CozyGameProgress.RecordYarn(level,earned);
            int points=CozyGameRules.YarnScore(level,earned,bonusTaken);score+=points;Say(CozyGameUi.Copy($"Sepette! +{points} puan · {earned} yıldız",$"In the basket! +{points} points · {earned} stars"));GameAudio.Play(AudioCue.Catch,.75f,AudioBus.MiniGame);
        }
        else Say(CozyGameUi.Copy("Yeni bölüm, yeni bir şans!","New puzzle, new chance!"));
        if(motion!=null)motion.Run(0);next.gameObject.SetActive(true);CozyGameUi.Label(next,CozyGameUi.Copy(level==23?"Sonuçlar":"Sonraki bölüm",level==23?"Results":"Next puzzle"));RefreshHud();SaveContinuedCheckpoint();
    }
    public void NextLevel(){if(kind!=CozyGameKind.Yarn||!running||paused||!levelEnded)return;if(level>=CozyGameRules.LevelCount-1)FinishRound();else {SetupLevel(level+1);SaveContinuedCheckpoint();}}
    private void YarnPointerDown(PointerEventData e){if(!CanShoot||dragging||!Point(e.position,out var point)||Vector2.Distance(point,ballPoint)>.75f)return;dragOrigin=point;dragging=true;pointerId=e.pointerId;}
    private void YarnPointerDrag(PointerEventData e){if(!dragging||e.pointerId!=pointerId||!CanShoot||!Point(e.position,out var point))return;DrawAim(Vector2.ClampMagnitude(dragOrigin-point,CozyGameRules.MaxPull));}
    private void YarnPointerUp(PointerEventData e){if(!dragging||e.pointerId!=pointerId)return;ClearPointer();HideAim();if(Point(e.position,out var point))Shoot(dragOrigin-point);}
    private YarnCheckpoint CaptureCheckpoint()=>new YarnCheckpoint{
        runId=roundId,level=level,shots=shots,solved=solved,stars=stars,score=score,paidCoins=paidCoins,
        phase=levelEnded?5:capturing?4:moving?3:striking?2:approaching?1:0,
        roundStars=(int[])roundStars.Clone(),pearls=(bool[])roundPearls.Clone(),bonusTaken=bonusTaken,continued=continued,completionRecorded=completionRecorded,
        seconds=clock,phaseTime=phaseTime,shotTime=shotTime,stepRemainder=physicsRemainder,ballHeight=ball.localPosition.y,ball=capturing?new Vector2(ball.localPosition.x,ball.localPosition.z):ballPoint,velocity=velocity,
        cat=actor.localPosition,catRotation=actor.localRotation,ballRotation=ball.localRotation,captureFrom=captureFrom};
    private void SaveContinuedCheckpoint(){if(kind!=CozyGameKind.Yarn||!continued||!running||clock<=0)return;CozyGameProgress.SetCheckpoint(CaptureCheckpoint());CatHomeSaveSystem.SaveNow();}
    public void RequestContinue()
    {
        if(kind!=CozyGameKind.Yarn||running||exiting)return;var saved=CozyGameProgress.Checkpoint;if(saved==null)return;
        if(saved.continued){RestoreCheckpoint(saved);return;}
        confirmationFromResult=result.activeSelf;result.SetActive(false);welcome.SetActive(false);
        confirm.SetActive(true);confirmation.text=CozyGameUi.Copy($"Bölüm {saved.level+1}, yumak konumu ve puanın korunur.\n60 saniye eklenir. Tur başına yalnız bir kez.\n\nÜcret: 2 elmas · Bakiyen: {EconomyService.Diamonds}",$"Keep puzzle {saved.level+1}, yarn position and score.\nAdd 60 seconds. Once per round.\n\nCost: 2 diamonds · Balance: {EconomyService.Diamonds}");
        confirmBuy.interactable=EconomyService.Diamonds>=CozyGameRules.ContinueDiamonds;
    }
    private void CancelContinue(){confirm.SetActive(false);result.SetActive(confirmationFromResult);welcome.SetActive(!confirmationFromResult);RefreshRecord();}
    public void ConfirmContinue()
    {
        if(kind!=CozyGameKind.Yarn||running||exiting||!confirm.activeSelf)return;var saved=CozyGameProgress.Checkpoint;if(saved==null||saved.continued)return;
        var payment=EconomyService.TrySpend(CurrencyType.Diamond,CozyGameRules.ContinueDiamonds,EconomySource.YarnRoute,"yarn-continue:"+saved.runId,EconomyPersistence.DeferToCaller);
        if(!payment.IsSettled){confirmation.text=CozyGameUi.Copy("Yeterli elmas yok. Hiçbir elmas harcanmadı.","Not enough diamonds. No diamonds were spent.");confirmBuy.interactable=false;return;}
        saved.continued=true;saved.seconds=CozyGameRules.ContinueSeconds;CozyGameProgress.SetCheckpoint(saved);CatHomeSaveSystem.SaveNow();RestoreCheckpoint(saved);
    }
    private void RestoreCheckpoint(YarnCheckpoint saved)
    {
        if(saved==null||running||exiting)return;
        roundId=saved.runId;continued=saved.continued;completionRecorded=saved.completionRecorded;paidCoins=saved.paidCoins;score=saved.score;solved=saved.solved;stars=saved.stars;
        roundStars=(int[])saved.roundStars.Clone();roundPearls=(bool[])saved.pearls.Clone();clock=saved.seconds;settled=paused=false;running=true;
        SetupLevel(saved.level);shots=saved.shots;ballPoint=saved.ball;velocity=saved.velocity;phaseTime=saved.phaseTime;shotTime=saved.shotTime;physicsRemainder=saved.stepRemainder;captureFrom=saved.captureFrom;
        bonusTaken=saved.bonusTaken;bonus.gameObject.SetActive(!bonusTaken);ball.localPosition=World(ballPoint,saved.ballHeight);ball.localRotation=saved.ballRotation;actor.localPosition=saved.cat;actor.localRotation=saved.catRotation;
        approaching=saved.phase==1;striking=saved.phase==2;moving=saved.phase==3;capturing=saved.phase==4;levelEnded=saved.phase==5;
        if(approaching)FindYarnApproach();next.gameObject.SetActive(levelEnded);CozyGameUi.Label(next,CozyGameUi.Copy("Sonraki bölüm","Next puzzle"));
        welcome.SetActive(false);result.SetActive(false);confirm.SetActive(false);pause.SetActive(false);hud.SetActive(true);ClearPointer();HideAim();RefreshHud();
    }
}

