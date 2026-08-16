using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LowPolyHandGraphic : MaskableGraphic
{
    [SerializeField] private Color shadowColor = new Color32(104, 57, 37, 210);
    [SerializeField] private Color outlineColor = new Color32(112, 61, 39, 255);
    [SerializeField] private Color handColor = new Color32(255, 204, 139, 255);
    [SerializeField] private Color highlightColor = new Color32(255, 231, 181, 220);
    [SerializeField] private Color warmFacetColor = new Color32(226, 158, 94, 150);
    [SerializeField] private Color cuffColor = new Color32(245, 104, 61, 255);
    [SerializeField] private Color cuffShadowColor = new Color32(164, 62, 31, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float unit = Mathf.Min(rect.width, rect.height) / 100f;
        Vector2 center = rect.center;

        AddHand(vh, center + new Vector2(5f, -6f) * unit, unit * 1.06f, shadowColor);
        AddHand(vh, center, unit * 1.08f, outlineColor);
        AddHand(vh, center, unit * 0.91f, handColor);
        AddFacet(
            vh,
            center + new Vector2(-5f, 2f) * unit,
            unit,
            highlightColor
        );
        AddWarmFacet(vh, center, unit, warmFacetColor);
        AddCuff(vh, center + new Vector2(4f, -2f) * unit, unit * 1.03f, cuffShadowColor);
        AddCuff(vh, center, unit, cuffColor);
    }

    private static void AddHand(VertexHelper vh, Vector2 center, float unit, Color color)
    {
        Vector2[] points =
        {
            new Vector2(-24f, -42f), new Vector2(20f, -42f),
            new Vector2(28f, -29f), new Vector2(30f, -5f),
            new Vector2(43f, 5f), new Vector2(45f, 18f),
            new Vector2(37f, 25f), new Vector2(27f, 18f),
            new Vector2(24f, 10f), new Vector2(23f, 34f),
            new Vector2(16f, 43f), new Vector2(7f, 41f),
            new Vector2(5f, 51f), new Vector2(-4f, 55f),
            new Vector2(-13f, 49f), new Vector2(-14f, 31f),
            new Vector2(-21f, 39f), new Vector2(-31f, 36f),
            new Vector2(-34f, 19f), new Vector2(-43f, 23f),
            new Vector2(-51f, 16f), new Vector2(-47f, 1f),
            new Vector2(-34f, -11f), new Vector2(-32f, -30f)
        };

        AddPolygon(vh, points, center, unit, color);
    }

    private static void AddFacet(VertexHelper vh, Vector2 center, float unit, Color color)
    {
        Vector2[] points =
        {
            new Vector2(-13f, -24f), new Vector2(7f, -25f),
            new Vector2(14f, -4f), new Vector2(7f, 28f),
            new Vector2(-5f, 36f), new Vector2(-12f, 18f)
        };
        AddPolygon(vh, points, center, unit, color);
    }

    private static void AddWarmFacet(VertexHelper vh, Vector2 center, float unit, Color color)
    {
        Vector2[] points =
        {
            new Vector2(8f, -25f), new Vector2(27f, -17f),
            new Vector2(30f, 8f), new Vector2(16f, 23f),
            new Vector2(4f, 4f)
        };
        AddPolygon(vh, points, center, unit, color);
    }

    private static void AddCuff(VertexHelper vh, Vector2 center, float unit, Color color)
    {
        Vector2[] points =
        {
            new Vector2(-29f, -47f), new Vector2(23f, -47f),
            new Vector2(29f, -34f), new Vector2(20f, -24f),
            new Vector2(-27f, -27f), new Vector2(-34f, -38f)
        };
        AddPolygon(vh, points, center, unit, color);
    }

    private static void AddPolygon(
        VertexHelper vh,
        Vector2[] points,
        Vector2 center,
        float unit,
        Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        foreach (Vector2 point in points)
            vh.AddVert(center + point * unit, color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % points.Length);
    }
}
