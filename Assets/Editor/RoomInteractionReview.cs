using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Full-room button review on an isolated save, at the normal gameplay rate.</summary>
[InitializeOnLoad]
public sealed class RoomInteractionReview : EditorWindow
{
    public const string Root="Docs/QA/ROOM_INTERACTIONS_2026-09-11";
    const string ManualKey="CatHome.RoomInteractionReview.Manual";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public static bool Recording {get;private set;}
    public static string Status {get;private set;}="Hazır";
    Vector2 scroll;
    static RoomInteractionReview()=>EditorApplication.playModeStateChanged+=state=>
    {if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(ManualKey,false))EditorApplication.delayCall+=()=>
        {if(SessionState.GetBool(ManualKey,false))EndSession();};};

    [MenuItem("Tools/Cat Home/Oda Etkileşim Denemesi")]
    public static void Open()=>GetWindow<RoomInteractionReview>("Oda etkileşim denemesi");
    public static void BeginSession()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorQaSession.IsActive)throw new InvalidOperationException("Clean Edit Mode required.");
        Directory.CreateDirectory(Root);
        var keys=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(LivingCatReview.Root+"/preferences-before.json"));
        foreach(var p in keys)
        {
            string key=(string)p["key"];p["existed"]=PlayerPrefs.HasKey(key);
            if((bool)p["isString"])p["text"]=PlayerPrefs.GetString(key);else p["number"]=PlayerPrefs.GetInt(key);
        }
        File.WriteAllText(Root+"/preferences-before.json",keys.ToString());
        var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        File.WriteAllText(Root+"/view-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(new {
            size=(int)window.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(window),timeScale=Time.timeScale,capture=Time.captureFramerate}));
        UiQaTestSession.ResultDirectory=Root;UiQaTestSession.Begin();
    }
    public static void EndSession()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var keys=Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Root+"/preferences-before.json"));
        var language=keys.First(p=>(string)p["key"]==GameLanguageService.PlayerPrefsKey);
        GameLanguageService.SetLanguage((GameLanguage)(int)language["number"]);
        foreach(var p in keys)
        {
            string key=(string)p["key"];
            if(!(bool)p["existed"])PlayerPrefs.DeleteKey(key);
            else if((bool)p["isString"])PlayerPrefs.SetString(key,(string)p["text"]);else PlayerPrefs.SetInt(key,(int)p["number"]);
        }
        PlayerPrefs.Save();UiQaTestSession.End();CatHomeEditPreview.Refresh();
        var view=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Root+"/view-before.json"));
        var window=EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        window.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(window,(int)view["size"]);
        Time.timeScale=(float)view["timeScale"];Time.captureFramerate=(int)view["capture"];
        SessionState.EraseBool(ManualKey);Status="Deneme bitti · kayıt ve tercihler korundu";
        File.WriteAllText(Root+"/preferences-restored.json",keys.ToString());
    }
    public static void ArmManualReview(){RequireCat();SessionState.SetBool(ManualKey,true);Open();}
    void OnInspectorUpdate()=>Repaint();
    void OnGUI()
    {
        EditorGUILayout.LabelField("Oda eşyaları · gerçek oyun düğmeleri",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Ayrı deneme kaydı. Eşyanın yanına gel; oyundaki düğmeyle dene. Dinlenmede Kalk ile çık.",MessageType.Info);
        EditorGUILayout.LabelField(Status,EditorStyles.wordWrappedLabel);
        if(!Application.isPlaying||!EditorQaSession.IsActive)return;
        var cat=Object.FindAnyObjectByType<CatMovement>();if(cat==null)return;
        using(new EditorGUI.DisabledScope(Recording||CatActivity.Active!=null))
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(var a in Products(cat).OrderBy(a=>a.DisplayName))
            {
                EditorGUILayout.BeginHorizontal();EditorGUILayout.LabelField(a.DisplayName);
                if(GUILayout.Button("Yanına getir",GUILayout.Width(100)))PrepareProduct(Key(a));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        if(GUILayout.Button("Dinlenmeden kalk / eylemi durdur")&&CatActivity.Active!=null&&!CatActivity.Active.RequestRestStop())CatActivity.Active.CancelForTransition();
        if(SessionState.GetBool(ManualKey,false)&&GUILayout.Button("Denemeyi bitir · normal salona dön"))EditorApplication.isPlaying=false;
    }
    static CatMovement RequireCat()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Isolated Play required.");
        var cat=Object.FindAnyObjectByType<CatMovement>();if(cat==null)throw new InvalidOperationException("Room not ready.");return cat;
    }
    static string Key(CatActivity a)=>string.IsNullOrEmpty(a.StoreProductId)?a.ActivityId:a.StoreProductId;
    static CatActivity[] Products(CatMovement cat)=>Object.FindObjectsByType<CatActivity>(FindObjectsSortMode.None)
        .Where(a=>a.gameObject.scene==cat.gameObject.scene&&!a.IsRetired&&(HomeStoreService.IsProductInRoomCollection(HomeRoomService.CurrentRoomId,a.StoreProductId)||a.Kind==CatActivityKind.DiningScatter||a.Kind==CatActivityKind.BirdWatch||a.Kind==CatActivityKind.SofaLounge||a.Kind==CatActivityKind.CoffeeTablePlay)).ToArray();
    public static CatActivity PrepareProduct(string id,int side=0)
    {
        var cat=RequireCat();CatActionState.CancelForTransition(cat);UiQaVisualTour.Clear();
        Object.FindAnyObjectByType<EnergySystem>()?.ApplySavedValue(70);Object.FindAnyObjectByType<HungerSystem>()?.ApplySavedValue(35);Object.FindAnyObjectByType<ThirstSystem>()?.ApplySavedValue(35);
        var a=Products(cat).Single(x=>Key(x)==id);var cc=cat.GetComponent<CharacterController>();
        Vector3 previous=cat.transform.position,entry=a.RoutineEntryPoint.position;entry.y=previous.y;
        var points=new List<Vector3>();bool enabled=cc.enabled;cc.enabled=false;
        try
        {
            for(int x=-15;x<=15;x++)for(int z=-15;z<=15;z++)
            {
                var p=new Vector3(entry.x+x*.1f,previous.y,entry.z+z*.1f);
                if(!CatActivityMotion.IsFloorClear(p,Mathf.Max(.27f,CatActivityMotion.ControllerFloorRadius(cat))))continue;
                cat.transform.position=p;if(a.TryGetPromptDistance(cat,out _))points.Add(p);
            }
            if(points.Count==0)throw new InvalidOperationException("No selectable floor: "+id);
            var ordered=points.OrderBy(p=>(p-entry).sqrMagnitude).ToArray();
            Vector3 selected=side==0?ordered[Mathf.Min(3,ordered.Length-1)]:ordered.OrderByDescending(p=>(p-ordered[0]).sqrMagnitude).First();
            var direction=a.transform.position-selected;direction.y=0;
            cat.transform.SetPositionAndRotation(selected,Quaternion.LookRotation(direction.normalized));
        }
        catch{cat.transform.position=previous;throw;}
        finally{cc.enabled=enabled;Physics.SyncTransforms();}
        SelectButton(a);Status=a.DisplayName+" · oyundaki düğme hazır";return a;
    }
    static Button SelectButton(CatActivity a)
    {
        var prompt=Object.FindAnyObjectByType<ActivityPromptController>();
        typeof(ActivityPromptController).GetField("selected",Flags).SetValue(prompt,a);ActivityPromptController.NotifyActivityChanged();
        var candidate=(CatActivity)typeof(ActivityPromptController).GetField("candidate",Flags).GetValue(prompt);
        if(CatActivity.Active!=a&&candidate!=a)throw new InvalidOperationException("The rendered button selects another product: "+a.StoreProductId);
        var button=(Button)typeof(ActivityPromptController).GetField("actionButton",Flags).GetValue(prompt);
        if(!button.isActiveAndEnabled||!button.interactable)throw new InvalidOperationException("Visible action button unavailable: "+a.StoreProductId);
        return button;
    }
    [Serializable] public sealed class Sample {public float time,x,y,z,yaw,dot,delta,turn;public string pose;public bool resting;}
    [Serializable] public sealed class Row
    {
        public string room,id,title,type,kind,breed,error;
        public bool started,completed,exitClear,released;
        public int events,frames;public float seconds,clickMilliseconds,walked,maxFrameTurn,stationaryWalkSeconds;
        public List<Sample> samples=new List<Sample>();
    }
    [Serializable] public sealed class Report {public bool finished;public string error;public List<Row> rows=new List<Row>();}
    public static void Audit(string room,string label,bool record=true,string only=null,int side=0)
    {
        RequireCat();if(Recording)throw new InvalidOperationException("Review running.");
        if(label.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new ArgumentException(nameof(label));
        Recording=true;Object.FindAnyObjectByType<LevelLoader>().StartCoroutine(Drive(room,label,record,only,side));
    }
    static IEnumerator Drive(string room,string label,bool record,string only,int side)
    {
        string folder=Root+"/"+label;Directory.CreateDirectory(folder);var report=new Report();
        var stack=new Stack<IEnumerator>();stack.Push(Run(room,folder,label,record,only,side,report));
        try
        {
            while(stack.Count>0)
            {
                bool moved;object current=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception e){report.error=e.ToString();Status="Kontrol durdu: "+e.Message;break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            report.finished=string.IsNullOrEmpty(report.error);
        }
        finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();File.WriteAllText(folder+"/report.json",JsonUtility.ToJson(report,true));Recording=false;}
    }
    static void Still(string path){var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(path,image.EncodeToPNG());}finally{Object.Destroy(image);}}
    static void ActivityDetail(string path,CatActivity activity,CatMovement cat)
    {
        var camera=Camera.main;if(camera==null)return;
        Vector3 target=cat.GetComponent<CatActivityAnimation>()?.IsNativeJump==true||cat.GetComponent<CatSurfaceTurnMotion>()?.IsTurning==true?cat.transform.position+Vector3.up*.27f:
            activity is SurfaceScatterActivity scatter?Vector3.Lerp(scatter.PerchPoint.position,scatter.CurrentContact,.5f)+Vector3.up*.16f:
            Vector3.Lerp(cat.transform.position,activity.transform.position,.5f)+Vector3.up*.23f;
        Vector3 oldPosition=camera.transform.position;Quaternion oldRotation=camera.transform.rotation;
        var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;var oldRect=camera.rect;float oldAspect=camera.aspect,oldFov=camera.fieldOfView;
        var render=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try
        {
            camera.transform.position=target+new Vector3(-1.30f,.90f,-1.80f);camera.transform.LookAt(target);
            camera.rect=new Rect(0,0,1,1);camera.aspect=16f/9;camera.fieldOfView=38;camera.targetTexture=render;camera.Render();
            RenderTexture.active=render;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally{camera.targetTexture=oldTarget;camera.rect=oldRect;camera.aspect=oldAspect;camera.fieldOfView=oldFov;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);RenderTexture.active=oldActive;render.Release();Object.Destroy(render);Object.Destroy(texture);}
    }
    static IEnumerator Run(string room,string folder,string label,bool record,string only,int side,Report report)
    {
        var loader=Object.FindAnyObjectByType<LevelLoader>();
        UiQaVisualTour.Clear();
        if(loader.CurrentRoom.Id!=room)
        {
            if(!loader.LoadRoom(room))throw new InvalidOperationException("Room load rejected: "+room);
            float deadline=Time.realtimeSinceStartup+30;
            while((loader.IsTransitioning||!loader.IsReady||loader.CurrentRoom.Id!=room)&&Time.realtimeSinceStartup<deadline)yield return null;
            if(!loader.IsReady||loader.IsTransitioning||loader.CurrentRoom.Id!=room)throw new InvalidOperationException("Room load timeout.");
        }
        var cat=RequireCat();var idle=cat.GetComponent<CatIdleBehavior>();bool idleEnabled=idle!=null&&idle.enabled;
        int rate=Time.captureFramerate;float scale=Time.timeScale;CatActivity tracked=null;int completions=0;
        Action<CatActivity> completed=a=>{if(a==tracked)completions++;};CatActivity.Completed+=completed;
        try
        {
            if(idle!=null)idle.enabled=false;Time.timeScale=1;Time.captureFramerate=24;UiQaVisualTour.Clear();
            yield return new WaitForSeconds(.6f);
            var products=Products(cat).OrderBy(Key).ToArray();
            int expectedActions = room == HomeRoomService.LivingRoomId ? 9 : room == HomeRoomService.BathroomId || room == HomeRoomService.BalconyId || room == HomeRoomService.PatioId ? 9 : room == HomeRoomService.BedroomId ? 8 : room==HomeRoomService.KitchenId?11:10;
            if(products.Select(Key).Distinct().Count()!=expectedActions)throw new InvalidOperationException("Room action inventory does not match its interactive products.");
            yield return new WaitForEndOfFrame();Still(folder+"/room.png");
            foreach(var product in products)
            {
                if(only!=null&&Key(product)!=only)continue;
                var row=new Row{room=room,id=Key(product),title=product.DisplayName,type=product.GetType().Name,kind=product.Kind.ToString(),breed=CatBreedService.SelectedBreedId};report.rows.Add(row);
                CatActivity a=null;try{a=PrepareProduct(row.id,side);}catch(Exception e){row.error=e.Message;}
                if(a==null)continue;
                Status=label+" · "+row.title;string frames="Library/RoomInteractionReview/"+label+"/"+row.id;if(record)Directory.CreateDirectory(frames);
                yield return new WaitForSeconds(.25f);yield return new WaitForEndOfFrame();Still(folder+"/"+row.id+"-before.png");
                tracked=a;completions=0;var watch=System.Diagnostics.Stopwatch.StartNew();SelectButton(a).onClick.Invoke();watch.Stop();row.clickMilliseconds=(float)watch.Elapsed.TotalMilliseconds;row.started=a.IsRunning;
                float start=Time.time,deadline=Time.realtimeSinceStartup+45;var previous=cat.transform.position;var rotation=cat.transform.rotation;bool shot=false,spillShot=false;
                var jumpShots=new HashSet<string>();float workStable=0f;
                var hips=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine");var shoulders=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-spine.003");
                while(a.IsRunning&&Time.realtimeSinceStartup<deadline)
                {
                    yield return new WaitForEndOfFrame();var p=cat.transform.position;float delta=Vector3.Distance(previous,p),turn=Quaternion.Angle(rotation,cat.transform.rotation);
                    var pose=cat.GetComponent<CatActivityAnimation>().CurrentPose;row.walked+=delta;row.maxFrameTurn=Mathf.Max(row.maxFrameTurn,turn);
                    var jump=cat.GetComponent<CatActivityAnimation>();
                    var turning=cat.GetComponent<CatSurfaceTurnMotion>();
                    if(!record&&turning!=null&&turning.IsTurning&&turning.CompletedSteps>=2&&jumpShots.Add("surface-turn"))
                    {Still(folder+"/"+row.id+"-surface-turn.png");ActivityDetail(folder+"/"+row.id+"-surface-turn-detail.png",a,cat);}
                    if(!record&&jump.IsNativeJump)
                    {
                        string stage=jump.NativeJumpPhase>=.64f&&jump.NativeJumpPhase<.80f?"landing":
                            jump.NativeJumpPhase>=.35f&&jump.NativeJumpPhase<.50f?"flight":null;
                        if(stage!=null)
                        {
                            string key=(pose==CatActivityPose.TowelJumpDown?"down-":"up-")+stage;
                            if(jumpShots.Add(key)){Still(folder+"/"+row.id+"-"+key+".png");ActivityDetail(folder+"/"+row.id+"-"+key+"-detail.png",a,cat);}
                        }
                    }
                    if(pose==CatActivityPose.Walk&&delta<.003f)row.stationaryWalkSeconds+=Time.deltaTime;
                    row.samples.Add(new Sample{time=Time.time-start,x=p.x,y=p.y,z=p.z,yaw=cat.transform.eulerAngles.y,dot=CatActivityFacing.FacingDot(shoulders.position-hips.position,(shoulders.position+hips.position)*.5f,CatActivityFacing.CameraPosition(cat)),delta=delta,turn=turn,pose=pose.ToString(),resting=a.IsWaitingForRestStop});
                    var livePaw=cat.GetComponent<CatToyContactMotion>();
                    bool physicalPaw=livePaw!=null&&livePaw.Distance<.025f;
                    bool working=a.SupportsContinuousRest?a.IsWaitingForRestStop&&a.RestingSeconds>1.1f:
                        a is PaperSpinActivity recordSpin&&recordSpin.Kind==CatActivityKind.RecordSpin?recordSpin.IsRecordTapping&&physicalPaw:
                        a is KnockOffActivity knock?knock.IsTapping&&physicalPaw:
                        a is CartNudgeActivity cart?cart.IsPushing&&physicalPaw:
                        a is BirdFeederShakeActivity feeder?feeder.IsTapping&&physicalPaw:
                        a is SinkSipActivity sipping?sipping.IsSipping:
                        a is ScratchPostActivity?pose==CatActivityPose.Scratch:
                        a is PantryClimbActivity?pose==CatActivityPose.Sleep||pose==CatActivityPose.Sit||(a.StoreProductId==HomeStoreService.BalconyHerbShelfId&&pose==CatActivityPose.GentleKnead&&!(cat.GetComponent<CatSurfaceTurnMotion>()?.IsTurning??false)&&p.y>.5f):
                        a is LitterDigActivity digging?digging.UsesRaisedPlanter?digging.Phase==CatLitterPhase.Digging:pose==CatActivityPose.Paw:
                        a is GardenYarnChaseActivity yarn?yarn.CatchCount>=1:
                        a is SitLookActivity look?look.GestureBeats>0:
                        a is SurfaceScatterActivity surfaceScatter?surfaceScatter.IsPawing&&surfaceScatter.LastPawDistance<.028f:
                        a is MealTimeActivity?pose==CatActivityPose.Eat&&Time.time-start>2f:
                        a.Kind==CatActivityKind.DaybedWatch?pose==CatActivityPose.Sleep&&a.RestingSeconds>1.3f:
                        a is BallChaseActivity ballPlay&&a.Kind==CatActivityKind.YarnSwat?ballPlay.CatchCount>=1&&ballPlay.Ball.gameObject.activeSelf:pose!=CatActivityPose.Walk&&pose!=CatActivityPose.Hop&&Time.time-start>.5f;
                    workStable=working&&!jump.IsNativeJump?workStable+Time.deltaTime:0f;
                    bool contactCapture=working&&(a is KnockOffActivity||a is CartNudgeActivity||a is BirdFeederShakeActivity||a.Kind==CatActivityKind.RecordSpin);
                    if(!shot&&(contactCapture||workStable>=.25f)){Still(folder+"/"+row.id+"-work.png");if(contactCapture||(room==HomeRoomService.KitchenId&&(a is SurfaceScatterActivity||a is MealTimeActivity))||a.Kind==CatActivityKind.DaybedWatch||a.Kind==CatActivityKind.YarnSwat)ActivityDetail(folder+"/"+row.id+"-detail.png",a,cat);shot=true;}
                    if(!spillShot&&a is KnockOffActivity fallen&&fallen.ContactStrokes==3&&!fallen.IsTapping&&fallen.GlassPivot.GetComponentInChildren<Renderer>().bounds.min.y<.035f)
                    {Still(folder+"/"+row.id+"-fall.png");spillShot=true;}
                    if(!spillShot&&a is SurfaceScatterActivity spilling&&spilling.IsScattering&&spilling.LooseParts.Any(p=>p.position.y<.30f))
                    {Still(folder+"/"+row.id+"-spill.png");spillShot=true;}
                    if(record){var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(frames+"/"+row.frames.ToString("D4")+".jpg",image.EncodeToJPG(90));Object.Destroy(image);}
                    row.frames++;previous=p;rotation=cat.transform.rotation;
                    if(a.IsWaitingForRestStop&&a.RestingSeconds>=2)SelectButton(a).onClick.Invoke();
                }
                row.seconds=Time.time-start;row.events=completions;row.completed=row.started&&!a.IsRunning&&completions==1;
                if(a.IsRunning){row.error="Routine timeout";a.CancelForTransition();}
                yield return new WaitForSeconds(.6f);row.exitClear=CatActivityMotion.IsControllerFloorClear(cat,cat.transform.position);row.released=!cat.IsMovementPhysicallyLocked&&cat.GetComponent<CharacterController>().enabled;
                yield return new WaitForEndOfFrame();Still(folder+"/"+row.id+"-after.png");File.WriteAllText(folder+"/report.json",JsonUtility.ToJson(report,true));
            }
            Status=room+" · "+report.rows.Count(r=>r.completed)+"/"+report.rows.Count+" tamamlandı";
        }
        finally{if(tracked!=null&&tracked.IsRunning)tracked.CancelForTransition();CatActivity.Completed-=completed;if(idle!=null)idle.enabled=idleEnabled;Time.captureFramerate=rate;Time.timeScale=scale;}
    }
}
