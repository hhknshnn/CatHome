using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LowPolyTutorialPointerGraphic : MaskableGraphic
{
    [SerializeField] private Color shadowColor = new Color32(139, 65, 31, 255);
    [SerializeField] private Color frameColor = new Color32(246, 111, 66, 255);
    [SerializeField] private Color faceColor = new Color32(255, 231, 184, 255);
    private float aimOffset;

    public void SetAim(float horizontalOffset)
    {
        float next = Mathf.Clamp(horizontalOffset * 0.18f, -18f, 18f);
        if (Mathf.Approximately(next, aimOffset)) return;
        aimOffset = next;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        Vector2 topLeft = new Vector2(r.center.x - 14f, r.yMax);
        Vector2 topRight = new Vector2(r.center.x + 14f, r.yMax);
        Vector2 tip = new Vector2(r.center.x + aimOffset, r.yMin + 2f);
        AddTriangle(vh, topLeft + new Vector2(4f, -4f), topRight + new Vector2(4f, -4f), tip + new Vector2(4f, -4f), shadowColor);
        AddTriangle(vh, topLeft, topRight, tip, frameColor);
        AddTriangle(vh, topLeft + new Vector2(5f, -2f), topRight + new Vector2(-5f, -2f), Vector2.Lerp(tip, new Vector2(r.center.x, r.yMax), 0.17f), faceColor);
    }

    private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero);
        vh.AddVert(b, color, Vector2.zero);
        vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }
}
