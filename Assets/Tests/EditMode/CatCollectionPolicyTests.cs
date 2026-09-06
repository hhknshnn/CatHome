using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CatCollectionPolicyTests
{
    HomeStoreSaveState before;
    [SetUp] public void Setup(){before=HomeStoreService.CaptureState();}
    [TearDown] public void Cleanup(){HomeStoreService.ApplySavedState(before);}
    static string[] Cats => HomeStoreService.Products.Where(p=>CatCollectionPolicy.IsCatItem(p.Id)).Select(p=>p.Id).ToArray();
    void OwnAllStored(){var s=HomeStoreSaveState.CreateDefault();s.ownedProductIds=Cats;s.storedProductIds=Cats;HomeStoreService.ApplySavedState(s);}
    [Test] public void FiveDisplayed_RejectsSixth_StorageFreesASlotAndKeepsOwnership()
    {
        OwnAllStored();var toys=Cats.Where(id=>!CatCollectionPolicy.IsBed(id)).Take(6).ToArray();
        for(int i=0;i<5;i++)Assert.That(HomeStoreService.TrySetStored(toys[i],false),Is.True);
        Assert.That(HomeStoreService.TrySetStored(toys[5],false),Is.False);
        Assert.That(HomeStoreService.TrySetStored(toys[0],true),Is.True);
        Assert.That(HomeStoreService.TrySetStored(toys[5],false),Is.True);
        Assert.That(HomeStoreService.IsOwned(toys[0]),Is.True);
        Assert.That(CatCollectionPolicy.DisplayedCount,Is.EqualTo(5));
    }
    [Test] public void OneBedLimit_AppliesBeforeCapacityAndSurvivesReload()
    {
        OwnAllStored();Assert.That(HomeStoreService.TrySetStored(HomeStoreService.CloudBedId,false),Is.True);
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.CanopyBedId,false),Is.False);
        HomeStoreService.ApplySavedState(HomeStoreService.CaptureState());
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.CozyPodBedId,false),Is.False);
        HomeStoreService.TrySetStored(HomeStoreService.CloudBedId,true);
        Assert.That(HomeStoreService.TrySetStored(HomeStoreService.NapPillowId,false),Is.True);
    }
    [Test] public void OldSave_MigratesWithoutLosingOwnedProductsOrCoordinates()
    {
        var s=HomeStoreSaveState.CreateDefault();s.storeVersion=7;s.ownedProductIds=Cats;
        HomeStoreService.ApplySavedState(s);
        Assert.That(CatCollectionPolicy.DisplayedCount,Is.EqualTo(5));
        Assert.That(Cats.Count(id=>CatCollectionPolicy.IsBed(id)&&!HomeStoreService.IsStored(id)),Is.LessThanOrEqualTo(1));
        Assert.That(Cats.All(HomeStoreService.IsOwned),Is.True);
        var first=HomeStoreService.CaptureState();HomeStoreService.ApplySavedState(first);
        Assert.That(HomeStoreService.CaptureState().storedProductIds,Is.EqualTo(first.storedProductIds));
    }
    [Test] public void HiddenFurnitureAndTriggerToys_ReserveFootprints()
    {
        OwnAllStored();var toy=new GameObject("toy");var furniture=new GameObject("future furniture");var second=new GameObject("trigger toy");
        try
        {
            var a=toy.AddComponent<HomeProductPlacement>();a.EditorConfigure(HomeStoreService.FeatherToyId,toy.transform,new Transform[0],new Vector2(.4f,.4f));
            var room=furniture.AddComponent<HomeProductPlacement>();room.EditorConfigure(HomeStoreService.TvUnitId,furniture.transform,new Transform[0],new Vector2(1.8f,.6f));
            var body=GameObject.CreatePrimitive(PrimitiveType.Cube);body.transform.SetParent(furniture.transform,false);body.transform.localPosition=Vector3.up*.3f;body.transform.localScale=new Vector3(1.8f,.6f,.6f);furniture.SetActive(false);
            Assert.That(a.IsCurrentPositionValid(),Is.False,"Unpurchased inactive furniture reserves its footprint");
            furniture.transform.position=Vector3.right*50;
            var b=second.AddComponent<HomeProductPlacement>();b.EditorConfigure(HomeStoreService.LeashId,second.transform,new Transform[0],new Vector2(.8f,.6f));
            second.AddComponent<BoxCollider>().isTrigger=true;HomeStoreService.TrySetStored(HomeStoreService.LeashId,false);
            Assert.That(a.IsCurrentPositionValid(),Is.False,"Walkable trigger toys must not stack");
            HomeStoreService.TrySetStored(HomeStoreService.LeashId,true);
            Assert.That(a.IsCurrentPositionValid(),Is.True,"Stored toy releases its area");
        }
        finally{Object.DestroyImmediate(toy);Object.DestroyImmediate(furniture);Object.DestroyImmediate(second);}
    }
    [Test] public void RotatedFootprints_KeepAGutterWithoutUsingWorldAabbs()
    {
        Assert.That(HomeProductPlacement.FootprintsOverlap(Vector3.zero,new Vector2(1,.4f),90,new Vector3(.5f,0,0),new Vector2(.4f,.4f),0,.09f),Is.False);
        Assert.That(HomeProductPlacement.FootprintsOverlap(Vector3.zero,new Vector2(1,.4f),45,new Vector3(.3f,0,0),new Vector2(.4f,.4f),0,.09f),Is.True);
    }
    [Test] public void RestBeds_HaveZeroEntryCostAndToyPosesUseDifferentSkeletalClips()
    {
        foreach(var name in new[]{"CozyPodBed","CloudBed","CanopyBed","NapPillow"})
            Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(CatProductContentBuilder.Prefabs+name+".prefab").GetComponent<CatEnrichmentActivity>().EnergyCost,Is.Zero);
        foreach(var name in new[]{"Stalk","Pounce","Sniff","BatLeft","BatRight","Push","Tug"})
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(CatToyAnimationBuilder.Folder+"/Toy"+name+".anim");
            Assert.That(clip,Is.Not.Null);Assert.That(AnimationUtility.GetCurveBindings(clip).Length,Is.GreaterThan(50));
        }
    }
}
