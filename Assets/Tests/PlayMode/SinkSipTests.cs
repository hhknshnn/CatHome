using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CatHome.Economy;

/// <summary>
/// The vanity sip lifts the cat onto a counter it cannot jump onto, so the
/// runtime facts worth proving are that the cat actually gets up there, drinks,
/// comes back down to the floor, and gets its physics, scale and movement lock
/// handed back.
///
/// Not run in this project's automation: PlayMode through the editor bridge
/// wedges the editor (see Docs/ROADMAP.md). Run it from the Test Runner window.
/// </summary>
public sealed class SinkSipTests
{
    private GameObject thirstObject;

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        RoomPlayModeSupport.ReleaseRoom();
        if (thirstObject != null)
            Object.DestroyImmediate(thirstObject);
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
    }

    [UnityTest]
    public IEnumerator Sip_LiftsTheCatOntoTheCounterAndBackDown()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");

        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());

        SinkSipActivity sip =
            Object.FindAnyObjectByType<SinkSipActivity>(FindObjectsInactive.Include);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(sip, Is.Not.Null, "The Bathroom scene must carry the vanity sip activity.");
        Assert.That(cat, Is.Not.Null);

        ThirstSystem thirst = Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (thirst == null)
        {
            // The thirst HUD lives in CatHome_UI, which a single-scene load skips.
            thirstObject = new GameObject("TestThirstSystem");
            thirst = thirstObject.AddComponent<ThirstSystem>();
            yield return null;
        }

        Transform perch = sip.transform.Find("SipPerchPoint");
        Transform floor = sip.transform.Find("SipFloorPoint");
        Assert.That(perch, Is.Not.Null);
        Assert.That(floor, Is.Not.Null);

        Assert.That(sip.IsUnlocked, Is.False, "The sip waits for the store purchase.");
        Assert.That(sip.TryStart(cat), Is.False, "An unowned vanity cannot be drunk from.");

        Assert.That(
            HomeStoreService.TryGetProduct(HomeStoreService.BathroomVanityId, out var product),
            Is.True);
        EconomyService.AddCurrency(CurrencyType.Coin, product.CoinPrice, EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BathroomVanityId).Succeeded, Is.True);
        sip.RefreshUnlockPresentation();
        Assert.That(sip.IsUnlocked, Is.True);

        thirst.ApplySavedValue(100f);
        Assert.That(sip.TryStart(cat), Is.False, "A cat that is not thirsty walks away.");
        Assert.That(sip.IsRunning, Is.False);

        CharacterController controller = cat.GetComponent<CharacterController>();
        Vector3 originalScale = cat.transform.localScale;
        RoomPlayModeSupport.ProvisionNeeds();
        thirst.ApplySavedValue(35f);

        Assert.That(sip.TryStart(cat), Is.True, "An unlocked tap must be usable when thirsty.");
        Assert.That(sip.IsRunning, Is.True);
        Assert.That(controller == null || !controller.enabled,
            "Physics must be off while the cat is on the counter.");

        Time.timeScale = 8f;
        float closestToPerch = float.PositiveInfinity;
        float highestCat = float.NegativeInfinity;
        float deadline = Time.realtimeSinceStartup + 25f;
        while (sip.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            closestToPerch = Mathf.Min(closestToPerch, Vector3.Distance(
                cat.transform.position, perch.position));
            highestCat = Mathf.Max(highestCat, cat.transform.position.y);
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.That(sip.IsRunning, Is.False, "The sip must finish on its own.");
        Assert.That(closestToPerch, Is.LessThan(0.2f),
            "The cat must actually reach the counter perch.");
        Assert.That(highestCat, Is.GreaterThan(perch.position.y),
            "The hop must arc above the counter, not slide up to it.");
        Assert.That(cat.transform.position.y, Is.LessThan(perch.position.y - 0.3f),
            "The cat must come back down to the floor.");
        Assert.That(controller == null || controller.enabled, "Physics must be handed back.");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False,
            "The sip must release the movement lock it took.");
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
        Assert.That(thirst.CurrentThirst, Is.GreaterThan(60f), "A sip must top the bar up.");
        Assert.That(CatActivity.Active, Is.Null);
    }
}
