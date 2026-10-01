#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Actual home HUD and copied-save fixture. A nearby offer must be discoverable
// without weakening the fresh physical admission checks performed on click.
public sealed class PhoneBowlPromptTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    GameObject obstruction;
    readonly List<string> measurements = new List<string>();
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/PHONE_BOWL_PROMPTS_2026-09-23");
    static T Read<T>(object owner, string field) => (T)owner.GetType().GetField(field, Private).GetValue(owner);
    static object Call(object owner, string method, params object[] values) => owner.GetType().GetMethod(method, Private).Invoke(owner, values);
    CatMovement Cat => Read<CatMovement>(home, "cat");
    BowlInteraction Bowls => Read<BowlInteraction>(home, "bowls");
    HungerSystem Hunger => Read<HungerSystem>(home, "hunger");
    ThirstSystem Thirst => Read<ThirstSystem>(home, "thirst");
    EnergySystem Energy => Read<EnergySystem>(home, "energy");
    BowlInteraction.BowlSetup Setup(string kind) => Read<BowlInteraction.BowlSetup>(Bowls, kind);
    static Transform Target(BowlInteraction.BowlSetup setup) => setup.ContactPoint != null ? setup.ContactPoint : setup.Bowl;

    [SetUp] public void Before()
    {
        home = new CareAlignmentPolishTests();
        home.Before();
        Time.captureDeltaTime = 0;
        measurements.Clear();
        measurements.Add("test,kind,samples,p95_ms,max_ms");
    }

    [TearDown] public void After()
    {
        if (obstruction != null) Object.DestroyImmediate(obstruction);
        if (Cat != null)
        {
            var joystick = Object.FindAnyObjectByType<MobileJoystick>();
            if (joystick != null) joystick.CancelInput();
        }
        home?.After();
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "phone-bowl-" + TestContext.CurrentContext.Test.Name + ".csv"), measurements);
    }

    IEnumerator Boot()
    {
        yield return (IEnumerator)Call(home, "Home");
        CatBreedService.Select("russian-blue");
        yield return QaBreedReadiness.WaitForSelected(Cat, "russian-blue");
        Call(home, "ReadyNeeds");
        var idle = Cat.GetComponent<CatIdleBehavior>();
        if (idle != null) idle.enabled = false;
        Assert.That(Cat.IsMovementLocked, Is.False);
    }

    void Isolate(string kind)
    {
        Setup(kind).Fill();
        Setup(kind == "food" ? "water" : "food").Empty();
    }

    Vector3 Outward(BowlInteraction.BowlSetup setup)
    {
        Vector3 direction = setup.InteractionPoint.position - Target(setup).position;
        direction.y = 0;
        return direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.back;
    }

    void PlaceNear(BowlInteraction.BowlSetup setup, float distance, float heading)
    {
        Vector3 outward = Outward(setup);
        Call(home, "Place", Target(setup).position + outward * distance,
            Quaternion.LookRotation(-outward) * Quaternion.Euler(0, heading, 0));
    }

    Button Offered(BowlInteraction.BowlSetup setup)
    {
        Call(Bowls, "Update");
        Assert.That(Read<BowlInteraction.BowlSetup>(Bowls, "currentBowl"), Is.SameAs(setup));
        var button = Read<Button>(Bowls, "interactionButton");
        Assert.That(button != null && button.isActiveAndEnabled && button.interactable, Is.True);
        Assert.That(button.GetComponentInParent<Canvas>(), Is.Not.Null, "Exercise the authored HUD, not a synthetic test button.");
        return button;
    }

    void AssertNoCare(Vector3 position, Quaternion rotation, float hunger, float thirst)
    {
        Assert.That(Bowls.IsInteracting || Hunger.IsEating || Thirst.IsDrinking, Is.False);
        Assert.That(Cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(Cat.transform.position, Is.EqualTo(position), "A rejected nearby offer cannot warp the player.");
        Assert.That(Cat.transform.rotation, Is.EqualTo(rotation));
        Assert.That(Hunger.CurrentHunger, Is.EqualTo(hunger));
        Assert.That(Thirst.CurrentThirst, Is.EqualTo(thirst));
    }

    void AssertVisibleFeedback(string expected = null)
    {
        var bubble = Cat.GetComponent<CatSpeechBubble>();
        Assert.That(bubble, Is.Not.Null);
        var label = Read<TMP_Text>(bubble, "label");
        Assert.That(label, Is.Not.Null);
        Assert.That(label.text, Is.Not.Null.And.Not.Empty);
        if (expected != null) Assert.That(label.text, Is.EqualTo(expected));
        Assert.That(Read<CanvasGroup>(bubble, "group").alpha, Is.GreaterThan(0));
    }


    void Hidden()
    {
        Call(Bowls,"Update");
        Assert.That(Bowls.HasVisibleAction,Is.False);
        Assert.That(Read<Button>(Bowls,"interactionButton").isActiveAndEnabled,Is.False);
    }
    void ReadyPose(BowlInteraction.BowlSetup setup)
    {
        Call(home,"FindCareStance",Target(setup),setup.InteractionPoint,new Func<bool>(() =>
            CatMealHeadMotion.TryPrepareBowlPose(Cat,Target(setup),out _) &&
            CatMealHeadMotion.TryPrepareCareStart(Cat,Target(setup),out _)));
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator WrongHeadingsDistanceEmptyAndFullNeeds_NeverOfferActions()
    {
        yield return Boot();Time.timeScale=0;
        foreach(string kind in new[]{"food","water"})
        {
            Isolate(kind);Call(home,"ReadyNeeds");var setup=Setup(kind);
            foreach(float heading in new[]{0f,90f,180f,270f}){PlaceNear(setup,.65f,heading);Hidden();}
            ReadyPose(setup);yield return new WaitForSecondsRealtime(.2f);Offered(setup);
            setup.Empty();Hidden();setup.Fill();
            Hunger.ApplySavedValue(100);Thirst.ApplySavedValue(100);Hidden();
            Call(home,"ReadyNeeds");ReadyPose(setup);yield return new WaitForSecondsRealtime(.2f);Offered(setup);
            setup.Bowl.gameObject.SetActive(false);Hidden();setup.Bowl.gameObject.SetActive(true);
            PlaceNear(setup,2,0);Hidden();
        }
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator SameFrameObstacle_WithdrawsStaleButtonWithoutSpeechMovementOrReward()
    {
        yield return Boot();Isolate("food");var setup=Setup("food");ReadyPose(setup);
        yield return new WaitForSecondsRealtime(.2f);var button=Offered(setup);Time.timeScale=0;
        var position=Cat.transform.position;var rotation=Cat.transform.rotation;
        float hunger=Hunger.CurrentHunger,thirst=Thirst.CurrentThirst;
        var bubble=Cat.GetComponent<CatSpeechBubble>();string before=Read<TMP_Text>(bubble,"label")?.text;
        obstruction=new GameObject("Fresh blocker",typeof(BoxCollider));
        SceneManager.MoveGameObjectToScene(obstruction,Cat.gameObject.scene);
        obstruction.transform.position=position+Vector3.up*.25f;
        obstruction.GetComponent<BoxCollider>().size=new Vector3(.7f,.5f,.7f);Physics.SyncTransforms();
        button.onClick.Invoke();AssertNoCare(position,rotation,hunger,thirst);Hidden();
        Assert.That(Read<TMP_Text>(bubble,"label")?.text,Is.EqualTo(before),"Stale action must not produce advice.");
    }
    [UnityTest,Timeout(90000)]
    public IEnumerator FullNeedOnClick_WithdrawsStaleButtonWithoutAdvice()
    {
        yield return Boot();Time.timeScale=0;
        foreach(string kind in new[]{"food","water"})
        {
            Call(home,"ReadyNeeds");Isolate(kind);var setup=Setup(kind);ReadyPose(setup);
            yield return new WaitForSecondsRealtime(.2f);var button=Offered(setup);
            Hunger.ApplySavedValue(100);Thirst.ApplySavedValue(100);Energy.ApplySavedValue(0);
            var position=Cat.transform.position;var rotation=Cat.transform.rotation;
            var bubble=Cat.GetComponent<CatSpeechBubble>();string before=Read<TMP_Text>(bubble,"label")?.text;
            button.onClick.Invoke();AssertNoCare(position,rotation,100,100);Hidden();
            Assert.That(Read<TMP_Text>(bubble,"label")?.text,Is.EqualTo(before));
        }
    }
    [UnityTest,Timeout(120000)]
    public IEnumerator WarmPolling_RejectsWrongHeadingsAndKeepsCheapFrameBudget()
    {
        yield return Boot();Time.timeScale=0;var timer=new Stopwatch();
        foreach(string kind in new[]{"food","water"})
        {
            Isolate(kind);var setup=Setup(kind);var durations=new List<double>();
            for(int i=0;i<160;i++)
            {
                PlaceNear(setup,.60f,i%4*90f);timer.Restart();Call(Bowls,"Update");timer.Stop();
                Assert.That(Bowls.HasVisibleAction,Is.False);
                if(i>=20)durations.Add(timer.Elapsed.TotalMilliseconds);
                if(i%20==0)yield return null;
            }
            durations.Sort();double p95=durations[(int)(durations.Count*.95f)];
            measurements.Add(kind+","+durations.Count+","+p95.ToString("F5",CultureInfo.InvariantCulture));
            Assert.That(p95,Is.LessThan(5));
        }
    }
}
#endif
