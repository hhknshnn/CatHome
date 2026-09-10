using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class CarePromptTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private HomeStoreSaveState saved;
    private GameObject needs, buttonHost;
    private BowlInteraction bowls;
    private SleepInteraction sleep;
    private HungerSystem hunger;
    private ThirstSystem thirst;
    private Button button;

    [SetUp] public void Before() => saved = HomeStoreService.CaptureState();

    [TearDown] public void After()
    {
        if (sleep != null) sleep.ForceAwakeForNewGame();
        if (bowls != null) bowls.enabled = false;
        if (buttonHost != null) Object.DestroyImmediate(buttonHost);
        if (needs != null) Object.DestroyImmediate(needs);
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(saved);
    }

    private IEnumerator Load(string room)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(room);
        bowls = Object.FindFirstObjectByType<BowlInteraction>();
        Assert.That(bowls, Is.Not.Null, room);
        sleep = bowls.GetComponent<SleepInteraction>();
        needs = new GameObject("CareTestNeeds");
        hunger = Object.FindFirstObjectByType<HungerSystem>() ?? needs.AddComponent<HungerSystem>();
        thirst = Object.FindFirstObjectByType<ThirstSystem>() ?? needs.AddComponent<ThirstSystem>();
        hunger.ApplySavedValue(35f);
        thirst.ApplySavedValue(35f);
        buttonHost = new GameObject("ActionButton", typeof(RectTransform), typeof(Button));
        button = buttonHost.GetComponent<Button>();
        Set(bowls, "hungerSystem", hunger);
        Set(bowls, "thirstSystem", thirst);
        Set(bowls, "interactionButton", button);
        bowls.ResolveSceneReferences();
        sleep.ForceAwakeForNewGame();
        yield return null;
    }

    private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static object Call(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    private BowlInteraction.BowlSetup Bowl(string name) => (BowlInteraction.BowlSetup)Get(bowls, name);
    private void Refresh() => Call(bowls, "Update");
    private void Move(Vector3 point)
    {
        var controller = bowls.GetComponent<CharacterController>();
        controller.enabled = false;
        point.y = .05f;
        bowls.transform.position = point;
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    [UnityTest] public IEnumerator EightRooms_OnlyRealCareFurnitureOffersFoodWaterOrSleep()
    {
        var rows = new List<string> { "room,care,visible,offered" };
        foreach (string room in new[] { "LivingRoom_Level01", "Bathroom_Level01", "Kitchen_Level01", "Bedroom_Level01",
            "Garden_Level01", "Balcony_Level01", "Patio_Level01", "SecondFloor_Level01" })
        {
            yield return Load(room);
            bool realCare = room == "LivingRoom_Level01";
            foreach (string kind in new[] { "food", "water" })
            {
                var setup = Bowl(kind);
                Assert.That(setup.IsFull, Is.True, "Empty needs must not mask the missing-visual regression.");
                foreach (Vector3 location in new[] { setup.Bowl.position, setup.InteractionPoint.position })
                {
                    Move(location);
                    Refresh();
                    if (!realCare)
                    {
                        Assert.That(Call(bowls, "FindClosestAvailableBowl"), Is.Null, room + " " + kind);
                        Assert.That(bowls.HasVisibleAction, Is.False, room + " ghost care action");
                        button.onClick.Invoke();
                        Assert.That(bowls.IsInteracting, Is.False);
                        Assert.That(hunger.IsEating || thirst.IsDrinking, Is.False);
                    }
                }
                bool visible = CareInteractionTarget.IsVisibleInRoom(setup.Bowl, bowls.transform);
                bool offered = ReferenceEquals(Call(bowls, "FindClosestAvailableBowl"), setup);
                Assert.That(visible, Is.EqualTo(realCare), room + " " + kind);
                Assert.That(offered, Is.EqualTo(realCare), room + " " + kind + " entrance");
                rows.Add(room + "," + kind + "," + visible + "," + offered);
            }
            Move(((Transform)Get(sleep, "bedInteractionPoint")).position);
            Refresh();
            Assert.That(sleep.WantsActionButton, Is.EqualTo(realCare), room + " sleep");
            rows.Add(room + ",sleep," + realCare + "," + sleep.WantsActionButton);
            if (!realCare)
            {
                Assert.That(sleep.TryHandleActionButton(), Is.False, "Empty rest anchor cannot start sleep.");
                Assert.That(sleep.TryRestoreSleepingState(out _), Is.False, "Loading cannot restore sleep onto an empty anchor.");
            }
        }
        System.IO.Directory.CreateDirectory("Docs/QA/CARE_PROMPTS_2026-09-07");
        System.IO.File.WriteAllLines("Docs/QA/CARE_PROMPTS_2026-09-07/room-care.csv", rows);
    }

    [UnityTest] public IEnumerator RealBowls_RecheckHiddenDistantBlockedAndForeignTargetsOnClick()
    {
        yield return Load("LivingRoom_Level01");
        foreach (string kind in new[] { "food", "water" })
        {
            var setup = Bowl(kind);
            Move(setup.InteractionPoint.position);
            Refresh();
            Assert.That(Get(bowls, "currentBowl"), Is.SameAs(setup), kind);
            Assert.That(bowls.HasVisibleAction, Is.True);

            // Move between rendering the button and receiving its click.
            Move(setup.InteractionPoint.position + Vector3.back * 1.5f);
            Vector3 before = bowls.transform.position;
            button.onClick.Invoke();
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(bowls.HasVisibleAction, Is.False);
            Assert.That(bowls.transform.position, Is.EqualTo(before), "Stale click must not teleport.");

            Move(setup.InteractionPoint.position);
            Refresh();
            var enabled = setup.Bowl.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            Assert.That(enabled, Is.Not.Empty);
            foreach (var renderer in enabled) renderer.enabled = false;
            button.onClick.Invoke();
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(bowls.HasVisibleAction, Is.False, "Disabled art is not a bowl.");
            foreach (var renderer in enabled) renderer.enabled = true;

            Refresh();
            setup.Bowl.gameObject.SetActive(false);
            button.onClick.Invoke();
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(bowls.HasVisibleAction, Is.False);
            setup.Bowl.gameObject.SetActive(true);

            Refresh();
            var wall = new GameObject("CareTestObstruction", typeof(BoxCollider));
            wall.transform.position = setup.InteractionPoint.position + Vector3.up * .25f;
            wall.GetComponent<BoxCollider>().size = new Vector3(.65f, .5f, .06f);
            Physics.SyncTransforms();
            button.onClick.Invoke();
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(bowls.HasVisibleAction, Is.False, "Blocked entrance");
            Object.DestroyImmediate(wall);
            Physics.SyncTransforms();

            Refresh();
            var parent = setup.Bowl.parent;
            var scene = setup.Bowl.gameObject.scene;
            var foreign = SceneManager.CreateScene("OtherCareRoom");
            setup.Bowl.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(setup.Bowl.gameObject, foreign);
            button.onClick.Invoke();
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(bowls.HasVisibleAction, Is.False, "Another loaded room cannot supply this bowl.");
            SceneManager.MoveGameObjectToScene(setup.Bowl.gameObject, scene);
            setup.Bowl.SetParent(parent, true);
            yield return SceneManager.UnloadSceneAsync(foreign);
        }
        Assert.That(hunger.IsEating || thirst.IsDrinking, Is.False);
    }

    [UnityTest] public IEnumerator RealCare_EatingDrinkingSleepingAndWakeStillWork()
    {
        yield return Load("LivingRoom_Level01");
        foreach (string kind in new[] { "food", "water" })
        {
            var setup = Bowl(kind);
            Set(bowls, kind == "food" ? "eatingDuration" : "drinkingDuration", .3f);
            Move(setup.InteractionPoint.position);
            Refresh();
            button.onClick.Invoke();
            Assert.That(bowls.IsInteracting, Is.True, kind);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (bowls.IsInteracting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(bowls.IsInteracting, Is.False);
            Assert.That(bowls.GetComponent<CatMovement>().IsMovementPhysicallyLocked, Is.False);
        }
        Assert.That(hunger.CurrentHunger, Is.GreaterThan(35f));
        Assert.That(thirst.CurrentThirst, Is.GreaterThan(35f));

        Move(((Transform)Get(sleep, "bedInteractionPoint")).position);
        Refresh();
        Assert.That(sleep.WantsActionButton, Is.True);
        button.onClick.Invoke();
        Assert.That(sleep.IsSleeping, Is.True);
        yield return new WaitForSeconds(1.5f);
        Refresh();
        Assert.That(bowls.HasVisibleAction, Is.True, "Wake must remain available on the mattress.");
        button.onClick.Invoke();
        Assert.That(sleep.IsSleeping, Is.False);
        Assert.That(bowls.GetComponent<CatMovement>().IsMovementPhysicallyLocked, Is.False);

        Refresh();
        sleep.TutorialBedTarget.gameObject.SetActive(false);
        button.onClick.Invoke();
        Assert.That(sleep.IsSleeping, Is.False, "Hidden bed cannot start sleep.");
        Assert.That(bowls.HasVisibleAction, Is.False);
        Assert.That(sleep.TryRestoreSleepingState(out _), Is.False);
        sleep.TutorialBedTarget.gameObject.SetActive(true);
    }
}
