using UnityEngine;
using UnityEngine.UI;

/// <summary>Uses the same correctly framed real-breed presentation as My Cat.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(RawImage))]
public sealed class CatCelebrationPreview : MonoBehaviour
{
    private CatBreedTurntablePreview preview;
    public void Begin(CatMovement source)
    {
        Cleanup();
        if (source == null) return;
        preview = GetComponent<CatBreedTurntablePreview>();
        if (preview == null) preview = gameObject.AddComponent<CatBreedTurntablePreview>();
        preview.Show(CatBreedService.SelectedEntry);
        preview.SetTint(CatIdentityService.CurrentTint);
        preview.SetReducedMotion(CatRunnerProgressService.ReducedMotion);
        GetComponent<RawImage>().raycastTarget = false;
    }
    public void Cleanup() { if (preview != null) preview.Cleanup(); }
    private void OnDisable() => Cleanup();
    private void OnDestroy() => Cleanup();
}
