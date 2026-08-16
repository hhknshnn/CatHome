using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Drives room brightness from the game clock so the room does not stay at the same
/// brightness all day long. It reads the existing <see cref="GameTimeService"/> - the same
/// clock the window visuals use - and never introduces a second time source.
///
/// Five values are touched: the skybox ambient intensity, the directional light intensity and
/// colour, and the intensity and colour of the single ceiling spot light this object owns. The
/// ceiling lamp is what makes the hour read as "the room light is being dimmed" rather than
/// "the whole scene got darker". Both light colours stay at a natural warm white throughout the
/// day so the hourly effect changes brightness without tinting the room's material colours.
/// Light direction, the window system, the skybox, the volume profile, post-processing and
/// materials are all left untouched.
/// </summary>
[DisallowMultipleComponent]
public class RoomLightingController : MonoBehaviour
{
    /// <summary>
    /// A single point on the daily brightness curve. Values between two keys are
    /// interpolated, so the room never jumps from one brightness to another.
    /// </summary>
    [Serializable]
    public struct LightingKey
    {
        [Tooltip("Game hour this key applies to (0-24).")]
        [Range(0f, 24f)] public float hour;

        [Tooltip("RenderSettings.ambientIntensity at this hour.")]
        [Min(0f)] public float ambientIntensity;

        [Tooltip("Directional light intensity at this hour.")]
        [Min(0f)] public float directionalIntensity;

        [Tooltip("Ceiling spot light intensity at this hour. High at night, low at noon.")]
        [Min(0f)] public float ceilingLightIntensity;

        [Tooltip("Legacy serialized value retained for scene compatibility. Runtime uses the " +
                 "controller's fixed natural warm-white directional colour.")]
        [ColorUsage(false)] public Color directionalColor;

        [Tooltip("Legacy serialized value retained for scene compatibility. Runtime uses the " +
                 "controller's fixed warm-white ceiling colour.")]
        [ColorUsage(false)] public Color ceilingColor;

        public LightingKey(
            float hour,
            float ambientIntensity,
            float directionalIntensity,
            float ceilingLightIntensity,
            Color directionalColor,
            Color ceilingColor)
        {
            this.hour = hour;
            this.ambientIntensity = ambientIntensity;
            this.directionalIntensity = directionalIntensity;
            this.ceilingLightIntensity = ceilingLightIntensity;
            this.directionalColor = directionalColor;
            this.ceilingColor = ceilingColor;
        }
    }

    /// <summary>
    /// Fixed, low-tint light colours. Hourly brightness still comes from the intensity keys.
    /// </summary>
    private static readonly Color FixedDirectionalColor =
        new Color(1f, 0.9568627f, 0.8392157f, 1f); // #FFF4D6

    private static readonly Color FixedCeilingColor =
        new Color(1f, 0.9411765f, 0.854902f, 1f); // #FFF0DA

    private static readonly LightingKey[] DefaultKeys =
    {
        new LightingKey(0f, 0.35f, 0.50f, 0.75f,
            FixedDirectionalColor, FixedCeilingColor),
        new LightingKey(6f, 0.59f, 0.94f, 0.33f,
            FixedDirectionalColor, FixedCeilingColor),
        new LightingKey(9f, 0.79f, 1.32f, 0.22f,
            FixedDirectionalColor, FixedCeilingColor),
        new LightingKey(13f, 0.99f, 1.65f, 0.15f,
            FixedDirectionalColor, FixedCeilingColor),
        new LightingKey(18f, 0.75f, 1.16f, 0.33f,
            FixedDirectionalColor, FixedCeilingColor),
        new LightingKey(21f, 0.51f, 0.72f, 0.68f,
            FixedDirectionalColor, FixedCeilingColor)
    };

    /// <summary>
    /// A fresh copy of the shipped defaults. The builder writes these into the scene, so both
    /// sides read from one table instead of drifting apart.
    /// </summary>
    public static LightingKey[] CreateDefaultKeys()
    {
        LightingKey[] copy = new LightingKey[DefaultKeys.Length];
        Array.Copy(DefaultKeys, copy, DefaultKeys.Length);
        return copy;
    }

    /// <summary>
    /// Returns the fixed light colours used for legacy-key migration and builder-created keys.
    /// </summary>
    public static void SampleDefaultColors(float hour, out Color directional, out Color ceiling)
    {
        directional = FixedDirectionalColor;
        ceiling = FixedCeilingColor;
    }

    /// <summary>Pure black is the shape old serialized data takes, not an authored colour.</summary>
    public static bool IsUnsetColor(Color color)
    {
        return color.maxColorComponent < 0.004f;
    }

    /// <summary>Longest frame delta fed into the smoothing, so a resume from background eases in.</summary>
    private const float MaxSmoothingDelta = 0.25f;

    /// <summary>Writes below this delta are skipped; ambient changes rebuild the ambient probe.</summary>
    private const float ApplyEpsilon = 0.0005f;

    /// <summary>The room sits on the Default layer; offscreen preview rigs use their own.</summary>
    private const int DefaultLayerMask = 1 << 0;

    [Header("References")]
    [Tooltip("Leave empty to resolve the scene's GameTimeService automatically.")]
    [SerializeField] private GameTimeService timeService;

    [Tooltip("Leave empty to resolve the scene's directional light automatically.")]
    [SerializeField] private Light directionalLight;

    [Tooltip("The ceiling lamp. Leave empty to resolve the spot light parented under this object.")]
    [SerializeField] private Light ceilingLight;

    [Header("Hourly Keys")]
    [Tooltip("Edit freely. Order does not matter, hours wrap around midnight.")]
    [SerializeField]
    private LightingKey[] keys = CreateDefaultKeys();

    [Header("Transition")]
    [Tooltip("Seconds the room takes to settle after the target brightness moves. 0 snaps.")]
    [SerializeField, Min(0f)] private float smoothingSeconds = 0.5f;

    private LightingKey[] sortedKeys;

    private float currentAmbient;
    private float currentDirectional;
    private float currentCeiling;
    private Color currentDirectionalColor;
    private Color currentCeilingColor;
    private float ambientVelocity;
    private float directionalVelocity;
    private float ceilingVelocity;
    private Vector3 directionalColorVelocity;
    private Vector3 ceilingColorVelocity;

    private float originalAmbientIntensity;
    private float originalDirectionalIntensity;
    private float originalCeilingIntensity;
    private Color originalDirectionalColor;
    private Color originalCeilingColor;
    private bool hasCapturedOriginals;
    private bool hasCapturedCeilingOriginal;

    private bool hasAppliedOnce;
    private double lastUpdateRealtime;

    private void OnEnable()
    {
        RebuildKeyCache();

        if (sortedKeys == null)
        {
            Debug.LogWarning(
                "RoomLightingController has no lighting keys, so it stays disabled.",
                this
            );
            enabled = false;
            return;
        }

        if (!ResolveReferences())
        {
            enabled = false;
            return;
        }

        if (RenderSettings.ambientMode != AmbientMode.Skybox)
        {
            Debug.LogWarning(
                "RoomLightingController: Ambient Mode is not Skybox, so ambient intensity has no " +
                "visible effect. Set Lighting > Environment > Ambient Mode to Skybox, or drive " +
                "RenderSettings.ambientLight instead. The directional light is still animated.",
                this
            );
        }

        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalDirectionalIntensity = directionalLight.intensity;
        originalDirectionalColor = directionalLight.color;
        hasCapturedOriginals = true;

        hasCapturedCeilingOriginal = ceilingLight != null;
        if (hasCapturedCeilingOriginal)
        {
            originalCeilingIntensity = ceilingLight.intensity;
            originalCeilingColor = ceilingLight.color;
        }

        hasAppliedOnce = false;
        ambientVelocity = 0f;
        directionalVelocity = 0f;
        ceilingVelocity = 0f;
        directionalColorVelocity = Vector3.zero;
        ceilingColorVelocity = Vector3.zero;

        // Start at the value for the current hour rather than fading in from the authored one.
        UpdateLighting();
    }

    private void OnDisable()
    {
        if (!hasCapturedOriginals)
            return;

        RenderSettings.ambientIntensity = originalAmbientIntensity;
        if (directionalLight != null)
        {
            directionalLight.intensity = originalDirectionalIntensity;
            directionalLight.color = originalDirectionalColor;
        }

        if (hasCapturedCeilingOriginal && ceilingLight != null)
        {
            ceilingLight.intensity = originalCeilingIntensity;
            ceilingLight.color = originalCeilingColor;
        }

        hasCapturedOriginals = false;
        hasCapturedCeilingOriginal = false;
    }

    private void Update()
    {
        UpdateLighting();
    }

    private void OnValidate()
    {
        smoothingSeconds = Mathf.Max(0f, smoothingSeconds);
        RebuildKeyCache();
    }

    /// <summary>
    /// Fills in any reference left empty in the inspector. Returns false and warns - rather
    /// than throwing - when the scene has no clock or no directional light.
    /// </summary>
    private bool ResolveReferences()
    {
        if (timeService == null)
            timeService = FindAnyObjectByType<GameTimeService>(FindObjectsInactive.Include);

        if (timeService == null)
        {
            Debug.LogWarning(
                "RoomLightingController found no GameTimeService in the scene, so it stays " +
                "disabled and the room keeps its authored lighting.",
                this
            );
            return false;
        }

        if (directionalLight == null)
            directionalLight = FindDirectionalLight();

        if (directionalLight == null)
        {
            Debug.LogWarning(
                "RoomLightingController found no directional light in the scene, so it stays " +
                "disabled and the room keeps its authored lighting.",
                this
            );
            return false;
        }

        if (ceilingLight == null)
            ceilingLight = FindOwnCeilingLight();

        if (ceilingLight == null)
        {
            // Not fatal: ambient and the directional light still animate. Only the "the lamp
            // is being dimmed" read is missing, so warn and carry on.
            Debug.LogWarning(
                "RoomLightingController has no ceiling light, so only ambient and directional " +
                "intensity are driven. Run Tools > Cat Home > Build Room Lighting Controller to " +
                "create it.",
                this
            );
        }

        return true;
    }

    /// <summary>
    /// Looks for the ceiling lamp among this object's own children only. Searching the whole
    /// scene would risk grabbing the window system's spot light, which
    /// <see cref="WindowDayNightController"/> owns and drives on its own curve.
    /// </summary>
    private Light FindOwnCeilingLight()
    {
        Light[] candidates = GetComponentsInChildren<Light>(true);
        foreach (Light light in candidates)
        {
            if (light != null && light.type == LightType.Spot)
                return light;
        }

        return null;
    }

    /// <summary>
    /// Prefers the light the scene already nominates as its sun, then falls back to a
    /// directional light that can actually reach the room. Filtering on
    /// <see cref="LightType.Directional"/> keeps the window system's spot light - which
    /// WindowDayNightController owns - out of scope, and the culling mask check keeps the
    /// offscreen portrait rig's directional light out of it too.
    /// </summary>
    private static Light FindDirectionalLight()
    {
        Light sun = RenderSettings.sun;
        if (sun != null && sun.type == LightType.Directional)
            return sun;

        Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include);

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

    private void UpdateLighting()
    {
        // The key array can be emptied from the inspector mid-play, so re-check it every frame.
        if (timeService == null || directionalLight == null || sortedKeys == null || sortedKeys.Length == 0)
            return;

        float hour = Mathf.Repeat(timeService.CurrentHour, 24f);
        Evaluate(
            hour,
            out float targetAmbient,
            out float targetDirectional,
            out float targetCeiling,
            out Color targetDirectionalColor,
            out Color targetCeilingColor
        );
        float playerMultiplier = PlayerLightingPreference.Multiplier;
        targetAmbient *= playerMultiplier;
        targetDirectional *= playerMultiplier;
        targetCeiling *= playerMultiplier;

        double realtime = Time.realtimeSinceStartupAsDouble;
        float delta = hasAppliedOnce
            ? Mathf.Min((float)Math.Max(0d, realtime - lastUpdateRealtime), MaxSmoothingDelta)
            : 0f;
        lastUpdateRealtime = realtime;

        if (!hasAppliedOnce || smoothingSeconds <= 0f)
        {
            currentAmbient = targetAmbient;
            currentDirectional = targetDirectional;
            currentCeiling = targetCeiling;
            currentDirectionalColor = targetDirectionalColor;
            currentCeilingColor = targetCeilingColor;
            ambientVelocity = 0f;
            directionalVelocity = 0f;
            ceilingVelocity = 0f;
            directionalColorVelocity = Vector3.zero;
            ceilingColorVelocity = Vector3.zero;
            hasAppliedOnce = true;
        }
        else
        {
            currentAmbient = Mathf.SmoothDamp(
                currentAmbient, targetAmbient, ref ambientVelocity,
                smoothingSeconds, Mathf.Infinity, delta
            );
            currentDirectional = Mathf.SmoothDamp(
                currentDirectional, targetDirectional, ref directionalVelocity,
                smoothingSeconds, Mathf.Infinity, delta
            );
            currentCeiling = Mathf.SmoothDamp(
                currentCeiling, targetCeiling, ref ceilingVelocity,
                smoothingSeconds, Mathf.Infinity, delta
            );
            currentDirectionalColor = SmoothDampColor(
                currentDirectionalColor, targetDirectionalColor, ref directionalColorVelocity,
                smoothingSeconds, delta
            );
            currentCeilingColor = SmoothDampColor(
                currentCeilingColor, targetCeilingColor, ref ceilingColorVelocity,
                smoothingSeconds, delta
            );
        }

        if (Mathf.Abs(RenderSettings.ambientIntensity - currentAmbient) > ApplyEpsilon)
            RenderSettings.ambientIntensity = currentAmbient;

        if (Mathf.Abs(directionalLight.intensity - currentDirectional) > ApplyEpsilon)
            directionalLight.intensity = currentDirectional;

        if (MaxChannelDifference(directionalLight.color, currentDirectionalColor) > ApplyEpsilon)
            directionalLight.color = currentDirectionalColor;

        if (ceilingLight == null)
            return;

        if (Mathf.Abs(ceilingLight.intensity - currentCeiling) > ApplyEpsilon)
            ceilingLight.intensity = currentCeiling;

        if (MaxChannelDifference(ceilingLight.color, currentCeilingColor) > ApplyEpsilon)
            ceilingLight.color = currentCeilingColor;
    }

    /// <summary>
    /// Per channel <see cref="Mathf.SmoothDamp"/>, so a colour eases in on exactly the same
    /// curve and the same <see cref="smoothingSeconds"/> the intensities already use. Alpha is
    /// meaningless on a light, so it is pinned to 1.
    /// </summary>
    private static Color SmoothDampColor(
        Color current,
        Color target,
        ref Vector3 velocity,
        float smoothTime,
        float deltaTime)
    {
        return new Color(
            Mathf.SmoothDamp(current.r, target.r, ref velocity.x, smoothTime, Mathf.Infinity, deltaTime),
            Mathf.SmoothDamp(current.g, target.g, ref velocity.y, smoothTime, Mathf.Infinity, deltaTime),
            Mathf.SmoothDamp(current.b, target.b, ref velocity.z, smoothTime, Mathf.Infinity, deltaTime),
            1f
        );
    }

    private static float MaxChannelDifference(Color left, Color right)
    {
        return Mathf.Max(
            Mathf.Abs(left.r - right.r),
            Mathf.Abs(left.g - right.g),
            Mathf.Abs(left.b - right.b)
        );
    }

    /// <summary>
    /// Reads the curve at <paramref name="hour"/>. The key list wraps around midnight, so the
    /// 00:00 key anchors both the end of the evening ramp and the start of the morning one.
    /// </summary>
    private void Evaluate(
        float hour,
        out float ambient,
        out float directional,
        out float ceiling,
        out Color directionalColor,
        out Color ceilingColor)
    {
        ResolveNeighbours(sortedKeys, hour, out int previous, out int next, out float t);

        ambient = Mathf.Lerp(sortedKeys[previous].ambientIntensity, sortedKeys[next].ambientIntensity, t);
        directional = Mathf.Lerp(sortedKeys[previous].directionalIntensity, sortedKeys[next].directionalIntensity, t);
        ceiling = Mathf.Lerp(sortedKeys[previous].ceilingLightIntensity, sortedKeys[next].ceilingLightIntensity, t);
        // Serialized colour fields remain in LightingKey for migration compatibility, but old
        // blue/orange scene values must never tint the room at runtime.
        directionalColor = FixedDirectionalColor;
        ceilingColor = FixedCeilingColor;
    }

    /// <summary>
    /// Finds the two keys <paramref name="hour"/> sits between and the SmoothStep blend between
    /// them. The list wraps around midnight, so the 00:00 key anchors both the end of the evening
    /// ramp and the start of the morning one. <paramref name="keys"/> must be sorted by hour and
    /// non-empty.
    /// </summary>
    private static void ResolveNeighbours(
        LightingKey[] keys,
        float hour,
        out int previous,
        out int next,
        out float t)
    {
        int count = keys.Length;
        hour = Mathf.Repeat(hour, 24f);

        previous = -1;
        for (int i = 0; i < count; i++)
        {
            if (keys[i].hour <= hour)
                previous = i;
        }

        float previousHour;
        float nextHour;

        if (previous < 0)
        {
            // Before the first key of the day: come in from yesterday's last key.
            previous = count - 1;
            next = 0;
            previousHour = keys[previous].hour - 24f;
            nextHour = keys[next].hour;
        }
        else if (previous == count - 1)
        {
            // After the last key of the day: head towards tomorrow's first key.
            next = 0;
            previousHour = keys[previous].hour;
            nextHour = keys[next].hour + 24f;
        }
        else
        {
            next = previous + 1;
            previousHour = keys[previous].hour;
            nextHour = keys[next].hour;
        }

        float span = nextHour - previousHour;
        t = span <= Mathf.Epsilon
            ? 0f
            : Mathf.SmoothStep(0f, 1f, (hour - previousHour) / span);
    }

    /// <summary>
    /// Builds the sorted working copy. The serialized array is left in the order the user typed
    /// it so rows do not jump around while being edited in the inspector.
    /// </summary>
    private void RebuildKeyCache()
    {
        if (keys == null || keys.Length == 0)
        {
            sortedKeys = null;
            return;
        }

        LightingKey[] copy = new LightingKey[keys.Length];
        Array.Copy(keys, copy, keys.Length);

        for (int i = 0; i < copy.Length; i++)
        {
            copy[i].hour = Mathf.Repeat(copy[i].hour, 24f);
            copy[i].ambientIntensity = Mathf.Max(0f, copy[i].ambientIntensity);
            copy[i].directionalIntensity = Mathf.Max(0f, copy[i].directionalIntensity);
            copy[i].ceilingLightIntensity = Mathf.Max(0f, copy[i].ceilingLightIntensity);

            // Keep the old serialized fields intact, while making the runtime cache migration
            // safe for scenes that still contain the former blue/orange ramp.
            copy[i].directionalColor = FixedDirectionalColor;
            copy[i].ceilingColor = FixedCeilingColor;
        }

        Array.Sort(copy, (a, b) => a.hour.CompareTo(b.hour));
        sortedKeys = copy;
    }
}
