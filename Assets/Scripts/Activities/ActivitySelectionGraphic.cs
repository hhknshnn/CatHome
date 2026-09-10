using UnityEngine;
using UnityEngine.UI;

/// <summary>Light corner marks identify the selected product without covering it.</summary>
public sealed class ActivitySelectionGraphic : MaskableGraphic
{
    Transform target;
    Renderer[] surfaces;
    public void SetTarget(Transform value)
    {
        if(target==value)return;
        target=value;surfaces=target!=null?target.GetComponentsInChildren<Renderer>():null;
        raycastTarget=false;SetVerticesDirty();
    }
    void LateUpdate()
    {
        if(target==null || Camera.main==null){canvasRenderer.SetAlpha(0);return;}
        bool found=false;Vector2 min=Vector2.one*float.PositiveInfinity,max=Vector2.one*float.NegativeInfinity;
        foreach(var surface in surfaces)
        {
            if(surface==null || !surface.enabled || !surface.gameObject.activeInHierarchy)continue;
            var bounds=surface.bounds;
            for(int i=0;i<8;i++)
            {
                var world=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var point=Camera.main.WorldToScreenPoint(world);if(point.z<=0)continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent,point,null,out var local);
                min=Vector2.Min(min,local);max=Vector2.Max(max,local);found=true;
            }
        }
        canvasRenderer.SetAlpha(found?1:0);if(!found)return;
        rectTransform.localPosition=(min+max)*.5f;rectTransform.sizeDelta=max-min+Vector2.one*16;
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=rectTransform.rect;float length=Mathf.Min(22,Mathf.Min(r.width,r.height)*.22f),thickness=2.5f;
        for(int x=0;x<2;x++)for(int y=0;y<2;y++)
        {
            float px=x==0?r.xMin:r.xMax-length,py=y==0?r.yMin:r.yMax-thickness;
            Quad(vh,new Rect(px,py,length,thickness));
            px=x==0?r.xMin:r.xMax-thickness;py=y==0?r.yMin:r.yMax-length;
            Quad(vh,new Rect(px,py,thickness,length));
        }
    }
    void Quad(VertexHelper vh,Rect r)
    {
        int n=vh.currentVertCount;var c=(Color32)color;
        vh.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);
        vh.AddVert(new Vector3(r.xMax,r.yMax),c,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),c,Vector2.zero);
        vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
    }
}
