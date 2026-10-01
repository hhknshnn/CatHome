using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Home game-picker and ranking materials; launch, life, account and reward flows stay unchanged.</summary>
public static class StorybookGamesPresentation
{
    public static void ApplyGames(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/GamesHubCard");
        if (panel == null) return;
        StorybookScreenStyle.Shell(panel.GetComponent<LowPolyPanelGraphic>(), 34f);
        StorybookScreenStyle.Text(panel, "GamesTitle", StorybookScreenStyle.Cream);
        StorybookScreenStyle.Text(panel, "GamesSubtitle", StorybookScreenStyle.Mint);
        ApplyGameCard(panel.Find("HubCatRunnerButton/Visual"), true);
        ApplyGameCard(panel.Find("HubCatCatchButton/Visual"), false);
        Action(panel, "GamesHubLeaderboards");
        Action(panel, "GamesHubClose", true);
    }

    public static void ApplyLeaderboard(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/LeaderboardCard");
        if (panel == null) return;
        StorybookScreenStyle.Shell(panel.GetComponent<LowPolyPanelGraphic>(), 34f);
        StorybookScreenStyle.Text(panel, "Title", StorybookScreenStyle.Cream);
        StorybookScreenStyle.Text(panel, "Status", StorybookScreenStyle.Mint);
        Action(panel, "Close", true);
        Action(panel, "Refresh");
        foreach (string name in new[] { "RunnerTab", "CatchTab", "DailyTab", "WeeklyTab", "AllTimeTab" })
            Action(panel, name, true);
        var allTime = panel.Find("AllTimeTab");
        var periodLabel = allTime == null ? null : allTime.GetComponentInChildren<TMP_Text>(true);
        if (periodLabel != null)
        {
            periodLabel.rectTransform.offsetMin = new Vector2(8f, periodLabel.rectTransform.offsetMin.y);
            periodLabel.rectTransform.offsetMax = new Vector2(-8f, periodLabel.rectTransform.offsetMax.y);
            periodLabel.textWrappingMode = TextWrappingModes.NoWrap;
            periodLabel.enableAutoSizing = true;
            periodLabel.fontSizeMin = 18f;
            periodLabel.fontSizeMax = 22f;
        }

        var list = panel.Find("LeaderboardList");
        if (list != null)
        {
            StorybookScreenStyle.Inset(list.GetComponent<LowPolyPanelGraphic>(), new Color32(31, 47, 90, 255), 24f);
            foreach (string name in new[] { "SelectedGame", "EmptyState" })
                StorybookScreenStyle.Text(list, name, StorybookScreenStyle.Cream);
            foreach (string name in new[] { "GlobalSummary", "SelectedPeriod", "OwnRank" })
                StorybookScreenStyle.Text(list, name, StorybookScreenStyle.Mint);
            foreach (var label in list.GetComponentsInChildren<TMP_Text>(true))
                if (label.name == "RowLabel") ApplyRankRow(label, false);
            var track = list.Find("Scrollbar");
            var image = track == null ? null : track.GetComponent<Image>();
            if (image != null) image.color = new Color32(21, 35, 71, 255);
            StorybookScreenStyle.Enamel(Surface(list, "Scrollbar/Handle"),
                StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 4f);
        }

        var identity = panel.Find("YourCat");
        if (identity != null)
        {
            StorybookScreenStyle.Card(identity.GetComponent<LowPolyPanelGraphic>(), 24f);
            foreach (string name in new[] { "CatName", "Connection" })
                StorybookScreenStyle.Text(identity, name, StorybookScreenStyle.Ink);
            foreach (string name in new[] { "Privacy", "Rewards" })
                StorybookScreenStyle.Text(identity, name, StorybookScreenStyle.Muted);
        }
    }

    public static void ApplyRankRow(TMP_Text label, bool currentPlayer)
    {
        if (label == null) return;
        var row = label.transform.parent;
        var surface = row == null ? null : row.GetComponent<LowPolyPanelGraphic>();
        if (currentPlayer)
            StorybookScreenStyle.Enamel(surface, StorybookScreenStyle.Cream, new Color32(213, 239, 225, 255), 16f);
        else
            StorybookScreenStyle.Card(surface, 16f);
        label.color = StorybookScreenStyle.Ink;
        // The authored 58-unit row reserved 20 units above and below a 30-unit line.
        label.rectTransform.offsetMin = new Vector2(label.rectTransform.offsetMin.x, 10f);
        label.rectTransform.offsetMax = new Vector2(label.rectTransform.offsetMax.x, -10f);
    }

    private static void ApplyGameCard(Transform visual, bool runner)
    {
        if (visual == null) return;
        StorybookScreenStyle.Card(visual.GetComponent<LowPolyPanelGraphic>(), 28f);
        StorybookScreenStyle.Text(visual, "Title", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(visual, "Descriptor", StorybookScreenStyle.Muted);
        StorybookScreenStyle.Text(visual, "Lives", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Enamel(Surface(visual, "Action"),
            runner ? new Color32(255, 157, 121, 255) : StorybookScreenStyle.Mint,
            runner ? StorybookScreenStyle.Coral : StorybookScreenStyle.Teal, 22f, false);
        StorybookScreenStyle.Text(visual, "Action/Label", StorybookScreenStyle.Ink);
        // The card's existing button and feedback own interaction; the coloured plate is decorative.
        // Hero image, mask, preview UVs and every label's live text remain untouched.
    }

    private static void Action(Transform root, string path, bool secondary = false)
    {
        var child = root.Find(path);
        if (child != null) StorybookScreenStyle.Action(child.GetComponent<Button>(), secondary);
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }
}
