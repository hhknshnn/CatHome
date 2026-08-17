using UnityEngine;
using UnityEngine.UI;

public sealed class LowPolyPanelGraphic : MaskableGraphic
{
    [SerializeField, Min(0f)] private float cornerCut = 22f;
    [SerializeField, Min(0f)] private float bevelWidth = 9f;
    [SerializeField] private Color topLeftHighlight = new Color32(255, 255, 255, 90);
    [SerializeField] private Color bottomRightShadow = new Color32(73, 42, 61, 105);
    [SerializeField] private Color warmFacet = new Color32(255, 203, 155, 58);
    [SerializeField] private bool useVerticalGradient;
    [SerializeField] private Color gradientTop = Color.white;
    [SerializeField] private Color gradientBottom = Color.white;
    [SerializeField] private bool useRoundedCorners;
    [SerializeField, Range(4, 24)] private int cornerSegments = 12;
    [SerializeField, Range(0f, 0.5f)] private float candyGlossStrength = 0.24f;
    [SerializeField, Range(0f, 0.35f)] private float innerGlowStrength = 0.1f;

    private float runtimeGlossPhase = -1f;
    private float runtimeGlossStrength;
    private Vector2[] outerPoints;
    private Vector2[] innerPoints;
    private Vector2[] glossPoints;
    private Vector2[] glowPoints;

    public void ConfigureTutorialStyle(Color baseColor, float cut, float bevel)
    {
        PremiumUiStyle.ConfigureSurface(this, baseColor, cut, bevel);
    }

    public void ConfigurePremiumStyle(
        Color top,
        Color bottom,
        float cut,
        float bevel,
        Color edgeHighlight,
        Color edgeShadow,
        Color facet)
    {
        color = bottom;
        gradientTop = top;
        gradientBottom = bottom;
        useVerticalGradient = true;
        useRoundedCorners = true;
        cornerSegments = 18;
        cornerCut = Mathf.Max(0f, cut);
        bevelWidth = Mathf.Max(0f, bevel);
        topLeftHighlight = edgeHighlight;
        bottomRightShadow = edgeShadow;
        warmFacet = facet;
        candyGlossStrength = 0.24f;
        innerGlowStrength = 0.1f;
        SetVerticesDirty();
    }

    public void ConfigureCandyPolish(float glossStrength, float glowStrength)
    {
        candyGlossStrength = Mathf.Clamp(glossStrength, 0f, 0.5f);
        innerGlowStrength = Mathf.Clamp(glowStrength, 0f, 0.35f);
        useRoundedCorners = true;
        cornerSegments = Mathf.Max(18, cornerSegments);
        SetVerticesDirty();
    }

    public void SetRuntimeGloss(float phase, float strength)
    {
        float safePhase = phase < 0f ? -1f : Mathf.Clamp01(phase);
        float safeStrength = Mathf.Clamp(strength, 0f, 0.65f);
        if (Mathf.Abs(runtimeGlossPhase - safePhase) < 0.015f &&
            Mathf.Abs(runtimeGlossStrength - safeStrength) < 0.015f)
        {
            return;
        }

        runtimeGlossPhase = safePhase;
        runtimeGlossStrength = safeStrength;
        SetVerticesDirty();
    }

    public void SetPremiumBaseColor(Color baseColor)
    {
        color = baseColor;
        gradientTop = Color.Lerp(baseColor, Color.white, 0.12f);
        gradientBottom = Color.Lerp(baseColor, Color.black, 0.08f);
        topLeftHighlight = new Color32(255, 255, 255, 105);
        bottomRightShadow = new Color32(8, 17, 25, 130);
        useVerticalGradient = true;
        useRoundedCorners = true;
        cornerSegments = 18;
        candyGlossStrength = 0.24f;
        innerGlowStrength = 0.1f;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0.01f || rect.height <= 0.01f)
            return;

        bool shadowSurface = IsShadowSurface();
        bool metalSurface = IsMetalTone(color) ||
                            (useVerticalGradient && IsMetalTone(gradientTop));
        float surfaceAlpha = useVerticalGradient
            ? Mathf.Max(color.a, Mathf.Max(gradientTop.a, gradientBottom.a))
            : color.a;
        bool rounded = useRoundedCorners || shadowSurface || metalSurface;
        int segments = rounded ? Mathf.Max(12, cornerSegments) : cornerSegments;
        float effectiveBevel = shadowSurface
            ? 0f
            : metalSurface ? Mathf.Min(3.8f, bevelWidth) : Mathf.Min(6f, bevelWidth);
        Color effectiveHighlight = shadowSurface
            ? Color.clear
            : metalSurface ? new Color32(255, 239, 190, 220) : FadeAlpha(topLeftHighlight, 0.92f);
        Color effectiveShadow = shadowSurface
            ? Color.clear
            : metalSurface ? new Color32(91, 56, 24, 170) : FadeAlpha(bottomRightShadow, 0.9f);
        Color effectiveFacet = shadowSurface ? Color.clear : FadeAlpha(warmFacet, 0.48f);
        effectiveHighlight = FadeAlpha(effectiveHighlight, surfaceAlpha);
        effectiveShadow = FadeAlpha(effectiveShadow, surfaceAlpha);
        effectiveFacet = FadeAlpha(effectiveFacet, surfaceAlpha);

        float cornerLimit = rounded ? 0.5f : 0.22f;
        float cut = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * cornerLimit);
        float bevel = Mathf.Min(effectiveBevel, cut * 0.75f);
        Vector2[] outer = rounded
            ? CreateRoundedRect(ref outerPoints, rect, cut, segments)
            : CreateOctagon(ref outerPoints, rect, cut);

        Rect innerRect = new Rect(
            rect.xMin + bevel,
            rect.yMin + bevel,
            Mathf.Max(0f, rect.width - bevel * 2f),
            Mathf.Max(0f, rect.height - bevel * 2f));
        Vector2[] inner = rounded
            ? CreateRoundedRect(ref innerPoints, innerRect, Mathf.Max(0f, cut - bevel), segments)
            : CreateOctagon(ref innerPoints, innerRect, Mathf.Max(0f, cut - bevel));

        if (useVerticalGradient)
            AddGradientPolygon(
                vh,
                inner,
                rect,
                shadowSurface ? color : gradientBottom,
                shadowSurface ? FadeAlpha(color, 0.78f) : gradientTop);
        else
            AddPolygon(vh, inner, color);

        bool hasVisibleBevel = bevel > 0.001f &&
                               (effectiveHighlight.a > 0.001f || effectiveShadow.a > 0.001f);
        if (hasVisibleBevel)
        {
            // One closed ring: dark/gold silhouette plus the light grey stroke.
            // Skipping the bottom-right quadrant left a C-shaped outline on every
            // pill (SHOP, GAMES, need bars, cards). Soft top shine stays in
            // AddCandyGloss; it must not punch a hole in the perimeter.
            Color frameRim = Color.Lerp(effectiveShadow, new Color(0.55f, 0.32f, 0.12f, 1f), 0.35f);
            frameRim.a = Mathf.Clamp01(
                Mathf.Max(0.78f, effectiveShadow.a) * Mathf.Clamp01(surfaceAlpha));
            Color edgeLight = effectiveHighlight;
            edgeLight.a = Mathf.Clamp01(Mathf.Max(0.42f, effectiveHighlight.a * 0.55f));
            for (int i = 0; i < outer.Length; i++)
            {
                int next = (i + 1) % outer.Length;
                AddQuad(vh, outer[i], outer[next], inner[next], inner[i], frameRim);
                AddQuad(vh, outer[i], outer[next], inner[next], inner[i], edgeLight);
            }
        }

        if (!shadowSurface)
        {
            AddCandyGloss(
                vh, innerRect, cut, segments,
                candyGlossStrength * surfaceAlpha, ref glossPoints);
            AddInnerGlow(
                vh, innerRect, cut, segments,
                innerGlowStrength * surfaceAlpha, ref glowPoints);
            AddRuntimeGloss(
                vh, innerRect, cut,
                runtimeGlossPhase, runtimeGlossStrength * surfaceAlpha);
        }

        if (!rounded && inner.Length >= 8)
        {
            AddTriangle(vh, inner[0], inner[1], inner[7], effectiveFacet);
            AddTriangle(vh, inner[3], inner[4], inner[5], effectiveFacet);
        }
    }

    private bool IsShadowSurface()
    {
        string objectName = gameObject.name;
        return objectName.IndexOf("Shadow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("Depth", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsMetalTone(Color value)
    {
        return value.r > 0.42f &&
               value.g > value.r * 0.62f &&
               value.g < value.r * 0.9f &&
               value.b < value.g * 0.78f;
    }

    private static Color FadeAlpha(Color value, float multiplier)
    {
        value.a *= multiplier;
        return value;
    }

    private static Vector2[] CreateOctagon(ref Vector2[] points, Rect rect, float cut)
    {
        EnsurePointBuffer(ref points, 8);
        points[0] = new Vector2(rect.xMin + cut, rect.yMin);
        points[1] = new Vector2(rect.xMax - cut, rect.yMin);
        points[2] = new Vector2(rect.xMax, rect.yMin + cut);
        points[3] = new Vector2(rect.xMax, rect.yMax - cut);
        points[4] = new Vector2(rect.xMax - cut, rect.yMax);
        points[5] = new Vector2(rect.xMin + cut, rect.yMax);
        points[6] = new Vector2(rect.xMin, rect.yMax - cut);
        points[7] = new Vector2(rect.xMin, rect.yMin + cut);
        return points;
    }

    private static Vector2[] CreateRoundedRect(
        ref Vector2[] points,
        Rect rect,
        float radius,
        int segments)
    {
        float clampedRadius = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        if (clampedRadius <= 0.01f)
        {
            EnsurePointBuffer(ref points, 4);
            points[0] = new Vector2(rect.xMin, rect.yMin);
            points[1] = new Vector2(rect.xMax, rect.yMin);
            points[2] = new Vector2(rect.xMax, rect.yMax);
            points[3] = new Vector2(rect.xMin, rect.yMax);
            return points;
        }

        int clampedSegments = Mathf.Clamp(segments, 4, 24);
        EnsurePointBuffer(ref points, clampedSegments * 4);

        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center;
            switch (corner)
            {
                case 0:
                    center = new Vector2(rect.xMin + clampedRadius, rect.yMin + clampedRadius);
                    break;
                case 1:
                    center = new Vector2(rect.xMax - clampedRadius, rect.yMin + clampedRadius);
                    break;
                case 2:
                    center = new Vector2(rect.xMax - clampedRadius, rect.yMax - clampedRadius);
                    break;
                default:
                    center = new Vector2(rect.xMin + clampedRadius, rect.yMax - clampedRadius);
                    break;
            }
            float startAngle = -180f + corner * 90f;
            for (int segment = 0; segment < clampedSegments; segment++)
            {
                // Inclusive endpoints keep each 90° corner closed. Exclusive
                // sampling left a visible hole at the bottom-right tip of every
                // LowPolyPanelGraphic frame.
                float t = clampedSegments == 1 ? 0f : segment / (float)(clampedSegments - 1);
                float radians = Mathf.Deg2Rad * (startAngle + 90f * t);
                points[corner * clampedSegments + segment] = center +
                    new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * clampedRadius;
            }
        }

        return points;
    }

    private static void EnsurePointBuffer(ref Vector2[] points, int length)
    {
        if (points == null || points.Length != length)
            points = new Vector2[length];
    }

    private static void AddPolygon(VertexHelper vh, Vector2[] points, Color tint)
    {
        int start = vh.currentVertCount;
        for (int i = 0; i < points.Length; i++)
            AddVertex(vh, points[i], tint);

        for (int i = 1; i < points.Length - 1; i++)
            vh.AddTriangle(start, start + i, start + i + 1);
    }

    private static void AddGradientPolygon(
        VertexHelper vh,
        Vector2[] points,
        Rect rect,
        Color bottom,
        Color top)
    {
        int start = vh.currentVertCount;
        float height = Mathf.Max(1f, rect.height);
        for (int i = 0; i < points.Length; i++)
        {
            float t = Mathf.Clamp01((points[i].y - rect.yMin) / height);
            AddVertex(vh, points[i], Color.Lerp(bottom, top, t));
        }

        for (int i = 1; i < points.Length - 1; i++)
            vh.AddTriangle(start, start + i, start + i + 1);
    }

    private static void AddCandyGloss(
        VertexHelper vh,
        Rect innerRect,
        float radius,
        int segments,
        float strength,
        ref Vector2[] points)
    {
        if (strength <= 0.001f || innerRect.width <= 2f || innerRect.height <= 2f)
            return;

        float sideInset = Mathf.Min(radius * 0.42f, innerRect.width * 0.1f);
        Rect glossRect = new Rect(
            innerRect.xMin + sideInset,
            Mathf.Lerp(innerRect.yMin, innerRect.yMax, 0.52f),
            Mathf.Max(0f, innerRect.width - sideInset * 2f),
            innerRect.height * 0.39f);
        Vector2[] gloss = CreateRoundedRect(
            ref points,
            glossRect,
            Mathf.Min(radius * 0.66f, glossRect.height * 0.5f),
            segments);
        AddGradientPolygon(
            vh,
            gloss,
            glossRect,
            new Color(1f, 1f, 1f, strength * 0.03f),
            new Color(1f, 1f, 1f, strength));
    }

    private static void AddInnerGlow(
        VertexHelper vh,
        Rect innerRect,
        float radius,
        int segments,
        float strength,
        ref Vector2[] points)
    {
        if (strength <= 0.001f)
            return;

        float inset = Mathf.Clamp(Mathf.Min(innerRect.width, innerRect.height) * 0.055f, 2f, 9f);
        Rect glowRect = new Rect(
            innerRect.xMin + inset,
            innerRect.yMin + inset,
            Mathf.Max(0f, innerRect.width - inset * 2f),
            Mathf.Max(0f, innerRect.height - inset * 2f));
        Vector2[] glow = CreateRoundedRect(
            ref points,
            glowRect,
            Mathf.Max(0f, radius - inset),
            segments);
        AddInsetGradientRing(
            vh,
            glow,
            innerRect.center,
            1.5f,
            new Color(1f, 0.92f, 0.68f, strength),
            new Color(1f, 0.92f, 0.68f, 0f));
    }

    private static void AddRuntimeGloss(
        VertexHelper vh,
        Rect innerRect,
        float radius,
        float phase,
        float strength)
    {
        if (phase < 0f || strength <= 0.001f)
            return;

        float safeInset = Mathf.Min(radius * 0.58f, innerRect.height * 0.28f);
        float yMin = innerRect.yMin + safeInset;
        float yMax = innerRect.yMax - safeInset;
        if (yMax <= yMin)
            return;

        float xInset = Mathf.Max(1f, safeInset * 0.55f);
        float xMin = innerRect.xMin + xInset;
        float xMax = innerRect.xMax - xInset;
        if (xMax <= xMin)
            return;

        float travel = innerRect.width * 0.68f;
        float centerX = Mathf.Lerp(innerRect.center.x - travel * 0.5f,
            innerRect.center.x + travel * 0.5f, phase);
        // A premium sheen is a thin translucent glass streak, not a wide bright
        // slash. The previous 30px half-width at full alpha cut straight through
        // the centered label; keep it narrow and faint so it reads as a moving
        // highlight the eye barely catches.
        float halfWidth = Mathf.Clamp(innerRect.width * 0.024f, 2f, 11f);
        float slant = innerRect.height * 0.16f;
        Color soft = new Color(1f, 1f, 1f, strength * 0.05f);
        Color bright = new Color(1f, 1f, 1f, strength * 0.4f);
        AddQuad(
            vh,
            new Vector2(Mathf.Clamp(centerX - halfWidth - slant, xMin, xMax), yMin),
            new Vector2(Mathf.Clamp(centerX - halfWidth + slant, xMin, xMax), yMax),
            new Vector2(Mathf.Clamp(centerX + slant, xMin, xMax), yMax),
            new Vector2(Mathf.Clamp(centerX - slant, xMin, xMax), yMin),
            soft);
        AddQuad(
            vh,
            new Vector2(Mathf.Clamp(centerX - slant, xMin, xMax), yMin),
            new Vector2(Mathf.Clamp(centerX + slant, xMin, xMax), yMax),
            new Vector2(Mathf.Clamp(centerX + halfWidth + slant, xMin, xMax), yMax),
            new Vector2(Mathf.Clamp(centerX + halfWidth - slant, xMin, xMax), yMin),
            bright);
    }

    private static void AddInsetGradientRing(
        VertexHelper vh,
        Vector2[] outer,
        Vector2 center,
        float inset,
        Color outerColor,
        Color innerColor)
    {
        if (outer == null || outer.Length < 3)
            return;

        int start = vh.currentVertCount;
        for (int i = 0; i < outer.Length; i++)
        {
            AddVertex(vh, outer[i], outerColor);
            Vector2 direction = (center - outer[i]).normalized * inset;
            AddVertex(vh, outer[i] + direction, innerColor);
        }

        for (int i = 0; i < outer.Length; i++)
        {
            int next = (i + 1) % outer.Length;
            int outerA = start + i * 2;
            int innerA = outerA + 1;
            int outerB = start + next * 2;
            int innerB = outerB + 1;
            vh.AddTriangle(outerA, outerB, innerB);
            vh.AddTriangle(outerA, innerB, innerA);
        }
    }

    private static void AddQuad(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Vector2 d,
        Color tint)
    {
        int start = vh.currentVertCount;
        AddVertex(vh, a, tint);
        AddVertex(vh, b, tint);
        AddVertex(vh, c, tint);
        AddVertex(vh, d, tint);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }

    private static void AddTriangle(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Color tint)
    {
        int start = vh.currentVertCount;
        AddVertex(vh, a, tint);
        AddVertex(vh, b, tint);
        AddVertex(vh, c, tint);
        vh.AddTriangle(start, start + 1, start + 2);
    }

    private static void AddVertex(VertexHelper vh, Vector2 position, Color tint)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = tint;
        vh.AddVert(vertex);
    }
}
