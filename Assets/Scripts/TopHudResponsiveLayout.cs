using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps the three existing need indicators centred as one visual unit and
/// scales their spacing only when the safe width would collide with the
/// clock/light/shop/menu cluster. It does not own or alter need behaviour.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class TopHudResponsiveLayout : MonoBehaviour
{
    public const float TopBarTop = -22f;
    public const float TopBarHeight = 88f;
    public const float TopBarCenterY = TopBarTop - TopBarHeight * 0.5f;
    public const float NeedBarWidth = 260f;
    public const float NeedSpacing = 284f;
    public const float NeedHalfWidth = NeedBarWidth * 0.5f;
    private const float RightControlsWidth = 438f;
    private const float MinimumGap = 16f;

    private RectTransform hunger;
    private RectTransform thirst;
    private RectTransform energy;
    private Canvas canvas;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private float lastCanvasScale = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeToSceneLoads()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAfterInitialScene() => Install();

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install();

    private static void Install()
    {
        RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include);
        RectTransform hunger = null;
        RectTransform thirst = null;
        RectTransform energy = null;

        for (int i = 0; i < rects.Length; i++)
        {
            switch (rects[i].name)
            {
                // The current UI scene calls this object FoodBar. HungerUI is
                // kept as a compatibility alias for older scenes/prefabs.
                case "HungerUI":
                case "FoodBar": hunger = rects[i]; break;
                case "ThirstUI": thirst = rects[i]; break;
                case "EnergyUI": energy = rects[i]; break;
            }
        }

        if (hunger == null || thirst == null || energy == null ||
            hunger.parent != thirst.parent || hunger.parent != energy.parent)
        {
            return;
        }

        TopHudResponsiveLayout layout =
            hunger.parent.GetComponent<TopHudResponsiveLayout>();
        if (layout == null)
            layout = hunger.parent.gameObject.AddComponent<TopHudResponsiveLayout>();
        layout.Configure(hunger, thirst, energy);
    }

    private void Configure(RectTransform hunger, RectTransform thirst, RectTransform energy)
    {
        this.hunger = hunger;
        this.thirst = thirst;
        this.energy = energy;
        canvas = hunger.GetComponentInParent<Canvas>();
        ApplyLayout(true);
    }

#if UNITY_EDITOR
    // The saved-home preview owns this temporary instance and restores its rects.
    public void ConfigureEditorPreview(RectTransform food, RectTransform water, RectTransform rest)
        => Configure(food, water, rest);
    public void RefreshEditorPreview() => ApplyLayout(true);
#endif

    private void Update()
    {
        ApplyLayout(false);
    }

    private void ApplyLayout(bool force)
    {
        if (GetComponentInParent<StorybookHudLayout>() != null) return;
        if (hunger == null || thirst == null || energy == null || canvas == null)
            return;

        Rect safe = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        float scaleFactor = Mathf.Max(0.001f, canvas.scaleFactor);
        if (!force && safe == lastSafeArea && screenSize == lastScreenSize &&
            Mathf.Abs(scaleFactor - lastCanvasScale) < 0.0001f)
        {
            return;
        }

        lastSafeArea = safe;
        lastScreenSize = screenSize;
        lastCanvasScale = scaleFactor;

        float safeWidth = safe.width / scaleFactor;
        float safeCenterX =
            (safe.center.x - Screen.width * 0.5f) / scaleFactor;

        float maximumHalfWidth =
            Mathf.Max(NeedHalfWidth, safeWidth * 0.5f - RightControlsWidth - MinimumGap);
        float groupHalfWidth = NeedSpacing + NeedHalfWidth;
        float layoutScale = Mathf.Clamp01(maximumHalfWidth / groupHalfWidth);
        bool secondRow = safeWidth < 1760f;
        if (secondRow)
            layoutScale = Mathf.Min(1f, (safeWidth - 68f) / (groupHalfWidth * 2f));
        float targetScreenY = safe.yMax + (secondRow ? -170f : TopBarCenterY) * scaleFactor;
        PositionNeed(hunger, safeCenterX - NeedSpacing * layoutScale, targetScreenY, layoutScale);
        PositionNeed(thirst, safeCenterX, targetScreenY, layoutScale);
        PositionNeed(energy, safeCenterX + NeedSpacing * layoutScale, targetScreenY, layoutScale);
    }

    private void PositionNeed(RectTransform need, float x, float targetScreenY, float scale)
    {
        // Keep the authored 68-unit top-bar height even if a parent lift or
        // safe-area scale tries to squash the capsules.
        need.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, TopBarHeight);

        Vector2 position = need.anchoredPosition;
        position.x = x;

        // PetTutorialHint reparents all needs beneath a presentation root and
        // lifts that root. Convert the common screen-space centre line back into
        // this actual parent's local coordinates so that reparenting cannot
        // introduce a visible vertical offset.
        RectTransform parent = need.parent as RectTransform;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;
        if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent,
                new Vector2(Screen.safeArea.center.x, targetScreenY),
                camera,
                out Vector2 localPoint))
        {
            float anchorY = Mathf.Lerp(
                parent.rect.yMin,
                parent.rect.yMax,
                (need.anchorMin.y + need.anchorMax.y) * 0.5f);
            position.y = localPoint.y - anchorY;
        }
        else
        {
            position.y = TopBarCenterY;
        }

        need.anchoredPosition = position;
        need.localScale = new Vector3(scale, scale, 1f);
    }
}
