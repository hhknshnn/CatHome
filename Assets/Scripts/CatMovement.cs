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
    [SerializeField] private float moveSpeed = HomeRunSpeed;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("Model Ayarları")]
    [Tooltip("Kedi hareket yönünün tersine bakıyorsa 180 yap.")]
    [SerializeField] private float modelForwardOffset = 0f;

    [Header("Animasyon Ayarları")]
    [SerializeField] private string speedParameterName = "Speed";

    [Header("Mobil Kontrol")]
    [SerializeField] private MobileJoystick mobileJoystick;

    [Header("Oda Sınırları")]
    [Tooltip("Kedinin görünen modelini duvardan ayıran ek dünya boşluğu.")]
    [SerializeField, Min(0f)] private float roomEdgeClearance = 0.03f;

    private CharacterController characterController;
    private Animator animator;
    private Transform cameraTransform;
    private HomeRoomBoundary roomBoundary;
    private float physicalFootprintRadius;

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
    private EnergySystem energySystem;
    private bool running;
    private bool hasLocomotionRate;
    private float currentGroundSpeed;
    private float requestedGroundSpeed;
    private CatHomeLocomotionCatalog.Entry locomotionProfile;
    private static readonly int LocomotionRateHash=Animator.StringToHash("LocomotionRate");
    public float GroundSpeed=>currentGroundSpeed;
    public bool IsRunning=>CatHomeLocomotionCatalog.RunBlendForSpeed(currentGroundSpeed)>.5f && !IsMovementLocked;

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
    public bool IsMovementInputActive => movementInputMagnitude > 0.08f;
    public bool IsIdle => !IsMovementLocked && !IsMovementInputActive;

    /// <summary>
    /// Soft-turns the cat toward a world point while it is standing still.
    /// Used by courtyard birds so they can steal attention without stealing
    /// the movement lock from care or activities.
    /// </summary>
    public void SuggestLookDirection(Vector3 worldPoint)
    {
        if (!IsIdle)
            return;

        Vector3 flat = worldPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.04f)
            return;

        Quaternion look = Quaternion.LookRotation(flat.normalized, Vector3.up) *
                          Quaternion.Euler(0f, modelForwardOffset, 0f);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            look,
            Time.deltaTime * 4f);
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        // Keep sub-millimetre analog steps even at high frame rates.
        characterController.minMoveDistance = 0f;
        animator = GetComponentInChildren<Animator>();
        speedParameterHash = Animator.StringToHash(speedParameterName);
        ResolveSceneReferences();
        physicalFootprintRadius = ResolvePhysicalFootprintRadius();
        ResolveLocomotionRate();
    }

    public void RebindAnimator(Animator replacement)
    {
        if (replacement == null)
            return;
        animator = replacement;
        speedParameterHash = Animator.StringToHash(speedParameterName);
        physicalFootprintRadius = ResolvePhysicalFootprintRadius();
        ResolveLocomotionRate();
    }

    private void ResolveLocomotionRate()
    {
        hasLocomotionRate=false;
        if(animator!=null)foreach(var p in animator.parameters)if(p.nameHash==LocomotionRateHash)hasLocomotionRate=true;
        var tag = animator != null ? animator.GetComponentInParent<CatBreedVisualTag>() : null;
        var catalog = Resources.Load<CatHomeLocomotionCatalog>(CatHomeLocomotionCatalog.ResourceName);
        locomotionProfile = catalog != null ? catalog.Find(tag != null ? tag.BreedId : CatBreedCatalog.DefaultBreedId) : null;
    }

    public void ResolveSceneReferences()
    {
        if (mobileJoystick == null)
            mobileJoystick = FindAnyObjectByType<MobileJoystick>(FindObjectsInactive.Include);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (roomBoundary == null || roomBoundary.gameObject.scene != gameObject.scene)
            roomBoundary = HomeRoomBoundary.FindFor(gameObject.scene);
        if(energySystem==null)energySystem=FindAnyObjectByType<EnergySystem>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        // Order matters: the save system restores progression state first, and
        // only afterwards does ProgressionService subscribe to gameplay events.
        // Loading can therefore never trigger quest progress or rewards.
        CatHomeSaveSystem.Initialize(this);
        ProgressionService.Initialize(this);
        CatIdleBehavior.EnsureOn(this);
        CatVoice.EnsureOn(this);
        if(GetComponent<CatCommandActivity>()==null)gameObject.AddComponent<CatCommandActivity>();
    }

    private void Update()
    {
        RemoveDestroyedInputBlockOwners();
        RemoveDestroyedMovementLockOwners();
        if (IsMovementLocked || characterController==null || !characterController.enabled)
        {
            movementInputMagnitude = 0f;
            currentGroundSpeed=0f;running=false;
            UpdateAnimation(0f);
            return;
        }

        Vector2 input = ReadInput();
        movementInputMagnitude = input.magnitude;
        Vector3 moveDirection = GetCameraRelativeDirection(input);

        RotateCat(moveDirection);
        Vector3 previous=transform.position;
        MoveCat(moveDirection);
        Vector3 distance=transform.position-previous;distance.y=0;
        // Controller depenetration can move an idle cat a few millimetres. It
        // is not a walking step; never animate it as intentional locomotion.
        currentGroundSpeed=Mathf.Min(requestedGroundSpeed,
            distance.magnitude/Mathf.Max(Time.deltaTime,.0001f));
        UpdateAnimation(currentGroundSpeed);
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

        if (movementAmount < .025f)
        {
            // Reset only locomotion parameters, never Animator.speed or the
            // current state: care, commands and furniture own their own poses.
            animator.SetFloat(speedParameterHash, 0f);
            if (hasLocomotionRate) animator.SetFloat(LocomotionRateHash, 1f);
            return;
        }

        float runBlend = CatHomeLocomotionCatalog.RunBlendForSpeed(movementAmount);
        float rate = locomotionProfile != null
            ? locomotionProfile.PlaybackRate(movementAmount, runBlend,
                transform.TransformVector(Vector3.forward).magnitude)
            // Legacy/custom controllers without a measured breed remain usable.
            // Shipped breeds are required to have a measured profile by validation.
            : movementAmount / Mathf.Lerp(.65f, 1.8f, runBlend);
        if (hasLocomotionRate) animator.SetFloat(LocomotionRateHash, rate);
        // Damping Speed independently of cadence blends in Idle while the body
        // already travels at full speed. Both now describe the same frame.
        animator.SetFloat(speedParameterHash, Mathf.Lerp(.35f, 1f, runBlend));
    }

    private void RotateCat(Vector3 direction)
    {
        if (direction.magnitude <= .08f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction, Vector3.up) *
            Quaternion.Euler(0f, modelForwardOffset, 0f);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            TurnDegreesPerSecond(IsRunning) * Time.deltaTime
        );
    }

    private void MoveCat(Vector3 direction)
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        float strength=direction.magnitude;
        bool hasEnergy=energySystem==null || energySystem.CurrentEnergy>1f;
        running=hasEnergy && (running?strength>.76f:strength>.88f);
        float targetSpeed=GroundSpeedForInput(strength,running,moveSpeed)*NeedsSpeedMultiplier;
        Vector3 forward=Quaternion.Euler(0,-modelForwardOffset,0)*transform.forward;
        forward.y=0;forward.Normalize();
        // Pivot briefly for a reverse command, then walk a curved path with the body facing its travel.
        bool aligned=direction.sqrMagnitude>.0001f && Vector3.Dot(forward,direction.normalized)>.5f;
        requestedGroundSpeed = aligned ? targetSpeed : 0f;
        Vector3 horizontalVelocity=aligned?forward*targetSpeed:Vector3.zero;

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        Vector3 movement = velocity * Time.deltaTime;
        if (roomBoundary != null)
        {
            Vector3 target = roomBoundary.ClampPosition(
                transform.position + new Vector3(movement.x, 0f, movement.z),
                physicalFootprintRadius + roomEdgeClearance);
            movement.x = target.x - transform.position.x;
            movement.z = target.z - transform.position.z;
        }

        characterController.Move(movement);
    }

    public const float NormalWalkSpeed=.65f;
    public const float HomeRunSpeed=1.5f;
    public static float TurnDegreesPerSecond(bool run)=>run?320f:220f;
    public static float GroundSpeedForInput(float strength,bool run,float maximum=HomeRunSpeed)
    {
        strength=Mathf.Clamp01(strength);
        if(strength<=.08f)return 0f;
        // Existing scenes store the old 2.2 value. Apply the home ceiling here
        // so updating locomotion never requires rewriting eight room scenes.
        if(run)return Mathf.Lerp(.85f,Mathf.Clamp(maximum,.85f,HomeRunSpeed),Mathf.InverseLerp(.76f,1f,strength));
        return Mathf.Lerp(NormalWalkSpeed,.85f,Mathf.InverseLerp(.08f,.88f,strength));
    }

    private float ResolvePhysicalFootprintRadius()
    {
        // Renderer bounds include the tail and change with breed/pose. Using
        // that diagonal as a wall margin pushed large breeds INTO nearby
        // furniture as soon as an activity released its movement lock.
        if (characterController == null) return .25f;
        Vector3 scale = transform.lossyScale;
        return characterController.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
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
        // The home scene stays loaded underneath both mini games. Their keys
        // must not also move or pet the hidden home cat, including launch frames.
        if (category != CatInputCategory.None && HomeUiFlow.IsMiniGameVisible)
            return true;
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

        if (roomBoundary == null)
            roomBoundary = HomeRoomBoundary.FindFor(gameObject.scene);
        if (physicalFootprintRadius <= 0f)
            physicalFootprintRadius = ResolvePhysicalFootprintRadius();
        if (roomBoundary != null)
            position = roomBoundary.ClampPosition(
                position,
                physicalFootprintRadius + roomEdgeClearance);

        position=CatBedObstacle.ResolveSavedPosition(this,position);
        position=CatCareStationObstacle.ResolveSavedPosition(this,position);
        position=RoomDoorObstacle.ResolveSavedPosition(this,position);
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
