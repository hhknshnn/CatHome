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

        StorybookScreenStyle.RoomShell(panel.GetComponent<LowPolyPanelGraphic>(), 34f, false, true);
        StorybookScreenStyle.Card(Surface(panel, "HomeSidebar"), 30f);
        // Leave a visible room vignette at the left edge; the text keeps its authored width.
        var sidebar = panel.Find("HomeSidebar") as RectTransform;
        if (sidebar != null) { sidebar.anchoredPosition = new Vector2(-544f, 0f); sidebar.sizeDelta = new Vector2(370f, 804f); }
        foreach (string name in new[] { "HomeEmblem", "Title", "HomeLevel", "Subtitle", "RoomHint", "RoomFeedback" })
        {
            var item = panel.Find(name) as RectTransform;
            if (item != null) item.anchoredPosition = new Vector2(-544f, item.anchoredPosition.y);
        }
        foreach (string name in new[] { "RoomsHeading", "Title", "Subtitle" })
            StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Ink);
        foreach (string name in new[] { "HomeLevel", "RoomHint", "RoomFeedback" })
            StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Muted);

        var close = panel.Find("CloseButton");
        if (close != null) StorybookScreenStyle.Action(close.GetComponent<Button>(), true);
        StorybookScreenStyle.Inset(Surface(panel, "RoomScroll/VerticalScrollbar"), StorybookScreenStyle.MintWash, 9f);
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
                    StorybookScreenStyle.Text(face.transform, "BadgeText", StorybookScreenStyle.Ink);
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
            StorybookScreenStyle.Selected(face, 22f);
        else
            StorybookScreenStyle.Card(face);

        var action = Surface(face.transform, "ActionFace");
        bool visit = unlocked && !current;
        if (visit)
            StorybookScreenStyle.Enamel(action, StorybookScreenStyle.CoralTop, StorybookScreenStyle.Coral, 17f, false);
        else
            StorybookScreenStyle.Enamel(action, new Color32(102, 142, 132, 255), StorybookScreenStyle.Teal, 17f, false);
        StorybookScreenStyle.Text(face.transform, "ActionFace/ActionText",
            StorybookScreenStyle.Cream);
        // The complete card remains the original button. Locked cards still open the store,
        // and the controller continues to suspend its existing feedback during transitions.
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }
}
