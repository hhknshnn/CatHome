using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>All 80 purchased products, with the complete room present, on all ten breeds.</summary>
public sealed class RoomFurnitureBreedMatrixTests
{
    private string originalBreed;
    private HomeStoreSaveState originalStore;
    private readonly List<string> failures = new List<string>();

    [SetUp] public void SetUp()
    {
        originalBreed = CatBreedService.SelectedBreedId;
        originalStore = HomeStoreService.CaptureState();
        failures.Clear();
    }

    [TearDown] public void TearDown()
    {
        Time.timeScale = 1f;
        if (CatActivity.Active != null) CatActivity.Active.enabled = false;
        RoomPlayModeSupport.ReleaseRoom();
        CatBreedService.Select(originalBreed);
        HomeStoreService.ApplySavedState(originalStore);
    }

    [UnityTest] public IEnumerator LivingRoom_AllBreeds() => RunRoom("LivingRoom_Level01", HomeRoomService.LivingRoomId);
    [UnityTest] public IEnumerator Bathroom_AllBreeds() => RunRoom("Bathroom_Level01", HomeRoomService.BathroomId);
    [UnityTest] public IEnumerator Kitchen_AllBreeds() => RunRoom("Kitchen_Level01", HomeRoomService.KitchenId);
    [UnityTest] public IEnumerator Bedroom_AllBreeds() => RunRoom("Bedroom_Level01", HomeRoomService.BedroomId);
    [UnityTest] public IEnumerator Garden_AllBreeds() => RunRoom("Garden_Level01", HomeRoomService.GardenId);
    [UnityTest] public IEnumerator Balcony_AllBreeds() => RunRoom("Balcony_Level01", HomeRoomService.BalconyId);
    [UnityTest] public IEnumerator Patio_AllBreeds() => RunRoom("Patio_Level01", HomeRoomService.PatioId);
    [UnityTest] public IEnumerator SecondFloor_AllBreeds() => RunRoom("SecondFloor_Level01", HomeRoomService.SecondFloorId);

    [UnityTest]
    public IEnumerator SwapDuringSwing_AndCancel_RestoresContactAndControls()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Garden_Level01");
        typeof(CatHomeSaveSystem).GetField("initialized", System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.NonPublic).SetValue(null, false);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = new[] { HomeStoreService.GardenHammockId };
        HomeStoreService.ApplySavedState(state);
        var cat = Object.FindAnyObjectByType<CatMovement>();
        SwingRideActivity ride = null;
        foreach (var candidate in Object.FindObjectsByType<SwingRideActivity>(FindObjectsSortMode.None))
            if (candidate.Kind == CatActivityKind.HammockSway) ride = candidate;
        Assert.That(ride, Is.Not.Null);
        Assert.That(ride.TryStart(cat), Is.True);
        Time.timeScale = 4f;
        float deadline = Time.realtimeSinceStartup + 10f;
        while (cat.GetComponent<CatActivityAnimation>().ContactSurface == null && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(cat.GetComponent<CatActivityAnimation>().ContactSurface, Is.Not.Null);
        CatBreedService.Select("maine-coon");
        yield return null; yield return null;
        Assert.That(cat.GetComponentsInChildren<Animator>().Length, Is.EqualTo(1));
        Assert.That(cat.GetComponent<CatActivityAnimation>().IsActive, Is.True);
        ride.enabled = false;
        yield return null;
        Assert.That(CatActivity.Active, Is.Null);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(CatActivityMotion.IsFloorClear(cat.transform.position, .24f), Is.True);
        Assert.That(Quaternion.Angle(ride.SwingPivot.localRotation, Quaternion.identity), Is.LessThan(.1f));
    }

    private IEnumerator RunRoom(string sceneName, string roomId)
    {
        yield return RoomPlayModeSupport.LoadRoomAlone(sceneName);
        // This isolated scene tour must never persist its temporary ownership.
        typeof(CatHomeSaveSystem).GetField("initialized", System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.NonPublic).SetValue(null, false);
        var products = new List<CatActivity>();
        var owned = new List<string>();
        foreach (var display in Object.FindObjectsByType<StoreProductDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!HomeStoreService.IsProductInRoomCollection(roomId, display.ProductId)) continue;
            var activity = display.GetComponent<CatActivity>();
            Assert.That(activity, Is.Not.Null, display.ProductId + " must have its own routine.");
            products.Add(activity); owned.Add(display.ProductId);
        }
        Assert.That(products.Count, Is.EqualTo(10), roomId);
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = owned.ToArray();
        HomeStoreService.ApplySavedState(state);
        yield return null;
        var cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(cat, Is.Not.Null);
        var controller = cat.GetComponent<CharacterController>();
        var parent = cat.transform.parent;
        Vector3 scale = cat.transform.localScale;
        Vector3 spawn = cat.transform.position;
        var breeds = CatBreedCatalog.Load();
        Assert.That(breeds.Count, Is.EqualTo(10));
        Directory.CreateDirectory("Temp/FixedRoomAudit/Matrix");
        var report = new System.Text.StringBuilder("product,breed,started,finished,poses,boneMotion,exitClear\n");
        for (int breed = 0; breed < breeds.Count; breed++)
        {
            string breedId = breeds.Get(breed).Id;
            Assert.That(CatBreedService.Select(breedId), Is.True);
            yield return null; yield return null;
            var animator = cat.GetComponentInChildren<Animator>();
            Transform probe = null;
            foreach (var bone in cat.GetComponentsInChildren<Transform>()) if (bone.name == "DEF-spine.003") probe = bone;
            Assert.That(probe, Is.Not.Null, breedId);
            foreach (var activity in products)
            {
                RoomPlayModeSupport.ProvisionNeeds();
                controller.enabled = false;
                var anchor = typeof(CatActivity).GetField("interactionAnchor", System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).GetValue(activity) as Transform;
                cat.transform.position = anchor.position;
                CatActivity nearest = null;
                float nearestDistance = float.PositiveInfinity;
                foreach (var candidate in CatActivity.Registered)
                {
                    if (!candidate.IsUnlocked || !candidate.isActiveAndEnabled) continue;
                    float distance = candidate.DistanceTo(cat);
                    if (distance < nearestDistance) { nearestDistance = distance; nearest = candidate; }
                }
                if (nearest != activity) failures.Add(activity.Kind + " is shadowed by another action prompt");
                cat.transform.SetPositionAndRotation(spawn, Quaternion.identity);
                controller.enabled = true;
                Physics.SyncTransforms();
                string label = roomId + "/" + activity.Kind + "/" + breedId;
                bool started = activity.TryStart(cat);
                if (!started) { failures.Add(label + " refused start"); continue; }
                Time.timeScale = 12f;
                float deadline = Time.realtimeSinceStartup + 12f;
                var poses = new HashSet<CatActivityPose>();
                bool productResponded = false;
                var previous = probe.localRotation;
                float boneMotion = 0f;
                bool captured = false;
                bool contactChecked = false;
                float poseTime = 0f;
                CatActivityPose lastPose = CatActivityPose.Walk;
                while (activity.IsRunning && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                    var animation = cat.GetComponent<CatActivityAnimation>();
                    productResponded |= activity.GetComponent<RoomProductFeedback>().IsResponding;
                    if (!activity.IsRunning) break;
                    poses.Add(animation.CurrentPose);
                    if (animation.CurrentPose == lastPose) poseTime += Time.deltaTime;
                    else { lastPose = animation.CurrentPose; poseTime = 0f; }
                    if (lastPose != CatActivityPose.Walk)
                        boneMotion = Mathf.Max(boneMotion, Quaternion.Angle(previous, probe.localRotation));
                    previous = probe.localRotation;
                    if (!contactChecked && poseTime > .5f && animation.ContactSurface != null &&
                        lastPose != CatActivityPose.Walk && lastPose != CatActivityPose.Hop)
                    {
                        yield return new WaitForEndOfFrame();
                        if (activity.IsRunning && animation.ContactSurface != null)
                        {
                            float gap = VisibleBodyContactGap(cat, animation.ContactSurface);
                            if (gap < -.005f || gap > .02f)
                                failures.Add(label + " visible body misses its support by " + gap.ToString("F4") + " m");
                            contactChecked = true;
                        }
                    }
                    if (!captured && breedId.Contains("maine") && poseTime > .5f &&
                        lastPose != CatActivityPose.Walk && lastPose != CatActivityPose.Hop)
                    {
                        yield return new WaitForEndOfFrame();
                        Capture(activity, cat, roomId);
                        captured = true;
                    }
                }
                bool finished = !activity.IsRunning;
                if (!finished) { failures.Add(label + " timed out"); activity.enabled = false; activity.enabled = true; }
                yield return RoomPlayModeSupport.WaitForMovementRelease(cat);
                bool exitClear = CatActivityMotion.IsFloorClear(cat.transform.position, .24f);
                if (!productResponded) failures.Add(label + " product did not respond visually");
                if (!exitClear) failures.Add(label + " ended inside a collider at " + cat.transform.position.ToString("F3"));
                if (!CatActivityMotion.TryFloorPath(cat.transform.position, spawn, out _)) failures.Add(label + " ended in an isolated floor pocket");
                if (cat.IsMovementPhysicallyLocked || !controller.enabled) failures.Add(label + " left controls locked");
                if (cat.transform.parent != parent || cat.transform.localScale != scale) failures.Add(label + " changed hierarchy/scale");
                if (poses.Count < 2 || boneMotion < .01f) failures.Add(label + " did not animate a furniture pose");
                report.AppendLine(activity.StoreProductId + "," + breedId + "," + started + "," + finished + "," +
                    string.Join("|", poses) + "," + boneMotion.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "," + exitClear);
            }
        }
        Time.timeScale = 1f;
        File.WriteAllText("Temp/FixedRoomAudit/Matrix/" + roomId + ".csv", report.ToString());
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    // Bake with the renderer scale so these positions match the visible skin,
    // then measure against the furniture plane after animation's LateUpdate.
    private static float VisibleBodyContactGap(CatMovement cat, Transform surface)
    {
        var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        var mesh = new Mesh();
        try
        {
            skin.BakeMesh(mesh, true);
            var vertices = mesh.vertices;
            float gap = float.PositiveInfinity;
            foreach (int index in CatBreedService.SelectedEntry.ContactVertexIndices)
                gap = Mathf.Min(gap, Vector3.Dot(skin.transform.TransformPoint(vertices[index]) -
                    surface.position, surface.up));
            return gap;
        }
        finally { Object.Destroy(mesh); }
    }

    private static void Capture(CatActivity activity, CatMovement cat, string roomId)
    {
        var host = new GameObject("Furniture Matrix Camera");
        var camera = host.AddComponent<Camera>();
        Vector3 center = activity.transform.position;
        center.y = Mathf.Max(.45f, cat.transform.position.y * .6f);
        Bounds framing = new Bounds(cat.transform.position + Vector3.up * .25f, Vector3.one * .5f);
        foreach (var renderer in activity.GetComponentsInChildren<Renderer>())
            framing.Encapsulate(renderer.bounds);
        center = framing.center;
        Vector3 front = activity.RoutineEntryPoint.position - activity.transform.position;
        front.y = 0f;
        if (front.sqrMagnitude < .01f) front = Vector3.back;
        camera.transform.position = center + front.normalized * Mathf.Max(2.7f, framing.size.magnitude * 1.3f) + Vector3.up * 1.3f;
        camera.transform.LookAt(center);
        camera.fieldOfView = 47f;
        var target = new RenderTexture(768, 640, 24);
        var previous = RenderTexture.active;
        var texture = new Texture2D(768, 640, TextureFormat.RGB24, false);
        // Keep the actual room's lighting/materials, but remove foreground
        // occlusion from adjacent products and the shell for contact inspection.
        var hidden = new List<Renderer>();
        foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (renderer.transform.IsChildOf(activity.transform) || renderer.transform.IsChildOf(cat.transform) ||
                renderer.bounds.max.y < .04f || renderer.forceRenderingOff) continue;
            renderer.forceRenderingOff = true; hidden.Add(renderer);
        }
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 768, 640), 0, 0); texture.Apply();
            File.WriteAllBytes("Temp/FixedRoomAudit/Matrix/" + activity.StoreProductId + ".png", texture.EncodeToPNG());
            var animation = cat.GetComponent<CatActivityAnimation>();
            File.WriteAllText("Temp/FixedRoomAudit/Matrix/" + activity.StoreProductId + "-contact.txt",
                "cat=" + cat.transform.position + " scale=" + cat.transform.lossyScale +
                " pose=" + animation.CurrentPose + " surface=" +
                (animation.ContactSurface == null ? "none" : animation.ContactSurface.position.ToString()));
        }
        finally
        {
            foreach (var renderer in hidden) if (renderer != null) renderer.forceRenderingOff = false;
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            Object.Destroy(texture); Object.Destroy(target); Object.Destroy(host);
        }
    }
}
