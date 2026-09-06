using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PolyperfectCatIntegrationTests
{
    private static readonly string[] HomeScenePaths =
    {
        HomeRoomService.LivingRoomScenePath,
        HomeRoomService.BathroomScenePath,
        HomeRoomService.KitchenScenePath,
        HomeRoomService.BedroomScenePath,
        HomeRoomService.GardenScenePath,
        HomeRoomService.BalconyScenePath,
        HomeRoomService.PatioScenePath,
        HomeRoomService.SecondFloorScenePath
    };

    [Test]
    public void ImportedDomesticShorthair_ContainsTheCompleteAnimationSet()
    {
        AnimationClip[] clips = AssetDatabase
            .LoadAllAssetsAtPath(PolyperfectCatIntegrationBuilder.SourceModelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .ToArray();

        Assert.That(clips.Length, Is.EqualTo(23));
        string[] required =
        {
            "Idle", "Walk", "Run", "Attack", "Sleeping", "Sitting", "Eating",
            "Cleaning", "Itching", "Stretch", "Jump", "JumpUp", "JumpDown"
        };
        foreach (string suffix in required)
        {
            Assert.That(clips.Any(clip => clip.name.EndsWith("|" + suffix)), Is.True,
                "Missing Polyperfect animation: " + suffix);
        }
    }

    [Test]
    public void IntegratedPrefab_MatchesCatHomeAnimatorAndSizeContracts()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            PolyperfectCatIntegrationBuilder.PrefabPath);
        Assert.That(prefab, Is.Not.Null);

        GameObject instance = Object.Instantiate(prefab);
        try
        {
            Animator animator = instance.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            Assert.That(
                AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
                Is.EqualTo(PolyperfectCatIntegrationBuilder.ControllerPath));

            Assert.That(animator.parameters.Any(parameter =>
                    parameter.name == "Speed" &&
                    parameter.type == AnimatorControllerParameterType.Float),
                Is.True, "Cat Home requires a Float Speed parameter.");

            animator.Rebind();
            animator.Update(0f);
            foreach (string state in PolyperfectCatIntegrationBuilder.RequiredStates)
            {
                int hash = Animator.StringToHash("Base Layer." + state);
                Assert.That(animator.HasState(0, hash), Is.True,
                    "Missing Cat Home Animator state: " + state);
            }

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.EqualTo(1));
            Bounds bounds = renderers[0].bounds;
            Assert.That(bounds.size.y, Is.InRange(1.15f, 1.45f));
            Assert.That(bounds.size.z, Is.InRange(1.45f, 1.85f));
            Assert.That(renderers[0].sharedMaterial.shader.name,
                Does.StartWith("Universal Render Pipeline/"));
            Assert.That(FindNamed(instance.transform, "HeartSpawn"), Is.Not.Null);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    [Test]
    public void EveryHomeRoom_UsesTheIntegratedCatVisual()
    {
        foreach (string path in HomeScenePaths)
        {
            WithScene(path, scene =>
            {
                CatMovement cat = FindInScene<CatMovement>(scene);
                Assert.That(cat, Is.Not.Null, path + " has no CatMovement.");
                Animator animator = cat.GetComponentInChildren<Animator>(true);
                Assert.That(animator, Is.Not.Null, path + " has no cat Animator.");
                Assert.That(
                    AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
                    Is.EqualTo(PolyperfectCatIntegrationBuilder.ControllerPath),
                    path + " still uses an older cat.");
            });
        }
    }

    [Test]
    public void RunnerAndCatch_UseTheIntegratedCatVisual()
    {
        AssertMiniGameCat<CatRunnerPlayer>(
            CatRunnerLauncher.RunnerScenePath,
            "Cat Runner");
        AssertMiniGameCat<CatCatchPlayer>(
            CatCatchLauncher.CatchScenePath,
            "Cat Catch");
    }

    private static void AssertMiniGameCat<T>(string path, string label)
        where T : Component
    {
        WithScene(path, scene =>
        {
            T player = FindInScene<T>(scene);
            Assert.That(player, Is.Not.Null, label + " has no player component.");
            Animator animator = player.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, label + " has no cat Animator.");
            Assert.That(
                AssetDatabase.GetAssetPath(animator.runtimeAnimatorController),
                Is.EqualTo(PolyperfectCatIntegrationBuilder.ControllerPath),
                label + " still uses an older cat.");
            Assert.That(animator.applyRootMotion, Is.False);
        });
    }

    private static void WithScene(string path, System.Action<Scene> body)
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(path), Is.Not.Null,
            "Scene asset is missing: " + path);

        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened)
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try
        {
            body(scene);
        }
        finally
        {
            if (opened && scene.IsValid())
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Transform FindNamed(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name)
                return child;
        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }
}
