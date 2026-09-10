using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CatFeedingAlignmentBuilder
{
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var breeds=CatBreedCatalog.Load();
        var clip=breeds.GameplayController.animationClips.First(c=>c.name.EndsWith("|Eating",StringComparison.Ordinal));
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var entries=new List<CatFeedingAlignmentCatalog.Entry>();
        try{foreach(var breed in breeds.Entries)
        {
            var root=CatBreedVisualFactory.Create(breed,breeds.GameplayController,null);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);root.transform.localScale=Vector3.one*.5f;
            var animator=root.GetComponentInChildren<Animator>();animator.enabled=false;
            var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();
            try
            {
                Vector3 mouth=Vector3.zero;float support=0;
                for(int frame=0;frame<64;frame++)
                {
                    clip.SampleAnimation(animator.gameObject,clip.length*frame/64f);skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
                    mouth+=CatSipMouthCatalog.Load().Find(breed.Id).vertices.Select(v=>skin.transform.TransformPoint(vertices[v.vertexIndex])).OrderBy(v=>v.y).First();
                    var feet=new List<float>();
                    foreach(string name in CatHomeLocomotionBuilder.PawNames)
                    {
                        var paw=CatBreedVisualFactory.FindDescendant(root.transform,name);
                        feet.Add(vertices.Select(v=>skin.transform.TransformPoint(v)).Where(v=>v.y<paw.position.y&&new Vector2(v.x-paw.position.x,v.z-paw.position.z).sqrMagnitude<.0049f).Min(v=>v.y));
                    }
                    support+=(feet.Min()+feet.Max())*.5f;
                }
                entries.Add(new CatFeedingAlignmentCatalog.Entry{breedId=breed.Id,mouthOffset=mouth/64f,rootHeight=.01f-support/64f});
            }
            finally{Object.DestroyImmediate(mesh);Object.DestroyImmediate(root);}
        }}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
        const string path="Assets/Resources/CatFeedingAlignmentCatalog.asset";
        var asset=AssetDatabase.LoadAssetAtPath<CatFeedingAlignmentCatalog>(path);
        if(asset==null){asset=ScriptableObject.CreateInstance<CatFeedingAlignmentCatalog>();AssetDatabase.CreateAsset(asset,path);}
        asset.sourceClip=clip;asset.entries=entries.ToArray();EditorUtility.SetDirty(asset);AssetDatabase.SaveAssets();
    }
}
