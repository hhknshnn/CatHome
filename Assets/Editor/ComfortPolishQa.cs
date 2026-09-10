using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Current-pass native evidence and preference restoration, separate from the player's JSON and historical reports.</summary>
[InitializeOnLoad]
public static class ComfortPolishQa
{
    public const string Root = "Docs/QA/CARE_MOTION_2026-09-09";
    const string PrefSnapshotKey = "CatHome.ComfortPolishQa.Preferences";
    const string RunKey = "CatHome.ComfortPolishQa.NativeRun";
    const string StatusKey = "CatHome.ComfortPolishQa.NativeStatus";
    static readonly TestRunnerApi api;
    public static string TestStatus => SessionState.GetString(StatusKey, "Idle");

    [Serializable] sealed class Preference { public string key, text; public bool existed, isString; public int number; }
    [Serializable] sealed class Preferences { public List<Preference> values = new List<Preference>(); }
    [Serializable] sealed class ResultSummary { public string mode, result, finished; public int passed, failed, skipped; public double duration; }

    static ComfortPolishQa()
    {
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
    }

    public static string Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Prepare QA from Edit Mode.");
        if (!string.IsNullOrEmpty(SessionState.GetString(PrefSnapshotKey, "")))
            throw new InvalidOperationException("Finish the existing action-state QA session before replacing its preference snapshot.");
        var prefs = new Preferences();
        foreach (string key in new[] { "cat.identity.breed.v1", "CatHome_CatName", "CatHome_CollectionMilestones", "cat-home.local-guest-id" })
            prefs.values.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), isString = true, text = PlayerPrefs.GetString(key, "") });
        foreach (string key in new[] { "cat.identity.coat", "home.audio.sound", "home.audio.music", "cat-home.language", "Player_LightLevel", "CatHome_PetTutorialCompleted", "CatHome_IntroductionCompleted", "CatHome_IntroductionStep", "CatHome_OnboardingStep", "CatHome_OnboardingCompleted", "cat-home.account-kind", "CatRunner_BestScore_v1" })
            prefs.values.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), number = PlayerPrefs.GetInt(key, 0) });
        string json = JsonUtility.ToJson(prefs);
        SessionState.SetString(PrefSnapshotKey, json);
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "preferences-before.json"), json);
        UiQaTestSession.ResultDirectory = Root;
        UiQaTestSession.ResultFileName = "Native-unspecified.xml";
        JoyfulVisualQa.RootDirectory = Root;
        UiQaVisualTour.OutputDirectory = Root + "/screens-wide";
        MiniGameVisualQa.Folder = Root + "/minigames";
        MiniGameVisualQa.FramesFolder = "Library/ComfortPolishMotion";
        return UiQaTestSession.Begin();
    }

    public static string End()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play before restoring preferences.");
        string json = SessionState.GetString(PrefSnapshotKey, "");
        if (string.IsNullOrEmpty(json)) throw new InvalidOperationException("No action-state QA preference snapshot is present.");
        var prefs = JsonUtility.FromJson<Preferences>(json);
        var language=prefs.values.Find(p=>p.key==GameLanguageService.PlayerPrefsKey);
        GameLanguageService.SetLanguage(language!=null&&language.existed?(GameLanguage)language.number:GameLanguageService.ResolveInitialLanguage(Application.systemLanguage));
        foreach (var item in prefs.values)
        {
            if (!item.existed) PlayerPrefs.DeleteKey(item.key);
            else if (item.isString) PlayerPrefs.SetString(item.key, item.text);
            else PlayerPrefs.SetInt(item.key, item.number);
        }
        PlayerPrefs.Save();
        foreach (var item in prefs.values)
        {
            if (item.existed != PlayerPrefs.HasKey(item.key) ||
                (item.existed && (item.isString ? PlayerPrefs.GetString(item.key) != item.text : PlayerPrefs.GetInt(item.key) != item.number)))
                throw new InvalidOperationException("Preference restore failed: " + item.key);
        }
        UiQaTestSession.End();
        SessionState.EraseString(PrefSnapshotKey);
        File.WriteAllText(Path.Combine(Root, "preferences-restored.txt"), "All " + prefs.values.Count + " presentation preferences and presence flags restored exactly. " + DateTime.UtcNow.ToString("O"));
        return "QA save override ended; original presentation preferences restored.";
    }

    public static string RunNative(string mode, string label, params string[] groups)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start native tests from Edit Mode.");
        if (TestStatus.StartsWith("Running") || TestStatus.StartsWith("Queued")) throw new InvalidOperationException("A native run is already active.");
        TestMode testMode = mode == "PlayMode" ? TestMode.PlayMode : TestMode.EditMode;
        if (testMode == TestMode.PlayMode)
        {
            if (!EditorQaSession.IsActive) throw new InvalidOperationException("Use isolated QA for PlayMode tests.");
            CatHomeAuthoringWorkspace.HoldFastPlayModeForManualTestRun();
        }
        if (label.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException("Use a simple result label.");
        Directory.CreateDirectory(Root);
        UiQaTestSession.ResultDirectory = Root;
        UiQaTestSession.ResultFileName = label + ".xml";
        SessionState.SetString(RunKey, label);
        SessionState.SetString(StatusKey, "Queued " + mode);
        SessionState.SetString("CatHome.ComfortPolishQa.Mode", mode);
        var filter = new Filter { testMode = testMode, groupNames = groups.Length == 0 ? null : groups };
        // Execute schedules the framework's own EditorApplication.update job.
        // An extra delayCall can remain queued while inspectors are unfocused.
        try { api.Execute(new ExecutionSettings(filter)); }
        catch (Exception error)
        {
            SessionState.SetString(StatusKey, "Failed to start: " + error.Message);
            SessionState.EraseString(RunKey);
            throw;
        }
        return TestStatus;
    }

    sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor test)
        { if (!string.IsNullOrEmpty(SessionState.GetString(RunKey, ""))) SessionState.SetString(StatusKey, "Running " + test.Name); }
        public void TestStarted(ITestAdaptor test)
        { if (!string.IsNullOrEmpty(SessionState.GetString(RunKey, ""))) SessionState.SetString(StatusKey, "Running " + test.FullName); }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            string label = SessionState.GetString(RunKey, "");
            if (string.IsNullOrEmpty(label)) return;
            Directory.CreateDirectory(Root);
            TestRunnerApi.SaveResultToFile(result, Path.Combine(Root, label + ".xml"));
            var summary = new ResultSummary { mode = SessionState.GetString("CatHome.ComfortPolishQa.Mode", ""), result = result.ResultState, passed = result.PassCount, failed = result.FailCount, skipped = result.SkipCount, duration = result.Duration, finished = DateTime.UtcNow.ToString("O") };
            File.WriteAllText(Path.Combine(Root, label + ".json"), JsonUtility.ToJson(summary, true));
            SessionState.SetString(StatusKey, result.ResultState + " " + result.PassCount + " passed / " + result.FailCount + " failed");
            SessionState.EraseString(RunKey);
        }
    }
}

