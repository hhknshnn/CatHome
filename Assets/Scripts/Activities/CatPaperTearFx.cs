using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Contact-driven paper scraps in one bounded, room-owned mesh. No physics objects or gameplay rewards.</summary>
[DefaultExecutionOrder(1050)]
[DisallowMultipleComponent]
public sealed class CatPaperTearFx : MonoBehaviour
{
    public const int MaximumPieces = 8;
    public const float Lifetime = 1.65f;
    struct Piece { public bool active, quiet; public float age, seed; public Vector3 origin, drift; }
    readonly Piece[] pieces = new Piece[MaximumPieces];
    readonly Vector3[] vertices = new Vector3[MaximumPieces * 9];
    readonly Color32[] colors = new Color32[MaximumPieces * 9];
    readonly int[] triangles = new int[MaximumPieces * 48];
    PaperSpinActivity owner;
    Transform visualRoot;
    Mesh mesh;
    Material material;
    int serial, lastStroke, vertexCount, triangleCount;
    float elapsed;
    bool needsBackFaces;

    public int ActivePieces { get; private set; }
    public int TotalTornPieces { get; private set; }
    public float Elapsed => elapsed;
    public Transform VisualRoot => visualRoot;
    public bool QuietMotion => CatRunnerProgressService.ReducedMotion ||
        (MobilePresentation.IsMobile && MobilePresentation.FrameRateForMemory(SystemInfo.systemMemorySize) == 30);

    internal void Begin(PaperSpinActivity activity)
    {
        Stop();
        if (activity == null || !activity.IsRunning || activity.gameObject != gameObject || !isActiveAndEnabled) return;
        owner = activity; serial = lastStroke = 0; TotalTornPieces = 0;
    }
    internal bool Tear(PaperSpinActivity activity, int stroke, Vector3 surface, Vector3 outward)
    {
        if (owner != activity || owner == null || !owner.IsRunning || !owner.isActiveAndEnabled ||
            Time.deltaTime <= 0f || stroke <= lastStroke || !EnsureVisual()) return false;
        lastStroke = stroke;
        int count = QuietMotion ? 1 : 2;
        Vector3 normal = Vector3.ProjectOnPlane(outward, Vector3.up).normalized;
        if (normal.sqrMagnitude < .01f) normal = transform.forward;
        Vector3 side = Vector3.Cross(Vector3.up, normal);
        for (int piece = 0; piece < count; piece++)
        {
            int slot = -1;
            for (int i = 0; i < pieces.Length; i++) if (!pieces[i].active) { slot = i; break; }
            if (slot < 0) break;
            float seed = ++serial;
            pieces[slot] = new Piece { active = true, quiet = QuietMotion, seed = seed,
                origin = surface + side * ((piece == 0 ? -.5f : .5f) * .028f),
                drift = normal * (.07f + (serial % 3) * .025f) + side * ((serial % 2 == 0 ? 1 : -1) * .09f) };
            TotalTornPieces++;
        }
        Draw(); return true;
    }
    void LateUpdate()
    {
        if (owner == null) return;
        if (!owner.isActiveAndEnabled || !owner.IsRunning || HomeUiFlow.IsMiniGameVisible) { Stop(); return; }
        if (Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        for (int i = 0; i < pieces.Length; i++)
        {
            if (!pieces[i].active) continue;
            pieces[i].age += Time.deltaTime;
            if (pieces[i].age >= Lifetime) pieces[i].active = false;
        }
        Draw();
    }
    public Vector3 PieceCenter(int index)
    {
        if (index < 0 || index >= pieces.Length || !pieces[index].active) return Vector3.zero;
        return Center(pieces[index]);
    }
    Vector3 Center(Piece piece)
    {
        float t = piece.age;
        Vector3 center = piece.origin + piece.drift * (piece.quiet ? t * .2f : t);
        center.y -= (piece.quiet ? .24f : .32f) * t * t;
        center.y = Mathf.Max(transform.position.y + .025f, center.y);
        if (!piece.quiet && center.y > transform.position.y + .04f)
            center += Vector3.Cross(Vector3.up, piece.drift).normalized * (Mathf.Sin(t * 8f + piece.seed) * .025f * Mathf.Min(1, t * 5));
        return center;
    }
    void Draw()
    {
        if (visualRoot == null || mesh == null) return;
        vertexCount = triangleCount = ActivePieces = 0;
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece piece = pieces[i]; if (!piece.active) continue;
            ActivePieces++;
            Vector3 center = Center(piece);
            float spread = Mathf.SmoothStep(.3f, 1f, piece.age / .16f);
            float yaw = piece.seed * 1.27f + (piece.quiet ? 0f : Mathf.Sin(piece.age * 5f + piece.seed) * .32f);
            Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            Vector3 down = new Vector3(-right.z * .32f, -.95f, right.x * .32f).normalized;
            float landing = 1f - Mathf.InverseLerp(transform.position.y + .03f, transform.position.y + .10f, center.y);
            down = Vector3.Slerp(down, Vector3.Cross(Vector3.up, right), landing).normalized;
            float alpha = 1f - Mathf.InverseLerp(Lifetime - .4f, Lifetime, piece.age);
            int start = vertexCount;
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                {
                    float x = (column - 1) * .037f * spread;
                    float y = (row - 1) * .046f * spread;
                    float curl = piece.quiet ? .002f : Mathf.Sin(piece.age * 8f + row * 1.1f + piece.seed) * .012f;
                    // The lower edge is slightly ragged, like a short torn sheet.
                    if (row == 2) y += (column == 1 ? -.008f : .003f) * spread;
                    Vector3 point = center + right * x + down * y + Vector3.Cross(right, down) * curl;
                    vertices[vertexCount] = visualRoot.InverseTransformPoint(point);
                    byte shade = (byte)(row == 1 ? 250 : 236);
                    colors[vertexCount++] = new Color32(shade, shade, (byte)(shade - 8), (byte)(alpha * 255));
                }
            for (int row = 0; row < 2; row++) for (int column = 0; column < 2; column++)
            {
                int a = start + row * 3 + column, b = a + 1, c = a + 3, d = c + 1;
                Triangle(a, c, b); Triangle(b, c, d);
                if (needsBackFaces) { Triangle(b, c, a); Triangle(d, c, b); }
            }
        }
        mesh.Clear(); mesh.SetVertices(vertices, 0, vertexCount); mesh.SetColors(colors, 0, vertexCount);
        mesh.SetTriangles(triangles, 0, triangleCount, 0, false); mesh.RecalculateBounds();
        visualRoot.gameObject.SetActive(ActivePieces > 0);
    }
    void Triangle(int a, int b, int c) { triangles[triangleCount++] = a; triangles[triangleCount++] = b; triangles[triangleCount++] = c; }
    bool EnsureVisual()
    {
        if (visualRoot != null) return true;
        Shader shader = Shader.Find("CatHome/Window Sun Beam");
        // WindowSunBeam.shader explicitly uses Cull Off: each triangle already
        // renders from both sides. Preserve the previous two-sided geometry only
        // for the fallback instead of assuming an unrelated shader's cull state.
        needsBackFaces = shader == null;
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return false;
        var visual = new GameObject("PaperTearSheets", typeof(MeshFilter), typeof(MeshRenderer));
        visual.layer = gameObject.layer; visualRoot = visual.transform; visualRoot.SetParent(transform, false);
        mesh = new Mesh { name = "Runtime_PaperTearSheets", hideFlags = HideFlags.DontSave }; mesh.MarkDynamic();
        material = new Material(shader) { name = "Runtime_PaperTearSheets", hideFlags = HideFlags.DontSave, renderQueue = (int)RenderQueue.Transparent };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = visual.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return true;
    }
    public void Stop()
    {
        owner = null; elapsed = 0; ActivePieces = 0;
        for (int i = 0; i < pieces.Length; i++) pieces[i].active = false;
        if (visualRoot != null) visualRoot.gameObject.SetActive(false);
    }
    void OnDisable() => Stop();
    void OnDestroy()
    {
        Stop();
        if (visualRoot != null) Destroy(visualRoot.gameObject);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
    }
}
