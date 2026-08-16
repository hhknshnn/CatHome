using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class LowPolyButtonPress : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private RectTransform face;
    [SerializeField] private float pressedOffset = 7f;
    [SerializeField] private float settleDuration = 0.08f;

    private Vector2 restingPosition;
    private Coroutine settleCoroutine;

    private void Awake()
    {
        if (face == null)
            face = transform as RectTransform;

        if (face != null)
            restingPosition = face.anchoredPosition;
    }

    private void OnDisable()
    {
        if (settleCoroutine != null)
        {
            StopCoroutine(settleCoroutine);
            settleCoroutine = null;
        }

        if (face != null)
            face.anchoredPosition = restingPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveFace(restingPosition + Vector2.down * pressedOffset);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SettleBack();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SettleBack();
    }

    private void MoveFace(Vector2 position)
    {
        if (settleCoroutine != null)
            StopCoroutine(settleCoroutine);

        if (face != null)
            face.anchoredPosition = position;
    }

    private void SettleBack()
    {
        if (!isActiveAndEnabled || face == null)
            return;

        if (settleCoroutine != null)
            StopCoroutine(settleCoroutine);
        settleCoroutine = StartCoroutine(SettleRoutine());
    }

    private IEnumerator SettleRoutine()
    {
        Vector2 start = face.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = settleDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / settleDuration);
            face.anchoredPosition = Vector2.LerpUnclamped(start, restingPosition, Smooth(progress));
            yield return null;
        }

        face.anchoredPosition = restingPosition;
        settleCoroutine = null;
    }

    private static float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }
}
