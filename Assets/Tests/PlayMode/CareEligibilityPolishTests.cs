#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Uses the established real-home/copied-save fixture so both suites exercise
// the same authored HUD buttons, scene loading and save separation.
public sealed class CareEligibilityPolishTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    GameLanguage language;
    bool hadLanguage;
    int languagePreference;
    [SetUp] public void Before()
    {
        home = new CareAlignmentPolishTests(); home.Before();
        language = GameLanguageService.Current;
        hadLanguage = PlayerPrefs.HasKey(GameLanguageService.PlayerPrefsKey);
        languagePreference = PlayerPrefs.GetInt(GameLanguageService.PlayerPrefsKey);
    }
    [TearDown] public void After()
    {
        foreach (var probe in Object.FindObjectsByType<CareRefusalProbe>(FindObjectsInactive.Include)) Object.DestroyImmediate(probe.gameObject);
        home.After(); GameLanguageService.SetLanguage(language);
        if (hadLanguage) PlayerPrefs.SetInt(GameLanguageService.PlayerPrefsKey, languagePreference);
        else PlayerPrefs.DeleteKey(GameLanguageService.PlayerPrefsKey);
        PlayerPrefs.Save();
    }
    static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    IEnumerator Boot() => (IEnumerator)Call(home, "Home");
    IEnumerator Room(string room) => (IEnumerator)Call(home, "Room", room);
    CatMovement Cat => Read<CatMovement>(home, "cat");
    HungerSystem Hunger => Read<HungerSystem>(home, "hunger");
    ThirstSystem Thirst => Read<ThirstSystem>(home, "thirst");
    EnergySystem Energy => Read<EnergySystem>(home, "energy");
    [Serializable] sealed class Quests { public QuestProgressEntry[] values; }
    static string QuestSnapshot() => JsonUtility.ToJson(new Quests { values = ProgressionService.CaptureQuestProgress().OrderBy(q => q.questId).ToArray() });
    void FullNeeds(float value = 100) { Hunger.ApplySavedValue(value); Thirst.ApplySavedValue(value); Energy.ApplySavedValue(0); }
    void AssertBubble(string key)
    {
        var bubble = Cat.GetComponent<CatSpeechBubble>();
        Assert.That(bubble, Is.Not.Null, "A room without a preattached bubble must create one.");
        Assert.That(Read<TMP_Text>(bubble, "label").text, Is.EqualTo(GameLanguageService.Text(key)));
        Assert.That(Read<CanvasGroup>(bubble, "group").alpha, Is.GreaterThan(0), "The feedback is visibly displayed.");
    }

    [UnityTest] public IEnumerator FullCare_EveryRoomAndLanguage_RefusesBeforeGeometryEnergyAndLock()
    {
        yield return Boot();
        int rooms = 0, checks = 0;
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return Room(room.Id); rooms++;
            var previousBubble = Cat.GetComponent<CatSpeechBubble>();
            if (previousBubble != null) Object.Destroy(previousBubble);
            yield return null;
            var host = new GameObject("QA future care adapter");
            SceneManager.MoveGameObjectToScene(host, Cat.gameObject.scene);
            var probe = host.AddComponent<CareRefusalProbe>();
            probe.EditorConfigure("qa.care", "QA", CatActivityKind.MealTime, QuestType.Eat, 0, "EAT", .4f, 20, host.transform, null, null);
            Time.timeScale = 0;
            foreach (var selected in new[] { GameLanguage.Turkish, GameLanguage.English })
            foreach (var need in new[] { CatCareNeed.Food, CatCareNeed.Water })
            foreach (float value in new[] { 90f, 100f })
            {
                GameLanguageService.SetLanguage(selected); FullNeeds(value); probe.Need = need;
                probe.Prepares = probe.Begins = probe.CanBegins = 0;
                var root = Cat.transform.position; var rotation = Cat.transform.rotation;
                var controller = Cat.GetComponent<CharacterController>(); bool enabled = controller.enabled;
                string quests = QuestSnapshot();
                Assert.That(CatCareEligibility.SatisfiedThreshold, Is.EqualTo(90));
                Assert.That(probe.TryStart(Cat), Is.False);
                Assert.That(probe.Prepares + probe.CanBegins + probe.Begins, Is.Zero, "Refusal precedes geometry and routine decisions.");
                Assert.That(CatActivity.Active, Is.Null); Assert.That(Cat.IsMovementPhysicallyLocked, Is.False);
                Assert.That(controller.enabled, Is.EqualTo(enabled));
                Assert.That(Cat.transform.position, Is.EqualTo(root)); Assert.That(Cat.transform.rotation, Is.EqualTo(rotation));
                Assert.That(Hunger.CurrentHunger, Is.EqualTo(value)); Assert.That(Thirst.CurrentThirst, Is.EqualTo(value));
                Assert.That(Energy.CurrentEnergy, Is.Zero); Assert.That(QuestSnapshot(), Is.EqualTo(quests));
                yield return null; yield return null;
                AssertBubble(need == CatCareNeed.Food ? "care.not_hungry" : "care.not_thirsty"); checks++;
            }
            // Just below 90 reaches the next policy stage despite zero energy;
            // this fake geometry intentionally refuses, keeping the test still.
            FullNeeds(89); probe.Prepares = 0;
            Assert.That(probe.TryStart(Cat), Is.False); Assert.That(probe.Prepares, Is.EqualTo(1));
            Object.Destroy(host); Time.timeScale = 1; yield return null;
        }
        Assert.That(rooms, Is.EqualTo(8)); Assert.That(checks, Is.EqualTo(64));
    }

    [UnityTest] public IEnumerator FullCare_ActualFoodWaterAndKitchenButtons_ShowRefusalAndNeverStart()
    {
        yield return Boot();
        yield return (IEnumerator)Call(home, "Breed", "oriental-shorthair");
        foreach (var selected in new[] { GameLanguage.Turkish, GameLanguage.English })
        {
            GameLanguageService.SetLanguage(selected);
            yield return Room(HomeRoomService.LivingRoomId);
            var bowls = Read<BowlInteraction>(home, "bowls");
            foreach (string kind in new[] { "food", "water" })
            {
                var setup = Read<BowlInteraction.BowlSetup>(bowls, kind); setup.Fill();
                Func<bool> offered = () => { Call(bowls, "Update"); return ReferenceEquals(Read<BowlInteraction.BowlSetup>(bowls, "currentBowl"), setup); };
                Call(home, "FindCareStance", setup.ContactPoint, setup.InteractionPoint, offered);
                var button = (Button)Call(home, "BowlButton", setup);
                FullNeeds(); Time.timeScale = 0;
                var root = Cat.transform.position; var yaw = Cat.transform.rotation; string quests = QuestSnapshot();
                int completions = 0; Action callback = () => completions++;
                Hunger.Ate += callback; Thirst.Drank += callback;
                try
                {
                    button.onClick.Invoke(); yield return null; yield return null;
                    Assert.That(bowls.IsInteracting || Hunger.IsEating || Thirst.IsDrinking, Is.False);
                    Assert.That(completions, Is.Zero); Assert.That(QuestSnapshot(), Is.EqualTo(quests));
                    Assert.That(Cat.transform.position, Is.EqualTo(root)); Assert.That(Cat.transform.rotation, Is.EqualTo(yaw));
                    Assert.That(Cat.IsMovementPhysicallyLocked, Is.False); Assert.That(Energy.CurrentEnergy, Is.Zero);
                    AssertBubble(kind == "food" ? "care.not_hungry" : "care.not_thirsty");
                }
                finally { Hunger.Ate -= callback; Thirst.Drank -= callback; Time.timeScale = 1; }
            }
            yield return Room(HomeRoomService.KitchenId);
            var meal = CatActivity.Registered.OfType<MealTimeActivity>().Single(a => a.gameObject.scene == Cat.gameObject.scene);
            Call(home, "FindCareStance", meal.BowlPoint, Read<Transform>(meal, "standPoint"),
                new Func<bool>(() => meal.TryGetPromptDistance(Cat, out _)));
            var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
            Call(Read<BowlInteraction>(home, "bowls"), "Update");
            FullNeeds(); Time.timeScale = 0; Call(prompt, "RefreshImmediate");
            Assert.That(Read<CatActivity>(prompt, "candidate"), Is.SameAs(meal));
            var action = Read<Button>(prompt, "actionButton");
            Assert.That(action.isActiveAndEnabled && action.interactable, Is.True);
            Assert.That(Read<TMP_Text>(prompt, "actionLabel").text, Does.Not.Contain(selected == GameLanguage.Turkish ? "enerji gerekli" : "Need"));
            var start = Cat.transform.position; var rotation = Cat.transform.rotation; string before = QuestSnapshot();
            action.onClick.Invoke(); yield return null; yield return null;
            Assert.That(meal.IsRunning || meal.IsEating || meal.InspectingOnly, Is.False);
            Assert.That(Cat.transform.position, Is.EqualTo(start)); Assert.That(Cat.transform.rotation, Is.EqualTo(rotation));
            Assert.That(QuestSnapshot(), Is.EqualTo(before)); AssertBubble("care.not_hungry"); Time.timeScale = 1;
        }
    }

    [UnityTest] public IEnumerator ExistingRoomCareAndPublicCareApis_AllUseOneThresholdAndOneFeedback()
    {
        yield return Boot();
        int authoredFood = 0, authoredWater = 0;
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return Room(room.Id); Time.timeScale = 0;
            foreach (var activity in CatActivity.Registered.Where(a => a.gameObject.scene == Cat.gameObject.scene &&
                a.isActiveAndEnabled && a.IsUnlocked && a.IsContentVisible && a.RequiredCareNeed != CatCareNeed.None).ToArray())
            {
                FullNeeds(90); var position = Cat.transform.position; string quests = QuestSnapshot();
                Assert.That(activity.TryStart(Cat), Is.False, activity.ActivityId);
                Assert.That(activity.IsRunning, Is.False); Assert.That(Cat.transform.position, Is.EqualTo(position));
                Assert.That(QuestSnapshot(), Is.EqualTo(quests));
                yield return null; yield return null;
                AssertBubble(activity.RequiredCareNeed == CatCareNeed.Food ? "care.not_hungry" : "care.not_thirsty");
                if (activity.RequiredCareNeed == CatCareNeed.Food) authoredFood++; else authoredWater++;
            }
            FullNeeds(90); Assert.That(Hunger.BeginEating(1), Is.False); yield return null; yield return null; AssertBubble("care.not_hungry");
            Assert.That(Thirst.BeginDrinking(1), Is.False); yield return null; yield return null; AssertBubble("care.not_thirsty");
            Assert.That(Hunger.CanEat || Thirst.CanDrink, Is.False); Time.timeScale = 1;
        }
        Assert.That(authoredFood, Is.GreaterThanOrEqualTo(1));
        Assert.That(authoredWater, Is.GreaterThanOrEqualTo(4), "Bathroom sink, kitchen sink, fountain and bird bath.");
    }
}

public sealed class CareRefusalProbe : CatActivity
{
    public CatCareNeed Need;
    public int Prepares, CanBegins, Begins;
    public override CatCareNeed RequiredCareNeed => Need;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start) { Prepares++; start = default; return false; }
    protected override bool CanBeginActivity(out string reason) { CanBegins++; reason = string.Empty; return true; }
    protected override bool BeginActivity() { Begins++; return false; }
}
#endif
