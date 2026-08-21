using UnityEngine;

/// <summary>
/// Slow horizontal drift for title-screen clouds. Reduced motion freezes in place.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitleCloudDrift : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float range = 48f;

    private RectTransform rect;
    private Vector2 home;
    private float phase;

    private void Awake()
    {
        rect = transform as RectTransform;
        if (rect != null)
            home = rect.anchoredPosition;
        phase = speed * 0.13f;
    }

    private void Update()
    {
        if (rect == null || CatRunnerProgressService.ReducedMotion)
            return;
        phase += Time.unscaledDeltaTime * speed * 0.02f;
        rect.anchoredPosition = home + new Vector2(Mathf.Sin(phase) * range, Mathf.Cos(phase * 0.5f) * 8f);
    }

#if UNITY_EDITOR
    public void EditorConfigure(float driftSpeed, float driftRange)
    {
        speed = driftSpeed;
        range = driftRange;
    }
#endif
}
