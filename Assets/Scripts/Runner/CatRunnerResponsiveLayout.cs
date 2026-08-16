using UnityEngine;

/// <summary>
/// Keeps Runner cards and the gameplay HUD inside the current safe area while
/// preserving the authored 1920x1080 proportions.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CatRunnerResponsiveLayout : MonoBehaviour
{
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform topHud;
    [SerializeField] private RectTransform welcomeCard;
    [SerializeField] private RectTransform resultsCard;
    [SerializeField] private RectTransform pauseCard;
    [SerializeField] private RectTransform tutorialCard;
    [SerializeField, Min(0f)] private float edgeMargin = 28f;

    private Vector2 lastSafeSize;

    private void OnEnable()
    {
        Apply();
    }

    private void LateUpdate()
    {
        if (safeArea == null)
            return;
        Vector2 size = safeArea.rect.size;
        if ((size - lastSafeSize).sqrMagnitude > 0.25f)
            Apply();
    }

    public void Apply()
    {
        if (safeArea == null)
            return;
        Vector2 available = safeArea.rect.size - Vector2.one * edgeMargin * 2f;
        if (available.x <= 1f || available.y <= 1f)
            return;

        Fit(welcomeCard, available);
        Fit(resultsCard, available);
        Fit(pauseCard, available);
        Fit(tutorialCard, available);
        FitWidth(topHud, available.x);
        lastSafeSize = safeArea.rect.size;
    }

    private static void Fit(RectTransform target, Vector2 available)
    {
        if (target == null)
            return;
        Vector2 size = target.sizeDelta;
        if (size.x <= 1f || size.y <= 1f)
            return;
        float scale = Mathf.Min(1f, available.x / size.x, available.y / size.y);
        target.localScale = Vector3.one * Mathf.Max(0.35f, scale);
    }

    private static void FitWidth(RectTransform target, float availableWidth)
    {
        if (target == null || target.rect.width <= 1f)
            return;
        float scale = Mathf.Min(1f, availableWidth / target.rect.width);
        target.localScale = Vector3.one * Mathf.Max(0.55f, scale);
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        RectTransform safeRoot,
        RectTransform hud,
        RectTransform welcome,
        RectTransform results,
        RectTransform pause,
        RectTransform tutorial)
    {
        safeArea = safeRoot;
        topHud = hud;
        welcomeCard = welcome;
        resultsCard = results;
        pauseCard = pause;
        tutorialCard = tutorial;
        edgeMargin = 28f;
        Apply();
    }
#endif
}
