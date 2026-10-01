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
public static class AudioRefinementReview
{
    public const string Root="Docs/QA/AUDIO_REFINEMENT_2026-09-15";
    const string Running="CatHome.AudioRefinement.Running", Restore="CatHome.AudioRefinement.Restore";
    static readonly TestRunnerApi api;
    static double nextPoll,restoreAfter;
    [Serializable] sealed class Pref {public string key,text;public bool existed,isString;public int number;}
    [Serializable] sealed class Prefs {public bool audioMuted;public Pref[] preferences;}
    [Serializable] sealed class PrefResult {public int checkedCount,matchedCount;public bool editorMuteMatches;public string[] mismatches;}
    [Serializable] sealed class SceneInfo {public string name;public bool dirty;}
    [Serializable] sealed class SourceInfo {public string name,clip;public bool playing;public float volume,time;}
    [Serializable] sealed class State
    {
        public bool play,qa,compiling,focused,editorMuted;
        public string copy,title,selection;
        public int cats,listeners,audioSystems;
        public float musicTarget,musicLevel;
        public SceneInfo[] scenes;public SourceInfo[] sources;
    }
    static AudioRefinementReview()
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
            else if(command=="review")Begin();
            else if(command=="end")FullAudioReview.End();
            else if(command=="validate")File.WriteAllText(Path.Combine(Root,"validator.txt"),LevelContentValidator.ValidateProject().ToString());
            else throw new InvalidOperationException("Unknown audio review command: "+command);
            File.WriteAllText(Path.Combine(Root,"command-result.txt"),DateTime.Now.ToString("s")+" "+command+" accepted");
        }
        catch(Exception e){File.WriteAllText(Path.Combine(Root,"command-result.txt"),e.ToString());Debug.LogException(e);}
    }
    [MenuItem("Tools/Cat Home/Ses İyileştirmesi/Kontrolleri Çalıştır")]
    public static void RunTests()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorQaSession.IsActive)throw new InvalidOperationException("Finish the current preview before audio tests.");
        Directory.CreateDirectory(Root);
        FullAudioReview.PrepareTests();
        File.Copy("Docs/QA/FULL_AUDIO_2026-09-14/manual-preview-before.json",Path.Combine(Root,"preferences-before.json"),true);
        UiQaTestSession.ResultDirectory=Root;UiQaTestSession.ResultFileName="native-audio-refinement.xml";
        CatHomeAuthoringWorkspace.HoldFastPlayModeForManualTestRun();
        SessionState.SetBool(Running,true);
        api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{"GameAudioTests"}}));
    }
    [MenuItem("Tools/Cat Home/Ses İyileştirmesi/Denemeyi Başlat")]
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
        public void TestFinished(ITestResultAdaptor r){}
        public void RunFinished(ITestResultAdaptor r)
        {
            if(!SessionState.GetBool(Running,false))return;
            TestRunnerApi.SaveResultToFile(r,Path.Combine(Root,"native-audio-refinement.xml"));
            SessionState.SetBool(Restore,true);
        }
    }
}
