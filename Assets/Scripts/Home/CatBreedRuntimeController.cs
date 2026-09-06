using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Runtime marker carried by the visual root currently installed on a playable cat.</summary>
[DisallowMultipleComponent]
public sealed class CatBreedVisualTag : MonoBehaviour
{
    [SerializeField] private string breedId;
    public string BreedId => breedId;
    public void Configure(string value) => breedId = value;
}

/// <summary>
/// Creates a Cat Home-compatible visual from an untouched vendor prefab. Both
/// the turntable and gameplay use this factory, which guarantees that the model,
/// controller and scale shown in the shop are the ones used in the game.
/// </summary>
public static class CatBreedVisualFactory
{
    // Polyperfect's Generic clips include this model object in every curve path.
    // The other breed prefabs use breed-specific names for the equivalent object,
    // so normalise only the instantiated copy before assigning the shared clips.
    private const string AnimationBindingRootName = "Cat_Domestic_Shorthair";

    public static GameObject Create(
        CatBreedCatalog.Entry entry,
        RuntimeAnimatorController controller,
        Transform parent,
        string rootName = "CatBreedVisual")
    {
        if (entry == null || entry.SourcePrefab == null)
            return null;

        var root = new GameObject(rootName, typeof(CatBreedVisualTag));
        root.transform.SetParent(parent, false);
        root.GetComponent<CatBreedVisualTag>().Configure(entry.Id);

        GameObject visual = Object.Instantiate(entry.SourcePrefab, root.transform, false);
        visual.name = "AnimatedVisual";
        visual.transform.localPosition = new Vector3(0f, .02f, 0f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * CatBreedCatalog.SourceVisualScale;

        Animator animator = visual.GetComponent<Animator>() ??
                            visual.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            NormalizeAnimationBindingRoot(animator);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
        }

        Transform head = FindDescendant(visual.transform, "DEF-spine.006");
        if (head != null && FindDescendant(visual.transform, "HeartSpawn") == null)
        {
            Transform heartSpawn = new GameObject("HeartSpawn").transform;
            heartSpawn.SetParent(head, false);
            heartSpawn.localPosition = new Vector3(0f, .045f, .035f);
        }

        return root;
    }

    private static void NormalizeAnimationBindingRoot(Animator animator)
    {
        Transform skeletonRoot = FindDescendant(animator.transform, "root");
        if (skeletonRoot == null)
            return;

        Transform bindingRoot = skeletonRoot;
        while (bindingRoot.parent != null && bindingRoot.parent != animator.transform)
            bindingRoot = bindingRoot.parent;

        if (bindingRoot.parent == animator.transform)
            bindingRoot.name = AnimationBindingRootName;
    }

    public static Transform FindDescendant(Transform root, string name)
    {
        if (root == null)
            return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name)
                return child;
        return null;
    }
}

/// <summary>
/// Keeps the selected breed on every playable cat root. It survives additive
/// room/game transitions and rebinds each gameplay consumer after changing the
/// visual, so animation state calls never point at a destroyed Animator.
/// </summary>
[DefaultExecutionOrder(-400)]
[DisallowMultipleComponent]
public sealed class CatBreedRuntimeController : MonoBehaviour
{
    private static CatBreedRuntimeController instance;
    private float nextScan;
    private bool missingCatalogReported;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;
        var host = new GameObject("CatBreedRuntimeController");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<CatBreedRuntimeController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void OnEnable()
    {
        CatBreedService.Changed += ApplyImmediately;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        nextScan = 0f;
    }

    private void OnDisable()
    {
        CatBreedService.Changed -= ApplyImmediately;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => nextScan = 0f;

    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + .25f;
        ApplyToAllPlayableCats();
    }

    private void ApplyImmediately()
    {
        nextScan = Time.unscaledTime + .25f;
        ApplyToAllPlayableCats();
    }

    private void ApplyToAllPlayableCats()
    {
        CatBreedCatalog catalog = CatBreedCatalog.Load();
        CatBreedCatalog.Entry entry = CatBreedService.SelectedEntry;
        if (catalog == null || entry == null || catalog.GameplayController == null)
        {
            if (!missingCatalogReported)
            {
                Debug.LogWarning("Cat breed catalog is missing or incomplete; keeping the authored cat visual.", this);
                missingCatalogReported = true;
            }
            return;
        }

        missingCatalogReported = false;
        foreach (CatMovement cat in FindObjectsByType<CatMovement>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            ApplyToOwner(cat.transform, entry, catalog, animator => BindHome(cat, animator));

        foreach (CatRunnerPlayer runner in FindObjectsByType<CatRunnerPlayer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            ApplyToOwner(runner.transform, entry, catalog,
                animator => runner.RebindBreedVisual(animator, FindVisualRoot(animator.transform, runner.transform)));

        foreach (CatCatchPlayer catcher in FindObjectsByType<CatCatchPlayer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            ApplyToOwner(catcher.transform, entry, catalog, catcher.RebindAnimator);
    }

    private static void ApplyToOwner(
        Transform owner,
        CatBreedCatalog.Entry entry,
        CatBreedCatalog catalog,
        Action<Animator> bind)
    {
        if (owner == null || !owner.gameObject.scene.IsValid())
            return;

        Animator currentAnimator = owner.GetComponentInChildren<Animator>(true);
        if (currentAnimator == null)
            return;
        Transform currentRoot = FindVisualRoot(currentAnimator.transform, owner);
        if (currentRoot == null)
            return;

        CatBreedVisualTag tag = currentRoot.GetComponent<CatBreedVisualTag>();
        if (tag == null && string.Equals(entry.Id, CatBreedCatalog.DefaultBreedId,
                                         StringComparison.Ordinal))
        {
            tag = currentRoot.gameObject.AddComponent<CatBreedVisualTag>();
            tag.Configure(entry.Id);
            bind(currentAnimator);
            return;
        }

        if (tag != null && string.Equals(tag.BreedId, entry.Id, StringComparison.Ordinal))
            return;

        Vector3 position = currentRoot.localPosition;
        Quaternion rotation = currentRoot.localRotation;
        Vector3 scale = currentRoot.localScale;
        bool active = currentRoot.gameObject.activeSelf;

        GameObject replacement = CatBreedVisualFactory.Create(
            entry, catalog.GameplayController, owner, currentRoot.name);
        if (replacement == null)
            return;
        replacement.transform.localPosition = position;
        replacement.transform.localRotation = rotation;
        replacement.transform.localScale = scale;
        replacement.SetActive(active);

        Animator replacementAnimator = replacement.GetComponentInChildren<Animator>(true);
        if (replacementAnimator == null)
        {
            Destroy(replacement);
            return;
        }

        // Destroy is deferred until the end of the frame. Remove the retired
        // visual from the owner now so another selection/scan cannot find its
        // inactive Animator and install a second, inactive replacement.
        currentRoot.gameObject.SetActive(false);
        currentRoot.SetParent(null, false);
        Destroy(currentRoot.gameObject);
        bind(replacementAnimator);
    }

    private static void BindHome(CatMovement cat, Animator animator)
    {
        cat.RebindAnimator(animator);
        cat.GetComponent<PetInteraction>()?.RebindAnimator(animator);
        cat.GetComponent<BowlInteraction>()?.RebindAnimator(animator);
        cat.GetComponent<SleepInteraction>()?.RebindAnimator(animator);
        cat.GetComponent<CatActivityReaction>()?.RebindAnimator(animator);
        cat.GetComponent<CatIdleBehavior>()?.RebindAnimator(animator);

        PetHeartEffect hearts = cat.GetComponent<PetHeartEffect>();
        Transform spawn = CatBreedVisualFactory.FindDescendant(animator.transform, "HeartSpawn");
        if (hearts != null)
            hearts.RebindSpawnPoint(spawn);
    }

    private static Transform FindVisualRoot(Transform animator, Transform owner)
    {
        Transform visual = animator;
        while (visual != null && visual.parent != null && visual.parent != owner)
            visual = visual.parent;
        return visual != null && visual.parent == owner ? visual : null;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
