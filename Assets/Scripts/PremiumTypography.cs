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
        // Legacy home action labels are named Text (TMP); their shared authored
        // context identifies the action after the face has been rebuilt.
        Transform context = label.transform.parent;
        if (context != null && context.name == "ContextFace") context = context.parent;
        bool contextAction = context != null && (context.name == "ActionButton" ||
            context.name == "ActivityActionButton" || context.name == "ActivityProgressBadge");
        bool action = name == "label" || name == "playlabel" || name == "actiontext" || name == "actionbuttontext" ||
            name == "roomprogresslabel" || name == "percentagetext" || name == "value" ||
            name.EndsWith("labelfront") || name == "catname" || contextAction;
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
