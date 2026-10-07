using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class CozyGamesReworkReview
{
    public const string Qa="Docs/QA/MINIGAMES_REWORK_2026-10-05";
    [Serializable] public class Routes {public Vector2[] pulls=new Vector2[CozyGameRules.LevelCount];public float[] times=new float[CozyGameRules.LevelCount];}
    public static string SolveRoutes()
    {
        var result=new Routes();var missing=new List<int>();
        for(int level=0;level<12;level++)
        {
            var obstacles=CozyGameRules.Cushions(level);float best=999;Vector2 pull=Vector2.zero;
            for(int a=0;a<360;a+=2)for(int force=65;force<=210;force+=3)
            {
                float angle=a*Mathf.Deg2Rad;var candidate=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(force*.01f);
                var p=CozyGameRules.Start(level);var v=CozyGameRules.Launch(candidate);
                for(int step=0;step<1100;step++)
                {
                    CozyGameRules.StepBall(ref p,ref v,obstacles,level,.012f);
                    if(CozyGameRules.IsInBasket(p,CozyGameRules.Goal(level),v.magnitude))
                    {float duration=step*.012f;if(duration<best){best=duration;pull=candidate;}break;}
                    if(v.sqrMagnitude<.0004f)break;
                }
            }
            if(best==999)missing.Add(level+1);
            result.pulls[level]=pull;result.pulls[level+12]=-pull;result.times[level]=result.times[level+12]=best;
        }
        Directory.CreateDirectory(Qa);File.WriteAllText(Qa+"/routes.json",JsonUtility.ToJson(result,true));return "Missing: "+string.Join(",",missing)+"; routes saved";
    }
    public static string FishPortraits()
    {
        var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var root=new GameObject("Fish portrait studio");SceneManager.MoveGameObjectToScene(root,scene);root.transform.position=new Vector3(300,1000,0);
        var camera=new GameObject("Portrait camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.transform.localPosition=new Vector3(.2f,.65f,-.8f);camera.transform.LookAt(root.transform.position);
        camera.orthographic=true;camera.orthographicSize=.45f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0,0,0,0);camera.nearClipPlane=.01f;camera.farClipPlane=5;
        var light=new GameObject("Portrait light").AddComponent<Light>();light.transform.SetParent(root.transform,false);light.type=LightType.Directional;light.transform.localRotation=Quaternion.Euler(40,-30,0);light.intensity=1.2f;
        var rt=new RenderTexture(256,192,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;var previous=RenderTexture.active;
        try
        {
            for(int i=0;i<6;i++)
            {
                var fish=CozyGamesBuilder.Model("Fish"+i,root.transform,Vector3.zero);fish.transform.localRotation=Quaternion.Euler(0,-40,0);
                camera.Render();RenderTexture.active=rt;var texture=new Texture2D(256,192,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,256,192),0,0);texture.Apply();
                string path="Assets/Resources/CozyGames/Fish"+i+"Preview.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);Object.DestroyImmediate(fish);AssetDatabase.ImportAsset(path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();
            }
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);}
        return "Six fish portraits saved.";
    }
}
