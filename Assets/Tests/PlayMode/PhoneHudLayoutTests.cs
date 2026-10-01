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
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Run against the real additive HUD inside UiQaTestSession's copied save.
// The runner selects the actual Game view resolution before invoking these tests.
public sealed class PhoneHudLayoutTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CareAlignmentPolishTests home;
    CatCompanionPanel panel;
    MobileJoystick joystick;
    RectTransform dock, shortcut;
    static string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/PHONE_BOWL_HUD_FIX_2026-09-23");

    [Serializable] sealed class LayoutProof
    {
        public string test, phase;
        public int width, height;
        public Rect physicalSafeArea, dock, commands, room, games, joystick, strip;
        public Vector2 joystickDirection;
        public bool copiedSave;
    }

    [SetUp] public void Before()
    {
        home = new CareAlignmentPolishTests();
        home.Before();
    }

    [TearDown] public void After()
    {
        if (joystick != null) joystick.CancelInput();
        if (panel != null) panel.Close();
        home.After();
    }

    IEnumerator Boot()
    {
        yield return (IEnumerator)typeof(CareAlignmentPolishTests).GetMethod("Home", Private).Invoke(home, null);
        panel = Object.FindAnyObjectByType<CatCompanionPanel>(FindObjectsInactive.Include);
        Assert.That(panel, Is.Not.Null);
        panel.Close();
        var until = Time.realtimeSinceStartup + 6f;
        while (Time.realtimeSinceStartup < until)
        {
            shortcut = panel.transform.Find("SafeArea/CompanionShortcut") as RectTransform;
            if (shortcut != null && shortcut.gameObject.activeInHierarchy && !WhileYouWereAwayPopup.IsAnyOpen)
                break;
            yield return null;
        }
        Assert.That(shortcut != null && shortcut.gameObject.activeInHierarchy, Is.True, "Real home shortcut must become available.");
        var layout = Object.FindAnyObjectByType<PremiumHomeDockLayout>();
        Assert.That(layout, Is.Not.Null);
        dock = layout.transform as RectTransform;
        joystick = Object.FindAnyObjectByType<MobileJoystick>();
        Assert.That(dock, Is.Not.Null);
        Assert.That(joystick, Is.Not.Null);
        Assert.That(EventSystem.current, Is.Not.Null);
        yield return null;
        yield return new WaitForSecondsRealtime(.8f);
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
    }

    IEnumerator Capture(string phase)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        Directory.CreateDirectory(Output);
        string stem = "phone-hud-" + TestContext.CurrentContext.Test.Name + "-" + Screen.width + "x" + Screen.height + "-" + phase;
        string path = Path.GetFullPath(Path.Combine(Output, stem + ".png"));
        Canvas.ForceUpdateCanvases();
        var strip = joystick.GetComponentInParent<Canvas>().transform.Find("ModernHomeViewportStrip") as RectTransform;
        var proof = new LayoutProof {
            test = TestContext.CurrentContext.Test.Name, phase = phase, width = Screen.width, height = Screen.height,
            physicalSafeArea = Screen.safeArea, dock = ScreenRect(dock), commands = ScreenRect(shortcut),
            room = ScreenRect(dock.Find("CurrentRoomStatus") as RectTransform),
            games = ScreenRect(dock.Find("PlayCatRunnerButton") as RectTransform),
            joystick = ScreenRect((RectTransform)joystick.transform), strip = ScreenRect(strip),
            joystickDirection = joystick.Direction, copiedSave = EditorQaSession.IsActive
        };
        File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonUtility.ToJson(proof, true));
        if(phase=="full-down") Assert.That(joystick.Direction.y,Is.LessThan(-.99f),"Pointer must remain held in the captured frame.");
        var pixels=ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(path,pixels.EncodeToPNG()); }
        finally { Object.Destroy(pixels); }
        Assert.That(File.Exists(path), Is.True, "Actual Game view screenshot must be written: " + path);
    }

    static Rect ScreenRect(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector2 max = min;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    void AssertDockSlot()
    {
        Canvas.ForceUpdateCanvases();
        var commands = shortcut.GetComponent<Button>();
        var room = dock.Find("CurrentRoomStatus") as RectTransform;
        var games = dock.Find("PlayCatRunnerButton") as RectTransform;
        Assert.That(room, Is.Not.Null); Assert.That(games, Is.Not.Null);
        Rect actual = ScreenRect(shortcut), left = ScreenRect(room), right = ScreenRect(games);
        Vector2 expected = RectTransformUtility.WorldToScreenPoint(null, dock.TransformPoint(new Vector3(135f, 0f, 0f)));
        Assert.That(Vector2.Distance(actual.center, expected), Is.LessThan(.6f), "Commands must use the real dock's third slot, even when its separate parent has a safe-area offset.");
        Assert.That(actual.width, Is.EqualTo(left.width).Within(.6f));
        Assert.That(actual.height, Is.EqualTo(left.height).Within(.6f));
        Assert.That(actual.yMin, Is.EqualTo(left.yMin).Within(.6f));
        float expectedGap = left.width * 22f / 248f;
        Assert.That(actual.xMin - left.xMax, Is.EqualTo(expectedGap).Within(.6f), "Room / commands gutter");
        Assert.That(right.xMin - actual.xMax, Is.EqualTo(expectedGap).Within(.6f), "Commands / games gutter");
        var surface = commands.targetGraphic as LowPolyPanelGraphic;
        Assert.That(surface, Is.Not.Null);
        Assert.That((bool)typeof(LowPolyPanelGraphic).GetField("hudPanelFinish", Private).GetValue(surface), Is.True, "Shortcut must keep the same HUD material finish.");
        var art = surface.transform.Find("StorybookCatHead").GetComponent<RawImage>();
        Assert.That(art.texture, Is.Not.Null); Assert.That(art.raycastTarget, Is.False);
        Assert.That(commands.GetComponentInChildren<TMP_Text>().text, Is.Not.Empty);
        foreach (Vector2 uv in new[] { new Vector2(.5f,.5f), new Vector2(.15f,.25f), new Vector2(.85f,.25f), new Vector2(.15f,.75f), new Vector2(.85f,.75f) })
        {
            Vector2 point = new Vector2(Mathf.Lerp(actual.xMin, actual.xMax, uv.x), Mathf.Lerp(actual.yMin, actual.yMax, uv.y));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            Assert.That(hits, Is.Not.Empty, "Shortcut must receive an actual UI raycast.");
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(commands), "A neighbouring button or overlay must not steal the command target.");
        }
    }

    [UnityTest] public IEnumerator Commands_RealDockSlotSurvivesAsymmetricSafeParent_AndModalRoundTrip()
    {
        yield return Boot();
        AssertDockSlot();
        var safe = (RectTransform)shortcut.parent;
        var owner = safe.GetComponent<SafeAreaRect>();
        bool enabled = owner != null && owner.enabled;
        Vector2 min = safe.anchorMin, max = safe.anchorMax, position = safe.anchoredPosition, size = safe.sizeDelta;
        Vector3 scale = safe.localScale;
        try
        {
            if (owner != null) owner.enabled = false;
            // These alter only the companion parent, representing unequal phone insets.
            // They do not claim to modify Unity's physical Screen.safeArea.
            foreach (Vector3 variation in new[] { new Vector3(92, 24, 1), new Vector3(-76, 42, 1), new Vector3(57, 30, .86f) })
            {
                safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
                safe.sizeDelta = new Vector2(-180f, -48f);
                safe.anchoredPosition = new Vector2(variation.x, variation.y);
                safe.localScale = Vector3.one * variation.z;
                yield return null; yield return new WaitForEndOfFrame();
                typeof(CatCompanionPanel).GetMethod("Update",Private).Invoke(panel,null);
                AssertDockSlot();
            }
        }
        finally
        {
            safe.anchorMin = min; safe.anchorMax = max; safe.anchoredPosition = position;
            safe.sizeDelta = size; safe.localScale = scale;
            if (owner != null) owner.enabled = enabled;
        }
        yield return null; yield return new WaitForEndOfFrame();
        AssertDockSlot();
        ExecuteEvents.Execute(shortcut.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        yield return null;
        Assert.That(CatCompanionPanel.IsAnyOpen, Is.True, "Actual shortcut submit opens commands.");
        Assert.That(shortcut.gameObject.activeSelf, Is.False, "Shortcut visibility remains owned by the companion modal.");
        panel.Close();
        yield return null; yield return new WaitForEndOfFrame();
        Assert.That(CatCompanionPanel.IsAnyOpen, Is.False);
        AssertDockSlot();
        yield return Capture("closed");
    }

    [UnityTest] public IEnumerator Joystick_EightFullDirectionsStayAboveStrip_AndReleaseCancels()
    {
        yield return Boot();
        var root = (RectTransform)joystick.transform;
        var handle = (RectTransform)typeof(MobileJoystick).GetField("handle", Private).GetValue(joystick);
        var face = handle.GetComponentsInChildren<LowPolyPanelGraphic>(true).First(g => g.name == "PremiumHandle");
        var strip = root.GetComponentInParent<Canvas>().transform.Find("ModernHomeViewportStrip") as RectTransform;
        Assert.That(strip, Is.Not.Null);
        try
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 direction = new Vector2(Mathf.Cos(i * Mathf.PI / 4f), Mathf.Sin(i * Mathf.PI / 4f));
                Vector3 local = new Vector3(direction.x * root.rect.width * .6f, direction.y * root.rect.height * .6f, 0f);
                var pointer = new PointerEventData(EventSystem.current) { pointerId = 641, position = RectTransformUtility.WorldToScreenPoint(null, root.TransformPoint(local)), button = PointerEventData.InputButton.Left };
                joystick.OnPointerDown(pointer); joystick.OnDrag(pointer);
                Assert.That(joystick.Direction.magnitude, Is.EqualTo(1f).Within(.001f), "Full direction must retain full input amplitude.");
                Assert.That(Vector2.Distance(joystick.Direction, direction), Is.LessThan(.002f));
                Canvas.ForceUpdateCanvases();
                var mesh = face.canvasRenderer.GetMesh();
                Assert.That(mesh.vertexCount, Is.GreaterThan(0), "Measure the real rendered thumb, including its bottom depth/shadow.");
                float bottom = float.PositiveInfinity;
                foreach (Vector3 vertex in mesh.vertices)
                    bottom = Mathf.Min(bottom, RectTransformUtility.WorldToScreenPoint(null, face.rectTransform.TransformPoint(vertex)).y);
                Assert.That(bottom, Is.GreaterThan(ScreenRect(strip).yMax + 1f), "Direction " + i + " must keep the entire thumb mesh above the opaque navigation strip.");
                if (i == 6) yield return Capture("full-down");
                joystick.OnPointerUp(pointer);
                Assert.That(joystick.Direction, Is.EqualTo(Vector2.zero));
                Assert.That(handle.anchoredPosition, Is.EqualTo(Vector2.zero));
            }
        }
        finally { joystick.CancelInput(); }
        yield return null;
    }

    [UnityTest] public IEnumerator Commands_RebindToReloadedRealHomeHud()
    {
        yield return Boot();
        AssertDockSlot();
        var oldDock = dock;
        yield return Boot();
        Assert.That(oldDock == null, Is.True, "This is a real scene reload, not rechecking the original dock.");
        Assert.That(Object.FindObjectsByType<CatCompanionPanel>(FindObjectsInactive.Include).Length, Is.EqualTo(1));
        AssertDockSlot();
        yield return Capture("reloaded");
    }
}
#endif
