using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Scoped final salon revision requested on 30 September. No player data writes.</summary>
public static class LivingFinalDetailsBuilder
{
    public const string Qa="Docs/QA/LIVING_FINISH_APK_2026-09-30";
    const string Art="Assets/Art/LivingComposition";
    static Transform Find(string name)=>LivingCompositionBuilder.Find(name);
    static void Move(Transform t,Vector3 p)
    {
        Undo.RecordObject(t,"Salon final placement");t.position=p;
        if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        EditorUtility.SetDirty(t);
    }
    static void Shift(string name,Vector3 delta){var t=Find(name);Move(t,t.position+delta);}
    public static string ApplyLayout()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        CatHomeEditPreview.Clear();
        foreach(var n in new[]{"ReadingBorder","ReadingInset","CareBayBorder","CareBayInset","CareBayPlant","NapPillow"})
        {var t=Find(n);if(t!=null)Undo.DestroyObjectImmediate(t.gameObject);}
        Move(Find("PairedCareStation"),LivingRoomReferenceLayout.CareStationPosition);
        var food=Find("FoodBowl");var water=Find("WaterBowl");
        var fp=LivingRoomReferenceLayout.CarePoint(new Vector3(-.35f,.018f,-.12f));
        var wp=LivingRoomReferenceLayout.CarePoint(new Vector3(.35f,.018f,-.12f));
        Shift("FoodInteractionPoint",fp-food.position);Shift("WaterInteractionPoint",wp-water.position);
        Move(food,fp);Move(water,wp);
        Move(Find("Bed5 V3"),LivingRoomReferenceLayout.BedPosition);
        Move(Find("TallHouseplant"),LivingProgressionComposition.PlantPosition);
        // Align the shelf's upper edge with the painting; keep floor entry anchors grounded.
        var shelf=Find("TallBookshelf");var painting=Find("ModernPainting");
        float top=painting.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Max(r=>r.bounds.max.y);
        float shelfTop=shelf.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Max(r=>r.bounds.max.y);
        float lift=top-shelfTop;
        Shift("TallBookshelf",Vector3.up*lift);Shift("ColorfulBookSet",Vector3.up*lift);
        foreach(var root in new[]{Find("TallBookshelf"),Find("ColorfulBookSet")})
        {var entry=root.Find("InteractionAnchor");if(entry!=null)Move(entry,new Vector3(entry.position.x,0,entry.position.z));}
        var curtain=Find("ShortWindowCurtains");Move(curtain,new Vector3(3.57f,1.61f,.644f));
        Undo.RecordObject(curtain,"Fit curtains to window");curtain.localScale=new Vector3(.71f,.90f,.72f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(curtain);
        BuildWindowLandscape();
        Physics.SyncTransforms();EditorSceneManager.MarkSceneDirty(LivingCompositionBuilder.Room);
        EditorSceneManager.SaveScene(LivingCompositionBuilder.Room);AssetDatabase.SaveAssets();
        return "Requested rugs/plant/pillow removed; symmetric bowls, bed, shelf, TV plant, curtains and panorama saved.";
    }
    public static void BuildWindowLandscape()
    {
        string texturePath=Art+"/Textures/WindowCountryside.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        var mf=Find("Window").GetComponentInChildren<MeshFilter>();var renderer=mf.GetComponent<Renderer>();
        var source=mf.sharedMesh;var mesh=Object.Instantiate(source);mesh.name="Window frame with continuous landscape UV";
        var vertices=mesh.vertices;var uv=mesh.uv;var ids=mesh.GetTriangles(1).Distinct().ToArray();
        float minX=ids.Min(i=>vertices[i].x),maxX=ids.Max(i=>vertices[i].x);
        float minY=ids.Min(i=>vertices[i].y),maxY=ids.Max(i=>vertices[i].y);
        foreach(int i in ids)uv[i]=new Vector2(Mathf.InverseLerp(minX,maxX,vertices[i].x),Mathf.InverseLerp(minY,maxY,vertices[i].y));
        mesh.uv=uv;
        string meshPath=Art+"/WindowLandscapeMesh.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(saved==null){AssetDatabase.CreateAsset(mesh,meshPath);saved=mesh;}
        else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}
        string matPath=Art+"/Materials/LC_WindowLandscape.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(material,matPath);}
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Cull",0);EditorUtility.SetDirty(material);
        mf.sharedMesh=saved;var mats=renderer.sharedMaterials;mats[1]=material;renderer.sharedMaterials=mats;
        PrefabUtility.RecordPrefabInstancePropertyModifications(mf);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        EditorUtility.SetDirty(mf);EditorUtility.SetDirty(renderer);
    }
}
