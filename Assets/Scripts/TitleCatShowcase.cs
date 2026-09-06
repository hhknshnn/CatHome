using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>A self-contained HD title scene using the game's real breed rigs.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
public sealed class TitleCatShowcase : MonoBehaviour
{
    public const int StageLayer = 30;
    public const int MinimumWidth = 1920;
    public const int MinimumHeight = 1080;
    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private Texture2D fallbackPoster;
    [SerializeField] private CatBreedCatalog catalog;
    private RawImage image;
    private GameObject stage;
    private Camera camera;
    private RenderTexture texture;
    private readonly List<Actor> actors = new List<Actor>();
    private Light[] lights;
    private readonly List<Light> maskedLights = new List<Light>();
    private readonly List<int> lightMasks = new List<int>();
    private float elapsed, nextFrame;
    private bool focused = true, reducedLastFrame, renderedFirstFrame;
    private readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
    public bool IsLive => stage != null && texture != null && actors.Count == 3;
    public RenderTexture Output => texture;
    public int ActorCount => actors.Count;
    public string HeroBreedId => actors.Count > 0 ? actors[0].entry.Id : string.Empty;

    private sealed class Actor
    {
        public CatBreedCatalog.Entry entry;
        public Transform root, visual;
        public Animator animator;
        public SkinnedMeshRenderer skin;
        public Mesh mesh;
        public readonly List<Vector3> vertices = new List<Vector3>();
        public Vector3 home;
        public int state;
    }

    private void OnEnable()
    {
        image = GetComponent<RawImage>();
        image.texture = fallbackPoster;
        image.color = Color.white;
        image.raycastTarget = false;
        if (!Application.isPlaying) return;
        CatBreedService.Changed += RebuildActors;
        CreateStage();
    }

    private void CreateStage()
    {
        if (stage != null || stagePrefab == null) return;
        image = GetComponent<RawImage>();
        catalog = catalog != null ? catalog : CatBreedCatalog.Load();
        if (catalog == null || catalog.Count < 3) return;
        stage = Instantiate(stagePrefab);
        stage.name = "Title Cat Showcase (Presentation Only)";
        stage.hideFlags = HideFlags.DontSave;
        stage.transform.position = new Vector3(2000f, 0f, 2000f);
        if (Application.isPlaying) DontDestroyOnLoad(stage);
        SetLayer(stage.transform);
        camera = stage.GetComponentInChildren<Camera>(true);
        camera.enabled = false; // Only explicit render requests; never a second gameplay camera.
        lights = stage.GetComponentsInChildren<Light>(true);
        foreach (var light in lights) light.enabled = false;
        RebuildActors();
        EnsureTexture();
        TickActors(.001f, CatRunnerProgressService.ReducedMotion);
        RenderFrame();
    }

    private void RebuildActors()
    {
        if (stage == null) return;
        foreach (var actor in actors)
        {
            actor.root.gameObject.SetActive(false);
            DestroyOwned(actor.root.gameObject);
            DestroyOwned(actor.mesh);
        }
        actors.Clear();
        var hero = catalog.Find(CatBreedService.SelectedBreedId) ?? catalog.Get(0);
        var ids = new List<CatBreedCatalog.Entry> { hero };
        foreach (string id in new[] { "domestic-shorthair", "maine-coon", "british-shorthair", "ragdoll" })
        {
            var entry = catalog.Find(id);
            if (entry != null && !ids.Contains(entry)) ids.Add(entry);
            if (ids.Count == 3) break;
        }
        for (int i = 0; ids.Count < 3 && i < catalog.Count; i++)
            if (!ids.Contains(catalog.Get(i))) ids.Add(catalog.Get(i));
        Vector3[] positions = { new Vector3(.3f, .09f, -.65f), new Vector3(-.85f, .09f, .5f), new Vector3(1.38f, .09f, .65f) };
        for (int i = 0; i < 3; i++)
        {
            var root = new GameObject("Showcase Cat " + ids[i].Id).transform;
            root.SetParent(stage.transform, false);
            root.localPosition = positions[i];
            root.localScale = Vector3.one * (i == 0 ? 1.2f : 1f);
            var visual = CatBreedVisualFactory.Create(ids[i], catalog.GameplayController, root);
            var animator = visual.GetComponentInChildren<Animator>();
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = false; // One manual animation clock, including while game time is paused.
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                skin.updateWhenOffscreen = true;
            SetLayer(root);
            actors.Add(new Actor { entry = ids[i], root = root, visual = visual.transform,
                animator = animator, skin = visual.GetComponentInChildren<SkinnedMeshRenderer>(),
                home = positions[i], mesh = new Mesh { name = "Title pose contact" } });
        }
        elapsed = 0f;
        reducedLastFrame = false;
        nextFrame = 0f;
    }

    public static Vector2Int HdSize(int width, int height)
    {
        float aspect = width > 0 && height > 0 ? width / (float)height : 16f / 9f;
        int h = Mathf.Clamp(Mathf.Max(MinimumHeight, height), MinimumHeight, 2160);
        int w = Mathf.CeilToInt(h * aspect);
        if (w < MinimumWidth) { w = MinimumWidth; h = Mathf.CeilToInt(w / aspect); }
        if (w > 3840) { h = Mathf.RoundToInt(h * (3840f / w)); w = 3840; }
        if (h > 2160) { w = Mathf.RoundToInt(w * (2160f / h)); h = 2160; }
        return new Vector2Int(w + (w & 1), h + (h & 1));
    }

    private void EnsureTexture()
    {
        Vector2Int size = HdSize(Screen.width, Screen.height);
        if (texture != null && texture.IsCreated() && texture.width == size.x && texture.height == size.y) return;
        ReleaseTexture();
        texture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32)
        { name = "Title Live Cats Full HD", antiAliasing = 4, useMipMap = false,
            filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        texture.Create();
        // Keep the HD poster until the first normal frame populates the render scene.
        image.texture = Application.isPlaying && !renderedFirstFrame ? fallbackPoster : texture;
        image.uvRect = new Rect(0f, 0f, 1f, 1f);
        camera.aspect = size.x / (float)size.y;
        request.destination = texture;
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || (!focused && renderedFirstFrame) || !IsLive) return;
        bool reduced = CatRunnerProgressService.ReducedMotion;
        bool resized = !texture.IsCreated() || texture.width != HdSize(Screen.width, Screen.height).x || texture.height != HdSize(Screen.width, Screen.height).y;
        if (reduced && reducedLastFrame && !resized) return;
        if (!reduced && Time.unscaledTime < nextFrame) return;
        float delta = reduced ? 0f : Mathf.Min(.0667f, Time.unscaledTime - nextFrame + 1f / 30f);
        nextFrame = Time.unscaledTime + 1f / 30f;
        EnsureTexture();
        TickActors(delta, reduced);
        RenderFrame();
        reducedLastFrame = reduced;
        image.texture = texture;
        renderedFirstFrame = true;
    }

    private void TickActors(float delta, bool reduced)
    {
        elapsed += delta;
        for (int i = 0; i < actors.Count; i++)
        {
            var actor = actors[i];
            float cycle = (elapsed + i * 5f) % 22f;
            string state = reduced ? "Idle" : i == 1 ? (cycle < 12f ? "Sleep" : "ActivityScratch") :
                cycle < 7f ? "Idle" : cycle < 15f ? "Pet" : cycle < 18f ? "ActivityPawSwat" : "Idle";
            int hash = Animator.StringToHash("Base Layer." + state);
            bool changed = actor.state != hash;
            if (changed || reduced)
            {
                if (actor.state == 0 || reduced) actor.animator.Play(hash, 0, i * .17f);
                else actor.animator.CrossFadeInFixedTime(hash, .28f);
                actor.state = hash;
            }
            actor.animator.SetFloat("Speed", 0f);
            actor.animator.Update(delta);
            actor.root.localPosition = actor.home;
            actor.root.localRotation = Quaternion.Euler(0f, i == 1 ? 110f : 180f + (reduced ? 0f : Mathf.Sin(elapsed * .32f + i) * 9f), 0f);
            actor.visual.localPosition = Vector3.zero;
            actor.skin.BakeMesh(actor.mesh, true);
            actor.mesh.GetVertices(actor.vertices);
            float minimum = float.PositiveInfinity;
            foreach (int vertex in actor.entry.ContactVertexIndices)
                minimum = Mathf.Min(minimum, actor.skin.transform.TransformPoint(actor.vertices[vertex]).y);
            if (!float.IsInfinity(minimum)) actor.visual.position += Vector3.up * (actor.root.position.y - minimum + .008f);
        }
    }

    private void RenderFrame()
    {
        if (camera == null || texture == null) return;
        // A room's clock-driven sun must not add another key light to the title.
        // Render requests are synchronous; restore the exact room state before its camera draws.
        var sun = RenderSettings.sun;
        bool fog = RenderSettings.fog;
        maskedLights.Clear(); lightMasks.Clear();
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.transform.IsChildOf(stage.transform) || (light.cullingMask & (1 << StageLayer)) == 0) continue;
            maskedLights.Add(light); lightMasks.Add(light.cullingMask);
            light.cullingMask &= ~(1 << StageLayer);
        }
        RenderSettings.sun = lights[0];
        RenderSettings.fog = false;
        foreach (var light in lights) light.enabled = true;
        try
        {
            if (GraphicsSettings.currentRenderPipeline != null)
                RenderPipeline.SubmitRenderRequest(camera, request);
            else { camera.targetTexture = texture; camera.Render(); camera.targetTexture = null; }
        }
        finally
        {
            foreach (var light in lights) if (light != null) light.enabled = false;
            for (int i = 0; i < maskedLights.Count; i++) if (maskedLights[i] != null) maskedLights[i].cullingMask = lightMasks[i];
            RenderSettings.sun = sun; RenderSettings.fog = fog;
        }
    }

    private static void SetLayer(Transform root)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = StageLayer;
    }
    private void OnApplicationFocus(bool value) { focused = value; reducedLastFrame = false; }
    private void OnDisable() { CatBreedService.Changed -= RebuildActors; Cleanup(); }
    private void OnDestroy() => Cleanup();
    private void ReleaseTexture()
    {
        if (texture == null) return;
        texture.Release(); DestroyOwned(texture); texture = null;
    }
    private static void DestroyOwned(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
    private void Cleanup()
    {
        foreach (var actor in actors) DestroyOwned(actor.mesh);
        actors.Clear();
        if (stage != null) { stage.SetActive(false); DestroyOwned(stage); }
        stage = null; camera = null;
        renderedFirstFrame = false;
        ReleaseTexture();
        if (image != null) image.texture = fallbackPoster;
    }
#if UNITY_EDITOR
    public void EditorConfigure(GameObject prefab, Texture2D poster, CatBreedCatalog breeds)
    { stagePrefab = prefab; fallbackPoster = poster; catalog = breeds; }
    public Texture2D EditorCapture()
    {
        image = GetComponent<RawImage>(); CreateStage();
        try
        {
            var before = RenderTexture.active;
            RenderTexture.active = texture;
            var still = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            still.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); still.Apply();
            RenderTexture.active = before; return still;
        }
        finally { Cleanup(); }
    }
#endif
}
