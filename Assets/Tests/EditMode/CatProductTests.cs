using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class CatProductTests
{
    static IEnumerable<StoreCatalogAsset> Products()
    {
        foreach(var p in CatProductContentBuilder.LegacyDefinitions)yield return p;
        foreach(var p in StoreCatalogAssets.PlaceableProducts)
            if(HomeStoreService.TryGetProduct(p.ProductId,out var item)&&item.StoreCategory==HomeStoreCategory.Cat)yield return p;
    }
    [TestCaseSource(nameof(Products))]
    public void BuiltProduct_IsUprightInsideItsContract_WithCanonicalArtAndInteraction(StoreCatalogAsset definition)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CatProductContentBuilder.Prefabs+definition.PrefabName+".prefab");
        Assert.That(prefab,Is.Not.Null);
        var bounds=CatProductContentBuilder.LocalBounds(prefab,prefab.transform);
        Assert.That(bounds.min.y,Is.InRange(-.005f,.005f),definition.PrefabName+" must sit on the floor");
        Assert.That(bounds.size.x,Is.LessThanOrEqualTo(definition.Footprint.x+.005f));
        Assert.That(bounds.size.y,Is.LessThanOrEqualTo(definition.Height+.005f));
        Assert.That(bounds.size.z,Is.LessThanOrEqualTo(definition.Footprint.y+.005f));
        Assert.That(prefab.GetComponent<StoreProductDisplay>().ProductId,Is.EqualTo(definition.ProductId));
        Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>(definition.IconPath).width,Is.EqualTo(1024));
        foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        foreach(var material in renderer.sharedMaterials)
            Assert.That(AssetDatabase.GetAssetPath(material),Does.StartWith("Assets/Art/StoreProducts/Materials/"));
        if(definition.ProductId.StartsWith("home."))return; // The scene owns the legacy chase/scratch routines.
        var activity=prefab.GetComponent<CatEnrichmentActivity>();
        Assert.That(activity,Is.Not.Null);Assert.That(prefab.GetComponents<CatActivity>().Length,Is.EqualTo(1));
        Assert.That(activity.StoreProductId,Is.EqualTo(definition.ProductId));
        Assert.That(activity.ContactPoint,Is.Not.Null);Assert.That(activity.RoutineEntryPoint.localPosition.z,Is.LessThan(bounds.min.z-.12f));
        if(activity.Mode==CatEnrichmentMode.Tunnel)Assert.That(activity.ContactPoint.localPosition.y,Is.EqualTo(0).Within(.001f));
        if(activity.Mode==CatEnrichmentMode.Nap||activity.Mode==CatEnrichmentMode.Hide)
            Assert.That(activity.ContactPoint.localPosition.y,Is.InRange(.05f,.2f));
        if(activity.Mode!=CatEnrichmentMode.Nap&&definition.PrefabName!="CeramicBowl")Assert.That(activity.MovingPart,Is.Not.Null);
    }
    [Test] public void PillowAndPlayMat_DoNotCreateStepsForTheCat()
    {
        foreach(string name in new[]{"NapPillow","WalkingLeash"})
        foreach(var collider in AssetDatabase.LoadAssetAtPath<GameObject>(CatProductContentBuilder.Prefabs+name+".prefab").GetComponentsInChildren<Collider>(true))
            Assert.That(collider.isTrigger,Is.True,name);
    }
}
