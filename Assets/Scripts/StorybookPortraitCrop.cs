using UnityEngine;
using UnityEngine.UI;

/// <summary>Clips the selected cat's actual portrait without substituting stylized artwork.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Image))]
public sealed class StorybookPortraitCrop : BaseMeshEffect
{
    private const int Segments = 64;

    protected override void OnEnable()
    {
        base.OnEnable();
        var image = graphic as Image;
        if (image != null) image.overrideSprite = null;
    }

    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || mesh.currentVertCount != 4) return;
        var portrait = graphic as Image;
        if (portrait == null || portrait.type != Image.Type.Simple) return;

        UIVertex first = default(UIVertex);
        mesh.PopulateUIVertex(ref first, 0);
        float left = first.position.x, right = left;
        float bottom = first.position.y, top = bottom;
        for (int i = 1; i < 4; i++)
        {
            UIVertex vertex = default(UIVertex);
            mesh.PopulateUIVertex(ref vertex, i);
            left = Mathf.Min(left, vertex.position.x);
            right = Mathf.Max(right, vertex.position.x);
            bottom = Mathf.Min(bottom, vertex.position.y);
            top = Mathf.Max(top, vertex.position.y);
        }
        float width = right - left, height = top - bottom;
        if (width <= .001f || height <= .001f) return;

        float centerX = (left + right) * .5f, centerY = (bottom + top) * .5f;
        UIVertex lowerLeft = first, lowerRight = first, upperLeft = first, upperRight = first;
        for (int i = 0; i < 4; i++)
        {
            UIVertex vertex = default(UIVertex);
            mesh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.y < centerY)
            {
                if (vertex.position.x < centerX) lowerLeft = vertex;
                else lowerRight = vertex;
            }
            else
            {
                if (vertex.position.x < centerX) upperLeft = vertex;
                else upperRight = vertex;
            }
        }

        mesh.Clear();
        mesh.AddVert(Sample(lowerLeft, lowerRight, upperLeft, upperRight, .5f, .5f));
        float radius = Mathf.Min(width, height) * .5f;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            float u = .5f + Mathf.Cos(angle) * radius / width;
            float v = .5f + Mathf.Sin(angle) * radius / height;
            mesh.AddVert(Sample(lowerLeft, lowerRight, upperLeft, upperRight, u, v));
        }
        for (int i = 0; i < Segments; i++)
            mesh.AddTriangle(0, (i + 1) % Segments + 1, i + 1);
    }

    private static UIVertex Sample(UIVertex lowerLeft, UIVertex lowerRight,
        UIVertex upperLeft, UIVertex upperRight, float u, float v)
    {
        return Interpolate(Interpolate(lowerLeft, lowerRight, u),
            Interpolate(upperLeft, upperRight, u), v);
    }

    private static UIVertex Interpolate(UIVertex a, UIVertex b, float amount)
    {
        a.position = Vector3.LerpUnclamped(a.position, b.position, amount);
        a.color = Color32.Lerp(a.color, b.color, amount);
        a.uv0 = Vector4.LerpUnclamped(a.uv0, b.uv0, amount);
        a.uv1 = Vector4.LerpUnclamped(a.uv1, b.uv1, amount);
        a.uv2 = Vector4.LerpUnclamped(a.uv2, b.uv2, amount);
        a.uv3 = Vector4.LerpUnclamped(a.uv3, b.uv3, amount);
        a.normal = Vector3.LerpUnclamped(a.normal, b.normal, amount);
        a.tangent = Vector4.LerpUnclamped(a.tangent, b.tangent, amount);
        return a;
    }
}
