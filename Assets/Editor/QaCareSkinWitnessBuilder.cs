#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Source indices only. No collider/mesh importer, catalog or scene is saved.
public static class QaCareSkinWitnessBuilder
{
    [Serializable] public sealed class Report
    {
        public string breed,sourceAssetGuid,sourceDependencyHash,skinMeshName;
        public int vertexCount;
        public int[] bodyIndices,neckIndices;
    }
    public static string MeasureTo(string outputPath)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        string path=Path.GetFullPath(outputPath);
        string root=Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Docs","QA"))+Path.DirectorySeparatorChar;
        if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("QA output only");
        var catalog=CatBreedCatalog.Load();var breed=catalog.Find("persian");
        var region=typeof(CatBodyGuardBuilder).GetMethod("Region",BindingFlags.NonPublic|BindingFlags.Static,
            null,new[]{typeof(BoneWeight),typeof(Transform[])},null);
        var regions=(string[])typeof(CatBodyGuardBuilder).GetField("Regions",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        if(region==null||!regions.SequenceEqual(new[]{"pelvis","chest","neck","head"}))throw new InvalidOperationException("Original four source regions required");
        var scene=EditorSceneManager.NewPreviewScene();GameObject visual=null;
        try
        {
            visual=CatBreedVisualFactory.Create(breed,catalog.GameplayController,null,"QA Persian source indices");
            SceneManager.MoveGameObjectToScene(visual,scene);
            var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
            // Same Editor source read used by the already measured 10 x 96 builder.
            var weights=mesh.boneWeights;var bones=skin.bones;
            var body=breed.ContactVertexIndices.Distinct().ToArray();
            if(body.Any(i=>i<0||i>=weights.Length))throw new InvalidOperationException("Source contact mask mismatch");
            var neck=body.Where(i=>(int)region.Invoke(null,new object[]{weights[i],bones})==2).ToArray();
            if(neck.Length==0)throw new InvalidOperationException("Source neck skin missing");
            string asset=AssetDatabase.GetAssetPath(breed.SourcePrefab);
            var report=new Report{breed=breed.Id,sourceAssetGuid=AssetDatabase.AssetPathToGUID(asset),
                sourceDependencyHash=AssetDatabase.GetAssetDependencyHash(asset).ToString(),skinMeshName=mesh.name,
                vertexCount=mesh.vertexCount,bodyIndices=body,neckIndices=neck};
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(report,true));
            return path+" body="+body.Length+" neck="+neck.Length;
        }
        finally{if(visual!=null)Object.DestroyImmediate(visual);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
