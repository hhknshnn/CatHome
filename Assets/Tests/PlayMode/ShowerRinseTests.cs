using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The rainbow shower rinse walks the cat in with its CharacterController
/// switched off, so the runtime facts worth proving are that the cat reaches the
/// tray, is actually lifted onto it, comes back out of the product footprint,
/// and gets its physics, scale and movement lock handed back.
/// </summary>
public sealed class ShowerRinseTests
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
    public IEnumerator Rinse_StepsTheCatOntoTheTrayAndBackOutWithPhysicsRestored()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());

        ShowerRinseActivity rinse =
            Object.FindAnyObjectByType<ShowerRinseActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(rinse, Is.Not.Null, "The Bathroom scene must carry the shower rinse activity.");
        Assert.That(cat, Is.Not.Null);

        EnergySystem energy = Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
        if (energy == null)
        {
            // The energy HUD lives in CatHome_UI, which a single-scene load skips.
            energyObject = new GameObject("TestEnergySystem");
            energy = energyObject.AddComponent<EnergySystem>();
            yield return null;
        }

        Transform stand = rinse.transform.Find("RinseStandPoint");
        Transform door = rinse.transform.Find("RinseDoorPoint");
        Assert.That(stand, Is.Not.Null);
        Assert.That(door, Is.Not.Null);
        Assert.That(stand.position.y, Is.GreaterThan(door.position.y + 0.05f),
            "The stand point must sit on the tray, above the room floor.");

        Assert.That(rinse.IsUnlocked, Is.False, "The rinse waits for the store purchase.");
        Assert.That(rinse.TryStart(cat), Is.False, "An unowned shower cannot be used.");

        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BathroomShowerId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomShowerId).Succeeded, Is.True);
        rinse.RefreshUnlockPresentation();
        Assert.That(rinse.IsUnlocked, Is.True);

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        energy.ApplySavedValue(60f);

        Assert.That(rinse.TryStart(cat), Is.True, "An unlocked shower must be usable.");
        Assert.That(rinse.IsRunning, Is.True);
        Assert.That(controller == null || !controller.enabled,
            "Physics must be off while the cat is inside the cabin.");

        Time.timeScale = 8f;
        float closestToStand = float.PositiveInfinity;
        float highestCat = float.NegativeInfinity;
        float deadline = Time.realtimeSinceStartup + 25f;
        while (rinse.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            closestToStand = Mathf.Min(closestToStand, Flat(cat.transform.position, stand.position));
            highestCat = Mathf.Max(highestCat, cat.transform.position.y);
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(rinse.IsRunning, Is.False, "The rinse must finish on its own.");
        Assert.That(closestToStand, Is.LessThan(0.2f),
            "The cat must actually reach the spot under the rain head.");
        Assert.That(highestCat, Is.GreaterThan(door.position.y + 0.08f),
            "The cat must be lifted onto the tray, which it cannot step onto itself.");
        Assert.That(Flat(cat.transform.position, door.position), Is.LessThan(0.3f),
            "The cat must come back out to the door.");
        Assert.That(Flat(cat.transform.position, stand.position), Is.GreaterThan(0.55f),
            "The cat must end outside the product footprint, not inside the collider.");
        Assert.That(controller == null || controller.enabled, "Physics must be handed back.");
        // IsMovementLocked also folds in input-category blocks owned by other
        // systems in a single-scene load; the rinse only owns the physical lock.
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The rinse must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(CatActivity.Active, Is.Null);
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
