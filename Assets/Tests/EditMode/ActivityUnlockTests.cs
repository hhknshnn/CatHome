using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using CatHome.Economy;

public sealed class ActivityUnlockTests
{
    private Scene scene;
    private BallChaseActivity ball;
    private ScratchPostActivity scratch;
    private MouseHuntActivity mouse;
    private SitLookActivity windowWatch;

    [SetUp]
    public void SetUp()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        scene = EditorSceneManager.OpenScene(
            SceneArchitectureBuilder.LevelScenePath,
            OpenSceneMode.Additive);
        ball = FindInScene<BallChaseActivity>();
        scratch = FindInScene<ScratchPostActivity>();
        mouse = FindInScene<MouseHuntActivity>();
        windowWatch = FindWindowWatch();
    }

    [TearDown]
    public void TearDown()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        if (scene.IsValid())
            EditorSceneManager.CloseScene(scene, true);
    }

    [Test]
    public void RoomProducts_StayHiddenUntilPurchasedWithCoins()
    {
        Assert.That(ball, Is.Not.Null);
        Assert.That(scratch, Is.Not.Null);
        Assert.That(mouse, Is.Not.Null);
        Assert.That(windowWatch, Is.Not.Null);
        Assert.That(windowWatch.RequiredBondXp, Is.EqualTo(BondMilestoneService.WindowWatchBond));

        Assert.That(ball.IsUnlocked, Is.False);
        Assert.That(scratch.IsUnlocked, Is.False);
        Assert.That(mouse.IsUnlocked, Is.False);
        Assert.That(windowWatch.IsUnlocked, Is.False);
        Assert.That(ball.IsContentVisible, Is.False);
        Assert.That(scratch.IsContentVisible, Is.False);
        Assert.That(mouse.IsContentVisible, Is.False);

        ProgressionService.ApplySavedState(0, 0, 35, 1, new QuestProgressEntry[0]);
        ball.RefreshUnlockPresentation();
        scratch.RefreshUnlockPresentation();
        mouse.RefreshUnlockPresentation();
        Assert.That(ball.IsUnlocked, Is.False, "Bond must not bypass store ownership.");
        Assert.That(scratch.IsUnlocked, Is.False, "Bond must not bypass store ownership.");
        Assert.That(mouse.IsUnlocked, Is.True);
        Assert.That(mouse.IsContentVisible, Is.True);
        windowWatch.RefreshUnlockPresentation();
        Assert.That(windowWatch.IsUnlocked, Is.False, "Window watch waits for 80 Bond.");

        ProgressionService.ApplySavedState(0, 0, BondMilestoneService.WindowWatchBond, 1, new QuestProgressEntry[0]);
        windowWatch.RefreshUnlockPresentation();
        Assert.That(windowWatch.IsUnlocked, Is.True);

        long totalPrice = HomeStoreService.BallBasketPrice + HomeStoreService.ScratchPostPrice;
        EconomyService.AddCurrency(CurrencyType.Coin, totalPrice, EconomySource.Debug);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.BallBasketId).Succeeded, Is.True);
        Assert.That(HomeStoreService.TryPurchase(HomeStoreService.ScratchPostId).Succeeded, Is.True);
        // EditMode scene loading does not reliably invoke the full runtime
        // OnEnable subscription lifecycle, so repaint explicitly here.
        ball.RefreshUnlockPresentation();
        scratch.RefreshUnlockPresentation();
        Assert.That(ball.IsUnlocked, Is.True);
        Assert.That(scratch.IsUnlocked, Is.True);
        Assert.That(ball.IsContentVisible, Is.True);
        Assert.That(scratch.IsContentVisible, Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(0));
    }

    [Test]
    public void EnergyCost_CannotOverdraw()
    {
        var root = new GameObject("EnergyTest");
        EnergySystem energy = root.AddComponent<EnergySystem>();
        energy.ApplySavedValue(9f);

        Assert.That(energy.TrySpendEnergy(10f), Is.False);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(9f));
        Assert.That(energy.TrySpendEnergy(8f), Is.True);
        Assert.That(energy.CurrentEnergy, Is.EqualTo(1f));

        Object.DestroyImmediate(root);
    }

    private T FindInScene<T>() where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }

    private SitLookActivity FindWindowWatch()
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            SitLookActivity[] activities = root.GetComponentsInChildren<SitLookActivity>(true);
            for (int i = 0; i < activities.Length; i++)
            {
                if (activities[i] != null && activities[i].Kind == CatActivityKind.WindowWatch)
                    return activities[i];
            }
        }

        return null;
    }
}
