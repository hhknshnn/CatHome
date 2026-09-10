using UnityEngine;

/// <summary>A quiet, screen-sized sleep medallion attached to the current breed's head.</summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class CatSleepZzzEffect : MonoBehaviour
{
    private CatCareFxCanvas presentation;
    private CatCareFxGraphic badge;
    private bool playing;
    private float elapsed;
    public bool IsPlaying => playing;
    public float Elapsed => elapsed;

    public void Begin()
    {
        if (!isActiveAndEnabled) return;
        presentation = CatCareFxCanvas.For(gameObject);
        if (badge == null)
            badge = presentation.CreateGraphic("SleepMedallion", CatCareFxGraphic.Shape.Sleep, 74f);
        elapsed = 0f;
        playing = true;
        Present();
    }

    public void Stop()
    {
        playing = false;
        elapsed = 0f;
        if (badge != null) badge.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!playing) return;
        elapsed += Time.deltaTime;
        Present();
    }

    private void Present()
    {
        if (presentation == null || badge == null) return;
        if (!presentation.TryHeadPosition(out Vector2 origin))
        {
            badge.gameObject.SetActive(false);
            return;
        }
        float floating = presentation.QuietMotion ? 0f : Mathf.Sin(elapsed * 1.45f) * 3f;
        badge.rectTransform.anchoredPosition = origin + new Vector2(30f, 61f + floating);
        badge.gameObject.SetActive(true);
    }

    private void OnDisable() => Stop();
    private void OnDestroy()
    {
        if (presentation != null) presentation.Release(badge);
    }
}
