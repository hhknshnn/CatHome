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
    public static readonly Color32 Night = new Color32(41, 58, 59, 255);
    public static readonly Color32 Navy = new Color32(41, 58, 59, 255);
    public static readonly Color32 NavyLift = new Color32(64, 91, 89, 255);
    public static readonly Color32 Ink = new Color32(36, 53, 54, 255);
    public static readonly Color32 Ivory = new Color32(255, 249, 239, 255);
    public static readonly Color32 WarmIvory = new Color32(249, 239, 220, 255);
    public static readonly Color32 Mint = new Color32(221, 237, 227, 255);
    public static readonly Color32 Rim = new Color32(211, 193, 155, 255);
    public static readonly Color32 Champagne = new Color32(228, 173, 62, 255);
    public static readonly Color32 ChampagneLight = new Color32(248, 215, 135, 255);
    public static readonly Color32 Coral = new Color32(245, 120, 108, 255);
    public static readonly Color32 CoralLift = new Color32(255, 153, 134, 255);
    public static readonly Color32 Teal = new Color32(33, 143, 135, 255);
    public static readonly Color32 TealLift = new Color32(71, 164, 153, 255);
    public static readonly Color32 Plum = new Color32(153, 124, 170, 255);
    public static readonly Color32 Muted = new Color32(95, 111, 105, 255);
    public static readonly Color32 Disabled = new Color32(146, 151, 141, 255);
    public static readonly Color32 CandySky = new Color32(126, 196, 220, 255);
    public static readonly Color32 CandyAqua = Teal;
    public static readonly Color32 CandyMint = new Color32(136, 192, 159, 255);
    public static readonly Color32 CandyPink = new Color32(224, 158, 169, 255);
    public static readonly Color32 CandyPeach = CoralLift;
    public static readonly Color32 CandyBerry = Plum;
    public static readonly Color32 CandyGrape = new Color32(146, 135, 188, 255);
    public static readonly Color32 CandyLemon = ChampagneLight;
    public static readonly Color32 CandyCloud = Ivory;
    public static readonly Color32 DeepInset = new Color32(48, 75, 73, 255);
    public static readonly Color32 DeepInsetLift = new Color32(64, 91, 89, 255);
    public static readonly Color32 Shadow = new Color32(70, 63, 44, 38);
    public static readonly Color32 SoftShadow = new Color32(70, 63, 44, 20);
    public static readonly Color32 WhiteHighlight = new Color32(255, 255, 255, 138);
    public static readonly Color32 DarkEdge = new Color32(104, 94, 70, 100);
    public static readonly Color32 WarmFacet = new Color32(255, 242, 209, 18);

    public static void ConfigureDarkSurface(LowPolyPanelGraphic graphic, float cornerCut, float bevelWidth)
    {
        // Historical default-shell entry point. Surfaces now use warm ivory.
        ConfigureLightSurface(graphic, cornerCut, bevelWidth);
    }

    public static void ConfigureDeepInsetSurface(LowPolyPanelGraphic graphic, float cornerCut, float bevelWidth)
    {
        ConfigureAccentSurface(graphic, DeepInsetLift, DeepInset, cornerCut, bevelWidth);
    }

    public static void ConfigureLightSurface(LowPolyPanelGraphic graphic, float cornerCut, float bevelWidth)
    {
        graphic.ConfigurePremiumStyle(new Color32(255, 253, 247, 255), Ivory,
            cornerCut, 2.4f, new Color32(255, 255, 255, 235),
            new Color32(179, 153, 104, 148), new Color32(255, 242, 214, 18));
        graphic.ConfigureCandyPolish(.055f, .025f);
    }

    public static void ConfigureAccentSurface(LowPolyPanelGraphic graphic, Color top, Color bottom,
        float cornerCut, float bevelWidth)
    {
        graphic.ConfigurePremiumStyle(top, bottom, cornerCut, 3f,
            new Color32(255, 255, 255, 150), DarkEdge, WarmFacet);
        graphic.ConfigureCandyPolish(.08f, .035f);
    }

    public static void ConfigureMetalSurface(LowPolyPanelGraphic graphic, Color baseColor,
        float cornerCut, float bevelWidth)
    {
        graphic.ConfigurePremiumStyle(Color.Lerp(baseColor, Color.white, .30f), baseColor,
            cornerCut, Mathf.Min(2f, bevelWidth), new Color32(255, 253, 221, 200),
            new Color32(137, 102, 49, 130), new Color32(255, 245, 192, 24));
        graphic.ConfigureCandyPolish(.10f, .035f);
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
