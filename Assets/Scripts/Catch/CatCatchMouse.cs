using UnityEngine;

/// <summary>
/// Arena prey. A mouse wanders in readable straight runs with short pauses, and
/// only bolts when the cat closes in, so a hunt can be planned instead of
/// guessed. A missed pounce sends it into a brief panic sprint.
/// </summary>
[DisallowMultipleComponent]
public sealed class CatCatchMouse : MonoBehaviour
{
    [SerializeField, Min(0.4f)] private float wanderSpeed = CatchHuntRules.MouseWanderSpeed;
    [SerializeField, Min(1.5f)] private float fleeSpeed = CatchHuntRules.MouseFleeSpeed;
    [SerializeField, Min(1.5f)] private float panicSpeed = CatchHuntRules.MousePanicSpeed;
    [SerializeField] private Transform arena;
    [SerializeField] private Vector3 localMin = new Vector3(-2.45f, 0.12f, -1.7f);
    [SerializeField] private Vector3 localMax = new Vector3(2.45f, 0.12f, 1.7f);
    [SerializeField] private Transform hunter;
    private Vector3 target;
    private bool active;
    private Vector3 restScale;
    private float graceRemaining;
    private float panicRemaining;
    private float pauseRemaining;
    private Vector3 velocity;
    private float fleeStamina = CatchHuntRules.MouseFleeStaminaSeconds;
    private float winded;

    /// <summary>True while the mouse is out of breath — the cat's opening.</summary>
    public bool IsWinded => winded > 0f;

    public bool IsActive => active && gameObject.activeInHierarchy;

    /// <summary>Planar movement per second, used by the cat to lead its pounce.</summary>
    public Vector3 Velocity => velocity;

    /// <summary>
    /// A freshly launched mouse is untouchable for a short grace window so a
    /// respawn can never be eaten by the pounce that just cleared the board.
    /// </summary>
    public bool IsCatchable => IsActive && graceRemaining <= 0f;

    private void Awake()
    {
        if (arena == null)
            arena = transform.parent;
        if (hunter == null && arena != null)
            hunter = arena.Find("CatchCat");
        restScale = transform.localScale;
        if (restScale.sqrMagnitude < 0.0001f)
            restScale = Vector3.one;
        if ((localMax - localMin).sqrMagnitude < 0.01f)
        {
            localMin = new Vector3(-2.45f, 0.12f, -1.7f);
            localMax = new Vector3(2.45f, 0.12f, 1.7f);
        }
    }

    public void Configure(Transform arenaRoot, Vector3 minLocal, Vector3 maxLocal, Transform cat)
    {
        arena = arenaRoot;
        localMin = minLocal;
        localMax = maxLocal;
        hunter = cat;
        restScale = transform.localScale;
        if (restScale.sqrMagnitude < 0.0001f)
            restScale = Vector3.one;
    }

    public void Launch(Vector3 worldPosition, float graceSeconds = 0f)
    {
        transform.position = worldPosition;
        gameObject.SetActive(true);
        active = true;
        graceRemaining = Mathf.Max(0f, graceSeconds);
        panicRemaining = 0f;
        pauseRemaining = 0f;
        velocity = Vector3.zero;
        fleeStamina = CatchHuntRules.MouseFleeStaminaSeconds;
        winded = 0f;
        PickTarget();
    }

    public void Hide()
    {
        active = false;
        graceRemaining = 0f;
        panicRemaining = 0f;
        pauseRemaining = 0f;
        velocity = Vector3.zero;
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    /// <summary>Reward for a missed pounce: the mouse bolts for a moment.</summary>
    public void Panic()
    {
        if (!active)
            return;
        panicRemaining = CatchHuntRules.MousePanicSeconds;
        pauseRemaining = 0f;
        if (hunter == null)
            return;
        Vector3 away = transform.position - hunter.position;
        away.y = 0f;
        if (away.sqrMagnitude > 0.0001f)
            target = Clamp(transform.position + away.normalized * 2.6f);
    }

    private void Update()
    {
        if (!active)
            return;

        float delta = Time.deltaTime;
        if (graceRemaining > 0f)
            graceRemaining = Mathf.Max(0f, graceRemaining - delta);
        if (panicRemaining > 0f)
            panicRemaining = Mathf.Max(0f, panicRemaining - delta);

        float speed = ChooseSpeedAndTarget(delta);
        Vector3 before = transform.position;
        Vector3 next = speed > 0f
            ? Vector3.MoveTowards(before, target, speed * delta)
            : before;
        Vector3 moved = next - before;
        moved.y = 0f;
        velocity = delta > 0f ? moved / delta : Vector3.zero;
        if (moved.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moved.normalized, Vector3.up),
                16f * delta);

        float bob = speed > 0f ? Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.04f : 0f;
        next.y = ArenaPoint(0f, localMin.y, 0f).y + bob;
        transform.position = next;
        float pulse = speed > 0f ? 0.06f : 0.03f;
        transform.localScale = restScale *
            (1f + Mathf.Sin(Time.time * 10f + GetInstanceID()) * pulse);

        if (speed > 0f && (Flatten(next) - Flatten(target)).sqrMagnitude < 0.04f)
            BeginPause();
    }

    private float ChooseSpeedAndTarget(float delta)
    {
        if (panicRemaining > 0f)
            return panicSpeed;

        if (winded > 0f)
        {
            winded = Mathf.Max(0f, winded - delta);
            if (winded <= 0f)
                fleeStamina = CatchHuntRules.MouseFleeStaminaSeconds;
            return wanderSpeed * 0.45f;
        }

        if (hunter != null)
        {
            Vector3 away = transform.position - hunter.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance < CatchHuntRules.MouseFleeDistance && distance > 0.0001f)
            {
                pauseRemaining = 0f;
                fleeStamina -= delta;
                if (fleeStamina <= 0f)
                {
                    winded = CatchHuntRules.MouseFleeRecoverSeconds;
                    return wanderSpeed * 0.45f;
                }

                // Escape on a tangent rather than straight back, so the cat can
                // still cut the corner and the run stays readable.
                Vector3 radial = away / distance;
                Vector3 tangent = Vector3.Cross(Vector3.up, radial);
                Vector3 escape = (radial + tangent * 0.55f).normalized;
                target = Clamp(transform.position + escape * 1.8f);
                return fleeSpeed;
            }

            fleeStamina = Mathf.Min(
                CatchHuntRules.MouseFleeStaminaSeconds,
                fleeStamina + delta * 0.6f);
        }

        if (pauseRemaining > 0f)
        {
            pauseRemaining = Mathf.Max(0f, pauseRemaining - delta);
            if (pauseRemaining > 0f)
                return 0f;
            PickTarget();
        }

        return wanderSpeed;
    }

    private void BeginPause()
    {
        pauseRemaining = Random.Range(0.4f, 1.2f);
    }

    private void PickTarget()
    {
        target = ArenaPoint(
            Random.Range(localMin.x, localMax.x),
            localMin.y,
            Random.Range(localMin.z, localMax.z));
    }

    private Vector3 Clamp(Vector3 point)
    {
        Vector3 local = arena != null ? arena.InverseTransformPoint(point) : point;
        local.x = Mathf.Clamp(local.x, localMin.x, localMax.x);
        local.z = Mathf.Clamp(local.z, localMin.z, localMax.z);
        local.y = localMin.y;
        return arena != null ? arena.TransformPoint(local) : local;
    }

    private Vector3 ArenaPoint(float x, float y, float z)
    {
        var local = new Vector3(x, y, z);
        return arena != null ? arena.TransformPoint(local) : local;
    }

    private static Vector3 Flatten(Vector3 point)
    {
        point.y = 0f;
        return point;
    }
}
