using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// Exercises the final presentation only in the isolated editor save.
public static class ProductionVisualQa
{
    public static string Status { get; private set; } = "Idle";
    public static void Begin()
    {
        if (!Application.isPlaying || !EditorQaSession.IsActive) throw new InvalidOperationException("Isolated Play required.");
        UiQaVisualTour.Find<CatMovement>().StartCoroutine(Run());
    }
    static IEnumerator Run()
    {
        UiQaVisualTour.Find<PetTutorialHint>().enabled=false;
        // Artificial ownership in the preceding matrix may have pending rewards.
        // Keep the queue intact, but present each overlay deliberately for this tour.
        UiQaVisualTour.Find<CollectionCompleteCelebrationView>().enabled=false;
        var companion=UiQaVisualTour.Find<CatCompanionPanel>();
        foreach(int width in new[]{1920,1440})
        {
            string folder="Docs/QA/PRODUCTION_PASS_2026-09-07/"+(width==1920?"screens-wide":"screens-narrow");
            UiQaVisualTour.Resolution(width,1080);UiQaVisualTour.OutputDirectory=folder;
            foreach(string view in new[]{"01_Title","02_NewGame","03_Menu","04_Collection","05_LevelUp","06_Onboarding","07_Name","08_Dialogue","09_Rooms","10_Games"})
            {
                companion.Close();UiQaVisualTour.Clear();yield return new WaitForSecondsRealtime(.4f);
                UiQaVisualTour.Show(view);yield return new WaitForSecondsRealtime(2.2f);
                Status=view+" "+width;UiQaVisualTour.Capture(view+"_Final");yield return new WaitForEndOfFrame();
            }
            UiQaVisualTour.Clear();yield return new WaitForSecondsRealtime(.5f);
            companion.Show(false);yield return new WaitForSecondsRealtime(.5f);UiQaVisualTour.Capture("11_Companion");yield return new WaitForEndOfFrame();
            companion.Show(true);
            for(int page=0;page<HomeGuideContent.Count;page++)
            {
                UiQaVisualTour.Set(companion,"page",page);UiQaVisualTour.Call(companion,"RefreshGuide");
                yield return new WaitForSecondsRealtime(.3f);UiQaVisualTour.Capture("Guide-"+(page+1));yield return new WaitForEndOfFrame();
                var scroll=UiQaVisualTour.Get<ScrollRect>(companion,"guideScroll");
                if(scroll.content.rect.height>scroll.viewport.rect.height+1)
                {
                    scroll.verticalNormalizedPosition=0;yield return new WaitForSecondsRealtime(.2f);
                    UiQaVisualTour.Capture("Guide-"+(page+1)+"-Bottom");yield return new WaitForEndOfFrame();
                }
                Status="Guide "+(page+1)+" "+width;
            }
            companion.Close();UiQaVisualTour.Clear();yield return new WaitForSecondsRealtime(.5f);
            var dialogue=UiQaVisualTour.Find<CatDialogueView>();
            var method=typeof(PetTutorialHint).GetMethod("IntroductionLine",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            for(int step=0;step<4;step++)
            {
                dialogue.ShowMessage(PetTutorialHint.CatName,(string)method.Invoke(null,new object[]{step,PetTutorialHint.CatName}));dialogue.SetLessonProgress(step+1,4);
                yield return new WaitForSecondsRealtime(.4f);UiQaVisualTour.Capture("Introduction-"+(step+1));yield return new WaitForEndOfFrame();
            }
        }
        companion.Close();UiQaVisualTour.Clear();UiQaVisualTour.Resolution(1920,1080);Status="Complete";
    }
}
