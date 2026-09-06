using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The star tipi nap moves the cat with its CharacterController switched off,
/// so the thing worth proving at runtime is that the cat actually reaches the
/// nest, comes back out of the product footprint, and gets its physics, scale
/// and movement handed back.
/// </summary>
public sealed class CanopyNapTests
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
    public IEnumerator Nap_WalksTheCatIntoTheTipiAndBackOutWithPhysicsRestored()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bedroom_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());

        CanopyNapActivity nap =
            Object.FindAnyObjectByType<CanopyNapActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(nap, Is.Not.Null, "The Bedroom scene must carry the star tipi nap activity.");
        Assert.That(cat, Is.Not.Null);

        EnergySystem energy = Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
        if (energy == null)
        {
            // The energy HUD lives in CatHome_UI, which a single-scene load skips.
            energyObject = new GameObject("TestEnergySystem");
            energy = energyObject.AddComponent<EnergySystem>();
            yield return null;
        }

        Transform nest = nap.transform.Find("NapNestPoint");
        Transform door = nap.transform.Find("NapDoorPoint");
        Assert.That(nest, Is.Not.Null);
        Assert.That(door, Is.Not.Null);

        Assert.That(nap.IsUnlocked, Is.False, "The nap waits for the store purchase.");
        Assert.That(nap.TryStart(cat), Is.False, "An unowned tipi cannot be napped in.");

        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BedroomStarCanopyId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BedroomStarCanopyId).Succeeded, Is.True);
        nap.RefreshUnlockPresentation();
        Assert.That(nap.IsUnlocked, Is.True);

        energy.ApplySavedValue(100f);
        Assert.That(nap.TryStart(cat), Is.False, "A fully rested cat has no reason to nap.");
        Assert.That(nap.IsRunning, Is.False);
        Assert.That(energy.CurrentEnergy, Is.GreaterThan(95f),
            "A refused nap must not spend energy.");

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        energy.ApplySavedValue(30f);

        Assert.That(nap.TryStart(cat), Is.True, "An unlocked, tired cat must be able to nap.");
        Assert.That(nap.IsRunning, Is.True);
        Assert.That(controller == null || !controller.enabled,
            "Physics must be off while the cat is inside the tent.");

        Time.timeScale = 8f;
        float closestToNest = float.PositiveInfinity;
        float deadline = Time.realtimeSinceStartup + 25f;
        while (nap.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            closestToNest = Mathf.Min(closestToNest, Flat(cat.transform.position, nest.position));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(nap.IsRunning, Is.False, "The nap must finish on its own.");
        Assert.That(closestToNest, Is.LessThan(0.2f),
            "The cat must actually reach the nest inside the tipi.");
        Assert.That(Flat(cat.transform.position, door.position), Is.LessThan(0.3f),
            "The cat must come back out to the door.");
        Assert.That(Flat(cat.transform.position, nest.position), Is.GreaterThan(0.55f),
            "The cat must end outside the product footprint, not inside the collider.");
        Assert.That(controller == null || controller.enabled, "Physics must be handed back.");
        // IsMovementLocked also folds in input-category blocks owned by other
        // systems in a single-scene load; the nap only owns the physical lock.
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The nap must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(energy.CurrentEnergy, Is.GreaterThan(35f),
            "A nap must give energy back, not take it.");
        Assert.That(CatActivity.Active, Is.Null);
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
