using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps level scenes useful as authoring scenes while making the Play button
/// safe: entering a level directly adds GameScene so shared UI, save services
/// and cross-scene bindings are created without dropping the authoring camera.
/// </summary>
public static class DirectLevelPlayBootstrap
{
    private const string BootstrapSceneName = "GameScene";
    private const string BootstrapScenePath = "Assets/Scenes/GameScene.unity";
    private static bool redirecting;
    private static string requestedRoomId;

    /// <summary>
    /// PlayMode fixtures load a single room scene on purpose and bring their own
    /// need systems and store state. Adding GameScene underneath them races that
    /// setup: LevelLoader re-applies the save file, so a product the fixture just
    /// reset comes back owned, and it may add a second room and therefore a
    /// second cat to the same session. A fixture sets this before loading a room
    /// and clears it afterwards; normal play never assigns it, and every Play
    /// session starts with it false.
    /// </summary>
    public static bool RedirectSuppressed { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        redirecting = false;
        requestedRoomId = null;
        RedirectSuppressed = false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CheckInitialScene()
    {
        // The first scene's sceneLoaded callback may occur before a
        // BeforeSceneLoad subscription on some Editor play-mode settings.
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (RedirectSuppressed || redirecting || mode != LoadSceneMode.Single ||
            !IsHomeRoomScene(scene))
        {
            return;
        }

        if (Object.FindAnyObjectByType<LevelLoader>(FindObjectsInactive.Include) != null)
            return;

        if (SceneUtility.GetBuildIndexByScenePath(BootstrapScenePath) < 0)
        {
            Debug.LogError(
                $"Direct level play needs '{BootstrapScenePath}' enabled in Build Settings.");
            return;
        }

        requestedRoomId = ResolveRoomId(scene);
        redirecting = true;
        // Keep the authored room and its camera alive while GameScene starts.
        // LevelLoader recognises that the requested room is already loaded and
        // completes the normal UI/rebinding path without a black transition gap.
        SceneManager.LoadSceneAsync(BootstrapSceneName, LoadSceneMode.Additive);
    }

    /// <summary>
    /// One-shot authoring hand-off consumed by LevelLoader after the bootstrap
    /// redirect. Normal build entry never sets this value.
    /// </summary>
    public static bool TryConsumeRequestedRoomId(out string roomId)
    {
        roomId = requestedRoomId;
        requestedRoomId = null;
        bool hadRequest = !string.IsNullOrWhiteSpace(roomId);
        if (hadRequest)
            redirecting = false;
        return hadRequest;
    }

    private static bool IsHomeRoomScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return false;
        string roomId = ResolveRoomId(scene);
        return HomeRoomService.TryGetRoom(roomId, out HomeRoomDefinition room) &&
               string.Equals(room.ScenePath, scene.path, System.StringComparison.Ordinal);
    }

    private static string ResolveRoomId(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            HomeRoomSceneMarker room =
                roots[i].GetComponentInChildren<HomeRoomSceneMarker>(true);
            if (room != null)
                return room.RoomId;

            LevelSceneMarker level =
                roots[i].GetComponentInChildren<LevelSceneMarker>(true);
            if (level != null)
                return level.SceneId;
        }

        return null;
    }
}
