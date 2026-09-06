using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The towel nest is the bathroom's sleep beat, and the first nap the cat has to
/// jump up into: the bed sits 0.86 above the floor. The three things that can go
/// wrong are the cat never reaching the stack, the cat being left up there when
/// the routine ends, and the scripted lift never handing physics back.
///
/// Not run in this project's automation: PlayMode through the editor bridge
/// wedges the editor (see Docs/ROADMAP.md). Run it from the Test Runner window.
/// </summary>
public sealed class TowelNestTests
{
    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        RoomPlayModeSupport.ReleaseRoom();
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    [UnityTest]
    public IEnumerator Nest_LiftsTheCatOntoTheStackAndPutsItBackDown()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());

        TowelNestActivity nest =
            Object.FindAnyObjectByType<TowelNestActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(nest, Is.Not.Null, "The Bathroom scene must carry the towel nest activity.");
        Assert.That(cat, Is.Not.Null);

        Assert.That(nest.IsUnlocked, Is.False, "The nest waits for the store purchase.");
        Assert.That(nest.TryStart(cat), Is.False, "An unowned cabinet has no niche to nap in.");

        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.BathroomTowelStorageId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomTowelStorageId).Succeeded,
            Is.True);
        nest.RefreshUnlockPresentation();
        Assert.That(nest.IsUnlocked, Is.True);

        // The nap is a sleep beat, so it wants a tired cat: the shared helper
        // leaves energy well under the activity's wide-awake refusal.
        RoomPlayModeSupport.ProvisionNeeds();

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        // Every room parents the cat under "03 Character/CatRoot", so the
        // contract is that the nap gives the cat back where it found it.
        Transform originalParent = cat.transform.parent;
        float floorY = cat.transform.position.y;

        Assert.That(nest.TryStart(cat), Is.True, "An unlocked niche must be nappable.");
        Assert.That(nest.IsRunning, Is.True);

        Time.timeScale = 8f;
        float peakY = floorY;
        float deadline = Time.realtimeSinceStartup + 30f;
        while (nest.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            peakY = Mathf.Max(peakY, cat.transform.position.y);
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(nest.IsRunning, Is.False, "The nap must finish on its own.");
        Assert.That(peakY, Is.GreaterThan(floorY + 0.5f),
            "The cat must actually be lifted onto the towel stack.");
        Assert.That(cat.transform.position.y, Is.LessThan(floorY + 0.25f),
            "The cat must come back down before physics resumes.");
        Assert.That(controller == null || controller.enabled, "Physics must be handed back.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The nap must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale),
            "The curl must be undone.");
        Assert.That(cat.transform.parent, Is.EqualTo(originalParent),
            "The cat is never re-parented under the cabinet.");
        Assert.That(CatActivity.Active, Is.Null);
    }

    [UnityTest]
    public IEnumerator Nest_RefusesWhenTheCatIsWideAwake()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());

        TowelNestActivity nest =
            Object.FindAnyObjectByType<TowelNestActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        EnergySystem energy = RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(nest, Is.Not.Null);
        Assert.That(cat, Is.Not.Null);
        Assert.That(energy, Is.Not.Null);

        Assert.That(
            HomeStoreService.TryGetProduct(
                HomeStoreService.BathroomTowelStorageId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomTowelStorageId).Succeeded,
            Is.True);
        nest.RefreshUnlockPresentation();

        energy.ApplySavedValue(100f);
        Assert.That(nest.TryStart(cat), Is.False,
            "A wide awake cat has no reason to climb into the towels.");
        Assert.That(nest.IsRunning, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }
}
