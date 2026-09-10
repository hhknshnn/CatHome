using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CatSpeechBubble : MonoBehaviour
{
    private const int CurrentVisualVersion = 14;
    private const float HoldDuration = 1.75f;
    private const string VisualRootName = "CatSpeechBubbleVisual";

    [Header("Runtime Visual Hierarchy")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform bubble;
    [SerializeField] private TMP_Text label;
    private Transform head;
    private Camera gameplayCamera;
    private Coroutine routine;
    private WhileYouWereAwayPopup offlinePopup;
    private float animationYOffset;
    [SerializeField, HideInInspector] private int visualVersion;
    private readonly RectTransform[] tails = new RectTransform[3];

    public void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EnsureVisualHierarchy();
        if (!IsVisualHierarchyReady() || IsModalBlocking())
            return;

        label.text = GameContentCopy.CatReaction(message);
        ResizeToMessage();
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private bool IsModalBlocking()
    {
        if (!PetTutorialHint.IsOnboardingCompleted || OnboardingCelebrationView.IsAnyOpen)
            return true;
        if (offlinePopup == null)
            offlinePopup = FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        return offlinePopup != null && offlinePopup.IsOpen;
    }

    private void EnsureVisualHierarchy()
    {
        gameplayCamera = Camera.main;
        head = FindNamed(transform, "DEF-spine.006") ?? FindNamed(transform, "Head") ?? FindNamed(transform, "Neck") ?? transform;

        Canvas hudCanvas = FindHudCanvas();
        visualRoot = ResolveSingleVisualRoot();
        bool createdRoot = visualRoot == null;
        if (createdRoot)
        {
            GameObject root = new GameObject(VisualRootName, typeof(RectTransform));
            visualRoot = root.GetComponent<RectTransform>();
        }

        Transform desiredParent = hudCanvas != null ? hudCanvas.transform : null;
        if (visualRoot.parent != desiredParent)
            visualRoot.SetParent(desiredParent, false);
        visualRoot.name = VisualRootName;
        visualRoot.anchorMin = Vector2.zero;
        visualRoot.anchorMax = Vector2.one;
        visualRoot.offsetMin = Vector2.zero;
        visualRoot.offsetMax = Vector2.zero;
        visualRoot.localScale = Vector3.one;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.gameObject.SetActive(true);
        visualRoot.SetAsLastSibling();

        canvas = GetOrAdd<Canvas>(visualRoot.gameObject);
        if (canvas == null)
            return; // Canvas ownership could not be established on the visual root; bail before touching it.
        if (hudCanvas == null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(visualRoot.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 75;
        GraphicRaycaster raycaster = GetOrAdd<GraphicRaycaster>(visualRoot.gameObject);
        raycaster.enabled = false;
        group = GetOrAdd<CanvasGroup>(visualRoot.gameObject);
        if (createdRoot)
            group.alpha = 0f;
        else
            group.alpha = Mathf.Clamp01(group.alpha);
        group.interactable = false;
        group.blocksRaycasts = false;

        bubble = EnsureRect(visualRoot, "Bubble", new Vector2(280f, 82f));
        bubble.pivot = new Vector2(.5f, .5f);
        bubble.anchorMin = bubble.anchorMax = new Vector2(.5f, .5f);
        bubble.localScale = bubble.localScale == Vector3.zero ? Vector3.one : bubble.localScale;
        bubble.gameObject.SetActive(true);

        RectTransform shadowTail = EnsureLayer(bubble, "ShadowTail", new Vector2(42f, 28f),
            new Vector2(0f, -48f), new Color32(10, 18, 27, 145), 0f, true, 0);
        RectTransform shadowPanel = EnsureLayer(bubble, "ShadowPanel", new Vector2(288f, 90f),
            new Vector2(0,-3), new Color32(23, 51, 86, 26), 25f, false, -1);
        RectTransform orangeTail = EnsureLayer(bubble, "OrangeTail", new Vector2(42f, 28f),
            new Vector2(0f, -42f), new Color32(158, 202, 249, 255), 0f, true, 1);
        RectTransform orangeFrame = EnsureLayer(bubble, "OrangeFrame", new Vector2(280f, 82f),
            Vector2.zero, new Color32(158, 202, 249, 255), 24f, false, -1);
        RectTransform creamTail = EnsureLayer(bubble, "CreamTail", new Vector2(42f, 28f),
            new Vector2(0f, -39f), new Color32(247, 250, 255, 255), 0f, true, 2);
        RectTransform creamFace = EnsureLayer(bubble, "CreamFace", new Vector2(276f, 78f),
            Vector2.zero, new Color32(247, 250, 255, 255), 22f, false, -1);

        shadowTail.SetSiblingIndex(0); shadowPanel.SetSiblingIndex(1);
        orangeTail.SetSiblingIndex(2); orangeFrame.SetSiblingIndex(3);
        creamTail.SetSiblingIndex(4); creamFace.SetSiblingIndex(5);
        shadowTail.gameObject.SetActive(false);
        shadowPanel.gameObject.SetActive(true);

        RectTransform text = EnsureRect(bubble, "Message", new Vector2(244f, 52f));
        text.anchoredPosition = new Vector2(0f, 2f);
        CanvasRenderer labelRenderer = GetOrAdd<CanvasRenderer>(text.gameObject);
        label = GetOrAdd<TextMeshProUGUI>(text.gameObject);
        text.gameObject.SetActive(true);
        label.enabled = true;
        if (label.font == null)
            label.font = FindPreferredFont();
        label.fontSize = 23f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = PremiumUiStyle.Ink;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 23f;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Truncate;
        label.maxVisibleLines = 2;
        label.raycastTarget = false;
        label.richText = false;
        if (labelRenderer != null)
            labelRenderer.SetAlpha(1f);
        text.SetAsLastSibling();
        RemoveExtraTextObjects();
        visualVersion = CurrentVisualVersion;
    }

    private RectTransform EnsureLayer(
        RectTransform parent, string layerName, Vector2 size, Vector2 position,
        Color32 layerColor, float cornerCut, bool isTail, int tailIndex)
    {
        RectTransform rect = EnsureRect(parent, layerName, size);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
        rect.gameObject.SetActive(true);
        // MaskableGraphic requires a CanvasRenderer. Guarantee one (Unity-aware, so a
        // destroyed / fake-null renderer on a reused layer is re-added) BEFORE the graphic
        // is enabled or its alpha touched, otherwise graphic.canvasRenderer throws.
        CanvasRenderer canvasRenderer = GetOrAdd<CanvasRenderer>(rect.gameObject);
        CatSpeechBubbleGraphic graphic = GetOrAdd<CatSpeechBubbleGraphic>(rect.gameObject);
        Graphic[] otherGraphics = rect.GetComponents<Graphic>();
        for (int i = 0; i < otherGraphics.Length; i++)
        {
            if (otherGraphics[i] == graphic)
                continue;
            otherGraphics[i].enabled = false;
            Destroy(otherGraphics[i]);
        }
        graphic.enabled = true;
        graphic.Configure(layerColor, cornerCut, isTail);
        graphic.raycastTarget = false;
        if (canvasRenderer != null)
            canvasRenderer.SetAlpha(1f);
        if (tailIndex >= 0)
            tails[tailIndex] = rect;
        return rect;
    }

    private Canvas FindHudCanvas()
    {
        Canvas[] candidates = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas best = null;
        for (int i = 0; i < candidates.Length; i++)
        {
            Canvas candidate = candidates[i];
            if (candidate == null || candidate.name != "Canvas" || !candidate.gameObject.scene.IsValid() ||
                candidate.name == VisualRootName || candidate.name == "CatSpeechBubbleCanvas" ||
                candidate.GetComponentInParent<PetTutorialHint>() != null)
                continue;

            Canvas rootCanvas = candidate.rootCanvas;
            if (rootCanvas != candidate || candidate.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;

            if (best == null || candidate.sortingOrder > best.sortingOrder)
                best = candidate;
        }
        return best;
    }

    private RectTransform ResolveSingleVisualRoot()
    {
        var matches = new System.Collections.Generic.List<RectTransform>();
        RectTransform[] all = Resources.FindObjectsOfTypeAll<RectTransform>();
        for (int i = 0; i < all.Length; i++)
        {
            RectTransform candidate = all[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid())
                continue;
            if (candidate.name == VisualRootName || candidate.name == "CatSpeechBubbleCanvas")
                matches.Add(candidate);
        }

        RectTransform chosen = visualRoot != null && visualRoot.gameObject.scene.IsValid()
            ? visualRoot
            : matches.Count > 0 ? matches[0] : null;
        for (int i = 0; i < matches.Count; i++)
            if (matches[i] != chosen)
                Retire(matches[i].gameObject);
        return chosen;
    }

    private static RectTransform EnsureRect(Transform parent, string objectName, Vector2 size)
    {
        Transform existing = parent.Find(objectName);
        RectTransform rect = existing as RectTransform;
        if (existing != null && rect == null)
        {
            Retire(existing.gameObject);
            rect = null;
        }
        if (rect == null)
            rect = CreateRect(parent, objectName, size);
        else if (rect.parent != parent)
            rect.SetParent(parent, false);

        rect.name = objectName;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private void RemoveExtraTextObjects()
    {
        TMP_Text[] texts = visualRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
            if (texts[i] != null && texts[i] != label)
                Retire(texts[i].gameObject);
    }

    private bool IsVisualHierarchyReady()
    {
        if (visualVersion != CurrentVisualVersion || visualRoot == null ||
            visualRoot.name != VisualRootName || canvas == null ||
            group == null || bubble == null || label == null || label.transform.parent != bubble)
            return false;

        string[] requiredLayers =
        {
            "ShadowTail", "ShadowPanel", "OrangeTail", "OrangeFrame", "CreamTail", "CreamFace"
        };
        for (int i = 0; i < requiredLayers.Length; i++)
        {
            RectTransform rect = bubble.Find(requiredLayers[i]) as RectTransform;
            CatSpeechBubbleGraphic graphic = rect != null ? rect.GetComponent<CatSpeechBubbleGraphic>() : null;
            if (rect == null || rect.rect.width <= 0f || rect.rect.height <= 0f || graphic == null ||
                !graphic.enabled || graphic.color.a <= 0f || graphic.canvasRenderer.GetAlpha() <= 0f)
                return false;
        }
        return label.rectTransform.rect.width > 0f && label.rectTransform.rect.height > 0f;
    }

    private static void Retire(GameObject value)
    {
        if (value == null)
            return;
        value.name = "__RetiredCatSpeechBubbleVisual";
        value.SetActive(false);
        Destroy(value);
    }

    private static TMP_FontAsset FindPreferredFont()
    {
        TMP_FontAsset[] fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < fonts.Length; i++)
            if (fonts[i] != null && fonts[i].name.IndexOf("NunitoSans-Premium", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return fonts[i];
        for (int i = 0; i < fonts.Length; i++)
            if (fonts[i] != null && fonts[i].name.IndexOf("Fredoka", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return fonts[i];
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private void ResizeToMessage()
    {
        label.ForceMeshUpdate();
        float width = Mathf.Clamp(label.preferredWidth + 64f, 200f, 320f);
        float textWidth = width - 48f;
        Vector2 preferred = label.GetPreferredValues(label.text, textWidth, 60f);
        float height = preferred.y > 30f ? 96f : 78f;
        bubble.sizeDelta = new Vector2(width, height);
        SetLayerSize("ShadowPanel", new Vector2(width + 8f, height + 8f));
        SetLayerSize("OrangeFrame", new Vector2(width, height));
        SetLayerSize("CreamFace", new Vector2(width - 3f, height - 3f));
        SetTailY("ShadowTail", -height * .5f - 5f);
        SetTailY("OrangeTail", -height * .5f - 1f);
        SetTailY("CreamTail", -height * .5f + 5f);
        label.rectTransform.sizeDelta = new Vector2(textWidth, height - 34f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 2f);
    }

    private void SetLayerSize(string layerName, Vector2 size)
    {
        RectTransform layer = bubble.Find(layerName) as RectTransform;
        if (layer != null)
            layer.sizeDelta = size;
    }

    private void SetTailY(string layerName, float y)
    {
        RectTransform layer = bubble.Find(layerName) as RectTransform;
        if (layer != null)
            layer.anchoredPosition = new Vector2(layer.anchoredPosition.x, y);
    }

    private IEnumerator ShowRoutine()
    {
        group.alpha = 0f;
        bubble.localScale = Vector3.one * .96f;
        bubble.localRotation = Quaternion.identity;
        animationYOffset = -5f;
        UpdatePosition();

        yield return Animate(.18f, 0f, 1f, .96f, 1f, -5f, 0f, 0f, 0f);

        float elapsed = 0f;
        while (elapsed < HoldDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f;
            bubble.localScale = Vector3.one;
            bubble.localRotation = Quaternion.identity;
            animationYOffset = 0f;
            UpdatePosition();
            yield return null;
        }

        yield return Animate(.14f, 1f, 0f, 1f, .98f, 0f, 4f, 0f, 0f);
        group.alpha = 0f;
        routine = null;
    }

    private IEnumerator Animate(
        float duration, float fromAlpha, float toAlpha, float fromScale, float toScale,
        float fromY, float toY, float fromAngle, float toAngle)
    {
        if(CatRunnerProgressService.ReducedMotion)
        {group.alpha=toAlpha;bubble.localScale=Vector3.one;bubble.localRotation=Quaternion.identity;animationYOffset=0;UpdatePosition();yield break;}
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Smooth(elapsed / duration);
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            bubble.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, t);
            bubble.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(fromAngle, toAngle, t));
            animationYOffset = Mathf.Lerp(fromY, toY, t);
            UpdatePosition();
            yield return null;
        }
    }

    private void UpdatePosition()
    {
        if (bubble == null || head == null)
            return;
        if (gameplayCamera == null)
            gameplayCamera = Camera.main;
        if (gameplayCamera == null)
            return;

        Vector3 screen = gameplayCamera.WorldToScreenPoint(GetHeadWorldPosition());
        if (screen.z <= 0f)
        {
            group.alpha = 0f;
            return;
        }

        float uiScale = canvas != null ? Mathf.Max(.01f,canvas.scaleFactor) : 1f;
        Vector2 desired = (Vector2)screen + new Vector2(0f, (bubble.rect.height * .5f + 34f + animationYOffset)*uiScale);
        Rect safe = Screen.safeArea;
        float halfWidth = bubble.rect.width * .5f*uiScale;
        float halfHeight = bubble.rect.height * .5f*uiScale;
        float minX = safe.xMin + halfWidth + 14f;
        float maxX = safe.xMax - halfWidth - 14f;
        float minY = safe.yMin + halfHeight + 14f;
        float maxY = safe.yMax - halfHeight - 14f;
        if (minX > maxX) minX = maxX = safe.center.x;
        if (minY > maxY) minY = maxY = safe.center.y;
        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        bubble.position = desired;
        float tailX = Mathf.Clamp((screen.x - desired.x)/uiScale, -bubble.rect.width*.5f + 34f, bubble.rect.width*.5f - 34f);
        for (int i = 0; i < tails.Length; i++)
            if (tails[i] != null)
                tails[i].anchoredPosition = new Vector2(tailX + (i == 0 ? 4f : 0f), tails[i].anchoredPosition.y);
    }

    private Vector3 GetHeadWorldPosition()
    {
        if (head != null && head != transform)
            return head.position + Vector3.up * .08f;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        return transform.position + Vector3.up * .72f;
    }

    private static Transform FindNamed(Transform root, string value)
    {
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(item.name, value, System.StringComparison.OrdinalIgnoreCase))
                return item;
        return null;
    }

    // Unity-aware get-or-add: uses Unity's overloaded equality so a "fake-null"
    // (destroyed / pending-destruction) component is treated as missing and re-added,
    // instead of slipping through the C# ?? operator's plain reference check.
    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T existing = target.GetComponent<T>();
        return existing != null ? existing : target.AddComponent<T>();
    }

    private static RectTransform CreateRect(Transform parent, string name, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        return rect;
    }

    private static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private void OnDisable()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
        if (group != null)
            group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
    }
}

[DisallowMultipleComponent]
public sealed class CatSpeechBubbleGraphic : MaskableGraphic
{
    [SerializeField] private float cornerCut = 12f;
    [SerializeField] private bool triangle;
    private readonly Vector2[] points = new Vector2[48];

    public void Configure(Color32 layerColor, float cut, bool drawTriangle)
    {
        color = layerColor;
        cornerCut = cut;
        triangle = drawTriangle;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (triangle)
        {
            AddTriangle(vh,
                new Vector2(rect.xMin, rect.yMax),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.center.x, rect.yMin), color);
            return;
        }

        float cut = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * .35f);
        for(int corner=0;corner<4;corner++)
        {
            Vector2 center=corner==0?new Vector2(rect.xMin+cut,rect.yMin+cut):corner==1?new Vector2(rect.xMax-cut,rect.yMin+cut):corner==2?new Vector2(rect.xMax-cut,rect.yMax-cut):new Vector2(rect.xMin+cut,rect.yMax-cut);
            for(int step=0;step<12;step++)
            {float angle=(-180+corner*90+step/11f*90)*Mathf.Deg2Rad;points[corner*12+step]=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*cut;}
        }
        AddConvex(vh, points, color);
    }

    private static void AddConvex(VertexHelper vh, Vector2[] points, Color32 color)
    {
        int start = vh.currentVertCount;
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in points) center += point;
        center /= points.Length;
        vh.AddVert(center, color, Vector2.zero);
        foreach (Vector2 point in points) vh.AddVert(point, color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
    }

    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }

}
