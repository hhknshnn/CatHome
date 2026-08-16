using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PetTutorialPanelGraphic : MaskableGraphic
{
    [SerializeField] private Color shadowColor = new Color32(111, 65, 42, 210);
    [SerializeField] private Color frameColor = new Color32(213, 126, 66, 255);
    [SerializeField] private Color faceColor = new Color32(255, 235, 190, 255);
    [SerializeField] private Vector2 shadowOffset = new Vector2(9f, -10f);
    [SerializeField, Min(0f)] private float frameThickness = 9f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        AddPolygon(vh, Polygon(r, shadowOffset, 0f), shadowColor);
        AddPolygon(vh, Polygon(r, Vector2.zero, 0f), frameColor);
        Rect inner = new Rect(
            r.xMin + frameThickness,
            r.yMin + frameThickness,
            r.width - frameThickness * 2f,
            r.height - frameThickness * 2f
        );
        AddPolygon(vh, Polygon(inner, Vector2.zero, 2f), faceColor);
    }

    private static Vector2[] Polygon(Rect rect, Vector2 offset, float inset)
    {
        float x0 = rect.xMin + offset.x;
        float x1 = rect.xMax + offset.x;
        float y0 = rect.yMin + offset.y;
        float y1 = rect.yMax + offset.y;
        return new[]
        {
            new Vector2(x0 + 13f + inset, y0),
            new Vector2(x1 - 18f, y0 + 2f + inset),
            new Vector2(x1, y0 + 15f + inset),
            new Vector2(x1 - 3f, y1 - 12f - inset),
            new Vector2(x1 - 15f - inset, y1),
            new Vector2(x0 + 11f + inset, y1 - 1f),
            new Vector2(x0, y1 - 14f - inset),
            new Vector2(x0 + 2f, y0 + 12f + inset)
        };
    }

    private static void AddPolygon(VertexHelper vh, Vector2[] points, Color color)
    {
        int start = vh.currentVertCount;
        Vector2 center = Vector2.zero;
        for (int i = 0; i < points.Length; i++)
            center += points[i];
        center /= points.Length;

        vh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddVert(points[i], color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
    }
}
