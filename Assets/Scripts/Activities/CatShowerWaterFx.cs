using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// A bounded, room-owned water/soap mesh. The rinse owns its phases; this class
/// adds no colliders, input, timers outside the activity, or gameplay rewards.
/// </summary>
[DefaultExecutionOrder(1050)]
[DisallowMultipleComponent]
public sealed class CatShowerWaterFx : MonoBehaviour
{
    public enum Phase { Off, Rinsing, ShakeOff }
    private const int NormalWaterCount = 18;
    private const int QuietWaterCount = 6;
    private const int NormalFoamCount = 12;
    private const int QuietFoamCount = 6;
    private const int CircleSegments = 16;
    private static readonly float[] BodyFoamAlong = { .25f, .38f, .46f, .63f, .77f, .86f };
    private static readonly float[] BodyFoamSide = { .60f, -.90f, .90f, -.55f, .45f, -.80f };
    private static readonly float[] BodyFoamSize = { .96f, .78f, 1f, .87f, .93f, .74f };
    private readonly Vector3[] vertices = new Vector3[1024];
    private readonly Color32[] colors = new Color32[1024];
    private readonly int[] triangles = new int[4096];
    private readonly Vector3[] foamCenters = new Vector3[NormalFoamCount];
    private readonly Vector3[] scatterOrigins = new Vector3[NormalFoamCount];
    private readonly Vector3[] scatterDirections = new Vector3[NormalFoamCount];
    private readonly float[] foamRadii = new float[NormalFoamCount];
    private readonly bool[] bodyFoam = new bool[NormalFoamCount];
    private readonly Vector3[] bodyFoamLocal = new Vector3[6];
    private readonly float[] bodyFoamRadius = new float[6];
    private readonly List<Vector3> skinVertices = new List<Vector3>(4096);
    private readonly List<int> skinTriangles = new List<int>(12288);
    private readonly List<int> submeshTriangles = new List<int>(12288);
    private ShowerRinseActivity owner;
    private CatMovement cat;
    private Transform stand, outlet, hips, shoulders;
    private Transform visualRoot;
    private Mesh mesh;
    private Mesh bodySampleMesh;
    private SkinnedMeshRenderer bodySkin;
    private bool bodySurfaceReady;
    private Material material;
    private Camera gameplayCamera;
    private float elapsed, scatterDuration, fixtureScale = 1f;
    private int vertexCount, triangleCount, scatterCount;
    private bool lowMemoryMobile;

    public Phase CurrentPhase { get; private set; }
    public float Elapsed => elapsed;
    public int WaterCount { get; private set; }
    public int FoamCount { get; private set; }
    public Transform VisualRoot => visualRoot;
    public bool QuietMotion => lowMemoryMobile || CatRunnerProgressService.ReducedMotion;

    public void BeginRinse(ShowerRinseActivity activity, CatMovement actor, Transform tray, Transform source)
    {
        if (!isActiveAndEnabled || activity == null || !activity.IsRunning ||
            !activity.BelongsTo(actor) || actor == null || tray == null || activity.gameObject != gameObject)
            return;
        Stop();
        owner = activity;
        cat = actor;
        stand = tray;
        outlet = source;
        var stamp = GetComponent<RoomProductScaleStamp>();
        fixtureScale = (stamp != null ? stamp.AppliedScale : 1f) * Mathf.Abs(transform.lossyScale.y);
        fixtureScale = Mathf.Clamp(fixtureScale, .4f, 2f);
        lowMemoryMobile = MobilePresentation.IsMobile && MobilePresentation.FrameRateForMemory(SystemInfo.systemMemorySize) == 30;
        if (!EnsureVisual()) { Stop(); return; }
        CurrentPhase = Phase.Rinsing;
        visualRoot.gameObject.SetActive(true);
        Draw();
    }

    public void Scatter(float duration)
    {
        if (CurrentPhase != Phase.Rinsing || cat == null) return;
        // Capture the last attached soap positions once. The shake never keeps
        // pulling detached bubbles back onto the moving cat.
        scatterCount = FoamCount;
        for (int i = 0; i < scatterCount; i++)
        {
            scatterOrigins[i] = foamCenters[i];
            float angle = i * 2.399963f;
            scatterDirections[i] = new Vector3(Mathf.Cos(angle), .45f + (i % 3) * .1f, Mathf.Sin(angle));
        }
        CurrentPhase = Phase.ShakeOff;
        elapsed = 0f;
        scatterDuration = Mathf.Clamp(duration, .2f, 1.2f);
        Draw(); // The falling water disappears synchronously at the phase boundary.
    }

    public void Stop()
    {
        CurrentPhase = Phase.Off;
        elapsed = 0f;
        WaterCount = FoamCount = scatterCount = 0;
        if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        owner = null;
        cat = null;
        stand = outlet = hips = shoulders = null;
        bodySkin = null;
        bodySurfaceReady = false;
        skinVertices.Clear();
        skinTriangles.Clear();
    }

    private void LateUpdate()
    {
        if (CurrentPhase == Phase.Off) return;
        if (owner == null || !owner.isActiveAndEnabled || !owner.IsRunning || cat == null ||
            !cat.isActiveAndEnabled || !owner.BelongsTo(cat) || stand == null || HomeUiFlow.IsMiniGameVisible)
        {
            Stop();
            return;
        }
        if (Time.deltaTime <= 0f) return;
        elapsed += Time.deltaTime;
        if (CurrentPhase == Phase.ShakeOff && elapsed >= scatterDuration) { Stop(); return; }
        Draw();
    }

    private void Draw()
    {
        if (visualRoot == null || mesh == null || cat == null || stand == null) return;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled || gameplayCamera.gameObject.scene != gameObject.scene)
            gameplayCamera = Camera.main;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled || gameplayCamera.gameObject.scene != gameObject.scene)
        {
            visualRoot.gameObject.SetActive(false);
            return;
        }
        visualRoot.gameObject.SetActive(true);
        vertexCount = triangleCount = 0;
        Vector3 right = gameplayCamera.transform.right;
        Vector3 up = gameplayCamera.transform.up;
        bool quiet = QuietMotion;
        WaterCount = CurrentPhase == Phase.Rinsing ? (quiet ? QuietWaterCount : NormalWaterCount) : 0;
        Vector3 source = outlet != null ? outlet.position : stand.position + Vector3.up * (1.68f * fixtureScale);
        for (int i = 0; i < WaterCount; i++)
        {
            float phase = quiet ? .14f + i * .135f : Mathf.Repeat(elapsed * 1.35f + i * .618034f, 1f);
            float angle = i * 2.399963f;
            float radius = (.065f + (i % 4) * .025f) * fixtureScale;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Vector3 top = source + offset;
            Vector3 bottom = stand.position + offset * 1.25f + Vector3.up * .015f;
            Vector3 direction = bottom - top;
            Vector3 dropTop = Vector3.Lerp(top, bottom, phase);
            Vector3 dropBottom = dropTop + direction.normalized * Mathf.Min(.13f * fixtureScale, direction.magnitude * (1f - phase));
            float opacity = quiet ? .35f : Mathf.Sin(phase * Mathf.PI) * .66f;
            WaterRibbon(dropTop, dropBottom, right * (.006f * fixtureScale), opacity);
        }
        if (CurrentPhase == Phase.Rinsing)
        {
            RebindBody();
            // The sit cross-fade needs real frames before its skinned surface
            // is measured. Sampling at BeginRinse would still capture the walk pose.
            if (!bodySurfaceReady && elapsed >= .35f) MeasureBodyFoam(right, up);
            FoamCount = quiet ? QuietFoamCount : NormalFoamCount;
            float appear = Mathf.SmoothStep(0f, 1f, (elapsed - .2f) / .85f);
            if (appear <= 0f || !bodySurfaceReady) FoamCount = 0;
            for (int i = 0; i < FoamCount; i++)
            {
                int sample = quiet && i >= 3 ? i + 3 : i;
                bodyFoam[i] = sample < 6;
                foamCenters[i] = FoamPosition(sample);
                float radius = bodyFoam[i] ? bodyFoamRadius[sample] : .029f + (sample % 3) * .006f;
                foamRadii[i] = radius * Mathf.Lerp(.55f, 1f, appear);
                Bubble(foamCenters[i], foamRadii[i], appear, right, up);
            }
        }
        else
        {
            FoamCount = quiet ? Mathf.Min(scatterCount, QuietFoamCount) : scatterCount;
            float t = Mathf.Clamp01(elapsed / scatterDuration);
            float ease = 1f - (1f - t) * (1f - t);
            for (int i = 0; i < FoamCount; i++)
            {
                Vector3 offset = Vector3.zero;
                if (!quiet && bodyFoam[i])
                {
                    offset = scatterDirections[i] * (.25f * ease);
                    offset.y -= .17f * t * t;
                }
                foamCenters[i] = scatterOrigins[i] + offset;
                Bubble(foamCenters[i], foamRadii[i] * Mathf.Lerp(1f, .4f, t), 1f - Mathf.SmoothStep(0f, 1f, t), right, up);
            }
        }
        mesh.Clear(true);
        mesh.SetVertices(vertices, 0, vertexCount);
        mesh.SetColors(colors, 0, vertexCount);
        mesh.SetTriangles(triangles, 0, triangleCount, 0, true);
    }

    private void RebindBody()
    {
        if (hips != null && shoulders != null && hips.IsChildOf(cat.transform) && shoulders.IsChildOf(cat.transform) &&
            bodySkin != null && bodySkin.transform.IsChildOf(cat.transform)) return;
        hips = FindBone(cat.transform, "DEF-spine");
        shoulders = FindBone(cat.transform, "DEF-spine.003");
        bodySkin = null;
        float largestBody = 0f;
        // One scan per owner/breed, not per frame. Ignore eye/teeth meshes when
        // choosing the body surface that actually hides the old bone-centred soap.
        foreach (var skin in cat.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!skin.enabled || skin.sharedMesh == null) continue;
            float size = skin.sharedMesh.bounds.size.sqrMagnitude;
            if (size <= largestBody) continue;
            largestBody = size;
            bodySkin = skin;
        }
        bodySurfaceReady = false;
    }

    private Vector3 FoamPosition(int index)
    {
        if (index < 6)
            return cat.transform.TransformPoint(bodyFoamLocal[index]);
        float angle = (index - 6) * Mathf.PI / 3f;
        return stand.position + new Vector3(Mathf.Cos(angle) * .23f, .018f, Mathf.Sin(angle) * .20f) * fixtureScale;
    }

    private void MeasureBodyFoam(Vector3 right, Vector3 up)
    {
        if (bodySkin == null || hips == null || shoulders == null) return;
        if (bodySampleMesh == null)
            bodySampleMesh = new Mesh { name = "Runtime_ShowerBodySurface", hideFlags = HideFlags.DontSave };
        bodySkin.BakeMesh(bodySampleMesh);
        bodySampleMesh.GetVertices(skinVertices);
        if (skinVertices.Count == 0) return;
        for (int i = 0; i < skinVertices.Count; i++)
            skinVertices[i] = bodySkin.transform.TransformPoint(skinVertices[i]);
        skinTriangles.Clear();
        for (int submesh = 0; submesh < bodySampleMesh.subMeshCount; submesh++)
        {
            bodySampleMesh.GetTriangles(submeshTriangles, submesh);
            skinTriangles.AddRange(submeshTriangles);
        }
        Vector3 cameraForward = gameplayCamera.transform.forward;
        Vector3 cameraPosition = gameplayCamera.transform.position;
        for (int i = 0; i < 6; i++)
        {
            // Uneven spacing and overlapping sizes form a little lather
            // cluster instead of two matching columns on the cat's chest.
            Vector3 bone = Vector3.Lerp(hips.position, shoulders.position, BodyFoamAlong[i]) + Vector3.up * .025f;
            float width = BodyWidthAt(bone, right, up, cameraForward);
            float side = Mathf.Clamp(width * .16f, .018f, .025f) * BodyFoamSide[i];
            Vector3 probe = bone + right * side;
            float radius = Mathf.Clamp(width * .17f, .024f, .036f) * BodyFoamSize[i];
            bodyFoamRadius[i] = radius;
            if (!TrySkinSurface(probe, out Vector3 hit))
                hit = ClosestProjectedVertex(probe, right, up);
            float depth = Vector3.Dot(hit - cameraPosition, cameraForward);
            float nearDepth = depth;
            // The small disc must clear the fur across its area, not just at
            // its centre. Sample four neighbours once, then keep the attached
            // local point; the animation loop never bakes or raycasts the skin.
            for (int edge = 0; edge < 4; edge++)
            {
                Vector3 offset = (edge < 2 ? right : up) * (edge % 2 == 0 ? radius * .65f : -radius * .65f);
                if (TrySkinSurface(probe + offset, out Vector3 nearby))
                    nearDepth = Mathf.Min(nearDepth, Vector3.Dot(nearby - cameraPosition, cameraForward));
            }
            float clearance = Mathf.Clamp(depth - nearDepth + .009f, .009f, .035f);
            Vector3 ray = (hit - cameraPosition).normalized;
            Vector3 visible = hit - ray * (clearance / Mathf.Max(.1f, Vector3.Dot(ray, cameraForward)));
            bodyFoamLocal[i] = cat.transform.InverseTransformPoint(visible);
        }
        bodySurfaceReady = true;
    }

    private float BodyWidthAt(Vector3 point, Vector3 right, Vector3 up, Vector3 forward)
    {
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        for (int i = 0; i < skinVertices.Count; i++)
        {
            Vector3 offset = skinVertices[i] - point;
            if (Mathf.Abs(Vector3.Dot(offset, up)) > .065f || Mathf.Abs(Vector3.Dot(offset, forward)) > .17f) continue;
            float side = Vector3.Dot(offset, right);
            min = Mathf.Min(min, side);
            max = Mathf.Max(max, side);
        }
        return min <= max ? max - min : .16f;
    }

    private Vector3 ClosestProjectedVertex(Vector3 point, Vector3 right, Vector3 up)
    {
        float closest = float.PositiveInfinity;
        Vector3 result = skinVertices[0];
        for (int i = 0; i < skinVertices.Count; i++)
        {
            Vector3 offset = skinVertices[i] - point;
            float x = Vector3.Dot(offset, right), y = Vector3.Dot(offset, up);
            float distance = x * x + y * y;
            if (distance >= closest) continue;
            closest = distance;
            result = skinVertices[i];
        }
        return result;
    }

    private bool TrySkinSurface(Vector3 target, out Vector3 point)
    {
        Vector3 origin = gameplayCamera.transform.position;
        Vector3 direction = (target - origin).normalized;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i + 2 < skinTriangles.Count; i += 3)
        {
            Vector3 a = skinVertices[skinTriangles[i]];
            Vector3 ab = skinVertices[skinTriangles[i + 1]] - a;
            Vector3 ac = skinVertices[skinTriangles[i + 2]] - a;
            Vector3 cross = Vector3.Cross(direction, ac);
            float determinant = Vector3.Dot(ab, cross);
            if (Mathf.Abs(determinant) < .0000001f) continue;
            float inverse = 1f / determinant;
            Vector3 fromA = origin - a;
            float u = Vector3.Dot(fromA, cross) * inverse;
            if (u < 0f || u > 1f) continue;
            Vector3 q = Vector3.Cross(fromA, ab);
            float v = Vector3.Dot(direction, q) * inverse;
            if (v < 0f || u + v > 1f) continue;
            float distance = Vector3.Dot(ac, q) * inverse;
            if (distance > 0f && distance < nearest) nearest = distance;
        }
        point = float.IsPositiveInfinity(nearest) ? Vector3.zero : origin + direction * nearest;
        return !float.IsPositiveInfinity(nearest);
    }

    private void WaterRibbon(Vector3 top, Vector3 bottom, Vector3 halfWidth, float alpha)
    {
        int first = vertexCount;
        Add(top - halfWidth, new Color( .69f, .93f, 1f, alpha * .35f));
        Add(top + halfWidth, new Color( .69f, .93f, 1f, alpha * .35f));
        Add(bottom + halfWidth, new Color(.88f, .98f, 1f, alpha));
        Add(bottom - halfWidth, new Color(.88f, .98f, 1f, alpha));
        Triangle(first, first + 1, first + 2);
        Triangle(first, first + 2, first + 3);
    }

    private void Bubble(Vector3 center, float radius, float alpha, Vector3 right, Vector3 up)
    {
        int first = vertexCount;
        Add(center, new Color(.98f, 1f, 1f, .80f * alpha));
        for (int i = 0; i < CircleSegments; i++)
        {
            float angle = i * (Mathf.PI * 2f / CircleSegments);
            Vector3 direction = right * Mathf.Cos(angle) + up * Mathf.Sin(angle);
            Color rim = Color.Lerp(new Color(.66f, .88f, .98f, .63f * alpha), new Color(.98f, .91f, 1f, .72f * alpha), (Mathf.Sin(angle) + 1f) * .5f);
            Add(center + direction * radius, rim);
            rim.a = 0f;
            Add(center + direction * (radius * 1.10f), rim);
        }
        for (int i = 0; i < CircleSegments; i++)
        {
            int a = first + 1 + i * 2, b = first + 1 + (i + 1) % CircleSegments * 2;
            Triangle(first, a, b);
            Triangle(a, a + 1, b);
            Triangle(a + 1, b + 1, b);
        }
        int glint = vertexCount;
        Vector3 highlight = center + (up * .32f - right * .28f) * radius;
        Add(highlight, new Color(1f, 1f, 1f, .95f * alpha));
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4f;
            Add(highlight + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * (radius * .20f), new Color(1f, 1f, 1f, .45f * alpha));
        }
        for (int i = 0; i < 8; i++) Triangle(glint, glint + 1 + i, glint + 1 + (i + 1) % 8);
    }

    private void Add(Vector3 point, Color color)
    {
        vertices[vertexCount] = visualRoot.InverseTransformPoint(point);
        colors[vertexCount++] = color;
    }

    private void Triangle(int a, int b, int c)
    {
        triangles[triangleCount++] = a;
        triangles[triangleCount++] = b;
        triangles[triangleCount++] = c;
    }

    private bool EnsureVisual()
    {
        if (visualRoot != null) return true;
        // This vertex-color, depth-tested transparent shader is already used by
        // the authored room windows and included in the existing player content.
        Shader shader = Shader.Find("CatHome/Window Sun Beam") ?? Shader.Find("Sprites/Default");
        if (shader == null) return false;
        var visual = new GameObject("ShowerWaterAndFoam", typeof(MeshFilter), typeof(MeshRenderer));
        visual.layer = gameObject.layer;
        visualRoot = visual.transform;
        visualRoot.SetParent(transform, false);
        mesh = new Mesh { name = "Runtime_ShowerWaterAndFoam", hideFlags = HideFlags.DontSave };
        mesh.MarkDynamic();
        material = new Material(shader) { name = "Runtime_ShowerWaterAndFoam", renderQueue = (int)RenderQueue.Transparent, hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        var renderer = visual.GetComponent<MeshRenderer>();
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        visual.SetActive(false);
        return true;
    }

    private static Transform FindBone(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            Transform found = FindBone(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void OnDisable() => Stop();
    private void OnDestroy()
    {
        Stop();
        if (visualRoot != null) Destroy(visualRoot.gameObject);
        if (mesh != null) Destroy(mesh);
        if (bodySampleMesh != null) Destroy(bodySampleMesh);
        if (material != null) Destroy(material);
    }
}
