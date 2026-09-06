using UnityEngine;

/// <summary>
/// Popup-local presentation layer. It owns safe-area fitting, staged card reveals,
/// counter pops and the calm idle sheen without touching offline-progress state.
/// </summary>
[DisallowMultipleComponent]
public sealed class WhileYouWereAwayPopupFx : MonoBehaviour
{
    [Header("Safe Area Layout")]
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private RectTransform layoutRoot;
    [SerializeField] private Vector2 designSize = new Vector2(980f, 680f);
    [SerializeField] private Vector2 safeMargin = new Vector2(44f, 34f);

    [Header("Reveal Targets")]
    [SerializeField] private RectTransform panelVisual;
    [SerializeField] private RectTransform durationCounter;
    [SerializeField] private RectTransform summaryBadge;
    [SerializeField] private RectTransform actionVisual;
    [SerializeField] private RectTransform[] rewardCards = new RectTransform[0];
    [SerializeField] private RectTransform[] counterTargets = new RectTransform[0];

    [Header("Ambient Polish")]
    [SerializeField] private LowPolyPanelGraphic[] sheenSurfaces = new LowPolyPanelGraphic[0];
    [SerializeField] private RectTransform[] idleFloatTargets = new RectTransform[0];
    [SerializeField] private bool motionEnabled = true;
    [SerializeField, Min(2f)] private float sheenPeriod = 5.2f;

    private Vector2 panelBasePosition;
    private Quaternion panelBaseRotation;
    private Vector2 closeStartPosition;
    private Quaternion closeStartRotation;
    private Vector2 summaryBasePosition;
    private Vector2 actionBasePosition;
    private Vector2[] cardBasePositions;
    private Vector3[] cardBaseScales;
    private Vector3[] counterBaseScales;
    private Vector2[] floatBasePositions;
    private Vector2 lastSafeSize = new Vector2(float.NaN, float.NaN);
    private float idleStartedAt;
    private float phaseOffset;
    private bool basesCaptured;
    private bool idle;

    private void Awake()
    {
        phaseOffset = (EntityId.ToULong(GetEntityId()) % 857UL) / 857f;
        ApplySafeAreaLayout(true);
        CaptureBases();
        ClearSheen();
    }

    private void OnEnable()
    {
        ApplySafeAreaLayout(true);
    }

    private void LateUpdate()
    {
        ApplySafeAreaLayout(false);
        if (!idle || !motionEnabled)
            return;

        float elapsed = Time.unscaledTime - idleStartedAt;
        // The return summary is calm information; motion belongs to the reveal.
    }

    public void PrepareForOpen()
    {
        ApplySafeAreaLayout(true);
        CaptureBases();
        idle = false;
        ClearSheen();

        if (!motionEnabled)
        {
            RestoreTargets();
            return;
        }

        if (panelVisual != null)
        {
            panelVisual.anchoredPosition = panelBasePosition + Vector2.down * 34f;
            panelVisual.localRotation = panelBaseRotation * Quaternion.Euler(0f, 0f, -2.25f);
        }

        if (durationCounter != null)
            durationCounter.localScale = Vector3.one * 0.74f;
        if (summaryBadge != null)
        {
            summaryBadge.anchoredPosition = summaryBasePosition + Vector2.down * 14f;
            summaryBadge.localScale = Vector3.one * 0.88f;
        }
        if (actionVisual != null)
        {
            actionVisual.anchoredPosition = actionBasePosition + Vector2.down * 18f;
            actionVisual.localScale = Vector3.one * 0.86f;
        }

        for (int i = 0; i < rewardCards.Length; i++)
        {
            RectTransform card = rewardCards[i];
            if (card == null)
                continue;
            float horizontal = (i - (rewardCards.Length - 1) * 0.5f) * 12f;
            card.anchoredPosition = cardBasePositions[i] + new Vector2(horizontal, -24f);
            card.localScale = cardBaseScales[i] * 0.82f;
        }

        for (int i = 0; i < counterTargets.Length; i++)
        {
            if (counterTargets[i] != null)
                counterTargets[i].localScale = counterBaseScales[i] * 0.68f;
        }
    }

    public void ApplyOpening(float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (!motionEnabled)
        {
            RestoreTargets();
            return;
        }

        float panelProgress = EaseOutCubic(progress);
        if (panelVisual != null)
        {
            panelVisual.anchoredPosition = Vector2.LerpUnclamped(
                panelBasePosition + Vector2.down * 34f,
                panelBasePosition,
                panelProgress);
            panelVisual.localRotation = Quaternion.SlerpUnclamped(
                panelBaseRotation * Quaternion.Euler(0f, 0f, -2.25f),
                panelBaseRotation,
                panelProgress);
        }

        ApplyPop(durationCounter, Vector3.one, Normalize(progress, 0.16f, 0.52f), 0.74f);

        for (int i = 0; i < rewardCards.Length; i++)
        {
            RectTransform card = rewardCards[i];
            if (card == null)
                continue;

            float reveal = Normalize(progress, 0.24f + i * 0.075f, 0.69f + i * 0.055f);
            float eased = EaseOutBack(reveal);
            float horizontal = (i - (rewardCards.Length - 1) * 0.5f) * 12f;
            card.anchoredPosition = Vector2.LerpUnclamped(
                cardBasePositions[i] + new Vector2(horizontal, -24f),
                cardBasePositions[i],
                EaseOutCubic(reveal));
            card.localScale = cardBaseScales[i] * Mathf.LerpUnclamped(0.82f, 1f, eased);
        }

        for (int i = 0; i < counterTargets.Length; i++)
        {
            RectTransform counter = counterTargets[i];
            if (counter == null)
                continue;
            float reveal = Normalize(progress, 0.43f + i * 0.065f, 0.76f + i * 0.055f);
            ApplyPop(counter, counterBaseScales[i], reveal, 0.68f);
        }

        float summaryProgress = Normalize(progress, 0.58f, 0.88f);
        if (summaryBadge != null)
        {
            summaryBadge.anchoredPosition = Vector2.LerpUnclamped(
                summaryBasePosition + Vector2.down * 14f,
                summaryBasePosition,
                EaseOutCubic(summaryProgress));
            summaryBadge.localScale = Vector3.one * Mathf.LerpUnclamped(
                0.88f, 1f, EaseOutBack(summaryProgress));
        }

        float actionProgress = Normalize(progress, 0.68f, 1f);
        if (actionVisual != null)
        {
            actionVisual.anchoredPosition = Vector2.LerpUnclamped(
                actionBasePosition + Vector2.down * 18f,
                actionBasePosition,
                EaseOutCubic(actionProgress));
            actionVisual.localScale = Vector3.one * Mathf.LerpUnclamped(
                0.86f, 1f, EaseOutBack(actionProgress));
        }
    }

    public void CompleteOpen()
    {
        RestoreTargets();
        idle = true;
        idleStartedAt = Time.unscaledTime;
    }

    public void BeginClose()
    {
        idle = false;
        ClearSheen();
        closeStartPosition = panelVisual != null
            ? panelVisual.anchoredPosition
            : panelBasePosition;
        closeStartRotation = panelVisual != null
            ? panelVisual.localRotation
            : panelBaseRotation;
    }

    public void ApplyClosing(float progress)
    {
        idle = false;
        ClearSheen();
        if (!motionEnabled || panelVisual == null)
            return;

        float eased = Smooth(progress);
        panelVisual.anchoredPosition = Vector2.LerpUnclamped(
            closeStartPosition,
            panelBasePosition + Vector2.up * 18f,
            eased);
        panelVisual.localRotation = Quaternion.SlerpUnclamped(
            closeStartRotation,
            panelBaseRotation * Quaternion.Euler(0f, 0f, 1.25f),
            eased);
    }

    public void ResetVisualState()
    {
        idle = false;
        ClearSheen();
        RestoreTargets();
    }

    public void SetReducedMotion(bool reducedMotion)
    {
        motionEnabled = !reducedMotion;
        if (reducedMotion)
            ResetVisualState();
    }

    public static float CalculateFitScale(Vector2 safeSize, Vector2 targetSize, Vector2 margin)
    {
        float availableWidth = Mathf.Max(1f, safeSize.x - margin.x * 2f);
        float availableHeight = Mathf.Max(1f, safeSize.y - margin.y * 2f);
        float widthScale = availableWidth / Mathf.Max(1f, targetSize.x);
        float heightScale = availableHeight / Mathf.Max(1f, targetSize.y);
        return Mathf.Clamp(Mathf.Min(1f, widthScale, heightScale), 0.01f, 1f);
    }

    private void ApplySafeAreaLayout(bool force)
    {
        if (safeAreaRoot == null || layoutRoot == null)
            return;

        Vector2 safeSize = safeAreaRoot.rect.size;
        if (!force && (safeSize - lastSafeSize).sqrMagnitude < 0.25f)
            return;

        lastSafeSize = safeSize;
        layoutRoot.anchorMin = new Vector2(0.5f, 0.5f);
        layoutRoot.anchorMax = new Vector2(0.5f, 0.5f);
        layoutRoot.pivot = new Vector2(0.5f, 0.5f);
        layoutRoot.anchoredPosition = Vector2.zero;
        layoutRoot.sizeDelta = designSize;
        float fit = CalculateFitScale(safeSize, designSize, safeMargin);
        layoutRoot.localScale = Vector3.one * fit;
    }

    private void CaptureBases()
    {
        panelBasePosition = panelVisual != null ? panelVisual.anchoredPosition : Vector2.zero;
        panelBaseRotation = panelVisual != null ? panelVisual.localRotation : Quaternion.identity;
        summaryBasePosition = summaryBadge != null ? summaryBadge.anchoredPosition : Vector2.zero;
        actionBasePosition = actionVisual != null ? actionVisual.anchoredPosition : Vector2.zero;

        cardBasePositions = EnsureLength(cardBasePositions, rewardCards.Length);
        cardBaseScales = EnsureLength(cardBaseScales, rewardCards.Length);
        for (int i = 0; i < rewardCards.Length; i++)
        {
            RectTransform card = rewardCards[i];
            cardBasePositions[i] = card != null ? card.anchoredPosition : Vector2.zero;
            cardBaseScales[i] = card != null ? card.localScale : Vector3.one;
        }

        counterBaseScales = EnsureLength(counterBaseScales, counterTargets.Length);
        for (int i = 0; i < counterTargets.Length; i++)
            counterBaseScales[i] = counterTargets[i] != null
                ? counterTargets[i].localScale
                : Vector3.one;

        floatBasePositions = EnsureLength(floatBasePositions, idleFloatTargets.Length);
        for (int i = 0; i < idleFloatTargets.Length; i++)
            floatBasePositions[i] = idleFloatTargets[i] != null
                ? idleFloatTargets[i].anchoredPosition
                : Vector2.zero;

        basesCaptured = true;
    }

    private void RestoreTargets()
    {
        if (!basesCaptured)
            CaptureBases();

        if (panelVisual != null)
        {
            panelVisual.anchoredPosition = panelBasePosition;
            panelVisual.localRotation = panelBaseRotation;
        }
        if (durationCounter != null)
            durationCounter.localScale = Vector3.one;
        if (summaryBadge != null)
        {
            summaryBadge.anchoredPosition = summaryBasePosition;
            summaryBadge.localScale = Vector3.one;
        }
        if (actionVisual != null)
        {
            actionVisual.anchoredPosition = actionBasePosition;
            actionVisual.localScale = Vector3.one;
        }

        for (int i = 0; i < rewardCards.Length; i++)
        {
            if (rewardCards[i] == null)
                continue;
            rewardCards[i].anchoredPosition = cardBasePositions[i];
            rewardCards[i].localScale = cardBaseScales[i];
        }
        for (int i = 0; i < counterTargets.Length; i++)
        {
            if (counterTargets[i] != null)
                counterTargets[i].localScale = counterBaseScales[i];
        }
        for (int i = 0; i < idleFloatTargets.Length; i++)
        {
            if (idleFloatTargets[i] != null)
                idleFloatTargets[i].anchoredPosition = floatBasePositions[i];
        }
    }

    private void ApplyIdleFloat(float elapsed)
    {
        for (int i = 0; i < idleFloatTargets.Length; i++)
        {
            RectTransform target = idleFloatTargets[i];
            if (target == null)
                continue;
            float phase = elapsed * (1.1f + i * 0.09f) + phaseOffset * 6.28318f + i * 1.73f;
            float travel = 1.6f + i * 0.35f;
            target.anchoredPosition = floatBasePositions[i] + Vector2.up * Mathf.Sin(phase) * travel;
        }
    }

    private void ApplyIdleSheen(float elapsed)
    {
        float cycle = Mathf.Repeat(elapsed / Mathf.Max(2f, sheenPeriod) + phaseOffset, 1f);
        for (int i = 0; i < sheenSurfaces.Length; i++)
        {
            LowPolyPanelGraphic surface = sheenSurfaces[i];
            if (surface == null)
                continue;
            float localCycle = Mathf.Repeat(cycle - i * 0.12f + 1f, 1f);
            if (localCycle < 0.42f)
            {
                float sweep = Mathf.Round(localCycle / 0.42f * 20f) / 20f;
                surface.SetRuntimeGloss(sweep, i == 0 ? 0.24f : 0.34f);
            }
            else
                surface.SetRuntimeGloss(-1f, 0f);
        }
    }

    private void ClearSheen()
    {
        for (int i = 0; i < sheenSurfaces.Length; i++)
        {
            if (sheenSurfaces[i] != null)
                sheenSurfaces[i].SetRuntimeGloss(-1f, 0f);
        }
    }

    private static void ApplyPop(RectTransform target, Vector3 baseScale, float progress, float startScale)
    {
        if (target == null)
            return;
        target.localScale = baseScale * Mathf.LerpUnclamped(startScale, 1f, EaseOutBack(progress));
    }

    private static float Normalize(float value, float start, float end)
    {
        return Mathf.Clamp01((value - start) / Mathf.Max(0.0001f, end - start));
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.28f;
        float shifted = Mathf.Clamp01(value) - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted +
               overshoot * shifted * shifted;
    }

    private static float Smooth(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static Vector2[] EnsureLength(Vector2[] source, int length)
    {
        return source != null && source.Length == length ? source : new Vector2[length];
    }

    private static Vector3[] EnsureLength(Vector3[] source, int length)
    {
        return source != null && source.Length == length ? source : new Vector3[length];
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        RectTransform safeRoot,
        RectTransform cardLayoutRoot,
        RectTransform visual,
        RectTransform duration,
        RectTransform summary,
        RectTransform action,
        RectTransform[] cards,
        RectTransform[] counters,
        LowPolyPanelGraphic[] sheen,
        RectTransform[] floatTargets)
    {
        safeAreaRoot = safeRoot;
        layoutRoot = cardLayoutRoot;
        panelVisual = visual;
        durationCounter = duration;
        summaryBadge = summary;
        actionVisual = action;
        rewardCards = cards ?? new RectTransform[0];
        counterTargets = counters ?? new RectTransform[0];
        sheenSurfaces = sheen ?? new LowPolyPanelGraphic[0];
        idleFloatTargets = floatTargets ?? new RectTransform[0];
        basesCaptured = false;
        ApplySafeAreaLayout(true);
        CaptureBases();
    }
#endif
}
