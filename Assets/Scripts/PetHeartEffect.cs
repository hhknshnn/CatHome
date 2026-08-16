using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class PetHeartEffect : MonoBehaviour
{
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
    [Tooltip("Multiplies the existing heart sizes without changing their animation.")]
    [SerializeField, Min(0.1f)] private float heartScaleMultiplier = 1.7f;
    [SerializeField, Range(0f, 0.15f)] private float heartScaleVariation = 0.12f;
    [SerializeField, Min(0.001f)] private float startSize = 0.055f;
    [SerializeField, Min(0.001f)] private float endSize = 0.09f;
    [SerializeField, Min(0f)] private float riseSpeed = 0.18f;
    [SerializeField, Min(0f)] private float horizontalSpread = 0.055f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Color[] HeartColors =
    {
        new Color(1f, 0.08f, 0.12f, 1f),
        new Color(1f, 0.28f, 0.38f, 1f),
        new Color(0.72f, 0.015f, 0.04f, 1f)
    };

    private Heart[] hearts;
    private bool isPlaying;
    private float emissionTimer;
    private int nextHeart;
    private Mesh runtimeMesh;
    private Material runtimeMaterial;

    private sealed class Heart
    {
        public GameObject gameObject;
        public Transform transform;
        public MeshRenderer renderer;
        public MaterialPropertyBlock properties;
        public Vector3 velocity;
        public Color color;
        public float age;
        public float scaleMultiplier;
        public bool active;
    }

    public void Play()
    {
        EnsureInitialized();
        isPlaying = true;
        emissionTimer = 0f;
        SpawnHeart();
    }

    public void Stop()
    {
        isPlaying = false;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureInitialized();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (isPlaying)
        {
            emissionTimer += Time.deltaTime;
            if (emissionTimer >= emissionInterval)
            {
                emissionTimer -= emissionInterval;
                SpawnHeart();
            }
        }

        if (hearts == null)
            return;

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < hearts.Length; i++)
        {
            Heart heart = hearts[i];
            if (!heart.active)
                continue;

            heart.age += deltaTime;
            float progress = heart.age / lifetime;
            if (progress >= 1f)
            {
                Deactivate(heart);
                continue;
            }

            heart.transform.position += heart.velocity * deltaTime;
            float size = Mathf.Lerp(startSize, endSize, SmoothStep(progress)) *
                         heartScaleMultiplier * heart.scaleMultiplier;
            heart.transform.localScale = Vector3.one * size;
            Color faded = heart.color;
            faded.a = 1f - SmoothStep(progress);
            SetHeartColor(heart, faded);
        }
    }

    private void LateUpdate()
    {
        if (hearts == null)
            return;

        Camera camera = billboardCamera != null ? billboardCamera : Camera.main;
        if (camera == null)
            return;

        Quaternion facing = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i].active)
                hearts[i].transform.rotation = facing;
        }
    }

    private void ResolveReferences()
    {
        if (spawnPoint == null)
            spawnPoint = FindDescendant(transform, "Head");
        if (billboardCamera == null)
            billboardCamera = Camera.main;
    }

    private void EnsureInitialized()
    {
        if (hearts != null && hearts.Length == poolSize)
            return;

        CleanupPool();
        ResolveReferences();

        Mesh mesh = heartMesh != null ? heartMesh : CreateRuntimeHeartMesh();
        Material material = heartMaterial != null ? heartMaterial : CreateRuntimeHeartMaterial();
        hearts = new Heart[poolSize];

        for (int i = 0; i < hearts.Length; i++)
        {
            var heartObject = new GameObject("PetHeart_" + i);
            heartObject.layer = gameObject.layer;
            heartObject.transform.SetParent(transform, false);

            MeshFilter filter = heartObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = heartObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            hearts[i] = new Heart
            {
                gameObject = heartObject,
                transform = heartObject.transform,
                renderer = renderer,
                properties = new MaterialPropertyBlock()
            };
            heartObject.SetActive(false);
        }
    }

    private void SpawnHeart()
    {
        if (hearts == null || hearts.Length == 0)
            return;

        Heart heart = hearts[nextHeart];
        nextHeart = (nextHeart + 1) % hearts.Length;
        Vector3 origin = spawnPoint != null
            ? spawnPoint.TransformPoint(headOffset)
            : transform.TransformPoint(new Vector3(0f, 0.75f, 0f));
        float side = Random.Range(-horizontalSpread, horizontalSpread);
        float depth = Random.Range(-horizontalSpread * 0.35f, horizontalSpread * 0.35f);

        heart.age = 0f;
        heart.active = true;
        heart.color = HeartColors[Random.Range(0, HeartColors.Length)];
        heart.velocity = new Vector3(side * 0.45f, riseSpeed, depth);
        heart.scaleMultiplier = Random.Range(
            1f - heartScaleVariation,
            1f + heartScaleVariation
        );
        heart.transform.position = origin + new Vector3(side, 0f, depth);
        heart.transform.localScale = Vector3.one *
                                     startSize *
                                     heartScaleMultiplier *
                                     heart.scaleMultiplier;
        SetHeartColor(heart, heart.color);
        heart.gameObject.SetActive(true);
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static Transform FindDescendant(Transform root, string targetName)
    {
        if (root.name == targetName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void SetHeartColor(Heart heart, Color color)
    {
        heart.properties.Clear();
        heart.properties.SetColor(BaseColorId, color);
        heart.properties.SetColor(ColorId, color);
        heart.renderer.SetPropertyBlock(heart.properties);
    }

    private static void Deactivate(Heart heart)
    {
        heart.active = false;
        heart.gameObject.SetActive(false);
    }

    private Mesh CreateRuntimeHeartMesh()
    {
        runtimeMesh = new Mesh { name = "Runtime_LowPolyHeart" };
        runtimeMesh.vertices = new[]
        {
            new Vector3(0f, -0.62f, 0f),
            new Vector3(-0.72f, 0.05f, 0f),
            new Vector3(-0.68f, 0.48f, 0f),
            new Vector3(-0.36f, 0.72f, 0f),
            new Vector3(0f, 0.43f, 0f),
            new Vector3(0.36f, 0.72f, 0f),
            new Vector3(0.68f, 0.48f, 0f),
            new Vector3(0.72f, 0.05f, 0f),
            Vector3.zero
        };
        runtimeMesh.triangles = new[]
        {
            8, 0, 1, 8, 1, 2, 8, 2, 3, 8, 3, 4,
            8, 4, 5, 8, 5, 6, 8, 6, 7, 8, 7, 0
        };
        runtimeMesh.RecalculateNormals();
        runtimeMesh.RecalculateBounds();
        return runtimeMesh;
    }

    private Material CreateRuntimeHeartMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        runtimeMaterial = new Material(shader) { name = "Runtime_PetHeart_Unlit" };
        ConfigureTransparentMaterial(runtimeMaterial);
        return runtimeMaterial;
    }

    public static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
            return;

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

    private void DeactivateAll()
    {
        if (hearts == null)
            return;
        for (int i = 0; i < hearts.Length; i++)
            Deactivate(hearts[i]);
    }

    private void CleanupPool()
    {
        if (hearts == null)
            return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] != null && hearts[i].gameObject != null)
                Destroy(hearts[i].gameObject);
        }
        hearts = null;
    }

    private void OnDestroy()
    {
        isPlaying = false;
        CleanupPool();
        if (runtimeMesh != null)
            Destroy(runtimeMesh);
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
