using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SleepInteraction : MonoBehaviour
{
    [Header("Bed")]
    [Tooltip("Place this at the accessible side of the bed. Distance is measured horizontally to it.")]
    [SerializeField] private Transform bedInteractionPoint;
    [Tooltip("Place this on the mattress. Its blue Z axis is the direction the cat will face.")]
    [SerializeField] private Transform sleepPoint;

    [Header("Cat")]
    [SerializeField] private Transform catTransform;
    [SerializeField] private CatMovement catMovement;
    [SerializeField] private Animator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private EnergySystem energySystem;
    [SerializeField] private CatSpeechBubble speechBubble;
    [Tooltip("Keep this equal to CatMovement's model forward offset.")]
    [SerializeField] private float modelForwardOffset;

    [Header("Interaction")]
    [SerializeField, Min(0f)] private float interactionDistance = 0.45f;
    [SerializeField] private string sleepButtonText = "SLEEP";
    [SerializeField] private string wakeUpButtonText = "WAKE UP";

    [Header("Animator")]
    [SerializeField] private string speedAnimatorParameter = "Speed";
    [SerializeField] private string lieDownState = "LieDown";
    [SerializeField] private string sleepState = "Sleep";
    [SerializeField] private string idleState = "Idle";
    [SerializeField, Min(0f)] private float lieDownDuration = 1.35f;
    [SerializeField, Min(0f)] private float animationTransitionTime = 0.15f;

    private const string BaseLayerPrefix = "Base Layer.";

    private Coroutine sleepCoroutine;
    private bool sleepSequenceActive;
    private bool ownsMovementLock;
    private int speedParameterHash;
    private float satisfiedActionThreshold = 90f;
    private CatSleepZzzEffect sleepEffect;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool decisionLogged;
#endif

    /// <summary>
    /// Raised when the player starts a sleep sequence. Deliberately not raised
    /// by TryRestoreSleepingState, so loading a save never counts as sleeping.
    /// </summary>
    public event Action SleepStarted;

    public bool IsSleeping => sleepSequenceActive;
    public bool WantsActionButton => sleepSequenceActive || IsCatNearBed();
    public string CurrentButtonText => sleepSequenceActive ? wakeUpButtonText : sleepButtonText;
    public Transform TutorialBedTarget =>
        sleepPoint != null && sleepPoint.parent != null ? sleepPoint.parent : bedInteractionPoint;

    private void Reset()
    {
        catTransform = transform;
        catMovement = GetComponent<CatMovement>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>(true);
    }

    private void Awake()
    {
        ResolveCatReferences();
        speedParameterHash = Animator.StringToHash(speedAnimatorParameter);
        satisfiedActionThreshold = GameBalanceConfig.GetSatisfiedActionThreshold();
        speechBubble = speechBubble != null ? speechBubble : GetComponent<CatSpeechBubble>() ?? gameObject.AddComponent<CatSpeechBubble>();
        sleepEffect = GetComponent<CatSleepZzzEffect>() ?? gameObject.AddComponent<CatSleepZzzEffect>();
    }

    public bool TryHandleActionButton()
    {
        if (sleepSequenceActive)
        {
            LogDecisionOnce("WakeUp");
            WakeUp();
            return true;
        }

        if (catMovement != null && catMovement.AreWorldActionsBlocked)
            return false;

        if (!IsCatNearBed())
            return false;

        if (catMovement.IsMovementLocked)
            return false;

        ResolveEnergySystem();
        if (energySystem == null)
        {
            Debug.LogError("SleepInteraction: The active EnergySystem could not be resolved; sleep was not started.", this);
            return false;
        }

        if (IsSatisfiedEnergy(energySystem.CurrentEnergy, satisfiedActionThreshold))
        {
            LogDecisionOnce("SatisfiedRejected");
            speechBubble?.Show("I'm not sleepy right now!");
            return true;
        }

        if (!ValidateSetup())
            return false;

        LogDecisionOnce("BeginSleep");
        BeginSleep();
        return true;
    }

    public static bool IsSatisfiedEnergy(float energy, float threshold) => energy >= threshold;

    public bool TryRestoreSleepingState(out string failureReason)
    {
        failureReason = null;

        if (sleepSequenceActive)
            return true;

        ResolveCatReferences();
        if (!CanRestoreSleepingState(out failureReason))
        {
            EnsureAwakeFallback();
            return false;
        }

        try
        {
            sleepSequenceActive = true;
            catMovement.SetMovementLocked(this, true);
            ownsMovementLock = true;
            animator.SetFloat(speedParameterHash, 0f);
            MoveCatToSleepPoint();

            int sleepStateHash =
                Animator.StringToHash(BaseLayerPrefix + sleepState);
            animator.Play(sleepStateHash, 0, 0f);
            animator.Update(0f);
            sleepEffect?.Begin();
            return true;
        }
        catch (Exception exception)
        {
            failureReason = exception.Message;
            EnsureAwakeFallback();
            return false;
        }
    }

    private void BeginSleep()
    {
        sleepSequenceActive = true;
        catMovement.SetMovementLocked(this, true);
        ownsMovementLock = true;
        animator.SetFloat(speedParameterHash, 0f);
        MoveCatToSleepPoint();

        animator.CrossFadeInFixedTime(
            BaseLayerPrefix + lieDownState,
            animationTransitionTime,
            0
        );

        sleepCoroutine = StartCoroutine(EnterSleepAfterLieDown());

        try
        {
            SleepStarted?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    private IEnumerator EnterSleepAfterLieDown()
    {
        yield return new WaitForSeconds(lieDownDuration);

        if (!sleepSequenceActive || animator == null)
            yield break;

        if (!IsAnimatorInOrTransitioningTo(sleepState))
        {
            animator.CrossFadeInFixedTime(
                BaseLayerPrefix + sleepState,
                animationTransitionTime,
                0
            );
        }

        sleepEffect?.Begin();

        sleepCoroutine = null;
    }

    private void WakeUp()
    {
        if (sleepCoroutine != null)
        {
            StopCoroutine(sleepCoroutine);
            sleepCoroutine = null;
        }

        sleepSequenceActive = false;
        sleepEffect?.Stop();

        // Kediyi yatağın üzerinden, yatağın önündeki noktaya taşı.
        MoveCatToBedInteractionPoint();

        if (animator != null && HasAnimatorState(idleState))
        {
            animator.CrossFadeInFixedTime(
                BaseLayerPrefix + idleState,
                animationTransitionTime,
                0
            );
        }

        if (ownsMovementLock && catMovement != null)
            catMovement.SetMovementLocked(this, false);

        ownsMovementLock = false;
    }

    private void MoveCatToBedInteractionPoint()
    {
        if (catTransform == null || bedInteractionPoint == null)
            return;

        bool controllerWasEnabled =
            characterController != null && characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        try
        {
            catTransform.position = bedInteractionPoint.position;

            Vector3 forward = bedInteractionPoint.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
            {
                catTransform.rotation =
                    Quaternion.LookRotation(forward.normalized, Vector3.up) *
                    Quaternion.Euler(0f, modelForwardOffset, 0f);
            }
        }
        finally
        {
            if (controllerWasEnabled && characterController != null)
                characterController.enabled = true;
        }
    }

    private void MoveCatToSleepPoint()
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (controllerWasEnabled)
            characterController.enabled = false;

        try
        {
            catTransform.position = sleepPoint.position;

            Vector3 forward = sleepPoint.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                catTransform.rotation =
                    Quaternion.LookRotation(forward.normalized, Vector3.up) *
                    Quaternion.Euler(0f, modelForwardOffset, 0f);
            }
        }
        finally
        {
            if (controllerWasEnabled && characterController != null)
                characterController.enabled = true;
        }
    }

    private bool IsCatNearBed()
    {
        if (!isActiveAndEnabled || catTransform == null || bedInteractionPoint == null)
            return false;

        Vector3 difference = bedInteractionPoint.position - catTransform.position;
        difference.y = 0f;
        return difference.sqrMagnitude <= interactionDistance * interactionDistance;
    }

    private bool ValidateSetup()
    {
        ResolveCatReferences();

        if (catTransform == null || catMovement == null || animator == null)
        {
            Debug.LogError(
                "SleepInteraction: Cat Transform, Cat Movement and Animator references are required.",
                this
            );
            return false;
        }

        if (bedInteractionPoint == null || sleepPoint == null)
        {
            Debug.LogError(
                "SleepInteraction: Bed Interaction Point and Sleep Point references are required.",
                this
            );
            return false;
        }

        if (!HasAnimatorState(lieDownState) ||
            !HasAnimatorState(sleepState) ||
            !HasAnimatorState(idleState))
        {
            Debug.LogError(
                $"SleepInteraction: Animator must contain '{lieDownState}', '{sleepState}' and '{idleState}' states on Base Layer.",
                animator
            );
            return false;
        }

        if (!HasFloatParameter(speedParameterHash))
        {
            Debug.LogError(
                $"SleepInteraction: Animator needs a Float parameter named '{speedAnimatorParameter}'.",
                animator
            );
            return false;
        }

        return true;
    }

    private bool CanRestoreSleepingState(out string failureReason)
    {
        if (!isActiveAndEnabled)
        {
            failureReason = "SleepInteraction is disabled.";
            return false;
        }

        if (catTransform == null || catMovement == null || animator == null ||
            characterController == null)
        {
            failureReason =
                "Cat Transform, Cat Movement, Animator or Character Controller is missing.";
            return false;
        }

        if (bedInteractionPoint == null || sleepPoint == null)
        {
            failureReason = "Bed Interaction Point or Sleep Point is missing.";
            return false;
        }

        if (!animator.isActiveAndEnabled)
        {
            failureReason = "The cat Animator is disabled.";
            return false;
        }

        if (!HasAnimatorState(sleepState) || !HasAnimatorState(idleState))
        {
            failureReason =
                $"Animator states '{sleepState}' and '{idleState}' are required.";
            return false;
        }

        if (!HasFloatParameter(speedParameterHash))
        {
            failureReason =
                $"Animator Float parameter '{speedAnimatorParameter}' is required.";
            return false;
        }

        failureReason = null;
        return true;
    }

    private void EnsureAwakeFallback()
    {
        if (sleepCoroutine != null)
        {
            StopCoroutine(sleepCoroutine);
            sleepCoroutine = null;
        }

        sleepSequenceActive = false;
        sleepEffect?.Stop();
        ownsMovementLock = false;

        if (catMovement != null)
            catMovement.SetMovementLocked(this, false);

        if (animator != null && animator.isActiveAndEnabled &&
            HasAnimatorState(idleState))
        {
            animator.Play(
                Animator.StringToHash(BaseLayerPrefix + idleState),
                0,
                0f
            );
            animator.Update(0f);
        }
    }

    private void ResolveCatReferences()
    {
        if (catTransform == null)
            catTransform = transform;

        if (catMovement == null)
            catMovement = catTransform.GetComponent<CatMovement>();

        if (characterController == null)
            characterController = catTransform.GetComponent<CharacterController>();

        if (animator == null)
            animator = catTransform.GetComponentInChildren<Animator>(true);

        ResolveEnergySystem();
    }

    private void ResolveEnergySystem()
    {
        if (energySystem != null)
            return;

        if (catTransform != null)
            energySystem = catTransform.GetComponent<EnergySystem>();

        if (energySystem != null)
            return;

        EnergySystem[] candidates =
            FindObjectsByType<EnergySystem>(FindObjectsInactive.Include);
        EnergySystem fallback = null;
        for (int i = 0; i < candidates.Length; i++)
        {
            EnergySystem candidate = candidates[i];
            if (candidate == null)
                continue;

            fallback ??= candidate;
            if (catMovement != null && candidate.CatMovement == catMovement)
            {
                energySystem = candidate;
                return;
            }
        }

        // A scene with one energy HUD has an unambiguous active source even when
        // an older scene has not serialized its CatMovement link yet.
        if (candidates.Length == 1)
            energySystem = fallback;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogDecisionOnce(string decision)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (decisionLogged)
            return;

        decisionLogged = true;
        string energy = energySystem != null ? energySystem.CurrentEnergy.ToString("0.###") : "missing";
        Debug.Log(
            $"SleepInteraction decision: energy={energy}, threshold={satisfiedActionThreshold:0.###}, " +
            $"instance={name}#{GetEntityId()}, decision={decision}",
            this);
#endif
    }

    private bool HasAnimatorState(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int stateHash = Animator.StringToHash(BaseLayerPrefix + stateName);
        return animator.HasState(0, stateHash);
    }

    private bool HasFloatParameter(int parameterHash)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash &&
                parameter.type == AnimatorControllerParameterType.Float)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAnimatorInOrTransitioningTo(string stateName)
    {
        int stateHash = Animator.StringToHash(BaseLayerPrefix + stateName);
        if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == stateHash)
            return true;

        return animator.IsInTransition(0) &&
               animator.GetNextAnimatorStateInfo(0).fullPathHash == stateHash;
    }

    private void OnDisable()
    {
        if (sleepCoroutine != null)
        {
            StopCoroutine(sleepCoroutine);
            sleepCoroutine = null;
        }

        sleepSequenceActive = false;
        sleepEffect?.Stop();

        if (ownsMovementLock && catMovement != null)
            catMovement.SetMovementLocked(this, false);

        ownsMovementLock = false;
    }
}
