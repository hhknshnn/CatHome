using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Small planted turning steps after a completed jump. No airborne steering.</summary>
[DefaultExecutionOrder(660)]
[DisallowMultipleComponent]
public sealed class CatSurfaceTurnMotion : MonoBehaviour
{
    sealed class Leg
    {
        public Transform upper, lower, foot;
        public Quaternion upperPose, lowerPose, footPose, plantedRotation, targetRotation, restRotation;
        public Vector3 planted, target, offset;
        public float sole;
        public bool fore;
    }
    readonly Leg[] legs = {new Leg(), new Leg(), new Leg(), new Leg()};
    readonly List<Vector3> supportPoints = new List<Vector3>();
    CatActivity owner;
    CatActivityAnimation animation;
    Transform support;
    Transform visual;
    Transform head;
    Quaternion headPose;
    Vector3 visualPose;
    Vector3 weightShift, releaseShift;
    float headLead, releaseHeadLead;
    float lowering;
    float standingLowering;
    float releaseElapsed = -1f, releaseLowering;
    bool adjusted, holding, prepared;
    public bool IsTurning => owner != null && owner.IsRunning && releaseElapsed < 0 && !holding;
    public float MaximumContactError { get; private set; }
    public int CompletedSteps { get; private set; }

    public IEnumerator Turn(CatActivity activity, Vector3 centre, Quaternion arrival)
    {
        Clear();
        if (activity == null || !activity.IsRunning) yield break;
        owner = activity; animation = GetComponent<CatActivityAnimation>();
        var anchor = new GameObject("Planted turn support") {hideFlags = HideFlags.DontSave};
        support = anchor.transform; support.SetParent(activity.transform, true);
        support.SetPositionAndRotation(centre, Quaternion.identity);
        anchor.AddComponent<CatActivitySurface>();
        Quaternion first = transform.rotation;
        bool lowDoor = activity.StoreProductId == HomeStoreService.BedroomStarCanopyId;
        var turningPose = CatActivityPose.GentleKnead;
        visual = GetComponentInChildren<Animator>().transform;
        var bones = GetComponentsInChildren<Transform>();
        foreach (var bone in bones) if (bone.name == "DEF-spine.006") head = bone;
        for (int i = 0; i < legs.Length; i++)
        {
            var leg = legs[i]; string side = i % 2 == 0 ? "L" : "R";
            leg.fore = i < 2;
            foreach (var bone in bones)
            {
                if (bone.name == (i < 2 ? "DEF-upper_arm." : "DEF-thigh.") + side) leg.upper = bone;
                if (bone.name == (i < 2 ? "DEF-forearm." : "DEF-shin.") + side) leg.lower = bone;
                if (bone.name == (i < 2 ? "DEF-hand." : "DEF-foot.") + side) leg.foot = bone;
            }
            if (leg.upper == null || leg.lower == null || leg.foot == null) { Clear(); yield break; }
            leg.planted = leg.target = leg.foot.position;
            leg.offset = Quaternion.Inverse(first) * (leg.planted - centre);
            leg.sole = Mathf.Max(.01f, leg.planted.y - centre.y);
            leg.plantedRotation = leg.targetRotation = leg.foot.rotation;
            leg.restRotation = Quaternion.Inverse(first) * leg.foot.rotation;
        }
        MeasureSupport(activity, centre);
        // The opening belongs to the furniture, not to the cat's current heading.
        // Reusing -forward on the way down sent the tent turn toward a side post.
        float openingStep = lowDoor ? .20f : activity.StoreProductId == "room.armchair" ? .10f :
            activity.StoreProductId == "balcony.hanging-chair" ? .15f : 0f;
        Vector3 turnOutward = activity.RoutineEntryPoint != null ?
            Vector3.ProjectOnPlane(activity.RoutineEntryPoint.position - centre, Vector3.up).normalized * openingStep : Vector3.zero;
        bool constrained = openingStep > 0f || activity.StoreProductId == HomeStoreService.BalconyHerbShelfId;
        // On an open surface give the following rear paw room beside the
        // leading one. Doorways and measured narrow shelves retain their fit.
        if (!constrained)
            for (int i = 2; i < 4; i++) legs[i].offset.x += i == 2 ? -.012f : .012f;
        float angle = Quaternion.Angle(first, arrival);
        float direction = Mathf.Sign(Vector3.SignedAngle(first * Vector3.forward, arrival * Vector3.forward, Vector3.up));
        // Open the direction with a reaching forepaw, then follow with low
        // rear placements. Tiny equal yaw cycles made the cat march sideways.
        int cycles = Mathf.Max(1, Mathf.CeilToInt(angle / (constrained ? 18f : 30f)));
        // An inside forepaw opens the turn; the opposite hind paw follows.
        // Only one paw swings. The body keeps turning through every footfall.
        int[] order = direction > 0 ? new[] {1, 2, 0, 3} : new[] {0, 3, 1, 2};
        float pace = constrained ? .80f : 1f;
        float foreSeconds = constrained ? .135f * pace : .155f;
        float hindSeconds = constrained ? .115f * pace : .095f;
        float cycleSeconds = 2f * (foreSeconds + hindSeconds);
        float duration = cycles * cycleSeconds;
        float Progress(float p) => constrained ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p)) : TurnProgress(p, duration);
        MaximumContactError = 0; CompletedSteps = 0;
        standingLowering = lowDoor ? .10f : .025f;
        try
        {
            // Leave room to bend the legs while the other three paws stay
            // planted. Fully straight standing legs cannot pivot in place.
            for (float t = 0; t < .20f; t += Time.deltaTime)
            {
                lowering = Mathf.SmoothStep(0, standingLowering, t / .20f);
                if (lowDoor) animation.SetPose(turningPose, support);
                else animation.SetTimedPose(turningPose, 0, support);
                yield return null;
            }
            lowering = standingLowering;
            prepared = true;
            float elapsed = 0f, start = 0f;
            for (int step = 0; step < cycles * 4; step++)
            {
                int index = order[step % 4];
                var swing = legs[index];
                float seconds = index < 2 ? foreSeconds : hindSeconds;
                float end = start + seconds;
                // Place ahead of the body, near the middle of the coming stance.
                // The last four placements settle directly into the final stance.
                float anticipation = step >= (cycles - 1) * 4 ? 1f :
                    Progress((end + cycleSeconds * (constrained ? .38f : .20f)) / duration);
                Quaternion next = Quaternion.Slerp(first, arrival, anticipation);
                Vector3 nextCentre = centre + turnOutward * OpeningBlend(anticipation, lowDoor);
                Vector3 target = Supported(nextCentre + next * swing.offset, centre.y, swing.sole, index, next);
                // Each foot keeps its original orientation relative to the cat.
                Quaternion targetRotation = next * swing.restRotation;
                float stride = Vector3.ProjectOnPlane(target - swing.planted, Vector3.up).magnitude;
                float lift = stride < .004f && Quaternion.Angle(swing.plantedRotation, targetRotation) < 3f ? 0f :
                    Mathf.Clamp(stride * .065f, .003f, index < 2 ? .012f : .008f);
                Vector3 desiredShift = -Vector3.ProjectOnPlane(swing.planted - transform.position, Vector3.up).normalized * .006f;
                while (elapsed < end)
                {
                    float t = Mathf.Clamp01((elapsed - start) / seconds);
                    float ease = Mathf.SmoothStep(0f, 1f, t);
                    float total = Progress(elapsed / duration);
                    // Keep the established doorway path and its moving support.
                    Vector3 turnCentre = centre + turnOutward * OpeningBlend(total, lowDoor);
                    support.position = turnCentre;
                    transform.SetPositionAndRotation(turnCentre, Quaternion.Slerp(first, arrival, total));
                    animation.SetTimedPose(turningPose, Mathf.Repeat(elapsed / pace * .16f, 1f), support);
                    weightShift = Vector3.Lerp(weightShift, desiredShift, 1f - Mathf.Exp(-Time.deltaTime * 12f / pace));
                    headLead = direction * Mathf.Min(9f, angle * .16f) * Mathf.Sin(Mathf.PI * total);
                    for (int i = 0; i < 4; i++)
                    {
                        var leg = legs[i];
                        float arc = Mathf.Sin(t * Mathf.PI);
                        leg.target = i == index ? Vector3.Lerp(leg.planted, target, ease) + Vector3.up * (arc * arc * lift) : leg.planted;
                        leg.targetRotation = i == index ? Quaternion.Slerp(leg.plantedRotation, targetRotation, ease) : leg.plantedRotation;
                    }
                    yield return null; elapsed += Time.deltaTime;
                }
                swing.planted = swing.target = target;
                swing.plantedRotation = swing.targetRotation = targetRotation;
                CompletedSteps++; start = end;
            }
            support.position = centre;
            transform.SetPositionAndRotation(centre, arrival);
            headLead = 0f;
            if (openingStep > 0f)
                for (float settle = 0f; settle < .18f; settle += Time.deltaTime)
                {
                    animation.SetTimedPose(turningPose, Mathf.Repeat(elapsed / pace * .16f, 1f), support);
                    yield return null;
                }
        }
        finally
        {
            // Keep the last supported stance while the caller starts sitting,
            // kneading or walking. Standing upright first would stretch legs
            // beyond sloping cushions and then pop them into the next pose.
            holding = owner != null && owner.StoreProductId == HomeStoreService.BalconyHerbShelfId;
            releaseElapsed = holding ? -1f : 0f; releaseLowering = lowering;
            releaseShift = weightShift; releaseHeadLead = headLead;
        }
    }

    // Brief ease-in/out around a steady turn: a whole-turn smoothstep makes
    // the middle paws hurry and the first/last ones stamp almost on the spot.
    static float TurnProgress(float fraction, float seconds)
    {
        float p = Mathf.Clamp01(fraction), ramp = Mathf.Min(.22f, .18f / seconds);
        if (p < ramp) return p * p / (2f * ramp * (1f - ramp));
        if (p > 1f - ramp) return 1f - (1f - p) * (1f - p) / (2f * ramp * (1f - ramp));
        return (p - ramp * .5f) / (1f - ramp);
    }

    // Stay in the open area until the hips have cleared the side posts, then
    // place the last paws back on the resting spot. A sine arc returns too soon.
    static float OpeningBlend(float progress, bool lowDoor) => lowDoor ? Mathf.Sin(Mathf.PI * progress) :
        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / .22f)) *
        (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.78f, 1f, progress)));

    void MeasureSupport(CatActivity activity, Vector3 centre)
    {
        supportPoints.Clear();
        var meshes = activity.GetComponentsInChildren<MeshCollider>();
        for (float x = -.48f; x <= .48f; x += .04f)
        for (float z = -.48f; z <= .48f; z += .04f)
        {
            var ray = new Ray(centre + new Vector3(x, .15f, z), Vector3.down);
            foreach (var mesh in meshes)
            {
                RaycastHit hit;
                if (mesh.enabled && !mesh.isTrigger && mesh.Raycast(ray, out hit, .23f) && hit.normal.y > .75f &&
                    (activity.StoreProductId != HomeStoreService.BalconyHerbShelfId || Mathf.Abs(hit.point.y-centre.y)<.025f))
                    supportPoints.Add(hit.point);
            }
        }
    }

    Vector3 Supported(Vector3 point, float height, float sole, int movingLeg, Quaternion stance)
    {
        Vector3 result = new Vector3(point.x, height, point.z); float best = float.PositiveInfinity;
        var opposite = legs[movingLeg ^ 1];
        float side = movingLeg % 2 == 0 ? -1f : 1f;
        float clearance = Mathf.Clamp(Mathf.Abs(legs[movingLeg].offset.x - opposite.offset.x) * .35f, .05f, .085f);
        foreach (var candidate in supportPoints)
        {
            // A narrow support can map both requested ankles onto the same
            // mesh sample. Reserve room for the three planted paws, and keep
            // each left/right pair in anatomical order in the coming stance.
            // Hind paws must stay in left/right order under the pelvis. Front
            // paws may step across one another during a turn; forcing the same
            // ordering there can trap a forepaw behind the shoulder on a seat.
            if (movingLeg >= 2 && side * Vector3.Dot(candidate - opposite.planted, stance * Vector3.right) < clearance) continue;
            bool occupied = false;
            for (int i = 0; i < legs.Length; i++)
                if (i != movingLeg && Vector3.ProjectOnPlane(candidate - legs[i].planted, Vector3.up).sqrMagnitude < clearance * clearance)
                    occupied = true;
            if (occupied) continue;
            float square = (new Vector2(candidate.x - point.x, candidate.z - point.z)).sqrMagnitude;
            if (square < best) { best = square; result = candidate; }
        }
        // If the support offers no free foothold, retain this paw's supported
        // position instead of inventing a point outside the furniture.
        if (float.IsPositiveInfinity(best) && supportPoints.Count > 0) return legs[movingLeg].planted;
        return result + Vector3.up * sole;
    }

    void Update() { Restore(); }
    void LateUpdate()
    {
        if (owner == null || !owner.IsRunning || animation.IsNativeJump) { if (owner != null) Clear(); return; }
        if (releaseElapsed >= 0)
        {
            releaseElapsed += Time.deltaTime;
            float blend = Mathf.SmoothStep(0, 1, Mathf.Clamp01(releaseElapsed / .20f));
            if (blend >= 1) { Clear(); return; }
            lowering = releaseLowering * (1 - blend);
            weightShift = releaseShift * (1 - blend);
            headLead = releaseHeadLead * (1 - blend);
            foreach (var leg in legs)
            {
                leg.target = Vector3.Lerp(leg.planted, leg.foot.position, blend);
                leg.targetRotation = Quaternion.Slerp(leg.plantedRotation, leg.foot.rotation, blend);
            }
        }
        visualPose = visual.localPosition;
        if (head != null) { headPose = head.localRotation; head.rotation = Quaternion.AngleAxis(headLead, Vector3.up) * head.rotation; }
        if (releaseElapsed < 0)
        {
            // Uneven laundry and cushion edges can be several centimetres
            // below the authored centre. Bend the body to actual leg reach;
            // never extend a bone to force the foot onto that lower surface.
            float requiredLowering = prepared ? standingLowering : lowering;
            float maximumLowering = owner.StoreProductId == "balcony.hanging-chair" ? .20f : .15f;
            if (owner.StoreProductId == HomeStoreService.BedroomStarCanopyId)
                requiredLowering = Mathf.Max(requiredLowering, lowering);
            foreach (var leg in legs)
            {
                float reach = Vector3.Distance(leg.upper.position, leg.lower.position) +
                    Vector3.Distance(leg.lower.position, leg.foot.position) - .004f;
                Vector3 delta = leg.upper.position + weightShift - leg.target;
                float vertical = Mathf.Sqrt(Mathf.Max(0, reach*reach - delta.x*delta.x - delta.z*delta.z));
                requiredLowering = Mathf.Max(requiredLowering, Mathf.Min(maximumLowering, delta.y - vertical));
            }
            // Recover height as the legs come back under the body. Retaining
            // the deepest crouch caused a pop when the next jump took over.
            lowering = requiredLowering >= lowering ? requiredLowering :
                Mathf.MoveTowards(lowering, requiredLowering, Time.deltaTime * .45f);
        }
        visual.position += weightShift - Vector3.up * lowering;
        foreach (var leg in legs)
        {
            if (leg.upper == null || leg.lower == null || leg.foot == null) continue;
            leg.upperPose = leg.upper.localRotation; leg.lowerPose = leg.lower.localRotation; leg.footPose = leg.foot.localRotation;
            Vector3 origin = leg.upper.position, direction = leg.target - origin;
            float a = Vector3.Distance(origin, leg.lower.position), b = Vector3.Distance(leg.lower.position, leg.foot.position);
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .0001f, a + b - .0001f);
            Vector3 axis = direction.normalized;
            // Derive the knee/elbow plane around the SOURCE leg axis first,
            // then transport that plane to the requested ankle. Projecting a
            // nearly vertical thigh straight onto the new axis instead lets
            // a sideways foot offset turn the knee outward by almost 90 degrees.
            Vector3 sourceAxis = (leg.foot.position - origin).normalized;
            Vector3 sourceBend = Vector3.ProjectOnPlane(leg.lower.position - origin, sourceAxis);
            if (sourceBend.sqrMagnitude < .000025f)
                sourceBend = Vector3.ProjectOnPlane(leg.fore ? -transform.forward : transform.forward, sourceAxis);
            Vector3 bend = Quaternion.FromToRotation(sourceAxis, axis) * sourceBend.normalized;
            float along = (a*a - b*b + distance*distance) / (2f*distance);
            Vector3 elbow = origin + axis*along + bend*Mathf.Sqrt(Mathf.Max(0, a*a-along*along));
            leg.upper.rotation = Quaternion.FromToRotation(leg.lower.position-origin, elbow-origin)*leg.upper.rotation;
            leg.lower.rotation = Quaternion.FromToRotation(leg.foot.position-leg.lower.position, leg.target-leg.lower.position)*leg.lower.rotation;
            leg.foot.rotation = leg.targetRotation;
            MaximumContactError = Mathf.Max(MaximumContactError, Vector3.Distance(leg.foot.position, leg.target));
        }
        adjusted = true;
    }

    void Restore()
    {
        if (!adjusted) return; adjusted = false;
        if (visual != null) visual.localPosition = visualPose;
        if (head != null) head.localRotation = headPose;
        foreach (var leg in legs)
        {
            if (leg.upper != null) leg.upper.localRotation = leg.upperPose;
            if (leg.lower != null) leg.lower.localRotation = leg.lowerPose;
            if (leg.foot != null) leg.foot.localRotation = leg.footPose;
        }
    }
    public void Clear()
    {
        Restore(); owner = null; holding = false; prepared = false; supportPoints.Clear();
        lowering = 0; releaseElapsed = -1; visual = null;
        weightShift = Vector3.zero; headLead = 0; head = null;
        if (support != null) Destroy(support.gameObject); support = null;
    }
    void OnDisable() { Clear(); }
}
