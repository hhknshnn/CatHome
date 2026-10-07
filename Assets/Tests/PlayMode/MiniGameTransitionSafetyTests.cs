using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CatHome.Economy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class MiniGameTransitionSafetyTests
{
    private DeferredAdProvider provider;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        for (int frame = 0; frame < 360; frame++)
        {
            var loader = Object.FindAnyObjectByType<LevelLoader>();
            if (loader != null && loader.IsReady)
                break;
            yield return null;
        }
        Assert.That(Object.FindAnyObjectByType<LevelLoader>().IsReady, Is.True);
        var now = DateTime.UtcNow;
        RunnerEnergyService.ApplySavedState(new RunnerEnergySaveState
        {
            energy = 5, regenerationAnchorUtc = now.ToString("O"),
            rewardedAdsDayUtc = now.ToString("yyyy-MM-dd"), unlimitedUntilUtc = ""
        }, now);
        var progress = CatRunnerProgressSaveState.CreateDefault(now);
        progress.tutorialCompleted = true;
        progress.soundEnabled = false;
        progress.hapticsEnabled = false;
        CatRunnerProgressService.ApplySavedState(progress, now);
        CatchLivesService.ApplySavedState(null, now);
        CatchLivesService.CompleteTutorial();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        if (provider != null)
            CatRunnerRewardedAdBridge.Unregister(provider);
    }

    [UnityTest]
    public IEnumerator Catch_BackgroundPausePreservesAirbornePounceAndTimer()
    {
        yield return LaunchCatch();
        var game = Object.FindAnyObjectByType<CatCatchGameController>();
        var player = Object.FindAnyObjectByType<CatCatchPlayer>();
        game.StartHunt();
        float floor = player.Position.y;
        float deadline = Time.time + 12f;
        while ((!player.IsPouncing || player.Position.y < floor + .025f) && Time.time < deadline)
        {
            if (!player.IsBusy && player.Prey == null)
            {
                var prey = Object.FindObjectsByType<CatCatchMouse>()
                    .Where(m => m.IsCatchable)
                    .OrderBy(m => (m.transform.position - player.Position).sqrMagnitude).FirstOrDefault();
                if (prey != null)
                    player.ChasePrey(prey);
            }
            if(player.CanPounce)player.RequestPounce();
            yield return null;
        }
        Assert.That(player.IsPouncing, Is.True);
        Assert.That(player.Position.y, Is.GreaterThan(floor + .025f));
        Vector3 position = player.Position;
        float remaining = Read<float>(game, "remaining");
        float phaseTimer = Read<float>(player, "phaseTimer");
        Invoke(game, "OnApplicationPause", true);
        Assert.That(game.IsPaused, Is.True);
        Assert.That(player.IsPouncing, Is.True, "Pausing must not erase an airborne hunt phase.");
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(player.Position, Is.EqualTo(position));
        Assert.That(Read<float>(game, "remaining"), Is.EqualTo(remaining));
        Assert.That(Read<float>(player, "phaseTimer"), Is.EqualTo(phaseTimer));
        game.ResumeHunt();
        Assert.That(player.IsPouncing, Is.True);
        deadline = Time.time + 2f;
        while (player.IsBusy && Time.time < deadline)
            yield return null;
        Assert.That(player.IsBusy, Is.False, "The same pounce must land and recover after resume.");
        Assert.That(game.StrikesResolved, Is.EqualTo(1), "Resume must resolve the committed strike exactly once.");
        Assert.That(player.Position.y, Is.EqualTo(floor).Within(.03f));
    }

    [UnityTest]
    public IEnumerator Catch_ExitRejectsQueuedRetryAndDuplicateExit()
    {
        yield return LaunchCatch();
        var game = Object.FindAnyObjectByType<CatCatchGameController>();
        game.StartHunt();
        int lives = CatchLivesService.CurrentLives;
        game.PauseHunt();
        game.ExitFromPause();
        game.StartHunt();
        game.ExitToHome();
        Assert.That(game.IsHunting, Is.False);
        Assert.That(CatchLivesService.CurrentLives, Is.EqualTo(lives),
            "A click queued after exit must not spend another hunt life.");
        yield return WaitUntilUnloaded(CatCatchLauncher.CatchSceneName);
        Assert.That(Time.timeScale, Is.EqualTo(1f));
        Assert.That(Object.FindObjectsByType<Camera>()
            .Count(c => c.enabled && c.targetTexture == null), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator Launchers_CompetingClicksLoadOnlyOneMiniGame()
    {
        Assert.That(CatRunnerSessionContext.TryBeginLaunch("MissingMiniGameScene"), Is.False);
        Assert.That(CatRunnerSessionContext.IsLaunching, Is.False,
            "A missing build scene must never capture the shared launch reservation.");
        var inactive = new GameObject("InactiveMiniGameLaunchers");
        inactive.SetActive(false);
        inactive.AddComponent<CatRunnerLauncher>().Launch();
        inactive.AddComponent<CatCatchLauncher>().Launch();
        Assert.That(CatRunnerSessionContext.IsLaunching, Is.False,
            "Inactive launchers cannot start a coroutine and must not capture the reservation.");
        Object.Destroy(inactive);
        yield return null;
        var runner = Object.FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include);
        var catcher = Object.FindAnyObjectByType<CatCatchLauncher>(FindObjectsInactive.Include);
        runner.Launch();
        Assert.That(CatRunnerSessionContext.IsLaunching, Is.True);
        catcher.Launch();
        runner.Launch();
        yield return WaitUntilLoaded(CatRunnerLauncher.RunnerSceneName);
        Assert.That(SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName).IsValid(), Is.False);
        Assert.That(Object.FindObjectsByType<CatRunnerGameController>().Length, Is.EqualTo(1));
        catcher.Launch();
        Assert.That(SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName).IsValid(), Is.False);
        Object.FindAnyObjectByType<CatRunnerGameController>().ExitFromWelcome();
        yield return WaitUntilUnloaded(CatRunnerLauncher.RunnerSceneName);
        yield return LaunchCatch();
        Assert.That(CatRunnerSessionContext.IsLaunching, Is.False, "Completed launch must release its reservation.");
    }

    [UnityTest]
    public IEnumerator Runner_CountdownPausesAndStaleActionsCannotSpendLivesOrResetRun()
    {
        yield return LaunchRunner();
        var game = Object.FindAnyObjectByType<CatRunnerGameController>();
        Object.FindAnyObjectByType<CatRunnerTrackManager>().enabled = false;
        game.StartFromWelcome();
        int lives = RunnerEnergyService.CurrentEnergy;
        game.StartFromWelcome();
        Invoke(game, "CollectAndRetry");
        Assert.That(RunnerEnergyService.CurrentEnergy, Is.EqualTo(lives));
        Invoke(game, "OnApplicationPause", true);
        Assert.That(game.IsPaused, Is.True, "Backgrounding during countdown must freeze the start.");
        yield return new WaitForSecondsRealtime(.15f);
        Assert.That(game.IsRunning, Is.False);
        game.ResumeRun();
        yield return new WaitForSeconds(2.6f);
        Assert.That(game.IsGameplayActive, Is.True);
        game.RegisterCoin();
        int score = game.CurrentScore;
        Invoke(game, "CollectAndRetry");
        game.StartFromWelcome();
        Assert.That(game.IsGameplayActive, Is.True);
        Assert.That(game.CurrentScore, Is.EqualTo(score));
        Assert.That(RunnerEnergyService.CurrentEnergy, Is.EqualTo(lives));
        Invoke(game, "CompleteRun");
        yield return null;
        game.StartFromWelcome();
        Assert.That(game.IsRunning, Is.False, "A hidden welcome action must not bypass result settlement.");
        Assert.That(RunnerEnergyService.CurrentEnergy, Is.EqualTo(lives));
    }

    [UnityTest]
    public IEnumerator Runner_DelayedDoubleRewardPaysAdvertisedRoundOnlyOnce()
    {
        yield return LaunchRunner();
        var game = Object.FindAnyObjectByType<CatRunnerGameController>();
        Object.FindAnyObjectByType<CatRunnerTrackManager>().enabled = false;
        game.StartFromWelcome();
        yield return new WaitForSeconds(2.6f);
        for (int i = 0; i < 3; i++) game.RegisterCoin();
        Invoke(game, "CompleteRun");
        yield return null;
        yield return null;
        CatRunnerResult first = Read<CatRunnerResult>(game, "latestResult");
        provider = new DeferredAdProvider();
        CatRunnerRewardedAdBridge.Register(provider);
        Invoke(game, "RequestDoubleCoinsAd");
        Assert.That(provider.Completed, Is.Not.Null);
        Invoke(game, "CollectAndRetry");
        yield return new WaitForSeconds(2.6f);
        for (int i = 0; i < 9; i++) game.RegisterCoin();
        Invoke(game, "CompleteRun");
        yield return null;
        yield return null;
        Assert.That(Read<CatRunnerResult>(game, "latestResult").RunId, Is.Not.EqualTo(first.RunId));
        long before = EconomyService.Coins;
        provider.Completed(true);
        Assert.That(EconomyService.Coins - before, Is.EqualTo(first.TotalCoins),
            "The delayed provider callback must not double the newer, larger round.");
        Assert.That(Read<bool>(game, "doubleCoinsGranted"), Is.False,
            "The second round still owns its separate optional bonus.");
        provider.Completed(true);
        Assert.That(EconomyService.Coins - before, Is.EqualTo(first.TotalCoins));
    }

    [UnityTest]
    public IEnumerator Catch_RoundEndsMidPounce_GroundsOnceAndRejectsLateStrike()
    {
        yield return LaunchCatch();
        var game = Object.FindAnyObjectByType<CatCatchGameController>();
        var player = Object.FindAnyObjectByType<CatCatchPlayer>();
        game.StartHunt();
        float floor = player.Position.y;
        float deadline = Time.time + 16f;
        while ((game.Catches < 1 || !player.IsPouncing || player.Position.y < floor + .025f) &&
               Time.time < deadline)
        {
            if (!player.IsBusy && player.Prey == null)
            {
                var prey = Object.FindObjectsByType<CatCatchMouse>()
                    .Where(m => m.IsCatchable)
                    .OrderBy(m => (m.transform.position - player.Position).sqrMagnitude).FirstOrDefault();
                if (prey != null) player.ChasePrey(prey);
            }
            if(player.CanPounce)player.RequestPounce();
            yield return null;
        }
        Assert.That(game.Catches, Is.GreaterThanOrEqualTo(1), "Use a real earned payout to detect duplicate settlement.");
        Assert.That(player.IsPouncing, Is.True);
        Assert.That(player.Position.y, Is.GreaterThan(floor + .025f));
        Vector3 airborneAt = player.Position;
        int catches = game.Catches;
        int score = game.Score;
        long coinsBefore = EconomyService.Coins;
        typeof(CatCatchGameController).GetField("remaining", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(game, 0f);
        Invoke(game, "Update"); // The normal timer-expiry path, in this airborne pose.
        Assert.That(game.IsHunting, Is.False);
        Assert.That(player.IsPouncing || player.IsBusy, Is.False);
        Assert.That(player.Position.y, Is.EqualTo(floor).Within(.001f));
        Assert.That(player.Position.x, Is.EqualTo(airborneAt.x).Within(.001f));
        Assert.That(player.Position.z, Is.EqualTo(airborneAt.z).Within(.001f));
        Assert.That(Read<bool>(player, "inputEnabled"), Is.False);
        Assert.That(Read<GameObject>(game, "resultPanel").activeSelf, Is.True);
        Assert.That(player.TryConsumeStrike(out _, out _), Is.False, "Cancelled air motion cannot resolve a late catch.");
        long payout = CatchScoring.CoinsForCatches(catches);
        Assert.That(EconomyService.Coins - coinsBefore, Is.EqualTo(payout));
        Invoke(game, "Update");
        Invoke(game, "EndHunt");
        yield return new WaitForSeconds(.2f);
        Assert.That(game.Catches, Is.EqualTo(catches));
        Assert.That(game.Score, Is.EqualTo(score));
        Assert.That(EconomyService.Coins - coinsBefore, Is.EqualTo(payout));
        Assert.That(player.Position.y, Is.EqualTo(floor).Within(.001f));
    }

    [UnityTest]
    public IEnumerator Runner_ThirdAirborneHitEndsOnRealPlatform_AndRetryResetsOnce()
    {
        yield return LaunchRunner();
        var game = Object.FindAnyObjectByType<CatRunnerGameController>();
        var player = Object.FindAnyObjectByType<CatRunnerPlayer>();
        var track = Object.FindAnyObjectByType<CatRunnerTrackManager>();
        track.enabled = false;
        game.StartFromWelcome();
        yield return new WaitForSeconds(2.6f);
        var platformTemplate = player.transform.root.GetComponentsInChildren<CatRunnerTrackObject>(true)
            .First(item => item.Kind == CatRunnerTrackObjectKind.Platform);
        var platform = Object.Instantiate(platformTemplate, platformTemplate.transform.parent);
        platform.transform.localPosition = Vector3.zero;
        platform.gameObject.SetActive(true);
        Read<System.Collections.Generic.List<CatRunnerTrackObject>>(track, "activeObjects").Add(platform);
        float support = track.SampleSurfaceHeightAt(player.LanePosition, 0f);
        Assert.That(support, Is.GreaterThan(.5f), "Use a real raised platform, not a hard-coded floor height.");
        player.SetTrackSurfaceHeight(support);
        yield return new WaitForSeconds(.2f);
        game.RegisterObstacleHit();
        yield return new WaitForSeconds(1.6f);
        game.RegisterObstacleHit();
        yield return new WaitForSeconds(1.6f);
        Assert.That(game.CollisionCount, Is.EqualTo(2));
        Invoke(player, "Jump");
        yield return new WaitForSeconds(.12f);
        Assert.That(player.IsAirborne, Is.True);
        Vector3 airborneAt = player.transform.localPosition;
        float airborneHeight = player.JumpHeight;
        game.PauseRun();
        yield return new WaitForSecondsRealtime(.1f);
        Assert.That(player.transform.localPosition, Is.EqualTo(airborneAt));
        Assert.That(player.JumpHeight, Is.EqualTo(airborneHeight), "Pause must preserve the jump that a final hit cancels.");
        game.ResumeRun();
        game.RegisterObstacleHit();
        Assert.That(game.CollisionCount, Is.EqualTo(3));
        Assert.That(game.IsRunning || player.IsAirborne || player.IsSliding, Is.False);
        Assert.That(player.JumpHeight, Is.Zero);
        Assert.That(player.SurfaceHeight, Is.EqualTo(support).Within(.001f));
        Assert.That(player.transform.localPosition.y, Is.EqualTo(support).Within(.001f));
        Assert.That(player.transform.localPosition.x, Is.EqualTo(airborneAt.x).Within(.001f));
        Assert.That(player.transform.localPosition.z, Is.EqualTo(airborneAt.z).Within(.001f));
        Assert.That(Read<GameObject>(game, "resultPanel").activeSelf, Is.True);
        yield return null;
        yield return null;
        int lives = RunnerEnergyService.CurrentEnergy;
        Invoke(game, "CollectAndRetry");
        Invoke(game, "CollectAndRetry");
        Assert.That(RunnerEnergyService.CurrentEnergy, Is.EqualTo(lives - 1));
        Assert.That(player.JumpHeight, Is.Zero);
        Assert.That(player.SurfaceHeight, Is.Zero, "A retry must not inherit the previous platform support.");
        Assert.That(game.CollisionCount, Is.Zero);
        yield return new WaitForSeconds(2.6f);
        Assert.That(game.IsGameplayActive, Is.True);
        Assert.That(player.IsAirborne, Is.False);
    }

    [UnityTest]
    public IEnumerator EveryRoom_BothWelcomeReturnsCancelRestAndCare_WithoutSpendingLives()
    {
        Assert.That(EditorQaSession.IsActive, Is.True,
            "The room/game transition matrix must use the isolated QA save.");
        HomeStoreSaveState savedStore = HomeStoreService.CaptureState();
        HomeProgressionSaveState savedProgression = HomeProgressionService.CaptureState();
        bool hadOnboarding = PlayerPrefs.HasKey(PetTutorialHint.OnboardingCompletedKey);
        int savedOnboarding = PlayerPrefs.GetInt(PetTutorialHint.OnboardingCompletedKey);
        try
        {
            PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, 1);
            foreach (var title in Object.FindObjectsByType<TitleScreen>(FindObjectsInactive.Include))
                title.gameObject.SetActive(false);
            foreach (var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include))
            {
                popup.Close();
                popup.enabled = false;
            }
            foreach (var popup in Object.FindObjectsByType<CollectionCompleteCelebrationView>(FindObjectsInactive.Include))
                popup.enabled = false;
            foreach (var popup in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include))
                popup.enabled = false;

            var loader = Object.FindAnyObjectByType<LevelLoader>();
            HomeProgressionService.ApplySavedState(new HomeProgressionSaveState
            {
                homeXp = HomeProgressionService.CumulativeXpForLevel(30)
            });
            var owned = HomeStoreSaveState.CreateDefault();
            owned.currentRoomId = loader.CurrentRoom.Id;
            // The current 97 ROOM/CAT items plus room ownership, never retired
            // historical furniture. Store all CAT toys to leave clear spawns.
            owned.ownedProductIds = HomeStoreService.Products.Where(p =>
                CatCollectionPolicy.IsCatItem(p.Id) || HomeRoomService.Rooms.Any(r =>
                    r.RequiredOwnershipId == p.Id || HomeStoreService.IsProductInRoomCollection(r.Id, p.Id)))
                .Select(p => p.Id).ToArray();
            owned.storedProductIds = owned.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
            HomeStoreService.ApplySavedState(owned);
            yield return null;
            yield return null;

            string initial = loader.CurrentRoom.Id;
            string[] roomIds = HomeRoomService.Rooms.Where(r => r.Id != initial)
                .Select(r => r.Id).Concat(new[] { initial }).ToArray();
            Assert.That(roomIds.Length, Is.EqualTo(8));
            foreach (string roomId in roomIds)
            {
                yield return ChangeRoomRejectingStaleLaunches(loader, roomId);
                foreach (string miniGame in new[] { CatRunnerLauncher.RunnerSceneName, CatCatchLauncher.CatchSceneName })
                {
                    var cat = Object.FindAnyObjectByType<CatMovement>();
                    CatActionState.CancelForTransition(cat);
                    var spawn = Object.FindObjectsByType<LevelSpawnPoint>()
                        .First(p => p.gameObject.scene == cat.gameObject.scene &&
                                    p.SpawnPointId == loader.CurrentRoom.SpawnPointId);
                    MoveHomeCat(cat, spawn.transform.position, spawn.transform.rotation);
                    RoomPlayModeSupport.ProvisionNeeds();
                    var command = cat.GetComponent<CatCommandActivity>() ?? cat.gameObject.AddComponent<CatCommandActivity>();
                    Assert.That(command.Issue(CatCompanionCommand.Sit), Is.True, roomId + "/" + miniGame);
                    float deadline = Time.realtimeSinceStartup + 5f;
                    while (!command.IsWaitingForRestStop && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    Assert.That(command.IsWaitingForRestStop, Is.True);
                    yield return WelcomeRoundTrip(loader, cat, miniGame);
                    Assert.That(command.IsRunning, Is.False, "A cancelled rest must not resume after returning home.");
                }
            }

            // These are the home's actual visible bowls; other rooms' empty
            // anchors are deliberately not manufactured into care targets.
            yield return ChangeRoomRejectingStaleLaunches(loader, HomeRoomService.LivingRoomId);
            foreach (string miniGame in new[] { CatRunnerLauncher.RunnerSceneName, CatCatchLauncher.CatchSceneName })
            foreach (string kind in new[] { "water", "food" })
            {
                var cat = Object.FindAnyObjectByType<CatMovement>();
                CatActionState.CancelForTransition(cat);
                RoomPlayModeSupport.ProvisionNeeds();
                var bowls = cat.GetComponent<BowlInteraction>();
                bowls.ResolveSceneReferences();
                var bowl = Read<BowlInteraction.BowlSetup>(bowls, kind);
                bowl.Fill();
                // Use a genuinely reachable heading after the accepted room/bowl revisions.
                var target=bowl.ContactPoint;var outward=bowl.InteractionPoint.position-target.position;outward.y=0;outward.Normalize();
                bool ready=false;
                foreach(float radius in new[]{.30f,.34f,.38f,.42f,.46f,.50f})
                {
                    foreach(float angle in new[]{0f,15f,-15f,30f,-30f,60f,-60f})
                    {
                        var side=Quaternion.Euler(0,angle,0)*outward;var point=target.position+side*radius;point.y=.05f;
                        MoveHomeCat(cat,point,Quaternion.LookRotation(-side));Invoke(bowls,"Update");
                        ready=CatMealHeadMotion.TryPrepareBowlPose(cat,target,out _)&&CatMealHeadMotion.TryPrepareCareStart(cat,target,out _)&&ReferenceEquals(Read<BowlInteraction.BowlSetup>(bowls,"currentBowl"),bowl);
                        if(ready)break;
                    }
                    if(ready)break;
                }
                Assert.That(ready,Is.True,"The actual care target must be reachable before testing mini-game cancellation.");
                Assert.That(Read<BowlInteraction.BowlSetup>(bowls, "currentBowl"), Is.SameAs(bowl), kind);
                Assert.That(bowls.HasVisibleAction, Is.True, kind);
                Read<UnityEngine.UI.Button>(bowls, "interactionButton").onClick.Invoke();
                Assert.That(bowls.IsInteracting, Is.True, kind);
                var thirst = Object.FindAnyObjectByType<ThirstSystem>();
                var hunger = Object.FindAnyObjectByType<HungerSystem>();
                float deadline = Time.realtimeSinceStartup + 6f;
                while (!(kind == "water" ? thirst.IsDrinking : hunger.IsEating) &&
                       Time.realtimeSinceStartup < deadline)
                    yield return null;
                Assert.That(kind == "water" ? thirst.IsDrinking : hunger.IsEating, Is.True,
                    kind + " must reach actual need recovery before launching " + miniGame);
                yield return new WaitForSeconds(.2f);
                yield return WelcomeRoundTrip(loader, cat, miniGame);
                Assert.That(bowls.IsInteracting, Is.False);
                Assert.That(bowls.ActiveCareSound, Is.Null.Or.Empty);
                Assert.That(thirst.IsDrinking || hunger.IsEating, Is.False);
            }
        }
        finally
        {
            CatActionState.CancelForTransition(Object.FindAnyObjectByType<CatMovement>());
            HomeProgressionService.ApplySavedState(savedProgression);
            HomeStoreService.ApplySavedState(savedStore);
            if (hadOnboarding) PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, savedOnboarding);
            else PlayerPrefs.DeleteKey(PetTutorialHint.OnboardingCompletedKey);
        }
    }

    private static IEnumerator ChangeRoomRejectingStaleLaunches(LevelLoader loader, string roomId)
    {
        if (loader.CurrentRoom.Id == roomId)
            yield break;
        bool started = false;
        Action<HomeRoomDefinition> onStarted = _ =>
        {
            started = true;
            Object.FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include).Launch();
            Object.FindAnyObjectByType<CatCatchLauncher>(FindObjectsInactive.Include).Launch();
            Assert.That(CatRunnerSessionContext.IsLaunching, Is.False,
                "Neither launcher may reserve a game from the RoomLoadStarted callback.");
            Assert.That(SceneManager.GetSceneByName(CatRunnerLauncher.RunnerSceneName).IsValid(), Is.False);
            Assert.That(SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName).IsValid(), Is.False);
        };
        loader.RoomLoadStarted += onStarted;
        try
        {
            Assert.That(loader.LoadRoom(roomId), Is.True, roomId);
            Assert.That(started, Is.True);
            float deadline = Time.realtimeSinceStartup + 30f;
            while ((!loader.IsReady || loader.IsTransitioning) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(loader.IsReady && !loader.IsTransitioning, Is.True, roomId);
            Assert.That(loader.CurrentRoom.Id, Is.EqualTo(roomId));
            yield return null;
        }
        finally
        {
            loader.RoomLoadStarted -= onStarted;
        }
    }

    private static IEnumerator WelcomeRoundTrip(LevelLoader loader, CatMovement cat, string miniGame)
    {
        string roomId = loader.CurrentRoom.Id;
        string roomScene = loader.CurrentRoom.SceneName;
        int runnerLives = RunnerEnergyService.CurrentEnergy;
        int catchLives = CatchLivesService.CurrentLives;
        var hunger = Object.FindAnyObjectByType<HungerSystem>();
        var thirst = Object.FindAnyObjectByType<ThirstSystem>();
        var energy = Object.FindAnyObjectByType<EnergySystem>();
        float maximumHunger = hunger.CurrentHunger;
        float maximumThirst = thirst.CurrentThirst;
        float maximumEnergy = energy.CurrentEnergy;
        if (miniGame == CatRunnerLauncher.RunnerSceneName)
            Object.FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include).Launch();
        else
            Object.FindAnyObjectByType<CatCatchLauncher>(FindObjectsInactive.Include).Launch();
        Assert.That(CatActivity.Active, Is.Null, "Cancel the old action before the first load yield.");
        Assert.That(CatActionState.IsBusy(cat), Is.False);
        Assert.That(thirst.IsDrinking || hunger.IsEating, Is.False, "Care recovery must stop before loading.");
        Assert.That(cat.IsMovementLocked && cat.AreWorldActionsBlocked, Is.True,
            "Loading a mini-game must also block the hidden home cat's keyboard and world actions.");
        Vector3 stoppedAt = cat.transform.position;
        string otherRoom = HomeRoomService.Rooms.First(r => r.Id != roomId).Id;
        Assert.That(loader.LoadRoom(otherRoom), Is.False, "A stale room action must be rejected while the mini-game loads.");
        yield return WaitUntilLoaded(miniGame);
        Assert.That(loader.LoadRoom(otherRoom), Is.False, "A hidden room action cannot replace the home behind a mini-game.");
        Assert.That(loader.CurrentRoom.Id, Is.EqualTo(roomId));
        Assert.That(CatRunnerSessionContext.ReturnRoomId, Is.EqualTo(roomId));
        Assert.That(cat.IsMovementLocked && cat.AreWorldActionsBlocked, Is.True);
        Assert.That(Object.FindObjectsByType<CatMovement>(), Has.Length.EqualTo(1));
        AssertSinglePresentation();
        var joystick = Read<MobileJoystick>(cat, "mobileJoystick");
        Assert.That(joystick, Is.Not.Null);
        // Simulate an input value that would normally move the home cat while
        // the mini-game owns the screen; the movement Update must ignore it.
        typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick, Vector2.right);
        try
        {
            yield return new WaitForSeconds(.15f);
        }
        finally
        {
            joystick.CancelInput();
        }
        Assert.That(hunger.CurrentHunger, Is.LessThanOrEqualTo(maximumHunger + .02f));
        Assert.That(thirst.CurrentThirst, Is.LessThanOrEqualTo(maximumThirst + .02f));
        Assert.That(energy.CurrentEnergy, Is.LessThanOrEqualTo(maximumEnergy + .02f));
        Vector3 whileHidden = cat.transform.position;
        TestContext.WriteLine(roomId + "/" + miniGame + " frozen: before=" +
            stoppedAt.ToString("F6") + ", hidden=" + whileHidden.ToString("F6"));
        Assert.That(Vector3.Distance(whileHidden, stoppedAt), Is.LessThan(.01f),
            "All three coordinates must remain fixed while the hidden cat receives joystick input.");
        if (miniGame == CatRunnerLauncher.RunnerSceneName)
            Object.FindAnyObjectByType<CatRunnerGameController>().ExitFromWelcome();
        else
            Object.FindAnyObjectByType<CatCatchGameController>().ExitToHome();
        yield return WaitUntilUnloaded(miniGame);
        yield return null;
        Assert.That(loader.CurrentRoom.Id, Is.EqualTo(roomId));
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(roomScene));
        Assert.That(Object.FindAnyObjectByType<CatMovement>(), Is.SameAs(cat));
        Assert.That(Object.FindObjectsByType<CatMovement>(), Has.Length.EqualTo(1));
        Assert.That(CatActionState.IsBusy(cat), Is.False);
        Assert.That(cat.IsMovementLocked || cat.AreWorldActionsBlocked, Is.False,
            "Returning from welcome must restore normal home input without restarting the old action.");
        Assert.That(RunnerEnergyService.CurrentEnergy, Is.EqualTo(runnerLives));
        Assert.That(CatchLivesService.CurrentLives, Is.EqualTo(catchLives));
        // Input resumes after unload, including normal CharacterController
        // gravity. A pose's small authored Y offset may settle onto its floor;
        // the invariant here is no horizontal relocation and real ground contact.
        var controller = cat.GetComponent<CharacterController>();
        float groundDeadline = Time.realtimeSinceStartup + 1f;
        while (!controller.isGrounded && Time.realtimeSinceStartup < groundDeadline)
            yield return null;
        Vector3 returnedAt = cat.transform.position;
        Vector3 returnedDelta = returnedAt - stoppedAt;
        var groundHits = Physics.RaycastAll(controller.bounds.center, Vector3.down, 2f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            .Where(h => h.collider.gameObject.scene == cat.gameObject.scene &&
                        h.collider.GetComponentInParent<CatMovement>() == null && h.normal.y > .8f)
            .OrderBy(h => h.distance).ToArray();
        string coordinates = roomId + "/" + miniGame + ": before=" + stoppedAt.ToString("F6") +
            ", returned=" + returnedAt.ToString("F6") + ", delta=" + returnedDelta.ToString("F6") +
            ", grounded=" + controller.isGrounded;
        TestContext.WriteLine(coordinates);
        Assert.That(new Vector2(returnedDelta.x, returnedDelta.z).magnitude, Is.LessThan(.01f), coordinates);
        Assert.That(controller.isGrounded, Is.True, coordinates);
        Assert.That(groundHits, Is.Not.Empty, "The resumed cat must stand over its own room's physical floor. " + coordinates);
        float groundGap = controller.bounds.min.y - groundHits[0].point.y;
        TestContext.WriteLine("Ground=" + groundHits[0].collider.name + ", feetGap=" + groundGap.ToString("F6"));
        Assert.That(Mathf.Abs(groundGap), Is.LessThanOrEqualTo(Mathf.Max(.025f, controller.skinWidth * 1.5f)),
            "The controller's feet must stay within the floor contact skin. " + coordinates);
        AssertSinglePresentation();
    }

    private static void MoveHomeCat(CatMovement cat, Vector3 position, Quaternion rotation)
    {
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        cat.transform.SetPositionAndRotation(position, rotation);
        controller.enabled = true;
        Physics.SyncTransforms();
    }

    private static void AssertSinglePresentation()
    {
        Assert.That(Object.FindObjectsByType<Camera>()
            .Count(c => c.enabled && c.targetTexture == null), Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<AudioListener>()
            .Count(l => l.enabled), Is.EqualTo(1));
    }

    private static IEnumerator LaunchCatch()
    {
        Object.FindAnyObjectByType<CatCatchLauncher>(FindObjectsInactive.Include).Launch();
        yield return WaitUntilLoaded(CatCatchLauncher.CatchSceneName);
    }

    private static IEnumerator LaunchRunner()
    {
        Object.FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include).Launch();
        yield return WaitUntilLoaded(CatRunnerLauncher.RunnerSceneName);
    }

    private static IEnumerator WaitUntilLoaded(string sceneName)
    {
        for (int frame = 0; frame < 360; frame++)
        {
            if (SceneManager.GetSceneByName(sceneName).isLoaded && !CatRunnerSessionContext.IsLaunching)
                break;
            yield return null;
        }
        Assert.That(SceneManager.GetSceneByName(sceneName).isLoaded, Is.True);
        yield return null;
    }

    private static IEnumerator WaitUntilUnloaded(string sceneName)
    {
        for (int frame = 0; frame < 360 && SceneManager.GetSceneByName(sceneName).isLoaded; frame++)
            yield return null;
        Assert.That(SceneManager.GetSceneByName(sceneName).isLoaded, Is.False);
    }

    private static T Read<T>(object target, string field) =>
        (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    private static void Invoke(object target, string method, params object[] arguments) =>
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);

    private sealed class DeferredAdProvider : ICatRunnerRewardedAdProvider
    {
        public Action<bool> Completed;
        public bool IsRewardedAdReady(string placementId) => true;
        public void ShowRewardedAd(string placementId, Action<bool> completed) => Completed = completed;
    }
}
