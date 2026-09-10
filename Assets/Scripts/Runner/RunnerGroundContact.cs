using System.Collections.Generic;
using UnityEngine;

/// <summary>Reconciles the posed cat with the visible road after animation and lane lean.</summary>
[DefaultExecutionOrder(300)]
[DisallowMultipleComponent]
public sealed class RunnerGroundContact : MonoBehaviour
{
    private CatRunnerPlayer player;
    private CatRunnerTrackManager track;
    private Animator animator;
    private SkinnedMeshRenderer skin;
    private Mesh baked;
    private readonly List<Vector3> vertices = new List<Vector3>(4096);
    // Reused quarter-metre samples cover the paws through a full gallop and
    // keep per-vertex work independent of the number of pooled track objects.
    private const float SurfaceSampleSpacing = .25f;
    private readonly float[] surfaceSamples = new float[9];

    private void Awake()
    {
        player = GetComponent<CatRunnerPlayer>();
        track = transform.root.GetComponentInChildren<CatRunnerTrackManager>(true);
        baked = new Mesh { name = "Runner contact scratch mesh" };
        baked.MarkDynamic();
    }

    private void LateUpdate()
    {
        if (player == null || !player.IsRunning || Time.timeScale <= 0 || player.IsAirborne) return;
        if (animator == null || !animator.gameObject.activeInHierarchy)
        {
            animator = GetComponentInChildren<Animator>();
            skin = animator != null ? animator.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (skin != null && skin.sharedMesh != null && vertices.Capacity < skin.sharedMesh.vertexCount)
                vertices.Capacity = skin.sharedMesh.vertexCount;
        }
        if (animator == null || skin == null) return;
        // Evaluate this one grounded cat after the final Animator/lean pose.
        // The same mesh and vertex buffer serve running, crouch and recovery;
        // airborne jumps bypass both the bake and the correction above.
        var visual = player.VisualRoot;
        if (visual == null) return;
        for (int i = 0; i < surfaceSamples.Length; i++)
            surfaceSamples[i] = track != null ? track.SampleSurfaceHeightAt(
                player.LanePosition, (i - 4) * SurfaceSampleSpacing) : 0;
        float grade = Mathf.Atan2(surfaceSamples[6] - surfaceSamples[2], 1f) * Mathf.Rad2Deg;
        // Player.UpdateLane restores its authored movement rotation each frame,
        // so slope alignment never accumulates or leaks into a later jump.
        visual.rotation = Quaternion.AngleAxis(-grade, transform.right) * visual.rotation;
        skin.BakeMesh(baked, true);
        baked.GetVertices(vertices);
        float originY = transform.position.y - player.Height;
        float correction = 0;
        foreach (var vertex in vertices)
        {
            Vector3 point = skin.transform.TransformPoint(vertex);
            float sample = Mathf.Clamp((point.z - transform.position.z) / SurfaceSampleSpacing + 4,
                0, surfaceSamples.Length - 1);
            int left = Mathf.Min((int)sample, surfaceSamples.Length - 2);
            float floor = Mathf.Lerp(surfaceSamples[left], surfaceSamples[left + 1], sample - left);
            correction = Mathf.Max(correction, originY + floor + .004f - point.y);
        }
        // Never pull an already raised gallop pose down to the floor.
        visual.position += Vector3.up * Mathf.Clamp(correction, 0, .4f);
    }

    private void OnDestroy() { if (baked != null) Destroy(baked); }
}
