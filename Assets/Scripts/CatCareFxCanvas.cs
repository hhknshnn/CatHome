using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One pooled, non-interactive overlay per cat, owned by the cat's room scene.</summary>
[DisallowMultipleComponent]
public sealed class CatCareFxCanvas : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform canvasRect;
    private Transform head;
    private SkinnedMeshRenderer visual;
    private Camera gameplayCamera;
    private bool lowMemoryMobile;
    private int graphicCount;

    public Canvas Overlay => canvas;
    public Transform Head => head;
    public bool QuietMotion => lowMemoryMobile || CatRunnerProgressService.ReducedMotion;

    public static CatCareFxCanvas For(GameObject owner) =>
        owner.GetComponent<CatCareFxCanvas>() ?? owner.AddComponent<CatCareFxCanvas>();

    private void Awake()
    {
        lowMemoryMobile = MobilePresentation.IsMobile &&
                          MobilePresentation.FrameRateForMemory(SystemInfo.systemMemorySize) == 30;
    }

    public CatCareFxGraphic CreateGraphic(string itemName, CatCareFxGraphic.Shape shape, float size)
    {
        EnsureCanvas();
        var item = new GameObject(itemName, typeof(RectTransform), typeof(CanvasRenderer), typeof(CatCareFxGraphic));
        var graphic = item.GetComponent<CatCareFxGraphic>();
        graphic.rectTransform.SetParent(canvasRect, false);
        graphic.rectTransform.anchorMin = graphic.rectTransform.anchorMax = new Vector2(.5f, .5f);
        graphic.rectTransform.sizeDelta = new Vector2(size, size);
        graphic.raycastTarget = false;
        graphic.maskable = false;
        graphic.SetShape(shape);
        item.SetActive(false);
        graphicCount++;
        return graphic;
    }

    public void Release(CatCareFxGraphic graphic)
    {
        if (graphic == null) return;
        graphic.gameObject.SetActive(false);
        Destroy(graphic.gameObject);
        graphicCount = Mathf.Max(0, graphicCount - 1);
        if (graphicCount == 0 && canvas != null)
        {
            canvas.gameObject.SetActive(false);
            Destroy(canvas.gameObject);
            canvas = null;
            canvasRect = null;
        }
    }

    public void RebindHead()
    {
        head = null;
        visual = null;
    }

    public bool TryHeadPosition(out Vector2 position)
    {
        position = Vector2.zero;
        if (!isActiveAndEnabled || canvas == null || TitleScreen.IsShowing || HomeUiFlow.IsMiniGameVisible)
            return false;
        // Retired breed roots are detached before deferred destruction; never follow that stale skeleton.
        if (head == null || !head.IsChildOf(transform))
        {
            head = FindBone(transform, "DEF-head") ?? FindBone(transform, "DEF-spine.006") ??
                   FindBone(transform, "Head") ?? FindBone(transform, "Neck");
            visual = GetComponentInChildren<SkinnedMeshRenderer>(true);
        }
        if (head == null || !head.gameObject.activeInHierarchy ||
            (visual != null && (!visual.enabled || !visual.gameObject.activeInHierarchy))) return false;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled ||
            gameplayCamera.gameObject.scene != gameObject.scene)
            gameplayCamera = Camera.main;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled ||
            gameplayCamera.gameObject.scene != gameObject.scene) return false;
        Vector3 screen = gameplayCamera.WorldToScreenPoint(head.position + Vector3.up * .06f);
        if (screen.z <= 0f || !gameplayCamera.pixelRect.Contains(screen)) return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out position);
    }

    private void EnsureCanvas()
    {
        if (canvas != null) return;
        var root = new GameObject("CatCareFxCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 48;
        canvasRect = (RectTransform)root.transform;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        var group = root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        // No GraphicRaycaster: decorative shapes cannot become an input surface.
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

    private void OnEnable() { if (canvas != null) canvas.gameObject.SetActive(true); }
    private void OnDisable() { if (canvas != null) canvas.gameObject.SetActive(false); }
    private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
}
