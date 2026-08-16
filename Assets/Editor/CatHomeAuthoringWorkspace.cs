using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the editor authoring view aligned with the runtime scene stack and
/// gives the living-room scene a stable, builder-friendly hierarchy.
/// </summary>
public static class CatHomeAuthoringWorkspace
{
    public const string BootstrapScenePath = "Assets/Scenes/GameScene.unity";
    public const string UiScenePath = "Assets/Scenes/UI/CatHome_UI.unity";
    public const string LevelScenePath = "Assets/Scenes/Levels/LivingRoom_Level01.unity";
    public const string RunnerScenePath = "Assets/Scenes/Runner/CatRunner.unity";

    public const string EnvironmentGroupName = "01 Environment";
    public const string FurnitureGroupName = "02 Furniture";
    public const string CharacterGroupName = "03 Character";
    public const string GameplayGroupName = "04 Gameplay";
    public const string PresentationGroupName = "05 Presentation";
    public const string LocalUiGroupName = "06 Local UI";
    public const string LevelSetupGroupName = "07 Level Setup";

    private const string WorkspaceMenu =
        "Tools/Cat Home/Workspace/Open Full Home Preview";
    private const string OrganizeMenu =
        "Tools/Cat Home/Workspace/Organize Living Room Hierarchy";
    private const string DisplayMenu =
        "Tools/Cat Home/Workspace/Apply 1920x1080 Landscape Preview";

    private const int PreviewWidth = 1920;
    private const int PreviewHeight = 1080;
    private const string PreviewLabel = "Cat Home 1920x1080";
    private const string McpTestRunActiveKey = "TestRunnerNoThrottle_TestRunActive";
    private const string McpPlayModeRestorePendingKey =
        "MCPForUnity.PlayModeOptions.PendingRestore";

    private static readonly string[] EnvironmentObjects =
    {
        "Floor",
        "BackWall",
        "Baseboard_Back",
        "LeftWall",
        "Baseboard_Left",
        "RightWall",
        "Baseboard_Right",
        "FrontBoundary"
    };

    private static readonly string[] FurnitureObjects =
    {
        "RoomFurniture"
    };

    private static readonly string[] CharacterObjects =
    {
        "CatRoot"
    };

    private static readonly string[] GameplayObjects =
    {
        "FoodInteractionPoint",
        "WaterInteractionPoint",
        "GameplayActivities"
    };

    private static readonly string[] PresentationObjects =
    {
        "Main Camera",
        "Directional Light",
        "WindowSystem",
        "RoomLighting"
    };

    private static readonly string[] LocalUiObjects =
    {
        "PetTutorialCanvas"
    };

    private static readonly string[] LevelSetupObjects =
    {
        "LevelArchitecture",
        "SpawnPoint_default"
    };

    [InitializeOnLoadMethod]
    private static void InitializeAuthoringWorkspace()
    {
        if (Application.isBatchMode)
            return;

        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

        ConfigurePlayModeStartScene();
        ConfigureCanonicalPlayModeReloadPolicy();
        EditorApplication.delayCall += RestoreCanonicalWorkspace;
    }

    [MenuItem(WorkspaceMenu, priority = 1)]
    public static void OpenFullHomePreview()
    {
        OpenFullHomePreview(true);
    }

    public static void OpenFullHomePreview(bool askToSave)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Full Home Preview cannot be opened while Play Mode is changing.");
            return;
        }

        if (askToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!askToSave)
            EditorSceneManager.SaveOpenScenes();

        EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
        EditorSceneManager.OpenScene(UiScenePath, OpenSceneMode.Additive);
        Scene levelScene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Additive);

        bool hierarchyChanged = OrganizeLivingRoomHierarchy(levelScene, true);
        EditorSceneManager.SetActiveScene(levelScene);
        if (hierarchyChanged)
            EditorSceneManager.SaveScene(levelScene);

        ApplyCanonicalDisplaySettings();
        TrySelectGameViewPreset();
        ConfigurePlayModeStartScene();
        ConfigureCanonicalPlayModeReloadPolicy();
        ApplyHierarchyPresentation();
        ScheduleHierarchyPresentation();

        SceneView.RepaintAll();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        Debug.Log(
            "Full Home Preview ready: GameScene + CatHome_UI + LivingRoom_Level01, " +
            "with LivingRoom_Level01 active at 1920x1080 landscape."
        );
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        // The automated PlayMode runner owns both its temporary start scene and
        // Enter Play Mode options. Reapplying the authoring policy here replaces
        // that bootstrap with GameScene, so the tests never reach RunStarted.
        if (IsAutomatedTestTransition())
            return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Apply the policy immediately before Unity snapshots the editor scene stack.
            // This closes the small timing window between a domain reload and pressing Play.
            ConfigurePlayModeStartScene();
            ConfigureCanonicalPlayModeReloadPolicy();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Keep the runtime scene stack readable in the same way as the authoring workspace.
            ScheduleHierarchyPresentation();
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += RestoreCanonicalWorkspace;
        }
    }

    private static void RestoreCanonicalWorkspace()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (IsCanonicalHomeWorkspaceOpen())
        {
            ConfigurePlayModeStartScene();
            ConfigureCanonicalPlayModeReloadPolicy();
            ScheduleHierarchyPresentation();
            return;
        }

        // Only take over Cat Home's known play/authoring scenes. Sample scenes, prefab stages
        // and third-party content remain under the designer's control.
        if (SceneManager.sceneCount == 1 && IsCatHomeWorkspaceScene(
                SceneManager.GetActiveScene().path))
        {
            OpenFullHomePreview(false);
        }
    }

    private static bool IsCanonicalHomeWorkspaceOpen()
    {
        return SceneManager.sceneCount == 3 &&
               IsSceneLoaded(BootstrapScenePath) &&
               IsSceneLoaded(UiScenePath) &&
               IsSceneLoaded(LevelScenePath) &&
               SceneManager.GetActiveScene().path == LevelScenePath;
    }

    private static bool IsSceneLoaded(string path)
    {
        Scene scene = SceneManager.GetSceneByPath(path);
        return scene.IsValid() && scene.isLoaded;
    }

    private static bool IsCatHomeWorkspaceScene(string path)
    {
        return path == BootstrapScenePath ||
               path == UiScenePath ||
               HomeRoomService.IsKnownScenePath(path) ||
               path == RunnerScenePath;
    }

    private static bool IsSingleRoomAuthoringSceneOpen()
    {
        return SceneManager.sceneCount == 1 &&
               HomeRoomService.IsKnownScenePath(SceneManager.GetActiveScene().path);
    }

    /// <summary>
    /// The canonical authoring workspace already contains the exact bootstrap + UI + room stack,
    /// so Play must preserve it instead of closing the visible scenes and briefly leaving the Game
    /// view without a camera. A non-canonical Cat Home scene still starts through the bootstrap,
    /// matching a real build and preventing direct scene entry from skipping initialization.
    /// </summary>
    private static void ConfigurePlayModeStartScene()
    {
        if (IsAutomatedTestTransition())
            return;

        if (IsCanonicalHomeWorkspaceOpen() || IsSingleRoomAuthoringSceneOpen())
        {
            if (EditorSceneManager.playModeStartScene != null)
                EditorSceneManager.playModeStartScene = null;
            return;
        }

        SceneAsset bootstrap = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath);
        if (bootstrap != null && EditorSceneManager.playModeStartScene != bootstrap)
            EditorSceneManager.playModeStartScene = bootstrap;
    }

    /// <summary>
    /// Unity normally serializes, destroys and reloads every open scene when Play starts. In this
    /// project's three-scene authoring stack the room camera lives in the last scene, so the Game
    /// view can expose Unity's temporary "No cameras rendering" frame while those scenes reload.
    /// Unity 6's DisableSceneReload mode resets the scene state and invokes the required lifecycle
    /// processing without destroying the already visible scene objects, removing that camera gap.
    /// Domain reload remains enabled so static runtime state still starts clean on every Play.
    /// </summary>
    private static void ConfigureCanonicalPlayModeReloadPolicy()
    {
        if (IsAutomatedTestTransition())
            return;

        if (!IsCanonicalHomeWorkspaceOpen() && !IsSingleRoomAuthoringSceneOpen())
            return;

        const EnterPlayModeOptions desiredOptions = EnterPlayModeOptions.DisableSceneReload;
        if (EditorSettings.enterPlayModeOptions != desiredOptions)
            EditorSettings.enterPlayModeOptions = desiredOptions;
        if (!EditorSettings.enterPlayModeOptionsEnabled)
            EditorSettings.enterPlayModeOptionsEnabled = true;
    }

    private static bool IsAutomatedTestTransition()
    {
        return SessionState.GetBool(McpTestRunActiveKey, false) ||
               SessionState.GetBool(McpPlayModeRestorePendingKey, false);
    }

    /// <summary>
    /// Unity must keep the bootstrap, UI and room as separate runtime scenes, but the Hierarchy
    /// should still present their contents together. Expanding the scene headers and their root
    /// groups prevents the editor from looking like it contains only the last active scene.
    /// Reflection is isolated here because SceneHierarchy is an editor-only internal API.
    /// </summary>
    private static void ApplyHierarchyPresentation()
    {
        try
        {
            Assembly editorAssembly = typeof(Editor).Assembly;
            Type windowType = editorAssembly.GetType("UnityEditor.SceneHierarchyWindow");
            if (windowType == null)
                return;

            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(windowType);
            if (windows.Length == 0)
                return;

            MethodInfo clearSearch = windowType.GetMethod("ClearSearchFilter", flags);
            MethodInfo setExpanded = FindTwoParameterMethod(windowType, "SetExpanded");
            PropertyInfo hierarchyProperty = windowType.GetProperty("sceneHierarchy", flags);

            var expandedSceneNames = new List<string>
            {
                "GameScene",
                "CatHome_UI",
                "LivingRoom_Level01"
            };

            for (int windowIndex = 0; windowIndex < windows.Length; windowIndex++)
            {
                object window = windows[windowIndex];
                clearSearch?.Invoke(window, null);

                object hierarchy = hierarchyProperty?.GetValue(window, null);
                MethodInfo setScenesExpanded = hierarchy?.GetType().GetMethod(
                    "SetScenesExpanded",
                    flags,
                    null,
                    new[] { typeof(List<string>) },
                    null);
                setScenesExpanded?.Invoke(hierarchy, new object[] { expandedSceneNames });

                // Reload first so the tree contains the newly opened scene rows. Expanding
                // object rows before ReloadData would be discarded by the reload.
                MethodInfo reload = windowType.GetMethod("ReloadData", flags);
                reload?.Invoke(window, null);

                if (setExpanded != null)
                {
                    for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
                    {
                        Scene scene = SceneManager.GetSceneAt(sceneIndex);
                        if (!scene.isLoaded || !IsCatHomeWorkspaceScene(scene.path))
                            continue;

                        GameObject[] roots = scene.GetRootGameObjects();
                        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                        {
                            setExpanded.Invoke(
                                window,
                                new object[] { roots[rootIndex].GetEntityId(), true });
                        }
                    }
                }

                (window as EditorWindow)?.Repaint();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Cat Home scenes are loaded, but the Hierarchy could not be expanded automatically: " +
                exception.Message);
        }
    }

    [MenuItem("Tools/Cat Home/Workspace/Show Complete Hierarchy", priority = 4)]
    public static void ShowCompleteHierarchy()
    {
        ApplyHierarchyPresentation();
        ScheduleHierarchyPresentation();
    }

    private static void ScheduleHierarchyPresentation()
    {
        // Unity restores the Hierarchy tree state a frame after a domain/scene reload. Apply
        // once on the next delay call and once more after that restoration has completed.
        EditorApplication.delayCall += () =>
        {
            ApplyHierarchyPresentation();
            EditorApplication.delayCall += ApplyHierarchyPresentation;
        };
    }

    private static MethodInfo FindTwoParameterMethod(Type type, string methodName)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo[] methods = type.GetMethods(flags);
        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name == methodName && methods[i].GetParameters().Length == 2)
                return methods[i];
        }

        return null;
    }

    [MenuItem(OrganizeMenu, priority = 2)]
    public static void OrganizeLivingRoomHierarchyFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("The living-room hierarchy cannot be reorganized in Play Mode.");
            return;
        }

        Scene levelScene = SceneManager.GetSceneByPath(LevelScenePath);
        if (!levelScene.IsValid() || !levelScene.isLoaded)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            levelScene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        }

        bool changed = OrganizeLivingRoomHierarchy(levelScene, true);
        EditorSceneManager.SetActiveScene(levelScene);
        if (changed)
            EditorSceneManager.SaveScene(levelScene);

        Debug.Log(changed
            ? "LivingRoom_Level01 hierarchy organized and saved."
            : "LivingRoom_Level01 hierarchy was already organized.");
    }

    [MenuItem(DisplayMenu, priority = 3)]
    public static void ApplyLandscapePreviewFromMenu()
    {
        ApplyCanonicalDisplaySettings();
        bool selected = TrySelectGameViewPreset();
        Debug.Log(selected
            ? "Cat Home Game View set to 1920x1080 landscape."
            : "Project display settings were updated, but the Game View preset could not be selected automatically.");
    }

    public static bool OrganizeLivingRoomHierarchy(Scene scene, bool recordUndo)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.path != LevelScenePath)
            return false;

        GameObject[] groups =
        {
            EnsureRootGroup(scene, EnvironmentGroupName, recordUndo),
            EnsureRootGroup(scene, FurnitureGroupName, recordUndo),
            EnsureRootGroup(scene, CharacterGroupName, recordUndo),
            EnsureRootGroup(scene, GameplayGroupName, recordUndo),
            EnsureRootGroup(scene, PresentationGroupName, recordUndo),
            EnsureRootGroup(scene, LocalUiGroupName, recordUndo),
            EnsureRootGroup(scene, LevelSetupGroupName, recordUndo)
        };

        bool changed = false;
        changed |= MoveObjects(scene, EnvironmentObjects, groups[0].transform, recordUndo);
        changed |= MoveObjects(scene, FurnitureObjects, groups[1].transform, recordUndo);
        changed |= MoveObjects(scene, CharacterObjects, groups[2].transform, recordUndo);
        changed |= MoveObjects(scene, GameplayObjects, groups[3].transform, recordUndo);
        changed |= MoveObjects(scene, PresentationObjects, groups[4].transform, recordUndo);
        changed |= MoveObjects(scene, LocalUiObjects, groups[5].transform, recordUndo);
        changed |= MoveObjects(scene, LevelSetupObjects, groups[6].transform, recordUndo);

        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i].transform.GetSiblingIndex() == i)
                continue;
            if (recordUndo)
                Undo.RecordObject(groups[i].transform, "Order Cat Home hierarchy groups");
            groups[i].transform.SetSiblingIndex(i);
            changed = true;
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(scene);
        return changed;
    }

    public static GameObject FindNamedInScene(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(objectName))
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
            for (int t = 0; t < transforms.Length; t++)
            {
                if (transforms[t].name == objectName)
                    return transforms[t].gameObject;
            }
        }
        return null;
    }

    private static GameObject EnsureRootGroup(Scene scene, string groupName, bool recordUndo)
    {
        GameObject group = FindRoot(scene, groupName);
        if (group != null)
            return group;

        group = new GameObject(groupName);
        SceneManager.MoveGameObjectToScene(group, scene);
        if (recordUndo)
            Undo.RegisterCreatedObjectUndo(group, "Create Cat Home hierarchy group");
        return group;
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
                return roots[i];
        }
        return null;
    }

    private static bool MoveObjects(
        Scene scene,
        string[] objectNames,
        Transform parent,
        bool recordUndo)
    {
        bool changed = false;
        for (int i = 0; i < objectNames.Length; i++)
        {
            GameObject target = FindNamedInScene(scene, objectNames[i]);
            if (target == null || target.transform.parent == parent)
                continue;

            if (recordUndo)
                Undo.SetTransformParent(target.transform, parent, "Organize Cat Home hierarchy");
            else
                target.transform.SetParent(parent, true);
            changed = true;
        }
        return changed;
    }

    private static void ApplyCanonicalDisplaySettings()
    {
        PlayerSettings.defaultScreenWidth = PreviewWidth;
        PlayerSettings.defaultScreenHeight = PreviewHeight;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        AssetDatabase.SaveAssets();
    }

    private static bool TrySelectGameViewPreset()
    {
        try
        {
            Assembly editorAssembly = typeof(Editor).Assembly;
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeEnumType = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            if (sizesType == null || sizeType == null || sizeEnumType == null || gameViewType == null)
                return false;

            const BindingFlags instanceFlags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            const BindingFlags staticFlags =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy;

            PropertyInfo instanceProperty = sizesType.GetProperty("instance", staticFlags);
            object sizes = instanceProperty != null ? instanceProperty.GetValue(null, null) : null;
            PropertyInfo currentGroupProperty = sizesType.GetProperty("currentGroup", instanceFlags);
            object group = currentGroupProperty != null && sizes != null
                ? currentGroupProperty.GetValue(sizes, null)
                : null;
            if (group == null)
                return false;

            Type groupType = group.GetType();
            MethodInfo getTotalCount = groupType.GetMethod("GetTotalCount", instanceFlags);
            MethodInfo getSize = groupType.GetMethod("GetGameViewSize", instanceFlags);
            MethodInfo addCustomSize = groupType.GetMethod("AddCustomSize", instanceFlags);
            if (getTotalCount == null || getSize == null || addCustomSize == null)
                return false;

            int selectedIndex = FindPreviewSizeIndex(group, getTotalCount, getSize, sizeType);
            if (selectedIndex < 0)
            {
                object fixedResolution = Enum.Parse(sizeEnumType, "FixedResolution");
                object size = Activator.CreateInstance(
                    sizeType,
                    instanceFlags,
                    null,
                    new[] { fixedResolution, (object)PreviewWidth, PreviewHeight, PreviewLabel },
                    CultureInfo.InvariantCulture);
                addCustomSize.Invoke(group, new[] { size });

                MethodInfo save = sizesType.GetMethod("SaveToHDD", instanceFlags);
                if (save != null)
                    save.Invoke(sizes, null);
                selectedIndex = FindPreviewSizeIndex(group, getTotalCount, getSize, sizeType);
            }

            if (selectedIndex < 0)
                return false;

            UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(gameViewType);
            EditorWindow gameView = views.Length > 0
                ? views[0] as EditorWindow
                : EditorWindow.GetWindow(gameViewType, false, "Game");
            if (gameView == null)
                return false;

            PropertyInfo selectedSize = gameViewType.GetProperty("selectedSizeIndex", instanceFlags);
            if (selectedSize == null || !selectedSize.CanWrite)
                return false;

            selectedSize.SetValue(gameView, selectedIndex, null);
            gameView.Repaint();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not select the Cat Home Game View preset: " + exception.Message);
            return false;
        }
    }

    private static int FindPreviewSizeIndex(
        object group,
        MethodInfo getTotalCount,
        MethodInfo getSize,
        Type sizeType)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        PropertyInfo widthProperty = sizeType.GetProperty("width", flags);
        PropertyInfo heightProperty = sizeType.GetProperty("height", flags);
        if (widthProperty == null || heightProperty == null)
            return -1;

        int total = (int)getTotalCount.Invoke(group, null);
        for (int i = 0; i < total; i++)
        {
            object size = getSize.Invoke(group, new object[] { i });
            int width = (int)widthProperty.GetValue(size, null);
            int height = (int)heightProperty.GetValue(size, null);
            if (width == PreviewWidth && height == PreviewHeight)
                return i;
        }
        return -1;
    }
}
