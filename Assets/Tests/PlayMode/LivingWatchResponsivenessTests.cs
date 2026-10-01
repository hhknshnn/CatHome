#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class LivingWatchResponsivenessTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CatMovement cat;
    GameObject ui, blocker;
    Button button;
    ActivityPromptController prompt;
    HomeStoreSaveState savedStore;
    readonly List<string> evidence = new List<string>();
    string Output => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Docs/QA/LIVING_WATCH_2026-09-10");
    static void Set(object owner, string field, object value) => owner.GetType().GetField(field, Private).SetValue(owner, value);

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        savedStore = HomeStoreService.CaptureState(); evidence.Clear();
    }
    [TearDown] public void After()
    {
        Time.timeScale = 1;
        if (cat != null) CatActionState.CancelForTransition(cat);
        if (ui != null) Object.DestroyImmediate(ui);
        if (blocker != null) Object.DestroyImmediate(blocker);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(savedStore);
        Directory.CreateDirectory(Output);
        File.WriteAllLines(Path.Combine(Output, TestContext.CurrentContext.Test.Name + ".txt"), evidence);
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        // Reproduce the current furnished room, including the armchair and
        // displayed CAT props that constrain the lamp's narrow access aisle.
        string fixture = Path.Combine(Output, "fixture-store.json");
        var collection = File.Exists(fixture) ? JsonUtility.FromJson<HomeStoreSaveState>(File.ReadAllText(fixture)) : savedStore;
        HomeStoreService.ApplySavedState(collection);
        foreach (var id in new[] { HomeStoreService.BookshelfId, HomeStoreService.TallPlantId, HomeStoreService.FloorLampId })
            HomeStoreService.TryAcquireForTesting(id);
        yield return null; yield return null;
        // Provisioning the isolated collection can create a persistent level-up
        // overlay. Dismiss that fixture-only celebration before testing buttons;
        // the production modal gate must remain effective.
        foreach (var celebration in Object.FindObjectsByType<HomeLevelUpCelebrationView>(FindObjectsInactive.Include))
            Object.DestroyImmediate(celebration.gameObject);
        cat = Object.FindAnyObjectByType<CatMovement>();
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        ui = new GameObject("Actual observation button fixture", typeof(RectTransform)); ui.SetActive(false);
        var buttonObject = new GameObject("Watch", typeof(RectTransform), typeof(Button));
        buttonObject.transform.SetParent(ui.transform, false);
        button = buttonObject.GetComponent<Button>(); prompt = ui.AddComponent<ActivityPromptController>();
        Set(prompt, "actionButton", button); ui.SetActive(true);
        Assert.That(Object.FindObjectsByType<CatActivity>(FindObjectsInactive.Include)
            .Any(a => a.Kind == CatActivityKind.WindowWatch), Is.False);
        Assert.That(BondMilestoneService.Milestones.Any(m => m.ActivityKind == CatActivityKind.WindowWatch), Is.False);
        yield return null;
    }
    SitLookActivity Find(string id) => Object.FindObjectsByType<SitLookActivity>().Single(a => a.StoreProductId == id);
    IEnumerator Place(SitLookActivity activity, Vector3 position)
    {
        CatActionState.CancelForTransition(cat); yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        RoomPlayModeSupport.ProvisionNeeds();
        var cc = cat.GetComponent<CharacterController>(); cc.enabled = false;
        position.y = .05f; cat.transform.SetPositionAndRotation(position, Quaternion.identity); cc.enabled = true;
        Physics.SyncTransforms(); yield return null;
        Set(prompt, "selected", activity);
        typeof(ActivityPromptController).GetMethod("RefreshImmediate", Private).Invoke(prompt, null);
        Assert.That(activity.TryGetPromptDistance(cat, out _), Is.True, activity.StoreProductId + " nearby prompt");
        Assert.That(HomeUiFlow.IsHomeControlBlocked, Is.False, "Fixture must dismiss its own provisioning popup");
        Assert.That(button.isActiveAndEnabled, Is.True, "Click only an actually visible button");
    }
    [UnityTest] public IEnumerator RealButtons_ThreeObservationsRepeatWithoutStallAndAnimate()
    {
        yield return Prepare();
        foreach (var row in new[] {
            (HomeStoreService.BookshelfId, new Vector3(-.5f,0,1.7f)),
            (HomeStoreService.TallPlantId, new Vector3(3.3f,0,1.9f)),
            (HomeStoreService.FloorLampId, new Vector3(2.7f,0,1.3f)) })
        {
            var activity = Find(row.Item1);
            for (int repeat = 0; repeat < 3; repeat++)
            {
                yield return Place(activity, row.Item2);
                int completed = 0;
                System.Action<CatActivity> finished = a => { if (a == activity) completed++; };
                CatActivity.Completed += finished;
                var timer = System.Diagnostics.Stopwatch.StartNew(); button.onClick.Invoke(); timer.Stop();
                double clickMs = timer.Elapsed.TotalMilliseconds;
                Assert.That(activity.IsRunning, Is.True, row.Item1 + " must accept the visible button");
                Assert.That(clickMs, Is.LessThan(100), row.Item1 + " must not block a click for seconds");
                float deadline = Time.realtimeSinceStartup + 15, deflection = 0, minDot = 1;
                int workingFrames = 0; bool captured = false;
                var hips = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine");
                var shoulders = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.003");
                while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForEndOfFrame();
                    if (activity.GestureBeats == 0 || !activity.IsRunning) continue;
                    workingFrames++;
                    var gaze = cat.GetComponent<CatFurnitureGaze>(); deflection = Mathf.Max(deflection, gaze.Deflection);
                    float dot = CatActivityFacing.FacingDot(shoulders.position - hips.position, (shoulders.position + hips.position) * .5f, CatActivityFacing.CameraPosition(cat));
                    minDot = Mathf.Min(minDot, dot);
                    Assert.That(CatActivityApproach.HasClearSight(activity, cat, activity.ViewStand + Vector3.up * .4f, activity.ActiveLookTarget), Is.True);
                    if (repeat == 0 && activity.GestureBeats == 2 && !captured)
                    {
                        Directory.CreateDirectory(Output + "/screens");
                        ScreenCapture.CaptureScreenshot(Output + "/screens/" + row.Item1 + ".png"); captured = true;
                    }
                }
                CatActivity.Completed -= finished;
                Assert.That(activity.IsRunning, Is.False, "Observation must finish");
                Assert.That(completed, Is.EqualTo(1));
                Assert.That(activity.GestureBeats, Is.EqualTo(3));
                Assert.That(workingFrames, Is.GreaterThan(10));
                Assert.That(deflection, Is.GreaterThan(.5f), "Visible head animation must actually play");
                Assert.That(minDot, Is.GreaterThanOrEqualTo(-.01f), "Final animated body must face the player");
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
                Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
                evidence.Add(row.Item1 + ",repeat=" + repeat + ",clickMs=" + clickMs.ToString("F3") + ",beats=3,complete=1,deflection=" + deflection.ToString("F3") + ",minBodyDot=" + minDot.ToString("F3"));
            }
        }
    }
    [UnityTest] public IEnumerator BlockedObservationRejectsThenRecovers_AndPauseCancelReleaseControl()
    {
        yield return Prepare(); var activity = Find(HomeStoreService.BookshelfId);
        yield return Place(activity, new Vector3(-.5f,0,1.7f));
        blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position = new Vector3(-.5f,.6f,1.7f); blocker.transform.localScale = new Vector3(4,2,4);
        Physics.SyncTransforms(); float energy = Object.FindAnyObjectByType<EnergySystem>().CurrentEnergy;
        var timer = System.Diagnostics.Stopwatch.StartNew(); bool accepted = activity.TryStart(cat); timer.Stop();
        Assert.That(accepted, Is.False); Assert.That(timer.Elapsed.TotalMilliseconds, Is.LessThan(100));
        Assert.That(Object.FindAnyObjectByType<EnergySystem>().CurrentEnergy, Is.EqualTo(energy));
        Object.DestroyImmediate(blocker); Physics.SyncTransforms();
        yield return Place(activity, new Vector3(-.5f,0,1.7f)); button.onClick.Invoke();
        Assert.That(activity.IsRunning, Is.True, "A rejected attempt must not poison the next route");
        float deadline = Time.realtimeSinceStartup + 10;
        while (activity.GestureBeats == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(activity.GestureBeats, Is.GreaterThan(0));
        Time.timeScale = 0; var position = cat.transform.position;
        for (int i = 0; i < 4; i++) yield return null;
        Assert.That(Vector3.Distance(cat.transform.position, position), Is.LessThan(.0001f));
        activity.CancelForTransition(); Time.timeScale = 1; yield return null;
        Assert.That(activity.IsRunning, Is.False); Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(cat.GetComponent<CatFurnitureGaze>().Deflection, Is.Zero);
        evidence.Add("Blocked click rejected without spending energy; next click starts; pause/cancel restores controller and gaze.");
    }
}
#endif

