using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Bake at most 24 real mouth vertices per shared cat geometry.</summary>
public static class CatSipMouthBuilder
{
    public const string CatalogPath="Assets/Resources/CatSipMouthCatalog.asset";
    public static string Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Bake mouth profiles in Edit Mode.");
        var breeds=CatBreedCatalog.Load();
        var clip=breeds.GameplayController.animationClips.First(c=>c.name.EndsWith("|Eating",StringComparison.Ordinal));
        var profiles=new List<CatSipMouthCatalog.Profile>();var bindings=new List<CatSipMouthCatalog.BreedBinding>();
        var shared=new Dictionary<Mesh,int>();
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var breed in breeds.Entries)
            {
                var mesh=breed.SourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh;
                if(!shared.TryGetValue(mesh,out int profileIndex))
                {
                    var visual=CatBreedVisualFactory.Create(breed,breeds.GameplayController,null);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual,scene);visual.transform.localScale=Vector3.one*.5f;
                    try
                    {
                        var animator=visual.GetComponentInChildren<Animator>();animator.enabled=false;
                        clip.SampleAnimation(animator.gameObject,clip.length*.35f);
                        var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();
                        var bones=skin.bones;var jaw=CatBreedVisualFactory.FindDescendant(visual.transform,"DEF-jaw");
                        int jawIndex=Array.IndexOf(bones,jaw);var weights=mesh.boneWeights;var vertices=mesh.vertices;var bind=mesh.bindposes;
                        var candidates=new List<(CatSipMouthCatalog.Vertex,float)>();float foremost=float.NegativeInfinity;
                        for(int v=0;v<weights.Length;v++)
                        {
                            var w=weights[v];int[] indices={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};
                            float[] amounts={w.weight0,w.weight1,w.weight2,w.weight3};float jawWeight=0;
                            for(int i=0;i<4;i++)if(indices[i]==jawIndex)jawWeight+=amounts[i];
                            if(jawWeight<.25f)continue;
                            var influences=new List<CatSipMouthCatalog.Influence>();Vector3 world=Vector3.zero;
                            for(int i=0;i<4;i++)
                            {
                                if(amounts[i]<=0)continue;Vector3 point=bind[indices[i]].MultiplyPoint3x4(vertices[v]);
                                influences.Add(new CatSipMouthCatalog.Influence{bonePath=AnimationUtility.CalculateTransformPath(bones[indices[i]],animator.transform),bindPosition=point,weight=amounts[i]});
                                world+=bones[indices[i]].TransformPoint(point)*amounts[i];
                            }
                            float forward=Vector3.Dot(world-jaw.position,visual.transform.forward);foremost=Mathf.Max(foremost,forward);
                            candidates.Add((new CatSipMouthCatalog.Vertex{vertexIndex=v,influences=influences.ToArray()},forward));
                        }
                        candidates.RemoveAll(v=>v.Item2<foremost-.035f);
                        if(candidates.Count==0)throw new InvalidOperationException(breed.Id+": no actual front jaw surface vertices");
                        int count=Mathf.Min(24,candidates.Count);var selected=new CatSipMouthCatalog.Vertex[count];
                        for(int i=0;i<count;i++)selected[i]=candidates[i*candidates.Count/count].Item1;
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out string guid,out long localId);
                        profileIndex=profiles.Count;shared.Add(mesh,profileIndex);
                        profiles.Add(new CatSipMouthCatalog.Profile{sourceKey=guid+":"+localId,vertices=selected});
                    }
                    finally{Object.DestroyImmediate(visual);}
                }
                bindings.Add(new CatSipMouthCatalog.BreedBinding{breedId=breed.Id,profileIndex=profileIndex});
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        var catalog=AssetDatabase.LoadAssetAtPath<CatSipMouthCatalog>(CatalogPath);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<CatSipMouthCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
        catalog.EditorConfigure(profiles.ToArray(),bindings.ToArray());EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        return bindings.Count+" breeds share "+profiles.Count+" mouth profiles / "+profiles.Sum(p=>p.vertices.Length)+" vertices; no gameplay mesh CPU copy.";
    }
}
