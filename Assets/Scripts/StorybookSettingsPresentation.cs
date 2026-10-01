using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Settings and privacy surfaces without changing preferences, account state or cloud actions.</summary>
public static class StorybookSettingsPresentation
{
    public static void ApplySettings(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/SettingsPanelVisual");
        if (panel == null) return;
        StorybookScreenStyle.Shell(panel.GetComponent<LowPolyPanelGraphic>(), 34f);
        foreach (string name in new[] { "Title", "SoundSection", "AccessSection", "AccountSection" })
            StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Cream);
        StorybookScreenStyle.Text(panel, "Subtitle", StorybookScreenStyle.Mint);
        foreach (var surface in panel.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            if (!surface.name.StartsWith("Row_", System.StringComparison.Ordinal)) continue;
            StorybookScreenStyle.Card(surface, 20f);
            StorybookScreenStyle.Text(surface.transform, "Label", StorybookScreenStyle.Ink);
            StorybookScreenStyle.Text(surface.transform, "AccountStatus", StorybookScreenStyle.Muted);
        }
        Action(panel, "CloseButton", true);
        Action(panel, "PrivacyDataButton");
        // RefreshRows applies the real on/off/account states after their existing labels and availability update.
    }

    public static void ApplyPrivacy(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/PrivacyDataCard");
        if (panel != null)
        {
            StorybookScreenStyle.Shell(panel.GetComponent<LowPolyPanelGraphic>(), 34f);
            StorybookScreenStyle.Text(panel, "Title", StorybookScreenStyle.Cream);
            foreach (string name in new[] { "DeleteNote", "UnityPortal" })
                StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Mint);
            var status = panel.Find("CloudStatusWell");
            if (status != null)
            {
                StorybookScreenStyle.Card(status.GetComponent<LowPolyPanelGraphic>(), 22f);
                StorybookScreenStyle.Text(status, "Status", StorybookScreenStyle.Ink);
            }
            foreach (string name in new[] { "CloseButton", "PrivacyPolicyButton", "DataRequestButton", "DeleteInfoButton" })
                Action(panel, name, true);
            Action(panel, "SyncNowButton");
            Action(panel, "DeleteCloudAccountButton", false, true);
        }
        var confirmation = root.Find("SafeArea/DeleteConfirmation");
        if (confirmation != null)
        {
            StorybookScreenStyle.Shell(confirmation.GetComponent<LowPolyPanelGraphic>(), 32f);
            StorybookScreenStyle.Text(confirmation, "Title", StorybookScreenStyle.Cream);
            StorybookScreenStyle.Text(confirmation, "Consequences", StorybookScreenStyle.Cream);
            Action(confirmation, "CancelDelete");
            Action(confirmation, "ConfirmDelete", false, true);
        }
        // Existing controls own enabled states and confirmation visibility; no account or cloud method is called here.
    }

    public static void Toggle(Button button, TMP_Text state, bool on)
    {
        if (button == null || state == null) return;
        StorybookScreenStyle.Action(button, !on);
        var face = button.targetGraphic as LowPolyPanelGraphic;
        if (face == null) return;
        face.ConfigureScreenStyle(on ? new Color32(111, 221, 197, 255) : new Color32(79, 102, 133, 255),
            on ? StorybookScreenStyle.Teal : new Color32(55, 75, 108, 255), 27f, false, false);
        var knob = face.transform.Find("SwitchKnob") as RectTransform;
        if (knob == null)
        {
            knob = new GameObject("SwitchKnob", typeof(RectTransform), typeof(CanvasRenderer), typeof(LowPolyPanelGraphic)).GetComponent<RectTransform>();
            knob.SetParent(face.transform, false);
            knob.GetComponent<LowPolyPanelGraphic>().raycastTarget = false;
        }
        knob.anchorMin = knob.anchorMax = new Vector2(on ? 1f : 0f, .5f);
        knob.pivot = new Vector2(.5f, .5f);
        knob.sizeDelta = new Vector2(38f, 38f);
        knob.anchoredPosition = new Vector2(on ? -27f : 27f, 0f);
        knob.GetComponent<LowPolyPanelGraphic>().ConfigureScreenStyle(Color.white, StorybookScreenStyle.Cream, 19f, false, true);
        state.rectTransform.anchorMin = Vector2.zero;
        state.rectTransform.anchorMax = Vector2.one;
        state.rectTransform.offsetMin = new Vector2(on ? 10f : 49f, 7f);
        state.rectTransform.offsetMax = new Vector2(on ? -49f : -8f, -7f);
        state.fontStyle = FontStyles.Bold;
        state.enableAutoSizing = true;
        state.fontSizeMin = 14f;
        state.fontSizeMax = 18f;
        state.textWrappingMode = TextWrappingModes.NoWrap;
        state.alignment = TextAlignmentOptions.Center;
        // The existing Button owns its full 124x54 hit area and preference callback.
        // State is communicated by position, text and colour together.
    }

    private static void Action(Transform root, string path, bool secondary = false, bool coral = false)
    {
        var child = root.Find(path);
        if (child != null) StorybookScreenStyle.Action(child.GetComponent<Button>(), secondary, coral);
    }
}
