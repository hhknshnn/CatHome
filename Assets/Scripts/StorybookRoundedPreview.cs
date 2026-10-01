using UnityEngine;
using UnityEngine.UI;

/// <summary>Rounds an existing preview mesh without replacing its texture or adding a stencil pass.</summary>
[RequireComponent(typeof(RawImage))]
public sealed class StorybookRoundedPreview : BaseMeshEffect
{
    [SerializeField] private float radius = 24f;
    public void Configure(float cornerRadius)
    {
        radius = Mathf.Max(0f, cornerRadius);
        if (graphic != null) graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0 || !(graphic is RawImage image)) return;
        Rect rect = image.GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f) return;
        float cut = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * .5f);
        Rect uv = image.uvRect;
        Color32 tint = image.color;
        vh.Clear();
        vh.AddVert(rect.center, tint, new Vector2(uv.x + uv.width * .5f, uv.y + uv.height * .5f));
        const int segments = 12;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector2 center = new Vector2(corner < 2 ? rect.xMin + cut : rect.xMax - cut,
                corner == 0 || corner == 3 ? rect.yMin + cut : rect.yMax - cut);
            // Bottom-left, top-left, top-right, bottom-right; inclusive corner ends.
            for (int sample = 0; sample < segments; sample++)
            {
                float angle = (270f - corner * 90f - sample * 90f / (segments - 1)) * Mathf.Deg2Rad;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * cut;
                Vector2 coord = new Vector2(uv.x + (point.x - rect.xMin) / rect.width * uv.width,
                    uv.y + (point.y - rect.yMin) / rect.height * uv.height);
                vh.AddVert(point, tint, coord);
            }
        }
        int count = segments * 4;
        for (int i = 0; i < count; i++) vh.AddTriangle(0, i + 1, (i + 1) % count + 1);
    }
}
