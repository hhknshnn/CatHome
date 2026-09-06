#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class UiPresentationRegressionTests
{
    private GameObject first,second;
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [UnityTest]
    public IEnumerator ConcurrentCatPreviews_DoNotPhotographEachOther()
    {
        var entry=CatBreedCatalog.Load().Get(0);
        first=new GameObject("Preview A",typeof(RectTransform),typeof(RawImage),typeof(CatBreedTurntablePreview));
        second=new GameObject("Preview B",typeof(RectTransform),typeof(RawImage),typeof(CatBreedTurntablePreview));
        var a=first.GetComponent<CatBreedTurntablePreview>();var b=second.GetComponent<CatBreedTurntablePreview>();
        a.Show(entry);b.Show(entry);yield return null;
        AssertOtherCatOutsideFrame(a,b);AssertOtherCatOutsideFrame(b,a);
        var stage=(GameObject)typeof(CatBreedTurntablePreview).GetField("stage",Private).GetValue(a);
        a.Cleanup();Assert.That(stage.activeSelf,Is.False,"Retired previews must stop rendering in the same frame.");
        yield return null;Assert.That(stage==null,Is.True);
    }
    static void AssertOtherCatOutsideFrame(CatBreedTurntablePreview viewer,CatBreedTurntablePreview other)
    {
        var camera=(Camera)typeof(CatBreedTurntablePreview).GetField("previewCamera",Private).GetValue(viewer);
        var model=(GameObject)typeof(CatBreedTurntablePreview).GetField("model",Private).GetValue(other);
        var point=camera.WorldToViewportPoint(model.transform.position+Vector3.up*.4f);
        bool inside=point.z>=camera.nearClipPlane&&point.z<=camera.farClipPlane&&point.x>=0&&point.x<=1&&point.y>=0&&point.y<=1;
        Assert.That(inside,Is.False,"Another screen's cat is visible in this preview camera.");
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if(first!=null)Object.Destroy(first);if(second!=null)Object.Destroy(second);yield return null;
    }
}
#endif
