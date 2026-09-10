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
    [SerializeField] private bool softElevation;
    private Vector2[] elevationPoints;
    private Vector2[] framePoints;
    private Vector2[] frameFringePoints;
    private Vector2[] frameInsetPoints;
    [SerializeField] private bool referenceFinish;
    [SerializeField] private bool modernFinish;
    [SerializeField] private bool modernAction;
    [SerializeField] private bool playfulAction;
    private bool modernPressed;
    private bool modernFocused;
    private bool modernDisabled;

    /// <summary>Content surfaces and controls share a thin, cool edge. Unlike
    /// the legacy enamel treatment, the authored corner radius is never
    /// expanded to half the height of a card.</summary>
    public void ConfigureModernStyle(Color top, Color bottom, float radius,
        bool elevated = false, bool action = false)
    {
        ConfigurePremiumStyle(top, bottom, radius, 1f, Color.clear, Color.clear, Color.clear);
        modernFinish = true;
        modernAction = action;
        playfulAction = false;
        referenceFinish = false;
        softElevation = elevated;
        cornerSegments = 12;
        candyGlossStrength = innerGlowStrength = 0f;
        SetVerticesDirty();
    }
    public void ConfigurePlayfulAction(Color top,Color bottom,float radius)
    {ConfigureModernStyle(top,bottom,radius,true,true);playfulAction=true;SetVerticesDirty();}

    public void SetInteractionState(bool pressed, bool focused, bool disabled)
    {
        if (modernPressed == pressed && modernFocused == focused && modernDisabled == disabled) return;
        modernPressed = pressed;
        modernFocused = focused;
        modernDisabled = disabled;
        if (modernFinish) SetVerticesDirty();
    }

    public void ConfigureReferenceFinish(bool enabled)
    {
        referenceFinish=enabled;SetVerticesDirty();
    }

    public void ConfigureElevation(bool enabled)
    {
        softElevation = enabled;
        SetVerticesDirty();
    }

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
        if (modernFinish)
        {
            ConfigureModernStyle(Color.Lerp(baseColor, Color.white, modernAction ? .10f : .035f),
                baseColor, cornerCut, softElevation, modernAction);
            return;
        }
        PremiumUiStyle.ConfigureAccentSurface(this, Color.Lerp(baseColor, Color.white, .035f),
            baseColor, cornerCut, Mathf.Min(2f, bevelWidth));
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0.01f || rect.height <= 0.01f)
            return;

        if (modernFinish)
        {
            DrawModernSurface(vh, rect);
            return;
        }

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
        if(referenceFinish && !shadowSurface && surfaceAlpha>.98f && rect.width>=88f && rect.height>=48f && cut>=12f)
        {
            DrawReferenceSurface(vh,rect,cut,segments);
            return;
        }
        float bevel = Mathf.Min(effectiveBevel, cut * 0.75f);
        Vector2[] outer = rounded
            ? CreateRoundedRect(ref outerPoints, rect, cut, segments)
            : CreateOctagon(ref outerPoints, rect, cut);

        if (softElevation && !shadowSurface && surfaceAlpha > .98f && rect.width > 90f && rect.height > 48f)
        {
            // Diffuse contact depth belongs to this graphic, never an interactive
            // sibling or an extra Shadow component that can intercept a pointer.
            for (int layer = 6; layer >= 1; layer--)
            {
                float spread = layer * 1.15f;
                Rect shadowRect = new Rect(rect.xMin-spread, rect.yMin-spread-2.5f, rect.width+spread*2, rect.height+spread*2);
                var points = CreateRoundedRect(ref elevationPoints, shadowRect, cut+spread, segments);
                AddPolygon(vh, points, new Color(.20f,.17f,.10f, .012f));
            }
        }

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
            for (int i = 0; i < outer.Length; i++)
            {
                int next = (i + 1) % outer.Length;
                float height = Mathf.InverseLerp(rect.yMin, rect.yMax, (outer[i].y + outer[next].y) * .5f);
                Color rim = Color.Lerp(effectiveShadow, effectiveHighlight, height);
                AddQuad(vh, outer[i], outer[next], inner[next], inner[i], rim);
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

    private void DrawReferenceSurface(VertexHelper vh, Rect rect, float radius, int segments)
    {
        // Nested enamel and champagne lips. All decoration stays inside the
        // same raycast surface, so art cannot steal the button's pointer.
        radius=Mathf.Min(rect.height*.5f,Mathf.Max(radius,rect.height<120f?rect.height*.48f:30f));
        if(softElevation)
            for(int layer=8;layer>=1;layer--)
            {
                float spread=layer*1.0f;
                var depth=new Rect(rect.xMin-spread,rect.yMin-spread-3f,rect.width+spread*2,rect.height+spread*2);
                AddPolygon(vh,CreateRoundedRect(ref elevationPoints,depth,radius+spread,segments),new Color(.25f,.19f,.09f,.014f));
            }
        bool light=color.r>.72f&&color.g>.70f&&color.b>.58f;
        Color top=light?new Color32(255,253,242,255):Color.Lerp(color,Color.white,.13f);
        Color bottom=light?Color.Lerp(color,new Color32(247,228,190,255),.28f):Color.Lerp(color,new Color32(161,75,60,255),.065f);
        DrawFrameLayer(vh,rect,radius,0,new Color32(195,157,101,235),new Color32(249,224,177,255),segments);
        DrawFrameLayer(vh,rect,radius,1.4f,new Color32(244,225,190,255),new Color32(255,255,249,255),segments);
        DrawFrameLayer(vh,rect,radius,4.0f,new Color32(255,250,230,255),new Color32(255,253,242,255),segments);
        DrawFrameLayer(vh,rect,radius,6.2f,new Color32(216,184,136,255),new Color32(224,197,155,255),segments);
        DrawFrameLayer(vh,rect,radius,7.5f,bottom,top,segments);
        // The inner white hairline catches the key light without a noisy shine band.
        var face=new Rect(rect.xMin+8.5f,rect.yMin+8.5f,rect.width-17f,rect.height-17f);
        AddInnerGlow(vh,face,Mathf.Max(0,radius-8.5f),segments,.08f,ref frameInsetPoints);
    }

    private void DrawModernSurface(VertexHelper vh, Rect rect)
    {
        float radius = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * .5f);
        Color bottom = useVerticalGradient ? gradientBottom : color;
        Color top = useVerticalGradient ? gradientTop : color;
        float alpha = Mathf.Max(bottom.a, top.a);
        if (alpha < .001f) return;
        bool mask = GetComponent<Mask>() != null;
        if (modernDisabled)
        {
            bottom = new Color32(175, 195, 216, 255);
            top = new Color32(194, 211, 228, 255);
        }
        else if (modernPressed)
        {
            bottom = Color.Lerp(bottom, new Color32(15, 51, 113, 255), modernAction ? .25f : .07f);
            top = Color.Lerp(top, bottom, .66f);
        }
        // Three small contact layers replace eight enamel rings and wide
        // warm shadows. Masks and tracks never draw outside their bounds.
        if (softElevation && !mask && alpha > .98f && !modernPressed)
            for (int layer = 3; layer >= 1; layer--)
            {
                float spread = layer * .95f;
                var depth = new Rect(rect.xMin - spread, rect.yMin - spread - 2f,
                    rect.width + spread * 2, rect.height + spread * 2);
                AddPolygon(vh, CreateRoundedRect(ref elevationPoints, depth, radius + spread, 12),
                    new Color(.08f, .20f, .38f, modernAction ? .034f : .022f));
            }
        if (modernFocused && !modernDisabled && !mask)
        {
            var focus = new Rect(rect.xMin - 3, rect.yMin - 3, rect.width + 6, rect.height + 6);
            DrawFrameLayer(vh, focus, radius + 3, 0, new Color32(123, 182, 255, 230), new Color32(170, 213, 255, 230), 12);
        }
        if (radius < .1f || rect.height < 16f || mask || alpha < .98f)
        {
            AddGradientPolygon(vh, CreateRoundedRect(ref outerPoints, rect, radius, 12), rect, bottom, top);
            return;
        }
        Color edgeBottom = modernAction && !modernDisabled ? Color.Lerp(bottom, new Color32(15, 67, 168, 255), .35f) : new Color32(207, 223, 242, 255);
        Color edgeTop = modernAction && !modernDisabled ? Color.Lerp(top, Color.white, .35f) : Color.white;
        if(playfulAction&&!mask)
        {
            float depth=modernPressed?1f:Mathf.Min(6f,rect.height*.085f);
            var foot=new Rect(rect.xMin,rect.yMin-depth,rect.width,rect.height);
            Color baseTone=Color.Lerp(bottom,Color.black,modernDisabled?.12f:.28f);
            DrawFrameLayer(vh,foot,radius,0,baseTone,baseTone,12);
            edgeBottom=Color.Lerp(bottom,Color.black,.18f);
        }
        DrawFrameLayer(vh, rect, radius, 0, edgeBottom, edgeTop, 12);
        DrawFrameLayer(vh, rect, radius, 1.1f, bottom, top, 12);
    }

    private void DrawFrameLayer(VertexHelper vh,Rect rect,float radius,float inset,Color bottom,Color top,int segments)
    {
        var inner=new Rect(rect.xMin+inset,rect.yMin+inset,rect.width-inset*2,rect.height-inset*2);
        var points=CreateRoundedRect(ref framePoints,inner,Mathf.Max(0,radius-inset),segments);
        const float aa=.8f;
        var fringeRect=new Rect(inner.xMin-aa,inner.yMin-aa,inner.width+aa*2,inner.height+aa*2);
        var fringe=CreateRoundedRect(ref frameFringePoints,fringeRect,Mathf.Max(0,radius-inset)+aa,segments);
        for(int i=0;i<points.Length;i++)
        {
            int n=(i+1)%points.Length,start=vh.currentVertCount;
            Color a=Color.Lerp(bottom,top,Mathf.InverseLerp(inner.yMin,inner.yMax,points[i].y));
            Color b=Color.Lerp(bottom,top,Mathf.InverseLerp(inner.yMin,inner.yMax,points[n].y));
            Color clearA=a,clearB=b;clearA.a=clearB.a=0;
            AddVertex(vh,fringe[i],clearA);AddVertex(vh,fringe[n],clearB);
            AddVertex(vh,points[n],b);AddVertex(vh,points[i],a);
            vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
        }
        AddGradientPolygon(vh,points,inner,bottom,top);
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
        float halfWidth = Mathf.Clamp(innerRect.width * 0.012f, 1.5f, 6f);
        float slant = innerRect.height * 0.16f;
        Color soft = new Color(1f, 1f, 1f, strength * 0.025f);
        Color bright = new Color(1f, 1f, 1f, strength * 0.22f);
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
