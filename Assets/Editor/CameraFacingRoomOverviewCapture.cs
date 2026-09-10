using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CatHome.Economy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Eight real, fully furnished home rooms through the normal loader, in isolated QA only.</summary>
public static class CameraFacingRoomOverviewCapture
{
    public const string Output = "Docs/QA/CAMERA_FACING_2026-09-09/rooms";
    public static string Status { get; private set; } = "Idle";
    public static string Error { get; private set; } = "";
    public static bool IsRunning { get; private set; }
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Dictionary<string, bool> enabledStates = new Dictionary<string, bool>();
    static readonly List<Preference> preferences = new List<Preference>();
    static LevelLoader loader;
    static CatMovement cat;
    static EditorWindow gameView;
    static CatHomeSaveData saved;
    static DateTime savedUtc;
    static UnityEngine.Random.State randomState;
    static string originalRoom, primaryPath, recoveryPath;
    static byte[] primaryBytes, recoveryBytes;
    static float oldScale, oldCaptureDelta;
    static int oldSize;
    static bool stopRequested, snapshotHeld, returned;
    static Report report;

    [Serializable] sealed class RoomShot
    {
        public string room, scene, image;
        public string[] products;
        public Vector3 catPosition, cameraPosition;
        public float cameraFieldOfView;
        public int width = 1920, height = 1080;
    }
    [Serializable] sealed class Report
    {
        public string status, error, startedUtc, finishedUtc, originalRoom;
        public bool settingsRestored, originalRoomRestored;
        public List<RoomShot> rooms = new List<RoomShot>();
    }
    sealed class Preference { public string key, text; public bool existed, isString; public int number; }

    public static void Begin()
    {
        Require(!IsRunning && !CameraFacingVisualCapture.IsRunning, "Another camera capture is running.");
        Require(Application.isPlaying && EditorQaSession.IsActive, "Use a prepared isolated QA Play session.");
        loader = Object.FindAnyObjectByType<LevelLoader>();
        Require(loader != null && loader.IsReady && loader.HasCurrentRoom && !loader.IsTransitioning,
            "The normal room loader must be ready.");
        cat = FindCat();
        Require(cat != null && !CatActionState.IsBusy(cat) && !cat.HasScopedInputBlock &&
            !HomeUiFlow.IsHomeControlBlocked && !HomeUiFlow.IsMiniGameVisible && !TitleScreen.IsShowing,
            "Start from the quiet home HUD with the cat's previous action finished.");
        foreach (var room in HomeRoomService.Rooms)
        {
            Require(HomeRoomService.IsRoomUnlocked(room.Id), "Room is locked: " + room.Id);
            var ids = Products(room.Id);
            Require(ids.Length > 0 && ids.All(id => HomeStoreService.IsOwned(id) && !HomeStoreService.IsStored(id)),
                "All current ROOM products must already be owned and displayed: " + room.Id);
        }
        primaryPath = Path.GetFullPath(CatHomeSaveSystem.SaveFilePath);
        recoveryPath = Path.GetFullPath(CatHomeSaveSystem.RecoveryFilePath);
        RequireQaPaths();
        SaveSettings();
        stopRequested = false; returned = false; Error = "";
        report = new Report { startedUtc = DateTime.UtcNow.ToString("O"), originalRoom = originalRoom };
        Status = "Preparing eight room overviews"; IsRunning = true;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        try { loader.StartCoroutine(Drive()); }
        catch (Exception exception) { AddError(exception); Finish(); throw; }
    }

    public static void Stop() { if (IsRunning) { stopRequested = true; Status = "Returning to the original room"; } }

    static IEnumerator Drive()
    {
        try
        {
            yield return ExecuteSafely(CaptureRooms(), true);
            // Cleanup is deliberately independent of Stop: an interrupted tour still returns home.
            yield return ExecuteSafely(ReturnToOriginalRoom(), false);
        }
        finally { Finish(); }
    }

    static IEnumerator ExecuteSafely(IEnumerator routine, bool mayStop)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        try
        {
            while (stack.Count > 0 && (!mayStop || !stopRequested))
            {
                bool moved; object current = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception exception) { AddError(exception); break; }
                if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
        }
        finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
    }

    static IEnumerator CaptureRooms()
    {
        Directory.CreateDirectory(Output);
        Time.timeScale = 1; Time.captureFramerate = 0;
        QuietRoom();
        UiQaVisualTour.Resolution(1920, 1080);
        yield return Wait(() => Screen.width == 1920 && Screen.height == 1080, "1920 x 1080 Game view", 10);
        foreach (var room in HomeRoomService.Rooms)
        {
            Status = "Loading " + room.Id;
            yield return Load(room.Id);
            QuietRoom();
            yield return Wait(() => !HomeUiFlow.IsHomeControlBlocked && !TitleScreen.IsShowing &&
                cat != null && !cat.HasScopedInputBlock, "quiet home HUD in " + room.Id, 6);
            Require(!CatActionState.IsBusy(cat), "Unexpected room action: " + room.Id);
            SetNeeds(100, 100, 70);
            Quiet<HungerSystem>(); Quiet<ThirstSystem>(); Quiet<EnergySystem>();
            cat.AcquireInputBlock(gameView);
            PlaceOnClearCentralFloor();
            var expected = Products(room.Id);
            var visible = VisibleProducts(room.Id);
            Require(expected.SequenceEqual(visible), room.Id + " missing current visible products: " +
                string.Join(", ", expected.Except(visible)));
            var camera = WorldCamera();
            Require((camera.transform.position - HomeRoomCameraProfile.Position).sqrMagnitude < .0001f &&
                Quaternion.Angle(camera.transform.rotation, Quaternion.Euler(HomeRoomCameraProfile.Angles)) < .1f &&
                camera.GetComponent<HomeWorldViewport>() != null && camera.GetComponent<HomeWorldViewport>().enabled,
                "The room must retain its normal shared camera and HUD viewport: " + room.Id);
            yield return new WaitForSecondsRealtime(.4f);
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Require(expected.SequenceEqual(VisibleProducts(room.Id)), "Product visibility changed before capture: " + room.Id);
            string file = "Room_" + room.SceneName + "_1920x1080.png";
            Texture2D frame = null;
            try
            {
                frame = ScreenCapture.CaptureScreenshotAsTexture();
                Require(frame != null && frame.width == 1920 && frame.height == 1080, "Incorrect screenshot dimensions.");
                File.WriteAllBytes(Path.Combine(Output, file), frame.EncodeToPNG());
            }
            finally { if (frame != null) Object.Destroy(frame); }
            report.rooms.Add(new RoomShot { room = room.Id, scene = room.SceneName, image = file, products = expected,
                catPosition = cat.transform.position, cameraPosition = camera.transform.position, cameraFieldOfView = camera.fieldOfView });
            Status = "Captured " + report.rooms.Count + " / " + HomeRoomService.Rooms.Count + " rooms";
            WriteReport();
        }
    }

    static IEnumerator Load(string roomId)
    {
        RequireQaPaths();
        if (cat != null) cat.ReleaseInputBlock(gameView);
        Require(loader != null, "Room loader was destroyed.");
        yield return Wait(() => !loader.IsTransitioning, "previous room transition", 30);
        Require(loader.LoadRoom(roomId), "Room load rejected: " + roomId);
        yield return Wait(() => loader.IsReady && !loader.IsTransitioning && loader.HasCurrentRoom &&
            loader.CurrentRoom.Id == roomId, "room load " + roomId, 30);
        cat = FindCat();
        Require(cat != null, "Missing active room cat: " + roomId);
        yield return null; yield return null;
    }

    static IEnumerator ReturnToOriginalRoom()
    {
        Status = "Restoring original room";
        yield return Load(originalRoom);
        QuietRoom();
        returned = true;
    }

    static string[] Products(string roomId) => HomeStoreService.Products.Where(p =>
        HomeStoreService.IsProductInRoomCollection(roomId, p.Id)).Select(p => p.Id).Distinct().OrderBy(id => id).ToArray();

    static string[] VisibleProducts(string roomId)
    {
        return Object.FindObjectsByType<StoreProductDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(p => p.gameObject.scene == cat.gameObject.scene && HomeStoreService.IsProductInRoomCollection(roomId, p.ProductId) &&
                HomeStoreService.IsOwned(p.ProductId) && !HomeStoreService.IsStored(p.ProductId) && HasVisibleMesh(p))
            .Select(p => p.ProductId).Distinct().OrderBy(id => id).ToArray();
    }

    static bool HasVisibleMesh(StoreProductDisplay product)
    {
        var root = new SerializedObject(product).FindProperty("visualRoot").objectReferenceValue as GameObject;
        return root != null && root.activeInHierarchy && root.GetComponentsInChildren<Renderer>(false)
            .Any(r => r.enabled && !r.forceRenderingOff && (r is SkinnedMeshRenderer skin ? skin.sharedMesh != null :
                r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null));
    }

    static Camera WorldCamera()
    {
        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled && c.targetTexture == null).ToArray();
        Require(cameras.Length == 1, "Exactly one existing world camera is required.");
        return cameras[0];
    }

    static CatMovement FindCat() => Object.FindObjectsByType<CatMovement>(FindObjectsSortMode.None)
        .FirstOrDefault(c => loader != null && loader.HasCurrentRoom && c.gameObject.scene.name == loader.CurrentRoom.SceneName);

    static void PlaceOnClearCentralFloor()
    {
        Vector3 from = cat.transform.position, desired = new Vector3(0, from.y, -.3f);
        var points = CatActivityMotion.ReachableFloor(from).OrderBy(p => (p - desired).sqrMagnitude);
        foreach (var point in points)
        {
            Vector3 candidate = new Vector3(point.x, from.y, point.z);
            if (!CatActivityMotion.IsControllerFloorClear(cat, candidate) ||
                !CatActivityMotion.TryFloorPath(cat, from, candidate, out _)) continue;
            cat.ApplySavedWorldPose(candidate, CatActivityFacing.Resolve(cat, candidate, Quaternion.Euler(0, 180, 0)));
            Physics.SyncTransforms(); return;
        }
        throw new InvalidOperationException("No reachable, controller-clear central floor position: " + loader.CurrentRoom.Id);
    }

    static void QuietRoom()
    {
        Quiet<CollectionCompleteCelebrationView>(); Quiet<HomeLevelUpCelebrationView>(); Quiet<PetTutorialHint>(); Quiet<CatIdleBehavior>();
        Object.FindAnyObjectByType<CatCompanionPanel>()?.Close(); UiQaVisualTour.Clear();
        foreach (var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { popup.Close(); RememberAndDisable(popup); }
    }
    static void Quiet<T>() where T : Behaviour
    { foreach (var item in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)) RememberAndDisable(item); }
    static string Key(Behaviour item)
    {
        string path = item.name;
        for (var parent = item.transform.parent; parent != null; parent = parent.parent) path = parent.name + "/" + path;
        return item.gameObject.scene.path + "|" + path + "|" + item.GetType().FullName;
    }
    static void RememberAndDisable(Behaviour item)
    { string key = Key(item); if (!enabledStates.ContainsKey(key)) enabledStates[key] = item.enabled; item.enabled = false; }

    static void SaveSettings()
    {
        enabledStates.Clear(); preferences.Clear();
        originalRoom = loader.CurrentRoom.Id; savedUtc = DateTime.UtcNow; randomState = UnityEngine.Random.state;
        saved = new CatHomeSaveData { hunger = Object.FindAnyObjectByType<HungerSystem>().CurrentHunger,
            thirst = Object.FindAnyObjectByType<ThirstSystem>().CurrentThirst, energy = Object.FindAnyObjectByType<EnergySystem>().CurrentEnergy,
            catWorldPosition = cat.transform.position, catWorldRotation = cat.transform.rotation,
            coins = ProgressionService.Coins, diamonds = ProgressionService.Diamonds, bondXp = ProgressionService.BondXp,
            playerLevel = ProgressionService.CurrentChapterNumber, questProgress = ProgressionService.CaptureQuestProgress(),
            economy = EconomyService.CaptureState(), homeStore = HomeStoreService.CaptureState(),
            homeProgression = HomeProgressionService.CaptureState(), dailyRetention = DailyRetentionService.CaptureState(),
            achievements = AchievementService.CaptureState(), runnerEnergy = RunnerEnergyService.CaptureState(savedUtc),
            runnerProgress = CatRunnerProgressService.CaptureState(savedUtc), catchLives = CatchLivesService.CaptureState(savedUtc),
            catchBestScore = CatCatchGameController.CaptureBestScore() };
        saved = JsonUtility.FromJson<CatHomeSaveData>(JsonUtility.ToJson(saved));
        primaryBytes = File.Exists(primaryPath) ? File.ReadAllBytes(primaryPath) : null;
        recoveryBytes = File.Exists(recoveryPath) ? File.ReadAllBytes(recoveryPath) : null;
        oldScale = Time.timeScale; oldCaptureDelta = Time.captureDeltaTime;
        gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        oldSize = (int)gameView.GetType().GetProperty("selectedSizeIndex", Flags).GetValue(gameView);
        foreach (string key in new[] { "cat.identity.breed.v1", "CatHome_CatName", "CatHome_CollectionMilestones", "cat-home.local-guest-id" })
            preferences.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), isString = true, text = PlayerPrefs.GetString(key, "") });
        foreach (string key in new[] { "cat.identity.coat", "home.audio.sound", "home.audio.music", "cat-home.language", "Player_LightLevel",
            "CatHome_PetTutorialCompleted", "CatHome_IntroductionCompleted", "CatHome_IntroductionStep", "CatHome_OnboardingStep",
            "CatHome_OnboardingCompleted", "cat-home.account-kind", "CatRunner_BestScore_v1" })
            preferences.Add(new Preference { key = key, existed = PlayerPrefs.HasKey(key), number = PlayerPrefs.GetInt(key, 0) });
        snapshotHeld = true;
    }

    static void SetNeeds(float food, float water, float rest)
    {
        foreach (var value in Object.FindObjectsByType<HungerSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) value.ApplySavedValue(food);
        foreach (var value in Object.FindObjectsByType<ThirstSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) value.ApplySavedValue(water);
        foreach (var value in Object.FindObjectsByType<EnergySystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)) value.ApplySavedValue(rest);
    }

    static void Finish()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        if (!snapshotHeld) return;
        snapshotHeld = false;
        try
        {
            RequireQaPaths();
            if (cat != null) cat.ReleaseInputBlock(gameView);
            HomeStoreService.ApplySavedState(saved.homeStore);
            ProgressionService.ApplySavedState(saved.coins, saved.diamonds, saved.bondXp, saved.playerLevel, saved.questProgress);
            EconomyService.ApplySavedState(saved.economy); HomeProgressionService.ApplySavedState(saved.homeProgression);
            DailyRetentionService.ApplySavedState(saved.dailyRetention); AchievementService.ApplySavedState(saved.achievements);
            RunnerEnergyService.ApplySavedState(saved.runnerEnergy, savedUtc); CatRunnerProgressService.ApplySavedState(saved.runnerProgress, savedUtc);
            CatchLivesService.ApplySavedState(saved.catchLives, savedUtc); CatCatchGameController.ApplyBestScore(saved.catchBestScore);
            SetNeeds(saved.hunger, saved.thirst, saved.energy);
            if (returned && cat != null) cat.ApplySavedWorldPose(saved.catWorldPosition, saved.catWorldRotation);
            // Only the guarded QA files are restored; normal LevelLoader saves never reach the real player directory.
            RestoreFile(primaryPath, primaryBytes); RestoreFile(recoveryPath, recoveryBytes);
            report.settingsRestored = true;
        }
        catch (Exception exception) { AddError(exception); }
        finally
        {
            foreach (var item in Object.FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (enabledStates.TryGetValue(Key(item), out bool enabled)) item.enabled = enabled;
            foreach (var pref in preferences)
            {
                if (!pref.existed) PlayerPrefs.DeleteKey(pref.key);
                else if (pref.isString) PlayerPrefs.SetString(pref.key, pref.text);
                else PlayerPrefs.SetInt(pref.key, pref.number);
            }
            PlayerPrefs.Save(); Time.timeScale = oldScale; Time.captureDeltaTime = oldCaptureDelta; UnityEngine.Random.state = randomState;
            if (gameView != null) { gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(gameView, oldSize); gameView.Repaint(); }
            IsRunning = false; report.originalRoomRestored = returned;
            Status = Error.Length > 0 ? "Failed" : stopRequested ? "Stopped; restored" :
                report.rooms.Count == HomeRoomService.Rooms.Count && returned ? "Complete" : "Incomplete";
            report.finishedUtc = DateTime.UtcNow.ToString("O"); WriteReport();
        }
    }

    static void RequireQaPaths()
    {
        Require(EditorQaSession.IsActive, "The isolated QA save override is no longer active.");
        string real = Path.GetFullPath(Application.persistentDataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string qa = Path.GetFullPath(EditorQaSession.SaveDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Require(!qa.StartsWith(real, StringComparison.OrdinalIgnoreCase) &&
            primaryPath.StartsWith(qa, StringComparison.OrdinalIgnoreCase) && recoveryPath.StartsWith(qa, StringComparison.OrdinalIgnoreCase) &&
            primaryPath.Equals(Path.GetFullPath(CatHomeSaveSystem.SaveFilePath), StringComparison.OrdinalIgnoreCase), "Save paths must remain exclusively inside the original QA directory.");
    }
    static void RestoreFile(string path, byte[] bytes)
    { RequireQaPaths(); if (bytes != null) File.WriteAllBytes(path, bytes); else if (File.Exists(path)) File.Delete(path); }
    static IEnumerator Wait(Func<bool> ready, string label, float timeout)
    { float deadline = Time.realtimeSinceStartup + timeout; while (!ready() && Time.realtimeSinceStartup < deadline) yield return null; Require(ready(), "Timed out: " + label); }
    static void WriteReport()
    { if (report == null) return; report.status = Status; report.error = Error; Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "capture-status.json"), JsonUtility.ToJson(report, true)); }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    static void AddError(Exception exception) { Error += (Error.Length == 0 ? "" : "\n") + Status + ": " + exception; }
    static void OnPlayModeChanged(PlayModeStateChange change)
    { if (change == PlayModeStateChange.ExitingPlayMode && IsRunning) { stopRequested = true; AddError(new InvalidOperationException("Play stopped before the tour restored its room.")); Finish(); } }
}
