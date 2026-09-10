using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The porch swing ride hops the cat onto a bench it cannot climb by itself and
/// pins it to the seat point under the rocking pivot. What is worth proving at
/// runtime is that the bench actually rocks, that the cat rides it rather than
/// hovering next to it, and that physics, scale and the movement lock come back.
/// </summary>
public sealed class SwingRideTests
{
    private GameObject energyObject;

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        RoomPlayModeSupport.ReleaseRoom();
        if (energyObject != null)
            Object.DestroyImmediate(energyObject);
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    [UnityTest]
    public IEnumerator Ride_RocksTheBenchWithTheCatAndPutsEverythingBack()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Patio_Level01");

        // After the room is up, not before: the ownership this fixture asserts on
        // has to be the state it set, and a room load used to bring the save file
        // back in underneath it.
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());

        SwingRideActivity ride =
            Object.FindAnyObjectByType<SwingRideActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(ride, Is.Not.Null, "The Patio scene must carry the porch swing ride.");
        Assert.That(cat, Is.Not.Null);
        Assert.That(ride.SwingPivot, Is.Not.Null);

        EnergySystem energy = Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
        if (energy == null)
        {
            // The energy HUD lives in CatHome_UI, which a single-scene load skips.
            energyObject = new GameObject("TestEnergySystem");
            energy = energyObject.AddComponent<EnergySystem>();
            yield return null;
        }

        Transform seat = ride.transform.Find("VisualContent/SwingPivot/SwingSeatPoint");
        Transform mount = ride.transform.Find("SwingMountPoint");
        Assert.That(seat, Is.Not.Null);
        Assert.That(mount, Is.Not.Null);

        Assert.That(ride.IsUnlocked, Is.False, "The ride waits for the store purchase.");
        Assert.That(ride.TryStart(cat), Is.False, "An unowned swing cannot be ridden.");

        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.PatioPorchSwingId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.PatioPorchSwingId).Succeeded, Is.True);
        ride.RefreshUnlockPresentation();
        Assert.That(ride.IsUnlocked, Is.True);

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        energy.ApplySavedValue(80f);
        long bondBefore = ProgressionService.BondXp;

        Assert.That(ride.TryStart(cat), Is.True, "An unlocked swing must be rideable.");
        Assert.That(ride.IsRunning, Is.True);
        Assert.That(controller == null || !controller.enabled,
            "Physics must be off while the cat is on the bench.");

        Time.timeScale = 6f;
        float maxRock = 0f;
        float closestToSeat = float.PositiveInfinity;
        float worstSeatOffsetWhileRocking = 0f;
        // A pendulum at a small angle travels mostly sideways, so measure how
        // far the cat moves from where the rocking first caught it rather than
        // just its height.
        bool anchored = false;
        Vector3 rideAnchor = Vector3.zero;
        float maxRideTravel = 0f;
        float deadline = Time.realtimeSinceStartup + 30f;
        while (ride.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(ride);
            float rock = Mathf.Abs(Mathf.DeltaAngle(0f, ride.SwingPivot.localEulerAngles.x));
            maxRock = Mathf.Max(maxRock, rock);
            float toSeat = Vector3.Distance(cat.transform.position, seat.position);
            closestToSeat = Mathf.Min(closestToSeat, toSeat);
            if (rock > 2f)
            {
                // While the bench is tilted the cat must be riding it, not
                // standing where the seat used to be.
                worstSeatOffsetWhileRocking = Mathf.Max(worstSeatOffsetWhileRocking, toSeat);
                if (!anchored)
                {
                    rideAnchor = cat.transform.position;
                    anchored = true;
                }
                maxRideTravel = Mathf.Max(
                    maxRideTravel, Vector3.Distance(cat.transform.position, rideAnchor));
            }
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(ride.IsRunning, Is.False, "The ride must finish on its own.");
        Assert.That(maxRock, Is.GreaterThan(3f), "The bench must actually rock.");
        Assert.That(maxRock, Is.LessThan(ride.SwingAngle + 1f), "The rock must stay bounded.");
        Assert.That(closestToSeat, Is.LessThan(0.2f), "The cat must reach the seat.");
        Assert.That(worstSeatOffsetWhileRocking, Is.LessThan(0.05f),
            "The cat must stay locked to the seat while the bench rocks.");
        Assert.That(maxRideTravel, Is.GreaterThan(0.03f),
            "The cat must be carried along as the bench swings, not sit still.");

        Assert.That(ride.SwingPivot.localRotation, Is.EqualTo(Quaternion.identity)
            .Using<Quaternion>((a, b) => Quaternion.Angle(a, b) < 0.01f ? 0 : 1),
            "The bench must be left level.");
        Assert.That(Vector3.Distance(cat.transform.position, mount.position), Is.LessThan(0.3f),
            "The cat must get off where it got on.");
        Assert.That(controller == null || controller.enabled, "Physics must be handed back.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The ride must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(ProgressionService.BondXp, Is.EqualTo(bondBefore + ride.BondReward),
            "Riding must pay its Bond reward.");
        Assert.That(energy.CurrentEnergy, Is.LessThan(80f), "Riding is play, so it costs energy.");
        Assert.That(CatActivity.Active, Is.Null);
    }
}
