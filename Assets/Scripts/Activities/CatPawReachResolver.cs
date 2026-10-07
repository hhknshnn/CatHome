using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A measured source-pose reach, shared by readiness and the actual hand solve.</summary>
public struct CatPawReachPlan
{
    public CatActivityPose Pose;
    public float SourcePhase, ChestPitch, ChestYaw, ReachMargin;
    public Vector3 LeftTarget, RightTarget;
    public int[] LeftPawVertices, RightPawVertices;
    public bool Left, Both;
    public CatPawSurfacePlan Surface;
    public Vector3 Target => Left ? LeftTarget : RightTarget;
}

/// <summary>Pure geometry over baked source bones. Never samples or moves the live actor.</summary>
public static partial class CatPawReachResolver
{
    public const float MaximumChestPitch = 32f;
    public const float MaximumChestYaw = 35f;
    public const int ScratchSolveIterations = 20;
    public const float ScratchCorrectionDamping = .65f;
    static readonly CatBodyGuardCatalog.Probe[] worldProbes = new CatBodyGuardCatalog.Probe[3];
    static readonly CatBodyGuardCatalog.Probe[] singleProbe = new CatBodyGuardCatalog.Probe[1];
    static readonly Bend[] bends = CreateBends();
    static readonly Dictionary<CatPawReachCatalog.Entry, SourcePose> sourceCache = new Dictionary<CatPawReachCatalog.Entry, SourcePose>();
    static readonly Dictionary<CatPawReachCatalog.Entry, IndexedPaws> indexedPawCache = new Dictionary<CatPawReachCatalog.Entry, IndexedPaws>();
    static CatMovement cachedActor;
    static CatBreedVisualTag cachedVisual;
    static Matrix4x4 cachedMatrix;
    static CatMovement scratchSearchActor;
    static long scratchSearchId;

    // Physics results belong to one synchronous readiness query, never a frame.
    // A later query in the same frame must see a newly inserted blocker.
    public readonly struct ScratchSearchScope : IDisposable
    {
        readonly long id;
        internal ScratchSearchScope(long value) { id = value; }
        public void Dispose() { if (scratchSearchId == id) scratchSearchActor = null; }
    }
    public static ScratchSearchScope BeginScratchSearch(CatMovement actor)
    {
        scratchSearchActor = actor;
        scratchSearchId++;
        foreach (var source in sourceCache.Values)
        {
            source.scratchBodyClearance.Clear();
            source.scratchFixedChecked = false;
            source.neutralSkinReady = false;
        }
        return new ScratchSearchScope(scratchSearchId);
    }

    struct Bend { public float pitch, yaw, cost; }
    sealed class IndexedPaws
    {
        public CatPawReachCatalog.PawVertex[] left, right;
    }
    struct WorldArm
    {
        public Vector3 upper, fore, hand;
        public float reach, minimum, pawExtent;
        public CatPawWeightedSkin.Frame skin;
        public int[] sourceTipPatch;
    }
    sealed class WorldSample
    {
        public float phase;
        public Vector3 pivot;
        public WorldArm left, right;
        public CatBodyGuardCatalog.Probe pelvis;
        public CatBodyGuardCatalog.Probe[] upper;
    }
    sealed class SourcePose
    {
        public WorldSample[] all;
        public WorldSample[] contacts;
        // Geometry plans survive stationary prompts. Every returned plan still
        // runs fresh physics: moving props cannot inherit an earlier approval.
        public readonly Dictionary<Request, CatPawReachPlan> plans = new Dictionary<Request, CatPawReachPlan>();
        public readonly Dictionary<Request, CatPawReachPlan> surfacePlans = new Dictionary<Request, CatPawReachPlan>();
        public readonly HashSet<Request> mathematicallyImpossible = new HashSet<Request>();
        public readonly Dictionary<BodyRequest, bool> scratchBodyClearance = new Dictionary<BodyRequest, bool>();
        public bool scratchFixedChecked, scratchFixedClear;
        public Vector3[] neutralLeft, neutralRight;
        public bool neutralSkinReady;
        public readonly Dictionary<string, Transform> neutralBones = new Dictionary<string, Transform>();
    }
    struct BodyRequest : IEquatable<BodyRequest>
    {
        public float phase, pitch, yaw;
        public bool Equals(BodyRequest other) => phase == other.phase && pitch == other.pitch && yaw == other.yaw;
        public override bool Equals(object other) => other is BodyRequest key && Equals(key);
        public override int GetHashCode() => phase.GetHashCode() ^ pitch.GetHashCode() * 17 ^ yaw.GetHashCode() * 31;
    }
    struct Request : IEquatable<Request>
    {
        public Vector3 a, b, c, d;
        public bool left, both, sweep, scratchSkin;
        public int left0, left1, left2, right0, right1, right2;
        public float pitch, yaw, minimumMargin;
        public bool Equals(Request other) => a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c) && d.Equals(other.d) &&
            left == other.left && both == other.both && sweep == other.sweep && pitch == other.pitch && yaw == other.yaw &&
            minimumMargin == other.minimumMargin &&
            scratchSkin == other.scratchSkin && left0 == other.left0 && left1 == other.left1 && left2 == other.left2 &&
            right0 == other.right0 && right1 == other.right1 && right2 == other.right2;
        public override bool Equals(object other) => other is Request key && Equals(key);
        public override int GetHashCode() => a.GetHashCode() ^ b.GetHashCode() * 17 ^ c.GetHashCode() * 31 ^ d.GetHashCode() * 43 ^
            (left ? 101 : 0) ^ (both ? 211 : 0) ^ (sweep ? 307 : 0) ^ pitch.GetHashCode() ^ yaw.GetHashCode() ^
            minimumMargin.GetHashCode() * 71 ^
            (scratchSkin ? 401 : 0) ^ left0 * 73 ^ left1 * 79 ^ left2 * 83 ^ right0 * 89 ^ right1 * 97 ^ right2 * 103;
    }

    public static bool TryResolve(CatMovement actor, Vector3 target, bool left,
        CatActivityPose pose, out CatPawReachPlan plan, float maxPitch = MaximumChestPitch, float maxYaw = 0f)
        => Resolve(actor, target, target, left, false, pose, maxPitch, maxYaw, out plan);
    public static bool TryResolveBoth(CatMovement actor, Vector3 left, Vector3 right,
        CatActivityPose pose, out CatPawReachPlan plan, float maxPitch = MaximumChestPitch, float maxYaw = 0f)
        => Resolve(actor, left, right, true, true, pose, maxPitch, maxYaw, out plan);
    public static bool TryResolveSingleSweep(CatMovement actor, Vector3 from, Vector3 to, bool left,
        CatActivityPose pose, out CatPawReachPlan plan, float maxPitch = MaximumChestPitch, float maxYaw = MaximumChestYaw,
        float minimumReachMargin = .004f)
        => Resolve(actor, from, from, left, false, pose, maxPitch, maxYaw, out plan,
            left ? (Vector3?)to : null, left ? null : (Vector3?)to, minimumReachMargin: minimumReachMargin);
    public static bool TryResolveSweep(CatMovement actor, Vector3 leftA, Vector3 leftB,
        Vector3 rightA, Vector3 rightB, CatActivityPose pose, out CatPawReachPlan plan,
        float maxPitch = MaximumChestPitch, float maxYaw = 0f)
        => Resolve(actor, leftA, rightA, true, true, pose, maxPitch, maxYaw, out plan, leftB, rightB);

    // Both paws share one source/chest pose and keep their measured skin patch
    // throughout the sweep. Hips, rear supports and arm lengths stay authored.
    public static bool TryResolveScratchSweep(CatMovement actor, Vector3 leftA, Vector3 leftB,
        Vector3 rightA, Vector3 rightB, out CatPawReachPlan plan,
        float maxPitch = MaximumChestPitch, float maxYaw = 10f, float minimumReachMargin = .004f)
        => Resolve(actor, leftA, rightA, true, true, CatActivityPose.Scratch, maxPitch, maxYaw, out plan,
            leftB, rightB, minimumReachMargin: minimumReachMargin, scratchSkin: true);

    public static bool TryResolveScratchSweepAt(CatMovement actor, Quaternion heading,
        Vector3 leftA, Vector3 leftB, Vector3 rightA, Vector3 rightB, out CatPawReachPlan plan,
        float minimumReachMargin)
        => Resolve(actor, leftA, rightA, true, true, CatActivityPose.Scratch, 32f, 10f, out plan,
            leftB, rightB, actor.transform.position, heading, minimumReachMargin, true);

    // Planned activity movement can be checked without moving the live actor.
    public static bool TryResolveAt(CatMovement actor, Vector3 position, Quaternion rotation,
        Vector3 target, bool left, CatActivityPose pose, out CatPawReachPlan plan)
        => Resolve(actor, target, target, left, false, pose, MaximumChestPitch, 0f,
            out plan, null, null, position, rotation);

    public static bool IsSourcePoseClear(CatMovement actor, CatActivityPose pose) => actor != null &&
        IsSourcePoseClearAt(actor, actor.transform.position, actor.transform.rotation, pose);

    public static bool IsSourcePoseClearAt(CatMovement actor, Vector3 position, Quaternion rotation, CatActivityPose pose)
    {
        if (actor == null) return false;
        var visual = actor.GetComponentInChildren<CatBreedVisualTag>();
        var entry = visual != null ? CatPawReachCatalog.Load()?.Find(visual.BreedId, pose) : null;
        if (entry?.samples == null || entry.samples.Length == 0) return false;
        var source = TransformSource(entry, MatrixAt(actor, visual.transform, position, rotation), visual.transform.lossyScale);
        if (source == null || !FixedTrajectoryClear(actor, source)) return false;
        foreach (var sample in source.all)
            if (!UpperBodyClear(actor, sample, Quaternion.identity)) return false;
        return true;
    }

    static Matrix4x4 MatrixAt(CatMovement actor, Transform visual, Vector3 position, Quaternion rotation) =>
        Matrix4x4.TRS(position, rotation, Vector3.one) *
        Matrix4x4.TRS(actor.transform.position, actor.transform.rotation, Vector3.one).inverse * visual.localToWorldMatrix;

    static bool Resolve(CatMovement actor, Vector3 leftTarget, Vector3 rightTarget, bool left, bool both,
        CatActivityPose pose, float maxPitch, float maxYaw, out CatPawReachPlan plan,
        Vector3? leftEnd = null, Vector3? rightEnd = null, Vector3? plannedPosition = null, Quaternion? plannedRotation = null,
        float minimumReachMargin = .004f, bool scratchSkin = false)
    {
        plan = default;
        if (actor == null) return false;
        var visual = actor.GetComponentInChildren<CatBreedVisualTag>();
        var entry = visual == null ? null : scratchSkin ? CatPawReachCatalog.LoadSurface(visual.BreedId, pose, out _) :
            CatPawReachCatalog.Load()?.Find(visual.BreedId, pose);
        if (entry?.samples == null || entry.samples.Length == 0) return false;
        Vector3 position = plannedPosition ?? actor.transform.position;
        Quaternion facing = plannedRotation ?? actor.transform.rotation;
        Vector3 right = facing * Vector3.right;
        var matrix = MatrixAt(actor, visual.transform, position, facing);
        if (cachedActor != actor || cachedVisual != visual || !cachedMatrix.Equals(matrix))
        {
            sourceCache.Clear(); cachedActor = actor; cachedVisual = visual; cachedMatrix = matrix;
        }
        if (!sourceCache.TryGetValue(entry, out var source))
        {
            source = TransformSource(entry, matrix, visual.transform.lossyScale);
            if (source == null) return false;
            sourceCache.Add(entry, source);
        }
        var request = new Request { a = leftTarget, b = rightTarget, c = leftEnd.GetValueOrDefault(), d = rightEnd.GetValueOrDefault(),
            left = left, both = both, sweep = leftEnd.HasValue || rightEnd.HasValue,
            pitch = Mathf.Clamp(maxPitch, 0, MaximumChestPitch), yaw = Mathf.Clamp(maxYaw, 0, MaximumChestYaw),
            minimumMargin = Mathf.Max(.004f,minimumReachMargin),
            scratchSkin = scratchSkin };
        int[] leftPatch = null, rightPatch = null;
        if (scratchSkin)
        {
            if (!PrepareNeutralSkin(actor, entry, source, scratchSearchActor == actor)) return false;
            if (plannedRotation.HasValue)
            {
                var delta = Matrix4x4.TRS(position, facing, Vector3.one) *
                    Matrix4x4.TRS(actor.transform.position, actor.transform.rotation, Vector3.one).inverse;
                for (int i = 0; i < source.neutralLeft.Length; i++) source.neutralLeft[i] = delta.MultiplyPoint3x4(source.neutralLeft[i]);
                for (int i = 0; i < source.neutralRight.Length; i++) source.neutralRight[i] = delta.MultiplyPoint3x4(source.neutralRight[i]);
                source.neutralSkinReady = false;
            }
            leftPatch = ClosestDistalPatch(entry.leftPaw, source.neutralLeft, leftTarget);
            rightPatch = ClosestDistalPatch(entry.rightPaw, source.neutralRight, rightTarget);
            if (leftPatch == null || rightPatch == null) return false;
            request.left0 = leftPatch[0]; request.left1 = leftPatch[1]; request.left2 = leftPatch[2];
            request.right0 = rightPatch[0]; request.right1 = rightPatch[1]; request.right2 = rightPatch[2];
        }
        float yawSign = Mathf.Sign(Vector3.Dot((left ? leftTarget : rightTarget) - position, right));
        if (source.mathematicallyImpossible.Contains(request)) return false;
        bool memoPhysics = scratchSearchActor == actor && pose == CatActivityPose.Scratch && scratchSkin;
        // Every request retains the unbent source boundary checks.
        bool fixedChecked = false;
        if (source.plans.TryGetValue(request, out var previous) && previous.ReachMargin >= request.minimumMargin)
        {
            if (!QueryFixedClear(actor, source, memoPhysics)) return false;
            fixedChecked = true;
            if (QueryBodyClear(actor, source, previous.SourcePhase, previous.ChestPitch, previous.ChestYaw, right, memoPhysics))
            { plan = previous; return true; }
        }
        bool hasMathematicalCandidate = false;
        foreach (var bend in bends)
        {
            if (bend.pitch > request.pitch || bend.yaw > request.yaw) continue;
            Quaternion rotation = Quaternion.AngleAxis(bend.yaw * yawSign, Vector3.up) * Quaternion.AngleAxis(bend.pitch, right);
            foreach (var sample in source.contacts)
            {
                float margin;
                int[] candidateLeftPatch = leftPatch, candidateRightPatch = rightPatch;
                if (scratchSkin)
                {
                    // Distal extent is a broad phase only. The final reserve is
                    // measured at the solved wrist with unchanged bone lengths.
                    if (!ScratchEndpointsWithPatch(sample, sample.left, true, leftPatch, leftTarget, leftEnd,
                        rotation, facing * Vector3.forward, request.minimumMargin, out float leftMargin, out candidateLeftPatch) ||
                        !ScratchEndpointsWithPatch(sample, sample.right, false, rightPatch, rightTarget, rightEnd,
                        rotation, facing * Vector3.forward, request.minimumMargin, out float rightMargin, out candidateRightPatch)) continue;
                    margin = Mathf.Min(leftMargin, rightMargin);
                }
                else
                {
                    // The shoulder is identical for both endpoints of an arm.
                    // Reject all squared annuli before paying for any square root.
                    float leftSquared = 0f, rightSquared = 0f;
                    bool useLeft = both || left, useRight = both || !left;
                    if (useLeft)
                    {
                        Vector3 shoulder = sample.pivot + rotation * (sample.left.upper - sample.pivot);
                        if (!ReachEndpointsSquared(sample.left, shoulder, leftTarget, leftEnd, request.minimumMargin, out leftSquared)) continue;
                    }
                    if (useRight)
                    {
                        Vector3 shoulder = sample.pivot + rotation * (sample.right.upper - sample.pivot);
                        if (!ReachEndpointsSquared(sample.right, shoulder, rightTarget, rightEnd, request.minimumMargin, out rightSquared)) continue;
                    }
                    margin = useLeft ? sample.left.reach - Mathf.Sqrt(leftSquared) : float.PositiveInfinity;
                    if (useRight) margin = Mathf.Min(margin, sample.right.reach - Mathf.Sqrt(rightSquared));
                    if (margin < request.minimumMargin) continue;
                }
                hasMathematicalCandidate = true;
                if (!fixedChecked)
                {
                    if (!QueryFixedClear(actor, source, memoPhysics)) return false;
                    fixedChecked = true;
                }
                if (!QueryBodyClear(actor, source, sample.phase, bend.pitch, bend.yaw * yawSign, right, memoPhysics)) continue;
                plan = new CatPawReachPlan { Pose = pose, SourcePhase = sample.phase, ChestPitch = bend.pitch,
                    ChestYaw = bend.yaw * yawSign, ReachMargin = margin, Left = left, Both = both,
                    LeftTarget = leftTarget, RightTarget = rightTarget, LeftPawVertices = candidateLeftPatch, RightPawVertices = candidateRightPatch };
                if (source.plans.Count >= 64) source.plans.Clear();
                source.plans[request] = plan;
                return true; // Smallest chest-bend cost first.
            }
        }
        if (!hasMathematicalCandidate)
        {
            if (source.mathematicallyImpossible.Count >= 128) source.mathematicallyImpossible.Clear();
            source.mathematicallyImpossible.Add(request);
        }
        return false;
    }

    static bool PrepareNeutralSkin(CatMovement actor, CatPawReachCatalog.Entry entry, SourcePose source, bool memo)
    {
        if (memo && source.neutralSkinReady) return true;
        var animator = actor.GetComponentInChildren<Animator>();
        if (animator == null) return false;
        if (!ReadNeutralSkin(entry.leftPaw, animator.transform, source, ref source.neutralLeft) ||
            !ReadNeutralSkin(entry.rightPaw, animator.transform, source, ref source.neutralRight)) return false;
        source.neutralSkinReady = memo;
        return true;
    }
    static bool ReadNeutralSkin(CatPawReachCatalog.PawVertex[] definitions, Transform root, SourcePose source, ref Vector3[] points)
    {
        if (definitions == null || definitions.Length < 3) return false;
        if (points == null || points.Length != definitions.Length) points = new Vector3[definitions.Length];
        for (int v = 0; v < definitions.Length; v++)
        {
            if (!definitions[v].distal) continue;
            Vector3 point = Vector3.zero; float total = 0f;
            foreach (var influence in definitions[v].influences)
            {
                if (!source.neutralBones.TryGetValue(influence.bonePath, out var bone))
                { bone = root.Find(influence.bonePath); source.neutralBones[influence.bonePath] = bone; }
                if (bone == null) return false;
                point += bone.TransformPoint(influence.bindPosition) * influence.weight; total += influence.weight;
            }
            if (Mathf.Abs(total - 1f) > .0001f) return false;
            points[v] = point;
        }
        return true;
    }
    static int[] ClosestDistalPatch(CatPawReachCatalog.PawVertex[] definitions, Vector3[] points, Vector3 target)
    {
        var indices = new[] { -1, -1, -1 };
        float first = float.PositiveInfinity, second = first, third = first;
        for (int v = 0; v < definitions.Length; v++)
        {
            if (!definitions[v].distal) continue;
            float distance = (points[v] - target).sqrMagnitude;
            if (distance < first)
            { third = second; second = first; first = distance; indices[2] = indices[1]; indices[1] = indices[0]; indices[0] = v; }
            else if (distance < second)
            { third = second; second = distance; indices[2] = indices[1]; indices[1] = v; }
            else if (distance < third) { third = distance; indices[2] = v; }
        }
        return indices[2] < 0 ? null : indices;
    }
    struct ScratchPatch
    {
        public Vector3 unchanged, upper, fore;
        public float upperWeight, foreWeight;
        public Vector3 SolvedMean(CatPawSurfaceCcd.State solve, Vector3 sourceUpper, Vector3 sourceFore) => unchanged +
            solve.Upper * upperWeight + solve.UpperRotation * (upper - sourceUpper * upperWeight) +
            solve.Fore * foreWeight + solve.PawRotation * (fore - sourceFore * foreWeight);
    }
    static Vector3 ScratchBodyPoint(WorldSample sample, Quaternion chest, Vector3 point) =>
        sample.pivot + chest * (point - sample.pivot);
    static bool ScratchEndpointsWithPatch(WorldSample sample, WorldArm arm, bool left, int[] preferred, Vector3 target, Vector3? end,
        Quaternion chest, Vector3 forward, float requiredMargin, out float margin, out int[] vertices)
    {
        vertices = preferred;
        if (ScratchEndpoints(sample, arm, left, preferred, target, end, chest, forward, requiredMargin, out margin)) return true;
        // A high board can make neutral proximity choose the back of a paw.
        // Its measured source toe is a second coherent patch, selected once
        // for this whole sweep and carried unchanged into the live binding.
        vertices = arm.sourceTipPatch;
        return ScratchEndpoints(sample, arm, left, vertices, target, end, chest, forward, requiredMargin, out margin);
    }
    static bool ScratchEndpoints(WorldSample sample, WorldArm arm, bool left, int[] vertices, Vector3 target, Vector3? end,
        Quaternion chest, Vector3 forward, float requiredMargin, out float margin)
    {
        margin = float.NegativeInfinity;
        if (arm.skin == null || vertices == null) return false;
        Vector3 upper = ScratchBodyPoint(sample, chest, arm.upper);
        // A real skin extent can reject distant requests, but never lengthens an arm.
        float maximum = Vector3.Distance(arm.upper, arm.fore) + Vector3.Distance(arm.fore, arm.hand) + arm.pawExtent;
        if ((target - upper).sqrMagnitude > maximum * maximum ||
            (end.HasValue && (end.Value - upper).sqrMagnitude > maximum * maximum)) return false;
        Vector3 fore = ScratchBodyPoint(sample, chest, arm.fore);
        Vector3 hand = ScratchBodyPoint(sample, chest, arm.hand);
        var patch = new ScratchPatch();
        var upperMotion = left ? CatPawReachCatalog.SkinMotion.LeftUpper : CatPawReachCatalog.SkinMotion.RightUpper;
        var foreMotion = left ? CatPawReachCatalog.SkinMotion.LeftFore : CatPawReachCatalog.SkinMotion.RightFore;
        foreach (int vertex in vertices)
        {
            if (vertex < 0 || vertex + 1 >= arm.skin.starts.Length) return false;
            for (int i = arm.skin.starts[vertex]; i < arm.skin.starts[vertex + 1]; i++)
            {
                float weight = arm.skin.weights[i] / vertices.Length;
                var motion = arm.skin.motions[i];
                // Paw profiles are weighted, so upper/fore seam vertices remain
                // exact instead of being treated as one rigid wrist marker.
                Vector3 point = motion == CatPawReachCatalog.SkinMotion.Fixed ? arm.skin.points[i] :
                    ScratchBodyPoint(sample, chest, arm.skin.points[i]);
                if (motion == upperMotion) { patch.upper += point * weight; patch.upperWeight += weight; }
                else if (motion == foreMotion) { patch.fore += point * weight; patch.foreWeight += weight; }
                else patch.unchanged += point * weight;
            }
        }
        if (!SolveScratchPatch(arm, upper, fore, hand, patch, target, forward, requiredMargin, out margin)) return false;
        if (end.HasValue)
        {
            if (!SolveScratchPatch(arm, upper, fore, hand, patch, end.Value, forward, requiredMargin, out float endMargin)) return false;
            margin = Mathf.Min(margin, endMargin);
        }
        return true;
    }
    static bool SolveScratchPatch(WorldArm arm, Vector3 upper, Vector3 fore, Vector3 hand, ScratchPatch patch,
        Vector3 target, Vector3 forward, float requiredMargin, out float margin)
    {
        margin = float.NegativeInfinity;
        var solve = new CatPawSurfaceCcd.State { Upper = upper, Fore = fore, Hand = hand,
            UpperRotation = Quaternion.identity, PawRotation = Quaternion.identity };
        // Match the live scratch endpoint iteration count and anatomical pole.
        for (int pass = 0; pass < ScratchSolveIterations; pass++)
        {
            Vector3 correction = (target - patch.SolvedMean(solve, upper, fore)) * ScratchCorrectionDamping;
            if (!SolveScratchArm(upper, solve.Fore, solve.Hand, solve.Hand + correction, forward, out var step)) return false;
            solve.UpperRotation = step.UpperRotation * solve.UpperRotation;
            solve.PawRotation = step.PawRotation * solve.PawRotation;
            solve.Fore = step.Fore; solve.Hand = step.Hand;
        }
        float error = Vector3.Distance(patch.SolvedMean(solve, upper, fore), target);
        float distance = Vector3.Distance(upper, solve.Hand);
        margin = arm.reach - distance;
        if (error > .002f) return false;
        return distance >= arm.minimum && margin >= requiredMargin;
    }
    static bool SolveScratchArm(Vector3 upper, Vector3 fore, Vector3 hand, Vector3 goal, Vector3 forward,
        out CatPawSurfaceCcd.State solve)
    {
        solve = default;
        float a = Vector3.Distance(upper, fore), b = Vector3.Distance(fore, hand);
        Vector3 delta = goal - upper; float distance = delta.magnitude;
        if (a < .0001f || b < .0001f || distance < .0001f) return false;
        Vector3 axis = delta / distance;
        float minimum = Mathf.Sqrt(a * a + b * b - 2f * a * b * Mathf.Cos(40f * Mathf.Deg2Rad));
        float maximum = Mathf.Sqrt(a * a + b * b - 2f * a * b * Mathf.Cos(165f * Mathf.Deg2Rad));
        distance = Mathf.Clamp(distance, minimum, maximum);
        Vector3 pole = Vector3.ProjectOnPlane(-forward * .65f - Vector3.up, axis);
        if (pole.sqrMagnitude < .0001f) pole = Vector3.ProjectOnPlane(-forward, axis);
        if (pole.sqrMagnitude < .00000001f) return false;
        pole.Normalize();
        float along = (a * a - b * b + distance * distance) / (2f * distance);
        Vector3 elbow = upper + axis * along + pole * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
        Vector3 wrist = upper + axis * distance;
        Quaternion upperRotation = Quaternion.FromToRotation(fore - upper, elbow - upper);
        Quaternion foreRotation = Quaternion.FromToRotation(upperRotation * (hand - fore), wrist - elbow) * upperRotation;
        solve = new CatPawSurfaceCcd.State { Upper = upper, Fore = elbow, Hand = wrist,
            UpperRotation = upperRotation, PawRotation = foreRotation };
        return true;
    }

    static bool ReachEndpointsSquared(WorldArm arm, Vector3 shoulder, Vector3 target, Vector3? end,
        float requiredMargin, out float greatestSquared)
    {
        greatestSquared = (shoulder - target).sqrMagnitude;
        float maximum = arm.reach - requiredMargin;
        if (maximum < arm.minimum) return false;
        float low = arm.minimum * arm.minimum, high = maximum * maximum;
        if (greatestSquared < low || greatestSquared > high) return false;
        if (end.HasValue)
        {
            float squared = (shoulder - end.Value).sqrMagnitude;
            if (squared < low || squared > high) return false;
            greatestSquared = Mathf.Max(greatestSquared, squared);
        }
        return true;
    }
    static bool QueryFixedClear(CatMovement actor, SourcePose source, bool memo)
    {
        if (memo && source.scratchFixedChecked) return source.scratchFixedClear;
        bool clear = FixedTrajectoryClear(actor, source);
        if (memo) { source.scratchFixedChecked = true; source.scratchFixedClear = clear; }
        return clear;
    }
    static bool QueryBodyClear(CatMovement actor, SourcePose source, float phase, float pitch, float yaw,
        Vector3 right, bool memo)
    {
        var key = new BodyRequest { phase = phase, pitch = pitch, yaw = yaw };
        if (memo && source.scratchBodyClearance.TryGetValue(key, out bool previous)) return previous;
        bool clear = TrajectoryClear(actor, source, phase, pitch, yaw, right);
        if (memo) source.scratchBodyClearance[key] = clear;
        return clear;
    }

    static Bend[] CreateBends()
    {
        var values = new List<Bend>();
        for (float yaw = 0; yaw <= MaximumChestYaw; yaw += 5f)
        for (float pitch = 0; pitch <= MaximumChestPitch; pitch += 1f)
            values.Add(new Bend { pitch = pitch, yaw = yaw, cost = pitch + yaw * .7f });
        values.Sort((a, b) => { int result = a.cost.CompareTo(b.cost); return result != 0 ? result : a.yaw.CompareTo(b.yaw); });
        return values.ToArray();
    }
    static SourcePose TransformSource(CatPawReachCatalog.Entry entry, Matrix4x4 matrix, Vector3 scale)
    {
        var source = new SourcePose { all = new WorldSample[entry.samples.Length] };
        var contacts = new List<WorldSample>(4);
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 lift = Vector3.up * CatPawReachCatalog.GroundClearance;
        for (int i = 0; i < entry.samples.Length; i++)
        {
            var sample = entry.samples[i];
            if (sample == null || sample.bodyProbes == null || sample.bodyProbes.Length != 4) return null;
            var world = new WorldSample { phase = sample.phase, pivot = matrix.MultiplyPoint3x4(sample.torsoPivot) + lift,
                left = TransformArm(sample.left, matrix, lift), right = TransformArm(sample.right, matrix, lift),
                upper = new CatBodyGuardCatalog.Probe[3] };
            int upper = 0; bool hasPelvis = false;
            foreach (var probe in sample.bodyProbes)
            {
                var transformed = new CatBodyGuardCatalog.Probe { region = probe.region, start = matrix.MultiplyPoint3x4(probe.start) + lift,
                    end = matrix.MultiplyPoint3x4(probe.end) + lift, radius = probe.radius * radiusScale };
                if (probe.region == "pelvis") { world.pelvis = transformed; hasPelvis = true; }
                else { if (upper >= world.upper.Length) return null; world.upper[upper++] = transformed; }
            }
            if (!hasPelvis || upper != world.upper.Length) return null;
            source.all[i] = world;
            if (IsContactPhase(sample.phase))
            {
                if (entry.pose == CatActivityPose.Scratch && entry.leftPaw.Length > 0 &&
                    (!TransformPawSkin(entry, sample, matrix, entry.leftPaw, ref world.left) ||
                     !TransformPawSkin(entry, sample, matrix, entry.rightPaw, ref world.right))) return null;
                contacts.Add(world);
            }
        }
        source.contacts = contacts.ToArray(); return source;
    }
    static WorldArm TransformArm(CatPawReachCatalog.ArmChain arm, Matrix4x4 matrix, Vector3 lift)
    {
        Vector3 upper = matrix.MultiplyPoint3x4(arm.upper) + lift, fore = matrix.MultiplyPoint3x4(arm.fore) + lift,
            hand = matrix.MultiplyPoint3x4(arm.hand) + lift;
        float a = Vector3.Distance(upper, fore), b = Vector3.Distance(fore, hand);
        return new WorldArm { upper = upper, fore = fore, hand = hand,
            reach = (a + b) * .985f, minimum = Mathf.Abs(a - b) + .01f };
    }
    static bool TransformPawSkin(CatPawReachCatalog.Entry entry, CatPawReachCatalog.Sample sample, Matrix4x4 matrix,
        CatPawReachCatalog.PawVertex[] definitions, ref WorldArm arm)
    {
        var indexed = IndexedPawDefinitions(entry, definitions);
        if (indexed == null) return false;
        arm.skin = CatPawWeightedSkin.Build(entry, sample, matrix, indexed);
        if (arm.skin == null) return false;
        var points = new Vector3[definitions.Length];
        Vector3 sourceForward = matrix.MultiplyVector(Vector3.forward).normalized;
        int tip = -1; float forwardMost = float.NegativeInfinity;
        for (int v = 0; v < definitions.Length; v++)
        {
            if (!definitions[v].distal) continue;
            float extent = 0f;
            for (int i = arm.skin.starts[v]; i < arm.skin.starts[v + 1]; i++)
            {
                extent += Vector3.Distance(arm.skin.points[i], arm.hand) * arm.skin.weights[i];
                points[v] += arm.skin.points[i] * arm.skin.weights[i];
            }
            arm.pawExtent = Mathf.Max(arm.pawExtent, extent);
            float ahead = Vector3.Dot(points[v] - arm.hand, sourceForward);
            if (ahead > forwardMost) { forwardMost = ahead; tip = v; }
        }
        arm.sourceTipPatch = tip >= 0 ? ClosestDistalPatch(definitions, points, points[tip]) : null;
        return arm.sourceTipPatch != null;
    }
    static CatPawReachCatalog.PawVertex[] IndexedPawDefinitions(CatPawReachCatalog.Entry entry,
        CatPawReachCatalog.PawVertex[] definitions)
    {
        if (!indexedPawCache.TryGetValue(entry, out var indexed))
        {
            // Legacy endpoint arrays retain bone paths but predate skin-matrix
            // indices. Resolve an immutable local copy; the baked profiles and
            // the runtime vertex order remain unchanged.
            var bones = new Dictionary<string, int>();
            for (int i = 0; i < entry.skinBones.Length; i++)
            {
                string path = entry.skinBones[i]?.path;
                if (string.IsNullOrEmpty(path) || bones.ContainsKey(path))
                { indexedPawCache[entry] = null; return null; }
                bones.Add(path, i);
            }
            indexed = new IndexedPaws
            {
                left = CopyIndexedPaw(entry.leftPaw, bones),
                right = CopyIndexedPaw(entry.rightPaw, bones)
            };
            if (indexed.left == null || indexed.right == null) indexed = null;
            indexedPawCache[entry] = indexed;
        }
        if (indexed == null) return null;
        return ReferenceEquals(definitions, entry.leftPaw) ? indexed.left :
            ReferenceEquals(definitions, entry.rightPaw) ? indexed.right : null;
    }
    static CatPawReachCatalog.PawVertex[] CopyIndexedPaw(CatPawReachCatalog.PawVertex[] definitions,
        Dictionary<string, int> bones)
    {
        if (definitions == null || definitions.Length == 0) return null;
        var copy = new CatPawReachCatalog.PawVertex[definitions.Length];
        for (int v = 0; v < definitions.Length; v++)
        {
            var definition = definitions[v];
            if (definition?.influences == null || definition.influences.Length == 0) return null;
            var influences = new CatPawReachCatalog.PawInfluence[definition.influences.Length];
            for (int i = 0; i < influences.Length; i++)
            {
                var original = definition.influences[i];
                if (original == null || original.bonePath == null || !bones.TryGetValue(original.bonePath, out int index)) return null;
                influences[i] = new CatPawReachCatalog.PawInfluence { bonePath = original.bonePath, sourceBone = index,
                    bindPosition = original.bindPosition, weight = original.weight };
            }
            copy[v] = new CatPawReachCatalog.PawVertex { vertexIndex = definition.vertexIndex,
                distal = definition.distal, influences = influences };
        }
        return copy;
    }
    static float ReachMargin(WorldSample sample, Quaternion bend, WorldArm arm, Vector3 target)
    {
        float distance = Vector3.Distance(sample.pivot + bend * (arm.upper - sample.pivot), target);
        return distance < arm.minimum ? float.NegativeInfinity : arm.reach - distance;
    }
    static bool IsContactPhase(float phase) => Mathf.Abs(phase - .32f) < .001f ||
        Mathf.Abs(phase - .42f) < .001f || Mathf.Abs(phase - .52f) < .001f || Mathf.Abs(phase - .62f) < .001f;
    static bool FixedTrajectoryClear(CatMovement actor, SourcePose source)
    {
        foreach (var sample in source.all)
        {
            singleProbe[0] = sample.pelvis;
            if (!actor.IsInteractionBodyClear(singleProbe)) return false;
            if ((sample.phase <= .001f || sample.phase >= .999f) && !UpperBodyClear(actor, sample, Quaternion.identity)) return false;
        }
        return true;
    }
    static bool TrajectoryClear(CatMovement actor, SourcePose source, float contactPhase, float pitch, float yaw, Vector3 right)
    {
        // The fully extended contact pose is the most likely obstruction.
        // Check it first, then every intermediate source pose as before.
        Quaternion contactBend = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, right);
        foreach (var sample in source.contacts)
            if (Mathf.Abs(sample.phase - contactPhase) < .001f && !UpperBodyClear(actor, sample, contactBend)) return false;
        foreach (var sample in source.all)
        {
            if (sample.phase <= .001f || sample.phase >= .999f || Mathf.Abs(sample.phase - contactPhase) < .001f) continue;
            float envelope = Mathf.Clamp01(sample.phase <= contactPhase ? sample.phase / contactPhase : (1f - sample.phase) / (1f - contactPhase));
            Quaternion bend = Quaternion.AngleAxis(yaw * envelope, Vector3.up) * Quaternion.AngleAxis(pitch * envelope, right);
            if (!UpperBodyClear(actor, sample, bend)) return false;
        }
        return true;
    }
    static bool UpperBodyClear(CatMovement actor, WorldSample sample, Quaternion bend)
    {
        for (int i = 0; i < sample.upper.Length; i++)
        {
            var probe = sample.upper[i];
            probe.start = sample.pivot + bend * (probe.start - sample.pivot);
            probe.end = sample.pivot + bend * (probe.end - sample.pivot);
            worldProbes[i] = probe;
        }
        return actor.IsInteractionBodyClear(worldProbes);
    }
}

