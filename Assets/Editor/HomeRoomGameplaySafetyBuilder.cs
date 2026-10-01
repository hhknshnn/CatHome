using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Authors the invisible walkable boundary shared by every home room and keeps
/// Living Room's fixed furniture and daylight presentation gameplay-safe.
/// </summary>
public static class HomeRoomGameplaySafetyBuilder
{
    public const string BoundaryRootName = "RoomMovementBoundary";
    public const string LivingSofaName = "LivingSofa_PremiumModel";
    public const string LivingCoffeeTableName = "LivingCoffeeTable_PremiumModel";
    public const float LivingRoomDaylightHour = 12f;

    private const float BoundaryClearance = 0.05f;

    private static readonly string[] RoomScenePaths =
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

    [MenuItem("Tools/Cat Home/Architecture/Repair Room Movement Safety")]
    public static void ApplyAllScenesSilently()
    {
        Scene activeBefore = SceneManager.GetActiveScene();
        for (int i = 0; i < RoomScenePaths.Length; i++)
        {
            string path = RoomScenePaths[i];
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            ApplyToScene(scene);
            EditorSceneManager.SaveScene(scene);

            if (opened)
                EditorSceneManager.CloseScene(scene, true);
        }

        if (activeBefore.IsValid() && activeBefore.isLoaded)
            SceneManager.SetActiveScene(activeBefore);
        AssetDatabase.SaveAssets();
        Debug.Log("Room movement boundaries, Living Room daylight and fixed furniture colliders repaired.");
    }

    public static void ApplyToScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            throw new ArgumentException("A loaded room scene is required.", nameof(scene));

        EnsureBoundary(scene);
        if (scene.path == HomeRoomService.LivingRoomScenePath)
            ApplyLivingRoomFixes(scene);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void EnsureBoundary(Scene scene)
    {
        Transform existing = FindNamed(scene, BoundaryRootName);
        GameObject root;
        if (existing == null)
        {
            root = new GameObject(BoundaryRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create room movement boundary");
            SceneManager.MoveGameObjectToScene(root, scene);
        }
        else
        {
            root = existing.gameObject;
        }

        HomeRoomBoundary boundary = root.GetComponent<HomeRoomBoundary>();
        if (boundary == null)
            boundary = Undo.AddComponent<HomeRoomBoundary>(root);
        Undo.RecordObject(boundary, "Configure room movement boundary");
        boundary.Configure(
            new Vector2(HomeRoomShellMetrics.InteriorMinX, HomeRoomShellMetrics.InteriorMinZ),
            new Vector2(HomeRoomShellMetrics.InteriorMaxX, HomeRoomShellMetrics.InteriorMaxZ),
            BoundaryClearance);
        EditorUtility.SetDirty(boundary);
    }

    private static void ApplyLivingRoomFixes(Scene scene)
    {
        GameTimeService timeService = FindComponent<GameTimeService>(scene);
        if (timeService == null)
            throw new InvalidOperationException("Living Room has no GameTimeService.");

        SerializedObject serializedTime = new SerializedObject(timeService);
        serializedTime.FindProperty("useTestTime").boolValue = true;
        serializedTime.FindProperty("testHour").floatValue = LivingRoomDaylightHour;
        serializedTime.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(timeService);

        EnsureFurnitureCollider(scene, LivingSofaName);
        EnsureFurnitureCollider(scene, LivingCoffeeTableName);
        LivingRoomArrangementBuilder.Apply(scene);

        WindowDayNightController window = FindComponent<WindowDayNightController>(scene);
        if (window != null)
        {
            window.RefreshVisuals();
            EditorUtility.SetDirty(window);
        }

        if (SceneManager.GetActiveScene() == scene)
            ApplyAuthoredNoonLighting(scene);
    }

    private static void ApplyAuthoredNoonLighting(Scene scene)
    {
        const float multiplier = PlayerLightingPreference.CanonicalMultiplier;
        RenderSettings.ambientIntensity = 0.94f * multiplier;

        Light directional = RenderSettings.sun;
        if (directional == null || directional.gameObject.scene != scene)
        {
            Light[] lights = FindComponents<Light>(scene);
            for (int i = 0; i < lights.Length; i++)
                if (lights[i].type == LightType.Directional)
                {
                    directional = lights[i];
                    break;
                }
        }

        if (directional != null)
        {
            directional.intensity = 1.5675f * multiplier;
            directional.color = new Color(1f, 0.9568627f, 0.8392157f, 1f);
            EditorUtility.SetDirty(directional);
        }

        RoomLightingController roomLighting = FindComponent<RoomLightingController>(scene);
        if (roomLighting == null)
            return;
        Light[] ownedLights = roomLighting.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < ownedLights.Length; i++)
        {
            if (ownedLights[i].type != LightType.Spot)
                continue;
            ownedLights[i].intensity = 0.1675f * multiplier;
            ownedLights[i].color = new Color(1f, 0.9411765f, 0.854902f, 1f);
            EditorUtility.SetDirty(ownedLights[i]);
        }
    }

    private static void EnsureFurnitureCollider(Scene scene, string objectName)
    {
        Transform target = FindNamed(scene, objectName);
        if (target == null)
            throw new InvalidOperationException($"Living Room fixed furniture '{objectName}' is missing.");

        HomeFixedFurnitureCollisionBuilder.EnsureMeshGeometry(target);
    }

    private static Transform FindNamed(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < all.Length; j++)
                if (string.Equals(all[j].name, objectName, StringComparison.Ordinal))
                    return all[j];
        }
        return null;
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        T[] components = FindComponents<T>(scene);
        return components.Length > 0 ? components[0] : null;
    }

    private static T[] FindComponents<T>(Scene scene) where T : Component
    {
        var results = new System.Collections.Generic.List<T>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            results.AddRange(roots[i].GetComponentsInChildren<T>(true));
        return results.ToArray();
    }
}
