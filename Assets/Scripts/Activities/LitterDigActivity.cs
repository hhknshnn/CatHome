using System.Collections;
using UnityEngine;

/// <summary>
/// Enter, inspect and dig real sand, briefly crouch, cover and leave the tray.
///
/// The tray is a walk-in like the star tipi and the shower, so its product box
/// becomes a trigger — the 0.15 sill is well above the CharacterController's
/// step offset and would otherwise stop the cat dead at the mouth. The
/// controller is switched off for the routine and the cat always ends up
/// outside the footprint before physics resumes, and it is never re-parented
/// under the product.
///
/// Bathroom sand has a reversible deformed mesh and measured alternating paws.
/// Unrelated authored uses retain their previous motion.
/// </summary>
[DisallowMultipleComponent]
public sealed class LitterDigActivity : CatActivity
{
    [Header("Litter dig")]
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private Transform digPoint;
    [SerializeField] private Transform digFacingPoint;
    [SerializeField, Min(0.5f)] private float digDuration = 2.6f;
    [SerializeField, Min(1)] private int scrapeCount = 4;

    private CharacterController characterController;
    private Vector3 originalScale;
    private CatLitterRoutineMotion litterMotion;
    private Transform toiletSupport;
    private int completedToilets;
    private CatLitterWasteFx waste;
    private float nextToiletSpeech;
    private CatSupportedFurnitureMotion planterMotion;
    private CatGentleKneadMotion planterPaws;
    private Vector3 planterFloor;
    public bool UsesRaisedPlanter => !UsesGentleScraping && digPoint != null && digPoint.position.y-transform.position.y>.16f;
    public CatLitterWasteFx Waste => waste;
    public bool IsSolidToilet { get; private set; }
    [SerializeField] private CatLitterSandSurface litterSurface;
    public CatLitterPhase Phase { get; private set; }
    public CatLitterSandSurface LitterSurface => litterSurface;

    public override string ProgressLabel => IsRunning ? "DIGGING..." : string.Empty;

    public float DigDuration => Mathf.Max(0.5f, digDuration);
    public int ScrapeCount => Mathf.Max(1, scrapeCount);
    public bool UsesGentleScraping => Kind == CatActivityKind.LitterDig &&
        StoreProductId == HomeStoreService.BathroomLitterBoxId;
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        if (mouthPoint == null || digPoint == null) return false;
        return UsesRaisedPlanter
            ? CatActivityStartResolver.GroundLaunch(this, actor, mouthPoint.position, digPoint.position, out start)
            : CatActivityStartResolver.Facing(actor, mouthPoint.position, .22f, digPoint.position, 35f, out start);
    }

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (mouthPoint == null || digPoint == null || (UsesGentleScraping && (litterSurface == null || !litterSurface.IsConfigured)))
        {
            failureReason = "THE TRAY IS NOT READY";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        if (UsesGentleScraping && !litterSurface.Begin(this)) return false;
        IsSolidToilet = completedToilets % 3 == 0;
        StartCoroutine(UsesRaisedPlanter?RaisedPlanterRoutine():DigRoutine());
        return true;
    }

    private IEnumerator RaisedPlanterRoutine()
    {
        Cat.SetMovementLocked(this,true);if(characterController!=null)characterController.enabled=false;
        planterFloor=Flatten(mouthPoint.position,AcceptedStart.Position.y);
        Vector3 dig=digPoint.position;
        Quaternion launch=AcceptedStart.Rotation;
        // Landing, planted turn and digging must agree on the same long
        // surface axis. These three measured soil bays use local X; their
        // surface does not opt into the nap-only visual alignment flag.
        Quaternion preferred=digPoint.rotation*Quaternion.Euler(0,90,0);
        Quaternion facing=CatActivityFacing.AlongAxis(Cat,dig,preferred);
        Vector3 landing=planterFloor;
        if(StoreProductId==HomeStoreService.PatioHerbTroughId)
        {
            // A diagonal departure gives the crouch room beside the wall.
            // Validate the landing against the other placed patio furniture.
            var outward=Vector3.ProjectOnPlane(planterFloor-dig,Vector3.up).normalized;
            bool found=false;
            foreach(float side in new[]{1f,-1f})
            {
                var direction=(outward+facing*Vector3.forward*side).normalized;
                foreach(float distance in new[]{.90f,1.05f,1.20f})
                {
                    var candidate=Flatten(dig+direction*distance,planterFloor.y);
                    if(!CatActivityMotion.IsControllerFloorClear(Cat,candidate)||!CatActivityMotion.TryFloorPath(candidate,planterFloor,out _))continue;
                    landing=candidate;found=true;break;
                }
                if(found)break;
            }
            if(!found){CancelForTransition();yield break;}
        }
        Phase=CatLitterPhase.Entering;
        planterMotion=new CatSupportedFurnitureMotion(this,Cat,digPoint);
        yield return planterMotion.Jump(AcceptedStart.Position,dig,launch,facing);
        yield return planterMotion.Pose(CatActivityPose.GentleKnead,.20f,dig,facing);
        Phase=CatLitterPhase.Digging;
        planterPaws=Cat.GetComponent<CatGentleKneadMotion>()??Cat.gameObject.AddComponent<CatGentleKneadMotion>();
        float elapsed=0,duration=Mathf.Max(DigDuration,ScrapeCount*CatGentleKneadMotion.ScrapeSeconds);
        while(elapsed<duration)
        {
            Cat.transform.SetPositionAndRotation(dig,facing);Cat.transform.localScale=originalScale;
            PlayCatPose(CatActivityPose.GentleKnead,digPoint);
            planterPaws.SampleScrape(this,elapsed,duration);
            yield return null;elapsed+=Time.deltaTime;
        }
        planterPaws.Clear();Phase=CatLitterPhase.Exiting;
        yield return planterMotion.Pose(CatActivityPose.GentleKnead,.20f,dig,facing);
        Quaternion away=LookTowards(landing-dig,facing);
        // Face the actual landing before takeoff, then keep one flight heading.
        yield return planterMotion.Jump(dig,landing,facing,away);
        RestoreCat();CompleteActivity("ALL COVERED UP!");
    }

    private IEnumerator DigRoutine()
    {
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Phase = CatLitterPhase.Entering;
        Vector3 start = AcceptedStart.Position;
        Vector3 mouth = Flatten(mouthPoint.position, start.y);
        Vector3 dig = digPoint.position;
        Quaternion inward = AcceptedStart.Rotation;
        // Step over the sill rather than hop: the litter surface is only 0.18
        // up, so an arc would read as the cat vaulting a kerb.
        yield return EnterTray(start, dig, inward, .40f);

        if (UsesGentleScraping)
        {
            var anchor = new GameObject("Litter routine support") { hideFlags = HideFlags.DontSave };
            toiletSupport = anchor.transform;
            toiletSupport.SetParent(transform, true);
            toiletSupport.SetPositionAndRotation(digPoint.position, digPoint.rotation);
            anchor.AddComponent<CatActivitySurface>();
            Quaternion preferred = digFacingPoint != null ? LookTowards(digFacingPoint.position - dig, inward) : inward;
            inward = CatActivityFacing.Resolve(Cat, dig, CatActivityFacing.AlongAxis(Cat, dig, preferred));
            PlayCatPose(CatActivityPose.GentleKnead, toiletSupport);
            yield return CatActivityFacing.Turn(Cat, inward, .4f);
            yield return new WaitForSeconds(.24f);
            litterMotion = Cat.GetComponent<CatLitterRoutineMotion>() ?? Cat.gameObject.AddComponent<CatLitterRoutineMotion>();
            yield return SandPhase(CatLitterPhase.Investigating, dig, inward, .8f);
            yield return SandPhase(CatLitterPhase.Digging, dig, inward, Mathf.Max(DigDuration, ScrapeCount * CatLitterRoutineMotion.StrokeSeconds));
            Vector3 pelvisStep = litterMotion.HoleTarget - litterMotion.PelvisPosition; pelvisStep.y = 0f;
            Vector3 toiletPosition = dig + pelvisStep;
            yield return SandStep(CatLitterPhase.Positioning, toiletPosition, inward);
            waste = GetComponent<CatLitterWasteFx>() ?? gameObject.AddComponent<CatLitterWasteFx>();
            waste.Begin(this, litterSurface, IsSolidToilet);
            yield return SandPhase(CatLitterPhase.Squatting, toiletPosition, inward, 2.8f);
            yield return SandStep(CatLitterPhase.Returning, dig, inward);
            Phase = CatLitterPhase.Covering;
            litterMotion.PrepareCovering(this);
            PlayCatPose(CatActivityPose.GentleKnead, toiletSupport);
            yield return new WaitForSeconds(.24f);
            yield return SandPhase(CatLitterPhase.Covering, dig, inward, 3f * CatLitterRoutineMotion.StrokeSeconds);
            litterSurface.Excavate(this, litterMotion.HoleTarget, 0f);
            litterMotion.Clear();
        }
        else
        {
            // Outdoor troughs use the same real contact patch from its visible
            // end. Turn only after entering; both axis directions fit the patch.
            inward = CatActivityFacing.AlongAxis(Cat, dig, inward);
            PlayCatPose(CatActivityPose.Sniff, digPoint);
            yield return CatActivityFacing.Turn(Cat, inward, .3f);
            float perScrape = DigDuration / ScrapeCount;
            for (int i = 0; i < ScrapeCount; i++)
            {
                float elapsed = 0f;
                PlayCatPose(CatActivityPose.Paw, digPoint);
                while (elapsed < perScrape)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / perScrape);
                    float stroke = Mathf.Sin(t * Mathf.PI);
                    Vector3 position = dig;
                    position += (inward * Vector3.forward) * (-stroke * 0.075f);
                    position.y = dig.y + stroke * 0.020f;
                    Cat.transform.position = position;
                    Cat.transform.rotation = inward * Quaternion.Euler(stroke * 13f, 0f, 0f);
                    Vector3 scale = originalScale;
                    scale.y *= 1f - stroke * 0.055f;
                    scale.z *= 1f + stroke * 0.045f;
                    Cat.transform.localScale = scale;
                    yield return null;
                }
            }
        }

        Cat.transform.localScale = originalScale;
        Cat.transform.position = dig;

        // Covering faces the same readable side as digging. Turn towards the
        // open mouth only when leaving the tray.
        Phase = CatLitterPhase.Exiting;
        Quaternion outward = LookTowards(mouth - dig, inward);
        yield return Move(dig, dig, inward, outward, UsesGentleScraping ? .4f : .26f);
        if (!UsesGentleScraping) for (int i = 0; i < 2; i++)
        {
            float elapsed = 0f;
            while (elapsed < 0.34f)
            {
                elapsed += Time.deltaTime;
                float stroke = Mathf.Sin(Mathf.Clamp01(elapsed / 0.34f) * Mathf.PI);
                Cat.transform.rotation = outward * Quaternion.Euler(stroke * 10f, 0f, 0f);
                Cat.transform.position = dig + (outward * Vector3.forward) * (stroke * 0.045f);
                yield return null;
            }
        }
        Cat.transform.rotation = outward;
        yield return Move(dig, mouth, outward, outward, 0.36f);
        yield return Move(mouth, Flatten(mouthPoint.position, start.y) +
                          (mouth - dig).normalized * 0.35f, outward, outward, 0.26f);

        if (UsesGentleScraping) completedToilets++;
        RestoreCat();
        CompleteActivity("ALL COVERED UP!");
    }

    private IEnumerator EnterTray(Vector3 from, Vector3 to, Quaternion facing, float minimumSeconds)
    {
        // Crossing the real sill is the activity itself. Start that motion at
        // the accepted stance without a separate walk-to-mouth or body turn.
        float distance = Vector3.ProjectOnPlane(to - from, Vector3.up).magnitude;
        float duration = Mathf.Max(minimumSeconds, distance / 1.5f), elapsed = 0f;
        var animation = Cat.GetComponent<CatActivityAnimation>();
        if (animation != null) animation.SetWalkSpeed(distance / duration, null);
        while (elapsed < duration)
        {
            Cat.transform.SetPositionAndRotation(Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration)), facing);
            yield return null;
            elapsed += Time.deltaTime;
        }
        Cat.transform.SetPositionAndRotation(to, facing);
    }

    private IEnumerator SandPhase(CatLitterPhase phase, Vector3 position, Quaternion rotation, float duration)
    {
        Phase = phase;
        float elapsed = 0f;
        bool spoke = false;
        var pose = Cat.GetComponent<CatActivityAnimation>();
        while (elapsed < duration)
        {
            if (phase == CatLitterPhase.Squatting)
            {
                // Hold partway through the genuine sit transition: the hips
                // lower and knees flex, without scaling or compressing the cat.
                float crouch = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed / .45f, (duration - elapsed) / .45f));
                pose.SetTimedPose(CatActivityPose.SitDown, (IsSolidToilet ? .38f : .62f) * crouch, toiletSupport);
                if (!spoke && elapsed >= 1.05f && Time.time >= nextToiletSpeech)
                {
                    ShowSpeech(IsSolidToilet ? "LITTER_STINKY" : "LITTER_PEE");
                    nextToiletSpeech = Time.time + 30f; spoke = true;
                }
            }
            litterMotion.Sample(this, litterSurface, phase, elapsed, duration);
            Cat.transform.SetPositionAndRotation(position, rotation);
            Cat.transform.localScale = originalScale;
            yield return null;
            elapsed += Time.deltaTime;
        }
    }
    private IEnumerator SandStep(CatLitterPhase phase, Vector3 target, Quaternion arrival)
    {
        Phase = phase; litterMotion.PrepareStep(this, phase);
        var pose = Cat.GetComponent<CatActivityAnimation>();
        pose.SetPose(CatActivityPose.GentleKnead, toiletSupport);
        yield return new WaitForSeconds(.18f);
        Vector3 from = Cat.transform.position;
        if (phase == CatLitterPhase.Returning)
        {
            // At the forward toilet stance a half-turn would swing the rear
            // paws over the tray's end. Turn sideways, then walk a short arc
            // back into its centre before returning to the original paw marks.
            Vector3 back = target - from; back.y = 0f; back.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, back);
            Vector3 plus = litterSurface.ClampPoint(from + side * .4f, .04f);
            Vector3 minus = litterSurface.ClampPoint(from - side * .4f, .04f);
            if (Vector3.Distance(minus, from) > Vector3.Distance(plus, from)) side = -side;
            yield return CatActivityFacing.Turn(Cat, Quaternion.LookRotation(side), .35f);
            const float radius = .20f;
            float arcDuration = radius * Mathf.PI * .5f / .42f, arcTime = 0f;
            pose.SetWalkSpeed(.42f, toiletSupport);
            while (arcTime < arcDuration)
            {
                arcTime += Time.deltaTime;
                float angle = Mathf.Clamp01(arcTime / arcDuration) * Mathf.PI * .5f;
                Vector3 point = from + radius * (side * Mathf.Sin(angle) + back * (1f - Mathf.Cos(angle)));
                Vector3 tangent = side * Mathf.Cos(angle) + back * Mathf.Sin(angle);
                Cat.transform.SetPositionAndRotation(point, Quaternion.LookRotation(tangent));
                toiletSupport.position = point;
                yield return null;
            }
            from = Cat.transform.position;
            pose.SetPose(CatActivityPose.GentleKnead, toiletSupport);
        }
        Quaternion travel = LookTowards(target - from, arrival);
        yield return CatActivityFacing.Turn(Cat, travel, .4f);
        float distance = Vector3.Distance(from, target), duration = Mathf.Max(.65f, distance / .42f), elapsed = 0f;
        pose.SetWalkSpeed(distance / duration, toiletSupport);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Vector3 position = Vector3.Lerp(from, target, Mathf.Clamp01(elapsed / duration));
            Cat.transform.SetPositionAndRotation(position, travel);
            toiletSupport.position = position;
            yield return null;
        }
        pose.SetPose(CatActivityPose.GentleKnead, toiletSupport);
        yield return CatActivityFacing.Turn(Cat, arrival, .4f);
        yield return new WaitForSeconds(.18f);
    }
    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        yield return CatActivityMotion.WalkAuthoredStep(Cat, to, toRotation, duration);
    }

    private void RestoreCat()
    {
        planterPaws?.Clear();
        if(planterMotion!=null)
        {
            planterMotion.End();planterMotion=null;
            // Shared prepared recovery owns interrupted floor restoration.
            // A completed landing must not be classified by absolute world Y.
        }
        if (litterMotion != null) litterMotion.Clear();
        if (waste != null) waste.Stop(this);
        if (litterSurface != null) litterSurface.Stop(this);
        if (toiletSupport != null)
        {
            if (Cat != null) Cat.GetComponent<CatActivityAnimation>()?.SetPose(CatActivityPose.GentleKnead);
            Destroy(toiletSupport.gameObject); toiletSupport = null;
        }
        Phase = CatLitterPhase.None;
        if (Cat == null)
            return;

        if (originalScale.sqrMagnitude > 0.0001f)
            Cat.transform.localScale = originalScale;
        if (characterController != null)
            characterController.enabled = true;
        Cat.SetMovementLocked(this, false);
    }

    private static Quaternion LookTowards(Vector3 direction, Quaternion fallback)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : fallback;
    }

    private static Vector3 Flatten(Vector3 point, float y)
    {
        point.y = y;
        return point;
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
    public void EditorConfigureLitterFacing(Transform point) => digFacingPoint = point;

    public void EditorConfigureLitterSurface(MeshFilter surface, Vector3 localCenter, Vector2 localSize)
    {
        litterSurface = GetComponent<CatLitterSandSurface>();
        if (litterSurface == null) litterSurface = gameObject.AddComponent<CatLitterSandSurface>();
        litterSurface.EditorConfigure(surface, localCenter, localSize);
    }

    public void EditorConfigureDig(
        Transform mouth, Transform dig, float duration, int scrapes)
    {
        mouthPoint = mouth;
        digPoint = dig;
        digDuration = Mathf.Max(0.5f, duration);
        scrapeCount = Mathf.Max(1, scrapes);
    }
#endif
}
