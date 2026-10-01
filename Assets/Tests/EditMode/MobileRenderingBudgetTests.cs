using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public sealed class MobileRenderingBudgetTests
{
    private readonly List<Object> temporary = new();
    private UniversalRenderPipelineAsset asset;

    [TearDown]
    public void Cleanup()
    {
        if (asset != null)
        {
            var cache = (IDictionary)typeof(MobilePresentation).GetField("pipelineBudgets", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            cache.Remove(asset);
        }
        for (int i = temporary.Count - 1; i >= 0; i--)
            if (temporary[i] != null) Object.DestroyImmediate(temporary[i]);
        temporary.Clear();
        asset = null;
    }

    [TestCase(.85f, 2400, 1080, 1600f / 2400f)]
    [TestCase(.85f, 1080, 2400, 1600f / 2400f)]
    [TestCase(.5f, 2400, 1080, .5f)]
    [TestCase(1f, 1280, 720, .85f)]
    [TestCase(.85f, 0, 0, .85f)]
    public void RenderBudget_BoundsPixelsWithoutUpscalingLowerAuthoredQuality(float authored, int width, int height, float expected)
    {
        float scale = MobilePresentation.RenderScaleForSize(authored, width, height);
        Assert.That(scale, Is.EqualTo(expected).Within(.00001f));
        Assert.That(scale, Is.LessThanOrEqualTo(authored));
        Assert.That(scale, Is.LessThanOrEqualTo(.85f));
        Assert.That(Mathf.Max(width, height) * scale, Is.LessThanOrEqualTo(1600.01f));
    }

    [TestCase(.85f)]
    [TestCase(.5f)]
    public void PipelineBudget_RepeatedFramesAndResizeUseOriginalScale(float authored)
    {
        var source = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        asset = Clone(source);
        // Keep a valid isolated renderer while exercising resizing only.
        var renderer = Clone(source.rendererDataList[0]);
        renderer.rendererFeatures.Clear();
        SetRenderers(asset, new[] { renderer });
        asset.renderScale = authored;
        var apply = ApplyDelegate();
        apply(asset, 2400, 1080);
        float first = Mathf.Min(authored, 1600f / 2400f);
        for (int frame = 0; frame < 120; frame++) apply(asset, 2400, 1080);
        Assert.That(asset.renderScale, Is.EqualTo(first).Within(.00001f));
        apply(asset, 1080, 2400);
        Assert.That(asset.renderScale, Is.EqualTo(first).Within(.00001f), "Rotation has the same pixel budget.");
        apply(asset, 3840, 2160);
        Assert.That(asset.renderScale, Is.EqualTo(Mathf.Min(authored, 1600f / 3840f)).Within(.00001f));
        apply(asset, 1280, 720);
        Assert.That(asset.renderScale, Is.EqualTo(authored).Within(.00001f), "A smaller display restores the authored cap instead of retaining the previous reduction.");
        apply(asset, 2400, 1080);
        Assert.That(asset.renderScale, Is.EqualTo(first).Within(.00001f));
    }

    [Test]
    public void PipelineBudget_DisablesOnlyClonedAoAndLeavesOtherAppearanceAndAssetsIntact()
    {
        var source = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        Assert.That(source, Is.Not.Null);
        var sourceRenderer = source.rendererDataList[0];
        float originalScale = source.renderScale;
        string rendererBefore = EditorJsonUtility.ToJson(sourceRenderer);
        var renderer = Clone(sourceRenderer);
        renderer.rendererFeatures.Clear();
        var ao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
        temporary.Add(ao); ao.SetActive(true);
        var unrelated = ScriptableObject.CreateInstance<RenderObjects>();
        temporary.Add(unrelated); unrelated.SetActive(true);
        renderer.rendererFeatures.Add(ao);
        renderer.rendererFeatures.Add(null);
        renderer.rendererFeatures.Add(unrelated);
        asset = Clone(source);
        SetRenderers(asset, new ScriptableRendererData[] { renderer, null });
        bool hdr = asset.supportsHDR;
        int msaa = asset.msaaSampleCount;
        float shadows = asset.shadowDistance;
        int screenWidth = Screen.width, screenHeight = Screen.height;
        ApplyDelegate()(asset, 2400, 1080);
        Assert.That(ao.isActive, Is.False);
        Assert.That(unrelated.isActive, Is.True);
        Assert.That(asset.supportsHDR, Is.EqualTo(hdr));
        Assert.That(asset.msaaSampleCount, Is.EqualTo(msaa));
        Assert.That(asset.shadowDistance, Is.EqualTo(shadows));
        Assert.That(Screen.width, Is.EqualTo(screenWidth));
        Assert.That(Screen.height, Is.EqualTo(screenHeight));
        Assert.That(source.renderScale, Is.EqualTo(originalScale));
        Assert.That(EditorJsonUtility.ToJson(sourceRenderer), Is.EqualTo(rendererBefore), "Only isolated renderer/feature clones may change in editor tests.");
    }

    private T Clone<T>(T source) where T : Object
    {
        Assert.That(source, Is.Not.Null);
        T clone = Object.Instantiate(source);
        clone.hideFlags = HideFlags.HideAndDontSave;
        temporary.Add(clone);
        return clone;
    }
    private static void SetRenderers(UniversalRenderPipelineAsset target, ScriptableRendererData[] renderers) =>
        typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, renderers);
    private static Action<UniversalRenderPipelineAsset, int, int> ApplyDelegate() =>
        (Action<UniversalRenderPipelineAsset, int, int>)Delegate.CreateDelegate(typeof(Action<UniversalRenderPipelineAsset, int, int>),
            typeof(MobilePresentation).GetMethod("ConfigurePipeline", BindingFlags.Static | BindingFlags.NonPublic));
}
