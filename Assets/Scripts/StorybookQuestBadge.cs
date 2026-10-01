using UnityEngine;
using UnityEngine.UI;
using QuestState = CatHome.Quests.QuestState;

/// <summary>Read-only quest progress; rebuilt only when its snapshot changes.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StorybookQuestBadge : MaskableGraphic
{
    private float progress;
    private QuestState state;

    public void Configure(QuestSnapshot snapshot)
    {
        float value = snapshot.RequiredCount > 0 ? Mathf.Clamp01((float)snapshot.Count / snapshot.RequiredCount) : 0f;
        raycastTarget = false;
        if (state == snapshot.State && Mathf.Approximately(progress, value)) return;
        progress = value;
        state = snapshot.State;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Color accent = state == QuestState.Locked ? StorybookScreenStyle.Muted : StorybookQuestPresentation.PositiveText;
        Circle(vh, Vector2.zero, .37f, new Color32(226, 238, 226, 255), 32);
        const int steps = 64;
        for (int i = 0; i < steps; i++)
        {
            float a = Mathf.PI * .5f - i * Mathf.PI * 2f / steps;
            float b = Mathf.PI * .5f - (i+1) * Mathf.PI * 2f / steps;
            Color tint = (float)i / steps < progress && state != QuestState.Locked ? StorybookScreenStyle.Teal : new Color32(220, 225, 211, 255);
            Quad(vh, Polar(a,.415f), Polar(b,.415f), Polar(b,.48f), Polar(a,.48f), tint);
        }
        if (state == QuestState.Completed || state == QuestState.Claimed)
        {
            Stroke(vh,new Vector2(-.19f,0),new Vector2(-.055f,-.13f),.065f,accent);
            Stroke(vh,new Vector2(-.055f,-.13f),new Vector2(.22f,.16f),.065f,accent);
        }
        else if (state == QuestState.Locked)
        {
            for(int i=0;i<20;i++)
            {
                float a=i*Mathf.PI/20f,b=(i+1)*Mathf.PI/20f;
                Quad(vh,Polar(a,.09f)+Vector2.up*.08f,Polar(b,.09f)+Vector2.up*.08f,
                    Polar(b,.15f)+Vector2.up*.08f,Polar(a,.15f)+Vector2.up*.08f,accent);
            }
            Quad(vh,new Vector2(-.18f,-.17f),new Vector2(.18f,-.17f),new Vector2(.18f,.075f),new Vector2(-.18f,.075f),accent);
        }
        else
        {
            Circle(vh,new Vector2(0,-.10f),.15f,accent,20);
            Circle(vh,new Vector2(-.20f,.035f),.073f,accent,12);
            Circle(vh,new Vector2(-.08f,.19f),.073f,accent,12);
            Circle(vh,new Vector2(.09f,.19f),.073f,accent,12);
            Circle(vh,new Vector2(.21f,.035f),.073f,accent,12);
        }
    }
    private Vector2 Position(Vector2 p) { var r=GetPixelAdjustedRect(); return r.center+p*Mathf.Min(r.width,r.height); }
    private static Vector2 Polar(float a,float radius) => new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
    private void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint)
    { int n=vh.currentVertCount;vh.AddVert(Position(a),tint,Vector2.zero);vh.AddVert(Position(b),tint,Vector2.zero);vh.AddVert(Position(c),tint,Vector2.zero);vh.AddVert(Position(d),tint,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3); }
    private void Circle(VertexHelper vh,Vector2 center,float radius,Color tint,int steps)
    { int n=vh.currentVertCount;vh.AddVert(Position(center),tint,Vector2.zero);for(int i=0;i<=steps;i++)vh.AddVert(Position(center+Polar(i*Mathf.PI*2/steps,radius)),tint,Vector2.zero);for(int i=0;i<steps;i++)vh.AddTriangle(n,n+i+1,n+i+2); }
    private void Stroke(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
    { var direction=(b-a).normalized;var normal=new Vector2(-direction.y,direction.x)*width*.5f;Quad(vh,a-normal,b-normal,b+normal,a+normal,tint);Circle(vh,a,width*.5f,tint,10);Circle(vh,b,width*.5f,tint,10); }
}
