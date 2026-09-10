using UnityEngine;

/// <summary>A reversible, genuinely excavated sand surface. Never edits an imported mesh.</summary>
[DisallowMultipleComponent]
public sealed class CatLitterSandSurface : MonoBehaviour
{
    public const float MaximumDepth = .038f;
    public const float HoleRadius = .145f;
    const int Columns = 41, Rows = 33, GrainCapacity = 24;
    [SerializeField] MeshFilter sand;
    [SerializeField] Vector3 localCenter;
    [SerializeField] Vector2 localSize;
    LitterDigActivity owner;
    Mesh original, excavated, grainMesh;
    Vector3[] flat, vertices, grainVertices;
    MeshRenderer grainRenderer;
    GameObject grainObject;
    readonly Vector3[] origins = new Vector3[GrainCapacity], velocities = new Vector3[GrainCapacity];
    readonly float[] ages = new float[GrainCapacity];
    int nextGrain;
    Vector2 paletteUv;

    public bool IsConfigured => sand != null && sand.sharedMesh != null && sand.GetComponent<MeshRenderer>() != null &&
        localSize.x > .01f && localSize.y > .01f;
    public bool IsActive => owner != null;
    public MeshFilter Sand => sand;
    public Mesh OriginalMesh => original;
    public float Depth { get; private set; }
    public int ActiveGrains { get; private set; }
    public int EmittedGrains { get; private set; }
    public Vector3 HolePosition { get; private set; }
    public float PlaneHeight => sand != null ? sand.transform.TransformPoint(localCenter).y : transform.position.y;

    public bool Begin(LitterDigActivity activity)
    {
        if (!IsConfigured || activity == null || (owner != null && owner != activity)) return false;
        if (owner == activity) return true;
        owner = activity; original = sand.sharedMesh;
        Vector2[] sourceUv = original.uv; paletteUv = sourceUv.Length > 0 ? sourceUv[0] : Vector2.zero;
        BuildGrid(); sand.sharedMesh = excavated;
        HolePosition = sand.transform.TransformPoint(localCenter);
        Depth = 0f; EmittedGrains = 0; nextGrain = 0;
        return true;
    }

    void BuildGrid()
    {
        flat = new Vector3[Columns * Rows]; vertices = new Vector3[flat.Length];
        var uv = new Vector2[flat.Length]; var triangles = new int[(Columns - 1) * (Rows - 1) * 6];
        int triangle = 0;
        for (int z = 0; z < Rows; z++) for (int x = 0; x < Columns; x++)
        {
            int index = z * Columns + x;
            flat[index] = localCenter + new Vector3((x / (float)(Columns - 1) - .5f) * localSize.x, 0f,
                (z / (float)(Rows - 1) - .5f) * localSize.y);
            vertices[index] = flat[index]; uv[index] = paletteUv;
            if (x == Columns - 1 || z == Rows - 1) continue;
            triangles[triangle++] = index; triangles[triangle++] = index + Columns; triangles[triangle++] = index + 1;
            triangles[triangle++] = index + 1; triangles[triangle++] = index + Columns; triangles[triangle++] = index + Columns + 1;
        }
        excavated = new Mesh { name = "Litter sand (temporary excavation)", hideFlags = HideFlags.DontSave };
        excavated.MarkDynamic(); excavated.vertices = vertices; excavated.uv = uv; excavated.triangles = triangles;
        excavated.RecalculateNormals(); excavated.RecalculateBounds();
    }

    public Vector3 ClampPoint(Vector3 world, float margin = .025f)
    {
        if (sand == null) return world;
        Vector3 point = sand.transform.InverseTransformPoint(world);
        float mx = margin / Mathf.Max(.001f, sand.transform.TransformVector(Vector3.right).magnitude);
        float mz = margin / Mathf.Max(.001f, sand.transform.TransformVector(Vector3.forward).magnitude);
        point.x = Mathf.Clamp(point.x, localCenter.x - localSize.x * .5f + mx, localCenter.x + localSize.x * .5f - mx);
        point.z = Mathf.Clamp(point.z, localCenter.z - localSize.y * .5f + mz, localCenter.z + localSize.y * .5f - mz);
        point.y = localCenter.y;
        return sand.transform.TransformPoint(point);
    }

    public bool Contains(Vector3 world, float margin = .015f) =>
        Vector2.Distance(new Vector2(world.x, world.z), new Vector2(ClampPoint(world, margin).x, ClampPoint(world, margin).z)) < .001f;

    public void Excavate(LitterDigActivity activity, Vector3 center, float amount)
    {
        if (owner != activity || excavated == null) return;
        HolePosition = ClampPoint(center, HoleRadius * .75f); Depth = MaximumDepth * Mathf.Clamp01(amount);
        float verticalScale = Mathf.Max(.001f, sand.transform.TransformVector(Vector3.up).y);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 world = sand.transform.TransformPoint(flat[i]);
            vertices[i] = flat[i] + Vector3.up * (HeightOffset(world) / verticalScale);
        }
        excavated.vertices = vertices; excavated.RecalculateNormals(); excavated.RecalculateBounds();
    }

    float HeightOffset(Vector3 world)
    {
        float distance = Vector2.Distance(new Vector2(world.x, world.z), new Vector2(HolePosition.x, HolePosition.z));
        float bowl = Mathf.Clamp01(1f - distance * distance / (HoleRadius * HoleRadius));
        float rim = Mathf.Clamp01(1f - Mathf.Abs(distance - HoleRadius * 1.05f) / (HoleRadius * .35f));
        return -Depth * bowl * bowl + Depth * .24f * rim * rim;
    }

    public float SampleHeight(Vector3 world) => PlaneHeight + HeightOffset(world);

    public void Scatter(LitterDigActivity activity, Vector3 contact, Vector3 direction)
    {
        if (owner != activity || !Contains(contact)) return;
        EnsureGrains(); direction.y = 0f; direction.Normalize();
        int count = CatRunnerProgressService.ReducedMotion ? 2 : 5;
        for (int n = 0; n < count; n++)
        {
            int index = nextGrain++ % GrainCapacity; float spread = (n - (count - 1) * .5f) * 12f;
            Vector3 scatter = Quaternion.AngleAxis(spread, Vector3.up) * direction;
            origins[index] = contact; origins[index].y = SampleHeight(contact) + .012f;
            velocities[index] = scatter * (.16f + (n % 3) * .055f) + Vector3.up * (.32f + (n % 2) * .09f);
            ages[index] = .0001f; EmittedGrains++;
        }
        DrawGrains(0f);
    }

    void EnsureGrains()
    {
        if (grainObject != null) return;
        grainObject = new GameObject("Litter scattered grains") { hideFlags = HideFlags.DontSave, layer = gameObject.layer };
        grainObject.transform.SetParent(transform, false);
        grainRenderer = grainObject.AddComponent<MeshRenderer>();
        grainRenderer.sharedMaterial = sand.GetComponent<MeshRenderer>().sharedMaterial;
        grainRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var filter = grainObject.AddComponent<MeshFilter>();
        grainMesh = new Mesh { name = "Litter grains (temporary)", hideFlags = HideFlags.DontSave }; grainMesh.MarkDynamic();
        grainVertices = new Vector3[GrainCapacity * 6]; var uv = new Vector2[grainVertices.Length];
        // Outward winding for the six vertices (+Y,-Y,+X,-X,+Z,-Z).
        int[] unit = { 0,4,2, 0,3,4, 0,5,3, 0,2,5, 1,2,4, 1,4,3, 1,3,5, 1,5,2 };
        var triangles = new int[GrainCapacity * unit.Length];
        for (int i = 0; i < uv.Length; i++) uv[i] = paletteUv;
        for (int n = 0; n < GrainCapacity; n++) for (int j = 0; j < unit.Length; j++) triangles[n * unit.Length + j] = n * 6 + unit[j];
        grainMesh.vertices = grainVertices; grainMesh.uv = uv; grainMesh.triangles = triangles; filter.sharedMesh = grainMesh;
    }

    void Update()
    {
        if (owner != null && (!owner.IsRunning || !owner.isActiveAndEnabled)) { Stop(owner); return; }
        if (owner != null && grainMesh != null && Time.deltaTime > 0f) DrawGrains(Time.deltaTime);
    }

    void DrawGrains(float delta)
    {
        ActiveGrains = 0;
        for (int n = 0; n < GrainCapacity; n++)
        {
            if (ages[n] > 0f) ages[n] += delta;
            if (ages[n] > .48f) ages[n] = 0f;
            bool active = ages[n] > 0f; float t = ages[n];
            Vector3 world = origins[n] + velocities[n] * t + Vector3.down * (1.1f * t * t);
            Vector3 bounded = ClampPoint(world, .015f); world.x = bounded.x; world.z = bounded.z;
            world.y = Mathf.Max(world.y, SampleHeight(world) + .006f);
            Vector3 center = grainObject.transform.InverseTransformPoint(world);
            float size = active ? .009f * Mathf.Clamp01((.48f - t) / .1f) : 0f;
            if (active) ActiveGrains++;
            for (int j = 0; j < 6; j++)
            {
                Vector3 axis = j < 2 ? Vector3.up : j < 4 ? Vector3.right : Vector3.forward;
                Vector3 offset = grainObject.transform.InverseTransformVector(axis * ((j % 2 == 0 ? 1f : -1f) * size));
                grainVertices[n * 6 + j] = center + offset;
            }
        }
        grainMesh.vertices = grainVertices; grainMesh.RecalculateNormals(); grainMesh.RecalculateBounds();
        grainRenderer.enabled = ActiveGrains > 0;
    }

    public void Stop(LitterDigActivity activity)
    {
        if (owner == null || owner != activity) return;
        if (sand != null && sand.sharedMesh == excavated) sand.sharedMesh = original;
        owner = null; original = null; Depth = 0f; ActiveGrains = 0;
        System.Array.Clear(ages, 0, ages.Length);
        if (grainRenderer != null) grainRenderer.enabled = false;
        Release(excavated); Release(grainMesh); Release(grainObject);
        excavated = grainMesh = null; grainObject = null; grainRenderer = null;
        flat = vertices = grainVertices = null;
    }

    static void Release(Object value) { if (value == null) return; if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }
    void OnDisable() { if (owner != null) Stop(owner); }
    void OnDestroy() { if (owner != null) Stop(owner); }

#if UNITY_EDITOR
    public void EditorConfigure(MeshFilter surface, Vector3 center, Vector2 size)
    { sand = surface; localCenter = center; localSize = size; }
#endif
}
