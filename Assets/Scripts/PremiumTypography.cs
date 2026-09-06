using TMPro;
using UnityEngine;

/// <summary>Two real typefaces; no synthetic bold on an already heavy face.</summary>
public static class PremiumTypography
{
    private static TMP_FontAsset display, body, emphasis;
    public static TMP_FontAsset Display => display != null ? display : display = Resources.Load<TMP_FontAsset>("Typography/FredokaDisplay");
    public static TMP_FontAsset Body => body != null ? body : body = Resources.Load<TMP_FontAsset>("Typography/NunitoBody");
    public static TMP_FontAsset Emphasis => emphasis != null ? emphasis : emphasis = Resources.Load<TMP_FontAsset>("Typography/FredokaEmphasis");

    public static void Apply(TMP_Text label, bool? heading = null)
    {
        if (label == null) return;
        string name = label.name.ToLowerInvariant();
        bool isHeading = heading ?? (label.fontSize >= 34f || name.Contains("title") && !name.Contains("subtitle") || name == "wordmark");
        bool action = name == "label" || name == "playlabel" || name == "actiontext" ||
            name == "roomprogresslabel" || name == "percentagetext" || name == "value" ||
            name.EndsWith("labelfront") || name == "catname";
        var font = isHeading || action ? (Emphasis != null ? Emphasis : Display) : Body;
        if (font == null) return;
        label.font = font;
        label.fontSharedMaterial = font.material;
        label.fontStyle &= ~FontStyles.Bold;
        label.fontWeight = FontWeight.Regular;
        label.characterSpacing = isHeading ? -.25f : .05f;
        label.wordSpacing = 0f;
        label.lineSpacing = isHeading ? -2f : 3f;
        label.extraPadding = true;
        label.enableVertexGradient = false;
    }
}
