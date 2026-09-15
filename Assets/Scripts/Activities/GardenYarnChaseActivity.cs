using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Self-driven yarn play used on the Garden lawn. Unlike <see cref="BallChaseActivity"/>
/// the player never steers the cat with the joystick (the Garden clone has no joystick),
/// so the cat chases the hopping yarn ball on its own: the ball bounces to a new spot,
/// the cat pounces or paw-swats it, and the loop repeats a few times before finishing.
/// The routine owns every pose; a catch requires measured paw contact.
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

    private CatToyContactMotion contact;
    private CatActivityAnimation poses;
    private CatToyBallCollision ballCollision;
    public float LastHitDistance { get; private set; }
    public string InterruptedReason { get; private set; }
    private CharacterController characterController;
    private Coroutine routine;
    private int catches;
    private int hopIndex = -1;

    public int CatchCount => catches;
    public int CatchGoal => Mathf.Max(1, catchesToComplete);
    public Transform YarnBall => yarnBall;
    public Transform[] HopPoints => hopPoints;
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
        contact = Cat.GetComponent<CatToyContactMotion>() ?? Cat.gameObject.AddComponent<CatToyContactMotion>();
        poses = Cat.GetComponent<CatActivityAnimation>();
        ballCollision = new CatToyBallCollision(gameObject.scene, yarnBall);
        LastHitDistance = float.PositiveInfinity; InterruptedReason = null;
        characterController = Cat.GetComponent<CharacterController>();
        catches = 0;
        hopIndex = -1;
        // Start the loose ball on an authored, actually reachable patch. Its
        // previous/random hidden position may be inside a newly placed prop.
        bool placed = false;
        for (int i = 0; i < hopPoints.Length; i++)
        {
            if (hopPoints[i] == null) continue;
            Vector3 first = ballCollision.OnFloor(hopPoints[i].position);
            if (!TryContactStand(first, out _, out _)) continue;
            yarnBall.position = first; hopIndex = i; placed = true; break;
        }
        if (!placed) { ballCollision.Dispose(); ballCollision = null; return false; }
        Cat.SetInputBlock(this, CatInputCategory.Petting | CatInputCategory.WorldActions);
        yarnBall.gameObject.SetActive(true);
        routine = StartCoroutine(PlayRoutine());
        return true;
    }

    private IEnumerator PlayRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null) characterController.enabled = false;
        PlayCatPose(CatActivityPose.Sniff);
        yield return CatActivityFacing.Turn(Cat, CatActivityFacing.Resolve(Cat, Cat.transform.position, Cat.transform.rotation));
        yield return new WaitForSeconds(.2f);
        while (IsRunning && catches < CatchGoal)
        {
            bool found = false; Vector3 destination = yarnBall.position, stand = default;
            List<Vector3> approach = null;
            int first = Random.Range(0, hopPoints.Length);
            for (int offset = 0; offset < hopPoints.Length; offset++)
            {
                int index = (first + offset) % hopPoints.Length;
                if (index == hopIndex || hopPoints[index] == null) continue;
                destination = hopPoints[index].position; destination.y = 0f;
                destination = ballCollision.OnFloor(destination);
                // Reserve a complete clear hop, including its arc. A valid
                // endpoint alone can still strand the ball against a prop.
                if (!HasClearHop(yarnBall.position, destination)) continue;
                if (!TryContactStand(destination, out stand, out approach)) continue;
                hopIndex = index; found = true; break;
            }
            if (!found)
            {
                // In a furnished corner a small vertical bounce is still a
                // real, reachable play beat; never teleport through furniture.
                destination = yarnBall.position;
                if (!HasClearHop(destination, destination) || !TryContactStand(destination, out stand, out approach))
                { InterruptedReason = "No clear visible yarn contact"; CancelActivity(); yield break; }
            }
            yield return HopBall(yarnBall.position, destination);
            // A swept obstacle can stop the hop early. Reach the real stop point.
            if (!TryContactStand(yarnBall.position, out stand, out approach))
            { InterruptedReason = "Yarn stop is unreachable"; CancelActivity(); yield break; }
            yield return FollowPath(approach);
            Vector3 toward = yarnBall.position - Cat.transform.position; toward.y = 0f;
            Quaternion facing = Quaternion.LookRotation(toward.normalized, Vector3.up);
            PlayCatPose(CatActivityPose.Sniff);
            yield return CatActivityFacing.Turn(Cat, facing);
            // A small ready pounce stays on the clear contact side. Its pose and
            // timing belong to this routine, not a separate reaction timer.
            if ((catches & 1) == 0)
            {
                Vector3 point = Cat.transform.position;
                yield return CatActivityMotion.Jump(Cat, point, point, facing, facing, .08f);
            }
            bool hit = false; float elapsed = 0f;
            contact.Clear();
            while (elapsed < .92f && IsRunning)
            {
                float phase = Mathf.Clamp01(elapsed / .92f);
                poses.SetTimedPose((catches & 1) == 0 ? CatActivityPose.BatLeft : CatActivityPose.BatRight, phase);
                contact.Reach(yarnBall.position, (catches & 1) == 0, phase);
                if (Time.deltaTime > 0f && phase >= .34f && phase < .75f && contact.Distance < .095f)
                {
                    LastHitDistance = contact.Distance; hit = true; catches++; NotifyChanged();
                    GameAudio.Play(AudioCue.Cloth, .85f);
                    contact.Clear(); yield return NudgeBall(toward.normalized); break;
                }
                elapsed += Time.deltaTime; yield return null;
            }
            contact.Clear();
            if (!hit) { InterruptedReason = "No real paw contact at " + yarnBall.position; CancelActivity(); yield break; }
            PlayCatPose(CatActivityPose.Sniff);
            yield return CatActivityFacing.Turn(Cat, CatActivityFacing.Resolve(Cat, Cat.transform.position, facing));
            PlayCatPose(CatActivityPose.Sit);
            yield return new WaitForSeconds(.22f);
        }
        yarnBall.gameObject.SetActive(false);
        FinishPlay();
    }

    private bool TryContactStand(Vector3 ballPoint, out Vector3 stand, out List<Vector3> path)
    {
        Vector3 authored = ballPoint - DirectionFromCat(ballPoint) * Mathf.Max(.28f, catchOffset);
        authored.y = Cat.transform.position.y;
        return CatActivityFacing.TryFindContactStand(Cat, ballPoint, authored, Cat.transform.position, out stand, out path);
    }

    private IEnumerator FollowPath(List<Vector3> path)
    {
        foreach (Vector3 point in path)
        {
            Vector3 target = point; target.y = Cat.transform.position.y;
            Vector3 direction = target - Cat.transform.position; direction.y = 0f;
            if (direction.sqrMagnitude < .0001f) continue;
            Quaternion travel = Quaternion.LookRotation(direction);
            PlayCatPose(CatActivityPose.Sniff);
            yield return CatActivityFacing.Turn(Cat, travel, .22f);
            PlayCatPose(CatActivityPose.Walk);
            while ((Cat.transform.position - target).sqrMagnitude > .0001f)
            {
                Cat.transform.position = Vector3.MoveTowards(Cat.transform.position, target, 1.5f * Time.deltaTime);
                yield return null;
            }
        }
    }

    private IEnumerator HopBall(Vector3 start, Vector3 destination)
    {
        float elapsed = 0f;
        while (elapsed < hopDuration && IsRunning)
        {
            elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / hopDuration);
            Vector3 next = Vector3.Lerp(start, destination, t);
            next.y += Mathf.Sin(t * Mathf.PI) * (CatRunnerProgressService.ReducedMotion ? .08f : hopHeight);
            yarnBall.position = ballCollision.Sweep(yarnBall.position, next - yarnBall.position, out bool blocked);
            if (!CatRunnerProgressService.ReducedMotion) yarnBall.Rotate(new Vector3(1f, .3f, .2f), 540f * Time.deltaTime, Space.Self);
            if (blocked) break;
            yield return null;
        }
        Vector3 floor = yarnBall.position; floor.y = 0f;
        yarnBall.position = ballCollision.OnFloor(floor);
    }

    private bool HasClearHop(Vector3 start, Vector3 destination)
    {
        Vector3 previous = start;
        const int samples = 64;
        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 next = Vector3.Lerp(start, destination, t);
            next.y += Mathf.Sin(t * Mathf.PI) * (CatRunnerProgressService.ReducedMotion ? .08f : hopHeight);
            previous = ballCollision.Sweep(previous, next - previous, out bool blocked);
            // A normal landing can hit the floor exactly at the sweep's skin
            // margin. Reject an early stop, not an endpoint already reached.
            if (blocked && (previous - next).sqrMagnitude > .0001f * .0001f) return false;
        }
        return true;
    }

    private IEnumerator NudgeBall(Vector3 direction)
    {
        float elapsed = 0f, previous = 0f;
        PlayCatPose(CatActivityPose.Sniff);
        while (elapsed < .30f && IsRunning)
        {
            elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / .30f);
            float distance = .28f * (1f - (1f - t) * (1f - t));
            Vector3 before = yarnBall.position;
            yarnBall.position = ballCollision.Sweep(before, direction * (distance - previous), out bool blocked);
            previous = distance;
            if (!CatRunnerProgressService.ReducedMotion)
                yarnBall.Rotate(Vector3.Cross(Vector3.up, direction), Vector3.Distance(before, yarnBall.position) / .09f * Mathf.Rad2Deg, Space.World);
            if (blocked) break;
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
        ballCollision?.Dispose(); ballCollision = null;
        if (contact != null) contact.Clear();
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
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        routine = null;
        if (yarnBall != null)
            yarnBall.gameObject.SetActive(false);
        ReleaseControl();
        base.CancelActivity();
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
