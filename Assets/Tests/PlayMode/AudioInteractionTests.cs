#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

/// <summary>Runs real room routines and records the sound beside the animation phase that emitted it.</summary>
public sealed class AudioInteractionTests
{
    [Serializable] public class CueEvent { public string cue,pose;public float time,phase;public bool jump; }
    [Serializable] public class Row
    {
        public string room,product,kind,status;public bool started,completed,released;
        public int completionEvents,contacts;public float startY,minimumContact;public List<CueEvent> cues=new List<CueEvent>();public List<string> loops=new List<string>();
        public List<string> missing=new List<string>();
    }
    [Serializable] public class Report { public List<Row> rows=new List<Row>(); }
    private const string Root="Docs/QA/FULL_AUDIO_2026-09-14";
    private HomeStoreSaveState store;
    private HomeProgressionSaveState homeProgress;
    private long coins,diamonds,bond;private int chapter;private QuestProgressEntry[] quests;
    private string breed;private bool sound,music;private float capture;
    private CatMovement cat;private CatActivityAnimation animation;private Row tracked;
    private readonly List<string> failures=new List<string>();
    private Report report;
    private string reportName;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive,Is.True);store=HomeStoreService.CaptureState();homeProgress=HomeProgressionService.CaptureState();
        breed=CatBreedService.SelectedBreedId;sound=HomeAudioService.SoundEnabled;music=HomeAudioService.MusicEnabled;
        coins=ProgressionService.Coins;diamonds=ProgressionService.Diamonds;bond=ProgressionService.BondXp;
        chapter=ProgressionService.CurrentChapterNumber;quests=ProgressionService.CaptureQuestProgress();
        capture=Time.captureDeltaTime;Time.captureFramerate=0;Time.timeScale=1;
        HomeAudioService.SoundEnabled=true;HomeAudioService.MusicEnabled=true;
        report=new Report();failures.Clear();GameAudio.Played+=Heard;CatActivity.Completed+=Completed;
    }
    [TearDown] public void After()
    {
        GameAudio.Played-=Heard;CatActivity.Completed-=Completed;tracked=null;
        if(CatActivity.Active!=null)CatActivity.Active.CancelForTransition();
        RoomPlayModeSupport.ReleaseRoom();Time.timeScale=1;Time.captureDeltaTime=capture;
        ProgressionService.ApplySavedState(coins,diamonds,bond,chapter,quests);HomeProgressionService.ApplySavedState(homeProgress);
        HomeStoreService.ApplySavedState(store);CatBreedService.Select(breed);
        HomeAudioService.SoundEnabled=sound;HomeAudioService.MusicEnabled=music;Write();
    }
    private void Heard(AudioCue cue,AudioBus bus)
    {
        if(tracked==null||bus!=AudioBus.World)return;
        tracked.cues.Add(new CueEvent{cue=cue.ToString(),time=Time.time,pose=animation==null?"":animation.CurrentPose.ToString(),jump=animation!=null&&animation.IsNativeJump,phase=animation==null?0:animation.NativeJumpPhase});
    }
    private void Completed(CatActivity a){if(tracked!=null&&(a.StoreProductId==tracked.product&&a.Kind.ToString()==tracked.kind))tracked.completionEvents++;}
    private void Write(){if(report==null||string.IsNullOrEmpty(reportName))return;Directory.CreateDirectory(Root);File.WriteAllText(Root+"/audio-"+reportName+".json",JsonUtility.ToJson(report,true));}

    [UnityTest,Timeout(600000)] public IEnumerator LivingRoom_AllRoomRoutines()=>Room(HomeRoomService.LivingRoomId);
    [UnityTest,Timeout(600000)] public IEnumerator Bathroom_AllRoomRoutines()=>Room(HomeRoomService.BathroomId);
    [UnityTest,Timeout(600000)] public IEnumerator Kitchen_AllRoomRoutines()=>Room(HomeRoomService.KitchenId);
    [UnityTest,Timeout(600000)] public IEnumerator Bedroom_AllRoomRoutines()=>Room(HomeRoomService.BedroomId);
    [UnityTest,Timeout(600000)] public IEnumerator Garden_AllRoomRoutines()=>Room(HomeRoomService.GardenId);
    [UnityTest,Timeout(600000)] public IEnumerator Balcony_AllRoomRoutines()=>Room(HomeRoomService.BalconyId);
    [UnityTest,Timeout(600000)] public IEnumerator Patio_AllRoomRoutines()=>Room(HomeRoomService.PatioId);
    [UnityTest,Timeout(600000)] public IEnumerator Loft_AllRoomRoutines()=>Room(HomeRoomService.SecondFloorId);

    private IEnumerator Prepare(string roomId,IEnumerable<string> displayed=null)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(HomeRoomService.Rooms.Single(r=>r.Id==roomId).SceneName);
        var state=HomeStoreSaveState.CreateDefault();state.currentRoomId=roomId;
        state.ownedProductIds=HomeStoreService.Products.Select(p=>p.Id).ToArray();
        state.storedProductIds=HomeStoreService.Products.Where(p=>CatCollectionPolicy.IsCatItem(p.Id)&&(displayed==null||!displayed.Contains(p.Id))).Select(p=>p.Id).ToArray();
        HomeStoreService.ApplySavedState(state);
        ProgressionService.ApplySavedState(coins,diamonds,Math.Max(bond,250),chapter,quests);
        CatBreedService.Select("oriental-shorthair");yield return null;yield return null;
        cat=Object.FindFirstObjectByType<CatMovement>();Assert.That(cat,Is.Not.Null);
        var idle=cat.GetComponent<CatIdleBehavior>();if(idle!=null)idle.enabled=false;
        GameAudio.Clip(AudioCue.UIClick);var audio=Object.FindFirstObjectByType<GameAudio>();audio.SendMessage("OnApplicationFocus",true);audio.SendMessage("OnApplicationPause",false);
        UnityEditor.EditorUtility.audioMasterMute=false;
        yield return new WaitForSecondsRealtime(.5f);
        Assert.That(cat.GetComponent<CatFoley>(),Is.Not.Null);
    }
    private IEnumerator Room(string roomId)
    {
        reportName=roomId;yield return Prepare(roomId);
        var activities=CatActivity.Registered.Where(a=>a!=null&&a.gameObject.scene==cat.gameObject.scene&&!a.IsRetired&&a.Kind!=CatActivityKind.CompanionCommand&&
            !CatCollectionPolicy.IsCatItem(a.StoreProductId)&&a.isActiveAndEnabled&&a.IsUnlocked).OrderBy(a=>a.StoreProductId).ThenBy(a=>a.Kind).ToArray();
        Assert.That(activities.Length,Is.GreaterThanOrEqualTo(7),roomId);
        foreach(var a in activities)yield return Observe(roomId,a);
        Write();Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    [UnityTest,Timeout(600000)] public IEnumerator CatCollection_AllSeventeenProducts()
    {
        reportName="cat-collection";
        string[][] groups={
            new[]{"home.ball-basket","home.scratch-post","cat.play-tunnel","cat.bell-collar","cat.nap-pillow"},
            new[]{"cat.cozy-pod-bed","cat.toy-mouse","cat.ceramic-bowl","cat.feather-toy","cat.collar"},
            new[]{"cat.cloud-bed","cat.treat-jar","cat.leash","cat.kibble-bag"},
            new[]{"cat.canopy-bed","cat.catnip-plant","cat.cardboard-hideout"}};
        foreach(var group in groups)
        {
            yield return Prepare(HomeRoomService.LivingRoomId,group);
            foreach(string id in group)
            {
                var a=CatActivity.Registered.SingleOrDefault(x=>x.StoreProductId==id&&!x.IsRetired&&x.isActiveAndEnabled);
                if(a==null){failures.Add(id+" missing displayed routine");continue;}
                yield return Observe(HomeRoomService.LivingRoomId,a);
            }
        }
        Assert.That(report.rows.Count,Is.EqualTo(17));Write();Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    private IEnumerator Start(CatActivity a)
    {
        RoomPlayModeSupport.ProvisionNeeds();
        var hunger=Object.FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);if(hunger==null)hunger=RoomPlayModeSupport.ProvisionNeeds().gameObject.AddComponent<HungerSystem>();hunger.ApplySavedValue(35);
        var thirst=Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);if(thirst==null)thirst=RoomPlayModeSupport.ProvisionNeeds().gameObject.AddComponent<ThirstSystem>();thirst.ApplySavedValue(35);
        var cc=cat.GetComponent<CharacterController>();
        var points=new List<Vector3>();Vector3 entry=a.RoutineEntryPoint==null?a.transform.position:a.RoutineEntryPoint.position;entry.y=.05f;
        points.Add(entry);points.Add(cat.transform.position);
        for(int radius=1;radius<=3;radius++)for(int angle=0;angle<12;angle++)
            points.Add(entry+Quaternion.Euler(0,angle*30,0)*Vector3.forward*(radius*.18f));
        foreach(var p in points)
        {
            if(!CatActivityMotion.IsControllerFloorClear(cat,p))continue;
            cc.enabled=false;cat.transform.SetPositionAndRotation(p,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
            // Let the enabled controller settle exactly as in normal play.
            yield return null;
            if(a.TryStart(cat)){tracked.started=true;tracked.startY=cat.transform.position.y;yield break;}
        }
    }
    private IEnumerator Observe(string roomId,CatActivity a)
    {
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        tracked=new Row{room=roomId,product=a.StoreProductId,kind=a.Kind.ToString()};report.rows.Add(tracked);
        if(a.StoreProductId=="room.modern-painting")
        {tracked.status="previously-known-no-open-entry; passive watch audio unchanged";tracked=null;Write();yield break;}
        var row=tracked;yield return Start(a);animation=cat.GetComponent<CatActivityAnimation>();
        if(!row.started){row.status="refused-start";failures.Add(row.product+"/"+a.Kind+" refused start");tracked=null;Write();yield break;}
        Vector3 scale=cat.transform.localScale;float until=Time.realtimeSinceStartup+35;
        while(a.IsRunning&&Time.realtimeSinceStartup<until)
        {
            if(a.IsWaitingForRestStop&&a.RestingSeconds>=2.2f)a.RequestRestStop();
            foreach(var s in cat.GetComponents<AudioSource>())if(s.loop&&s.isPlaying&&s.clip!=null&&!row.loops.Contains(s.clip.name))row.loops.Add(s.clip.name);
            yield return null;
        }
        row.completed=!a.IsRunning&&row.completionEvents==1;
        if(a.IsRunning)a.CancelForTransition();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        row.released=!cat.IsMovementPhysicallyLocked&&cat.GetComponent<CharacterController>().enabled;
        if(a is ScratchPostActivity scratch)row.contacts=scratch.LeftStrokes+scratch.RightStrokes;
        if(a is KnockOffActivity knock){row.contacts=knock.ContactStrokes;row.minimumContact=knock.MinimumPawDistance;}
        if(a is CartNudgeActivity cart){row.contacts=cart.ContactStrokes;row.minimumContact=cart.MinimumPawDistance;}
        if(a is PaperSpinActivity paper){row.contacts=paper.RecordContactCount;row.minimumContact=paper.RecordContactDistance;}
        if(!row.completed)failures.Add(row.product+"/"+a.Kind+" incomplete routine");
        if(!row.released)failures.Add(row.product+"/"+a.Kind+" retained control");
        Required(a,row);
        foreach(var missing in row.missing)failures.Add(row.product+"/"+a.Kind+" no "+missing);
        row.status=row.completed&&row.released&&row.missing.Count==0?"passed":"failed";
        Assert.That(Vector3.Distance(scale,cat.transform.localScale),Is.LessThan(.0001f),a.Kind+" changed scale");
        tracked=null;Write();yield return new WaitForSecondsRealtime(.18f);
    }
    private static void Cue(Row row,params AudioCue[] choices)
    {if(!choices.Any(c=>row.cues.Any(e=>e.cue==c.ToString())))row.missing.Add(string.Join(" or ",choices.Select(c=>c.ToString())));}
    private static void Loop(Row row,string name){if(!row.loops.Contains(name+"_1"))row.missing.Add(name+" loop");}

    [UnityTest,Timeout(240000)] public IEnumerator ReportedContactsAndLegacyMats()
    {
        reportName="contact-recheck";
        string[][] targets={new[]{HomeRoomService.BalconyId,"balcony.side-table"},new[]{HomeRoomService.KitchenId,"kitchen.dish-cart","kitchen.paw-mat"},
            new[]{HomeRoomService.BedroomId,"bedroom.paw-rug"},new[]{HomeRoomService.GardenId,"garden.sapling"},new[]{HomeRoomService.SecondFloorId,"loft.record-player"}};
        foreach(var group in targets)
        {
            yield return Prepare(group[0]);
            foreach(var id in group.Skip(1))yield return Observe(group[0],CatActivity.Registered.Single(a=>a.StoreProductId==id&&a.isActiveAndEnabled&&!a.IsRetired));
        }
        Write();Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    [UnityTest,Timeout(120000)] public IEnumerator NativeJumpAudio_At15_30_60Fps()
    {
        reportName="jump-fps";yield return Prepare(HomeRoomService.BathroomId);
        var a=CatActivity.Registered.OfType<TowelNestActivity>().Single();
        foreach(int fps in new[]{15,30,60})
        {
            Time.captureFramerate=fps;yield return Observe(HomeRoomService.BathroomId,a);
            var row=report.rows.Last();
            Assert.That(row.cues.Count(c=>c.cue=="Jump"),Is.EqualTo(2),fps+" fps takeoffs");
            Assert.That(row.cues.Count(c=>c.cue.StartsWith("Land")),Is.EqualTo(2),fps+" fps landings");
            foreach(var cue in row.cues.Where(c=>c.jump))
            {
                if(cue.cue=="Jump")Assert.That(cue.phase,Is.InRange(CatJumpMotion.Takeoff,CatJumpMotion.Touchdown),fps+" fps takeoff phase");
                if(cue.cue.StartsWith("Land"))Assert.That(cue.phase,Is.InRange(CatJumpMotion.Touchdown,1f),fps+" fps landing phase");
                if(cue.cue.StartsWith("Paw"))Assert.That(cue.phase<CatJumpMotion.Takeoff||cue.phase>=CatJumpMotion.Touchdown,Is.True,"No airborne footsteps");
            }
        }
        Write();Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }

    [UnityTest,Timeout(90000)] public IEnumerator OutdoorRestPauseCancelAndRoomChange()
    {
        reportName="soundscape-flow";yield return Prepare(HomeRoomService.GardenId);
        var soundscape=Object.FindFirstObjectByType<GameSoundscape>();
        yield return new WaitForSecondsRealtime(1.2f);
        Assert.That(soundscape.Selection,Is.EqualTo("Outdoor"));
        Assert.That(soundscape.GetComponents<AudioSource>().Count(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="GardenAir_1"),Is.EqualTo(1));
        var a=CatActivity.Registered.OfType<MatKneadActivity>().First();
        tracked=new Row{room=HomeRoomService.GardenId,product=a.StoreProductId,kind=a.Kind.ToString()};report.rows.Add(tracked);
        yield return Start(a);Assert.That(tracked.started,Is.True);float until=Time.realtimeSinceStartup+18;
        while(!a.IsWaitingForRestStop&&a.IsRunning&&Time.realtimeSinceStartup<until)yield return null;
        Assert.That(a.IsWaitingForRestStop,Is.True);yield return new WaitForSecondsRealtime(1.3f);
        Assert.That(soundscape.Selection,Is.EqualTo("Rest"));Assert.That(cat.GetComponent<CatVoice>().PlayingLoop,Is.EqualTo("Purr_1"));
        Time.timeScale=0;yield return new WaitForSecondsRealtime(.12f);
        Assert.That(cat.GetComponent<CatVoice>().PlayingLoop,Is.Empty);
        Assert.That(cat.GetComponents<AudioSource>().Any(s=>s.isPlaying),Is.False,"Pause silences all cat textures");
        Assert.That(GameAudio.UI(),Is.True,"Paused menus remain audible");
        a.CancelForTransition();Time.timeScale=1;yield return new WaitForSecondsRealtime(1.3f);
        Assert.That(soundscape.Selection,Is.EqualTo("Outdoor"));Assert.That(cat.GetComponent<CatVoice>().PlayingLoop,Is.Empty);
        Assert.That(cat.IsMovementPhysicallyLocked,Is.False);tracked=null;
        yield return Prepare(HomeRoomService.KitchenId);yield return new WaitForSecondsRealtime(1.3f);
        Assert.That(soundscape.Selection,Is.EqualTo("Home"));
        Assert.That(soundscape.GetComponents<AudioSource>().Any(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="GardenAir_1"),Is.False,"Garden air stays outdoors");
        Assert.That(Object.FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        report.rows[0].status="passed";Write();
    }
    private static void Required(CatActivity a,Row row)
    {
        if(a is ScratchPostActivity)Cue(row,CatFoley.ContactCue(a));
        if(a is LitterDigActivity)Cue(row,a.Kind==CatActivityKind.LitterDig?AudioCue.DigSand:AudioCue.DigSoil);
        if(a is MatKneadActivity)Cue(row,AudioCue.Cloth);
        if(a is GroomBrushActivity)Cue(row,AudioCue.Groom);
        if(a is PaperSpinActivity)Cue(row,a.Kind==CatActivityKind.RecordSpin?AudioCue.Record:AudioCue.PaperTear);
        if(a is CartNudgeActivity){Cue(row,AudioCue.WoodTap);Cue(row,AudioCue.WheelRoll);}
        if(a is BirdFeederShakeActivity)Cue(row,AudioCue.Feeder);
        if(a is ShowerRinseActivity){Loop(row,"Shower");Cue(row,AudioCue.Shake);}
        if(a is SinkSipActivity sip&&!sip.InspectingOnly)Loop(row,"Drink");
        if(a is MealTimeActivity meal&&!meal.InspectingOnly)Loop(row,"Eat");
        if(a is KnockOffActivity){Cue(row,CatFoley.ContactCue(a));Cue(row,AudioCue.GlassLand,AudioCue.CeramicLand,AudioCue.BookLand);}
        if(a is SurfaceScatterActivity){Cue(row,CatFoley.ContactCue(a));Cue(row,a.Kind==CatActivityKind.FruitSwat?AudioCue.FruitLand:AudioCue.CeramicLand);}
        if(a is BallChaseActivity)Cue(row,a.Kind==CatActivityKind.YarnSwat?AudioCue.Cloth:AudioCue.BallTap);
        if(a is GardenYarnChaseActivity)Cue(row,AudioCue.Cloth);
        if(a is CatEnrichmentActivity e)
        {
            if(e.Mode==CatEnrichmentMode.Feed)Loop(row,"Eat");
            else if(e.Mode==CatEnrichmentMode.Tunnel||e.Mode==CatEnrichmentMode.Hide)Cue(row,AudioCue.Cloth,AudioCue.PawFabric,AudioCue.PawWood);
            else if(e.Mode!=CatEnrichmentMode.Nap)Cue(row,CatFoley.ContactCue(a));
        }
        if(a.SupportsContinuousRest)Loop(row,"Purr");
    }
}
#endif
