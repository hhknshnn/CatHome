using UnityEngine;
using UnityEngine.Video;

/// <summary>A baked cartoon broadcast, with no extra runtime scene camera.</summary>
[RequireComponent(typeof(MeshRenderer),typeof(VideoPlayer))]
public sealed class CatTelevisionScreen : MonoBehaviour
{
    [SerializeField] VideoClip clip;
    [SerializeField] Texture2D poster;
    VideoPlayer player;
    Material screenMaterial;
    Material originalMaterial;
    MeshRenderer screenRenderer;
    RenderTexture output;
    void OnEnable()
    {
        if(!Application.isPlaying)return;
        player=GetComponent<VideoPlayer>();screenRenderer=GetComponent<MeshRenderer>();
        originalMaterial=screenRenderer.sharedMaterial;screenMaterial=new Material(originalMaterial);
        screenRenderer.sharedMaterial=screenMaterial;
        screenMaterial.SetTexture("_BaseMap",poster);
        if(clip==null || CatRunnerProgressService.ReducedMotion)return;
        output=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32){name="Cat TV Cartoon",filterMode=FilterMode.Bilinear};output.Create();
        player.clip=clip;player.isLooping=true;player.playOnAwake=false;player.audioOutputMode=VideoAudioOutputMode.None;
        player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=output;player.waitForFirstFrame=true;
        player.prepareCompleted+=Prepared;player.Prepare();
    }
    void Prepared(VideoPlayer source){if(!isActiveAndEnabled)return;screenMaterial.SetTexture("_BaseMap",output);source.Play();}
    void OnDisable()
    {
        if(player!=null){player.prepareCompleted-=Prepared;player.Stop();player.targetTexture=null;}
        if(output!=null){output.Release();Destroy(output);output=null;}
        if(screenRenderer!=null && originalMaterial!=null)screenRenderer.sharedMaterial=originalMaterial;
        if(screenMaterial!=null){Destroy(screenMaterial);screenMaterial=null;}
    }
#if UNITY_EDITOR
    public void EditorConfigure(VideoClip movie,Texture2D still){clip=movie;poster=still;}
#endif
}
