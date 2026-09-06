#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class TitleCatShowcaseTests
{
    private GameObject host;
    private TitleCatShowcase showcase;
    private string originalBreed;
    private bool originalReduced;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject Stage => (GameObject)typeof(TitleCatShowcase).GetField("stage", Private).GetValue(showcase);

    [SetUp]
    public void SetUp()
    {
        originalBreed = CatBreedService.SelectedBreedId;
        originalReduced = CatRunnerProgressService.ReducedMotion;
        CatRunnerProgressService.SetReducedMotion(false);
        host = new GameObject("Title lifecycle test", typeof(RectTransform), typeof(RawImage));
        host.SetActive(false);
        showcase = host.AddComponent<TitleCatShowcase>();
        showcase.EditorConfigure(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Title/LiveCats/TitleShowcaseStage.prefab"),
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Title/LiveCats/CatHome_LiveCats_HD.png"), CatBreedCatalog.Load());
        host.SetActive(true);
        showcase.SendMessage("OnApplicationFocus", true);
    }
    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
        CatBreedService.Select(originalBreed);
        CatRunnerProgressService.SetReducedMotion(originalReduced);
    }

    [UnityTest]
    public IEnumerator FirstFrameRendersRealCats_AndClosingReleasesAllRenderResources()
    {
        // Even an unfocused first launch must draw once instead of leaving a blank sky.
        showcase.SendMessage("OnApplicationFocus", false);
        yield return null; yield return null; yield return null;
        Assert.That(showcase.IsLive, Is.True);
        Assert.That(showcase.ActorCount, Is.EqualTo(3));
        Assert.That(showcase.HeroBreedId, Is.EqualTo(originalBreed));
        Assert.That(Stage.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        Assert.That(Stage.GetComponentInChildren<Camera>().enabled, Is.False);
        Assert.That(host.GetComponent<RawImage>().texture, Is.SameAs(showcase.Output));
        AssertNonBlank(showcase.Output);
        var oldStage = Stage; var oldTexture = showcase.Output;
        host.SetActive(false);
        yield return null;
        Assert.That(oldStage == null, Is.True, "Title close leaked its set.");
        Assert.That(oldTexture == null, Is.True, "Title close leaked its HD render texture.");
        host.SetActive(true);
        yield return null; yield return null;
        Assert.That(showcase.IsLive, Is.True);
        AssertNonBlank(showcase.Output);
    }

    [UnityTest]
    public IEnumerator EverySelectedBreedDrivesItsRealSkeletonOnTheTitle()
    {
        var catalog = CatBreedCatalog.Load();
        for (int breed = 0; breed < catalog.Count; breed++)
        {
            Assert.That(CatBreedService.Select(catalog.Get(breed).Id), Is.True);
            yield return null; yield return null;
            Assert.That(showcase.HeroBreedId, Is.EqualTo(catalog.Get(breed).Id));
            Assert.That(Stage.GetComponentsInChildren<Animator>().Length, Is.EqualTo(3));
            var hero = Stage.transform.Find("Showcase Cat " + catalog.Get(breed).Id);
            var bones = hero.GetComponentsInChildren<Transform>();
            var before = Snapshot(bones);
            yield return new WaitForSecondsRealtime(.65f);
            float moved = 0f;
            for (int i = 0; i < bones.Length; i++)
                if (bones[i].name.StartsWith("DEF-")) moved = Mathf.Max(moved, Quaternion.Angle(before[i], bones[i].localRotation));
            Assert.That(moved, Is.GreaterThan(.05f), catalog.Get(breed).Id + " is frozen in its bind pose.");
            AssertNonBlank(showcase.Output);
        }
    }

    [UnityTest]
    public IEnumerator TitleRenderingRestoresTheRoomsLightingExactly()
    {
        yield return null; yield return null;
        var lightObject = new GameObject("Room sun isolation probe");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 8f;
        var sun = RenderSettings.sun; var mode = RenderSettings.ambientMode;
        var sky = RenderSettings.ambientSkyColor; bool fog = RenderSettings.fog;
        try
        {
            RenderSettings.sun = light; RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.magenta; RenderSettings.fog = true;
            var probe = RenderSettings.ambientProbe;
            for (int frame = 0; frame < 20; frame++)
                typeof(TitleCatShowcase).GetMethod("RenderFrame", Private).Invoke(showcase, null);
            Assert.That(light.enabled, Is.True); Assert.That(light.cullingMask, Is.EqualTo(-1));
            Assert.That(RenderSettings.sun, Is.EqualTo(light));
            Assert.That(RenderSettings.ambientLight, Is.EqualTo(Color.magenta)); Assert.That(RenderSettings.fog, Is.True);
            for (int channel = 0; channel < 3; channel++)
                for (int coefficient = 0; coefficient < 9; coefficient++)
                    Assert.That(RenderSettings.ambientProbe[channel, coefficient], Is.EqualTo(probe[channel, coefficient]).Within(.0001f),
                        "Repeated title renders must not compound the room's ambient probe.");
            AssertNonBlank(showcase.Output);
        }
        finally
        {
            RenderSettings.sun = sun; RenderSettings.ambientMode = mode; RenderSettings.ambientSkyColor = sky;
            RenderSettings.fog = fog; Object.DestroyImmediate(lightObject);
        }
    }

    [UnityTest]
    public IEnumerator ReducedMotionAndFocusFreezeThePose_AndResumeAnimation()
    {
        yield return null; yield return null;
        CatRunnerProgressService.SetReducedMotion(true);
        yield return null; yield return null;
        var bones = Stage.GetComponentsInChildren<Transform>();
        var before = Snapshot(bones);
        yield return new WaitForSecondsRealtime(.25f);
        AssertSamePose(bones, before);
        // Selection must still refresh the static scene when reduced motion is on.
        string next = originalBreed == "maine-coon" ? "sphynx" : "maine-coon";
        CatBreedService.Select(next);
        yield return null; yield return null;
        Assert.That(showcase.HeroBreedId, Is.EqualTo(next));
        AssertNonBlank(showcase.Output);
        CatRunnerProgressService.SetReducedMotion(false);
        yield return new WaitForSecondsRealtime(.1f);
        showcase.SendMessage("OnApplicationFocus", false);
        bones = Stage.GetComponentsInChildren<Transform>(); before = Snapshot(bones);
        yield return new WaitForSecondsRealtime(.25f);
        AssertSamePose(bones, before);
        showcase.SendMessage("OnApplicationFocus", true);
        yield return new WaitForSecondsRealtime(.3f);
        bool moved = false;
        for (int i = 0; i < bones.Length; i++) moved |= Quaternion.Angle(before[i], bones[i].localRotation) > .05f;
        Assert.That(moved, Is.True);
    }
    private static Quaternion[] Snapshot(Transform[] bones)
    {
        var pose = new Quaternion[bones.Length];
        for (int i = 0; i < bones.Length; i++) pose[i] = bones[i].localRotation;
        return pose;
    }
    private static void AssertSamePose(Transform[] bones, Quaternion[] pose)
    {
        for (int i = 0; i < bones.Length; i++) Assert.That(Quaternion.Angle(pose[i], bones[i].localRotation), Is.LessThan(.05f), bones[i].name);
    }
    private static void AssertNonBlank(RenderTexture render)
    {
        var before = RenderTexture.active;
        var image = new Texture2D(render.width, render.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = render; image.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); image.Apply();
            var colors = new HashSet<Color32>();
            for (int y = 5; y < image.height; y += 41)
                for (int x = 5; x < image.width; x += 41) colors.Add(image.GetPixel(x, y));
            Assert.That(colors.Count, Is.GreaterThan(80), "The HD output is blank or a solid clear color.");
        }
        finally { RenderTexture.active = before; Object.DestroyImmediate(image); }
    }
}
#endif
