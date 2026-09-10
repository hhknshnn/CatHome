using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Final town composition. Existing nine theme IDs and gameplay dimensions stay intact.</summary>
public static class RunnerBoulevardBuilder
{
    public static void Apply(Transform root)
    {
        int index=0;
        foreach(var scenery in root.GetComponentsInChildren<CatRunnerScenerySegment>(true))
        {
            var segment=scenery.transform;
            foreach(Transform t in segment)
            {
                if(t.name=="NearScenery")foreach(Transform child in t)child.gameObject.SetActive(false);
                if(t.name.StartsWith("LaneDash")||t.name.StartsWith("Gateway")||t.name.StartsWith("RibbonEar")||
                   t.name=="CatTrailRibbon"||t.name.Contains("RoadCurb")||t.name.StartsWith("Sidewalk"))t.gameObject.SetActive(false);
            }
            segment.Find("CandyBoulevard").gameObject.SetActive(false);
            segment.Find("RoadSurround").localScale=new Vector3(18,.24f,8);
            var street=MiniGameArtBuilder.Model("BoulevardStreet",segment,Vector3.zero);
            var near=segment.Find("NearScenery");
            // One high festival cord every third block, with a clear road underneath.
            if (index % 3 == 1)
                MiniGameArtBuilder.Model("RunnerFestivalSpan",near,new Vector3(0,.16f,2.2f));
            for(int side=-1;side<=1;side+=2)
            {
                MiniGameArtBuilder.Model("BoulevardFacade"+((index+(side+1)/2)%3),near,new Vector3(side*4.40f,.10f,-.55f),side*65);
                var rear=MiniGameArtBuilder.Model("BoulevardFacade"+((index+2)%3),near,new Vector3(side*7.3f,.08f,2.15f),side*38);
                rear.transform.localScale=Vector3.one*.9f;
                MiniGameArtBuilder.Model("BoulevardLamp",near,new Vector3(side*2.86f,.16f,-2.70f),-side*32);
            }
            for(int v=0;v<CatRunnerContentBuilder.SceneryVariantNames.Length;v++)
            {
                var variant=segment.Find(CatRunnerContentBuilder.SceneryVariantNames[v]);
                foreach(Transform old in variant)old.gameObject.SetActive(false);
                int side=v%2==0?-1:1;
                var landmark=MiniGameArtBuilder.Model("BoulevardLandmark"+v,variant,new Vector3(side*3.34f,.16f,2.40f),side*50);
                landmark.transform.localScale=Vector3.one*.66f;
            }
            index++;
        }
        var camera=root.GetComponentInChildren<Camera>();
        camera.transform.localPosition=new Vector3(0,1.50f,-3.10f);
        camera.transform.localRotation=Quaternion.LookRotation(new Vector3(0,.42f,3.5f)-camera.transform.localPosition);
        camera.fieldOfView=46;
        var so=new SerializedObject(camera.GetComponent<CatRunnerCameraRig>());
        so.FindProperty("baseFieldOfView").floatValue=46;so.FindProperty("maximumFieldOfView").floatValue=50;
        so.ApplyModifiedPropertiesWithoutUndo();
        var data=camera.GetComponent<UniversalAdditionalCameraData>();
        data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
        // The canonical paw medal remains upright; its silhouette is smaller than the cat.
        var coin=root.Find("ScrollingTrack/Templates/Coin_Template");
        foreach(Transform child in coin)
            if(child.name.Contains("Halo")||child.name.Contains("Sparkle"))child.gameObject.SetActive(false);
        foreach(var platform in root.Find("ScrollingTrack/Templates").GetComponentsInChildren<CatRunnerTrackObject>(true))
        {
            if(platform.Kind!=CatRunnerTrackObjectKind.Platform)continue;
            foreach(Transform old in platform.transform)old.gameObject.SetActive(false);
            MiniGameArtBuilder.Model("RunnerBoardwalk",platform.transform,Vector3.zero);
        }
        var warning=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Runner/Materials/RunnerHazardWarning.mat");
        if(warning!=null){warning.color=new Color32(197,99,76,255);warning.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(warning);}
        // Serialize the actual final geometry after all scales and new FBXs are applied.
        foreach(var item in root.Find("ScrollingTrack/Templates").GetComponentsInChildren<CatRunnerTrackObject>(true))
        {
            Bounds bounds=new Bounds();bool found=false;
            foreach(var filter in item.GetComponentsInChildren<MeshFilter>(true))
            {
                var r=filter.GetComponent<Renderer>();if(r==null||!r.enabled||filter.sharedMesh==null)continue;
                bool visible=true;
                for(var t=filter.transform;t!=item.transform&&t!=null;t=t.parent)
                    if(!t.gameObject.activeSelf||t.name.Contains("Halo")||t.name.Contains("Sparkle")||t.name.Contains("Telegraph")||t.name.Contains("Warning"))visible=false;
                if(!visible)continue;
                var b=filter.sharedMesh.bounds;
                for(int i=0;i<8;i++)
                {
                    var corner=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var p=item.transform.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
                }
            }
            if(found)item.EditorSetVisualBounds(bounds);
        }
        var theme=root.GetComponentInChildren<CatRunnerThemeController>();
        if(theme!=null)
        {
            var settings=new SerializedObject(theme);
            SetPalette(settings,"skyColors",new Color32(141,189,195,255),new Color32(190,162,157,255),new Color32(121,160,190,255));
            SetPalette(settings,"fogColors",new Color32(207,222,206,255),new Color32(234,210,185,255),new Color32(188,208,221,255));
            SetPalette(settings,"ambientColors",new Color32(223,229,214,255),new Color32(232,215,204,255),new Color32(207,220,237,255));
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            if(ps.name.Contains("Speed"))ps.gameObject.SetActive(false);
        RunnerRetiredArtBuilder.Clean(root);
        AssetDatabase.SaveAssets();
    }
    static void SetPalette(SerializedObject so,string name,params Color[] colors)
    {
        var p=so.FindProperty(name);p.arraySize=colors.Length;
        for(int i=0;i<colors.Length;i++)p.GetArrayElementAtIndex(i).colorValue=colors[i];
    }
}
