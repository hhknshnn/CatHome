using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Small scene-transition context. It carries only the positive care bonus;
/// wallets and save data remain owned by their existing services.
/// </summary>
public static class CatRunnerSessionContext
{
    private static string launchingScene;
    private static bool returnToGames;
    public static bool IsLaunching => !string.IsNullOrEmpty(launchingScene);

    public static void SetReturnToGames(bool value) => returnToGames = value;

    public static void RestoreRequestedNavigation()
    {
        if (!returnToGames)
            return;
        var loader = Object.FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
        var hub = Object.FindAnyObjectByType<GamesHubPanel>(FindObjectsInactive.Include);
        if (loader == null || !loader.IsReady || loader.IsTransitioning || hub == null)
            return;
        returnToGames = false;
        // Called after home canvases return but before additive unload finishes:
        // the hub's input owner bridges the transition without an unlocked frame.
        hub.ShowFromMiniGameReturn();
    }

    public static bool TryBeginLaunch(string sceneName)
    {
        // Reject a missing/disabled build scene before reserving the transition
        // or cancelling the cat's current care activity.
        if ((sceneName != CatRunnerLauncher.RunnerSceneName &&
             sceneName != CatCatchLauncher.CatchSceneName &&
             sceneName != CozyMiniGame.YarnScene && sceneName != CozyMiniGame.PondScene) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
            return false;
        LevelLoader loader = Object.FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include);
        if (loader == null || !loader.IsReady || !loader.HasCurrentRoom || loader.IsTransitioning)
            return false;
        // Both launchers share a reservation, including the frame before an
        // additive scene is loaded and visible to HomeUiFlow.
        if (IsLaunching ||
            SceneManager.GetSceneByName(CatRunnerLauncher.RunnerSceneName).IsValid() ||
            SceneManager.GetSceneByName(CatCatchLauncher.CatchSceneName).IsValid() ||
            SceneManager.GetSceneByName(CozyMiniGame.YarnScene).IsValid() ||
            SceneManager.GetSceneByName(CozyMiniGame.PondScene).IsValid())
            return false;
        launchingScene = sceneName;
        returnToGames = false;
        return true;
    }

    public static void CompleteLaunch(string sceneName)
    {
        if (launchingScene == sceneName)
            launchingScene = null;
    }

    public static int CareBonusPercent { get; private set; }
    public static string ReturnRoomId { get; private set; } =
        HomeRoomService.LivingRoomId;
    public static string ReturnRoomSceneName =>
        HomeRoomService.GetOrLivingRoom(ReturnRoomId).SceneName;

    public static void CaptureFromHome()
    {
        // Snapshot the launch origin. Save migration or another service may
        // normalize HomeRoomService while Runner is open; returning must still
        // target the room the player actually left.
        ReturnRoomId = HomeRoomService.CurrentRoomId;

        HungerSystem hunger = Object.FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        ThirstSystem thirst = Object.FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        EnergySystem energy = Object.FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);

        if (hunger == null || thirst == null || energy == null)
        {
            CareBonusPercent = 0;
            return;
        }

        float average = (hunger.CurrentHunger + thirst.CurrentThirst + energy.CurrentEnergy) / 3f;
        CareBonusPercent = average >= 80f ? 20 : average >= 60f ? 10 : 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        launchingScene = null;
        returnToGames = false;
        CareBonusPercent = 0;
        ReturnRoomId = HomeRoomService.LivingRoomId;
    }
}
