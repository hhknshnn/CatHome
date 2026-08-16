using System;
using UnityEngine;

[ExecuteAlways]
public class WindowDayNightController : MonoBehaviour
{
    private enum TimePeriod
    {
        Dawn,
        Day,
        Sunset,
        Night
    }

    /// <summary>
    /// One inspector-editable point on the sun beam's daily curve. The beam always starts at the
    /// window opening and lands on the floor plane; the key decides how far into the room it
    /// reaches, how far it has swung sideways, how wide it is, how strong it reads and what
    /// colour the light pooling on the floor is.
    /// </summary>
    [Serializable]
    public struct SunBeamKey
    {
        [Range(0f, 24f)] public float hour;

        [Tooltip("0 = invisible, 1 = full Sun Beam Max Alpha.")]
        [Range(0f, 1f)] public float intensity;

        [Tooltip("Multiplies the builder's beam width.")]
        [Min(0.05f)] public float width;

        [Tooltip("Multiplies the builder's floor distance. 1 = the base landing point (carpet).")]
        [Min(0.05f)] public float reach;

        [Tooltip("Horizontal swing of the landing point, in degrees around the room's up axis.")]
        [Range(-60f, 60f)] public float yaw;

        [Tooltip("Beam tint at this hour. Alpha is ignored: the beam's opacity is intensity " +
                 "multiplied by Sun Beam Max Alpha.")]
        [ColorUsage(false)] public Color beamColor;
    }

    /// <summary>
    /// Shipped beam curve, and the upgrade source for keys serialized before <see
    /// cref="SunBeamKey.beamColor"/> existed. Colours are the authored sRGB hex codes
    /// #FFD5A3 / #FFE8C7 / #FFF4DC / #FFD0A8; the 20:00 key sits at the end of the visible window, so
    /// it repeats the sunset tint and the beam simply fades out warm instead of drifting cool.
    /// </summary>
    private static readonly Color SunsetBeamColor =
        new Color(1f, 0.8156863f, 0.6588235f, 1f); // #FFD0A8

    private static readonly Color LegacySunsetBeamColor =
        new Color(1f, 0.7254902f, 0.4705882f, 1f); // #FFB978

    private static SunBeamKey[] CreateDefaultSunBeamKeys()
    {
        return new[]
        {
            new SunBeamKey
            {
                hour = 6f, intensity = 0.16f, width = 0.52f, reach = 0.34f, yaw = -18f,
                beamColor = new Color(1f, 0.8352941f, 0.6392157f, 1f)
            },
            new SunBeamKey
            {
                hour = 9f, intensity = 0.58f, width = 0.80f, reach = 0.68f, yaw = -10f,
                beamColor = new Color(1f, 0.9098039f, 0.7803922f, 1f)
            },
            new SunBeamKey
            {
                hour = 13f, intensity = 1f, width = 1.15f, reach = 1f, yaw = 0f,
                beamColor = new Color(1f, 0.9568627f, 0.8627451f, 1f)
            },
            new SunBeamKey
            {
                hour = 18f, intensity = 0.24f, width = 0.60f, reach = 0.42f, yaw = 14f,
                beamColor = SunsetBeamColor
            },
            new SunBeamKey
            {
                hour = 20f, intensity = 0f, width = 0.50f, reach = 0.34f, yaw = 18f,
                beamColor = SunsetBeamColor
            }
        };
    }

    /// <summary>
    /// Samples the default beam tint with the same wrap-around SmoothStep walk the runtime curve
    /// uses, so a key added at an hour the defaults do not cover still upgrades to a sensible
    /// colour instead of an unlit black beam.
    /// </summary>
    public static Color SampleDefaultBeamColor(float hour)
    {
        ResolveNeighbours(DefaultSunBeamKeys, hour, out int previous, out int next, out float t);
        return Color.Lerp(
            DefaultSunBeamKeys[previous].beamColor,
            DefaultSunBeamKeys[next].beamColor,
            t
        );
    }

    /// <summary>Pure black is the shape old serialized data takes, not an authored colour.</summary>
    public static bool IsUnsetColor(Color color)
    {
        return color.maxColorComponent < 0.004f;
    }

    private static readonly SunBeamKey[] DefaultSunBeamKeys = CreateDefaultSunBeamKeys();

    private readonly struct VisualState
    {
        public readonly Color SkyColor;
        public readonly Color HorizonColor;
        public readonly float StarStrength;
        public readonly Color LightColor;
        public readonly float LightIntensity;
        public readonly Texture2D Texture;

        public VisualState(
            Color skyColor,
            Color horizonColor,
            float starStrength,
            Color lightColor,
            float lightIntensity,
            Texture2D texture)
        {
            SkyColor = skyColor;
            HorizonColor = horizonColor;
            StarStrength = starStrength;
            LightColor = lightColor;
            LightIntensity = lightIntensity;
            Texture = texture;
        }
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int HorizonColorId = Shader.PropertyToID("_HorizonColor");
    private static readonly int StarStrengthId = Shader.PropertyToID("_StarStrength");
    private static readonly int MoonStrengthId = Shader.PropertyToID("_MoonStrength");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    [Header("References")]
    [SerializeField] private GameTimeService timeService;
    [SerializeField] private Renderer skyRenderer;
    [SerializeField] private Renderer glassRenderer;
    [SerializeField] private MeshRenderer sunBeamRenderer;
    [SerializeField] private Light windowLight;

    [Header("Optional Sky Textures")]
    [SerializeField] private Texture2D dayTexture;
    [SerializeField] private Texture2D dawnTexture;
    [SerializeField] private Texture2D sunsetTexture;
    [SerializeField] private Texture2D nightTexture;

    [Header("Fallback Sky Colors")]
    [SerializeField] private Color dawnSkyColor = new Color(0.48f, 0.68f, 0.84f, 1f);
    [SerializeField] private Color daySkyColor = new Color(0.22f, 0.62f, 1f, 1f);
    [SerializeField] private Color sunsetSkyColor = new Color(0.91f, 0.49f, 0.39f, 1f);
    [SerializeField] private Color nightSkyColor = new Color(0.06f, 0.11f, 0.27f, 1f);

    [Header("Fallback Horizon Colors")]
    [SerializeField] private Color dawnHorizonColor = new Color(0.96f, 0.77f, 0.62f, 1f);
    [SerializeField] private Color dayHorizonColor = new Color(0.72f, 0.91f, 1f, 1f);
    [SerializeField] private Color sunsetHorizonColor = new Color(1f, 0.72f, 0.62f, 1f);
    [SerializeField] private Color nightHorizonColor = new Color(0.15f, 0.22f, 0.39f, 1f);

    [Header("Window Light")]
    [SerializeField] private Color dawnLightColor = new Color(1f, 0.84f, 0.68f, 1f);
    [SerializeField] private Color dayLightColor = new Color(1f, 0.97f, 0.91f, 1f);
    [SerializeField] private Color sunsetLightColor = new Color(1f, 0.69f, 0.44f, 1f);
    [SerializeField] private Color nightLightColor = new Color(0.56f, 0.66f, 0.85f, 1f);
    [SerializeField, Min(0f)] private float dawnLightIntensity = 1.70f;
    [SerializeField, Min(0f)] private float dayLightIntensity = 3.15f;
    [SerializeField, Min(0f)] private float sunsetLightIntensity = 1.60f;
    [SerializeField, Min(0f)] private float nightLightIntensity = 0.26f;

    [Header("Transition")]
    [SerializeField, Range(0.05f, 1f)] private float transitionDurationHours = 1f;
    [SerializeField] private Color glassTint = new Color(0.86f, 0.92f, 1f, 0.1f);
    [SerializeField, Range(0f, 0.5f)] private float glassSkyInfluence = 0.18f;

    [Header("Sun Beam")]
    [SerializeField, Range(0.02f, 0.25f)] private float sunBeamMaxAlpha = 0.12f;
    [SerializeField, Range(0f, 24f)] private float sunBeamStartHour = 6f;
    [SerializeField, Range(0f, 24f)] private float sunBeamEndHour = 20f;
    [SerializeField, Min(0f)] private float sunBeamSmoothTime = 0.6f;
    [SerializeField] private SunBeamKey[] sunBeamKeys = CreateDefaultSunBeamKeys();

    [Header("Sun Beam Base Pose")]
    [Tooltip("Written by the window builder. Every key is expressed relative to this pose.")]
    [SerializeField] private Vector3 sunBeamBaseLocalPosition;
    [SerializeField] private Vector3 sunBeamBaseVector;
    [SerializeField, Min(0.0001f)] private float sunBeamBaseWidth = 1f;

    private const float MaxSmoothingDelta = 0.25f;

    private MaterialPropertyBlock skyPropertyBlock;
    private MaterialPropertyBlock glassPropertyBlock;
    private MaterialPropertyBlock sunBeamPropertyBlock;
    private float lastAppliedHour = -1f;
    private double nextTimedRefresh;

    private SunBeamKey[] sortedSunBeamKeys;
    private float beamIntensity;
    private float beamWidth;
    private float beamReach;
    private float beamYaw;
    private Color beamTint;
    private float beamIntensityVelocity;
    private float beamWidthVelocity;
    private float beamReachVelocity;
    private float beamYawVelocity;
    private Vector3 beamTintVelocity;
    private bool hasBeamState;
    private double lastBeamRealtime;
    private float currentBrightnessMultiplier = 1f;
    private float brightnessMultiplierVelocity;

#if UNITY_EDITOR
    private bool editorVisualRefreshScheduled;
#endif

    private void OnEnable()
    {
        RebuildSunBeamKeyCache();
        currentBrightnessMultiplier = PlayerLightingPreference.Multiplier;
        brightnessMultiplierVelocity = 0f;
        ApplyVisuals(true);
    }

    private void Update()
    {
        if (timeService == null)
            return;

        float hour = Mathf.Repeat(timeService.CurrentHour, 24f);
        double realtime = Time.realtimeSinceStartupAsDouble;
        currentBrightnessMultiplier = Mathf.SmoothDamp(
            currentBrightnessMultiplier,
            PlayerLightingPreference.Multiplier,
            ref brightnessMultiplierVelocity,
            0.5f,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        if (realtime >= nextTimedRefresh ||
            Mathf.Abs(hour - lastAppliedHour) >= 0.001f ||
            Mathf.Abs(currentBrightnessMultiplier - PlayerLightingPreference.Multiplier) >= 0.0005f)
        {
            ApplyPanelVisuals(hour);
            nextTimedRefresh = realtime + 0.25d;
        }

        // The beam eases towards its target with SmoothDamp, so it needs a frame accurate delta
        // even while the panels sit on their quarter second refresh.
        float delta = hasBeamState
            ? Mathf.Min((float)Math.Max(0d, realtime - lastBeamRealtime), MaxSmoothingDelta)
            : 0f;
        lastBeamRealtime = realtime;
        UpdateSunBeam(hour, delta, false);
    }

    private void OnValidate()
    {
        transitionDurationHours = Mathf.Max(0.05f, transitionDurationHours);
        sunBeamSmoothTime = Mathf.Max(0f, sunBeamSmoothTime);
        sunBeamBaseWidth = Mathf.Max(0.0001f, sunBeamBaseWidth);
        RebuildSunBeamKeyCache();

#if UNITY_EDITOR
        ScheduleEditorVisualRefresh();
#endif
    }

#if UNITY_EDITOR
    private void ScheduleEditorVisualRefresh()
    {
        if (editorVisualRefreshScheduled)
            return;

        editorVisualRefreshScheduled = true;
        UnityEditor.EditorApplication.delayCall += ApplyDelayedEditorVisualRefresh;
    }

    private void ApplyDelayedEditorVisualRefresh()
    {
        if (this == null)
            return;

        editorVisualRefreshScheduled = false;
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        ApplyVisuals(true);
    }
#endif

    public void RefreshVisuals()
    {
        ApplyVisuals(true);
    }

    private void ApplyVisuals(bool force)
    {
        if (timeService == null)
            return;

        float hour = Mathf.Repeat(timeService.CurrentHour, 24f);
        if (!force && Mathf.Abs(hour - lastAppliedHour) < 0.0001f)
            return;

        ApplyPanelVisuals(hour);
        lastBeamRealtime = Time.realtimeSinceStartupAsDouble;
        UpdateSunBeam(hour, 0f, force);
    }

    private void ApplyPanelVisuals(float hour)
    {
        GetBlendedState(hour, out VisualState state, out Texture2D texture);

        if (skyRenderer != null)
        {
            skyPropertyBlock ??= new MaterialPropertyBlock();
            skyPropertyBlock.Clear();
            skyPropertyBlock.SetColor(BaseColorId, state.SkyColor);
            skyPropertyBlock.SetColor(ColorId, state.SkyColor);
            skyPropertyBlock.SetColor(HorizonColorId, state.HorizonColor);
            skyPropertyBlock.SetFloat(StarStrengthId, state.StarStrength);
            skyPropertyBlock.SetFloat(
                MoonStrengthId,
                Mathf.Clamp01(state.StarStrength / 0.32f)
            );
            if (texture != null)
            {
                skyPropertyBlock.SetTexture(BaseMapId, texture);
                skyPropertyBlock.SetTexture(MainTexId, texture);
            }
            skyRenderer.SetPropertyBlock(skyPropertyBlock);
        }

        if (glassRenderer != null)
        {
            glassPropertyBlock ??= new MaterialPropertyBlock();
            glassPropertyBlock.Clear();
            Color glassColor = Color.Lerp(
                new Color(glassTint.r, glassTint.g, glassTint.b, 1f),
                new Color(state.SkyColor.r, state.SkyColor.g, state.SkyColor.b, 1f),
                glassSkyInfluence
            );
            glassColor.a = glassTint.a;
            glassPropertyBlock.SetColor(BaseColorId, glassColor);
            glassPropertyBlock.SetColor(ColorId, glassColor);
            glassRenderer.SetPropertyBlock(glassPropertyBlock);
        }

        if (windowLight != null)
        {
            windowLight.color = state.LightColor;
            windowLight.intensity = state.LightIntensity * currentBrightnessMultiplier;
        }

        lastAppliedHour = hour;
    }

    /// <summary>
    /// Eases the beam towards the pose and strength the hour asks for. Nothing here allocates or
    /// touches lights: it is a transform write plus one MaterialPropertyBlock on a 12 vertex mesh.
    /// </summary>
    private void UpdateSunBeam(float hour, float deltaTime, bool snap)
    {
        if (sunBeamRenderer == null)
            return;

        EvaluateSunBeam(
            hour,
            out float targetIntensity,
            out float targetWidth,
            out float targetReach,
            out float targetYaw,
            out Color targetTint
        );

        // Scrubbing the test hour in edit mode should land on the authored pose immediately
        // instead of creeping towards it between inspector repaints.
        if (snap || !hasBeamState || sunBeamSmoothTime <= 0f || deltaTime <= 0f || !Application.isPlaying)
        {
            beamIntensity = targetIntensity;
            beamWidth = targetWidth;
            beamReach = targetReach;
            beamYaw = targetYaw;
            beamTint = targetTint;
            beamIntensityVelocity = 0f;
            beamWidthVelocity = 0f;
            beamReachVelocity = 0f;
            beamYawVelocity = 0f;
            beamTintVelocity = Vector3.zero;
            hasBeamState = true;
        }
        else
        {
            beamIntensity = Mathf.SmoothDamp(
                beamIntensity, targetIntensity, ref beamIntensityVelocity,
                sunBeamSmoothTime, Mathf.Infinity, deltaTime
            );
            beamWidth = Mathf.SmoothDamp(
                beamWidth, targetWidth, ref beamWidthVelocity,
                sunBeamSmoothTime, Mathf.Infinity, deltaTime
            );
            beamReach = Mathf.SmoothDamp(
                beamReach, targetReach, ref beamReachVelocity,
                sunBeamSmoothTime, Mathf.Infinity, deltaTime
            );
            beamYaw = Mathf.SmoothDampAngle(
                beamYaw, targetYaw, ref beamYawVelocity,
                sunBeamSmoothTime, Mathf.Infinity, deltaTime
            );
            beamTint = new Color(
                Mathf.SmoothDamp(beamTint.r, targetTint.r, ref beamTintVelocity.x,
                    sunBeamSmoothTime, Mathf.Infinity, deltaTime),
                Mathf.SmoothDamp(beamTint.g, targetTint.g, ref beamTintVelocity.y,
                    sunBeamSmoothTime, Mathf.Infinity, deltaTime),
                Mathf.SmoothDamp(beamTint.b, targetTint.b, ref beamTintVelocity.z,
                    sunBeamSmoothTime, Mathf.Infinity, deltaTime),
                1f
            );
        }

        bool shouldRender = beamIntensity > 0.002f;
        if (sunBeamRenderer.enabled != shouldRender)
            sunBeamRenderer.enabled = shouldRender;

        if (shouldRender)
        {
            ApplySunBeamPose(beamWidth, beamReach, beamYaw);

            // Only the alpha carries the beam's strength. The hour drives the tint, and
            // Sun Beam Max Alpha stays the single owner of how opaque the ribbon ever gets.
            Color beamColor = beamTint;
            beamColor.a = sunBeamMaxAlpha * beamIntensity;

            sunBeamPropertyBlock ??= new MaterialPropertyBlock();
            sunBeamPropertyBlock.Clear();
            sunBeamPropertyBlock.SetColor(BaseColorId, beamColor);
            sunBeamPropertyBlock.SetColor(ColorId, beamColor);
            sunBeamRenderer.SetPropertyBlock(sunBeamPropertyBlock);
        }
    }

    /// <summary>
    /// Rebuilds the beam transform from its base pose. The landing point is kept on the floor
    /// plane the builder aimed at, so shortening the reach slides the pool of light back towards
    /// the window instead of leaving the ribbon floating in mid air.
    /// </summary>
    private void ApplySunBeamPose(float width, float reach, float yaw)
    {
        if (!EnsureSunBeamBasePose())
            return;

        Vector3 flat = new Vector3(sunBeamBaseVector.x, 0f, sunBeamBaseVector.z);
        float flatDistance = flat.magnitude;
        Vector3 beamVector;

        if (flatDistance < 0.0001f)
        {
            // Degenerate base pose (beam points straight down); reach can only scale it.
            beamVector = sunBeamBaseVector * reach;
        }
        else
        {
            Vector3 flatDirection = Quaternion.AngleAxis(yaw, Vector3.up) * (flat / flatDistance);
            beamVector = flatDirection * (flatDistance * reach) + Vector3.up * sunBeamBaseVector.y;
        }

        float length = beamVector.magnitude;
        if (length < 0.0001f)
            return;

        Transform beam = sunBeamRenderer.transform;
        Quaternion rotation = Quaternion.LookRotation(beamVector / length, Vector3.up);
        Vector3 scale = new Vector3(sunBeamBaseWidth * width, 1f, length);

        // Only write when something actually moved; ExecuteAlways would otherwise dirty the scene
        // on every editor repaint.
        if (Quaternion.Angle(beam.localRotation, rotation) > 0.01f)
            beam.localRotation = rotation;
        if ((beam.localScale - scale).sqrMagnitude > 0.0000001f)
            beam.localScale = scale;
        if ((beam.localPosition - sunBeamBaseLocalPosition).sqrMagnitude > 0.0000001f)
            beam.localPosition = sunBeamBaseLocalPosition;
    }

    /// <summary>
    /// The builder writes the base pose, but a scene authored before this system existed still has
    /// a hand placed beam. Bake the base from the untouched transform the first time we run.
    /// </summary>
    private bool EnsureSunBeamBasePose()
    {
        if (sunBeamBaseVector.sqrMagnitude > 0.0000001f)
            return true;

        if (sunBeamRenderer == null)
            return false;

        Transform beam = sunBeamRenderer.transform;
        Vector3 baseVector = (beam.localRotation * Vector3.forward) * beam.localScale.z;
        if (baseVector.sqrMagnitude < 0.0000001f)
            return false;

        sunBeamBaseLocalPosition = beam.localPosition;
        sunBeamBaseVector = baseVector;
        sunBeamBaseWidth = Mathf.Max(0.0001f, beam.localScale.x);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        return true;
    }

    /// <summary>
    /// Samples the beam curve. Keys wrap around midnight the same way RoomLightingController's do,
    /// and the start/end hours hard gate the beam so it is fully off between 21:00 and 05:59.
    /// </summary>
    private void EvaluateSunBeam(
        float hour,
        out float intensity,
        out float width,
        out float reach,
        out float yaw,
        out Color tint)
    {
        intensity = 0f;
        width = 1f;
        reach = 1f;
        yaw = 0f;
        tint = Color.white;

        SunBeamKey[] keys = sortedSunBeamKeys;
        if (keys == null || keys.Length == 0)
            return;

        hour = Mathf.Repeat(hour, 24f);
        ResolveNeighbours(keys, hour, out int previous, out int next, out float t);

        intensity = Mathf.Lerp(keys[previous].intensity, keys[next].intensity, t);
        width = Mathf.Max(0.05f, Mathf.Lerp(keys[previous].width, keys[next].width, t));
        reach = Mathf.Max(0.05f, Mathf.Lerp(keys[previous].reach, keys[next].reach, t));
        yaw = Mathf.LerpAngle(keys[previous].yaw, keys[next].yaw, t);
        tint = Color.Lerp(keys[previous].beamColor, keys[next].beamColor, t);

        if (!IsWithinSunBeamWindow(hour))
            intensity = 0f;
    }

    /// <summary>
    /// Finds the two keys <paramref name="hour"/> sits between and the SmoothStep blend between
    /// them. Keys wrap around midnight the same way RoomLightingController's do.
    /// <paramref name="keys"/> must be sorted by hour and non-empty.
    /// </summary>
    private static void ResolveNeighbours(
        SunBeamKey[] keys,
        float hour,
        out int previous,
        out int next,
        out float t)
    {
        hour = Mathf.Repeat(hour, 24f);
        int count = keys.Length;

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
            previous = count - 1;
            next = 0;
            previousHour = keys[previous].hour - 24f;
            nextHour = keys[next].hour;
        }
        else if (previous == count - 1)
        {
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

    private bool IsWithinSunBeamWindow(float hour)
    {
        float start = Mathf.Repeat(sunBeamStartHour, 24f);
        float end = Mathf.Repeat(sunBeamEndHour, 24f);
        if (Mathf.Approximately(start, end))
            return true;

        return start < end
            ? hour >= start && hour < end
            : hour >= start || hour < end;
    }

    /// <summary>
    /// Sorted working copy, so the serialized array can stay in whatever order it is typed in.
    /// </summary>
    private void RebuildSunBeamKeyCache()
    {
        if (sunBeamKeys == null || sunBeamKeys.Length == 0)
        {
            sortedSunBeamKeys = null;
            return;
        }

        if (sortedSunBeamKeys == null || sortedSunBeamKeys.Length != sunBeamKeys.Length)
            sortedSunBeamKeys = new SunBeamKey[sunBeamKeys.Length];

        for (int i = 0; i < sunBeamKeys.Length; i++)
        {
            SunBeamKey key = sunBeamKeys[i];
            key.hour = Mathf.Repeat(key.hour, 24f);
            key.intensity = Mathf.Clamp01(key.intensity);
            key.width = Mathf.Max(0.05f, key.width);
            key.reach = Mathf.Max(0.05f, key.reach);

            // Keys serialized before beamColor existed read back as black, which would render a
            // dark smudge on the floor. Upgrade them in the working copy so the scene is correct
            // even before the builder is re-run; the serialized array stays as typed.
            if (IsUnsetColor(key.beamColor))
                key.beamColor = SampleDefaultBeamColor(key.hour);
            else if (MaxColorDifference(key.beamColor, LegacySunsetBeamColor) < 0.002f)
                key.beamColor = SunsetBeamColor;
            key.beamColor.a = 1f;

            sortedSunBeamKeys[i] = key;
        }

        Array.Sort(sortedSunBeamKeys, (left, right) => left.hour.CompareTo(right.hour));
    }

    private static float MaxColorDifference(Color left, Color right)
    {
        return Mathf.Max(
            Mathf.Abs(left.r - right.r),
            Mathf.Abs(left.g - right.g),
            Mathf.Abs(left.b - right.b)
        );
    }

    private void GetBlendedState(float hour, out VisualState blended, out Texture2D texture)
    {
        TimePeriod currentPeriod = GetPeriod(hour);
        TimePeriod nextPeriod = GetNextPeriod(currentPeriod);
        VisualState current = GetState(currentPeriod);
        VisualState next = GetState(nextPeriod);

        float boundary = GetEndHour(currentPeriod);
        float hoursUntilBoundary = Mathf.Repeat(boundary - hour, 24f);
        float blend = hoursUntilBoundary <= transitionDurationHours
            ? 1f - hoursUntilBoundary / transitionDurationHours
            : 0f;
        blend = Mathf.SmoothStep(0f, 1f, blend);

        blended = new VisualState(
            Color.Lerp(current.SkyColor, next.SkyColor, blend),
            Color.Lerp(current.HorizonColor, next.HorizonColor, blend),
            Mathf.Lerp(current.StarStrength, next.StarStrength, blend),
            Color.Lerp(current.LightColor, next.LightColor, blend),
            Mathf.Lerp(current.LightIntensity, next.LightIntensity, blend),
            null
        );

        texture = blend >= 0.5f ? next.Texture : current.Texture;
    }

    private TimePeriod GetPeriod(float hour)
    {
        if (hour >= 6f && hour < 10f)
            return TimePeriod.Dawn;
        if (hour >= 10f && hour < 16f)
            return TimePeriod.Day;
        if (hour >= 16f && hour < 20f)
            return TimePeriod.Sunset;
        return TimePeriod.Night;
    }

    private VisualState GetState(TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Dawn => new VisualState(
                dawnSkyColor, dawnHorizonColor, 0f,
                dawnLightColor, dawnLightIntensity, dawnTexture),
            TimePeriod.Day => new VisualState(
                daySkyColor, dayHorizonColor, 0f,
                dayLightColor, dayLightIntensity, dayTexture),
            TimePeriod.Sunset => new VisualState(
                sunsetSkyColor, sunsetHorizonColor, 0f,
                sunsetLightColor, sunsetLightIntensity, sunsetTexture),
            _ => new VisualState(
                nightSkyColor, nightHorizonColor, 0.32f,
                nightLightColor, nightLightIntensity, nightTexture)
        };
    }

    private static TimePeriod GetNextPeriod(TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Dawn => TimePeriod.Day,
            TimePeriod.Day => TimePeriod.Sunset,
            TimePeriod.Sunset => TimePeriod.Night,
            _ => TimePeriod.Dawn
        };
    }

    private static float GetEndHour(TimePeriod period)
    {
        return period switch
        {
            TimePeriod.Dawn => 10f,
            TimePeriod.Day => 16f,
            TimePeriod.Sunset => 20f,
            _ => 6f
        };
    }
}
