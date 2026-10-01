using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AndroidPresentationTests
{
    [TestCase(1920,1200)] [TestCase(2340,1080)] [TestCase(1280,800)]
    public void MobileShowcaseBoundsPixelCostWithoutChangingAspect(int width,int height)
    {
        var size=MobilePresentation.ShowcaseSize(width,height);
        Assert.That(Mathf.Max(size.x,size.y),Is.LessThanOrEqualTo(1600));
        Assert.That(size.x,Is.LessThanOrEqualTo(width));
        Assert.That(size.y,Is.LessThanOrEqualTo(height));
        Assert.That(size.x/(float)size.y,Is.EqualTo(width/(float)height).Within(.003f));
    }
    [TestCase(-1,30)] [TestCase(0,30)] [TestCase(3072,30)] [TestCase(4096,30)]
    [TestCase(6144,30)] [TestCase(8192,30)] [TestCase(16384,30)]
    public void MobileFrameBudgetDoesNotTreatRamAsGpuCapability(int memory,int expected)
        => Assert.That(MobilePresentation.FrameRateForMemory(memory),Is.EqualTo(expected));

    [Test]
    public void TitleArtworkCoversOutsideTheSafeArea()
    {
        var title=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/TitleScreen.prefab");
        var hero=title.GetComponentInChildren<TitleCatShowcase>(true).transform as RectTransform;
        Assert.That(hero.parent,Is.EqualTo(title.transform));
        Assert.That(hero.anchorMin,Is.EqualTo(Vector2.zero));
        Assert.That(hero.anchorMax,Is.EqualTo(Vector2.one));
        Assert.That(title.transform.Find("SafeArea/BrandDockLayout"),Is.Not.Null);
    }

    [Test]
    public void RunnerDoesNotPackageTheRetiredPetShopTextures()
    {
        var deps=AssetDatabase.GetDependencies("Assets/Scenes/Runner/CatRunner.unity",true);
        Assert.That(deps.Where(p=>p.StartsWith("Assets/Bublisher/")),Is.Empty);
    }
}
