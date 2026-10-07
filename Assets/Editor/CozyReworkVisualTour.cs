using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using Object=UnityEngine.Object;

public static class CozyReworkVisualTour
{
    public static string Status="Idle";
    static GameLanguage original;
    const string Root=CozyGamesReworkReview.Qa;
    public static void Start()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new Exception("Copied-save Play required.");
        original=GameLanguageService.Current;UiQaVisualTour.Find<CatMovement>().StartCoroutine(Run());
    }
    static IEnumerator Run()
    {
        UiQaVisualTour.OutputDirectory=Root+"/screens";
        foreach(var language in new[]{GameLanguage.Turkish,GameLanguage.English})
        {
            Time.timeScale=1;GameLanguageService.SetLanguage(language);UiQaVisualTour.Clear();
            string prefix=language==GameLanguage.Turkish?"TR":"EN";
            var hub=UiQaVisualTour.Find<GamesHubPanel>();hub.Show();yield return Views(prefix+"-Games");
            hub.Hide();var leader=UiQaVisualTour.Find<LeaderboardPanel>();leader.Show();
            foreach(var game in new[]{CompetitionGame.CatRunner,CompetitionGame.CatCatch,CompetitionGame.YarnRoute,CompetitionGame.PondPlay})
            {
                UiQaVisualTour.Call(leader,"SelectGame",game);yield return Views(prefix+"-Results-"+game);
            }
            leader.Hide();
            foreach(var kind in new[]{CozyGameKind.Yarn,CozyGameKind.Pond})
            {
                UiQaVisualTour.Clear();hub.Show();hub.PlayCozy(kind);yield return new WaitForSecondsRealtime(1.5f);
                var game=UiQaVisualTour.Find<CozyMiniGame>();if(game==null){Status="Failed to launch";yield break;}
                string page=prefix+"-"+kind;yield return Views(page+"-Welcome");
                if(kind==CozyGameKind.Pond){UiQaVisualTour.Call(game,"ShowAlbum");yield return Views(page+"-Album");UiQaVisualTour.Get<GameObject>(game,"album").SetActive(false);}
                game.StartRound();yield return new WaitForSecondsRealtime(2.5f);Time.timeScale=0;
                if(kind==CozyGameKind.Yarn)
                {
                    UiQaVisualTour.Call(game,"SetupLevel",3);
                    var e=new PointerEventData(EventSystem.current){pointerId=-1,position=game.gameCamera.WorldToScreenPoint(game.ball.position)};
                    game.PointerDown(e);e.position=game.gameCamera.WorldToScreenPoint(game.ball.position-new Vector3(.8f,0,1.6f));game.PointerDrag(e);
                }
                if(language==GameLanguage.Turkish)CozyGamesReview.Photo(game.gameCamera,kind==CozyGameKind.Yarn?"Yarn":"Pond");
                yield return Views(page+"-Play");game.Pause();yield return Views(page+"-Pause");game.Resume();
                UiQaVisualTour.Set(game,"clock",0f);game.FinishRound();yield return Views(page+"-Result");
                if(kind==CozyGameKind.Yarn)
                {
                    game.RequestContinue();yield return Views(page+"-DiamondConfirm");UiQaVisualTour.Get<GameObject>(game,"confirm").SetActive(false);
                }
                Time.timeScale=1;game.Exit(true);yield return new WaitForSecondsRealtime(1.2f);
            }
        }
        GameLanguageService.SetLanguage(original);Time.timeScale=1;UiQaVisualTour.Resolution(1920,1080);UiQaVisualTour.Clear();UiQaVisualTour.Find<GamesHubPanel>().Show();
        Status="Complete";File.WriteAllText(Root+"/visual-tour.txt",Status);AssetDatabase.Refresh();
    }
    static IEnumerator Views(string name)
    {
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1440,1080),new Vector2Int(2340,1080)})
        {
            UiQaVisualTour.Resolution(size.x,size.y);yield return new WaitForSecondsRealtime(.25f);
            Status=name+"-"+size.x;UiQaVisualTour.Capture(Status);
            var report=new System.Text.StringBuilder();
            foreach(var label in Object.FindObjectsByType<TMPro.TMP_Text>())
            {
                if(!label.isActiveAndEnabled||label.color.a<.03f)continue;float alpha=1;
                foreach(var group in label.GetComponentsInParent<CanvasGroup>())alpha*=group.alpha;
                if(alpha<.05f)continue;label.ForceMeshUpdate();if(label.isTextOverflowing)report.AppendLine("OVERFLOW "+label.name+": "+label.text);
            }
            File.WriteAllText(Root+"/screens/"+Status+".text.txt",report.Length==0?"No visible TMP overflow.":report.ToString());
            yield return new WaitForSecondsRealtime(.08f);
        }
    }
}

