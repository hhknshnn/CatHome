using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The Garden's ten store routines. Geometry stays in EditMode; this file only
/// proves each routine runs and hands the cat back. Lookups are BY KIND —
/// the lawn also carries a free yarn chase of a different kind.
///
/// Not run through the editor bridge: it wedges PlayMode. Use the Test Runner.
/// </summary>
public sealed class GardenActivityTests
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

    private static IEnumerator LoadGarden()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Garden_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    /// <summary>
    /// The Garden is the one room scene that authors its own hunger, thirst and
    /// energy, and the cat has to actually want each routine: the bird bath is a
    /// <see cref="SinkSipActivity"/> and refuses above 96 thirst. Full bars are
    /// why a routine can be perfectly built and still say no.
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
        var cat = Object.FindAnyObjectByType<CatMovement>();
        object[] args = { "" };
        bool ready = (bool)activity.GetType().GetMethod("CanBeginActivity", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).Invoke(activity, args);
        bool path = CatActivityMotion.TryFloorPath(cat.transform.position, activity.RoutineEntryPoint.position, out var route);
        return activity.Kind + " must be usable when owned." +
               " unlocked=" + activity.IsUnlocked +
               " cost=" + activity.EnergyCost +
               " energy=" + (energy == null ? "NO ENERGY SYSTEM" : energy.CurrentEnergy.ToString("F1")) +
               " active=" + (CatActivity.Active == null ? "none" : CatActivity.Active.Kind.ToString()) +
               " entry=" + activity.RoutineEntryPoint.position +
               " entryClear=" + CatActivityMotion.IsFloorClear(activity.RoutineEntryPoint.position) +
               " cat=" + cat.transform.position + " ready=" + ready + "/" + args[0] + " route=" + path;
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
            {RoomPlayModeSupport.StopObservedRest(activity);yield return null;}
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
    public IEnumerator EveryGardenRoutine_RunsCleanWhenOwned()
    {
        yield return LoadGarden();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null);

        var rows = new (CatActivityKind Kind, string ProductId)[]
        {
            (CatActivityKind.PergolaClimb, HomeStoreService.GardenPergolaId),
            (CatActivityKind.TreeScratch, HomeStoreService.GardenSaplingId),
            (CatActivityKind.BistroPerch, HomeStoreService.GardenBistroSetId),
            (CatActivityKind.HammockSway, HomeStoreService.GardenHammockId),
            (CatActivityKind.SunBask, HomeStoreService.GardenSunLoungerId),
            (CatActivityKind.BirdBathSip, HomeStoreService.GardenBirdBathId),
            (CatActivityKind.PotDig, HomeStoreService.GardenFlowerPotsId),
            (CatActivityKind.DaisyRoll, HomeStoreService.GardenDaisyBedId),
            (CatActivityKind.YarnBallChase, HomeStoreService.GardenYarnBallId),
        };

        foreach (var row in rows)
        {
            CatActivity activity = FindByKind(row.Kind);
            Assert.That(activity, Is.Not.Null, "Garden is missing " + row.Kind + ".");
            Assert.That(activity.TryStart(cat), Is.False,
                row.Kind + " must wait for the purchase.");
            Buy(row.ProductId);
            activity.RefreshUnlockPresentation();
            yield return RunAndAssertClean(activity, cat);
        }
    }

    [UnityTest]
    public IEnumerator HammockSway_RocksTheBedAndLeavesItLevel()
    {
        yield return LoadGarden();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var ride = FindByKind(CatActivityKind.HammockSway) as SwingRideActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(ride, Is.Not.Null);
        Assert.That(ride.SwingPivot, Is.Not.Null);

        Buy(HomeStoreService.GardenHammockId);
        ride.RefreshUnlockPresentation();

        Transform pivot = ride.SwingPivot;

        yield return RunAndAssertClean(ride, cat);

        Assert.That(pivot.localRotation, Is.EqualTo(Quaternion.identity)
            .Using<Quaternion>((a, b) => Quaternion.Angle(a, b) < 0.5f ? 0 : 1),
            "The hammock bed must be left level for the next ride.");
    }

    [UnityTest]
    public IEnumerator YarnBallChase_HopsTheBallThenHidesIt()
    {
        yield return LoadGarden();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var chase = FindByKind(CatActivityKind.YarnBallChase) as GardenYarnChaseActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(chase, Is.Not.Null);
        Assert.That(chase.YarnBall, Is.Not.Null);

        Buy(HomeStoreService.GardenYarnBallId);
        chase.RefreshUnlockPresentation();

        Transform ball = chase.YarnBall;
        Vector3 home = ball.position;

        EnergySystem energy = ProvisionNeeds();

        Assert.That(chase.TryStart(cat), Is.True, Diagnose(chase, energy));
        Time.timeScale = 8f;
        float maxTravel = 0f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (chase.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(chase);
            maxTravel = Mathf.Max(maxTravel, Vector3.Distance(home, ball.position));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(chase.IsRunning, Is.False,
            "The chase must finish on its own. catches=" + chase.CatchCount + "/" + chase.CatchGoal);
        Assert.That(maxTravel, Is.GreaterThan(0.15f), "The ball must hop, not sit in the nest.");
        Assert.That(ball.gameObject.activeSelf, Is.False,
            "The toy hides again so the next chase starts from the nest.");
        // The chase ends on a pounce, and that reaction holds a movement lock of
        // its own for a beat after the routine reports itself finished.
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The chase must leave the cat free to walk away.");
        Assert.That(CatActivity.Active, Is.Null);
    }
}
