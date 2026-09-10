using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>One world-only, softly defocused snapshot per modal opening.</summary>
public sealed class PremiumModalBackdrop : MonoBehaviour
{
    RawImage image;
    RenderTexture output;
    Material opaqueCopy;
    bool captured;
    Vector2Int size;
    void LateUpdate()
    {
        if(!Application.isPlaying)return;
        float alpha=1;foreach(var group in GetComponentsInParent<CanvasGroup>())alpha*=group.alpha;
        bool visible=alpha>.025f&&gameObject.activeInHierarchy&&!TitleScreen.IsShowing;
        if(!visible){if(captured)Release();return;}
        var current=new Vector2Int(Screen.width,Screen.height);
        if(captured&&size==current)return;
        var camera=Camera.main;if(camera==null||!camera.isActiveAndEnabled)return;
        if(image==null)
        {
            var rect=PremiumUiElements.Rect("ReferenceWorldBlur",transform);PremiumUiElements.Fill(rect);rect.SetAsFirstSibling();
            image=rect.gameObject.AddComponent<RawImage>();image.raycastTarget=false;
            image.color=new Color(.86f,.86f,.81f,1);
        }
        if(opaqueCopy==null)
        {
            var shader=Resources.Load<Shader>("PremiumInterface/BackdropCopy");if(shader==null)return;
            opaqueCopy=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
        }
        Release();size=current;
        int w=Mathf.Clamp(Screen.width/3,320,960),h=Mathf.Max(180,Mathf.RoundToInt(w*Screen.height/(float)Screen.width));
        var source=RenderTexture.GetTemporary(w,h,24,RenderTextureFormat.ARGB32);
        RenderTexture medium=null;
        Rect worldViewport=camera.rect;
        try
        {
            camera.rect=new Rect(0,0,1,1);
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=source};
            if(!RenderPipeline.SupportsRenderRequest(camera,request))return;
            RenderPipeline.SubmitRenderRequest(camera,request);
            medium=RenderTexture.GetTemporary(w,h,0);
            source.filterMode=medium.filterMode=FilterMode.Bilinear;
            opaqueCopy.SetVector("_BlurDirection",new Vector4(2.5f/w,0,0,0));
            Graphics.Blit(source,medium,opaqueCopy);
            output=new RenderTexture(w,h,0){name="Modal soft world",filterMode=FilterMode.Bilinear};output.Create();
            opaqueCopy.SetVector("_BlurDirection",new Vector4(0,2.5f/h,0,0));
            Graphics.Blit(medium,output,opaqueCopy);image.texture=output;image.enabled=true;captured=true;
        }
        finally
        {
            camera.rect=worldViewport;
            RenderTexture.ReleaseTemporary(source);
            if(medium!=null)RenderTexture.ReleaseTemporary(medium);
        }
    }
    void Release()
    {
        captured=false;
        if(image!=null){image.texture=null;image.enabled=false;}
        if(output!=null){output.Release();Destroy(output);output=null;}
    }
    void OnDisable()=>Release();
    void OnDestroy(){Release();if(opaqueCopy!=null)Destroy(opaqueCopy);}
}
