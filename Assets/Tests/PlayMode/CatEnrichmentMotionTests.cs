using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class CatEnrichmentMotionTests
{
    HomeStoreSaveState store;
    float scale, capture;
    bool initialized;
    string breed;
    readonly System.Reflection.FieldInfo saveInitialized = typeof(CatHomeSaveSystem).GetField("initialized",
        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Isolated QA required.");
        store = HomeStoreService.CaptureState(); scale = Time.timeScale; capture = Time.captureDeltaTime; breed = CatBreedService.SelectedBreedId;
        initialized = (bool)saveInitialized.GetValue(null); saveInitialized.SetValue(null, false);
        Time.timeScale = 1; Time.captureFramerate = 60;
    }
    [TearDown] public void After()
    {
        if (CatActivity.Active != null) CatActivity.Active.CancelForTransition();
        HomeStoreService.ApplySavedState(store); RoomPlayModeSupport.ReleaseRoom();
        CatBreedService.Select(breed);
        Time.timeScale = scale; Time.captureDeltaTime = capture; saveInitialized.SetValue(null, initialized);
    }

    [UnityTest] public IEnumerator AllFifteenProducts_TurnSmoothly_AndDoNotWalkWithoutTravel()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        saveInitialized.SetValue(null, false);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p => CatCollectionPolicy.IsCatItem(p.Id) ||
            HomeRoomService.Rooms.Any(r => HomeStoreService.IsProductInRoomCollection(r.Id, p.Id))).Select(p => p.Id).ToArray();
        var ids = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        state.storedProductIds = ids; HomeStoreService.ApplySavedState(state);
        yield return null; yield return null;
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        var cc = cat.GetComponent<CharacterController>();
        var products = Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include);
        Assert.That(products.Length, Is.EqualTo(15));
        var failures = new List<string>();
        int events = 0; CatActivity tracked = null;
        System.Action<CatActivity> completed = a => { if (a == tracked) events++; };
        CatActivity.Completed += completed;
        try
        {
            foreach (var a in products)
            {
                foreach (string id in ids) HomeStoreService.TrySetStored(id, true);
                Assert.That(HomeStoreService.TrySetStored(a.StoreProductId, false), Is.True, a.StoreProductId);
                yield return null; yield return null;
                RoomPlayModeSupport.ProvisionNeeds();
                cc.enabled = false; var entry = a.RoutineEntryPoint.position; entry.y = 0;
                cat.transform.SetPositionAndRotation(entry, Quaternion.Euler(0, 180, 0));
                cc.enabled = true; Physics.SyncTransforms();
                tracked = a; events = 0;
                var previous = cat.transform.position; var rotation = cat.transform.rotation;
                Assert.That(a.TryStart(cat), Is.True, a.StoreProductId);
                var animation = cat.GetComponent<CatActivityAnimation>();
                float deadline = Time.realtimeSinceStartup + 45, stillWalk = 0, maxStillWalk = 0, turnSpeed = 0;
                int samples = 0;
                while (a.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForEndOfFrame();
                    if (!a.IsRunning) break;
                    float moved = Vector3.Distance(previous, cat.transform.position);
                    float turned = Quaternion.Angle(rotation, cat.transform.rotation);
                    if (Time.deltaTime > 0) turnSpeed = Mathf.Max(turnSpeed, turned / Time.deltaTime);
                    stillWalk = animation.CurrentPose == CatActivityPose.Walk && moved < .001f ? stillWalk + Time.deltaTime : 0;
                    maxStillWalk = Mathf.Max(maxStillWalk, stillWalk);
                    previous = cat.transform.position; rotation = cat.transform.rotation; samples++;
                    if (a.IsWaitingForRestStop && a.RestingSeconds >= .7f) a.RequestRestStop();
                }
                if (a.IsRunning || events != 1) failures.Add(a.StoreProductId + ": completion " + events);
                if (turnSpeed > 960) failures.Add(a.StoreProductId + ": abrupt turn " + turnSpeed);
                if (maxStillWalk > .10f) failures.Add(a.StoreProductId + ": walking in place " + maxStillWalk);
                Assert.That(samples, Is.GreaterThan(20), a.StoreProductId);
                if (a.IsRunning) a.CancelForTransition();
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(cc.enabled && !cat.IsMovementPhysicallyLocked, Is.True, a.StoreProductId);
                Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True, a.StoreProductId);
            }
        }
        finally { CatActivity.Completed -= completed; }
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest] public IEnumerator BasketInTheFurnishedFiveItemGroup_FinishesEveryAcceptedNearbyGame()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        saveInitialized.SetValue(null, false);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p => CatCollectionPolicy.IsCatItem(p.Id) ||
            HomeRoomService.Rooms.Any(r => HomeStoreService.IsProductInRoomCollection(r.Id, p.Id))).Select(p => p.Id).ToArray();
        state.storedProductIds = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        HomeStoreService.ApplySavedState(state);
        foreach (string id in new[] { HomeStoreService.BallBasketId, HomeStoreService.ScratchPostId,
            HomeStoreService.PlayTunnelId, HomeStoreService.BellCollarId, HomeStoreService.FeatherToyId })
            Assert.That(HomeStoreService.TrySetStored(id, false), Is.True, id);
        yield return null; yield return null;
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        var cc = cat.GetComponent<CharacterController>();
        var basket = Object.FindAnyObjectByType<BallChaseActivity>();
        var center = basket.transform.position;
        var candidates = new List<Vector3>();
        for (int x = -5; x <= 5; x++) for (int z = -5; z <= 5; z++)
            candidates.Add(new Vector3(center.x + x * .20f, 0, center.z + z * .20f));
        int accepted = 0, completions = 0;
        System.Action<CatActivity> complete = a => { if (a == basket) completions++; };
        CatActivity.Completed += complete;
        try
        {
            foreach (var candidate in candidates)
            {
                if (!CatActivityMotion.IsControllerFloorClear(cat, candidate)) continue;
                cc.enabled = false; cat.transform.SetPositionAndRotation(candidate, Quaternion.Euler(0, 180, 0));
                cc.enabled = true; Physics.SyncTransforms(); RoomPlayModeSupport.ProvisionNeeds();
                if (!basket.TryGetPromptDistance(cat, out _) || !basket.TryStart(cat)) continue;
                accepted++; var rotation = cat.transform.rotation;
                float deadline = Time.realtimeSinceStartup + 20, maxTurnSpeed = 0; int samples = 0;
                while (basket.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    yield return new WaitForEndOfFrame();
                    if (samples++ > 0 && Time.deltaTime > 0)
                        maxTurnSpeed = Mathf.Max(maxTurnSpeed, Quaternion.Angle(rotation, cat.transform.rotation) / Time.deltaTime);
                    rotation = cat.transform.rotation;
                }
                Assert.That(basket.IsRunning, Is.False, candidate.ToString());
                Assert.That(basket.CatchCount, Is.EqualTo(basket.CatchGoal), basket.InterruptedReason + " from " + candidate);
                Assert.That(completions, Is.EqualTo(accepted), "Complete exactly once.");
                Assert.That(maxTurnSpeed, Is.LessThanOrEqualTo(960f), "No single-frame facing jump.");
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                Assert.That(cc.enabled && !cat.IsMovementPhysicallyLocked, Is.True);
                if (accepted == 6) break;
            }
        }
        finally { CatActivity.Completed -= complete; }
        Assert.That(accepted, Is.EqualTo(6), "Six selectable starts must support the full game in the furnished room.");
    }

    [UnityTest] public IEnumerator NapBeds_AllTenBreeds_RestFacingCameraOnTheirRealSupport()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        saveInitialized.SetValue(null, false);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p => CatCollectionPolicy.IsCatItem(p.Id) ||
            HomeRoomService.Rooms.Any(r => HomeStoreService.IsProductInRoomCollection(r.Id, p.Id))).Select(p => p.Id).ToArray();
        var ids = state.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
        state.storedProductIds = ids; HomeStoreService.ApplySavedState(state);
        yield return null; yield return null;
        var cat = Object.FindAnyObjectByType<CatMovement>();
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        var cc = cat.GetComponent<CharacterController>();
        var beds = Object.FindObjectsByType<CatEnrichmentActivity>(FindObjectsInactive.Include)
            .Where(a => a.Mode == CatEnrichmentMode.Nap).ToArray();
        Assert.That(beds.Length, Is.EqualTo(4));
        var mesh = new Mesh();
        var report = new System.Text.StringBuilder("product,breed,samples,minDot,minSupportY,bodyX,bodyZ,supportX,supportZ\n");
        int completions = 0, runs = 0;
        System.Action<CatActivity> completed = a => { if (beds.Contains(a)) completions++; };
        CatActivity.Completed += completed;
        try
        {
            foreach (var bed in beds)
            {
                foreach (string id in ids) HomeStoreService.TrySetStored(id, true);
                Assert.That(HomeStoreService.TrySetStored(bed.StoreProductId, false), Is.True);
                yield return null; yield return null;
                foreach (var entry in CatBreedCatalog.Load().Entries)
                {
                    CatBreedService.Select(entry.Id); yield return null; yield return null;
                    var originalScale = cat.transform.localScale;
                    RoomPlayModeSupport.ProvisionNeeds(); cc.enabled = false;
                    var start = bed.RoutineEntryPoint.position; start.y = 0;
                    cat.transform.SetPositionAndRotation(start, Quaternion.identity); cc.enabled = true; Physics.SyncTransforms();
                    var hips = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine");
                    var shoulders = CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.003");
                    var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
                    Assert.That(bed.TryStart(cat), Is.True, bed.StoreProductId + "/" + entry.Id); runs++;
                    float deadline = Time.realtimeSinceStartup + 20, minDot = 1; int samples = 0;
                    Bounds bounds = default;
                    while (bed.IsRunning && Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForEndOfFrame();
                        if (!bed.IsWaitingForRestStop || bed.RestingSeconds < .3f) continue;
                        float dot = CatActivityFacing.FacingDot(shoulders.position - hips.position,
                            (shoulders.position + hips.position) * .5f, CatActivityFacing.CameraPosition(cat));
                        minDot = Mathf.Min(minDot, dot);
                        Assert.That(dot, Is.GreaterThanOrEqualTo(-.01f), bed.StoreProductId + "/" + entry.Id + " resting torso");
                        if (samples++ == 0)
                        {
                            skin.BakeMesh(mesh, true); var vertices = mesh.vertices;
                            var mask = entry.ContactVertexIndices;
                            var local = bed.ContactPoint.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                            bounds = new Bounds(local.MultiplyPoint3x4(vertices[mask[0]]), Vector3.zero);
                            foreach (int index in mask) bounds.Encapsulate(local.MultiplyPoint3x4(vertices[index]));
                            Assert.That(bounds.min.y, Is.InRange(-.01f, .025f), "Real support height preserved.");
                        }
                        if (bed.RestingSeconds >= .7f) bed.RequestRestStop();
                    }
                    Assert.That(samples, Is.GreaterThan(0)); Assert.That(bed.IsRunning, Is.False);
                    Assert.That(completions, Is.EqualTo(runs)); Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
                    yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                    Assert.That(cc.enabled && !cat.IsMovementPhysicallyLocked, Is.True);
                    Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True);
                    var surface = bed.ContactPoint.GetComponent<CatActivitySurface>();
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    report.AppendLine(string.Join(",", bed.StoreProductId, entry.Id, samples, minDot.ToString(inv),
                        bounds.min.y.ToString(inv), bounds.size.x.ToString(inv), bounds.size.z.ToString(inv),
                        surface.Size.x.ToString(inv), surface.Size.y.ToString(inv)));
                }
            }
        }
        finally
        {
            CatActivity.Completed -= completed; Object.Destroy(mesh);
            string path = System.IO.Path.Combine(EditorQaSession.SaveDirectory, "nap-facing.csv");
            System.IO.File.WriteAllText(path, report.ToString());
        }
    }
}
