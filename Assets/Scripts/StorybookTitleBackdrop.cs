using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent title reading panel, separate from the live room.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StorybookTitleBackdrop : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        const int rows = 64, columns = 12;
        for (int y = 0; y <= rows; y++)
        {
            float v = y / (float)rows;
            float edge = Edge(v, r);
            for (int x = 0; x <= columns; x++)
            {
                float u = x / (float)columns;
                Color shade = Color.Lerp(new Color32(255, 248, 229, 255),
                    new Color32(255, 253, 242, 255), Mathf.Clamp01(v * .58f + (1f-u) * .24f));
                shade.a = .99f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.40f, 1f, u)));
                vh.AddVert(new Vector3(Mathf.Lerp(r.xMin, edge, u), Mathf.Lerp(r.yMin, r.yMax, v)), shade, Vector2.zero);
                if (x == 0 || y == 0) continue;
                int a = y * (columns+1) + x;
                vh.AddTriangle(a-columns-2, a-columns-1, a);
                vh.AddTriangle(a-columns-2, a, a-1);
            }
        }

    }

    private static float Edge(float t, Rect r) => r.xMin + r.width *
        (.925f + .055f*Mathf.Sin(t*Mathf.PI*2f-.65f) + .02f*Mathf.Sin(t*Mathf.PI));

    private static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
    {
        int n=vh.currentVertCount;
        vh.AddVert(a,tint,Vector2.zero);vh.AddVert(b,tint,Vector2.zero);
        vh.AddVert(c,tint,Vector2.zero);vh.AddVert(d,tint,Vector2.zero);
        vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
    }
}
