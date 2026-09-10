using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>HD previews photographed from the built game worlds, never a concept illustration.</summary>
public static class MiniGamePreviewBuilder
{
    public static void CaptureLive(bool runner)
    {
        if(!Application.isPlaying||!EditorQaSession.IsActive)throw new System.InvalidOperationException("Use the isolated live QA scene.");
        var camera=runner?UiQaVisualTour.Find<CatRunnerGameController>().GetComponentInChildren<Camera>():UiQaVisualTour.Find<CatCatchGameController>().GetComponentInChildren<Camera>();
        string path="Assets/Art/Games/"+(runner?"Runner":"Catch")+"Preview.png";
        var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();var bytes=texture.EncodeToPNG();File.WriteAllBytes(path,bytes);if(runner)File.WriteAllBytes("Assets/Art/Runner/UI/CatRunnerHero_v1.png",bytes);}
        finally{camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(texture);}
    }
}
