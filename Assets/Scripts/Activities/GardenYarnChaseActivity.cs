using System.Collections;
using UnityEngine;

/// <summary>
/// Self-driven yarn play used on the Garden lawn. Unlike <see cref="BallChaseActivity"/>
/// the player never steers the cat with the joystick (the Garden clone has no joystick),
/// so the cat chases the hopping yarn ball on its own: the ball bounces to a new spot,
/// the cat pounces or paw-swats it, and the loop repeats a few times before finishing.
/// Reuses the shared <see cref="CatActivityReaction"/> pounce / paw-swat animations.
/// </summary>
[DisallowMultipleComponent]
public sealed class GardenYarnChaseActivity : CatActivity
{
    [Header("Yarn Chase")]
    [SerializeField] private Transform yarnBall;
    [SerializeField] private Transform[] hopPoints = new Transform[0];
    [SerializeField, Min(1)] private int catchesToComplete = 4;
    [SerializeField, Min(0.1f)] private float hopDuration = 0.42f;
    [SerializeField, Min(0.1f)] private float hopHeight = 0.32f;
    [SerializeField, Min(0.1f)] private float pounceDuration = 0.62f;
    [SerializeField, Min(0f)] private float catchOffset = 0.34f;

    private CatActivityReaction reaction;
    private CharacterController characterController;
    private Coroutine routine;
    private int catches;
    private int hopIndex = -1;

    public int CatchCount => catches;
    public int CatchGoal => Mathf.Max(1, catchesToComplete);
    public override string ProgressLabel => IsRunning
        ? $"CHASE THE YARN  {catches}/{CatchGoal}"
        : string.Empty;

    protected override void Awake()
    {
        base.Awake();
        if (yarnBall != null)
            yarnBall.gameObject.SetActive(false);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (yarnBall == null || hopPoints == null || hopPoints.Length < 2)
        {
            failureReason = "YARN IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        reaction = Cat.GetComponent<CatActivityReaction>() ??
                   Cat.gameObject.AddComponent<CatActivityReaction>();
        characterController = Cat.GetComponent<CharacterController>();
        catches = 0;
        hopIndex = -1;
        Cat.SetInputBlock(this, CatInputCategory.Petting | CatInputCategory.WorldActions);
        yarnBall.gameObject.SetActive(true);
        yarnBall.position = PickNextHopPoint();
        routine = StartCoroutine(PlayRoutine());
        return true;
    }

    private IEnumerator PlayRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        yield return new WaitForSeconds(0.2f);

        while (IsRunning && catches < CatchGoal)
        {
            Vector3 start = yarnBall.position;
            Vector3 destination = PickNextHopPoint();
            yield return HopBall(start, destination);
            if (!IsRunning)
                break;

            FaceCatTo(destination);
            bool leap = (catches & 1) == 0;
            if (leap)
            {
                reaction.PlayPounceReaction();
                yield return LungeCatTo(destination);
            }
            else
            {
                reaction.PlayPawSwatReaction();
                yield return NudgeBall(destination);
            }

            catches++;
            NotifyChanged();
            yield return new WaitForSeconds(0.22f);
        }

        if (yarnBall != null)
            yarnBall.gameObject.SetActive(false);
        FinishPlay();
    }

    private IEnumerator HopBall(Vector3 start, Vector3 destination)
    {
        float elapsed = 0f;
        while (elapsed < hopDuration && IsRunning)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hopDuration);
            Vector3 position = Vector3.Lerp(start, destination, t);
            position.y += Mathf.Sin(t * Mathf.PI) * hopHeight;
            yarnBall.position = position;
            yarnBall.Rotate(new Vector3(1f, 0.3f, 0.2f), 540f * Time.deltaTime, Space.Self);
            yield return null;
        }

        if (IsRunning)
            yarnBall.position = destination;
    }

    private IEnumerator LungeCatTo(Vector3 ballPoint)
    {
        Vector3 start = Cat.transform.position;
        Vector3 target = ballPoint - DirectionFromCat(ballPoint) * catchOffset;
        target.y = start.y;
        float elapsed = 0f;
        while (elapsed < pounceDuration && IsRunning)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / pounceDuration));
            Vector3 position = Vector3.Lerp(start, target, t);
            // A shallow arc so the pounce reads as a leap rather than a slide.
            position.y = start.y + Mathf.Sin(t * Mathf.PI) * 0.12f;
            Cat.transform.position = position;
            yield return null;
        }

        if (IsRunning)
        {
            Vector3 grounded = target;
            grounded.y = start.y;
            Cat.transform.position = grounded;
        }
    }

    private IEnumerator NudgeBall(Vector3 destination)
    {
        // The paw swat bats the yarn a short skip sideways for a lively bounce.
        Vector3 start = destination;
        Vector3 nudged = destination + DirectionFromCat(destination) * -0.28f;
        float elapsed = 0f;
        const float nudgeTime = 0.3f;
        while (elapsed < nudgeTime && IsRunning)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / nudgeTime);
            Vector3 position = Vector3.Lerp(start, nudged, t);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.14f;
            yarnBall.position = position;
            yarnBall.Rotate(Vector3.right, 420f * Time.deltaTime, Space.Self);
            yield return null;
        }
    }

    private Vector3 PickNextHopPoint()
    {
        if (hopPoints.Length <= 1)
            return hopPoints.Length == 1 && hopPoints[0] != null
                ? hopPoints[0].position
                : transform.position;

        int step = Random.Range(1, hopPoints.Length);
        hopIndex = (hopIndex + step) % hopPoints.Length;
        Transform point = hopPoints[hopIndex];
        return point != null ? point.position : transform.position;
    }

    private Vector3 DirectionFromCat(Vector3 point)
    {
        Vector3 direction = point - Cat.transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private void FaceCatTo(Vector3 point)
    {
        if (Cat == null)
            return;
        Vector3 direction = DirectionFromCat(point);
        Cat.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    private void FinishPlay()
    {
        ReleaseControl();
        CompleteActivity("YARN CHAMPION!");
    }

    private void ReleaseControl()
    {
        if (characterController != null)
            characterController.enabled = true;
        if (Cat != null)
        {
            Cat.SetMovementLocked(this, false);
            Cat.ReleaseInputBlock(this);
        }
    }

    protected override void CancelActivity()
    {
        ReleaseControl();
        base.CancelActivity();
    }

    protected override void OnDisable()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = null;
        if (yarnBall != null)
            yarnBall.gameObject.SetActive(false);
        ReleaseControl();
        base.OnDisable();
    }

#if UNITY_EDITOR
    public void EditorConfigureYarn(
        Transform ball,
        Transform[] points,
        int catchGoal)
    {
        yarnBall = ball;
        hopPoints = points ?? new Transform[0];
        catchesToComplete = Mathf.Max(1, catchGoal);
    }
#endif
}
