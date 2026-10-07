using UnityEngine;
using UnityEngine.UI;

/// <summary>Non-interactive, rounded room illustration inside the original popup hit surface.</summary>
[DisallowMultipleComponent]
public sealed class StorybookRoomBackdrop : MaskableGraphic
{
    const string ChildName = "IllustratedRoomBackdrop";
    static Texture2D artwork;
    static Texture2D contentArtwork;
    float radius = 24f;
    bool quiet;
    bool decorated;

    public override Texture mainTexture
    {
        get
        {
            if (decorated)
            {
                if (artwork == null) artwork = Resources.Load<Texture2D>("PopupRoom/RoomBackdrop");
                return artwork != null ? artwork : Texture2D.whiteTexture;
            }
            if (contentArtwork == null) contentArtwork = Resources.Load<Texture2D>("PopupRoom/ContentBackdrop");
            return contentArtwork != null ? contentArtwork : Texture2D.whiteTexture;
        }
    }

    public static void Apply(LowPolyPanelGraphic surface, float cornerRadius, bool quiet = false, bool decorated = false)
    {
        if (surface == null) return;
        surface.ConfigureHudArtwork(null);
        surface.ConfigureRoomFrame(cornerRadius);
        var child = surface.transform.Find(ChildName);
        var art = child != null ? child.GetComponent<StorybookRoomBackdrop>() : null;
        if (art == null)
        {
            var go = new GameObject(ChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(StorybookRoomBackdrop), typeof(LayoutElement));
            go.transform.SetParent(surface.transform, false);
            art = go.GetComponent<StorybookRoomBackdrop>();
            go.GetComponent<LayoutElement>().ignoreLayout = true;
        }
        art.gameObject.SetActive(true);
        art.transform.SetAsFirstSibling();
        art.raycastTarget = false;
        art.color = Color.white;
        art.quiet = quiet;
        art.decorated = decorated;
        art.radius = Mathf.Max(0, cornerRadius - 13f);
        var rect = art.rectTransform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * 13f; rect.offsetMax = Vector2.one * -13f;
        rect.localScale = Vector3.one;
        art.SetAllDirty();
    }

    public static void Hide(LowPolyPanelGraphic surface)
    {
        var child = surface != null ? surface.transform.Find(ChildName) : null;
        if (child != null) child.gameObject.SetActive(false);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var rect = GetPixelAdjustedRect();
        if (rect.width <= 0 || rect.height <= 0) return;
        float r = Mathf.Clamp(radius, 0, Mathf.Min(rect.width, rect.height) * .5f);
        // Compact dialogs use the quiet wall's light/shadow detail, with no stretched furniture.
        Rect uv = quiet ? new Rect(.28f, .38f, .54f, .55f) : new Rect(0, 0, 1, 1);
        if (quiet)
        {
            float textureAspect = mainTexture.width / (float)mainTexture.height;
            float desiredHeight = uv.width * textureAspect * rect.height / rect.width;
            if (desiredHeight < uv.height) { uv.y += (uv.height - desiredHeight) * .5f; uv.height = desiredHeight; }
            else { float w = uv.height / textureAspect * rect.width / rect.height; uv.x += (uv.width - w) * .5f; uv.width = w; }
        }
        Add(vh, rect.center, rect, uv);
        const int segments = 12;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = corner == 0 ? new Vector2(rect.xMin+r,rect.yMin+r) :
                corner == 1 ? new Vector2(rect.xMax-r,rect.yMin+r) :
                corner == 2 ? new Vector2(rect.xMax-r,rect.yMax-r) : new Vector2(rect.xMin+r,rect.yMax-r);
            float start = 180 + corner * 90;
            for (int step = 0; step <= segments; step++)
            {
                float a = (start + step * 90f / segments) * Mathf.Deg2Rad;
                Add(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, rect, uv);
            }
        }
        int count = 4 * (segments + 1);
        for (int i = 1; i <= count; i++) vh.AddTriangle(0, i, i == count ? 1 : i+1);
    }

    void Add(VertexHelper vh, Vector2 p, Rect rect, Rect uv)
    {
        var v = UIVertex.simpleVert; v.position = p; v.color = color;
        v.uv0 = new Vector2(uv.x + (p.x-rect.xMin)/rect.width*uv.width, uv.y + (p.y-rect.yMin)/rect.height*uv.height);
        vh.AddVert(v);
    }
}
