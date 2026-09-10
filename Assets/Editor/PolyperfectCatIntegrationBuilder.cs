using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Adapts the imported Polyperfect cat to Cat Home's stable Animator contract.
/// The vendor controller uses bools and triggers; Cat Home gameplay expects a
/// Speed float plus named care/activity states. This builder keeps the vendor
/// files untouched and authors one project-owned wrapper prefab/controller.
/// </summary>
public static class PolyperfectCatIntegrationBuilder
{
    public const string SourcePrefabPath =
        "Assets/polyperfect/Low Poly Animated Cats/_Prefabs/Cat_Domestic_Shorthair.prefab";
    public const string SourceModelPath =
        "Assets/polyperfect/Low Poly Animated Cats/Meshes/SKM_Cat_Domestic_Shorthair.fbx";
    public const string OutputFolder = "Assets/Art/Cat/Polyperfect";
    public const string ControllerPath = OutputFolder + "/CatHome_Polyperfect.controller";
    public const string PrefabPath = OutputFolder + "/CatHome_DomesticShorthair.prefab";
    public const string CatalogPath = "Assets/Resources/CatBreedCatalog.asset";
    public const string VisualRootName = "CatHome_DomesticShorthair";
    public const float VisualScale = 3f;

    private const string SpeedParameter = "Speed";
    private const string MenuPath = "Tools/Cat Home/Cat/Install Low Poly Animated Cat";

    private static readonly string[] HomeScenePaths =
    {
        HomeRoomService.LivingRoomScenePath,
        HomeRoomService.BathroomScenePath,
        HomeRoomService.KitchenScenePath,
        HomeRoomService.BedroomScenePath,
        HomeRoomService.GardenScenePath,
        HomeRoomService.BalconyScenePath,
        HomeRoomService.PatioScenePath,
        HomeRoomService.SecondFloorScenePath
    };

    private static readonly string[] RequiredStateNames =
    {
        "Idle",
        "Pet",
        "Eat",
        "Drink",
        "LieDown",
        "Sleep",
        "ActivityPounce",
        "ActivityPawSwat",
        "ActivityScratch",
        "ActivityTunnelCrawl"
    };

    private static readonly BreedAsset[] BreedAssets =
    {
        new BreedAsset("domestic-shorthair", "Domestic Shorthair",
            "Cat_Domestic_Shorthair.prefab", "Icon_Cat_Domestic_Shorthair_LP.png"),
        new BreedAsset("domestic-shorthair-orange", "Orange Shorthair",
            "Cat_Domestic_Shorthair Orange.prefab", "Icon_Cat_Domestic_Shorthair_Orange_LP.png"),
        new BreedAsset("khao-manee", "Khao Manee",
            "Cat_Domestic_Khao Manee.prefab", "Icon_Cat_Khao_Manee_LP.png"),
        new BreedAsset("british-shorthair", "British Shorthair",
            "Cat_British_Shorthair.prefab", "Icon_Cat_British_Shorthair_LP.png"),
        new BreedAsset("domestic-longhair", "Domestic Longhair",
            "Cat_Domestic_Longhair.prefab", "Icon_Cat_Domestic_Longhair_LP.png"),
        new BreedAsset("maine-coon", "Maine Coon",
            "Cat_Mainecoon.prefab", "Icon_Cat_Mainecoon_LP.png"),
        new BreedAsset("oriental-shorthair", "Oriental Shorthair",
            "Cat_Oriental_Shorthair.prefab", "Icon_Cat_Oriental_Shorthair_LP.png"),
        new BreedAsset("persian", "Persian",
            "Cat_Persian.prefab", "Icon_Cat_Persian_LP.png"),
        new BreedAsset("russian-blue", "Russian Blue",
            "Cat_Russian_Blue.prefab", "Icon_Cat_Russian_Blue_LP.png"),
        new BreedAsset("sphynx", "Sphynx",
            "Cat_Sphynx.prefab", "Icon_Cat_Sphynx_LP.png")
    };

    public static IReadOnlyList<string> RequiredStates => RequiredStateNames;

    [MenuItem(MenuPath, false, 4090)]
    public static void BuildSilently()
    {
        RequireSourceAssets();
        EnsureFolder(OutputFolder);

        AnimatorController controller = BuildController();
        BuildPrefab(controller);
        BuildBreedCatalog(controller);
        CatToyAnimationBuilder.Build();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        PatchAllPlayableCatScenes();
        DisableLegacyRigV21PlaySwap();

        AssetDatabase.SaveAssets();
        Debug.Log(
            "[CatHome Cat] Polyperfect Domestic Shorthair installed in every home room, " +
            "Cat Runner and Cat Catch. CAT SHOP catalog exposes all 10 cats; all 23 " +
            "vendor clips remain untouched.");
    }

    private static void BuildBreedCatalog(RuntimeAnimatorController controller)
    {
        EnsureFolder("Assets/Resources");
        CatBreedCatalog catalog = AssetDatabase.LoadAssetAtPath<CatBreedCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CatBreedCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var entries = new CatBreedCatalog.Entry[BreedAssets.Length];
        const string prefabRoot =
            "Assets/polyperfect/Low Poly Animated Cats/_Prefabs/";
        const string iconRoot =
            "Assets/polyperfect/Low Poly Animated Cats/UI/Icons/";
        for (int i = 0; i < BreedAssets.Length; i++)
        {
            BreedAsset breed = BreedAssets[i];
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabRoot + breed.PrefabFile);
            Sprite portrait = AssetDatabase.LoadAssetAtPath<Sprite>(
                iconRoot + breed.IconFile);
            if (source == null || portrait == null)
                throw new InvalidOperationException(
                    $"CAT SHOP asset is missing for '{breed.DisplayName}'.");
            entries[i] = CatBreedCatalog.EditorCreateEntry(
                breed.Id, breed.DisplayName, source, portrait);
        }

        catalog.EditorConfigure(controller, entries);
        EditorUtility.SetDirty(catalog);
    }

    private static AnimatorController BuildController()
    {
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        EnsureSpeedParameter(controller);
        if(!controller.parameters.Any(p=>p.name=="LocomotionRate"))controller.AddParameter("LocomotionRate",AnimatorControllerParameterType.Float);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;

        AnimationClip idle = RequireClip("Idle");
        AnimationClip walk = RequireClip("Walk");
        AnimationClip run = RequireClip("Run");

        AnimatorState locomotion = GetOrCreateState(machine, "Idle", new Vector3(240f, 80f));
        BlendTree blendTree = locomotion.motion as BlendTree;
        if (blendTree == null)
        {
            blendTree = new BlendTree { name = "CatHome Locomotion" };
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            locomotion.motion = blendTree;
        }

        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = SpeedParameter;
        blendTree.useAutomaticThresholds = false;
        blendTree.children = new ChildMotion[0];
        blendTree.AddChild(idle, 0f);
        blendTree.AddChild(walk, .35f);
        blendTree.AddChild(run, 1f);
        machine.defaultState = locomotion;
        locomotion.speedParameter="LocomotionRate";locomotion.speedParameterActive=true;
        var rate=controller.parameters;for(int i=0;i<rate.Length;i++)if(rate[i].name=="LocomotionRate")rate[i].defaultFloat=1f;controller.parameters=rate;

        SetState(machine, "Pet", RequireClip("Sitting"), 1f, 470f, 10f);
        SetState(machine, "Eat", RequireClip("Eating"), 1f, 470f, 75f);
        SetState(machine, "Drink", RequireClip("Eating"), 1f, 470f, 140f);
        SetState(machine, "LieDown", RequireClip("Sitting_to_Sleep"), 1.3f, 470f, 205f);
        SetState(machine, "Sleep", RequireClip("Sleeping"), 1f, 470f, 270f);

        SetState(machine, "ActivityPounce", RequireClip("Attack"), 2.5f, 15f, 10f);
        SetState(machine, "ActivityPawSwat", RequireClip("Attack"), 3.25f, 15f, 75f);
        SetState(machine, "ActivityScratch", RequireClip("Itching"), 2f, 15f, 140f);
        SetState(machine, "ActivityTunnelCrawl", walk, .8f, 15f, 205f);

        EditorUtility.SetDirty(blendTree);
        EditorUtility.SetDirty(machine);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void EnsureSpeedParameter(AnimatorController controller)
    {
        AnimatorControllerParameter existing = null;
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == SpeedParameter)
            {
                existing = parameter;
                break;
            }
        }

        if (existing != null && existing.type == AnimatorControllerParameterType.Float)
            return;

        if (existing != null)
            controller.RemoveParameter(existing);
        controller.AddParameter(SpeedParameter, AnimatorControllerParameterType.Float);
    }

    private static AnimatorState GetOrCreateState(
        AnimatorStateMachine machine,
        string name,
        Vector3 position)
    {
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state != null && child.state.name == name)
                return child.state;
        }

        return machine.AddState(name, position);
    }

    private static void SetState(
        AnimatorStateMachine machine,
        string name,
        AnimationClip clip,
        float speed,
        float x,
        float y)
    {
        AnimatorState state = GetOrCreateState(machine, name, new Vector3(x, y));
        state.motion = clip;
        state.speed = speed;
        state.writeDefaultValues = true;
        EditorUtility.SetDirty(state);
    }

    private static AnimationClip RequireClip(string suffix)
    {
        string expectedName = "Cat_Domestic_Shorthair|" + suffix;
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(SourceModelPath))
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip != null && clip.name == expectedName)
                return clip;
        }

        throw new InvalidOperationException(
            $"Polyperfect animation '{expectedName}' is missing from '{SourceModelPath}'.");
    }

    private static void BuildPrefab(AnimatorController controller)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (source == null)
            throw new InvalidOperationException($"Polyperfect cat prefab is missing at '{SourcePrefabPath}'.");

        var root = new GameObject(VisualRootName);
        try
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (visual == null)
                throw new InvalidOperationException("Polyperfect cat prefab could not be instantiated.");

            visual.name = "AnimatedVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, .02f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * VisualScale;

            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
                throw new InvalidOperationException("Polyperfect cat prefab has no Animator on its root.");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Transform head = FindDescendant(visual.transform, "DEF-spine.006");
            if (head == null)
                throw new InvalidOperationException("Polyperfect cat head bone 'DEF-spine.006' is missing.");

            Transform heartSpawn = new GameObject("HeartSpawn").transform;
            heartSpawn.SetParent(head, false);
            heartSpawn.localPosition = new Vector3(0f, .045f, .035f);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (saved == null)
                throw new InvalidOperationException($"Could not save integrated cat prefab to '{PrefabPath}'.");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void PatchAllPlayableCatScenes()
    {
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in HomeScenePaths)
                PatchHomeScene(path);
            PatchRunnerScene(CatRunnerLauncher.RunnerScenePath);
            PatchCatchScene(CatCatchLauncher.CatchScenePath);
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    private static void PatchHomeScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        CatMovement cat = FindInScene<CatMovement>(scene);
        if (cat == null)
            throw new InvalidOperationException($"'{path}' has no CatRoot/CatMovement.");

        Animator oldAnimator = cat.GetComponentInChildren<Animator>(true);
        DestroyVisualContaining(cat.transform, oldAnimator);

        GameObject visual = InstantiateIntegratedPrefab(scene, cat.transform, VisualRootName, Vector3.one);
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        RebindAnimatorFields(cat.gameObject, animator);
        RebindHeartSpawn(cat.gameObject, visual.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchRunnerScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        CatRunnerPlayer player = FindInScene<CatRunnerPlayer>(scene);
        if (player == null)
            throw new InvalidOperationException($"'{path}' has no CatRunnerPlayer.");

        Animator oldAnimator = player.GetComponentInChildren<Animator>(true);
        DestroyVisualContaining(player.transform, oldAnimator);

        GameObject visual = InstantiateIntegratedPrefab(
            scene, player.transform, "CurrentCat_Visual", Vector3.one * .5f);
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        player.EditorConfigure(animator, visual.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchCatchScene(string path)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        CatCatchPlayer player = FindInScene<CatCatchPlayer>(scene);
        if (player == null)
            throw new InvalidOperationException($"'{path}' has no CatCatchPlayer.");

        Animator oldAnimator = player.GetComponentInChildren<Animator>(true);
        DestroyVisualContaining(player.transform, oldAnimator);

        GameObject visual = InstantiateIntegratedPrefab(
            scene, player.transform, "CatModel", Vector3.one);
        Animator animator = visual.GetComponentInChildren<Animator>(true);
        SetSerializedObjectReference(player, "animator", animator);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject InstantiateIntegratedPrefab(
        Scene scene,
        Transform parent,
        string name,
        Vector3 localScale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
            throw new InvalidOperationException($"Integrated cat prefab could not be instantiated from '{PrefabPath}'.");

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = localScale;

        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator == null)
            throw new InvalidOperationException("Integrated cat instance has no Animator.");
        animator.applyRootMotion = false;
        return instance;
    }

    private static void DestroyVisualContaining(Transform owner, Animator animator)
    {
        if (animator == null)
            return;

        Transform visualRoot = animator.transform;
        while (visualRoot.parent != null && visualRoot.parent != owner)
            visualRoot = visualRoot.parent;

        if (visualRoot.parent == owner)
            Object.DestroyImmediate(visualRoot.gameObject);
    }

    private static void RebindAnimatorFields(GameObject catRoot, Animator animator)
    {
        foreach (Component component in catRoot.GetComponents<Component>())
        {
            if (component != null)
                SetSerializedObjectReference(component, "animator", animator);
        }
    }

    private static void RebindHeartSpawn(GameObject catRoot, Transform visualRoot)
    {
        PetHeartEffect hearts = catRoot.GetComponent<PetHeartEffect>();
        Transform spawn = FindDescendant(visualRoot, "HeartSpawn");
        if (hearts != null && spawn != null)
            SetSerializedObjectReference(hearts, "spawnPoint", spawn);
    }

    private static void SetSerializedObjectReference(
        Component component,
        string propertyName,
        Object value)
    {
        var serialized = new SerializedObject(component);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            return;
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(component);
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
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

    private static void RequireSourceAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath) == null)
            throw new InvalidOperationException(
                "Low Poly Animated Cats is not fully imported. Missing: " + SourcePrefabPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) == null)
            throw new InvalidOperationException(
                "Low Poly Animated Cats is not fully imported. Missing: " + SourceModelPath);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private readonly struct BreedAsset
    {
        public BreedAsset(string id, string displayName, string prefabFile, string iconFile)
        {
            Id = id;
            DisplayName = displayName;
            PrefabFile = prefabFile;
            IconFile = iconFile;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string PrefabFile { get; }
        public string IconFile { get; }
    }

    private static void DisableLegacyRigV21PlaySwap()
    {
        EditorPrefs.SetBool("CatHome.RigV21.HomePlayTest.Enabled", false);
    }
}
