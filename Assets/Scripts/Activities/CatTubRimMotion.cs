using System.Collections.Generic;
using UnityEngine;

/// <summary>Measured narrow-rim support. Only joint rotations change; the source gait keeps its swing.</summary>
[DefaultExecutionOrder(650)]
[DisallowMultipleComponent]
public sealed class CatTubRimMotion : MonoBehaviour
{
    sealed class Leg
    {
        public Transform upper, lower, foot;
        public Quaternion upperPose, lowerPose, footPose, worldFoot;
        public Vector3 target;
        public float sole, lift;
    }
    readonly Leg[] legs = { new Leg(), new Leg(), new Leg(), new Leg() };
    readonly List<Vector3> surface = new List<Vector3>();
    readonly List<Vector3> vertices = new List<Vector3>();
    TubEdgeWalkActivity owner;
    Transform hips, head, tail;
    Quaternion hipsPose, headPose, tailPose;
    SkinnedMeshRenderer skin;
    Mesh sample;
    bool adjusted;
    float referenceHeight;
    public float BalanceSeconds { get; set; }
    public float MaximumContactError { get; private set; }
    public bool IsActive => owner != null && owner.IsRimWalking;
    public int SurfaceSampleCount => surface.Count;

    public void Prepare(TubEdgeWalkActivity activity, MeshCollider mesh, Vector3 first, Vector3 last)
    {
        Clear(); owner = activity; referenceHeight = first.y;
        Vector3 axis = (last - first).normalized, across = Vector3.Cross(Vector3.up, axis);
        float length = Vector3.Distance(first, last);
        if (mesh != null)
        {
            // Read only this product's actual mesh, never its interaction box or water.
            for (float along = -.12f; along <= length + .12f; along += .015f)
            for (float side = -.18f; side <= .18f; side += .006f)
            {
                Vector3 p = first + axis * along + across * side;
                RaycastHit hit;
                if (mesh.Raycast(new Ray(p + Vector3.up * .25f, Vector3.down), out hit, .36f) &&
                    hit.normal.y >= .82f && hit.point.y >= referenceHeight - .09f && hit.point.y <= referenceHeight + .025f)
                    surface.Add(hit.point);
            }
        }
        var bones = GetComponentsInChildren<Transform>();
        for (int i = 0; i < legs.Length; i++)
        {
            string side = i % 2 == 0 ? "L" : "R";
            foreach (var bone in bones)
            {
                if (bone.name == (i < 2 ? "DEF-upper_arm." : "DEF-thigh.") + side) legs[i].upper = bone;
                if (bone.name == (i < 2 ? "DEF-forearm." : "DEF-shin.") + side) legs[i].lower = bone;
                if (bone.name == (i < 2 ? "DEF-hand." : "DEF-foot.") + side) legs[i].foot = bone;
            }
        }
        foreach (var bone in bones)
        {
            if (bone.name == "DEF-spine") hips = bone;
            if (bone.name == "DEF-spine.006") head = bone;
            if (bone.name == "DEF-tail.001") tail = bone;
        }
        skin = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    public Vector3 ClosestSurface(Vector3 point)
    {
        Vector3 result = point; result.y = referenceHeight;
        float best = float.PositiveInfinity;
        foreach (var candidate in surface)
        {
            float dx = candidate.x - point.x, dz = candidate.z - point.z;
            float square = dx * dx + dz * dz;
            if (square >= best) continue;
            best = square; result = candidate;
        }
        return result;
    }
    public float HeightAt(Vector3 point) => ClosestSurface(point).y;

    void Update() { Restore(); }
    void LateUpdate()
    {
        if (!IsActive || hips == null || skin == null || surface.Count == 0) return;
        foreach (var leg in legs) if (leg.upper == null || leg.lower == null || leg.foot == null) return;
        if (sample == null) sample = new Mesh { name = "Tub paw contact sample" };
        skin.BakeMesh(sample, true); sample.GetVertices(vertices);
        foreach (var leg in legs) leg.sole = leg.foot.position.y;
        // Read the real paw soles after the native animation and common support.
        // This preserves lifted swing paws instead of pinning all four to the rim.
        foreach (var vertex in vertices)
        {
            Vector3 p = skin.transform.TransformPoint(vertex);
            Leg nearest = null; float best = .085f * .085f;
            foreach (var leg in legs)
            {
                float distance = (p - leg.foot.position).sqrMagnitude;
                if (distance < best) { best = distance; nearest = leg; }
            }
            if (nearest != null) nearest.sole = Mathf.Min(nearest.sole, p.y);
        }
        float lowest = float.PositiveInfinity;
        foreach (var leg in legs) lowest = Mathf.Min(lowest, leg.sole);
        hipsPose = hips.localRotation;
        if (head != null) headPose = head.localRotation;
        if (tail != null) tailPose = tail.localRotation;
        foreach (var leg in legs)
        {
            leg.upperPose = leg.upper.localRotation; leg.lowerPose = leg.lower.localRotation;
            leg.footPose = leg.foot.localRotation; leg.worldFoot = leg.foot.rotation;
            leg.lift = Mathf.Max(0f, leg.sole - lowest);
            float soleOffset = Mathf.Max(.008f, leg.foot.position.y - leg.sole);
            leg.target = ClosestSurface(leg.foot.position) + Vector3.up * (soleOffset + leg.lift + .005f);
        }
        float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(BalanceSeconds / .25f, (owner.WalkDuration - BalanceSeconds) / .25f));
        float roll = Mathf.Sin(BalanceSeconds * 4.6f) * 3f * envelope;
        hips.rotation = Quaternion.AngleAxis(roll, transform.forward) * hips.rotation;
        if (head != null) head.rotation = Quaternion.AngleAxis(-roll * 1.4f, transform.forward) * head.rotation;
        if (tail != null) tail.rotation = Quaternion.AngleAxis(-roll * 3f, Vector3.up) * tail.rotation;
        MaximumContactError = 0f;
        foreach (var leg in legs)
        {
            Solve(leg);
            MaximumContactError = Mathf.Max(MaximumContactError, Vector3.Distance(leg.foot.position, leg.target));
        }
        adjusted = true;
    }
    static void Solve(Leg leg)
    {
        Vector3 origin = leg.upper.position, direction = leg.target - origin;
        float a = Vector3.Distance(origin, leg.lower.position), b = Vector3.Distance(leg.lower.position, leg.foot.position);
        float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
        Vector3 axis = direction.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(leg.lower.position - origin, axis).normalized;
        if (bend.sqrMagnitude < .001f) bend = Vector3.ProjectOnPlane(leg.upper.forward, axis).normalized;
        float along = (a * a - b * b + distance * distance) / (2f * distance);
        Vector3 elbow = origin + axis * along + bend * Mathf.Sqrt(Mathf.Max(0f, a * a - along * along));
        leg.upper.rotation = Quaternion.FromToRotation(leg.lower.position - origin, elbow - origin) * leg.upper.rotation;
        leg.lower.rotation = Quaternion.FromToRotation(leg.foot.position - leg.lower.position, leg.target - leg.lower.position) * leg.lower.rotation;
        leg.foot.rotation = leg.worldFoot;
    }
    void Restore()
    {
        if (!adjusted) return; adjusted = false;
        if (hips != null) hips.localRotation = hipsPose;
        if (head != null) head.localRotation = headPose;
        if (tail != null) tail.localRotation = tailPose;
        foreach (var leg in legs)
        {
            if (leg.upper != null) leg.upper.localRotation = leg.upperPose;
            if (leg.lower != null) leg.lower.localRotation = leg.lowerPose;
            if (leg.foot != null) leg.foot.localRotation = leg.footPose;
        }
    }
    public void Clear()
    {
        Restore(); owner = null; surface.Clear(); BalanceSeconds = 0f;
        foreach (var leg in legs) leg.upper = leg.lower = leg.foot = null;
        hips = head = tail = null; skin = null;
    }
    void OnDisable() { Clear(); }
    void OnDestroy() { if (sample != null) Destroy(sample); }
}
