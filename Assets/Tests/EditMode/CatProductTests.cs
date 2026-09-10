using System.Collections.Generic;
using System.Linq;
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
            AssertCanonicalMaterial(material);
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

    [Test]
    public void ReapplyingModernFinish_PreservesMaterialIdentityPropertiesAndCanonicalSources()
    {
        var root=PrefabUtility.LoadPrefabContents(CatProductContentBuilder.Prefabs+"PlayTunnel.prefab");
        try
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(renderers.Length,Is.GreaterThan(0));
            var sources=renderers.SelectMany(r=>r.sharedMaterials)
                .Select(ModernWorldArtBuilder.ResolveSourceMaterial).Distinct().ToArray();
            foreach(var source in sources)
            {
                Assert.That(source,Is.Not.Null);
                Assert.That(AssetDatabase.GetAssetPath(source),Does.StartWith("Assets/Art/StoreProducts/Materials/"));
            }
            var sourceState=sources.ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));
            ModernWorldArtBuilder.ApplyRoot(root.transform,HomeRoomService.LivingRoomId);
            var references=renderers.ToDictionary(r=>r,r=>r.sharedMaterials);
            var materials=references.Values.SelectMany(m=>m).Distinct().ToArray();
            Assert.That(materials.Any(m=>m.shader.name==ModernWorldArtBuilder.ShaderName),Is.True);
            foreach(var material in materials)AssertCanonicalMaterial(material);
            var state=materials.ToDictionary(m=>m,m=>EditorJsonUtility.ToJson(m));
            var assets=AssetDatabase.FindAssets("t:Material",new[]{ModernWorldArtBuilder.AssetRoot+"/Materials"}).OrderBy(g=>g).ToArray();

            Assert.That(ModernWorldArtBuilder.ApplyRoot(root.transform,HomeRoomService.LivingRoomId),Is.Zero,
                "A second application must reuse the same renderer material references.");
            foreach(var pair in references)Assert.That(pair.Key.sharedMaterials,Is.EqualTo(pair.Value));
            foreach(var pair in state)Assert.That(EditorJsonUtility.ToJson(pair.Key),Is.EqualTo(pair.Value),pair.Key.name);
            foreach(var pair in sourceState)Assert.That(EditorJsonUtility.ToJson(pair.Key),Is.EqualTo(pair.Value),"Canonical source changed: "+pair.Key.name);
            Assert.That(AssetDatabase.FindAssets("t:Material",new[]{ModernWorldArtBuilder.AssetRoot+"/Materials"}).OrderBy(g=>g).ToArray(),Is.EqualTo(assets),
                "Repeated application must not create further derived material assets.");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static void AssertCanonicalMaterial(Material material)
    {
        const string canonicalFolder="Assets/Art/StoreProducts/Materials/";
        Assert.That(material,Is.Not.Null);
        string path=AssetDatabase.GetAssetPath(material);
        if(!path.StartsWith(ModernWorldArtBuilder.AssetRoot+"/Materials/",System.StringComparison.Ordinal))
        {
            // Deliberately excluded surfaces still use their canonical asset.
            Assert.That(path,Does.StartWith(canonicalFolder));
            Assert.That(material.shader,Is.Not.Null);
            Assert.That(material.shader.name,Is.Not.EqualTo(ModernWorldArtBuilder.ShaderName));
            return;
        }
        Assert.That(material.shader,Is.Not.Null);
        Assert.That(material.shader.name,Is.EqualTo(ModernWorldArtBuilder.ShaderName));
        Assert.That(material.shader.isSupported,Is.True,"The finished surface must render in the current pipeline.");
        string sourcePath=material.GetTag("ModernSourcePath",false,"");
        Assert.That(sourcePath,Does.StartWith(canonicalFolder),material.name);
        var source=AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        Assert.That(source,Is.Not.Null,"Missing canonical source: "+sourcePath);
        Assert.That(source.shader,Is.Not.Null);
        Assert.That(source.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
        Assert.That(material.GetTag("ModernSourceName",false,""),Is.EqualTo(source.name));
        Assert.That(ModernWorldArtBuilder.ResolveSourceMaterial(material),Is.SameAs(source));
        Assert.That(new[]{"Oak","Linen","Stone","Ceramic","Plaster","Leaf","Rattan","Suede"},Does.Contain(material.GetTag("ModernSurfaceFamily",false,"")));
        Assert.That(AssetDatabase.GetAssetPath(material.GetTexture("_DetailAlbedoMap")),Does.StartWith(ModernWorldArtBuilder.AssetRoot+"/Textures/"));
        Assert.That(material.GetTexture("_BaseMap"),Is.EqualTo(source.GetTexture("_BaseMap")),"Authored albedo must be retained.");
        Assert.That(material.GetTexture("_BumpMap"),Is.EqualTo(source.GetTexture("_BumpMap")),"Authored normal map must be retained.");
    }
}
