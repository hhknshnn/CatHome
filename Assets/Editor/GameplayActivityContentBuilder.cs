using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameplayActivityContentBuilder
{
    private const string BallModelPath =
        "Assets/Art/Activities/Models/ActivityBall.fbx";
    private const string BasketModelPath =
        "Assets/Art/Activities/Models/ToyBasket.fbx";
    private const string ScratchModelPath =
        "Assets/Art/Activities/Models/ScratchPost.fbx";
    private const string MouseModelPath =
        "Assets/Art/Activities/Models/ClockworkMouse.fbx";

    [MenuItem("Tools/Cat Home/Gameplay/Build Activity Playground")]
    public static void BuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Activity Playground",
                "Place the ball, scratching post and mouse hunt activities in Living Room Level 01?",
                "Build",
                "Cancel"))
        {
            return;
        }

        BuildSilently();
    }

    public static string BuildSilently()
    {
        CatActivityAnimationBuilder.BuildSilently();
        ImportModels();
        BuildUiScene();
        BuildLevelScene();
        ProgressionConfigBuilder.CreateOrUpdateSilently();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "activity-playground-built";
    }

    private static void ImportModels()
    {
        string[] paths = { BallModelPath, BasketModelPath, ScratchModelPath, MouseModelPath };
        for (int i = 0; i < paths.Length; i++)
        {
            ModelImporter importer = AssetImporter.GetAtPath(paths[i]) as ModelImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(paths[i]) as ModelImporter;
            }

            if (importer != null && !Mathf.Approximately(importer.globalScale, 100f))
            {
                importer.globalScale = 100f;
                importer.SaveAndReimport();
            }
        }
    }

    private static void BuildUiScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SceneArchitectureBuilder.UiScenePath, OpenSceneMode.Single);
        GameObject canvasObject = FindRoot(scene, "Canvas");
        if (canvasObject == null)
            throw new InvalidOperationException("Shared UI scene has no Canvas root.");

        Transform oldRoot = canvasObject.transform.Find("ActivityUIRoot");
        if (oldRoot != null)
            UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);

        Transform actionTemplate = canvasObject.transform.Find("ActionButton");
        if (actionTemplate == null)
            throw new InvalidOperationException("Shared UI scene has no ActionButton template.");

        var uiRoot = new GameObject("ActivityUIRoot", typeof(RectTransform));
        RectTransform uiRect = uiRoot.GetComponent<RectTransform>();
        uiRect.SetParent(canvasObject.transform, false);
        uiRect.anchorMin = Vector2.zero;
        uiRect.anchorMax = Vector2.one;
        uiRect.offsetMin = Vector2.zero;
        uiRect.offsetMax = Vector2.zero;
        uiRect.SetAsLastSibling();

        GameObject actionObject = UnityEngine.Object.Instantiate(actionTemplate.gameObject, uiRect);
        actionObject.name = "ActivityActionButton";
        RectTransform actionRect = actionObject.GetComponent<RectTransform>();
        actionRect.anchorMin = actionRect.anchorMax = new Vector2(1f, 0f);
        actionRect.pivot = new Vector2(0.5f, 0.5f);
        actionRect.anchoredPosition = new Vector2(-190f, 320f);
        actionRect.sizeDelta = new Vector2(280f, 94f);
        CanvasGroup actionGroup = actionObject.GetComponent<CanvasGroup>() ??
                                  actionObject.AddComponent<CanvasGroup>();
        Button actionButton = actionObject.GetComponent<Button>();
        TMP_Text[] actionTexts = actionObject.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text actionLabel = null;
        TMP_Text actionShadow = null;
        for (int i = 0; i < actionTexts.Length; i++)
        {
            TMP_Text text = actionTexts[i];
            text.enableAutoSizing = true;
            text.fontSizeMin = 15f;
            text.fontSizeMax = 31f;
            if (text.name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0)
                actionShadow = text;
            else
                actionLabel = text;
        }

        GameObject progressObject = UnityEngine.Object.Instantiate(actionTemplate.gameObject, uiRect);
        progressObject.name = "ActivityProgressBadge";
        RectTransform progressRect = progressObject.GetComponent<RectTransform>();
        progressRect.anchorMin = progressRect.anchorMax = new Vector2(0.5f, 0f);
        progressRect.pivot = new Vector2(0.5f, 0.5f);
        progressRect.anchoredPosition = new Vector2(0f, 78f);
        progressRect.sizeDelta = new Vector2(420f, 66f);
        Button progressButton = progressObject.GetComponent<Button>();
        if (progressButton != null)
            UnityEngine.Object.DestroyImmediate(progressButton);
        LowPolyButtonPress press = progressObject.GetComponent<LowPolyButtonPress>();
        if (press != null)
            UnityEngine.Object.DestroyImmediate(press);
        TMP_Text[] progressTexts = progressObject.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text progressLabel = null;
        for (int i = 0; i < progressTexts.Length; i++)
        {
            TMP_Text text = progressTexts[i];
            if (text.name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                UnityEngine.Object.DestroyImmediate(text.gameObject);
                continue;
            }

            progressLabel = text;
            text.fontSize = 24f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 17f;
            text.fontSizeMax = 26f;
            text.color = new Color32(255, 244, 214, 255);
        }

        CanvasGroup progressGroup = progressObject.GetComponent<CanvasGroup>();
        if (progressGroup == null)
            progressGroup = progressObject.AddComponent<CanvasGroup>();

        ActivityPromptController controller = uiRoot.AddComponent<ActivityPromptController>();
        controller.EditorConfigure(
            actionButton,
            actionLabel,
            actionShadow,
            actionGroup,
            progressGroup,
            progressLabel);

        actionObject.SetActive(false);
        if (progressGroup != null)
        {
            progressGroup.alpha = 0f;
            progressGroup.interactable = false;
            progressGroup.blocksRaycasts = false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildLevelScene()
    {
        Scene scene = EditorSceneManager.OpenScene(SceneArchitectureBuilder.LevelScenePath, OpenSceneMode.Single);
        GameObject existing = FindNamedInScene(scene, "GameplayActivities");
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);

        var root = new GameObject("GameplayActivities");
        SceneManager.MoveGameObjectToScene(root, scene);
        GameObject gameplayGroup =
            FindNamedInScene(scene, CatHomeAuthoringWorkspace.GameplayGroupName);
        if (gameplayGroup != null)
            root.transform.SetParent(gameplayGroup.transform, false);

        BuildBallActivity(root.transform);
        BuildScratchActivity(root.transform);
        BuildMouseActivity(root.transform);

        CatMovement cat = FindInScene<CatMovement>(scene);
        if (cat != null && cat.GetComponent<CatActivityReaction>() == null)
            cat.gameObject.AddComponent<CatActivityReaction>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildBallActivity(Transform parent)
    {
        GameObject station = CreateStation("BallActivity", parent, new Vector3(-3.15f, 0f, 0.75f));
        Transform[] placementSlots = CreatePlacementSlots(
            parent,
            "BallBasketPlacement",
            new[]
            {
                new Vector3(-3.15f, 0f, 0.75f),
                new Vector3(-3.15f, 0f, -0.55f),
                new Vector3(-3.15f, 0f, -1.72f)
            });
        GameObject content = CreateContentRoot(station.transform, false);
        GameObject basket = InstantiateModel(BasketModelPath, content.transform, "ToyBasket_Visual", 0.78f, 180f);
        basket.transform.localPosition = Vector3.zero;

        BoxCollider collider = content.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.17f, 0f);
        collider.size = new Vector3(0.58f, 0.34f, 0.43f);

        Transform anchor = CreatePoint(station.transform, "InteractionAnchor", new Vector3(0.52f, 0f, 0f));

        GameObject ballObject = InstantiateModel(BallModelPath, content.transform, "ActivityBall_Target", 0.62f);
        ballObject.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        RemoveColliders(ballObject);

        Vector3[] worldPoints =
        {
            new Vector3(-1.85f, 0.16f, -1.05f),
            new Vector3(1.75f, 0.16f, -1.05f),
            new Vector3(-1.75f, 0.16f, 1.15f),
            new Vector3(1.65f, 0.16f, 1.15f)
        };
        // The basket may move, but the chase remains on the clear central rug.
        Transform[] points = CreateWorldPoints(parent, "BallLanding", worldPoints);

        BallChaseActivity activity = station.AddComponent<BallChaseActivity>();
        activity.EditorConfigure(
            "ball-chase",
            "BALL CHASE",
            CatActivityKind.BallChase,
            QuestType.PlayBall,
            0,
            "PLAY BALL",
            1.35f,
            8f,
            anchor,
            null,
            content);
        activity.EditorConfigureBall(ballObject.transform, points, 3);
        activity.EditorConfigureStoreProduct(HomeStoreService.BallBasketId);
        station.AddComponent<HomeProductPlacement>().EditorConfigure(
            HomeStoreService.BallBasketId,
            station.transform,
            placementSlots,
            new Vector2(0.78f, 0.62f));
    }

    private static void BuildScratchActivity(Transform parent)
    {
        GameObject station = CreateStation("ScratchPostActivity", parent, new Vector3(3.15f, 0f, 1.55f));
        Transform[] placementSlots = CreatePlacementSlots(
            parent,
            "ScratchPostPlacement",
            new[]
            {
                new Vector3(3.15f, 0f, 1.55f),
                new Vector3(3.15f, 0f, 0.12f),
                new Vector3(3.15f, 0f, -1.42f)
            });
        GameObject content = CreateContentRoot(station.transform, false);
        GameObject model = InstantiateModel(ScratchModelPath, content.transform, "ScratchPost_Visual", 0.72f, 180f);
        model.transform.localPosition = Vector3.zero;

        BoxCollider collider = content.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.43f, 0f);
        collider.size = new Vector3(0.56f, 0.86f, 0.45f);

        Transform anchor = CreatePoint(station.transform, "InteractionAnchor", new Vector3(0f, 0f, -0.68f));
        Transform scratchPoint = CreatePoint(station.transform, "ScratchPoint", new Vector3(0f, 0f, -0.42f));
        GameObject lockVisual = CreateLockVisual(station.transform, new Vector3(0f, 1.32f, 0f), "LV 2  •  18 BOND");
        lockVisual.SetActive(false);

        ScratchPostActivity activity = station.AddComponent<ScratchPostActivity>();
        activity.EditorConfigure(
            "scratch-post",
            "SCRATCH POST",
            CatActivityKind.ScratchPost,
            QuestType.Scratch,
            0,
            "SCRATCH",
            1.25f,
            10f,
            anchor,
            null,
            content);
        activity.EditorConfigureScratch(scratchPoint, 2.4f);
        activity.EditorConfigureStoreProduct(HomeStoreService.ScratchPostId);
        station.AddComponent<HomeProductPlacement>().EditorConfigure(
            HomeStoreService.ScratchPostId,
            station.transform,
            placementSlots,
            new Vector2(0.72f, 0.72f));
    }

    private static void BuildMouseActivity(Transform parent)
    {
        GameObject station = CreateStation("MouseHuntActivity", parent, new Vector3(3.05f, 0f, -1.82f));
        GameObject content = CreateContentRoot(station.transform, false);
        Transform home = CreatePoint(station.transform, "MouseHome", new Vector3(0f, 0f, 0f));
        Transform anchor = CreatePoint(station.transform, "InteractionAnchor", new Vector3(-0.62f, 0f, 0f));

        var mouseObject = new GameObject("ClockworkMouse_Target");
        mouseObject.transform.SetParent(content.transform, false);
        InstantiateModel(MouseModelPath, mouseObject.transform, "ClockworkMouse_Visual", 0.42f, -90f);
        mouseObject.transform.localPosition = Vector3.zero;
        RemoveColliders(mouseObject);
        GameObject lockVisual = CreateLockVisual(station.transform, new Vector3(0f, 0.66f, 0f), "LV 3  •  35 BOND");

        Vector3[] worldPoints =
        {
            new Vector3(-2.85f, 0f, -1.85f),
            new Vector3(2.85f, 0f, -1.65f),
            new Vector3(-3.0f, 0f, 1.15f),
            new Vector3(2.7f, 0f, 0.95f),
            new Vector3(-1.2f, 0f, 1.45f)
        };
        Transform[] points = CreateWorldPoints(station.transform, "MousePatrol", worldPoints);

        MouseHuntActivity activity = station.AddComponent<MouseHuntActivity>();
        activity.EditorConfigure(
            "mouse-hunt",
            "MOUSE HUNT",
            CatActivityKind.MouseHunt,
            QuestType.MouseHunt,
            35,
            "HUNT",
            1.25f,
            12f,
            anchor,
            lockVisual,
            content);
        activity.EditorConfigureMouse(mouseObject.transform, home, points, 3);
    }

    private static GameObject CreateStation(string name, Transform parent, Vector3 position)
    {
        var station = new GameObject(name);
        station.transform.SetParent(parent, false);
        station.transform.position = position;
        return station;
    }

    private static GameObject CreateContentRoot(Transform parent, bool initiallyVisible)
    {
        var content = new GameObject("UnlockedContent");
        content.transform.SetParent(parent, false);
        content.SetActive(initiallyVisible);
        return content;
    }

    private static GameObject InstantiateModel(
        string path,
        Transform parent,
        string name,
        float visualScale,
        float yaw = 0f)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null)
            throw new InvalidOperationException($"Activity model is missing at '{path}'.");

        GameObject instance = PrefabUtility.InstantiatePrefab(asset) as GameObject;
        if (instance == null)
            throw new InvalidOperationException($"Activity model '{path}' could not be instantiated.");
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation =
            Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-90f, 0f, 0f);
        instance.transform.localScale = Vector3.one * visualScale;
        return instance;
    }

    private static Transform CreatePoint(Transform parent, string name, Vector3 localPosition)
    {
        var point = new GameObject(name).transform;
        point.SetParent(parent, false);
        point.localPosition = localPosition;
        return point;
    }

    private static Transform[] CreateWorldPoints(
        Transform parent,
        string prefix,
        Vector3[] positions)
    {
        var result = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            Transform point = CreatePoint(parent, $"{prefix}_{i + 1:00}", Vector3.zero);
            point.position = positions[i];
            result[i] = point;
        }
        return result;
    }

    private static Transform[] CreatePlacementSlots(
        Transform parent,
        string prefix,
        Vector3[] positions)
    {
        Transform[] result = CreateWorldPoints(parent, prefix, positions);
        for (int i = 0; i < result.Length; i++)
            result[i].rotation = Quaternion.identity;
        return result;
    }

    private static GameObject CreateLockVisual(
        Transform parent,
        Vector3 localPosition,
        string message)
    {
        var root = new GameObject("LockedVisual");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.AddComponent<WorldSpaceBillboard>();

        var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "LockPlate";
        plate.transform.SetParent(root.transform, false);
        plate.transform.localScale = new Vector3(0.9f, 0.28f, 0.055f);
        UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());
        MeshRenderer plateRenderer = plate.GetComponent<MeshRenderer>();
        plateRenderer.sharedMaterial = GetOrCreateMaterial(
            "ActivityLockPlate",
            new Color32(72, 55, 72, 255));

        var labelObject = new GameObject("LockLabel", typeof(TextMeshPro));
        labelObject.transform.SetParent(root.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.07f);
        labelObject.transform.localRotation = Quaternion.identity;
        TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
        label.text = message;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 3.4f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color32(255, 232, 176, 255);
        label.rectTransform.sizeDelta = new Vector2(4.8f, 1.2f);
        label.enableAutoSizing = true;
        label.fontSizeMin = 2.2f;
        label.fontSizeMax = 3.4f;
        label.transform.localScale = Vector3.one * 0.19f;
        return root;
    }

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        string path = $"Assets/Art/Activities/Materials/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void RemoveColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            UnityEngine.Object.DestroyImmediate(colliders[i]);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].name == name)
                return roots[i];
        return null;
    }

    private static GameObject FindNamedInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                    return transforms[i].gameObject;
            }
        }
        return null;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }
        return null;
    }
}
