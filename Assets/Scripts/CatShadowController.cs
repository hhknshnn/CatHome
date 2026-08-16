using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Replaces only the playable cat's realtime mesh shadow with a soft, inexpensive grounding
/// shadow. Room and furniture shadow settings are never touched.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatShadowController : MonoBehaviour
{
    private const float FadeHours = 0.5f;
    private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField] private GameTimeService timeService;
    [SerializeField, Range(0f, 0.25f)] private float daytimeOpacity = 0.075f;
    [SerializeField] private Vector2 shadowSize = new Vector2(0.64f, 0.36f);
    [SerializeField, Min(0f)] private float floorOffset = 0.012f;

    private Renderer[] catRenderers;
    private MeshRenderer shadowRenderer;
    private Material shadowMaterial;
    private Mesh shadowMesh;
    private MaterialPropertyBlock propertyBlock;
    private float lastOpacity = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallOnPlayableCat()
    {
        CatMovement cat = FindAnyObjectByType<CatMovement>();
        if (cat != null && cat.GetComponent<CatShadowController>() == null)
            cat.gameObject.AddComponent<CatShadowController>();
    }

    private void OnEnable()
    {
        if (timeService == null)
            timeService = FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include);

        CacheAndDisableRealtimeCatShadows();
        CreateSoftShadow();
        UpdateShadow(true);
    }

    private void Update()
    {
        UpdateShadow(false);
    }

    private void OnDisable()
    {
        if (shadowRenderer != null)
            shadowRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        if (shadowMaterial != null)
            Destroy(shadowMaterial);
        if (shadowMesh != null)
            Destroy(shadowMesh);
    }

    private void CacheAndDisableRealtimeCatShadows()
    {
        catRenderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < catRenderers.Length; i++)
        {
            Renderer renderer = catRenderers[i];
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private void CreateSoftShadow()
    {
        if (shadowRenderer != null)
        {
            shadowRenderer.enabled = true;
            return;
        }

        Shader shader = Resources.Load<Shader>("Shaders/SoftCatShadow");
        if (shader == null)
            shader = Shader.Find("CatHome/Soft Cat Shadow");
        if (shader == null)
        {
            Debug.LogWarning("CatShadowController could not find the soft cat shadow shader.", this);
            return;
        }

        GameObject proxy = new GameObject("SoftCatShadow");
        proxy.transform.SetParent(transform, false);
        proxy.transform.localPosition = new Vector3(0f, floorOffset, 0.04f);
        proxy.transform.localRotation = Quaternion.identity;
        proxy.transform.localScale = new Vector3(shadowSize.x, 1f, shadowSize.y);

        MeshFilter filter = proxy.AddComponent<MeshFilter>();
        shadowMesh = CreateGroundQuad();
        filter.sharedMesh = shadowMesh;

        shadowRenderer = proxy.AddComponent<MeshRenderer>();
        shadowMaterial = new Material(shader)
        {
            name = "Soft Cat Shadow (Runtime)",
            hideFlags = HideFlags.DontSave
        };
        shadowRenderer.sharedMaterial = shadowMaterial;
        shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
        propertyBlock = new MaterialPropertyBlock();
    }

    private static Mesh CreateGroundQuad()
    {
        Mesh mesh = new Mesh { name = "Soft Cat Shadow Quad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3(-0.5f, 0f,  0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        mesh.UploadMeshData(true);
        return mesh;
    }

    private void UpdateShadow(bool force)
    {
        if (shadowRenderer == null || timeService == null)
            return;

        float hour = Mathf.Repeat(timeService.CurrentHour, 24f);
        float visibility = DayVisibility(hour);
        float opacity = daytimeOpacity * visibility;
        if (!force && Mathf.Abs(opacity - lastOpacity) < 0.0005f)
            return;

        lastOpacity = opacity;
        shadowRenderer.enabled = opacity > 0.001f;
        if (!shadowRenderer.enabled)
            return;

        propertyBlock.Clear();
        propertyBlock.SetColor(ColorId, new Color(0.20f, 0.13f, 0.11f, opacity));
        shadowRenderer.SetPropertyBlock(propertyBlock);
    }

    private static float DayVisibility(float hour)
    {
        if (hour < 5.5f || hour >= 20f)
            return 0f;
        if (hour < 6f)
            return Smooth01((hour - 5.5f) / FadeHours);
        if (hour < 19.5f)
            return 1f;
        return 1f - Smooth01((hour - 19.5f) / FadeHours);
    }

    private static float Smooth01(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
    }
}
