using NUnit.Framework;
using UnityEngine;

public sealed class SitLookSurfaceTargetTests
{
    GameObject root;

    [TearDown] public void After()
    {
        if (root != null) Object.DestroyImmediate(root);
    }

    SitLookActivity MakeProduct(Vector3 position)
    {
        root = new GameObject("Surface target isolated fixture");
        root.SetActive(false);
        var product = new GameObject("Observed product"); product.transform.SetParent(root.transform);
        product.transform.position = position;
        var activity = product.AddComponent<SitLookActivity>();
        var look = new GameObject("Legacy center target"); look.transform.SetParent(product.transform, false);
        activity.EditorConfigureLook(look.transform, SitLookReaction.Sit, 2f, "Done");
        return activity;
    }

    [Test] public void BakedSurfacePoints_FollowProductTransform_AndDoNotUseTheObsoleteCenter()
    {
        var activity = MakeProduct(new Vector3(100f, 0f, 100f));
        activity.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        activity.transform.localScale = new Vector3(2f, 1f, .5f);
        var points = new[] { new Vector3(.2f, .75f, -.3f) };
        Vector3 actualSurface = activity.transform.TransformPoint(points[0]);
        activity.EditorConfigureVisibleLookTargets(points);
        points[0] = Vector3.one * 50f; // The caller's scratch array is not runtime state.
        Physics.SyncTransforms();
        Assert.That(activity.TryGetVisibleLookPoint(null, actualSurface + Vector3.back, out Vector3 selected), Is.True);
        Assert.That(Vector3.Distance(actualSurface, selected), Is.LessThan(.0001f));
        Assert.That(Vector3.Distance(activity.LookPoint.position, selected), Is.GreaterThan(.5f));
    }

    [Test] public void SightIgnoresOnlyTheObservedProduct_AndSelectsAnotherRealSurfaceWhenOccluded()
    {
        var activity = MakeProduct(new Vector3(100f, 0f, 100f));
        activity.EditorConfigureVisibleLookTargets(new[] { new Vector3(0f, .8f, 0f), new Vector3(1f, .8f, 0f) });
        // Activity remains disabled: these pure geometry queries need no live
        // game state, ownership or save. Its visible child has a real collider.
        var ownBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ownBody.transform.SetParent(activity.transform, false);
        ownBody.transform.localPosition = new Vector3(0f, .8f, 0f);
        ownBody.transform.localScale = new Vector3(2.5f, 1f, .15f);
        activity.enabled = false; root.SetActive(true);
        Vector3 from = activity.transform.position + new Vector3(0f, .8f, -1f);
        Physics.SyncTransforms();
        Assert.That(activity.TryGetVisibleLookPoint(null, from, out Vector3 unobstructed), Is.True);
        Assert.That(Vector3.Distance(unobstructed, activity.transform.position + new Vector3(0f, .8f, 0f)), Is.LessThan(.0001f));

        var screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        screen.transform.SetParent(root.transform);
        screen.transform.position = activity.transform.position + new Vector3(0f, .8f, -.5f);
        screen.transform.localScale = new Vector3(.3f, 1f, .1f);
        Physics.SyncTransforms();
        Assert.That(activity.TryGetVisibleLookPoint(null, from, out Vector3 visibleSide), Is.True);
        Assert.That(Vector3.Distance(visibleSide, activity.transform.position + new Vector3(1f, .8f, 0f)), Is.LessThan(.0001f));
        screen.transform.localScale = new Vector3(3f, 1f, .1f);
        Physics.SyncTransforms();
        Assert.That(activity.TryGetVisibleLookPoint(null, from, out _), Is.False,
            "A different prop in the same room must still occlude every true surface target.");
    }
}
