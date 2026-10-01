using System;
using UnityEngine;

/// <summary>Pure source-pose care reach. No Transform, physics query, root move or live animation sampling.</summary>
public static class CatCareReachGeometry
{
    public const float ContactDistance = .045f;
    public const float MaximumShoulderPitch = 34f;
    const int ShoulderSteps = 16;
    const int NeckIterations = 8;
    static readonly float[] NeckLimits = { 30f, 25f, 20f };

    [Serializable] public sealed class Pose
    {
        public Vector3 root, right, torsoPosition;
        public Quaternion torsoRotation;
        public Joint[] joints;
        public Vertex[] mouth;
        public Leg left, rightLeg;
    }
    [Serializable] public struct Joint
    {
        // -1 is the torso. A joint may only depend on an earlier joint.
        public int anchor;
        public Vector3 positionFromAnchor;
        public Quaternion parentRotationFromAnchor, sourceLocalRotation;
    }
    [Serializable] public struct Influence
    {
        // -2 is a fixed world point, -1 is torso, 0..2 are neck/head joints.
        public int anchor;
        public Vector3 point;
        public float weight;
    }
    [Serializable] public sealed class Vertex { public Influence[] influences; }
    [Serializable] public struct Leg
    {
        public Vector3 upper, fore, pawTarget, sourceElbow;
        public float upperLength, lowerLength;
    }
    public sealed class Workspace
    {
        internal readonly Vector3[] position = new Vector3[3];
        internal readonly Quaternion[] rotation = new Quaternion[3];
        internal readonly Quaternion[] parentRotation = new Quaternion[3];
        internal readonly Quaternion[] local = new Quaternion[3];
        internal Quaternion torsoRotation;
    }
    public struct Solution
    {
        public bool valid;
        public float shoulderPitch, goalDistance, foodDistance, nearestFoodDistance;
        public float sourceRequiredReach, postShoulderRequiredReach, chainLength;
        public float leftMaximumMargin, rightMaximumMargin;
        public Vector3 mouthPosition, neckBase, goal;
        public Quaternion neck0, neck1, neck2;
        public int evaluatedCandidates;
        public bool Contact => valid && foodDistance <= ContactDistance && nearestFoodDistance <= ContactDistance;
    }

    public static bool TrySolve(Pose pose, Vector3 food, float weight, Workspace work, out Solution result)
    {
        result = default;
        if (pose == null || work == null || pose.joints == null || pose.joints.Length != 3 ||
            pose.mouth == null || pose.mouth.Length == 0 || pose.right.sqrMagnitude < .9f) return false;
        for (int i = 0; i < 3; i++) if (pose.joints[i].anchor < -1 || pose.joints[i].anchor >= i) return false;
        weight = Mathf.Clamp01(weight);
        Vector3 outside = pose.root - food; outside.y = 0;
        Vector3 goal = food + Vector3.up * .025f + outside.normalized * .025f;
        Reset(pose, work, 0);
        int sourceEndpoint = Lowest(pose, work);
        Vector3 sourceMouth = Mouth(pose, work, sourceEndpoint);
        float chain = Vector3.Distance(work.position[0], work.position[1]) +
            Vector3.Distance(work.position[1], work.position[2]) + Vector3.Distance(work.position[2], sourceMouth);
        float sourceRequired = Vector3.Distance(work.position[0], goal), best = float.PositiveInfinity;
        int evaluated = 0;
        for (int choice = 0; choice <= ShoulderSteps * 2; choice++)
        {
            evaluated++;
            int step = (choice + 1) / 2 * (choice % 2 == 0 ? -1 : 1);
            float pitch = MaximumShoulderPitch * weight * step / ShoulderSteps;
            Reset(pose, work, pitch);
            Quaternion bend = work.torsoRotation * Quaternion.Inverse(pose.torsoRotation);
            bool left = Support(pose, pose.left, bend, out float leftMargin);
            bool right = Support(pose, pose.rightLeg, bend, out float rightMargin);
            if (!left || !right) continue;
            for (int iteration = 0; iteration < NeckIterations; iteration++)
            {
                int endpoint = Lowest(pose, work);
                for (int i = 2; i >= 0; i--)
                {
                    Vector3 from = Mouth(pose, work, endpoint) - work.position[i], to = goal - work.position[i];
                    if (from.sqrMagnitude < .000001f || to.sqrMagnitude < .000001f) continue;
                    Quaternion world = Quaternion.FromToRotation(from, to) * work.rotation[i];
                    Quaternion local = Quaternion.Inverse(work.parentRotation[i]) * world;
                    work.local[i] = CatCareJointLimit.Clamp(pose.joints[i].sourceLocalRotation, local, NeckLimits[i] * weight);
                    UpdateJoints(pose, work);
                }
            }
            Vector3 mouth = Mouth(pose, work, Lowest(pose, work));
            float distance = Vector3.Distance(mouth, goal);
            if (distance < best)
            {
                best = distance;
                float nearest = float.PositiveInfinity;
                for (int v = 0; v < pose.mouth.Length; v++) nearest = Mathf.Min(nearest, Vector3.Distance(Mouth(pose, work, v), food));
                result = new Solution { valid = true, shoulderPitch = pitch, goalDistance = distance,
                    foodDistance = Vector3.Distance(mouth, food), nearestFoodDistance = nearest,
                    sourceRequiredReach = sourceRequired, postShoulderRequiredReach = Vector3.Distance(work.position[0], goal),
                    chainLength = chain, leftMaximumMargin = leftMargin, rightMaximumMargin = rightMargin,
                    mouthPosition = mouth, neckBase = work.position[0], goal = goal,
                    neck0 = work.local[0], neck1 = work.local[1], neck2 = work.local[2] };
            }
            if (distance < .009f) break;
        }
        result.evaluatedCandidates = evaluated;
        return result.valid;
    }

    static bool Support(Pose pose, Leg leg, Quaternion bend, out float maximumMargin)
    {
        Vector3 upper = pose.torsoPosition + bend * (leg.upper - pose.torsoPosition);
        Vector3 fore = pose.torsoPosition + bend * (leg.fore - pose.torsoPosition);
        Vector3 delta = leg.pawTarget - upper; float distance = delta.magnitude;
        maximumMargin = leg.upperLength + leg.lowerLength - .00001f - distance;
        if (distance < Mathf.Abs(leg.upperLength - leg.lowerLength) + .00001f || maximumMargin < 0f) return false;
        Vector3 direction = delta / distance;
        Vector3 plane = Vector3.ProjectOnPlane(leg.sourceElbow - upper, direction);
        if (plane.sqrMagnitude < .00000001f) plane = Vector3.ProjectOnPlane(fore - upper, direction);
        return plane.sqrMagnitude >= .00000001f;
    }
    static void Reset(Pose pose, Workspace work, float pitch)
    {
        work.torsoRotation = Quaternion.AngleAxis(pitch, pose.right) * pose.torsoRotation;
        for (int i = 0; i < 3; i++) work.local[i] = pose.joints[i].sourceLocalRotation;
        UpdateJoints(pose, work);
    }
    static void UpdateJoints(Pose pose, Workspace work)
    {
        for (int i = 0; i < 3; i++)
        {
            var joint = pose.joints[i];
            Vector3 position = joint.anchor < 0 ? pose.torsoPosition : work.position[joint.anchor];
            Quaternion rotation = joint.anchor < 0 ? work.torsoRotation : work.rotation[joint.anchor];
            work.position[i] = position + rotation * joint.positionFromAnchor;
            work.parentRotation[i] = rotation * joint.parentRotationFromAnchor;
            work.rotation[i] = work.parentRotation[i] * work.local[i];
        }
    }
    static Vector3 Mouth(Pose pose, Workspace work, int vertex)
    {
        Vector3 point = Vector3.zero;
        foreach (var influence in pose.mouth[vertex].influences)
        {
            if (influence.weight <= 0) continue;
            Vector3 position;
            if (influence.anchor == -2) position = influence.point;
            else if (influence.anchor == -1) position = pose.torsoPosition + work.torsoRotation * influence.point;
            else position = work.position[influence.anchor] + work.rotation[influence.anchor] * influence.point;
            point += position * influence.weight;
        }
        return point;
    }
    static int Lowest(Pose pose, Workspace work)
    {
        int result = 0; float lowest = Mouth(pose, work, 0).y;
        for (int i = 1; i < pose.mouth.Length; i++)
        {
            float height = Mouth(pose, work, i).y;
            if (height < lowest) { lowest = height; result = i; }
        }
        return result;
    }
}
