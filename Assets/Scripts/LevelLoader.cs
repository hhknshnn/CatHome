using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class LevelLoader : MonoBehaviour
{
    public const string DefaultUiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";

    [SerializeField] private string uiScenePath = DefaultUiScenePath;
    [SerializeField] private bool loadOnStart = true;

    private Coroutine loadRoutine;
    private ProgressionConfig config;
    private string pendingRoomScenePath;
    private CatMovement outgoingTransitionCat;
    private CatMovement incomingTransitionCat;

    public bool IsReady { get; private set; }
    public bool IsTransitioning => loadRoutine != null;
    public bool HasCurrentRoom { get; private set; }
    public LevelDefinition CurrentLevel { get; private set; }
    public HomeRoomDefinition CurrentRoom { get; private set; }

    // LevelLoaded stays as a compatibility event for quest/chapter UI. Room
    // navigation has its own strongly typed lifecycle below.
    public event Action<LevelDefinition> LevelLoaded;
    public event Action<HomeRoomDefinition> RoomLoadStarted;
    public event Action<HomeRoomDefinition> RoomLoaded;
    public event Action<string, string> RoomLoadFailed;

    private void OnEnable()
    {
        ProgressionService.StateChanged += HandleProgressionChanged;
    }

    private void OnDisable()
    {
        ProgressionService.StateChanged -= HandleProgressionChanged;
        SceneManager.sceneLoaded -= HandlePendingRoomSceneLoaded;
        ReleaseTransitionInputBlocks();
    }

    private void Start()
    {
        if (loadOnStart)
            LoadInitialLevel();
    }

    public void LoadInitialLevel()
    {
        TryResolveConfig();
        CurrentLevel = config != null ? config.GetChapter(1) : null;

        string roomId = CatHomeSaveSystem.PrepareRoomBootstrap();
        bool authoringOverride = false;
        if (DirectLevelPlayBootstrap.TryConsumeRequestedRoomId(out string requestedRoomId) &&
            HomeRoomService.TryGetRoom(requestedRoomId, out _))
        {
            roomId = requestedRoomId;
            authoringOverride = true;
        }

        if (!HomeRoomService.TryGetRoom(roomId, out HomeRoomDefinition room) ||
            (!authoringOverride && !HomeRoomService.IsRoomUnlocked(room.Id)))
        {
            room = HomeRoomService.GetOrLivingRoom(HomeRoomService.LivingRoomId);
        }

        BeginLoad(room, false, authoringOverride);
    }

    /// <summary>
    /// Compatibility lookup for quest chapters. Chapters update quest metadata
    /// only and never choose a home room.
    /// </summary>
    public bool LoadLevel(string levelId)
    {
        if (!TryResolveConfig())
            return false;

        LevelDefinition level = config.GetChapterById(levelId);
        if (level == null)
            return false;

        CurrentLevel = level;
        LevelLoaded?.Invoke(level);
        return true;
    }

    /// <summary>
    /// Queues an additive room transition. Returns false without changing scenes
    /// when the id is unknown, locked, already loading, or the asset is missing
    /// from Build Settings.
    /// </summary>
    public bool LoadRoom(string roomId)
    {
        // A queued home button must not replace the hidden return room while
        // either mini-game is loading, showing its welcome, or being played.
        if (HomeUiFlow.IsMiniGameVisible)
            return false;
        if (loadRoutine != null || !HomeRoomService.TryGetRoom(roomId, out HomeRoomDefinition room))
            return false;

        if (!HomeRoomService.IsRoomUnlocked(room.Id))
        {
            RoomLoadFailed?.Invoke(room.Id, "ROOM LOCKED");
            return false;
        }

        if (HasCurrentRoom &&
            string.Equals(CurrentRoom.Id, room.Id, StringComparison.Ordinal) &&
            IsSceneLoaded(room.ScenePath))
        {
            return true;
        }

        if (SceneUtility.GetBuildIndexByScenePath(room.ScenePath) < 0)
        {
            string message = $"Room scene '{room.ScenePath}' is not enabled in Build Settings.";
            Debug.LogError(message, this);
            RoomLoadFailed?.Invoke(room.Id, message);
            return false;
        }

        // Care and furniture routines own shared need systems that survive the
        // old room. End them before either saving or loading another cat.
        Scene outgoingScene = HasCurrentRoom
            ? SceneManager.GetSceneByPath(CurrentRoom.ScenePath)
            : SceneManager.GetActiveScene();
        outgoingTransitionCat = outgoingScene.IsValid() && outgoingScene.isLoaded
            ? FindInScene<CatMovement>(outgoingScene)
            : null;
        if (outgoingTransitionCat != null)
        {
            outgoingTransitionCat.AcquireInputBlock(this);
            CatActionState.CancelForTransition(outgoingTransitionCat);
        }
        try
        {
            CatHomeSaveSystem.SaveNow();
            BeginLoad(room, true, false);
        }
        catch
        {
            ReleaseTransitionInputBlocks();
            throw;
        }
        return true;
    }

    private bool TryResolveConfig()
    {
        return config != null || ProgressionConfig.TryGetActive(out config);
    }

    private void BeginLoad(
        HomeRoomDefinition room,
        bool applySpawn,
        bool authoringOverride)
    {
        if (loadRoutine != null)
            return;

        loadRoutine = StartCoroutine(LoadRoutine(room, applySpawn, authoringOverride));
    }

    private IEnumerator LoadRoutine(
        HomeRoomDefinition room,
        bool applySpawn,
        bool authoringOverride)
    {
        IsReady = false;
        CatPawReachCatalog.Preload(CatBreedService.SelectedBreedId);
        RoomLoadStarted?.Invoke(room);

        // Finish measured-data integration before starting scene integration.
        // A selection can supersede another request; both must settle, and the
        // final selected breed must stay ready for a complete rendered frame.
        float dataDeadline = Time.realtimeSinceStartup + 15f;
        string dataBreed = null;
        int dataReadyFrame = -1;
        while (true)
        {
            string selected = CatBreedService.SelectedBreedId;
            if (dataBreed != selected)
            {
                dataBreed = selected;
                dataReadyFrame = -1;
                CatPawReachCatalog.Preload(dataBreed);
            }
            bool ready = CatPawReachCatalog.IsReadyFor(dataBreed) && !CatPawReachCatalog.HasPendingLoads;
            if (!ready) dataReadyFrame = -1;
            else if (dataReadyFrame < 0) dataReadyFrame = Time.frameCount;
            else if (Time.frameCount > dataReadyFrame) break;
            if (Time.realtimeSinceStartup >= dataDeadline)
            {
                FailLoad(room, "Measured contact data loading timed out for " + dataBreed);
                yield break;
            }
            yield return null;
        }

        yield return LoadAdditiveIfNeeded(uiScenePath);

        bool targetWasLoaded = IsSceneLoaded(room.ScenePath);
        pendingRoomScenePath = room.ScenePath;
        SceneManager.sceneLoaded -= HandlePendingRoomSceneLoaded;
        SceneManager.sceneLoaded += HandlePendingRoomSceneLoaded;
        yield return LoadAdditiveIfNeeded(room.ScenePath);
        SceneManager.sceneLoaded -= HandlePendingRoomSceneLoaded;
        pendingRoomScenePath = null;

        Scene roomScene = SceneManager.GetSceneByPath(room.ScenePath);
        if (!roomScene.IsValid() || !roomScene.isLoaded)
        {
            FailLoad(room, $"Room scene '{room.ScenePath}' could not be loaded.");
            yield break;
        }

        if (!ValidateRoomScene(room, roomScene, out string validationFailure))
        {
            if (!targetWasLoaded)
                yield return SceneManager.UnloadSceneAsync(roomScene);
            FailLoad(room, validationFailure);
            yield break;
        }

        CatMovement cat = FindInScene<CatMovement>(roomScene);
        incomingTransitionCat = cat;
        if (cat != null)
            cat.AcquireInputBlock(this);

        // Switch presentation atomically after the target is known-good. The
        // sceneLoaded callback parked its camera before a rendered frame, so an
        // additive load cannot expose two active level cameras/listeners.
        EnableOnlyRoomCamera(roomScene);
        SceneManager.SetActiveScene(roomScene);

        yield return UnloadOtherRoomScenes(room.ScenePath);

        if (applySpawn)
            ApplySpawn(room, roomScene, cat);

        SceneReferenceBinder.RebindAll();
        if (cat != null)
        {
            CatHomeSaveSystem.Initialize(cat);
            ProgressionService.Initialize(cat);
        }

        // Bound asynchronous loading and fail through the existing release path
        // before marking the room active. Missing data never means permission.
        string pawBreed=CatBreedService.SelectedBreedId;
        float pawDeadline=Time.realtimeSinceStartup+15f;
        while(CatPawReachCatalog.IsLoadingFor(pawBreed))
        {
            if(Time.realtimeSinceStartup>=pawDeadline)
            {FailLoad(room,"Measured contact data loading timed out for "+pawBreed);yield break;}
            yield return null;
        }
        if(!CatPawReachCatalog.IsReadyFor(pawBreed))
        {FailLoad(room,"Measured contact data is missing for "+pawBreed);yield break;}

        bool marked;
#if UNITY_EDITOR
        marked = HomeRoomService.TryMarkRoomActive(room.Id, authoringOverride);
#else
        marked = HomeRoomService.TryMarkRoomActive(room.Id);
#endif
        if (!marked)
        {
            FailLoad(room, $"Room '{room.Id}' lost access before activation.");
            yield break;
        }

        CurrentRoom = room;
        HasCurrentRoom = true;
        CurrentLevel = ProgressionService.GetActiveChapterDefinition() ?? CurrentLevel;
        IsReady = true;
        loadRoutine = null;
        CatHomeSaveSystem.SaveNow();
        try
        {
            RoomLoaded?.Invoke(room);
        }
        finally
        {
            ReleaseTransitionInputBlocks();
        }
        if (CurrentLevel != null)
            LevelLoaded?.Invoke(CurrentLevel);
    }

    private void HandlePendingRoomSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(pendingRoomScenePath) ||
            !string.Equals(scene.path, pendingRoomScenePath, StringComparison.Ordinal))
        {
            return;
        }

        SetScenePresentationEnabled(scene, false, null);
        incomingTransitionCat = FindInScene<CatMovement>(scene);
        if (incomingTransitionCat != null)
            incomingTransitionCat.AcquireInputBlock(this);
    }

    private void FailLoad(HomeRoomDefinition room, string message)
    {
        Debug.LogError(message, this);
        SceneManager.sceneLoaded -= HandlePendingRoomSceneLoaded;
        pendingRoomScenePath = null;
        IsReady = HasCurrentRoom;
        loadRoutine = null;
        ReleaseTransitionInputBlocks();
        RoomLoadFailed?.Invoke(room.Id, message);
    }

    private void ReleaseTransitionInputBlocks()
    {
        if (outgoingTransitionCat != null)
            outgoingTransitionCat.ReleaseInputBlock(this);
        if (incomingTransitionCat != null)
            incomingTransitionCat.ReleaseInputBlock(this);
        outgoingTransitionCat = null;
        incomingTransitionCat = null;
    }

    private static bool ValidateRoomScene(
        HomeRoomDefinition room,
        Scene scene,
        out string failure)
    {
        HomeRoomSceneMarker roomMarker = FindInScene<HomeRoomSceneMarker>(scene);
        LevelSceneMarker levelMarker = FindInScene<LevelSceneMarker>(scene);
        if (roomMarker == null && levelMarker == null)
        {
            failure = $"Room scene '{room.ScenePath}' has no room/level marker.";
            return false;
        }

        string markerId = roomMarker != null ? roomMarker.RoomId : levelMarker.SceneId;
        if (!string.Equals(markerId, room.Id, StringComparison.Ordinal))
        {
            failure = $"Room scene '{room.ScenePath}' marker is '{markerId}', expected '{room.Id}'.";
            return false;
        }

        if (FindInScene<Camera>(scene) == null)
        {
            failure = $"Room scene '{room.ScenePath}' has no level camera.";
            return false;
        }

        if (FindInScene<CatMovement>(scene) == null)
        {
            failure = $"Room scene '{room.ScenePath}' has no cat.";
            return false;
        }

        failure = null;
        return true;
    }

    private static IEnumerator LoadAdditiveIfNeeded(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath) || IsSceneLoaded(scenePath))
            yield break;

        if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
        {
            Debug.LogError($"Scene '{scenePath}' is not enabled in Build Settings.");
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogError($"Scene '{scenePath}' could not be queued for loading.");
            yield break;
        }

        while (!operation.isDone)
            yield return null;
    }

    private static IEnumerator UnloadOtherRoomScenes(string targetScenePath)
    {
        IReadOnlyList<HomeRoomDefinition> rooms = HomeRoomService.Rooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            string path = rooms[i].ScenePath;
            if (string.Equals(path, targetScenePath, StringComparison.Ordinal))
                continue;

            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            SetScenePresentationEnabled(scene, false, null);
            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            if (unload != null)
                while (!unload.isDone)
                    yield return null;
        }
    }

    private static void EnableOnlyRoomCamera(Scene targetScene)
    {
        IReadOnlyList<HomeRoomDefinition> rooms = HomeRoomService.Rooms;
        for (int i = 0; i < rooms.Count; i++)
        {
            Scene loaded = SceneManager.GetSceneByPath(rooms[i].ScenePath);
            if (loaded.IsValid() && loaded.isLoaded)
                SetScenePresentationEnabled(loaded, false, null);
        }

        Camera preferred = ResolvePreferredCamera(targetScene);
        HomeRoomCameraProfile.Apply(preferred);
        SetScenePresentationEnabled(targetScene, true, preferred);
    }

    private static Camera ResolvePreferredCamera(Scene scene)
    {
        HomeRoomSceneMarker marker = FindInScene<HomeRoomSceneMarker>(scene);
        if (marker != null && marker.LevelCamera != null &&
            marker.LevelCamera.gameObject.scene == scene)
        {
            return marker.LevelCamera;
        }

        Camera[] cameras = FindAllInScene<Camera>(scene);
        for (int i = 0; i < cameras.Length; i++)
            if (cameras[i].CompareTag("MainCamera"))
                return cameras[i];
        return cameras.Length > 0 ? cameras[0] : null;
    }

    private static void SetScenePresentationEnabled(
        Scene scene,
        bool enabled,
        Camera preferred)
    {
        Camera[] cameras = FindAllInScene<Camera>(scene);
        for (int i = 0; i < cameras.Length; i++)
        {
            bool cameraEnabled = enabled && cameras[i] == preferred;
            cameras[i].enabled = cameraEnabled;
            AudioListener listener = cameras[i].GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = cameraEnabled;
        }
    }

    private static void ApplySpawn(
        HomeRoomDefinition room,
        Scene roomScene,
        CatMovement cat)
    {
        if (cat == null)
            return;

        LevelSpawnPoint[] spawnPoints = FindAllInScene<LevelSpawnPoint>(roomScene);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            LevelSpawnPoint spawn = spawnPoints[i];
            if (!string.Equals(spawn.SpawnPointId, room.SpawnPointId, StringComparison.Ordinal))
                continue;

            CharacterController controller = cat.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            cat.transform.SetPositionAndRotation(spawn.transform.position, spawn.transform.rotation);
            if (controller != null)
                controller.enabled = true;
            return;
        }

        Debug.LogWarning(
            $"LevelLoader found no spawn point '{room.SpawnPointId}' in '{room.ScenePath}'.");
    }

    private void HandleProgressionChanged()
    {
        if (!IsReady || loadRoutine != null || !TryResolveConfig())
            return;

        LevelDefinition activeChapter = ProgressionService.GetActiveChapterDefinition();
        if (activeChapter == null ||
            (CurrentLevel != null && activeChapter.LevelId == CurrentLevel.LevelId))
        {
            return;
        }

        CurrentLevel = activeChapter;
        LevelLoaded?.Invoke(activeChapter);
    }

    private static bool IsSceneLoaded(string scenePath)
    {
        Scene scene = SceneManager.GetSceneByPath(scenePath);
        return scene.IsValid() && scene.isLoaded;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        T[] components = FindAllInScene<T>(scene);
        return components.Length > 0 ? components[0] : null;
    }

    private static T[] FindAllInScene<T>(Scene scene) where T : Component
    {
        var result = new List<T>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            result.AddRange(roots[i].GetComponentsInChildren<T>(true));
        return result.ToArray();
    }
}
