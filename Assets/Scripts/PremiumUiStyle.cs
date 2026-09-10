using UnityEngine;

/// <summary>
/// Shared visual language for Cat Home's premium UI. Keeping the palette here
/// prevents each independently-authored canvas from drifting into a different
/// orange, cream or shadow treatment.
/// </summary>
public static class PremiumUiStyle
{
    public const string PremiumFontAssetPath = "Assets/Fonts/Fredoka-SemiBold SDF.asset";

    // Approved September 2026 reference palette. Legacy token names remain
    // available to builders; new layouts use surface / ink / action roles.
    public static readonly Color32 Night = new Color32(23, 40, 65, 255);
    public static readonly Color32 Navy = new Color32(23, 40, 65, 255);
    public static readonly Color32 NavyLift = new Color32(42, 65, 95, 255);
    public static readonly Color32 Ink = new Color32(23, 40, 65, 255);
    public static readonly Color32 Ivory = new Color32(247, 250, 255, 255);
    public static readonly Color32 WarmIvory = new Color32(234, 243, 255, 255);
    public static readonly Color32 Mint = new Color32(234, 243, 255, 255);
    public static readonly Color32 Rim = new Color32(202, 222, 246, 255);
    public static readonly Color32 Champagne = new Color32(255, 200, 87, 255);
    public static readonly Color32 ChampagneLight = new Color32(255, 233, 186, 255);
    public static readonly Color32 Coral = new Color32(255, 105, 90, 255);
    public static readonly Color32 CoralLift = new Color32(255, 153, 140, 255);
    public static readonly Color32 Teal = new Color32(39, 121, 245, 255);
    public static readonly Color32 TealLift = new Color32(70, 153, 255, 255);
    public static readonly Color32 Plum = new Color32(149, 111, 232, 255);
    public static readonly Color32 Muted = new Color32(83, 103, 125, 255);
    public static readonly Color32 Disabled = new Color32(175, 195, 216, 255);
    public static readonly Color32 CandySky = new Color32(126, 196, 220, 255);
    public static readonly Color32 CandyAqua = Teal;
    public static readonly Color32 CandyMint = new Color32(136, 192, 159, 255);
    public static readonly Color32 CandyPink = new Color32(224, 158, 169, 255);
    public static readonly Color32 CandyPeach = CoralLift;
    public static readonly Color32 CandyBerry = Plum;
    public static readonly Color32 CandyGrape = new Color32(146, 135, 188, 255);
    public static readonly Color32 CandyLemon = ChampagneLight;
    public static readonly Color32 CandyCloud = Ivory;
    public static readonly Color32 DeepInset = new Color32(31, 56, 90, 255);
    public static readonly Color32 DeepInsetLift = new Color32(48, 79, 119, 255);
    public static readonly Color32 Shadow = new Color32(23, 51, 86, 32);
    public static readonly Color32 SoftShadow = new Color32(23, 51, 86, 16);
    public static readonly Color32 WhiteHighlight = new Color32(255, 255, 255, 138);
    public static readonly Color32 DarkEdge = new Color32(104, 94, 70, 100);
    public static readonly Color32 WarmFacet = new Color32(255, 242, 209, 18);

    public static void ConfigureDarkSurface(LowPolyPanelGraphic graphic, float cornerCut, float bevelWidth)
    {
        // Historical shell entry point now resolves to a clean content sheet.
        ConfigureLightSurface(graphic, cornerCut, bevelWidth);
    }

    public static void ConfigureDeepInsetSurface(LowPolyPanelGraphic graphic, float cornerCut, float bevelWidth)
    {
        ConfigureAccentSurface(graphic, DeepInsetLift, DeepInset, cornerCut, bevelWidth);
    }

    public static void ConfigureLightSurface(LowPolyPanelGraphic graphic, float cornerCut, float bevelWidth)
    {
        graphic.ConfigureModernStyle(Color.white, Ivory, cornerCut, cornerCut >= 20f);
    }

    public static void ConfigureAccentSurface(LowPolyPanelGraphic graphic, Color top, Color bottom,
        float cornerCut, float bevelWidth)
    {
        graphic.ConfigureModernStyle(top, bottom, cornerCut, false);
    }

    public static void ConfigureMetalSurface(LowPolyPanelGraphic graphic, Color baseColor,
        float cornerCut, float bevelWidth)
    {
        graphic.ConfigureModernStyle(Color.Lerp(baseColor, Color.white, .16f), baseColor, cornerCut, false);
    }

    public static void ConfigureShadowSurface(
        LowPolyPanelGraphic graphic,
        Color shadowColor,
        float cornerCut)
    {
        Color top = shadowColor;
        top.a *= 0.72f;
        Color bottom = shadowColor;
        graphic.ConfigurePremiumStyle(
            top,
            bottom,
            cornerCut,
            0f,
            Color.clear,
            Color.clear,
            Color.clear);
        graphic.ConfigureCandyPolish(0f, 0f);
    }

    /// <summary>
    /// Places a stretched depth surface on the same centre as its parent and
    /// expands it evenly on every edge. Translated +X/-Y drop shadows make the
    /// right and bottom edges read as missing button fill.
    /// </summary>
    public static void SetCenteredShadowStretch(RectTransform rect, float expansion)
    {
        if (rect == null)
            return;

        float safeExpansion = Mathf.Max(0f, expansion);
        Vector2 inset = Vector2.one * safeExpansion;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = -inset;
        rect.offsetMax = inset;
    }

    /// <summary>
    /// Places a fixed-size depth surface on the exact centre of its paired
    /// surface and gives it an even outline on all four sides.
    /// </summary>
    public static void SetCenteredShadowRect(
        RectTransform rect,
        Vector2 anchor,
        Vector2 position,
        Vector2 surfaceSize,
        float expansion)
    {
        if (rect == null)
            return;

        float safeExpansion = Mathf.Max(0f, expansion);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = surfaceSize + Vector2.one * safeExpansion * 2f;
    }

    public static void ConfigureSurface(
        LowPolyPanelGraphic graphic,
        Color color,
        float cornerCut,
        float bevelWidth)
    {
        if (color.a < 0.01f)
        {
            ConfigureShadowSurface(graphic, Color.clear, cornerCut);
            return;
        }

        if (Approximately(color, Shadow) || Approximately(color, SoftShadow))
        {
            ConfigureShadowSurface(graphic, color, cornerCut);
            return;
        }

        if (Approximately(color, Champagne) || Approximately(color, ChampagneLight))
        {
            ConfigureMetalSurface(graphic, color, cornerCut, bevelWidth);
            return;
        }

        if (Approximately(color, Night) || Approximately(color, Navy) ||
            Approximately(color, NavyLift) || Approximately(color, Ink) ||
            (color.r < 0.25f && color.g < 0.35f && color.b < 0.42f))
        {
            ConfigureDarkSurface(graphic, cornerCut, Mathf.Min(4.8f, bevelWidth));
            return;
        }

        if (Approximately(color, Ivory) || Approximately(color, WarmIvory))
        {
            ConfigureLightSurface(graphic, cornerCut, Mathf.Min(3.8f, bevelWidth));
            return;
        }

        ConfigureAccentSurface(
            graphic,
            Color.Lerp(color, Color.white, 0.18f),
            Color.Lerp(color, DeepInset, 0.08f),
            cornerCut,
            Mathf.Min(4.5f, bevelWidth));
    }

    private static bool Approximately(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.01f &&
               Mathf.Abs(a.g - b.g) < 0.01f &&
               Mathf.Abs(a.b - b.b) < 0.01f &&
               Mathf.Abs(a.a - b.a) < 0.03f;
    }
}
