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
public static class NaturalTurnReview
{
    public const string Root="Docs/QA/NATURAL_TURN_2026-09-15";
    const string Running="CatHome.NaturalTurn.Running", Restore="CatHome.NaturalTurn.Restore";
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
    static NaturalTurnReview()
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
            else if(command=="tests")RunTests();
            else if(command=="impact")RunSelected(new[]{"GameAudioTests.CoffeeTable_FloorImpactAtContact_ThreeFrameRates","GameAudioTests.CoffeeTable_PausedOrCancelledFallIsQuiet_ThenReplayWorks"});
            else if(command=="final")RunSelected(new[]{"GameAudioTests.Bank_AllAuthoredClipsDecodeAndMusicStreams","GameAudioTests.CoffeeTable_FloorImpactAtContact_ThreeFrameRates","GameAudioTests.CoffeeTable_PausedOrCancelledFallIsQuiet_ThenReplayWorks","KitchenBedroomSupportedMotionTests.NarrowPerches_TenBreeds_KeepAllPawsOnTheRealSurface","FurnitureBodyClearanceTests.CorrectedContacts_TenBreeds_KeepBodyAndArmsOutsideFurniture"});
            else if(command=="baseline")RunSelected(new[]{"JumpContinuityTests.CoffeeTable_RecordsSupportedTurn"});
            else if(command=="review")Begin();
            else if(command=="home"){if(!EditorQaSession.IsActive||!EditorApplication.isPlaying)throw new InvalidOperationException("Isolated Play required.");var title=UnityEngine.Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);if(TitleScreen.IsShowing)((UnityEngine.UI.Button)typeof(TitleScreen).GetField("playButton",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(title)).onClick.Invoke();}
            else if(command=="coffee")RoomInteractionReview.PrepareProduct("CoffeeTablePlay");
            else if(command=="demo")RoomInteractionReview.Audit(HomeRoomService.LivingRoomId,"coffee-release-20260915",false,"CoffeeTablePlay");
            else if(command=="end")FullAudioReview.End();
            else if(command=="validate")File.WriteAllText(Path.Combine(Root,"validator.txt"),LevelContentValidator.ValidateProject().ToString());
            else throw new InvalidOperationException("Unknown audio review command: "+command);
            File.WriteAllText(Path.Combine(Root,"command-result.txt"),DateTime.Now.ToString("s")+" "+command+" accepted");
        }
        catch(Exception e){File.WriteAllText(Path.Combine(Root,"command-result.txt"),e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Cat Home/Doğal Dönüş/Kontrolleri Çalıştır")]
    public static void RunTests()=>RunSelected(new[]{"JumpContinuityTests.CoffeeTable_RecordsSupportedTurn","JumpContinuityTests.CoffeeTable_TenBreedsAndFrameRates_UseShortFlowingTurn","JumpContinuityTests.EveryRoom_AllJumpingFurniture_HoldsLandingAndReleasesContinuously","JumpContinuityTests.ReportedPerchTurns_TenBreeds_PreserveLegAnatomy","JumpContinuityTests.NarrowFurnitureTurns_TenBreeds_PlantPawsAndKeepOriginalJumps","JumpContinuityTests.SurfaceTurn_PauseAndCancel_KeepPoseAndReleaseControl","JumpContinuityTests.TenBreeds_At15_30_60Fps_PreserveNativeSkeletonAndLanding"});
    static void RunSelected(string[] names)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorQaSession.IsActive)throw new InvalidOperationException("Finish the current preview before audio tests.");
        Directory.CreateDirectory(Root);
        FullAudioReview.PrepareTests();
        File.Copy("Docs/QA/FULL_AUDIO_2026-09-14/manual-preview-before.json",Path.Combine(Root,"preferences-before.json"),true);
        UiQaTestSession.ResultDirectory=Root;UiQaTestSession.ResultFileName="native-turn.xml";
        CatHomeAuthoringWorkspace.HoldFastPlayModeForManualTestRun();
        SessionState.SetBool(Running,true);
        api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=names}));
    }
    [MenuItem("Tools/Cat Home/Doğal Dönüş/Denemeyi Başlat")]
    public static void Begin()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Already in Play Mode.");
        CatHomeAuthoringWorkspace.OpenFullHomePreview();FullAudioReview.Begin();
    }
    public static void Inspect(string file)
    {
        Directory.CreateDirectory(Root);var mix=UnityEngine.Object.FindFirstObjectByType<GameSoundscape>();
        var state=new State{play=EditorApplication.isPlaying,qa=EditorQaSession.IsActive,copy=EditorQaSession.SaveDirectory,
            compiling=EditorApplication.isCompiling,focused=Application.isFocused,editorMuted=EditorUtility.audioMasterMute,
            building=BuildPipeline.isBuildingPlayer,recording=RoomInteractionReview.Recording,
            soundEnabled=HomeAudioService.SoundEnabled,musicEnabled=HomeAudioService.MusicEnabled,
            captureFramerate=Time.captureFramerate,timeScale=Time.timeScale,
            title=TitleScreen.IsShowing.ToString(),selection=mix==null?"":mix.Selection,
            musicTarget=mix==null?0:mix.TargetMusicVolume,musicLevel=mix==null?0:mix.CurrentMusicVolume,
            cats=UnityEngine.Object.FindObjectsByType<CatMovement>(FindObjectsSortMode.None).Length,
            listeners=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(s=>s.isActiveAndEnabled),
            audioSystems=UnityEngine.Object.FindObjectsByType<GameAudio>(FindObjectsSortMode.None).Length,
            scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Select(s=>new SceneInfo{name=s.name,dirty=s.isDirty}).ToArray(),
            sources=UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(s=>s.clip!=null).Select(s=>new SourceInfo{name=s.name,clip=s.clip.name,playing=s.isPlaying,volume=s.volume,time=s.time}).ToArray()};
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






