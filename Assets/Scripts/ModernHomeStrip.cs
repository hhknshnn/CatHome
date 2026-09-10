using UnityEngine;
using UnityEngine.UI;

/// <summary>Opaque repaint for the room camera's reserved navigation pixels.
/// It stays outside the controls' hidden CanvasGroup, so a moving dialogue
/// cannot accumulate translucent frames in the region the camera skips.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform), typeof(Image))]
public sealed class ModernHomeStrip : MonoBehaviour
{
    private RectTransform rect;
    private Canvas owner;
    private Image fill;

    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (rect == null) rect = (RectTransform)transform;
        if (owner == null) owner = GetComponentInParent<Canvas>();
        if (fill == null) fill = GetComponent<Image>();
        if (owner == null || fill == null) return;
        fill.raycastTarget = false;
        fill.color = ModernUiArt.Paper;
        // Mini-game cameras render their entire viewport; a home strip must
        // never paint over their lower controls or scenery.
        fill.enabled = !HomeUiFlow.IsMiniGameVisible;
        float scale = Mathf.Max(.01f, owner.scaleFactor);
        float height = HomeWorldViewport.StripHeight + Screen.safeArea.yMin / scale;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(0, height);
        rect.localScale = Vector3.one;
    }
}
