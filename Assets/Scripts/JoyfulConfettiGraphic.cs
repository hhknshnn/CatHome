using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class JoyfulConfettiGraphic:MaskableGraphic
{
    private static readonly Color[] Palette={JoyfulUiArt.Gold,JoyfulUiArt.OceanLight,JoyfulUiArt.Coral,JoyfulUiArt.Purple};
    private float elapsed=3;
    protected override void Awake(){base.Awake();raycastTarget=false;}
    public void Replay(){elapsed=CatRunnerProgressService.ReducedMotion?3:0;SetVerticesDirty();}
    protected override void OnEnable(){base.OnEnable();Replay();}
    private void Update()
    {
        if(elapsed>=2.2f)return;
        elapsed=CatRunnerProgressService.ReducedMotion?3:elapsed+Time.unscaledDeltaTime;SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();if(elapsed>=2.2f)return;var r=GetPixelAdjustedRect();
        for(int i=0;i<24;i++)
        {
            float u=Mathf.Repeat(i*.618034f,1);float t=elapsed/2.2f;
            float x=Mathf.Lerp(r.xMin+18,r.xMax-18,u)+Mathf.Sin(i+t*5)*16;
            float y=r.yMax+20-(r.height+60)*Mathf.Clamp01(t*(.65f+u*.35f));
            if(y>r.yMax-4||y<r.yMin+4)continue;
            var c=Palette[i%4];c.a=(1-t)*.85f;var p=new Vector2(x,y);
            float a=i+t*5;var dx=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5;var dy=new Vector2(-dx.y,dx.x)*.5f;
            JoyfulMotifGraphic.Quad(vh,p-dx-dy,p-dx+dy,p+dx+dy,p+dx-dy,c);
        }
    }
}
