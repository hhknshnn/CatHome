using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Normal-speed captures of the actual bathroom routines, with the complete current room present.</summary>
public static class CameraFacingVisualCapture
{
    public const string Screens = "Docs/QA/CAMERA_FACING_2026-09-09/screens";
    public const string Frames = "Library/CameraFacingMotion";
    public static string Status { get; private set; } = "Idle";
    public static string Error { get; private set; } = "";
    public static bool IsRunning { get; private set; }
    public static int FrameCount { get; private set; }
    static readonly CatActivityKind[] Kinds = { CatActivityKind.PaperSpin, CatActivityKind.LitterDig, CatActivityKind.MatKnead,
        CatActivityKind.TubEdgeWalk, CatActivityKind.ShowerRinse, CatActivityKind.MirrorGaze };
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static readonly Dictionary<Behaviour, bool> states = new Dictionary<Behaviour, bool>();
    static CatMovement cat;
    static CatActivity activity;
    static LevelLoader host;
    static bool stopRequested, snapshotHeld, detailMode, cameraSnapshotHeld;
    static int previousRate, previousSize, completionCount;
    static float previousScale;
    static Vector3 originalPosition;
    static Quaternion originalRotation;
    static EditorWindow gameView;
    static HungerSystem hunger;
    static ThirstSystem thirst;
    static EnergySystem energy;
    static float hungerValue, thirstValue, energyValue;
    static string request;
    static CatActivityKind requestedActivity;
    static Camera detailCamera;
    static HomeWorldViewport detailViewport;
    static Vector3 cameraPosition, cameraScale;
    static Quaternion cameraRotation;
    static float cameraFieldOfView;
    static Rect cameraRect;
    static bool cameraEnabled, viewportEnabled;
    static CaptureReport report;

    [Serializable] sealed class CaptureReport
    {
        public string kind, activityKind, status, error;
        public int fps = 24, frames, completionCount;
        public bool requestedRestStop, detail;
        public float seconds, promptDistance;
        public Vector3 entryPosition, cameraPosition, cameraTarget;
        public float cameraFieldOfView;
    }

    public static void RoomOverview() => Begin("RoomOverview");

    /// <param name="kind">RoomOverview, All, an activity name, or LitterDigDetail, MatKneadDetail, ShowerRinseDetail.</param>
    public static void Begin(string kind)
    {
        Require(Application.isPlaying && EditorQaSession.IsActive, "Use an isolated QA Play session.");
        Require(!IsRunning, "Another bathroom capture is still running.");
        bool overview = string.Equals(kind, "RoomOverview", StringComparison.OrdinalIgnoreCase);
        bool all = string.Equals(kind, "All", StringComparison.OrdinalIgnoreCase);
        detailMode = kind != null && kind.EndsWith("Detail", StringComparison.OrdinalIgnoreCase);
        string activityName = detailMode ? kind.Substring(0, kind.Length - "Detail".Length) : kind;
        bool validActivity = Enum.TryParse(activityName, true, out requestedActivity) && Kinds.Contains(requestedActivity);
        Require(overview || all || validActivity,
            "Choose RoomOverview, All, a bathroom activity, or LitterDigDetail, MatKneadDetail, ShowerRinseDetail.");
        Require(!detailMode || requestedActivity == CatActivityKind.LitterDig || requestedActivity == CatActivityKind.MatKnead ||
            requestedActivity == CatActivityKind.ShowerRinse || requestedActivity == CatActivityKind.PaperSpin, "Detail mode supports the litter box, mat and shower only.");
        host = Object.FindAnyObjectByType<LevelLoader>();
        Require(host != null && host.IsReady && !host.IsTransitioning && host.CurrentRoom.Id == HomeRoomService.BathroomId,
            "Start in the ready Bathroom with its ten current products already owned.");
        Require(!HomeUiFlow.IsMiniGameVisible, "Close the mini-game before recording the bathroom.");
        cat = Object.FindAnyObjectByType<CatMovement>();
        Require(cat != null && !CatActionState.IsBusy(cat), "Finish the cat's current action before recording.");
        request = kind; stopRequested = false; Error = ""; FrameCount = 0; activity = null; report = null;
        SaveSettings(); IsRunning = true; Status = "Preparing " + kind;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        CatActivity.Completed += OnCompleted;
        try { host.StartCoroutine(Drive(overview, all)); }
        catch (Exception exception) { Error = exception.ToString(); Finish(); throw; }
    }

    public static void Stop() { if (IsRunning) { stopRequested = true; Status = "Stopping bathroom capture"; } }

    static IEnumerator Drive(bool overview, bool all)
    {
        try { yield return ExecuteSafely(Run(overview, all)); }
        finally { Finish(); }
    }

    // Drive nested routines ourselves so any preparation/capture exception is
    // reported by Status and still reaches the same restoration path.
    static IEnumerator ExecuteSafely(IEnumerator routine)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(routine);
        try
        {
            while (stack.Count > 0 && !stopRequested)
            {
                bool moved; object current = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception exception) { Error = Status + ": " + exception; break; }
                if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
        }
        finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
    }

    static IEnumerator Run(bool overview, bool all)
    {
        Directory.CreateDirectory(Screens);
        Quiet<CollectionCompleteCelebrationView>(); Quiet<HomeLevelUpCelebrationView>();
        Quiet<PetTutorialHint>(); Quiet<CatIdleBehavior>();
        Object.FindAnyObjectByType<CatCompanionPanel>()?.Close(); UiQaVisualTour.Clear();
        foreach (var popup in Object.FindObjectsByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        { popup.Close(); RememberAndDisable(popup); }
        yield return Wait(() => !HomeUiFlow.IsHomeControlBlocked && !TitleScreen.IsShowing && !cat.HasScopedInputBlock,
            "home modal close", 5);
        var products = Object.FindObjectsByType<StoreProductDisplay>(FindObjectsSortMode.None)
            .Where(p => p.gameObject.scene == cat.gameObject.scene && p.gameObject.activeInHierarchy &&
                HomeStoreService.IsProductInRoomCollection(HomeRoomService.BathroomId, p.ProductId))
            .Select(p => p.ProductId).Distinct().OrderBy(id => id).ToArray();
        Require(products.Length == 10 && products.All(HomeStoreService.IsOwned),
            "The capture needs all ten current Bathroom products visible together.");
        File.WriteAllLines(Path.Combine(Screens, "Bathroom_CurrentProducts.txt"), products);
        Require(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.enabled && c.targetTexture == null) == 1,
            "The normal bathroom must have exactly one world camera.");
        Time.timeScale = 1f;
        if (hunger != null) hunger.ApplySavedValue(100);
        if (thirst != null) thirst.ApplySavedValue(100);
        if (energy != null) energy.ApplySavedValue(70);
        if (overview || all)
        {
            foreach (int width in new[] { 1920, 1440 })
            {
                yield return Resolution(width);
                yield return Still("Room_Bathroom_Level01_" + width + "x1080");
            }
        }
        if (overview) yield break;
        yield return Resolution(1920);
        foreach (var kind in all ? Kinds : new[] { requestedActivity })
        {
            activity = CatActivity.Registered.Single(a => a.Kind == kind && a.gameObject.scene == cat.gameObject.scene);
            Require(activity.isActiveAndEnabled && activity.IsUnlocked && activity.IsContentVisible, "Unavailable activity: " + kind);
            Require(!cat.HasScopedInputBlock && !CatActionState.IsBusy(cat), "A different action or modal still owns the cat.");
            if (energy != null) energy.ApplySavedValue(70);
            float promptDistance = PlaceAtReachablePrompt(activity);
            report = new CaptureReport { kind = kind + (detailMode ? "Detail" : ""), activityKind = kind.ToString(),
                detail = detailMode, promptDistance = promptDistance, entryPosition = cat.transform.position };
            if (detailMode) ConfigureDetailCamera(kind);
            yield return null; yield return null;
            yield return Record(kind);
            activity = null;
            yield return new WaitForSecondsRealtime(.3f);
        }
    }

    static void ConfigureDetailCamera(CatActivityKind kind)
    {
        Require(cameraSnapshotHeld && detailCamera != null, "The existing world camera is unavailable for detail capture.");
        // The actual authored pose anchor follows the current room plan. Nothing
        // is moved/hidden in the room, and the activity's near-start gate is intact.
        string anchorField = kind == CatActivityKind.MatKnead ? "padPoint" :
            kind == CatActivityKind.LitterDig ? "digPoint" : kind == CatActivityKind.PaperSpin ? "swatPoint" : "standPoint";
        var anchor = activity.GetType().GetField(anchorField, Flags)?.GetValue(activity) as Transform;
        Require(anchor != null, "Missing detail pose anchor: " + kind + "." + anchorField);
        bool shower = kind == CatActivityKind.ShowerRinse;
        Vector3 subject = anchor.position + Vector3.up * (shower ? .35f : .3f);
        Vector3 offset = (HomeRoomCameraProfile.Position - subject).normalized * (shower ? 3f : 2.6f);
        if (detailViewport != null) detailViewport.enabled = false;
        detailCamera.transform.SetPositionAndRotation(subject + offset, Quaternion.LookRotation(-offset, Vector3.up));
        detailCamera.fieldOfView = 32f;
        // Preserve the normal dock/HUD reservation after OnDisable resets it.
        detailCamera.rect = cameraRect;
        report.cameraPosition = detailCamera.transform.position; report.cameraTarget = subject;
        report.cameraFieldOfView = detailCamera.fieldOfView;
    }

    static string ActionShotName(CatActivityKind kind, bool returned)
    {
        string phase = returned ? (detailMode ? "_ReturnedDetail" : "_Returned") : (detailMode ? "_Detail" : "");
        return "Action_" + kind + phase + "_1920x1080";
    }

    static float PlaceAtReachablePrompt(CatActivity target)
    {
        Require(target.RoutineEntryPoint != null, "Missing authored entry for " + target.Kind);
        Vector3 origin = cat.transform.position, anchor = target.RoutineEntryPoint.position; anchor.y = origin.y;
        var candidates = CatActivityMotion.ReachableFloor(origin);
        if (CatActivityMotion.IsFloorClear(anchor) && CatActivityMotion.TryFloorPath(origin, anchor, out _)) candidates.Add(anchor);
        candidates = candidates.OrderBy(p => HorizontalSquare(p - anchor)).ToList();
        foreach (Vector3 candidate in candidates)
        {
            Vector3 point = candidate; point.y = origin.y;
            if (!CatActivityMotion.IsFloorClear(point) || !CatActivityMotion.TryFloorPath(point, anchor, out _)) continue;
            Vector3 direction = target.transform.position - point; direction.y = 0;
            cat.ApplySavedWorldPose(point, direction.sqrMagnitude > .0001f ? Quaternion.LookRotation(direction) : originalRotation);
            Physics.SyncTransforms();
            // Use the product's actual near/geometry/visibility gate. No enlarged
            // radius, forced interaction point, or reflected BeginActivity call.
            if (target.TryGetPromptDistance(cat, out float distance)) return distance;
        }
        cat.ApplySavedWorldPose(origin, originalRotation);
        throw new InvalidOperationException("No reachable, visible, capsule-clear nearby prompt for " + target.Kind);
    }

    static IEnumerator Record(CatActivityKind kind)
    {
        string directory = Path.Combine(Frames, report.kind); Directory.CreateDirectory(directory);
        // Only this helper's numbered temporary frames are replaced; manifests,
        // encodes and other files in the directory remain untouched.
        foreach (string path in Directory.EnumerateFiles(directory, "*.jpg"))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.Length == 4 && name.All(c => c >= '0' && c <= '9')) File.Delete(path);
        }
        completionCount = 0; Time.timeScale = 1; Time.captureFramerate = 24;
        int readyFrames = 0, written = 0, litterPhaseFrames = 0; bool shot = false, started = false;
        CatLitterPhase previousLitterPhase = CatLitterPhase.None;
        var litterShots = new HashSet<CatLitterPhase>();
        for (int frame = 0; frame < 432; frame++)
        {
            if (frame == 12)
            {
                Require(activity.TryGetPromptDistance(cat, out _), "The nearby prompt changed before start.");
                Require(activity.TryStart(cat), "The real activity refused to start: " + kind);
                started = true;
            }
            if (started && activity.IsWaitingForRestStop && activity.RestingSeconds >= 1.4f)
            {
                Require(activity.RequestRestStop(), "The resting activity did not accept its normal stop action.");
                report.requestedRestStop = true;
            }
            yield return new WaitForEndOfFrame();
            Texture2D image = null;
            try
            {
                image = ScreenCapture.CaptureScreenshotAsTexture();
                Require(image != null, "The Game view did not supply a frame.");
                File.WriteAllBytes(Path.Combine(directory, frame.ToString("D4") + ".jpg"), image.EncodeToJPG(94));
                readyFrames = started && IsActionPhotoReady(kind) ? readyFrames + 1 : 0;
                if (!shot && readyFrames >= 8)
                {
                    File.WriteAllBytes(Path.Combine(Screens, ActionShotName(kind, false) + ".png"), image.EncodeToPNG()); shot = true;
                }
                if (kind == CatActivityKind.LitterDig && activity is LitterDigActivity litter)
                {
                    litterPhaseFrames = litter.Phase == previousLitterPhase ? litterPhaseFrames + 1 : 0;
                    previousLitterPhase = litter.Phase;
                    if (litterPhaseFrames >= 12 && (litter.Phase == CatLitterPhase.Squatting || litter.Phase == CatLitterPhase.Covering)
                        && litterShots.Add(litter.Phase))
                    {
                        string name = "Action_LitterDig_" + litter.Phase + (detailMode ? "_Detail" : "") + "_1920x1080.png";
                        File.WriteAllBytes(Path.Combine(Screens, name), image.EncodeToPNG());
                    }
                }
            }
            finally { if (image != null) Object.Destroy(image); }
            FrameCount++; written++; Status = "Recording " + report.kind + " " + written + "/432";
            report.frames = written; report.seconds = written / 24f; report.completionCount = completionCount;
            if (started && !activity.IsRunning)
            {
                Require(completionCount == 1, "The routine was interrupted instead of completing once: " + kind);
                if (written >= 240) break;
            }
        }
        report.frames = written; report.seconds = written / 24f; report.completionCount = completionCount;
        Require(started && !activity.IsRunning && completionCount == 1, "The complete routine exceeded the 18 second capture bound: " + kind);
        Require(shot, "The expected active pose was never observed: " + kind);
        Require(cat.GetComponent<CharacterController>().enabled && !cat.IsMovementPhysicallyLocked &&
            CatActivityMotion.IsFloorClear(cat.transform.position, .24f), "The routine did not restore clear-floor control: " + kind);
        Time.captureFramerate = 0;
        yield return Still(ActionShotName(kind, true));
        report.status = "Complete";
        File.WriteAllText(Path.Combine(directory, "capture.json"), JsonUtility.ToJson(report, true));
    }

    static bool IsActionPhotoReady(CatActivityKind kind)
    {
        if (activity == null || !activity.IsRunning) return false;
        if (kind == CatActivityKind.ShowerRinse)
            return activity.TryGetComponent<CatShowerWaterFx>(out var water) &&
                water.CurrentPhase == CatShowerWaterFx.Phase.Rinsing && water.FoamCount > 0 && water.Elapsed >= .65f;
        if (kind == CatActivityKind.MirrorGaze) return ((SitLookActivity)activity).GestureBeats > 0;
        if (kind == CatActivityKind.PaperSpin)
            return ((PaperSpinActivity)activity).PaperContactCount > 0 &&
                activity.TryGetComponent<CatPaperTearFx>(out var paper) && paper.ActivePieces > 0;
        if (kind == CatActivityKind.LitterDig)
            return ((LitterDigActivity)activity).Phase == CatLitterPhase.Digging &&
                cat.TryGetComponent<CatLitterRoutineMotion>(out var digging) && digging.ContactStrokes >= 2 &&
                ((LitterDigActivity)activity).LitterSurface.Depth >= .012f;
        var animation = cat.GetComponent<CatActivityAnimation>();
        if (animation == null) return false;
        if (kind == CatActivityKind.TubEdgeWalk)
        {
            var rim = (Transform)typeof(TubEdgeWalkActivity).GetField("rimStartPoint", Flags).GetValue(activity);
            return animation.CurrentPose == CatActivityPose.Walk && cat.transform.position.y >= rim.position.y - .04f;
        }
        return animation.CurrentPose == CatActivityPose.GentleKnead;
    }
    static IEnumerator Resolution(int width)
    {
        Time.captureFramerate = 0; UiQaVisualTour.Resolution(width, 1080);
        yield return Wait(() => Screen.width == width && Screen.height == 1080, "Game view resolution", 10);
        yield return new WaitForSecondsRealtime(.4f);
    }
    static IEnumerator Still(string name)
    {
        Status = "Capturing " + name; Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Texture2D image = null;
        try { image = ScreenCapture.CaptureScreenshotAsTexture(); Require(image != null, "Missing screenshot frame.");
            File.WriteAllBytes(Path.Combine(Screens, name + ".png"), image.EncodeToPNG());
            File.WriteAllText(Path.Combine(Screens, name + ".layout.txt"), UiQaVisualTour.Measure()); }
        finally { if (image != null) Object.Destroy(image); }
    }
    static IEnumerator Wait(Func<bool> ready, string label, float seconds)
    { float deadline = Time.realtimeSinceStartup + seconds; while (!ready() && Time.realtimeSinceStartup < deadline) yield return null; Require(ready(), "Timed out: " + label); }
    static float HorizontalSquare(Vector3 vector) => vector.x * vector.x + vector.z * vector.z;
    static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
    static void OnCompleted(CatActivity completed) { if (completed == activity) completionCount++; }
    static void Quiet<T>() where T : Behaviour
    { foreach (var item in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)) RememberAndDisable(item); }
    static void RememberAndDisable(Behaviour item) { if (!states.ContainsKey(item)) states[item] = item.enabled; item.enabled = false; }

    static void SaveSettings()
    {
        cameraSnapshotHeld = false; detailCamera = null; detailViewport = null;
        states.Clear(); previousRate = Time.captureFramerate; previousScale = Time.timeScale;
        originalPosition = cat.transform.position; originalRotation = cat.transform.rotation;
        hunger = Object.FindAnyObjectByType<HungerSystem>(); thirst = Object.FindAnyObjectByType<ThirstSystem>(); energy = Object.FindAnyObjectByType<EnergySystem>();
        hungerValue = hunger != null ? hunger.CurrentHunger : 0; thirstValue = thirst != null ? thirst.CurrentThirst : 0; energyValue = energy != null ? energy.CurrentEnergy : 0;
        gameView = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
        previousSize = (int)gameView.GetType().GetProperty("selectedSizeIndex", Flags).GetValue(gameView);
        if (detailMode)
        {
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled && c.targetTexture == null).ToArray();
            Require(cameras.Length == 1, "Detail capture requires the existing single world camera.");
            detailCamera = cameras[0]; detailViewport = detailCamera.GetComponent<HomeWorldViewport>();
            cameraPosition = detailCamera.transform.position; cameraRotation = detailCamera.transform.rotation;
            cameraScale = detailCamera.transform.localScale; cameraFieldOfView = detailCamera.fieldOfView;
            cameraRect = detailCamera.rect; cameraEnabled = detailCamera.enabled;
            viewportEnabled = detailViewport != null && detailViewport.enabled;
            cameraSnapshotHeld = true;
        }
        snapshotHeld = true;
    }
    static void RestoreDetailCamera()
    {
        if (!cameraSnapshotHeld) return;
        cameraSnapshotHeld = false;
        try
        {
            if (detailCamera != null)
            {
                detailCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                detailCamera.transform.localScale = cameraScale;
                detailCamera.fieldOfView = cameraFieldOfView; detailCamera.rect = cameraRect;
                detailCamera.enabled = cameraEnabled;
            }
        }
        finally { if (detailViewport != null) detailViewport.enabled = viewportEnabled; }
    }
    static void Finish()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged; CatActivity.Completed -= OnCompleted;
        if (!snapshotHeld) return;
        snapshotHeld = false;
        try
        {
            if (activity != null && activity.IsRunning) activity.CancelForTransition();
            if (cat != null) cat.ApplySavedWorldPose(originalPosition, originalRotation);
            if (hunger != null) hunger.ApplySavedValue(hungerValue);
            if (thirst != null) thirst.ApplySavedValue(thirstValue);
            if (energy != null) energy.ApplySavedValue(energyValue);
        }
        catch (Exception exception) { Error += (Error.Length == 0 ? "" : "\n") + "Cleanup: " + exception; }
        finally
        {
            try { RestoreDetailCamera(); }
            catch (Exception exception) { Error += (Error.Length == 0 ? "" : "\n") + "Camera cleanup: " + exception; }
            Time.captureFramerate = previousRate; Time.timeScale = previousScale;
            foreach (var item in states) if (item.Key != null) item.Key.enabled = item.Value;
            if (gameView != null) { gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(gameView, previousSize); gameView.Repaint(); }
            IsRunning = false;
            Status = Error.Length > 0 ? "Failed: " + Error.Split('\n')[0] : stopRequested ? "Stopped; settings restored" : "Complete " + request;
            Directory.CreateDirectory(Screens); File.WriteAllText(Path.Combine(Screens, "capture-status.txt"), Status + "\n" + Error);
            if (report != null && (report.status != "Complete" || Error.Length > 0 || stopRequested))
            {
                report.status = Status; report.error = Error;
                string directory = Path.Combine(Frames, report.kind); Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "capture.json"), JsonUtility.ToJson(report, true));
            }
        }
    }
    static void OnPlayModeChanged(PlayModeStateChange change)
    { if (change == PlayModeStateChange.ExitingPlayMode && IsRunning) { stopRequested = true; Finish(); } }
}

