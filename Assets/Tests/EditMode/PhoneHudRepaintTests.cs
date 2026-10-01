#if UNITY_EDITOR
using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PhoneHudRepaintTests
{
    private GameObject canvasRoot, controllerRoot;
    private readonly Type[] needTypes = { typeof(HungerSystem), typeof(ThirstSystem), typeof(EnergySystem) };
    private readonly string[] needNames = { "Hunger", "Thirst", "Energy" };
    private Component[] needs;
    private TextMeshProUGUI[] labels;
    private Image[] fills, frames;
    private RectTransform[] panels;

    [SetUp]
    public void SetUp()
    {
        canvasRoot = new GameObject("Phone repaint fixture", typeof(RectTransform), typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        controllerRoot = new GameObject("Inactive need owners");
        controllerRoot.SetActive(false);
        needs = new Component[3]; labels = new TextMeshProUGUI[3];
        fills = new Image[3]; frames = new Image[3]; panels = new RectTransform[3];
        for (int i = 0; i < 3; i++)
        {
            panels[i] = Rect(needNames[i], canvasRoot.transform);
            labels[i] = Rect("PercentageText", panels[i]).gameObject.AddComponent<TextMeshProUGUI>();
            fills[i] = Rect("Fill", panels[i]).gameObject.AddComponent<Image>();
            fills[i].type = Image.Type.Filled;
            frames[i] = Rect("Frame", panels[i]).gameObject.AddComponent<Image>();
            needs[i] = controllerRoot.AddComponent(needTypes[i]);
            Set(needs[i], "percentageText", labels[i]);
            Set(needs[i], needNames[i].ToLowerInvariant() + "Fill", fills[i]);
            Set(needs[i], needNames[i].ToLowerInvariant() + "Frame", frames[i]);
            Set(needs[i], "catMovement", null);
        }
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(controllerRoot);
        Object.DestroyImmediate(canvasRoot);
    }

    [Test]
    public void StorybookNeeds_IdleHasNoLabelRepaint_ChangesAndCriticalRecoveryStillRender()
    {
        var layout = canvasRoot.AddComponent<StorybookHudLayout>();
        // Keep the fixture independent of whichever authoring scenes are open.
        Set(layout, "nextResolve", float.PositiveInfinity);
        layout.Configure(panels[0], panels[1], panels[2]);
        SetNeeds(80.75f); layout.Refresh();
        int dirty = 0;
        foreach (var label in labels) label.RegisterDirtyVerticesCallback(() => dirty++);
        for (int pass = 0; pass < 120; pass++) { RefreshNeeds(); layout.Refresh(); }
        Assert.That(dirty, Is.Zero, "Idle need owners and Storybook must agree before the render pass.");
        foreach (var label in labels)
        {
            Assert.That(label.text, Is.EqualTo("81%"));
            Assert.That(label.color, Is.EqualTo((Color)new Color32(255, 253, 242, 255)));
        }

        // A fractional need change still moves the bar without regenerating the same integer label.
        SetNeeds(80.25f); layout.Refresh();
        Assert.That(dirty, Is.Zero);
        foreach (var fill in fills) Assert.That(fill.fillAmount, Is.EqualTo(.8025f).Within(.00001f));
        SetNeeds(49.25f); layout.Refresh();
        Assert.That(dirty, Is.GreaterThan(0));
        foreach (var label in labels) Assert.That(label.text, Is.EqualTo("50%"));
        SetNeeds(0f); layout.Refresh();
        foreach (var label in labels)
        {
            Assert.That(label.text, Is.EqualTo("0%"));
            Assert.That(label.color, Is.EqualTo((Color)new Color32(255, 230, 175, 255)));
        }
        foreach (var fill in fills) Assert.That(fill.fillAmount, Is.EqualTo(.08f));
        dirty = 0;
        for (int pass = 0; pass < 20; pass++) { RefreshNeeds(); layout.Refresh(); }
        Assert.That(dirty, Is.Zero, "Critical Storybook labels retain the current warm-gold color.");
        SetNeeds(100f); layout.Refresh();
        foreach (var label in labels)
        {
            Assert.That(label.text, Is.EqualTo("100%"));
            Assert.That(label.color, Is.EqualTo((Color)new Color32(255, 253, 242, 255)));
        }
        foreach (var frame in frames) Assert.That(frame.color, Is.EqualTo(Color.white));
        layout.enabled = false;
        RefreshNeeds();
        foreach (var label in labels) Assert.That(label.color, Is.EqualTo((Color)PremiumUiStyle.Ink), "Disabling Storybook releases its palette ownership.");
    }

    [Test]
    public void LegacyNeeds_KeepInkAndCriticalPulseWithoutRedundantWrites()
    {
        SetNeeds(75f);
        int dirty = 0;
        foreach (var label in labels) label.RegisterDirtyVerticesCallback(() => dirty++);
        for (int pass = 0; pass < 120; pass++) RefreshNeeds();
        Assert.That(dirty, Is.Zero);
        foreach (var label in labels) Assert.That(label.color, Is.EqualTo((Color)PremiumUiStyle.Ink));
        SetNeeds(0f);
        float pulse = (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * .5f;
        Color critical = Color.Lerp(PremiumUiStyle.Ink, new Color32(152, 53, 43, 255), pulse);
        foreach (var label in labels) Assert.That(label.color, Is.EqualTo(critical));
        SetNeeds(100f);
        foreach (var label in labels) Assert.That(label.color, Is.EqualTo((Color)PremiumUiStyle.Ink));
    }

    [Test]
    public void Strip_PreservesStorybookColorWithoutIdleRebuild_AndLegacyDefault()
    {
        var strip = Rect("ViewportStrip", canvasRoot.transform).gameObject.AddComponent<ModernHomeStrip>();
        var image = strip.GetComponent<Image>();
        Assert.That(image.color, Is.EqualTo((Color)ModernUiArt.Paper));
        var presentation = canvasRoot.AddComponent<StorybookHudBottomPresentation>();
        presentation.Configure(null, image);
        int dirty = 0;
        image.RegisterDirtyVerticesCallback(() => dirty++);
        for (int pass = 0; pass < 120; pass++)
        {
            strip.Refresh();
            typeof(StorybookHudBottomPresentation).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presentation, null);
        }
        Assert.That(dirty, Is.Zero, "Strip sizing must not fight its Storybook color owner.");
        Assert.That(image.color, Is.EqualTo(StorybookHudBottomPresentation.StripColor));
        Assert.That(image.raycastTarget, Is.False);
        Assert.That(image.rectTransform.rect.height, Is.GreaterThanOrEqualTo(HomeWorldViewport.StripHeight));
    }

    private void SetNeeds(float value)
    {
        for (int i = 0; i < needs.Length; i++) Set(needs[i], "current" + needNames[i], value);
        RefreshNeeds();
    }
    private void RefreshNeeds()
    {
        foreach (var need in needs)
            need.GetType().GetMethod("UpdateUI", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null).Invoke(need, null);
    }
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }
}
#endif
