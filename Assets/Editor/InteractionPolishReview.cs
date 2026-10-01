using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

/// <summary>Runs bounded native checks against a copied save, then restores the authoring workspace.</summary>
[InitializeOnLoad]
public static class InteractionPolishReview
{
    public const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const string Running = "CatHome.InteractionPolish.Running";
    const string Restore = "CatHome.InteractionPolish.Restore";
    static readonly TestRunnerApi api;
    static double restoreAfter;

    static InteractionPolishReview()
    {
        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
        EditorApplication.update += Poll;
    }

    public static void Run(string[] names, string file, TestMode mode = TestMode.PlayMode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorQaSession.IsActive)
            throw new InvalidOperationException("End the current copied preview before tests.");
        Directory.CreateDirectory(Root);
        FullAudioReview.PrepareTests();
        UiQaTestSession.ResultDirectory = Root;
        UiQaTestSession.ResultFileName = file;
        CatHomeAuthoringWorkspace.HoldFastPlayModeForManualTestRun();
        SessionState.SetBool(Running, true);
        api.Execute(new ExecutionSettings(new Filter { testMode = mode, testNames = names }));
    }

    static void Poll()
    {
        if (!SessionState.GetBool(Restore, false) || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (restoreAfter == 0) { restoreAfter = EditorApplication.timeSinceStartup + 3; return; }
        if (EditorApplication.timeSinceStartup < restoreAfter) return;
        FullAudioReview.EndTests();
        // Opening the full authoring menu calls SaveAssets, which can persist
        // test-time font caches and the currency prefab's preview values.
        // Reopen only the normal scene stack; no unrelated asset is saved.
        SessionState.SetBool("CatHome.Workspace.ManualPlayModeTestRun", false);
        EditorSceneManager.playModeStartScene = null;
        EditorSceneManager.OpenScene(CatHomeAuthoringWorkspace.BootstrapScenePath, OpenSceneMode.Single);
        EditorSceneManager.OpenScene(CatHomeAuthoringWorkspace.UiScenePath, OpenSceneMode.Additive);
        var level = EditorSceneManager.OpenScene(CatHomeAuthoringWorkspace.LevelScenePath, OpenSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(level);
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableSceneReload;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        CatHomeEditPreview.Refresh();
        SessionState.SetBool(Restore, false);
        SessionState.SetBool(Running, false);
        restoreAfter = 0;
    }

    sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor test) { }
        public void TestStarted(ITestAdaptor test) { Progress("start", test.FullName, ""); }
        public void TestFinished(ITestResultAdaptor result) { Progress("finish", result.Test.FullName, result.ResultState); }
        static void Progress(string stage, string name, string state)
        {
            if (!SessionState.GetBool(Running, false)) return;
            string path = Path.ChangeExtension(Path.Combine(Root, UiQaTestSession.ResultFileName), ".progress.tsv");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, DateTime.UtcNow.ToString("o") + "\t" + stage + "\t" + name + "\t" + state + Environment.NewLine);
        }
        public void RunFinished(ITestResultAdaptor result)
        {
            if (!SessionState.GetBool(Running, false)) return;
            TestRunnerApi.SaveResultToFile(result, Path.Combine(Root, UiQaTestSession.ResultFileName));
            SessionState.SetBool(Restore, true);
        }
    }
}
