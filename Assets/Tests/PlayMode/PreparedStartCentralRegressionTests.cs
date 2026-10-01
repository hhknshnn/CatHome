using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// QA draft: import as a separate PlayMode test only after the current native run.
// Reuse the existing real-room fixture; never duplicate or weaken its readiness.
public sealed class PreparedStartCentralRegressionTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    PreparedInteractionStartTests fixture;
    CatMovement cat;
    BoxCollider obstacle;
    bool autoSync, fixtureReady;
    // This regression must deliberately disable the legacy automatic sync switch.
    static readonly PropertyInfo AutoSyncSetting = typeof(Physics).GetProperty("autoSyncTransforms", BindingFlags.Public | BindingFlags.Static);
    readonly List<string> rows = new List<string>();
    Vector3 endPosition;
    float measuredDepth;
    bool foundCapsuleOnly;

    [SetUp] public void Before()
    {
        autoSync = (bool)AutoSyncSetting.GetValue(null);
        fixture = new PreparedInteractionStartTests(); fixture.Before(); fixtureReady = true;
        rows.Clear(); rows.Add("case,stage,x,y,z,yaw,bodyClear,interactionClear,ccDepth,detail");
    }

    [TearDown] public void After()
    {
        // Remove only this fixture's temporary obstacle, before activity cleanup
        // restores the accepted floor. No scene or prefab is ever saved.
        if (obstacle != null) Object.DestroyImmediate(obstacle.gameObject);
        AutoSyncSetting.SetValue(null, autoSync); Physics.SyncTransforms();
        try { if (fixtureReady) fixture.After(); }
        finally { Flush(); }
    }

    object Invoke(string method, params object[] args) => typeof(PreparedInteractionStartTests)
        .GetMethod(method, Private).Invoke(fixture, args);
    T Field<T>(string name) => (T)typeof(PreparedInteractionStartTests).GetField(name, Private).GetValue(fixture);
    IEnumerator Prepare()
    {
        yield return (IEnumerator)Invoke("Prepare", "LivingRoom_Level01");
        cat = Field<CatMovement>("cat"); Assert.That(cat, Is.Not.Null);
        Assert.That(cat.HasBodyGuardProfile, Is.True);
    }
    void Place(Vector3 position, Quaternion rotation) => Invoke("Place", position, rotation);
    bool FindReady(CatActivity activity, out CatActivityStart accepted, out string reason)
    {
        object[] args = { activity, default(CatActivityStart), string.Empty };
        bool found = (bool)Invoke("FindReadyPose", args);
        accepted = (CatActivityStart)args[1]; reason = (string)args[2]; return found;
    }
    CatActivity ReadyJump(out CatActivityStart accepted)
    {
        accepted = default;
        var reasons = new List<string>();
        foreach (var activity in CatActivity.Registered.Where(a => a.gameObject.scene == cat.gameObject.scene &&
            a is LivingFurnitureActivity).OrderByDescending(a => a.EnergyCost))
        {
            if (FindReady(activity, out accepted, out string reason) && accepted.Kind == CatActivityStartKind.GroundLaunch)
                return activity;
            reasons.Add(activity.ActivityId + ": " + reason);
        }
        Row("fixture", "No ready native furniture launch: " + string.Join("; ", reasons));
        Assert.Fail("No ready native furniture launch: " + string.Join("; ", reasons)); return null;
    }
    void MakeObstacle(Vector3 size)
    {
        var go = new GameObject("QA central-start temporary solid");
        SceneManager.MoveGameObjectToScene(go, cat.gameObject.scene);
        go.transform.position = new Vector3(0, -5000, 0);
        obstacle = go.AddComponent<BoxCollider>(); obstacle.size = size;
        Physics.SyncTransforms();
    }
    void Row(string stage, string detail = "", float depth = 0)
    {
        Vector3 p = cat != null ? cat.transform.position : Vector3.zero;
        bool body = cat != null && cat.IsBodyPoseClear(p, cat.transform.rotation);
        bool interaction = cat != null && cat.IsInteractionPoseClear(p, cat.transform.rotation);
        rows.Add(string.Join(",", TestContext.CurrentContext.Test.Name, stage,
            p.x.ToString("F6", CultureInfo.InvariantCulture), p.y.ToString("F6", CultureInfo.InvariantCulture),
            p.z.ToString("F6", CultureInfo.InvariantCulture),
            (cat != null ? cat.transform.eulerAngles.y : 0).ToString("F4", CultureInfo.InvariantCulture),
            body, interaction, depth.ToString("F6", CultureInfo.InvariantCulture),
            "\"" + detail.Replace("\"", "\"\"") + "\""));
        Flush();
    }
    void Flush()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllLines(Root + "/prepared-central-" + TestContext.CurrentContext.Test.Name + ".csv", rows);
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator AcceptedLaunchAtPoint13_WithAngledHeading_EntersNativePreparationWithoutStaging()
    {
        yield return Prepare();
        CatActivity activity = ReadyJump(out var floor);
        Vector3 raised = floor.Position; raised.y = .13f;
        CatActivityStart accepted = default; bool ready = false;
        foreach (float angle in new[] { 15f, -15f, 10f, -10f })
        {
            Place(raised, Quaternion.AngleAxis(angle, Vector3.up) * floor.Rotation);
            if (activity.TryGetStartPose(cat, out accepted)) { ready = true; break; }
        }
        Row("raised-ready", "accepted=" + ready);
        Assert.That(ready, Is.True, "A genuinely clear .13 m launch with an angled in-cone heading is required");
        Assert.That(accepted.Position.y, Is.EqualTo(.13f).Within(.00001f));
        Vector3 direction = accepted.ActionTarget - accepted.Position; direction.y = 0;
        Assert.That(Vector3.Angle(accepted.Rotation * Vector3.forward, direction), Is.GreaterThan(5f),
            "An aligned heading would not expose the old pre-jump pivot");
        Assert.That(activity.TryGetPromptDistance(cat, out _), Is.True);
        RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat), Is.True);
        var driver = cat.GetComponent<CatActivityAnimation>(); int native = 0;
        float elapsed = 0, shift = 0, turn = 0;
        while (elapsed < .30f)
        {
            yield return new WaitForEndOfFrame(); elapsed += Time.deltaTime;
            shift = Mathf.Max(shift, Vector3.Distance(accepted.Position, cat.transform.position));
            turn = Mathf.Max(turn, Quaternion.Angle(accepted.Rotation, cat.transform.rotation));
            Assert.That(activity.IsRunning, Is.True);
            Assert.That(driver.IsNativeJump, Is.True, "The first frame is native preparation, not a pre-launch turn");
            Assert.That(driver.NativeJumpPhase, Is.LessThanOrEqualTo(CatJumpMotion.Takeoff + .001f));
            Assert.That(cat.GetComponentInChildren<Animator>().GetCurrentAnimatorClipInfo(0)
                .Any(c => c.clip.name.EndsWith("|Jump", StringComparison.Ordinal)), Is.True);
            native++;
        }
        Row("raised-native-preparation", "nativeFrames=" + native + ";shift=" + shift + ";turn=" + turn);
        Assert.That(native, Is.GreaterThanOrEqualTo(5));
        Assert.That(shift, Is.LessThan(.001f)); Assert.That(turn, Is.LessThan(.2f));
        activity.CancelForTransition();
        Assert.That(Vector3.Distance(accepted.Position, cat.transform.position), Is.LessThan(.001f));
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Assert.That(cat.GetComponent<CharacterController>().enabled && !cat.IsMovementPhysicallyLocked, Is.True);
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator SameFrameMovedObstacle_WithAutoSyncOff_RejectsClickBeforeEnergySpend()
    {
        yield return Prepare();
        CatActivity activity = ReadyJump(out var accepted);
        Assert.That(activity.EnergyCost, Is.GreaterThan(0), "Exercise a start that would spend energy if accepted");
        var energy = RoomPlayModeSupport.ProvisionNeeds(); float before = energy.CurrentEnergy;
        AutoSyncSetting.SetValue(null, false);
        MakeObstacle(new Vector3(.28f, .45f, .28f));
        Assert.That(activity.TryGetStartPose(cat, out accepted), Is.True);
        Assert.That(activity.TryGetPromptDistance(cat, out _), Is.True);
        Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
        var cc = cat.GetComponent<CharacterController>();
        Vector3 target = origin + heading * Vector3.Scale(cc.center, cat.transform.lossyScale);
        obstacle.transform.position = target;
        // No yield, explicit SyncTransforms, or transform-setting helper is
        // allowed between this move and TryStart. First prove PhysX is stale.
        bool staleWorldContainsObstacle = Physics.OverlapSphere(target, .01f, ~0, QueryTriggerInteraction.Ignore).Contains(obstacle);
        Assert.That(staleWorldContainsObstacle, Is.False, "The test must exercise an unsynchronized same-frame collider move");
        bool started = activity.TryStart(cat);
        Row("same-frame-click", "started=" + started + ";energyBefore=" + before + ";energyAfter=" + energy.CurrentEnergy);
        Assert.That(started, Is.False);
        Assert.That(Physics.OverlapSphere(target, .01f, ~0, QueryTriggerInteraction.Ignore).Contains(obstacle), Is.True,
            "Click-time preflight synchronized the obstacle before deciding");
        Assert.That(energy.CurrentEnergy, Is.EqualTo(before).Within(.00001f));
        Assert.That(activity.IsRunning || CatActivity.Active != null || cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(Vector3.Distance(origin, cat.transform.position), Is.LessThan(.00001f));
        Assert.That(Quaternion.Angle(heading, cat.transform.rotation), Is.LessThan(.001f));
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(activity.TryGetPromptDistance(cat, out _), Is.False);
    }

    [UnityTest, Timeout(90000)]
    public IEnumerator NormalCompletion_WithBodyClearButCapsuleBlockedEnd_RestoresAcceptedFloor()
    {
        yield return Prepare(); ReadyJump(out var accepted);
        MakeObstacle(new Vector3(.035f, .12f, .035f));
        yield return FindCapsuleOnlyEnd(accepted);
        Assert.That(foundCapsuleOnly, Is.True,
            "No measured body-clear / CC-blocked distinction within 384 candidates and 8 seconds; see CSV");
        Place(accepted.Position, accepted.Rotation);
        Assert.That(cat.IsInteractionPoseClear(accepted.Position, accepted.Rotation), Is.True);
        var command = cat.GetComponent<CatCommandActivity>() ?? cat.gameObject.AddComponent<CatCommandActivity>();
        int completed = 0; Action<CatActivity> count = a => { if (a == command) completed++; };
        CatActivity.Completed += count;
        try
        {
            Assert.That(command.Issue(CatCompanionCommand.Meow), Is.True);
            Place(endPosition, accepted.Rotation);
            Assert.That(cat.IsBodyPoseClear(endPosition, accepted.Rotation), Is.True);
            Assert.That(cat.IsInteractionPoseClear(endPosition, accepted.Rotation), Is.False);
            Assert.That(CapsuleDepth(endPosition, accepted.Rotation, out _), Is.GreaterThan(.003f));
            Row("normal-end-body-clear-cc-blocked", "accepted=" + accepted.Position, measuredDepth);
            float until = Time.realtimeSinceStartup + 7f;
            while (command.IsRunning && Time.realtimeSinceStartup < until)
            {
                yield return new WaitForEndOfFrame();
                if (command.IsRunning)
                    Assert.That(Vector3.Distance(endPosition, cat.transform.position), Is.LessThan(.001f),
                        "The measured obstruction must survive until normal completion; no fixture or controller may silently rescue it");
            }
            Assert.That(command.IsRunning, Is.False, "Normal command completion deadline");
            Assert.That(completed, Is.EqualTo(1), "Exercise CompleteActivity, not cancellation or a reflected completion");
            Assert.That(Vector3.Distance(accepted.Position, cat.transform.position), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(accepted.Rotation, cat.transform.rotation), Is.LessThan(.2f));
            Assert.That(cat.IsInteractionPoseClear(cat.transform.position, cat.transform.rotation), Is.True);
            Assert.That(CatActivity.Active, Is.Null);
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Assert.That(cat.GetComponent<CharacterController>().enabled && !cat.IsMovementPhysicallyLocked, Is.True);
            Row("normal-end-restored", "completions=" + completed, measuredDepth);
        }
        finally { CatActivity.Completed -= count; }
    }

    IEnumerator FindCapsuleOnlyEnd(CatActivityStart accepted)
    {
        foundCapsuleOnly = false; int attempts = 0;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var cc = cat.GetComponent<CharacterController>();
        Vector3 scale = cat.transform.lossyScale;
        float radius = cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        foreach (float distance in new[] { .7f, .9f, 1.1f })
        foreach (float direction in new[] { 0f, 90f, -90f, 180f, 45f, -45f, 135f, -135f })
        {
            Vector3 end = accepted.Position + Quaternion.Euler(0, direction, 0) * Vector3.forward * distance;
            obstacle.transform.position = new Vector3(0, -5000, 0); Physics.SyncTransforms();
            if (!cat.IsInteractionPoseClear(end, accepted.Rotation)) continue;
            Vector3 centre = end + accepted.Rotation * Vector3.Scale(cc.center, scale);
            foreach (float side in new[] { 90f, -90f, 135f, -135f, 45f, -45f, 180f, 0f })
            foreach (float inward in new[] { .008f, .016f })
            {
                if (++attempts > 384 || timer.Elapsed.TotalSeconds > 8)
                { Row("cc-only-search-budget", "attempts=" + attempts + ";seconds=" + timer.Elapsed.TotalSeconds); yield break; }
                Vector3 directionToObstacle = accepted.Rotation * Quaternion.Euler(0, side, 0) * Vector3.forward;
                obstacle.transform.position = centre + directionToObstacle * (radius + .0175f - inward);
                Physics.SyncTransforms();
                bool body = cat.IsBodyPoseClear(end, accepted.Rotation);
                bool interaction = cat.IsInteractionPoseClear(end, accepted.Rotation);
                float depth = CapsuleDepth(end, accepted.Rotation, out Vector3 normal);
                if (body && !interaction && depth > .003f && normal.y < .8f &&
                    cat.IsInteractionPoseClear(accepted.Position, accepted.Rotation))
                {
                    endPosition = end; measuredDepth = depth; foundCapsuleOnly = true;
                    Row("cc-only-search-found", "attempts=" + attempts + ";end=" + end + ";body=" + body +
                        ";interaction=" + interaction + ";obstacle=" + obstacle.transform.position, depth);
                    yield break;
                }
                if (attempts % 16 == 0)
                {
                    Row("cc-only-search", "attempts=" + attempts + ";end=" + end + ";body=" + body + ";interaction=" + interaction, depth);
                    obstacle.transform.position = new Vector3(0, -5000, 0); Physics.SyncTransforms();
                    yield return null;
                }
            }
        }
        Row("cc-only-search-exhausted", "attempts=" + attempts + ";seconds=" + timer.Elapsed.TotalSeconds);
    }

    float CapsuleDepth(Vector3 position, Quaternion rotation, out Vector3 normal)
    {
        var cc = cat.GetComponent<CharacterController>(); var probe = Field<CapsuleCollider>("controllerProbe");
        Vector3 scale = cat.transform.lossyScale;
        probe.radius = cc.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        probe.height = Mathf.Max(probe.radius * 2, cc.height * Mathf.Abs(scale.y));
        Vector3 centre = position + rotation * Vector3.Scale(cc.center, scale);
        return Physics.ComputePenetration(probe, centre, Quaternion.identity, obstacle,
            obstacle.transform.position, obstacle.transform.rotation, out normal, out float depth) ? depth : 0;
    }
}
