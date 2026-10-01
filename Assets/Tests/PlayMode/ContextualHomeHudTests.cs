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
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Full GameScene + the real additive home HUD. Only the QA player is placed;
// neither the prompt candidate nor any activity geometry/animation is changed.
public sealed class ContextualHomeHudTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    GameLanguage savedLanguage;
    bool hadLanguage;
    int savedLanguagePreference;
    long coins, diamonds, bond;
    int chapter;
    QuestProgressEntry[] quests;
    ActivityPromptController prompt;
    CatActivity observed;
    int completions;
    string completionCopy;
    readonly List<string> rows = new List<string>();
    readonly List<string> performanceFailures=new List<string>();
    readonly List<string> frameRows=new List<string>();
    float baselineMax,baselineP95;
    CatMovement Cat => Read<CatMovement>(home, "cat");
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory",
        "Docs/QA/INTERACTION_POLISH_2026-09-16");
    static T Read<T>(object target, string name) =>
        (T)target.GetType().GetField(name, Private).GetValue(target);
    static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);
    static string Csv(string text) => "\"" + (text ?? "").Replace("\"", "\"\"") + "\"";

    [SetUp] public void Before()
    {
        rows.Clear();performanceFailures.Clear();frameRows.Clear();baselineMax=baselineP95=0;
        home = new CareAlignmentPolishTests(); home.Before();
        savedLanguage = GameLanguageService.Current;
        hadLanguage = PlayerPrefs.HasKey(GameLanguageService.PlayerPrefsKey);
        savedLanguagePreference = PlayerPrefs.GetInt(GameLanguageService.PlayerPrefsKey);
        coins = ProgressionService.Coins; diamonds = ProgressionService.Diamonds;
        bond = ProgressionService.BondXp; chapter = ProgressionService.CurrentChapterNumber;
        quests = ProgressionService.CaptureQuestProgress();
        // Measure actual editor frames, not the synthetic 60 Hz care-fixture clock.
        Time.captureDeltaTime = 0;
        rows.Add("test,language,product,attempt,stage,clickMs,worstStartFrameMs,startFrames,rootXZ,rootYaw,walkFrames,detail");
        CatActivity.Completed += OnCompleted;
    }
    [TearDown] public void After()
    {
        CatActivity.Completed -= OnCompleted;
        home.After();
        ProgressionService.ApplySavedState(coins, diamonds, bond, chapter, quests);
        GameLanguageService.SetLanguage(savedLanguage);
        if (hadLanguage) PlayerPrefs.SetInt(GameLanguageService.PlayerPrefsKey, savedLanguagePreference);
        else PlayerPrefs.DeleteKey(GameLanguageService.PlayerPrefsKey);
        PlayerPrefs.Save();
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "contextual-hud-" + TestContext.CurrentContext.Test.Name + ".csv"), rows);
        File.WriteAllLines(Path.Combine(Output,"contextual-frames-"+TestContext.CurrentContext.Test.Name+".csv"),frameRows);
    }
    void OnCompleted(CatActivity activity)
    {
        if (activity != observed) return;
        completions++;
        var bubble = Cat.GetComponent<CatSpeechBubble>();
        completionCopy = bubble != null ? Read<TMP_Text>(bubble, "label").text : "<missing bubble>";
    }
    IEnumerator Boot(string room)
    {
        yield return (IEnumerator)Call(home, "Home");
        // This is a selection request through the production replacement barrier.
        // Do not invoke the care fixture's optional diagnostic preload path.
        CatBreedService.Select("russian-blue");
        yield return QaBreedReadiness.WaitForSelected(Cat, "russian-blue");
        yield return RoomPlayModeSupport.WaitForPendingContactData();
        yield return (IEnumerator)Call(home, "Room", room);
        yield return QaBreedReadiness.WaitForSelected(Cat, "russian-blue");
        prompt = Object.FindAnyObjectByType<ActivityPromptController>();
        Assert.That(prompt, Is.Not.Null);
        Assert.That(Cat.gameObject.scene.name, Is.EqualTo(HomeRoomService.GetOrLivingRoom(room).SceneName));
        Assert.That(UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameScene").isLoaded, Is.True);
        Assert.That(Read<Button>(prompt, "actionButton").GetComponentInParent<Canvas>(), Is.Not.Null);
        ProgressionService.ApplySavedState(coins, diamonds, Math.Max(bond, 1000), chapter, quests);
        RoomPlayModeSupport.ProvisionNeeds();
    }
    void RefreshHud()
    {
        // The bowl controller shares HUD context. Let it discard a previous
        // care offer after the player has moved before asking the real prompt.
        Call(Read<BowlInteraction>(home, "bowls"), "Update");
        prompt.ResolveSceneReferences(); Call(prompt, "RefreshImmediate");
    }
    Button OfferedButton(CatActivity expected)
    {
        RefreshHud();
        Assert.That(Read<CatActivity>(prompt, "candidate"), Is.SameAs(expected),
            "The naturally selected HUD candidate must match the clicked activity.");
        var button = Read<Button>(prompt, "actionButton");
        Assert.That(button.isActiveAndEnabled && button.interactable, Is.True);
        Assert.That(Visible(Read<TMP_Text>(prompt, "actionLabel")), Is.True, "Actual action text is visible in the live HUD.");
        Assert.That(expected.TryGetPromptDistance(Cat, out _), Is.True, "Prompt/click agreement");
        return button;
    }
    static bool Visible(TMP_Text label) => label != null && label.enabled && label.gameObject.activeInHierarchy &&
        label.GetComponentsInParent<CanvasGroup>().All(group => group.alpha > .99f);
    void Place(Vector3 point, Quaternion rotation)
    {
        var cc = Cat.GetComponent<CharacterController>(); bool enabled = cc.enabled;
        cc.enabled = false; point.y = .05f;
        Cat.transform.SetPositionAndRotation(point, rotation);
        cc.enabled = enabled; Physics.SyncTransforms();
    }
    IEnumerator FindFernOffer(SitLookActivity fern)
    {
        // Same bounded standing rings as InteractionPolishTests. Search belongs
        // to this fixture; runtime never receives a destination or forced plan.
        var timer = System.Diagnostics.Stopwatch.StartNew(); int attempts = 0;
        Vector3 target = fern.LookPoint.position;
        for (int ring = 0; ring < 4; ring++)
        for (int angle = 0; angle < 24; angle++)
        {
            Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(20), "Bounded HUD stance search");
            Vector3 direction = Quaternion.Euler(0, angle * 15, 0) * Vector3.forward;
            Place(target + direction * (.55f + ring * .15f), Quaternion.LookRotation(-direction));
            var before = Cat.transform.position; var yaw = Cat.transform.rotation;
            bool ready = fern.TryGetPromptDistance(Cat, out _); attempts++;
            Assert.That(Cat.transform.position, Is.EqualTo(before));
            Assert.That(Cat.transform.rotation, Is.EqualTo(yaw));
            if (ready)
            {
                yield return null; yield return new WaitForEndOfFrame();
                RefreshHud();
                if (Read<CatActivity>(prompt, "candidate") == fern &&
                    Read<Button>(prompt, "actionButton").isActiveAndEnabled)
                {
                    rows.Add($"{TestContext.CurrentContext.Test.Name},{GameLanguageService.Current},{fern.StoreProductId},0,stance,0,0,0,0,0,0,{Csv("attempts=" + attempts + ";editor search ms=" + timer.Elapsed.TotalMilliseconds.ToString("F3"))}");
                    yield break;
                }
            }
            if (attempts % 4 == 0) yield return null;
        }
        Assert.Fail("No real HUD planter offer in the unchanged authored room.");
    }

    IEnumerator MeasureIdleBaseline()
    {
        var times=new List<float>();
        for(int i=0;i<60;i++){yield return new WaitForEndOfFrame();times.Add(Time.unscaledDeltaTime*1000f);}
        times.Sort();baselineMax=times[times.Count-1];baselineP95=times[(int)(times.Count*.95f)];
        frameRows.Add(FormattableString.Invariant($"baseline,{baselineP95:F4},{baselineMax:F4},60"));
    }
    Vector3 ClearWarmPoint(Vector3 centre,Quaternion heading,OvenWarmthActivity oven)
    {
        foreach(float radius in new[]{0f,.04f,.08f,.12f,.16f})
        for(int i=0;i<(radius==0?1:16);i++)
        {
            Vector3 p=centre+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*radius;
            if(!Cat.IsInteractionPoseClear(p,heading))continue;
            Place(p,heading);if(oven.TryGetPromptDistance(Cat,out _))return p;
        }
        Assert.Fail("No physically clear local oven stance for heading "+heading.eulerAngles.y);return centre;
    }

    sealed class StartMeasurement
    {
        public Vector3 position;
        public Quaternion rotation;
        public int clickFrame, lastFrame = -1, frames, walks;
        public float started, maxRoot, maxYaw, worstFrameMs;
        public double clickMs;
    }
    StartMeasurement Click(CatActivity activity)
    {
        // The HUD has already advanced naturally during the idle baseline.
        // Do not inject extra Bowl.Update/RefreshImmediate/readiness work into
        // the measured click frame; a player presses the existing real button.
        var button = Read<Button>(prompt,"actionButton");
        Assert.That(Read<CatActivity>(prompt,"candidate"),Is.SameAs(activity));
        Assert.That(button.isActiveAndEnabled&&button.interactable,Is.True);
        observed = activity; completions = 0; completionCopy = null;
        var m = new StartMeasurement { position = Cat.transform.position, rotation = Cat.transform.rotation,
            clickFrame = Time.frameCount, started = Time.realtimeSinceStartup };
        var timer = System.Diagnostics.Stopwatch.StartNew(); button.onClick.Invoke(); timer.Stop();
        m.clickMs = timer.Elapsed.TotalMilliseconds;
        Assert.That(activity.IsRunning, Is.True, "The real button must start its displayed activity.");
        Sample(m); return m;
    }
    void Sample(StartMeasurement m)
    {
        Vector3 delta = Cat.transform.position - m.position; delta.y = 0;
        m.maxRoot = Mathf.Max(m.maxRoot, delta.magnitude);
        m.maxYaw = Mathf.Max(m.maxYaw, Quaternion.Angle(m.rotation, Cat.transform.rotation));
        Assert.That(m.maxRoot, Is.LessThan(.002f), "No automatic horizontal staging");
        Assert.That(m.maxYaw, Is.LessThan(.2f), "No automatic root/camera-facing turn");
        if (Time.frameCount == m.lastFrame) return;
        m.lastFrame = Time.frameCount;
        var animation = Cat.GetComponent<CatActivityAnimation>();
        if (animation != null && animation.CurrentPose == CatActivityPose.Walk) m.walks++;
        // The next frame's unscaled delta includes the actual click frame. Keep
        // setup/search and file I/O outside this first half-second observation.
        if (Time.frameCount > m.clickFrame && Time.realtimeSinceStartup - m.started <= .55f)
        { m.frames++; m.worstFrameMs = Mathf.Max(m.worstFrameMs, Time.unscaledDeltaTime * 1000f); frameRows.Add(FormattableString.Invariant($"start,{m.frames},{Time.unscaledDeltaTime*1000f:F4},{Time.realtimeSinceStartup-m.started:F4}")); }
    }
    void RecordAndAssert(StartMeasurement m, CatActivity activity, int attempt, string detail)
    {
        rows.Add(FormattableString.Invariant($"{TestContext.CurrentContext.Test.Name},{GameLanguageService.Current},{activity.StoreProductId},{attempt},cycle,{m.clickMs:F3},{m.worstFrameMs:F3},{m.frames},{m.maxRoot:F6},{m.maxYaw:F4},{m.walks},{Csv(detail)}"));
        Assert.That(m.frames, Is.GreaterThan(0), "At least one actual start frame was observed.");
        Assert.That(m.clickMs, Is.LessThan(50), "Real HUD first/repeat click CPU budget (editor measurement)");
        if(m.worstFrameMs>=50)performanceFailures.Add(FormattableString.Invariant($"{activity.Kind}/attempt{attempt}: first .55s max {m.worstFrameMs:F3}ms >=50ms; baseline p95 {baselineP95:F3}/max {baselineMax:F3}ms"));
        Assert.That(m.walks, Is.Zero, "No entrance walk before this stationary action");
    }
    IEnumerator VerifyVisibleBubble(string expected)
    {
        float deadline = Time.realtimeSinceStartup + .5f;
        CatSpeechBubble bubble = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame();
            bubble = Cat.GetComponent<CatSpeechBubble>();
            if (bubble != null && Read<CanvasGroup>(bubble, "group").alpha > .05f) break;
        }
        Assert.That(bubble, Is.Not.Null);
        Assert.That(Read<CanvasGroup>(bubble, "group").alpha, Is.GreaterThan(.05f));
        Assert.That(Read<TMP_Text>(bubble, "label").text, Is.EqualTo(expected));
    }
    IEnumerator Fern(GameLanguage language, string action, string progress, string completion)
    {
        yield return Boot(HomeRoomService.PatioId);
        GameLanguageService.SetLanguage(language); yield return null;
        var fern = CatActivity.Registered.OfType<SitLookActivity>().Single(a =>
            a.gameObject.scene == Cat.gameObject.scene && a.Kind == CatActivityKind.FernWatch);
        Assert.That(fern.StoreProductId, Is.EqualTo("patio.potted-ferns"));
        Assert.That(fern.ReactionKind, Is.EqualTo(SitLookReaction.Sit));
        yield return FindFernOffer(fern);
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            RoomPlayModeSupport.ProvisionNeeds(); OfferedButton(fern);
            Assert.That(Read<TMP_Text>(prompt, "actionLabel").text, Is.EqualTo(action));
            yield return MeasureIdleBaseline();
            var m = Click(fern); bool sawProgress = false;
            float deadline = Time.realtimeSinceStartup + 12;
            while (fern.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame(); Sample(m);
                if (Visible(Read<TMP_Text>(prompt, "progressLabel")))
                {
                    Assert.That(Read<TMP_Text>(prompt, "progressLabel").text, Is.EqualTo(progress));
                    sawProgress = true;
                }
            }
            Assert.That(fern.IsRunning, Is.False); Assert.That(sawProgress, Is.True);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(completionCopy, Is.EqualTo(completion), "Contextual planter completion, never a generic make-room warning");
            yield return VerifyVisibleBubble(completion);
            yield return RoomPlayModeSupport.WaitForMovementRelease(Cat);
            Assert.That(Cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(Cat.GetComponent<CharacterController>().enabled, Is.True);
            Sample(m); RecordAndAssert(m, fern, attempt, "actual HUD/progress/completion; first versus repeat");
        }
        Assert.That(performanceFailures,Is.Empty,string.Join("\n",performanceFailures));
    }
    [UnityTest, Timeout(120000)] public IEnumerator FernTurkish_ActualHud_FirstAndRepeatRemainStillAndContextual() =>
        Fern(GameLanguage.Turkish, "Saksıları incele", "Yaprakları inceliyor", "Yaprakları seyretmek ne güzel.");
    [UnityTest, Timeout(120000)] public IEnumerator FernEnglish_ActualHud_FirstAndRepeatRemainStillAndContextual() =>
        Fern(GameLanguage.English, "Inspect the planters", "Watching the leaves", "The leaves are lovely to watch.");

    [UnityTest, Timeout(180000)] public IEnumerator OvenNotMat_SixPlayerHeadings_BothLanguages_ActualHudAndGetUp()
    {
        yield return Boot(HomeRoomService.KitchenId);
        var oven = CatActivity.Registered.OfType<OvenWarmthActivity>().Single(a => a.gameObject.scene == Cat.gameObject.scene);
        var mat = CatActivity.Registered.OfType<MatKneadActivity>().Single(a => a.gameObject.scene == Cat.gameObject.scene);
        // This is the measured existing oven use spot, not a generated point
        // chosen after a failure. It is independent of either routine's target.
        Vector3 measured = new Vector3(-2.70f, .05f, -.35f);
        int completedCycles = 0;
        foreach (var language in new[] { GameLanguage.Turkish, GameLanguage.English })
        {
            GameLanguageService.SetLanguage(language);
            foreach (float yaw in new[] { 0f, 60f, 120f, 180f, 240f, 300f })
            {
                RoomPlayModeSupport.ProvisionNeeds(); Vector3 point=ClearWarmPoint(measured,Quaternion.Euler(0,yaw,0),oven); Place(point, Quaternion.Euler(0, yaw, 0));
                yield return null; yield return new WaitForEndOfFrame();
                Assert.That(oven.TryGetPromptDistance(Cat, out _), Is.True, "Measured oven spot at player heading " + yaw);
                Assert.That(mat.TryGetPromptDistance(Cat, out _), Is.False, "The nearby mat must not own the oven use spot.");
                OfferedButton(oven);
                string offer = Read<TMP_Text>(prompt, "actionLabel").text;
                Assert.That(offer.ToLowerInvariant(), Does.Contain(language == GameLanguage.Turkish ? "ısın" : "warm up"));
                yield return MeasureIdleBaseline();
                var m = Click(oven);
                float deadline = Time.realtimeSinceStartup + 8;
                while (!oven.IsWaitingForRestStop && oven.IsRunning && Time.realtimeSinceStartup < deadline)
                { yield return new WaitForEndOfFrame(); Sample(m); }
                Assert.That(oven.IsWaitingForRestStop, Is.True); Assert.That(mat.IsRunning, Is.False);
                RefreshHud();
                var getUp = Read<Button>(prompt, "actionButton");
                Assert.That(getUp.isActiveAndEnabled && getUp.interactable, Is.True);
                Assert.That(Read<TMP_Text>(prompt, "actionLabel").text,
                    Does.EndWith(language == GameLanguage.Turkish ? "\nKalk" : "\nGet up"));
                Time.timeScale = 0; yield return null; yield return new WaitForEndOfFrame(); Sample(m);
                Time.timeScale = 1; getUp.onClick.Invoke();
                deadline = Time.realtimeSinceStartup + 6;
                while (oven.IsRunning && Time.realtimeSinceStartup < deadline)
                { yield return new WaitForEndOfFrame(); Sample(m); }
                Assert.That(oven.IsRunning, Is.False); Assert.That(completions, Is.EqualTo(1));
                yield return RoomPlayModeSupport.WaitForMovementRelease(Cat);
                Assert.That(Cat.IsMovementPhysicallyLocked, Is.False);
                Assert.That(Cat.GetComponent<CharacterController>().enabled, Is.True);
                RecordAndAssert(m, oven, ++completedCycles, "measured oven point; yaw=" + yaw + "; mat refused; actual Get up button");
            }
        }
        Assert.That(completedCycles, Is.EqualTo(12));
        Assert.That(performanceFailures,Is.Empty,string.Join("\n",performanceFailures));
    }
    [UnityTest, Timeout(90000)] public IEnumerator FernStaleRealHud_SameFrameMoveAndObstacleReject_ThenLegalClickStarts()
    {
        yield return Boot(HomeRoomService.PatioId);
        var fern=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>
            a.gameObject.scene==Cat.gameObject.scene&&a.Kind==CatActivityKind.FernWatch);
        yield return FindFernOffer(fern);
        var legalPoint=Cat.transform.position;var legalHeading=Cat.transform.rotation;
        var energy=RoomPlayModeSupport.ProvisionNeeds();
        observed=fern;completions=0;
        void AssertRejected(Button button,string cause)
        {
            var point=Cat.transform.position;var heading=Cat.transform.rotation;
            float beforeEnergy=energy.CurrentEnergy;
            long beforeCoins=ProgressionService.Coins,beforeDiamonds=ProgressionService.Diamonds,beforeBond=ProgressionService.BondXp;
            int beforeChapter=ProgressionService.CurrentChapterNumber,frame=Time.frameCount;
            var beforeQuests=ProgressionService.CaptureQuestProgress().Select(q=>JsonUtility.ToJson(q)).ToArray();
            Assert.That(Read<CatActivity>(prompt,"candidate"),Is.SameAs(fern),"The unrefreshed actual HUD offer is stale.");
            button.onClick.Invoke(); // No yield or HUD refresh after the scene/actor change.
            Assert.That(Time.frameCount,Is.EqualTo(frame));
            Assert.That(fern.IsRunning,Is.False,cause);Assert.That(CatActivity.Active,Is.Null,cause);
            Assert.That(energy.CurrentEnergy,Is.EqualTo(beforeEnergy),cause+" spends no energy");
            Assert.That(Cat.transform.position,Is.EqualTo(point),cause+" cannot restore an obsolete start");
            Assert.That(Cat.transform.rotation,Is.EqualTo(heading));
            Assert.That(completions,Is.Zero);Assert.That(Cat.IsMovementPhysicallyLocked,Is.False);
            Assert.That(ProgressionService.Coins,Is.EqualTo(beforeCoins));Assert.That(ProgressionService.Diamonds,Is.EqualTo(beforeDiamonds));
            Assert.That(ProgressionService.BondXp,Is.EqualTo(beforeBond));Assert.That(ProgressionService.CurrentChapterNumber,Is.EqualTo(beforeChapter));
            Assert.That(ProgressionService.CaptureQuestProgress().Select(q=>JsonUtility.ToJson(q)).ToArray(),Is.EqualTo(beforeQuests));
            rows.Add($"{TestContext.CurrentContext.Test.Name},{GameLanguageService.Current},{fern.StoreProductId},0,rejected,0,0,0,0,0,0,{Csv(cause)}");
        }
        var offered=OfferedButton(fern);
        Place(legalPoint+Vector3.forward*4,legalHeading);
        AssertRejected(offered,"same-frame out-of-range player");

        Place(legalPoint,legalHeading);
        offered=OfferedButton(fern);
        // This regression deliberately disables automatic synchronization so
        // production TryStart must synchronize the same-frame moved obstacle.
#pragma warning disable CS0618
        bool oldAutoSync=Physics.autoSyncTransforms;
        var obstacle=new GameObject("QA stale HUD moved solid");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obstacle,Cat.gameObject.scene);
        try
        {
            var box=obstacle.AddComponent<BoxCollider>();box.size=new Vector3(.4f,.5f,.4f);
            obstacle.transform.position=legalPoint+Vector3.up*5;Physics.SyncTransforms();
            Physics.autoSyncTransforms=false;
            obstacle.transform.position=legalPoint+Vector3.up*.28f;
            // The obstacle pose is deliberately unsynced until production TryStart.
            AssertRejected(offered,"same-frame unsynced solid moved into player");
        }
        finally
        {Object.DestroyImmediate(obstacle);Physics.autoSyncTransforms=oldAutoSync;Physics.SyncTransforms();}
#pragma warning restore CS0618

        Place(legalPoint,legalHeading);RoomPlayModeSupport.ProvisionNeeds();
        offered=OfferedButton(fern);offered.onClick.Invoke();
        Assert.That(fern.IsRunning,Is.True,"Restored real legal HUD offer must still start.");
        Assert.That(Vector3.Distance(Cat.transform.position,legalPoint),Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(Cat.transform.rotation,legalHeading),Is.LessThan(.2f));
        yield return new WaitForEndOfFrame();
        Assert.That(fern.IsRunning,Is.True);
        fern.CancelForTransition();yield return RoomPlayModeSupport.WaitForMovementRelease(Cat);
        Assert.That(fern.IsRunning,Is.False);Assert.That(Cat.IsMovementPhysicallyLocked,Is.False);
        Assert.That(Cat.GetComponent<CharacterController>().enabled,Is.True);Assert.That(completions,Is.Zero);
        rows.Add($"{TestContext.CurrentContext.Test.Name},{GameLanguageService.Current},{fern.StoreProductId},1,legal-start-cancel,0,0,0,0,0,0,{Csv("actual button starts; cancellation releases without completion")}");
    }

}
#endif
