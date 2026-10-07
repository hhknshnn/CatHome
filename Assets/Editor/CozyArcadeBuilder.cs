using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class CozyArcadeBuilder
{
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required");
        CozyGamesBuilder.EnsureMaterials();var active=SceneManager.GetActiveScene();
        foreach(bool runner in new[]{true,false})
        {
            var scene=EditorSceneManager.OpenScene(runner?CatRunnerLauncher.RunnerScenePath:CatCatchLauncher.CatchScenePath,OpenSceneMode.Additive);
            var root=scene.GetRootGameObjects().First(r=>r.GetComponent(runner?typeof(CatRunnerGameController):typeof(CatCatchGameController))!=null).transform;
            if(runner)Runner(root);else Catch(root);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);
        }
        SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();return "Both arcade worlds refreshed.";
    }
    static void Catch(Transform root)
    {
        var arena=root.Find("CatchArena");
        foreach(Transform t in arena)if(t.name!="Floor")t.gameObject.SetActive(false);
        var old=root.Find("CozyPlayroom");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var decor=new GameObject("CozyPlayroom").transform;decor.SetParent(root,false);
        CozyGamesBuilder.LivingBackdrop(decor);
        foreach(Transform item in decor)
        {
            var p=item.localPosition;
            if(item.name!="OakPlank")
            {
                if(p.z>2.6f)p.z+=.8f;
                else if(Mathf.Abs(p.x)>3.4f)p.x+=Mathf.Sign(p.x)*1.1f;
                item.localPosition=p;
            }
        }
        var floor=arena.Find("Floor").GetComponent<Renderer>();if(floor!=null)floor.sharedMaterial=CozyGamesBuilder.Mat("Oak");
        CozyGamesBuilder.Shape("HuntRugEdge",decor,new Vector3(0,-.004f,0),new Vector3(6.8f,.08f,4.8f),"Petrol");
        CozyGamesBuilder.Shape("HuntRug",decor,new Vector3(0,.038f,0),new Vector3(6.55f,.006f,4.56f),"Rug");
        for(int side=-1;side<=1;side+=2)
        {
            CozyGamesBuilder.Shape("WovenRugBand",decor,new Vector3(side*3.17f,.043f,0),new Vector3(.13f,.005f,4.4f),"Mint");
            for(int i=0;i<25;i++)CozyGamesBuilder.Shape("RugStitch",decor,new Vector3(side*3.25f,.044f,-2.1f+i*.17f),new Vector3(.09f,.007f,.022f),"Honey");
        }
        int index=0;foreach(var mouse in root.GetComponentsInChildren<CatCatchMouse>(true))
        {
            foreach(var renderer in mouse.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer is LineRenderer){renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CozyGames/Aim.mat");continue;}
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>Remap(m,index%3==0?"Coral":index%3==1?"Mint":"Honey")).ToArray();
            }
            index++;
        }
        var cam=root.GetComponentInChildren<Camera>();cam.transform.localPosition=new Vector3(0,7.5f,-7.2f);cam.transform.localRotation=Quaternion.LookRotation(new Vector3(0,.1f,.3f)-cam.transform.localPosition);cam.fieldOfView=43;cam.backgroundColor=new Color32(120,153,142,255);
    }
    static void Runner(Transform root)
    {
        int index=0;
        foreach(var segment in root.GetComponentsInChildren<CatRunnerScenerySegment>(true))
        {
            var near=segment.transform.Find("NearScenery");foreach(Transform child in near)child.gameObject.SetActive(false);
            var old=segment.transform.Find("CozyDistricts");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var districts=new GameObject("CozyDistricts").transform;districts.SetParent(segment.transform,false);
            var section=segment.GetComponent<CozyRouteSection>();if(section==null)section=segment.gameObject.AddComponent<CozyRouteSection>();section.districts=new GameObject[3];
            for(int stage=0;stage<3;stage++)
            {
                var region=new GameObject("District"+stage);region.transform.SetParent(districts,false);section.districts[stage]=region;
                for(int side=-1;side<=1;side+=2)
                {
                    if(stage==0)
                    {
                        CozyGamesBuilder.Shape("GardenWall",region.transform,new Vector3(side*3.6f,.5f,0),new Vector3(.22f,1,8),"Cream");
                        CozyGamesBuilder.Model("Plant",region.transform,new Vector3(side*3.5f,1.01f,(index%3-1)*2.2f),Vector3.one*.92f);
                        var house=MiniGameArtBuilder.Model("BoulevardFacade"+((index+side+3)%3),region.transform,new Vector3(side*6.4f,.07f,0),side*65);Recolor(house.transform);
                    }
                    else
                    {
                        var house=MiniGameArtBuilder.Model("BoulevardFacade"+((index+stage+(side+1)/2)%3),region.transform,new Vector3(side*4.4f,.07f,-.55f),side*65);Recolor(house.transform);
                        if(stage==1)
                        {
                            CozyGamesBuilder.Shape("MarketTable",region.transform,new Vector3(side*3.0f,.6f,1.8f),new Vector3(.8f,.16f,1.25f),"Oak");
                            for(int j=0;j<3;j++)CozyGamesBuilder.Model("Basket",region.transform,new Vector3(side*3.0f,.69f,1.4f+j*.43f),Vector3.one*.38f);
                        }
                        else {var lamp=MiniGameArtBuilder.Model("BoulevardLamp",region.transform,new Vector3(side*2.85f,.05f,2),-side*32);Recolor(lamp.transform);CozyGamesBuilder.Model("Plant",region.transform,new Vector3(side*3.2f,.05f,-2),Vector3.one*1.8f);}
                    }
                }
            }
            section.Select(0);
            foreach(string name in CatRunnerContentBuilder.SceneryVariantNames){var variant=segment.transform.Find(name);if(variant!=null)foreach(Transform child in variant)child.gameObject.SetActive(false);}
            Recolor(segment.transform);
            foreach(var street in segment.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="ArcadeStreet"))
                street.sharedMaterials=street.sharedMaterials.Select(m=>CozyGamesBuilder.Mat("Paving")).ToArray();
            var theme=segment.GetComponent<CatRunnerThemeSegment>();if(theme!=null)
            {
                var so=new SerializedObject(theme);foreach(var field in new[]{"floorThemes","detailThemes","laneThemes","edgeThemes","accentThemes"}){var p=so.FindProperty(field);for(int i=0;i<p.arraySize;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=CozyGamesBuilder.Mat(field=="floorThemes"?"Paving":field=="detailThemes"?"Cream":field=="laneThemes"?"Honey":field=="edgeThemes"?"Petrol":"Coral");}so.ApplyModifiedPropertiesWithoutUndo();
            }
            index++;
        }
        Recolor(root.Find("ScrollingTrack/Templates"));
        var cam=root.GetComponentInChildren<Camera>();var horizon=cam.transform.Find("SoftHorizon");if(horizon!=null)horizon.gameObject.SetActive(false);
        var cloud=root.Find("ArcadeCloudBank");if(cloud!=null)foreach(var r in cloud.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>CozyGamesBuilder.Mat("Pearl")).ToArray();
        var sky=root.GetComponentInChildren<CatRunnerThemeController>();if(sky!=null)
        {
            var so=new SerializedObject(sky);
            foreach(string field in new[]{"skyColors","fogColors","ambientColors","lightColors"})
            {
                var p=so.FindProperty(field);p.arraySize=3;for(int i=0;i<3;i++)p.GetArrayElementAtIndex(i).colorValue=field=="lightColors"?new Color(1,.94f,.85f):field=="ambientColors"?new Color(.79f,.86f,.79f):new Color(.68f,.79f,.80f);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    static void Recolor(Transform root)
    {
        if(root==null)return;foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))r.sharedMaterials=r.sharedMaterials.Select(m=>Remap(m)).ToArray();
    }
    static Material Remap(Material m,string mouse=null)
    {
        if(m==null)return CozyGamesBuilder.Mat("Cream");if(AssetDatabase.GetAssetPath(m).StartsWith(CozyGamesBuilder.Root+"/Materials/",StringComparison.Ordinal))return m;string n=m.name.ToLowerInvariant();
        if(n.Contains("eye")||n.Contains("ink"))return CozyGamesBuilder.Mat("Ink");
        if(n.Contains("white")||n.Contains("porcelain")||n.Contains("pearl"))return CozyGamesBuilder.Mat("Pearl");
        if(n.Contains("road")||n.Contains("pav")||n.Contains("sand")||n.Contains("cream")||n.Contains("stucco"))return CozyGamesBuilder.Mat("Cream");
        if(n.Contains("gold")||n.Contains("sun"))return CozyGamesBuilder.Mat("Honey");
        if(n.Contains("leaf")||n.Contains("grass")||n.Contains("green"))return CozyGamesBuilder.Mat("Sage");
        if(n.Contains("coral")||n.Contains("terra")||n.Contains("pink"))return CozyGamesBuilder.Mat("Coral");
        if(n.Contains("mint"))return CozyGamesBuilder.Mat("Mint");
        if(n.Contains("teal")||n.Contains("sky")||n.Contains("roof")||n.Contains("screen"))return CozyGamesBuilder.Mat("Petrol");
        if(n.Contains("wood")||n.Contains("oak"))return CozyGamesBuilder.Mat("Oak");
        return CozyGamesBuilder.Mat(mouse??"Sage");
    }
}
