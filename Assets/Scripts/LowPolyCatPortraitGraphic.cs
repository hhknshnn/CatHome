using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LowPolyCatPortraitGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        float s = Mathf.Min(r.width, r.height);
        Vector2 c = r.center + Vector2.down * (s * 0.02f);
        Color cream = new Color32(244, 196, 126, 255);
        Color light = new Color32(255, 224, 166, 255);
        Color shade = new Color32(213, 143, 83, 255);
        Color dark = new Color32(91, 55, 42, 255);
        Color coral = new Color32(231, 111, 74, 255);

        AddTriangle(vh, c + P(-.43f, .18f, s), c + P(-.23f, .48f, s), c + P(-.05f, .25f, s), shade);
        AddTriangle(vh, c + P(.43f, .18f, s), c + P(.23f, .48f, s), c + P(.05f, .25f, s), shade);
        AddPolygon(vh, new[]
        {
            c + P(-.37f,.20f,s), c + P(-.22f,.36f,s), c + P(.22f,.36f,s),
            c + P(.39f,.17f,s), c + P(.34f,-.22f,s), c + P(.12f,-.39f,s),
            c + P(-.15f,-.38f,s), c + P(-.36f,-.18f,s)
        }, cream);
        AddTriangle(vh, c + P(-.34f,.18f,s), c + P(0,.34f,s), c + P(-.05f,-.35f,s), light);
        AddTriangle(vh, c + P(.34f,.18f,s), c + P(0,.34f,s), c + P(-.05f,-.35f,s), new Color32(235,174,106,255));
        AddDiamond(vh, c + P(-.15f,.02f,s), s*.035f, dark);
        AddDiamond(vh, c + P(.15f,.02f,s), s*.035f, dark);
        AddTriangle(vh, c + P(-.045f,-.10f,s), c + P(.045f,-.10f,s), c + P(0,-.16f,s), coral);
        AddTriangle(vh, c + P(0,-.16f,s), c + P(-.075f,-.22f,s), c + P(.005f,-.19f,s), dark);
        AddTriangle(vh, c + P(0,-.16f,s), c + P(.075f,-.22f,s), c + P(-.005f,-.19f,s), dark);
        AddPolygon(vh, new[] { c+P(-.34f,-.27f,s), c+P(-.08f,-.34f,s), c+P(-.04f,-.49f,s), c+P(-.38f,-.46f,s) }, cream);
        AddPolygon(vh, new[] { c+P(.34f,-.27f,s), c+P(.08f,-.34f,s), c+P(.04f,-.49f,s), c+P(.38f,-.46f,s) }, cream);
    }

    private static Vector2 P(float x, float y, float size) => new Vector2(x * size, y * size);
    private static void AddDiamond(VertexHelper vh, Vector2 c, float radius, Color color) =>
        AddPolygon(vh, new[] { c+Vector2.up*radius, c+Vector2.right*radius, c+Vector2.down*radius, c+Vector2.left*radius }, color);
    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color) => AddPolygon(vh, new[] { a, b, c }, color);
    private static void AddPolygon(VertexHelper vh, Vector2[] p, Color color)
    {
        int start = vh.currentVertCount;
        Vector2 center = Vector2.zero;
        for (int i=0;i<p.Length;i++) center += p[i];
        center /= p.Length;
        vh.AddVert(center, color, Vector2.zero);
        for (int i=0;i<p.Length;i++) vh.AddVert(p[i], color, Vector2.zero);
        for (int i=0;i<p.Length;i++) vh.AddTriangle(start, start+1+i, start+1+(i+1)%p.Length);
    }
}
