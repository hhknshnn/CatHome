using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnvironmentCollisionPolishTests
{
    const string Root = "Docs/QA/INTERACTION_POLISH_2026-09-16";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    HomeStoreSaveState saved;
    string breed;
    float capture;
    GameObject inputHost;
    MobileJoystick joystick;
    Mesh baked;
    GameObject testSolid;
    GameObject testCorner;
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<string> rows = new List<string>();
    readonly List<string> failures = new List<string>();

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        saved = HomeStoreService.CaptureState(); breed = CatBreedService.SelectedBreedId;
        capture = Time.captureDeltaTime; Time.captureFramerate = 30;
        baked = new Mesh();
        rows.Clear(); failures.Clear();
        rows.Add("room,target,breed,fps,approach,travel,bodyPenetration,rootGap");
    }
    [TearDown] public void After()
    {
        if (inputHost != null) Object.DestroyImmediate(inputHost);
        if (baked != null) Object.DestroyImmediate(baked);
        if (testSolid != null) Object.DestroyImmediate(testSolid);
        if (testCorner != null) Object.DestroyImmediate(testCorner);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(saved);
        CatBreedService.Select(breed); Time.captureDeltaTime = capture;
        System.IO.File.WriteAllLines(Root + "/collision-" + TestContext.CurrentContext.Test.Name + ".csv", rows);
        if (TestContext.CurrentContext.Test.Name.StartsWith("EightRooms")) System.IO.File.WriteAllLines(Root + "/collision-walks.csv", rows);
    }
    void Input(Vector2 value) => typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick, value);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);

    [UnityTest] public IEnumerator CornerEscape_AndFirstInput_DoNotTrapOrShoveTheCat()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var cat = Object.FindAnyObjectByType<CatMovement>(); cat.GetComponent<CatIdleBehavior>().enabled = false;
        yield return QaBreedReadiness.WaitForSelected(cat);
        var cc = cat.GetComponent<CharacterController>();
        inputHost = new GameObject("Corner escape input", typeof(RectTransform)); joystick = inputHost.AddComponent<MobileJoystick>();
        Set(cat, "mobileJoystick", joystick); Set(cat, "cameraTransform", null);
        testSolid = GameObject.CreatePrimitive(PrimitiveType.Cube); testCorner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testSolid.transform.position = new Vector3(-.7f, .5f, .05f); testSolid.transform.localScale = new Vector3(1.5f, 1f, .1f);
        testCorner.transform.position = new Vector3(.05f, .5f, -.7f); testCorner.transform.localScale = new Vector3(.1f, 1f, 1.5f);
        cc.enabled = false; cat.transform.SetPositionAndRotation(new Vector3(-.85f, .05f, -.85f), Quaternion.Euler(0, 45, 0));
        cc.enabled = true; Physics.SyncTransforms(); yield return null;
        Input(Vector2.one.normalized); for (int frame = 0; frame < 50; frame++) yield return new WaitForEndOfFrame();
        Vector3 trapped = cat.transform.position; float depth = 0;
        Input(-Vector2.one.normalized);
        for (int frame = 0; frame < 65; frame++)
        {
            yield return new WaitForEndOfFrame();
            depth = Mathf.Max(depth, BodyDepth(cat, testSolid.GetComponent<Collider>()), BodyDepth(cat, testCorner.GetComponent<Collider>()));
        }
        Input(Vector2.zero);
        Assert.That(Vector3.Distance(cat.transform.position, trapped), Is.GreaterThan(.25f), "Away input must escape a two-wall corner");
        Assert.That(depth, Is.LessThan(.015f), "Turning/backing must keep the rendered body outside both walls");
        Object.DestroyImmediate(testSolid); Object.DestroyImmediate(testCorner); testSolid = testCorner = null;
        var boundary = HomeRoomBoundary.FindFor(cat.gameObject.scene);
        var min = boundary.MinimumXZ; var max = boundary.MaximumXZ; float margin = boundary.EdgeClearance;
        try
        {
            // Mimic a supported activity's release beyond the old circular inset,
            // without an actual physical wall at this artificial test boundary.
            boundary.Configure(new Vector2(-1, -2), new Vector2(1, 2), .05f);
            cc.enabled = false; cat.transform.SetPositionAndRotation(new Vector3(.82f, .05f, -.8f), Quaternion.identity);
            cc.enabled = true; Physics.SyncTransforms(); yield return null;
            Vector3 before = cat.transform.position; Input(Vector2.up); yield return new WaitForEndOfFrame(); Input(Vector2.zero);
            Assert.That(Mathf.Abs(cat.transform.position.x - before.x), Is.LessThan(.002f), "First input must not apply the old absolute boundary correction sideways");
        }
        finally { boundary.Configure(min, max, margin); }
    }

    [UnityTest, Timeout(600000)] public IEnumerator TenBreeds_ThreeFrameRates_WallAndTurningRespectActualBody()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        var cat = Object.FindAnyObjectByType<CatMovement>(); cat.GetComponent<CatIdleBehavior>().enabled = false;
        var cc = cat.GetComponent<CharacterController>();
        inputHost = new GameObject("Collision anatomy input", typeof(RectTransform)); joystick = inputHost.AddComponent<MobileJoystick>();
        Set(cat, "mobileJoystick", joystick); Set(cat, "cameraTransform", null);
        testSolid = GameObject.CreatePrimitive(PrimitiveType.Cube); testSolid.name = "Body clearance calibration wall";
        testSolid.transform.SetPositionAndRotation(new Vector3(0, .5f, -.1f), Quaternion.identity);
        testSolid.transform.localScale = new Vector3(3.2f, 1f, .12f);
        var solid = testSolid.GetComponent<BoxCollider>(); Physics.SyncTransforms();
        foreach (var entry in CatBreedCatalog.Load().Entries)
        {
            Assert.That(CatBreedService.Select(entry.Id), Is.True);
            yield return QaBreedReadiness.WaitForSelected(cat,entry.Id);
            Assert.That(cat.HasBodyGuardProfile, Is.True, entry.Id + " measured body profile is active");
            foreach (int fps in new[] { 15, 30, 60 })
            {
                Time.captureFramerate = fps; Input(Vector2.zero); cc.enabled = false;
                cat.transform.SetPositionAndRotation(new Vector3(0, .05f, -1.3f), Quaternion.identity);
                cc.enabled = true; Set(cat, "verticalVelocity", 0f); Physics.SyncTransforms(); yield return null;
                Input(Vector2.up);
                float maxDepth = 0;
                for (int frame = 0; frame < fps * 2; frame++)
                { yield return new WaitForEndOfFrame(); if (frame % Mathf.Max(1, fps / 15) == 0) maxDepth = Mathf.Max(maxDepth, BodyDepth(cat, solid)); }
                Input(Vector2.zero); for (int f = 0; f < 3; f++) yield return new WaitForEndOfFrame();
                Vector3 stopped = cat.transform.position;
                for (int f = 0; f < fps / 2; f++) yield return new WaitForEndOfFrame();
                Assert.That(Vector3.Distance(cat.transform.position, stopped), Is.LessThan(.001f), entry.Id + " no idle depenetration");
                float gap = Mathf.Abs(cat.transform.position.z - solid.bounds.min.z);
                // Turn next to the wall and leave along its tangent, then reverse while still close.
                Input(Vector2.right);
                for (int frame = 0; frame < fps; frame++)
                { yield return new WaitForEndOfFrame(); if (frame % Mathf.Max(1, fps / 15) == 0) maxDepth = Mathf.Max(maxDepth, BodyDepth(cat, solid)); }
                Input(Vector2.left);
                for (int frame = 0; frame < fps; frame++)
                { yield return new WaitForEndOfFrame(); if (frame % Mathf.Max(1, fps / 15) == 0) maxDepth = Mathf.Max(maxDepth, BodyDepth(cat, solid)); }
                Input(Vector2.zero);
                rows.Add(System.FormattableString.Invariant($"calibration,wall-turn,{entry.Id},{fps},0,0,{maxDepth:F5},{gap:F4}"));
                if (maxDepth > .015f) failures.Add(entry.Id + "/" + fps + " body penetration " + maxDepth.ToString("F4"));
                Assert.That(gap, Is.LessThan(.55f), entry.Id + " must still approach naturally");
            }
        }
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [UnityTest, Timeout(600000)] public IEnumerator EightRooms_RealMovementStopsAtSolidGeometry()
    {
        int checkedRooms = 0, walks = 0;
        foreach (var room in HomeRoomService.Rooms)
        {
            yield return RoomPlayModeSupport.LoadRoomAlone(room.SceneName);
            var owned = HomeStoreSaveState.CreateDefault();
            owned.ownedProductIds = HomeStoreService.Products.Select(p => p.Id).ToArray();
            owned.storedProductIds = owned.ownedProductIds.Where(CatCollectionPolicy.IsCatItem).ToArray();
            HomeStoreService.ApplySavedState(owned); yield return null; yield return null;
            var cat = Object.FindAnyObjectByType<CatMovement>();
            cat.GetComponent<CatIdleBehavior>().enabled = false;
            yield return QaBreedReadiness.WaitForSelected(cat);
            var controller = cat.GetComponent<CharacterController>();
            RoomPlayModeSupport.ProvisionNeeds();
            inputHost = new GameObject("Collision walk input", typeof(RectTransform));
            joystick = inputHost.AddComponent<MobileJoystick>();
            Set(cat, "mobileJoystick", joystick); Set(cat, "cameraTransform", null);
            var all = Object.FindObjectsByType<Collider>()
                .Where(c => c.gameObject.scene == cat.gameObject.scene && c.enabled && !c.isTrigger &&
                    c.GetComponentInParent<CatMovement>() == null && c.bounds.min.y < .75f && c.bounds.max.y > .15f).ToArray();
            var structural = all.Where(c => c.name == "WallBody" || c.name == "RailLow" || c.name == "MidRail" ||
                c.name == "Newel" || c.name.StartsWith("Baluster") && c.transform.parent.name == "Loft Stair Landing" ||
                (c.name == "Pot" && c.transform.parent.name.StartsWith("CornerPot"))).ToArray();
            var shell = all.Where(c => c.GetComponentInParent<HomeProductPlacement>() == null &&
                c.bounds.size.y > .8f && (c.bounds.size.x > 2f || c.bounds.size.z > 2f) &&
                c.bounds.size.y > Mathf.Min(c.bounds.size.x, c.bounds.size.z)).Take(2);
            var furniture = all.Where(c => c.GetComponentInParent<HomeProductPlacement>() != null &&
                c.bounds.size.y > .25f && c.bounds.size.x + c.bounds.size.z > .65f)
                .OrderByDescending(c => c.bounds.size.x * c.bounds.size.z);
            var targets = structural.Concat(shell).Concat(furniture).Distinct().ToArray();
            Assert.That(targets.Length, Is.GreaterThan(0), room.Id + " solid targets");
            int roomWalks = 0;
            foreach (var target in targets)
            {
                bool approached = false;
                // Find a physically reachable side. No teleport inside furniture.
                for (int sample = 0; sample < 24 && !approached; sample++)
                {
                    int angle = sample % 8;
                    Vector3 outward = Quaternion.Euler(0, angle * 45, 0) * Vector3.forward;
                    Vector3 far = target.bounds.center + outward * (target.bounds.extents.magnitude + 1f);
                    // Sample several points along long rails, which may have furniture in front of their centre.
                    float along = sample / 8 == 1 ? -.3f : sample / 8 == 2 ? .3f : 0;
                    if (target.bounds.size.x > target.bounds.size.z) far.x += target.bounds.size.x * along;
                    else far.z += target.bounds.size.z * along;
                    far.y = Mathf.Clamp(.3f, target.bounds.min.y + .005f, target.bounds.max.y - .005f);
                    if (!target.Raycast(new Ray(far, -outward), out var faceHit, target.bounds.extents.magnitude * 2 + 2)) continue;
                    Vector3 face = faceHit.point;
                    Vector3 start = face + outward * .70f; start.y = .05f;
                    var boundary = HomeRoomBoundary.FindFor(cat.gameObject.scene);
                    if (boundary != null && Vector3.Distance(boundary.ClampPosition(start, .35f), start) > .01f) continue;
                    if (!CatActivityMotion.IsControllerFloorClear(cat, start)) continue;
                    controller.enabled = false;
                    cat.transform.SetPositionAndRotation(start, Quaternion.LookRotation(-outward));
                    controller.enabled = true; Set(cat, "verticalVelocity", 0f); Physics.SyncTransforms();
                    yield return null;
                    Assert.That(cat.IsMovementLocked, Is.False, "Walk must use the real movement controller");
                    Input(new Vector2(-outward.x, -outward.z));
                    for (int frame = 0; frame < 40; frame++) yield return new WaitForEndOfFrame();
                    float travel = Vector3.Distance(start, cat.transform.position);
                    Input(Vector2.zero); yield return new WaitForEndOfFrame();
                    float depth = BodyDepth(cat, target);
                    float gap = Vector3.ProjectOnPlane(cat.transform.position - face, Vector3.up).magnitude;
                    rows.Add(System.FormattableString.Invariant($"{room.Id},{target.transform.parent.name}/{target.name},{CatBreedService.SelectedBreedId},30,{angle * 45},{travel:F4},{depth:F5},{gap:F4}"));
                    if (depth > .025f) { failures.Add(room.Id + "/" + target.transform.parent.name + "/" + target.name + " body penetration " + depth.ToString("F4")); Capture(cat, target, room.Id); }
                    // A nearby object may block an approach; record it, but don't claim this target was reached.
                    if (travel > .15f && gap < .65f)
                    {
                        approached = true; roomWalks++; walks++;
                        if (roomWalks == 1) Capture(cat, target, "final-" + room.Id);
                    }
                }
            }
            Assert.That(roomWalks, Is.GreaterThan(0), room.Id + " actual obstacle approach"); checkedRooms++;
            Object.DestroyImmediate(inputHost); inputHost = null;
        }
        Assert.That(checkedRooms, Is.EqualTo(8));
        Assert.That(walks, Is.GreaterThanOrEqualTo(20));
        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    float BodyDepth(CatMovement cat, Collider solid)
    {
        var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        skin.BakeMesh(baked, true); baked.GetVertices(vertices);
        var tag=skin.GetComponentInParent<CatBreedVisualTag>();
        Assert.That(tag,Is.Not.Null);
        Assert.That(tag.BreedId,Is.EqualTo(CatBreedService.SelectedBreedId),"Measure the selected rig after production replacement, never another breed's vertex mask.");
        var entry = CatBreedCatalog.Load().Find(tag.BreedId);
        Assert.That(entry.ContactVertexIndices, Is.Not.Empty);
        float maximum = 0;
        foreach (int index in entry.ContactVertexIndices)
        {
            Vector3 world = skin.transform.TransformPoint(vertices[index]);
            if (!solid.bounds.Contains(world)) continue;
            if (solid is BoxCollider box)
            {
                Vector3 local = box.transform.InverseTransformPoint(world) - box.center;
                Vector3 inside = box.size * .5f - new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
                if (inside.x < 0 || inside.y < 0 || inside.z < 0) continue;
                Vector3 scale = box.transform.lossyScale;
                maximum = Mathf.Max(maximum, Mathf.Min(inside.x * Mathf.Abs(scale.x), Mathf.Min(inside.y * Mathf.Abs(scale.y), inside.z * Mathf.Abs(scale.z))));
            }
            else if (solid is MeshCollider mesh && InsideMesh(mesh, world, out float depth)) maximum = Mathf.Max(maximum, depth);
        }
        return maximum;
    }
    static void Capture(CatMovement cat, Collider target, string room)
    {
        var camera = Camera.main; if (camera == null) return;
        var position = camera.transform.position; var rotation = camera.transform.rotation;
        var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
        var image = new Texture2D(960, 720, TextureFormat.RGB24, false); var render = new RenderTexture(960, 720, 24);
        Vector3 aim = cat.transform.position + Vector3.up * .3f;
        try
        {
            camera.transform.position = aim - cat.transform.forward * 1.3f + cat.transform.right * 1.6f + Vector3.up * .8f;
            camera.transform.LookAt(aim); camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
            image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); image.Apply();
            System.IO.File.WriteAllBytes(Root + "/collision-" + room + "-" + target.name + ".png", image.EncodeToPNG());
        }
        finally { camera.targetTexture = oldTarget; RenderTexture.active = oldActive; camera.transform.SetPositionAndRotation(position, rotation); render.Release(); Object.Destroy(render); Object.Destroy(image); }
    }
    static bool InsideMesh(MeshCollider mesh, Vector3 point, out float depth)
    {
        bool previous = Physics.queriesHitBackfaces; Physics.queriesHitBackfaces = true;
        depth = float.PositiveInfinity; int inside = 0;
        var vertices = mesh.sharedMesh.vertices; var triangles = mesh.sharedMesh.triangles;
        try
        {
            foreach (var axis in new[] { new Vector3(.013f, 1, .027f).normalized, new Vector3(1, .017f, .031f).normalized, new Vector3(.019f, .023f, 1).normalized,
                -new Vector3(.013f, 1, .027f).normalized, -new Vector3(1, .017f, .031f).normalized, -new Vector3(.019f, .023f, 1).normalized })
                if (mesh.Raycast(new Ray(point, axis), out var hit, 5f))
                {
                    int i = hit.triangleIndex * 3;
                    Vector3 normal = Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]], vertices[triangles[i + 2]] - vertices[triangles[i]]);
                    if (Vector3.Dot(mesh.transform.TransformDirection(normal), axis) > 0) { inside++; depth = Mathf.Min(depth, hit.distance); }
                }
            return inside >= 4;
        }
        finally { Physics.queriesHitBackfaces = previous; }
    }
}
