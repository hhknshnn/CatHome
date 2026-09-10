using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class PetHeartEffect : MonoBehaviour
{
    // Older prefabs/builders retain their serialized bindings. Runtime uses the vector pool.
    [Header("References")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Mesh heartMesh;
    [SerializeField] private Material heartMaterial;
    [SerializeField] private Camera billboardCamera;
    [Header("Emission")]
    [SerializeField, Range(4, 7)] private int poolSize = 7;
    [SerializeField, Min(0.05f)] private float emissionInterval = 0.24f;
    [SerializeField, Min(0.1f)] private float lifetime = 1.35f;
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 0.22f, 0f);
    [SerializeField, Min(0.1f)] private float heartScaleMultiplier = 1.7f;
    [SerializeField, Range(0f, 0.15f)] private float heartScaleVariation = 0.12f;
    [SerializeField, Min(0.001f)] private float startSize = 0.055f;
    [SerializeField, Min(0.001f)] private float endSize = 0.09f;
    [SerializeField, Min(0f)] private float riseSpeed = 0.18f;
    [SerializeField, Min(0f)] private float horizontalSpread = 0.055f;

    public const float PresentationInterval = .45f;
    public const float PresentationLifetime = 1.65f;
    private const int VisualPoolSize = 4;
    private readonly CatCareFxGraphic[] hearts = new CatCareFxGraphic[VisualPoolSize];
    private readonly float[] ages = new float[VisualPoolSize];
    private CatCareFxCanvas presentation;
    private bool isPlaying;
    private bool quietMotion;
    private float emissionTimer;
    private int nextHeart;

    public bool IsPlaying => isPlaying;
    public int ActiveHeartCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < hearts.Length; i++)
                if (hearts[i] != null && hearts[i].gameObject.activeSelf) count++;
            return count;
        }
    }

    public void RebindSpawnPoint(Transform replacement)
    {
        spawnPoint = replacement;
        if (presentation != null) presentation.RebindHead();
    }

    public void Play()
    {
        if (!isActiveAndEnabled) return;
        EnsurePool();
        if (isPlaying) return;
        isPlaying = true;
        emissionTimer = 0f;
        quietMotion = presentation.QuietMotion;
        if (presentation.TryHeadPosition(out Vector2 origin)) SpawnHeart(origin);
    }

    // Existing hearts finish their short fade when petting ends.
    public void Stop() => isPlaying = false;

    private void EnsurePool()
    {
        if (presentation == null) presentation = CatCareFxCanvas.For(gameObject);
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null) continue;
            hearts[i] = presentation.CreateGraphic("PetHeart_" + i, CatCareFxGraphic.Shape.Heart, 46f);
            ages[i] = -1f;
        }
    }

    private void LateUpdate()
    {
        if (presentation == null) return;
        if (!presentation.TryHeadPosition(out Vector2 origin))
        {
            DeactivateAll();
            emissionTimer = 0f;
            return;
        }
        bool quiet = presentation.QuietMotion;
        if (quietMotion != quiet)
        {
            quietMotion = quiet;
            DeactivateAll();
            emissionTimer = quiet ? PresentationLifetime : PresentationInterval;
        }
        float delta = Time.deltaTime;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (ages[i] < 0f || hearts[i] == null) continue;
            ages[i] += delta;
            if (ages[i] >= PresentationLifetime)
            {
                ages[i] = -1f;
                hearts[i].gameObject.SetActive(false);
            }
            else Present(i, origin);
        }
        if (!isPlaying || delta <= 0f) return;
        emissionTimer += delta;
        float interval = quietMotion ? PresentationLifetime : PresentationInterval;
        if (emissionTimer >= interval)
        {
            emissionTimer %= interval;
            SpawnHeart(origin);
        }
    }

    private void SpawnHeart(Vector2 origin)
    {
        int index = quietMotion ? 0 : nextHeart++ % hearts.Length;
        ages[index] = 0f;
        hearts[index].gameObject.SetActive(true);
        Present(index, origin);
    }

    private void Present(int index, Vector2 origin)
    {
        float progress = Mathf.Clamp01(ages[index] / PresentationLifetime);
        float rise = quietMotion ? 0f : 62f * Mathf.SmoothStep(0f, 1f, progress);
        float side = quietMotion ? 0f : (index % 2 == 0 ? -1f : 1f) * 19f;
        hearts[index].rectTransform.anchoredPosition = origin + new Vector2(side, 44f + rise);
        float scale = quietMotion ? 1f : Mathf.Lerp(.9f, 1f, Mathf.SmoothStep(0f, 1f, progress * 5f));
        hearts[index].rectTransform.localScale = Vector3.one * scale;
        float alpha = Mathf.SmoothStep(0f, 1f, progress / .12f) *
                      (1f - Mathf.SmoothStep(0f, 1f, (progress - .58f) / .42f));
        hearts[index].canvasRenderer.SetAlpha(alpha);
    }

    private void DeactivateAll()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            ages[i] = -1f;
            if (hearts[i] != null) hearts[i].gameObject.SetActive(false);
        }
    }

    public static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null) return;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private void OnDisable()
    {
        isPlaying = false;
        DeactivateAll();
    }

    private void OnDestroy()
    {
        if (presentation == null) return;
        for (int i = 0; i < hearts.Length; i++) presentation.Release(hearts[i]);
    }
}
