using TMPro;
using UnityEngine;

/// <summary>
/// Lays out one currency HUD entry — icon, amount, "+" — as a single compact
/// content group centred inside the entry frame.
///
/// The amount is deliberately not a fixed-width stretched field. Its box is
/// measured from TMP's own preferred width every time the text changes, so the
/// "+" sits a small fixed gap to the right of the digits and travels with them
/// ("0" keeps it close, "123,456" pushes it out) instead of being pinned to the
/// far right edge of the entry. Icon, amount and "+" are then centred together,
/// which is why a short amount reads as a tight badge rather than as a wide
/// frame with a hole in the middle.
///
/// Font fitting is done here rather than through TMP's own auto-sizing: with
/// auto-sizing on, the size is resolved during TMP's layout pass, so the width
/// measured here would describe the previous size and the "+" would lag the
/// digits by a frame. SDF glyph advances scale linearly with point size, so one
/// ratio step lands on the fitting size and the second measurement below only
/// confirms it.
///
/// Every final offset is a whole canvas unit. The fixed parts are sized so the
/// arithmetic stays integral (icon + gap + gap + "+" is an even total, and the
/// measured amount box is rounded up to an even width), which keeps the
/// low-poly meshes and the TMP glyphs on the same pixel grid as the top bar
/// instead of landing on half units.
///
/// This component only positions three RectTransforms and sets a font size. It
/// reads no service, owns no amount and never touches save data.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CurrencyEntryLayout : MonoBehaviour
{
    [Header("Content (authored by CurrencyHudBuilder)")]
    [SerializeField] private RectTransform iconRect;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private RectTransform plusRect;

    [Header("Fixed content metrics (canvas units, 1920x1080 reference)")]
    [SerializeField] private float iconSize = 28f;
    [SerializeField] private float iconValueGap = 5f;
    [SerializeField] private float valuePlusGap = 7f;
    [SerializeField] private float plusSize = 26f;

    [Header("Measured amount box")]
    [SerializeField] private float minValueWidth = 16f;
    [SerializeField] private float maxValueWidth = 38f;
    [SerializeField] private float minFontSize = 11f;
    [SerializeField] private float maxFontSize = 26f;

    // Last laid-out text. Null forces the next pass to run.
    private string lastText;

    /// <summary>
    /// Width the whole content group currently occupies. Exposed for tooling and
    /// tests; the entry frame itself keeps its authored width regardless.
    /// </summary>
    public float ContentWidth { get; private set; }

    public void Configure(
        RectTransform icon,
        TMP_Text value,
        RectTransform plus,
        float iconSize,
        float iconValueGap,
        float valuePlusGap,
        float plusSize,
        float minValueWidth,
        float maxValueWidth,
        float minFontSize,
        float maxFontSize)
    {
        iconRect = icon;
        valueText = value;
        plusRect = plus;
        this.iconSize = iconSize;
        this.iconValueGap = iconValueGap;
        this.valuePlusGap = valuePlusGap;
        this.plusSize = plusSize;
        this.minValueWidth = minValueWidth;
        this.maxValueWidth = maxValueWidth;
        this.minFontSize = minFontSize;
        this.maxFontSize = maxFontSize;

        lastText = null;
        Relayout();
    }

    private void OnEnable()
    {
        // The amount may have changed while this object was disabled, and TMP's
        // measurements are only valid once it is active, so never trust the
        // cached text across an enable.
        lastText = null;
        Relayout();
    }

    /// <summary>
    /// Safety net for an amount written straight into the TMP component (the
    /// inspector while authoring, or any future writer that does not go through
    /// <see cref="CurrencyHudController.Refresh"/>). The controller calls
    /// <see cref="Relayout"/> itself on every repaint, so in the running game
    /// this normally finds nothing to do.
    /// </summary>
    private void LateUpdate()
    {
        if (valueText != null && !string.Equals(valueText.text, lastText))
            Relayout();
    }

    /// <summary>
    /// Re-measures the amount and re-centres the content group. Cheap enough to
    /// call on every repaint: it costs at most two TMP preferred-width queries
    /// and three RectTransform writes, and only runs when an amount changed.
    /// </summary>
    public void Relayout()
    {
        if (valueText == null)
            return;

        float valueWidth = FitValueText(out float measured);

        float total = iconSize + iconValueGap + valueWidth + valuePlusGap + plusSize;
        ContentWidth = total;

        // Left edge of the content group, measured from the entry's centre. Every
        // offset below stays integral because the fixed parts sum to an even
        // number and valueWidth is rounded up to an even width.
        float left = -total * 0.5f;

        PlaceSquare(iconRect, left + iconSize * 0.5f, iconSize);
        PlaceValue(valueText.rectTransform, left + iconSize + iconValueGap + valueWidth * 0.5f, valueWidth);
        PlaceSquare(plusRect, left + total - plusSize * 0.5f, plusSize);

        // TMP can report a zero preferred width before its font asset and
        // material are ready, which happens on the very first pass after a scene
        // load. Leaving the cache empty in that case makes LateUpdate re-measure
        // next frame instead of freezing a "+" that sits too close to the digits.
        bool trustworthy = measured > 0.01f || string.IsNullOrEmpty(valueText.text);
        lastText = trustworthy ? valueText.text : null;
    }

    /// <summary>
    /// Picks the largest font size at or below <see cref="maxFontSize"/> whose
    /// digits fit <see cref="maxValueWidth"/>, and returns the box width to give
    /// them. Long amounts bottom out at <see cref="minFontSize"/> rather than
    /// shrinking away; the gaps on either side absorb the small overhang that
    /// leaves, so the "+" stays adjacent and unoverlapped.
    /// </summary>
    private float FitValueText(out float measured)
    {
        // Owned here, not by TMP: see the class comment.
        valueText.enableAutoSizing = false;

        valueText.fontSize = maxFontSize;
        float width = valueText.GetPreferredValues().x;

        if (width > maxValueWidth && width > 0.01f)
        {
            valueText.fontSize = Mathf.Max(minFontSize, maxFontSize * (maxValueWidth / width));
            width = valueText.GetPreferredValues().x;
        }

        measured = width;

        // Even width keeps the half-width offsets in Relayout on whole units.
        float box = Mathf.Ceil(width);
        if (box % 2f != 0f)
            box += 1f;

        return Mathf.Clamp(box, minValueWidth, maxValueWidth);
    }

    private static void PlaceSquare(RectTransform rect, float centreX, float size)
    {
        if (rect == null)
            return;

        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = new Vector2(Mathf.Round(centreX), 0f);
    }

    // The amount keeps its authored height: only its width tracks the digits.
    private static void PlaceValue(RectTransform rect, float centreX, float width)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        rect.anchoredPosition = new Vector2(Mathf.Round(centreX), 0f);
    }
}
