using UnityEngine;

/// <summary>Keeps the title's controls inside the safe area at tablet and wide aspect ratios.</summary>
[DisallowMultipleComponent]
public sealed class TitleScreenLayout : MonoBehaviour
{
    [SerializeField] private RectTransform brandDock, shortcuts;
    [SerializeField] private bool storybook;
    [SerializeField] private RectTransform utilities, readingPanel;
    private Vector2 lastSize;
    private bool lastQuit;
    private Vector2 lastSafeAnchor;
    private bool lastLocked;
    private void OnEnable() => Apply();
    private void LateUpdate() => Apply();
    public void Apply()
    {
        var safe = transform as RectTransform;
        if (safe == null) return;
        float scale = Mathf.Min(1f, safe.rect.width / 1920f, safe.rect.height / 1080f);
        if (storybook)
        {
            var quit = utilities != null ? utilities.Find("QuitButton") : null;
            bool hasQuit = quit != null && quit.gameObject.activeSelf;
            var badge=shortcuts.Find("ShopShortcut/Visual/AfterTourBadge");
            bool locked=badge!=null&&badge.gameObject.activeSelf;
            if (lastSize == safe.rect.size && lastQuit == hasQuit && lastSafeAnchor==safe.anchorMin && lastLocked==locked) return;
            lastSize = safe.rect.size; lastQuit = hasQuit;
            lastSafeAnchor=safe.anchorMin;lastLocked=locked;
            brandDock.anchorMin = brandDock.anchorMax = new Vector2(0f,.5f);
            brandDock.localScale = Vector3.one*scale;
            brandDock.anchoredPosition = new Vector2(365f*scale,0f);
            shortcuts.anchorMin = shortcuts.anchorMax = new Vector2(1f,0f);
            shortcuts.localScale = Vector3.one*scale;
            shortcuts.anchoredPosition = new Vector2(-406f*scale,146f*scale);
            foreach(string shortcut in new[]{"ShopShortcut","RoomsShortcut","GamesShortcut"})
            {
                var preview=shortcuts.Find(shortcut+"/Visual/Preview") as RectTransform;
                if(preview==null)continue;
                preview.anchoredPosition=new Vector2(0,locked?18:26);
                preview.sizeDelta=Vector2.one*(locked?80:104);
            }
            if (readingPanel != null)
            {
                var canvas = readingPanel.parent as RectTransform;
                float inset = canvas.InverseTransformPoint(safe.TransformPoint(new Vector3(safe.rect.xMin,0,0))).x-canvas.rect.xMin;
                readingPanel.sizeDelta = new Vector2(inset+755f*scale,0);
            }
            if (utilities != null)
            {
                float width = hasQuit ? 696f : 540f;
                utilities.anchorMin = utilities.anchorMax = Vector2.one;
                utilities.pivot = new Vector2(.5f,.5f);
                utilities.sizeDelta = new Vector2(width,80f);
                utilities.localScale = Vector3.one*scale;
                utilities.anchoredPosition = new Vector2(-(56f+width*.5f)*scale,-70f*scale);
                float x = -width*.5f;
                foreach (string name in new[]{"SettingsButton","CreditsButton","QuitButton"})
                {
                    var item=utilities.Find(name) as RectTransform;
                    if(item==null||!item.gameObject.activeSelf)continue;
                    item.anchorMin=item.anchorMax=item.pivot=new Vector2(.5f,.5f);
                    item.anchoredPosition=new Vector2(x+item.sizeDelta.x*.5f,0);
                    x+=item.sizeDelta.x+16f;
                }
            }
            return;
        }
        if (brandDock != null) { brandDock.localScale = Vector3.one * scale; brandDock.anchoredPosition = new Vector2(-632f * scale, 0f); }
        if (shortcuts != null) { shortcuts.localScale = Vector3.one * scale; shortcuts.anchoredPosition = new Vector2(500f * scale, -420f * scale); }
    }
#if UNITY_EDITOR
    public void EditorConfigure(RectTransform dock, RectTransform links) { brandDock = dock; shortcuts = links; }
    public void EditorConfigureStorybook(RectTransform dock, RectTransform links, RectTransform tools, RectTransform panel)
    { brandDock=dock;shortcuts=links;utilities=tools;readingPanel=panel;storybook=true;lastSize=Vector2.zero;Apply(); }
#endif
}
