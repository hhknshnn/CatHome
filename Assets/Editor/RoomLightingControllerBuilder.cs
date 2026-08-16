using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Places the <see cref="RoomLightingController"/> into the open scene and wires it to the
/// clock, the directional light and the room's own ceiling lamp. The lamp is a single
/// wide-angle spot light parented under the RoomLighting object, aimed straight down at the
/// centre of the floor and the carpet, so dimming it reads as "the ceiling light is being
/// turned down" instead of "the whole scene got darker".
///
/// Re-running the builder is idempotent: it reuses the existing RoomLighting object and the
/// existing ceiling lamp rather than producing a second one, and it never overwrites hourly
/// keys the designer has already tuned.
/// </summary>
public static class RoomLightingControllerBuilder
{
    private const string MenuPath = "Tools/Cat Home/Build Room Lighting Controller";
    private const string RootName = "RoomLighting";
    private const string CeilingLightName = "CeilingLight";
    private const int DefaultLayerMask = 1 << 0;

    /// <summary>
    /// Authored resting colour for a freshly created lamp, matching the 00:00 key (#FFE8C8).
    /// At runtime the controller owns this and drives it off the hourly colour ramp.
    /// </summary>
    private static readonly Color CeilingLightColor = new Color(1f, 0.9098039f, 0.7843137f, 1f);

    private const float CeilingClearance = 0.12f;
    private const float FallbackCeilingHeight = 2.8f;
    private const float MinCeilingHeight = 1.8f;
    private const float SpotAngle = 110f;
    private const float InnerSpotAngle = 72f;
    private const float MinRange = 6f;
    private const float MaxRange = 8f;
    private const float ShadowStrength = 0.45f;

    /// <summary>How far the lamp steps away from the window so the two cones do not stack.</summary>
    private const float WindowClearanceOffset = 0.5f;

    /// <summary>
    /// Read straight from the controller so the builder and the component can never disagree
    /// about what "the defaults" are.
    /// </summary>
    private static readonly RoomLightingController.LightingKey[] DefaultKeys =
        RoomLightingController.CreateDefaultKeys();

    [MenuItem(MenuPath)]
    public static void Build()
    {
        GameTimeService timeService =
            Object.FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include);
        Light directionalLight = FindDirectionalLight();

        if (timeService == null || directionalLight == null)
        {
            EditorUtility.DisplayDialog(
                "Room Lighting Controller",
                timeService == null
                    ? "No GameTimeService was found. Open GameScene and make sure the WindowSystem " +
                      "object still carries its GameTimeService component."
                    : "No directional light was found. Open GameScene and make sure its Directional " +
                      "Light still exists.",
                "OK"
            );
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Room Lighting Controller");

        RoomLightingController controller =
            Object.FindAnyObjectByType<RoomLightingController>(FindObjectsInactive.Include);

        bool created = controller == null;
        if (created)
        {
            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Room Lighting root");
            Scene targetScene = timeService.gameObject.scene;
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(root, targetScene);
                GameObject presentationGroup = CatHomeAuthoringWorkspace.FindNamedInScene(
                    targetScene,
                    CatHomeAuthoringWorkspace.PresentationGroupName);
                if (presentationGroup != null)
                {
                    Undo.SetTransformParent(
                        root.transform,
                        presentationGroup.transform,
                        "Parent Room Lighting root");
                }
            }
            controller = Undo.AddComponent<RoomLightingController>(root);
        }

        Light ceilingLight = EnsureCeilingLight(controller.transform, out bool lightCreated);

        SerializedObject serialized = new SerializedObject(controller);

        AssignIfEmpty(serialized, "timeService", timeService);
        AssignIfEmpty(serialized, "directionalLight", directionalLight);
        // Always re-point this one: it must track the lamp the builder owns.
        FindProperty(serialized, "ceilingLight").objectReferenceValue = ceilingLight;

        SerializedProperty keys = FindProperty(serialized, "keys");
        bool wroteKeys = created || keys.arraySize == 0 || NeedsCeilingUpgrade(keys);
        if (wroteKeys)
            WriteDefaultKeys(keys);

        // Keys tuned before the colour ramp existed keep their hours and intensities and only
        // gain the two missing colours. A second run finds them filled in and changes nothing.
        int upgradedColors = wroteKeys ? 0 : UpgradeMissingColors(keys);

        serialized.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = controller.gameObject;
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            $"Room Lighting Controller {(created ? "created" : "updated")} on " +
            $"'{controller.gameObject.name}'. Clock: '{timeService.gameObject.name}', " +
            $"light: '{directionalLight.gameObject.name}'. " +
            $"Ceiling lamp {(lightCreated ? "created" : "updated")} at " +
            $"{Format(ceilingLight.transform.position)}, range {ceilingLight.range:F2}, " +
            $"spot {ceilingLight.spotAngle:F0}/{ceilingLight.innerSpotAngle:F0}, soft shadows at " +
            $"strength {ceilingLight.shadowStrength:F2}. " +
            $"Hourly keys {(wroteKeys ? "written" : "left untouched")}" +
            $"{(upgradedColors > 0 ? $", {upgradedColors} key(s) upgraded to the colour ramp" : string.Empty)}.",
            controller
        );

        WarnAboutDisabledAdditionalLightShadows(controller);
    }

    /// <summary>
    /// Creates the ceiling lamp once and re-aligns it on every later run. Any spot light already
    /// parented under RoomLighting is reused - including a renamed one - so a second run can
    /// never leave the scene with two ceiling lamps.
    /// </summary>
    private static Light EnsureCeilingLight(Transform root, out bool created)
    {
        Light existing = FindExistingCeilingLight(root);
        GameObject lightObject;

        created = existing == null;
        if (created)
        {
            lightObject = new GameObject(CeilingLightName);
            Undo.RegisterCreatedObjectUndo(lightObject, "Create Ceiling Light");
            Undo.SetTransformParent(lightObject.transform, root, "Parent Ceiling Light");
        }
        else
        {
            lightObject = existing.gameObject;
        }

        ResolveCeilingPlacement(root, out Vector3 worldPosition, out float height);

        Undo.RecordObject(lightObject.transform, "Align Ceiling Light");
        lightObject.transform.position = worldPosition;
        // Straight down from the ceiling. The directional light's own rotation is never touched.
        lightObject.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        lightObject.transform.localScale = Vector3.one;

        Light light = lightObject.GetComponent<Light>();
        bool componentCreated = light == null;
        if (componentCreated)
            light = Undo.AddComponent<Light>(lightObject);

        Undo.RecordObject(light, "Configure Ceiling Light");
        light.type = LightType.Spot;
        light.color = CeilingLightColor;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = ShadowStrength;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        light.renderMode = LightRenderMode.Auto;
        // Default layer only: the room lives there, while CatCelebrationPreview's offscreen rig
        // sits on its own layer with its own light and must not pick this lamp up.
        light.cullingMask = DefaultLayerMask;

        // Cone shape is a creation-time default so a designer can tune it without the builder
        // stomping the tuning on the next run.
        if (created || componentCreated)
        {
            light.range = Mathf.Clamp(height * 2.2f, MinRange, MaxRange);
            light.spotAngle = SpotAngle;
            light.innerSpotAngle = InnerSpotAngle;
            light.intensity = DefaultKeys[0].ceilingLightIntensity;
        }

        return light;
    }

    private static Light FindExistingCeilingLight(Transform root)
    {
        Transform named = root.Find(CeilingLightName);
        if (named != null)
        {
            Light namedLight = named.GetComponent<Light>();
            if (namedLight != null)
                return namedLight;
        }

        Light[] candidates = root.GetComponentsInChildren<Light>(true);
        foreach (Light light in candidates)
        {
            if (light != null && light.type == LightType.Spot)
                return light;
        }

        return null;
    }

    /// <summary>
    /// Puts the lamp just under the ceiling, above the point halfway between the floor centre
    /// and the carpet centre, then steps it away from the window so the ceiling cone and the
    /// window's spot do not wash the same patch of floor. The step is small enough that the
    /// carpet stays well inside the cone.
    /// </summary>
    private static void ResolveCeilingPlacement(Transform root, out Vector3 position, out float height)
    {
        bool foundFloor = TryFindSceneRendererBounds(root, out Bounds floorBounds, "Floor");
        Vector3 floorCentre = foundFloor ? floorBounds.center : Vector3.zero;
        float floorTop = foundFloor ? floorBounds.max.y : 0f;

        Vector3 target = floorCentre;
        if (TryFindSceneRendererBounds(root, out Bounds carpetBounds, "Carpet", "Rug"))
            target = Vector3.Lerp(floorCentre, carpetBounds.center, 0.5f);

        height = TryFindSceneRendererBounds(root, out Bounds wallBounds, "Wall")
            ? Mathf.Max(wallBounds.max.y - floorTop, MinCeilingHeight)
            : FallbackCeilingHeight;

        Vector3 awayFromWindow = ResolveDirectionAwayFromWindow(target);
        target += awayFromWindow * WindowClearanceOffset;

        position = new Vector3(target.x, floorTop + height - CeilingClearance, target.z);
    }

    /// <summary>
    /// Horizontal direction pointing from the window into the room. Returns zero when the scene
    /// has no window, which simply leaves the lamp dead centre.
    /// </summary>
    private static Vector3 ResolveDirectionAwayFromWindow(Vector3 roomCentre)
    {
        WindowDayNightController window =
            Object.FindAnyObjectByType<WindowDayNightController>(FindObjectsInactive.Include);
        if (window == null)
            return Vector3.zero;

        Vector3 away = Vector3.ProjectOnPlane(roomCentre - window.transform.position, Vector3.up);
        return away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.zero;
    }

    private static bool TryFindSceneRendererBounds(
        Transform context,
        out Bounds bounds,
        params string[] nameFragments)
    {
        bounds = default;
        bool initialized = false;

        GameObject[] sceneRoots = context.gameObject.scene.GetRootGameObjects();
        foreach (GameObject sceneRoot in sceneRoots)
        {
            Renderer[] renderers = sceneRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !MatchesAnyFragment(renderer.gameObject.name, nameFragments))
                    continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return initialized;
    }

    private static bool MatchesAnyFragment(string name, string[] fragments)
    {
        foreach (string fragment in fragments)
        {
            if (name.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// True when the serialized keys still carry no ceiling value at all - the state left by the
    /// first lighting pass. Once any key has a ceiling intensity the array is treated as tuned
    /// and left alone, which keeps a second run idempotent.
    /// </summary>
    private static bool NeedsCeilingUpgrade(SerializedProperty keys)
    {
        for (int i = 0; i < keys.arraySize; i++)
        {
            SerializedProperty element = keys.GetArrayElementAtIndex(i);
            SerializedProperty ceiling = element.FindPropertyRelative("ceilingLightIntensity");
            if (ceiling != null && ceiling.floatValue > 0.0001f)
                return false;
        }

        return true;
    }

    /// <summary>
    /// The ceiling lamp is authored with soft shadows, but URP only renders them when the render
    /// pipeline asset for that quality level allows additional-light shadows. This is left as a
    /// warning rather than an edit: turning them on is a project-wide, mobile-visible cost.
    /// </summary>
    private static void WarnAboutDisabledAdditionalLightShadows(Object context)
    {
        List<string> levels = new List<string>();
        string[] names = QualitySettings.names;

        for (int i = 0; i < names.Length; i++)
        {
            RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(i);
            if (asset == null)
                asset = GraphicsSettings.defaultRenderPipeline;
            if (asset == null)
                continue;

            SerializedObject serialized = new SerializedObject(asset);
            SerializedProperty additional = serialized.FindProperty("m_AdditionalLightShadowsSupported");
            SerializedProperty soft = serialized.FindProperty("m_SoftShadowsSupported");

            if (additional == null || soft == null)
                continue;

            if (!additional.boolValue || !soft.boolValue)
                levels.Add(names[i]);
        }

        if (levels.Count == 0)
            return;

        Debug.LogWarning(
            $"Ceiling lamp shadows will not render on these quality levels: {string.Join(", ", levels)}. " +
            "Enable Lighting > Additional Lights > Cast Shadows and Shadows > Soft Shadows on the " +
            "matching URP asset if you want the soft shadow there. The lamp itself still lights " +
            "the room, and leaving these off is the cheaper mobile setting.",
            context
        );
    }

    /// <summary>
    /// Mirrors <see cref="RoomLightingController"/>'s own lookup: the scene's nominated sun
    /// first, then a directional light that can reach the Default layer. The window system's
    /// spot light, the offscreen portrait rig's light and the new ceiling lamp are never
    /// candidates.
    /// </summary>
    private static Light FindDirectionalLight()
    {
        Light sun = RenderSettings.sun;
        if (sun != null && sun.type == LightType.Directional)
            return sun;

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);

        Light fallback = null;
        foreach (Light light in lights)
        {
            if (light.type != LightType.Directional)
                continue;

            if ((light.cullingMask & DefaultLayerMask) != 0)
                return light;

            fallback ??= light;
        }

        return fallback;
    }

    private static void WriteDefaultKeys(SerializedProperty keys)
    {
        keys.arraySize = DefaultKeys.Length;

        for (int i = 0; i < DefaultKeys.Length; i++)
        {
            SerializedProperty element = keys.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("hour").floatValue = DefaultKeys[i].hour;
            element.FindPropertyRelative("ambientIntensity").floatValue = DefaultKeys[i].ambientIntensity;
            element.FindPropertyRelative("directionalIntensity").floatValue = DefaultKeys[i].directionalIntensity;
            element.FindPropertyRelative("ceilingLightIntensity").floatValue = DefaultKeys[i].ceilingLightIntensity;
            element.FindPropertyRelative("directionalColor").colorValue = DefaultKeys[i].directionalColor;
            element.FindPropertyRelative("ceilingColor").colorValue = DefaultKeys[i].ceilingColor;
        }
    }

    /// <summary>
    /// Fills in the two colours on keys that predate them, sampling the shipped ramp at each
    /// key's own hour. Colours already authored - including ones a designer changed away from
    /// the default - are never touched, so re-running the builder is a no-op. Returns how many
    /// keys were upgraded.
    /// </summary>
    private static int UpgradeMissingColors(SerializedProperty keys)
    {
        int upgraded = 0;

        for (int i = 0; i < keys.arraySize; i++)
        {
            SerializedProperty element = keys.GetArrayElementAtIndex(i);
            SerializedProperty directional = element.FindPropertyRelative("directionalColor");
            SerializedProperty ceiling = element.FindPropertyRelative("ceilingColor");
            if (directional == null || ceiling == null)
                continue;

            bool directionalUnset = RoomLightingController.IsUnsetColor(directional.colorValue);
            bool ceilingUnset = RoomLightingController.IsUnsetColor(ceiling.colorValue);
            if (!directionalUnset && !ceilingUnset)
                continue;

            float hour = element.FindPropertyRelative("hour").floatValue;
            RoomLightingController.SampleDefaultColors(
                hour,
                out Color defaultDirectional,
                out Color defaultCeiling
            );

            if (directionalUnset)
                directional.colorValue = defaultDirectional;
            if (ceilingUnset)
                ceiling.colorValue = defaultCeiling;

            upgraded++;
        }

        return upgraded;
    }

    private static void AssignIfEmpty(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = FindProperty(serialized, propertyName);
        if (property.objectReferenceValue == null)
            property.objectReferenceValue = value;
    }

    private static SerializedProperty FindProperty(SerializedObject serialized, string propertyName)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            throw new System.InvalidOperationException(
                "Missing serialized RoomLightingController property: " + propertyName
            );
        }

        return property;
    }

    private static string Format(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }
}
