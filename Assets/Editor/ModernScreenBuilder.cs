using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using U = PremiumUiElements;

/// <summary>Single final authoring pass for the approved white / azure screens.
/// Reuses controller references and input roots; generated art is decoration.</summary>
public static class ModernScreenBuilder
{
    public static void Polish(Transform root)
    {
        if (root == null) return;
        foreach (var panel in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            var rect = panel.rectTransform.rect;
            if (Utility(panel) || rect.width < 110 || rect.height < 42) continue;
            bool content = rect.height >= 150;
            ModernUiArt.Surface(panel, content ? ModernUiArt.Paper : ModernUiArt.Inset,
                content ? (rect.height > 500 ? 28 : 22) : 14, content);
        }
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.color.a < .5f) continue;
            string name = label.name.ToLowerInvariant();
            label.color = name.Contains("subtitle") || name.Contains("description") || name.Contains("hint") ||
                name.Contains("feedback") || name.Contains("caption") ? ModernUiArt.Muted : ModernUiArt.Ink;
            PremiumTypography.Apply(label);
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (!(button.targetGraphic is LowPolyPanelGraphic face) || Utility(face)) continue;
            if (face.rectTransform.rect.height > 180)
            {
                ModernUiArt.Surface(face, ModernUiArt.Paper, 22, true);
                continue;
            }
            string name = button.name.ToLowerInvariant();
            bool secondary = name.Contains("close") || name.Contains("cancel") || name.Contains("back") ||
                name.Contains("exit") || name.Contains("notnow") || name.Contains("later") ||
                name.Contains("previous") || name.StartsWith("row_") || name.Contains("menu") ||
                name.Contains("homebutton") || name.Contains("sound") || name.Contains("motion") ||
                name.Contains("haptic") || name.StartsWith("tab_") && name != "tab_cat";
            ModernUiArt.Action(button, secondary, name.Contains("delete") || name.Contains("erase"));
        }
        foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == "BackdropMotif" || rect.name == "ProductStars" || rect.name == "TravelStars" ||
                rect.name == "GameIdentityStars" || rect.name == "JoyfulGamesBanner" || rect.name == "ResultBanner" ||
                rect.name == "GameIdentityBanner" || rect.name == "ShortcutBackdropArt") rect.gameObject.SetActive(false);
            switch (rect.name)
            {
                case "FoodBar": Need(rect, ModernUiArt.Coral); break;
                case "ThirstUI": Need(rect, ModernUiArt.Azure); break;
                case "EnergyUI": Need(rect, ModernUiArt.Purple); break;
                case "CatShopButton": Identity(rect); break;
                case "HomeDock": Dock(rect); break;
                case "CoinEntry": case "DiamondEntry": Wallet(rect); break;
                case "PremiumBase":
                    if (rect.GetComponentInParent<MobileJoystick>() != null)
                        ModernUiArt.Surface(rect.GetComponent<LowPolyPanelGraphic>(), new Color32(105, 186, 215, 255), 94, true);
                    break;
                case "PremiumHandle":
                    ModernUiArt.Surface(rect.GetComponent<LowPolyPanelGraphic>(), ModernUiArt.Paper, 46, true); break;
                case "HomeSidebar": Surface(rect, ModernUiArt.Inset, 22); break;
                case "ScorePanel": Surface(rect, ModernUiArt.Paper, 16); TextColor(rect, ModernUiArt.Ink); break;
                case "WelcomeTitle": case "ResultTitle": case "GamesTitle": case "GamesSubtitle":
                    if (rect.GetComponent<TMP_Text>() != null) rect.GetComponent<TMP_Text>().color = ModernUiArt.Ink; break;
                case "OwnedBadge": case "CurrentBadge": Surface(rect, ModernUiArt.Mint, 10); TextColor(rect, ModernUiArt.Ink); break;
            }
            if (rect.name.StartsWith("Product_", StringComparison.Ordinal)) Product(rect);
            if (rect.name.StartsWith("RoomCard_", StringComparison.Ordinal)) RoomCard(rect);
            if (rect.name == "HubCatRunnerButton" || rect.name == "HubCatCatchButton") GameCard(rect);
        }
        foreach (var shop in root.GetComponentsInChildren<ShopPanelController>(true)) Shop(shop.transform);
    }

    private static bool Utility(LowPolyPanelGraphic panel)
    {
        string name = panel.name.ToLowerInvariant();
        return panel.color.a < .98f || panel.GetComponent<Mask>() != null || name.Contains("shadow") ||
            name.Contains("depth") || name.Contains("scrim") || name.Contains("blocker") || name.Contains("mask") ||
            name.Contains("dragsurface") || name.Contains("glow") || name.Contains("filltrack");
    }
    private static RectTransform Find(Transform root, string name)
    {
        foreach (var rect in root.GetComponentsInChildren<RectTransform>(true)) if (rect.name == name) return rect;
        return null;
    }
    private static void Surface(Transform root, Color color, float radius)
        => ModernUiArt.Surface(root != null ? root.GetComponent<LowPolyPanelGraphic>() : null, color, radius, radius >= 20);
    private static void NamedSurface(Transform root, string name, Color color, float radius = 20)
        => Surface(Find(root, name), color, radius);
    private static void TextColor(Transform root, Color color)
    { foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) label.color = color; }
    private static void Row(RectTransform rect, float y, float height, float inset)
    {
        if (rect == null) return;
        rect.anchorMin = new Vector2(0, .5f); rect.anchorMax = new Vector2(1, .5f); rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(-inset * 2, height);
    }
    private static void Type(RectTransform rect, float size, Color color)
    {
        if (rect == null) return;
        var label = rect.GetComponent<TMP_Text>(); if (label == null) return;
        label.fontSize = size; label.color = color; label.enableAutoSizing = false;
        PremiumTypography.Apply(label);
    }

    private static void Need(RectTransform root, Color accent)
    {
        NamedSurface(root, "Background", Color.Lerp(accent, Color.white, .95f), 20);
        var well = Find(root, "NeedArtWell"); if (well != null) well.gameObject.SetActive(false);
        var track = Find(root, "FillTrackMask");
        if (track != null)
        {
            U.At(track, 31, -20, 164, 12);
            ModernUiArt.Surface(track.GetComponent<LowPolyPanelGraphic>(), Color.Lerp(accent, Color.white, .82f), 6);
        }
        var fill = Find(root, "Fill");
        if (fill != null && fill.GetComponent<Image>() != null) fill.GetComponent<Image>().color = accent;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            label.color = ModernUiArt.Ink;
            if (label.name == "PercentageText") { label.fontSize = 26; label.enableAutoSizing = false; }
        }
    }

    private static void Identity(RectTransform root)
    {
        NamedSurface(root, "Face", ModernUiArt.Paper, 20);
        NamedSurface(root, "PortraitMedallion", new Color32(199, 230, 255, 255), 38);
        TextColor(root, ModernUiArt.Ink);
        var level = Find(root, "HomeLevelText"); Type(level, 19, ModernUiArt.Muted);
        var button = root.GetComponent<Button>(); if (button != null) ModernUiArt.FlatNavigation(button);
    }

    private static void Wallet(RectTransform root)
    {
        foreach (var face in root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
            if (face.name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) < 0 && face.rectTransform.rect.width > 75)
                ModernUiArt.Surface(face, ModernUiArt.Paper, 16);
        TextColor(root, ModernUiArt.Ink);
        foreach (var button in root.GetComponentsInChildren<Button>(true)) ModernUiArt.Action(button);
    }

    private static void Dock(RectTransform root)
    {
        var canvas=root.GetComponentInParent<Canvas>();
        if(canvas!=null)
        {
            // This backing belongs to the canvas, not HomeDock: launcher
            // visibility fades that entire CanvasGroup during dialogue.
            var strip=canvas.transform.Find("ModernHomeViewportStrip") as RectTransform;
            if(strip==null)strip=U.Rect("ModernHomeViewportStrip",canvas.transform);
            var fill=strip.GetComponent<Image>()??strip.gameObject.AddComponent<Image>();
            fill.raycastTarget=false;fill.color=ModernUiArt.Paper;
            var repaint=strip.GetComponent<ModernHomeStrip>()??strip.gameObject.AddComponent<ModernHomeStrip>();
            strip.SetAsFirstSibling();repaint.Refresh();
        }
        root.sizeDelta = new Vector2(1080, 64);
        NamedSurface(root, "DockEnamelTray", ModernUiArt.Paper, 0);
        for (int i = 0; i < 3; i++)
        {
            string name = "ModernDockDivider" + i;
            var divider = root.Find(name) as RectTransform;
            if (divider == null)
            {
                divider = U.Rect(name, root);
                var line = divider.gameObject.AddComponent<Image>(); line.color = ModernUiArt.Border; line.raycastTarget = false;
            }
            U.At(divider, -270 + i * 270, 0, 1, 32); divider.SetSiblingIndex(1);
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            int slot = button.name == "ShopButton" ? 0 : button.name == "CurrentRoomStatus" ? 1 : button.name == "PlayCatRunnerButton" ? 3 : -1;
            if (slot < 0) continue;
            U.At((RectTransform)button.transform, -405 + slot * 270, 0, 248, 56);
            ModernUiArt.FlatNavigation(button);
            var well = Find(button.transform, "DockArtWell"); if (well != null) well.gameObject.SetActive(false);
            string icon = slot == 0 ? "SculptedShop" : slot == 1 ? "SculptedRooms" : "SculptedGames";
            var art = Find(button.transform, icon); if (art != null) U.At(art, -83, 0, 48, 48);
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name == "RunnerEnergyLabel")
                {
                    // The live energy summary remains accessible inside Games;
                    // it no longer competes with room navigation on the dock.
                    label.gameObject.SetActive(false); continue;
                }
                if (label.name == "RoomProgressLabel")
                {
                    U.At(label.rectTransform, 28, -13, 170, 26); label.fontSize = 22; label.enableAutoSizing = true;
                    label.fontSizeMin = 18; label.fontSizeMax = 22; label.color = ModernUiArt.Muted;
                    label.textWrappingMode = TextWrappingModes.NoWrap; label.alignment = TextAlignmentOptions.Center;
                }
                else { U.At(label.rectTransform, 23, 0, 162, 42); label.fontSize = 23; label.color = ModernUiArt.Ink; }
            }
            if (slot == 1)
            {
                var title = Find(button.transform, "ModernRoomTitle");
                if (title == null)
                {
                    var label = U.Label("ModernRoomTitle", button.targetGraphic.transform, PremiumTypography.Emphasis, 23,
                        ModernUiArt.Ink, 28, 10, 170, 30, TextAlignmentOptions.Center);
                    label.gameObject.AddComponent<BilingualCopyLabel>().Configure("Oda", "Room");
                    title=label.rectTransform;
                }
                U.At(title,28,12,170,28);
            }
        }
    }

    private static void Shop(Transform root)
    {
        var panel = Find(root, "Panel"); if (panel == null) return;
        NamedSurface(panel, "Surface", ModernUiArt.Paper, 28);
        var title = Find(panel,"Title");
        if (title != null)
        {
            U.At(title, -575, 378, 500, 64); Type(title, 42, ModernUiArt.Ink);
            var label = title.GetComponent<TMP_Text>(); label.margin = new Vector4(72, 0, 0, 0);
            var icon = Find(title, "SculptedShop"); if (icon != null)
            { icon.anchorMin = icon.anchorMax = new Vector2(0, .5f); icon.anchoredPosition = new Vector2(32, 0); icon.sizeDelta = new Vector2(58, 58); }
        }
        var subtitle = Find(panel,"Subtitle");
        if (subtitle != null) { U.At(subtitle, -460, 324, 720, 36); Type(subtitle, 22, ModernUiArt.Muted); }
        var firstTab = Find(panel,"Tab_CAT");
        // Older authoring passes place tabs inside Surface. A rail parented
        // beside that Surface draws over its entire subtree, including labels.
        // Keep decoration in the real tab sibling list, before every control.
        Transform tabParent = firstTab != null ? firstTab.parent : panel;
        var rail = Find(panel,"ModernTabRail");
        if (rail == null)
        {
            rail = U.Panel("ModernTabRail", tabParent, ModernUiArt.Inset, -170, 254, 1096, 76, 18).rectTransform;
        }
        if(rail.parent!=tabParent)rail.SetParent(tabParent,false);
        U.At(rail,-170,254,1096,76);rail.SetAsFirstSibling();
        var railGraphic=rail.GetComponent<LowPolyPanelGraphic>();
        if(railGraphic!=null){railGraphic.raycastTarget=false;ModernUiArt.Surface(railGraphic,ModernUiArt.Inset,18);}
        foreach (string name in new[] { "Tab_CAT", "Tab_ROOM", "Tab_HOME" })
        {
            var tab = Find(panel,name); if (tab == null) continue;
            tab.sizeDelta = new Vector2(344, 60);
            ModernUiArt.Action(tab.GetComponent<Button>(), name != "Tab_CAT");
        }
        var scroll = Find(panel, "ProductScroll");
        if (scroll != null)
        {
            scroll.anchoredPosition = new Vector2(0, -118);
            var grip = Find(scroll, "VerticalScrollbar");
            if (grip != null) { grip.sizeDelta = new Vector2(10, -10); Surface(grip, ModernUiArt.Inset, 5); }
            NamedSurface(scroll, "Handle", new Color32(135, 181, 231, 255), 4);
        }
    }

    private static void Product(RectTransform root)
    {
        var preview = Find(root, "Preview"); if (preview == null) return;
        NamedSurface(root, "Card", Color.white, 22);
        var stage = Find(root, "ProductArtStage");
        if (stage != null) { Row(stage, 81, 290, 10); Surface(stage, ModernUiArt.Paper, 18); }
        string id = root.name.Substring("Product_".Length);
        bool room = HomeStoreService.TryGetProduct(id, out var product) && product.StoreCategory == HomeStoreCategory.Home;
        // Photo size responds with the card width; the image keeps its source
        // aspect ratio through the existing 1:1 / 16:9 preview composition.
        Row(preview, 81, room ? 186 : 280, room ? 12 : 20);
        var photo = preview.GetComponentInChildren<RawImage>(true);
        if (photo != null && photo.transform != preview)
        {
            var aspect = photo.GetComponent<AspectRatioFitter>() ?? photo.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = room ? 16f / 9f : 1f;
            photo.uvRect = new Rect(0, 0, 1, 1);
        }
        var title = Find(root, "ProductTitle"); Row(title, -90, 56, 20); Type(title, 25, ModernUiArt.Ink);
        if (title != null)
        {
            var label = title.GetComponent<TMP_Text>(); label.enableAutoSizing = true; label.fontSizeMin = 22; label.fontSizeMax = 25;
            label.alignment = TextAlignmentOptions.Left;
        }
        Row(Find(root, "CurrencyPriceGroup"), -145, 46, 22);
        Row(Find(root, "SpecialPrice"), -145, 46, 22);
        var buy = Find(root, "BuyButton"); Row(buy, -202, 58, 18);
        if (buy != null) ModernUiArt.Action(buy.GetComponent<Button>());
        var badge = Find(root, "OwnedBadge");
        if (badge != null)
        {
            badge.anchorMin = badge.anchorMax = new Vector2(1, 1); badge.pivot = new Vector2(1, 1);
            badge.anchoredPosition = new Vector2(-18, -18); badge.sizeDelta = new Vector2(126, 32);
            Surface(badge, ModernUiArt.Mint, 10); TextColor(badge, ModernUiArt.Ink);
        }
    }

    private static void RoomCard(RectTransform root)
    {
        NamedSurface(root, "CardVisual", Color.white, 22);
        NamedSurface(root, "ActionFace", ModernUiArt.Azure, 14);
        var action = Find(root, "ActionText"); if (action != null) action.GetComponent<TMP_Text>().color = Color.white;
        var photo = Find(root, "PreviewWell"); if (photo != null) U.At(photo, 0, 41, 480, 270);
        Type(Find(root, "RoomTitle"), 25, ModernUiArt.Ink);
        Type(Find(root, "RoomStatus"), 18, ModernUiArt.Muted);
    }

    private static void GameCard(RectTransform root)
    {
        NamedSurface(root, "Visual", Color.white, 24);
        NamedSurface(root, "Action", ModernUiArt.Azure, 16);
        var action = Find(root, "Action"); if (action != null) TextColor(action, Color.white);
        var badge = Find(root, "ModeBadge"); if (badge != null) badge.gameObject.SetActive(false);
    }
}
