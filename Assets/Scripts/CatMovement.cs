using UnityEngine;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[System.Flags]
public enum CatInputCategory
{
    None = 0,
    Movement = 1 << 0,
    Petting = 1 << 1,
    WorldActions = 1 << 2,
    All = Movement | Petting | WorldActions
}

[RequireComponent(typeof(CharacterController))]
public class CatMovement : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("Model Ayarları")]
    [Tooltip("Kedi hareket yönünün tersine bakıyorsa 180 yap.")]
    [SerializeField] private float modelForwardOffset = 0f;

    [Header("Animasyon Ayarları")]
    [SerializeField] private string speedParameterName = "Speed";
    [SerializeField] private float animationDampTime = 0.1f;

    [Header("Mobil Kontrol")]
    [SerializeField] private MobileJoystick mobileJoystick;

    private CharacterController characterController;
    private Animator animator;
    private Transform cameraTransform;

    private float verticalVelocity;
    private int speedParameterHash;
    private float hungerSpeedMultiplier = 1f;
    private float thirstSpeedMultiplier = 1f;
    private float energySpeedMultiplier = 1f;
    private bool movementLocked;
    private readonly HashSet<Object> movementLockOwners = new HashSet<Object>();
    private readonly Dictionary<Object, CatInputCategory> inputBlockOwners =
        new Dictionary<Object, CatInputCategory>();
    private float movementInputMagnitude;

    private float NeedsSpeedMultiplier =>
        Mathf.Min(
            Mathf.Min(hungerSpeedMultiplier, thirstSpeedMultiplier),
            energySpeedMultiplier
        );

    public bool IsMovementLocked
    {
        get
        {
            RemoveDestroyedInputBlockOwners();
            RemoveDestroyedMovementLockOwners();
            return IsMovementPhysicallyLocked || IsInputCategoryBlocked(CatInputCategory.Movement);
        }
    }
    public bool IsInputBlocked
    {
        get
        {
            RemoveDestroyedInputBlockOwners();
            return inputBlockOwners.Count > 0;
        }
    }
    public bool HasScopedInputBlock => IsInputBlocked;
    public bool IsMovementPhysicallyLocked
    {
        get
        {
            RemoveDestroyedMovementLockOwners();
            return movementLocked || movementLockOwners.Count > 0;
        }
    }
    public bool IsPettingInputBlocked => IsInputCategoryBlocked(CatInputCategory.Petting);
    public bool AreWorldActionsBlocked => IsInputCategoryBlocked(CatInputCategory.WorldActions);
    public bool IsJoystickInputActive =>
        mobileJoystick != null && mobileJoystick.Direction.sqrMagnitude > 0.001f;
    public bool IsMovementInputActive => movementInputMagnitude > 0.15f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        speedParameterHash = Animator.StringToHash(speedParameterName);
        ResolveSceneReferences();
    }

    public void ResolveSceneReferences()
    {
        if (mobileJoystick == null)
            mobileJoystick = FindAnyObjectByType<MobileJoystick>(FindObjectsInactive.Include);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // Order matters: the save system restores progression state first, and
        // only afterwards does ProgressionService subscribe to gameplay events.
        // Loading can therefore never trigger quest progress or rewards.
        CatHomeSaveSystem.Initialize(this);
        ProgressionService.Initialize(this);
    }

    private void Update()
    {
        RemoveDestroyedInputBlockOwners();
        RemoveDestroyedMovementLockOwners();
        if (IsMovementLocked)
        {
            movementInputMagnitude = 0f;
            UpdateAnimation(0f);
            return;
        }

        Vector2 input = ReadInput();
        movementInputMagnitude = input.magnitude;
        Vector3 moveDirection = GetCameraRelativeDirection(input);

        UpdateAnimation(input.magnitude);
        RotateCat(moveDirection);
        MoveCat(moveDirection);
    }

    private Vector2 ReadInput()
    {
        if (mobileJoystick != null &&
            mobileJoystick.Direction.sqrMagnitude > 0.001f)
        {
            return Vector2.ClampMagnitude(mobileJoystick.Direction, 1f);
        }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
            return Vector2.zero;

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            vertical += 1f;

        return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
#else
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
#endif
    }

    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        if (cameraTransform != null)
        {
            cameraForward = cameraTransform.forward;
            cameraRight = cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();
        }

        Vector3 direction = cameraForward * input.y + cameraRight * input.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private void UpdateAnimation(float movementAmount)
    {
        if (animator == null)
            return;

        float adjustedMovementAmount = movementAmount * NeedsSpeedMultiplier;

        animator.SetFloat(
            speedParameterHash,
            adjustedMovementAmount,
            animationDampTime,
            Time.deltaTime
        );
    }

    private void RotateCat(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction, Vector3.up) *
            Quaternion.Euler(0f, modelForwardOffset, 0f);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void MoveCat(Vector3 direction)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 horizontalVelocity =
            direction * moveSpeed * NeedsSpeedMultiplier;

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    public void SetHungerSpeedMultiplier(float multiplier)
    {
        hungerSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    public void SetThirstSpeedMultiplier(float multiplier)
    {
        thirstSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    public void SetEnergySpeedMultiplier(float multiplier)
    {
        energySpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
    }

    public void SetMovementLocked(bool locked)
    {
        if (movementLocked == locked)
            return;

        movementLocked = locked;

        if (!movementLocked)
            return;

        verticalVelocity = 0f;
        UpdateAnimation(0f);
    }

    public void SetMovementLocked(Object owner, bool locked)
    {
        if (owner == null)
            return;

        if (locked)
        {
            if (!movementLockOwners.Add(owner))
                return;

            if (mobileJoystick != null)
                mobileJoystick.CancelInput();

            verticalVelocity = 0f;
            UpdateAnimation(0f);
            return;
        }

        movementLockOwners.Remove(owner);
    }

    public void AcquireInputBlock(Object owner)
    {
        SetInputBlock(owner, CatInputCategory.All);
    }

    public void SetInputBlock(Object owner, CatInputCategory categories)
    {
        if (owner == null)
            return;

        if (categories == CatInputCategory.None)
        {
            ReleaseInputBlock(owner);
            return;
        }

        if (inputBlockOwners.TryGetValue(owner, out CatInputCategory existing) &&
            existing == categories)
            return;

        inputBlockOwners[owner] = categories;

        if ((categories & CatInputCategory.Movement) != 0 && mobileJoystick != null)
            mobileJoystick.CancelInput();

        if ((categories & CatInputCategory.Movement) != 0)
        {
            movementInputMagnitude = 0f;
            verticalVelocity = 0f;
            UpdateAnimation(0f);
        }
        LogInputBlockChange("acquired", owner);
    }

    public void ReleaseInputBlock(Object owner)
    {
        // Do not use Unity's overloaded null check here. A destroyed native owner can
        // still have the managed reference needed to remove its exact dictionary entry.
        if (!ReferenceEquals(owner, null) && inputBlockOwners.Remove(owner))
            LogInputBlockChange("released", owner);

        RemoveDestroyedInputBlockOwners();
    }

    private void RemoveDestroyedInputBlockOwners()
    {
        if (inputBlockOwners.Count == 0)
            return;

        var destroyed = new List<Object>();
        foreach (Object owner in inputBlockOwners.Keys)
            if (owner == null)
                destroyed.Add(owner);
        foreach (Object owner in destroyed)
            inputBlockOwners.Remove(owner);
    }

    public bool IsInputCategoryBlocked(CatInputCategory category)
    {
        RemoveDestroyedInputBlockOwners();
        foreach (CatInputCategory categories in inputBlockOwners.Values)
            if ((categories & category) != 0)
                return true;
        return false;
    }

    private void RemoveDestroyedMovementLockOwners()
    {
        movementLockOwners.RemoveWhere(owner => owner == null);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public void LogInputBlockOwners(string context)
    {
        RemoveDestroyedInputBlockOwners();
        var names = new List<string>();
        foreach (KeyValuePair<Object, CatInputCategory> entry in inputBlockOwners)
            names.Add($"{DescribeOwner(entry.Key)} [{entry.Value}]");
        Debug.Log(
            $"CatMovement input blockers ({context}): count={names.Count}; " +
            (names.Count == 0 ? "<none>" : string.Join(", ", names)),
            this
        );
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogInputBlockChange(string action, Object owner)
    {
        Debug.Log(
            $"CatMovement input block {action} by {DescribeOwner(owner)}; " +
            $"active blockers={inputBlockOwners.Count}.",
            this
        );
    }

    private static string DescribeOwner(Object owner)
    {
        if (ReferenceEquals(owner, null))
            return "<managed-null>";
        if (owner == null)
            return $"<destroyed:{owner.GetType().Name}>";
        return $"{owner.GetType().Name}('{owner.name}', id={owner.GetEntityId()})";
    }

    public void ApplySavedWorldPose(Vector3 position, Quaternion rotation)
    {
        bool controllerWasEnabled =
            characterController != null && characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        if (controllerWasEnabled && characterController != null)
            characterController.enabled = true;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            CatHomeSaveSystem.SaveForSuspension();
        else
            CatHomeSaveSystem.ResumeAfterSuspension();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            CatHomeSaveSystem.ResumeAfterSuspension();
        else
            CatHomeSaveSystem.SaveForSuspension();
    }

    private void OnApplicationQuit()
    {
        CatHomeSaveSystem.SaveNow();
    }
}
