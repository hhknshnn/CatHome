using UnityEngine;

/// <summary>Unsynchronised, allocation-free sparkle motion for premium UI ornaments.</summary>
[DisallowMultipleComponent]
public sealed class PremiumAmbientSparkle : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField, Range(0f, 0.18f)] private float scaleAmount = 0.1f;
    [SerializeField, Range(0f, 20f)] private float rotationDegrees = 8f;
    [SerializeField, Range(0f, 4f)] private float floatDistance = 1.5f;
    [SerializeField, Min(0.2f)] private float speed = 1.8f;

    private Vector3 baseScale;
    private Quaternion baseRotation;
    private Vector2 basePosition;
    private float phase;
    private bool animatePosition;
    private bool hasScaleMotion;
    private bool hasRotationMotion;
    private bool reducedMotion;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;
        Capture();
        RefreshMotionFlags();
        phase = (EntityId.ToULong(GetEntityId()) % 631UL) / 631f * Mathf.PI * 2f;
    }

    private void OnEnable()
    {
        if (target == null)
            target = transform as RectTransform;
        Capture();
        RefreshMotionFlags();
    }

    private void Update()
    {
        if (target == null)
            return;
        if (reducedMotion ||
            (!hasScaleMotion && !hasRotationMotion && !animatePosition))
            return;

        float wave = Mathf.Sin(Time.unscaledTime * speed + phase);
        float secondary = Mathf.Sin(Time.unscaledTime * speed * 0.63f + phase * 1.7f);
        if (hasScaleMotion)
            target.localScale = baseScale * (1f + wave * scaleAmount);
        if (hasRotationMotion)
            target.localRotation = baseRotation * Quaternion.Euler(0f, 0f, secondary * rotationDegrees);
        if (animatePosition)
            target.anchoredPosition = basePosition + Vector2.up * secondary * floatDistance;
    }

    private void Capture()
    {
        if (target == null)
            return;
        baseScale = target.localScale;
        baseRotation = target.localRotation;
        basePosition = target.anchoredPosition;
    }

    private void RefreshMotionFlags()
    {
        hasScaleMotion = scaleAmount > 0.0001f;
        hasRotationMotion = rotationDegrees > 0.0001f;
        bool layoutDriven = target != null &&
                            (target.GetComponent<UnityEngine.UI.LayoutElement>() != null ||
                             target.GetComponent<UnityEngine.UI.ContentSizeFitter>() != null ||
                             (target.parent != null &&
                              target.parent.GetComponent<UnityEngine.UI.LayoutGroup>() != null));
        animatePosition = floatDistance > 0.0001f && !layoutDriven;
    }

    private void OnDisable()
    {
        RestorePose();
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
        if (reducedMotion)
            RestorePose();
    }

    private void RestorePose()
    {
        if (target == null)
            return;
        if (hasScaleMotion)
            target.localScale = baseScale;
        if (hasRotationMotion)
            target.localRotation = baseRotation;
        if (animatePosition)
            target.anchoredPosition = basePosition;
    }

#if UNITY_EDITOR
    public void EditorConfigure(float sparkleSpeed, float scale, float rotation, float travel)
    {
        speed = sparkleSpeed;
        scaleAmount = scale;
        rotationDegrees = rotation;
        floatDistance = travel;
        RefreshMotionFlags();
    }
#endif
}
