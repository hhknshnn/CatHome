using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Rewards and notices use the same cream popup finish in every room.</summary>
public sealed class LivingPopupSurface : MonoBehaviour
{
    public static void Apply(Transform panel, string faceName, string kind)
    {
        if (panel == null) return;
        var face = string.IsNullOrEmpty(faceName) ? panel : panel.Find(faceName);
        if (face == null) return;
        StorybookScreenStyle.RoomShell(face.GetComponent<LowPolyPanelGraphic>(), kind == "success" ? 24f : 32f, kind == "success");
        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "CollectButton" || button.name == "WelcomeBackButton")
                StorybookScreenStyle.Action(button, false, true);
            else if (button.name == "WatchAdButton") StorybookScreenStyle.Action(button, true);
        }
        foreach (var label in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.name == "Title" || label.name == "Message") label.color = StorybookScreenStyle.Ink;
            else if (label.name == "Subtitle" || label.name == "Detail" || label.name == "MomentCaption")
                label.color = StorybookScreenStyle.Muted;
        }
    }
}
