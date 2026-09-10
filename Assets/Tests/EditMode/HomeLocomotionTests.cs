using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HomeLocomotionTests
{
    [Test]
    public void EveryBreed_HasFreshMeasurementsOfBothRealClips()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CatHomeLocomotionCatalog>(CatHomeLocomotionBuilder.AssetPath);
        var breeds = CatBreedCatalog.Load();
        Assert.That(catalog, Is.Not.Null, "Run CatHomeLocomotionBuilder.Build after changing a breed or locomotion clip.");
        Assert.That(catalog.Entries.Count, Is.EqualTo(breeds.Count));
        Assert.That(catalog.Entries.Select(e => e.breedId).Distinct().Count(), Is.EqualTo(breeds.Count));
        foreach (var breed in breeds.Entries)
        {
            var profile = catalog.Find(breed.Id);
            Assert.That(profile, Is.Not.Null, breed.Id);
            var visual = CatBreedVisualFactory.Create(breed, breeds.GameplayController, null);
            try
            {
                var animator = visual.GetComponentInChildren<Animator>(); animator.enabled = false;
                foreach (bool run in new[] { false, true })
                {
                    var clip = CatHomeLocomotionBuilder.FindClip(animator, run ? "Run" : "Walk");
                    Assert.That(run ? profile.runClip : profile.walkClip, Is.EqualTo(clip));
                    // A different sample grid catches stale data and sensitivity
                    // to one lucky imported keyframe, without changing the rig.
                    var fresh = CatHomeLocomotionBuilder.Measure(animator, visual.transform, clip, 180);
                    float saved = run ? profile.runCycleDistance : profile.walkCycleDistance;
                    Assert.That(fresh.cycleDistance, Is.EqualTo(saved).Within(saved * .04f), breed.Id + " " + clip.name);
                    Assert.That(fresh.contactSamples, Is.GreaterThan(12));
                }
            }
            finally { Object.DestroyImmediate(visual); }
        }
    }

    [Test]
    public void RealBlendTree_CyclesCoverTheMeasuredDistance_AtSlowWalkBlendAndRun()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CatHomeLocomotionCatalog>(CatHomeLocomotionBuilder.AssetPath);
        var breeds = CatBreedCatalog.Load();
        Assert.That(catalog, Is.Not.Null);
        foreach (var breed in breeds.Entries)
        {
            var owner = new GameObject("Home cycle measurement"); owner.transform.localScale = Vector3.one * .5f;
            var visual = CatBreedVisualFactory.Create(breed, breeds.GameplayController, owner.transform);
            try
            {
                var animator = visual.GetComponentInChildren<Animator>();
                var profile = catalog.Find(breed.Id);
                foreach (float speed in new[] { .2925f, .675f, .85f, 1.225f, 1.5f })
                {
                    float blend = CatHomeLocomotionCatalog.RunBlendForSpeed(speed);
                    animator.SetFloat("Speed", Mathf.Lerp(.35f, 1f, blend));
                    animator.SetFloat("LocomotionRate", profile.PlaybackRate(speed, blend, .5f));
                    animator.Play("Idle", 0, .13f); animator.Update(.001f);
                    float before = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    for (int frame = 0; frame < 240; frame++) animator.Update(1f / 120f);
                    float cycles = animator.GetCurrentAnimatorStateInfo(0).normalizedTime - before;
                    float covered = cycles * profile.CycleDistance(blend, .5f);
                    Assert.That(covered, Is.EqualTo(speed * 2f).Within(speed * 2f * .025f),
                        breed.Id + " " + speed + "m/s: Animator's actual phase must cover the same ground.");
                }
            }
            finally { Object.DestroyImmediate(owner); }
        }
    }

    [Test]
    public void ExistingSceneSpeed_CannotRestoreTheOldSprint_AndSlowTravelUsesWalk()
    {
        Assert.That(CatMovement.GroundSpeedForInput(1f, true, 2.2f), Is.EqualTo(1.5f));
        Assert.That(CatMovement.GroundSpeedForInput(.081f, false), Is.GreaterThanOrEqualTo(.65f));
        Assert.That(CatMovement.GroundSpeedForInput(.08f, true), Is.Zero);
        Assert.That(CatHomeLocomotionCatalog.RunBlendForSpeed(1.5f * .45f), Is.Zero);
        Assert.That(CatHomeLocomotionCatalog.RunBlendForSpeed(.85f * .65f), Is.Zero);
        Assert.That(CatHomeLocomotionCatalog.RunBlendForSpeed(1.5f), Is.EqualTo(1f));
    }
}
