using UnityEngine;
using UnityEngine.UI;

/// <summary>Starts an isolated real-breed preview only while its UI is visible.</summary>
[RequireComponent(typeof(RawImage), typeof(CatBreedTurntablePreview))]
public sealed class SelectedCatShowcase : MonoBehaviour
{
    private CatBreedTurntablePreview preview;
    private string shown;
    private void OnEnable() { if(Application.isPlaying) Rebuild(); }
    private void Rebuild()
    {
        preview=GetComponent<CatBreedTurntablePreview>();
        shown=CatBreedService.SelectedBreedId;
        preview.Show(CatBreedService.SelectedEntry);
        preview.SetTint(CatIdentityService.CurrentTint);
        preview.SetReducedMotion(CatRunnerProgressService.ReducedMotion);
    }
    private void LateUpdate() { if(Application.isPlaying && shown!=CatBreedService.SelectedBreedId) Rebuild(); }
    private void OnDisable() { if(preview!=null) preview.Cleanup(); shown=null; }
}
