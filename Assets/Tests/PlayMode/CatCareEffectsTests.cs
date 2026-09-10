using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>Native frames exercise the actual room cat, breed replacement and overlay lifecycle.</summary>
public sealed class CatCareEffectsTests
{
    private bool restoreMotion;
    private string restoreBreed;
    private float restoreTimeScale;
    private CatMovement cat;
    private CatSleepZzzEffect sleep;
    private PetHeartEffect hearts;
    private CatCareFxCanvas fx;
    private Scene miniProbe;
    private GameObject inputOwner;

    [SetUp]
    public void Before()
    {
        Assert.That(EditorQaSession.IsActive, Is.True, "Run these native checks in the isolated QA save session.");
        restoreMotion = CatRunnerProgressService.ReducedMotion;
        restoreBreed = CatBreedService.SelectedBreedId;
        restoreTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        CatRunnerProgressService.SetReducedMotion(false);
    }

    [UnityTearDown]
    public IEnumerator After()
    {
        Time.timeScale = restoreTimeScale;
        if (sleep != null) sleep.Stop();
        if (hearts != null) hearts.enabled = false;
        if (cat != null)
        {
            cat.gameObject.SetActive(true);
            cat.ReleaseInputBlock(inputOwner);
        }
        if (miniProbe.IsValid() && miniProbe.isLoaded)
            yield return SceneManager.UnloadSceneAsync(miniProbe);
        CatRunnerProgressService.SetReducedMotion(restoreMotion);
        CatBreedService.Select(restoreBreed);
        RoomPlayModeSupport.ReleaseRoom();
        if(inputOwner!=null)Object.Destroy(inputOwner);
    }

    private IEnumerator Prepare()
    {
        yield return RoomPlayModeSupport.LoadRoomAlone("LivingRoom_Level01");
        cat = Object.FindAnyObjectByType<CatMovement>();
        Assert.That(cat, Is.Not.Null);
        CatActionState.CancelForTransition(cat);
        inputOwner=new GameObject("Care effects input owner");
        cat.AcquireInputBlock(inputOwner);
        sleep = cat.GetComponent<CatSleepZzzEffect>() ?? cat.gameObject.AddComponent<CatSleepZzzEffect>();
        hearts = cat.GetComponent<PetHeartEffect>() ?? cat.gameObject.AddComponent<PetHeartEffect>();
        hearts.enabled = true;
        sleep.Begin();
        hearts.Play();
        yield return null;
        yield return null;
        fx = cat.GetComponent<CatCareFxCanvas>();
        Assert.That(fx, Is.Not.Null);
        Assert.That(fx.TryHeadPosition(out _), Is.True, "The authored cat head must project inside the actual room camera.");
    }

    private CatCareFxGraphic Badge => fx.Overlay.GetComponentsInChildren<CatCareFxGraphic>(true)
        .Single(g => g.name == "SleepMedallion");

    [UnityTest]
    public IEnumerator SharedOverlay_FreezesWithPause_AndRetainsDistinctStopSemantics()
    {
        yield return Prepare();
        yield return new WaitForSeconds(.6f);
        Canvas overlay = fx.Overlay;
        Assert.That(overlay.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
        Assert.That(overlay.sortingOrder, Is.EqualTo(48));
        Assert.That(overlay.gameObject.scene, Is.EqualTo(cat.gameObject.scene));
        Assert.That(overlay.GetComponent<CanvasScaler>().referenceResolution, Is.EqualTo(new Vector2(1920, 1080)));
        Assert.That(overlay.GetComponentsInChildren<GraphicRaycaster>(true), Is.Empty);
        Assert.That(overlay.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
        CatCareFxGraphic[] graphics = overlay.GetComponentsInChildren<CatCareFxGraphic>(true);
        Assert.That(graphics.Length, Is.EqualTo(5), "Both effects share one fixed five-item vector pool.");
        Assert.That(graphics.All(g => !g.raycastTarget), Is.True);
        Assert.That(cat.GetComponentsInChildren<MeshFilter>(true).Any(m => m.name.StartsWith("PetHeart_")), Is.False);
        Assert.That(hearts.ActiveHeartCount, Is.InRange(1, 4));
        Time.timeScale = 0f;
        yield return null;
        Vector2[] positions = graphics.Select(g => g.rectTransform.anchoredPosition).ToArray();
        float[] alpha = graphics.Select(g => g.canvasRenderer.GetAlpha()).ToArray();
        bool[] active = graphics.Select(g => g.gameObject.activeSelf).ToArray();
        float elapsed = sleep.Elapsed;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.That(sleep.Elapsed, Is.EqualTo(elapsed));
        for (int i = 0; i < graphics.Length; i++)
        {
            Assert.That(graphics[i].gameObject.activeSelf, Is.EqualTo(active[i]));
            Assert.That(Vector2.Distance(graphics[i].rectTransform.anchoredPosition, positions[i]), Is.LessThan(.01f));
            Assert.That(graphics[i].canvasRenderer.GetAlpha(), Is.EqualTo(alpha[i]).Within(.001f));
        }
        Time.timeScale = 1f;
        sleep.Stop();
        hearts.Stop();
        Assert.That(Badge.gameObject.activeSelf, Is.False, "Wake-up removes its badge synchronously.");
        Assert.That(hearts.ActiveHeartCount, Is.GreaterThan(0), "Ending petting preserves the final gentle fade.");
        yield return new WaitForSeconds(PetHeartEffect.PresentationLifetime + .1f);
        Assert.That(hearts.ActiveHeartCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ReducedMotion_UsesOneStationaryHeart_AndTracksTheCameraViewport()
    {
        yield return Prepare();
        CatRunnerProgressService.SetReducedMotion(true);
        yield return null;
        yield return new WaitForEndOfFrame();
        CatCareFxGraphic badge = Badge;
        Vector2 badgeOffset = default;
        float deadline = Time.time + 2f;
        bool sampled = false;
        while (Time.time < deadline)
        {
            Assert.That(fx.QuietMotion, Is.True);
            Assert.That(hearts.ActiveHeartCount, Is.LessThanOrEqualTo(1));
            Assert.That(fx.TryHeadPosition(out Vector2 origin), Is.True);
            if (!sampled) { badgeOffset = badge.rectTransform.anchoredPosition - origin; sampled = true; }
            Assert.That(Vector2.Distance(badge.rectTransform.anchoredPosition - origin, badgeOffset), Is.LessThan(.2f));
            foreach (var heart in fx.Overlay.GetComponentsInChildren<CatCareFxGraphic>())
            {
                if (!heart.name.StartsWith("PetHeart_")) continue;
                Assert.That(Vector2.Distance(heart.rectTransform.anchoredPosition - origin, new Vector2(0, 44)), Is.LessThan(.2f));
                Assert.That(heart.rectTransform.localScale, Is.EqualTo(Vector3.one));
            }
            yield return new WaitForEndOfFrame();
        }
        Camera camera = Camera.main;
        Rect previous = camera.rect;
        try
        {
            camera.rect = new Rect(.15f, .12f, .7f, .78f);
            Canvas.ForceUpdateCanvases();
            Assert.That(fx.TryHeadPosition(out Vector2 actual), Is.True);
            Vector3 screen = camera.WorldToScreenPoint(fx.Head.position + Vector3.up * .06f);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)fx.Overlay.transform, screen, null, out Vector2 expected);
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(.01f), "Overlay coordinates follow the camera viewport and CanvasScaler.");
        }
        finally { camera.rect = previous; }
    }

    [UnityTest]
    public IEnumerator EveryBreed_RebindsBothEffectsToTheCurrentHead_WithoutGrowingThePool()
    {
        yield return Prepare();
        Canvas original = fx.Overlay;
        CatBreedCatalog catalog = CatBreedCatalog.Load();
        for (int i = 0; i < catalog.Count; i++)
        {
            Assert.That(CatBreedService.Select(catalog.Get(i).Id), Is.True);
            yield return null;
            yield return null;
            Assert.That(fx.TryHeadPosition(out _), Is.True, catalog.Get(i).Id);
            Assert.That(fx.Head.IsChildOf(cat.transform), Is.True);
            Assert.That(fx.Head, Is.EqualTo(CatBreedVisualFactory.FindDescendant(cat.transform, "DEF-spine.006")));
            Assert.That(fx.Overlay, Is.SameAs(original));
            Assert.That(original.transform.childCount, Is.EqualTo(5));
            Assert.That(Badge.gameObject.activeSelf, Is.True);
        }
    }

    [UnityTest]
    public IEnumerator HiddenCatAndMiniGame_SuppressEffects_AndRoomUnloadDestroysOverlay()
    {
        yield return Prepare();
        Canvas original = fx.Overlay;
        var renderer = cat.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.That(renderer, Is.Not.Null);
        bool rendererEnabled = renderer.enabled;
        try
        {
            renderer.enabled = false;
            yield return null;
            yield return null;
            Assert.That(Badge.gameObject.activeSelf, Is.False);
            Assert.That(hearts.ActiveHeartCount, Is.Zero);
        }
        finally { if (renderer != null) renderer.enabled = rendererEnabled; }
        Assert.That(SceneManager.GetSceneByName(CatRunnerLauncher.RunnerSceneName).isLoaded, Is.False);
        // The visibility boundary is the loaded scene, independent of an arena's gameplay behaviour.
        miniProbe = SceneManager.CreateScene(CatRunnerLauncher.RunnerSceneName);
        yield return null;
        yield return null;
        Assert.That(Badge.gameObject.activeSelf, Is.False);
        Assert.That(hearts.ActiveHeartCount, Is.Zero);
        yield return SceneManager.UnloadSceneAsync(miniProbe);
        cat.gameObject.SetActive(false);
        Assert.That(original.gameObject.activeInHierarchy, Is.False);
        Assert.That(sleep.IsPlaying, Is.False);
        Assert.That(hearts.IsPlaying, Is.False);
        cat.gameObject.SetActive(true);
        sleep.Begin();
        hearts.Play();
        yield return null;
        Assert.That(Badge.gameObject.activeSelf, Is.True);
        yield return RoomPlayModeSupport.LoadRoomAlone("Garden_Level01");
        yield return null;
        Assert.That(original == null, Is.True, "A room-owned overlay must not survive as a floating orphan.");
    }
}
