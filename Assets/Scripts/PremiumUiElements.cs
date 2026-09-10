using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Native components for the approved warm, content-first screen layouts.</summary>
public static class PremiumUiElements
{
    public static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static void At(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    public static void Fill(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
    }

    public static LowPolyPanelGraphic Panel(string name, Transform parent, Color color,
        float x, float y, float width, float height, float radius = 24f, bool raycast = false)
    {
        var rect = Rect(name, parent); At(rect, x, y, width, height);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var graphic = rect.gameObject.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureSurface(graphic, color, radius, 2f);
        graphic.raycastTarget = raycast;
        ModernUiArt.Surface(graphic, graphic.color, radius, radius > 0 && color.a > .98f && height >= 120);
        return graphic;
    }

    public static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size,
        Color color, float x, float y, float width, float height,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var rect = Rect(name, parent); At(rect, x, y, width, height);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = size; text.color = color; text.alignment = alignment;
        text.raycastTarget = false; text.extraPadding = true; text.characterSpacing = .5f;
        text.textWrappingMode = TextWrappingModes.Normal;
        PremiumTypography.Apply(text);
        return text;
    }

    public static void Localize(TMP_Text text, string key)
    {
        var localized = text.GetComponent<LocalizedLabel>() ?? text.gameObject.AddComponent<LocalizedLabel>();
        localized.EditorConfigure(text, key);
    }

    public static Button Action(string name, Transform parent, TMP_FontAsset font, string key,
        Color color, float x, float y, float width, float height, out TMP_Text label)
    {
        var root = Rect(name, parent); At(root, x, y, width, height);
        var surface = Panel("Visual", root, color, 0, 0, width, height, Mathf.Min(16f,height*.25f), true);
        Fill(surface.rectTransform);
        var button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = surface; button.transition = Selectable.Transition.None;
        label = Label("Label", surface.transform, font, 26f, PremiumUiStyle.Ink,
            0, 0, width - 40f, height - 12f, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(20,6); label.rectTransform.offsetMax = new Vector2(-20,-6);
        if (!string.IsNullOrEmpty(key)) Localize(label, key);
        root.gameObject.AddComponent<PremiumButtonFx>().Configure(surface.rectTransform, surface, button, false);
        ModernUiArt.Action(button, ModernUiArt.IsLight(color));
        return button;
    }
}
