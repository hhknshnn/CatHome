#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RoomSelectorFeedbackTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Output = "Docs/QA/ROOM_SELECTOR_2026-09-10";
    RoomSelectorPanel panel;
    LevelLoader loader;
    HomeStoreSaveState savedStore;
    Type visualQa;
    int originalWidth, originalHeight;

    [UnitySetUp]
    public IEnumerator Prepare()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use an isolated copy of the player save.");
        originalWidth = Screen.width; originalHeight = Screen.height;
        visualQa = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UiQaVisualTour")).First(t => t != null);
        yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        float deadline = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < deadline)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader != null && loader.IsReady && !loader.IsTransitioning) break;
            yield return null;
        }
        Assert.That(loader != null && loader.IsReady, Is.True);
        savedStore = HomeStoreService.CaptureState();
        var unlocked = HomeStoreService.CaptureState();
        unlocked.ownedProductIds = unlocked.ownedProductIds.Concat(HomeRoomService.Rooms
            .Select(r => r.RequiredOwnershipId).Where(id => !string.IsNullOrEmpty(id))).Distinct().ToArray();
        HomeStoreService.ApplySavedState(unlocked);
        foreach (var celebration in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            celebration.enabled = false;
        visualQa.GetMethod("Clear").Invoke(null, null);
        foreach (var tutorial in Object.FindObjectsByType<PetTutorialHint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            tutorial.enabled = false;
        yield return new WaitForSecondsRealtime(.5f);
        panel = Object.FindAnyObjectByType<RoomSelectorPanel>();
        Assert.That(panel, Is.Not.Null);
        Directory.CreateDirectory(Output);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (panel != null) Call("ForceCloseImmediate");
        if (savedStore != null) HomeStoreService.ApplySavedState(savedStore);
        if (visualQa != null && originalWidth > 0 && originalHeight > 0)
            visualQa.GetMethod("Resolution").Invoke(null, new object[] { originalWidth, originalHeight });
        yield return null;
    }

    [UnityTest]
    public IEnumerator TemporaryTravelBlock_PreservesAllCardColours_AndFailureRestoresInput()
    {
        foreach (int width in new[] { 1920, 1440 })
        {
            visualQa.GetMethod("Resolution").Invoke(null, new object[] { width, 1080 });
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Screen.width, Is.EqualTo(width));
            panel.RequestOpen();
            yield return new WaitForSecondsRealtime(.35f);
            var buttons = Cards();
            Assert.That(buttons.Length, Is.EqualTo(HomeRoomService.Rooms.Count));
            Assert.That(buttons.All(b => b.IsInteractable()), Is.True);
            var baseline = buttons.ToDictionary(b => b, Colours);
            yield return Capture("open-" + width);
            var destination = HomeRoomService.Rooms.First(r => r.Id != HomeRoomService.CurrentRoomId);
            var chosen = buttons.First(b => b.name == "RoomCard_" + destination.Id);
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(chosen.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Call("HandleRoomLoadStarted", destination);
            yield return null;
            yield return null;
            foreach (var button in buttons)
            {
                Assert.That(button.IsInteractable(), Is.False, button.name + " still blocks duplicate input.");
                CollectionAssert.AreEqual(baseline[button], Colours(button), button.name + " must retain its rendered vertex colours while travelling.");
            }
            yield return Capture("travelling-" + width);
            int starts = 0;
            Action<HomeRoomDefinition> started = _ => starts++;
            loader.RoomLoadStarted += started;
            try
            {
                foreach (var button in buttons)
                {
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
                    button.onClick.Invoke();
                }
                Assert.That(starts, Is.Zero, "Pointer, keyboard and stale listeners cannot start another trip.");
            }
            finally { loader.RoomLoadStarted -= started; }
            panel.RequestClose();
            Assert.That(panel.IsTravelling, Is.True);
            Call("HandleRoomLoadFailed", destination.Id, "Test recovery");
            yield return null;
            Assert.That(buttons.All(b => b.IsInteractable()), Is.True);
            foreach (var button in buttons) CollectionAssert.AreEqual(baseline[button], Colours(button), button.name);
            ExecuteEvents.Execute(chosen.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
            yield return null;
            CollectionAssert.AreNotEqual(baseline[chosen], Colours(chosen), "Hover feedback must return after a failed trip.");
            ExecuteEvents.Execute(chosen.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            panel.RequestClose();
            yield return new WaitForSecondsRealtime(.3f);
            panel.RequestOpen();
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(buttons.All(b => b.IsInteractable()), Is.True, "Reopening restores the card controls.");
            panel.RequestClose();
            yield return new WaitForSecondsRealtime(.3f);
        }
    }

    [UnityTest]
    public IEnumerator RealRoomSelection_LoadsOnlyTheChosenRoom_AndReopensNormally()
    {
        foreach (string destination in new[] { HomeRoomService.BathroomId, HomeRoomService.LivingRoomId })
        {
            if (HomeRoomService.CurrentRoomId == destination) continue;
            panel.RequestOpen();
            yield return new WaitForSecondsRealtime(.3f);
            var buttons = Cards();
            var chosen = buttons.First(b => b.name == "RoomCard_" + destination);
            var baseline = buttons.ToDictionary(b => b, Colours);
            int starts = 0;
            string accepted = null;
            Action<HomeRoomDefinition> started = room => { starts++; accepted = room.Id; };
            loader.RoomLoadStarted += started;
            try
            {
                var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(chosen.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                Assert.That(panel.IsTravelling, Is.True);
                foreach (var button in buttons)
                {
                    CollectionAssert.AreEqual(baseline[button], Colours(button), button.name + " immediate travel colours");
                    button.onClick.Invoke();
                }
                Assert.That(starts, Is.EqualTo(1));
                Assert.That(accepted, Is.EqualTo(destination));
                float deadline = Time.realtimeSinceStartup + 30f;
                while ((!loader.IsReady || loader.IsTransitioning || RoomSelectorPanel.IsAnyOpen) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(loader.IsReady && !loader.IsTransitioning, Is.True);
                Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(destination));
                Assert.That(RoomSelectorPanel.IsAnyOpen, Is.False);
                Assert.That(starts, Is.EqualTo(1));
                Assert.That(Object.FindAnyObjectByType<CatMovement>().AreWorldActionsBlocked, Is.False);
                panel.RequestOpen();
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(buttons.All(b => b.IsInteractable()), Is.True);
                Assert.That(buttons.Count(b => b.transform.Find("CardVisual/PreviewWell/CurrentBadge").gameObject.activeSelf), Is.EqualTo(1));
                yield return Capture("arrived-" + destination);
                panel.RequestClose();
                yield return new WaitForSecondsRealtime(.3f);
            }
            finally { loader.RoomLoadStarted -= started; }
        }
    }

    Button[] Cards() => panel.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("RoomCard_", StringComparison.Ordinal)).ToArray();
    void Call(string method, params object[] args) => typeof(RoomSelectorPanel).GetMethod(method, Private).Invoke(panel, args);
    static Color32[] Colours(Button button)
    {
        var surface = button.targetGraphic as LowPolyPanelGraphic;
        Assert.That(surface, Is.Not.Null);
        using (var vertices = new VertexHelper())
        {
            typeof(LowPolyPanelGraphic).GetMethod("OnPopulateMesh", Private, null,
                new[] { typeof(VertexHelper) }, null).Invoke(surface, new object[] { vertices });
            var stream = new List<UIVertex>();
            vertices.GetUIVertexStream(stream);
            Assert.That(stream.Count, Is.GreaterThan(0));
            return stream.Select(v => v.color).ToArray();
        }
    }
    static IEnumerator Capture(string name)
    {
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/" + name + ".png"));
        // CaptureScreenshot writes a subsequent rendered frame; keep this state
        // visible until that frame is saved instead of capturing the next phase.
        yield return new WaitForSecondsRealtime(.12f);
    }
}
#endif
