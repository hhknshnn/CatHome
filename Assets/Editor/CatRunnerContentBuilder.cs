using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CatRunnerContentBuilder
{
    private sealed class RunnerUiExtras
    {
        public RectTransform SafeArea;
        public RectTransform TopHud;
        public RectTransform WelcomeCard;
        public RectTransform ResultsCard;
        public RectTransform PauseCard;
        public RectTransform TutorialCard;
        public TMP_Text Score;
        public TMP_Text Combo;
        public TMP_Text PowerUps;
        public Button Pause;
        public TMP_Text WelcomeMissions;
        public Button WelcomeRewarded;
        public TMP_Text ResultMissions;
        public GameObject NewBestBadge;
        public GameObject PausePanel;
        public Button Resume;
        public Button PauseExit;
        public Button ReducedMotion;
        public Button Sound;
        public Button Haptics;
        public GameObject TutorialPanel;
        public TMP_Text TutorialText;
        public Button TutorialSkip;
    }

    public const string RunnerScenePath = "Assets/Scenes/Runner/CatRunner.unity";
    private const string CatPrefabPath =
        "Assets/PolyOne/Cartoon Dog, Cat/Prefab/SM_CartoonAnimal_Cat.prefab";
    private const string RunnerMaterialFolder = "Assets/Art/Runner/Materials";
    private const string PawCoinModelPath =
        "Assets/Art/PremiumCurrency/PawCoin.fbx";
    private const string RunnerVolumeProfilePath =
        "Assets/Art/Runner/CatRunner_VolumeProfile.asset";
    private const string RunnerHeroPath =
        "Assets/Art/Runner/UI/CatRunnerHero_v1.png";
    private const string PetshopPrefabFolder =
        "Assets/Bublisher/Small Kit 3D Stylized Petshop Asset/Asset/Prefabs/";
    private const string LivingRoomPrefabFolder =
        "Assets/LowPolyLivingRoomPack/Prefabs/";

    private static readonly Color32 Cream = new Color32(255, 239, 202, 255);
    private static readonly Color32 Dark = new Color32(64, 50, 64, 255);
    private static readonly Color32 Teal = new Color32(47, 151, 149, 255);
    private static readonly Color32 TealDark = new Color32(31, 100, 108, 255);
    private static readonly Color32 Orange = new Color32(238, 126, 48, 255);
    private static readonly Color32 Gold = new Color32(255, 196, 42, 255);
    private static readonly Color32 Wood = new Color32(151, 91, 47, 255);
    private static readonly Color32 Wall = new Color32(111, 126, 130, 255);

    [MenuItem("Tools/Cat Home/Runner/Build Cat Runner Prototype")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Cat Runner Prototype",
                "Build the additive Cat Runner graybox and add the PLAY button?",
                "Build",
                "Cancel"))
        {
            return;
        }

        BuildSilently();
    }

    public static string BuildSilently()
    {
        EnsureFolders();
        BuildLaunchButton();
        BuildRunnerScene();
        AddRunnerToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(SceneArchitectureBuilder.BootstrapScenePath, OpenSceneMode.Single);
        return "cat-runner-prototype-built";
    }

    private static void BuildLaunchButton()
    {
        Scene scene = EditorSceneManager.OpenScene(SceneArchitectureBuilder.UiScenePath, OpenSceneMode.Single);
        Canvas canvas = FindRootComponent<Canvas>(scene, "Canvas");
        if (canvas == null)
            throw new InvalidOperationException("Shared UI scene has no root-level Canvas.");

        Transform old = canvas.transform.Find("RunnerLaunchUI");
        RemoveRogueLaunchRoots(scene, old != null ? old.gameObject : null);
        if (old != null)
            UnityEngine.Object.DestroyImmediate(old.gameObject);

        GameObject root = CreateUiObject("RunnerLaunchUI", canvas.transform);
        Stretch(root.GetComponent<RectTransform>());
        CanvasGroup group = root.AddComponent<CanvasGroup>();

        GameObject dock = CreateUiObject("HomeDock", root.transform);
        LowPolyPanelGraphic dockGraphic = dock.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(
            dockGraphic, PremiumUiStyle.NavyLift, PremiumUiStyle.Night, 44f, 5f);
        SetAnchored(dock.GetComponent<RectTransform>(), new Vector2(.5f, 0f),
            new Vector2(0f, 76f), new Vector2(790f, 106f));

        GameObject dockRimObject = CreateUiObject("DockRim", dock.transform);
        LowPolyPanelGraphic dockRim = dockRimObject.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(
            dockRim, PremiumUiStyle.ChampagneLight, PremiumUiStyle.Champagne, 40f, 2f);
        dockRim.raycastTarget = false;
        StretchWithOffsets(dockRim.rectTransform, 3f, 3f, -3f, -3f);
        GameObject dockInnerObject = CreateUiObject("DockInner", dock.transform);
        LowPolyPanelGraphic dockInner = dockInnerObject.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(
            dockInner, PremiumUiStyle.NavyLift, PremiumUiStyle.Navy, 38f, 3f);
        dockInner.raycastTarget = false;
        StretchWithOffsets(dockInner.rectTransform, 7f, 7f, -7f, -7f);

        Button rooms = CreateButton(
            dock.transform, "ShopButton", "SHOP", PremiumUiStyle.Teal, new Vector2(230f, 72f));
        SetAnchored(rooms.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            new Vector2(-260f, 0f), new Vector2(230f, 72f));

        Button button = CreateButton(
            dock.transform,
            "PlayCatRunnerButton",
            "GAMES",
            Orange,
            new Vector2(250f, 72f));
        SetAnchored(button.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            new Vector2(255f, 0f), new Vector2(250f, 72f));

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        label.fontSize = 31f;
        label.fontStyle = FontStyles.Bold;

        GameObject statusPanel = CreateUiObject("CurrentRoomStatus", dock.transform);
        LowPolyPanelGraphic statusGraphic = statusPanel.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(
            statusGraphic, PremiumUiStyle.Night, PremiumUiStyle.Navy, 28f, 2f);
        SetAnchored(
            statusPanel.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(255f, 72f));
        TMP_Text roomProgress = CreateLabel(
            statusPanel.transform, "RoomProgressLabel",
            "LIVING ROOM  •  0/" + HomeStoreService.LivingRoomItemCount, 19f,
            PremiumUiStyle.ChampagneLight);
        roomProgress.rectTransform.anchorMin = new Vector2(0f, .48f);
        roomProgress.rectTransform.anchorMax = new Vector2(1f, 1f);
        roomProgress.rectTransform.offsetMin = Vector2.zero;
        roomProgress.rectTransform.offsetMax = Vector2.zero;
        roomProgress.alignment = TextAlignmentOptions.Center;
        roomProgress.fontStyle = FontStyles.Bold;
        TMP_Text energy = CreateLabel(
            statusPanel.transform,
            "RunnerEnergyLabel",
            "RUN 5/5  •  CATCH 5/5",
            17f,
            PremiumUiStyle.TealLift);
        energy.rectTransform.anchorMin = new Vector2(0f, 0f);
        energy.rectTransform.anchorMax = new Vector2(1f, .52f);
        energy.rectTransform.offsetMin = new Vector2(12f, 6f);
        energy.rectTransform.offsetMax = new Vector2(-12f, 0f);
        energy.alignment = TextAlignmentOptions.Center;
        energy.fontStyle = FontStyles.Bold;
        // The capsule is fixed width but the text is not: the unlimited Catch
        // entitlement prints a whole word where a count usually sits, and at a
        // fixed size that wrapped out of the capsule and over the dock.
        energy.textWrappingMode = TextWrappingModes.NoWrap;
        energy.enableAutoSizing = true;
        energy.fontSizeMin = 11f;
        energy.fontSizeMax = 17f;

        Button rewardedAd = CreateButton(
            root.transform,
            "RewardedEnergyButton",
            "ENERGY +2",
            Teal,
            new Vector2(176f, 42f));
        SetAnchored(
            rewardedAd.GetComponent<RectTransform>(),
            new Vector2(.5f, 0f),
            new Vector2(0f, 151f),
            new Vector2(176f, 42f));
        rewardedAd.GetComponentInChildren<TMP_Text>(true).fontSize = 17f;

        CatRunnerLauncher launcher = root.AddComponent<CatRunnerLauncher>();
        CatCatchLauncher catchLauncher = root.AddComponent<CatCatchLauncher>();
        GamesHubPanel hub = BuildGamesHub(root.transform, launcher, catchLauncher);
        launcher.EditorConfigure(button, rooms, group, energy, roomProgress, rewardedAd, hub);

        TMP_FontAsset premiumFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        PremiumUiFactory.PolishHierarchy(root.transform, premiumFont);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GamesHubPanel BuildGamesHub(
        Transform parent,
        CatRunnerLauncher runnerLauncher,
        CatCatchLauncher catchLauncher)
    {
        GameObject hubRoot = CreateUiObject("GamesHubPanel", parent);
        Stretch(hubRoot.GetComponent<RectTransform>());
        CanvasGroup hubGroup = hubRoot.AddComponent<CanvasGroup>();
        Image scrim = hubRoot.AddComponent<Image>();
        scrim.color = new Color32(44, 27, 67, 160);
        Button closeScrim = hubRoot.AddComponent<Button>();
        closeScrim.targetGraphic = scrim;
        closeScrim.transition = Selectable.Transition.None;

        Color32 headerPink = new Color32(255, 101, 154, 255);

        // Gold glow rim so the panel reads as the same premium class as the game
        // welcome cards, not a plain cream slab.
        GameObject glow = CreatePremiumPanel(
            hubRoot.transform, "GamesHubGlow", new Color32(255, 221, 86, 220), 50f);
        SetAnchored(glow.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(1044f, 724f));
        GameObject cardShadow = CreatePremiumPanel(
            hubRoot.transform, "GamesHubCardShadow", new Color32(126, 92, 176, 255), 46f);
        SetAnchored(cardShadow.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(1020f, 700f));
        GameObject card = CreatePremiumPanel(
            hubRoot.transform, "GamesHubCard", new Color32(255, 242, 211, 255), 46f);
        SetAnchored(card.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(1000f, 680f));

        // Pink header band carrying the eyebrow + title, matching the two welcome
        // cards so the whole Games flow shares one identity.
        GameObject header = CreatePremiumPanel(card.transform, "GamesHeader", headerPink, 38f);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, 140f);
        headerRect.anchoredPosition = Vector2.zero;

        TMP_Text eyebrow = CreateLabel(header.transform, "GamesEyebrow", "ARCADE CORNER", 18f, Cream);
        SetAnchored(eyebrow.rectTransform, new Vector2(.5f, 1f),
            new Vector2(0f, -30f), new Vector2(520f, 26f));
        eyebrow.alignment = TextAlignmentOptions.Center;
        eyebrow.characterSpacing = 8f;
        eyebrow.fontStyle = FontStyles.Bold;

        TMP_Text title = CreateLabel(header.transform, "GamesTitle", "GAMES", 52f, Cream);
        SetAnchored(title.rectTransform, new Vector2(.5f, 1f),
            new Vector2(0f, -84f), new Vector2(400f, 64f));
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;

        TMP_Text subtitle = CreateLabel(card.transform, "GamesSubtitle",
            "EACH GAME KEEPS ITS OWN 5 LIVES", 19f, TealDark);
        SetAnchored(subtitle.rectTransform, new Vector2(.5f, 1f),
            new Vector2(0f, -170f), new Vector2(720f, 30f));
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.fontStyle = FontStyles.Bold;

        // Corner sparkles for a little candy polish.
        AddHubSparkle(card.transform, "SparkleLeft", new Vector2(-430f, 150f), 26f, headerPink);
        AddHubSparkle(card.transform, "SparkleRight", new Vector2(430f, 150f), 26f, new Color32(91, 210, 191, 255));
        AddHubSparkle(card.transform, "SparkleLower", new Vector2(-452f, -250f), 20f, Gold);

        Button runnerCard = BuildGameCard(
            card.transform, "HubCatRunnerButton", "CAT RUNNER", "ENDLESS DASH",
            "RUN", Orange, new Vector2(-214f, -30f), out TMP_Text runnerLives);
        Button catchCard = BuildGameCard(
            card.transform, "HubCatCatchButton", "CAT CATCH", "60s MOUSE HUNT",
            "HUNT", Teal, new Vector2(214f, -30f), out TMP_Text catchLives);

        TMP_Text hubHint = CreateLabel(card.transform, "GamesHubHint",
            "TAP A GAME TO START PLAYING", 16f, TealDark);
        SetAnchored(hubHint.rectTransform, new Vector2(.5f, 0f),
            new Vector2(0f, 132f), new Vector2(720f, 26f));
        hubHint.alignment = TextAlignmentOptions.Center;
        hubHint.alpha = 0.85f;
        hubHint.fontStyle = FontStyles.Bold;

        Button close = CreateButton(
            card.transform, "GamesHubClose", "CLOSE", new Color32(188, 143, 235, 255),
            new Vector2(260f, 66f));
        SetAnchored(close.GetComponent<RectTransform>(), new Vector2(.5f, 0f),
            new Vector2(0f, 44f), new Vector2(260f, 66f));
        closeScrim.onClick.AddListener(close.onClick.Invoke);

        GamesHubPanel hub = hubRoot.AddComponent<GamesHubPanel>();
        hub.EditorConfigure(
            hubGroup, close, runnerCard, catchCard,
            runnerLives, catchLives, runnerLauncher, catchLauncher);
        return hub;
    }

    private static void AddHubSparkle(
        Transform parent, string name, Vector2 position, float size, Color color)
    {
        GameObject sparkle = CreatePremiumPanel(parent, name, color, size * 0.5f);
        SetAnchored(sparkle.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            position, new Vector2(size, size));
        sparkle.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    /// <summary>
    /// One mini-game tile: cream outer frame, coloured body, mode badge, name,
    /// short descriptor and a lives readout. The cream rim is intentional — flat
    /// coloured slabs without a frame read as unfinished next to the dock CTAs.
    /// </summary>
    private static Button BuildGameCard(
        Transform parent,
        string name,
        string title,
        string descriptor,
        string badge,
        Color color,
        Vector2 position,
        out TMP_Text livesLabel)
    {
        var outerSize = new Vector2(420f, 312f);
        var innerSize = new Vector2(400f, 292f);

        GameObject frame = CreatePremiumPanel(
            parent, name + "Frame", new Color32(255, 247, 224, 255), 40f);
        SetAnchored(frame.GetComponent<RectTransform>(), new Vector2(.5f, .5f), position, outerSize);

        Button card = CreateButton(frame.transform, name, string.Empty, color, innerSize);
        SetAnchored(card.GetComponent<RectTransform>(), new Vector2(.5f, .5f), Vector2.zero, innerSize);

        TMP_Text existing = card.GetComponentInChildren<TMP_Text>(true);
        if (existing != null)
            existing.gameObject.SetActive(false);

        GameObject badgePanel = CreatePremiumPanel(
            card.transform, "Badge", new Color32(255, 247, 224, 255), 22f);
        SetAnchored(badgePanel.GetComponent<RectTransform>(), new Vector2(.5f, 1f),
            new Vector2(0f, -34f), new Vector2(150f, 52f));
        TMP_Text badgeLabel = CreateLabel(badgePanel.transform, "BadgeLabel", badge, 22f, Dark);
        Stretch(badgeLabel.rectTransform);
        badgeLabel.alignment = TextAlignmentOptions.Center;
        badgeLabel.fontStyle = FontStyles.Bold;

        TMP_Text nameLabel = CreateLabel(card.transform, "Name", title, 38f, Cream);
        SetAnchored(nameLabel.rectTransform, new Vector2(.5f, 1f),
            new Vector2(0f, -112f), new Vector2(360f, 52f));
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.fontStyle = FontStyles.Bold;

        TMP_Text descriptorLabel = CreateLabel(card.transform, "Descriptor", descriptor, 19f, Cream);
        SetAnchored(descriptorLabel.rectTransform, new Vector2(.5f, 1f),
            new Vector2(0f, -164f), new Vector2(360f, 30f));
        descriptorLabel.alignment = TextAlignmentOptions.Center;
        descriptorLabel.alpha = 0.86f;
        descriptorLabel.fontStyle = FontStyles.Bold;

        GameObject livesPanel = CreatePremiumPanel(
            card.transform, "LivesPanel", new Color32(255, 247, 224, 255), 20f);
        SetAnchored(livesPanel.GetComponent<RectTransform>(), new Vector2(.5f, 0f),
            new Vector2(0f, 34f), new Vector2(340f, 58f));
        livesLabel = CreateLabel(livesPanel.transform, "Lives", "LIVES  5/5", 22f, Dark);
        Stretch(livesLabel.rectTransform);
        livesLabel.alignment = TextAlignmentOptions.Center;
        livesLabel.fontStyle = FontStyles.Bold;
        return card;
    }

    private static void BuildRunnerScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("CatRunnerRoot");
        SceneManager.MoveGameObjectToScene(root, scene);
        // The home remains loaded additively during a run so its save services
        // stay alive. Keep the runner world well outside the home camera volume
        // to prevent both scenes from visually overlapping.
        root.transform.position = new Vector3(0f, 1000f, 0f);
        CatRunnerGameController game = root.AddComponent<CatRunnerGameController>();

        Camera camera = BuildCamera(root.transform);
        Light keyLight = BuildLighting(root.transform);
        BuildPostProcessing(root.transform);
        CatRunnerPlayer player = BuildPlayer(root.transform);
        CatRunnerAudioController audio = BuildAudio(root.transform, player);
        CatRunnerTrackManager track = BuildTrack(root.transform, game, player);
        CatRunnerCameraRig cameraRig = camera.gameObject.AddComponent<CatRunnerCameraRig>();
        cameraRig.EditorConfigure(game, player, track);
        BuildUi(
            root.transform,
            out Canvas canvas,
            out TMP_Text timer,
            out TMP_Text coins,
            out TMP_Text distance,
            out TMP_Text bonus,
            out TMP_Text chances,
            out TMP_Text countdown,
            out GameObject welcome,
            out TMP_Text welcomeBest,
            out TMP_Text welcomeEnergy,
            out Button welcomeStart,
            out Button welcomeExit,
            out GameObject results,
            out TMP_Text resultTitle,
            out TMP_Text resultDetails,
            out Button collect,
            out Button retry,
            out CanvasGroup hitFlash,
            out RunnerUiExtras uiExtras);
        // Runner is additive and intentionally reuses CatHome_UI's one shared
        // EventSystem. Authoring a second one logs a duplicate-system error in
        // the frame before the Runner presentation controller can arbitrate it.
        CatRunnerFeedbackController feedback = BuildFeedback(
            root.transform,
            player,
            cameraRig,
            coins,
            countdown,
            hitFlash);
        CatRunnerThemeController theme = root.AddComponent<CatRunnerThemeController>();
        theme.EditorConfigure(game, camera, keyLight);

        game.EditorConfigure(
            player,
            track,
            camera,
            canvas,
            timer,
            coins,
            distance,
            bonus,
            chances,
            countdown,
            welcome,
            welcomeBest,
            welcomeEnergy,
            welcomeStart,
            welcomeExit,
            results,
            resultTitle,
            resultDetails,
            collect,
            retry);
        game.EditorConfigurePremium(
            audio,
            uiExtras.Score,
            uiExtras.Combo,
            uiExtras.PowerUps,
            uiExtras.Pause,
            uiExtras.WelcomeMissions,
            uiExtras.WelcomeRewarded,
            uiExtras.ResultMissions,
            uiExtras.NewBestBadge,
            uiExtras.PausePanel,
            uiExtras.Resume,
            uiExtras.PauseExit,
            uiExtras.ReducedMotion,
            uiExtras.Sound,
            uiExtras.Haptics,
            uiExtras.TutorialPanel,
            uiExtras.TutorialText,
            uiExtras.TutorialSkip);

        CatRunnerResponsiveLayout responsive =
            canvas.gameObject.AddComponent<CatRunnerResponsiveLayout>();
        responsive.EditorConfigure(
            uiExtras.SafeArea,
            uiExtras.TopHud,
            uiExtras.WelcomeCard,
            uiExtras.ResultsCard,
            uiExtras.PauseCard,
            uiExtras.TutorialCard);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, RunnerScenePath);
    }

    private static Camera BuildCamera(Transform parent)
    {
        var cameraObject = new GameObject("Runner Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0f, 2.35f, -5.4f);
        cameraObject.transform.localRotation = Quaternion.LookRotation(
            new Vector3(0f, 0.55f, 5f) - cameraObject.transform.localPosition,
            Vector3.up);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.fieldOfView = 56f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 120f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(76, 196, 231, 255);
        UniversalAdditionalCameraData cameraData =
            cameraObject.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
            cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;
        cameraData.stopNaN = true;
        cameraData.dithering = true;
        return camera;
    }

    private static Light BuildLighting(Transform parent)
    {
        var sunObject = new GameObject("Warm Directional Light", typeof(Light));
        sunObject.transform.SetParent(parent, false);
        sunObject.transform.localRotation = Quaternion.Euler(48f, -28f, 0f);
        Light sun = sunObject.GetComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color32(255, 228, 190, 255);
        sun.intensity = 1.25f;
        sun.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color32(190, 225, 242, 255);
        RenderSettings.ambientEquatorColor = new Color32(140, 184, 207, 255);
        RenderSettings.ambientGroundColor = new Color32(91, 71, 118, 255);
        return sun;
    }

    private static void BuildPostProcessing(Transform parent)
    {
        var volumeObject = new GameObject("Runner Global Volume", typeof(Volume));
        volumeObject.transform.SetParent(parent, false);
        Volume volume = volumeObject.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 110f;
        volume.weight = 1f;
        volume.sharedProfile = GetOrCreateRunnerVolumeProfile();
    }

    private static VolumeProfile GetOrCreateRunnerVolumeProfile()
    {
        VolumeProfile profile =
            AssetDatabase.LoadAssetAtPath<VolumeProfile>(RunnerVolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "CatRunner_VolumeProfile";
            AssetDatabase.CreateAsset(profile, RunnerVolumeProfilePath);
        }

        Bloom bloom = GetOrAddVolumeComponent<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.82f);
        bloom.intensity.Override(0.38f);
        bloom.scatter.Override(0.68f);
        bloom.highQualityFiltering.Override(false);
        bloom.maxIterations.Override(4);

        ColorAdjustments color = GetOrAddVolumeComponent<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(0.06f);
        color.contrast.Override(12f);
        color.saturation.Override(20f);

        Vignette vignette = GetOrAddVolumeComponent<Vignette>(profile);
        vignette.active = true;
        vignette.color.Override(new Color32(65, 27, 111, 255));
        vignette.intensity.Override(0.13f);
        vignette.smoothness.Override(0.72f);
        vignette.rounded.Override(true);

        Tonemapping tonemapping = GetOrAddVolumeComponent<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static T GetOrAddVolumeComponent<T>(VolumeProfile profile)
        where T : VolumeComponent
    {
        T component;
        if (!profile.TryGet<T>(out component))
        {
            component = profile.Add<T>(true);
            if (!AssetDatabase.Contains(component))
                AssetDatabase.AddObjectToAsset(component, profile);
        }
        EditorUtility.SetDirty(component);
        return component;
    }

    private static CatRunnerPlayer BuildPlayer(Transform parent)
    {
        var root = new GameObject("RunnerCat");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;

        GameObject catPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatPrefabPath);
        if (catPrefab == null)
            throw new InvalidOperationException($"Current cat prefab is missing at '{CatPrefabPath}'.");

        GameObject visual = PrefabUtility.InstantiatePrefab(catPrefab) as GameObject;
        if (visual == null)
            throw new InvalidOperationException("Current cat prefab could not be instantiated.");
        visual.name = "CurrentCat_Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 0.5f;

        Animator animator = visual.GetComponent<Animator>();
        CatRunnerPlayer player = root.AddComponent<CatRunnerPlayer>();
        player.EditorConfigure(animator, visual.transform);
        return player;
    }

    private static CatRunnerAudioController BuildAudio(
        Transform parent,
        CatRunnerPlayer player)
    {
        var audioObject = new GameObject(
            "RunnerAudio",
            typeof(AudioSource),
            typeof(CatRunnerAudioController));
        audioObject.transform.SetParent(parent, false);
        AudioSource music = audioObject.GetComponent<AudioSource>();
        AudioSource effects = audioObject.AddComponent<AudioSource>();
        CatRunnerAudioController controller =
            audioObject.GetComponent<CatRunnerAudioController>();
        controller.EditorConfigure(player, music, effects);
        return controller;
    }

    private static CatRunnerTrackManager BuildTrack(
        Transform parent,
        CatRunnerGameController game,
        CatRunnerPlayer player)
    {
        Material[] floorThemes =
        {
            GetOrCreateMaterial("RunnerFloorSunset", new Color32(164, 64, 132, 255)),
            GetOrCreateMaterial("RunnerFloorCandy", new Color32(111, 74, 178, 255)),
            GetOrCreateMaterial("RunnerFloorSky", new Color32(34, 125, 177, 255))
        };
        Material[] roadThemes =
        {
            GetOrCreateMaterial("RunnerRoadPeach", new Color32(255, 145, 118, 255)),
            GetOrCreateMaterial("RunnerRoadBerry", new Color32(241, 119, 184, 255)),
            GetOrCreateMaterial("RunnerRoadAqua", new Color32(74, 194, 202, 255))
        };
        Material[] laneThemes =
        {
            GetOrCreateGlowMaterial("RunnerLaneMint", new Color32(78, 250, 216, 255)),
            GetOrCreateGlowMaterial("RunnerLanePink", new Color32(255, 112, 190, 255)),
            GetOrCreateGlowMaterial("RunnerLaneBlue", new Color32(112, 225, 255, 255))
        };
        Material[] edgeThemes =
        {
            GetOrCreateMaterial("RunnerEdgeCream", Cream),
            GetOrCreateMaterial("RunnerEdgeLemon", new Color32(255, 226, 85, 255)),
            GetOrCreateMaterial("RunnerEdgeIce", new Color32(206, 248, 255, 255))
        };
        Material[] accentThemes =
        {
            GetOrCreateMaterial("RunnerAccentCoral", new Color32(255, 102, 105, 255)),
            GetOrCreateMaterial("RunnerAccentPurple", new Color32(116, 75, 194, 255)),
            GetOrCreateMaterial("RunnerAccentTeal", new Color32(37, 204, 181, 255))
        };
        Material goldMaterial = GetOrCreateGlowMaterial(
            "RunnerCoinGold", new Color32(255, 196, 42, 255), true, 2.05f);
        Material coinCore = GetOrCreateGlowMaterial(
            "RunnerCoinCore", new Color32(255, 238, 111, 255), true, 2.35f);
        Material coinPaw = GetOrCreateGlowMaterial(
            "RunnerCoinPaw", new Color32(255, 118, 36, 255), true, 1.7f);
        Material coinRim = GetOrCreateMaterial(
            "RunnerCoinRim", new Color32(119, 65, 63, 255), true);
        Material orangeMaterial = accentThemes[0];
        Material tealMaterial = accentThemes[2];
        Material purpleMaterial = accentThemes[1];
        Material creamMaterial = edgeThemes[0];
        Material darkMaterial = GetOrCreateMaterial("RunnerObstacleDark", new Color32(71, 49, 86, 255));
        Material boxMaterial = GetOrCreateMaterial("RunnerBox", Wood);
        Material hazardWarning = GetOrCreateGlowMaterial(
            "RunnerHazardWarning", new Color32(255, 92, 117, 255), false, 2.2f);
        Material magnetMaterial = GetOrCreateGlowMaterial(
            "RunnerPowerMagnet", new Color32(64, 235, 218, 255), true, 2.15f);
        Material shieldMaterial = GetOrCreateGlowMaterial(
            "RunnerPowerShield", new Color32(109, 155, 255, 255), true, 2.15f);
        Material doubleMaterial = GetOrCreateGlowMaterial(
            "RunnerPowerDouble", new Color32(255, 107, 187, 255), true, 2.15f);
        Material[] sceneryMaterials =
        {
            GetOrCreateMaterial("RunnerSceneryPink", new Color32(255, 151, 190, 255)),
            GetOrCreateMaterial("RunnerSceneryLavender", new Color32(181, 155, 244, 255)),
            GetOrCreateMaterial("RunnerSceneryMint", new Color32(125, 229, 196, 255)),
            GetOrCreateMaterial("RunnerSceneryPeach", new Color32(255, 190, 126, 255)),
            GetOrCreateMaterial("RunnerScenerySky", new Color32(128, 211, 246, 255)),
            GetOrCreateGlowMaterial("RunnerSceneryWindow", new Color32(160, 244, 255, 255)),
            GetOrCreateMaterial("RunnerCloudCream", new Color32(255, 247, 226, 255))
        };

        var trackRoot = new GameObject("ScrollingTrack");
        trackRoot.transform.SetParent(parent, false);
        CatRunnerTrackManager manager = trackRoot.AddComponent<CatRunnerTrackManager>();

        const int segmentCount = 10;
        const float segmentLength = 8f;
        var segments = new Transform[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            var segment = new GameObject($"RoomSegment_{i + 1:00}");
            segment.transform.SetParent(trackRoot.transform, false);
            segment.transform.localPosition = new Vector3(0f, 0f, i * segmentLength - segmentLength);
            segments[i] = segment.transform;

            Renderer floor = CreatePrimitive(
                PrimitiveType.Cube,
                "RoadSurround",
                segment.transform,
                new Vector3(0f, -0.18f, 0f),
                new Vector3(9.7f, 0.24f, segmentLength),
                floorThemes[0]).GetComponent<Renderer>();
            Renderer road = CreatePrimitive(
                PrimitiveType.Cube, "CandyBoulevard", segment.transform,
                new Vector3(0f, -0.035f, 0f),
                new Vector3(7.05f, 0.13f, segmentLength), roadThemes[0])
                .GetComponent<Renderer>();
            Renderer sidewalkLeft = CreatePrimitive(
                PrimitiveType.Cube, "SidewalkLeft", segment.transform,
                new Vector3(-4.18f, -0.015f, 0f),
                new Vector3(1.05f, 0.18f, segmentLength), edgeThemes[0])
                .GetComponent<Renderer>();
            Renderer sidewalkRight = CreatePrimitive(
                PrimitiveType.Cube, "SidewalkRight", segment.transform,
                new Vector3(4.18f, -0.015f, 0f),
                new Vector3(1.05f, 0.18f, segmentLength), edgeThemes[0])
                .GetComponent<Renderer>();
            Renderer edgeLeft = CreatePrimitive(
                PrimitiveType.Cube, "LeftRoadCurb", segment.transform,
                new Vector3(-3.59f, 0.09f, 0f),
                new Vector3(0.16f, 0.24f, segmentLength), edgeThemes[0])
                .GetComponent<Renderer>();
            Renderer edgeRight = CreatePrimitive(
                PrimitiveType.Cube, "RightRoadCurb", segment.transform,
                new Vector3(3.59f, 0.09f, 0f),
                new Vector3(0.16f, 0.24f, segmentLength), edgeThemes[0])
                .GetComponent<Renderer>();

            var accentRenderers = new List<Renderer>();
            var laneRenderers = new List<Renderer>();
            var edgeRenderers = new List<Renderer>
            {
                edgeLeft, edgeRight, sidewalkLeft, sidewalkRight
            };
            for (int lane = -1; lane <= 1; lane += 2)
            {
                for (int dash = 0; dash < 4; dash++)
                {
                    Renderer marker = CreatePrimitive(
                        PrimitiveType.Cube,
                        $"LaneDash_{(lane < 0 ? "L" : "R")}_{dash + 1}",
                        segment.transform,
                        new Vector3(lane * .675f, .045f, -3f + dash * 2f),
                        new Vector3(.055f, .025f, .72f), laneThemes[0])
                        .GetComponent<Renderer>();
                    marker.shadowCastingMode = ShadowCastingMode.Off;
                    marker.receiveShadows = false;
                    laneRenderers.Add(marker);
                }
            }

            // A slim gateway every four segments gives the route a recurring
            // landmark without forming the giant blocky tunnel seen in the
            // first visual pass.
            if (i % 4 == 2)
            {
                const float gatewayPostX = 4.12f;
                const float gatewayTopY = 3.38f;
                accentRenderers.Add(CreatePrimitive(
                    PrimitiveType.Cylinder, "GatewayLeft", segment.transform,
                    new Vector3(-gatewayPostX, gatewayTopY * .5f, 0f),
                    new Vector3(.075f, gatewayTopY * .5f, .075f),
                    accentThemes[0]).GetComponent<Renderer>());
                accentRenderers.Add(CreatePrimitive(
                    PrimitiveType.Cylinder, "GatewayRight", segment.transform,
                    new Vector3(gatewayPostX, gatewayTopY * .5f, 0f),
                    new Vector3(.075f, gatewayTopY * .5f, .075f),
                    accentThemes[0]).GetComponent<Renderer>());
                accentRenderers.Add(CreatePrimitive(
                    PrimitiveType.Cube, "CatTrailRibbon", segment.transform,
                    new Vector3(0f, gatewayTopY, 0f),
                    new Vector3(gatewayPostX * 2f + .08f, .055f, .065f),
                    accentThemes[0]).GetComponent<Renderer>());
                accentRenderers.Add(CreateCatEar(
                    segment.transform, "RibbonEarLeft", -.19f, gatewayTopY + .12f,
                    accentThemes[0], .13f, .055f)
                    .GetComponent<Renderer>());
                accentRenderers.Add(CreateCatEar(
                    segment.transform, "RibbonEarRight", .19f, gatewayTopY + .12f,
                    accentThemes[0], .13f, .055f)
                    .GetComponent<Renderer>());
            }

            BuildSegmentScenery(
                segment.transform,
                i,
                accentThemes[0],
                edgeThemes[0],
                laneThemes[0],
                sceneryMaterials,
                out GameObject[] sceneryVariants,
                out Transform[] floaters,
                out Transform[] spinners,
                out Renderer[] sceneryAccents,
                out Renderer[] sceneryEdges,
                out Renderer[] sceneryGlows);
            accentRenderers.AddRange(sceneryAccents);
            edgeRenderers.AddRange(sceneryEdges);
            laneRenderers.AddRange(sceneryGlows);

            CatRunnerThemeSegment themed = segment.AddComponent<CatRunnerThemeSegment>();
            themed.EditorConfigure(
                new[] { floor },
                new[] { road },
                laneRenderers.ToArray(),
                edgeRenderers.ToArray(),
                accentRenderers.ToArray(),
                floorThemes,
                roadThemes,
                laneThemes,
                edgeThemes,
                accentThemes);
            themed.ApplyTheme(0);
            CatRunnerScenerySegment scenery =
                segment.AddComponent<CatRunnerScenerySegment>();
            scenery.EditorConfigure(sceneryVariants, floaters, spinners, i);
        }

        var templateRoot = new GameObject("Templates");
        templateRoot.transform.SetParent(trackRoot.transform, false);

        GameObject coin = CreateTemplateRoot("Coin_Template", templateRoot.transform);
        coin.AddComponent<CatRunnerTrackObject>().EditorConfigure(CatRunnerTrackObjectKind.Coin);
        if (!TryBuildPremiumCoinVisual(
                coin.transform,
                goldMaterial,
                coinCore,
                coinPaw,
                coinRim))
        {
            GameObject coinOuter = CreatePrimitive(
                PrimitiveType.Cylinder, "CoinOuter", coin.transform, Vector3.zero,
                new Vector3(.34f, .075f, .34f), goldMaterial);
            coinOuter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject coinInner = CreatePrimitive(
                PrimitiveType.Cylinder, "CoinCore", coin.transform, new Vector3(0f, 0f, -.055f),
                new Vector3(.265f, .085f, .265f), coinCore);
            coinInner.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive(PrimitiveType.Sphere, "PawPad", coin.transform,
                new Vector3(0f, -.055f, -.155f), new Vector3(.13f, .1f, .045f), coinPaw);
            CreatePrimitive(PrimitiveType.Sphere, "ToeA", coin.transform,
                new Vector3(-.12f, .09f, -.15f), new Vector3(.065f, .065f, .035f), coinPaw);
            CreatePrimitive(PrimitiveType.Sphere, "ToeB", coin.transform,
                new Vector3(0f, .13f, -.15f), new Vector3(.065f, .065f, .035f), coinPaw);
            CreatePrimitive(PrimitiveType.Sphere, "ToeC", coin.transform,
                new Vector3(.12f, .09f, -.15f), new Vector3(.065f, .065f, .035f), coinPaw);
            CreatePrimitive(PrimitiveType.Sphere, "CoinGlint", coin.transform,
                new Vector3(-.17f, .17f, -.16f), new Vector3(.055f, .055f, .025f), creamMaterial);
        }
        BuildCoinAura(coin.transform, goldMaterial, laneThemes[0], creamMaterial);
        coin.SetActive(false);

        GameObject yarn = CreateTemplateRoot("YarnBasket_Template", templateRoot.transform);
        yarn.AddComponent<CatRunnerTrackObject>().EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        CreatePrimitive(PrimitiveType.Cube, "Basket", yarn.transform,
            new Vector3(0f, .24f, 0f), new Vector3(.92f, .48f, .68f), boxMaterial);
        CreatePrimitive(PrimitiveType.Sphere, "PinkYarn", yarn.transform,
            new Vector3(-.22f, .58f, 0f), new Vector3(.42f, .42f, .42f), orangeMaterial);
        CreatePrimitive(PrimitiveType.Sphere, "PurpleYarn", yarn.transform,
            new Vector3(.23f, .56f, .02f), new Vector3(.4f, .4f, .4f), purpleMaterial);
        yarn.SetActive(false);

        GameObject scratch = CreateTemplateRoot("ScratchPost_Template", templateRoot.transform);
        scratch.AddComponent<CatRunnerTrackObject>().EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        if (InstantiateDecorPrefabFitted(
                PetshopPrefabFolder + "ScratchingPost5 V3.prefab",
                scratch.transform,
                "PetshopScratchPostVisual",
                Vector3.zero,
                Vector3.zero,
                1.15f,
                true) == null)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Base", scratch.transform,
                new Vector3(0f, .09f, 0f), new Vector3(.7f, .09f, .7f), tealMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, "Post", scratch.transform,
                new Vector3(0f, .52f, 0f), new Vector3(.18f, .46f, .18f), creamMaterial);
            CreatePrimitive(PrimitiveType.Sphere, "TopToy", scratch.transform,
                new Vector3(0f, 1.02f, 0f), new Vector3(.27f, .27f, .27f), orangeMaterial);
        }
        scratch.SetActive(false);

        GameObject bowl = CreateTemplateRoot("FoodBowl_Template", templateRoot.transform);
        bowl.AddComponent<CatRunnerTrackObject>().EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        if (InstantiateDecorPrefabFitted(
                PetshopPrefabFolder + "Bowl1 V3.prefab",
                bowl.transform,
                "PetshopFoodBowlVisual",
                Vector3.zero,
                Vector3.zero,
                .42f,
                true) == null)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "Bowl", bowl.transform,
                new Vector3(0f, 0.18f, 0f), new Vector3(0.78f, 0.24f, 0.78f), tealMaterial);
            CreatePrimitive(PrimitiveType.Cylinder, "Food", bowl.transform,
                new Vector3(0f, 0.33f, 0f), new Vector3(0.48f, 0.06f, 0.48f), orangeMaterial);
        }
        bowl.SetActive(false);

        GameObject vacuum = CreateTemplateRoot("RobotVacuum_Template", templateRoot.transform);
        vacuum.AddComponent<CatRunnerTrackObject>().EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        CreatePrimitive(PrimitiveType.Cylinder, "VacuumBody", vacuum.transform,
            new Vector3(0f, .16f, 0f), new Vector3(.68f, .16f, .68f), darkMaterial);
        CreatePrimitive(PrimitiveType.Cylinder, "VacuumLight", vacuum.transform,
            new Vector3(0f, .33f, 0f), new Vector3(.13f, .025f, .13f), laneThemes[0]);
        vacuum.SetActive(false);

        GameObject catBed = CreateTemplateRoot("CatBed_Template", templateRoot.transform);
        catBed.AddComponent<CatRunnerTrackObject>()
            .EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        if (InstantiateDecorPrefabFitted(
                PetshopPrefabFolder + "Bed5 V3.prefab",
                catBed.transform,
                "PlushCatBedVisual",
                Vector3.zero,
                Vector3.zero,
                .62f,
                true) == null)
        {
            CreatePrimitive(PrimitiveType.Cylinder, "BedCushion", catBed.transform,
                new Vector3(0f, .2f, 0f), new Vector3(.72f, .2f, .58f), purpleMaterial);
            CreatePrimitive(PrimitiveType.Sphere, "BedPillow", catBed.transform,
                new Vector3(0f, .37f, .05f), new Vector3(.58f, .16f, .42f), creamMaterial);
        }
        catBed.SetActive(false);

        GameObject treatStack = CreateTemplateRoot("TreatStack_Template", templateRoot.transform);
        treatStack.AddComponent<CatRunnerTrackObject>()
            .EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        GameObject treatLeft = InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Food3 V3.prefab",
            treatStack.transform,
            "SalmonTreatBag",
            new Vector3(-.22f, 0f, .04f),
            new Vector3(0f, -12f, -5f),
            .76f,
            true);
        GameObject treatRight = InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Food2 V1.prefab",
            treatStack.transform,
            "TunaTreatBag",
            new Vector3(.23f, 0f, -.02f),
            new Vector3(0f, 14f, 4f),
            .68f,
            true);
        if (treatLeft == null && treatRight == null)
        {
            CreatePrimitive(PrimitiveType.Cube, "TreatBagA", treatStack.transform,
                new Vector3(-.2f, .36f, 0f), new Vector3(.42f, .72f, .38f), orangeMaterial);
            CreatePrimitive(PrimitiveType.Cube, "TreatBagB", treatStack.transform,
                new Vector3(.24f, .31f, .02f), new Vector3(.4f, .62f, .36f), tealMaterial);
        }
        treatStack.SetActive(false);

        GameObject carrier = CreateTemplateRoot("CatCarrier_Template", templateRoot.transform);
        carrier.AddComponent<CatRunnerTrackObject>()
            .EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        CreatePrimitive(PrimitiveType.Cube, "CarrierShell", carrier.transform,
            new Vector3(0f, .34f, 0f), new Vector3(.92f, .62f, .68f), purpleMaterial);
        CreatePrimitive(PrimitiveType.Cube, "CarrierDoorInset", carrier.transform,
            new Vector3(0f, .34f, -.355f), new Vector3(.62f, .42f, .035f), darkMaterial);
        for (int bar = -2; bar <= 2; bar++)
            CreatePrimitive(PrimitiveType.Cube, $"CarrierDoorBar_{bar + 3}", carrier.transform,
                new Vector3(bar * .105f, .34f, -.38f), new Vector3(.025f, .39f, .025f),
                creamMaterial);
        CreatePrimitive(PrimitiveType.Cylinder, "CarrierHandle", carrier.transform,
            new Vector3(0f, .72f, 0f), new Vector3(.26f, .055f, .26f), laneThemes[0]);
        carrier.SetActive(false);

        GameObject pillowPile = CreateTemplateRoot("PillowPile_Template", templateRoot.transform);
        pillowPile.AddComponent<CatRunnerTrackObject>()
            .EditorConfigure(CatRunnerTrackObjectKind.Obstacle);
        GameObject pillowA = InstantiateDecorPrefabFitted(
            LivingRoomPrefabFolder + "Pillow_Square_1.prefab",
            pillowPile.transform,
            "MintPillow",
            new Vector3(-.18f, 0f, 0f),
            new Vector3(0f, 18f, -8f),
            .46f,
            true);
        GameObject pillowB = InstantiateDecorPrefabFitted(
            LivingRoomPrefabFolder + "Pillow_Square_2.prefab",
            pillowPile.transform,
            "BerryPillow",
            new Vector3(.2f, .18f, .02f),
            new Vector3(0f, -14f, 10f),
            .48f,
            true);
        if (pillowA == null && pillowB == null)
            CreatePrimitive(PrimitiveType.Sphere, "PlushPillowFallback", pillowPile.transform,
                new Vector3(0f, .3f, 0f), new Vector3(.78f, .42f, .58f), orangeMaterial);
        pillowPile.SetActive(false);

        GameObject leashArch = BuildSlideArchTemplate(
            templateRoot.transform,
            "LeashArch_Template",
            tealMaterial,
            orangeMaterial,
            laneThemes[0],
            creamMaterial,
            true);
        GameObject napCanopy = BuildSlideArchTemplate(
            templateRoot.transform,
            "CatNapCanopy_Template",
            purpleMaterial,
            orangeMaterial,
            laneThemes[1],
            creamMaterial,
            false);

        GameObject mintPlatform = BuildElevatedPawPlatform(
            templateRoot.transform,
            "MintPawPlatform_Template",
            tealMaterial,
            laneThemes[0],
            creamMaterial,
            coinPaw);
        GameObject berryPlatform = BuildElevatedPawPlatform(
            templateRoot.transform,
            "BerryPawPlatform_Template",
            purpleMaterial,
            laneThemes[1],
            creamMaterial,
            goldMaterial);

        GameObject magnetPowerUp = BuildPowerUpTemplate(
            templateRoot.transform,
            "MagnetPowerUp_Template",
            CatRunnerPowerUpKind.Magnet,
            magnetMaterial,
            creamMaterial,
            coinPaw,
            goldMaterial);
        GameObject shieldPowerUp = BuildPowerUpTemplate(
            templateRoot.transform,
            "ShieldPowerUp_Template",
            CatRunnerPowerUpKind.Shield,
            shieldMaterial,
            creamMaterial,
            coinPaw,
            goldMaterial);
        GameObject doublePowerUp = BuildPowerUpTemplate(
            templateRoot.transform,
            "DoubleCoinsPowerUp_Template",
            CatRunnerPowerUpKind.DoubleCoins,
            doubleMaterial,
            goldMaterial,
            coinPaw,
            creamMaterial);

        GameObject[] groundHazards =
        {
            yarn, scratch, bowl, vacuum,
            catBed, treatStack, carrier, pillowPile
        };
        for (int i = 0; i < groundHazards.Length; i++)
            BuildHazardTelegraph(groundHazards[i], hazardWarning);
        BuildHazardTelegraph(leashArch, hazardWarning);
        BuildHazardTelegraph(napCanopy, hazardWarning);

        manager.EditorConfigure(
            game,
            player,
            segments,
            coin,
            groundHazards);
        manager.EditorConfigurePremiumRoutes(
            new[] { leashArch, napCanopy },
            new[] { mintPlatform, berryPlatform },
            new[] { magnetPowerUp, shieldPowerUp, doublePowerUp });
        return manager;
    }

    private static GameObject BuildPowerUpTemplate(
        Transform parent,
        string name,
        CatRunnerPowerUpKind kind,
        Material primary,
        Material secondary,
        Material paw,
        Material sparkle)
    {
        GameObject root = CreateTemplateRoot(name, parent);
        root.AddComponent<CatRunnerTrackObject>().EditorConfigure(
            CatRunnerTrackObjectKind.PowerUp,
            pickupKind: kind);

        GameObject aura = CreatePrimitive(
            PrimitiveType.Cylinder,
            "PowerAura",
            root.transform,
            new Vector3(0f, 0f, .08f),
            new Vector3(.55f, .035f, .55f),
            primary);
        aura.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        GameObject core = CreatePrimitive(
            PrimitiveType.Sphere,
            "PowerCore",
            root.transform,
            Vector3.zero,
            new Vector3(.42f, .46f, .12f),
            secondary);

        switch (kind)
        {
            case CatRunnerPowerUpKind.Magnet:
            {
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "MagnetLeft",
                    root.transform,
                    new Vector3(-.18f, .05f, -.14f),
                    new Vector3(.075f, .22f, .075f),
                    primary);
                CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "MagnetRight",
                    root.transform,
                    new Vector3(.18f, .05f, -.14f),
                    new Vector3(.075f, .22f, .075f),
                    primary);
                GameObject bridge = CreatePrimitive(
                    PrimitiveType.Cube,
                    "MagnetBridge",
                    root.transform,
                    new Vector3(0f, -.16f, -.14f),
                    new Vector3(.36f, .13f, .13f),
                    primary);
                bridge.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                break;
            }
            case CatRunnerPowerUpKind.Shield:
            {
                CreatePrimitive(
                    PrimitiveType.Sphere,
                    "ShieldPawPad",
                    root.transform,
                    new Vector3(0f, -.06f, -.14f),
                    new Vector3(.16f, .13f, .055f),
                    paw);
                for (int toe = -1; toe <= 1; toe++)
                {
                    CreatePrimitive(
                        PrimitiveType.Sphere,
                        $"ShieldToe_{toe + 2}",
                        root.transform,
                        new Vector3(toe * .12f, .12f + Mathf.Abs(toe) * -.025f, -.14f),
                        new Vector3(.065f, .065f, .035f),
                        paw);
                }
                break;
            }
            default:
            {
                for (int token = -1; token <= 1; token += 2)
                {
                    GameObject medal = CreatePrimitive(
                        PrimitiveType.Cylinder,
                        token < 0 ? "DoubleCoinLeft" : "DoubleCoinRight",
                        root.transform,
                        new Vector3(token * .16f, 0f, -.14f),
                        new Vector3(.19f, .035f, .19f),
                        primary);
                    medal.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CreatePrimitive(
                        PrimitiveType.Sphere,
                        token < 0 ? "DoublePawLeft" : "DoublePawRight",
                        root.transform,
                        new Vector3(token * .16f, -.02f, -.19f),
                        new Vector3(.075f, .06f, .025f),
                        paw);
                }
                break;
            }
        }

        CreatePrimitive(
            PrimitiveType.Sphere,
            "PowerSparkleA",
            root.transform,
            new Vector3(-.36f, .34f, -.08f),
            new Vector3(.065f, .065f, .035f),
            sparkle);
        CreatePrimitive(
            PrimitiveType.Sphere,
            "PowerSparkleB",
            root.transform,
            new Vector3(.38f, -.27f, -.08f),
            new Vector3(.05f, .05f, .028f),
            sparkle);
        core.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        root.SetActive(false);
        return root;
    }

    private static void BuildHazardTelegraph(
        GameObject template,
        Material warningMaterial)
    {
        if (template == null || warningMaterial == null)
            return;
        GameObject warningRoot = CreateTemplateRoot(
            "HazardWarningTelegraph",
            template.transform);
        for (int index = 0; index < 3; index++)
        {
            float z = -1.05f - index * .56f;
            GameObject left = CreatePrimitive(
                PrimitiveType.Cube,
                $"WarningChevronLeft_{index + 1}",
                warningRoot.transform,
                new Vector3(-.13f, .035f, z),
                new Vector3(.07f, .025f, .42f),
                warningMaterial);
            left.transform.localRotation = Quaternion.Euler(0f, -27f, 0f);
            GameObject right = CreatePrimitive(
                PrimitiveType.Cube,
                $"WarningChevronRight_{index + 1}",
                warningRoot.transform,
                new Vector3(.13f, .035f, z),
                new Vector3(.07f, .025f, .42f),
                warningMaterial);
            right.transform.localRotation = Quaternion.Euler(0f, 27f, 0f);
            foreach (Renderer renderer in new[]
                     {
                         left.GetComponent<Renderer>(),
                         right.GetComponent<Renderer>()
                     })
            {
                if (renderer == null)
                    continue;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }
    }

    private static bool TryBuildPremiumCoinVisual(
        Transform parent,
        Material gold,
        Material core,
        Material paw,
        Material rim)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PawCoinModelPath);
        if (model == null || parent == null)
            return false;

        GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (visual == null)
            return false;
        visual.name = "PremiumPawCoinVisual";
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = Vector3.zero;
        // The imported mesh already lies in XY with its embossed face along Z.
        // Its authored front points away from the gameplay camera, so turn the
        // visual once and let the pickup root perform only a small showcase sway.
        visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        visual.transform.localScale = Vector3.one * .34f;
        foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
        {
            Material[] authored = renderer.sharedMaterials;
            var premium = new Material[Mathf.Max(1, authored.Length)];
            for (int i = 0; i < premium.Length; i++)
            {
                string authoredName = i < authored.Length && authored[i] != null
                    ? authored[i].name
                    : string.Empty;
                string key = string.IsNullOrEmpty(authoredName)
                    ? renderer.name.ToLowerInvariant()
                    : authoredName.ToLowerInvariant();
                premium[i] = key.Contains("dark") || key.Contains("rim") || key.Contains("edge")
                    ? rim
                    : key.Contains("paw") || key.Contains("toe") || key.Contains("pad") ||
                      key.Contains("coral")
                    ? paw
                    : key.Contains("core") || key.Contains("face") || key.Contains("inset") ||
                      key.Contains("light") || key.Contains("glint")
                        ? core
                        : gold;
            }
            renderer.sharedMaterials = premium;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        }
        return true;
    }

    private static void BuildCoinAura(
        Transform parent,
        Material gold,
        Material aqua,
        Material cream)
    {
        GameObject halo = CreateTemplateRoot("CoinHalo", parent);
        for (int index = 0; index < 4; index++)
        {
            float angle = index * Mathf.PI * .5f;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * .46f,
                Mathf.Sin(angle) * .46f,
                .035f);
            CreatePrimitive(
                PrimitiveType.Sphere,
                $"HaloGem_{index + 1}",
                halo.transform,
                position,
                Vector3.one * (index % 2 == 0 ? .045f : .032f),
                index % 2 == 0 ? gold : aqua);
        }

        BuildCoinSparkle(parent, "CoinSparkleA", new Vector3(-.42f, .34f, -.035f), cream);
        BuildCoinSparkle(parent, "CoinSparkleB", new Vector3(.4f, -.3f, .035f), aqua);
    }

    private static void BuildCoinSparkle(
        Transform parent,
        string name,
        Vector3 position,
        Material material)
    {
        GameObject sparkle = CreateTemplateRoot(name, parent);
        sparkle.transform.localPosition = position;
        CreatePrimitive(PrimitiveType.Cube, "SparkleVertical", sparkle.transform,
            Vector3.zero, new Vector3(.035f, .16f, .025f), material);
        CreatePrimitive(PrimitiveType.Cube, "SparkleHorizontal", sparkle.transform,
            Vector3.zero, new Vector3(.16f, .035f, .025f), material);
    }

    private static GameObject BuildSlideArchTemplate(
        Transform parent,
        string name,
        Material body,
        Material accent,
        Material glow,
        Material cream,
        bool hangingToys)
    {
        GameObject root = CreateTemplateRoot(name, parent);
        root.AddComponent<CatRunnerTrackObject>()
            .EditorConfigure(CatRunnerTrackObjectKind.OverheadObstacle);
        CreatePrimitive(PrimitiveType.Cylinder, "LeftPawPost", root.transform,
            new Vector3(-.49f, .39f, 0f), new Vector3(.09f, .39f, .09f), body);
        CreatePrimitive(PrimitiveType.Cylinder, "RightPawPost", root.transform,
            new Vector3(.49f, .39f, 0f), new Vector3(.09f, .39f, .09f), body);
        CreatePrimitive(PrimitiveType.Cube, "SoftCrossbar", root.transform,
            new Vector3(0f, .79f, 0f), new Vector3(1.08f, .12f, .2f), accent);
        CreateCatEar(root.transform, "ArchEarLeft", -.25f, .95f, accent, .22f);
        CreateCatEar(root.transform, "ArchEarRight", .25f, .95f, accent, .22f);
        CreatePrimitive(PrimitiveType.Sphere, "ArchPawGlow", root.transform,
            new Vector3(0f, .88f, -.11f), new Vector3(.13f, .09f, .035f), glow);

        if (hangingToys)
        {
            CreatePrimitive(PrimitiveType.Cube, "ToyStringLeft", root.transform,
                new Vector3(-.25f, .67f, 0f), new Vector3(.018f, .18f, .018f), cream);
            CreatePrimitive(PrimitiveType.Sphere, "ToyBallLeft", root.transform,
                new Vector3(-.25f, .56f, 0f), Vector3.one * .09f, glow);
            CreatePrimitive(PrimitiveType.Cube, "ToyStringRight", root.transform,
                new Vector3(.25f, .68f, 0f), new Vector3(.018f, .16f, .018f), cream);
            CreatePrimitive(PrimitiveType.Sphere, "ToyBallRight", root.transform,
                new Vector3(.25f, .58f, 0f), Vector3.one * .075f, accent);
        }
        else
        {
            CreatePrimitive(PrimitiveType.Sphere, "CanopyPillowLeft", root.transform,
                new Vector3(-.27f, .72f, 0f), new Vector3(.22f, .1f, .15f), cream);
            CreatePrimitive(PrimitiveType.Sphere, "CanopyPillowRight", root.transform,
                new Vector3(.27f, .72f, 0f), new Vector3(.22f, .1f, .15f), glow);
        }
        root.SetActive(false);
        return root;
    }

    private static GameObject BuildElevatedPawPlatform(
        Transform parent,
        string name,
        Material deck,
        Material glow,
        Material trim,
        Material paw)
    {
        const float height = .82f;
        const float totalLength = 13f;
        const float rampLength = 3f;
        GameObject root = CreateTemplateRoot(name, parent);
        root.AddComponent<CatRunnerTrackObject>().EditorConfigure(
            CatRunnerTrackObjectKind.Platform,
            height,
            totalLength,
            rampLength,
            .65f);

        GameObject entry = CreatePrimitive(PrimitiveType.Cube, "EntryRamp", root.transform,
            new Vector3(0f, .41f, -5f), new Vector3(1.18f, .14f, 3.18f), deck);
        entry.transform.localRotation = Quaternion.Euler(-14.5f, 0f, 0f);
        CreatePrimitive(PrimitiveType.Cube, "RaisedDeck", root.transform,
            new Vector3(0f, .74f, 0f), new Vector3(1.18f, .16f, 7.02f), deck);
        GameObject exit = CreatePrimitive(PrimitiveType.Cube, "ExitRamp", root.transform,
            new Vector3(0f, .41f, 5f), new Vector3(1.18f, .14f, 3.18f), deck);
        exit.transform.localRotation = Quaternion.Euler(14.5f, 0f, 0f);

        CreatePlatformEdge(root.transform, "DeckEdgeLeft",
            new Vector3(-.55f, .85f, 0f), new Vector3(.035f, .035f, 7.02f),
            Quaternion.identity, glow);
        CreatePlatformEdge(root.transform, "DeckEdgeRight",
            new Vector3(.55f, .85f, 0f), new Vector3(.035f, .035f, 7.02f),
            Quaternion.identity, glow);
        for (int side = -1; side <= 1; side += 2)
        {
            CreatePlatformEdge(root.transform, side < 0 ? "EntryEdgeLeft" : "EntryEdgeRight",
                new Vector3(side * .55f, .48f, -5f), new Vector3(.035f, .035f, 3.16f),
                Quaternion.Euler(-14.5f, 0f, 0f), trim);
            CreatePlatformEdge(root.transform, side < 0 ? "ExitEdgeLeft" : "ExitEdgeRight",
                new Vector3(side * .55f, .48f, 5f), new Vector3(.035f, .035f, 3.16f),
                Quaternion.Euler(14.5f, 0f, 0f), trim);
        }

        for (int mark = -1; mark <= 1; mark++)
        {
            float z = mark * 2.15f;
            CreatePrimitive(PrimitiveType.Sphere, $"DeckPawPad_{mark + 2}", root.transform,
                new Vector3(0f, .85f, z), new Vector3(.16f, .025f, .13f), paw);
            for (int toe = -1; toe <= 1; toe++)
                CreatePrimitive(PrimitiveType.Sphere, $"DeckToe_{mark + 2}_{toe + 2}", root.transform,
                    new Vector3(toe * .13f, .86f, z + .15f + Mathf.Abs(toe) * -.025f),
                    new Vector3(.06f, .022f, .055f), paw);
        }

        root.SetActive(false);
        return root;
    }

    private static void CreatePlatformEdge(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        GameObject edge = CreatePrimitive(
            PrimitiveType.Cube, name, parent, position, scale, material);
        edge.transform.localRotation = rotation;
    }

    private static void BuildSegmentScenery(
        Transform segment,
        int segmentIndex,
        Material accent,
        Material edge,
        Material glow,
        Material[] scenery,
        out GameObject[] variants,
        out Transform[] floaters,
        out Transform[] spinners,
        out Renderer[] accentRenderers,
        out Renderer[] edgeRenderers,
        out Renderer[] glowRenderers)
    {
        var accents = new List<Renderer>();
        var edges = new List<Renderer>();
        var glows = new List<Renderer>();
        var floatingItems = new List<Transform>();
        var spinningItems = new List<Transform>();
        float side = segmentIndex % 2 == 0 ? -1f : 1f;
        Material pink = scenery[0];
        Material lavender = scenery[1];
        Material mint = scenery[2];
        Material peach = scenery[3];
        Material sky = scenery[4];
        Material window = scenery[5];
        Material cloud = scenery[6];

        GameObject near = CreateTemplateRoot("NearScenery", segment);
        BuildPawLamp(
            near.transform, new Vector3(-4.35f, .08f, -2.35f), edge, glow, accents, edges, glows);
        BuildPawLamp(
            near.transform, new Vector3(4.35f, .08f, 2.35f), edge, glow, accents, edges, glows);

        // A continuous, small-scale pet-town silhouette. These narrow homes
        // replace the old white megablocks and leave the road/coins readable.
        BuildTownhouse(
            near.transform, "LeftPastelTownhouse", -1f, -1.25f,
            2.15f + (segmentIndex % 3) * .3f,
            segmentIndex % 2 == 0 ? lavender : pink,
            segmentIndex % 2 == 0 ? peach : mint,
            window);
        BuildTownhouse(
            near.transform, "RightPastelTownhouse", 1f, 1.35f,
            2.35f + ((segmentIndex + 1) % 3) * .28f,
            segmentIndex % 2 == 0 ? mint : sky,
            segmentIndex % 2 == 0 ? pink : lavender,
            window);

        if (segmentIndex % 3 == 0)
        {
            GameObject cloudCluster = BuildCloudCluster(
                near.transform,
                "SoftCloudCluster",
                new Vector3(side * 7.1f, 4.65f + segmentIndex * .025f, 1.7f),
                cloud);
            floatingItems.Add(cloudCluster.transform);
        }

        GameObject petShop = CreateTemplateRoot("Variant_PetShop", segment);
        BuildCandyStorefront(
            petShop.transform, "PetTreatBoutique", side, .45f,
            segmentIndex % 2 == 0 ? pink : lavender, peach, edge, window);
        GameObject banner = InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Banner1 V1.prefab",
            petShop.transform,
            "PetShopBanner",
            new Vector3(side * 4.72f, .1f, -1.48f),
            new Vector3(0f, -side * 90f, 0f),
            .72f);
        InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Rack2 V1.prefab",
            petShop.transform,
            "PetShopDisplayRack",
            new Vector3(side * 4.75f, .1f, 1.63f),
            new Vector3(0f, -side * 90f, 0f),
            1.05f);
        GameObject shopSign = CreateDecorPrimitive(
            PrimitiveType.Cylinder, "SpinningPawMedallion", petShop.transform,
            new Vector3(side * 4.64f, 2.58f, -.62f), new Vector3(.31f, .055f, .31f),
            glow, glows);
        shopSign.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spinningItems.Add(shopSign.transform);
        if (banner != null)
            floatingItems.Add(banner.transform);

        GameObject toyCorner = CreateTemplateRoot("Variant_ToyCorner", segment);
        BuildCandyStorefront(
            toyCorner.transform, "ToyAndNapShop", side, .2f,
            mint, lavender, peach, window);
        InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "ScratchingPost5 V3.prefab",
            toyCorner.transform,
            "PremiumScratchingPost",
            new Vector3(side * 4.7f, .1f, 1.5f),
            new Vector3(0f, -side * 90f, 0f),
            1.25f,
            true);
        InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Bed3 V1.prefab",
            toyCorner.transform,
            "WindowPetBed",
            new Vector3(side * 4.7f, .1f, -1.45f),
            new Vector3(0f, -side * 90f, 0f),
            .48f);
        GameObject toy = InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Toy5 V3.prefab",
            toyCorner.transform,
            "BobbingCatToy",
            new Vector3(side * 4.48f, .1f, -.62f),
            Vector3.zero,
            .38f);
        if (toy != null)
            floatingItems.Add(toy.transform);
        BuildYarnBasketDisplay(
            toyCorner.transform,
            new Vector3(side * 4.55f, .1f, .55f),
            pink, lavender, peach,
            spinningItems);

        GameObject market = CreateTemplateRoot("Variant_CozyMarket", segment);
        BuildCandyStorefront(
            market.transform, "CozyPetMarket", side, .5f,
            peach, pink, mint, window);
        InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Bowl1 V3.prefab",
            market.transform,
            "TreatBowlDisplay",
            new Vector3(side * 4.52f, .1f, -1.48f),
            new Vector3(0f, -side * 90f, 0f),
            .24f);
        InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Food2 V1.prefab",
            market.transform,
            "PetFoodDisplay",
            new Vector3(side * 4.66f, .1f, -.9f),
            new Vector3(0f, -side * 90f, 0f),
            .68f);
        InstantiateDecorPrefabFitted(
            LivingRoomPrefabFolder + "Box_Open.prefab",
            market.transform,
            "OpenDeliveryBox",
            new Vector3(side * 4.72f, .1f, 1.42f),
            new Vector3(0f, -side * 90f, 0f),
            .48f);
        InstantiateDecorPrefabFitted(
            LivingRoomPrefabFolder + "PottedPlant_Small_1.prefab",
            market.transform,
            "SidewalkFlowers",
            new Vector3(side * 4.48f, .1f, .65f),
            Vector3.zero,
            .52f);
        GameObject paradeSign = CreateDecorPrimitive(
            PrimitiveType.Cylinder, "MarketPawSign", market.transform,
            new Vector3(side * 4.63f, 2.55f, -.32f), new Vector3(.3f, .055f, .3f),
            glow, glows);
        paradeSign.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spinningItems.Add(paradeSign.transform);

        GameObject catCafe = CreateTemplateRoot("Variant_CatCafe", segment);
        BuildCandyStorefront(
            catCafe.transform,
            "WhiskerCatCafe",
            side,
            .35f,
            sky,
            peach,
            pink,
            window);
        GameObject fishSign = CreateDecorPrimitive(
            PrimitiveType.Sphere,
            "CafeFishSign",
            catCafe.transform,
            new Vector3(side * 4.62f, 2.55f, -.38f),
            new Vector3(.34f, .2f, .075f),
            glow,
            glows);
        CreateDecorPrimitive(
            PrimitiveType.Cube,
            "CafeFishTail",
            catCafe.transform,
            new Vector3(side * 4.61f, 2.55f, -.73f),
            new Vector3(.15f, .06f, .15f),
            accent,
            accents).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        spinningItems.Add(fishSign.transform);
        InstantiateDecorPrefabFitted(
            LivingRoomPrefabFolder + "Chair_1.prefab",
            catCafe.transform,
            "CafePatioChair",
            new Vector3(side * 4.68f, .08f, 1.45f),
            new Vector3(0f, -side * 90f, 0f),
            .72f);
        InstantiateDecorPrefabFitted(
            PetshopPrefabFolder + "Bowl1 V3.prefab",
            catCafe.transform,
            "CafeWaterBowl",
            new Vector3(side * 4.45f, .08f, .76f),
            Vector3.zero,
            .22f);

        GameObject pawPark = CreateTemplateRoot("Variant_PawPark", segment);
        CreateDecorPrimitive(
            PrimitiveType.Cube,
            "MintParkLawn",
            pawPark.transform,
            new Vector3(side * 5.55f, .05f, .25f),
            new Vector3(2.7f, .1f, 4.8f),
            mint,
            null);
        CreateDecorPrimitive(
            PrimitiveType.Cylinder,
            "ParkTreeTrunk",
            pawPark.transform,
            new Vector3(side * 5.9f, .85f, 1.3f),
            new Vector3(.22f, .85f, .22f),
            edge,
            edges);
        CreateDecorPrimitive(
            PrimitiveType.Sphere,
            "ParkTreeCrown",
            pawPark.transform,
            new Vector3(side * 5.9f, 2.05f, 1.3f),
            new Vector3(1.05f, .9f, .85f),
            mint,
            null);
        CreateDecorPrimitive(
            PrimitiveType.Cube,
            "ParkBenchSeat",
            pawPark.transform,
            new Vector3(side * 4.72f, .48f, -.8f),
            new Vector3(.66f, .13f, 1.55f),
            peach,
            null);
        CreateDecorPrimitive(
            PrimitiveType.Cube,
            "ParkBenchBack",
            pawPark.transform,
            new Vector3(side * 5.05f, .83f, -.8f),
            new Vector3(.12f, .68f, 1.55f),
            pink,
            null);
        GameObject parkMedallion = CreateDecorPrimitive(
            PrimitiveType.Cylinder,
            "PawParkMedallion",
            pawPark.transform,
            new Vector3(side * 4.65f, 1.95f, .08f),
            new Vector3(.34f, .06f, .34f),
            glow,
            glows);
        parkMedallion.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spinningItems.Add(parkMedallion.transform);
        GameObject balloonCluster = CreateTemplateRoot(
            "ParkBalloonCluster",
            pawPark.transform);
        balloonCluster.transform.localPosition = new Vector3(side * 5.3f, 2.65f, -1.45f);
        CreateDecorPrimitive(
            PrimitiveType.Sphere,
            "PinkBalloon",
            balloonCluster.transform,
            new Vector3(-.22f, .08f, 0f),
            new Vector3(.28f, .36f, .24f),
            pink,
            null);
        CreateDecorPrimitive(
            PrimitiveType.Sphere,
            "SkyBalloon",
            balloonCluster.transform,
            new Vector3(.18f, .22f, .05f),
            new Vector3(.28f, .36f, .24f),
            sky,
            null);
        CreateDecorPrimitive(
            PrimitiveType.Sphere,
            "LemonBalloon",
            balloonCluster.transform,
            new Vector3(.02f, -.16f, -.04f),
            new Vector3(.26f, .34f, .22f),
            peach,
            null);
        floatingItems.Add(balloonCluster.transform);

        variants = new[] { petShop, toyCorner, market, catCafe, pawPark };
        floaters = floatingItems.ToArray();
        spinners = spinningItems.ToArray();
        accentRenderers = accents.ToArray();
        edgeRenderers = edges.ToArray();
        glowRenderers = glows.ToArray();
    }

    private static void BuildTownhouse(
        Transform parent,
        string name,
        float side,
        float z,
        float height,
        Material body,
        Material roof,
        Material window)
    {
        GameObject root = CreateTemplateRoot(name, parent);
        float x = side * 6.35f;
        CreateDecorPrimitive(
            PrimitiveType.Cube, "PastelFacade", root.transform,
            new Vector3(x, height * .5f + .08f, z),
            new Vector3(1.35f, height, 2.15f), body, null);
        CreateDecorPrimitive(
            PrimitiveType.Cube, "RoofCap", root.transform,
            new Vector3(x, height + .18f, z),
            new Vector3(1.55f, .18f, 2.42f), roof, null);
        float faceX = side * (6.35f - .7f);
        CreateDecorPrimitive(
            PrimitiveType.Cube, "UpperWindow", root.transform,
            new Vector3(faceX, height * .68f, z - .52f),
            new Vector3(.055f, .45f, .48f), window, null);
        CreateDecorPrimitive(
            PrimitiveType.Cube, "LowerWindow", root.transform,
            new Vector3(faceX, height * .33f, z + .5f),
            new Vector3(.055f, .38f, .5f), window, null);
        CreateDecorPrimitive(
            PrimitiveType.Cube, "WindowAwning", root.transform,
            new Vector3(side * (6.35f - .82f), height * .56f, z + .5f),
            new Vector3(.28f, .09f, .65f), roof, null);
    }

    private static void BuildCandyStorefront(
        Transform parent,
        string name,
        float side,
        float z,
        Material body,
        Material awningA,
        Material awningB,
        Material window)
    {
        GameObject root = CreateTemplateRoot(name, parent);
        float x = side * 5.45f;
        CreateDecorPrimitive(
            PrimitiveType.Cube, "StoreBody", root.transform,
            new Vector3(x, 1.22f, z),
            new Vector3(1.45f, 2.35f, 2.75f), body, null);
        CreateDecorPrimitive(
            PrimitiveType.Cube, "StoreRoof", root.transform,
            new Vector3(x, 2.5f, z),
            new Vector3(1.68f, .18f, 3.02f), awningA, null);
        float faceX = side * 4.69f;
        CreateDecorPrimitive(
            PrimitiveType.Cube, "DisplayWindow", root.transform,
            new Vector3(faceX, 1.2f, z + .3f),
            new Vector3(.055f, .9f, 1.45f), window, null);
        CreateDecorPrimitive(
            PrimitiveType.Cube, "CandyDoor", root.transform,
            new Vector3(faceX - side * .005f, .74f, z - .95f),
            new Vector3(.065f, 1.28f, .5f), awningB, null);

        for (int stripe = 0; stripe < 6; stripe++)
        {
            Material stripeMaterial = stripe % 2 == 0 ? awningA : awningB;
            GameObject piece = CreateDecorPrimitive(
                PrimitiveType.Cube, $"AwningStripe_{stripe + 1}", root.transform,
                new Vector3(side * 4.53f, 1.93f, z - 1.25f + stripe * .5f),
                new Vector3(.34f, .15f, .47f), stripeMaterial, null);
            piece.transform.localRotation = Quaternion.Euler(0f, 0f, side * 9f);
        }

        CreateDecorPrimitive(
            PrimitiveType.Cube, "SignBracket", root.transform,
            new Vector3(side * 4.62f, 2.32f, z - .7f),
            new Vector3(.22f, .08f, .58f), awningA, null);
    }

    private static GameObject BuildCloudCluster(
        Transform parent,
        string name,
        Vector3 position,
        Material material)
    {
        GameObject root = CreateTemplateRoot(name, parent);
        root.transform.localPosition = position;
        CreateDecorPrimitive(
            PrimitiveType.Sphere, "CloudCenter", root.transform,
            Vector3.zero, new Vector3(1.15f, .48f, .62f), material, null);
        CreateDecorPrimitive(
            PrimitiveType.Sphere, "CloudLeft", root.transform,
            new Vector3(-.72f, -.08f, .04f), new Vector3(.78f, .34f, .52f), material, null);
        CreateDecorPrimitive(
            PrimitiveType.Sphere, "CloudRight", root.transform,
            new Vector3(.72f, -.1f, -.03f), new Vector3(.86f, .38f, .55f), material, null);
        CreateDecorPrimitive(
            PrimitiveType.Sphere, "CloudPuff", root.transform,
            new Vector3(.15f, .25f, 0f), new Vector3(.65f, .45f, .5f), material, null);
        return root;
    }

    private static void BuildYarnBasketDisplay(
        Transform parent,
        Vector3 position,
        Material first,
        Material second,
        Material third,
        List<Transform> spinners)
    {
        GameObject root = CreateTemplateRoot("YarnBasketDisplay", parent);
        root.transform.localPosition = position;
        CreateDecorPrimitive(
            PrimitiveType.Cube, "LowBasket", root.transform,
            new Vector3(0f, .15f, 0f), new Vector3(.62f, .3f, .54f),
            GetOrCreateMaterial("RunnerBox", Wood), null);
        GameObject yarnA = CreateDecorPrimitive(
            PrimitiveType.Sphere, "CoralYarn", root.transform,
            new Vector3(-.18f, .42f, 0f), new Vector3(.28f, .28f, .28f), first, null);
        GameObject yarnB = CreateDecorPrimitive(
            PrimitiveType.Sphere, "PurpleYarn", root.transform,
            new Vector3(.14f, .45f, .08f), new Vector3(.26f, .26f, .26f), second, null);
        GameObject yarnC = CreateDecorPrimitive(
            PrimitiveType.Sphere, "PeachYarn", root.transform,
            new Vector3(.04f, .52f, -.15f), new Vector3(.22f, .22f, .22f), third, null);
        spinners.Add(yarnA.transform);
        spinners.Add(yarnB.transform);
        spinners.Add(yarnC.transform);
    }

    private static GameObject InstantiateDecorPrefabFitted(
        string assetPath,
        Transform parent,
        string name,
        Vector3 groundPosition,
        Vector3 eulerAngles,
        float targetHeight,
        bool castShadows = false)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null || parent == null)
            return null;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return null;

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = groundPosition;
        instance.transform.localRotation = Quaternion.Euler(eulerAngles);
        instance.transform.localScale = Vector3.one;

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (!TryGetCombinedBounds(renderers, out Bounds bounds))
        {
            UnityEngine.Object.DestroyImmediate(instance);
            return null;
        }

        float scale = Mathf.Clamp(targetHeight / Mathf.Max(.001f, bounds.size.y), .04f, 18f);
        instance.transform.localScale = Vector3.one * scale;
        TryGetCombinedBounds(renderers, out bounds);
        float targetWorldY = parent.TransformPoint(groundPosition).y;
        Vector3 correction = parent.InverseTransformVector(
            Vector3.up * (targetWorldY - bounds.min.y));
        instance.transform.localPosition += correction;

        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
        foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            UnityEngine.Object.DestroyImmediate(body);
        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = castShadows
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
            renderer.receiveShadows = castShadows;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        return instance;
    }

    private static bool TryGetCombinedBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        if (renderers == null)
            return false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }

    private static void BuildPawLamp(
        Transform parent,
        Vector3 basePosition,
        Material edge,
        Material glow,
        List<Renderer> accents,
        List<Renderer> edges,
        List<Renderer> glows)
    {
        CreateDecorPrimitive(
            PrimitiveType.Cylinder, "PawLampPost", parent,
            basePosition + new Vector3(0f, .48f, 0f), new Vector3(.075f, .48f, .075f),
            edge, edges);
        CreateDecorPrimitive(
            PrimitiveType.Sphere, "PawLampGlow", parent,
            basePosition + new Vector3(0f, 1.02f, 0f), new Vector3(.28f, .28f, .28f),
            glow, glows);
        CreateDecorPrimitive(
            PrimitiveType.Sphere, "PawLampCollar", parent,
            basePosition + new Vector3(0f, .8f, 0f), new Vector3(.16f, .1f, .16f),
            edge, accents);
    }

    private static GameObject CreateDecorPrimitive(
        PrimitiveType type,
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        List<Renderer> category)
    {
        GameObject item = CreatePrimitive(
            type, name, parent, localPosition, localScale, material);
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            category?.Add(renderer);
        }
        return item;
    }

    private static void BuildUi(
        Transform parent,
        out Canvas canvas,
        out TMP_Text timer,
        out TMP_Text coins,
        out TMP_Text distance,
        out TMP_Text bonus,
        out TMP_Text chances,
        out TMP_Text countdown,
        out GameObject welcome,
        out TMP_Text welcomeBest,
        out TMP_Text welcomeEnergy,
        out Button welcomeStart,
        out Button welcomeExit,
        out GameObject results,
        out TMP_Text resultTitle,
        out TMP_Text resultDetails,
        out Button collect,
        out Button retry,
        out CanvasGroup hitFlash,
        out RunnerUiExtras extras)
    {
        extras = new RunnerUiExtras();
        var canvasObject = new GameObject(
            "RunnerCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject safeAreaObject = CreateUiObject("SafeArea", canvasObject.transform);
        RectTransform safeArea = safeAreaObject.GetComponent<RectTransform>();
        Stretch(safeArea);
        safeAreaObject.AddComponent<SafeAreaRect>();
        extras.SafeArea = safeArea;

        // Harmonised HUD grid: three equal-width stat capsules on top (coins /
        // time / distance) and a matching secondary row below (chances / stage •
        // happy / score) whose left and right pills line up under the top row.
        // Column centres are symmetric (+-250) so nothing drifts or overlaps.
        const float columnCenter = 250f;
        const float topPillWidth = 300f;
        const float topPillHeight = 76f;
        const float subPillWidth = 300f;
        const float subPillHeight = 50f;
        const float subRowY = -140f;

        GameObject topBar = CreateUiObject("TopBar", safeAreaObject.transform);
        RectTransform topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(0f, 96f);
        topRect.anchoredPosition = Vector2.zero;
        extras.TopHud = topRect;

        GameObject coinHudCapsule = CreatePremiumPanel(
            topBar.transform, "CoinHudCapsule", new Color32(113, 58, 181, 242), 30f);
        SetAnchored(coinHudCapsule.GetComponent<RectTransform>(), new Vector2(0f, .5f),
            new Vector2(columnCenter, 0f), new Vector2(topPillWidth, topPillHeight));
        GameObject timerHudCapsule = CreatePremiumPanel(
            topBar.transform, "TimerHudCapsule", new Color32(255, 91, 157, 242), 30f);
        SetAnchored(timerHudCapsule.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(topPillWidth, topPillHeight));
        GameObject distanceHudCapsule = CreatePremiumPanel(
            topBar.transform, "DistanceHudCapsule", new Color32(31, 191, 185, 242), 30f);
        SetAnchored(distanceHudCapsule.GetComponent<RectTransform>(), new Vector2(1f, .5f),
            new Vector2(-columnCenter, 0f), new Vector2(topPillWidth, topPillHeight));

        // Coin badge + value share the left capsule: badge pinned to the inner
        // left, value centred in the space to its right.
        GameObject coinBadge = CreatePremiumPanel(
            coinHudCapsule.transform, "PawCoinBadge", Gold, 26f);
        SetAnchored(coinBadge.GetComponent<RectTransform>(), new Vector2(0f, .5f),
            new Vector2(46f, 0f), new Vector2(56f, 56f));
        if (!PremiumUiFactory.BuildCurrencyIcon(
                coinBadge.transform, PremiumUiFactory.CurrencyVisual.Coin, true))
        {
            TMP_Text coinMark = CreateLabel(
                coinBadge.transform, "PawMark", "PAW", 13f, new Color32(111, 63, 37, 255));
            Stretch(coinMark.rectTransform);
            coinMark.alignment = TextAlignmentOptions.Center;
            coinMark.fontStyle = FontStyles.Bold;
        }
        coins = CreateLabel(coinHudCapsule.transform, "CoinLabel", "0", 34f, Gold);
        SetAnchored(coins.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(36f, 0f),
            new Vector2(topPillWidth - 120f, topPillHeight - 12f));
        coins.alignment = TextAlignmentOptions.Center;
        coins.fontStyle = FontStyles.Bold;

        timer = CreateLabel(timerHudCapsule.transform, "TimerLabel", "00:00", 42f, Cream);
        Stretch(timer.rectTransform);
        timer.alignment = TextAlignmentOptions.Center;
        timer.fontStyle = FontStyles.Bold;

        distance = CreateLabel(distanceHudCapsule.transform, "DistanceLabel", "0 m", 34f, Cream);
        Stretch(distance.rectTransform);
        distance.alignment = TextAlignmentOptions.Center;
        distance.fontStyle = FontStyles.Bold;

        GameObject chancesPanel = CreatePremiumPanel(
            safeAreaObject.transform,
            "ChancesPanel",
            new Color32(255, 112, 116, 235), 20f);
        SetAnchored(
            chancesPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 1f),
            new Vector2(columnCenter, subRowY),
            new Vector2(subPillWidth, subPillHeight));
        chances = CreateLabel(
            chancesPanel.transform, "ChancesLabel", "CHANCES  3/3", 24f, Cream);
        Stretch(chances.rectTransform);
        chances.alignment = TextAlignmentOptions.Center;
        chances.fontStyle = FontStyles.Bold;

        GameObject bonusPanel = CreatePremiumPanel(
            safeAreaObject.transform,
            "HappyBonusPanel",
            new Color32(47, 192, 184, 235), 20f);
        SetAnchored(
            bonusPanel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f),
            new Vector2(0f, subRowY),
            new Vector2(560f, subPillHeight));
        bonus = CreateLabel(bonusPanel.transform, "HappyBonusLabel", "STAGE 1   •   HAPPY CAT +0%", 23f, Cream);
        Stretch(bonus.rectTransform);
        bonus.alignment = TextAlignmentOptions.Center;
        bonus.fontStyle = FontStyles.Bold;

        GameObject scorePanel = CreatePremiumPanel(
            safeAreaObject.transform,
            "ScorePanel",
            new Color32(118, 77, 197, 235),
            20f);
        SetAnchored(
            scorePanel.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(-columnCenter, subRowY),
            new Vector2(subPillWidth, subPillHeight));
        extras.Score = CreateLabel(
            scorePanel.transform, "ScoreLabel", "SCORE  0", 24f, Cream);
        Stretch(extras.Score.rectTransform);
        extras.Score.alignment = TextAlignmentOptions.Center;
        extras.Score.fontStyle = FontStyles.Bold;

        extras.Pause = CreateButton(
            safeAreaObject.transform,
            "PauseButton",
            "II",
            new Color32(120, 79, 197, 255),
            new Vector2(66f, 62f));
        SetAnchored(
            extras.Pause.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(-44f, -48f),
            new Vector2(66f, 62f));
        extras.Pause.GetComponentInChildren<TMP_Text>(true).fontSize = 26f;

        GameObject comboPanel = CreatePremiumPanel(
            safeAreaObject.transform,
            "ComboPanel",
            new Color32(255, 174, 55, 238),
            20f);
        SetAnchored(
            comboPanel.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(0f, -202f),
            new Vector2(290f, 48f));
        extras.Combo = CreateLabel(
            comboPanel.transform, "ComboLabel", "COMBO  x2", 25f, Dark);
        Stretch(extras.Combo.rectTransform);
        extras.Combo.alignment = TextAlignmentOptions.Center;
        extras.Combo.fontStyle = FontStyles.Bold;

        GameObject powerUpPanel = CreatePremiumPanel(
            safeAreaObject.transform,
            "PowerUpPanel",
            new Color32(50, 210, 190, 238),
            20f);
        SetAnchored(
            powerUpPanel.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(0f, -258f),
            new Vector2(620f, 48f));
        extras.PowerUps = CreateLabel(
            powerUpPanel.transform, "PowerUpLabel", "MAGNET 9s", 22f, Dark);
        Stretch(extras.PowerUps.rectTransform);
        extras.PowerUps.alignment = TextAlignmentOptions.Center;
        extras.PowerUps.fontStyle = FontStyles.Bold;

        countdown = CreateLabel(safeAreaObject.transform, "CountdownLabel", "3", 110f, Cream);
        SetAnchored(countdown.rectTransform, new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(500f, 180f));
        countdown.alignment = TextAlignmentOptions.Center;
        countdown.fontStyle = FontStyles.Bold;

        welcome = CreatePanel(
            canvasObject.transform, "WelcomePanel", new Color32(52, 44, 137, 238));
        Stretch(welcome.GetComponent<RectTransform>());
        GameObject candyLeft = CreatePanel(
            welcome.transform, "CandyGlowLeft", new Color32(54, 227, 216, 118));
        SetAnchored(candyLeft.GetComponent<RectTransform>(), new Vector2(0f, .5f),
            new Vector2(85f, 0f), new Vector2(390f, 1240f));
        candyLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -17f);
        GameObject candyRight = CreatePanel(
            welcome.transform, "CandyGlowRight", new Color32(255, 100, 177, 124));
        SetAnchored(candyRight.GetComponent<RectTransform>(), new Vector2(1f, .5f),
            new Vector2(-70f, 0f), new Vector2(390f, 1240f));
        candyRight.transform.localRotation = Quaternion.Euler(0f, 0f, 17f);

        GameObject welcomeSafeObject = CreateUiObject("WelcomeSafeArea", welcome.transform);
        RectTransform welcomeSafe = welcomeSafeObject.GetComponent<RectTransform>();
        Stretch(welcomeSafe);
        welcomeSafeObject.AddComponent<SafeAreaRect>();
        GameObject welcomeLayout = CreateUiObject(
            "WelcomeCardLayout",
            welcomeSafeObject.transform);
        RectTransform welcomeLayoutRect = welcomeLayout.GetComponent<RectTransform>();
        SetAnchored(
            welcomeLayoutRect,
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(1120f, 790f));
        extras.WelcomeCard = welcomeLayoutRect;

        GameObject welcomeGlow = CreatePremiumPanel(
            welcomeLayout.transform, "WelcomeCardGlow", new Color32(255, 221, 86, 220), 48f);
        SetAnchored(welcomeGlow.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(1120f, 790f));
        GameObject welcomeCard = CreatePremiumPanel(
            welcomeLayout.transform, "WelcomeCard", new Color32(255, 242, 211, 255), 42f);
        SetAnchored(welcomeCard.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(1100f, 770f));
        GameObject cardHeader = CreatePremiumPanel(
            welcomeCard.transform, "CardHeader", new Color32(255, 101, 154, 255), 34f);
        RectTransform cardHeaderRect = cardHeader.GetComponent<RectTransform>();
        cardHeaderRect.anchorMin = new Vector2(0f, 1f);
        cardHeaderRect.anchorMax = new Vector2(1f, 1f);
        cardHeaderRect.pivot = new Vector2(.5f, 1f);
        cardHeaderRect.sizeDelta = new Vector2(0f, 170f);
        cardHeaderRect.anchoredPosition = Vector2.zero;
        TMP_Text eyebrow = CreateLabel(
            cardHeader.transform, "Eyebrow", "A PURRFECT ADVENTURE", 20f, Cream);
        SetAnchored(eyebrow.rectTransform, new Vector2(.5f, 1f),
            new Vector2(0f, -36f), new Vector2(600f, 30f));
        eyebrow.alignment = TextAlignmentOptions.Center;
        eyebrow.fontStyle = FontStyles.Bold;
        TMP_Text welcomeTitle = CreateLabel(
            cardHeader.transform, "WelcomeTitle", "CAT RUNNER", 64f, Cream);
        SetAnchored(welcomeTitle.rectTransform, new Vector2(.5f, 1f),
            new Vector2(0f, -100f), new Vector2(680f, 82f));
        welcomeTitle.alignment = TextAlignmentOptions.Center;
        welcomeTitle.fontStyle = FontStyles.Bold;

        ConfigureRunnerHeroImporter();
        Texture2D runnerHero = AssetDatabase.LoadAssetAtPath<Texture2D>(RunnerHeroPath);
        if (runnerHero != null)
        {
            GameObject heroFrame = CreatePremiumPanel(
                welcomeCard.transform, "WelcomeHeroFrame", new Color32(255, 205, 64, 255), 34f);
            SetAnchored(heroFrame.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
                new Vector2(-274f, -64f), new Vector2(516f, 516f));
            Mask heroMask = heroFrame.AddComponent<Mask>();
            heroMask.showMaskGraphic = true;
            GameObject heroObject = CreateUiObject("WelcomeHeroArt", heroFrame.transform);
            RawImage heroImage = heroObject.AddComponent<RawImage>();
            heroImage.texture = runnerHero;
            heroImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            heroImage.color = Color.white;
            heroImage.raycastTarget = false;
            StretchWithOffsets(heroImage.rectTransform, 8f, 8f, -8f, -8f);

            GameObject heroGlint = CreatePremiumPanel(
                heroFrame.transform, "HeroGlintSparkle", new Color32(255, 255, 255, 96), 16f);
            SetAnchored(heroGlint.GetComponent<RectTransform>(), new Vector2(.72f, .82f),
                Vector2.zero, new Vector2(94f, 14f));
            heroGlint.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        }

        TMP_Text tagline = CreateLabel(
            welcomeCard.transform, "Tagline",
            "CHASE PAW COINS  •  DODGE CAT TOYS  •  BEAT YOUR BEST", 20f, Dark);
        SetAnchored(tagline.rectTransform, new Vector2(.5f, 1f),
            new Vector2(270f, -230f), new Vector2(470f, 66f));
        tagline.alignment = TextAlignmentOptions.Center;
        tagline.fontStyle = FontStyles.Bold;

        GameObject scorePill = CreatePremiumPanel(
            welcomeCard.transform, "BestScorePill", new Color32(100, 59, 167, 255), 28f);
        SetAnchored(scorePill.GetComponent<RectTransform>(), new Vector2(.5f, 1f),
            new Vector2(270f, -326f), new Vector2(430f, 82f));
        welcomeBest = CreateLabel(
            scorePill.transform, "BestScore", "BEST SCORE   0", 31f, Gold);
        Stretch(welcomeBest.rectTransform);
        welcomeBest.alignment = TextAlignmentOptions.Center;
        welcomeBest.fontStyle = FontStyles.Bold;

        welcomeEnergy = CreateLabel(
            welcomeCard.transform, "WelcomeEnergy", "1 LIFE PER RUN", 18f, TealDark);
        SetAnchored(welcomeEnergy.rectTransform, new Vector2(.5f, 1f),
            new Vector2(270f, -394f), new Vector2(470f, 44f));
        welcomeEnergy.alignment = TextAlignmentOptions.Center;
        welcomeEnergy.fontStyle = FontStyles.Bold;

        GameObject missionPill = CreatePremiumPanel(
            welcomeCard.transform,
            "DailyMissionPill",
            new Color32(91, 210, 191, 255),
            24f);
        SetAnchored(
            missionPill.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(270f, -475f),
            new Vector2(470f, 80f));
        extras.WelcomeMissions = CreateLabel(
            missionPill.transform,
            "DailyMissions",
            "DAILY MISSIONS  •  COINS 0/50  •  JUMPS 0/10  •  DISTANCE 0/500 m",
            15f,
            Dark);
        StretchWithOffsets(extras.WelcomeMissions.rectTransform, 12f, 6f, -12f, -6f);
        extras.WelcomeMissions.alignment = TextAlignmentOptions.Center;
        extras.WelcomeMissions.fontStyle = FontStyles.Bold;

        welcomeStart = CreateButton(
            welcomeCard.transform, "WelcomeStartButton", "START RUN", Orange,
            new Vector2(430f, 94f));
        SetAnchored(welcomeStart.GetComponent<RectTransform>(), new Vector2(.5f, 0f),
            new Vector2(270f, 175f), new Vector2(430f, 94f));
        welcomeStart.GetComponentInChildren<TMP_Text>(true).fontSize = 36f;
        extras.WelcomeRewarded = CreateButton(
            welcomeCard.transform,
            "WelcomeRewardedEnergyButton",
            "WATCH • LIVES +2",
            Teal,
            new Vector2(430f, 94f));
        SetAnchored(
            extras.WelcomeRewarded.GetComponent<RectTransform>(),
            new Vector2(.5f, 0f),
            new Vector2(270f, 175f),
            new Vector2(430f, 94f));
        extras.WelcomeRewarded.GetComponentInChildren<TMP_Text>(true).fontSize = 28f;
        extras.WelcomeRewarded.gameObject.SetActive(false);
        welcomeExit = CreateButton(
            welcomeCard.transform, "WelcomeExitButton", "EXIT TO MAIN MENU", Teal,
            new Vector2(430f, 76f));
        SetAnchored(welcomeExit.GetComponent<RectTransform>(), new Vector2(.5f, 0f),
            new Vector2(270f, 80f), new Vector2(430f, 76f));
        TMP_Text controls = CreateLabel(
            welcomeCard.transform,
            "Controls",
            "DRAG TO MOVE  •  SWIPE UP TO JUMP  •  SWIPE DOWN TO SLIDE",
            14f,
            Dark);
        SetAnchored(controls.rectTransform, new Vector2(.5f, 0f),
            new Vector2(270f, 23f), new Vector2(480f, 34f));
        controls.alignment = TextAlignmentOptions.Center;

        results = CreatePanel(canvasObject.transform, "ResultsPanel", new Color32(70, 34, 132, 232));
        Stretch(results.GetComponent<RectTransform>());

        GameObject resultRibbonLeft = CreatePanel(
            results.transform, "ResultCandyRibbonLeft", new Color32(47, 233, 218, 108));
        SetAnchored(resultRibbonLeft.GetComponent<RectTransform>(), new Vector2(0f, .5f),
            new Vector2(100f, 0f), new Vector2(430f, 1260f));
        resultRibbonLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject resultRibbonRight = CreatePanel(
            results.transform, "ResultCandyRibbonRight", new Color32(255, 104, 171, 112));
        SetAnchored(resultRibbonRight.GetComponent<RectTransform>(), new Vector2(1f, .5f),
            new Vector2(-90f, 0f), new Vector2(430f, 1260f));
        resultRibbonRight.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

        GameObject resultsSafeObject = CreateUiObject("ResultsSafeArea", results.transform);
        RectTransform resultsSafe = resultsSafeObject.GetComponent<RectTransform>();
        Stretch(resultsSafe);
        resultsSafeObject.AddComponent<SafeAreaRect>();
        GameObject resultsLayout = CreateUiObject(
            "ResultsCardLayout",
            resultsSafeObject.transform);
        RectTransform resultsLayoutRect = resultsLayout.GetComponent<RectTransform>();
        SetAnchored(
            resultsLayoutRect,
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(720f, 800f));
        extras.ResultsCard = resultsLayoutRect;

        GameObject resultCardGlow = CreatePremiumPanel(
            resultsLayout.transform, "ResultsCardGlow", new Color32(255, 210, 64, 225), 48f);
        SetAnchored(resultCardGlow.GetComponent<RectTransform>(), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(720f, 800f));

        GameObject card = CreatePremiumPanel(
            resultsLayout.transform, "ResultsCard", new Color32(255, 239, 202, 255), 46f);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(696f, 776f);
        cardRect.anchoredPosition = Vector2.zero;

        GameObject resultHeader = CreatePremiumPanel(
            card.transform, "ResultsHeader", new Color32(255, 92, 157, 255), 34f);
        RectTransform resultHeaderRect = resultHeader.GetComponent<RectTransform>();
        resultHeaderRect.anchorMin = new Vector2(0f, 1f);
        resultHeaderRect.anchorMax = new Vector2(1f, 1f);
        resultHeaderRect.pivot = new Vector2(.5f, 1f);
        resultHeaderRect.sizeDelta = new Vector2(0f, 138f);
        resultHeaderRect.anchoredPosition = Vector2.zero;

        resultTitle = CreateLabel(resultHeader.transform, "ResultTitle", "RUN OVER", 48f, Cream);
        Stretch(resultTitle.rectTransform);
        resultTitle.alignment = TextAlignmentOptions.Center;
        resultTitle.fontStyle = FontStyles.Bold;

        extras.NewBestBadge = CreatePremiumPanel(
            card.transform,
            "NewBestBadge",
            new Color32(255, 205, 54, 255),
            22f);
        SetAnchored(
            extras.NewBestBadge.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(0f, -151f),
            new Vector2(260f, 52f));
        TMP_Text newBestText = CreateLabel(
            extras.NewBestBadge.transform,
            "NewBestLabel",
            "NEW BEST!",
            25f,
            Dark);
        Stretch(newBestText.rectTransform);
        newBestText.alignment = TextAlignmentOptions.Center;
        newBestText.fontStyle = FontStyles.Bold;

        GameObject resultStatsSurface = CreatePremiumPanel(
            card.transform,
            "ResultStatsSurface",
            new Color32(255, 250, 231, 255),
            30f);
        SetAnchored(
            resultStatsSurface.GetComponent<RectTransform>(),
            new Vector2(.5f, .62f),
            new Vector2(0f, -16f),
            new Vector2(570f, 334f));

        resultDetails = CreateLabel(card.transform, "ResultDetails", string.Empty, 30f, Dark);
        SetAnchored(
            resultDetails.rectTransform,
            new Vector2(0.5f, 0.62f),
            new Vector2(0f, -16f),
            new Vector2(560f, 310f));
        resultDetails.alignment = TextAlignmentOptions.Center;
        resultDetails.lineSpacing = 18f;

        GameObject resultMissionPill = CreatePremiumPanel(
            card.transform,
            "ResultMissionPill",
            new Color32(100, 215, 194, 255),
            22f);
        SetAnchored(
            resultMissionPill.GetComponent<RectTransform>(),
            new Vector2(.5f, 0f),
            new Vector2(0f, 210f),
            new Vector2(560f, 56f));
        extras.ResultMissions = CreateLabel(
            resultMissionPill.transform,
            "ResultMissions",
            "DAILY MISSIONS",
            14f,
            Dark);
        StretchWithOffsets(extras.ResultMissions.rectTransform, 10f, 4f, -10f, -4f);
        extras.ResultMissions.alignment = TextAlignmentOptions.Center;
        extras.ResultMissions.fontStyle = FontStyles.Bold;

        collect = CreateButton(card.transform, "CollectButton", "HOME", Orange, new Vector2(450f, 82f));
        SetAnchored(collect.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 126f), new Vector2(450f, 82f));
        retry = CreateButton(card.transform, "RetryButton", "RUN AGAIN", Teal, new Vector2(450f, 72f));
        SetAnchored(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(450f, 72f));

        extras.PausePanel = CreatePanel(
            canvasObject.transform,
            "PausePanel",
            new Color32(35, 35, 88, 220));
        Stretch(extras.PausePanel.GetComponent<RectTransform>());
        GameObject pauseRibbonLeft = CreatePanel(
            extras.PausePanel.transform,
            "PauseAquaRibbon",
            new Color32(50, 224, 211, 105));
        SetAnchored(
            pauseRibbonLeft.GetComponent<RectTransform>(),
            new Vector2(0f, .5f),
            new Vector2(110f, 0f),
            new Vector2(420f, 1260f));
        pauseRibbonLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject pauseRibbonRight = CreatePanel(
            extras.PausePanel.transform,
            "PausePinkRibbon",
            new Color32(255, 102, 169, 110));
        SetAnchored(
            pauseRibbonRight.GetComponent<RectTransform>(),
            new Vector2(1f, .5f),
            new Vector2(-100f, 0f),
            new Vector2(420f, 1260f));
        pauseRibbonRight.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

        GameObject pauseSafeObject = CreateUiObject(
            "PauseSafeArea",
            extras.PausePanel.transform);
        RectTransform pauseSafe = pauseSafeObject.GetComponent<RectTransform>();
        Stretch(pauseSafe);
        pauseSafeObject.AddComponent<SafeAreaRect>();
        GameObject pauseLayout = CreateUiObject(
            "PauseCardLayout",
            pauseSafeObject.transform);
        RectTransform pauseLayoutRect = pauseLayout.GetComponent<RectTransform>();
        SetAnchored(
            pauseLayoutRect,
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(770f, 720f));
        extras.PauseCard = pauseLayoutRect;
        GameObject pauseGlow = CreatePremiumPanel(
            pauseLayout.transform,
            "PauseCardGlow",
            new Color32(255, 211, 65, 225),
            46f);
        SetAnchored(
            pauseGlow.GetComponent<RectTransform>(),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(770f, 720f));
        GameObject pauseCard = CreatePremiumPanel(
            pauseLayout.transform,
            "PauseCard",
            new Color32(255, 241, 211, 255),
            42f);
        SetAnchored(
            pauseCard.GetComponent<RectTransform>(),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(748f, 698f));
        GameObject pauseHeader = CreatePremiumPanel(
            pauseCard.transform,
            "PauseHeader",
            new Color32(255, 98, 158, 255),
            32f);
        RectTransform pauseHeaderRect = pauseHeader.GetComponent<RectTransform>();
        pauseHeaderRect.anchorMin = new Vector2(0f, 1f);
        pauseHeaderRect.anchorMax = new Vector2(1f, 1f);
        pauseHeaderRect.pivot = new Vector2(.5f, 1f);
        pauseHeaderRect.sizeDelta = new Vector2(0f, 132f);
        pauseHeaderRect.anchoredPosition = Vector2.zero;
        TMP_Text pauseTitle = CreateLabel(
            pauseHeader.transform, "PauseTitle", "RUN PAUSED", 50f, Cream);
        Stretch(pauseTitle.rectTransform);
        pauseTitle.alignment = TextAlignmentOptions.Center;
        pauseTitle.fontStyle = FontStyles.Bold;

        extras.Resume = CreateButton(
            pauseCard.transform,
            "ResumeButton",
            "RESUME RUN",
            Orange,
            new Vector2(520f, 82f));
        SetAnchored(
            extras.Resume.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(0f, -190f),
            new Vector2(520f, 82f));
        extras.ReducedMotion = CreateButton(
            pauseCard.transform,
            "ReducedMotionButton",
            "REDUCED MOTION  OFF",
            new Color32(122, 86, 205, 255),
            new Vector2(520f, 68f));
        SetAnchored(
            extras.ReducedMotion.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(0f, -292f),
            new Vector2(520f, 68f));
        extras.Sound = CreateButton(
            pauseCard.transform,
            "SoundButton",
            "SOUND  ON",
            Teal,
            new Vector2(520f, 68f));
        SetAnchored(
            extras.Sound.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(0f, -378f),
            new Vector2(520f, 68f));
        extras.Haptics = CreateButton(
            pauseCard.transform,
            "HapticsButton",
            "HAPTICS  ON",
            new Color32(255, 163, 73, 255),
            new Vector2(520f, 68f));
        SetAnchored(
            extras.Haptics.GetComponent<RectTransform>(),
            new Vector2(.5f, 1f),
            new Vector2(0f, -464f),
            new Vector2(520f, 68f));
        extras.PauseExit = CreateButton(
            pauseCard.transform,
            "PauseExitButton",
            "EXIT TO MAIN MENU",
            new Color32(233, 93, 125, 255),
            new Vector2(520f, 72f));
        SetAnchored(
            extras.PauseExit.GetComponent<RectTransform>(),
            new Vector2(.5f, 0f),
            new Vector2(0f, 58f),
            new Vector2(520f, 72f));

        extras.TutorialPanel = CreateUiObject(
            "RunnerTutorialPanel",
            canvasObject.transform);
        Stretch(extras.TutorialPanel.GetComponent<RectTransform>());
        GameObject tutorialSafeObject = CreateUiObject(
            "TutorialSafeArea",
            extras.TutorialPanel.transform);
        RectTransform tutorialSafe = tutorialSafeObject.GetComponent<RectTransform>();
        Stretch(tutorialSafe);
        tutorialSafeObject.AddComponent<SafeAreaRect>();
        GameObject tutorialLayout = CreateUiObject(
            "TutorialCardLayout",
            tutorialSafeObject.transform);
        RectTransform tutorialLayoutRect = tutorialLayout.GetComponent<RectTransform>();
        SetAnchored(
            tutorialLayoutRect,
            new Vector2(.5f, 1f),
            new Vector2(0f, -170f),
            new Vector2(760f, 194f));
        extras.TutorialCard = tutorialLayoutRect;
        GameObject tutorialGlow = CreatePremiumPanel(
            tutorialLayout.transform,
            "TutorialGlow",
            new Color32(255, 209, 61, 225),
            30f);
        SetAnchored(
            tutorialGlow.GetComponent<RectTransform>(),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(760f, 194f));
        tutorialGlow.GetComponent<LowPolyPanelGraphic>().raycastTarget = false;
        GameObject tutorialCard = CreatePremiumPanel(
            tutorialLayout.transform,
            "TutorialCard",
            new Color32(255, 241, 211, 255),
            28f);
        SetAnchored(
            tutorialCard.GetComponent<RectTransform>(),
            new Vector2(.5f, .5f),
            Vector2.zero,
            new Vector2(744f, 180f));
        tutorialCard.GetComponent<LowPolyPanelGraphic>().raycastTarget = false;
        extras.TutorialText = CreateLabel(
            tutorialCard.transform,
            "TutorialMessage",
            "DRAG LEFT OR RIGHT\nCHANGE LANES",
            30f,
            Dark);
        SetAnchored(
            extras.TutorialText.rectTransform,
            new Vector2(.44f, .5f),
            new Vector2(-34f, 0f),
            new Vector2(500f, 136f));
        extras.TutorialText.alignment = TextAlignmentOptions.Center;
        extras.TutorialText.fontStyle = FontStyles.Bold;
        extras.TutorialSkip = CreateButton(
            tutorialCard.transform,
            "TutorialSkipButton",
            "SKIP",
            new Color32(125, 87, 205, 255),
            new Vector2(150f, 60f));
        SetAnchored(
            extras.TutorialSkip.GetComponent<RectTransform>(),
            new Vector2(.86f, .5f),
            Vector2.zero,
            new Vector2(150f, 60f));

        GameObject flashObject = CreatePanel(
            canvasObject.transform, "RunnerHitFlash", new Color32(255, 72, 139, 255));
        Stretch(flashObject.GetComponent<RectTransform>());
        Image flashImage = flashObject.GetComponent<Image>();
        flashImage.raycastTarget = false;
        hitFlash = flashObject.AddComponent<CanvasGroup>();
        hitFlash.alpha = 0f;
        hitFlash.blocksRaycasts = false;
        hitFlash.interactable = false;

        TMP_FontAsset premiumFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        PremiumUiFactory.PolishHierarchy(canvasObject.transform, premiumFont);
        results.SetActive(false);
        extras.PausePanel.SetActive(false);
        extras.TutorialPanel.SetActive(false);
        extras.NewBestBadge.SetActive(false);
        extras.Combo.transform.parent.gameObject.SetActive(false);
        extras.PowerUps.transform.parent.gameObject.SetActive(false);
    }

    private static CatRunnerFeedbackController BuildFeedback(
        Transform parent,
        CatRunnerPlayer player,
        CatRunnerCameraRig cameraRig,
        TMP_Text coinLabel,
        TMP_Text countdownLabel,
        CanvasGroup hitFlash)
    {
        GameObject feedbackObject = new GameObject("RunnerFeedback");
        feedbackObject.transform.SetParent(parent, false);
        CatRunnerFeedbackController feedback =
            feedbackObject.AddComponent<CatRunnerFeedbackController>();

        Mesh sphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
        Material goldFx = GetOrCreateGlowMaterial(
            "RunnerFxGold", new Color32(255, 211, 54, 255), true);
        Material pinkFx = GetOrCreateGlowMaterial(
            "RunnerFxPink", new Color32(255, 78, 166, 255));
        Material creamFx = GetOrCreateGlowMaterial(
            "RunnerFxCream", new Color32(255, 244, 210, 255));
        Material cyanFx = GetOrCreateGlowMaterial(
            "RunnerFxCyan", new Color32(63, 239, 230, 255));

        ParticleSystem coinBurst = CreateMeshBurst(
            feedbackObject.transform, "CoinPawBurst", sphereMesh, goldFx,
            .55f, 3.2f, .105f, 0.2f);
        ParticleSystem coinSparkles = CreateMeshBurst(
            feedbackObject.transform, "CoinPrismSparkles", sphereMesh, cyanFx,
            .68f, 4.1f, .065f, -.04f);
        ParticleSystem hitBurst = CreateMeshBurst(
            feedbackObject.transform, "HitCandyStars", sphereMesh, pinkFx,
            .7f, 3.8f, .13f, 0.08f);
        ParticleSystem landingDust = CreateMeshBurst(
            feedbackObject.transform, "LandingPawDust", sphereMesh, creamFx,
            .42f, 1.45f, .115f, 0.35f);
        ParticleSystem slideDust = CreateMeshBurst(
            feedbackObject.transform, "SlideCandyDust", sphereMesh, creamFx,
            .38f, 1.8f, .075f, .15f);
        ParticleSystem speedStreaks = CreateSpeedStreaks(
            feedbackObject.transform, cyanFx);

        feedback.EditorConfigure(
            player,
            cameraRig,
            coinLabel,
            countdownLabel,
            hitFlash,
            coinBurst,
            coinSparkles,
            hitBurst,
            landingDust,
            slideDust,
            speedStreaks);
        return feedback;
    }

    private static ParticleSystem CreateMeshBurst(
        Transform parent,
        string name,
        Mesh mesh,
        Material material,
        float lifetime,
        float speed,
        float size,
        float gravity)
    {
        GameObject particleObject = new GameObject(name, typeof(ParticleSystem));
        particleObject.transform.SetParent(parent, false);
        ParticleSystem system = particleObject.GetComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = size;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 36;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .12f;

        ParticleSystemRenderer renderer =
            particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return system;
    }

    private static ParticleSystem CreateSpeedStreaks(
        Transform parent,
        Material material)
    {
        GameObject particleObject = new GameObject("SideSpeedStreaks", typeof(ParticleSystem));
        particleObject.transform.SetParent(parent, false);
        particleObject.transform.localPosition = new Vector3(0f, 1.25f, 14f);
        particleObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        ParticleSystem system = particleObject.GetComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = .8f;
        main.startSpeed = 21f;
        main.startSize = .065f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 32;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;
        emission.rateOverTime = 18f;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7.2f, 2.4f, .15f);

        ParticleSystemRenderer renderer =
            particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 3.4f;
        renderer.velocityScale = .09f;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return system;
    }

    private static Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject temporary = GameObject.CreatePrimitive(type);
        Mesh mesh = temporary.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(temporary);
        return mesh;
    }

    private static GameObject CreateCatEar(
        Transform parent,
        string name,
        float x,
        float y,
        Material material,
        float size = .55f,
        float depth = .25f)
    {
        GameObject ear = CreatePrimitive(
            PrimitiveType.Cube, name, parent,
            new Vector3(x, y, 0f),
            new Vector3(size, size, Mathf.Max(.025f, depth)),
            material);
        ear.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        return ear;
    }

    private static GameObject CreateTemplateRoot(string name, Transform parent)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        return root;
    }

    private static GameObject CreatePrimitive(
        PrimitiveType type,
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = localScale;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
        return item;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        // Every object created through this helper is a UGUI object. Custom
        // Graphic subclasses do not consistently receive their inherited
        // CanvasRenderer dependency when added by an editor script in Unity 6.
        // Author the dependency at the common creation point so panels,
        // buttons, labels and future dock elements cannot enter the scene in
        // an invalid raycast state.
        var gameObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = CreateUiObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static GameObject CreatePremiumPanel(
        Transform parent,
        string name,
        Color color,
        float cornerCut,
        bool raycastTarget = false)
    {
        GameObject panel = CreateUiObject(name, parent);
        LowPolyPanelGraphic graphic = panel.AddComponent<LowPolyPanelGraphic>();
        PremiumUiStyle.ConfigureAccentSurface(
            graphic,
            Color.Lerp(color, Color.white, .1f),
            Color.Lerp(color, PremiumUiStyle.Night, .08f),
            cornerCut,
            3f);
        graphic.raycastTarget = raycastTarget;
        return panel;
    }

    private static TMP_Text CreateLabel(
        Transform parent,
        string name,
        string value,
        float fontSize,
        Color color)
    {
        GameObject labelObject = CreateUiObject(name, parent);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = fontSize;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string labelText,
        Color background,
        Vector2 size)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        LowPolyPanelGraphic image = buttonObject.AddComponent<LowPolyPanelGraphic>();
        Color highlight = Color.Lerp(background, Color.white, 0.2f);
        Color depth = Color.Lerp(background, PremiumUiStyle.Night, 0.16f);
        PremiumUiStyle.ConfigureAccentSurface(image, highlight, depth, 34f, 5f);
        image.raycastTarget = true;
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.targetGraphic = image;
        button.GetComponent<RectTransform>().sizeDelta = size;

        TMP_Text label = CreateLabel(buttonObject.transform, "Label", labelText, 28f, PremiumUiStyle.Ivory);
        TMP_FontAsset premiumFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PremiumUiStyle.PremiumFontAssetPath);
        if (premiumFont != null)
            label.font = premiumFont;
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Normal;
        label.fontWeight = FontWeight.Regular;
        label.extraPadding = true;
        label.characterSpacing = 1.15f;
        label.margin = new Vector4(4f, 2f, 4f, 2f);
        return button;
    }

    private static void SetAnchored(
        RectTransform rect,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StretchWithOffsets(
        RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static Material GetOrCreateMaterial(string name, Color color, bool metallic = false)
    {
        string path = RunnerMaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", metallic ? 0.55f : 0f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", metallic ? 0.72f : 0.25f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetOrCreateGlowMaterial(
        string name,
        Color color,
        bool metallic = false,
        float emissionIntensity = 1.35f)
    {
        Material material = GetOrCreateMaterial(name, color, metallic);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * Mathf.Max(1f, emissionIntensity));
        }
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", metallic ? .92f : .72f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AddRunnerToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (!string.Equals(scenes[i].path, RunnerScenePath, StringComparison.Ordinal))
                continue;
            scenes[i] = new EditorBuildSettingsScene(RunnerScenePath, true);
            found = true;
            break;
        }
        if (!found)
            scenes.Add(new EditorBuildSettingsScene(RunnerScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ConfigureRunnerHeroImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(RunnerHeroPath) as TextureImporter;
        if (importer == null)
            return;
        bool dirty = false;
        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            dirty = true;
        }
        if (importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = false;
            dirty = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            dirty = true;
        }
        if (importer.filterMode != FilterMode.Bilinear)
        {
            importer.filterMode = FilterMode.Bilinear;
            dirty = true;
        }
        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            dirty = true;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            dirty = true;
        }
        if (importer.maxTextureSize != 2048)
        {
            importer.maxTextureSize = 2048;
            dirty = true;
        }
        if (dirty)
            importer.SaveAndReimport();
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets/Scenes", "Runner");
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Runner");
        EnsureFolder("Assets/Art/Runner", "Materials");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null)
                return result;
        }
        return null;
    }

    private static T FindRootComponent<T>(Scene scene, string rootName) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == rootName)
                return root.GetComponent<T>();
        }

        return null;
    }

    private static void RemoveRogueLaunchRoots(Scene scene, GameObject canonicalRoot)
    {
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            CatRunnerLauncher[] launchers =
                sceneRoot.GetComponentsInChildren<CatRunnerLauncher>(true);
            for (int i = 0; i < launchers.Length; i++)
            {
                CatRunnerLauncher launcher = launchers[i];
                if (launcher == null || launcher.gameObject == canonicalRoot)
                    continue;

                UnityEngine.Object.DestroyImmediate(launcher.gameObject);
            }
        }
    }
}
