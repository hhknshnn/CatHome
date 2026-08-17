using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class CatRunnerSceneryVariantTests
{
    [Test]
    public void BuildSegmentScenery_HasExpectedVariantCount()
    {
        GameObject segment = new GameObject("RunnerSceneryVariantTest");
        Shader standard = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        try
        {
            MethodInfo buildSegmentScenery = typeof(CatRunnerContentBuilder).GetMethod(
                "BuildSegmentScenery",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(buildSegmentScenery, Is.Not.Null);

            Material accent = new Material(standard);
            Material edge = new Material(standard);
            Material glow = new Material(standard);
            Material[] scenery = new[]
            {
                new Material(standard),
                new Material(standard),
                new Material(standard),
                new Material(standard),
                new Material(standard),
                new Material(standard),
                new Material(standard)
            };

            object[] parameters =
            {
                segment.transform,
                0,
                accent,
                edge,
                glow,
                scenery,
                null,
                null,
                null,
                null,
                null,
                null
            };
            buildSegmentScenery.Invoke(null, parameters);

            GameObject[] variants = parameters[6] as GameObject[];
            Assert.That(variants, Is.Not.Null);
            Assert.That(variants.Length, Is.EqualTo(CatRunnerContentBuilder.SceneryVariantCount));

            string[] expected = CatRunnerContentBuilder.SceneryVariantNames;

            for (int i = 0; i < expected.Length; i++)
                Assert.That(
                    Array.Exists(variants, v => v != null && string.Equals(v.name, expected[i])),
                    Is.True,
                    "Missing expected variant: " + expected[i]);

            string[] uniqueNames = new string[variants.Length];
            for (int i = 0; i < variants.Length; i++)
            {
                Assert.That(variants[i], Is.Not.Null);
                uniqueNames[i] = variants[i].name;
            }
            Assert.That(
                uniqueNames.Length,
                Is.EqualTo(new System.Collections.Generic.HashSet<string>(uniqueNames).Count));
        }
        finally
        {
            if (segment != null)
                UnityEngine.Object.DestroyImmediate(segment);
        }
    }

    [Test]
    public void GetSafeSceneryVariantIndex_DoesNotRepeatRecentVariants()
    {
        var random = new System.Random(1337);
        int last = -1;
        int previous = -1;

        for (int i = 0; i < 200; i++)
        {
            int selected = CatRunnerTrackManager.GetSafeSceneryVariantIndex(
                random, CatRunnerContentBuilder.SceneryVariantCount, last, previous);
            Assert.That(selected, Is.GreaterThanOrEqualTo(0));
            Assert.That(selected, Is.LessThan(CatRunnerContentBuilder.SceneryVariantCount));
            if (last >= 0)
                Assert.That(selected, Is.Not.EqualTo(last));
            if (previous >= 0)
                Assert.That(selected, Is.Not.EqualTo(previous));

            previous = last;
            last = selected;
        }
    }

    [Test]
    public void GetSafeSceneryVariantIndex_AvoidsLastTwoWhenThreeExist()
    {
        var random = new System.Random(42);
        int last = -1;
        int previous = -1;
        for (int i = 0; i < 120; i++)
        {
            int selected = CatRunnerTrackManager.GetSafeSceneryVariantIndex(
                random, 3, last, previous);
            Assert.That(selected, Is.InRange(0, 2));
            if (last >= 0)
                Assert.That(selected, Is.Not.EqualTo(last));
            if (previous >= 0)
                Assert.That(selected, Is.Not.EqualTo(previous));
            previous = last;
            last = selected;
        }
    }

    [Test]
    public void GetAvailableSceneryVariantCount_HidesGardenUntilRoomUnlock()
    {
        Assert.That(
            CatRunnerTrackManager.GetAvailableSceneryVariantCount(8, false),
            Is.EqualTo(8));
        Assert.That(
            CatRunnerTrackManager.GetAvailableSceneryVariantCount(9, false),
            Is.EqualTo(CatRunnerTrackManager.IndoorSceneryVariantCount));
        Assert.That(
            CatRunnerTrackManager.GetAvailableSceneryVariantCount(9, true),
            Is.EqualTo(9));

        var random = new System.Random(7);
        int last = -1;
        int previous = -1;
        for (int i = 0; i < 80; i++)
        {
            int selected = CatRunnerTrackManager.GetSafeSceneryVariantIndex(
                random,
                CatRunnerTrackManager.GetAvailableSceneryVariantCount(9, false),
                last,
                previous);
            Assert.That(selected, Is.InRange(0, 7));
            Assert.That(selected, Is.Not.EqualTo(CatRunnerTrackManager.GardenCourtyardVariantIndex));
            previous = last;
            last = selected;
        }
    }

    [Test]
    public void AuthoredRunnerScene_KeepsExclusiveSceneryVariantsIncludingGarden()
    {
        Scene scene = SceneManager.GetSceneByPath(CatRunnerContentBuilder.RunnerScenePath);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(
                CatRunnerContentBuilder.RunnerScenePath,
                OpenSceneMode.Additive);

        try
        {
            int authored = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                CatRunnerScenerySegment[] scenery =
                    roots[r].GetComponentsInChildren<CatRunnerScenerySegment>(true);
                for (int i = 0; i < scenery.Length; i++)
                {
                    CatRunnerScenerySegment segment = scenery[i];
                    authored++;
                    Assert.That(
                        segment.VariantCount,
                        Is.EqualTo(CatRunnerContentBuilder.SceneryVariantCount),
                        segment.name);
                    for (int v = 0; v < CatRunnerContentBuilder.SceneryVariantNames.Length; v++)
                    {
                        Assert.That(
                            segment.transform.Find(CatRunnerContentBuilder.SceneryVariantNames[v]),
                            Is.Not.Null,
                            segment.name + " missing " +
                            CatRunnerContentBuilder.SceneryVariantNames[v]);
                    }
                }
            }

            Assert.That(authored, Is.GreaterThanOrEqualTo(8));
        }
        finally
        {
            if (opened && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}

