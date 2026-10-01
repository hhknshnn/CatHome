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

// Import as Assets/Tests/PlayMode/CareAlignmentPolishTests.cs only after the
// current native run finishes. This fixture operates solely in the copied QA
// save. The two-breed tests form the first gate; the ten-breed test is separate.
public sealed class CareAlignmentPolishTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const float RootTolerance = .002f;
    const float YawTolerance = .15f;
    const float MouthTolerance = .045f;
    HomeStoreSaveState savedStore;
    HomeProgressionSaveState savedProgression;
    string savedBreed;
    float savedTimeScale, savedCapture;
    bool hadOnboarding;
    int savedOnboarding;
    CatMovement cat;
    BowlInteraction bowls;
    SleepInteraction sleep;
    HungerSystem hunger;
    ThirstSystem thirst;
    EnergySystem energy;
    LevelLoader loader;
    readonly List<string> rows = new List<string>();
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory",
        "Docs/QA/INTERACTION_POLISH_2026-09-16");

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use UiQaTestSession's copied save.");
        Assert.That(Path.GetFullPath(EditorQaSession.SaveDirectory).TrimEnd('/', '\\'),
            Is.Not.EqualTo(Path.GetFullPath(Application.persistentDataPath).TrimEnd('/', '\\')),
            "The QA flag alone must never authorize the real save directory.");
        savedStore = HomeStoreService.CaptureState();
        savedProgression = HomeProgressionService.CaptureState();
        savedBreed = CatBreedService.SelectedBreedId;
        savedTimeScale = Time.timeScale; savedCapture = Time.captureDeltaTime;
        hadOnboarding = PlayerPrefs.HasKey(PetTutorialHint.OnboardingCompletedKey);
        savedOnboarding = PlayerPrefs.GetInt(PetTutorialHint.OnboardingCompletedKey);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, 1);
        Time.timeScale = 1; Time.captureFramerate = 60;
        rows.Clear(); CareBlockerTopology.Clear();
        rows.Add("test,breed,care,samples,completions,rootDrift,yawDrift,minActualMouth,needDelta");
    }

    [TearDown] public void After()
    {
        CareBlockerTopology.Clear();
        Time.timeScale = 1;
        if (cat != null) CatActionState.CancelForTransition(cat);
        RoomPlayModeSupport.ReleaseRoom();
        HomeProgressionService.ApplySavedState(savedProgression);
        HomeStoreService.ApplySavedState(savedStore);
        CatBreedService.Select(savedBreed);
        if (hadOnboarding) PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, savedOnboarding);
        else PlayerPrefs.DeleteKey(PetTutorialHint.OnboardingCompletedKey);
        Time.timeScale = savedTimeScale; Time.captureDeltaTime = savedCapture;
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, TestContext.CurrentContext.Test.Name + ".csv"), rows);
    }

    static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, Private).SetValue(target, value);
    static void Refresh(object target, string method = "Update") =>
        target.GetType().GetMethod(method, Private).Invoke(target, null);
    static string[] CriticalBreeds() => new[] { "oriental-shorthair", "persian" };

    IEnumerator Home()
    {
        DirectLevelPlayBootstrap.RedirectSuppressed = false;
        yield return RoomPlayModeSupport.WaitForPendingContactData();
        yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        float deadline = Time.realtimeSinceStartup + 25;
        while (Time.realtimeSinceStartup < deadline)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader != null && loader.IsReady) break;
            yield return null;
        }
        Assert.That(loader != null && loader.IsReady, Is.True, "The full home bootstrap must settle.");
        var title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
        if (TitleScreen.IsShowing)
        {
            Assert.That(title, Is.Not.Null);
            Field<Button>(title, "playButton").onClick.Invoke();
            deadline = Time.realtimeSinceStartup + 8;
            while (TitleScreen.IsShowing && Time.realtimeSinceStartup < deadline) yield return null;
        }
        Assert.That(TitleScreen.IsShowing, Is.False);
        foreach (var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include))
        { popup.Close(); popup.enabled = false; }
        foreach (var popup in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include))
            popup.enabled = false;
        foreach (var popup in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include))
            popup.enabled = false;
        HomeProgressionService.ApplySavedState(new HomeProgressionSaveState
        { homeXp = HomeProgressionService.CumulativeXpForLevel(30) });
        var owned = HomeStoreSaveState.CreateDefault();
        owned.currentRoomId = loader.CurrentRoom.Id;
        owned.ownedProductIds = HomeStoreService.Products.Select(p => p.Id).ToArray();
        owned.storedProductIds = owned.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(owned);
        yield return null; yield return null;
        yield return Room(HomeRoomService.LivingRoomId);
    }

    public static void TransitionMark(string text)
    {
        if (!UnityEditor.SessionState.GetBool("CatHome.QA.TraceCareTransition", false)) return;
        File.AppendAllText(Path.Combine(Output, "care-transition-progress.txt"), DateTime.Now.ToString("O") + " frame=" + Time.frameCount + " " + text + "\n");
    }

    IEnumerator Room(string id)
    {
        TransitionMark("Room enter " + id);
        if (cat != null) CatActionState.CancelForTransition(cat);
        if (loader.CurrentRoom.Id != id) Assert.That(loader.LoadRoom(id), Is.True, id);
        float deadline = Time.realtimeSinceStartup + 20;
        while ((!loader.IsReady || loader.CurrentRoom.Id != id ||
            Object.FindAnyObjectByType<CatMovement>() == null ||
            Object.FindAnyObjectByType<CatMovement>().gameObject.scene.name != HomeRoomService.GetOrLivingRoom(id).SceneName) &&
            Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(loader.IsReady && loader.CurrentRoom.Id == id, Is.True, id);
        TransitionMark("Room loader ready " + id);
        cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(cat, Is.Not.Null);
        bowls = cat.GetComponent<BowlInteraction>(); sleep = cat.GetComponent<SleepInteraction>();
        hunger = Object.FindAnyObjectByType<HungerSystem>();
        thirst = Object.FindAnyObjectByType<ThirstSystem>();
        energy = Object.FindAnyObjectByType<EnergySystem>();
        Assert.That(hunger != null && thirst != null && energy != null, Is.True, "Use the real HUD needs.");
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        foreach (var hint in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include)) hint.enabled = false;
        TransitionMark("Room before references " + id);
        bowls.ResolveSceneReferences();
        Object.FindAnyObjectByType<ActivityPromptController>().ResolveSceneReferences();
        ReadyNeeds();
        TransitionMark("Room before final frames " + id);
        yield return null; yield return null;
        TransitionMark("Room completed " + id);
        // Close() fades the return popup; two simulated frames do not always
        // cover its real-time transition after an otherwise fast room load.
        float uiDeadline=Time.realtimeSinceStartup+2f;
        while((HomeUiFlow.IsHomeControlBlocked||cat.AreWorldActionsBlocked)&&Time.realtimeSinceStartup<uiDeadline)yield return null;
        Assert.That(HomeUiFlow.IsHomeControlBlocked || cat.AreWorldActionsBlocked, Is.False,
            "Home UI ready: dialogue="+CatDialogueView.IsAnyVisible+" away="+WhileYouWereAwayPopup.IsAnyOpen+
            " collection="+CollectionCompleteCelebrationView.IsAnyOpen+" onboarding="+OnboardingCelebrationView.IsAnyOpen);
    }

    void ReadyNeeds()
    {
        RoomPlayModeSupport.ProvisionNeeds();
        hunger.ApplySavedValue(35); thirst.ApplySavedValue(35); energy.ApplySavedValue(40);
    }

    IEnumerator Breed(string id)
    {
        Assert.That(CatBreedCatalog.Load().Find(id), Is.Not.Null, id);
        CatActionState.CancelForTransition(cat);
        if (UnityEditor.SessionState.GetBool("CatHome.QA.PrewarmCareBreed", false))
        {
            TransitionMark("Breed before preload " + id);
            CatPawReachCatalog.Preload(id);
            TransitionMark("Breed preload returned " + id);
            float deadline = Time.realtimeSinceStartup + 15;
            while (!CatPawReachCatalog.IsReadyFor(id) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                TransitionMark("Breed preload wait frame " + id);
            }
            Assert.That(CatPawReachCatalog.IsReadyFor(id), Is.True, "Bounded QA preload must complete");
            TransitionMark("Breed preload ready " + id);
            yield return null;
        }
        TransitionMark("Breed before Select " + id + " old=" + CatBreedService.SelectedBreedId);
        CatBreedService.Select(id);
        TransitionMark("Breed after Select " + id);
        yield return null;
        TransitionMark("Breed after first frame " + id);
        yield return null;
        TransitionMark("Breed after frames " + id);
        float visualDeadline = Time.realtimeSinceStartup + 15;
        while (cat.GetComponentInChildren<CatBreedVisualTag>().BreedId != id && Time.realtimeSinceStartup < visualDeadline)
            yield return null;
        TransitionMark("Breed production visual ready " + id);
        ReadyNeeds();
        Assert.That(cat.GetComponentInChildren<CatBreedVisualTag>().BreedId, Is.EqualTo(id));
    }

    void Place(Vector3 position, Quaternion rotation)
    {
        var controller = cat.GetComponent<CharacterController>();
        bool enabled = controller.enabled; controller.enabled = false;
        position.y = .05f; cat.transform.SetPositionAndRotation(position, rotation);
        controller.enabled = enabled; Physics.SyncTransforms();
    }

    // Probe world-space stances; the test never asks the production resolver
    // for a suggested root. An offered stance must really reach the target.
    void FindCareStance(Transform target, Transform authoredSide, Func<bool> offered)
    {
        Vector3 outward = authoredSide.position - target.position; outward.y = 0;
        if (outward.sqrMagnitude < .0001f) outward = Vector3.back;
        outward.Normalize();
        foreach (float radius in new[] { .30f, .34f, .38f, .42f, .44f, .46f, .26f, .50f })
        foreach (float angle in new[] { 0f, 15f, -15f, 30f, -30f, 60f, -60f, 90f, -90f, 180f })
        {
            Vector3 side = Quaternion.Euler(0, angle, 0) * outward;
            Place(target.position + side * radius, Quaternion.LookRotation(-side));
            if (offered()) return;
        }
        Assert.Fail("No visible, collision-clear care stance: " + target.name + ", breed=" + CatBreedService.SelectedBreedId);
    }

    Button BowlButton(BowlInteraction.BowlSetup setup)
    {
        Refresh(bowls);
        Assert.That(Field<BowlInteraction.BowlSetup>(bowls, "currentBowl"), Is.SameAs(setup));
        var button = Field<Button>(bowls, "interactionButton");
        Assert.That(button != null && button.isActiveAndEnabled && button.interactable, Is.True);
        Assert.That(button.GetComponentInParent<Canvas>(), Is.Not.Null, "Use the actual authored HUD button.");
        return button;
    }

    float ActualMouthDistance(Transform target)
    {
        var animator = cat.GetComponentInChildren<Animator>();
        var profile = CatSipMouthCatalog.Load().Find(cat.GetComponentInChildren<CatBreedVisualTag>().BreedId);
        Assert.That(profile, Is.Not.Null);
        float minimum = float.PositiveInfinity;
        foreach (var vertex in profile.vertices)
        {
            Vector3 world = Vector3.zero;
            foreach (var influence in vertex.influences)
            {
                Transform bone = animator.transform.Find(influence.bonePath);
                Assert.That(bone, Is.Not.Null, influence.bonePath);
                world += bone.TransformPoint(influence.bindPosition) * influence.weight;
            }
            minimum = Mathf.Min(minimum, Vector3.Distance(world, target.position));
        }
        return minimum;
    }

    [UnityTest] public IEnumerator TwoBreeds_ActualButtons_BowlsAndKitchenMeal_KeepRootAndReachFood()
    { yield return CareMatrix(CriticalBreeds()); }

    [UnityTest] public IEnumerator TenBreeds_ActualButtons_BowlsAndKitchenMeal_KeepRootAndReachFood()
    { yield return CareMatrix(CatBreedCatalog.Load().Entries.Select(b => b.Id).ToArray()); }

    IEnumerator CareMatrix(string[] breeds)
    {
        yield return Home();
        foreach (string id in breeds)
        {
            yield return Breed(id);
            foreach (string kind in new[] { "food", "water" }) yield return RunBowl(kind);
        }
        yield return Room(HomeRoomService.KitchenId);
        foreach (string id in breeds) { yield return Breed(id); yield return RunMeal(); }
    }

    IEnumerator RunBowl(string kind)
    {
        ReadyNeeds();
        var setup = Field<BowlInteraction.BowlSetup>(bowls, kind); setup.Fill();
        Transform target = setup.ContactPoint != null ? setup.ContactPoint : setup.Bowl;
        Set(bowls, kind == "food" ? "eatingDuration" : "drinkingDuration", .8f);
        FindCareStance(target, setup.InteractionPoint, () =>
        { Refresh(bowls); return ReferenceEquals(Field<BowlInteraction.BowlSetup>(bowls, "currentBowl"), setup) &&
            CatMealHeadMotion.TryPrepareCareStart(cat, target, out _); });
        var button = BowlButton(setup);
        Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
        float before = kind == "food" ? hunger.CurrentHunger : thirst.CurrentThirst;
        int completions = 0; Action complete = () => completions++;
        if (kind == "food") hunger.Ate += complete; else thirst.Drank += complete;
        float drift = 0, turn = 0, mouth = float.PositiveInfinity;
        int samples = 0; bool sawRecovery = false;
        try
        {
            button.onClick.Invoke();
            Assert.That(bowls.IsInteracting, Is.True, kind);
            Assert.That(Vector3.Distance(origin, cat.transform.position), Is.LessThan(.0001f), "Click does not relocate root.");
            Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.01f), "Click does not turn root.");
            float deadline = Time.realtimeSinceStartup + 15;
            while (bowls.IsInteracting && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                drift = Mathf.Max(drift, Vector3.Distance(origin, cat.transform.position));
                turn = Mathf.Max(turn, Quaternion.Angle(heading, cat.transform.rotation));
                if (bowls.IsAtContact)
                { samples++; mouth = Mathf.Min(mouth, ActualMouthDistance(target)); }
                bool recovering = kind == "food" ? hunger.IsEating : thirst.IsDrinking;
                if (recovering)
                {
                    sawRecovery = true;
                    Assert.That(bowls.IsAtContact, Is.True, "No need gain before care owns actual contact.");
                    Assert.That(mouth, Is.LessThanOrEqualTo(MouthTolerance), "Independent mouth vertices reach the actual food/water.");
                }
            }
            Assert.That(bowls.IsInteracting, Is.False, kind + " completes");
            Assert.That(samples, Is.GreaterThan(8)); Assert.That(sawRecovery, Is.True);
            Assert.That(completions, Is.EqualTo(1), "One real need completion");
            Assert.That(drift, Is.LessThan(RootTolerance)); Assert.That(turn, Is.LessThan(YawTolerance));
            float after = kind == "food" ? hunger.CurrentHunger : thirst.CurrentThirst;
            Assert.That(after - before, Is.GreaterThan(60));
            rows.Add(FormattableString.Invariant($"bowl,{CatBreedService.SelectedBreedId},{kind},{samples},{completions},{drift:F5},{turn:F3},{mouth:F5},{after-before:F3}"));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(CatMealHeadMotion.TryPrepareCareStart(cat,target,out _), Is.True,
                "Release keeps fresh standing skin and ordinary blockers clear; fur proxy alone is not exact skin.");
        }
        finally { if (kind == "food") hunger.Ate -= complete; else thirst.Drank -= complete; }
    }

    IEnumerator RunMeal()
    {
        ReadyNeeds();
        var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene);
        FindCareStance(meal.BowlPoint, Field<Transform>(meal, "standPoint"), () => meal.TryGetPromptDistance(cat, out _));
        var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
        Refresh(bowls); Refresh(prompt, "RefreshImmediate");
        Assert.That(Field<CatActivity>(prompt, "candidate"), Is.SameAs(meal), "The real UI selects this meal.");
        var button = Field<Button>(prompt, "actionButton");
        Assert.That(button.isActiveAndEnabled && button.interactable, Is.True);
        Assert.That(button.GetComponentInParent<Canvas>(), Is.Not.Null);
        Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
        float before = hunger.CurrentHunger, drift = 0, turn = 0, mouth = float.PositiveInfinity;
        int completions = 0, samples = 0;
        Action<CatActivity> complete = a => { if (a == meal) completions++; };
        CatActivity.Completed += complete;
        try
        {
            button.onClick.Invoke(); Assert.That(meal.IsRunning, Is.True);
            Assert.That(meal.InspectingOnly, Is.False, "Hungry fixture must exercise a real meal.");
            Assert.That(Vector3.Distance(origin, cat.transform.position), Is.LessThan(.0001f));
            Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.01f));
            float deadline = Time.realtimeSinceStartup + 15;
            while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                drift = Mathf.Max(drift, Vector3.Distance(origin, cat.transform.position));
                turn = Mathf.Max(turn, Quaternion.Angle(heading, cat.transform.rotation));
                if (meal.IsEating) { samples++; if (samples > 20) mouth = Mathf.Min(mouth, ActualMouthDistance(meal.BowlPoint)); }
            }
            Assert.That(meal.IsRunning, Is.False); Assert.That(completions, Is.EqualTo(1));
            Assert.That(samples, Is.GreaterThan(45)); Assert.That(mouth, Is.LessThanOrEqualTo(MouthTolerance));
            Assert.That(drift, Is.LessThan(RootTolerance)); Assert.That(turn, Is.LessThan(YawTolerance));
            Assert.That(hunger.CurrentHunger - before, Is.GreaterThan(35));
            rows.Add(FormattableString.Invariant($"meal,{CatBreedService.SelectedBreedId},food,{samples},{completions},{drift:F5},{turn:F3},{mouth:F5},{hunger.CurrentHunger-before:F3}"));
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(CatMealHeadMotion.TryPrepareCareStart(cat,meal.BowlPoint,out _), Is.True);
        }
        finally { CatActivity.Completed -= complete; }
    }

    [UnityTest] public IEnumerator StaleBowlYaw_RejectedWithoutMovementOrNeedGain()
    {
        yield return Home(); yield return Breed(CriticalBreeds()[0]);
        var setup = Field<BowlInteraction.BowlSetup>(bowls, "food"); setup.Fill();
        Transform target = setup.ContactPoint != null ? setup.ContactPoint : setup.Bowl;
        FindCareStance(target, setup.InteractionPoint, () =>
        { Refresh(bowls); return ReferenceEquals(Field<BowlInteraction.BowlSetup>(bowls, "currentBowl"), setup) &&
            CatMealHeadMotion.TryPrepareCareStart(cat, target, out _); });
        var button = BowlButton(setup);
        Place(cat.transform.position, cat.transform.rotation * Quaternion.Euler(0, 180, 0));
        Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
        float before = hunger.CurrentHunger;
        // Deliberately click the rendered old candidate before the next Update.
        button.onClick.Invoke();
        Assert.That(bowls.IsInteracting || hunger.IsEating, Is.False);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cat.transform.position, Is.EqualTo(origin)); Assert.That(cat.transform.rotation, Is.EqualTo(heading));
        Assert.That(hunger.CurrentHunger, Is.EqualTo(before));
        Refresh(bowls); Assert.That(Field<BowlInteraction.BowlSetup>(bowls, "currentBowl"), Is.SameAs(setup));
    }

    void FindBedStance()
    {
        Transform floor = Field<Transform>(sleep, "bedInteractionPoint");
        foreach (float radius in new[] { 0f, .06f, .12f, .18f })
        for (int i = 0; i < 16; i++)
        {
            Vector3 point = floor.position + Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward * radius;
            Vector3 inward = sleep.SleepSurface.position - point; inward.y = 0;
            Place(point, Quaternion.LookRotation(inward));
            if (sleep.WantsActionButton) return;
        }
        Assert.Fail("No body-clear bed launch for " + CatBreedService.SelectedBreedId);
    }

    Button SleepButton()
    {
        Refresh(bowls);
        var button = Field<Button>(bowls, "interactionButton");
        Assert.That(Field<bool>(bowls, "showingSleepStyle"), Is.True);
        Assert.That(button.isActiveAndEnabled && button.interactable, Is.True);
        Assert.That(button.GetComponentInParent<Canvas>(), Is.Not.Null);
        return button;
    }

    IEnumerator PauseAndCheckPose()
    {
        Time.timeScale = 0;
        Vector3 position = cat.transform.position; Quaternion heading = cat.transform.rotation;
        var driver = cat.GetComponent<CatActivityAnimation>(); float phase = driver.NativeJumpPhase;
        for (int frame = 0; frame < 6; frame++)
        {
            yield return new WaitForEndOfFrame();
            Assert.That(Vector3.Distance(position, cat.transform.position), Is.LessThan(.0001f), "Pause holds root.");
            Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.01f), "Pause holds yaw.");
            Assert.That(driver.NativeJumpPhase, Is.EqualTo(phase).Within(.0001f));
        }
        Time.timeScale = 1;
    }

    [UnityTest] public IEnumerator TwoBreeds_Bed_NativeJumpPauseCancel_SettledSleepAndWakeRelease()
    {
        yield return Home();
        foreach (string id in CriticalBreeds())
        {
            yield return Breed(id); FindBedStance();
            Vector3 floor = cat.transform.position; Quaternion heading = cat.transform.rotation;
            int starts = 0; Action started = () => starts++; sleep.SleepStarted += started;
            try
            {
                SleepButton().onClick.Invoke();
                Assert.That(sleep.IsSleeping, Is.True); Assert.That(sleep.IsSettledOnBed, Is.False);
                Assert.That(Vector3.Distance(floor, cat.transform.position), Is.LessThan(.0001f), "No bed teleport on click.");
                Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.01f), "No launch alignment turn.");
                var driver = cat.GetComponent<CatActivityAnimation>();
                float deadline = Time.realtimeSinceStartup + 10;
                while ((!driver.IsNativeJump || driver.NativeJumpPhase <= CatJumpMotion.Takeoff + .02f) &&
                    Time.realtimeSinceStartup < deadline) yield return new WaitForEndOfFrame();
                Assert.That(driver.IsNativeJump, Is.True);
                Assert.That(cat.transform.position.y, Is.GreaterThan(floor.y + .04f), "Source jump is actually airborne.");
                Assert.That(sleep.IsSettledOnBed, Is.False, "Mattress contact must not pull the ascending cat onto the bed.");
                yield return PauseAndCheckPose();
                Time.timeScale = 0; sleep.CancelForTransition();
                Assert.That(sleep.IsSleeping || sleep.IsSettledOnBed || driver.IsActive, Is.False);
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False); Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
                Assert.That(Vector3.Distance(cat.transform.position, floor), Is.LessThan(RootTolerance));
                Assert.That(Quaternion.Angle(cat.transform.rotation, heading), Is.LessThan(YawTolerance));
                Time.timeScale = 1; ReadyNeeds();
                SleepButton().onClick.Invoke(); Assert.That(sleep.IsSleeping, Is.True);
                deadline = Time.realtimeSinceStartup + 12;
                while ((!sleep.IsSettledOnBed || Field<Coroutine>(sleep, "sleepCoroutine") != null) &&
                    Time.realtimeSinceStartup < deadline) yield return new WaitForEndOfFrame();
                Assert.That(sleep.IsSettledOnBed, Is.True); Assert.That(Field<Coroutine>(sleep, "sleepCoroutine"), Is.Null);
                Assert.That(Mathf.Abs(cat.transform.position.y - sleep.SleepSurface.position.y), Is.LessThan(.015f));
                Assert.That(cat.IsMovementPhysicallyLocked, Is.True); Assert.That(driver.IsActive, Is.False);
                yield return PauseAndCheckPose();
                Vector3 settled = cat.transform.position;
                SleepButton().onClick.Invoke();
                Assert.That(sleep.IsSleeping, Is.True, "Ownership survives the real wake/exit motion.");
                Assert.That(Vector3.Distance(settled, cat.transform.position), Is.LessThan(.0001f), "Wake is not a floor teleport.");
                bool exitJump = false; float lowest = settled.y;
                deadline = Time.realtimeSinceStartup + 12;
                while (sleep.IsSleeping && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForEndOfFrame();
                    exitJump |= driver.IsNativeJump;
                    lowest = Mathf.Min(lowest, cat.transform.position.y);
                }
                Assert.That(sleep.IsSleeping, Is.False); Assert.That(exitJump, Is.True);
                Assert.That(starts, Is.EqualTo(2), "Cancelled attempt and retry each emit one explicit sleep start.");
                Assert.That(lowest, Is.LessThan(settled.y - .10f));
                Assert.That(Vector3.Distance(cat.transform.position, floor), Is.LessThan(RootTolerance));
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False); Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
                Assert.That(cat.IsBodyPoseClear(cat.transform.position, cat.transform.rotation), Is.True, "Native landing releases into clear space.");
                rows.Add("sleep," + id + ",bed,0," + starts + ",0,0,0,0");
            }
            finally { sleep.SleepStarted -= started; Time.timeScale = 1; }
        }
    }
    [UnityTest] public IEnumerator SleepTransitions_CopiedSaveCapturesSafeFloor_AndOnlySettledRestRestoresEnergy()
    {
        string[] realFiles = Directory.GetFiles(Application.persistentDataPath)
            .Where(p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".recovery", StringComparison.OrdinalIgnoreCase)).ToArray();
        var originalFiles = realFiles.ToDictionary(p => p, File.ReadAllBytes);
        try
        {
            yield return Home(); yield return Breed(CriticalBreeds()[0]); FindBedStance();
            Vector3 floor = cat.transform.position; Quaternion heading = cat.transform.rotation;
            float before = energy.CurrentEnergy;
            SleepButton().onClick.Invoke();
            var driver = cat.GetComponent<CatActivityAnimation>();
            float deadline = Time.realtimeSinceStartup + 10;
            while ((!driver.IsNativeJump || driver.NativeJumpPhase <= CatJumpMotion.Takeoff + .03f) &&
                Time.realtimeSinceStartup < deadline) yield return new WaitForEndOfFrame();
            Assert.That(cat.transform.position.y, Is.GreaterThan(floor.y + .04f));
            Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(before + .01f), "Entering the bed earns no sleep energy.");
            Time.timeScale = 0; yield return null;
            AssertSavedSleep(false, floor, heading, "paused ascent");
            Time.timeScale = 1;
            deadline = Time.realtimeSinceStartup + 10;
            while (!sleep.IsSettledOnBed && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(sleep.IsSettledOnBed, Is.True);
            before = energy.CurrentEnergy;
            yield return new WaitForSeconds(.25f);
            Assert.That(energy.CurrentEnergy, Is.GreaterThan(before), "Actual supported rest restores energy.");
            Time.timeScale = 0; yield return null;
            AssertSavedSleep(true, cat.transform.position, cat.transform.rotation, "settled sleep");
            Time.timeScale = 1;
            // Stop during the wake motion, before it reaches the downward jump.
            SleepButton().onClick.Invoke();
            Assert.That(sleep.IsSleeping && !sleep.IsSettledOnBed, Is.True);
            before = energy.CurrentEnergy;
            yield return new WaitForSeconds(.25f);
            Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(before + .01f), "Wake/exit earns no sleep energy.");
            Time.timeScale = 0; yield return null;
            AssertSavedSleep(false, floor, heading, "paused wake");
            Time.timeScale = 1;
            deadline = Time.realtimeSinceStartup + 10;
            while ((!driver.IsNativeJump || driver.NativeJumpPhase <= CatJumpMotion.Takeoff + .03f) &&
                Time.realtimeSinceStartup < deadline) yield return new WaitForEndOfFrame();
            Assert.That(driver.IsNativeJump, Is.True);
            Time.timeScale = 0; yield return null;
            AssertSavedSleep(false, floor, heading, "paused descent");
            sleep.CancelForTransition();
        }
        finally
        {
            Time.timeScale = 1;
            if (cat != null) CatActionState.CancelForTransition(cat);
            string[] currentFiles = Directory.GetFiles(Application.persistentDataPath)
                .Where(p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".recovery", StringComparison.OrdinalIgnoreCase)).ToArray();
            CollectionAssert.AreEquivalent(realFiles, currentFiles, "The test must not create or remove actual saves.");
            foreach (var file in originalFiles)
                CollectionAssert.AreEqual(file.Value, File.ReadAllBytes(file.Key), "Actual save bytes changed: " + Path.GetFileName(file.Key));
        }
    }

    void AssertSavedSleep(bool sleeping, Vector3 position, Quaternion rotation, string phase)
    {
        CatHomeSaveSystem.SaveForSuspension();
        Assert.That(File.Exists(CatHomeSaveSystem.SaveFilePath), Is.True);
        var saved = JsonUtility.FromJson<CatHomeSaveData>(File.ReadAllText(CatHomeSaveSystem.SaveFilePath));
        Assert.That(saved.wasSleeping, Is.EqualTo(sleeping), phase);
        Assert.That(Vector3.Distance(saved.catWorldPosition, position), Is.LessThan(.001f), phase + " safe saved position");
        Assert.That(Quaternion.Angle(saved.catWorldRotation, rotation), Is.LessThan(.01f), phase + " safe saved rotation");
        Assert.That((bool)typeof(CatHomeSaveSystem).GetField("suspendedWhileSleeping", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null),
            Is.EqualTo(sleeping), phase + " offline rest eligibility");
        CatHomeSaveSystem.ResumeAfterSuspension();
        rows.Add("save," + CatBreedService.SelectedBreedId + "," + phase + ",0,0,0,0,0,0");
    }

    [UnityTest] public IEnumerator NativeCareStanceDiagnostic()
    {
        yield return Home();
        var measurements = new List<string> { "room,breed,target,radius,angle,bodyClear,controllerClear,careGate,zoneDistance,mouthHeightDelta" };
        var dimensions = new List<string>();
        foreach (string room in new[] { HomeRoomService.LivingRoomId, HomeRoomService.KitchenId })
        {
            yield return Room(room);
            foreach (string breed in CriticalBreeds())
            {
                yield return Breed(breed);
                var cc = cat.GetComponent<CharacterController>();
                var profile = CatFeedingAlignmentCatalog.Load().Find(breed);
                var guard = Field<CatBodyGuard>(cat, "bodyGuard");
                dimensions.Add(room + "," + breed + ",rootScale=" + cat.transform.lossyScale +
                    ",cc=" + cc.center + ";" + cc.height + ";" + cc.radius + ";" + cc.skinWidth +
                    ",mouthOffset=" + profile.mouthOffset);
                Transform[] targets = room == HomeRoomService.LivingRoomId
                    ? new[] { Field<BowlInteraction.BowlSetup>(bowls, "food").ContactPoint, Field<BowlInteraction.BowlSetup>(bowls, "water").ContactPoint }
                    : new[] { CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene).BowlPoint };
                for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
                {
                    Transform target = targets[targetIndex];
                    for (int angle = 0; angle < 360; angle += 10)
                    {
                        Vector3 side = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                        for (int radiusStep = 0; radiusStep <= 26; radiusStep++)
                        {
                            float radius = .20f + radiusStep * .02f;
                            Place(target.position + side * radius, Quaternion.LookRotation(-side));
                            bool body = cat.IsBodyPoseClear(cat.transform.position, cat.transform.rotation);
                            bool controller = guard.IsControllerClear(cat.transform.position, cat.transform.rotation);
                            bool ready = CatMealHeadMotion.TryPrepareCareStart(cat, target, out var start);
                            Vector3 mouth = cat.transform.position + cat.transform.rotation * profile.mouthOffset * (cat.transform.lossyScale.y / .5f);
                            measurements.Add(FormattableString.Invariant($"{room},{breed},{targetIndex},{radius:F3},{angle},{body},{controller},{ready},{start.PromptDistance:F4},{mouth.y-target.position.y:F4}"));
                        }
                        yield return null;
                    }
                }
            }
        }
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "care-stance-diagnostic.csv"), measurements);
        File.WriteAllLines(Path.Combine(Output, "care-stance-dimensions.txt"), dimensions);
    }
    // Insert in CareAlignmentPolishTests after the current native run ends.
    // Diagnostic only: a clear test pose bypasses the use-zone gate, never the
    // anatomical/controller collision checks. Runtime gates remain unchanged.
    [UnityTest] public IEnumerator NativeCarePhysicalReachDiagnostic()
    {
        yield return Home();
        var evidence = new List<string> { "breed,kind,radius,angle,frame,active,atContact,actualMouth,headDistance,minHeadDistance,requiredReach,chainLength,deficit,forequarter,totalNeck,pawError,rootDrift,yawDrift" };
        foreach (string breed in CriticalBreeds())
        {
            yield return Breed(breed);
            foreach (string kind in new[] { "food", "water" })
            foreach (float radius in new[] { .44f, .48f, .50f, .52f, .54f })
            {
                ReadyNeeds();
                var setup = Field<BowlInteraction.BowlSetup>(bowls, kind); setup.Fill();
                Transform target = setup.ContactPoint != null ? setup.ContactPoint : setup.Bowl;
                int chosenAngle = -1;
                for (int angle = 0; angle < 360; angle += 10)
                {
                    Vector3 side = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    Place(target.position + side * radius, Quaternion.LookRotation(-side));
                    if (!cat.IsInteractionPoseClear(cat.transform.position, cat.transform.rotation)) continue;
                    chosenAngle = angle; break;
                }
                if (chosenAngle < 0)
                { evidence.Add(breed + "," + kind + "," + radius.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",NO_CLEAR_POSE"); continue; }
                Set(bowls, kind == "food" ? "eatingDuration" : "drinkingDuration", .25f);
                Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
                var sequence = (IEnumerator)typeof(BowlInteraction).GetMethod("PerformInteraction", Private).Invoke(bowls, new object[] { setup });
                bowls.StartCoroutine(sequence);
                Assert.That(bowls.IsInteracting, Is.True, "Diagnostic starts the production contact routine.");
                int frame = 0; float deadline = Time.realtimeSinceStartup + 6;
                while (bowls.IsInteracting && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForEndOfFrame(); frame++;
                    var head = cat.GetComponent<CatMealHeadMotion>();
                    if (head == null) continue;
                    float actual = ActualMouthDistance(target);
                    evidence.Add(FormattableString.Invariant($"{breed},{kind},{radius:F3},{chosenAngle},{frame},{head.IsActive},{bowls.IsAtContact},{actual:F5},{head.Distance:F5},{head.MinimumDistance:F5},{head.RequiredReach:F5},{head.PhysicalChainLength:F5},{head.ReachDeficit:F5},{head.ForequarterDeflection:F3},{head.TotalDeflection:F3},{head.PawPlantError:F5},{Vector3.Distance(origin,cat.transform.position):F5},{Quaternion.Angle(heading,cat.transform.rotation):F3}"));
                }
                bowls.CancelInteraction();
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            }
        }
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "care-physical-reach-diagnostic.csv"), evidence);
    }

// Insert these members inside CareAlignmentPolishTests after the native run stops.
// Uses its existing Home(), Breed(), Place(), Field<T>(), Output and copied-save teardown.
// Diagnostic only: it does not enlarge gates, write assets or claim a collision pass.
// 2 breeds x 3 radii x 36 headings = 216 stances. All four guard regions are emitted.
// Current native skin and four source Idle phases are compared against the SAME
// solids accepted by CatBodyGuard.Solid; floors are included and named in the CSV.

// QA-only insert inside CareAlignmentPolishTests; existing helpers and copied-save cleanup stay unchanged.
[UnityTest, Timeout(300000)]
public IEnumerator NativeKitchenCareBlockerAndReachDiagnostic()
{
    yield return Home();
    yield return Room(HomeRoomService.KitchenId);
    var detail = new List<string> {
        "breed,radius,angle,bodyClear,controllerClear,careGate,region,collider,colliderType,meshAsset,probeBlocks,probeDepth,probeNormal,probeStart,probeEnd,probeRadius,currentSkinDepth,currentInsideVertices,idleSkinDepth,idleInsideVertices,currentDeepest,idleDeepest,profilePoses,profileVertices,sourceStart,sourceEnd,sourceRadius,currentState,currentPhase,idleClip,idlePhases,currentSkinBounds,idleSkinBounds,careDistance,mouthDistance,colliderBounds,controllerHeight,controllerRadius,controllerCentre"
    };
    var summary = new List<string> {
        "breed,radius,angle,bodyClear,controllerClear,careGate,blockingRegions,blockingColliders,maxProbeDepth,maxCurrentSkinDepth,maxIdleSkinDepth,careDistance,mouthDistance,blockingControllerColliders,maxControllerDepth,maxBlockingControllerDepth,maxSkinDepthAtBlockingController"
    };
    var reachRows = new List<string> {
        "breed,frame,running,eating,root,yaw,target,actualMouth,headDistance,minHeadDistance,sourceDistance,requiredReach,chainLength,deficit,forequarter,totalNeck,pawError,rootDrift,yawDrift,completions"
    };
    var mesh = new Mesh { name = "QA care blocker skin" };
    try
    {
        foreach (string breedId in CriticalBreeds())
        {
            // Use precisely RunMeal's ordered stance search and real HUD path.
            // Complete both breed runs before any diagnostic grid/clip sampling.
            yield return Breed(breedId);
            ReadyNeeds();
            var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene);
            Transform target = meal.BowlPoint;
            Assert.That(target, Is.Not.Null);
            FindCareStance(target, Field<Transform>(meal, "standPoint"),
                () => meal.TryGetPromptDistance(cat, out _));
            Vector3 firstReadyPosition = cat.transform.position;
            Quaternion firstReadyRotation = cat.transform.rotation;
            var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
            Refresh(bowls); Refresh(prompt, "RefreshImmediate");
            Assert.That(Field<CatActivity>(prompt, "candidate"), Is.SameAs(meal));
            var button = Field<Button>(prompt, "actionButton");
            Assert.That(button != null && button.isActiveAndEnabled && button.interactable, Is.True);
            // Preserve the accepted stance even if the click/start assertion fails.
            reachRows.Add(CareBlockerCsv(breedId, "ready", meal.IsRunning, meal.IsEating,
                cat.transform.position, cat.transform.eulerAngles.y, target.position, ActualMouthDistance(target),
                "", "", "", "", "", "", "", "", "", 0f, 0f, 0));
            int completions = 0;
            Action<CatActivity> completed = a => { if (a == meal) completions++; };
            CatActivity.Completed += completed;
            try
            {
                button.onClick.Invoke(); Assert.That(meal.IsRunning, Is.True);
                int frame = 0; float deadline = Time.realtimeSinceStartup + 12f;
                while (meal.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForEndOfFrame(); frame++;
                    var head = cat.GetComponent<CatMealHeadMotion>();
                    if (head == null) continue;
                    reachRows.Add(CareBlockerCsv(breedId, frame, meal.IsRunning, meal.IsEating,
                        cat.transform.position, cat.transform.eulerAngles.y, target.position, ActualMouthDistance(target),
                        head.Distance, head.MinimumDistance, head.SourceDistance, head.RequiredReach, head.PhysicalChainLength,
                        head.ReachDeficit, head.ForequarterDeflection, head.TotalDeflection, head.PawPlantError,
                        Vector3.Distance(firstReadyPosition, cat.transform.position), Quaternion.Angle(firstReadyRotation, cat.transform.rotation), completions));
                }
                Assert.That(meal.IsRunning, Is.False, "A contact failure must still release its controller and lock.");
                // Completion/contact are measurements in this diagnostic, not
                // assertions that would discard the failing Persian telemetry.
                reachRows.Add(CareBlockerCsv(breedId, "final", meal.IsRunning, meal.IsEating,
                    cat.transform.position, cat.transform.eulerAngles.y, target.position, ActualMouthDistance(target),
                    "", "", "", "", "", "", "", "", "",
                    Vector3.Distance(firstReadyPosition, cat.transform.position), Quaternion.Angle(firstReadyRotation, cat.transform.rotation), completions));
            }
            finally { CatActivity.Completed -= completed; CatActionState.CancelForTransition(cat); }
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        }

        // Independent geometry inventory: these deliberately sparse angles are
        // never used to decide whether the actual-button reach run may execute.
        foreach (string breedId in CriticalBreeds())
        {
            yield return Breed(breedId);
            var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == cat.gameObject.scene);
            Transform target = meal.BowlPoint;
            Assert.That(target, Is.Not.Null);
            Vector3 outward = Field<Transform>(meal, "standPoint").position - target.position; outward.y = 0;
            Assert.That(outward.sqrMagnitude, Is.GreaterThan(.0001f)); outward.Normalize();
            var guard = Field<CatBodyGuard>(cat, "bodyGuard");
            var catalog = Resources.Load<CatBodyGuardCatalog>(CatBodyGuardCatalog.ResourceName);
            var profile = catalog.Find(breedId);
            Assert.That(guard != null && profile != null && guard.ProbeCount == 4, Is.True);
            var runtimeProbes = Field<CatBodyGuardCatalog.Probe[]>(guard, "probes");
            var cc = cat.GetComponent<CharacterController>();
            QaCCBodyMeasure.Controller(cc, cat.transform, out var ccCentreLocal, out var ccStartLocal, out var ccEndLocal,
                out float ccRadius, out float ccHeight);
            var solidMethod = typeof(CatBodyGuard).GetMethod("Solid", Private);
            var penetration = typeof(CatBodyGuard).GetMethod("Penetration", Private);
            Assert.That(solidMethod != null && penetration != null, Is.True);

            // Capture local skin once per breed. The Idle clip samples only this
            // copied QA actor, and every local transform is restored immediately.
            var animator = cat.GetComponentInChildren<Animator>();
            var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
            var breed = CatBreedCatalog.Load().Find(breedId);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            var current = CareBlockerSkin(skin, mesh, breed.ContactVertexIndices);
            var idleClip = animator.runtimeAnimatorController.animationClips.First(c => c.name.EndsWith("|Idle", StringComparison.Ordinal));
            var transforms = animator.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var rotations = transforms.Select(t => t.localRotation).ToArray();
            var scales = transforms.Select(t => t.localScale).ToArray();
            var idle = new List<Vector3>();
            try
            {
                foreach (float phase in new[] { 0f, .25f, .5f, .75f })
                {
                    for (int i = 0; i < transforms.Length; i++)
                    { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                    idleClip.SampleAnimation(animator.gameObject, idleClip.length * phase);
                    idle.AddRange(CareBlockerSkin(skin, mesh, breed.ContactVertexIndices));
                }
            }
            finally
            {
                for (int i = 0; i < transforms.Length; i++)
                { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
            }

            foreach (float radius in new[] { .26f, .30f, .34f, .38f, .40f, .42f, .44f, .46f })
            foreach (int angle in new[] { 0, 10, -10, 20, -20, 30, -30 })
            {
                Vector3 side = Quaternion.Euler(0, angle, 0) * outward;
                Place(target.position + side * radius, Quaternion.LookRotation(-side));
                Physics.SyncTransforms();
                Vector3 position = cat.transform.position; Quaternion rotation = cat.transform.rotation;
                bool bodyClear = cat.IsBodyPoseClear(position, rotation);
                bool controllerClear = guard.IsControllerClear(position, rotation);
                bool careGate = CatMealHeadMotion.TryPrepareCareStart(cat, target, out var careStart);
                var feeding = CatFeedingAlignmentCatalog.Load().Find(breedId);
                Vector3 predictedMouth = position + rotation * feeding.mouthOffset * (cat.transform.lossyScale.y / .5f);
                float mouthDistance = Vector3.Distance(predictedMouth, target.position);
                Vector3[] currentWorld = current.Select(p => cat.transform.TransformPoint(p)).ToArray();
                Vector3[] idleWorld = idle.Select(p => cat.transform.TransformPoint(p)).ToArray();
                Bounds currentBounds = CareBlockerBounds(currentWorld), idleBounds = CareBlockerBounds(idleWorld);
                Bounds allBounds = currentBounds; allBounds.Encapsulate(idleBounds);
                var obstacles = new HashSet<Collider>();
                foreach (var collider in Physics.OverlapBox(allBounds.center, allBounds.extents + Vector3.one * .002f,
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    if ((bool)solidMethod.Invoke(guard, new object[] { collider })) obstacles.Add(collider);
                Vector3 ccA = position + rotation * ccStartLocal, ccB = position + rotation * ccEndLocal;
                Vector3 ccCentre = position + rotation * ccCentreLocal;
                var ccCandidates = Physics.OverlapCapsule(ccA, ccB, ccRadius, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => (bool)solidMethod.Invoke(guard, new object[] { c })).ToArray();
                foreach (var collider in ccCandidates) obstacles.Add(collider);
                var candidates = new Collider[runtimeProbes.Length][];
                for (int i = 0; i < runtimeProbes.Length; i++)
                {
                    var p = runtimeProbes[i];
                    candidates[i] = Physics.OverlapCapsule(position + rotation * p.start, position + rotation * p.end,
                        p.radius, ~0, QueryTriggerInteraction.Ignore).Where(c => (bool)solidMethod.Invoke(guard, new object[] { c })).ToArray();
                    foreach (var collider in candidates[i]) obstacles.Add(collider);
                }
                var currentDepth = new Dictionary<Collider, CareBlockerSkinResult>();
                var idleDepth = new Dictionary<Collider, CareBlockerSkinResult>();
                foreach (var collider in obstacles)
                {
                    currentDepth[collider] = CareBlockerDepth(guard, penetration, collider, currentWorld);
                    idleDepth[collider] = CareBlockerDepth(guard, penetration, collider, idleWorld);
                }
                var blockingController = new HashSet<string>();
                float maxControllerDepth = 0f, maxBlockingControllerDepth = 0f, maxControllerSkin = 0f;
                foreach (var collider in ccCandidates.Length == 0 ? new Collider[] { null } : ccCandidates)
                {
                    Vector3 normal = Vector3.zero; float depth = 0f;
                    bool penetrates = collider != null && CareBlockerPenetrates(guard, penetration, ccA, ccB, ccRadius,
                        collider, out normal, out depth);
                    bool blocks = penetrates && normal.y < .8f && depth > .002f;
                    string path = collider != null ? CareBlockerPath(collider.transform) : "<clear>";
                    var now = collider != null ? currentDepth[collider] : default;
                    var sourceIdle = collider != null ? idleDepth[collider] : default;
                    maxControllerDepth = Mathf.Max(maxControllerDepth, depth);
                    if (blocks)
                    {
                        blockingController.Add(path); maxBlockingControllerDepth = Mathf.Max(maxBlockingControllerDepth, depth);
                        maxControllerSkin = Mathf.Max(maxControllerSkin, now.depth, sourceIdle.depth);
                    }
                    detail.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate, "controller",
                        path, collider != null ? collider.GetType().Name : "", collider is MeshCollider mc ? UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh) : "",
                        blocks, depth, normal, ccA, ccB, ccRadius, now.depth, now.inside, sourceIdle.depth, sourceIdle.inside, now.point, sourceIdle.point,
                        0, 0, ccStartLocal, ccEndLocal, ccRadius, state.fullPathHash, state.normalizedTime,
                        idleClip.name, "0;.25;.5;.75", currentBounds, idleBounds, careStart.PromptDistance, mouthDistance,
                        collider != null ? collider.bounds : default(Bounds), ccHeight, ccRadius, ccCentre));
                }
                Assert.That(blockingController.Count == 0, Is.EqualTo(controllerClear),
                    "Explicit controller contacts must reproduce the production gate exactly.");
                var blockingRegions = new HashSet<string>(); var blockingColliders = new HashSet<string>();
                float maxProbe = 0f;
                for (int i = 0; i < runtimeProbes.Length; i++)
                {
                    var probe = runtimeProbes[i];
                    Vector3 a = position + rotation * probe.start, b = position + rotation * probe.end;
                    // A clear region still has one row; overlapping but allowed
                    // contacts retain depth/normal instead of disappearing.
                    foreach (var collider in candidates[i].Length == 0 ? new Collider[] { null } : candidates[i])
                    {
                        Vector3 normal = Vector3.zero; float depth = 0f;
                        bool penetrates = collider != null && CareBlockerPenetrates(guard, penetration, a, b, probe.radius, collider, out normal, out depth);
                        bool blocks = penetrates && depth > .015f;
                        string path = collider != null ? CareBlockerPath(collider.transform) : "<clear>";
                        if (blocks) { blockingRegions.Add(probe.region); blockingColliders.Add(path); }
                        maxProbe = Mathf.Max(maxProbe, depth);
                        var now = collider != null ? currentDepth[collider] : default;
                        var sourceIdle = collider != null ? idleDepth[collider] : default;
                        var source = profile.probes.First(p => p.region == probe.region);
                        detail.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate, probe.region,
                            path, collider != null ? collider.GetType().Name : "", collider is MeshCollider mc ? UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh) : "",
                            blocks, depth, normal, a, b, probe.radius, now.depth, now.inside, sourceIdle.depth, sourceIdle.inside, now.point, sourceIdle.point,
                            profile.sampledPoses, profile.sampledVertices, source.start, source.end, source.radius, state.fullPathHash, state.normalizedTime,
                            idleClip.name, "0;.25;.5;.75", currentBounds, idleBounds, careStart.PromptDistance, mouthDistance,
                            collider != null ? collider.bounds : default(Bounds), ccHeight, ccRadius, ccCentre));
                    }
                }
                summary.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate,
                    string.Join(";", blockingRegions), string.Join(";", blockingColliders), maxProbe,
                    currentDepth.Count == 0 ? 0f : currentDepth.Values.Max(v => v.depth),
                    idleDepth.Count == 0 ? 0f : idleDepth.Values.Max(v => v.depth), careStart.PromptDistance, mouthDistance,
                    string.Join(";", blockingController), maxControllerDepth, maxBlockingControllerDepth, maxControllerSkin));
                // One stance per native frame keeps the editor responsive.
                yield return null;
            }

        }
        Assert.That(summary.Count - 1, Is.EqualTo(112));
    }
    finally
    {
        Object.DestroyImmediate(mesh);
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "kitchen-care-blockers.csv"), detail);
        File.WriteAllLines(Path.Combine(Output, "kitchen-care-blockers-summary.csv"), summary);
        File.WriteAllLines(Path.Combine(Output, "kitchen-care-reach.csv"), reachRows);
    }
}


[UnityTest, Timeout(300000)]
public IEnumerator NativeCareFrontArcDiagnostic()
{
    yield return Home();
    var detail = new List<string> {
        "breed,radius,angle,bodyClear,controllerClear,careGate,region,collider,colliderType,meshAsset,probeBlocks,probeDepth,probeNormal,probeStart,probeEnd,probeRadius,currentSkinDepth,currentInsideVertices,idleSkinDepth,idleInsideVertices,currentDeepest,idleDeepest,profilePoses,profileVertices,sourceStart,sourceEnd,sourceRadius,currentState,currentPhase,idleClip,idlePhases,currentSkinBounds,idleSkinBounds,careDistance,mouthDistance,colliderBounds,controllerHeight,controllerRadius,controllerCentre"
    };
    var summary = new List<string> {
        "breed,radius,angle,bodyClear,controllerClear,careGate,blockingRegions,blockingColliders,maxProbeDepth,maxCurrentSkinDepth,maxIdleSkinDepth,careDistance,mouthDistance,blockingControllerColliders,maxControllerDepth,maxBlockingControllerDepth,maxSkinDepthAtBlockingController"
    };
    var bodyMeasure = new List<string> { QaCCBodyMeasure.Header };
    var mesh = new Mesh { name = "QA care blocker skin" };
    try
    {
        foreach (string breedId in new[] { "persian", "oriental-shorthair" })
        {
            yield return Breed(breedId);
            var setup = Field<BowlInteraction.BowlSetup>(bowls, "food");
            Assert.That(setup?.ContactPoint, Is.Not.Null);
            Transform target = setup.ContactPoint;
            Vector3 outward = setup.InteractionPoint.position - target.position; outward.y = 0;
            Assert.That(outward.sqrMagnitude, Is.GreaterThan(.0001f)); outward.Normalize();
            var guard = Field<CatBodyGuard>(cat, "bodyGuard");
            var catalog = Resources.Load<CatBodyGuardCatalog>(CatBodyGuardCatalog.ResourceName);
            var profile = catalog.Find(breedId);
            Assert.That(guard != null && profile != null && guard.ProbeCount == 4, Is.True);
            var runtimeProbes = Field<CatBodyGuardCatalog.Probe[]>(guard, "probes");
            yield return QaCCBodyMeasure.Capture(cat, runtimeProbes, mesh, breedId, bodyMeasure);
            var cc = cat.GetComponent<CharacterController>();
            QaCCBodyMeasure.Controller(cc, cat.transform, out var ccCentreLocal, out var ccStartLocal, out var ccEndLocal,
                out float ccRadius, out float ccHeight);
            var solidMethod = typeof(CatBodyGuard).GetMethod("Solid", Private);
            var penetration = typeof(CatBodyGuard).GetMethod("Penetration", Private);
            Assert.That(solidMethod != null && penetration != null, Is.True);

            // Capture local skin once per breed. The Idle clip samples only this
            // copied QA actor, and every local transform is restored immediately.
            var animator = cat.GetComponentInChildren<Animator>();
            var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
            var breed = CatBreedCatalog.Load().Find(breedId);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            var current = CareBlockerSkin(skin, mesh, breed.ContactVertexIndices);
            var idleClip = animator.runtimeAnimatorController.animationClips.First(c => c.name.EndsWith("|Idle", StringComparison.Ordinal));
            var transforms = animator.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var rotations = transforms.Select(t => t.localRotation).ToArray();
            var scales = transforms.Select(t => t.localScale).ToArray();
            var idle = new List<Vector3>();
            try
            {
                foreach (float phase in new[] { 0f, .25f, .5f, .75f })
                {
                    for (int i = 0; i < transforms.Length; i++)
                    { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                    idleClip.SampleAnimation(animator.gameObject, idleClip.length * phase);
                    idle.AddRange(CareBlockerSkin(skin, mesh, breed.ContactVertexIndices));
                }
            }
            finally
            {
                for (int i = 0; i < transforms.Length; i++)
                { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
            }

            foreach (float radius in new[] { .32f, .34f, .36f, .38f, .40f, .42f, .44f, .46f })
            foreach (int angle in new[] { 0, 10, -10, 20, -20, 30, -30 })
            {
                Vector3 side = Quaternion.Euler(0, angle, 0) * outward;
                Place(target.position + side * radius, Quaternion.LookRotation(-side));
                Physics.SyncTransforms();
                Vector3 position = cat.transform.position; Quaternion rotation = cat.transform.rotation;
                bool bodyClear = cat.IsBodyPoseClear(position, rotation);
                bool controllerClear = guard.IsControllerClear(position, rotation);
                bool careGate = CatMealHeadMotion.TryPrepareCareStart(cat, target, out var careStart);
                var feeding = CatFeedingAlignmentCatalog.Load().Find(breedId);
                Vector3 predictedMouth = position + rotation * feeding.mouthOffset * (cat.transform.lossyScale.y / .5f);
                float mouthDistance = Vector3.Distance(predictedMouth, target.position);
                Vector3[] currentWorld = current.Select(p => cat.transform.TransformPoint(p)).ToArray();
                Vector3[] idleWorld = idle.Select(p => cat.transform.TransformPoint(p)).ToArray();
                Bounds currentBounds = CareBlockerBounds(currentWorld), idleBounds = CareBlockerBounds(idleWorld);
                Bounds allBounds = currentBounds; allBounds.Encapsulate(idleBounds);
                var obstacles = new HashSet<Collider>();
                foreach (var collider in Physics.OverlapBox(allBounds.center, allBounds.extents + Vector3.one * .002f,
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    if ((bool)solidMethod.Invoke(guard, new object[] { collider })) obstacles.Add(collider);
                Vector3 ccA = position + rotation * ccStartLocal, ccB = position + rotation * ccEndLocal;
                Vector3 ccCentre = position + rotation * ccCentreLocal;
                var ccCandidates = Physics.OverlapCapsule(ccA, ccB, ccRadius, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => (bool)solidMethod.Invoke(guard, new object[] { c })).ToArray();
                foreach (var collider in ccCandidates) obstacles.Add(collider);
                var candidates = new Collider[runtimeProbes.Length][];
                for (int i = 0; i < runtimeProbes.Length; i++)
                {
                    var p = runtimeProbes[i];
                    candidates[i] = Physics.OverlapCapsule(position + rotation * p.start, position + rotation * p.end,
                        p.radius, ~0, QueryTriggerInteraction.Ignore).Where(c => (bool)solidMethod.Invoke(guard, new object[] { c })).ToArray();
                    foreach (var collider in candidates[i]) obstacles.Add(collider);
                }
                var currentDepth = new Dictionary<Collider, CareBlockerSkinResult>();
                var idleDepth = new Dictionary<Collider, CareBlockerSkinResult>();
                foreach (var collider in obstacles)
                {
                    currentDepth[collider] = CareBlockerDepth(guard, penetration, collider, currentWorld);
                    idleDepth[collider] = CareBlockerDepth(guard, penetration, collider, idleWorld);
                }
                var blockingController = new HashSet<string>();
                float maxControllerDepth = 0f, maxBlockingControllerDepth = 0f, maxControllerSkin = 0f;
                foreach (var collider in ccCandidates.Length == 0 ? new Collider[] { null } : ccCandidates)
                {
                    Vector3 normal = Vector3.zero; float depth = 0f;
                    bool penetrates = collider != null && CareBlockerPenetrates(guard, penetration, ccA, ccB, ccRadius,
                        collider, out normal, out depth);
                    bool blocks = penetrates && normal.y < .8f && depth > .002f;
                    string path = collider != null ? CareBlockerPath(collider.transform) : "<clear>";
                    var now = collider != null ? currentDepth[collider] : default;
                    var sourceIdle = collider != null ? idleDepth[collider] : default;
                    maxControllerDepth = Mathf.Max(maxControllerDepth, depth);
                    if (blocks)
                    {
                        blockingController.Add(path); maxBlockingControllerDepth = Mathf.Max(maxBlockingControllerDepth, depth);
                        maxControllerSkin = Mathf.Max(maxControllerSkin, now.depth, sourceIdle.depth);
                    }
                    detail.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate, "controller",
                        path, collider != null ? collider.GetType().Name : "", collider is MeshCollider mc ? UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh) : "",
                        blocks, depth, normal, ccA, ccB, ccRadius, now.depth, now.inside, sourceIdle.depth, sourceIdle.inside, now.point, sourceIdle.point,
                        0, 0, ccStartLocal, ccEndLocal, ccRadius, state.fullPathHash, state.normalizedTime,
                        idleClip.name, "0;.25;.5;.75", currentBounds, idleBounds, careStart.PromptDistance, mouthDistance,
                        collider != null ? collider.bounds : default(Bounds), ccHeight, ccRadius, ccCentre));
                }
                Assert.That(blockingController.Count == 0, Is.EqualTo(controllerClear),
                    "Explicit controller contacts must reproduce the production gate exactly.");
                var blockingRegions = new HashSet<string>(); var blockingColliders = new HashSet<string>();
                float maxProbe = 0f;
                for (int i = 0; i < runtimeProbes.Length; i++)
                {
                    var probe = runtimeProbes[i];
                    Vector3 a = position + rotation * probe.start, b = position + rotation * probe.end;
                    // A clear region still has one row; overlapping but allowed
                    // contacts retain depth/normal instead of disappearing.
                    foreach (var collider in candidates[i].Length == 0 ? new Collider[] { null } : candidates[i])
                    {
                        Vector3 normal = Vector3.zero; float depth = 0f;
                        bool penetrates = collider != null && CareBlockerPenetrates(guard, penetration, a, b, probe.radius, collider, out normal, out depth);
                        bool blocks = penetrates && depth > .015f;
                        string path = collider != null ? CareBlockerPath(collider.transform) : "<clear>";
                        if (blocks) { blockingRegions.Add(probe.region); blockingColliders.Add(path); }
                        maxProbe = Mathf.Max(maxProbe, depth);
                        var now = collider != null ? currentDepth[collider] : default;
                        var sourceIdle = collider != null ? idleDepth[collider] : default;
                        var source = profile.probes.First(p => p.region == probe.region);
                        detail.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate, probe.region,
                            path, collider != null ? collider.GetType().Name : "", collider is MeshCollider mc ? UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh) : "",
                            blocks, depth, normal, a, b, probe.radius, now.depth, now.inside, sourceIdle.depth, sourceIdle.inside, now.point, sourceIdle.point,
                            profile.sampledPoses, profile.sampledVertices, source.start, source.end, source.radius, state.fullPathHash, state.normalizedTime,
                            idleClip.name, "0;.25;.5;.75", currentBounds, idleBounds, careStart.PromptDistance, mouthDistance,
                            collider != null ? collider.bounds : default(Bounds), ccHeight, ccRadius, ccCentre));
                    }
                }
                summary.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate,
                    string.Join(";", blockingRegions), string.Join(";", blockingColliders), maxProbe,
                    currentDepth.Count == 0 ? 0f : currentDepth.Values.Max(v => v.depth),
                    idleDepth.Count == 0 ? 0f : idleDepth.Values.Max(v => v.depth), careStart.PromptDistance, mouthDistance,
                    string.Join(";", blockingController), maxControllerDepth, maxBlockingControllerDepth, maxControllerSkin));
                // One stance per native frame keeps the editor responsive.
                yield return null;
            }
        }
        Assert.That(summary.Count - 1, Is.EqualTo(112));
    }
    finally
    {
        Object.DestroyImmediate(mesh);
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "care-front-arc-details.csv"), detail);
        File.WriteAllLines(Path.Combine(Output, "care-front-arc-summary.csv"), summary);
        File.WriteAllLines(Path.Combine(Output, "care-cc-body-measure.csv"), bodyMeasure);
    }
}


[UnityTest, Timeout(300000)]
public IEnumerator NativeCareBodyBlockerDiagnostic()
{
    yield return Home();
    var detail = new List<string> {
        "breed,radius,angle,bodyClear,controllerClear,careGate,region,collider,colliderType,meshAsset,probeBlocks,probeDepth,probeNormal,probeStart,probeEnd,probeRadius,currentSkinDepth,currentInsideVertices,idleSkinDepth,idleInsideVertices,currentDeepest,idleDeepest,profilePoses,profileVertices,sourceStart,sourceEnd,sourceRadius,currentState,currentPhase,idleClip,idlePhases,currentSkinBounds,idleSkinBounds,careDistance,mouthDistance"
    };
    var summary = new List<string> {
        "breed,radius,angle,bodyClear,controllerClear,careGate,blockingRegions,blockingColliders,maxProbeDepth,maxCurrentSkinDepth,maxIdleSkinDepth,careDistance,mouthDistance"
    };
    var mesh = new Mesh { name = "QA care blocker skin" };
    try
    {
        foreach (string breedId in new[] { "persian", "oriental-shorthair" })
        {
            yield return Breed(breedId);
            var setup = Field<BowlInteraction.BowlSetup>(bowls, "food");
            Assert.That(setup?.ContactPoint, Is.Not.Null);
            Transform target = setup.ContactPoint;
            Vector3 outward = setup.InteractionPoint.position - target.position; outward.y = 0;
            Assert.That(outward.sqrMagnitude, Is.GreaterThan(.0001f)); outward.Normalize();
            var guard = Field<CatBodyGuard>(cat, "bodyGuard");
            var catalog = Resources.Load<CatBodyGuardCatalog>(CatBodyGuardCatalog.ResourceName);
            var profile = catalog.Find(breedId);
            Assert.That(guard != null && profile != null && guard.ProbeCount == 4, Is.True);
            var runtimeProbes = Field<CatBodyGuardCatalog.Probe[]>(guard, "probes");
            var solidMethod = typeof(CatBodyGuard).GetMethod("Solid", Private);
            var penetration = typeof(CatBodyGuard).GetMethod("Penetration", Private);
            Assert.That(solidMethod != null && penetration != null, Is.True);

            // Capture local skin once per breed. The Idle clip samples only this
            // copied QA actor, and every local transform is restored immediately.
            var animator = cat.GetComponentInChildren<Animator>();
            var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
            var breed = CatBreedCatalog.Load().Find(breedId);
            var state = animator.GetCurrentAnimatorStateInfo(0);
            var current = CareBlockerSkin(skin, mesh, breed.ContactVertexIndices);
            var idleClip = animator.runtimeAnimatorController.animationClips.First(c => c.name.EndsWith("|Idle", StringComparison.Ordinal));
            var transforms = animator.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(t => t.localPosition).ToArray();
            var rotations = transforms.Select(t => t.localRotation).ToArray();
            var scales = transforms.Select(t => t.localScale).ToArray();
            var idle = new List<Vector3>();
            try
            {
                foreach (float phase in new[] { 0f, .25f, .5f, .75f })
                {
                    for (int i = 0; i < transforms.Length; i++)
                    { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                    idleClip.SampleAnimation(animator.gameObject, idleClip.length * phase);
                    idle.AddRange(CareBlockerSkin(skin, mesh, breed.ContactVertexIndices));
                }
            }
            finally
            {
                for (int i = 0; i < transforms.Length; i++)
                { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
            }

            foreach (float radius in new[] { .30f, .34f, .38f })
            for (int angle = 0; angle < 360; angle += 10)
            {
                Vector3 side = Quaternion.Euler(0, angle, 0) * outward;
                Place(target.position + side * radius, Quaternion.LookRotation(-side));
                Physics.SyncTransforms();
                Vector3 position = cat.transform.position; Quaternion rotation = cat.transform.rotation;
                bool bodyClear = cat.IsBodyPoseClear(position, rotation);
                bool controllerClear = guard.IsControllerClear(position, rotation);
                bool careGate = CatMealHeadMotion.TryPrepareCareStart(cat, target, out var careStart);
                var feeding = CatFeedingAlignmentCatalog.Load().Find(breedId);
                Vector3 predictedMouth = position + rotation * feeding.mouthOffset * (cat.transform.lossyScale.y / .5f);
                float mouthDistance = Vector3.Distance(predictedMouth, target.position);
                Vector3[] currentWorld = current.Select(p => cat.transform.TransformPoint(p)).ToArray();
                Vector3[] idleWorld = idle.Select(p => cat.transform.TransformPoint(p)).ToArray();
                Bounds currentBounds = CareBlockerBounds(currentWorld), idleBounds = CareBlockerBounds(idleWorld);
                Bounds allBounds = currentBounds; allBounds.Encapsulate(idleBounds);
                var obstacles = new HashSet<Collider>();
                foreach (var collider in Physics.OverlapBox(allBounds.center, allBounds.extents + Vector3.one * .002f,
                    Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                    if ((bool)solidMethod.Invoke(guard, new object[] { collider })) obstacles.Add(collider);
                var candidates = new Collider[runtimeProbes.Length][];
                for (int i = 0; i < runtimeProbes.Length; i++)
                {
                    var p = runtimeProbes[i];
                    candidates[i] = Physics.OverlapCapsule(position + rotation * p.start, position + rotation * p.end,
                        p.radius, ~0, QueryTriggerInteraction.Ignore).Where(c => (bool)solidMethod.Invoke(guard, new object[] { c })).ToArray();
                    foreach (var collider in candidates[i]) obstacles.Add(collider);
                }
                var currentDepth = new Dictionary<Collider, CareBlockerSkinResult>();
                var idleDepth = new Dictionary<Collider, CareBlockerSkinResult>();
                foreach (var collider in obstacles)
                {
                    currentDepth[collider] = CareBlockerDepth(guard, penetration, collider, currentWorld);
                    idleDepth[collider] = CareBlockerDepth(guard, penetration, collider, idleWorld);
                }
                var blockingRegions = new HashSet<string>(); var blockingColliders = new HashSet<string>();
                float maxProbe = 0f;
                for (int i = 0; i < runtimeProbes.Length; i++)
                {
                    var probe = runtimeProbes[i];
                    Vector3 a = position + rotation * probe.start, b = position + rotation * probe.end;
                    // A clear region still has one row; overlapping but allowed
                    // contacts retain depth/normal instead of disappearing.
                    foreach (var collider in candidates[i].Length == 0 ? new Collider[] { null } : candidates[i])
                    {
                        Vector3 normal = Vector3.zero; float depth = 0f;
                        bool penetrates = collider != null && CareBlockerPenetrates(guard, penetration, a, b, probe.radius, collider, out normal, out depth);
                        bool blocks = penetrates && depth > .015f;
                        string path = collider != null ? CareBlockerPath(collider.transform) : "<clear>";
                        if (blocks) { blockingRegions.Add(probe.region); blockingColliders.Add(path); }
                        maxProbe = Mathf.Max(maxProbe, depth);
                        var now = collider != null ? currentDepth[collider] : default;
                        var sourceIdle = collider != null ? idleDepth[collider] : default;
                        var source = profile.probes.First(p => p.region == probe.region);
                        detail.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate, probe.region,
                            path, collider != null ? collider.GetType().Name : "", collider is MeshCollider mc ? UnityEditor.AssetDatabase.GetAssetPath(mc.sharedMesh) : "",
                            blocks, depth, normal, a, b, probe.radius, now.depth, now.inside, sourceIdle.depth, sourceIdle.inside, now.point, sourceIdle.point,
                            profile.sampledPoses, profile.sampledVertices, source.start, source.end, source.radius, state.fullPathHash, state.normalizedTime,
                            idleClip.name, "0;.25;.5;.75", currentBounds, idleBounds, careStart.PromptDistance, mouthDistance));
                    }
                }
                summary.Add(CareBlockerCsv(breedId, radius, angle, bodyClear, controllerClear, careGate,
                    string.Join(";", blockingRegions), string.Join(";", blockingColliders), maxProbe,
                    currentDepth.Count == 0 ? 0f : currentDepth.Values.Max(v => v.depth),
                    idleDepth.Count == 0 ? 0f : idleDepth.Values.Max(v => v.depth), careStart.PromptDistance, mouthDistance));
                // One stance per native frame keeps the editor responsive.
                yield return null;
            }
        }
        Assert.That(summary.Count - 1, Is.EqualTo(216));
    }
    finally
    {
        Object.DestroyImmediate(mesh);
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, "care-body-blockers.csv"), detail);
        File.WriteAllLines(Path.Combine(Output, "care-body-blockers-summary.csv"), summary);
    }
}

Vector3[] CareBlockerSkin(SkinnedMeshRenderer skin, Mesh mesh, IReadOnlyList<int> indices)
{
    skin.BakeMesh(mesh, true); var vertices = mesh.vertices;
    return indices.Select(i => cat.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]))).ToArray();
}

static Bounds CareBlockerBounds(Vector3[] points)
{
    var result = new Bounds(points[0], Vector3.zero);
    foreach (var point in points) result.Encapsulate(point);
    return result;
}

struct CareBlockerSkinResult { public float depth; public int inside; public Vector3 point; }

static bool CareBlockerPenetrates(CatBodyGuard guard, MethodInfo method, Vector3 a, Vector3 b, float radius,
    Collider collider, out Vector3 normal, out float depth)
{
    object[] arguments = { a, b, radius, collider, Vector3.zero, 0f };
    bool result = (bool)method.Invoke(guard, arguments);
    normal = (Vector3)arguments[4]; depth = (float)arguments[5];
    return result;
}

static CareBlockerSkinResult CareBlockerDepth(CatBodyGuard guard, MethodInfo penetration, Collider collider, Vector3[] points)
{
    var result = new CareBlockerSkinResult();
    var bounds = collider.bounds;
    foreach (Vector3 point in points)
    {
        if (!bounds.Contains(point)) continue;
        float depth = 0f;
        if (collider is BoxCollider box)
        {
            Vector3 p = box.transform.InverseTransformPoint(point) - box.center;
            Vector3 inward = box.size * .5f - new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
            if (inward.x <= 0 || inward.y <= 0 || inward.z <= 0) continue;
            Vector3 s = box.transform.lossyScale;
            depth = Mathf.Min(inward.x * Mathf.Abs(s.x), Mathf.Min(inward.y * Mathf.Abs(s.y), inward.z * Mathf.Abs(s.z)));
        }
        else if (collider is MeshCollider mesh && !mesh.convex)
        {
            if (!CareBlockerInsideMesh(mesh, point, out depth)) continue;
        }
        else if (CareBlockerPenetrates(guard, penetration, point, point, .0005f, collider, out _, out float withRadius))
            depth = Mathf.Max(0f, withRadius - .0005f);
        if (depth <= 0f) continue;
        result.inside++;
        if (depth > result.depth) { result.depth = depth; result.point = point; }
    }
    return result;
}

static readonly Vector3[] CareBlockerDirections = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
static readonly QaMeshTopologyCache CareBlockerTopology = new QaMeshTopologyCache();

static bool CareBlockerInsideMesh(MeshCollider mesh, Vector3 point, out float depth)
{
    depth = float.PositiveInfinity;
    var data = CareBlockerTopology.Get(mesh.sharedMesh);
    bool previous = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
    int inside = 0;
    try
    {
        foreach (Vector3 direction in CareBlockerDirections)
            if (mesh.Raycast(new Ray(point, direction), out var hit, 5f))
            {
                int index = hit.triangleIndex * 3;
                if (index < 0 || index + 2 >= data.triangles.Length) continue;
                Vector3 a = mesh.transform.TransformPoint(data.vertices[data.triangles[index]]);
                Vector3 b = mesh.transform.TransformPoint(data.vertices[data.triangles[index + 1]]);
                Vector3 c = mesh.transform.TransformPoint(data.vertices[data.triangles[index + 2]]);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), direction) > 0f)
                { inside++; depth = Mathf.Min(depth, hit.distance); }
            }
        return inside >= 4;
    }
    finally { Physics.queriesHitBackfaces = previous; }
}

static string CareBlockerPath(Transform value)
{
    string path = value.name;
    while (value.parent != null) { value = value.parent; path = value.name + "/" + path; }
    return value.gameObject.scene.name + "/" + path;
}

static string CareBlockerCsv(params object[] values) => string.Join(",", values.Select(value =>
{
    string text = value is Vector3 v ? FormattableString.Invariant($"{v.x:F6};{v.y:F6};{v.z:F6}") :
        value is Bounds b ? FormattableString.Invariant($"{b.min.x:F6};{b.min.y:F6};{b.min.z:F6}|{b.max.x:F6};{b.max.y:F6};{b.max.z:F6}") :
        value is IFormattable f ? f.ToString(null, System.Globalization.CultureInfo.InvariantCulture) : value?.ToString() ?? "";
    return "\"" + text.Replace("\"", "\"\"") + "\"";
}));

// Insert inside CareAlignmentPolishTests. It shares the existing copied-save
// lifecycle and the original-mesh topology reader; no production geometry edits.
[UnityTest]
public IEnumerator NativeCareTrayColliderMatchesVisibleAssembly()
{
    yield return Home();
    var station = Object.FindAnyObjectByType<CatCareStationObstacle>();
    Assert.That(station != null && station.Body != null && station.FrontExit != null, Is.True);
    Assert.That(station.Body.transform, Is.SameAs(station.transform));
    Assert.That(station.Body.enabled && !station.Body.isTrigger, Is.True);
    var tray = station.transform.Find("StationVisual");
    Assert.That(tray, Is.Not.Null);
    var filters = tray.GetComponentsInChildren<MeshFilter>(true);
    Assert.That(filters, Is.Not.Empty);
    var vertices = new List<Vector3>();
    var solids = new List<MeshCollider>();
    foreach (var filter in filters)
    {
        Assert.That(UnityEditor.AssetDatabase.GetAssetPath(filter.sharedMesh),
            Is.EqualTo("Assets/Art/PremiumFurniture/Models/CareStationTray_Premium.fbx"));
        var colliders = filter.GetComponents<MeshCollider>();
        Assert.That(colliders.Length, Is.EqualTo(1));
        var collider = colliders[0];
        Assert.That(collider.enabled && !collider.isTrigger && !collider.convex, Is.True);
        Assert.That(collider.sharedMesh, Is.SameAs(filter.sharedMesh));
        solids.Add(collider);
        vertices.AddRange(CareBlockerTopology.Get(filter.sharedMesh).vertices.Select(v =>
            station.transform.InverseTransformPoint(filter.transform.TransformPoint(v))));
    }
    Assert.That(tray.GetComponentsInChildren<BoxCollider>(true).Any(c => c.enabled), Is.False,
        "The old broad rear box must not double the rendered lip.");
    var bounds = CareBlockerBounds(vertices.ToArray());
    float floorTop = vertices.Where(v => v.z <= bounds.center.z).Max(v => v.y);
    Assert.That(floorTop, Is.EqualTo(.028f).Within(.001f));
    Assert.That(station.Body.center.y + station.Body.size.y * .5f, Is.EqualTo(floorTop).Within(.0001f));
    Assert.That(station.Body.center.y - station.Body.size.y * .5f, Is.EqualTo(bounds.min.y).Within(.0001f));
    Assert.That(station.Body.size.x, Is.EqualTo(bounds.size.x).Within(.0001f));
    Assert.That(station.Body.size.z, Is.EqualTo(bounds.size.z).Within(.0001f));

    Vector3 air = station.transform.TransformPoint(new Vector3(0, .12f, -.12f));
    Assert.That(solids.Any(c => CareBlockerInsideMesh(c, air, out _)), Is.False,
        "The central space above the actual tray is empty air.");
    Assert.That(station.Body.bounds.Contains(air), Is.False);
    Vector3 basePoint = station.transform.TransformPoint(new Vector3(0, .012f, -.12f));
    Assert.That(solids.Any(c => CareBlockerInsideMesh(c, basePoint, out _)), Is.True,
        "The retained original rendered base is a closed physical surface.");
    Vector3 back = station.transform.TransformPoint(new Vector3(0, .085f, .255f));
    Assert.That(solids.Any(c => CareBlockerInsideMesh(c, back, out _)), Is.True,
        "The actual backplate still blocks contact.");
}

}
#endif
