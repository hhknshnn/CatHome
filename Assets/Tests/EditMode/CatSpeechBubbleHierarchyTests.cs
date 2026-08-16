#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CatSpeechBubbleHierarchyTests
{
    [Test]
    public void EnsureVisualHierarchy_RepairsEveryRequiredVisualPart()
    {
        GameObject hud = new GameObject("TestHudCanvas", typeof(RectTransform), typeof(Canvas));
        hud.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        GameObject cat = new GameObject("TestCat");
        CatSpeechBubble speech = cat.AddComponent<CatSpeechBubble>();

        MethodInfo ensure = typeof(CatSpeechBubble).GetMethod(
            "EnsureVisualHierarchy", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(ensure, Is.Not.Null);
        ensure.Invoke(speech, null);

        GameObject visual = GameObject.Find("CatSpeechBubbleVisual");
        Assert.That(visual, Is.Not.Null);
        Assert.That(visual.GetComponent<Canvas>(), Is.Not.Null);
        Assert.That(visual.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);

        Transform bubble = visual.transform.Find("Bubble");
        Assert.That(bubble, Is.Not.Null);
        string[] names =
        {
            "ShadowTail", "ShadowPanel", "OrangeTail", "OrangeFrame",
            "CreamTail", "CreamFace", "Message"
        };
        foreach (string childName in names)
            Assert.That(bubble.Find(childName), Is.Not.Null, childName);
        Assert.That(bubble.Find("Message").GetComponent<TextMeshProUGUI>(), Is.Not.Null);

        Object.DestroyImmediate(visual);
        Object.DestroyImmediate(cat);
        Object.DestroyImmediate(hud);
    }
}
#endif
