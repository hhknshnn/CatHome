using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The Patio's ten routines. The porch swing already has SwingRideTests; this
/// file is the room tour, lookups BY KIND.
///
/// Not run through the editor bridge: it wedges PlayMode. Use the Test Runner.
/// </summary>
public sealed class PatioActivityTests
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

    private static IEnumerator LoadPatio()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Patio_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    /// <summary>
    /// The need bars live in <c>CatHome_UI</c>, which a single-scene load skips,
    /// so the fixture brings its own. The cat still has to want each routine:
    /// the fountain is a <see cref="SinkSipActivity"/> and refuses above 96
    /// thirst, and the naps refuse above about 92 energy.
    /// </summary>
    private static EnergySystem ProvisionNeeds()
    {
        return RoomPlayModeSupport.ProvisionNeeds();
    }

    private static CatActivity FindByKind(CatActivityKind kind)
    {
        foreach (CatActivity candidate in Object.FindObjectsByType<CatActivity>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.Kind == kind)
                return candidate;
        }

        return null;
    }

    private static void Buy(string productId)
    {
        Assert.That(HomeStoreService.TryGetProduct(productId, out var product), Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(productId).Succeeded, Is.True);
    }

    /// <summary>
    /// A refused start looks identical from the outside whether the product is
    /// unowned, the cat is out of energy or another routine is still active, so
    /// the failure message has to carry all three.
    /// </summary>
    private static string Diagnose(CatActivity activity, EnergySystem energy)
    {
        return activity.Kind + " must be usable when owned." +
               " unlocked=" + activity.IsUnlocked +
               " cost=" + activity.EnergyCost +
               " energy=" + (energy == null ? "NO ENERGY SYSTEM" : energy.CurrentEnergy.ToString("F1")) +
               " active=" + (CatActivity.Active == null ? "none" : CatActivity.Active.Kind.ToString());
    }

    private IEnumerator RunAndAssertClean(CatActivity activity, CatMovement cat)
    {
        EnergySystem energy = ProvisionNeeds();

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        // Every room parents the cat under "03 Character/CatRoot", so the
        // contract is that the routine gives the cat back where it found it.
        Transform originalParent = cat.transform.parent;

        Assert.That(activity.TryStart(cat), Is.True, Diagnose(activity, energy));
        Assert.That(activity.IsRunning, Is.True, activity.Kind + " must report itself running.");

        Time.timeScale = 8f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            yield return null;
        Time.timeScale = 1f;

        // The tour runs ten routines in a row, so every message names the one
        // that broke — a bare failure would only give a line number.
        Assert.That(activity.IsRunning, Is.False, activity.Kind + " must finish on its own.");
        Assert.That(controller == null || controller.enabled,
            activity.Kind + " must hand physics back.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            activity.Kind + " must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale),
            activity.Kind + " must undo any curl or stretch.");
        Assert.That(cat.transform.parent, Is.EqualTo(originalParent),
            activity.Kind + " must not re-parent the cat under a product.");
        Assert.That(Vector3.Dot(cat.transform.up, Vector3.up), Is.GreaterThan(0.9f),
            activity.Kind + " must leave the cat upright.");
        Assert.That(CatActivity.Active, Is.Null,
            activity.Kind + " must clear the active activity.");
    }

    [UnityTest]
    public IEnumerator EveryPatioRoutine_RunsCleanWhenOwned()
    {
        yield return LoadPatio();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null);

        var rows = new (CatActivityKind Kind, string ProductId)[]
        {
            (CatActivityKind.ArchClimb, HomeStoreService.PatioPergolaArchId),
            (CatActivityKind.SwingRide, HomeStoreService.PatioPorchSwingId),
            (CatActivityKind.ParasolScratch, HomeStoreService.PatioParasolId),
            (CatActivityKind.DiningPerch, HomeStoreService.PatioDiningSetId),
            (CatActivityKind.FirePitBask, HomeStoreService.PatioFirePitId),
            (CatActivityKind.FountainSip, HomeStoreService.PatioWaterFountainId),
            (CatActivityKind.FernWatch, HomeStoreService.PatioPottedFernsId),
            (CatActivityKind.HerbTroughDig, HomeStoreService.PatioHerbTroughId),
            (CatActivityKind.FestoonGaze, HomeStoreService.PatioStringLightsId),
            (CatActivityKind.StoneRugKnead, HomeStoreService.PatioStoneRugId),
        };

        foreach (var row in rows)
        {
            CatActivity activity = FindByKind(row.Kind);
            Assert.That(activity, Is.Not.Null, "Patio is missing " + row.Kind + ".");
            Assert.That(activity.TryStart(cat), Is.False,
                row.Kind + " must wait for the purchase.");
            Buy(row.ProductId);
            activity.RefreshUnlockPresentation();
            yield return RunAndAssertClean(activity, cat);
        }
    }
}
