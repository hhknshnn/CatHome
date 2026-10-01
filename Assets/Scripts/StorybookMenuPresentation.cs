using UnityEngine;
using UnityEngine.UI;

/// <summary>Compact navigation-list finish; preserves the authored icons, button effects and responsive sizing.</summary>
public static class StorybookMenuPresentation
{
    public static void Apply(Transform root)
    {
        if (root == null) return;
        var list = root.Find("SafeArea/MenuList");
        if (list == null) return;
        var listRect = list as RectTransform;
        if (listRect != null)
            listRect.anchoredPosition = new Vector2(listRect.anchoredPosition.x,
                -(StorybookHudLayout.TopPanelBottom + 16f));

        StorybookScreenStyle.Inset(Surface(list, "ListRim"), new Color32(44, 86, 103, 255), 20f);
        StorybookScreenStyle.Shell(Surface(list, "ListFace"), 18f);
        foreach (Transform row in list)
        {
            if (!row.name.StartsWith("Row_", System.StringComparison.Ordinal)) continue;
            // Face is the authored target graphic. Leave the transparent hit frame and existing FX intact.
            StorybookScreenStyle.Shell(Surface(row, "Face"), 12f);
            StorybookScreenStyle.Text(row, "Content/Label", StorybookScreenStyle.Cream);
            var divider = row.Find("Divider");
            var line = divider == null ? null : divider.GetComponent<Image>();
            if (line != null)
            {
                var tint = StorybookScreenStyle.Mint;
                tint.a = .22f;
                line.color = tint;
            }
        }
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }
}
