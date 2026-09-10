using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Candy-style interaction feedback shared by every premium CTA. It animates
/// scale and the panel's internal sheen without allocating or changing layout.
/// </summary>
[DisallowMultipleComponent]
public sealed class PremiumButtonFx : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private LowPolyPanelGraphic surface;
    [SerializeField] private Button button;
    [SerializeField] private bool primaryAction = true;
    [SerializeField, Min(1.8f)] private float shimmerSeconds = 3.6f;
    [SerializeField, Range(0f, 0.04f)] private float idlePulse = 0f;
    [SerializeField, Range(0.9f, 1f)] private float pressedScale = 0.97f;
    [SerializeField, Range(1f, 1.12f)] private float hoverScale = 1.015f;
    [SerializeField] private bool animateScale = true;

    private Vector3 baseScale = Vector3.one;
    private float currentScale = 1f;
    private float scaleVelocity;
    private float releaseBounce;
    private bool hovering;
    private bool pressed;
    private bool selected;
    private bool reducedMotion;
    private float phaseOffset;
    private float lastAppliedScale = float.NaN;

    private void Awake()
    {
        Resolve();
        CaptureBaseScale();
        phaseOffset = (EntityId.ToULong(GetEntityId()) % 997UL) / 997f;
    }

    private void OnEnable()
    {
        Resolve();
        CaptureBaseScale();
        ResetInteractionState();
        if (animateScale)
            ApplyScale(1f, true);
    }

    private void Update()
    {
        bool interactable = button == null || button.IsInteractable();
        bool motionReduced = reducedMotion || CatRunnerProgressService.ReducedMotion;
        bool scaleActive = animateScale && !motionReduced &&
                           (pressed || hovering || selected ||
                            releaseBounce > 0f || Mathf.Abs(currentScale - 1f) > 0.001f);
        if (scaleActive)
        {
            float target = 1f;
            if (interactable)
            {
                if (pressed)
                    target = pressedScale;
                else if (releaseBounce > 0f)
                    target = Mathf.Lerp(1f, 1.025f, releaseBounce);
                else if (hovering || selected)
                    target = hoverScale;
                else if (primaryAction)
                    target += Mathf.Sin((Time.unscaledTime + phaseOffset) * 2.1f) * idlePulse;
            }

            releaseBounce = Mathf.MoveTowards(releaseBounce, 0f, Time.unscaledDeltaTime * 5.8f);
            currentScale = Mathf.SmoothDamp(
                currentScale,
                target,
                ref scaleVelocity,
                pressed ? 0.045f : 0.105f,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            ApplyScale(currentScale);
        }
        else
        {
            scaleVelocity = 0f;
            releaseBounce = 0f;
            if (Mathf.Abs(currentScale - 1f) > 0.0001f)
            {
                currentScale = 1f;
                ApplyScale(1f);
            }
        }

        if (surface == null)
            return;

        surface.SetInteractionState(pressed && interactable, selected || hovering, !interactable);

        // Feedback is driven by intent. No perpetual pulse or gloss sweep
        // competes with the cat while the player reads or watches the room.
        if (motionReduced || !interactable || (!hovering && !selected))
            surface.SetRuntimeGloss(-1f, 0f);
        else
            surface.SetRuntimeGloss(.5f, .10f);
    }

    public void OnPointerEnter(PointerEventData eventData) => hovering = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button == null || button.IsInteractable())
            pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pressed)
            return;
        pressed = false;
        releaseBounce = 1f;
    }

    public void OnSelect(BaseEventData eventData) => selected = true;
    public void OnDeselect(BaseEventData eventData) => selected = false;

    public void OnSubmit(BaseEventData eventData)
    {
        if (button == null || button.IsInteractable())
            releaseBounce = 1f;
    }

    public void SetReducedMotion(bool value)
    {
        if (reducedMotion == value)
            return;
        reducedMotion = value;
        if (!reducedMotion)
            return;
        currentScale = 1f;
        scaleVelocity = 0f;
        releaseBounce = 0f;
        ApplyScale(1f, true);
        if (surface != null)
            surface.SetRuntimeGloss(-1f, 0f);
    }

    public void Configure(
        RectTransform visual,
        LowPolyPanelGraphic graphic,
        Button targetButton,
        bool isPrimary,
        bool allowScale = true)
    {
        visualRoot = visual;
        surface = graphic;
        button = targetButton;
        primaryAction = isPrimary;
        animateScale = allowScale;
        shimmerSeconds = isPrimary ? 3.6f : 4.8f;
        idlePulse = 0f;
        CaptureBaseScale();
    }

    private void Resolve()
    {
        if (visualRoot == null)
            visualRoot = transform as RectTransform;
        if (button == null)
            button = GetComponent<Button>();
        if (surface == null && button != null)
            surface = button.targetGraphic as LowPolyPanelGraphic;
        if (surface == null)
            surface = GetComponentInChildren<LowPolyPanelGraphic>(true);

        RectTransform buttonRect = button != null ? button.transform as RectTransform : null;
        if (animateScale && visualRoot == buttonRect && IsLayoutDriven(buttonRect))
            animateScale = false;
    }

    private void CaptureBaseScale()
    {
        if (visualRoot != null)
            baseScale = visualRoot.localScale;
        lastAppliedScale = float.NaN;
    }

    private void ApplyScale(float value, bool force = false)
    {
        if (visualRoot == null || (!animateScale && !force))
            return;
        if (!force && Mathf.Abs(lastAppliedScale - value) <= 0.0005f)
            return;
        visualRoot.localScale = baseScale * value;
        lastAppliedScale = value;
    }

    private static bool IsLayoutDriven(RectTransform rect)
    {
        if (rect == null)
            return false;
        if (rect.GetComponent<LayoutElement>() != null ||
            rect.GetComponent<ContentSizeFitter>() != null)
        {
            return true;
        }
        return rect.parent != null && rect.parent.GetComponent<LayoutGroup>() != null;
    }

    private void ResetInteractionState()
    {
        currentScale = 1f;
        scaleVelocity = 0f;
        releaseBounce = 0f;
        hovering = false;
        pressed = false;
        selected = false;
    }

    private void OnDisable()
    {
        if (surface != null)
        {
            surface.SetRuntimeGloss(-1f, 0f);
            surface.SetInteractionState(false, false, false);
        }
        if (animateScale)
            ApplyScale(1f, true);
        ResetInteractionState();
    }

#if UNITY_EDITOR
    public void EditorConfigure(
        RectTransform visual,
        LowPolyPanelGraphic graphic,
        Button targetButton,
        bool isPrimary,
        bool allowScale = true)
    {
        Configure(visual, graphic, targetButton, isPrimary, allowScale);
    }
#endif
}
