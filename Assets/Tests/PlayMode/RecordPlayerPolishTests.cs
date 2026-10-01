#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Import only after the current native run ends and the record-player drafts compile.
// The fixture clicks the real HUD action; it never calls ToggleFromContact directly.
public sealed class RecordPlayerPolishTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    HomeStoreSaveState savedStore;
    HomeProgressionSaveState savedHome;
    QuestProgressEntry[] savedQuests;
    string savedBreed;
    long savedCoins, savedDiamonds, savedBond;
    int savedChapter;
    bool savedMusic, savedSound, savedMute, savedFocus, savedPause;
    float savedScale, savedCapture;
    readonly Dictionary<string, int?> prefs = new Dictionary<string, int?>();
    readonly List<string> rows = new List<string>();
    LevelLoader loader;
    CatMovement cat;
    PaperSpinActivity record;
    RecordPlayerMusic player;
    ActivityPromptController prompt;
    GameAudio audio;
    int completed;
    Vector3 stance;
    Quaternion heading;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use the copied QA session.");
        Assert.That(Path.GetFullPath(EditorQaSession.SaveDirectory).TrimEnd('/', '\\'),
            Is.Not.EqualTo(Path.GetFullPath(Application.persistentDataPath).TrimEnd('/', '\\')));
        savedStore = HomeStoreService.CaptureState(); savedHome = HomeProgressionService.CaptureState();
        savedBreed = CatBreedService.SelectedBreedId; savedQuests = ProgressionService.CaptureQuestProgress();
        savedCoins = ProgressionService.Coins; savedDiamonds = ProgressionService.Diamonds;
        savedBond = ProgressionService.BondXp; savedChapter = ProgressionService.CurrentChapterNumber;
        savedMusic = HomeAudioService.MusicEnabled; savedSound = HomeAudioService.SoundEnabled;
        savedMute = UnityEditor.EditorUtility.audioMasterMute;
        savedScale = Time.timeScale; savedCapture = Time.captureDeltaTime;
        foreach (string key in new[] { "home.audio.music", "home.audio.sound", PetTutorialHint.OnboardingCompletedKey })
            prefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, 1);
        Time.timeScale = 1; Time.captureDeltaTime = 0; // Audio is measured against the actual DSP clock.
        HomeAudioService.MusicEnabled = HomeAudioService.SoundEnabled = true;
        UnityEditor.EditorUtility.audioMasterMute = false;
        GameAudio.Clip(AudioCue.UIClick);
        audio = Object.FindAnyObjectByType<GameAudio>(); Assert.That(audio, Is.Not.Null);
        savedFocus = Field<bool>(audio, "focused"); savedPause = Field<bool>(audio, "suspended");
        audio.SendMessage("OnApplicationFocus", true); audio.SendMessage("OnApplicationPause", false);
        rows.Clear(); rows.Add("test,breed,stage,attempts,elapsedMs,maxResolveMs,x,z,yaw,contacts,toggles,isOn,sample,detail");
        Directory.CreateDirectory(Root);
        CatActivity.Completed += Completed;
    }
    [TearDown] public void After()
    {
        CatActivity.Completed -= Completed;
        Time.timeScale = 1;
        if (cat != null) CatActionState.CancelForTransition(cat);
        // Normal component lifetime stops the scene-owned source even after a failed assertion.
        if (player != null) player.enabled = false;
        RoomPlayModeSupport.ReleaseRoom();
        HomeProgressionService.ApplySavedState(savedHome);
        HomeStoreService.ApplySavedState(savedStore); CatBreedService.Select(savedBreed);
        ProgressionService.ApplySavedState(savedCoins, savedDiamonds, savedBond, savedChapter, savedQuests);
        HomeAudioService.MusicEnabled = savedMusic; HomeAudioService.SoundEnabled = savedSound;
        foreach (var pair in prefs)
            if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
        PlayerPrefs.Save(); UnityEditor.EditorUtility.audioMasterMute = savedMute;
        if (audio != null) { audio.SendMessage("OnApplicationFocus", savedFocus); audio.SendMessage("OnApplicationPause", savedPause); }
        Time.timeScale = savedScale; Time.captureDeltaTime = savedCapture;
        Flush();
    }
    static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);
    static void Refresh(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    void Completed(CatActivity activity) { if (activity == record) completed++; }
    void Flush() => File.WriteAllLines(Path.Combine(Root, "record-player-" + TestContext.CurrentContext.Test.Name + ".csv"), rows);
    void Row(string stage, string detail = "", int attempts = 0, double elapsed = 0, double resolve = 0)
    {
        Vector3 p = cat != null ? cat.transform.position : Vector3.zero;
        rows.Add(FormattableString.Invariant($"{TestContext.CurrentContext.Test.Name},{CatBreedService.SelectedBreedId},{stage},{attempts},{elapsed:F3},{resolve:F3},{p.x:F4},{p.z:F4},{(cat != null ? cat.transform.eulerAngles.y : 0):F2},{(record != null ? record.RecordContactCount : 0)},{(player != null ? player.ToggleCount : 0)},{(player != null && player.IsOn)},{(player != null && player.Source != null ? player.Source.timeSamples : 0)},{Csv(detail)}"));
        Flush();
    }

    IEnumerator Home()
    {
        DirectLevelPlayBootstrap.RedirectSuppressed = false;
        var operation = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        Assert.That(operation, Is.Not.Null);
        float until = Time.realtimeSinceStartup + 25;
        while (!operation.isDone && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(operation.isDone, Is.True, "GameScene load deadline");
        until = Time.realtimeSinceStartup + 25;
        while (Time.realtimeSinceStartup < until)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader != null && loader.IsReady) break;
            yield return null;
        }
        Assert.That(loader != null && loader.IsReady, Is.True, "Full home bootstrap");
        var title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        if (TitleScreen.IsShowing) Field<Button>(title, "playButton").onClick.Invoke();
        until = Time.realtimeSinceStartup + 8;
        while (TitleScreen.IsShowing && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(TitleScreen.IsShowing, Is.False);
        foreach (var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include)) { popup.Close(); popup.enabled = false; }
        foreach (var popup in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include)) popup.enabled = false;
        foreach (var popup in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include)) popup.enabled = false;
        HomeProgressionService.ApplySavedState(new HomeProgressionSaveState { homeXp = HomeProgressionService.CumulativeXpForLevel(30) });
        var store = HomeStoreSaveState.CreateDefault(); store.currentRoomId = loader.CurrentRoom.Id;
        store.ownedProductIds = HomeStoreService.Products.Select(p => p.Id).ToArray();
        store.storedProductIds = store.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(store);
        ProgressionService.ApplySavedState(savedCoins, savedDiamonds, Math.Max(savedBond, 1000), savedChapter, savedQuests);
        yield return null; yield return null;
        yield return Room(HomeRoomService.SecondFloorId);
        record = CatActivity.Registered.OfType<PaperSpinActivity>().Single(a => a.Kind == CatActivityKind.RecordSpin && a.gameObject.scene == cat.gameObject.scene);
        player = record.RecordMusic;
        Assert.That(player, Is.Not.Null); Assert.That(player.Source, Is.Not.Null);
        Assert.That(player.Source.clip, Is.SameAs(Resources.Load<AudioClip>(RecordPlayerMusic.ResourcePath)));
        Assert.That(player.Source.clip.length, Is.EqualTo(38.4f).Within(.01f));
        Assert.That(player.Source.loop, Is.True); Assert.That(player.Source.pitch, Is.EqualTo(1));
        Assert.That(player.IsOn, Is.False, "Room enters with its record switched off");
        audio.SendMessage("OnApplicationFocus", true); audio.SendMessage("OnApplicationPause", false);
        Row("room-ready");
    }
    IEnumerator Room(string id)
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        Row("room-request", id);
        if (loader.CurrentRoom.Id != id) Assert.That(loader.LoadRoom(id), Is.True, id);
        float until = Time.realtimeSinceStartup + 25;
        while ((!loader.IsReady || loader.CurrentRoom.Id != id) && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(loader.IsReady && loader.CurrentRoom.Id == id, Is.True, "Room transition deadline: " + id);
        cat = Object.FindAnyObjectByType<CatMovement>(); Assert.That(cat, Is.Not.Null);
        Assert.That(cat.gameObject.scene.name, Is.EqualTo(HomeRoomService.GetOrLivingRoom(id).SceneName));
        cat.GetComponent<CatIdleBehavior>().enabled = false;
        foreach (var hint in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include)) hint.enabled = false;
        RoomPlayModeSupport.ProvisionNeeds();
        prompt = Object.FindAnyObjectByType<ActivityPromptController>(); Assert.That(prompt, Is.Not.Null);
        prompt.ResolveSceneReferences();
        yield return null; yield return null;
        Assert.That(HomeUiFlow.IsHomeControlBlocked || cat.AreWorldActionsBlocked, Is.False);
        Row("room-arrived", id);
    }
    void Place(Vector3 point, Quaternion rotation)
    {
        var cc = cat.GetComponent<CharacterController>(); bool wasEnabled = cc.enabled;
        cc.enabled = false; point.y = .05f; cat.transform.SetPositionAndRotation(point, rotation);
        cc.enabled = wasEnabled; Physics.SyncTransforms();
    }
    IEnumerator FindPlayerStance()
    {
        yield return QaBreedReadiness.WaitForSelected(cat);
        // Only the QA player searches. Production TryGetStartPose remains a pure current-stance query.
        Vector3 target = record.RollPivot.GetComponentInChildren<Renderer>().bounds.center;
        Vector3 authored = record.RoutineEntryPoint.position - target; authored.y = 0;
        if (authored.sqrMagnitude < .0001f) authored = record.transform.forward;
        authored.Normalize();
        var timer = System.Diagnostics.Stopwatch.StartNew(); int attempts = 0; double maxResolve = 0;
        foreach (float radius in new[] { .40f, .36f, .44f, .48f, .52f, .56f, .60f, .64f })
        foreach (float angle in new[] { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 180f })
        foreach (float yaw in new[] { 0f, 30f, -30f, 60f, -60f })
        {
            if (timer.Elapsed.TotalSeconds > 20)
            { Row("stance-timeout", "Search budget exhausted; no geometry was altered", attempts, timer.Elapsed.TotalMilliseconds, maxResolve); Assert.Fail("Record stance search exceeded 20 s"); }
            Vector3 side = Quaternion.Euler(0, angle, 0) * authored;
            Place(target + side * radius, Quaternion.LookRotation(-side) * Quaternion.Euler(0, yaw, 0));
            Vector3 before = cat.transform.position; Quaternion rotation = cat.transform.rotation;
            var resolveTimer = System.Diagnostics.Stopwatch.StartNew();
            bool ready = record.TryGetStartPose(cat, out var start); resolveTimer.Stop(); attempts++;
            maxResolve = Math.Max(maxResolve, resolveTimer.Elapsed.TotalMilliseconds);
            Assert.That(Vector3.Distance(before, cat.transform.position), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(rotation, cat.transform.rotation), Is.LessThan(.001f));
            if (ready)
            {
                yield return null; // Let the real controller settle before accepting the HUD offer.
                prompt.ResolveSceneReferences(); Refresh(prompt, "RefreshImmediate");
                if (Field<CatActivity>(prompt, "candidate") == record && Field<Button>(prompt, "actionButton").isActiveAndEnabled)
                {
                    Assert.That(record.TryGetStartPose(cat, out start) && start.HasPawPlan, Is.True);
                    stance = cat.transform.position; heading = cat.transform.rotation;
                    Row("stance-ready", "unchanged scene/profile/target", attempts, timer.Elapsed.TotalMilliseconds, maxResolve);
                    yield break;
                }
            }
            if (attempts % 4 == 0) { Row("stance-search", "", attempts, timer.Elapsed.TotalMilliseconds, maxResolve); yield return null; }
        }
        Row("stance-none", "All 320 candidates consumed", attempts, timer.Elapsed.TotalMilliseconds, maxResolve);
        Assert.Fail("No actual HUD record offer for " + CatBreedService.SelectedBreedId);
    }
    void ClickRecord()
    {
        RoomPlayModeSupport.ProvisionNeeds(); prompt.ResolveSceneReferences(); Refresh(prompt, "RefreshImmediate");
        Assert.That(Field<CatActivity>(prompt, "candidate"), Is.SameAs(record), "Actual prompt/click agreement");
        var button = Field<Button>(prompt, "actionButton");
        Assert.That(button != null && button.isActiveAndEnabled && button.interactable, Is.True);
        Assert.That(button.GetComponentInParent<Canvas>(), Is.Not.Null);
        completed = 0; stance = cat.transform.position; heading = cat.transform.rotation;
        button.onClick.Invoke(); Assert.That(record.IsRunning, Is.True);
        Assert.That(record.RecordContactCount, Is.Zero, "Click itself cannot toggle");
        AssertRoot();
    }
    void AssertRoot()
    {
        Assert.That(Vector3.Distance(stance, cat.transform.position), Is.LessThan(.002f), "No root staging");
        Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.2f), "No root turn");
    }
    IEnumerator Toggle(bool expected)
    {
        int previous = player.ToggleCount; bool wasOn = player.IsOn;
        ClickRecord(); Assert.That(player.IsOn, Is.EqualTo(wasOn));
        float until = Time.realtimeSinceStartup + 8;
        while (record.IsRunning && Time.realtimeSinceStartup < until)
        {
            yield return null; AssertRoot();
            Assert.That(player.ToggleCount - previous, Is.InRange(0, 1));
            if (record.RecordContactCount == 0) Assert.That(player.IsOn, Is.EqualTo(wasOn), "State cannot change before actual paw contact");
        }
        Row("toggle-finished", "expected=" + expected + ";paw=" + record.RecordContactDistance.ToString("F6"));
        Assert.That(record.IsRunning, Is.False); Assert.That(completed, Is.EqualTo(1));
        Assert.That(record.RecordContactCount, Is.EqualTo(1)); Assert.That(record.RecordContactDistance, Is.LessThanOrEqualTo(.025f));
        Assert.That(player.ToggleCount, Is.EqualTo(previous + 1)); Assert.That(player.IsOn, Is.EqualTo(expected));
        if (expected)
        {
            Assert.That(Field<CatMovement>(player, "cat"), Is.SameAs(cat), "The actual contact binds its gameplay actor");
            Assert.That(Field<BowlInteraction>(player, "bowls"), Is.SameAs(cat.GetComponent<BowlInteraction>()));
            Assert.That(Field<SleepInteraction>(player, "sleep"), Is.SameAs(cat.GetComponent<SleepInteraction>()));
            Assert.That(Field<CatActivityAnimation>(player, "poses"), Is.SameAs(cat.GetComponent<CatActivityAnimation>()));
        }
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False); Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include)
            .Count(s => s.clip != null && s.clip.name == "Record"), Is.EqualTo(1), "One scene-owned record source");
    }

    [UnityTest, Timeout(180000)] public IEnumerator PoisonedCachedDistance_CannotSuppressARealCurrentFrameContact()
    {
        yield return Home(); CatBreedService.Select("oriental-shorthair");
        yield return FindPlayerStance();
        var poison = cat.gameObject.AddComponent<RecordDistancePoisonProbe>();
        try
        {
            yield return Toggle(true);
            Assert.That(poison.PoisonedFrames, Is.GreaterThan(3));
            Assert.That(record.RecordContactDistance, Is.LessThan(.025f));
        }
        finally { Object.DestroyImmediate(poison); }
    }

    [UnityTest, Timeout(180000)] public IEnumerator PauseBeforeFirstContact_ResumesWithOneCurrentFrameTouch()
    {
        yield return Home(); CatBreedService.Select("oriental-shorthair"); yield return null; yield return null;
        yield return FindPlayerStance();
        Assert.That(record.TryGetStartPose(cat, out var prepared), Is.True);
        var hand = CatBreedVisualFactory.FindDescendant(cat.transform, prepared.PawPlan.Left ? "DEF-hand.L" : "DEF-hand.R");
        int previous = player.ToggleCount;
        ClickRecord(); Time.timeScale = 0;
        for (int frame = 0; frame < 5; frame++)
        {
            yield return new WaitForEndOfFrame();
            Assert.That(record.RecordContactCount, Is.Zero);
            Assert.That(player.ToggleCount, Is.EqualTo(previous));
            Assert.That(player.IsOn, Is.False); AssertRoot();
        }
        Time.timeScale = 1;
        float until = Time.realtimeSinceStartup + 8;
        bool observed = false;
        while (record.IsRunning && Time.realtimeSinceStartup < until)
        {
            yield return new WaitForEndOfFrame(); AssertRoot();
            Assert.That(record.RecordContactCount, Is.InRange(0, 1));
            Assert.That(player.ToggleCount - previous, Is.InRange(0, 1));
            if (!observed && record.RecordContactCount == 1)
            {
                observed = true;
                Assert.That(Time.timeScale, Is.GreaterThan(0));
                Assert.That(Vector3.Distance(hand.position, prepared.PawPlan.Target), Is.LessThanOrEqualTo(.025f),
                    "First credit is observed after this frame's actual hand solve");
                Assert.That(player.ToggleCount, Is.EqualTo(previous + 1));
            }
            yield return null;
        }
        Assert.That(observed, Is.True); Assert.That(record.IsRunning, Is.False);
        Assert.That(record.RecordContactCount, Is.EqualTo(1)); Assert.That(player.ToggleCount, Is.EqualTo(previous + 1));
        Assert.That(player.IsOn, Is.True); Assert.That(completed, Is.EqualTo(1));
        Row("pause-before-first-contact", "same-frame solved hand; one toggle after resume");
    }

    [UnityTest, Timeout(240000)] public IEnumerator TwoBreeds_ActualHud_OnOffTwice_OnePawOneToggleAndFixedRoot()
    {
        yield return Home();
        foreach (string breed in new[] { "oriental-shorthair", "persian" })
        {
            CatBreedService.Select(breed); yield return null; yield return null;
            yield return FindPlayerStance();
            for (int cycle = 0; cycle < 2; cycle++)
            {
                yield return Toggle(true); yield return AssertAdvancing("ON");
                var mix = Object.FindAnyObjectByType<GameSoundscape>();
                Assert.That(mix.TargetMusicVolume, Is.Zero);
                yield return new WaitForSecondsRealtime(.5f);
                Assert.That(mix.CurrentMusicVolume, Is.LessThan(.003f), "The record owns the music slot after the existing crossfade");
                yield return Toggle(false); yield return null;
                Assert.That(player.Source.isPlaying, Is.False); Assert.That(player.Source.timeSamples, Is.Zero);
            }
        }
    }

    [UnityTest, Timeout(180000)] public IEnumerator CancellationBeforeContactDoesNothing_AfterContactAndOtherActivityKeepsState()
    {
        yield return Home(); CatBreedService.Select("oriental-shorthair"); yield return null; yield return null;
        yield return FindPlayerStance();
        int previous = player.ToggleCount;
        ClickRecord(); record.CancelForTransition(); yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(record.RecordContactCount, Is.Zero); Assert.That(player.ToggleCount, Is.EqualTo(previous)); Assert.That(player.IsOn, Is.False);
        Row("cancel-before");
        ClickRecord(); float until = Time.realtimeSinceStartup + 6;
        while (record.IsRunning && record.RecordContactCount == 0 && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(record.RecordContactCount, Is.EqualTo(1)); Assert.That(record.RecordContactDistance, Is.LessThanOrEqualTo(.025f));
        Assert.That(player.IsOn, Is.True);
        yield return PauseContactPose();
        record.CancelForTransition();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(completed, Is.Zero); Assert.That(player.ToggleCount, Is.EqualTo(previous + 1)); Assert.That(player.IsOn, Is.True);
        Row("cancel-after"); yield return AssertAdvancing("after-cancel");
        var command = cat.GetComponent<CatCommandActivity>() ?? cat.gameObject.AddComponent<CatCommandActivity>();
        Assert.That(command.Issue(CatCompanionCommand.Meow), Is.True, "A second genuine activity starts after cancellation");
        until = Time.realtimeSinceStartup + 8;
        while (command.IsRunning && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(command.IsRunning, Is.False); yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(player.IsOn, Is.True); Assert.That(player.ToggleCount, Is.EqualTo(previous + 1));
        yield return AssertAdvancing("after-other-activity");
    }

    IEnumerator PauseContactPose()
    {
        // Capture the fully solved real pose before pausing. Taking the baseline
        // after a paused frame would hide the very rollback this test detects.
        yield return new WaitForEndOfFrame();
        Assert.That(record.IsRunning && record.RecordContactCount == 1, Is.True);
        var chest = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.001");
        var left = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-hand.L");
        var right = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-hand.R");
        var motion = cat.GetComponent<CatPawReachMotion>();
        Assert.That(motion != null && motion.IsActive, Is.True, "Exercise an active contact correction");
        Vector3 leftBefore = left.position, rightBefore = right.position;
        Quaternion chestBefore = chest.rotation;
        int contacts = record.RecordContactCount, toggles = player.ToggleCount;
        float maxPawDrift = 0, maxChestAngle = 0;
        Time.timeScale = 0;
        try
        {
            float until = Time.realtimeSinceStartup + .22f;
            while (Time.realtimeSinceStartup < until)
            {
                yield return new WaitForEndOfFrame();
                maxPawDrift = Mathf.Max(maxPawDrift, Vector3.Distance(leftBefore, left.position), Vector3.Distance(rightBefore, right.position));
                maxChestAngle = Mathf.Max(maxChestAngle, Quaternion.Angle(chestBefore, chest.rotation));
                AssertRoot();
                Assert.That(record.RecordContactCount, Is.EqualTo(contacts));
                Assert.That(player.ToggleCount, Is.EqualTo(toggles));
            }
            Row("active-contact-paused", "maxPawDrift=" + maxPawDrift.ToString("F6") + ";maxChestAngle=" + maxChestAngle.ToString("F6"));
            Assert.That(maxPawDrift, Is.LessThan(.001f), "Paused paws retain their actual solved world positions");
            Assert.That(maxChestAngle, Is.LessThan(.01f), "Paused chest correction remains unchanged");
        }
        finally { Time.timeScale = 1; }
        yield return null;
        Assert.That(record.RecordContactCount, Is.EqualTo(contacts));
        Assert.That(player.ToggleCount, Is.EqualTo(toggles), "Resume cannot repeat the observed contact");
    }

    IEnumerator AssertAdvancing(string stage)
    {
        float until = Time.realtimeSinceStartup + 3;
        while (!player.IsAudible && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(player.IsAudible, Is.True, stage);
        var source = player.Source; int first = source.timeSamples; double dsp = AudioSettings.dspTime;
        yield return new WaitForSecondsRealtime(.25f);
        int delta = (source.timeSamples - first + source.clip.samples) % source.clip.samples;
        double seconds = AudioSettings.dspTime - dsp;
        AudioSettings.GetDSPBufferSize(out int buffer, out _);
        Assert.That(seconds, Is.GreaterThan(.15)); Assert.That(delta, Is.GreaterThan(0));
        Assert.That(Math.Abs(delta - seconds * source.clip.frequency), Is.LessThanOrEqualTo(buffer * 2 + 4), "Playback sample cursor follows the real DSP clock");
        Row(stage, "sampleDelta=" + delta + ";dspSeconds=" + seconds.ToString("F6"));
    }

    [UnityTest, Timeout(180000)] public IEnumerator ActiveSourcePoseLevels_UseContactActorInAdditiveRoom_AndRecoverAfterRelease()
    {
        yield return Home(); CatBreedService.Select("oriental-shorthair"); yield return null; yield return null;
        Assert.That(SceneManager.GetSceneByName("GameScene").isLoaded, Is.True);
        Assert.That(SceneManager.sceneCount, Is.GreaterThanOrEqualTo(3), "Real GameScene, UI and additive room stay loaded");
        Assert.That(cat.gameObject.scene, Is.EqualTo(record.gameObject.scene));
        Assert.That(cat.gameObject.scene.name, Is.Not.EqualTo("GameScene"));
        yield return FindPlayerStance(); yield return Toggle(true);
        yield return AssertRecordLevel(.16f, "record-baseline");
        var command = cat.GetComponent<CatCommandActivity>() ?? cat.gameObject.AddComponent<CatCommandActivity>();
        var driver = cat.GetComponent<CatActivityAnimation>();
        int toggle = player.ToggleCount;
        foreach (var pose in new[] { CatActivityPose.Eat, CatActivityPose.Drink, CatActivityPose.Sleep })
        {
            // This is the audio/source-pose contract, not a bowl/bed-flow test.
            // Loft's legacy care anchors have no visible furniture. Keep real
            // activity ownership, sample the native source pose, and never fake
            // care flags or restore a saved sleeping state to claim interaction.
            Assert.That(command.Issue(CatCompanionCommand.Meow), Is.True);
            yield return null;
            driver.SetTimedPose(pose, .5f);
            yield return AssertRecordLevel(pose == CatActivityPose.Sleep ? .012f : .025f,
                "source-" + pose, () => command.IsRunning && driver.IsActive && driver.CurrentPose == pose);
            Assert.That(player.ToggleCount, Is.EqualTo(toggle));
            Assert.That(Field<CatMovement>(player, "cat"), Is.SameAs(cat));
            command.CancelForTransition();
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            yield return AssertRecordLevel(.16f, "source-" + pose + "-released");
        }
        Assert.That(command.Issue(CatCompanionCommand.Sit), Is.True);
        float until = Time.realtimeSinceStartup + 4f;
        while (!command.IsWaitingForRestStop && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(command.IsWaitingForRestStop, Is.True, "The genuine sit command reached its owned rest state");
        yield return AssertRecordLevel(.018f, "actual-command-rest", () => command.IsWaitingForRestStop);
        command.CancelForTransition();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        yield return AssertRecordLevel(.16f, "actual-command-rest-released");
    }

    IEnumerator AssertRecordLevel(float expected, string stage, Func<bool> ownerStillActive = null)
    {
        int toggle = player.ToggleCount;
        float until = Time.realtimeSinceStartup + 1.5f;
        do
        {
            yield return null;
            Assert.That(player.IsOn && player.Source.isPlaying, Is.True, stage);
            Assert.That(player.ToggleCount, Is.EqualTo(toggle));
            if (ownerStillActive != null) Assert.That(ownerStillActive(), Is.True, "Measure the owned source pose, not its exit");
        } while (Mathf.Abs(player.Source.volume - expected) > .0005f && Time.realtimeSinceStartup < until);
        Assert.That(player.Source.volume, Is.EqualTo(expected).Within(.0005f), stage);
        Row(stage, "volume=" + player.Source.volume.ToString("F6"));
        yield return AssertAdvancing(stage);
        Assert.That(player.Source.volume, Is.EqualTo(expected).Within(.0005f), "Ducking keeps the source playing at the selected level");
        if (ownerStillActive != null) Assert.That(ownerStillActive(), Is.True);
    }

    IEnumerator AssertPausedAndResumes(string stage, Action mute, Action resume, bool freezePlatter = true)
    {
        mute(); yield return null; yield return null;
        Assert.That(player.IsOn, Is.True); Assert.That(player.Source.isPlaying, Is.False); Assert.That(player.Source.volume, Is.Zero);
        int sample = player.Source.timeSamples; int toggles = player.ToggleCount; Quaternion platter = record.RollPivot.localRotation;
        yield return new WaitForSecondsRealtime(.22f);
        Assert.That(player.Source.timeSamples, Is.EqualTo(sample), "Paused record retains its exact sample cursor");
        if (freezePlatter)
            Assert.That(Quaternion.Angle(platter, record.RollPivot.localRotation), Is.LessThan(.01f), "Paused record disc stops");
        else
            Assert.That(Quaternion.Angle(platter, record.RollPivot.localRotation), Is.GreaterThan(1f), "Music preference mutes audio while the switched-on mechanism continues");
        Assert.That(player.ToggleCount, Is.EqualTo(toggles));
        Row(stage + "-paused", "sample=" + sample);
        resume(); yield return null;
        Assert.That(player.IsOn, Is.True); Assert.That(player.ToggleCount, Is.EqualTo(toggles));
        Assert.That(player.Source.timeSamples, Is.GreaterThanOrEqualTo(sample), "Resume does not restart the recording");
        yield return AssertAdvancing(stage + "-resumed");
    }
    [UnityTest, Timeout(180000)] public IEnumerator MusicFocusAndTimePause_KeepSampleCursor_RoomUnloadRemovesSourceAndResetsSwitch()
    {
        yield return Home(); CatBreedService.Select("persian"); yield return null; yield return null;
        yield return FindPlayerStance(); yield return Toggle(true);
        yield return AssertAdvancing("initial");
        HomeAudioService.SoundEnabled = false;
        yield return AssertAdvancing("effects-muted-music-continues");
        HomeAudioService.SoundEnabled = true;
        yield return AssertPausedAndResumes("music", () => HomeAudioService.MusicEnabled = false, () => HomeAudioService.MusicEnabled = true, false);
        yield return AssertPausedAndResumes("focus", () => audio.SendMessage("OnApplicationFocus", false), () => audio.SendMessage("OnApplicationFocus", true));
        yield return AssertPausedAndResumes("application", () => audio.SendMessage("OnApplicationPause", true), () => audio.SendMessage("OnApplicationPause", false));
        yield return AssertPausedAndResumes("time", () => Time.timeScale = 0, () => Time.timeScale = 1);
        var oldPlayer = player; var oldSource = player.Source;
        yield return Room(HomeRoomService.LivingRoomId); yield return null;
        Assert.That(oldPlayer == null && oldSource == null, Is.True, "Unloading owns and destroys the whole record source");
        Assert.That(RecordPlayerMusic.HasActiveRecord, Is.False);
        Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include)
            .Any(s => s.clip != null && s.clip.name == "Record"), Is.False, "No orphan record audio");
        yield return Room(HomeRoomService.SecondFloorId);
        record = CatActivity.Registered.OfType<PaperSpinActivity>().Single(a => a.Kind == CatActivityKind.RecordSpin && a.gameObject.scene == cat.gameObject.scene);
        player = record.RecordMusic;
        Assert.That(player.IsOn, Is.False); Assert.That(player.ToggleCount, Is.Zero); Assert.That(player.Source.isPlaying, Is.False);
        Row("reentered-off");
    }
}
#endif

// Runs after the ordinary IK but before the routine's EndOfFrame observation.
// Corrupt only the old cached scalar; the native pose and real solved hand stay.
[DefaultExecutionOrder(2000)]
public sealed class RecordDistancePoisonProbe : MonoBehaviour
{
    static readonly FieldInfo DistanceField = typeof(CatToyContactMotion).GetField("<Distance>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    public int PoisonedFrames { get; private set; }
    void LateUpdate()
    {
        var contact = GetComponent<CatToyContactMotion>();
        if (contact == null || DistanceField == null) return;
        DistanceField.SetValue(contact, float.PositiveInfinity); PoisonedFrames++;
    }
}
