#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed partial class GameAudioTests
{
    CatMovement coffeeCat;
    LivingFurnitureActivity coffee;
    Vector3 coffeeLanding;
    IEnumerator PrepareCoffee()
    {
        yield return NormalHome(); yield return EnterHome(); yield return DismissReturnPopup();
        coffeeCat = Object.FindAnyObjectByType<CatMovement>();
        coffeeCat.GetComponent<CatIdleBehavior>().enabled = false;
        coffee = CatActivity.Registered.OfType<LivingFurnitureActivity>().Single(a => a.Kind == CatActivityKind.CoffeeTablePlay);
        coffeeLanding = coffee.transform.TransformPoint((Vector3)typeof(LivingFurnitureActivity).GetField("toyLanding", Private).GetValue(coffee));
        audio.SendMessage("OnApplicationFocus", true); audio.SendMessage("OnApplicationPause", false);
    }
    void StartCoffee()
    {
        CatActionState.CancelForTransition(coffeeCat);
        var cc = coffeeCat.GetComponent<CharacterController>(); cc.enabled = false;
        var p = coffee.RoutineEntryPoint.position; p.y = coffeeCat.transform.position.y;
        coffeeCat.transform.SetPositionAndRotation(p, Quaternion.identity); cc.enabled = true;
        Physics.SyncTransforms(); RoomPlayModeSupport.ProvisionNeeds();
        heard.Clear(); Assert.That(coffee.TryStart(coffeeCat), Is.True,
            "Coffee start: busy="+CatActionState.IsBusy(coffeeCat)+" blocked="+coffeeCat.AreWorldActionsBlocked+
            " away="+WhileYouWereAwayPopup.IsAnyOpen+" level="+HomeLevelUpCelebrationView.IsAnyOpen);
    }
    IEnumerator FinishCoffee()
    {
        float until = Time.realtimeSinceStartup + 20;
        while (coffee.IsRunning && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(coffee.IsRunning, Is.False);
        yield return RoomPlayModeSupport.WaitForMovementRelease(coffeeCat);
    }
    [Serializable] sealed class FloorImpact { public int fps,events; public float positionError; public bool afterPaw; public string clip; }
    [Serializable] sealed class ImpactReport { public FloorImpact[] rows; }

    [UnityTest] public IEnumerator CoffeeTable_FloorImpactAtContact_ThreeFrameRates()
    {
        yield return PrepareCoffee();
        float oldCapture = Time.captureDeltaTime;
        var rows = new List<FloorImpact>(); FloorImpact current = null;
        Action<AudioCue,AudioBus> listen = (cue,bus) =>
        {
            if (cue != AudioCue.PropLand || current == null) return;
            current.events++; current.positionError = Vector3.Distance(coffee.Toy.position, coffeeLanding);
            current.afterPaw = coffee.DidPush;
            current.clip = Object.FindObjectsByType<AudioSource>()
                .Where(s => s.isPlaying && s.clip != null && s.clip.name.StartsWith("PropLand_"))
                .OrderBy(s => s.time).FirstOrDefault()?.clip.name;
        };
        GameAudio.Played += listen;
        try
        {
            foreach (int fps in new[]{15,30,60})
            {
                Time.captureFramerate = fps; current = new FloorImpact{fps=fps}; rows.Add(current);
                StartCoffee(); yield return FinishCoffee();
                Assert.That(current.events, Is.EqualTo(1), "One first-floor impact; no extra event on prop reset");
                Assert.That(current.positionError, Is.LessThan(.001f), "Impact must share the landing frame");
                Assert.That(current.afterPaw, Is.True); Assert.That(current.clip, Does.StartWith("PropLand_"));
            }
            Assert.That(rows.Select(r => r.clip).Distinct().Count(), Is.EqualTo(3), "Use all authored takes");
            string folder = UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Temp");
            File.WriteAllText(Path.Combine(folder,"coffee-floor-impact.json"), JsonUtility.ToJson(new ImpactReport{rows=rows.ToArray()},true));
        }
        finally { GameAudio.Played -= listen; Time.captureDeltaTime = oldCapture; }
    }

    [UnityTest] public IEnumerator CoffeeTable_PausedOrCancelledFallIsQuiet_ThenReplayWorks()
    {
        yield return PrepareCoffee(); Vector3 original = coffee.Toy.position;
        StartCoffee(); float until = Time.realtimeSinceStartup + 15;
        while ((!coffee.DidPush || coffee.Toy.position.y > coffee.Perch.position.y - .08f) && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(coffee.DidPush, Is.True); Assert.That(coffee.Toy.position.y, Is.GreaterThan(coffeeLanding.y + .05f));
        Vector3 paused = coffee.Toy.position; Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.That(Vector3.Distance(coffee.Toy.position,paused), Is.LessThan(.0001f));
        Assert.That(heard.Count(c => c == AudioCue.PropLand), Is.Zero);
        coffee.CancelForTransition(); Time.timeScale = 1;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(Vector3.Distance(coffee.Toy.position,original), Is.LessThan(.001f));
        Assert.That(heard.Count(c => c == AudioCue.PropLand), Is.Zero);
        StartCoffee(); yield return FinishCoffee();
        Assert.That(heard.Count(c => c == AudioCue.PropLand), Is.EqualTo(1));
        HomeAudioService.SoundEnabled = false; StartCoffee(); yield return FinishCoffee();
        Assert.That(heard.Count(c => c == AudioCue.PropLand), Is.Zero, "Sound preference also mutes the new landing");
    }
}
#endif
