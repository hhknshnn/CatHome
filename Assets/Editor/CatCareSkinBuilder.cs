using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Copy immutable bind data only. No clip sampling, importer setting or source mutation.</summary>
public static class CatCareSkinBuilder
{
    public static CatCareSkinCatalog.Profile Capture(CatBreedCatalog.Entry breed,Animator animator,SkinnedMeshRenderer skin)
    {
        Mesh mesh=skin.sharedMesh;var weights=mesh.boneWeights;var positions=mesh.vertices;var bind=mesh.bindposes;
        var used=new Dictionary<int,int>();var paths=new List<string>();var starts=new List<int>();
        var bones=new List<int>();var points=new List<Vector3>();var amounts=new List<float>();
        int[] vertices=breed.ContactVertexIndices.ToArray();
        foreach(int vertex in vertices)
        {
            if(vertex<0||vertex>=positions.Length)throw new InvalidOperationException("Contact source index mismatch: "+breed.Id);
            starts.Add(bones.Count);var w=weights[vertex];
            int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] values={w.weight0,w.weight1,w.weight2,w.weight3};float sum=0;
            for(int i=0;i<4;i++)
            {
                if(values[i]<=0)continue;int bone=ids[i];sum+=values[i];
                if(!used.TryGetValue(bone,out int slot))
                {
                    string path=AnimationUtility.CalculateTransformPath(skin.bones[bone],animator.transform);
                    if(animator.transform.Find(path)!=skin.bones[bone])throw new InvalidOperationException("Unbound source bone: "+path);
                    slot=paths.Count;paths.Add(path);used.Add(bone,slot);
                }
                bones.Add(slot);points.Add(bind[bone].MultiplyPoint3x4(positions[vertex]));amounts.Add(values[i]);
            }
            if(Mathf.Abs(sum-1f)>.0001f)throw new InvalidOperationException("Invalid source weight sum: "+breed.Id+"/"+vertex);
        }
        starts.Add(bones.Count);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long localId);
        return new CatCareSkinCatalog.Profile{sourceKey=guid+":"+localId,sourceHash=AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(mesh)).ToString(),
            meshName=mesh.name,meshVertexCount=mesh.vertexCount,bonePaths=paths.ToArray(),vertexIndices=vertices,starts=starts.ToArray(),
            bones=bones.ToArray(),bindPoints=points.ToArray(),weights=amounts.ToArray()};
    }
    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        var breeds=CatBreedCatalog.Load();var profiles=new List<CatCareSkinCatalog.Profile>();var bindings=new List<CatCareSkinCatalog.Binding>();
        var shared=new Dictionary<Mesh,int>();var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var breed in breeds.Entries)
            {
                var sourceMesh=breed.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
                if(!shared.TryGetValue(sourceMesh,out int index))
                {
                    var visual=CatBreedVisualFactory.Create(breed,breeds.GameplayController,null);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual,scene);
                    try
                    {index=profiles.Count;profiles.Add(Capture(breed,visual.GetComponentInChildren<Animator>(),visual.GetComponentInChildren<SkinnedMeshRenderer>()));shared.Add(sourceMesh,index);}
                    finally{Object.DestroyImmediate(visual);}
                }
                else if(!profiles[index].vertexIndices.SequenceEqual(breed.ContactVertexIndices))throw new InvalidOperationException("Shared mesh has another contact mask.");
                bindings.Add(new CatCareSkinCatalog.Binding{breedId=breed.Id,profile=index});
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        var catalog=AssetDatabase.LoadAssetAtPath<CatCareSkinCatalog>(CatCareSkinCatalog.AssetPath);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<CatCareSkinCatalog>();AssetDatabase.CreateAsset(catalog,CatCareSkinCatalog.AssetPath);}
        catalog.EditorConfigure(profiles.ToArray(),bindings.ToArray());EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        return bindings.Count+" breeds / "+profiles.Count+" source geometries / "+profiles.Sum(p=>p.vertexIndices.Length)+" exact contact vertices; no model/clip references.";
    }
}
