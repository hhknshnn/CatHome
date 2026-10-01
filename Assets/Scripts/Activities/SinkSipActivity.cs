using System.Collections;
using UnityEngine;

/// <summary>
/// Drink from the bathroom vanity tap.
///
/// The counter stands 0.83 above the floor and the cat has no jump, so like the
/// star tipi and the shower this is scripted: the controller is switched off,
/// the cat is lifted onto the counter, laps at the running water and hops back
/// down, ending outside the product footprint before physics resumes.
///
/// Thirst is topped up with <see cref="ThirstSystem.RestoreThirst"/> rather than
/// BeginDrinking, because BeginDrinking fires Drank, which already records a
/// Drink quest — and CatActivity records one on completion too.
/// </summary>
[DisallowMultipleComponent]
public sealed class SinkSipActivity : CatActivity
{
    [Header("Sink sip")]
    [SerializeField] private Transform floorPoint;
    [SerializeField] private Transform perchPoint;
    [SerializeField] private Transform sipTarget;
    [SerializeField, Min(0.5f)] private float sipDuration = 2.4f;
    [SerializeField, Min(0f)] private float thirstRestore = 45f;
    [SerializeField, Range(0f, 100f)] private float notThirstyAbove = 96f;

    private CharacterController characterController;
    private ThirstSystem thirst;
    private Vector3 originalScale;
    private CatSipHeadMotion sipHead;
    private Transform workingPerch;

    public override CatCareNeed RequiredCareNeed => CatCareNeed.Water;
    public override string ProgressLabel => IsRunning ? "SIPPING..." : string.Empty;

    public float SipDuration => Mathf.Max(0.5f, sipDuration);
    public float ThirstRestore => Mathf.Max(0f, thirstRestore);
    public float NotThirstyAbove => Mathf.Clamp(notThirstyAbove, 0f, 100f);
    public bool InspectingOnly => false; // Compatibility: full cats now refuse before starting.
    public Transform SipTarget => sipTarget;
    public Transform PerchPoint => workingPerch != null ? workingPerch : perchPoint;
    public bool IsSipping { get; private set; }
    protected override bool RecordsQuestProgress => !InspectingOnly;
    protected override bool UsesFloorApproach => false;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        return floorPoint != null && perchPoint != null &&
            CatActivityStartResolver.GroundLaunch(this, actor, floorPoint.position, LandingPosition, out start);
    }
    private Vector3 LandingPosition => perchPoint.position +
        (StoreProductId == HomeStoreService.GardenBirdBathId && sipTarget != null
            ? Vector3.ProjectOnPlane(sipTarget.position - perchPoint.position, Vector3.up).normalized * .055f
            : Vector3.zero);

    protected override bool CanBeginActivity(out string failureReason)
    {
        if (floorPoint == null || perchPoint == null)
        {
            failureReason = "THE TAP IS NOT READY";
            return false;
        }

        if (thirst == null)
            thirst = FindAnyObjectByType<ThirstSystem>(FindObjectsInactive.Include);


        failureReason = string.Empty;
        return true;
    }

    protected override bool BeginActivity()
    {
        characterController = Cat.GetComponent<CharacterController>();
        originalScale = Cat.transform.localScale;
        if(StoreProductId==HomeStoreService.GardenBirdBathId&&sipTarget!=null)
        {
            // Keep the hind paws inside the shallow bowl instead of on its
            // sloping rim. One fixed support serves landing, sipping and exit.
            workingPerch=Instantiate(perchPoint,transform,true);
            workingPerch.name="Bird bath pose support";workingPerch.gameObject.hideFlags=HideFlags.DontSave;
            workingPerch.position=LandingPosition;
        }
        StartCoroutine(SipRoutine());
        return true;
    }

    private IEnumerator SipRoutine()
    {
        var perchPoint=PerchPoint;
        Cat.SetMovementLocked(this, true);
        if (characterController != null)
            characterController.enabled = false;

        Vector3 floor = Flatten(floorPoint.position, AcceptedStart.Position.y);
        Vector3 perch = perchPoint.position;
        Quaternion inward = LookTowards(perch - floor, AcceptedStart.Rotation);

        // Shared Jump owns its real skeletal preparation and landing.
        bool fountain = StoreProductId == HomeStoreService.PatioWaterFountainId && sipTarget != null;
        if (fountain)
        {
            var working = LookTowards(sipTarget.position-perch, inward);
            yield return CatActivityMotion.Jump(Cat, AcceptedStart.Position, perch, inward, working);
            inward = working;
        }
        else yield return Hop(AcceptedStart.Position, perch, inward, 0.42f);

        // The builder supplies the real water contact and a supported perch.
        // Preserve the existing inward reach for older, unbound fountains.
        if (sipTarget != null)
        {
            inward = LookTowards(sipTarget.position - perch, inward);
            if (!fountain)
            {
                PlayCatPose(CatActivityPose.Sniff, perchPoint);
                yield return CatActivityFacing.Turn(Cat, inward, .24f);
            }
        }

        float elapsed = 0f;
        IsSipping = true;
        PlayCatPose(CatActivityPose.Drink, perchPoint);
        if(sipTarget!=null)
        {
            sipHead=Cat.GetComponent<CatSipHeadMotion>()??Cat.gameObject.AddComponent<CatSipHeadMotion>();
            if(!sipHead.Begin(this,sipTarget))
            {
                // Missing/stale bindings must release the current routine;
                // never award water for an unperformed drinking contact.
                CancelForTransition();
                yield break;
            }
        }
        while (elapsed < SipDuration)
        {
            elapsed += Time.deltaTime;
            // The Eating/Drink skeleton supplies the real head lap. Keep all
            // four planted paws on the support instead of bobbing/pitching the
            // whole body beneath that animation.
            Cat.transform.SetPositionAndRotation(perch, inward);
            sipHead?.Sample(this,Mathf.SmoothStep(0,1,Mathf.Min(elapsed/.2f,(SipDuration-elapsed)/.2f)));
            yield return null;
        }

        IsSipping = false;
        sipHead?.Stop(this);
        Cat.transform.rotation = inward;
        Quaternion outward = LookTowards(floor - perch, inward);
        if(fountain)
        {
            // Finish the drinking posture before freezing the brief pivot.
            // The supported crossfade avoids a low chest snapping into Jump.
            PlayCatPose(CatActivityPose.GentleKnead,perchPoint);
            yield return new WaitForSeconds(.20f);
        }
        else
            yield return Move(perch, perch, inward, outward, 0.20f);
        yield return Hop(perch, floor, outward, 0.38f);

        RestoreCat();
        if (thirst != null)
            thirst.RestoreThirst(ThirstRestore);
        CompleteActivity("REFRESHING!");
    }

    /// <summary>Arc between two points, peaking above the higher end.</summary>
    private IEnumerator Hop(Vector3 from, Vector3 to, Quaternion facing, float duration)
    {
        yield return CatActivityMotion.Jump(Cat,from,to,Cat.transform.rotation,facing);
    }

    private IEnumerator Move(
        Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation, float duration)
    {
        Vector3 direction = to - from; direction.y = 0f;
        PlayCatPose(CatActivityPose.Sniff);
        if (direction.sqrMagnitude < .000001f)
        {
            if (Quaternion.Angle(Cat.transform.rotation, toRotation) > .1f)
                yield return CatActivityFacing.Turn(Cat, toRotation, duration);
            yield break;
        }
        Quaternion travel = Quaternion.LookRotation(direction);
        yield return CatActivityFacing.Turn(Cat, travel, .18f);
        PlayCatPose(CatActivityPose.Walk);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            Cat.transform.SetPositionAndRotation(Vector3.Lerp(from, to, t), travel);
            yield return null;
        }
        Cat.transform.SetPositionAndRotation(to, travel);
        if (Quaternion.Angle(travel, toRotation) > .1f)
        {
            PlayCatPose(CatActivityPose.Sniff);
            yield return CatActivityFacing.Turn(Cat, toRotation, .18f);
        }
    }

    private void RestoreCat()
    {
        IsSipping = false;
        sipHead?.Stop(this);
        if(workingPerch!=null){Destroy(workingPerch.gameObject);workingPerch=null;}
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
    [System.Serializable] public sealed class ContactDiagnostic
    {
        public string product, breed, pose, animation, nextAnimation, supportName, skinName;
        public float normalizedTime;
        public Vector3 rootPosition, rootScale, rootEuler, perchPosition, perchEuler, perchScale, targetPosition;
        public Vector3 visualLocalPosition, visualLocalScale, visualLocalEuler, bodyMinimumInSupport, bodyMaximumInSupport;
        public Vector3 jawWorld, jawInSupport, jawToWater, closestJawSurfaceWorld;
        public int jawSurfaceVertexCount;
        public float closestJawSurfaceToWater;
        public float solvedMouthDistance,headDeflection;
        public int solvedMouthVertices;
        public Vector3 sourceMouthPosition,neckBasePosition,jointDeflections;
        public float sourceMouthDistance,physicalNeckReach,requiredNeckReach,neckReachDeficit;
        public float forequarterDeflection,pawPlantError;
        public Vector3 reachedNeckBasePosition;
        public ContactPawDiagnostic[] paws;
    }
    [System.Serializable] public sealed class ContactPawDiagnostic
    {
        public string name;
        public Vector3 world, inProduct, inSupport, inActor;
        public bool supported;
        public bool surfaceWithin35mmOfPerch;
        public float nearestProductSurfaceGap;
        public string[] rayHits;
    }
    /// <summary>Read-only native QA evidence; never changes the pose or support.</summary>
    public ContactDiagnostic EditorCaptureContactDiagnostic()
    {
        var perchPoint=PerchPoint;
        var animator=Cat.GetComponentInChildren<Animator>();
        var pose=Cat.GetComponent<CatActivityAnimation>();
        var report=new ContactDiagnostic {
            product=StoreProductId,breed=CatBreedService.SelectedBreedId,pose=pose.CurrentPose.ToString(),
            supportName=perchPoint.name+" parent="+perchPoint.parent.name,
            rootPosition=Cat.transform.position,rootScale=Cat.transform.lossyScale,rootEuler=Cat.transform.eulerAngles,
            perchPosition=perchPoint.position,perchEuler=perchPoint.eulerAngles,perchScale=perchPoint.lossyScale,
            targetPosition=sipTarget!=null?sipTarget.position:Vector3.zero,
            visualLocalPosition=animator.transform.localPosition,visualLocalScale=animator.transform.localScale,
            visualLocalEuler=animator.transform.localEulerAngles,normalizedTime=animator.GetCurrentAnimatorStateInfo(0).normalizedTime
        };
        if(sipHead!=null)
        {
            report.solvedMouthDistance=sipHead.Distance;report.headDeflection=sipHead.TotalDeflection;report.solvedMouthVertices=sipHead.MouthVertexCount;
            report.sourceMouthPosition=sipHead.SourceMouthPosition;report.neckBasePosition=sipHead.NeckBasePosition;report.jointDeflections=sipHead.JointDeflections;
            report.sourceMouthDistance=sipHead.SourceDistance;report.physicalNeckReach=sipHead.PhysicalChainLength;
            report.requiredNeckReach=sipHead.RequiredReach;report.neckReachDeficit=sipHead.ReachDeficit;
            report.forequarterDeflection=sipHead.ForequarterDeflection;report.pawPlantError=sipHead.PawPlantError;
            report.reachedNeckBasePosition=sipHead.ReachedNeckBasePosition;
        }
        foreach(var clip in animator.GetCurrentAnimatorClipInfo(0))report.animation+=(clip.clip.name+"@"+clip.weight.ToString("F3")+" ");
        foreach(var clip in animator.GetNextAnimatorClipInfo(0))report.nextAnimation+=(clip.clip.name+"@"+clip.weight.ToString("F3")+" ");
        var jaw=CatBreedVisualFactory.FindDescendant(Cat.transform,"DEF-jaw");
        report.jawWorld=jaw.position;report.jawInSupport=perchPoint.InverseTransformPoint(jaw.position);
        report.jawToWater=report.targetPosition-jaw.position;
        var names=new[]{"DEF-hand.L","DEF-hand.R","DEF-foot.L","DEF-foot.R"};
        report.paws=new ContactPawDiagnostic[names.Length];
        for(int i=0;i<names.Length;i++)
        {
            var paw=CatBreedVisualFactory.FindDescendant(Cat.transform,names[i]);
            var item=new ContactPawDiagnostic{name=names[i],world=paw.position,inProduct=transform.InverseTransformPoint(paw.position),
                inSupport=perchPoint.InverseTransformPoint(paw.position),inActor=Cat.transform.InverseTransformPoint(paw.position),nearestProductSurfaceGap=999f};
            var hits=Physics.RaycastAll(paw.position+Vector3.up*.10f,Vector3.down,.55f,~0,QueryTriggerInteraction.Ignore);
            item.rayHits=new string[hits.Length];
            for(int h=0;h<hits.Length;h++)
            {
                var hit=hits[h];bool owned=hit.transform.IsChildOf(transform);
                item.supported|=owned&&hit.normal.y>.5f;
                if(owned&&hit.normal.y>.5f)
                {
                    item.surfaceWithin35mmOfPerch|=Mathf.Abs(hit.point.y-perchPoint.position.y)<=.035f;
                    if(Mathf.Abs(paw.position.y-hit.point.y)<Mathf.Abs(item.nearestProductSurfaceGap))item.nearestProductSurfaceGap=paw.position.y-hit.point.y;
                }
                item.rayHits[h]=hit.transform.name+" type="+hit.collider.GetType().Name+" owned="+owned+" point="+hit.point.ToString("F5")+" normal="+hit.normal.ToString("F4");
            }
            report.paws[i]=item;
        }
        var mesh=new Mesh();
        try
        {
            var skin=Cat.GetComponentInChildren<SkinnedMeshRenderer>();skin.BakeMesh(mesh,true);var vertices=mesh.vertices;
            report.skinName=skin.name+" lossyScale="+skin.transform.lossyScale.ToString("F5");
            var indices=CatBreedService.SelectedEntry.ContactVertexIndices;
            var bounds=new Bounds(perchPoint.InverseTransformPoint(skin.transform.TransformPoint(vertices[indices[0]])),Vector3.zero);
            foreach(int index in indices)bounds.Encapsulate(perchPoint.InverseTransformPoint(skin.transform.TransformPoint(vertices[index])));
            report.bodyMinimumInSupport=bounds.min;report.bodyMaximumInSupport=bounds.max;
            // A jaw pivot is not mouth contact. Also report the closest actual
            // skinned surface vertex whose jaw weight is at least one quarter.
            report.closestJawSurfaceToWater=999f;
            var profile=CatSipMouthCatalog.Load()?.Find(CatBreedService.SelectedBreedId);
            if(profile!=null)foreach(var vertex in profile.vertices)
            {
                report.jawSurfaceVertexCount++;
                Vector3 at=skin.transform.TransformPoint(vertices[vertex.vertexIndex]);float distance=Vector3.Distance(at,report.targetPosition);
                if(distance>=report.closestJawSurfaceToWater)continue;
                report.closestJawSurfaceToWater=distance;report.closestJawSurfaceWorld=at;
            }
        }
        finally{DestroyImmediate(mesh);}
        return report;
    }

    public void EditorConfigureSipTarget(Transform target) => sipTarget = target;

    public void EditorConfigureSip(
        Transform floor, Transform perch, float duration, float restore, float thirstyBelow)
    {
        floorPoint = floor;
        perchPoint = perch;
        sipDuration = Mathf.Max(0.5f, duration);
        thirstRestore = Mathf.Max(0f, restore);
        notThirstyAbove = Mathf.Clamp(thirstyBelow, 0f, 100f);
    }
#endif
}
