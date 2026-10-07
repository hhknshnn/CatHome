using UnityEngine;
using UnityEngine.UI;

/// <summary>Companion modal and guide finish, independent of commands, photos and the home shortcut.</summary>
public static class StorybookCompanionPresentation
{
    public static void Apply(Transform root)
    {
        if (root == null) return;
        var card = root.Find("SafeArea/CompanionModal/CompanionCard");
        if (card == null) return;
        StorybookScreenStyle.RoomShell(Surface(card, "Paper"), 32f, true);
        StorybookScreenStyle.Inset(Surface(card, "HeaderWash"), StorybookScreenStyle.MintWash, 24f);
        StorybookScreenStyle.Inset(Surface(card, "CompanionTabs"), StorybookScreenStyle.MintWash, 17f);
        StorybookScreenStyle.Text(card, "Title", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(card, "Subtitle", StorybookScreenStyle.Muted);
        Action(card, "Close", true);
        Action(card, "TogetherTab", true);
        Action(card, "GuideTab", true);

        var commands = card.Find("CommandCards");
        if (commands != null)
        {
            for (int i = 0; i < 3; i++)
            {
                string tile = "Command" + i;
                StorybookScreenStyle.Card(Surface(commands, tile), 24f);
                StorybookScreenStyle.Inset(Surface(commands, tile + "/PoseFrame"), StorybookScreenStyle.Photo, 18f);
                StorybookScreenStyle.Text(commands, tile + "/CommandHint", StorybookScreenStyle.Muted);
                Action(commands, tile + "/CommandButton" + i);
                string need = "CurrentNeeds/Need" + i;
                StorybookScreenStyle.Card(Surface(commands, need), 18f);
                StorybookScreenStyle.Text(commands, need + "/Value", StorybookScreenStyle.Ink);
            }
            StorybookScreenStyle.Text(commands, "CatStatus", StorybookScreenStyle.Muted);
            Action(commands, "StopRest", false, true);
        }

        var guide = card.Find("GuidePages");
        if (guide != null)
        {
            StorybookScreenStyle.Card(Surface(guide, "GuidePaper"), 24f);
            StorybookScreenStyle.Text(guide, "GuideTitle", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Text(guide, "GuideViewport/GuideCopy", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Text(guide, "GuidePage", StorybookScreenStyle.Muted);
            Action(guide, "Previous", true);
            Action(guide, "Next");
        }
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }

    private static void Action(Transform root, string path, bool secondary = false, bool coral = false)
    {
        var child = root.Find(path);
        if (child != null) StorybookScreenStyle.Action(child.GetComponent<Button>(), secondary, coral);
    }
}
