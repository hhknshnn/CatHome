using UnityEngine;

/// <summary>
/// The Cat Catch hunter. The cat runs on a CharacterController with the same
/// Idle/Run blend the home scene uses, so it is never sliding or frozen, and a
/// pounce is a short committed lunge that ends in a single strike.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatCatchPlayer : MonoBehaviour
{
    private const float PounceClipLength = 0.72f;
    private const float PounceClipLaunch = 0.23f;
    private const string IdleState = "Idle";
    private const string PounceState = "ActivityPounce";
    private const string PawSwatState = "ActivityPawSwat";

    private enum Phase
    {
        Idle,
        Chase,
        Pounce,
        Recover
    }

    [SerializeField] private Animator animator;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField, Min(0.5f)] private float moveSpeed = CatchHuntRules.CatRunSpeed;
    [SerializeField, Min(1f)] private float rotationSpeed = 16f;
    [SerializeField, Min(0.02f)] private float animationDampTime = 0.1f;
    [SerializeField, Min(0.05f)] private float pounceHeight = 0.45f;
    [SerializeField] private string speedParameterName = "Speed";

    private Phase phase = Phase.Idle;
    private bool inputEnabled;
    private int speedParameterHash;
    private CatCatchMouse prey;
    private float chaseSeconds;
    private Vector3 movePoint;
    private bool hasMovePoint;
    private float phaseTimer;
    private Vector3 pounceFrom;
    private Vector3 pounceTo;
    private float pounceTravel;
    private float floorY;
    private bool floorReady;
    private Transform arena;
    private Collider floorCollider;
    private bool pounceStarted;
    private bool strikePending;
    private Vector3 strikePoint;
    private CatCatchMouse strikePrey;

    public Vector3 Position => transform.position;
    public bool IsPouncing => phase == Phase.Pounce;
    public bool IsBusy => phase == Phase.Pounce || phase == Phase.Recover;
    public CatCatchMouse Prey => prey;

    public void EditorBind(Animator catAnimator, Camera camera)
    {
        RebindAnimator(catAnimator);
        BindCamera(camera);
    }

    public void RebindAnimator(Animator replacement)
    {
        if (replacement == null)
            return;
        animator = replacement;
        animator.applyRootMotion = false;
        speedParameterHash = Animator.StringToHash(speedParameterName);
    }

    public void BindCamera(Camera camera)
    {
        if (camera != null)
            gameplayCamera = camera;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        pounceStarted = false;
        if (enabled)
            return;
        prey = null;
        hasMovePoint = false;
        strikePending = false;
        phase = Phase.Idle;
        SetAnimatorSpeed(1f);
    }

    public bool ConsumePounceStarted()
    {
        bool started = pounceStarted;
        pounceStarted = false;
        return started;
    }

    /// <summary>
    /// Reports the landing of a pounce. This is the only moment a catch may be
    /// resolved, which keeps the cat from absorbing mice it merely stands near.
    /// </summary>
    public bool TryConsumeStrike(out Vector3 point, out CatCatchMouse target)
    {
        point = strikePoint;
        target = strikePrey;
        if (!strikePending)
            return false;
        strikePending = false;
        strikePrey = null;
        return true;
    }

    public void ChasePrey(CatCatchMouse target)
    {
        if (!inputEnabled || IsBusy || target == null || !target.IsActive)
            return;
        prey = target;
        chaseSeconds = 0f;
        hasMovePoint = false;
        phase = Phase.Chase;
    }

    public void MoveTo(Vector3 worldPoint)
    {
        if (!inputEnabled || IsBusy)
            return;
        prey = null;
        movePoint = ClampToArena(worldPoint);
        movePoint.y = floorY;
        hasMovePoint = true;
        phase = Phase.Chase;
    }

    public void PlayCatchReaction()
    {
        CrossFade(PawSwatState, 0.06f);
    }

    public bool TryResolveScreenPoint(Vector2 screenPosition, out Vector3 world)
    {
        world = default;
        Camera camera = ResolveCamera();
        return camera != null && TryWorldFromScreen(camera, screenPosition, out world);
    }

    public void SnapTo(Vector3 position)
    {
        CacheArena();
        DisablePhysicsBody();
        Vector3 clamped = ClampToArena(position);
        floorY = MeasureStandHeight(clamped);
        clamped.y = floorY;
        transform.position = clamped;
        floorReady = true;
        phase = Phase.Idle;
        phaseTimer = 0f;
        prey = null;
        hasMovePoint = false;
        pounceStarted = false;
        strikePending = false;
        SetAnimatorSpeed(1f);
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.SetFloat(speedParameterHash, 0f);
        }
        CrossFade(IdleState, 0.1f);
    }

    private void Awake()
    {
        DisablePhysicsBody();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
            animator.applyRootMotion = false;
        speedParameterHash = Animator.StringToHash(speedParameterName);
        CacheArena();
        ResolveCamera();
        if (!floorReady)
            SnapTo(transform.position);
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        if (delta <= 0f)
            return;

        switch (phase)
        {
            case Phase.Chase:
                TickChase(delta);
                break;
            case Phase.Pounce:
                TickPounce(delta);
                break;
            case Phase.Recover:
                ApplyGroundMotion(Vector3.zero, delta);
                ReportAnimatedSpeed(0f, delta);
                phaseTimer += delta;
                if (phaseTimer >= CatchHuntRules.PounceRecoverSeconds)
                {
                    phase = Phase.Idle;
                    SetAnimatorSpeed(1f);
                    CrossFade(IdleState, 0.12f);
                }
                break;
            default:
                ApplyGroundMotion(Vector3.zero, delta);
                ReportAnimatedSpeed(0f, delta);
                break;
        }
    }

    private void TickChase(float delta)
    {
        if (prey != null && !prey.IsActive)
            prey = null;

        bool chasingPrey = prey != null;
        Vector3 destination = chasingPrey
            ? CatchHuntRules.PredictInterceptPoint(
                prey.transform.position, prey.Velocity, transform.position, moveSpeed)
            : movePoint;
        destination.y = floorY;
        Vector3 toTarget = destination - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (!chasingPrey && (!hasMovePoint || distance <= 0.12f))
        {
            hasMovePoint = false;
            phase = Phase.Idle;
            ApplyGroundMotion(Vector3.zero, delta);
            ReportAnimatedSpeed(0f, delta);
            return;
        }

        Vector3 direction = distance > 0.0001f ? toTarget / distance : Vector3.zero;
        RotateTowards(direction, delta);

        if (chasingPrey)
        {
            // The lunge is judged against the mouse itself, not the intercept
            // point the cat is running towards.
            Vector3 toPrey = prey.transform.position - transform.position;
            toPrey.y = 0f;
            float preyDistance = toPrey.magnitude;
            Vector3 preyDirection = preyDistance > 0.0001f ? toPrey / preyDistance : direction;
            float facingAngle = Vector3.Angle(FlatForward(), preyDirection);
            chaseSeconds += delta;
            if (CatchHuntRules.ShouldPounce(preyDistance, facingAngle) ||
                CatchHuntRules.ShouldForcePounce(preyDistance, chaseSeconds))
            {
                Vector3 predicted = CatchHuntRules.PredictPreyPoint(
                    prey.transform.position, prey.Velocity);
                predicted.y = floorY;
                Vector3 lunge = predicted - transform.position;
                lunge.y = 0f;
                BeginPounce(predicted, lunge.magnitude);
                return;
            }
        }

        float step = Mathf.Min(moveSpeed, distance / Mathf.Max(delta, 0.0001f));
        ApplyGroundMotion(direction * step, delta);
        ReportAnimatedSpeed(step / moveSpeed, delta);
    }

    private void BeginPounce(Vector3 destination, float distance)
    {
        Vector3 from = transform.position;
        from.y = floorY;
        float reach = CatchHuntRules.PounceDistanceFor(distance);
        Vector3 direction = destination - from;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            direction.Normalize();
        else
            direction = FlatForward();

        pounceFrom = from;
        pounceTo = ClampToArena(from + direction * reach);
        pounceTo.y = floorY;
        pounceTravel = CatchHuntRules.PounceTravelSeconds;
        phase = Phase.Pounce;
        phaseTimer = 0f;
        pounceStarted = true;
        ReportAnimatedSpeed(0f, 0f);
        PlayPounceClip();
    }

    private void TickPounce(float delta)
    {
        phaseTimer += delta;
        float t = Mathf.Clamp01(phaseTimer / pounceTravel);
        float eased = t * t * (3f - 2f * t);
        Vector3 next = Vector3.Lerp(pounceFrom, pounceTo, eased);
        next.y = floorY + (4f * pounceHeight * t * (1f - t));
        MoveBody(next - transform.position);

        Vector3 heading = pounceTo - pounceFrom;
        heading.y = 0f;
        RotateTowards(heading.sqrMagnitude > 0.0001f ? heading.normalized : FlatForward(), delta);
        ReportAnimatedSpeed(0f, delta);

        if (t < 1f)
            return;

        Vector3 landed = pounceTo;
        landed.y = floorY;
        MoveBody(landed - transform.position);
        strikePending = true;
        strikePoint = landed;
        strikePrey = prey;
        prey = null;
        hasMovePoint = false;
        phase = Phase.Recover;
        phaseTimer = 0f;
    }

    private void PlayPounceClip()
    {
        // Skip the crouch keys: the cat is already running, so the lunge should
        // start on the launch pose and land on the clip's landing pose.
        float remainingClip = PounceClipLength - PounceClipLaunch;
        float target = pounceTravel + CatchHuntRules.PounceRecoverSeconds;
        SetAnimatorSpeed(remainingClip / Mathf.Max(0.05f, target));
        if (animator == null)
            return;
        int hash = Animator.StringToHash("Base Layer." + PounceState);
        if (animator.HasState(0, hash))
            animator.CrossFadeInFixedTime(hash, 0.05f, 0, PounceClipLaunch);
    }

    // The arena is a single flat floor with every wall collider stripped by the
    // content builder, so the hunt moves the cat directly and clamps it to the
    // floor instead of paying for character physics that has nothing to collide
    // with.
    private void ApplyGroundMotion(Vector3 planarVelocity, float delta)
    {
        if (planarVelocity.sqrMagnitude <= 0.0000001f)
            return;
        Vector3 next = transform.position + planarVelocity * delta;
        next = ClampToArena(next);
        next.y = floorY;
        transform.position = next;
    }

    private void MoveBody(Vector3 offset)
    {
        transform.position += offset;
    }

    private void RotateTowards(Vector3 direction, float delta)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;
        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * delta);
    }

    private Vector3 FlatForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private void ReportAnimatedSpeed(float normalized, float delta)
    {
        if (animator == null)
            return;
        if (delta <= 0f)
            animator.SetFloat(speedParameterHash, normalized);
        else
            animator.SetFloat(speedParameterHash, normalized, animationDampTime, delta);
    }

    private bool TryWorldFromScreen(Camera camera, Vector2 screen, out Vector3 world)
    {
        world = default;
        Ray ray = camera.ScreenPointToRay(screen);
        RaycastHit[] hits = Physics.RaycastAll(ray, 120f, ~0, QueryTriggerInteraction.Ignore);
        Vector3 floorHit = default;
        bool foundFloor = false;
        float floorDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
                continue;
            if (hitCollider.name != "Floor" && (floorCollider == null || hitCollider != floorCollider))
                continue;
            if (hits[i].distance >= floorDistance)
                continue;
            floorDistance = hits[i].distance;
            floorHit = hits[i].point;
            foundFloor = true;
        }

        if (foundFloor)
        {
            world = floorHit;
            world.y = floorY;
            return true;
        }

        var plane = new Plane(Vector3.up, new Vector3(0f, floorY, 0f));
        if (!plane.Raycast(ray, out float distance) || distance <= 0f || distance > 120f)
            return false;
        world = ray.GetPoint(distance);
        world.y = floorY;
        return true;
    }

    private Camera ResolveCamera()
    {
        if (gameplayCamera != null)
            return gameplayCamera;

        Camera[] cameras = transform.root.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null || !cameras[i].enabled)
                continue;
            gameplayCamera = cameras[i];
            return gameplayCamera;
        }

        gameplayCamera = Camera.main;
        return gameplayCamera;
    }

    /// <summary>
    /// A CharacterController left over from the shared cat prefab fights the
    /// hunt's own movement and drags the cat back every frame. The arena has
    /// nothing to collide with, so it is switched off outright.
    /// </summary>
    private void DisablePhysicsBody()
    {
        CharacterController controller = GetComponentInChildren<CharacterController>(true);
        if (controller != null)
            controller.enabled = false;
        Rigidbody rigidBody = GetComponentInChildren<Rigidbody>(true);
        if (rigidBody != null)
            rigidBody.isKinematic = true;
    }

    private void CacheArena()
    {
        if (arena == null)
            arena = transform.root.Find("CatchArena");
        if (floorCollider == null && arena != null)
        {
            Transform floor = arena.Find("Floor");
            if (floor != null)
                floorCollider = floor.GetComponent<Collider>();
        }
    }

    private Vector3 ClampToArena(Vector3 world)
    {
        if (floorCollider != null)
        {
            Bounds bounds = floorCollider.bounds;
            world.x = Mathf.Clamp(world.x, bounds.min.x + 0.4f, bounds.max.x - 0.4f);
            world.z = Mathf.Clamp(world.z, bounds.min.z + 0.4f, bounds.max.z - 0.4f);
            return world;
        }

        if (arena == null)
            return world;
        Vector3 local = arena.InverseTransformPoint(world);
        local.x = Mathf.Clamp(local.x, -3.4f, 3.4f);
        local.z = Mathf.Clamp(local.z, -2.4f, 2.4f);
        local.y = 0f;
        return arena.TransformPoint(local);
    }

    private float MeasureStandHeight(Vector3 world)
    {
        float top = world.y;
        if (floorCollider != null)
            top = floorCollider.bounds.max.y + 0.02f;
        return top;
    }

    private void SetAnimatorSpeed(float speed)
    {
        if (animator != null)
            animator.speed = Mathf.Clamp(speed, 0.35f, 2.5f);
    }

    private void CrossFade(string state, float duration)
    {
        if (animator == null || string.IsNullOrEmpty(state))
            return;
        int hash = Animator.StringToHash("Base Layer." + state);
        if (animator.HasState(0, hash))
            animator.CrossFadeInFixedTime(hash, duration, 0);
    }
}
