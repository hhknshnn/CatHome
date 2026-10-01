using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Real-time controls in the isolated save; screenshots are native pixels.</summary>
public static class MiniGameVisualQa
{
    public static string Status{get;private set;}="Idle";
    public static string Folder="Docs/QA/MINIGAMES_2026-09-07";
    public static string FramesFolder="Library/MiniGameMotion";
    public static bool FollowObstacles;
    public static void Begin(bool runner,bool record=false)
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Isolated Play required.");
        UiQaVisualTour.Find<CatMovement>().StartCoroutine(Tour(runner,record));
    }
    static IEnumerator Tour(bool runner,bool record)
    {
        int old=Time.captureFramerate;Time.captureFramerate=24;Time.timeScale=1;
        UiQaVisualTour.Resolution(1920,1080);UiQaVisualTour.OutputDirectory=Folder;
        Directory.CreateDirectory(Folder);Status="Opening";
        try
        {
            UiQaVisualTour.Clear();
            var now=DateTime.UtcNow;
            if(runner)RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState{energy=5,regenerationAnchorUtc=now.ToString("O"),unlimitedUntilUtc=string.Empty,rewardedAdsDayUtc=now.ToString("yyyy-MM-dd")},now);
            else CatchLivesService.ApplySavedState(null,now);
            if(runner)UiQaVisualTour.Find<CatRunnerLauncher>().Launch();else UiQaVisualTour.Find<CatCatchLauncher>().Launch();
            for(int i=0;i<48;i++)yield return null;
            string label=runner?"Runner":"Catch";
            yield return CaptureAspects(label+"-Welcome");
            yield return new WaitForSeconds(.3f);
            if(runner){var g=UiQaVisualTour.Find<CatRunnerGameController>();g.StartFromWelcome();g.SkipTutorial();}
            else{var g=UiQaVisualTour.Find<CatCatchGameController>();g.StartHunt();g.SkipTutorial();}
            string frames=FramesFolder+"/"+label;Directory.CreateDirectory(frames);
            int catches=0;
            for(int f=0;f<(record?24*18:24*8);f++)
            {
                if(runner)
                {
                    var p=UiQaVisualTour.Find<CatRunnerPlayer>();
                    if(FollowObstacles)
                    {
                        var g=UiQaVisualTour.Find<CatRunnerGameController>();
                        var hazard=Object.FindObjectsByType<CatRunnerTrackObject>()
                            .Where(h=>h.IsHazard&&Mathf.Abs(h.transform.localPosition.x-p.LanePosition)<.6f&&h.transform.localPosition.z>0)
                            .OrderBy(h=>h.transform.localPosition.z).FirstOrDefault();
                        if(hazard!=null&&hazard.transform.localPosition.z<g.CurrentSpeed*.37f+.12f)
                            UiQaVisualTour.Call(p,hazard.Kind==CatRunnerTrackObjectKind.OverheadObstacle?"Slide":"Jump");
                    }
                    else
                    {
                        if(f%96==30)UiQaVisualTour.Call(p,"Jump");
                        if(f%96==62)UiQaVisualTour.Call(p,"Slide");
                        if(f%144==12)UiQaVisualTour.Call(p,"MoveLane",1);
                        if(f%144==84)UiQaVisualTour.Call(p,"MoveLane",-1);
                    }
                }
                else
                {
                    var p=UiQaVisualTour.Find<CatCatchPlayer>();
                    var mouse=Object.FindObjectsByType<CatCatchMouse>().Where(m=>m.IsCatchable).OrderBy(m=>Vector3.Distance(p.transform.position,m.transform.position)).FirstOrDefault();
                    if(mouse!=null&&!p.IsBusy&&p.Prey==null)p.ChasePrey(mouse);
                    catches=UiQaVisualTour.Find<CatCatchGameController>().Catches;
                }
                if(f==24||f==60||f==112)UiQaVisualTour.Capture(label+"-Gameplay-"+f);
                if(f==180)MiniGamePreviewBuilder.CaptureLive(runner);
                yield return new WaitForEndOfFrame();
                if(record)
                {
                    var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(frames+"/"+f.ToString("D4")+".jpg",t.EncodeToJPG(95));Object.Destroy(t);
                }
                Status=label+" frame "+f+" catches "+catches;
            }
            bool active=runner?UiQaVisualTour.Find<CatRunnerGameController>().IsRunning:UiQaVisualTour.Find<CatCatchGameController>().IsHunting;
            if(active)
            {
                if(runner)UiQaVisualTour.Find<CatRunnerGameController>().PauseRun();else UiQaVisualTour.Find<CatCatchGameController>().PauseHunt();
                for(int i=0;i<12;i++)yield return null;
                yield return CaptureAspects(label+"-Pause");
                if(runner)UiQaVisualTour.Find<CatRunnerGameController>().ResumeRun();else UiQaVisualTour.Find<CatCatchGameController>().ResumeHunt();
            }
            float until=Time.time+80;
            while(Time.time<until&&(runner?UiQaVisualTour.Find<CatRunnerGameController>().IsRunning:UiQaVisualTour.Find<CatCatchGameController>().IsHunting))
            {
                if(!runner)
                {
                    var p=UiQaVisualTour.Find<CatCatchPlayer>();var prey=Object.FindObjectsByType<CatCatchMouse>().Where(m=>m.IsCatchable).OrderBy(m=>Vector3.Distance(m.transform.position,p.Position)).FirstOrDefault();
                    if(prey!=null&&!p.IsBusy&&p.Prey==null)p.ChasePrey(prey);
                }
                Status=label+" finishing a real round";yield return null;
            }
            for(int i=0;i<16;i++)yield return null;
            yield return CaptureAspects(label+"-Results");
            Status="Complete "+label+" catches "+catches;
        }
        finally{Time.captureFramerate=old;Time.timeScale=1;}
    }
    private static IEnumerator CaptureAspects(string name)
    {
        UiQaVisualTour.Resolution(1920,1080);for(int i=0;i<5;i++)yield return null;UiQaVisualTour.Capture(name);
        for(int i=0;i<3;i++)yield return null;
        UiQaVisualTour.Resolution(1440,1080);for(int i=0;i<5;i++)yield return null;UiQaVisualTour.Capture(name+"-4x3");
        for(int i=0;i<3;i++)yield return null;
        UiQaVisualTour.Resolution(1920,1080);for(int i=0;i<5;i++)yield return null;
    }
}
