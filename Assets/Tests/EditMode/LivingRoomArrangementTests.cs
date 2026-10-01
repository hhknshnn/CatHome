using System.Collections.Generic;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class LivingRoomArrangementTests
{
    [UnityTest,Timeout(360000)] public IEnumerator All3432AllowedFiveItemCollections_HaveConnectedAutomaticLayouts()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene=SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(HomeRoomService.LivingRoomScenePath,OpenSceneMode.Additive);
            var transforms=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var sofa=transforms.First(t=>t.name==HomeRoomGameplaySafetyBuilder.LivingSofaName);
            var lounge=transforms.Select(t=>t.GetComponent<LivingFurnitureActivity>()).First(a=>a!=null&&a.Kind==CatActivityKind.SofaLounge);
            foreach(float offset in new[]{-.20f,0f,.20f})
            {
                var probe=lounge.Perch.position+lounge.Perch.forward*offset;
                float cushion=LivingRoomArrangementBuilder.MeasureTop(sofa,probe,lounge.Perch.position.y);
                Assert.That(lounge.Perch.position.y,Is.InRange(cushion-.01f,cushion+.02f),"The whole sleeping body must sit above the cushion, not inside its centre seam.");
            }
            var layout=CatRoomArrangement.Request(scene);layout.Invalidate();
            var cats=HomeStoreService.Products.Where(p=>CatCollectionPolicy.IsCatItem(p.Id)).Select(p=>p.Id).ToArray();
            var toys=cats.Where(id=>!CatCollectionPolicy.IsBed(id)).ToArray();var beds=cats.Where(CatCollectionPolicy.IsBed).ToArray();
            int checkedCount=0;
            foreach(var ids in Combinations(toys,5)) {Progress(checkedCount,ids);Assert.That(layout.TryPlan(ids,out _),Is.True,string.Join(",",ids));checkedCount++;if(checkedCount%16==0)yield return null;}
            foreach(var bed in beds)foreach(var four in Combinations(toys,4))
            {var ids=new List<string>(four){bed};Progress(checkedCount,ids);Assert.That(layout.TryPlan(ids,out _),Is.True,string.Join(",",ids));checkedCount++;if(checkedCount%16==0)yield return null;}
            Assert.That(checkedCount,Is.EqualTo(3432));
        }
        finally
        {
            if(setup.Length>0)EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }
    }
    static void Progress(int count,IEnumerable<string> ids)
    {
        System.IO.Directory.CreateDirectory(UiQaTestSession.ResultDirectory);
        System.IO.File.WriteAllText(System.IO.Path.Combine(UiQaTestSession.ResultDirectory,"arrangement-progress.txt"),count+" "+string.Join(",",ids));
    }
    static IEnumerable<string[]> Combinations(string[] values,int count,int start=0)
    {
        if(count==0){yield return new string[0];yield break;}
        for(int i=start;i<=values.Length-count;i++)foreach(var tail in Combinations(values,count-1,i+1))
        {var result=new string[count];result[0]=values[i];System.Array.Copy(tail,0,result,1,tail.Length);yield return result;}
    }
    [Test] public void RetiredPillow_IsAbsentFromSceneAndActiveCatalog()
    {
        Assert.That(HomeStoreService.Products.Any(p=>p.Id==HomeStoreService.NapPillowId),Is.False);
        Assert.That(StoreCatalogAssets.PlaceableProducts.Any(p=>p.ProductId==HomeStoreService.NapPillowId),Is.False);
        var scene=SceneManager.GetSceneByPath(HomeRoomService.LivingRoomScenePath);
        bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(HomeRoomService.LivingRoomScenePath,OpenSceneMode.Additive);
        try {Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HomeProductPlacement>(true)).Any(p=>p.ProductId==HomeStoreService.NapPillowId),Is.False);}
        finally {if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    [Test] public void CompactArmchair_UsesMeasuredSeatAndLeavesCareSeparate()
    {
        StoreCatalogAssets.TryGet(HomeStoreService.ArmchairId,out var d);
        Assert.That(d.Footprint.x,Is.LessThanOrEqualTo(1.05f));
        var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/ClassicArmchair.prefab");
        var activity=root.GetComponent<PerchNapActivity>();
        var perch=new SerializedObject(activity).FindProperty("perchPoint").objectReferenceValue as Transform;
        Assert.That(perch,Is.Not.Null);Assert.That(perch.GetComponent<CatActivitySurface>(),Is.Not.Null);
        float y=LivingRoomArrangementBuilder.MeasureTop(root.transform,perch.position,perch.position.y);
        Assert.That(Mathf.Abs(perch.position.y-y),Is.LessThan(.015f));
        Assert.That(Vector3.Distance(d.DefaultPosition,new Vector3(.8f,0,2.25f)),Is.GreaterThan(.75f));
    }
    [Test] public void CatItems_CannotStartTheRetiredDragFlow()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/StoreProducts/Prefabs/PlayTunnel.prefab");
        var p=prefab.GetComponent<HomeProductPlacement>();Assert.That(p.SupportsRotation,Is.False);Assert.That(p.BeginPreview(),Is.False);
    }
}
