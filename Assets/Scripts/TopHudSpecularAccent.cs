using UnityEngine;
using UnityEngine.UI;

/// <summary>Decorative top HUD glints only; no layout, input, or service ownership.</summary>
public sealed class TopHudSpecularAccent : MaskableGraphic
{
    private float period=5,offset,baseOpacity=.18f,lastSample=-1;
    private bool sweep,motion;
    private float pulse,travel;
    public void Configure(Color tint,float interval,float phase,bool isSweep=false,float restingOpacity=.18f)
    {
        color=tint;period=interval;offset=phase;sweep=isSweep;baseOpacity=restingOpacity;
        raycastTarget=false;SetVerticesDirty();
    }
    public void SetMotion(bool enabled){motion=enabled;pulse=0;travel=0;SetVerticesDirty();}
    private void Update()
    {
        if(!Application.isPlaying||!motion)return;
        float sample=Mathf.Floor(Time.unscaledTime*30f)/30f;
        if(Mathf.Approximately(sample,lastSample))return;lastSample=sample;
        float phase=Mathf.Repeat(sample+offset,period);
        float next=phase<.65f?Mathf.Sin(phase/.65f*Mathf.PI):0;
        if(next==0&&pulse==0)return;
        pulse=next;travel=phase/.65f;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Rect r=rectTransform.rect;
        Color c=Color.Lerp(color,Color.white,pulse*.7f);c.a=color.a*(baseOpacity+(1-baseOpacity)*pulse);
        if(sweep)
        {
            c.a*=.35f;if(c.a<=.001f)return;
            float x=Mathf.Lerp(r.xMin,r.xMax,Mathf.Clamp01(travel));float w=r.width*.13f;
            var p0=new Vector2(Mathf.Clamp(x-w-r.width*.13f,r.xMin,r.xMax),r.yMin);
            var p1=new Vector2(Mathf.Clamp(x+w-r.width*.13f,r.xMin,r.xMax),r.yMin);
            var p2=new Vector2(Mathf.Clamp(x+w+r.width*.13f,r.xMin,r.xMax),r.yMax);
            var p3=new Vector2(Mathf.Clamp(x-w+r.width*.13f,r.xMin,r.xMax),r.yMax);
            vh.AddVert(p0,c,Vector2.zero);vh.AddVert(p1,c,Vector2.zero);vh.AddVert(p2,c,Vector2.zero);vh.AddVert(p3,c,Vector2.zero);
            vh.AddTriangle(0,1,2);vh.AddTriangle(0,2,3);return;
        }
        vh.AddVert(r.center,c,Vector2.zero);
        for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f;float s=i%2==0?1:.21f;vh.AddVert(r.center+new Vector2(Mathf.Cos(a)*r.width*.5f*s,Mathf.Sin(a)*r.height*.5f*s),c,Vector2.zero);}
        for(int i=0;i<8;i++)vh.AddTriangle(0,1+i,1+(i+1)%8);
    }
}
