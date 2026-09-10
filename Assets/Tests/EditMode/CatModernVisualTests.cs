using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class CatModernVisualTests
{
    [Test]
    public void Portraits_SeparateIdentityCropFromBreedCardsAndKeepMobileImports()
    {
        var report = CatModernPortraitBuilder.Validate();
        Assert.That(report.valid, Is.True, JsonUtility.ToJson(report, true));
        Assert.That(report.images, Is.EqualTo(20));
        var finish = CatModernVisualCatalog.Load();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatBreedShopPanelBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);
        foreach (var pair in finish.Portraits)
        {
            Assert.That(pair.identity, Is.Not.SameAs(pair.fullBody), pair.breedId);
            var card = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "BreedCard_" + pair.breedId);
            Assert.That(card, Is.Not.Null, pair.breedId);
            var portrait = card.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(i => i.name == "RealCatPortrait");
            Assert.That(portrait, Is.Not.Null, pair.breedId);
            Assert.That(portrait.sprite, Is.SameAs(pair.fullBody), pair.breedId + " card must show its actual whole cat.");
        }
    }

    [Test]
    public void ModernAssets_PreserveGeometrySkinningBreedColorsAndFacialSurfaces()
    {
        var report = CatModernVisualBuilder.Validate();
        Assert.That(report.valid, Is.True, JsonUtility.ToJson(report, true));
        Assert.That(report.breeds, Is.EqualTo(10));
        Assert.That(report.softenedVertices, Is.GreaterThan(0), "The finish must actually soften faceted body normals.");
        Assert.That(report.protectedFaceVertices, Is.GreaterThan(0), "The eye/mouth surfaces need explicit protection.");
        foreach (var mesh in report.meshes)
            Assert.That(mesh.headSoftenedVertices, Is.GreaterThan(0), "Head surfaces must also soften: " + mesh.source);
    }

    [Test]
    public void PortraitPose_UsesEachBreedsAuthoredNeutralFaceAndRetainsActualSittingBody()
    {
        var catalog = CatBreedCatalog.Load();
        var sitting = catalog.GameplayController.animationClips.First(c => c.name.EndsWith("|Sitting", StringComparison.Ordinal));
        foreach (var breed in catalog.Entries)
        {
            var cat = CatBreedVisualFactory.Create(breed, catalog.GameplayController, null);
            try
            {
                var animator = cat.GetComponentInChildren<Animator>(true); animator.enabled = false;
                float time = CatModernPortraitBuilder.SamplePortraitPose(animator, breed, sitting);
                var bones = animator.GetComponentsInChildren<Transform>(true);
                var photoPositions = bones.Select(b => b.localPosition).ToArray();
                var photoRotations = bones.Select(b => b.localRotation).ToArray();
                var photoScales = bones.Select(b => b.localScale).ToArray();
                var sourceBones = breed.SourcePrefab.GetComponentsInChildren<Transform>(true);
                int facialBones = 0;
                sitting.SampleAnimation(animator.gameObject, time);
                for (int i = 0; i < bones.Length; i++)
                {
                    bool facial = bones[i].name == "DEF-jaw" || bones[i].name.StartsWith("DEF-eyelids", StringComparison.Ordinal);
                    Transform expected = facial ? sourceBones.First(b => b.name == bones[i].name) : bones[i];
                    string label = breed.Id + "/" + bones[i].name;
                    Assert.That((photoPositions[i] - expected.localPosition).sqrMagnitude, Is.LessThan(1e-12f), label + " position");
                    Assert.That(Quaternion.Angle(photoRotations[i], expected.localRotation), Is.LessThan(.02f), label + " rotation");
                    Assert.That((photoScales[i] - expected.localScale).sqrMagnitude, Is.LessThan(1e-12f), label + " scale");
                    if (facial) facialBones++;
                }
                Assert.That(facialBones, Is.EqualTo(3), breed.Id);
            }
            finally { Object.DestroyImmediate(cat); }
        }
    }

    [Test]
    public void EveryBreed_PosedContactsAreIdenticalAndApplyingTwiceCreatesNoCopies()
    {
        var breeds = CatBreedCatalog.Load();
        var finish = AssetDatabase.LoadAssetAtPath<CatModernVisualCatalog>(CatModernVisualBuilder.CatalogPath);
        Assert.That(breeds, Is.Not.Null);
        Assert.That(finish, Is.Not.Null, "Build Modern Surface Finish before running authored asset tests.");
        var clips = AssetDatabase.LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath)
            .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__", StringComparison.Ordinal) &&
                (c.name.EndsWith("|Walk", StringComparison.Ordinal) || c.name.EndsWith("|Sleeping", StringComparison.Ordinal))).ToArray();
        Assert.That(clips.Length, Is.EqualTo(2));
        var sourceBake = new Mesh();
        var modernBake = new Mesh();
        try
        {
            foreach (var breed in breeds.Entries)
            {
                var originals = breed.SourcePrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var originalMeshes = originals.Select(s => s.sharedMesh).ToArray();
                var originalMaterials = originals.Select(s => s.sharedMaterials).ToArray();
                var cat = CatBreedVisualFactory.Create(breed, breeds.GameplayController, null);
                try
                {
                    var animator = cat.GetComponentInChildren<Animator>(true);
                    animator.enabled = false;
                    var skins = cat.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    Assert.That(skins.Length, Is.EqualTo(originals.Length), breed.Id);
                    Assert.That(cat.GetComponents<CatModernVisual>().Length, Is.EqualTo(1), breed.Id);
                    Assert.That(CatModernVisual.Apply(cat), Is.False, "The factory already applied this catalog.");
                    Assert.That(cat.GetComponents<CatModernVisual>().Length, Is.EqualTo(1), breed.Id);
                    foreach (var skin in skins)
                    {
                        Mesh modern = skin.sharedMesh;
                        var pair = finish.Meshes.FirstOrDefault(p => p.modern == modern);
                        Assert.That(pair, Is.Not.Null, breed.Id + " must use the shared finished mesh asset.");
                        Assert.That(EditorUtility.IsPersistent(modern), Is.True, "Do not clone skinned meshes per cat.");
                        var bones = skin.bones;
                        var rootBone = skin.rootBone;
                        var bounds = skin.localBounds;
                        foreach (var clip in clips)
                        foreach (float phase in new[] { 0f, .37f, .73f })
                        {
                            clip.SampleAnimation(animator.gameObject, phase * clip.length);
                            try
                            {
                                skin.sharedMesh = pair.source;
                                skin.BakeMesh(sourceBake, true);
                                skin.sharedMesh = modern;
                                skin.BakeMesh(modernBake, true);
                                var before = sourceBake.vertices;
                                var after = modernBake.vertices;
                                Assert.That(after.Length, Is.EqualTo(before.Length), breed.Id);
                                float maximumDelta = 0;
                                for (int i = 0; i < before.Length; i++)
                                    maximumDelta = Mathf.Max(maximumDelta, (before[i] - after[i]).sqrMagnitude);
                                Assert.That(maximumDelta, Is.LessThanOrEqualTo(1e-12f),
                                    breed.Id + " / " + clip.name + " / " + phase + ": shading changed a posed contact vertex.");
                            }
                            finally { skin.sharedMesh = modern; }
                        }
                        Assert.That(skin.bones, Is.EqualTo(bones), breed.Id);
                        Assert.That(skin.rootBone, Is.SameAs(rootBone), breed.Id);
                        Assert.That(skin.localBounds, Is.EqualTo(bounds), breed.Id);
                        foreach (var material in skin.sharedMaterials)
                            Assert.That(finish.Materials.Any(p => p.modern == material), Is.True, breed.Id + " uses a copied runtime material.");
                    }
                    for (int i = 0; i < originals.Length; i++)
                    {
                        Assert.That(originals[i].sharedMesh, Is.SameAs(originalMeshes[i]), "Vendor prefab mesh changed: " + breed.Id);
                        Assert.That(originals[i].sharedMaterials, Is.EqualTo(originalMaterials[i]), "Vendor materials changed: " + breed.Id);
                    }
                }
                finally { Object.DestroyImmediate(cat); }
            }
        }
        finally { Object.DestroyImmediate(sourceBake); Object.DestroyImmediate(modernBake); }
    }

    [Test]
    public void AuthoredDefaultVisual_UsesTheSameFinishWithoutFactoryRecreation()
    {
        var breeds = CatBreedCatalog.Load();
        var finish = AssetDatabase.LoadAssetAtPath<CatModernVisualCatalog>(CatModernVisualBuilder.CatalogPath);
        Assert.That(finish, Is.Not.Null);
        var cat = Object.Instantiate(breeds.Find(CatBreedCatalog.DefaultBreedId).SourcePrefab);
        try
        {
            var animator = cat.GetComponentInChildren<Animator>(true);
            var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var originalMesh = skin.sharedMesh;
            var originalBones = skin.bones;
            var transformCount = cat.GetComponentsInChildren<Transform>(true).Length;
            Assert.That(CatModernVisual.Apply(cat), Is.True);
            Assert.That(skin.sharedMesh, Is.SameAs(finish.Resolve(originalMesh)));
            Assert.That(cat.GetComponentInChildren<Animator>(true), Is.SameAs(animator));
            Assert.That(skin.bones, Is.EqualTo(originalBones));
            Assert.That(cat.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(transformCount));
            Assert.That(CatModernVisual.Apply(cat), Is.False);
        }
        finally { Object.DestroyImmediate(cat); }
    }
}
