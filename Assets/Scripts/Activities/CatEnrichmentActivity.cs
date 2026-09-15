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
    List<Vector3> approach, workApproach;
    bool captured;
    Transform[] tailBones;
    Quaternion[] tailPose;
    bool tailAdjusted;
    float tailBlend;
    CatToyContactMotion toyContact;
    CatActivityAnimation poseDriver;
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
    public Vector3 ContactPosition => IsRunning ? touchPosition : contactPoint != null ? contactPoint.position : transform.position;
    public int ContactCount { get; private set; }
    public bool IsPerformingGesture { get; private set; }
    public float ClosestPawDistance { get; private set; }
    public const float RestEnergyPerSecond = .75f;
    public bool IsResting { get; private set; }
    public override float EnergyCost => mode == CatEnrichmentMode.Nap ? 0f : base.EnergyCost;
    public override bool SupportsContinuousRest=>mode==CatEnrichmentMode.Nap;

    public CatEnrichmentMode Mode=>mode;
    public Transform ContactPoint=>contactPoint;
    public Transform ExitPoint=>exitPoint;
    public Transform MovingPart=>movingPart;
    public override bool TryGetPromptDistance(CatMovement cat, out float distance)
    {
        bool front = base.TryGetPromptDistance(cat, out distance);
        if (mode != CatEnrichmentMode.Tunnel) return front;
        bool back = TryPromptAt(cat, exitPoint, out float other);
        if (back && (!front || other < distance)) distance = other;
        return front || back;
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
        Physics.SyncTransforms();
        touchPosition = contactPoint.position;
        workPoint = RoutineFloorPosition;
        workApproach = null;
        reverseTunnel=false;
        if(mode==CatEnrichmentMode.Tunnel)
        {
            bool front=CatActivityMotion.TryFloorPath(Cat.transform.position,RoutineEntryPoint.position,out var frontPath);
            bool back=CatActivityMotion.TryFloorPath(Cat.transform.position,exitPoint.position,out var backPath);
            if(!front && !back){failureReason="LET'S GET A LITTLE CLOSER!";return false;}
            reverseTunnel=back && (!front || PathLength(backPath)<PathLength(frontPath));
            approach=reverseTunnel?backPath:frontPath;return true;
        }
        if (mode != CatEnrichmentMode.Nap && mode != CatEnrichmentMode.Hide)
        {
            if(!TryNearbyContact()) { failureReason="LET'S GET A LITTLE CLOSER!";return false; }
        }
        if(!CatActivityMotion.TryFloorPath(Cat.transform.position,RoutineFloorPosition,out approach))
        {failureReason="LET'S GET A LITTLE CLOSER!";return false;}
        return true;
    }
    bool TryNearbyContact()
    {
        if(IsCeramicMeal)return TryNearbyMeal();
        Bounds bounds=new Bounds(contactPoint.position,Vector3.zero);bool measured=false;
        if(movingPart!=null)
            foreach(var renderer in movingPart.GetComponentsInChildren<Renderer>())
            {
                if(!renderer.enabled)continue;
                if(!measured){bounds=renderer.bounds;measured=true;}else bounds.Encapsulate(renderer.bounds);
            }
        Vector3 origin=RoutineFloorPosition;
        Vector3 outward=origin-bounds.center;outward.y=0;
        if(outward.sqrMagnitude<.001f)outward=-transform.forward;
        outward.Normalize();
        float reachDistance=mode==CatEnrichmentMode.Spring||mode==CatEnrichmentMode.Grass?.30f:
            StoreProductId==HomeStoreService.TreatJarId||StoreProductId==HomeStoreService.KibbleBagId?.37f:.39f;
        float best=float.PositiveInfinity;
        // Rank only sides that keep the real contact facing front/side to the
        // player. Rotating the cat after choosing a rear contact would miss it.
        Vector3 camera=CatActivityFacing.CameraPosition(Cat);
        for(int i=0;i<24;i++)
        {
            int step=(i+1)/2*(i%2==0?-1:1);
            Vector3 side=Quaternion.Euler(0,step*15f,0)*outward;
            Vector3 probe=bounds.center+side*2f;probe.y=contactPoint.position.y;
            Vector3 touch=bounds.ClosestPoint(probe);
            Vector3 stand=touch+side*reachDistance;stand.y=0;
            if(CatActivityFacing.FacingDot(touch-stand,stand,camera)<CatActivityFacing.MinimumViewDot)continue;
            if(!CatActivityMotion.IsFloorClear(stand))continue;
            if(!CatActivityMotion.TryFloorPath(origin,stand,out var path))continue;
            float length=0;Vector3 last=origin;
            foreach(var point in path){length+=Vector3.Distance(last,point);last=point;}
            if(length>2.2f||length>=best)continue;
            best=length;workPoint=stand;touchPosition=touch;workApproach=path;
        }
        return workApproach!=null;
    }
    bool TryNearbyMeal()
    {
        var tag=Cat.GetComponentInChildren<CatBreedVisualTag>();
        var profile=CatFeedingAlignmentCatalog.Load()?.Find(tag!=null?tag.BreedId:CatBreedService.SelectedBreedId);
        if(profile==null||foodSurfaceRadii.x<=0||foodSurfaceRadii.y<=0)return false;
        Vector3 origin=RoutineFloorPosition,outward=origin-contactPoint.position;outward.y=0;
        if(outward.sqrMagnitude<.001f)outward=-transform.forward;
        outward.Normalize();float best=float.PositiveInfinity;
        Vector3 camera=CatActivityFacing.CameraPosition(Cat);
        Vector3 mouth=profile.mouthOffset*(Cat.transform.lossyScale.y/.5f);mouth.y=0;
        mouth.z+=.035f*(Cat.transform.lossyScale.y/.5f);
        for(int i=0;i<24;i++)
        {
            int step=(i+1)/2*(i%2==0?-1:1);
            Vector3 side=Quaternion.Euler(0,step*15f,0)*outward;
            Vector3 local=contactPoint.InverseTransformDirection(side);
            float radius=1f/Mathf.Sqrt(local.x*local.x/(foodSurfaceRadii.x*foodSurfaceRadii.x)+local.z*local.z/(foodSurfaceRadii.y*foodSurfaceRadii.y));
            Vector3 touch=contactPoint.TransformPoint(local*(radius*.60f));
            var heading=Quaternion.LookRotation(-side);
            Vector3 stand=touch-heading*mouth;stand.y=0;
            if(CatActivityFacing.FacingDot(-side,stand,camera)<CatActivityFacing.PreferredViewDot||
               !CatActivityMotion.IsFloorClear(stand,.075f))continue;
            // The real paws fit beside the bowl; the broad walking controller
            // approaches and is restored only on the clear floor outside it.
            for(float back=.12f;back<=.60f;back+=.08f)
            {
                Vector3 release=stand+side*back;
                if(!CatActivityMotion.IsControllerFloorClear(Cat,release)||
                   !CatActivityMotion.ClearSegment(release,stand,.075f)||
                   !CatActivityMotion.TryFloorPath(Cat,origin,release,out var path))continue;
                float length=PathLengthFrom(origin,path)+back;
                if(length<best&&length<=2.2f)
                {
                    path.Add(stand);best=length;workPoint=stand;touchPosition=touch;
                    workApproach=path;mealReleasePoint=release;mealHeading=heading;
                }
                break;
            }
        }
        return workApproach!=null;
    }
    static float PathLengthFrom(Vector3 origin,List<Vector3> path)
    {float result=0;foreach(var point in path){result+=Vector3.Distance(origin,point);origin=point;}return result;}
    float PathLength(List<Vector3> path)
    {float distance=0;Vector3 last=Cat.transform.position;foreach(var p in path){distance+=Vector3.Distance(last,p);last=p;}return distance;}
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
        foreach(var target in approach)yield return Walk(target,CatActivityPose.Walk);
        Vector3 entry=Cat.transform.position;
        bool enter=mode==CatEnrichmentMode.Nap||mode==CatEnrichmentMode.Hide||mode==CatEnrichmentMode.Tunnel;
        if(enter)yield return Walk(contactPoint.position,CatActivityPose.Crawl);
        else
        {
            // Move from the clear approach lane into paw reach of the actual toy.
            bool lowPlay=mode==CatEnrichmentMode.Track||mode==CatEnrichmentMode.Ribbon||mode==CatEnrichmentMode.Roller;
            Vector3 working=workApproach!=null?workPoint:RoutineEntryPoint.position+transform.forward*(lowPlay?.42f:.22f);
            if(workApproach!=null)
            {foreach(var point in workApproach)yield return Walk(point,CatActivityPose.Walk);}
            else yield return Walk(working,CatActivityPose.Walk);
            yield return Face(IsCeramicMeal?Cat.transform.position+mealHeading*Vector3.forward:touchPosition);
        }

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
            Vector3 restFacing=entry;
            if(mode==CatEnrichmentMode.Nap)
            {
                // Keep the bed's safe lengthwise axis, choosing the visible end.
                // A rear doorway need not make the resting cat face the doorway.
                Vector3 axis=entry-Cat.transform.position;axis.y=0;
                if(axis.sqrMagnitude>.001f)
                    restFacing=Cat.transform.position+CatActivityFacing.AlongAxis(Cat,contactPoint.position,Quaternion.LookRotation(axis))*Vector3.forward;
            }
            yield return Face(restFacing,true);
            PlayCatPose(CatActivityPose.Sleep,contactPoint);yield return React(.25f);
            IsResting=mode==CatEnrichmentMode.Nap;
            if(mode==CatEnrichmentMode.Nap){while(KeepResting)yield return null;}
            else yield return React(duration);
            IsResting=false;
            yield return Walk(entry,CatActivityPose.Crawl);
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
    void AnimateAfterContact()
    {
        if(movingPart==null)return;
        float t=Time.time-lastImpact;
        if(t>2)return;
        float damping=Mathf.Exp(-t*2.4f),wave=Mathf.Sin(t*13)*damping;
        if(mode==CatEnrichmentMode.Track)
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
    IEnumerator Walk(Vector3 target,CatActivityPose pose)
    {
        if (mode == CatEnrichmentMode.Chase) target.y = startPosition.y;
        Vector3 from=Cat.transform.position;
        // Paths can contain the current position, including the final tunnel
        // exit. Do not play a quarter-second walking loop without traveling.
        if(Vector3.Distance(from,target)<=.005f)yield break;
        yield return Face(target,pose==CatActivityPose.Crawl);
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
        RestoreTail();tailBones=null;
        if(!captured)return;captured=false;
        if(movingPart!=null){movingPart.localPosition=movingPosition;movingPart.localRotation=movingRotation;}
        if(Cat!=null)
        {
            if(cancelled){Cat.transform.position=startPosition;Cat.transform.rotation=startRotation;}
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
