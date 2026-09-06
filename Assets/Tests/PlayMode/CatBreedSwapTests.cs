using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Swapping the cat breed must leave a cat that still animates.
///
/// Regression from 5 September 2026: deferred Destroy left the retired visual
/// discoverable for the rest of the frame. A second selection or runtime scan
/// cloned its inactive state and rebound gameplay to an invisible Animator.
/// Cover real frames, same-frame ordering, rapid changes and inactive owners.
/// Shared clips still bind through the normalized Cat_Domestic_Shorthair path.
///
/// Not run through the editor bridge: it wedges PlayMode. Use the Test Runner.
/// </summary>
public sealed class CatBreedSwapTests
{
    /// <summary>A walking cat's spine turns well past this in half a second.</summary>
    private const float AnimatedRotationDegrees = .25f;

    private const string ProbeBoneName = "DEF-spine";
    private const string SpeedParameter = "Speed";

    private string restoreBreedId;

    [SetUp]
    public void SetUp()
    {
        restoreBreedId = CatBreedService.SelectedBreedId;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        CatBreedService.Select(restoreBreedId);
        RoomPlayModeSupport.ReleaseRoom();
    }

    /// <summary>
    /// This fixture is the first one to leave the LIVING ROOM loaded, and the
    /// Living Room is the one room whose floor object is called exactly "Floor".
    /// `CatCatchHuntTests` loads its arena additively straight afterwards and
    /// used to measure the floor with a global `GameObject.Find("Floor")`, so it
    /// picked this room's floor and read the cat as flying. That lookup is
    /// scoped to the arena now; the note stays so the next fixture that loads
    /// the Living Room knows what it is standing next to.
    /// </summary>
    private const string LivingRoomSceneName = "LivingRoom_Level01";

    /// <summary>
    /// Every breed, one after another in the same session, then back to the
    /// starting breed. The order matters: the reported failure only showed up
    /// after a swap had already happened, and the return leg is the half that
    /// the player said stayed broken.
    /// </summary>
    [UnityTest]
    public IEnumerator EveryBreed_StillAnimatesAfterTheSwap()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(LivingRoomSceneName);

        CatBreedCatalog catalog = CatBreedCatalog.Load();
        Assert.That(catalog, Is.Not.Null, "The breed catalog resource is missing.");
        Assert.That(catalog.Count, Is.GreaterThan(1), "Nothing to swap between.");

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null, "The Living Room has no cat.");

        // The starting cat is the authored one; it is the control sample.
        yield return SettleFrames();
        AssertAnimates(cat, "the authored starting cat");

        var broken = new List<string>();
        for (int i = 0; i < catalog.Count; i++)
        {
            CatBreedCatalog.Entry entry = catalog.Get(i);
            Assert.That(CatBreedService.Select(entry.Id), Is.True, entry.Id);
            yield return SettleFrames();

            if (!Animates(cat, out float turned))
                broken.Add(entry.Id + " (" + turned.ToString("F3") + " deg)");
        }

        Assert.That(CatBreedService.Select(CatBreedCatalog.DefaultBreedId), Is.True);
        yield return SettleFrames();
        if (!Animates(cat, out float back))
            broken.Add("return to " + CatBreedCatalog.DefaultBreedId +
                       " (" + back.ToString("F3") + " deg)");

        Assert.That(broken, Is.Empty,
            "These breeds left the cat in its bind pose: " + string.Join(", ", broken));
    }

    /// <summary>
    /// One swapped-in cat must be the ONLY cat visual on the owner. A leftover
    /// root would keep a second Animator alive and every rebind that resolves
    /// the animator by GetComponentInChildren could pick the dead one.
    /// </summary>
    [UnityTest]
    public IEnumerator SwappingBreeds_LeavesExactlyOneVisualRoot()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(LivingRoomSceneName);

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null, "The Living Room has no cat.");
        CatBreedCatalog catalog = CatBreedCatalog.Load();

        for (int i = 0; i < catalog.Count; i++)
        {
            Assert.That(CatBreedService.Select(catalog.Get(i).Id), Is.True);
            yield return SettleFrames();

            Animator[] animators = cat.GetComponentsInChildren<Animator>(true);
            Assert.That(animators.Length, Is.EqualTo(1),
                "After swapping to " + catalog.Get(i).Id +
                " the cat carries " + animators.Length + " animators.");

            CatBreedVisualTag[] tags = cat.GetComponentsInChildren<CatBreedVisualTag>(true);
            Assert.That(tags.Length, Is.EqualTo(1),
                "After swapping to " + catalog.Get(i).Id +
                " the cat carries " + tags.Length + " visual roots.");
            Assert.That(tags[0].BreedId, Is.EqualTo(catalog.Get(i).Id),
                "The surviving visual root is tagged as the wrong breed.");
        }
    }

    [UnityTest]
    public IEnumerator SelectionBeforeRuntimeUpdate_KeepsConsumersOnTheVisibleCat()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(LivingRoomSceneName);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>();
        CatBreedRuntimeController runtime = Object.FindAnyObjectByType<CatBreedRuntimeController>();
        Assert.That(runtime, Is.Not.Null);
        yield return SettleFrames();

        CatBreedCatalog catalog = CatBreedCatalog.Load();
        string next = catalog.Get((catalog.IndexOf(CatBreedService.SelectedBreedId) + 1) % catalog.Count).Id;
        Assert.That(CatBreedService.Select(next), Is.True);
        // Reproduce selection arriving before the manager's Update in the same
        // frame, while Unity has not yet completed deferred Destroy calls.
        runtime.SendMessage("Update", SendMessageOptions.RequireReceiver);
        yield return SettleFrames();

        AssertSingleBoundVisual(cat, next);
        AssertAnimates(cat, "selection followed by a same-frame scan");
    }

    [UnityTest]
    public IEnumerator MultipleSelectionsInOneFrame_KeepOnlyTheLatestVisual()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(LivingRoomSceneName);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>();
        yield return SettleFrames();
        CatBreedCatalog catalog = CatBreedCatalog.Load();

        for (int i = 0; i < catalog.Count; i++)
            Assert.That(CatBreedService.Select(catalog.Get(i).Id), Is.True);
        Assert.That(CatBreedService.Select(CatBreedCatalog.DefaultBreedId), Is.True);
        yield return SettleFrames();

        AssertSingleBoundVisual(cat, CatBreedCatalog.DefaultBreedId);
        AssertAnimates(cat, "rapid selections and return to the starting breed");
    }

    [UnityTest]
    public IEnumerator SwappingWhileOwnerIsInactive_AnimatesWhenReactivated()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(LivingRoomSceneName);
        CatMovement cat = Object.FindAnyObjectByType<CatMovement>();
        yield return SettleFrames();
        try
        {
            cat.gameObject.SetActive(false);
            Assert.That(CatBreedService.Select("persian"), Is.True);
            Assert.That(CatBreedService.Select("maine-coon"), Is.True);
            yield return SettleFrames();
        }
        finally
        {
            cat.gameObject.SetActive(true);
        }
        yield return SettleFrames();

        AssertSingleBoundVisual(cat, "maine-coon");
        AssertAnimates(cat, "reactivated cat after breed changes");
    }

    private static void AssertSingleBoundVisual(CatMovement cat, string breedId)
    {
        Animator[] animators = cat.GetComponentsInChildren<Animator>(true);
        Assert.That(animators.Length, Is.EqualTo(1),
            "A retired visual must not survive a breed change.");
        Animator visible = animators[0];
        Assert.That(visible.isActiveAndEnabled, Is.True);
        Assert.That(visible.GetComponentInParent<CatBreedVisualTag>().BreedId,
            Is.EqualTo(breedId));
        foreach (MonoBehaviour consumer in cat.GetComponents<MonoBehaviour>())
        {
            FieldInfo field = consumer.GetType().GetField("animator",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(Animator))
                Assert.That(field.GetValue(consumer), Is.SameAs(visible),
                    consumer.GetType().Name + " must drive the visible cat.");
        }
    }

    /// <summary>
    /// The runtime controller rescans on a timer, so give it more than its
    /// quarter second and let the Animator run real frames afterwards.
    /// </summary>
    private static IEnumerator SettleFrames()
    {
        float until = Time.unscaledTime + .6f;
        while (Time.unscaledTime < until)
            yield return null;
    }

    private static void AssertAnimates(CatMovement cat, string what)
    {
        Assert.That(Animates(cat, out float turned), Is.True,
            what + " did not animate: the probe bone turned " +
            turned.ToString("F3") + " degrees over half a second.");
    }

    /// <summary>
    /// Drives the locomotion blend tree and measures whether the skeleton
    /// actually moves. A cat that slides without animating - the reported
    /// symptom - reads as a bone that never turns.
    /// </summary>
    private static bool Animates(CatMovement cat, out float turned)
    {
        turned = 0f;
        Animator animator = cat.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        Transform bone = null;
        Transform[] all = animator.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].name == ProbeBoneName)
                bone = all[i];
        if (bone == null)
            return false;

        animator.SetFloat(SpeedParameter, 1.4f);
        animator.Update(.05f);
        Quaternion start = bone.localRotation;
        for (int i = 0; i < 10; i++)
            animator.Update(.05f);

        turned = Quaternion.Angle(start, bone.localRotation);
        return turned > AnimatedRotationDegrees;
    }
}
