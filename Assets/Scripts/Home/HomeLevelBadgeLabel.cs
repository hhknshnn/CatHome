using TMPro;
using UnityEngine;

/// <summary>
/// Paints a single "HOME LV. N" badge straight from
/// <see cref="HomeProgressionService.HomeLevel"/> and keeps it live.
///
/// This sits on the same object as its <see cref="TMP_Text"/> so the label is
/// authoritative on its own: it does not depend on a parent controller's Refresh
/// running. That matters because the currency HUD and the shop panel controllers
/// only repaint during play, while this label is <see cref="ExecuteAlways"/> and
/// so also tracks the level in edit mode and inside instantiated prefabs — the
/// same self-contained, per-element idiom as <see cref="CurrencyEntryLayout"/>.
///
/// It is a pure view: it subscribes to <see cref="HomeProgressionService.Changed"/>,
/// never writes progression, save data or economy.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class HomeLevelBadgeLabel : MonoBehaviour
{
    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();

        HomeProgressionService.Changed += Apply;
        GameLanguageService.Changed += Apply;
        Apply();
    }

    private void OnDisable()
    {
        HomeProgressionService.Changed -= Apply;
        GameLanguageService.Changed -= Apply;
    }

    private void Apply()
    {
        if (label != null)
            label.text = GameLanguageService.Format("home.level", HomeProgressionService.HomeLevel);
    }
}
