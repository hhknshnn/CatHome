using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LowPolyPawGraphic : MaskableGraphic
{
    [SerializeField] private Color shadowColor = new Color32(105, 57, 37, 220);
    [SerializeField] private Color outlineColor = new Color32(126, 68, 43, 255);
    [SerializeField] private Color padColor = new Color32(244, 151, 98, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        float unit = Mathf.Min(r.width, r.height) / 100f;
        Vector2 center = r.center;
        Vector2 shadow = new Vector2(4f, -5f) * unit;

        AddPaw(vh, center + shadow, unit, shadowColor, 1.08f);
        AddPaw(vh, center, unit, outlineColor, 1.08f);
        AddPaw(vh, center, unit, padColor, 0.88f);
    }

    private static void AddPaw(VertexHelper vh, Vector2 center, float unit, Color color, float scale)
    {
        AddEllipse(vh, center + new Vector2(0f, -14f) * unit, new Vector2(29f, 25f) * unit * scale, color, 8, -6f);
        AddEllipse(vh, center + new Vector2(-27f, 17f) * unit, new Vector2(11f, 15f) * unit * scale, color, 7, 18f);
        AddEllipse(vh, center + new Vector2(-10f, 29f) * unit, new Vector2(11f, 16f) * unit * scale, color, 7, 5f);
        AddEllipse(vh, center + new Vector2(10f, 29f) * unit, new Vector2(11f, 16f) * unit * scale, color, 7, -5f);
        AddEllipse(vh, center + new Vector2(27f, 17f) * unit, new Vector2(11f, 15f) * unit * scale, color, 7, -18f);
    }

    private static void AddEllipse(
        VertexHelper vh,
        Vector2 center,
        Vector2 radius,
        Color color,
        int segments,
        float rotationDegrees)
    {
        int start = vh.currentVertCount;
        float rotation = rotationDegrees * Mathf.Deg2Rad;
        float sinR = Mathf.Sin(rotation);
        float cosR = Mathf.Cos(rotation);
        vh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector2 point = new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
            point = new Vector2(point.x * cosR - point.y * sinR, point.x * sinR + point.y * cosR);
            vh.AddVert(center + point, color, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % segments);
    }
}
