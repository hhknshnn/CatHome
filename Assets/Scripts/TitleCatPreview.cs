using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Off-screen 3D cat for the title hero well. Renders to a RawImage via a
/// texture-only camera (no AudioListener, not a gameplay camera). Applies the
/// live coat tint without mutating shared materials.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public sealed class TitleCatPreview : MonoBehaviour
{
    private const int PreviewLayer = 31;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private RawImage targetImage;
    private GameObject previewRoot;
    private GameObject modelClone;
    private Camera previewCamera;
    private Light previewLight;
    private RenderTexture renderTexture;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float elapsed;
    private bool cleaning;

    public bool IsLive => modelClone != null && previewCamera != null;

    public void Begin(CatMovement source)
    {
        Cleanup();
        if (source == null)
            return;
        Animator sourceAnimator = source.GetComponentInChildren<Animator>(true);
        if (sourceAnimator == null)
            return;

        targetImage = GetComponent<RawImage>();
        targetImage.raycastTarget = false;
        renderTexture = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32)
        {
            name = "TitleCatPreviewRT",
            antiAliasing = 2,
            useMipMap = false
        };
        renderTexture.Create();
        targetImage.texture = renderTexture;
        targetImage.color = Color.white;

        previewRoot = new GameObject("TitleCatPreviewRuntime");
        previewRoot.hideFlags = HideFlags.DontSave;
        Vector3 origin = new Vector3(12000f, 12000f, 12000f);
        modelClone = Instantiate(
            sourceAnimator.gameObject,
            origin,
            sourceAnimator.transform.rotation,
            previewRoot.transform);
        modelClone.name = "TitleCatVisual";
        SetLayerRecursively(modelClone, PreviewLayer);
        DisableNonVisualComponents(modelClone);
        ApplyCoatTint(modelClone);

        Animator animator = modelClone.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            int idle = Animator.StringToHash("Base Layer.Idle");
            if (animator.HasState(0, idle))
                animator.Play(idle, 0, 0f);
            animator.Update(0f);
        }

        Bounds bounds = CalculateBounds(modelClone);
        float height = Mathf.Max(0.5f, bounds.size.y);
        float distance = Mathf.Max(1.15f, height * 2.2f);
        GameObject cameraObject = new GameObject("TitlePreviewCamera", typeof(Camera));
        cameraObject.transform.SetParent(previewRoot.transform, false);
        previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.targetTexture = renderTexture;
        previewCamera.fieldOfView = 26f;
        previewCamera.nearClipPlane = 0.05f;
        previewCamera.farClipPlane = distance * 4f;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;
        AudioListener extraListener = cameraObject.GetComponent<AudioListener>();
        if (extraListener != null)
            Destroy(extraListener);
        Vector3 viewDirection = modelClone.transform.forward;
        viewDirection.y = 0f;
        if (viewDirection.sqrMagnitude < 0.001f)
            viewDirection = Vector3.forward;
        viewDirection.Normalize();
        previewCamera.transform.position =
            bounds.center + viewDirection * distance + Vector3.up * (height * 0.04f);
        previewCamera.transform.LookAt(bounds.center + Vector3.up * (height * 0.02f));

        GameObject lightObject = new GameObject("TitlePreviewLight", typeof(Light));
        lightObject.transform.SetParent(previewRoot.transform, false);
        previewLight = lightObject.GetComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;
        previewLight.color = new Color(1f, 0.92f, 0.8f);
        previewLight.cullingMask = 1 << PreviewLayer;
        previewLight.transform.rotation = Quaternion.Euler(32f, 148f, 0f);

        basePosition = modelClone.transform.position;
        baseRotation = modelClone.transform.rotation;
        elapsed = 0f;
        enabled = true;
    }

    private void Update()
    {
        if (modelClone == null)
            return;
        elapsed += Time.unscaledDeltaTime;
        if (CatRunnerProgressService.ReducedMotion)
        {
            modelClone.transform.SetPositionAndRotation(basePosition, baseRotation);
            return;
        }

        float sway = Mathf.Sin(elapsed * 0.9f) * 8f;
        modelClone.transform.position = basePosition + Vector3.up * (Mathf.Sin(elapsed * 1.4f) * 0.03f);
        modelClone.transform.rotation = baseRotation * Quaternion.Euler(0f, sway, 0f);
    }

    public void Cleanup()
    {
        if (cleaning)
            return;
        cleaning = true;
        enabled = false;
        if (previewCamera != null)
            previewCamera.targetTexture = null;
        if (targetImage == null)
            targetImage = GetComponent<RawImage>();
        if (targetImage != null && targetImage.texture == renderTexture)
            targetImage.texture = null;
        if (previewRoot != null)
            Destroy(previewRoot);
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        previewRoot = null;
        modelClone = null;
        previewCamera = null;
        previewLight = null;
        renderTexture = null;
        cleaning = false;
    }

    private static void ApplyCoatTint(GameObject root)
    {
        Color tint = CatIdentityService.CurrentTint;
        var block = new MaterialPropertyBlock();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, tint);
            block.SetColor(LegacyColorId, tint);
            renderer.SetPropertyBlock(block);
        }
    }

    private static void DisableNonVisualComponents(GameObject root)
    {
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (CharacterController controller in root.GetComponentsInChildren<CharacterController>(true))
            controller.enabled = false;
        foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
        {
            source.Stop();
            source.enabled = false;
        }
        foreach (ParticleSystem particle in root.GetComponentsInChildren<ParticleSystem>(true))
            particle.gameObject.SetActive(false);
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds result = new Bounds(root.transform.position, Vector3.one);
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;
            if (!found)
            {
                result = renderer.bounds;
                found = true;
            }
            else
            {
                result.Encapsulate(renderer.bounds);
            }
        }
        return result;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void OnDisable()
    {
        if (!cleaning && previewRoot != null)
            Cleanup();
    }

    private void OnDestroy() => Cleanup();
}
