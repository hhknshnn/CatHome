using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in presentation shared by the storybook screens. Never changes actions or layout ownership.</summary>
public static class StorybookScreenStyle
{
    public static readonly Color Indigo = new Color32(30, 46, 81, 255);
    public static readonly Color IndigoTop = new Color32(49, 72, 112, 255);
    public static readonly Color Cream = new Color32(255, 247, 226, 255);
    public static readonly Color Paper = new Color32(248, 241, 225, 255);
    public static readonly Color Ink = new Color32(38, 53, 87, 255);
    public static readonly Color Muted = new Color32(91, 105, 128, 255);
    public static readonly Color Mint = new Color32(155, 236, 216, 255);
    public static readonly Color Teal = new Color32(47, 179, 178, 255);
    public static readonly Color Coral = new Color32(238, 122, 117, 255);

    public static void Enamel(LowPolyPanelGraphic surface, Color top, Color bottom, float radius = 22f, bool quiet = true)
    {
        if (surface == null) return;
        surface.ConfigureScreenStyle(top, bottom, radius, !quiet);
    }

    public static void Shell(LowPolyPanelGraphic surface, float radius = 32f)
    { Enamel(surface, IndigoTop, Indigo, radius); }

    public static void Card(LowPolyPanelGraphic surface, float radius = 22f)
    { Enamel(surface, Cream, Paper, radius); }

    public static void Inset(LowPolyPanelGraphic surface, Color color, float radius = 18f)
    {
        if (surface != null) surface.ConfigureScreenStyle(Color.Lerp(color, Color.white, .025f), color, radius, false, false);
    }

    public static void Action(Button button, bool secondary = false, bool coral = false)
    {
        if (button == null) return;
        var face = button.targetGraphic as LowPolyPanelGraphic;
        float radius = face == null ? 18f : Mathf.Min(22f, Mathf.Max(12f, face.rectTransform.rect.height * .3f));
        Enamel(face, secondary ? new Color32(64, 87, 124, 255) : coral ? new Color32(255, 181, 151, 255) : new Color32(135, 232, 206, 255),
            secondary ? new Color32(45, 66, 104, 255) : coral ? Coral : Teal, radius, false);
        if (face != null) face.CrossFadeColor(Color.white, 0f, true, true);
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.pressedColor = colors.disabledColor = Color.white;
        button.colors = colors;
        var contrast = button.GetComponent<StorybookActionContrast>();
        if (contrast == null) contrast = button.gameObject.AddComponent<StorybookActionContrast>();
        contrast.Configure(button, secondary ? Cream : Ink, Cream);
        var fx = button.GetComponent<PremiumButtonFx>();
        if (fx == null && face != null)
        {
            fx = button.gameObject.AddComponent<PremiumButtonFx>();
            fx.Configure(face.rectTransform, face, button, !secondary);
        }
    }

    public static void Text(Transform root, string name, Color color)
    {
        if (root == null) return;
        var child = root.Find(name);
        var label = child == null ? null : child.GetComponent<TMP_Text>();
        if (label != null) label.color = color;
    }

    public static void CurrencyIcons(Transform root)
    {
        if (root == null) return;
        var art = Resources.Load<PremiumMomentArtSet>("PremiumMomentArt");
        Texture texture = art != null ? art.Coin : null;
        // The shared asset is available before the home HUD has been instantiated.
        if (texture == null) foreach (var wallet in Object.FindObjectsByType<CurrencyHudController>(FindObjectsInactive.Include))
        {
            var icon = wallet.transform.Find("SafeArea/Group/CoinEntry/Icon");
            var image = icon == null ? null : icon.GetComponent<RawImage>();
            if (image != null && image.texture != null) { texture = image.texture; break; }
        }
        if (texture == null) return;
        foreach (var image in root.GetComponentsInChildren<RawImage>(true))
        {
            if (image.name != "PremiumCoinSprite" && image.name != "RewardCoin") continue;
            image.texture = texture;
            image.uvRect = new Rect(0, 0, 1, 1);
            image.color = Color.white;
            var sparkle = image.GetComponent<PremiumAmbientSparkle>();
            if (sparkle != null) sparkle.enabled = false;
        }
    }
}
