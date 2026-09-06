using UnityEngine;

/// <summary>Keeps the title's controls inside the safe area at tablet and wide aspect ratios.</summary>
[DisallowMultipleComponent]
public sealed class TitleScreenLayout : MonoBehaviour
{
    [SerializeField] private RectTransform brandDock, shortcuts;
    private void OnEnable() => Apply();
    private void LateUpdate() => Apply();
    public void Apply()
    {
        var safe = transform as RectTransform;
        if (safe == null) return;
        float scale = Mathf.Min(1f, safe.rect.width / 1920f, safe.rect.height / 1080f);
        if (brandDock != null) { brandDock.localScale = Vector3.one * scale; brandDock.anchoredPosition = new Vector2(-632f * scale, 0f); }
        if (shortcuts != null) { shortcuts.localScale = Vector3.one * scale; shortcuts.anchoredPosition = new Vector2(500f * scale, -420f * scale); }
    }
#if UNITY_EDITOR
    public void EditorConfigure(RectTransform dock, RectTransform links) { brandDock = dock; shortcuts = links; }
#endif
}
