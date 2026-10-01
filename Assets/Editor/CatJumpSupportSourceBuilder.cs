using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Append complete preparation bone matrices to the installed jump catalog.
// No body refit, triangle recook, source edit, scene save, or live cat sampling.
public static class CatJumpSupportSourceBuilder
{
    sealed class Pending
    {
        public CatJumpClearanceCatalog.Entry entry;
        public CatCareSkinCatalog.Profile profile;
        public Matrix4x4[][] samples;
    }
    public static string Append()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Bake support source outside Play Mode.");
        var catalog=CatJumpClearanceCatalog.Load();var breeds=CatBreedCatalog.Load();var care=CatCareSkinCatalog.Load();
        if(catalog==null||breeds==null||care==null)throw new InvalidOperationException("Required installed catalogs are absent.");
        var pending=new List<Pending>();int frameCount=0,boneMatrices=0;float maximumError=0;
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            foreach(var breed in breeds.Entries)
            {
                var entry=catalog.Find(breed.Id);var profile=care.Find(breed.Id);
                if(entry?.sourceClip==null||entry.samples==null||profile?.bonePaths==null||profile.bonePaths.Length==0)
                    throw new InvalidOperationException("Missing exact source for "+breed.Id);
                var visual=CatBreedVisualFactory.Create(breed,breeds.GameplayController,null,"Supported jump source measurement");
                SceneManager.MoveGameObjectToScene(visual,scene);var baked=new Mesh();
                try
                {
                    var animator=visual.GetComponentInChildren<Animator>();animator.enabled=false;
                    var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();
                    if(!CatCareWeightedSkin.TryBind(profile,animator,skin,out _))throw new InvalidOperationException("Full source skin binding mismatch "+breed.Id);
                    var bones=profile.bonePaths.Select(path=>animator.transform.Find(path)).ToArray();
                    var transforms=visual.GetComponentsInChildren<Transform>(true);
                    var positions=transforms.Select(t=>t.localPosition).ToArray();
                    var rotations=transforms.Select(t=>t.localRotation).ToArray();
                    var scales=transforms.Select(t=>t.localScale).ToArray();
                    Vector3 basePosition=animator.transform.localPosition,baseScale=animator.transform.localScale;
                    Quaternion baseRotation=animator.transform.localRotation;
                    var item=new Pending{entry=entry,profile=profile,samples=new Matrix4x4[entry.samples.Length][]};
                    for(int i=0;i<entry.samples.Length;i++)
                    {
                        var frame=entry.samples[i];
                        if(frame==null||frame.phase>CatJumpMotion.Takeoff+.000001f&&frame.phase<CatJumpMotion.Touchdown-.000001f)
                        {item.samples[i]=Array.Empty<Matrix4x4>();continue;}
                        for(int t=0;t<transforms.Length;t++)
                        {transforms[t].localPosition=positions[t];transforms[t].localRotation=rotations[t];transforms[t].localScale=scales[t];}
                        entry.sourceClip.SampleAnimation(animator.gameObject,entry.sourceClip.length*frame.phase);
                        animator.transform.localPosition=basePosition;animator.transform.localRotation=baseRotation;animator.transform.localScale=baseScale;
                        skin.BakeMesh(baked,true);var vertices=baked.vertices;float minY=float.PositiveInfinity;
                        foreach(int index in breed.ContactVertexIndices)
                            minY=Mathf.Min(minY,visual.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[index])).y);
                        if(float.IsInfinity(minY))throw new InvalidOperationException("No source contact mask "+breed.Id);
                        Matrix4x4 toTag=Matrix4x4.Translate(-Vector3.up*minY)*visual.transform.worldToLocalMatrix;
                        var matrices=new Matrix4x4[bones.Length];
                        for(int b=0;b<bones.Length;b++)matrices[b]=toTag*bones[b].localToWorldMatrix;
                        // Compare every stored full-contact vertex independently to
                        // Unity's posed skin before any new catalog field is assigned.
                        for(int v=0;v<profile.vertexIndices.Length;v++)
                        {
                            Vector3 predicted=Vector3.zero;
                            for(int k=profile.starts[v];k<profile.starts[v+1];k++)
                                predicted+=matrices[profile.bones[k]].MultiplyPoint3x4(profile.bindPoints[k])*profile.weights[k];
                            Vector3 actual=toTag.MultiplyPoint3x4(skin.transform.TransformPoint(vertices[profile.vertexIndices[v]]));
                            float error=Vector3.Distance(predicted,actual);maximumError=Mathf.Max(maximumError,error);
                            if(error>.0001f)throw new InvalidOperationException("Weighted support source differs from real skin "+breed.Id+"/"+frame.phase+"/"+profile.vertexIndices[v]+" error="+error.ToString("R",CultureInfo.InvariantCulture));
                        }
                        item.samples[i]=matrices;frameCount++;boneMatrices+=matrices.Length;
                    }
                    pending.Add(item);
                }
                finally{Object.DestroyImmediate(baked);Object.DestroyImmediate(visual);}
            }
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
        // Commit only after every breed/frame has independently matched source.
        foreach(var item in pending)
        {
            item.entry.supportedPreparationVersion=CatJumpClearanceCatalog.SupportedPreparationVersion;
            item.entry.supportProfileKey=item.profile.sourceKey;item.entry.supportProfileHash=item.profile.sourceHash;
            item.entry.supportBonePaths=(string[])item.profile.bonePaths.Clone();
            for(int i=0;i<item.entry.samples.Length;i++)if(item.entry.samples[i]!=null)item.entry.samples[i].supportSkinMatrices=item.samples[i];
        }
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        return $"Supported preparation source: breeds={pending.Count},frames={frameCount},matrices={boneMatrices},maxWeightedError={maximumError.ToString("R",CultureInfo.InvariantCulture)}; existing body/source/scene data unchanged.";
    }
}
