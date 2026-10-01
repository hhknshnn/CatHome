using TMPro;
using UnityEngine;

/// <summary>Two real typefaces; no synthetic bold on an already heavy face.</summary>
public static class PremiumTypography
{
    private static TMP_FontAsset display, body, emphasis;
    public static TMP_FontAsset Display => display != null ? display : display = Resources.Load<TMP_FontAsset>("Typography/FredokaDisplay");
    public static TMP_FontAsset Body => body != null ? body : body = Resources.Load<TMP_FontAsset>("Typography/NunitoBody");
    public static TMP_FontAsset Emphasis => emphasis != null ? emphasis : emphasis = Resources.Load<TMP_FontAsset>("Typography/FredokaEmphasis");

    /// <summary>Keep name editing on one pre-baked Turkish face and weight.</summary>
    public static void ApplyNameInput(TMP_InputField input)
    {
        if(input==null||Body==null)return;
        input.fontAsset=Body;
        input.richText=false;
        Apply(input.textComponent,false);
        if(input.textComponent!=null)input.textComponent.richText=false;
        if(input.placeholder is TMP_Text placeholder)Apply(placeholder,false);
        input.onValueChanged.AddListener(value=>ComposeNameInput(input,value));
    }

    private static void ComposeNameInput(TMP_InputField input,string value)
    {
        if(string.IsNullOrEmpty(value))return;
        // Mobile keyboards can send decomposed accents. Use the font's native
        // composed glyph while preserving spaces, case and selection positions.
        try
        {
            var form=System.Text.NormalizationForm.FormC;
            string composed=value.Normalize(form);
            if(composed==value)return;
            int anchor=Mathf.Clamp(input.selectionStringAnchorPosition,0,value.Length);
            int focus=Mathf.Clamp(input.selectionStringFocusPosition,0,value.Length);
            input.SetTextWithoutNotify(composed);
            input.selectionStringAnchorPosition=value.Substring(0,anchor).Normalize(form).Length;
            input.selectionStringFocusPosition=value.Substring(0,focus).Normalize(form).Length;
        }
        catch(System.ArgumentException) { /* Wait for an incomplete keyboard Unicode sequence. */ }
    }

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
