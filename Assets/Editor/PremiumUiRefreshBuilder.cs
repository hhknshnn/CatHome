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
            StyleJoystick(canvas.transform);
            var groups=new System.Collections.Generic.List<CanvasGroup>();
            foreach(var rect in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if(rect.name!="FoodBar"&&rect.name!="ThirstUI"&&rect.name!="EnergyUI"&&rect.GetComponent<MobileJoystick>()==null)continue;
                var group=rect.GetComponent<CanvasGroup>();if(group==null)group=rect.gameObject.AddComponent<CanvasGroup>();groups.Add(group);
            }
            var visibility=canvas.GetComponent<HomeHudVisibility>()??canvas.AddComponent<HomeHudVisibility>();visibility.Configure(groups.ToArray());
            var notice=canvas.transform.Find("HomeRewardToast");
            if(notice==null)notice=PremiumUiElements.Rect("HomeRewardToast",canvas.transform);
            PremiumUiElements.Fill((RectTransform)notice);
            var toast=notice.GetComponent<HomeRewardToast>()??notice.gameObject.AddComponent<HomeRewardToast>();
            toast.EditorConfigure(premiumFont);
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

    private static void StyleJoystick(Transform canvas)
    {
        var joystick=canvas.GetComponentInChildren<MobileJoystick>(true);
        if(joystick==null)return;
        var serialized=new SerializedObject(joystick);
        var handle=serialized.FindProperty("handle").objectReferenceValue as RectTransform;
        var background=joystick.GetComponent<Image>();if(background!=null)background.color=Color.clear;
        var old=joystick.transform.Find("PremiumBase");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var face=PremiumUiElements.Panel("PremiumBase",joystick.transform,PremiumUiStyle.Ivory,0,0,172,172,86);
        face.transform.SetAsFirstSibling();face.raycastTarget=false;
        if(handle!=null)
        {
            handle.sizeDelta=new Vector2(70,70);
            var image=handle.GetComponent<Image>();if(image!=null)image.color=Color.clear;
            old=handle.Find("PremiumHandle");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var thumb=PremiumUiElements.Panel("PremiumHandle",handle,PremiumUiStyle.Teal,0,0,70,70,35);thumb.raycastTarget=false;
        }
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
        if (root == null) return;
        var rect = (RectTransform)root; rect.sizeDelta = new Vector2(NeedBarWidth, TopBarHeight);
        // The value/fill objects are owned by the existing needs components.
        // Reuse them so presentation changes never detach live hunger/thirst.
        var background = EnsureNeedSurface(root, "Background");
        PremiumUiStyle.ConfigureLightSurface(background, 23f, 2f);
        background.raycastTarget = false; PremiumUiElements.Fill(background.rectTransform);
        background.rectTransform.SetAsFirstSibling();
        foreach (string name in new[] { "BackgroundShadow", "CapsuleDepth", "CapsuleRim", "IconDiscShadow", "IconDisc", "Frame", "FillTrackRim" })
        {
            var old = root.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
        }
        var area = root.Find("Fill Area") ?? root.Find("FillTrackMask/Fill Area");
        var track = EnsureNeedSurface(root, "FillTrackMask");
        PremiumUiStyle.ConfigureAccentSurface(track, PremiumUiStyle.Mint, PremiumUiStyle.Mint, 5f, 0f);
        track.raycastTarget = false;
        SetNeedContentRect(track.rectTransform, 25f, -17f, 166f, 9f);
        var mask = track.GetComponent<Mask>() ?? track.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = true;
        if (area != null)
        {
            area.SetParent(track.transform, false); PremiumUiElements.Fill((RectTransform)area);
            var fill = area.Find("Fill");
            if (fill != null && fill.GetComponent<Image>() != null)
            {
                var image = fill.GetComponent<Image>(); image.color = fillColor;
                image.sprite = PremiumUiFactory.SolidSprite(); image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0;
                PremiumUiElements.Fill(image.rectTransform);
            }
        }
        foreach (Transform child in root)
        {
            if (!child.name.EndsWith("Icon")) continue;
            SetNeedContentRect((RectTransform)child, -91f, 0f, 34f, 34f);
            var image = child.GetComponent<Image>(); if (image != null) { image.preserveAspect = true; image.raycastTarget = false; }
            child.SetAsLastSibling(); break;
        }
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            label.color = PremiumUiStyle.Ink; label.characterSpacing = .3f;
            if (label.name.Contains("Shadow")) { label.gameObject.SetActive(false); continue; }
            if (label.name.EndsWith("LabelFront"))
            {
                label.fontSize = 19; label.alignment = TextAlignmentOptions.Left;
                SetNeedContentRect(label.rectTransform, 2f, 10f, 116f, 28f);
                PremiumUiElements.Localize(label, root.name == "FoodBar" ? "home.hunger" : root.name == "ThirstUI" ? "home.thirst" : "home.energy");
            }
            else if (label.name == "PercentageText")
            {
                label.fontSize = 19; label.alignment = TextAlignmentOptions.Right;
                SetNeedContentRect(label.rectTransform, 83f, 10f, 58f, 28f);
            }
            label.transform.SetAsLastSibling();
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

        if(target.name=="ActionButton" || target.name=="ActivityActionButton" || target.name=="ActivityProgressBadge")
        { StyleContextAction(target,premiumFont); return; }
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

    private static void StyleContextAction(Transform root,TMP_FontAsset font)
    {
        bool progress=root.name=="ActivityProgressBadge";
        foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))
            if(!(graphic is TMP_Text))graphic.enabled=false;
        foreach(var fx in root.GetComponentsInChildren<Shadow>(true))Object.DestroyImmediate(fx);
        foreach(var press in root.GetComponents<LowPolyButtonPress>())Object.DestroyImmediate(press);
        var old=root.Find("ContextFace");
        var surface=old!=null?old.GetComponent<LowPolyPanelGraphic>():PremiumUiElements.Panel("ContextFace",root,progress?PremiumUiStyle.Mint:PremiumUiStyle.Coral,0,0,1,1,24,!progress);
        surface.enabled=true;surface.SetPremiumBaseColor(progress?PremiumUiStyle.Mint:PremiumUiStyle.Coral);
        var bounds=(RectTransform)root;bounds.anchorMin=bounds.anchorMax=new Vector2(progress?.5f:1f,0f);bounds.pivot=new Vector2(.5f,.5f);
        bounds.sizeDelta=progress?new Vector2(420,66):new Vector2(280,94);bounds.anchoredPosition=progress?new Vector2(0,190):new Vector2(-190,204);
        PremiumUiElements.Fill(surface.rectTransform);
        foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if(label.name.ToUpperInvariant().Contains("SHADOW")){label.gameObject.SetActive(false);continue;}
            label.transform.SetParent(surface.transform,false);PremiumUiElements.Fill(label.rectTransform,12);
            label.font=font;label.fontSize=26;label.enableAutoSizing=true;label.fontSizeMin=20;label.fontSizeMax=26;
            label.color=PremiumUiStyle.Ink;label.characterSpacing=.5f;label.alignment=TextAlignmentOptions.Center;
        }
        var button=root.GetComponent<Button>();
        if(button!=null){button.targetGraphic=surface;PremiumUiFactory.PolishButton(button,true,font);}
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name)
                return root;
        return null;
    }
}
