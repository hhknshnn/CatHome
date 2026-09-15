using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Manual and recorded CAT review in the furnished room, on an isolated save only.</summary>
[InitializeOnLoad]
public sealed class LivingCatReview : EditorWindow
{
    public const string Root = "Docs/QA/LIVING_CAT_REVIEW_2026-09-11";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    const string ManualKey = "CatHome.LivingCatReview.Manual";
    const string PreferencesKey = "CatHome.LivingCatReview.Preferences";
    const string ViewKey = "CatHome.LivingCatReview.View";
    public static readonly string[][] Groups = {
        new[] { "home.ball-basket", "home.scratch-post", "cat.play-tunnel", "cat.bell-collar", "cat.nap-pillow" },
        new[] { "cat.cozy-pod-bed", "cat.toy-mouse", "cat.ceramic-bowl", "cat.feather-toy", "cat.collar" },
        new[] { "cat.cloud-bed", "cat.treat-jar", "cat.leash", "cat.kibble-bag" },
        new[] { "cat.canopy-bed", "cat.catnip-plant", "cat.cardboard-hideout" }
    };
    public static string Status { get; private set; } = "Hazır";
    public static bool Recording { get; private set; }
    static int group;
    Vector2 scroll;

    static LivingCatReview() => EditorApplication.playModeStateChanged += state =>
    {
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ManualKey, false))
            EditorApplication.delayCall += FinishManualReview;
    };

    public static void ArmManualReview()
    {
        RequireCat();
        SessionState.SetString(PreferencesKey, File.ReadAllText(Root + "/preferences-before.json"));
        SessionState.SetString(ViewKey, File.ReadAllText(Root + "/view-before.json"));
        SessionState.SetBool(ManualKey, true);
        Open();
    }
    static void FinishManualReview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var preferences = Newtonsoft.Json.Linq.JArray.Parse(SessionState.GetString(PreferencesKey, "[]"));
        var language = preferences.FirstOrDefault(p => (string)p["key"] == GameLanguageService.PlayerPrefsKey);
        if (language != null) GameLanguageService.SetLanguage((GameLanguage)(int)language["number"]);
        foreach (var item in preferences)
        {
            string key = (string)item["key"];
            if (!(bool)item["existed"]) PlayerPrefs.DeleteKey(key);
            else if ((bool)item["isString"]) PlayerPrefs.SetString(key, (string)item["text"]);
            else PlayerPrefs.SetInt(key, (int)item["number"]);
        }
        PlayerPrefs.Save();
        UiQaTestSession.End();
        CatHomeEditPreview.Refresh();
        var view = Newtonsoft.Json.Linq.JObject.Parse(SessionState.GetString(ViewKey, "{}"));
        if (view["size"] != null)
        {
            var window = EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView"));
            window.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(window, (int)view["size"]);
            Time.timeScale = (float)view["timeScale"]; Time.captureFramerate = (int)view["capture"];
        }
        SessionState.EraseBool(ManualKey); SessionState.EraseString(PreferencesKey); SessionState.EraseString(ViewKey);
        Status = "Deneme bitti · normal salon ve tercihler geri yüklendi";
    }

    [MenuItem("Tools/Cat Home/Salon Eşya Denemesi")]
    public static void Open() => GetWindow<LivingCatReview>("Salon eşya denemesi");

    void OnInspectorUpdate() => Repaint();
    void OnGUI()
    {
        EditorGUILayout.LabelField("Salon · 17 kedi eşyası", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Deneme kayıt kopyasında çalışır. Bir grubu göster; eşyaya yaklaş ve oyundaki düğmeyle dene. Yataklarda Kalk ile çık.", MessageType.Info);
        EditorGUILayout.LabelField(Status, EditorStyles.wordWrappedLabel);
        if (!Application.isPlaying || !EditorQaSession.IsActive)
        {
            EditorGUILayout.HelpBox("Ayrı deneme kaydıyla Play oturumu gerekli.", MessageType.Info);
            return;
        }
        using (new EditorGUI.DisabledScope(Recording || CatActivity.Active != null))
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < Groups.Length; i++)
                if (GUILayout.Button("Grup " + (i + 1))) Try(() => ShowGroup(i));
            EditorGUILayout.EndHorizontal();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (string id in Groups[group])
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(HomeStoreService.TryGetProduct(id, out var p) ? p.Title : id);
                if (GUILayout.Button("Yanına getir", GUILayout.Width(100))) Try(() => PrepareProduct(id));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        if (GUILayout.Button("Dinlenmeden kalk / eylemi durdur"))
        {
            if (CatActivity.Active != null && !CatActivity.Active.RequestRestStop())
                CatActivity.Active.CancelForTransition();
        }
        if (SessionState.GetBool(ManualKey, false) && GUILayout.Button("Denemeyi bitir · normal salona dön"))
            EditorApplication.isPlaying = false;
    }
    static void Try(Action action)
    { try { action(); } catch (Exception e) { Status = e.Message; Debug.LogException(e); } }

    static CatMovement RequireCat()
    {
        if (!Application.isPlaying || !EditorQaSession.IsActive) throw new InvalidOperationException("Isolated Play required.");
        var cat = Object.FindAnyObjectByType<CatMovement>();
        if (cat == null || cat.gameObject.scene.path != HomeRoomService.LivingRoomScenePath)
            throw new InvalidOperationException("Ready living room required.");
        return cat;
    }
    public static void ShowGroup(int index)
    {
        var cat = RequireCat();
        if (index < 0 || index >= Groups.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (CatActivity.Active != null) throw new InvalidOperationException("Önce eylemin bitmesini bekle.");
        UiQaVisualTour.Clear();
        foreach (string id in Groups.SelectMany(x => x))
            if (!HomeStoreService.IsOwned(id)) throw new InvalidOperationException("Missing owned product: " + id);
        foreach (string id in Groups.SelectMany(x => x)) HomeStoreService.TrySetStored(id, true);
        foreach (string id in Groups[index])
            if (!HomeStoreService.TrySetStored(id, false)) throw new InvalidOperationException("Cannot display " + id);
        Physics.SyncTransforms();
        CatActivityMotion.KeepCatClearAfterPurchase(cat.gameObject.scene);
        group = index;
        Status = "Grup " + (index + 1) + " hazır · " + Groups[index].Length + " eşya";
    }
    public static CatActivity PrepareProduct(string id, int side = 0)
    {
        var cat = RequireCat();
        CatActionState.CancelForTransition(cat);
        UiQaVisualTour.Clear();
        Object.FindAnyObjectByType<EnergySystem>()?.ApplySavedValue(70);
        Object.FindAnyObjectByType<HungerSystem>()?.ApplySavedValue(35);
        Object.FindAnyObjectByType<ThirstSystem>()?.ApplySavedValue(35);
        var a = Object.FindObjectsByType<CatActivity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(x => x.StoreProductId == id && !x.IsRetired);
        var cc = cat.GetComponent<CharacterController>();
        Vector3 previous = cat.transform.position;
        bool wasEnabled = cc.enabled;
        var points = new List<Vector3>();
        Vector3 center = a.transform.position;
        Vector3 entry = a.RoutineEntryPoint.position; entry.y = 0;
        // Search the real player's selectable perimeter, with the full walking capsule.
        cc.enabled = false;
        try
        {
            for (int x = -15; x <= 15; x++)
            for (int z = -15; z <= 15; z++)
            {
                var p = new Vector3(center.x + x * .10f, 0, center.z + z * .10f);
                if (!CatActivityMotion.IsFloorClear(p, Mathf.Max(.27f, CatActivityMotion.ControllerFloorRadius(cat)))) continue;
                cat.transform.position = p;
                if (a.TryGetPromptDistance(cat, out float distance)) points.Add(p);
            }
            // The ball also needs a real roll corridor past its first contact.
            // Pick a valid test starting point; keep the game's refusal intact.
            if (a is BallChaseActivity basket)
            {
                var field = typeof(BallChaseActivity).GetField("ballCollision", Flags);
                var method = typeof(BallChaseActivity).GetMethod("ChooseDirection", Flags);
                using (var collision = new CatToyBallCollision(a.gameObject.scene, basket.Ball))
                {
                    field.SetValue(basket, collision);
                    try { points.RemoveAll(p => !(bool)method.Invoke(basket, new object[] {
                        p, (p - a.transform.position).normalized, 1.30f, Vector3.zero })); }
                    finally { field.SetValue(basket, null); }
                }
            }
            if (points.Count == 0) throw new InvalidOperationException("No selectable open floor: " + id);
            var ordered = points.OrderBy(p => (p - entry).sqrMagnitude).ToArray();
            Vector3 chosen = side == 0 ? ordered[Mathf.Min(3, ordered.Length - 1)] :
                ordered.OrderByDescending(p => (p - ordered[0]).sqrMagnitude).First();
            cat.transform.SetPositionAndRotation(chosen, Quaternion.LookRotation((a.transform.position - chosen).normalized));
            var euler = cat.transform.eulerAngles; cat.transform.rotation = Quaternion.Euler(0, euler.y, 0);
        }
        catch { cat.transform.position = previous; throw; }
        finally { cc.enabled = wasEnabled; Physics.SyncTransforms(); }
        var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
        typeof(ActivityPromptController).GetField("selected", Flags).SetValue(prompt, a);
        ActivityPromptController.NotifyActivityChanged();
        Status = a.DisplayName + " · oyundaki eylem düğmesi hazır";
        return a;
    }
    static Button SelectButton(CatActivity a)
    {
        var prompt = Object.FindAnyObjectByType<ActivityPromptController>();
        typeof(ActivityPromptController).GetField("selected", Flags).SetValue(prompt, a);
        ActivityPromptController.NotifyActivityChanged();
        var button = (Button)typeof(ActivityPromptController).GetField("actionButton", Flags).GetValue(prompt);
        if (!button.isActiveAndEnabled || !button.interactable) throw new InvalidOperationException("No visible action button: " + a.StoreProductId);
        return button;
    }
    [Serializable] public sealed class Sample
    {
        public float time, x, y, z, yaw, dot, delta, turn;
        public string pose;
        public int contacts;
        public bool work, resting;
    }
    [Serializable] public sealed class Row
    {
        public string id, title, breed, error;
        public bool started, completed, exitClear, released;
        public int events, contacts, frames;
        public float duration, walked, maxFrameTurn, stationaryWalkSeconds, workMinDot = 1;
        public List<Sample> samples = new List<Sample>();
    }
    [Serializable] public sealed class Report { public bool finished; public List<Row> rows = new List<Row>(); }
    public static void Audit(string label, bool record = true, string only = null, int side = 0)
    {
        RequireCat();
        if (Recording) throw new InvalidOperationException("Review already running.");
        if (label.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException(nameof(label));
        Recording = true;
        Object.FindAnyObjectByType<LevelLoader>().StartCoroutine(Run(label, record, only, side));
    }
    static void Still(string path)
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(path, texture.EncodeToPNG()); } finally { Object.Destroy(texture); }
    }
    static IEnumerator Run(string label, bool record, string only, int side)
    {
        var cat = RequireCat();
        var idle = cat.GetComponent<CatIdleBehavior>(); bool idleEnabled = idle != null && idle.enabled;
        int fps = Time.captureFramerate; float scale = Time.timeScale;
        string folder = Root + "/" + label; Directory.CreateDirectory(folder);
        var report = new Report();
        CatActivity tracked = null; int completions = 0;
        Action<CatActivity> completed = a => { if (a == tracked) completions++; };
        CatActivity.Completed += completed;
        try
        {
            if (idle != null) idle.enabled = false;
            Time.timeScale = 1; Time.captureFramerate = 24;
            UiQaVisualTour.Clear();
            yield return new WaitForSeconds(.6f);
            for (int g = 0; g < Groups.Length; g++)
            {
                if (only != null && !Groups[g].Contains(only)) continue;
                ShowGroup(g);
                yield return new WaitForSeconds(.5f);
                foreach (string id in Groups[g])
                {
                    if (only != null && only != id) continue;
                    var row = new Row { id = id, breed = CatBreedService.SelectedBreedId };
                    report.rows.Add(row);
                    CatActivity a = null;
                    try { a = PrepareProduct(id, side); row.title = a.DisplayName; }
                    catch (Exception e) { row.error = e.Message; }
                    if (a == null) { Write(folder, report); continue; }
                    yield return new WaitForSeconds(.25f);
                    Status = label + " · " + row.title;
                    string frames = "Library/LivingCatReview/" + label + "/" + id;
                    if (record) Directory.CreateDirectory(frames);
                    yield return new WaitForEndOfFrame(); Still(folder + "/" + id + "-before.png");
                    tracked = a; completions = 0;
                    CatActivityAnimation animation = null;
                    var hips = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine");
                    var shoulders = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.003");
                    Vector3 previous = cat.transform.position; Quaternion rotation = cat.transform.rotation;
                    float began = Time.time, deadline = Time.realtimeSinceStartup + 100;
                    try { SelectButton(a).onClick.Invoke(); row.started = a.IsRunning; }
                    catch (Exception e) { row.error = e.Message; }
                    animation = cat.GetComponent<CatActivityAnimation>();
                    if (!row.started && string.IsNullOrEmpty(row.error)) row.error = "Visible action was refused";
                    bool shot = false;
                    while (a.IsRunning && Time.time - began < 40 && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        var p = cat.transform.position; float distance = Vector3.Distance(previous, p);
                        float turn = Quaternion.Angle(rotation, cat.transform.rotation);
                        var enrichment = a as CatEnrichmentActivity;
                        bool working = enrichment != null && enrichment.IsPerformingGesture || a.IsWaitingForRestStop ||
                            a is ScratchPostActivity && animation.CurrentPose == CatActivityPose.Scratch ||
                            a is BallChaseActivity && (animation.CurrentPose == CatActivityPose.BatLeft || animation.CurrentPose == CatActivityPose.BatRight);
                        float dot = CatActivityFacing.FacingDot(shoulders.position - hips.position,
                            (shoulders.position + hips.position) * .5f, CatActivityFacing.CameraPosition(cat));
                        row.walked += distance; row.maxFrameTurn = Mathf.Max(row.maxFrameTurn, turn);
                        if (animation.CurrentPose == CatActivityPose.Walk && distance < .003f) row.stationaryWalkSeconds += Time.deltaTime;
                        if (working) row.workMinDot = Mathf.Min(row.workMinDot, dot);
                        row.samples.Add(new Sample { time = Time.time - began, x = p.x, y = p.y, z = p.z,
                            yaw = cat.transform.eulerAngles.y, pose = animation.CurrentPose.ToString(), dot = dot,
                            delta = distance, turn = turn, work = working, resting = a.IsWaitingForRestStop,
                            contacts = enrichment != null ? enrichment.ContactCount :
                                a is ScratchPostActivity post ? post.LeftStrokes + post.RightStrokes :
                                a is BallChaseActivity play ? play.CatchCount : 0 });
                        if (!shot && (working || a.IsWaitingForRestStop || Time.time - began > 2))
                        { Still(folder + "/" + id + "-work.png"); shot = true; }
                        if (record)
                        {
                            var texture = ScreenCapture.CaptureScreenshotAsTexture();
                            File.WriteAllBytes(frames + "/" + row.frames.ToString("D4") + ".jpg", texture.EncodeToJPG(90));
                            Object.Destroy(texture);
                        }
                        row.frames++; previous = p; rotation = cat.transform.rotation;
                        if (a.IsWaitingForRestStop && a.RestingSeconds >= 2) SelectButton(a).onClick.Invoke();
                    }
                    row.duration = Time.time - began; row.events = completions;
                    row.completed = row.started && !a.IsRunning && completions == 1;
                    row.contacts = a is CatEnrichmentActivity product ? product.ContactCount :
                        a is BallChaseActivity basket ? basket.CatchCount :
                        a is ScratchPostActivity scratch ? scratch.LeftStrokes + scratch.RightStrokes : 0;
                    if (!row.completed && string.IsNullOrEmpty(row.error) && a is BallChaseActivity interrupted)
                        row.error = interrupted.InterruptedReason;
                    if (a.IsRunning) { row.error = "Routine timeout"; a.CancelForTransition(); }
                    yield return new WaitForSeconds(1.5f);
                    row.exitClear = CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position);
                    row.released = !cat.IsMovementPhysicallyLocked && cat.GetComponent<CharacterController>().enabled;
                    yield return new WaitForEndOfFrame(); Still(folder + "/" + id + "-after.png");
                    Write(folder, report);
                }
            }
            report.finished = true; Write(folder, report); Status = label + " tamamlandı · " + report.rows.Count + " eşya";
        }
        finally
        {
            if (tracked != null && tracked.IsRunning) tracked.CancelForTransition();
            CatActivity.Completed -= completed;
            if (idle != null) idle.enabled = idleEnabled;
            Time.captureFramerate = fps; Time.timeScale = scale; Recording = false;
            if (!report.finished) { Status = "Kontrol durdu; raporu incele"; Write(folder, report); }
        }
    }
    static void Write(string folder, Report report) => File.WriteAllText(folder + "/report.json", JsonUtility.ToJson(report, true));
}
