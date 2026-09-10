using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The Kitchen's ten routines. One file, like the Bathroom's props, because they
/// share the same contract: buy the product, run the routine, get the cat back
/// with its physics, scale and rotation intact and outside the footprint. The
/// per-product geometry is asserted in EditMode where no scene load is needed.
///
/// Six of the ten come from three shared classes, so every lookup here is BY
/// KIND — a FindAnyObjectByType would return whichever instance loaded first.
///
/// Not run in this project's automation: PlayMode through the editor bridge
/// wedges the editor (see Docs/ROADMAP.md). Run it from the Test Runner window.
/// </summary>
public sealed class KitchenActivityTests
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

    private static IEnumerator LoadKitchen()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Kitchen_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
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

    private static IEnumerator RunAndAssertClean(CatActivity activity, CatMovement cat)
    {
        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        // Every room parents the cat under "03 Character/CatRoot", so the
        // contract is that the routine gives the cat back where it found it.
        Transform originalParent = cat.transform.parent;

        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat), Is.True, activity.Kind + " must be usable when owned.");
        Assert.That(activity.IsRunning, Is.True);

        Time.timeScale = 8f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            {RoomPlayModeSupport.StopObservedRest(activity);yield return null;}
        Time.timeScale = 1f;

        Assert.That(activity.IsRunning, Is.False, activity.Kind + " must finish on its own.");
        Assert.That(controller == null || controller.enabled, "Physics must be handed back.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The routine must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(cat.transform.parent, Is.EqualTo(originalParent),
            "The cat is never re-parented under a product.");
        Assert.That(Vector3.Dot(cat.transform.up, Vector3.up), Is.GreaterThan(0.9f),
            "The cat must end upright.");
        Assert.That(CatActivity.Active, Is.Null);
    }

    [UnityTest]
    public IEnumerator EveryKitchenRoutine_RunsCleanWhenOwned()
    {
        yield return LoadKitchen();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null);

        var rows = new (CatActivityKind Kind, string ProductId)[]
        {
            (CatActivityKind.IslandPerch, HomeStoreService.KitchenIslandId),
            (CatActivityKind.StoolPerch, HomeStoreService.KitchenCounterStoolId),
            (CatActivityKind.FridgeStare, HomeStoreService.KitchenRefrigeratorId),
            (CatActivityKind.FruitSwat, HomeStoreService.KitchenFruitBasketId),
            (CatActivityKind.PantryClimb, HomeStoreService.KitchenPantryShelfId),
            (CatActivityKind.OvenWarmth, HomeStoreService.KitchenStoveOvenId),
            (CatActivityKind.CartNudge, HomeStoreService.KitchenDishCartId),
            (CatActivityKind.MealTime, HomeStoreService.KitchenFeedingStationId),
            (CatActivityKind.KitchenSip, HomeStoreService.KitchenSinkCabinetId),
            (CatActivityKind.KitchenMatKnead, HomeStoreService.KitchenPawMatId),
        };

        foreach (var row in rows)
        {
            CatActivity activity = FindByKind(row.Kind);
            Assert.That(activity, Is.Not.Null, "Kitchen is missing " + row.Kind + ".");
            Assert.That(activity.TryStart(cat), Is.False,
                row.Kind + " must wait for the purchase.");
            Buy(row.ProductId);
            activity.RefreshUnlockPresentation();
            yield return RunAndAssertClean(activity, cat);
        }
    }

    [UnityTest]
    public IEnumerator PantryClimb_StepsUpTwiceAndComesAllTheWayDown()
    {
        yield return LoadKitchen();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        CatActivity climb = FindByKind(CatActivityKind.PantryClimb);
        Assert.That(cat, Is.Not.Null);
        Assert.That(climb, Is.Not.Null);

        Buy(HomeStoreService.KitchenPantryShelfId);
        climb.RefreshUnlockPresentation();

        float floorY = cat.transform.position.y;
        float peakY = floorY;
        // The point of a shelf unit is that the cat stages the climb, so the
        // trace has to show a plateau on the way up rather than one long arc.
        bool sawLowerPlateau = false;

        Assert.That(climb.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (climb.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(climb);
            float y = cat.transform.position.y;
            peakY = Mathf.Max(peakY, y);
            if (y > floorY + 0.30f && y < floorY + 0.75f)
                sawLowerPlateau = true;
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(climb.IsRunning, Is.False);
        Assert.That(sawLowerPlateau, Is.True, "The cat must stop on the lower shelf.");
        Assert.That(peakY, Is.GreaterThan(floorY + 0.75f), "And then reach the upper one.");
        Assert.That(cat.transform.position.y, Is.LessThan(floorY + 0.25f),
            "And be back on the floor before physics resumes.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }

    [UnityTest]
    public IEnumerator CartNudge_MovesTheCartAndPutsItBack()
    {
        yield return LoadKitchen();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var nudge = FindByKind(CatActivityKind.CartNudge) as CartNudgeActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(nudge, Is.Not.Null);

        Buy(HomeStoreService.KitchenDishCartId);
        nudge.RefreshUnlockPresentation();

        Transform visual = nudge.CartVisual;
        Assert.That(visual, Is.Not.Null);
        Vector3 home = visual.localPosition;

        Assert.That(nudge.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float maxOffset = 0f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (nudge.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            RoomPlayModeSupport.StopObservedRest(nudge);
            maxOffset = Mathf.Max(maxOffset, Vector3.Distance(home, visual.localPosition));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(nudge.IsRunning, Is.False);
        Assert.That(maxOffset, Is.GreaterThan(0.05f), "The cart must visibly roll.");
        // A cart that kept a little of every shove would eventually stand
        // somewhere the catalog never placed it.
        Assert.That(Vector3.Distance(home, visual.localPosition), Is.LessThan(0.001f),
            "And it must come to rest exactly where it started.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }
}
