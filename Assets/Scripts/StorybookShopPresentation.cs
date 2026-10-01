using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The store, its purchase sheets and placement toolbar share the home HUD's materials.</summary>
public static class StorybookShopPresentation
{
    public static void Apply(Transform root)
    {
        if (root == null) return;
        StorybookScreenStyle.CurrencyIcons(root);
        foreach (var preview in root.GetComponentsInChildren<RawImage>(true))
        {
            if (preview.name != "DialogProductIcon" && preview.name != "ProductPreview" &&
                !(preview.name == "Icon" && preview.transform.parent.name == "RequestedProduct")) continue;
            var rounding = preview.GetComponent<StorybookRoundedPreview>();
            if (rounding == null) rounding = preview.gameObject.AddComponent<StorybookRoundedPreview>();
            rounding.Configure(preview.name == "Icon" ? 12f : 26f);
        }
        foreach (var face in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            if (face.color.a < .98f || face.name.Contains("Blocker") || face.name.Contains("Scrim")) continue;
            switch (face.name)
            {
                case "Surface":
                case "DialogFace":
                case "ConfirmationFace":
                case "DiamondStoreFace":
                    StorybookScreenStyle.Shell(face, 34f); break;
                case "Wallet":
                case "OwnedBadge":
                case "BestMatch":
                    StorybookScreenStyle.Shell(face, 18f); break;
                case "ProductArtStage":
                    // Match the authored product photography backdrop exactly;
                    // square source images then sit naturally inside the rounded stage.
                    var photoPaper = new Color32(255, 249, 239, 255);
                    face.ConfigureScreenStyle(photoPaper, photoPaper, 20f, false, false); break;
                case "ModernTabRail":
                    StorybookScreenStyle.Inset(face, new Color32(22, 36, 76, 255), 26f); break;
                case "Card":
                case "RequestedProduct":
                case "PlacementInfo":
                    StorybookScreenStyle.Card(face); break;
                default:
                    if (face.name.StartsWith("Pack_")) StorybookScreenStyle.Card(face);
                    break;
            }
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name.IndexOf("Scrim", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
            bool secondary = button.name.Contains("Close") || button.name.Contains("Cancel") || button.name == "NotNow";
            StorybookScreenStyle.Action(button, secondary);
        }
        var content = root.Find("SafeArea/Panel/StoreContent");
        if (content != null)
        {
            var balance = content.Find("Wallet/Balance") as RectTransform;
            if (balance != null)
            {
                balance.anchoredPosition = new Vector2(-48f, balance.anchoredPosition.y);
                balance.sizeDelta = new Vector2(88f, balance.sizeDelta.y);
            }
            foreach (string name in new[] { "Title", "SectionTitle" }) StorybookScreenStyle.Text(content, name, StorybookScreenStyle.Cream);
            foreach (string name in new[] { "Subtitle", "OwnedCount", "HomeLevelText", "Feedback" }) StorybookScreenStyle.Text(content, name, StorybookScreenStyle.Mint);
            var surface = content.Find("Surface");
            if (surface != null) surface.SetAsFirstSibling();
        }
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            switch (label.name)
            {
                case "Balance": case "DiamondBalance": case "OwnedText":
                case "DialogTitle": case "ConfirmationTitle": case "ConfirmationMessage":
                    label.color = StorybookScreenStyle.Cream; break;
                case "DialogMessage": label.color = StorybookScreenStyle.Mint; break;
                case "ProductTitle": case "SpecialPrice": case "CoinPrice": case "DiamondPrice":
                    label.color = StorybookScreenStyle.Ink; break;
            }
        }
        var diamondSheet = root.Find("SafeArea/Panel/DiamondStore/DiamondStoreFace");
        foreach (string name in new[] { "Title", "Subtitle", "Feedback" }) StorybookScreenStyle.Text(diamondSheet, name, StorybookScreenStyle.Cream);
    }
}
