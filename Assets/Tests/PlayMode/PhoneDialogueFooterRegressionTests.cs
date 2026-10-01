#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class PhoneDialogueFooterRegressionTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    PhoneOnboardingGuidanceTests home;
    GameLanguage language;
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","Docs/QA/OVERNIGHT_FIX_POLISH_2026-09-27");
    static T Get<T>(object target,string name)=>(T)target.GetType().GetField(name,Private).GetValue(target);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
    [SetUp]public void Before(){home=new PhoneOnboardingGuidanceTests();home.Before();language=GameLanguageService.Current;}
    [TearDown]public void After(){GameLanguageService.SetLanguage(language);home?.After();}
    [UnityTest,Timeout(90000)]
    public IEnumerator IntroAndName_StayAboveActualFooter_WithReadableCopyAndLiveInput()
    {
        yield return (IEnumerator)Call(home,"Home");
        var hint=Object.FindAnyObjectByType<PetTutorialHint>();var dialogue=Get<CatDialogueView>(hint,"dialogue");hint.enabled=false;Call(hint,"SetAllHidden");
        foreach(var popup in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include))popup.enabled=false;
        foreach(var popup in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include))popup.enabled=false;
        foreach(var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include)){popup.Close();popup.enabled=false;}
        yield return null;
        var dock=Object.FindAnyObjectByType<PremiumHomeDockLayout>();Assert.That(dock,Is.Not.Null);
        var footer=dock.transform.Find("DockEnamelTray") as RectTransform;Assert.That(footer,Is.Not.Null);
        var failures=new List<string>();var rows=new List<string>{"width,height,language,mode,gap,messageOverflow"};
        foreach(var lang in new[]{GameLanguage.Turkish,GameLanguage.English})
        {
            GameLanguageService.SetLanguage(lang);
            foreach(bool naming in new[]{false,true})
            {
                if(naming)dialogue.ShowNamePrompt(GameContentCopy.Text("Birlikte çok güzel anılar biriktireceğiz. Bana ne isim vermek istersin?","We will make lovely memories together. What would you like to call me?"));
                else dialogue.ShowMessage("Misket",GameContentCopy.Text("Burası bizim yuvamız! Mama, su ve güzel bir uykuyla her güne birlikte başlayabiliriz.","This is our home! With food, water and a cosy nap, we can start every day together."));
                float until=Time.realtimeSinceStartup+.35f;
                while(Time.realtimeSinceStartup<until)
                {
                    yield return new WaitForEndOfFrame();
                    var moving=Get<RectTransform>(dialogue,"panel");
                    if(dialogue.IsVisible&&ScreenRect(moving).yMin<ScreenRect(footer).yMax-1f)
                        failures.Add("Entrance overlaps footer: "+lang+" name="+naming);
                }
                Canvas.ForceUpdateCanvases();
                var panel=Get<RectTransform>(dialogue,"panel");var message=Get<TMP_Text>(dialogue,"messageLabel");message.ForceMeshUpdate();
                Rect bounds=ScreenRect(panel),foot=ScreenRect(footer);float gap=bounds.yMin-foot.yMax;
                string label=Screen.width+"x"+Screen.height+"-"+lang+"-"+(naming?"name":"intro");
                rows.Add(label+","+gap.ToString(System.Globalization.CultureInfo.InvariantCulture)+","+message.isTextOverflowing);
                var pixels=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(Output,"dialogue-footer-"+label+".png"),pixels.EncodeToPNG());}finally{Object.Destroy(pixels);}
                float minimumGap=12f*Mathf.Abs(panel.lossyScale.y);
                if(gap<minimumGap||message.isTextOverflowing||bounds.yMax>Screen.safeArea.yMax+1)failures.Add(label+" gap="+gap+" overflow="+message.isTextOverflowing);
                Button button=Get<Button>(dialogue,naming?"confirm":"panelButton");
                if(naming){Get<TMP_InputField>(dialogue,"input").text="Misket";Assert.That(button.interactable,Is.True);}
                var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=ScreenRect((RectTransform)button.transform).center},hits);
                Assert.That(hits,Is.Not.Empty,"Dialogue receives actual raycast "+label);
                Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),"Another window must not cover the tested dialogue: "+label);
            }
        }
        File.WriteAllLines(Path.Combine(Output,"dialogue-footer-"+Screen.width+"x"+Screen.height+".csv"),rows);
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    static Rect ScreenRect(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);var canvas=rect.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        Vector2 min=RectTransformUtility.WorldToScreenPoint(camera,corners[0]),max=min;
        foreach(var corner in corners){Vector2 p=RectTransformUtility.WorldToScreenPoint(camera,corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
}
#endif
