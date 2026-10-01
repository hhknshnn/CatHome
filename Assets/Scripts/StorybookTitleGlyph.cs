using UnityEngine;
using UnityEngine.UI;

/// <summary>Small title utility marks, drawn sharply without font fallback glyphs.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StorybookTitleGlyph : MaskableGraphic
{
    public enum Symbol { Settings, Credits, Restart, Progress, Exit }
    [SerializeField] private Symbol symbol;
    public void Configure(Symbol value, Color tint) { symbol=value;color=tint;raycastTarget=false;SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if(symbol==Symbol.Settings)
        {
            const int steps=64;
            for(int i=0;i<steps;i++)
            {
                float a=i*Mathf.PI*2/steps,b=(i+1)*Mathf.PI*2/steps;
                float ra=((i+1)/4%2==0)?.44f:.35f,rb=((i+2)/4%2==0)?.44f:.35f;
                Quad(vh,Polar(a,.16f),Polar(b,.16f),Polar(b,rb),Polar(a,ra));
            }
        }
        else if(symbol==Symbol.Credits)
        {
            Circle(vh,new Vector2(0,.23f),.18f,24);
            var p=new Vector2[27];p[0]=new Vector2(-.32f,-.39f);p[1]=new Vector2(.32f,-.39f);
            for(int i=0;i<=24;i++){float a=i*Mathf.PI/24;p[i+2]=new Vector2(Mathf.Cos(a)*.32f,-.17f+Mathf.Sin(a)*.25f);}
            Polygon(vh,p);
        }
        else if(symbol==Symbol.Progress)
        {
            RoundBar(vh,-.30f,-.28f,-.03f);RoundBar(vh,0,-.28f,.15f);RoundBar(vh,.30f,-.28f,.35f);
        }
        else if(symbol==Symbol.Restart)
        {
            for(int i=0;i<40;i++)
            {
                float a=Mathf.Lerp(-1.05f,4.3f,i/40f),b=Mathf.Lerp(-1.05f,4.3f,(i+1)/40f);
                Quad(vh,Polar(a,.27f),Polar(b,.27f),Polar(b,.36f),Polar(a,.36f));
            }
            Polygon(vh,new[]{new Vector2(-.36f,.34f),new Vector2(-.39f,.02f),new Vector2(-.09f,.10f)});
        }
        else
        {
            Quad(vh,new Vector2(-.40f,-.36f),new Vector2(-.30f,-.36f),new Vector2(-.30f,.36f),new Vector2(-.40f,.36f));
            Quad(vh,new Vector2(-.35f,.27f),new Vector2(-.08f,.27f),new Vector2(-.08f,.36f),new Vector2(-.35f,.36f));
            Quad(vh,new Vector2(-.35f,-.36f),new Vector2(-.08f,-.36f),new Vector2(-.08f,-.27f),new Vector2(-.35f,-.27f));
            Quad(vh,new Vector2(-.18f,-.045f),new Vector2(.21f,-.045f),new Vector2(.21f,.045f),new Vector2(-.18f,.045f));
            Polygon(vh,new[]{new Vector2(.12f,-.20f),new Vector2(.39f,0),new Vector2(.12f,.20f)});
        }
    }
    private Vector2 Position(Vector2 p){var r=GetPixelAdjustedRect();return r.center+p*Mathf.Min(r.width,r.height);}
    private static Vector2 Polar(float a,float radius)=>new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
    private void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d){int n=vh.currentVertCount;vh.AddVert(Position(a),color,Vector2.zero);vh.AddVert(Position(b),color,Vector2.zero);vh.AddVert(Position(c),color,Vector2.zero);vh.AddVert(Position(d),color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);}
    private void Circle(VertexHelper vh,Vector2 center,float radius,int steps){var p=new Vector2[steps];for(int i=0;i<steps;i++)p[i]=center+Polar(i*Mathf.PI*2/steps,radius);Polygon(vh,p);}
    private void Polygon(VertexHelper vh,Vector2[] points){int n=vh.currentVertCount;Vector2 center=Vector2.zero;foreach(var p in points)center+=p;center/=points.Length;vh.AddVert(Position(center),color,Vector2.zero);foreach(var p in points)vh.AddVert(Position(p),color,Vector2.zero);for(int i=0;i<points.Length;i++)vh.AddTriangle(n,n+1+i,n+1+(i+1)%points.Length);}
    private void RoundBar(VertexHelper vh,float x,float low,float high){Quad(vh,new Vector2(x-.09f,low),new Vector2(x+.09f,low),new Vector2(x+.09f,high),new Vector2(x-.09f,high));Circle(vh,new Vector2(x,low),.09f,16);Circle(vh,new Vector2(x,high),.09f,16);}
}
