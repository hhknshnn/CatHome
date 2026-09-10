using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayfulRunnerBuilder
{
    const string Folder="Assets/Art/MiniGames/Playful";
    static readonly string[] Props={"PlayfulSkateboard","PlayfulToyTrain","PlayfulParcelStack","PlayfulDuck","PlayfulDonutStack","PlayfulFlowerCart"};
    public static string ApplyScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Finish Play first.");
        string actual=AssetDatabase.FindAssets("CatRunner t:Scene").Select(AssetDatabase.GUIDToAssetPath).First(p=>p.EndsWith("/CatRunner.unity"));
        var scene=SceneManager.GetSceneByPath(actual);bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(actual,OpenSceneMode.Additive);
        try
        {
            var track=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatRunnerTrackManager>(true)).Single();
            Apply(track.transform.root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();return "Six new obstacles and two bonus pickups saved in "+actual;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    public static void Apply(Transform root)
    {
        if(Application.isPlaying)throw new InvalidOperationException("Author outside Play.");
        var track=root.GetComponentInChildren<CatRunnerTrackManager>(true);
        if(track==null)throw new InvalidOperationException("Runner track missing.");
        var parent=track.transform.Find("Templates");
        if(parent==null)throw new InvalidOperationException("Runner templates missing.");
        if(!AssetDatabase.IsValidFolder(Folder+"/Materials"))AssetDatabase.CreateFolder(Folder,"Materials");
        string materialPath=Folder+"/Materials/PlayfulPalette.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
        material.color=Color.white;material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MiniGames/ArcadeWorlds/Textures/ArcadePalette.png");
        material.SetFloat("_Smoothness",.36f);material.enableInstancing=true;EditorUtility.SetDirty(material);
        var obstacles=Props.Select(n=>Template(parent,n,material,false,CatRunnerPowerUpKind.Magnet)).ToArray();
        var bonuses=new[]{Template(parent,"BonusScoreStar",material,true,CatRunnerPowerUpKind.ScoreStar),Template(parent,"BonusGift",material,true,CatRunnerPowerUpKind.MysteryGift)};
        var data=new SerializedObject(track);
        Append(data.FindProperty("obstacleTemplates"),obstacles);Append(data.FindProperty("powerUpTemplates"),bonuses);
        data.FindProperty("minimumPowerUpInterval").floatValue=12f;data.FindProperty("maximumPowerUpInterval").floatValue=18f;
        data.ApplyModifiedPropertiesWithoutUndo();
        // Re-measure the three existing power-ups too. Their original generic
        // box omitted parts of the shield, magnet and double-coin silhouettes.
        foreach(var pickup in root.GetComponentsInChildren<CatRunnerTrackObject>(true).Where(x=>x.IsPickup))
        {
            bool first=true;Bounds measured=default;
            foreach(var renderer in pickup.GetComponentsInChildren<MeshRenderer>(true))
            {
                bool visible=renderer.enabled;
                for(var parentNode=renderer.transform;parentNode!=null&&parentNode!=pickup.transform;parentNode=parentNode.parent)
                    visible&=parentNode.gameObject.activeSelf;
                if(!visible)continue;
                var b=renderer.localBounds;
                for(int corner=0;corner<8;corner++)
                {
                    var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                    var point=pickup.transform.InverseTransformPoint(renderer.transform.TransformPoint(b.center+Vector3.Scale(b.extents,sign)));
                    if(first){measured=new Bounds(point,Vector3.zero);first=false;}else measured.Encapsulate(point);
                }
            }
            if(!first)pickup.EditorSetVisualBounds(measured);
        }
    }
    static void Append(SerializedProperty array,GameObject[] added)
    {
        var all=new List<GameObject>();
        for(int i=0;i<array.arraySize;i++)
        {
            var item=array.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if(item!=null&&!item.name.StartsWith("Playful_")&&!added.Contains(item))all.Add(item);
        }
        all.AddRange(added);array.arraySize=all.Count;
        for(int i=0;i<all.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=all[i];
    }
    static GameObject Template(Transform parent,string name,Material material,bool pickup,CatRunnerPowerUpKind kind)
    {
        string path=Folder+"/Models/"+name+".fbx";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(source==null)throw new InvalidOperationException("Missing Blender model "+path);
        string templateName="Playful_"+name+"_Template";
        var old=parent.Find(templateName);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
        var root=new GameObject(templateName);root.transform.SetParent(parent,false);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source,root.transform);model.name="RunnerPlayfulVisual";
        model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.identity;
        model.transform.localScale=Vector3.one;
        var item=root.AddComponent<CatRunnerTrackObject>();
        bool first=true;Bounds bounds=default;
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
            var b=renderer.bounds;
            for(int i=0;i<8;i++)
            {
                var sign=new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);
                Vector3 point=root.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,sign));
                if(first){bounds=new Bounds(point,Vector3.zero);first=false;}else bounds.Encapsulate(point);
            }
        }
        if(bounds.size.x>1.1f||bounds.size.y>1f||bounds.size.x<.1f)throw new InvalidOperationException("Unexpected model units: "+name+" "+bounds);
        item.EditorConfigure(pickup?CatRunnerTrackObjectKind.PowerUp:CatRunnerTrackObjectKind.Obstacle,
            hazardBottom:bounds.min.y,hazardTop:bounds.max.y,pickupKind:kind);
        item.EditorSetVisualBounds(bounds);
        if(!pickup)CatRunnerContentBuilder.BuildHazardTelegraph(root,AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Runner/Materials/RunnerHazardWarning.mat"));
        root.SetActive(false);return root;
    }
}
