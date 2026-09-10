using UnityEngine;

/// <summary>Reserve the navigation strip below the room, including safe-area padding.</summary>
[ExecuteAlways, RequireComponent(typeof(Camera))]
public sealed class HomeWorldViewport : MonoBehaviour
{
    public const float StripHeight=80f;
    Camera roomCamera;
    Canvas homeCanvas;
    bool titleCulled;
    int worldMask;
    [SerializeField] float referenceFieldOfView;
    public void ConfigureFraming(float fieldOfView){referenceFieldOfView=fieldOfView;}
    public static float FitFieldOfView(float verticalFov,float aspect)
    {
        float expansion=Mathf.Max(1,1.92f/Mathf.Max(.5f,aspect));
        return 2*Mathf.Atan(Mathf.Tan(verticalFov*Mathf.Deg2Rad*.5f)*expansion)*Mathf.Rad2Deg;
    }
    void LateUpdate()
    {
        if(roomCamera==null)roomCamera=GetComponent<Camera>();
        SetWorldHidden(Application.isPlaying && MobilePresentation.IsMobile && TitleScreen.IsShowing && roomCamera.targetTexture==null);
        if(roomCamera.targetTexture!=null){roomCamera.rect=new Rect(0,0,1,1);return;}
        if(homeCanvas==null)foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(canvas.name=="Canvas" && canvas.isRootCanvas){homeCanvas=canvas;break;}
        float scale=homeCanvas!=null?homeCanvas.scaleFactor:Screen.height/1080f;
        float y=Mathf.Clamp((Screen.safeArea.yMin+StripHeight*scale)/Mathf.Max(1,Screen.height),0,.22f);
        roomCamera.rect=new Rect(0,y,1,1-y);
        if(Application.isPlaying && referenceFieldOfView>0)
            roomCamera.fieldOfView=FitFieldOfView(referenceFieldOfView,Screen.width/(Screen.height*(1-y)));
    }
    void SetWorldHidden(bool hidden)
    {
        if(hidden==titleCulled||roomCamera==null)return;
        if(hidden){worldMask=roomCamera.cullingMask;roomCamera.cullingMask=0;}
        else roomCamera.cullingMask=worldMask;
        titleCulled=hidden;
    }
    void OnDisable(){SetWorldHidden(false);if(roomCamera!=null){roomCamera.rect=new Rect(0,0,1,1);if(referenceFieldOfView>0)roomCamera.fieldOfView=referenceFieldOfView;}}
}
