using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Bounded local review commands; native tests always use a copied player save.</summary>
[InitializeOnLoad]
public static class DirectJumpTurnReview
{
    public const string Root="Docs/QA/DIRECT_JUMP_TURNS_2026-09-15";
    const string Running="CatHome.DirectJumpTurn.Running", Restore="CatHome.DirectJumpTurn.Restore";
    static readonly TestRunnerApi api;
    static double nextPoll,restoreAfter;
    [Serializable] sealed class Pref {public string key,text;public bool existed,isString;public int number;}
    [Serializable] sealed class Prefs {public bool audioMuted;public Pref[] preferences;}
    [Serializable] sealed class PrefResult {public int checkedCount,matchedCount;public bool editorMuteMatches;public string[] mismatches;}
    [Serializable] sealed class SceneInfo {public string name;public bool dirty;}
    [Serializable] sealed class SourceInfo {public string name,clip;public bool playing;public float volume,time;}
    [Serializable] sealed class State
    {
        public bool play,qa,compiling,focused,editorMuted,building,recording,soundEnabled,musicEnabled;
        public string copy,title,selection;
        public int cats,listeners,audioSystems,captureFramerate;
        public float musicTarget,musicLevel,timeScale;
        public SceneInfo[] scenes;public SourceInfo[] sources;
    }
    static DirectJumpTurnReview()
    {
        api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results());
        EditorApplication.update+=Poll;
    }
    static void Poll()
    {
        if(EditorApplication.timeSinceStartup<nextPoll||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        nextPoll=EditorApplication.timeSinceStartup+.5;
        if(SessionState.GetBool(Restore,false)&&!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if(restoreAfter==0){restoreAfter=EditorApplication.timeSinceStartup+3;return;}
            if(EditorApplication.timeSinceStartup<restoreAfter)return;
            FullAudioReview.EndTests();
            CatHomeAuthoringWorkspace.OpenFullHomePreview();
            SessionState.SetBool(Restore,false);SessionState.SetBool(Running,false);restoreAfter=0;
            VerifyPreferences();
            Inspect("after-tests.json");
        }
        var path=Path.Combine(Root,"request.txt");if(!File.Exists(path))return;
        string command=File.ReadAllText(path).Trim();File.Delete(path);
        try
        {
            if(command=="inspect")Inspect("editor-state.json");
            else if(command=="inspect-action")InspectAction();
            else if(command=="verify-preferences")VerifyPreferences();
            else if(command=="verify-closed-save")
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorQaSession.IsActive)throw new InvalidOperationException("End the copied preview first.");
                var paths=new[]{CatHomeSaveSystem.SaveFilePath,CatHomeSaveSystem.RecoveryFilePath};
                var before=paths.Select(File.ReadAllBytes).ToArray();
                CatHomeSaveSystem.SaveNow(true);CatHomeSaveSystem.SaveForSuspension();
                bool unchanged=paths.Select((p,i)=>File.ReadAllBytes(p).SequenceEqual(before[i])).All(v=>v);
                File.WriteAllText(Path.Combine(Root,"closed-save-regression.json"),"{\"lateSaveCallbacksPreserveRealFiles\":"+unchanged.ToString().ToLowerInvariant()+"}");
                if(!unchanged)throw new InvalidOperationException("A closed preview wrote into the player save.");
            }
            else if(command=="tests")RunTests();
            else if(command=="body")RunSelected(new[]{"FurnitureBodyClearanceTests.AllJumpingFurniture_KeepActualBodyOutsideFurniture"});
            else if(command=="sofa-body")RunSelected(new[]{"FurnitureBodyClearanceTests.LivingSofa_TenBreeds_KeepActualBodyOutsideFurniture"});
            else if(command=="release")RunRelease();
            else if(command=="edit-tests")RunSelected(new[]{"NativeJumpSourceTests"},TestMode.EditMode);
            else if(command=="water-release")RunSelected(new[]{"HomeContactFacingTests.VanitySip_TenBreeds_KeepTheirSupportedPerch_AndPointIntoTheActualWaterTarget","JumpContinuityTests.BirdBath_TenBreeds_LeavesDrinkContinuously","JumpContinuityTests.AllRooms_JumpingActivities_UseDirectTurns","FurnitureBodyClearanceTests.AllJumpingFurniture_KeepActualBodyOutsideFurniture"});
            else if(command=="fountain")RunSelected(new[]{"JumpContinuityTests.Fountain_TenBreeds_LeavesDrinkContinuously"});
            else if(command=="shelves")RunSelected(new[]{"FurnitureBodyClearanceTests.NarrowShelves_TenBreeds_KeepBodyOutsideTheWall","KitchenBedroomSupportedMotionTests.NarrowPerches_TenBreeds_KeepAllPawsOnTheRealSurface","JumpContinuityTests.ReportedPerchTurns_TenBreeds_PreserveLegAnatomy"});
            else if(command=="planters")RunSelected(new[]{"FurnitureBodyClearanceTests.Planters_TenBreeds_TurnAndLeaveWithoutBodyClipping","JumpContinuityTests.RaisedPlanters_TenBreeds_UseOriginalJumpAndContinuousLanding"});
            else if(command=="baseline")RunSelected(new[]{"JumpContinuityTests.EveryRoom_AllJumpingFurniture_HoldsLandingAndReleasesContinuously"});
            else if(command=="living")RunSelected(new[]{"JumpContinuityTests.LivingSurfaces_TenBreedsThreeRates_TurnDirectly","GameAudioTests.CoffeeTable_FloorImpactAtContact_ThreeFrameRates","GameAudioTests.CoffeeTable_PausedOrCancelledFallIsQuiet_ThenReplayWorks","JumpContinuityTests.Armchair_TenBreedsAndFrameRates_DirectEntryAndExit"});
            else if(command=="sofa")RoomInteractionReview.PrepareProduct("SofaLounge");
            else if(command=="coffee")RoomInteractionReview.PrepareProduct("CoffeeTablePlay");
            else if(command=="demo-sofa")RoomInteractionReview.Audit(HomeRoomService.LivingRoomId,"direct-sofa-20260915",false,"SofaLounge");
            else if(command=="demo-coffee")RoomInteractionReview.Audit(HomeRoomService.LivingRoomId,"direct-coffee-20260915",false,"CoffeeTablePlay");
            else if(command=="demo-chair")Demo(HomeRoomService.LivingRoomId,"chair","room.armchair");
            else if(command=="demo-bathroom")Demo(HomeRoomService.BathroomId,"bathroom","bathroom.towel-storage");
            else if(command=="demo-kitchen")Demo(HomeRoomService.KitchenId,"kitchen","kitchen.fruit-basket");
            else if(command=="demo-bedroom")Demo(HomeRoomService.BedroomId,"bedroom","bedroom.star-canopy");
            else if(command=="demo-garden")Demo(HomeRoomService.GardenId,"garden","garden.flower-pots");
            else if(command=="demo-balcony")Demo(HomeRoomService.BalconyId,"balcony","balcony.herb-shelf");
            else if(command=="demo-hanging")Demo(HomeRoomService.BalconyId,"hanging","balcony.hanging-chair");
            else if(command=="demo-patio")Demo(HomeRoomService.PatioId,"patio","patio.herb-trough");
            else if(command=="demo-fountain")Demo(HomeRoomService.PatioId,"fountain","patio.water-fountain");
            else if(command=="demo-loft")Demo(HomeRoomService.SecondFloorId,"loft","loft.tall-bookcase");
            else if(command=="review")Begin();
            else if(command=="home"){if(!EditorQaSession.IsActive||!EditorApplication.isPlaying)throw new InvalidOperationException("Isolated Play required.");var title=UnityEngine.Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);if(TitleScreen.IsShowing)((UnityEngine.UI.Button)typeof(TitleScreen).GetField("playButton",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(title)).onClick.Invoke();}
            else if(command=="chair")RoomInteractionReview.PrepareProduct("room.armchair");
            else if(command=="end")FullAudioReview.End();
            else if(command=="validate")
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate the authored scenes in Edit Mode.");
                CatHomeEditPreview.Clear();
                try { File.WriteAllText(Path.Combine(Root,"validator.txt"),LevelContentValidator.ValidateProject().ToString()); }
                finally { CatHomeEditPreview.Refresh(); }
            }
            else throw new InvalidOperationException("Unknown chair review command: "+command);
            File.WriteAllText(Path.Combine(Root,"command-result.txt"),DateTime.Now.ToString("s")+" "+command+" accepted");
        }
        catch(Exception e){File.WriteAllText(Path.Combine(Root,"command-result.txt"),e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Cat Home/Doğrudan Eşya Dönüşü/Kontrolleri Çalıştır")]
    public static void RunTests()=>RunSelected(new[]{"JumpContinuityTests.AllRooms_JumpingActivities_UseDirectTurns","FurnitureBodyClearanceTests.AllJumpingFurniture_KeepActualBodyOutsideFurniture","KitchenBedroomSupportedMotionTests.NarrowPerches_TenBreeds_KeepAllPawsOnTheRealSurface"});
    static void Demo(string room,string label,string id)=>RoomInteractionReview.Audit(room,"direct-"+label+"-20260915",false,id);
    static void InspectAction()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public;
        var p=UnityEngine.Object.FindAnyObjectByType<ActivityPromptController>();
        var c=UnityEngine.Object.FindAnyObjectByType<CatMovement>();
        var candidate=(CatActivity)typeof(ActivityPromptController).GetField("candidate",flags).GetValue(p);
        var promptCat=(CatMovement)typeof(ActivityPromptController).GetField("cat",flags).GetValue(p);
        File.WriteAllText(Path.Combine(Root,"action-diagnostic.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new{
            cat=c.name,catEnabled=c.isActiveAndEnabled,catActive=c.gameObject.activeInHierarchy,
            promptEnabled=p.isActiveAndEnabled,promptCatSame=promptCat==c,
            worldBlocked=c.AreWorldActionsBlocked,homeBlocked=HomeUiFlow.IsHomeControlBlocked,busy=CatActionState.IsBusy(c),
            active=CatActivity.Active?.name,candidate=candidate?.name,candidateEnabled=candidate?.isActiveAndEnabled,
            unlocked=candidate?.IsUnlocked,sceneMatches=candidate!=null&&candidate.gameObject.scene==c.gameObject.scene,
            selected=typeof(ActivityPromptController).GetField("selected",flags).GetValue(p)?.ToString(),
            cameras=UnityEngine.Object.FindObjectsByType<Camera>().Select(v=>new{name=v.name,pos=v.transform.position.ToString("F5"),rot=v.transform.eulerAngles.ToString("F5"),fov=v.fieldOfView,main=v==Camera.main}).ToArray()
        },Newtonsoft.Json.Formatting.Indented));
    }
    static void RunRelease()=>RunSelected(new[]{
        "JumpContinuityTests.AllRooms_JumpingActivities_UseDirectTurns",
        "FurnitureBodyClearanceTests.AllJumpingFurniture_KeepActualBodyOutsideFurniture",
        "FurnitureBodyClearanceTests.LivingSofa_TenBreeds_KeepActualBodyOutsideFurniture",
        "JumpContinuityTests.LivingSurfaces_TenBreedsThreeRates_TurnDirectly",
        "JumpContinuityTests.Armchair_TenBreedsAndFrameRates_DirectEntryAndExit",
        "JumpContinuityTests.Armchair_PauseCancel_ReleasesWithoutMovingOnPause",
        "JumpContinuityTests.SurfaceTurn_PauseAndCancel_KeepPoseAndReleaseControl",
        "JumpContinuityTests.TenBreeds_At15_30_60Fps_PreserveNativeSkeletonAndLanding",
        "JumpContinuityTests.NarrowFurnitureTurns_TenBreeds_PlantPawsAndKeepOriginalJumps",
        "JumpContinuityTests.Fountain_TenBreeds_LeavesDrinkContinuously",
        "JumpContinuityTests.CoffeeTable_RecordsSupportedTurn",
        "HomeContactFacingTests.VanitySip_TenBreeds_KeepTheirSupportedPerch_AndPointIntoTheActualWaterTarget",
        "GameAudioTests.CoffeeTable_FloorImpactAtContact_ThreeFrameRates",
        "GameAudioTests.CoffeeTable_PausedOrCancelledFallIsQuiet_ThenReplayWorks"});
    static void RunSelected(string[] names,TestMode mode=TestMode.PlayMode)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorQaSession.IsActive)throw new InvalidOperationException("Finish the current preview before chair tests.");
        Directory.CreateDirectory(Root);
        FullAudioReview.PrepareTests();
        File.Copy("Docs/QA/FULL_AUDIO_2026-09-14/manual-preview-before.json",Path.Combine(Root,"preferences-before.json"),true);
        UiQaTestSession.ResultDirectory=Root;UiQaTestSession.ResultFileName="native-turn.xml";
        CatHomeAuthoringWorkspace.HoldFastPlayModeForManualTestRun();
        SessionState.SetBool(Running,true);
        api.Execute(new ExecutionSettings(new Filter{testMode=mode,testNames=names}));
    }
    [MenuItem("Tools/Cat Home/Doğrudan Eşya Dönüşü/Denemeyi Başlat")]
    public static void Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Already in Play Mode.");
        CatHomeAuthoringWorkspace.OpenFullHomePreview();FullAudioReview.Begin();
    }
    public static void Inspect(string file)
    {
        Directory.CreateDirectory(Root);var mix=UnityEngine.Object.FindAnyObjectByType<GameSoundscape>();
        var state=new State{play=EditorApplication.isPlaying,qa=EditorQaSession.IsActive,copy=EditorQaSession.SaveDirectory,
            compiling=EditorApplication.isCompiling,focused=Application.isFocused,editorMuted=EditorUtility.audioMasterMute,
            building=BuildPipeline.isBuildingPlayer,recording=RoomInteractionReview.Recording,
            soundEnabled=HomeAudioService.SoundEnabled,musicEnabled=HomeAudioService.MusicEnabled,
            captureFramerate=Time.captureFramerate,timeScale=Time.timeScale,
            title=TitleScreen.IsShowing.ToString(),selection=mix==null?"":mix.Selection,
            musicTarget=mix==null?0:mix.TargetMusicVolume,musicLevel=mix==null?0:mix.CurrentMusicVolume,
            cats=UnityEngine.Object.FindObjectsByType<CatMovement>().Length,
            listeners=UnityEngine.Object.FindObjectsByType<AudioListener>().Count(s=>s.isActiveAndEnabled),
            audioSystems=UnityEngine.Object.FindObjectsByType<GameAudio>().Length,
            scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new SceneInfo{name=s.name,dirty=s.isDirty}).ToArray(),
            sources=UnityEngine.Object.FindObjectsByType<AudioSource>().Where(s=>s.clip!=null).Select(s=>new SourceInfo{name=s.name,clip=s.clip.name,playing=s.isPlaying,volume=s.volume,time=s.time}).ToArray()};
        File.WriteAllText(Path.Combine(Root,file),JsonUtility.ToJson(state,true));
    }
    static void VerifyPreferences()
    {
        var before=JsonUtility.FromJson<Prefs>(File.ReadAllText(Path.Combine(Root,"preferences-before.json")));
        var mismatches=before.preferences.Where(p=>PlayerPrefs.HasKey(p.key)!=p.existed||
            p.existed&&(p.isString?PlayerPrefs.GetString(p.key)!=p.text:PlayerPrefs.GetInt(p.key)!=p.number)).Select(p=>p.key).ToArray();
        File.WriteAllText(Path.Combine(Root,"preferences-verified.json"),JsonUtility.ToJson(new PrefResult{
            checkedCount=before.preferences.Length,matchedCount=before.preferences.Length-mismatches.Length,
            editorMuteMatches=EditorUtility.audioMasterMute==before.audioMuted,mismatches=mismatches},true));
    }
    sealed class Results:ICallbacks
    {
        public void RunStarted(ITestAdaptor t){}
        public void TestStarted(ITestAdaptor t){}
        public void TestFinished(ITestResultAdaptor r){if(SessionState.GetBool(Running,false))File.AppendAllText(Path.Combine(Root,"progress.txt"),DateTime.Now.ToString("s")+" "+r.Test.FullName+" "+r.TestStatus+" "+r.Message+Environment.NewLine);}
        public void RunFinished(ITestResultAdaptor r)
        {
            if(!SessionState.GetBool(Running,false))return;
            TestRunnerApi.SaveResultToFile(r,Path.Combine(Root,"native-turn.xml"));
            SessionState.SetBool(Restore,true);
        }
    }
}






