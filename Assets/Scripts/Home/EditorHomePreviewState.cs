#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Temporary editor presentation. Never included in a scene save or player.</summary>
[ExecuteAlways]
public sealed class EditorHomePreviewState : MonoBehaviour
{
    [Serializable] class Visibility { public GameObject target;public bool original,shown; }
    [Serializable] class Pose { public Transform target;public Vector3 originalPosition,shownPosition;public Quaternion originalRotation,shownRotation; }
    [Serializable] class LayoutRect { public RectTransform target;public Vector2 originalPosition,shownPosition,originalSize,shownSize;public Vector3 originalScale,shownScale; }
    [SerializeField] List<Visibility> visibility=new List<Visibility>();
    [SerializeField] List<Pose> poses=new List<Pose>();
    [SerializeField] List<Renderer> hidden=new List<Renderer>();
    [SerializeField] List<LayoutRect> layoutRects=new List<LayoutRect>();
    [SerializeField] Camera previewCamera;
    [SerializeField] float originalFov,shownFov;
    TopHudResponsiveLayout topLayout;
    [SerializeField] HomeStoreSaveState originalStore;
    [SerializeField] bool restored;
    RectTransform dock,safe,sourceDock;
    public void ConfigureDock(RectTransform value,RectTransform safeArea,RectTransform source){dock=value;safe=safeArea;sourceDock=source;}
    public void ConfigureProjection(Camera camera,RectTransform food,RectTransform water,RectTransform rest)
    {
        previewCamera=camera;if(camera!=null){originalFov=camera.fieldOfView;shownFov=originalFov;}
        if(food==null||water==null||rest==null)return;
        foreach(var rect in new[]{food,water,rest})
            layoutRects.Add(new LayoutRect{target=rect,originalPosition=rect.anchoredPosition,originalSize=rect.sizeDelta,originalScale=rect.localScale});
        topLayout=gameObject.AddComponent<TopHudResponsiveLayout>();topLayout.ConfigureEditorPreview(food,water,rest);
        RefreshProjection();
    }
    void RefreshProjection()
    {
        if(topLayout!=null)topLayout.RefreshEditorPreview();
        foreach(var item in layoutRects)if(item.target!=null)
        {item.shownPosition=item.target.anchoredPosition;item.shownSize=item.target.sizeDelta;item.shownScale=item.target.localScale;}
        if(previewCamera!=null&&previewCamera.targetTexture==null)
        {
            shownFov=HomeWorldViewport.FitFieldOfView(originalFov,Screen.width/(Mathf.Max(1f,Screen.height)*previewCamera.rect.height));
            previewCamera.fieldOfView=shownFov;
        }
    }
    void LateUpdate()
    {
        if(Application.isPlaying){Restore();gameObject.SetActive(false);return;}
        RefreshProjection();
        if(dock==null||safe==null)return;
        PremiumHomeDockLayout.AlignShortcut(sourceDock,dock);
    }
    public void CaptureStore(){originalStore=HomeStoreService.CaptureState();}
    public void Show(GameObject target,bool visible)
    {
        if(target==null)return;
        var old=visibility.Find(v=>v.target==target);
        if(old==null){old=new Visibility{target=target,original=target.activeSelf};visibility.Add(old);}
        old.shown=visible;target.SetActive(visible);
    }
    public void Place(Transform target,Vector3 position,Quaternion rotation)
    {
        var old=poses.Find(p=>p.target==target);
        if(old==null){old=new Pose{target=target,originalPosition=target.position,originalRotation=target.rotation};poses.Add(old);}
        old.shownPosition=position;old.shownRotation=rotation;target.SetPositionAndRotation(position,rotation);
    }
    public void HideRenderer(Renderer renderer)
    {if(renderer!=null&&!renderer.forceRenderingOff){hidden.Add(renderer);renderer.forceRenderingOff=true;}}
    public void Restore()
    {
        if(restored)return;restored=true;
        if(!Application.isPlaying&&originalStore!=null)HomeStoreService.ApplySavedState(originalStore);
        foreach(var item in visibility)
            if(item.target!=null&&item.target.activeSelf==item.shown)item.target.SetActive(item.original);
        foreach(var item in poses)
            if(item.target!=null&&(item.target.position-item.shownPosition).sqrMagnitude<.000001f&&Quaternion.Angle(item.target.rotation,item.shownRotation)<.01f)
                item.target.SetPositionAndRotation(item.originalPosition,item.originalRotation);
        foreach(var renderer in hidden)if(renderer!=null)renderer.forceRenderingOff=false;
        foreach(var item in layoutRects)if(item.target!=null)
        {
            if((item.target.anchoredPosition-item.shownPosition).sqrMagnitude<.001f)item.target.anchoredPosition=item.originalPosition;
            if((item.target.sizeDelta-item.shownSize).sqrMagnitude<.001f)item.target.sizeDelta=item.originalSize;
            if((item.target.localScale-item.shownScale).sqrMagnitude<.001f)item.target.localScale=item.originalScale;
        }
        if(previewCamera!=null&&Mathf.Abs(previewCamera.fieldOfView-shownFov)<.01f)previewCamera.fieldOfView=originalFov;
    }
    void OnEnable(){if(Application.isPlaying){Restore();gameObject.SetActive(false);}}
    void OnDisable(){Restore();}
    void OnDestroy(){Restore();}
}
#endif
