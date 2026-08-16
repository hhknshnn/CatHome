#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PetInteractionBuilder
{
    private const string MenuPath = "Tools/Cat Home/Build Pet Interaction";
    private const string OutputFolder = "Assets/Animations/Cat";
    private const string ClipPath = OutputFolder + "/Cat_Pet.anim";
    private const string EffectFolder = "Assets/Effects/Pet";
    private const string HeartMeshPath = EffectFolder + "/PetHeart_LowPoly.asset";
    private const string HeartMaterialPath = EffectFolder + "/PetHeart_Unlit.mat";
    private const string StateName = "Pet";
    private const float ClipLength = 0.8f;
    private const float FrameRate = 30f;
    private const string TutorialCanvasName = "PetTutorialCanvas";
    private const string PurrSourceName = "PetPurrAudioSource";
    private const string MeowSourceName = "PetMeowAudioSource";
    private const string PurrClipPath = "Assets/Audio/Cat/Pet/cat_purrsleepy_loop.wav";
    private static readonly string[] MeowClipPaths =
    {
        "Assets/Audio/Cat/Pet/cat_mewpurr.wav",
        "Assets/Audio/Cat/Pet/cat_mewpurr2.wav",
        "Assets/Audio/Cat/Pet/cat_softmew.wav"
    };

    [MenuItem(MenuPath)]
    private static void Build()
    {
        CatMovement catMovement = FindCatMovement();
        if (catMovement == null)
            return;

        GameObject catRoot = catMovement.gameObject;
        Animator animator = catRoot.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogError(
                $"Pet Interaction Builder: No Animator was found under '{catRoot.name}'. Nothing was changed.",
                catRoot
            );
            return;
        }

        AnimatorController controller = GetAnimatorController(animator.runtimeAnimatorController);
        if (controller == null || controller.layers == null || controller.layers.Length == 0)
        {
            Debug.LogError(
                $"Pet Interaction Builder: '{animator.name}' needs an editable Animator Controller with a Base Layer. Nothing was changed.",
                animator
            );
            return;
        }

        AnimatorStateMachine baseLayer = controller.layers[0].stateMachine;
        if (FindDirectState(baseLayer, "Idle") == null ||
            !HasFloatParameter(controller, "Speed"))
        {
            Debug.LogError(
                "Pet Interaction Builder: The Animator needs a direct 'Base Layer.Idle' state and a Float parameter named 'Speed'. Nothing was changed.",
                controller
            );
            return;
        }

        if (catRoot.GetComponentInChildren<Collider>(true) == null)
        {
            Debug.LogError(
                $"Pet Interaction Builder: '{catRoot.name}' needs a CharacterController or a Collider on the cat hierarchy. Nothing was changed.",
                catRoot
            );
            return;
        }

        Transform spine1 = FindDescendant(animator.transform, "Spine1");
        Transform pelvis = FindDescendant(animator.transform, "Spine");
        Transform spine2 = FindDescendant(animator.transform, "Spine2");
        Transform neck = FindDescendant(animator.transform, "Neck");
        Transform head = FindDescendant(animator.transform, "Head");
        Transform tail = FindDescendant(animator.transform, "Tail");
        Transform tail1 = FindDescendant(animator.transform, "Tail1");
        Transform tail2 = FindDescendant(animator.transform, "Tail2");
        Transform tail3 = FindDescendant(animator.transform, "Tail3");
        var missingBones = new List<string>();
        AddMissingBone(missingBones, spine1, "Spine1");
        AddMissingBone(missingBones, pelvis, "Spine (pelvis/root-of-spine)");
        AddMissingBone(missingBones, spine2, "Spine2");
        AddMissingBone(missingBones, neck, "Neck");
        AddMissingBone(missingBones, head, "Head");

        if (missingBones.Count > 0)
        {
            Debug.LogError(
                "Pet Interaction Builder: The cat rig is missing required bones: " +
                string.Join(", ", missingBones) + ". Nothing was changed.",
                animator
            );
            return;
        }

        EnsureOutputFolders();
        AnimationClip clip = BuildPetClip(
            animator,
            pelvis,
            spine1,
            spine2,
            neck,
            head,
            tail,
            tail1,
            tail2,
            tail3
        );
        clip = SaveOrUpdateClip(clip);
        int bindingCount = ValidateClipBindings(clip, animator);

        Undo.RecordObject(controller, "Build Pet Interaction Animator State");
        AnimatorState state = FindDirectState(baseLayer, StateName);
        if (state == null)
            state = baseLayer.AddState(StateName);

        state.motion = clip;
        state.speed = 1f;
        state.writeDefaultValues = true;
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(controller);

        PetInteraction petInteraction = catRoot.GetComponent<PetInteraction>();
        if (petInteraction == null)
            petInteraction = Undo.AddComponent<PetInteraction>(catRoot);

        PetHeartEffect heartEffect = catRoot.GetComponentInChildren<PetHeartEffect>(true);
        if (heartEffect == null)
            heartEffect = Undo.AddComponent<PetHeartEffect>(catRoot);

        PetSoundController soundController = catRoot.GetComponent<PetSoundController>();
        if (soundController == null)
            soundController = Undo.AddComponent<PetSoundController>(catRoot);

        CatSpeechBubble speechBubble = catRoot.GetComponent<CatSpeechBubble>();
        if (speechBubble == null)
            speechBubble = Undo.AddComponent<CatSpeechBubble>(catRoot);
        ConfigureSharedInteractionReferences(catRoot, speechBubble);

        AudioSource purrSource = EnsureAudioSource(catRoot.transform, PurrSourceName, true);
        AudioSource meowSource = EnsureAudioSource(catRoot.transform, MeowSourceName, false);
        ConfigureSoundController(soundController, purrSource, meowSource);

        PetTutorialHint tutorialHint = EnsureTutorialHint(
            petInteraction,
            catRoot.transform,
            Camera.main
        );

        Mesh heartMesh = GetOrCreateHeartMesh();
        Material heartMaterial = GetOrCreateHeartMaterial();
        ConfigureHeartEffect(heartEffect, head, heartMesh, heartMaterial);
        ConfigureComponent(
            petInteraction,
            catRoot,
            animator,
            heartEffect,
            soundController,
            tutorialHint
        );
        EditorUtility.SetDirty(petInteraction);
        EditorUtility.SetDirty(heartEffect);
        EditorUtility.SetDirty(soundController);
        EditorUtility.SetDirty(speechBubble);
        EditorUtility.SetDirty(tutorialHint);
        EditorSceneManager.MarkSceneDirty(catRoot.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = catRoot;
        Debug.Log(
            "Pet Interaction Builder completed.\n" +
            $"Cat: {catRoot.name}\n" +
            $"Clip: {ClipPath}\n" +
            $"Animator state: Base Layer.{StateName}\n" +
            $"Loop duration: {ClipLength:0.0}s\n" +
            $"Validated curve bindings: {bindingCount}\n" +
            $"Heart assets: {HeartMeshPath}, {HeartMaterialPath}\n" +
            "Pet audio clips, existing sources, and first-use tutorial are configured.\n" +
            "The active scene was marked dirty but was not saved automatically.",
            catRoot
        );
    }

    public static void BuildGameSceneFromCommandLine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        Selection.activeGameObject = null;
        Build();
        EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Tools/Cat Home/Rebuild Onboarding & Dialogue UI")]
    public static void RebuildPetTutorialCard()
    {
        PetInteraction petInteraction =
            UnityEngine.Object.FindAnyObjectByType<PetInteraction>(FindObjectsInactive.Include);
        if (petInteraction == null)
        {
            Debug.LogError("Pet Tutorial Builder: No PetInteraction exists in the active scene.");
            return;
        }

        PetTutorialHint tutorial = EnsureTutorialHint(
            petInteraction,
            petInteraction.transform,
            Camera.main
        );
        ConfigureTutorialReference(petInteraction, tutorial);
        EditorUtility.SetDirty(petInteraction);
        EditorUtility.SetDirty(tutorial);
        EditorSceneManager.MarkSceneDirty(petInteraction.gameObject.scene);
    }

    public static void RebuildGameScenePetTutorialFromCommandLine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        RebuildPetTutorialCard();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Cat Home/Reset Onboarding Progress")]
    private static void ResetPetTutorialProgress()
    {
        PetTutorialHint.ClearProgressKeys();
        Debug.Log("Cat Home onboarding progress was reset. It will restart in the next Play Mode session.");
    }

    private static CatMovement FindCatMovement()
    {
        if (Selection.activeGameObject != null &&
            !EditorUtility.IsPersistent(Selection.activeGameObject))
        {
            CatMovement selected = Selection.activeGameObject.GetComponentInParent<CatMovement>();
            if (selected == null)
                selected = Selection.activeGameObject.GetComponentInChildren<CatMovement>(true);
            if (selected != null)
                return selected;
        }

        CatMovement[] candidates =
            UnityEngine.Object.FindObjectsByType<CatMovement>(FindObjectsInactive.Include);

        if (candidates.Length == 1)
            return candidates[0];

        string reason = candidates.Length == 0
            ? "No CatMovement exists in the active scene."
            : "More than one CatMovement exists in the active scene.";
        Debug.LogError(
            "Pet Interaction Builder: " + reason +
            " Select the intended CatRoot in the Hierarchy and run the command again. Nothing was changed."
        );
        return null;
    }

    private static void ConfigureComponent(
        PetInteraction component,
        GameObject catRoot,
        Animator animator,
        PetHeartEffect heartEffect,
        PetSoundController soundController,
        PetTutorialHint tutorialHint)
    {
        Undo.RecordObject(component, "Configure Pet Interaction");
        var serialized = new SerializedObject(component);
        serialized.FindProperty("catMovement").objectReferenceValue =
            catRoot.GetComponent<CatMovement>();
        serialized.FindProperty("bowlInteraction").objectReferenceValue =
            catRoot.GetComponent<BowlInteraction>();
        serialized.FindProperty("sleepInteraction").objectReferenceValue =
            catRoot.GetComponent<SleepInteraction>();
        serialized.FindProperty("hungerSystem").objectReferenceValue =
            catRoot.GetComponent<HungerSystem>();
        serialized.FindProperty("thirstSystem").objectReferenceValue =
            catRoot.GetComponent<ThirstSystem>();
        serialized.FindProperty("animator").objectReferenceValue = animator;
        serialized.FindProperty("heartEffect").objectReferenceValue = heartEffect;
        serialized.FindProperty("soundController").objectReferenceValue = soundController;
        serialized.FindProperty("tutorialHint").objectReferenceValue = tutorialHint;
        serialized.ApplyModifiedProperties();
    }

    private static void ConfigureSharedInteractionReferences(
        GameObject catRoot, CatSpeechBubble speechBubble)
    {
        CatMovement movement = catRoot.GetComponent<CatMovement>();
        EnergySystem energy = null;
        EnergySystem[] energySystems =
            UnityEngine.Object.FindObjectsByType<EnergySystem>(FindObjectsInactive.Include);
        foreach (EnergySystem candidate in energySystems)
        {
            if (candidate != null && candidate.CatMovement == movement)
            {
                energy = candidate;
                break;
            }
        }
        if (energy == null && energySystems.Length == 1)
            energy = energySystems[0];

        BowlInteraction bowls = catRoot.GetComponent<BowlInteraction>();
        if (bowls != null)
        {
            Undo.RecordObject(bowls, "Configure Shared Cat Speech Bubble");
            SerializedObject serialized = new SerializedObject(bowls);
            serialized.FindProperty("speechBubble").objectReferenceValue = speechBubble;
            serialized.ApplyModifiedProperties();
        }

        SleepInteraction sleep = catRoot.GetComponent<SleepInteraction>();
        if (sleep != null)
        {
            Undo.RecordObject(sleep, "Configure Sleep Need References");
            SerializedObject serialized = new SerializedObject(sleep);
            serialized.FindProperty("speechBubble").objectReferenceValue = speechBubble;
            serialized.FindProperty("energySystem").objectReferenceValue = energy;
            serialized.ApplyModifiedProperties();
        }
    }

    private static void ConfigureHeartEffect(
        PetHeartEffect effect,
        Transform head,
        Mesh mesh,
        Material material)
    {
        Undo.RecordObject(effect, "Configure Pet Heart Effect");
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("spawnPoint").objectReferenceValue = head;
        serialized.FindProperty("heartMesh").objectReferenceValue = mesh;
        serialized.FindProperty("heartMaterial").objectReferenceValue = material;
        serialized.FindProperty("headOffset").vector3Value = new Vector3(0f, 0.22f, 0f);
        serialized.FindProperty("heartScaleMultiplier").floatValue = 1.7f;
        serialized.FindProperty("heartScaleVariation").floatValue = 0.12f;
        serialized.ApplyModifiedProperties();
    }

    private static AudioSource EnsureAudioSource(
        Transform catRoot,
        string objectName,
        bool loop)
    {
        Transform child = catRoot.Find(objectName);
        GameObject sourceObject;
        if (child == null)
        {
            sourceObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(sourceObject, "Create Pet Audio Source");
            sourceObject.transform.SetParent(catRoot, false);
        }
        else
        {
            sourceObject = child.gameObject;
        }

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null)
            source = Undo.AddComponent<AudioSource>(sourceObject);

        Undo.RecordObject(source, "Configure Pet Audio Source");
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        EditorUtility.SetDirty(source);
        return source;
    }

    private static void ConfigureSoundController(
        PetSoundController controller,
        AudioSource purrSource,
        AudioSource meowSource)
    {
        Undo.RecordObject(controller, "Configure Pet Sound Controller");
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("purrLoop").objectReferenceValue =
            LoadAndConfigureAudioClip(PurrClipPath);
        SerializedProperty meowClips = serialized.FindProperty("meowClips");
        meowClips.arraySize = MeowClipPaths.Length;
        for (int i = 0; i < MeowClipPaths.Length; i++)
            meowClips.GetArrayElementAtIndex(i).objectReferenceValue =
                LoadAndConfigureAudioClip(MeowClipPaths[i]);
        serialized.FindProperty("audioSource").objectReferenceValue = purrSource;
        serialized.FindProperty("meowAudioSource").objectReferenceValue = meowSource;
        serialized.ApplyModifiedProperties();
    }

    private static AudioClip LoadAndConfigureAudioClip(string assetPath)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        if (clip == null)
        {
            Debug.LogError($"Pet Interaction Builder: Missing audio clip at '{assetPath}'.");
            return null;
        }

        AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
        if (importer != null)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }
        return clip;
    }

    private static PetTutorialHint EnsureTutorialHint(
        PetInteraction petInteraction,
        Transform catTarget,
        Camera gameplayCamera)
    {
        PetTutorialHint tutorial =
            UnityEngine.Object.FindAnyObjectByType<PetTutorialHint>(FindObjectsInactive.Include);
        GameObject canvasObject = tutorial != null ? tutorial.gameObject : GameObject.Find(TutorialCanvasName);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(TutorialCanvasName);
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Pet Tutorial Canvas");
        }
        if (tutorial == null)
        {
            tutorial = canvasObject.GetComponent<PetTutorialHint>();
            if (tutorial == null)
                tutorial = Undo.AddComponent<PetTutorialHint>(canvasObject);
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = Undo.AddComponent<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = false;
        canvas.sortingOrder = 0;
        canvas.pixelPerfect = true;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.localScale = Vector3.one;
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.sizeDelta = Vector2.zero;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = Undo.AddComponent<CanvasScaler>(canvasObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = canvasObject.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            raycaster = Undo.AddComponent<GraphicRaycaster>(canvasObject);

        CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
        if (group == null)
            group = Undo.AddComponent<CanvasGroup>(canvasObject);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        RectTransform hintRoot = EnsureUiObject(canvasObject.transform, "PetHint");
        hintRoot.sizeDelta = new Vector2(360f, 246f);
        hintRoot.localScale = Vector3.one;
        RemoveGraphics(hintRoot.gameObject, null);
        ClearTutorialChildren(hintRoot);

        Undo.RecordObject(tutorial, "Configure Pet Tutorial Hint");
        var serialized = new SerializedObject(tutorial);
        serialized.FindProperty("petInteraction").objectReferenceValue = petInteraction;
        serialized.FindProperty("catTarget").objectReferenceValue = catTarget;
        serialized.FindProperty("gameplayCamera").objectReferenceValue = gameplayCamera;
        serialized.FindProperty("canvasGroup").objectReferenceValue = group;
        serialized.FindProperty("hintRoot").objectReferenceValue = hintRoot;
        serialized.FindProperty("whileYouWereAwayPopup").objectReferenceValue =
            UnityEngine.Object.FindAnyObjectByType<WhileYouWereAwayPopup>(FindObjectsInactive.Include);
        serialized.FindProperty("visualVersion").intValue = 10;
        serialized.FindProperty("cardSize").vector2Value = new Vector2(320f, 176f);
        serialized.FindProperty("horizontalTravel").floatValue = 46f;
        serialized.FindProperty("swipePeriod").floatValue = 2.1f;
        serialized.FindProperty("floatPeriod").floatValue = 3.2f;
        serialized.FindProperty("floatDistance").floatValue = 3f;
        serialized.FindProperty("fadeDuration").floatValue = 0.22f;
        serialized.FindProperty("targetGapPixels").floatValue = 28f;
        serialized.FindProperty("catFallbackHeight").floatValue = 0.72f;
        serialized.FindProperty("followSmoothTime").floatValue = 0.1f;
        serialized.FindProperty("safeAreaPadding").floatValue = 20f;
        serialized.FindProperty("topUiClearance").floatValue = 104f;
        serialized.FindProperty("bottomUiClearance").floatValue = 118f;
        serialized.FindProperty("movementDistance").floatValue = 0.25f;
        serialized.ApplyModifiedProperties();

        RectTransform dialogueRoot = EnsureUiObject(canvasObject.transform, "CatDialogue");
        dialogueRoot.anchorMin = Vector2.zero;
        dialogueRoot.anchorMax = Vector2.one;
        dialogueRoot.offsetMin = Vector2.zero;
        dialogueRoot.offsetMax = Vector2.zero;

        CanvasGroup dialogueGroup = dialogueRoot.GetComponent<CanvasGroup>();
        if (dialogueGroup == null)
            dialogueGroup = Undo.AddComponent<CanvasGroup>(dialogueRoot.gameObject);
        dialogueGroup.alpha = 0f;
        dialogueGroup.interactable = false;
        dialogueGroup.blocksRaycasts = false;

        CatDialogueView dialogueView = dialogueRoot.GetComponent<CatDialogueView>();
        if (dialogueView == null)
            dialogueView = Undo.AddComponent<CatDialogueView>(dialogueRoot.gameObject);

        var dialogueSerialized = new SerializedObject(dialogueView);
        SerializedProperty dialogueCanvasGroup = dialogueSerialized.FindProperty("canvasGroup");
        if (dialogueCanvasGroup != null)
            dialogueCanvasGroup.objectReferenceValue = dialogueGroup;
        dialogueSerialized.ApplyModifiedProperties();

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Fredoka-SemiBold SDF.asset"
        );
        dialogueView.Rebuild(font, true);
        tutorial.ConfigureDefaultStepsForScene();
        tutorial.RebuildVisuals(font, true);
        tutorial.RebuildCelebration(font, true);

        hintRoot.gameObject.SetActive(false);
        EditorUtility.SetDirty(canvas);
        EditorUtility.SetDirty(scaler);
        EditorUtility.SetDirty(group);
        EditorUtility.SetDirty(raycaster);
        EditorUtility.SetDirty(tutorial);
        EditorUtility.SetDirty(dialogueGroup);
        EditorUtility.SetDirty(dialogueView);
        return tutorial;
    }

    private static void ConfigureTutorialReference(
        PetInteraction petInteraction,
        PetTutorialHint tutorial)
    {
        var serialized = new SerializedObject(petInteraction);
        serialized.FindProperty("tutorialHint").objectReferenceValue = tutorial;
        serialized.ApplyModifiedProperties();
    }

    private static void ClearTutorialChildren(RectTransform hintRoot)
    {
        for (int i = hintRoot.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(hintRoot.GetChild(i).gameObject);
    }

    private static LowPolyPanelGraphic CreateTutorialPanel(
        Transform parent,
        string objectName,
        Vector2 size,
        Vector2 position,
        Color color,
        float cornerCut,
        float bevelWidth)
    {
        RectTransform rect = EnsureUiObject(parent, objectName);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        EnsureCanvasRenderer(rect.gameObject);
        LowPolyPanelGraphic panel = rect.GetComponent<LowPolyPanelGraphic>();
        if (panel == null)
            panel = Undo.AddComponent<LowPolyPanelGraphic>(rect.gameObject);
        RemoveGraphics(rect.gameObject, panel);
        panel.raycastTarget = false;

        panel.ConfigureTutorialStyle(color, cornerCut, bevelWidth);
        return panel;
    }

    private static CanvasRenderer EnsureCanvasRenderer(GameObject target)
    {
        CanvasRenderer renderer = target.GetComponent<CanvasRenderer>();
        return renderer != null ? renderer : Undo.AddComponent<CanvasRenderer>(target);
    }

    private static void RemoveGraphics(GameObject target, Graphic keep)
    {
        Graphic[] graphics = target.GetComponents<Graphic>();
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != keep)
                Undo.DestroyObjectImmediate(graphics[i]);
        }
    }

    private static RectTransform EnsureUiObject(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            RectTransform existingRect = existing as RectTransform;
            if (existingRect == null)
                throw new InvalidOperationException(
                    $"Pet Interaction Builder: '{objectName}' exists but is not a RectTransform."
                );
            return existingRect;
        }

        var child = new GameObject(objectName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, "Create Pet Tutorial UI");
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static AnimationClip BuildPetClip(
        Animator animator,
        Transform pelvis,
        Transform spine1,
        Transform spine2,
        Transform neck,
        Transform head,
        Transform tail,
        Transform tail1,
        Transform tail2,
        Transform tail3)
    {
        var clip = new AnimationClip
        {
            name = "Cat_Pet",
            frameRate = FrameRate
        };

        float[] times = { 0f, 0.2f, 0.4f, 0.6f, ClipLength };
        AddRotationCurves(
            clip,
            AnimationUtility.CalculateTransformPath(pelvis, animator.transform),
            pelvis.localRotation,
            times,
            new[]
            {
                Vector3.zero,
                new Vector3(1.2f, -2.2f, -2.5f),
                new Vector3(0.5f, 0f, 0f),
                new Vector3(1.2f, 2.2f, 2.5f),
                Vector3.zero
            }
        );
        AddPositionCurves(
            clip,
            AnimationUtility.CalculateTransformPath(pelvis, animator.transform),
            pelvis.localPosition,
            times,
            new[] { 0f, 0.0015f, 0.0025f, 0.0015f, 0f }
        );
        AddRotationCurves(
            clip,
            AnimationUtility.CalculateTransformPath(spine1, animator.transform),
            spine1.localRotation,
            times,
            new[]
            {
                Vector3.zero,
                new Vector3(3f, 1f, -3.5f),
                new Vector3(1f, 0f, 0f),
                new Vector3(-1f, -1f, 3.5f),
                Vector3.zero
            }
        );
        AddRotationCurves(
            clip,
            AnimationUtility.CalculateTransformPath(spine2, animator.transform),
            spine2.localRotation,
            times,
            new[]
            {
                Vector3.zero,
                new Vector3(7f, 0f, -4.5f),
                new Vector3(3.5f, 0f, 0f),
                new Vector3(0f, 0f, 4.5f),
                Vector3.zero
            }
        );
        AddRotationCurves(
            clip,
            AnimationUtility.CalculateTransformPath(neck, animator.transform),
            neck.localRotation,
            times,
            new[]
            {
                Vector3.zero,
                new Vector3(-12f, -2f, -7f),
                new Vector3(-8f, 0f, 0f),
                new Vector3(-6f, 2f, 7f),
                Vector3.zero
            }
        );
        AddRotationCurves(
            clip,
            AnimationUtility.CalculateTransformPath(head, animator.transform),
            head.localRotation,
            times,
            new[]
            {
                Vector3.zero,
                new Vector3(-17f, -4f, -10f),
                new Vector3(-11f, 0f, 0f),
                new Vector3(-8f, 4f, 10f),
                Vector3.zero
            }
        );

        AddOptionalTailCurves(
            clip,
            animator,
            times,
            tail,
            new[]
            {
                Vector3.zero,
                new Vector3(2f, -10f, -4f),
                Vector3.zero,
                new Vector3(-1f, 10f, 4f),
                Vector3.zero
            }
        );
        AddOptionalTailCurves(
            clip,
            animator,
            times,
            tail1,
            new[]
            {
                Vector3.zero,
                new Vector3(0f, -12f, -6f),
                Vector3.zero,
                new Vector3(0f, 12f, 6f),
                Vector3.zero
            }
        );
        AddOptionalTailCurves(
            clip,
            animator,
            times,
            tail2,
            new[]
            {
                Vector3.zero,
                new Vector3(0f, -15f, -7f),
                Vector3.zero,
                new Vector3(0f, 15f, 7f),
                Vector3.zero
            }
        );
        AddOptionalTailCurves(
            clip,
            animator,
            times,
            tail3,
            new[]
            {
                Vector3.zero,
                new Vector3(0f, -18f, -9f),
                Vector3.zero,
                new Vector3(0f, 18f, 9f),
                Vector3.zero
            }
        );

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.startTime = 0f;
        settings.stopTime = ClipLength;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.EnsureQuaternionContinuity();
        return clip;
    }

    private static void AddRotationCurves(
        AnimationClip clip,
        string path,
        Quaternion baseRotation,
        float[] times,
        Vector3[] localEulerOffsets)
    {
        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();
        var w = new AnimationCurve();
        Quaternion previous = baseRotation;

        for (int i = 0; i < times.Length; i++)
        {
            Vector3 offset = localEulerOffsets[i];
            Quaternion localOffset =
                Quaternion.AngleAxis(offset.x, Vector3.right) *
                Quaternion.AngleAxis(offset.y, Vector3.up) *
                Quaternion.AngleAxis(offset.z, Vector3.forward);
            Quaternion rotation = baseRotation * localOffset;
            if (i > 0 && Quaternion.Dot(previous, rotation) < 0f)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);

            x.AddKey(times[i], rotation.x);
            y.AddKey(times[i], rotation.y);
            z.AddKey(times[i], rotation.z);
            w.AddKey(times[i], rotation.w);
            previous = rotation;
        }

        SetSmoothTangents(x);
        SetSmoothTangents(y);
        SetSmoothTangents(z);
        SetSmoothTangents(w);
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.x"),
            x
        );
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.y"),
            y
        );
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.z"),
            z
        );
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), "localRotation.w"),
            w
        );
    }

    private static void AddOptionalTailCurves(
        AnimationClip clip,
        Animator animator,
        float[] times,
        Transform bone,
        Vector3[] localEulerOffsets)
    {
        if (bone == null)
            return;

        AddRotationCurves(
            clip,
            AnimationUtility.CalculateTransformPath(bone, animator.transform),
            bone.localRotation,
            times,
            localEulerOffsets
        );
    }

    private static void AddPositionCurves(
        AnimationClip clip,
        string path,
        Vector3 basePosition,
        float[] times,
        float[] verticalOffsets)
    {
        var x = new AnimationCurve();
        var y = new AnimationCurve();
        var z = new AnimationCurve();
        for (int i = 0; i < times.Length; i++)
        {
            x.AddKey(times[i], basePosition.x);
            y.AddKey(times[i], basePosition.y + verticalOffsets[i]);
            z.AddKey(times[i], basePosition.z);
        }

        SetSmoothTangents(x);
        SetSmoothTangents(y);
        SetSmoothTangents(z);
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.x"),
            x
        );
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.y"),
            y
        );
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), "localPosition.z"),
            z
        );
    }

    private static void SetSmoothTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.ClampedAuto
            );
            AnimationUtility.SetKeyRightTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.ClampedAuto
            );
        }
    }

    private static AnimationClip SaveOrUpdateClip(AnimationClip generated)
    {
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(generated, ClipPath);
            return generated;
        }

        Undo.RecordObject(existing, "Update Pet Animation");
        EditorCurveBinding[] oldBindings = AnimationUtility.GetCurveBindings(existing);
        for (int i = 0; i < oldBindings.Length; i++)
            AnimationUtility.SetEditorCurve(existing, oldBindings[i], null);

        EditorCurveBinding[] newBindings = AnimationUtility.GetCurveBindings(generated);
        for (int i = 0; i < newBindings.Length; i++)
        {
            AnimationCurve curve = AnimationUtility.GetEditorCurve(generated, newBindings[i]);
            AnimationUtility.SetEditorCurve(existing, newBindings[i], curve);
        }

        existing.frameRate = generated.frameRate;
        existing.legacy = false;
        AnimationUtility.SetAnimationClipSettings(
            existing,
            AnimationUtility.GetAnimationClipSettings(generated)
        );
        existing.name = "Cat_Pet";
        existing.EnsureQuaternionContinuity();
        EditorUtility.SetDirty(existing);
        UnityEngine.Object.DestroyImmediate(generated);
        return existing;
    }

    private static int ValidateClipBindings(AnimationClip clip, Animator animator)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        if (bindings.Length == 0)
            throw new InvalidOperationException(
                "Pet Interaction Builder: Cat_Pet contains no runtime curve bindings."
            );

        for (int i = 0; i < bindings.Length; i++)
        {
            string path = bindings[i].path;
            if (string.IsNullOrEmpty(path) || animator.transform.Find(path) == null)
            {
                throw new InvalidOperationException(
                    $"Pet Interaction Builder: Animation binding path '{path}' does not exist under Animator '{animator.name}'."
                );
            }
        }

        return bindings.Length;
    }

    private static AnimatorController GetAnimatorController(RuntimeAnimatorController runtime)
    {
        while (runtime is AnimatorOverrideController overrideController)
            runtime = overrideController.runtimeAnimatorController;
        return runtime as AnimatorController;
    }

    private static AnimatorState FindDirectState(
        AnimatorStateMachine stateMachine,
        string stateName)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state.name == stateName)
                return child.state;
        }

        return null;
    }

    private static bool HasFloatParameter(
        AnimatorController controller,
        string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (string.Equals(root.name, name, StringComparison.Ordinal))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDescendant(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void AddMissingBone(List<string> missing, Transform bone, string name)
    {
        if (bone == null)
            missing.Add(name);
    }

    private static void EnsureOutputFolders()
    {
        EnsureFolder("Assets", "Animations");
        EnsureFolder("Assets/Animations", "Cat");
        EnsureFolder("Assets", "Effects");
        EnsureFolder("Assets/Effects", "Pet");
    }

    private static Mesh GetOrCreateHeartMesh()
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(HeartMeshPath);
        if (mesh != null)
            return mesh;

        mesh = new Mesh { name = "PetHeart_LowPoly" };

        mesh.vertices = new[]
        {
            new Vector3(0f, -0.62f, 0f),
            new Vector3(-0.72f, 0.05f, 0f),
            new Vector3(-0.68f, 0.48f, 0f),
            new Vector3(-0.36f, 0.72f, 0f),
            new Vector3(0f, 0.43f, 0f),
            new Vector3(0.36f, 0.72f, 0f),
            new Vector3(0.68f, 0.48f, 0f),
            new Vector3(0.72f, 0.05f, 0f),
            Vector3.zero
        };
        mesh.triangles = new[]
        {
            8, 1, 0, 8, 2, 1, 8, 3, 2, 8, 4, 3,
            8, 5, 4, 8, 6, 5, 8, 7, 6, 8, 0, 7
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        AssetDatabase.CreateAsset(mesh, HeartMeshPath);
        return mesh;
    }

    private static Material GetOrCreateHeartMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(HeartMaterialPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException(
                "Pet Interaction Builder: URP Unlit shader was not found."
            );

        material = new Material(shader) { name = "PetHeart_Unlit" };
        AssetDatabase.CreateAsset(material, HeartMaterialPath);

        material.SetColor("_BaseColor", Color.red);
        material.SetFloat("_Cull", 0f);
        PetHeartEffect.ConfigureTransparentMaterial(material);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
