using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Minimal probe: load the Patio room in play mode and look at it, nothing else.
/// The porch swing ride test wedged the editor twice, and the two candidate
/// causes were the ride itself and simply play-loading this scene. This test
/// isolates the scene half so the two can be told apart.
/// </summary>
public sealed class PatioSceneProbeTests
{
    [TearDown]
    public void TearDown()
    {
        RoomPlayModeSupport.ReleaseRoom();
    }

    [UnityTest]
    public IEnumerator PatioScene_LoadsWithItsSwingAndCat()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Patio_Level01");

        Assert.That(Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include),
            Is.Not.Null, "The Patio scene must have its cat.");
        SwingRideActivity ride =
            Object.FindAnyObjectByType<SwingRideActivity>(FindObjectsInactive.Include);
        Assert.That(ride, Is.Not.Null, "The Patio scene must carry the porch swing ride.");
        Assert.That(ride.SwingPivot, Is.Not.Null, "The bench must hang under a pivot.");
        Assert.That(ride.StoreProductId, Is.EqualTo(HomeStoreService.PatioPorchSwingId));
        Assert.That(ride.IsRunning, Is.False);
    }
}
