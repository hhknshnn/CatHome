using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

/// <summary>Water and soap must follow the real shower routine, including interrupted exits.</summary>
public sealed class BathroomShowerPolishTests
{
    private HomeStoreSaveState restoreStore;
    private bool restoreMotion;
    private float restoreTimeScale;
    private int restoreCaptureFramerate;
    private ShowerRinseActivity shower;
    private CatMovement cat;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Use the isolated QA save session for native room tests.");
        restoreStore = HomeStoreService.CaptureState();
        restoreMotion = CatRunnerProgressService.ReducedMotion;
        restoreTimeScale = Time.timeScale;
        restoreCaptureFramerate = Time.captureFramerate;
        // Screenshot encoding must not consume the rinse when the paused
        // pixel proof resumes. Keep the simulated frame length deterministic.
        Time.captureFramerate = 60;
        Time.timeScale = 1f;
        CatRunnerProgressService.SetReducedMotion(false);
    }

    [TearDown]
    public void After()
    {
        Time.timeScale = restoreTimeScale;
        Time.captureFramerate = restoreCaptureFramerate;
        if (shower != null)
        {
            shower.CancelForTransition();
            shower.enabled = true;
        }
        RoomPlayModeSupport.ReleaseRoom();
        HomeStoreService.ApplySavedState(restoreStore);
        CatRunnerProgressService.SetReducedMotion(restoreMotion);
    }

    private IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("Bathroom_Level01");
        var state = HomeStoreSaveState.CreateDefault();
        state.ownedProductIds = new[] { HomeStoreService.BathroomShowerId };
        HomeStoreService.ApplySavedState(state);
        yield return null;
        // A preceding purchase test may leave its level celebration above the
        // shower. Close that fixture overlay before comparing real foam pixels.
        var celebration = Object.FindAnyObjectByType<HomeLevelUpCelebrationView>();
        if (celebration != null && HomeLevelUpCelebrationView.IsAnyOpen)
        {
            typeof(HomeLevelUpCelebrationView).GetMethod("BeginClose", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(celebration, null);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (HomeLevelUpCelebrationView.IsAnyOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(HomeLevelUpCelebrationView.IsAnyOpen, Is.False, "The previous test's celebration must close before shower capture.");
        }
        shower = Object.FindAnyObjectByType<ShowerRinseActivity>();
        cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(shower, Is.Not.Null);
        Assert.That(cat, Is.Not.Null);
        ReadyCat();
    }

    private void ReadyCat()
    {
        CatActionState.CancelForTransition(cat);
        var controller = cat.GetComponent<CharacterController>();
        controller.enabled = false;
        Vector3 point = shower.RoutineEntryPoint.position;
        point.y = .05f;
        cat.transform.SetPositionAndRotation(point, Quaternion.identity);
        controller.enabled = true;
        Physics.SyncTransforms();
        RoomPlayModeSupport.ProvisionNeeds().ApplySavedValue(70f);
    }

    private IEnumerator WaitForPhase(CatShowerWaterFx.Phase phase)
    {
        float deadline = Time.realtimeSinceStartup + 12f;
        while (Time.realtimeSinceStartup < deadline)
        {
            var fx = shower.GetComponent<CatShowerWaterFx>();
            if (fx != null && fx.CurrentPhase == phase) yield break;
            yield return null;
        }
        Assert.Fail("The real shower never reached " + phase + ".");
    }

    private static Mesh EffectMesh(CatShowerWaterFx fx) => fx.VisualRoot.GetComponent<MeshFilter>().sharedMesh;

    [UnityTest]
    public IEnumerator WaterAndSoap_FollowRinseThenScatter_AndFinishBeforeWalkingOut()
    {
        yield return Prepare();
        Vector3 originalScale = cat.transform.localScale;
        Assert.That(shower.TryStart(cat), Is.True);
        var fx = shower.GetComponent<CatShowerWaterFx>();
        Assert.That(fx, Is.Not.Null);
        Assert.That(fx.CurrentPhase, Is.EqualTo(CatShowerWaterFx.Phase.Off), "Walking into the shower must not start falling water.");
        yield return WaitForPhase(CatShowerWaterFx.Phase.Rinsing);
        Assert.That(fx.WaterCount, Is.EqualTo(18));
        Assert.That(fx.VisualRoot.gameObject.scene, Is.EqualTo(shower.gameObject.scene));
        Assert.That(fx.VisualRoot.GetComponentsInChildren<Collider>(true), Is.Empty);
        Assert.That(fx.VisualRoot.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(1));
        Assert.That(fx.VisualRoot.GetComponent<MeshRenderer>().shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
        Mesh mesh = EffectMesh(fx);
        yield return new WaitForSeconds(.65f);
        Assert.That(fx.FoamCount, Is.EqualTo(12));
        Assert.That(mesh.vertexCount, Is.LessThan(1024));
        Assert.That(mesh.colors32.Any(c => c.a > 0 && c.a < 255), Is.True, "Water and foam need soft transparent edges.");
        Assert.That(fx.CurrentPhase, Is.EqualTo(CatShowerWaterFx.Phase.Rinsing));
        float rinseElapsed = fx.Elapsed;
        Vector3[] falling = mesh.vertices;
        yield return new WaitForSeconds(.12f);
        Assert.That(fx.CurrentPhase, Is.EqualTo(CatShowerWaterFx.Phase.Rinsing), "Water movement must be compared within the rinse phase.");
        Assert.That(fx.Elapsed, Is.GreaterThan(rinseElapsed), "The rinse must advance before its water geometry is compared.");
        Assert.That(EffectMesh(fx), Is.SameAs(mesh), "Animation updates the pooled mesh instead of creating a new one.");
        Assert.That(falling.SequenceEqual(mesh.vertices), Is.False, "The active rinse visibly moves its water.");
        yield return AssertBodyFoamIsVisible(fx);
        yield return WaitForPhase(CatShowerWaterFx.Phase.ShakeOff);
        Assert.That(fx.WaterCount, Is.Zero, "Water stops at the beginning of shake-off.");
        Assert.That(fx.FoamCount, Is.GreaterThan(0));
        Vector3[] attached = mesh.vertices;
        yield return new WaitForSeconds(.16f);
        Assert.That(fx.CurrentPhase, Is.EqualTo(CatShowerWaterFx.Phase.ShakeOff));
        Assert.That(attached.SequenceEqual(mesh.vertices), Is.False, "Detached foam visibly disperses during the shake.");
        yield return WaitForPhase(CatShowerWaterFx.Phase.Off);
        Assert.That(fx.VisualRoot.gameObject.activeSelf, Is.False);
        Assert.That(fx.WaterCount + fx.FoamCount, Is.Zero);
        float deadline = Time.realtimeSinceStartup + 5f;
        while (shower.IsRunning && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(shower.IsRunning, Is.False);
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(cat.transform.localScale, Is.EqualTo(originalScale));
    }

    [UnityTest]
    public IEnumerator ReducedMotionAndPause_KeepTheSmallPoolStillAndBounded()
    {
        yield return Prepare();
        CatRunnerProgressService.SetReducedMotion(true);
        Assert.That(shower.TryStart(cat), Is.True);
        yield return WaitForPhase(CatShowerWaterFx.Phase.Rinsing);
        var fx = shower.GetComponent<CatShowerWaterFx>();
        yield return new WaitForSeconds(.65f);
        Assert.That(fx.QuietMotion, Is.True);
        Assert.That(fx.WaterCount, Is.EqualTo(6));
        Assert.That(fx.FoamCount, Is.EqualTo(6));
        Time.timeScale = 0f;
        yield return null;
        float elapsed = fx.Elapsed;
        Mesh mesh = EffectMesh(fx);
        Vector3[] positions = mesh.vertices;
        Color32[] colors = mesh.colors32;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(fx.Elapsed, Is.EqualTo(elapsed));
        Assert.That(positions.SequenceEqual(mesh.vertices), Is.True, "Pause freezes water and foam geometry.");
        Assert.That(colors.SequenceEqual(mesh.colors32), Is.True, "Pause freezes opacity as well as position.");
        Time.timeScale = 1f;
        yield return WaitForPhase(CatShowerWaterFx.Phase.ShakeOff);
        Assert.That(fx.WaterCount, Is.Zero);
        Assert.That(fx.FoamCount, Is.LessThanOrEqualTo(6));
        yield return WaitForPhase(CatShowerWaterFx.Phase.Off);
        Assert.That(fx.VisualRoot.gameObject.activeSelf, Is.False);
    }

    [UnityTest]
    public IEnumerator CancelDisableAndUnload_ClearSoapWaterAndOwnedResources()
    {
        yield return Prepare();
        Assert.That(shower.TryStart(cat), Is.True);
        yield return WaitForPhase(CatShowerWaterFx.Phase.Rinsing);
        var fx = shower.GetComponent<CatShowerWaterFx>();
        yield return new WaitForSeconds(.45f);
        Mesh mesh = EffectMesh(fx);
        Mesh bodySample = ReadField<Mesh>(fx, "bodySampleMesh");
        Assert.That(bodySample, Is.Not.Null);
        Material material = fx.VisualRoot.GetComponent<MeshRenderer>().sharedMaterial;
        Transform visual = fx.VisualRoot;
        shower.CancelForTransition();
        AssertStopped(fx);
        ReadyCat();
        Assert.That(shower.TryStart(cat), Is.True);
        yield return WaitForPhase(CatShowerWaterFx.Phase.ShakeOff);
        Assert.That(fx.VisualRoot, Is.SameAs(visual));
        Assert.That(EffectMesh(fx), Is.SameAs(mesh));
        Assert.That(ReadField<Mesh>(fx, "bodySampleMesh"), Is.SameAs(bodySample));
        shower.enabled = false;
        AssertStopped(fx);
        shower.enabled = true;
        ReadyCat();
        Assert.That(shower.TryStart(cat), Is.True);
        yield return WaitForPhase(CatShowerWaterFx.Phase.Rinsing);
        yield return RoomPlayModeSupport.LoadRoomAlone("Garden_Level01");
        yield return null;
        Assert.That(visual == null, Is.True, "The room unload owns the effect hierarchy.");
        Assert.That(mesh == null, Is.True, "The runtime mesh must be disposed on unload.");
        Assert.That(bodySample == null, Is.True, "The reused skinned-surface sample must also be disposed on unload.");
        Assert.That(material == null, Is.True, "The runtime material must be disposed on unload.");
        Assert.That(Object.FindObjectsByType<CatShowerWaterFx>(FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty,
            "A bathroom effect must not follow the cat into another room.");
    }

    private void AssertStopped(CatShowerWaterFx fx)
    {
        Assert.That(fx.CurrentPhase, Is.EqualTo(CatShowerWaterFx.Phase.Off));
        Assert.That(fx.WaterCount + fx.FoamCount, Is.Zero);
        Assert.That(fx.VisualRoot.gameObject.activeSelf, Is.False);
        Assert.That(cat.GetComponent<CharacterController>().enabled, Is.True);
        Assert.That(cat.IsMovementPhysicallyLocked, Is.False);
    }

    private static T ReadField<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

    private static IEnumerator AssertBodyFoamIsVisible(CatShowerWaterFx fx)
    {
        float previousTimeScale = Time.timeScale;
        var renderer = fx.VisualRoot.GetComponent<MeshRenderer>();
        bool wasEnabled = renderer.enabled;
        Texture2D withFoam = null, withoutFoam = null;
        try
        {
            Time.timeScale = 0f;
            yield return new WaitForEndOfFrame();
            Vector3[] bodyCenters = ReadField<Vector3[]>(fx, "foamCenters").Take(6).ToArray();
            withFoam = ScreenCapture.CaptureScreenshotAsTexture();
            renderer.enabled = false;
            yield return new WaitForEndOfFrame();
            withoutFoam = ScreenCapture.CaptureScreenshotAsTexture();
            Assert.That(withFoam.width, Is.EqualTo(withoutFoam.width));
            Assert.That(withFoam.height, Is.EqualTo(withoutFoam.height));
            Color32[] on = withFoam.GetPixels32(), off = withoutFoam.GetPixels32();
            Camera camera = Camera.main;
            int visiblePatches = 0;
            foreach (Vector3 center in bodyCenters)
            {
                Vector3 screen = camera.WorldToScreenPoint(center);
                int x = Mathf.RoundToInt(screen.x * withFoam.width / Screen.width);
                int y = Mathf.RoundToInt(screen.y * withFoam.height / Screen.height);
                int changed = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int px = x + dx, py = y + dy;
                        if (px < 0 || py < 0 || px >= withFoam.width || py >= withFoam.height) continue;
                        int pixel = py * withFoam.width + px;
                        int difference = Mathf.Abs(on[pixel].r - off[pixel].r) +
                                         Mathf.Abs(on[pixel].g - off[pixel].g) + Mathf.Abs(on[pixel].b - off[pixel].b);
                        if (difference > 45) changed++;
                    }
                if (changed >= 4) visiblePatches++;
            }
            System.IO.Directory.CreateDirectory("Library/BathroomFoamProof");
            System.IO.File.WriteAllBytes("Library/BathroomFoamProof/foam-on.png", withFoam.EncodeToPNG());
            System.IO.File.WriteAllBytes("Library/BathroomFoamProof/foam-off.png", withoutFoam.EncodeToPNG());
            Assert.That(visiblePatches, Is.GreaterThanOrEqualTo(4),
                "At least four attached soap patches must visibly change pixels on the actual cat; counters and hidden mesh vertices are insufficient.");
        }
        finally
        {
            renderer.enabled = wasEnabled;
            Time.timeScale = previousTimeScale;
            if (withFoam != null) Object.Destroy(withFoam);
            if (withoutFoam != null) Object.Destroy(withoutFoam);
        }
    }
}
