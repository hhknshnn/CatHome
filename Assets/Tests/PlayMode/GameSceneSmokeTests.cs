using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class GameSceneSmokeTests
{
    [UnityTest]
    public IEnumerator CatRunnerScene_ContinuesBeyondSixtySecondsAndKeepsCoinBudget()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("CatRunner", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;

        CatRunnerGameController game =
            Object.FindAnyObjectByType<CatRunnerGameController>(FindObjectsInactive.Include);
        CatRunnerTrackManager track =
            Object.FindAnyObjectByType<CatRunnerTrackManager>(FindObjectsInactive.Include);
        Assert.That(game, Is.Not.Null);
        Assert.That(track, Is.Not.Null);

        FieldInfo obstacles = typeof(CatRunnerTrackManager).GetField(
            "obstacleTemplates",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(obstacles, Is.Not.Null);
        obstacles.SetValue(track, System.Array.Empty<GameObject>());

        Assert.That(game.IsRunning, Is.False,
            "Cat Runner must wait on its premium welcome panel before starting.");
        game.StartFromWelcome();

        Time.timeScale = 20f;
        try
        {
            float realTimeDeadline = Time.realtimeSinceStartup + 6f;
            while (game.ElapsedSeconds <= 60f &&
                   Time.realtimeSinceStartup < realTimeDeadline)
            {
                yield return null;
            }
            Assert.That(game.ElapsedSeconds, Is.GreaterThan(60f));
            Assert.That(game.IsRunning, Is.True,
                "The endless run must not finish when the old 60-second limit is crossed.");
            Assert.That(track.SpawnedCoins,
                Is.EqualTo(CatRunnerGameController.GetScheduledCoinCount(game.ElapsedSeconds)));
        }
        finally
        {
            Time.timeScale = 1f;
        }
    }

    [UnityTest]
    public IEnumerator LivingRoomDirectEntry_RedirectsThroughBootstrapAndLoadsFullGame()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(
            "LivingRoom_Level01",
            LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);

        while (!load.isDone)
            yield return null;

        LevelLoader loader = null;
        for (int frame = 0; frame < 300; frame++)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
            if (loader != null && loader.IsReady)
                break;
            yield return null;
        }

        Assert.That(loader, Is.Not.Null);
        Assert.That(loader.IsReady, Is.True);
        Assert.That(SceneManager.GetSceneByName("GameScene").isLoaded, Is.True);
        Assert.That(SceneManager.GetSceneByName("CatHome_UI").isLoaded, Is.True);
        Assert.That(SceneManager.GetSceneByName("LivingRoom_Level01").isLoaded, Is.True);
        Assert.That(Object.FindAnyObjectByType<MainPanelController>(
            FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<ShopPanelController>(
            FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<CatRunnerLauncher>(
            FindObjectsInactive.Include), Is.Not.Null);

        WhileYouWereAwayPopup[] offlinePopups =
            Object.FindObjectsByType<WhileYouWereAwayPopup>(
                FindObjectsInactive.Include);
        Assert.That(offlinePopups, Has.Length.EqualTo(1),
            "The shared UI must contain exactly one offline popup. A duplicate can " +
            "open invisibly under another Canvas and permanently hide the top HUD.");
        Assert.That(offlinePopups[0].transform.parent, Is.Not.Null);
        Assert.That(offlinePopups[0].transform.parent.name, Is.EqualTo("Canvas"));
        Assert.That(offlinePopups[0].transform.parent.parent, Is.Null,
            "The offline popup must be a direct child of the root gameplay Canvas.");

        CatRunnerLauncher homeLauncher = Object.FindAnyObjectByType<CatRunnerLauncher>(
            FindObjectsInactive.Include);
        Assert.That(homeLauncher.transform.parent, Is.Not.Null);
        Assert.That(homeLauncher.transform.parent.name, Is.EqualTo("Canvas"));
        Assert.That(homeLauncher.transform.parent.parent, Is.Null,
            "RunnerLaunchUI must remain a direct child of the root gameplay Canvas.");
    }

    [UnityTest]
    public IEnumerator KitchenDirectEntry_PreservesKitchenCameraAndLoadsSharedSystems()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(
            "Kitchen_Level01",
            LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null);
        while (!load.isDone)
            yield return null;

        LevelLoader loader = null;
        for (int frame = 0; frame < 300; frame++)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
            if (loader != null && loader.IsReady)
                break;
            yield return null;
        }

        Assert.That(loader, Is.Not.Null);
        Assert.That(loader.IsReady, Is.True);
        Assert.That(SceneManager.GetSceneByName("Kitchen_Level01").isLoaded, Is.True);
        Assert.That(SceneManager.GetSceneByName("GameScene").isLoaded, Is.True);
        Assert.That(SceneManager.GetSceneByName("CatHome_UI").isLoaded, Is.True);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Kitchen_Level01"));
        Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(HomeRoomService.KitchenId));
        Assert.That(CountScreenCameras(), Is.EqualTo(1));
        Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude),
            Has.Length.EqualTo(1));
        Assert.That(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsInactive.Exclude), Has.Length.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator GameScene_StartsWithSavedRoomCoreGameplayAndProgressionReady()
    {
        string expectedRoomId = CatHomeSaveSystem.PrepareRoomBootstrap();
        HomeRoomDefinition expectedRoom = HomeRoomService.GetOrLivingRoom(expectedRoomId);
        AsyncOperation load = SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, "GameScene could not be queued for loading.");

        while (!load.isDone)
            yield return null;

        LevelLoader loader = null;
        for (int frame = 0; frame < 300; frame++)
        {
            loader = Object.FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
            if (loader != null && loader.IsReady)
                break;
            yield return null;
        }

        Assert.That(loader, Is.Not.Null);
        Assert.That(loader.IsReady, Is.True, "LevelLoader did not finish within 300 frames.");
        Assert.That(loader.CurrentLevel, Is.Not.Null);
        Assert.That(SceneManager.GetSceneByName("GameScene").isLoaded, Is.True);
        Assert.That(SceneManager.GetSceneByName("CatHome_UI").isLoaded, Is.True);
        Assert.That(SceneManager.GetSceneByName(expectedRoom.SceneName).isLoaded, Is.True);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(expectedRoom.SceneName));
        Assert.That(HomeRoomService.CurrentRoomId, Is.EqualTo(expectedRoom.Id));

        CatMovement cat = Object.FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        Assert.That(cat, Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<SleepInteraction>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<BowlInteraction>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<MainPanelController>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<QuestPanelController>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<CurrencyHudController>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<ActivityPromptController>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<CatRunnerLauncher>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<CatActivityReaction>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(cat.GetComponent<CatIdleBehavior>(), Is.Not.Null);
        if (expectedRoom.Id == HomeRoomService.LivingRoomId)
        {
            Assert.That(Object.FindAnyObjectByType<BallChaseActivity>(
                FindObjectsInactive.Include), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<ScratchPostActivity>(
                FindObjectsInactive.Include), Is.Not.Null);
            Assert.That(Object.FindAnyObjectByType<MouseHuntActivity>(
                FindObjectsInactive.Include), Is.Null);
        }
        else
        {
            StoreProductDisplay[] roomProducts = Object.FindObjectsByType<StoreProductDisplay>(
                FindObjectsInactive.Include);
            int matchingRoomProducts = 0;
            for (int i = 0; i < roomProducts.Length; i++)
            {
                if (HomeStoreService.IsProductInRoomCollection(
                        expectedRoom.Id, roomProducts[i].ProductId))
                {
                    matchingRoomProducts++;
                }
            }
            Assert.That(matchingRoomProducts,
                Is.EqualTo(HomeStoreService.GetRoomCollection(expectedRoom.Id).Count));
        }

        AssertBound<MobileJoystick>(cat, "mobileJoystick");
        AssertBound<HungerSystem>(Object.FindAnyObjectByType<BowlInteraction>(), "hungerSystem");
        AssertBound<ThirstSystem>(Object.FindAnyObjectByType<BowlInteraction>(), "thirstSystem");
        AssertBound<UnityEngine.UI.Button>(Object.FindAnyObjectByType<BowlInteraction>(), "interactionButton");
        AssertBound<CatMovement>(Object.FindAnyObjectByType<HungerSystem>(), "catMovement");
        AssertBound<CatMovement>(Object.FindAnyObjectByType<ThirstSystem>(), "catMovement");
        AssertBound<CatMovement>(Object.FindAnyObjectByType<EnergySystem>(), "catMovement");
        AssertBound<SleepInteraction>(Object.FindAnyObjectByType<EnergySystem>(), "sleepInteraction");
        AssertBound<CatMovement>(Object.FindAnyObjectByType<MainPanelController>(), "catMovement");
        AssertBound<CatMovement>(Object.FindAnyObjectByType<QuestPanelController>(), "catMovement");

        Assert.That(ProgressionConfig.TryGetActive(out ProgressionConfig config), Is.True);
        Assert.That(config.ChapterCount, Is.GreaterThanOrEqualTo(3));

        for (int chapterNumber = 1; chapterNumber <= config.ChapterCount; chapterNumber++)
        {
            LevelDefinition level = config.GetChapter(chapterNumber);
            Assert.That(level, Is.Not.Null, $"Quest chapter {chapterNumber} is missing.");
            Assert.That(level.Quests.Count, Is.GreaterThan(0),
                $"Quest chapter {chapterNumber} has no quests.");
        }
    }

    [UnityTest]
    public IEnumerator CatRunnerScene_UsesCurrentCatAndStartsPlayable()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("CatRunner", LoadSceneMode.Single);
        Assert.That(load, Is.Not.Null, "CatRunner could not be queued for loading.");

        while (!load.isDone)
            yield return null;

        CatRunnerGameController game =
            Object.FindAnyObjectByType<CatRunnerGameController>(FindObjectsInactive.Include);
        CatRunnerPlayer player =
            Object.FindAnyObjectByType<CatRunnerPlayer>(FindObjectsInactive.Include);
        CatRunnerTrackManager track =
            Object.FindAnyObjectByType<CatRunnerTrackManager>(FindObjectsInactive.Include);

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("CatRunner"));
        Assert.That(game, Is.Not.Null);
        Assert.That(player, Is.Not.Null);
        Assert.That(track, Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<Camera>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include), Is.Not.Null);
        Assert.That(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(
            FindObjectsInactive.Include), Is.Null,
            "The additive Runner scene must reuse CatHome_UI's shared EventSystem.");
        Assert.That(game.transform.root.position.y, Is.GreaterThanOrEqualTo(900f));
        AssertBound<GameObject>(game, "welcomePanel");
        Assert.That(game.IsRunning, Is.False,
            "The run must not bypass the welcome panel.");

        Animator animator = player.GetComponentInChildren<Animator>(true);
        Assert.That(animator, Is.Not.Null);
        Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
        Assert.That(animator.runtimeAnimatorController.name,
            Is.EqualTo("CatHome_Polyperfect"));

        FieldInfo obstacleTemplates = typeof(CatRunnerTrackManager).GetField(
            "obstacleTemplates",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(obstacleTemplates, Is.Not.Null);
        obstacleTemplates.SetValue(track, System.Array.Empty<GameObject>());

        game.StartFromWelcome();
        yield return new WaitForSeconds(3.2f);

        Assert.That(game.IsRunning, Is.True,
            "Cat Runner did not finish its countdown within three seconds.");

        MethodInfo applyGesture = typeof(CatRunnerPlayer).GetMethod(
            "ApplyGesture",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(applyGesture, Is.Not.Null);
        applyGesture.Invoke(player, new object[] { CatRunnerGestureDirection.Up });
        yield return new WaitForSeconds(0.15f);
        Assert.That(player.Height, Is.GreaterThan(0.1f),
            "An upward swipe must launch the cat into a real jump.");

        game.RegisterObstacleHit();
        Assert.That(game.CollisionCount, Is.EqualTo(1));
        Assert.That(game.ChancesRemaining, Is.EqualTo(2));
        Assert.That(game.IsRunning, Is.True);

        yield return new WaitForSeconds(1.6f);
        game.RegisterObstacleHit();
        Assert.That(game.CollisionCount, Is.EqualTo(2));
        Assert.That(game.ChancesRemaining, Is.EqualTo(1));
        Assert.That(game.IsRunning, Is.True);

        yield return new WaitForSeconds(1.6f);
        game.RegisterObstacleHit();
        Assert.That(game.CollisionCount, Is.EqualTo(3));
        Assert.That(game.ChancesRemaining, Is.Zero);
        Assert.That(game.IsRunning, Is.False,
            "The third accepted collision must end the run.");
    }

    private static int CountScreenCameras()
    {
        int count = 0;
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].enabled && cameras[i].targetTexture == null)
                count++;
        }

        return count;
    }

    private static void AssertBound<T>(object owner, string fieldName) where T : Object
    {
        Assert.That(owner, Is.Not.Null);
        FieldInfo field = owner.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
        Assert.That(field.GetValue(owner), Is.InstanceOf<T>(),
            $"{owner.GetType().Name}.{fieldName} was not rebound across scenes.");
    }
}
