using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Builds only an isolated title stage. The gameplay room and its materials are read-only.</summary>
public static class WelcomeGlossBuilder
{
    public static void ImportArtwork()
    {
        foreach(string file in new[]{"WelcomeGloss_Atlas.png","PawGold.png"})
        {
            string path="Assets/Resources/WelcomeGloss/"+file;
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var i=AssetImporter.GetAtPath(path) as TextureImporter;if(i==null)throw new Exception("Missing "+path);
            i.textureType=TextureImporterType.Default;i.alphaIsTransparency=true;i.mipmapEnabled=false;
            i.npotScale=TextureImporterNPOTScale.None;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.Uncompressed;
            i.wrapMode=TextureWrapMode.Clamp;i.filterMode=FilterMode.Bilinear;i.SaveAndReimport();
        }
    }
    public static string BuildTitleRoom()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Author from Edit Mode.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("LivingRoom_Level01");
        if(!scene.isLoaded)throw new Exception("Open the living room first.");
        var original=AssetDatabase.LoadAssetAtPath<GameObject>(TitleShowcaseContentBuilder.PrefabPath);
        var stage=Object.Instantiate(original);stage.name="WelcomeGlossTitleStage";
        try
        {
            // Preserve the existing title-only light/volume configuration, replace just its display set.
            foreach(Transform child in stage.transform.Cast<Transform>().ToArray())
                if(child.GetComponent<Camera>()==null&&child.GetComponent<Light>()==null&&child.GetComponent<Volume>()==null)Object.DestroyImmediate(child.gameObject);
            string[] roots={"01 Environment","02 Furniture","LivingPermanentDecor","LivingZoneAccents"};
            foreach(var go in scene.GetRootGameObjects())
            {
                if(roots.Contains(go.name))Copy(go.transform,stage.transform,true);
                if(go.name=="05 Presentation")foreach(Transform child in go.transform)
                    if(child.name=="PremiumWorldPresentation"||child.name=="WindowSystem")Copy(child,stage.transform,true);
            }
            var camera=stage.GetComponentInChildren<Camera>();
            // Match the gameplay room's elevated perspective without changing its actual camera.
            camera.transform.localPosition=new Vector3(0f,2.7f,-6.5f);
            camera.transform.localRotation=Quaternion.Euler(18f,0f,0f);
            camera.fieldOfView=36;
            foreach(var light in stage.GetComponentsInChildren<Light>())
            {if(light.name.Contains("key"))light.intensity=1.2f;light.enabled=false;}
            foreach(var t in stage.GetComponentsInChildren<Transform>(true))t.gameObject.layer=TitleCatShowcase.StageLayer;
            string target="Assets/Resources/WelcomeGloss/TitleRoom.prefab";
            PrefabUtility.SaveAsPrefabAsset(stage,target);
            return target;
        }
        finally{Object.DestroyImmediate(stage);}
    }
    static void Copy(Transform source,Transform parent,bool root=false)
    {
        if(source.name.Contains("Editor")||source.GetComponent<SkinnedMeshRenderer>()!=null)return;
        if(source.name=="CAT Products"||source.name=="LivingFurniturePlay"||source.name=="Bed5 V3"||source.name=="WaterBowl"||source.name=="FoodBowl"||source.name=="LivingPearlThreshold")return;
        var product=source.GetComponent<StoreProductDisplay>();
        if(product!=null&&product.ProductId.StartsWith("cat."))return;
        var r=source.GetComponent<MeshRenderer>();var f=source.GetComponent<MeshFilter>();
        var target=new GameObject(source.name).transform;target.SetParent(parent,false);
        if(root){target.position=source.position;target.rotation=source.rotation;target.localScale=source.lossyScale;}
        else{target.localPosition=source.localPosition;target.localRotation=source.localRotation;target.localScale=source.localScale;}
        if(source.name=="Floor")target.localScale=new Vector3(source.localScale.x,source.localScale.y,source.localScale.z*3f);
        if(r!=null&&f!=null&&r.enabled&&f.sharedMesh!=null)
        {
            target.gameObject.AddComponent<MeshFilter>().sharedMesh=f.sharedMesh;
            var copy=target.gameObject.AddComponent<MeshRenderer>();copy.sharedMaterials=r.sharedMaterials;
            copy.shadowCastingMode=r.shadowCastingMode;copy.receiveShadows=r.receiveShadows;
        }
        foreach(Transform child in source)Copy(child,target);
    }
}
