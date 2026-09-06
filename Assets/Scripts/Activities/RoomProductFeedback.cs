using System.Collections.Generic;
using UnityEngine;

/// <summary>A gentle material response on every used product, without moving its collision or contact surfaces.</summary>
[DisallowMultipleComponent]
public sealed class RoomProductFeedback : MonoBehaviour
{
    private struct Surface
    {
        public Renderer renderer;
        public int slot;
        public Color color;
        public MaterialPropertyBlock original, animated;
    }

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private readonly List<Surface> surfaces = new List<Surface>();
    private CatActivity activity;
    private bool responding;
    private float elapsed;
    public bool IsResponding => responding;

    private void Awake()
    {
        activity = GetComponent<CatActivity>();
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.name.EndsWith("_PremiumModel") && renderer.name != "HammockBed" && renderer.name != "SwingSeat") continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].HasProperty(BaseColor)) continue;
                var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original, i);
                surfaces.Add(new Surface { renderer = renderer, slot = i,
                    color = materials[i].GetColor(BaseColor), original = original, animated = new MaterialPropertyBlock() });
            }
        }
    }

    private void LateUpdate()
    {
        if (activity == null || !activity.IsRunning)
        {
            if (responding) Restore();
            return;
        }
        responding = true;
        elapsed += Time.deltaTime;
        float amount = CatRunnerProgressService.ReducedMotion ? .07f : .055f + .055f * Mathf.Sin(elapsed * 3f);
        foreach (var surface in surfaces)
        {
            if (surface.renderer == null) continue;
            surface.renderer.GetPropertyBlock(surface.animated, surface.slot);
            surface.animated.SetColor(BaseColor, Color.Lerp(surface.color, Color.white, amount));
            surface.renderer.SetPropertyBlock(surface.animated, surface.slot);
        }
    }

    private void Restore()
    {
        foreach (var surface in surfaces)
            if (surface.renderer != null) surface.renderer.SetPropertyBlock(surface.original, surface.slot);
        responding = false;
        elapsed = 0f;
    }

    private void OnDisable() { if (responding) Restore(); }
}
