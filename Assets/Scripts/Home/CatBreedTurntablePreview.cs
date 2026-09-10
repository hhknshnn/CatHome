using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Live package-prefab preview with an automotive-style orbit turntable.
/// Horizontal and vertical drags orbit the camera; mouse wheel/pinch-compatible
/// scroll changes distance. It resumes a gentle spin after a short idle pause.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class CatBreedTurntablePreview : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private const int PreviewLayer = 31;
    private static readonly HashSet<int> OccupiedStages=new HashSet<int>();
    private int stageSlot=-1;

    [SerializeField] private float horizontalSensitivity = .22f;
    [SerializeField] private float verticalSensitivity = .13f;
    [SerializeField] private float idleSpinSpeed = 0f;
    [SerializeField] private float resumeSpinDelay = 1.8f;
    [SerializeField, Range(256, 1024)] private int maximumTextureSize = 1024;
    [SerializeField, Range(10, 30)] private int maximumFrameRate = 30;

    private bool reducedMotion;
    private Color tint = Color.white;
    private float nextFrame;
    private float nextQualityCheck;
    private readonly Vector3[] viewportCorners = new Vector3[4];
    private MaterialPropertyBlock tintProperties;
    private readonly List<Light> maskedLights = new List<Light>();
    private readonly List<int> lightMasks = new List<int>();
    private Light[] stageLights;
    private readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
    private RawImage target;
    private GameObject stage;
    private GameObject model;
    private Camera previewCamera;
    private RenderTexture renderTexture;
    private Material platformMaterial;
    private Material platformTopMaterial;
    private Animator previewAnimator;
    private Vector3 focus;
    private float distance = 4f;
    private float baseDistance = 4f;
    private float yaw = 18f;
    private float pitch = 16f;
    private float lastInteraction;
    private bool dragging;
    private bool previewEnabled;
    private bool frameDirty = true;

    private void Awake()
    {
        target = GetComponent<RawImage>();
        target.raycastTarget = true;
    }

    public void Show(CatBreedCatalog.Entry entry)
    {
        CatBreedCatalog catalog = CatBreedCatalog.Load();
        if (entry == null || catalog == null)
            return;

        EnsureStage();
        if (model != null) { model.SetActive(false); Destroy(model); }

        model = CatBreedVisualFactory.Create(
            entry, catalog.GameplayController, stage.transform, "TurntableCat");
        if (model == null)
            return;

        SetLayerRecursively(model, PreviewLayer);
        DisableNonVisualComponents(model);
        previewAnimator = model.GetComponentInChildren<Animator>(true);
        if (previewAnimator != null)
        {
            previewAnimator.enabled = true;
            previewAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            previewAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            int idle = Animator.StringToHash("Base Layer.Idle");
            if (previewAnimator.HasState(0, idle))
                previewAnimator.Play(idle, 0, 0f);
            previewAnimator.Update(0f);
        }

        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        model.transform.localScale = Vector3.one;
        Bounds initial = CalculateBounds(model);
        float platformTop = stage.transform.position.y + .17f;
        model.transform.position += Vector3.up * (platformTop - initial.min.y);
        Bounds bounds = CalculateBounds(model);
        focus = bounds.center + Vector3.up * bounds.size.y * .02f;

        float radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        baseDistance = Mathf.Max(1.2f, radius / Mathf.Tan(28f * .5f * Mathf.Deg2Rad) * 1.22f);
        distance = baseDistance;
        yaw = 18f;
        pitch = 16f;
        lastInteraction = Time.unscaledTime;
        SetTint(tint);
        SetReducedMotion(CatRunnerProgressService.ReducedMotion);
        SetPreviewActive(true);
        UpdateCamera();
        RenderFrame();
    }

    public void SetPreviewActive(bool value)
    {
        previewEnabled = value;
        if (previewCamera != null)
            previewCamera.enabled = false;
        if (previewAnimator != null)
            previewAnimator.enabled = value;
        nextFrame = 0f;
        frameDirty = true;
    }

    /// <summary>Optional per-screen budget; actual pixel coverage can reduce it further.</summary>
    public void ConfigureQuality(int maxTextureSize, int maxFrameRate)
    {
        maximumTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(maxTextureSize), 256, 1024);
        maximumFrameRate = Mathf.Clamp(maxFrameRate, 10, 30);
        nextQualityCheck = 0f;
        nextFrame = 0f;
        frameDirty = true;
    }

    private bool CompactPreview => renderTexture != null && renderTexture.width <= 512;
    private int PreviewFrameRate => Mathf.Min(maximumFrameRate,
        MobilePresentation.IsMobile || CompactPreview ? 15 : 30);

    private int RequiredTextureSize()
    {
        var rect = target.rectTransform;
        float pixels = Mathf.Max(rect.rect.width, rect.rect.height);
        var canvas = target.canvas;
        if (canvas != null)
        {
            rect.GetWorldCorners(viewportCorners);
            var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, viewportCorners[0]);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(uiCamera, viewportCorners[1]);
            Vector2 topRight = RectTransformUtility.WorldToScreenPoint(uiCamera, viewportCorners[2]);
            pixels = Mathf.Max(Vector2.Distance(bottomLeft, topLeft), Vector2.Distance(topLeft, topRight));
        }
        // A little oversampling keeps a small portrait sharp without a permanent
        // 1024-square, four-sample render target behind a 148-pixel card.
        int limit = Mathf.Min(maximumTextureSize, MobilePresentation.IsMobile ? 512 : 1024);
        return Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.CeilToInt(pixels * 1.25f)), 256, limit);
    }

    private void EnsureRenderTexture()
    {
        nextQualityCheck = Time.unscaledTime + .5f;
        int size = RequiredTextureSize();
        int samples = size <= 256 ? 1 : size <= 512 ? 2 : 4;
        if (renderTexture != null && renderTexture.IsCreated() && renderTexture.width == size &&
            renderTexture.antiAliasing == samples) return;
        var previous = renderTexture;
        renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32)
        {
            name = "CatBreedTurntableRT", antiAliasing = samples, useMipMap = false,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        renderTexture.Create();
        target.texture = renderTexture;
        request.destination = renderTexture;
        if (previous != null) { previous.Release(); Destroy(previous); }
        ApplyLightQuality();
        nextFrame = 0f;
        frameDirty = true;
    }

    private void ApplyLightQuality()
    {
        if (stageLights == null) return;
        for (int i = 0; i < stageLights.Length; i++)
        {
            // The fill needs no second shadow map. Keep grounding from the key.
            stageLights[i].shadows = i == 0 ? CompactPreview ? LightShadows.Hard : LightShadows.Soft : LightShadows.None;
            // URP owns the main-light atlas resolution; Light.shadowResolution
            // is a Built-in-only setting and would log a warning here.
        }
    }

    private void EnsureStage()
    {
        if (stage != null)
            return;

        target = target != null ? target : GetComponent<RawImage>();
        EnsureRenderTexture();
        target.color = Color.white;

        stage = new GameObject("CatBreedTurntableRuntime");
        stage.hideFlags = HideFlags.DontSave;
        stageSlot=0;while(OccupiedStages.Contains(stageSlot))stageSlot++;
        OccupiedStages.Add(stageSlot);
        // All preview cameras share a layer. Keep other live previews beyond
        // the 30-unit far plane so a hidden My Cat cannot appear in a welcome.
        stage.transform.position = new Vector3(2200f+64f*stageSlot, 0f, 2200f);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        platformMaterial = new Material(shader) { name = "CatTurntable_Gold" };
        SetMaterialColor(platformMaterial, new Color32(199, 213, 196, 255));
        platformTopMaterial = new Material(shader) { name = "CatTurntable_Pearl" };
        SetMaterialColor(platformTopMaterial, new Color32(255, 249, 239, 255));

        CreatePlatform("TurntableBase", new Vector3(0f, .055f, 0f),
            new Vector3(1.58f, .11f, 1.58f), platformMaterial);
        CreatePlatform("TurntableTop", new Vector3(0f, .135f, 0f),
            new Vector3(1.42f, .06f, 1.42f), platformTopMaterial);

        var cameraObject = new GameObject("CatTurntableCamera", typeof(Camera));
        cameraObject.transform.SetParent(stage.transform, false);
        previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.enabled = false;
        request.destination = renderTexture;
        previewCamera.fieldOfView = 28f;
        previewCamera.nearClipPlane = .05f;
        previewCamera.farClipPlane = 30f;
        previewCamera.allowHDR = true;
        previewCamera.allowMSAA = true;

        CreateDirectionalLight("CatTurntableKey", 1.25f,
            new Color(1f, .98f, .93f), new Vector3(32f, 145f, 0f));
        CreateDirectionalLight("CatTurntableFill", .4f,
            new Color(.55f, .91f, 1f), new Vector3(24f, -48f, 0f));
        ApplyLightQuality();
    }

    private void CreatePlatform(string name, Vector3 localPosition, Vector3 localScale,
        Material material)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = name;
        platform.transform.SetParent(stage.transform, false);
        platform.transform.localPosition = localPosition;
        platform.transform.localScale = localScale;
        platform.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = platform.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
        SetLayerRecursively(platform, PreviewLayer);
    }

    private void CreateDirectionalLight(string name, float intensity, Color color, Vector3 euler)
    {
        var lightObject = new GameObject(name, typeof(Light));
        lightObject.transform.SetParent(stage.transform, false);
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = color;
        light.cullingMask = 1 << PreviewLayer;
        light.shadows = LightShadows.Soft;
        lightObject.transform.localRotation = Quaternion.Euler(euler);
        lightObject.layer = PreviewLayer;
        light.enabled = false;
        stageLights = stage.GetComponentsInChildren<Light>(true);
    }

    private void Update()
    {
        if (!previewEnabled || previewCamera == null)
            return;
        if (Time.unscaledTime >= nextQualityCheck) EnsureRenderTexture();
        if (!reducedMotion && !dragging && Time.unscaledTime - lastInteraction > resumeSpinDelay)
            yaw += idleSpinSpeed * Time.unscaledDeltaTime;
        UpdateCamera();
        // Reduced motion has a frozen pose, so redraw only after a user change.
        if (reducedMotion && !dragging && !frameDirty) return;
        if (Time.unscaledTime >= nextFrame)
        { nextFrame = Time.unscaledTime + 1f / PreviewFrameRate; RenderFrame(); frameDirty = false; }
    }

    private void UpdateCamera()
    {
        if (previewCamera == null)
            return;
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        previewCamera.transform.position = focus + orbit * new Vector3(0f, 0f, -distance);
        previewCamera.transform.LookAt(focus, Vector3.up);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        lastInteraction = Time.unscaledTime;
        nextFrame = 0f;
        frameDirty = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        yaw -= eventData.delta.x * horizontalSensitivity;
        pitch = Mathf.Clamp(pitch + eventData.delta.y * verticalSensitivity, -5f, 50f);
        lastInteraction = Time.unscaledTime;
        frameDirty = true;
        UpdateCamera();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        lastInteraction = Time.unscaledTime;
        nextFrame = 0f;
        frameDirty = true;
    }

    public void OnScroll(PointerEventData eventData)
    {
        distance = Mathf.Clamp(
            distance - eventData.scrollDelta.y * baseDistance * .065f,
            baseDistance * .72f,
            baseDistance * 1.45f);
        lastInteraction = Time.unscaledTime;
        nextFrame = 0f;
        frameDirty = true;
        UpdateCamera();
    }

    private static void DisableNonVisualComponents(GameObject root)
    {
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (AudioSource audioSource in root.GetComponentsInChildren<AudioSource>(true))
        {
            audioSource.Stop();
            audioSource.enabled = false;
        }
        foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>(true))
            particles.gameObject.SetActive(false);
    }

    public void SetTint(Color value)
    {
        tint = value;
        if (tintProperties == null) tintProperties = new MaterialPropertyBlock();
        if (model == null) return;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.GetPropertyBlock(tintProperties);
            tintProperties.SetColor("_BaseColor", tint);
            tintProperties.SetColor("_Color", tint);
            renderer.SetPropertyBlock(tintProperties);
        }
        nextFrame = 0f;
        frameDirty = true;
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
        if (previewAnimator != null) previewAnimator.speed = value ? 0f : 1f;
        nextFrame = 0f;
        frameDirty = true;
    }

    private void RenderFrame()
    {
        if (previewCamera == null || renderTexture == null || !previewEnabled) return;
        var sun = RenderSettings.sun;
        bool fog = RenderSettings.fog;
        maskedLights.Clear(); lightMasks.Clear();
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.transform.IsChildOf(stage.transform) || (light.cullingMask & (1 << PreviewLayer)) == 0) continue;
            maskedLights.Add(light); lightMasks.Add(light.cullingMask);
            light.cullingMask &= ~(1 << PreviewLayer);
        }
        RenderSettings.sun = stageLights[0]; RenderSettings.fog = false;
        foreach (var light in stageLights) light.enabled = true;
        try
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(previewCamera, request);
            else { previewCamera.targetTexture = renderTexture; previewCamera.Render(); previewCamera.targetTexture = null; }
        }
        finally
        {
            foreach (var light in stageLights) if (light != null) light.enabled = false;
            for (int i = 0; i < maskedLights.Count; i++) if (maskedLights[i] != null) maskedLights[i].cullingMask = lightMasks[i];
            RenderSettings.sun = sun; RenderSettings.fog = fog;
        }
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        bool found = false;
        Bounds result = new Bounds(root.transform.position, Vector3.zero);
        var vertices = new List<Vector3>();
        var mesh = new Mesh();
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!renderer.enabled) continue;
            if (renderer is SkinnedMeshRenderer skin)
            {
                skin.BakeMesh(mesh, true); mesh.GetVertices(vertices);
                foreach (var vertex in vertices)
                {
                    var point = skin.transform.TransformPoint(vertex);
                    if (!found) { result = new Bounds(point, Vector3.zero); found = true; }
                    else result.Encapsulate(point);
                }
            }
            else if (!found) { result = renderer.bounds; found = true; }
            else result.Encapsulate(renderer.bounds);
        }
        Destroy(mesh);
        return result;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        material.color = color;
    }

    public void Cleanup()
    {
        previewEnabled = false;
        if (previewCamera != null)
            previewCamera.targetTexture = null;
        if (target == null)
            target = GetComponent<RawImage>();
        if (target != null && target.texture == renderTexture)
            target.texture = null;
        if (stage != null) { stage.SetActive(false); Destroy(stage); }
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (platformMaterial != null)
            Destroy(platformMaterial);
        if (platformTopMaterial != null)
            Destroy(platformTopMaterial);
        if(stageSlot>=0)OccupiedStages.Remove(stageSlot);stageSlot=-1;
        stage = null;
        model = null;
        previewCamera = null;
        previewAnimator = null;
        renderTexture = null;
        platformMaterial = null;
        platformTopMaterial = null;
        stageLights = null;
    }

    private void OnDisable() => SetPreviewActive(false);
    private void OnDestroy() => Cleanup();
}
