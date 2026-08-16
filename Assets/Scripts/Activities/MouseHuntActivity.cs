using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MouseHuntActivity : CatActivity
{
    [Header("Mouse Hunt")]
    [SerializeField] private Transform mouse;
    [SerializeField] private Transform homePoint;
    [SerializeField] private Transform[] patrolPoints = new Transform[0];
    [SerializeField, Min(1)] private int catchesToComplete = 3;
    [SerializeField, Min(0.1f)] private float catchDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float moveSpeed = 1.65f;

    private CatActivityReaction reaction;
    private int catches;
    private int targetIndex = -1;
    private float catchPause;

    public int CatchCount => catches;
    public int CatchGoal => Mathf.Max(1, catchesToComplete);
    public override string ProgressLabel => IsRunning
        ? $"CATCH THE MOUSE  {catches}/{CatchGoal}"
        : string.Empty;

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (mouse == null || homePoint == null || patrolPoints == null || patrolPoints.Length < 2)
        {
            failureReason = "MOUSE TOY IS NOT READY";
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
        catchPause = 0f;
        mouse.gameObject.SetActive(true);
        mouse.position = homePoint.position;
        PickNextTarget();
        return true;
    }

    private void Update()
    {
        if (!IsRunning || mouse == null || Cat == null)
            return;

        if (catchPause > 0f)
        {
            catchPause -= Time.deltaTime;
            return;
        }

        Transform target = patrolPoints[targetIndex];
        Vector3 before = mouse.position;
        mouse.position = Vector3.MoveTowards(before, target.position, moveSpeed * Time.deltaTime);
        Vector3 direction = mouse.position - before;
        if (direction.sqrMagnitude > 0.0001f)
            mouse.rotation = Quaternion.Slerp(
                mouse.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                12f * Time.deltaTime);

        if ((mouse.position - target.position).sqrMagnitude < 0.01f)
            PickNextTarget();

        Vector3 catDelta = Cat.transform.position - mouse.position;
        catDelta.y = 0f;
        if (catDelta.sqrMagnitude <= catchDistance * catchDistance)
            CatchMouse();
    }

    private void CatchMouse()
    {
        catches++;
        catchPause = 0.62f;
        reaction.PlayPounceReaction();
        NotifyChanged();

        if (catches >= CatchGoal)
        {
            StartCoroutine(FinishAfterReaction());
            return;
        }

        PickNextTarget();
        mouse.position = patrolPoints[targetIndex].position;
        PickNextTarget();
    }

    private IEnumerator FinishAfterReaction()
    {
        yield return new WaitForSeconds(0.66f);
        if (mouse != null && homePoint != null)
            mouse.position = homePoint.position;
        CompleteActivity("MIGHTY HUNTER!");
    }

    private void PickNextTarget()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        int step = Random.Range(1, patrolPoints.Length);
        targetIndex = (targetIndex + step) % patrolPoints.Length;
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();
        if (mouse != null && homePoint != null)
            mouse.position = homePoint.position;
        base.OnDisable();
    }

#if UNITY_EDITOR
    public void EditorConfigureMouse(
        Transform mouseTransform,
        Transform home,
        Transform[] points,
        int catchGoal)
    {
        mouse = mouseTransform;
        homePoint = home;
        patrolPoints = points ?? new Transform[0];
        catchesToComplete = Mathf.Max(1, catchGoal);
    }
#endif
}
