using System;
using CatHome.Economy;
using NUnit.Framework;
using UnityEngine;

public sealed class BondMilestoneAndLoopTests
{
    [SetUp]
    public void SetUp()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, Array.Empty<QuestProgressEntry>());
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
    }

    [TearDown]
    public void TearDown()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, Array.Empty<QuestProgressEntry>());
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
    }

    [Test]
    public void BondMilestones_UnlockInCareOrder()
    {
        Assert.That(BondMilestoneService.Count, Is.EqualTo(4));
        Assert.That(BondMilestoneService.CountReached(0), Is.EqualTo(0));
        Assert.That(
            BondMilestoneService.HasReached(BondMilestoneService.MouseHuntId, 34),
            Is.False);
        Assert.That(
            BondMilestoneService.HasReached(BondMilestoneService.MouseHuntId, 35),
            Is.True);
        Assert.That(BondMilestoneService.CountReached(80), Is.EqualTo(2));
        Assert.That(BondMilestoneService.CountReached(150), Is.EqualTo(3));
        Assert.That(BondMilestoneService.CountReached(250), Is.EqualTo(4));

        Assert.That(
            BondMilestoneService.TryGetNext(0, out BondMilestone first),
            Is.True);
        Assert.That(first.Id, Is.EqualTo(BondMilestoneService.MouseHuntId));
        Assert.That(
            BondMilestoneService.TryGetNext(250, out _),
            Is.False);
        Assert.That(
            BondMilestoneService.FormatNextGiftLabel(79),
            Does.Contain("Window Watch"));
    }

    [Test]
    public void SitLookActivity_UsesBondThresholds()
    {
        var root = new GameObject("BondSitLook");
        try
        {
            SitLookActivity activity = root.AddComponent<SitLookActivity>();
            activity.EditorConfigure(
                "window-watch",
                "WINDOW WATCH",
                CatActivityKind.WindowWatch,
                QuestType.WindowWatch,
                BondMilestoneService.WindowWatchBond,
                "WATCH",
                1.2f,
                6f,
                root.transform,
                null,
                null);
            Assert.That(activity.IsUnlocked, Is.False);

            ProgressionService.ApplySavedState(0, 0, 79, 1, Array.Empty<QuestProgressEntry>());
            Assert.That(activity.IsUnlocked, Is.False);

            ProgressionService.ApplySavedState(0, 0, 80, 1, Array.Empty<QuestProgressEntry>());
            Assert.That(activity.IsUnlocked, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void HomeLoopChapter_TracksRunnerAndStorePurchases()
    {
        if (!ProgressionConfig.TryGetActive(out ProgressionConfig config))
            Assert.Ignore("Resources/ProgressionConfig.asset could not be loaded.");
        if (config.GetChapter(4) == null)
            Assert.Ignore("Home Loop chapter is not in ProgressionConfig yet.");

        ProgressionService.ApplySavedState(0, 0, 0, 4, Array.Empty<QuestProgressEntry>());
        Assert.That(ProgressionService.CurrentChapterNumber, Is.EqualTo(4));

        ProgressionService.RecordProgress(QuestType.PlayRunner);
        Assert.That(
            ProgressionService.TryGetQuestState("level4_runner", out var runnerState),
            Is.True);
        Assert.That(runnerState, Is.EqualTo(CatHome.Quests.QuestState.Completed));

        EconomyService.AddCurrency(
            CurrencyType.Coin,
            HomeStoreService.BallBasketPrice,
            EconomySource.Debug);
        Assert.That(
            HomeStoreService.TryPurchase(HomeStoreService.BallBasketId).Succeeded,
            Is.True);
        Assert.That(
            ProgressionService.TryGetQuestState("level4_shop", out var shopState),
            Is.True);
        Assert.That(shopState, Is.EqualTo(CatHome.Quests.QuestState.Completed));
    }
}
