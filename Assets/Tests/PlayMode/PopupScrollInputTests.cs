#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PopupScrollInputTests
{
    private GameObject canvasRoot, eventRoot, prefabRoot;
    private EventSystem events;
    private bool previousReducedMotion;
    private bool prepared;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Run native input tests in the isolated QA save session.");
        previousReducedMotion = CatRunnerProgressService.ReducedMotion;
        prepared = true;
        canvasRoot = new GameObject("Scroll input test", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        events = EventSystem.current;
        if (events == null)
        {
            eventRoot = new GameObject("Scroll test events", typeof(EventSystem));
            events = eventRoot.GetComponent<EventSystem>();
        }
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (!prepared) yield break;
        if (prefabRoot != null) Object.Destroy(prefabRoot);
        if (canvasRoot != null) Object.Destroy(canvasRoot);
        if (eventRoot != null) Object.Destroy(eventRoot);
        CatRunnerProgressService.SetReducedMotion(previousReducedMotion);
        prepared = false;
        yield return null;
    }

    [UnityTest]
    public IEnumerator ContentPhotosAndGaps_ReceiveWheelMouseDragAndTouchDrag()
    {
        foreach (bool viewportIsScrollRoot in new[] { false, true })
        {
            var scroll = MakeList(viewportIsScrollRoot, out var button);
            PremiumScrollInput.Ensure(scroll);
            yield return Settle();

            var point = ScreenPoint(scroll.viewport, new Vector2(-100f, -50f));
            var pointer = Pointer(point, -1);
            var hit = Hit(pointer);
            Assert.That(hit.GetComponentInParent<ScrollRect>(), Is.SameAs(scroll), "The content must not hit the outer popup.");
            pointer.scrollDelta = new Vector2(0f, -1f);
            float before = scroll.verticalNormalizedPosition;
            Assert.That(ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.scrollHandler), Is.Not.Null);
            Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(before - .03f), "Wheel over a photo/gap must move the list.");

            foreach (int pointerId in new[] { -1, 0 })
            {
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = 1f;
                Canvas.ForceUpdateCanvases();
                pointer = Pointer(point, pointerId);
                hit = Hit(pointer);
                var dragTarget = ExecuteEvents.GetEventHandler<IDragHandler>(hit);
                Assert.That(dragTarget, Is.SameAs(scroll.gameObject));
                pointer.pointerDrag = dragTarget;
                pointer.pressPosition = pointer.position;
                ExecuteEvents.Execute(dragTarget, pointer, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.Execute(dragTarget, pointer, ExecuteEvents.beginDragHandler);
                pointer.position += Vector2.up * 90f;
                pointer.delta = Vector2.up * 90f;
                pointer.dragging = true;
                ExecuteEvents.Execute(dragTarget, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(dragTarget, pointer, ExecuteEvents.endDragHandler);
                Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(.9f), "A mouse or touch swipe on the page must scroll without grabbing the bar.");
            }

            // Native ScrollRect remains the ancestor drag handler of real card
            // buttons, so the input module can cancel their click after a swipe.
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
            yield return Settle();
            pointer = Pointer(RectTransformUtility.WorldToScreenPoint(null, button.transform.position), -1);
            hit = Hit(pointer);
            Assert.That(hit.GetComponentInParent<Button>(), Is.SameAs(button), "The viewport surface must stay behind interactive cards.");
            Assert.That(ExecuteEvents.GetEventHandler<IDragHandler>(hit), Is.SameAs(scroll.gameObject));
            int clicks = 0;
            button.onClick.AddListener(() => clicks++);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(clicks, Is.EqualTo(1), "An ordinary tap is still an ordinary card click.");
            Object.Destroy(scroll.gameObject);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator ReducedMotion_StopsInertiaButKeepsDirectScrollInput()
    {
        CatRunnerProgressService.SetReducedMotion(false);
        var scroll = MakeList(false, out _);
        PremiumScrollInput.Ensure(scroll);
        yield return Settle();
        Assert.That(scroll.inertia, Is.True);
        scroll.velocity = new Vector2(0f, 200f);
        CatRunnerProgressService.SetReducedMotion(true);
        Assert.That(scroll.inertia, Is.False);
        Assert.That(scroll.velocity, Is.EqualTo(Vector2.zero));
        var pointer = Pointer(ScreenPoint(scroll.viewport, Vector2.zero), -1);
        pointer.scrollDelta = new Vector2(0f, -1f);
        ExecuteEvents.ExecuteHierarchy(Hit(pointer), pointer, ExecuteEvents.scrollHandler);
        Assert.That(scroll.verticalNormalizedPosition, Is.LessThan(.97f), "Reduced motion must not disable deliberate wheel/touch navigation.");
        CatRunnerProgressService.SetReducedMotion(false);
        Assert.That(scroll.inertia, Is.True);
    }

    [UnityTest]
    public IEnumerator AuthoredPopupViewports_RouteTheirNativeContentThroughSharedInput()
    {
        foreach (string path in new[] { "Assets/UI/ShopPanel.prefab", "Assets/UI/RoomSelectorPanel.prefab", "Assets/UI/QuestPanel.prefab", "Assets/UI/CatBreedShopPanel.prefab" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            // Keep popup controllers asleep: their singleton/opening state is
            // tested by the full UI tour. Here we exercise the real authored
            // viewport and card hierarchy without introducing duplicate panels.
            prefabRoot = new GameObject("Inactive popup fixture");
            prefabRoot.transform.SetParent(canvasRoot.transform, false);
            prefabRoot.SetActive(false);
            var instance = Object.Instantiate(prefab, prefabRoot.transform);
            var scroll = instance.GetComponentInChildren<ScrollRect>(true);
            Assert.That(scroll, Is.Not.Null, path);
            PremiumScrollInput.Ensure(scroll);
            Assert.That(scroll.viewport.GetComponent<Graphic>().raycastTarget, Is.True, path);
            Assert.That(scroll.viewport.GetComponent<CanvasRenderer>().cullTransparentMesh, Is.False, path);

            // Exercise each authored content tree on an isolated canvas, outside
            // its closed modal animation, without changing the source prefab.
            scroll.transform.SetParent(canvasRoot.transform, false);
            var rect = (RectTransform)scroll.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(800f, 400f);
            foreach (var group in scroll.GetComponentsInChildren<CanvasGroup>(true))
            { group.alpha = 1f; group.interactable = true; group.blocksRaycasts = true; }
            scroll.gameObject.SetActive(true);
            Object.Destroy(prefabRoot);
            prefabRoot = null;
            yield return Settle();
            var pointer = Pointer(ScreenPoint(scroll.viewport, new Vector2(0f, -120f)), -1);
            Assert.That(Hit(pointer).GetComponentInParent<ScrollRect>(), Is.SameAs(scroll), path + " must route empty list space to its ScrollRect.");
            Object.Destroy(scroll.gameObject);
            yield return null;
        }
    }

    private ScrollRect MakeList(bool selfViewport, out Button button)
    {
        var root = Rect("Test list", canvasRoot.transform);
        root.sizeDelta = new Vector2(340f, 280f);
        var scroll = root.gameObject.AddComponent<ScrollRect>();
        var viewport = selfViewport ? root : Rect("Viewport", root);
        if (!selfViewport) { viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one; viewport.sizeDelta = Vector2.zero; }
        viewport.gameObject.AddComponent<RectMask2D>();
        if (selfViewport) viewport.gameObject.AddComponent<Image>().color = Color.clear;
        var content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1f);
        content.sizeDelta = new Vector2(0f, 900f);
        var photo = Rect("Noninteractive photo", content);
        photo.anchorMin = photo.anchorMax = new Vector2(.5f, 1f);
        photo.anchoredPosition = new Vector2(-60f, -140f);
        photo.sizeDelta = new Vector2(190f, 260f);
        photo.gameObject.AddComponent<Image>().raycastTarget = false;
        var action = Rect("Real card action", content);
        action.anchorMin = action.anchorMax = new Vector2(.5f, 1f);
        action.anchoredPosition = new Vector2(105f, -65f);
        action.sizeDelta = new Vector2(90f, 70f);
        var face = action.gameObject.AddComponent<Image>();
        button = action.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        return scroll;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
    private static IEnumerator Settle()
    { Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame(); }
    private PointerEventData Pointer(Vector2 point, int id)
    { return new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left, pointerId = id }; }
    private GameObject Hit(PointerEventData pointer)
    {
        var hits = new List<RaycastResult>();
        events.RaycastAll(pointer, hits);
        Assert.That(hits, Is.Not.Empty, "The clipped content needs a real EventSystem raycast surface.");
        pointer.pointerCurrentRaycast = hits[0];
        return hits[0].gameObject;
    }
    private static Vector2 ScreenPoint(RectTransform rect, Vector2 local)
    { return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local)); }
}
#endif
