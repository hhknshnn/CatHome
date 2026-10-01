using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>A measured source-pose reach, shared by readiness and the actual hand solve.</summary>
public struct CatPawReachPlan
{
    public CatActivityPose Pose;
    public float SourcePhase, ChestPitch, ChestYaw, ReachMargin;
    public Vector3 LeftTarget, RightTarget;
    public bool Left, Both;
    public CatPawSurfacePlan Surface;
    public Vector3 Target => Left ? LeftTarget : RightTarget;
}

/// <summary>Pure geometry over baked source bones. Never samples or moves the live actor.</summary>
public static partial class CatPawReachResolver
{
    public const float MaximumChestPitch = 32f;
    public const float MaximumChestYaw = 35f;
    static readonly CatBodyGuardCatalog.Probe[] worldProbes = new CatBodyGuardCatalog.Probe[3];
    static readonly CatBodyGuardCatalog.Probe[] singleProbe = new CatBodyGuardCatalog.Probe[1];
    static readonly Bend[] bends = CreateBends();
    static readonly Dictionary<CatPawReachCatalog.Entry, SourcePose> sourceCache = new Dictionary<CatPawReachCatalog.Entry, SourcePose>();
    static CatMovement cachedActor;
    static CatBreedVisualTag cachedVisual;
    static Matrix4x4 cachedMatrix;

    struct Bend { public float pitch, yaw, cost; }
    struct WorldArm { public Vector3 upper; public float reach, minimum; }
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
    }
    struct Request : IEquatable<Request>
    {
        public Vector3 a, b, c, d;
        public bool left, both, sweep;
        public float pitch, yaw;
        public bool Equals(Request other) => a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c) && d.Equals(other.d) &&
            left == other.left && both == other.both && sweep == other.sweep && pitch == other.pitch && yaw == other.yaw;
        public override bool Equals(object other) => other is Request key && Equals(key);
        public override int GetHashCode() => a.GetHashCode() ^ b.GetHashCode() * 17 ^ c.GetHashCode() * 31 ^ d.GetHashCode() * 43 ^
            (left ? 101 : 0) ^ (both ? 211 : 0) ^ (sweep ? 307 : 0) ^ pitch.GetHashCode() ^ yaw.GetHashCode();
    }

    public static bool TryResolve(CatMovement actor, Vector3 target, bool left,
        CatActivityPose pose, out CatPawReachPlan plan, float maxPitch = MaximumChestPitch, float maxYaw = 0f)
        => Resolve(actor, target, target, left, false, pose, maxPitch, maxYaw, out plan);
    public static bool TryResolveBoth(CatMovement actor, Vector3 left, Vector3 right,
        CatActivityPose pose, out CatPawReachPlan plan, float maxPitch = MaximumChestPitch, float maxYaw = 0f)
        => Resolve(actor, left, right, true, true, pose, maxPitch, maxYaw, out plan);
    public static bool TryResolveSweep(CatMovement actor, Vector3 leftA, Vector3 leftB,
        Vector3 rightA, Vector3 rightB, CatActivityPose pose, out CatPawReachPlan plan,
        float maxPitch = MaximumChestPitch, float maxYaw = 0f)
        => Resolve(actor, leftA, rightA, true, true, pose, maxPitch, maxYaw, out plan, leftB, rightB);

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
        Vector3? leftEnd = null, Vector3? rightEnd = null, Vector3? plannedPosition = null, Quaternion? plannedRotation = null)
    {
        plan = default;
        if (actor == null) return false;
        var visual = actor.GetComponentInChildren<CatBreedVisualTag>();
        var entry = visual != null ? CatPawReachCatalog.Load()?.Find(visual.BreedId, pose) : null;
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
            pitch = Mathf.Clamp(maxPitch, 0, MaximumChestPitch), yaw = Mathf.Clamp(maxYaw, 0, MaximumChestYaw) };
        float yawSign = Mathf.Sign(Vector3.Dot((left ? leftTarget : rightTarget) - position, right));
        // Pitch and yaw never move the pelvis or alter the unbent beginning/end.
        // Reject those impossible paths before considering hundreds of bends.
        bool fixedChecked = false;
        if (source.plans.TryGetValue(request, out var previous))
        {
            if (!FixedTrajectoryClear(actor, source)) return false;
            fixedChecked = true;
            if (TrajectoryClear(actor, source, previous.SourcePhase, previous.ChestPitch, previous.ChestYaw, right))
            { plan = previous; return true; }
        }
        foreach (var bend in bends)
        {
            if (bend.pitch > request.pitch || bend.yaw > request.yaw) continue;
            Quaternion rotation = Quaternion.AngleAxis(bend.yaw * yawSign, Vector3.up) * Quaternion.AngleAxis(bend.pitch, right);
            foreach (var sample in source.contacts)
            {
                float margin = both ? Mathf.Min(ReachMargin(sample, rotation, sample.left, leftTarget), ReachMargin(sample, rotation, sample.right, rightTarget)) :
                    ReachMargin(sample, rotation, left ? sample.left : sample.right, left ? leftTarget : rightTarget);
                if (leftEnd.HasValue) margin = Mathf.Min(margin, ReachMargin(sample, rotation, sample.left, leftEnd.Value));
                if (rightEnd.HasValue) margin = Mathf.Min(margin, ReachMargin(sample, rotation, sample.right, rightEnd.Value));
                if (margin < .004f) continue;
                if (!fixedChecked)
                {
                    if (!FixedTrajectoryClear(actor, source)) return false;
                    fixedChecked = true;
                }
                if (!TrajectoryClear(actor, source, sample.phase, bend.pitch, bend.yaw * yawSign, right)) continue;
                plan = new CatPawReachPlan { Pose = pose, SourcePhase = sample.phase, ChestPitch = bend.pitch,
                    ChestYaw = bend.yaw * yawSign, ReachMargin = margin, Left = left, Both = both,
                    LeftTarget = leftTarget, RightTarget = rightTarget };
                if (source.plans.Count >= 64) source.plans.Clear();
                source.plans[request] = plan;
                return true; // Bends are ordered by the same cost used by the original exhaustive solver.
            }
        }
        return false;
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
            if (IsContactPhase(sample.phase)) contacts.Add(world);
        }
        source.contacts = contacts.ToArray(); return source;
    }
    static WorldArm TransformArm(CatPawReachCatalog.ArmChain arm, Matrix4x4 matrix, Vector3 lift)
    {
        Vector3 upper = matrix.MultiplyPoint3x4(arm.upper) + lift, fore = matrix.MultiplyPoint3x4(arm.fore) + lift,
            hand = matrix.MultiplyPoint3x4(arm.hand) + lift;
        float a = Vector3.Distance(upper, fore), b = Vector3.Distance(fore, hand);
        return new WorldArm { upper = upper, reach = (a + b) * .985f, minimum = Mathf.Abs(a - b) + .01f };
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

