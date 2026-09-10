using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The paper spin is the first activity where the product moves and the cat does
/// not: the roll has to actually turn, run down, and the cat has to keep its
/// physics. The pivot must also come back to rest, or a second swat would start
/// from a random angle.
///
/// Not run in this project's automation: PlayMode through the editor bridge
/// wedges the editor (see Docs/ROADMAP.md). Run it from the Test Runner window.
/// </summary>
public sealed class PaperSpinTests
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
    public IEnumerator Spin_TurnsTheRollAndLeavesTheCatOnItsFeet()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());

        PaperSpinActivity spin =
            Object.FindAnyObjectByType<PaperSpinActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(spin, Is.Not.Null, "The Bathroom scene must carry the paper spin activity.");
        Assert.That(cat, Is.Not.Null);

        Transform pivot = spin.RollPivot;
        Assert.That(pivot, Is.Not.Null);
        Quaternion restRotation = pivot.localRotation;

        Assert.That(spin.IsUnlocked, Is.False, "The spin waits for the store purchase.");
        Assert.That(spin.TryStart(cat), Is.False, "An unowned toilet has no roll to swat.");

        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BathroomToiletId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomToiletId).Succeeded, Is.True);
        spin.RefreshUnlockPresentation();
        Assert.That(spin.IsUnlocked, Is.True);

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;

        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(spin.TryStart(cat), Is.True, "An unlocked roll must be swattable.");
        Assert.That(spin.IsRunning, Is.True);

        Time.timeScale = 8f;
        float maxTurn = 0f;
        float deadline = Time.realtimeSinceStartup + 25f;
        while (spin.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(spin);
            maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(restRotation, pivot.localRotation));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(spin.IsRunning, Is.False, "The spin must finish on its own.");
        Assert.That(maxTurn, Is.GreaterThan(45f), "The roll must visibly turn.");
        Assert.That(controller == null || controller.enabled, "Physics must be handed back.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The spin must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(CatActivity.Active, Is.Null);
    }
}
