using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The six routines that closed out the Bathroom: litter tray, grooming cart,
/// laundry hamper, wall mirror, bath mat and the tub's rim walk.
///
/// One file rather than six, unlike the earlier hero activities, because these
/// share a single shape — buy the product, run the routine, assert the cat came
/// back with its physics, scale and rotation intact and outside the footprint.
/// The per-product detail that actually differs is asserted in EditMode, on the
/// prefab, where it does not need a scene load.
///
/// Not run in this project's automation: PlayMode through the editor bridge
/// wedges the editor (see Docs/ROADMAP.md). Run it from the Test Runner window.
/// </summary>
public sealed class BathroomPropActivityTests
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

    private static IEnumerator LoadBathroom()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    private static void Buy(string productId)
    {
        Assert.That(HomeStoreService.TryGetProduct(productId, out var product), Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(productId).Succeeded, Is.True);
    }

    /// <summary>
    /// Runs the activity to completion and asserts the contract every scripted
    /// routine in this project owes the cat: physics back, scale back, upright,
    /// movement lock released, never re-parented, and no activity left active.
    /// </summary>
    private static IEnumerator RunAndAssertClean(CatActivity activity, CatMovement cat)
    {
        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        // Every room parents the cat under "03 Character/CatRoot", so the
        // contract is that the routine gives the cat back where it found it.
        Transform originalParent = cat.transform.parent;

        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat), Is.True,
            activity.Kind + " must be usable when owned.");
        Assert.That(activity.IsRunning, Is.True);

        Time.timeScale = 8f;
        float deadline = Time.realtimeSinceStartup + 40f;
        while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            yield return null;
        Time.timeScale = 1f;

        Assert.That(activity.IsRunning, Is.False, "The routine must finish on its own.");
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
    public IEnumerator LitterDig_WalksTheCatInAndBackOut()
    {
        yield return LoadBathroom();

        LitterDigActivity dig =
            Object.FindAnyObjectByType<LitterDigActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(dig, Is.Not.Null, "The Bathroom scene must carry the litter dig activity.");
        Assert.That(cat, Is.Not.Null);

        Assert.That(dig.IsUnlocked, Is.False);
        Assert.That(dig.TryStart(cat), Is.False, "An unowned tray cannot be dug in.");
        Buy(HomeStoreService.BathroomLitterBoxId);
        dig.RefreshUnlockPresentation();

        float floorY = cat.transform.position.y;
        yield return RunAndAssertClean(dig, cat);
        Assert.That(cat.transform.position.y, Is.LessThan(floorY + 0.30f),
            "The tray is stepped into, not climbed onto.");
    }

    [UnityTest]
    public IEnumerator GroomBrush_DragsTheCatAlongTheRoller()
    {
        yield return LoadBathroom();

        GroomBrushActivity groom =
            Object.FindAnyObjectByType<GroomBrushActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(groom, Is.Not.Null, "The Bathroom scene must carry the groom activity.");
        Assert.That(cat, Is.Not.Null);

        Assert.That(groom.TryStart(cat), Is.False, "An unowned cart has no brush.");
        Buy(HomeStoreService.BathroomGroomingCartId);
        groom.RefreshUnlockPresentation();

        // The rub is lateral, so the cat has to actually travel along the roller
        // rather than pump in place.
        Vector3 before = cat.transform.position;
        yield return RunAndAssertClean(groom, cat);
        Assert.That(Vector3.Distance(before, cat.transform.position), Is.GreaterThan(0.2f),
            "The cat must move to the cart, not groom where it stood.");
    }

    [UnityTest]
    public IEnumerator HamperDive_SinksTheCatBelowTheRimAndBringsItBack()
    {
        yield return LoadBathroom();

        HamperDiveActivity dive =
            Object.FindAnyObjectByType<HamperDiveActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(dive, Is.Not.Null, "The Bathroom scene must carry the hamper dive.");
        Assert.That(cat, Is.Not.Null);

        Assert.That(dive.TryStart(cat), Is.False, "An unowned hamper cannot be dived into.");
        Buy(HomeStoreService.BathroomLaundryHamperId);
        dive.RefreshUnlockPresentation();

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        float floorY = cat.transform.position.y;

        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(dive.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float peakY = floorY;
        float lowestAfterPeak = float.PositiveInfinity;
        float deadline = Time.realtimeSinceStartup + 40f;
        while (dive.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            float y = cat.transform.position.y;
            peakY = Mathf.Max(peakY, y);
            if (peakY > floorY + 0.4f)
                lowestAfterPeak = Mathf.Min(lowestAfterPeak, y);
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(dive.IsRunning, Is.False);
        Assert.That(peakY, Is.GreaterThan(floorY + 0.5f), "The cat must clear the rim.");
        Assert.That(lowestAfterPeak, Is.LessThan(peakY - 0.15f),
            "The cat must sink into the pile, not perch on top of it.");
        Assert.That(cat.transform.position.y, Is.LessThan(floorY + 0.25f),
            "The cat must come back down before physics resumes.");
        Assert.That(controller == null || controller.enabled);
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(CatActivity.Active, Is.Null);
    }

    [UnityTest]
    public IEnumerator MirrorGaze_RunsTheSharedSitLook()
    {
        yield return LoadBathroom();

        SitLookActivity gaze = null;
        foreach (SitLookActivity candidate in
                 Object.FindObjectsByType<SitLookActivity>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.Kind == CatActivityKind.MirrorGaze)
                gaze = candidate;
        }
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(gaze, Is.Not.Null, "The Bathroom scene must carry the mirror gaze.");
        Assert.That(cat, Is.Not.Null);

        Assert.That(gaze.TryStart(cat), Is.False, "An unowned mirror has no reflection.");
        Buy(HomeStoreService.BathroomMirrorId);
        gaze.RefreshUnlockPresentation();

        yield return RunAndAssertClean(gaze, cat);
    }

    [UnityTest]
    public IEnumerator MatKnead_FlopsTheCatAndStandsItBackUp()
    {
        yield return LoadBathroom();

        MatKneadActivity knead =
            Object.FindAnyObjectByType<MatKneadActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(knead, Is.Not.Null, "The Bathroom scene must carry the mat knead.");
        Assert.That(cat, Is.Not.Null);

        Assert.That(knead.TryStart(cat), Is.False, "An unowned mat cannot be kneaded.");
        Buy(HomeStoreService.BathroomBathMatId);
        knead.RefreshUnlockPresentation();

        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(knead.TryStart(cat), Is.True);
        // The flop rolls the cat past what the capsule allows, so the one thing
        // that can go wrong here is it being handed back to physics on its side.
        Time.timeScale = 8f;
        float minUpright = 1f;
        float deadline = Time.realtimeSinceStartup + 40f;
        while (knead.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            minUpright = Mathf.Min(minUpright, Vector3.Dot(cat.transform.up, Vector3.up));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(knead.IsRunning, Is.False);
        Assert.That(minUpright, Is.LessThan(0.5f), "The cat must actually roll over.");
        Assert.That(Vector3.Dot(cat.transform.up, Vector3.up), Is.GreaterThan(0.9f),
            "And it must be upright again before physics resumes.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }

    [UnityTest]
    public IEnumerator TubEdgeWalk_PutsTheCatOnTheRimAndBackOnTheFloor()
    {
        yield return LoadBathroom();

        TubEdgeWalkActivity edge =
            Object.FindAnyObjectByType<TubEdgeWalkActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(edge, Is.Not.Null, "The Bathroom scene must carry the tub rim walk.");
        Assert.That(cat, Is.Not.Null);

        Assert.That(edge.TryStart(cat), Is.False, "An unowned tub has no rim to walk.");
        Buy(HomeStoreService.BathroomTubId);
        edge.RefreshUnlockPresentation();

        float floorY = cat.transform.position.y;
        float travelled = 0f;
        Vector3 previous = cat.transform.position;
        float peakY = floorY;

        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(edge.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float deadline = Time.realtimeSinceStartup + 40f;
        while (edge.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            peakY = Mathf.Max(peakY, cat.transform.position.y);
            if (cat.transform.position.y > floorY + 0.4f)
                travelled += Vector3.Distance(previous, cat.transform.position);
            previous = cat.transform.position;
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(edge.IsRunning, Is.False);
        Assert.That(peakY, Is.GreaterThan(floorY + 0.5f), "The cat must get onto the rim.");
        Assert.That(travelled, Is.GreaterThan(0.8f),
            "The cat must walk the rim, not just stand on it.");
        Assert.That(cat.transform.position.y, Is.LessThan(floorY + 0.25f),
            "The cat must come back down before physics resumes.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }
}
