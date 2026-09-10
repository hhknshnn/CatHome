using UnityEngine;
using UnityEngine.UI;

/// <summary>Smooth vector care icons. Meshes rebuild for layout/shape changes, never for age/fade.</summary>
public sealed class CatCareFxGraphic : MaskableGraphic
{
    public enum Shape { Heart, Sleep }
    private const int Segments = 64;
    private Shape shape;
    private readonly Vector2[] outline = new Vector2[Segments];

    public void SetShape(Shape value) { shape = value; SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        float size = Mathf.Min(rect.width, rect.height);
        Vector2 center = rect.center;
        if (shape == Shape.Heart) Heart(mesh, center, size);
        else Sleep(mesh, center, size);
    }

    private void Heart(VertexHelper mesh, Vector2 center, float size)
    {
        for (int i = 0; i < Segments; i++)
        {
            float t = (i % 16) / 16f;
            switch (i / 16)
            {
                case 0: outline[i] = Bezier(new Vector2(0f, -.46f), new Vector2(-.06f, -.38f), new Vector2(-.48f, -.10f), new Vector2(-.48f, .20f), t); break;
                case 1: outline[i] = Bezier(new Vector2(-.48f, .20f), new Vector2(-.48f, .52f), new Vector2(-.12f, .58f), new Vector2(0f, .31f), t); break;
                case 2: outline[i] = Bezier(new Vector2(0f, .31f), new Vector2(.12f, .58f), new Vector2(.48f, .52f), new Vector2(.48f, .20f), t); break;
                default: outline[i] = Bezier(new Vector2(.48f, .20f), new Vector2(.48f, -.10f), new Vector2(.06f, -.38f), new Vector2(0f, -.46f), t); break;
            }
        }
        Polygon(mesh, center + Vector2.down * size * .025f, size * 1.045f,
            new Color32(157, 53, 100, 26), new Color32(157, 53, 100, 26));
        Polygon(mesh, center, size, new Color32(244, 81, 117, 255), new Color32(255, 155, 182, 255));
        Stroke(mesh, center, size, new Vector2(-.34f, .23f), new Vector2(-.35f, .37f),
            new Vector2(-.23f, .43f), new Vector2(-.14f, .37f), .026f, new Color32(255, 244, 249, 210));
    }

    private void Sleep(VertexHelper mesh, Vector2 center, float size)
    {
        Circle(mesh, center + Vector2.down * size * .025f, size * .49f,
            new Color32(54, 45, 120, 24), new Color32(54, 45, 120, 24));
        Circle(mesh, center, size * .48f, new Color32(211, 210, 255, 255), new Color32(255, 250, 255, 255));
        Circle(mesh, center, size * .438f, new Color32(90, 94, 174, 255), new Color32(151, 145, 220, 255));
        // Real crescent geometry leaves a concave edge instead of painting over the gradient.
        int first = mesh.currentVertCount;
        const int steps = 32;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 outer = Bezier(new Vector2(.055f, .245f), new Vector2(-.34f, .27f), new Vector2(-.34f, -.255f), new Vector2(.035f, -.225f), t);
            Vector2 inner = Bezier(new Vector2(.055f, .245f), new Vector2(-.115f, .09f), new Vector2(-.12f, -.08f), new Vector2(.035f, -.225f), t);
            Color c = Color.Lerp(new Color32(255, 255, 255, 255), new Color32(224, 225, 255, 255), t);
            Add(mesh, center + outer * size, c);
            Add(mesh, center + inner * size, c);
            if (i > 0)
            {
                int p = first + (i - 1) * 2;
                mesh.AddTriangle(p, p + 1, p + 2);
                mesh.AddTriangle(p + 1, p + 3, p + 2);
            }
        }
        Star(mesh, center + new Vector2(.20f, .15f) * size, size * .105f, new Color32(255, 239, 184, 255));
        Star(mesh, center + new Vector2(.25f, -.10f) * size, size * .049f, new Color32(241, 239, 255, 230));
        Circle(mesh, center + new Vector2(-.21f, .31f) * size, size * .022f,
            new Color32(255, 255, 255, 140), new Color32(255, 255, 255, 140));
    }

    private void Circle(VertexHelper mesh, Vector2 center, float radius, Color bottom, Color top)
    {
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * (Mathf.PI * 2f / Segments);
            outline[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .5f;
        }
        Polygon(mesh, center, radius * 2f, bottom, top);
    }

    private void Polygon(VertexHelper mesh, Vector2 center, float size, Color bottom, Color top)
    {
        int first = mesh.currentVertCount;
        Add(mesh, center, Color.Lerp(bottom, top, .5f));
        for (int i = 0; i < Segments; i++)
        {
            Vector2 point = outline[i];
            Color edge = Color.Lerp(bottom, top, Mathf.Clamp01(point.y + .5f));
            Add(mesh, center + point * size, edge);
            edge.a = 0f;
            Add(mesh, center + point * (size + 1.6f), edge);
        }
        for (int i = 0; i < Segments; i++)
        {
            int a = first + 1 + i * 2, b = first + 1 + ((i + 1) % Segments) * 2;
            mesh.AddTriangle(first, a, b);
            mesh.AddTriangle(a, a + 1, b);
            mesh.AddTriangle(a + 1, b + 1, b);
        }
    }

    private static void Star(VertexHelper mesh, Vector2 center, float radius, Color color)
    {
        int first = mesh.currentVertCount;
        Add(mesh, center, color);
        const int count = 32;
        for (int i = 0; i < count; i++)
        {
            float angle = i * (Mathf.PI * 2f / count);
            float r = Mathf.Lerp(.26f, 1f, Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 2f)), 3f));
            Add(mesh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * r), color);
        }
        for (int i = 0; i < count; i++) mesh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % count);
    }

    private static void Stroke(VertexHelper mesh, Vector2 center, float size, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float width, Color color)
    {
        const int steps = 16;
        int first = mesh.currentVertCount;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            Vector2 p = Bezier(a, b, c, d, t);
            Vector2 tangent = (Bezier(a, b, c, d, Mathf.Min(1f, t + .01f)) -
                               Bezier(a, b, c, d, Mathf.Max(0f, t - .01f))).normalized;
            Vector2 offset = new Vector2(-tangent.y, tangent.x) * (width * Mathf.Sin(t * Mathf.PI));
            Add(mesh, center + (p - offset) * size, color);
            Add(mesh, center + (p + offset) * size, color);
            if (i > 0)
            {
                int p0 = first + (i - 1) * 2;
                mesh.AddTriangle(p0, p0 + 1, p0 + 2);
                mesh.AddTriangle(p0 + 1, p0 + 3, p0 + 2);
            }
        }
    }

    private static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
    {
        float u = 1f - t;
        return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
    }

    private static void Add(VertexHelper mesh, Vector2 point, Color color) =>
        mesh.AddVert(point, color, Vector2.zero);
}
