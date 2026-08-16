using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OnboardingCelebrationGraphic : MaskableGraphic
{
    public enum ShapeKind { Burst, Star, Heart, Diamond }

    [SerializeField] private ShapeKind shape;
    [SerializeField, Range(6, 16)] private int burstRays = 12;

    public void Configure(ShapeKind value, Color tint)
    {
        shape = value;
        color = tint;
        raycastTarget = false;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * .5f;
        if (shape == ShapeKind.Burst)
        {
            for (int i = 0; i < burstRays; i++)
            {
                float angle = i * Mathf.PI * 2f / burstRays;
                float half = Mathf.PI / burstRays * .34f;
                float inner = radius * (i % 2 == 0 ? .32f : .43f);
                AddTriangle(vh, center + Direction(angle-half)*inner,
                    center + Direction(angle)*radius, center + Direction(angle+half)*inner, color);
            }
            return;
        }
        if (shape == ShapeKind.Star)
        {
            Vector2[] points = new Vector2[10];
            for(int i=0;i<10;i++) points[i]=center+Direction(Mathf.PI*.5f+i*Mathf.PI/5f)*radius*(i%2==0?1f:.43f);
            AddPolygon(vh,points,color); return;
        }
        if (shape == ShapeKind.Heart)
        {
            AddPolygon(vh,new[]{center+new Vector2(0,-radius),center+new Vector2(-radius*.9f,0),center+new Vector2(-radius*.72f,radius*.72f),center+new Vector2(0,radius*.35f),center+new Vector2(radius*.72f,radius*.72f),center+new Vector2(radius*.9f,0)},color);
            return;
        }
        AddPolygon(vh,new[]{center+Vector2.up*radius,center+Vector2.right*radius,center+Vector2.down*radius,center+Vector2.left*radius},color);
    }

    private static Vector2 Direction(float angle) => new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    private static void AddTriangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)=>AddPolygon(vh,new[]{a,b,c},tint);
    private static void AddPolygon(VertexHelper vh,Vector2[] points,Color tint)
    {
        int start=vh.currentVertCount; Vector2 center=Vector2.zero;
        for(int i=0;i<points.Length;i++)center+=points[i]; center/=points.Length;
        vh.AddVert(center,tint,Vector2.zero);
        for(int i=0;i<points.Length;i++)vh.AddVert(points[i],tint,Vector2.zero);
        for(int i=0;i<points.Length;i++)vh.AddTriangle(start,start+1+i,start+1+(i+1)%points.Length);
    }
}
