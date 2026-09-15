using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GroomBrushCadenceTests
{
    HomeStoreSaveState savedStore;
    string savedBreed;
    float savedScale, savedCapture;
    CatMovement cat;
    GroomBrushActivity brush;
    GameObject otherOwner;
    static string Evidence => UnityEditor.SessionState.GetString("CatHome.QA.ResultDirectory", "Temp/PawGroomEvidence");

    [SetUp] public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True);
        savedStore = HomeStoreService.CaptureState(); savedBreed = CatBreedService.SelectedBreedId;
        savedScale = Time.timeScale; savedCapture = Time.captureDeltaTime;
        Time.timeScale = 1; Time.captureFramerate = 60;
    }
    [TearDown] public void After()
    {
        if (cat != null) CatActionState.CancelForTransition(cat);
        if (otherOwner != null) Object.DestroyImmediate(otherOwner);
        RoomPlayModeSupport.ReleaseRoom(); HomeStoreService.ApplySavedState(savedStore);
        CatBreedService.Select(savedBreed); Time.timeScale = savedScale; Time.captureDeltaTime = savedCapture;
    }
    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = HomeStoreService.Products.Where(p => HomeStoreService.IsProductInRoomCollection(HomeRoomService.BathroomId, p.Id)).Select(p => p.Id).ToArray();
        HomeStoreService.ApplySavedState(state); yield return null; yield return null;
        cat = Object.FindAnyObjectByType<CatMovement>(); brush = Object.FindAnyObjectByType<GroomBrushActivity>();
        CatActionState.CancelForTransition(cat);
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
    }
    void Start()
    {
        RoomPlayModeSupport.ProvisionNeeds();
        var cc = cat.GetComponent<CharacterController>(); cc.enabled = false;
        Vector3 entry = brush.RoutineEntryPoint.position; entry.y = .05f;
        cat.transform.SetPositionAndRotation(entry, Quaternion.Euler(0, 180, 0));
        cc.enabled = true; Physics.SyncTransforms();
        Assert.That(brush.TryGetPromptDistance(cat, out _), Is.True);
        Assert.That(brush.TryStart(cat), Is.True);
    }
    static Vector3 Flat(Vector3 p) { p.y = 0; return p; }

    [UnityTest] public IEnumerator TenBreeds_OriginalPawGroomStaysOnNearbyFloor()
    {
        yield return Prepare();
        var rows = new List<string> { "breed,samples,maxRootDrift,pawMotion" };
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            Assert.That(CatBreedService.Select(breed.Id), Is.True); yield return null; yield return null;
            Start(); var animator = cat.GetComponentInChildren<Animator>();
            Vector3 origin = cat.transform.position; float drift = 0, pawMotion = 0;
            var paws = cat.GetComponentsInChildren<Transform>().Where(t =>
                t.name == "DEF-hand.L" || t.name == "DEF-hand.R" ||
                t.name == "DEF-foot.L" || t.name == "DEF-foot.R").ToArray();
            Assert.That(paws.Length, Is.EqualTo(4));
            Vector3[] first = null; int samples = 0, groomingFrames = 0;
            float deadline = Time.realtimeSinceStartup + 20;
            while (brush.IsRunning && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForEndOfFrame();
                drift = Mathf.Max(drift, Flat(cat.transform.position - origin).magnitude);
                Assert.That(cat.GetComponent<CatActivityAnimation>().CurrentPose, Is.Not.EqualTo(CatActivityPose.Walk));
                if (!brush.IsGrooming) continue;
                if (++groomingFrames < 5) continue;
                if (animator.IsInTransition(0)) continue;
                Assert.That(cat.GetComponent<CatActivityAnimation>().CurrentPose, Is.EqualTo(CatActivityPose.Groom));
                Assert.That(animator.GetFloat("Speed"), Is.Zero);
                Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(c => c.clip.name.EndsWith("|Itching")), Is.True,
                    "Use the breed's original paw-groom clip: " + string.Join(",", animator.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name)));
                var points = paws.Select(p => cat.transform.InverseTransformPoint(p.position)).ToArray();
                if (first == null) first = points;
                for (int i = 0; i < points.Length; i++) pawMotion = Mathf.Max(pawMotion, Vector3.Distance(points[i], first[i]));
                samples++;
            }
            Assert.That(brush.IsRunning, Is.False);
            Assert.That(samples, Is.GreaterThan(200));
            Assert.That(drift, Is.LessThan(.001f), "No approach loop, rubbing pass, or return trip.");
            Assert.That(pawMotion, Is.GreaterThan(.025f), "The original clip must visibly move a paw.");
            Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True);
            Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
            rows.Add(string.Join(",", breed.Id, samples, drift.ToString("F5", CultureInfo.InvariantCulture), pawMotion.ToString("F5", CultureInfo.InvariantCulture)));
            Directory.CreateDirectory(Evidence); File.WriteAllLines(Evidence + "/paw-groom.csv", rows);
        }
    }

    [UnityTest] public IEnumerator PauseAndCancel_PreservePositionAndOtherInputOwner()
    {
        yield return Prepare(); Start();
        float deadline = Time.realtimeSinceStartup + 15;
        while (!brush.IsRubbing && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(brush.IsRubbing, Is.True); yield return new WaitForEndOfFrame();
        Time.timeScale = 0; Vector3 position = cat.transform.position;
        var animator = cat.GetComponentInChildren<Animator>();
        float phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        for (int i = 0; i < 5; i++) yield return new WaitForEndOfFrame();
        Assert.That(Vector3.Distance(position, cat.transform.position), Is.LessThan(.00001f));
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(phase).Within(.0001f));
        otherOwner = new GameObject("Groom cancellation input owner"); cat.SetMovementLocked(otherOwner, true);
        brush.CancelForTransition(); Time.timeScale = 1;
        Assert.That(brush.IsRunning, Is.False); Assert.That(cat.IsMovementPhysicallyLocked, Is.True);
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(CatActivityMotion.IsControllerFloorClear(cat, cat.transform.position), Is.True);
        cat.SetMovementLocked(otherOwner, false); yield return null;
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
        Assert.That(animator.GetFloat("LocomotionRate"), Is.EqualTo(1).Within(.001f));
    }
}
