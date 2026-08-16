using UnityEngine;

/// <summary>
/// Gentle, allocation-free motion for the small wall sparkles authored by the premium
/// room presentation builder. It never moves gameplay geometry or placement surfaces.
/// </summary>
[DisallowMultipleComponent]
public sealed class PremiumWorldAmbientFx : MonoBehaviour
{
    [SerializeField] private Transform[] sparkles = new Transform[0];
    [SerializeField, Range(0f, 0.2f)] private float pulseAmount = 0.09f;
    [SerializeField, Range(0f, 24f)] private float rotationDegrees = 8f;
    [SerializeField, Range(0f, 0.08f)] private float floatDistance = 0.025f;
    [SerializeField, Min(0.1f)] private float speed = 1.05f;
    [SerializeField] private bool reducedMotion;

    private Vector3[] basePositions = new Vector3[0];
    private Vector3[] baseScales = new Vector3[0];
    private Quaternion[] baseRotations = new Quaternion[0];

    public int SparkleCount => sparkles != null ? sparkles.Length : 0;
    public bool ReducedMotion => reducedMotion;

    private void Awake()
    {
        CapturePose();
    }

    private void OnEnable()
    {
        CapturePose();
    }

    private void Update()
    {
        if (reducedMotion || sparkles == null)
            return;

        float time = Time.unscaledTime * speed;
        for (int i = 0; i < sparkles.Length; i++)
        {
            Transform sparkle = sparkles[i];
            if (sparkle == null || i >= basePositions.Length)
                continue;

            float phase = i * 1.731f;
            float primary = Mathf.Sin(time + phase);
            float secondary = Mathf.Sin(time * 0.71f + phase * 1.37f);
            sparkle.localScale = baseScales[i] * (1f + primary * pulseAmount);
            sparkle.localRotation = baseRotations[i] *
                                    Quaternion.Euler(0f, 0f, secondary * rotationDegrees);
            sparkle.localPosition = basePositions[i] + Vector3.up * secondary * floatDistance;
        }
    }

    private void OnDisable()
    {
        RestorePose();
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
        if (value)
            RestorePose();
        else
            CapturePose();
    }

    private void CapturePose()
    {
        int count = sparkles != null ? sparkles.Length : 0;
        basePositions = new Vector3[count];
        baseScales = new Vector3[count];
        baseRotations = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            Transform sparkle = sparkles[i];
            if (sparkle == null)
                continue;
            basePositions[i] = sparkle.localPosition;
            baseScales[i] = sparkle.localScale;
            baseRotations[i] = sparkle.localRotation;
        }
    }

    private void RestorePose()
    {
        if (sparkles == null)
            return;
        for (int i = 0; i < sparkles.Length; i++)
        {
            Transform sparkle = sparkles[i];
            if (sparkle == null || i >= basePositions.Length)
                continue;
            sparkle.localPosition = basePositions[i];
            sparkle.localScale = baseScales[i];
            sparkle.localRotation = baseRotations[i];
        }
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        Transform[] authoredSparkles,
        float authoredPulseAmount,
        float authoredRotationDegrees,
        float authoredFloatDistance,
        float authoredSpeed)
    {
        sparkles = authoredSparkles ?? new Transform[0];
        pulseAmount = Mathf.Clamp(authoredPulseAmount, 0f, 0.2f);
        rotationDegrees = Mathf.Clamp(authoredRotationDegrees, 0f, 24f);
        floatDistance = Mathf.Clamp(authoredFloatDistance, 0f, 0.08f);
        speed = Mathf.Max(0.1f, authoredSpeed);
        CapturePose();
    }
#endif
}
