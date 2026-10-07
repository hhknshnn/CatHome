using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Explicit, copied-save-only visual review of the two revised screens.</summary>
public static class WelcomeGlossReview
{
    const string Root="Docs/QA/WELCOME_VERTICAL_2026-10-05";
    static int index,phase;static double next;static GameLanguage language;static bool running;
    static readonly List<string> results=new List<string>();
    static readonly int[] widths={1920,1440,2400,848};
    public static string Status {get;private set;}="Idle";
    public static void Start()
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new InvalidOperationException("Use copied-save Play.");
        language=GameLanguageService.Current;index=phase=0;next=0;results.Clear();running=true;
        UiQaVisualTour.OutputDirectory=Root;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!running||!Application.isPlaying){EditorApplication.update-=Tick;return;}
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            if(index>=16)
            {
                GameLanguageService.SetLanguage(language);UiQaVisualTour.Resolution(1920,1080);UiQaVisualTour.Clear();
                UiQaVisualTour.Show("Title");Status="Complete";running=false;EditorApplication.update-=Tick;
                File.WriteAllLines(Root+"/visual-review.txt",results);return;
            }
            bool title=index%2==0;int variant=index/2;int width=widths[variant%4];
            string label=(variant<4?"TR":"EN")+"-"+width+"-"+(title?"title":"return");Status=label;
            if(phase==0)
            {
                UiQaVisualTour.Clear();GameLanguageService.SetLanguage(variant<4?GameLanguage.Turkish:GameLanguage.English);
                UiQaVisualTour.Resolution(width,width==848?392:1080);phase=1;next=EditorApplication.timeSinceStartup+.7;
            }
            else if(phase==1){UiQaVisualTour.Show(title?"Title":"ReturnExample");phase=2;next=EditorApplication.timeSinceStartup+2;}
            else
            {
                Canvas.ForceUpdateCanvases();string measurements=UiQaVisualTour.Measure();
                var scope=title?UiQaVisualTour.Find<TitleScreen>().transform:UiQaVisualTour.Find<WhileYouWereAwayPopup>().transform;
                var textProblems=new List<string>();
                foreach(var t in scope.GetComponentsInChildren<TMP_Text>())
                {
                    float alpha=1;foreach(var g in t.GetComponentsInParent<CanvasGroup>())alpha*=g.alpha;
                    if(alpha<.01f||!t.isActiveAndEnabled)continue;
                    t.ForceMeshUpdate();if(t.isTextTruncated||t.isTextOverflowing)textProblems.Add(t.name);
                }
                string report=label+" "+(measurements.Contains("OVERLAP")||measurements.Contains("OUTSIDE")||measurements.Contains("UNCLICKABLE")||textProblems.Count>0?"FAIL":"PASS")+" text="+string.Join(",",textProblems);
                results.Add(report);UiQaVisualTour.Capture(label);phase=0;index++;next=EditorApplication.timeSinceStartup+.5;
                File.WriteAllLines(Root+"/visual-review-progress.txt",results);
            }
        }
        catch(Exception e){Status="Failed: "+e;running=false;EditorApplication.update-=Tick;File.WriteAllText(Root+"/visual-review-error.txt",e.ToString());GameLanguageService.SetLanguage(language);}
    }
    public static string Tap(Button button)
    {
        if(!EditorQaSession.IsActive)throw new InvalidOperationException("Use copied-save Play.");
        Canvas.ForceUpdateCanvases();var es=EventSystem.current;
        var data=new PointerEventData(es){position=RectTransformUtility.WorldToScreenPoint(null,button.transform.position),button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();es.RaycastAll(data,hits);
        if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=button)throw new Exception("Wrong receiver for "+button.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);return button.name+" received real pointer click.";
    }
    public static void RunNative()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||!EditorQaSession.IsActive)throw new Exception("Use a copied-save Edit session.");
        UiQaTestSession.ResultDirectory=Root;UiQaTestSession.ResultFileName="PlayMode-native.xml";
        CatHomeAuthoringWorkspace.HoldFastPlayModeForManualTestRun();
        var api=ScriptableObject.CreateInstance<TestRunnerApi>();
        api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.PlayMode,testNames=new[]{
            "TitleCatShowcaseTests.FirstFrameRendersRealCats_AndClosingReleasesAllRenderResources",
            "TitleCatShowcaseTests.EverySelectedBreedDrivesItsRealSkeletonOnTheTitle",
            "TitleCatShowcaseTests.TitleRenderingRestoresTheRoomsLightingExactly",
            "TitleCatShowcaseTests.ReducedMotionAndFocusFreezeThePose_AndResumeAnimation",
            "ReturnPopupInputTests.VisibleContinueButton_ReceivesPointerAndClosesReturnScreen"}}));
    }
}
