using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The Second Floor's ten routines. Lookups are BY KIND. The two moving parts
/// — the loose book and the record — get their own restore checks.
///
/// Not run through the editor bridge: it wedges PlayMode. Use the Test Runner.
/// </summary>
public sealed class SecondFloorActivityTests
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

    private static IEnumerator LoadLoft()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("SecondFloor_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    /// <summary>
    /// The need bars live in <c>CatHome_UI</c>, which a single-scene load skips,
    /// so the fixture brings its own. The cat still has to want each routine: a
    /// sip refuses above 96 thirst and the naps refuse above about 92 energy.
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
    public IEnumerator EverySecondFloorRoutine_RunsCleanWhenOwned()
    {
        yield return LoadLoft();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null);

        var rows = new (CatActivityKind Kind, string ProductId)[]
        {
            (CatActivityKind.RunnerKnead, HomeStoreService.LoftFloorRunnerId),
            (CatActivityKind.CushionNest, HomeStoreService.LoftFloorCushionsId),
            (CatActivityKind.BookKnockOff, HomeStoreService.LoftBookStackId),
            (CatActivityKind.LampGlowBask, HomeStoreService.LoftArcLampId),
            (CatActivityKind.BeanBagNap, HomeStoreService.LoftBeanBagId),
            (CatActivityKind.RecordSpin, HomeStoreService.LoftRecordPlayerId),
            (CatActivityKind.DeskPerch, HomeStoreService.LoftStudyDeskId),
            (CatActivityKind.GalleryGaze, HomeStoreService.LoftWallGalleryId),
            (CatActivityKind.BookcaseClimb, HomeStoreService.LoftTallBookcaseId),
            (CatActivityKind.ChaiseNap, HomeStoreService.LoftChaiseLoungeId),
        };

        foreach (var row in rows)
        {
            CatActivity activity = FindByKind(row.Kind);
            Assert.That(activity, Is.Not.Null, "Second Floor is missing " + row.Kind + ".");
            Assert.That(activity.TryStart(cat), Is.False,
                row.Kind + " must wait for the purchase.");
            Buy(row.ProductId);
            activity.RefreshUnlockPresentation();
            yield return RunAndAssertClean(activity, cat);
        }
    }

    [UnityTest]
    public IEnumerator BookKnockOff_PushesTheVolumeOffAndPutsItBack()
    {
        yield return LoadLoft();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var knock = FindByKind(CatActivityKind.BookKnockOff) as KnockOffActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(knock, Is.Not.Null);

        Buy(HomeStoreService.LoftBookStackId);
        knock.RefreshUnlockPresentation();

        Transform book = knock.GlassPivot;
        Assert.That(book, Is.Not.Null);
        Vector3 home = book.localPosition;
        Quaternion homeRotation = book.localRotation;

        EnergySystem energy = ProvisionNeeds();

        Assert.That(knock.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float lowestY = home.y;
        float maxTilt = 0f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (knock.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(knock);
            lowestY = Mathf.Min(lowestY, book.localPosition.y);
            maxTilt = Mathf.Max(maxTilt, Quaternion.Angle(homeRotation, book.localRotation));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(knock.IsRunning, Is.False);
        Assert.That(home.y - lowestY, Is.GreaterThan(knock.FallHeight * 0.5f),
            "The book must leave the stack, not just rock.");
        Assert.That(maxTilt, Is.GreaterThan(45f), "And it must land on its side.");
        Assert.That(Vector3.Distance(home, book.localPosition), Is.LessThan(0.001f),
            "And it must be back on the stack.");
        Assert.That(Quaternion.Angle(homeRotation, book.localRotation), Is.LessThan(0.5f),
            "Standing up again, not lying beside the pile.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }

    [UnityTest]
    public IEnumerator RecordSpin_TurnsTheDiscThenHandsTheCatBack()
    {
        yield return LoadLoft();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var spin = FindByKind(CatActivityKind.RecordSpin) as PaperSpinActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(spin, Is.Not.Null);
        Assert.That(spin.RollPivot, Is.Not.Null);

        Buy(HomeStoreService.LoftRecordPlayerId);
        spin.RefreshUnlockPresentation();

        Transform pivot = spin.RollPivot;
        Quaternion rest = pivot.localRotation;

        EnergySystem energy = ProvisionNeeds();

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;

        Assert.That(spin.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float maxTurn = 0f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (spin.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(spin);
            maxTurn = Mathf.Max(maxTurn, Quaternion.Angle(rest, pivot.localRotation));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(spin.IsRunning, Is.False);
        Assert.That(maxTurn, Is.GreaterThan(45f), "The record must visibly turn.");
        Assert.That(controller == null || controller.enabled);
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(CatActivity.Active, Is.Null);
    }
}
