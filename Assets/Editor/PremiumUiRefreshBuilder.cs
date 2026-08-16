using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Applies the shared premium treatment to legacy UI templates.</summary>
public static class PremiumUiRefreshBuilder
{
    private const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string RunnerScenePath = "Assets/Scenes/Runner/CatRunner.unity";

    [MenuItem("Tools/Cat Home/UI/Apply Premium UI Refresh")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Single);
        TMP_FontAsset premiumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        GameObject canvas = FindRoot(scene, "Canvas");
        if (canvas != null)
        {
            StyleNeedsHud(canvas.transform);
            StyleActionButton(canvas.transform.Find("ActionButton"), premiumFont);
            Transform activities = canvas.transform.Find("ActivityUIRoot");
            if (activities != null)
            {
                StyleActionButton(activities.Find("ActivityActionButton"), premiumFont);
                StyleActionButton(activities.Find("ActivityProgressBadge"), premiumFont);
            }

            Transform runner = canvas.transform.Find("RunnerLaunchUI");
            if (runner != null)
            {
                StyleActionButton(runner.Find("PlayCatRunnerButton"), premiumFont);
                StyleActionButton(runner.Find("RewardedEnergyButton"), premiumFont);
            }
        }


        if (premiumFont != null)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                ApplyPremiumFont(root.transform, premiumFont);
        }

        StyleNamedActionButtons(scene, premiumFont);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        ApplyToAuxiliaryScene(GameScenePath, premiumFont);
        ApplyToAuxiliaryScene(RunnerScenePath, premiumFont);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyToAuxiliaryScene(string path, TMP_FontAsset premiumFont)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (premiumFont != null)
                ApplyPremiumFont(root.transform, premiumFont);
        }
        StyleNamedActionButtons(scene, premiumFont);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void StyleNamedActionButtons(Scene scene, TMP_FontAsset premiumFont)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                string name = buttons[i].name;
                if (name == "ActionButton" || name == "ActivityActionButton" ||
                    name == "PlayCatRunnerButton" || name == "RewardedEnergyButton" ||
                    name == "ButtonRoot" || name == "ClaimButton" ||
                    name == "ConfirmPlacement" || name == "CancelPlacement" ||
                    name == "RotateLeft" || name == "RotateRight" ||
                    name == "CollectButton" || name == "RetryButton" ||
                    name == "CloseButton" ||
                    name == "LetsPlayButton" || name == "Confirm")
                {
                    StyleActionButton(buttons[i].transform, premiumFont);
                }
            }
        }
    }

    private static void ApplyPremiumFont(Transform root, TMP_FontAsset font)
    {
        // The shared factory preserves authored font weight and semantic text
        // hierarchy while adding the common candy polish to panels, glints and
        // controls. The previous global Regular assignment made every heading,
        // amount and CTA read at the same visual weight.
        PremiumUiFactory.PolishHierarchy(root, font);

        // Full-screen click catchers are navigation plumbing rather than visual
        // controls. They must not pulse or scale when the pointer enters them.
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            string name = button.name.ToUpperInvariant();
            bool invisibleTarget = button.targetGraphic != null &&
                                   button.targetGraphic.color.a <= 0.01f;
            bool nonVisualCatcher = name.Contains("SCRIM") ||
                                    name.Contains("BLOCKER") ||
                                    name.Contains("DRAGSURFACE") ||
                                    invisibleTarget;
            if (!nonVisualCatcher)
                continue;
            PremiumButtonFx fx = button.GetComponent<PremiumButtonFx>();
            if (fx != null)
                Object.DestroyImmediate(fx);
        }
    }

    // Same 68-unit top-bar row as CurrencyHudBuilder / MainPanelBuilder /
    // TopHudResponsiveLayout. Authoring at 64px and stretching at runtime left
    // the 58px icon discs floating above the taller capsule.
    private const float TopBarHeight = TopHudResponsiveLayout.TopBarHeight;
    private const float TopBarCenterY = TopHudResponsiveLayout.TopBarCenterY;
    private const float NeedBarWidth = TopHudResponsiveLayout.NeedBarWidth;
    private const float NeedSpacing = TopHudResponsiveLayout.NeedSpacing;
    private const float NeedCapsuleLeft = 0.10f;
    private const float NeedIconX = -118f;
    private const float NeedDiscSize = 64f;
    private const float NeedDiscShadowSize = 68f;
    private const float NeedGlyphSize = 42f;
    private const float NeedContentX = 42f;
    private const float NeedContentWidth = 214f;
    private const float NeedLabelY = 13f;
    private const float NeedTrackY = -14f;
    private const float NeedTrackHeight = 18f;

    private static void StyleNeedsHud(Transform canvas)
    {
        Transform needsRoot = canvas.Find("NeedsPresentationRoot");
        if (needsRoot == null)
            needsRoot = canvas;
        else
        {
            // Tutorial may lift this root at runtime. Authoring must stay on
            // the shared top-bar centre so Edit Mode Game view matches Play.
            RectTransform needsRootRect = needsRoot as RectTransform;
            needsRootRect.anchorMin = Vector2.zero;
            needsRootRect.anchorMax = Vector2.one;
            needsRootRect.pivot = new Vector2(0.5f, 0.5f);
            needsRootRect.anchoredPosition = Vector2.zero;
            needsRootRect.offsetMin = Vector2.zero;
            needsRootRect.offsetMax = Vector2.zero;
            needsRootRect.localScale = Vector3.one;
        }

        Transform hunger = needsRoot.Find("FoodBar");
        Transform thirst = needsRoot.Find("ThirstUI");
        Transform energy = needsRoot.Find("EnergyUI");
        PositionNeedBar(hunger, -NeedSpacing);
        PositionNeedBar(thirst, 0f);
        PositionNeedBar(energy, NeedSpacing);
        StyleNeedBar(hunger, PremiumUiStyle.Coral);
        StyleNeedBar(thirst, PremiumUiStyle.Teal);
        StyleNeedBar(energy, PremiumUiStyle.Plum);

    }

    private static void PositionNeedBar(Transform root, float x)
    {
        RectTransform rect = root as RectTransform;
        if (rect == null)
            return;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, TopBarCenterY);
        rect.sizeDelta = new Vector2(NeedBarWidth, TopBarHeight);
    }

    private static void StyleNeedBar(Transform root, Color fillColor)
    {
        if (root == null)
            return;

        RectTransform rootRect = root as RectTransform;
        if (rootRect != null)
            rootRect.sizeDelta = new Vector2(NeedBarWidth, TopBarHeight);

        Transform backgroundTransform = root.Find("Background");
        Image background = backgroundTransform != null ? backgroundTransform.GetComponent<Image>() : null;
        if (backgroundTransform != null)
        {
            if (background != null)
                Object.DestroyImmediate(background);

            Transform obsoleteDepth = root.Find("CapsuleDepth");
            if (obsoleteDepth != null)
                Object.DestroyImmediate(obsoleteDepth.gameObject);
            Transform obsoleteRim = root.Find("CapsuleRim");
            if (obsoleteRim != null)
                Object.DestroyImmediate(obsoleteRim.gameObject);

            Shadow[] legacyEffects = backgroundTransform.GetComponents<Shadow>();
            for (int i = 0; i < legacyEffects.Length; i++)
                Object.DestroyImmediate(legacyEffects[i]);

            LowPolyPanelGraphic depth = EnsureNeedSurface(root, "BackgroundShadow");
            PremiumUiStyle.ConfigureShadowSurface(
                depth, new Color32(68, 31, 96, 132), TopBarHeight * 0.5f);
            depth.raycastTarget = false;
            SetNeedCapsuleRect(depth.rectTransform, 4f);
            depth.rectTransform.SetSiblingIndex(0);

            LowPolyPanelGraphic capsule = backgroundTransform.GetComponent<LowPolyPanelGraphic>();
            if (capsule == null)
                capsule = backgroundTransform.gameObject.AddComponent<LowPolyPanelGraphic>();
            Color capsuleTop = Color.Lerp(fillColor, Color.white, 0.28f);
            Color capsuleBottom = Color.Lerp(fillColor, PremiumUiStyle.DeepInset, 0.16f);
            capsule.ConfigurePremiumStyle(
                capsuleTop,
                capsuleBottom,
                TopBarHeight * 0.5f,
                3.8f,
                new Color32(255, 255, 255, 195),
                new Color32(65, 30, 92, 120),
                new Color32(255, 213, 237, 68));
            capsule.ConfigureCandyPolish(0.31f, 0.13f);
            capsule.raycastTarget = false;
            RectTransform capsuleRect = backgroundTransform as RectTransform;
            SetNeedCapsuleRect(capsuleRect, 0f);
            capsuleRect.SetSiblingIndex(1);

        }

        Transform frameTransform = root.Find("Frame");
        if (frameTransform != null)
            frameTransform.gameObject.SetActive(false);

        Transform fillAreaTransform = root.Find("Fill Area");
        if (fillAreaTransform == null)
            fillAreaTransform = root.Find("FillTrackMask/Fill Area");
        Transform fillTransform = fillAreaTransform != null
            ? fillAreaTransform.Find("Fill")
            : null;
        Image fill = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
        if (fill != null)
            fill.color = fillColor;

        LowPolyPanelGraphic trackRim = EnsureNeedSurface(root, "FillTrackRim");
        PremiumUiStyle.ConfigureMetalSurface(
            trackRim, PremiumUiStyle.CandyLemon, 12f, 3f);
        trackRim.raycastTarget = false;
        SetNeedContentRect(
            trackRim.rectTransform, NeedContentX, NeedTrackY, NeedContentWidth, NeedTrackHeight);

        LowPolyPanelGraphic trackMask = EnsureNeedSurface(root, "FillTrackMask");
        trackMask.ConfigurePremiumStyle(
            PremiumUiStyle.DeepInsetLift,
            PremiumUiStyle.DeepInset,
            10f,
            1.5f,
            new Color32(255, 255, 255, 45),
            new Color32(20, 14, 49, 150),
            Color.clear);
        trackMask.ConfigureCandyPolish(0.06f, 0.02f);
        trackMask.raycastTarget = false;
        SetNeedContentRect(
            trackMask.rectTransform,
            NeedContentX,
            NeedTrackY,
            NeedContentWidth - 4f,
            NeedTrackHeight - 4f);
        Mask mask = trackMask.GetComponent<Mask>();
        if (mask == null)
            mask = trackMask.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        RectTransform fillArea = fillAreaTransform as RectTransform;
        if (fillArea != null)
        {
            fillArea.SetParent(trackMask.transform, false);
            fillArea.anchorMin = Vector2.zero;
            fillArea.anchorMax = Vector2.one;
            fillArea.offsetMin = Vector2.zero;
            fillArea.offsetMax = Vector2.zero;
        }
        trackRim.rectTransform.SetSiblingIndex(2);
        trackMask.rectTransform.SetSiblingIndex(3);

        Transform icon = null;
        foreach (Transform child in root)
            if (child.name.EndsWith("Icon")) { icon = child; break; }
        if (icon != null)
        {
            RectTransform iconRect = icon as RectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(NeedGlyphSize, NeedGlyphSize);
            iconRect.anchoredPosition = new Vector2(NeedIconX, 0f);
            Image iconImage = icon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
            }
            Transform oldDiscShadow = root.Find("IconDiscShadow");
            if (oldDiscShadow != null)
                Object.DestroyImmediate(oldDiscShadow.gameObject);
            Transform oldDisc = root.Find("IconDisc");
            if (oldDisc != null)
                Object.DestroyImmediate(oldDisc.gameObject);

            LowPolyPanelGraphic discShadow = EnsureNeedSurface(root, "IconDiscShadow");
            PremiumUiStyle.ConfigureShadowSurface(
                discShadow, new Color32(68, 31, 96, 145), NeedDiscShadowSize * 0.5f);
            RectTransform discShadowRect = discShadow.rectTransform;
            discShadowRect.anchorMin = discShadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            discShadowRect.pivot = new Vector2(0.5f, 0.5f);
            discShadowRect.sizeDelta = new Vector2(NeedDiscShadowSize, NeedDiscShadowSize);
            discShadowRect.anchoredPosition = new Vector2(NeedIconX, 0f);
            discShadow.raycastTarget = false;

            GameObject discObject = new GameObject("IconDisc", typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform discRect = discObject.GetComponent<RectTransform>();
            discRect.SetParent(root, false);
            discRect.anchorMin = discRect.anchorMax = new Vector2(0.5f, 0.5f);
            discRect.pivot = new Vector2(0.5f, 0.5f);
            discRect.sizeDelta = new Vector2(NeedDiscSize, NeedDiscSize);
            discRect.anchoredPosition = new Vector2(NeedIconX, 0f);
            LowPolyPanelGraphic disc = discObject.AddComponent<LowPolyPanelGraphic>();
            disc.ConfigurePremiumStyle(
                Color.Lerp(fillColor, Color.white, 0.34f),
                Color.Lerp(fillColor, PremiumUiStyle.DeepInset, 0.13f),
                NeedDiscSize * 0.5f,
                3.4f,
                new Color32(255, 255, 255, 210),
                new Color32(65, 30, 92, 125),
                new Color32(255, 225, 239, 65));
            disc.ConfigureCandyPolish(0.34f, 0.14f);
            disc.raycastTarget = false;
            GameObject glintObject = new GameObject(
                "PremiumGlint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform glintRect = glintObject.GetComponent<RectTransform>();
            glintRect.SetParent(discObject.transform, false);
            glintRect.anchorMin = glintRect.anchorMax = new Vector2(0.5f, 0.5f);
            glintRect.pivot = new Vector2(0.5f, 0.5f);
            glintRect.sizeDelta = new Vector2(7f, 7f);
            glintRect.anchoredPosition = new Vector2(16f, 15f);
            glintRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image glint = glintObject.GetComponent<Image>();
            glint.color = new Color32(255, 255, 255, 220);
            glint.raycastTarget = false;
            discShadowRect.SetSiblingIndex(4);
            discRect.SetSiblingIndex(5);
            icon.SetAsLastSibling();
        }

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].color = labels[i].name.Contains("Shadow")
                ? PremiumUiStyle.Shadow
                : PremiumUiStyle.Ivory;
            labels[i].characterSpacing = 0.5f;
            if (labels[i].name.EndsWith("LabelFront"))
            {
                labels[i].fontSize = 16f;
                labels[i].alignment = TextAlignmentOptions.MidlineLeft;
                SetNeedContentRect(
                    labels[i].rectTransform, NeedContentX, NeedLabelY, NeedContentWidth, 22f);
            }
            else if (labels[i].name.EndsWith("LabelShadow"))
            {
                labels[i].gameObject.SetActive(false);
            }
            else if (labels[i].name == "PercentageText")
            {
                labels[i].fontSize = 14f;
                labels[i].alignment = TextAlignmentOptions.Center;
                SetNeedContentRect(
                    labels[i].rectTransform,
                    NeedContentX,
                    NeedTrackY,
                    NeedContentWidth - 4f,
                    NeedTrackHeight - 4f);
                labels[i].transform.SetAsLastSibling();
            }
        }
    }

    private static LowPolyPanelGraphic EnsureNeedSurface(
        Transform root,
        string name)
    {
        Transform existing = root.Find(name);
        GameObject surfaceObject;
        if (existing != null)
        {
            surfaceObject = existing.gameObject;
        }
        else
        {
            surfaceObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer));
            surfaceObject.transform.SetParent(root, false);
        }

        Image oldImage = surfaceObject.GetComponent<Image>();
        if (oldImage != null)
            Object.DestroyImmediate(oldImage);
        LowPolyPanelGraphic surface = surfaceObject.GetComponent<LowPolyPanelGraphic>();
        if (surface == null)
            surface = surfaceObject.AddComponent<LowPolyPanelGraphic>();
        return surface;
    }

    private static void SetNeedCapsuleRect(RectTransform rect, float expansion)
    {
        float pad = Mathf.Max(0f, expansion);
        rect.anchorMin = new Vector2(NeedCapsuleLeft, 0f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(-pad, -pad);
        rect.offsetMax = new Vector2(pad, pad);
    }

    private static void SetNeedContentRect(
        RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void StyleActionButton(Transform target, TMP_FontAsset premiumFont)
    {
        if (target == null)
            return;

        Button button = target.GetComponent<Button>();
        if (button == null)
            button = target.GetComponentInChildren<Button>(true);
        if (button == null)
        {
            PremiumUiFactory.PolishHierarchy(target, premiumFont);
            return;
        }

        // Preserve the authored colour layers and target graphic. The factory
        // adds internal gloss plus PremiumButtonFx without replacing a sprite,
        // depth child or carefully composed button face.
        bool secondaryAction = target.name == "RewardedEnergyButton" ||
                               target.name == "RetryButton" ||
                               target.name == "RotateLeft" ||
                               target.name == "RotateRight" ||
                               target.name == "CancelPlacement" ||
                               target.name == "CloseButton";
        PremiumUiFactory.PolishButton(button, !secondaryAction, premiumFont);

        TMP_Text[] labels = target.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            bool isShadow = labels[i].name.ToUpperInvariant().Contains("SHADOW");
            labels[i].gameObject.SetActive(!isShadow);
            if (!isShadow)
                PremiumUiFactory.PolishText(labels[i], premiumFont);
        }
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }
}
