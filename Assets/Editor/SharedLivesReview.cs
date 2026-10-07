using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SharedLivesReview
{
    public const string Root="Docs/QA/SHARED_LIVES_2026-10-06";
    public static string Status="Idle";
    static List<string> checks;
    static void Check(bool value,string label){if(!value)throw new Exception(label);checks.Add("PASS "+label);File.WriteAllLines(Root+"/runtime-checks.txt",checks);}
    public static void CheckGameplay()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new Exception("Copied QA required.");
        checks=new List<string>();Status="Checking gameplay";
        var host=new GameObject("Shared lives QA").AddComponent<SharedLivesReviewHost>();Object.DontDestroyOnLoad(host.gameObject);
        host.StartCoroutine(Guard(Checks(),host));
    }
    static IEnumerator Guard(IEnumerator work,MonoBehaviour host)
    {
        var stack=new Stack<IEnumerator>();stack.Push(work);
        while(stack.Count>0)
        {
            object next=null;bool moved=false;
            try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}
            catch(Exception e){Status="FAILED: "+e.Message;File.WriteAllText(Root+"/runtime-failure.txt",e.ToString());yield break;}
            if(!moved){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        Status="Gameplay checks complete";File.WriteAllText(Root+"/runtime-complete.txt",Status);Object.Destroy(host.gameObject);
    }
    static IEnumerator ReturnHome()
    {
        float end=Time.realtimeSinceStartup+15;while(HomeUiFlow.IsMiniGameVisible&&Time.realtimeSinceStartup<end)yield return null;
        Check(!HomeUiFlow.IsMiniGameVisible,"Returned to home");yield return null;
    }
    public static void CheckCatchRefill()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new Exception("Copied QA required.");
        checks=new List<string>(File.ReadAllLines(Root+"/runtime-checks.txt"));
        var host=new GameObject("Catch refill QA").AddComponent<SharedLivesReviewHost>();Object.DontDestroyOnLoad(host.gameObject);host.StartCoroutine(Guard(CatchRefill(),host));
    }
    static IEnumerator CatchRefill()
    {
        UiQaVisualTour.Clear();Lives(1);CatchLivesService.CompleteTutorial();
        var hub=UiQaVisualTour.Find<GamesHubPanel>();yield return Launch(hub,1);
        var game=UiQaVisualTour.Find<CatCatchGameController>();game.StartHunt();UiQaVisualTour.Call(game,"FinishHunt",true);yield return null;
        var retry=UiQaVisualTour.Get<UnityEngine.UI.Button>(game,"retryButton");Check(!retry.interactable,"Catch result retry disabled at zero lives");
        var state=MiniGameLivesSaveState.CreateDefault(DateTime.UtcNow.AddMinutes(-10));state.lives=0;MiniGameLivesService.ApplySavedState(state,DateTime.UtcNow);yield return null;
        Check(retry.interactable,"Catch result retry reenables after regeneration");retry.onClick.Invoke();Check(game.IsHunting&&MiniGameLivesService.CurrentLives==0,"Catch regenerated retry spends exactly one");
        game.PauseHunt();game.ReturnToGamesFromPause();yield return ReturnHome();
    }
    static IEnumerator Launch(GamesHubPanel hub,int index)
    {
        hub.Show();yield return null;
        if(index==0)UiQaVisualTour.Call(hub,"PlayRunner");else if(index==1)UiQaVisualTour.Call(hub,"PlayCatch");else hub.PlayCozy(index==2?CozyGameKind.Yarn:CozyGameKind.Pond);
        float end=Time.realtimeSinceStartup+15;
        while((index==0?UiQaVisualTour.Find<CatRunnerGameController>()==null:index==1?UiQaVisualTour.Find<CatCatchGameController>()==null:UiQaVisualTour.Find<CozyMiniGame>()==null)&&Time.realtimeSinceStartup<end)yield return null;
        yield return new WaitForSecondsRealtime(.15f);
    }
    static IEnumerator Checks()
    {
        Application.runInBackground=true;Time.timeScale=1;UiQaVisualTour.Clear();Lives(20);
        var now=DateTime.UtcNow;var progress=CatRunnerProgressSaveState.CreateDefault(now);progress.tutorialCompleted=true;CatRunnerProgressService.ApplySavedState(progress,now);
        var catcher=CatchLivesSaveState.CreateDefault(now);catcher.tutorialCompleted=true;catcher.lives=0;CatchLivesService.ApplySavedState(catcher,now);
        var legacy=RunnerEnergySaveState.CreateDefault(now);legacy.energy=0;RunnerEnergyService.ApplySavedState(legacy,now);
        var hub=UiQaVisualTour.Find<GamesHubPanel>();int expected=20;
        for(int index=0;index<4;index++)
        {
            Status="Checking game "+index;yield return Launch(hub,index);Check(MiniGameLivesService.CurrentLives==expected,"Game "+index+" menu costs no life");
            if(index==0)
            {
                var g=UiQaVisualTour.Find<CatRunnerGameController>();Check(g!=null,"Runner loaded");UiQaVisualTour.Find<CatRunnerTrackManager>().enabled=false;
                g.StartFromWelcome();g.StartFromWelcome();Check(MiniGameLivesService.CurrentLives==--expected,"Runner double start costs exactly one");
                yield return new WaitForSeconds(3);g.PauseRun();g.ResumeRun();Check(MiniGameLivesService.CurrentLives==expected,"Runner pause/resume free");
                UiQaVisualTour.Call(g,"CompleteRun");yield return null;UiQaVisualTour.Call(g,"CollectAndRetry");UiQaVisualTour.Call(g,"CollectAndRetry");
                Check(MiniGameLivesService.CurrentLives==--expected,"Runner retry costs exactly one");yield return new WaitForSeconds(3);g.PauseRun();g.ReturnToGamesFromPause();
            }
            else if(index==1)
            {
                var g=UiQaVisualTour.Find<CatCatchGameController>();Check(g!=null,"Catch loaded");g.StartHunt();g.StartHunt();Check(MiniGameLivesService.CurrentLives==--expected,"Catch double start costs exactly one");
                g.PauseHunt();g.ResumeHunt();Check(MiniGameLivesService.CurrentLives==expected,"Catch pause/resume free");UiQaVisualTour.Call(g,"FinishHunt",true);yield return null;
                g.StartHunt();g.StartHunt();Check(MiniGameLivesService.CurrentLives==--expected,"Catch retry costs exactly one");g.PauseHunt();g.ReturnToGamesFromPause();
            }
            else
            {
                var g=UiQaVisualTour.Find<CozyMiniGame>();Check(g!=null,"Cozy "+index+" loaded");g.StartRound();g.StartRound();Check(MiniGameLivesService.CurrentLives==--expected,"Cozy "+index+" double start costs exactly one");
                g.Pause();g.Resume();Check(MiniGameLivesService.CurrentLives==expected,"Cozy "+index+" pause/resume free");g.FinishRound();yield return null;
                g.StartRound();g.StartRound();Check(MiniGameLivesService.CurrentLives==--expected,"Cozy "+index+" retry costs exactly one");g.Exit(true);
            }
            yield return ReturnHome();hub.Show();yield return null;
            Check(hub.transform.Find("SafeArea/GamesHubCard/SharedLives/Count").GetComponent<TMP_Text>().text.Contains(expected+"/20"),"Hub reflects game "+index+" spend");
        }
        yield return null;CatHomeSaveSystem.SaveNow();yield return null;
        var saved=JsonUtility.FromJson<CatHomeSaveData>(File.ReadAllText(CatHomeSaveSystem.SaveFilePath));Check(saved.miniGameLives.lives==12,"Real QA save captured remaining 12 lives");
        Lives(0);MiniGameLivesService.ApplySavedState(saved.miniGameLives,DateTime.UtcNow);Check(MiniGameLivesService.CurrentLives==12,"Saved pool applies without refill");
        for(int index=0;index<4;index++)
        {
            Lives(0);yield return Launch(hub,index);
            if(index==0){var g=UiQaVisualTour.Find<CatRunnerGameController>();g.StartFromWelcome();Check(!g.IsRunning,"Runner empty pool blocks start");g.ReturnToGamesFromWelcome();}
            else if(index==1){var g=UiQaVisualTour.Find<CatCatchGameController>();g.StartHunt();Check(!g.IsHunting,"Catch empty pool blocks start");g.ReturnToGamesFromWelcome();}
            else
            {
                var g=UiQaVisualTour.Find<CozyMiniGame>();g.StartRound();Check(!g.IsRunning,"Cozy "+index+" empty pool blocks start");
                var state=MiniGameLivesSaveState.CreateDefault(DateTime.UtcNow.AddMinutes(-10));state.lives=0;MiniGameLivesService.ApplySavedState(state,DateTime.UtcNow);yield return null;
                var button=g.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b=>b.name=="Start");Check(button.interactable,"Cozy "+index+" button reenables after refill");button.onClick.Invoke();Check(g.IsRunning&&MiniGameLivesService.CurrentLives==0,"Cozy "+index+" uses regenerated last life");g.Exit(true);
            }
            yield return ReturnHome();Check(MiniGameLivesService.CurrentLives==0,"Game "+index+" cannot make pool negative");
        }
        Lives(1);yield return Launch(hub,2);var yarn=UiQaVisualTour.Find<CozyMiniGame>();yarn.StartRound();UiQaVisualTour.Set(yarn,"clock",0f);yarn.FinishRound();yield return null;
        CatHome.Economy.EconomyService.ApplyLegacyBalances(CatHome.Economy.EconomyService.Coins,5);var position=yarn.ball.localPosition;
        yarn.RequestContinue();yarn.ConfirmContinue();yarn.ConfirmContinue();Check(yarn.IsRunning&&MiniGameLivesService.CurrentLives==0&&CatHome.Economy.EconomyService.Diamonds==3,"Diamond continuation succeeds with zero lives and charges diamonds once");
        Check(yarn.ball.localPosition==position,"Diamond continuation preserves ball position");yarn.Pause();yarn.Exit(true);yield return ReturnHome();yield return Launch(hub,2);
        yarn=UiQaVisualTour.Find<CozyMiniGame>();yarn.RequestContinue();Check(yarn.IsRunning&&MiniGameLivesService.CurrentLives==0&&CatHome.Economy.EconomyService.Diamonds==3,"Paid saved round resumes without life or diamond charge");yarn.Exit(true);yield return ReturnHome();
        Time.timeScale=1;
    }
    public static void Start()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new Exception("Copied QA session required.");
        UiQaVisualTour.Find<CatMovement>().StartCoroutine(Run());
    }
    static void Lives(int count)
    {
        var state=MiniGameLivesSaveState.CreateDefault(DateTime.UtcNow);state.lives=count;
        MiniGameLivesService.ApplySavedState(state,DateTime.UtcNow);
    }
    static IEnumerator Run()
    {
        var original=GameLanguageService.Current;var originalLives=MiniGameLivesService.CaptureState(DateTime.UtcNow);
        UiQaVisualTour.OutputDirectory=Root+"/screens";
        foreach(var language in new[]{GameLanguage.Turkish,GameLanguage.English})
        {
            GameLanguageService.SetLanguage(language);UiQaVisualTour.Clear();
            var hub=UiQaVisualTour.Find<GamesHubPanel>();string prefix=language==GameLanguage.Turkish?"TR":"EN";
            foreach(int lives in new[]{20,12,0}){Lives(lives);hub.Show();yield return Views(prefix+"-Games-"+lives);}
            hub.Hide();CozyGameProgress.SetCheckpoint(null);
            foreach(var kind in new[]{CozyGameKind.Yarn,CozyGameKind.Pond})
            {
                Lives(0);hub.Show();hub.PlayCozy(kind);
                float until=Time.realtimeSinceStartup+15;
                while(UiQaVisualTour.Find<CozyMiniGame>()==null&&Time.realtimeSinceStartup<until)yield return null;
                var game=UiQaVisualTour.Find<CozyMiniGame>();if(game==null){Status="Launch failed";yield break;}
                yield return new WaitForSecondsRealtime(.3f);yield return Views(prefix+"-"+kind+"-Empty");
                Lives(20);yield return Views(prefix+"-"+kind+"-Ready");
                game.StartRound();yield return new WaitForSecondsRealtime(.15f);game.FinishRound();Lives(0);
                yield return Views(prefix+"-"+kind+"-ResultEmpty");game.Exit(true);
                until=Time.realtimeSinceStartup+15;while(game!=null&&Time.realtimeSinceStartup<until)yield return null;
                yield return new WaitForSecondsRealtime(.2f);
            }
        }
        GameLanguageService.SetLanguage(original);MiniGameLivesService.ApplySavedState(originalLives,DateTime.UtcNow);
        UiQaVisualTour.Clear();UiQaVisualTour.Resolution(1920,1080);Lives(20);UiQaVisualTour.Find<GamesHubPanel>().Show();
        Status="Complete";File.WriteAllText(Root+"/visual-status.txt",Status);
    }
    static IEnumerator Views(string name)
    {
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
        {
            UiQaVisualTour.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.3f);Status=name+"-"+size.x;UiQaVisualTour.Capture(Status);
            var report=new StringBuilder();
            foreach(var text in Object.FindObjectsByType<TMP_Text>())
            {
                if(!text.isActiveAndEnabled||text.color.a<.03f)continue;float alpha=1;
                foreach(var group in text.GetComponentsInParent<CanvasGroup>())alpha*=group.alpha;
                if(alpha<.05f)continue;text.ForceMeshUpdate();if(text.isTextOverflowing)report.AppendLine("OVERFLOW "+text.name+": "+text.text);
            }
            File.WriteAllText(Root+"/screens/"+Status+".text.txt",report.Length==0?"No visible TMP overflow.":report.ToString());
            yield return new WaitForSecondsRealtime(.1f);
        }
    }
}
public sealed class SharedLivesReviewHost:MonoBehaviour{}
