using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class UiQaTestSession
{
    public static string ResultDirectory
    {
        get => SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/UIUX_2026-09-06");
        set => SessionState.SetString("CatHome.QA.ResultDirectory", value);
    }
    public static string ResultFileName
    {
        get => SessionState.GetString("CatHome.QA.ResultFileName", string.Empty);
        set => SessionState.SetString("CatHome.QA.ResultFileName", value);
    }
    private static readonly TestRunnerApi api;
    static UiQaTestSession()
    {
        api=ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
    }
    public static string Begin()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Start QA from Edit Mode.");
        string path=Path.GetFullPath(Path.Combine("Library","UiQaSession",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
        Directory.CreateDirectory(path);
        string source=Path.Combine(Application.persistentDataPath,CatHomeSaveSystem.SaveFileName);
        if(File.Exists(source))File.Copy(source,Path.Combine(path,CatHomeSaveSystem.SaveFileName));
        if(File.Exists(source+CatHomeSaveSystem.RecoveryFileSuffix))File.Copy(source+CatHomeSaveSystem.RecoveryFileSuffix,Path.Combine(path,CatHomeSaveSystem.SaveFileName+CatHomeSaveSystem.RecoveryFileSuffix));
        SessionState.SetString("CatHome.QA.SaveDirectory",path);
        return "QA uses a separate copy of the local save; cloud synchronization is disabled for this session.";
    }
    public static void End()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Finish Play Mode before ending QA.");
        SessionState.EraseString("CatHome.QA.SaveDirectory");
    }
    private sealed class Results:ICallbacks
    {
        public void RunStarted(ITestAdaptor test){}
        public void TestStarted(ITestAdaptor test){}
        public void TestFinished(ITestResultAdaptor result){}
        public void RunFinished(ITestResultAdaptor result)
        {
            string directory=ResultDirectory;Directory.CreateDirectory(directory);
            string file = string.IsNullOrEmpty(ResultFileName) ? (EditorQaSession.IsActive?"PlayMode.xml":"EditMode.xml") : ResultFileName;
            TestRunnerApi.SaveResultToFile(result,Path.Combine(directory,file));
        }
    }
}
