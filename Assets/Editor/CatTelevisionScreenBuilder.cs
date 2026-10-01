using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

public static class CatTelevisionScreenBuilder
{
    public static void Apply(GameObject product)
    {
        var clip=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Art/Television/CatHomeTelevision.mp4");
        var poster=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Television/CatTelevisionPoster.png");
        if(clip==null || poster==null)return;
        Transform model=null;foreach(var f in product.GetComponentsInChildren<MeshFilter>(true))if(f.name=="ModernTelevision_PremiumModel"){model=f.transform;break;}
        if(model==null)return;
        var old=model.Find("Cat Television Broadcast");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var screen=GameObject.CreatePrimitive(PrimitiveType.Quad);screen.name="Cat Television Broadcast";Object.DestroyImmediate(screen.GetComponent<Collider>());
        screen.transform.SetParent(model,false);screen.transform.localPosition=new Vector3(0,.50f,.09f);screen.transform.localRotation=Quaternion.Euler(0,180,0);screen.transform.localScale=new Vector3(1.42f,1.42f*9f/16f,1);
        const string path="Assets/Art/Television/CatTelevisionScreen.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",poster);material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);
        screen.GetComponent<MeshRenderer>().sharedMaterial=material;
        var video=screen.AddComponent<VideoPlayer>();video.playOnAwake=false;video.audioOutputMode=VideoAudioOutputMode.None;
        screen.AddComponent<CatTelevisionScreen>().EditorConfigure(clip,poster);
    }
}
