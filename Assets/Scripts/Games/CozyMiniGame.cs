using System;
using System.Collections;
using System.Collections.Generic;
using CatHome.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class CozyMiniGame:MonoBehaviour
{
    public const string YarnScene="YarnRoute",PondScene="PondPlay";
    public CozyGameKind kind;
    public Camera gameCamera;
    public Transform actor,ball,basket,bonus,obstacleRoot,fishRoot,ripple;
    public GameObject cushionPrefab;
    private Canvas canvas;
    private RectTransform safe;
    private GameObject welcome,hud,result,pause,album,confirm;
    private TMP_Text status,instruction,progress,resultDetails,resultTitle,feedback,record,confirmation,pondBonusLabel;
    private RawImage pondBonusPortrait;
    private Button next,welcomeContinue,resultContinue,confirmBuy,welcomeStart,resultRetry;
    private TMP_Text welcomeLives,resultLives;
    private MiniGameCatAnimation motion;
    private readonly List<Behaviour> parked=new List<Behaviour>();
    private float clock,feedbackTime,checkpointSaveTime;
    private int score,paidCoins;
    private string roundId;
    private bool running,paused,exiting,settled,continued,completionRecorded,confirmationFromResult;
    public bool IsRunning=>running;
    public bool IsPaused=>paused;
    public float Remaining=>clock;
    public int Score=>score;
    public string Title=>CozyGameUi.Copy(kind==CozyGameKind.Yarn?"Yumak Rotası":"Gölet Keyfi",kind==CozyGameKind.Yarn?"Yarn Trail":"Pond Moments");
    private void Awake()
    {
        var catalog=CatBreedCatalog.Load();
        if(actor!=null&&catalog!=null){var visual=CatBreedVisualFactory.Create(CatBreedService.SelectedEntry,catalog.GameplayController,actor);var animator=visual==null?null:visual.GetComponentInChildren<Animator>();if(animator!=null)motion=animator.gameObject.AddComponent<MiniGameCatAnimation>();}
        BuildUi();ParkHome();
        if(kind==CozyGameKind.Yarn){BuildAim();SetupLevel(0);}else {BuildPondFeedback();SetupPond();}
        hud.SetActive(false);welcome.SetActive(true);RefreshRecord();
    }
    private void Start(){if(gameObject.scene.isLoaded)SceneManager.SetActiveScene(gameObject.scene);}
    private void BuildUi()
    {
        var go=new GameObject("CozyGameCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=240;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1;
        safe=CozyGameUi.Rect("SafeArea",go.transform,Vector2.zero,Vector2.one,Vector2.zero);safe.gameObject.AddComponent<SafeAreaRect>();
        var input=CozyGameUi.Rect("BoardInput",safe,Vector2.zero,Vector2.one,Vector2.zero).gameObject;input.AddComponent<Image>().color=Color.clear;input.AddComponent<CozyGamePointer>().owner=this;
        hud=CozyGameUi.Rect("HUD",safe,Vector2.zero,Vector2.one,Vector2.zero).gameObject;
        var top=CozyGameUi.Panel("Top",hud.transform,new Vector2(.035f,.865f),new Vector2(.84f,.975f),true);
        CozyGameUi.Text("Title",top.transform,Title,new Vector2(.025f,.44f),new Vector2(.55f,.96f),33,true);
        status=CozyGameUi.Text("Status",top.transform,"",new Vector2(.025f,.025f),new Vector2(.67f,.45f),23);
        progress=CozyGameUi.Text("Progress",top.transform,"",new Vector2(.61f,.15f),new Vector2(.985f,.85f),30,true);progress.alignment=TextAlignmentOptions.MidlineRight;
        CozyGameUi.Button("Pause",hud.transform,"II",new Vector2(.885f,.881f),new Vector2(.968f,.966f),Pause,true);
        var bottom=CozyGameUi.Panel("InstructionCard",hud.transform,new Vector2(.035f,.025f),new Vector2(.76f,.125f),true);
        instruction=CozyGameUi.Text("Instruction",bottom.transform,"",new Vector2(.02f,.04f),new Vector2(.98f,.96f),24);
        next=CozyGameUi.Button("Next",hud.transform,"",new Vector2(.785f,.035f),new Vector2(.965f,.12f),NextLevel);next.gameObject.SetActive(false);
        if(kind==CozyGameKind.Pond)
        {
            var bonusCard=CozyGameUi.Panel("BonusFish",hud.transform,new Vector2(.785f,.025f),new Vector2(.965f,.125f),true);
            pondBonusPortrait=CozyGameUi.Rect("Portrait",bonusCard.transform,new Vector2(.02f,.08f),new Vector2(.40f,.92f),Vector2.zero).gameObject.AddComponent<RawImage>();pondBonusPortrait.raycastTarget=false;
            pondBonusLabel=CozyGameUi.Text("Bonus",bonusCard.transform,"",new Vector2(.42f,.08f),new Vector2(.97f,.92f),22,true);
        }
        feedback=CozyGameUi.Text("Feedback",hud.transform,"",new Vector2(.18f,.725f),new Vector2(.82f,.815f),32,true);feedback.alignment=TextAlignmentOptions.Center;
        welcome=Overlay("Welcome",out var card);
        CozyGameUi.Text("Eyebrow",card,CozyGameUi.Copy("KÜÇÜK BİR MACERA","A LITTLE ADVENTURE"),new Vector2(.065f,.85f),new Vector2(.7f,.94f),21);
        CozyGameUi.Text("Title",card,Title,new Vector2(.065f,.69f),new Vector2(.94f,.855f),51,true);
        string description=kind==CozyGameKind.Yarn?
            CozyGameUi.Copy("Geriye çek, nişan al, sepete ulaştır.\n24 bölüm · 3 dakika · Bölüm başına 3 pati hakkı\nİlerledikçe her sepet daha çok puan kazandırır.","Pull back, aim and roll into the basket.\n24 puzzles · 3 minutes · 3 strokes per puzzle\nLater baskets are worth more points."):
            CozyGameUi.Copy("İskelede sağa/sola kaydırarak kedini yönlendir.\nKıyıya gelen balık yeşilken üzerine dokun.\nSeri yap, bonus balığı yakala, albümünü doldur.","Slide along the deck to move your cat.\nTap a fish when it reaches shore and glows mint.\nBuild a streak and catch the bonus fish.");
        CozyGameUi.Text("Description",card,description,new Vector2(.065f,.42f),new Vector2(.94f,.69f),27);
        record=CozyGameUi.Text("Record",card,"",new Vector2(.065f,.335f),new Vector2(.94f,.415f),22);
        welcomeContinue=CozyGameUi.Button("ContinueRun",card,"",new Vector2(.065f,.225f),new Vector2(.94f,.32f),RequestContinue,true);
        welcomeLives=CozyGameUi.Text("SharedLives",card,"",new Vector2(.065f,.205f),new Vector2(.94f,.325f),23);
        welcomeStart=CozyGameUi.Button("Start",card,CozyGameUi.Copy("Yeni tur","New round"),new Vector2(.065f,.065f),new Vector2(.61f,.20f),StartRound);
        CozyGameUi.Button("Games",card,CozyGameUi.Copy("Oyunlar","Games"),new Vector2(.65f,.065f),new Vector2(.94f,.20f),()=>Exit(true),true);
        result=Overlay("Results",out card);
        resultTitle=CozyGameUi.Text("Title",card,"",new Vector2(.065f,.77f),new Vector2(.94f,.92f),47,true);
        resultDetails=CozyGameUi.Text("Details",card,"",new Vector2(.065f,.405f),new Vector2(.94f,.76f),28);
        resultContinue=CozyGameUi.Button("ContinueRun",card,"",new Vector2(.065f,.285f),new Vector2(.94f,.39f),RequestContinue);
        resultLives=CozyGameUi.Text("SharedLives",card,"",new Vector2(.065f,.265f),new Vector2(.94f,.39f),23);
        resultRetry=CozyGameUi.Button("Retry",card,CozyGameUi.Copy("Yeni tur","New round"),new Vector2(.065f,.15f),new Vector2(.485f,.255f),StartRound,true);
        CozyGameUi.Button("Games",card,CozyGameUi.Copy("Oyunlar","Games"),new Vector2(.525f,.15f),new Vector2(.94f,.255f),()=>Exit(true),true);
        CozyGameUi.Button("Home",card,CozyGameUi.Copy("Eve dön","Back home"),new Vector2(.065f,.025f),new Vector2(.94f,.12f),()=>Exit(false),true);
        pause=Overlay("Pause",out card);
        CozyGameUi.Text("Title",card,CozyGameUi.Copy("Küçük bir ara","A little pause"),new Vector2(.065f,.72f),new Vector2(.94f,.88f),48,true);
        CozyGameUi.Text("Description",card,CozyGameUi.Copy("Süre de seninle birlikte durdu.","The timer is taking a break, too."),new Vector2(.065f,.63f),new Vector2(.94f,.72f),23);
        CozyGameUi.Button("Resume",card,CozyGameUi.Copy("Devam et","Continue"),new Vector2(.065f,.45f),new Vector2(.94f,.58f),Resume);
        CozyGameUi.Button("Games",card,CozyGameUi.Copy("Oyunlara dön","Back to games"),new Vector2(.065f,.265f),new Vector2(.94f,.395f),()=>Exit(true),true);
        CozyGameUi.Button("Home",card,CozyGameUi.Copy("Eve dön","Back home"),new Vector2(.065f,.08f),new Vector2(.94f,.21f),()=>Exit(false),true);
        album=Overlay("Album",out card);
        CozyGameUi.Text("Title",card,CozyGameUi.Copy("Gölet albümüm","My pond album"),new Vector2(.065f,.8f),new Vector2(.94f,.94f),42,true);
        for(int i=0;i<6;i++)
        {
            float x=.065f+(i%2)*.455f,y=.59f-(i/2)*.19f;
            var tile=CozyGameUi.Panel("FishCard"+i,card,new Vector2(x,y),new Vector2(x+.42f,y+.17f));
            var photo=CozyGameUi.Rect("Portrait",tile.transform,new Vector2(.02f,.08f),new Vector2(.34f,.92f),Vector2.zero).gameObject.AddComponent<RawImage>();photo.texture=Resources.Load<Texture2D>("CozyGames/Fish"+i+"Preview");photo.raycastTarget=false;
            CozyGameUi.Text("Fish"+i,tile.transform,"",new Vector2(.34f,.05f),new Vector2(.98f,.95f),24,true);
        }
        CozyGameUi.Button("Back",card,CozyGameUi.Copy("Geri","Back"),new Vector2(.065f,.04f),new Vector2(.94f,.165f),()=>{album.SetActive(false);welcome.SetActive(true);RefreshRecord();},true);
        if(kind==CozyGameKind.Pond)CozyGameUi.Button("Album",welcome.transform.Find("Card"),CozyGameUi.Copy("Albüm","Album"),new Vector2(.76f,.855f),new Vector2(.94f,.94f),ShowAlbum,true);
        confirm=Overlay("ContinueConfirmation",out card);
        CozyGameUi.Text("Title",card,CozyGameUi.Copy("Bir şans daha?","One more chance?"),new Vector2(.065f,.73f),new Vector2(.94f,.9f),46,true);
        confirmation=CozyGameUi.Text("Details",card,"",new Vector2(.065f,.39f),new Vector2(.94f,.7f),29);
        confirmBuy=CozyGameUi.Button("SpendDiamonds",card,CozyGameUi.Copy("2 elmas harca · +60 sn","Spend 2 diamonds · +60s"),new Vector2(.065f,.205f),new Vector2(.94f,.34f),ConfirmContinue);
        CozyGameUi.Button("Cancel",card,CozyGameUi.Copy("Vazgeç","Cancel"),new Vector2(.065f,.05f),new Vector2(.94f,.165f),CancelContinue,true);
        result.SetActive(false);pause.SetActive(false);album.SetActive(false);confirm.SetActive(false);
    }
    private GameObject Overlay(string name,out RectTransform card)
    {
        var root=CozyGameUi.Rect(name,safe,Vector2.zero,Vector2.one,Vector2.zero);root.gameObject.AddComponent<Image>().color=new Color(.035f,.10f,.11f,.40f);
        card=(RectTransform)CozyGameUi.Panel("Card",root,new Vector2(.205f,.13f),new Vector2(.795f,.87f),true).transform;return root.gameObject;
    }
    private void RefreshRecord()
    {
        record.text=kind==CozyGameKind.Yarn?CozyGameUi.Copy($"Rekor {CozyGameProgress.YarnBest} puan · {CozyGameProgress.TotalStars}/72 yıldız",$"Best {CozyGameProgress.YarnBest} points · {CozyGameProgress.TotalStars}/72 stars"):CozyGameUi.Copy($"Rekor {CozyGameProgress.PondBest} puan · Albüm {CozyGameProgress.FishCount}/6",$"Best {CozyGameProgress.PondBest} points · Album {CozyGameProgress.FishCount}/6");
        RefreshEntryLives();
        var checkpoint=kind==CozyGameKind.Yarn?CozyGameProgress.Checkpoint:null;
        foreach(var button in new[]{welcomeContinue,resultContinue})
        {
            button.gameObject.SetActive(checkpoint!=null);if(checkpoint!=null)CozyGameUi.Label(button,checkpoint.continued?CozyGameUi.Copy("Kaydedilen tura devam et · Ücret ödendi","Resume saved round · Already paid"):CozyGameUi.Copy($"Bölüm {checkpoint.level+1} · 2 elmas ile +60 sn",$"Puzzle {checkpoint.level+1} · +60s for 2 diamonds"));
        }
    }
    private void RefreshEntryLives()
    {
        bool canStart=MiniGameLivesService.CanStartRound();
        string label=canStart?CozyGameUi.Copy("Yeni tur · 1 can","New round · 1 life"):CozyGameUi.Copy("Can bekleniyor","Waiting for a life");
        if(MiniGameLivesService.IsUnlimited)label=CozyGameUi.Copy("Yeni tur","New round");
        foreach(var button in new[]{welcomeStart,resultRetry}){button.interactable=canStart;CozyGameUi.Label(button,label);}
        bool hasContinue=kind==CozyGameKind.Yarn&&CozyGameProgress.Checkpoint!=null;
        welcomeLives.gameObject.SetActive(!hasContinue);resultLives.gameObject.SetActive(!hasContinue);
        welcomeLives.text=resultLives.text=MiniGameLivesPresentation.Summary();
    }
    private void ShowAlbum()
    {
        welcome.SetActive(false);album.SetActive(true);
        for(int i=0;i<6;i++){var tile=album.transform.Find("Card/FishCard"+i);bool found=CozyGameProgress.HasFish(i);tile.Find("Fish"+i).GetComponent<TMP_Text>().text=found?CozyGameRules.FishName(i):CozyGameUi.Copy("Keşfedilmedi","Undiscovered");tile.Find("Portrait").GetComponent<RawImage>().color=found?Color.white:new Color(.18f,.33f,.32f,.7f);}
    }
    public void StartRound()
    {
        if(running||exiting)return;
        if(!MiniGameLivesService.TrySpendLife()){RefreshEntryLives();return;}
        roundId=Guid.NewGuid().ToString("N");running=true;settled=paused=continued=completionRecorded=false;score=paidCoins=0;
        clock=kind==CozyGameKind.Yarn?CozyGameRules.YarnSeconds:CozyGameRules.PondSeconds;feedback.text="";feedbackTime=0;
        welcome.SetActive(false);result.SetActive(false);pause.SetActive(false);album.SetActive(false);confirm.SetActive(false);hud.SetActive(true);
        if(kind==CozyGameKind.Yarn){solved=stars=0;roundStars=new int[CozyGameRules.LevelCount];roundPearls=new bool[CozyGameRules.LevelCount];CozyGameProgress.SetCheckpoint(null);SetupLevel(0);}
        else SetupPond();
        CatHomeSaveSystem.SaveNow();
        GameAudio.Play(AudioCue.Start,.7f,AudioBus.MiniGame);RefreshHud();
    }
    private void Update()
    {
        if(!running&&!exiting)RefreshEntryLives();
        if(!running||paused||exiting)return;float dt=Time.deltaTime;
        clock=Mathf.Max(0,clock-dt);if(clock<=0){FinishRound();return;}
        if(feedbackTime>0){feedbackTime-=dt;if(feedbackTime<=0)feedback.text="";}
        if(kind==CozyGameKind.Yarn){TickYarn(dt);if(continued&&(checkpointSaveTime-=dt)<=0){SaveContinuedCheckpoint();checkpointSaveTime=5;}}
        else TickPond(dt);
        RefreshHud();
    }
    private void Say(string text){feedback.text=text;feedbackTime=1.7f;}
    private void RefreshHud()
    {
        if(status==null)return;
        string time=TimeSpan.FromSeconds(Mathf.CeilToInt(clock)).ToString(@"mm\:ss");
        progress.text=CozyGameUi.Copy($"{score} puan  ·  {time}",$"{score} points  ·  {time}");
        progress.color=clock<=15?StorybookScreenStyle.CoralTop:StorybookScreenStyle.Ink;
        if(kind==CozyGameKind.Yarn)
        {
            status.text=CozyGameUi.Copy($"Bölüm {level+1}/24 · {Mathf.Max(0,3-shots)} pati · {stars} yıldız",$"Puzzle {level+1}/24 · {Mathf.Max(0,3-shots)} strokes · {stars} stars");
            instruction.text=CozyGameUi.Copy(levelEnded?"Sepet tamam! Yeni bölüm geliyor…":moving||striking||approaching||capturing?"Yumağın yolculuğunu izle…":"Yumağı geriye çek; noktalı rotayı izle ve bırak.",levelEnded?"Basket complete! Next puzzle…":moving||striking||approaching||capturing?"Watch your yarn roll…":"Pull the yarn back; follow the dotted trail and release.");
        }
        else
        {
            status.text=CozyGameUi.Copy($"{fishCaught} balık · Seri x{Mathf.Max(1,pondCombo)} · Albüm {CozyGameProgress.FishCount}/6",$"{fishCaught} fish · Streak x{Mathf.Max(1,pondCombo)} · Album {CozyGameProgress.FishCount}/6");
            instruction.text=CozyGameUi.Copy("İskelede kaydır · Yeşil balığa dokun\nBonus: ","Slide along the deck · Tap a mint fish\nBonus: ")+CozyGameRules.FishName(RequestedFish);
            if(pondBonusPortrait!=null)pondBonusPortrait.texture=fishPortraits[RequestedFish];
            if(pondBonusLabel!=null)pondBonusLabel.text=CozyGameUi.Copy("BONUS +75\n","BONUS +75\n")+Mathf.CeilToInt(12-pondElapsed%12)+CozyGameUi.Copy(" sn","s");
        }
    }
    public void FinishRound()
    {
        if(!running||settled||exiting)return;
        YarnCheckpoint checkpoint=kind==CozyGameKind.Yarn&&clock<=0&&!continued&&!(levelEnded&&level==CozyGameRules.LevelCount-1)?CaptureCheckpoint():null;
        settled=true;running=paused=false;ClearPointer();HideAim();
        int totalReward=kind==CozyGameKind.Yarn?CozyGameRules.YarnCoins(score):CozyGameRules.PondReward(fishCaught);
        int delta=Mathf.Max(0,totalReward-paidCoins);paidCoins=totalReward;
        if(!completionRecorded){CozyGameProgress.Complete(kind,score);completionRecorded=true;}
        CozyGameProgress.RecordScore(kind,roundId,score,DateTime.UtcNow);
        if(checkpoint!=null){checkpoint.paidCoins=paidCoins;checkpoint.completionRecorded=true;CozyGameProgress.SetCheckpoint(checkpoint);}else if(kind==CozyGameKind.Yarn)CozyGameProgress.SetCheckpoint(null);
        if(delta>0)EconomyService.GrantReward(RewardBundle.Coins(delta),kind==CozyGameKind.Yarn?EconomySource.YarnRoute:EconomySource.PondPlay,"cozy:"+roundId+":"+(continued?"continued":"first"),EconomyPersistence.DeferToCaller);
        CatHomeSaveSystem.SaveNow();
        if(motion!=null)motion.Run(0);hud.SetActive(false);pause.SetActive(false);result.SetActive(true);
        resultTitle.text=CozyGameUi.Copy(clock<=0?"Süre doldu!":"Güzel bir tur!",clock<=0?"Time is up!":"A lovely round!");
        resultDetails.text=CozyGameUi.Copy($"{score} puan\n",$"{score} points\n")+(kind==CozyGameKind.Yarn?CozyGameUi.Copy($"Bölüm {level+1}/24 · {solved} sepet · {stars} yıldız",$"Puzzle {level+1}/24 · {solved} baskets · {stars} stars"):CozyGameUi.Copy($"{fishCaught} balık · En iyi seri x{bestPondCombo}\n{pondPerfect} kusursuz · {pondBonuses} bonus balık",$"{fishCaught} fish · Best streak x{bestPondCombo}\n{pondPerfect} perfect · {pondBonuses} bonus fish"))+CozyGameUi.Copy($"\n\n+{delta} jeton · Tura ait toplam {paidCoins}",$"\n\n+{delta} coins · Round total {paidCoins}");
        RefreshRecord();GameAudio.Play(AudioCue.Result,.75f,AudioBus.MiniGame);
        _=CompetitionService.SubmitCozyAsync(kind,roundId+(continued?"-c":""),score,roundStars,roundPearls,fishCaught,pondBasePoints,pondComboSteps,pondPerfect,pondBonuses);
    }
    public void Pause(){if(!running||paused||exiting)return;paused=true;ClearPointer();HideAim();hud.SetActive(false);pause.SetActive(true);if(motion!=null)motion.GetComponent<Animator>().speed=0;SaveContinuedCheckpoint();}
    public void Resume(){if(!paused||exiting)return;paused=false;pause.SetActive(false);hud.SetActive(true);if(motion!=null&&!striking)motion.Run(0);}
    private void OnApplicationPause(bool value){if(value)Pause();}
    private void OnApplicationFocus(bool value){
#if !UNITY_EDITOR
        if(!value)Pause();
#endif
    }
    public void PointerDown(PointerEventData e){if(!running||paused||exiting)return;if(kind==CozyGameKind.Yarn)YarnPointerDown(e);else PondPointerDown(e);}
    public void PointerDrag(PointerEventData e){if(!running||paused||exiting)return;if(kind==CozyGameKind.Yarn)YarnPointerDrag(e);else PondPointerDrag(e);}
    public void PointerUp(PointerEventData e){if(kind==CozyGameKind.Yarn)YarnPointerUp(e);else if(e.pointerId==pointerId)ClearPointer();}
    private bool Point(Vector2 screen,out Vector2 p){p=Vector2.zero;var plane=new Plane(Vector3.up,transform.position+Vector3.up*.24f);Ray ray=gameCamera.ScreenPointToRay(screen);if(!plane.Raycast(ray,out float hit))return false;var local=transform.InverseTransformPoint(ray.GetPoint(hit));p=new Vector2(local.x,local.z);return true;}
    private void ClearPointer(){dragging=pondDragging=false;pointerId=int.MinValue;}
    private static Vector3 World(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
    public void Exit(bool games)
    {
        if(exiting)return;SaveContinuedCheckpoint();exiting=true;running=false;CatRunnerSessionContext.SetReturnToGames(games);canvas.enabled=false;StartCoroutine(Unload());
    }
    private IEnumerator Unload(){RestoreHome();CatRunnerSessionContext.RestoreRequestedNavigation();var op=SceneManager.UnloadSceneAsync(gameObject.scene);while(op!=null&&!op.isDone)yield return null;}
    private void ParkHome(){foreach(var b in FindObjectsByType<Canvas>(FindObjectsInactive.Include))Park(b);foreach(var b in FindObjectsByType<Camera>(FindObjectsInactive.Include))Park(b);foreach(var b in FindObjectsByType<AudioListener>(FindObjectsInactive.Include))Park(b);foreach(var b in FindObjectsByType<Light>(FindObjectsInactive.Include))Park(b);}
    private void Park(Behaviour b){if(b.gameObject.scene!=gameObject.scene&&b.enabled){parked.Add(b);b.enabled=false;}}
    private void RestoreHome(){foreach(var b in parked)if(b!=null)b.enabled=true;parked.Clear();if(gameCamera!=null){gameCamera.enabled=false;var l=gameCamera.GetComponent<AudioListener>();if(l!=null)l.enabled=false;}}
    private void OnDestroy(){RestoreHome();}
}
public sealed class CozyGamePointer:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
{
    public CozyMiniGame owner;
    public void OnPointerDown(PointerEventData e)=>owner?.PointerDown(e);
    public void OnDrag(PointerEventData e)=>owner?.PointerDrag(e);
    public void OnPointerUp(PointerEventData e)=>owner?.PointerUp(e);
}

