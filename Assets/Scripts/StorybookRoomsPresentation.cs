using UnityEngine;
using UnityEngine.UI;

/// <summary>Room-picker materials and state accents; navigation, previews and layout stay with their existing owners.</summary>
public static class StorybookRoomsPresentation
{
    public static void Apply(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/RoomSelectorPanelVisual");
        if (panel == null) return;

        StorybookScreenStyle.Shell(panel.GetComponent<LowPolyPanelGraphic>(), 34f);
        StorybookScreenStyle.Inset(Surface(panel, "HomeSidebar"), new Color32(41, 66, 119, 255), 30f);
        foreach (string name in new[] { "RoomsHeading", "Title", "Subtitle" })
            StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Cream);
        foreach (string name in new[] { "HomeLevel", "RoomHint", "RoomFeedback" })
            StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Mint);

        var close = panel.Find("CloseButton");
        if (close != null) StorybookScreenStyle.Action(close.GetComponent<Button>(), true);
        StorybookScreenStyle.Inset(Surface(panel, "RoomScroll/VerticalScrollbar"), new Color32(22, 36, 76, 255), 9f);
        StorybookScreenStyle.Enamel(Surface(panel, "RoomScroll/VerticalScrollbar/Handle"),
            StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 6f);

        foreach (var face in panel.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            switch (face.name)
            {
                case "CardVisual":
                    StorybookScreenStyle.Card(face);
                    StorybookScreenStyle.Text(face.transform, "RoomTitle", StorybookScreenStyle.Ink);
                    StorybookScreenStyle.Text(face.transform, "RoomStatus", StorybookScreenStyle.Muted);
                    break;
                case "CurrentBadge":
                    StorybookScreenStyle.Enamel(face, StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 12f);
                    StorybookScreenStyle.Text(face.transform, "BadgeText", StorybookScreenStyle.Ink);
                    break;
                case "LockBadge":
                    StorybookScreenStyle.Shell(face, 12f);
                    StorybookScreenStyle.Text(face.transform, "BadgeText", StorybookScreenStyle.Cream);
                    break;
            }
        }
        StorybookScreenStyle.CurrencyIcons(root);
    }

    public static void ApplyCard(Button button, bool current, bool unlocked)
    {
        if (button == null) return;
        var face = button.targetGraphic as LowPolyPanelGraphic;
        if (face == null) return;

        // A cream top keeps the large card a quiet content surface; only its lower tint signals the current room.
        if (current)
            StorybookScreenStyle.Enamel(face, StorybookScreenStyle.Cream, new Color32(213, 239, 225, 255), 22f);
        else
            StorybookScreenStyle.Card(face);

        var action = Surface(face.transform, "ActionFace");
        bool visit = unlocked && !current;
        if (visit)
            StorybookScreenStyle.Enamel(action, StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 17f, false);
        else
            StorybookScreenStyle.Shell(action, 17f);
        StorybookScreenStyle.Text(face.transform, "ActionFace/ActionText",
            visit ? StorybookScreenStyle.Ink : StorybookScreenStyle.Cream);
        // The complete card remains the original button. Locked cards still open the store,
        // and the controller continues to suspend its existing feedback during transitions.
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }
}
