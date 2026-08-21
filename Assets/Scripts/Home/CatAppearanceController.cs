using UnityEngine;

/// <summary>
/// Applies the player's chosen coat tint (from <see cref="CatIdentityService"/>) to
/// the playable cat. Runs as a self-bootstrapped DontDestroyOnLoad manager so it
/// keeps working when a room change clones a fresh cat: it re-finds the current cat
/// and re-tints it, and reapplies instantly whenever the identity changes. The tint
/// is a non-destructive <see cref="MaterialPropertyBlock"/> on the cat renderers, so
/// the shared cat material is never mutated.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatAppearanceController : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");

    private static CatAppearanceController instance;

    private CatMovement trackedCat;
    private Renderer[] catRenderers;
    private MaterialPropertyBlock propertyBlock;
    private float nextScan;
    private Color appliedTint = Color.clear;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;
        var host = new GameObject("CatAppearanceController");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<CatAppearanceController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        CatIdentityService.Changed += HandleIdentityChanged;
    }

    private void OnDisable()
    {
        CatIdentityService.Changed -= HandleIdentityChanged;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.5f;

        CatMovement cat = FindAnyObjectByType<CatMovement>(FindObjectsInactive.Include);
        if (cat != trackedCat || catRenderers == null)
        {
            trackedCat = cat;
            catRenderers = cat != null
                ? cat.GetComponentsInChildren<Renderer>(true)
                : null;
            appliedTint = Color.clear;
        }
        Apply();
    }

    private void HandleIdentityChanged()
    {
        appliedTint = Color.clear;
        Apply();
    }

    private void Apply()
    {
        if (catRenderers == null)
            return;
        Color tint = CatIdentityService.CurrentTint;
        if (tint == appliedTint)
            return;
        appliedTint = tint;
        for (int i = 0; i < catRenderers.Length; i++)
        {
            Renderer renderer = catRenderers[i];
            if (renderer == null)
                continue;
            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, tint);
            propertyBlock.SetColor(LegacyColorId, tint);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
