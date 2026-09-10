using UnityEngine;
using UnityEngine.UI;

/// <summary>A single decorative UI mesh: no textures, input surfaces or particle system.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class JoyfulMotifGraphic:MaskableGraphic
{
    public bool Rays;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=GetPixelAdjustedRect();
        if(Rays)
        {
            float radius=Mathf.Min(r.width,r.height)*.49f;
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6f;
                Quad(vh,Polar(a-.075f,radius*.28f),Polar(a-.15f,radius),Polar(a+.15f,radius),Polar(a+.075f,radius*.28f),color);
            }
        }
        else for(int i=0;i<10;i++)
        {
            float x=Mathf.Lerp(r.xMin+12,r.xMax-12,Mathf.Repeat(i*.381966f+.07f,1));
            float y=Mathf.Lerp(r.yMin+12,r.yMax-12,Mathf.Repeat(i*.618034f+.13f,1));
            float size=i%3==0?9:5;var p=new Vector2(x,y);
            Quad(vh,p+new Vector2(0,size),p+new Vector2(size*.5f,0),p-new Vector2(0,size),p-new Vector2(size*.5f,0),color);
        }
    }
    static Vector2 Polar(float a,float r)=>new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
    internal static void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color)
    {int n=vh.currentVertCount;vh.AddVert(a,color,Vector2.zero);vh.AddVert(b,color,Vector2.zero);vh.AddVert(c,color,Vector2.zero);vh.AddVert(d,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
}

