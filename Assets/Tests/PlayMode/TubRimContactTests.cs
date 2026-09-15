using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TubRimContactTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedTime, savedCapture;
    CatMovement cat;
    TubEdgeWalkActivity tub;
    int completions;
    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedTime = Time.timeScale; savedCapture = Time.captureDeltaTime;
        Time.timeScale = 1f; Time.captureFramerate = 60; CatActivity.Completed += Completed;
    }
    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        CatActivity.Completed -= Completed; RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(savedStore); CatBreedService.Select(savedBreed);
        Time.timeScale = savedTime; Time.captureDeltaTime = savedCapture;
    }
    void Completed(CatActivity a) { if (a == tub) completions++; }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        var state = HomeStoreSaveState.CreateDefault(); state.ownedProductIds = HomeStoreService.BathroomCollection.ToArray();
        HomeStoreService.ApplySavedState(state); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>(); CatActionState.CancelForTransition(cat);
        cat.GetComponent<CatIdleBehavior>().enabled = false;
        tub = CatActivity.Registered.OfType<TubEdgeWalkActivity>().Single();
    }
    void Start()
    {
        RoomPlayModeSupport.ProvisionNeeds(); completions = 0;
        Assert.That(tub.TryStart(cat), Is.True);
    }
    [UnityTest] public IEnumerator TenBreeds_PawsFollowTheActualMesh_WithoutStretching_ThenExitOnce()
    {
        yield return Prepare();
        var mesh = tub.GetComponentInChildren<MeshCollider>();
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            CatBreedService.Select(breed.Id); yield return null; yield return null;
            var feet = cat.GetComponentsInChildren<Transform>().Where(t => t.name == "DEF-hand.L" || t.name == "DEF-hand.R" || t.name == "DEF-foot.L" || t.name == "DEF-foot.R").ToArray();
            var bones = cat.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("DEF-forearm.") || t.name.StartsWith("DEF-hand.") || t.name.StartsWith("DEF-shin.") || t.name.StartsWith("DEF-foot.")).ToArray();
            var local = bones.Select(t => t.localPosition).ToArray();
            Vector3 scale = cat.transform.localScale; int samples = 0;
            var skin = cat.GetComponentInChildren<SkinnedMeshRenderer>(); var posed = new Mesh();
            Start(); float deadline = Time.realtimeSinceStartup + 22f;
            while (tub.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                if (!tub.IsRimWalking) continue;
                var motion = cat.GetComponent<CatTubRimMotion>();
                Assert.That(motion.SurfaceSampleCount, Is.GreaterThan(10));
                if (motion.MaximumContactError >= .015f)
                {
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                    var limbs = (System.Collections.IEnumerable)typeof(CatTubRimMotion).GetField("legs", flags).GetValue(motion);
                    var details = new System.Text.StringBuilder(breed.Id + " root=" + cat.transform.position.ToString("F5") + " time=" + motion.BalanceSeconds);
                    foreach (var limb in limbs)
                    {
                        var type = limb.GetType(); var upper = (Transform)type.GetField("upper", flags).GetValue(limb);
                        var lower = (Transform)type.GetField("lower", flags).GetValue(limb); var foot = (Transform)type.GetField("foot", flags).GetValue(limb);
                        var target = (Vector3)type.GetField("target", flags).GetValue(limb);
                        details.AppendLine("\n" + foot.name + " upper=" + upper.position.ToString("F5") + " foot=" + foot.position.ToString("F5") + " target=" + target.ToString("F5") + " span=" + (Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, foot.position)));
                    }
                    string output = UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Temp/TubContact");
                    System.IO.Directory.CreateDirectory(output);
                    System.IO.File.WriteAllText(output + "/contact-failure.txt", details.ToString());
                    var picture = ScreenCapture.CaptureScreenshotAsTexture();
                    System.IO.File.WriteAllBytes(output + "/contact-failure.png", picture.EncodeToPNG()); Object.Destroy(picture);
                }
                Assert.That(motion.MaximumContactError, Is.LessThan(.015f), breed.Id + " reachable paws");
                Assert.That(cat.transform.localScale, Is.EqualTo(scale));
                for (int i = 0; i < bones.Length; i++)
                    // Imported gait keys include sub-millimetre joint translation.
                    Assert.That(Vector3.Distance(bones[i].localPosition, local[i]), Is.LessThan(.002f), breed.Id + " bone length " + bones[i].name);
                foreach (var foot in feet)
                {
                    RaycastHit hit;
                    Assert.That(mesh.Raycast(new Ray(foot.position + Vector3.up * .2f, Vector3.down), out hit, .5f), Is.True, breed.Id + " paw outside tub " + foot.name);
                    Assert.That(hit.normal.y, Is.GreaterThan(.78f), breed.Id + " needs actual rim top " + foot.name);
                    Assert.That(foot.position.y - hit.point.y, Is.InRange(.001f, .23f), breed.Id + " ankle clearance " + foot.name);
                }
                skin.BakeMesh(posed, true); var soles = feet.Select(f => f.position.y).ToArray();
                foreach (var vertex in posed.vertices)
                {
                    Vector3 p = skin.transform.TransformPoint(vertex); int nearest = -1; float best = .085f * .085f;
                    for (int i = 0; i < feet.Length; i++)
                    {
                        float square = (p - feet[i].position).sqrMagnitude;
                        if (square < best) { best = square; nearest = i; }
                    }
                    if (nearest >= 0) soles[nearest] = Mathf.Min(soles[nearest], p.y);
                }
                float lowest = float.PositiveInfinity;
                for (int i = 0; i < feet.Length; i++)
                {
                    RaycastHit hit; mesh.Raycast(new Ray(feet[i].position + Vector3.up * .2f, Vector3.down), out hit, .5f);
                    float gap = soles[i] - hit.point.y; lowest = Mathf.Min(lowest, gap);
                    Assert.That(gap, Is.InRange(-.018f, .16f), breed.Id + " actual paw sole " + feet[i].name);
                }
                Assert.That(lowest, Is.LessThan(.020f), breed.Id + " actual planted sole cannot float");
                samples++;
            }
            Assert.That(samples, Is.GreaterThan(30), breed.Id);
            Assert.That(tub.IsRunning, Is.False); Assert.That(completions, Is.EqualTo(1));
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True);
            Assert.That(cat.GetComponent<CatTubRimMotion>().IsActive, Is.False);
            Object.Destroy(posed);
        }
    }
    [UnityTest] public IEnumerator RimBalance_PausesAndCancels_WithoutReleasingAnotherOwner()
    {
        yield return Prepare(); Start();
        float deadline = Time.realtimeSinceStartup + 15f;
        while (!tub.IsRimWalking && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(tub.IsRimWalking, Is.True);
        Time.timeScale = 0f; yield return new WaitForEndOfFrame(); yield return new WaitForEndOfFrame();
        var feet = cat.GetComponentsInChildren<Transform>().Where(t => t.name == "DEF-hand.L" || t.name == "DEF-foot.R").ToArray();
        var positions = feet.Select(t => t.position).ToArray(); var root = cat.transform.position;
        for (int i = 0; i < 8; i++) yield return new WaitForEndOfFrame();
        Assert.That(cat.transform.position, Is.EqualTo(root));
        for (int i = 0; i < feet.Length; i++) Assert.That(Vector3.Distance(feet[i].position, positions[i]), Is.LessThan(.0001f));
        var other = new GameObject("Tub cancel owner");
        try
        {
            cat.AcquireInputBlock(other); tub.CancelForTransition();
            Assert.That(cat.HasScopedInputBlock, Is.True); Assert.That(completions, Is.Zero);
            Assert.That(cat.GetComponent<CatTubRimMotion>().IsActive, Is.False);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True);
        }
        finally { cat.ReleaseInputBlock(other); Object.Destroy(other); Time.timeScale = 1f; }
    }
}
