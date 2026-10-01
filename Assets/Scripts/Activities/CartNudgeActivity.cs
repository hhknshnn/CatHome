using System.Collections;
using UnityEngine;

/// <summary>
/// Shove the dish cart and watch it roll.
///
/// The whole cart rolls, so the routine drives the VisualContent transform.
/// After the cat leaves its footprint, the cart is left exactly
/// where it started — a product that drifted a little further every time the cat
/// played with it would eventually be somewhere the catalog never placed it.
///
/// The cat stays planted beside the actual shelf. A measured paw contact
/// starts each roll; the cart never rebounds through the cat.
/// </summary>
[DisallowMultipleComponent]
public sealed class CartNudgeActivity : CatActivity
{
    [Header("Cart nudge")]
    [SerializeField] private Transform shovePoint;
    [SerializeField] private Transform cartVisual;
    [SerializeField] private Vector3 rollDirection = Vector3.right;
    [SerializeField, Min(0.02f)] private float rollDistance = 0.32f;
    [SerializeField, Min(1)] private int shoveCount = 2;

    private CharacterController characterController;
    private Vector3 visualHome;
    private bool visualHomeCaptured;
    private CatToyContactMotion contact;
    private CatPawReachMotion pawReach;
    private CatPawMeshContactResolver meshContacts;
    private Transform meshContactRoot;
    public bool IsPushing {get;private set;}
    public int ContactStrokes {get;private set;}
    public float MinimumPawDistance {get;private set;}
    public Vector3 LastStrikePosition {get;private set;}
    public Vector3 ContactPoint {get;private set;}

    public override string ProgressLabel => IsRunning ? "PUSHING..." : string.Empty;

    public float RollDistance => Mathf.Max(0.02f, rollDistance);
    public int ShoveCount => Mathf.Max(1, shoveCount);
    public Transform CartVisual => cartVisual;
    public Vector3 WorldRollDirection => transform.TransformDirection(rollDirection.sqrMagnitude > .0001f ? rollDirection.normalized : Vector3.right).normalized;

    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        if (actor == null || shovePoint == null || cartVisual == null) return false;
        Vector3 centre = shovePoint.position; centre.y = actor.transform.position.y;
        // Reject distant/blocked roots before triangle and source-pose work.
        if (!CatActivityStartResolver.Current(actor, centre, .44f, out start)) return false;
        var renderer = cartVisual.GetComponentInChildren<Renderer>();
        if (renderer == null) return false;
        Vector3 toward = renderer.bounds.center - actor.transform.position; toward.y = 0;
        if (toward.sqrMagnitude <= .00000001f) return false;
        // No Collider.Raycast remains, including the old zero-direction case.
        // Every seed and accepted target comes from real source bones and mesh triangles.
        if (!EnsureMeshContacts().TryResolve(actor, Vector3.zero, out var first,
            candidate => AllStrokesReachable(actor, centre, candidate))) return false;
        if (!CatActivityStartResolver.Facing(actor, centre, .44f, first.Plan.Target, 70f, out start)) return false;
        start.PawPlan = first.Plan; start.HasPawPlan = true; return true;
    }

    CatPawMeshContactResolver EnsureMeshContacts()
    {
        if (meshContacts == null || meshContactRoot != cartVisual)
        {
            meshContactRoot = cartVisual;
            meshContacts = new CatPawMeshContactResolver(cartVisual, CatActivityPose.BatLeft,
                CatActivityPose.BatRight, CatActivityPose.Push, CatActivityPose.Scratch);
        }
        return meshContacts;
    }

    bool AllStrokesReachable(CatMovement actor, Vector3 centre, CatPawMeshContactResolver.Solution first)
    {
        if (!CatActivityStartResolver.Facing(actor, centre, .44f, first.Plan.Target, 70f, out _)) return false;
        Vector3 roll = RollAwayFrom(first.Plan.Target);
        for (int beat = 1; beat < ShoveCount; beat++)
            if (!EnsureMeshContacts().TryResolve(actor, roll * (RollDistance * beat / ShoveCount), out _)) return false;
        return true;
    }

    Vector3 RollAwayFrom(Vector3 target)
    {
        Vector3 away = cartVisual.GetComponentInChildren<Renderer>().bounds.center - target;
        return Vector3.Dot(away, WorldRollDirection) >= 0 ? WorldRollDirection : -WorldRollDirection;
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (shovePoint == null || cartVisual == null)
        {
            failureReason = "THE CART IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        if (!visualHomeCaptured)
        {
            visualHome = cartVisual.localPosition;
            visualHomeCaptured = true;
        }
        ContactStrokes=0;MinimumPawDistance=float.PositiveInfinity;
        StartCoroutine(NudgeRoutine());
        return true;
    }

    private IEnumerator NudgeRoutine()
    {
        Cat.SetMovementLocked(this, true); if (characterController != null) characterController.enabled = false;
        Vector3 stand = AcceptedStart.Position; Quaternion facing = AcceptedStart.Rotation;
        contact = Cat.GetComponent<CatToyContactMotion>() ?? Cat.gameObject.AddComponent<CatToyContactMotion>();
        pawReach = Cat.GetComponent<CatPawReachMotion>() ?? Cat.gameObject.AddComponent<CatPawReachMotion>();
        // Keep the wheel direction that was checked for the entire sequence.
        // Small IK error around the cart centre cannot reverse the second roll.
        Vector3 roll = RollAwayFrom(AcceptedStart.ActionTarget);
        for (int beat = 0; beat < ShoveCount; beat++)
        {
            CatPawReachPlan plan = AcceptedStart.PawPlan;
            CatMeshContactSurface.Hit surface;
            if (beat == 0)
            {
                if (!EnsureMeshContacts().TryClosest(plan.Target, out surface)) { CancelForTransition(); yield break; }
            }
            else
            {
                // The live cart now really occupies the preflight translation.
                // Recheck fresh scene physics before the next actual hand contact.
                Physics.SyncTransforms();
                if (!EnsureMeshContacts().TryResolve(Cat, Vector3.zero, out var next)) { CancelForTransition(); yield break; }
                plan = next.Plan; surface = next.Surface;
            }
            if (!surface.IsValid) { CancelForTransition(); yield break; }
            ContactPoint = surface.Point;
            bool touched = false; float elapsed = 0; IsPushing = true;
            while (elapsed < .80f)
            {
                if (Time.deltaTime <= 0f) { yield return null; continue; }
                float t = elapsed / .80f; Cat.transform.SetPositionAndRotation(stand, facing);
                pawReach.Sample(this, plan, t);
                if (t > .28f && t < .72f && surface.IsValid)
                {
                    float actualDistance = Vector3.Distance(contact.LastContactPosition, surface.Point);
                    MinimumPawDistance = Mathf.Min(MinimumPawDistance, actualDistance);
                    if (!touched && contact.Distance < .028f && actualDistance < .028f)
                    { LastStrikePosition = contact.LastContactPosition; touched = true; GameAudio.Play(AudioCue.WoodTap, .7f); }
                }
                yield return null; elapsed += Time.deltaTime;
            }
            IsPushing = false; pawReach.Clear(); contact.Clear();
            if (!touched) { CancelForTransition(); yield break; }
            while (Time.deltaTime <= 0f) yield return null;
            ContactStrokes++;
            GameAudio.Play(AudioCue.WheelRoll, .8f);
            Vector3 begin = cartVisual.position, end = begin + roll * (RollDistance / ShoveCount);
            elapsed = 0;
            while (elapsed < .55f)
            {
                if (Time.deltaTime <= 0f) { yield return null; continue; }
                PlayCatPose(CatActivityPose.GentleKnead);
                float t = Mathf.Clamp01(elapsed / .55f); cartVisual.position = Vector3.Lerp(begin, end, 1 - (1 - t) * (1 - t));
                yield return null; elapsed += Time.deltaTime;
            }
            cartVisual.position = end; yield return new WaitForSeconds(.15f);
        }
        RestoreCat(); CompleteActivity("IT MOVES!");
    }

    private void RestoreCat()
    {
        IsPushing=false;pawReach?.Clear();contact?.Clear();
        if (cartVisual != null && visualHomeCaptured)
            cartVisual.localPosition = visualHome;
        if (Cat == null)
            return;

        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    protected override void CancelActivity()
    {
        if (!IsRunning) return;
        StopAllCoroutines();
        if (!HasBegunActivity) { base.CancelActivity(); return; }
        RestoreCat();
        base.CancelActivity();
    }

#if UNITY_EDITOR
    public void EditorConfigureNudge(
        Transform shove, Transform visual, Vector3 direction, float distance, int shoves)
    {
        shovePoint = shove;
        cartVisual = visual;
        rollDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        rollDistance = Mathf.Max(0.02f, distance);
        shoveCount = Mathf.Max(1, shoves);
    }
#endif
}
