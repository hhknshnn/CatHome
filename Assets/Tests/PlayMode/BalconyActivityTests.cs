using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The Balcony's ten routines. Lookups are BY KIND. The feeder shake is this
/// room's own class; the rest reuse shared beats.
///
/// Not run through the editor bridge: it wedges PlayMode. Use the Test Runner.
/// </summary>
public sealed class BalconyActivityTests
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

    private static IEnumerator LoadBalcony()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    /// <summary>
    /// The need bars live in <c>CatHome_UI</c>, which a single-scene load skips,
    /// so the fixture brings its own. Where a room authors its own set the cat
    /// still has to want each routine: a sip refuses above 96 thirst and the
    /// naps refuse above about 92 energy.
    /// </summary>
    private static EnergySystem ProvisionNeeds()
    {
        return RoomPlayModeSupport.ProvisionNeeds();
    }

    private static CatActivity FindByKind(CatActivityKind kind)
    {
        foreach (CatActivity candidate in Object.FindObjectsByType<CatActivity>(
                     FindObjectsInactive.Include))
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
    public IEnumerator EveryBalconyRoutine_RunsCleanWhenOwned()
    {
        yield return LoadBalcony();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null);

        var rows = new (CatActivityKind Kind, string ProductId)[]
        {
            (CatActivityKind.AwningGaze, HomeStoreService.BalconySunAwningId),
            (CatActivityKind.HerbShelfClimb, HomeStoreService.BalconyHerbShelfId),
            (CatActivityKind.FeederShake, HomeStoreService.BalconyBirdFeederId),
            (CatActivityKind.EggChairNap, HomeStoreService.BalconyHangingChairId),
            (CatActivityKind.BenchNap, HomeStoreService.BalconyCushionBenchId),
            (CatActivityKind.TableKnockOff, HomeStoreService.BalconySideTableId),
            (CatActivityKind.SunMatBask, HomeStoreService.BalconySunMatId),
            (CatActivityKind.PlanterDig, HomeStoreService.BalconyPlanterBoxId),
            (CatActivityKind.RailingSwat, HomeStoreService.BalconyRailingFlowersId),
            (CatActivityKind.LanternGaze, HomeStoreService.BalconyLanternStringId),
        };

        foreach (var row in rows)
        {
            CatActivity activity = FindByKind(row.Kind);
            Assert.That(activity, Is.Not.Null, "Balcony is missing " + row.Kind + ".");
            Assert.That(activity.TryStart(cat), Is.False,
                row.Kind + " must wait for the purchase.");
            Buy(row.ProductId);
            activity.RefreshUnlockPresentation();
            yield return RunAndAssertClean(activity, cat);
        }
    }

    [UnityTest]
    public IEnumerator FeederShake_SwingsTheFeederAndPutsItBack()
    {
        yield return LoadBalcony();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var shake = FindByKind(CatActivityKind.FeederShake) as BirdFeederShakeActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(shake, Is.Not.Null);
        Assert.That(shake.FeederPivot, Is.Not.Null);

        Buy(HomeStoreService.BalconyBirdFeederId);
        shake.RefreshUnlockPresentation();

        Transform feeder = shake.FeederPivot;
        Transform seed = shake.SeedPivot;
        Quaternion feederHome = feeder.localRotation;
        Vector3 feederHomePos = feeder.localPosition;
        Vector3 seedHome = seed != null ? seed.localPosition : Vector3.zero;

        EnergySystem energy = ProvisionNeeds();

        Assert.That(shake.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float maxSwing = 0f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (shake.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(shake);
            maxSwing = Mathf.Max(maxSwing, Quaternion.Angle(feederHome, feeder.localRotation));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(shake.IsRunning, Is.False);
        Assert.That(maxSwing, Is.GreaterThan(4f), "The feeder must actually swing.");
        Assert.That(Quaternion.Angle(feederHome, feeder.localRotation), Is.LessThan(0.5f),
            "The feeder must hang still again.");
        Assert.That(Vector3.Distance(feederHomePos, feeder.localPosition), Is.LessThan(0.001f));
        if (seed != null)
        {
            Assert.That(Vector3.Distance(seedHome, seed.localPosition), Is.LessThan(0.001f),
                "The seed must be back in the cup.");
        }

        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }

    [UnityTest]
    public IEnumerator TableKnockOff_PushesTheCupOffAndPutsItBack()
    {
        yield return LoadBalcony();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var knock = FindByKind(CatActivityKind.TableKnockOff) as KnockOffActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(knock, Is.Not.Null);

        Buy(HomeStoreService.BalconySideTableId);
        knock.RefreshUnlockPresentation();

        Transform glass = knock.GlassPivot;
        Assert.That(glass, Is.Not.Null);
        Vector3 home = glass.localPosition;
        Quaternion homeRotation = glass.localRotation;

        EnergySystem energy = ProvisionNeeds();

        Assert.That(knock.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float lowestY = home.y;
        float maxTilt = 0f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (knock.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(knock);
            lowestY = Mathf.Min(lowestY, glass.localPosition.y);
            maxTilt = Mathf.Max(maxTilt, Quaternion.Angle(homeRotation, glass.localRotation));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(knock.IsRunning, Is.False);
        Assert.That(home.y - lowestY, Is.GreaterThan(knock.FallHeight * 0.5f),
            "The cup must go over the edge, not just rock.");
        Assert.That(maxTilt, Is.GreaterThan(45f), "And it must end up on its side.");
        Assert.That(Vector3.Distance(home, glass.localPosition), Is.LessThan(0.001f),
            "And it must be back exactly where it started.");
        Assert.That(Quaternion.Angle(homeRotation, glass.localRotation), Is.LessThan(0.5f),
            "Standing up again, not lying on the table.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }
}
