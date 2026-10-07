using UnityEngine;
using UnityEngine.UI;

/// <summary>Storybook surfaces around the real breed preview, name input and unchanged coat palette.</summary>
public static class StorybookCatPresentation
{
    public static void Apply(Transform root)
    {
        if (root == null) return;
        var panel = root.Find("SafeArea/CatBreedShopPanelVisual");
        if (panel == null) return;

        StorybookScreenStyle.RoomShell(panel.GetComponent<LowPolyPanelGraphic>(), 34f);
        foreach (string name in new[] { "Title", "AppearanceTitle" })
            StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Ink);
        foreach (string name in new[] { "Subtitle", "BreedStatus", "CoatLabel" })
            StorybookScreenStyle.Text(panel, name, StorybookScreenStyle.Muted);

        // The transparent live preview sits directly on the glass, as in the approved mockup.
        var well = Surface(panel, "TurntableWell");
        if (well != null) { StorybookRoomBackdrop.Hide(well); well.enabled = false; }
        StorybookScreenStyle.Text(panel, "BreedName", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Inset(Surface(panel, "NameInputWell"), StorybookScreenStyle.Paper, 33f);
        var textArea = panel.Find("NameInputWell/TextArea") as RectTransform;
        if (textArea != null)
        {
            textArea.offsetMin = new Vector2(textArea.offsetMin.x, 11f);
            textArea.offsetMax = new Vector2(textArea.offsetMax.x, -11f);
        }
        StorybookScreenStyle.Text(panel, "NameInputWell/TextArea/Text", StorybookScreenStyle.Ink);
        StorybookScreenStyle.Text(panel, "NameInputWell/TextArea/Placeholder", StorybookScreenStyle.Muted);

        var use = panel.Find("UseThisCatButton");
        if (use != null) StorybookScreenStyle.Action(use.GetComponent<Button>(), false, true);
        var close = panel.Find("CloseButton");
        if (close != null) StorybookScreenStyle.Action(close.GetComponent<Button>(), true);
        StorybookScreenStyle.Inset(Surface(panel, "BreedScroll/ScrollTrack"), StorybookScreenStyle.MintWash, 5f);
        StorybookScreenStyle.Enamel(Surface(panel, "BreedScroll/ScrollTrack/Handle"),
            StorybookScreenStyle.Mint, StorybookScreenStyle.Teal, 5f);

        foreach (var surface in panel.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            switch (surface.name)
            {
                case "CardVisual":
                    StorybookScreenStyle.Card(surface, 17f);
                    StorybookScreenStyle.Text(surface.transform, "BreedLabel", StorybookScreenStyle.Ink);
                    break;
                case "SelectionRing":
                    surface.ConfigureGlassOutline(StorybookScreenStyle.Mint, 20f);
                    break;
                case "ActiveBadge":
                    StorybookScreenStyle.Enamel(surface, StorybookScreenStyle.CoralTop, StorybookScreenStyle.Coral, 16f);
                    CheckColor(surface.transform, "CheckShort");
                    CheckColor(surface.transform, "CheckLong");
                    break;
                default:
                    if (surface.name.StartsWith("CoatSelection_", System.StringComparison.Ordinal))
                        surface.ConfigureGlassOutline(StorybookScreenStyle.Mint, 30f);
                    break;
            }
        }
        // RefreshSelection owns marker visibility; RefreshCoat keeps every swatch at its real palette colour.
        // The live RawImage, portraits and TMP_InputField are not replaced or rebound.
    }

    public static void Selection(Button button, bool selected)
    {
        if (button == null) return;
        var face = button.targetGraphic as LowPolyPanelGraphic;
        if (selected) StorybookScreenStyle.Selected(face, 17f);
        else StorybookScreenStyle.Card(face, 17f);
        foreach (var label in button.GetComponentsInChildren<TMPro.TMP_Text>(true)) label.color = StorybookScreenStyle.Ink;
    }

    public static void Coat(Button button, int index)
    {
        if (button == null || index < 0 || index >= CatIdentityService.CoatCount) return;
        // Retain the exact selectable fur colour, sharing only the frame with the rest of this screen.
        Color tint = CatIdentityService.Palette[index].Tint;
        StorybookScreenStyle.Enamel(button.targetGraphic as LowPolyPanelGraphic, tint, tint, 30f);
    }

    private static LowPolyPanelGraphic Surface(Transform root, string path)
    {
        var child = root.Find(path);
        return child == null ? null : child.GetComponent<LowPolyPanelGraphic>();
    }

    private static void CheckColor(Transform badge, string name)
    {
        var child = badge.Find(name);
        var image = child == null ? null : child.GetComponent<Image>();
        if (image != null) image.color = StorybookScreenStyle.Ink;
    }
}
