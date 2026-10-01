using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared authoring factory for the bright, rounded Cat Home candy language.
/// Builders call this instead of re-inventing flat buttons and currency glyphs.
/// </summary>
public static class PremiumUiFactory
{
    public static Sprite SolidSprite()
    {
        const string path = "Assets/UI/PremiumSolid.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        System.IO.Directory.CreateDirectory("Assets/UI");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); texture.Apply();
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public enum CurrencyVisual
    {
        Coin,
        Diamond
    }

    public const string CoinIconPath =
        "Assets/Art/PremiumCurrency/Icons/PawCoin_Icon.png";
    public const string DiamondIconPath =
        "Assets/Art/PremiumCurrency/Icons/DiamondGem_Icon.png";

    public static bool BuildCurrencyIcon(
        Transform parent,
        CurrencyVisual visual,
        bool animate = true)
    {
        string path = visual == CurrencyVisual.Coin ? CoinIconPath : DiamondIconPath;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null || parent == null)
            return false;

        GameObject iconObject = new GameObject(
            visual == CurrencyVisual.Coin ? "PremiumCoinSprite" : "PremiumDiamondSprite",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-2f, -2f);
        rect.offsetMax = new Vector2(2f, 2f);
        RawImage image = iconObject.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;

        if (animate)
        {
            PremiumAmbientSparkle motion = iconObject.AddComponent<PremiumAmbientSparkle>();
            motion.EditorConfigure(.7f, 0f, 0f, 0f);
        }
        return true;
    }

    public static void PolishHierarchy(Transform root, TMP_FontAsset font)
    {
        if (root == null)
            return;

        foreach (LowPolyPanelGraphic panel in
                 root.GetComponentsInChildren<LowPolyPanelGraphic>(true))
        {
            bool utilitySurface = IsUtilitySurface(panel);
            panel.ConfigureElevation(!utilitySurface && panel.GetComponent<Mask>() == null);
            panel.ConfigureReferenceFinish(!utilitySurface && panel.GetComponent<Mask>() == null);
            panel.ConfigureCandyPolish(
                utilitySurface ? 0f : 0.065f,
                utilitySurface ? 0f : 0.025f);
        }

        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (!IsVisualButton(button))
                continue;
            bool primary = IsPrimaryAction(button.name);
            PolishButton(button, primary, font);
        }

        foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            PolishText(label, font);

        PremiumReferenceArtBuilder.PolishScreen(root);
        JoyfulScreenBuilder.Polish(root);
        ModernScreenBuilder.Polish(root);
        PlayfulScreenBuilder.Polish(root);
        MiniGameNavigationBuilder.Apply(root);
        if (root.GetComponent<TitleScreen>() != null)
            StorybookTitleBuilder.Apply(root);
        if (root.GetComponent<CurrencyHudController>() != null || root.GetComponent<MainPanelController>() != null ||
            (root.name == "Canvas" && root.Find("FoodBar") != null))
            StorybookHudBuilder.Apply(root);

        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            string name = rect.name;
            if (name.IndexOf("Sparkle", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                name.IndexOf("Glint", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }
            if (rect.GetComponent<PremiumAmbientSparkle>() == null)
            {
                PremiumAmbientSparkle sparkle = rect.gameObject.AddComponent<PremiumAmbientSparkle>();
                sparkle.EditorConfigure(.6f, .025f, 1f, .3f);
            }
        }
    }

    public static void PolishButton(Button button, bool primary, TMP_FontAsset font)
    {
        if (button == null)
            return;
        if (!IsVisualButton(button))
        {
            PremiumButtonFx obsoleteFx = button.GetComponent<PremiumButtonFx>();
            if (obsoleteFx != null)
                Object.DestroyImmediate(obsoleteFx);
            return;
        }

        LowPolyPanelGraphic surface = button.targetGraphic as LowPolyPanelGraphic;
        if (surface == null)
            surface = button.GetComponent<LowPolyPanelGraphic>();
        if (surface == null)
            surface = button.GetComponentInChildren<LowPolyPanelGraphic>(true);
        if (surface != null)
            surface.ConfigureCandyPolish(primary ? 0.10f : 0.055f, primary ? 0.04f : 0.025f);

        PremiumButtonFx fx = button.GetComponent<PremiumButtonFx>();
        if (fx == null)
            fx = button.gameObject.AddComponent<PremiumButtonFx>();
        RectTransform visual = surface != null
            ? surface.rectTransform
            : button.transform as RectTransform;
        RectTransform buttonRect = button.transform as RectTransform;
        bool allowScale = visual != buttonRect || !IsLayoutDriven(buttonRect);
        fx.EditorConfigure(visual, surface, button, primary, allowScale);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(.88f, .88f, .88f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.55f, .58f, .64f, .78f);
        colors.fadeDuration = .06f;
        button.colors = colors;

        foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            PolishText(label, font);
            PremiumTypography.Apply(label, label.fontSize >= 40f);
        }
    }

    public static void PolishText(TMP_Text label, TMP_FontAsset font)
    {
        if (label == null)
            return;
        label.extraPadding = true;
        label.isTextObjectScaleStatic = true;
        PremiumTypography.Apply(label);
    }

    public static void ConfigureCurrencyImporters()
    {
        ConfigureCurrencyImporter(CoinIconPath);
        ConfigureCurrencyImporter(DiamondIconPath);
    }

    private static void ConfigureCurrencyImporter(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;
        bool dirty = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            dirty = true;
        }
        if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
        if (importer.alphaSource != TextureImporterAlphaSource.FromInput)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            dirty = true;
        }
        if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
        if (importer.streamingMipmaps) { importer.streamingMipmaps = false; dirty = true; }
        if (importer.isReadable) { importer.isReadable = false; dirty = true; }
        if (!importer.sRGBTexture) { importer.sRGBTexture = true; dirty = true; }
        if (importer.npotScale != TextureImporterNPOTScale.None)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            dirty = true;
        }
        if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
        if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
        if (importer.anisoLevel != 0) { importer.anisoLevel = 0; dirty = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }
        if (importer.maxTextureSize != 512) { importer.maxTextureSize = 512; dirty = true; }
        if (dirty)
            importer.SaveAndReimport();
    }

    private static bool IsUtilitySurface(LowPolyPanelGraphic panel)
    {
        string upper = panel.name.ToUpperInvariant();
        return panel.color.a < 0.35f ||
               upper.Contains("SHADOW") || upper.Contains("DEPTH") ||
               upper.Contains("SCRIM") || upper.Contains("BLOCKER") ||
               upper.Contains("MASK") || upper.Contains("DRAGSURFACE") ||
               upper.Contains("GLOW");
    }

    private static bool IsVisualButton(Button button)
    {
        if (button == null)
            return false;
        string upper = button.name.ToUpperInvariant();
        if (upper.Contains("SCRIM") || upper.Contains("BLOCKER") ||
            upper.Contains("DRAGSURFACE"))
        {
            return false;
        }
        return button.targetGraphic == null || button.targetGraphic.color.a > 0.01f;
    }

    private static bool IsLayoutDriven(RectTransform rect)
    {
        if (rect == null)
            return false;
        if (rect.GetComponent<LayoutElement>() != null ||
            rect.GetComponent<ContentSizeFitter>() != null)
        {
            return true;
        }
        return rect.parent != null && rect.parent.GetComponent<LayoutGroup>() != null;
    }

    private static bool IsPrimaryAction(string name)
    {
        string upper = name.ToUpperInvariant();
        // Keep hub card shimmer phases staggered but both primary so neither
        // card sits in the idle pause while the other is sweeping.
        return upper.Contains("BUY") || upper.Contains("START") ||
               upper.Contains("PLAY") || upper.Contains("CLAIM") ||
               upper.Contains("CONFIRM") || upper.Contains("COLLECT") ||
               upper.Contains("RUNNER") || upper.Contains("CATCH") ||
               upper.Contains("HUNT") || upper.Contains("GAMES") ||
               upper.Contains("SHOP") || upper.Contains("WELCOME") ||
               upper.Contains("HUB") || upper.Contains("CONTINUE");
    }
}
