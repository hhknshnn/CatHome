using System;
using System.Collections;
using System.IO;
using UnityEngine;
using TMPro;

/// <summary>Mini-game overlay presentation examples, without starting or settling a round.</summary>
public static class MiniPopupReview
{
    public static string Status { get; private set; } = "Idle";
    public static void Start()
    {
        if (!Application.isPlaying || !EditorQaSession.IsActive) throw new InvalidOperationException("Copied-save Play required.");
        UiQaVisualTour.Find<CatMovement>().StartCoroutine(Run());
    }
    static IEnumerator Run()
    {
        var original=GameLanguageService.Current;
        UiQaVisualTour.OutputDirectory=InGamePolishReview.Root+"/minigames";
        foreach (var lang in new[] { GameLanguage.Turkish, GameLanguage.English })
        foreach (bool runner in new[] { true, false })
        {
            UiQaVisualTour.Find<CatCompanionPanel>()?.Close(); UiQaVisualTour.Clear();
            GameLanguageService.SetLanguage(lang);
            yield return new WaitForSecondsRealtime(.8f);
            if (runner) UiQaVisualTour.Find<CatRunnerLauncher>().Launch();
            else UiQaVisualTour.Find<CatCatchLauncher>().Launch();
            float deadline=Time.realtimeSinceStartup+30;
            Component controller=null;
            while (controller==null && Time.realtimeSinceStartup<deadline)
            {
                yield return null;
                controller=runner?(Component)UiQaVisualTour.Find<CatRunnerGameController>():UiQaVisualTour.Find<CatCatchGameController>();
            }
            if (controller==null) { Status="Failed: launch"; yield break; }
            yield return new WaitForSecondsRealtime(1.5f);
            string[] panels={"welcomePanel","pausePanel","resultPanel","tutorialPanel"};
            for (int state=0;state<panels.Length;state++)
            {
                for (int i=0;i<panels.Length;i++) UiQaVisualTour.Get<GameObject>(controller,panels[i]).SetActive(i==state);
                if (state==2)
                {
                    // Display sample values only; Present formats labels and grants no reward.
                    var result=UiQaVisualTour.Get<GameObject>(controller,"resultPanel");
                    result.GetComponent<MiniGameResultView>().Present(1240,24);
                    UiQaVisualTour.Get<TMP_Text>(controller,"resultTitle").text=GameContentCopy.Text("Güzel bir oyun!","A lovely game!");
                    UiQaVisualTour.Get<TMP_Text>(controller,"resultDetails").text=GameContentCopy.Text("Birlikte küçük bir mola.","A little moment together.");
                }
                if (state==3) UiQaVisualTour.Get<TMP_Text>(controller,"tutorialText").text=GameContentCopy.Text(runner?"Şerit değiştirmek için sağa veya sola kaydır.":"Fareyi yakalamak için üzerine dokun.",runner?"Swipe left or right to change lanes.":"Tap a mouse to catch it.");
                foreach (int width in new[] { 1920,848 })
                {
                    UiQaVisualTour.Resolution(width,width==848?392:1080);
                    yield return new WaitForSecondsRealtime(.65f);
                    Status=(lang==GameLanguage.Turkish?"TR":"EN")+"-"+(runner?"Runner":"Catch")+"-"+panels[state]+"-"+width;
                    UiQaVisualTour.Capture(Status);
                    yield return new WaitForSecondsRealtime(.3f);
                }
            }
            // Only the presentation was shown; exit through the normal welcome path.
            if (runner) ((CatRunnerGameController)controller).ExitFromWelcome();
            else ((CatCatchGameController)controller).ExitToHome();
            yield return new WaitForSecondsRealtime(1.5f);
        }
        GameLanguageService.SetLanguage(original); UiQaVisualTour.Resolution(1920,1080);
        Status="Complete: 32 overlay presentation views; no round started or reward claimed.";
        File.WriteAllText(InGamePolishReview.Root+"/minigames-review.txt",Status);
    }
}
