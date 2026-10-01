using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Bounded mobile rendering; HUD resolution and gameplay dimensions stay native.</summary>
public static class MobilePresentation
{
    public static bool IsMobile => Application.isMobilePlatform;
    // RAM capacity does not identify GPU performance. Keep one predictable
    // mobile frame budget, including older phones with 6-8 GB of memory.
    // The existing API also selects the quieter cosmetic care effects.
    public static int FrameRateForMemory(int memoryMb) => 30;

    private sealed class PipelineBudget
    {
        public float authoredScale;
        public int longestEdge = -1;
    }
    private static readonly Dictionary<UniversalRenderPipelineAsset, PipelineBudget> pipelineBudgets = new();

    public static float RenderScaleForSize(float authoredScale, int width, int height)
    {
        int longestEdge = Mathf.Max(1, Mathf.Max(width, height));
        return Mathf.Min(authoredScale, Mathf.Min(.85f, 1600f / longestEdge));
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
        RenderPipelineManager.beginCameraRendering -= ConfigureCamera;
        pipelineBudgets.Clear();
        if (!IsMobile || Application.isEditor) return;
        Application.targetFrameRate = FrameRateForMemory(SystemInfo.systemMemorySize);
        ConfigurePipeline(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset, Screen.width, Screen.height);
        RenderPipelineManager.beginCameraRendering += ConfigureCamera;
    }

    private static void ConfigureCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!IsMobile || Application.isEditor || camera == null) return;
        // One persistent asset budget covers world and preview cameras. Do not
        // restore it between cameras: overlay stacks share the same pipeline.
        ConfigurePipeline(GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset, Screen.width, Screen.height);
        // Mobile uses 2x MSAA; a second full-screen SMAA pass adds avoidable cost.
        if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            data.antialiasing = AntialiasingMode.None;
    }

    private static void ConfigurePipeline(UniversalRenderPipelineAsset asset, int width, int height)
    {
        if (asset == null) return;
        if (!pipelineBudgets.TryGetValue(asset, out var budget))
        {
            budget = new PipelineBudget { authoredScale = asset.renderScale };
            pipelineBudgets.Add(asset, budget);
            // Remove only the extra screen-space AO pass. Authored HDR, bloom,
            // MSAA and shadows keep their current settings.
            foreach (var rendererData in asset.rendererDataList)
            {
                if (rendererData == null) continue;
                foreach (var feature in rendererData.rendererFeatures)
                    if (feature is ScreenSpaceAmbientOcclusion && feature.isActive)
                        feature.SetActive(false);
            }
        }

        int longestEdge = Mathf.Max(1, Mathf.Max(width, height));
        if (budget.longestEdge == longestEdge) return;
        budget.longestEdge = longestEdge;
        // Recompute from the authored value so resizing cannot compound a
        // previously reduced scale. Screen and overlay HUD resolution stay native.
        asset.renderScale = RenderScaleForSize(budget.authoredScale, width, height);
    }

    public static Vector2Int ShowcaseSize(int width, int height)
    {
        width = Mathf.Max(2, width); height = Mathf.Max(2, height);
        float scale = Mathf.Min(1f, 1600f / Mathf.Max(width, height));
        int w = Mathf.Max(2, Mathf.RoundToInt(width * scale));
        int h = Mathf.Max(2, Mathf.RoundToInt(height * scale));
        return new Vector2Int(w + (w & 1), h + (h & 1));
    }
}
