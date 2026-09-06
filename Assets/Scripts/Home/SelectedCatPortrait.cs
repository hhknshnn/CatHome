using UnityEngine;
using UnityEngine.UI;

/// <summary>Uses the selected breed's real portrait, never a crop of its material atlas.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Image))]
public sealed class SelectedCatPortrait : MonoBehaviour
{
    private Image portrait;
    private void OnEnable()
    {
        portrait = GetComponent<Image>();
        CatBreedService.Changed += Refresh;
        Refresh();
    }
    private void OnDisable() => CatBreedService.Changed -= Refresh;
    public void Refresh()
    {
        if (portrait == null) portrait = GetComponent<Image>();
        var catalog = CatBreedCatalog.Load();
        if (catalog == null || catalog.Count == 0) return;
        var entry = catalog.Find(CatBreedService.SelectedBreedId) ?? catalog.Get(0);
        portrait.sprite = entry.Portrait;
        portrait.preserveAspect = true;
        portrait.color = Color.white;
    }
}
