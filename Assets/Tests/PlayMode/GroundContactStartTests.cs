using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

/// <summary>Exercises the public readiness contract from real, unmodified room geometry.</summary>
public sealed class GroundContactStartTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly string[] Rooms = { "LivingRoom_Level01", "Bathroom_Level01", "Kitchen_Level01",
        "Bedroom_Level01", "Garden_Level01", "Balcony_Level01", "Patio_Level01", "SecondFloor_Level01" };
    static readonly float[] Radii = { 0f, .12f, .24f, .32f, .36f, .40f, .44f, .48f, .52f, .56f, .64f, .66f, .72f, .84f, 1f, 1.2f };
    static readonly float[] Yaws = { 0f, -5f, 5f, -15f, 15f, -25f, 25f, -35f, 35f, -65f, 65f, -80f, 80f, -90f, 90f };
    readonly List<string> rows = new List<string>();
    readonly List<string> failures = new List<string>();
    HomeStoreSaveState savedStore;
    QuestProgressEntry[] savedQuests;
    string savedBreed, room;
    long savedCoins, savedDiamonds, savedBond;
    int savedChapter;
    float savedScale, savedCapture;
    CatMovement cat;
    CatActivity observed;
    int completions, attempts;

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use the isolated QA save session.");
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedCoins = ProgressionService.Coins; savedDiamonds = ProgressionService.Diamonds;
        savedBond = ProgressionService.BondXp; savedChapter = ProgressionService.CurrentChapterNumber;
        savedQuests = ProgressionService.CaptureQuestProgress();
        savedScale = Time.timeScale; savedCapture = Time.captureDeltaTime;
        Time.timeScale = 1; Time.captureFramerate = 30;
        rows.Clear(); failures.Clear();
        rows.Add("room,product,breed,phase,candidates,x,z,yaw,startMs,rootXZ,rootYaw,contacts,completions,result");
        Directory.CreateDirectory(Root);
        CatActivity.Completed += Completed;
    }

    [TearDown] public void After()
    {
        CatActivity.Completed -= Completed;
        Time.timeScale = 1;
        if (cat != null) CatActionState.CancelForTransition(cat);
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(savedStore); CatBreedService.Select(savedBreed);
        ProgressionService.ApplySavedState(savedCoins, savedDiamonds, savedBond, savedChapter, savedQuests);
        Time.timeScale = savedScale; Time.captureDeltaTime = savedCapture;
        Flush();
    }

    void Completed(CatActivity activity) { if (activity == observed) completions++; }
    void Flush()
    {
        string stem = Root + "/ground-start-" + TestContext.CurrentContext.Test.Name;
        File.WriteAllLines(stem + ".csv", rows);
        File.WriteAllLines(stem + "-failures.csv", new[] { "failure" }.Concat(failures.Select(Escape)));
    }
    static string Escape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    string Label(CatActivity a) => room + "/" + a.StoreProductId + "/" + CatBreedService.SelectedBreedId;
    void Check(bool condition, CatActivity a, string message)
    { if (!condition) failures.Add(Label(a) + ": " + message); }
    void Row(CatActivity a, string phase, string result, double milliseconds = 0, float drift = 0, float turn = 0)
    {
        Vector3 p = cat.transform.position;
        rows.Add(FormattableString.Invariant($"{room},{a.StoreProductId},{CatBreedService.SelectedBreedId},{phase},{attempts},{p.x:F4},{p.z:F4},{cat.transform.eulerAngles.y:F2},{milliseconds:F3},{drift:F5},{turn:F3},{Contacts(a)},{completions},{result}"));
        Flush();
    }

    IEnumerator Prepare(string scene)
    {
        room = scene;
        yield return RoomPlayModeSupport.LoadRoomAlone(scene);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Select(p => p.Id).ToArray();
        state.storedProductIds = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state);
        ProgressionService.ApplySavedState(savedCoins, savedDiamonds, Math.Max(1000, savedBond), savedChapter, savedQuests);
        cat = Object.FindAnyObjectByType<CatMovement>(); Assert.That(cat, Is.Not.Null, scene);
        yield return QaBreedReadiness.WaitForSelected(cat);
        CatActionState.CancelForTransition(cat);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        foreach (var activity in CatActivity.Registered) activity.RefreshUnlockPresentation();
    }

    static bool GroundContact(CatActivity a) => a is PaperSpinActivity || a is BirdFeederShakeActivity ||
        a is CartNudgeActivity || a is ScratchPostActivity || a is KnockOffActivity knock && knock.PerchPoint == null;
    CatActivity[] InRoom(Func<CatActivity, bool> match) => CatActivity.Registered.Where(a =>
        a != null && a.gameObject.scene == cat.gameObject.scene && !a.IsRetired && match(a)).ToArray();
    static int Contacts(CatActivity a) => a is PaperSpinActivity paper ?
        (paper.UsesPaperTears ? paper.PaperContactCount : paper.RecordContactCount) :
        a is BirdFeederShakeActivity bird ? bird.ContactStrokes : a is CartNudgeActivity cart ? cart.ContactStrokes :
        a is ScratchPostActivity scratch ? Mathf.Min(scratch.LeftStrokes, scratch.RightStrokes) :
        a is KnockOffActivity knock ? knock.ContactStrokes : 0;
    static Vector3 Flat(Vector3 p) { p.y = 0; return p; }
    static Transform MovingProp(CatActivity a) => a is PaperSpinActivity paper ? paper.RollPivot :
        a is BirdFeederShakeActivity bird ? bird.FeederPivot : a is CartNudgeActivity cart ? cart.CartVisual :
        a is KnockOffActivity knock ? knock.GlassPivot : null;
    static T Field<T>(CatActivity a, string name) => (T)a.GetType().GetField(name, Private).GetValue(a);
    static Vector3 TargetHint(CatActivity a)
    {
        if (a is PaperSpinActivity paper) return paper.UsesPaperTears ? paper.PaperContactPosition : paper.RollPivot.position;
        if (a is BirdFeederShakeActivity) return a.transform.position;
        if (a is CartNudgeActivity cart) return cart.CartVisual.GetComponentInChildren<Renderer>().bounds.center;
        if (a is ScratchPostActivity) return a.transform.TransformPoint(Field<Vector3>(a, "ropeCenter"));
        if (a is KnockOffActivity knock) return knock.GlassPivot.position;
        if (a is MatKneadActivity) return Field<Transform>(a, "padPoint").position;
        if (a is OvenWarmthActivity) return Field<Transform>(a, "baskPoint").position;
        if (a is SitLookActivity view) return view.LookPoint.position;
        return a.transform.position;
    }
    void Place(Vector3 position, Quaternion rotation)
    {
        var cc = cat.GetComponent<CharacterController>(); bool wasEnabled = cc != null && cc.enabled;
        if (cc != null) cc.enabled = false;
        position.y = .05f; cat.transform.SetPositionAndRotation(position, rotation);
        if (cc != null) cc.enabled = wasEnabled;
        Physics.SyncTransforms();
    }

    // This search is exclusively the test player's stance selection. It calls
    // the unchanged public readiness API; it never changes a contact, collider,
    // authored entry, body profile or runtime radius to make a fixture pass.
    bool foundStance;
    CatActivityStart foundStart;
    IEnumerator FindStance(CatActivity a)
    {
        foundStance = false; foundStart = default; attempts = 0;
        CatActivityStart accepted = default;
        double began = Time.realtimeSinceStartupAsDouble;
        Vector3 target = TargetHint(a);
        var centres = new List<Vector3>();
        if (a.RoutineEntryPoint != null) centres.Add(a.RoutineEntryPoint.position);
        if (a is PaperSpinActivity) centres.Add(Field<Transform>(a, "swatPoint").position);
        if (a is CartNudgeActivity) centres.Add(Field<Transform>(a, "shovePoint").position);
        centres.Add(target);
        // Prefer the API's own zone/target whenever it can expose them here.
        a.TryGetStartPose(cat, out var hint);
        if (hint.PromptDistance > 0 && !float.IsNaN(hint.ZoneCentre.x) && hint.ZoneCentre.sqrMagnitude > .001f)
            centres.Insert(0, hint.ZoneCentre);
        foreach (Vector3 centre in centres)
        foreach (float radius in Radii)
        for (int angle = 0; angle < (radius == 0 ? 1 : 72); angle++)
        {
            Vector3 point = centre + Quaternion.Euler(0, angle * 5, 0) * Vector3.forward * radius;
            Vector3 toward = Flat(target - point);
            Quaternion aim = toward.sqrMagnitude > .001f ? Quaternion.LookRotation(toward) : Quaternion.Euler(0, 37, 0);
            foreach (float yaw in Yaws)
            {
                if ((attempts & 15) == 0)
                {
                    if (Time.realtimeSinceStartupAsDouble - began > 120)
                    { failures.Add(Label(a) + ": bounded stance search exceeded 120 seconds"); yield break; }
                    yield return null;
                }
                Place(point, aim * Quaternion.Euler(0, yaw, 0)); attempts++;
                Vector3 before = cat.transform.position; Quaternion rotation = cat.transform.rotation;
                bool ready = a.TryGetStartPose(cat, out var start);
                if (!a.HasPreparedStart && a is SitLookActivity) ready = a.TryGetPromptDistance(cat, out _);
                if (!ready) continue;
                Check(Flat(cat.transform.position - before).sqrMagnitude < .00000001f &&
                    Quaternion.Angle(rotation, cat.transform.rotation) < .01f, a, "readiness mutated the actor");
                accepted = a.HasPreparedStart ? start : new CatActivityStart { Position = before,
                    Rotation = rotation, ZoneCentre = centre, ActionTarget = target, Kind = CatActivityStartKind.Contact };
                Check(Vector3.Distance(accepted.Position, before) < .0001f &&
                    Quaternion.Angle(accepted.Rotation, rotation) < .01f, a, "resolver proposed a different body pose");
                foundStance = true; foundStart = accepted; yield break;
            }
        }
        failures.Add(Label(a) + ": no ready current stance in authored/target neighbourhood; strict body/geometry gate retained");
        Row(a, "find", "no-ready-stance"); yield break;
    }

    void Refusals(CatActivity a, CatActivityStart accepted)
    {
        RoomPlayModeSupport.ProvisionNeeds();
        Place(accepted.Position + Vector3.forward * 4f, accepted.Rotation);
        Check(!a.TryGetPromptDistance(cat, out _), a, "out-of-range prompt");
        bool started = a.TryStart(cat); Check(!started, a, "out-of-range click accepted");
        if (started) a.CancelForTransition();
        if (accepted.Kind == CatActivityStartKind.Contact)
        {
            Place(accepted.Position, accepted.Rotation * Quaternion.Euler(0, 180, 0));
            Check(!a.TryGetPromptDistance(cat, out _), a, "backwards prompt");
            started = a.TryStart(cat); Check(!started, a, "backwards click accepted");
            if (started) a.CancelForTransition();
        }
        Place(accepted.Position, accepted.Rotation);
    }

    IEnumerator EquipTarget(CatActivity a)
    {
        // Optional cat products were stored by Prepare. Display exactly the
        // current optional target; authored room furniture stays untouched.
        foreach(var product in HomeStoreService.Products)
            if(CatCollectionPolicy.IsCatItem(product.Id)&&HomeStoreService.IsOwned(product.Id))
                HomeStoreService.TrySetStored(product.Id,product.Id!=a.StoreProductId);
        a.RefreshUnlockPresentation();
        yield return null;yield return null;Physics.SyncTransforms();
        Check(a.IsUnlocked&&a.IsContentVisible,a,"fixture target remains stored/hidden");
    }

    IEnumerator Run(CatActivity a, bool cancel, bool requireContacts)
    {
        yield return EquipTarget(a);
        if (!a.IsUnlocked || !a.IsContentVisible) { Row(a, "fixture", "target-hidden"); yield break; }
        yield return FindStance(a);
        if (!foundStance) yield break;
        var accepted = foundStart;
        Refusals(a, accepted); yield return null;
        RoomPlayModeSupport.ProvisionNeeds();
        bool ready = a.TryGetPromptDistance(cat, out _);
        Check(ready, a, "accepted stance lost prompt after one settled frame");
        if (!ready) { Row(a, "ready", "settled-rejected"); yield break; }
        Vector3 origin = cat.transform.position; Quaternion rotation = cat.transform.rotation;
        Transform prop = MovingProp(a);
        Vector3 propHome = prop != null ? prop.localPosition : Vector3.zero;
        Quaternion propRotation = prop != null ? prop.localRotation : Quaternion.identity;
        observed = a; completions = 0;
        var watch = System.Diagnostics.Stopwatch.StartNew(); bool started = a.TryStart(cat); watch.Stop();
        Check(started, a, "visible prompt disagrees with click");
        Check(watch.Elapsed.TotalMilliseconds < 50, a, "click exceeded 50ms; investigate synchronous search");
        if (!started) { Row(a, "click", "refused", watch.Elapsed.TotalMilliseconds); yield break; }
        float drift = Flat(cat.transform.position - origin).magnitude;
        float turn = Quaternion.Angle(rotation, cat.transform.rotation);
        float elapsed = 0, deadline = Time.realtimeSinceStartup + 30;
        bool paused = false;
        while (a.IsRunning && Time.realtimeSinceStartup < deadline)
        {
            yield return new WaitForEndOfFrame(); elapsed += Time.deltaTime;
            if (elapsed <= .55f)
            {
                drift = Mathf.Max(drift, Flat(cat.transform.position - origin).magnitude);
                turn = Mathf.Max(turn, Quaternion.Angle(rotation, cat.transform.rotation));
            }
            if (!paused && elapsed >= .3f)
            {
                paused = true; Time.timeScale = 0;
                yield return new WaitForEndOfFrame();
                Vector3 still = cat.transform.position; Quaternion heading = cat.transform.rotation;
                int count = Contacts(a);
                yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
                Check(Vector3.Distance(still, cat.transform.position) < .0001f &&
                    Quaternion.Angle(heading, cat.transform.rotation) < .01f, a, "pause advanced body pose");
                Check(Contacts(a) == count, a, "pause added a physical contact");
                if (cancel) a.CancelForTransition();
                Time.timeScale = 1;
            }
            if (a.IsWaitingForRestStop && elapsed > 1.1f) a.RequestRestStop();
        }
        Check(!a.IsRunning, a, "routine timed out");
        if (a.IsRunning) a.CancelForTransition();
        Check(drift < .002f, a, "first .5 seconds moved root XZ " + drift);
        Check(turn < .2f, a, "first .5 seconds rotated root " + turn);
        Check(completions == (cancel ? 0 : 1), a, "completion count " + completions);
        if (requireContacts && !cancel) Check(Contacts(a) > 0, a, "completed without actual contact on required paw(s)");
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        Check(!cat.IsMovementPhysicallyLocked && cat.GetComponent<CharacterController>().enabled,
            a, "control/controller not restored");
        Check(CatActivity.Active == null, a, "activity owner leaked");
        if (cancel && prop != null)
            Check(Vector3.Distance(prop.localPosition, propHome) < .0001f &&
                Quaternion.Angle(prop.localRotation, propRotation) < .01f, a, "cancel did not restore moving prop");
        Row(a, cancel ? "cancel" : "complete", "measured", watch.Elapsed.TotalMilliseconds, drift, turn);
    }

    [UnityTest, Timeout(360000)] public IEnumerator EightRooms_GroundContacts_ReadyClickContactAndNoStaging()
    {
        int count = 0;
        foreach (string scene in Rooms)
        {
            yield return Prepare(scene);
            var activities = InRoom(GroundContact);
            if (activities.Length == 0) failures.Add(scene + ": no ground-contact coverage");
            foreach (var a in activities) { count++; yield return Run(a, false, true); }
        }
        Assert.That(count, Is.GreaterThanOrEqualTo(8));
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest, Timeout(240000)] public IEnumerator GroundContacts_PauseCancelAndRetryReleaseControl()
    {
        foreach (string scene in new[] { "Bathroom_Level01", "Kitchen_Level01", "Balcony_Level01", "SecondFloor_Level01" })
        {
            yield return Prepare(scene);
            foreach (var a in InRoom(GroundContact))
            {
                yield return Run(a, true, false);
                yield return Run(a, false, true);
            }
        }
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest, Timeout(360000)] public IEnumerator RecordAndFeeder_TenBreeds_ActualContactsFromAcceptedPose()
    {
        int count = 0;
        foreach (string scene in new[] { "Balcony_Level01", "SecondFloor_Level01" })
        {
            yield return Prepare(scene);
            var activity = InRoom(a => a is BirdFeederShakeActivity || a.Kind == CatActivityKind.RecordSpin).Single();
            foreach (var entry in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(entry.Id); yield return null; yield return null;
                count++; yield return Run(activity, false, true);
            }
        }
        Assert.That(count, Is.EqualTo(20));
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest, Timeout(360000)] public IEnumerator StationaryRestViewsAndCommands_RetainThePlayerStance()
    {
        foreach (string scene in Rooms)
        {
            yield return Prepare(scene);
            foreach (var a in InRoom(a => a is MatKneadActivity || a is OvenWarmthActivity ||
                a is SitLookActivity view && view.ReactionKind == SitLookReaction.Sit))
                yield return Run(a, false, false);
        }
        yield return Prepare("LivingRoom_Level01");
        var command = cat.GetComponent<CatCommandActivity>() ?? cat.gameObject.AddComponent<CatCommandActivity>();
        foreach (var value in new[] { CatCompanionCommand.Sit, CatCompanionCommand.Loaf })
        {
            bool clear = false;
            for (int x = -3; x <= 3 && !clear; x++) for (int z = -3; z <= 3 && !clear; z++)
            { Place(new Vector3(x * .5f, .05f, z * .5f), Quaternion.Euler(0, 37, 0)); clear = cat.IsBodyPoseClear(cat.transform.position, cat.transform.rotation); }
            Check(clear, command, "no open command stance"); if (!clear) continue;
            yield return null; RoomPlayModeSupport.ProvisionNeeds();
            Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
            Check(command.TryGetStartPose(cat, out var start) && start.Position == origin, command, "command resolver is not current stance");
            observed = command; completions = 0;
            bool issued = command.Issue(value); Check(issued, command, "command refused clear pose");
            if (!issued) continue;
            float deadline = Time.realtimeSinceStartup + 6, drift = 0, turn = 0;
            while (command.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                drift = Mathf.Max(drift, Flat(cat.transform.position - origin).magnitude);
                turn = Mathf.Max(turn, Quaternion.Angle(heading, cat.transform.rotation));
                if (command.IsWaitingForRestStop) command.RequestRestStop();
            }
            Check(!command.IsRunning && completions == 1, command, "command failed to complete once");
            if (command.IsRunning) command.CancelForTransition();
            Check(drift < .002f && turn < .2f, command, "command changed root XZ/yaw");
            yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
            Check(!cat.IsMovementPhysicallyLocked, command, "command retained control");
            Row(command, value.ToString(), "measured", 0, drift, turn);
        }
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }
// Insert inside GroundContactStartTests after the corrected EquipTarget method.
// Uses the actual room meshes, source-pose resolver and unchanged 25mm limit.
// QuickOneScratch is the first native gate. The ten-breed inventory is separate.
bool scratchReady;
CatActivityStart scratchStart;
readonly List<string> scratchRows=new List<string>();

[UnityTest,Timeout(90000)] public IEnumerator QuickOneScratch()
{
    scratchRows.Clear();
    try
    {
        yield return Prepare("LivingRoom_Level01");
        CatBreedService.Select("russian-blue");yield return null;yield return null;
        var a=InRoom(x=>x is ScratchPostActivity).Cast<ScratchPostActivity>().Single();
        yield return EquipTarget(a);
        yield return FindMeasuredScratch(a);
        Check(scratchReady,a,"bounded visible-post surface/4-endpoint gate failed; see scratch CSV");
        if(scratchReady)yield return RunMeasuredScratch(a);
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    finally{File.WriteAllLines(Root+"/scratch-quick.csv",scratchRows);}
}

[UnityTest,Timeout(360000)] public IEnumerator ScratchSurfaces_FourProducts_TenBreeds()
{
    scratchRows.Clear();int count=0;
    try
    {
        foreach(string scene in new[]{"LivingRoom_Level01","Bedroom_Level01","Garden_Level01","Patio_Level01"})
        {
            yield return Prepare(scene);
            var a=InRoom(x=>x is ScratchPostActivity).Cast<ScratchPostActivity>().Single();
            yield return EquipTarget(a);
            foreach(var breed in CatBreedCatalog.Load().Entries)
            {
                CatBreedService.Select(breed.Id);yield return null;yield return null;count++;
                yield return FindMeasuredScratch(a);
                Check(scratchReady,a,"no body-clear reachable stance with all four source endpoints");
                if(scratchReady)yield return RunMeasuredScratch(a);
            }
        }
        Assert.That(count,Is.EqualTo(40));
        Assert.That(failures,Is.Empty,string.Join("\n",failures));
    }
    finally{File.WriteAllLines(Root+"/scratch-ten-breeds.csv",scratchRows);}
}

IEnumerator FindMeasuredScratch(ScratchPostActivity a)
{
    scratchReady=false;scratchStart=default;
    if(scratchRows.Count==0)scratchRows.Add("room,product,breed,radius,lateral,yaw,visible,body,cc,centreRay,endpoints,oldMarkerYaw,sourceSweep,publicReady,incoming,phase,pitch,margin,leftTop,leftBottom,rightTop,rightBottom,result");
    Vector3 centre=a.transform.TransformPoint(Field<Vector3>(a,"ropeCenter"));
    Transform marker=Field<Transform>(a,"scratchPoint");
    Vector3 outward=Flat(marker.position-centre);if(outward.sqrMagnitude<.0001f)outward=-a.transform.forward;outward.Normalize();
    float spread=HomeStoreService.IsFixedRoomProduct(a.StoreProductId)?.025f:.045f;
    int checkedCount=0;
    // These are test-player stances, never a runtime approach or a relaxed gate.
    foreach(float radius in new[]{.34f,.38f,.42f,.30f,.46f,.50f,.54f,.58f,.62f,.66f,.70f,.26f,.22f})
    foreach(float lateral in new[]{0f,-.05f,.05f})
    foreach(float yaw in new[]{0f,-10f,10f})
    {
        Vector3 position=centre+outward*radius+Vector3.Cross(Vector3.up,outward)*lateral;
        Quaternion rotation=Quaternion.LookRotation(-outward)*Quaternion.Euler(0,yaw,0);
        Place(position,rotation);checkedCount++;
        if(checkedCount%12==0)yield return null;
        // A yielded fixture can settle on its controller: restore the exact
        // candidate before comparing independently measured and public gates.
        Place(position,rotation);
        var guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Private).GetValue(cat);
        bool body=cat.IsBodyPoseClear(cat.transform.position,rotation),cc=guard.IsControllerClear(cat.transform.position,rotation);
        bool centreRay=ScratchSurfaceRay(a,.43f,0,out var centreHit);
        var targets=new Vector3[4];int endpoints=0;
        float[] heights={.48f,.34f,.48f,.34f};float[] sides={-spread,-spread,spread,spread};
        for(int i=0;i<4;i++)if(ScratchSurfaceRay(a,heights[i],sides[i],out var hit))
        {targets[i]=hit.point+hit.normal*.005f;endpoints++;}
        var plan=default(CatPawReachPlan);
        bool source=body&&cc&&endpoints==4&&CatPawReachResolver.TryResolveSweep(cat,targets[0],targets[1],targets[2],targets[3],CatActivityPose.Scratch,out plan);
        bool ready=a.TryGetStartPose(cat,out var accepted);
        float markerYaw=Vector3.Angle(cat.transform.forward,Flat(marker.position-cat.transform.position));
        bool incoming=ready;
        if(incoming)for(int step=1;step<=6&&incoming;step++)
            incoming=cat.IsInteractionPoseClear(cat.transform.position-cat.transform.forward*(step*.04f),rotation);
        string point(Vector3 p)=>FormattableString.Invariant($"{p.x:F5};{p.y:F5};{p.z:F5}");
        scratchRows.Add(string.Join(",",room,a.StoreProductId,CatBreedService.SelectedBreedId,
            radius.ToString(System.Globalization.CultureInfo.InvariantCulture),lateral.ToString(System.Globalization.CultureInfo.InvariantCulture),yaw,
            a.IsContentVisible,body,cc,centreRay,endpoints,markerYaw.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),source,ready,incoming,
            plan.SourcePhase.ToString(System.Globalization.CultureInfo.InvariantCulture),plan.ChestPitch.ToString(System.Globalization.CultureInfo.InvariantCulture),
            plan.ReachMargin.ToString("F5",System.Globalization.CultureInfo.InvariantCulture),point(targets[0]),point(targets[1]),point(targets[2]),point(targets[3]),
            ready&&incoming?"accepted":!a.IsContentVisible?"hidden":!body?"body":!cc?"controller":!centreRay||endpoints!=4?"ray":!source?"source-reach-or-body":!ready?"public-gate": "incoming"));
        if(!ready||!incoming)continue;
        Check(source&&accepted.HasPawPlan&&accepted.PawPlan.Both,a,"public gate bypassed four measured endpoints");
        Check(Quaternion.Angle(accepted.Rotation,rotation)<.01f&&Vector3.Distance(accepted.Position,cat.transform.position)<.0001f,a,"readiness moved the actor");
        scratchReady=true;scratchStart=accepted;yield break;
    }
}

bool ScratchSurfaceRay(ScratchPostActivity a,float height,float lateral,out RaycastHit measured)
{
    measured=default;
    var hits=Physics.RaycastAll(cat.transform.position+Vector3.up*height+cat.transform.right*lateral,
        cat.transform.forward,.70f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
    foreach(var hit in hits)
    {
        if(hit.collider.GetComponentInParent<CatMovement>()==cat)continue;
        if(!hit.transform.IsChildOf(a.transform))return false;
        // The four authored products use their actual imported mesh collider.
        Check(hit.collider is MeshCollider,a,"scratch ray hit a proxy primitive, not the real product mesh");
        measured=hit;return true;
    }
    return false;
}

IEnumerator RunMeasuredScratch(ScratchPostActivity a)
{
    Place(scratchStart.Position,scratchStart.Rotation);yield return null;
    RoomPlayModeSupport.ProvisionNeeds();observed=a;completions=0;
    Vector3 start=cat.transform.position;Quaternion rotation=cat.transform.rotation;
    bool ready=a.TryGetPromptDistance(cat,out _);bool began=a.TryStart(cat);
    Check(ready&&began,a,"visible scratch prompt/click disagree");if(!began)yield break;
    int left=0,right=0;bool paused=false;float elapsed=0,deadline=Time.realtimeSinceStartup+10;
    var lh=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-hand.L");
    var rh=CatBreedVisualFactory.FindDescendant(cat.transform,"DEF-hand.R");
    while(a.IsRunning&&Time.realtimeSinceStartup<deadline)
    {
        yield return new WaitForEndOfFrame();elapsed+=Time.deltaTime;
        Check(Vector3.Distance(start,cat.transform.position)<.001f&&Quaternion.Angle(rotation,cat.transform.rotation)<.2f,a,"scratch moved root/heading");
        if(a.LeftStrokes>left)
        {
            Check(a.LastLeftSurfaceDistance<.025f&&Vector3.Distance(lh.position,a.LastLeftSurface)<.0251f,a,"left credit lacks current-frame real-surface contact");
            left=a.LeftStrokes;
        }
        if(a.RightStrokes>right)
        {
            Check(a.LastRightSurfaceDistance<.025f&&Vector3.Distance(rh.position,a.LastRightSurface)<.0251f,a,"right credit lacks current-frame real-surface contact");
            right=a.RightStrokes;
        }
        if(!paused&&elapsed>.7f)
        {
            paused=true;Time.timeScale=0;int strokes=a.LeftStrokes+a.RightStrokes;
            yield return null;yield return new WaitForEndOfFrame();yield return null;yield return new WaitForEndOfFrame();
            Check(strokes==a.LeftStrokes+a.RightStrokes,a,"pause credited scratch");Time.timeScale=1;
        }
    }
    Check(!a.IsRunning,a,"scratch timed out");if(a.IsRunning)a.CancelForTransition();
    Check(completions==1&&left>0&&right>0,a,"both hands must contact before one completion");
    Check(cat.GetComponent<CharacterController>().enabled&&!cat.IsMovementPhysicallyLocked,a,"scratch leaked control");
    Row(a,"scratch-known-stance",completions==1?"measured":"failed");
}

// Insert INSIDE GroundContactStartTests. Diagnostic only: a passing NUnit result
// means five captured stances were measured, never that Scratch is accepted.
// Calls the production resolver's private math/query methods, so geometry and
// source-path rejection cannot be confused with an independently changed rule.
[UnityTest, Timeout(90000)]
public IEnumerator DiagnoseFiveScratchSourceGates()
{
    var summary = new List<string> { "case,root,yaw,body,cc,endpoints,fixedClear,best4,phase4,pitch4,bestPairA,phaseA,pitchA,bestPairB,phaseB,pitchB,mathCandidates,safeTrajectories,publicSweep,reason" };
    var details = new List<string> { "case,path,sourcePhase,contactPhase,pitch,region,clear,start,end,radius,boundsMin,boundsMax,maxDepth,blockingCollider,normal" };
    try
    {
        yield return Prepare("LivingRoom_Level01");
        CatBreedService.Select("russian-blue"); yield return null; yield return null;
        var activity = InRoom(x => x is ScratchPostActivity).Cast<ScratchPostActivity>().Single();
        yield return EquipTarget(activity);
        Vector3 centre = activity.transform.TransformPoint(Field<Vector3>(activity, "ropeCenter"));
        Vector3 outward = Flat(Field<Transform>(activity, "scratchPoint").position - centre).normalized;
        Assert.That(outward.sqrMagnitude, Is.GreaterThan(.5f));
        float spread = HomeStoreService.IsFixedRoomProduct(activity.StoreProductId) ? .025f : .045f;
        var stances = new[] { new Vector3(.58f,-.05f,0), new Vector3(.58f,-.05f,-10),
            new Vector3(.70f,0,0), new Vector3(.70f,-.05f,0), new Vector3(.70f,.05f,0) };
        var type = typeof(CatPawReachResolver);
        const BindingFlags hidden = BindingFlags.Static | BindingFlags.NonPublic;
        var transformSource = type.GetMethod("TransformSource", hidden);
        var reachMargin = type.GetMethod("ReachMargin", hidden);
        var fixedClearMethod = type.GetMethod("FixedTrajectoryClear", hidden);
        var trajectoryClear = type.GetMethod("TrajectoryClear", hidden);
        Assert.That(transformSource, Is.Not.Null); Assert.That(reachMargin, Is.Not.Null);
        Assert.That(fixedClearMethod, Is.Not.Null); Assert.That(trajectoryClear, Is.Not.Null);
        int reproduced = 0;
        for (int index = 0; index < stances.Length; index++)
        {
            var stance = stances[index];
            Quaternion rotation = Quaternion.LookRotation(-outward) * Quaternion.Euler(0, stance.z, 0);
            Place(centre + outward * stance.x + Vector3.Cross(Vector3.up, outward) * stance.y, rotation);
            var guard = (CatBodyGuard)typeof(CatMovement).GetField("bodyGuard", Private).GetValue(cat);
            bool body = cat.IsBodyPoseClear(cat.transform.position, rotation);
            bool cc = guard.IsControllerClear(cat.transform.position, rotation);
            var targets = new Vector3[4]; int endpoints = 0;
            float[] heights = { .48f, .34f, .48f, .34f }, sides = { -spread, -spread, spread, spread };
            for (int i = 0; i < 4; i++)
                if (ScratchSurfaceRay(activity, heights[i], sides[i], out var hit))
                { targets[i] = hit.point + hit.normal * .005f; endpoints++; }
            Assert.That(body && cc && endpoints == 4, Is.True, "Captured stance changed: " + index);
            var visual = cat.GetComponentInChildren<CatBreedVisualTag>();
            var entry = CatPawReachCatalog.Load().Find(visual.BreedId, CatActivityPose.Scratch);
            object source = transformSource.Invoke(null, new object[] { entry, visual.transform.localToWorldMatrix, visual.transform.lossyScale });
            Assert.That(source, Is.Not.Null);
            bool fixedClear = (bool)fixedClearMethod.Invoke(null, new[] { (object)cat, source });
            var contacts = (Array)ScratchDiagnosticField(source, "contacts");
            float best4 = float.NegativeInfinity, bestA = best4, bestB = best4;
            float phase4 = 0, pitch4 = 0, phaseA = 0, pitchA = 0, phaseB = 0, pitchB = 0;
            int mathCandidates = 0, safeTrajectories = 0;
            foreach (object sample in contacts)
            for (float pitch = 0; pitch <= CatPawReachResolver.MaximumChestPitch; pitch += 1f)
            {
                float phase = (float)ScratchDiagnosticField(sample, "phase");
                Quaternion bend = Quaternion.AngleAxis(pitch, cat.transform.right);
                var margins = new float[4];
                for (int i = 0; i < 4; i++)
                    margins[i] = (float)reachMargin.Invoke(null, new[] { sample, (object)bend,
                        ScratchDiagnosticField(sample, i < 2 ? "left" : "right"), targets[i] });
                float all = Mathf.Min(margins[0], margins[1], margins[2], margins[3]);
                float pairA = Mathf.Min(margins[0], margins[3]); // left top / right bottom
                float pairB = Mathf.Min(margins[1], margins[2]); // left bottom / right top
                if (all > best4) { best4 = all; phase4 = phase; pitch4 = pitch; }
                if (pairA > bestA) { bestA = pairA; phaseA = phase; pitchA = pitch; }
                if (pairB > bestB) { bestB = pairB; phaseB = phase; pitchB = pitch; }
                if (all < .004f) continue;
                mathCandidates++;
                if (fixedClear && (bool)trajectoryClear.Invoke(null, new object[] { cat, source, phase, pitch, 0f })) safeTrajectories++;
            }
            bool publicSweep = CatPawReachResolver.TryResolveSweep(cat, targets[0], targets[1], targets[2], targets[3], CatActivityPose.Scratch, out _);
            string reason = best4 < .004f ? "four-endpoint-math" : !fixedClear ? "fixed-source-body" : safeTrajectories == 0 ? "bent-source-body" : "resolver-disagreement";
            summary.Add(string.Join(",", index, ScratchDiagnosticPoint(cat.transform.position), ScratchDiagnosticNumber(rotation.eulerAngles.y),
                body, cc, endpoints, fixedClear, ScratchDiagnosticNumber(best4), phase4, pitch4,
                ScratchDiagnosticNumber(bestA), phaseA, pitchA, ScratchDiagnosticNumber(bestB), phaseB, pitchB,
                mathCandidates, safeTrajectories, publicSweep, publicSweep ? "accepted" : reason));
            // Full per-region bounds at the best four-endpoint pose and both
            // opposed-pair alternatives. Includes all fixed pelvis samples.
            ScratchDiagnosticRegions(index, "four", source, phase4, pitch4, details);
            if (phaseA != phase4 || pitchA != pitch4) ScratchDiagnosticRegions(index, "pairA", source, phaseA, pitchA, details);
            if (phaseB != phase4 || pitchB != pitch4) ScratchDiagnosticRegions(index, "pairB", source, phaseB, pitchB, details);
            reproduced++;
            yield return null;
        }
        Assert.That(reproduced, Is.EqualTo(5), "Diagnostic coverage only; inspect CSV for acceptance.");
    }
    finally
    {
        File.WriteAllLines(Root + "/scratch-source-gates.csv", summary);
        File.WriteAllLines(Root + "/scratch-source-regions.csv", details);
    }
}

static object ScratchDiagnosticField(object instance, string name) => instance.GetType().GetField(name,
    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(instance);
static string ScratchDiagnosticNumber(float value) => value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
static string ScratchDiagnosticPoint(Vector3 value) => ScratchDiagnosticNumber(value.x) + ";" + ScratchDiagnosticNumber(value.y) + ";" + ScratchDiagnosticNumber(value.z);

void ScratchDiagnosticRegions(int index, string path, object source, float contactPhase, float pitch, List<string> rows)
{
    var guard = (CatBodyGuard)typeof(CatMovement).GetField("bodyGuard", Private).GetValue(cat);
    var solidMethod = typeof(CatBodyGuard).GetMethod("Solid", Private);
    var penetrationMethod = typeof(CatBodyGuard).GetMethod("Penetration", Private);
    foreach (object sample in (Array)ScratchDiagnosticField(source, "all"))
    {
        float phase = (float)ScratchDiagnosticField(sample, "phase");
        Vector3 pivot = (Vector3)ScratchDiagnosticField(sample, "pivot");
        float envelope = Mathf.Clamp01(phase <= contactPhase ? phase / Mathf.Max(.0001f, contactPhase) : (1f - phase) / Mathf.Max(.0001f, 1f - contactPhase));
        Quaternion bend = Quaternion.AngleAxis(pitch * envelope, cat.transform.right);
        var probes = new List<CatBodyGuardCatalog.Probe> { (CatBodyGuardCatalog.Probe)ScratchDiagnosticField(sample, "pelvis") };
        foreach (CatBodyGuardCatalog.Probe raw in (CatBodyGuardCatalog.Probe[])ScratchDiagnosticField(sample, "upper"))
        {
            var p = raw; p.start = pivot + bend * (p.start - pivot); p.end = pivot + bend * (p.end - pivot); probes.Add(p);
        }
        foreach (var probe in probes)
        {
            bool clear = cat.IsInteractionBodyClear(new[] { probe });
            float maxDepth = 0; string blocker = ""; Vector3 normal = Vector3.zero;
            foreach (var collider in Physics.OverlapCapsule(probe.start, probe.end, probe.radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!(bool)solidMethod.Invoke(guard, new object[] { collider })) continue;
                object[] args = { probe.start, probe.end, probe.radius, collider, Vector3.zero, 0f };
                if (!(bool)penetrationMethod.Invoke(guard, args)) continue;
                float depth = (float)args[5]; if (depth <= maxDepth) continue;
                maxDepth = depth; normal = (Vector3)args[4];
                blocker = collider.gameObject.scene.name + "/" + collider.name;
                for (var parent = collider.transform.parent; parent != null; parent = parent.parent) blocker = parent.name + "/" + blocker;
            }
            Vector3 extent = Vector3.one * probe.radius;
            rows.Add(string.Join(",", index, path, phase, contactPhase, pitch, probe.region, clear,
                ScratchDiagnosticPoint(probe.start), ScratchDiagnosticPoint(probe.end), ScratchDiagnosticNumber(probe.radius),
                ScratchDiagnosticPoint(Vector3.Min(probe.start, probe.end) - extent), ScratchDiagnosticPoint(Vector3.Max(probe.start, probe.end) + extent),
                ScratchDiagnosticNumber(maxDepth), "\"" + blocker.Replace("\"", "\"\"") + "\"", ScratchDiagnosticPoint(normal)));
        }
    }
}

}
