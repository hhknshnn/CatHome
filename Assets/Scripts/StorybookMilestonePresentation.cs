using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared milestone surfaces; reward grants, portraits and celebration timelines remain with each view.</summary>
public static class StorybookMilestonePresentation
{
    public static void Apply(Transform panel)
    {
        if (panel == null) return;
        var content = panel;
        var face = panel.Find("Face");
        if (face != null && face.GetComponent<LowPolyPanelGraphic>() != null)
        {
            content = face;
            StorybookScreenStyle.RoomShell(face.GetComponent<LowPolyPanelGraphic>(), 32f);
        }
        else StorybookScreenStyle.RoomShell(Surface(panel, "IvoryFace"), 32f);

        // A light mint stage keeps its illustrated captions readable without changing the existing art layers.
        StorybookScreenStyle.Inset(Surface(content, "MomentStage"), StorybookScreenStyle.Mint, 28f);
        StorybookScreenStyle.Card(Surface(content, "RewardTray"), 22f);
        StorybookScreenStyle.Text(content, "Title", StorybookScreenStyle.Ink);
        bool onboarding = content.Find("LetsPlayButton") != null;
        StorybookScreenStyle.Text(content, "Subtitle", onboarding ? StorybookScreenStyle.Ink : StorybookScreenStyle.Muted);
        StorybookScreenStyle.Text(content, "Detail", StorybookScreenStyle.Muted);
        StorybookScreenStyle.Text(content, "Reward", StorybookScreenStyle.Ink);
        if (onboarding)
        {
            RefreshCopy(content, "Title", "celebration.care");
            RefreshCopy(content, "Subtitle", "celebration.care_reward");
            RefreshCopy(content, "LetsPlayButton/ButtonFace/Label", "celebration.play");
        }

        foreach (var caption in content.GetComponentsInChildren<TMP_Text>(true))
        {
            if (caption.name != "MomentCaption") continue;
            Vector2 position = caption.rectTransform.anchoredPosition;
            bool onStage = caption.transform.parent.name == "MomentStage" || position.x < -200f;
            caption.color = onStage ? StorybookScreenStyle.Ink
                : position.y > 150f ? StorybookScreenStyle.Muted : StorybookScreenStyle.Ink;
            if (onboarding && Mathf.Abs(position.x - 176f) < .1f && Mathf.Abs(position.y + 134f) < .1f)
            {
                var copy = caption.GetComponent<BilingualCopyLabel>();
                if (copy == null) copy = caption.gameObject.AddComponent<BilingualCopyLabel>();
                copy.Configure("Kedi komutları düğmesinden komutları ve Oyun rehberi sekmesini açabilirsin.",
                    "Open Cat commands for commands and the Play guide tab.");
            }
        }
        Action(content, "CollectButton", true);
        Action(content, "WatchAdButton", false);
        Action(content, "LetsPlayButton", true);
        StorybookScreenStyle.CurrencyIcons(content);
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }

    private static void Action(Transform root, string path, bool coral)
    {
        var child = root.Find(path);
        if (child != null) StorybookScreenStyle.Action(child.GetComponent<Button>(), false, coral);
    }

    private static void RefreshCopy(Transform root, string path, string key)
    {
        var child = root.Find(path);
        var label = child == null ? null : child.GetComponent<TMP_Text>();
        if (label == null) return;
        var localized = label.GetComponent<LocalizedLabel>();
        if (localized != null) localized.Refresh();
        else label.text = GameLanguageService.Text(key);
    }
}
