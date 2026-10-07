#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Runs only on the copied QA save. Positioning between chapters is test setup;
// recorded chapters use the authored HUD, animation, physics and completion.
public sealed class ActionReadyReviewTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    CatMovement Cat=>Read<CatMovement>(home,"cat");
    BowlInteraction Bowls=>Read<BowlInteraction>(home,"bowls");
    EnergySystem Energy=>Read<EnergySystem>(home,"energy");
    ActivityPromptController Prompt=>Object.FindAnyObjectByType<ActivityPromptController>();
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,F).Invoke(o,args);
    static string Output=>SessionState.GetString("CatHome.QA.ResultDirectory","Temp");
    readonly List<Row> rows=new List<Row>();
    MediaEncoder encoder;
    int frame;
    bool record;
    TMP_Text chapter;
    GameObject chapterRoot;
    FurnitureBodyClearanceTests skinCheck;
    readonly HashSet<string> skinFailures=new HashSet<string>();
    [Serializable] public class Row {public string room,id,title,error,breed;public bool ready,started,completed,released;public int completions,startFrame,endFrame,contacts;public float clickMs;}
    [Serializable] public class Report {public Row[] rows;public int frames;public bool finished;}
    [SetUp] public void Before(){home=new CareAlignmentPolishTests();home.Before();rows.Clear();frame=0;record=false;}
    [TearDown] public void After()
    {
        encoder?.Dispose();encoder=null;
        if(chapterRoot!=null)Object.DestroyImmediate(chapterRoot);
        home?.After();
    }
    void Write(bool finished)=>File.WriteAllText(Path.Combine(Output,"review-"+TestContext.CurrentContext.Test.Name+".json"),JsonUtility.ToJson(new Report{rows=rows.ToArray(),frames=frame,finished=finished},true));
    void Note(string text)=>File.AppendAllText(Path.Combine(Output,"review-progress.txt"),DateTime.UtcNow.ToString("O")+" "+text+"\n");
    IEnumerator Boot(){string breed=SessionState.GetString("CatHome.QA.ReviewBreed","persian");Note("Boot "+breed);yield return (IEnumerator)Call(home,"Home");Note("Home ready");yield return (IEnumerator)Call(home,"Breed",breed);Note("Breed selected");yield return QaBreedReadiness.WaitForSelected(Cat,breed);Note("Boot complete");}
    void Place(Vector3 p,Quaternion q)=>Call(home,"Place",p,q);
    void ReadyNeeds()=>Call(home,"ReadyNeeds");
    static string Id(CatActivity a)=>string.IsNullOrEmpty(a.StoreProductId)?a.Kind.ToString():a.StoreProductId;
    IEnumerator FindStance(CatActivity a,Row row)
    {
        yield return RoomPlayModeSupport.WaitForMovementRelease(Cat);ReadyNeeds();
        a.TryGetStartPose(Cat,out var seed);
        Vector3 origin=a.RoutineEntryPoint!=null?a.RoutineEntryPoint.position:a.transform.position;
        if(seed.Kind==CatActivityStartKind.GroundLaunch)origin=seed.ZoneCentre;
        Vector3 target=seed.ActionTarget;
        if(a is SitLookActivity view)target=view.LookPoint.position;
        if(a is CatEnrichmentActivity toy)target=toy.ContactPoint.position;
        if(target==Vector3.zero)target=a.transform.position;
        if(a is ScratchPostActivity || a.StoreProductId==HomeStoreService.CeramicBowlId)
        {
            Vector3 centre=a is ScratchPostActivity?a.transform.TransformPoint(Read<Vector3>(a,"ropeCenter")):target;
            Vector3 outward=origin-centre;outward.y=0;outward.Normalize();int checkedCount=0;
            foreach(float radius in new[]{.42f,.46f,.50f,.54f,.58f,.62f,.38f,.34f,.30f,.66f,.70f})
            foreach(float angle in new[]{0f,-30f,30f,-60f,60f,-90f,90f,180f})
            foreach(float yaw in a is ScratchPostActivity?new[]{35f,-35f,50f,-50f,20f,-20f,65f,-65f,0f}:new[]{0f})
            {
                var side=Quaternion.Euler(0,angle,0)*outward;var position=centre+side*radius;
                var rotation=Quaternion.LookRotation(-side)*Quaternion.Euler(0,yaw,0);
                Place(position,rotation);
                bool ready=a.CanStartFromPrompt(Cat,out _);
                if(ready)
                {
                    yield return null;yield return null;
                    if(a.CanStartFromPrompt(Cat,out _)){row.ready=true;yield break;}
                }
                if(++checkedCount%24==0)yield return null;
            }
        }
        int probes=0;
        foreach(float radius in new[]{0f,.06f,.12f,.18f,.215f,.3f,.42f,.55f,.7f,.84f,1f,1.2f})
        {
            int count=radius==0?1:16;
            for(int angle=0;angle<count;angle++)
            {
                Vector3 p=origin+Quaternion.Euler(0,angle*360f/count,0)*Vector3.forward*radius;p.y=.05f;
                Vector3 toward=target-p;toward.y=0;if(toward.sqrMagnitude<.0001f)toward=Vector3.forward;
                foreach(float yaw in new[]{0f,-15f,15f,-30f,30f,90f,-90f,180f})
                {
                    var q=Quaternion.AngleAxis(yaw,Vector3.up)*Quaternion.LookRotation(toward);
                    Place(p,q);
                    if(a.TryGetStartPose(Cat,out _)&&a.CanStartFromPrompt(Cat,out _))
                    {
                        yield return null;yield return null;
                        if(a.CanStartFromPrompt(Cat,out _)){row.ready=true;yield break;}
                    }
                    if(++probes%48==0)yield return null;
                }
            }
        }
        row.error="No usable stance around authored entry";
        Note(row.id+" origin="+origin+" target="+target+" root="+a.transform.position+" entry="+a.RoutineEntryPoint.position);
        foreach(var collider in a.GetComponentsInChildren<Collider>())Note("collider "+collider.GetType().Name+" "+collider.name+" "+collider.bounds+" enabled="+collider.enabled+" trigger="+collider.isTrigger);
    }
    Button Select(CatActivity a)
    {
        Call(Bowls,"Update");typeof(ActivityPromptController).GetField("selected",F).SetValue(Prompt,a);
        ActivityPromptController.NotifyActivityChanged();
        var button=Read<Button>(Prompt,"actionButton");
        if(CatActivity.Active!=a&&Read<CatActivity>(Prompt,"candidate")!=a)
            Note("HUD missing "+Id(a)+" position="+Cat.transform.position+" yaw="+Cat.transform.eulerAngles.y+" start="+a.TryGetStartPose(Cat,out _)+" admission="+a.CanStartFromPrompt(Cat,out _)+" busy="+CatActionState.IsBusy(Cat)+" ui="+HomeUiFlow.IsHomeControlBlocked+" world="+Cat.AreWorldActionsBlocked+" energy="+Energy.CurrentEnergy+" food="+Read<HungerSystem>(home,"hunger").CurrentHunger+" water="+Read<ThirstSystem>(home,"thirst").CurrentThirst);
        if(CatActivity.Active!=a)Assert.That(Read<CatActivity>(Prompt,"candidate"),Is.SameAs(a),Id(a)+" selected HUD target");
        Assert.That(button.isActiveAndEnabled&&button.interactable,Is.True,Id(a)+" enabled HUD");return button;
    }
    void Tap(Button b)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
        var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Assert.That(hits.Any(h=>h.gameObject.transform.IsChildOf(b.transform)),Is.True,"Actual HUD hit target");
        ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    void Capture()
    {
        if(!record)return;
        var image=ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            Assert.That(encoder.AddFrame(image),Is.True);frame++;
            var current=rows.LastOrDefault();
            if(current!=null&&(frame==current.startFrame+36||frame==current.startFrame+120))
            {string folder=Path.Combine(Output,"video-frames");Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,frame.ToString("D6")+"-"+current.id+".png"),image.EncodeToPNG());}
        }finally{Object.Destroy(image);}
    }
    IEnumerator Frames(float seconds)
    {for(int i=0;i<Mathf.CeilToInt(seconds*24);i++){yield return new WaitForEndOfFrame();Capture();}}
    void BeginChapter(string title)
    {
        if(!record)return;
        if(chapter==null)
        {
            var canvas=Read<Button>(Prompt,"actionButton").GetComponentInParent<Canvas>();
            chapterRoot=new GameObject("Review chapter panel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));chapterRoot.transform.SetParent(canvas.transform,false);
            var backdrop=chapterRoot.GetComponent<Image>();backdrop.color=new Color(1f,.985f,.95f,.93f);backdrop.raycastTarget=false;
            var panel=(RectTransform)chapterRoot.transform;panel.anchorMin=panel.anchorMax=new Vector2(.5f,1);panel.pivot=new Vector2(.5f,1);panel.anchoredPosition=new Vector2(0,-125);panel.sizeDelta=new Vector2(540,74);
            var go=new GameObject("Review chapter",typeof(RectTransform),typeof(CanvasRenderer));go.transform.SetParent(chapterRoot.transform,false);
            chapter=go.AddComponent<TextMeshProUGUI>();chapter.font=Read<TMP_Text>(Prompt,"actionLabel").font;chapter.fontSize=25;
            chapter.color=new Color32(24,49,70,255);chapter.alignment=TextAlignmentOptions.Center;chapter.raycastTarget=false;
            var rt=(RectTransform)go.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
        }
        chapter.text=title+"\n<size=18>TEST KAYDI · Eşyalar arası geçişler kesildi</size>";
    }
    IEnumerator Observe(CatActivity a)
    {
        string only=SessionState.GetString("CatHome.QA.ReviewOnly","");if(only.Length>0&&!only.Split(',').Contains(Id(a)))yield break;
        var row=new Row{room=HomeRoomService.CurrentRoomId,id=Id(a),title=a.DisplayName,breed=CatBreedService.SelectedBreedId};rows.Add(row);Write(false);
        Note("Find stance "+row.id);yield return FindStance(a,row);Note("Stance "+row.id+" "+row.ready);
        if(!row.ready){Write(false);yield break;}
        // Energy and care must hide an otherwise valid world action.
        if(a.EnergyCost>0){Energy.ApplySavedValue(0);Assert.That(a.CanStartFromPrompt(Cat,out _),Is.False);ReadyNeeds();}
        if(a.RequiredCareNeed!=CatCareNeed.None)
        {
            if(a.RequiredCareNeed==CatCareNeed.Food)Read<HungerSystem>(home,"hunger").ApplySavedValue(100);
            else Read<ThirstSystem>(home,"thirst").ApplySavedValue(100);
            Assert.That(a.CanStartFromPrompt(Cat,out _),Is.False);ReadyNeeds();
        }
        // Negative admission probes may consume this frame's cooperative
        // surface-query budget. Let the normal HUD refresh on a fresh frame.
        yield return null;
        var button=Select(a);BeginChapter(row.title);row.startFrame=frame;yield return Frames(.75f);
        if(!a.CanStartFromPrompt(Cat,out _)){yield return FindStance(a,row);button=Select(a);}
        Action<CatActivity> completed=x=>{if(x==a)row.completions++;};CatActivity.Completed+=completed;
        try
        {
            var watch=System.Diagnostics.Stopwatch.StartNew();Tap(button);watch.Stop();row.clickMs=(float)watch.Elapsed.TotalMilliseconds;row.started=a.IsRunning;
            if(!row.started){row.error="Visible button did not start";yield break;}
            float deadline=Time.realtimeSinceStartup+90,gameDeadline=Time.time+45;
            var trace=new List<string>();
            while(a.IsRunning&&Time.realtimeSinceStartup<deadline&&Time.time<gameDeadline)
            {
                yield return new WaitForEndOfFrame();Capture();
                if(skinCheck!=null&&Time.frameCount%2==0)CheckChairSkin();
                if(a.StoreProductId=="room.armchair")trace.Add(Time.time+","+Cat.transform.position+","+Cat.transform.eulerAngles.y+","+Cat.GetComponent<CatActivityAnimation>().CurrentPose+","+a.IsWaitingForRestStop);
                if(a.IsWaitingForRestStop&&a.RestingSeconds>=2.5f)Tap(Select(a));
            }
            if(trace.Count>0)File.WriteAllLines(Path.Combine(Output,"chair-review-trace.csv"),trace);
            row.completed=!a.IsRunning&&row.completions==1;
            if(a is ScratchPostActivity scratch){row.contacts=scratch.LeftStrokes+scratch.RightStrokes;Assert.That(row.contacts,Is.GreaterThanOrEqualTo(3));}
            if(a.IsRunning)a.CancelForTransition();
            yield return RoomPlayModeSupport.WaitForMovementRelease(Cat);row.released=!Cat.IsMovementPhysicallyLocked;
            if(!row.completed)row.error="Routine did not complete exactly once: "+CatJumpLimbClearance.LastRejection;
            yield return Frames(.75f);row.endFrame=frame;
        }
        finally{CatActivity.Completed-=completed;Write(false);}
    }

    IEnumerator ObserveBowl(string kind)
    {
        ReadyNeeds();Energy.ApplySavedValue(0);
        var setup=Read<BowlInteraction.BowlSetup>(Bowls,kind);setup.Fill();
        Read<BowlInteraction.BowlSetup>(Bowls,kind=="food"?"water":"food").Empty();
        var target=setup.ContactPoint;
        Call(home,"FindCareStance",target,setup.InteractionPoint,new Func<bool>(()=>CatMealHeadMotion.TryPrepareBowlPose(Cat,target,out _)&&CatMealHeadMotion.TryPrepareCareStart(Cat,target,out _)));
        yield return new WaitForSecondsRealtime(.2f);Call(Bowls,"Update");
        var row=new Row{room=HomeRoomService.LivingRoomId,id=kind,title=kind=="food"?"Mama ye":"Su iç",ready=Bowls.HasVisibleAction,startFrame=frame};rows.Add(row);BeginChapter(row.title);
        yield return Frames(.75f);
        UnityEngine.Events.UnityAction completed=()=>row.completions++;setup.OnInteractionCompleted.AddListener(completed);
        try
        {
            Tap(Read<Button>(Bowls,"interactionButton"));row.started=Bowls.IsInteracting;
            float deadline=Time.realtimeSinceStartup+90,gameDeadline=Time.time+20;
            while(Bowls.IsInteracting&&Time.realtimeSinceStartup<deadline&&Time.time<gameDeadline){yield return new WaitForEndOfFrame();Capture();}
            row.completed=!Bowls.IsInteracting&&row.completions==1;row.released=!Cat.IsMovementPhysicallyLocked;
            if(!row.completed)row.error="Care did not complete exactly once";
            yield return Frames(.75f);row.endFrame=frame;Write(false);
        }
        finally{setup.OnInteractionCompleted.RemoveListener(completed);}
    }
    IEnumerator ObserveBed()
    {
        ReadyNeeds();Energy.ApplySavedValue(0);var sleep=Read<SleepInteraction>(home,"sleep");
        var floor=Read<Transform>(sleep,"bedInteractionPoint");var target=sleep.SleepSurface;
        bool ready=false;
        foreach(float radius in new[]{0f,.06f,.12f,.18f,.215f})
        {
            for(int i=0;i<12&&!ready;i++)
            {
                var p=floor.position+Quaternion.Euler(0,i*30,0)*Vector3.forward*radius;var d=target.position-p;d.y=0;
                Place(p,Quaternion.LookRotation(d));ready=sleep.WantsActionButton;
            }
            if(ready)break;
        }
        var row=new Row{room=HomeRoomService.LivingRoomId,id="bed",title="Uyku ve uyanma",ready=ready,startFrame=frame};rows.Add(row);BeginChapter(row.title);
        if(!ready){row.error="Bed unavailable";yield break;}
        yield return null;Call(Bowls,"Update");yield return Frames(.75f);Tap(Read<Button>(Bowls,"interactionButton"));row.started=sleep.IsSleeping;
        float deadline=Time.realtimeSinceStartup+90;float settledAt=-1;bool woke=false;
        while(sleep.IsSleeping&&Time.realtimeSinceStartup<deadline)
        {
            yield return new WaitForEndOfFrame();Capture();
            if(sleep.IsSettledOnBed&&settledAt<0)settledAt=Time.time;
            if(!woke&&settledAt>=0&&Time.time-settledAt>=3){Call(Bowls,"Update");Tap(Read<Button>(Bowls,"interactionButton"));woke=true;}
        }
        row.completed=woke&&!sleep.IsSleeping;row.completions=row.completed?1:0;row.released=!Cat.IsMovementPhysicallyLocked;
        yield return Frames(.75f);row.endFrame=frame;Write(false);
    }

    void CheckChairSkin()
    {
        string label=CatBreedService.SelectedBreedId+"/"+Cat.GetComponent<CatActivityAnimation>().CurrentPose;
        foreach(var spec in new[]{("DEF-spine",.075f),("DEF-spine.003",.075f),("DEF-spine.006",.06f)})
            Call(skinCheck,"Check",Cat,CatBreedVisualFactory.FindDescendant(Cat.transform,spec.Item1).position,spec.Item2,label,skinFailures);
        foreach(string side in new[]{"L","R"})
        {
            var arm=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-upper_arm."+side);var fore=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-forearm."+side);var hand=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-hand."+side);
            for(int i=1;i<=3;i++)
            {Call(skinCheck,"Check",Cat,Vector3.Lerp(arm.position,fore.position,i/3f),.023f,label,skinFailures);Call(skinCheck,"Check",Cat,Vector3.Lerp(fore.position,hand.position,i/3f),.018f,label,skinFailures);}
        }
    }
    void FinishSkinCheck()
    {
        File.WriteAllText(Path.Combine(Output,TestContext.CurrentContext.Test.Name+"-skin.txt"),
            "Broad probe candidates: "+Read<int>(skinCheck,"chairProbeCandidates")+
            "\nActual skin vertices checked: "+Read<int>(skinCheck,"chairVerticesChecked")+
            "\nMaximum measured skin penetration (m): "+Read<float>(skinCheck,"chairMaximumSkinPenetration").ToString("F6",System.Globalization.CultureInfo.InvariantCulture));
        skinCheck.After();skinCheck=null;
    }
    [UnityTest,Timeout(600000)] public IEnumerator Armchair_TenBreeds_CompleteAndKeepActualSkinClear()
    {
        yield return Boot();skinCheck=new FurnitureBodyClearanceTests();skinCheck.Before();skinFailures.Clear();
        try
        {
            foreach(var breed in CatBreedCatalog.Load().Entries)
            {
                yield return (IEnumerator)Call(home,"Breed",breed.Id);yield return QaBreedReadiness.WaitForSelected(Cat,breed.Id);
                var chair=CatActivity.Registered.Single(a=>a.StoreProductId=="room.armchair");yield return Observe(chair);
            }
            Write(true);Assert.That(rows.Count,Is.EqualTo(10));
            Assert.That(rows.Where(r=>!r.ready||!r.started||!r.completed||!r.released).Select(r=>r.id+": "+r.error),Is.Empty);
            Assert.That(skinFailures,Is.Empty,string.Join("\n",skinFailures));
        }
        finally{FinishSkinCheck();}
    }
    [UnityTest,Timeout(600000)] public IEnumerator ScratchPost_TenBreeds_ThreeContactsAndActualSkinClear()
    {
        yield return Boot();
        foreach(string id in new[]{HomeStoreService.ScratchPostId,HomeStoreService.BellCollarId,HomeStoreService.FeatherToyId,HomeStoreService.PlayTunnelId,HomeStoreService.BallBasketId})HomeStoreService.TrySetStored(id,false);
        yield return null;yield return null;
        skinCheck=new FurnitureBodyClearanceTests();skinCheck.Before();typeof(FurnitureBodyClearanceTests).GetField("confirmActualSkin",F).SetValue(skinCheck,true);skinFailures.Clear();
        try
        {
            foreach(var breed in CatBreedCatalog.Load().Entries)
            {
                yield return (IEnumerator)Call(home,"Breed",breed.Id);yield return QaBreedReadiness.WaitForSelected(Cat,breed.Id);
                var post=CatActivity.Registered.Single(a=>a.StoreProductId==HomeStoreService.ScratchPostId);yield return Observe(post);
            }
            Write(true);Assert.That(rows.Count,Is.EqualTo(10));
            Assert.That(rows.Where(r=>!r.ready||!r.started||!r.completed||!r.released).Select(r=>r.breed+": "+r.error),Is.Empty);
            Assert.That(skinFailures,Is.Empty,string.Join("\n",skinFailures));
        }
        finally{FinishSkinCheck();}
    }
    [UnityTest,Timeout(1800000)] public IEnumerator LivingRoom_AllFurnitureAndToys_RealButtonsComplete()
    {
        yield return Boot();record=SessionState.GetBool("CatHome.QA.RecordActionReview",false);
        Time.captureFramerate=24;Time.timeScale=1;GameLanguageService.SetLanguage(GameLanguage.Turkish);
        if(record)encoder=new MediaEncoder(Path.Combine(Output,"CatHome_Salon_Etkilesimleri.mp4"),new VideoTrackAttributes{frameRate=new MediaRational(24),width=(uint)Screen.width,height=(uint)Screen.height,includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        var allCats=HomeStoreService.Products.Where(p=>CatCollectionPolicy.IsCatItem(p.Id)).Select(p=>p.Id).ToArray();
        foreach(string id in new[]{HomeStoreService.ScratchPostId,HomeStoreService.BellCollarId,HomeStoreService.FeatherToyId,HomeStoreService.PlayTunnelId,HomeStoreService.BallBasketId}){Note("Display "+id);HomeStoreService.TrySetStored(id,false);Note("Displayed "+id);}
        yield return null;yield return null;
        var roomActions=CatActivity.Registered.Where(a=>a.gameObject.scene==Cat.gameObject.scene&&!a.IsRetired&&!(a is CatCommandActivity)&&!CatCollectionPolicy.IsCatItem(a.StoreProductId)).OrderBy(Id).ToArray();
        Assert.That(roomActions.Length,Is.EqualTo(9));
        if(SessionState.GetString("CatHome.QA.ReviewOnly","").Length==0){yield return ObserveBowl("food");yield return ObserveBowl("water");yield return ObserveBed();}
        foreach(var a in roomActions)yield return Observe(a);
        foreach(string id in allCats)
        {
            string only=SessionState.GetString("CatHome.QA.ReviewOnly","");if(only.Length>0&&!only.Split(',').Contains(id))continue;
            CatActionState.CancelForTransition(Cat);
            foreach(string other in allCats)if(!HomeStoreService.IsStored(other))HomeStoreService.TrySetStored(other,true);
            Assert.That(HomeStoreService.TrySetStored(id,false),Is.True,id+" display");
            foreach(string filler in new[]{HomeStoreService.FeatherToyId,HomeStoreService.BellCollarId,HomeStoreService.LeashId,HomeStoreService.ToyMouseId,HomeStoreService.ScratchPostId})
                if(filler!=id&&CatCollectionPolicy.DisplayedCount<5)HomeStoreService.TrySetStored(filler,false);
            yield return null;yield return null;
            var a=CatActivity.Registered.Single(x=>x.StoreProductId==id&&!x.IsRetired);yield return Observe(a);
        }
        encoder?.Dispose();encoder=null;
        Write(true);
        Assert.That(rows.Where(r=>!r.ready||!r.started||!r.completed||!r.released).Select(r=>r.id+": "+r.error),Is.Empty);
        if(SessionState.GetString("CatHome.QA.ReviewOnly","").Length==0)Assert.That(rows.Count,Is.EqualTo(28));
    }
    [UnityTest,Timeout(600000)] public IEnumerator Bathroom_TwoSizes_RealButtonsComplete()
    {
        yield return Boot();
        yield return (IEnumerator)Call(home,"Room",HomeRoomService.BathroomId);
        foreach(string breed in new[]{"persian","maine-coon"})
        {
            yield return (IEnumerator)Call(home,"Breed",breed);
            yield return QaBreedReadiness.WaitForSelected(Cat,breed);
            var actions=CatActivity.Registered.Where(a=>a.gameObject.scene==Cat.gameObject.scene&&!a.IsRetired&&!(a is CatCommandActivity)&&a.IsUnlocked).OrderBy(Id).ToArray();
            Assert.That(actions.Length,Is.EqualTo(9));
            foreach(var action in actions)yield return Observe(action);
        }
        Write(true);
        Assert.That(rows.Count,Is.EqualTo(18));
        Assert.That(rows.Where(r=>!r.ready||!r.started||!r.completed||!r.released).Select(r=>r.breed+"/"+r.id+": "+r.error),Is.Empty);
    }
    [UnityTest,Timeout(600000)] public IEnumerator Kitchen_TwoSizes_RealButtonsComplete()
    {
        yield return Boot();
        yield return (IEnumerator)Call(home,"Room",HomeRoomService.KitchenId);
        foreach(string breed in new[]{"persian","maine-coon"})
        {
            yield return (IEnumerator)Call(home,"Breed",breed);
            yield return QaBreedReadiness.WaitForSelected(Cat,breed);
            var actions=CatActivity.Registered.Where(a=>a.gameObject.scene==Cat.gameObject.scene&&!a.IsRetired&&!(a is CatCommandActivity)&&a.IsUnlocked).OrderBy(Id).ToArray();
            Assert.That(actions.Length,Is.EqualTo(11));
            foreach(var action in actions)yield return Observe(action);
        }
        Write(true);
        Assert.That(rows.Count,Is.EqualTo(22));
        Assert.That(rows.Where(r=>!r.ready||!r.started||!r.completed||!r.released).Select(r=>r.breed+"/"+r.id+": "+r.error),Is.Empty);
    }
    [UnityTest,Timeout(180000)] public IEnumerator Bathroom_WallLitter_TwoSizes_RealButtonsComplete()
    {
        yield return Boot();
        yield return (IEnumerator)Call(home,"Room",HomeRoomService.BathroomId);
        foreach(string breed in new[]{"persian","maine-coon"})
        {
            yield return (IEnumerator)Call(home,"Breed",breed);
            yield return QaBreedReadiness.WaitForSelected(Cat,breed);
            yield return Observe(CatActivity.Registered.Single(a=>a.StoreProductId==HomeStoreService.BathroomLitterBoxId&&!a.IsRetired));
        }
        Write(true);
        Assert.That(rows.Count,Is.EqualTo(2));
        Assert.That(rows.Where(r=>!r.ready||!r.started||!r.completed||!r.released).Select(r=>r.breed+": "+r.error),Is.Empty);
    }
    [UnityTest,Timeout(1200000)] public IEnumerator EightRooms_EnabledActionsStartWithoutAdvice()
    {
        yield return Boot();
        foreach(var room in HomeRoomService.Rooms)
        {
            yield return (IEnumerator)Call(home,"Room",room.Id);yield return QaBreedReadiness.WaitForSelected(Cat,CatBreedService.SelectedBreedId);
            foreach(var a in CatActivity.Registered.Where(a=>a.gameObject.scene==Cat.gameObject.scene&&!a.IsRetired&&!(a is CatCommandActivity)&&a.IsUnlocked).OrderBy(Id).ToArray())
            {
                var row=new Row{room=room.Id,id=Id(a),title=a.DisplayName};rows.Add(row);yield return FindStance(a,row);
                if(row.ready)
                {
                    var p=Cat.transform.position;var q=Cat.transform.rotation;float energy=Energy.CurrentEnergy;
                    Assert.That(a.CanStartFromPrompt(Cat,out _),Is.True);
                    Assert.That(Cat.transform.position,Is.EqualTo(p));Assert.That(Cat.transform.rotation,Is.EqualTo(q));Assert.That(Energy.CurrentEnergy,Is.EqualTo(energy));
                    Tap(Select(a));row.started=a.IsRunning;yield return null;a.CancelForTransition();
                    yield return RoomPlayModeSupport.WaitForMovementRelease(Cat);row.released=!Cat.IsMovementPhysicallyLocked;
                }
                else
                {
                    Assert.That(a.CanStartFromPrompt(Cat,out _),Is.False);
                    typeof(ActivityPromptController).GetField("selected",F).SetValue(Prompt,a);ActivityPromptController.NotifyActivityChanged();
                    Assert.That(Read<CatActivity>(Prompt,"candidate"),Is.Not.SameAs(a),"Unavailable action must not be offered");
                    row.error="No ready stance in sampled area; HUD correctly hides this action";
                }
                Write(false);
            }
        }
        Write(true);Assert.That(rows.Count(r=>r.ready),Is.GreaterThanOrEqualTo(61));
        Assert.That(rows.Where(r=>r.ready&&(!r.started||!r.released)).Select(r=>r.room+"/"+r.id+": "+r.error),Is.Empty);
    }
}
#endif

