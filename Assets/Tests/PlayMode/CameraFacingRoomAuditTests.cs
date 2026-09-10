using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CatHome.Economy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Native evidence for the current ROOM inventory and the separately displayed CAT collection.</summary>
public sealed class CameraFacingRoomAuditTests
{
    private const string Output = "Temp/CameraFacingAudit";
    private const string BreedPreference = "cat.identity.breed.v1";
    private const string SoundPreference = "home.audio.sound";
    private const float SettledSeconds = .35f;
    private const float MinimumDot = -.01f;

    [Serializable]
    public sealed class StageRow
    {
        public string room, product, instance, type, kind, breed, stage, status, entrySource;
        public bool entered, completed, naturallyEnded, viewBlocked, facingAssessed, exitClear;
        public int completionEvents, samples;
        public float minDot = 1f, maxDot = -1f, sampledSeconds, elapsedSeconds;
    }

    [Serializable]
    public sealed class AuditReport
    {
        public string suite, generatedUtc, breed, savePath, saveBeforeSha256, saveAfterSha256;
        public int rooms, inventoryProducts, routineInstances, sampledRoutineInstances, decorations;
        public int captureFramerate = 60;
        public float timeScale = 1f;
        public bool finished;
        public List<StageRow> rows = new List<StageRow>();
        public List<string> issues = new List<string>();
        public List<string> noStationaryStage = new List<string>();
    }

    private readonly FieldInfo saveInitialized = typeof(CatHomeSaveSystem).GetField("initialized", BindingFlags.Static | BindingFlags.NonPublic);
    private AuditReport report;
    private string fileStem;
    private HomeStoreSaveState savedStore;
    private EconomySaveState savedEconomy;
    private HomeProgressionSaveState savedHomeProgression;
    private DailyRetentionSaveState savedDaily;
    private AchievementSaveState savedAchievements;
    private QuestProgressEntry[] savedQuests;
    private long savedCoins, savedDiamonds, savedBond;
    private int savedChapter;
    private string savedBreed, rawBreed;
    private int rawSound;
    private bool hadBreed, hadSound, savedSound, wasInitialized, wasRedirectSuppressed, prepared;
    private float savedTimeScale, savedCaptureDelta;
    private float? savedHunger, savedThirst, savedEnergy;
    private UnityEngine.Random.State savedRandom;
    private CatMovement cat;
    private GameObject extraNeeds;
    private CatActivity tracked;
    private int completedEvents;
    private string[] roomProducts, catProducts;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "The camera audit requires an isolated QA save session.");
        savedStore = HomeStoreService.CaptureState();
        savedEconomy = EconomyService.CaptureState();
        savedHomeProgression = HomeProgressionService.CaptureState();
        savedDaily = DailyRetentionService.CaptureState();
        savedAchievements = AchievementService.CaptureState();
        savedCoins = ProgressionService.Coins; savedDiamonds = ProgressionService.Diamonds;
        savedBond = ProgressionService.BondXp; savedChapter = ProgressionService.CurrentChapterNumber;
        savedQuests = ProgressionService.CaptureQuestProgress();
        savedBreed = CatBreedService.SelectedBreedId;
        hadBreed = PlayerPrefs.HasKey(BreedPreference); rawBreed = PlayerPrefs.GetString(BreedPreference);
        hadSound = PlayerPrefs.HasKey(SoundPreference); rawSound = PlayerPrefs.GetInt(SoundPreference);
        savedSound = HomeAudioService.SoundEnabled;
        savedTimeScale = Time.timeScale; savedCaptureDelta = Time.captureDeltaTime;
        savedRandom = UnityEngine.Random.state;
        savedHunger = Object.FindAnyObjectByType<HungerSystem>()?.CurrentHunger;
        savedThirst = Object.FindAnyObjectByType<ThirstSystem>()?.CurrentThirst;
        savedEnergy = Object.FindAnyObjectByType<EnergySystem>()?.CurrentEnergy;
        wasInitialized = (bool)saveInitialized.GetValue(null);
        wasRedirectSuppressed = DirectLevelPlayBootstrap.RedirectSuppressed;
        saveInitialized.SetValue(null, false); // Also suppress SaveNow calls made by temporary CAT display changes.
        report = new AuditReport { generatedUtc = DateTime.UtcNow.ToString("O"), breed = CatBreedCatalog.DefaultBreedId,
            savePath = CatHomeSaveSystem.SaveFilePath, saveBeforeSha256 = SaveHash() };
        roomProducts = HomeStoreService.Products.Where(p => HomeRoomService.Rooms.Any(r =>
                HomeStoreService.IsProductInRoomCollection(r.Id, p.Id))).Select(p => p.Id).Distinct().OrderBy(id => id).ToArray();
        catProducts = HomeStoreService.Products.Where(p => CatCollectionPolicy.IsCatItem(p.Id)).Select(p => p.Id).OrderBy(id => id).ToArray();
        Time.timeScale = 1f; Time.captureFramerate = 60;
        HomeAudioService.SoundEnabled = false;
        CatBreedService.Select(CatBreedCatalog.DefaultBreedId);
        CatActivity.Completed += OnCompleted;
        prepared = true;
    }

    [TearDown]
    public void After()
    {
        if (!prepared) return;
        if (cat != null) CatActionState.CancelForTransition(cat);
        CatActivity.Completed -= OnCompleted;
        saveInitialized.SetValue(null, false);
        HomeStoreService.ApplySavedState(savedStore);
        ProgressionService.ApplySavedState(savedCoins, savedDiamonds, savedBond, savedChapter, savedQuests);
        EconomyService.ApplySavedState(savedEconomy);
        HomeProgressionService.ApplySavedState(savedHomeProgression);
        DailyRetentionService.ApplySavedState(savedDaily);
        AchievementService.ApplySavedState(savedAchievements);
        CatBreedService.Select(savedBreed);
        HomeAudioService.SoundEnabled = savedSound;
        if (hadBreed) PlayerPrefs.SetString(BreedPreference, rawBreed); else PlayerPrefs.DeleteKey(BreedPreference);
        if (hadSound) PlayerPrefs.SetInt(SoundPreference, rawSound); else PlayerPrefs.DeleteKey(SoundPreference);
        PlayerPrefs.Save();
        if (savedHunger.HasValue) Object.FindAnyObjectByType<HungerSystem>()?.ApplySavedValue(savedHunger.Value);
        if (savedThirst.HasValue) Object.FindAnyObjectByType<ThirstSystem>()?.ApplySavedValue(savedThirst.Value);
        if (savedEnergy.HasValue) Object.FindAnyObjectByType<EnergySystem>()?.ApplySavedValue(savedEnergy.Value);
        RoomPlayModeSupport.ReleaseRoom();
        if (extraNeeds != null) Object.DestroyImmediate(extraNeeds);
        DirectLevelPlayBootstrap.RedirectSuppressed = wasRedirectSuppressed;
        Time.timeScale = savedTimeScale; Time.captureDeltaTime = savedCaptureDelta;
        UnityEngine.Random.state = savedRandom;
        report.saveAfterSha256 = SaveHash();
        if (report.saveBeforeSha256 != report.saveAfterSha256) Issue("QA save copy changed during the audit");
        WriteReport();
        saveInitialized.SetValue(null, wasInitialized);
        Assert.That(report.saveAfterSha256, Is.EqualTo(report.saveBeforeSha256), "The audit cannot write even its isolated save copy.");
    }

    private void OnCompleted(CatActivity activity) { if (activity == tracked) completedEvents++; }

    private IEnumerator Prepare(HomeRoomDefinition room)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(room.SceneName);
        saveInitialized.SetValue(null, false);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = roomProducts.Concat(catProducts).Distinct().ToArray();
        state.storedProductIds = catProducts;
        state.currentRoomId = room.Id;
        HomeStoreService.ApplySavedState(state);
        extraNeeds = new GameObject("Camera audit needs");
        if (Object.FindAnyObjectByType<HungerSystem>() == null) extraNeeds.AddComponent<HungerSystem>();
        if (Object.FindAnyObjectByType<ThirstSystem>() == null) extraNeeds.AddComponent<ThirstSystem>();
        yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>();
        if (cat == null) { Issue(room.Id + ": missing cat"); yield break; }
        CatActionState.CancelForTransition(cat);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Physics.SyncTransforms();
        report.rooms++;
    }

    [UnityTest]
    public IEnumerator Rooms_DefaultBreed_AllCurrentRoomProducts()
    {
        fileStem = "room-default"; report.suite = nameof(Rooms_DefaultBreed_AllCurrentRoomProducts);
        report.inventoryProducts = roomProducts.Length;
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return Prepare(room);
            if (cat == null) continue;
            foreach (string product in roomProducts.Where(id => HomeStoreService.IsProductInRoomCollection(room.Id, id)))
                yield return AuditProduct(room.Id, product);
        }
        Finish();
    }

    [UnityTest]
    public IEnumerator Cat_DefaultBreed_CurrentCollectionWithinFiveAndOne()
    {
        fileStem = "cat-default"; report.suite = nameof(Cat_DefaultBreed_CurrentCollectionWithinFiveAndOne);
        report.inventoryProducts = catProducts.Length;
        yield return Prepare(HomeRoomService.Rooms.Single(r => r.Id == HomeRoomService.LivingRoomId));
        if (cat != null)
        {
            foreach (string product in catProducts)
            {
                CatActionState.CancelForTransition(cat);
                foreach (string id in catProducts)
                    if (!HomeStoreService.IsStored(id)) HomeStoreService.TrySetStored(id, true);
                // The real CAT layout planner decides the legal display bay.
                // A single displayed product respects both the five-item and one-bed limits.
                bool displayed = HomeStoreService.TrySetStored(product, false);
                yield return null; yield return null;
                Physics.SyncTransforms();
                int visibleBeds = catProducts.Count(id => CatCollectionPolicy.IsBed(id) && !HomeStoreService.IsStored(id));
                if (!displayed || CatCollectionPolicy.DisplayedCount > CatCollectionPolicy.Capacity || visibleBeds > 1)
                {
                    Issue(product + ": could not display within CAT 5/1");
                    AddEmpty(HomeRoomService.LivingRoomId, product, "display_failed");
                    continue;
                }
                yield return AuditProduct(HomeRoomService.LivingRoomId, product);
                HomeStoreService.TrySetStored(product, true);
                yield return null;
            }
        }
        Finish();
    }

    private IEnumerator AuditProduct(string room, string product)
    {
        var activities = Object.FindObjectsByType<CatActivity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(a => a.gameObject.scene == cat.gameObject.scene && a.StoreProductId == product && !a.IsRetired)
            .OrderBy(a => HierarchyPath(a.transform)).ToArray();
        if (activities.Length == 0)
        {
            bool decoration = product == HomeStoreService.StereoId || product == HomeStoreService.GameConsoleId || product == HomeStoreService.TvUnitId;
            AddEmpty(room, product, decoration ? "intentional_decoration" : "missing_activity");
            if (decoration) report.decorations++;
            else Issue(room + "/" + product + ": missing activity");
            WriteReport();
            yield break;
        }
        for (int index = 0; index < activities.Length; index++)
        {
            yield return Observe(room, product, activities[index], index + 1);
            WriteReport(); // Keep all earlier evidence if a later native routine fails unexpectedly.
        }
    }

    [UnityTest]
    public IEnumerator FreeScenes_DefaultBreed_FourAuthoredActivities()
    {
        fileStem = "free-default"; report.suite = nameof(FreeScenes_DefaultBreed_FourAuthoredActivities);
        report.inventoryProducts = 0; // These scene activities are not store inventory.
        long requiredBond = Math.Max(BondMilestoneService.WindowWatchBond, BondMilestoneService.BirdWatchBond);
        ProgressionService.ApplySavedState(savedCoins, savedDiamonds, Math.Max(savedBond, requiredBond), savedChapter, savedQuests);
        var expected = new Dictionary<string, string[]>
        {
            { HomeRoomService.LivingRoomId, new[] { "SofaLounge", "CoffeeTablePlay" } },
            { HomeRoomService.GardenId, new[] { "BirdWatchActivity", "YarnChaseActivity" } }
        };
        foreach (var pair in HomeRoomService.Rooms.Select(room => new KeyValuePair<string,string[]>(room.Id,
                     expected.TryGetValue(room.Id,out var names) ? names : new string[0])))
        {
            yield return Prepare(HomeRoomService.Rooms.Single(room => room.Id == pair.Key));
            if (cat == null) continue;
            var activities = Object.FindObjectsByType<CatActivity>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(activity => activity.gameObject.scene == cat.gameObject.scene &&
                    string.IsNullOrEmpty(activity.StoreProductId) && !activity.IsRetired && !(activity is CatCommandActivity))
                .OrderBy(activity => HierarchyPath(activity.transform)).ToArray();
            foreach (string name in pair.Value)
                if (activities.Count(activity => activity.name == name) != 1) Issue(pair.Key + ": expected one free scene action " + name);
            foreach (var activity in activities)
            {
                if (!pair.Value.Contains(activity.name)) Issue(pair.Key + ": unexpected free scene action " + activity.name);
                if (activity is SitLookActivity view && view.VisibleLookTargetCount == 0)
                    Issue(pair.Key + "/" + activity.name + ": no baked real scene surface for gaze");
                yield return Observe(pair.Key, "free." + activity.ActivityId, activity, 1);
                WriteReport();
            }
        }
        if (report.routineInstances != 4) Issue("Expected all four free scene routines; found " + report.routineInstances);
        Finish();
    }

    private IEnumerator Observe(string room, string product, CatActivity activity, int ordinal)
    {
        report.routineInstances++;
        var stages = new List<StageRow>();
        string instance = HierarchyPath(activity.transform) + "#" + ordinal;
        string label = room + "/" + product + "/" + ordinal;
        CatActionState.CancelForTransition(cat);
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        RoomPlayModeSupport.ProvisionNeeds();
        UnityEngine.Random.InitState(90209 + report.routineInstances * 31);
        tracked = activity; completedEvents = 0;
        string entrySource;
        bool entered = TryEnter(activity, out entrySource);
        if (!entered)
        {
            var failed = NewRow(room, product, activity, instance, "entry", entrySource);
            failed.status = "entry_failed"; report.rows.Add(failed);
            Issue(label + ": no clear, nearby entry started the activity");
            yield break;
        }
        var animation = cat.GetComponent<CatActivityAnimation>();
        var bones = cat.GetComponentsInChildren<Transform>();
        Transform hips = bones.FirstOrDefault(b => b.name == "DEF-spine");
        Transform shoulders = bones.FirstOrDefault(b => b.name == "DEF-spine.003");
        if (animation == null || hips == null || shoulders == null) Issue(label + ": missing rendered body landmarks");
        float startedAt = Time.time, deadline = Time.realtimeSinceStartup + 65f;
        float stableFor = 0f;
        Vector3 previousPosition = cat.transform.position;
        Quaternion previousRotation = cat.transform.rotation;
        CatActivityPose previousPose = CatActivityPose.Walk;
        Transform previousSurface = null;
        StageRow stage = null;
        int stageIndex = 0;
        bool anySample = false;
        while (activity.IsRunning && Time.realtimeSinceStartup < deadline && Time.time - startedAt < 50f)
        {
            yield return new WaitForEndOfFrame();
            if (!activity.IsRunning || cat == null) break;
            CatActivityPose pose = animation != null ? animation.CurrentPose : CatActivityPose.Walk;
            Transform surface = animation != null ? animation.ContactSurface : null;
            Vector3 delta = cat.transform.position - previousPosition; delta.y = 0f;
            bool traveling = IsTravelPose(pose) || delta.magnitude > Mathf.Max(.002f, Time.deltaTime * .20f);
            // A staged turn can retain the previous held pose while its root
            // rotates. Wait for that actual turn to settle, without excluding
            // a stationary backwards work pose or slow support sway.
            bool turning = Quaternion.Angle(previousRotation, cat.transform.rotation) > Mathf.Max(.1f, Time.deltaTime * 30f);
            bool changed = pose != previousPose || surface != previousSurface;
            if (traveling || turning || changed)
            {
                stableFor = 0f;
                stage = null;
            }
            else stableFor += Time.deltaTime;
            previousPose = pose; previousSurface = surface; previousPosition = cat.transform.position;
            previousRotation = cat.transform.rotation;
            if (!traveling && !turning && stableFor >= SettledSeconds && hips != null && shoulders != null)
            {
                Vector3 forward = shoulders.position - hips.position; forward.y = 0f;
                if (forward.sqrMagnitude > .000001f)
                {
                    if (stage == null)
                    {
                        stage = NewRow(room, product, activity, instance,
                            (++stageIndex) + ":" + pose + (activity.IsWaitingForRestStop ? ":held" : ":work"), entrySource);
                        stages.Add(stage);
                        if (File.Exists(Output + "/capture.enabled"))
                        {
                            string folder = Output + "/frames/" + room;
                            Directory.CreateDirectory(folder);
                            string identity;
                            using (var digest = SHA256.Create())
                                identity = BitConverter.ToString(digest.ComputeHash(Encoding.UTF8.GetBytes(instance))).Replace("-", "").Substring(0, 10);
                            string name = product.Replace('.', '_') + "_" + identity + "_" + stageIndex + "_" + pose + ".png";
                            ScreenCapture.CaptureScreenshot(folder + "/" + name);
                        }
                    }
                    float dot = CatActivityFacing.FacingDot(forward, (hips.position + shoulders.position) * .5f,
                        CatActivityFacing.CameraPosition(cat));
                    stage.minDot = Mathf.Min(stage.minDot, dot); stage.maxDot = Mathf.Max(stage.maxDot, dot);
                    stage.samples++; stage.sampledSeconds += Time.deltaTime; stage.facingAssessed = true;
                    stage.viewBlocked |= activity is SitLookActivity view && view.ViewStandBlocked;
                    anySample = true;
                }
            }
            // The fixture supplies the player's explicit stop only after a
            // meaningful held pose has been available to the camera.
            if (activity.IsWaitingForRestStop && activity.RestingSeconds >= 1f) activity.RequestRestStop();
        }
        bool naturallyEnded = !activity.IsRunning;
        bool completed = naturallyEnded && completedEvents == 1;
        float elapsedSeconds = Time.time - startedAt;
        if (activity.IsRunning) activity.CancelForTransition();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        bool exitClear = CatActivityMotion.IsFloorClear(cat.transform.position, .24f);
        if (!naturallyEnded) Issue(label + ": timed out");
        else if (!completed) Issue(label + ": ended without one completion (" + completedEvents + ")");
        if (!exitClear || cat.IsMovementPhysicallyLocked || !cat.GetComponent<CharacterController>().enabled)
            Issue(label + ": exit or control release failed");
        if (!anySample)
        {
            var empty = NewRow(room, product, activity, instance, "no_stationary_stage", entrySource);
            empty.status = "not_assessed"; stages.Add(empty);
            report.noStationaryStage.Add(label);
        }
        else report.sampledRoutineInstances++;
        foreach (var row in stages)
        {
            row.entered = true; row.completed = completed; row.naturallyEnded = naturallyEnded;
            row.completionEvents = completedEvents; row.exitClear = exitClear; row.elapsedSeconds = elapsedSeconds;
            row.viewBlocked |= activity is SitLookActivity view && view.ViewStandBlocked;
            if (row.facingAssessed)
            {
                row.status = row.minDot < MinimumDot || row.viewBlocked ? "back_or_blocked" : "visible_stable_work";
                if (row.minDot < MinimumDot || row.viewBlocked)
                    Issue(label + "/" + row.stage + ": minDot=" + row.minDot.ToString("F3", CultureInfo.InvariantCulture) +
                        (row.viewBlocked ? ", viewing bay blocked" : ""));
            }
            report.rows.Add(row);
        }
        tracked = null;
    }

    private bool TryEnter(CatActivity activity, out string source)
    {
        source = "none";
        if (!activity.isActiveAndEnabled || !activity.IsUnlocked || !activity.IsContentVisible) return false;
        var candidates = new List<Vector3>();
        if (activity.RoutineEntryPoint != null) candidates.Add(activity.RoutineEntryPoint.position);
        var placement = activity.GetComponent<HomeProductPlacement>();
        Transform visual = activity is LivingFurnitureActivity furniture ? furniture.SelectionVisual : null;
        if (visual == null && placement != null) visual = placement.MovableRoot;
        if (visual != null)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>()
                .Where(r => r.enabled && !(r is ParticleSystemRenderer) && !(r is LineRenderer)).ToArray();
            foreach (var renderer in renderers)
            {
                // A distant radial probe of a world AABB mostly repeats its
                // four corners. Sample every actual local edge instead, so a
                // wide wall picture or TV has candidates along its clear face.
                Bounds bounds = renderer.localBounds;
                for (int edge = 0; edge < 4; edge++)
                for (int step = 0; step <= 12; step++)
                foreach (float clearance in new[] { .30f, .48f, .66f })
                {
                    float t = step / 12f;
                    Vector3 point = bounds.center;
                    Vector3 localOutward;
                    if (edge < 2)
                    {
                        point.x = Mathf.Lerp(bounds.min.x, bounds.max.x, t);
                        point.z = edge == 0 ? bounds.min.z : bounds.max.z;
                        localOutward = edge == 0 ? Vector3.back : Vector3.forward;
                    }
                    else
                    {
                        point.x = edge == 2 ? bounds.min.x : bounds.max.x;
                        point.z = Mathf.Lerp(bounds.min.z, bounds.max.z, t);
                        localOutward = edge == 2 ? Vector3.left : Vector3.right;
                    }
                    Vector3 outward = renderer.transform.TransformVector(localOutward); outward.y = 0f;
                    if (outward.sqrMagnitude < .0001f) continue;
                    candidates.Add(renderer.transform.TransformPoint(point) + outward.normalized * clearance);
                }
                // Furniture and a wall may block all four straight face strips
                // while leaving a real rounded corner bay. Cover that nearby
                // floor without expanding the runtime .80 m visibility gate.
                foreach (float x in new[] { bounds.min.x,bounds.max.x })
                foreach (float z in new[] { bounds.min.z,bounds.max.z })
                for (int angle = 1; angle < 6; angle++)
                foreach (float clearance in new[] { .30f,.48f,.66f })
                {
                    float radians = angle * 15f * Mathf.Deg2Rad;
                    Vector3 local = new Vector3((x == bounds.min.x ? -1f : 1f)*Mathf.Cos(radians),0,
                        (z == bounds.min.z ? -1f : 1f)*Mathf.Sin(radians));
                    Vector3 outward = renderer.transform.TransformVector(local); outward.y=0f;
                    if(outward.sqrMagnitude<.0001f)continue;
                    candidates.Add(renderer.transform.TransformPoint(new Vector3(x,bounds.center.y,z))+outward.normalized*clearance);
                }
            }
        }
        var controller = cat.GetComponent<CharacterController>();
        for (int i = 0; i < candidates.Count; i++)
        {
            Vector3 point = candidates[i]; point.y = .05f;
            if (!CatActivityMotion.IsFloorClear(point, .27f)) continue;
            controller.enabled = false;
            cat.transform.SetPositionAndRotation(point, Quaternion.identity);
            controller.enabled = true; Physics.SyncTransforms();
            if (!activity.TryGetPromptDistance(cat, out _)) continue;
            source = i == 0 && activity.RoutineEntryPoint != null ? "authored" : "clear_perimeter_" + i;
            if (activity.TryStart(cat)) return true;
        }
        return false;
    }

    private static bool IsTravelPose(CatActivityPose pose) => pose == CatActivityPose.Walk || pose == CatActivityPose.Hop ||
        pose == CatActivityPose.Crawl || pose == CatActivityPose.Stalk;

    private static StageRow NewRow(string room, string product, CatActivity activity, string instance, string stage, string entrySource) =>
        new StageRow { room = room, product = product, instance = instance, type = activity.GetType().Name,
            kind = activity.Kind.ToString(), breed = CatBreedService.SelectedBreedId, stage = stage, entrySource = entrySource };

    private void AddEmpty(string room, string product, string status) => report.rows.Add(new StageRow {
        room = room, product = product, breed = CatBreedCatalog.DefaultBreedId, stage = status, status = status });

    private void Issue(string issue) { if (!report.issues.Contains(issue)) report.issues.Add(issue); }

    private void Finish()
    {
        report.finished = true; report.saveAfterSha256 = SaveHash();
        if (report.saveAfterSha256 != report.saveBeforeSha256) Issue("QA save copy changed during the audit");
        WriteReport();
        Assert.That(report.issues, Is.Empty, string.Join("\n", report.issues));
    }

    private void WriteReport()
    {
        if (report == null || string.IsNullOrEmpty(fileStem)) return;
        Directory.CreateDirectory(Output);
        var csv = new StringBuilder("room,product,instance,type,kind,breed,stage,status,entrySource,entered,completed,naturallyEnded,completionEvents,samples,minDot,maxDot,sampledSeconds,elapsedSeconds,viewBlocked,facingAssessed,exitClear\n");
        foreach (var r in report.rows)
            csv.AppendLine(string.Join(",", new[] { r.room, r.product, r.instance, r.type, r.kind, r.breed, r.stage, r.status, r.entrySource,
                r.entered.ToString(), r.completed.ToString(), r.naturallyEnded.ToString(), r.completionEvents.ToString(), r.samples.ToString(),
                r.facingAssessed ? r.minDot.ToString("F4", CultureInfo.InvariantCulture) : "",
                r.facingAssessed ? r.maxDot.ToString("F4", CultureInfo.InvariantCulture) : "",
                r.sampledSeconds.ToString("F3", CultureInfo.InvariantCulture), r.elapsedSeconds.ToString("F3", CultureInfo.InvariantCulture),
                r.viewBlocked.ToString(), r.facingAssessed.ToString(), r.exitClear.ToString() }.Select(Csv)));
        File.WriteAllText(Path.Combine(Output, fileStem + ".csv"), csv.ToString());
        File.WriteAllText(Path.Combine(Output, fileStem + ".json"), JsonUtility.ToJson(report, true));
    }

    private static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    private static string HierarchyPath(Transform value) => value.parent == null ? value.name : HierarchyPath(value.parent) + "/" + value.name;
    private static string SaveHash()
    {
        string path = CatHomeSaveSystem.SaveFilePath;
        if (!File.Exists(path)) return "absent";
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
}
