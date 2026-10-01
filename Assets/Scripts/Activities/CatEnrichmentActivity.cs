using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CatEnrichmentMode { Nap, Tunnel, Chase, Spring, Track, Ribbon, Roller, Feed, Grass, Hide }

/// <summary>Authored cat-to-toy routines, with geometry-derived contact and moving pivots.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(550)]
public sealed class CatEnrichmentActivity : CatActivity
{
    [SerializeField] CatEnrichmentMode mode;
    [SerializeField] Transform contactPoint;
    [SerializeField] Transform exitPoint;
    [SerializeField] Transform movingPart;
    [SerializeField] float duration=3.2f;
    [SerializeField] Vector2 footprint;
    [SerializeField] Vector2 foodSurfaceRadii;
    CharacterController controller;
    bool controllerWasEnabled;
    Vector3 startPosition, originalScale, movingPosition;
    Quaternion startRotation, movingRotation;
    List<Vector3> approach;
    bool captured;
    Transform[] tailBones;
    Quaternion[] tailPose;
    bool tailAdjusted;
    float tailBlend;
    CatToyContactMotion toyContact;
    CatActivityAnimation poseDriver;
    CatSpringToyMotion springMotion;
    CatMouseToyMotion mouseMotion;
    CatMeshContactSurface.TargetSet mouseSurfaces;
    CatMeshContactSurface.Hit mouseHit;
    int mouseVariant;
    bool IsMouse=>mode==CatEnrichmentMode.Chase&&StoreProductId==HomeStoreService.ToyMouseId;
    CatMeshContactSurface.TargetSet springSurfaces;
    CatMeshContactSurface.Hit springHit;
    float lastImpact=-100f;
    bool reverseTunnel;
    Vector3 workPoint, touchPosition;
    Vector3 mealReleasePoint;
    Quaternion mealHeading;
    bool IsCeramicMeal=>StoreProductId==HomeStoreService.CeramicBowlId&&mode==CatEnrichmentMode.Feed;
    public Vector2 FoodSurfaceRadii=>foodSurfaceRadii;
    protected override bool AllowsPerimeterApproach => mode != CatEnrichmentMode.Tunnel &&
        mode != CatEnrichmentMode.Hide && (mode != CatEnrichmentMode.Nap ||
        StoreProductId == HomeStoreService.NapPillowId || StoreProductId == HomeStoreService.CloudBedId);
    protected override bool UsesNearbyRoutineEntry => true;
    protected override bool UsesPreparedStart => true;
    protected override bool TryPrepareStart(CatMovement actor, out CatActivityStart start)
    {
        start = default;
        if (actor == null || contactPoint == null || exitPoint == null || RoutineEntryPoint == null) return false;
        if(IsMouse) return CatMouseToyMotion.Resolve(actor,movingPart,RoutineEntryPoint.position.y,ref mouseSurfaces,out start,out mouseHit);
        if(mode==CatEnrichmentMode.Spring)
            return CatSpringGeometry.Resolve(actor,movingPart,RoutineEntryPoint.position.y,ref springSurfaces,out start,out springHit);
        bool enter = mode == CatEnrichmentMode.Nap || mode == CatEnrichmentMode.Hide || mode == CatEnrichmentMode.Tunnel;
        if (enter)
        {
            bool front = CatActivityStartResolver.Facing(actor, RoutineEntryPoint.position, .18f,
                contactPoint.position, 20f, out start);
            if (mode != CatEnrichmentMode.Tunnel) return front;
            bool back = CatActivityStartResolver.Facing(actor, exitPoint.position, .18f,
                contactPoint.position, 20f, out var reverse);
            if (back && (!front || reverse.PromptDistance < start.PromptDistance)) start = reverse;
            return front || back;
        }
        Vector3 touch = contactPoint.position;
        if (movingPart != null)
        {
            Bounds bounds = new Bounds(touch, Vector3.zero); bool measured = false;
            foreach (var renderer in movingPart.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled) continue;
                if (!measured) { bounds = renderer.bounds; measured = true; } else bounds.Encapsulate(renderer.bounds);
            }
            if (measured)
            {
                Vector3 near = actor.transform.position; near.y = touch.y;
                touch = bounds.ClosestPoint(near);
            }
        }
        Vector3 inward = touch - actor.transform.position; inward.y = 0f;
        if (inward.sqrMagnitude < .0001f) return false;
        float reach = mode == CatEnrichmentMode.Grass ? .30f :
            StoreProductId == HomeStoreService.TreatJarId || StoreProductId == HomeStoreService.KibbleBagId ? .37f : .39f;
        if (IsCeramicMeal)
        {
            var tag = actor.GetComponentInChildren<CatBreedVisualTag>();
            var profile = CatFeedingAlignmentCatalog.Load()?.Find(tag != null ? tag.BreedId : CatBreedService.SelectedBreedId);
            if (profile == null) return false;
            Vector3 mouth = profile.mouthOffset * (actor.transform.lossyScale.y / .5f);
            Vector3 predicted = actor.transform.position + actor.transform.rotation * mouth;
            Vector3 local = contactPoint.InverseTransformPoint(predicted);
            float q = foodSurfaceRadii.x > 0f && foodSurfaceRadii.y > 0f ?
                local.x * local.x / (foodSurfaceRadii.x * foodSurfaceRadii.x) +
                local.z * local.z / (foodSurfaceRadii.y * foodSurfaceRadii.y) : float.PositiveInfinity;
            Vector3 centre = contactPoint.position - actor.transform.rotation * new Vector3(mouth.x, 0, mouth.z);
            centre.y = RoutineEntryPoint.position.y;
            bool ready = CatActivityStartResolver.Facing(actor, centre, .14f, contactPoint.position, 25f, out start);
            start.ActionTarget = contactPoint.TransformPoint(new Vector3(local.x, 0, local.z));
            return ready && q <= 1f;
        }
        Vector3 stand = touch - inward.normalized * reach; stand.y = RoutineEntryPoint.position.y;
        return CatActivityStartResolver.Facing(actor, stand, .12f, touch, 25f, out start);
    }
    public Vector3 ContactPosition => IsRunning ? touchPosition : contactPoint != null ? contactPoint.position : transform.position;
    public int ContactCount { get; private set; }
    public bool IsPerformingGesture { get; private set; }
    public float ClosestPawDistance { get; private set; }
    public const float RestEnergyPerSecond = .75f;
    public bool IsResting { get; private set; }
    public override float EnergyCost => mode == CatEnrichmentMode.Nap ? 0f : base.EnergyCost;
    public override bool SupportsContinuousRest=>mode==CatEnrichmentMode.Nap;

    public CatEnrichmentMode Mode=>mode;
    public override CatCareNeed RequiredCareNeed => mode == CatEnrichmentMode.Feed ? CatCareNeed.Food : CatCareNeed.None;
    public Transform ContactPoint=>contactPoint;
    public Transform ExitPoint=>exitPoint;
    public Transform MovingPart=>movingPart;
    public override bool TryGetPromptDistance(CatMovement cat, out float distance)
    {
        return base.TryGetPromptDistance(cat, out distance);
    }

    public override float DistanceTo(CatMovement cat)
    {
        float first=base.DistanceTo(cat);
        if(mode!=CatEnrichmentMode.Tunnel || cat==null || exitPoint==null)return first;
        Vector3 delta=cat.transform.position-exitPoint.position;delta.y=0;return Mathf.Min(first,delta.magnitude);
    }
    public override string ProgressLabel=>!IsRunning?string.Empty:mode==CatEnrichmentMode.Nap?
        (GameLanguageService.Current==GameLanguage.Turkish?"Dinleniyor · Enerji topluyor":"Resting · Recovering energy"):
        (GameLanguageService.Current==GameLanguage.Turkish?"Birlikte oyun zamanı":"Playtime together");

    protected override bool CanBeginActivity(out string failureReason)
    {
        failureReason=string.Empty;
        if(contactPoint==null||exitPoint==null||RoutineEntryPoint==null){failureReason="THE TOY IS NOT READY";return false;}
        touchPosition = AcceptedStart.ActionTarget;
        workPoint = AcceptedStart.Position;
        mealReleasePoint = AcceptedStart.Position;
        mealHeading = AcceptedStart.Rotation;
        approach = new List<Vector3>();
        reverseTunnel = mode == CatEnrichmentMode.Tunnel &&
            Vector3.Distance(AcceptedStart.ZoneCentre, exitPoint.position) < .01f;
        return true;
    }
    protected override bool BeginActivity()
    {
        controller=Cat.GetComponent<CharacterController>();controllerWasEnabled=controller!=null&&controller.enabled;
        startPosition=Cat.transform.position;startRotation=Cat.transform.rotation;originalScale=Cat.transform.localScale;
        if(movingPart!=null){movingPosition=movingPart.localPosition;movingRotation=movingPart.localRotation;}
        captured=true;Cat.SetMovementLocked(this,true);if(controller!=null)controller.enabled=false;
        toyContact=Cat.GetComponent<CatToyContactMotion>();
        if(toyContact==null)toyContact=Cat.gameObject.AddComponent<CatToyContactMotion>();
        poseDriver=Cat.GetComponent<CatActivityAnimation>(); ContactCount=0;ClosestPawDistance=float.PositiveInfinity;lastImpact=-100;
        if(mode==CatEnrichmentMode.Tunnel)
        {
            var bones=new List<Transform>();
            foreach(var bone in Cat.GetComponentsInChildren<Transform>())
                if(bone.name.StartsWith("DEF-tail.",System.StringComparison.Ordinal))bones.Add(bone);
            bones.Sort((a,b)=>string.CompareOrdinal(a.name,b.name));
            tailBones=bones.ToArray();tailPose=new Quaternion[tailBones.Length];tailBlend=0;
        }
        StartCoroutine(Routine());return true;
    }
    // Keep the real skeleton's tail behind the cat inside the cloth arch.
    // Restore the sampled pose before the next Animator evaluation, including
    // paused frames; the adjustment must never accumulate or survive the game.
    void Update(){RestoreTail();}
    void LateUpdate()
    {
        if(!IsRunning||tailBones==null||Cat==null)return;
        var pose=Cat.GetComponent<CatActivityAnimation>().CurrentPose;
        tailBlend=Mathf.MoveTowards(tailBlend,pose==CatActivityPose.Walk?0:1,Time.deltaTime*8);
        if(tailBlend<=0)return;
        for(int i=0;i<tailBones.Length;i++)
        {if(tailBones[i]==null)return;tailPose[i]=tailBones[i].localRotation;}
        tailAdjusted=true;
        Vector3 direction=(-Cat.transform.forward-Vector3.up*.16f).normalized;
        for(int i=0;i<tailBones.Length-1;i++)
        {
            Vector3 segment=tailBones[i+1].position-tailBones[i].position;
            if(segment.sqrMagnitude<.00001f)continue;
            Quaternion aimed=Quaternion.FromToRotation(segment,direction)*tailBones[i].rotation;
            tailBones[i].rotation=Quaternion.Slerp(tailBones[i].rotation,aimed,tailBlend);
        }
    }
    void RestoreTail()
    {
        if(!tailAdjusted)return;tailAdjusted=false;
        for(int i=0;i<tailBones.Length;i++)if(tailBones[i]!=null)tailBones[i].localRotation=tailPose[i];
    }
    IEnumerator Routine()
    {
        Vector3 entry = AcceptedStart.Position;
        bool enter = mode == CatEnrichmentMode.Nap || mode == CatEnrichmentMode.Hide || mode == CatEnrichmentMode.Tunnel;
        if (enter) yield return Walk(contactPoint.position, CatActivityPose.Crawl, true);

        if(mode==CatEnrichmentMode.Tunnel)
        {
            // Stay low inside the arch; a standing paw-swat would put the head
            // through the fabric even after a safe walk-in.
            PlayCatPose(CatActivityPose.Crawl);yield return React(.35f);
            // A newly placed obstacle can close the far end during play. Back
            // out through the entrance while still low instead of walking tall
            // back through the cloth or moving through the obstruction.
            Vector3 farEnd=reverseTunnel?RoutineEntryPoint.position:exitPoint.position;
            yield return Walk(CatActivityMotion.IsFloorClear(farEnd,.25f)?farEnd:entry,CatActivityPose.Crawl);
        }
        else if(mode==CatEnrichmentMode.Nap||mode==CatEnrichmentMode.Hide)
        {
            PlayCatPose(CatActivityPose.Sleep,contactPoint);yield return React(.25f);
            IsResting=mode==CatEnrichmentMode.Nap;
            if(mode==CatEnrichmentMode.Nap){while(KeepResting)yield return null;}
            else yield return React(duration);
            IsResting=false;
            yield return Walk(entry,CatActivityPose.Crawl);
        }
        else if(IsMouse)
        {
            yield return MouseHunt();
            if(ContactCount==0){CancelActivity();yield break;}
        }
        else if(mode==CatEnrichmentMode.Chase)
        {
            Vector3 center=Cat.transform.position;
            for(int beat=0;beat<2;beat++)
            {
                float side=(beat==0?-1:1)*footprint.x*.16f;
                Vector3 sidePoint=center+Cat.transform.right*side;
                if(CatActivityFacing.FacingDot(touchPosition-sidePoint,sidePoint,CatActivityFacing.CameraPosition(Cat))>=CatActivityFacing.MinimumViewDot &&
                    CatActivityMotion.ClearSegment(center,sidePoint))yield return Walk(sidePoint,CatActivityPose.Stalk);
                yield return Face(touchPosition);
                IsPerformingGesture = true;
                Vector3 launchPoint = Cat.transform.position;
                yield return CatActivityMotion.Jump(Cat, launchPoint, launchPoint,
                    Cat.transform.rotation, Cat.transform.rotation, .10f, false);
                IsPerformingGesture = false;
                yield return Gesture(beat==0?CatActivityPose.BatLeft:CatActivityPose.BatRight,.9f,true);
            }
        }
        else if(mode==CatEnrichmentMode.Spring)
        {
            yield return Face(touchPosition);
            springMotion=Cat.GetComponent<CatSpringToyMotion>()??Cat.gameObject.AddComponent<CatSpringToyMotion>();
            bool left=Cat.transform.InverseTransformPoint(touchPosition).x<=0;
            springMotion.Begin(left,springHit);
            yield return SpringGesture(left,.95f,false,0);
            yield return SpringGesture(left,1.35f,true,0);
            yield return SpringGesture(left,.55f,false,1);
            yield return SpringGesture(left,.95f,true,1);
            yield return SpringGesture(left,.85f,false,2);
            springMotion.Clear();
        }
        else
        {
            yield return Gesture(CatActivityPose.Sniff,.9f,false);
            if(mode==CatEnrichmentMode.Feed)
            {
                if(movingPart!=null)yield return Gesture(CatActivityPose.Push,1.15f,true);
                yield return Gesture(CatActivityPose.Eat,2.2f,false);
            }
            else if(mode==CatEnrichmentMode.Grass)
            {
                yield return Gesture(CatActivityPose.BatLeft,1.1f,true);
                yield return Gesture(CatActivityPose.Sniff,1.4f,false);
            }
            else if(mode==CatEnrichmentMode.Ribbon)
            {
                yield return Gesture(CatActivityPose.Stalk,.7f,false);
                yield return Gesture(CatActivityPose.Tug,1.4f,true);
                yield return Gesture(CatActivityPose.BatRight,.9f,true);
            }
            else if(mode==CatEnrichmentMode.Roller)
            {
                yield return Gesture(CatActivityPose.Push,1.25f,true);
                yield return Gesture(CatActivityPose.Sniff,.8f,false);
                yield return Gesture(CatActivityPose.BatLeft,1.1f,true);
            }
            else
            {
                yield return Gesture(CatActivityPose.BatLeft,1f,true);
                yield return Gesture(CatActivityPose.Sniff,.65f,false);
                yield return Gesture(CatActivityPose.BatRight,1.1f,true);
            }
        }
        // A floor toy can release the cat where it finished when the full
        // walking controller fits. Beds/hideouts still use their real exit.
        if(IsCeramicMeal&&!CatActivityMotion.IsControllerFloorClear(Cat,Cat.transform.position))
            yield return Walk(mealReleasePoint,CatActivityPose.Walk);
        bool clearFinish=!enter && CatActivityMotion.IsControllerFloorClear(Cat,Cat.transform.position);
        Vector3 exit=mode==CatEnrichmentMode.Tunnel||clearFinish?Cat.transform.position:entry;
        if(!CatActivityMotion.IsFloorClear(exit,.25f))exit=startPosition;
        if(HasNearbyApproach && !enter && CatActivityMotion.TryFloorPath(Cat.transform.position,exit,out var returnPath))
        {foreach(var point in returnPath)yield return Walk(point,CatActivityPose.Walk);}
        else if(!HasNearbyApproach || enter || CatActivityMotion.ClearSegment(Cat.transform.position,exit))
            yield return Walk(exit,CatActivityPose.Walk);
        Restore(false);
        CompleteActivity(GameLanguageService.Current==GameLanguage.Turkish?"Çok iyi geldi!":"That felt good!");
    }
    IEnumerator MouseHunt()
    {
        var geometry=CatSpringGeometry.Measure(Cat);
        var gaze=Cat.GetComponent<CatFurnitureGaze>()??Cat.gameObject.AddComponent<CatFurnitureGaze>();
        int variation=mouseVariant++%2;bool left=Cat.transform.InverseTransformPoint(touchPosition).x<=0;
        // Notice first, then let the existing root-turn owner orient the cat.
        float t=0;while(t<.35f){poseDriver.SetWalkSpeed(0,null);gaze.LookAt(touchPosition,Mathf.SmoothStep(0,1,t/.35f),38);t+=Time.deltaTime;yield return null;}
        yield return Face(touchPosition);
        Vector3 direction=Vector3.ProjectOnPlane(touchPosition-Cat.transform.position,Vector3.up).normalized;
        Vector3 end=Cat.transform.position+direction*CatMouseToyMotion.ApproachDistance(geometry);
        // One measured, speed-matched stalk step. No root motion during the paw strike.
        if(!CatActivityMotion.ClearSegment(Cat.transform.position,end,CatActivityMotion.ControllerFloorRadius(Cat)))yield break;
        float stepSeconds=variation==0?.70f:.86f;
        float speed=Vector3.Distance(Cat.transform.position,end)/stepSeconds;
        while(Vector3.Distance(Cat.transform.position,end)>geometry.Reach*.004f){poseDriver.SetWalkSpeed(speed,null);Cat.transform.position=Vector3.MoveTowards(Cat.transform.position,end,speed*Time.deltaTime);gaze.LookAt(touchPosition,1,38);yield return null;}
        Cat.transform.position=end;
        mouseMotion=Cat.GetComponent<CatMouseToyMotion>()??Cat.gameObject.AddComponent<CatMouseToyMotion>();
        mouseMotion.Begin(left,mouseHit);IsPerformingGesture=true;
        t=0;while(t<.48f){float p=t/.48f;poseDriver.SetTimedPose(CatActivityPose.Stalk,.12f+p*.16f);mouseMotion.Sample(touchPosition,Mathf.SmoothStep(0,1,p),0);t+=Time.deltaTime;yield return null;}
        bool struck=false;float strikeSeconds=variation==0?.62f:.76f;
        Vector3 mouseStart=movingPart.position,escape=direction+Cat.transform.right*(variation==0?.38f:-.3f);
        escape=Vector3.ProjectOnPlane(escape,Vector3.up).normalized;
        float slide=geometry.Reach*(variation==0?.55f:.42f);
        CatMouseToyMotion.Escape(movingPart,transform,escape,slide,out var escapeEnd);
        t=0;float hitTime=0;
        while(t<strikeSeconds+1.15f)
        {
            float p=Mathf.Clamp01(t/strikeSeconds);
            // Brief fold, fast extension, then a slower recovery: a ground-prey swipe.
            float reach=p<.24f?Mathf.SmoothStep(0,.28f,p/.24f):p<.44f?Mathf.SmoothStep(.28f,1,(p-.24f)/.20f):p<.61f?1:Mathf.SmoothStep(1,0,(p-.61f)/.39f);
            if(p>=1)reach=0;
            poseDriver.SetTimedPose(CatActivityPose.Stalk,.28f+(1-Mathf.Sin(p*Mathf.PI))*.12f);
            float distance=left?toyContact.LeftDistance:toyContact.RightDistance;
            if(p>=.4f&&p<.75f){ClosestPawDistance=Mathf.Min(ClosestPawDistance,distance);if(!struck&&distance<geometry.Paw*.10f){struck=true;ContactCount++;hitTime=t;}}
            if(struck){float q=Mathf.Clamp01((t-hitTime)/.46f);float ease=1-(1-q)*(1-q);movingPart.position=Vector3.Lerp(mouseStart,escapeEnd,ease);movingPart.localRotation=movingRotation*Quaternion.Euler(0,(variation==0?13:-10)*ease,0);}
            if(struck)reach=Mathf.Min(reach,Mathf.Max(0,1-(t-hitTime)/.16f));
            var point=mouseHit.IsValid?mouseHit.Filter.transform.TransformPoint(mouseHit.LocalPoint):movingPart.position;
            float body= p<1?1-.35f*p:Mathf.Lerp(.65f,0,Mathf.Clamp01((t-strikeSeconds)/1.15f));
            mouseMotion.Sample(point,body,reach);t+=Time.deltaTime;yield return null;
        }
        IsPerformingGesture=false;mouseMotion.Clear();gaze.Clear();
    }
    IEnumerator Gesture(CatActivityPose pose,float seconds,bool contact)
    {
        IsPerformingGesture=true;
        toyContact.Clear();float t=0;bool hit=false;
        while(t<seconds)
        {
            float phase=Mathf.Clamp01(t/seconds);
            poseDriver.SetTimedPose(pose,phase);
            if(contact)
            {
                toyContact.Reach(touchPosition,pose!=CatActivityPose.BatRight,phase);
                if(phase>=.34f&&phase<.75f)ClosestPawDistance=Mathf.Min(ClosestPawDistance,toyContact.Distance);
                // The response starts after the real, breed-scaled paw arrives.
                // Merely entering an animation state is not a hit.
                if(!hit && phase>=.34f && phase<.75f && toyContact.Distance<.085f)
                { hit=true;ContactCount++;lastImpact=Time.time; }
            }
            AnimateAfterContact(); t+=Time.deltaTime;yield return null;
        }
        toyContact.Clear();IsPerformingGesture=false;
    }
    IEnumerator SpringGesture(bool left,float seconds,bool strike,int variant)
    {
        IsPerformingGesture=true;float t=0;bool hit=false;
        Vector3 local=movingPart!=null?movingPart.InverseTransformPoint(touchPosition):touchPosition;
        while(t<seconds)
        {
            float p=Mathf.Clamp01(t/seconds);
            // Different anticipation/release timing, on the same existing paw clip.
            float sample=variant==0?p:Mathf.Clamp01(p<.28f?p*.55f:.154f+(p-.28f)*1.175f);
            poseDriver.SetTimedPose(strike?(left?CatActivityPose.BatLeft:CatActivityPose.BatRight):CatActivityPose.Sniff,strike?sample:p*.18f);
            AnimateAfterContact();
            Vector3 target=movingPart!=null?movingPart.TransformPoint(local):touchPosition;
            touchPosition=target;
            float weight=strike?Mathf.Sin(sample*Mathf.PI):0;
            springMotion.Sample(target,sample,weight);
            float distance=left?toyContact.LeftDistance:toyContact.RightDistance;
            if(strike&&sample>=.34f&&sample<.65f)
            {
                ClosestPawDistance=Mathf.Min(ClosestPawDistance,distance);
                if(!hit&&distance<springMotion.ContactTolerance){hit=true;ContactCount++;lastImpact=Time.time;}
            }
            t+=Time.deltaTime;yield return null;
        }
        IsPerformingGesture=false;
    }
    void AnimateAfterContact()
    {
        if(movingPart==null)return;
        float t=Time.time-lastImpact;
        if(t>2)return;
        float damping=Mathf.Exp(-t*2.4f),wave=Mathf.Sin(t*13)*damping;
        if(mode==CatEnrichmentMode.Spring)
        {
            var geometry=CatSpringGeometry.Measure(Cat);
            Vector3 away=Vector3.ProjectOnPlane(movingPart.position-Cat.transform.position,Vector3.up).normalized;
            Vector3 axis=movingPart.parent.InverseTransformDirection(Vector3.Cross(Vector3.up,away));
            float height=0;foreach(var renderer in movingPart.GetComponentsInChildren<Renderer>())height=Mathf.Max(height,renderer.bounds.max.y-movingPart.position.y);
            float angle=Mathf.Atan2(geometry.Height*.14f,Mathf.Max(height,geometry.Height))*Mathf.Rad2Deg;
            movingPart.localRotation=Quaternion.AngleAxis((wave<0?wave*.12f:wave)*angle,axis)*movingRotation;
        }
        else if(mode==CatEnrichmentMode.Track)
            movingPart.localRotation=movingRotation*Quaternion.Euler(0,180*(1-damping),0);
        else if(mode==CatEnrichmentMode.Roller)
            movingPart.localRotation=movingRotation*Quaternion.Euler(220*(1-damping),0,0);
        else if(mode==CatEnrichmentMode.Chase||mode==CatEnrichmentMode.Ribbon)
            movingPart.localPosition=movingPosition+new Vector3(wave*footprint.x*.17f,0,(1-damping)*footprint.y*.045f);
        else if(mode==CatEnrichmentMode.Feed)
            movingPart.localPosition=movingPosition+Vector3.up*(Mathf.Max(0,wave)*.025f);
        else movingPart.localRotation=movingRotation*Quaternion.Euler(wave*16,0,wave*9);
    }
    IEnumerator React(float seconds)
    {
        float t=0;
        while(t<seconds)
        {
            t+=Time.deltaTime;
            if(movingPart!=null)
            {
                float wave=Mathf.Sin(t*8f)*Mathf.Sin(Mathf.Clamp01(t/seconds)*Mathf.PI);
                if(mode==CatEnrichmentMode.Track)
                    movingPart.localRotation=movingRotation*Quaternion.Euler(0,t*145,0);
                else if(mode==CatEnrichmentMode.Roller)
                    movingPart.localRotation=movingRotation*Quaternion.Euler(t*300,0,0);
                else if(mode==CatEnrichmentMode.Chase||mode==CatEnrichmentMode.Ribbon)
                {
                    movingPart.localPosition=movingPosition+new Vector3(wave*footprint.x*.16f,0,Mathf.Sin(t*4)*footprint.y*.05f);
                    movingPart.localRotation=movingRotation*Quaternion.Euler(0,wave*22,0);
                }
                else if(mode==CatEnrichmentMode.Feed)
                    movingPart.localPosition=movingPosition+Vector3.up*(Mathf.Max(0,wave)*.035f);
                else movingPart.localRotation=movingRotation*Quaternion.Euler(wave*12,0,wave*8);
            }
            yield return null;
        }
    }
    IEnumerator Walk(Vector3 target,CatActivityPose pose,bool preserveHeading=false)
    {
        if (mode == CatEnrichmentMode.Chase) target.y = startPosition.y;
        Vector3 from=Cat.transform.position;
        // Paths can contain the current position, including the final tunnel
        // exit. Do not play a quarter-second walking loop without traveling.
        if(Vector3.Distance(from,target)<=.005f)yield break;
        if(!preserveHeading)yield return Face(target,pose==CatActivityPose.Crawl);
        PlayCatPose(pose);
        while(Vector3.Distance(Cat.transform.position,target)>.005f)
        {
            Cat.transform.position=Vector3.MoveTowards(Cat.transform.position,target,1.5f*Time.deltaTime);
            yield return null;
        }
        Cat.transform.position=target;
    }
    IEnumerator Face(Vector3 target,bool stayLow=false)
    {
        Vector3 direction=target-Cat.transform.position;direction.y=0;
        if(direction.sqrMagnitude<=.001f)yield break;
        Quaternion facing=Quaternion.LookRotation(direction);
        float angle=Quaternion.Angle(Cat.transform.rotation,facing);
        if(angle>1f)
        {
            // Hold a neutral pose during the shortest turn. Inside a tunnel
            // keep the crawl envelope instead of lifting the head through it.
            poseDriver.SetTimedPose(stayLow?CatActivityPose.Crawl:CatActivityPose.Sniff,0);
            yield return CatActivityFacing.Turn(Cat,facing,Mathf.Max(.08f,angle/540f));
        }
        Cat.transform.rotation=facing;
    }
    void Restore(bool cancelled)
    {
        IsPerformingGesture=false;
        IsResting=false;
        toyContact?.Clear();
        springMotion?.Clear();mouseMotion?.Clear();
        RestoreTail();tailBones=null;
        if(!captured)return;captured=false;
        if(movingPart!=null&&(!IsMouse||cancelled)){movingPart.localPosition=movingPosition;movingPart.localRotation=movingRotation;}
        if(Cat!=null)
        {
            // Shared prepared recovery validates the current and accepted poses.
            Cat.transform.localScale=originalScale;Cat.SetMovementLocked(this,false);
        }
        if(controller!=null)controller.enabled=controllerWasEnabled;
    }
    protected override void CancelActivity(){if(!IsRunning)return;StopAllCoroutines();if(!HasBegunActivity){base.CancelActivity();return;}Restore(true);base.CancelActivity();}
#if UNITY_EDITOR
    public void EditorConfigureFoodSurface(Vector2 radii){foodSurfaceRadii=radii;}
    public void EditorConfigureEnrichment(CatEnrichmentMode value,Transform contact,Transform exit,Transform moving,Vector2 size)
    {mode=value;contactPoint=contact;exitPoint=exit;movingPart=moving;footprint=size;}
#endif
}
