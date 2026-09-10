using UnityEngine;

public enum CatLitterPhase { None, Entering, Investigating, Digging, Squatting, Covering, Exiting }

/// <summary>Sand-directed head and alternating paw strokes, over the breed's real skeleton.</summary>
[DefaultExecutionOrder(550)]
[DisallowMultipleComponent]
public sealed class CatLitterRoutineMotion : MonoBehaviour
{
    public const float StrokeSeconds = .82f;
    public const float MaximumLift = .052f;
    // Preserve a bent elbow over the deepest excavated surface; the old .072
    // endpoint exceeded the right foreleg's reachable target by .016 m.
    public const float MaximumReach = .055f;
    public const float ContactTolerance = .014f;
    public const float MaximumChestPitch = 14f;
    LitterDigActivity owner;
    CatLitterSandSurface surface;
    CatToyContactMotion contact;
    Transform left, right, head, chest, hips, leftRear, rightRear, leftArm, leftForearm, rightArm, rightForearm;
    Quaternion headPose, chestPose;
    bool headAdjusted, chestAdjusted, planted, hasHole, awaitingReplant;
    Vector3 leftPlant, rightPlant;
    float leftClearance, rightClearance, elapsed, duration;
    int lastEmittedStroke = -1;
    int pendingStroke, pendingFrame;
    bool pendingContact, pendingLeft;

    public bool IsActive => owner != null && owner.IsRunning;
    public CatLitterPhase Phase { get; private set; }
    public float LeftLift { get; private set; }
    public float RightLift { get; private set; }
    public float HeadPitch { get; private set; }
    public float ChestPitch { get; private set; }
    public float ChestShoulderDrop { get; private set; }
    public float RearSupportDisplacement { get; private set; }
    public float ForelegLengthChange { get; private set; }
    public Quaternion ChestSourceLocalRotation => chestPose;
    public float HeadHeightAboveSand => head != null && surface != null ? head.position.y - surface.SampleHeight(head.position) : 0f;
    public Vector3 LeftTarget { get; private set; }
    public Vector3 RightTarget { get; private set; }
    public Vector3 HoleTarget { get; private set; }
    public int ContactStrokes { get; private set; }
    public int ReplantCount { get; private set; }
    public Vector3 LastContactTarget { get; private set; }
    public bool LastContactWasLeft { get; private set; }
    public float LastContactDistance { get; private set; }
    public Vector3 LastContactPawPosition { get; private set; }
    public float LeftGeometricDeficit => ReachDeficit(LeftTarget, leftArm, leftForearm, left);
    public float RightGeometricDeficit => ReachDeficit(RightTarget, rightArm, rightForearm, right);
    public float RightDeficitBeforeSolve { get; private set; }
    public Vector3 RightShoulderBeforeSolve { get; private set; }
    public Vector3 RightShoulder => rightArm != null ? rightArm.position : transform.position;
    public string FailureReason { get; private set; }
    public string FailureReportPath { get; private set; }

    public void Sample(LitterDigActivity activity, CatLitterSandSurface sand, CatLitterPhase phase, float time, float total)
    {
        if (owner != activity)
        {
            Clear(); owner = activity; surface = sand;
            FailureReason = FailureReportPath = string.Empty;
            contact = GetComponent<CatToyContactMotion>() ?? gameObject.AddComponent<CatToyContactMotion>();
            var observer = GetComponent<CatLitterContactObserver>() ?? gameObject.AddComponent<CatLitterContactObserver>();
            observer.Motion = this;
            foreach (Transform bone in GetComponentsInChildren<Transform>())
            {
                if (bone.name == "DEF-hand.L") left = bone;
                if (bone.name == "DEF-hand.R") right = bone;
                if (bone.name == "DEF-spine.006") head = bone;
                if (bone.name == "DEF-spine.001") chest = bone;
                if (bone.name == "DEF-spine") hips = bone;
                if (bone.name == "DEF-foot.L") leftRear = bone;
                if (bone.name == "DEF-foot.R") rightRear = bone;
                if (bone.name == "DEF-upper_arm.L") leftArm = bone;
                if (bone.name == "DEF-forearm.L") leftForearm = bone;
                if (bone.name == "DEF-upper_arm.R") rightArm = bone;
                if (bone.name == "DEF-forearm.R") rightForearm = bone;
            }
        }
        if (Phase != phase) lastEmittedStroke = -1;
        awaitingReplant = false;
        Phase = phase; elapsed = time; duration = Mathf.Max(.001f, total);
    }

    public void PrepareCovering(LitterDigActivity activity)
    {
        if (owner != activity) return;
        // Let the settled standing clip supply a fresh neutral stance. Keeping
        // the old IK requested during this blend would capture its own old pose.
        contact?.Clear(); RestoreBodyPose();
        planted = false; awaitingReplant = true;
        LeftLift = RightLift = 0f; Phase = CatLitterPhase.Covering;
        lastEmittedStroke = -1;
    }

    void Update() => RestoreBodyPose();

    void LateUpdate()
    {
        pendingContact = false;
        if (!IsActive || awaitingReplant || surface == null || contact == null || left == null || right == null) return;
        if (!planted)
        {
            // The hand bone is above the paw sole. Retain that measured offset
            // instead of forcing a skeletal joint through the sand plane.
            leftPlant = surface.ClampPoint(left.position, .035f); rightPlant = surface.ClampPoint(right.position, .035f);
            leftClearance = Mathf.Clamp(left.position.y - surface.PlaneHeight, .012f, .08f);
            rightClearance = Mathf.Clamp(right.position.y - surface.PlaneHeight, .012f, .08f);
            if (!hasHole)
            {
                HoleTarget = surface.ClampPoint((leftPlant + rightPlant) * .5f + transform.forward * .035f, CatLitterSandSurface.HoleRadius * .75f);
                hasHole = true;
            }
            else ReplantCount++;
            planted = true;
        }

        float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed / .4f, (duration - elapsed) / .3f));
        bool strokes = Phase == CatLitterPhase.Digging || Phase == CatLitterPhase.Covering;
        int strokeIndex = Mathf.FloorToInt(elapsed / StrokeSeconds);
        float phase = Mathf.Repeat(elapsed / StrokeSeconds, 1f);
        bool leftStroke = strokeIndex % 2 == 0;
        float lift = strokes ? Mathf.Sin(Mathf.PI * Mathf.Clamp01(phase / .55f)) * MaximumLift * envelope : 0f;
        // First lift/reach; then touch and pull through the surface. The final
        // quiet quarter plants the foot before the opposite foot begins.
        if (phase >= .55f) lift = 0f;
        float reach = strokes ? (phase < .55f ? Mathf.Sin(phase / .55f * Mathf.PI * .5f) :
            Mathf.Lerp(1f, -.35f, Mathf.SmoothStep(0f, 1f, (phase - .55f) / .45f))) * MaximumReach * envelope : 0f;
        if (Phase == CatLitterPhase.Covering) reach = -reach;
        LeftLift = leftStroke ? lift : 0f; RightLift = leftStroke ? 0f : lift;
        float digAmount = Phase == CatLitterPhase.Digging ? Mathf.SmoothStep(0f, 1f, elapsed / duration) :
            Phase == CatLitterPhase.Covering ? 1f - Mathf.SmoothStep(0f, 1f, elapsed / duration) :
            Phase == CatLitterPhase.Squatting ? 1f : 0f;
        // Set this frame's real surface before sampling contact heights and
        // constraining the stroke to the current shoulder's reachable sphere.
        surface.Excavate(owner, HoleTarget, digAmount);
        if (Phase == CatLitterPhase.Squatting)
        {
            // The genuine sit transition supplies its own bent forelegs. A
            // standing foot plant cannot constrain those changed shoulders.
            contact.Clear();
            ChestPitch = ChestShoulderDrop = RearSupportDisplacement = ForelegLengthChange = 0f;
            ApplyHeadPose(envelope);
            LeftTarget = left.position; RightTarget = right.position;
            return;
        }
        LeftTarget = Target(leftPlant, leftClearance, leftStroke ? reach : 0f, LeftLift);
        RightTarget = Target(rightPlant, rightClearance, leftStroke ? 0f : reach, RightLift);
        // The ground-support pass has already placed the native standing pose.
        // Follow the excavation with the chest, leaving hips and rear paws in
        // that real support pose, before measuring either foreleg's reach.
        ApplyChestPose();
        ApplyHeadPose(envelope);
        bool leftReachable = TryReachableTarget(LeftTarget, leftArm, leftForearm, left, leftClearance, LeftLift, out Vector3 reachableLeft);
        bool rightReachable = TryReachableTarget(RightTarget, rightArm, rightForearm, right, rightClearance, RightLift, out Vector3 reachableRight);
        if (!leftReachable || !rightReachable)
        {
            // A changed/unusable tray pose must not stretch the rig or emit a
            // timed hit. Normal cancellation owns the sand and control cleanup.
            try { RecordUnreachable(leftReachable, rightReachable); }
            catch (System.Exception error) { FailureReason += "; diagnostic write failed: " + error.Message; }
            owner.CancelForTransition();
            return;
        }
        LeftTarget = reachableLeft; RightTarget = reachableRight;
        RightDeficitBeforeSolve = RightGeometricDeficit; RightShoulderBeforeSolve = rightArm.position;
        // The near-extended cover stroke converges slowly with the generic
        // eight-step toy solve. Only litter requests the larger bounded solve.
        contact.ReachBoth(LeftTarget, RightTarget, 1f, 32);
        // ReachBoth queues the targets; the shared solver runs at order 600.
        // Only the later observer may turn the rendered hand arrival into sand.
        pendingContact = strokes && phase >= .57f && lastEmittedStroke != strokeIndex && envelope > .1f;
        pendingStroke = strokeIndex; pendingLeft = leftStroke; pendingFrame = Time.frameCount;

    }

    void ApplyChestPose()
    {
        ChestShoulderDrop = RearSupportDisplacement = ForelegLengthChange = 0f;
        if (chest == null || leftArm == null || rightArm == null || leftForearm == null || rightForearm == null ||
            !leftArm.IsChildOf(chest) || !rightArm.IsChildOf(chest)) return;
        chestPose = chest.localRotation; chestAdjusted = true;
        Quaternion neutral = chest.rotation;
        Vector3 leftShoulder = leftArm.position, rightShoulder = rightArm.position;
        Vector3 hipsBefore = hips != null ? hips.position : transform.position;
        Vector3 leftRearBefore = leftRear != null ? leftRear.position : transform.position;
        Vector3 rightRearBefore = rightRear != null ? rightRear.position : transform.position;
        float leftLength = LimbLength(leftArm, leftForearm, left), rightLength = LimbLength(rightArm, rightForearm, right);
        float desired = 0f;
        // Do not bend just to satisfy the optional .965 elbow margin. Bend only
        // when the real requested sand target needs more than .995 of the limb.
        if (ChestReachError(leftLength, rightLength, .995f) > 0f)
        {
            float bestError = ChestReachError(leftLength, rightLength, .985f), previous = 0f;
            for (float angle = 1f; angle <= MaximumChestPitch; angle += 1f)
            {
                chest.rotation = Quaternion.AngleAxis(angle, transform.right) * neutral;
                float error = ChestReachError(leftLength, rightLength, .985f);
                if (error < bestError) { bestError = error; desired = angle; }
                if (error <= 0f)
                {
                    float low = previous, high = angle;
                    for (int iteration = 0; iteration < 9; iteration++)
                    {
                        float middle = (low + high) * .5f;
                        chest.rotation = Quaternion.AngleAxis(middle, transform.right) * neutral;
                        if (ChestReachError(leftLength, rightLength, .985f) <= 0f) high = middle;
                        else low = middle;
                    }
                    desired = high; break;
                }
                previous = angle;
            }
        }
        // Increasing support correction must reach this frame's real surface;
        // the gradually excavated mesh makes that correction gradual too. Ease
        // its release, and preserve the identical pose while scaled time stops.
        if (Time.deltaTime > 0f)
            ChestPitch = Mathf.Max(desired, Mathf.MoveTowards(ChestPitch, desired, 75f * Time.deltaTime));
        chest.rotation = Quaternion.AngleAxis(ChestPitch, transform.right) * neutral;
        ChestShoulderDrop = ((leftShoulder.y - leftArm.position.y) + (rightShoulder.y - rightArm.position.y)) * .5f;
        RearSupportDisplacement = Mathf.Max(Vector3.Distance(hipsBefore, hips != null ? hips.position : transform.position),
            Mathf.Max(Vector3.Distance(leftRearBefore, leftRear != null ? leftRear.position : transform.position),
                Vector3.Distance(rightRearBefore, rightRear != null ? rightRear.position : transform.position)));
        ForelegLengthChange = Mathf.Max(Mathf.Abs(leftLength - LimbLength(leftArm, leftForearm, left)),
            Mathf.Abs(rightLength - LimbLength(rightArm, rightForearm, right)));
    }

    float ChestReachError(float leftLength, float rightLength, float margin) => Mathf.Max(
        Vector3.Distance(leftArm.position, LeftTarget) - leftLength * margin,
        Vector3.Distance(rightArm.position, RightTarget) - rightLength * margin);

    static float LimbLength(Transform arm, Transform forearm, Transform hand) =>
        Vector3.Distance(arm.position, forearm.position) + Vector3.Distance(forearm.position, hand.position);

    void ApplyHeadPose(float envelope)
    {
        if (head != null)
        {
            headPose = head.localRotation; headAdjusted = true;
            float targetPitch = Phase == CatLitterPhase.Squatting ? 8f : 30f;
            float desiredPitch = targetPitch * (Phase == CatLitterPhase.Investigating ? Mathf.SmoothStep(0f, 1f, elapsed / .55f) :
                Phase == CatLitterPhase.Digging ? 1f : envelope);
            HeadPitch = Mathf.MoveTowards(HeadPitch, desiredPitch, 75f * Time.deltaTime);
            head.rotation = Quaternion.AngleAxis(HeadPitch, transform.right) * head.rotation;
        }
    }

    internal void ObserveSolvedContact()
    {
        if (!isActiveAndEnabled || !IsActive || awaitingReplant || !pendingContact ||
            pendingFrame != Time.frameCount || Time.deltaTime <= 0f || surface == null) return;
        pendingContact = false;
        Transform hand = pendingLeft ? left : right;
        if (hand == null) return;
        Vector3 point = pendingLeft ? LeftTarget : RightTarget;
        Vector3 toHole = point - HoleTarget; toHole.y = 0f;
        float distance = Vector3.Distance(hand.position, point);
        if (distance >= ContactTolerance || toHole.magnitude > CatLitterSandSurface.HoleRadius * 1.35f) return;
        lastEmittedStroke = pendingStroke; ContactStrokes++;
        LastContactTarget = point; LastContactWasLeft = pendingLeft;
        LastContactPawPosition = hand.position; LastContactDistance = distance;
        surface.Scatter(owner, point, transform.forward * (Phase == CatLitterPhase.Covering ? 1f : -1f));
    }

    Vector3 Target(Vector3 plant, float clearance, float reach, float lift)
    {
        Vector3 point = surface.ClampPoint(plant + transform.forward * reach, .025f);
        point.y = surface.SampleHeight(point) + clearance + lift;
        return point;
    }

    bool TryReachableTarget(Vector3 point, Transform arm, Transform forearm, Transform hand, float clearance, float lift, out Vector3 target)
    {
        target = point;
        if (arm == null || forearm == null || hand == null) return false;
        float span = Vector3.Distance(arm.position, forearm.position) + Vector3.Distance(forearm.position, hand.position);
        // Prefer the bent-elbow stroke. A planted natural foreleg can already
        // exceed that optional bend margin, while remaining physically inside
        // its real two-bone reach. Retry this SAME sand surface with a small
        // nonzero extension margin; never stretch bones or raise the target.
        return TryReachableSurfaceTarget(point, arm.position, span * .965f, clearance, lift, out target) ||
            TryReachableSurfaceTarget(point, arm.position, span * .995f, clearance, lift, out target);
    }

    bool TryReachableSurfaceTarget(Vector3 point, Vector3 shoulder, float span, float clearance, float lift, out Vector3 target)
    {
        target = point;
        float squareSpan = span * span;
        // Preserve height above the actual deformed surface and the full lift;
        // only shorten the horizontal reach enough to keep a naturally bent elbow.
        for (int iteration = 0; iteration < 32; iteration++)
        {
            if ((point - shoulder).sqrMagnitude <= squareSpan) { target = point; return true; }
            float vertical = point.y - shoulder.y;
            if (Mathf.Abs(vertical) >= span) break;
            float horizontal = Mathf.Sqrt(span * span - vertical * vertical);
            Vector3 delta = point - shoulder; delta.y = 0f;
            if (delta.sqrMagnitude <= horizontal * horizontal) break;
            Vector3 candidate = shoulder + delta.normalized * horizontal;
            point = surface.ClampPoint(candidate, .025f);
            point.y = surface.SampleHeight(point) + clearance + lift;
        }
        // Clamping a sphere projection back to the sand rectangle can put it
        // outside the sphere again. Find a VERIFIED feasible point on this same
        // surface, then retain as much of the requested stroke as possible.
        Vector3 nearest = surface.ClampPoint(shoulder, .025f);
        Vector3 inside = nearest; bool found = false; float closest = float.PositiveInfinity;
        for (int ring = 0; ring <= 6; ring++)
        for (int angle = 0; angle < (ring == 0 ? 1 : 12); angle++)
        {
            Vector3 candidate = surface.ClampPoint(nearest + Quaternion.Euler(0f, angle * 30f, 0f) * Vector3.forward * (ring * .025f), .025f);
            candidate.y = surface.SampleHeight(candidate) + clearance + lift;
            float distance = (candidate - point).sqrMagnitude;
            if ((candidate - shoulder).sqrMagnitude > squareSpan || distance >= closest) continue;
            inside = candidate; closest = distance; found = true;
        }
        if (!found) return false;
        Vector3 outside = point;
        for (int iteration = 0; iteration < 16; iteration++)
        {
            Vector3 candidate = surface.ClampPoint((inside + outside) * .5f, .025f);
            candidate.y = surface.SampleHeight(candidate) + clearance + lift;
            if ((candidate - shoulder).sqrMagnitude <= squareSpan) inside = candidate;
            else outside = candidate;
        }
        target = inside;
        return (target - shoulder).sqrMagnitude <= squareSpan;
    }

    static float ReachDeficit(Vector3 target, Transform arm, Transform forearm, Transform hand)
    {
        if (arm == null || forearm == null || hand == null) return float.PositiveInfinity;
        return Vector3.Distance(arm.position, target) - Vector3.Distance(arm.position, forearm.position) -
            Vector3.Distance(forearm.position, hand.position);
    }

    void RecordUnreachable(bool leftReachable, bool rightReachable)
    {
        FailureReason = "Litter reach unavailable: " + Phase + " t=" + elapsed.ToString("F5") +
            "; left=" + leftReachable + "; right=" + rightReachable +
            "; targets=" + LeftTarget.ToString("F5") + "/" + RightTarget.ToString("F5") +
            "; deficit=" + LeftGeometricDeficit.ToString("F5") + "/" + RightGeometricDeficit.ToString("F5") +
            "; lift=" + LeftLift.ToString("F5") + "/" + RightLift.ToString("F5") + "; chest=" + ChestPitch.ToString("F5");
#if UNITY_EDITOR
        var dump = new ReachFailure
        {
            phase = Phase.ToString(), elapsed = elapsed, duration = duration,
            reason = FailureReason, breed = CatBreedService.SelectedBreedId,
            rootPosition = transform.position, rootRotation = transform.rotation, rootScale = transform.lossyScale,
            hole = HoleTarget, depth = surface.Depth, plane = surface.PlaneHeight,
            chestPitch = ChestPitch, shoulderDrop = ChestShoulderDrop, rearSupportDisplacement = RearSupportDisplacement,
            forelegLengthChange = ForelegLengthChange, chestPosition = chest != null ? chest.position : Vector3.zero,
            chestSourceLocalRotation = chestPose,
            left = CaptureLimb(leftArm, leftForearm, left, LeftTarget, leftPlant, leftClearance, LeftLift, leftReachable),
            right = CaptureLimb(rightArm, rightForearm, right, RightTarget, rightPlant, rightClearance, RightLift, rightReachable),
            sandPosition = surface.Sand.transform.position, sandRotation = surface.Sand.transform.rotation,
            sandScale = surface.Sand.transform.lossyScale, sandLocalToWorld = surface.Sand.transform.localToWorldMatrix,
            sandWorldToLocal = surface.Sand.transform.worldToLocalMatrix
        };
        const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        dump.sandLocalCenter = (Vector3)typeof(CatLitterSandSurface).GetField("localCenter", fields).GetValue(surface);
        dump.sandLocalSize = (Vector2)typeof(CatLitterSandSurface).GetField("localSize", fields).GetValue(surface);
        string folder = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Library/CameraFacingAudit"));
        System.IO.Directory.CreateDirectory(folder);
        FailureReportPath = System.IO.Path.Combine(folder, "litter-reach-failure-" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".json");
        System.IO.File.WriteAllText(FailureReportPath, JsonUtility.ToJson(dump, true));
#endif
    }

#if UNITY_EDITOR
    [System.Serializable] sealed class LimbFailure
    {
        public Vector3 shoulder, forearm, hand, target, plant, nearestSurfaceToShoulder;
        public float upperLength, foreLength, clearance, lift, nearestHeight, targetHeight;
        public bool reachable;
    }
    [System.Serializable] sealed class ReachFailure
    {
        public string reason, phase, breed;
        public float elapsed, duration, depth, plane, chestPitch, shoulderDrop, rearSupportDisplacement, forelegLengthChange;
        public Vector3 rootPosition, rootScale, hole, sandPosition, sandScale, sandLocalCenter, chestPosition;
        public Vector2 sandLocalSize;
        public Quaternion rootRotation, sandRotation, chestSourceLocalRotation;
        public Matrix4x4 sandLocalToWorld, sandWorldToLocal;
        public LimbFailure left, right;
    }
    LimbFailure CaptureLimb(Transform arm, Transform forearm, Transform hand, Vector3 target, Vector3 plant, float clearance, float lift, bool reachable)
    {
        Vector3 nearest = surface.ClampPoint(arm.position, .025f);
        return new LimbFailure { shoulder = arm.position, forearm = forearm.position, hand = hand.position,
            target = target, plant = plant, nearestSurfaceToShoulder = nearest,
            upperLength = Vector3.Distance(arm.position, forearm.position), foreLength = Vector3.Distance(forearm.position, hand.position),
            clearance = clearance, lift = lift, nearestHeight = surface.SampleHeight(nearest), targetHeight = surface.SampleHeight(target), reachable = reachable };
    }
#endif

    void RestoreHead()
    {
        if (!headAdjusted) return;
        if (head != null) head.localRotation = headPose;
        headAdjusted = false;
    }

    void RestoreBodyPose()
    {
        RestoreHead();
        if (!chestAdjusted) return;
        if (chest != null) chest.localRotation = chestPose;
        chestAdjusted = false;
    }

    public void Clear()
    {
        RestoreBodyPose();
        if (owner != null && contact != null) contact.Clear();
        owner = null; surface = null; contact = null; left = right = head = null;
        chest = hips = leftRear = rightRear = null;
        leftArm = leftForearm = rightArm = rightForearm = null;
        planted = hasHole = awaitingReplant = false; ReplantCount = 0;
        pendingContact = false;
        Phase = CatLitterPhase.None; LeftLift = RightLift = HeadPitch = ChestPitch = 0f;
        ChestShoulderDrop = RearSupportDisplacement = ForelegLengthChange = 0f;
        ContactStrokes = 0; lastEmittedStroke = -1;
    }

    void OnDisable() => Clear();
}

// Keep target construction before CatToyContactMotion (600), and contact
// acceptance after it. This observer is created once on the same cat and is
// inert whenever its owner's routine is paused, disabled or cancelled.
[DefaultExecutionOrder(650)]
[DisallowMultipleComponent]
public sealed class CatLitterContactObserver : MonoBehaviour
{
    internal CatLitterRoutineMotion Motion;
    void LateUpdate() { if (Motion != null) Motion.ObserveSolvedContact(); }
}
