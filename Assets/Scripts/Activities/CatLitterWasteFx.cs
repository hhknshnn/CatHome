using UnityEngine;

/// <summary>Three small droppings or one wet patch, owned by this tray routine.</summary>
[DisallowMultipleComponent]
public sealed class CatLitterWasteFx : MonoBehaviour
{
    const int Segments = 10, Rings = 6, Count = 3;
    const int PerPiece = (Segments + 1) * (Rings + 1);
    LitterDigActivity owner;
    CatLitterSandSurface sand;
    GameObject visual;
    Mesh mesh;
    Material material;
    MeshRenderer wasteRenderer;
    Vector3[] vertices;
    readonly Vector3[] origins = new Vector3[Count];
    Vector3 hole;
    float sampledTime, cover;
    public bool IsActive => owner != null;
    public bool IsSolid { get; private set; }
    public int Emitted { get; private set; }
    public int VisibleCount { get; private set; }
    public Vector3 Hole => hole;
    public Mesh RuntimeMesh => mesh;

    public void Begin(LitterDigActivity activity, CatLitterSandSurface surface, bool solid)
    {
        if (owner != null && owner != activity) return;
        Stop(activity); owner = activity; sand = surface; IsSolid = solid;
        sampledTime = cover = 0f; Emitted = VisibleCount = 0;
        visual = new GameObject("Litter temporary waste") { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
        visual.transform.SetParent(transform, false);
        wasteRenderer = visual.AddComponent<MeshRenderer>();
        wasteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { name = "Litter temporary waste", hideFlags = HideFlags.DontSave };
        Color color = solid ? new Color(.29f, .105f, .038f) : new Color(.49f, .36f, .10f);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .16f);
        wasteRenderer.sharedMaterial = material; wasteRenderer.enabled = false;
        mesh = new Mesh { name = "Litter temporary waste", hideFlags = HideFlags.DontSave }; mesh.MarkDynamic();
        vertices = new Vector3[PerPiece * Count];
        var triangles = new int[Count * Rings * Segments * 6]; int index = 0;
        for (int piece = 0; piece < Count; piece++)
        for (int ring = 0; ring < Rings; ring++)
        for (int segment = 0; segment < Segments; segment++)
        {
            int a = piece * PerPiece + ring * (Segments + 1) + segment, b = a + Segments + 1;
            triangles[index++] = a; triangles[index++] = a + 1; triangles[index++] = b;
            triangles[index++] = b; triangles[index++] = a + 1; triangles[index++] = b + 1;
        }
        mesh.vertices = vertices; mesh.triangles = triangles;
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
    }

    public void Sample(LitterDigActivity activity, Vector3 target, Vector3 pelvis, float time)
    {
        if (owner != activity || mesh == null) return;
        hole = target; sampledTime = time;
        int wanted = IsSolid ? Mathf.Clamp(Mathf.FloorToInt((time - .72f) / .34f) + 1, 0, Count) : time >= .72f ? 1 : 0;
        while (Emitted < wanted)
        {
            origins[Emitted] = pelvis + Vector3.down * .06f;
            Emitted++;
        }
        Draw();
    }

    public void Cover(LitterDigActivity activity, float progress)
    {
        if (owner != activity) return;
        cover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.12f, .88f, progress));
        Draw();
    }

    void Draw()
    {
        if (mesh == null || sand == null) return;
        VisibleCount = 0;
        for (int piece = 0; piece < Count; piece++)
        {
            bool visible = piece < Emitted && cover < .999f && (IsSolid || piece == 0);
            float radius = visible ? (IsSolid ? .024f : .060f) * (1f - cover) : 0f;
            Vector3 landing = hole + (IsSolid ? new Vector3((piece - 1) * .029f, 0f, piece % 2 == 0 ? .019f : -.019f) : Vector3.zero);
            landing = sand.ClampPoint(landing, .03f);
            landing.y = sand.SampleHeight(landing) + (IsSolid ? radius * .75f : .003f);
            float age = Mathf.Max(0f, sampledTime - (.72f + piece * .34f));
            Vector3 center = landing;
            if (IsSolid && visible && !CatRunnerProgressService.ReducedMotion && cover <= 0f)
            {
                center = Vector3.Lerp(origins[piece], landing, Mathf.Clamp01(age / .27f));
                center.y = Mathf.Max(landing.y, origins[piece].y - 4.9f * age * age);
            }
            if (visible) VisibleCount++;
            for (int ring = 0; ring <= Rings; ring++)
            for (int segment = 0; segment <= Segments; segment++)
            {
                float latitude = ring * Mathf.PI / Rings, longitude = segment * Mathf.PI * 2f / Segments;
                Vector3 offset = new Vector3(Mathf.Sin(latitude) * Mathf.Cos(longitude),
                    Mathf.Cos(latitude) * (IsSolid ? .83f : .035f), Mathf.Sin(latitude) * Mathf.Sin(longitude));
                Vector3 world = center + offset * radius;
                if (!IsSolid) world.y = sand.SampleHeight(world) + .003f + offset.y * radius;
                vertices[piece * PerPiece + ring * (Segments + 1) + segment] = visual.transform.InverseTransformPoint(world);
            }
        }
        mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        wasteRenderer.enabled = VisibleCount > 0;
    }

    public void Stop(LitterDigActivity activity)
    {
        if (owner != null && owner != activity) return;
        if (wasteRenderer != null) wasteRenderer.enabled = false;
        if (visual != null) Destroy(visual);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        owner = null; sand = null; visual = null; mesh = null; material = null; wasteRenderer = null;
        vertices = null; VisibleCount = 0;
    }

    void OnDisable() { if (owner != null) Stop(owner); }
    void OnDestroy() { if (owner != null) Stop(owner); }
}
