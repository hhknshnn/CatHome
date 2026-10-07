using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Small, offline portraits of the actual selected breed and command pose.</summary>
public static class CompanionPreviewBuilder
{
    public const string Folder="Assets/Resources/Companion";
    private const int Width=512, Height=288, PreviewLayer=31;
    private static readonly string[] Poses={"Meow","Sit","Loaf"};

    public static void Build(bool diagnostics=false)
    {
        if(Application.isPlaying)throw new InvalidOperationException("Companion portraits must be authored outside Play mode.");
        var catalog=CatBreedCatalog.Load();
        if(diagnostics)
        {
            const string studies="Docs/QA/PRODUCTION_PASS_2026-09-07/pose-studies";
            Directory.CreateDirectory(studies);
            foreach(string pose in new[]{"Sleeping","Transition50","Transition80"})
                Render(catalog.Find(CatBreedCatalog.DefaultBreedId),catalog,pose,studies+"/"+pose+".png",true);
            return;
        }

        Directory.CreateDirectory(Folder);
        var paths=new List<string>();
        foreach(var breed in catalog.Entries)
        {
            string folder=Folder+"/"+breed.Id;
            Directory.CreateDirectory(folder);
            foreach(string pose in Poses)
            {
                string path=folder+"/"+pose+".png";
                Render(breed,catalog,pose,path,false);
                paths.Add(path);
                if(breed.Id==CatBreedCatalog.DefaultBreedId)
                {
                    // Existing callers and older UI prefabs still have these canonical paths.
                    string canonical=Folder+"/"+pose+".png";
                    File.Copy(path,canonical,true);
                    paths.Add(canonical);
                }
            }
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(string path in paths)ImportPortrait(path);
        AssetDatabase.SaveAssets();
        Debug.Log("[Cat Home] Companion portraits: "+catalog.Entries.Count+" breeds, three real poses each; 512x288, Android ETC2.");
    }

    private static void Render(CatBreedCatalog.Entry breed,CatBreedCatalog catalog,string pose,string path,bool diagnostics)
    {
        var preview=new PreviewRenderUtility();Texture2D texture=null;
        var mesh=new Mesh();
        try
        {
            var cat=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null);
            if(cat==null)throw new InvalidOperationException("Missing companion portrait breed: "+breed.Id);
            preview.AddSingleGO(cat);
            foreach(var joint in cat.GetComponentsInChildren<Transform>(true))joint.gameObject.layer=PreviewLayer;
            var animator=cat.GetComponentInChildren<Animator>();animator.enabled=false;
            var clip=ResolveClip(catalog,pose,diagnostics);
            if(clip==null)throw new InvalidOperationException("Missing companion pose: "+pose);
            float time=diagnostics?clip.length*(pose=="Transition50"?.5f:pose=="Transition80"?.8f:0):pose=="Meow"?.8f:0;
            clip.SampleAnimation(animator.gameObject,time);
            cat.transform.rotation=Quaternion.Euler(0,140,0);

            var vertices=new List<Vector3>();
            foreach(var skin in cat.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.BakeMesh(mesh,true);
                foreach(var point in mesh.vertices)vertices.Add(skin.transform.TransformPoint(point));
            }
            if(vertices.Count==0)throw new InvalidOperationException("Empty companion portrait: "+breed.Id);
            var bounds=new Bounds(vertices[0],Vector3.zero);
            foreach(var vertex in vertices)bounds.Encapsulate(vertex);
            var camera=preview.camera;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Background(pose);
            camera.cullingMask=1<<PreviewLayer;
            camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=30;
            camera.allowHDR=false;camera.useOcclusionCulling=false;
            camera.transform.position=bounds.center+new Vector3(1.7f,1.05f,-3.6f);
            camera.transform.LookAt(bounds.center);
            // Project the posed vertices, not the enclosing world-space box. Long
            // tails and different ear shapes still fit, without tiny cats in the cards.
            Vector2 minimum=new Vector2(float.PositiveInfinity,float.PositiveInfinity);
            Vector2 maximum=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
            foreach(var vertex in vertices)
            {
                var point=camera.transform.InverseTransformPoint(vertex);
                minimum=Vector2.Min(minimum,new Vector2(point.x,point.y));
                maximum=Vector2.Max(maximum,new Vector2(point.x,point.y));
            }
            Vector2 center=(minimum+maximum)*.5f,half=(maximum-minimum)*.5f;
            camera.transform.position+=camera.transform.right*center.x+camera.transform.up*center.y;
            camera.orthographicSize=Mathf.Max(half.y,half.x/(Width/(float)Height))*1.13f;
            preview.ambientColor=new Color(.68f,.72f,.75f);
            preview.lights[0].intensity=1.25f;preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);
            preview.lights[1].intensity=.75f;preview.lights[1].transform.rotation=Quaternion.Euler(320,210,0);
            foreach(var light in preview.lights)light.cullingMask=1<<PreviewLayer;
            preview.BeginPreview(new Rect(0,0,Width,Height),GUIStyle.none);preview.Render(true);preview.EndPreview();
            preview.BeginPreview(new Rect(0,0,Width,Height),GUIStyle.none);preview.Render(true);
            var rendered=preview.EndPreview() as RenderTexture;
            if(rendered==null)throw new InvalidOperationException("Portrait rendering returned no image: "+path);
            var previous=RenderTexture.active;
            var resolved=RenderTexture.GetTemporary(Width,Height,0,RenderTextureFormat.ARGB32);
            try
            {
                Graphics.Blit(rendered,resolved);RenderTexture.active=resolved;
                texture=new Texture2D(Width,Height,TextureFormat.RGBA32,false);
                texture.ReadPixels(new Rect(0,0,Width,Height),0,0);texture.Apply();
            }
            finally { RenderTexture.active=previous;RenderTexture.ReleaseTemporary(resolved); }
            File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally
        {
            if(texture!=null)Object.DestroyImmediate(texture);
            Object.DestroyImmediate(mesh);
            preview.Cleanup();
        }
    }

    private static AnimationClip ResolveClip(CatBreedCatalog catalog,string pose,bool diagnostics)
    {
        if(diagnostics)
            return AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath)
                .OfType<AnimationClip>().First(c=>!c.name.StartsWith("__")&&
                    c.name.EndsWith("|"+(pose=="Sleeping"?"Sleeping":"Sitting_to_Sleep"),StringComparison.Ordinal));
        if(pose=="Sit")return catalog.GameplayController.animationClips.First(c=>c.name.EndsWith("|Sitting",StringComparison.Ordinal));
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(CompanionAnimationBuilder.Folder+"/Companion"+pose+".anim");
    }

    private static Color Background(string pose)
    {
        return Color.clear;
    }

    private static void ImportPortrait(string path)
    {
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=true;
        importer.sRGBTexture=true;
        importer.mipmapEnabled=false;importer.isReadable=false;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=512;
        importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Compressed;
        importer.compressionQuality=70;
        var android=importer.GetPlatformTextureSettings("Android");
        android.name="Android";android.overridden=true;android.maxTextureSize=512;
        android.format=TextureImporterFormat.ETC2_RGBA8;android.compressionQuality=70;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
    }
}
