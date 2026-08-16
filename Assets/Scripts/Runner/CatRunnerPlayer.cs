using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum CatRunnerGestureDirection
{
    None = 0,
    Left = 1,
    Right = 2,
    Up = 3,
    Down = 4
}

[DisallowMultipleComponent]
public sealed class CatRunnerPlayer : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float laneWidth = 1.35f;
    [SerializeField] private float laneChangeSpeed = 10.5f;
    [SerializeField] private float jumpVelocity = 5.7f;
    [SerializeField] private float gravity = -16f;
    [SerializeField] private float swipeThreshold = 45f;
    [SerializeField] private float dragSensitivity = 1.08f;
    [SerializeField] private float surfaceFollowSpeed = 8f;
    [SerializeField] private float slideDuration = 0.72f;
    [SerializeField, Range(0.45f, 0.9f)] private float slideVisualHeight = 0.62f;
    [SerializeField, Min(.2f)] private float standingCollisionHeight = .68f;
    [SerializeField, Min(.15f)] private float slidingCollisionHeight = .34f;
    [SerializeField] private bool reducedMotion;

    private Vector3 baseVisualPosition;
    private Vector3 baseVisualScale;
    private Quaternion baseVisualRotation;
    private int speedHash;
    private float targetLane;
    private float jumpHeight;
    private float surfaceHeight;
    private float targetSurfaceHeight;
    private float verticalVelocity;
    private float slideRemaining;
    private float hitCooldown;
    private Vector2 pointerStart;
    private float pointerLaneStart;
    private bool pointerTracking;
    private bool pointerGestureConsumed;
    private bool pointerHorizontalDrag;
    private bool running;
    private float hitReaction;
    private float landingReaction;
    private float celebrationRemaining;

    public float LanePosition => transform.localPosition.x;
    public float Height => surfaceHeight + jumpHeight;
    public float JumpHeight => jumpHeight;
    public float SurfaceHeight => surfaceHeight;
    public bool IsSliding => slideRemaining > 0f;
    public bool IsAirborne => jumpHeight > 0.001f || verticalVelocity > 0f;
    public float CollisionLower => Height;
    public float CollisionUpper => Height +
                                   (IsSliding ? slidingCollisionHeight : standingCollisionHeight);
    public event Action Landed;
    public event Action SlideStarted;
    public event Action JumpStarted;
    public event Action LaneChanged;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (visualRoot == null && animator != null)
            visualRoot = animator.transform;
        if (visualRoot != null)
        {
            baseVisualPosition = visualRoot.localPosition;
            baseVisualRotation = visualRoot.localRotation;
            baseVisualScale = visualRoot.localScale;
        }
        speedHash = Animator.StringToHash("Speed");
    }

    private void Update()
    {
        if (hitCooldown > 0f)
            hitCooldown -= Time.deltaTime;
        hitReaction = Mathf.MoveTowards(hitReaction, 0f, Time.deltaTime * 2.8f);
        landingReaction = Mathf.MoveTowards(landingReaction, 0f, Time.deltaTime * 5.5f);
        celebrationRemaining = Mathf.Max(0f, celebrationRemaining - Time.deltaTime);

        if (!running)
        {
            SetAnimationSpeed(0f);
            UpdateCelebrationPose();
            return;
        }

        ReadInput();
        UpdateSlide();
        UpdateSurface();
        UpdateJump();
        UpdateLane();
        SetAnimationSpeed(1f);
    }

    public void SetRunning(bool value)
    {
        running = value;
        if (!running)
        {
            SetAnimationSpeed(0f);
            pointerTracking = false;
            pointerGestureConsumed = false;
            pointerHorizontalDrag = false;
        }
    }

    public void ResetRun()
    {
        targetLane = 0f;
        jumpHeight = 0f;
        surfaceHeight = 0f;
        targetSurfaceHeight = 0f;
        verticalVelocity = 0f;
        slideRemaining = 0f;
        hitCooldown = 0f;
        pointerTracking = false;
        pointerGestureConsumed = false;
        pointerHorizontalDrag = false;
        hitReaction = 0f;
        landingReaction = 0f;
        celebrationRemaining = 0f;
        transform.localPosition = Vector3.zero;
        if (visualRoot != null)
        {
            visualRoot.localPosition = baseVisualPosition;
            visualRoot.localRotation = baseVisualRotation;
            visualRoot.localScale = baseVisualScale;
        }
    }

    /// <summary>
    /// Supplies the safe floor under the cat. Track ramps call this every frame;
    /// the player keeps jump height separate so jumping on an elevated deck is
    /// identical to jumping on the boulevard.
    /// </summary>
    public void SetTrackSurfaceHeight(float value)
    {
        targetSurfaceHeight = Mathf.Max(0f, value);
    }

    public bool TryRegisterHit()
    {
        if (hitCooldown > 0f)
            return false;

        hitCooldown = 1.5f;
        hitReaction = 1f;
        return true;
    }

    private void ReadInput()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
                MoveLane(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
                MoveLane(1);
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame ||
                keyboard.wKey.wasPressedThisFrame)
                Jump();
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                Slide();
        }

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                BeginPointer(touch.position.ReadValue());
            }
            if (pointerTracking && touch.press.isPressed)
                ContinuePointer(touch.position.ReadValue(), Screen.width);
            if (pointerTracking && touch.press.wasReleasedThisFrame)
            {
                EndPointer(touch.position.ReadValue(), Screen.width);
            }
        }
        if (Mouse.current != null &&
            (Touchscreen.current == null || !Touchscreen.current.primaryTouch.press.isPressed))
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                BeginPointer(Mouse.current.position.ReadValue());
            }
            if (pointerTracking && Mouse.current.leftButton.isPressed)
                ContinuePointer(Mouse.current.position.ReadValue(), Screen.width);
            if (pointerTracking && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                EndPointer(Mouse.current.position.ReadValue(), Screen.width);
            }
        }
#endif
    }

    private void BeginPointer(Vector2 position)
    {
        pointerStart = position;
        pointerLaneStart = targetLane;
        pointerTracking = true;
        pointerGestureConsumed = false;
        pointerHorizontalDrag = false;
    }

    private void ContinuePointer(Vector2 position, float screenWidth)
    {
        Vector2 delta = position - pointerStart;
        float gestureThreshold = GetScaledSwipeThreshold(
            swipeThreshold,
            screenWidth,
            Screen.height,
            Screen.dpi);
        CatRunnerGestureDirection intent = ClassifySwipe(delta, gestureThreshold);
        if (!pointerGestureConsumed && !pointerHorizontalDrag &&
            (intent == CatRunnerGestureDirection.Up || intent == CatRunnerGestureDirection.Down))
        {
            ApplyGesture(intent);
            pointerGestureConsumed = true;
            return;
        }

        float horizontalActivation = Mathf.Max(8f, gestureThreshold * 0.2f);
        if (!pointerGestureConsumed && Mathf.Abs(delta.x) >= horizontalActivation &&
            Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 0.72f)
        {
            bool beganDrag = !pointerHorizontalDrag;
            pointerHorizontalDrag = true;
            targetLane = GetDraggedLaneTarget(
                pointerLaneStart,
                delta.x,
                screenWidth,
                dragSensitivity);
            if (beganDrag)
                LaneChanged?.Invoke();
        }
    }

    private void EndPointer(Vector2 position, float screenWidth)
    {
        ContinuePointer(position, screenWidth);
        if (!pointerGestureConsumed)
        {
            float gestureThreshold = GetScaledSwipeThreshold(
                swipeThreshold,
                screenWidth,
                Screen.height,
                Screen.dpi);
            CatRunnerGestureDirection intent = ClassifySwipe(
                position - pointerStart,
                gestureThreshold);
            if (intent == CatRunnerGestureDirection.Up || intent == CatRunnerGestureDirection.Down)
                ApplyGesture(intent);
            else if (!pointerHorizontalDrag)
                ApplyGesture(intent);
        }

        if (pointerHorizontalDrag)
            targetLane = Mathf.Round(Mathf.Clamp(targetLane, -1f, 1f));
        pointerTracking = false;
        pointerGestureConsumed = false;
        pointerHorizontalDrag = false;
    }

    public static bool IsJumpSwipe(Vector2 delta, float threshold = 45f)
    {
        return ClassifySwipe(delta, threshold) == CatRunnerGestureDirection.Up;
    }

    public static bool IsSlideSwipe(Vector2 delta, float threshold = 45f)
    {
        return ClassifySwipe(delta, threshold) == CatRunnerGestureDirection.Down;
    }

    public static CatRunnerGestureDirection ClassifySwipe(
        Vector2 delta,
        float threshold = 45f)
    {
        float safeThreshold = Mathf.Max(1f, threshold);
        float absoluteX = Mathf.Abs(delta.x);
        float absoluteY = Mathf.Abs(delta.y);
        if (Mathf.Max(absoluteX, absoluteY) < safeThreshold)
            return CatRunnerGestureDirection.None;

        // The dominant axis always owns the gesture. This keeps a deliberate
        // lane drag from becoming a jump on wide or high-DPI screens.
        if (absoluteY >= absoluteX)
            return delta.y >= 0f
                ? CatRunnerGestureDirection.Up
                : CatRunnerGestureDirection.Down;
        return delta.x >= 0f
            ? CatRunnerGestureDirection.Right
            : CatRunnerGestureDirection.Left;
    }

    public static float GetDraggedLaneTarget(
        float initialLane,
        float dragPixels,
        float screenWidth,
        float sensitivity = 1.08f)
    {
        float laneSpanPixels = Mathf.Max(96f, Mathf.Max(1f, screenWidth) * 0.22f);
        float laneDelta = dragPixels / laneSpanPixels * Mathf.Max(0.1f, sensitivity);
        return Mathf.Clamp(initialLane + laneDelta, -1f, 1f);
    }

    public static float GetScaledSwipeThreshold(
        float referenceThreshold,
        float screenWidth,
        float screenHeight,
        float screenDpi)
    {
        float safeReference = Mathf.Max(1f, referenceThreshold);
        if (screenDpi >= 72f)
            return safeReference * Mathf.Clamp(screenDpi / 160f, .72f, 2.2f);

        float shortEdge = Mathf.Max(1f, Mathf.Min(screenWidth, screenHeight));
        return safeReference * Mathf.Clamp(shortEdge / 1080f, .65f, 1.65f);
    }

    public void SetReducedMotion(bool value)
    {
        reducedMotion = value;
    }

    private void ApplyGesture(CatRunnerGestureDirection direction)
    {
        switch (direction)
        {
            case CatRunnerGestureDirection.Left:
                MoveLane(-1);
                break;
            case CatRunnerGestureDirection.Right:
                MoveLane(1);
                break;
            case CatRunnerGestureDirection.Up:
                Jump();
                break;
            case CatRunnerGestureDirection.Down:
                Slide();
                break;
        }
    }

    private void MoveLane(int direction)
    {
        int currentLane = Mathf.RoundToInt(targetLane);
        float nextLane = Mathf.Clamp(currentLane + direction, -1, 1);
        if (Mathf.Abs(nextLane - targetLane) <= .001f)
            return;
        targetLane = nextLane;
        LaneChanged?.Invoke();
    }

    private void Jump()
    {
        if (jumpHeight <= 0.001f)
        {
            slideRemaining = 0f;
            verticalVelocity = jumpVelocity;
            JumpStarted?.Invoke();
        }
    }

    private void Slide()
    {
        bool wasSliding = IsSliding;
        slideRemaining = Mathf.Max(slideRemaining, slideDuration);
        if (jumpHeight > 0.05f)
            verticalVelocity = Mathf.Min(verticalVelocity, -jumpVelocity * 1.22f);
        if (!wasSliding)
            SlideStarted?.Invoke();
    }

    private void UpdateSlide()
    {
        if (slideRemaining > 0f)
            slideRemaining = Mathf.Max(0f, slideRemaining - Time.deltaTime);
    }

    private void UpdateSurface()
    {
        surfaceHeight = Mathf.MoveTowards(
            surfaceHeight,
            targetSurfaceHeight,
            Mathf.Max(1f, surfaceFollowSpeed) * Time.deltaTime);
    }

    private void UpdateJump()
    {
        bool wasAirborne = jumpHeight > 0.001f || verticalVelocity > 0f;
        if (jumpHeight > 0f || verticalVelocity > 0f)
        {
            verticalVelocity += gravity * Time.deltaTime;
            jumpHeight += verticalVelocity * Time.deltaTime;
            if (jumpHeight <= 0f)
            {
                jumpHeight = 0f;
                verticalVelocity = 0f;
                if (wasAirborne)
                {
                    landingReaction = 1f;
                    Landed?.Invoke();
                }
            }
        }
    }

    private void UpdateLane()
    {
        Vector3 position = transform.localPosition;
        float targetX = targetLane * laneWidth;
        float previousX = position.x;
        float laneBlend = 1f - Mathf.Exp(-Mathf.Max(1f, laneChangeSpeed) * Time.deltaTime);
        position.x = Mathf.Lerp(position.x, targetX, laneBlend);
        if (Mathf.Abs(position.x - targetX) < 0.001f)
            position.x = targetX;
        position.y = Height;
        position.z = 0f;
        transform.localPosition = position;

        if (visualRoot != null)
        {
            float lateral = position.x - previousX;
            float lean = Mathf.Clamp(-lateral * 70f, -12f, 12f);
            if (hitCooldown > 0f)
                lean += Mathf.Sin(Time.time * 34f) * 7f;
            float slide01 = IsSliding ? (reducedMotion ? .35f : 1f) : 0f;
            float jump01 = reducedMotion ? 0f : Mathf.Clamp01(jumpHeight / 1.1f);
            float landing01 = reducedMotion ? 0f : landingReaction;
            float hit01 = reducedMotion ? 0f : hitReaction;
            Vector3 desiredScale = Vector3.Scale(
                baseVisualScale,
                new Vector3(
                    1f + slide01 * .08f + landing01 * .1f,
                    Mathf.Lerp(1f + jump01 * .08f - landing01 * .13f,
                        slideVisualHeight,
                        slide01),
                    1f + slide01 * .12f));
            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                desiredScale,
                1f - Mathf.Exp(-16f * Time.deltaTime));
            Vector3 desiredVisualPosition = baseVisualPosition +
                                            new Vector3(0f, -slide01 * .1f, slide01 * .15f);
            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                desiredVisualPosition,
                1f - Mathf.Exp(-16f * Time.deltaTime));
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                baseVisualRotation * Quaternion.Euler(
                    slide01 * 9f - jump01 * 6f,
                    hit01 * Mathf.Sin(Time.time * 30f) * 5f,
                    reducedMotion ? 0f : lean + hit01 * Mathf.Sin(Time.time * 34f) * 5f),
                1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }

    public void PlayCelebration(float seconds = 1.8f)
    {
        celebrationRemaining = Mathf.Max(celebrationRemaining, Mathf.Max(0f, seconds));
    }

    private void UpdateCelebrationPose()
    {
        if (visualRoot == null)
            return;
        if (celebrationRemaining <= 0f || reducedMotion)
        {
            visualRoot.localPosition = Vector3.Lerp(
                visualRoot.localPosition,
                baseVisualPosition,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                baseVisualRotation,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            visualRoot.localScale = Vector3.Lerp(
                visualRoot.localScale,
                baseVisualScale,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
            return;
        }
        float wave = Mathf.Sin(Time.unscaledTime * 9f);
        visualRoot.localPosition = Vector3.Lerp(
            visualRoot.localPosition,
            baseVisualPosition + Vector3.up * (0.06f + Mathf.Max(0f, wave) * .08f),
            1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
        visualRoot.localRotation = Quaternion.Slerp(
            visualRoot.localRotation,
            baseVisualRotation * Quaternion.Euler(0f, wave * 8f, wave * 5f),
            1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
    }

    private void SetAnimationSpeed(float value)
    {
        if (animator != null && animator.isActiveAndEnabled)
            animator.SetFloat(speedHash, value, 0.08f, Time.deltaTime);
    }

#if UNITY_EDITOR
    public void EditorConfigure(Animator runnerAnimator, Transform modelRoot)
    {
        animator = runnerAnimator;
        visualRoot = modelRoot;
        swipeThreshold = 45f;
        laneChangeSpeed = 10.5f;
        dragSensitivity = 1.08f;
        surfaceFollowSpeed = 8f;
        slideDuration = .72f;
        slideVisualHeight = .62f;
        standingCollisionHeight = .68f;
        slidingCollisionHeight = .34f;
    }
#endif
}
