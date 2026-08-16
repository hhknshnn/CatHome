using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The shared panel mesh owns the light grey outline on every button and bar.
/// Skipping the bottom-right quadrant made that stroke stop on the right cap.
/// </summary>
public sealed class LowPolyPanelGraphicRimTests
{
    [Test]
    public void PanelRim_KeepsTheLightStrokeOnTheRightCap()
    {
        GameObject go = new GameObject(
            "RimProbe", typeof(RectTransform), typeof(CanvasRenderer));
        try
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(230f, 72f);

            LowPolyPanelGraphic graphic = go.AddComponent<LowPolyPanelGraphic>();
            graphic.ConfigurePremiumStyle(
                new Color32(89, 238, 207, 255),
                new Color32(29, 200, 188, 255),
                36f,
                5f,
                new Color32(255, 255, 255, 195),
                new Color32(65, 30, 92, 120),
                new Color32(255, 213, 237, 68));
            graphic.ConfigureCandyPolish(0.32f, 0.14f);

            Mesh mesh = BuildMesh(graphic);
            try
            {
                CountLightRim(mesh, out int left, out int right);
                Assert.That(left, Is.GreaterThan(8), "Left cap lost its light stroke.");
                Assert.That(
                    right,
                    Is.GreaterThan(8),
                    "Right cap of the outline is missing — the grey stroke is a C, not a ring.");
                Assert.That(
                    right,
                    Is.GreaterThanOrEqualTo(left / 2),
                    "Right cap of the outline is much thinner than the left.");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static Mesh BuildMesh(LowPolyPanelGraphic graphic)
    {
        MethodInfo populate = typeof(LowPolyPanelGraphic).GetMethod(
            "OnPopulateMesh",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(VertexHelper) },
            null);
        Assert.That(populate, Is.Not.Null);

        var helper = new VertexHelper();
        populate.Invoke(graphic, new object[] { helper });
        var mesh = new Mesh();
        helper.FillMesh(mesh);
        helper.Dispose();
        return mesh;
    }

    private static void CountLightRim(Mesh mesh, out int left, out int right)
    {
        Vector3[] vertices = mesh.vertices;
        Color32[] colors = mesh.colors32;
        left = 0;
        right = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            float luminance = (colors[i].r + colors[i].g + colors[i].b) / 765f;
            if (luminance < 0.72f || colors[i].a < 40)
                continue;
            if (vertices[i].x > 80f)
                right++;
            else if (vertices[i].x < -80f)
                left++;
        }
    }
}
