using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CompanionRestEnergyTests
{
    private HomeStoreSaveState previousStore;
    private bool previousSound;
    private bool prepared;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Companion tests must use the isolated QA save session.");
        previousStore = HomeStoreService.CaptureState();
        previousSound = HomeAudioService.SoundEnabled;
        prepared = true;
        HomeAudioService.SoundEnabled = false;
    }

    [TearDown]
    public void After()
    {
        if (!prepared) return;
        if (CatActivity.Active != null) CatActivity.Active.enabled = false;
        Time.timeScale = 1f;
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(previousStore);
        HomeAudioService.SoundEnabled = previousSound;
        prepared = false;
    }

    [UnityTest]
    public IEnumerator SitAndLoaf_RecoverGraduallyOnlyDuringTheHeldPose()
    {
        yield return Prepare();
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var command = cat.GetComponent<CatCommandActivity>();
        var energy = Object.FindAnyObjectByType<EnergySystem>();
        long bond = ProgressionService.BondXp, coins = ProgressionService.Coins;
        string quests = string.Join("|", ProgressionService.CaptureQuestProgress().Select(JsonUtility.ToJson));

        foreach (var kind in new[] { CatCompanionCommand.Sit, CatCompanionCommand.Loaf })
        {
            energy.ApplySavedValue(20f);
            Assert.That(command.Issue(kind), Is.True, kind.ToString());
            Assert.That(command.IsRecoveringEnergy, Is.False, "Sitting down is an entry animation, not a rest bonus.");
            yield return new WaitForSeconds(.3f);
            Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(20.001f));
            yield return new WaitForSeconds(.6f);
            Assert.That(command.IsRecoveringEnergy, Is.True);
            float started = Time.time, before = energy.CurrentEnergy;
            yield return new WaitForSeconds(3f);
            float expected = (Time.time - started) * CatCommandActivity.RestEnergyPerSecond;
            Assert.That(energy.CurrentEnergy - before, Is.EqualTo(expected).Within(.15f), kind + " should restore gently and continuously.");

            float paused = energy.CurrentEnergy;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(energy.CurrentEnergy, Is.EqualTo(paused).Within(.001f), "Paused rest cannot advance energy.");
            Time.timeScale = 1f;
            Assert.That(command.RequestRestStop(), Is.True);
            Assert.That(command.IsRecoveringEnergy, Is.False);
            before = energy.CurrentEnergy;
            yield return new WaitForSeconds(1.5f);
            Assert.That(command.IsRunning, Is.False);
            Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(before + .001f), "Getting up must not add a completion bonus.");
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        }
        Assert.That(ProgressionService.BondXp, Is.EqualTo(bond));
        Assert.That(ProgressionService.Coins, Is.EqualTo(coins));
        Assert.That(string.Join("|", ProgressionService.CaptureQuestProgress().Select(JsonUtility.ToJson)), Is.EqualTo(quests), "Companion rest is not a repeatable quest reward.");
    }

    [UnityTest]
    public IEnumerator MeowNeverRecoversEnergy_AndRestCapsAtOneHundred()
    {
        yield return Prepare();
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var command = cat.GetComponent<CatCommandActivity>();
        var energy = Object.FindAnyObjectByType<EnergySystem>();
        energy.ApplySavedValue(40f);
        Assert.That(command.Issue(CatCompanionCommand.Meow), Is.True);
        yield return new WaitForSeconds(4.6f);
        Assert.That(command.IsRecoveringEnergy, Is.False);
        Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(40f));
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        energy.ApplySavedValue(99.9f);
        Assert.That(command.Issue(CatCompanionCommand.Loaf), Is.True);
        yield return new WaitForSeconds(2f);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(100f));
        command.enabled = false;
        Assert.That(command.IsRecoveringEnergy, Is.False, "Cancellation must end recovery immediately.");
        yield return new WaitForSeconds(.3f);
        Assert.That(energy.CurrentEnergy, Is.LessThan(100f));
    }

    private IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p => HomeStoreService.IsLivingRoomCollectionProduct(p.Id)).Select(p => p.Id).ToArray();
        HomeStoreService.ApplySavedState(state);
        yield return null;
        var cat = Object.FindAnyObjectByType<CatMovement>();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        cat.transform.SetPositionAndRotation(new Vector3(-.5f, .05f, -1.4f), Quaternion.Euler(0f, 180f, 0f));
        controller.enabled = true;
        Physics.SyncTransforms();
    }
}
