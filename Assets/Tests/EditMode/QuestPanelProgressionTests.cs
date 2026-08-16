using System.Collections.Generic;
using NUnit.Framework;

using QuestState = CatHome.Quests.QuestState;

/// <summary>
/// EditMode coverage for the read-only data path the CP3 Quest Panel is built on,
/// and for the claim guarantee the panel's Claim button relies on.
///
/// These tests never touch the local save: CatHomeSaveSystem.SaveNow is a no-op
/// until CatHomeSaveSystem.Initialize has run, which only happens in Play Mode.
/// They only move ProgressionService's in-memory static state, which is reset by
/// its SubsystemRegistration hook when Play Mode starts.
/// </summary>
public sealed class QuestPanelProgressionTests
{
    private const string EatQuestId = "level1_eat";
    private const string DrinkQuestId = "level1_drink";
    private const string BallQuestId = "level1_ball";

    private readonly List<QuestSnapshot> snapshots = new List<QuestSnapshot>();

    [SetUp]
    public void SetUp()
    {
        // Belt-and-suspenders: this fixture lives in Assets/Editor and therefore
        // only runs in EditMode, where CatHomeSaveSystem is uninitialized and
        // SaveNow does nothing. The guard makes that guarantee explicit so the
        // claim coverage below can never write the player's local save.
        if (UnityEngine.Application.isPlaying)
            Assert.Ignore("QuestPanelProgressionTests run in EditMode only; the local save is never written.");

        // Level 1, empty wallet, no recorded quest progress.
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
        snapshots.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, new QuestProgressEntry[0]);
    }

    [Test]
    public void ActiveLevel_ExposesTitlesDescriptionsAndRewards()
    {
        QuestBoardStatus status = ProgressionService.CaptureActiveChapterQuests(
            snapshots,
            out string levelName
        );
        RequireConfig(status);

        Assert.AreEqual(QuestBoardStatus.ChapterInProgress, status);
        Assert.AreEqual("First Play", levelName);
        Assert.AreEqual(3, snapshots.Count);

        QuestSnapshot eat = Find(EatQuestId);
        Assert.AreEqual("First Bite", eat.Title);
        Assert.AreEqual("Eat food once", eat.Description);
        Assert.AreEqual(1, eat.RequiredCount);
        Assert.AreEqual(10, eat.RewardCoins);
        Assert.AreEqual(5, eat.RewardBondXp);
        Assert.AreEqual(0, eat.RewardDiamonds);

        Assert.AreEqual("First Sip", Find(DrinkQuestId).Title);
        Assert.AreEqual("Ball Champion", Find(BallQuestId).Title);
    }

    [Test]
    public void QuestWithoutSavedProgress_ReadsAsActiveAtZero()
    {
        RequireConfig(ProgressionService.CaptureActiveChapterQuests(snapshots, out _));

        QuestSnapshot eat = Find(EatQuestId);

        // Not the QuestState.Locked enum default: an unrecorded quest is Active,
        // exactly as ProgressionService.GetOrCreateEntry would create it.
        Assert.AreEqual(QuestState.Active, eat.State);
        Assert.AreEqual(0, eat.Count);
        Assert.IsFalse(eat.IsClaimable);
    }

    [Test]
    public void CompletedAndClaimed_AreDistinguishable()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, new[]
        {
            Entry(EatQuestId, 1, QuestState.Completed),
            Entry(DrinkQuestId, 1, QuestState.Claimed)
        });

        RequireConfig(ProgressionService.CaptureActiveChapterQuests(snapshots, out _));

        QuestSnapshot eat = Find(EatQuestId);
        QuestSnapshot drink = Find(DrinkQuestId);

        Assert.AreEqual(QuestState.Completed, eat.State);
        Assert.IsTrue(eat.IsClaimable, "A Completed quest is the only claimable one.");

        Assert.AreEqual(QuestState.Claimed, drink.State);
        Assert.IsFalse(drink.IsClaimable, "A Claimed quest must never offer a Claim button again.");
    }

    [Test]
    public void AllChaptersCompleted_ReportsCompletionAndNoQuests()
    {
        // Level 4 with the three-level config is the verified "everything done"
        // save state; the panel must show a completion message and no rows.
        ProgressionService.ApplySavedState(55, 0, 28, 4, new QuestProgressEntry[0]);

        QuestBoardStatus status = ProgressionService.CaptureActiveChapterQuests(snapshots, out _);
        if (status == QuestBoardStatus.Unavailable)
            Assert.Ignore("Resources/ProgressionConfig.asset could not be loaded.");

        Assert.AreEqual(QuestBoardStatus.AllChaptersCompleted, status);
        Assert.AreEqual(0, snapshots.Count);
        Assert.AreEqual(55, ProgressionService.Coins);
        Assert.AreEqual(28, ProgressionService.BondXp);
        Assert.AreEqual(0, ProgressionService.Diamonds);
    }

    [Test]
    public void CapturingSnapshots_GrantsNothingAndChangesNoState()
    {
        ProgressionService.ApplySavedState(55, 0, 28, 4, new[]
        {
            Entry(EatQuestId, 1, QuestState.Claimed)
        });

        for (int i = 0; i < 5; i++)
            ProgressionService.CaptureActiveChapterQuests(snapshots, out _);

        Assert.AreEqual(55, ProgressionService.Coins);
        Assert.AreEqual(28, ProgressionService.BondXp);
        Assert.AreEqual(0, ProgressionService.Diamonds);
        Assert.AreEqual(4, ProgressionService.CurrentChapterNumber);

        Assert.IsTrue(ProgressionService.TryGetQuestState(EatQuestId, out QuestState state));
        Assert.AreEqual(QuestState.Claimed, state);
    }

    [Test]
    public void RepeatedClaims_PayExactlyOnce()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, new[]
        {
            Entry(EatQuestId, 1, QuestState.Completed)
        });

        if (ProgressionService.CaptureActiveChapterQuests(snapshots, out _) !=
            QuestBoardStatus.ChapterInProgress)
        {
            Assert.Ignore("Resources/ProgressionConfig.asset could not be loaded.");
        }

        Assert.IsTrue(ProgressionService.TryClaimQuest(EatQuestId), "The first claim must pay.");
        Assert.AreEqual(10, ProgressionService.Coins);
        Assert.AreEqual(5, ProgressionService.BondXp);

        // What a burst of taps on the panel's Claim button would do.
        for (int i = 0; i < 4; i++)
            Assert.IsFalse(ProgressionService.TryClaimQuest(EatQuestId));

        Assert.AreEqual(10, ProgressionService.Coins, "A repeated claim must never pay twice.");
        Assert.AreEqual(5, ProgressionService.BondXp);

        // The level still waits for the second quest, so the panel stays on level 1.
        Assert.AreEqual(1, ProgressionService.CurrentChapterNumber);

        RequireConfig(ProgressionService.CaptureActiveChapterQuests(snapshots, out _));
        Assert.AreEqual(QuestState.Claimed, Find(EatQuestId).State);
        Assert.IsFalse(Find(EatQuestId).IsClaimable);
    }

    [Test]
    public void ClaimingEveryQuest_AdvancesToTheNextChapter()
    {
        ProgressionService.ApplySavedState(0, 0, 0, 1, new[]
        {
            Entry(EatQuestId, 1, QuestState.Completed),
            Entry(DrinkQuestId, 1, QuestState.Completed),
            Entry(BallQuestId, 1, QuestState.Completed)
        });

        if (ProgressionService.CaptureActiveChapterQuests(snapshots, out _) !=
            QuestBoardStatus.ChapterInProgress)
        {
            Assert.Ignore("Resources/ProgressionConfig.asset could not be loaded.");
        }

        Assert.IsTrue(ProgressionService.TryClaimQuest(EatQuestId));
        Assert.AreEqual(1, ProgressionService.CurrentChapterNumber, "Two claims remain, so the chapter must hold.");

        Assert.IsTrue(ProgressionService.TryClaimQuest(DrinkQuestId));
        Assert.AreEqual(1, ProgressionService.CurrentChapterNumber, "The ball quest is still claimable.");

        Assert.IsTrue(ProgressionService.TryClaimQuest(BallQuestId));
        Assert.AreEqual(2, ProgressionService.CurrentChapterNumber);

        // The panel repaints onto the new level after the claim.
        ProgressionService.CaptureActiveChapterQuests(snapshots, out string levelName);
        Assert.AreEqual("Cozy Claws", levelName);
        Assert.AreEqual(2, snapshots.Count);
        Assert.AreEqual("Nap Time", snapshots[0].Title);
    }

    [Test]
    public void LevelOne_FreshProgressClaimUnlockAndReload_RemainsConsistent()
    {
        if (!ProgressionConfig.TryGetActive(out ProgressionConfig config))
            Assert.Ignore("Resources/ProgressionConfig.asset could not be loaded.");

        ProgressionService.RecordProgress(QuestType.Eat);
        ProgressionService.RecordProgress(QuestType.Drink);
        ProgressionService.RecordProgress(QuestType.PlayBall);

        Assert.IsTrue(ProgressionService.TryGetQuestState(EatQuestId, out QuestState eatState));
        Assert.AreEqual(QuestState.Completed, eatState);
        Assert.IsTrue(ProgressionService.TryGetQuestState(DrinkQuestId, out QuestState drinkState));
        Assert.AreEqual(QuestState.Completed, drinkState);
        Assert.IsTrue(ProgressionService.TryGetQuestState(BallQuestId, out QuestState ballState));
        Assert.AreEqual(QuestState.Completed, ballState);
        Assert.AreEqual(1, ProgressionService.CurrentChapterNumber);

        Assert.IsTrue(ProgressionService.TryClaimQuest(EatQuestId));
        Assert.IsTrue(ProgressionService.TryClaimQuest(DrinkQuestId));
        Assert.IsTrue(ProgressionService.TryClaimQuest(BallQuestId));
        Assert.AreEqual(2, ProgressionService.CurrentChapterNumber);
        Assert.AreEqual("sweet-dreams",
            config.GetChapter(ProgressionService.CurrentChapterNumber).LevelId);

        long coins = ProgressionService.Coins;
        long diamonds = ProgressionService.Diamonds;
        long bondXp = ProgressionService.BondXp;
        int chapterNumber = ProgressionService.CurrentChapterNumber;
        QuestProgressEntry[] savedProgress = ProgressionService.CaptureQuestProgress();

        // Simulates a process reload using the exact payload persisted in the
        // canonical save section, without touching the developer's local file.
        ProgressionService.ApplySavedState(
            coins,
            diamonds,
            bondXp,
            chapterNumber,
            savedProgress);

        Assert.AreEqual(2, ProgressionService.CurrentChapterNumber);
        Assert.AreEqual(coins, ProgressionService.Coins);
        Assert.AreEqual(bondXp, ProgressionService.BondXp);
        Assert.IsTrue(ProgressionService.TryGetQuestState(EatQuestId, out eatState));
        Assert.AreEqual(QuestState.Claimed, eatState);
        Assert.IsTrue(ProgressionService.TryGetQuestState(DrinkQuestId, out drinkState));
        Assert.AreEqual(QuestState.Claimed, drinkState);
        Assert.IsTrue(ProgressionService.TryGetQuestState(BallQuestId, out ballState));
        Assert.AreEqual(QuestState.Claimed, ballState);
        Assert.IsFalse(ProgressionService.TryClaimQuest(EatQuestId));
        Assert.IsFalse(ProgressionService.TryClaimQuest(DrinkQuestId));
        Assert.IsFalse(ProgressionService.TryClaimQuest(BallQuestId));
        Assert.AreEqual("sweet-dreams",
            config.GetChapter(ProgressionService.CurrentChapterNumber).LevelId);
    }

    private static QuestProgressEntry Entry(string questId, int count, QuestState state)
    {
        var entry = new QuestProgressEntry { questId = questId, count = count };
        entry.SetState(state);
        return entry;
    }

    private static void RequireConfig(QuestBoardStatus status)
    {
        if (status == QuestBoardStatus.Unavailable)
            Assert.Ignore("Resources/ProgressionConfig.asset could not be loaded.");
    }

    private QuestSnapshot Find(string questId)
    {
        for (int i = 0; i < snapshots.Count; i++)
            if (snapshots[i].QuestId == questId)
                return snapshots[i];

        Assert.Fail($"Quest '{questId}' was not present in the snapshot.");
        return default;
    }
}
