using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class FlowerContactPolishTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    HomeStoreSaveState saved;
    string breed;
    float capture;
    const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    readonly PreparedInteractionStartTests skinObserver = new PreparedInteractionStartTests();
    Mesh skinMesh;
    readonly System.Reflection.MethodInfo skinDepth = typeof(PreparedInteractionStartTests).GetMethod("ActualSkinDepth", Private);
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        saved = HomeStoreService.CaptureState(); breed = CatBreedService.SelectedBreedId;
        capture = Time.captureDeltaTime; Time.captureFramerate = 30;
        skinMesh = new Mesh();
    }
    [TearDown] public void After()
    {
        if (CatActivity.Active != null) CatActivity.Active.CancelForTransition();
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(saved);
        CatBreedService.Select(breed); Time.timeScale = 1; Time.captureDeltaTime = capture;
        if (skinMesh != null) Object.DestroyImmediate(skinMesh);
    }
    static void Place(CatMovement cat, Vector3 point, Quaternion rotation)
    {
        var cc = cat.GetComponent<CharacterController>(); cc.enabled = false;
        point.y = .05f; cat.transform.SetPositionAndRotation(point, rotation);
        cc.enabled = true; Physics.SyncTransforms();
    }
    static IEnumerator FindStance(CatMovement cat, SitLookActivity activity, System.Action<bool> completed)
    {
        var field = typeof(SitLookActivity).GetField("visibleLookTargets",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var targets = ((Vector3[])field.GetValue(activity)).Select(activity.transform.TransformPoint)
            .OrderBy(p => Vector3.ProjectOnPlane(p - activity.RoutineEntryPoint.position, Vector3.up).sqrMagnitude).ToArray();
        // Keep the proven centre and 5 mm neighbour first for every breed.
        // Then vary direction and distance before spending the whole deadline
        // on tiny neighbours of one stance. The full old grid remains available.
        // No point is injected into an activity or its accepted start pose.
        var offsets = new[]{Vector2.zero,new Vector2(0,-.005f),new Vector2(.005f,0),new Vector2(.005f,-.005f),
            new Vector2(-.005f,0),new Vector2(0,.005f),new Vector2(-.005f,-.005f),new Vector2(.005f,.005f),new Vector2(-.005f,.005f)};
        int attempts = 0;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var searchRows = new List<string> { "attempts,seconds,ready,point,yaw,pending,sliceSeconds,offsetRight,offsetForward" };
        string searchPath = Root + "/flower-search-" + CatBreedService.SelectedBreedId + ".csv";
        var reaches = new[] { .40f, .38f, .42f, .36f, .44f, .46f, .34f, .50f, .54f };
        var yaws = new[] { -80f, 80f, -90f, 90f, -65f, 65f, -45f, 45f, 0f };
        foreach (var target in targets)
        foreach (var candidate in FlowerStanceOrder(reaches.Length,yaws.Length,offsets.Length))
        {
            float reach = reaches[candidate.x], yaw = yaws[candidate.y];
            Vector2 offset = offsets[candidate.z];
            if (timer.Elapsed.TotalSeconds >= 45)
            {
                searchRows.Add(System.FormattableString.Invariant($"{attempts},{timer.Elapsed.TotalSeconds:F3},false,timeout,0,false,0,0,0"));
                System.IO.File.WriteAllLines(searchPath,searchRows);completed(false);yield break;
            }
            Vector3 authored = target + activity.transform.forward * reach; authored.y = .05f;
            Vector3 heading = target - authored; heading.y = 0;
            Vector3 point = authored + activity.transform.right*offset.x + activity.transform.forward*offset.y;
            Place(cat,point,Quaternion.LookRotation(heading)*Quaternion.Euler(0,yaw,0));
            double began=timer.Elapsed.TotalSeconds,until=System.Math.Min(45,began+4);
            bool ready = activity.TryGetPromptDistance(cat, out _);
            // An incomplete solver is not a reachable stance. Give it bounded
            // production work, then let another real nearby stance be tested.
            while(!ready&&activity.IsStartSearchPending&&timer.Elapsed.TotalSeconds<until)
            {yield return null;ready=activity.TryGetPromptDistance(cat,out _);}
            attempts++;
            searchRows.Add(System.FormattableString.Invariant($"{attempts},{timer.Elapsed.TotalSeconds:F3},{ready},\"{point.ToString("G9")}\",{cat.transform.eulerAngles.y:G9},{activity.IsStartSearchPending},{timer.Elapsed.TotalSeconds-began:F4},{offset.x:F4},{offset.y:F4}"));
            System.IO.File.WriteAllLines(searchPath,searchRows);
            if(ready){completed(true);yield break;}
            // Keep the next stance in a new frame so an exhausted shared
            // search slice does not consume its initial opportunity.
            yield return null;
        }
        searchRows.Add(System.FormattableString.Invariant($"{attempts},{timer.Elapsed.TotalSeconds:F3},false,exhausted,0,false,0,0,0"));
        System.IO.File.WriteAllLines(searchPath,searchRows);completed(false);
    }

    static IEnumerable<Vector3Int> FlowerStanceOrder(int reachCount,int yawCount,int offsetCount)
    {
        // Preserve the proven centre and its 5 mm neighbour, then sample
        // genuinely different existing distances and facing directions. A side
        // reach can put the forearm through the planter even when the endpoint
        // reaches a leaf. No candidate bypasses current body or full arm gates.
        var first = new[] {
            new Vector3Int(0,0,0), new Vector3Int(0,0,1),
            new Vector3Int(7,6,0), new Vector3Int(7,7,0),
            new Vector3Int(8,8,0), new Vector3Int(6,8,0),
            new Vector3Int(6,6,0), new Vector3Int(6,7,0),
            new Vector3Int(7,8,0), new Vector3Int(7,0,0),
            new Vector3Int(8,6,0), new Vector3Int(8,7,0),
            new Vector3Int(7,1,0), new Vector3Int(0,6,0),
            new Vector3Int(0,7,0), new Vector3Int(0,8,0)
        };
        var emitted = new HashSet<Vector3Int>();
        foreach(var candidate in first)
            if(emitted.Add(candidate))yield return candidate;
        for(int offset=0;offset<offsetCount;offset++)
        for(int reach=0;reach<reachCount;reach++)
        for(int yaw=0;yaw<yawCount;yaw++)
        {
            var candidate = new Vector3Int(reach,yaw,offset);
            if(emitted.Add(candidate))yield return candidate;
        }
    }

    [UnityTest,Timeout(60000)] public IEnumerator CompactSurface_PauseDuringReturn_CancelAndRetry()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();
        store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");
        var cat=Object.FindAnyObjectByType<CatMovement>();
        yield return QaBreedReadiness.WaitForSelected(cat,"domestic-shorthair");
        cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        bool ready=false;yield return FindStance(cat,activity,v=>ready=v);Assert.That(ready,Is.True);
        Assert.That(activity.TryGetStartPose(cat,out var start),Is.True);
        Assert.That(start.PawPlan.Surface.ReturnToStart,Is.True);
        RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        var animation=cat.GetComponent<CatActivityAnimation>();
        var phaseField=typeof(CatActivityAnimation).GetField("phase",Private);
        float until=Time.realtimeSinceStartup+8;bool returning=false;
        while(activity.IsRunning&&Time.realtimeSinceStartup<until)
        {
            yield return new WaitForEndOfFrame();
            float phase=(float)phaseField.GetValue(animation);
            if(activity.ContactCount>0&&animation.CurrentPose==CatActivityPose.Scratch&&
                phase>.02f&&phase<start.PawPlan.SourcePhase-.04f){returning=true;break;}
            yield return null;
        }
        Assert.That(returning,Is.True,"Exercise the actual played return interval");
        Time.timeScale=0;
        var bones=cat.GetComponentInChildren<Animator>().GetComponentsInChildren<Transform>();
        var points=bones.Select(t=>t.position).ToArray();
        Vector3 root=cat.transform.position;Quaternion heading=cat.transform.rotation;
        for(int frame=0;frame<3;frame++)
        {
            yield return null;yield return new WaitForEndOfFrame();
            for(int i=0;i<bones.Length;i++)Assert.That(Vector3.Distance(points[i],bones[i].position),Is.LessThan(.0002f));
            Assert.That(Vector3.Distance(root,cat.transform.position),Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(heading,cat.transform.rotation),Is.LessThan(.001f));
        }
        activity.CancelForTransition();Assert.That(activity.IsRunning,Is.False);
        Assert.That(cat.GetComponent<CatPawReachMotion>().IsActive,Is.False);
        Time.timeScale=1;yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        yield return null;RoomPlayModeSupport.ProvisionNeeds();
        Assert.That(activity.TryStart(cat),Is.True,"The same real player stance must start again after cancellation");
        activity.CancelForTransition();yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
    }

    [UnityTest, Timeout(90000)] public IEnumerator OneBreed_ReachRealFlowers_KeepRoot_AndReleaseContact() => ReachFlowers(1);
    [UnityTest, Timeout(360000)] public IEnumerator TenBreeds_ReachRealFlowers_KeepRoot_AndReleaseContact() => ReachFlowers(int.MaxValue);
    [UnityTest, Timeout(90000)] public IEnumerator BritishShorthair_ReachRealFlowers_KeepRoot_AndReleaseContact() => ReachFlowers(int.MaxValue,"british-shorthair");
    [UnityTest, Timeout(90000)] public IEnumerator Longhair_NearbyLegalStance_ReachRealFlowers_KeepRoot_AndReleaseContact() => ReachFlowers(int.MaxValue,"domestic-longhair");
    [UnityTest, Timeout(90000)] public IEnumerator MaineCoon_ReachRealFlowers_KeepRoot_AndReleaseContact() => ReachFlowers(int.MaxValue,"maine-coon");
    IEnumerator ReachFlowers(int breedLimit,string onlyBreed=null)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store = HomeStoreSaveState.CreateDefault();
        store.ownedProductIds = HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store); yield return null;
        var cat = Object.FindAnyObjectByType<CatMovement>();
        cat.GetComponent<CatIdleBehavior>().enabled = false;
        var activity = CatActivity.Registered.OfType<SitLookActivity>().Single(a => a.Kind == CatActivityKind.RailingSwat);
        var catalog = CatBreedCatalog.Load();
        typeof(PreparedInteractionStartTests).GetField("cat", Private).SetValue(skinObserver, cat);
        typeof(PreparedInteractionStartTests).GetField("skinSample", Private).SetValue(skinObserver, skinMesh);
        var solids = Object.FindObjectsByType<Collider>().Where(c => c.enabled && !c.isTrigger &&
            c.gameObject.scene == cat.gameObject.scene && c.GetComponentInParent<CatMovement>() == null && c.bounds.max.y > .12f).ToArray();
        typeof(PreparedInteractionStartTests).GetField("roomSolids", Private).SetValue(skinObserver, solids);
        var rows = new List<string> { "breed,contactCount,pawDistance,rootDrift,yawDrift,limbLengthChange,rearSupportChange,maxChestPitch,maxChestYaw,target,root,actualSkinDepth,deepestCollider,deepestPoint,deepestVertex,deepestPhase,deepestFrame,approachLift,approachNormal" };
        var skinFailures = new List<string>();
        var readinessRows = new List<string> { "breed,ready" };
        for (int index = 0; index < Mathf.Min(catalog.Count, breedLimit); index++)
        {
            if(onlyBreed!=null&&catalog.Get(index).Id!=onlyBreed)continue;
            string selectedBreed=catalog.Get(index).Id;
            CatBreedService.Select(selectedBreed);
            float breedDeadline=Time.realtimeSinceStartup+15;
            while(Time.realtimeSinceStartup<breedDeadline &&
                (cat.GetComponentInChildren<CatBreedVisualTag>()?.BreedId!=selectedBreed||CatPawReachCatalog.HasPendingLoads))yield return null;
            Assert.That(cat.GetComponentInChildren<CatBreedVisualTag>().BreedId,Is.EqualTo(selectedBreed),"Measure the effective live breed after its production preload barrier");
            Assert.That(CatPawReachCatalog.HasPendingLoads,Is.False);
            yield return null;
            bool ready = false; yield return FindStance(cat, activity, value => ready = value);
            readinessRows.Add(selectedBreed + "," + ready);
            System.IO.File.WriteAllLines(Root + "/flower-readiness-summary.csv", readinessRows);
            if (!ready)
            {
                skinFailures.Add(selectedBreed + ": no reachable player stance within the unchanged search budget");
                continue;
            }
            yield return null; RoomPlayModeSupport.ProvisionNeeds();
            Vector3 origin = cat.transform.position; Quaternion heading = cat.transform.rotation;
            Assert.That(activity.TryGetStartPose(cat,out var prepared),Is.True);
            Assert.That(prepared.PawPlan.Surface,Is.Not.Null,"flower must use visible skin endpoint");
            var surfacePlan=prepared.PawPlan.Surface;
            Assert.That(activity.TryStart(cat), Is.True);
            var arm = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-upper_arm.L");
            var fore = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-forearm.L");
            var hand = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-hand.L");
            var rightArm = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-upper_arm.R");
            var rightFore = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-forearm.R");
            var rightHand = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-hand.R");
            float[] lengths = { Vector3.Distance(arm.position,fore.position), Vector3.Distance(fore.position,hand.position),
                Vector3.Distance(rightArm.position,rightFore.position), Vector3.Distance(rightFore.position,rightHand.position) };
            float limbChange = 0, rearChange = 0, pitch = 0, chestYaw = 0;
            float maximumBindError=0,maximumSurfaceDistance=0;int measuredContacts=0;
            float deepestPhase = -1; int deepestFrame = -1; float maximumSkin = 0; string deepest = ""; Vector3 deepestPoint = Vector3.zero; int deepestVertex = -1; int frame = 0; bool captured = false;
            float drift = 0, turn = 0, deadline = Time.realtimeSinceStartup + 12;
            while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                frame++;
                var contact=cat.GetComponent<CatToyContactMotion>();
                if(contact!=null&&!float.IsPositiveInfinity(contact.Distance))
                {
                    var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(skinMesh,true);
                    var actual=skinMesh.vertices;Vector3 patch=Vector3.zero;
                    foreach(int vertex in surfacePlan.Vertices)
                        patch+=skin.transform.TransformPoint(actual[surfacePlan.Definitions[vertex].vertexIndex]);
                    patch/=surfacePlan.Vertices.Length;
                    maximumBindError=Mathf.Max(maximumBindError,Vector3.Distance(patch,contact.LastContactPosition));
                    if(contact.Distance<=.025f)
                    {measuredContacts++;maximumSurfaceDistance=Mathf.Max(maximumSurfaceDistance,ActualLeafDistance(surfacePlan.Collider,patch));}
                }
                if (frame % 2 == 0)
                {
                    float depth = (float)skinDepth.Invoke(skinObserver, null);
                    if (depth > maximumSkin) { maximumSkin = depth; deepest = skinObserver.DeepestSkinCollider;
                        deepestPoint = skinObserver.DeepestSkinPoint; deepestVertex = skinObserver.DeepestSkinVertex;
                        deepestPhase=(float)typeof(CatActivityAnimation).GetField("phase",Private).GetValue(cat.GetComponent<CatActivityAnimation>()); deepestFrame=frame;
                        if (index==0 && depth>.025f) CaptureFlower(cat, activity.ActiveLookTarget, "peak-depth"); }
                }
                if (!captured && activity.ContactCount == 1 && (index == 0 || catalog.Get(index).Id == "oriental-shorthair" || catalog.Get(index).Id == "persian"))
                { CaptureFlower(cat, activity.ActiveLookTarget, catalog.Get(index).Id); captured = true; }
                drift = Mathf.Max(drift, Vector3.Distance(origin, cat.transform.position));
                turn = Mathf.Max(turn, Quaternion.Angle(heading, cat.transform.rotation));
                float[] measured = { Vector3.Distance(arm.position,fore.position), Vector3.Distance(fore.position,hand.position),
                    Vector3.Distance(rightArm.position,rightFore.position), Vector3.Distance(rightFore.position,rightHand.position) };
                for (int link = 0; link < lengths.Length; link++) limbChange = Mathf.Max(limbChange, Mathf.Abs(measured[link] - lengths[link]));
                var reachMotion = cat.GetComponent<CatPawReachMotion>();
                if (reachMotion != null)
                {
                    rearChange = Mathf.Max(rearChange, reachMotion.RearSupportDisplacement);
                    pitch = Mathf.Max(pitch, reachMotion.AppliedChestPitch);
                    chestYaw = Mathf.Max(chestYaw, Mathf.Abs(reachMotion.AppliedChestYaw));
                }
            }
            rows.Add(System.FormattableString.Invariant($"{catalog.Get(index).Id},{activity.ContactCount},{activity.MinimumPawDistance:F5},{drift:F5},{turn:F3},{limbChange:F6},{rearChange:F6},{pitch:F2},{chestYaw:F2},\"{activity.ActiveLookTarget}\",\"{origin}\",{maximumSkin:F6},\"{deepest}\",\"{deepestPoint}\",{deepestVertex},{deepestPhase:F6},{deepestFrame},{surfacePlan.ApproachLift:F6},{PawDiagnosticPoint(surfacePlan.ApproachNormal)}"));
            System.IO.File.WriteAllLines(Root + "/flower-contacts.csv", rows);
            Assert.That(activity.IsRunning, Is.False, "finishes");
            Assert.That(activity.ContactCount, Is.EqualTo(3), catalog.Get(index).Id + " each paw reaches real flower surface");
            Assert.That(activity.MinimumPawDistance, Is.LessThanOrEqualTo(.025f));
            Assert.That(measuredContacts,Is.GreaterThan(0));
            Assert.That(maximumBindError,Is.LessThan(.0002f),"bind-weight endpoint must equal independently baked visible skin patch");
            Assert.That(maximumSurfaceDistance,Is.LessThan(.0251f),"visible skin patch must touch the actual leaf triangle mesh");
            Assert.That(drift, Is.LessThan(.001f)); Assert.That(turn, Is.LessThan(.2f));
            if (maximumSkin > .025f) skinFailures.Add(catalog.Get(index).Id + ": " + maximumSkin + " / " + deepest);
            Assert.That(limbChange, Is.LessThan(.002f), "source foreleg lengths remain unchanged");
            Assert.That(rearChange, Is.LessThan(.0001f), "chest reaction cannot displace hips or hind paws");
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(float.IsPositiveInfinity(cat.GetComponent<CatToyContactMotion>().Distance), Is.True);
        }
        Assert.That(skinFailures, Is.Empty, string.Join("; ", skinFailures));
    }

    [UnityTest] public IEnumerator NativeFlowerPose_ReachDiagnostic()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store = HomeStoreSaveState.CreateDefault();
        store.ownedProductIds = HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store); yield return null;
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var activity = CatActivity.Registered.OfType<SitLookActivity>().Single(a => a.Kind == CatActivityKind.RailingSwat);
        bool ready = false; yield return FindStance(cat, activity, value => ready = value);
        Assert.That(ready, Is.True);
        RoomPlayModeSupport.ProvisionNeeds(); Assert.That(activity.TryStart(cat), Is.True);
        activity.StopAllCoroutines();
        cat.GetComponent<CatToyContactMotion>().Clear();
        var animation = cat.GetComponent<CatActivityAnimation>();
        var rows = new List<string> { "pose,phase,hand,shoulder,armReach,target,unreachable" };
        var arm = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-upper_arm.L");
        var fore = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-forearm.L");
        var hand = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-hand.L");
        foreach (var pose in new[] { CatActivityPose.Paw, CatActivityPose.Scratch, CatActivityPose.BatLeft, CatActivityPose.Push })
        for (int phase = 0; phase < 11; phase++)
        {
            animation.SetTimedPose(pose, phase * .1f); yield return new WaitForEndOfFrame();
            float reach = Vector3.Distance(arm.position, fore.position) + Vector3.Distance(fore.position, hand.position);
            rows.Add(System.FormattableString.Invariant($"{pose},{phase * .1f:F1},\"{hand.position}\",\"{arm.position}\",{reach:F4},\"{activity.ActiveLookTarget}\",{Vector3.Distance(arm.position,activity.ActiveLookTarget)-reach:F4}"));
        }
        System.IO.File.WriteAllLines(Root + "/flower-native-poses.csv", rows);
        activity.CancelForTransition();
    }

// Add this method inside FlowerContactPolishTests after the active native batch ends.
// It only samples original animation poses in the isolated QA world, records
// bones, and restores the activity through normal cancellation.
[UnityTest, Timeout(180000)] public IEnumerator NativeFlowerAnatomy_SourcePoseDiagnostic()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    var store = HomeStoreSaveState.CreateDefault();
    store.ownedProductIds = HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
    HomeStoreService.ApplySavedState(store); yield return null;
    var cat = Object.FindAnyObjectByType<CatMovement>();
    cat.GetComponent<CatIdleBehavior>().enabled = false;
    var activity = CatActivity.Registered.OfType<SitLookActivity>().Single(a => a.Kind == CatActivityKind.RailingSwat);
    var names = new[] { "DEF-spine", "DEF-spine.001", "DEF-spine.002", "DEF-spine.003", "DEF-spine.004", "DEF-spine.005", "DEF-spine.006",
        "DEF-upper_arm.L", "DEF-forearm.L", "DEF-hand.L", "DEF-upper_arm.R", "DEF-forearm.R", "DEF-hand.R",
        "DEF-thigh.L", "DEF-shin.L", "DEF-foot.L", "DEF-thigh.R", "DEF-shin.R", "DEF-foot.R" };
    var rows = new List<string> { "breed,pose,phase,bone,wx,wy,wz,rootLocalX,rootLocalY,rootLocalZ,parent,rootX,rootY,rootZ,rootYaw" };
    foreach (string id in new[] { "domestic-shorthair", "british-shorthair", "oriental-shorthair" })
    {
        CatBreedService.Select(id); yield return null; yield return null;
        bool ready = false; yield return FindStance(cat, activity, value => ready = value);
        Assert.That(ready, Is.True, id);
        RoomPlayModeSupport.ProvisionNeeds(); Assert.That(activity.TryStart(cat), Is.True);
        activity.StopAllCoroutines(); cat.GetComponent<CatToyContactMotion>().Clear();
        var animation = cat.GetComponent<CatActivityAnimation>();
        foreach (var pose in new[] { CatActivityPose.GentleKnead, CatActivityPose.Scratch, CatActivityPose.Paw,
            CatActivityPose.BatLeft, CatActivityPose.BatRight, CatActivityPose.Push, CatActivityPose.Stretch,
            CatActivityPose.Tug, CatActivityPose.Sit, CatActivityPose.StandUp })
        foreach (float phase in new[] { 0f, .3f, .42f, .55f, .7f, 1f })
        {
            animation.SetTimedPose(pose, phase); yield return new WaitForEndOfFrame();
            foreach (var name in names)
            {
                var bone = CatBreedVisualFactory.FindDescendant(cat.transform, name);
                if (bone == null) continue;
                var p = bone.position; var local = cat.transform.InverseTransformPoint(p); var root = cat.transform.position;
                rows.Add(System.FormattableString.Invariant($"{id},{pose},{phase:F2},{name},{p.x:F6},{p.y:F6},{p.z:F6},{local.x:F6},{local.y:F6},{local.z:F6},{bone.parent.name},{root.x:F6},{root.y:F6},{root.z:F6},{cat.transform.eulerAngles.y:F3}"));
            }
        }
        activity.CancelForTransition();
        yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
    }
    System.IO.File.WriteAllLines(Root + "/flower-source-anatomy.csv", rows);
}

// Insert inside FlowerContactPolishTests only after the current native run ends.
// No full stance search: this separates the current capsule/body, line of sight,
// measured arm reach and source-body trajectory gates at 24 explicit player poses.
[UnityTest, Timeout(90000)] public IEnumerator NativeFlowerReadiness_GatesDiagnostic()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    var store = HomeStoreSaveState.CreateDefault();
    store.ownedProductIds = HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
    HomeStoreService.ApplySavedState(store);
    CatBreedService.Select("domestic-shorthair"); yield return null; yield return null;
    var cat = Object.FindAnyObjectByType<CatMovement>();
    cat.GetComponent<CatIdleBehavior>().enabled = false;
    var activity = CatActivity.Registered.OfType<SitLookActivity>().Single(a => a.Kind == CatActivityKind.RailingSwat);
    var field = typeof(SitLookActivity).GetField("visibleLookTargets", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
    var targets = ((Vector3[])field.GetValue(activity)).Select(activity.transform.TransformPoint)
        .OrderByDescending(p => Vector3.Dot(p - activity.transform.position, activity.transform.forward))
        .ThenBy(p => Vector3.ProjectOnPlane(p - activity.RoutineEntryPoint.position, Vector3.up).sqrMagnitude).Take(2).ToArray();
    var visual = cat.GetComponentInChildren<CatBreedVisualTag>();
    var entry = CatPawReachCatalog.Load().Find(visual.BreedId, CatActivityPose.Scratch);
    var source = entry.samples.Single(s => Mathf.Abs(s.phase - .42f) < .001f);
    var rows = new List<string> { "reach,stanceYaw,left,target,currentClear,sightClear,armMargin,sourceBodyClear,firstBlocked,root" };
    foreach (var target in targets)
    foreach (float reach in new[] { .38f, .40f, .42f })
    foreach (float stanceYaw in new[] { 80f, -80f, 90f, -90f })
    {
        var point = target + activity.transform.forward * reach; point.y = .05f;
        var direction = target - point; direction.y = 0;
        Place(cat, point, Quaternion.LookRotation(direction) * Quaternion.Euler(0, stanceYaw, 0));
        bool current = cat.IsInteractionPoseClear(cat.transform.position, cat.transform.rotation);
        bool sight = CatActivityApproach.HasClearSight(activity, cat, point + Vector3.up * .4f, target);
        bool left = Vector3.Dot(target - point, cat.transform.right) <= 0;
        var arm = left ? source.left : source.right;
        Vector3 lift = Vector3.up * CatPawReachCatalog.GroundClearance;
        Vector3 pivot = visual.transform.TransformPoint(source.torsoPivot) + lift;
        Vector3 upper = visual.transform.TransformPoint(arm.upper) + lift;
        Vector3 fore = visual.transform.TransformPoint(arm.fore) + lift;
        Vector3 hand = visual.transform.TransformPoint(arm.hand) + lift;
        float chestYaw = left ? -35f : 35f;
        Quaternion bend = Quaternion.AngleAxis(chestYaw, Vector3.up);
        float margin = (Vector3.Distance(upper, fore) + Vector3.Distance(fore, hand)) * .985f - Vector3.Distance(pivot + bend * (upper - pivot), target);
        bool clear = true; string blocked = "none";
        Vector3 scale = visual.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        foreach (var sample in entry.samples)
        {
            float envelope = Mathf.Clamp01(sample.phase <= .42f ? sample.phase / .42f : (1 - sample.phase) / .58f);
            pivot = visual.transform.TransformPoint(sample.torsoPivot) + lift;
            bend = Quaternion.AngleAxis(chestYaw * envelope, Vector3.up);
            foreach (var sourceProbe in sample.bodyProbes)
            {
                var probe = sourceProbe;
                probe.start = visual.transform.TransformPoint(probe.start) + lift;
                probe.end = visual.transform.TransformPoint(probe.end) + lift;
                probe.radius *= radiusScale;
                if (probe.region != "pelvis")
                {
                    probe.start = pivot + bend * (probe.start - pivot);
                    probe.end = pivot + bend * (probe.end - pivot);
                }
                if (cat.IsInteractionBodyClear(new[] { probe })) continue;
                clear = false; blocked = probe.region + ":" + sample.phase.ToString("F2", System.Globalization.CultureInfo.InvariantCulture); break;
            }
            if (!clear) break;
        }
        rows.Add(System.FormattableString.Invariant($"{reach:F2},{stanceYaw:F0},{left},\"{target}\",{current},{sight},{margin:F6},{clear},{blocked},\"{point}\""));
        System.IO.File.WriteAllLines(Root + "/flower-readiness-gates.csv", rows);
        yield return null;
    }
}


    static void CaptureFlower(CatMovement cat, Vector3 target, string name)
    {
        var camera = Camera.main; Assert.That(camera, Is.Not.Null);
        var position = camera.transform.position; var rotation = camera.transform.rotation;
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        bool orthographic = camera.orthographic; float fov = camera.fieldOfView;
        var image = new Texture2D(1100, 800, TextureFormat.RGB24, false);
        var render = new RenderTexture(1100, 800, 24);
        try
        {
            Vector3 aim = Vector3.Lerp(cat.transform.position + Vector3.up * .32f, target, .35f);
            camera.orthographic = false; camera.fieldOfView = 43;
            camera.transform.position = aim + new Vector3(1.25f, .70f, -1.25f);
            camera.transform.LookAt(aim); camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
            image.ReadPixels(new Rect(0, 0, 1100, 800), 0, 0); image.Apply();
            System.IO.File.WriteAllBytes(Root + "/flower-contact-" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
            camera.orthographic = orthographic; camera.fieldOfView = fov;
            camera.transform.SetPositionAndRotation(position, rotation); render.Release(); Object.Destroy(render); Object.Destroy(image);
        }
    }

    readonly QaMeshTopologyCache pawSurfaceTopology=new QaMeshTopologyCache();
    float ActualLeafDistance(Collider collider,Vector3 point)
    {
        Assert.That(collider,Is.TypeOf<MeshCollider>());
        var mesh=(MeshCollider)collider;var data=pawSurfaceTopology.Get(mesh.sharedMesh);float squared=float.PositiveInfinity;
        for(int i=0;i+2<data.triangles.Length;i+=3)
            squared=Mathf.Min(squared,PlaneEdgeDistanceSquared(point,
                mesh.transform.TransformPoint(data.vertices[data.triangles[i]]),
                mesh.transform.TransformPoint(data.vertices[data.triangles[i+1]]),
                mesh.transform.TransformPoint(data.vertices[data.triangles[i+2]])));
        return Mathf.Sqrt(squared);
    }
    static float PlaneEdgeDistanceSquared(Vector3 point,Vector3 a,Vector3 b,Vector3 c)
    {
        Vector3 ab=b-a,ac=c-a,normal=Vector3.Cross(ab,ac);
        float normalSquared=normal.sqrMagnitude;
        float result=Mathf.Min(SegmentDistanceSquared(point,a,b),
            Mathf.Min(SegmentDistanceSquared(point,b,c),SegmentDistanceSquared(point,c,a)));
        if(normalSquared<1e-16f)return result;
        float along=Vector3.Dot(point-a,normal);
        Vector3 projection=point-normal*(along/normalSquared),ap=projection-a;
        float aa=Vector3.Dot(ab,ab),bb=Vector3.Dot(ac,ac),cross=Vector3.Dot(ab,ac);
        float pa=Vector3.Dot(ap,ab),pb=Vector3.Dot(ap,ac),denominator=aa*bb-cross*cross;
        if(denominator<=1e-16f)return result;
        float u=(bb*pa-cross*pb)/denominator,v=(aa*pb-cross*pa)/denominator;
        if(u>=0f&&v>=0f&&u+v<=1f)result=Mathf.Min(result,along*along/normalSquared);
        return result;
    }

    static float SegmentDistanceSquared(Vector3 point,Vector3 a,Vector3 b)
    {
        Vector3 edge=b-a;float length=edge.sqrMagnitude;
        float t=length>1e-16f?Mathf.Clamp01(Vector3.Dot(point-a,edge)/length):0f;
        return (point-(a+edge*t)).sqrMagnitude;
    }


    [UnityTest,Timeout(90000)] public IEnumerator OneBreed_PawSurfacePrediction() => PawPrediction(1);
    [UnityTest,Timeout(360000)] public IEnumerator PawSurfacePrediction_MatchesNativeSkinAllBakedSourceSamples() => PawPrediction(int.MaxValue);
    IEnumerator PawPrediction(int breedLimit)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);yield return null;
        var cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
        var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        var output=new List<string>{"breed,phase,returning,patchError,leftSkinError,rightSkinError,leftArmSkinError,rightArmSkinError,contactDistance,approachLift,approachNormal"};
        var errors=new List<string>();
        foreach(var breedEntry in CatBreedCatalog.Load().Entries.Take(breedLimit))
        {
            CatBreedService.Select(breedEntry.Id);yield return null;yield return null;
            bool ready=false;yield return FindStance(cat,activity,value=>ready=value);Assert.That(ready,Is.True,breedEntry.Id);
            Assert.That(activity.TryGetStartPose(cat,out var start),Is.True);var plan=start.PawPlan;
            Assert.That(plan.Surface,Is.Not.Null);RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
            activity.StopAllCoroutines();var motion=cat.GetComponent<CatPawReachMotion>();var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
            Assert.That(plan.Surface.Entry.samples.Length,Is.EqualTo(CatPawReachCatalog.SurfaceSampleCount),"the baked proof includes the added surface-only contact neighbourhoods");
            for(int i=1;i<plan.Surface.Entry.samples.Length;i++)
                Assert.That(plan.Surface.Entry.samples[i].phase-plan.Surface.Entry.samples[i-1].phase,Is.LessThanOrEqualTo(.05001f));
            // Check every played source sample in both directions. An
            // unplayed tail is explicitly refused by the prediction API.
            if(plan.Surface.ReturnToStart)
                foreach(var unused in plan.Surface.Entry.samples.Where(v=>v.phase>plan.SourcePhase+.00001f))
                    Assert.That(CatPawReachResolver.TryPredictSurfaceSample(cat,plan,unused,out _,out _,out _),Is.False);
            foreach(var sample in plan.Surface.Entry.samples.Where(v=>!plan.Surface.ReturnToStart||v.phase<=plan.SourcePhase+.00001f))
            foreach(bool returning in plan.Surface.ReturnToStart?new[]{false,true}:new[]{false})
            {
                Assert.That(CatPawReachResolver.TryPredictSurfaceSample(cat,plan,sample,out var predictedLeft,out var predictedRight,out var predictedPatch),Is.True);
                Assert.That(CatPawReachResolver.TryPredictSurfaceArmSample(cat,plan,sample,out var predictedLeftArm,out var predictedRightArm),Is.True);
                motion.Sample(activity,plan,SurfaceRuntimePhase(sample.phase,plan.SourcePhase,returning));yield return new WaitForEndOfFrame();
                if(sample.phase==0f){yield return null;motion.Sample(activity,plan,SurfaceRuntimePhase(0,plan.SourcePhase,returning));yield return new WaitForEndOfFrame();}
                skin.BakeMesh(skinMesh,true);var vertices=skinMesh.vertices;
                float leftError=0,rightError=0;Vector3 patch=Vector3.zero;
                for(int v=0;v<predictedLeft.Length;v++)leftError=Mathf.Max(leftError,Vector3.Distance(predictedLeft[v],skin.transform.TransformPoint(vertices[plan.Surface.Entry.leftPaw[v].vertexIndex])));
                for(int v=0;v<predictedRight.Length;v++)rightError=Mathf.Max(rightError,Vector3.Distance(predictedRight[v],skin.transform.TransformPoint(vertices[plan.Surface.Entry.rightPaw[v].vertexIndex])));
                float leftArmError=0,rightArmError=0;
                for(int v=0;v<predictedLeftArm.Length;v++)leftArmError=Mathf.Max(leftArmError,Vector3.Distance(predictedLeftArm[v],skin.transform.TransformPoint(vertices[plan.Surface.Entry.leftArmSkin[v].vertexIndex])));
                for(int v=0;v<predictedRightArm.Length;v++)rightArmError=Mathf.Max(rightArmError,Vector3.Distance(predictedRightArm[v],skin.transform.TransformPoint(vertices[plan.Surface.Entry.rightArmSkin[v].vertexIndex])));
                foreach(int v in plan.Surface.Vertices)patch+=skin.transform.TransformPoint(vertices[plan.Surface.Definitions[v].vertexIndex]);
                patch/=plan.Surface.Vertices.Length;float patchError=Vector3.Distance(patch,predictedPatch);
                output.Add(System.FormattableString.Invariant($"{breedEntry.Id},{sample.phase:F3},{returning},{patchError:F6},{leftError:F6},{rightError:F6},{leftArmError:F6},{rightArmError:F6},{Vector3.Distance(patch,plan.Surface.Point):F6},{plan.Surface.ApproachLift:F6},{PawDiagnosticPoint(plan.Surface.ApproachNormal)}"));
                System.IO.File.WriteAllLines(Root+"/paw-surface-prediction.csv",output);
                if(Mathf.Max(Mathf.Max(leftArmError,rightArmError),Mathf.Max(patchError,Mathf.Max(leftError,rightError)))>.002f)errors.Add(breedEntry.Id+" / "+sample.phase+" source prediction differs from rendered skin");
                yield return null;
            }
            activity.CancelForTransition();yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
        }
        System.IO.File.WriteAllLines(Root+"/paw-surface-prediction.csv",output);
        Assert.That(errors,Is.Empty,string.Join("; ",errors));
    }
    static float SurfaceRuntimePhase(float source,float contact,bool returning)
    {
        if(!returning)return RuntimePhase(source,contact);
        float weight=1f-source/contact,lo=0,hi=1;
        for(int i=0;i<24;i++){float mid=(lo+hi)*.5f;if(Mathf.SmoothStep(0,1,mid)<weight)lo=mid;else hi=mid;}
        return .68f+.32f*(lo+hi)*.5f;
    }
    static float RuntimePhase(float source,float contact)
    {
        float weight=source<=contact?source/contact:(source-contact)/(1-contact),lo=0,hi=1;
        for(int i=0;i<24;i++){float mid=(lo+hi)*.5f;if(Mathf.SmoothStep(0,1,mid)<weight)lo=mid;else hi=mid;}
        float t=(lo+hi)*.5f;return source<=contact?.35f*t:.68f+.32f*t;
    }


// Insert these members inside FlowerContactPolishTests; no standalone class.
// Run only after the surface catalog and first native flower contact pass.
[UnityTest,Timeout(90000)] public IEnumerator SurfacePlanWarmCache_Allocations_AndFreshPhysics()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    var store=HomeStoreSaveState.CreateDefault();
    store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
    HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");
    var cat=Object.FindAnyObjectByType<CatMovement>();
    yield return QaBreedReadiness.WaitForSelected(cat,"domestic-shorthair");
    cat.GetComponent<CatIdleBehavior>().enabled=false;
    var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
    bool found=false;yield return FindStance(cat,activity,value=>found=value);Assert.That(found,Is.True);
    Assert.That(activity.TryGetStartPose(cat,out var prepared),Is.True);
    var plan=prepared.PawPlan;Assert.That(plan.Surface,Is.Not.Null);
    // The original ray result retains the exact request bits and triangle.
    // Re-raycasting the final contact could select a different surface normal.
    var surface=plan.Surface.SourceRequest;
    Assert.That(surface.collider,Is.EqualTo(plan.Surface.Collider));
    for(int i=0;i<4;i++)Assert.That(CatPawReachResolver.TryResolveSurface(cat,surface,plan.Left,plan.Pose,out _,32,35),Is.True);
    bool allReady=true;int mathCandidates=0;
    var clock=new System.Diagnostics.Stopwatch();clock.Start();
    long before=System.GC.GetAllocatedBytesForCurrentThread();
    for(int i=0;i<64;i++)
    {
        allReady&=CatPawReachResolver.TryResolveSurface(cat,surface,plan.Left,plan.Pose,out _,32,35);
        mathCandidates+=CatPawReachResolver.SurfaceMathCandidatesLastQuery;
    }
    long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;clock.Stop();
    System.IO.File.WriteAllText(Root+"/paw-surface-cache.csv",
        "requests,totalMs,managedBytes,newMathCandidates\n"+
        System.FormattableString.Invariant($"64,{clock.Elapsed.TotalMilliseconds:F4},{allocated},{mathCandidates}\n"));
    Assert.That(allReady,Is.True);Assert.That(mathCandidates,Is.Zero);
    Assert.That(allocated,Is.Zero,"stable requests must not rebuild arrays or candidate plans");

    GameObject blocker=null;
    try
    {
        blocker=new GameObject("QA new obstruction after cached paw approval");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(blocker,cat.gameObject.scene);
        blocker.transform.position=cat.transform.position+Vector3.up*.5f;
        var box=blocker.AddComponent<BoxCollider>();box.size=Vector3.one*2;
        Physics.SyncTransforms();
        Assert.That(CatPawReachResolver.TryResolveSurface(cat,surface,plan.Left,plan.Pose,out _,32,35),Is.False,
            "a cached geometry plan cannot retain permission after another collider moves into it");
        box.enabled=false;Physics.SyncTransforms();
        Assert.That(CatPawReachResolver.TryResolveSurface(cat,surface,plan.Left,plan.Pose,out _,32,35),Is.True,
            "failed physics must not be cached after the obstruction disappears");
    }
    finally{if(blocker!=null){blocker.SetActive(false);Object.Destroy(blocker);}Physics.SyncTransforms();}
}

// Insert inside the current FlowerContactPolishTests. One breed, all baked
// source phases (catalogue phases plus .05 intervals). Diagnostic, not approval.
[UnityTest,Timeout(90000)] public IEnumerator OneBreed_PawCapsuleVersusActualSkinDiagnostic() => PawCapsuleVersusSkin(false);
[UnityTest,Timeout(90000)] public IEnumerator OneBreed_AcceptedPawPlanAllBakedPhases() => PawCapsuleVersusSkin(true);
IEnumerator PawCapsuleVersusSkin(bool validateClearance)
{
    var rows=new List<string>{"sourcePhase,runtimePhase,catalogSample,activePaw,probeSide,skinGlobalDepth,skinGlobalVertex,probeStart,probeEnd,radius,collider,solid,overlapHit,computeFalse,depthFalse,computeTrue,depthTrue,normalFalse,witnessVertex,witnessInsideDepth,witnessOutwardVotes,witnessPosition,witnessInCapsule,gateClear,predictionError,reason"};
    var rays=new List<string>{"sourcePhase,probeSide,collider,vertex,direction,hit,distance,outwardDot"};
    SitLookActivity activity=null;var timer=new System.Diagnostics.Stopwatch();
    try
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
        var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
        HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");yield return null;yield return null;
        var cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
        activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
        bool ready=false;yield return FindStance(cat,activity,value=>ready=value);Assert.That(ready,Is.True);
        Assert.That(activity.TryGetStartPose(cat,out var accepted),Is.True);var plan=accepted.PawPlan;
        Assert.That(plan.Surface,Is.Not.Null);RoomPlayModeSupport.ProvisionNeeds();Assert.That(activity.TryStart(cat),Is.True);
        activity.StopAllCoroutines();var motion=cat.GetComponent<CatPawReachMotion>();var skin=cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var guard=(CatBodyGuard)typeof(CatMovement).GetField("bodyGuard",Private).GetValue(cat);
        var penetration=typeof(CatBodyGuard).GetMethod("Penetration",Private);
        var solidMethod=typeof(CatBodyGuard).GetMethod("Solid",Private);
        var query=(CapsuleCollider)typeof(CatBodyGuard).GetField("query",Private).GetValue(guard);
        Assert.That(query.enabled,Is.True,"Use the existing enabled, remote primitive query");
        var fit=typeof(CatPawReachResolver).GetMethod("FitPaw",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        Assert.That(fit,Is.Not.Null);
        var colliders=Object.FindObjectsByType<Collider>().Where(c=>c.enabled&&!c.isTrigger&&
            c.gameObject.scene==cat.gameObject.scene&&c.GetComponentInParent<CatMovement>()==null).ToArray();
        typeof(PreparedInteractionStartTests).GetField("cat",Private).SetValue(skinObserver,cat);
        typeof(PreparedInteractionStartTests).GetField("skinSample",Private).SetValue(skinObserver,skinMesh);
        typeof(PreparedInteractionStartTests).GetField("roomSolids",Private).SetValue(skinObserver,colliders);
        var phases=plan.Surface.Entry.samples.Select(s=>s.phase).Concat(Enumerable.Range(0,21).Select(i=>i*.05f))
            .Select(p=>Mathf.RoundToInt(p*10000)).Distinct().OrderBy(p=>p).Select(p=>p/10000f).ToArray();
        timer.Start(); // Excludes room loading and cold geometry/readiness.
        foreach(float phase in phases)
        {
            float runtime=RuntimePhase(phase,plan.SourcePhase);
            // Settle the first timed animator evaluation before inspecting skin.
            // This also removes the known phase-zero first-frame fixture error.
            motion.Sample(activity,plan,runtime);yield return null;
            motion.Sample(activity,plan,runtime);yield return new WaitForEndOfFrame();
            Physics.SyncTransforms();
            float actualDepth=(float)skinDepth.Invoke(skinObserver,null);var vertices=skinMesh.vertices;
            var source=plan.Surface.Entry.samples.FirstOrDefault(s=>Mathf.Abs(s.phase-phase)<.00001f);
            Vector3[] predictedLeft=null,predictedRight=null;
            if(source!=null)Assert.That(CatPawReachResolver.TryPredictSurfaceSample(cat,plan,source,out predictedLeft,out predictedRight,out _),Is.True);
            for(int side=0;side<2;side++)
            {
                var definitions=side==0?plan.Surface.Entry.leftPaw:plan.Surface.Entry.rightPaw;
                var points=new Vector3[definitions.Length];float predictionError=0;
                for(int v=0;v<points.Length;v++)
                {
                    points[v]=skin.transform.TransformPoint(vertices[definitions[v].vertexIndex]);
                    if(source!=null)predictionError=Mathf.Max(predictionError,Vector3.Distance(points[v],(side==0?predictedLeft:predictedRight)[v]));
                }
                string label=side==0?"left-paw":"right-paw";
                // Fit the ACTUAL rendered points through the production fitter.
                // Catalogue samples also report native-vs-prediction error.
                var probe=(CatBodyGuardCatalog.Probe)fit.Invoke(null,new object[]{points,label});
                bool gate=cat.IsInteractionBodyClear(new[]{probe});
                if(validateClearance)
                {
                    foreach(var vertex in points)
                        Assert.That(SegmentDistanceSquared(vertex,probe.start,probe.end),Is.LessThanOrEqualTo(probe.radius*probe.radius+.0000001f),"FitPaw must contain every actual rendered paw vertex");
                    Assert.That(cat.IsInteractionBodyClear(new[]{probe},.002f),Is.True,"accepted paw path clips geometry at source phase "+phase+" / "+label);
                    Assert.That(actualDepth,Is.LessThanOrEqualTo(.025f),"independent actual skin clips geometry at source phase "+phase);
                }
                var overlap=Physics.OverlapCapsule(probe.start,probe.end,probe.radius,~0,QueryTriggerInteraction.Ignore);
                var probeBounds=new Bounds((probe.start+probe.end)*.5f,
                    new Vector3(Mathf.Abs(probe.start.x-probe.end.x),Mathf.Abs(probe.start.y-probe.end.y),Mathf.Abs(probe.start.z-probe.end.z))+Vector3.one*probe.radius*2);
                int witnessIndex=side==0?1147:2143;
                Vector3 witness=skin.transform.TransformPoint(vertices[witnessIndex]);
                bool contained=SegmentDistanceSquared(witness,probe.start,probe.end)<=probe.radius*probe.radius+.0000001f;
                foreach(var collider in colliders)
                {
                    if(collider!=plan.Surface.Collider&&!probeBounds.Intersects(collider.bounds)&&!collider.bounds.Contains(witness))continue;
                    bool solid=(bool)solidMethod.Invoke(guard,new object[]{collider});
                    bool oldBackfaces=Physics.queriesHitBackfaces,hitFalse=false,hitTrue=false;float depthFalse=0,depthTrue=0;Vector3 normal=Vector3.zero;
                    try
                    {
                        Physics.queriesHitBackfaces=false;
                        object[] args={probe.start,probe.end,probe.radius,collider,Vector3.zero,0f};
                        hitFalse=(bool)penetration.Invoke(guard,args);normal=(Vector3)args[4];depthFalse=(float)args[5];
                        Physics.queriesHitBackfaces=true;args[4]=Vector3.zero;args[5]=0f;
                        hitTrue=(bool)penetration.Invoke(guard,args);depthTrue=(float)args[5];
                    }
                    finally{Physics.queriesHitBackfaces=oldBackfaces;}
                    float insideDepth=PawWitnessInside(collider,witness,phase,label,witnessIndex,rays,out int votes);
                    bool overlapHit=overlap.Contains(collider);
                    string reason=insideDepth>.025f&&contained&&solid?
                        !overlapHit?"contained-skin-missed-by-overlap":!hitFalse?"contained-skin-missed-by-penetration":
                        depthFalse<=.015f?"primitive-MTD-below-gate":"probe-detects-skin":
                        actualDepth>.025f?"other-phase-region-or-vertex":"no-deep-witness";
                    if(insideDepth>.025f&&!contained)reason="witness-outside-paw-envelope";
                    rows.Add(string.Join(",",PawDiagnosticNumber(phase),PawDiagnosticNumber(runtime),source!=null,plan.Left?"left":"right",label,
                        PawDiagnosticNumber(actualDepth),skinObserver.DeepestSkinVertex,PawDiagnosticPoint(probe.start),PawDiagnosticPoint(probe.end),PawDiagnosticNumber(probe.radius),
                        PawDiagnosticPath(collider.transform),solid,overlapHit,hitFalse,PawDiagnosticNumber(depthFalse),hitTrue,PawDiagnosticNumber(depthTrue),PawDiagnosticPoint(normal),
                        witnessIndex,PawDiagnosticNumber(insideDepth),votes,PawDiagnosticPoint(witness),contained,gate,PawDiagnosticNumber(predictionError),reason));
                }
            }
            System.IO.File.WriteAllLines(Root+"/paw-capsule-vs-skin.csv",rows);
            System.IO.File.WriteAllLines(Root+"/paw-capsule-witness-rays.csv",rays);
            yield return null;
        }
        timer.Stop();
        System.IO.File.WriteAllText(Root+"/paw-capsule-diagnostic-duration.txt",System.FormattableString.Invariant($"{phases.Length} source phases; {timer.Elapsed.TotalSeconds:F3}s excluding cold readiness. Diagnostic only.\n"));
        Assert.That(phases.Length,Is.LessThanOrEqualTo(45));
    }
    finally
    {
        System.IO.File.WriteAllLines(Root+"/paw-capsule-vs-skin.csv",rows);
        System.IO.File.WriteAllLines(Root+"/paw-capsule-witness-rays.csv",rays);
        if(activity!=null&&activity.IsRunning)activity.CancelForTransition();
    }
}

static string PawDiagnosticNumber(float value)=>value.ToString("F7",System.Globalization.CultureInfo.InvariantCulture);
static string PawDiagnosticPoint(Vector3 point)=>PawDiagnosticNumber(point.x)+";"+PawDiagnosticNumber(point.y)+";"+PawDiagnosticNumber(point.z);
static string PawDiagnosticPath(Transform value)
{string path=value.name;for(var t=value.parent;t!=null;t=t.parent)path=t.name+"/"+path;return "\""+path.Replace("\"","\"\"")+"\"";}
float PawWitnessInside(Collider collider,Vector3 point,float phase,string side,int vertex,List<string> rays,out int votes)
{
    votes=0;if(!collider.bounds.Contains(point))return 0;
    if(collider is BoxCollider box)
    {
        Vector3 local=box.transform.InverseTransformPoint(point)-box.center;
        Vector3 inside=box.size*.5f-new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z));
        if(inside.x<0||inside.y<0||inside.z<0)return 0;
        Vector3 scale=box.transform.lossyScale;votes=6;
        return Mathf.Min(inside.x*Mathf.Abs(scale.x),Mathf.Min(inside.y*Mathf.Abs(scale.y),inside.z*Mathf.Abs(scale.z)));
    }
    if(!(collider is MeshCollider mesh))return 0;
    var data=pawSurfaceTopology.Get(mesh.sharedMesh);float depth=float.PositiveInfinity;
    Vector3[] directions={new Vector3(.013f,1,.027f).normalized,new Vector3(1,.017f,.031f).normalized,new Vector3(.019f,.023f,1).normalized,
        -new Vector3(.013f,1,.027f).normalized,-new Vector3(1,.017f,.031f).normalized,-new Vector3(.019f,.023f,1).normalized};
    bool old=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
    try
    {
        for(int i=0;i<directions.Length;i++)
        {
            Vector3 direction=directions[i];bool hit=mesh.Raycast(new Ray(point,direction),out var measured,5);float outward=0;
            if(hit && measured.triangleIndex >= 0 && measured.triangleIndex*3+2 < data.triangles.Length)
            {
                int triangle=measured.triangleIndex*3;
                Vector3 a=mesh.transform.TransformPoint(data.vertices[data.triangles[triangle]]),
                    b=mesh.transform.TransformPoint(data.vertices[data.triangles[triangle+1]]),c=mesh.transform.TransformPoint(data.vertices[data.triangles[triangle+2]]);
                outward=Vector3.Dot(Vector3.Cross(b-a,c-a).normalized,direction);
                if(outward>0){votes++;depth=Mathf.Min(depth,measured.distance);}
            }
            rays.Add(string.Join(",",PawDiagnosticNumber(phase),side,PawDiagnosticPath(collider.transform),vertex,i,hit,
                hit?PawDiagnosticNumber(measured.distance):"",PawDiagnosticNumber(outward)));
        }
        return votes>=4?depth:0;
    }
    finally{Physics.queriesHitBackfaces=old;}
}

// Insert inside FlowerContactPolishTests. Captured native geometry is intentionally
// fixed: this tests the three previously approved unsafe samples against the real
// authored mesh without asking the tightened gate to approve that old plan.
[UnityTest,Timeout(90000)] public IEnumerator PawClearance_ThreeNativeRegressions_KeepBodyTolerance()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
    HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");yield return null;yield return null;
    var cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
    var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
    var solid=activity.GetComponentsInChildren<MeshCollider>().Single(c=>c.name=="BalconyRailingFlowers_PremiumModel");
    var probes=new[]{
        new CatBodyGuardCatalog.Probe{region="native-right-paw",start=new Vector3(-3.3965740f,0.4630989f,1.1276420f),end=new Vector3(-3.3965740f,0.4879507f,1.1276420f),radius=0.0316380f},
        new CatBodyGuardCatalog.Probe{region="native-right-paw",start=new Vector3(-3.4023050f,0.4685209f,1.1355450f),end=new Vector3(-3.4023050f,0.4897221f,1.1355450f),radius=0.0314084f},
        new CatBodyGuardCatalog.Probe{region="native-right-paw",start=new Vector3(-3.3899830f,0.4584791f,1.1282270f),end=new Vector3(-3.3899830f,0.4801493f,1.1282270f),radius=0.0316842f}
    };
    var witnesses=new[]{
        new Vector3(-3.3823140f,0.4446785f,1.1097060f),
        new Vector3(-3.3886010f,0.4504918f,1.1174280f),
        new Vector3(-3.3748250f,0.4373823f,1.1135140f)
    };
    float[] phases={.50f,.52f,.70f};var rays=new List<string>();
    var rows=new List<string>{"sourcePhase,independentInsideDepth,default15mmClear,paw2mmClear"};
    Physics.SyncTransforms();
    for(int i=0;i<probes.Length;i++)
    {
        float inside=PawWitnessInside(solid,witnesses[i],phases[i],"right-paw",2143,rays,out int votes);
        Assert.That(inside,Is.GreaterThan(.025f),"the native failure fixture must still exist");
        Assert.That(SegmentDistanceSquared(witnesses[i],probes[i].start,probes[i].end),Is.LessThanOrEqualTo(probes[i].radius*probes[i].radius));
        bool body=cat.IsInteractionBodyClear(new[]{probes[i]}),paw=cat.IsInteractionBodyClear(new[]{probes[i]},.002f);
        rows.Add(System.FormattableString.Invariant($"{phases[i]:F2},{inside:F7},{body},{paw}"));
        System.IO.File.WriteAllLines(Root+"/paw-tolerance-regression.csv",rows);
        Assert.That(body,Is.True,"default body tolerance remains 15mm");
        Assert.That(paw,Is.False,"the measured deep paw must no longer receive approval");
    }
}

[UnityTest,Timeout(90000)] public IEnumerator OneBreed_ColdPawCandidateProfile()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
    HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");yield return null;yield return null;
    var cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
    var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
    var targets=((Vector3[])typeof(SitLookActivity).GetField("visibleLookTargets",Private).GetValue(activity))
        .Select(activity.transform.TransformPoint).OrderBy(p=>Vector3.ProjectOnPlane(p-activity.RoutineEntryPoint.position,Vector3.up).sqrMagnitude).ToArray();
    Vector3 nearest=targets[0],point=nearest+activity.transform.forward*.40f;point.y=.05f;
    var rows=new List<string>{"stanceYaw,targetRank,pass,ready,ms,managedBytes,mathCandidates,physicsCandidates,contactPhase,pitch,chestYaw"};
    foreach(float yaw in new[]{80f,-80f})
    {
        Vector3 forward=nearest-point;forward.y=0;Place(cat,point,Quaternion.LookRotation(forward)*Quaternion.Euler(0,yaw,0));
        int rank=0;
        foreach(var target in targets.OrderBy(p=>Vector3.ProjectOnPlane(p-point,Vector3.up).sqrMagnitude).Take(3))
        {
            rank++;if(!CatPawReachResolver.TryMeasureSurface(activity.transform,target,out var surface))continue;
            bool left=Vector3.Dot(target-point,cat.transform.right)<=0;
            for(int pass=0;pass<2;pass++)
            {
                var clock=new System.Diagnostics.Stopwatch();long before=System.GC.GetAllocatedBytesForCurrentThread();clock.Start();
                bool ready=CatPawReachResolver.TryResolveSurface(cat,surface,left,CatActivityPose.Scratch,out var plan,32,35);
                int math=CatPawReachResolver.SurfaceMathCandidatesLastQuery,physics=CatPawReachResolver.SurfacePhysicsCandidatesLastQuery;
                while(!ready&&CatPawReachResolver.SurfaceQueryPending)
                {
                    yield return null;
                    ready=CatPawReachResolver.TryResolveSurface(cat,surface,left,CatActivityPose.Scratch,out plan,32,35);
                    math+=CatPawReachResolver.SurfaceMathCandidatesLastQuery;physics+=CatPawReachResolver.SurfacePhysicsCandidatesLastQuery;
                }
                clock.Stop();long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
                rows.Add(System.FormattableString.Invariant($"{yaw:F1},{rank},{pass},{ready},{clock.Elapsed.TotalMilliseconds:F3},{allocated},{math},{physics},{plan.SourcePhase:F2},{plan.ChestPitch:F1},{plan.ChestYaw:F1}"));
                System.IO.File.WriteAllLines(Root+"/paw-cold-candidates.csv",rows);
                yield return null;
            }
        }
    }
}


[UnityTest,Timeout(90000)] public IEnumerator CooperativePawSearch_SixtyFrames_BoundsEachCall()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    var store=HomeStoreSaveState.CreateDefault();store.ownedProductIds=HomeStoreService.GetRoomCollection(HomeRoomService.BalconyId).ToArray();
    HomeStoreService.ApplySavedState(store);CatBreedService.Select("domestic-shorthair");yield return null;yield return null;
    var cat=Object.FindAnyObjectByType<CatMovement>();cat.GetComponent<CatIdleBehavior>().enabled=false;
    yield return QaBreedReadiness.WaitForSelected(cat,"domestic-shorthair");
    var activity=CatActivity.Registered.OfType<SitLookActivity>().Single(a=>a.Kind==CatActivityKind.RailingSwat);
    var targets=((Vector3[])typeof(SitLookActivity).GetField("visibleLookTargets",Private).GetValue(activity))
        .Select(activity.transform.TransformPoint).OrderBy(p=>Vector3.ProjectOnPlane(p-activity.RoutineEntryPoint.position,Vector3.up).sqrMagnitude).ToArray();
    Vector3 target=targets[0],point=target+activity.transform.forward*.40f;point.y=.05f;
    Vector3 direction=target-point;direction.y=0;
    var rows=new List<string>{"yaw,frame,ready,pending,ms,managedBytes,mathCandidates,physicsChecks"};
    double maximum=0;int work=0;
    foreach(float yaw in new[]{80f,-80f})
    {
        Place(cat,point,Quaternion.LookRotation(direction)*Quaternion.Euler(0,yaw,0));
        Assert.That(CatPawReachResolver.TryMeasureSurface(activity.transform,target,out var surface),Is.True);
        bool left=Vector3.Dot(target-point,cat.transform.right)<=0;
        for(int frame=0;frame<60;frame++)
        {
            yield return null;
            var clock=new System.Diagnostics.Stopwatch();long before=System.GC.GetAllocatedBytesForCurrentThread();clock.Start();
            bool ready=CatPawReachResolver.TryResolveSurface(cat,surface,left,CatActivityPose.Scratch,out _,32,35);
            clock.Stop();long allocated=System.GC.GetAllocatedBytesForCurrentThread()-before;
            int math=CatPawReachResolver.SurfaceMathCandidatesLastQuery;work+=math;
            maximum=System.Math.Max(maximum,clock.Elapsed.TotalMilliseconds);
            rows.Add(System.FormattableString.Invariant($"{yaw:F1},{frame},{ready},{CatPawReachResolver.SurfaceQueryPending},{clock.Elapsed.TotalMilliseconds:F3},{allocated},{math},{CatPawReachResolver.SurfacePhysicsCandidatesLastQuery}"));
            System.IO.File.WriteAllLines(Root+"/paw-cooperative-frames.csv",rows);
            Assert.That(ready&&CatPawReachResolver.SurfaceQueryPending,Is.False);
            if(ready||!CatPawReachResolver.SurfaceQueryPending)break;
        }
    }
    Assert.That(work,Is.GreaterThan(0),"the rolling search must make progress");
    Assert.That(maximum,Is.LessThan(50),"2ms search slice plus one atomic native validation must not become a long frame");
    // Pending after 60 frames is reported, never relabeled as ready or complete.
}


[UnityTest,Timeout(90000)] public IEnumerator MeshCatalog_ColdLoad_AdvancesFramesBeforeInteraction()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    while(CatPawReachCatalog.HasPendingLoads)yield return null;
    Assert.That(CatActivity.Active,Is.Null);
    CatMeshContactSurface.EditorClearResourceCacheForQa();
    var rows=new List<string>{"sample,elapsedMs,ready"};
    var step=System.Diagnostics.Stopwatch.StartNew();CatMeshContactSurface.Preload();step.Stop();
    double maximum=step.Elapsed.TotalMilliseconds;int frames=0;float deadline=Time.realtimeSinceStartup+15;
    rows.Add(System.FormattableString.Invariant($"request,{maximum:F3},{CatMeshContactSurface.IsReady}"));
    while(!CatMeshContactSurface.IsReady)
    {
        step.Restart();yield return null;bool ready=CatMeshContactSurface.IsReady;step.Stop();frames++;
        maximum=System.Math.Max(maximum,step.Elapsed.TotalMilliseconds);
        rows.Add(System.FormattableString.Invariant($"frame{frames},{step.Elapsed.TotalMilliseconds:F3},{ready}"));
        Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));
    }
    System.IO.File.WriteAllLines(Root+"/mesh-catalog-cold-load.csv",rows);
    Assert.That(frames,Is.GreaterThan(0),"Cold geometry must yield before interaction readiness.");
    Assert.That(maximum,Is.LessThan(50),"Measure integration frames, not only the request call.");
}

[UnityTest,Timeout(90000)] public IEnumerator PawCatalog_ColdSmallShards_LoadAsyncWithoutLongFrames()
{
    yield return RoomPlayModeSupport.LoadRoomAlone("Balcony_Level01");
    CatBreedService.Select("domestic-shorthair");yield return null;yield return null;
    string breedId=CatBreedService.SelectedBreedId;
    while(CatPawReachCatalog.IsLoadingFor(breedId))yield return null;
    Assert.That(CatActivity.Active,Is.Null,"cold-load QA requires an idle copied-save scene");
    CatPawReachCatalog.EditorClearResourceCacheForQa();
    var rows=new List<string>{"sample,elapsedMs,loading"};
    double maximum=0;var total=System.Diagnostics.Stopwatch.StartNew();var step=System.Diagnostics.Stopwatch.StartNew();
    CatPawReachCatalog.Preload(breedId);step.Stop();maximum=step.Elapsed.TotalMilliseconds;
    rows.Add(System.FormattableString.Invariant($"request,{step.Elapsed.TotalMilliseconds:F3},true"));
    int frame=0;
    while(true)
    {
        step.Restart();yield return null;
        bool loading=CatPawReachCatalog.IsLoadingFor(breedId);step.Stop();
        maximum=System.Math.Max(maximum,step.Elapsed.TotalMilliseconds);
        rows.Add(System.FormattableString.Invariant($"frame{frame++},{step.Elapsed.TotalMilliseconds:F3},{loading}"));
        System.IO.File.WriteAllLines(Root+"/paw-catalog-cold-load.csv",rows);
        Assert.That(total.Elapsed.TotalSeconds,Is.LessThan(10),"small-shard async load should finish promptly");
        if(!loading)break;
    }
    total.Stop();rows.Add(System.FormattableString.Invariant($"complete,{total.Elapsed.TotalMilliseconds:F3},false"));
    System.IO.File.WriteAllLines(Root+"/paw-catalog-cold-load.csv",rows);
    var catalog=CatPawReachCatalog.Load();Assert.That(catalog,Is.Not.Null);Assert.That(catalog.Count,Is.EqualTo(100));
    var body=catalog.Find(breedId,CatActivityPose.Scratch);Assert.That(body,Is.Not.Null);
    Assert.That(body.leftPaw,Is.Empty);Assert.That(body.rightPaw,Is.Empty);
    Assert.That(body.samples.All(p=>p.leftPaw.Length==0&&p.rightPaw.Length==0),Is.True,"legacy pose shards must not duplicate dense skin");
    var surface=CatPawReachCatalog.LoadSurface(breedId,CatActivityPose.Scratch,out bool pending);
    Assert.That(pending,Is.False);Assert.That(surface,Is.Not.Null);Assert.That(surface.samples.Length,Is.EqualTo(CatPawReachCatalog.SurfaceSampleCount));
    Assert.That(surface.leftPaw.Length,Is.GreaterThan(100));
    string[] paths={"Assets/Resources/"+CatPawReachCatalog.ResourceName+".asset",
        "Assets/Resources/"+CatPawReachCatalog.BodyResourceName(breedId)+".asset",
        "Assets/Resources/"+CatPawReachCatalog.SurfaceResourceName(breedId,CatActivityPose.Scratch)+".asset"};
    // Full weighted arm data adds mixed elbow/torso skin and 33 phases;
    // each measured shard is 2.97-3.26 MB, still separate from other breeds.
    // The real 50 ms cold-frame requirement below is unchanged.
    foreach(var path in paths)Assert.That(new System.IO.FileInfo(path).Length,Is.LessThan(4000000),path+" exceeds the measured full-limb shard budget");
    Assert.That(maximum,Is.LessThan(50),"measure the async load frames too; do not hide a long deserialization behind UI");
}

}
