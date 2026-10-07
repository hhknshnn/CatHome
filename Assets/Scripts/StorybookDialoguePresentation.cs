using UnityEngine;
using UnityEngine.UI;

/// <summary>Dialogue and name-field surfaces without changing their input or continuation behavior.</summary>
public static class StorybookDialoguePresentation
{
    // Receive the newly built panel directly: an older panel may still await deferred destruction during Rebuild.
    public static void Apply(Transform panel)
    {
        if (panel == null) return;
        StorybookScreenStyle.RoomShell(Surface(panel, "Face"), 28f, true);
        StorybookScreenStyle.Enamel(Surface(panel, "CatPortrait/PortraitFrame"),
            StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 28f);
        StorybookScreenStyle.Text(panel, "Name", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(panel, "Message", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(panel, "LessonProgress", StorybookScreenStyle.Muted);
        StorybookScreenStyle.Enamel(Surface(panel, "ContinueFace"),
            StorybookScreenStyle.CoralTop, StorybookScreenStyle.Coral, 16f, false);
        StorybookScreenStyle.Text(panel, "Continue", StorybookScreenStyle.Cream);
        var arrow = panel.Find("Continue/ContinueArrow");
        if (arrow != null)
            foreach (var stroke in arrow.GetComponentsInChildren<Image>(true))
                stroke.color = StorybookScreenStyle.Cream;

        StorybookScreenStyle.Inset(Surface(panel, "NameInput/FieldFace"), StorybookScreenStyle.Paper, 18f);
        StorybookScreenStyle.Text(panel, "NameInput/Text", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(panel, "NameInput/Placeholder", StorybookScreenStyle.Muted);
        var confirm = panel.Find("Confirm");
        if (confirm != null) StorybookScreenStyle.Action(confirm.GetComponent<Button>());
        // The entire DialoguePanel is also a Button. Its original target, hit area and transition remain intact.
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }
}
