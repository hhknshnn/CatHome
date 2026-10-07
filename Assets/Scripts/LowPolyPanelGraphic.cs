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
    [SerializeField] private bool storybookFinish;
    private bool screenFinish;
    private bool roomFrameFinish;
    private bool cozyFinish;
    private bool glassFinish;
    private bool glassOutline;
    private bool pearlHudFinish;
    private bool jewelHudFinish;
    private Color jewelRim;
    private bool modernPressed;
    private bool modernFocused;
    private bool modernDisabled;
    private Sprite hudArtwork;
    private int hudJoystickPart;

    public void ConfigureHudJoystickFinish(int part)
    {
        if(hudJoystickPart==part)return;
        hudJoystickPart=part;SetVerticesDirty();
    }

    public override Texture mainTexture => hudArtwork != null ? hudArtwork.texture : base.mainTexture;

    /// <summary>Optional HUD artwork on the existing input graphic; no overlay or new hit area.</summary>
    public void ConfigureHudArtwork(Sprite artwork)
    {
        if (hudArtwork == artwork) return;
        hudArtwork = artwork;
        SetMaterialDirty();
        SetVerticesDirty();
    }

    /// <summary>Content surfaces and controls share a thin, cool edge. Unlike
    /// the legacy enamel treatment, the authored corner radius is never
    /// expanded to half the height of a card.</summary>
    public void ConfigureModernStyle(Color top, Color bottom, float radius,
        bool elevated = false, bool action = false)
    {
        ConfigurePremiumStyle(top, bottom, radius, 1f, Color.clear, Color.clear, Color.clear);
        modernFinish = true;
        pearlHudFinish = false;
        jewelHudFinish = false;
        modernAction = action;
        playfulAction = false;
        storybookFinish = false;
        screenFinish = false;
        roomFrameFinish = false;
        cozyFinish = false;
        glassFinish = false;
        glassOutline = false;
        referenceFinish = false;
        softElevation = elevated;
        cornerSegments = 12;
        candyGlossStrength = innerGlowStrength = 0f;
        SetVerticesDirty();
    }
    public void ConfigurePlayfulAction(Color top,Color bottom,float radius)
    {ConfigureModernStyle(top,bottom,radius,true,true);playfulAction=true;SetVerticesDirty();}

    /// <summary>Quiet content surfaces and satin actions for the storybook screens.
    /// This is opt-in; the home HUD keeps its authored enamel finish.</summary>
    public void ConfigureScreenStyle(Color top, Color bottom, float radius, bool action = false, bool elevated = true)
    {
        ConfigureModernStyle(top, bottom, radius, elevated, action);
        hudPanelFinish = hudPortraitFinish = hudInsetFinish = false;
        screenFinish = true;
        SetVerticesDirty();
    }

    public void ConfigureRoomFrame(float radius)
    {
        ConfigureScreenStyle(new Color32(219, 164, 95, 255), new Color32(160, 99, 52, 255), radius);
        roomFrameFinish = true;
        SetVerticesDirty();
    }

    /// <summary>Wood-edged, softly glazed controls and upholstered content surfaces.</summary>
    public void ConfigureCozyStyle(Color top, Color bottom, float radius, bool action = false, bool elevated = true)
    {
        ConfigureScreenStyle(top, bottom, radius, action, elevated);
        cozyFinish = true;
        SetVerticesDirty();
    }

    /// <summary>Translucent sea glass with actual thin rings: borders never fill the transparent centre.</summary>
    public void ConfigureGlassStyle(Color top, Color bottom, float radius, bool action = false, bool elevated = true)
    {
        ConfigureScreenStyle(top, bottom, radius, action, elevated);
        glassFinish = true;
        SetVerticesDirty();
    }

    public void ConfigureGlassOutline(Color tint, float radius)
    {
        ConfigureGlassStyle(tint, tint, radius, false, false);
        glassOutline = true;
        SetVerticesDirty();
    }

    /// <summary>Opt-in title treatment; other screens retain their authored finish.</summary>
    public void ConfigureStorybookStyle(Color top, Color bottom, float radius)
    {
        ConfigureModernStyle(top, bottom, radius, true, true);
        storybookFinish = true;
        SetVerticesDirty();
    }

    public void SetInteractionState(bool pressed, bool focused, bool disabled)
    {
        if (modernPressed == pressed && modernFocused == focused && modernDisabled == disabled) return;
        modernPressed = pressed;
        modernFocused = focused;
        modernDisabled = disabled;
        if (modernFinish || hudArtwork != null) SetVerticesDirty();
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
        if (glassFinish)
        {
            ConfigureGlassStyle(Color.Lerp(baseColor, StorybookScreenStyle.Mint, .08f),
                baseColor, cornerCut, modernAction, softElevation);
            return;
        }
        if (cozyFinish)
        {
            ConfigureCozyStyle(Color.Lerp(baseColor, StorybookScreenStyle.Cream, .10f),
                baseColor, cornerCut, modernAction, softElevation);
            return;
        }
        if (screenFinish)
        {
            ConfigureScreenStyle(Color.Lerp(baseColor, Color.white, modernAction ? .12f : .025f),
                baseColor, cornerCut, modernAction, softElevation);
            return;
        }
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

        if (hudJoystickPart != 0)
        {
            DrawHudJoystick(vh,rect);
            return;
        }

        if (hudArtwork != null)
        {
            DrawHudArtwork(vh, rect);
            return;
        }

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

    // Final HUD only: polished colored enamel and a restrained metal rim.
    public void ConfigureJewelHudStyle(Color top,Color bottom,Color rim,float radius)
    {
        ConfigureModernStyle(top,bottom,radius,true,true);
        hudPanelFinish=hudPortraitFinish=hudInsetFinish=false;
        jewelHudFinish=true;jewelRim=rim;SetVerticesDirty();
    }
    private void DrawJewelHudSurface(VertexHelper vh,Rect rect)
    {
        float radius=Mathf.Min(cornerCut,Mathf.Min(rect.width,rect.height)*.5f);
        Color top=gradientTop,bottom=gradientBottom,rim=jewelRim;
        if(modernPressed){top=Color.Lerp(top,bottom,.6f);bottom=Color.Lerp(bottom,Color.black,.12f);}
        if(modernDisabled){top=Color.Lerp(top,new Color32(192,207,215,255),.12f);bottom=Color.Lerp(bottom,new Color32(149,174,194,255),.12f);}
        for(int layer=3;layer>=1;layer--)
        {
            float spread=layer*.9f;
            var shadow=new Rect(rect.xMin-spread,rect.yMin-spread-3,rect.width+spread*2,rect.height+spread*2);
            AddPolygon(vh,CreateRoundedRect(ref elevationPoints,shadow,radius+spread,18),new Color(.035f,.065f,.14f,.048f));
        }
        float depth=modernPressed?.5f:2.5f;
        var foot=new Rect(rect.xMin,rect.yMin-depth,rect.width,rect.height);
        DrawFrameLayer(vh,foot,radius,0,Color.Lerp(bottom,Color.black,.28f),bottom,18);
        DrawFrameLayer(vh,rect,radius,0,Color.Lerp(rim,new Color32(117,73,24,255),.42f),Color.Lerp(rim,Color.white,.35f),18);
        DrawFrameLayer(vh,rect,radius,1.1f,Color.Lerp(rim,Color.white,.58f),Color.white,18);
        DrawFrameLayer(vh,rect,radius,2.1f,Color.Lerp(bottom,Color.black,.35f),rim,18);
        var face=new Rect(rect.xMin+3.1f,rect.yMin+3.1f,rect.width-6.2f,rect.height-6.2f);
        AddGradientPolygon(vh,CreateRoundedRect(ref innerPoints,face,Mathf.Max(0,radius-3.1f),18),face,bottom,top);
        if(!modernPressed)
        {
            AddCandyGloss(vh,face,Mathf.Max(0,radius-3.1f),18,.38f,ref glossPoints);
            // A short highlight at the upper rim, not a perpetual moving sheen.
            HudSpark(vh,new Vector2(rect.xMin+radius*.7f,rect.yMax-5.5f),3.4f,new Color(1,1,.95f,.78f));
        }
    }
    private static void HudSpark(VertexHelper vh,Vector2 p,float r,Color color)
    {
        int n=vh.currentVertCount;
        vh.AddVert(p,color,Vector2.zero);
        for(int i=0;i<8;i++)
        {
            float a=Mathf.PI*i*.25f;float distance=i%2==0?r:r*.19f;
            vh.AddVert(p+new Vector2(Mathf.Cos(a)*distance,Mathf.Sin(a)*distance),color,Vector2.zero);
        }
        for(int i=0;i<8;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%8);
    }

    // Opt-in Top HUD V2.1 only; no world or other UI surface uses this finish.
    public void ConfigurePearlHudStyle(Color top, Color bottom, float radius, bool action = false)
    {
        ConfigureModernStyle(top,bottom,radius,true,action);
        hudPanelFinish=hudPortraitFinish=hudInsetFinish=false;
        pearlHudFinish=true;
        SetVerticesDirty();
    }
    private void DrawPearlHudSurface(VertexHelper vh, Rect rect)
    {
        float radius=Mathf.Min(cornerCut,Mathf.Min(rect.width,rect.height)*.5f);
        Color top=gradientTop,bottom=gradientBottom;
        if(modernPressed){top=Color.Lerp(top,bottom,.6f);bottom=Color.Lerp(bottom,new Color32(54,101,130,255),.1f);}
        if(modernDisabled){top=Color.Lerp(top,new Color32(223,236,240,255),.18f);bottom=Color.Lerp(bottom,new Color32(198,218,228,255),.18f);}
        for(int layer=4;layer>=1;layer--)
        {
            float spread=layer*.65f;
            var shadow=new Rect(rect.xMin-spread,rect.yMin-spread-2.2f,rect.width+2*spread,rect.height+2*spread);
            AddPolygon(vh,CreateRoundedRect(ref elevationPoints,shadow,radius+spread,18),new Color(.055f,.12f,.23f,.024f));
        }
        float depth=modernPressed?.6f:1.8f;
        var foot=new Rect(rect.xMin,rect.yMin-depth,rect.width,rect.height);
        Color side=Color.Lerp(bottom,new Color32(109,146,177,255),modernAction?.23f:.18f);
        DrawFrameLayer(vh,foot,radius,0,side,bottom,18);
        var edgeBottom=Color.Lerp(bottom,new Color32(101,145,176,255),.20f);
        DrawFrameLayer(vh,rect,radius,0,edgeBottom,Color.Lerp(top,Color.white,.7f),18);
        var face=new Rect(rect.xMin+.85f,rect.yMin+.85f,rect.width-1.7f,rect.height-1.7f);
        AddGradientPolygon(vh,CreateRoundedRect(ref innerPoints,face,Mathf.Max(0,radius-.85f),18),face,bottom,top);
        if(!modernPressed)AddCandyGloss(vh,face,Mathf.Max(0,radius-1),18,modernAction?.15f:.075f,ref glossPoints);
        if(modernFocused&&!modernDisabled)
            DrawFrameLayer(vh,rect,radius,.8f,Color.Lerp(bottom,new Color32(79,181,189,255),.25f),Color.Lerp(top,Color.white,.6f),18);
    }

    private void DrawModernSurface(VertexHelper vh, Rect rect)
    {
        if (jewelHudFinish) { DrawJewelHudSurface(vh, rect); return; }
        if (pearlHudFinish) { DrawPearlHudSurface(vh, rect); return; }
        if (screenFinish) { DrawScreenSurface(vh, rect); return; }
        if (hudInsetFinish)
        {
            float insetRadius=Mathf.Min(cornerCut,Mathf.Min(rect.width,rect.height)*.5f);
            DrawFrameLayer(vh,rect,insetRadius,0f,new Color32(108,143,188,255),new Color32(13,29,62,255),18);
            DrawFrameLayer(vh,rect,insetRadius,1.6f,gradientBottom,gradientTop,18);
            return;
        }
        if (storybookFinish)
        {
            DrawStorybookSurface(vh, rect);
            return;
        }
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

    private void DrawScreenSurface(VertexHelper vh, Rect rect)
    {
        if (glassFinish) { DrawGlassSurface(vh, rect); return; }
        float radius = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * .5f);
        if (roomFrameFinish)
        {
            var shadow = new Rect(rect.xMin-2f, rect.yMin-5f, rect.width+4f, rect.height+4f);
            AddPolygon(vh, CreateRoundedRect(ref elevationPoints, shadow, radius+2f, 12), new Color(.12f,.07f,.035f,.22f));
            DrawFrameLayer(vh, rect, radius, 0, new Color32(116,75,43,255), new Color32(174,119,65,255), 12);
            DrawFrameLayer(vh, rect, radius, 1.6f, new Color32(168,105,52,255), new Color32(243,197,128,255), 12);
            DrawFrameLayer(vh, rect, radius, 3.4f, new Color32(181,121,66,255), new Color32(216,154,82,255), 12);
            DrawFrameLayer(vh, rect, radius, 10.4f, new Color32(225,184,125,255), new Color32(133,87,51,255), 12);
            DrawFrameLayer(vh, rect, radius, 12f, new Color32(240,204,154,255), new Color32(199,146,86,255), 12);
            return;
        }
        Color top = gradientTop, bottom = gradientBottom;
        if (modernDisabled)
        {
            top = cozyFinish ? new Color32(113, 132, 125, 255) : new Color32(237, 243, 231, 255);
            bottom = cozyFinish ? new Color32(86, 107, 102, 255) : new Color32(209, 224, 213, 255);
        }
        else if (modernPressed)
        {
            top = Color.Lerp(top, bottom, .72f);
            bottom = Color.Lerp(bottom, Color.black, .08f);
        }
        bool mask = GetComponent<Mask>() != null;
        if (softElevation && !mask && !modernPressed)
        {
            for (int layer = 2; layer >= 1; layer--)
            {
                float spread = layer * 1.2f;
                var shadow = new Rect(rect.xMin-spread, rect.yMin-spread-2f,
                    rect.width+spread*2f, rect.height+spread*2f);
                AddPolygon(vh, CreateRoundedRect(ref elevationPoints, shadow, radius+spread, 12),
                    new Color(.035f, .065f, .13f, .055f));
            }
        }
        if (modernFocused && !modernDisabled && !mask)
        {
            var focus = new Rect(rect.xMin-2f, rect.yMin-2f, rect.width+4f, rect.height+4f);
            DrawFrameLayer(vh, focus, radius+2f, 0f,
                cozyFinish ? StorybookScreenStyle.Wood : new Color32(77, 197, 183, 255),
                cozyFinish ? StorybookScreenStyle.Honey : new Color32(171, 243, 222, 255), 12);
        }
        if (radius < .1f || rect.height < 12f || mask || (cozyFinish && rect.width < 12f))
        {
            AddGradientPolygon(vh, CreateRoundedRect(ref outerPoints, rect, radius, 12), rect, bottom, top);
            return;
        }
        if (cozyFinish)
        {
            if (modernAction && !modernPressed)
            {
                var foot = new Rect(rect.xMin, rect.yMin-3f, rect.width, rect.height);
                DrawFrameLayer(vh, foot, radius, 0, new Color32(78, 61, 45, 255), StorybookScreenStyle.Wood, 12);
            }
            DrawFrameLayer(vh, rect, radius, 0, new Color32(111, 83, 56, 255), StorybookScreenStyle.Honey, 12);
            DrawFrameLayer(vh, rect, radius, 1.6f, new Color32(163, 127, 83, 255), new Color32(223, 198, 151, 255), 12);
            var inset = new Rect(rect.xMin+3.4f, rect.yMin+3.4f, rect.width-6.8f, rect.height-6.8f);
            AddGradientPolygon(vh, CreateRoundedRect(ref innerPoints, inset, Mathf.Max(0, radius-3.4f), 12), inset, bottom, top);
            if (modernAction && !modernDisabled && !modernPressed)
                AddCandyGloss(vh, inset, Mathf.Max(0, radius-3.4f), 12, .085f, ref glossPoints);
            return;
        }
        // The accepted home menu's fine jade edge and ivory inner lip, shared by popup surfaces.
        DrawFrameLayer(vh, rect, radius, 0f, new Color32(144, 184, 161, 255), new Color32(201, 225, 203, 255), 12);
        DrawFrameLayer(vh, rect, radius, 1.1f, new Color32(245, 236, 208, 255), new Color32(255, 255, 246, 255), 12);
        var face = new Rect(rect.xMin+3f, rect.yMin+3f, rect.width-6f, rect.height-6f);
        AddGradientPolygon(vh, CreateRoundedRect(ref innerPoints, face, Mathf.Max(0, radius-3f), 12), face, bottom, top);
        if (modernAction && !modernDisabled && !modernPressed)
            AddCandyGloss(vh, face, Mathf.Max(0, radius-3f), 12, .16f, ref glossPoints);
    }

    private void DrawGlassSurface(VertexHelper vh, Rect rect)
    {
        float radius = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * .5f);
        if (glassOutline) { DrawGlassRing(vh, rect, radius, 2.5f, gradientBottom, gradientTop); return; }
        Color top = gradientTop, bottom = gradientBottom;
        if (modernDisabled)
        {
            top = new Color32(86, 116, 115, 230);
            bottom = new Color32(58, 85, 87, 230);
        }
        else if (modernPressed)
        {
            top = Color.Lerp(top, bottom, .7f);
            bottom = Color.Lerp(bottom, new Color32(25, 57, 62, 255), .17f);
        }
        bool mask = GetComponent<Mask>() != null;
        if (softElevation && !mask && !modernPressed)
        {
            var shadow = new Rect(rect.xMin - 1f, rect.yMin - 3f, rect.width + 2f, rect.height + 2f);
            AddPolygon(vh, CreateRoundedRect(ref elevationPoints, shadow, radius + 1f, 16),
                new Color(.025f, .08f, .085f, modernAction ? .16f : .07f));
        }
        AddGradientPolygon(vh, CreateRoundedRect(ref outerPoints, rect, radius, 16), rect, bottom, top);
        if (radius < .1f || rect.height < 12f || rect.width < 12f || mask) return;
        bool coral = modernAction && top.r > top.g * 1.2f;
        Color rimTop = coral ? new Color32(255, 220, 196, 220) : new Color32(184, 238, 224, 195);
        Color rimBottom = coral ? new Color32(255, 162, 137, 175) : new Color32(126, 201, 193, 145);
        if (modernDisabled) { rimTop.a *= .35f; rimBottom.a *= .35f; }
        DrawGlassRing(vh, rect, radius, modernAction ? 1.5f : 1.2f, rimBottom, rimTop);
        if (modernFocused && !modernDisabled)
        {
            var focus = new Rect(rect.xMin - 2f, rect.yMin - 2f, rect.width + 4f, rect.height + 4f);
            DrawGlassRing(vh, focus, radius + 2f, 2f, StorybookScreenStyle.Mint, StorybookScreenStyle.Cream);
        }
        if (modernAction && !modernPressed && !modernDisabled)
        {
            var inset = new Rect(rect.xMin + 3f, rect.yMin + 3f, rect.width - 6f, rect.height - 6f);
            AddCandyGloss(vh, inset, Mathf.Max(0, radius - 3f), 16, .10f, ref glossPoints);
        }
    }

    private void DrawGlassRing(VertexHelper vh, Rect rect, float radius, float width, Color bottom, Color top)
    {
        var outer = CreateRoundedRect(ref framePoints, rect, radius, 16);
        var inset = new Rect(rect.xMin + width, rect.yMin + width, rect.width - width * 2, rect.height - width * 2);
        var inner = CreateRoundedRect(ref frameInsetPoints, inset, Mathf.Max(0, radius - width), 16);
        int start = vh.currentVertCount;
        for (int i = 0; i < outer.Length; i++)
        {
            vh.AddVert(outer[i], Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, outer[i].y)), Vector2.zero);
            vh.AddVert(inner[i], Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, inner[i].y)), Vector2.zero);
        }
        for (int i = 0; i < outer.Length; i++)
        {
            int a = start + i * 2, b = start + ((i + 1) % outer.Length) * 2;
            vh.AddTriangle(a, b, a + 1); vh.AddTriangle(a + 1, b, b + 1);
        }
    }

    private void DrawStorybookSurface(VertexHelper vh, Rect rect)
    {
        if (hudPortraitFinish) { DrawHudPortrait(vh, rect); return; }
        float radius = Mathf.Min(cornerCut, Mathf.Min(rect.width, rect.height) * .5f);
        Color top = gradientTop, bottom = gradientBottom;
        if (hudPanelFinish)
        {
            // Saturated enamel colours keep cream labels distinct from the reflected light.
            if (top.b > top.r && top.b > top.g)
            { top=hudBackdrop?new Color32(61,91,150,255):new Color32(70,112,187,255); bottom=new Color32(25,42,91,255); }
            else if (top.g > top.r && top.g > top.b)
            { top=new Color32(120,238,205,255); bottom=new Color32(23,142,153,255); }
            else if (top.r > top.g * 1.15f)
            { top=new Color32(255,151,117,255); bottom=new Color32(222,67,76,255); }
        }
        if (modernDisabled)
        {
            top = new Color32(80, 99, 123, 255);
            bottom = new Color32(49, 64, 89, 255);
        }
        else if (modernPressed)
        {
            top = Color.Lerp(top, bottom, .45f);
            bottom = Color.Lerp(bottom, Color.black, .08f);
        }
        float depth = modernPressed ? 1f : Mathf.Min(7f, rect.height * .075f);
        if(hudPanelFinish&&hudBackdrop)depth=Mathf.Min(depth,3.5f);
        for (int layer = 3; layer >= 1; layer--)
        {
            float spread = layer * 1.35f;
            Rect shadow = new Rect(rect.xMin - spread, rect.yMin - depth - spread - 2f,
                rect.width + spread * 2f, rect.height + spread * 2f);
            AddPolygon(vh, CreateRoundedRect(ref elevationPoints, shadow, radius + spread, 18),
                new Color(.025f, .055f, .13f, .045f));
        }
        Color side = Color.Lerp(bottom, new Color32(13, 23, 50, 255), .40f);
        Rect foot = new Rect(rect.xMin, rect.yMin - depth, rect.width, rect.height);
        DrawFrameLayer(vh, foot, radius, 0f, Color.Lerp(side, Color.black, .18f), side, 18);
        Color rimTop = Color.Lerp(top, Color.white, modernFocused && !modernDisabled ? .70f : hudPanelFinish?.34f:.50f);
        Color rimBottom = Color.Lerp(bottom, Color.black, .30f);
        DrawFrameLayer(vh, rect, radius, 0f, rimBottom, rimTop, 18);
        if (hudPanelFinish)
        {
            DrawFrameLayer(vh, rect, radius, 1.6f, bottom, top, 18);
            var enamel=new Rect(rect.xMin+2.4f,rect.yMin+2.4f,rect.width-4.8f,rect.height-4.8f);
            float reflection=modernDisabled?0f:modernPressed?.055f:hudBackdrop?.19f:.36f;
            DrawHudEnamel(vh,enamel,Mathf.Max(0,radius-2.4f),bottom,top,reflection);
            if (!modernDisabled)
            {
                AddRuntimeGloss(vh,enamel,Mathf.Max(0,radius-2.4f),runtimeGlossPhase,
                    modernPressed?0f:runtimeGlossStrength);
            }
            return;
        }
        DrawFrameLayer(vh, rect, radius, 2.2f, bottom, top, 18);
        Rect highlight = new Rect(rect.xMin + 5f, rect.yMin + 5f, rect.width - 10f, rect.height - 10f);
        AddCandyGloss(vh, highlight, Mathf.Max(0f, radius - 5f), 18,
            modernDisabled ? 0f : .055f, ref glossPoints);
    }

    // Only the home HUD opts into these finishes; other screens retain their authored material.
    private bool hudPanelFinish, hudPortraitFinish, hudInsetFinish, hudBackdrop;
    public void ConfigureHudInsetStyle(Color top,Color bottom,float radius)
    {
        ConfigureModernStyle(top,bottom,radius);
        hudInsetFinish=true;SetVerticesDirty();
    }
    public void ConfigureHudPanelFinish(bool backdrop=false)
    { hudPanelFinish = true; hudPortraitFinish = false; hudBackdrop=backdrop; SetVerticesDirty(); }
    public void ConfigureHudPortraitFinish()
    { hudPortraitFinish = true; hudPanelFinish = false; SetVerticesDirty(); }

    private void DrawHudJoystick(VertexHelper vh,Rect rect)
    {
        float radius=Mathf.Min(rect.width,rect.height)*.5f;
        bool outer=hudJoystickPart==1,cap=hudJoystickPart==3;
        DrawFrameLayer(vh,rect,radius,0,new Color32(26,57,115,255),new Color32(150,210,245,255),24);
        DrawFrameLayer(vh,rect,radius,1.2f,new Color32(116,77,30,255),new Color32(255,236,174,255),24);
        if(outer)DrawFrameLayer(vh,rect,radius,3.1f,new Color32(23,58,119,255),new Color32(121,181,233,255),24);
        float inset=outer?5f:2.5f;
        var inner=new Rect(rect.xMin+inset,rect.yMin+inset,rect.width-inset*2,rect.height-inset*2);
        DrawHudEnamel(vh,inner,radius-inset,
            cap?(Color)new Color32(207,178,133,255):outer?(Color)new Color32(28,66,138,255):new Color32(13,132,162,255),
            cap?(Color)new Color32(255,251,230,255):outer?(Color)new Color32(79,139,209,255):new Color32(119,255,239,255),
            cap?.42f:outer?.27f:.38f);
    }

    private void DrawHudPortrait(VertexHelper vh, Rect rect)
    {
        float radius = Mathf.Min(rect.width, rect.height) * .5f;
        var foot = new Rect(rect.xMin, rect.yMin - 3f, rect.width, rect.height);
        DrawFrameLayer(vh, foot, radius, 0f, new Color32(11,28,52,255), new Color32(31,58,90,255), 24);
        DrawFrameLayer(vh, rect, radius, 0f, new Color32(25,54,80,255), new Color32(142,193,219,255), 24);
        var ring=new Rect(rect.xMin+2,rect.yMin+2,rect.width-4,rect.height-4);
        DrawHudEnamel(vh,ring,radius-2,new Color32(25,141,153,255),new Color32(123,243,212,255),.42f);
        DrawFrameLayer(vh, rect, radius, 10f, new Color32(22,72,87,255), new Color32(43,132,140,255), 24);
        DrawFrameLayer(vh, rect, radius, 11.5f, new Color32(74,160,168,255), new Color32(102,207,208,255), 24);
        // The portrait covers the central well; the light stays on its sculpted mint surround.
    }

    private static void DrawHudEnamel(VertexHelper vh,Rect rect,float radius,Color bottom,Color top,float reflection)
    {
        if(rect.width<=0||rect.height<=0)return;
        const int rows=24,columns=12;
        radius=Mathf.Clamp(radius,0,Mathf.Min(rect.width,rect.height)*.5f);
        int start=vh.currentVertCount;
        for(int row=0;row<=rows;row++)
        {
            // Cosine spacing gives the curved silhouette more samples near its poles.
            float v=(1f-Mathf.Cos(Mathf.PI*row/rows))*.5f;
            float y=rect.yMin+v*rect.height;
            float dy=y<rect.yMin+radius?y-(rect.yMin+radius):y>rect.yMax-radius?y-(rect.yMax-radius):0f;
            float inset=radius-Mathf.Sqrt(Mathf.Max(0,radius*radius-dy*dy));
            for(int col=0;col<=columns;col++)
            {
                float u=(float)col/columns;
                float x=Mathf.Lerp(rect.xMin+inset,rect.xMax-inset,u);
                float surfaceU=(x-rect.xMin)/rect.width;
                Color baseColor=Color.Lerp(bottom,top,Mathf.SmoothStep(0,1,v));
                float across=(surfaceU-.22f)/.62f,vertical=(v-.91f)/.24f;
                float softbox=Mathf.Exp(-across*across-vertical*vertical)*reflection;
                float edge=Mathf.Exp(-Mathf.Pow((v-.965f)/.028f,2f))*(1f-surfaceU)*reflection*.42f;
                Color lit=Color.Lerp(baseColor,new Color(0.91f,1f,1f,baseColor.a),Mathf.Clamp01(softbox+edge));
                AddVertex(vh,new Vector2(x,y),lit);
                if(row>0&&col>0)
                {
                    int current=start+row*(columns+1)+col;
                    vh.AddTriangle(current-columns-2,current-columns-1,current);
                    vh.AddTriangle(current-columns-2,current,current-1);
                }
            }
        }
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

    private void DrawHudArtwork(VertexHelper vh, Rect rect)
    {
        Vector4 outer = UnityEngine.Sprites.DataUtility.GetOuterUV(hudArtwork);
        Vector4 inner = UnityEngine.Sprites.DataUtility.GetInnerUV(hudArtwork);
        Vector4 border = hudArtwork.border;
        // Scale the corner artwork with height, including the much wider dock frame.
        float scale = Mathf.Min(rect.height / hudArtwork.rect.height,
            rect.width / Mathf.Max(1f, border.x + border.z));
        border *= scale;
        float brightness = modernDisabled ? .56f : modernPressed ? .82f : modernFocused ? 1.06f : 1f;
        Color tint = new Color(brightness, brightness, brightness, color.a);
        for (int y = 0; y < 4; y++)
        {
            float py = y == 0 ? rect.yMin : y == 1 ? rect.yMin + border.y : y == 2 ? rect.yMax - border.w : rect.yMax;
            float v = y == 0 ? outer.y : y == 1 ? inner.y : y == 2 ? inner.w : outer.w;
            for (int x = 0; x < 4; x++)
            {
                float px = x == 0 ? rect.xMin : x == 1 ? rect.xMin + border.x : x == 2 ? rect.xMax - border.z : rect.xMax;
                float u = x == 0 ? outer.x : x == 1 ? inner.x : x == 2 ? inner.z : outer.z;
                vh.AddVert(new Vector3(px, py), tint, new Vector2(u, v));
            }
        }
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                int i = y * 4 + x;
                vh.AddTriangle(i, i + 4, i + 5);
                vh.AddTriangle(i, i + 5, i + 1);
            }
    }

    private static void AddVertex(VertexHelper vh, Vector2 position, Color tint)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = tint;
        vh.AddVert(vertex);
    }
}
