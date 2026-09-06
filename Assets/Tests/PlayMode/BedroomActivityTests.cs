using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The Bedroom's ten routines. One file, like the Kitchen's, because they share
/// the same contract: buy the product, run the routine, get the cat back with
/// its physics, scale and rotation intact and outside the footprint. The
/// per-product geometry is asserted in EditMode where no scene load is needed.
///
/// Nine of the ten come from six shared classes, so every lookup here is BY
/// KIND — a FindAnyObjectByType would return whichever instance loaded first.
///
/// Not run in this project's automation: PlayMode through the editor bridge
/// wedges the editor (see Docs/ROADMAP.md). Run it from the Test Runner window.
/// </summary>
public sealed class BedroomActivityTests
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

    private static IEnumerator LoadBedroom()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bedroom_Level01");

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
            yield return null;
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
    public IEnumerator EveryBedroomRoutine_RunsCleanWhenOwned()
    {
        yield return LoadBedroom();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null);

        var rows = new (CatActivityKind Kind, string ProductId)[]
        {
            (CatActivityKind.BedNap, HomeStoreService.BedroomQueenBedId),
            (CatActivityKind.WardrobeScratch, HomeStoreService.BedroomWardrobeId),
            (CatActivityKind.DaybedWatch, HomeStoreService.BedroomWindowDaybedId),
            (CatActivityKind.KnockOff, HomeStoreService.BedroomNightstandId),
            (CatActivityKind.VanityStoolNap, HomeStoreService.BedroomVanityStoolId),
            (CatActivityKind.YarnSwat, HomeStoreService.BedroomYarnBasketId),
            (CatActivityKind.NightLightGaze, HomeStoreService.BedroomNightLightId),
            (CatActivityKind.ArtGaze, HomeStoreService.BedroomDreamArtId),
            (CatActivityKind.BedroomMatKnead, HomeStoreService.BedroomPawRugId),
            (CatActivityKind.CanopyNap, HomeStoreService.BedroomStarCanopyId),
        };

        foreach (var row in rows)
        {
            CatActivity activity = FindByKind(row.Kind);
            Assert.That(activity, Is.Not.Null, "Bedroom is missing " + row.Kind + ".");
            Assert.That(activity.TryStart(cat), Is.False,
                row.Kind + " must wait for the purchase.");
            Buy(row.ProductId);
            activity.RefreshUnlockPresentation();
            yield return RunAndAssertClean(activity, cat);
        }
    }

    [UnityTest]
    public IEnumerator KnockOff_PushesTheGlassOffAndPutsItBack()
    {
        yield return LoadBedroom();

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        var knock = FindByKind(CatActivityKind.KnockOff) as KnockOffActivity;
        Assert.That(cat, Is.Not.Null);
        Assert.That(knock, Is.Not.Null);

        Buy(HomeStoreService.BedroomNightstandId);
        knock.RefreshUnlockPresentation();

        Transform glass = knock.GlassPivot;
        Assert.That(glass, Is.Not.Null);
        Vector3 home = glass.localPosition;
        Quaternion homeRotation = glass.localRotation;

        Assert.That(knock.TryStart(cat), Is.True);
        Time.timeScale = 8f;
        float lowestY = home.y;
        float maxTilt = 0f;
        float deadline = Time.realtimeSinceStartup + 45f;
        while (knock.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            lowestY = Mathf.Min(lowestY, glass.localPosition.y);
            maxTilt = Mathf.Max(maxTilt, Quaternion.Angle(homeRotation, glass.localRotation));
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(knock.IsRunning, Is.False);
        // The whole point of the routine: the glass has to actually leave the
        // top and land on its side, not wobble in place.
        Assert.That(home.y - lowestY, Is.GreaterThan(knock.FallHeight * 0.5f),
            "The glass must go over the edge, not just rock.");
        Assert.That(maxTilt, Is.GreaterThan(45f), "And it must end up on its side.");
        // A glass that stayed on the floor would be somewhere the catalog never
        // placed it, and the next cat would knock one that was already down.
        Assert.That(Vector3.Distance(home, glass.localPosition), Is.LessThan(0.001f),
            "And it must be back exactly where it started.");
        Assert.That(Quaternion.Angle(homeRotation, glass.localRotation), Is.LessThan(0.5f),
            "Standing up again, not lying on the nightstand.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(CatActivity.Active, Is.Null);
    }
}
