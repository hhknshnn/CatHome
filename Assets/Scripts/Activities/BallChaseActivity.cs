using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallChaseActivity : CatActivity
{
    [Header("Ball Chase")]
    [SerializeField] private Transform ball;
    [SerializeField] private Transform[] landingPoints = new Transform[0];
    [SerializeField, Min(1)] private int catchesToComplete = 3;
    [SerializeField, Min(0.1f)] private float catchDistance = 0.55f;
    [SerializeField, Min(0.1f)] private float flightDuration = 0.65f;
    [SerializeField, Min(0f)] private float flightHeight = 0.35f;

    private CatActivityReaction reaction;
    private Coroutine flightRoutine;
    private int catches;
    private int landingIndex = -1;
    private bool canCatch;

    public int CatchCount => catches;
    public int CatchGoal => Mathf.Max(1, catchesToComplete);
    public override string ProgressLabel => IsRunning
        ? $"CATCH THE BALL  {catches}/{CatchGoal}"
        : string.Empty;

    protected override void Awake()
    {
        base.Awake();
        if (ball != null)
            ball.gameObject.SetActive(false);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (ball == null || landingPoints == null || landingPoints.Length < 2)
        {
            failureReason = "BALL GAME IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        reaction = Cat.GetComponent<CatActivityReaction>() ??
                   Cat.gameObject.AddComponent<CatActivityReaction>();
        catches = 0;
        canCatch = false;
        Cat.SetInputBlock(this, CatInputCategory.Petting | CatInputCategory.WorldActions);
        ball.gameObject.SetActive(true);
        ball.position = transform.position + Vector3.up * 0.24f;
        LaunchNextBall(0.15f);
        return true;
    }

    private void Update()
    {
        if (!IsRunning || !canCatch || Cat == null || ball == null)
            return;

        ball.Rotate(Vector3.right, 190f * Time.deltaTime, Space.Self);
        TrackBallWithAttention();
        Vector3 delta = Cat.transform.position - ball.position;
        delta.y = 0f;
        if (delta.sqrMagnitude <= catchDistance * catchDistance)
            CatchBall();
    }

    private void CatchBall()
    {
        canCatch = false;
        catches++;
        FaceBall();
        if ((catches & 1) == 1)
            reaction.PlayPawSwatReaction();
        else
            reaction.PlayPounceReaction();
        NotifyChanged();

        if (catches >= CatchGoal)
        {
            StartCoroutine(FinishAfterReaction());
            return;
        }

        LaunchNextBall(0.76f);
    }

    private IEnumerator FinishAfterReaction()
    {
        yield return new WaitForSeconds(0.76f);
        if (ball != null)
            ball.gameObject.SetActive(false);
        ReleaseActivityInput();
        CompleteActivity("BALL CHAMPION!");
    }

    private void LaunchNextBall(float delay)
    {
        if (flightRoutine != null)
            StopCoroutine(flightRoutine);
        flightRoutine = StartCoroutine(LaunchRoutine(delay));
    }

    private IEnumerator LaunchRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (!IsRunning || ball == null)
            yield break;

        Vector3 start = ball.position;
        int nextIndex = PickNextLandingIndex();
        Vector3 destination = landingPoints[nextIndex].position;
        float elapsed = 0f;
        while (elapsed < flightDuration && IsRunning)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flightDuration);
            float horizontalT = t * t * (3f - 2f * t);
            Vector3 position = Vector3.Lerp(start, destination, horizontalT);
            // One lively throw followed by a small settling bounce reads much
            // closer to a soft toy ball than a single floating sine arc.
            float bounce = t < 0.72f
                ? Mathf.Sin((t / 0.72f) * Mathf.PI) * flightHeight
                : Mathf.Sin(((t - 0.72f) / 0.28f) * Mathf.PI) * flightHeight * 0.24f;
            position.y += bounce;
            ball.position = position;
            ball.Rotate(new Vector3(1f, 0.25f, 0.35f), 620f * Time.deltaTime, Space.Self);
            yield return null;
        }

        if (IsRunning)
        {
            ball.position = destination;
            canCatch = true;
        }
        flightRoutine = null;
    }

    private int PickNextLandingIndex()
    {
        if (landingPoints.Length <= 1)
            return 0;

        int step = Random.Range(1, landingPoints.Length);
        landingIndex = (landingIndex + step) % landingPoints.Length;
        return landingIndex;
    }

    private void FaceBall()
    {
        if (Cat == null || ball == null)
            return;
        Vector3 direction = ball.position - Cat.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            Cat.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private void TrackBallWithAttention()
    {
        if (Cat == null || ball == null || Cat.IsMovementInputActive)
            return;
        Vector3 direction = ball.position - Cat.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.05f)
            return;
        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Cat.transform.rotation = Quaternion.Slerp(
            Cat.transform.rotation,
            target,
            7f * Time.deltaTime);
    }

    private void ReleaseActivityInput()
    {
        if (Cat != null)
            Cat.ReleaseInputBlock(this);
    }

    protected override void CancelActivity()
    {
        ReleaseActivityInput();
        base.CancelActivity();
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();
        flightRoutine = null;
        canCatch = false;
        if (ball != null)
            ball.gameObject.SetActive(false);
        ReleaseActivityInput();
        base.OnDisable();
    }

#if UNITY_EDITOR
    public void EditorConfigureBall(
        Transform ballTransform,
        Transform[] points,
        int catchGoal)
    {
        ball = ballTransform;
        landingPoints = points ?? new Transform[0];
        catchesToComplete = Mathf.Max(1, catchGoal);
    }
#endif
}
