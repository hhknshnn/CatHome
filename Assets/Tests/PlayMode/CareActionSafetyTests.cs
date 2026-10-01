using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class CareActionSafetyTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    HomeStoreSaveState savedStore;
    float savedSpeed;
    bool hadOnboarding;
    int savedOnboarding;
    GameObject needsHost, careButtonHost, activityButtonHost, promptHost;
    CatMovement cat;
    BowlInteraction bowls;
    SleepInteraction sleep;
    HungerSystem hunger;
    ThirstSystem thirst;
    EnergySystem energy;
    CatActivity armchair;
    ActivityPromptController prompt;
    Button careButton, activityButton;

    [SetUp] public void Before()
    {
        savedStore = HomeStoreService.CaptureState();
        savedSpeed = Time.timeScale;
        hadOnboarding = PlayerPrefs.HasKey(PetTutorialHint.OnboardingCompletedKey);
        savedOnboarding = PlayerPrefs.GetInt(PetTutorialHint.OnboardingCompletedKey);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, 1);
    }

    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        if (promptHost != null) Object.DestroyImmediate(promptHost);
        if (activityButtonHost != null) Object.DestroyImmediate(activityButtonHost);
        if (careButtonHost != null) Object.DestroyImmediate(careButtonHost);
        if (needsHost != null) Object.DestroyImmediate(needsHost);
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(savedStore);
        if (hadOnboarding) PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, savedOnboarding);
        else PlayerPrefs.DeleteKey(PetTutorialHint.OnboardingCompletedKey);
        Time.timeScale = savedSpeed;
    }

    static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    static object Call(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    BowlInteraction.BowlSetup Bowl(string kind) => (BowlInteraction.BowlSetup)Get(bowls, kind);

    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p =>
            HomeStoreService.IsLivingRoomCollectionProduct(p.Id)).Select(p => p.Id).ToArray();
        HomeStoreService.ApplySavedState(state);
        yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>();
        bowls = cat.GetComponent<BowlInteraction>();
        sleep = cat.GetComponent<SleepInteraction>();
        armchair = CatActivity.Registered.First(a => a.Kind == CatActivityKind.ArmchairNap);
        needsHost = new GameObject("Care safety needs");
        hunger = Object.FindAnyObjectByType<HungerSystem>() ?? needsHost.AddComponent<HungerSystem>();
        thirst = Object.FindAnyObjectByType<ThirstSystem>() ?? needsHost.AddComponent<ThirstSystem>();
        energy = RoomPlayModeSupport.ProvisionNeeds();
        hunger.ApplySavedValue(35f); thirst.ApplySavedValue(35f);
        Set(bowls, "hungerSystem", hunger); Set(bowls, "thirstSystem", thirst);
        careButtonHost = new GameObject("ActionButton", typeof(RectTransform), typeof(Button));
        careButton = careButtonHost.GetComponent<Button>();
        Set(bowls, "interactionButton", careButton);
        bowls.ResolveSceneReferences();
        sleep.CancelForTransition();
        activityButtonHost = new GameObject("Activity safety button", typeof(RectTransform), typeof(Button));
        activityButton = activityButtonHost.GetComponent<Button>();
        promptHost = new GameObject("Activity safety prompt");
        promptHost.SetActive(false);
        prompt = promptHost.AddComponent<ActivityPromptController>();
        Set(prompt, "actionButton", activityButton);
        promptHost.SetActive(true);
        yield return null;
        Assert.That(CatActionState.IsBusy(cat), Is.False, "Fixture must begin with an available cat.");
    }

    void Move(Vector3 point)
    {
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        point.y = .05f;
        cat.transform.SetPositionAndRotation(point, Quaternion.identity);
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    bool Recovering(string kind) => kind == "water" ? thirst.IsDrinking : hunger.IsEating;
    float Need(string kind) => kind == "water" ? thirst.CurrentThirst : hunger.CurrentHunger;

    IEnumerator BeginCare(string kind)
    {
        Bowl(kind).Fill();
        Move(Bowl(kind).InteractionPoint.position);
        Call(bowls, "Update");
        Assert.That(Get(bowls, "currentBowl"), Is.SameAs(Bowl(kind)), kind + " real nearby bowl selection");
        Assert.That(bowls.HasVisibleAction, Is.True, kind + " prompt");
        careButton.onClick.Invoke();
        Assert.That(bowls.IsInteracting, Is.True, kind + " owns the approach immediately");
        float deadline = Time.realtimeSinceStartup + 6f;
        while (!Recovering(kind) && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Recovering(kind), Is.True, kind + " actual need recovery must have begun");
        yield return new WaitForSeconds(.25f);
        Assert.That(Need(kind), Is.GreaterThan(35f), kind + " actual recovery tick");
    }

    [UnityTest]
    public IEnumerator WaterAndFood_RejectFurniturePrompts_AndCancellationHandsOverToRealRest()
    {
        yield return Prepare();
        foreach (var kind in new[] { "water", "food" })
        {
            hunger.ApplySavedValue(35f); thirst.ApplySavedValue(35f); energy.ApplySavedValue(40f);
            Move(armchair.RoutineEntryPoint.position);
            Set(prompt, "selected", armchair);
            Call(prompt, "RefreshImmediate");
            Assert.That(Get(prompt, "candidate"), Is.SameAs(armchair), "An available nearby armchair must be offered before care.");
            Assert.That(activityButton.gameObject.activeSelf, Is.True);

            int careEvents = 0, bowlEvents = 0;
            System.Action onCare = () => careEvents++;
            UnityEngine.Events.UnityAction onBowl = () => bowlEvents++;
            if (kind == "water") thirst.Drank += onCare; else hunger.Ate += onCare;
            Bowl(kind).OnInteractionCompleted.AddListener(onBowl);
            yield return BeginCare(kind);
            Vector3 drinkingAt = cat.transform.position;
            Assert.That(CatActionState.IsBusy(cat), Is.True);
            Call(prompt, "RefreshImmediate");
            Assert.That(Get(prompt, "candidate"), Is.Null, kind + " must suppress unrelated action selection");
            Assert.That(activityButton.gameObject.activeSelf, Is.False, kind + " must hide the furniture button");
            Assert.That(armchair.TryStart(cat), Is.False, kind + " direct start must enforce the same admission gate");
            Assert.That(CatActivity.Active, Is.Null);
            Assert.That(cat.transform.position, Is.EqualTo(drinkingAt), kind + " rejected chair start must not teleport");
            Assert.That(cat.GetComponent<CatCommandActivity>().Issue(CatCompanionCommand.Loaf), Is.False, kind + " command overlap");

            bowls.CancelInteraction();
            float stoppedNeed = Need(kind);
            Assert.That(Recovering(kind), Is.False, kind + " cancelled needs coroutine");
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(bowls.ActiveCareSound, Is.Null.Or.Empty);
            Assert.That(CatActionState.IsBusy(cat), Is.False, "Care released its exact movement owner.");
            Move(armchair.RoutineEntryPoint.position);
            Assert.That(armchair.TryStart(cat), Is.True, "The armchair is usable after cancellation.");
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!armchair.IsWaitingForRestStop && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(armchair.IsWaitingForRestStop, Is.True, "The cat must reach a held resting pose, not stand on the cushion.");
            var animation = cat.GetComponent<CatActivityAnimation>();
            Assert.That(animation.CurrentPose, Is.EqualTo(CatActivityPose.Sleep));
            Assert.That(CatActivity.Active, Is.SameAs(armchair));
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.False);
            float restEnergy = energy.CurrentEnergy;

            // Closing idle care components must not reset the new resting pose.
            bowls.enabled = false; sleep.enabled = false;
            yield return new WaitForSeconds(6f);
            Assert.That(Need(kind), Is.LessThanOrEqualTo(stoppedNeed + .01f), kind + " old recovery must not follow the cat onto the chair");
            Assert.That(careEvents, Is.Zero, kind + " cancellation cannot issue completion reward");
            Assert.That(bowlEvents, Is.Zero, kind + " cancelled bowl must not complete");
            Assert.That(armchair.IsWaitingForRestStop, Is.True);
            Assert.That(animation.CurrentPose, Is.EqualTo(CatActivityPose.Sleep));
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.False, "Idle care cleanup cannot enable furniture physics.");
            Assert.That(energy.CurrentEnergy, Is.GreaterThan(restEnergy), "Only current resting activity replenishes its own need.");
            Assert.That(armchair.RequestRestStop(), Is.True);
            deadline = Time.realtimeSinceStartup + 8f;
            while (armchair.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(armchair.IsRunning, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(CatActionState.IsBusy(cat), Is.False);
            bowls.enabled = true; sleep.enabled = true;
            if (kind == "water") thirst.Drank -= onCare; else hunger.Ate -= onCare;
            Bowl(kind).OnInteractionCompleted.RemoveListener(onBowl);
        }
    }

    [UnityTest]
    public IEnumerator SleepingCat_RejectsFurnitureAndCommands_AndTransitionEndsRecovery()
    {
        yield return Prepare();
        Move(((Transform)Get(sleep, "bedInteractionPoint")).position);
        energy.ApplySavedValue(35f);
        Assert.That(sleep.WantsActionButton, Is.True);
        Assert.That(sleep.TryHandleActionButton(), Is.True);
        Assert.That(sleep.IsSleeping, Is.True);
        Assert.That(CatActionState.IsBusy(cat), Is.True);
        Assert.That(armchair.TryStart(cat), Is.False, "Sleep cannot be overlapped by furniture activity.");
        Assert.That(cat.GetComponent<CatCommandActivity>().Issue(CatCompanionCommand.Sit), Is.False);
        Call(prompt, "RefreshImmediate");
        Assert.That(activityButton.gameObject.activeSelf, Is.False);
        yield return new WaitForSeconds(1.5f);
        Assert.That(energy.CurrentEnergy, Is.GreaterThan(35f), "Actual bed recovery must run before cancellation.");
        CatActionState.CancelForTransition(cat);
        float stoppedEnergy = energy.CurrentEnergy;
        Assert.That(sleep.IsSleeping, Is.False);
        Assert.That(CatActionState.IsBusy(cat), Is.False);
        yield return new WaitForSeconds(2f);
        Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(stoppedEnergy + .01f));
        Assert.That(cat.transform.position.y, Is.LessThan(.15f), "Wake returns the cat to the open floor.");
    }

    [UnityTest]
    public IEnumerator NeedRecovery_StaleOwnerCancellationCannotStopNewOwner_AndCompletesExactlyOnce()
    {
        yield return Prepare();
        var ownerA = new GameObject("Earlier care owner");
        var ownerB = new GameObject("Current care owner");
        try
        {
            foreach (var kind in new[] { "water", "food" })
            {
                hunger.ApplySavedValue(35f); thirst.ApplySavedValue(35f);
                int events = 0;
                System.Action finished = () => events++;
                if (kind == "water") thirst.Drank += finished; else hunger.Ate += finished;
                Assert.That(kind == "water" ? thirst.BeginDrinking(1f, ownerA) : hunger.BeginEating(1f, ownerA), Is.True);
                yield return new WaitForSeconds(.1f);
                if (kind == "water") thirst.CancelDrinking(ownerA); else hunger.CancelEating(ownerA);
                Assert.That(events, Is.Zero, "Cancelled owner cannot complete.");
                Assert.That(kind == "water" ? thirst.BeginDrinking(.4f, ownerB) : hunger.BeginEating(.4f, ownerB), Is.True);
                if (kind == "water") { thirst.CancelDrinking(ownerA); thirst.CancelDrinking(null); }
                else { hunger.CancelEating(ownerA); hunger.CancelEating(null); }
                Assert.That(Recovering(kind), Is.True, "Old/null cleanup cannot cancel another owner.");
                yield return new WaitForSeconds(.65f);
                Assert.That(Recovering(kind), Is.False);
                Assert.That(events, Is.EqualTo(1), kind + " one completion");
                Assert.That(Need(kind), Is.GreaterThan(99f));
                if (kind == "water") thirst.CancelDrinking(ownerB); else hunger.CancelEating(ownerB);
                yield return null;
                Assert.That(events, Is.EqualTo(1), "Cleanup after completion cannot award twice.");
                if (kind == "water") thirst.Drank -= finished; else hunger.Ate -= finished;
            }
        }
        finally { Object.DestroyImmediate(ownerA); Object.DestroyImmediate(ownerB); }
    }

    [UnityTest]
    public IEnumerator ReplacedWaterRecovery_CannotCompleteTheAbandonedBowl_OrCancelItsNewOwner()
    {
        yield return Prepare();
        int bowlEvents = 0, drankEvents = 0;
        UnityEngine.Events.UnityAction onBowl = () => bowlEvents++;
        System.Action onDrink = () => drankEvents++;
        Bowl("water").OnInteractionCompleted.AddListener(onBowl);
        thirst.Drank += onDrink;
        var replacementOwner = new GameObject("Replacement water recovery owner");
        try
        {
            yield return BeginCare("water");
            // Both operations happen before the abandoned bowl coroutine gets
            // another frame. A global Drank event belongs to the new operation.
            thirst.ApplySavedValue(30f);
            Assert.That(thirst.BeginDrinking(.6f, replacementOwner), Is.True);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (bowls.IsInteracting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(bowls.IsInteracting, Is.False, "The bowl must release ownership when its own recovery was replaced.");
            Assert.That(thirst.IsDrinking, Is.True, "Old bowl cleanup must leave replacement recovery running.");
            Assert.That(bowlEvents, Is.Zero, "Replacement recovery cannot complete the old bowl.");
            Assert.That(drankEvents, Is.Zero, "No completed recovery yet.");
            yield return new WaitForSeconds(.8f);
            Assert.That(thirst.IsDrinking, Is.False);
            Assert.That(thirst.CurrentThirst, Is.GreaterThan(99f));
            Assert.That(drankEvents, Is.EqualTo(1), "The replacement owner completes normally once.");
            Assert.That(bowlEvents, Is.Zero, "Foreign completion must never award the abandoned bowl.");
        }
        finally
        {
            Bowl("water").OnInteractionCompleted.RemoveListener(onBowl);
            thirst.Drank -= onDrink;
            thirst.CancelDrinking(replacementOwner);
            Object.DestroyImmediate(replacementOwner);
        }
    }

    [UnityTest]
    public IEnumerator RealBowlCompletion_AwardsOneNeedEventAndOneBowlEvent_EvenAfterRepeatedClicks()
    {
        yield return Prepare();
        Time.timeScale = 3f;
        foreach (var kind in new[] { "water", "food" })
        {
            hunger.ApplySavedValue(35f); thirst.ApplySavedValue(35f);
            int needEvents = 0, bowlEvents = 0;
            System.Action onNeed = () => needEvents++;
            UnityEngine.Events.UnityAction onBowl = () => bowlEvents++;
            if (kind == "water") thirst.Drank += onNeed; else hunger.Ate += onNeed;
            Bowl(kind).OnInteractionCompleted.AddListener(onBowl);
            yield return BeginCare(kind);
            for (int click = 0; click < 4; click++) careButton.onClick.Invoke();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (bowls.IsInteracting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(Recovering(kind), Is.False);
            Assert.That(needEvents, Is.EqualTo(1), kind + " need completion");
            Assert.That(bowlEvents, Is.EqualTo(1), kind + " bowl completion");
            Assert.That(Need(kind), Is.GreaterThan(99f));
            Assert.That(CatActionState.IsBusy(cat), Is.False);
            bowls.CancelInteraction(); bowls.CancelInteraction();
            yield return new WaitForSeconds(.5f);
            Assert.That(needEvents, Is.EqualTo(1)); Assert.That(bowlEvents, Is.EqualTo(1));
            if (kind == "water") thirst.Drank -= onNeed; else hunger.Ate -= onNeed;
            Bowl(kind).OnInteractionCompleted.RemoveListener(onBowl);
        }
    }
}
