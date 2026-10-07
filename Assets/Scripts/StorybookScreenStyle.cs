using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in presentation shared by the storybook screens. Never changes actions or layout ownership.</summary>
public static class StorybookScreenStyle
{
    public static readonly Color Indigo = new Color32(30, 46, 81, 255);
    public static readonly Color IndigoTop = new Color32(49, 72, 112, 255);
    public static readonly Color Cream = new Color32(255, 241, 213, 255);
    public static readonly Color Paper = new Color32(49, 88, 91, 215);
    public static readonly Color Ink = new Color32(255, 245, 222, 255);
    public static readonly Color Muted = new Color32(204, 223, 209, 255);
    public static readonly Color MintWash = new Color32(43, 78, 82, 180);
    public static readonly Color Track = new Color32(37, 72, 78, 255);
    public static readonly Color Mint = new Color32(151, 232, 209, 255);
    public static readonly Color Teal = new Color32(51, 94, 98, 225);
    public static readonly Color Coral = new Color32(222, 79, 75, 255);
    public static readonly Color CoralTop = new Color32(255, 148, 120, 255);
    public static readonly Color Honey = new Color32(168, 235, 212, 255);
    public static readonly Color Wood = new Color32(74, 138, 135, 255);
    public static readonly Color Photo = new Color32(62, 103, 106, 255);
    public static readonly Color GlassTop = new Color32(75, 119, 118, 195);
    public static readonly Color GlassBottom = new Color32(36, 76, 82, 205);
    public static readonly Color CardTop = new Color32(86, 137, 134, 125);
    public static readonly Color CardBottom = new Color32(49, 92, 97, 165);

    public static void Enamel(LowPolyPanelGraphic surface, Color top, Color bottom, float radius = 22f, bool quiet = true)
    {
        if (surface == null) return;
        StorybookRoomBackdrop.Hide(surface);
        surface.ConfigureHudArtwork(null);
        surface.ConfigureGlassStyle(top, bottom, radius, !quiet);
    }

    public static void Shell(LowPolyPanelGraphic surface, float radius = 32f)
    { Enamel(surface, GlassTop, GlassBottom, radius); }

    public static void RoomShell(LowPolyPanelGraphic surface, float radius = 32f, bool quiet = false, bool decorated = false)
    { Enamel(surface, GlassTop, GlassBottom, Mathf.Min(48f, radius * 1.4f)); }

    public static void Card(LowPolyPanelGraphic surface, float radius = 22f)
    { Enamel(surface, CardTop, CardBottom, radius); }

    public static void Selected(LowPolyPanelGraphic surface, float radius = 22f)
    { Enamel(surface, new Color32(104, 178, 163, 165), new Color32(52, 112, 111, 205), radius); }

    public static void Inset(LowPolyPanelGraphic surface, Color color, float radius = 18f)
    {
        if (surface == null) return;
        StorybookRoomBackdrop.Hide(surface);
        surface.ConfigureHudArtwork(null);
        if (color == Mint) color = Teal;
        var top = Color.Lerp(color, Mint, .06f); top.a = color.a;
        surface.ConfigureGlassStyle(top, color, radius, false, false);
    }

    public static void Action(Button button, bool secondary = false, bool coral = false)
    {
        if (button == null) return;
        var face = button.targetGraphic as LowPolyPanelGraphic;
        float radius = face == null ? 24f : Mathf.Max(18f, face.rectTransform.rect.height * .5f);
        Enamel(face, secondary ? new Color32(80, 130, 130, 205) : CoralTop,
            secondary ? Teal : Coral, radius, false);
        if (face != null) face.CrossFadeColor(Color.white, 0f, true, true);
        var colors = button.colors;
        colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.pressedColor = colors.disabledColor = Color.white;
        button.colors = colors;
        var contrast = button.GetComponent<StorybookActionContrast>();
        if (contrast == null) contrast = button.gameObject.AddComponent<StorybookActionContrast>();
        contrast.Configure(button, Cream, new Color32(181, 200, 188, 255));
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
