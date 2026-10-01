using UnityEngine;
using UnityEngine.UI;

/// <summary>Opaque satin navy on the existing reserved strip; no extra input or layout object.</summary>
[DisallowMultipleComponent]
public sealed class PremiumHudNavySurface : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper mesh)
    {
        if(!IsActive())return;
        var rect=graphic.rectTransform.rect;
        mesh.Clear();
        const int columns=32,rows=8;
        for(int y=0;y<=rows;y++)
            for(int x=0;x<=columns;x++)
            {
                float u=(float)x/columns,v=(float)y/rows;
                Color tone=Color.Lerp(new Color32(29,61,124,255),new Color32(68,117,181,255),v);
                float light=Mathf.Exp(-Mathf.Pow((u-.28f)/.34f,2)-Mathf.Pow((v-.92f)/.72f,2));
                tone=Color.Lerp(tone,new Color32(115,178,230,255),light*.32f);
                tone.a=graphic.color.a;
                mesh.AddVert(new Vector3(Mathf.Lerp(rect.xMin,rect.xMax,u),Mathf.Lerp(rect.yMin,rect.yMax,v)),tone,Vector2.zero);
                if(x==0||y==0)continue;
                int i=y*(columns+1)+x;
                mesh.AddTriangle(i-columns-2,i-columns-1,i);
                mesh.AddTriangle(i-columns-2,i,i-1);
            }
    }
}
