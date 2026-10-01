using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QuestState = CatHome.Quests.QuestState;

/// <summary>Quest chrome and readable state colours; the controller remains the sole owner of rows and claims.</summary>
public static class StorybookQuestPresentation
{
    public static readonly Color PositiveText = new Color32(22, 104, 108, 255);

    public static void Apply(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/Panel");
        if (panel == null) return;
        StorybookScreenStyle.Shell(Surface(panel, "Face"), 34f);
        StorybookScreenStyle.Text(panel, "Header/Title", StorybookScreenStyle.Cream);
        StorybookScreenStyle.Text(panel, "LevelLabel/ChapterLabel", StorybookScreenStyle.Mint);
        // Message is itself the TMP text object in the authored quest panel.
        StorybookScreenStyle.Text(panel, "Message", StorybookScreenStyle.Cream);
        StorybookScreenStyle.Inset(Surface(panel, "Footer/Bar"), new Color32(22, 36, 76, 255), 16f);
        StorybookScreenStyle.Text(panel, "Footer/Label", StorybookScreenStyle.Mint);
        var content = panel.Find("ScrollView/Viewport/Content");
        if (content != null) foreach (Transform row in content)
        {
            var info = row.Find("Content") as RectTransform;
            if (info == null) continue;
            // Keep the authored type size while giving the progress line its full line height.
            info.offsetMin = new Vector2(info.offsetMin.x, 12f);
            info.offsetMax = new Vector2(info.offsetMax.x, -12f);
            var meta = info.Find("Info/Meta");
            var sizing = meta == null ? null : meta.GetComponent<LayoutElement>();
            if (sizing != null) sizing.minHeight = sizing.preferredHeight = 36f;
        }
        foreach (string path in new[] { "Header/CloseButton", "LevelLabel/ChapterTab", "LevelLabel/DailyTab" })
        {
            var child = panel.Find(path);
            if (child != null) StorybookScreenStyle.Action(child.GetComponent<Button>(), true);
        }
        foreach (var scrollbar in panel.GetComponentsInChildren<Scrollbar>(true))
        {
            var track = scrollbar.GetComponent<LowPolyPanelGraphic>();
            if (track != null) StorybookScreenStyle.Inset(track, new Color32(22, 36, 76, 255), 6f);
            var image = scrollbar.GetComponent<Image>();
            if (image != null) image.color = new Color32(22, 36, 76, 255);
            var handle = scrollbar.targetGraphic as LowPolyPanelGraphic;
            if (handle != track)
                StorybookScreenStyle.Enamel(handle, StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 5f);
        }
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }

    public static void ApplyRow(Transform row, QuestSnapshot snapshot, TMP_Text progress,
        TMP_Text title, TMP_Text description, TMP_Text rewards)
    {
        if (row == null) return;
        var content = row.Find("Content") as RectTransform;
        if (content != null) content.offsetMin = new Vector2(126f, content.offsetMin.y);
        var badgeRoot = row.Find("QuestProgressBadge") as RectTransform;
        if (badgeRoot == null)
        {
            badgeRoot = new GameObject("QuestProgressBadge", typeof(RectTransform)).GetComponent<RectTransform>();
            badgeRoot.SetParent(row, false);
            badgeRoot.anchorMin = badgeRoot.anchorMax = new Vector2(0f, .5f);
            badgeRoot.pivot = new Vector2(0f, .5f);
            badgeRoot.anchoredPosition = new Vector2(22f, 0f);
            badgeRoot.sizeDelta = new Vector2(82f, 114f);
            var icon = new GameObject("ProgressRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(StorybookQuestBadge)).GetComponent<RectTransform>();
            icon.SetParent(badgeRoot, false);
            icon.anchorMin = new Vector2(0, 1); icon.anchorMax = Vector2.one;
            icon.offsetMin = new Vector2(0, -82); icon.offsetMax = Vector2.zero;
        }
        badgeRoot.GetComponentInChildren<StorybookQuestBadge>().Configure(snapshot);
        if (progress != null)
        {
            progress.transform.SetParent(badgeRoot, false);
            progress.rectTransform.anchorMin = Vector2.zero;
            progress.rectTransform.anchorMax = new Vector2(1, 0);
            progress.rectTransform.offsetMin = Vector2.zero;
            progress.rectTransform.offsetMax = new Vector2(0, 27);
            progress.fontSize = 19f;
            progress.enableAutoSizing = true; progress.fontSizeMin = 15f; progress.fontSizeMax = 19f;
            progress.alignment = TextAlignmentOptions.Center;
            progress.textWrappingMode = TextWrappingModes.NoWrap;
            progress.color = snapshot.State == QuestState.Locked ? StorybookScreenStyle.Muted : PositiveText;
        }
        if (title != null) title.color = StorybookScreenStyle.Ink;
        if (description != null) description.color = StorybookScreenStyle.Muted;
        if (rewards != null)
        {
            rewards.color = PositiveText;
            rewards.fontStyle = FontStyles.Bold;
            rewards.fontSizeMax = 21f;
        }
    }
}
