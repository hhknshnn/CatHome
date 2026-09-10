using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The approved September 8 visual roles, also used by runtime-created UI.</summary>
public static class ModernUiArt
{
    public static readonly Color Ink = new Color32(23, 40, 65, 255);
    public static readonly Color Muted = new Color32(83, 103, 125, 255);
    public static readonly Color Azure = new Color32(39, 121, 245, 255);
    public static readonly Color AzureTop = new Color32(70, 153, 255, 255);
    public static readonly Color Paper = new Color32(247, 250, 255, 255);
    public static readonly Color Inset = new Color32(234, 243, 255, 255);
    public static readonly Color Border = new Color32(202, 222, 246, 255);
    public static readonly Color Coral = new Color32(255, 105, 90, 255);
    public static readonly Color Gold = new Color32(255, 200, 87, 255);
    public static readonly Color Mint = new Color32(217, 246, 235, 255);
    public static readonly Color Purple = new Color32(149, 111, 232, 255);

    public static bool IsLight(Color value) => value.r * .2126f + value.g * .7152f + value.b * .0722f > .64f;

    public static void Surface(LowPolyPanelGraphic graphic, Color color, float radius = 22f, bool elevated = false)
    {
        if (graphic == null) return;
        Color top = Color.Lerp(color, Color.white, IsLight(color) ? .36f : .07f);
        top.a = color.a;
        graphic.ConfigureModernStyle(top, color, radius, elevated, false);
    }

    public static void Action(Button button, bool secondary = false, bool destructive = false)
    {
        if (button == null) return;
        var surface = button.targetGraphic as LowPolyPanelGraphic;
        int coatIndex = -1;
        bool coatSwatch = button.name.StartsWith("Coat_", System.StringComparison.Ordinal) &&
            int.TryParse(button.name.Substring(5), out coatIndex) &&
            coatIndex >= 0 && coatIndex < CatIdentityService.CoatCount;
        // These eight controls communicate a real colour choice, so their face
        // must show the palette instead of inheriting the primary action colour.
        Color bottom = coatSwatch ? CatIdentityService.Palette[coatIndex].Tint : secondary ? Inset : destructive ? Coral : Azure;
        Color top = coatSwatch ? bottom : secondary ? Paper : destructive ? Color.Lerp(Coral, Color.white, .13f) : AzureTop;
        if (surface != null)
        {
            float radius = Mathf.Min(16f, Mathf.Max(6f, surface.rectTransform.rect.height * .24f));
            surface.ConfigureModernStyle(top, bottom, radius, !secondary && !coatSwatch, !secondary && !coatSwatch);
            if(!secondary&&!coatSwatch)surface.ConfigurePlayfulAction(top,bottom,radius);
            // End legacy Selectable tint tweens; modern interaction states are
            // drawn by the surface. Parent CanvasGroup fades stay independent.
            surface.CrossFadeColor(Color.white, 0f, true, true);
        }
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.disabledColor = Color.white;
        colors.fadeDuration = .08f;
        button.colors = colors;
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            label.color = secondary ? Ink : Color.white;
        var fx = button.GetComponent<PremiumButtonFx>();
        if (fx == null)
        {
            fx = button.gameObject.AddComponent<PremiumButtonFx>();
            fx.Configure(surface != null ? surface.rectTransform : button.transform as RectTransform, surface, button, !secondary);
        }
    }

    public static void FlatNavigation(Button button)
    {
        if (button == null) return;
        Surface(button.targetGraphic as LowPolyPanelGraphic, Paper, 12f, false);
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) label.color = Ink;
        var fx = button.GetComponent<PremiumButtonFx>();
        if (fx != null) fx.Configure(button.targetGraphic != null ? button.targetGraphic.rectTransform : button.transform as RectTransform,
            button.targetGraphic as LowPolyPanelGraphic, button, false, false);
    }
}
