using UnityEngine;

/// <summary>Measured usable support around an activity contact point (local X/Z).</summary>
[DisallowMultipleComponent]
public sealed class CatActivitySurface : MonoBehaviour
{
    [SerializeField] private Vector2 size = Vector2.one;
    [SerializeField] private bool overridePose;
    [SerializeField] private CatActivityPose preferredPose = CatActivityPose.Sleep;
    [SerializeField] private bool alignAlongSurface;
    public Vector2 Size => size;
    public bool AlignAlongSurface => alignAlongSurface;
    public CatActivityPose ResolvePose(CatActivityPose requested) => overridePose ? preferredPose : requested;
#if UNITY_EDITOR
    public void EditorConfigure(Vector2 value)
    {
        size = new Vector2(Mathf.Max(.1f, value.x), Mathf.Max(.1f, value.y));
    }
    public void EditorConfigurePose(CatActivityPose pose, bool align)
    {
        overridePose = true;
        preferredPose = pose;
        alignAlongSurface = align;
    }
#endif
}
