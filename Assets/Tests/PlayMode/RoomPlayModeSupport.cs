using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared setup for the room activity fixtures. Every one of them loads one room
/// scene on its own and drives a routine, so they all need the same two things
/// the room scene does not carry by itself.
///
/// First, the authoring redirect. Loading a room scene in Single mode looks
/// exactly like a designer pressing Play from that scene, so
/// DirectLevelPlayBootstrap adds GameScene underneath it. That arrives some
/// frames later and re-applies the save file, which silently re-grants products
/// a fixture just reset, and it can add a second room and a second cat. Every
/// fixture suppresses it so the room under test is the only world in the
/// session.
///
/// Second, the need systems. Hunger, Thirst and Energy live in CatHome_UI, which
/// a single-scene load skips, and Garden is the one room scene that authors its
/// own. Without energy CatActivity.TryStart refuses every routine, and with the
/// thirst system present and full, SinkSipActivity rightly refuses too.
/// </summary>
internal static class RoomPlayModeSupport
{
    private const float ActivityEnergy = 40f;
    private const float SipReadyThirst = 35f;
    private const float MealReadyHunger = 35f;

    private static GameObject provisionedNeeds;

    /// <summary>
    /// Loads a room scene as the only world in the session, with the authoring
    /// redirect held off and the cat's needs in a state where every routine can
    /// start. Call ReleaseRoom in TearDown.
    /// </summary>
    internal static IEnumerator LoadRoomAlone(string sceneName)
    {
        DirectLevelPlayBootstrap.RedirectSuppressed = true;

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, sceneName + " must be in Build Settings.");
        while (!load.isDone)
            yield return null;
        yield return null;

        ProvisionNeeds();
        yield return null;
    }

    internal static void ReleaseRoom()
    {
        DirectLevelPlayBootstrap.RedirectSuppressed = false;
        if (provisionedNeeds != null)
        {
            Object.DestroyImmediate(provisionedNeeds);
            provisionedNeeds = null;
        }
    }

    /// <summary>
    /// Puts energy, thirst and hunger where a routine can run: enough energy for
    /// the most expensive activity, and thirst/hunger low enough that the sip and
    /// meal routines do not refuse. Systems the room scene does not author are
    /// created here, matching the pattern in SinkSipTests.
    /// </summary>
    internal static EnergySystem ProvisionNeeds()
    {
        EnergySystem energy =
            Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
        if (energy == null)
        {
            energy = EnsureNeedsHost().AddComponent<EnergySystem>();
        }

        energy.ApplySavedValue(ActivityEnergy);

        ThirstSystem thirst =
            Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (thirst != null)
            thirst.ApplySavedValue(SipReadyThirst);

        HungerSystem hunger =
            Object.FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        if (hunger != null)
            hunger.ApplySavedValue(MealReadyHunger);

        return energy;
    }

    /// <summary>
    /// A routine may finish while a cosmetic CatActivityReaction is still
    /// playing, and that reaction holds a movement lock of its own. The contract
    /// is that the cat is not left locked, so give the tail a bounded moment
    /// instead of sampling the single frame the routine ended on.
    /// </summary>
    internal static IEnumerator WaitForMovementRelease(CatMovement cat)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (cat != null && cat.IsMovementPhysicallyLocked &&
               Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
    }

    private static GameObject EnsureNeedsHost()
    {
        if (provisionedNeeds == null)
            provisionedNeeds = new GameObject("TestNeedSystems");
        return provisionedNeeds;
    }
}
