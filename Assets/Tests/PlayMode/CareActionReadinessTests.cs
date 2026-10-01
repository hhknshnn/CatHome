#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Object=UnityEngine.Object;

public sealed class CareActionReadinessTests
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic;
    CareAlignmentPolishTests fixture;
    static T Read<T>(object o,string n)=>(T)o.GetType().GetField(n,F).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,F).Invoke(o,a);
    CatMovement Cat=>Read<CatMovement>(fixture,"cat");
    BowlInteraction Bowls=>Read<BowlInteraction>(fixture,"bowls");
    SleepInteraction Sleep=>Read<SleepInteraction>(fixture,"sleep");
    static string Output=>UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory","");
    [SetUp] public void Before(){fixture=new CareAlignmentPolishTests();fixture.Before();}
    [TearDown] public void After(){Object.FindAnyObjectByType<MobileJoystick>()?.CancelInput();fixture?.After();}
    IEnumerator Boot(string breed){yield return (IEnumerator)Call(fixture,"Home");yield return (IEnumerator)Call(fixture,"Breed",breed);Call(fixture,"ReadyNeeds");}
    Button Button=>Read<Button>(Bowls,"interactionButton");
    string Label=>Read<TMP_Text>(Bowls,"buttonText").text;
    void Tap()=>Tap(Button);
    void Tap(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)button.transform;
        var canvas=button.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
        var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        Assert.That(hits.Any(h=>h.gameObject.transform.IsChildOf(button.transform)),Is.True,"Real HUD button is hit by the pointer; point="+point+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name)));
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
    }
    IEnumerator Walk(Vector3 direction,Func<bool> reached,float minimumTravel=.15f)
    {
        var joystick=Object.FindAnyObjectByType<MobileJoystick>();Assert.That(joystick,Is.Not.Null);
        typeof(CatMovement).GetField("cameraTransform",F).SetValue(Cat,null);
        typeof(CatMovement).GetField("mobileJoystick",F).SetValue(Cat,joystick);
        Call(Bowls,"Update");
        var rect=(RectTransform)joystick.transform;var v=new Vector2(direction.x,direction.z).normalized*.22f;
        var local=Vector2.Scale(v,rect.rect.size*.5f);
        var canvas=joystick.GetComponentInParent<Canvas>();
        var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(local));
        var data=new PointerEventData(EventSystem.current){position=point,pointerId=27,pointerPressRaycast=new RaycastResult{module=canvas.GetComponent<GraphicRaycaster>()}};
        var before=Cat.transform.position;float until=Time.realtimeSinceStartup+14;
        joystick.OnPointerDown(data);
        try{while(!reached()&&Time.realtimeSinceStartup<until){joystick.OnDrag(data);yield return null;}}
        finally{joystick.OnPointerUp(data);}
        Assert.That(reached(),Is.True,"Normal joystick approach must reach a usable stance; position="+Cat.transform.position+" label="+Label);
        Assert.That(Vector3.Distance(before,Cat.transform.position),Is.GreaterThanOrEqualTo(minimumTravel));
        yield return null;yield return null;
    }
    [UnityTest,Timeout(180000)] public IEnumerator JoystickApproach_FoodAndWater_OnlyReadyButtonsStartAndCompleteForTwoBreeds()
    {
        foreach(string breed in new[]{"persian","oriental-shorthair"})
        {
            yield return Boot(breed);
            foreach(string kind in new[]{"food","water"})
            {
                Call(fixture,"ReadyNeeds");Read<EnergySystem>(fixture,"energy").ApplySavedValue(0);
                var setup=Read<BowlInteraction.BowlSetup>(Bowls,kind);setup.Fill();
                Read<BowlInteraction.BowlSetup>(Bowls,kind=="food"?"water":"food").Empty();
                var target=setup.ContactPoint;var outward=setup.InteractionPoint.position-target.position;outward.y=0;outward.Normalize();
                Call(fixture,"Place",target.position+outward*.65f,Quaternion.LookRotation(-outward));
                Call(Bowls,"Update");
                Assert.That(Bowls.HasVisibleAction,Is.False,"Nearby but unreachable care must not offer an action.");
                var p=Cat.transform.position;
                yield return Walk(-outward,()=>Bowls.HasVisibleAction&&CatMealHeadMotion.TryPrepareBowlPose(Cat,target,out _));
                Assert.That(Button.targetGraphic.mainTexture,Is.SameAs(Resources.Load<Sprite>("PremiumHudFinal/action-coral").texture));
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(Output,breed+"-"+kind+"-button.png"));
                p=Cat.transform.position;Tap();Assert.That(Bowls.IsInteracting,Is.True,breed+" "+kind);Assert.That(Cat.transform.position,Is.EqualTo(p));
                Assert.That(Cat.GetComponent<CatSpeechBubble>().IsOwnedBy(Bowls),Is.False,"Successful care dismisses its previous approach hint.");
                bool contact=false;float until=Time.realtimeSinceStartup+20;
                while(Bowls.IsInteracting&&Time.realtimeSinceStartup<until)
                {
                    if(!contact&&Bowls.IsAtContact)
                        ScreenCapture.CaptureScreenshot(Path.Combine(Output,breed+"-"+kind+".png"));
                    contact|=Bowls.IsAtContact;yield return null;
                }
                Assert.That(contact,Is.True);Assert.That(Bowls.IsInteracting,Is.False);Assert.That(Cat.IsMovementPhysicallyLocked,Is.False);
                float need=kind=="food"?Read<HungerSystem>(fixture,"hunger").CurrentHunger:Read<ThirstSystem>(fixture,"thirst").CurrentThirst;
                Assert.That(need,Is.GreaterThan(70));
            }
        }
    }
    [UnityTest,Timeout(120000)] public IEnumerator JoystickApproach_Bed_SleepsAndWakesForTwoBreeds()
    {
        foreach(string breed in new[]{"persian","oriental-shorthair"})
        {
            yield return Boot(breed);Read<EnergySystem>(fixture,"energy").ApplySavedValue(0);
            var floor=Read<Transform>(Sleep,"bedInteractionPoint");var d=Sleep.SleepSurface.position-floor.position;d.y=0;d.Normalize();
            Call(fixture,"Place",floor.position-d*.6f,Quaternion.LookRotation(d));
            yield return Walk(d,()=>Bowls.HasVisibleAction&&Read<bool>(Bowls,"showingSleepStyle")&&Label==GameInteractionCopy.Action("SLEEP"));
            Assert.That(Button.targetGraphic.mainTexture,Is.SameAs(Resources.Load<Sprite>("PremiumHudFinal/action-coral").texture));
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Output,breed+"-sleep-button.png"));
            var p=Cat.transform.position;Tap();Assert.That(Sleep.IsSleeping,Is.True);Assert.That(Cat.transform.position,Is.EqualTo(p));
            float until=Time.realtimeSinceStartup+15;
            while(!Sleep.IsSettledOnBed&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(Sleep.IsSettledOnBed,Is.True,breed);yield return new WaitForSeconds(2);
            Assert.That(Read<EnergySystem>(fixture,"energy").CurrentEnergy,Is.GreaterThan(0));
            ScreenCapture.CaptureScreenshot(Path.Combine(Output,breed+"-sleep.png"));
            while(Read<Coroutine>(Sleep,"sleepCoroutine")!=null&&Time.realtimeSinceStartup<until)yield return null;
            Call(Bowls,"Update");Assert.That(Label,Is.EqualTo(GameInteractionCopy.Action("WAKE UP")));Tap();
            until=Time.realtimeSinceStartup+15;while(Sleep.IsSleeping&&Time.realtimeSinceStartup<until)yield return null;
            Assert.That(Sleep.IsSleeping,Is.False);Assert.That(Cat.IsMovementPhysicallyLocked,Is.False);
        }
    }
    [UnityTest,Timeout(120000)] public IEnumerator Sofa_UserStanceHidesThenJoystickLaunchRestsAndReturns_CurtainPersists()
    {
        foreach(string breed in new[]{"persian","oriental-shorthair"})
        {
            yield return Boot(breed);yield return new WaitForSecondsRealtime(1);
            var curtains=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t=>t.name=="ShortWindowCurtains");
            Assert.That(curtains.position.x,Is.EqualTo(3.63f).Within(.0001f));
            Assert.That(curtains.position.z,Is.EqualTo(.648384f).Within(.0001f));
            var sofa=Object.FindObjectsByType<LivingFurnitureActivity>(FindObjectsSortMode.None).Single(a=>a.Kind==CatActivityKind.SofaLounge);
            var prompt=Object.FindAnyObjectByType<ActivityPromptController>();var button=Read<Button>(prompt,"actionButton");
            Call(fixture,"Place",new Vector3(2.2578938f,.05f,-.0647035f),Quaternion.Euler(0,159.8f,0));
            yield return null;Call(Bowls,"Update");Call(prompt,"RefreshImmediate");yield return new WaitForEndOfFrame();
            Assert.That(button.isActiveAndEnabled,Is.False,"The old misleading sofa offer is hidden at the user's exact pose.");
            var before=Cat.transform.position;
            var direction=sofa.Perch.position-sofa.RoutineEntryPoint.position;direction.y=0;direction.Normalize();
            // Recover from the user's exact rejected pose with real joystick
            // input, rather than resetting the actor to an easier starting point.
            yield return Walk(direction,()=>sofa.TryGetStartPose(Cat,out _),0f);
            Call(prompt,"RefreshImmediate");yield return new WaitForEndOfFrame();
            Assert.That(Read<TMP_Text>(prompt,"actionLabel").text,Is.EqualTo(GameInteractionCopy.Action("JUMP ON SOFA")));
            before=Cat.transform.position;Tap(button);Assert.That(sofa.IsRunning,Is.True);Assert.That(Cat.transform.position,Is.EqualTo(before));
            Assert.That(Cat.GetComponent<CatSpeechBubble>().IsOwnedBy(sofa),Is.False,"Accepted launch dismisses the previous approach hint.");
            float deadline=Time.realtimeSinceStartup+15;
            while(!sofa.IsResting&&sofa.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(sofa.IsResting,Is.True,breed+" reaches the sofa");
            yield return new WaitForSeconds(1);
            // Rebuild static labels after the editor's restored dynamic font cache.
            Canvas.ForceUpdateCanvases();
            foreach(var text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))text.ForceMeshUpdate(false,true);
            foreach(var portrait in Object.FindObjectsByType<SelectedCatPortrait>(FindObjectsSortMode.None))portrait.Refresh();
            yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Output,"sofa-rest-"+breed+".png"));
            Call(prompt,"RefreshImmediate");Tap(button);
            deadline=Time.realtimeSinceStartup+15;while(sofa.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(sofa.IsRunning,Is.False);Assert.That(Cat.IsMovementPhysicallyLocked,Is.False);
            Assert.That(Cat.transform.position.y,Is.LessThan(.15f));
        }
    }
    [UnityTest,Timeout(90000)] public IEnumerator TableAction_UsesNewArtworkAndLocalizedRealHudClick()
    {
        var language=GameLanguageService.Current;
        try
        {
            yield return Boot("persian");
            yield return new WaitForSecondsRealtime(1);
            var table=Object.FindObjectsByType<LivingFurnitureActivity>(FindObjectsSortMode.None).Single(a=>a.Kind==CatActivityKind.CoffeeTablePlay);
            yield return RoomPlayModeSupport.WaitForMovementRelease(Cat);
            table.TryGetStartPose(Cat,out var start);
            var origin=start.ZoneCentre;origin.y=.05f;
            bool ready=false;
            // Fixture explores the existing legal stance region; production never relocates the cat.
            foreach(float radius in new[]{0f,.06f,.12f,.18f,.215f})
            {
                for(int angle=0;angle<12&&!ready;angle++)
                {
                    var point=origin+Quaternion.Euler(0,angle*30,0)*Vector3.forward*radius;
                    var direction=start.ActionTarget-point;direction.y=0;
                    foreach(float yaw in new[]{0f,-15f,15f,-30f,30f})
                    {
                        var rotation=Quaternion.AngleAxis(yaw,Vector3.up)*Quaternion.LookRotation(direction);
                        if(!CatActivityMotion.TryFloorPath(Vector3.zero,point,out _)||!Cat.IsInteractionPoseClear(point,rotation))continue;
                        Call(fixture,"Place",point,rotation);ready=table.TryGetStartPose(Cat,out _);if(ready)break;
                    }
                }
                if(ready)break;
            }
            Assert.That(ready,Is.True,"Table has a valid real launch stance");
            yield return null;yield return null;
            var prompt=Object.FindAnyObjectByType<ActivityPromptController>();
            var button=Read<Button>(prompt,"actionButton");
            foreach(var selected in new[]{GameLanguage.Turkish,GameLanguage.English})
            {
                GameLanguageService.SetLanguage(selected);Call(Bowls,"Update");Call(prompt,"RefreshImmediate");
                yield return null;yield return new WaitForEndOfFrame();
                Assert.That(Read<CatActivity>(prompt,"candidate"),Is.SameAs(table));
                Assert.That(button.isActiveAndEnabled&&button.interactable,Is.True);
                Assert.That(button.targetGraphic.mainTexture,Is.SameAs(Resources.Load<Sprite>("PremiumHudFinal/action-coral").texture));
                var label=Read<TMP_Text>(prompt,"actionLabel");label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing,Is.False,label.text);
                Assert.That(label.text,Is.EqualTo(GameInteractionCopy.Action("JUMP ON TABLE")));
                ScreenCapture.CaptureScreenshot(Path.Combine(Output,"table-button-"+Screen.width+"-"+selected+".png"));
            }
            var before=Cat.transform.position;Tap(button);
            Assert.That(table.IsRunning,Is.True);Assert.That(Cat.transform.position,Is.EqualTo(before));
            float deadline=Time.realtimeSinceStartup+22;
            while(table.IsRunning&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.That(table.IsRunning,Is.False);Assert.That(table.DidPush,Is.True);
            Assert.That(Cat.IsMovementPhysicallyLocked,Is.False);
        }
        finally{GameLanguageService.SetLanguage(language);}
    }
}
#endif
