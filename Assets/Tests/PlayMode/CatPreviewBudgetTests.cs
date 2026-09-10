#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class CatPreviewBudgetTests
{
    private GameObject canvasObject, host;
    private CatBreedTurntablePreview preview;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private T Field<T>(string name) => (T)typeof(CatBreedTurntablePreview).GetField(name, Private).GetValue(preview);

    private void Create(float size)
    {
        canvasObject = new GameObject("Preview budget canvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        host = new GameObject("Preview budget viewport", typeof(RectTransform), typeof(RawImage));
        var rect = host.GetComponent<RectTransform>();
        rect.SetParent(canvasObject.transform, false);
        rect.sizeDelta = Vector2.one * size;
        preview = host.AddComponent<CatBreedTurntablePreview>();
        Canvas.ForceUpdateCanvases();
        preview.Show(CatBreedCatalog.Load().Get(0));
    }

    [UnityTest]
    public IEnumerator SmallPortrait_UsesSmallRenderTargetAndOneShadowCaster()
    {
        Create(148);
        yield return null;
        var texture = Field<RenderTexture>("renderTexture");
        Assert.That(texture.width, Is.EqualTo(256));
        Assert.That(texture.antiAliasing, Is.EqualTo(1));
        Assert.That(texture.IsCreated(), Is.True);
        Assert.That(host.GetComponent<RawImage>().texture, Is.SameAs(texture));
        var lights = Field<Light[]>("stageLights");
        Assert.That(lights[0].shadows, Is.EqualTo(LightShadows.Hard));
        Assert.That(lights[1].shadows, Is.EqualTo(LightShadows.None));
    }

    [UnityTest]
    public IEnumerator ResizeAndScreenBudget_ReuseCatAndKeepOrbitControls()
    {
        Create(548);
        yield return null;
        var original = Field<RenderTexture>("renderTexture");
        var model = Field<GameObject>("model");
        Assert.That(original.width, Is.EqualTo(1024));
        preview.ConfigureQuality(512, 15);
        yield return null;
        yield return null;
        Assert.That(Field<RenderTexture>("renderTexture").width, Is.EqualTo(512));
        Assert.That(original == null, Is.True, "The previous render target must be released.");
        host.GetComponent<RectTransform>().sizeDelta = Vector2.one * 148;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSecondsRealtime(.55f);
        Assert.That(Field<RenderTexture>("renderTexture").width, Is.EqualTo(256));
        Assert.That(Field<GameObject>("model"), Is.SameAs(model));
        var camera = Field<Camera>("previewCamera");
        Vector3 before = camera.transform.position;
        preview.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = Vector2.up });
        Assert.That(Vector3.Distance(before, camera.transform.position), Is.GreaterThan(.01f));
    }

    [UnityTest]
    public IEnumerator ReducedMotion_DoesNotRedrawFrozenPortraitButStillAcceptsInput()
    {
        Create(148);
        preview.SetReducedMotion(true);
        yield return null;
        float next = Field<float>("nextFrame");
        yield return new WaitForSecondsRealtime(.25f);
        Assert.That(Field<float>("nextFrame"), Is.EqualTo(next));
        preview.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = Vector2.up });
        yield return null;
        Assert.That(Field<float>("nextFrame"), Is.GreaterThan(next));
        Assert.That(Field<Animator>("previewAnimator").speed, Is.Zero);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (canvasObject != null) Object.Destroy(canvasObject);
        yield return null;
    }
}
#endif
