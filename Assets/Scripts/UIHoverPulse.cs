using UnityEngine;

public class UIHoverPulse : MonoBehaviour
{
    [SerializeField] private float animationSpeed = 2.5f;
    [SerializeField] private float floatingDistance = 4f;
    [SerializeField] private float rotationAmount = 1.5f;
    [SerializeField] private float scaleAmount = 0.025f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        originalPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;
    }

    private void Update()
    {
        float wave =
            Mathf.Sin(Time.unscaledTime * animationSpeed);

        float scaleWave =
            Mathf.Sin(
                Time.unscaledTime *
                animationSpeed *
                1.25f
            );

        rectTransform.anchoredPosition =
            originalPosition +
            Vector2.up * wave * floatingDistance;

        rectTransform.localRotation =
            originalRotation *
            Quaternion.Euler(
                0f,
                0f,
                wave * rotationAmount
            );

        rectTransform.localScale =
            originalScale *
            (1f + scaleWave * scaleAmount);
    }

    private void OnDisable()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;
    }
}