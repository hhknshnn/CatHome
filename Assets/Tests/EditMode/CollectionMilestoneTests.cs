using System.Collections.Generic;
using CatHome.Economy;
using NUnit.Framework;

public sealed class CollectionMilestoneTests
{
    [SetUp]
    public void SetUp()
    {
        CollectionMilestoneService.ClearAll();
        EconomyService.ApplyLegacyBalances(0, 0);
        ProgressionService.ApplySavedState(0, 0, 0, 1, System.Array.Empty<QuestProgressEntry>());
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        AchievementService.ApplySavedState(new AchievementSaveState
        {
            achievementVersion = AchievementService.SaveVersion,
            unlockedIds = new[]
            {
                AchievementService.FirstRunId,
                AchievementService.FirstShopId,
                AchievementService.HomeLevel3Id,
                AchievementService.HomeLevel5Id,
                AchievementService.Bond80Id,
                AchievementService.Bond250Id,
                AchievementService.LoginStreak7Id
            }
        });
    }

    [TearDown]
    public void TearDown()
    {
        CollectionMilestoneService.ClearAll();
        EconomyService.ApplyLegacyBalances(0, 0);
        HomeStoreService.ApplySavedState(HomeStoreSaveState.CreateDefault());
        HomeProgressionService.ApplySavedState(HomeProgressionSaveState.CreateDefault());
        AchievementService.ApplySavedState(AchievementSaveState.CreateDefault());
        ProgressionService.ApplySavedState(0, 0, 0, 1, System.Array.Empty<QuestProgressEntry>());
    }

    [Test]
    public void FormatOwnedLabel_UsesCatalogSize()
    {
        Assert.That(CollectionMilestoneService.CatalogSize, Is.EqualTo(114));
        Assert.That(CollectionMilestoneService.FormatOwnedLabel(),
            Is.EqualTo("0 OF 114 • COLLECTED"));
    }

    [Test]
    public void CompletingARoom_DoesNotPayUntilCollect()
    {
        IReadOnlyList<string> living = HomeStoreService.LivingRoomCollection;
        Assert.That(living.Count, Is.EqualTo(10));

        for (int i = 0; i < living.Count; i++)
        {
            if (HomeStoreService.IsOwned(living[i]))
                continue;
            Assert.That(HomeStoreService.TryAcquireForTesting(living[i]).Succeeded, Is.True,
                living[i]);
            if (i < living.Count - 1)
                Assert.That(CollectionMilestoneService.TryPeekPending(out _), Is.False);
        }
        Assert.That(CollectionMilestoneService.TryPeekPending(out CollectionMilestone pending),
            Is.True);
        Assert.That(pending.Id, Is.EqualTo(CollectionMilestoneService.LivingRoomId));
        Assert.That(CollectionMilestoneService.IsClaimed(pending.Id), Is.False);

        long coinsAfterAcquire = EconomyService.Coins;
        long bondAfterAcquire = ProgressionService.BondXp;
        Assert.That(CollectionMilestoneService.TryClaim(pending.Id), Is.True);
        Assert.That(EconomyService.Coins, Is.EqualTo(coinsAfterAcquire + pending.Coins));
        Assert.That(ProgressionService.BondXp, Is.EqualTo(bondAfterAcquire + pending.BondXp));
        Assert.That(CollectionMilestoneService.IsClaimed(pending.Id), Is.True);
        Assert.That(CollectionMilestoneService.TryPeekPending(out _), Is.False);
    }

    [Test]
    public void EmptyProductId_DoesNotEnqueue()
    {
        CollectionMilestoneService.HandleOwned("");
        CollectionMilestoneService.HandleOwned(null);
        Assert.That(CollectionMilestoneService.TryPeekPending(out _), Is.False);
    }

    [Test]
    public void CatalogClaim_FailsUntilEverythingIsOwned()
    {
        Assert.That(CollectionMilestoneService.TryClaim(CollectionMilestoneService.CatalogId),
            Is.False);
        Assert.That(EconomyService.Coins, Is.Zero);
        Assert.That(EconomyService.Diamonds, Is.Zero);
    }
}
