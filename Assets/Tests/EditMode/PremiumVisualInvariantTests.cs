using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class PremiumVisualInvariantTests
{
    [Test]
    public void PremiumCurrencyArt_IsAuthoredAndImportable()
    {
        Assert.That(
            AssetDatabase.LoadAssetAtPath<Texture2D>(PremiumUiFactory.CoinIconPath),
            Is.Not.Null,
            "The shared glossy paw-coin UI render is missing.");
        Assert.That(
            AssetDatabase.LoadAssetAtPath<Texture2D>(PremiumUiFactory.DiamondIconPath),
            Is.Not.Null,
            "The shared faceted diamond UI render is missing.");
        GameObject coin = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/PremiumCurrency/PawCoin.fbx");
        GameObject diamond = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Art/PremiumCurrency/DiamondGem.fbx");
        Assert.That(coin, Is.Not.Null,
            "Cat Runner must keep using the authored 3D paw coin.");
        Assert.That(diamond, Is.Not.Null,
            "The premium diamond source model is missing.");
        Assert.That(coin.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(1),
            "The paw coin must remain a single-renderer runtime asset.");
        Assert.That(diamond.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(1),
            "The diamond must remain a single-renderer runtime asset.");
    }

    [Test]
    public void PremiumPalette_KeepsLightSurfacesAndReadableText()
    {
        Assert.That(Luminance(PremiumUiStyle.Ivory),Is.GreaterThan(.8f));
        Assert.That(Luminance(PremiumUiStyle.Mint),Is.GreaterThan(.7f));
        foreach(var surface in new[]{PremiumUiStyle.Ivory,PremiumUiStyle.Mint,PremiumUiStyle.Coral})
            Assert.That((Luminance(surface)+.05f)/(Luminance(PremiumUiStyle.Ink)+.05f),Is.GreaterThanOrEqualTo(4.5f));
        Assert.That(1.05f/(Luminance(PremiumUiStyle.Teal)+.05f),Is.GreaterThanOrEqualTo(3f));
    }
    private static float Luminance(Color color)
    {
        Color linear=color.linear;
        return .2126f*linear.r+.7152f*linear.g+.0722f*linear.b;
    }

    [Test]
    public void PremiumHomeFinish_HasAReusableVolumeProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(
            "Assets/Art/PremiumWorld/CatHomeRoom_PremiumVolume.asset");
        Assert.That(profile, Is.Not.Null,
            "Run the complete premium rebuild so the home colour-grade is preserved.");
        Assert.That(profile.components.Count, Is.GreaterThanOrEqualTo(4));
    }

    [Test]
    public void CatRunnerPremiumPresentation_AssetsRemainAvailable()
    {
        Assert.That(
            AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Runner/UI/CatRunnerHero_v1.png"),
            Is.Not.Null,
            "The Cat Runner welcome hero art is missing.");
        Assert.That(
            AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                "Assets/Art/Runner/CatRunner_VolumeProfile.asset"),
            Is.Not.Null,
            "The Cat Runner post-processing profile is missing.");
        Assert.That(
            AssetDatabase.LoadAssetAtPath<SceneAsset>(CatRunnerContentBuilder.RunnerScenePath),
            Is.Not.Null,
            "The premium Cat Runner scene must remain authored and importable.");
    }

    private static void AssertCandyColour(Color colour, string token)
    {
        Color.RGBToHSV(colour, out float hue, out float saturation, out float value);
        Assert.That(value, Is.GreaterThan(0.72f), token + " became too dark.");
        Assert.That(saturation, Is.GreaterThan(0.35f), token + " became too grey.");
        Assert.That(hue, Is.InRange(0f, 1f));
    }
}
