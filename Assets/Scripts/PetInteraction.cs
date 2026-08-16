using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class PetInteraction : MonoBehaviour
{
    public event System.Action SuccessfulPetGesture;

    /// <summary>
    /// Raised every time a petting session actually starts (hold or swipe).
    /// Unlike SuccessfulPetGesture this does not require a validated swipe.
    /// </summary>
    public event System.Action PettingStarted;
    [Header("Cat")]
    [SerializeField] private CatMovement catMovement;
    [SerializeField] private BowlInteraction bowlInteraction;
    [SerializeField] private SleepInteraction sleepInteraction;
    [SerializeField] private HungerSystem hungerSystem;
    [SerializeField] private ThirstSystem thirstSystem;
    [SerializeField] private Animator animator;

    [Header("Feedback")]
    [SerializeField] private PetHeartEffect heartEffect;
    [SerializeField] private PetSoundController soundController;
    [SerializeField] private PetTutorialHint tutorialHint;

    [Header("Pointer")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField, Min(0f)] private float holdThreshold = 0.2f;
    [Tooltip("Pointer movement needed to start petting immediately. Movement never cancels petting.")]
    [SerializeField, Min(0f)] private float dragTolerance = 24f;
    [SerializeField, Min(0.01f)] private float raycastDistance = 100f;
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Header("Animator")]
    [SerializeField] private string speedAnimatorParameter = "Speed";
    [SerializeField] private string petState = "Pet";
    [SerializeField] private string idleState = "Idle";
    [SerializeField, Min(0f)] private float animationTransitionTime = 0.15f;

    [Header("Diagnostics")]
    [SerializeField] private bool debugLogs;

    private const string BaseLayerPrefix = "Base Layer.";
    private const int MousePointerId = -1;

    private static PetInteraction activeSession;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private bool pointerTracked;
    private bool trackingTouch;
    private int pointerId;
    private Vector2 pointerStartPosition;
    private float pointerStartTime;
    private int speedParameterHash;

    public bool IsPetting { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveSession()
    {
        activeSession = null;
    }

    private void Reset()
    {
        ResolveReferences();
    }

    private void Awake()
    {
        ResolveReferences();
        speedParameterHash = Animator.StringToHash(speedAnimatorParameter);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        UpdateInputSystemPointer();
#else
        UpdateLegacyMousePointer();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void UpdateInputSystemPointer()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
                TryTrackPointer(touch.position.ReadValue(), touch.touchId.ReadValue(), true);

            if (pointerTracked && trackingTouch)
            {
                if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    LogEvent("Touch was canceled.");
                    CancelPointer();
                    return;
                }

                if (!touch.press.isPressed)
                {
                    ReleasePointer();
                    return;
                }

                if (touch.touchId.ReadValue() != pointerId)
                {
                    LogEvent("Tracked touch id changed; pointer session was canceled.");
                    CancelPointer();
                    return;
                }

                UpdateTrackedPointer(touch.position.ReadValue());
                return;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
            TryTrackPointer(mouse.position.ReadValue(), MousePointerId, false);

        if (!pointerTracked || trackingTouch)
            return;

        if (!mouse.leftButton.isPressed)
        {
            ReleasePointer();
            return;
        }

        UpdateTrackedPointer(mouse.position.ReadValue());
    }
#else
    private void UpdateLegacyMousePointer()
    {
        if (Input.GetMouseButtonDown(0))
            TryTrackPointer(Input.mousePosition, MousePointerId, false);

        if (!pointerTracked)
            return;

        if (!Input.GetMouseButton(0))
        {
            ReleasePointer();
            return;
        }

        UpdateTrackedPointer(Input.mousePosition);
    }
#endif

    private void TryTrackPointer(Vector2 screenPosition, int newPointerId, bool isTouch)
    {
        LogEvent($"Pointer press detected ({(isTouch ? "touch" : "mouse")}) at {screenPosition}.");

        if (pointerTracked || IsPetting)
        {
            LogEvent("Pointer press ignored because this component already has an active pointer.");
            return;
        }

        if (activeSession != null)
        {
            LogEvent($"Pointer press ignored because '{activeSession.name}' owns the active petting session.");
            return;
        }

        if (TryGetBlockingUi(screenPosition, newPointerId, out GameObject blockingUi))
        {
            LogEvent($"Pointer blocked by interactive UI '{GetHierarchyPath(blockingUi.transform)}'.");
            return;
        }

        Camera camera = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (camera == null)
        {
            LogWarning("Pointer press ignored because no gameplay camera is assigned and Camera.main was not found.");
            return;
        }

        if (!TryFindCatCollider(camera.ScreenPointToRay(screenPosition), out Collider catCollider))
        {
            LogEvent("Cat collider was not found under the pointer.");
            return;
        }

        LogEvent($"Cat collider found: '{GetHierarchyPath(catCollider.transform)}'.");

        if (!CanBeginPetting(out string failureReason))
        {
            LogEvent($"CanBeginPetting returned false: {failureReason}");
            return;
        }

        pointerTracked = true;
        trackingTouch = isTouch;
        pointerId = newPointerId;
        pointerStartPosition = screenPosition;
        pointerStartTime = Time.unscaledTime;

        if (holdThreshold <= 0f)
            TryBeginPetting();
    }

    private void UpdateTrackedPointer(Vector2 screenPosition)
    {
        if (IsPetting)
            return;

        bool pettingGestureStarted =
            (screenPosition - pointerStartPosition).sqrMagnitude >=
            dragTolerance * dragTolerance;
        bool holdCompleted = Time.unscaledTime - pointerStartTime >= holdThreshold;

        if (pettingGestureStarted || holdCompleted)
            TryBeginPetting();
    }

    private void TryBeginPetting()
    {
        if (!pointerTracked || IsPetting)
            return;

        if (activeSession != null)
        {
            LogEvent($"Petting could not start because '{activeSession.name}' owns the active session.");
            CancelPointer();
            return;
        }

        if (!CanBeginPetting(out string failureReason))
        {
            LogEvent($"CanBeginPetting returned false: {failureReason}");
            CancelPointer();
            return;
        }

        if (!ValidateAnimator(out failureReason))
        {
            LogWarning(failureReason);
            CancelPointer();
            return;
        }

        bool tutorialSwipeValidated =
            (CurrentPointerPosition() - pointerStartPosition).sqrMagnitude >=
            dragTolerance * dragTolerance;

        activeSession = this;
        IsPetting = true;

        try
        {
            catMovement.SetMovementLocked(this, true);
            animator.SetFloat(speedParameterHash, 0f);
            animator.SetLayerWeight(0, 1f);
            int petStateHash = Animator.StringToHash(BaseLayerPrefix + petState);
            LogEvent($"CrossFade target: '{BaseLayerPrefix + petState}' (hash {petStateHash}).");
            animator.CrossFadeInFixedTime(
                petStateHash,
                animationTransitionTime,
                0
            );
            if (heartEffect != null)
                heartEffect.Play();
            if (soundController != null)
                soundController.BeginPettingAudio();
            if (tutorialSwipeValidated)
                SuccessfulPetGesture?.Invoke();
            try
            {
                PettingStarted?.Invoke();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
            }
            LogEvent("Petting started.");
            if (debugLogs)
                StartCoroutine(LogPetStateAfterTransition(petStateHash));
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
            EndPetting();
            ClearPointerTracking();
        }
    }

    private bool CanBeginPetting(out string failureReason)
    {
        ResolveReferences();

        if (!isActiveAndEnabled)
            failureReason = "PetInteraction is not active and enabled.";
        else if (catMovement == null)
            failureReason = "CatMovement reference is missing.";
        else if (sleepInteraction != null && sleepInteraction.IsSleeping)
            failureReason = "The cat is sleeping or in the sleep sequence.";
        else if (bowlInteraction != null && bowlInteraction.IsInteracting)
            failureReason = "BowlInteraction is active.";
        else if (hungerSystem != null && hungerSystem.IsEating)
            failureReason = "HungerSystem reports that the cat is eating.";
        else if (thirstSystem != null && thirstSystem.IsDrinking)
            failureReason = "ThirstSystem reports that the cat is drinking.";
        else if (CatActivity.Active != null)
            failureReason = "The cat is busy with a home activity.";
        else if (catMovement.IsJoystickInputActive)
            failureReason = "Joystick movement input is active.";
        else if (catMovement.IsPettingInputBlocked)
            failureReason = "Petting input is blocked by a scoped owner.";
        else if (catMovement.IsMovementPhysicallyLocked)
            failureReason = "CatMovement is physically locked by another interaction.";
        else
        {
            failureReason = null;
            return true;
        }

        return false;
    }

    public bool CanShowTutorialHint()
    {
        if (!CanBeginPetting(out _) || animator == null || !animator.isActiveAndEnabled)
            return false;

        int idleHash = Animator.StringToHash(BaseLayerPrefix + idleState);
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.fullPathHash == idleHash)
            return true;

        return animator.IsInTransition(0) &&
               animator.GetNextAnimatorStateInfo(0).fullPathHash == idleHash;
    }

    private IEnumerator LogPetStateAfterTransition(int expectedStateHash)
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0.1f, animationTransitionTime + 0.05f)
        );

        if (animator == null || !animator.isActiveAndEnabled)
        {
            LogWarning("Animator became unavailable before the Pet state check.");
            yield break;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        bool isPetState = current.fullPathHash == expectedStateHash;
        bool isInTransition = animator.IsInTransition(0);
        string nextState = "none";
        if (isInTransition)
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            nextState =
                $"hash {next.fullPathHash}, normalizedTime {next.normalizedTime:0.###}";
            isPetState |= next.fullPathHash == expectedStateHash;
        }

        LogEvent(
            $"Animator state check: current hash {current.fullPathHash}, " +
            $"normalizedTime {current.normalizedTime:0.###}, " +
            $"inTransition {isInTransition}, next {nextState}, Pet active {isPetState}."
        );
    }

    private bool ValidateAnimator(out string failureReason)
    {
        if (animator == null)
        {
            failureReason = "Animator reference is missing.";
            return false;
        }

        if (!animator.isActiveAndEnabled)
        {
            failureReason = $"Animator '{GetHierarchyPath(animator.transform)}' is not active and enabled.";
            return false;
        }

        if (!HasAnimatorState(petState))
        {
            failureReason = $"Animator state '{BaseLayerPrefix + petState}' is missing.";
            return false;
        }

        if (!HasAnimatorState(idleState))
        {
            failureReason = $"Animator state '{BaseLayerPrefix + idleState}' is missing.";
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == speedParameterHash &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                failureReason = null;
                return true;
            }
        }

        failureReason =
            $"Animator Float parameter '{speedAnimatorParameter}' is missing or has the wrong type.";
        return false;
    }

    private bool HasAnimatorState(string stateName)
    {
        return !string.IsNullOrWhiteSpace(stateName) &&
               animator != null &&
               animator.HasState(0, Animator.StringToHash(BaseLayerPrefix + stateName));
    }

    private bool TryGetBlockingUi(
        Vector2 screenPosition,
        int eventPointerId,
        out GameObject blockingUi)
    {
        blockingUi = null;
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        var eventData = new PointerEventData(eventSystem)
        {
            pointerId = eventPointerId,
            position = screenPosition
        };

        uiRaycastResults.Clear();
        eventSystem.RaycastAll(eventData, uiRaycastResults);
        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            if (TryFindInteractiveUi(uiRaycastResults[i].gameObject, out blockingUi))
                return true;
        }

        return false;
    }

    private static bool TryFindInteractiveUi(GameObject raycastObject, out GameObject control)
    {
        control = null;
        if (raycastObject == null)
            return false;

        Transform current = raycastObject.transform;
        while (current != null)
        {
            GameObject candidate = current.gameObject;
            Selectable selectable = candidate.GetComponent<Selectable>();
            if (selectable != null && selectable.isActiveAndEnabled)
            {
                control = candidate;
                return true;
            }

            Component[] components = candidate.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component is IPointerDownHandler ||
                    component is IPointerClickHandler ||
                    component is IBeginDragHandler ||
                    component is IDragHandler ||
                    component is IScrollHandler)
                {
                    control = candidate;
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }

    private bool TryFindCatCollider(Ray ray, out Collider catCollider)
    {
        catCollider = null;
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            raycastDistance,
            raycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
        {
            LogEvent("Physics raycast hit no collider.");
            return false;
        }

        System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        LogEvent(
            $"Physics raycast first hit '{GetHierarchyPath(hits[0].collider.transform)}' " +
            $"at {hits[0].distance:0.###}m ({hits.Length} total hit(s))."
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider candidate = hits[i].collider;
            if (candidate != null && candidate.transform.IsChildOf(transform))
            {
                catCollider = candidate;
                return true;
            }
        }

        return false;
    }

    private void ReleasePointer()
    {
        if (IsPetting)
            EndPetting();

        ClearPointerTracking();
    }

    private void CancelPointer()
    {
        if (IsPetting)
            EndPetting();

        ClearPointerTracking();
    }

    private void EndPetting()
    {
        if (!IsPetting)
        {
            if (activeSession == this)
                activeSession = null;
            return;
        }

        IsPetting = false;

        if (heartEffect != null)
            heartEffect.Stop();
        if (soundController != null)
            soundController.EndPettingAudio();

        try
        {
            if (animator != null && animator.isActiveAndEnabled && HasAnimatorState(idleState))
            {
                animator.SetFloat(speedParameterHash, 0f);
                animator.CrossFadeInFixedTime(
                    Animator.StringToHash(BaseLayerPrefix + idleState),
                    animationTransitionTime,
                    0
                );
            }
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception, this);
        }
        finally
        {
            if (catMovement != null)
                catMovement.SetMovementLocked(this, false);

            if (activeSession == this)
                activeSession = null;

            LogEvent("Petting ended; movement lock released.");
        }
    }

    private Vector2 CurrentPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (trackingTouch && Touchscreen.current != null)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        return Mouse.current != null ? Mouse.current.position.ReadValue() : pointerStartPosition;
#else
        return Input.mousePosition;
#endif
    }

    private void ClearPointerTracking()
    {
        pointerTracked = false;
        trackingTouch = false;
        pointerId = 0;
    }

    private void ResolveReferences()
    {
        if (catMovement == null)
            catMovement = GetComponent<CatMovement>();
        if (bowlInteraction == null)
            bowlInteraction = GetComponent<BowlInteraction>();
        if (sleepInteraction == null)
            sleepInteraction = GetComponent<SleepInteraction>();
        if (hungerSystem == null)
            hungerSystem = FindAnyObjectByType<HungerSystem>(FindObjectsInactive.Include);
        if (thirstSystem == null)
            thirstSystem = FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (heartEffect == null)
            heartEffect = GetComponentInChildren<PetHeartEffect>(true);
        if (soundController == null)
            soundController = GetComponent<PetSoundController>();
        if (tutorialHint == null)
            tutorialHint = FindAnyObjectByType<PetTutorialHint>(FindObjectsInactive.Include);
    }

    private static string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return "<missing>";

        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }

    private void LogEvent(string message)
    {
        if (debugLogs)
            Debug.Log("PetInteraction: " + message, this);
    }

    private void LogWarning(string message)
    {
        if (debugLogs)
            Debug.LogWarning("PetInteraction: " + message, this);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            CancelPointer();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            CancelPointer();
    }

    private void OnDisable()
    {
        CancelPointer();
    }

    private void OnDestroy()
    {
        CancelPointer();
    }
}
