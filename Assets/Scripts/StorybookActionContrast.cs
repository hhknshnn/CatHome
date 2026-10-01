using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Bright action faces need dark text; disabled indigo faces need cream text.</summary>
[DisallowMultipleComponent]
public sealed class StorybookActionContrast : MonoBehaviour
{
    private Button button;
    private TMP_Text[] labels;
    private Color activeColor, disabledColor;
    private bool lastInteractable;
    private bool applied;

    public void Configure(Button owner, Color active, Color disabled)
    {
        button = owner;
        if (labels == null) labels = GetComponentsInChildren<TMP_Text>(true);
        activeColor = active; disabledColor = disabled; applied = false;
        Refresh();
    }

    private void OnEnable() { applied = false; Refresh(); }
    private void LateUpdate() { Refresh(); }
    private void Refresh()
    {
        if (button == null || labels == null) return;
        bool interactable = button.IsInteractable();
        if (applied && lastInteractable == interactable) return;
        applied = true; lastInteractable = interactable;
        foreach (var label in labels) if (label != null) label.color = interactable ? activeColor : disabledColor;
    }
}
