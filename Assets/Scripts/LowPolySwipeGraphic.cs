using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LowPolySwipeGraphic : MaskableGraphic
{
    [SerializeField] private Color shadowColor = new Color32(111, 65, 42, 90);
    [SerializeField] private Color arrowColor = new Color32(255, 252, 239, 255);
    [SerializeField] private Color trailColor = new Color32(255, 252, 239, 225);

    private float handOffset;
    private float velocity;

    public void SetMotion(float horizontalOffset, float normalizedVelocity)
    {
        horizontalOffset = Mathf.Clamp(horizontalOffset, -100f, 100f);
        normalizedVelocity = Mathf.Clamp(normalizedVelocity, -1f, 1f);
        if (Mathf.Approximately(handOffset, horizontalOffset) &&
            Mathf.Approximately(velocity, normalizedVelocity))
        {
            return;
        }

        handOffset = horizontalOffset;
        velocity = normalizedVelocity;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float unit = Mathf.Min(rect.width / 330f, rect.height / 105f);
        Vector2 center = rect.center;
        float arrowX = rect.width * 0.5f - 27f * unit;
        Vector2 shadowOffset = new Vector2(3f, -4f) * unit;

        AddDirectionArrow(vh, center + new Vector2(-arrowX, 0f) + shadowOffset, unit, -1f, shadowColor);
        AddDirectionArrow(vh, center + new Vector2(arrowX, 0f) + shadowOffset, unit, 1f, shadowColor);
        AddDirectionArrow(vh, center + new Vector2(-arrowX, 0f), unit, -1f, arrowColor);
        AddDirectionArrow(vh, center + new Vector2(arrowX, 0f), unit, 1f, arrowColor);

        AddMotionTrails(vh, center, unit);
    }

    private void AddMotionTrails(VertexHelper vh, Vector2 center, float unit)
    {
        float speed = Mathf.Abs(velocity);
        if (speed < 0.015f)
            return;

        float direction = velocity >= 0f ? 1f : -1f;
        float[] lengths = { 66f, 52f, 38f };
        float[] heights = { 18f, 0f, -18f };
        float[] alphas = { 0.92f, 0.62f, 0.34f };
        for (int i = 0; i < 3; i++)
        {
            float length = lengths[i] * unit;
            float x = handOffset * unit - direction * (48f + i * 7f) * unit;
            Color color = trailColor;
            color.a *= alphas[i] * Mathf.Lerp(0.35f, 1f, speed);
            AddTrail(
                vh,
                center + new Vector2(x, heights[i] * unit),
                length,
                5f * unit * (1f - i * 0.14f),
                direction,
                color
            );
        }
    }

    private static void AddDirectionArrow(
        VertexHelper vh,
        Vector2 center,
        float unit,
        float direction,
        Color color)
    {
        Vector2[] points =
        {
            new Vector2(22f, 0f), new Vector2(3f, 21f),
            new Vector2(3f, 9f), new Vector2(-20f, 9f),
            new Vector2(-20f, -9f), new Vector2(3f, -9f),
            new Vector2(3f, -21f)
        };

        AddPolygon(vh, points, center, new Vector2(direction * unit, unit), color);
    }

    private static void AddTrail(
        VertexHelper vh,
        Vector2 center,
        float length,
        float halfHeight,
        float direction,
        Color color)
    {
        float halfLength = length * 0.5f;
        Vector2[] points =
        {
            new Vector2(-halfLength, 0f),
            new Vector2(-halfLength + halfHeight, halfHeight),
            new Vector2(halfLength - halfHeight * 1.6f, halfHeight * 0.72f),
            new Vector2(halfLength, 0f),
            new Vector2(halfLength - halfHeight * 1.6f, -halfHeight * 0.72f),
            new Vector2(-halfLength + halfHeight, -halfHeight)
        };
        AddPolygon(vh, points, center, new Vector2(direction, 1f), color);
    }

    private static void AddPolygon(
        VertexHelper vh,
        Vector2[] points,
        Vector2 center,
        Vector2 scale,
        Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddVert(center + Vector2.Scale(points[i], scale), color, Vector2.zero);
        for (int i = 0; i < points.Length; i++)
            vh.AddTriangle(start, start + i + 1, start + (i + 1) % points.Length + 1);
    }
}
