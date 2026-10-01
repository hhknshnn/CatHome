using UnityEngine;

/// <summary>Short turn overlay on the existing gait. Never translates the cat or schedules footfalls.</summary>
[DefaultExecutionOrder(670)]
[DisallowMultipleComponent]
public sealed class CatNaturalTurnMotion : MonoBehaviour
{
    sealed class Leg
    {
        public Transform upper, lower, foot;
        public Quaternion upperPose, lowerPose, footPose;
        public Vector3 anchor;
        public float sole, heading;
        public bool planted;
    }
    readonly Leg[] legs = { new Leg(), new Leg(), new Leg(), new Leg() };
    Animator animator;
    Transform spine;
    Quaternion spinePose;
    float yawRate, weight;
    int signalledFrame = -10;
    bool adjusted;
    public float LastRawContactTravel { get; private set; }
    public float LastCorrectedContactTravel { get; private set; }
    public int ContactSamples { get; private set; }

    public static CatNaturalTurnMotion For(CatMovement cat) =>
        cat.GetComponent<CatNaturalTurnMotion>() ?? cat.gameObject.AddComponent<CatNaturalTurnMotion>();

    public void Signal(float degreesPerSecond)
    {
        if (Time.deltaTime > 0f) yawRate = degreesPerSecond;
        signalledFrame = Time.frameCount;
    }

    // The tangential speed drives the existing measured walk cadence, including
    // when the root stays still. One reverse pivot uses less than one gait cycle.
    public static float GaitSpeed(float degreesPerSecond) =>
        Mathf.Min(.72f, Mathf.Abs(degreesPerSecond) * Mathf.Deg2Rad * .105f);

    void Update() { Restore(); }
    void Bind(Animator current)
    {
        Restore(); animator = current; spine = null;
        var bones = current.GetComponentsInChildren<Transform>();
        foreach (var bone in bones) if (bone.name == "DEF-spine.002") spine = bone;
        for (int i = 0; i < 4; i++)
        {
            var l = legs[i]; l.upper = l.lower = l.foot = null; l.planted = false;
            string side = i % 2 == 0 ? "L" : "R";
            foreach (var b in bones)
            {
                if (b.name == (i < 2 ? "DEF-upper_arm." : "DEF-thigh.") + side) l.upper = b;
                if (b.name == (i < 2 ? "DEF-forearm." : "DEF-shin.") + side) l.lower = b;
                if (b.name == (i < 2 ? "DEF-hand." : "DEF-foot.") + side) l.foot = b;
            }
            if (l.foot != null) l.sole = l.foot.position.y - transform.position.y;
        }
    }
    void LateUpdate()
    {
        LastRawContactTravel = LastCorrectedContactTravel = 0f;
        var current = GetComponentInChildren<Animator>();
        if (current == null) return;
        if (animator != current) Bind(current);
        float active = signalledFrame == Time.frameCount ? Mathf.InverseLerp(25f, 160f, Mathf.Abs(yawRate)) : 0f;
        var activity = GetComponent<CatActivityAnimation>();
        // Airborne poses remain untouched. Supported pivots run before the
        // furniture solver, which retains final collision/contact ownership.
        if (activity != null && !activity.IsSupportedPivot && (activity.IsNativeJump || activity.ContactSurface != null ||
            activity.IsActive && activity.CurrentPose != CatActivityPose.Walk)) { active = 0f; weight = 0f; }
        weight = Mathf.MoveTowards(weight, active, Time.deltaTime * 14f);
        if (weight <= 0f) { foreach (var l in legs) l.planted = false; return; }
        if (spine != null)
        {
            spinePose = spine.localRotation;
            spine.rotation = Quaternion.AngleAxis(Mathf.Sign(yawRate) * 1.5f * weight, transform.forward) * spine.rotation;
        }
        foreach (var l in legs)
        {
            if (l.upper == null || l.lower == null || l.foot == null) continue;
            l.upperPose = l.upper.localRotation; l.lowerPose = l.lower.localRotation; l.footPose = l.foot.localRotation;
            Vector3 source = l.foot.position;
            float height = source.y - transform.position.y;
            l.sole = Mathf.Min(l.sole, height);
            // Source gait decides contact. Release before the leg stretches;
            // no fabricated four-paw step order, and no root-position correction.
            if (height > l.sole + .014f || Mathf.Abs(Mathf.DeltaAngle(l.heading, transform.eulerAngles.y)) > 22f ||
                Vector3.Distance(source, l.anchor) > .065f) l.planted = false;
            if (!l.planted && height <= l.sole + .008f)
            { l.anchor = source; l.heading = transform.eulerAngles.y; l.planted = true; }
            if (!l.planted) continue;
            Vector3 correction = l.anchor - source; correction.y = 0f;
            Vector3 target = source + Vector3.ClampMagnitude(correction, .04f) * weight;
            float raw = Vector3.ProjectOnPlane(source - l.anchor, Vector3.up).magnitude;
            Quaternion orientation = l.foot.rotation;
            Solve(l, target);
            l.foot.rotation = orientation;
            LastRawContactTravel += raw;
            LastCorrectedContactTravel += Vector3.ProjectOnPlane(l.foot.position - l.anchor, Vector3.up).magnitude;
            if (raw > .001f) ContactSamples++;
        }
        adjusted = true;
    }
    static void Solve(Leg l, Vector3 target)
    {
        Vector3 origin = l.upper.position, sourceAxis = (l.foot.position - origin).normalized;
        Vector3 delta = target - origin;
        float a = Vector3.Distance(origin,l.lower.position), b = Vector3.Distance(l.lower.position,l.foot.position);
        float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a-b)+.0001f, a+b-.0001f);
        Vector3 axis = delta.normalized;
        Vector3 bend = Vector3.ProjectOnPlane(l.lower.position-origin,sourceAxis);
        if (bend.sqrMagnitude < .000001f) return;
        bend = Quaternion.FromToRotation(sourceAxis,axis)*bend.normalized;
        float along = (a*a-b*b+distance*distance)/(2f*distance);
        Vector3 joint = origin+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        l.upper.rotation = Quaternion.FromToRotation(l.lower.position-origin,joint-origin)*l.upper.rotation;
        l.lower.rotation = Quaternion.FromToRotation(l.foot.position-l.lower.position,target-l.lower.position)*l.lower.rotation;
    }
    void Restore()
    {
        if (!adjusted) return; adjusted = false;
        foreach (var l in legs)
        {
            if (l.upper != null) l.upper.localRotation = l.upperPose;
            if (l.lower != null) l.lower.localRotation = l.lowerPose;
            if (l.foot != null) l.foot.localRotation = l.footPose;
        }
        if (spine != null) spine.localRotation = spinePose;
    }
    void OnDisable() { Restore(); weight=0f; foreach(var l in legs) l.planted=false; }
}
