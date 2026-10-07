using UnityEngine;
using UnityEngine.UI;

/// <summary>Return-summary finish that leaves offline values, reveal animation and safe-area fitting unchanged.</summary>
public static class StorybookReturnPresentation
{
    public static void Apply(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/CenteredSafeLayout/AnimationContainer");
        if (panel == null) return;
        StorybookScreenStyle.RoomShell(Surface(panel, "ReturnFace"), 32f);
        StorybookScreenStyle.Inset(Surface(panel, "ReturnBanner"), StorybookScreenStyle.MintWash, 26f);
        StorybookScreenStyle.Enamel(Surface(panel, "PortraitMedallion"),
            StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 66f);
        StorybookScreenStyle.Text(panel, "Title", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(panel, "AwayDurationBadge/Duration", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(panel, "ReturnKicker", StorybookScreenStyle.Muted);
        StorybookScreenStyle.Card(Surface(panel, "ReturnSummaryBadge/SummaryFace"), 22f);
        StorybookScreenStyle.Text(panel, "ReturnSummaryBadge/ReturnSummary", StorybookScreenStyle.Ink);
        foreach (string name in new[] { "HungerRewardCard", "ThirstRewardCard", "EnergyRewardCard" })
        {
            var need = panel.Find(name);
            if (need == null) continue;
            StorybookScreenStyle.Card(need.GetComponent<LowPolyPanelGraphic>(), 22f);
            StorybookScreenStyle.Text(need, "NeedLabel", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Text(need, "Value", StorybookScreenStyle.Ink);
        }
        var welcome = panel.Find("WelcomeBackButton");
        if (welcome != null) StorybookScreenStyle.Action(welcome.GetComponent<Button>(), false, true);
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }
}
