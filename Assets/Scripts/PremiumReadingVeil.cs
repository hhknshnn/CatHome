using UnityEngine;
using UnityEngine.UI;

/// <summary>Blends the reading area into the live scene instead of cutting it in two.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PremiumReadingVeil : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var r = rectTransform.rect;
        const int steps = 32;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Color tint = PremiumUiStyle.Ivory;
            tint.a = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.60f, 1f, t));
            float x = Mathf.Lerp(r.xMin, r.xMax, t);
            vh.AddVert(new Vector3(x, r.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector3(x, r.yMax), tint, Vector2.one);
            if(i == 0) continue;
            int n = i * 2; vh.AddTriangle(n-2,n-1,n); vh.AddTriangle(n,n-1,n+1);
        }
    }
}
