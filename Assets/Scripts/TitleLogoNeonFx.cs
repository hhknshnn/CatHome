using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A restrained, reduced-motion-aware neon breath for the title emblem. The
/// effect only animates presentation layers; it never owns layout or input.
/// </summary>
[DisallowMultipleComponent]
public sealed class TitleLogoNeonFx : MonoBehaviour
{
    [SerializeField] private CanvasGroup auraGroup;
    [SerializeField] private RectTransform pulseRoot;
    [SerializeField] private Graphic logoGraphic;
    [SerializeField] private RectTransform sparkleRoot;
    [SerializeField, Min(.2f)] private float speed = 1.15f;
    [SerializeField, Range(0f, 1f)] private float minimumAura = .52f;
    [SerializeField, Range(0f, 1f)] private float maximumAura = .92f;
    [SerializeField, Range(0f, .04f)] private float scaleAmount = .012f;
    [SerializeField, Range(0f, 8f)] private float sparkleSwayDegrees = 2.4f;

    private Vector3 baseScale = Vector3.one;
    private Quaternion baseSparkleRotation = Quaternion.identity;
    private Color baseLogoColor = Color.white;
    private PremiumAmbientSparkle[] sparkleMotions;
    private bool lastReducedMotion;
    private bool reducedMotionInitialized;

    private void Awake() => CaptureBasePose();

    private void OnEnable()
    {
        CaptureBasePose();
        ApplyReducedMotionPolicy(true);
        ApplyStaticPose();
    }

    private void Update()
    {
        bool reducedMotion = CatRunnerProgressService.ReducedMotion;
        ApplyReducedMotionPolicy(false);
        if (reducedMotion)
        {
            ApplyStaticPose();
            return;
        }

        float time = Time.unscaledTime * speed;
        float wave = (Mathf.Sin(time) + 1f) * .5f;
        float secondary = Mathf.Sin(time * .43f + .8f);

        if (auraGroup != null)
            auraGroup.alpha = Mathf.Lerp(minimumAura, maximumAura, wave);
        if (pulseRoot != null)
            pulseRoot.localScale = baseScale * (1f + (wave - .5f) * 2f * scaleAmount);
        if (logoGraphic != null)
            logoGraphic.color = Color.Lerp(baseLogoColor, Color.white, .08f + wave * .14f);
        if (sparkleRoot != null)
        {
            sparkleRoot.localRotation = baseSparkleRotation *
                Quaternion.Euler(0f, 0f, secondary * sparkleSwayDegrees);
        }
    }

    private void OnDisable() => RestoreBasePose();

    private void CaptureBasePose()
    {
        if (pulseRoot != null)
            baseScale = pulseRoot.localScale;
        if (sparkleRoot != null)
            baseSparkleRotation = sparkleRoot.localRotation;
        if (logoGraphic != null)
            baseLogoColor = logoGraphic.color;
        sparkleMotions = sparkleRoot != null
            ? sparkleRoot.GetComponentsInChildren<PremiumAmbientSparkle>(true)
            : null;
    }

    private void ApplyReducedMotionPolicy(bool force)
    {
        bool reducedMotion = CatRunnerProgressService.ReducedMotion;
        if (!force && reducedMotionInitialized && lastReducedMotion == reducedMotion)
            return;

        if (sparkleMotions == null && sparkleRoot != null)
            sparkleMotions = sparkleRoot.GetComponentsInChildren<PremiumAmbientSparkle>(true);
        if (sparkleMotions != null)
        {
            for (int i = 0; i < sparkleMotions.Length; i++)
            {
                if (sparkleMotions[i] != null)
                    sparkleMotions[i].SetReducedMotion(reducedMotion);
            }
        }

        lastReducedMotion = reducedMotion;
        reducedMotionInitialized = true;
    }

    private void ApplyStaticPose()
    {
        if (auraGroup != null)
            auraGroup.alpha = (minimumAura + maximumAura) * .5f;
        RestoreBasePose();
    }

    private void RestoreBasePose()
    {
        if (pulseRoot != null)
            pulseRoot.localScale = baseScale;
        if (sparkleRoot != null)
            sparkleRoot.localRotation = baseSparkleRotation;
        if (logoGraphic != null)
            logoGraphic.color = baseLogoColor;
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        CanvasGroup authoredAura,
        RectTransform authoredPulseRoot,
        Graphic authoredLogo,
        RectTransform authoredSparkles)
    {
        auraGroup = authoredAura;
        pulseRoot = authoredPulseRoot;
        logoGraphic = authoredLogo;
        sparkleRoot = authoredSparkles;
        CaptureBasePose();
    }
#endif
}
