using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Presentation-only tour. Round mechanics are exercised by separate native tests.</summary>
public static class CozyGamesReview
{
    public const string Root="Docs/QA/MINIGAMES_COMPLETE_2026-10-05";
    public static string Status{get;private set;}="Idle";
    static GameLanguage original;
    static bool arcadeOnly;
    public static void FinalPages()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Copied-save Play required.");
        original=GameLanguageService.Current;UiQaVisualTour.Find<CatMovement>().StartCoroutine(FinalPagesRoutine());
    }
    static IEnumerator FinalPagesRoutine()
    {
        while(UiQaVisualTour.Find<LevelLoader>()==null||!UiQaVisualTour.Find<LevelLoader>().IsReady)yield return null;
        UiQaVisualTour.OutputDirectory=Root+"/screens";
        foreach(var language in new[]{GameLanguage.Turkish,GameLanguage.English})
        {
            Time.timeScale=1;GameLanguageService.SetLanguage(language);UiQaVisualTour.Clear();UiQaVisualTour.Find<GamesHubPanel>().Show();
            yield return new WaitForSecondsRealtime(.8f);
            var prefix=language==GameLanguage.Turkish?"TR":"EN";
            yield return Views(prefix+"-Games");
            for(int index=0;index<4;index++)
            {
                UiQaVisualTour.Clear();
                if(index==0)UiQaVisualTour.Find<CatRunnerLauncher>().Launch();
                else if(index==1)UiQaVisualTour.Find<CatCatchLauncher>().Launch();
                else {UiQaVisualTour.Find<GamesHubPanel>().Show();UiQaVisualTour.Find<GamesHubPanel>().PlayCozy(index==2?CozyGameKind.Yarn:CozyGameKind.Pond);}
                yield return new WaitForSecondsRealtime(1.8f);
                yield return Views(prefix+"-"+new[]{"Runner","Catch","Yarn","Pond"}[index]+"-Welcome");
                if(index==0)UiQaVisualTour.Find<CatRunnerGameController>().ReturnToGamesFromWelcome();
                else if(index==1)UiQaVisualTour.Find<CatCatchGameController>().ExitToHome();
                else UiQaVisualTour.Find<CozyMiniGame>().Exit(true);
                yield return new WaitForSecondsRealtime(1.2f);
            }
        }
        GameLanguageService.SetLanguage(original);UiQaVisualTour.Resolution(1920,1080);UiQaVisualTour.Clear();UiQaVisualTour.Find<GamesHubPanel>().Show();
        Status="Final hub and welcomes complete: 30 views.";
        File.WriteAllText(Root+"/final-pages.txt",Status);
    }
    public static void Start(bool onlyArcade=false)
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Copied-save Play required.");
        original=GameLanguageService.Current;arcadeOnly=onlyArcade;
        UiQaVisualTour.Find<CatMovement>().StartCoroutine(Run());
    }
    static IEnumerator Run()
    {
        while(UiQaVisualTour.Find<LevelLoader>()==null||!UiQaVisualTour.Find<LevelLoader>().IsReady)yield return null;
        UiQaVisualTour.OutputDirectory=Root+"/screens";
        foreach(var language in new[]{GameLanguage.Turkish,GameLanguage.English})
        for(int index=0;index<(arcadeOnly?2:4);index++)
        {
            Time.timeScale=1;UiQaVisualTour.Clear();GameLanguageService.SetLanguage(language);
            string name=new[]{"Runner","Catch","Yarn","Pond"}[index];
            string prefix=(language==GameLanguage.Turkish?"TR":"EN")+"-"+name;
            var now=DateTime.UtcNow;
            RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState{energy=5,regenerationAnchorUtc=now.ToString("O")},now);
            var progress=CatRunnerProgressService.CaptureState(now);progress.tutorialCompleted=true;CatRunnerProgressService.ApplySavedState(progress,now);
            CatchLivesService.ApplySavedState(null,now);CatchLivesService.CompleteTutorial();
            if(index==0)UiQaVisualTour.Find<CatRunnerLauncher>().Launch();
            else if(index==1)UiQaVisualTour.Find<CatCatchLauncher>().Launch();
            else {var hub=UiQaVisualTour.Find<GamesHubPanel>();hub.Show();hub.PlayCozy(index==2?CozyGameKind.Yarn:CozyGameKind.Pond);}
            yield return new WaitForSecondsRealtime(2);
            var runner=index==0?UiQaVisualTour.Find<CatRunnerGameController>():null;
            var catcher=index==1?UiQaVisualTour.Find<CatCatchGameController>():null;
            var cozy=index>=2?UiQaVisualTour.Find<CozyMiniGame>():null;
            if(runner==null&&catcher==null&&cozy==null){Status="Failed to load "+name;yield break;}
            yield return Views(prefix+"-Welcome");
            if(runner!=null)runner.StartFromWelcome();else if(catcher!=null)catcher.StartHunt();else cozy.StartRound();
            yield return new WaitForSecondsRealtime(runner!=null?7f:3f);
            Time.timeScale=0;
            if(cozy!=null&&cozy.kind==CozyGameKind.Yarn){UiQaVisualTour.Call(cozy,"SetupLevel",3);}
            var camera=runner!=null?runner.GetComponentInChildren<Camera>():catcher!=null?catcher.GetComponentInChildren<Camera>():cozy.gameCamera;
            if(language==GameLanguage.Turkish)Photo(camera,name);
            yield return Views(prefix+"-Play");
            if(runner!=null)runner.PauseRun();else if(catcher!=null)catcher.PauseHunt();else cozy.Pause();
            yield return Views(prefix+"-Pause");
            if(runner!=null){runner.ResumeRun();UiQaVisualTour.Call(runner,"CompleteRun");}
            else if(catcher!=null){catcher.ResumeHunt();UiQaVisualTour.Call(catcher,"EndHunt");}
            else {cozy.Resume();cozy.FinishRound();}
            Time.timeScale=0;
            yield return Views(prefix+"-Result");
            Time.timeScale=1;
            if(runner!=null)runner.ReturnToGamesFromResult();else if(catcher!=null)catcher.ExitToHome();else cozy.Exit(true);
            yield return new WaitForSecondsRealtime(1.8f);
        }
        GameLanguageService.SetLanguage(original);UiQaVisualTour.Resolution(1920,1080);UiQaVisualTour.Clear();UiQaVisualTour.Find<GamesHubPanel>().Show();
        Status="Complete: "+(arcadeOnly?48:96)+" presentation views; layouts use QA rounds, not player progress.";
        File.WriteAllText(Root+"/visual-review.txt",Status);AssetDatabase.Refresh();
    }
    static IEnumerator Views(string prefix)
    {
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
        {
            UiQaVisualTour.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.25f);
            Status=prefix+"-"+size.x;UiQaVisualTour.Capture(Status);
            var report=new System.Text.StringBuilder();
            foreach(var label in Object.FindObjectsByType<TMPro.TMP_Text>())
            {
                if(!label.isActiveAndEnabled||label.color.a<.03f)continue;
                float alpha=1;foreach(var group in label.GetComponentsInParent<CanvasGroup>())alpha*=group.alpha;
                if(alpha<.05f)continue;
                label.ForceMeshUpdate();if(label.isTextOverflowing)report.AppendLine("OVERFLOW "+label.name+": "+label.text);
            }
            File.WriteAllText(Root+"/screens/"+Status+".text.txt",report.Length==0?"No visible TMP overflow.":report.ToString());
            yield return new WaitForSecondsRealtime(.15f);
        }
    }
    public static void Photo(Camera camera,string name)
    {
        string path="Assets/Resources/CozyGames/"+name+"Preview.png";
        var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            var bytes=image.EncodeToPNG();File.WriteAllBytes(path,bytes);
            if(name=="Runner"||name=="Catch")File.WriteAllBytes("Assets/Art/Games/"+name+"Preview.png",bytes);
            if(name=="Runner")File.WriteAllBytes("Assets/Art/Runner/UI/CatRunnerHero_v1.png",bytes);
        }
        finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
    }
}

