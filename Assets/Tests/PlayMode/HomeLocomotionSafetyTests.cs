using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class HomeLocomotionSafetyTests
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    CatMovement cat;
    CharacterController controller;
    MobileJoystick joystick;
    HungerSystem hunger;
    ThirstSystem thirst;
    EnergySystem energy;
    GameObject needsHost, joystickHost, wall, lockOwner;
    string savedBreed;
    float savedTimeScale, savedCaptureDelta;
    bool hadOnboarding;
    int savedOnboarding;

    [SetUp] public void Before()
    {
        savedBreed = CatBreedService.SelectedBreedId;
        savedTimeScale = Time.timeScale; savedCaptureDelta = Time.captureDeltaTime;
        hadOnboarding = PlayerPrefs.HasKey(PetTutorialHint.OnboardingCompletedKey);
        savedOnboarding = PlayerPrefs.GetInt(PetTutorialHint.OnboardingCompletedKey);
        PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, 1);
        Time.timeScale = 1f; Time.captureFramerate = 60;
    }

    [TearDown] public void After()
    {
        if (cat != null && lockOwner != null) cat.SetMovementLocked(lockOwner, false);
        if (joystickHost != null) Object.DestroyImmediate(joystickHost);
        if (needsHost != null) Object.DestroyImmediate(needsHost);
        if (wall != null) Object.DestroyImmediate(wall);
        if (lockOwner != null) Object.DestroyImmediate(lockOwner);
        CatBreedService.Select(savedBreed);
        RoomPlayModeSupport.ReleaseRoom();
        if (hadOnboarding) PlayerPrefs.SetInt(PetTutorialHint.OnboardingCompletedKey, savedOnboarding);
        else PlayerPrefs.DeleteKey(PetTutorialHint.OnboardingCompletedKey);
        Time.timeScale = savedTimeScale; Time.captureDeltaTime = savedCaptureDelta;
    }

    static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    void Input(Vector2 value) => typeof(MobileJoystick).GetProperty("Direction").SetValue(joystick, value);
    Animator Animator => cat.GetComponentInChildren<Animator>();

    IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        cat = Object.FindAnyObjectByType<CatMovement>();
        controller = cat.GetComponent<CharacterController>();
        var idle = cat.GetComponent<CatIdleBehavior>(); if (idle != null) idle.enabled = false;
        CatActionState.CancelForTransition(cat);
        needsHost = new GameObject("Locomotion needs");
        hunger = needsHost.AddComponent<HungerSystem>(); thirst = needsHost.AddComponent<ThirstSystem>();
        energy = RoomPlayModeSupport.ProvisionNeeds();
        Set(hunger, "catMovement", cat); Set(thirst, "catMovement", cat); Set(energy, "catMovement", cat);
        Set(cat, "energySystem", energy);
        joystickHost = new GameObject("Locomotion input", typeof(RectTransform));
        joystick = joystickHost.AddComponent<MobileJoystick>();
        Set(cat, "mobileJoystick", joystick); Set(cat, "cameraTransform", null);
        yield return null;
        yield return QaBreedReadiness.WaitForSelected(cat);
        Assert.That(cat.IsMovementLocked, Is.False, "Fixture must have no world/menu/activity input owner.");
    }

    void Place(Vector3 point)
    {
        Input(Vector2.zero);
        controller.enabled = false;
        cat.transform.SetPositionAndRotation(point, Quaternion.identity);
        controller.enabled = true;
        Set(cat, "verticalVelocity", 0f); Set(cat, "running", false);
        Physics.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator TenBreeds_ActualNeedsSpeedAndAnimatorCyclesStayTogether()
    {
        yield return Prepare();
        var catalog = Resources.Load<CatHomeLocomotionCatalog>(CatHomeLocomotionCatalog.ResourceName);
        Assert.That(catalog, Is.Not.Null);
        var report = new StringBuilder("breed,needs,ground_mps,cycle_mps,blend,playback_rate\n");
        string folder = Path.GetFullPath("Docs/QA/HOME_LOCOMOTION_2026-09-09"); Directory.CreateDirectory(folder);
        foreach (var breed in CatBreedCatalog.Load().Entries)
        {
            Assert.That(CatBreedService.Select(breed.Id), Is.True);
            yield return null; yield return null;
            yield return QaBreedReadiness.WaitForSelected(cat, breed.Id);
            var animator = Animator;
            var profile = catalog.Find(breed.Id);
            foreach (string needs in new[] { "full", "hunger0-water34-energy99", "thirst0", "energy0" })
            {
                hunger.ApplySavedValue(needs == "hunger0-water34-energy99" ? 0f : 100f);
                thirst.ApplySavedValue(needs == "thirst0" ? 0f : needs == "hunger0-water34-energy99" ? 34f : 100f);
                energy.ApplySavedValue(needs == "energy0" ? 0f : 99f);
                Place(new Vector3(0f, .05f, -1.8f));
                yield return null; yield return null;
                Input(Vector2.up);
                // Stabilise after the breed bind and first contact with the floor.
                for (int i = 0; i < 4; i++) yield return new WaitForEndOfFrame();
                Vector3 before = cat.transform.position;
                float phase = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                float elapsed = 0f;
                for (int i = 0; i < 32; i++)
                {
                    yield return new WaitForEndOfFrame(); elapsed += Time.deltaTime;
                }
                Vector3 travel = cat.transform.position - before; travel.y = 0f;
                float speed = travel.magnitude / elapsed;
                float blend = CatHomeLocomotionCatalog.RunBlendForSpeed(speed);
                float cycles = animator.GetCurrentAnimatorStateInfo(0).normalizedTime - phase;
                float cycleSpeed = cycles * profile.CycleDistance(blend, cat.transform.lossyScale.z) / elapsed;
                float rate = animator.GetFloat("LocomotionRate");
                string context = breed.Id + " / " + needs;
                Assert.That(speed, Is.GreaterThan(.2f), context + " fixture path must be open.");
                Assert.That(cycleSpeed, Is.EqualTo(speed).Within(speed * .06f), context + " feet/body distance diverged.");
                if (needs == "full")
                {
                    Assert.That(speed, Is.EqualTo(CatMovement.HomeRunSpeed).Within(.04f), context);
                    Assert.That(animator.GetFloat("Speed"), Is.EqualTo(1f).Within(.01f));
                }
                else
                {
                    Assert.That(speed, Is.LessThan(1.05f), context);
                    Assert.That(animator.GetFloat("Speed"), Is.EqualTo(.35f).Within(.01f), context + " slow needs should walk.");
                    Assert.That(cat.IsRunning, Is.False, context);
                }
                report.AppendLine(string.Join(",", breed.Id, needs, F(speed), F(cycleSpeed), F(blend), F(rate)));
                File.WriteAllText(Path.Combine(folder, "live-needs-cadence.csv"), report.ToString());
                Input(Vector2.zero); yield return new WaitForEndOfFrame();
                Assert.That(animator.GetFloat("Speed"), Is.Zero, context + " release must have no damping tail.");
            }
        }
    }

    [UnityTest]
    public IEnumerator ReverseAndWallStopStepping_WhilePhysicalOwnerKeepsItsPose()
    {
        yield return Prepare();
        hunger.ApplySavedValue(100f); thirst.ApplySavedValue(100f); energy.ApplySavedValue(99f);
        Place(new Vector3(0f, .05f, -1.8f)); yield return null;
        var animator = Animator;
        Vector3 before = cat.transform.position;
        Input(Vector2.down); yield return null; yield return new WaitForEndOfFrame();
        Vector3 drift = cat.transform.position - before; drift.y = 0f;
        Assert.That(drift.magnitude, Is.LessThan(.005f), "A reverse input should pivot before taking a forward step.");
        Assert.That(animator.GetFloat("Speed"), Is.GreaterThan(0f), "The short reverse pivot must animate the existing gait instead of sliding in Idle.");

        Place(new Vector3(0f, .05f, -1.8f));
        wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Locomotion test wall";
        wall.transform.position = new Vector3(0f, .5f, -1f); wall.transform.localScale = new Vector3(2f, 1f, .1f);
        Physics.SyncTransforms(); Input(Vector2.up);
        for (int i = 0; i < 55; i++) yield return new WaitForEndOfFrame();
        Assert.That(cat.GroundSpeed, Is.LessThan(.025f));
        Assert.That(animator.GetFloat("Speed"), Is.Zero, "Blocked cat must stop stepping immediately.");

        lockOwner = new GameObject("Care animation owner"); cat.SetMovementLocked(lockOwner, true);
        animator.Play("Sleep", 0, .2f); animator.speed = .7f;
        for (int i = 0; i < 4; i++) yield return new WaitForEndOfFrame();
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Sleep"), Is.True);
        Assert.That(animator.speed, Is.EqualTo(.7f), "Home cadence must never use the shared Animator.speed override.");
        animator.speed = 1f; animator.Play("Idle");
        cat.SetMovementLocked(lockOwner, false);
    }
    static string F(float value) => value.ToString("F6", CultureInfo.InvariantCulture);
}
