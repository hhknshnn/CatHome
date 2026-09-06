using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Reversible in-Editor play test for the Rig V2.1 cat.
/// Home scene assets stay untouched; the visual is replaced only in Play mode.
/// </summary>
[InitializeOnLoad]
internal static class CatRigV21HomePlayModeSwap
{
    private const string PrefabPath =
        "Assets/Art/CatRigV21/SM_CartoonAnimal_Cat_RigV21_QA.prefab";
    private const string ApprovedCatControllerFolder =
        "Assets/Art/Cat/Polyperfect/";
    private const string EnabledKey = "CatHome.RigV21.HomePlayTest.Enabled";
    private const string MenuPath = "Tools/Cat Home/Cat/Rig V2.1 Home Play Test";
    private const float VisualScale = 0.85f;

    private static readonly HashSet<string> HomeSceneNames = new HashSet<string>
    {
        "LivingRoom_Level01",
        "Bathroom_Level01",
        "Kitchen_Level01",
        "Bedroom_Level01",
        "Garden_Level01",
        "Balcony_Level01",
        "Patio_Level01",
        "SecondFloor_Level01"
    };

    static CatRigV21HomePlayModeSwap()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Menu.SetChecked(MenuPath, IsEnabled);

        if (EditorApplication.isPlaying)
            HookSceneLoads();
    }

    private static bool IsEnabled => EditorPrefs.GetBool(EnabledKey, true);

    [MenuItem(MenuPath, false, 4100)]
    private static void Toggle()
    {
        EditorPrefs.SetBool(EnabledKey, !IsEnabled);
        Menu.SetChecked(MenuPath, IsEnabled);
        Debug.Log($"[RigV21] Home Play test {(IsEnabled ? "enabled" : "disabled")}.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidation()
    {
        Menu.SetChecked(MenuPath, IsEnabled);
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            HookSceneLoads();
            EditorApplication.delayCall += SwapLoadedHomeScenes;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private static void HookSceneLoads()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsEnabled || !HomeSceneNames.Contains(scene.name))
            return;

        EditorApplication.delayCall += () => TrySwap(scene);
    }

    private static void SwapLoadedHomeScenes()
    {
        if (!IsEnabled || !EditorApplication.isPlaying)
            return;

        for (int i = 0; i < SceneManager.sceneCount; i++)
            TrySwap(SceneManager.GetSceneAt(i));
    }

    private static void TrySwap(Scene scene)
    {
        if (!IsEnabled || !EditorApplication.isPlaying || !scene.IsValid() ||
            !scene.isLoaded || !HomeSceneNames.Contains(scene.name))
            return;

        GameObject catRoot = FindCatRoot(scene);
        if (catRoot == null)
            return;

        Animator legacyAnimator = catRoot.GetComponentInChildren<Animator>(true);
        if (legacyAnimator == null || IsRigV21OrApprovedCat(legacyAnimator))
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[RigV21] QA prefab is missing at '{PrefabPath}'.");
            return;
        }

        GameObject scaleWrapper = new GameObject("RigV21_VisualScale");
        SceneManager.MoveGameObjectToScene(scaleWrapper, scene);
        scaleWrapper.transform.SetParent(catRoot.transform, false);
        scaleWrapper.transform.localPosition = Vector3.zero;
        scaleWrapper.transform.localRotation = Quaternion.identity;
        scaleWrapper.transform.localScale = Vector3.one * VisualScale;

        GameObject visual = Object.Instantiate(prefab);
        visual.name = prefab.name;
        SceneManager.MoveGameObjectToScene(visual, scene);
        visual.transform.SetParent(scaleWrapper.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = prefab.transform.localRotation;
        visual.transform.localScale = Vector3.one;

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null)
        {
            Object.Destroy(scaleWrapper);
            Debug.LogError("[RigV21] QA prefab has no Animator.");
            return;
        }

        RebindAnimatorFields(catRoot, animator);
        RebindHeartSpawn(catRoot, visual);

        GameObject legacyVisual = legacyAnimator.gameObject;
        GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(legacyVisual);
        if (outermost != null && outermost.transform.parent == catRoot.transform)
            legacyVisual = outermost;

        legacyVisual.SetActive(false);
        Object.Destroy(legacyVisual);

        animator.applyRootMotion = false;
        animator.Rebind();
        animator.Update(0f);
        Debug.Log($"[RigV21] Play-only cat installed in {scene.name}.");
    }

    private static GameObject FindCatRoot(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "CatRoot")
                    return child.gameObject;
            }
        }

        return null;
    }

    private static bool IsRigV21OrApprovedCat(Animator animator)
    {
        if (animator.runtimeAnimatorController == null)
            return false;

        string controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
        return controllerPath.StartsWith("Assets/Art/CatRigV21/", StringComparison.Ordinal) ||
               controllerPath.StartsWith(ApprovedCatControllerFolder, StringComparison.Ordinal);
    }

    private static void RebindAnimatorFields(GameObject catRoot, Animator animator)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic;

        foreach (Component component in catRoot.GetComponents<Component>())
        {
            if (component == null)
                continue;

            FieldInfo field = component.GetType().GetField("animator", flags);
            if (field != null && field.FieldType == typeof(Animator))
                field.SetValue(component, animator);
        }
    }

    private static void RebindHeartSpawn(GameObject catRoot, GameObject visual)
    {
        PetHeartEffect hearts = catRoot.GetComponent<PetHeartEffect>();
        if (hearts == null)
            return;

        Transform head = null;
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Head")
            {
                head = child;
                break;
            }
        }

        if (head == null)
            return;

        FieldInfo field = typeof(PetHeartEffect).GetField(
            "spawnPoint",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(hearts, head);
    }
}
