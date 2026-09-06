using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PremiumHdTests
{
    [TestCase(1280, 720)] [TestCase(1920, 1080)] [TestCase(2560, 1440)]
    [TestCase(2400, 1080)] [TestCase(1440, 1080)] [TestCase(3840, 2160)] [TestCase(5120, 2160)]
    public void ShowcaseRendersAtLeastFullHdWithoutStretching(int width, int height)
    {
        var output = TitleCatShowcase.HdSize(width, height);
        Assert.That(output.x, Is.GreaterThanOrEqualTo(1920));
        Assert.That(output.y, Is.GreaterThanOrEqualTo(1080));
        Assert.That(output.x / (float)output.y, Is.EqualTo(width / (float)height).Within(.002f));
        Assert.That(output.x, Is.LessThanOrEqualTo(3840));
        Assert.That(output.y, Is.LessThanOrEqualTo(2160));
    }

    [Test]
    public void ShowcaseSetCannotRunGameplayOrAddAnEnabledCamera()
    {
        var stage = AssetDatabase.LoadAssetAtPath<GameObject>(TitleShowcaseContentBuilder.PrefabPath);
        Assert.That(stage, Is.Not.Null);
        Assert.That(stage.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(stage.GetComponentsInChildren<AudioListener>(true), Is.Empty);
        Assert.That(stage.GetComponentsInChildren<CatActivity>(true), Is.Empty);
        Assert.That(stage.GetComponentsInChildren<StoreProductDisplay>(true), Is.Empty);
        Assert.That(stage.GetComponentsInChildren<CatMovement>(true), Is.Empty);
        var cameras = stage.GetComponentsInChildren<Camera>(true);
        Assert.That(cameras.Length, Is.EqualTo(1));
        Assert.That(cameras[0].enabled, Is.False);
        Assert.That(cameras[0].cullingMask, Is.EqualTo(1 << TitleCatShowcase.StageLayer));
        foreach (var light in stage.GetComponentsInChildren<Light>(true))
        {
            Assert.That(light.enabled, Is.False);
            Assert.That(light.cullingMask, Is.EqualTo(1 << TitleCatShowcase.StageLayer));
        }
    }

    [TestCase("Mobile", 2048, 2)] [TestCase("PC", 4096, 4)]
    public void QualityKeepsNativeResolutionAndSoftDetailedShadows(string name, int shadowSize, int cascades)
    {
        var asset = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/" + name + "_RPAsset.asset");
        var data = new SerializedObject(asset);
        Assert.That(data.FindProperty("m_RenderScale").floatValue, Is.EqualTo(1f));
        Assert.That(data.FindProperty("m_MSAA").intValue, Is.EqualTo(4));
        Assert.That(data.FindProperty("m_SoftShadowsSupported").boolValue, Is.True);
        Assert.That(data.FindProperty("m_MainLightShadowmapResolution").intValue, Is.EqualTo(shadowSize));
        Assert.That(data.FindProperty("m_ShadowCascadeCount").intValue, Is.EqualTo(cascades));
    }

    [Test]
    public void EveryRoomPreviewIsAnActualFullHdImage()
    {
        foreach (string room in new[] { "LivingRoom", "Bathroom", "Kitchen", "Bedroom", "Garden", "Balcony", "Patio", "SecondFloor" })
        {
            string path = RoomPreviewCaptureBuilder.PreviewFolder + "/" + room + "Preview.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, room);
            Assert.That(texture.width, Is.EqualTo(1920), room);
            Assert.That(texture.height, Is.EqualTo(1080), room);
        }
    }
}
