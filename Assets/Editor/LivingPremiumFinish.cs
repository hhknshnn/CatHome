using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Small authored accents defining the reading and care bays.</summary>
public static class LivingPremiumFinish
{
    static Material Fabric(string name,Color color)
    {
        string path=LivingCompositionBuilder.Art+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(LivingCompositionBuilder.Material("LC_Cream")){name=name};AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
    }
    static void Rug(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var source=LivingCompositionBuilder.Find("Carpet_1");
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
        var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        go.transform.SetPositionAndRotation(position,Quaternion.identity);go.transform.localScale=new Vector3(scale.x*4f,scale.y,scale.z*4f);
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        CatHomeEditPreview.Clear();var room=LivingCompositionBuilder.Room;
        var old=LivingCompositionBuilder.Find("LivingZoneAccents");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("LivingZoneAccents");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,room);
        var sand=Fabric("LC_WovenSand",new Color(.72f,.58f,.40f));
        var cream=Fabric("LC_WovenOat",new Color(.90f,.80f,.63f));
        // Flat, non-colliding textiles identify the zones without adding obstacles.
        Rug(root.transform,"CareBayBorder",new Vector3(1.35f,.006f,2.08f),new Vector3(1.52f,.12f,.71f),sand);
        Rug(root.transform,"CareBayInset",new Vector3(1.35f,.009f,2.08f),new Vector3(1.47f,.12f,.65f),cream);
        Rug(root.transform,"ReadingBorder",new Vector3(-2.20f,.006f,1.63f),new Vector3(.85f,.12f,1.02f),sand);
        Rug(root.transform,"ReadingInset",new Vector3(-2.20f,.009f,1.63f),new Vector3(.80f,.12f,.96f),cream);
        var bed=LivingCompositionBuilder.Find("Bed5 V3");bed.position=LivingRoomReferenceLayout.BedPosition;
        var plant=LivingCompositionBuilder.Model("SmallCeramicPlant",root.transform);plant.name="CareBayPlant";
        plant.transform.position=new Vector3(1.58f,0,2.48f);plant.transform.localScale=Vector3.one*1.10f;
        var window=LivingCompositionBuilder.Find("WindowSystem");window.position=new Vector3(3.77f,1.17f,.70f);window.localScale=Vector3.one*.72f;
        var curtains=LivingCompositionBuilder.Find("ShortWindowCurtains");curtains.position=new Vector3(3.61f,1.66f,.73f);curtains.localScale=new Vector3(.59f,.72f,.72f);
        var sill=LivingCompositionBuilder.Find("WindowSillPlant");sill.position=new Vector3(3.61f,1.185f,.94f);sill.localScale=Vector3.one*.55f;
        foreach(var r in LivingCompositionBuilder.Find("Window").GetComponentsInChildren<Renderer>(true))
            r.sharedMaterials=r.sharedMaterials.Select(m=>m.name.Contains("WindowGlass")?LivingCompositionBuilder.Material("LC_Cream"):m).ToArray();
        // Close the original liner opening and provide an opaque inset in front of the wall.
        foreach(var spec in new[]{new {name="FormerWindowLiner",p=new Vector3(3.79f,1.85f,-.43f),s=new Vector3(.04f,1.26f,.90f)},new {name="DecorativeWindowInset",p=new Vector3(3.745f,1.66f,.73f),s=new Vector3(.006f,.82f,.56f)}}){
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=spec.name;g.transform.SetParent(root.transform,false);g.transform.position=spec.p;g.transform.localScale=spec.s;
            Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=spec.name=="FormerWindowLiner"?LivingCompositionBuilder.Find("RightWall").GetComponent<Renderer>().sharedMaterial:LivingCompositionBuilder.Material("LC_Cream");
        }
        var motif=LivingCompositionBuilder.Find("BackPaw_Right");if(motif!=null)motif.position=new Vector3(2.72f,2.02f,2.65f);
        SceneObservationFacingBuilder.Configure(room);
        Physics.SyncTransforms();
        var camera=room.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First();camera.rect=new Rect(0,.074074075f,1,.9259259f);
        EditorSceneManager.MarkSceneDirty(room);EditorSceneManager.SaveScene(room);AssetDatabase.SaveAssets();
        return "Reading and care bays, compact decorative window saved";
    }
}
