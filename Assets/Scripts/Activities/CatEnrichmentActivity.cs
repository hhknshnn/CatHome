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
    float lastImpact=-100f;
    public int ContactCount { get; private set; }
    public const float RestEnergyPerSecond = .75f;
    public bool IsResting { get; private set; }
    public override float EnergyCost => mode == CatEnrichmentMode.Nap ? 0f : base.EnergyCost;

    public CatEnrichmentMode Mode=>mode;
    public Transform ContactPoint=>contactPoint;
    public Transform ExitPoint=>exitPoint;
    public Transform MovingPart=>movingPart;
    public override string ProgressLabel=>!IsRunning?string.Empty:mode==CatEnrichmentMode.Nap?
        (GameLanguageService.Current==GameLanguage.Turkish?"Dinleniyor · Enerji topluyor":"Resting · Recovering energy"):
        (GameLanguageService.Current==GameLanguage.Turkish?"Birlikte oyun zamanı":"Playtime together");

    protected override bool CanBeginActivity(out string failureReason)
    {
        failureReason=string.Empty;
        if(contactPoint==null||exitPoint==null||RoutineEntryPoint==null){failureReason="THE TOY IS NOT READY";return false;}
        Physics.SyncTransforms();
        if(!CatActivityMotion.TryFloorPath(Cat.transform.position,RoutineEntryPoint.position,out approach))
        {failureReason="LET'S GET A LITTLE CLOSER!";return false;}
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
        poseDriver=Cat.GetComponent<CatActivityAnimation>(); ContactCount=0;lastImpact=-100;
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
            Vector3 working=RoutineEntryPoint.position+transform.forward*(lowPlay?.42f:.22f);
            yield return Walk(working,CatActivityPose.Walk);Face(contactPoint.position);
        }

        if(mode==CatEnrichmentMode.Tunnel)
        {
            // Stay low inside the arch; a standing paw-swat would put the head
            // through the fabric even after a safe walk-in.
            PlayCatPose(CatActivityPose.Crawl);yield return React(.35f);
            // A newly placed obstacle can close the far end during play. Back
            // out through the entrance while still low instead of walking tall
            // back through the cloth or moving through the obstruction.
            yield return Walk(CatActivityMotion.IsFloorClear(exitPoint.position,.25f)?exitPoint.position:entry,CatActivityPose.Crawl);
        }
        else if(mode==CatEnrichmentMode.Nap||mode==CatEnrichmentMode.Hide)
        {
            Face(entry);
            PlayCatPose(CatActivityPose.Sleep,contactPoint);yield return React(.25f);
            IsResting=mode==CatEnrichmentMode.Nap;
            yield return React(duration); IsResting=false;
            yield return Walk(entry,CatActivityPose.Crawl);
        }
        else if(mode==CatEnrichmentMode.Chase)
        {
            Vector3 center=Cat.transform.position;
            for(int beat=0;beat<2;beat++)
            {
                float side=(beat==0?-1:1)*footprint.x*.16f;
                yield return Walk(center+transform.right*side,CatActivityPose.Stalk);
                Face(contactPoint.position);
                yield return Gesture(CatActivityPose.Pounce,1.05f,false);
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
        Vector3 exit=mode==CatEnrichmentMode.Tunnel?Cat.transform.position:entry;
        if(!CatActivityMotion.IsFloorClear(exit,.25f))exit=startPosition;
        yield return Walk(exit,CatActivityPose.Walk);
        Restore(false);
        CompleteActivity(GameLanguageService.Current==GameLanguage.Turkish?"Çok iyi geldi!":"That felt good!");
    }
    IEnumerator Gesture(CatActivityPose pose,float seconds,bool contact)
    {
        toyContact.Clear();float t=0;bool hit=false;
        while(t<seconds)
        {
            float phase=Mathf.Clamp01(t/seconds);
            poseDriver.SetTimedPose(pose,phase);
            if(contact)
            {
                toyContact.Reach(contactPoint.position,pose!=CatActivityPose.BatRight,phase);
                // The response starts after the real, breed-scaled paw arrives.
                // Merely entering an animation state is not a hit.
                if(!hit && phase>=.34f && phase<.75f && toyContact.Distance<.085f)
                { hit=true;ContactCount++;lastImpact=Time.time; }
            }
            AnimateAfterContact(); t+=Time.deltaTime;yield return null;
        }
        toyContact.Clear();
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
        PlayCatPose(pose);
        Vector3 from=Cat.transform.position;
        float seconds=Mathf.Max(.25f,Vector3.Distance(from,target)/1.5f),t=0;
        Face(target);
        while(t<seconds)
        {
            t+=Time.deltaTime;Cat.transform.position=Vector3.Lerp(from,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds)));
            yield return null;
        }
        Cat.transform.position=target;
    }
    void Face(Vector3 target)
    {
        Vector3 direction=target-Cat.transform.position;direction.y=0;
        if(direction.sqrMagnitude>.001f)Cat.transform.rotation=Quaternion.LookRotation(direction);
    }
    void Restore(bool cancelled)
    {
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
    protected override void CancelActivity(){StopAllCoroutines();Restore(true);base.CancelActivity();}
    protected override void OnDisable(){StopAllCoroutines();Restore(true);base.OnDisable();}
#if UNITY_EDITOR
    public void EditorConfigureEnrichment(CatEnrichmentMode value,Transform contact,Transform exit,Transform moving,Vector2 size)
    {mode=value;contactPoint=contact;exitPoint=exit;movingPart=moving;footprint=size;}
#endif
}
