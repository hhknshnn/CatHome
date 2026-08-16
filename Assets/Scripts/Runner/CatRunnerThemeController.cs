using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Slowly blends the runner sky and key light between playful palettes. The
/// transition is intentionally longer than one track segment so the player feels
/// a changing journey without seeing a hard theme cut.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatRunnerThemeController : MonoBehaviour
{
    [SerializeField] private CatRunnerGameController game;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Light keyLight;
    [SerializeField, Min(20f)] private float secondsPerTheme = 45f;
    [SerializeField] private Color[] skyColors =
    {
        new Color32(76, 196, 231, 255),
        new Color32(211, 116, 228, 255),
        new Color32(72, 103, 214, 255)
    };
    [SerializeField] private Color[] lightColors =
    {
        new Color32(255, 231, 184, 255),
        new Color32(255, 205, 236, 255),
        new Color32(196, 224, 255, 255)
    };
    [SerializeField] private Color[] fogColors =
    {
        new Color32(150, 225, 240, 255),
        new Color32(232, 159, 229, 255),
        new Color32(112, 151, 226, 255)
    };
    [SerializeField] private Color[] ambientColors =
    {
        new Color32(199, 235, 244, 255),
        new Color32(246, 193, 231, 255),
        new Color32(163, 193, 240, 255)
    };

    private bool capturedRenderSettings;
    private bool previousFog;
    private FogMode previousFogMode;
    private float previousFogStart;
    private float previousFogEnd;
    private Color previousFogColor;
    private UnityEngine.Rendering.AmbientMode previousAmbientMode;
    private Color previousAmbientSky;
    private Color previousAmbientEquator;
    private Color previousAmbientGround;
    private float previousAmbientIntensity;
    private bool themeInitialized;

    private void Awake()
    {
        if (game == null)
            game = FindAnyObjectByType<CatRunnerGameController>(FindObjectsInactive.Include);
        if (targetCamera == null)
            targetCamera = GetComponentInChildren<Camera>(true);
    }

    private void Update()
    {
        if (!themeInitialized)
        {
            if (SceneManager.GetActiveScene() != gameObject.scene)
                return;
            CaptureRenderSettings();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 52f;
            RenderSettings.fogEndDistance = 112f;
            themeInitialized = true;
        }

        if (skyColors == null || skyColors.Length == 0)
            return;

        float elapsed = game != null ? game.ElapsedSeconds : 0f;
        float phase = Mathf.Max(0f, elapsed) / Mathf.Max(20f, secondsPerTheme);
        int from = Mathf.FloorToInt(phase) % skyColors.Length;
        int to = (from + 1) % skyColors.Length;
        float blend = Smooth(Mathf.Repeat(phase, 1f));

        if (targetCamera != null)
            targetCamera.backgroundColor = Color.Lerp(skyColors[from], skyColors[to], blend);

        if (fogColors != null && fogColors.Length > 0)
        {
            Color fromFog = fogColors[from % fogColors.Length];
            Color toFog = fogColors[to % fogColors.Length];
            RenderSettings.fogColor = Color.Lerp(fromFog, toFog, blend);
        }
        if (ambientColors != null && ambientColors.Length > 0)
        {
            Color fromAmbient = ambientColors[from % ambientColors.Length];
            Color toAmbient = ambientColors[to % ambientColors.Length];
            Color ambient = Color.Lerp(fromAmbient, toAmbient, blend);
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = Color.Lerp(ambient, Color.white, .16f);
            RenderSettings.ambientGroundColor = Color.Lerp(ambient, new Color32(75, 48, 115, 255), .48f);
        }

        if (keyLight != null && lightColors != null && lightColors.Length > 0)
        {
            Color fromLight = lightColors[from % lightColors.Length];
            Color toLight = lightColors[to % lightColors.Length];
            keyLight.color = Color.Lerp(fromLight, toLight, blend);
            keyLight.intensity = Mathf.Lerp(1.15f, 1.35f, 0.5f + 0.5f * Mathf.Sin(phase * Mathf.PI));
        }
    }

    private static float Smooth(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private void CaptureRenderSettings()
    {
        if (capturedRenderSettings)
            return;
        capturedRenderSettings = true;
        previousFog = RenderSettings.fog;
        previousFogMode = RenderSettings.fogMode;
        previousFogStart = RenderSettings.fogStartDistance;
        previousFogEnd = RenderSettings.fogEndDistance;
        previousFogColor = RenderSettings.fogColor;
        previousAmbientMode = RenderSettings.ambientMode;
        previousAmbientSky = RenderSettings.ambientSkyColor;
        previousAmbientEquator = RenderSettings.ambientEquatorColor;
        previousAmbientGround = RenderSettings.ambientGroundColor;
        previousAmbientIntensity = RenderSettings.ambientIntensity;
    }

    private void OnDisable()
    {
        if (!capturedRenderSettings ||
            SceneManager.GetActiveScene() != gameObject.scene)
            return;
        RenderSettings.fog = previousFog;
        RenderSettings.fogMode = previousFogMode;
        RenderSettings.fogStartDistance = previousFogStart;
        RenderSettings.fogEndDistance = previousFogEnd;
        RenderSettings.fogColor = previousFogColor;
        RenderSettings.ambientMode = previousAmbientMode;
        RenderSettings.ambientSkyColor = previousAmbientSky;
        RenderSettings.ambientEquatorColor = previousAmbientEquator;
        RenderSettings.ambientGroundColor = previousAmbientGround;
        RenderSettings.ambientIntensity = previousAmbientIntensity;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CatRunnerGameController controller,
        Camera camera,
        Light light)
    {
        game = controller;
        targetCamera = camera;
        keyLight = light;
        secondsPerTheme = 45f;
    }
#endif
}
