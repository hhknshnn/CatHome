using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Bounded mobile rendering; HUD resolution and gameplay dimensions stay native.</summary>
public static class MobilePresentation
{
    public static bool IsMobile => Application.isMobilePlatform;
    public static int FrameRateForMemory(int memoryMb) => memoryMb > 0 && memoryMb <= 4096 ? 30 : 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
        RenderPipelineManager.beginCameraRendering -= ConfigureCamera;
        if (!IsMobile) return;
        Application.targetFrameRate = FrameRateForMemory(SystemInfo.systemMemorySize);
        RenderPipelineManager.beginCameraRendering += ConfigureCamera;
    }

    private static void ConfigureCamera(ScriptableRenderContext context, Camera camera)
    {
        if (!IsMobile || camera == null) return;
        // Mobile uses 2x MSAA; a second full-screen SMAA pass adds avoidable cost.
        if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            data.antialiasing = AntialiasingMode.None;
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
