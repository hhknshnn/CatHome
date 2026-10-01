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
    private bool settledOnBed;
    private bool waking;
    private bool controllerHeld;
    private bool controllerWasEnabled;
    private bool hasAcceptedFloor;
    private CatActivityStart acceptedFloor;
    private CatActivityAnimation poseDriver;
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
    public bool IsSettledOnBed => sleepSequenceActive && settledOnBed;
    // Entry and wake animations keep action ownership, but are not sleep.
    // Persist their last accepted floor pose instead of an airborne root.
    public bool TryGetTransitionSavePose(out Vector3 position, out Quaternion rotation)
    {
        position = catTransform != null ? catTransform.position : transform.position;
        rotation = catTransform != null ? catTransform.rotation : transform.rotation;
        if (!sleepSequenceActive || settledOnBed) return false;
        if (hasAcceptedFloor)
        {
            position = acceptedFloor.Position;
            rotation = acceptedFloor.Rotation;
            return true;
        }
        if (bedInteractionPoint == null) return false;
        position = bedInteractionPoint.position;
        rotation = bedInteractionPoint.rotation * Quaternion.Euler(0, modelForwardOffset, 0);
        return true;
    }
    public Transform SleepSurface=>sleepPoint;
    public bool WantsActionButton
    {
        get
        {
            if (sleepSequenceActive) return settledOnBed && !waking;
            ResolveEnergySystem();
            return energySystem != null && !IsSatisfiedEnergy(energySystem.CurrentEnergy, satisfiedActionThreshold) &&
                animator != null && !CatActionState.IsBusy(catMovement) && IsCatNearBed();
        }
    }
    public string CurrentButtonText => sleepSequenceActive ? wakeUpButtonText : sleepButtonText;
    public Transform TutorialBedTarget =>
        sleepPoint != null && sleepPoint.parent != null ? sleepPoint.parent : bedInteractionPoint;

    public void RebindAnimator(Animator replacement)
    {
        if (replacement != null)
            animator = replacement;
    }

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
        if(GetComponent<CatSleepContactAlignment>()==null)gameObject.AddComponent<CatSleepContactAlignment>();
        satisfiedActionThreshold = GameBalanceConfig.GetSatisfiedActionThreshold();
        speechBubble = speechBubble != null ? speechBubble : GetComponent<CatSpeechBubble>() ?? gameObject.AddComponent<CatSpeechBubble>();
        sleepEffect = GetComponent<CatSleepZzzEffect>() ?? gameObject.AddComponent<CatSleepZzzEffect>();
    }

    public bool TryHandleActionButton()
    {
        if (!isActiveAndEnabled || catMovement == null || catMovement.AreWorldActionsBlocked || HomeUiFlow.IsHomeControlBlocked)
            return false;
        if (sleepSequenceActive)
        {
            LogDecisionOnce("WakeUp");
            WakeUp();
            return true;
        }

        if (catMovement != null && catMovement.AreWorldActionsBlocked)
            return false;

        if (!TryPrepareSleepStart(out CatActivityStart start))
            return false;

        if (CatActionState.IsBusy(catMovement))
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
            speechBubble?.Show(GameContentCopy.Text("Şu an uykum yok!","I'm not sleepy right now!"));
            return true;
        }

        if (!ValidateSetup())
            return false;

        LogDecisionOnce("BeginSleep");
        BeginSleep(start);
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
            HoldController();
            animator.SetFloat(speedParameterHash, 0f);
            MoveCatToSleepPoint();
            settledOnBed = true;
            waking = false;
            hasAcceptedFloor = false;

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

    private void BeginSleep(CatActivityStart start)
    {
        acceptedFloor = start;
        hasAcceptedFloor = true;
        sleepSequenceActive = true;
        settledOnBed = false;
        waking = false;
        catMovement.SetMovementLocked(this, true);
        ownsMovementLock = true;
        HoldController();
        animator.SetFloat(speedParameterHash, 0f);
        poseDriver = GetComponent<CatActivityAnimation>() ?? gameObject.AddComponent<CatActivityAnimation>();
        if (!poseDriver.BeginExternal(this))
        {
            CancelForTransition();
            return;
        }
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
        Quaternion resting = RestingRotation();
        yield return CatJumpMotion.Play(catMovement, acceptedFloor.Position, sleepPoint.position,
            acceptedFloor.Rotation, resting, true, preserveLaunchHeading: true);
        poseDriver.EndExternal(this);
        settledOnBed = true;
        animator.CrossFadeInFixedTime(BaseLayerPrefix + lieDownState, animationTransitionTime, 0);
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
        if (waking) return;
        if (sleepCoroutine != null)
        {
            StopCoroutine(sleepCoroutine);
            sleepCoroutine = null;
        }

        sleepEffect?.Stop();
        if (!settledOnBed)
        {
            CancelForTransition();
            return;
        }
        waking = true;
        settledOnBed = false;
        poseDriver = GetComponent<CatActivityAnimation>() ?? gameObject.AddComponent<CatActivityAnimation>();
        if (!poseDriver.BeginExternal(this)) { CancelForTransition(); return; }
        sleepCoroutine = StartCoroutine(WakeAndLeaveBed());
    }

    private IEnumerator WakeAndLeaveBed()
    {
        // Keep the source sleep-to-sit and sit-to-stand motion on the mattress
        // before the genuine downward jump releases the movement controller.
        yield return WakePose(CatActivityPose.TowelWake, .85f, true);
        yield return WakePose(CatActivityPose.StandUp, .60f, false);
        Vector3 floor = hasAcceptedFloor ? acceptedFloor.Position : bedInteractionPoint.position;
        Vector3 direction = floor - catTransform.position;
        direction.y = 0f;
        Quaternion outward = direction.sqrMagnitude > .0001f
            ? Quaternion.LookRotation(direction) : catTransform.rotation;
        yield return CatJumpMotion.Play(catMovement, catTransform.position, floor,
            outward, outward, false);
        poseDriver.EndExternal(this);
        sleepCoroutine = null;
        sleepSequenceActive = false;
        waking = false;
        hasAcceptedFloor = false;
        ReleaseController();

        if (ownsMovementLock && catMovement != null)
            catMovement.SetMovementLocked(this, false);

        ownsMovementLock = false;
    }

    private IEnumerator WakePose(CatActivityPose pose, float seconds, bool reverse)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            if (Time.timeScale <= 0f) { yield return null; continue; }
            float phase = Mathf.Clamp01(elapsed / seconds);
            poseDriver.SetTimedPose(pose, reverse ? 1f - phase : phase, sleepPoint);
            yield return null;
            elapsed += Time.deltaTime;
        }
        poseDriver.SetTimedPose(pose, reverse ? 0f : 1f, sleepPoint);
    }

    private void HoldController()
    {
        if (controllerHeld) return;
        controllerHeld = true;
        controllerWasEnabled = characterController != null && characterController.enabled;
        if (controllerWasEnabled) characterController.enabled = false;
    }

    private void ReleaseController()
    {
        if (!controllerHeld) return;
        controllerHeld = false;
        if (characterController != null) characterController.enabled = controllerWasEnabled;
    }

    public void ForceAwakeForNewGame()
    {
        ResolveCatReferences();
        if (IsSleeping) CancelForTransition();
        else EnsureAwakeFallback();
    }

    public void CancelForTransition()
    {
        // An idle bed must not reset a pose or controller owned by another action.
        if (!sleepSequenceActive && !ownsMovementLock && sleepCoroutine == null) return;
        if (sleepCoroutine != null) StopCoroutine(sleepCoroutine);
        sleepCoroutine = null;
        poseDriver?.EndExternal(this);
        if (catTransform != null)
        {
            if (hasAcceptedFloor)
                catTransform.SetPositionAndRotation(acceptedFloor.Position, acceptedFloor.Rotation);
            else if (settledOnBed || waking)
                MoveCatToBedInteractionPoint();
        }
        EnsureAwakeFallback();
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
                catTransform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up) *
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
                catTransform.rotation = RestingRotation();
            }
        }
        finally
        {
            if (controllerWasEnabled && characterController != null)
                characterController.enabled = true;
        }
    }

    private Quaternion RestingRotation()
    {
        Quaternion authored = sleepPoint.rotation * Quaternion.Euler(0f, modelForwardOffset, 0f);
        return CatActivityFacing.AlongAxis(catMovement, sleepPoint.position, authored);
    }

    private bool IsCatNearBed() => TryPrepareSleepStart(out _);

    private bool TryPrepareSleepStart(out CatActivityStart start)
    {
        start = default;
        if (!isActiveAndEnabled || !HasVisibleBed())
            return false;
        return CatActivityStartResolver.GroundLaunch(null, catMovement,
            bedInteractionPoint.position, sleepPoint.position, out start);
    }

    private bool HasVisibleBed()
    {
        return CareInteractionTarget.IsInRoom(sleepPoint, catTransform) &&
            CareInteractionTarget.IsInRoom(bedInteractionPoint, catTransform) &&
            CareInteractionTarget.IsVisibleInRoom(TutorialBedTarget, catTransform);
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
            !HasAnimatorState(idleState) || !HasAnimatorState("NativeJump") ||
            !HasAnimatorState("TowelWake") || !HasAnimatorState("CompanionStandUp"))
        {
            Debug.LogError(
                $"SleepInteraction: Animator needs '{lieDownState}', '{sleepState}', '{idleState}', NativeJump, TowelWake and CompanionStandUp on Base Layer.",
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

        if (!HasVisibleBed())
        {
            failureReason = "No visible bed is available in the cat's room.";
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
        settledOnBed = false;
        waking = false;
        hasAcceptedFloor = false;
        sleepEffect?.Stop();
        ownsMovementLock = false;
        poseDriver?.EndExternal(this);
        ReleaseController();

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
        CancelForTransition();
    }
}
