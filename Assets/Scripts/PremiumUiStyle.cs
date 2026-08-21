using UnityEngine;

/// <summary>
/// Shared visual language for Cat Home's premium UI. Keeping the palette here
/// prevents each independently-authored canvas from drifting into a different
/// orange, cream or shadow treatment.
/// </summary>
public static class PremiumUiStyle
{
    public const string PremiumFontAssetPath = "Assets/Fonts/Fredoka-SemiBold SDF.asset";

    // Bright candy tokens are the default surface language. Night/Ink remain
    // available for copy, masks and deliberately recessed wells; they are no
    // longer the colour that a generic panel recipe paints across the screen.
    public static readonly Color32 Night = new Color32(35, 27, 72, 255);
    public static readonly Color32 Navy = new Color32(48, 119, 181, 255);
    public static readonly Color32 NavyLift = new Color32(43, 195, 191, 255);
    public static readonly Color32 Ink = new Color32(50, 35, 68, 255);
    public static readonly Color32 Ivory = new Color32(255, 250, 235, 255);
    public static readonly Color32 WarmIvory = new Color32(255, 236, 207, 255);
    public static readonly Color32 Champagne = new Color32(255, 187, 43, 255);
    public static readonly Color32 ChampagneLight = new Color32(255, 235, 119, 255);
    public static readonly Color32 Coral = new Color32(246, 82, 132, 255);
    public static readonly Color32 CoralLift = new Color32(255, 126, 91, 255);
    public static readonly Color32 Teal = new Color32(39, 190, 180, 255);
    public static readonly Color32 TealLift = new Color32(83, 224, 195, 255);
    public static readonly Color32 Plum = new Color32(169, 75, 206, 255);
    public static readonly Color32 Muted = new Color32(105, 86, 116, 255);
    public static readonly Color32 Disabled = new Color32(148, 139, 159, 255);

    public static readonly Color32 CandySky = new Color32(83, 184, 238, 255);
    public static readonly Color32 CandyAqua = new Color32(41, 202, 194, 255);
    public static readonly Color32 CandyMint = new Color32(84, 216, 161, 255);
    public static readonly Color32 CandyPink = new Color32(246, 83, 143, 255);
    public static readonly Color32 CandyPeach = new Color32(255, 132, 82, 255);
    public static readonly Color32 CandyBerry = new Color32(187, 75, 196, 255);
    public static readonly Color32 CandyGrape = new Color32(126, 86, 204, 255);
    public static readonly Color32 CandyLemon = new Color32(255, 207, 61, 255);
    public static readonly Color32 CandyCloud = new Color32(255, 246, 226, 255);
    public static readonly Color32 DeepInset = new Color32(43, 31, 84, 255);
    public static readonly Color32 DeepInsetLift = new Color32(70, 66, 137, 255);

    public static readonly Color32 Shadow = new Color32(57, 27, 82, 112);
    public static readonly Color32 SoftShadow = new Color32(75, 34, 104, 46);
    public static readonly Color32 WhiteHighlight = new Color32(255, 255, 255, 138);
    // Alpha kept high enough that the continuous LowPolyPanelGraphic rim still
    // reads on cream/peach parents — the old 116a rim vanished on the BR edge.
    public static readonly Color32 DarkEdge = new Color32(64, 30, 91, 188);
    public static readonly Color32 WarmFacet = new Color32(255, 196, 225, 62);

    public static void ConfigureDarkSurface(
        LowPolyPanelGraphic graphic,
        float cornerCut,
        float bevelWidth)
    {
        // Kept for builder compatibility: legacy callers used "dark surface"
        // for their default shell. That default is now a glossy sky/aqua candy
        // recipe. Truly recessed areas should call ConfigureDeepInsetSurface.
        graphic.ConfigurePremiumStyle(
            new Color32(91, 214, 228, 255),
            new Color32(42, 151, 207, 255),
            cornerCut,
            bevelWidth,
            new Color32(255, 255, 255, 190),
            new Color32(55, 35, 112, 120),
            new Color32(255, 206, 234, 76));
        graphic.ConfigureCandyPolish(0.25f, 0.1f);
    }

    public static void ConfigureDeepInsetSurface(
        LowPolyPanelGraphic graphic,
        float cornerCut,
        float bevelWidth)
    {
        graphic.ConfigurePremiumStyle(
            DeepInsetLift,
            DeepInset,
            cornerCut,
            bevelWidth,
            new Color32(255, 255, 255, 66),
            new Color32(27, 19, 58, 170),
            new Color32(112, 226, 255, 26));
        graphic.ConfigureCandyPolish(0.08f, 0.035f);
    }

    public static void ConfigureLightSurface(
        LowPolyPanelGraphic graphic,
        float cornerCut,
        float bevelWidth)
    {
        graphic.ConfigurePremiumStyle(
            new Color32(255, 253, 246, 255),
            new Color32(255, 236, 210, 255),
            cornerCut,
            bevelWidth,
            new Color32(255, 255, 255, 220),
            new Color32(130, 72, 117, 78),
            new Color32(255, 184, 214, 58));
        graphic.ConfigureCandyPolish(0.22f, 0.075f);
    }

    public static void ConfigureAccentSurface(
        LowPolyPanelGraphic graphic,
        Color top,
        Color bottom,
        float cornerCut,
        float bevelWidth)
    {
        Color candyTop = Color.Lerp(top, Color.white, 0.08f);
        Color candyBottom = Color.Lerp(bottom, DeepInset, 0.025f);
        graphic.ConfigurePremiumStyle(
            candyTop,
            candyBottom,
            cornerCut,
            Mathf.Max(3.5f, bevelWidth),
            new Color32(255, 255, 255, 210),
            DarkEdge,
            WarmFacet);
        graphic.ConfigureCandyPolish(0.28f, 0.1f);
    }

    public static void ConfigureMetalSurface(
        LowPolyPanelGraphic graphic,
        Color baseColor,
        float cornerCut,
        float bevelWidth)
    {
        Color top = Color.Lerp(baseColor, Color.white, 0.48f);
        Color bottom = Color.Lerp(baseColor, new Color32(221, 111, 25, 255), 0.28f);
        graphic.ConfigurePremiumStyle(
            top,
            bottom,
            cornerCut,
            Mathf.Min(4f, bevelWidth),
            new Color32(255, 249, 197, 235),
            new Color32(128, 58, 18, 165),
            new Color32(255, 247, 175, 72));
        graphic.ConfigureCandyPolish(0.34f, 0.13f);
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
